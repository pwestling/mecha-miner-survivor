---
doc_id: DEC-042
title: Use Movement-Derived Persistent Mech Facing
status: accepted
authoritative: false
---

# DEC-042 — Use Movement-Derived Persistent Mech Facing

## Decision

While the player supplies nonzero movement input, the mech faces in that movement direction. When movement input returns to zero, the mech retains its last nonzero facing direction rather than snapping to a default direction or tracking an enemy.

Weapons may read this persistent facing direction for their automatic attack patterns. Other weapons may instead use current movement, enemy targeting, radial geometry, or autonomous behavior. The player never receives a separate facing or aiming control in the baseline game.

## Status

Accepted.

## Context

Directional automatic weapons need predictable orientation under movement-only controls, including while the mech pauses inside a mining zone. Enemy-tracking facing would remove positioning intent, while snapping to a default direction when stationary would create surprising attacks.

## Considered options

### Automatically face the nearest enemy

This maximizes immediate offensive convenience but turns facing weapons into ordinary enemy-targeting weapons and weakens movement-position coupling.

### Face only while moving and become directionless while stationary

This makes stationary directional attacks ambiguous or unusable.

### Retain the last movement direction

This gives the player indirect, movement-only control over facing and preserves meaningful orientation after stopping.

## Rationale

Persistent movement-derived facing is simple to learn, works with keyboard, stick, or directional-pad movement, and allows directional attacks without adding aim input. It also makes the final approach into a mining zone matter for weapons that use facing.

## Consequences

- The mech's facing changes whenever nonzero movement input changes direction.
- Releasing movement preserves the most recent facing.
- Facing-based weapons keep attacking in that retained direction while stationary unless their own rules specify another automatic target relationship.
- A weapon based on current movement rather than facing must separately define its stationary behavior.
- DEC-097 fixes the deployment-facing direction as east, or screen-right on the north-up camera; presentation must make it visually clear.
- Animation and silhouette must communicate facing in the fully top-down view.
- No right-stick, mouse-pointer, or separate rotation input controls combat facing in the baseline game.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-009 — How does the mech fight and move?](../open-questions.md#oq-009--how-does-the-mech-fight-and-move)
- [DEC-097 — Inherit direct movement, collision, and camera](./DEC-097-inherit-direct-movement-collision-and-camera.md)

## Supersedes / superseded by

This resolves facing derivation left open by [DEC-019](./DEC-019-movement-only-combat-controls.md) and remains compatible with the broad automatic patterns in [DEC-038](./DEC-038-broad-automatic-weapon-taxonomy.md). [DEC-097](./DEC-097-inherit-direct-movement-collision-and-camera.md) later fixes the initial direction and completes the surrounding movement model.
