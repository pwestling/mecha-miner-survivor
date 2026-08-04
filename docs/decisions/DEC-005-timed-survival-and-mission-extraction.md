---
doc_id: DEC-005
title: Use Timed Survival, Interval Bosses, and Mission Extraction
status: accepted
authoritative: false
---

# DEC-005 — Use Timed Survival, Interval Bosses, and Mission Extraction

## Decision

Each level is completed by surviving until a time limit, thematically presented as mission extraction. Boss aliens arrive at intervals during the level. Mission extraction permanently banks collected cross-run resources; death before the time limit forfeits them.

## Status

Accepted.

## Context

The game needed an explicit success condition and a definition of “survive” for persistent-resource banking. It also needed periodic threat peaks within the continuous horde curve.

## Considered options

### Objective-based or location-based completion

Requiring a final objective or evacuation location could deepen routing, but it would make success depend on an additional system beyond the intended time-survival reference.

### Timed completion presented as extraction

This preserves the familiar survival arc while giving the ending a coherent science-fiction mission frame.

### Continuous horde pressure without interval bosses

This would create a smooth curve but fewer authored peaks and fewer opportunities to test a build against a distinct threat.

### Interval boss arrivals

Bosses punctuate the timer, create memorable pressure spikes, and riff on the reference game's pacing.

## Rationale

Timed survival makes exploration and mining compete against a legible global clock. Mission-extraction framing connects survival to the mech expedition theme. Interval bosses create landmarks in the run's difficulty and pacing curve.

## Consequences

- The level must clearly show time remaining and warn about boss arrivals and mission extraction.
- Rare resources remain at risk until the time limit is reached.
- Mission extraction triggers at the time limit without requiring a separate evacuation location or final interaction.
- A surviving interval boss does not prevent timed mission extraction.
- The later [DEC-079](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md) sets the current standard duration at 35 minutes of active simulation and schedules bosses at 7:00, 14:00, 21:00, and 28:00, superseding the original DEC-011 and DEC-012 timing.
- The later [DEC-013](./DEC-013-persistent-overlapping-bosses.md) makes bosses persist until killed and permits scheduled overlap; [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) resolves boss rewards as physical resource bursts, and [DEC-119](./DEC-119-accept-initial-alien-encounter-baseline.md) supplies the boss and wave content.
- The later [DEC-010](./DEC-010-one-deployment-per-run.md) establishes that one timed level is one complete run.
- Mission extraction presentation must distinguish itself from mining extraction.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)

## Supersedes / superseded by

Extends [DEC-004](./DEC-004-mining-retention-threat-and-banking.md) by defining successful survival as reaching timed mission extraction.
