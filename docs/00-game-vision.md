---
doc_id: GDD-VISION
title: Game Vision
status: active
authoritative: true
---

# Game Vision

## High concept

The game is a survivor-like action game in which science-fiction mechs fight alien monsters. It begins with *Vampire Survivors* as its high-level genre reference, retaining automatic multi-weapon combat, horde pressure, movement emphasis, and its broad camera perspective. It changes the progression formula by removing experience points and replacing experience-gem and treasure-chest progression with map exploration, resource mining, positional commitment, and upgrade crafting.

The simplest core single-player normal-stage behavior of *Vampire Survivors* is the default precedent for movement, collision feel, camera tracking, enemy pressure and spawning, boss pursuit, pause flow, and results conventions whenever an accepted decision does not say otherwise. This bounded inheritance rule is recorded in [DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md). It does not import XP, chests, stage construction, run timing, weapon acquisition, economies, modes, multiplayer, platforms, or art medium. The progression substitution is recorded in [DEC-002](./decisions/DEC-002-mining-replaces-xp-and-chests.md).

## Player fantasy

The player chooses one science-fiction mech from an initial roster of six before each run. Kestrel, Pike, Prospector, Lodestar, Bastion, and Razorback each pair a different signature automatic weapon with one simple positive inherent trait and a distinct fully top-down silhouette. All six are available on a fresh profile; Kestrel is recommended but not forced. The opposing force consists of alien monsters. Final mech presentation names, numerical tuning, framing of the conflict, and the identities and unlock requirements of later roster additions remain open.

Each run loadout has four weapon slots, three utility slots, and one mech-wide relic slot. The signature weapon occupies one weapon slot at deployment; the mech's inherent trait consumes no slot. Weapons and utilities become irrevocably committed when installed; the relic is the explicit replaceable exception. Combat uses movement-only direct control: weapons aim and attack automatically, and the player does not manually aim or fire. Ordinary enemies and elites do not drop items or progression resources. Mining remains the primary source of common ore, specialized ordinary resources, and Hyper Gold. Relic sales provide common ore, while bosses explode into limited physical piles of all three resource categories. A replenishing population capped at 16 destructible rocks provides the standard map's only ordinary health-pack opportunities; each destroyed rock has a fixed 20% chance to drop one pack.

The content-complete utility catalog contains twelve passive or automatic non-radar support systems, two assigned to each specialized material. Every four-material profile offers exactly eight of them plus the universally available common-ore radar. Each installed non-radar utility has three capped common-ore ranks; utilities add no manual combat inputs and systems primarily intended for sustained damage remain weapons.

Every mech deploys with full Hull Integrity. The shared baseline is 100 maximum Hull Integrity and no passive recovery before PowerUps and mech modifiers. Damage reduces Hull Integrity and zero causes death unless an explicit revival applies. The standard map's active-healing route is an occasional contact-collected health pack from its replenishing destructible rocks; ordinary enemies and elites never drop healing.

Base weapons beyond the selected mech's signature require specialized ordinary resources. Six resource families form 15 unique two-color base-weapon recipes; each run includes four families and therefore supports exactly six recipes. The selected signature weapon belongs to that normal catalog, and generation guarantees at least two of its three branch-resource colors. This prevents universal access to the same favorite loadout while preserving fixed, understandable recipes.

The camera is fully top-down with a wide field of view in the style of *Vampire Survivors*. It remains north-up at a fixed active-play world scale, follows the mech without manual camera control, and clamps only near the finite world boundary. Gameplay uses native low-poly 3D models through an orthographic projection, while the HUD and interfaces remain 2D and VFX may mix suitable techniques. The art direction prioritizes simple geometry, strong top-down silhouettes, a controlled shared palette, and coherent adaptation of freely available CC0-first assets. The initial release targets Windows PC through Steam and treats Steam Deck as a first-class target, using 1920×1080 desktop and 1280×800 handheld reference layouts with full keyboard-and-mouse and gamepad support.

## Core differentiators

### Exploration is required for power

The player must explore the map to find resources. Resource acquisition therefore pulls the player through the play space instead of allowing the full game plan to revolve around surviving in one favorable location.

### Mining creates positional commitment

Resources are extracted from mining points. The player must remain near a mining point during extraction. This forces the player to defend or maneuver around a constrained area while alien pressure continues, turning location and timing into meaningful concerns.

