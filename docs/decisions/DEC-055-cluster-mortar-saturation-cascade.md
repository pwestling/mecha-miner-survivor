---
doc_id: DEC-055
title: Amplify Cluster Mortar with Saturation Cascade
status: accepted
authoritative: false
---

# DEC-055 — Amplify Cluster Mortar with Saturation Cascade

## Decision

Cluster Mortar's `C`-funded amplification is **Saturation Cascade**. The funding assignment is fixed by [DEC-060](./DEC-060-balance-native-branch-funding.md).

When the primary mortar explosion damages an enemy, that enemy becomes the center of a smaller secondary explosion after a brief delay. Secondary explosions from different enemies can overlap and damage the same targets. A secondary explosion cannot create further explosions, even when it damages an enemy that was not hit by the primary blast.

The weapon retains its automatic concentration targeting, committed impact position, visible warning marker, arcing shell, and travel delay.

## Status

Accepted behavior and funding color; secondary-blast rules and numeric tuning open.

## Context

Cluster Mortar's amplification needs to preserve its delayed area-bombardment identity while producing a major qualitative increase rather than merely adding another shell. Dense hordes should make the upgrade feel especially powerful.

## Considered options

### Fire additional ordinary shells

This increases output but reads as a routine projectile-count multiplier.

### Seed secondary blasts from every enemy hit

This makes a well-placed primary explosion scale dramatically with enemy density while preserving the original targeting and delivery pattern.

## Rationale

Saturation Cascade is “samey but bigger and better”: the player still wants the mortar to land on the densest group, but a successful hit now turns that density into overlapping explosions and substantially greater coverage.

## Consequences

- Only enemies damaged by the primary explosion seed secondary blasts.
- Secondary blasts overlap but never recurse.
- The warning and effects must distinguish the primary impact from the delayed cascade without obscuring enemy attacks.
- Exact secondary delay, radius, damage, repeated-hit behavior, and handling of enemies that die before their seeded blast remain open.
- All eventual common-ore stats must remain meaningful; secondary damage and area should derive visibly from the primary shell where applicable.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Extends the accepted base behavior in [DEC-054](./DEC-054-cluster-mortar-base-behavior.md). [DEC-060](./DEC-060-balance-native-branch-funding.md) later assigns `C` as its funding resource. Numeric tuning remains open.
