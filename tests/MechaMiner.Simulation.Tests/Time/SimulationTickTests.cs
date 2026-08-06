using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The derived-seconds contract: run time comes from the integer tick index, so two runs that
/// reach the same tick through different frame pacing agree bit for bit.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-004</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "Game time is derived from
/// integer tick count, not accumulated floating-point frame deltas."
/// <c>docs/technical/20-simulation-core.md</c> § Numeric and unit conventions gives run time as
/// a "64-bit integer simulation tick plus derived seconds".
/// </remarks>
[TestFixture]
internal sealed class SimulationTickTests
{
    private const string GoldenName = "time-tick-index-derived-seconds.txt";

    /// <summary>
    /// Verification: <c>VER-SIM-001-004</c>.
    ///
    /// An irregular delta stream and a uniform delta stream of equal total elapsed time produce
    /// the identical tick index and the identical derived seconds, bit for bit; and the derived
    /// seconds of a set of tick indices match a golden computed independently of the
    /// implementation.
    /// </summary>
    [Test]
    public void DerivedSecondsComeFromTheTickIndexNotAccumulatedDeltas()
    {
        SimulationTick fromIrregular = RunToCompletion(FrameDeltaStreams.FullRunIrregular());
        SimulationTick fromUniform = RunToCompletion(FrameDeltaStreams.FullRunUniform());

        double accumulatedSeconds = 0.0;
        for (long tick = 0; tick < FrameDeltaStreams.FullRunTicks; tick++)
        {
            accumulatedSeconds += TickRate.SecondsPerTick;
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                FrameDeltaStreams.FullRunTicks,
                fromIrregular.Index,
                "the irregular stream must reach the full run's tick count");
            NumericAssert.AreExactlyEqual(
                fromIrregular.Index,
                fromUniform.Index,
                "two streams of equal total elapsed time reach the same tick index");
            Assert.That(
                BitConverter.DoubleToInt64Bits(fromUniform.Seconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(fromIrregular.Seconds)),
                "derived seconds are a function of the tick index alone, so they are bit-identical "
                    + "however the run was paced (doc 10 § Clock domains)");
            Assert.That(
                BitConverter.DoubleToInt64Bits(fromIrregular.Seconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(FrameDeltaStreams.FullRunSeconds)),
                "126,000 ticks is exactly 2,100 seconds, because the rate is an exact rational");
            Assert.That(
                BitConverter.DoubleToInt64Bits(accumulatedSeconds),
                Is.Not.EqualTo(BitConverter.DoubleToInt64Bits(fromIrregular.Seconds)),
                "accumulating SecondsPerTick 126,000 times drifts away from the derived value, which is "
                    + "why doc 10 forbids deriving game time that way");
        });

        GoldenText.Matches(GoldenName, RenderDerivedSeconds());
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-004</c>.
    ///
    /// The derived value is one division of the index, not an accumulation: every tick's seconds
    /// equals the index over the rational rate exactly, and reaching a tick one step at a time
    /// gives the same bits as constructing it directly.
    /// </summary>
    [Test]
    public void SecondsAreOneDivisionOfTheIndex()
    {
        SimulationTick stepwise = SimulationTick.Zero;
        for (int step = 0; step < 5_000; step++)
        {
            stepwise = stepwise.Next();
        }

        SimulationTick direct = new(5_000);

        Expect.Multiple(() =>
        {
            Assert.That(
                stepwise,
                Is.EqualTo(direct),
                "advancing one tick at a time reaches the same tick as constructing it");
            Assert.That(
                BitConverter.DoubleToInt64Bits(stepwise.Seconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(direct.Seconds)),
                "and therefore the same derived seconds, bit for bit");
            Assert.That(
                BitConverter.DoubleToInt64Bits(direct.Seconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(5_000.0 / 60.0)),
                "the derived value is the single quotient of the index and the rational rate");
        });
    }

    /// <summary>Runs a delta stream through the accumulator and returns the tick it reached.</summary>
    private static SimulationTick RunToCompletion(ImmutableArray<double> elapsedSecondsPerStep)
    {
        FixedStepAccumulator accumulator = new(CatchUpPolicy.Default);
        SimulationTick tick = SimulationTick.Zero;
        foreach (double elapsed in elapsedSecondsPerStep)
        {
            TickBudget budget = accumulator.Advance(elapsed);
            Assert.That(
                budget.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.None),
                "neither comparison stream may discard time; both are inside the catch-up bound");
            tick = tick.Advance(budget.TickCount);
        }

        return tick;
    }

    /// <summary>
    /// Renders the derived seconds of the tick indices the golden pins, as IEEE 754 bit patterns
    /// so the comparison cannot be a formatting difference.
    /// </summary>
    private static string RenderDerivedSeconds()
    {
        long[] indices =
        {
            0, 1, 2, 3, 4, 30, 59, 60, 61, 120, 600, 1_800, 3_599, 3_600,
            60_000, 125_996, 125_997, 125_998, 125_999, 126_000,
        };

        StringBuilder rendered = new();
        rendered.Append("# authority: docs/technical/10-runtime-architecture.md § Clock domains\n");
        rendered.Append("#   \"The simulation frequency is 60 ticks per second. ... Game time is derived from\n");
        rendered.Append("#   integer tick count, not accumulated floating-point frame deltas.\"\n");
        rendered.Append("#   docs/technical/20-simulation-core.md § Numeric and unit conventions: run time is a\n");
        rendered.Append("#   \"64-bit integer simulation tick plus derived seconds\".\n");
        rendered.Append("#\n");
        rendered.Append("# derived by: an independent Python reference that divides the tick index by the exact\n");
        rendered.Append("#   rational rate 60/1 exactly once, not by the C# implementation under test. Any\n");
        rendered.Append("#   implementation that accumulates SecondsPerTick instead of dividing once disagrees\n");
        rendered.Append("#   with this file.\n");
        rendered.Append("#\n");
        rendered.Append("# The seconds column is the IEEE 754 binary64 bit pattern of index/60, not a decimal\n");
        rendered.Append("# rendering: a bit pattern has no formatting ambiguity, so a mismatch here is always a\n");
        rendered.Append("# disagreement about the value and never about how it was printed.\n");
        rendered.Append("#\n");
        rendered.Append("# Tick 126000 is the 35:00 terminal boundary (docs/technical/20 § Boundary and tie\n");
        rendered.Append("# ordering); it is included because its derived seconds must be exactly 2100.\n");
        rendered.Append("#\n");
        rendered.Append("# columns (tab separated): tickIndex, derivedSecondsIeee754Bits\n");
        rendered.Append("# entries=").Append(indices.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (long index in indices)
        {
            SimulationTick tick = new(index);
            rendered
                .Append(index.ToString(CultureInfo.InvariantCulture))
                .Append('\t')
                .Append("0x")
                .Append(BitConverter.DoubleToUInt64Bits(tick.Seconds).ToString("X16", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return rendered.ToString();
    }
}
