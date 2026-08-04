---
doc_id: GDD-COMBAT
title: Combat, Weapons, Movement, and Camera
status: active
authoritative: true
---

# Combat, Weapons, Movement, and Camera

## Purpose and player promise

Combat preserves the approachable, movement-centered pressure of *Vampire Survivors*: the mech's weapons attack automatically while the player concentrates on navigating through alien hordes, maintaining favorable positioning, exploring, and holding mining areas. Multiple weapons allow the run's combat capabilities to grow through crafted additions and upgrades.

## Adopted reference behavior

The game uses the simplest core single-player normal-stage behavior of *Vampire Survivors* as its default precedent wherever an explicit project decision does not replace it. The inheritance applies most strongly in four areas:

1. **Automatic attacks:** Equipped weapons perform their attacks without the player repeatedly pressing a fire button.
2. **Horde pressure:** Large numbers of alien monsters create continuous spatial pressure and threaten the player through their presence and movement.
3. **Movement emphasis:** Direct movement is a primary continuous player responsibility and a major means of survival and positioning.
4. **Camera perspective:** The camera angle and broad framing follow the elevated, battlefield-readable perspective associated with *Vampire Survivors*.

This is a bounded rules precedent, not an instruction to copy content, numbers, audiovisual assets, or presentation. It does not restore XP, random chest progression, static stage layouts, the reference game's weapon-acquisition rules, or any other system replaced by mining, fabrication, procedural maps, or explicit decisions. [DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md) defines the complete precedence rule.

## Camera and visual format

The gameplay camera looks fully top-down over a wide battlefield area. It is not an isometric or close over-the-shoulder view. The wide field of view must let the player read horde movement, mining boundaries, resource navigation, and automatic weapon patterns at the same time.

The camera uses a fixed world scale, orthographic projection, and north-up orientation. It follows the mech at screen center without intentional lag, look-ahead, or aiming offset, except that it clamps at the finite map boundary rather than revealing space beyond the level. It does not rotate, zoom dynamically for combat, or accept manual pan input during active play. Exact world-to-screen scale remains a presentation and tuning choice.

Gameplay uses native low-poly 3D models for the world, actors, terrain, mining sites, and physical weapon objects. HUD, menus, maps, icons, and informational overlays remain 2D, while VFX may combine meshes, particles, decals, trails, and camera-facing sprites. Imported CC0-first assets receive a shared scale, material, palette, and readability pass. Simple geometry, strong silhouettes, clear ground contact, and restrained materials preserve the readable character of a top-down survivor game. Required state never depends on realistic lighting, subtle texture detail, or color alone. See [DEC-114](./decisions/DEC-114-use-native-low-poly-3d-gameplay.md) and [RES-005](./research/RES-005-free-asset-strategy.md).

## Weapons

- The player can equip four weapons simultaneously.
- Weapons attack automatically.
- The selected mech begins each run with its fixed signature automatic weapon occupying one weapon slot, leaving three empty weapon slots.
- Weapons and weapon upgrades hook into the mining and crafting system.
- Six specialized ordinary-resource families define 15 normal pair-weapons; each four-resource profile supports six recipes.
- All 15 base behaviors, three-stat bundles, branch sets, and resource mappings are accepted for playtesting in the [Weapon Specification Index](./weapons/README.md).
- Every equipped weapon must have a different identity. The mech cannot equip duplicate copies of its signature or any fabricated weapon.
- Fabricating a weapon permanently commits its slot for that run. A weapon cannot be removed, replaced, dismantled, sold, or refunded.
- All 15 base weapons occupy the same intended power tier. Each is useful when first equipped and can anchor a successful build; differences are sidegrades in behavior, strengths, weaknesses, and situational performance.
- XP level-ups and treasure chests do not grant the game's ordinary weapon progression.

When all four weapon slots are occupied, the mech cannot fabricate another weapon. The fabrication interface must preview the recipe, automatic behavior, stats, and slot commitment before confirmation.

### Weapon boundary and automatic behavior

