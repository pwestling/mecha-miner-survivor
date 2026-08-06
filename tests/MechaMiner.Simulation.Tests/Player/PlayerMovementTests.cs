using System;
using System.Linq;
using System.Reflection;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Tests.Support;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Player;

/// <summary>
/// Phases 4 and 5 for the player: intent resolution, facing, and movement integration.
/// </summary>
/// <remarks>
/// Verification: <c>VER-PLY-001-002</c>, <c>VER-PLY-001-003</c>, <c>VER-PLY-001-004</c>,
/// <c>VER-PLY-001-005</c>.
///
/// The three documents that fix the speed rule, all agreeing:
/// <c>docs/technical/60-ui-input-and-accessibility.md</c>:65 - analog movement "remaps remaining
/// magnitude to [0,1], then the gameplay rule converts any nonzero magnitude to full movement speed
/// while preserving direction"; <c>docs/technical/20-simulation-core.md</c>:215 - "immediate
/// direction and full current speed for nonzero input"; and
/// <c>docs/30-combat-weapons-movement-camera.md</c>:64 - "Movement input sets the mech's direction
/// and full movement speed immediately."
/// </remarks>
[TestFixture]
internal sealed class PlayerMovementTests
{
    private const double Step = PlayerMovement.BaseDisplacementPerTickMeters;

    /// <summary>Bounds that constrain nothing, so a test of motion is only a test of motion.</summary>
    private sealed class UnboundedPlane : IPlanarBounds
    {
        public PlanarVector ResolveMove(PlanarVector from, PlanarVector proposed, double radius)
        {
            return proposed;
        }

        public bool Contains(PlanarVector centre, double radius)
        {
            return true;
        }
    }

    /// <summary>Bounds that refuse every move, to prove facing commits independently of motion.</summary>
    private sealed class ImmovablePlane : IPlanarBounds
    {
        public PlanarVector ResolveMove(PlanarVector from, PlanarVector proposed, double radius)
        {
            return from;
        }

        public bool Contains(PlanarVector centre, double radius)
        {
            return true;
        }
    }

    private static PlayerState Deployed()
    {
        return PlayerState.Deploy(PlanarVector.Zero);
    }

    private static PlayerState AdvanceOneTick(PlayerState state, MovementIntent intent, IPlanarBounds bounds)
    {
        PlayerSteering steering = PlayerSteering.Resolve(intent, state.FacingRadians);
        return PlayerMovement.Integrate(state, steering, bounds);
    }

