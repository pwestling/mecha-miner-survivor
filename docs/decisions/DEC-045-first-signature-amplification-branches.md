---
doc_id: DEC-045
title: Define the First Signature-Weapon Amplifications
status: accepted
authoritative: false
---

# DEC-045 — Define the First Signature-Weapon Amplifications

## Decision

The first three signature weapons use these accepted base limitations and amplification branches:

- **Rail Lance:** The base projectile pierces a fixed, non-upgradeable number of targets. Its `A` amplification removes the target-count limit, allowing the lance to pierce every intersected enemy until maximum range.
- **Pulse Repeater:** Base pulses travel at a finite, non-upgradeable projectile speed. Its `B` amplification makes each pulse hit its selected target instantly, eliminating travel delay and misses caused by target movement.
- **Gravity Projector:** The base attack is a damaging pull pulse at an automatically selected ground position. Its `A` amplification makes each deployed pulse repeat once at that same position after a fixed delay.

Exact fixed target count, projectile speed, echo delay, damage values, and other tuning remain open.

## Status

Accepted behavior; tuning open.

## Context

Earlier proposed amplifications merely duplicated projectiles or fields. They were too basic for the intended “samey but bigger and better” branch. An amplification should feel like a major development of the familiar weapon even when it does not change the player's fundamental play style.

## Considered options

### Duplicate the base attack

Twin projectiles or paired wells increase output but can read as a routine multiplier rather than a major branch.

### Remove or transcend a defining limitation

Infinite target penetration, instantaneous delivery, or a delayed repeat preserves the recognizable attack while changing its ceiling or reliability in a distinctive way.

## Rationale

Each selected amplification preserves what the player already likes about the weapon while producing a clearly communicable qualitative improvement. None requires a new input or a new ore stat.

## Consequences

- Rail Lance penetration and Pulse Repeater projectile speed are fixed base properties rather than common-ore stat tracks, preventing their amplifications from invalidating ore investment.
- Gravity Projector is based on discrete pulses rather than a continuously active remote field.
- The amplification previews must clearly compare the removed limit or added echo with base behavior.
- Future amplification concepts should be held to a similarly consequential standard; merely adding a second copy is insufficient when it feels like an ordinary numeric multiplier.
- This decision does not itself accept any functional variants or playstyle conversions; those are settled by later weapon-specific decisions.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This replaces the Twin-Bore Overdrive, Twinlink Overclock, and Binary Singularity proposals created under [DEC-040](./DEC-040-three-branch-transformation-gradient.md). It does not alter the three-category branch structure.
