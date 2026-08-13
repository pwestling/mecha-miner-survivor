using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Stream independence: one family's draws never shift another's, and instance keys separate
/// streams within a family.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-011</c>, <c>VER-SIM-005-012</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract): "A category retry or an added
/// visual draw cannot consume another family's sequence." Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-stream-independence.txt</c>.
/// </para>
/// <para>
/// This is also the gate for the copy-fork hazard. <c>Pcg32</c> is a mutable struct whose
/// identity is its advancing state, so a stream copied into a local silently forks: the copy
/// replays values the original already produced. <c>RandomStreamSet</c> holds streams in an
/// array and mutates elements in place; a fork would show up here as a source whose draws do
/// not advance the stored state.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RandomStreamIndependenceTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-011</c>. Extra draws in one family leave every other
    /// family's sequence bit-identical, and a source is a view of its stream rather than a
    /// copy.
    /// </summary>
    [Test]
    public void AnExtraDrawInOneFamilyShiftsNoOtherFamily()
    {
        // Checked first, and deliberately outside the Expect.Multiple block below, so that a fork
        // fails fast and names what diverged. A fork used to be reported by the run never
        // finishing: every draw becomes a copy of one value, and rejection sampling cannot
        // terminate against a source that does not advance. A check that fails identically whether
        // the property was violated or the machine died trains people to re-run it, so the timeout
        // is now an assertion. Expect.Multiple would defeat that by continuing into the statements
        // that spin.
        AssertASourceIsAViewOfItsStreamRatherThanACopy();

        GoldenText.Matches(
            RandomVectorRendering.StreamIndependenceGolden,
            RandomGoldenHeaders.StreamIndependence
                + RandomVectorRendering.StreamIndependenceBody(ProductionVectorEngine.Instance));

        IReadOnlyList<ushort> familyKeys = RegisteredKeys();
        Dictionary<ushort, uint> undisturbed = FirstOutputs(familyKeys, 0x0000, 0);
        Dictionary<ushort, uint> afterExtraDraws = FirstOutputs(familyKeys, 0x0202, 5);

        Expect.Multiple(() =>
        {
            // Every family's first output is unchanged by five extra draws taken from 0x0202,
            // including 0x0202's neighbours 0x0201 and 0x0203 - an off-by-one in the registry
            // would show up exactly there.
            foreach (ushort key in familyKeys)
            {
                if (key == 0x0202)
                {
                    continue;
                }

                Assert.That(
                    afterExtraDraws[key],
                    Is.EqualTo(undisturbed[key]),
                    "0x" + key.ToString("X4", CultureInfo.InvariantCulture)
                        + " must not shift when 0x0202 draws extra values (doc 20 § Authoritative random-number contract)");
            }

            // Independence is not vacuous: no two families share a first output, so the family key
            // really separates the sequences.
            Dictionary<uint, ushort> byOutput = new();
            foreach (ushort key in familyKeys)
            {
                Assert.That(
                    byOutput.TryAdd(undisturbed[key], key),
                    Is.True,
                    "0x" + key.ToString("X4", CultureInfo.InvariantCulture)
                        + " collides with 0x"
                        + (byOutput.TryGetValue(undisturbed[key], out ushort other)
                            ? other.ToString("X4", CultureInfo.InvariantCulture)
                            : "?"));
            }

            // The extra draws did advance 0x0202 itself, so the control is live.
            Assert.That(
                afterExtraDraws[0x0202],
                Is.Not.EqualTo(undisturbed[0x0202]),
                "the disturbed family must itself have moved, or the test proves nothing");

            // Finally, the accessibility that makes a fork impossible rather than merely
            // untested. No caller can obtain a stream value at all: the generator type is not
            // exported, so there is nothing to copy into a local, no ref-return to assign, and
            // no property or tuple element that could hand one over. This assertion exists to
            // fail if that ever changes - the assertions above would still pass while the hazard
            // quietly became available again.
            List<string> exported = new();
            foreach (Type type in typeof(RandomStreamSet).Assembly.GetExportedTypes())
            {
                if (string.Equals(
                    type.Namespace,
                    typeof(RandomStreamSet).Namespace,
                    StringComparison.Ordinal))
                {
                    exported.Add(type.Name);
                }
            }

            exported.Sort(StringComparer.Ordinal);
            Assert.That(
                exported,
                Is.EqualTo(new[]
                {
                    "BoundedRandom",
                    "CanonicalSelection",
                    "IRandomSource",
                    "InstanceKeyRule",
                    "RandomSchemaVersion",
                    "RandomStreamFamilies",
                    "RandomStreamFamily",
                    "RandomStreamKey",
                    "RandomStreamRecoveryState",
                    "RandomStreamSet",
                    "ScriptedRandomSource",
                    "SeedDerivation",
                }),
                "the public contract of MechaMiner.Simulation.Random, and Pcg32 is deliberately "
                    + "not in it: a stream value that cannot leave the assembly cannot be forked");

            foreach (string typeName in exported)
            {
                Assert.That(typeName, Is.Not.EqualTo("Pcg32"));
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-012</c>. Instance keys separate streams, the same instance
    /// key reproduces its stream, and a zero-key family refuses a nonzero instance key.
    /// </summary>
    [Test]
    public void InstanceKeysSeparateStreamsWithinAFamily()
    {
        uint[] instanceThree = Sequence(0x0220, 3UL, 16);
        uint[] instanceFour = Sequence(0x0220, 4UL, 16);
        uint[] instanceThreeAgain = Sequence(0x0220, 3UL, 16);

        Expect.Multiple(() =>
        {
            Assert.That(
                instanceThreeAgain,
                Is.EqualTo(instanceThree),
                "the same master seed, family, and instance key reproduce the same sequence");
            Assert.That(
                instanceFour[0],
                Is.Not.EqualTo(instanceThree[0]),
                "two instance keys share no prefix, which is what makes instance separation visible");

            for (int index = 0; index < instanceThree.Length; index++)
            {
                Assert.That(
                    instanceFour[index],
                    Is.Not.EqualTo(instanceThree[index]),
                    "instance 3 and 4 differ at output " + index.ToString(CultureInfo.InvariantCulture));
            }

            // Pinned by random-stream-initialization.txt, whose last two streams differ only in
            // instance key.
            Assert.That(instanceThree[0], Is.EqualTo(0x3504DADDU));
            Assert.That(instanceFour[0], Is.EqualTo(0x5BD716A4U));

            // A family whose registered instance key is zero rejects a nonzero one rather than
            // deriving an unregistered stream (doc 20 § Authoritative random-number contract).
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0100, 1UL));
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0200, 0xFFUL));
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0230, 1UL));
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0100, 0UL));

            // The ranges doc 20 states as closed are enforced too: canonical material ordinal 0-5
            // (doc 20 § Authoritative random-number contract), scheduled boss index 0-3 (), weapon slot ordinal 0-3
            // (doc 20 § Authoritative random-number contract).
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0220, 5UL));
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0220, 6UL));
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0303, 3UL));
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0303, 4UL));
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0400, 3UL));
            Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(0x0400, 4UL));

            // Identifier-shaped instance keys are unbounded, because their canonical derivation
            // belongs to the owning system (doc 20 § Authoritative random-number contract).
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0250, 0xDEADBEEFUL));
            Expect.DoesNotThrow(() => RandomStreamKey.Create(0x0410, ulong.MaxValue));

            // Two instances of one family are two streams in one set, not one shared stream.
            RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
            RandomStreamKey three = RandomStreamKey.Create(0x0220, 3UL);
            RandomStreamKey four = RandomStreamKey.Create(0x0220, 4UL);
            _ = set.NextUInt32(three);
            _ = set.NextUInt32(three);
            Assert.That(set.DrawCountOf(four), Is.Zero, "drawing from instance 3 does not move instance 4");
            Assert.That(set.NextUInt32(four), Is.EqualTo(0x5BD716A4U));
            Assert.That(set.InstantiatedKeys, Has.Count.EqualTo(2));
        });
    }

    /// <summary>
    /// A source is a view onto the stored stream, not a copy of it, and the primitive that used to
    /// spin against a copy now reports instead.
    /// </summary>
    /// <remarks>
    /// Every assertion here fails fast rather than aggregating, because the defect it detects is the
    /// one that used to consume the whole run: if the set handed out a copy, or if any internal step
    /// copied the struct into a local, the stored state would not advance, two sources would replay
    /// the same values, and any bounded draw would reject forever.
    /// </remarks>
    private static void AssertASourceIsAViewOfItsStreamRatherThanACopy()
    {
        RandomStreamSet set = new(
            RandomSchemaVersion.Current,
            RandomVectorRendering.FixtureMasterSeed);
        RandomStreamKey key0100 = RandomStreamKey.Create(0x0100, 0UL);
        IRandomSource first = set.Source(key0100);
        IRandomSource second = set.Source(key0100);
        ulong primedState = set.StateOf(key0100);

        uint fromFirst = first.NextUInt32();
        Assert.That(
            set.StateOf(key0100),
            Is.Not.EqualTo(primedState),
            "drawing through a source must advance the stored stream, not a copy of it; a stream "
                + "that does not advance is forked, and every later draw is a copy of "
                + fromFirst.ToString("X8", CultureInfo.InvariantCulture));
        Assert.That(set.DrawCountOf(key0100), Is.EqualTo(1UL));

        uint fromSecond = second.NextUInt32();
        Assert.That(
            fromSecond,
            Is.Not.EqualTo(fromFirst),
            "two sources for one key are two views of one advancing stream, not two streams; both "
                + "returned " + fromFirst.ToString("X8", CultureInfo.InvariantCulture));
        Assert.That(set.DrawCountOf(key0100), Is.EqualTo(2UL));
        Assert.That(first.DrawCount, Is.EqualTo(2UL), "both views report the same shared count");
        Assert.That(second.DrawCount, Is.EqualTo(2UL));

        // And the two values are the fixture's first two outputs, in order, so the shared
        // stream is the pinned one rather than merely a consistent one.
        Assert.That(fromFirst, Is.EqualTo(0x04552DDAU));
        Assert.That(fromSecond, Is.EqualTo(0x6013D277U));

        // A real stream reaches a bounded draw without exhausting the rejection bound, so the bound
        // is not in the way of correct behaviour.
        Expect.DoesNotThrow(() => set.NextBounded(RandomStreamKey.Create(0x0220, 3UL), 3U));

        // And the primitive that used to spin now names the divergence. A source whose draws never
        // change is exactly what a forked Pcg32 behaves like, and rejection sampling cannot
        // terminate against one; bound 3 has threshold 1, so a constant zero is rejected every
        // time.
        InvalidOperationException notAdvancing = Expect.Throws<InvalidOperationException>(
            () => BoundedRandom.NextBounded(new NonAdvancingSource(0U), 3U));
        Assert.That(
            notAdvancing.Message,
            Does.Contain("not advancing"),
            "the failure must say what diverged rather than time out");
        Assert.That(notAdvancing.Message, Does.Contain("0x00000000"), "and name the repeated draw");
        Assert.That(
            notAdvancing.Message,
            Does.Contain("consecutive draws"),
            "and report the consecutive-rejection count as its evidence, so the message distinguishes "
                + "a non-advancing source from an unlucky one");
    }

    /// <summary>
    /// A source that never advances: every draw is the same value.
    /// </summary>
    /// <remarks>
    /// This is how a forked <c>Pcg32</c> behaves, and it is the input rejection sampling cannot
    /// terminate against. It is a test double rather than a
    /// <see cref="ScriptedRandomSource"/> because a scripted source throws on exhaustion, so it
    /// cannot be made to repeat one value indefinitely.
    /// </remarks>
    private sealed class NonAdvancingSource : IRandomSource
    {
        private readonly uint _value;
        private ulong _drawCount;

        internal NonAdvancingSource(uint value)
        {
            this._value = value;
        }

        public ulong DrawCount => this._drawCount;

        public uint NextUInt32()
        {
            this._drawCount++;
            return this._value;
        }

        public override string ToString()
        {
            return "non-advancing test source at 0x"
                + this._value.ToString("X8", CultureInfo.InvariantCulture);
        }
    }

    private static IReadOnlyList<ushort> RegisteredKeys()
    {
        List<ushort> keys = new();
        foreach (RandomStreamFamily family in RandomStreamFamilies.All)
        {
            keys.Add(family.Key);
        }

        return keys;
    }

    /// <summary>
    /// First output of every registered family, after taking <paramref name="extraDraws"/>
    /// extra draws from <paramref name="disturbedFamilyKey"/> first.
    /// </summary>
    private static Dictionary<ushort, uint> FirstOutputs(
        IReadOnlyList<ushort> familyKeys,
        ushort disturbedFamilyKey,
        int extraDraws)
    {
        RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
        if (extraDraws > 0)
        {
            RandomStreamKey disturbed = RandomStreamKey.Create(disturbedFamilyKey, 0UL);
            for (int draw = 0; draw < extraDraws; draw++)
            {
                _ = set.NextUInt32(disturbed);
            }
        }

        Dictionary<ushort, uint> outputs = new();
        foreach (ushort key in familyKeys)
        {
            outputs[key] = set.NextUInt32(RandomStreamKey.Create(key, 0UL));
        }

        return outputs;
    }

    private static uint[] Sequence(ushort familyKey, ulong instanceKey, int length)
    {
        RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
        RandomStreamKey key = RandomStreamKey.Create(familyKey, instanceKey);
        uint[] outputs = new uint[length];
        for (int index = 0; index < length; index++)
        {
            outputs[index] = set.NextUInt32(key);
        }

        return outputs;
    }
}
