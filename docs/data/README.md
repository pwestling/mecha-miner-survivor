---
doc_id: GDD-MACHINE-READABLE-DATA-INDEX
title: Machine-Readable Data Index
status: active
authoritative: false
---

# Machine-Readable Data Index

These files mirror selected gameplay values for validation, comparison, and future content tooling. They are intentionally subordinate to the linked authoritative Markdown specification: when values disagree, update the data mirror to match the Markdown rather than silently treating the data file as a new design decision.

## Weapon data

- [weapon-base-balance.csv](weapon-base-balance.csv) — one row for each of the 15 base weapons, including three rank-zero stats, exact additive rank increments, activation model, analytic damage estimates, and fixed delivery properties.
- [weapon-branch-balance.csv](weapon-branch-balance.csv) — one row for each of the 45 branches, including transformation class, exact specialized-material price, condensed mechanical parameters, favorable effect, and primary tradeoff.
- [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md) — authoritative definitions, full edge rules, global Attack Rate mapping, reference-build arithmetic, and playtest capture requirements.

## Survivability data

- [survivability-baseline.csv](survivability-baseline.csv) — shared player, damage, recovery, rock, control, and cohort-level difficulty values.
- [contact-damage-pressure.csv](contact-damage-pressure.csv) — all ten ordinary enemies and four bosses converted into exact movement, footprint, contact-damage, hits-to-defeat, time-to-defeat, and control-resistance values.
- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md) — authoritative movement, collision, damage-resolution, healing, control, and failure-margin rules.

## Update rule

Any accepted numeric catalog revision should update, in the same change:

1. the authoritative gameplay section;
2. every affected analytic estimate, reference-build result, damage margin, and cohort target;
3. the corresponding CSV row;
4. any boss or economy value whose feasibility changes; and
5. the decision log if the revision is consequential rather than ordinary playtest tuning.

Do not add engine class names, serialized implementation fields, internal asset identifiers, or other technical schema to these gameplay mirrors. Those belong to the later implementation specification.
