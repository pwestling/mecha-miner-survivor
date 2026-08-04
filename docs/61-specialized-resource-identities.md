---
doc_id: GDD-SPECIALIZED-RESOURCE-IDENTITIES
title: Specialized Resource Identities
status: active
authoritative: true
---

# Specialized Resource Identities

## Purpose and player promise

The six specialized ordinary resources turn a run's abstract four-of-six recipe profile into a readable piece of alien-world geology. Each material has a memorable name, silhouette, surface treatment, color, icon, motion cue, and sound. A player should be able to recognize a deposit in the world, find the same material in the geological survey, and trace it into fabrication recipes without relying on color alone.

The identities create fiction and loose associations rather than six rigid equipment schools. The fabrication interface always states exact material requirements. A player is never expected to infer a recipe solely because an effect “looks like” one material.

## Accepted identity map

The letter codes remain stable authoring shorthand and preserve the accepted weapon-graph IDs. Player-facing interfaces use the material names and icons, never a bare letter code.

| Code | Player-facing name | Material character | Loose association | Primary color | Icon and silhouette cue | Audio character |
| --- | --- | --- | --- | --- | --- | --- |
| `A` | **Asterite** | Clean, faceted field-aligning crystal | Precision, focus, stable fields, anchoring | Saturated cyan | Three-point prism | Clear rising chime |
| `B` | **Barysteel** | Extremely dense layered alien metal | Mass, impact, armor, force redirection | Muted gold | Squared hexagonal slab | Low metallic strike |
| `C` | **Cinderglass** | Dark glass with energetic internal fractures | Charge, conduction, propagation, controlled release | Deep violet | Split diamond with a lightning fissure | Brittle crack followed by an electric snap |
| `D` | **Driftmetal** | Polarized metallic fins that continually realign | Direction, momentum, displacement, geometry | Vermilion red | Paired chevrons or compass fins | Directional whir with a Doppler shift |
| `E` | **Eidolon Coral** | Branching techno-organic conductive growth | Coordination, autonomous systems, cycling, reserves | Acid green | Branching three-lobed node | Layered harmonic pulse |
| `F` | **Flux Amber** | Translucent metastable resin containing moving inclusions | Multiplication, instability, conversion, unusual patterns | Hot magenta with a warm inner core | Spiral droplet | Unsteady two-tone warble |

These colors are presentation defaults, not the resource identities themselves. Lighting, biome palettes, and visual effects may shift their exact rendered values while preserving their other recognition channels.

## Asterite (`A`)

Asterite grows as tall clusters of clean triangular prisms. Its facets align local energy fields, causing nearby particles and the active extraction effect to resolve into orderly parallel lines. Unmined clusters periodically catch the light with a cyan sweep even when the environment is dim.

Its loose personality is exactness and stability: focused beams, coherent fields, anchored structures, and attacks whose effectiveness depends on alignment. This association helps the setting feel consistent, but Asterite is not a universal “precision stat” and does not guarantee a particular targeting rule.

World deposits use an unmistakable three-spire silhouette. The map and fabrication icon is a three-point prism. Collection feedback uses a clean rising chime and narrow cyan particle streaks.

## Barysteel (`B`)

Barysteel occurs as squat, layered metallic masses that appear too heavy for the surrounding geology. Exposed planes have a muted gold sheen over dark edges, and impacts produce a momentary compression ripple rather than loose sparks.

Its loose personality is mass and redirected force: penetration, impact, protection, fortification, and mechanisms that turn one collision into another. The material does not promise that every Barysteel recipe is defensive or kinetic.

World deposits use broad stacked slabs with an asymmetric hexagonal outline. Its icon is a squared hexagonal ingot divided into weight-like layers. Collection feedback uses a low resonant strike and short, heavy particles that fall rather than float.

## Cinderglass (`C`)

Cinderglass forms jagged translucent-black plates. Deep-violet energy crawls through internal fractures, briefly illuminating a different path on every pulse. Mining peels away thin glass sheets before they dissolve into energized fragments.

Its loose personality is transfer and release: chaining, cascading, building charge, carrying effects through a medium, and focusing distributed energy into a payoff. It is not an elemental damage type and does not automatically make a weapon electrical.

World deposits use a fan of sharp overlapping plates. Its icon is a split diamond crossed by one lightning-like fissure. Collection feedback combines a brittle glass note with a delayed electric snap and angular violet fragments.

## Driftmetal (`D`)

Driftmetal grows in long polarized fins. Individual blades slowly realign as the mech circles them, as though following a magnetic direction that does not match the planet's compass. Red faces alternate with cool metallic edges to preserve their form under varied lighting.

Its loose personality is vector control: direction, momentum, orbit, knockback, paths, containment geometry, and attacks transformed by where the player moves. Driftmetal is not exclusively a mobility or crowd-control material.

World deposits use paired leaning fins that form a large chevron from above. Its icon is a pair of opposing compass chevrons. Collection feedback uses a panning mechanical whir, a brief Doppler pitch change, and thin vermilion slivers that rotate into alignment before entering the inventory counter.

## Eidolon Coral (`E`)

