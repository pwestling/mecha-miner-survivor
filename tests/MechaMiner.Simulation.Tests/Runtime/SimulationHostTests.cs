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

    /// <summary>
    /// Verification: <c>VER-SIM-001-010</c>.
    ///
    /// A blocking reason raised from inside a tick makes that tick uncommittable, so the run ends through
    /// the safe technical-failure path instead of leaving the world one tick ahead of the clock and
    /// re-running the tick on the next frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The repeat is what this test exists for.</b> The pause-set condition sits in two places: in
    /// <see cref="RunClock.CommitTick"/>, which refuses while a blocking reason is present, and in
    /// <see cref="SimulationHost.Step(double)"/>, which consults the set before its loop. A reason raised
    /// while a tick is in flight satisfies the first and misses the second, so the tick target ran, the
    /// commit was refused, and a caller that cleared the reason and drove another frame got the same tick
    /// again: the recorded sequence was 0, 1, 1 where <c>VER-SIM-001-010</c> requires each tick "exactly
    /// once, ascending, with no gap and no repeat".
    /// </para>
    /// <para>
    /// <b>Ending the run is the only available answer, and it is the documented one.</b> Once the tick
    /// target has returned, the world has moved and the clock has not, and no later step can undo either.
    /// <c>docs/technical/20-simulation-core.md</c> § Tick transaction: "An exception or invariant failure
    /// before commit invalidates the tick and ends the run through the safe technical-failure path."
    /// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Crash handling names what
    /// "safe" excludes: reporting is registered "without attempting to continue corrupted simulation".
    /// </para>
    /// <para>
    /// The world is not a writer of pause state - <c>docs/technical/115-component-contract-and-schema-registry.md</c>
    /// § Mutable-state ownership matrix gives run pause state to <c>CMP-RUN-001</c> alone - so a reason
    /// arriving from inside a tick is a defect in the caller, which is exactly why the run ends rather than
    /// absorbing it. The callback below stands in for such a caller.
    /// </para>
    /// </remarks>
    [Test]
    public void ABlockingReasonRaisedInsideATickEndsTheRunInsteadOfRepeatingTheTick()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);
        world.DuringTick = tick =>
        {
            if (tick.Index == 1)
            {
                host.Clock.Raise(PauseReason.RelicResolution);
            }
        };

        HostStepResult first = host.Step(TickRate.SecondsForTicks(1));
        InvalidOperationException refusedCommit = Expect.Throws<InvalidOperationException>(
            () => host.Step(TickRate.SecondsForTicks(1)));

        // The caller that cleared the reason and drove one more frame is what re-ran tick 1.
        host.Clock.Clear(PauseReason.RelicResolution);
        InvalidOperationException refusedStep = Expect.Throws<InvalidOperationException>(
            () => host.Step(TickRate.SecondsForTicks(1)));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(1L, first.TickCount, "the first step runs tick 0 and commits it");
            Assert.That(
                world.AdvancedTicks,
                Is.EqualTo(new long[] { 0L, 1L }).AsCollection,
                "each tick reaches the tick target exactly once, with no gap and no repeat: tick 1 must not "
                    + "run a second time because its commit was refused");
            Assert.That(
                refusedCommit.Message,
                Does.Contain("no tick commits while a blocking reason is present"),
                "the refusal the caller sees is the run clock's own, rethrown unchanged rather than wrapped");
            Assert.That(
                host.HasEndedInTechnicalFailure,
                Is.True,
                "a tick applied to the world that cannot be committed ends the run (doc 20 § Tick "
                    + "transaction), which is what stops the next frame from repeating it");
            NumericAssert.AreExactlyEqual(
                1L,
                host.TechnicalFailureTick.Index,
                "and the recorded failure names the tick that was in flight");
            Assert.That(
                host.TechnicalFailure,
                Is.SameAs(refusedCommit),
                "the recorded failure is the one the caller was given, not a copy or a summary");
            Assert.That(
                refusedStep.InnerException,
                Is.SameAs(refusedCommit),
                "and a later step refuses by naming that failure, so the run cannot be nursed along");
            NumericAssert.AreExactlyEqual(
                1L,
                host.Clock.CommittedTickCount,
                "the clock committed tick 0 only, so nothing was committed for the tick that failed");
            NumericAssert.AreExactlyEqual(
                0L,
                world.TerminalBoundaryCallCount,
                "and no terminal boundary was evaluated: a technical failure publishes no terminal result "
                    + "(doc 20 § Tick transaction never publishes a partial state)");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-010</c>.
    ///
    /// A tick target that throws ends the run through the safe technical-failure path: the exception
    /// reaches the caller unchanged, the run clock commits nothing for that tick, and no later step runs
    /// the tick again.
    /// </summary>
    /// <remarks>
    /// <see cref="ISimulationWorld.AdvanceTick(SimulationTick)"/>'s own remarks promise this - "doc 20
    /// § Tick transaction requires an exception or invariant failure before commit to end the run through
    /// the safe technical-failure path" - and the half that was implemented was only the second sentence,
    /// that the clock is not committed. Nothing ended the run and nothing recorded the failure, so a caller
    /// driving the next frame ran the same tick again: the recorded sequence was 0, 0.
    /// </remarks>
    [Test]
    public void ATickTargetThatThrowsEndsTheRunThroughTheTechnicalFailurePath()
    {
        const string failureMessage = "the tick target failed its own invariant";

        RecordingWorld world = new();
        SimulationHost host = new(world);
        world.DuringTick = tick =>
        {
            if (tick.Index == 1)
            {
                throw new InvalidOperationException(failureMessage);
            }
        };

        host.Step(TickRate.SecondsForTicks(1));
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => host.Step(TickRate.SecondsForTicks(1)));
        InvalidOperationException refusedStep = Expect.Throws<InvalidOperationException>(
            () => host.Step(TickRate.SecondsForTicks(1)));

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Is.EqualTo(failureMessage),
                "the tick target's failure reaches the caller unchanged, so the diagnostic names the real "
                    + "defect (doc 20 § Mid-commit invalidation rethrows unchanged for the other half of "
                    + "the tick, and this half does the same)");
            Assert.That(
                world.AdvancedTicks,
                Is.EqualTo(new long[] { 0L, 1L }).AsCollection,
                "the failed tick is not retried, so no tick index appears twice");
            NumericAssert.AreExactlyEqual(
                1L,
                host.Clock.CommittedTickCount,
                "the run clock is not committed for a tick whose call threw");
            Assert.That(host.HasEndedInTechnicalFailure, Is.True, "and the run has ended");
            Assert.That(
                host.TechnicalFailure,
                Is.SameAs(failure),
                "with the failure retained, so it is observable rather than only thrown");
            NumericAssert.AreExactlyEqual(
                1L,
                host.TechnicalFailureTick.Index,
                "naming the tick that was in flight");
            Assert.That(
                refusedStep.InnerException,
                Is.SameAs(failure),
                "every later step refuses and names it: doc 90 § Crash handling does not continue a "
                    + "corrupted simulation");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-012</c>.
    ///
    /// A step that begins with the clock already at 35:00 and the boundary not yet evaluated evaluates it
    /// there, rather than breaking out of the tick loop and leaving the run past the boundary, unblocked,
    /// and still admitting scheduled events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The boundary condition occupies two positions in the step: after a commit that reached it, and
    /// before the first tick of a step that begins past it. Only the first was visited, so deleting the
    /// second left the whole suite green - nothing reached that position - while the state it guards is a
    /// run that returns a zero-tick result for ever and never resolves.
    /// </para>
    /// <para>
    /// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering, phase 2: "the 35:00
    /// terminal boundary is handled before another tick can begin." The clock arrives at the boundary here
    /// through <see cref="RunClock.CommitTick"/>, which is public, and the host is constructed over that
    /// clock through its public explicit-collaborator constructor, so the state is reachable with no test
    /// hook and nothing internal.
    /// </para>
    /// </remarks>
    [Test]
    public void AStepThatBeginsPastTheBoundaryEvaluatesItRatherThanRunningOnForever()
    {
        const string preBoundaryEventId = "SCH-0060-WAVE";

        RunClock clock = new();
        while (!clock.HasReachedFinalBoundary)
        {
            clock.CommitTick();
        }

        RecordingWorld world = new();
        SimulationHost host = new(
            world,
            clock,
            new FixedStepAccumulator(),
            new PerformanceDiagnostics());

        // The state the guard is for, asserted to exist before the step rather than assumed: past 35:00,
        // nothing evaluated, nothing blocking, and a pre-boundary event still admitted.
        bool blockingBefore = clock.IsBlocking;
        bool admittedBefore = host.TryBeginScheduledEvent(new SimulationTick(60), preBoundaryEventId);

        HostStepResult atTheBoundary = host.Step(TickRate.SecondsForTicks(1));
        HostStepResult afterwards = host.Step(TickRate.SecondsForTicks(1));
        bool admittedAfter = host.TryBeginScheduledEvent(new SimulationTick(60), preBoundaryEventId);

        Expect.Multiple(() =>
        {
            Assert.That(
                blockingBefore,
                Is.False,
                "the run began the step past 35:00 and unblocked, which is the state under test");
            Assert.That(
                admittedBefore,
                Is.True,
                "and it still admitted a scheduled event, so the position really is unguarded before the "
                    + "step");

            Assert.That(
                atTheBoundary.TerminalBoundaryEvaluated,
                Is.True,
                "the step that finds the clock past 35:00 evaluates the boundary");
            NumericAssert.AreExactlyEqual(
                1L,
                world.TerminalBoundaryCallCount,
                "exactly once, on the tick target");
            Assert.That(
                clock.TerminalBoundaryEvaluated,
                Is.True,
                "and the run clock records it (doc 20 § Scope and invariants: assigned once)");
            NumericAssert.AreExactlyEqual(
                0L,
                atTheBoundary.TickCount,
                "no tick runs at or after the boundary");
            NumericAssert.AreExactlyEqual(
                0L,
                world.AdvanceTickCallCount,
                "so the tick target was never advanced");
            Assert.That(
                clock.BlockingReasons,
                Is.EqualTo(PauseReasonSet.Of(PauseReason.TerminalTransition)),
                "the terminal transition is raised, which is what stops the run rather than a zero-tick "
                    + "step repeating for ever");

            Assert.That(
                afterwards.WasBlocked,
                Is.True,
                "the following step is blocked rather than reaching the boundary position again");
            Assert.That(
                afterwards.TerminalBoundaryEvaluated,
                Is.False,
                "and evaluates nothing: the evaluation is idempotent, so occupying both positions cannot "
                    + "evaluate twice");
            NumericAssert.AreExactlyEqual(
                1L,
                world.TerminalBoundaryCallCount,
                "still exactly one evaluation in the whole run");
            Assert.That(
                admittedAfter,
                Is.False,
                "and no scheduled event is admitted afterwards, which is the ordering doc 20 § Boundary and "
                    + "tie ordering requires the evaluation to establish");
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
