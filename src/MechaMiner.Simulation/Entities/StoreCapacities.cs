using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// The initial <see cref="StoreCapacity"/> of every authoritative population category,
/// each with its cited derivation and its <see cref="CapacityAuthority"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior:
/// "Initial capacities are derived from the encounter schedule plus a documented margin
/// in each specialized system rather than selected as arbitrary powers of two." One place
/// holds them so a reader comparing two rows sees both derivations side by side, and so a
/// document revision has one destination.
/// </para>
/// <para>
/// Every row states its <see cref="CapacityAuthority"/>, which is what makes a stale
/// figure findable. Three rows - enemy projectile, weapon actor, damage zone - are
/// <see cref="CapacityAuthority.CombatRuntimeCeiling"/> and therefore move whenever
/// <c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity moves.
/// The other nine do not depend on doc 22 at all.
/// <c>StoreCapacityTests</c> asserts exactly which rows those three are, so a doc 22
/// revision is answered by a test rather than by re-reading twelve comments.
/// </para>
/// <para>
/// Ordering never comes from a dictionary here.
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c>, doc 20 § Entity
/// identity, and doc 10 § System phase ordering all forbid observable order derived from
/// collection enumeration, so <see cref="Categories"/> is a fixed array in doc 20's table
/// order and the per-category lookup is an array indexed by the enum's value.
/// </para>
/// </remarks>
public static class StoreCapacities
{
    /// <summary>
    /// The lower bound the map validator may assert on the mining-site manifest count.
    /// </summary>
    /// <remarks>
    /// The fixed 20 standard and 8 rich seams
    /// (<c>docs/51-standard-map-generation-contract.md</c> § Common-ore seams), plus eight
    /// geodes for each of four present materials (§ Specialized-material geodes), plus the
    /// three Hyper Gold sites (§ Hyper Gold sites). The store is still sized exactly from
    /// the manifest; this is a validation bound, not a capacity.
    /// </remarks>
    public const int MiningSiteManifestLowerBound = 63;

    /// <summary>
    /// The upper bound the map validator may assert on the mining-site manifest count.
    /// </summary>
    /// <remarks>
    /// The same rows at their upper counts: 20 standard plus 8 rich seams, up to ten geodes
    /// per present material across four present materials, plus three Hyper Gold sites
    /// gives 71. Only a validation bound: the store is sized from the manifest, so a map
    /// with 63 sites gets a store of 63.
    /// </remarks>
    public const int MiningSiteManifestUpperBound = 71;

    private static readonly PopulationCategory[] CategoryOrder =
    [
        PopulationCategory.Player,
        PopulationCategory.OrdinaryEnemy,
        PopulationCategory.Elite,
        PopulationCategory.Boss,
        PopulationCategory.EnemyProjectile,
        PopulationCategory.WeaponActor,
        PopulationCategory.DamageZone,
        PopulationCategory.MiningSite,
        PopulationCategory.Pickup,
        PopulationCategory.DestructibleRock,
        PopulationCategory.RelicCache,
        PopulationCategory.StaticWorldObject,
    ];

    private static readonly ReadOnlyCollection<PopulationCategory> ReadOnlyCategoryOrder =
        new(CategoryOrder);

    /// <summary>
    /// The twelve categories in <c>docs/technical/20-simulation-core.md</c> § Authoritative
    /// population categories table order.
    /// </summary>
    /// <remarks>
    /// This is the canonical iteration order for anything that walks every store. It is a
    /// fixed sequence, not a set enumeration, precisely because doc 10 § System phase
    /// ordering requires "documented stable ordering rather than collection or thread
    /// timing".
    /// </remarks>
    public static IReadOnlyList<PopulationCategory> Categories => ReadOnlyCategoryOrder;

