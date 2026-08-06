using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.World;

/// <summary>
/// The world's tick: the documented phase order, held intent, publication, and the two provisional
/// seams.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-PLY-001-006</c>, <c>VER-PLY-001-007</c>, <c>VER-PLY-001-008</c>,
/// <c>VER-PLY-001-010</c>.
/// </para>
/// <para>
/// These tests drive <c>AdvanceTick</c> directly rather than through <c>SimulationHost</c>. That is
/// deliberate and it is not laziness about integration: a throw from <c>AdvanceTick</c> takes the
/// host down its technical-failure path, which records the failure, rethrows, and refuses every
/// later step, so a fixture that drove the host would get one exception and a dead host for the rest
/// of the case. <c>MovementCommandPathTests</c> is where the host drives the world, once, for the
/// integration claim.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class GameplayWorldTests
{
    private const ulong RunSession = 0xA5A5_0000_0000_0001UL;
    private const double Step = PlayerMovement.BaseDisplacementPerTickMeters;

    private static RunComposition Fresh()
    {
        return RunComposition.CreateGraybox(RunSession);
    }

    /// <summary>Submits a raw sample for whichever tick the window is open for.</summary>
    private static void Submit(RunComposition run, long sequence, double rawX, double rawY)
    {
        CommandEnvelope envelope = run.ComposeEnvelope(sequence, rawX, rawY);
        Assert.That(
            run.CommandGate.TryAdmit(envelope, out CommandRejection rejection),
            Is.True,
            "the sample should have been admitted but was rejected: " + rejection.ToString());
    }

    /// <summary>
    /// <c>VER-PLY-001-006</c>: the literal phase numbers, in sequence, as data.
    /// </summary>
    [Test]
    public void PhasesRunInTheDocumentedAscendingOrder()
    {
        RunComposition run = Fresh();

        run.World.AdvanceTick(SimulationTick.Zero);

        int[] observed = new int[run.World.LastTickPhaseCount];
        int written = run.World.CopyLastTickPhases(observed);

        // The literal numbers from doc 10:144-157. doc 10 § System phase ordering warns that
        // "renumbering an unchanged order is invisible to any test that asserts only relative order",
        // so this asserts the values and not merely that they ascend.
        int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

        Expect.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(14), "all fourteen phases were entered");
            Assert.That(
                observed,
                Is.EqualTo(expected),
                "the phase identifiers are contract and are never renumbered (doc 10 § System phase "
                    + "ordering). An empty phase is still entered, so the next package's work has an "
                    + "ordered place to go");
            Assert.That(observed, Is.Ordered.Ascending, "and they ascend");
        });
    }

    [Test]
    public void ThePhaseIdentifiersMatchTheDocumentedBindings()
    {
        Expect.Multiple(() =>
        {
            Assert.That(TickPhase.AdmitCommands, Is.EqualTo(1), "doc 10:144");
            Assert.That(TickPhase.EvaluateScheduleBoundaries, Is.EqualTo(2), "doc 10:145");
            Assert.That(TickPhase.MaterializeSpawns, Is.EqualTo(3), "doc 10:146");
            Assert.That(TickPhase.ResolveIntentAndSteering, Is.EqualTo(4), "doc 10:147");
            Assert.That(TickPhase.IntegrateMovement, Is.EqualTo(5), "doc 10:148");
            Assert.That(TickPhase.UpdateSpatialStructures, Is.EqualTo(6), "doc 10:149");
            Assert.That(TickPhase.AcquireTargets, Is.EqualTo(7), "doc 10:150");
            Assert.That(TickPhase.SimulateWeapons, Is.EqualTo(8), "doc 10:151");
            Assert.That(TickPhase.CollectDamageCandidates, Is.EqualTo(9), "doc 10:152");
            Assert.That(TickPhase.ResolveDamage, Is.EqualTo(10), "doc 10:153");
            Assert.That(TickPhase.AdvanceMining, Is.EqualTo(11), "doc 10:154");
            Assert.That(TickPhase.ApplyDeferredStructuralChanges, Is.EqualTo(12), "doc 10:155");
            Assert.That(TickPhase.EvaluateTerminalConditions, Is.EqualTo(13), "doc 10:156");
            Assert.That(TickPhase.Publish, Is.EqualTo(14), "doc 10:157");
            Assert.That(TickPhase.First, Is.EqualTo(1));
            Assert.That(TickPhase.Last, Is.EqualTo(14));
        });
    }

    [Test]
    public void APhaseOrderIsRecordedForEveryTickNotOnlyTheFirst()
    {
        RunComposition run = Fresh();

        run.World.AdvanceTick(SimulationTick.Zero);
        run.World.AdvanceTick(new SimulationTick(1));
        run.World.AdvanceTick(new SimulationTick(2));

        int[] observed = new int[run.World.LastTickPhaseCount];
        run.World.CopyLastTickPhases(observed);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.LastTickPhaseCount,
                Is.EqualTo(14),
                "the recording resets each tick rather than accumulating; three ticks would show 42 if "
                    + "it did not");
            Assert.That(observed[0], Is.EqualTo(TickPhase.AdmitCommands));
            Assert.That(observed[13], Is.EqualTo(TickPhase.Publish));
            Assert.That(run.World.CommittedTickCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ATickWithNoCommandLeavesTheBodyStopped()
    {
        RunComposition run = Fresh();

        run.World.AdvanceTick(SimulationTick.Zero);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Player.Position,
                Is.EqualTo(PlanarVector.Zero),
                "nothing was commanded, so nothing moved");
            Assert.That(
                run.World.Player.FacingRadians,
                Is.EqualTo(PlayerBaseline.InitialFacingRadians),
                "docs/30:70: before the first input the mech faces east");
        });
    }

    [Test]
    public void AnAdmittedIntentMovesTheBodyOnTheTickItTargets()
    {
        RunComposition run = Fresh();

        Submit(run, 0, 1.0, 0.0);
        run.World.AdvanceTick(SimulationTick.Zero);

        Expect.Multiple(() =>
        {
            Assert.That(run.World.Player.Position.X, Is.EqualTo(Step).Within(1e-15));
            Assert.That(run.World.Player.Position.Y, Is.EqualTo(0.0));
            Assert.That(run.World.HeldIntent.IsStop, Is.False);
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-007</c>: absence of a command is not a release.
    /// </summary>
    [Test]
    public void AnEmptyAdmittedSetKeepsTheHeldIntent()
    {
        RunComposition run = Fresh();

        // One command, then a burst of ticks carrying none - which is exactly the shape of a catch-up
        // burst, where the host runs several ticks for one frame and only the first has a sample.
        Submit(run, 0, 1.0, 0.0);
        run.World.AdvanceTick(SimulationTick.Zero);
        for (long index = 1; index <= 5; index++)
        {
            run.World.AdvanceTick(new SimulationTick(index));
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Player.Position.X,
                Is.EqualTo(6.0 * Step).Within(1e-14),
                "all six ticks moved. doc 20 § Active commands makes a stop an explicit zero-magnitude "
                    + "intent, so absence and zero are different facts: treating the five silent ticks "
                    + "as releases would leave the body at one step and stutter a held direction");
            Assert.That(run.World.HeldIntent.IsStop, Is.False, "the intent is still held");
        });
    }

    [Test]
    public void AnExplicitZeroIntentReleasesWhereSilenceDoesNot()
    {
        RunComposition run = Fresh();

        Submit(run, 0, 1.0, 0.0);
        run.World.AdvanceTick(SimulationTick.Zero);
        double afterOneStep = run.World.Player.Position.X;

        // An explicit zero sample, which is what the input adapter sends on release.
        Submit(run, 1, 0.0, 0.0);
        run.World.AdvanceTick(new SimulationTick(1));
        run.World.AdvanceTick(new SimulationTick(2));

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Player.Position.X,
                Is.EqualTo(afterOneStep).Within(1e-15),
                "an explicit zero stops the body, and it stays stopped through the silent tick after it");
            Assert.That(run.World.HeldIntent.IsStop, Is.True);
        });
    }

    [Test]
    public void TheLatestSampleForATickWinsWhenSeveralAreAdmitted()
    {
        RunComposition run = Fresh();

        // Several frames can elapse without a tick, so several samples target one tick. The last one
        // is the player's current input and must be the one that applies.
        Submit(run, 0, 1.0, 0.0);
        Submit(run, 1, -1.0, 0.0);
        Submit(run, 2, 0.0, 1.0);
        run.World.AdvanceTick(SimulationTick.Zero);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.Player.Position.Y,
                Is.EqualTo(Step).Within(1e-15),
                "the last sample, north, is the one that applied");
            Assert.That(run.World.Player.Position.X, Is.EqualTo(0.0).Within(1e-15));
            Assert.That(
                run.World.Player.FacingRadians,
                Is.EqualTo(Math.PI / 2.0).Within(1e-15),
                "and facing followed the same sample");
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-008</c>: publication carries what phase 5 integrated.
    /// </summary>
    [Test]
    public void PublicationCarriesTheIntegratedPositionAndTheHudHull()
    {
        RunComposition run = Fresh();

        Submit(run, 0, 0.0, 1.0);
        run.World.AdvanceTick(SimulationTick.Zero);

        PresentationSnapshot? published = run.Snapshots.Latest;

        Assert.That(published, Is.Not.Null, "phase 14 must have published");
        Expect.Multiple(() =>
        {
            Assert.That(published!.Tick, Is.EqualTo(0L));
            Assert.That(published.RunSession, Is.EqualTo(RunSession));
            Assert.That(
                published.PlayerPositionX,
                Is.EqualTo(run.World.Player.Position.X),
                "the snapshot carries the authoritative ground-plane centre unmodified (TDR-005)");
            Assert.That(published.PlayerPositionY, Is.EqualTo(run.World.Player.Position.Y));
            Assert.That(published.PlayerFacingRadians, Is.EqualTo(run.World.Player.FacingRadians));
            Assert.That(
                published.Hud.DisplayedHull,
                Is.EqualTo(PlayerBaseline.MaximumHull),
                "the HUD publishes the authoritative Hull, which is full because nothing damages it yet");
            Assert.That(published.Hud.IsPublished, Is.True);
            Assert.That(published.Hud.DisplayedArmor, Is.EqualTo(0), "docs/72:36 Armor is 0");
            Assert.That(published.Hud.DisplayedCommonOre, Is.EqualTo(0L));
            Assert.That(published.Hud.DisplayedHyperGold, Is.EqualTo(0L));
            Assert.That(published.Hud.DisplayedExtractionPercent, Is.EqualTo(0));
            Assert.That(published.IsTerminal, Is.False, "run termination is out of this slice's scope");
            Assert.That(published.VisibleEntityCount, Is.EqualTo(0), "the player is not an entity entry");
        });
    }

    [Test]
    public void TheRunClockOnTheHudIsDerivedFromTheIntegerTickIndex()
    {
        RunComposition run = Fresh();

        // 120 ticks is exactly two seconds at 60 Hz. Derived from the index, never accumulated.
        for (long index = 0; index < 120; index++)
        {
            run.World.AdvanceTick(new SimulationTick(index));
        }

        Assert.That(
            run.Snapshots.Latest!.Hud.DisplayedRunClockSeconds,
            Is.EqualTo(1),
            "tick 119 covers [119/60, 120/60) seconds, so its run clock is 1.98s and the HUD's rounding "
                + "rule displays 2 only from tick 120. doc 10 § Clock domains derives game time from the "
                + "integer tick count");
    }

    [Test]
    public void TwoPublicationsGiveThePresentationLayerAPreviousAndALatest()
    {
        RunComposition run = Fresh();

        Submit(run, 0, 1.0, 0.0);
        run.World.AdvanceTick(SimulationTick.Zero);
        run.World.AdvanceTick(new SimulationTick(1));

        Expect.Multiple(() =>
        {
            Assert.That(run.Snapshots.Previous, Is.Not.Null, "presentation interpolates between two");
            Assert.That(run.Snapshots.Previous!.Tick, Is.EqualTo(0L));
            Assert.That(run.Snapshots.Latest!.Tick, Is.EqualTo(1L));
            Assert.That(
                run.Snapshots.Latest.PlayerPositionX,
                Is.GreaterThan(run.Snapshots.Previous.PlayerPositionX),
                "and the two differ, so there is something to interpolate");
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-010</c>: the provisional seams decide nothing.
    /// </summary>
    [Test]
    public void TheProvisionalSeamsDecideNothing()
    {
        RunComposition run = Fresh();

        run.World.AdvanceTick(SimulationTick.Zero);
        PresentationSnapshot? beforeSeams = run.Snapshots.Latest;
        PlayerState playerBeforeSeams = run.World.Player;

        run.World.EvaluateTerminalBoundary(RunClockBoundaryTick());
        run.World.BeginScheduledEvent(new SimulationTick(30), "SCHED-TEST-ROW");

        Expect.Multiple(() =>
        {
            Assert.That(run.World.BoundaryEvaluationCount, Is.EqualTo(1), "the call was recorded");
            Assert.That(run.World.ScheduledEventCount, Is.EqualTo(1));
            Assert.That(run.World.LastScheduledEventId, Is.EqualTo("SCHED-TEST-ROW"));

            Assert.That(
                run.World.Player,
                Is.EqualTo(playerBeforeSeams),
                "neither seam changed authoritative state");
            Assert.That(
                run.Snapshots.Latest,
                Is.SameAs(beforeSeams),
                "and neither published a snapshot. The boundary tick is never executed, so it has no "
                    + "phase 14, and staging one would invent a tick the run clock never committed");
            Assert.That(
                run.Snapshots.Latest!.IsTerminal,
                Is.False,
                "EvaluateTerminalBoundary proposes no terminal result: ISimulationWorld's own remarks "
                    + "put extraction resolution in the packages that own damage and extraction");
        });
    }

    [Test]
    public void AScheduledEventWithNoIdentityIsRefused()
    {
        RunComposition run = Fresh();

        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentException>(
                    () => run.World.BeginScheduledEvent(new SimulationTick(1), "  ")).ParamName,
                Is.EqualTo("scheduleEventId"));
            Assert.That(run.World.ScheduledEventCount, Is.EqualTo(0), "and nothing was recorded");
        });
    }

    [Test]
    public void TheWorldRefusesToBeComposedAcrossTwoRuns()
    {
        // The gate and the publisher must speak for one run: doc 10 § Commands and mutations makes the
        // run-session identity what keeps a command from crossing between runs.
        CommandAdmissionGate gate = new(RunSession);
        SnapshotPublisher publisher = new(RunSession + 1, 1, 1, 1);

        ArgumentException failure = Expect.Throws<ArgumentException>(
            () => new GameplayWorld(
                gate,
                publisher,
                new DomainEventBuffer(1, 8),
                new PresentationEventBuffer(1, 8),
                PresentationCoalescingPolicy.Verbatim,
                GrayboxArenaBounds.Default,
                PlanarVector.Zero));

        Assert.That(failure.ParamName, Is.EqualTo("publisher"));
    }

    [Test]
    public void TheWorldRefusesADeploymentPositionOutsideItsBounds()
    {
        CommandAdmissionGate gate = new(RunSession);
        SnapshotPublisher publisher = new(RunSession, 1, 1, 1);

        ArgumentException failure = Expect.Throws<ArgumentException>(
            () => new GameplayWorld(
                gate,
                publisher,
                new DomainEventBuffer(1, 8),
                new PresentationEventBuffer(1, 8),
                PresentationCoalescingPolicy.Verbatim,
                new GrayboxArenaBounds(-1.0, -1.0, 1.0, 1.0),
                PlanarVector.FromComponents(50.0, 0.0)));

        Expect.Multiple(() =>
        {
            Assert.That(failure.ParamName, Is.EqualTo("deploymentPosition"));
            Assert.That(
                failure.Message,
                Does.Contain("movement nobody commanded"),
                "the message names the consequence: the first phase 5 would correct the body, which "
                    + "looks like motion nothing asked for");
        });
    }

    [Test]
    public void ANullDependencyIsRefusedAtConstruction()
    {
        CommandAdmissionGate gate = new(RunSession);
        SnapshotPublisher publisher = new(RunSession, 1, 1, 1);

        Assert.That(
            Expect.Throws<ArgumentNullException>(
                () => new GameplayWorld(
                    gate,
                    publisher,
                    new DomainEventBuffer(1, 8),
                    new PresentationEventBuffer(1, 8),
                    PresentationCoalescingPolicy.Verbatim,
                    null!,
                    PlanarVector.Zero)).ParamName,
            Is.EqualTo("bounds"));
    }

    [Test]
    public void TheAdmissionWindowIsOpenForTickZeroBeforeTheFirstTickRuns()
    {
        RunComposition run = Fresh();

        Expect.Multiple(() =>
        {
            Assert.That(
                run.CommandGate.IsAdmissionOpen,
                Is.True,
                "something has to open the first window, because presentation submits before the first "
                    + "tick runs; the constructor does");
            Assert.That(run.OpenTick, Is.EqualTo(SimulationTick.Zero));
        });
    }

    [Test]
    public void TheWindowAdvancesToTheNextTickAfterEachTick()
    {
        RunComposition run = Fresh();

        run.World.AdvanceTick(SimulationTick.Zero);
        SimulationTick afterFirst = run.OpenTick;
        run.World.AdvanceTick(new SimulationTick(1));

        Expect.Multiple(() =>
        {
            Assert.That(afterFirst, Is.EqualTo(new SimulationTick(1)));
            Assert.That(run.OpenTick, Is.EqualTo(new SimulationTick(2)));
            Assert.That(run.CommandGate.IsAdmissionOpen, Is.True, "and it is open across the gap");
        });
    }

    [Test]
    public void AdvancingATickTheWindowIsNotOpenForIsAnInvariantFailure()
    {
        RunComposition run = Fresh();

        // The window is open for tick 0. Asking for tick 5 is a defect, and it must fail loudly: doc 20
        // § Tick transaction ends the run rather than publishing a tick whose commands were never
        // frozen.
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => run.World.AdvanceTick(new SimulationTick(5)));

        Assert.That(failure.Message, Does.Contain("must freeze the admission window for tick"));
    }

    [Test]
    public void CopyingThePhaseOrderIntoATooShortBufferIsRefused()
    {
        RunComposition run = Fresh();
        run.World.AdvanceTick(SimulationTick.Zero);

        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentException>(() => run.World.CopyLastTickPhases(new int[3])).ParamName,
                Is.EqualTo("destination"));
            Assert.That(
                Expect.Throws<ArgumentNullException>(() => run.World.CopyLastTickPhases(null!)).ParamName,
                Is.EqualTo("destination"));
        });
    }

    private static SimulationTick RunClockBoundaryTick()
    {
        return RunClock.FinalBoundaryTick;
    }
}
