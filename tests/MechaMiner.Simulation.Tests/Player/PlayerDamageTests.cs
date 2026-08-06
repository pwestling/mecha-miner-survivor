using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Player;

/// <summary>
/// Hull damage: what it changes, what it must not change, and that nothing gives it back.
/// </summary>
/// <remarks>Verification: <c>VER-PLY-001-011</c>, <c>VER-PLY-001-012</c>.</remarks>
[TestFixture]
internal sealed class PlayerDamageTests
{
    private static PlayerState Deployed => PlayerState.Create(
        PlanarVector.FromComponents(3.0, -2.0),
        facingRadians: 1.25,
        hull: PlayerBaseline.MaximumHull);

    [Test]
    public void DamageSubtractsFromHullAndChangesNothingElse()
    {
        PlayerState before = Deployed;
        PlayerState after = before.Damaged(5);

        Expect.Multiple(() =>
        {
            Assert.That(after.Hull, Is.EqualTo(95));
            Assert.That(
                after.Position,
                Is.EqualTo(before.Position),
                "docs/31:29 - 'Damage does not make the mech flinch, move, stop mining, or lose control.' "
                    + "This method has no position parameter, so the rule is a property of the type");
            Assert.That(
                after.FacingRadians,
                Is.EqualTo(before.FacingRadians),
                "and docs/72:44 gives the mech no post-hit invulnerability or knockback either");
            Assert.That(
                after.Footprint,
                Is.EqualTo(before.Footprint),
                "so a mining zone's inclusive occupancy test gives the same answer on the tick damage "
                    + "lands - docs/40:56, 'does not interrupt extraction, reset progress, or move the mech'");
        });
    }

    [Test]
    public void HullFloorsAtZeroAndOverkillIsDiscarded()
    {
        PlayerState nearlyDead = PlayerState.Create(PlanarVector.Zero, 0.0, 3);

        PlayerState overkilled = nearlyDead.Damaged(1000);

        Expect.Multiple(() =>
        {
            Assert.That(overkilled.Hull, Is.EqualTo(0), "doc 20 § Numeric and unit conventions gives "
                + "durability a validated nonnegative domain");
            Assert.That(
                overkilled.IsDestroyed,
                Is.True,
                "and IsDestroyed tests for exactly zero, which is what phase 13 reads");
            Assert.That(
                overkilled.Damaged(5).Hull,
                Is.EqualTo(0),
                "further damage cannot drive it below zero, so two bodies that died to different overkills "
                    + "are not distinguishable by a number no rule reads");
        });
    }

    [Test]
    public void TwentyContactInstancesFromTheBaselineEnemyExhaustAFullHull()
    {
        // docs/31:39 gives EN-01 a contact damage of 5 and docs/72:34 the mech 100 Hull, so twenty
        // instances is the exact budget. Driven as a sequence rather than asserted as a quotient, because
        // a flooring or an off-by-one in the subtraction would not show in the quotient.
        PlayerState state = Deployed;
        int instances = 0;

        while (!state.IsDestroyed)
        {
            state = state.Damaged(5);
            instances++;
            Assert.That(instances, Is.LessThanOrEqualTo(100), "the loop must terminate");
        }

        Assert.That(instances, Is.EqualTo(20), "100 Hull at 5 damage per instance");
    }

    [Test]
    public void DamageAccumulatesMonotonicallyBecauseNothingRecoversHull()
    {
        // docs/72:37 Passive Recovery is 0 Hull/s and docs/72:38 revival charges are 0, so the only thing
        // that could return Hull is a health pack, which this slice has none of.
        PlayerState state = Deployed;
        int previous = state.Hull;

        for (int instance = 0; instance < 10; instance++)
        {
            state = state.Damaged(5);
            Assert.That(state.Hull, Is.LessThan(previous), "Hull only ever falls");
            previous = state.Hull;
        }

        Expect.Multiple(() =>
        {
            Assert.That(state.Hull, Is.EqualTo(50));
            Assert.That(
                PlayerBaseline.PassiveRecoveryHullPerSecond,
                Is.EqualTo(0.0),
                "docs/72:37 - 'Passive Recovery | 0 Hull/s'. There is no elapsed-time path that could add "
                    + "any back: PlayerState takes no duration anywhere");
        });
    }

    [Test]
    public void NonPositiveDamageIsRefusedRatherThanTreatedAsAHeal()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => Deployed.Damaged(0)).ParamName,
                Is.EqualTo("damage"),
                "a zero-damage instance would consume the 0.20 s global grace of docs/72:43 without "
                    + "dealing anything, which would make a harmless contact suppress a real one");
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => Deployed.Damaged(-25)).ParamName,
                Is.EqualTo("damage"),
                "and negative damage would be a repair through the damage path, bypassing every rule "
                    + "docs/72 § Health Packs and Destructible Rocks puts on one");
        });
    }
}
