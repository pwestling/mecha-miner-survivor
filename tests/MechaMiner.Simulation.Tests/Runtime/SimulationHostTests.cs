using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Tests.Time;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The host's own contract: one call per whole tick in ascending order, never re-entrant, and a
/// lifecycle resume that discards elapsed wall time instead of catching it up.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-009</c>, <c>VER-SIM-001-010</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains and § System phase ordering;
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry
/// (<c>CMP-RUN-001</c> is the sole writer of run clock state, so it is also the only caller of the
/// tick target).
/// </remarks>
[TestFixture]
internal sealed class SimulationHostTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-001-009</c>.
    ///
    /// A focus-loss or suspension resume discards the elapsed wall time: the step after the resume
    /// runs zero ticks, reports the discarded seconds, and leaves the tick index and run clock exactly
    /// where the interruption found them. The resume is invoked directly, with no frame in between.
    /// </summary>
    [TestCase(PauseReason.FocusLoss, AccumulatorDiscardReason.FocusLoss)]
    [TestCase(PauseReason.OperatingSystemSuspension, AccumulatorDiscardReason.OperatingSystemSuspension)]
    public void FocusLossAndSuspendResumeDiscardElapsedWallTime(
        PauseReason interruption,
        AccumulatorDiscardReason expectedDiscardReason)
    {
        const double blackoutSeconds = 900.0;

        RecordingWorld world = new();
        SimulationHost host = new(world);

        // Some progress first, and a retained sub-tick fraction, so "unchanged" is a real claim. The
        // warm-up step stays inside the catch-up bound so it cannot itself record a diagnostic.
        host.Step(TickRate.SecondsForTicks(3));
        host.Step(TickRate.SecondsPerTick * 0.75);
        long tickAtInterruption = host.Clock.CommittedTickCount;
        double runSecondsAtInterruption = host.Clock.RunSeconds;
        int tickCallsAtInterruption = world.AdvanceTickCallCount;

        Interrupt(host, interruption);
        HostStepResult whileInterrupted = host.Step(blackoutSeconds);
        Resume(host, interruption);

        HostStepResult afterResume = host.Step(blackoutSeconds);

        // Snapshotted immediately after the resume step, because the next step deliberately advances
        // the clock again and "unchanged" is a claim about this moment.
        long tickAfterResume = host.Clock.CommittedTickCount;
        double runSecondsAfterResume = host.Clock.RunSeconds;
        int tickCallsAfterResume = world.AdvanceTickCallCount;
        int diagnosticsAfterResume = host.Diagnostics.CatchUpBoundReachedCount;

        HostStepResult nextNormalStep = host.Step(TickRate.SecondsForTicks(1));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                0L,
                whileInterrupted.TickCount,
                "no tick runs while " + interruption.ToString() + " is present");

            NumericAssert.AreExactlyEqual(
                0L,
                afterResume.TickCount,
                "the step after the resume runs zero ticks: 15 minutes of wall time is discarded, not "
                    + "caught up (doc 10 § Clock domains)");
            Assert.That(
                afterResume.DiscardReason,
                Is.EqualTo(expectedDiscardReason),
                "and the discard names the interruption that caused it");
            Assert.That(
                BitConverter.DoubleToInt64Bits(afterResume.DiscardedSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(blackoutSeconds
                    + (TickRate.SecondsPerTick * 0.75))),
                "the reported discard is the blackout plus the fraction retained when it began: the run "
                    + "resumes from a clean timing baseline");
            Assert.That(
                afterResume.CatchUpBoundReached,
                Is.False,
                "a lifecycle discard is expected behaviour, not the performance defect the catch-up bound "
                    + "diagnoses");
            NumericAssert.AreExactlyEqual(
                0L,
                diagnosticsAfterResume,
                "so no performance diagnostic is recorded");

            NumericAssert.AreExactlyEqual(
                tickAtInterruption,
                tickAfterResume,
                "the tick index is exactly where the interruption found it");
            Assert.That(
                BitConverter.DoubleToInt64Bits(runSecondsAfterResume),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(runSecondsAtInterruption)),
                "and so is the run clock, bit for bit");
            NumericAssert.AreExactlyEqual(
                tickCallsAtInterruption,
                tickCallsAfterResume,
                "and the tick target was not called once during or after the interruption");

            NumericAssert.AreExactlyEqual(
                1L,
                nextNormalStep.TickCount,
                "the run then continues at one tick per interval, with nothing banked");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-010</c>.
    ///
    /// The tick target is invoked exactly once per whole tick, in strictly ascending order with no gap
    /// and no repeat, for a mixed run of zero-, one-, and multi-tick steps; and never re-entrantly.
    /// </summary>
    [Test]
    public void TickTargetIsInvokedOncePerTickInAscendingOrder()
    {
        ImmutableArray<double> deltas = FrameDeltaStreams.ShortIrregular();

        RecordingWorld world = new();
        SimulationHost host = new(world);
        List<int> perStepTickCounts = new();
        List<long> expectedSequence = new();
        long nextExpectedTick = 0;

        foreach (double elapsed in deltas)
        {
            HostStepResult result = host.Step(elapsed);
            perStepTickCounts.Add(result.TickCount);
            for (int index = 0; index < result.TickCount; index++)
            {
                expectedSequence.Add(nextExpectedTick);
                nextExpectedTick++;
            }

            if (result.TickCount > 0)
            {
                Assert.That(
                    result.FirstTick.Index,
                    Is.EqualTo(expectedSequence[expectedSequence.Count - result.TickCount]),
                    "the step reports the first tick it ran");
                Assert.That(
                    result.LastTick.Index,
                    Is.EqualTo(expectedSequence[^1]),
                    "and the last");
            }
        }

        ImmutableArray<long> actual = world.AdvancedTicks;

        Expect.Multiple(() =>
        {
            Assert.That(
                actual,
                Is.EqualTo(expectedSequence).AsCollection,
                "every tick is invoked exactly once, ascending, with no gap and no repeat");
            NumericAssert.AreExactlyEqual(
                host.Clock.CommittedTickCount,
                actual.Length,
                "one tick-target call per committed tick");
            Assert.That(
                perStepTickCounts,
                Has.Some.EqualTo(0),
                "the mixed run must contain zero-tick steps");
            Assert.That(
                perStepTickCounts,
                Has.Some.EqualTo(1),
                "and one-tick steps");
            Assert.That(
                perStepTickCounts,
                Has.Some.GreaterThan(1),
                "and multi-tick steps");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-010</c>.
    ///
    /// A step started from inside a tick is refused. doc 10 § Concurrency baseline runs the
    /// authoritative simulation serially, so a re-entrant step would run a tick inside a tick and
    /// commit the run clock twice for the same interval.
    /// </summary>
    [Test]
    public void TheHostRefusesToRunATickInsideATick()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);
        List<Exception> caught = new();
        world.DuringTick = _ =>
        {
            try
            {
                host.Step(TickRate.SecondsForTicks(1));
            }
            catch (InvalidOperationException refusal)
            {
                caught.Add(refusal);
            }
        };

        HostStepResult result = host.Step(TickRate.SecondsForTicks(2));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(2L, result.TickCount, "the outer step still runs its two ticks");
            NumericAssert.AreExactlyEqual(2L, caught.Count, "and each re-entrant attempt was refused");
            NumericAssert.AreExactlyEqual(
                2L,
                world.AdvanceTickCallCount,
                "so the tick target ran exactly twice, not once per nesting level");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-010</c>.
    ///
    /// A frame delta that is negative or not finite is refused rather than absorbed: a monotonic clock
    /// never runs backwards, so such a value is a defect in the caller.
    /// </summary>
    [Test]
    public void AnImpossibleFrameDeltaIsRefused()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => host.Step(-1.0));
            Expect.Throws<ArgumentOutOfRangeException>(() => host.Step(double.NaN));
            Expect.Throws<ArgumentOutOfRangeException>(() => host.Step(double.PositiveInfinity));
            Expect.DoesNotThrow(() => host.Step(0.0));
            NumericAssert.AreExactlyEqual(0L, world.AdvanceTickCallCount, "and no tick ran");
        });
    }

    private static void Interrupt(SimulationHost host, PauseReason interruption)
    {
        if (interruption == PauseReason.FocusLoss)
        {
            host.Lifecycle.OnFocusLost();
            return;
        }

        host.Lifecycle.OnOperatingSystemSuspended();
    }

    private static void Resume(SimulationHost host, PauseReason interruption)
    {
        if (interruption == PauseReason.FocusLoss)
        {
            host.Lifecycle.OnFocusRegained();
            return;
        }

        host.Lifecycle.OnOperatingSystemResumed();
    }
}
