---
doc_id: DEC-079
title: Use a Thirty-Five-Minute Run and Seven-Minute Boss Cycle
status: accepted
authoritative: false
---

# DEC-079 — Use a Thirty-Five-Minute Run and Seven-Minute Boss Cycle

## Decision

The standard run lasts 35 minutes of active gameplay simulation. Boss aliens arrive at 7:00, 14:00, 21:00, and 28:00. No new boss spawns at 35:00; the phase from 28:00 through mission extraction is a seven-minute final horde crescendo.

The one-minute opening orientation phase remains unchanged. Standard escalation begins at 1:00, leaving six active minutes before the first boss. Fabrication and relic-resolution pauses freeze the run timer and therefore do not advance boss or extraction thresholds.

Each boss persists until killed, later bosses still arrive on schedule even if earlier bosses survive, and a living boss at 35:00 does not block mission extraction.

## Status

Accepted as the new standard-run playtest baseline.

## Rationale

The earlier 25-minute schedule left too little time to explore a large randomized map, complete repeated positional mining commitments, fabricate a multi-weapon build, and test it between bosses. Extending each phase to seven minutes distributes ten additional active minutes across the full run rather than placing them all at the end.

The new thresholds shift progressively from the old schedule: `+2`, `+4`, `+6`, `+8`, and `+10` minutes at the four bosses and extraction. This preserves a regular, learnable rhythm while giving early and mature builds more time to develop.

## Consequences

- A standard run contains five seven-minute phases.
- The first boss arrives six minutes after standard escalation begins.
- Four bosses and the final crescendo retain equal phase lengths.
- Existing wave beats must be redistributed across the longer phases so the added time supports exploration and build development without becoming dead air.
- Total wall-clock sessions normally exceed 35 minutes because full-simulation pauses do not consume active time.
- Enemy-wave growth, map scale, resource density, fabrication prices, and boss strength must be tuned for the longer economy and combat arc.

## Specification links

- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [DEC-005 — Use timed survival and mission extraction](./DEC-005-timed-survival-and-mission-extraction.md)

## Supersedes / superseded by

Supersedes the 25-minute duration in DEC-011 and the five-minute boss cadence in DEC-012. It preserves the boss persistence and overlap rules in DEC-013 and the one-minute opening orientation in DEC-016.
