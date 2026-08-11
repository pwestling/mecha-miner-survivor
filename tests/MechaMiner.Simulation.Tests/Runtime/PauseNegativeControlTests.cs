using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The negative control for the pause gates: each assertion is shown failing against a subject that
/// breaks exactly the rule it checks.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-002-010</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires a gate to be
/// falsifiable. Without this control, <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>, and
/// <c>VER-SIM-002-005</c> could all be green while asserting nothing. Here the identical helpers
/// (<see cref="PauseContract"/>) that those three gates call are pointed at subjects that are wrong
/// in exactly one way each, and the control passes only if they fail.
/// </para>
/// <para>
/// The two broken subjects are the two plausible wrong implementations doc 10 § Pause contract exists
/// to rule out: <see cref="SingleTogglePausableRun"/> is the "single toggle" its first sentence
/// rejects, and <see cref="ClearEverythingOnFocusRecoveryRun"/> is the focus recovery that "dismisses
/// a menu, tutorial, relic choice, or user-requested pause" its last sentence forbids.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class PauseNegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-002-010</c>.
    ///
    /// A stub host that consults only a single boolean toggle instead of the reason set fails the
    /// assertions of <c>VER-SIM-002-002</c> and <c>VER-SIM-002-003</c>; a stub that clears the whole set
    /// on focus recovery fails the assertion of <c>VER-SIM-002-005</c>; and each stub still passes the
    /// assertions it does not break, so neither control is merely a stub that fails everything.
    /// </summary>
    [Test]
    public void PauseAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        AssertionException singleReasonFailure = Expect.Throws<AssertionException>(
            () => PauseContract.AssertNoTickExecutesWhileAnySingleReasonIsPresent(
                () => new SingleTogglePausableRun()));

        AssertionException overlappingFailure = Expect.Throws<AssertionException>(
            () => PauseContract.AssertResumesOnlyWhenEveryReasonIsCleared(
                () => new SingleTogglePausableRun()));

        AssertionException focusRecoveryFailure = Expect.Throws<AssertionException>(
            () => PauseContract.AssertFocusRecoveryDismissesOnlyFocusLoss(
                () => new ClearEverythingOnFocusRecoveryRun()));

        Expect.Multiple(() =>
        {
            Assert.That(
                singleReasonFailure.Message,
                Does.Contain("the run must be blocked while Fabrication is present"),
                "the toggle stub fails on the first reason it does not recognise, which is what proves "
                    + "VER-SIM-002-002 is checking every reason and not only the pause menu");
            Assert.That(
                overlappingFailure.Message,
                Does.Contain("must block the run"),
                "and it fails the overlapping-subset sweep too, because a toggle it does not set cannot "
                    + "block the run at all");
            Assert.That(
                focusRecoveryFailure.Message,
                Does.Contain("focus recovery never dismisses"),
                "the clear-everything stub fails for exactly the reason doc 10 § Pause contract states");
        });

        // The clear-everything stub is wrong in exactly one way: it still blocks correctly and still
        // resumes correctly. If it failed every assertion, this control would not show which one is
        // load-bearing.
        Expect.DoesNotThrow(
            () => PauseContract.AssertNoTickExecutesWhileAnySingleReasonIsPresent(
                () => new ClearEverythingOnFocusRecoveryRun()));
        Expect.DoesNotThrow(
            () => PauseContract.AssertResumesOnlyWhenEveryReasonIsCleared(
                () => new ClearEverythingOnFocusRecoveryRun()));

        // And the real run session passes all three, which is the positive half of the control.
        Expect.DoesNotThrow(
            () => PauseContract.AssertNoTickExecutesWhileAnySingleReasonIsPresent(
                () => new HostPausableRun()));
        Expect.DoesNotThrow(
            () => PauseContract.AssertResumesOnlyWhenEveryReasonIsCleared(() => new HostPausableRun()));
        Expect.DoesNotThrow(
            () => PauseContract.AssertFocusRecoveryDismissesOnlyFocusLoss(() => new HostPausableRun()));
    }
}
