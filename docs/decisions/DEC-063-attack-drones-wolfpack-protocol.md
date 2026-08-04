---
doc_id: DEC-063
title: Give Attack Drones a Wolfpack Protocol
status: accepted
authoritative: false
---

# DEC-063 — Give Attack Drones a Wolfpack Protocol

## Decision

Attack Drones' `A`-funded functional variant is **Wolfpack Protocol**.

The squad stops independently distributing itself among targets. It designates one high-priority valid enemy, favoring bosses and elites before ordinary high-health threats, and every permanent drone converges on that enemy. Temporary drones do not exist in this branch because major branches are mutually exclusive.

The squad's combined effectiveness against the designated target increases as more drones establish active attack locks on it. When the target dies or becomes invalid, the squad immediately chooses another priority target and begins establishing new locks.

Under the catalog-balancing method in [DEC-060](./DEC-060-balance-native-branch-funding.md), `A` funds Wolfpack Protocol and `E` funds the accepted Replicator Swarm amplification.

## Status

Accepted behavior and native funding assignments; lock rules and numeric tuning open.

## Context

Base Attack Drones distribute autonomous pressure and may split across many enemies. The functional branch needs to retain recognizable drone strafing while changing the squad's role and providing an obvious improvement against dangerous priority targets.

For native funding balance, `A` previously funded two amplifications and one functional variant, while `E` had no native assignment. Assigning Wolfpack to `A` and Replicator Swarm to `E` leaves `A` at two of each category and gives `E` its first amplification.

## Considered options

### Add a status effect while drones retain independent targets

This adds utility but does not substantially change the squad's distributed role.

### Coordinate the full squad against one priority target

This trades broad coverage for elite and boss removal while preserving autonomous flight and short-range fire.

## Rationale

Wolfpack Protocol fits the “a bit different in function” category. It remains recognizably a squad of strafing drones, but target coordination and multi-drone locks turn it into a focused threat-removal weapon.

## Consequences

- All permanent drones attack the same designated enemy when one is valid.
- Target priority favors boss and elite status before ordinary health-based scoring; exact scoring and tie-breaking remain open.
- Lock-based effectiveness resets or transfers according to rules still to define when the designated target changes.
- The effectiveness bonus may be expressed as damage, firing cadence, accuracy, or another visible benefit, but must scale clearly with active drone locks and must not add a fourth ore stat.
- Exact lock acquisition time, maximum bonus, target-switch behavior, and handling of unreachable targets remain open.
- Native funding is fixed as `A` for Wolfpack Protocol and `E` for Replicator Swarm.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [DEC-060 — Assign native branch funding for catalog balance](./DEC-060-balance-native-branch-funding.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Extends the base behavior in [DEC-061](./DEC-061-attack-drones-base-behavior.md) and resolves Replicator Swarm's funding color left open by [DEC-062](./DEC-062-attack-drones-replicator-swarm.md). It does not settle the playstyle conversion, common-ore stats, or numeric tuning.