A system belongs in a weapon slot when its primary purpose is dealing automatic damage. The catalog may therefore include conventional projectiles and beams, autonomous drones, automatically placed turrets, mines, contact-damage auras, and movement-dependent ramming systems. Form does not determine slot category; primary gameplay purpose does.

Weapon behaviors may target the nearest enemy, fire relative to movement or facing, emit radial patterns, orbit the mech, choose ground locations automatically, or operate through autonomous agents. These patterns can demand different positioning but never require manual aim, fire, placement, or activation in the baseline game. Every weapon defines its complete automatic selection, placement, trigger, and retargeting rules as applicable.

## Utility systems

The mech has three utility slots separate from its four weapon slots. Utility systems primarily provide passive or automatic non-weapon support such as navigation, mining, defense, mobility, recovery, economy, or weapon support; they add no manual gameplay input. The resource radar occupies one utility slot and costs 300 common ore. The content-complete catalog has twelve non-radar utilities, two assigned to each specialized material, and each costs one unit of its single assigned material. Every four-material profile therefore offers exactly eight of them plus the radar. A first playable may use six, one per material. A support system may interact with enemies, but sustained automatic damage belongs in a weapon slot unless an explicit exception is established.

Fabricating a utility permanently commits its slot for that run. Utilities cannot be removed, replaced, dismantled, sold, or refunded. Each installed non-radar utility has exactly three run-local ranks costing 50, 100, and 150 common ore; its blueprint fixes and previews the improvement at each rank. Ranks are independent and consume no additional slot. The radar has no initial ranks. The twelve accepted concepts, material assignments, base effects, rank totals, stacking rules, and exclusions are defined in the [Utility Catalog](./68-utility-catalog.md); numeric tuning remains open.

## Mech relic

The mech has one relic slot separate from its weapon and utility slots. Every standard map contains three recognizable relic caches; touching one automatically opens its fully paused choice without adding an interaction button. Only one relic is active at a time. Selling the discovery for common ore keeps the current effect, while installing it replaces the active relic and automatically sells the displaced relic for common ore. Installation or sale is required before play resumes.

Relics favor transformative rules and tradeoffs over simple unconditional stat increases. The ten accepted initial effects alter whole-build geometry, cadence, targeting, enemy grouping, kill chains, heat, or mining pressure and are indexed in the [Initial Relic Catalog](./69-initial-relic-catalog.md). Exact numerical tuning and explicitly deferred per-weapon edge mappings remain playtest work.

## Direct controls

Movement is the baseline direct moment-to-moment combat control. The player does not manually aim, fire, deploy, or activate weapons. Movement input sets the mech's direction and full movement speed immediately: digital input supports eight normalized directions and analog input supports the full circle. Releasing input stops the mech immediately. Standard movement has no acceleration, braking lag, momentum, turn radius, sprint, dash, dodge, stamina, reverse penalty, or strafing penalty. The shared unmodified speed is 3.0 mech diameters (`M`) per second, and the mech uses a circular 1.0M collision footprint.

While movement input is nonzero, the mech faces in that direction. Releasing movement preserves the last nonzero facing direction. Before the first input, the mech faces east—screen-right on the fixed north-up camera—so facing-based starting weapons have a deterministic initial attack. Each weapon owns its automatic targeting rule or non-targeted attack pattern, which may respond to enemy position, mech position, current movement, persistent facing, or its own autonomous agent without becoming manual aim. A current-movement weapon separately defines what it does while the mech is stationary.

Blocking terrain and the finite map boundary are solid. Ordinary enemies, elites, and bosses do not physically block the mech and may overlap each other. Enemy contact repeats once per overlapping enemy every 0.75 seconds, while the mech receives a 0.20-second global contact-only grace after each resolved contact instance to prevent simultaneous bodies from deleting Hull invisibly. Leaving and re-entering does not reset an attacker's cooldown. Projectiles and explicit hazards remain eligible during contact grace. Taking ordinary damage does not impose hitstun, knockback, control loss, or mining interruption. Exact player, ordinary-enemy, elite, and boss footprints are fixed in the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md).

## Hull Integrity and survival

