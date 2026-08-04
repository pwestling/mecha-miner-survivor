---
doc_id: DEC-074
title: Complete the Missile Rack Weapon
status: accepted
authoritative: false
---

# DEC-074 — Complete the Missile Rack Weapon

## Decision

Missile Rack (`E + F`, off-color `D`) is accepted with the following complete high-level design.

### Base behavior

At each launch event, the rack fires a fixed-size salvo of homing missiles. Guidance distributes missiles among distinct nearby valid enemies before assigning extras to already-targeted enemies. A missile retargets if its target becomes invalid and explodes in a small area on contact. Missiles expire after a fixed flight lifetime.

### Common-ore stats

- **Damage:** impact explosion damage.
- **Blast radius:** impact explosion area.
- **Launch rate:** salvo launches per unit of time.

Salvo size, targeting range, missile speed, turn rate, and flight lifetime are fixed properties.

### Branches

- **`F` amplification — MIRV Saturation:** Each missile splits once during flight into a fixed cluster of micro-missiles. Children retain the parent target where useful and distribute extras among nearby valid enemies. Micro-missiles use derived damage and radius and cannot split again.
- **`E` functional variant — Guardian Reserve:** Launched missiles enter an orbiting reserve instead of immediately pursuing distant enemies. Stored missiles automatically dive when enemies enter a defensive trigger radius, prioritizing threats closest to the mech. The reserve has a fixed capacity; launch rate determines how quickly it refills.
- **`D` playstyle conversion — Spiral Barrage:** Homing and enemy selection are removed. Each salvo launches missiles in evenly spaced radial directions with a rotating angular offset; the missiles curve outward into a spiral and explode on first contact or at maximum travel. The player positions the mech so repeated geometric waves intersect the horde.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base distributes reliable homing damage, MIRV Saturation dramatically expands a successful salvo, Guardian Reserve converts offense into stored close defense, and Spiral Barrage makes the mech itself the origin of a learnable area pattern.

## Consequences

- MIRV children are non-recursive and use deterministic distribution rules.
- Guardian Reserve missiles remain indestructible player-weapon objects and are visibly countable around the mech.
- A full Guardian Reserve does not create additional missiles; exact full-cap launch handling remains tuning.
- Spiral Barrage uses no target and fires even in empty space.
- Exact salvo size, distribution priority, split timing, child count, reserve capacity and release count, spiral curvature, rotation step, and terrain interaction remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Missile Rack concept and locks `F` amplification, `E` functional, and `D` conversion funding. Exact values remain subject to playtesting.
