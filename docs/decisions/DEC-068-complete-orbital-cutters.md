---
doc_id: DEC-068
title: Complete the Orbital Cutters Weapon
status: accepted
authoritative: false
---

# DEC-068 — Complete the Orbital Cutters Weapon

## Decision

Orbital Cutters (`B + F`, off-color `E`) is accepted with the following complete high-level design.

### Base behavior

A fixed number of indestructible cutter discs orbit the mech clockwise at evenly spaced angles and a fixed base radius. A disc deals contact damage as it passes through an enemy. Each disc has a short per-enemy hit cooldown so lingering overlap does not create unbounded frame-rate damage. Cutters pass through enemies and do not block movement.

### Common-ore stats

- **Damage:** contact damage per valid cutter hit.
- **Cutter size:** damaging contact area of each disc.
- **Orbit speed:** revolutions per unit of time.

Cutter count, base orbit radius, and per-enemy contact cooldown are fixed properties.

### Branches

- **`F` amplification — Kinetic Flywheel:** Valid cutter hits add temporary momentum stacks that increase orbit speed and contact damage up to a fixed cap. Momentum decays when cutters stop hitting enemies.
- **`B` functional variant — Deflection Ring:** Cutters destroy enemy projectiles they contact. Each successful interception also emits a short outward shard burst that damages enemies, preserving offensive value when the defensive function triggers.
- **`E` playstyle conversion — Tethered Reaper:** All cutters fuse into one much larger blade connected to the mech by an energy tether. The blade lags behind linear movement and swings wide during turns and reversals; contact damage gains a fixed multiplier based on blade speed. Cutter size becomes blade size and orbit speed becomes tether response and attainable swing speed.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base creates predictable orbit timing, Kinetic Flywheel turns horde contact into escalating momentum, Deflection Ring adds a defensive interception role, and Tethered Reaper makes the player's curved movement directly steer a single high-impact weapon.

## Consequences

- Cutter and projectile effects must remain readable when several cutters overlap.
- Deflection Ring does not erase beams, ground zones, contact attacks, or explicitly non-interceptable enemy projectiles.
- Tethered Reaper uses deterministic lag rather than random motion; the player must be able to learn how turns produce swings.
- Exact cutter count, orbit radius, hit cooldown, momentum cap and decay, shard geometry, tether length, lag, and speed multiplier remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Orbital Cutters concept and locks `F` amplification, `B` functional, and `E` conversion funding. Exact values remain subject to playtesting.
