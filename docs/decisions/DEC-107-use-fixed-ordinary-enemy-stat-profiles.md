---
doc_id: DEC-107
title: Use Fixed Ordinary Enemy Stat Profiles
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-107 — Use Fixed Ordinary Enemy Stat Profiles

> **Completion note:** DEC-119 supplies the enemy values and DEC-126 supplies their world-speed, footprint, damage-pressure, and control-resolution context. Those remain playtest-tunable rather than missing.

## Decision

Each ordinary enemy identity has one fixed base statistic profile throughout a standard run. Reusing an identity later in the 35-minute schedule does not invisibly increase its maximum health, movement speed, contact damage, body size, or control resistance.

Standard-run escalation instead comes from authored, observable changes:

- introducing identities with intrinsically stronger fixed profiles;
- increasing desired population pressure and spawn cadence;
- combining up to three ordinary identities in a minute;
- using fixed-direction event formations;
- introducing elites and persistent interval bosses; and
- applying explicit, legible encounter modifiers such as a material geode's resonance field.

An elite is a clearly presented enhanced version and may apply an authored multiplier or replacement profile. A distinct later-game variant counts as its own ordinary identity under DEC-106 rather than silently scaling an earlier identity.

Future modes, challenges, or difficulty settings may apply disclosed stage-wide modifiers through explicit later decisions. The standard baseline has no hidden scaling based on elapsed time, current equipment, resources held, Hull Integrity, account PowerUps, mining route, or perceived player strength.

## Status

Accepted as the standard ordinary-enemy scaling model. DEC-119 later supplies the initial base profiles, elite modifiers, encounter compositions, and minute-by-minute populations; exact values remain tuning work.

## Rationale

Fixed profiles make enemy appearances learnable: a player can recognize an alien and form a reliable expectation about its durability and threat. They also keep the deterministic wave table auditable because later difficulty comes from visible composition and density rather than an unseen multiplier.

The rule preserves room for geode resonance fields and elites because those changes are geographically, visually, or categorically signaled. It also supports production-efficient variants: when a variant becomes materially tougher, its distinct presentation and roster entry communicate that fact.

## Consequences

- Balance data should store one standard base profile per ordinary identity.
- Minute records select identities and quantities; they do not contain hidden per-minute stat inflation for those identities.
- Later reuse can create recognition and contrast without invalidating what the player learned earlier.
- Stronger late-run enemies require a distinct identity, an elite presentation, or another explicit modifier.
- Playtests should separately diagnose roster-profile problems and wave-composition problems.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Open Questions](../open-questions.md)

## Supersedes / superseded by

Completes the ordinary-enemy scaling rule left open by [DEC-098](./DEC-098-use-minute-authored-horde-waves.md), [DEC-105](./DEC-105-use-a-simple-pursuer-first-enemy-roster.md), and [DEC-106](./DEC-106-use-ten-ordinary-enemy-identities.md). It does not alter the explicit local resonance modifiers in [DEC-078](./DEC-078-geode-resonance-fields.md).
