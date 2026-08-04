---
doc_id: DEC-073
title: Complete the Ram Field Weapon
status: accepted
authoritative: false
---

# DEC-073 — Complete the Ram Field Weapon

## Decision

Ram Field (`D + F`, off-color `A`) is accepted with the following complete high-level design.

### Base behavior

While the mech moves, a short energy ram remains active immediately in front of it along persistent facing. An enemy entering the ram takes damage and is knocked away from the mech. Each enemy has a brief contact cooldown before the base field can damage it again. The field becomes inactive while the mech is stationary.

### Common-ore stats

- **Damage:** damage per valid ram impact.
- **Ram width:** lateral size of the forward field.
- **Knockback distance:** displacement caused by a valid impact.

Forward reach, contact cooldown, activation speed threshold, and the mech's own movement speed are fixed properties.

### Branches

- **`D` amplification — Momentum Cascade:** Uninterrupted movement and successful impacts build momentum stacks that increase damage and ram width up to a fixed cap. Momentum decays rapidly while stationary.
- **`F` functional variant — Impact Transfer:** Rammed ordinary enemies become ballistic. If a launched enemy collides with another enemy, both take derived damage and knockback; remaining momentum may allow a fixed number of further collisions. Resistant elites and bosses still take impact damage even when they cannot be launched normally.
- **`A` playstyle conversion — Siege Anchor:** The moving forward ram is removed. After the mech remains stationary for a setup delay, a circular barrier arms around it. Enemies crossing the barrier take damage and are knocked outward. Moving collapses the barrier and requires a new setup period, creating a hold-ground weapon for mining stands.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base rewards charging through gaps, Momentum Cascade amplifies sustained aggression, Impact Transfer turns enemies into kinetic projectiles, and Siege Anchor reverses the movement requirement to support deliberate territorial defense.

## Consequences

- The ram is front-facing rather than a full contact aura.
- Momentum state and Siege Anchor setup must be clearly telegraphed.
- Siege Anchor maps ram width to barrier radius and uses the same damage and knockback stats.
- Exact activation threshold, momentum gain and decay, ballistic collision count, boss resistance, setup delay, barrier hit cooldown, and map-edge behavior remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Ram Field concept and locks `D` amplification, `F` functional, and `A` conversion funding. Exact values remain subject to playtesting.
