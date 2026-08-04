---
doc_id: DEC-098
title: Use Minute-Authored Horde Waves
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-098 — Use Minute-Authored Horde Waves

## Decision

The standard 35-minute run uses a deterministic time-authored horde director modeled on core *Vampire Survivors* stages.

Each elapsed minute has a wave definition that specifies its ordinary enemy families, desired minimum on-field population, spawn interval, and any authored sub-events. The director replenishes the horde toward that minimum and may use short event formations such as directional rushes, encircling groups, or concentrated swarms. Exact counts and compositions are content and balance data.

Ordinary enemies generally spawn on valid navigable ground just outside the active camera and approach the mech. They can be recycled or despawned after remaining sufficiently far from the player so the game maintains pressure around the current play area. Bosses never despawn. Aliens summoned by an activated Hyper Gold threat beacon also remain active under the existing beacon rule rather than being erased when the player retreats.

Under the later DEC-105, the large majority of ordinary aliens continuously pursue the mech and threaten through contact. Fixed-direction movement primarily belongs to scheduled event formations. DEC-108 later selects exactly one straight-shot non-boss specialist. The wave schedule, not player level or an adaptive assessment of build strength, drives baseline escalation.

Bosses arrive on the existing 7:00 cadence. A boss that falls far outside the combat area re-enters or repositions to a valid off-screen approach point so it cannot be permanently escaped. Re-entry never occurs on top of the mech or as an untelegraphed unavoidable hit. Bosses use greater resistance to knockback, control, and instant defeat than ordinary enemies.

## Status

Accepted as the baseline horde-director, ordinary spawn, and boss anti-avoidance model. DEC-119 later supplies the enemy catalog, minute table, caps, events, beacon responses, and boss mechanics; exact values remain tuning work.

## Rationale

Minute-authored waves create the predictable escalation arc that supports routing and pre-boss planning while still producing dense local uncertainty. Off-screen replenishment lets a finite randomized map retain survivor-like pressure without fixing spawn nests or exhausting the horde in explored areas.

Boss persistence and off-screen re-entry preserve the accepted damage-check pressure. Exceptions for threat-beacon survivors preserve the lasting cost of starting a Hyper Gold encounter.

## Consequences

- The first 0:00–1:00 wave remains deliberately minor; later minute definitions escalate toward each boss and the final 28:00–35:00 crescendo.
- A living boss is not counted as ordinary population and cannot block ordinary replenishment.
- Spawn points must be reachable from the mech and must respect solid map topology.
- The director may enforce an on-field cap for performance and readability, but it does not visibly stop pressure at ordinary play densities.
- Fresh-profile and upgraded-account balance use the same authored schedule; the game does not secretly weaken a wave because the player is struggling.
- Spawn and despawn distances must prevent visible popping and obvious boundary exploitation.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timer, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [OQ-020 — How do interval boss encounters resolve?](../open-questions.md#oq-020--how-do-interval-boss-encounters-resolve)

## Supersedes / superseded by

Completes the baseline wave-director and boss anti-avoidance behavior left open by [DEC-012](./DEC-012-four-boss-five-minute-cadence.md), [DEC-013](./DEC-013-persistent-overlapping-bosses.md), and [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md). [DEC-105](./DEC-105-use-a-simple-pursuer-first-enemy-roster.md) later narrows ordinary and boss behavioral complexity, and [DEC-119](./DEC-119-accept-initial-alien-encounter-baseline.md) supplies the complete initial content and schedule. This record does not decide boss rewards.
