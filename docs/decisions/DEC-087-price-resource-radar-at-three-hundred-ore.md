---
doc_id: DEC-087
title: Price the Resource Radar at Three Hundred Ore
status: accepted
authoritative: false
validation: playtest
---

# DEC-087 — Price the Resource Radar at Three Hundred Ore

## Decision

Fabricating the run-local resource radar costs 300 common ore. It remains available from the beginning, always offered in the fixed catalog, and occupies one utility slot.

The 300 ore is the fabrication price for acquiring the radar. DEC-088 later removes manual retargeting by tracking all four present materials simultaneously, and DEC-089 adds standard ore, rich ore, and super-resource sites.

## Status

Accepted as the initial playtest price.

## Rationale

The radar is a dependable recovery tool that can remove blind searching for a desired present material. It should therefore be available but should not be a routine or effectively free opening purchase. Three hundred ore makes navigation certainty a real build tradeoff.

At the accepted economy values, 300 ore equals three complete standard seams, one rich plus one standard seam, or six geode jackpots. It could instead buy the first three stat upgrades on three different weapons. Choosing the radar therefore sacrifices substantial immediate combat development while preserving the player's route to an intended specialized-material build.

## Consequences

- Fabrication shows a fixed 300-ore radar price from deployment onward.
- The player cannot purchase it until 300 ore has been collected and left unspent.
- Its price does not change with weapon upgrade depths or map geology.
- Its utility-slot cost remains separate from its ore cost.
- Removal, replacement, refund, and exact signal-layout rules remain open; DEC-088 resolves continuous targeting behavior and DEC-089 expands its coverage.

## Specification links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-019 — How does the resource radar work?](../open-questions.md#oq-019--how-does-the-resource-radar-work)

## Supersedes / superseded by

Resolves the common-ore price left open by [DEC-009](./DEC-009-ore-powered-directional-resource-radar.md). [DEC-088](./DEC-088-show-continuous-multi-material-radar-directions.md) later replaces manual targeting with continuous directions for all present materials. [DEC-089](./DEC-089-expand-radar-to-all-mining-categories.md) expands target coverage while preserving the 300-ore price, direction-only output, and utility-slot requirement.
