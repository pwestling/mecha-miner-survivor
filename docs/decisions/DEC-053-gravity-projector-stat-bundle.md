---
doc_id: DEC-053
title: Give Gravity Projector Damage Radius and Duration Stats
status: accepted
authoritative: false
---

# DEC-053 — Give Gravity Projector Damage, Radius, and Duration Stats

## Decision

Gravity Projector exposes exactly three common-ore-upgradeable stats:

- **Damage:** damage dealt by its gravity effects.
- **Field radius:** the area affected by each gravity field or singularity.
- **Field duration:** how long its relevant gravity effect remains active.

Deployment cadence and field placement range are fixed weapon properties rather than ore-upgradeable stats. Pull behavior is expressed through field duration rather than a separate pull-force track.

In the accepted branches:

- Echo Well applies the upgraded damage, radius, and duration to both the initial field and its delayed echo.
- Gravity Slingshot applies duration to its inward gathering phase before the launch burst; exact scheduling of the launch within the weapon cycle remains open.
- Singularity Forge keeps its slow firing cadence fixed. Duration extends the devastating localized singularity at the impact position rather than increasing shot frequency.

## Status

Accepted stat bundle and branch mappings; increments, prices, scheduling edge rules, and numeric tuning open.

## Context

Gravity Projector had four candidate stats under the three-stat limit. Pull force was rejected as insufficiently legible: it would describe how quickly enemies accelerate toward a field center, but players would have difficulty comparing its practical value. Duration produces a clearer visible effect.

## Considered options

### Upgrade pull force

Increase inward acceleration or displacement speed. This is mechanically valid but subtle, enemy-mass-dependent, and difficult to communicate in the fabrication menu.

### Upgrade field duration

Extend the time during which the weapon's gravity effect acts. This has an obvious visual and tactical result and can be reinterpreted cleanly for the impact phase of Singularity Forge.

## Rationale

Damage, radius, and duration describe how severe, broad, and persistent a field is. They create three readable investment axes without weakening the weapon's fixed cadence or allowing Singularity Forge's intended rare payoff to become routine.

## Consequences

- Increasing duration must never increase Singularity Forge's firing frequency.
- Gravity Slingshot's UI must disclose how duration affects the timing of its launch phase.
- Enemy displacement behavior remains fixed except for the longer period during which applicable pull effects act.
- Boss displacement resistance may reduce movement without reducing the displayed duration of damage or other field effects.
- Exact linear per-rank gains, units, and combat-value rounding remain open; DEC-085 fixes the shared weapon-depth price curve.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Resolves Gravity Projector's candidate stat bundle under [DEC-047](./DEC-047-three-stat-weapon-bundles.md). It does not set numeric tuning.
