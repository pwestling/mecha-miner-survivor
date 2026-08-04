---
doc_id: DEC-110
title: Use an Open Multi-Route Map Topology
status: accepted
authoritative: false
validation: procedural-generation-and-playtest
---

# DEC-110 — Use an Open Multi-Route Map Topology

## Decision

Standard maps use a **mostly open, multi-route topology** assembled from randomized authored terrain chunks or biome structures.

### Major regions and connections

- The map contains broad traversable regions suitable for survivor-like horde combat, circling a mining point, and changing approach direction.
- Major neighboring regions connect through multiple wide routes rather than one mandatory passage wherever the world boundary permits.
- The generated major-region graph cannot depend on a single narrow bridge or corridor whose blockage separates a substantial portion of the map.
- Connections must be wide enough for two-way travel, ordinary horde flow, and meaningful lateral dodging. Exact world-unit widths remain playtest variables.

### Solid terrain

- Solid obstacles are sparse and locally meaningful rather than maze-forming.
- Obstacles may shape movement, break up sightlines, mark landmarks, or create short positional choices, but cannot form long compulsory funnels that dictate one route through the map.
- No required route uses a narrow mandatory chokepoint.
- A mining zone cannot overlap solid terrain or be placed where nearby collision geometry removes the maneuvering space its risk model assumes.

### Optional spurs and dead ends

Optional side pockets and dead ends are allowed as exploration choices. They may contain deposits, relic caches, containers, landmarks, or other optional opportunities, but must provide a clearly readable exit and enough open space at the destination for the mech to turn, fight, and mine where applicable. A dead end cannot be the only connector between major regions.

The map should not become a featureless empty plane. Authored landmarks, terrain clusters, region boundaries, and optional pockets provide spatial identity within these traversal constraints.

## Status

Accepted as a standard-map topology and traversability invariant. DEC-115 later fixes the initial scale, region-count range, connection widths, obstacle-density targets, repetition limit, placement contract, and validation thresholds. Exact chunk library, biome composition, boundary fiction, and technical generation method remain production work.

## Rationale

The game asks the player to explore while hundreds of simple pursuers maintain pressure and to spend extended periods circling within mining areas. Labyrinthine terrain or mandatory narrow corridors would make map generation dominate weapon effectiveness, cause enemies to pile up unpredictably, and turn some randomized deposit placements into traps rather than informed risks.

Broad spaces and redundant routes preserve the readable horde flow of the genre reference while still allowing terrain to affect navigation and local positioning. Optional pockets create route commitments and discoveries without making the whole world a corridor puzzle.

## Consequences

- Procedural validation must evaluate the region graph, chokepoints, route redundancy, mining-zone clearance, offscreen spawn ground, and dead-end combat space.
- Chunks are authored with generous connection apertures and explicit obstacle-free placement envelopes around eligible mining sockets.
- A visually attractive generation result is invalid if one narrow connection controls access to a major part of the map.
- Terrain may create local advantage or danger, but it cannot routinely invalidate radial, orbiting, backward-firing, or movement-dependent weapon patterns.
- Exact generation algorithms can be chosen later without revisiting the player-facing topology promise.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [DEC-115 — Adopt the standard map-generation contract](./DEC-115-adopt-standard-map-generation-contract.md)

## Supersedes / superseded by

Completes the broad topology direction left open by [DEC-024](./DEC-024-large-finite-map.md) and [DEC-026](./DEC-026-recombined-authored-map-chunks.md). [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) later supplies first-pass dimensions and placement rules without selecting a generation algorithm.
