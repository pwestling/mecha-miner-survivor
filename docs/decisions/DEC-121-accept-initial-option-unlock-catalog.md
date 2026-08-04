---
doc_id: DEC-121
title: Accept the Initial Permanent Option-Unlock Catalog
status: accepted
authoritative: false
validation: progression-and-content-playtest
---

# DEC-121 — Accept the initial permanent option-unlock catalog

## Decision

The fresh profile begins with all six initial mechs, all 15 base weapons, the resource radar, one basic utility per specialized material, five of the ten initial relics, the standard map, and standard mode.

The initial permanent option catalog contains six purchases, all visible and purchasable without prerequisites from a fresh profile:

| ID | Purchase | Effect | Hyper Gold |
| --- | --- | --- | ---: |
| `UNL-01` | Advanced Utility Suite | Unlocks Survey Aperture, Reinforced Bulkhead, Capacitor Screen, Extraction Tether, Priority Uplink, and Field Expander together | 600 |
| `UNL-02` | Ghostline Chassis | Adds the relic to the cache pool | 250 |
| `UNL-03` | Dead-Reckoning Array | Adds the relic to the cache pool | 250 |
| `UNL-04` | War-Drum Oscillator | Adds the relic to the cache pool | 300 |
| `UNL-05` | Redline Crucible | Adds the relic to the cache pool | 350 |
| `UNL-06` | Sequential Reactor | Adds the relic to the cache pool | 400 |

The catalog costs 2,150 Hyper Gold in total. Purchases occur only between runs, are permanent and nonrefundable, have fixed prices, and cannot be disabled. Unlocking content never bypasses its geology, recipe, resource, slot, run-local acquisition, or random-cache rules. Refund PowerUps never affects these purchases.

## Status

Accepted as the initial option-unlock catalog and fresh-profile content baseline. Later breadth additions require separate decisions.

## Rationale

The initial weapons and mechs expose the central build graph and starting-playstyle choices immediately. Locking individual base weapons could make identical geological profiles offer inconsistent recipe breadth, while locking the original mechs would delay testing of signature-aware generation.

The utility bundle adds one alternate per material at the same moment, preserving symmetric material coverage. The five relic purchases provide understandable, relatively inexpensive breadth unlocks whose value is additional run variety rather than guaranteed starting power. Sharing Hyper Gold with numerical PowerUps creates a visible choice between breadth and strength without adding another currency.

Permanent, prerequisite-free purchases keep the catalog legible. Preventing the player from disabling unlocked relics preserves the meaning of expanding a random discovery pool and avoids a metagame of pruning outcomes after purchase.

## Consequences

- A fresh profile can use every initial mech and weapon and can fabricate four basic material utilities on any valid four-material profile.
- Buying the Advanced Utility Suite raises that profile availability to eight non-radar utilities; it does not fabricate any of them.
- The fresh relic-cache pool contains Retrograde Engine, Colossus Governor, Event-Horizon Coupler, Fission Seed, and Claim-Jumper Core.
- Each relic purchase permanently adds one named relic to the random cache pool without guaranteeing it in a run.
- The initial option catalog competes with the 9,450-Hyper-Gold numerical catalog and creates an 11,600-Hyper-Gold combined initial spending horizon.
- OQ-010 is resolved for the foundational progression-layer structure. Future content additions remain separate content decisions rather than an undefined base layer.
- The hangar must distinguish nonrefundable option unlocks from refundable and rank-adjustable PowerUps.

## Specification links

- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md)
- [Utility Catalog](../68-utility-catalog.md)
- [Initial Relic Catalog](../69-initial-relic-catalog.md)
- [Initial Mech Catalog](../36-initial-mech-catalog.md)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)

## Supersedes / superseded by

This supplies the fresh-profile baseline, individual option purchases, prices, and ownership rules left open by DEC-092, DEC-094, DEC-116, DEC-117, DEC-118, and DEC-120. It does not define later mechs, weapons, maps, modes, relics, cosmetics, or alternate progression catalogs.
