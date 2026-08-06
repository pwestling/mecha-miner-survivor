using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Combat;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.World;

/// <summary>
/// The whole loop through the shipping composition: spawn, pursue, shoot, die, drill, pay.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-ENC-001-006</c>, <c>VER-MIN-001-004</c>, <c>VER-COM-005-003</c>.
/// </para>
/// <para>
/// These drive <c>RunComposition</c> and its host rather than constructing a world, because what is
/// being asserted is that the phases are wired to each other in the arrangement that ships. A fixture
/// that assembled its own stores would prove the phases work when correctly wired and say nothing about
/// whether the shipping wiring is correct.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class GameplayLoopTests
{
    private static RunComposition Fresh(ulong seed)
    {
        return RunComposition.CreateGraybox(seed);
    }

    private static void Step(RunComposition run, PlanarVector intent, long sequence)
    {
        _ = run.CommandGate.TryAdmit(run.ComposeEnvelope(sequence, intent.X, intent.Y), out _);
        run.Host.Step(TickRate.SecondsPerTick);
        _ = run.SettleTerminalTransition();
    }

    [Test]
    public void PhaseThreeMaterializesTheMinuteZeroPulseAndHoldsThePopulationAtItsMinimum()
    {
        RunComposition run = Fresh(0x100B_0000_0001UL);

        Step(run, PlanarVector.Zero, 0);
        int afterFirstTick = run.World.LiveEnemyCount;

        // Long enough for the first entrants to cross the arena and be killed, so attrition has had a
        // chance to reopen room. A pursuer enters 19.76 m out at 1.26 m/s, so it needs about 940 ticks to
        // reach the mech's 8 m weapon range; 600 ticks was too few and this assertion was measured, not
        // guessed.
        for (long tick = 1; tick < 2400; tick++)
        {
            Step(run, PlanarVector.Zero, tick);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                afterFirstTick,
                Is.EqualTo(MinuteZeroBaseline.PulseBatchSize),
                "tick zero is a pulse tick and admitted the batch of two");
            Assert.That(
                run.World.LiveEnemyCount,
                Is.LessThanOrEqualTo(MinuteZeroBaseline.DesiredMinimumPopulation),
                "forty seconds later the population has not exceeded the desired minimum of eight, "
                    + "because docs/32:20 makes replenishment wait once the minimum is met");
            Assert.That(
                run.World.EnemiesMaterialized,
                Is.GreaterThan(MinuteZeroBaseline.DesiredMinimumPopulation),
                "and more than eight have been materialized in total, so the population is a loop with "
                    + "attrition rather than a crowd that filled once and stopped");
        });
    }

    [Test]
    public void EveryMaterializedEnemyEntersOnTheRingTheArenaAdmits()
    {
        RunComposition run = Fresh(0x100B_0000_0002UL);
        double expected = GrayboxSpawnRing.LargestFittingRadius(
            GrayboxArenaBounds.DefaultHalfExtentMeters,
            EnemyRoster.Skitterling.ContactRadiusMeters);

        // Only the tick a pulse lands on has enemies still at their entrance position, so this samples
        // immediately after the first pulse rather than after arbitrary drift.
        Step(run, PlanarVector.Zero, 0);

        List<double> radii = new();
        for (int index = 0; index < run.World.LiveEnemyCount; index++)
        {
            radii.Add(run.World.EnemyAt(index).Position.Magnitude);
        }

        Expect.Multiple(() =>
        {
            Assert.That(radii, Is.Not.Empty, "there is something to check");
            Assert.That(
                radii,
                Is.All.EqualTo(expected - EnemyPursuit.DisplacementPerTickMeters(EnemyRoster.Skitterling))
                    .Within(1e-9),
                "one tick's pursuit inside the ring, which is exactly the phase order doing its job: "
                    + "phase 3 materialized the body on the ring and phase 5 of the SAME tick integrated "
                    + "its first step. Asserting the bare ring radius here failed, and the 0.021 m "
                    + "discrepancy is one Skitterling tick");
            Assert.That(
                expected,
                Is.LessThan(24.5),
                "and it is INSIDE the camera's 24.5 m half-diagonal, which is why docs/32:23's "
                    + "'outside the active camera' cannot be honoured in a 40 m graybox arena. Recorded as "
                    + "a measured gap rather than implied");
        });
    }

    [Test]
    public void PursuersCloseOnTheMechOverTime()
    {
        RunComposition run = Fresh(0x100B_0000_0003UL);

        Step(run, PlanarVector.Zero, 0);
        double nearestAtStart = double.PositiveInfinity;
        for (int index = 0; index < run.World.LiveEnemyCount; index++)
        {
            nearestAtStart = Math.Min(nearestAtStart, run.World.EnemyAt(index).Position.Magnitude);
        }

        for (long tick = 1; tick < 300; tick++)
        {
            Step(run, PlanarVector.Zero, tick);
        }

        double nearestLater = double.PositiveInfinity;
        for (int index = 0; index < run.World.LiveEnemyCount; index++)
        {
            nearestLater = Math.Min(nearestLater, run.World.EnemyAt(index).Position.Magnitude);
        }

        Expect.Multiple(() =>
        {
            Assert.That(nearestAtStart, Is.GreaterThan(19.0), "they entered at the arena edge");
            Assert.That(
                nearestLater,
                Is.LessThan(nearestAtStart),
                "and five seconds later the nearest is closer, so pursuit is wired into phases 4 and 5 "
                    + "rather than only implemented");
        });
    }

    [Test]
    public void TheWeaponSelectsTheNearestEnemyInRangeAndKillsIt()
    {
        RunComposition run = Fresh(0x100B_0000_0004UL);

        for (long tick = 0; tick < 1800; tick++)
        {
            Step(run, PlanarVector.Zero, tick);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.ProjectilesFired,
                Is.GreaterThan(0L),
                "phase 7 spent activations, so a target was found within the 8 m range");
            Assert.That(
                run.World.EnemiesDestroyed,
                Is.GreaterThan(0L),
                "and phase 10 resolved deaths from phase 8's hits: two 12-damage projectiles exhaust a "
                    + "20-Hull Skitterling");
            Assert.That(
                run.World.EnemiesDestroyed * 2,
                Is.LessThanOrEqualTo(run.World.ProjectilesFired),
                "every kill cost at least two projectiles, which is the arithmetic of a 12-damage shot "
                    + "against 20 Hull. Fewer would mean a projectile damaged more than one body, which "
                    + "docs/71:30 forbids for a weapon with no repeat interval");
        });
    }

    [Test]
    public void ADeadEnemyIsRemovedThroughPhaseTwelveAndItsIdentityStopsResolving()
    {
        RunComposition run = Fresh(0x100B_0000_0005UL);

        // Run until a kill happens, capturing the live identity set on each tick so the tick a record
        // disappears can be identified.
        HashSet<EntityId> previousLive = new();
        HashSet<EntityId> vanished = new();
        long killsBefore = 0;

        for (long tick = 0; tick < 3600 && vanished.Count == 0; tick++)
        {
            Step(run, PlanarVector.Zero, tick);

            HashSet<EntityId> live = new();
            for (int index = 0; index < run.World.LiveEnemyCount; index++)
            {
                live.Add(SnapshotEnemyIdentity(run, index));
            }

            if (run.World.EnemiesDestroyed > killsBefore)
            {
                foreach (EntityId id in previousLive)
                {
                    if (!live.Contains(id))
                    {
                        vanished.Add(id);
                    }
                }

                killsBefore = run.World.EnemiesDestroyed;
            }

            previousLive = live;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                vanished,
                Is.Not.Empty,
                "at least one enemy record disappeared on the tick a kill was recorded");
            Assert.That(
                run.World.LiveEnemyCount,
                Is.LessThanOrEqualTo(MinuteZeroBaseline.DesiredMinimumPopulation),
                "and the population never grew past the row's minimum, so removals really happen rather "
                    + "than dead records accumulating");
            Assert.That(
                run.World.EnemiesDestroyed,
                Is.GreaterThan(0L));
            Assert.That(
                run.World.EnemiesRemovedInPhaseTwelve,
                Is.EqualTo(run.World.EnemiesDestroyed),
                "and every one of those deaths was APPLIED IN PHASE 12 rather than in the phase that "
                    + "decided it. This is the assertion that makes doc 10 § System phase ordering's "
                    + "deferral rule falsifiable: a negative control moving the removal into phase 10 ran "
                    + "green against every other assertion in this fixture, because within one tick a "
                    + "record removed in phase 10 and one removed in phase 12 are indistinguishable to "
                    + "every later phase");
        });
    }

    [Test]
    public void PhaseElevenPaysInstallmentsIntoTheRunLocalTotalAndTheHudShowsThem()
    {
        RunComposition run = Fresh(0x100B_0000_0006UL);
        PlanarVector seam = run.World.MiningSiteAt(0).Centre;

        long oreWhenFirstInstallmentPaid = -1;
        for (long tick = 0; tick < 1800; tick++)
        {
            PlanarVector toSeam = seam - run.World.Player.Position;
            PlanarVector intent = toSeam.Magnitude > 0.05 ? toSeam.Normalized() : PlanarVector.Zero;
            Step(run, intent, tick);

            if (oreWhenFirstInstallmentPaid < 0 && run.World.InstallmentsPaid == 1)
            {
                oreWhenFirstInstallmentPaid = run.World.RunLocalCommonOre;
            }
        }

        PresentationSnapshot published = run.Snapshots.Latest!;

        Expect.Multiple(() =>
        {
            Assert.That(
                oreWhenFirstInstallmentPaid,
                Is.EqualTo(StandardOreSeamProfile.OrePerInstallment),
                "the first installment paid exactly 10 ore");
            Assert.That(
                run.World.RunLocalCommonOre,
                Is.EqualTo(StandardOreSeamProfile.TotalOre),
                "and thirty seconds on the seam banked its whole 100-ore capacity and no more");
            Assert.That(
                run.World.MiningSiteAt(0).IsDepleted,
                Is.True,
                "the seam is spent");
            Assert.That(
                published.Hud.DisplayedCommonOre,
                Is.EqualTo(StandardOreSeamProfile.TotalOre),
                "and the HUD publishes the same total, so the number a player sees is the authoritative one");
        });
    }

    [Test]
    public void EveryVisibleEntityIsPublishedAndThePlayerIsNotOneOfThem()
    {
        RunComposition run = Fresh(0x100B_0000_0007UL);

        for (long tick = 0; tick < 240; tick++)
        {
            Step(run, PlanarVector.Zero, tick);
        }

        PresentationSnapshot published = run.Snapshots.Latest!;
        int sites = 0;
        int enemies = 0;
        int projectiles = 0;
        foreach (SnapshotEntity entity in published.VisibleEntities.ToArray())
        {
            switch (entity.Category)
            {
                case PopulationCategory.MiningSite: sites++; break;
                case PopulationCategory.OrdinaryEnemy: enemies++; break;
                case PopulationCategory.WeaponActor: projectiles++; break;
                default: Assert.Fail("unexpected category " + entity.Category); break;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(sites, Is.EqualTo(GrayboxRunLayout.MiningSiteCount));
            Assert.That(
                enemies,
                Is.EqualTo(run.World.LiveEnemyCount),
                "every live pursuer is in the snapshot, so presentation can draw it");
            Assert.That(
                projectiles,
                Is.EqualTo(run.World.LiveProjectileCount));
            Assert.That(
                published.PlayerPositionX,
                Is.EqualTo(run.World.Player.Position.X),
                "and the player is a first-class field rather than an entity entry");
        });
    }

    [Test]
    public void DamageDoesNotInterruptExtraction()
    {
        // docs/40:56 - "Taking ordinary contact or projectile damage does not interrupt extraction, reset
        // progress, or move the mech." Under the stress row the mech is being hit continuously while it
        // stands on a seam, so the seam must still pay its full capacity.
        RunComposition run = RunComposition.CreateGraybox(
            0x100B_0000_0008UL,
            HarnessStressRows.LethalSwarm);
        PlanarVector seam = run.World.MiningSiteAt(0).Centre;

        for (long tick = 0; tick < 3600 && !run.World.HasEnded; tick++)
        {
            PlanarVector toSeam = seam - run.World.Player.Position;
            PlanarVector intent = toSeam.Magnitude > 0.05 ? toSeam.Normalized() : PlanarVector.Zero;
            Step(run, intent, tick);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                run.World.ContactInstancesResolved,
                Is.GreaterThan(0L),
                "the mech really was being damaged while it drilled");
            Assert.That(
                run.World.InstallmentsPaid,
                Is.GreaterThan(0L),
                "and installments still completed. A damage path that moved the mech or reset progress "
                    + "would have shown as zero here");
            Assert.That(
                run.World.Player.Hull,
                Is.LessThan(PlayerBaseline.MaximumHull));
        });
    }

    private static EntityId SnapshotEnemyIdentity(RunComposition run, int ordinal)
    {
        // The published snapshot is where an enemy's identity is observable from outside the world, which
        // is the diagnostic surface doc 91 § Test project separation permits - as opposed to reflecting
        // into the store.
        int seen = 0;
        foreach (SnapshotEntity entity in run.Snapshots.Latest!.VisibleEntities.ToArray())
        {
            if (entity.Category != PopulationCategory.OrdinaryEnemy)
            {
                continue;
            }

            if (seen == ordinal)
            {
                return entity.Id;
            }

            seen++;
        }

        return EntityId.Unset;
    }
}
