using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Time;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The accumulator assertions themselves, factored out so the positive gates and the negative
/// control run the identical checks against different subjects.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-002</c>, <c>VER-SIM-001-005</c>, and the negative control
/// <c>VER-SIM-001-008</c>.
/// </para>
/// <para>
/// Every assertion here uses <c>Assert.That</c> directly rather than
/// <c>Expect.Multiple</c>. <c>Expect.Multiple</c> collects failures and raises
/// <c>MultipleAssertException</c>, which does not derive from <c>AssertionException</c>, so a
/// negative control could not catch it. A single failing assertion throwing immediately is
/// exactly what the negative control needs to observe.
/// </para>
/// </remarks>
internal static class AccumulatorContract
{
    /// <summary>
    /// Asserts that every step yields a whole, nonnegative number of ticks and that the run
    /// totals exactly the expected count.
    /// </summary>
    /// <param name="subject">The accumulator under test.</param>
    /// <param name="elapsedSecondsPerStep">One step's elapsed seconds, in order.</param>
    /// <param name="expectedTotalTicks">The whole ticks the stream must produce in total.</param>
    /// <remarks>
    /// doc 10 § Clock domains: the host executes "zero or more complete ticks per rendered
    /// frame" and "never passes a variable delta to authoritative systems". A fractional yield
    /// fails the first check; a drifting total fails the last.
    /// </remarks>
    internal static void AssertOnlyWholeTicksAreYielded(
        IStepwiseAccumulator subject,
        IReadOnlyList<double> elapsedSecondsPerStep,
        long expectedTotalTicks)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(elapsedSecondsPerStep);

        double total = 0.0;
        for (int index = 0; index < elapsedSecondsPerStep.Count; index++)
        {
            double yielded = subject.AdvanceAndCountTicks(elapsedSecondsPerStep[index]);

            Assert.That(
                yielded,
                Is.EqualTo(Math.Floor(yielded)),
                "step " + index.ToString(CultureInfo.InvariantCulture)
                    + " yielded a fractional tick amount; doc 10 § Clock domains: the host executes "
                    + "\"zero or more complete ticks\" and \"never passes a variable delta to "
                    + "authoritative systems\"");
            Assert.That(
                yielded,
                Is.GreaterThanOrEqualTo(0.0),
                "step " + index.ToString(CultureInfo.InvariantCulture)
                    + " yielded a negative tick count; run time advances only forwards");

            total += yielded;
        }

        Assert.That(
            total,
            Is.EqualTo((double)expectedTotalTicks),
            "the stream must yield exactly the whole ticks its total elapsed time covers, with no "
                + "accumulated drift (doc 10 § Clock domains, VER-SIM-001-003)");
    }

    /// <summary>
    /// Asserts that one step never runs more than the catch-up bound, reports the surplus as
    /// discarded, and never queues that surplus into a later step.
    /// </summary>
    /// <param name="subject">The accumulator under test.</param>
    /// <remarks>
    /// doc 10 § Clock domains: "A bounded catch-up limit prevents an unresponsive spiral after a
    /// stall; reaching that bound produces a performance diagnostic." A queued surplus would
    /// reintroduce exactly the spiral the bound exists to prevent, so the third assertion is as
    /// load-bearing as the first.
    /// </remarks>
    internal static void AssertCatchUpBoundIsRespected(IStepwiseAccumulator subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        int bound = subject.MaximumTicksPerStep;
        const int surplusTicks = 6;
        double stalledStepSeconds = TickRate.SecondsForTicks(bound + surplusTicks);

        double yielded = subject.AdvanceAndCountTicks(stalledStepSeconds);
        Assert.That(
            yielded,
            Is.EqualTo((double)bound),
            "a stall worth " + (bound + surplusTicks).ToString(CultureInfo.InvariantCulture)
                + " ticks must run exactly the bound's " + bound.ToString(CultureInfo.InvariantCulture)
                + " ticks (doc 10 § Clock domains)");

        Assert.That(
            subject.LastDiscardedSeconds,
            Is.EqualTo(TickRate.SecondsForTicks(surplusTicks)),
            "the surplus beyond the bound must be reported as discarded seconds, which doc 90 "
                + "§ Frame metrics calls the accumulator debt");

        double afterwards = subject.AdvanceAndCountTicks(0.0);
        Assert.That(
            afterwards,
            Is.EqualTo(0.0),
            "the discarded surplus must never be queued into a later step; a queued surplus is the "
                + "unresponsive spiral the bound exists to prevent");
    }
}
