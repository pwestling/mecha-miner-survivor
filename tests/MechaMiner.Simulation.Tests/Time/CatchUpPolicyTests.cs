using System;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The catch-up bound: what it discards, what it reports, and that its value is a derivation
/// rather than a literal.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-005</c>, <c>VER-SIM-001-006</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "A bounded catch-up limit
/// prevents an unresponsive spiral after a stall; reaching that bound produces a performance
/// diagnostic."
/// <c>docs/technical/decisions/TDR-003-require-sixty-fps-on-steam-deck.md</c> § Performance
/// contract: "No repeatable active-play stall may exceed 50 milliseconds."
/// </remarks>
[TestFixture]
internal sealed class CatchUpPolicyTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-001-005</c>.
    ///
    /// Elapsed time beyond the bound runs exactly the bound's ticks, reports the bound as
    /// reached, reports the discarded seconds, and never queues the surplus into a later step.
    /// </summary>
    [Test]
    public void SurplusBeyondTheBoundIsDiscardedAndReportedNotQueued()
    {
        // The same assertion the negative control VER-SIM-001-008 proves can fail.
        AccumulatorContract.AssertCatchUpBoundIsRespected(new FixedStepAccumulatorSubject());

        CatchUpPolicy policy = CatchUpPolicy.Default;
        int bound = policy.MaximumTicksPerStep;
        FixedStepAccumulator accumulator = new(policy);

        // A stall worth ten ticks arrives as one step.
        TickBudget stalled = accumulator.Advance(TickRate.SecondsForTicks(10));

        // Nothing is owed afterwards: the surplus was discarded, not banked.
        TickBudget afterStall = accumulator.Advance(0.0);
        TickBudget nextNormalStep = accumulator.Advance(TickRate.SecondsPerTick);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(bound, stalled.TickCount, "the bound caps one step");
            Assert.That(
                stalled.CatchUpBoundReached,
                "reaching the bound must be reported, not silently clamped (doc 10 § Clock domains)");
            Assert.That(
                stalled.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.CatchUpBoundReached),
                "a catch-up discard is a performance defect and is reported as its own reason, not as a "
                    + "lifecycle discard");
            NumericAssert.AreExactlyEqual(
                10 - bound,
                stalled.DiscardedTickCount,
                "every whole tick beyond the bound is discarded");
            Assert.That(
                BitConverter.DoubleToInt64Bits(stalled.DiscardedSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(10 - bound))),
                "the discarded seconds are the discarded whole ticks converted by one division: doc 90 "
                    + "§ Frame metrics requires the accumulator debt to be reported");

            NumericAssert.AreExactlyEqual(
                0L,
                afterStall.TickCount,
                "the discarded surplus must never be queued into a later step");
            Assert.That(
                afterStall.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.None),
                "the following step discards nothing of its own");
            NumericAssert.AreExactlyEqual(
                1L,
                nextNormalStep.TickCount,
                "and the run continues normally at one tick per interval");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-006</c>.
    ///
    /// The bound absorbs the largest stall TDR-003 tolerates without discarding any game time -
    /// 50 ms is exactly three ticks at 60 Hz and all three run - and the first tick of debt
    /// beyond the bound is discarded and diagnosed. The four-tick baseline is asserted as a
    /// derivation, so the number cannot be edited without contradicting its own inputs.
    /// </summary>
    [Test]
    public void BoundAbsorbsTheLargestToleratedStallAndNoMore()
    {
        CatchUpPolicy policy = CatchUpPolicy.Default;

        FixedStepAccumulator toleratedStall = new(policy);
        TickBudget atTolerance = toleratedStall.Advance(policy.ToleratedStallSeconds);

        FixedStepAccumulator atBound = new(policy);
        TickBudget exactlyTheBound = atBound.Advance(TickRate.SecondsForTicks(policy.MaximumTicksPerStep));

        FixedStepAccumulator justBeyond = new(policy);
        TickBudget oneTickBeyondTheBound =
            justBeyond.Advance(TickRate.SecondsForTicks(policy.MaximumTicksPerStep + 1));

        Expect.Multiple(() =>
        {
            // The derivation, input by input.
            NumericAssert.AreExactlyEqual(
                50L,
                policy.ToleratedStallMilliseconds,
                "TDR-003 § Performance contract: \"No repeatable active-play stall may exceed 50 "
                    + "milliseconds\"");
            NumericAssert.AreExactlyEqual(
                3L,
                policy.ToleratedStallTicks,
                "50 ms at 60 Hz is exactly three ticks, by integer arithmetic against the rational rate");
            NumericAssert.AreExactlyEqual(
                1L,
                policy.HeadroomTicks,
                "one tick of headroom, so a frame measured at the tolerance cannot trip the bound on a "
                    + "fractional remainder");
            NumericAssert.AreExactlyEqual(
                4L,
                policy.MaximumTicksPerStep,
                "the bound is the derivation's sum, not a chosen number: 3 tolerated + 1 headroom");
            Assert.That(
                policy,
                Is.EqualTo(CatchUpPolicy.FromStallTolerance(
                    CatchUpPolicy.ToleratedStallMillisecondsDefault,
                    CatchUpPolicy.HeadroomTicksDefault)),
                "the default policy is exactly that derivation applied to the two published inputs");

            // The upper-bound check of the derivation: a four-tick recovery step is itself inside
            // the 50 ms stall tolerance, using doc 90 § Target device frame budget's allocation.
            const double frameBudgetMilliseconds = 16.67;
            const double simulationAllocationMilliseconds = 5.00;
            double recoveryStepMilliseconds =
                (frameBudgetMilliseconds - simulationAllocationMilliseconds)
                + (policy.MaximumTicksPerStep * simulationAllocationMilliseconds);
            Assert.That(
                recoveryStepMilliseconds,
                Is.LessThan(policy.ToleratedStallMilliseconds),
                "a step at the bound must not itself exceed the stall tolerance it was derived from");

            // The behaviour the derivation exists to produce.
            NumericAssert.AreExactlyEqual(
                3L,
                atTolerance.TickCount,
                "a stall exactly at the TDR-003 tolerance runs all three of its ticks");
            Assert.That(
                atTolerance.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.None),
                "a stall the performance contract permits must not discard any game time");

            NumericAssert.AreExactlyEqual(
                policy.MaximumTicksPerStep,
                exactlyTheBound.TickCount,
                "a stall of exactly the bound runs every one of its ticks");
            Assert.That(
                exactlyTheBound.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.None),
                "reaching the bound exactly is not exceeding it, so nothing is discarded or diagnosed");

            NumericAssert.AreExactlyEqual(
                policy.MaximumTicksPerStep,
                oneTickBeyondTheBound.TickCount,
                "one tick beyond the bound still runs exactly the bound's ticks");
            NumericAssert.AreExactlyEqual(
                1L,
                oneTickBeyondTheBound.DiscardedTickCount,
                "and discards exactly that one tick of debt");
            Assert.That(
                oneTickBeyondTheBound.CatchUpBoundReached,
                "the first tick of debt beyond the bound is diagnosed");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-006</c>.
    ///
    /// A policy derived from other inputs still derives its bound the same way, so the
    /// derivation is a rule rather than a coincidence of the accepted numbers.
    /// </summary>
    [TestCase(50, 1, 4)]
    [TestCase(50, 0, 3)]
    [TestCase(100, 1, 7)]
    [TestCase(16, 2, 2)]
    public void TheBoundIsAlwaysToleratedTicksPlusHeadroom(
        int toleratedStallMilliseconds,
        int headroomTicks,
        int expectedBound)
    {
        CatchUpPolicy policy = CatchUpPolicy.FromStallTolerance(toleratedStallMilliseconds, headroomTicks);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(expectedBound, policy.MaximumTicksPerStep, "derived bound");
            NumericAssert.AreExactlyEqual(
                policy.ToleratedStallTicks + policy.HeadroomTicks,
                policy.MaximumTicksPerStep,
                "the bound is the sum of its two stated inputs and nothing else");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-006</c>.
    ///
    /// A derivation that would admit no tick at all is refused rather than producing an
    /// accumulator that can never advance a run.
    /// </summary>
    [Test]
    public void ADerivationThatAdmitsNoTickIsRefused()
    {
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => CatchUpPolicy.FromStallTolerance(0, 1));
            Expect.Throws<ArgumentOutOfRangeException>(() => CatchUpPolicy.FromStallTolerance(50, -1));
            Expect.Throws<ArgumentOutOfRangeException>(() => CatchUpPolicy.FromStallTolerance(1, 0));
            Expect.Throws<ArgumentOutOfRangeException>(() => new FixedStepAccumulator(default));
        });
    }
}
