---
doc_id: DEC-066
title: Complete the Mine Layer Weapon
status: accepted
authoritative: false
---

# DEC-066 — Complete the Mine Layer Weapon

## Decision

Mine Layer (`B + D`, off-color `F`) is accepted with the following complete high-level design.

### Base behavior

Traveling a fixed distance automatically places a mine at the mech's route position; standing still produces no mines. A mine arms after a brief delay, then explodes when the first valid enemy enters its trigger area. Mines are indestructible, expire after a fixed lifetime, and do not block movement.

The weapon has a maximum active-mine capacity. Placing a new parent mine above capacity removes the oldest parent mine without detonating it.

### Common-ore stats

- **Damage:** explosion damage.
- **Blast radius:** explosion area; trigger area remains a fixed derived proportion.
- **Active-mine capacity:** maximum simultaneous parent mines.

Placement distance, arming delay, and parent-mine lifetime are fixed properties.

### Branches

- **`B` amplification — Seed Charges:** Each parent detonation scatters a fixed number of smaller mines nearby. Seed mines arm quickly, deal derived damage in a derived radius, expire rapidly, do not count against parent capacity, and cannot create more mines.
- **`D` functional variant — Selective Detonators:** Mines ignore isolated ordinary enemies and wait for either a configured local enemy density or an elite or boss. Their damage increases with the number and mass class of enemies inside the blast at trigger time. Near expiry, a mine may trigger on any valid enemy rather than vanish unused.
- **`F` playstyle conversion — Hunter Mines:** Placed mines become mobile spider charges after arming. Each pursues a nearby target and explodes on contact or when its pursuit lifetime ends near a valid enemy. Capacity becomes the maximum simultaneous hunters.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base weapon converts exploration movement into a defensive route. Seed Charges amplify a successful trap, Selective Detonators make traps wait for high-value moments, and Hunter Mines convert a route-based weapon into an autonomous pursuit system.

## Consequences

- Seed mines are non-recursive and visually distinct from parent mines.
- Selective Detonators must show when they are waiting for density versus ready to expire-fire.
- Hunter Mines retain distance-based production, so movement remains their resource even after conversion.
- Exact capacity increments, trigger density, mass weighting, seed layout, hunter speed, acquisition radius, and terrain traversal remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Mine Layer concept and locks `B` amplification, `D` functional, and `F` conversion funding. Exact values remain subject to playtesting.
