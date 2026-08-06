using System;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Encounters;

/// <summary>
/// The pure-pursuer steering rule: straight at the mech, at the identity's own speed, through anything
/// in the way.
/// </summary>
/// <remarks>Verification: <c>VER-ENC-001-002</c>.</remarks>
[TestFixture]
internal sealed class EnemyPursuitTests
{
    private const double Tolerance = 1e-12;

    private static readonly EnemyProfile Skitterling = EnemyRoster.Skitterling;

    [Test]
    public void OneTickOfPursuitCoversTheIdentitysOwnPerTickDisplacement()
    {
        Assert.That(
            EnemyPursuit.DisplacementPerTickMeters(Skitterling),
            Is.EqualTo(1.26 / TickRate.TicksPerSecond).Within(Tolerance),
            "1.26 m/s (docs/72:65) over one 60 Hz tick is 0.021 m. Derived from TickRate.SecondsPerTick "
                + "and nothing else, so doc 10 § Clock domains' ban on a variable delta reaching an "
                + "authoritative system holds by construction");
    }

    [Test]
    public void AHundredTicksOfPursuitCoversTheDocumentedDistance()
    {
        // Straight west of the mech, so the direction is exactly east and no normalization error can
        // accumulate into the distance being measured.
        EnemyState state = EnemyState.Spawn(Skitterling, PlanarVector.FromComponents(-10.0, 0.0));
        PlanarVector target = PlanarVector.Zero;
        PlanarVector start = state.Position;

        for (int tick = 0; tick < 100; tick++)
        {
            state = EnemyPursuit.Advance(state, Skitterling, target);
        }

        Assert.That(
            start.DistanceTo(state.Position),
            Is.EqualTo(1.26 * 100.0 / TickRate.TicksPerSecond).Within(1e-9),
            "100 ticks is 1.6667 s, and 1.26 m/s over that is 2.1 m. A pursuer that had acceleration, a "
                + "turn rate, or a per-tick rounding error would not land on the product");
    }

    [Test]
    public void APursuerWalksStraightAtTheMechFromEveryBearing
        ([Values(0.0, 0.7, 1.5707963267948966, 3.0, -2.2, 4.9)] double bearing)
    {
        PlanarVector target = PlanarVector.FromComponents(3.0, -4.0);
        PlanarVector from = target + (PlanarVector.FromBearing(bearing) * 9.0);
        EnemyState state = EnemyState.Spawn(Skitterling, from);

        EnemyState advanced = EnemyPursuit.Advance(state, Skitterling, target);

        Expect.Multiple(() =>
        {
            Assert.That(
                advanced.Position.DistanceTo(target),
                Is.EqualTo(9.0 - EnemyPursuit.DisplacementPerTickMeters(Skitterling)).Within(1e-9),
                "the whole step closed the gap, so the direction was exactly toward the target");
            Assert.That(
                from.DistanceTo(advanced.Position),
                Is.EqualTo(EnemyPursuit.DisplacementPerTickMeters(Skitterling)).Within(1e-9),
                "and the step was exactly one tick's displacement regardless of how far away it started, "
                    + "which is what distinguishes a unit direction from an unnormalized one");
        });
    }

    [Test]
    public void APursuerWhoseCentreIsTheMechsDoesNotMoveOrAcquireADirectionFromNothing()
    {
        // Reachable rather than theoretical: docs/31:26 makes ordinary enemies non-solid, so a pursuer
        // walks through the mech and its centre passes through the mech's.
        PlanarVector target = PlanarVector.FromComponents(2.0, 2.0);
        EnemyState onTop = EnemyState.Spawn(Skitterling, target);

        EnemyState advanced = EnemyPursuit.Advance(onTop, Skitterling, target);

        Assert.That(
            advanced,
            Is.EqualTo(onTop),
            "a zero displacement has no direction, and normalizing it would produce whatever the "
                + "normalization happened to return - which is the defect the first slice found in "
                + "PlanarVector.Normalized and fixed");
    }

    [Test]
    public void PursuitIsNotSlowedOrRedirectedByAnotherBodyOnTheSamePath()
    {
        // docs/31:26 - ordinary enemies "do not collide with the mech, one another, mining points,
        // pickups, or other enemies". Two pursuers on the same bearing, one behind the other, both
        // advance the full step and stay exactly the distance apart they started.
        PlanarVector target = PlanarVector.Zero;
        EnemyState leader = EnemyState.Spawn(Skitterling, PlanarVector.FromComponents(-2.0, 0.0));
        EnemyState follower = EnemyState.Spawn(Skitterling, PlanarVector.FromComponents(-2.3, 0.0));
        double separationBefore = leader.Position.DistanceTo(follower.Position);

        for (int tick = 0; tick < 50; tick++)
        {
            leader = EnemyPursuit.Advance(leader, Skitterling, target);
            follower = EnemyPursuit.Advance(follower, Skitterling, target);
        }

        Assert.That(
            leader.Position.DistanceTo(follower.Position),
            Is.EqualTo(separationBefore).Within(1e-9),
            "neither body pushed, blocked, or slowed the other over fifty ticks. A separation rule would "
                + "have changed this distance, and no document states one for ordinary enemies");
    }

    [Test]
    public void APursuerOvershootsRatherThanSnappingToTheTarget()
    {
        // A body inside one tick's displacement of the target. Snapping would make its speed a function
        // of how close it already was, which is a rule docs/31 does not have.
        double step = EnemyPursuit.DisplacementPerTickMeters(Skitterling);
        PlanarVector target = PlanarVector.Zero;
        EnemyState nearly = EnemyState.Spawn(Skitterling, PlanarVector.FromComponents(-step / 4.0, 0.0));

        EnemyState advanced = EnemyPursuit.Advance(nearly, Skitterling, target);

        Assert.That(
            advanced.Position.X,
            Is.EqualTo(step * 0.75).Within(1e-12),
            "it travelled the whole step and is now east of the target, not sitting on it");
    }
}
