---
doc_id: DEC-101
title: Target an Approachable Escalating Standard Difficulty
status: accepted
authoritative: false
validation: playtest
---

# DEC-101 — Target an Approachable, Escalating Standard Difficulty

> **Completion note:** DEC-126 supplies the initial milestone-reach and extraction-rate bands, two-hit survival guarantee, and damage fairness thresholds used to validate this difficulty intent. Alternate modes and accessibility assists remain open.

## Decision

The standard mode targets the core *Vampire Survivors* difficulty experience: immediately understandable low-input play, meaningful movement and build decisions, rapidly escalating horde pressure, and a late-run power fantasy earned by developing a functional build.

A fresh save with no account PowerUps must have a plausible path to successful mission extraction using the initially available mech and content. Permanent PowerUps improve consistency and expand strategic latitude but are not required to make the standard run mathematically viable.

The standard mode is not dynamically adjusted to guarantee success. The authored time schedule does not secretly weaken enemies because the player's build is poor, health is low, or mining was inefficient. Failure is an expected part of learning and metaprogression, but unavoidable losses caused solely by a valid resource profile or unreachable generation are defects.

The first minute is deliberately forgiving, early waves leave room to learn and mine, middle phases pressure route and build choices, and the final seven-minute crescendo demands a mature build. A strong build should visibly overwhelm large enemy numbers without eliminating the need to move or respect major threats.

## Status

Accepted as the intended standard-mode audience and difficulty envelope. DEC-112 later establishes that permanent PowerUps substantially ease early play but remain weaker than a developed run build. Exact numbers, accessibility assists, and later challenge modes remain open.

## Rationale

The reference succeeds by being simple to control without being passive. This game's extra navigation and mining decisions increase cognitive load, so standard combat should remain legible while still punishing weak positioning and underdevelopment.

Fresh-save viability ensures metaprogression feels empowering rather than compulsory. Deterministic waves make improvement and failure understandable and support meaningful route planning.

## Consequences

- Playtests must separately cover fresh profiles, partially upgraded profiles, and highly upgraded profiles.
- A fresh-profile win need not be likely for a first-time player, but it must be mechanically possible without hidden unlocks.
- Standard generation validation rejects unreachable required content and profiles with no plausible survival route.
- Difficulty accessibility can include readable telegraphs, effect-opacity controls, damage numbers, and other assists without changing the core schedule.
- Optional challenge, hyper, endless, or custom modes require later decisions and do not alter the standard baseline.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timer, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [OQ-012 — Who is the intended player, and what difficulty experience should the game provide?](../open-questions.md#oq-012--who-is-the-intended-player-and-what-difficulty-experience-should-the-game-provide)

## Supersedes / superseded by

Resolves the baseline audience and difficulty intent under the expanded reference rule in DEC-096. [DEC-112](./DEC-112-bound-permanent-power-below-run-build-power.md) later completes the account-progression relationship. Neither decision sets numeric balance values.
