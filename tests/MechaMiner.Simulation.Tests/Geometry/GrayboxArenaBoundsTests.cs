using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Tests.Support;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Geometry;

/// <summary>
/// The graybox arena's clamp: the whole footprint stays inside, and clamping is idempotent.
/// </summary>
/// <remarks>
/// Verification: <c>VER-GEO-003-003</c>.
///
/// This is scaffolding that <c>MAP-007</c> replaces, and these assertions are about the scaffolding
/// rather than about terrain. What is deliberately not asserted, because the type deliberately does
/// not do it: obstacles, swept resolution, tangent sliding against a non-axis-aligned edge,
/// clearance, and correction of an already-penetrating body toward a validated free point. doc 21
/// § Player and enemy movement requires all of those of the real implementation.
/// </remarks>
[TestFixture]
internal sealed class GrayboxArenaBoundsTests
{
    private const double Radius = PlayerBaseline.CollisionRadiusMeters;

    private static PlanarVector At(double x, double y)
    {
        return PlanarVector.FromComponents(x, y);
    }

    [Test]
    public void TheDefaultArenaIsCentredOnTheOriginAndLargerThanTheViewport()
    {
        GrayboxArenaBounds arena = GrayboxArenaBounds.Default;

        Expect.Multiple(() =>
        {
            Assert.That(arena.MinimumX, Is.EqualTo(-GrayboxArenaBounds.DefaultHalfExtentMeters));
            Assert.That(arena.MaximumX, Is.EqualTo(GrayboxArenaBounds.DefaultHalfExtentMeters));
            Assert.That(arena.MinimumY, Is.EqualTo(-GrayboxArenaBounds.DefaultHalfExtentMeters));
            Assert.That(arena.MaximumY, Is.EqualTo(GrayboxArenaBounds.DefaultHalfExtentMeters));
            Assert.That(
                arena.MaximumY - arena.MinimumY,
                Is.GreaterThan(24.0),
                "the arena must exceed the camera's 24 m vertical extent (doc 30 § Camera) or the "
                    + "player could never see ground move past the mech");
            Assert.That(arena.Contains(PlanarVector.Zero, Radius), Is.True, "the origin is legal");
        });
    }

