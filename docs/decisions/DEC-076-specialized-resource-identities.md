---
doc_id: DEC-076
title: Give the Six Specialized Resources Strong Non-Exclusive Identities
status: accepted
authoritative: false
---

# DEC-076 — Give the Six Specialized Resources Strong Non-Exclusive Identities

## Decision

Assign the six specialized ordinary-resource codes these player-facing identities:

| Code | Name | Loose association | Primary recognition shape |
| --- | --- | --- | --- |
| `A` | Asterite | Precision, focus, stable fields, anchoring | Three-point prism |
| `B` | Barysteel | Mass, impact, armor, force redirection | Layered hexagonal slab |
| `C` | Cinderglass | Charge, conduction, propagation, controlled release | Fissured split diamond |
| `D` | Driftmetal | Direction, momentum, displacement, geometry | Paired compass fins |
| `E` | Eidolon Coral | Coordination, autonomous systems, cycling, reserves | Branching three-lobed node |
| `F` | Flux Amber | Multiplication, instability, conversion, unusual patterns | Spiral droplet |

The associations guide fiction, effects, and naming but are not exclusive mechanical categories. Exact recipes remain authoritative and visible. No material guarantees an effect family, damage type, weapon role, rarity, or power tier.

Each identity must be communicated redundantly through name, icon contour, deposit silhouette, surface or motion behavior, color, particles, and audio. Player-facing interfaces never rely on a bare color or internal letter code.

## Status

Accepted for the prototype content baseline. Presentation details may be refined while preserving the six identities and their non-color recognition channels.

## Rationale

The complete weapon graph deliberately gives every resource varied recipes and branches. A rigid semantic taxonomy would either misdescribe those relationships or pressure later content to become repetitive. Purely abstract colors would make geological exploration and mining feel like manipulating a spreadsheet.

Strong material fiction with soft associations provides memory, world flavor, and coherent effects without turning those associations into hidden recipe rules. Distinct silhouettes and sounds also make the system usable when colors are difficult to distinguish.

## Consequences

- Existing graph codes and weapon IDs remain stable for authoring.
- Surveys, deposits, maps, radar states, inventories, and recipes use the names and icons consistently.
- Resource VFX and later content may draw from each material's loose personality but must not imply an unsupported universal rule.
- Exact material quantities, specialized-node behavior, utilities, and rare cross-run resources remain separate decisions. DEC-109 and DEC-116 later resolve the initial utility structure and content.
- Resource identities can now inform biome art, fabrication UI, audio, and narrative work.

## Specification links

- [Specialized Resource Identities](../61-specialized-resource-identities.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Open Questions](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)

## Supersedes / superseded by

Resolves the player-facing identity portion of OQ-013. It does not itself resolve quantities, utility recipes, extraction behavior, fungibility, or rare cross-run resource content; later decisions resolve several of those areas, including utilities in DEC-109 and DEC-116.
