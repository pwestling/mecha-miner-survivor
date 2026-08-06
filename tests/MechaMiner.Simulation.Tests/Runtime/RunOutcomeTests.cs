using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The run ends: once, in one of two distinguishable ways, without a technical failure.
/// </summary>
/// <remarks>Verification: <c>VER-PRG-006-001</c>, <c>VER-PRG-006-002</c>.</remarks>
[TestFixture]
internal sealed class RunOutcomeTests
{
    private static void Step(RunComposition run, long sequence)
    {
        _ = run.CommandGate.TryAdmit(run.ComposeEnvelope(sequence, 0.0, 0.0), out _);
        run.Host.Step(TickRate.SecondsPerTick);
        _ = run.SettleTerminalTransition();
    }

    private static RunComposition RunUntilDestroyed(ulong seed, out long ticks)
    {
        RunComposition run = RunComposition.CreateGraybox(seed, HarnessStressRows.LethalSwarm);
        ticks = 0;
        for (long tick = 0; tick < 7200 && !run.World.HasEnded; tick++)
        {
            Step(run, tick);
            ticks = tick + 1;
        }

        return run;
    }

    [Test]
    public void ARunWhoseHullReachesZeroIsSettledDestroyedByPhaseThirteen()
    {
        RunComposition run = RunUntilDestroyed(0xDEAD_0000_0001UL, out long ticks);

        Expect.Multiple(() =>
        {
            Assert.That(run.World.HasEnded, Is.True, "the run ended inside the tick budget");
            Assert.That(
                run.World.Terminal.Outcome,
                Is.EqualTo(RunOutcome.Destroyed),
                "docs/technical/10:156 - phase 13 evaluates 'death or extraction terminal conditions'");
            Assert.That(run.World.Player.Hull, Is.EqualTo(0));
            Assert.That(
                run.World.Terminal.Tick,
                Is.EqualTo(run.World.CommittedTickCount - 1),
                "settled on the tick Hull reached zero, not on a later one");
            Assert.That(
                run.World.BoundaryEvaluationCount,
                Is.EqualTo(0L),
                "and it was NOT the 35:00 boundary that settled it: the boundary was never reached, which "
                    + "is what makes the two outcomes distinguishable rather than two names for one path");
            Assert.That(ticks, Is.LessThan(7200L));
        });
    }

    [Test]
    public void ADestroyedRunPublishesItsTerminalFlagAndStopsWithoutATechnicalFailure()
    {
        RunComposition run = RunUntilDestroyed(0xDEAD_0000_0002UL, out _);
        long committedAtSettlement = run.World.CommittedTickCount;

        // Ten more host steps after the settlement.
        for (long extra = 0; extra < 10; extra++)
        {
            run.Host.Step(TickRate.SecondsPerTick);
        }

        PresentationSnapshot published = run.Snapshots.Latest!;

        Expect.Multiple(() =>
        {
            Assert.That(
                published.IsTerminal,
                Is.True,
                "phase 14 staged the terminal flag, so presentation can see the run ended without being "
                    + "told separately");
            Assert.That(
                run.Host.HasEndedInTechnicalFailure,
                Is.False,
                "and no technical failure was recorded. Raising the pause from inside phase 13 would have "
                    + "made the settling tick uncommittable and the host would have filed a crash report "
                    + "for a run that ended exactly as designed");
            Assert.That(
                run.Host.Clock.BlockingReasons.Contains(PauseReason.TerminalTransition),
                Is.True,
                "the one-way terminal transition is present, raised by the driver's bridge between steps");
            Assert.That(
                run.World.CommittedTickCount,
                Is.EqualTo(committedAtSettlement),
                "and ten further host steps ran no tick: doc 10 § Pause contract freezes every gameplay "
                    + "clock while a blocking reason is present");
        });
    }

