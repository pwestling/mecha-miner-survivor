---
doc_id: DEC-026
title: Recombine Recurring Authored Map Chunks
status: accepted
authoritative: false
---

# DEC-026 — Recombine Recurring Authored Map Chunks

## Decision

Map generation may reuse authored terrain chunks or recurring biome structures. Their arrangement and every important player-relevant location change between runs, so recurrence does not create a fixed route, reward coordinate, or level layout.

## Status

Accepted.

## Context

The map must vary significantly without requiring every terrain detail to be generated from unconstrained rules. Reusable authored structures can support visual quality, navigational clarity, content production, and reliable traversal when their higher-level composition remains variable.

## Considered options

### Entirely unique unconstrained generation

This maximizes theoretical variation but makes visual composition, traversal guarantees, and content authoring harder to control.

### Fixed map templates

This provides reliable layouts but lets players memorize routes and important spatial relationships.

### Recurring chunks in randomized compositions

This preserves authored local quality while allowing topology, adjacency, and important content placement to vary from run to run.

## Rationale

Players may recognize a local terrain structure without knowing where it is in the world, what connects to it, or what valuable content it contains this run. This preserves useful recognition while keeping exploration and routing adaptive.

## Consequences

- A recurring chunk cannot guarantee the same deposit, relic, landmark reward, hazard, deployment point, or other important content at the same local position every run.
- Chunk combinations must preserve traversability, spacing, radar routes, and boundary clarity.
- The generator should vary arrangement, connections, and relevant content enough that recognizing a chunk does not solve the route.
- DEC-110 later requires mostly open major regions, wide redundant connections, sparse collision obstacles, and validated optional dead ends. DEC-115 then limits recurring structures to two nonadjacent appearances, independently randomizes important content, and fixes the broader player-facing generation contract. Exact technical transformations and biome art rules remain future production work.

## Specification links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [OQ-024 — How is a highly randomized map constructed?](../open-questions.md#oq-024--how-is-a-highly-randomized-map-constructed)
- [DEC-115 — Adopt the standard map-generation contract](./DEC-115-adopt-standard-map-generation-contract.md)

## Supersedes / superseded by

Narrows the generation methods left open by [DEC-022](./DEC-022-randomized-map-locations.md) and [DEC-024](./DEC-024-large-finite-map.md). [DEC-110](./DEC-110-use-open-multi-route-map-topology.md) later chooses the broad topology, and [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) completes its first-pass player-facing generation rules. The technical generation algorithm remains open.
