---
doc_id: DEC-040
title: Use a Three-Level Weapon-Branch Transformation Gradient
status: accepted
authoritative: false
---

# DEC-040 — Use a Three-Level Weapon-Branch Transformation Gradient

## Decision

Every weapon has exactly three mutually exclusive major branches with three different degrees of transformation:

1. **Amplification branch — “samey but bigger and better.”** It preserves the base weapon's targeting, positioning demands, and recognizable play pattern while substantially increasing or expanding what that pattern accomplishes.
2. **Functional-variant branch — “a bit different in function.”** It preserves the weapon's core identity but changes one important behavior, use case, or tactical emphasis.
3. **Playstyle-conversion branch — “much different in play style.”** It remains recognizably derived from the base weapon but substantially changes how the player positions, routes, targets through movement, or builds around it.

The categories describe distance from the base weapon, not a power hierarchy. All three branches should be credible run choices. The playstyle-conversion branch is not automatically stronger because it is more transformative.

Each branch uses one of the weapon's three fixed specialized-resource colors. One recipe color funds amplification, and the other recipe color funds the functional variant; their assignment is weapon-specific. The assigned off-color third resource always funds the playstyle conversion.

## Status

Accepted.

## Context

Earlier decisions established three mutually exclusive resource-funded branches but described all branches as materially changing play patterns. The intended catalog instead needs one reliable route that feels like an expanded version of the base weapon, one moderate variant, and one branch capable of reorganizing the player's relationship with the weapon.

## Considered options

### Make every branch equally transformative

This maximizes novelty but denies players a straightforward way to deepen a weapon they already enjoy.

### Make every branch a numerical upgrade

This is easy to compare but does not create enough run identity or adaptation.

### Use a transformation gradient

This supports conservative, moderate, and radical development choices within every weapon family.

## Rationale

The gradient gives players control over how much they want an upgraded weapon to depart from its familiar baseline. It also provides a repeatable structure for authoring 45 branches without demanding 45 equally radical redesigns.

## Consequences

- Every catalog row requires one amplification, one functional-variant, and one playstyle-conversion branch.
- The amplification branch must be more substantial than an ordinary common-ore stat rank even though it preserves the base play pattern.
- Amplification may use effects such as additional simultaneous output, larger geometry, penetration, coverage, or a new payoff that reinforces existing use; exact forms remain weapon-specific.
- The functional variant should change at least one important targeting, timing, delivery, control, or damage behavior without replacing the weapon's entire tactical identity.
- The playstyle conversion should cause a meaningful change in positioning, routing, movement incentives, range preference, or build interaction.
- Branch previews must communicate both the retained core behavior and the degree of change.
- Branch balance compares total value, risks, and synergies rather than assuming that more novelty deserves more raw power.
- Each catalog row must designate which of its two recipe colors funds amplification and which funds the functional variant.
- The assigned off-color always funds the playstyle conversion, making that rarer geological coincidence the most behaviorally transformative path.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This narrows the branch-content requirements in [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md) and [DEC-027](./DEC-027-mutually-exclusive-weapon-branches.md). It does not change their determinism or mutual exclusivity. The accepted off-color mapping also specializes the three-resource structure established by [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md).
