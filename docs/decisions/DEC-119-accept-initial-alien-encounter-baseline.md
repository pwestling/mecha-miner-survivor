---
doc_id: DEC-119
title: Accept the Initial Alien Encounter Baseline
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-119 — Accept the Initial Alien Encounter Baseline

> **Completion note:** DEC-125 revises boss Hull for legal-build feasibility, and DEC-126 fixes movement scale, collision footprints, damage resolution, recovery, control stacking, and failure margins. [DEC-128](./DEC-128-set-extraction-zone-and-resonance-field-radii.md) fixes the 6.0M geode resonance-field radius left open here; independent modifier tuning remains open. Encounter values remain playtest-tunable.

## Decision

Accept the first complete standard-map encounter baseline defined by the [Initial Alien and Boss Roster](../31-initial-alien-roster.md) and [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md).

The baseline contains ten fixed-profile ordinary identities built from six silhouette families and four readable variants; exactly one straight-shot specialist; a shared stat-only elite treatment; four persistent bosses with one defining behavior each; a deterministic 35-row minute schedule; boss-arrival population relief followed by renewed escalation; a seven-minute final density crescendo; and four progress-scaled Hyper Gold beacon responses that reuse the current ordinary roster.

Its pressure curve follows the broad normal-stage method established by the *Vampire Survivors* reference—simple contact pursuit, minute-driven population and composition changes, economical variants, periodic bosses, formation events, and late saturation—while using this game's 35-minute mining, fabrication, resource, and extraction rules.

## Status

Accepted as the initial prototyping and playtesting baseline. Names and exact balance values remain adjustable without reopening the architecture.

## Context

Earlier decisions fixed the director, roster size, simple behavior budget, fixed identity profiles, one ordinary specialist, boss cadence, boss persistence, and boss rewards but left the actual content and schedule open. The mining loop cannot be evaluated without an authored pressure curve, and arbitrary placeholder waves would make failures difficult to attribute.

## Rationale

A complete baseline makes every major combat minute reproducible and gives weapon, mining, map, relic, performance, and progression tests a shared scenario. Simple identities keep the horde readable and asset-feasible, while composition and formations provide variety. Brief boss relief preserves entrance legibility without pausing the core horde. Beacon formulas stay relevant throughout the run without adding bespoke enemies.

The values intentionally form a coherent starting hypothesis rather than claiming balance before implementation. The schedule includes an explicit adjustment order so tests change the smallest relevant layer first.

## Consequences

- OQ-030 is resolved at catalog-baseline level; further changes are balance revisions rather than missing encounter architecture.
- OQ-020 is resolved at encounter-baseline level: every interval uses one boss, ordinary density briefly drops at arrival, warnings begin 15 seconds beforehand, difficulty escalates through four fixed bosses, and their accepted loot bursts remain unchanged.
- OQ-005 no longer needs beacon composition, formation, response timing, elite involvement, or phase scaling; geode-field radii and independent modifier tuning remain open.
- The standard baseline uses no separate Reaper-like end-state attacker at 35:00. Automatic extraction ends the run immediately.
- Ordinary contact initially uses a 0.75-second per-enemy repeat and a 0.20-second global contact grace period.
- Needler is the only ordinary ranged identity and first appears at minute 16.
- Elites add statistics and presentation but no behavior or loot.
- The four bosses are Riftjaw, Brood Titan, Prism Crown, and Skybreaker Apex, with charge, minion ring, radial burst, and marked leap respectively.
- Exact Hull, damage, speed, cadence, population, and spawn values remain subject to instrumentation and playtesting.

## Specification links

- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-005 — What makes mining a push-your-luck system?](../open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system)
- [OQ-020 — How do interval boss encounters resolve?](../open-questions.md#oq-020--how-do-interval-boss-encounters-resolve)
- [OQ-030 — What enemies, bosses, and minute waves fill a standard run?](../open-questions.md#oq-030--what-enemies-bosses-and-minute-waves-fill-a-standard-run)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

This completes the initial content and schedule left open by DEC-098 and DEC-105 through DEC-108 without changing their architecture. It supplies the boss content left open by DEC-013 and DEC-079, preserves DEC-111's reward model, and fills the threat-beacon response left open by DEC-032. It does not supersede the standard timer, extraction, boss persistence, no-drop, resonance-field, or mining-progress rules.
