using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MechaMiner.Simulation.Runtime;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The pause assertions themselves, factored out so the positive gates and the negative control run
/// the identical checks against different subjects.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>, <c>VER-SIM-002-005</c>, and the
/// negative control <c>VER-SIM-002-010</c>.
/// </para>
/// <para>
/// Every assertion uses <c>Assert.That</c> directly rather than <c>Expect.Multiple</c>:
/// <c>Expect.Multiple</c> raises <c>MultipleAssertException</c>, which does not derive from
/// <c>AssertionException</c>, so a negative control could not catch it. The first failure throwing
/// immediately is exactly what the control needs to observe.
/// </para>
/// </remarks>
internal static class PauseContract
{
    /// <summary>An arbitrarily long step, far more than the catch-up bound would ever permit.</summary>
    private const double LongStepSeconds = 10.0;

    /// <summary>
    /// Asserts that no tick executes while any single blocking reason is present, for each of the
    /// seven reasons alone.
    /// </summary>
    /// <param name="newRun">Creates a fresh, unblocked run.</param>
    /// <remarks>
    /// doc 10 § Pause contract: "The simulation executes no ticks while any blocking reason is
    /// present" and "Run time, AI, movement, spawning, projectiles, attacks, cooldowns, status
    /// effects, mining progress and decay, hazards, pickups, and gameplay physics remain
    /// unchanged."
    /// </remarks>
    internal static void AssertNoTickExecutesWhileAnySingleReasonIsPresent(Func<IPausableRun> newRun)
    {
        ArgumentNullException.ThrowIfNull(newRun);

        foreach (PauseReason reason in PauseReasonSet.AllReasons)
        {
            IPausableRun run = newRun();
            run.Raise(reason);

            Assert.That(
                run.IsBlocking,
                "the run must be blocked while " + reason.ToString() + " is present; doc 10 § Pause "
                    + "contract represents pause as a set of reasons, not a single toggle");

            int ticks = run.Step(LongStepSeconds);

            Assert.That(
                ticks,
                Is.EqualTo(0),
                "no tick may execute while " + reason.ToString()
                    + " is present, however long the step (doc 10 § Pause contract)");
            Assert.That(
                run.CommittedTickCount,
                Is.EqualTo(0L),
                "the tick index must be unchanged while " + reason.ToString() + " is present");
        }
    }

    /// <summary>
    /// Asserts, exhaustively over every non-empty subset of the seven reasons and in both entry
    /// orders, that the run stays blocked until the last clearable reason is cleared.
    /// </summary>
    /// <param name="newRun">Creates a fresh, unblocked run.</param>
    /// <remarks>
    /// <para>
    /// doc 10 § Pause contract: "Multiple reasons may overlap. Simulation resumes only when all
    /// blocking reasons are cleared." doc 10 § Verification requirements asks for "every pause
    /// source in overlapping combinations", which <c>tests/verification/SIM-002.json</c> reads as
    /// all 128 subsets rather than a sample.
    /// </para>
    /// <para>
    /// A subset containing <see cref="PauseReason.TerminalTransition"/> never resumes, because that
    /// reason is one-way (doc 20 § Scope and invariants: "a run terminal result is assigned once and
    /// is immutable"). The sweep asserts that outcome rather than skipping those subsets, so it stays
    /// exhaustive.
    /// </para>
    /// </remarks>
    internal static void AssertResumesOnlyWhenEveryReasonIsCleared(Func<IPausableRun> newRun)
    {
        ArgumentNullException.ThrowIfNull(newRun);

        for (int mask = 1; mask < PauseReasonSet.SubsetCount; mask++)
        {
            PauseReasonSet subset = PauseReasonSet.FromMask(mask);
            ImmutableArray<PauseReason> inDocumentOrder = subset.ToOrderedArray();

            AssertOneSubset(newRun, subset, inDocumentOrder, Reversed(inDocumentOrder));
            AssertOneSubset(newRun, subset, Reversed(inDocumentOrder), inDocumentOrder);
        }
    }

