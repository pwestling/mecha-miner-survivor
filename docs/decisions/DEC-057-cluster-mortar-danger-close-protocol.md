---
doc_id: DEC-057
title: Convert Cluster Mortar to Danger-Close Protocol
status: accepted
authoritative: false
---

# DEC-057 — Convert Cluster Mortar to Danger-Close Protocol

## Decision

Cluster Mortar's `F`-funded playstyle conversion is **Danger-Close Protocol**.

The mortar stops selecting enemy concentrations. Whenever it fires, it selects the mech's current ground position, locks that position immediately, and displays the delayed impact marker there. The resulting explosion is substantially larger and more devastating than the base shell.

The player supplies the targeting intelligence through movement: remain near or circle the committed marker long enough to lure pursuing aliens into the blast area while surviving their pressure. Moving too far or too early draws the horde away from the impact.

## Status

Accepted behavior; exact blast advantages and numeric tuning open. Owner self-damage is resolved by [DEC-058](./DEC-058-danger-close-no-self-damage.md).

## Context

The off-color branch must create a much different playstyle from automatic concentration targeting. A player-positioned bombardment turns the mortar into a delayed baiting tool while preserving its defining telegraph and committed ground impact.

## Considered options

### Continue automatic enemy targeting with a different payload

This can change the result of a hit but leaves the player's positioning relationship to the weapon largely unchanged.

### Target the mech's position at launch

This makes movement and horde manipulation determine whether the mortar succeeds. A much larger blast makes the risky setup visibly worthwhile.

## Rationale

Danger-Close Protocol changes the player's attention from predicting an automatically chosen cluster to deliberately creating one. It naturally complements mining-zone pressure: holding valuable ground can also bait enemies into a powerful scheduled impact.

## Consequences

- The marker locks at the mech's position when the firing event occurs; it does not follow the mech afterward.
- The branch does not require a valid enemy target to fire.
- Its blast must be visibly and materially more powerful than the base explosion.
- The impact marker must remain distinguishable from enemy area attacks even though it appears near the player.
- The owning mech takes no damage from this explosion under [DEC-058](./DEC-058-danger-close-no-self-damage.md); surrounding enemies and unrelated hazards remain dangerous.
- Exact damage, radius, cadence, travel delay, warning presentation, and behavior near map boundaries remain open.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Completes Cluster Mortar's branch concepts alongside [DEC-055](./DEC-055-cluster-mortar-saturation-cascade.md) and [DEC-056](./DEC-056-cluster-mortar-interdiction-payload.md). It does not settle the two native funding colors, common-ore stats, or numeric tuning. [DEC-058](./DEC-058-danger-close-no-self-damage.md) later resolves owner self-damage.
