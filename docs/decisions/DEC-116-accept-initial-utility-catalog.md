---
doc_id: DEC-116
title: Accept the Initial Utility Catalog
status: accepted
authoritative: false
validation: catalog-review-and-playtest
---

# DEC-116 — Accept the Initial Utility Catalog

## Decision

Accept the twelve concepts, material assignments, behavior rules, numeric starting values, three-rank improvements, and six-item first-playable subset in the [Utility Catalog](../68-utility-catalog.md).

The proposed distribution is:

| Material | Utility 1 | Utility 2 |
| --- | --- | --- |
| Asterite | Harmonic Calibrator: weapon damage | Survey Aperture: exploration discovery radius |
| Barysteel | Reinforced Bulkhead: maximum Hull Integrity | Extraction Accelerator: faster mining |
| Cinderglass | Cycle Capacitor: weapon attack rate | Capacitor Screen: rechargeable one-hit negation |
| Driftmetal | Vector Thrusters: movement speed | Extraction Tether: larger mining zones |
| Eidolon Coral | Repair Swarm: passive Recovery | Priority Uplink: elite and boss damage |
| Flux Amber | Ore Catalyzer: mined common-ore bonus | Field Expander: weapon area |

## Status

Accepted as the initial utility-content and numeric playtest baseline. DEC-109's accepted recipe, slot, rank-count, and price structure remains unchanged.

## Rationale

Each material receives one immediately understandable broadly useful utility and one more situational utility. Across any four-material profile, the eight offered choices span several support roles without guaranteeing the same favorite trio. Effects are basic enough to remain utilities rather than relic-scale behavior changes, while the irreversible slot and 300-ore full rank investment preserve meaningful competition.

The six-item first-playable subset covers direct offense, mining speed, weapon tempo, mobility, recovery, and economy with one item per material. It tests the material-availability structure before the more specialized second item for each material is required.

The proposal originally used slower mining-progress decay, reactive movement after damage, and finite weapon-effect duration in these three positions. Owner review redirected mining support toward a modest forward-rate bonus and rejected a second movement utility. A weapon audit found that only Gravity Projector, Mine Layer, Sentry Pod, Wake Projector, and Missile Rack have clear base-state finite-duration behavior, with several additional weapons benefiting only through one branch. Persistence Matrix was therefore too frequently dead or redundant for an irreversible utility slot and was replaced by the broadly applicable but encounter-specific Priority Uplink.

## Consequences

- OQ-013 and OQ-014 no longer list utility concepts, assignments, and rank effects as open.
- The fabrication interface gains stable utility IDs, complete effect previews, and explicit affected-weapon disclosure.
- Weapon-support utilities require a mapping pass across all fifteen weapons and their branches.
- Map, mining, survivability, economy, and UI specs must adopt the catalog's interaction rules.
- Numeric values remain playtest baselines rather than permanent balance promises.
- DEC-121 later makes the six-item first-playable subset the fresh-profile utility set and bundles the other six as one permanent Hyper Gold unlock.

## Specification links

- [Utility Catalog](../68-utility-catalog.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [DEC-109 — Use single-material utilities with three ore ranks](./DEC-109-use-single-material-utilities-with-three-ore-ranks.md)
- [DEC-121 — Accept the initial option-unlock catalog](./DEC-121-accept-initial-option-unlock-catalog.md)

## Supersedes / superseded by

Completes the utility-content variables left open by DEC-018, DEC-035, DEC-100, and DEC-109 without changing the common-ore resource radar. [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) later fixes permanent availability for the six-item first-playable subset and its six alternates.
