---
doc_id: DEC-105
title: Use a Simple Pursuer-First Enemy Roster
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-105 — Use a Simple Pursuer-First Enemy Roster

## Decision

The initial standard map uses a deliberately simple *Vampire Survivors*-like enemy behavior structure.

### Ordinary pursuers

The large majority of ordinary aliens continuously move toward the mech and deal contact damage. Different enemy identities primarily vary through:

- maximum health;
- movement speed;
- contact damage;
- body size and collision area;
- resistance to knockback or control;
- visual identity and death feedback; and
- the number, cadence, and combinations in which they appear.

An elite is normally a tougher or more resistant version of an ordinary pursuer, not a new AI behavior.

### Wave-event enemies

Scheduled wave events may introduce simple fixed-direction formations such as lateral sweeps, walls, streams, swarms, or encirclements. Their challenge comes from spawn geometry, timing, density, and statistics rather than complex decision-making.

### Rare specialists

The initial standard map may use at most two ordinary non-boss specialist behaviors. Each specialist has one readily legible exception—such as firing a simple projectile or self-destructing—on top of otherwise minimal behavior. [DEC-108](./DEC-108-use-one-straight-shot-enemy-specialist.md) later narrows this allowance to exactly one straight-shot ranged specialist.

### Bosses

Each interval boss begins from persistent pursuit and contact pressure, with no more than one defining additional behavior in its initial design. Durability, damage, size, resistance, and wave context may provide the rest of its distinction.

Do not create ordinary baseline roles that require coordinated support AI, buff networks, tactical flanking, multi-stage attacks, elaborate area denial, or several interacting abilities. A later enemy may add such behavior only through an explicit content decision.

## Status

Accepted as the initial enemy-complexity and roster-architecture constraint. [DEC-106](./DEC-106-use-ten-ordinary-enemy-identities.md) later fixes the ordinary roster at ten identities, [DEC-108](./DEC-108-use-one-straight-shot-enemy-specialist.md) assigns exactly one of them the sole specialist behavior, and [DEC-119](./DEC-119-accept-initial-alien-encounter-baseline.md) supplies their identities, statistics, formations, bosses, and minute assignments.

## Rationale

Core *Vampire Survivors* pressure comes primarily from simple contact enemies whose health, speed, density, and timed combinations change. Keeping individual AI simple lets hundreds of enemies remain readable and makes the minute schedule—not a catalog of tactical abilities—the main encounter-design tool.

This also reduces animation, VFX, telegraphing, and free-asset requirements while the project's mining and procedural-map systems already supply additional complexity.

## Consequences

- Enemy visual variety does not imply behavioral complexity.
- Wave authors first vary statistics, populations, cadence, and formations before inventing another AI rule.
- Weapon balance must be tested against masses of simple pursuers rather than assuming every weapon has a bespoke counter-role.
- The later-selected straight-shot specialist must remain individually understandable amid dense hordes.
- Boss concepts should remain readable while ordinary waves continue around them.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

Narrows the open specialist-enemy space in [DEC-098](./DEC-098-use-minute-authored-horde-waves.md) without changing its minute-authored director, spawn, despawn, or boss-persistence rules. [DEC-106](./DEC-106-use-ten-ordinary-enemy-identities.md) later completes the ordinary-roster count.
