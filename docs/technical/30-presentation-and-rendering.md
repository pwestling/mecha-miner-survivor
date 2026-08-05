---
doc_id: TDD-PRESENTATION
title: Presentation and Rendering
status: active
authoritative: true
---

# Presentation and Rendering

## Purpose

This document defines how authoritative planar state becomes a readable low-poly 3D game at 60 FPS. It covers scene ownership, synchronization, camera, crowd rendering, durable actors, terrain, materials, lighting, VFX, quality tiers, and presentation degradation.

## Presentation principles

- Gameplay geometry and timing come from simulation snapshots and events.
- The fixed top-down view prioritizes silhouettes, ground contact, threat boundaries, and resource identity over detail visible only from cinematic angles.
- Presentation may interpolate, pool, batch, simplify, or omit cosmetic effects but may not change authoritative state.
- A missing visual resource uses an unmistakable diagnostic fallback; it never removes the simulated entity.
- Critical telegraphs, mining boundaries, pickups, player state, and navigation cues outrank cosmetic density.

## Godot scene boundary

The persistent game scene contains:

- application composition root and run-session bridge;
- orthographic gameplay camera and camera-ground-footprint service;
- static generated-world root;
- durable actor root for player, bosses, mining sites, caches, loot, and landmarks;
- crowd-render batches for ordinary enemies/elites;
- pooled transient-render roots for projectiles, zones, trails, decals, and particles;
- lighting/environment root;
- audio presentation root; and
- CanvasLayer-based HUD and paused interface shell.

Loading a new run replaces the generated-world and run-presentation roots as one operation. Front-end scenes do not retain references to disposed run entities.

## Snapshot synchronization

Presentation consumes the two most recent committed simulation snapshots.

- Render interpolation uses the render-frame fraction between their tick anchors.
- Position and facing interpolate along the shortest valid planar/angle path.
- Spawned actors appear at the newest transform without extrapolating backward.
- Teleports, boss re-entry, large correction, and terminal transitions snap and use a transition effect if appropriate.
- Animation state derives from authoritative flags and interpolated velocity; animation events never create gameplay effects.
- Presentation detects missed event sequence numbers and requests a snapshot reconciliation rather than inventing effects.

The bridge maps simulation entity IDs to presentation handles. Handle disposal is idempotent and tolerant of a death effect outliving its authoritative entity.

## Camera

The gameplay camera is orthographic, north-up, and non-rotating and shows **24 gameplay meters vertically**. At 16:9 this yields approximately 42.7 meters horizontally; at 16:10 approximately 38.4 meters. Agents retain this value through M4 without preference review. A later tuning task may propose a change only with paired reference-layout captures showing a concrete readability failure and proving spawn, HUD-bearing, telegraph, and performance behavior at the replacement fixed scale.

- The camera follows the authoritative player ground point with critically damped visual smoothing limited so the player never visibly leaves the position assumed by warnings and HUD bearings.
- Camera smoothing does not feed back into simulation or spawn geometry; the camera service publishes the actual rendered ground footprint each frame and a conservative next-tick footprint for spawn validation.
- Near map boundaries, the camera clamps to avoid showing beyond authored world treatment where possible. The player may move away from center; HUD bearings and spawn validation use the clamped footprint.
- There is no manual rotation, combat zoom, aim offset, shake-induced gameplay displacement, or perspective scale change.
- Screen shake offsets only presentation, is clamped to a small fraction of player diameter for routine hits, and can be disabled.

The camera uses a shallow perspective-like visual setup only if orthographic projection cannot achieve asset readability; adopting perspective requires a superseding gameplay and technical decision. The default is true orthographic.

## Static world rendering

Generated regions instantiate from validated chunk/landmark manifests, not scene-tree searches.

- Terrain ground uses large batched meshes or chunked surfaces with shared palette materials.
- Solid obstacle presentation aligns with the conservative planar footprint and includes a readable base/contact edge.
- Dressing props that are not authoritative are instanced in spatial chunks and cannot overlap validated interaction or route envelopes.
- Tall geometry is limited or faded when it could obscure the player, enemies, attacks, zones, sites, or exits.
- Map boundary presentation is visible before contact and cannot resemble an opening.
- Static chunks use baked or inexpensive lighting information; no runtime global illumination is required.

## Ordinary enemy crowd renderer

Ordinary enemies and elites do not receive one full Godot scene, Skeleton3D, AnimationTree, physics body, or NavigationAgent each.

Use GPU-instanced batches grouped by mesh family, material variant, animation clip, and LOD. Animation uses baked vertex animation textures or an equivalently measured GPU-instancing technique.

Per-instance data includes:

- interpolated transform and scale;
- animation clip, normalized phase, and playback rate;
- palette/material variant;
- hit-flash and death-transition parameters;
- elite flags/crown variant; and
- optional visibility/fade state.

Batches update contiguous instance buffers each rendered frame. Entity-to-batch slot mappings use swap-remove and generation checks. The renderer may maintain spare capacity but must not allocate per frame.

Because the active horde is centered around pressure rather than spread across the entire map, one batch per visual grouping is preferred initially. Spatially partition batches only if measured offscreen vertex cost or buffer updates justify the additional migration complexity.

### Crowd animation clips

Minimum ordinary clip vocabulary is locomotion, attack/contact tell when applicable, Needler charge, hit response, and death. Pure pursuers do not require bespoke idle behavior during active pursuit. Variants may share motion data when their silhouette remains distinct.

Death may use a short baked clip, collapse pose, dissolve, or pooled effect. It cannot leave one scene node per corpse.

## Player, bosses, and durable actors

