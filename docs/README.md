---
doc_id: GDD-INDEX
title: Gameplay Specification Index
status: active
authoritative: true
---

# Gameplay Specification

This directory is the canonical description of everything a player can perceive, do, learn, earn, encounter, configure, or experience in the game. It intentionally excludes technical architecture and implementation details unless they create a player-visible constraint.

## Start here

1. Read [Documentation Conventions](./conventions.md) to understand authority, certainty, IDs, and terminology.
2. Read the foundation and gameplay documents listed in [Specification map](#specification-map) in order.
3. Consult [Glossary](./glossary.md) for the canonical meaning of game-specific terms.
4. Consult [Open Questions](./open-questions.md) before making an assumption.
5. Consult [Decision Log](./decisions/README.md) when the reason behind a rule matters.
6. Consult [Research Index](./research/README.md) for externally sourced facts and comparisons.

When a player-facing standard-mode detail is not stated elsewhere, apply the bounded *Vampire Survivors* precedent in [DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md) before treating it as unspecified. Explicit project decisions always override that precedent.

## Scope boundary

This specification covers player-visible behavior, including:

- The intended fantasy, emotional arc, and design pillars.
- Player verbs, controls, rules, feedback, and failure states.
- Game loops at moment-to-moment, run/session, and long-term timescales.
- Characters, equipment, abilities, enemies, environments, encounters, rewards, and other content.
- Progression, resources, economies, unlocks, difficulty, balance intent, and replayability.
- Modes, menus, HUD, tutorials, settings, accessibility, audio, visual presentation, narrative delivery, and social features.
- Edge cases and system interactions that alter what the player experiences.

The [Technical Design Specification](./technical/README.md) describes how the game is implemented. A technical constraint belongs here only when the player can observe its consequence—for example, supported player count, save behavior, input latency targets, or deterministic rules.

## Specification map

The detailed map will grow with the design. Planned domains are listed below so omissions remain visible; a domain receives its own linked document only once it contains substantive information.

| Order | Domain | Coverage status |
| --- | --- | --- |
| 00 | [Vision, fantasy, and design pillars](./00-game-vision.md) | Core promise, standard audience, scope, and bounded reference rule established |
| 10 | [Player experience and game loops](./10-core-game-loop.md) | Standard loop and foundational behavior established; content and tuning open |
| 20 | [Run structure, timer, bosses, and mission extraction](./20-run-structure-and-timing.md) | Standard 35-minute pause, failure, results, four bosses, and wave flow established; tuning open |
| 30 | [Combat, weapons, movement, and camera](./30-combat-weapons-movement-camera.md) | Direct movement, collision, fixed camera, automatic combat, drops, and horde director established; weapon values accepted separately in document 71 |
| 31 | [Initial alien and boss roster](./31-initial-alien-roster.md) | Ten ordinary identities, elite treatment, four bosses, profiles, behaviors, and feasible initial Hull baselines accepted |
| 32 | [Standard wave and beacon schedule](./32-standard-wave-and-beacon-schedule.md) | Complete 35-minute composition, density, event, boss-pressure, crescendo, and Hyper Gold response baseline accepted |
| 35 | [Playable mechs and starting loadouts](./35-playable-mechs.md) | Selectable roster, signature weapons, traits, availability, and selection rules established; tuning and later roster growth open |
| 36 | [Initial mech catalog](./36-initial-mech-catalog.md) | Six mech identities, traits, silhouettes, fresh-profile availability, and selection rules accepted; numeric tuning open |
| 40 | [Mining and extraction](./40-mining-and-extraction.md) | 20 standard seams, 8 rich seams, material geodes, and three 100-unit Hyper Gold beacons established; tuning open |
| 50 | [Maps, resource surveys, exploration, and navigation](./50-maps-resources-and-navigation.md) | Large randomized maps, ore and geode counts, fogged navigation, survey, and seven-category radar captured |
| 51 | [Standard map generation contract](./51-standard-map-generation-contract.md) | First-pass scale, regions, topology, distribution, deployment fairness, landmarks, boundary, and valid-seed rules accepted; tuning remains playtest-driven |
| 60 | [Resources, crafting, and progression](./60-resources-crafting-progression.md) | Run economy, radar, refundable account power, and permanent option-unlock structure and initial catalogs established |
| 61 | [Specialized resource identities](./61-specialized-resource-identities.md) | Asterite through Flux Amber accepted with redundant visual and audio identities |
| 62 | [Permanent PowerUp catalog](./62-permanent-powerup-catalog.md) | Thirteen account-wide tracks, effects, caps, fixed prices, active ranks, stacking, and refund behavior accepted |
| 63 | [Permanent option-unlock catalog](./63-permanent-option-unlock-catalog.md) | Fresh-profile content and six nonrefundable purchases totaling 2,150 Hyper Gold accepted |
| 65 | [Weapon stat and branch upgrades](./65-weapon-stat-and-branch-upgrades.md) | Uncapped linear stat ranks, shared-depth prices, and specialized-resource branch lifecycle established; initial increments accepted |
| 66 | [Weapon catalog and resource graph](./66-weapon-catalog-and-resource-graph.md) and [weapon lookup index](./weapons/README.md) | All fifteen weapon designs, stats, branches, mappings, and signatures accepted; exact baseline lives in document 71 |
| 67 | [Mech relics](./67-mech-relics.md) | One exploration-found install-or-sell relic slot, three distinct caches, fixed sale value, selection, signaling, and fresh/unlocked pools established; effect tuning open |
| 68 | [Utility catalog](./68-utility-catalog.md) | Twelve non-radar utilities, ranks, interactions, six fresh blueprints, and six bundled unlocks accepted; numeric tuning open |
| 69 | [Initial relic catalog](./69-initial-relic-catalog.md) | Ten transformative relics, five fresh-pool entries, and five permanent pool unlocks accepted; tuning open |
| 70 | [Combat and economy balance framework](./70-combat-and-economy-balance-framework.md) | DPS anchors, horde throughput, six benchmarks, boss milestones, rank/branch value bands, and adjustment order accepted |
| 71 | [Initial weapon numeric catalog](./71-initial-weapon-numeric-catalog.md) | All 15 rank-zero weapons, 45 stat increments, 45 branches, edge rules, analytic estimates, reference build, and feasible boss Hull accepted |
| 72 | [Player survivability and damage baseline](./72-player-survivability-and-damage-baseline.md) | Movement scale, collision footprints, damage order, health packs, rocks, control resistance, and failure margins accepted |
| 73 | [Interface, screen flow, and information architecture](./73-interface-screen-flow-and-information-architecture.md) | Active HUD, run console, fabrication, map, relic choice, results, hangar, inputs, and reference-resolution behavior accepted |
| 80 | Narrative, characters, lore, and delivery | Theme boundary captured; [narrative and thematic frame open](./open-questions.md#oq-033--what-narrative-and-thematic-frame-connects-the-visible-systems) |
| 90 | [UI, screen flow, and information architecture](./73-interface-screen-flow-and-information-architecture.md) | Screen composition, active feedback, gamepad navigation, comparisons, confirmations, results, and hangar flow accepted; final visual and audio language open |
| 100 | Onboarding, accessibility, settings, and player support | Foundational readability and first-mining needs captured; [dedicated specification open](./open-questions.md#oq-032--what-onboarding-accessibility-and-settings-does-standard-mode-require) |
| 110 | Multiplayer, social, platform, and lifecycle features | Standard mode established as single-player; platform and any future multiplayer or social features open |
| 120 | Content inventory and completion matrix | Initial weapons and player-survival combat are numerically specified; other initial catalogs are complete at their stated level; later breadth, remaining numeric catalogs, biome/map themes, and presentation inventories open |

Coverage status is an editorial signal, not a statement about implementation progress.

## Ledgers

- [Open Questions](./open-questions.md): the single authoritative register of unresolved design questions.
- [Decision Log](./decisions/README.md): durable records of consequential decisions and their rationale.
- [Glossary](./glossary.md): canonical vocabulary and disambiguation.
- [Research Index](./research/README.md): sourced external findings and design comparisons.
- [Technical Design Specification](./technical/README.md): implementation architecture, contracts, verification, and delivery planning derived from this gameplay specification.
