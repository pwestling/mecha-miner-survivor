using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Run recovery for authoritative streams: state and odd increment round-trip, an incompatible
/// schema version is refused, and presentation state is never serialized.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-013</c>, <c>VER-SIM-005-014</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract, :55, :81, :91) and
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary
/// contract registry (<c>CTR-PST-003</c>).
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RandomStreamRecoveryTests
{
    /// <summary>Streams instantiated with different draw counts, so no two are at the same
    /// offset.</summary>
    /// <remarks>
    /// Deliberately <em>not</em> in ascending key order, and <c>0x0220</c> appears twice with its
    /// higher instance key first. Both facts are load-bearing. A fixture that instantiates in
    /// strictly ascending family-key order with one instance per family has insertion order equal
    /// to sorted order, so it passes whether or not the artifact is sorted at all and whether or
    /// not the comparator has an instance-key tiebreak; it pins neither.
    /// </remarks>
    private static readonly (ushort Family, ulong Instance, int Draws)[] LiveStreams =
    {
        (0x0410, ulong.MaxValue, 40),
        (0x0220, 5UL, 3),
        (0x0100, 0UL, 0),
        (0x0303, 2UL, 2),
        (0x0220, 2UL, 6),
        (0x0250, 0xDEADBEEFUL, 11),
        (0x0202, 7UL, 1),
    };

    /// <summary>
    /// A registered authoritative family absent from <see cref="LiveStreams"/>, instantiated last
    /// and sorting into the middle of the artifact, so a sort has to move it.
    /// </summary>
    private const ushort UntouchedFamily = 0x0230;

    /// <summary>
    /// Verification: <c>VER-SIM-005-013</c>. Every instantiated authoritative stream
    /// round-trips and continues the identical sequence.
    /// </summary>
    [Test]
    public void StateAndIncrementRoundTripAndContinueTheSequence()
    {
        RandomStreamSet original = new(
            RandomSchemaVersion.Current,
            RandomVectorRendering.FixtureMasterSeed);
        List<RandomStreamKey> keys = new();
        foreach ((ushort family, ulong instance, int draws) in LiveStreams)
        {
            RandomStreamKey key = RandomStreamKey.Create(family, instance);
            keys.Add(key);

            // Instantiate before drawing, so the zero-draw stream is instantiated too: doc 20 § Authoritative random-number contract
            // scopes recovery to every instantiated stream, including one still at its primed state.
            _ = original.StateOf(key);
            for (int draw = 0; draw < draws; draw++)
            {
                _ = original.NextUInt32(key);
            }
        }

        // Instantiate the last stream without drawing, so "every instantiated stream" includes one
        // that is still at its primed state.
        RandomStreamKey untouched = RandomStreamKey.Create(UntouchedFamily, 0UL);
        keys.Add(untouched);
        _ = original.StateOf(untouched);

        IReadOnlyList<RandomStreamRecoveryState> artifact = original.CaptureRecoveryState();
        RandomStreamSet restored = RandomStreamSet.Restore(
            RandomSchemaVersion.Current,
            RandomVectorRendering.FixtureMasterSeed,
            artifact);

        Expect.Multiple(() =>
        {
            Assert.That(
                artifact,
                Has.Count.EqualTo(keys.Count),
                "doc 20 § Authoritative random-number contract includes every instantiated authoritative stream");

            foreach (RandomStreamRecoveryState record in artifact)
            {
                Assert.That(
                    record.SchemaVersion,
                    Is.EqualTo(RandomSchemaVersion.Current),
                    "each record carries the schema version it was written under (doc 20 § Authoritative random-number contract)");
                Assert.That(
                    record.Increment % 2UL,
                    Is.EqualTo(1UL),
                    record.ToString() + ": the odd increment is part of the record (doc 20 § Authoritative random-number contract)");
            }

            // The artifact is canonical: strictly ascending family key, then instance key. This
            // assertion can see the sort only because LiveStreams instantiates out of ascending
            // order and gives 0x0220 two instances with the higher key first, so insertion order
            // and sorted order are different sequences here.
            for (int index = 1; index < artifact.Count; index++)
            {
                bool ascending = artifact[index - 1].FamilyKey < artifact[index].FamilyKey
                    || (artifact[index - 1].FamilyKey == artifact[index].FamilyKey
                        && artifact[index - 1].InstanceKey < artifact[index].InstanceKey);
                Assert.That(
                    ascending,
                    Is.True,
                    "recovery records are ordered canonically, at " + artifact[index - 1].ToString()
                        + " before " + artifact[index].ToString());
            }

            // And the canonical order is a property of the artifact rather than of the run that
            // produced it: a second set instantiating the same streams with the same draw counts in
            // the reverse order yields the same artifact, value for value. Ordering alone does not
            // establish that - a comparator with no instance-key tiebreak still leaves an ascending
            // artifact, because 0x0220's two instances then compare equal and keep whichever
            // relative order each set inserted them in, which is opposite between these two sets.
            List<(ushort Family, ulong Instance, int Draws)> reversedOrder = new()
            {
                (UntouchedFamily, 0UL, 0),
            };
            for (int index = LiveStreams.Length - 1; index >= 0; index--)
            {
                reversedOrder.Add(LiveStreams[index]);
            }

            Assert.That(
                CaptureInInstantiationOrder(reversedOrder),
                Is.EqualTo(artifact),
                "the artifact's order cannot depend on the order a run happened to instantiate its "
                    + "streams");

            // The round-trip stated as an invariant over the resulting state, not over the calls
            // that produced it: capturing the restored set must yield the same artifact, value for
            // value. This holds regardless of how either set reached its state, so it cannot be
            // satisfied by a restore that happens to replay the same calls.
            Assert.That(
                restored.CaptureRecoveryState(),
                Is.EqualTo(artifact),
                "capture after restore is the identity on the artifact");

            foreach (RandomStreamKey key in keys)
            {
                Assert.That(
                    restored.IsInstantiated(key),
                    Is.True,
                    key.ToString() + " must exist after recovery");
                Assert.That(
                    restored.StateOf(key),
                    Is.EqualTo(original.StateOf(key)),
                    key.ToString() + ": state round-trips exactly");
                Assert.That(
                    restored.IncrementOf(key),
                    Is.EqualTo(original.IncrementOf(key)),
                    key.ToString() + ": odd increment round-trips exactly");
            }

            // The restored stream continues the sequence the original would have produced, rather
            // than restarting it or resuming a re-derived look-alike.
            foreach (RandomStreamKey key in keys)
            {
                for (int draw = 0; draw < 8; draw++)
                {
                    Assert.That(
                        restored.NextUInt32(key),
                        Is.EqualTo(original.NextUInt32(key)),
                        key.ToString() + ": continuation draw "
                            + draw.ToString(CultureInfo.InvariantCulture));
                }
            }

            // A stream not present in the artifact is still derivable afterwards, so recovery does
            // not close the set.
            RandomStreamKey late = RandomStreamKey.Create(0x0301, 21UL);
            Assert.That(restored.IsInstantiated(late), Is.False);
            Assert.That(
                restored.NextUInt32(late),
                Is.EqualTo(original.NextUInt32(late)),
                "a stream instantiated after recovery derives identically from the master seed");
        });
    }

    /// <summary>
    /// Instantiates every stream in <paramref name="order"/>, takes each one's draws, and returns
    /// the recovery artifact, so two instantiation orders over the same streams can be compared.
    /// </summary>
    /// <param name="order">The streams to instantiate, in the order to instantiate them.</param>
    /// <returns>The recovery artifact the resulting set captures.</returns>
    private static IReadOnlyList<RandomStreamRecoveryState> CaptureInInstantiationOrder(
        IReadOnlyList<(ushort Family, ulong Instance, int Draws)> order)
    {
        RandomStreamSet set = new(
            RandomSchemaVersion.Current,
            RandomVectorRendering.FixtureMasterSeed);
        foreach ((ushort family, ulong instance, int draws) in order)
        {
            RandomStreamKey key = RandomStreamKey.Create(family, instance);
            _ = set.StateOf(key);
            for (int draw = 0; draw < draws; draw++)
            {
                _ = set.NextUInt32(key);
            }
        }

        return set.CaptureRecoveryState();
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-014</c>. A different schema version is refused, and the
    /// presentation-only family never reaches the artifact.
    /// </summary>
    [Test]
    public void IncompatibleSchemaVersionIsRejectedAndPresentationStateIsNeverSerialized()
    {
        RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
        RandomStreamKey authoritative = RandomStreamKey.Create(0x0100, 0UL);
        RandomStreamKey presentation = RandomStreamKey.Create(0xF000, 0x5150UL);

        _ = set.NextUInt32(authoritative);
        for (int draw = 0; draw < 5; draw++)
        {
            _ = set.NextUInt32(presentation);
        }

        IReadOnlyList<RandomStreamRecoveryState> artifact = set.CaptureRecoveryState();

        Expect.Multiple(() =>
        {
            // Presentation state is never serialized (doc 20 § Authoritative random-number contract), even though the
            // presentation stream was instantiated and drawn from.
            Assert.That(set.IsInstantiated(presentation), Is.True, "the presentation stream exists");
            Assert.That(set.DrawCountOf(presentation), Is.EqualTo(5UL), "and was drawn from");
            Assert.That(artifact, Has.Count.EqualTo(1), "but only the authoritative stream is captured");
            Assert.That(artifact[0].FamilyKey, Is.EqualTo((ushort)0x0100));
            foreach (RandomStreamRecoveryState record in artifact)
            {
                Assert.That(
                    record.FamilyKey,
                    Is.Not.EqualTo((ushort)0xF000),
                    "doc 20 § Authoritative random-number contract: presentation variation is never serialized into authoritative state");
            }

            // A recovery artifact written under a different schema version is invalidated rather
            // than resumed (doc 20 § Authoritative random-number contract).
            RandomSchemaVersion futureVersion = new(2);
            RandomStreamRecoveryState[] futureArtifact =
            {
                new(futureVersion, 0x0100, 0UL, 0x1122334455667788UL, 0x0000000000000001UL),
            };

            InvalidOperationException incompatible = Expect.Throws<InvalidOperationException>(
                () => RandomStreamSet.Restore(
                    RandomSchemaVersion.Current,
                    RandomVectorRendering.FixtureMasterSeed,
                    futureArtifact));
            Assert.That(incompatible.Message, Does.Contain("random schema version"));
            Assert.That(incompatible.Message, Does.Contain("invalidates incompatible recovery"));

            // The same records under the matching version restore normally, so the refusal is about
            // the version and nothing else.
            RandomStreamRecoveryState[] compatibleArtifact =
            {
                new(RandomSchemaVersion.Current, 0x0100, 0UL, 0x1122334455667788UL, 0x0000000000000001UL),
            };
            Expect.DoesNotThrow(() => RandomStreamSet.Restore(
                RandomSchemaVersion.Current,
                RandomVectorRendering.FixtureMasterSeed,
                compatibleArtifact));

            // An artifact that somehow carries presentation state is refused rather than resumed.
            RandomStreamRecoveryState[] presentationArtifact =
            {
                new(RandomSchemaVersion.Current, 0xF000, 0UL, 0x1122334455667788UL, 0x0000000000000001UL),
            };
            InvalidOperationException refused = Expect.Throws<InvalidOperationException>(
                () => RandomStreamSet.Restore(
                    RandomSchemaVersion.Current,
                    RandomVectorRendering.FixtureMasterSeed,
                    presentationArtifact));
            Assert.That(refused.Message, Does.Contain("presentation-only"));

            // And the schema version is a carried, comparable value rather than a constant, which
            // is the only reason the version check above is possible at all.
            Assert.That(futureVersion, Is.Not.EqualTo(RandomSchemaVersion.Current));
            Assert.That(RandomSchemaVersion.Current.Value, Is.EqualTo(1), "doc 20 § Authoritative random-number contract");
            Assert.That(artifact[0].SchemaVersion, Is.EqualTo(RandomSchemaVersion.Current));
            Expect.Throws<ArgumentOutOfRangeException>(() => new RandomSchemaVersion(0));
        });
    }
}
