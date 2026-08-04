---
doc_id: DEC-003
title: Use Automatic Proximity Mining with Resource-Specific Payouts
status: accepted
authoritative: false
---

# DEC-003 — Use Automatic Proximity Mining with Resource-Specific Payouts

## Decision

Mining starts and continues automatically while the player remains within a mining point's valid area. Leaving causes unfinished progress to decay very quickly. The common basic ore pays continuously, while rare reward resources pay only when their extraction completes.

## Status

Accepted.

## Context

Mining must create positional commitment without displacing the simple, movement-centered controls inherited from the combat reference. It also needs more than one risk profile: routine resources should remain useful after a partial attempt, while rare resources should create a stronger all-or-nothing commitment.

## Considered options

### Require a mining button or held input

This would make mining explicit but add a continuous input that competes with movement and could confuse whether position or button state controls progress.

### Activate automatically through proximity

This makes the player's position the mining input and keeps attention on movement, dodging, and combat readability.

### Give every resource only at completion

This would make every mining attempt highly punitive and reduce the value of brief or interrupted common-ore stops.

### Give every resource continuously

This would support partial success but weaken the stakes of rare discoveries.

### Use resource-specific payout profiles

Continuous common ore and completion-only rare rewards create distinct commitment levels without changing the core controls.

## Rationale

Automatic proximity activation connects mining directly to the game's central positional challenge. Rapid decay makes leaving a real cost while retaining player agency to flee. Different payout profiles let common mining support steady run growth and let rare resources feel consequential.

## Consequences

- The mining boundary must be clearly visible and accurately communicated.
- Re-entering before progress reaches zero resumes the remaining progress.
- Common mining requires frequent, legible payout feedback.
- Rare mining must clearly warn that partial progress grants no rare reward.
- Exact distance, timings, decay curve, depletion, and boundary edge cases require later decisions.
- The base push-your-luck pressure comes from reduced dodging space plus rapid decay. This decision did not choose an additional rare-resource hazard; [DEC-004](./DEC-004-mining-retention-threat-and-banking.md) later selected the threat-beacon model.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [RES-002 — Holdout and extraction pressure patterns](../research/RES-002-holdout-extraction-pressure-patterns.md)

## Supersedes / superseded by

Extended by [DEC-004](./DEC-004-mining-retention-threat-and-banking.md), which selects finite common deposits, threat beacons for rare resources, and survival-gated cross-run banking. [DEC-031](./DEC-031-circular-mining-zone-and-fast-decay.md) later establishes circular zones, a 0.5-second exit grace period, and four-times linear decay as initial playtest rules.
