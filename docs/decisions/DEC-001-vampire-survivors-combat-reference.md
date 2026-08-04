---
doc_id: DEC-001
title: Use a Vampire Survivors-Inspired Combat Reference
status: superseded
authoritative: false
---

# DEC-001 — Use a *Vampire Survivors*-Inspired Combat Reference

## Decision

The game riffs on *Vampire Survivors* in its automatic attacks, multi-weapon combat, horde pressure, movement emphasis, and camera angle. Mechanics outside this explicit boundary are not inherited automatically.

## Status

Superseded in part by [DEC-096](./DEC-096-use-vampire-survivors-as-the-default-precedent.md). The five explicit reference areas remain accepted; the rule that all other reference behavior must stay uninherited is replaced by a bounded default precedent.

## Context

The game needs a clear, low-level attention model that leaves the player able to navigate alien hordes, explore for mining points, and make positional extraction decisions. The phrase “*Vampire Survivors* clone” is a useful high-level brief but too broad for agents to interpret consistently.

## Considered options

### Treat the entire reference game as the default

This would be concise, but it would silently import XP, chests, exact run structure, and numerous other systems that conflict with or distract from the mining identity.

### Adopt only an explicit combat and presentation boundary

This preserves the intended feel while requiring every other system to be decided on its own merits.

## Rationale

Automatic attacks place the player's continuous attention on movement and positioning. Horde pressure supplies the danger needed to make location-bound mining meaningful. Multiple weapons support build growth, while the familiar camera perspective keeps dense combat and navigation readable.

## Consequences

- Weapon attacks occur automatically. The later [DEC-019](./DEC-019-movement-only-combat-controls.md) excludes manual aim and fire from the baseline, while [DEC-038](./DEC-038-broad-automatic-weapon-taxonomy.md) permits broad weapon-specific targeting and delivery patterns without adding combat inputs.
- The game supports multiple weapons and their upgrades.
- Movement is a primary player input and defensive skill.
- Large alien hordes create continuous spatial pressure.
- Camera angle and framing take *Vampire Survivors* as a reference; DEC-097 later fixes the standard tracking, rotation, scale, and boundary behavior.
- XP, treasure chests, timers, bosses, pickups, weapon evolutions, and other reference systems require separate decisions.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [RES-001 — *Vampire Survivors* reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

[DEC-096](./DEC-096-use-vampire-survivors-as-the-default-precedent.md) supersedes this record's narrow exclusion rule while retaining its explicit combat reference. [DEC-097](./DEC-097-inherit-direct-movement-collision-and-camera.md) resolves camera and movement details.