Every mech has current and maximum **Hull Integrity**. The shared baseline is 100 maximum, zero passive Recovery, zero Armor, and full current Hull Integrity at deployment before account PowerUps and mech modifiers. Contact, projectile, and hazard damage reduces it after Armor: each Armor point subtracts one from an incoming damage instance, to a minimum of one, unless an attack explicitly ignores Armor. At zero, the mech dies and the run fails unless an equipped revival effect intervenes.

Health packs and other explicit repairs restore current Hull Integrity without exceeding the current maximum. A health pack restores 25 Hull; collecting one at full Hull Integrity still consumes it, and any excess repair is wasted. The standard mech has no passive healing or regeneration unless an explicit trait, PowerUp, relic, or utility provides it. It has no universal post-hit invulnerability beyond contact-only grace; Emergency Reboot's two seconds are an explicit exception. Current Hull Integrity is continuously readable during play and its exact current/maximum values appear on pause. Damage order, rounding, shielding, and failure margins are fixed in the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md), while presentation remains playtest and production work.

Menu navigation and pre-run selections are not combat controls. Activated utility abilities, dashes requiring a separate button, and manual aim are absent from the baseline; any later addition requires an explicit extension to this rule.

The initial Windows Steam and Steam Deck target supports complete keyboard-and-mouse and gamepad operation. Gamepad alone must access every active-play, fabrication, map, hangar, progression, settings, and results function through the default mapping. Prompts switch automatically between current input families. All gameplay and HUD state remains available at the 1920×1080 desktop reference and the Steam Deck's 1280×800 reference; touch is never required.

## Player attention model

The decided controls imply that the player's combat attention should focus on:

- Navigating between threats rather than repeatedly firing.
- Positioning so automatic weapon patterns are effective.
- Choosing routes toward mining opportunities.
- Holding or circling within a mining point's valid area under pressure.
- Choosing how mined resources change the current weapon build.

This list describes the intended allocation of attention. Damage-first automatic drones are weapons; support-first autonomous systems may be utilities. Separately activated combat actions are not part of the baseline.

## Combat progression

Ordinary combat growth during a run comes primarily from crafting weapons and weapon upgrades with mined resources. Hyper Gold supports cross-run unlocks and PowerUps. See [Resources, Crafting, and Progression](./60-resources-crafting-progression.md).

There is no XP system. Ordinary enemies and elites drop no items, XP, resources, repair pickups, consumables, or temporary effects. Their immediate reward is the space and safety created by killing them. Enemy defeats can still count for explicitly described weapon or relic effects, mech traits, challenges, achievements, and unlock conditions.

Mining is the primary source of common ore, specialized ordinary resources, and Hyper Gold. Selling a relic provides common ore, and boss loot provides limited quantities of all three categories. Standard mode maintains up to 16 active destructible non-enemy rocks near the player's explored area. It attempts one valid offscreen spawn per active-simulation second at 10% success, filling an empty slot or recycling the farthest eligible offscreen rock at the cap. A rock has 100 Hull, zero Armor, a 0.80M weapon-damage footprint, and no physical collision. Target-selecting weapons consider it only when no enemy is in range; geometric attacks may strike it incidentally. Each destroyed rock has a fixed 20% chance to release one health pack and otherwise releases nothing. The pack persists until collected or run end, is collected when its 0.25M radius overlaps the mech, and immediately repairs 25 Hull; the baseline has no attraction beyond contact. Rocks never award common ore, specialized materials, Hyper Gold, or temporary effects.

Defeated ordinary enemies and elites leave no loot or collision-bearing corpse. Brief death feedback may finish before the entity disappears.

## Horde director and enemy pressure

Standard mode uses a deterministic, minute-authored 35-minute schedule. Each active minute specifies enemy families, desired minimum population pressure, spawn interval, and any authored swarm, wall, stream, or encirclement event. The schedule is not adaptively weakened for a poor build, low health, or a fresh account.

Ordinary enemies spawn on valid navigable ground just outside the visible camera. Enemies that travel sufficiently far away may despawn or be recycled so the intended pressure can re-enter around the player's current location.

