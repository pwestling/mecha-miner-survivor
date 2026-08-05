---
doc_id: TDD-OBSERVABILITY
title: Performance, Diagnostics, and Observability
status: active
authoritative: true
---

# Performance, Diagnostics, and Observability

## Purpose

This document defines performance budgets, benchmark scenarios, structured logging, metrics, local balance capture, diagnostic packages, development controls, crash behavior, and the evidence required before optimization or tuning changes.

## Performance philosophy

- Frame-rate and correctness budgets are designed in, not inspected only near release.
- Optimize measured bottlenecks while preserving the simplest architecture that meets targets.
- Presentation quality degrades before gameplay state.
- No adaptive enemy reduction, delayed attacks, hidden time dilation, or simulation-result change is a performance technique.
- Per-system timings, counts, and allocations accompany every benchmark so an agent can identify ownership rather than guess.

## Target-device frame budget

[TDR-003](./decisions/TDR-003-require-sixty-fps-on-steam-deck.md) requires a 16.67 ms frame at 60 FPS.

### CPU initial enforceable allocation

| Area | p95 target |
| --- | ---: |
| Input, application flow, command admission | 0.40 ms |
| Complete authoritative simulation | 5.00 ms |
| Snapshot publication and presentation synchronization | 1.00 ms |
| Crowd/actor/VFX presentation updates | 2.00 ms |
| HUD/UI update and drawing preparation | 1.00 ms |
| Audio/haptics event processing | 0.40 ms |
| Godot engine/render submission outside measured presentation | 3.00 ms |
| Unallocated safety margin | 3.87 ms |

Subsystem budgets inside simulation are documented in their specifications and must sum inside the 5.00 ms envelope at the same stress state.

Agents treat these allocations as failure thresholds from the first measurable implementation. They may rebalance sub-budgets without human input only when the same PERF-04 capture still satisfies the 16.67 ms total, GPU/memory targets, and at least 2.0 ms measured p95 safety margin; the evidence bundle records old/new allocations. Relaxing the total target or removing accepted pressure requires the response ladder and a TDR.

### GPU and memory

| Metric | Target |
| --- | ---: |
| GPU p95 | ≤14.0 ms |
| GPU p99 | ≤18.0 ms |
| Process working set in active standard run | ≤2.5 GiB |
| Managed heap after warm-up | ≤256 MiB target; investigate sustained growth |
| Steady active-play managed allocation | ≤1 KiB/frame aggregate target; zero in hot simulation systems |
| Run transition peak working set | ≤3.5 GiB and returns to baseline after disposal |
| Recovery artifact | ≤16 MiB compressed |

The aggregate allocation target accommodates engine/UI behavior; project-controlled hot paths target zero.

## Canonical benchmark scenarios

| ID | Scenario | Purpose |
| --- | --- | --- |
| PERF-01 | Empty generated map, HUD, player | engine/presentation floor |
| PERF-02 | Minute 0 with signature weapon | common baseline and frame pacing |
| PERF-03 | Minute 20 mixed horde, active geode | normal mid-run combined load |
| PERF-04 | Minute 34, event overflow, 75% beacon response, one boss, mature representative build, rocks/pickups/HUD | target-device acceptance benchmark |
| PERF-05 | Four living bosses, hard population ceilings, maximum legal projectile/zone combination | pathological correctness/headroom |
| PERF-06 | Full map generation and validation by every profile/region count | loading/generation budget |
| PERF-07 | Fabrication with full catalog and maximum meaningful ranks, handheld layout | UI allocation/layout stress |
| PERF-08 | Save/recovery capture at peak population | persistence pause/stall check |

PERF-04 runs for ten warmed minutes on retail Steam Deck at 1280×800. Windows uses a documented minimum-spec representative machine at 1920×1080 once selected. Benchmarks pin seed, content/build, camera, build, settings, input script, and driver/OS metadata.

## Frame metrics

Capture per frame/tick:

- wall, CPU, GPU, render-thread, simulation, and idle/present duration;
- per-system simulation and presentation timings;
- frame/tick catch-up count and accumulator debt;
- managed/native allocations and garbage collection;
- draw calls, primitives, instances, materials, shadow casters, particles, decals, and render scale;
- population by entity/source/effect type and queue/capacity high-water marks;
- spatial queries/candidates, navigation rebuild work, collision tests, and damage candidates;
- audio voices/steals and VFX requests/drops; and
- memory by managed, texture, mesh, audio, navigation/map, and recovery categories where available.

Percentile reports use frame distributions after warm-up, not average FPS alone. A benchmark report includes worst-frame timeline and markers for boss, spawn, save, UI, garbage collection, and shader events.

## Structured logging

Logs use timestamp, monotonic sequence, severity, category, stable event code, build/content identity, run/profile diagnostic IDs, tick where relevant, and structured fields.

Initial categories:

- bootstrap/build/platform;
- content/import/asset;
- persistence/cloud/migration;
- generation/validation;
- simulation invariant/command/transaction;
- director/spawn/capacity;
- presentation/resource fallback;
- UI/input/accessibility;
- performance/benchmark; and
- crash/shutdown.

