---
doc_id: DEC-028
title: Use One Exploration-Found Mech Relic
status: accepted
authoritative: false
---

# DEC-028 — Use One Exploration-Found Mech Relic

> **Completion note:** DEC-127 fixes the three-cache selection, signaling, guarding, sale value, compatibility preview, and resolution presentation left open here.

## Decision

The mech has one run-local relic slot. Relics are found through map exploration rather than fabricated. Each discovery can be installed or sold for resources; installing a later relic can replace the currently installed relic.

Relics operate at the mech or whole-build level and should change gameplay significantly through unusual rules, geometry, constraints, or tradeoffs rather than primarily providing obvious unconditional stat increases.

## Status

Accepted.

## Context

The game needs exploration rewards beyond materials for an already planned recipe. A separate relic system can introduce surprising run adaptation without adding more per-weapon upgrade slots or randomizing the fabrication catalog.

## Considered options

### One relic slot per weapon

This permits many simultaneous modifiers but creates excessive loadout complexity across four weapons and weakens the importance of each find.

### One mech-wide relic slot

This gives a relic room to change the whole build and makes every later discovery a significant replacement or sale decision.

### Craftable relics

This keeps acquisition deterministic but makes relics another recipe target rather than an independent reward for exploration.

## Rationale

A single mech-wide slot keeps the system legible and lets individual relics be dramatic. Map discovery strengthens the exploration loop, while the option to sell prevents an incompatible or undesirable relic from becoming a worthless find.

## Consequences

- The relic slot is separate from four weapon slots, three utility slots, weapon branches, weapon stats, and the mech's inherent trait.
- Relic locations vary with the generated map and are not disclosed by the geological resource survey.
- A discovered relic needs a complete effect preview and sale value before the player commits.
- Installing a new relic stops the old relic's active effect.
- Selling the new relic retains the currently installed relic.
- DEC-029 establishes a full-simulation pause, common-ore sale payout, and automatic sale of a displaced relic.
- DEC-030 later fixes three randomized automatic caches per standard map, and DEC-029 requires immediate resolution with no deferral. Exact sale values, guarding, selection, duplicate rules, and presentation remain open in OQ-027.
- Relic content must be evaluated for significant behavioral impact, build adaptation, and non-obvious tradeoffs.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Mech Relics](../67-mech-relics.md)
- [OQ-027 — How are mech relic discoveries presented and resolved?](../open-questions.md#oq-027--how-are-mech-relic-discoveries-presented-and-resolved)
- [DEC-020 — Keep ordinary crafting materials exclusive to mining](./DEC-020-mining-exclusive-ordinary-materials.md)
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./DEC-029-pause-and-resolve-relic-discoveries.md)
- [DEC-030 — Place three automatic relic caches on each standard map](./DEC-030-three-automatic-relic-caches.md)

## Supersedes / superseded by

Resolves the module-slot proposal recorded in OQ-026 in favor of a single mech-wide relic. It rejects separate per-weapon relic slots. [DEC-029](./DEC-029-pause-and-resolve-relic-discoveries.md) later defines the immediate paused install-or-sell resolution, and [DEC-030](./DEC-030-three-automatic-relic-caches.md) fixes the standard map count and contact behavior.
