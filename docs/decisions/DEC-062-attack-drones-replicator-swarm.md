---
doc_id: DEC-062
title: Amplify Attack Drones with a Replicator Swarm
status: accepted
authoritative: false
---

# DEC-062 — Amplify Attack Drones with a Replicator Swarm

## Decision

Attack Drones' `E`-funded amplification is **Replicator Swarm**. The funding assignment is fixed by [DEC-063](./DEC-063-attack-drones-wolfpack-protocol.md) under the balancing method in [DEC-060](./DEC-060-balance-native-branch-funding.md).

Whenever a permanent base-squad drone kills an enemy, it fabricates one temporary duplicate. A temporary drone uses the base drone's autonomous movement, targeting, and short-range strafing attack, then expires after a limited lifetime.

Temporary drones cannot fabricate further drones. The number of simultaneous temporary drones is capped for tuning, presentation, and bounded power.

## Status

Accepted behavior and funding color; lifetime, cap, and numeric tuning open.

## Context

The amplification needs to preserve autonomous drone combat while creating a major increase beyond simply adding one fixed squad member. A combat-grown swarm makes successful drone kills escalate visibly during dense encounters.

## Considered options

### Permanently add more base drones

This is clear but behaves like an ordinary count increase and does not create a distinct payoff loop.

### Generate temporary non-recursive copies on permanent-drone kills

This allows the squad to swell dynamically without an unbounded chain reaction.

## Rationale

Replicator Swarm is “samey but bigger and better”: every drone still seeks and strafes enemies autonomously, but sustained success produces a much larger temporary squad and greater target coverage.

## Consequences

- Only permanent drones can trigger replication.
- A qualifying kill creates at most one temporary drone.
- Temporary drones behave as attackers but never replicate.
- When at the temporary-drone cap, further qualifying kills create no additional drone unless a later rule grants another benefit.
- Exact lifetime, cap, spawn position, target inheritance, expiry presentation, and treatment of simultaneous kills remain open.
- All eventual common-ore stats must affect temporary drones or their derived behavior visibly.

## Specification links

- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

Extends the accepted base behavior in [DEC-061](./DEC-061-attack-drones-base-behavior.md). [DEC-063](./DEC-063-attack-drones-wolfpack-protocol.md) later assigns `E` as its funding resource. It does not settle the playstyle conversion, common-ore stats, or numeric tuning.
