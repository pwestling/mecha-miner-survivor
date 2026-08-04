---
doc_id: RES-005
title: Free-Asset Strategy
status: complete
authoritative: false
---

# RES-005 — Free-Asset Strategy

## Research question

What freely available asset ecosystems could support a cohesive top-down mech-versus-alien game, and what licensing constraints should influence the eventual 2D-versus-3D visual-medium choice?

## Retrieval date

Initially retrieved 2026-08-02; platform and display references rechecked 2026-08-03.

## Sources

- [Kenney support and licensing](https://kenney.nl/support) — official statement that assets on Kenney asset pages are CC0, usable commercially, and do not require attribution.
- [Kenney asset catalog](https://kenney.nl/assets) — official catalog covering 2D, 3D, UI, audio, pixel assets, and textures.
- [Quaternius FAQ](https://quaternius.com/faq.html) — official statement that Quaternius models are CC0, commercially usable, modifiable, and available without required attribution.
- [Quaternius asset catalog](https://quaternius.com/) — official catalog of free low-poly 3D game assets.
- [Quaternius Animated Mech Pack](https://quaternius.com/packs/animatedmech.html) — four animated CC0 mech models.
- [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) — fifty animated CC0 monster models.
- [Quaternius Sci-Fi Essentials Kit](https://quaternius.com/packs/scifiessentialskit.html) — sixty-five CC0 sci-fi models including robot enemies, weapons, screens, and crates.
- [Quaternius Sci-Fi Modular Gun Pack](https://quaternius.com/packs/scifimodularguns.html) — seventy-eight CC0 modular weapon models.
- [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) — 330 CC0 environment models.
- [Kenney Space Kit](https://kenney.nl/assets/space-kit) — 150 CC0 space-themed models.
- [Kenney Space Station Kit](https://kenney.nl/assets/space-station-kit) — ninety CC0 station-themed models.
- [Kenney Modular Space Kit](https://kenney.nl/assets/modular-space-kit) — forty CC0 modular space-interior models.
- [Kenney Sci-Fi UI](https://kenney.nl/assets/ui-pack-sci-fi) — 130 CC0 interface assets.
- [OpenGameArt FAQ](https://opengameart.org/node/5571) — official explanation that assets use multiple free licenses and that compatibility and attribution obligations depend on the selected asset's license.
- [Itch.io 2D asset collection licensing note](https://itch.io/c/6403322/game-assets-2d) — curator warning that many free itch.io assets do not clearly state licenses and should be verified before commercial use.
- [Steam Hardware & Software Survey](https://store.steampowered.com/hwsurvey/) — official current survey used to check operating-system share and common desktop resolutions.
- [Steam Deck and Steam Machine Compatibility Review](https://partner.steamgames.com/doc/steamhardware/compat?l=french&language=english) — official controller, display-resolution, text-legibility, and performance checklist.

## Relevant findings

### CC0-first sources reduce legal and production friction

Kenney states that assets on its asset pages are CC0 and offers categories spanning 2D, 3D, UI, audio, pixel assets, and textures. Quaternius states that its 3D models are CC0, can be modified or combined, and can be used commercially without attribution. These two sources are strong candidates for a low-friction prototype and may support complementary interface, environment, prop, character, or effects needs.

### “Free” does not imply one consistent license

OpenGameArt accepts multiple licenses, some of which require attribution or raise compatibility questions depending on how the game is distributed. Free itch.io listings likewise require item-by-item license verification. Asset price and asset license must therefore be tracked separately.

### Asset-pack breadth matters more than isolated quality

A survivor-like needs many coherent enemies, readable player units, terrain elements, deposits, projectiles, effects, icons, and interface elements. Choosing a style around one appealing mech model or tileset can create a content bottleneck if the same ecosystem cannot cover the rest of the game.

### The top-down decision does not force 2D or 3D

A fully top-down wide camera can use native 2D assets, prerendered sprites, or 3D models. The best medium should be selected after testing complete candidate asset families at gameplay zoom, including animation and horde readability, rather than assuming pixel art is the only readily available option.

### The audited low-poly 3D ecosystem has promising whole-game coverage

The current CC0 shortlist contains four animated mech models, fifty animated monsters, sixty-five general sci-fi models, seventy-eight modular weapon models, several hundred environment and space-station models, and a 130-piece sci-fi interface set. This is enough breadth to make native low-poly 3D the leading medium for a composition prototype rather than merely a theoretical option.

Native 3D would also let directional weapons and mech facing rotate continuously without requiring a separate sprite for every direction. Models can be recolored, rescaled, and combined to produce the game's rank-and-file variants while retaining the accepted six-silhouette-plus-variant enemy structure.

This is not yet proof of complete coverage. The dedicated mech pack contains four models while the initial roster requires six, and assets from different creators may not share scale, materials, or proportions. Mining landmarks, projectile effects, branch-specific weapon forms, pickups, and the remaining mech silhouettes must still be constructed, adapted, or sourced. The audit also does not establish that hundreds of animated enemies will meet the eventual Steam Deck performance budget.

### The accepted platform target creates two mandatory composition tests

DEC-113 targets Windows Steam and treats Steam Deck as first-class. The Steam survey supports 1920×1080 as the desktop reference, while Valve recommends 1280×800 for Steam Deck and requires complete default-controller access and handheld text legibility. Every asset-medium comparison must therefore test actual horde, mining, HUD, fabrication, and map compositions at both reference canvases. A style that reads well only on a desktop monitor is not sufficient.

## Recommended evaluation process

The medium recommendation has been accepted by DEC-114; the remaining steps validate and operationalize it:

1. Define a minimum asset coverage matrix for mechs, alien archetypes, terrain, mining points, projectiles, VFX, pickups, UI, and audio.
2. Shortlist CC0-first asset families and record source, author, license, modification permission, and attribution requirement for every pack.
3. Build representative top-down composition tests at the intended wide zoom.
4. Evaluate silhouette readability, animation coverage, palette cohesion, scale compatibility, and the effort required to adapt missing categories.
5. Prototype native low-poly 3D with a fixed orthographic top-down camera and 2D HUD, because it has the strongest audited CC0 coverage.
6. Validate the accepted production medium through adequate whole-game coverage and acceptable Steam Deck performance; revise the asset plan or rendering approach if a gate fails.

## Risks

- Mixing individually attractive packs can produce inconsistent scale, palette, outlines, lighting, or animation quality.
- A 3D catalog may have broad props but insufficient mech or alien animation coverage.
- A 2D catalog may fit top-down readability but lock the game to one projection or provide too few directional animations.
- License obligations can become difficult to satisfy if provenance is not recorded when an asset enters the project.
- Asset availability can shape the roster or enemy taxonomy in ways that should be made explicit rather than silently dictating design.

## Resulting links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [DEC-021 — Use a wide fully top-down camera](../decisions/DEC-021-wide-fully-top-down-camera.md)
- [DEC-113 — Target Windows PC and Steam Deck first](../decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
- [DEC-114 — Use native low-poly 3D gameplay](../decisions/DEC-114-use-native-low-poly-3d-gameplay.md)
- [OQ-011 — What is the intended platform and presentation format?](../open-questions.md#oq-011--what-is-the-intended-platform-and-presentation-format)
- [OQ-023 — Which asset medium and visual style best fit the free-asset constraint?](../open-questions.md#oq-023--which-asset-medium-and-visual-style-best-fit-the-free-asset-constraint)
