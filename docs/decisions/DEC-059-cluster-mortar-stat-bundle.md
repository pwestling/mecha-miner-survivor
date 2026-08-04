---
doc_id: DEC-059
title: Give Cluster Mortar Damage Radius and Rate Stats
status: accepted
authoritative: false
---

# DEC-059 — Give Cluster Mortar Damage, Radius, and Rate Stats

## Decision

Cluster Mortar exposes exactly three common-ore-upgradeable stats:

- **Damage:** damage dealt by the shell's primary explosion and the basis for branch-derived damage.
- **Blast radius:** the area covered by the primary explosion and the basis for branch-derived areas.
- **Attack rate:** automatic mortar firing events per unit of time.

Targeting range and shell travel delay are fixed weapon properties rather than common-ore stats. Danger-Close Protocol ignores ordinary targeting range because it selects the mech's position, but it retains the fixed travel delay.

## Status

Accepted stat bundle and branch mappings; increments, prices, and numeric tuning open.

## Context

Cluster Mortar has several plausible upgrade axes under the three-stat limit. Its selected bundle must remain useful for its automatic density-targeted base form, cascading explosions, lingering area denial, and player-positioned Danger-Close conversion.

## Considered options

### Upgrade targeting range or reduce travel delay

These improve convenience, but targeting range becomes irrelevant under Danger-Close Protocol and reducing delay can erase the prediction or baiting window that defines the weapon.

### Upgrade damage, blast radius, and attack rate

These strengthen every form while preserving committed delayed impact as a fixed identity.

## Rationale

Damage, radius, and rate provide clear potency, coverage, and frequency choices. None removes the possibility of missing a moving concentration, and all three carry visibly into each branch.

## Consequences

- Saturation Cascade derives secondary-blast damage and radius from the upgraded primary values using fixed branch multipliers.
- Interdiction Payload derives its field damage and footprint from the upgraded shell values; its slow and duration remain fixed branch behaviors.
- Danger-Close Protocol applies fixed branch multipliers to damage and radius while inheriting attack rate.
- Targeting range and travel delay cannot be improved with common ore.
- Exact linear per-rank gains, units, combat-value rounding, and branch multipliers remain open; DEC-085 fixes the shared weapon-depth price curve.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Resolves Cluster Mortar's candidate stat bundle under [DEC-047](./DEC-047-three-stat-weapon-bundles.md). [DEC-060](./DEC-060-balance-native-branch-funding.md) later sets its native branch funding colors. Numeric tuning remains open.
