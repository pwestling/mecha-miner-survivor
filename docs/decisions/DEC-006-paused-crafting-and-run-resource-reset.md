---
doc_id: DEC-006
title: Pause Combat for Crafting and Discard Unspent Ordinary Resources
status: accepted
authoritative: false
---

# DEC-006 — Pause Combat for Crafting and Discard Unspent Ordinary Resources

## Decision

Crafting and upgrading pause gameplay and function as intentional breaks in the action. This decision originally left their access trigger and full pause boundary open; [DEC-007](./DEC-007-unlimited-on-demand-fabrication.md) selects unlimited on-demand access and a complete simulation freeze. Unspent ordinary resources are discarded when the run ends.

## Status

Accepted.

## Context

Crafting requires more deliberate comparison than a reflex action, and the game needs relief from uninterrupted horde pressure. Ordinary resources are collected throughout a run and may be saved for later crafting, but their relationship to cross-run progression must remain clear.

## Considered options

### Keep combat active during crafting

This would preserve continuous pressure but force rushed menu decisions, increase accessibility burdens, and prevent crafting from serving as a rest beat.

### Pause combat during crafting

This separates build planning from movement execution and gives the player a deliberate break.

### Convert unspent ordinary resources at run end

Conversion would reduce waste but blur the separation between ordinary run progression and rare cross-run progression.

### Discard unspent ordinary resources

Discarding preserves the run-local economy and encourages spending before mission extraction.

## Rationale

Paused crafting makes intentional recipes and upgrades readable without compromising survival controls. Losing leftovers keeps ordinary resources focused on the current build and protects rare resources as the dedicated cross-run currency.

## Consequences

- Crafting access must occur often enough that players have a fair opportunity to spend mined resources and grow stronger before each interval boss.
- The UI must warn players before the final spending opportunity or clearly communicate that leftovers will be lost.
- [DEC-007](./DEC-007-unlimited-on-demand-fabrication.md) freezes the level timer and the rest of the gameplay simulation with combat.
- Fabrication access must allow pre-boss growth; [DEC-007](./DEC-007-unlimited-on-demand-fabrication.md) provides unrestricted access and defines relevant playtest risks.
- No ordinary-resource conversion occurs at success or failure unless a later explicit exception is adopted.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [RES-003 — Crafting-break cadence](../research/RES-003-crafting-break-cadence.md)

## Supersedes / superseded by

Extended by [DEC-007](./DEC-007-unlimited-on-demand-fabrication.md), which selects unrestricted player-invoked access.
