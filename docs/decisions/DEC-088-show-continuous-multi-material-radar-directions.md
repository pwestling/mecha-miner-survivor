---
doc_id: DEC-088
title: Show Continuous Multi-Material Radar Directions
status: superseded
authoritative: false
validation: playtest
---

# DEC-088 — Show Continuous Multi-Material Radar Directions

> **Superseded scope:** DEC-089 expands the maximum from four to seven and adds standard ore, rich ore, and super-resource targets. The decision below records the earlier rule.

## Decision

While the resource radar is installed, the active-play HUD continuously tracks the nearest remaining unopened geode of each of the four specialized materials present in the run. For every tracked geode that is off-screen, a directional indicator appears at the corresponding edge of the game screen.

The result is at most four directional indicators, not one indicator for every geode. Each uses the tracked material's icon, name or abbreviation where space permits, shape, and color language. Indicators update continuously as the mech moves and immediately select the next-nearest geode of that material when the current target opens.

The radar requires no manual target selection, retargeting command, or fabrication pause after installation. It provides direction only: no distance, exact waypoint, hidden-terrain reveal, or undiscovered map marker. It does not track standard seams, rich seams, super-resource sites, relic caches, or other undiscovered content.

## Status

Accepted as the initial playtest radar presentation and targeting behavior, then partially superseded by DEC-089's broader target coverage.

## Rationale

A 300-ore, one-slot navigation investment should provide strong, low-friction value during active exploration. Showing one nearest target for every present specialized material lets the player compare routes without repeatedly pausing or operating a targeting menu. Limiting output to four screen-edge directions prevents the 32–40 geodes from creating indicator clutter while preserving the need to travel and discover exact locations.

## Consequences

- Radar guidance remains visible and updates during unpaused gameplay.
- The maximum active directional count is four, one per present material.
- When a tracked geode is on-screen, ordinary world presentation makes it visible and its off-screen edge indicator is unnecessary.
- If no unopened geode of a material remains, the radar shows that material as exhausted in its compact status display and emits no false direction.
- Indicators that occupy similar bearings must stack or separate legibly without merging material identities.
- Opening fabrication may explain the radar but is not required to operate it.
- Common ore and super resources retain their separate discovery rules.

## Specification links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)
- [OQ-019 — How does the resource radar work?](../open-questions.md#oq-019--how-does-the-resource-radar-work)

## Supersedes / superseded by

Supersedes the single selected-material targeting and unresolved retargeting behavior in [DEC-009](./DEC-009-ore-powered-directional-resource-radar.md) and [DEC-087](./DEC-087-price-resource-radar-at-three-hundred-ore.md). [DEC-089](./DEC-089-expand-radar-to-all-mining-categories.md) later supersedes this record's four-indicator maximum and exclusions of standard ore, rich ore, and super resources while preserving its continuous display, automatic nearest-target behavior, and direction-only output.
