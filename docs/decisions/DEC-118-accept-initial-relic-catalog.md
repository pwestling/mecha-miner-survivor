---
doc_id: DEC-118
title: Accept the Initial Relic Catalog
status: accepted
authoritative: false
---

# DEC-118 — Accept the Initial Relic Catalog

> **Completion note:** DEC-127 fixes cache selection without replacement, duplicate exclusion, no dedicated guards, in-view signaling, the discovery comparison, and the 150-ore sale value left open here.

## Decision

The initial relic catalog contains ten run-local relics: Retrograde Engine, Ghostline Chassis, Dead-Reckoning Array, Colossus Governor, War-Drum Oscillator, Event-Horizon Coupler, Fission Seed, Redline Crucible, Claim-Jumper Core, and Sequential Reactor.

Relic tuning deliberately begins on the powerful side. Nine entries transform most or all equipped weapons through a substantial benefit paired with a gameplay-altering constraint or danger. Claim-Jumper Core is the intentional mining-focused exception, doubling mining speed while enemies move 50% faster during active extraction.

Every relic must be conceptually explained in one sentence at discovery. Expanded details may clarify values and interactions but cannot conceal a major consequence absent from that sentence.

## Status

Accepted for prototyping and playtesting.

## Context

DEC-028 establishes one exploration-found mech-wide relic slot and requires relics to create significant behavioral changes rather than ordinary passive bonuses. A concrete catalog is required to exercise replacement choices, weapon interactions, exploration value, and the intended appetite for surprising run adaptation.

## Considered options

### Conservative mostly-positive bonuses

These would be easy to balance but overlap utilities and fail to justify a unique replaceable relic slot.

### Narrow per-weapon modifiers

These can create strong synergies but make discoveries frequently irrelevant to the current four-weapon loadout.

### Powerful whole-build transformations with legible tradeoffs

These make discoveries exciting, demand adaptation, and preserve the option to sell a relic that does not fit the current build.

## Rationale

The selected catalog changes geometry, timing, targeting, clustering, kill behavior, heat management, and extraction pressure. Its effects are deliberately dramatic enough to be felt immediately. Starting above the eventual power target makes the novel behavior easy to evaluate; later playtesting can reduce values without needing to invent the identity of each relic.

One-sentence concept summaries prevent complexity from turning into surprise punishment. A player can understand the essential benefit and tradeoff before replacing an existing relic, while expanded inspection handles exact weapon interactions.

## Consequences

- The initial content target is ten distinct relic effects.
- Nine relics broadly affect weapons; Claim-Jumper Core broadly affects mining and enemy movement instead.
- Retrograde Engine uses a three-times attack-frequency target rather than the earlier illustrative two-times projectile-only example.
- Ghostline Chassis retains reduced original weapon damage in addition to its delayed weaker copy.
- Fission Seed reduces direct weapon damage in exchange for chain-capable death explosions.
- Claim-Jumper Core does not establish a stationary arsenal anchor; it doubles extraction rate while adding 50% enemy movement speed during active mining.
- Exact tuning remains revisable, but revisions should preserve the player-facing behavioral identity and tradeoff of each entry.
- DEC-121 later fixes the five fresh-pool relics and five individually purchased permanent pool additions. Cache selection weights, duplicates, sale values, guarding, and discovery presentation remain open under OQ-027.

## Specification links

- [Mech Relics](../67-mech-relics.md)
- [Initial Relic Catalog](../69-initial-relic-catalog.md)
- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-027 — How are mech relic discoveries presented and resolved?](../open-questions.md#oq-027--how-are-mech-relic-discoveries-presented-and-resolved)

## Supersedes / superseded by

This supplies the initial content catalog required by [DEC-028](./DEC-028-one-exploration-found-mech-relic.md). It supersedes the illustrative two-times reversed-projectile example as the accepted Retrograde Engine and rejects the stationary-anchor version considered for Claim-Jumper Core. It does not supersede the one-slot, exploration, installation, replacement, sale, or cache-count rules in DEC-028 through DEC-030. [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) later fixes fresh-profile and permanently unlocked pool membership.
