---
doc_id: DEC-043
title: Assign the Fifteen Base Weapons to the Resource Graph
status: accepted
authoritative: false
---

# DEC-043 — Assign the Fifteen Base Weapons to the Resource Graph

## Decision

The following equal-tier base-weapon concepts, recipe pairs, and playstyle-conversion off-colors form the accepted 15-weapon catalog structure. Names are working labels rather than final presentation names.

| Pair | Weapon | Off-color |
| --- | --- | --- |
| `A + B` | Rail Lance | `C` |
| `A + C` | Cluster Mortar | `F` |
| `A + D` | Gravity Projector | `B` |
| `A + E` | Attack Drones | `D` |
| `A + F` | Tracking Laser | `B` |
| `B + C` | Pulse Repeater | `E` |
| `B + D` | Mine Layer | `F` |
| `B + E` | Sentry Pod | `A` |
| `B + F` | Orbital Cutters | `E` |
| `C + D` | Arc Emitter | `B` |
| `C + E` | Reactor Pulse | `F` |
| `C + F` | Wake Projector | `D` |
| `D + E` | Scatter Array | `C` |
| `D + F` | Ram Field | `A` |
| `E + F` | Missile Rack | `D` |

The six initial signature weapons are Rail Lance, Pulse Repeater, Gravity Projector, Reactor Pulse, Missile Rack, and Ram Field. Their recipe edges form the cycle `AB–BC–CE–EF–FD–DA`, so each abstract resource appears in exactly two initial signature recipes.

## Status

Accepted catalog structure; exact weapon details remain open.

## Context

The six-resource complete pair graph requires 15 equal-tier weapons. Concepts, pair placement, off-colors, and signature selection need to work as one structure without assigning fictional resource themes prematurely or forcing every run into equal tactical-role coverage.

## Considered options

### Assign concepts to pairs after fully designing every branch

This delays graph feedback and risks producing a weapon set that cannot be distributed cleanly.

### Assign weapons randomly

This preserves abstract resources but can accidentally cluster targeting and delivery patterns around particular colors.

### Use a deliberately distributed graph assignment

This balances broad delivery families and signature incidence while retaining freedom inside individual weapon designs.

## Rationale

The assignment gives every resource two incident direct-fire or directly targeted weapons. Ground, route, deployable, body-centered, and autonomous systems are then distributed across the remaining edges. The signature cycle exposes six markedly different starting patterns while treating every resource symmetrically at the initial-roster level.

The off-color assignment is near-balanced and provides controlled variation in access to playstyle conversions without making every profile identical.

## Consequences

- The concept identity, recipe pair, off-color, and initial-signature status of every base weapon are fixed.
- Working names may change without changing the underlying catalog concept.
- Each weapon still needs exact automatic behavior, stat bundle, amplification branch, functional-variant branch, playstyle-conversion branch, costs, feedback, and edge cases.
- Each weapon must designate which native recipe color funds amplification and which funds functional variation.
- Direct-fire or directly targeted weapons occupy the cycle `AB–BC–CD–DE–EF–FA`, giving every resource exactly two such incident weapons.
- Ground, route, or deployable weapons occupy `AC`, `AD`, `BD`, `BE`, and `CF`.
- Body-centered or autonomous weapons occupy `AE`, `BF`, `CE`, and `DF`.
- `A`, `C`, and `E` each fund two playstyle conversions; `B`, `D`, and `F` each fund three.
- All 15 endpoint-plus-off-color three-resource sets are unique.
- Of the 15 unconstrained four-color profiles, 2 expose two playstyle conversions, 11 expose three, and 2 expose four.
- Final resource identities were deferred until the weapon and branch relationships provided enough evidence to name them coherently; [DEC-076](./DEC-076-specialized-resource-identities.md) later resolves them.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This resolves the catalog-concept, graph-placement, off-color, and signature-selection work left open by [DEC-037](./DEC-037-unique-weapons-and-soft-profile-balance.md), [DEC-039](./DEC-039-six-mech-initial-roster.md), [DEC-040](./DEC-040-three-branch-transformation-gradient.md), and [DEC-041](./DEC-041-equal-tier-base-weapon-catalog.md).
