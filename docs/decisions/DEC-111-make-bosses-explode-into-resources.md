---
doc_id: DEC-111
title: Make Bosses Explode into Collectible Resources
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-111 — Make Bosses Explode into Collectible Resources

## Decision

Defeating any of the four interval bosses produces an immediate, conspicuous physical loot explosion at the boss's death position. It opens no chest, menu, reward choice, or simulation pause.

### Initial payouts

Every boss drops:

- **300 common ore**;
- **25 Hyper Gold**; and
- specialized crafting materials selected from the four materials present in the current resource profile.

The 7:00 and 14:00 bosses each drop one specialized-material unit. The 21:00 and 28:00 bosses each drop two units. Each unit selects its material independently and uniformly from the four present materials, so a two-unit burst may contain two different materials or a useful matching pair. Bosses cannot introduce an absent fifth or sixth material into the run.

If every boss is defeated and every item collected, boss loot adds 1,200 common ore, 100 Hyper Gold, and six specialized-material units to the run. Together with the three 100-unit sites, the maximum unmodified Hyper Gold available on a standard map is therefore 400.

### Physical collection

- The reward appears as multiple visually distinct ore chunks, Hyper Gold pieces, and specialized-material pieces that burst outward and settle on valid nearby navigable ground.
- Pieces never land inside solid terrain or outside the finite map.
- Every piece persists until collected or the run ends and receives a visible minimap marker immediately.
- The mech collects a piece automatically on contact. There is no interaction button or baseline attraction beyond the pickup's contact area.
- Combat and the run timer continue throughout the burst and collection. Recovering the pile under ongoing horde pressure is part of the reward moment.
- Uncollected pieces are lost at run end. Collected common ore and specialized materials are run-local; collected Hyper Gold remains unsecured and is banked only by successful mission extraction.

The listed payouts are fixed and are not increased by PowerUps, Luck, boss order delay, or loot modifiers. Exact piece counts, scatter radius, animation, sound, pickup radius, and numeric payouts remain playtest variables without changing the reward categories.

## Status

Accepted as the initial boss-reward model. The reward categories, physical non-modal burst, present-material restriction, and early/late specialized-unit counts are fixed; numeric currency payouts and presentation require playtesting.

## Rationale

A large visible resource burst makes boss defeat immediately legible and celebratory. Physical pieces create a short positional payoff under continuing pressure rather than another fabrication-style decision screen.

The reward reinforces the existing economy instead of granting random weapons or upgrades. Common ore accelerates intentional purchases, specialized materials help realize the current geology-constrained build, and Hyper Gold gives boss mastery a cross-run benefit that remains subject to survival-gated banking.

Restricting material drops to the four surveyed resources preserves the run's recipe boundary. Six total units are helpful but remain well below the map's 32–40 geode units, so exploration and mining remain the main source of specialized materials.

## Consequences

- Bosses are explicit exceptions to the no-drop rule for ordinary enemies and elites.
- Mining remains the primary source of all three reward categories but is no longer the exclusive source of specialized materials or Hyper Gold.
- A full standard run contains up to 400 Hyper Gold: 300 from sites and 100 from boss loot.
- Boss-death effects must visually separate collectable rewards from damage, enemy projectiles, player attacks, and ordinary death debris.
- The minimap requires a temporary persistent boss-loot marker state.
- Reward collection can be delayed safely, but returning costs travel time and may expose the player to later waves.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Open Questions](../open-questions.md)

## Supersedes / superseded by

Supersedes the provisional no-reward baseline in [DEC-102](./DEC-102-separate-enemy-kills-from-field-pickups.md), creates explicit boss exceptions to [DEC-020](./DEC-020-mining-exclusive-ordinary-materials.md), and increases the standard-map Hyper Gold ceiling established by [DEC-091](./DEC-091-name-and-quantify-hyper-gold.md) without changing any site's 100-unit payout.
