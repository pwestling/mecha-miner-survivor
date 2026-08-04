---
doc_id: DEC-035
title: Integrate Utilities Without Fixed Weapon Pairing
status: accepted
authoritative: false
---

# DEC-035 — Integrate Utilities Without Fixed Weapon Pairing

## Decision

Utility items beyond the common-ore resource radar should participate in the specialized-resource economy, but their recipes must not make each utility repeatedly appear only alongside the same narrow set of weapons.

The leading model in this record gave each utility two alternative single-color recipes: it could be fabricated with resource `A` **or** resource `B`, rather than requiring both. [DEC-109](./DEC-109-use-single-material-utilities-with-three-ore-ranks.md) later replaces that proposal with twelve non-radar utilities, two assigned to each material, each costing one unit of its single assigned material.

## Status

Accepted direction; its alternative-recipe structure is superseded by DEC-109.

## Context

Resource-gated base weapons create run variety, but applying the same two-color `AND` recipe structure to utilities would repeatedly bind a utility to the weapons using that pair. Utilities should still make mining and resource allocation matter while remaining broadly useful across different weapon builds.

## Considered options

### Common-ore-only utilities

This guarantees access but largely removes utilities from the randomized resource economy.

### The same two-color `AND` recipes as weapons

This creates availability variation but strongly couples each utility to a recurring weapon pair.

### Alternative single-color `OR` recipes

This makes a utility broadly available while changing which present resource can most economically purchase it.

## Rationale

Alternative recipes make utilities flexible resource sinks rather than extensions of one weapon family. They preserve route and scarcity decisions without requiring the same weapon combination to be available.

## Consequences

- The resource radar remains an explicit common-ore-only safety valve.
- Other utilities should consume specialized resources under fixed visible recipes; DEC-109 later replaces the alternative-recipe proposal with one assigned material per utility.
- Under this record's original model, if both listed resources were present, the player could choose which listed recipe to pay. DEC-109 supersedes that rule with one assigned material per utility.
- DEC-109 and DEC-116 later balance the catalog at exactly two accepted utilities per specialized material.
- Every valid profile must offer enough utility choices to fill three slots, including or excluding the radar according to player preference.
- DEC-116 later resolves the exact concepts, assignments, and rank effects; numeric tuning remains open.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [RES-006 — Resource-color graph for weapon availability](../research/RES-006-resource-color-weapon-graph.md)
- [DEC-116 — Accept the initial utility catalog](./DEC-116-accept-initial-utility-catalog.md)

## Supersedes / superseded by

Narrows utility acquisition left open by [DEC-018](./DEC-018-four-weapons-three-utilities.md). [DEC-109](./DEC-109-use-single-material-utilities-with-three-ore-ranks.md) later supersedes its two-alternative model, and [DEC-116](./DEC-116-accept-initial-utility-catalog.md) completes the initial content. None alters the common-ore radar recipe established by [DEC-009](./DEC-009-ore-powered-directional-resource-radar.md).
