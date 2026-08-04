---
doc_id: DEC-022
title: Randomize All Player-Relevant Map Locations
status: accepted
authoritative: false
---

# DEC-022 — Randomize All Player-Relevant Map Locations

## Decision

Every run significantly randomizes its map layout and the positions of all player-relevant world elements. No meaningful location is fixed between runs. A map may guarantee that particular elements appear, but their locations are randomized.

The exact construction technique—authored templates, modular chunks, procedural terrain rules, or a combination—remains open. The required player-visible outcome is substantial spatial variation without fixed important coordinates.

## Status

Accepted.

## Context

Exploration and resource routing are core run decisions. Fixed deposit locations or stable important landmarks would allow memorized routes to replace adaptation, especially after the geological survey reveals the run's resource types.

## Considered options

### Authored fixed map with randomized deposits

This offers strong composition and readability but lets players optimize traversal around a permanently known topology.

### Fixed templates with randomized contents

This increases variation while preserving recognizable structures, but may still become predictable if template count is low.

### Significantly randomized topology and locations

This makes navigation and discovery meaningfully run-specific while still permitting authored rules and guaranteed content.

## Rationale

Spatial uncertainty reinforces the game's exploration identity and prevents the resource profile from reducing a run to a memorized route. Keeping construction technique open allows later prototyping to find a reliable balance between novelty, legibility, and fairness.

## Consequences

- Mining points, rare opportunities, landmarks, hazards, required features, and deployment locations cannot rely on fixed coordinates across runs.
- Guaranteed elements require placement rules rather than authored permanent locations.
- Generation must protect reachability, useful spacing, navigation clarity, and viable access before boss thresholds.
- Radar and other navigation systems must operate correctly on every generated arrangement.
- The map is large and finite under DEC-024 and may recombine recurring authored chunks under DEC-026. DEC-110 and DEC-115 later resolve initial dimensions, topology, repetition, connectivity, boundary behavior, distribution, and validation. Exact technical construction and biome art rules remain future work.
- Playtesting must measure recognizable repetition as well as technically different layouts.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [OQ-008 — How does exploration work?](../open-questions.md#oq-008--how-does-exploration-work)
- [OQ-024 — How is a highly randomized map constructed?](../open-questions.md#oq-024--how-is-a-highly-randomized-map-constructed)
- [DEC-024 — Use a large finite map](./DEC-024-large-finite-map.md)
- [DEC-026 — Recombine recurring authored map chunks](./DEC-026-recombined-authored-map-chunks.md)
- [DEC-033 — Use a fogged exploration map with persistent discovery markers](./DEC-033-fogged-exploration-map.md)
- [DEC-115 — Adopt the standard map-generation contract](./DEC-115-adopt-standard-map-generation-contract.md)

## Supersedes / superseded by

Rejects a fixed-location interpretation of the map rules. [DEC-024](./DEC-024-large-finite-map.md) later establishes that the generated play space is large and finite, [DEC-026](./DEC-026-recombined-authored-map-chunks.md) permits recurring authored chunks within randomized compositions, [DEC-033](./DEC-033-fogged-exploration-map.md) defines how player discovery is recorded, and [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) completes the initial player-facing generation contract.
