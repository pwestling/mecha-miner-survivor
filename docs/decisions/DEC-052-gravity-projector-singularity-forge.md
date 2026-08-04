---
doc_id: DEC-052
title: Convert Gravity Projector into a Singularity Forge
status: accepted
authoritative: false
---

# DEC-052 — Convert Gravity Projector into a Singularity Forge

## Decision

Gravity Projector's `B`-funded playstyle conversion is **Singularity Forge**.

The weapon operates on an intentionally slow cycle. Its collection field pulls and damages enemies while accumulating mass from the aliens it affects. When the collection phase ends, that mass is compressed into a micro-singularity round and automatically fired at the strongest valid nearby enemy.

On impact, the round creates a devastating localized singularity at the target position, dealing extreme damage within its affected spot. The shot occurs infrequently enough to read as a major event rather than another ordinary gravity pulse. The quantity and size of enemies processed during collection increase the eventual payoff.

Exact collection duration, mass formula and cap, target priority, travel behavior, impact radius, damage, pull behavior at impact, and fallback behavior without a valid target remain open.

## Status

Accepted behavior; targeting, scaling, edge rules, and numeric tuning open.

## Context

The earlier Gravity Harness proposal replaced remote fields with recurring pulses centered on the mech. That created a close-range pattern but risked overlapping other body-centered weapons and did not produce a sufficiently distinctive payoff. The conversion needs to change why the player seeks dense groups and create a much larger behavioral departure from ordinary field placement.

## Considered options

### Gravity Harness

Center recurring gravity pulses on the mech. This creates close-range risk but overlaps Reactor Pulse's body-centered delivery and offers no singular payoff.

### Singularity Forge

Use ordinary aliens as feedstock for an infrequent, devastating localized strike against a priority target.

## Rationale

Singularity Forge changes Gravity Projector from recurring crowd control into a harvest-and-payoff weapon. The player values dense groups as ammunition, wants them gathered during the collection phase, and anticipates a rare strike capable of severely damaging an elite or boss and anything caught beside it.

## Consequences

- The collection state, stored mass, target selection, firing event, and impact location must all be readable amid horde combat.
- The firing cadence remains intentionally slow; ordinary tuning must not make singularity shots feel frequent.
- The impact is localized rather than global, so positioning the target among other enemies remains valuable.
- Strongest-target selection needs deterministic tie-breaking and clear handling when targets leave range or die before impact.
- Enemies that resist displacement still contribute mass and take impact damage unless later enemy rules explicitly say otherwise.
- Bosses and elites may require special mass-contribution rules to prevent circular scaling or unintended instant kills.
- The conversion must reinterpret every eventual Gravity Projector common-ore stat without adding a mandatory fourth track.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Replaces the unaccepted Gravity Harness proposal. It does not settle Gravity Projector's common-ore stat bundle or numeric tuning.
