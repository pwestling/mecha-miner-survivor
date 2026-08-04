---
doc_id: DEC-096
title: Use Vampire Survivors as the Default Precedent
status: accepted
authoritative: false
validation: reference-and-playtest
---

# DEC-096 — Use *Vampire Survivors* as the Default Precedent

## Decision

Unless an accepted decision says otherwise, use the simplest core single-player normal-stage behavior of *Vampire Survivors* as the default precedent for:

- direct movement and collision feel;
- camera tracking and combat framing;
- automatic-combat pressure;
- ordinary enemy approach, spawning, waves, and despawning;
- boss pursuit and anti-avoidance behavior;
- standard pause and run-flow conventions; and
- combat HUD and results-information conventions.

Explicit game decisions always take precedence. A reference behavior is not inherited when it conflicts with the mining-and-crafting progression loop, the science-fiction mech theme, the large finite randomized map, the 35-minute structure, or another accepted rule.

Do not automatically inherit *Vampire Survivors*' XP, level-up offerings, treasure chests, static or repeating stage layout, weapon acquisition and evolution rules, run duration, loadout limits, gold economy, metaprogression prices, multiplayer, target platforms, art medium, DLC systems, alternate modes, exceptional characters, or secret mechanics. Those systems use this game's decisions or remain open.

When the reference contains several modes or exceptions, agents use the simplest base normal-stage behavior rather than combining variants. When no clear analogue exists, the question remains open instead of being invented under the reference rule.

## Status

Accepted as the interpretation rule for completing the gameplay specification.

## Rationale

The project began as a *Vampire Survivors* clone whose differentiating systems are mining, deterministic fabrication, randomized geology, finite exploration, mechs, and aliens. Requiring separate approval for every familiar low-level survivor-like convention adds ambiguity without strengthening those differentiators.

A bounded default gives agents a consistent answer while protecting the decisions that make this game distinct. Choosing the simplest core single-player stage also prevents later DLC, challenge modes, co-op exceptions, and secret content from silently expanding scope.

## Consequences

- Canonical gameplay documents state inherited behaviors explicitly rather than relying on readers to know the reference game.
- Reference-derived numbers remain prototype baselines subject to playtesting unless separately locked.
- A later explicit decision can override any inherited default without reopening unrelated reference behavior.
- The standard gameplay specification is single-player; future multiplayer requires its own rules and does not introduce unresolved multiplayer branches into baseline systems.
- Reference research records the factual behavior used to support inherited decisions.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Documentation Conventions](../conventions.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

Supersedes [DEC-001](./DEC-001-vampire-survivors-combat-reference.md) only where it said all behavior outside five named areas must remain uninherited. DEC-001's explicit combat reference remains accepted, and every concrete project decision that conflicts with the reference continues to take precedence.
