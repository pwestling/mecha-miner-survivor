using System;
using MechaMiner.Simulation.Combat;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Combat;

/// <summary>
/// A projectile's flight, and the swept hit test that keeps it from tunnelling or missing the muzzle.
/// </summary>
/// <remarks>Verification: <c>VER-COM-005-004</c>.</remarks>
[TestFixture]
internal sealed class ProjectileStateTests
{
    /// <summary>
    /// <c>W-AB</c> Rail Lance's projectile speed in gameplay meters per second: <c>30</c>.
    /// </summary>
    /// <remarks>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:76, Fixed properties column - "3.0 s cadence,
    /// 30M/s projectile, pierces four targets". Named here, in a test, because this slice does not
    /// implement Rail Lance and putting the number in production code would be authoring content for a
    /// weapon nothing runs. It is used only to show that the current tunnelling margin is a ratio rather
    /// than a guarantee.
    /// </remarks>
    private const double RailLanceProjectileSpeed = 30.0;

    [Test]
    public void AProjectileTravelsTheBearingItWasFiredAlongAndDoesNotHome()
    {
        ProjectileState projectile = ProjectileState.Launch(
            PlanarVector.Zero,
            PlanarVector.FromComponents(1.0, 1.0));
        PlanarVector launchDirection = projectile.Direction;

        for (int tick = 0; tick < 10; tick++)
        {
            projectile = projectile.Advanced();
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                projectile.Direction,
                Is.EqualTo(launchDirection),
                "docs/66:94 - 'Projectiles continue on their fired trajectory rather than homing after "
                    + "launch'. There is no target reference to home toward and no setter to change the "
                    + "bearing");
            Assert.That(
                projectile.Position.Magnitude,
                Is.EqualTo(10.0 * PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters).Within(1e-9),
                "and its speed did not scale with how far away the target was, because Launch normalizes "
                    + "the displacement it is handed");
        });
    }

    [Test]
    public void AProjectileExpiresAfterTravellingItsWeaponsRange()
    {
        ProjectileState projectile = ProjectileState.Launch(PlanarVector.Zero, PlanarVector.East);

        for (int tick = 0; tick < PulseRepeaterBaseline.ProjectileLifetimeTicks - 1; tick++)
        {
            projectile = projectile.Advanced();
        }

        Expect.Multiple(() =>
        {
            Assert.That(projectile.IsSpent, Is.False, "one tick short of the range it is still in flight");
            Assert.That(
                projectile.Advanced().IsSpent,
                Is.True,
                "and on the thirtieth it is spent, having travelled 8 m");
            Assert.That(
                projectile.Advanced().Position.X,
                Is.EqualTo(PulseRepeaterBaseline.TargetingRangeMeters).Within(1e-9));
        });
    }

    [Test]
    public void AZeroBearingIsRefusedRatherThanProducingAStationaryProjectile()
    {
        Assert.That(
            Expect.Throws<ArgumentException>(
                () => ProjectileState.Launch(PlanarVector.Zero, PlanarVector.Zero)).ParamName,
            Is.EqualTo("direction"),
            "a target exactly at the muzzle has no bearing, and a projectile with a zero direction would "
                + "sit at the mech for thirty ticks and then expire");
    }

    [Test]
    public void TheSweptTestHitsABodySmallEnoughToFitInsideOneTicksStep()
    {
        // The tunnelling case, against a body small enough to demonstrate it. It is NOT EN-01: measured,
        // a Pulse Repeater step is 0.267 m against a 0.44 m Skitterling, so that body is 1.65 times the
        // step and cannot currently be tunnelled. The ratio is asserted separately below rather than
        // assumed either way.
        double step = PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters;
        PlanarVector from = PlanarVector.Zero;
        PlanarVector to = PlanarVector.FromComponents(step, 0.0);
        PlanarCircle small = PlanarCircle.FromCentreAndRadius(
            PlanarVector.FromComponents(step / 2.0, 0.0),
            step / 4.0);

        Expect.Multiple(() =>
        {
            Assert.That(small.Contains(from), Is.False, "a point test at the start of the step misses");
            Assert.That(small.Contains(to), Is.False, "and so does one at the end");
            Assert.That(
                small.OverlapsSegment(from, to),
                Is.True,
                "but the swept segment hits it. A point test would have passed a projectile straight "
                    + "through a body it should have destroyed");
        });
    }

    [Test]
    public void TheCurrentTunnellingMarginAgainstTheBaselineEnemyIsAMeasuredRatioAndNotAGuarantee()
    {
        double step = PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters;
        double diameter = EnemyRoster.Skitterling.ContactRadiusMeters * 2.0;

        Expect.Multiple(() =>
        {
            Assert.That(
                diameter / step,
                Is.EqualTo(1.65).Within(0.005),
                "0.44 m of body against a 0.267 m step. A point test would NOT currently tunnel through "
                    + "EN-01, and saying otherwise would be a claim this arithmetic contradicts");
            double railLanceStep = RailLanceProjectileSpeed
                / MechaMiner.Simulation.Time.TickRate.TicksPerSecond;
            Assert.That(
                railLanceStep,
                Is.GreaterThan(diameter),
                "the margin is a ratio rather than a guarantee: docs/71:76 already specifies a 30 m/s "
                    + "Rail Lance projectile, whose 0.5 m step exceeds a Skitterling's whole diameter, so "
                    + "the next weapon to land would consume it");
        });
    }

    [Test]
    public void TheSweptTestHitsABodyOverlappingTheMuzzle()
    {
        // The case that makes the run unwinnable if a point test is used: docs/31:26 makes enemies
        // non-solid, so a pursuer's centre reaches the mech's, and the first projectile position after
        // launch is already 0.267 m away - outside a 0.22 m footprint centred on the mech.
        double step = PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters;
        PlanarVector muzzle = PlanarVector.Zero;
        PlanarVector afterOneTick = PlanarVector.FromComponents(step, 0.0);
        PlanarCircle onTopOfTheMech = PlanarCircle.FromCentreAndRadius(
            PlanarVector.Zero,
            EnemyRoster.Skitterling.ContactRadiusMeters);

        Expect.Multiple(() =>
        {
            Assert.That(
                onTopOfTheMech.Contains(afterOneTick),
                Is.False,
                "the premise: one tick out is beyond a Skitterling standing on the mech");
            Assert.That(
                onTopOfTheMech.OverlapsSegment(muzzle, afterOneTick),
                Is.True,
                "the swept segment starts at the muzzle, so the body is hit. Without this the mech's own "
                    + "weapon could never touch a pursuer that had reached it, and the run would be "
                    + "unwinnable rather than merely mis-hit");
        });
    }
}
