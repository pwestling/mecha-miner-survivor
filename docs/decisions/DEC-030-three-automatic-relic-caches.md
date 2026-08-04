---
doc_id: DEC-030
title: Place Three Automatic Relic Caches on Each Standard Map
status: accepted
authoritative: false
---

# DEC-030 — Place Three Automatic Relic Caches on Each Standard Map

> **Completion note:** DEC-127 fixes distinct without-replacement contents, no dedicated guards, in-view signaling, mapping after observation, and the 150-ore sale value left open here.

## Decision

Every standard map contains exactly three relic caches at randomized locations. A cache is a clearly recognizable world object that opens automatically when the mech touches it, preserving movement-only gameplay controls.

Opening a cache immediately enters the fully paused relic-resolution interface. The player must install the relic or sell it for common ore before returning to active play; there is no relic inventory, deferred decision, or unresolved relic carried away from the cache.

The map guarantees the three caches exist, but does not guarantee the player will discover or reach all three during the 35-minute run.

## Status

Accepted.

## Context

Relics need a concrete discovery form, enough opportunities for the one-slot replacement rule to matter, and an interaction compatible with movement-only controls. Deferral or an unequipped inventory would add another loadout-management layer and weaken the immediate exploration decision.

## Considered options

### Manual interaction button

This prevents accidental activation but adds a context action to the movement-only baseline.

### Automatic contact activation

This keeps controls simple and is avoidable if the cache has a distinctive silhouette and approach space.

### Variable or unbounded cache count

This increases uncertainty but makes it harder to tune exploration value and replacement frequency.

### Three guaranteed caches

This supports an initial install and up to two later replace-or-sell decisions without guaranteeing that every player finds every opportunity.

### Deferred relic inventory

This permits later comparison but lets players postpone the intended immediate commitment and adds inventory handling.

### Immediate resolution

This keeps the reward legible: touching a cache produces one consequential install-or-sell decision before play resumes.

## Rationale

Three caches give the system a reliable presence in each run while the large randomized map preserves discovery uncertainty. Automatic contact respects the control model, and immediate resolution keeps the single-slot system free of inventory bookkeeping.

## Consequences

- Cache silhouettes, effects, and approach signaling must be unmistakable from the fully top-down wide camera.
- A player must be able to route around a visible cache to avoid triggering it before they are ready.
- Cache positions obey all randomized-location and reachability rules.
- Generation must place exactly three valid caches on every standard map.
- Touching a cache freezes the complete simulation before the relic choice appears.
- The player cannot close the choice and retain the relic for later; installation or sale is required to resume.
- DEC-115 later fixes initial cache spacing and region/distance distribution. Exact guarding, discovery signaling, relic selection pool, duplicate rules, and sale values remain open in OQ-027.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Mech Relics](../67-mech-relics.md)
- [OQ-027 — How are mech relic discoveries presented and resolved?](../open-questions.md#oq-027--how-are-mech-relic-discoveries-presented-and-resolved)

## Supersedes / superseded by

Narrows the discovery-form, quantity, activation, and deferral questions left open by [DEC-028](./DEC-028-one-exploration-found-mech-relic.md) and [DEC-029](./DEC-029-pause-and-resolve-relic-discoveries.md).
