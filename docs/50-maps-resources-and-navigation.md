---
doc_id: GDD-MAPS
title: Maps, Resource Surveys, Exploration, and Navigation
status: active
authoritative: true
---

# Maps, Resource Surveys, Exploration, and Navigation

## Purpose and player promise

Each level asks the player to absorb a geological opportunity during its one-minute minor-wave orientation phase, then explore a large finite world under escalating pressure to locate the deposits. Compact opening information supports intentional early-run planning; incomplete spatial information preserves navigation, discovery, and risk.

## Randomized resource profile

Each level instance has a randomized **resource profile** that determines:

- Which four of the six specialized resource families exist on the map.
- Whether each present specialized material has eight, nine, or ten geodes and the corresponding Scarce, Moderate, or Rich label.
- The randomized placement of individual deposits. Established deposit classes have fixed yields.

Separate from the specialized-resource profile, every standard map contains 20 standard ore seams and 8 rich ore seams. Their locations are randomized with all other player-relevant elements. Together they hold 3,600 common ore; the profile's 32–40 material geodes add another 1,600–2,000 common ore through their completion jackpots.

Every standard map also contains exactly three randomly located Hyper Gold sites. Each is worth 100 Hyper Gold at full completion, for 300 site-based Hyper Gold per map. They are not part of the four-material geological profile. Four boss loot bursts can add another 100 Hyper Gold, raising the complete standard-run ceiling to 400 if every boss is defeated and every piece collected.

Blueprints, recipe costs, and crafting outcomes do not change with this roll. The geological differences change which fixed recipes are economical or realistically attainable during that run.

The selected mech constrains, but does not reveal, this random roll. Its signature weapon has three fixed branch-resource colors: the two resources in its normal base recipe and one distinct third branch resource. At least two of those three colors must be among the four selected for the map. Among the 12 valid profiles for a particular signature weapon, 9 contain exactly two of its branch colors and 3 contain all three.

## Opening geological survey

The randomized resource profile is hidden during mech selection and becomes available only after the player deploys. The run timer and one-minute minor-wave orientation phase are already active while the player reads it. The map information shows:

- Every specialized resource type known to be present.
- A detected geode count and corresponding abundance label for each present type: `8 / scarce`, `9 / moderate`, or `10 / rich`.
- Enough explanation for the player to connect those resources to relevant known recipes.

Each survey entry repeats the material's player-facing name, distinct icon contour, and visual sample. These use the accepted Asterite, Barysteel, Cinderglass, Driftmetal, Eidolon Coral, and Flux Amber identities rather than bare graph codes or color swatches. See [Specialized Resource Identities](./61-specialized-resource-identities.md).

The map information does not show:

- Exact deposit positions.
- The position corresponding to any detected geode.
- A guaranteed safe or optimal route.

This allows the player to quickly choose an intended build family during the opening phase without pre-solving the exploration route. The information must be concise enough to absorb under intentionally minor, but nonzero, enemy pressure.

The survey appears 0.5 active seconds after deployment as a compact, non-modal card beneath the minimap. Its appearance does not pause the simulation, block movement control, or stop the opening waves. It remains expanded for 12 active seconds, then collapses into the normal resource wallet. The complete survey remains available throughout the run inside the fabrication interface; opening that interface freezes the simulation and the survey's remaining active display time. The [interface specification](./73-interface-screen-flow-and-information-architecture.md#opening-geological-survey) fixes its layout and controller behavior, while first-run teaching remains under OQ-032.

## In-run exploration

The player must explore to turn the geological survey into usable resources. Each level is a large, finite, bounded play space. It does not wrap, repeat infinitely, or extend through endless generation. The complete run therefore contains a finite set of deposits and spatial opportunities, even when the player cannot discover all of them before the timer ends.

Each run significantly randomizes the map layout and the locations of persistent player-relevant elements. No mining point, relic, rare opportunity, landmark, required feature, deployment point, or other meaningful persistent world location is fixed between runs. Elements may be guaranteed to appear, but their locations are randomized. Destructible rocks use a separate dynamic offscreen replenishment system around the player.

The generator may reuse authored terrain chunks or biome structures. Their placement, orientation or connection where applicable, surrounding context, and contained important locations vary so a recurring chunk does not establish a permanent route or reward coordinate. The [Standard Map Generation Contract](./51-standard-map-generation-contract.md) fixes the first-pass player-facing scale, region graph, connection widths, obstacle density, deployment fairness, site distribution, boundary behavior, repetition limits, and seed-validity rules without requiring one technical construction method.

Standard topology is mostly open and multi-route. Six to eight broad major regions support horde combat and circling within mining zones, and neighboring regions use multiple wide connections. Removing any single major connector cannot isolate a region. No compulsory route is narrower than one mining-zone diameter, while solid obstacles target 8–12% coverage and create local positioning choices rather than maze-like funnels. These numeric values are initial playtest baselines.

