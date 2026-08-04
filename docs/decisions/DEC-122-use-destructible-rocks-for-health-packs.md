---
doc_id: DEC-122
title: Use Destructible Rocks as the Health-Pack Source
status: superseded
authoritative: false
validation: playtest
---

# DEC-122 — Use Destructible Rocks as the Health-Pack Source

> **Completion note:** DEC-126 supplies the durability, footprint, targeting, spawn-annulus, pickup-radius, and repair values that were open when this decision was recorded. DEC-123's population supersession remains unchanged.

## Decision

The 16 non-enemy breakable objects already distributed across each standard map are **destructible rocks** rather than a general field-container class.

Automatic weapons can damage and destroy them when their targeting rules permit. A destroyed rock sometimes drops one health pack and otherwise drops nothing. Health packs persist in the world until the mech touches them or the run ends, are collected automatically on contact, and immediately restore Hull Integrity without exceeding the current maximum. Touching a health pack at full Hull still consumes it.

Destructible rocks have no other reward table. They never drop temporary effects, common ore, specialized materials, Hyper Gold, weapons, utilities, relics, or permanent rewards. Exact rock durability, healing amount, pickup radius, and audiovisual treatment remain playtest and presentation variables; DEC-123 later fixes the health-pack chance at 20%.

## Status

Superseded in part by DEC-123. Destructible rocks remain the standard mode's sole ordinary map-based healing-drop system, but 16 is now an active population cap rather than a whole-map lifetime count, and replenishment and drop chances are explicit.

## Rationale

The game needs a finite route to active healing because the shared baseline has no passive Recovery. A single occasional health-pack result supplies that recovery without introducing a second catalog of temporary effects, competing with mining rewards, or making ordinary enemy kills a loot source.

Destructible rocks fit the map and mining theme while remaining mechanically separate from resource deposits: breaking one is combat incidental, not extraction, and it never yields crafting material.

## Consequences

- This decision originally retained 16 lifetime breakable objects; DEC-123 supersedes that consequence with a replenishing population capped at 16 active rocks.
- OQ-016 is resolved: ordinary enemies and elites drop nothing, rocks sometimes drop health packs, and bosses retain their fixed resource bursts.
- The player may leave a health pack uncollected for later recovery, but it receives no resource-radar direction.
- The initial pickup catalog contains only the health pack.
- Temporary battlefield-effect pickups are removed from the standard baseline.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [OQ-016 — What rewards, if any, come directly from defeating monsters?](../open-questions.md#oq-016--what-rewards-if-any-come-directly-from-defeating-monsters)

## Supersedes / superseded by

This narrows the general repair-or-temporary-effect field-container allowance in DEC-102 and DEC-103 to one destructible-rock health-pack result. [DEC-123](./DEC-123-replenish-destructible-rocks-around-the-player.md) supersedes its fixed whole-map count with a replenishing 16-rock active population. Neither decision alters DEC-111's boss resource rewards.
