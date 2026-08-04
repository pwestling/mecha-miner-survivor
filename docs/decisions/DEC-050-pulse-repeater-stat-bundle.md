---
doc_id: DEC-050
title: Give Pulse Repeater Damage Rate and Range Stats
status: accepted
authoritative: false
---

# DEC-050 — Give Pulse Repeater Damage, Rate, and Range Stats

## Decision

Pulse Repeater exposes exactly three common-ore-upgradeable stats:

- **Damage:** damage dealt by each pulse.
- **Attack rate:** automatic pulse firing events per unit of time.
- **Range:** enemy acquisition distance for the base weapon, Zero-Lag Emitter, and Suppressive Sequencer; lateral pulse travel distance for Broadside Oscillator.

Pulse projectile speed and outward impact force are fixed weapon properties rather than ore-upgradeable stats. Zero-Lag Emitter replaces finite projectile travel with an instant hit as its branch benefit. Suppressive Sequencer's slow is also branch behavior rather than an additional stat track.

## Status

Accepted stat bundle; increments, prices, and numeric tuning open.

## Context

The weapon had four candidate stats after the catalog adopted a default maximum of three. Its final bundle needs to remain legible and useful across automatic nearest-target fire, instant delivery, distributed suppression, and lateral broadside fire.

## Considered options

### Upgrade impact force instead of range

This emphasizes crowd displacement, but it removes a clear reach investment and becomes less predictable when firing toward both sides.

### Upgrade damage, attack rate, and range

These stats express the rapid-repeater fantasy, offer straightforward independent investments, and carry cleanly into every branch.

## Rationale

Damage and attack rate are the weapon's core throughput axes. Range creates a positioning choice in every form: automatic target reach in its targeted forms and broadside reach in its conversion. Keeping impact force fixed prevents a fourth track and avoids making increasingly strong displacement mandatory to improve the weapon.

## Consequences

- Attack-rate ranks increase firing-event cadence in all three branches.
- Broadside Oscillator applies one firing event to its paired lateral output; exact per-event pulse count remains branch tuning.
- Range changes meaning between targeted and broadside forms, and the fabrication preview must disclose that mapping before branch commitment.
- Impact force remains small and fixed unless a later branch or global effect explicitly modifies it.
- Projectile speed remains fixed and finite except when Zero-Lag Emitter replaces travel with an instant hit.
- Exact linear per-rank gains, units, and combat-value rounding remain open; DEC-085 fixes the shared weapon-depth price curve.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)

## Supersedes / superseded by

Resolves Pulse Repeater's four-stat candidate list under [DEC-047](./DEC-047-three-stat-weapon-bundles.md). It does not set numeric tuning.
