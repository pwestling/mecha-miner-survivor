---
doc_id: DEC-060
title: Assign Native Branch Funding for Catalog Balance
status: accepted
authoritative: false
---

# DEC-060 — Assign Native Branch Funding for Catalog Balance

## Decision

Native-resource branch mappings are catalog-balancing assignments rather than separate creative approvals. Once a weapon's amplification and functional variant are accepted, their two recipe resources may be assigned and locked according to the distribution that best supports the catalog as a whole. These assignments do not require individual confirmation unless they introduce a new gameplay consequence or require revisiting an accepted mapping.

Assignments should consider:

- the number of amplifications and functional variants funded by each resource;
- avoiding a resource that overwhelmingly signals only one branch category or weapon behavior;
- branch desirability across the four-resource profiles that expose it;
- signature-weapon guarantees and likely early-run access;
- relationships that may later support coherent player-facing resource identities without prematurely forcing those identities.

For Cluster Mortar, `C` funds **Saturation Cascade** and `A` funds **Interdiction Payload**. Its existing off-color `F` funds **Danger-Close Protocol**.

## Status

Accepted assignment method and accepted Cluster Mortar mapping.

## Context

Every pair-recipe weapon needs one native resource for amplification and the other for functional variation. The abstract resource labels currently have no fictional or mechanical identities, so repeatedly asking for arbitrary pair orientation adds process without improving the creative decision. Distribution can be evaluated more effectively against the growing graph.

Before this assignment, `A` already funded Rail Lance and Gravity Projector amplifications, while `C` funded Pulse Repeater's functional variant. Giving Cluster Mortar's amplification to `C` and its functional branch to `A` produces better category diversity than the reverse.

## Considered options

### Confirm every native mapping independently

This maximizes direct approval but interrupts mechanical co-design for choices best evaluated globally.

### Assign mappings through ongoing graph balancing

This keeps branch design focused on gameplay while preserving a deliberate, documented distribution.

## Rationale

Resource mappings matter through their aggregate availability patterns, not through an isolated `A`-versus-`C` choice. Treating assignment as a balancing pass makes those choices more consistent and allows later mappings to correct developing biases.

## Consequences

- Future accepted weapon branches receive native funding assignments without a separate user question.
- Each mapping is still written into the catalog and decision history.
- A later global rebalance may revise mappings, but must preserve each weapon's recipe pair and fixed off-color unless a new explicit decision changes the graph.
- Resource identities were deferred until enough mappings existed to derive them from the completed catalog; [DEC-076](./DEC-076-specialized-resource-identities.md) later resolves them.
- Cluster Mortar is now fully mapped: `C` amplification, `A` functional variant, and `F` playstyle conversion.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [RES-006 — Resource-color graph for weapon availability](../research/RES-006-resource-color-weapon-graph.md)

## Supersedes / superseded by

Resolves Cluster Mortar's native mapping left open by [DEC-055](./DEC-055-cluster-mortar-saturation-cascade.md), [DEC-056](./DEC-056-cluster-mortar-interdiction-payload.md), and [DEC-059](./DEC-059-cluster-mortar-stat-bundle.md). It establishes the assignment process for later weapons without changing accepted pair recipes or off-colors.
