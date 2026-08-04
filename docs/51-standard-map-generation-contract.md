---
doc_id: GDD-MAP-GENERATION
title: Standard Map Generation Contract
status: active
authoritative: true
---

# Standard Map Generation Contract

## Purpose

This document defines what a player can expect from every valid standard-map seed. It specifies observable scale, topology, distribution, fairness, variation, and presentation guarantees without selecting a generation algorithm. Numbers labeled **initial baseline** are accepted starting points for playtesting rather than claims of final balance.

## Distance language

Map distances are expressed as **base-travel time**: the time the shared unmodified mech would need to follow the shortest valid route between two points with no enemies or stops. Account PowerUps, mech traits, utilities, relics, and temporary effects do not change which distance band a location occupies during generation.

The initial distance bands are:

| Band | Base-travel time from deployment | Route distance at 3.0M/s |
| --- | ---: | ---: |
| Near | Up to 45 seconds | Up to 135M |
| Middle | More than 45 and up to 90 seconds | More than 135M and up to 270M |
| Far | More than 90 and up to 150 seconds | More than 270M and up to 450M |

These bands express route distance through traversable ground, not straight-line distance through obstacles.

## World scale and major regions

- A standard map contains **six to eight major regions**, with seven as the initial target.
- The shortest valid route between the two most distant traversable points takes **4:00–5:00 of base travel**, or 720–900M at the accepted 3.0M/s baseline, targeting roughly 4:30 / 810M.
- Deployment occurs in an interior portion of a randomly selected major region. Every important world location is within 2:30 of base travel from deployment.
- Regions are large enough to support normal survivor-like circling, hundreds of pursuing enemies, and a complete mining circle without forcing the mech into a connector.
- Region boundaries may be visual, topographic, or loosely implied. They do not need to be rooms enclosed by walls.
- Each region has one prominent navigational landmark or terrain composition that is distinguishable at normal gameplay zoom. No two regions use the same primary landmark in one seed.

The starting scale is intended to make crossing the world a meaningful commitment without making advertised resources practically unreachable during a 35-minute run.

## Topology contract

### Redundant major routes

- Every major region connects to at least two other major regions.
- Removing any one major connector cannot isolate a major region or a substantial portion of the map.
- The major-region layout contains multiple loops, so reversing course is not the only response to discovering pressure or an undesirable route.
- A route that reaches another major region never depends on a compulsory narrow bridge, single-file gap, or corridor.
- Primary connectors are never narrower than one mining-zone diameter and target at least one and a half mining-zone diameters.

### Open combat ground

- Solid collision geometry occupies an initial target of **8–12% of each major-region footprint**, excluding the outer world boundary.
- No major region exceeds 15% solid-obstacle coverage.
- Obstacles form sparse local clusters rather than continuous maze walls.
- A local obstacle cluster can normally be routed around in no more than six seconds of base travel. A longer barrier requires multiple broad openings.
- Terrain cannot routinely invalidate radial, orbiting, backward-firing, ground-targeting, or movement-dependent weapon patterns.

### Optional pockets

- A standard map may contain zero to two optional spur pockets or dead ends.
- A pocket is no more than 20 seconds of one-way base travel from its last route choice.
- Its terminal area provides enough open ground for the complete mining zone plus an additional maneuvering band around it if a mining point can appear there.
- The exit is visually readable from the terminal area.
- Across all pockets, no more than one Hyper Gold site and no more than one relic cache may appear. Near-band guarantees never depend on entering a pocket.

## Deployment and opening fairness

The deployment point changes every run and obeys all of the following:

- It is not inside a spur pocket, against the world boundary, or inside a narrow connector.
- It has obstacle-free space at least one mining-zone diameter around the mech.
- It offers at least two visibly distinct broad departure routes.
- Ordinary enemies can enter from valid offscreen ground in at least three general directions without appearing inside the camera or overlapping the mech.
- No Hyper Gold site, material geode, relic cache, damaging hazard, or automatic-contact choice appears inside the initial camera view.
- One standard ore seam lies 10–20 seconds of base travel away.
- The complete Near band contains at least two standard seams, one rich seam, and at least one geode of each of the four present materials.

The player is not told the directions of these opportunities. The guarantee prevents a resource-starved opening while preserving search and route choice.

## Placement contract shared by important sites

Every mining point, relic cache, and future optional persistent site follows these rules:

