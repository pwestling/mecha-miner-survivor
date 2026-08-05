---
doc_id: TDR-002
title: Use Seeded Reproducibility Without Lockstep Replay
status: accepted
authoritative: false
validation: deterministic-fixtures-and-diagnostic-reproduction
---

# TDR-002 — Use Seeded Reproducibility Without Lockstep Replay

## Decision

Run gameplay on a fixed simulation timestep. Use a repository-owned, versioned pseudorandom implementation with named independent streams so generated maps, resource profiles, content rolls, and diagnostic scenarios can be reproduced from recorded inputs.

Record the master seed, build identity, content/schema versions, relevant configuration, and major run events in diagnostic summaries. Do not promise bit-exact frame-by-frame input replay across different builds, engine versions, hardware architectures, or platforms.

## Context

Procedural maps, randomized resource profiles, relic selection, target selection, attacks, drops, and authored spawning all need explicit randomness ownership. Reproducible generation and test fixtures make invalid seeds and balance regressions diagnosable. Full lockstep determinism would additionally constrain floating-point math, iteration order, concurrency, physics, engine upgrades, and every source of time.

The accepted game is single-player and has no player-visible replay, rollback networking, daily leaderboard, or shared-seed competition requirement.

## Considered alternatives

### Unspecified or global randomness

Using engine or framework-global random state is initially convenient but makes unrelated code changes alter generated content and prevents focused reproduction. Rejected.

### Full cross-platform deterministic replay

Recording input and replaying a run exactly would aid debugging and could support future replay features. It was rejected as a product guarantee because it imposes significant ongoing constraints without an accepted player-facing need.

### Seeded subsystem reproducibility

Independent streams preserve reproducibility within a compatible build and keep one subsystem's random consumption from perturbing unrelated outcomes. Selected.

## Required random-stream boundary

- The project owns the random algorithm and its serialized state; it does not rely on unspecified `System.Random` or engine behavior for authoritative outcomes.
- A master run seed derives named child streams through a stable hash or split operation.
- Map topology, landmark selection, site placement, run content, combat rolls, loot, and presentation variation use distinct stream families.
- Presentation-only randomness never affects gameplay state.
- Stream identity and derivation version are included in diagnostic metadata.
- Tests may inject scripted random sources for boundary cases.

The precise stream catalog and generation algorithm are fixed in the simulation and procedural-generation specifications. Changing either increments the corresponding compatibility version.

## Consequences

- Generated content can be recreated from a seed when the compatible build and content versions are available.
- Test fixtures can assert stable generation outputs and combat sequences within their declared compatibility version.
- Entity and system iteration order remains explicit for correctness, but the project need not eliminate every cross-platform floating-point difference.
- Multithreading cannot introduce unordered mutation of authoritative state.
- Diagnostic event logs are bounded summaries, not save states or guaranteed replays.
- A future replay, daily challenge, multiplayer, or leaderboard feature requires a new TDR and may require stricter determinism.

## Validation

- Repeated generation with the same seed and versions produces the same canonical generation manifest.
- Changing presentation-only effects does not change authoritative checksums.
- A recorded failing seed can be rerun through map validation and simulation test harnesses.
- Each authoritative random draw is attributable to a documented stream family.

## Specification links

- [Runtime Architecture](../10-runtime-architecture.md)
- [Technical Foundation](../00-technical-foundation.md)
- [Standard Map Generation Contract](../../51-standard-map-generation-contract.md)
- [Standard Wave and Beacon Schedule](../../32-standard-wave-and-beacon-schedule.md)

## Supersedes / superseded by

Initial reproducibility decision; supersedes no prior technical record.
