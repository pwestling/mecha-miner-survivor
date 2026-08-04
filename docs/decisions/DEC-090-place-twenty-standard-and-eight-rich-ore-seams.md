---
doc_id: DEC-090
title: Place Twenty Standard and Eight Rich Ore Seams
status: accepted
authoritative: false
validation: playtest
---

# DEC-090 — Place Twenty Standard and Eight Rich Ore Seams

## Decision

Every standard 35-minute map initially contains:

- 20 standard ore seams, each worth 100 common ore when fully depleted.
- 8 rich ore seams, each worth 200 common ore when fully depleted.

All 28 seam locations are randomized under the map's ordinary placement and fairness rules. Rich seams remain less common than standard seams. The counts are fixed for the initial playtest baseline. DEC-115 later establishes the initial spacing, clustering, region-distribution, and opening-fairness rules; exact biome weighting remains content work.

The seams contain 3,600 common ore in total. The map's 32–40 material geodes add another 1,600–2,000 common ore, for a theoretical ordinary mining supply of 5,200–5,600 common ore before relic sales.

## Status

Accepted as the initial playtest density and economy baseline.

## Rationale

A large map built around exploration needs enough ore targets that natural travel can reveal useful choices without making the resource radar mandatory. Sixteen proposed seams would have made common-ore opportunities too spatially sparse. Twenty-eight preserves rich seams as comparatively uncommon while supporting route planning and recovery.

The theoretical total is not expected player income. Fully depleting all seams alone takes seven minutes of uninterrupted extraction; opening every material geode adds 10:40–13:20 before travel, combat, retreats, super-resource sites, or other objectives. The 35-minute timer therefore forces selection among plentiful opportunities rather than allowing effortless collection of the entire map.

## Consequences

- Fully depleting all standard seams yields 2,000 common ore.
- Fully depleting all rich seams yields 1,600 common ore.
- Fully depleting all 28 seams requires seven minutes of uninterrupted forward extraction.
- Natural exploration should encounter ore with useful regularity, while the radar can still provide the nearest target of each seam class.
- Map-generation tests must measure discovered seams per route, time between ore opportunities, clustering, inaccessible placements, and the share of generated ore players actually collect.
- Counts, payouts, and placement constraints remain subject to playtesting as a connected economy rather than independently tuned values.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)

## Supersedes / superseded by

Resolves the seam-count variables left open by [DEC-077](./DEC-077-ore-seams-and-material-geodes.md) and [DEC-082](./DEC-082-fifteen-second-ore-seams.md). [DEC-115](./DEC-115-adopt-standard-map-generation-contract.md) later resolves the initial spatial-distribution rules.
