---
doc_id: DEC-038
title: Use a Broad Automatic-Weapon Taxonomy
status: accepted
authoritative: false
---

# DEC-038 — Use a Broad Automatic-Weapon Taxonomy

## Decision

Any equipped system whose primary player-facing purpose is dealing automatic damage may be designed as a weapon. Valid weapon forms include conventional guns and beams as well as autonomous drones, deployable turrets, mines, contact-damage auras, and movement-dependent ramming systems.

Weapons may use widely different automatic targeting and delivery rules, including nearest-enemy targeting, movement-direction attacks, radial patterns, orbiting attacks, automatically selected ground locations, and autonomous agents. These variations do not add manual aiming, firing, deployment, or activation inputs; movement remains the only direct baseline combat control.

Utilities remain primarily support systems. A utility may interact with enemies or weapons, but a system whose central function is sustained automatic damage belongs in a weapon slot unless explicitly established as an exception.

## Status

Accepted.

## Context

The 15-weapon catalog needs enough mechanical breadth to make pair-graph profiles and four-weapon builds feel different. Restricting weapons to conventional projectiles would unnecessarily narrow the catalog, while allowing manual placement or activation would undermine the movement-centered survivor-like control model.

## Considered options

### Limit weapons to direct guns and launchers

This is visually familiar but makes 15 distinct base behaviors harder to sustain and pushes unusual damage delivery into ambiguous utility categories.

### Treat every deployable or autonomous system as a utility

This preserves a narrow weapon definition but overloads the three utility slots with systems whose actual role is damage.

### Classify by primary purpose

Damage-first automatic systems are weapons; support-first systems are utilities. Both can use varied behaviors without changing controls.

## Rationale

A primary-purpose boundary is understandable to players and useful to content designers. Broad delivery patterns create positioning differences while the lack of direct activation preserves the intended attention model: movement, navigation, dodging, mining-zone commitment, and fabrication choices.

## Consequences

- A drone, turret, or mine can occupy one of four weapon slots when its primary output is automatic damage.
- A movement-dependent weapon may reward facing or velocity without requiring a separate attack input.
- Deployables choose their placement or trigger conditions automatically.
- Every weapon specification must completely define its automatic targeting, placement, trigger, return, and retargeting behavior as applicable.
- Weapon visuals must distinguish allied autonomous agents and hazards from aliens and hostile hazards.
- Utility design must not become a back door for adding extra sustained-damage weapons beyond the four-weapon limit.
- Exact targeting rules remain weapon-specific content rather than one universal combat rule.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-009 — How does the mech fight and move?](../open-questions.md#oq-009--how-does-the-mech-fight-and-move)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This specializes the automatic-attack and movement-only control rules in [DEC-001](./DEC-001-vampire-survivors-combat-reference.md) and [DEC-019](./DEC-019-movement-only-combat-controls.md) without adding direct combat inputs.
