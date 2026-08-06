using System;
using System.Collections.Immutable;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The catch-up performance diagnostic: one record per occurrence, carrying the catch-up count and
/// the accumulator debt.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-007</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "A bounded catch-up limit
/// prevents an unresponsive spiral after a stall; reaching that bound produces a performance
/// diagnostic."
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics names the
/// metric: "frame/tick catch-up count and accumulator debt".
/// </remarks>
[TestFixture]
internal sealed class SimulationHostCatchUpDiagnosticTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-001-007</c>.
    ///
    /// Reaching the bound produces exactly one diagnostic per occurrence - not one per discarded tick
    /// and not a silent clamp - and that diagnostic carries the catch-up count and the accumulator
    /// debt.
    /// </summary>
    [Test]
    public void ReachingTheBoundEmitsOneDiagnosticCarryingCountAndDebt()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);
        int bound = host.CatchUpPolicy.MaximumTicksPerStep;
        const int firstStallTicks = 10;
        const int secondStallTicks = 30;

        // A step inside the bound must not be diagnosed at all.
        HostStepResult withinBound = host.Step(TickRate.SecondsForTicks(bound));
        int diagnosticsAfterAConformingStep = host.Diagnostics.CatchUpBoundReachedCount;

        HostStepResult firstStall = host.Step(TickRate.SecondsForTicks(firstStallTicks));
        long tickAfterFirstStall = host.Clock.CommittedTickCount;
        HostStepResult secondStall = host.Step(TickRate.SecondsForTicks(secondStallTicks));

        ImmutableArray<CatchUpDiagnostic> occurrences = host.Diagnostics.CatchUpOccurrences;

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(bound, withinBound.TickCount, "a step at the bound runs fully");
            NumericAssert.AreExactlyEqual(
                0L,
                diagnosticsAfterAConformingStep,
                "and produces no diagnostic: reaching the bound exactly is not exceeding it");

            Assert.That(firstStall.CatchUpBoundReached, "the first stall is diagnosed, not silently clamped");
            Assert.That(secondStall.CatchUpBoundReached, "and so is the second");

            NumericAssert.AreExactlyEqual(
                2L,
                host.Diagnostics.CatchUpBoundReachedCount,
                "exactly one diagnostic per occurrence: two stalls discarding 32 ticks between them "
                    + "produce two records, not 32");
            NumericAssert.AreExactlyEqual(2L, occurrences.Length, "and the record list agrees");

            // Record one: the catch-up count and the accumulator debt.
            NumericAssert.AreExactlyEqual(
                bound,
                occurrences[0].ExecutedTickCount,
                "the catch-up count is the ticks the bound permitted");
            NumericAssert.AreExactlyEqual(
                firstStallTicks - bound,
                occurrences[0].DiscardedTickCount,
                "the accumulator debt is the whole ticks discarded");
            Assert.That(
                BitConverter.DoubleToInt64Bits(occurrences[0].DebtSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(
                    TickRate.SecondsForTicks(firstStallTicks - bound))),
                "and the debt in seconds is that count converted by one division");
            NumericAssert.AreExactlyEqual(
                bound,
                occurrences[0].Tick.Index,
                "the record is stamped with the tick the run stood at when the bound was reached, which "
                    + "is where the conforming step left it");

            // Record two: stamped at the later tick, with its own larger debt.
            NumericAssert.AreExactlyEqual(
                tickAfterFirstStall,
                occurrences[1].Tick.Index,
                "the second record is stamped at the tick the second stall began from");
            NumericAssert.AreExactlyEqual(
                secondStallTicks - bound,
                occurrences[1].DiscardedTickCount,
                "with its own debt");

            NumericAssert.AreExactlyEqual(
                (firstStallTicks - bound) + (secondStallTicks - bound),
                host.Diagnostics.TotalDiscardedTickCount,
                "and the run total is the sum of the occurrences");
            Assert.That(
                BitConverter.DoubleToInt64Bits(host.Diagnostics.TotalDebtSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(
                    (firstStallTicks - bound) + (secondStallTicks - bound)))),
                "in seconds too");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-007</c>.
    ///
    /// The diagnostic is a record with named fields, not a rendered log line, and it refuses to be
    /// constructed for a step that discarded nothing - so a counter cannot be inflated by steps that
    /// were within budget.
    /// </summary>
    [Test]
    public void ADiagnosticCannotBeRecordedForAStepThatDiscardedNothing()
    {
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(
                () => new CatchUpDiagnostic(SimulationTick.Zero, 4, 0));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => new CatchUpDiagnostic(SimulationTick.Zero, -1, 1));
            Expect.DoesNotThrow(() => new CatchUpDiagnostic(SimulationTick.Zero, 4, 1));

            CatchUpDiagnostic diagnostic = new(new SimulationTick(1_234), 4, 6);
            Assert.That(
                diagnostic.ToString(),
                Does.Contain("catch-up-bound-reached tick=1234"),
                "the record renders canonically for a diagnostic line");
            Assert.That(diagnostic.ToString(), Does.Contain("executedTicks=4"));
            Assert.That(diagnostic.ToString(), Does.Contain("discardedTicks=6"));
        });
    }
}
