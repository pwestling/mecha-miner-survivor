---
doc_id: DEC-010
title: Make One Timed Deployment One Complete Run
status: accepted
authoritative: false
---

# DEC-010 — Make One Timed Deployment One Complete Run

## Decision

One timed deployment into one level is one complete run. Reaching mission extraction is the successful culmination of that run's build and immediately ends the run. The player does not continue into another level with the same ordinary resources, weapons, or run-local upgrades. Death before extraction also ends the run.

## Status

Accepted.

## Context

Earlier decisions established timed level completion, run-local crafting, extraction-gated banking of rare resources, and loss of unspent ordinary resources at run end. It remained unclear whether mission extraction ended the full attempt or only advanced the player into another level while preserving the build.

## Considered options

### Multi-level run

A sequence of levels could create a longer campaign arc and let builds continue developing, but it would require intermediate settlement rules, multiple power curves, and a distinction between level extraction and final run extraction.

### One level per run

A single timed deployment gives each build one complete arc and makes mission extraction, resource settlement, and replay boundaries unambiguous.

## Rationale

The single-level structure keeps the run focused on exploring one resource profile, constructing one build, and testing it against one escalating survival timeline. Extraction becomes a meaningful conclusion rather than an intermission, and all resource-persistence rules align at one obvious endpoint.

## Consequences

- The level timer is also the run timer.
- The run's full power curve must fit within one level.
- Mission extraction banks rare cross-run resources, discards unspent ordinary resources, and retires the completed run-local build.
- Death forfeits unsecured rare resources and retires the failed run-local build.
- There is no between-level continuation screen, ordinary-resource carryover, or multi-level escalation within a standard run.
- The post-run flow can present results, persistent progression, and selection of a new deployment, but it begins outside the completed run.
- Run duration and interval-boss cadence become especially important because they must support the entire build arc.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-003 — What is the complete run or session structure?](../open-questions.md#oq-003--what-is-the-complete-run-or-session-structure)

## Supersedes / superseded by

Clarifies the run boundary left open by [DEC-005](./DEC-005-timed-survival-and-mission-extraction.md); it does not reverse that decision.
