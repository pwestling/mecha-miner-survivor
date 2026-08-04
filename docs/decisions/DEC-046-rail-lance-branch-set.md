---
doc_id: DEC-046
title: Define the Rail Lance Branch Set
status: accepted
authoritative: false
---

# DEC-046 — Define the Rail Lance Branch Set

## Decision

Rail Lance uses the following three mutually exclusive branches:

- **`A` amplification — Unbounded Bore:** The normal facing-based lance retains its cadence and area but removes its fixed target-count limit, piercing every intersected enemy until maximum range.
- **`B` functional variant — Fracture Lance:** Every enemy pierced by the main lance emits a short perpendicular shockwave, spreading damage laterally while preserving the original facing-based line attack.
- **`C` playstyle conversion — Kinetic Capacitor:** The weapon fires less often through a charge cycle. Charge accumulates continuously and substantially faster while the mech moves. When full, it automatically fires along persistent facing with a much larger area and unlimited target penetration.

The conversion's slower cadence, movement incentive, enlarged area, and unlimited penetration operate together; it is not merely Unbounded Bore with a damage multiplier.

## Status

Accepted behavior; exact values and final names remain open.

## Context

Rail Lance needs branches matching the accepted amplification, functional-variation, and playstyle-conversion gradient. Its conversion must be clearly distinct from the unlimited-penetration amplification.

## Rationale

The three paths preserve a recognizable piercing-line identity while serving different preferences: reliable dense-line penetration, lateral horde coverage, or infrequent movement-powered screen clearing.

## Consequences

- The base Rail Lance retains a fixed, non-upgradeable target penetration count.
- Unbounded Bore preserves normal cadence and projectile area.
- Fracture Lance requires clear lateral shockwave feedback and rules for shockwave damage, reach, width, and repeat hits.
- Kinetic Capacitor must fire less often than the comparable base weapon. Its charge timing and movement bonus are fixed branch behavior rather than an ore-upgradeable attack-rate track.
- Kinetic Capacitor's projectile is substantially wider and pierces every intersected target until maximum range.
- Movement increases Kinetic Capacitor charge rate; being stationary does not stop charge entirely.
- Exact charge rate, movement multiplier, width multiplier, damage relationship, shockwave values, and names remain tuning or presentation work.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)
- [DEC-047 — Limit weapons to three common-ore stats](./DEC-047-three-stat-weapon-bundles.md)

## Supersedes / superseded by

Extends the accepted Rail Lance amplification in [DEC-045](./DEC-045-first-signature-amplification-branches.md) by fixing its functional variant and playstyle conversion.
