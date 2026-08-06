using System;
using MechaMiner.Simulation.Commands;
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
/// The whole input path, end to end through the real host: an intent submitted, admitted,
/// integrated, and published at the position expected.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-PLY-001-009</c>.
/// </para>
/// <para>
/// This is the one fixture that drives <c>SimulationHost</c> rather than calling
/// <c>AdvanceTick</c> directly, because the claim is about the composition and not about the world:
/// that the host's accumulator, the gate's admission window, phase 5's integration, and phase 14's
/// publication line up when wired the way the shipping composition wires them.
/// </para>
/// <para>
/// It steps the host with exact multiples of <c>TickRate.SecondsPerTick</c>. That is not a
/// determinism dodge - it is the only honest way to assert a tick count, since the host's whole
/// purpose is to convert a variable frame delta into a whole number of fixed ticks, and a test that
/// fed it an arbitrary delta would be asserting the accumulator's rounding rather than the movement.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class MovementCommandPathTests
{
    private const ulong RunSession = 0x0BAD_C0DE_0000_0007UL;
    private const double Step = PlayerMovement.BaseDisplacementPerTickMeters;
    private const double OneTickOfWallClock = TickRate.SecondsPerTick;

    /// <summary>
    /// The headline claim: submit east, step one tick, and read the expected position back out of the
    /// published snapshot.
    /// </summary>
    [Test]
    public void AnIntentSubmittedIsAdmittedIntegratedAndPublished()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        // Submitted, for the tick the window is open for.
        CommandEnvelope envelope = run.ComposeEnvelope(sequence: 0, rawInputX: 1.0, rawInputY: 0.0);
        bool admitted = run.CommandGate.TryAdmit(envelope, out CommandRejection rejection);

        // Integrated: one tick of wall clock is exactly one tick of simulation.
        HostStepResult result = run.Host.Step(OneTickOfWallClock);

        PresentationSnapshot? published = run.Snapshots.Latest;

        Assert.That(published, Is.Not.Null, "the tick must have published a snapshot");
        Expect.Multiple(() =>
        {
            Assert.That(admitted, Is.True, "admitted at phase 1; rejection was " + rejection.ToString());
            Assert.That(envelope.TargetTick, Is.EqualTo(SimulationTick.Zero));
            Assert.That(result.TickCount, Is.EqualTo(1), "one tick ran");
            Assert.That(run.CommandGate.AdmittedInRun, Is.EqualTo(1));
            Assert.That(run.CommandGate.RejectedInRun, Is.EqualTo(0));

            Assert.That(
                published!.PlayerPositionX,
                Is.EqualTo(Step).Within(1e-15),
                "3.0 m/s at 60 Hz is 0.05 m for one tick, and that is what the snapshot carries");
            Assert.That(published.PlayerPositionY, Is.EqualTo(0.0));
            Assert.That(
                published.PlayerFacingRadians,
                Is.EqualTo(0.0),
                "pushing east faces east, which is zero radians");
            Assert.That(published.Tick, Is.EqualTo(0L));
            Assert.That(published.Hud.DisplayedHull, Is.EqualTo(PlayerBaseline.MaximumHull));
        });
    }

    [Test]
    public void OneSecondOfHostSteppingMovesExactlyTheBaseSpeed()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        // Hold north for one second of ticks, resubmitting each frame the way an adapter does.
        for (int frame = 0; frame < TickRate.TicksPerSecond; frame++)
        {
            CommandEnvelope envelope = run.ComposeEnvelope(frame, 0.0, 1.0);
            Assert.That(
                run.CommandGate.TryAdmit(envelope, out CommandRejection rejection),
                Is.True,
                "frame " + frame + " was rejected: " + rejection.ToString());
            run.Host.Step(OneTickOfWallClock);
        }

        PresentationSnapshot published = run.Snapshots.Latest!;

        Expect.Multiple(() =>
        {
            Assert.That(run.Host.Clock.CommittedTickCount, Is.EqualTo(TickRate.TicksPerSecond));
            Assert.That(
                published.PlayerPositionY,
                Is.EqualTo(PlayerBaseline.BaseMovementSpeedMetersPerSecond).Within(1e-12),
                "docs/72:44 \"One base-travel second therefore equals 3.0M of shortest-path travel\"");
            Assert.That(published.PlayerPositionX, Is.EqualTo(0.0).Within(1e-15));
            Assert.That(
                published.PlayerFacingRadians,
                Is.EqualTo(Math.PI / 2.0).Within(1e-15),
                "and it is facing north");
        });
    }

    /// <summary>
    /// A held direction must survive a catch-up burst, in which one long frame becomes several ticks
    /// and only the first of them carries a command.
    /// </summary>
    /// <remarks>
    /// The burst length is the host's own catch-up bound rather than a number chosen here.
    /// <c>CatchUpPolicy.Default</c> tolerates a 50 ms stall, which at 60 Hz is three ticks, plus one
    /// headroom tick: four. Asking for five ticks in one step does not produce five, it produces the
    /// bound - which is the behaviour, not a defect - so the expectation is derived from the policy and
    /// the step is deliberately longer than the bound so the clamp is what is being exercised.
    /// </remarks>
    [Test]
    public void AHeldDirectionSurvivesACatchUpBurst()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);
        int bound = run.Host.CatchUpPolicy.MaximumTicksPerStep;

        // One sample, then a single long frame. Only tick 0 carries a command; the rest are silent, and
        // a held direction must survive them.
        Assert.That(
            run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 1.0, 0.0), out _),
            Is.True);

        HostStepResult result = run.Host.Step(OneTickOfWallClock * (bound + 4));

        Expect.Multiple(() =>
        {
            Assert.That(bound, Is.EqualTo(4), "50 ms of tolerated stall at 60 Hz is 3 ticks, plus 1 headroom");
            Assert.That(
                result.TickCount,
                Is.EqualTo(bound),
                "a frame longer than the catch-up bound is clamped to the bound rather than running every "
                    + "tick it is owed; that is what keeps a stall from becoming an unbounded burst");
            Assert.That(result.CatchUpBoundReached, Is.True, "and the host reports that it clamped");
            Assert.That(
                run.Snapshots.Latest!.PlayerPositionX,
                Is.EqualTo(bound * Step).Within(1e-14),
                "every tick in the burst moved. Treating a tick with no admitted command as a release "
                    + "would leave the body at one step and make a held key stutter whenever the frame "
                    + "rate dipped");
        });
    }

    [Test]
    public void AFrameShorterThanATickRunsNoTickAndPublishesNothing()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        Assert.That(run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 1.0, 0.0), out _), Is.True);
        HostStepResult result = run.Host.Step(OneTickOfWallClock / 4.0);

        Expect.Multiple(() =>
        {
            Assert.That(result.TickCount, Is.EqualTo(0), "a quarter tick of wall clock runs no tick");
            Assert.That(
                run.Snapshots.Latest,
                Is.Null,
                "and nothing is published, so presentation has nothing to interpolate and must keep "
                    + "showing what it already had");
            Assert.That(
                run.World.Player.Position,
                Is.EqualTo(PlanarVector.Zero),
                "the authoritative body has not moved either");
        });
    }

    [Test]
    public void FourFramesOfAQuarterTickAccumulateIntoExactlyOneTick()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        Assert.That(run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 1.0, 0.0), out _), Is.True);

        int executed = 0;
        for (int frame = 0; frame < 4; frame++)
        {
            executed += run.Host.Step(OneTickOfWallClock / 4.0).TickCount;
        }

        Expect.Multiple(() =>
        {
            Assert.That(executed, Is.EqualTo(1), "the accumulator paid out one tick, not four and not none");
            Assert.That(run.Snapshots.Latest!.PlayerPositionX, Is.EqualTo(Step).Within(1e-15));
        });
    }

    /// <summary>
    /// A sample addressed at a tick that has already frozen is rejected rather than applied late.
    /// </summary>
    [Test]
    public void ASampleForAFrozenTickIsRejectedRatherThanAppliedLate()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        // Compose for tick 0, then let tick 0 run before submitting. The envelope is now stale.
        CommandEnvelope stale = run.ComposeEnvelope(0, 1.0, 0.0);
        run.Host.Step(OneTickOfWallClock);

        bool admitted = run.CommandGate.TryAdmit(stale, out CommandRejection rejection);

        Expect.Multiple(() =>
        {
            Assert.That(admitted, Is.False, "tick 0 has frozen, so a sample for it cannot be admitted");
            Assert.That(run.CommandGate.RejectedInRun, Is.EqualTo(1));
            Assert.That(
                run.Snapshots.Latest!.PlayerPositionX,
                Is.EqualTo(0.0),
                "and the stale sample changed nothing: applying it a tick late would be a command "
                    + "applied outside the tick it was normalized for");
            Assert.That(rejection.ToString(), Is.Not.Empty, "the rejection names a reason");
        });
    }

    [Test]
    public void ThePublishedPositionIsTheOnlyRouteToPlayerPositionAndMatchesTheWorld()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        Assert.That(run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 1.0, 1.0), out _), Is.True);
        run.Host.Step(OneTickOfWallClock);

        PresentationSnapshot published = run.Snapshots.Latest!;
        PlayerState authoritative = run.World.Player;

        Expect.Multiple(() =>
        {
            Assert.That(published.PlayerPositionX, Is.EqualTo(authoritative.Position.X));
            Assert.That(published.PlayerPositionY, Is.EqualTo(authoritative.Position.Y));
            Assert.That(published.PlayerFacingRadians, Is.EqualTo(authoritative.FacingRadians));
            Assert.That(
                published.PlayerPositionX,
                Is.EqualTo(Step / Math.Sqrt(2.0)).Within(1e-15),
                "a north-east diagonal is normalized, so it is not faster than a cardinal");
        });
    }

    /// <summary>
    /// The published pair maps into presentation with the documented signs, which is the last link in
    /// the chain from a key press to a rendered position.
    /// </summary>
    [Test]
    public void ThePublishedPairMapsIntoPresentationWithTheDocumentedSigns()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        // Push north, so the sign that TDR-005 fixes is the one under test.
        Assert.That(run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 0.0, 1.0), out _), Is.True);
        run.Host.Step(OneTickOfWallClock);

        PresentationSnapshot published = run.Snapshots.Latest!;
        PlanarVector authoritative = PlanarVector.FromComponents(
            published.PlayerPositionX,
            published.PlayerPositionY);

        PresentationGroundMapping.ToPresentationWorld(
            authoritative,
            0.0,
            out double worldX,
            out double worldY,
            out double worldZ);

        Expect.Multiple(() =>
        {
            Assert.That(authoritative.Y, Is.EqualTo(Step).Within(1e-15), "it moved north on the plane");
            Assert.That(
                worldZ,
                Is.EqualTo(-Step).Within(1e-15),
                "and north renders along world negative Z (TDR-005 § Coordinate contract)");
            Assert.That(worldX, Is.EqualTo(0.0));
            Assert.That(worldY, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void APausedRunRunsNoTickAndTheBodyDoesNotMove()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        Assert.That(run.CommandGate.TryAdmit(run.ComposeEnvelope(0, 1.0, 0.0), out _), Is.True);
        run.Host.Clock.Raise(PauseReason.GeneralPause);

        HostStepResult result = run.Host.Step(OneTickOfWallClock * 10.0);

        Expect.Multiple(() =>
        {
            Assert.That(result.TickCount, Is.EqualTo(0), "no tick executes while a reason is present");
            Assert.That(run.World.Player.Position, Is.EqualTo(PlanarVector.Zero));
            Assert.That(run.Snapshots.Latest, Is.Null, "and nothing was published");
        });

        run.Host.Clock.Clear(PauseReason.GeneralPause);
        run.Host.Step(OneTickOfWallClock);

        Assert.That(
            run.Snapshots.Latest!.PlayerPositionX,
            Is.EqualTo(Step).Within(1e-15),
            "and clearing the reason resumes the run with the intent still held");
    }

    [Test]
    public void TheBodyCannotLeaveTheGrayboxArena()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        // Hold east for long enough to cross the arena several times over: 20 m at 3 m/s is under
        // 7 seconds, and this drives 20.
        int sequence = 0;
        for (int frame = 0; frame < TickRate.TicksPerSecond * 20; frame++)
        {
            run.CommandGate.TryAdmit(run.ComposeEnvelope(sequence++, 1.0, 0.0), out _);
            run.Host.Step(OneTickOfWallClock);
        }

        PresentationSnapshot published = run.Snapshots.Latest!;
        double expectedRestX =
            GrayboxArenaBounds.DefaultHalfExtentMeters - PlayerBaseline.CollisionRadiusMeters;

        Expect.Multiple(() =>
        {
            Assert.That(
                published.PlayerPositionX,
                Is.EqualTo(expectedRestX).Within(1e-9),
                "the body rests with its collision circle tangent to the eastern wall of the graybox "
                    + "arena, which MAP-007 replaces");
            Assert.That(
                GrayboxArenaBounds.Default.Contains(
                    PlanarVector.FromComponents(published.PlayerPositionX, published.PlayerPositionY),
                    PlayerBaseline.CollisionRadiusMeters),
                Is.True,
                "and the whole footprint is still inside");
        });
    }

    [Test]
    public void AnEnvelopeFromAnotherRunIsRefused()
    {
        RunComposition run = RunComposition.CreateGraybox(RunSession);

        CommandEnvelope foreign = CommandEnvelope.Create(
            RunSession + 1,
            run.OpenTick,
            0,
            1.0,
            0.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.CommandGate.TryAdmit(foreign, out _),
                Is.False,
                "doc 10 § Commands and mutations makes the run-session identity what keeps a command "
                    + "from crossing between runs");
            Assert.That(run.RunSession, Is.EqualTo(RunSession));
        });
    }
}
