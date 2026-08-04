---
doc_id: DEC-093
title: Make Permanent Power Account-Wide
status: accepted
authoritative: false
validation: playtest
---

# DEC-093 — Make Permanent Power Account-Wide

## Decision

Follow the *Vampire Survivors* model for permanent numerical PowerUps: every permanent power rank purchased with Hyper Gold applies account-wide to every playable mech.

The player purchases each rank once. The same rank applies to currently available mechs and automatically applies to mechs unlocked later. There is no duplicated per-mech power tree and no need to repurchase the same numerical upgrade when changing mechs.

Account-wide PowerUps modify the shared starting baseline. Each mech's signature weapon, inherent trait, and any explicit mech-specific stat differences continue to apply on top of that baseline.

## Status

Accepted as the scope of permanent numerical progression. DEC-094 later establishes free full between-run refunds, DEC-095 requires four-domain coverage, DEC-112 bounds their combined role below run-build power, and DEC-120 supplies the individual effects, ranks, prices, and active-rank rules.

## Rationale

Account-wide progression lets players experiment with different mechs without abandoning accumulated power or repeating the same grind. It also keeps the six-mech roster comparable and makes Hyper Gold purchases easy to understand.

Mech identity remains in signatures and traits rather than separate permanent grind tracks. This preserves the intended character-selection model while following the relevant *Vampire Survivors* convention.

## Consequences

- The save profile records one purchased rank for each permanent power category, not one rank per mech.
- Newly unlocked mechs immediately inherit all purchased account-wide PowerUps.
- Mech selection must display final starting values after both the shared PowerUps and the mech's own modifiers.
- Balance tests must cover both a fresh profile with no PowerUps and a substantially upgraded account.
- Content and option unlocks remain discrete account unlocks; this decision specifically resolves the scope of numerical power.
- No mech-specific permanent numerical progression is part of the baseline design. A later exception would require an explicit new decision.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)

## Supersedes / superseded by

Resolves the account-wide-versus-mech-specific scope left open by [DEC-092](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md). [DEC-094](./DEC-094-allow-free-powerup-refunds.md) later resolves refund behavior, [DEC-112](./DEC-112-bound-permanent-power-below-run-build-power.md) resolves the broad balance envelope, and [DEC-120](./DEC-120-accept-permanent-powerup-catalog.md) supplies the individual catalog.
