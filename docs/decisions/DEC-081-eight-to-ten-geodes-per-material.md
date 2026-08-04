---
doc_id: DEC-081
title: Place Eight to Ten Geodes for Each Present Material
status: accepted
authoritative: false
validation: playtest
---

# DEC-081 — Place Eight to Ten Geodes for Each Present Material

## Decision

Each of the four specialized materials present in a standard run has eight, nine, or ten material geodes on the map. The geological survey reports the exact count and corresponding abundance label:

| Survey state | Geodes |
| --- | ---: |
| Scarce | 8 |
| Moderate | 9 |
| Rich | 10 |

A standard map therefore contains 32–40 material geodes. Each geode still awards one specialized material unit plus its common-ore jackpot after 20 seconds of forward extraction; DEC-086 later fixes that jackpot at 50 ore.

## Status

Accepted as the initial material-supply baseline for playtesting.

## Rationale

Four to six units per material added another severe build restriction on top of randomized four-color profiles, fixed weapon recipes, branch assignments, travel time, mining danger, and the 35-minute survival clock. Eight to ten deposits makes every present material broadly plentiful while exploration and extraction still determine how much of that theoretical supply the player actually captures.

The minimum map supply of 32 material units substantially exceeds the 17 units needed for a completely filled, fully branched four-weapon and three-utility loadout. Extreme demand can still concentrate as many as 11 units into one material under the accepted catalog, so a ten-geode material does not mathematically guarantee every pathological allocation. This is an accepted edge rather than a reason to add conversion or unlimited supply at this stage.

## Consequences

- Scarce, Moderate, and Rich now mean 8, 9, and 10 geodes rather than 4, 5, and 6.
- Opening every material geode would require 10:40–13:20 of uninterrupted extraction, or about 30.5%–38.1% of a 35-minute run before travel and interruption.
- Map generation must accommodate 32–40 separated resonance fields without overlaps or excessive world clutter.
- The survey remains strategically useful because counts still vary and exact locations remain hidden.
- The player is not expected to clear every geode; the larger supply supports routing choice and recovery from missed deposits.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)

## Supersedes / superseded by

Supersedes only the four-to-six per-material quantity and related Scarce, Moderate, and Rich thresholds in [DEC-077](./DEC-077-ore-seams-and-material-geodes.md). DEC-077's payout model, one-unit geodes, and ordinary recipe costs remain accepted.