Optional spur pockets and dead ends may hold randomized opportunities, but each must have a clearly readable exit and enough open space to turn, fight, and perform any placed mining interaction. They cannot serve as the only connection between major regions. Mining-zone placement requires an obstacle-free envelope around the complete extraction circle so procedural collision does not silently tighten its intended commitment.

The generated level must provide valid navigable offscreen entry ground around the player's current camera so the minute-authored horde director can maintain pressure without visibly popping ordinary enemies into view. Bosses that fall far behind likewise need valid offscreen re-entry points. A spawn or reposition point cannot overlap the mech or create unavoidable immediate damage.

The resource profile should change plans without creating unwinnable runs. Every advertised material has exactly its surveyed eight, nine, or ten completion-only geodes, each worth one unit. The 32–40 total geodes make present materials broadly plentiful, although an extreme allocation can demand 11 units of one material and is not guaranteed by the ten-geode ceiling. Every unordered pair of the six specialized resources defines one normal base weapon, for 15 total. A four-resource profile exposes exactly six of those recipes, or 40%. Because duplicate weapons are forbidden, five or six are different from the equipped signature. Profiles may be tactically lopsided; generation and catalog review intervene only when a combination lacks a plausible survival path, strongly encourages immediate restart, or reflects a pervasive graph bias. See [RES-006](./research/RES-006-resource-color-weapon-graph.md).

