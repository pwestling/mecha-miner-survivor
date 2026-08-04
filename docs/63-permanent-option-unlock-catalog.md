---
doc_id: GDD-PERMANENT-OPTION-UNLOCK-CATALOG
title: Permanent Option-Unlock Catalog
status: active
authoritative: true
---

# Permanent Option-Unlock Catalog

## Catalog status

This document defines the accepted fresh-profile content baseline and the six initial permanent option purchases. [DEC-121](./decisions/DEC-121-accept-initial-option-unlock-catalog.md) accepts the catalog for progression and content-availability testing. The initial prices and grouping are fixed baselines subject to a later whole-economy balance pass.

## Purpose

Option unlocks widen the player's future choices rather than directly adding account statistics. They share banked Hyper Gold with [Permanent PowerUps](./62-permanent-powerup-catalog.md), creating a persistent choice between immediate numerical strength and a broader utility or relic pool.

An option unlock grants account access only. It never grants an item inside the current run, supplies crafting resources, bypasses randomized geology, fills a slot, guarantees a cache result, or relaxes an item's ordinary acquisition rules.

## Fresh-profile baseline

A new profile begins with enough breadth to test the game's central build system without first grinding out graph-critical content.

| Category | Available immediately |
| --- | --- |
| Mechs | All six initial mechs: Kestrel, Pike, Prospector, Lodestar, Bastion, and Razorback |
| Weapons | All 15 base-weapon blueprints; signature weapons come from this same set |
| Universal utility | Resource radar |
| Material utilities | Harmonic Calibrator, Extraction Accelerator, Cycle Capacitor, Vector Thrusters, Repair Swarm, and Ore Catalyzer |
| Relics | Retrograde Engine, Colossus Governor, Event-Horizon Coupler, Fission Seed, and Claim-Jumper Core |
| Play | Standard map and standard mode |

All 15 initial weapons are available because locking individual edges of the six-material recipe graph could make a valid four-material profile arbitrarily narrow or misleading. Their presence in the blueprint catalog does not guarantee their recipe materials in a particular run.

## Shared purchase rules

- Option unlocks are purchased only between runs with banked Hyper Gold.
- All six initial purchases are visible and purchasable on a fresh profile. There is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
- Prices are fixed and do not rise with the number of purchases owned.
- Purchases are permanent and nonrefundable. They cannot be sold, disabled, or removed from the profile.
- Option unlocks cannot be purchased during an active run.
- **Refund PowerUps** affects only numerical PowerUp ranks and never returns Hyper Gold spent on option unlocks.
- The purchase interface distinguishes these permanent, nonrefundable unlocks from refundable PowerUps before confirmation.
- Unlocking a utility makes its blueprint permanently available whenever its required material occurs. Unlocking a relic permanently adds it to the random relic-cache selection pool.

## Catalog overview

| ID | Unlock | Category | Effect | Cost |
| --- | --- | --- | --- | ---: |
| `UNL-01` | Advanced Utility Suite | Utilities | Adds the six alternate material utilities | 600 |
| `UNL-02` | Ghostline Chassis | Relic | Adds Ghostline Chassis to relic caches | 250 |
| `UNL-03` | Dead-Reckoning Array | Relic | Adds Dead-Reckoning Array to relic caches | 250 |
| `UNL-04` | War-Drum Oscillator | Relic | Adds War-Drum Oscillator to relic caches | 300 |
| `UNL-05` | Redline Crucible | Relic | Adds Redline Crucible to relic caches | 350 |
| `UNL-06` | Sequential Reactor | Relic | Adds Sequential Reactor to relic caches | 400 |

The complete initial option catalog costs **2,150 Hyper Gold**. A perfect standard run can bank at most 400, so purchasing all six requires at least six successful perfect-collection runs and ordinarily more. Either 250-Hyper-Gold relic unlock can be purchased after one successful run that banks at least that amount.

## UNL-01 — Advanced Utility Suite

The fresh profile begins with one basic material utility per specialized resource:

| Material | Fresh-profile utility |
| --- | --- |
| Asterite | Harmonic Calibrator |
| Barysteel | Extraction Accelerator |
| Cinderglass | Cycle Capacitor |
| Driftmetal | Vector Thrusters |
| Eidolon Coral | Repair Swarm |
| Flux Amber | Ore Catalyzer |

Buying the suite simultaneously and permanently unlocks the alternate utility for all six materials:

| Material | Utility added by the suite |
| --- | --- |
| Asterite | Survey Aperture |
| Barysteel | Reinforced Bulkhead |
| Cinderglass | Capacitor Screen |
| Driftmetal | Extraction Tether |
| Eidolon Coral | Priority Uplink |
| Flux Amber | Field Expander |

The six are bundled so unlocking does not temporarily bias the material system toward one repeatedly favored resource. Before the purchase, any four-material profile offers four non-radar utilities plus the resource radar. After the purchase, the same profile offers eight non-radar utilities plus the radar.

