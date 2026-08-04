---
doc_id: DEC-024
title: Use a Large Finite Map
status: accepted
authoritative: false
---

# DEC-024 — Use a Large Finite Map

## Decision

Each run takes place on a large, finite map. The traversable world has real bounds and does not wrap, repeat infinitely, or generate endlessly as the player travels.

[DEC-110](./DEC-110-use-open-multi-route-map-topology.md) later fixes the broad topology as mostly open, multi-route, and free of mandatory narrow chokepoints. [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) subsequently sets a 4:00–5:00 base-travel diameter, makes the boundary non-damaging and discoverable through fog, and completes the first-pass distribution contract. Exact boundary fiction and art remain open.

## Status

Accepted.

## Context

Exploration and route choice require enough space for deposits and other opportunities to be meaningfully separated. At the same time, a finite level supports a knowable resource ecology, exhausted deposits, meaningful abundance bands, and navigation toward remaining resources.

## Considered options

### Small bounded arena

This keeps action dense but leaves little room for search, routing, or distant mining commitments.

### Infinite or repeating world

This can sustain endless travel but weakens the meaning of finite resource availability and makes the level harder to mentally map.

### Large finite world

This provides meaningful exploration distances while preserving real boundaries and a finite set of run opportunities.

## Rationale

A large finite space best supports the intended tension between searching, committing time to travel, holding mining areas, and returning to unexplored regions. Its finite contents also make the geological survey and resource radar truthful descriptions of one level instance.

## Consequences

- Generation must create a bounded, traversable play space with a finite set of deposits and other world elements.
- The player can eventually reach a boundary; its visual, collision, and navigation treatment must be specified.
- Map scale must be large enough for exploration to matter within a 35-minute run without routinely making advertised resources unreachable.
- Radar behavior at boundaries and across the generated topology must remain legible.
- Exact boundary fiction, map reveal presentation, and any future fast-travel support remain open in OQ-008. Initial dimensions, topology, and boundary behavior are fixed by DEC-115.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [OQ-008 — How does exploration work?](../open-questions.md#oq-008--how-does-exploration-work)
- [OQ-024 — How is a highly randomized map constructed?](../open-questions.md#oq-024--how-is-a-highly-randomized-map-constructed)

## Supersedes / superseded by

Narrows the map bounds left open by [DEC-022](./DEC-022-randomized-map-locations.md). [DEC-110](./DEC-110-use-open-multi-route-map-topology.md) later completes the broad topology direction, and [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) fixes the first-pass scale and boundary behavior. Exact generation technique and boundary fiction remain open.
