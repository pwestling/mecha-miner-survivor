---
doc_id: DEC-086
title: Award Fifty Common Ore from Each Material Geode
status: accepted
authoritative: false
validation: playtest
---

# DEC-086 — Award Fifty Common Ore from Each Material Geode

## Decision

Completing a material geode awards 50 common ore together with its one specialized material unit. Both rewards remain completion-only and run-local.

## Status

Accepted as the initial playtest geode jackpot.

## Rationale

Fifty ore keeps material routing connected to incremental weapon growth without allowing the 32–40 material geodes on a standard map to replace dedicated ore seams. A geode pays half of a standard seam and one quarter of a rich seam in common ore; its specialized material remains the primary reward.

The value also aligns with the current build and price structures. Mining the 16 geodes required for a fully slotted, branched build using the common-ore radar yields 800 jackpot ore—exactly enough to buy the first four stat upgrades on each of four weapons when distributed evenly.

## Consequences

- A completed geode pays exactly one specialized material and 50 common ore.
- No fraction of either reward is paid before completion.
- The complete 32–40-geode map population contains 1,600–2,000 potential jackpot ore, although clearing the entire population is not expected.
- Common-ore seams remain the better dedicated source: 100 from standard and 200 from rich in less extraction time.
- The HUD and completion feedback must present the two geode rewards separately.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)

## Supersedes / superseded by

Resolves the material-geode common-ore jackpot left open by [DEC-077](./DEC-077-ore-seams-and-material-geodes.md), [DEC-080](./DEC-080-twenty-second-geodes-forty-five-second-super-resources.md), and [DEC-083](./DEC-083-set-common-ore-unit-to-ten.md).
