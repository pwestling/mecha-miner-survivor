---
doc_id: DEC-106
title: Use Ten Ordinary Enemy Identities
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-106 — Use Ten Ordinary Enemy Identities

## Decision

The content-complete initial standard map uses **ten ordinary enemy identities** across its 35-minute schedule.

- Target six substantially distinct silhouette families and four production-efficient variants built through palette, scale, detail, animation, statistics, or feedback changes.
- A variant must remain quickly distinguishable in dense combat; a numerical change with no readable presentation does not constitute a separate identity.
- Each authored minute uses no more than three ordinary identities in its baseline wave composition. Enemies surviving a minute transition may briefly produce additional overlap.
- [DEC-108](./DEC-108-use-one-straight-shot-enemy-specialist.md) assigns exactly one of the ten identities a straight-projectile specialist behavior. The other nine use ordinary pursuit and contact behavior.
- Elites and interval bosses are outside the ten-identity count. A fixed-direction event version that reuses an existing identity also does not consume another identity. DEC-119 later establishes that the standard baseline has no separate end-state attacker at 35:00.

The first playable prototype may implement only six of the ten identities. Six is a production milestone, not the intended content-complete roster.

## Status

Accepted as the initial standard-map roster-size and simultaneous-variety baseline. DEC-108 later fixes the specialist count and behavior; DEC-119 supplies the ten identities, statistics, appearances, event formations, elite treatment, bosses, and minute assignments. Exact values remain playtest tuning.

## Rationale

The initial recommendation of six identities was materially leaner than the first *Vampire Survivors* stage. Mad Forest uses about fifteen distinct normal-wave entries when stat and palette variants are counted, or roughly ten broader enemy families, while ordinarily composing a given minute from only one to three types.

Ten identities should provide enough change over this game's longer 35-minute run without requiring ten bespoke behavior systems. Reusing four production-efficient variants follows the reference game's economical use of related enemies, while the three-identity wave ceiling protects readability during mining commitments and dense automatic combat.

## Consequences

- Enemy production plans need six strong base silhouettes and four readable variations rather than ten wholly unrelated asset sets.
- The minute schedule should introduce, retire, and later recombine identities instead of displaying the full roster simultaneously.
- A six-identity prototype can validate pursuit, density, damage, and formation pressure, but repetition observed there is not evidence that the final ten-identity target is unnecessary.
- The single specialist identity is part of the ten rather than bonus normal-wave content.
- Elite and boss production budgets are tracked separately.

## Specification links

- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

Completes the ordinary-roster size left open by [DEC-105](./DEC-105-use-a-simple-pursuer-first-enemy-roster.md) without changing its behavior-complexity constraints.
