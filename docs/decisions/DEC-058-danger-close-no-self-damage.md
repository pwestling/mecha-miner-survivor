---
doc_id: DEC-058
title: Make Danger-Close Protocol Harmless to Its Owner
status: accepted
authoritative: false
---

# DEC-058 — Make Danger-Close Protocol Harmless to Its Owner

## Decision

Danger-Close Protocol's explosion does not damage the mech that owns and fires it.

The branch's danger comes from enemy pressure during the delay. The player must remain near the committed impact marker long enough to draw pursuing aliens into the blast area, which also allows those enemies to surround or strike the mech. The player may still occupy the blast area when it detonates without receiving damage from the mortar itself.

## Status

Accepted branch rule.

## Context

Danger-Close Protocol intentionally targets the mech's position at launch. Making that blast harm its owner would introduce a unique self-damage rule and could turn an automatic weapon into unavoidable punishment when movement routes close.

## Considered options

### Allow self-damage

This heightens the danger-close fantasy but can punish the player for an automatically scheduled firing event and requires a broader self-damage framework.

### Exempt the owning mech

This keeps the baiting and horde-pressure challenge while making the weapon reliable under the game's automatic-attack controls.

## Rationale

The surrounding horde already supplies the intended risk. Owner immunity lets the player focus on positioning enemies inside the marker rather than tracking an exceptional friendly-fire rule.

## Consequences

- The owner may remain inside the explosion at detonation.
- Enemy collision, contact attacks, projectiles, and other hazards remain dangerous inside the marked area.
- Presentation must identify the marker as a player-owned attack rather than an enemy hazard.
- This decision applies specifically to Danger-Close Protocol and does not by itself establish a universal rule for every future player weapon or environmental effect.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [DEC-057 — Convert Cluster Mortar to Danger-Close Protocol](./DEC-057-cluster-mortar-danger-close-protocol.md)

## Supersedes / superseded by

Resolves the owner self-damage question left open by [DEC-057](./DEC-057-cluster-mortar-danger-close-protocol.md). Other friendly-fire and self-damage rules remain open unless separately accepted.
