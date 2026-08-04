---
doc_id: DEC-037
title: Use Unique Weapons and Soft Profile Balance
status: accepted
authoritative: false
---

# DEC-037 — Use Unique Weapons and Soft Profile Balance

## Decision

A mech cannot equip more than one copy of the same weapon. Signature weapons and fabricated weapons follow the same uniqueness rule.

The 15 weapon concepts, their two-color recipe positions, and their fixed third branch colors will be designed together. During this work, the six resources use abstract labels rather than predetermined fictional or mechanical themes. Player-facing resource identities will be derived later from the completed relationship graph rather than constraining weapon ideation now.

Resource profiles do not need equal representation of every tactical role. Individual runs may be noticeably lopsided if they remain viable and interesting. Catalog or graph assignments must change when an imbalance recurs systematically across profiles or creates combinations that feel impossible, obviously noncompetitive, or worth immediately abandoning after the survey reveal.

## Status

Accepted.

## Context

The selected signature weapon belongs to the normal 15-weapon pair catalog. When its recipe pair is present, counting that recipe among six supported weapons overstates the number of new weapons available for the three open slots unless duplicates are explicitly forbidden or allowed.

The resource graph also needs weapon concepts and third-color assignments. Giving each abstract resource a strong theme before designing the arsenal could unnecessarily restrict which weapon belongs on which pair. Conversely, demanding perfectly even tactical-role coverage in every profile could make the catalog formulaic and erase desirable run-to-run texture.

## Considered options

### Allow duplicate weapons

This increases stacking possibilities but weakens weapon identity, complicates automatic attack readability, and lets a preferred weapon consume multiple slots.

### Forbid duplicate weapons

This makes each slot a distinct addition to the build and preserves the intended breadth of the four-weapon loadout.

### Theme resources before designing weapons

This creates immediate fictional coherence but turns every pair recipe and third branch into a thematic constraint before the weapon space is understood.

### Require every profile to cover every combat role evenly

This minimizes bad rolls but can homogenize profiles and overconstrain a 15-weapon catalog.

### Use soft profile balance with viability floors

This permits distinctive, skewed runs while targeting only repeated structural problems and profiles players rationally abandon.

## Rationale

Unique weapons make the small four-slot loadout legible and ensure that crafting adds new behavior rather than copies of an already automatic attack. Co-designing concepts and graph placement allows weapon mechanics, recipe relationships, and third branches to inform one another. Soft balance preserves the desired adaptation pressure without treating every unusual profile as a defect.

## Consequences

- Fabrication cannot add a weapon already equipped on the mech.
- If the signature weapon's recipe pair is supported by the profile, the player has five different additional pair-weapons to consider for three open slots; otherwise the player has six.
- The fabrication interface must distinguish an already-equipped recipe from absent-resource and insufficient-resource states.
- Replacement, dismantling, and whether an equipped weapon can be transformed into another weapon remain open, but none may produce duplicate equipped identities.
- Working design materials use neutral labels such as `A` through `F`; these are not final player-facing names or color-only accessibility solutions.
- The catalog receives no hard quota requiring every profile to offer every targeting geometry, damage cadence, range band, or combat role.
- Validation must still identify profiles with no plausible way to survive the combined horde and boss schedule or a strong incentive to restart immediately after the survey.
- A lopsided profile is acceptable when it creates a playable adaptation problem and does not recur as a pervasive graph bias.
- Final resource identities should be assigned only after their complete recipe and branch relationships are visible.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This resolves the duplicate-weapon rule left open by [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md) and narrows its tactical-role validation requirement from even coverage to soft viability guardrails.
