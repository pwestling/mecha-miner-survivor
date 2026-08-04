---
doc_id: DEC-031
title: Use Visible Circular Mining Zones with Fast Exit Decay
status: accepted for playtesting
authoritative: false
---

# DEC-031 — Use Visible Circular Mining Zones with Fast Exit Decay

## Decision

Every mining point has a clearly visible circular extraction zone. Mining is active while the mech is inside the zone.

Leaving grants a 0.5-second grace period during which unfinished progress does not change. After that grace period, unfinished progress decays linearly at four times that point's forward extraction rate. Re-entering before progress reaches zero resumes extraction from the remaining value.

These timing values are initial playtest settings.

## Status

Accepted for playtesting.

## Context

The player needs a boundary that is readable from a fully top-down wide camera. Exit decay should punish abandoning a commitment without allowing tiny boundary crossings or collision jitter to erase progress immediately.

## Considered options

### No grace period

This makes every boundary crossing costly but can feel noisy or unfair when the mech briefly clips the edge.

### Long grace period or paused progress

This is forgiving but allows the player to kite outside the intended hold area without sacrificing much commitment.

### Brief grace followed by accelerated decay

This ignores incidental crossings while making a genuine retreat rapidly expensive.

## Rationale

A circle is immediately legible and supports circling or weaving inside the point. A 0.5-second grace period protects control feel, while four-times decay makes leaving a meaningful decision across resource types with different extraction durations.

## Consequences

- Every mining point needs a non-color-only boundary treatment visible before entry.
- Boundary crossing, grace, and decay states need distinct feedback.
- A half-complete extraction loses its remaining progress in one eighth of the full forward-extraction duration after the grace period.
- Common ore already paid out is never removed by progress decay.
- Pausing the complete simulation also pauses the grace period and decay.
- Exact zone radius, resource-specific exceptions, fractional ore behavior, and forced-displacement treatment remain open in OQ-004.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)

## Supersedes / superseded by

Narrows the boundary and decay behavior established by [DEC-003](./DEC-003-proximity-mining-and-resource-payouts.md). Values remain subject to playtesting.
