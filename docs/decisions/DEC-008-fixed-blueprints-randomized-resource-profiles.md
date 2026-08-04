---
doc_id: DEC-008
title: Use Fixed Fabrication Rules with Surveyed Randomized Resource Profiles
status: accepted
authoritative: false
---

# DEC-008 — Use Fixed Fabrication Rules with Surveyed Randomized Resource Profiles

## Decision

Unlocked blueprints, recipes, effects, and resource prices remain fixed. Each level randomizes which specialized ordinary resource types are present and their broad abundance. After deployment, the map's geological survey reveals every present specialized resource type and a qualitative abundance band for each, while exact deposit positions, counts, and yields remain hidden until exploration. [DEC-015](./DEC-015-in-run-opening-geological-survey.md) establishes this in-run reveal timing.

Opening or reopening the fabrication menu does not change the resource profile or any fabrication rule.

## Status

Accepted.

## Context

The game needs enough uncertainty to prevent every run from collapsing into the same favorite or solved loadout. It also promises more intentional build control than randomized XP choices or treasure-chest rewards. Unlimited fabrication access makes any menu randomization that occurs on opening especially vulnerable to reroll fishing.

## Considered options

### Fully fixed catalog and resource availability

This offers maximum planning certainty but makes repeated optimal routes and builds likely after the game is understood.

### Random fabrication offers

This produces immediate build variety, but obscures intentional crafting and either enables fishing or requires extra reroll restrictions.

### Fixed run-specific blueprint manifest

This can strongly vary builds without rerolling, but adds a second availability layer before testing whether the game's distinctive resource ecology creates enough variation by itself.

### Fixed fabrication rules with randomized surveyed geology

This makes the economic viability of builds vary while keeping every known recipe dependable and understandable.

## Rationale

The decision locates randomness in the part of the game that is meant to be distinctive: exploration and mining. Revealing the broad resource profile lets the player form an informed early-run build plan, while hiding deposits preserves spatial exploration and adaptation. Stable recipes maintain crafting agency, and a fabrication menu that never rerolls remains compatible with unlimited on-demand access.

## Consequences

- Opening map information must quickly connect advertised resources to known recipes while minor enemies are active.
- Abundance bands need stable player-facing meanings even though they do not expose exact totals.
- Every generated profile must support viable routes to run power for every playable mech; resource scarcity can constrain a build without making the selected mech or level nonfunctional.
- Specialized resources should serve overlapping recipe possibilities so one profile does not dictate one obvious weapon set.
- Exact deposits and yields remain exploration discoveries.
- The profile remains fixed throughout active play and cannot be changed through fabrication access.
- Whether restarting after the opening reveal generates a new profile remains open and must be resolved without creating a fishing exploit.
- If resource ecology alone produces repetitive builds, fixed run manifests or stable major-upgrade drafts may be tested as later layers rather than silently added to this rule.
- DEC-034 later makes specialized-resource presence a hard feasibility gate for base-weapon recipes while keeping every recipe fixed. DEC-036 fixes the model at four of six resource families, supporting exactly six of 15 normal pair-weapon recipes.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-018 — How does each run randomize build availability without enabling fishing?](../open-questions.md#oq-018--how-does-each-run-randomize-build-availability-without-enabling-fishing)
- [RES-004 — Run randomization and build agency](../research/RES-004-run-randomization-and-build-agency.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](./DEC-034-gate-base-weapons-by-resource-profile.md)
- [RES-006 — Resource-color graph for weapon availability](../research/RES-006-resource-color-weapon-graph.md)

## Supersedes / superseded by

No earlier accepted decision is superseded. This accepts the leading model developed in RES-004 and leaves its additional randomization layers as playtest fallbacks. [DEC-015](./DEC-015-in-run-opening-geological-survey.md) supersedes only this decision's original pre-deployment reveal timing. [DEC-034](./DEC-034-gate-base-weapons-by-resource-profile.md) later makes geology a hard base-weapon feasibility gate rather than merely an economic influence, and [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md) resolves its numeric structure.
