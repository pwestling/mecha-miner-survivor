---
doc_id: DEC-071
title: Complete the Wake Projector Weapon
status: accepted
authoritative: false
---

# DEC-071 — Complete the Wake Projector Weapon

## Decision

Wake Projector (`C + F`, off-color `D`) is accepted with the following complete high-level design.

### Base behavior

As the mech travels, it lays contiguous temporary wake segments behind its route at a fixed distance interval. Enemies occupying the wake take damage at a fixed tick cadence. Segments expire individually after their lifetime. Standing still creates no new wake but does not remove existing segments.

### Common-ore stats

- **Damage:** wake damage per tick.
- **Trail width:** damaging width of the laid route.
- **Trail duration:** lifetime of each segment.

Placement interval and damage-tick cadence are fixed properties.

### Branches

- **`C` amplification — Runaway Wake:** Uninterrupted movement builds momentum stacks. Newly laid segments gain increasing damage and width with momentum up to a fixed cap. Momentum decays after the mech stops and may also decay after sufficiently sharp reversals.
- **`F` functional variant — Carrier Ignition:** Enemies damaged by the mech's wake become ignited and leave their own short damaging trails as they move. Enemy trails use derived damage, width, and duration and cannot ignite additional enemies.
- **`D` playstyle conversion — Circuit Closure:** The damaging wake is replaced by a temporary conductive trace. Crossing an active trace laid by the same weapon closes a loop; the enclosed area immediately erupts for high damage and the consumed loop trace disappears. Damage controls the eruption, trail width controls connection tolerance and boundary thickness, and duration controls how long a route remains available for closure.

## Status

Accepted complete high-level design; numeric tuning, loop interpretation, and listed edge rules open.

## Rationale

The base converts kiting routes into hazards, Runaway Wake amplifies sustained movement, Carrier Ignition makes enemies spread the route effect, and Circuit Closure turns freeform movement into deliberate shape drawing with a large payoff.

## Consequences

- Carrier trails are non-recursive and visually distinguishable from the player's original wake.
- Circuit Closure does not deal ordinary continuous trail damage; its power is concentrated in completed loops.
- Only a self-intersection with a still-active trace can close a loop.
- Exact reversal tolerance, momentum rates, ignition cooldown, enclosure choice when paths self-intersect multiple times, minimum loop area, boundary treatment, and terrain interaction remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Wake Projector concept and locks `C` amplification, `F` functional, and `D` conversion funding. Exact values remain subject to playtesting.
