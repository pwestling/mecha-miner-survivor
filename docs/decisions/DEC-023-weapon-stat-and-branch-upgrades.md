---
doc_id: DEC-023
title: Use Per-Stat Ore Upgrades and Specialized-Resource Weapon Branches
status: accepted
authoritative: false
---

# DEC-023 — Use Per-Stat Ore Upgrades and Specialized-Resource Weapon Branches

## Decision

Once a weapon is equipped, each of its defined upgradeable stats can be improved separately with common basic ore. The player chooses the exact stat to purchase instead of buying a bundled weapon level.

Larger weapon-specific branches cost specialized ordinary resources. DEC-040 defines one branch that substantially amplifies the existing play pattern, one that changes function moderately, and one that substantially changes play style. Both stat upgrades and branches use deterministic, visible pricing rules and outcomes and modify the weapon within its existing slot.

## Status

Accepted.

## Context

The project wants more intentional weapon development than *Vampire Survivors*. Common ore needs a reliable universal use, while randomized specialized resources need a strong reason to shape each run's major weapon choices.

## Considered options

### Bundled weapon levels

This is simple and familiar but gives the player little control over how a weapon develops.

### Random upgrade rolls

This creates variation but conflicts with fixed fabrication rules and risks recreating randomized level-up progression.

### Independent stats plus specialized-resource branches

This supports precise common-ore spending and reserves rarer materials for transformations that meaningfully change play.

## Rationale

The two-layer structure gives both common and specialized resources clear run-local roles. Incremental ore purchases provide frequent power growth before bosses, while major branches connect weapon identity to exploration and randomized geology.

## Consequences

- Every weapon requires a fixed, explicit bundle of player-visible upgradeable stats appropriate to that weapon.
- Each stat rank needs a deterministic price derived from the weapon's shared upgrade depth and an exact fixed effect under DEC-084.
- Major branches need fixed specialized-resource recipes and clearly described amplification, functional-variant, or playstyle-conversion outcomes.
- Upgrades remain attached to the weapon's existing slot.
- Common-ore growth must keep every signature weapon viable across all resource profiles.
- Stat ranks are uncapped and use linear stat gains with nonlinear shared weapon-depth prices under DEC-025 and DEC-084.
- Major weapon branches are mutually exclusive under DEC-027.
- Every weapon has exactly three branches under DEC-040. DEC-044 removes rank prerequisites and ordinary respec. DEC-084 makes all stat purchases on one weapon share a price ladder, and DEC-085 fixes that ladder's curve. Follow-on upgrades, stat persistence through removal, and dismantling remain open in OQ-025.
- The single mech-wide relic established by DEC-028 is separate from this weapon-upgrade system.
- Utility-system upgrading is not defined by this decision.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)
- [DEC-025 — Use uncapped linear stat ranks with nonlinear prices](./DEC-025-uncapped-linear-stat-ranks.md)
- [DEC-027 — Make major weapon branches mutually exclusive](./DEC-027-mutually-exclusive-weapon-branches.md)
- [DEC-028 — Use one exploration-found mech relic](./DEC-028-one-exploration-found-mech-relic.md)
- [DEC-040 — Use a three-level weapon-branch transformation gradient](./DEC-040-three-branch-transformation-gradient.md)
- [DEC-044 — Use immediate permanent branch commitment](./DEC-044-immediate-permanent-branch-commitment.md)
- [DEC-084 — Price stat upgrades by total weapon upgrade depth](./DEC-084-price-stat-upgrades-by-weapon-depth.md)
- [DEC-085 — Use a triangular shared-depth price curve](./DEC-085-use-triangular-shared-depth-prices.md)

## Supersedes / superseded by

Narrows the weapon-upgrade structure left open by [DEC-002](./DEC-002-mining-replaces-xp-and-chests.md) and [DEC-008](./DEC-008-fixed-blueprints-randomized-resource-profiles.md). It does not define final tuning or weapon content. [DEC-040](./DEC-040-three-branch-transformation-gradient.md) later fixes the count at three and defines their transformation categories. [DEC-044](./DEC-044-immediate-permanent-branch-commitment.md) resolves eligibility, inherited stat tracks, and irreversible commitment. [DEC-084](./DEC-084-price-stat-upgrades-by-weapon-depth.md) establishes the shared weapon-level price ladder.
