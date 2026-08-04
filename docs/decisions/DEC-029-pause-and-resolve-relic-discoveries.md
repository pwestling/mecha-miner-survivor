---
doc_id: DEC-029
title: Pause and Resolve Relic Discoveries Through Installation or Common-Ore Sale
status: accepted
authoritative: false
---

# DEC-029 — Pause and Resolve Relic Discoveries Through Installation or Common-Ore Sale

> **Completion note:** DEC-127 fixes the exact 150-ore sale, side-by-side compatibility comparison, confirmation flow, and replacement presentation left open here.

## Decision

Discovering a mech relic freezes the entire gameplay simulation while the player reviews and resolves it.

- Selling the new relic awards common basic ore and retains the currently installed relic.
- Installing the new relic activates it. If another relic was installed, that displaced relic is automatically sold for its displayed common-ore value.

This creates the first explicit non-mining source of common ore and supersedes the absolute mining-exclusivity rule in DEC-020. Ordinary enemies still never drop ordinary crafting materials. DEC-111 later adds boss-loot exceptions for common ore and present-profile specialized resources.

## Status

Accepted.

## Context

A relic can significantly transform the entire build, so evaluating it during active horde pressure would force rushed or unreadable decisions. Replacement also needs a clear treatment for the old relic, and the sale reward must be universally useful without bypassing the randomized specialized-resource profile.

## Considered options

### Keep simulation active

This preserves uninterrupted action but conflicts with the amount of comparison required for a run-defining effect.

### Pause the complete simulation

This matches fabrication's decision-space rule and lets the player inspect all affected weapons safely.

### Destroy a displaced relic

This creates severe replacement commitment but wastes an exploration reward and makes experimentation punitive.

### Automatically sell a displaced relic

This turns replacement into a clean comparison: keep the old effect and sell the new relic, or install the new effect and receive the old relic's sale value.

### Pay specialized resources

This could rescue a planned branch but would bypass the map's randomized specialized-resource ecology.

### Pay common basic ore

This is always useful, supports any weapon build, and does not supply a missing specialized branch material.

## Rationale

The paused comparison protects comprehension and accessibility. Common ore makes every relic find economically useful, while automatic sale removes unexplained destruction without eliminating the meaningful choice of which one relic effect remains active.

## Consequences

- The level timer, enemies, spawning, attacks, projectiles, cooldowns, mining progress and decay, threats, hazards, timed effects, pickups, and physics do not advance during relic resolution.
- The choice interface must show the new relic's effect, all currently affected equipment, the installed relic, and both relevant common-ore sale values.
- The player never loses a displaced relic without value: replacing it converts it to common ore automatically.
- Selling the new relic leaves the installed relic unchanged.
- Relic sales are a finite exploration reward rather than a repeatable passive combat-income stream.
- Common ore can now enter the inventory through mining or relic sale. Specialized ordinary materials remain exclusive to mining.
- Exact common-ore sale values and scaling remain open in OQ-027.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Mech Relics](../67-mech-relics.md)
- [OQ-027 — How are mech relic discoveries presented and resolved?](../open-questions.md#oq-027--how-are-mech-relic-discoveries-presented-and-resolved)

## Supersedes / superseded by

Extends [DEC-028](./DEC-028-one-exploration-found-mech-relic.md) and supersedes [DEC-020](./DEC-020-mining-exclusive-ordinary-materials.md) only where DEC-020 claimed mining was the sole way common ore could enter the run inventory. The prohibition on ordinary enemy crafting-material drops remains accepted.
