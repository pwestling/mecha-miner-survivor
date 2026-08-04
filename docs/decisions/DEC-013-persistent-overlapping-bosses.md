---
doc_id: DEC-013
title: Keep Bosses Active and Allow Scheduled Overlap
status: accepted
authoritative: false
validation: playtest
---

# DEC-013 — Keep Bosses Active and Allow Scheduled Overlap

## Decision

Each interval boss persists until killed. Ordinary horde pressure continues during boss encounters, and later bosses arrive at their scheduled times even if an earlier boss remains alive. Multiple bosses can therefore be active simultaneously.

Boss defeat is not required for mission extraction: a living player still succeeds when the 35-minute active-simulation limit is reached.

## Status

Accepted for playtesting.

## Context

The fixed boss schedule establishes regular build checks, but a boss that simply expires or is replaced would not strongly test damage output. Conversely, requiring every boss kill would contradict timed survival as the success condition. The encounter needs to remain threatening without becoming an objective gate.

## Considered options

### Despawn or replace the boss after a fixed interval

This limits difficulty spikes and prevents overlap, but allows the player to evade each boss until it disappears instead of testing the build.

### Pause later boss spawns until the current boss dies

This prevents overlap but turns the schedule into a soft kill gate and removes the consequence of failing an earlier damage check.

### Persist bosses and preserve the schedule

This makes killing a boss the clearest way to relieve pressure while retaining survival, rather than mandatory kills, as the victory condition.

## Rationale

Persistent bosses make damage output matter even though the objective is timed survival. Continuing ordinary hordes preserves the game's core spatial pressure during the fight. Scheduled overlap converts an underpowered build into escalating danger without invalidating automatic extraction for a player skilled enough to survive it.

## Consequences

- Killing a boss relieves an ongoing source of pressure even if it grants no separate reward.
- Avoiding a boss may preserve the player temporarily but risks overlap at the next five-minute threshold.
- Bosses and ordinary hordes must remain readable when several large threats coexist.
- Boss collision, pursuit, leashing, and anti-avoidance behavior require explicit design so persistence remains meaningful across a large exploration map.
- Difficulty tuning must account for the possibility of two or more bosses without making recovery categorically impossible.
- [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) later resolves boss rewards as physical resource bursts; [DEC-119](./DEC-119-accept-initial-alien-encounter-baseline.md) supplies the four bosses and surrounding horde schedule.

## Playtest validation

Measure:

- Time to kill for each boss and the distribution by build family.
- Frequency and duration of two-, three-, or four-boss overlap.
- Whether players intentionally kite bosses rather than engage them.
- Recovery rates after a boss survives into the next phase.
- Readability, unavoidable-damage reports, and performance during overlap.
- Successful extractions with a boss still alive.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [OQ-020 — How do interval boss encounters resolve?](../open-questions.md#oq-020--how-do-interval-boss-encounters-resolve)

## Supersedes / superseded by

Resolves persistence and overlap rules left open by [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md) and [DEC-012](./DEC-012-four-boss-five-minute-cadence.md). It does not change timed extraction.
