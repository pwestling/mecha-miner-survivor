---
doc_id: DEC-047
title: Limit Weapons to Three Common-Ore Stats
status: accepted
authoritative: false
---

# DEC-047 — Limit Weapons to Three Common-Ore Stats

## Decision

Each weapon should expose no more than three common-ore-upgradeable stats. A smaller bundle is allowed when a weapon lacks three worthwhile independent upgrade axes. Exceeding three requires an explicit later exception rather than becoming ordinary catalog practice.

Rail Lance uses exactly three common-ore stats: damage, projectile width, and range. Its slow firing cadence and very fast projectile speed are fixed weapon properties rather than upgrade tracks.

## Status

Accepted default and accepted Rail Lance bundle.

## Context

Uncapped per-stat upgrades can create excessive fabrication-menu complexity when repeated across four equipped weapons. Some proposed bundles also included stats that fought the weapon fantasy or became irrelevant under a branch.

## Considered options

### Give every weapon four or five upgrade axes

This supports granular optimization but creates many repetitive purchase rows and makes branch stat inheritance harder to keep legible.

### Use at most three meaningful stats

This keeps each weapon's ore decisions readable and forces its fixed fantasy-defining properties to remain stable.

## Rationale

Three stats provide room for specialization without turning fabrication into a large stat spreadsheet. Fixed cadence or delivery properties can strengthen weapon identity when changing them would undermine the concept.

## Consequences

- Every catalog weapon needs zero to three meaningful common-ore stats.
- A property should not become upgradeable merely to fill all three slots.
- Each selected stat still has uncapped ranks, fixed linear gains, and nonlinear prices.
- Branches must preserve or visibly reinterpret all selected stats.
- Fixed weapon properties can still change through a major branch when that change is the branch's defining benefit.
- Rail Lance attack rate cannot be increased with common ore.
- Rail Lance projectile speed cannot be increased with common ore and is always very fast.
- Kinetic Capacitor charge cadence is controlled by fixed branch behavior and its movement bonus rather than an inherited attack-rate track.
- Exact per-rank increments and prices remain open.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Narrows the fixed weapon-stat bundles established by [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md) and the uncapped rank model in [DEC-025](./DEC-025-uncapped-linear-stat-ranks.md). It does not change rank growth or pricing structure.
