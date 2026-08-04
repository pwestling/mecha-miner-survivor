---
doc_id: DEC-114
title: Use Native Low-Poly 3D Gameplay
status: accepted
authoritative: false
validation: representative-art-and-performance-prototype
---

# DEC-114 — Use Native Low-Poly 3D Gameplay

## Decision

The gameplay world uses **native low-poly 3D models viewed through the established fixed, north-up, fully top-down camera**. The projection is orthographic so distance from the camera does not change gameplay scale or undermine the battlefield-readable survivor presentation.

### Medium boundary

- Mechs, aliens, bosses, terrain, obstacles, mining sites, field pickups, and weapon objects are native 3D unless a specific effect reads better as a billboard or sprite.
- The HUD, menus, maps, fabrication interface, icons, and most informational overlays are 2D.
- VFX may combine meshes, particles, decals, trails, and camera-facing sprites. Their medium is selected for clarity and performance rather than uniformity.
- The choice does not introduce manual camera rotation, perspective aiming, combat zoom, or hidden information behind 3D geometry.

### Cohesion rules

- Art direction favors simple geometry, restrained materials, a controlled shared palette, strong silhouettes, and clear ground contact or shadows.
- Imported assets are adapted to common scale, material, palette, animation, and readability standards rather than used unchanged merely because they are available.
- Required gameplay state never depends on realistic lighting, subtle material response, texture detail, or color alone.
- Enemy identities must remain distinguishable at normal gameplay zoom amid survivor-scale hordes. Rank-and-file variants may reuse a model family through controlled changes, but the ten ordinary enemy identities retain distinct silhouettes.
- The asset pipeline is CC0-first. Every external asset retains a source-and-license ledger even when attribution is not required.

### Validation gates

Before the style is treated as production-proven, a representative prototype must demonstrate:

1. a credible route to six visually distinct playable mechs;
2. readable ordinary enemies, bosses, mining zones, pickups, and weapon effects at the fixed gameplay scale;
3. visual cohesion across the selected free-asset families after the shared adaptation pass;
4. acceptable peak-horde performance at both DEC-113 reference layouts, including first-class Steam Deck hardware; and
5. legible active play when effects, mining pressure, and dense enemies overlap.

Failure of a gate requires asset, rendering, or content-scope revision first. Reconsidering the medium remains possible if the prototype shows that native 3D cannot satisfy the accepted gameplay or hardware constraints.

## Status

Accepted as the production visual-medium target, subject to representative art and performance validation. Exact palette, lighting model, outline treatment, animation style, model budgets, effects budgets, and graphics settings remain open art-direction and technical targets.

## Rationale

The audited CC0 ecosystem offers unusually broad low-poly 3D coverage across animated mechs, animated monsters, modular weapons, sci-fi props, environments, and interface assets. Native 3D also supports continuous mech and weapon facing, arbitrary directional movement, model-part variation, recoloring, rescaling, and animation reuse without requiring separate sprite sets for every direction.

This choice prioritizes feasible whole-game asset coverage and adaptation flexibility. It does not assume that 3D is inherently more visually impressive than 2D; the intended result remains visually simple, highly readable, and close in informational character to a top-down 2D survivor game.

## Consequences

- The first composition prototype uses low-poly 3D rather than comparing equal-production 2D and 3D implementations.
- The orthographic camera and fixed world scale become art-production constraints.
- Asset evaluation must include animation and top-down silhouette quality, not just attractive preview renders.
- Art budgets include a unifying material and palette pass for mixed-source CC0 assets.
- Continuous facing and weapon rotation do not require multi-direction sprite sheets.
- Steam Deck performance must be measured with representative animated enemy counts, effects, mining presentation, and UI—not an empty environment.
- Bespoke 2D work remains appropriate for interface, icons, overlays, and effects where it offers clearer communication.

## Research basis

- [RES-005 — Free-asset strategy](../research/RES-005-free-asset-strategy.md)
- [Quaternius Animated Mech Pack](https://quaternius.com/packs/animatedmech.html)
- [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html)
- [Quaternius Sci-Fi Essentials Kit](https://quaternius.com/packs/scifiessentialskit.html)
- [Kenney asset catalog](https://kenney.nl/assets)

## Specification links

- [Game Vision](../00-game-vision.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Open Questions](../open-questions.md)
- [DEC-021 — Use a wide fully top-down camera](./DEC-021-wide-fully-top-down-camera.md)
- [DEC-113 — Target Windows PC and Steam Deck first](./DEC-113-target-windows-pc-and-steam-deck-first.md)

## Supersedes / superseded by

Resolves the 2D-versus-3D medium portion of OQ-011 and OQ-023. Exact art direction, asset adaptation, performance budgets, and presentation tuning remain open.