Eidolon Coral is a techno-organic mineral lattice rather than a conventional plant. It forms branching conductive arms around a dense mineral core and pulses in coordinated waves. Separated branches briefly continue pulsing in synchronization before becoming inert crafting material.

Its loose personality is coordination and sustained systems: autonomous agents, linked emplacements, repeating cycles, stored reserves, and several components behaving as one network. It does not imply that every associated weapon is biological, intelligent, or summon-based.

World deposits use a rounded three-lobed branching silhouette with visible negative space between arms. Its icon is a Y-shaped node with connected tips. Collection feedback uses a layered harmonic pulse and acid-green motes that travel along branching paths rather than flying directly inward.

## Flux Amber (`F`)

Flux Amber forms bulbous translucent nodules around luminous inclusions that never remain still. The outer material reads as hot magenta while its suspended core shifts through warmer tones. Its surface subtly expands and contracts out of rhythm with nearby nodes.

Its loose personality is unstable possibility: splitting, replication, delayed transformation, dramatic conversion, and spatial patterns that depart from a weapon's ordinary behavior. Flux Amber is not automatically rarer, stronger, or more dangerous than the other five specialized materials.

World deposits use clustered droplets around one larger spiral-cored bulb. Its icon is a spiral inside a teardrop. Collection feedback uses an intentionally unstable two-tone warble and curved motes that briefly duplicate before recombining at the inventory counter.

## Shared player-facing rules

- The geological survey presents each available material with its name, icon, visual sample, detected geode count, and corresponding abundance label.
- World labels, map markers, radar targeting, inventory counts, recipe costs, and branch costs repeat the same name-and-icon pairing.
- A bare color swatch or bare `A`–`F` code is never the only player-facing identifier.
- Each specialized-material direction among the resource radar's simultaneous screen-edge indicators uses the tracked material's icon and preserves its name or abbreviation in the active HUD where space permits. The separately tracked standard seam, rich seam, and Hyper Gold site use distinct class icons and labels rather than borrowing a specialized material identity.
- Recipe cards show exact required materials and amounts. Loose material associations never substitute for recipe disclosure.
- When a material is absent from the run profile, fabrication still shows its icon and name on impossible recipes, together with an explicit absent-from-geology state.
- Specialized materials share the same economy tier. Their identity does not establish a global rarity or power hierarchy.
- The names are mass nouns in counters: for example, `12 Asterite`, not `12 Asterites`.

## Geode resonance behavior

Each unopened material geode projects a larger resonance field around its smaller extraction zone. Enemies inside gain the material's accepted 20% modifier until they leave or the geode opens:

| Material | Resonance modifier |
| --- | --- |
| Asterite | Enemy outgoing damage +20% |
| Barysteel | Enemy incoming damage −20% |
| Cinderglass | Enemy projectile damage +20% |
| Driftmetal | Player-imposed displacement and control duration −20% |
| Eidolon Coral | Enemy attack cadence +20% |
| Flux Amber | Enemy movement speed +20% |

Field boundaries and affected-enemy treatments reuse the material's established shape, motion, color, and audio language. The modifier must also be named in the geode label or contextual HUD so the player does not have to infer mechanics from effects. See [Mining and Extraction](./40-mining-and-extraction.md#geode-resonance-fields).

## Accessibility and recognition standard

Every material must remain distinguishable when color information is missing or altered. Its deposit, icon, particle path, and core audio cadence provide redundant cues:

| Material | Static shape | Surface or motion | Sound |
| --- | --- | --- | --- |
| Asterite | Triangular spires | Ordered light sweep | Single rising chime |
| Barysteel | Stacked heavy slabs | Compression ripple | Low strike |
| Cinderglass | Jagged plate fan | Traveling internal fissures | Crack then snap |
| Driftmetal | Leaning compass fins | Slow realignment | Panning whir |
| Eidolon Coral | Branching lobes | Synchronized pulses | Layered harmonic beat |
| Flux Amber | Rounded spiral bulbs | Asynchronous inclusions | Two-tone warble |

Map icons must retain different outer contours at the smallest supported minimap size. Text labels and an optional icon legend remain available. Any color-vision setting may alter hues without changing the canonical shape assignments.

## Boundaries and remaining questions

These identities do not yet decide:

- fungibility, conversion, carrying limits, or abundance weights;
- whether biomes affect material presentation or yield;
- Hyper Gold's appearance and audio identity; its initial cross-run purchases and prices are fixed in the [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md) and [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md).

Those questions remain in [OQ-004](./open-questions.md#oq-004--how-does-a-mining-point-behave) and [OQ-013](./open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase). Utility material assignments are now fixed by the [Utility Catalog](./68-utility-catalog.md).

## Related documents

- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](./weapons/README.md)
- [Utility Catalog](./68-utility-catalog.md)
- [DEC-076 — Give the six specialized resources strong non-exclusive identities](./decisions/DEC-076-specialized-resource-identities.md)
- [DEC-077 — Use ore seams and completion-only material geodes](./decisions/DEC-077-ore-seams-and-material-geodes.md)
- [DEC-078 — Give material geodes thematic enemy resonance fields](./decisions/DEC-078-geode-resonance-fields.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
