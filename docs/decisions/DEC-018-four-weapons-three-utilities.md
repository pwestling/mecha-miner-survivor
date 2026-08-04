---
doc_id: DEC-018
title: Use Four Weapon Slots and Three Utility Slots
status: accepted
authoritative: false
---

# DEC-018 — Use Four Weapon Slots and Three Utility Slots

## Decision

Each run loadout contains four simultaneous weapon slots and three separate utility slots. The selected mech's signature weapon occupies one weapon slot at deployment, normally leaving three weapon slots available. The mech's inherent trait consumes no slot.

Utility systems occupy utility slots rather than weapon slots. The resource radar occupies one utility slot. Unless an explicit mech-specific exception is added later, all three utility slots are available at deployment.

## Status

Accepted.

## Context

The game needs enough weapon capacity to create a multi-weapon survivor-like build while keeping intentional fabrication choices meaningful. Non-weapon systems such as the resource radar also need capacity that does not compete directly with the player's core damage output.

## Considered options

### One shared equipment capacity

Weapons and utilities could compete for the same slots, but recovery or navigation tools would directly reduce firepower and could worsen an already underpowered run.

### Six or more weapon slots

This more closely resembles the reference game's broad arsenal but risks diluting individual crafting choices and increasing visual or balance complexity.

### Four weapons and three utilities

This supports a signature weapon plus three crafted weapons and leaves a parallel capacity for meaningful support systems.

## Rationale

Four weapons preserve multi-weapon interactions while keeping each weapon a substantial part of the build. Three utility slots create room for navigation, mining, defense, or mobility choices without sacrificing a weapon merely to access a safety valve.

## Consequences

- The standard deployment begins with one of four weapon slots occupied and three weapon slots available.
- Weapon-slot and utility-slot occupancy must be visible during fabrication and normal play.
- The resource radar consumes one utility slot.
- A mech's inherent trait is not equipment and consumes no slot.
- DEC-100 later forbids replacement and dismantling; DEC-109 later gives every non-radar utility three capped ore ranks; DEC-116 fixes the initial concepts and stacking rules. Mech-specific starting exceptions remain open.
- Content and balance must make both weapon and utility slot choices meaningful rather than automatically filling every slot with a universal best set.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)

## Supersedes / superseded by

Resolves equipment capacity left open by [DEC-009](./DEC-009-ore-powered-directional-resource-radar.md) and [DEC-014](./DEC-014-selectable-mechs-and-signature-weapons.md). No earlier accepted capacity is superseded.
