---
doc_id: DEC-094
title: Allow Free PowerUp Refunds
status: accepted
authoritative: false
validation: playtest
---

# DEC-094 — Allow Free PowerUp Refunds

## Decision

Between runs, the player can use **Refund PowerUps** to reset every purchased account-wide numerical PowerUp rank and receive exactly all Hyper Gold spent on those ranks.

The refund has no fee, penalty, cooldown, or usage limit. The returned Hyper Gold is immediately available to buy a different PowerUp allocation before the next deployment.

Refund PowerUps affects only numerical PowerUp ranks. Permanent content and option unlocks remain unlocked, are not converted back into Hyper Gold, and never need to be repurchased after a PowerUp refund. PowerUps cannot be refunded or reallocated during an active run.

## Status

Accepted as the initial respec rule.

## Rationale

Free reallocation follows the relevant *Vampire Survivors* convention and encourages players to test different builds, mechs, and strategies without risking previously earned progression currency. Because a refund only moves already-earned Hyper Gold and cannot occur during a run, it creates flexibility without changing the current deployment's stakes.

Keeping option unlocks permanent avoids confusing loss of access, invalid loadouts, or repeated content purchases. The reset remains a numerical-build tool rather than a general account rollback.

## Consequences

- The between-run progression interface displays the total Hyper Gold that Refund PowerUps will return before confirmation.
- Confirming the action sets every numerical PowerUp rank to zero and adds the exact cumulative purchase cost back to the banked Hyper Gold balance.
- Purchased content and option unlocks remain unchanged.
- There is no partial loss caused by changing prices after the original purchase; the system refunds the actual Hyper Gold paid for the active ranks.
- The player can immediately rebuy any affordable allocation and may repeat the process without cost.
- The interface must clearly distinguish refundable PowerUps from nonrefundable permanent unlocks.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)
- [OQ-006 — Where do resources, crafting, and upgrades persist?](../open-questions.md#oq-006--where-do-resources-crafting-and-upgrades-persist)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)

## Supersedes / superseded by

Resolves the PowerUp refund rule left open by [DEC-092](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md) and [DEC-093](./DEC-093-make-permanent-power-account-wide.md). It does not make content or option unlocks refundable; [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) later confirms that the initial six option purchases remain permanent.