    /// <summary>
    /// Asserts that focus recovery clears focus loss and nothing else, against each of the four
    /// reasons doc 10 names.
    /// </summary>
    /// <param name="newRun">Creates a fresh, unblocked run.</param>
    /// <remarks>
    /// doc 10 § Pause contract: "Focus recovery never dismisses a menu, tutorial, relic choice, or
    /// user-requested pause."
    /// </remarks>
    internal static void AssertFocusRecoveryDismissesOnlyFocusLoss(Func<IPausableRun> newRun)
    {
        ArgumentNullException.ThrowIfNull(newRun);

        PauseReason[] mustSurvive =
        {
            PauseReason.GeneralPause,
            PauseReason.BlockingTutorialOrModal,
            PauseReason.Fabrication,
            PauseReason.RelicResolution,
        };

        foreach (PauseReason survivor in mustSurvive)
        {
            IPausableRun run = newRun();
            run.Raise(survivor);
            run.Raise(PauseReason.FocusLoss);

            run.RecoverFocus();

            Assert.That(
                run.Contains(PauseReason.FocusLoss),
                Is.False,
                "focus recovery clears focus loss");
            Assert.That(
                run.Contains(survivor),
                "focus recovery never dismisses " + survivor.ToString()
                    + "; doc 10 § Pause contract: \"Focus recovery never dismisses a menu, tutorial, "
                    + "relic choice, or user-requested pause\"");
            Assert.That(
                run.IsBlocking,
                "the run stays blocked because " + survivor.ToString() + " is still present");
            Assert.That(
                run.Step(LongStepSeconds),
                Is.EqualTo(0),
                "and therefore still executes no tick");
        }
    }

    /// <summary>Asserts one subset, raised in one order and cleared in another.</summary>
    private static void AssertOneSubset(
        Func<IPausableRun> newRun,
        PauseReasonSet subset,
        ImmutableArray<PauseReason> entryOrder,
        ImmutableArray<PauseReason> exitOrder)
    {
        IPausableRun run = newRun();
        string subsetName = "{" + subset.ToString() + "}";

        foreach (PauseReason reason in entryOrder)
        {
            run.Raise(reason);
            Assert.That(
                run.IsBlocking,
                "raising " + reason.ToString() + " into " + subsetName + " must block the run");
        }

        for (int index = 0; index < exitOrder.Length; index++)
        {
            run.ClearReason(exitOrder[index]);

            bool anyClearableRemains = index + 1 < exitOrder.Length;
            bool terminalPresent = subset.Contains(PauseReason.TerminalTransition);
            if (anyClearableRemains || terminalPresent)
            {
                Assert.That(
                    run.IsBlocking,
                    "clearing a proper subset of " + subsetName
                        + " must leave the run blocked; doc 10 § Pause contract: \"Simulation resumes "
                        + "only when all blocking reasons are cleared\"");
                Assert.That(
                    run.Step(LongStepSeconds),
                    Is.EqualTo(0),
                    "and no tick may execute while it is blocked");
            }
        }

        bool expectedStillBlocking = subset.Contains(PauseReason.TerminalTransition);
        Assert.That(
            run.IsBlocking,
            Is.EqualTo(expectedStillBlocking),
            expectedStillBlocking
                ? "a subset containing the terminal transition never resumes, because that reason is "
                    + "one-way (doc 20 § Scope and invariants)"
                : "clearing the last reason of " + subsetName + " resumes the run");

        if (!expectedStillBlocking)
        {
            Assert.That(
                run.Step(LongStepSeconds),
                Is.GreaterThan(0),
                "and the resumed run executes ticks again");
        }
    }

    /// <summary>Returns the reasons in reverse order, so both entry and exit orders are covered.</summary>
    private static ImmutableArray<PauseReason> Reversed(ImmutableArray<PauseReason> reasons)
    {
        List<PauseReason> reversed = new(reasons);
        reversed.Reverse();
        return reversed.ToImmutableArray();
    }
}
