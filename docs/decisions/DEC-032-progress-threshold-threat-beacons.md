---
doc_id: DEC-032
title: Escalate Rare Threat Beacons at Progress Thresholds
status: accepted for playtesting
authoritative: false
---

# DEC-032 — Escalate Rare Threat Beacons at Progress Thresholds

## Decision

The first mining progress on a rare-resource point activates its threat beacon and begins a focused alien response. The beacon escalates with stronger responses when extraction first reaches 25%, 50%, and 75% progress.

Each threshold triggers at most once for that mining point. Leaving the zone stops additional beacon escalation while the player is absent, but enemies already summoned remain. Progress continues through its normal grace and decay rules. Returning resumes the operation, and any not-yet-triggered thresholds can activate as progress advances again.

Completing extraction stops all new beacon-generated responses, but surviving summoned enemies remain in the world.

## Status

Accepted for playtesting.

## Context

Rare mining needs escalating risk that the player can read and abandon, but retreat should not erase the danger already created. Repeatedly crossing a threshold must not become a way to farm or accidentally multiply the same wave.

## Considered options

### Constant beacon pressure

This is simple but provides few discrete moments at which the player reevaluates whether to stay.

### Timer-based escalation

This creates pressure but is less directly tied to how close the pending reward is to completion.

### Progress-threshold escalation

This makes greater commitment visibly produce both greater danger and greater proximity to the completion-only payout.

## Rationale

The 25%, 50%, and 75% thresholds create three understandable commitment checkpoints. Persistent summoned enemies make abandonment a retreat rather than a free reset, while stopping future escalation lets leaving still reduce additional risk.

## Consequences

- Threat activation and each threshold require advance warning and unmistakable feedback.
- Progress decay below a triggered threshold does not reset it or allow it to trigger again.
- Abandonment does not despawn, refund, or pacify summoned enemies.
- Completion stops beacon generation rather than deleting active threats.
- DEC-119 later fixes phase-scaled counts, current-roster composition, formation geometry, a two-second warning, elite involvement, persistence, and capacity behavior. Numeric response tuning remains under OQ-005.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-005 — What makes mining a push-your-luck system?](../open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system)

## Supersedes / superseded by

Narrows the threat-beacon encounter left open by [DEC-004](./DEC-004-mining-retention-threat-and-banking.md). Exact encounter content remains subject to playtesting.
