---
doc_id: DEC-016
title: Use a One-Minute Minor-Wave Orientation Phase
status: accepted
authoritative: false
validation: playtest
---

# DEC-016 — Use a One-Minute Minor-Wave Orientation Phase

## Decision

The opening orientation phase lasts from deployment at 0:00 through 1:00 of active simulation. During this minute, the geological survey is available and enemy waves are deliberately minor. The 35-minute run timer advances normally, and the player retains full control to move, fight, explore, mine, or fabricate. Standard enemy escalation begins at 1:00.

## Status

Accepted for playtesting.

## Context

The geological survey is deliberately withheld until active play begins. The player needs enough breathing room to absorb its resource types and abundance bands, orient on the map, and start moving without turning the survey into a pre-run planning screen or a pause.

## Considered options

### No protected opening cadence

This keeps pressure immediate but risks making the survey unreadable or punishing players for engaging with required information.

### Thirty-second orientation

This is compact but may be too short for an unfamiliar resource vocabulary, accessibility needs, or early route orientation.

### One-minute orientation

This creates a clear opening phase. Under the later seven-minute boss cadence, it leaves six active minutes of standard escalation before the first boss at 7:00.

## Rationale

One active minute is long enough to support quick information intake without feeling like a separate planning mode. The player can act throughout, so experienced players are not forced to wait. The clean 1:00 boundary also fits the standard boss cadence.

## Consequences

- The opening minute counts toward the 35-minute run.
- Fabrication pauses the opening clock under the normal full-simulation pause rule.
- Minor does not mean harmless or inactive. DEC-119 later supplies eight Skitterlings and slow two-enemy replenishment pulses as the initial minute-zero baseline; exact tuning remains open.
- The player may begin exploration, mining, and crafting before 1:00 if opportunities are available.
- Standard enemy escalation begins at 1:00, and the first boss is scheduled at 7:00.
- [DEC-017](./DEC-017-persistent-survey-review.md) defines the initial survey as automatic and non-modal and provides paused later review through fabrication; its presentation and accessibility must still fit the active opening.

## Playtest validation

Measure:

- Time taken to recognize available resources and begin a route.
- Damage and deaths during the opening minute.
- Whether experienced players remain meaningfully active rather than waiting for 1:00.
- Whether new players can understand the survey without opening fabrication to create extra reading time.
- Whether six minutes of standard escalation is sufficient preparation for the first boss.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [OQ-022 — How does the opening survey phase work?](../open-questions.md#oq-022--how-does-the-opening-survey-phase-work)

## Supersedes / superseded by

Resolves the opening-phase duration left open by [DEC-015](./DEC-015-in-run-opening-geological-survey.md). [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md) later changed the standard run and boss timing without changing this opening minute. [DEC-119](./DEC-119-accept-initial-alien-encounter-baseline.md) supplies its first complete enemy baseline.
