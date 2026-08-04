---
doc_id: DEC-084
title: Price Stat Upgrades by Total Weapon Upgrade Depth
status: accepted
authoritative: false
validation: playtest
---

# DEC-084 — Price Stat Upgrades by Total Weapon Upgrade Depth

## Decision

Each weapon has one shared **upgrade depth** equal to the total number of common-ore stat ranks purchased across all of its upgradeable stats.

Buying a stat upgrade adds one rank to the stat selected by the player and increases that weapon's upgrade depth by one. The ore price is determined by the weapon's current upgrade depth, not by the existing rank of the selected stat. Consequently, every stat offered by one weapon has the same next-purchase price at a given moment.

Each equipped weapon tracks its own upgrade depth. Specialized-material branch purchases do not increase it. [DEC-085](./DEC-085-use-triangular-shared-depth-prices.md) sets purchase number `n` to `5n(n + 1)` ore.

## Status

Accepted as the stat-upgrade pricing structure; numeric curve pending.

## Rationale

If each stat has an independent price ladder, the efficient default is to buy several cheap early ranks in every stat before specializing, even when that allocation does not fit the player's intended build. A shared weapon-level ladder charges for the weapon's total development instead. The player can still distribute ranks or specialize freely, but cannot evade escalating prices by moving to a stat whose personal ladder has not advanced.

## Consequences

- A weapon with stat ranks `4 / 1 / 0` has upgrade depth 5, as does one with `2 / 2 / 1`; both pay the same price for their sixth purchase.
- Buying any stat immediately raises the displayed price of every other stat on that weapon.
- A newly fabricated weapon begins at upgrade depth 0 unless an explicit starting-rank exception says otherwise.
- Each weapon has an independent counter, so investing in a different weapon begins on that weapon's own price ladder.
- A branch keeps the same depth and stat ranks before and after transformation.
- Removal or reacquisition cannot reset price independently of whatever later rule governs that weapon's stat ranks.
- The interface must show the weapon's shared depth, common next price, each stat's individual rank, and each possible resulting value.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Refines the nonlinear pricing rule in [DEC-025](./DEC-025-uncapped-linear-stat-ranks.md): price rises with the weapon's total purchased upgrade depth rather than independently with the selected stat's rank. It preserves uncapped individual ranks and fixed linear stat gains. [DEC-085](./DEC-085-use-triangular-shared-depth-prices.md) later fixes the numeric curve.
