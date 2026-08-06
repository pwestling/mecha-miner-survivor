using System;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The focus and suspension hooks: each clears only its own reason, and each resume discards the
/// elapsed wall time it spanned.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-002-005</c>, <c>VER-SIM-002-006</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: "Focus recovery never dismisses
/// a menu, tutorial, relic choice, or user-requested pause." § Clock domains: "Operating-system
/// suspension or focus-loss pause discards elapsed wall time rather than catching up gameplay."
/// </remarks>
[TestFixture]
internal sealed class FocusAndSuspendHookTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-002-005</c>.
    ///
    /// Clearing focus loss while a user-requested pause, a blocking tutorial or modal, a fabrication
    /// session, or a relic choice is present leaves the run paused and leaves that reason present.
    /// </summary>
    [Test]
    public void FocusRecoveryDismissesOnlyTheFocusLossReason()
    {
        // The same assertion the negative control VER-SIM-002-010 proves can fail.
        PauseContract.AssertFocusRecoveryDismissesOnlyFocusLoss(() => new HostPausableRun());

        // And the same claim stated through the hooks and the typed transition result directly, so
        // the outcome is observable rather than only inferable from the resulting set.
        Expect.Multiple(() =>
        {
            foreach (PauseReason survivor in PauseReasonSet.AllReasons)
            {
                if (survivor == PauseReason.FocusLoss)
                {
                    continue;
                }

                RecordingWorld world = new();
                SimulationHost host = new(world);
                host.Clock.Raise(survivor);
                host.Lifecycle.OnFocusLost();

                PauseTransitionResult recovery = host.Lifecycle.OnFocusRegained();

                Assert.That(
                    recovery.Outcome,
                    Is.EqualTo(PauseTransitionOutcome.Cleared),
                    "focus recovery clears focus loss");
                Assert.That(
                    recovery.ResultingSet,
                    Is.EqualTo(PauseReasonSet.Of(survivor)),
                    "and leaves exactly the other reasons present: " + survivor.ToString());
                Assert.That(
                    recovery.IsBlocking,
                    "so the run is still blocked, which the transition result says directly");
                Assert.That(
                    host.Step(1.0).TickCount,
                    Is.EqualTo(0),
                    "and no tick executes");
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-006</c>.
    ///
    /// A suspension resume clears only the suspension reason and discards the elapsed wall time it
    /// spanned, so a long suspension neither resumes into a catch-up burst nor clears an unrelated
    /// reason.
    /// </summary>
    [Test]
    public void SuspendResumeClearsOnlySuspensionAndDiscardsElapsedTime()
    {
        const double suspensionSeconds = 3_600.0;

        RecordingWorld world = new();
        SimulationHost host = new(world);

        // A little progress first, so "unchanged" is a real claim rather than a claim about zero.
        HostStepResult beforeSuspension = host.Step(TickRate.SecondsForTicks(2));
        long tickAtSuspension = host.Clock.CommittedTickCount;

        host.Clock.Raise(PauseReason.RelicResolution);
        PauseTransitionResult suspended = host.Lifecycle.OnOperatingSystemSuspended();
        HostStepResult whileSuspended = host.Step(suspensionSeconds);
        PauseTransitionResult resumed = host.Lifecycle.OnOperatingSystemResumed();

        // The relic choice is still present, so the run is still blocked and still runs no tick.
        HostStepResult afterResumeStillBlocked = host.Step(suspensionSeconds);

        // Once the last reason clears, the first step to actually reach the accumulator carries the
        // wall time the blackout spanned, and discards it.
        host.Clock.Clear(PauseReason.RelicResolution);
        HostStepResult firstUnblockedStep = host.Step(suspensionSeconds);
        long tickAfterTheDiscard = host.Clock.CommittedTickCount;
        int diagnosticsAfterTheDiscard = host.Diagnostics.CatchUpBoundReachedCount;
        HostStepResult afterEverythingCleared = host.Step(TickRate.SecondsForTicks(1));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(2L, beforeSuspension.TickCount, "two ticks ran before the suspension");

            Assert.That(
                suspended.Outcome,
                Is.EqualTo(PauseTransitionOutcome.Raised),
                "the suspension raises its own reason");
            Assert.That(
                suspended.ResultingSet,
                Is.EqualTo(PauseReasonSet.Of(
                    PauseReason.RelicResolution,
                    PauseReason.OperatingSystemSuspension)),
                "alongside the unrelated reason that was already present");
            NumericAssert.AreExactlyEqual(0L, whileSuspended.TickCount, "no tick runs while suspended");

            Assert.That(
                resumed.Outcome,
                Is.EqualTo(PauseTransitionOutcome.Cleared),
                "the resume clears the suspension");
            Assert.That(
                resumed.ResultingSet,
                Is.EqualTo(PauseReasonSet.Of(PauseReason.RelicResolution)),
                "and only the suspension: the relic choice is still present");

            NumericAssert.AreExactlyEqual(
                0L,
                afterResumeStillBlocked.TickCount,
                "the step after the resume runs zero ticks: the relic choice is still blocking");
            NumericAssert.AreExactlyEqual(
                0L,
                firstUnblockedStep.TickCount,
                "and the first unblocked step runs zero ticks too: an hour of suspension is discarded, "
                    + "not caught up (doc 10 § Clock domains)");
            Assert.That(
                firstUnblockedStep.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.OperatingSystemSuspension),
                "and it says why it discarded that hour");
            Assert.That(
                BitConverter.DoubleToInt64Bits(firstUnblockedStep.DiscardedSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(suspensionSeconds)),
                "reporting the whole discarded interval rather than dropping it silently");
            NumericAssert.AreExactlyEqual(
                tickAtSuspension,
                tickAfterTheDiscard,
                "so the tick index is exactly where the suspension found it");
            NumericAssert.AreExactlyEqual(
                0L,
                diagnosticsAfterTheDiscard,
                "and no performance diagnostic is produced: a lifecycle discard is correct behaviour, not "
                    + "a defect");

            NumericAssert.AreExactlyEqual(
                1L,
                afterEverythingCleared.TickCount,
                "once every reason is cleared the run continues one tick per interval");
            NumericAssert.AreExactlyEqual(
                tickAtSuspension + 1,
                host.Clock.CommittedTickCount,
                "from exactly where it left off, with nothing banked from the blackout");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-006</c>.
    ///
    /// The discarded interval is reported with the reason that caused it, so a caller can tell a
    /// lifecycle discard - which is correct - from a catch-up discard, which is a defect.
    /// </summary>
    [Test]
    public void ALifecycleDiscardIsReportedWithItsOwnReason()
    {
        const double blackoutSeconds = 120.0;

        RecordingWorld focusWorld = new();
        SimulationHost focusHost = new(focusWorld);
        focusHost.Lifecycle.OnFocusLost();
        focusHost.Step(blackoutSeconds);
        focusHost.Lifecycle.OnFocusRegained();
        HostStepResult afterFocusRecovery = focusHost.Step(blackoutSeconds);

        RecordingWorld suspendWorld = new();
        SimulationHost suspendHost = new(suspendWorld);
        suspendHost.Lifecycle.OnOperatingSystemSuspended();
        suspendHost.Step(blackoutSeconds);
        suspendHost.Lifecycle.OnOperatingSystemResumed();
        HostStepResult afterSuspendResume = suspendHost.Step(blackoutSeconds);

        Expect.Multiple(() =>
        {
            Assert.That(
                afterFocusRecovery.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.FocusLoss),
                "a focus-loss discard names focus loss");
            Assert.That(
                afterSuspendResume.DiscardReason,
                Is.EqualTo(AccumulatorDiscardReason.OperatingSystemSuspension),
                "a suspension discard names the suspension");
            Assert.That(
                BitConverter.DoubleToInt64Bits(afterFocusRecovery.DiscardedSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(blackoutSeconds)),
                "and the discarded interval is reported, not silently dropped");
            Assert.That(
                afterFocusRecovery.CatchUpBoundReached,
                Is.False,
                "a lifecycle discard is not a catch-up discard");
            NumericAssert.AreExactlyEqual(
                0L,
                focusHost.Diagnostics.CatchUpBoundReachedCount,
                "and produces no performance diagnostic");
            NumericAssert.AreExactlyEqual(
                0L,
                suspendHost.Diagnostics.CatchUpBoundReachedCount,
                "for either lifecycle reason");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-006</c>.
    ///
    /// Only the two lifecycle reasons doc 10 § Clock domains names may arm a discard. A general pause
    /// does not discard elapsed time - it simply never accumulates any.
    /// </summary>
    [Test]
    public void OnlyTheTwoLifecycleReasonsCanArmADiscard()
    {
        FixedStepAccumulator accumulator = new(CatchUpPolicy.Default);

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(
                () => accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.None));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.CatchUpBoundReached));
            Expect.DoesNotThrow(
                () => accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.FocusLoss));
            Expect.DoesNotThrow(
                () => accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.OperatingSystemSuspension));
            Assert.That(accumulator.IsLifecycleDiscardArmed, "arming is observable");
        });
    }
}
