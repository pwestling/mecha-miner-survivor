---
doc_id: DEC-109
title: Use Single-Material Utilities with Three Ore Ranks
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-109 — Use Single-Material Utilities with Three Ore Ranks

## Decision

The content-complete initial catalog contains **twelve non-radar utilities**, with exactly two assigned to each specialized material.

- Each non-radar utility has one fixed single-material recipe costing one unit of its assigned material.
- A four-material resource profile therefore makes exactly eight non-radar utilities craftable.
- The common-ore resource radar remains universally offered outside this material catalog, bringing the normal run's available utility choices to nine.
- A first playable may implement six non-radar utilities, one assigned to each material. A four-material profile then offers four of those plus the radar.

Utilities are passive or automatic support systems whose primary purposes may include navigation, mining, defense, mobility, economy, recovery, or weapon support. They add no manual gameplay input. A system primarily intended to deal sustained automatic damage remains a weapon.

### Utility upgrades

Every installed non-radar utility has exactly three run-local common-ore upgrade ranks. Its blueprint defines one fixed, visible base effect and the improvement produced by each rank. Ranks normally strengthen one named magnitude by a fixed increment; a utility whose effect cannot be expressed cleanly as a magnitude may define three predetermined discrete improvements.

| Rank purchased | Cost | Cumulative utility investment |
| ---: | ---: | ---: |
| 1 | 50 ore | 50 ore |
| 2 | 100 ore | 150 ore |
| 3 | 150 ore | 300 ore |

Each utility tracks its ranks independently. Utility ranks do not affect weapon upgrade depth or another utility's price. They consume no additional slot and are lost with the utility at run end.

The resource radar has no upgrade ranks in the initial rules because its 300-ore installation already provides its complete binary navigation function. A later radar progression model requires an explicit decision.

## Status

Accepted as the initial utility availability, recipe, control, and upgrade structure. DEC-116 later accepts the twelve utility concepts, material assignments, base effects, and rank improvements; numeric effects and the 50/100/150 prices require playtesting.

## Rationale

Under the earlier two-alternative recipe proposal, any given utility would be available in fourteen of the fifteen possible four-material profiles. That would barely constrain a favorite utility trio and would weaken the geology-driven build variation used for weapons.

One material per utility makes each utility available in ten of fifteen profiles, or two thirds of runs. Assigning two utilities to every material guarantees that every profile offers exactly eight of the twelve without making a utility an appendage of one two-material weapon recipe.

Three capped ore ranks give utilities their own in-run development cadence while keeping them structurally simpler than weapons. A fully upgraded utility costs 300 ore—the value of three standard seams, one standard plus one rich seam, or the radar itself—so supporting a favorite utility competes meaningfully with weapon development and navigation certainty.

## Consequences

- Resource profiles now vary both weapon and utility availability in predictable amounts.
- Utility concepts should be broadly useful rather than depending on the five weapons incident to their assigned material.
- The fabrication interface shows all twelve blueprints, marks four as unavailable under the current geology, and shows the eight valid material recipes.
- An installed utility displays its current rank, next effect, and next ore price.
- The three-slot commitment remains irreversible even though ranks can be purchased later.
- A six-utility prototype tests the recipe structure but supplies less choice than the twelve-utility target.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Open Questions](../open-questions.md)

## Supersedes / superseded by

Supersedes the leading two-alternative recipe model in [DEC-035](./DEC-035-integrate-utilities-without-fixed-weapon-pairing.md) while preserving its goal of avoiding one fixed weapon-pair association. Completes utility upgrades left open by [DEC-018](./DEC-018-four-weapons-three-utilities.md) and [DEC-100](./DEC-100-commit-installed-weapons-and-utilities.md).

[DEC-116](./DEC-116-accept-initial-utility-catalog.md) subsequently completes the initial utility content and assignments without changing this structure.
