---
doc_id: TDR-005
title: Simulate Gameplay on a Two-Dimensional Plane
status: accepted
authoritative: false
validation: geometry-and-presentation-correspondence-tests
---

# TDR-005 — Simulate Gameplay on a Two-Dimensional Plane

## Decision

Represent authoritative gameplay on a two-dimensional horizontal plane even though the world is rendered with native 3D assets. Use explicit two-dimensional circles, segments, rays, capsules, polygons, and zones for movement, targeting, collision, damage, mining, discovery, and spawning. Treat height, ballistic arcs, hovering, and most vertical motion as presentation unless a gameplay definition explicitly assigns timing or ground geometry.

## Coordinate contract

- Simulation X increases east and simulation Y increases north.
- Distances use the gameplay meter `M`; speeds use `M/s`; angles are measured clockwise from north for player-facing bearings and converted explicitly at API boundaries.
- Godot presentation maps simulation east to world positive X, north to world negative Z, and vertical height to world positive Y.
- The authoritative position is the ground-plane center. Decorative model pivots and animation root motion never modify it.

## Rationale

The fixed top-down camera, non-solid enemies, simple circular footprints, automatic weapons, and map contract are planar. A 2D simulation makes broad-phase queries, deterministic tests, content validation, navigation, and batched presentation substantially simpler while preserving the accepted 3D visual medium.

## Consequences

- Godot 3D physics and navigation are not authoritative for horde gameplay.
- Model animation cannot silently change hit geometry.
- Lobbed attacks simulate launch time, impact time, and a ground target; the visible arc interpolates between them.
- Hovering enemies still obey traversable-ground topology unless their gameplay entry explicitly grants terrain traversal.
- Boss leaps remove or change the boss's planar interaction flags during the authored airborne interval and land on a validated ground point.
- Terrain authoring and generation produce both visible 3D geometry and matched planar collision/navigation data from the same validated source.

## Specification links

- [Runtime Architecture](../10-runtime-architecture.md)
- [Combat, Weapons, Movement, and Camera](../../30-combat-weapons-movement-camera.md)
- [Player Survivability and Damage Baseline](../../72-player-survivability-and-damage-baseline.md)
- [DEC-114 — Use Native Low-Poly 3D Gameplay](../../decisions/DEC-114-use-native-low-poly-3d-gameplay.md)
