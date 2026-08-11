using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Stream initialization: the odd increment, the two priming advances, and the primed state the
/// first caller-visible draw reads.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-003</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract, the initialization rule: "Initialize PCG32 with state zero and increment
/// <c>(selector shifted left one bit) OR 1</c>; advance once, add <c>state seed</c> to state
/// modulo <c>2^64</c>, and advance once again before returning the first caller-visible value."
/// Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-stream-initialization.txt</c>.
/// </para>
/// <para>
/// This is a separate gate from <c>VER-SIM-005-001</c> because initialization and output are
/// separately wrong-able: a generator with the right transformation and one priming advance
/// instead of two produces a perfectly self-consistent stream that no output-only assertion
/// distinguishes.
/// </para>
/// <para>
/// The pinned streams are read out of the fixture with the master seed, family key, and
/// instance key it declares, so production is driven from the fixture's own inputs and observed
/// through <see cref="RandomStreamSet"/> - the only way to observe a stream, since
/// <c>Pcg32</c> is internal and is never handed out.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class Pcg32InitializationGoldenVectorTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-003</c>. Increment and primed state match the committed
    /// vector, and the priming advances are excluded from the caller-visible draw count.
    /// </summary>
    [Test]
    public void InitializationMatchesTheCommittedVector()
    {
        string rendered = RandomGoldenHeaders.StreamInitialization
            + RandomVectorRendering.StreamInitializationBody(ProductionVectorEngine.Instance);
        GoldenText.Matches(RandomVectorRendering.StreamInitializationGolden, rendered);

        IReadOnlyList<PinnedStream> pinned = ReadPinnedStreams();
        Assert.That(pinned, Has.Count.EqualTo(5), "the fixture declares streams=5");

        Expect.Multiple(() =>
        {
            foreach (PinnedStream pinnedStream in pinned)
            {
                RandomStreamSet set = new(RandomSchemaVersion.Current, pinnedStream.MasterSeed);
                RandomStreamKey key = RandomStreamKey.Create(pinnedStream.FamilyKey, pinnedStream.InstanceKey);

                ulong derivedStateSeed = SeedDerivation.DeriveStateSeed(
                    SeedDerivation.DeriveD1(
                        SeedDerivation.DeriveD0(RandomSchemaVersion.Current, pinnedStream.MasterSeed),
                        pinnedStream.FamilyKey),
                    pinnedStream.InstanceKey);
                ulong derivedSelector = SeedDerivation.DeriveSelector(derivedStateSeed);

                Assert.That(
                    derivedStateSeed,
                    Is.EqualTo(pinnedStream.StateSeed),
                    pinnedStream.Label + ": derived state seed matches the committed vector");
                Assert.That(
                    derivedSelector,
                    Is.EqualTo(pinnedStream.Selector),
                    pinnedStream.Label + ": derived selector matches the committed vector");
                Assert.That(
                    set.IncrementOf(key),
                    Is.EqualTo(unchecked((pinnedStream.Selector << 1) | 1UL)),
                    pinnedStream.Label + ": increment is (selector << 1) | 1");
                Assert.That(
                    set.IncrementOf(key),
                    Is.EqualTo(pinnedStream.Increment),
                    pinnedStream.Label + ": increment matches the committed vector");
                Assert.That(
                    set.IncrementOf(key) % 2UL,
                    Is.EqualTo(1UL),
                    pinnedStream.Label + ": the per-stream increment is odd");
                Assert.That(
                    set.StateOf(key),
                    Is.EqualTo(pinnedStream.PrimedState),
                    pinnedStream.Label + ": primed state matches the committed vector");
                Assert.That(
                    set.DrawCountOf(key),
                    Is.EqualTo(0UL),
                    pinnedStream.Label + ": the two priming advances are not caller-visible draws");

                // The state after only the first advance and the addition. The document requires a
                // second advance, so the primed state must not be this.
                ulong afterOneAdvanceAndAddition = unchecked(
                    pinnedStream.StateSeed + set.IncrementOf(key));
                Assert.That(
                    set.StateOf(key),
                    Is.Not.EqualTo(afterOneAdvanceAndAddition),
                    pinnedStream.Label + ": a single priming advance is not enough; the document advances twice");
                Assert.That(
                    set.StateOf(key),
                    Is.Not.EqualTo(pinnedStream.StateSeed),
                    pinnedStream.Label + ": the state seed is added to the state, it is not the state");
                Assert.That(
                    set.StateOf(key),
                    Is.Not.Zero,
                    pinnedStream.Label + ": initialization starts at state zero but does not stay there");
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-003</c>. An even increment is refused rather than used.
    /// </summary>
    /// <remarks>
    /// doc 20 § Authoritative random-number contract requires a per-stream
    /// <em>odd</em> increment; an even one halves the generator's period and cannot be produced
    /// by the documented <c>(selector &lt;&lt; 1) | 1</c>. Accepting one on recovery would
    /// resume a run onto a stream this contract cannot have created, so the recovery record
    /// refuses to exist at all - the check is in the constructor, not in the consumer.
    /// </remarks>
    [Test]
    public void AnEvenIncrementIsRefused()
    {
        ArgumentException failure = Expect.Throws<ArgumentException>(
            () => new RandomStreamRecoveryState(
                RandomSchemaVersion.Current,
                0x0100,
                0UL,
                0x0123456789ABCDEFUL,
                0x1000000000000000UL));

        Expect.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("odd"));
            Expect.DoesNotThrow(() => new RandomStreamRecoveryState(
                RandomSchemaVersion.Current,
                0x0100,
                0UL,
                0x0123456789ABCDEFUL,
                0x1000000000000001UL));
        });
    }

    private static IReadOnlyList<PinnedStream> ReadPinnedStreams()
    {
        string[] lines = RandomGoldenFile
            .Body(RandomVectorRendering.StreamInitializationGolden)
            .Split('\n');

        List<PinnedStream> streams = new();
        string label = string.Empty;
        ulong masterSeed = 0UL;
        ushort familyKey = 0;
        ulong instanceKey = 0UL;
        ulong stateSeed = 0UL;
        ulong selector = 0UL;
        ulong increment = 0UL;

        foreach (string line in lines)
        {
            string[] fields = line.Split('\t');
            switch (fields[0])
            {
                case "stream":
                    label = line.Replace("\t", " ", StringComparison.Ordinal);
                    masterSeed = ParseHex(fields[1], "master=");
                    familyKey = (ushort)ParseHex(fields[2], "family=");
                    instanceKey = ParseHex(fields[3], "instance=");
                    break;
                case "state-seed":
                    stateSeed = ParseHex(fields[1], string.Empty);
                    break;
                case "selector":
                    selector = ParseHex(fields[1], string.Empty);
                    break;
                case "increment":
                    increment = ParseHex(fields[1], string.Empty);
                    break;
                case "primed-state":
                    streams.Add(new PinnedStream(
                        label,
                        masterSeed,
                        familyKey,
                        instanceKey,
                        stateSeed,
                        selector,
                        increment,
                        ParseHex(fields[1], string.Empty)));
                    break;
                default:
                    break;
            }
        }

        return streams;
    }

    private static ulong ParseHex(string field, string prefix)
    {
        string digits = field.Substring(prefix.Length + 2);
        return ulong.Parse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    /// <summary>One stream block of the committed initialization vector.</summary>
    private sealed class PinnedStream
    {
        internal PinnedStream(
            string label,
            ulong masterSeed,
            ushort familyKey,
            ulong instanceKey,
            ulong stateSeed,
            ulong selector,
            ulong increment,
            ulong primedState)
        {
            this.Label = label;
            this.MasterSeed = masterSeed;
            this.FamilyKey = familyKey;
            this.InstanceKey = instanceKey;
            this.StateSeed = stateSeed;
            this.Selector = selector;
            this.Increment = increment;
            this.PrimedState = primedState;
        }

        internal string Label { get; }

        internal ulong MasterSeed { get; }

        internal ushort FamilyKey { get; }

        internal ulong InstanceKey { get; }

        internal ulong StateSeed { get; }

        internal ulong Selector { get; }

        internal ulong Increment { get; }

        internal ulong PrimedState { get; }
    }
}
