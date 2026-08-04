---
doc_id: DEC-125
title: Adopt the Initial Numerical Weapon Catalog and Feasible Boss Hull
status: accepted
authoritative: false
validation: analytic-model-and-playtest
---

# DEC-125 — Adopt the Initial Numerical Weapon Catalog and Feasible Boss Hull

## Decision

Adopt the complete values in the [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md) as the authoritative first-playable baseline for all 15 weapons and 45 branches.

The catalog fixes:

- every weapon's rank-zero Damage, cadence, targeting, area, duration, capacity, and other delivery properties;
- exact additive increments for all 45 uncapped ore-stat tracks;
- exact multipliers, timing, caps, control values, child-object rules, and edge cases for all 45 mutually exclusive branches;
- rank-zero analytic single-target and favorable-horde estimates;
- a common relative spatial unit and simulation-time convention; and
- machine-readable base-weapon and branch mirrors for agent and tooling use.

The rank-zero catalog averages 31.7 ideal sustained single-target DPS. Individual weapons deliberately range from 18.0 to 45.0 DPS because area, control, range, reliability, setup, safety, and positional burden remain part of equal-tier balance.

Also replace the provisional boss Hull sequence of 6,000 / 24,000 / 75,000 / 220,000 with:

| Boss | Hull | Target defeat time | Required realized build DPS |
|---|---:|---:|---:|
| Riftjaw | 6,000 | 45–75 s | 80–133 |
| Brood Titan | 14,000 | 60–90 s | 156–233 |
| Prism Crown | 30,000 | 75–105 s | 286–400 |
| Skybreaker Apex | 45,000 | 90–120 s | 375–500 |

Keep their phase timing, spectacle, attacks, control pressure, adds, reward explosions, and overlapping persistence unchanged.

## Status

Accepted as the initial implementation and playtest baseline. Every value remains tunable after benchmark captures, but an agent should implement these values literally unless a later decision supersedes them.

## Rationale

The catalog applies the multi-metric framework without normalizing every weapon to identical dummy damage. High-safety trackers and exceptional radial coverage receive less raw focused output; short-range or positionally demanding weapons may receive more. Explicit caps and inheritance rules prevent branch-generated objects from causing accidental exponential scaling.

A legal no-relic Kestrel reference build under the Asterite / Barysteel / Cinderglass / Eidolon Coral resource profile produces approximately 81, 164, 328, and 391 realized boss DPS at the four milestones with plausible weapon depths and utility spending. The earlier late-boss Hull values required approximately 714–1,000 DPS at minute 21 and 1,833–2,444 at minute 28. Reaching those values would require either implausible within-run multiplication or base weapons that trivialize ordinary enemies.

Reducing boss Hull preserves the intended fight durations while allowing boss difficulty to escalate through behavior, adds, attack damage, arena denial, and persistent overlap rather than health inflation alone.

## Consequences

- The weapon catalog is numerically implementable without additional design judgment.
- All ore ranks are exact additive changes to their named stat and keep the accepted nonlinear shared-depth price curve.
- The base and branch CSVs may drive validation or content-pipeline work but never override the Markdown specification.
- Boss feasibility now has one legal fresh-account reference progression with resource, slot, utility, and ore constraints accounted for.
- Early playtests should change values using benchmark evidence, not by equalizing one dummy-DPS column.
- Balance captures should pay special attention to geometric branches whose favorable ceilings exceed normal branch targets.
- The late bosses may still feel much harder than early bosses through pressure and mechanics even though Hull now scales 1× / 2.33× / 5× / 7.5× rather than 1× / 4× / 12.5× / 36.7×.

## Specification links

- [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md)
- [Base weapon data](../data/weapon-base-balance.csv)
- [Branch data](../data/weapon-branch-balance.csv)
- [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md)

## Supersedes / superseded by

This supplies the numerical catalog deliberately left open by DEC-047, DEC-075, and DEC-124. It preserves the accepted weapon concepts, stat identities, resource assignments, branch classes, and branch fantasies from DEC-043 through DEC-075.

It supersedes only the boss Hull values and derived boss-DPS bands in DEC-112 and DEC-124. It does not supersede boss arrival times, fight-duration targets, persistence, attacks, rewards, or identities.
