---
doc_id: DEC-067
title: Complete the Sentry Pod Weapon
status: accepted
authoritative: false
---

# DEC-067 — Complete the Sentry Pod Weapon

## Decision

Sentry Pod (`B + E`, off-color `A`) is accepted with the following complete high-level design.

### Base behavior

At a fixed deployment cadence, the weapon places an indestructible, non-blocking temporary pod at the mech's current position. Pods may deploy without a current enemy, allowing the player to establish fire support before a mining stand. Each pod fires at its nearest valid enemy within range. A fixed maximum number may be active; deploying above the limit replaces the oldest.

### Common-ore stats

- **Damage:** pod projectile damage.
- **Attack rate:** each pod's firing rate.
- **Range:** pod acquisition and firing distance.

Pod lifetime, capacity, deployment cadence, and projectile speed are fixed properties.

### Branches

- **`E` amplification — Battery Overclock:** Every additional active pod increases the attack rate of all active pods up to a fixed network cap. Losing pods immediately reduces the shared bonus.
- **`B` functional variant — Guardian Firmware:** Pods prioritize enemies closest to the mech rather than closest to themselves. Hits push targets away from the mech and briefly stagger them while retaining normal damage.
- **`A` playstyle conversion — Forward Bastion:** Temporary multi-pod deployment is replaced by one persistent heavy bastion. It automatically establishes at the mech's location after the mech remains within a small area for a setup period. It uses strong fixed multipliers to all three stats. Leaving its operating range causes it to pack up; it redeploys after the player next holds a new area long enough.

## Status

Accepted complete high-level design; numeric tuning and listed edge rules open.

## Rationale

Ordinary pods create a trail of temporary territory, Battery Overclock rewards maintaining a full network, Guardian Firmware converts it into personal protection, and Forward Bastion creates a deliberate hold-ground style suited to risky mining stands.

## Consequences

- Pod placement and replacement are automatic and need clear lifetime and capacity feedback.
- Guardian knockback is directed away from the mech, not away from the firing pod.
- Forward Bastion has no manual placement button; setup and relocation are driven by movement and lingering.
- Exact capacity, lifetime, setup area, setup time, operating range, pack-up delay, multipliers, and terrain placement rules remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-060 — Native branch funding balance](./DEC-060-balance-native-branch-funding.md)

## Supersedes / superseded by

Completes the Sentry Pod concept and locks `E` amplification, `B` functional, and `A` conversion funding. Exact values remain subject to playtesting.
