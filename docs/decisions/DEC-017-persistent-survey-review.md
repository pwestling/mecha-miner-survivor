---
doc_id: DEC-017
title: Keep the Survey Reviewable Through Fabrication
status: accepted
authoritative: false
---

# DEC-017 — Keep the Survey Reviewable Through Fabrication

> **Completion note:** DEC-127 fixes the survey's 0.5-second appearance, 12-active-second expanded duration, HUD placement, Fabrication entry behavior, and controller presentation left open here.

## Decision

At deployment, the geological survey appears automatically as a compact, non-modal display. Its initial presentation does not pause the simulation, halt the timer, or take movement control from the player.

The complete survey remains reviewable throughout the run from the fabrication interface. Opening that interface freezes the full gameplay simulation under the existing fabrication rule.

## Status

Accepted.

## Context

The survey contains run-defining information but first appears while active enemies are approaching. A player may miss or forget part of it, especially while learning the resource vocabulary or handling accessibility needs. The information should remain recoverable without adding a separate pause system.

## Considered options

### Opening display only

This emphasizes quick comprehension but makes a brief distraction or memory lapse permanently costly.

### Permanently pinned full survey

This guarantees visibility but consumes combat HUD space for information that is not always needed.

### Compact opening display with fabrication review

This preserves the active opening while using an existing paused planning interface for detailed later reference.

## Rationale

Automatic non-modal presentation ensures every player receives the information without interrupting the live opening. Persistent fabrication access prevents memory from becoming an unintended difficulty test. Reusing fabrication keeps all detailed build-planning information in one place and introduces no additional pausing privilege.

## Consequences

- The opening survey must be readable without capturing movement input.
- The fabrication interface requires a persistent survey view or section.
- The fabrication survey must show the same resource types and abundance bands revealed at deployment.
- Reviewing the survey through fabrication freezes the simulation exactly like any other fabrication activity.
- Exact opening layout, duration or dismissal, information density, controller or touch behavior, and accessibility presentation remain open in OQ-022.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-022 — How does the opening survey phase work?](../open-questions.md#oq-022--how-does-the-opening-survey-phase-work)

## Supersedes / superseded by

Resolves the survey persistence and later-review behavior left open by [DEC-015](./DEC-015-in-run-opening-geological-survey.md) and [DEC-016](./DEC-016-one-minute-opening-orientation.md). No earlier accepted behavior is reversed.
