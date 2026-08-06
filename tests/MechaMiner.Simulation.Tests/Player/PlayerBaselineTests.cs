using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Player;

/// <summary>
/// The shared player baseline equals the accepted table.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-PLY-001-001</c>.
/// </para>
/// <para>
/// Source: <c>docs/72-player-survivability-and-damage-baseline.md</c> § Shared Player Baseline.
/// This restates the table's numbers deliberately. It is normally a defect for a test to repeat a
/// constant the production code declares, because the test then asserts only that the file has not
/// changed; here the numbers are authored content that this repository hardcodes because the typed
/// content layer does not exist on this ref, so the assertion is against the <em>document</em> and
/// the duplication is the point. When <c>DAT-006</c> lands, this fixture is repointed at the loaded
/// definition and the literals below are deleted with the constants.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class PlayerBaselineTests
{
    [Test]
    public void TheBaselineEqualsTheAcceptedTable()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                PlayerBaseline.MaximumHull,
                Is.EqualTo(100),
                "docs/72:34 \"| Maximum Hull Integrity | 100 |\"");
            Assert.That(
                PlayerBaseline.PassiveRecoveryHullPerSecond,
                Is.EqualTo(0.0),
                "docs/72:37 \"| Passive Recovery | 0 Hull/s |\"");
            Assert.That(
                PlayerBaseline.BaseMovementSpeedMetersPerSecond,
                Is.EqualTo(3.0),
                "docs/72:39 \"| Base movement speed | 3.0M/s |\"");
            Assert.That(
                PlayerBaseline.CollisionDiameterMeters,
                Is.EqualTo(1.0),
                "docs/72:40 \"| Mech collision diameter | 1.0M |\"");
        });
    }

    [Test]
    public void StartingHullIsTheCurrentMaximumRatherThanASecondLiteral()
    {
        Assert.That(
            PlayerBaseline.StartingHull,
            Is.EqualTo(PlayerBaseline.MaximumHull),
            "docs/72:35 \"| Starting Hull Integrity | Current maximum |\" states a relationship, not a "
                + "number. Two independent literals would let a later maximum change leave the starting "
                + "value behind");
    }

    [Test]
    public void TheCollisionRadiusIsHalfTheAuthoredDiameter()
    {
        Assert.That(
            PlayerBaseline.CollisionRadiusMeters,
            Is.EqualTo(PlayerBaseline.CollisionDiameterMeters / 2.0),
            "the document authors a diameter and every geometry primitive takes a radius, so the "
                + "division happens exactly once");
    }

    [Test]
    public void TheInitialFacingIsEast()
    {
        Assert.That(
            PlayerBaseline.InitialFacingRadians,
            Is.EqualTo(0.0),
            "docs/30:70 \"Before the first input, the mech faces east\", and east is zero radians under "
                + "the counterclockwise-from-east convention this assembly uses internally");
    }

    /// <summary>
    /// The <c>M</c> unit and the meter coincide numerically only because the diameter is 1.0.
    /// </summary>
    [Test]
    public void TheMechDiameterUnitAndTheMeterCoincideOnlyBecauseTheDiameterIsOne()
    {
        Assert.That(
            PlayerBaseline.BaseMovementSpeedMetersPerSecond,
            Is.EqualTo(3.0 * PlayerBaseline.CollisionDiameterMeters),
            "docs/72:44 defines M as one unmodified collision diameter and states \"One base-travel "
                + "second therefore equals 3.0M of shortest-path travel\". The speed constant is "
                + "already in meters per second and must not be multiplied by the diameter again");
    }

    [Test]
    public void ThePerTickDisplacementIsDerivedFromTheRateAndTheSpeed()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                PlayerMovement.BaseDisplacementPerTickMeters,
                Is.EqualTo(PlayerBaseline.BaseMovementSpeedMetersPerSecond * TickRate.SecondsPerTick),
                "the derivation is asserted rather than the product restated");
            Assert.That(
                PlayerMovement.BaseDisplacementPerTickMeters,
                Is.EqualTo(0.05).Within(1e-15),
                "3.0 m/s at 60 Hz is 0.05 m per tick");
            Assert.That(
                PlayerMovement.BaseDisplacementPerTickMeters * TickRate.TicksPerSecond,
                Is.EqualTo(3.0).Within(1e-12),
                "one second of ticks covers exactly the base speed");
        });
    }
}
