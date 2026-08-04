---
doc_id: RES-001
title: Vampire Survivors Reference Mechanics
status: complete
authoritative: false
---

# RES-001 — *Vampire Survivors* Reference Mechanics

## Research question

Which high-level mechanics are being invoked when this game uses *Vampire Survivors* as a reference, and which progression functions are mining and crafting intended to replace?

## Retrieval date

Initially retrieved 2026-08-01; enemy, weapon, map, and light-source conventions rechecked 2026-08-03.

## Sources

- [*Vampire Survivors* official Steam store page](https://store.steampowered.com/app/1794680/Vampire_Survivors/) — developer-controlled commercial description; strongest source here for the intended high-level experience and supported features.
- [Experience Gem — *Vampire Survivors Wiki*](https://vampire.survivors.wiki/w/Experience_Gem) — community-maintained mechanics reference; used for the XP-to-level-up flow.
- [Treasure Chest — *Vampire Survivors Wiki*](https://vampire.survivors.wiki/w/Treasure_Chest) — community-maintained mechanics reference; used for chest reward behavior.
- [Weapons — *Vampire Survivors Wiki*](https://vampire.survivors.wiki/w/Weapons) — community-maintained mechanics reference; used for the multi-weapon and weapon-progression comparison.
- [Mad Forest — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Mad_Forest) — community-maintained stage reference; used to verify the first stage's 30-minute time limit and post-limit Reaper arrival.
- [Characters — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Characters) — community-maintained roster reference; used to verify the starting-weapon and passive-bonus character structure.
- [Enemies — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Enemies) — community-maintained mechanics reference; used for minute waves, offscreen spawning, ordinary-enemy recycling, boss persistence, and map-event pressure patterns.
- [Weapons — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Weapons) — current Fandom-hosted reference; used to corroborate unique weapons and capped simultaneous weapon slots in the normal acquisition flow.
- [Milky Way Map — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Milky_Way_Map) — community-maintained reference; used to corroborate the pause-menu map and offscreen directional-marker precedent.
- [Light source — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Light_source) — community-maintained mechanics reference; used for destructible field objects and their immediate pickups.
- [Player stats — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Player_stats) — community-maintained reference; used for the 100 Max Health, zero Recovery, and zero Armor baseline.
- [Armor — *Vampire Survivors Wiki*](https://vampire-survivors.fandom.com/wiki/Armor_%28stat%29) — community-maintained mechanics reference; used for flat per-instance damage reduction with a one-damage floor.

The official wiki blocked automated retrieval during this research pass. Corresponding indexed wiki material and the older Fandom-hosted pages were used to corroborate the basic XP and chest descriptions. Those details are included only to define the comparison target and are not treated as authoritative rules for this game.

## Relevant facts about the reference

### High-level experience

The official store description presents *Vampire Survivors* as a minimalist time-survival game with roguelite elements. It emphasizes surviving against hundreds or thousands of enemies, rapidly snowballing through choices, collecting items during a run, using multiple offensive weapons, and spending run-earned gold on upgrades for later survivors.

### XP progression

In the reference game, defeated enemies can drop experience gems. Gathering enough XP produces a level-up, which pauses play and offers a small set of weapons or passive items. Repeated XP gains therefore supply much of the run's build cadence.

### Treasure-chest progression

Strong enemies can drop treasure chests. Chests can grant random weapon or passive-item levels, coins, and eligible weapon evolutions. They therefore provide burst progression layered on top of XP level-ups.

### Initial stage duration

Mad Forest, the stage available from the start, has a 30-minute normal time limit. At that limit the stage counts as complete and a Reaper arrives, absent an Endless-mode exception. This game initially tested a 25-minute target, then moved to a 35-minute standard in [DEC-079](../decisions/DEC-079-thirty-five-minute-seven-minute-boss-cycle.md) to make room for navigation, mining, and build development. Paused fabrication still adds wall-clock time beyond that active-simulation target.

### Character structure

The reference game's roster pairs each character with its own starting weapon or weapons and a passive bonus. Characters can then obtain additional weapons during play. This game adopts the same high-level structure for selectable mechs while leaving its exact mech stats, passive traits, signature weapons, unlocks, and shared-catalog rules to its own specification.

### Waves, spawning, and bosses

The community mechanics reference describes normal enemies as entering through minute-based waves. Each wave specifies enemy types, a minimum population, and a spawn interval. Ordinary enemies generally appear just outside the visible screen and may despawn after the player moves sufficiently far away. Short map events can add swarms, encirclements, or other pressure outside the regular wave cycle.

Bosses are stronger wave enemies with effect resistances. Unlike ordinary enemies, they do not despawn for distance; the reference brings a distant boss back to the visible battlefield. These behaviors support pressure centered on the player's current location and prevent permanent boss avoidance on a large stage.

### First-stage enemy variety

Mad Forest's normal-wave table uses about fifteen distinct entries across its 30 minutes when palette or stat variants and late boss-grade enemies reused as ordinary waves are counted. Grouping closely related variants produces roughly ten broader visual families. An individual minute ordinarily lists one to three normal-wave identities, with additional bosses and event formations layered on top. This distinction matters: the reference creates run-long variety without requiring the player to parse its entire roster simultaneously.

### Weapons, slots, and map access

The reference normally limits the number of simultaneous weapon identities and prevents acquiring duplicate copies of most weapons. This project already chose a different four-slot count and different upgrade system, but the stable committed-loadout convention offers a simple answer to removal and replacement questions.

The Milky Way Map establishes a reference precedent for viewing a map from the pause menu and for pointing toward important offscreen targets. This project replaces the reference's static-stage map knowledge with exploration fog and expands the directional system into the crafted resource radar.

### Health and incidental field pickups

The reference's ordinary shared player-stat baseline is 100 Max Health, zero Recovery, and zero Armor before character or PowerUp modifiers. Each Armor point reduces an incoming damage instance by one but cannot reduce it below one. Breakable light sources can appear in the field and release an immediate pickup such as a fixed health restoration or temporary battlefield effect. Most stages make a spawn attempt every second, use a stage-specific chance, and maintain a small simultaneous destructible cap; at cap, a successful spawn can replace an existing light source with one nearer the player. This supplies an ongoing low-input survivability opportunity without requiring defeated ordinary enemies to become the reward source.

## Comparison to this game

| Dimension | *Vampire Survivors* reference | This game |
| --- | --- | --- |
| Continuous combat input | Weapons attack automatically; movement carries much of the continuous attention | Adopt the automatic-attack and movement-emphasis pattern |
| Combat pressure | Large and escalating masses of enemies constrain space | Adopt horde pressure, rethemed as alien monsters |
| Arsenal | Multiple weapons create a growing run build | Four simultaneous weapons, each with a fixed stat bundle and weapon-specific branch upgrades |
| Camera | Elevated, battlefield-readable framing follows the player | Fully top-down, fixed-scale, north-up tracking with a wide field of view |
| Enemy schedule | Minute waves with population and spawn-cadence definitions plus special events | A complete deterministic 35-row schedule with weighted composition, population, spawn pulses, formations, bosses, and mining-beacon responses |
| Ordinary enemy variety | About fifteen Mad Forest wave entries, roughly ten broader families, and ordinarily one to three identities per minute | Ten fixed-profile identities built from six substantially distinct silhouettes and four readable variants; Needler is the sole specialist and no more than three identities appear in an authored minute |
| Spawn persistence | Ordinary enemies enter outside the screen and can recycle at distance; bosses persist and return | Adopt the same pressure-centered distinction, constrained to valid procedural-map ground and readable boss re-entry |
| Equipment lifecycle | Most weapon identities are unique within a capped loadout | Four unique committed weapon slots and three committed utility slots; no removal or replacement |
| Survivability | Finite Max Health, zero baseline Recovery, and replenishing healing or temporary pickups from capped breakable field objects | 100 baseline Hull Integrity, zero baseline Recovery, and 20%-chance health packs from a replenishing population capped at 16 destructible rocks |
| Ordinary run progression source | XP gems from defeated enemies | Multiple kinds of resources mined from map points |
| Ordinary upgrade acquisition | Randomized level-up offerings plus chest rewards | Intentional crafting of weapons and upgrades |
| Cross-run progression | Run-earned gold and unlock systems | Hyper Gold mined at three sites funds account-wide PowerUps and permanent option unlocks after successful extraction |

## Interpretation

The largest structural change is not merely substituting ore for XP. XP in the reference rewards killing wherever enemies happen to be, while mined resources require navigation to specific locations and continued proximity during extraction. The new progression source therefore changes route planning, exposure, positioning, reward timing, and the relationship between combat and growth.

The exact cadence needs careful design. XP and chests provide frequent progression beats in the reference; mining and crafting must replace enough of those beats to keep early-run growth legible and satisfying without making mining stops feel like constant menu interruptions.

## Application boundary

[DEC-096](../decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md) now uses the simplest core single-player normal-stage behavior as a bounded default for movement and collision feel, camera, combat pressure and spawning, boss pursuit, pause flow, and results conventions. Explicit project decisions override it. XP, chests, static stages, reference-game weapon acquisition and evolution, run duration, loadout counts, economy, modes, multiplayer, platform, art, DLC, secrets, and exceptional characters are not imported.

Pause and results details in this project's DEC-099 are explicit project rules chosen to preserve the reference's low-friction single-player run cadence. They should not be read as claims that every field or pause condition exactly matches a particular current *Vampire Survivors* version.

## Possible implications

The following are proposals, not decided rules:

- Ordinary mining points may need differentiated resource identities so route choice affects the build rather than merely filling a universal bar.
- The player may need reliable access to an early crafting opportunity so automatic weapons scale before horde pressure overtakes them.
- Mining rewards and recipes may need some flexibility to prevent map distribution from making a run nonviable.
- Direct monster rewards should be constrained if mining is to remain the primary source of power.

## Resulting links

- [DEC-001 — Use a *Vampire Survivors*-inspired combat reference](../decisions/DEC-001-vampire-survivors-combat-reference.md)
- [DEC-002 — Replace XP and treasure chests with mining and crafting](../decisions/DEC-002-mining-replaces-xp-and-chests.md)
- [DEC-011 — Start with a 25-minute standard run timer](../decisions/DEC-011-twenty-five-minute-run-timer.md)
- [DEC-014 — Use a selectable mech roster with signature starting weapons](../decisions/DEC-014-selectable-mechs-and-signature-weapons.md)
- [DEC-096 — Use *Vampire Survivors* as the default precedent](../decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md)
- [DEC-097 — Inherit direct movement, collision, and camera](../decisions/DEC-097-inherit-direct-movement-collision-and-camera.md)
- [DEC-098 — Use minute-authored horde waves](../decisions/DEC-098-use-minute-authored-horde-waves.md)
- [DEC-099 — Use single-player pause and results flow](../decisions/DEC-099-use-single-player-pause-and-results-flow.md)
- [DEC-100 — Commit installed weapons and utilities](../decisions/DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-101 — Target an approachable escalating standard difficulty](../decisions/DEC-101-target-an-approachable-escalating-standard-difficulty.md)
- [DEC-102 — Separate enemy kills from field pickups](../decisions/DEC-102-separate-enemy-kills-from-field-pickups.md)
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](../decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-122 — Use destructible rocks as the health-pack source](../decisions/DEC-122-use-destructible-rocks-for-health-packs.md)
- [DEC-123 — Replenish destructible rocks around the player](../decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
- [DEC-104 — Show a compact survivor-like active HUD](../decisions/DEC-104-show-a-compact-survivor-like-active-hud.md)
- [DEC-105 — Use a simple pursuer-first enemy roster](../decisions/DEC-105-use-a-simple-pursuer-first-enemy-roster.md)
- [DEC-106 — Use ten ordinary enemy identities](../decisions/DEC-106-use-ten-ordinary-enemy-identities.md)
- [DEC-119 — Accept the initial alien encounter baseline](../decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Open Questions](../open-questions.md)
