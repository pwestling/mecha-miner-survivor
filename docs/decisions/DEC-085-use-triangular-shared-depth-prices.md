---
doc_id: DEC-085
title: Use a Triangular Shared-Depth Price Curve
status: accepted
authoritative: false
validation: playtest
---

# DEC-085 — Use a Triangular Shared-Depth Price Curve

## Decision

The common-ore price of stat purchase number `n` on one weapon is:

```text
price(n) = 5n(n + 1) ore
```

`n` is one greater than the weapon's current upgrade depth. Equivalently, at current depth `d`:

```text
next price(d) = 5(d + 1)(d + 2) ore
```

The initial price schedule is:

| Purchase number | Price | Cumulative cost |
| ---: | ---: | ---: |
| 1 | 10 | 10 |
| 2 | 30 | 40 |
| 3 | 60 | 100 |
| 4 | 100 | 200 |
| 5 | 150 | 350 |
| 6 | 210 | 560 |
| 7 | 280 | 840 |
| 8 | 360 | 1,200 |
| 9 | 450 | 1,650 |
| 10 | 550 | 2,200 |

The same schedule applies independently to every weapon. The player chooses which of that weapon's stats receives each purchase.

## Status

Accepted as the initial playtest stat-price curve.

## Rationale

The curve makes early development accessible and later weapon investment increasingly expensive without imposing a cap. Its first milestones align with the ore economy: one complete standard seam funds the first three purchases on a weapon, and one complete rich seam funds the first four. Every price is a multiple of 10, matching the smallest established ore installment and avoiding currency fragments.

## Consequences

- Purchase prices grow quadratically with weapon upgrade depth; cumulative weapon investment grows cubically.
- A player cannot reduce a weapon's next price by choosing a different stat.
- The first three ranks on each newly fabricated weapon are comparatively accessible, encouraging the player to bring additional weapons online.
- Deep investment remains possible but competes sharply with developing the rest of the arsenal.
- The fabrication interface must show the next common price before the player selects the receiving stat.
- Per-stat effect increments remain weapon-specific tuning variables.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Resolves the numeric price function left open by [DEC-025](./DEC-025-uncapped-linear-stat-ranks.md) and [DEC-084](./DEC-084-price-stat-upgrades-by-weapon-depth.md). It preserves their uncapped ranks, fixed gains, and shared per-weapon depth structure.
