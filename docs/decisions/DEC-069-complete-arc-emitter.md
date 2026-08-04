---
doc_id: DEC-069
title: Complete the Arc Emitter Weapon
status: accepted
authoritative: false
---

# DEC-069 — Complete the Arc Emitter Weapon

## Decision

Arc Emitter (`C + D`, off-color `B`) is accepted with the following complete high-level design.

### Base behavior

At each discharge, the emitter instantly strikes the nearest valid enemy within a fixed acquisition range. The arc then chains to the nearest unhit enemy within chain range until it reaches a fixed target-count limit or finds no valid next target. Every enemy may be hit only once by the same discharge.

### Common-ore stats

- **Damage:** damage to each target in a discharge.
- **Attack rate:** discharge frequency.
- **Chain range:** maximum distance between successive targets.

Initial acquisition range, base target-count limit, and damage-tick behavior are fixed properties.

### Branches

- **`C` amplification — Total Conduction:** The fixed target-count limit is removed. A discharge continues through unhit valid enemies until no next target exists within chain range.
- **`D` functional variant — Disruption Current:** Every struck enemy is briefly stunned after taking damage. Stun duration increases modestly with later positions in the chain, making a long arc a broad interruption tool. Bosses and resistant enemies receive a reduced effect rather than automatic full immunity unless their own rules say otherwise.
- **`B` playstyle conversion — Ball-Lightning Projector:** Instant target selection is replaced by a slow ball-lightning orb launched along persistent facing. The orb travels for a fixed lifetime and repeatedly arcs to nearby enemies. Attack rate controls orb launch frequency, chain range becomes the orb's arc radius, and damage controls each arc.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

Total Conduction realizes unlimited chain propagation without invalidating an ore-upgradeable chain-count stat, Disruption Current adds crowd interruption, and Ball-Lightning Projector turns an instantaneous topology problem into a moving storm the player positions through facing.

## Consequences

- Chain routing is deterministic and never revisits an enemy within one discharge.
- Total Conduction may still end after one target when the horde lacks a connected path.
- Disruption Current requires clear control-resistance presentation for bosses and elites.
- Ball-lightning orbs may coexist according to attack rate; exact orb speed, lifetime, per-orb arc cadence, target count per tick, and terrain interaction remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Arc Emitter concept and locks `C` amplification, `D` functional, and `B` conversion funding. Exact values remain subject to playtesting.