The purchase grants blueprints, not fabricated equipment. Every unlocked utility still costs one unit of its assigned present material, occupies one of three utility slots, obeys the irreversible run-local commitment rule, and uses the three ore ranks specified in the [Utility Catalog](./68-utility-catalog.md).

## UNL-02 — Ghostline Chassis

Permanently adds **Ghostline Chassis** to the random relic-cache selection pool. Finding it remains unguaranteed, and installing or selling it uses the ordinary relic discovery rules.

## UNL-03 — Dead-Reckoning Array

Permanently adds **Dead-Reckoning Array** to the random relic-cache selection pool. Finding it remains unguaranteed, and installing or selling it uses the ordinary relic discovery rules.

## UNL-04 — War-Drum Oscillator

Permanently adds **War-Drum Oscillator** to the random relic-cache selection pool. Finding it remains unguaranteed, and installing or selling it uses the ordinary relic discovery rules.

## UNL-05 — Redline Crucible

Permanently adds **Redline Crucible** to the random relic-cache selection pool. Finding it remains unguaranteed, and installing or selling it uses the ordinary relic discovery rules.

## UNL-06 — Sequential Reactor

Permanently adds **Sequential Reactor** to the random relic-cache selection pool. Finding it remains unguaranteed, and installing or selling it uses the ordinary relic discovery rules.

## Relic-pool behavior

The fresh relic pool contains Retrograde Engine, Colossus Governor, Event-Horizon Coupler, Fission Seed, and Claim-Jumper Core. Each purchased relic unlock permanently joins that pool; the player cannot disable an owned relic to narrow future random results. Every standard run draws three distinct relics without replacement from the current pool for its three caches, so owning all ten expands possibility without making any one effect reliable.

An unlocked relic retains its full normal run-local rules: it must be found, its blocking choice must be resolved immediately, it occupies the single mech relic slot when installed, it may be sold for 150 common ore, and replacing an installed relic sells the displaced relic for 150. Ownership does not grant a starting relic.

## Relationship to permanent PowerUps

The option catalog costs 2,150 Hyper Gold and the initial numerical PowerUp catalog costs 9,450, for **11,600 Hyper Gold** across the two accepted initial spending catalogs. The two categories intentionally compete for the same survival-secured currency:

- PowerUps improve every eligible future deployment and are freely refundable and voluntarily rank-adjustable between runs.
- Option unlocks expand future availability and are permanent, nonrefundable, and not disableable.

Unlock prices do not promise equivalent direct power. A relic purchase adds variance to three random cache results; it does not guarantee that relic or improve the player's starting numbers.

## Interface requirements

- The hangar shows every initial purchase, its exact cost, owned state, effect, and nonrefundable status from a fresh profile.
- The Advanced Utility Suite preview lists all six blueprints it adds and explains that ordinary material, slot, and ore-rank costs still apply.
- A relic unlock preview shows the relic's one-sentence discovery concept and states that the purchase adds it to a random pool rather than equipping it.
- The interface shows banked Hyper Gold and separates refundable PowerUp investment from permanently spent option-unlock currency.
- Confirmation is required for every option purchase because no refund exists.

## Expansion boundaries

Later mechs, maps, modes, relics, cosmetics, and other breadth unlocks may extend this catalog through separate accepted decisions. They must state their fresh-profile visibility, cost, prerequisite if any, acquisition consequences, and refund behavior.

New weapons require special care: an isolated locked weapon can distort the complete pair-recipe graph and make some four-material profiles less useful. A later weapon expansion should preserve balanced geology-driven availability, preferably by adding a deliberately designed complete graph-compatible set rather than casually gating one existing edge.

Core onboarding, accessibility features, necessary settings, and baseline quality-of-life information are not progression rewards. They remain available without a Hyper Gold purchase.

## Validation questions

- Does a fresh profile have meaningful choices without an unlock grind?
- Does the 600-Hyper-Gold utility bundle compete credibly with early PowerUp ranks?
- Do added relics increase run variety without making the cache pool feel diluted or punitive?
- Do players understand that relic purchases add random possibilities rather than guaranteed run power?
- Does permanent, non-disableable pool growth create regret after players learn which relics they prefer?
- Does the combined 11,600-Hyper-Gold initial economy produce a satisfying progression horizon at observed extraction and collection rates?

## Related documents

- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Initial Relic Catalog](./69-initial-relic-catalog.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md)
- [Initial Mech Catalog](./36-initial-mech-catalog.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [DEC-092 — Use Hyper Gold for power and option unlocks](./decisions/DEC-092-use-hyper-gold-for-power-and-option-unlocks.md)
- [DEC-094 — Allow free PowerUp refunds](./decisions/DEC-094-allow-free-powerup-refunds.md)
- [DEC-120 — Accept the permanent PowerUp catalog](./decisions/DEC-120-accept-permanent-powerup-catalog.md)
- [DEC-121 — Accept the initial option-unlock catalog](./decisions/DEC-121-accept-initial-option-unlock-catalog.md)