    /// <summary>
    /// The clamp is on the footprint, not the centre. A centre clamp looks right until the corners.
    /// </summary>
    [Test]
    public void TheWholeFootprintStaysInsideAtEveryEdge()
    {
        GrayboxArenaBounds arena = new(-10.0, -10.0, 10.0, 10.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                arena.ClampFootprint(At(100.0, 0.0), Radius),
                Is.EqualTo(At(10.0 - Radius, 0.0)),
                "driven east, the body stops with its circle tangent to the eastern wall, so its centre "
                    + "is one radius short of the edge rather than on it");
            Assert.That(arena.ClampFootprint(At(-100.0, 0.0), Radius), Is.EqualTo(At(-10.0 + Radius, 0.0)));
            Assert.That(arena.ClampFootprint(At(0.0, 100.0), Radius), Is.EqualTo(At(0.0, 10.0 - Radius)));
            Assert.That(arena.ClampFootprint(At(0.0, -100.0), Radius), Is.EqualTo(At(0.0, -10.0 + Radius)));
        });
    }

    [Test]
    public void BothAxesClampAtACorner()
    {
        GrayboxArenaBounds arena = new(-10.0, -10.0, 10.0, 10.0);

        Assert.That(
            arena.ClampFootprint(At(50.0, 50.0), Radius),
            Is.EqualTo(At(10.0 - Radius, 10.0 - Radius)),
            "a corner clamps on both axes; a centre-only clamp would leave half the circle outside "
                + "both walls at once, which is where that bug becomes visible");
    }

    [Test]
    public void ALegalPositionIsReturnedUnchangedAndTheClampIsIdempotent()
    {
        GrayboxArenaBounds arena = new(-10.0, -10.0, 10.0, 10.0);
        PlanarVector interior = At(1.25, -3.5);

        PlanarVector once = arena.ClampFootprint(At(100.0, 100.0), Radius);
        PlanarVector twice = arena.ClampFootprint(once, Radius);

        Expect.Multiple(() =>
        {
            Assert.That(
                arena.ClampFootprint(interior, Radius),
                Is.EqualTo(interior),
                "an already-legal centre is untouched");
            Assert.That(
                twice,
                Is.EqualTo(once),
                "clamping a clamped centre changes nothing further. A non-idempotent clamp would drift "
                    + "a body held against a wall along that wall, tick after tick, with nothing "
                    + "commanding the motion");
        });
    }

    [Test]
    public void ResolveMoveClampsAndIgnoresTheOriginForARectangleWithNoObstacles()
    {
        GrayboxArenaBounds arena = new(-10.0, -10.0, 10.0, 10.0);
        PlanarVector proposed = At(100.0, 2.0);

        PlanarVector fromWest = arena.ResolveMove(At(-9.0, 2.0), proposed, Radius);
        PlanarVector fromEast = arena.ResolveMove(At(9.0, 2.0), proposed, Radius);

        Expect.Multiple(() =>
        {
            Assert.That(fromWest, Is.EqualTo(At(10.0 - Radius, 2.0)));
            Assert.That(
                fromEast,
                Is.EqualTo(fromWest),
                "the rectangle has nothing for a swept test to hit, so the origin cannot change the "
                    + "answer. An implementation over real geometry must not ignore it, which is why "
                    + "the parameter is on the interface rather than absent from it");
        });
    }

    [Test]
    public void ContainsIsInclusiveAtTangencyAndExcludesAProtrudingFootprint()
    {
        GrayboxArenaBounds arena = new(-10.0, -10.0, 10.0, 10.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                arena.Contains(At(10.0 - Radius, 0.0), Radius),
                Is.True,
                "a body exactly tangent to a wall is inside");
            Assert.That(
                arena.Contains(At(10.0, 0.0), Radius),
                Is.False,
                "a centre on the wall puts half the circle outside");
            Assert.That(arena.Contains(At(0.0, 0.0), Radius), Is.True);
        });
    }

    [Test]
    public void EveryEdgeIsReachedByDrivingAtItFromTheCentre()
    {
        GrayboxArenaBounds arena = GrayboxArenaBounds.Default;
        double half = GrayboxArenaBounds.DefaultHalfExtentMeters;

        // Drive the body one tick at a time in each of the four cardinal directions until it stops
        // moving, and assert it stopped tangent to the expected wall rather than short of it or
        // beyond it. This is the graybox standing in for terrain, exercised the way phase 5 uses it.
        (PlanarVector Direction, PlanarVector Expected)[] cases =
        {
            (PlanarVector.East, At(half - Radius, 0.0)),
            (-PlanarVector.East, At(-half + Radius, 0.0)),
            (PlanarVector.North, At(0.0, half - Radius)),
            (-PlanarVector.North, At(0.0, -half + Radius)),
        };

        foreach ((PlanarVector direction, PlanarVector expected) in cases)
        {
            PlanarVector position = PlanarVector.Zero;

            // 20 m at 0.05 m per tick is 400 ticks; 2000 is generous headroom and still bounded, so a
            // clamp that failed to stop the body fails this test instead of looping forever.
            for (int tick = 0; tick < 2000; tick++)
            {
                PlanarVector proposed = position + (direction * PlayerMovement.BaseDisplacementPerTickMeters);
                position = arena.ResolveMove(position, proposed, Radius);
            }

            PlanarAssert.AreClose(
                position,
                expected,
                1e-9,
                "driving " + direction.ToString() + " should rest tangent to that wall");
        }
    }

    [Test]
    public void ADegenerateOrInvertedArenaIsRefused()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => new GrayboxArenaBounds(0.0, 0.0, 0.0, 1.0))
                    .ParamName,
                Is.EqualTo("maximumX"),
                "a zero-width arena makes every position illegal and every clamp arbitrary");
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => new GrayboxArenaBounds(0.0, 0.0, 1.0, -1.0))
                    .ParamName,
                Is.EqualTo("maximumY"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => new GrayboxArenaBounds(double.NaN, 0.0, 1.0, 1.0)).ParamName,
                Is.EqualTo("minimumX"));
        });
    }

    /// <summary>
    /// A body wider than the arena has no legal position, and that must be named as such rather than
    /// surfacing as an argument error about clamp bounds the caller never supplied.
    /// </summary>
    [Test]
    public void ABodyTooLargeForTheArenaIsRefusedByName()
    {
        GrayboxArenaBounds arena = new(-1.0, -1.0, 1.0, 1.0);

        ArgumentOutOfRangeException failure = Expect.Throws<ArgumentOutOfRangeException>(
            () => arena.ClampFootprint(PlanarVector.Zero, 5.0));

        Expect.Multiple(() =>
        {
            Assert.That(failure.ParamName, Is.EqualTo("radius"));
            Assert.That(
                failure.Message,
                Does.Contain("does not fit"),
                "the message must name the actual problem, which is that the body does not fit");
            Assert.That(
                arena.Contains(PlanarVector.Zero, 1.0),
                Is.True,
                "a body exactly filling the arena still fits");
        });
    }

    [Test]
    public void ANegativeOrNonFiniteRadiusIsRefused()
    {
        GrayboxArenaBounds arena = GrayboxArenaBounds.Default;

        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => arena.ClampFootprint(PlanarVector.Zero, -1.0)).ParamName,
                Is.EqualTo("radius"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => arena.Contains(PlanarVector.Zero, double.NaN)).ParamName,
                Is.EqualTo("radius"));
        });
    }
}
