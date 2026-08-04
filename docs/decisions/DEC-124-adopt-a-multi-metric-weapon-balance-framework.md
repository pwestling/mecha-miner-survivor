---
doc_id: DEC-124
title: Adopt a Multi-Metric Weapon Balance Framework
status: accepted
authoritative: false
validation: analytic-model-and-playtest
---

# DEC-124 — Adopt a Multi-Metric Weapon Balance Framework

> **Supersession note:** DEC-125 preserves this measurement framework but replaces the provisional four boss-DPS bands with 80–133, 156–233, 286–400, and 375–500 after completing the legal-build feasibility pass.

## Decision

Use the [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md) as the common basis for assigning and revising weapon, branch, enemy, boss, upgrade, and resource-economy values.

Single-target DPS is the primary arithmetic anchor but never the sole weapon score. Every weapon is also evaluated for horde throughput, realized boss reliability, coverage, overkill, setup, relocation recovery, control, safety, and positional burden across six canonical benchmark scenes.

The accepted initial anchors are:

- approximately 32 ideal single-target DPS averaged across all zero-rank base weapons, with archetype-specific bands;
- 80–133, 267–400, 714–1,000, and 1,833–2,444 realized whole-build boss DPS at the four boss milestones, derived from accepted Hull and time-to-kill targets;
- phase-specific whole-build defeat-rate bands rising from 1.5–2.5 enemies per second at minute zero to 14–22 at minute 34;
- ordinary stat ranks generally adding 8–12% of a zero-rank named base stat and producing 6–12% realized improvement in a relevant benchmark;
- branches producing roughly 35–70% favorable effectiveness improvement or comparable control and safety value; and
- no follow-on branch upgrades in the initial catalog.

These numbers are first-pass targets rather than promises that override playtest evidence. Their definitions and measurement context remain stable when a target is revised.

## Status

Accepted as the initial cross-catalog tuning method. It does not yet assign the complete numeric values for all 15 weapons.

## Rationale

Pure theoretical DPS rewards unlimited piercing and area on paper, ignores misses and setup, and undervalues control. Pure feel-based tuning makes it difficult for multiple agents to assign compatible numbers. The combined framework provides explicit arithmetic without collapsing distinct automatic-weapon fantasies into one score.

Deriving boss DPS from already accepted Hull and target fight durations makes those existing values useful rather than decorative. Standard benchmark scenes make later changes comparable and expose whether a stat improves the situation its label promises.

Removing initial follow-on branch upgrades keeps specialized-resource progression legible: each weapon makes one large irreversible transformation, while common ore supplies open-ended depth. Additional branch ranks can be reconsidered after the base 45 branch choices have real playtest evidence.

## Consequences

- Every numeric weapon specification must publish base values, fixed rank increments, branch values, and analytic or measured benchmark estimates.
- Equal tier means a useful portfolio across shared scenes, not identical DPS in every scene.
- Control and safety remain separate visible metrics rather than receiving a hidden universal damage conversion.
- Weapon tuning begins at a fresh neutral account and then validates traits, PowerUps, utilities, branches, relics, resource profiles, and full builds.
- A companion machine-readable balance table will mirror, but never replace, the authoritative Markdown values when the numeric weapon catalog is assigned.
- Every boss milestone requires a legal fresh-account no-relic reference build that can reach its lower DPS bound with plausible resources by that minute. The current late-boss Hull values are explicitly provisional because the feasibility table exposes steep required per-weapon growth.
- DEC-125 resolves OQ-025 by assigning the per-weapon numbers. Only the optional extreme-investment Easter egg remains deferred under OQ-034.

## Specification links

- [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](../weapons/README.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

This supplies the shared numeric methodology left open by DEC-047, DEC-075, DEC-084, DEC-085, DEC-112, DEC-119, and the individual weapon decisions. It preserves their accepted weapon behaviors, three-stat bundles, price curve, enemy identities, boss targets, and progression envelope.
