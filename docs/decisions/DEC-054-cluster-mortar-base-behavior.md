---
doc_id: DEC-054
title: Give Cluster Mortar Delayed Committed Area Targeting
status: accepted
authoritative: false
---

# DEC-054 — Give Cluster Mortar Delayed, Committed Area Targeting

## Decision

Cluster Mortar automatically selects the densest enemy concentration within its targeting range whenever its firing cadence completes. It launches an arcing shell toward the selected ground position and immediately locks that impact point; the shell does not retarget enemies or redirect after launch.

A visible ground marker telegraphs the committed impact area during the shell's travel delay. When the shell arrives, it explodes and damages every valid enemy within its blast area. Enemies that leave the marked area before impact avoid the explosion.

## Status

Accepted base behavior; targeting details, stats, branches, edge rules, and numeric tuning open.

## Context

Cluster Mortar's catalog identity was delayed area bombardment against enemy concentrations. Its base rules need to distinguish it from immediate ground effects and make both successful prediction and misses understandable during dense combat.

## Considered options

### Track the selected group until impact

This improves reliability but weakens the mortar fantasy and removes much of the delayed attack's distinctiveness.

### Commit to a telegraphed ground position

This preserves automatic targeting while allowing enemy movement to matter. The marker explains both where the attack will land and why a moving group escaped it.

## Rationale

Committed delayed impact creates a readable strength and weakness: the mortar can damage many enemies at once, but its effectiveness depends on cluster movement after launch. It also creates clear room for branches that improve saturation, alter targeting, or convert the delivery pattern.

## Consequences

- The impact marker must remain legible without being confused with enemy warnings or mining-zone boundaries.
- Target selection uses enemy concentration rather than simply the closest enemy.
- The shell continues toward its locked location if the original enemies die or leave range.
- Exact density scoring, targeting range, cadence, travel delay, blast radius, damage, and terrain interaction remain open.
- Whether any of those properties become common-ore stats is a later bundle decision.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Refines the Cluster Mortar concept fixed by [DEC-043](./DEC-043-fifteen-weapon-graph-assignment.md). It does not settle common-ore stats, branch behaviors, or numeric tuning.
