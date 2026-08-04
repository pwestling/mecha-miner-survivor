---
doc_id: DEC-099
title: Use Single-Player Pause and Results Flow
status: accepted
authoritative: false
validation: prototype-and-usability-test
---

# DEC-099 — Use Single-Player Pause and Results Flow

> **Completion note:** DEC-127 fixes the shared run-console navigation, Status contents, four-page Results organization, hangar return, confirmations, and gamepad behavior left open here.

## Decision

The standard gameplay specification is single-player. Multiplayer is not part of the baseline run and requires a separate future mode and rules if added.

A pause command is available throughout active play. It freezes the complete simulation and opens a run-status surface showing the timer and phase, mech status, weapons and branches, utilities, relic, ordinary resources, unsecured Hyper Gold, relevant aggregate stats, and the explored map. It provides resume, settings, controls, and abandon-run actions. Fabrication remains its own unlimited fully paused interface and may expose the same status information.

The 35-minute timer advances only during active simulation. Manual pause, fabrication, relic resolution, required tutorial or confirmation screens, operating-system suspension, and automatic focus-loss pause where supported do not advance it. The non-modal opening survey remains the explicit exception: it appears while active simulation continues.

Death ends the run unless a separately acquired revival effect explicitly intercepts it. Abandoning from the pause menu requires confirmation and uses the same failure persistence rules as death. Reaching 35:00 succeeds immediately under the accepted extraction rule even if bosses remain alive.

Success or failure transitions to a results screen. At minimum it reports:

- outcome and active survival time;
- enemies and bosses defeated;
- final mech, weapons, branches, stat ranks, utilities, relic, and account PowerUps;
- damage contribution by weapon;
- mining points attempted and completed by class;
- ordinary resources collected, spent, and discarded;
- Hyper Gold collected, banked, or forfeited;
- explored-map share; and
- newly completed unlock conditions and content made available.

After results and unlock notifications, the player returns to the between-run hangar or main progression flow.

## Status

Accepted as the standard run-interruption, completion, failure, and results flow. Exact layouts and alternate modes remain open.

## Rationale

The reference game's standard single-player loop freely pauses and concludes with a statistical summary. Explicitly limiting the baseline to single-player removes speculative synchronization and shared-resource branches from every gameplay system.

Results must reflect this game's actual decisions: mining, fabrication, exploration, and survival-gated Hyper Gold matter as much as combat totals.

## Consequences

- Baseline mining, fabrication, relic, map, and timer rules do not require multiplayer edge cases.
- Abandoning cannot bank Hyper Gold or count as mission extraction.
- Unspent ordinary resources are shown before being discarded so the loss is legible.
- A future revival PowerUp or relic resumes the same run rather than creating a new run.
- Quitting the application without completing extraction must not convert unsecured Hyper Gold into banked currency.
- Platform-specific suspension behavior may save recovery state but cannot grant progress while simulation is stopped.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timer, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-003 — What is the complete run or session structure?](../open-questions.md#oq-003--what-is-the-complete-run-or-session-structure)

## Supersedes / superseded by

Completes standard pause and results behavior around [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md), [DEC-006](./DEC-006-paused-crafting-and-run-resource-reset.md), [DEC-007](./DEC-007-unlimited-on-demand-fabrication.md), and [DEC-010](./DEC-010-one-deployment-per-run.md).
