---
doc_id: DEC-091
title: Name and Quantify Hyper Gold
status: accepted
authoritative: false
validation: playtest
---

# DEC-091 — Name and Quantify Hyper Gold

## Decision

The game's cross-run mining resource is named **Hyper Gold**. “Super resource” is no longer its working player-facing name.

Every standard map contains exactly three Hyper Gold sites at randomized locations. Each site:

- Requires 45 seconds of forward extraction to complete.
- Awards exactly 100 Hyper Gold, all at completion.
- Awards nothing for an incomplete attempt.
- Uses the established threat-beacon response at activation and at 25%, 50%, and 75% progress.

The three sites therefore contain 300 potential Hyper Gold. DEC-111 later adds 25 collectible Hyper Gold to each of four boss loot bursts, raising the full standard-map ceiling to 400. Collected Hyper Gold remains unsecured during the run. Successful timed mission extraction permanently banks it; death before extraction forfeits all Hyper Gold collected in that run.

## Status

Accepted as the initial name, map count, and payout baseline. DEC-092 later fixes both power upgrades and option unlocks as required purchase categories; DEC-120 and DEC-121 supply their initial purchases, prices, and rules. Hyper Gold appearance, audio identity, and final interface presentation remain open.

## Rationale

Three sites create multiple high-stakes exploration objectives without making the cross-run currency commonplace. Each site's 100-unit payout gives the reward a legible, substantial denomination and makes a full site-clear value of 300 easy to understand. Extracting all three requires at least 2:15 of uninterrupted beacon exposure before travel, combat, or retreats, so obtaining the complete site allocation remains an intentional risk investment. DEC-111 later adds a separate 100-unit boss-loot ceiling.

Using one explicit resource name prevents the generic “super resource” placeholder from implying multiple unresolved resource families. Hyper Gold can now receive consistent world, radar, inventory, results-screen, and metaprogression presentation.

## Consequences

- The radar's Hyper Gold category points toward the nearest incomplete one of the three sites and retargets after completion.
- The HUD must distinguish unsecured Hyper Gold carried during the run from permanently banked Hyper Gold.
- Completing one, two, or three sites yields 100, 200, or 300 unsecured Hyper Gold respectively; boss loot is additional under DEC-111.
- Hyper Gold has no established run-local crafting use.
- DEC-092 requires both permanent numerical PowerUps and permanent option unlocks. DEC-120 and DEC-121 fix their initial catalogs, prices, and ownership rules; later expansion and final interface presentation remain open.
- Map generation must randomize all three locations and prevent inaccessible or procedurally invalid placements.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timer, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md)
- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)

## Supersedes / superseded by

Replaces the “super resource” working name in [DEC-080](./DEC-080-twenty-second-geodes-forty-five-second-super-resources.md) with Hyper Gold and resolves its site-count and payout variables. It preserves the 45-second completion-only extraction, threat-beacon behavior, and survival-gated banking established by DEC-032, DEC-005, and DEC-080. [DEC-092](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md) later resolves the broad purchase categories, with [DEC-120](./DEC-120-accept-permanent-powerup-catalog.md) and [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) supplying their initial catalogs. [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) adds a fixed boss-loot source without altering site behavior.
