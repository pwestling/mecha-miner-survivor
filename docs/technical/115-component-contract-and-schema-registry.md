---
doc_id: TDD-CONTRACT-REGISTRY
title: Component, Contract, and Schema Registry
status: active
authoritative: true
---

# Component, Contract, and Schema Registry

## Purpose

This registry gives autonomous implementation agents one routing table for system ownership and cross-boundary data. It prevents two agents from creating competing abstractions for the same state and makes producer, consumer, timing, failure, thread, and version responsibility explicit.

The IDs identify logical contracts, not mandatory C# class names. An implementation may use several private types inside one component, but it may not split mutable ownership or change a cross-boundary meaning without updating this registry and its cited subsystem specification.

## Accepted project boundary

| Project | Owns | Allowed project dependencies | Godot types allowed |
| --- | --- | --- | --- |
| `MechaMiner.Content` | strict source models, schemas, compiler, immutable runtime definitions, behavior-registration contracts | .NET base libraries only | No |
| `MechaMiner.Diagnostics` | build identity and its manifest, stable diagnostic codes, bounded structured logs, redaction, rotation, metric/profiler-marker registry, frame budget allocation, benchmark reports, evidence manifests | .NET base libraries only | No |
| `MechaMiner.Simulation` | run domain, geometry, combat, encounters, mining, progression, commands, events, snapshots | `MechaMiner.Content` | No |
| `MechaMiner.Persistence` | save envelopes, canonical serialization, checksums, migrations, atomic storage abstractions | `MechaMiner.Content`, narrow immutable types from `MechaMiner.Simulation` | No |
| `MechaMiner.Tools` | command-line workflows, audits, generators, reports, benchmark orchestration | all pure projects | No; it may launch the pinned Godot executable as an external process |
| `MechaMiner.Game` | Godot composition root, input adapters, presentation, UI, audio, platform adapters | all pure projects plus Godot APIs | Yes |

Tests mirror those projects. Production projects never depend on test projects or generated test fixtures. `Content`, `Diagnostics`, `Simulation`, and `Persistence` never reference `MechaMiner.Game` or engine assemblies.

