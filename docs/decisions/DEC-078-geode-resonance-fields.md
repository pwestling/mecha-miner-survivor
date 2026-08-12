---
doc_id: DEC-078
title: Give Material Geodes Thematic Enemy Resonance Fields
status: accepted
authoritative: false
---

# DEC-078 — Give Material Geodes Thematic Enemy Resonance Fields

> **Completion note:** [DEC-128](./DEC-128-set-extraction-zone-and-resonance-field-radii.md) fixes the 6.0M resonance-field radius left as tuning work here, twice the 3.0M extraction zone. The shared 20% value, effect communication, and practical equivalence remain tuning work.

## Decision

Every unopened material geode projects a visible resonance field larger than its extraction zone. Enemies within it receive a 20% material-specific modifier:

| Material | Enemy modifier |
| --- | --- |
| Asterite | 20% increased outgoing damage |
| Barysteel | 20% reduced incoming damage |
| Cinderglass | 20% increased projectile damage |
| Driftmetal | 20% reduced player-imposed displacement and control duration |
| Eidolon Coral | 20% increased attack cadence |
| Flux Amber | 20% increased movement speed |

The field is active before and during extraction and collapses when the geode opens. Effects depend on the enemy's physical presence in the field and do not persist after it leaves. Ordinary enemies, elites, and bosses are affected. Standard generation prevents resonance fields from overlapping.

Geode fields amplify nearby enemies but do not summon reinforcements or escalate at progress thresholds. Those behaviors remain exclusive to rare cross-run resource threat beacons.

## Status

Accepted as initial playtest behavior. The shared 20% value, field radius, effect communication, and practical equivalence remain tuning work.

## Rationale

Completion-only payout creates commitment; the resonance field makes each material create a distinct positional combat problem during that commitment. Local buffs preserve ordinary horde continuity and use the accepted material identities without adding six bespoke encounter scripts.

Preventing overlap keeps randomized placement from creating unplanned multiplicative difficulty spikes. Keeping geode fields distinct from reinforcement beacons also reserves escalating encounter pressure for the rarer cross-run reward.

## Consequences

- A geode's field and affected enemies require strong material-specific visual and audio feedback.
- Some fields vary with enemy composition or player build; equal percentage values are not assumed to produce equal difficulty.
- Field modifiers may be revised after testing without changing geode payout or resource identity.
- Enemy and boss specifications must state how attacks, projectiles, movement, damage reduction, displacement, and timed control interact with these fields.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)
- [DEC-032 — Escalate rare threat beacons at progress thresholds](./DEC-032-progress-threshold-threat-beacons.md)

## Supersedes / superseded by

Adds a non-escalating local danger rule to material geodes. It does not supersede the rare cross-run resource threat-beacon rules in DEC-032.