The [Initial Alien and Boss Roster](./31-initial-alien-roster.md) defines Skitterling, Ripper, Shellback, Lurker, Gloomwing, Needler, and four readable late variants. Nine continuously pursue and deal contact damage. Needler retains pursuit and adds the sole ordinary specialist behavior: one conspicuously charged straight non-homing projectile every 4.5 seconds. Every identity has one fixed Hull, movement, damage, size, and resistance profile throughout standard play. The [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md) converts those percentages and body scales into world speeds, contact footprints, hits-to-defeat, and control behavior. The shared elite treatment multiplies statistics and presentation without adding behavior or loot. The [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md) fixes their compositions, populations, replenishment pulses, events, and beacon appearances across all 35 minutes.

## Interval bosses

Boss aliens arrive at 7:00, 14:00, 21:00, and 28:00 of active simulation, following the broad pacing role of *Vampire Survivors* bosses. Riftjaw uses a telegraphed straight charge, Brood Titan sheds an incomplete Skitterling ring, Prism Crown fires one radial projectile burst, and Skybreaker Apex leaps to a locked ground marker. Each boss persists until killed, ordinary hordes continue during the encounter, and later bosses arrive on schedule even if an earlier boss remains alive. Failing a damage check can therefore create boss overlap. No new boss or end-state attacker spawns at 35:00; the final seven minutes are a horde crescendo.

Bosses never despawn for distance. If one falls far enough behind to allow permanent avoidance, it is repositioned to valid ground beyond the camera for a readable re-entry; it cannot appear directly on the mech or deal unavoidable damage at the reposition point. Bosses have greater resistance than ordinary enemies to knockback, control effects, and instant-kill effects, with boss-specific values.

Defeating a boss produces an immediate non-modal physical loot explosion while combat continues. Every boss drops 300 common ore and 25 unsecured Hyper Gold. The first two bosses each add one specialized-material unit; the final two add two units, each independently selected from the four present materials. Pieces scatter to valid nearby ground, persist until contact-collected or run end, and appear immediately on the minimap. Exact numerical encounter and reward tuning remains open, while boss identities, behaviors, surrounding horde schedule, and reward structure are fixed.

## Feedback requirements

The eventual presentation must make the following player-visible states legible:

- Each equipped weapon's attack pattern and effective area.
- Weapon activation, cooldown cadence, targeting behavior, and impact.
- Damage, control effects, and relevant enemy reactions.
- Threat density and safe or unsafe movement space amid visual effects.
- Changes caused by crafted weapon upgrades.
- The active relic and every weapon or system whose behavior it changes.
- Whether combat state changes mining progress or access.
- Incoming damage and the contact-damage immunity cadence.
- Destructible-rock Hull and footprint, health-pack 25-Hull repair, valid 18–45M spawn annulus, 0.25M pickup radius, and remaining audiovisual feedback.

The active HUD persistently shows the upward timer and next boss threshold, Hull Integrity gauge, ordinary-resource and unsecured-Hyper-Gold counts, weapon/utility/relic slots, total defeats, and compact north-up map. Contextual panels show mining, beacon, boss, damage, and pickup states. Pause expands these into exact values and full run statistics. Layout, input, comparison, warning priority, and reference-resolution behavior are fixed by the [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md); final art, sound, and accessibility ranges remain later presentation work.

## Open questions

