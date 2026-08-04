---
doc_id: DEC-103
title: Use Hull Integrity and Contact-Collected Field Pickups
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-103 — Use Hull Integrity and Contact-Collected Field Pickups

> **Completion note:** DEC-126 fixes damage resolution, collision radii, contact grace, the 25-Hull health pack, rock durability, and the survival margins left open here. Display composition remains presentation work.

## Decision

Every mech has current and maximum **Hull Integrity**, the player-facing health measure. The shared fresh-run baseline is 100 maximum Hull Integrity, zero passive recovery, and full current Hull Integrity at deployment before account PowerUps and mech-specific modifiers apply.

Incoming contact, projectile, and hazard damage reduces current Hull Integrity after Armor. Each Armor point subtracts one point from an incoming damage instance, to a minimum of one damage, unless that attack explicitly ignores Armor. Reaching zero causes death unless an equipped revival effect explicitly intercepts it. The standard game has no passive healing or regeneration unless a mech trait, PowerUp, relic, utility, or temporary field effect says otherwise.

Repairs restore current Hull Integrity up to its current maximum and cannot overheal. Hull Integrity remains visible during active play and appears with its exact current and maximum values on the pause surface.

DEC-122 later narrows the breakable-object rule to destructible rocks, and DEC-123 fixes their replenishment and 20% health-pack chance. A destroyed rock otherwise releases nothing. The pack remains in the world until collected or the run ends and is collected automatically when the mech touches its pickup area; there is no interaction button. It repairs Hull and consumes itself on contact even if some or all of its value is wasted at maximum Hull Integrity. No baseline pickup attraction exists beyond the contact area, although an explicit trait, PowerUp, utility, or relic may extend it.

Defeated ordinary enemies and elites leave no loot or collision-bearing corpse. They may play brief readable death feedback before disappearing.

## Status

Accepted as the standard survivability and incidental-pickup model. Exact damage values, pickup and collision radii, post-hit immunity, health-pack quantity, rock durability, and display layout remain tuning work. Rock replenishment and drop chance are supplied by DEC-123.

## Rationale

Finite visible health, zero baseline recovery, destructible field objects, and automatic contact pickup preserve the reference game's low-input survival rhythm. Separating pickups from enemies keeps mining as the resource economy and makes incidental recovery a spatial exploration reward rather than a kill-rate reward.

A 100-point shared baseline makes mech and PowerUp modifiers legible without fixing enemy damage balance prematurely.

## Consequences

- Mech-selection previews show Max Hull Integrity, Armor, and Recovery modifiers relative to the shared baseline whenever a mech changes them.
- Damage feedback must distinguish Hull Integrity loss, blocked or reduced damage, post-hit immunity, repair, and death.
- The player can deliberately avoid a field pickup until it is useful, but touching it always collects it.
- Destructible rocks and health packs cannot become a source of common ore, specialized materials, or Hyper Gold.
- An explicit revival resumes the same run under DEC-099; without one, zero Hull Integrity ends the run.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Open Questions](../open-questions.md)
- [Glossary](../glossary.md)

## Supersedes / superseded by

Extends the non-solid contact-damage model in [DEC-097](./DEC-097-inherit-direct-movement-collision-and-camera.md), the no-drop enemy and separate-breakable-object rule in [DEC-102](./DEC-102-separate-enemy-kills-from-field-pickups.md), and the death flow in [DEC-099](./DEC-099-use-single-player-pause-and-results-flow.md). [DEC-122](./DEC-122-use-destructible-rocks-for-health-packs.md) later removes temporary-effect pickups and identifies the breakables and their only possible drop; [DEC-123](./DEC-123-replenish-destructible-rocks-around-the-player.md) supplies the ongoing availability model.
