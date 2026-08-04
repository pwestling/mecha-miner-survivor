---
doc_id: DEC-033
title: Use a Fogged Exploration Map with Persistent Discovery Markers
status: accepted
authoritative: false
---

# DEC-033 — Use a Fogged Exploration Map with Persistent Discovery Markers

## Decision

Active play includes a compact minimap covered by exploration fog. Nearby terrain is revealed as the mech travels. Once observed, deposits, depleted deposits, landmarks, and opened or unopened relic caches remain recorded with distinct markers.

Undiscovered terrain, deposits, and relic caches remain hidden. The player can review a larger version of the explored map through the fabrication interface, using fabrication's normal complete-simulation pause.

## Status

Accepted.

## Context

The world is large, finite, and highly randomized. Exploration requires incomplete information, but the player should not need to memorize every discovered route or resource location while managing horde pressure.

## Considered options

### No map

This maximizes navigation pressure but places excessive memory burden on a long run with randomized deposits.

### Fully revealed map

This aids planning but removes terrain discovery and can turn exploration into waypoint execution.

### Fogged persistent-discovery map

This preserves the unknown while letting the player's accumulated spatial knowledge remain useful.

## Rationale

Fog protects discovery, persistent markers support intentional routing, and the paused large map creates a planning view without obscuring active combat. The compact minimap helps orientation without revealing resource locations the player has not earned.

## Consequences

- The minimap must distinguish unexplored, explored, traversable, and blocked space without relying only on color.
- Discovered active, depleted, and rare deposits require distinguishable persistent states.
- A relic cache seen within the discovery rules remains marked even if the player routes around it unopened.
- Opened caches remain marked in a completed state.
- Radar direction may complement the map but cannot reveal an undiscovered exact deposit marker.
- Exact reveal radius, line-of-sight behavior, map orientation, zoom, marker filters, boundary display, and waypoint support remain open in OQ-008.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [OQ-008 — How does exploration work?](../open-questions.md#oq-008--how-does-exploration-work)

## Supersedes / superseded by

Narrows the navigation and visibility questions left open by [DEC-022](./DEC-022-randomized-map-locations.md) and [DEC-024](./DEC-024-large-finite-map.md).