    [Test]
    public void ATerminalResultIsAssignedOnceAndIsNeverReplaced()
    {
        RunComposition run = RunUntilDestroyed(0xDEAD_0000_0003UL, out _);
        RunTerminalResult settled = run.World.Terminal;

        // The boundary evaluation, called directly, on a run that already ended in destruction.
        run.World.EvaluateTerminalBoundary(RunClock.FinalBoundaryTick);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Terminal,
                Is.EqualTo(settled),
                "doc 20 § Scope and invariants - 'a run terminal result is assigned once and is "
                    + "immutable'. Reaching the boundary after a death does not convert a loss into a win");
            Assert.That(
                run.World.Terminal.Outcome,
                Is.EqualTo(RunOutcome.Destroyed));
            Assert.That(
                run.World.BoundaryEvaluationCount,
                Is.EqualTo(1L),
                "and the call was still recorded, so the host's ordering stays observable");
        });
    }

    [Test]
    public void ReachingTheFinalBoundaryAliveSettlesTheRunSurvived()
    {
        // Driven through EvaluateTerminalBoundary rather than by executing 126,000 ticks, because what is
        // being asserted is the member's contract. The engine-tier transcript drives the real 35 minutes.
        RunComposition run = RunComposition.CreateGraybox(0xA11E_0000_0001UL);
        for (long tick = 0; tick < 120; tick++)
        {
            Step(run, tick);
        }

        run.World.EvaluateTerminalBoundary(RunClock.FinalBoundaryTick);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Terminal.Outcome,
                Is.EqualTo(RunOutcome.Survived),
                "doc 20 § Boundary and tie ordering - at 35:00 'successful extraction is evaluated'");
            Assert.That(
                run.World.Terminal.Tick,
                Is.EqualTo(RunClock.FinalBoundaryTick.Index),
                "the boundary tick itself, index 126,000, which is never executed");
            Assert.That(
                run.World.Player.Hull,
                Is.GreaterThan(0),
                "the mech was alive, which is the only difference between this outcome and the other");
        });
    }

    [Test]
    public void TheHostRaisesTheTerminalTransitionItselfWhenTheBoundaryIsReached()
    {
        // The asymmetry worth asserting: the host knows when the clock reaches 35:00 and cannot know when
        // Hull reaches zero, so only the second needs the composition's bridge.
        RunComposition run = RunComposition.CreateGraybox(0xA11E_0000_0002UL);

        // One step large enough to exhaust the accumulator's catch-up bound repeatedly is not enough to
        // cross 35 minutes, so the clock is advanced by many steps. Each Step is one tick; running the
        // whole boundary here would be 126,000 iterations, which this fixture does deliberately because it
        // is the only way to observe the host reaching the boundary on its own.
        for (long tick = 0; tick < RunClock.FinalBoundaryTick.Index + 5; tick++)
        {
            if (run.Host.Clock.BlockingReasons.IsBlocking)
            {
                break;
            }

            _ = run.CommandGate.TryAdmit(run.ComposeEnvelope(tick, 0.0, 1.0), out _);
            run.Host.Step(TickRate.SecondsPerTick);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.BoundaryEvaluationCount,
                Is.EqualTo(1L),
                "the host reached the boundary and evaluated it once");
            Assert.That(
                run.World.Terminal.Outcome,
                Is.EqualTo(RunOutcome.Survived),
                "kiting north into a wall for 35 minutes at minute-0 pressure survives; the mech's own "
                    + "weapon clears the population faster than it replenishes");
            Assert.That(
                run.Host.Clock.BlockingReasons.Contains(PauseReason.TerminalTransition),
                Is.True,
                "and the host raised the transition itself, so SettleTerminalTransition finds it present");
            Assert.That(
                run.SettleTerminalTransition(),
                Is.False,
                "which is exactly what the bridge reports for a surviving run: nothing left to raise");
            Assert.That(run.Host.HasEndedInTechnicalFailure, Is.False);
        });
    }

    [Test]
    public void AnUndecidedOutcomeCannotBeAssignedAsAResult()
    {
        Assert.That(
            Expect.Throws<ArgumentOutOfRangeException>(
                () => RunTerminalResult.Assign(RunOutcome.Undecided, 10, 0)).ParamName,
            Is.EqualTo("outcome"),
            "a result carrying the undecided outcome would read as no result, so a run could be settled "
                + "and still look unsettled");
    }

    [Test]
    public void TheTwoOutcomesAreDistinguishableInEveryObservableWay()
    {
        RunComposition destroyed = RunUntilDestroyed(0xDEAD_0000_0004UL, out _);

        RunComposition survived = RunComposition.CreateGraybox(0xA11E_0000_0003UL);
        for (long tick = 0; tick < 120; tick++)
        {
            Step(survived, tick);
        }

        survived.World.EvaluateTerminalBoundary(RunClock.FinalBoundaryTick);

        Expect.Multiple(() =>
        {
            Assert.That(destroyed.World.Terminal.Outcome, Is.Not.EqualTo(survived.World.Terminal.Outcome));
            Assert.That(
                destroyed.World.Terminal.ToString(),
                Does.Contain("Destroyed").And.Not.Contain("Survived"),
                "and the rendered form says which, so an artifact does not need a lookup table");
            Assert.That(
                survived.World.Terminal.ToString(),
                Does.Contain("Survived").And.Not.Contain("Destroyed"));
            Assert.That(
                destroyed.World.Terminal.Tick,
                Is.LessThan(RunClock.FinalBoundaryTick.Index),
                "a destroyed run ended before the boundary and a survived one at it");
            Assert.That(
                survived.World.Terminal.Tick,
                Is.EqualTo(RunClock.FinalBoundaryTick.Index));
        });
    }
}
