---
doc_id: DEC-034
title: Gate Base Weapons Through the Specialized-Resource Profile
status: accepted
authoritative: false
---

# DEC-034 — Gate Base Weapons Through the Specialized-Resource Profile

## Decision

Common ore alone cannot fabricate every unlocked base weapon. Base-weapon recipes require specialized ordinary resources, and each run's selected resource types should expose only a substantial subset of the unlocked base-weapon arsenal.

DEC-036 resolves the numeric model: six specialized resource families exist, four appear per run, and the complete pair graph defines 15 base weapons of which exactly six are theoretically craftable from any profile. Under DEC-040, the two endpoint resources fund amplification and functional variation while a fixed distinct third resource funds playstyle conversion.

## Status

Accepted; numeric parameters resolved by DEC-036.

## Context

If every unlocked base weapon is always purchasable with universal common ore, players can repeatedly force a favorite or solved four-weapon loadout. The randomized resource profile should constrain base-weapon availability as well as branch economics while still offering enough coherent choices to fill the run loadout.

## Considered options

### Common-ore-only base weapons

This guarantees access but allows the same preferred base arsenal every run.

### Random weapon offers

This varies builds but recreates arbitrary reward rolls and can be exploited through fishing if offers refresh.

### Fixed recipes gated by randomized specialized resources

This makes availability deterministic once geology is known and connects weapon variation directly to exploration and mining.

## Rationale

Resource-gated base weapons preserve player agency within a run-specific possibility space. Showing the resource profile after deployment lets players understand the theoretical arsenal, while finding and extracting the required deposits remains the spatial challenge.

## Consequences

- Every base weapon requires one or more specialized-resource recipe requirements unless explicitly exempted.
- The signature starting weapon remains usable because it is equipped before resource availability matters.
- Every valid resource profile exposes six pair recipes and, after excluding the equipped signature from duplication, five or six distinct choices for three open slots.
- Fabrication must distinguish theoretically available recipes from recipes blocked by absent resource types and recipes that are possible but not yet affordable.
- Resource-profile generation and weapon-recipe design must be validated together rather than independently.
- The available fraction must prevent universal favorite builds while providing meaningful choice for three open weapon slots. DEC-037 permits tactically lopsided profiles and replaces even role-coverage expectations with soft viability and systemic-bias checks.
- DEC-036 fixes the combinatorial model at six total resource families, four present per run, and 15 unique pair-weapons.

## Specification links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [RES-006 — Resource-color graph for weapon availability](../research/RES-006-resource-color-weapon-graph.md)

## Supersedes / superseded by

Narrows the fixed-catalog model in [DEC-008](./DEC-008-fixed-blueprints-randomized-resource-profiles.md): blueprints and recipes remain fixed, but absent specialized resources can make a base weapon impossible to fabricate during that run. [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md) later resolves this decision's numeric and signature-weapon questions. [DEC-037](./DEC-037-unique-weapons-and-soft-profile-balance.md) resolves duplicate equipment and the profile-balance standard.
