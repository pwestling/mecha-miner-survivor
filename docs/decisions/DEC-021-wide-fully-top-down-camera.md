---
doc_id: DEC-021
title: Use a Wide Fully Top-Down Camera
status: accepted
authoritative: false
---

# DEC-021 — Use a Wide Fully Top-Down Camera

## Decision

Gameplay uses a fully top-down camera with a wide field of view, following the broad battlefield angle and situational awareness of *Vampire Survivors*. The game is not required to use pixel art.

This decision originally left the final asset medium open so free-asset ecosystems could be audited. [DEC-114](./DEC-114-use-native-low-poly-3d-gameplay.md) subsequently selects native low-poly 3D gameplay through an orthographic projection, with 2D interfaces and mixed-technique VFX.

## Status

Accepted.

## Context

The camera must support dense horde readability, automatic weapon patterns, resource navigation, and constrained movement around mining areas. The project should also maximize its ability to use freely available assets instead of assuming bespoke production.

## Considered options

### Isometric or oblique camera

This can give mechs more visible volume but complicates occlusion, directional readability, and compatibility among asset packs made for different projections.

### Close third-person or over-the-shoulder camera

This strengthens embodiment but changes the survivor-like attention model and sharply reduces battlefield awareness.

### Wide fully top-down camera

This preserves the reference framing, makes spatial pressure legible, and remains compatible with both 2D and 3D asset sources.

## Rationale

The top-down angle best supports the movement-and-mining gameplay already chosen. Leaving the art medium open allows the project to audit free asset ecosystems before committing to a style that cannot supply enough coherent mechs, aliens, environments, and effects.

## Consequences

- Enemy silhouettes, mining boundaries, projectiles, deposits, and hazards must read from above and at wide zoom.
- Important gameplay information cannot depend on front or side details hidden by the camera.
- Pixel art is permitted but not required.
- Exact camera height, world scale, and animation style remain open. Projection, tracking, rotation, asset medium, and reference aspect ratios are resolved by later decisions.
- Asset evaluation should prioritize license clarity, pack breadth, stylistic cohesion, and top-down readability.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [OQ-011 — What is the intended platform and presentation format?](../open-questions.md#oq-011--what-is-the-intended-platform-and-presentation-format)
- [OQ-023 — Which asset medium and visual style best fit the free-asset constraint?](../open-questions.md#oq-023--which-asset-medium-and-visual-style-best-fit-the-free-asset-constraint)
- [RES-005 — Free-asset strategy](../research/RES-005-free-asset-strategy.md)
- [DEC-114 — Use native low-poly 3D gameplay](./DEC-114-use-native-low-poly-3d-gameplay.md)

## Supersedes / superseded by

Narrows the camera reference in [DEC-001](./DEC-001-vampire-survivors-combat-reference.md). Its open asset-medium and projection questions are resolved by [DEC-114](./DEC-114-use-native-low-poly-3d-gameplay.md).
