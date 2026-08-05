---
doc_id: TDD-WORLD-GEOMETRY
title: World Geometry, Navigation, and Spatial Queries
status: active
authoritative: true
---

# World Geometry, Navigation, and Spatial Queries

## Purpose

This document defines the shared planar geometry used by movement, navigation, combat, mining, spawning, discovery, and procedural validation. One geometry authority prevents visible 3D assets, collision, targeting, and map rules from drifting apart.

## Coordinate spaces

The authoritative coordinate contract is fixed by [TDR-005](./decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md).

| Space | Use | Conversion owner |
| --- | --- | --- |
| Simulation world | all authoritative planar gameplay | simulation |
| Generated-map local | chunk/region authoring before placement | map generator |
| Godot world | 3D presentation and camera | presentation adapter |
| Camera ground footprint | visibility, offscreen spawning, screen-edge bearings | camera service |
| Minimap/fog | explored cells and marker projection | map presentation service |
| UI viewport | HUD and input hit testing | UI layer |

Conversions are explicit named operations. Content never stores raw screen coordinates for world behavior.

## Static geometry manifest

A valid generated map produces one immutable geometry manifest containing:

- outer traversable boundary;
- solid obstacle polygons and conservative presentation footprints;
- walkable connected components and clearance information;
- major-region polygons and adjacency graph;
- connector centerlines and widths;
- landmark and authored-structure footprints;
- mining, cache, deployment, boss-entry, ordinary-entry, and dynamic-rock exclusion zones;
- validated important-site positions and circular zones;
- navigation raster and route-distance data or inputs sufficient to reproduce them; and
- stable generated IDs for every static object and region.

The manifest is canonical for the run. Presentation instantiates assets from it; it does not derive collision from imported mesh triangles at runtime.

## Collision primitives

- Player, enemies, bosses, rocks, pickups, and caches use circles with gameplay-authored radii.
- Solid terrain uses simple polygons expanded by the moving circle's radius for navigation and swept collision.
- Projectiles use circles or swept segments/capsules according to content.
- Beams and rail attacks use finite segments or capsules.
- Cones and fans use angular sectors with an explicit near origin.
- Area attacks, mining zones, resonance fields, boss landings, and discovery zones use circles unless their gameplay definition explicitly requires another readable shape.
- Trails and wakes use ordered capsule segments with controlled overlap.

Decorative mesh bounds never substitute for a gameplay primitive. Debug builds can render every authoritative primitive over the 3D scene.

## Player and enemy movement

The player uses swept-circle movement against the boundary and solid obstacles, resolving the earliest contact and sliding along the remaining tangent. Two iterations are the normal maximum; unresolved penetration is corrected toward the nearest validated free point and recorded as a defect.

Enemies are non-solid to the player and one another but remain constrained by solid terrain. Ordinary pursuit combines a global navigation direction with short-range obstacle avoidance; it does not perform separation steering that would turn the horde into a rigid crowd. Overlap is permitted and presentation may apply small visual offsets that do not affect damage geometry.

Movement never uses 3D animation root motion. Simulation position drives model placement, and locomotion animation derives from planar speed and state.

## Navigation representation

Use a generated static navigation raster with an initial one-meter cell size and eight-connected traversal. Cells store traversability for standard enemy clearance, cost, major region, and distance-to-solid boundary. Separate clearance checks handle larger boss circles.

### Ordinary flow field

- A shortest-path integration field targets the player's current navigation cell.
- Recompute when the player changes navigation cell and at most four times per simulation second; reuse the prior valid field between updates.
- Ordinary enemies sample a descent direction with deterministic tie ordering, then blend a bounded local obstacle correction.
- A direct unobstructed path to the player may bypass the grid direction to avoid stair-step motion.
- If an enemy cannot reach a lower integration value for a bounded duration, mark it stuck and use the offscreen recycle or recovery rule only when not visible and not protected from recycling.

The one-meter/four-Hz baseline is enforceable through M4. If route fixtures fail or navigation exceeds its budget, an assigned tuning task evaluates cell sizes `0.75m`, `1.0m`, and `1.25m` and update caps `2Hz`, `4Hz`, and `6Hz`; it selects the lowest-cost combination that passes every route, stuck, boss-clearance, visibility, and PERF fixture. It may not change shared static navigation or flow-directed ordinary pursuit without a TDR.

### Boss navigation

Bosses use the same field where their clearance permits. When the ordinary raster direction would enter insufficient clearance, a boss requests a route over the region/connector graph followed by a local clearance-aware path. Re-entry remains a boss behavior state and never teleports visibly.

### Navigation exclusions

Mining points, pickups, caches, rocks, the player, and enemies are not navigation obstacles. Only the immutable world boundary and authored solid terrain affect standard routing. Boss ability markers and weapon effects do not modify navigation.

## Dynamic spatial index

Use a simulation-owned uniform spatial hash for dynamic overlap and targeting queries.

- Initial cell width is four gameplay meters.
- Dynamic targetable circles register by center cell with radius retained on the record; large objects are either registered in all overlapped cells or queried with their maximum radius explicitly.
- Stores maintain separate faction/category lists where that materially reduces candidate scans.
- Insert, move, and remove operations occur in the spatial-index phase and are allocation-free after warm-up.
- Query results are scratch-buffered, deduplicated when a record spans cells, and sorted only by the consuming system's deterministic selection keys.
- Long rays traverse cells in geometric order and stop only when their pierce/terrain rule permits.

