---
doc_id: DEC-019
title: Use Movement-Only Baseline Combat Controls
status: accepted
authoritative: false
---

# DEC-019 — Use Movement-Only Baseline Combat Controls

> **Completion note:** DEC-097 fixes the movement model and DEC-126 fixes its 3.0M/s speed plus the player, enemy, and boss collision footprints. Input-device bindings remain platform and accessibility work.

## Decision

Movement is the only direct moment-to-moment combat control in the baseline game. The player does not manually aim or fire weapons. Every equipped weapon attacks automatically using its own targeting rule or non-targeted pattern.

Menu navigation and pre-run selection controls are separate from combat. Activated utilities, manual aim, and button-triggered combat abilities or dashes are not part of the baseline, though a later explicit decision may extend the controls.

## Status

Accepted.

## Context

The game already adopts automatic attacks and movement-centered survival from *Vampire Survivors*. Mining adds route choice and position-constrained dodging, increasing the player's attention burden without needing manual weapon execution.

## Considered options

### Twin-stick aiming or manual fire

This could deepen mech handling but would compete with navigation, mining-area positioning, and fabrication planning for attention.

### Movement plus activated abilities

This can add tactical expression but creates cooldown and input obligations not yet required by the core loop.

### Movement-only baseline

This keeps the player's continuous focus on navigation, dodging, positioning, and route decisions while weapon choice determines automatic combat behavior.

## Rationale

Movement-only control preserves the intended survivor-like accessibility and gives the mining constraint room to become the primary mechanical addition. Weapon-specific patterns can still create positional skill without manual aiming.

## Consequences

- Every weapon requires a complete automatic targeting rule or non-targeted pattern.
- DEC-042 derives facing from the last nonzero movement direction and preserves it while stationary. Weapon descriptions must explain how current movement, persistent facing, proximity, or enemy position affects automatic behavior.
- The control scheme does not require aim input, a fire button, an ability button, or a dash button during baseline combat.
- Activated utilities or auxiliary abilities require an explicit later extension and must be evaluated against the intended attention budget.
- DEC-097 fixes immediate normalized movement, non-solid enemy collision, contact damage, damage response, and fixed camera behavior. Exact speeds, collision shapes, and input-device bindings remain tuning and platform work.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [OQ-009 — How does the mech fight and move?](../open-questions.md#oq-009--how-does-the-mech-fight-and-move)
- [DEC-038 — Use a broad automatic-weapon taxonomy](./DEC-038-broad-automatic-weapon-taxonomy.md)
- [DEC-042 — Use movement-derived persistent mech facing](./DEC-042-movement-derived-persistent-facing.md)
- [DEC-097 — Inherit direct movement, collision, and camera](./DEC-097-inherit-direct-movement-collision-and-camera.md)

## Supersedes / superseded by

Narrows the automatic-combat reference in [DEC-001](./DEC-001-vampire-survivors-combat-reference.md). It does not supersede automatic attacks or movement emphasis. [DEC-038](./DEC-038-broad-automatic-weapon-taxonomy.md) later broadens the allowed automatic weapon forms and delivery patterns without changing the control rule. [DEC-042](./DEC-042-movement-derived-persistent-facing.md) resolves how movement determines facing. [DEC-097](./DEC-097-inherit-direct-movement-collision-and-camera.md) completes the standard movement, collision, damage-response, and camera model.