- It is reachable from deployment through terrain that accommodates the mech's full collision footprint.
- The player can approach and leave it without crossing collision geometry or the world boundary.
- A mining point's entire extraction circle is clear of solid terrain, with an additional one-mech-width maneuvering band outside the circle.
- Extraction circles never overlap one another. Material-geode resonance fields also never overlap.
- A site is not visually hidden under a landmark, tall prop, boundary treatment, or another interactable from the fixed top-down camera.
- Sites do not stack so tightly that their interaction zones or identity cues become ambiguous. A normal gameplay view targets one to three visible mining opportunities and never contains more than four.
- Random dressing cannot reduce the validated clearance after important sites are placed.

These rules protect the intended mining commitment. Terrain may influence the route to a site, but cannot secretly make its holdout area smaller or more dangerous than its authored behavior indicates.

## Specialized-material geodes

The profile still supplies exactly eight, nine, or ten geodes for each of four present materials. Their placement additionally guarantees:

- Every present material has at least one geode in the Near band.
- Every material is represented in at least five major regions.
- No major region contains more than two geodes of the same material.
- No major region contains more than 30% of all material geodes on the map.
- Geodes of one material do not form a single obvious directional cluster.
- Resonance-field separation is evaluated using the full field, not only the extraction circle.

Abundance changes the number of route options and recoverable units; it does not relegate a material to one remote corner.

## Common-ore seams

The fixed 20 standard and 8 rich seams use these distribution rules:

- Every major region contains at least two standard seams.
- The Near band contains at least two standard seams and one rich seam.
- Rich seams appear in at least four major regions, with no more than two in one region.
- No major region contains more than 25% of all ore seams.
- Neither seam class forms one dominant cluster that makes a single route overwhelmingly superior.

The intended result is regular ore discovery during natural exploration without making the resource radar compulsory.

## Hyper Gold sites

The three Hyper Gold sites:

- occupy three different major regions;
- are separated from one another by at least 60 seconds of base travel;
- include at least one Middle-band site and at least one Far-band site;
- never appear inside the initial camera view; and
- place no more than one site in an optional spur pocket.

All three are reachable in one run, but collecting all 300 site-based Hyper Gold requires deliberate travel and holdout time rather than following the most convenient ordinary-resource route.

## Relic caches

The three relic caches:

- occupy three different major regions;
- are separated from one another by at least 45 seconds of base travel;
- include at least one Middle-band cache and at least one Far-band cache;
- never appear inside the initial camera view; and
- place no more than one cache in an optional spur pocket.

The distribution supports an incidental first discovery and meaningful later replace-or-sell opportunities without placing all relic decisions on one route.

The run assigns three distinct relics without replacement from the unlocked pool after these cache locations are valid. Caches receive no dedicated guard package or global through-fog bearing; their presentation becomes discoverable only when it enters the gameplay view. These content and presentation rules do not alter spatial validation.

## Destructible rocks

Standard mode maintains a dynamic population capped at **16 active destructible rocks** rather than baking a fixed lifetime set into the seed. The run begins with 16 rocks at valid offscreen positions around deployment. During active simulation, the game attempts one replenishment per second at 10% success. A successful attempt fills an empty slot or replaces the farthest eligible offscreen rock at the cap. If no valid new position and eligible replacement exist, the attempt produces nothing and leaves current rocks unchanged.

A dynamic rock position:

- lies 18–45M from the mech and at least 2M beyond the visible camera rectangle;
- lies on valid traversable ground and never inside blocking terrain;
- does not intersect a mining zone, resonance-field source, relic interaction area, boundary, required connector, landmark focal footprint, deployed pickup, or another important object;
- cannot appear or disappear while visible; and
- favors nearby unexplored or recently entered space without becoming a persistent map marker or exploration-completion item.

Every rock has 100 Hull, zero Armor, a non-solid 0.80M weapon-damage footprint, and no response to control. Every destroyed rock independently has a fixed 20% chance to drop one health pack. A pack has a 0.25M pickup radius and repairs 25 Hull on contact. Audiovisual treatment remains production work; the numerical survival rules are fixed in the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md).

## Landmarks, authored structures, and repetition

- One seed uses one coherent primary biome or map theme. Major regions vary through compatible terrain motifs and landmarks rather than stitching together unrelated visual ecosystems.
- Each major region receives a primary landmark independently of its rewards and mining points.
- A primary landmark provides shape, orientation, or environmental identity; it does not guarantee a resource beside it.
- A recurring authored terrain structure may appear at most twice in one map and never in adjacent major regions.
- When a structure supports safe rotation or reflection, its orientation may vary. Important content is rolled independently rather than permanently attached to its familiar local socket.
- Recognizing a structure may help the player understand its local traversal, but cannot reveal where they are globally or which reward is present.
- The first standard-map baseline introduces no mandatory environmental-damage hazards. Mining resonance fields, Hyper Gold beacons, enemies, bosses, and terrain already supply positional pressure. Any later hazard family requires its own readable behavior and placement rules.

