---
doc_id: DEC-056
title: Give Cluster Mortar an Interdiction Payload
status: accepted
authoritative: false
---

# DEC-056 — Give Cluster Mortar an Interdiction Payload

## Decision

Cluster Mortar's `A`-funded functional variant is **Interdiction Payload**. The funding assignment is fixed by [DEC-060](./DEC-060-balance-native-branch-funding.md).

The shell retains the base weapon's automatic concentration targeting, committed and telegraphed impact point, travel delay, and initial area explosion. After impact, the marked blast footprint becomes a temporary interdiction field that continues damaging enemies and slows their movement while they remain inside it.

## Status

Accepted behavior and funding color; field rules and numeric tuning open.

## Context

The functional branch needs to build visibly upon the mortar rather than merely trading burst damage for control. Retaining the initial explosion and adding a lingering field makes the improvement clear while shifting the weapon's role toward area denial.

## Considered options

### Replace the explosion with a control field

This creates a distinct role but risks feeling like a loss of the weapon's primary value.

### Leave an interdiction field after the explosion

This preserves the expected mortar impact and adds persistent damage and movement control in the same area.

## Rationale

Interdiction Payload fits the “a bit different in function” category. The player still wants to land a delayed blast on a dense group, but the result also shapes enemy movement, protects territory, and can help keep enemies inside later attacks.

## Consequences

- The initial explosion remains a meaningful damage event.
- The lingering field damages and slows only while an enemy occupies its area unless later status rules establish a brief residual slow.
- Field visuals must communicate both hazardous area and remaining lifetime without resembling mining zones.
- Exact field duration, damage cadence, damage amount, slow strength, stacking, overlap, and boss resistance remain open.
- The field's duration and slow are branch behavior rather than automatically adding common-ore stat tracks.
- All eventual mortar stats must remain meaningful in this branch.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Extends the base behavior in [DEC-054](./DEC-054-cluster-mortar-base-behavior.md). [DEC-060](./DEC-060-balance-native-branch-funding.md) later assigns `A` as its funding resource. Numeric tuning remains open.
