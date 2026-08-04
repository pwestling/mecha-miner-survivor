---
doc_id: DEC-051
title: Give Gravity Projector a Two-Stage Slingshot Branch
status: accepted
authoritative: false
---

# DEC-051 — Give Gravity Projector a Two-Stage Slingshot Branch

## Decision

Gravity Projector's `D`-funded functional variant is **Gravity Slingshot**.

Each automatically placed field begins with the base weapon's damaging inward pull, gathering affected enemies toward its center. At the end of the pulse, the field produces a second damaging burst that hurls the gathered cluster away from the mech.

The initial grouping window, second hit, and player-relative launch are fixed behaviors. Exact phase duration, second-hit damage, launch force, affected-target rules, and whether launch direction samples the mech at deployment or detonation remain open for tuning.

## Status

Accepted behavior; edge rules and numeric tuning open.

## Context

The original Repulsor Array proposal simply replaced the inward pull with an outward push. That sacrificed the base weapon's useful grouping and did not present an obvious improvement. The functional branch needs a distinct control role while still building upon the weapon players chose.

## Considered options

### Repulsor Array

Reverse the field's force throughout its pulse. This opens space but discards grouping and can scatter enemies before concentrated attacks capitalize on them.

### Gravity Slingshot

Retain the damaging pull, then add a second damaging phase that launches the gathered enemies away from the mech.

## Rationale

Gravity Slingshot makes the upgrade legible: it keeps the original field, hits twice, and converts a gathered cluster into player-relative space. Its launch phase changes the field's function without making the base behavior irrelevant.

## Consequences

- The field must clearly telegraph its inward and launch phases.
- The launch direction is away from the mech rather than merely radially outward from the field, so the branch reliably creates space on the player's side.
- Enemies resistant or immune to displacement must still receive the second damage event unless later enemy rules explicitly say otherwise.
- If pull force becomes an ore stat, it must visibly inform both the inward pull and launch force.
- The second phase is branch behavior rather than a separate ore-upgradeable stat.
- Exact interactions with obstacles, other enemies, bosses, and simultaneous fields remain open.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Replaces the unaccepted Repulsor Array proposal. It does not settle Gravity Projector's common-ore stat bundle, playstyle conversion, or numeric tuning.
