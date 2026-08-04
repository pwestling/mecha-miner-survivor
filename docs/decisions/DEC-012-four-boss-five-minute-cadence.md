---
doc_id: DEC-012
title: Schedule Four Bosses Before the Final Horde Crescendo
status: superseded
authoritative: false
validation: playtest
---

# DEC-012 — Schedule Four Bosses Before the Final Horde Crescendo

## Decision

During a standard 25-minute run, boss aliens arrive at 5:00, 10:00, 15:00, and 20:00 of active simulation. No new boss spawns at the 25-minute mission-extraction threshold. The final phase from 20:00 through extraction is a horde crescendo that culminates the run's build.

A boss that remains alive at 25:00 does not block mission extraction.

## Status

Its five-minute cadence is superseded by [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md). The four-boss structure and final boss-free crescendo remain accepted at the new seven-minute cadence.

## Context

Bosses were already established as periodic threat spikes, but their timing was unspecified. The 25-minute run target naturally supports five-minute phases. The final phase also needs room to demonstrate the completed build rather than introducing a boss that cannot affect the automatic extraction condition.

## Considered options

### Irregular or randomized boss times

Uncertain timing could increase tension, but it would make resource routing and the promised opportunity to fabricate before a boss harder to read and balance.

### Boss every five minutes, including extraction

A 25:00 boss would arrive at the same instant the run automatically succeeds, making it irrelevant unless extraction rules changed.

### Four bosses followed by a final horde crescendo

Bosses at 5:00 through 20:00 provide four predictable build checks. The last five minutes then test the mature build under peak horde pressure before automatic extraction.

## Rationale

Regular five-minute checkpoints make the power curve legible to players and designers. They create clear targets for early resource availability and fabrication without requiring fabrication itself to be scheduled. Omitting a 25:00 boss preserves automatic extraction and gives the final build a distinct mass-combat culmination.

## Consequences

- The player must receive adequate warning before each five-minute boss threshold.
- Map generation, mining opportunities, and recipe prices must permit meaningful power growth before the 5:00 boss and between later bosses.
- Time spent in fabrication does not advance boss arrival thresholds.
- The final horde crescendo needs its own escalation design rather than relying on a fifth boss spawn.
- [DEC-013](./DEC-013-persistent-overlapping-bosses.md) makes bosses persist until killed, keeps ordinary hordes active, and permits scheduled overlap. DEC-111 later resolves rewards, and DEC-119 supplies encounter composition, surrounding horde intensity, warnings, and the four boss behaviors.
- The cadence is an initial standard-run target and should be measured in playtesting.

## Playtest validation

Measure:

- Player power and recipe purchases reached before each boss.
- Death rates and damage taken in each five-minute phase.
- Bosses still alive at the next arrival or at extraction.
- Whether five-minute predictability creates useful planning or mechanical repetition.
- Whether 20:00–25:00 feels like a satisfying culmination rather than an easier gap after the last boss.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [OQ-016 — What rewards, if any, come directly from defeating monsters?](../open-questions.md#oq-016--what-rewards-if-any-come-directly-from-defeating-monsters)
- [OQ-020 — How do interval boss encounters resolve?](../open-questions.md#oq-020--how-do-interval-boss-encounters-resolve)

## Supersedes / superseded by

Originally resolved the timestamps left open by [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md). Superseded by [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md), which preserves automatic extraction and the four-boss structure.
