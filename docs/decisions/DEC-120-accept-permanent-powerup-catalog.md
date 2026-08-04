---
doc_id: DEC-120
title: Accept the Permanent PowerUp Catalog
status: accepted
authoritative: false
validation: progression-and-difficulty-playtest
---

# DEC-120 — Accept the Permanent PowerUp Catalog

## Decision

Accept the thirteen-track account-wide [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md): four combat tracks, four survivability tracks, two mobility/exploration tracks, and three mining/run-economy tracks.

All tracks are visible and purchasable on a fresh profile, have fixed sequential rank prices and explicit caps, and may be set between runs to any active rank up to the purchased rank. The complete catalog costs 9,450 Hyper Gold. Refund PowerUps continues to reset every purchased numerical rank and return its exact paid cost without affecting permanent option unlocks.

The catalog translates relevant *Vampire Survivors* permanent statistics into this game's vocabulary while excluding XP, coin, random-offering, pickup-magnet, projectile-count, and universal-projectile-speed upgrades that conflict with the accepted mining and weapon systems.

## Status

Accepted as the initial permanent numerical progression baseline. Exact values and prices remain subject to a catalog-wide progression balance pass.

## Rationale

The selected effects give Hyper Gold an immediately understandable use, cover all four previously required domains, and create genuine allocation choices between direct combat safety and stronger in-run resource development. Low initial prices permit progress after an imperfect extraction, while rising costs make full completion a long-term objective.

The fully upgraded account is meaningfully stronger early but gains no starting equipment, specialized materials, automatic navigation, or resource acquisition. The build still depends on exploring, mining, and fabricating. Fixed caps and an explicit composite envelope make the standard difficulty testable at fresh and max progression states.

## Consequences

- The accepted initial tracks are Weapons Calibration, Cycle Optimizer, Field Geometry, Persistence Lattice, Hull Reinforcement, Ablative Armor, Repair Nanites, Emergency Reboot, Servo Overdrive, Survey Optics, Extraction Tuning, Tether Amplifier, and Ore Assay.
- Emergency Reboot provides one automatic 40%-Hull revival and two seconds of invulnerability per run at its sole rank.
- No PowerUp increases specialized-material units or Hyper Gold, modifies boss payouts, or creates resources without mining.
- PowerUp Attack Rate, Area, Damage, movement, discovery, extraction, zone-radius, Recovery, and ore modifiers share the named-stat boundaries already used by mechs and utilities.
- Active ranks can be lowered without refund between runs; ownership remains until the full Refund PowerUps action is confirmed.
- DEC-121 later supplies the initial option catalog and resolves OQ-010's foundational progression structure. OQ-013 remains open for other resource-economy questions, not for either initial permanent spending catalog.
- Exact values must be tuned as a set against the same standard director; it never scales up to cancel purchased power.

## Specification links

- [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)

## Supersedes / superseded by

This supplies the individual effects, caps, prices, and activation rules left open by DEC-092, DEC-093, DEC-095, and DEC-112. It preserves DEC-094's complete free refund rather than replacing it with per-rank sales. [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) later defines the separate initial permanent option catalog.