## Variation independence

- Resource profile, topology, deployment region, landmarks, mining sites, relic caches, and Hyper Gold sites vary independently except where the validity contract requires distance, clearance, or distribution corrections. Dynamic rocks are selected during play from currently valid offscreen ground.
- Recognizing a topology, chunk, landmark, or biome motif never predicts a particular reward, material, cache, or Hyper Gold site.
- Specialized materials have no preferred region type, landmark, compass direction, or biome subtheme in the initial baseline.
- The same resource profile can occur across substantially different layouts, and a familiar broad layout can host different profiles and reward routes.
- The map does not make the authored horde schedule easier or harder in response to its resource abundance or the selected mech. It supplies valid spawn and re-entry ground for the unchanged director.

## Boundary and fog presentation

- The finite boundary is solid, non-damaging, and visually unmistakable before the mech contacts it.
- Boundary art cannot resemble an unexplored opening or hide a traversable route.
- The complete world outline is not revealed at deployment. The minimap records boundary segments as the player discovers them under the ordinary fog rules.
- The boundary contains no narrow peninsula holding required Near-band content.
- Offscreen spawn and boss re-entry logic always has valid ground when the camera is clamped near an edge; enemies never appear between the mech and the visible boundary merely because exterior ground is unavailable.

## Valid-seed contract

A standard map is invalid and never presented to the player if it violates any of the following:

1. exact resource, relic, or Hyper Gold counts;
2. mech-sized reachability from deployment;
3. major-region route redundancy or connector width;
4. deployment clearance, opening routes, or offscreen entry ground;
5. mining-zone or resonance-field clearance and non-overlap;
6. Near-, Middle-, or Far-band placement guarantees;
7. per-region distribution and anti-clustering limits;
8. relic-cache or Hyper Gold separation;
9. dead-end depth, exit, capacity, or content limits;
10. landmark and authored-structure repetition limits; or
11. clear finite-boundary presentation.

Validation uses navigable route distance rather than straight-line proximity. A visually attractive seed is still invalid if it violates the gameplay contract.

## Playtest targets, not seed guarantees

These targets determine whether the initial numbers should change:

- At least 90% of blind first-run players who begin exploring should discover a mining opportunity within 45 seconds without radar guidance.
- During purposeful travel through non-exhausted regions, the median time between newly visible mining opportunities should be 15–30 seconds and rarely exceed 60 seconds.
- A successful player who explores naturally should reveal roughly 45–70% of the traversable world. Consistently revealing more suggests the map is too small; consistently revealing less suggests it is too large or too slow to read.
- Before the 7:00 boss, a geology-prioritizing route should plausibly produce either one additional weapon or one branch for the signature weapon, plus some common-ore stat investment.
- A normal successful run should commonly discover one or two relic caches. Finding all three should be possible through exploration focus rather than routine.
- One Hyper Gold site should be a plausible incidental discovery. Completing all three should remain an explicit routing priority.
- No clockwise perimeter route, center sweep, landmark sequence, or other repeated pattern should dominate across seeds.
- Profiles may be tactically lopsided, but placement alone should not make a valid recipe or advertised material feel practically absent.

## Initial tuning variables

The following are deliberately easy to revise after generated-map audits and playtests:

- six-to-eight region count and 4:00–5:00 traversable diameter;
- the 45/90/150-second distance bands;
- 8–12% obstacle coverage and six-second obstacle-detour target;
- connector widths relative to the mining zone;
- two-pocket maximum and 20-second pocket depth;
- per-region distribution caps and site-separation times;
- the 16-rock active cap, offscreen spawn validity, one-second attempt interval, 10% spawn chance, and 20% health-pack chance; and
- expected exploration share and encounter cadence.

Changing one of these values does not reopen the large finite world, randomized important locations, open multi-route topology, fixed content counts, mining clearance, or basic fairness contract.

## Related documents

- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Mech Relics](./67-mech-relics.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md)
- [DEC-110 — Use an open multi-route map topology](./decisions/DEC-110-use-open-multi-route-map-topology.md)
- [DEC-115 — Adopt the standard map-generation contract](./decisions/DEC-115-adopt-standard-map-generation-contract.md)
- [DEC-122 — Use destructible rocks as the health-pack source](./decisions/DEC-122-use-destructible-rocks-for-health-packs.md)
- [DEC-123 — Replenish destructible rocks around the player](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
