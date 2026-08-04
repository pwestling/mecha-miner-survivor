---
doc_id: DEC-011
title: Start with a 25-Minute Standard Run Timer
status: superseded
authoritative: false
validation: playtest
---

# DEC-011 — Start with a 25-Minute Standard Run Timer

## Decision

The initial standard run lasts 25 minutes of active gameplay simulation. Time spent in the fabrication menu does not consume the run timer because the entire gameplay simulation freezes there. Twenty-five minutes is an initial playtest target rather than a promise that every future map, mode, or difficulty uses the same duration.

## Status

Superseded by [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md), which extends the standard run to 35 minutes.

## Context

One timed level now contains the complete build arc and ends the run. The duration must allow exploration, repeated mining commitments, multiple crafting decisions, interval bosses, and a final build culmination without making each attempt unnecessarily long. The first *Vampire Survivors* stage uses a 30-minute limit; this game adds navigation and fabrication time, with fabrication excluded from the gameplay clock.

## Considered options

### 20-minute standard

This would create a compact run but may compress exploration and make early route mistakes too difficult to recover from.

### 25-minute standard

This preserves room for a multi-phase build while remaining shorter than the 30-minute starting-stage reference.

### 30-minute standard

This most closely matches the original reference but could produce substantially longer wall-clock sessions once paused fabrication decisions are included.

## Rationale

Twenty-five active minutes is a middle starting point: long enough to support the mining-driven build arc and short enough to test whether exploration and menu time make the total session feel overlong. A five-minute unit also gives the first boss-cadence experiments a simple structure without deciding those timestamps here.

## Consequences

- Mission extraction triggers after the living player accumulates 25 minutes of active simulation in a standard run.
- Fabrication-menu time does not advance the 25-minute clock.
- Total wall-clock time will generally exceed 25 minutes.
- The complete economic and combat power curve must culminate within that interval.
- [DEC-012](./DEC-012-four-boss-five-minute-cadence.md) uses the five-minute phases for four boss arrivals and a final crescendo; resource availability and recipe affordability must support that schedule.
- Later maps or modes may use other durations only through an explicit exception.

## Playtest validation

Measure:

- Total wall-clock duration in addition to gameplay-clock duration.
- Fabrication time per run and the distribution of long menu sessions.
- When builds first become coherent, reach major upgrades, and feel complete.
- Whether players have enough time to recover from an unsuccessful resource route.
- Whether the final minutes feel like a meaningful culmination or dead time after the build is solved.
- Abandonment and fatigue relative to death and successful extraction.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [OQ-003 — What is the complete run or session structure?](../open-questions.md#oq-003--what-is-the-complete-run-or-session-structure)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

Originally resolved the duration left open by [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md). Superseded by [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md).
