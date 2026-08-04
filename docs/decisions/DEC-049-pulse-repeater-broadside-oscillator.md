---
doc_id: DEC-049
title: Convert Pulse Repeater into a Broadside Weapon
status: accepted
authoritative: false
---

# DEC-049 — Convert Pulse Repeater into a Broadside Weapon

## Decision

Pulse Repeater's `E`-funded playstyle conversion is **Broadside Oscillator**.

The converted weapon stops selecting enemies. Each automatic firing event launches rapid pulse fire in both directions perpendicular to the mech's persistent facing. The pulses travel along their fired trajectories rather than homing.

This makes circling, strafing alongside a horde, and arranging enemies beside the mech more valuable than approaching them head-on. Persistent facing remains defined by the mech's most recent nonzero movement direction and is retained while stationary.

## Status

Accepted behavior; exact pulse geometry and numeric tuning open.

## Context

The rejected Return Circuit proposal also removed automatic targeting, but its forward-and-return path felt less compelling and overlapped Rail Lance's facing-directed play. Pulse Repeater needs a conversion that changes how the player moves around hordes while remaining recognizable as a rapid projectile weapon.

## Considered options

### Return Circuit

Facing-directed pulses traveled outward, reversed at maximum range, and damaged enemies again while returning. This was considered but not selected.

### Broadside Oscillator

Pulse streams fire laterally to both sides of the mech, rewarding parallel movement, orbiting, and lateral passes through enemy formations.

## Rationale

Broadside Oscillator creates a pronounced playstyle change without adding a combat input. It replaces the base weapon's automatic nearest-enemy aim with movement-derived lateral alignment and occupies a different directional niche from Rail Lance's forward shot.

## Consequences

- Each firing event attacks both perpendicular sides; exact pulse count and per-pulse damage remain tuning.
- The player controls the firing axis indirectly through movement-derived persistent facing.
- The weapon continues firing from its retained facing while the mech is stationary.
- The presentation must clearly distinguish the left-right firing axis from the mech's forward facing.
- Every selected common-ore stat must remain meaningful in this conversion.
- Exact behavior at map boundaries and with projectile-blocking terrain remains open.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Replaces the unaccepted Return Circuit proposal. It does not settle Pulse Repeater's common-ore stat bundle or numeric tuning.
