---
doc_id: DEC-025
title: Use Uncapped Linear Stat Ranks with Nonlinear Prices
status: accepted
authoritative: false
---

# DEC-025 — Use Uncapped Linear Stat Ranks with Nonlinear Prices

## Decision

Individual weapon stats have no explicit rank cap. Every purchased rank adds a fixed amount to the chosen stat. Under the later [DEC-084](./DEC-084-price-stat-upgrades-by-weapon-depth.md), the common-ore price rises nonlinearly with total stat ranks purchased on that weapon rather than independently with the chosen stat's rank.

The result is linear stat growth but diminishing gain per ore. The finite ore available during a 35-minute run provides the practical limit without forbidding extreme specialization.

## Status

Accepted.

## Context

Independent stat purchasing needs to permit expressive specialization without allowing one cheap stat to scale efficiently forever. A hard cap would prevent excess but would also remove the possibility of deliberately irrational investment and hidden high-rank discoveries.

## Considered options

### Fixed rank caps

Caps are easy to communicate and balance but eventually remove the choice to keep specializing.

### Diminishing per-rank stat gains

This limits scaling but makes the next purchase less legible because the benefit itself continually changes.

### Fixed gains with nonlinear prices and no cap

This keeps each rank's benefit predictable while making later ranks increasingly expensive relative to their gain.

## Rationale

The player always knows what the next rank does, and the economy discourages excessive concentration without making it illegal. This also leaves room for an optional Easter egg hidden behind an intentionally inefficient degree of investment.

## Consequences

- Every weapon has a fixed authored bundle of relevant upgradeable stats.
- Every stat needs a stable player-facing unit and fixed per-rank increment.
- Prices are deterministic and visible but increase nonlinearly with the weapon's shared upgrade depth under DEC-084.
- Cadence-like stats must be expressed in a form that can increase linearly without reaching an undefined endpoint; attacks per second is preferable to uncapped percentage cooldown reduction.
- DEC-085 fixes the shared-depth price function; per-rank gains and cross-stat interactions remain open in OQ-025.
- Extreme specialization must be tested for breakpoints that outperform its intended economic inefficiency.
- A hidden reward for excessive investment is only a proposal; its existence and rules are not accepted by this decision.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Narrows the rank-cap and cost-curve questions left open by [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md). [DEC-084](./DEC-084-price-stat-upgrades-by-weapon-depth.md) later changes the price input from individual stat rank to total weapon upgrade depth, and [DEC-085](./DEC-085-use-triangular-shared-depth-prices.md) fixes the curve. This record does not determine per-rank stat gains or weapon stat bundles.
