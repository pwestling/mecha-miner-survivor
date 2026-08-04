---
doc_id: DEC-004
title: Use Finite Common Deposits, Rare Threat Beacons, and Survival-Gated Banking
status: accepted
authoritative: false
---

# DEC-004 — Use Finite Common Deposits, Rare Threat Beacons, and Survival-Gated Banking

## Decision

Common ore deposits contain finite quantities, and every awarded unit remains available for crafting later in the current run. Mining a rare resource acts as a threat beacon that attracts a focused alien response. Cross-run resources collected during a run are banked only if the player survives that run and are forfeited on failure.

## Status

Accepted.

## Context

The design needed to clarify whether common mining can continue indefinitely, whether leaving a node removes prior payouts, how rare nodes add pressure, and when persistent resources become safe.

## Considered options

### Infinite common deposits

This could support prolonged defense but would reduce the need to resume exploration and could make one favorable point the dominant resource strategy.

### Finite common deposits

Finite capacity gives each point a completion arc and eventually sends the player back into the map.

### Revoke collected common ore after leaving

This would make common mining closer to an all-or-nothing reward and conflict with its intended continuous payout profile.

### Retain collected common ore during the run

This preserves partial success and lets the player accumulate resources for a later crafting decision.

### Make rare mining only spatially difficult

Completion-only rewards and restricted dodging already create risk, but rare points may not feel behaviorally distinct from common deposits.

### Make rare mining a threat beacon

A focused enemy response builds on the existing horde vocabulary and gives rare discoveries a clear, legible escalation.

### Bank cross-run resources immediately

This would remove the resource from risk after mining completes and weaken the importance of surviving the remainder of the run.

### Bank cross-run resources only on survival

This preserves stakes after the mining event and connects rare exploration success to the run's ultimate outcome.

## Rationale

Finite common deposits reinforce continued exploration. Retaining paid-out ore makes the continuous reward model honest and supports later crafting. Threat beacons distinguish rare mining without weakening core player controls. Survival-gated banking makes persistent gains valuable cargo rather than guaranteed rewards at discovery time.

## Consequences

- Common node capacity and depletion must be visible.
- Rapid decay affects only unfinished progress, not common ore already awarded.
- The inventory must support saving ordinary resources for later within-run crafting.
- Rare mining must visibly announce the focused alien response it causes.
- Threat-beacon wave composition, escalation, and cleanup remain open.
- The UI must distinguish collected-but-unsecured cross-run resources from permanently banked resources.
- [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md) defines successful survival as reaching the level's time limit and completing mission extraction.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [RES-002 — Holdout and extraction pressure patterns](../research/RES-002-holdout-extraction-pressure-patterns.md)

## Supersedes / superseded by

Extended by [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md), which defines the successful-survival state used for banking. [DEC-032](./DEC-032-progress-threshold-threat-beacons.md) later establishes immediate activation, one-time 25%, 50%, and 75% escalation thresholds, and persistent summoned enemies as initial playtest behavior.
