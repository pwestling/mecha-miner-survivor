---
doc_id: DEC-070
title: Complete the Reactor Pulse Weapon
status: accepted
authoritative: false
---

# DEC-070 — Complete the Reactor Pulse Weapon

## Decision

Reactor Pulse (`C + E`, off-color `F`) is accepted with the following complete high-level design.

### Base behavior

At a regular cadence, the mech emits an instantaneous radial pulse centered on itself. Every valid enemy within the radius takes damage once. The pulse requires no target and fires even when no enemy is nearby.

### Common-ore stats

- **Damage:** damage per pulse.
- **Pulse radius:** radial effect area.
- **Pulse rate:** pulses per unit of time.

Pulse shape and the one-hit-per-enemy rule are fixed properties.

### Branches

- **`E` amplification — Critical-Mass Cycle:** Every enemy hit by a pulse contributes charge to the next pulse. Stored charge increases the next pulse's damage and radius up to fixed caps; after that pulse, charge is recalculated from the enemies it hits rather than accumulating permanently.
- **`C` functional variant — Kinetic Vent:** Each pulse also drives affected enemies outward and briefly slows them after displacement. Damage remains intact, shifting the weapon toward emergency spacing and perimeter control.
- **`F` playstyle conversion — Supernova Cycle:** Regular pulses are replaced by a much slower, visibly charging supernova. A growing ring telegraphs the final radius; completion releases a vastly stronger and larger pulse. Pulse rate controls the slow charge cycle through a fixed branch multiplier, so it remains infrequent relative to the base weapon.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base is reliable body-centered coverage, Critical-Mass Cycle rewards diving into dense hordes, Kinetic Vent adds breathing room, and Supernova Cycle turns frequent maintenance damage into a high-risk positional payoff.

## Consequences

- Critical-Mass charge and caps must be visible before the next pulse.
- A missed or empty Critical-Mass pulse produces no charge for the following pulse.
- Kinetic Vent pushes away from the mech and uses resistance rules for bosses.
- The Supernova telegraph is player-owned, follows the mech throughout charging, and cannot damage its owner.
- Exact charge contribution, caps, push distance, slow, supernova multipliers, and warning timing remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Reactor Pulse concept and locks `E` amplification, `C` functional, and `F` conversion funding. Exact values remain subject to playtesting.
