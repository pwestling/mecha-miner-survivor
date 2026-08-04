---
doc_id: DEC-075
title: Accept the Complete Initial Weapon Catalog for Playtesting
status: accepted
authoritative: false
---

# DEC-075 — Accept the Complete Initial Weapon Catalog for Playtesting

> **Supersession note:** DEC-125 has supplied the numerical values that were still open when this concept-catalog decision was accepted. Final names and audiovisual production remain open.

## Decision

The complete high-level design for all 15 initial normal weapons is accepted as the baseline for prototyping and playtesting.

For every weapon, the accepted baseline includes:

- its automatic base targeting, placement, trigger, movement, and damage-delivery pattern;
- exactly three common-ore stat tracks and the fixed properties excluded from ore upgrading;
- one amplification, one functional variant, and one playstyle conversion;
- the specialized resource funding each branch;
- the reinterpretation of existing stats after every branch;
- explicit non-recursion, ownership, cap, self-damage, and other qualitative edge rules where the concept requires them.

DEC-064 completes Attack Drones. DEC-065 through DEC-074 complete Tracking Laser, Mine Layer, Sentry Pod, Orbital Cutters, Arc Emitter, Reactor Pulse, Wake Projector, Scatter Array, Ram Field, and Missile Rack. Earlier decisions complete Rail Lance, Cluster Mortar, Gravity Projector, and Pulse Repeater.

The designs are accepted hypotheses rather than promises that playtesting cannot revise. Exact values and explicitly deferred edge rules remain tuning work.

## Status

Accepted catalog baseline; playtesting and numeric tuning open.

## Resource-assignment validation

Every resource is native to five weapons. Amplification funding is distributed:

- `A:3`, `B:2`, `C:3`, `D:2`, `E:3`, `F:2`.

Functional funding is the complement:

- `A:2`, `B:3`, `C:2`, `D:3`, `E:2`, `F:3`.

Off-color conversion funding remains:

- `A:2`, `B:3`, `C:2`, `D:3`, `E:2`, `F:3`.

This orientation gives every resource both native branch categories and keeps every category count within one of the ideal average of 2.5.

## Rationale

A complete coherent baseline is more useful for prototype comparisons than leaving content mechanically blank until each idea can be tuned in isolation. The catalog deliberately spans direct target locks, facing weapons, ground attacks, autonomous agents, deployables, orbiting contact, radial pulses, route drawing, homing salvos, and movement-dependent contact.

The branches emphasize major visible rules rather than minor percentage bonuses, while the three-stat ceiling keeps fabrication decisions readable. Accepting the whole baseline now allows testing to reveal systemic overlap, underperforming roles, control problems, visual overload, and resource-profile bias.

## Consequences

- Weapon-content questions should start from the accepted catalog rather than inventing unnamed placeholders.
- Prototype data may justify revising a weapon, branch, stat, or mapping; any revision should identify the observed playtest problem it addresses.
- Final numeric values, names, VFX, SFX, descriptions, and implementation order remain open.
- Resource identities were deferred at the time of this decision; [DEC-076](./DEC-076-specialized-resource-identities.md) later resolves the six ordinary specialized materials, and DEC-109 plus DEC-116 later complete the initial utility structure and content.
- Mech identities and traits remain separate content work even though six signature weapons are selected.

## Specification links

- [Weapon Specification Index](../weapons/README.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Assign native branch funding for catalog balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Closes the high-level weapon-content portion of OQ-028. It does not close numeric tuning, fabrication pricing, final presentation, or mech design. Later decisions separately resolve resource identities and utility content.
