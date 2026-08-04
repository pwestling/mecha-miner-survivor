---
doc_id: DEC-082
title: Deplete Both Ore-Seam Classes in Fifteen Seconds
status: accepted
authoritative: false
validation: playtest
---

# DEC-082 — Deplete Both Ore-Seam Classes in Fifteen Seconds

## Decision

Both common-ore seam classes take 15 seconds of uninterrupted forward extraction to exhaust from untouched:

| Seam | Installment | Interval | Installments | Total payout | Total time |
| --- | ---: | ---: | ---: | ---: | ---: |
| Standard | 10 ore | 1.5 seconds | 10 | 100 ore | 15 seconds |
| Rich | 40 ore | 3 seconds | 5 | 200 ore | 15 seconds |

Every completed installment remains a permanent run-local checkpoint. Only progress toward the current unfinished installment can decay after leaving the extraction zone. [DEC-083](./DEC-083-set-common-ore-unit-to-ten.md) fixes the formerly open value at `X = 10`.

## Status

Accepted as the initial playtest cadence and capacity.

## Rationale

Equal depletion times make the route commitment easy to compare while preserving visibly different payout rhythms. Standard seams provide frequent, resilient progress. Rich seams require twice as long before each checkpoint but pay four times as much, producing exactly twice the ore per second and twice the total ore over the same 15-second commitment.

## Consequences

- Standard-seam payouts occur at 1.5-second intervals through the 15-second depletion point.
- Rich-seam payouts occur at 3-second intervals through the 15-second depletion point.
- A full standard seam yields 100 ore; a full rich seam yields 200 ore.
- DEC-090 later fixes the map at 20 standard and 8 rich seams; DEC-115 subsequently fixes the initial spatial-distribution and opening-fairness rules.
- Interrupted standard seams risk less time per unsecured installment than rich seams.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)

## Supersedes / superseded by

Resolves `N` and finite installment capacity left open by [DEC-077](./DEC-077-ore-seams-and-material-geodes.md) while preserving its `X`/`4X`, `N`/`2N`, checkpoint, and finite-seam relationships. [DEC-083](./DEC-083-set-common-ore-unit-to-ten.md) later sets `X = 10`, and [DEC-090](./DEC-090-place-twenty-standard-and-eight-rich-ore-seams.md) fixes map counts.
