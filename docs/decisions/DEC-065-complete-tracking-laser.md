---
doc_id: DEC-065
title: Complete the Tracking Laser Weapon
status: accepted
authoritative: false
---

# DEC-065 — Complete the Tracking Laser Weapon

## Decision

Tracking Laser (`A + F`, off-color `B`) is accepted with the following complete high-level design.

### Base behavior

The laser acquires the nearest valid enemy within range and maintains a continuous beam until that target dies, leaves range, or otherwise becomes invalid. It does not switch merely because another enemy becomes closer. Damage is applied continuously at a fixed tick cadence.

Holding the same target builds **focus** from zero to a fixed maximum, progressively increasing beam damage. Losing or changing the target normally resets focus.

### Common-ore stats

- **Damage:** base beam damage per second.
- **Range:** acquisition distance and beam reach.
- **Focus rate:** how quickly the beam reaches its fixed maximum focus.

Beam width, damage-tick cadence, and maximum focus are fixed properties.

### Branches

- **`A` amplification — Coherence Memory:** Focus no longer resets on target change. It transfers to the next valid target and decays gradually only while no target is held.
- **`F` functional variant — Target Designator:** Reaching a fixed focus threshold exposes the target, causing it to take increased damage from every player weapon. Exposure persists briefly after the beam leaves; exact stacking and boss rules remain tuning.
- **`B` playstyle conversion — Cutting Vector:** Automatic enemy selection is removed. A continuous beam projects along persistent facing and damages every intersected enemy. Holding facing steady builds focus; rotating the firing axis beyond a tolerance resets it. Range becomes beam length.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base form owns sustained target commitment, Coherence Memory improves continuity, Target Designator turns that commitment into whole-build support, and Cutting Vector makes the player steer a crowd-cutting beam through movement. The branches preserve a common focus vocabulary without collapsing into the same use pattern.

## Consequences

- Focus state and its current damage benefit must be visible.
- Target Designator's increased-damage preview must state whether it affects bosses and how it combines with other vulnerability effects.
- Cutting Vector retains its last axis while stationary under persistent-facing rules.
- Exact focus cap, transfer grace, decay, exposure multiplier and duration, facing tolerance, and terrain interception remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Tracking Laser concept and locks `A` amplification, `F` functional, and `B` conversion funding. Exact values remain subject to playtesting.