Unopened geodes project material-specific enemy resonance fields. Standard generation enforces enough separation that these fields do not overlap, while still randomizing every geode location. Each present material appears in at least five major regions, no region contains more than two geodes of one material, and the first 45 seconds of route distance from deployment contain at least one geode of each present material. See [Mining and Extraction](./40-mining-and-extraction.md#geode-resonance-fields).

Every playable mech must remain viable on every profile valid for its signature-weapon constraint because the player chooses a mech before seeing randomized geology. At least two signature branch colors are guaranteed; the other weapons, available third branches, abundance bands, and deposit layout still require adaptation.

## Fogged exploration map

During active play, a compact north-up minimap reveals nearby terrain as the mech explores. It never rotates with mech facing. The player marker uses the same stable world orientation as the fully top-down playfield. Undiscovered terrain, deposits, and relic caches remain hidden by fog. Once observed, the following remain recorded for the rest of the run:

- Explored terrain and known routes.
- Active discovered deposits.
- Depleted deposits in a distinct completed state.
- Discovered landmarks.
- Unopened relic caches that were seen but avoided.
- Opened relic caches in a distinct completed state.
- Uncollected boss-loot piles, which appear immediately when created and remain marked until collected or run end.

The paused run console contains a larger version of this explored map and provides a direct Map shortcut from active play. Reviewing it uses the normal complete-simulation pause. The large map does not reveal anything the player has not discovered.

Both compact and expanded map presentations remain fully usable at 1920×1080 desktop and 1280×800 Steam Deck references. The expanded map has region, route, and local zoom levels; filters for all markers, mining sites, relics and boss loot, or landmarks and waypoint; and one movable player waypoint on explored terrain. The waypoint appears on both maps and as a distinct active-play direction bearing without a path or distance. Inspection, filters, pan, zoom, recentering, marker detail, and waypoint placement are operable with gamepad alone; neither hover nor touch is required.

The resource radar may point toward an undiscovered deposit, but it does not create an exact map marker until the normal discovery rule is satisfied. Exact reveal radius, line-of-sight behavior, and boundary art remain open in [OQ-008](./open-questions.md#oq-008--how-does-exploration-work); map zoom, filters, and waypoint behavior are fixed by the [interface specification](./73-interface-screen-flow-and-information-architecture.md#full-map-and-waypoints).

## Destructible rocks

Standard mode maintains up to 16 active destructible non-enemy rocks as incidental exploration, combat, and recovery objects. The run begins with 16 valid rocks outside the initial camera view around deployment. During active simulation, one replenishment attempt occurs every second with a fixed 10% success chance. A success fills an empty population slot or, at the cap, replaces the farthest eligible offscreen rock with a new valid rock near but outside the player's current view. Rocks never appear or disappear on-screen. If no valid new position and eligible replacement exist, the attempt does nothing.

A valid rock location is traversable, offscreen ground outside mining and relic interaction zones, blocking terrain, required connectors, and the presentation footprint of another important object. Dynamic rocks do not count toward exploration completion, receive persistent map markers, appear in the geological survey, or receive resource-radar directions.

Each non-solid rock has 100 Hull, zero Armor, and a 0.80M weapon-damage footprint. Valid new positions lie 18–45M from the mech and at least 2M beyond the current camera view. Enemy-selecting weapons attack rocks only when no enemy is in acquisition range; geometric attacks may hit them incidentally. Every destroyed rock has a fixed 20% chance to release one health pack and otherwise releases nothing. The pack has a 0.25M pickup radius, remains in the world until the mech touches and automatically collects it or the run ends, and restores 25 Hull Integrity. It is consumed even at full Hull and is never removed when the rock population recycles. Rocks never yield resources or temporary effects. Presentation remains tuning and production work.

## Relics as exploration rewards

Every standard map contains exactly three run-local mech relics in clearly recognizable caches that reward exploration independently of mining and fabrication. Cache locations are randomized with the rest of the map's important content. They are not included in the geological resource survey and cannot be fabricated from ore or specialized resources.

A cache opens automatically when the mech touches it and freezes the complete gameplay simulation. The player must install its relic in the mech's single relic slot or sell it for 150 common ore before play resumes. Installing a later relic replaces the current one and automatically sells the displaced relic for 150 ore. The caches are guaranteed to exist, but the player may discover none, some, or all of them during the 35-minute run. The three occupy different major regions, maintain at least 45 seconds of route separation, and include Middle- and Far-band opportunities. The run draws three distinct relics without replacement from the unlocked pool. Caches have no dedicated guards or through-fog bearing; an in-view silhouette, emblem, and vertical signal reveal them, and observed caches remain mapped. See the [relic-resolution specification](./73-interface-screen-flow-and-information-architecture.md#relic-cache-discovery-and-resolution).

## Resource radar

The **resource radar** is a run-local utility blueprint intended to prevent a player from becoming completely stuck while searching for specialized ingredients needed by an intended build.

### Decided behavior

- The blueprint is available from the beginning of the game and is always present in the fixed fabrication catalog.
- It costs 300 common ore, so the ingredient needed to find a specialized resource cannot itself be missing.
- During active play, the radar simultaneously tracks the nearest remaining unopened geode of each of the four materials known to be present from the geological survey, the nearest nondepleted standard ore seam, the nearest nondepleted rich ore seam, and the nearest incomplete Hyper Gold site.
- Every tracked target produces a continuously updating directional indicator at the corresponding edge of the game screen, for at most seven indicators. The bearing persists even when its target enters the gameplay view and disappears only when the category retargets or exhausts.
- Each indicator carries non-color identity cues for its material or mining-point class; no targeting or retargeting input is required.
- It supplies direction rather than an exact map marker or exact distance.
- It does not consume a weapon slot.
- It occupies one of the mech's three utility slots.
- It does not track relic caches or other non-mining discoveries.
- The player must still traverse the map, survive the route, and perform mining extraction.
- Opening a geode, depleting an ore seam, or completing a Hyper Gold site immediately retargets that category to its next-nearest valid site.
- If no valid target in a tracked category remains, the radar reports that category as exhausted and shows no false direction.

Installing the radar permanently fills its utility slot for that run; it cannot be removed, replaced, dismantled, sold, or refunded. Bearings use category icons and patterns at the safe screen edge; bearings within six degrees fan outward, clusters beyond three collapse with a `+N` count, and exhaustion produces a brief strike-through before the bearing disappears. Exact behavior is defined by the [interface specification](./73-interface-screen-flow-and-information-architecture.md#radar-bearings-and-waypoint-bearing).

### Design role

The radar converts 300 basic ore into navigation certainty. It is a recovery option, not a source of resources. Its substantial cost preserves the consequence of failing to find the desired material naturally, while its directional-only output preserves exploration.

### Risks to test

- If nearly every player crafts it every run, radar guidance may belong in the base interface instead of being an upgrade tax.
- If it is too cheap or too precise, the intended exploration loop becomes direct waypoint following.
- If it is too expensive, it fails as an “out” for a resource-starved build.
- If seven simultaneous directions replace too much exploration or overload the HUD, indicators may need weaker precision, filtering, or presentation changes without reverting to pause-driven operation.

## Feedback requirements

- Resource presence and abundance bands must be quickly readable after deployment while minor enemies are active.
- The abundance vocabulary must have a stable player-facing meaning across maps.
- The game must distinguish survey-level knowledge from deposits actually discovered during the run.
- The minimap must distinguish unexplored, explored, traversable, and blocked space without relying only on color.
- Active deposits, depleted deposits, unopened caches, and opened caches require distinct persistent marker states.
- Uncollected boss loot requires a distinct marker that cannot be mistaken for a deposit or cache.
- Radar directions, material identities, overlapping bearings, and exhausted states must be understandable without relying only on color.
- Reaching or viewing the finite world boundary must not be mistaken for an unexplored continuation or traversable route.

## Open questions

- [OQ-008 — How does exploration work?](./open-questions.md#oq-008--how-does-exploration-work)
- [OQ-011 — What is the intended platform and presentation format?](./open-questions.md#oq-011--what-is-the-intended-platform-and-presentation-format)
- [OQ-013 — What resource types exist, and what does each purchase?](./open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-032 — What onboarding, accessibility, and settings does standard mode require?](./open-questions.md#oq-032--what-onboarding-accessibility-and-settings-does-standard-mode-require)

## Related documents

- [Game Vision](./00-game-vision.md)
- [Core Game Loop](./10-core-game-loop.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md)
- [Standard Map Generation Contract](./51-standard-map-generation-contract.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Specialized Resource Identities](./61-specialized-resource-identities.md)
- [Mech Relics](./67-mech-relics.md)
- [DEC-008 — Use fixed fabrication rules with surveyed randomized resource profiles](./decisions/DEC-008-fixed-blueprints-randomized-resource-profiles.md)
- [DEC-015 — Reveal randomized geology during the active opening](./decisions/DEC-015-in-run-opening-geological-survey.md)
- [DEC-016 — Use a one-minute minor-wave orientation phase](./decisions/DEC-016-one-minute-opening-orientation.md)
- [DEC-017 — Keep the survey reviewable through fabrication](./decisions/DEC-017-persistent-survey-review.md)
- [DEC-009 — Provide an ore-powered directional resource radar](./decisions/DEC-009-ore-powered-directional-resource-radar.md)
- [DEC-087 — Price the resource radar at three hundred ore](./decisions/DEC-087-price-resource-radar-at-three-hundred-ore.md)
- [DEC-088 — Show continuous multi-material radar directions](./decisions/DEC-088-show-continuous-multi-material-radar-directions.md)
- [DEC-089 — Expand the radar to all mining categories](./decisions/DEC-089-expand-radar-to-all-mining-categories.md)
- [DEC-022 — Randomize all player-relevant map locations](./decisions/DEC-022-randomized-map-locations.md)
- [DEC-024 — Use a large finite map](./decisions/DEC-024-large-finite-map.md)
- [DEC-026 — Recombine recurring authored map chunks](./decisions/DEC-026-recombined-authored-map-chunks.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./decisions/DEC-029-pause-and-resolve-relic-discoveries.md)
- [DEC-030 — Place three automatic relic caches on each standard map](./decisions/DEC-030-three-automatic-relic-caches.md)
- [DEC-033 — Use a fogged exploration map with persistent discovery markers](./decisions/DEC-033-fogged-exploration-map.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](./decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [DEC-036 — Use six-color signature-aware resource profiles](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-037 — Use unique weapons and soft profile balance](./decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [DEC-076 — Give the six specialized resources strong non-exclusive identities](./decisions/DEC-076-specialized-resource-identities.md)
- [DEC-077 — Use ore seams and completion-only material geodes](./decisions/DEC-077-ore-seams-and-material-geodes.md)
- [DEC-078 — Give material geodes thematic enemy resonance fields](./decisions/DEC-078-geode-resonance-fields.md)
- [DEC-081 — Place eight to ten geodes for each present material](./decisions/DEC-081-eight-to-ten-geodes-per-material.md)
- [DEC-090 — Place twenty standard and eight rich ore seams](./decisions/DEC-090-place-twenty-standard-and-eight-rich-ore-seams.md)
- [DEC-091 — Name and quantify Hyper Gold](./decisions/DEC-091-name-and-quantify-hyper-gold.md)
- [DEC-097 — Inherit direct movement, collision, and camera](./decisions/DEC-097-inherit-direct-movement-collision-and-camera.md)
- [DEC-098 — Use minute-authored horde waves](./decisions/DEC-098-use-minute-authored-horde-waves.md)
- [DEC-100 — Commit installed weapons and utilities](./decisions/DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-102 — Separate enemy kills from field pickups](./decisions/DEC-102-separate-enemy-kills-from-field-pickups.md)
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](./decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-122 — Use destructible rocks as the health-pack source](./decisions/DEC-122-use-destructible-rocks-for-health-packs.md)
- [DEC-123 — Replenish destructible rocks around the player](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
- [DEC-104 — Show a compact survivor-like active HUD](./decisions/DEC-104-show-a-compact-survivor-like-active-hud.md)
- [DEC-110 — Use an open multi-route map topology](./decisions/DEC-110-use-open-multi-route-map-topology.md)
- [DEC-115 — Adopt the standard map-generation contract](./decisions/DEC-115-adopt-standard-map-generation-contract.md)
- [DEC-111 — Make bosses explode into collectible resources](./decisions/DEC-111-make-bosses-explode-into-resources.md)
- [DEC-113 — Target Windows PC and Steam Deck first](./decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [RES-004 — Run randomization and build agency](./research/RES-004-run-randomization-and-build-agency.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