    [Test]
    public void ADeployedBodyIsAtFullHullFacingEast()
    {
        PlayerState state = PlayerState.Deploy(PlanarVector.FromComponents(2.0, -3.0));

        Expect.Multiple(() =>
        {
            Assert.That(state.Position, Is.EqualTo(PlanarVector.FromComponents(2.0, -3.0)));
            Assert.That(state.FacingRadians, Is.EqualTo(PlayerBaseline.InitialFacingRadians));
            Assert.That(state.Hull, Is.EqualTo(PlayerBaseline.StartingHull));
            Assert.That(state.IsDestroyed, Is.False);
            Assert.That(state.Footprint.Radius, Is.EqualTo(PlayerBaseline.CollisionRadiusMeters));
            Assert.That(state.Footprint.Centre, Is.EqualTo(state.Position));
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-002</c>: magnitude sets direction, never speed.
    /// </summary>
    [Test]
    public void NonzeroIntentMovesAtFullBaseSpeedWhateverItsMagnitude()
    {
        UnboundedPlane plane = new();

        // Three samples along the same eastward direction with very different magnitudes. Under the
        // accepted rule all three travel identically; under the plausible wrong rule - magnitude as a
        // throttle - they would travel 0.05, 0.025, and 0.0005 metres.
        MovementIntent full = MovementIntent.Normalize(1.0, 0.0);
        MovementIntent half = MovementIntent.Normalize(0.5, 0.0);
        MovementIntent barely = MovementIntent.Normalize(0.01, 0.0);

        PlanarVector fromFull = AdvanceOneTick(Deployed(), full, plane).Position;
        PlanarVector fromHalf = AdvanceOneTick(Deployed(), half, plane).Position;
        PlanarVector fromBarely = AdvanceOneTick(Deployed(), barely, plane).Position;

        Expect.Multiple(() =>
        {
            PlanarAssert.AreClose(
                fromFull,
                PlanarVector.FromComponents(Step, 0.0),
                1e-15,
                "a fully deflected eastward sample covers one tick of base speed");
            PlanarAssert.AreClose(
                fromHalf,
                fromFull,
                1e-15,
                "a half-deflected sample travels the same distance as a full one: doc 60:65 converts "
                    + "\"any nonzero magnitude to full movement speed\". Treating magnitude as a "
                    + "throttle is the plausible wrong answer and would give 0.025 m here");
            PlanarAssert.AreClose(
                fromBarely,
                fromFull,
                1e-15,
                "and a barely deflected sample travels the same distance too");
        });
    }

    [Test]
    public void ADiagonalTravelsTheSameDistanceAsACardinal()
    {
        UnboundedPlane plane = new();

        PlayerState east = AdvanceOneTick(Deployed(), MovementIntent.Normalize(1.0, 0.0), plane);
        PlayerState diagonal = AdvanceOneTick(Deployed(), MovementIntent.Normalize(1.0, 1.0), plane);

        Expect.Multiple(() =>
        {
            Assert.That(
                diagonal.Position.Magnitude,
                Is.EqualTo(east.Position.Magnitude).Within(1e-15),
                "docs/30:64 gives digital input \"eight normalized directions\", so a diagonal is not "
                    + "faster than a cardinal");
            Assert.That(
                diagonal.Position.X,
                Is.EqualTo(diagonal.Position.Y).Within(1e-15),
                "a north-east diagonal splits evenly between the axes");
            Assert.That(diagonal.Position.X, Is.EqualTo(Step / Math.Sqrt(2.0)).Within(1e-15));
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-003</c>: releasing stops the body in the same tick.
    /// </summary>
    [Test]
    public void ZeroIntentStopsWithinTheSameTick()
    {
        UnboundedPlane plane = new();

        PlayerState moving = AdvanceOneTick(Deployed(), MovementIntent.Normalize(1.0, 0.0), plane);
        PlayerState released = AdvanceOneTick(moving, MovementIntent.Stop, plane);
        PlayerState stillReleased = AdvanceOneTick(released, MovementIntent.Stop, plane);

        Expect.Multiple(() =>
        {
            Assert.That(
                released.Position,
                Is.EqualTo(moving.Position),
                "docs/30:64 \"Releasing input stops the mech immediately\" - immediately means this tick, "
                    + "with no residual displacement. Any momentum at all would show up here");
            Assert.That(stillReleased.Position, Is.EqualTo(moving.Position), "and it stays stopped");
        });
    }

    [Test]
    public void ThereIsNoAccelerationRampAcrossTicks()
    {
        UnboundedPlane plane = new();
        MovementIntent east = MovementIntent.Normalize(1.0, 0.0);
        PlayerState state = Deployed();

        // Every tick must cover exactly the same distance. An acceleration ramp, a turn radius, or a
        // carried velocity would all make the first ticks differ from the later ones.
        for (int tick = 1; tick <= 10; tick++)
        {
            PlayerState next = AdvanceOneTick(state, east, plane);
            Assert.That(
                next.Position.X - state.Position.X,
                Is.EqualTo(Step).Within(1e-15),
                "tick " + tick + " covered a different distance from its predecessor; docs/30:64 forbids "
                    + "acceleration, braking lag, momentum, and turn radius");
            state = next;
        }

        Assert.That(state.Position.X, Is.EqualTo(10.0 * Step).Within(1e-14));
    }

    [Test]
    public void OneSecondOfTicksCoversExactlyTheBaseSpeed()
    {
        UnboundedPlane plane = new();
        MovementIntent east = MovementIntent.Normalize(1.0, 0.0);
        PlayerState state = Deployed();

        for (int tick = 0; tick < TickRate.TicksPerSecond; tick++)
        {
            state = AdvanceOneTick(state, east, plane);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                state.Position.X,
                Is.EqualTo(PlayerBaseline.BaseMovementSpeedMetersPerSecond).Within(1e-12),
                "60 ticks at 0.05 m is 3.0 m, which is docs/72:44's \"One base-travel second therefore "
                    + "equals 3.0M of shortest-path travel\"");
            Assert.That(state.Position.Y, Is.EqualTo(0.0));
        });
    }

    /// <summary>
    /// <c>VER-PLY-001-004</c>: facing follows nonzero intent and survives release.
    /// </summary>
    [Test]
    public void FacingFollowsNonzeroIntent()
    {
        UnboundedPlane plane = new();

        Expect.Multiple(() =>
        {
            Assert.That(
                AdvanceOneTick(Deployed(), MovementIntent.Normalize(0.0, 1.0), plane).FacingRadians,
                Is.EqualTo(Math.PI / 2.0).Within(1e-15),
                "pushing north faces north");
            Assert.That(
                AdvanceOneTick(Deployed(), MovementIntent.Normalize(-1.0, 0.0), plane).FacingRadians,
                Is.EqualTo(Math.PI).Within(1e-15),
                "pushing west faces west");
            Assert.That(
                AdvanceOneTick(Deployed(), MovementIntent.Normalize(0.0, -1.0), plane).FacingRadians,
                Is.EqualTo(-Math.PI / 2.0).Within(1e-15),
                "pushing south faces south");
        });
    }

    [Test]
    public void ReleasingPreservesTheLastNonzeroFacing()
    {
        UnboundedPlane plane = new();

        PlayerState facingNorth = AdvanceOneTick(Deployed(), MovementIntent.Normalize(0.0, 1.0), plane);
        PlayerState released = AdvanceOneTick(facingNorth, MovementIntent.Stop, plane);
        PlayerState stillReleased = AdvanceOneTick(released, MovementIntent.Stop, plane);

        Expect.Multiple(() =>
        {
            Assert.That(
                released.FacingRadians,
                Is.EqualTo(facingNorth.FacingRadians),
                "docs/30:70 \"Releasing movement preserves the last nonzero facing direction.\" Resetting "
                    + "to east on release would silently re-aim every facing-based weapon each time the "
                    + "player let go");
            Assert.That(stillReleased.FacingRadians, Is.EqualTo(facingNorth.FacingRadians));
            Assert.That(
                released.FacingRadians,
                Is.Not.EqualTo(PlayerBaseline.InitialFacingRadians),
                "and the preserved facing is genuinely not the initial one, so this test would fail if "
                    + "release reset to east");
        });
    }

    [Test]
    public void ABodyPressedIntoAWallStillTurnsToFaceThePush()
    {
        ImmovablePlane wall = new();

        PlayerState state = AdvanceOneTick(Deployed(), MovementIntent.Normalize(0.0, 1.0), wall);

        Expect.Multiple(() =>
        {
            Assert.That(state.Position, Is.EqualTo(PlanarVector.Zero), "the move was refused");
            Assert.That(
                state.FacingRadians,
                Is.EqualTo(Math.PI / 2.0).Within(1e-15),
                "docs/30:70 \"While movement input is nonzero, the mech faces in that direction\" is not "
                    + "qualified by whether the move succeeded");
        });
    }

    /// <summary>
    /// The smallest representable nonzero intent still commands full speed, because the rule is about
    /// nonzero-ness and not about magnitude.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This began as a test that a subnormal intent underflows to a zero direction and is therefore a
    /// stop. That premise is false and the test failed: <c>MovementIntent.Normalize</c> divides by the
    /// largest component, so a pair of <c>double.Epsilon</c> components survives as
    /// <c>(epsilon, epsilon)</c>, and <c>double.Hypot</c> of two subnormals is itself subnormal and
    /// nonzero. For any nonzero intent at least one component is at least <c>double.Epsilon</c>, so the
    /// magnitude is never exactly zero and the zero branch in <c>PlayerSteering.Resolve</c> is
    /// unreachable from any nonzero intent.
    /// </para>
    /// <para>
    /// Rewriting it then found a second, real defect: <c>PlanarVector.Normalized</c> returned
    /// <c>(1, 1)</c> for this input, a direction of length sqrt 2, which movement integration would
    /// have turned into a body moving 41 per cent faster than its speed stat. That is fixed at the
    /// source and pinned by
    /// <c>PlanarVectorTests.NormalizationIsUnitLengthEvenAtTheExtremesOfTheRange</c>.
    /// </para>
    /// <para>
    /// The guard is kept, and the reason it is kept is recorded at its own site rather than asserted
    /// here. What is asserted instead is the reachable behaviour, which is also the documented one: a
    /// vanishingly small sample is still nonzero input, so doc 60:65 gives it full movement speed. In
    /// the shipping path such a sample never arrives, because the adapter's radial deadzone turns it
    /// into an explicit stop first.
    /// </para>
    /// </remarks>
    [Test]
    public void TheSmallestRepresentableNonzeroIntentStillCommandsFullSpeed()
    {
        MovementIntent vanishing = MovementIntent.Normalize(double.Epsilon, double.Epsilon);
        PlayerState facingNorth = PlayerState.Create(PlanarVector.Zero, Math.PI / 2.0, 100);

        PlayerSteering steering = PlayerSteering.Resolve(vanishing, facingNorth.FacingRadians);

        Expect.Multiple(() =>
        {
            Assert.That(
                vanishing.IsStop,
                Is.False,
                "the intent is nonzero, which is the premise the rest of this rests on");
            Assert.That(
                steering.IsStop,
                Is.False,
                "so it is not a stop: doc 60:65 converts any nonzero magnitude to full movement speed");
            Assert.That(
                steering.Direction.Magnitude,
                Is.EqualTo(1.0).Within(1e-15),
                "and the direction is unit length, carrying no trace of how small the sample was");
            Assert.That(
                steering.FacingRadians,
                Is.EqualTo(Math.PI / 4.0).Within(1e-15),
                "an equal-component sample faces north-east");
        });
    }

    [Test]
    public void SteeringDirectionIsAlwaysUnitLengthOrExactlyZero()
    {
        double[] samples = { -1.0, -0.5, -0.01, 0.0, 0.01, 0.5, 1.0 };

        foreach (double x in samples)
        {
            foreach (double y in samples)
            {
                MovementIntent intent = MovementIntent.Normalize(x, y);
                PlayerSteering steering = PlayerSteering.Resolve(intent, 0.0);

                if (steering.IsStop)
                {
                    Assert.That(steering.Direction, Is.EqualTo(PlanarVector.Zero));
                    continue;
                }

                Assert.That(
                    steering.Direction.Magnitude,
                    Is.EqualTo(1.0).Within(1e-15),
                    "the steering direction carries no speed, so it is unit length at (" + x + "," + y + ")");
            }
        }
    }

    /// <summary>
    /// <c>VER-PLY-001-005</c>: the integrator is structurally incapable of reading a frame delta.
    /// </summary>
    [Test]
    public void TheIntegratorTakesNoDurationParameter()
    {
        // The same structural argument ISimulationWorld makes about itself: a member that cannot see
        // a duration cannot be handed a variable one. Asserted over the reflected signature rather
        // than by reading the source, so adding such a parameter later fails this test.
        MethodInfo[] methods = typeof(PlayerMovement)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.That(methods, Is.Not.Empty, "the integrator must exist for this to mean anything");

        foreach (MethodInfo method in methods)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                Assert.That(
                    parameter.ParameterType,
                    Is.Not.EqualTo(typeof(TimeSpan)),
                    method.Name + " takes a TimeSpan; doc 10 § Clock domains forbids passing a variable "
                        + "delta to an authoritative system");

                bool looksLikeADuration = parameter.ParameterType == typeof(double)
                    && (parameter.Name?.Contains("second", StringComparison.OrdinalIgnoreCase) == true
                        || parameter.Name?.Contains("delta", StringComparison.OrdinalIgnoreCase) == true
                        || parameter.Name?.Contains("elapsed", StringComparison.OrdinalIgnoreCase) == true
                        || parameter.Name?.Contains("duration", StringComparison.OrdinalIgnoreCase) == true);

                Assert.That(
                    looksLikeADuration,
                    Is.False,
                    method.Name + " takes '" + parameter.Name + "', which reads as a duration. The only "
                        + "duration in scope is TickRate.SecondsPerTick, a compile-time constant");
            }
        }

        Assert.That(
            typeof(PlayerMovement).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(method => method.Name == nameof(PlayerMovement.Integrate)),
            Is.True,
            "and the method this is about is present under the name asserted");
    }

    [Test]
    public void IntegrationRefusesANullBounds()
    {
        Assert.That(
            Expect.Throws<ArgumentNullException>(
                () => PlayerMovement.Integrate(
                    Deployed(),
                    PlayerSteering.Resolve(MovementIntent.Normalize(1.0, 0.0), 0.0),
                    null!)).ParamName,
            Is.EqualTo("bounds"));
    }

    [Test]
    public void HullIsCarriedThroughMovementUnchanged()
    {
        UnboundedPlane plane = new();
        PlayerState damaged = PlayerState.Create(PlanarVector.Zero, 0.0, 42);

        PlayerState moved = AdvanceOneTick(damaged, MovementIntent.Normalize(1.0, 0.0), plane);

        Assert.That(
            moved.Hull,
            Is.EqualTo(42),
            "movement is not a damage path; phases 9 and 10 own Hull and neither exists yet");
    }

    [Test]
    public void AnOutOfDomainHullOrFacingIsRefused()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlayerState.Create(PlanarVector.Zero, 0.0, -1)).ParamName,
                Is.EqualTo("hull"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlayerState.Create(PlanarVector.Zero, 0.0, PlayerBaseline.MaximumHull + 1))
                    .ParamName,
                Is.EqualTo("hull"),
                "doc 20 § Scope and invariants forbids a durability value above its maximum");
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlayerState.Create(PlanarVector.Zero, double.NaN, 100)).ParamName,
                Is.EqualTo("facingRadians"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlayerSteering.Resolve(MovementIntent.Stop, double.NaN)).ParamName,
                Is.EqualTo("currentFacingRadians"));
        });
    }

    [Test]
    public void ZeroHullIsReportedButNotActedOn()
    {
        PlayerState destroyed = PlayerState.Create(PlanarVector.Zero, 0.0, 0);

        Expect.Multiple(() =>
        {
            Assert.That(destroyed.IsDestroyed, Is.True);
            Assert.That(
                AdvanceOneTick(destroyed, MovementIntent.Normalize(1.0, 0.0), new UnboundedPlane())
                    .Position.X,
                Is.EqualTo(Step).Within(1e-15),
                "a destroyed body still integrates: refusing to move it would be a terminal rule, and "
                    + "phase 13 owns those. Run termination is out of this slice's scope");
        });
    }

    [Test]
    public void MovementAgainstTheGrayboxArenaStopsTangentToTheWall()
    {
        GrayboxArenaBounds arena = new(-1.0, -1.0, 1.0, 1.0);
        MovementIntent east = MovementIntent.Normalize(1.0, 0.0);
        PlayerState state = PlayerState.Deploy(PlanarVector.Zero);

        for (int tick = 0; tick < 200; tick++)
        {
            state = AdvanceOneTick(state, east, arena);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                state.Position.X,
                Is.EqualTo(1.0 - PlayerBaseline.CollisionRadiusMeters).Within(1e-12),
                "the body rests with its collision circle tangent to the wall");
            Assert.That(
                arena.Contains(state.Position, PlayerBaseline.CollisionRadiusMeters),
                Is.True,
                "and the whole footprint is inside");
        });
    }
}
