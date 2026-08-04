---
doc_id: DEC-048
title: Give Pulse Repeater a Suppressive Functional Branch
status: accepted
authoritative: false
---

# DEC-048 — Give Pulse Repeater a Suppressive Functional Branch

## Decision

Pulse Repeater's `C`-funded functional variant is **Suppressive Sequencer**.

The branch changes automatic target selection to favor nearby enemies that have not been struck by the most recent pulses. This distributes rapid fire across the approaching horde instead of repeatedly concentrating it on the nearest target. Each hit also briefly slows the affected enemy.

The weapon remains an automatic, rapid, directly targeted projectile weapon. The targeting memory window, slow strength and duration, target-scoring rules, and behavior when every valid target was recently struck remain open for tuning.

## Status

Accepted behavior; targeting details and tuning open.

## Context

Pulse Repeater's base behavior concentrates fire on the nearest enemy, while its amplification removes projectile travel time. Its functional branch needs to change the weapon's role without replacing its recognizable rapid-pulse behavior or becoming a simple damage increase.

## Considered options

### Continue focusing the nearest target with an added status effect

This would add utility but would not materially change how the weapon distributes its output.

### Distribute shots and add a brief slow

This trades concentrated damage for broader horde suppression and gives the weapon a distinct tactical role while preserving its automatic targeting and firing rhythm.

## Rationale

Suppressive Sequencer fits the intended “a bit different in function” category. It remains recognizably Pulse Repeater, but the player values it for controlling a wider section of the horde rather than rapidly removing the single closest threat.

## Consequences

- The branch must make its recent-hit targeting behavior legible through targeting, impact, or enemy-status feedback.
- The slow is a branch behavior, not automatically a new common-ore stat track.
- The branch's total power should account for reduced focus fire as well as the defensive value of distributed slows.
- An enemy may still be targeted again; the system favors untagged candidates rather than permanently excluding recent targets.
- Exact behavior under very small enemy counts remains an edge rule to specify.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This accepts the Suppressive Sequencer proposal recorded after [DEC-045](./DEC-045-first-signature-amplification-branches.md). It does not settle Pulse Repeater's common-ore stat bundle, playstyle conversion, or numeric tuning.
