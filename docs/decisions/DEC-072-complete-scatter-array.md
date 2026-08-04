---
doc_id: DEC-072
title: Complete the Scatter Array Weapon
status: accepted
authoritative: false
---

# DEC-072 — Complete the Scatter Array Weapon

## Decision

Scatter Array (`D + E`, off-color `C`) is accepted with the following complete high-level design.

### Base behavior

At each firing event, the array launches a fixed number of fast projectiles evenly across a fixed short-range cone centered on persistent facing. Each projectile travels straight, damages the first enemy it contacts, and then disappears. Large enemies may intersect more than one projectile when geometry permits.

### Common-ore stats

- **Damage:** damage per projectile.
- **Attack rate:** firing events per unit of time.
- **Range:** projectile travel distance.

Projectile count, cone angle, projectile size, and projectile speed are fixed properties.

### Branches

- **`D` amplification — Saturation Choke:** Each firing event becomes a continuous damaging wave filling the entire cone rather than separated projectiles. Every enemy intersecting the cone is hit once, eliminating gaps and first-target blocking while preserving facing, range, and cadence.
- **`E` functional variant — Concussive Fan:** Projectiles retain normal damage and additionally inflict strong outward knockback and a brief stagger. Displacement is away from the mech and scales down against resistant enemies.
- **`C` playstyle conversion — Focal Array:** Projectiles initially spread across the cone, then curve inward and converge at the end of their upgraded range. Enemies along the paths can still be struck, while a target held near the focal distance can take several projectile hits. The player manages distance and facing to place priority enemies at the convergence point.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

The base is a close facing-based shotgun, Saturation Choke turns it into reliable cone clearing, Concussive Fan makes it a spacing tool, and Focal Array replaces point-blank use with a learned sweet-spot pattern.

## Consequences

- Saturation Choke hits an enemy at most once per firing event regardless of body size.
- Concussive Fan retains the base multi-projectile geometry and can knock a large enemy only once per event unless tuning explicitly permits more.
- Focal Array must telegraph its focal distance and projectile curvature.
- Exact cone angle, projectile count, multi-hit rule, wave geometry, knockback, stagger, convergence curve, and terrain collision remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Scatter Array concept and locks `D` amplification, `E` functional, and `C` conversion funding. Exact values remain subject to playtesting.
