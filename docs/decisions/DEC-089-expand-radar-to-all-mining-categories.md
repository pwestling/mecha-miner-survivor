---
doc_id: DEC-089
title: Expand the Radar to All Mining Categories
status: accepted
authoritative: false
validation: playtest
---

# DEC-089 — Expand the Radar to All Mining Categories

> **Completion note:** DEC-127 fixes the seven-bearing active-play layout, overlap fanning and clustering, and exhaustion feedback left open here.

## Decision

While the resource radar is installed, it continuously tracks one nearest remaining target in each of seven mining categories:

- The nearest unopened geode of each of the four specialized materials present in the run.
- The nearest nondepleted standard ore seam.
- The nearest nondepleted rich ore seam.
- The nearest incomplete Hyper Gold site, as named by DEC-091.

Each off-screen target produces a directional indicator at the corresponding edge of the active-play screen. The radar therefore can show up to seven directions at once. It does not show one indicator per deposit; each specialized material or mining-point class contributes at most its single nearest valid target.

Indicators update continuously as the mech moves. Opening a geode, depleting an ore seam, or completing a Hyper Gold site immediately retargets that category to its next-nearest valid site. If a category has no remaining target, the radar reports it as exhausted and emits no false direction.

The radar requires no manual selection, retargeting command, or fabrication pause after installation. It provides direction only: no distance, exact waypoint, hidden-terrain reveal, or undiscovered map marker. Relic caches and non-mining discoveries remain outside its scope.

## Status

Accepted as the initial playtest target coverage. Exact indicator layout remains open to interface design and playtesting.

## Rationale

The radar is a 300-ore, one-utility-slot investment in navigation. Comprehensive mining guidance makes that investment broadly useful even after a desired weapon recipe has been found, while retaining the exploration game because players receive bearings rather than positions or distances.

Tracking only the nearest target in each category prevents the map's many deposits from producing one indicator each. Seven simultaneous directions are a deliberate information load: the interface must make the four material identities and three mining-point classes distinguishable and must handle similar bearings cleanly.

## Consequences

- The maximum active directional count is seven: four specialized-material geodes plus one standard seam, one rich seam, and one Hyper Gold site.
- A category's nearest target may be undiscovered; radar guidance alone does not add it to the explored map.
- When a tracked target is on-screen, ordinary world presentation can replace its off-screen edge indicator.
- Specialized-material indicators reuse each material's established icon, shape, label, and color language.
- Standard seams, rich seams, and Hyper Gold sites require distinct non-color class icons and compact labels.
- Indicators on similar bearings must stack or separate legibly without merging their identities.
- DEC-091 later names the tracked category Hyper Gold and fixes three sites per map; the indicator tracks the nearest incomplete site.
- Comprehensive targeting increases the risk that the radar becomes a universal purchase or turns exploration into bearing-following, so price, utility-slot opportunity cost, directional precision, and presentation require playtesting.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)
- [OQ-019 — How does the resource radar work?](../open-questions.md#oq-019--how-does-the-resource-radar-work)

## Supersedes / superseded by

Supersedes the four-indicator maximum and the standard-seam, rich-seam, and super-resource exclusions in [DEC-088](./DEC-088-show-continuous-multi-material-radar-directions.md). It preserves DEC-088's continuous active-play display, nearest-target-per-category rule, automatic retargeting, and direction-only output. It also supersedes the special-resource exclusion in [DEC-009](./DEC-009-ore-powered-directional-resource-radar.md) and the target restrictions preserved by [DEC-087](./DEC-087-price-resource-radar-at-three-hundred-ore.md).