Player-facing expected failures such as unaffordable purchase are not error logs; unexpected rejection/invariant divergence is. Rate-limit repetitive diagnostics and emit a summary count.

Release logs exclude full filesystem paths, usernames, raw Steam identifiers, and uncontrolled content text. Logs rotate at 4 MiB and retain the five newest files. Local diagnostic/balance run records retain the newest 20 with an 8 MiB per-record cap; crash packages retain the newest five with a 64 MiB per-package cap. Cleanup uses validated owned-directory entries and never follows links or deletes a user-exported destination. Settings exposes an explicit sanitized export action.

## Gameplay and balance metrics

Local run records implement every capture requirement in the gameplay balance documents, including:

- build progression and exact resource ledger by boss threshold;
- weapon attempted/effective/overkill damage, activations, hits, targets, uptime, control, and actor counts;
- boss realized DPS and time-to-kill;
- ordinary kill throughput and active populations;
- mining attempts, work, grace, decay, completion, beacon thresholds, damage during holds;
- movement, route, region, discovery, radar, cache, rock, and pack behavior;
- Hull thresholds, hit sequences, source damage, Armor, shield, revival, healing, and death;
- pause/fabrication frequency and wall/active duration; and
- outcome and settlement.

These records are local development data by default under TDR-004. They use stable IDs and schema versions and can be exported as JSON/CSV reports. They never feed runtime adaptive difficulty.

## Diagnostic run record

Every run creates a bounded technical header:

- diagnostic/run ID;
- master seed and generation manifest checksum;
- build/content/schema/random/map versions;
- platform, renderer, quality, resolution, input family;
- selected mech, profile/account-power summary, and active options;
- warnings/invariant/capacity counters; and
- terminal outcome or last recovery tick.

Major event breadcrumbs include deployment, survey, purchases, relic decisions, boss boundaries/deaths, beacon thresholds, recovery writes/restores, and terminal settlement. It is not an exact replay.

## Diagnostic package export

An internal/development action creates a shareable package containing:

- sanitized logs;
- diagnostic run record and result manifest;
- seed/generated map manifest or rejection trace;
- content/build hashes and settings;
- performance summary and recent frame ring buffer;
- screenshot and optional short capture only with explicit action;
- save/recovery headers without private payload unless explicitly included; and
- README describing reproduction commands/tools.

The exporter redacts paths and identifiers, applies size limits, and never uploads automatically.

## Development overlay and controls

Development builds provide an in-game overlay and command palette unavailable in release exports. Capabilities are typed tool actions, not arbitrary script evaluation.

Required actions:

- display tick/FPS/CPU/GPU/memory/allocation and system timings;
- draw collision, navigation, flow field, spatial cells, targeting, attack geometry, mining/resonance, spawn sectors, camera/fog, and map constraints;
- show entity IDs/content IDs/state/provenance under a debug cursor;
- start a run with explicit seed/profile/mech/account state;
- jump to a schedule minute with a declared constructed build;
- grant exact resources or fabricate through the real transaction service;
- spawn named enemy/boss/formation/beacon response through validated services;
- set player Hull/invulnerability for testing without changing content;
- run WB-01 through WB-06 and performance scenarios;
- force save/recovery/migration/cloud failure points; and
- export the diagnostic package.

Every action logs invocation and parameters. Release builds compile out or hard-disable the surface and reject development command-line flags.

## Crash handling

- Register top-level reporting around application boundaries without attempting to continue corrupted simulation.
- Flush a minimal crash header and the preallocated recent-log/frame ring buffers using best-effort safe operations.
- Preserve existing profile/recovery artifacts; crash handling never writes progression directly.
- Next boot recognizes an unclean shutdown, validates recovery, and offers local diagnostic export.
- Native dumps/symbols are retained as separate build artifacts where platform tooling supports them.
- External crash reporting is not included without a later privacy/backend decision.

## Performance regression policy

- Benchmark artifacts store baseline distributions by tagged build and device.
- CI or device runs fail when p95 budget is exceeded, p99 regresses materially, allocations appear in prohibited systems, or memory grows without release.
- A regression exception needs issue/work-item owner, measured cause, allowed duration, and expiration build; exceptions cannot redefine the target.
- Optimization changes include before/after profiles, exact scenario, correctness tests, and readability captures.
- Microbenchmarks do not replace target-device end-to-end results.

## Verification

- Logging schema and redaction tests cover every category and package field.
- Metric reconciliation compares aggregates with authoritative ledgers/event counts.
- Development commands prove release exclusion.
- Long soak tests detect handle/pool/node/memory growth across repeated runs and scene transitions.
- Performance runs are reproducible from one manifest and input script.
- Target-device reports satisfy TDR-003 percentile/stall requirements with representative assets.

## Related documents

- [Verification Strategy](./91-verification-strategy.md)
- [Presentation and Rendering](./30-presentation-and-rendering.md)
- [Build, Dependencies, and Release Operations](./100-build-dependencies-and-release-operations.md)
- [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md)
- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md)
