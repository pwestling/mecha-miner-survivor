---
doc_id: DEC-077
title: Use Ore Seams and Completion-Only Material Geodes
status: accepted
authoritative: false
---

# DEC-077 — Use Ore Seams and Completion-Only Material Geodes

## Decision

Use three ordinary run-local mining-point classes:

- Standard ore seams award `X` common ore every `N` seconds, with a short `N`.
- Rich ore seams award `4X` common ore every `2N` seconds, producing twice the ore rate with longer exposure before each secured installment.
- Material geodes award one specialized material and a common-ore jackpot only at full completion; DEC-086 later fixes that jackpot at 50 ore.

Completed seam installments are permanent run-local checkpoints. Only progress toward the unfinished current installment can decay. A geode has one uncheckpointed extraction bar and pays nothing if the attempt never reaches completion.

Every selected specialized material initially had exactly four, five, or six geodes on the map, reported in the survey as Scarce, Moderate, or Rich. [DEC-081](./DEC-081-eight-to-ten-geodes-per-material.md) supersedes those quantities with eight, nine, or ten geodes per present material. The survey still discloses the count but not locations; inability to collect that supply due to routing, combat, risk, or time remains part of play.

Fix initial specialized-material costs at:

- base weapon: one unit of each of its two recipe materials;
- non-radar utility: one specialized-material unit; DEC-109 later assigns exactly one material to each utility rather than two alternatives;
- weapon branch: two units of the branch's assigned material;
- common-ore stat rank: its deterministic common-ore price rather than specialized material.

## Status

Accepted for the prototype economy. [DEC-080](./DEC-080-twenty-second-geodes-forty-five-second-super-resources.md) later fixes geode completion at 20 seconds; [DEC-082](./DEC-082-fifteen-second-ore-seams.md) fixes `N`, installment capacities, and both seam depletion times; [DEC-083](./DEC-083-set-common-ore-unit-to-ten.md) fixes `X = 10`; [DEC-086](./DEC-086-fifty-ore-geode-jackpot.md) fixes the geode jackpot at 50 ore; and [DEC-090](./DEC-090-place-twenty-standard-and-eight-rich-ore-seams.md) fixes map seam counts.

## Rationale

Frequent standard payouts make basic progression resilient to interruption. Rich seams trade longer checkpoint exposure for visibly superior income. Completion-only geodes make specialized materials feel like deliberate exploration prizes without making their total map supply another severe build restriction.

Unit-sized specialized costs are immediately legible: paired materials construct a weapon, one flexible material constructs a utility, and committing two matching units transforms a weapon.

## Consequences

- “Rare ore” should be labeled rich ore in canonical text so it is not confused with rare cross-run resources.
- This record originally produced 16–24 geodes; DEC-081 raises the current standard-map total to 32–40.
- Specialized-material abundance becomes countable and strategically legible.
- DEC-109 supersedes the original two-alternative utility assignment: the current catalog target assigns two single-material utilities to each of the six materials.
- DEC-081 replaces the four-unit floor with an eight-unit floor and accepts that the most extreme 11-unit single-material allocation can still exceed the ten-geode ceiling.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)

## Supersedes / superseded by

Refines the payout categories established by DEC-003 and resolves the ordinary specialized-resource payout and initial unit-cost portions of OQ-004, OQ-013, OQ-014, and OQ-025. DEC-081 supersedes only its per-material geode quantities, DEC-082 resolves its ore-seam cadence and capacities, DEC-083 resolves its ore denomination, DEC-090 resolves its seam counts, and DEC-109 supersedes its alternative-material utility recipe. This record does not change the separate progress-threshold threat beacon or survival-gated banking rules for super resources.
