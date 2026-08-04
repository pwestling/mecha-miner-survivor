---
doc_id: DEC-041
title: Use an Equal-Tier Base-Weapon Catalog
status: accepted
authoritative: false
---

# DEC-041 — Use an Equal-Tier Base-Weapon Catalog

## Decision

All 15 normal base weapons belong to the same intended power tier. The catalog contains no deliberately weak starter weapons, higher-tier replacement weapons, or recipe pairs that are inherently more prestigious or powerful than others.

Every weapon must be independently useful when first equipped and capable of anchoring a successful build. Equal tier does not require identical damage, range, safety, ease of use, cost, or performance in every situation; weapons may exchange strengths, weaknesses, targeting demands, and synergy potential.

## Status

Accepted.

## Context

Every resource profile exposes a different subset of the catalog, and six weapons serve as initial signatures. If some pair positions are intentionally lower tier, profiles and mechs containing them become disadvantaged before player decisions or mining performance matter.

## Considered options

### Use explicit weapon tiers

This creates a conventional progression ladder but makes geology partly determine whether the player receives inferior or superior equipment access.

### Let signature weapons be stronger

This strengthens mech identity but makes the signature slot structurally mandatory and undermines shared-catalog fabrication.

### Use one sidegrade tier

Every weapon is a complete build option with situational strengths, while ore investment and branches provide vertical growth during the run.

## Rationale

The resource profile is meant to create different possibility spaces, not roll weapon quality. Equal-tier weapons preserve the value of deterministic crafting and let any catalog weapon become a present or future signature without redesigning its baseline power.

## Consequences

- Every unupgraded weapon needs a functional baseline automatic attack.
- No weapon exists mainly to be replaced by another catalog weapon later in the run.
- Recipe position and resource color cannot communicate a power tier.
- Signature status grants free starting access, not a stronger version of the weapon unless a later explicit mech trait says otherwise.
- Fabrication costs may vary for economy or behavior reasons, but higher cost must not silently define a superior weapon tier.
- Balance compares whole-weapon performance across different situations rather than forcing identical damage output.
- Some weapons may be easier or more specialized than others while remaining credible build anchors.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This adds a catalog-wide balance rule to [DEC-034](./DEC-034-gate-base-weapons-by-resource-profile.md), [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md), and [DEC-039](./DEC-039-six-mech-initial-roster.md). It does not require equal tuning values or identical situational performance.
