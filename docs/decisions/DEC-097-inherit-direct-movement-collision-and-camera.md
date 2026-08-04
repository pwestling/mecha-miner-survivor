---
doc_id: DEC-097
title: Inherit Direct Movement Collision and Camera
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-097 — Inherit Direct Movement, Collision, and Camera

> **Completion note:** DEC-126 fixes the 3.0M/s base speed, 1.0M player circle, enemy and boss contact footprints, 0.75-second repeat interval, 0.20-second contact grace, and absence of universal post-hit invulnerability. Exact camera scale remains open.

## Decision

Use the core *Vampire Survivors* feel for the standard mech's direct motion:

- Movement input immediately sets the mech's movement direction at its current movement speed.
- Digital input supports eight directions; analog input supports the full directional circle. Diagonal input is normalized so it is not faster.
- The mech has no baseline acceleration ramp, braking delay, momentum, turn radius, sprint, dash, dodge, or movement stamina.
- Releasing movement stops translation immediately and preserves the last nonzero facing direction.
- Moving backward or sideways relative to the mech's persistent facing has no separate speed penalty because movement input also establishes facing.
- Before the first movement input, the mech faces east, which is screen-right on the fixed north-up camera.

Ordinary aliens, elites, and bosses do not form solid physical walls against the mech. Overlapping an enemy causes contact damage at a controlled repeat cadence, but neither participant pushes the other through rigid-body collision. Enemies may overlap one another. Blocking terrain and the finite world boundary remain solid.

Taking damage does not cause baseline hitstun, knockback, forced movement, control loss, or interruption of automatic mining while the mech remains inside the extraction zone. Exact contact-damage repeat interval and post-hit protection are tuning values.

The camera uses a fixed world scale during active standard play, remains north-up, does not rotate, and offers no manual pan, aim, look-ahead, or combat zoom. It tracks the mech at the center of the view except when clamped by the finite map boundary. Camera motion does not introduce intentional lag that changes aiming or dodging.

## Status

Accepted as the baseline movement, body-collision, damage-reaction, and camera model. Numeric speed, collision shapes, contact cadence, and camera scale remain playtest values.

## Rationale

Immediate movement is the reference game's primary defensive verb and keeps the player's attention on spatial decisions rather than vehicle simulation. Non-solid enemy bodies prevent dense hordes from creating unavoidable rigid-body traps while contact damage still makes overlap dangerous.

A fixed player-following camera makes automatic attack patterns, mining boundaries, and enemy pressure predictable. The finite map requires boundary clamping, which is the only normal reason for the mech to leave screen center.

## Consequences

- Movement abilities or utilities may explicitly override this model, but no activated movement action exists by default.
- Enemy attacks can create hazards and projectiles but do not displace the mech unless an explicit future effect says so.
- Mining edge cases do not need a baseline forced-movement branch.
- Mech animation may visually rotate or articulate without delaying the movement vector.
- Keyboard and controller movement must produce equivalent speed and control authority.
- The camera's exact orthographic scale, smoothing required only to suppress visual jitter, and aspect-ratio framing remain implementation and playtest concerns.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-009 — How does the mech fight and move?](../open-questions.md#oq-009--how-does-the-mech-fight-and-move)

## Supersedes / superseded by

Completes the movement and camera behavior established by [DEC-019](./DEC-019-movement-only-combat-controls.md), [DEC-021](./DEC-021-wide-fully-top-down-camera.md), and [DEC-042](./DEC-042-movement-derived-persistent-facing.md) using the expanded reference rule in DEC-096.