Mining begins automatically when the player enters a mining point's valid area and continues while the player remains there. Leaving the area causes unfinished extraction progress to decay very quickly.

### Mining creates push-your-luck decisions

Committing to a mining point exposes the player to additional risk. The player must weigh the value of extracting resources against the danger created by remaining near the point. The baseline push-your-luck pressure comes from restricting the space available for dodging while the horde remains dangerous. Unopened material geodes strengthen nearby enemies through thematic resonance fields, while mining Hyper Gold activates a progress-escalating threat beacon.

### Resources create upgrades through crafting

Mined resources are used to craft weapons and weapon upgrades. Mining and crafting replace both experience-point leveling and treasure-chest weapon progression from *Vampire Survivors*. Ordinary resources support only the current run and remain available after collection so the player can spend them later in that run. Each map contains three Hyper Gold sites worth 100 Hyper Gold apiece, and each of four bosses drops another 25 Hyper Gold alongside ore and present-profile crafting materials. Hyper Gold buys both permanent content or option unlocks and account-wide PowerUps spanning combat, survivability, mobility, and mining/economy, but collected Hyper Gold is forfeited unless the run is survived successfully. PowerUps never increase fixed Hyper Gold payouts.

Unlocked blueprints, recipes, effects, and prices remain fixed, while each level randomizes which four specialized materials are present and whether each has eight, nine, or ten geodes. The geological survey is withheld until deployment, then presented as compact live information during a one-minute active orientation phase with deliberately minor enemy waves. It reveals the resource profile and geode counts without revealing their locations. The player quickly forms an early-run plan while combat is already underway, then explores and adapts to what is found.

Map layout is significantly randomized on every run. No player-relevant location remains fixed between runs: even elements guaranteed to appear are placed at randomized locations. The visible result is procedural variation, while the exact use of templates, modules, or unconstrained generation remains open.

The standard world is mostly open rather than labyrinthine. Broad combat regions connect through multiple wide routes, collision obstacles remain sparse, and no narrow mandatory chokepoint controls access to a major region. Optional dead ends may reward exploration when they provide a readable exit and enough space to fight and mine.

## Initial design pillars

These are a provisional articulation of the decided brief. Their wording can change as the intended experience becomes clearer.

1. **Survive alien mass:** The player's automatically attacking weapons face sustained horde pressure while movement keeps the mech alive and correctly positioned.
2. **Explore under pressure:** Traversing the map is necessary to find valuable resources, not merely an optional route to bonuses.
3. **Commit to extract:** Valuable mining requires the player to accept a temporary positional constraint.
4. **Build with intent:** Resources let the player craft upgrades rather than relying entirely on randomized weapon acquisition.
5. **Make position a strategic resource:** Where and when the player mines matters alongside combat power.

The horde uses the accepted ten-identity [Initial Alien and Boss Roster](./31-initial-alien-roster.md), a complete deterministic [35-minute wave schedule](./32-standard-wave-and-beacon-schedule.md), off-screen replenishment, simple pursuit as its ordinary behavior, four persistent one-mechanic bosses, and a final seven-minute density crescendo. The mech uses direct normalized movement without acceleration, momentum, hitstun, forced displacement, or solid enemy-body blocking. The [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md) fixes first-playable movement, collision, damage, recovery, control, and failure-margin values. Encounter numbers and weapon values are also specified as first-playable baselines; camera scale and all numerical balance remain subject to playtesting.

## Experience thesis

The central tension is between movement and commitment:

- Exploration asks the player to leave known or favorable ground.
- The opening resource profile asks the player to form a plan during the one-minute minor-wave orientation phase.
- A discovered mining point offers future power.
- Extraction asks the player to remain near that point despite danger.
- Successfully mined resources give the player intentional influence over upgrades.

The intended result is a survivor-like in which route choice, map knowledge, territorial defense, and risk appetite matter more than they do in the genre reference.

## Timed survival frame

Each level has a 35-minute active-simulation time limit as the standard playtest target. Surviving until the limit completes the level and is presented thematically as the player's mission extraction. Boss aliens arrive at 7:00, 14:00, 21:00, and 28:00, punctuating the horde pressure in the broad style of *Vampire Survivors*. Bosses persist until killed, ordinary hordes continue during their encounters, and later bosses arrive on schedule even if this creates overlap. The final seven minutes form a horde crescendo rather than introducing another boss at extraction.