`MechaMiner.Diagnostics` is the project that owns `CMP-OBS-001`. It sits **below `MechaMiner.Content`** in the dependency order: it is a dependency leaf that references no other project, so every other project may depend on it and it may depend on none of them, which is what lets both the pure projects and `MechaMiner.Game` reach it without any of them reaching each other. It exists as its own project because its consumers are `MechaMiner.Game`, whose [initialization order](#initialization-order) reads build identity at step 1 and opens bounded local logging at step 2 before content loads, and `MechaMiner.Tools`, which renders reports and orchestrates benchmarks. No game or pure project may depend on the tool host, so the component cannot live in `MechaMiner.Tools`; and placing logging, metrics, and build identity inside `Content`, `Simulation`, or `Persistence` would contradict [placing a type with the project that owns its semantics](./114-autonomous-agent-execution-protocol.md#naming-and-file-placement-defaults), because this registry already separates “Logs/metrics/evidence buffers” from every persistence row in the [mutable-state ownership matrix](#mutable-state-ownership-matrix). `Simulation` deliberately does not reference it: simulation components publish `CTR-SIM-001` domain event batches that `CMP-OBS-001` consumes, which is also what keeps diagnostic I/O off the authoritative tick.

Dependency direction is enforced through project references and an architecture test. Moving a type “for convenience” may not create a reverse edge.

## Component registry

| ID | Logical component | Mutable state owned | Consumes | Produces | Thread/frame affinity | Forbidden responsibility |
| --- | --- | --- | --- | --- | --- | --- |
| `CMP-APP-001` | Application coordinator | top-level lifecycle, current route/session identity | boot result, UI intents, terminal result | application transitions | Godot main thread | gameplay rules or direct save mutation |
| `CMP-CNT-001` | Content compiler/validator | no runtime mutable state | source JSON, schemas, registries, manifests | canonical compiled bundle and reports | build/tool process | silently defaulting invalid content |
| `CMP-CNT-002` | Runtime content registry | one immutable loaded bundle | compiled bundle | typed definitions by stable ID | initialized before UI/run; read-only thereafter | file I/O or behavior execution |
| `CMP-RUN-001` | Run session | run identity, seed streams, pause reasons, command sequences, terminal state | validated run package, commands/transactions | simulation ticks, results, diagnostics | authoritative main-thread tick | persistent-profile mutation |
| `CMP-SIM-001` | Simulation world | entities, system stores, schedules, run-local state | compiled definitions, map manifest, normalized commands | domain events, snapshot state, terminal proposal | fixed 60 Hz authoritative tick | Godot/files/platform/wall-time access |
| `CMP-SIM-002` | Command/transaction gate | admitted sequence/idempotency history | input commands and paused transaction requests | accepted normalized commands or typed rejection | before tick or atomically between ticks | UI presentation or partial mutations |
| `CMP-SIM-003` | Snapshot/event publisher | double buffers and event sequence | committed simulation state/events | immutable presentation snapshot and ordered event batch | end of committed tick | owning authoritative gameplay state |
| `CMP-GEO-001` | Geometry and world-query service | static geometry, raster, spatial index, query scratch | map manifest and entity transforms | deterministic collision, route, visibility, spawn, target candidates | simulation phases only | using mesh/physics-node bounds as authority |
| `CMP-MAP-001` | Map generator/validator | attempt-local candidates only | generation request, content, RNG streams | fully validated immutable map manifest or typed failure | preparation/background-safe immutable job | publishing a partially valid map |
| `CMP-COM-001` | Combat runtime | attack schedules, projectiles, zones, weapon actors, status/control state | loadout, geometry queries, definitions | damage/control/death candidates and combat events | fixed tick phases 7–10 | awarding resources or driving visuals directly |
| `CMP-ENC-001` | Encounter director | schedule cursors, populations, queues, boss/formation state | run clock, schedule, map/spawn queries, beacon requests | spawn intents, encounter/boss events | fixed tick phases 2–3 and owned boss phases | adaptive difficulty from player strength |
| `CMP-MIN-001` | Mining runtime | site progress, grace/decay, checkpoints, beacon history | player position, site definitions, modifiers | payout and encounter-response intents | fixed tick phase 11 | directly changing profile currency |
| `CMP-PRG-001` | Run progression/economy | resource ledger, loadout, ranks, branches, utilities, relic state | payouts/pickups and validated transactions | ledger entries, loadout versions, result manifest | gameplay phase 11 or paused atomic boundary | presentation-priced alternatives or unledgered grants |
| `CMP-PRE-001` | Presentation bridge | simulation-ID to presentation-handle mappings | snapshot and event batch | durable/transient adapter updates | Godot main/render frame | authoritative targeting, collision, or timing |
| `CMP-PRE-002` | World/crowd/VFX renderer | render instances, pools, animation/VFX state | presentation adapter updates | frames and visual diagnostics | Godot main/render frame | removing authoritative actors on saturation |
| `CMP-UI-001` | UI route/view-model system | focus, scroll, selection, transient editor state | application/profile/run view models | typed UI intents | UI/render clock | recomputing or mutating domain rules |
| `CMP-AUD-001` | Audio/haptics service | voices, buses, aggregation, music/haptic state | presentation events and settings | bounded audio/haptic output | render/UI clock; pauses by policy | authoritative randomness or gameplay results |
| `CMP-PST-001` | Persistence service | write queue, last-good metadata, migration/settlement journal | typed profile/settings/recovery transactions | durable atomic files or typed failure | I/O boundary; no simulation mutation | direct UI or cloud conflict policy |
| `CMP-PLT-001` | Platform adapter | platform availability/session metadata | capability requests, portable save bytes | typed local/Steam results | platform API affinity; gracefully unavailable | owning game rules or canonical local save |
| `CMP-OBS-001` | Diagnostics/metrics service | bounded logs, metrics, rings, evidence manifests | stable events/counters/build identity | local reports/packages | never blocks authoritative tick on I/O | changing behavior from telemetry |

Each mutable datum has exactly one row owner. Other components receive immutable values, commands, or handles; they do not retain a second writable copy.

## Cross-boundary contract registry

| ID | Contract | Producer | Consumer | Delivery and ordering | Failure behavior | Normative source |
| --- | --- | --- | --- | --- | --- | --- |
| `CTR-CNT-001` | Canonical content bundle | `CMP-CNT-001` | all runtime/tool consumers via `CMP-CNT-002` | immutable, canonical order, hash/version identified, loaded before use | startup/deployment fails on any invalid definition | [Content Data](./40-content-data-and-validation.md) |
| `CTR-CNT-002` | Behavior registry manifest | component owners/build tooling | `CMP-CNT-001` | explicit/generated stable kind IDs; one implementation per kind | build fails on missing/duplicate/incompatible kind | [Content Data](./40-content-data-and-validation.md#behavior-registries) |
| `CTR-MAP-001` | Generation request | `CMP-RUN-001` | `CMP-MAP-001` | immutable seed/signature/profile/version request | typed invalid-request failure before generation | [Map Generation](./50-procedural-map-generation.md) |
| `CTR-MAP-002` | Validated map manifest | `CMP-MAP-001` | `CMP-RUN-001`, `CMP-GEO-001`, presentation/map UI | one immutable canonical manifest published only after all validators; checksum identified | bounded retry then compatible validated fallback; never partial | [Map Generation](./50-procedural-map-generation.md#generated-manifest) |
| `CTR-RUN-001` | Validated run package | application preparation | `CMP-RUN-001` | content/profile/map/build identities frozen at deployment | preparation returns to Hangar without mutation | [Runtime](./10-runtime-architecture.md#application-lifecycle) |
| `CTR-RUN-002` | Active command envelope | input adapter | `CMP-SIM-002` | run ID, target tick, monotonic sequence, normalized payload | stale/duplicate/invalid commands return typed rejection/no change | [Runtime](./10-runtime-architecture.md#commands-and-mutations) |
| `CTR-RUN-003` | Paused transaction request/result | `CMP-UI-001` through application | `CMP-SIM-002`, owning domain component | immutable preview version plus idempotency key; commit between ticks | all-or-nothing typed result; stale preview changes nothing | [Mining/Progression](./24-mining-fabrication-and-progression-runtime.md) |
| `CTR-SIM-001` | Domain event batch | simulation components | other simulation owners and `CMP-OBS-001` | tick-local, stable sequence, never dropped | invariant failure ends run safely rather than omitting authoritative event | [Simulation Core](./20-simulation-core.md#domain-and-presentation-events) |
| `CTR-SIM-002` | Presentation event batch | `CMP-SIM-003` | presentation/UI/audio | ordered after each committed tick; may carry coalescing policy | noncritical visual/audio event may degrade; authority unaffected | [Simulation Core](./20-simulation-core.md#domain-and-presentation-events) |
| `CTR-SIM-003` | Presentation snapshot | `CMP-SIM-003` | `CMP-PRE-001`, `CMP-UI-001`, `CMP-AUD-001` | immutable latest complete snapshot with run/tick/version; double-buffered | consumer drops stale snapshot or fully rebuilds; never mutates it | [Simulation Core](./20-simulation-core.md#presentation-snapshot) |
| `CTR-SIM-004` | Terminal run result manifest | `CMP-PRG-001`/`CMP-RUN-001` | application, UI, persistence | created once after committed terminal outcome; canonical ledger reconciliation | invalid/incomplete result cannot settle; prior profile remains | [Mining/Progression](./24-mining-fabrication-and-progression-runtime.md#terminal-settlement) |
| `CTR-PST-001` | Profile/settings transaction | application/domain services | `CMP-PST-001` | expected revision, idempotency key, complete replacement value | conflict or I/O failure returns typed result and preserves prior good file | [Persistence](./70-persistence-and-platform-services.md#persistent-transaction-model) |
| `CTR-PST-002` | Pending extraction settlement | `CMP-RUN-001` | `CMP-PST-001` | durable intent precedes profile commit; settlement ID applied once | replay completes or observes applied ID; never duplicates reward | [Persistence](./70-persistence-and-platform-services.md#extraction-settlement) |
| `CTR-PST-003` | Run recovery snapshot | `CMP-RUN-001` | `CMP-PST-001`, future compatible `CMP-RUN-001` | coherent post-tick capture with map/content/random/build identities | incompatible/corrupt artifact is preserved diagnostically and not resumed | [Persistence](./70-persistence-and-platform-services.md#run-recovery) |
| `CTR-UI-001` | Immutable screen/view model | application/profile/run owners | `CMP-UI-001` | complete versioned model per relevant state change; display-ready derived values | UI retains prior complete model or shows typed unavailable state | [UI](./60-ui-input-and-accessibility.md#ui-architecture) |
| `CTR-UI-002` | Typed UI intent | `CMP-UI-001` | application or command/transaction gate | focus/navigation never emits mutation; confirmation carries model revision | stale or unavailable action produces explicit feedback/no change | [UI](./60-ui-input-and-accessibility.md#fabrication-and-transactional-ui) |
| `CTR-PRE-001` | Presentation binding lifecycle | `CMP-RUN-001`/`CMP-SIM-003` | `CMP-PRE-001` | bind, update from snapshot/events, dispose; run identity fences all handles | missed events trigger snapshot rebuild; disposed handles ignore late work | [Presentation](./30-presentation-and-rendering.md#snapshot-synchronization) |
| `CTR-PLT-001` | Platform capability/result | application/persistence | `CMP-PLT-001` and callers | explicit capability query; async result carries request identity/cancellation | unavailable platform returns supported typed local fallback, never blocks play | [Persistence](./70-persistence-and-platform-services.md#steam-platform-adapter) |
| `CTR-OBS-001` | Metric/log/evidence record | every owner | `CMP-OBS-001` | stable code/ID, bounded structured fields, monotonic sequence where ordered | rate-limit/drop only declared diagnostics; never block or change authority | [Observability](./90-performance-diagnostics-and-observability.md) |

Cross-boundary payloads never expose mutable collections. Producers may reuse internal buffers only after the consumer-facing snapshot/batch lifetime has ended under an explicit buffer-lease contract.

## Schema registry

Schema IDs identify durable or machine-consumed structures. The implementation assigns each a version and fixture corpus before the first consumer lands.

| ID | Schema | Scope and required identity | Owner |
| --- | --- | --- | --- |
| `SCH-CNT-001` | Common content definition envelope | stable ID, schema/content versions, status, localization, source refs, presentation | `CMP-CNT-001` |
| `SCH-CNT-002` | Gameplay catalog definitions | resources, mechs, enemies, bosses, weapons, branches, utilities, relics, PowerUps, unlocks, mining, encounters, maps | `CMP-CNT-001` |
| `SCH-CNT-003` | Presentation/audio definitions | logical asset/event mappings and fallback, never gameplay outcomes | `CMP-CNT-001` |
| `SCH-CNT-004` | Canonical compiled bundle | normalized defaults, canonical order, derived values, registrations, source map, bundle hash | `CMP-CNT-001` |
| `SCH-AST-001` | Asset/license/import manifest | source/runtime hashes, provenance/license, budgets, importer, consumers, fallback | asset pipeline with content validation |
| `SCH-MAP-001` | Map generation request | master derivation, profile/signature, generator/content versions | `CMP-MAP-001` |
| `SCH-MAP-002` | Validated map manifest | topology, geometry, deployment, sites, landmarks, discovery/nav, audit/checksum | `CMP-MAP-001` |
| `SCH-RUN-001` | Run configuration/identity | build/content/map/random versions, mech/profile/account modifiers, seed | `CMP-RUN-001` |
| `SCH-RUN-002` | Result manifest | terminal reason, ledger reconciliation, build/loadout/map, metrics, settlement ID | `CMP-PRG-001` |
| `SCH-RUN-003` | Recovery snapshot | full authoritative state required to resume after a committed tick | `CMP-RUN-001` with `CMP-PST-001` serialization |
| `SCH-PST-001` | Save envelope | kind/version/revision/timestamps/checksum/payload | `CMP-PST-001` |
| `SCH-PST-002` | Profile payload | banked currency, PowerUps, unlocks, records/history, settlement history | application domain persisted by `CMP-PST-001` |
| `SCH-PST-003` | Portable settings | gameplay/UI/audio/accessibility/input settings allowed in cloud | application domain persisted by `CMP-PST-001` |
| `SCH-PST-004` | Device settings | display/adapter/device-only values excluded from cloud | application domain persisted by `CMP-PST-001` |
| `SCH-OBS-001` | Diagnostic run record | run/build/content/map identity, bounded breadcrumbs, outcome | `CMP-OBS-001` |
| `SCH-OBS-002` | Performance report | scenario/device/settings, distributions, subsystem counters, budgets | `CMP-OBS-001` |
| `SCH-OBS-003` | Task evidence summary | task/work package, authority, commands, results, artifacts, risks | implementation tooling/`CMP-OBS-001` |
| `SCH-QUA-001` | Work-package verification registry | `VER-*` IDs, requirements, selectors/scenarios, evidence kinds, tiers/status | verification tooling and owning work package |
| `SCH-BLD-001` | Build/release manifest | version/commit/tool/content hashes, target/configuration, artifacts/checksums | build tooling |

Schemas reject unknown fields, have structural and semantic validators, enforce size/count/depth limits, and include valid/current/old/future/corrupt fixtures as applicable. Runtime code consumes typed validated values rather than generic JSON trees.

## Mutable-state ownership matrix

| State | Sole writer | Read path |
| --- | --- | --- |
| Content definitions | compiler at build/load; immutable afterward | runtime registry |
| Profile, settings, history | application domain transaction committed by persistence | immutable profile view model/snapshot |
| Map topology and placed sites | generator before run publication | immutable manifest |
| Entity transforms/Hull/status | simulation phase owner | queries and presentation snapshot |
| Run clock/pause/terminal state | run session | snapshot/view models/diagnostics |
| Resource balances/loadout/upgrades | progression ledger/transactions | immutable ledger/loadout views |
| Mining progress/checkpoints/beacon bits | mining runtime | snapshot/events |
| Encounter populations/queues/boss state | encounter runtime | snapshot/events/metrics |
| Render nodes/instances/VFX | presentation | render frame only |
| UI focus/selection/scroll | UI route | UI only; never domain truth |
| Audio voices/music phase | audio service | audio diagnostics only |
| Cloud transfer state | platform adapter | typed platform results/UI status |
| Logs/metrics/evidence buffers | diagnostics service | exported immutable reports |

Any implementation that requires two writers must redesign the interaction as a command plus result or identify one writer as a cache rebuilt from the owner.

## Initialization order

Application startup follows this order and fails within the owning boundary:

1. Verify build/tool identity embedded in the executable.
2. Start bounded local logging and crash breadcrumbs.
3. Load and validate the canonical content bundle and behavior registries.
4. Load/migrate/recover settings and profile from local persistence.
5. Initialize the platform adapter; failure selects the local unavailable result rather than failing the game.
6. Construct application routes, UI services, presentation settings, and audio.
7. Enter the front end/hangar only with a valid content registry and coherent local profile.

Run preparation then snapshots profile/options, derives the run seed/streams, generates and validates a map, constructs the pure run session, binds presentation/UI, publishes the opening survey model, and only then admits active ticks.

Shutdown reverses ownership: stop new commands, freeze/terminate the run coherently, flush only bounded diagnostics and already-authorized atomic persistence, dispose presentation/audio/platform handles, and exit without an indefinite wait.

## Contract change rules

| Change | Required action |
| --- | --- |
| Private implementation with identical observable contract | local task, tests only |
| Additive nonpersistent field with explicit default | update owner/consumers/fixtures and registry if cross-boundary |
| Ordering, timing, failure, ownership, or thread-affinity change | update subsystem spec, registry, requirements, and integration tests before consumers |
| Content/save/recovery schema change | increment version and add old/current/future/migration fixtures |
| Stable ID rename/removal | preserve alias/tombstone/migration; never silently reuse |
| Project dependency edge | architecture review and registry update; reverse Godot dependency is forbidden |
| New cross-component mutable writer | reject; redesign or record a superseding TDR |

Contract changes land producer, schema/validation, compatibility behavior, fakes, and tests before or atomically with consumers. Agents do not use reflection, dynamic dictionaries, raw engine nodes, or unversioned serialized blobs to bypass a missing contract.

## Verification

- Architecture tests enforce project-reference direction and prohibit Godot references in pure projects.
- Registration tests assert every `CMP-*`, `CTR-*`, `SCH-*`, and `VER-*` ID is unique, indexed, and resolves its references.
- Contract fixtures prove happy, invalid, stale, duplicate, capacity, cancellation, disposal, and version-mismatch paths where applicable.
- Snapshot/view/event mutation tests prove consumers cannot alter producer state.
- Lifecycle tests exercise boot failure at each initialization stage and late results after run disposal.
- Documentation validation checks that referenced registry and normative anchors exist.
- Every work package cites the component(s) it may modify and the contract(s)/schema(s) it may change or consume.

## Related documents

- [Runtime Architecture](./10-runtime-architecture.md)
- [Simulation Core](./20-simulation-core.md)
- [Content Data and Validation](./40-content-data-and-validation.md)
- [Persistence and Platform Services](./70-persistence-and-platform-services.md)
- [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md)
- [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md)
