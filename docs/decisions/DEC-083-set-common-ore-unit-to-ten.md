---
doc_id: DEC-083
title: Set the Common-Ore Installment Unit to Ten
status: accepted
authoritative: false
validation: playtest
---

# DEC-083 — Set the Common-Ore Installment Unit to Ten

## Decision

Set the initial common-ore payout unit to `X = 10` ore. Under the accepted 15-second seam profiles:

| Seam | Checkpoint payout | Checkpoints | Complete-seam payout |
| --- | ---: | ---: | ---: |
| Standard | 10 ore every 1.5 seconds | 10 | 100 ore |
| Rich | 40 ore every 3 seconds | 5 | 200 ore |

## Status

Accepted as the initial playtest currency scale.

## Rationale

Ten provides simple mental arithmetic while leaving enough integer granularity for nonlinear stat prices, radar cost, relic sale values, and geode jackpots. The choice fixes the denomination rather than the overall generosity of the economy; deposit counts and purchase prices still determine actual progression speed.

## Consequences

- Common-ore feedback uses concrete integer amounts rather than an algebraic placeholder.
- A standard seam is worth 100 ore and a rich seam 200 ore.
- Rich seams retain exactly twice the total value and income rate of standard seams.
- Relic sale values remain a separate tuning decision; DEC-085 fixes stat prices, DEC-086 fixes the geode jackpot at 50, DEC-087 fixes the radar at 300, and DEC-090 fixes map seam counts.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Resolves the `X` value left open by [DEC-077](./DEC-077-ore-seams-and-material-geodes.md) and [DEC-082](./DEC-082-fifteen-second-ore-seams.md). It does not alter their timing, checkpoint, or relative-yield rules.
