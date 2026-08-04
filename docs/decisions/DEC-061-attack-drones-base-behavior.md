---
doc_id: DEC-061
title: Use an Autonomous Indestructible Attack-Drone Squadron
status: accepted
authoritative: false
---

# DEC-061 — Use an Autonomous, Indestructible Attack-Drone Squadron

## Decision

Attack Drones maintains a small squadron of indestructible autonomous drones.

When enemies are available within the squad's operational range, each drone independently acquires a nearby valid target, flies out from the mech, and strafes that target with short-range automatic fire. Different drones may select different targets. A drone retargets when its target becomes invalid and returns to orbit the mech when no valid target remains.

The drones require no manual combat input. Enemies cannot target, damage, destroy, or physically block them.

## Status

Accepted base behavior; stats, branches, targeting details, movement rules, and numeric tuning open.

## Context

Attack Drones needs to occupy the autonomous-agent niche without becoming another homing-projectile weapon or a stationary deployable. Independent mobile attackers create visible squad behavior and distribute damage without adding controls.

## Considered options

### Use destructible combat pets

This creates recovery and replacement decisions but adds enemy targeting rules, health presentation, and periods when the equipped weapon may be unavailable.

### Use indestructible weapon agents

This treats drones as the persistent delivery mechanism for an equipped weapon. Their target choice and travel still affect performance without requiring pet-health management.

## Rationale

Independent strafing drones are readable, distinct from direct-fire weapons, and compatible with movement-only controls. Indestructibility keeps attention on positioning, mining, and fabrication rather than maintaining subordinate units.

## Consequences

- Drones may divide their fire among different enemies.
- Idle drones orbit the mech as a presentation and readiness state, not as damaging orbital weapons.
- Drone bodies do not deal contact damage unless a later branch explicitly says otherwise.
- Short-range fire, rather than collision, is the base damage source.
- Exact squad size, operational range, target scoring, flight speed, strafe geometry, firing cadence, projectile behavior, and retarget timing remain open.
- Terrain traversal and whether drones can cross otherwise impassable geometry remain open.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Refines the Attack Drones concept fixed by [DEC-043](./DEC-043-fifteen-weapon-graph-assignment.md). It does not settle common-ore stats, branch behaviors, funding orientation, or numeric tuning.
