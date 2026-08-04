---
doc_id: DEC-020
title: Keep Ordinary Crafting Materials Exclusive to Mining
status: superseded
authoritative: false
---

# DEC-020 — Keep Ordinary Crafting Materials Exclusive to Mining

## Decision

Ordinary enemies do not drop ordinary crafting materials. Mining is the exclusive way new ordinary crafting materials enter the player's inventory during a run. Killing enemies creates safety and space but does not directly advance the ordinary crafting economy.

Under the later DEC-102, ordinary enemies and elites also drop no repair pickups, consumables, temporary effects, or other items. DEC-122/123 supply the separate recovery route: replenishing non-enemy destructible rocks have a fixed 20% chance to drop one health pack and otherwise drop nothing. [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) explicitly revisits the resource rule for bosses, which drop common ore and present-profile specialized materials.

## Status

Superseded in part by [DEC-029](./DEC-029-pause-and-resolve-relic-discoveries.md) and [DEC-111](./DEC-111-make-bosses-explode-into-resources.md), and extended by [DEC-102](./DEC-102-separate-enemy-kills-from-field-pickups.md). The ordinary-enemy and elite prohibition remains accepted; relic sales and boss loot are non-mining ore sources, bosses can also drop present-profile specialized materials, and ordinary enemies have no item drops of any kind.

## Context

The game removes XP specifically so exploration and mining, rather than killing wherever the player happens to stand, drive ordinary run progression. Enemy material drops would gradually turn combat back into a passive progression stream and weaken the need to seek and hold mining points.

## Considered options

### Ordinary crafting materials from all enemies

This rewards combat continuously but risks recreating XP with additional resource names.

### Crafting fragments from elites or bosses

This preserves some combat milestones but makes the crafting curve partly dependent on kills instead of the map's resource ecology.

### Mining-exclusive ordinary materials

This makes combat instrumental to reaching and holding resource sites while keeping material acquisition spatial and intentional.

## Rationale

Exclusive mining preserves the game's central differentiator. Automatic combat clears routes and protects extraction; exploration and positional commitment produce build power. Non-crafting drops can still make kills satisfying without undermining that division.

## Consequences

- Ordinary enemies never drop common ore or specialized ordinary resources.
- Enemy density and kill rate do not directly determine ordinary crafting income.
- Map generation and mining availability carry full responsibility for the run's ordinary material economy.
- Weapons and utilities cannot be refunded or dismantled under DEC-100.
- Ordinary enemies and elites have no item drops. Dynamically replenished destructible rocks sometimes provide one health pack under DEC-122/123.
- DEC-111 later reconsiders the boss restriction and makes bosses a limited source of common ore and present-profile specialized materials.
- DEC-029 establishes relic sale as another non-mining source of common ore.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-016 — What rewards, if any, come directly from defeating monsters?](../open-questions.md#oq-016--what-rewards-if-any-come-directly-from-defeating-monsters)
- [OQ-027 — How are mech relic discoveries presented and resolved?](../open-questions.md#oq-027--how-are-mech-relic-discoveries-presented-and-resolved)
- [DEC-028 — Use one exploration-found mech relic](./DEC-028-one-exploration-found-mech-relic.md)
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./DEC-029-pause-and-resolve-relic-discoveries.md)
- [DEC-100 — Commit installed weapons and utilities](./DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-102 — Separate enemy kills from field pickups](./DEC-102-separate-enemy-kills-from-field-pickups.md)

## Supersedes / superseded by

Extended [DEC-002](./DEC-002-mining-replaces-xp-and-chests.md) by excluding ordinary crafting-material drops from enemies. [DEC-029](./DEC-029-pause-and-resolve-relic-discoveries.md) supersedes the absolute common-ore source rule through relic sales. [DEC-102](./DEC-102-separate-enemy-kills-from-field-pickups.md) completes the ordinary-enemy rule by prohibiting all item drops, while [DEC-122](./DEC-122-use-destructible-rocks-for-health-packs.md) and [DEC-123](./DEC-123-replenish-destructible-rocks-around-the-player.md) assign ongoing health-pack opportunities to separate destructible rocks. [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) later creates an explicit boss exception for common ore and specialized materials.
