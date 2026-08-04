---
doc_id: DEC-115
title: Adopt the Standard Map-Generation Contract
status: accepted
authoritative: false
validation: generated-seed-audit-and-playtest
---

# DEC-115 — Adopt the Standard Map-Generation Contract

## Decision

Adopt the complete first-pass [Standard Map Generation Contract](../51-standard-map-generation-contract.md) as the player-facing baseline for standard maps.

The contract establishes:

- six to eight broad major regions, targeting seven;
- a 4:00–5:00 base-travel diameter and a maximum 2:30 deployment-to-important-site route;
- redundant looped topology with no connector whose removal isolates a major region;
- sparse 8–12% local collision coverage, broad connectors, and tightly bounded optional pockets;
- a safe randomized deployment area and a guaranteed choice of ordinary and specialized mining opportunities in the first 45 seconds of route distance;
- per-region distribution, distance-band, separation, clearance, and anti-clustering rules for every existing map reward class;
- three deliberately separated Hyper Gold sites and three deliberately separated relic caches;
- an initial baseline of sixteen non-enemy breakable objects, later changed by DEC-122/123 into a replenishing destructible-rock population capped at sixteen;
- one coherent primary biome per seed, distinct regional landmarks without fixed attached rewards, and independence among recognizable terrain and important content;
- a non-damaging, discoverable finite boundary and fogged world outline;
- no mandatory environmental-damage hazard in the initial standard-map baseline; and
- an explicit valid-seed contract plus softer playtest targets.

Base-travel time is the standard distance unit: the shared unmodified mech's uninterrupted travel time along the shortest valid route. This keeps spatial requirements meaningful if implementation scale changes.

## Status

Accepted as the complete first-pass generation baseline. Values explicitly labeled initial targets remain open to playtest revision, but a valid standard map must satisfy the contract until a later decision changes it.

## Rationale

The consequential topology choices were already accepted: large finite worlds, randomized important locations, recurring authored structures in new arrangements, and open multi-route traversal. The remaining questions are chiefly fairness and tuning defaults. A concrete baseline is more useful to prototyping agents than leaving every spacing and density variable unspecified.

Expressing scale as travel time and connector size relative to mining zones avoids prematurely selecting engine units. Separating hard seed validity from softer experience targets permits iteration without quietly admitting unreachable resources, bad spawn areas, resonance overlap, mandatory chokepoints, or severe clustering.

## Consequences

- Map prototypes do not require further design approval before selecting a generation algorithm.
- Implementation may use chunks, graphs, fields, stamps, or another method if the player-visible contract is preserved.
- Generated-seed review must cover the full four-material profile space and all six signature mechs.
- Map size, obstacle density, spacing, and container count may change together when playtests reveal pacing problems.
- OQ-024 is resolved; minimap interaction, exact world signals, visual biome content, and mining-zone radii remain separate open work.

## Specification links

- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Open Questions](../open-questions.md)
- [DEC-022 — Randomize all player-relevant map locations](./DEC-022-randomized-map-locations.md)
- [DEC-024 — Use a large finite map](./DEC-024-large-finite-map.md)
- [DEC-026 — Recombine recurring authored map chunks](./DEC-026-recombined-authored-map-chunks.md)
- [DEC-110 — Use an open multi-route map topology](./DEC-110-use-open-multi-route-map-topology.md)

## Supersedes / superseded by

Completes the initial dimensions, region structure, spacing, distribution, deployment, boundary, landmark, repetition, and validation variables left open by DEC-022, DEC-024, DEC-026, DEC-030, DEC-081, DEC-090, and DEC-110. It does not select a technical generation algorithm.