- [OQ-005 — What makes mining a push-your-luck system?](./open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system)
- [OQ-011 — What is the intended platform and presentation format?](./open-questions.md#oq-011--what-is-the-intended-platform-and-presentation-format)
- [OQ-023 — Which asset medium and visual style best fit the free-asset constraint?](./open-questions.md#oq-023--which-asset-medium-and-visual-style-best-fit-the-free-asset-constraint)
- [DEC-114 — Use native low-poly 3D gameplay](./decisions/DEC-114-use-native-low-poly-3d-gameplay.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Initial Alien and Boss Roster](./31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md)
- [DEC-119 — Accept the initial alien encounter baseline](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [OQ-032 — What onboarding, accessibility, and settings does standard mode require?](./open-questions.md#oq-032--what-onboarding-accessibility-and-settings-does-standard-mode-require)

## Related documents

- [Game Vision](./00-game-vision.md)
- [Core Game Loop](./10-core-game-loop.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md)
- [Run Structure and Timing](./20-run-structure-and-timing.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](./weapons/README.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [Mech Relics](./67-mech-relics.md)
- [DEC-001 — Use a Vampire Survivors-inspired combat reference](./decisions/DEC-001-vampire-survivors-combat-reference.md)
- [DEC-018 — Use four weapon slots and three utility slots](./decisions/DEC-018-four-weapons-three-utilities.md)
- [DEC-019 — Use movement-only baseline combat controls](./decisions/DEC-019-movement-only-combat-controls.md)
- [DEC-020 — Keep ordinary crafting materials exclusive to mining](./decisions/DEC-020-mining-exclusive-ordinary-materials.md)
- [DEC-021 — Use a wide fully top-down camera](./decisions/DEC-021-wide-fully-top-down-camera.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](./decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [DEC-036 — Use six-color signature-aware resource profiles](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-037 — Use unique weapons and soft profile balance](./decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [DEC-038 — Use a broad automatic-weapon taxonomy](./decisions/DEC-038-broad-automatic-weapon-taxonomy.md)
- [DEC-041 — Use an equal-tier base-weapon catalog](./decisions/DEC-041-equal-tier-base-weapon-catalog.md)
- [DEC-042 — Use movement-derived persistent mech facing](./decisions/DEC-042-movement-derived-persistent-facing.md)
- [DEC-075 — Accept the complete initial weapon catalog for playtesting](./decisions/DEC-075-accept-complete-initial-weapon-catalog.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
- [DEC-035 — Integrate utilities without fixed weapon pairing](./decisions/DEC-035-integrate-utilities-without-fixed-weapon-pairing.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./decisions/DEC-029-pause-and-resolve-relic-discoveries.md)
- [DEC-030 — Place three automatic relic caches on each standard map](./decisions/DEC-030-three-automatic-relic-caches.md)
- [RES-005 — Free-asset strategy](./research/RES-005-free-asset-strategy.md)
- [DEC-096 — Use Vampire Survivors as the default precedent](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md)
- [DEC-097 — Inherit direct movement, collision, and camera](./decisions/DEC-097-inherit-direct-movement-collision-and-camera.md)
- [DEC-098 — Use minute-authored horde waves](./decisions/DEC-098-use-minute-authored-horde-waves.md)
- [DEC-100 — Commit installed weapons and utilities](./decisions/DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-109 — Use single-material utilities with three ore ranks](./decisions/DEC-109-use-single-material-utilities-with-three-ore-ranks.md)
- [DEC-101 — Target an approachable escalating standard difficulty](./decisions/DEC-101-target-an-approachable-escalating-standard-difficulty.md)
- [DEC-102 — Separate enemy kills from field pickups](./decisions/DEC-102-separate-enemy-kills-from-field-pickups.md)
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](./decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-122 — Use destructible rocks as the health-pack source](./decisions/DEC-122-use-destructible-rocks-for-health-packs.md)
- [DEC-123 — Replenish destructible rocks around the player](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
- [DEC-104 — Show a compact survivor-like active HUD](./decisions/DEC-104-show-a-compact-survivor-like-active-hud.md)
- [DEC-105 — Use a simple pursuer-first enemy roster](./decisions/DEC-105-use-a-simple-pursuer-first-enemy-roster.md)
- [DEC-106 — Use ten ordinary enemy identities](./decisions/DEC-106-use-ten-ordinary-enemy-identities.md)
- [DEC-107 — Use fixed ordinary enemy stat profiles](./decisions/DEC-107-use-fixed-ordinary-enemy-stat-profiles.md)
- [DEC-108 — Use one straight-shot enemy specialist](./decisions/DEC-108-use-one-straight-shot-enemy-specialist.md)
- [DEC-111 — Make bosses explode into collectible resources](./decisions/DEC-111-make-bosses-explode-into-resources.md)
- [DEC-113 — Target Windows PC and Steam Deck first](./decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
