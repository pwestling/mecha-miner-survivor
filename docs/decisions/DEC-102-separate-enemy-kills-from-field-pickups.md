---
doc_id: DEC-102
title: Separate Enemy Kills from Field Pickups
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-102 — Separate Enemy Kills from Field Pickups

## Decision

Ordinary enemies and elites provide no direct item, XP, common-ore, specialized-material, Hyper Gold, repair, consumable, or temporary-effect drop when killed. Their immediate reward is the space and safety created by removing them.

Kills are still counted and may satisfy explicitly disclosed weapon effects, mech traits, achievements, challenges, or permanent content-unlock conditions. Such counters do not create an ordinary run currency or XP-like progression track.

To inherit the reference game's limited battlefield-recovery role without making kills the progression source, standard maps use separate destructible non-enemy objects. DEC-122 later identifies them as destructible rocks whose only possible reward is one health pack; DEC-123 changes the original fixed 16-object placement into a replenishing population capped at 16 and fixes the pack chance at 20%. They otherwise drop nothing and never award common ore, specialized materials, or Hyper Gold.

[DEC-111](./DEC-111-make-bosses-explode-into-resources.md) later resolves boss rewards as a physical, non-modal burst of common ore, unsecured Hyper Gold, and one or two random present-profile specialized units. The *Vampire Survivors* treasure-chest answer is not inherited because random chest weapon progression is explicitly replaced by mining and deterministic fabrication.

## Status

Accepted for ordinary enemies and elites. DEC-111 later resolves boss rewards, DEC-122 resolves the separate destructible-rock recovery content, and DEC-123 supplies its replenishment and drop cadence.

## Rationale

Dropping progression value from enemies would pull the player toward stationary kill farming and undermine exploration and mining. Separating recovery objects from enemies preserves occasional battlefield relief while keeping location and movement important.

Kill-based challenges retain the reference game's content-unlock language without becoming a hidden XP economy. Bosses need a bespoke reward compatible with this game's systems rather than a renamed random chest.

## Consequences

- Enemy death feedback never resembles a collectible crafting-material drop.
- Destructible rocks are dynamically spawned world content and remain discoverable even when no enemies are nearby.
- Their possible health packs resolve immediately and are not stored as ordinary crafting materials.
- Mining remains the primary source of common ore, specialized materials, and Hyper Gold. DEC-111 later creates limited boss-loot sources for all three.
- Boss reward design is resolved by DEC-111.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-016 — What rewards, if any, come directly from defeating monsters?](../open-questions.md#oq-016--what-rewards-if-any-come-directly-from-defeating-monsters)

## Supersedes / superseded by

Completes the ordinary-enemy drop prohibition in [DEC-020](./DEC-020-mining-exclusive-ordinary-materials.md). [DEC-111](./DEC-111-make-bosses-explode-into-resources.md) later resolves boss rewards, [DEC-122](./DEC-122-use-destructible-rocks-for-health-packs.md) narrows separate breakable-object content to destructible rocks and health packs, and [DEC-123](./DEC-123-replenish-destructible-rocks-around-the-player.md) resolves OQ-016's availability cadence.