The index supports circle, annulus, capsule, ray, sector, axis-aligned camera rectangle, nearest-category, and k-nearest queries. A query always states category mask, faction, targetability, terrain behavior, and whether rocks are fallback targets.

## Contact and overlap

Circle overlap uses squared distance and inclusive summed radii. Contact cooldown belongs to the attacking entity, not the overlap pair, because the gameplay rule limits the same enemy's repeated contact. The player's global contact grace is evaluated after attacker eligibility.

The index reports candidate pairs; the damage system owns cooldown, grace, shield, Armor, and Hull changes. Presentation contact effects consume resolved hit events rather than raw overlap.

## Projectile and terrain collision

Fast finite projectiles and charges use swept tests from prior to proposed position to prevent tunneling. Resolve the earliest normalized time of impact; ties use terrain before target unless the attack explicitly pierces terrain, then target ID.

- Non-piercing projectiles stop at the first eligible target or solid terrain.
- Piercing projectiles retain a per-projectile hit set or last-hit policy so one target is not damaged repeatedly in adjacent ticks unless specified.
- Area explosions query at their committed center after terrain/impact correction.
- Enemy projectiles snapshot resonance-adjusted damage at creation as required by gameplay rules.
- A visual projectile may interpolate or arc, but collision follows the authoritative planar path and timing.

## Spawn and re-entry geometry

The camera service publishes its current ground footprint to the simulation. Valid spawn positions must:

- lie on navigable ground with actor clearance;
- be outside the footprint plus an authored safety margin;
- not overlap the player, active mining zone, important interaction, solid terrain, or another prohibited envelope;
- have a valid navigation route toward the pressure area; and
- avoid unavoidable immediate damage paths.

Ordinary director spawning samples eligible perimeter sectors and validates candidates in deterministic sequence from its stream. Failure queues the spawn rather than placing it visibly. Boss entries additionally select a warning bearing and restart ability cooldown after re-entry.

## Offscreen recycling

Only ordinary baseline/event enemies and eligible dynamic rocks may recycle. A candidate must be outside the camera safety envelope for the configured duration or distance and must not be a boss, beacon-tagged enemy, protected elite, active attack owner whose removal changes a visible attack, or object with persistent loot/state.

Recycling is a state-preserving relocation for ordinary pressure, not a death: it grants no kill, effect, drop, or statistic. Presentation removes and rebinds the instance offscreen. The destination passes the same validation as a fresh spawn.

## Discovery and fog

The minimap uses a two-meter exploration raster derived from static geometry. At every committed player position:

- all cells inside the camera ground footprint are revealed;
- a four-meter margin beyond the footprint reveals nearby route continuity without exposing distant sites;
- solid boundaries and obstacles intersecting the revealed area are recorded; and
- newly visible important objects become discovered when their presentation footprint enters the camera footprint and is not completely occluded by an authored landmark mask.

Discovery is permanent for the run. Radar bearings do not mark fog or discover the target. Boss loot bypasses discovery and receives its required immediate marker. Reveal radius and margin are content-tunable presentation values, but the invariant that everything visibly on-screen is mapped must remain.

## Geometry validation

Automated validators prove:

- the player's clearance-adjusted ground is one connected component covering every required site;
- all important zones and maneuvering envelopes avoid solids and one another as specified;
- connector widths and optional-pocket limits satisfy the gameplay map contract;
- every resource, cache, boss-entry, deployment, and landmark constraint passes using route distance;
- navigation produces a route between deployment and every important site for standard and boss clearance;
- camera-edge positions retain valid offscreen spawn sectors;
- presentation footprints do not hide interaction zones at the fixed camera; and
- no imported scene scale or pivot changes its declared gameplay footprint.

## Performance budgets

Steam Deck peak budgets within the 60 FPS contract:

- all dynamic spatial-index maintenance and ordinary queries: 1.0 ms at 95th percentile;
- navigation-field maintenance plus enemy direction sampling: 1.0 ms;
- player, enemy, boss, and projectile movement/collision: 1.0 ms;
- zero steady-state managed allocations from these systems; and
- navigation plus exploration data under 64 MiB for a maximum standard map.

Budgets are measured separately and together because cache behavior and query mixtures matter.

## Verification

- Geometry unit fixtures cover tangent contact, high-speed tunneling, corner sliding, inclusive zones, circle/polygon clearance, ray tie order, and pierce.
- Generated-property tests create thousands of random valid and invalid layouts and compare validator results with independent slow reference queries.
- Navigation fixtures cover every connector width, pocket, boundary edge, boss radius, and stuck recovery.
- A debug overlay shows IDs, circles, paths, flow vectors, spatial cells, camera safety envelope, exclusions, and route-distance bands.
- Stress tests run peak actors through dense mixed geometry and verify budgets and allocation invariants on Steam Deck.

## Related documents

- [Simulation Core](./20-simulation-core.md)
- [Procedural Map Generation](./50-procedural-map-generation.md)
- [Presentation and Rendering](./30-presentation-and-rendering.md)
- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md)
