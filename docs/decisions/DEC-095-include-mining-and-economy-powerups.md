---
doc_id: DEC-095
title: Include Mining and Economy PowerUps
status: accepted
authoritative: false
validation: playtest
---

# DEC-095 — Include Mining and Economy PowerUps

## Decision

The account-wide permanent PowerUp catalog must include meaningful upgrades across four gameplay domains:

1. Combat effectiveness.
2. Survivability.
3. Mobility.
4. Mining and run-local economy.

Mining and economy progression is a first-class part of the permanent-power system rather than leaving every account-wide upgrade focused on horde combat. DEC-120 later defines Extraction Tuning, Tether Amplifier, and Ore Assay alongside the other ten tracks.

No PowerUp increases Hyper Gold payouts. Every completed site continues to award exactly 100 and each boss continues to drop 25 under [DEC-111](./DEC-111-make-bosses-explode-into-resources.md), so the standard-run ceiling remains 400. Mining or economy PowerUps may affect ordinary resources and other mining behavior once their effects are explicitly defined.

## Status

Accepted as the required breadth of the initial PowerUp catalog. DEC-112 later bounds the complete catalog below the power of a developed run build.

## Rationale

Mining, routing, and fabrication distinguish this game from a straightforward survivor-like. Permanent progression should reinforce those systems alongside familiar damage, defense, and movement improvements. This also creates allocation choices between making combat easier and improving the player's ability to build power during a run.

Excluding Hyper Gold yield multipliers preserves the clear 100-unit site reward and prevents the permanent currency from automatically compounding its own acquisition rate. Hyper Gold progression can still make runs more achievable through the other PowerUps.

## Consequences

- The initial PowerUp catalog is incomplete unless all four domains have meaningful representation.
- Individual PowerUps may span more than one domain, but the overall catalog must not reduce mining/economy to a cosmetic or negligible choice.
- Mining/economy PowerUps must state exactly which ordinary resources, extraction rules, fabrication rules, or navigation systems they affect.
- Hyper Gold yield remains outside all percentage-gain, bonus-payout, duplication, or ordinary-resource modifiers.
- The interface should label each PowerUp's affected systems clearly enough that players can compare immediate combat strength with run-economy development.
- Account-wide application and free full between-run refunds remain governed by DEC-093 and DEC-094.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)

## Supersedes / superseded by

Narrows the PowerUp catalog left open by [DEC-092](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md) and [DEC-093](./DEC-093-make-permanent-power-account-wide.md). It preserves fixed Hyper Gold payouts established by [DEC-091](./DEC-091-name-and-quantify-hyper-gold.md) and [DEC-111](./DEC-111-make-bosses-explode-into-resources.md). [DEC-112](./DEC-112-bound-permanent-power-below-run-build-power.md) later resolves the broad power ceiling, and [DEC-120](./DEC-120-accept-permanent-powerup-catalog.md) supplies the catalog.