The player mech and bosses use ordinary Godot 3D scenes with skinned meshes and animation state machines because their small count, telegraphs, and identity justify the cost. Mining sites, relic caches, and important landmarks use durable scenes but avoid per-frame scripts; one presentation coordinator updates them from snapshot state.

- Boss animation state follows the authoritative behavior state and known phase time.
- Mech facing follows persistent simulation facing while locomotion uses current velocity.
- Weapon mounts and decorative aiming may follow attack events but never alter attack origin.
- Durable scenes expose named presentation anchors through validated asset metadata rather than arbitrary node-path strings in gameplay code.

## Materials and lighting

- Use a controlled shared palette and a small number of atlas/material families.
- Ordinary enemies target one material slot; two require justification.
- The player, resources, enemies, hostile projectiles, friendly effects, and terrain occupy distinct value/saturation families under all supported color-identity settings.
- One primary directional light and restrained ambient/environment light are the baseline.
- Ordinary horde meshes do not cast expensive real-time shadows. Use instanced blob/contact shadows or baked ground-contact treatment.
- Player, bosses, and selected landmarks may cast real-time shadows only within the GPU budget.
- Required state never depends on specular response, subtle normal maps, or shadows.

Shader variants are enumerated and warmed before deployment. Runtime shader compilation during active play fails the frame-stall gate.

## VFX architecture

Simulation presentation events map to versioned VFX recipes. A recipe declares priority, lifetime, pooling class, world/screen space, geometry correspondence, reduced-intensity variants, maximum concurrent count, and degradation policy.

### Priority order

1. player lethal-state and revival feedback;
2. boss, Needler, beacon, and extraction telegraphs;
3. mining zone/progress/resonance boundaries;
4. hostile projectile core and impact geometry;
5. pickups, cache signals, and navigation identity;
6. player attack primary geometry and hit confirmation;
7. enemy hit/death embellishment;
8. ambient and decorative effects.

Lower tiers may never obscure or replace higher tiers. When a pool is saturated, reject or simplify the lowest-priority request and increment a metric.

### Geometry correspondence

- Every damaging area has a stable core or boundary matching the authoritative shape within 0.10M at ground scale.
- Telegraph start/impact/expiry use simulation ticks; presentation may not wait for an animation callback.
- Fast projectiles render a minimum readable streak without increasing collision size.
- Delayed impacts show fixed ground centers throughout the warning.
- Pull, knockback, slow, and hard control use distinct restrained feedback.
- Resonance fields show both zone edge and material identity without color alone.

## Quality tiers

The baseline ships with Low, Medium, and High presentation profiles plus independent accessibility controls. Steam Deck defaults to a measured profile derived from Medium, not an automatically detected moving target.

Quality settings may change:

- ordinary enemy crowd LOD;
- noncritical particle count and trail subdivision;
- ordinary death effect richness;
- real-time shadow scope/resolution;
- decal count/lifetime;
- material detail and environment effects; and
- render scale within a bounded range with UI remaining native-resolution.

They may not change view scale, telegraph duration, effect boundaries, enemy visibility, pickup identity, minimap information, or simulation counts. Dynamic resolution is permitted only if it does not cause unstable text/HUD or obscure required 3D cues; it is disabled by default until device tests justify it.

## Rendering budgets

At the Steam Deck peak benchmark:

| Budget | Target |
| --- | ---: |
| GPU frame time p95 | ≤14.0 ms |
| Render-thread/main submission p95 | ≤3.0 ms |
| Visible ordinary/elite instances | 700 normal ceiling; pathological 900 test |
| Draw calls in active gameplay | ≤250, target ≤180 |
| Unique active materials | ≤64, target ≤40 |
| Real-time shadow-casting actors | player + active bosses only, maximum five |
| Concurrent GPU particles | 20,000 High; 10,000 Deck baseline; 4,000 Low |
| Persistent decals/trail visual segments | 512 pooled |
| Runtime shader compilation stalls | zero after deployment begins |
| Process working set | ≤2.5 GiB during standard play |

Budgets include HUD and representative effects. Lowering legal enemy population is not an allowed optimization.

## Failure and fallback

- Missing model/material/animation uses a bright diagnostic proxy with the correct footprint and ID in non-release builds; release content validation blocks packaging.
- A failed cosmetic effect is logged and omitted. A failed critical telegraph uses a generic high-contrast ground primitive and warning sound.
- Crowd renderer failure may switch affected entities to a minimal instanced proxy, not hide them.
- Presentation rebind from a current snapshot must recover after scene reload without restarting simulation in development builds.

## Verification

- Automated captures at 1920×1080 and 1280×800 compare collision debug overlays with visible geometry.
- Representative scenes cover every enemy, boss ability, mining site, material field, weapon/branch, relic, pickup, and accessibility variant.
- Steam Deck performance runs the TDR-003 benchmark for ten warmed minutes and records CPU/GPU percentiles, draw calls, instances, particles, memory, and pool drops.
- Occlusion tests place critical state beneath every landmark/terrain family and verify fade or alternate presentation.
- Reduced flash, reduced motion, low VFX, and color-identity modes pass the same gameplay-readability cases.

## Related documents

- [Runtime Architecture](./10-runtime-architecture.md)
- [World Geometry, Navigation, and Spatial Queries](./21-world-geometry-navigation-and-spatial-queries.md)
- [Audiovisual Feedback](./31-audiovisual-feedback.md)
- [Asset Pipeline and Budgets](./80-asset-pipeline-and-budgets.md)
- [DEC-114 — Use Native Low-Poly 3D Gameplay](../decisions/DEC-114-use-native-low-poly-3d-gameplay.md)
