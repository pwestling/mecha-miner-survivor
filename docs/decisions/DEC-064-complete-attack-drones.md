---
doc_id: DEC-064
title: Complete Attack Drones with Containment Lattice and Three Stats
status: accepted
authoritative: false
---

# DEC-064 — Complete Attack Drones with Containment Lattice and Three Stats

## Decision

Attack Drones' `D`-funded playstyle conversion is **Containment Lattice**. The drones stop seeking targets and firing projectiles. Instead, the permanent squad holds a wide formation around the mech, aligned to persistent facing, and damaging energy links connect adjacent drones. Enemies that intersect a link take damage at a fixed tick cadence. The links damage but do not physically block enemies.

The player aims and repositions the lattice indirectly by moving to change persistent facing and by carrying the formation through the horde. The formation remains active while the mech is stationary and retains its last facing.

Attack Drones exposes exactly three common-ore stats:

- **Damage:** projectile damage normally and lattice-link damage after conversion.
- **Attack rate:** drone firing rate normally and lattice damage-tick rate after conversion.
- **Operational range:** acquisition and roaming range normally and formation radius after conversion.

Squad size, drone movement speed, projectile speed, lattice geometry, and temporary-drone cap remain fixed weapon or branch properties rather than ore stats.

## Status

Accepted complete high-level weapon specification; numeric tuning and listed edge rules open.

## Rationale

Containment Lattice turns autonomous ranged agents into a movement-positioned damage structure without overlapping the behavior of ordinary orbiting weapons: its formation is aligned to player-derived facing rather than continuously rotating. The three selected stats remain legible in every branch and avoid an unbounded permanent-drone-count track.

## Consequences

- Replicator Swarm temporary drones inherit damage, attack rate, and operational range.
- Wolfpack Protocol uses operational range for valid target designation and attack rate for strafing fire; its lock bonus derives from damage rather than adding a stat.
- Containment Lattice uses only the permanent base squad and creates no temporary drones.
- Lattice links connect adjacent drones in the authored formation; exact squad count, polygon shape, link thickness, and hit cooldown remain tuning.
- Enemies already inside the formation are damaged only when intersecting a link, not merely for occupying its interior.
- Changing facing rotates the formation; exact rotation smoothing and collision sampling remain open.

## Specification links

- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-061 — Attack Drones base behavior](./DEC-061-attack-drones-base-behavior.md)
- [DEC-062 — Replicator Swarm](./DEC-062-attack-drones-replicator-swarm.md)
- [DEC-063 — Wolfpack Protocol](./DEC-063-attack-drones-wolfpack-protocol.md)

## Supersedes / superseded by

Completes the high-level Attack Drones design established by DEC-061 through DEC-063. Exact values remain subject to playtesting.
