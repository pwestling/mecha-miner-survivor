---
doc_id: GDD-GLOSSARY
title: Glossary
status: active
authoritative: true
---

# Glossary

Canonical game-specific language belongs here. Definitions describe player-visible meaning rather than implementation.

## Armor

A mech survivability statistic. Each Armor point subtracts one point from each incoming contact, projectile, or hazard damage instance, to a minimum of one damage, unless the attack explicitly ignores Armor. The shared baseline is zero before account PowerUps and mech modifiers.

## Asterite

Specialized ordinary resource `A`: a cyan, field-aligning crystal presented through three-point prisms, orderly light sweeps, and a rising chime. Its loose fictional affinity is precision, focus, stable fields, and anchoring. The affinity does not impose an exclusive recipe or weapon category. See [Specialized Resource Identities](./61-specialized-resource-identities.md#asterite-a).

## Barysteel

Specialized ordinary resource `B`: an extremely dense layered alien metal presented through squat hexagonal slabs, compression ripples, and a low metallic strike. Its loose fictional affinity is mass, impact, armor, and force redirection. The affinity does not impose an exclusive recipe or weapon category. See [Specialized Resource Identities](./61-specialized-resource-identities.md#barysteel-b).

## Base-travel time

The map-generation distance measure: how long the shared unmodified 3.0M/s mech would take to follow the shortest valid route between two points with no enemies or stops. One base-travel second equals 3.0M. PowerUps, mech traits, utilities, relics, and temporary effects do not change a site's generation band. Standard Near, Middle, and Far bands end at 45, 90, and 150 seconds from deployment.

## Blueprint

An unlocked fabrication design that defines a craftable weapon, upgrade, or utility and its recipe, effect, and price. The unlocked blueprint catalog remains fixed during a level; the randomized resource profile changes which blueprints are feasible or economical rather than changing what they do.

## Boss loot burst

The physical non-modal resource explosion produced by defeating an interval boss. Every burst contains 300 common ore and 25 unsecured Hyper Gold. The first two bosses add one random present-profile specialized-material unit; the final two add two independently selected units. Pieces scatter onto valid nearby ground, persist and remain marked on the minimap until collected or run end, and are collected automatically on mech contact while combat continues.

## Cinderglass

Specialized ordinary resource `C`: dark energized glass presented through jagged plates, traveling violet fissures, and a brittle crack followed by an electric snap. Its loose fictional affinity is charge, conduction, propagation, and controlled release. The affinity does not establish an elemental damage type. See [Specialized Resource Identities](./61-specialized-resource-identities.md#cinderglass-c).

## Common basic ore

The unnamed universal ordinary crafting resource. A 15-second standard ore seam awards 10 ore every 1.5 seconds for 100 total; a 15-second rich seam awards 40 every 3 seconds for 200 total; a completed material geode awards 50. Selling a newly discovered or displaced relic and collecting boss loot provide non-mining ore. It purchases individual weapon stat ranks, non-radar utility ranks, and universally available recipes such as the resource radar.

## Contact damage

Damage dealt while an enemy's contact footprint overlaps the mech's 1.0M circular footprint. The same enemy may repeat contact once every 0.75 seconds and a resolved contact gives the mech 0.20 seconds of global contact-only grace. Enemies do not physically block the mech. Ordinary contact causes no hitstun, knockback, forced movement, control loss, or mining interruption. Exact footprints and damage margins appear in the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md).

## Control resistance

An enemy percentage that reduces player-authored displacement magnitude and timed-control duration. A value `R` produces `authored value × (1 − R)`; slow magnitude is unchanged while its duration is reduced. Driftmetal resonance multiplies the resisted result by 0.80 rather than adding resistance points. Slows do not add, hard-control durations do not add, and ordinary enemies, elites, and bosses receive 0.25, 0.75, and 1.50 seconds of hard-control immunity after an effect ends. See the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md#control-resistance-and-status-stacking).

## Crafting

Spending run resources to create weapons or upgrades. Ordinary crafting develops only the current run; Hyper Gold supports cross-run upgrades. The player can open the fabrication menu anywhere and at any time without an access limit, and the entire gameplay simulation freezes while it is open. Recipes, prices, and outcomes are fixed; the map's resource profile changes which recipes can be fulfilled. See [Resources, Crafting, and Progression](./60-resources-crafting-progression.md).

## Crafting break

A player-invoked freeze of the gameplay simulation during which weapons or upgrades can be crafted. The fabrication menu can be opened anywhere and as often as desired; available resources and recipe rules constrain actual purchases.

## Fabrication menu

The on-demand run-console destination for crafting weapons, weapon ranks and branches, utilities, and utility ranks during a run. It is available anywhere without charges, milestones, or location requirements. The entire gameplay simulation freezes while it is open; only the interface continues. It provides Weapons, Weapon Upgrades, Utilities, and Survey pages with exact costs, comparisons, compatibility, and irreversible-choice warnings.

## Facing

The mech's persistent orientation used by facing-based automatic weapons and presentation. It begins east, or screen-right, at deployment. Nonzero movement input sets facing to the movement direction. When the mech stops, facing retains the last nonzero direction; it does not automatically track an enemy or accept a separate aim input.

## Direct movement

The standard movement model in which input immediately sets the mech's direction and full movement speed and release stops it immediately. The shared unmodified speed is 3.0M/s. Digital input supports eight normalized directions and analog input supports the full circle. There is no acceleration, braking lag, momentum, turn radius, sprint, dash, dodge, stamina, reverse penalty, or strafing penalty.

## Driftmetal

Specialized ordinary resource `D`: polarized alien metal presented through paired compass-like fins, slow directional realignment, and a panning whir. Its loose fictional affinity is direction, momentum, displacement, and geometry. The affinity does not make it exclusively a mobility or crowd-control material. See [Specialized Resource Identities](./61-specialized-resource-identities.md#driftmetal-d).

## Eidolon Coral

Specialized ordinary resource `E`: a branching techno-organic mineral lattice presented through a three-lobed node, synchronized pulses, and layered harmonic audio. Its loose fictional affinity is coordination, autonomous systems, cycling, and reserves. It does not imply that every associated item is biological or summon-based. See [Specialized Resource Identities](./61-specialized-resource-identities.md#eidolon-coral-e).

## Elite alien

A visibly enhanced stat-only version of one of the initial pure ordinary pursuers. It has four times maximum Hull, 1.10 times movement speed, 1.50 times contact damage, 1.25 times body scale, and 25 additional control-resistance percentage points up to 90%. It adds no new behavior or loot. Needler has no elite form in the initial standard schedule. See the [Initial Alien and Boss Roster](./31-initial-alien-roster.md#elite-treatment).

## Flux Amber

Specialized ordinary resource `F`: metastable translucent resin presented through spiral-cored droplets, moving inclusions, and an unsteady two-tone warble. Its loose fictional affinity is multiplication, instability, conversion, and unusual spatial patterns. It is not inherently rarer, stronger, or more dangerous than another specialized material. See [Specialized Resource Identities](./61-specialized-resource-identities.md#flux-amber-f).

## Geological survey

The map information that appears automatically 0.5 active seconds after deployment during the one-minute minor-wave orientation phase. Its compact non-modal card stays expanded for 12 active seconds and lists every specialized material present using its name, distinct icon, visual sample, detected geode count, and corresponding abundance label, but not geode positions. It remains reviewable through the paused fabrication interface for the rest of the run. See [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md).

## Interval boss

A boss alien scheduled as a periodic threat and build-readiness check. Riftjaw, Brood Titan, Prism Crown, and Skybreaker Apex arrive at 7:00, 14:00, 21:00, and 28:00 after a 15-second warning. Each has one defining behavior, persists while the ordinary horde continues, and may overlap a later boss. Bosses never despawn for distance and do not gate extraction, even if still alive at 35:00. See the [Initial Alien and Boss Roster](./31-initial-alien-roster.md#interval-boss-overview).

## Depleted mining point

A finite mining point whose available resource quantity has been fully extracted. It remains as a mapped, non-interactive landmark and cannot provide more resources or reactivate during the current run. Exact depleted-point audiovisual presentation remains open.

## Destructible rock

A dynamically replenished non-enemy breakable world object separate from mining points. A rock has 100 Hull, zero Armor, a non-solid 0.80M weapon-damage footprint, and no control response. Standard mode begins with and maintains up to 16 active rocks through one offscreen replenishment attempt per active-simulation second at 10% success. Valid new rocks lie 18–45M from the mech and at least 2M beyond the current view. Each destroyed rock has a fixed 20% chance to release one 25-Hull health pack and otherwise releases nothing. Rocks never award resources or temporary effects and are not tracked by the geological survey, resource radar, or persistent map.

## Health pack

The sole ordinary recovery pickup in standard mode. Each destroyed rock independently has a fixed 20% chance to drop one. A pack has a 0.25M pickup radius, persists until the mech touches it or the run ends, restores 25 Hull Integrity immediately without exceeding maximum Hull, and is consumed even when collected at full Hull. It is not attracted beyond its contact area, tracked on the persistent map, or affected by PowerUps unless a later explicit rule says otherwise.

## Horde damage throughput

Total weapon damage dealt across all ordinary enemies divided by active-simulation time in a fixed finite crowd benchmark. It is reported with defeats, unique targets, overkill, coverage, and ending active count so unlimited piercing or area cannot claim an unbounded paper value. See the [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md).

## Ideal single-target DPS

The sustained damage-per-second estimate against one stationary, unarmored, boss-sized target held in a weapon's intended legal geometry with no competing targets. It is an arithmetic anchor rather than expected run output and is always compared with realized boss DPS, horde throughput, reliability, control, and positional burden. The 15 rank-zero values average 31.7 DPS in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md).

## Realized boss DPS

Weapon-attributed boss damage divided by boss lifetime during a standard arrival-minute encounter with its ordinary horde still active. It includes targeting competition, misses, range loss, movement, setup, and defensive repositioning and therefore measures what a build actually delivers rather than its isolated ideal DPS.

## Horde director

The deterministic minute-authored schedule that supplies standard enemy pressure. Each active minute defines a weighted composition, desired minimum population, replenishment batch and interval, and any formation or boundary event. Ordinary enemies enter just outside the camera on valid ground and may be recycled when far away; the schedule does not adapt to the current build or player health. See the complete [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md).

## Hull Integrity

The mech's player-facing health measure. The shared baseline is 100 maximum, zero passive Recovery, and full current Hull Integrity at deployment before modifiers. Damage reduces it; reaching zero causes death unless an explicit revival applies. Repairs restore it only up to the current maximum; collecting one at full Hull Integrity consumes it without effect.

## Cross-run upgrade

An upgrade whose effect persists into future runs and is purchased with Hyper Gold. The two categories are capped account-wide numerical PowerUps and permanent option unlocks. The thirteen numerical tracks have accepted effects, prices, caps, optional active ranks, and a complete free refund. The initial six-purchase [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md) costs 2,150 Hyper Gold; its purchases remain permanent, nonrefundable, and non-disableable. Permanent stats provide a substantial early-run advantage but remain weaker than a coherent run build.

## Permanent option unlock

A nonrefundable between-run Hyper Gold purchase that permanently expands future content availability without directly adding an account statistic. The initial catalog contains one six-blueprint utility bundle and five relic-pool additions. Unlocking an option never grants it inside the current run or bypasses geology, recipes, resource costs, slots, cache randomization, or other run-local acquisition rules. Owned option unlocks cannot be disabled and are unaffected by Refund PowerUps.

## PowerUp

A capped permanent upgrade purchased once with Hyper Gold and applied account-wide to every current and future mech. The thirteen-track [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md) covers combat, survivability, mobility/exploration, and mining/economy for 9,450 total Hyper Gold. Purchased tracks may use any lower active rank between runs. Their combined effect substantially eases early standard play but cannot replace exploration, fabrication, or a mature late-run build. No PowerUp increases fixed specialized-material, boss, or Hyper Gold payouts. Refund PowerUps resets all numerical ranks and returns their complete actual cost without affecting permanent option unlocks.

## Level

A large, finite, bounded timed-survival play space. Remaining alive until its time limit completes the level through mission extraction. Each run consists of exactly one level, with a standard target of 35 active-simulation minutes.

## Mining extraction

The process that produces resources while the player remains near a mining point. Use the qualified term when mission extraction could also be in context.

## Extraction progress

The unfinished progress toward a mining installment or completion. It advances automatically inside the circular mining zone. Leaving grants 0.5 seconds without loss, after which progress decays linearly at four times that point's forward extraction rate. Completed ore-seam installments are checkpoints and never decay; a material geode or Hyper Gold site withholds its reward until full completion.

## Mining point

A resource-bearing location on the map with a clearly visible circular extraction zone. Mining activates automatically while the player remains inside it. The accepted initial classes are 15-second standard and rich ore seams, 20-second material geodes, and 45-second Hyper Gold sites. A standard map contains 20 standard seams, 8 rich seams, 8–10 geodes of each of its four present specialized materials, and 3 Hyper Gold sites. The [Standard Map Generation Contract](./51-standard-map-generation-contract.md) fixes initial spatial distribution and clearance, and [DEC-128](./decisions/DEC-128-set-extraction-zone-and-resonance-field-radii.md) fixes the 3.0M zone radius; remaining interaction edge cases and depleted-point presentation remain open in [OQ-004](./open-questions.md#oq-004--how-does-a-mining-point-behave).

## Hyper Gold

The single resource used to purchase both permanent numerical PowerUps and permanent option unlocks. A standard map contains three randomized Hyper Gold sites. Each completion-only site requires 45 seconds of forward extraction, awards 100 Hyper Gold, and summons a focused alien response at activation and the 25%, 50%, and 75% thresholds. Each of four bosses also drops 25 physical Hyper Gold. Collected Hyper Gold remains unsecured until mission extraction; dying beforehand forfeits it. The standard-run ceiling is 400. The accepted initial spending catalogs total 11,600 Hyper Gold.

## Material geode

A single-use 20-second completion-only mining point containing exactly one unit of one specialized material and 50 common ore. Each present material has eight, nine, or ten geodes on the map. An unopened geode projects a larger material-specific [resonance field](#resonance-field) around its extraction zone and pays nothing until fully opened.

## Mission extraction

The successful completion of a level after the player survives until its time limit. Mission extraction triggers automatically at 35:00 even if bosses remain, banks unsecured Hyper Gold, ends the run build, and proceeds to results. Its exact audiovisual presentation remains open; it is distinct from mining extraction.

## Mech relic

A run-local, mech-wide behavioral modifier found through map exploration rather than fabrication. The mech can have only one installed relic. Discovery freezes the complete gameplay simulation. A new initial relic may be sold for 150 common ore, retaining the active relic, or installed; installation replaces the active relic and automatically sells the displaced relic for 150 ore. Relics are intentionally powerful and change gameplay through unusual rules or tradeoffs rather than ordinary passive bonuses. The ten accepted initial effects appear in the [Initial Relic Catalog](./69-initial-relic-catalog.md); system rules appear in [Mech Relics](./67-mech-relics.md).

## Mech diameter (`M`)

The relative spatial unit used throughout combat. `1M` equals the 1.0M collision diameter of an unmodified player mech, which moves at 3.0M/s. It keeps weapon ranges, widths, radii, displacement values, collision footprints, and map travel comparable before final engine coordinate scale is chosen.

## Playable mech

A selectable player character used for one run. Each playable mech has a fixed signature starting weapon and one slotless inherent trait. The six fresh-profile initial mechs are Kestrel, Pike, Prospector, Lodestar, Bastion, and Razorback; their accepted pairings, traits, silhouettes, and selection rules are defined in the [Initial Mech Catalog](./36-initial-mech-catalog.md). Final presentation names, numeric tuning, and later-roster identities and unlock requirements remain open.

## Ordinary resource

One of multiple resource types used to craft weapons or upgrades for the current run. Mining is the primary source of common basic ore and specialized ordinary resources. Relic sales and boss loot add common ore; boss loot also adds six total units randomly selected from the four present specialized materials if all bosses are defeated and their pieces collected. Awarded units remain available for later spending during the run and are discarded at its end. The six specialized identities, weapon assignments, geode unit payouts, initial recipe quantities, and [utility assignments](./68-utility-catalog.md) are fixed, while fungibility, carrying limits, and numeric tuning remain open in [OQ-013](./open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase).

## Pair-weapon

One of the 15 equal-tier normal base weapons defined by an unordered pair of the six specialized resource families. Fabrication costs one unit of each paired material, and both must occur in the profile. A signature weapon is a normal pair-weapon granted at deployment without paying that recipe rather than a separate weapon class. A mech cannot equip duplicate copies of any pair-weapon. See the [Weapon Specification Index](./weapons/README.md).

## Opening orientation phase

The first minute of active simulation, from deployment through 1:00. The geological survey becomes available while deliberately minor enemy waves approach. The run timer advances normally, the player retains full control, and standard escalation begins at 1:00.

## Ordinary enemy identity

A named, visually readable normal-wave alien with its own fixed base statistic profile. The initial roster has ten: Skitterling, Ripper, Shellback, Lurker, Gloomwing, Needler, and four readable family variants. Needler is the sole ordinary specialist; elites and bosses are not included. Each authored minute uses no more than three identities, apart from brief overlap by survivors of the preceding minute. Reusing an identity later does not invisibly inflate it. See the [Initial Alien and Boss Roster](./31-initial-alien-roster.md).

## Ordinary pursuer

The default alien behavior: continuously move toward the mech and deal contact damage. Most ordinary enemy identities and elites share this behavior and differ through health, speed, damage, size, control resistance, appearance, density, and wave placement.

## Push-your-luck

A decision pattern in which the player chooses whether to accept further risk in pursuit of additional reward. Mining creates this through constrained dodging space, rapid progress decay after leaving, completion-only material geodes, geode resonance fields, and the four escalating Hyper Gold response packages in the [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md). Spatial and numerical tuning remains open in [OQ-005](./open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system).

## Rare special resource

An earlier descriptive term for [Hyper Gold](#hyper-gold), retained for interpreting historical decision records. It is not the current player-facing name.

## Recovery

The rate at which a mech passively restores Hull Integrity. The shared baseline is zero; regeneration occurs only when an explicit mech trait, PowerUp, relic, or utility provides Recovery.

## Relic cache

One of exactly three clearly recognizable relic-bearing world objects placed at randomized locations on every standard map. The run assigns three distinct relics without replacement from the unlocked pool. A cache has no dedicated guard or through-fog bearing, but its tall silhouette, ground emblem, and vertical signal identify it once in view. It opens automatically when touched and freezes the complete gameplay simulation. The contained relic must be installed or sold before play resumes; the player cannot store it for later.

## Resource profile

The four specialized resource families selected from the game's six-family set for a level, plus whether each has eight, nine, or ten geodes. Every profile theoretically supports exactly six of the 15 normal pair-weapon recipes. Generation includes at least two of the selected signature weapon's three branch resources. The geological survey discloses the profile and counts after deployment during the active opening, but not geode locations.

## Resource radar

An always-offered run-local utility blueprint crafted for 300 common ore. While installed, it continuously shows up to seven active-play screen-edge indicators: one toward the nearest remaining unopened geode of each of the four present specialized materials, plus the nearest nondepleted standard ore seam, rich ore seam, and incomplete Hyper Gold site. It automatically retargets each category, requires no target selection or pause, gives no exact map marker or distance, and permanently commits one utility slot for the run. Overlapping bearings fan or cluster according to the interface specification.

## Results screen

The mandatory post-run summary shown after extraction, death, or confirmed abandonment. Its Summary, Build and Combat, Mining and Economy, and Exploration pages report outcome and time, kills and bosses, final build and account PowerUps, weapon damage, mining, ordinary-resource collection and spending, Hyper Gold banking or loss, and exploration. Unlock notifications follow settlement before returning to the hangar.

## Resonance field

The visible circular danger area projected by an unopened material geode, larger than its extraction zone. Its radius is 6.0M under [DEC-128](./decisions/DEC-128-set-extraction-zone-and-resonance-field-radii.md), twice the extraction zone's. Enemies inside receive that material's thematic 20% modifier. The effect ends when an enemy leaves or the geode opens. Standard map generation prevents resonance fields from overlapping.

## Rich ore seam

A finite, relatively uncommon common-ore mining point that awards 40 ore every 3 seconds for five installments, 200 total, and depletion after 15 uninterrupted seconds. A standard map contains 8 at randomized locations. It yields twice as much ore per extraction time and per complete seam as a standard seam but requires twice as long before each secured installment. It is run-local and distinct from Hyper Gold.

## Relic slot

The mech's single loadout space for one active [mech relic](#mech-relic). It is separate from weapon slots, utility slots, weapon stat upgrades, weapon branches, and the mech's inherent trait.

## Run

One complete timed deployment into a single level. Standard mode lasts 35 minutes of active simulation; pause menus, fabrication, relic resolution, blocking modals, operating-system suspension, and focus loss do not advance that timer. Mission extraction successfully culminates the build and ends the run; death or confirmed abandonment ends it in failure. Ordinary resources, equipment, and run-local upgrades reset at every ending.

## Run-local

Existing or applying only within the current run. Ordinary resources, weapons, utilities, weapon upgrades, and relics are run-local and are discarded when the deployment ends.

## Run console

The shared fully paused in-run interface shell containing Status, Fabrication, Map, Settings, and Controls. Pause, Fabrication, and Map inputs open their destination directly; switching destinations never resumes gameplay. Relic resolution is a separate blocking modal and cannot be bypassed through the run console.

## Specialized resource

One of six run-local ordinary crafting material families other than common basic ore: Asterite, Barysteel, Cinderglass, Driftmetal, Eidolon Coral, or Flux Amber. A level's resource profile selects four and assigns each eight, nine, or ten one-unit geodes. Each unordered resource pair defines one normal base weapon, while individual resources fund major weapon branches and non-radar utilities. Each material has redundant non-color recognition cues and a soft fictional affinity, not a rigid equipment school. This term does not include [Hyper Gold](#hyper-gold), which serves cross-run progression.

## Specialist alien

A rare non-boss enemy with one behavior that departs from ordinary homing contact pursuit. The initial standard map has exactly one: Needler, a pursuer that charges for 0.8 seconds and fires one straight, non-homing projectile every 4.5 seconds at its initial tuning. It debuts in minute 16 and is included among the ten ordinary identities.

## Standard mode

The baseline single-player 35-minute run to which unqualified gameplay rules apply. It targets an approachable, escalating survivor-like difficulty that is plausibly completable on a fresh account. Alternate difficulties, durations, and multiplayer require explicit mode-specific rules.

## Standard ore seam

A finite common-ore mining point that awards 10 ore every 1.5 seconds for ten installments, 100 total, and depletion after 15 uninterrupted seconds. A standard map contains 20 at randomized locations. Every completed installment is retained; only progress toward the current unfinished installment can decay.

## Super resource

An earlier working name for [Hyper Gold](#hyper-gold), retained for interpreting historical decision records. It is not the current player-facing name.

## Stat upgrade

A deterministic improvement purchased separately for one property in an equipped weapon's fixed, weapon-appropriate bundle of at most three common-ore stats by default. A weapon may expose fewer, although every weapon in the accepted initial catalog uses exactly three. Stat upgrades cost common basic ore rather than XP and do not consume additional weapon slots. Individual ranks have no explicit cap and each adds a fixed linear amount. The next price is shared across that weapon's stats and rises nonlinearly with its total purchased [upgrade depth](#upgrade-depth), not with the selected stat's personal rank.

## Upgrade depth

The total number of common-ore stat ranks purchased across all stats on one weapon. Buying any stat raises that weapon's depth by one, so every stat on the weapon shares the same next-purchase price. Purchase number `n` costs `5n(n + 1)` ore. Each weapon tracks depth independently; specialized-material branches do not increase it.

## Signature weapon

One of the 15 normal pair-weapons, assigned as the automatic weapon in a particular playable mech's starting loadout. It is equipped without paying its recipe when that mech deploys and provides the run's initial attack before additional weapons are fabricated. Other mechs may fabricate it under normal rules. Profile generation guarantees at least two of its three branch-resource colors.

## Utility slot

One of three loadout spaces reserved for passive or automatic non-weapon support systems. The content-complete [Utility Catalog](./68-utility-catalog.md) has twelve non-radar utilities, two assigned to each specialized material; every four-material profile offers eight of them plus the common-ore radar. Fabricating a utility permanently fills its slot for the run; it cannot be removed, replaced, dismantled, sold, or refunded. Each installed non-radar utility has three ore-funded ranks costing 50, 100, and 150. The resource radar occupies one utility slot but has no initial ranks; the mech's inherent trait occupies none.

## Weapon slot

One of four simultaneous weapon spaces. The selected mech's signature weapon occupies one at deployment, leaving three spaces for additional crafted weapons. Every occupied slot must contain a different weapon identity. Fabricating a weapon permanently fills its slot for the run; it cannot be removed, replaced, dismantled, sold, or refunded.

## Weapon branch

One of three larger mutually exclusive weapon-specific upgrades purchased for exactly two units of its assigned specialized material. Every weapon offers an amplification branch that is “samey but bigger and better,” a functional variant that is “a bit different in function,” and a playstyle conversion that is “much different in play style.” One recipe color funds amplification, the other funds the functional variant, and the assigned off-color funds the conversion. These categories measure behavioral change rather than power. Branches are immediately eligible without ore-rank prerequisites, inherit the weapon's existing stat tracks, and are irreversible for the run. All 45 initial-catalog branches and their first-playable values are accepted in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md); those numbers remain adjustable through benchmarked playtesting.

## Wave-event enemy

An enemy spawned through a scheduled temporary formation such as a fixed-direction swarm, wall, stream, or encirclement. Its challenge comes from geometry, timing, density, and statistics rather than complex AI.

## Threat beacon

The focused alien response caused by mining a 45-second Hyper Gold site. It activates with the first extraction progress and escalates once each at 25%, 50%, and 75%. Each trigger uses a phase-scaled current-roster formation, with elites added to the final two packages. Leaving halts further escalation while absent but does not remove summoned enemies; completion stops new responses while survivors remain. Material geodes instead use non-escalating local resonance fields. See the [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md#hyper-gold-threat-beacon-response).

## Unsecured resource

A collected cross-run resource that will become persistent only if the player reaches timed mission extraction. It is forfeited if the player dies or confirms abandonment beforehand.

## Survivor-like

The broad genre shorthand used for a game based on the high-level *Vampire Survivors* formula. In this specification, the simplest core single-player normal-stage reference behavior is a bounded default for movement, collision feel, camera, enemy pressure and spawning, boss pursuit, pause flow, and results. Explicit decisions override it, and it never restores systems replaced by mining and fabrication. See [DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md).

## Waypoint

The one player-placed navigation marker allowed on explored terrain. It appears on the compact and full maps and as a pin-shaped active-play edge bearing. It provides direction without revealing undiscovered terrain, drawing a route, or reporting distance.
