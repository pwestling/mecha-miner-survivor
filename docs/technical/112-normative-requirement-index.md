---
doc_id: TDD-REQUIREMENTS
title: Normative Technical Requirement Index
status: active
authoritative: true
---

# Normative Technical Requirement Index

## Purpose

This index gives implementation agents stable requirement IDs for planning, commits, tests, and handoffs. It summarizes—not replaces—the linked normative sections. If summary and source conflict, the linked source controls and this index must be corrected.

## Foundation and runtime

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-FND-001 | Use pinned Godot 4.7.1 .NET, C# runtime/tooling, and Mobile renderer. | [Foundation](./00-technical-foundation.md) | clean build/import/export spike |
| TR-FND-002 | Runtime logic must not use GDScript or mixed-language ownership. | [Foundation](./00-technical-foundation.md#language-boundary) | source/build scan |
| TR-FND-003 | Windows/Steam and Steam Deck are initial release targets; macOS is an authoring host. | [Foundation](./00-technical-foundation.md#platform-boundary) | target exports/device flow |
| TR-FND-004 | Steam Deck must sustain 60 FPS at 1280×800 in PERF-04. | [TDR-003](./decisions/TDR-003-require-sixty-fps-on-steam-deck.md) | ten-minute device report |
| TR-RUN-001 | Simulation is the sole authority for active-run gameplay. | [Runtime](./10-runtime-architecture.md#architectural-style) | presentation mutation/rebuild tests |
| TR-RUN-002 | Simulation uses a fixed 60 Hz tick and never authoritative variable delta. | [Runtime](./10-runtime-architecture.md#clock-domains) | clock fixtures |
| TR-RUN-003 | All blocking pause reasons freeze every gameplay clock and system while UI remains responsive. | [Runtime](./10-runtime-architecture.md#pause-contract) | pause-reason matrix |
| TR-RUN-004 | Authoritative external mutation enters through typed commands or atomic paused transactions. | [Runtime](./10-runtime-architecture.md#commands-and-mutations) | command/idempotency tests |
| TR-RUN-005 | System phase order is fixed and structural mutation is deferred. | [Runtime](./10-runtime-architecture.md#system-phase-ordering) | phase/order fixtures |
| TR-RUN-006 | Authoritative simulation begins serial on the main game thread. | [Runtime](./10-runtime-architecture.md#concurrency-baseline) | architecture and race tests |
| TR-RUN-007 | A run technical failure preserves the existing profile and does not publish partial state. | [Simulation](./20-simulation-core.md#tick-transaction) | injected failure test |
| TR-RUN-008 | Extraction occurs immediately at 35:00 before any event at or after that boundary. | [Simulation](./20-simulation-core.md#boundary-and-tie-ordering) | final-boundary golden fixtures |
| TR-RUN-009 | Run content/RNG/build/version identity must be recorded for diagnostics. | [TDR-002](./decisions/TDR-002-use-seeded-reproducibility-without-lockstep-replay.md) | diagnostic header fixture |
| TR-RUN-010 | Cross-build/platform bit-exact replay is not promised. | [TDR-002](./decisions/TDR-002-use-seeded-reproducibility-without-lockstep-replay.md) | scope/architecture review |

## Simulation, geometry, and combat

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-SIM-001 | Use generational run-local entity IDs and reject stale references. | [Simulation](./20-simulation-core.md#entity-identity) | reuse/property tests |
| TR-SIM-002 | Use purpose-built packed stores, not an unapproved generic ECS. | [Simulation](./20-simulation-core.md#entity-identity) | architecture review/performance |
| TR-SIM-003 | Derived modifiers follow base, flat ranks, additive named percentages, branch, relic, conditional, target-side order. | [Simulation](./20-simulation-core.md#derived-statistics-and-modifiers) | modifier matrix |
| TR-SIM-004 | Finite actors snapshot declared values; persistent actors use declared live values for future actions. | [Simulation](./20-simulation-core.md#derived-statistics-and-modifiers) | fabrication/relic actor fixtures |
| TR-SIM-005 | Presentation consumes immutable snapshots/events and can rebuild without state change. | [Simulation](./20-simulation-core.md#presentation-snapshot) | reconstruction test |
| TR-SIM-006 | Authoritative randomness uses the exact versioned PCG32, derivation, stream-family, conversion, ordering, and recovery contract. | [Simulation](./20-simulation-core.md#authoritative-random-number-contract) | golden-vector/independence/recovery fixtures |
| TR-GEO-001 | Authoritative gameplay is planar; simulation X east/Y north maps to Godot X/-Z. | [TDR-005](./decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md) | coordinate/overlay tests |
| TR-GEO-002 | Mesh/animation bounds never define gameplay collision. | [Geometry](./21-world-geometry-navigation-and-spatial-queries.md#collision-primitives) | debug overlay captures |
| TR-GEO-003 | Player uses swept circle/slide against static terrain; enemies remain mutually non-solid. | [Geometry](./21-world-geometry-navigation-and-spatial-queries.md#player-and-enemy-movement) | collision fixtures |
| TR-GEO-004 | Ordinary navigation uses shared static raster/flow guidance rather than one navigation agent per enemy. | [Geometry](./21-world-geometry-navigation-and-spatial-queries.md#navigation-representation) | route/performance tests |
| TR-GEO-005 | Dynamic targeting/collision queries use the shared allocation-free spatial index. | [Geometry](./21-world-geometry-navigation-and-spatial-queries.md#dynamic-spatial-index) | differential/performance tests |
| TR-GEO-006 | Valid spawns are offscreen, navigable, safe, and nonoverlapping with prohibited envelopes. | [Geometry](./21-world-geometry-navigation-and-spatial-queries.md#spawn-and-re-entry-geometry) | map-edge spawn matrix |
| TR-COM-001 | Every weapon ID maps to exactly one registered behavior and exactly three registered branches. | [Combat](./22-combat-and-weapon-runtime.md#behavior-implementation-strategy) | content registration gate |
| TR-COM-002 | Target selection is deterministic after authored priorities and never depends on hash/render order. | [Combat](./22-combat-and-weapon-runtime.md#target-acquisition) | tie/randomized fixtures |
| TR-COM-003 | Every damage/control event carries immutable source/weapon/branch/relic/actor provenance. | [Combat](./22-combat-and-weapon-runtime.md#attack-provenance) | attribution reconciliation |
| TR-COM-004 | Player damage resolves in the exact gameplay Armor/shield/revival order. | [Combat](./22-combat-and-weapon-runtime.md#incoming-player-damage) | damage matrix |
| TR-COM-005 | Rock hits cannot trigger enemy-only targeting, kill, focus, chain, stack, or resource behavior. | [Combat](./22-combat-and-weapon-runtime.md#target-acquisition) | every weapon rock fixture |
| TR-COM-006 | Secondary effects obey explicit recursion flags and hard actor/target caps. | [Combat](./22-combat-and-weapon-runtime.md#effect-recursion-and-caps) | pathological chain tests |
| TR-COM-007 | Relics integrate through declared hook points with compatibility results for every weapon. | [Combat](./22-combat-and-weapon-runtime.md#relic-integration) | ten-by-fifteen matrix |
| TR-COM-008 | Authoritative combat actors may not disappear because visual pools are saturated. | [Simulation](./20-simulation-core.md#capacity-and-overload-behavior) | pool saturation test |

## Encounters, mining, and progression

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-ENC-001 | Director executes the fixed 35-row schedule and never adapts to player strength/health/route. | [Encounters](./23-encounter-director-and-enemy-runtime.md#minute-schedule-execution) | schedule compile/full-run test |
| TR-ENC-002 | Baseline, event, beacon, minion, elite, and boss populations retain explicit source accounting. | [Encounters](./23-encounter-director-and-enemy-runtime.md#population-classes) | ceiling/queue metrics |
| TR-ENC-003 | Capacity-blocked authored spawns queue rather than cancel or visibly violate placement. | [Encounters](./23-encounter-director-and-enemy-runtime.md#formation-materialization) | capped formation fixtures |
| TR-ENC-004 | Ordinary enemies use fixed profiles; Needler is the sole ordinary projectile specialist. | [Encounters](./23-encounter-director-and-enemy-runtime.md#ordinary-enemy-runtime) | profile/behavior registry |
| TR-ENC-005 | Boss special timelines, overlap, re-entry, persistence, and reward follow their state machines. | [Encounters](./23-encounter-director-and-enemy-runtime.md#boss-behavior-state-machines) | four-boss matrix |
| TR-ENC-006 | Boss death creates exact physical loot and never directly banks it. | [Encounters](./23-encounter-director-and-enemy-runtime.md#boss-death-and-physical-loot) | loot/collection/settlement test |
| TR-MIN-001 | At most one validated standard mining zone advances; occupancy is inclusive and automatic. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#occupancy-and-progress) | boundary/invalid-map tests |
| TR-MIN-002 | Outside work holds for 0.5 seconds then decays at four times current forward rate without undoing checkpoints. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#outside) | timing/modifier fixtures |
| TR-MIN-003 | Site payouts and thresholds commit atomically in ascending work order. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#payout-profiles) | site-class golden tests |
| TR-MIN-004 | Geode resonance is spatial/current except projectile creation-time damage; field ends at completion. | [Encounters](./23-encounter-director-and-enemy-runtime.md#resonance-evaluation) | boundary/completion tests |
| TR-MIN-005 | Hyper Gold response threshold bits never reset on decay and living responders persist. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#hyper-gold-beacon-state) | threshold/multi-site test |
| TR-PRG-001 | Every resource mutation is reconciled through the typed run ledger. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#run-resource-ledger) | result reconciliation |
| TR-PRG-002 | Fabrication previews and commits use the same immutable content/domain calculations. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#fabrication-availability-model) | preview/result equality |
| TR-PRG-003 | Slot, duplicate, recipe, price, branch, utility, and relic commitments are atomic and idempotent. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md) | full transaction matrix |
| TR-PRG-004 | Radar reveals category bearing only, with seven exact categories and deterministic retarget/exhaustion. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#resource-radar) | radar fixture suite |
| TR-PRG-005 | Extraction banks only unsecured Hyper Gold through durable idempotent settlement. | [Mining runtime](./24-mining-fabrication-and-progression-runtime.md#terminal-settlement) | crash settlement tests |

## Content and generation

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-DAT-001 | Strict JSON plus schemas/semantic validation is machine-consumed content source. | [TDR-006](./decisions/TDR-006-author-validated-content-as-strict-json.md) | compiler fixture suite |
| TR-DAT-002 | Unknown fields, missing references/registrations/assets/strings, and invalid units fail builds. | [Content](./40-content-data-and-validation.md) | invalid corpus |
| TR-DAT-003 | Canonical bundle/hash is independent of source enumeration order. | [Content](./40-content-data-and-validation.md#compilation-pipeline) | permutation test |
| TR-DAT-004 | Gameplay IDs remain stable and are never reassigned. | [Content](./40-content-data-and-validation.md#stable-id-policy) | tombstone/migration test |
| TR-DAT-005 | Every content change updates gameplay source, JSON, generated reports, and fixtures together. | [Content](./40-content-data-and-validation.md#agent-content-change-workflow) | CI staleness/diff gate |
| TR-DAT-006 | All project JSON uses the exact strict typed `System.Text.Json`, schema, canonical-order, numeric, and SHA-256 codec policy. | [Content](./40-content-data-and-validation.md#json-codec-and-schema-baseline) | cross-domain codec/schema fixture corpus |
| TR-MAP-001 | Profile selects exactly four materials uniformly among signature-valid subsets. | [Generation](./50-procedural-map-generation.md#resource-profile-selection) | profile distribution tests |
| TR-MAP-002 | Major-region graph is bridgeless with degree at least two. | [Generation](./50-procedural-map-generation.md#bridgeless-backbone) | graph property tests |
| TR-MAP-003 | Generated scale/topology/obstacles/connectors satisfy all hard gameplay constraints. | [Generation](./50-procedural-map-generation.md) | manifest validators |
| TR-MAP-004 | Important sites are constraint-solved with exact counts/bands/separations/distribution/clearance. | [Generation](./50-procedural-map-generation.md#constraint-solved-placement) | 10,000-seed audits |
| TR-MAP-005 | Only a fully validated immutable manifest reaches a run. | [Generation](./50-procedural-map-generation.md#generated-manifest) | publish boundary tests |
| TR-MAP-006 | Generation attempts are bounded and release fallback remains a prevalidated profile-compatible generated map. | [Generation](./50-procedural-map-generation.md#retry-and-failure-strategy) | forced failure/fallback test |
| TR-MAP-007 | Dynamic rocks obey independent chance, annulus, offscreen, exclusion, and no-visible-recycle rules. | [Generation](./50-procedural-map-generation.md#dynamic-destructible-rocks) | placement/probability test |

## Presentation, UI, audio, and assets

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-PRE-001 | Ordinary hordes use GPU-instanced crowd rendering, not full node/skeleton/physics actors. | [Presentation](./30-presentation-and-rendering.md#ordinary-enemy-crowd-renderer) | 900-instance spike |
| TR-PRE-002 | Gameplay camera is fixed-scale, north-up, orthographic, nonrotating, with 24-meter vertical framing through M4. | [Presentation](./30-presentation-and-rendering.md#camera) | aspect/boundary captures |
| TR-PRE-003 | Critical visual geometry matches authoritative shapes/ticks and survives low/reduced settings. | [Presentation](./30-presentation-and-rendering.md#geometry-correspondence) | overlay/accessibility captures |
| TR-PRE-004 | Presentation degrades by priority without changing gameplay or hiding critical cues. | [Presentation](./30-presentation-and-rendering.md#quality-tiers) | saturation/quality matrix |
| TR-AUD-001 | Audio is event-driven through one service with priority/concurrency/voice budgets. | [Audio](./31-audiovisual-feedback.md#audio-architecture) | 64-voice stress |
| TR-AUD-002 | Gameplay-relevant audio has visual redundancy and critical captions. | [Audio](./31-audiovisual-feedback.md#captions-and-visual-redundancy) | accessibility registry audit |
| TR-UI-001 | Views consume immutable view models and emit typed intents; they do not own domain rules. | [UI](./60-ui-input-and-accessibility.md#ui-architecture) | UI/domain separation tests |
| TR-UI-002 | Every screen and standard flow is gamepad complete with visible/restored focus. | [UI](./60-ui-input-and-accessibility.md#gamepad-navigation) | controller-only route matrix |
| TR-UI-003 | Desktop and handheld use explicit responsive compositions; required text never drops below 9 pixels. | [UI](./60-ui-input-and-accessibility.md#responsive-layout-system) | screenshot/text measurement |
| TR-UI-004 | Confirmations and UI state never spend resources on focus/navigation or duplicate submission. | [UI](./60-ui-input-and-accessibility.md#fabrication-and-transactional-ui) | held/stale/double tests |
| TR-UI-005 | No required distinction depends on color/audio/haptic/motion/flash/hover/hold alone. | [UI](./60-ui-input-and-accessibility.md#accessibility-invariants) | accessibility matrix |
| TR-AST-001 | Every packaged external asset has accepted provenance/license and generated attribution. | [Assets](./80-asset-pipeline-and-budgets.md#license-and-provenance-ledger) | 100% coverage report |
| TR-AST-002 | Runtime 3D interchange is normalized glTF 2.0 GLB with pinned derivation/import settings. | [Assets](./80-asset-pipeline-and-budgets.md#3d-interchange-and-tools) | clean deterministic import |
| TR-AST-003 | Actual imported assets meet category geometry/material/texture/animation budgets. | [Assets](./80-asset-pipeline-and-budgets.md#3d-asset-categories-and-initial-enforceable-budgets) | manifest/import audit |
| TR-AST-004 | M2–M4 UI uses pinned Atkinson Hyperlegible Next Regular/Medium/Bold with retained license and glyph/layout validation. | [Assets](./80-asset-pipeline-and-budgets.md#font-policy) | font manifest/license/glyph/screenshot matrix |
| TR-AST-005 | M2–M4 input glyphs use the pinned CC0 Kenney Input Prompts 1.5a target subset through semantic action mapping. | [Assets](./80-asset-pipeline-and-budgets.md#2d-ui-and-icon-assets) | asset/license/action/glyph matrix |

## Autonomous execution and cross-component contracts

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-AGT-001 | Agents implement accepted and provisional baselines without preference-seeking; provisional work runs its named proof gate. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#default-mandate) | task-brief/evidence audit |
| TR-AGT-002 | Unspecified reversible internal choices follow the deterministic local-choice hierarchy. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#deterministic-local-choice-defaults) | handoff rationale and architecture checks |
| TR-AGT-003 | Only Ready tasks start, and work-package dependencies are satisfied only by Done evidence. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#work-states-and-integration) | work-state/dependency validator |
| TR-AGT-004 | Every implementation task emits a complete deterministic evidence bundle beyond compilation. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#required-evidence-bundle) | `SCH-OBS-003` fixtures and CI artifact |
| TR-AGT-005 | Agents perform the fixed authority-through-handoff self-review before declaring completion. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#self-review-sequence) | evidence checklist audit |
| TR-AGT-006 | Failures are classified and never hidden by unchanged retry, disabled gates, looser tolerances, or blind golden updates. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#failure-and-retry-policy) | injected-failure/CI-policy tests |
| TR-AGT-007 | Human escalation is limited to the explicit authority boundary and stops only the affected slice. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#explicit-escalation-boundary) | blocker-dossier/work-queue audit |
| TR-AGT-008 | Time-sensitive external facts and dependency choices use authoritative sources, stable compatible versions, pins, provenance, and offline-safe failure. | [Autonomous execution](./114-autonomous-agent-execution-protocol.md#external-research-and-dependency-defaults) | tool/dependency ledger audit |
| TR-CTR-001 | Project dependencies follow the accepted pure/domain/Godot direction and are architecture-tested. | [Contract registry](./115-component-contract-and-schema-registry.md#accepted-project-boundary) | project-reference architecture tests |
| TR-CTR-002 | Every mutable state has exactly one registered writer; other components use commands, immutable values, or rebuildable caches. | [Contract registry](./115-component-contract-and-schema-registry.md#mutable-state-ownership-matrix) | ownership and mutation tests |
| TR-CTR-003 | Cross-boundary contracts have registered producer, consumer, delivery/order, failure, and normative ownership. | [Contract registry](./115-component-contract-and-schema-registry.md#cross-boundary-contract-registry) | registry coverage/contract fixtures |
| TR-CTR-004 | Cross-boundary payloads are immutable and versioned where durable or compatibility-sensitive. | [Contract registry](./115-component-contract-and-schema-registry.md#cross-boundary-contract-registry) | mutation/version fixtures |
| TR-CTR-005 | Consumers do not implement against guessed Draft/Active contracts; producer schemas/fakes/tests land first or atomically. | [Contract registry](./115-component-contract-and-schema-registry.md#contract-change-rules) | dependency/evidence review |
| TR-CTR-006 | Component, contract, and schema IDs are unique, indexed, and referenced by work-package task briefs. | [Contract registry](./115-component-contract-and-schema-registry.md#verification) | registry/document validator |

## Persistence, quality, and release

| ID | Requirement | Normative source | Primary evidence |
| --- | --- | --- | --- |
| TR-PST-001 | Game remains fully playable/savable offline without Steam/backend. | [TDR-004](./decisions/TDR-004-use-an-offline-first-client-without-a-game-backend.md) | offline packaged flow |
| TR-PST-002 | Profile/settings use versioned JSON, atomic replace, checksums, and rotating backups. | [Persistence](./70-persistence-and-platform-services.md#atomic-write-protocol) | fault injection |
| TR-PST-003 | Save migrations are sequential, one-way, validated, and preserve pre-migration archives. | [Persistence](./70-persistence-and-platform-services.md#migrations) | every-version golden fixtures |
| TR-PST-004 | Run recovery restores a compatible run paused without time/resource duplication. | [Persistence](./70-persistence-and-platform-services.md#run-recovery) | round-trip continuation test |
| TR-PST-005 | Divergent cloud profiles require preserved user choice, not unsafe field merge. | [Persistence](./70-persistence-and-platform-services.md#steam-cloud-conflict-policy) | conflict matrix |
| TR-PST-006 | Local artifacts use the exact owned layout, canonical JSON policy, and versioned Brotli recovery format without unsafe path derivation. | [Persistence](./70-persistence-and-platform-services.md#local-file-layout-and-encoding) | path/codec/retention/package fixtures |
| TR-PLT-001 | Steam integration uses pinned Steamworks.NET Standalone 2025.164.1/SDK 1.64 solely behind the platform adapter. | [TDR-008](./decisions/TDR-008-use-steamworks-net-behind-the-platform-adapter.md) | dependency/package/architecture tests |
| TR-PLT-002 | Steam initialization/callback/shutdown failure degrades to the local path and never mutates gameplay directly. | [Persistence](./70-persistence-and-platform-services.md#steam-platform-adapter) | callback/offline/lifecycle matrix |
| TR-PLT-003 | Matching Valve native redistributables ship by target while development App ID hints never enter release packages. | [TDR-008](./decisions/TDR-008-use-steamworks-net-behind-the-platform-adapter.md#consequences) | Windows/Linux package inventory |
| TR-QUA-001 | Every work item includes automated evidence beyond compilation. | [Verification](./91-verification-strategy.md#acceptance-evidence) | task/CI evidence |
| TR-QUA-002 | Randomized failures emit seed/version and are directly reproducible. | [Verification](./91-verification-strategy.md#determinism-and-fixture-policy) | failing-fixture output |
| TR-QUA-003 | Required tests cannot be made green through retries or unowned quarantine. | [Verification](./91-verification-strategy.md#flake-policy) | CI policy audit |
| TR-QUA-004 | Every work package owns a stable machine-readable verification registry connecting requirements to selectors/scenarios and evidence. | [Verification](./91-verification-strategy.md#verification-registry) | registry/coverage validator |
| TR-OBS-001 | Performance captures percentiles, budgets, counts, allocation, and worst-frame timelines. | [Observability](./90-performance-diagnostics-and-observability.md#frame-metrics) | PERF reports |
| TR-OBS-002 | Development tools are typed/logged and absent from Release. | [Observability](./90-performance-diagnostics-and-observability.md#development-overlay-and-controls) | release scan/test |
| TR-BLD-001 | Tool/dependency versions are exact pinned and locked. | [Build](./100-build-dependencies-and-release-operations.md#toolchain-pinning) | clean locked build |
| TR-BLD-002 | CI imports and exports from a clean checkout without editor/global-cache dependence. | [Build](./100-build-dependencies-and-release-operations.md#continuous-integration) | clean runner build |
| TR-BLD-003 | Release excludes source assets, tests, development content/tools, credentials, and secrets. | [Build](./100-build-dependencies-and-release-operations.md#godot-import-and-export) | package inventory/security gate |
| TR-BLD-004 | Release artifacts include checksums, identity, notices, SBOM, logs, and separate symbols. | [Build](./100-build-dependencies-and-release-operations.md#artifacts) | artifact manifest |
| TR-BLD-005 | Root shell/PowerShell wrappers expose the exact noninteractive standard command surface and emit structured artifacts. | [Build](./100-build-dependencies-and-release-operations.md#standard-command-surface) | command contract matrix |
| TR-BLD-006 | Repository/project directories follow the accepted ownership layout and clean builds never depend on alternate search paths. | [Build](./100-build-dependencies-and-release-operations.md#repository-structure) | layout/architecture/clean-build tests |

## Usage

Work items and tests cite the relevant `TR-*` IDs. A requirement is not “done” globally because one test exists; it remains enforced by all listed subsystem and milestone gates throughout development.
