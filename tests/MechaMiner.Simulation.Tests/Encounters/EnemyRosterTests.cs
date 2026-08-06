using System;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Player;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Encounters;

/// <summary>
/// The <c>EN-01</c> Skitterling row, against the roster document and against the two derived figures
/// other documents state independently.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-ENC-001-001</c>.
/// </para>
/// <para>
/// The derived checks are the point. Asserting that <c>SkitterlingMovementShare</c> is 0.42 only
/// restates the constant; asserting that it produces the 1.26 m/s
/// <c>docs/72-player-survivability-and-damage-baseline.md</c>:65 tabulates, and that the body scale
/// produces the 0.44 m first data column of <c>docs/data/contact-damage-pressure.csv</c>:2, checks the
/// constants against two documents that were written from the same source and could disagree with it.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EnemyRosterTests
{
    private const double Tolerance = 1e-12;

    [Test]
    public void TheSkitterlingRowEqualsTheAuthoredRosterRow()
    {
        EnemyProfile skitterling = EnemyRoster.Skitterling;

        Expect.Multiple(() =>
        {
            Assert.That(skitterling.ContentId, Is.EqualTo("EN-01"), "docs/40 technical:67 reuses IDs exactly");
            Assert.That(skitterling.MaximumHull, Is.EqualTo(20), "docs/31:39 Hull column");
            Assert.That(skitterling.ContactDamage, Is.EqualTo(5), "docs/31:39 Contact column");
            Assert.That(skitterling.MovementShare, Is.EqualTo(0.42).Within(Tolerance), "docs/31:39, 42%");
            Assert.That(skitterling.BodyScale, Is.EqualTo(0.55).Within(Tolerance), "docs/31:39, 0.55x");
            Assert.That(skitterling.IsAuthored, Is.True);
        });
    }

    [Test]
    public void TheWorldSpeedIsTheProductDocumentSeventyTwoTabulates()
    {
        Assert.That(
            EnemyRoster.Skitterling.WorldSpeedMetersPerSecond,
            Is.EqualTo(1.26).Within(1e-12),
            "docs/72:65 states the product: 'Skitterling | 42% | 1.26M/s'. 42% of the 3.0 m/s baseline "
                + "of docs/72:39 is 1.26 m/s, so this checks two documents against each other rather "
                + "than restating one constant");
    }

    [Test]
    public void TheContactDiameterIsTheProductTheContactPressureDataStates()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                EnemyRoster.RipperContactDiameterMeters,
                Is.EqualTo(0.80).Within(Tolerance),
                "docs/31:35 - body scale multiplies 'the Ripper's 0.80M contact diameter'. M is one mech "
                    + "collision diameter, 1.0 m by docs/72:40 and :47");
            Assert.That(
                EnemyRoster.Skitterling.ContactRadiusMeters * 2.0,
                Is.EqualTo(0.44).Within(1e-12),
                "docs/data/contact-damage-pressure.csv:2 reads 'EN-01,Skitterling,ordinary,0.44,...', so "
                    + "0.55 x 0.80 m must be the 0.44 m that data file already states");
            Assert.That(
                EnemyRoster.Skitterling.ContactRadiusMeters,
                Is.LessThan(PlayerBaseline.CollisionRadiusMeters),
                "a Skitterling is smaller than the mech: docs/31:54 calls it 'low, tiny'");
        });
    }

    [Test]
    public void ADefaultedProfileIsNotAuthoredAndCannotSpawnAnEnemy()
    {
        EnemyProfile defaulted = default;

        Expect.Multiple(() =>
        {
            Assert.That(defaulted.IsAuthored, Is.False);
            Assert.That(
                Expect.Throws<ArgumentException>(
                    () => EnemyState.Spawn(defaulted, Simulation.Geometry.PlanarVector.Zero)).ParamName,
                Is.EqualTo("profile"),
                "a defaulted profile names no identity and carries no Hull, so an enemy built from one "
                    + "would be a body with zero Hull that phase 10 would call dead on the tick it "
                    + "materialized");
        });
    }

    [Test]
    public void ARosterRowRefusesANonPositiveMultiplier()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => EnemyProfile.Authored("EN-XX", 20, 5, 0.0, 0.55)).ParamName,
                Is.EqualTo("movementShare"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => EnemyProfile.Authored("EN-XX", 20, 5, 0.42, -1.0)).ParamName,
                Is.EqualTo("bodyScale"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => EnemyProfile.Authored("EN-XX", 0, 5, 0.42, 0.55)).ParamName,
                Is.EqualTo("maximumHull"));
        });
    }
}
