---
doc_id: DEC-044
title: Use Immediate Permanent Branch Commitment
status: accepted
authoritative: false
---

# DEC-044 — Use Immediate Permanent Branch Commitment

## Decision

Once a weapon is equipped, each of its three major branches is immediately eligible for purchase if the player has the required specialized resource. A branch has no prerequisite common-ore stat rank, weapon level, elapsed-time milestone, boss defeat, or other progression gate.

Installing a branch is one deterministic specialized-resource purchase. The weapon's existing common-ore stat tracks continue to upgrade the branched behavior and are reinterpreted consistently where the branch changes delivery. A branch should not add a new ore-upgradeable stat track unless that weapon receives an explicit documented exception.

Branch commitment is irreversible for the remainder of the run. The player cannot respec, refund, replace, or overwrite it with another branch. Under the later DEC-100, the weapon itself also cannot be removed or reacquired during the run.

## Status

Accepted.

## Context

The branch system needs a clear relationship to uncapped common-ore ranks. Requiring stat investment before a branch would make access harder to understand and could trap players who find the desired specialized resource early. Allowing respec or branch laundering through weapon replacement would weaken the intended geological commitment.

## Considered options

### Require ore ranks before branching

This creates a conventional upgrade ladder but delays adaptation and adds prerequisites to a system intended to respond directly to mining discoveries.

### Add branch-specific stat tracks

This can support bespoke scaling but expands fabrication complexity across 45 branch outcomes.

### Allow paid respec

This reduces commitment risk but lets a sufficiently rich run erase the consequences of resource and branch choices.

### Use immediate, permanent commitment with inherited stats

This keeps branch access legible, preserves earlier ore investment, and makes the choice consequential for the run.

## Rationale

The player can react to geology as soon as the relevant resource is extracted. Existing stat tracks keep common ore useful before and after branching, while irreversibility preserves distinct weapon versions and prevents the unlimited fabrication menu from becoming a free experimentation screen during live runs.

## Consequences

- Fabrication shows all three branches immediately after a weapon is equipped.
- Resource affordability and mutual exclusion are the normal branch-purchase gates.
- Branch previews show how every existing stat affects the resulting behavior.
- Existing stat ranks and future purchases continue to apply after branching.
- New branch-only stat tracks require an explicit exception and must be documented in the weapon catalog.
- A confirmation step must clearly state that the choice lasts for the rest of the run.
- No ordinary respec or refund exists.
- Weapons cannot be dismantled, replaced, or removed under DEC-100, so no equipment transaction can reset a selected branch or its stat ranks.
- Later decisions fix branches at two specialized units and common-ore stat prices through the shared depth curve; follow-on branch upgrades remain open.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)
- [DEC-100 — Commit installed weapons and utilities](./DEC-100-commit-installed-weapons-and-utilities.md)

## Supersedes / superseded by

This resolves branch prerequisites, ordinary respec, and branch-state persistence left open by [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md), [DEC-027](./DEC-027-mutually-exclusive-weapon-branches.md), and [DEC-040](./DEC-040-three-branch-transformation-gradient.md). [DEC-100](./DEC-100-commit-installed-weapons-and-utilities.md) supersedes only the contingency for weapon removal by prohibiting removal altogether.
