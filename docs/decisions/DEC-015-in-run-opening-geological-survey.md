---
doc_id: DEC-015
title: Reveal Randomized Geology During the Active Opening
status: accepted
authoritative: false
---

# DEC-015 — Reveal Randomized Geology During the Active Opening

> **Completion note:** DEC-127 fixes the opening survey layout, duration, automatic collapse, signature-branch markings, and input behavior left open here.

## Decision

The randomized resource profile is unavailable before deployment. The player selects a mech without seeing the level's specialized-resource types or abundance bands.

When the player deploys, the 35-minute timer, gameplay simulation, and intentionally minor opening enemy waves begin. The geological survey then becomes available and reveals every present specialized resource type and its abundance. Its initial presentation does not pause gameplay. Exact deposit locations remain hidden.

Every playable mech must remain viable on every resource profile valid for that mech. DEC-036 constrains generation to profiles containing at least two of the selected signature weapon's three branch-resource colors. Randomized geology can influence the player's early build plan and make particular options more or less economical, but cannot make the already-selected mech fundamentally unsuitable for the run.

## Status

Accepted.

## Context

The earlier model revealed randomized geology before deployment so the player could plan a build and mech choice around it. The game owner prefers the map information to become a quick live assessment while the first very minor waves are already approaching. Mech traits should create different playstyles without becoming compatibility requirements for particular geological rolls.

## Considered options

### Reveal geology before mech selection

This maximizes planning information but encourages choosing a mech as an answer to the roll and moves adaptation outside the run.

### Hide all resource information until deposits are discovered

This maximizes exploration uncertainty but can make recipe planning arbitrary and leave the player unable to form an informed early build direction.

### Reveal geology during the active opening

This preserves the survey's planning value while turning comprehension and adaptation into the first live task of the run.

## Rationale

The opening reveal creates immediate engagement without demanding difficult combat and reading at the same time. Selecting the mech first keeps mech identity independent of map luck. The universal-viability rule lets geology produce preferences and constraints rather than hard counters.

## Consequences

- The pre-deployment flow must not expose randomized specialized-resource presence or abundance.
- The opening waves must be threatening enough that the run has begun but minor enough that the survey can be read quickly.
- The initial survey presentation cannot pause the timer, enemies, or simulation.
- Survey information must be concise, readable under motion, and understandable without relying solely on color.
- All mechs and signature weapons require viable development paths on every generated profile; at least two signature branch colors are guaranteed by DEC-036.
- [DEC-016](./DEC-016-one-minute-opening-orientation.md) sets the opening phase to one active minute. [DEC-017](./DEC-017-persistent-survey-review.md) makes the initial display automatic and non-modal, then keeps the survey available through fabrication. Wave composition, exact layout, dismissal, and input-specific behavior remain open in OQ-022.
- Restart behavior after seeing the survey must avoid making immediate restarts the optimal way to fish for preferred geology.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-021 — What is the pre-deployment selection order?](../open-questions.md#oq-021--what-is-the-pre-deployment-selection-order)
- [OQ-022 — How does the opening survey phase work?](../open-questions.md#oq-022--how-does-the-opening-survey-phase-work)
- [DEC-036 — Use six-color signature-aware resource profiles](./DEC-036-six-color-signature-aware-resource-profiles.md)

## Supersedes / superseded by

Supersedes only the pre-deployment reveal timing in [DEC-008](./DEC-008-fixed-blueprints-randomized-resource-profiles.md). Fixed fabrication rules, randomized profiles, disclosed resource types and abundance bands, and hidden exact deposits remain accepted. [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md) later conditions the hidden profile on the selected signature weapon without changing reveal timing.