The standard mode is approachable and low-input but not dynamically adjusted to guarantee victory. A fresh account has a plausible successful path. A highly upgraded account receives a real and substantial advantage, especially during early play, but permanent stats remain weaker than the power developed through a coherent run build and cannot universally replace exploration, fabrication, movement, or respect for the late crescendo. Early waves leave room to learn and mine; later waves demand a developed build and reward the player with a large-scale mech power fantasy when that build succeeds.

One timed deployment is one complete run. Mission extraction is the culmination of that run's build and ends the run; there is no subsequent level that carries forward its ordinary resources, weapons, or run-local upgrades.

The standard gameplay specification is single-player. Pause, fabrication, relic resolution, required modal screens, and platform suspension freeze the active-simulation timer; the opening survey is the explicit non-modal exception. Death or confirmed abandonment fails the run, while mission extraction succeeds at 35:00. Both outcomes lead to a results screen covering combat, build, mining, exploration, resources, Hyper Gold, and unlock progress.

The player can open the fabrication menu on demand, anywhere and without a usage limit. Crafting and upgrading pause the entire gameplay simulation so the player can consider the run build and take a break from continuous action. If unrestricted access harms pacing or balance in playtesting, its access rules may be revised.

## Current scope boundaries

The following are not yet decided by the high-level brief:

- Whether alternate maps or modes use different run durations, and the final audiovisual presentation of mission extraction and results. The standard single-player flow, results organization, and required information are decided.
- Later numerical tuning and prototype-discovered edge cases for individual weapons. Their base behaviors, targeting models, stats, branches, and first-playable numerical values are accepted in the weapon catalog.
- Whether any non-XP concept of a player level exists; experience points themselves are excluded.
- Final visual styling for Fabrication. Its screen hierarchy, comparisons, purchase behavior, confirmation rules, and reference-resolution reflow are decided.
- Hyper Gold's appearance and audio identity and later option-catalog expansion. The initial spending-interface hierarchy, six-purchase option catalog, and 2,150-Hyper-Gold total are decided, as are the thirteen-track account-wide numerical PowerUp catalog, caps, prices, stacking, optional active ranks, and free refund.
- Final relic-cache art and effect tuning. Selection without replacement, duplicate exclusion, absence of dedicated guards, in-view signaling, discovery comparison, fixed 150-ore sale, ten-effect catalog, five-item fresh pool, five permanent additions, one relic slot, immediate install-or-sell choice, and automatic sale of a displaced relic are decided.
- The exact procedural construction method, dimensions, boundary presentation, connection widths, obstacle density, chunk library, and visibility model. Maps are already large, finite, bounded, significantly randomized, mostly open, and connected through wide redundant routes without mandatory narrow chokepoints.
- Whether a later multiplayer mode exists. Multiplayer is outside the standard gameplay baseline and requires separate rules if added.
- Exact fixed camera scale, low-poly palette and material rules, model and effects budgets, device mappings, ultrawide behavior, and detailed presentation style. The initial target is Windows Steam plus first-class Steam Deck support, with 1920×1080 and 1280×800 reference layouts, landscape presentation, and complete keyboard-and-mouse and gamepad access. Native 3D gameplay with an orthographic north-up fully top-down camera, 2D interfaces, player tracking, boundary clamping, no manual camera control, and a wide field of view are already decided.
- Narrative premise beyond mechs opposing alien monsters.

## Related documents

- [Core Game Loop](./10-core-game-loop.md)
- [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md)
- [Run Structure and Timing](./20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Initial Alien and Boss Roster](./31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Initial Mech Catalog](./36-initial-mech-catalog.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Mech Relics](./67-mech-relics.md)
- [Open Questions](./open-questions.md)
- [OQ-033 — What narrative and thematic frame connects the visible systems?](./open-questions.md#oq-033--what-narrative-and-thematic-frame-connects-the-visible-systems)
- [Glossary](./glossary.md)
- [DEC-096 — Use Vampire Survivors as the default precedent](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md)
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](./decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-122 — Use destructible rocks as the health-pack source](./decisions/DEC-122-use-destructible-rocks-for-health-packs.md)
- [DEC-123 — Replenish destructible rocks around the player](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