    /// <summary>
    /// The capacity of one category's store.
    /// </summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <param name="miningSiteManifestCount">
    /// The mining-site count the validated map manifest declares. Required even for other
    /// categories' lookups so the caller cannot construct a store set without it.
    /// </param>
    /// <param name="staticWorldObjectManifestCount">
    /// The static-world-object count the validated map manifest declares.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="category"/> is not one of the twelve, or a manifest count is
    /// negative.
    /// </exception>
    public static StoreCapacity For(
        PopulationCategory category,
        int miningSiteManifestCount,
        int staticWorldObjectManifestCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(miningSiteManifestCount);
        ArgumentOutOfRangeException.ThrowIfNegative(staticWorldObjectManifestCount);

        return category switch
        {
            PopulationCategory.Player => Player,
            PopulationCategory.OrdinaryEnemy => OrdinaryEnemy,
            PopulationCategory.Elite => Elite,
            PopulationCategory.Boss => Boss,
            PopulationCategory.EnemyProjectile => EnemyProjectile,
            PopulationCategory.WeaponActor => WeaponActor,
            PopulationCategory.DamageZone => DamageZone,
            PopulationCategory.MiningSite => MiningSites(miningSiteManifestCount),
            PopulationCategory.Pickup => Pickup,
            PopulationCategory.DestructibleRock => DestructibleRock,
            PopulationCategory.RelicCache => RelicCache,
            PopulationCategory.StaticWorldObject => StaticWorldObjects(staticWorldObjectManifestCount),
            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "not one of the twelve authoritative population categories of doc 20 "
                    + "§ Authoritative population categories; a thirteenth store is an "
                    + "unregistered category"),
        };
    }

    /// <summary>
    /// Player: exactly one, for the whole run.
    /// </summary>
    /// <remarks>
    /// <b>Authority: simulation invariant.</b> doc 20 § Scope and invariants: "exactly one
    /// player entity exists until terminal resolution". Not a margin candidate: a second
    /// resident player is the invariant failing, so headroom would hide the only bug this
    /// row can have.
    /// </remarks>
    public static StoreCapacity Player => StoreCapacity.WithoutMargin(
        1,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.SimulationInvariant,
        "doc 20 § Scope and invariants: exactly one player entity exists until terminal resolution");

    /// <summary>
    /// Ordinary enemy: soft target 700, margin 30, hard capacity 730.
    /// </summary>
    /// <remarks>
    /// <b>Authority: encounter schedule.</b> A doc 22 revision does not reach this row.
    /// The 450 baseline, 100 event-overflow, and 150 beacon ceilings are the three source
    /// tags of <c>docs/technical/23-encounter-director-and-enemy-runtime.md</c> §
    /// Population classes, whose response volumes come from
    /// <c>docs/32-standard-wave-and-beacon-schedule.md</c> § Hyper Gold threat-beacon
    /// response. All three tags are ordinary-enemy records distinguished by spawn tag - "an
    /// entity has one source tag even when elite" - so they share one store. The margin is
    /// the largest authored single pulse batch in the schedule, so a director accounting bug
    /// trips the invariant with that batch resident.
    /// </remarks>
    public static StoreCapacity OrdinaryEnemy => StoreCapacity.WithMargin(
        700,
        30,
        OverflowBehaviour.QueueAuthored,
        CapacityAuthority.EncounterSchedule,
        "the largest authored single pulse batch in doc 32 § Complete 35-minute schedule",
        "450 baseline + 100 event-overflow + 150 beacon ceilings "
            + "(doc 23 § Population classes, doc 32 § Hyper Gold threat-beacon response)");

    /// <summary>
    /// Elite: soft target 13, margin 2, hard capacity 15.
    /// </summary>
    /// <remarks>
    /// <b>Authority: encounter schedule.</b> Four authored elites in
    /// <c>docs/32-standard-wave-and-beacon-schedule.md</c> § Complete 35-minute schedule,
    /// plus three per Hyper Gold site - one at the 50% trigger and two at 75%
    /// (§ Hyper Gold threat-beacon response) - across the three sites
    /// (<c>docs/51-standard-map-generation-contract.md</c> § Hyper Gold sites) gives 13
    /// concurrent if none has died. The margin is one 75% trigger's elite addition.
    /// </remarks>
    public static StoreCapacity Elite => StoreCapacity.WithMargin(
        13,
        2,
        OverflowBehaviour.QueueAuthored,
        CapacityAuthority.EncounterSchedule,
        "one Hyper Gold 75% trigger's elite addition (doc 32 § Hyper Gold threat-beacon response)",
        "4 authored elites + 3 per Hyper Gold site x 3 sites "
            + "(doc 32 § Complete 35-minute schedule and § Hyper Gold threat-beacon "
            + "response, doc 51 § Hyper Gold sites)");

    /// <summary>
    /// Boss: exactly four, all four concurrently alive.
    /// </summary>
    /// <remarks>
    /// <b>Authority: encounter schedule.</b> Four scheduled bosses
    /// (<c>docs/32-standard-wave-and-beacon-schedule.md</c> § Complete 35-minute schedule;
    /// doc 20 § Authoritative random-number contract names "scheduled boss index 0-3"), all
    /// four able to be alive simultaneously - doc 23 § Population classes gives the boss
    /// tag a "separate maximum four". The authored set is closed, so it admits no
    /// defensible margin.
    /// </remarks>
    public static StoreCapacity Boss => StoreCapacity.WithoutMargin(
        4,
        OverflowBehaviour.QueueAuthored,
        CapacityAuthority.EncounterSchedule,
        "4 scheduled bosses, closed authored set (doc 32 § Complete 35-minute schedule; "
            + "doc 23 § Population classes, boss separate maximum four)");

    /// <summary>
    /// Enemy projectile: 512, taken verbatim from doc 22.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authority: doc 22 § Performance and capacity - this number moves when doc 22
    /// moves.</b> That section states "512 enemy projectiles" as a "provisional
    /// authoritative hard capacit[y] for stress validation ... intentionally above current
    /// gameplay maxima", and owns its own revision path: "Profiling and legal
    /// maximum-output analysis must tighten or expand them before content complete." It is
    /// therefore re-read, never re-derived here; re-deriving it would create the second
    /// source of truth doc 40 § Unit and numeric policy warns about.
    /// </para>
    /// <para>
    /// <b>Open question, filed to the encounter stream.</b> A schedule-derived worst case
    /// can exceed 512. A Needler fires every 4.5 s
    /// (<c>docs/31-standard-enemy-and-boss-roster.md</c>) with a lifetime carrying the
    /// projectile "slightly beyond one screen width", which is about 42.7 M at the 24 m
    /// camera (<c>docs/technical/30-presentation-and-rendering.md</c> § Camera), so about
    /// 19 s of flight and about 4.2 projectiles in the air per Needler. At the 45% peak
    /// authored Needler share of a population legally reaching the combined ordinary-enemy
    /// ceiling, that is well past 512, and Eidolon Coral resonance raises Needler cadence
    /// further. doc 22 § Performance and capacity already says legal maximum-output
    /// analysis must settle it, and it is not this package's to resolve.
    /// </para>
    /// <para>
    /// <b>Second qualification on any raise of this ceiling.</b> Composition shares govern
    /// <em>replenishment</em>, not the alive mix, so under high churn the alive Needler
    /// share can exceed its composition share and the replenishment-bounded worst case is
    /// not an absolute bound. Whatever figure doc 22 carries is safe against the
    /// replenishment-bounded case only.
    /// </para>
    /// </remarks>
    public static StoreCapacity EnemyProjectile => StoreCapacity.WithoutMargin(
        512,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.CombatRuntimeCeiling,
        "doc 22 § Performance and capacity: 512 enemy projectiles, a provisional "
            + "authoritative hard capacity for stress validation");

    /// <summary>
    /// Weapon actor: 2,048, taken verbatim from doc 22.
    /// </summary>
    /// <remarks>
    /// <b>Authority: doc 22 § Performance and capacity - this number moves when doc 22
    /// moves.</b> "2,048 player weapon projectiles/actors combined", with 128
    /// deployable/autonomous actors as a sub-bucket of it. Labelled provisional there,
    /// with the same revision path as every other ceiling in that section. doc 20 §
    /// Capacity and overload behavior requires that these never disappear for pool
    /// reasons: "Authoritative projectiles and persistent weapon actors may not disappear
    /// because a visual pool is full", which is why the overflow behaviour is a failed
    /// invariant rather than degradation.
    /// </remarks>
    public static StoreCapacity WeaponActor => StoreCapacity.WithoutMargin(
        2048,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.CombatRuntimeCeiling,
        "doc 22 § Performance and capacity: 2,048 player weapon projectiles/actors combined");

    /// <summary>
    /// Damage zone: 512, taken verbatim from doc 22.
    /// </summary>
    /// <remarks>
    /// <b>Authority: doc 22 § Performance and capacity - this number moves when doc 22
    /// moves.</b> "512 persistent damage zones/trail segments after behavior-specific
    /// compaction". The compaction clause matters: the ceiling counts compacted segments,
    /// so a store that stopped compacting would breach it, which is exactly the invariant
    /// failure doc 20 § Capacity and overload behavior wants surfaced.
    /// </remarks>
    public static StoreCapacity DamageZone => StoreCapacity.WithoutMargin(
        512,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.CombatRuntimeCeiling,
        "doc 22 § Performance and capacity: 512 persistent damage zones/trail segments "
            + "after behavior-specific compaction");

    /// <summary>
    /// Pickup: soft target 75, margin 12, hard capacity 87. <b>Weakly sourced.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authority: derived from gameplay rates, because no document bounds this
    /// population.</b> Neither the encounter schedule nor doc 22 states a pickup ceiling.
    /// Smallest defensible basis: 16 initial rocks plus one replenishment attempt per second
    /// at 10% success over 35 minutes is about 226 rocks destroyed
    /// (<c>docs/51-standard-map-generation-contract.md</c> § Destructible rocks); at the 20%
    /// health-pack chance stated in
    /// <c>docs/72-player-survivability-and-damage-baseline.md</c> § Health Packs and
    /// Destructible Rocks that is about 45 packs, and packs "persist until collected or run
    /// end". The binomial standard deviation is about 6, so mean plus five standard
    /// deviations is 75. The margin is the boss contribution: four bosses times the three
    /// loot groups of doc 23 § Boss death and physical loot.
    /// </para>
    /// <para>
    /// <b>Weakly sourced, and the reason must survive review.</b> Two document inputs are
    /// missing. First, how many pickup <em>entities</em> a boss death creates. doc 23 § Boss
    /// death and physical loot describes "300 common ore as contact-collected pickup pieces"
    /// and then says "If individual visual pieces are combined for performance, collection
    /// still grants exactly the manifest totals", deliberately leaving the entity count
    /// open. The margin here therefore <em>assumes</em> each of a boss's three loot groups
    /// materializes as one collected entity; if the 300 ore pieces are separate entities the
    /// margin is short by three orders of magnitude. Second, whether mining payouts
    /// materialize pickup entities at all or credit directly
    /// (<c>docs/40-mining-and-extraction.md</c> § Resource payout profiles). Both belong to
    /// the mining and encounter packages; this row is re-derived when either is fixed.
    /// </para>
    /// </remarks>
    public static StoreCapacity Pickup => StoreCapacity.WithMargin(
        75,
        12,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.DerivedFromGameplayRates,
        "4 bosses x 3 loot groups, assuming one collected entity per group "
            + "(doc 23 § Boss death and physical loot)",
        "16 initial rocks plus ~210 successful replenishments over 35 minutes gives ~226 "
            + "destroyed; at the 20% health-pack chance that is ~45 concurrent packs; mean "
            + "+ 5 binomial standard deviations = 75 (doc 51 § Destructible rocks, "
            + "doc 72 § Health Packs and Destructible Rocks)")
        .AsWeaklySourced(
            "assumes one collected pickup entity per boss loot group. doc 23 § Boss death "
                + "and physical loot states the manifest totals, calls the 300 common ore "
                + "'pickup pieces', and then permits combining them, so the pickup-entity "
                + "count is deliberately open. doc 40 § Resource payout profiles also does "
                + "not say whether a mining payout materializes a pickup entity at all. "
                + "Re-derive when either input is fixed.");

    /// <summary>
    /// Destructible rock: 16, the stated dynamic population cap.
    /// </summary>
    /// <remarks>
    /// <b>Authority: encounter schedule.</b>
    /// <c>docs/51-standard-map-generation-contract.md</c> § Destructible rocks states "a
    /// dynamic population capped at 16 active destructible rocks", and the replenishment
    /// loop enforces that cap, so no margin: a seventeenth rock is the loop failing.
    /// </remarks>
    public static StoreCapacity DestructibleRock => StoreCapacity.WithoutMargin(
        16,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.EncounterSchedule,
        "doc 51 § Destructible rocks: a dynamic population capped at 16 active destructible rocks");

    /// <summary>
    /// Relic cache: exactly three, fixed at generation.
    /// </summary>
    /// <remarks>
    /// <b>Authority: encounter schedule.</b>
    /// <c>docs/51-standard-map-generation-contract.md</c> § Relic caches: "The three relic
    /// caches". Fixed at generation and unable to grow, so no margin. Not
    /// <see cref="CapacityAuthority.MapManifest"/>: the <em>count</em> is authored at three
    /// rather than read from the manifest, so it does not move when a manifest does.
    /// </remarks>
    public static StoreCapacity RelicCache => StoreCapacity.WithoutMargin(
        3,
        OverflowBehaviour.FailInvariant,
        CapacityAuthority.EncounterSchedule,
        "doc 51 § Relic caches: the three relic caches, fixed at generation");

    /// <summary>
    /// Mining site: sized exactly from the validated map manifest.
    /// </summary>
    /// <param name="manifestCount">The mining-site count the manifest declares.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="manifestCount"/> is negative.</exception>
    /// <remarks>
    /// <b>Authority: map manifest.</b> Fixed at generation and unable to change during a
    /// run (<c>docs/51-standard-map-generation-contract.md</c>), so the store is sized to
    /// the manifest exactly and needs no margin.
    /// <see cref="MiningSiteManifestLowerBound"/> and
    /// <see cref="MiningSiteManifestUpperBound"/> are validation bounds on the manifest,
    /// not capacities.
    /// </remarks>
    public static StoreCapacity MiningSites(int manifestCount)
    {
        return StoreCapacity.FromManifest(
            manifestCount,
            "sized exactly from the validated map manifest; doc 51 § Common-ore seams, "
                + "§ Specialized-material geodes, and § Hyper Gold sites bound a conforming "
                + "manifest at 63 to 71 sites");
    }

    /// <summary>
    /// Static world object: sized exactly from the validated map manifest.
    /// <b>Weakly sourced.</b>
    /// </summary>
    /// <param name="manifestCount">The static-world-object count the manifest declares.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="manifestCount"/> is negative.</exception>
    /// <remarks>
    /// <b>Authority: map manifest.</b> Landmarks, authored structures, and obstacle stamps
    /// are all fixed at generation, so the store is sized to the manifest exactly.
    /// <b>Weakly sourced:</b> unlike mining sites, no document states a total.
    /// <c>docs/51-standard-map-generation-contract.md</c> § Landmarks, authored structures,
    /// and repetition gives landmarks per major region without a structure or
    /// obstacle-stamp total, so unlike the mining row there is no compile-time upper bound
    /// to assert and none is invented here. A conforming manifest is the only bound that
    /// exists.
    /// </remarks>
    public static StoreCapacity StaticWorldObjects(int manifestCount)
    {
        return StoreCapacity.FromManifest(
            manifestCount,
            "sized exactly from the validated map manifest: landmarks, authored structures, "
                + "and obstacle stamps, all fixed at generation "
                + "(doc 51 § Landmarks, authored structures, and repetition)")
            .AsWeaklySourced(
                "no document states a total. doc 51 § Landmarks, authored structures, and "
                    + "repetition gives landmarks per major region and no structure or "
                    + "obstacle-stamp count, so no compile-time upper bound is assertable and "
                    + "none is invented here, in contrast to the mining-site row's 63-to-71 "
                    + "validation bound.");
    }
}
