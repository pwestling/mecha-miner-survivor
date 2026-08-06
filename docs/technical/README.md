---
doc_id: TDD-INDEX
title: Technical Design Specification Index
status: active
authoritative: true
---

# Technical Design Specification

This directory is the canonical implementation specification for the game defined by the [Gameplay Specification](../README.md). It describes the architecture, contracts, data, tooling, budgets, verification, and delivery process required to build that design. It does not override player-visible behavior.

Repository-aware coding agents begin with the root [AGENTS.md](../../AGENTS.md), which routes every task into this specification and the autonomous execution protocol.

## Start here

1. Read [Technical Documentation Conventions](./conventions.md).
2. Read [Technical Foundation](./00-technical-foundation.md) for the accepted stack and platform boundary.
3. Consult the [Technical Decision Log](./decisions/README.md) when the reason behind an architectural rule matters.
4. Consult [Technical Open Questions](./open-questions.md) before making an assumption that could constrain the architecture or product.
5. Use the [Normative Requirement Index](./112-normative-requirement-index.md) for stable acceptance IDs and the [Technical Risk Register](./113-technical-risk-register.md) for early proof gates.
6. Apply the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md) for decision, escalation, evidence, and completion rules.
7. Use the [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md) to determine ownership before creating an interface or moving state.
8. Follow the subsystem documents below in order when planning implementation.

## Authority boundary

- The gameplay specification is authoritative for everything a player can perceive or experience.
- The technical specification is authoritative for implementation behavior that preserves that experience.
- A technical constraint that would change player-visible behavior must first be reconciled with the gameplay specification.
- Exact APIs, class names, and work-item ordering may evolve without changing the gameplay design, but recorded subsystem contracts and accepted technical decisions may not be silently bypassed.

## Specification map

| Order | Domain | Intended coverage | Status |
| --- | --- | --- | --- |
| 00 | [Technical foundation](./00-technical-foundation.md) | Engine, language, renderer, targets, dependency and version policy | Core contract established |
| 10 | [Runtime architecture](./10-runtime-architecture.md) | process boundary, composition root, lifecycle, clocks, pause, state ownership, events | Core contract established |
| 20 | [Simulation core](./20-simulation-core.md) | fixed-step transaction, entity storage, commands, modifiers, events, snapshots, capacity | Core contract established |
| 21 | [World geometry, navigation, and spatial queries](./21-world-geometry-navigation-and-spatial-queries.md) | planar geometry, movement, flow navigation, spatial index, collision, spawning, discovery | Core contract established |
| 22 | [Combat and weapon runtime](./22-combat-and-weapon-runtime.md) | weapon behaviors, targeting, damage, control, relic hooks, provenance, capacities | Core contract established |
| 23 | [Encounter director and enemy runtime](./23-encounter-director-and-enemy-runtime.md) | authored schedule, population, spawning, formations, enemy and boss behavior | Core contract established |
| 24 | [Mining, fabrication, and progression runtime](./24-mining-fabrication-and-progression-runtime.md) | mining state, payouts, transactions, radar, relics, settlement | Core contract established |
| 30 | [Presentation and rendering](./30-presentation-and-rendering.md) | scene boundary, snapshots, camera, crowd rendering, materials, VFX, quality | Core contract established |
| 31 | [Audiovisual feedback](./31-audiovisual-feedback.md) | event audio, music, haptics, captions, priority and voice budgets | Core contract established |
| 40 | [Content data and validation](./40-content-data-and-validation.md) | JSON authoring, schemas, identifiers, compilation, catalogs, localization | Core contract established |
| 50 | [Procedural map generation](./50-procedural-map-generation.md) | seeded profile/topology, placement solver, validation, fallback, audit tooling | Core contract established |
| 60 | [UI, input, and accessibility](./60-ui-input-and-accessibility.md) | view models, routing, focus, gamepad, maps, settings, responsive layouts | Core contract established |
| 70 | [Persistence and platform services](./70-persistence-and-platform-services.md) | profiles, settings, recovery, migrations, cloud conflicts, atomicity | Core contract established |
| 80 | [Asset pipeline and budgets](./80-asset-pipeline-and-budgets.md) | free-asset provenance, glTF/VAT, import validation, art/audio budgets | Core contract established |
| 90 | [Performance, diagnostics, and observability](./90-performance-diagnostics-and-observability.md) | frame/memory budgets, benchmarks, logs, metrics, debug tools | Core contract established |
| 91 | [Verification strategy](./91-verification-strategy.md) | test layers, coverage matrices, CI suites, evidence and flake policy | Core contract established |
| 100 | [Build, dependencies, and release operations](./100-build-dependencies-and-release-operations.md) | repository, toolchain, CI, exports, Steam, security, artifacts | Core contract established |
| 110 | [Implementation plan for AI agents](./110-implementation-plan-for-ai-agents.md) | agent contract, dependency graph, work packages, M0–M7 gates | Core contract established |
| 111 | [Traceability and completion matrix](./111-traceability-and-completion-matrix.md) | gameplay ownership, implementation packages, evidence, explicit deferrals | Core contract established |
| 112 | [Normative requirement index](./112-normative-requirement-index.md) | stable `TR-*` IDs linking requirements to evidence | Core contract established |
| 113 | [Technical risk register](./113-technical-risk-register.md) | early proofs, triggers, and bounded responses | Core contract established |
| 114 | [Autonomous agent execution protocol](./114-autonomous-agent-execution-protocol.md) | autonomous decision authority, work states, evidence, retries, self-review, escalation | Core contract established |
| 115 | [Component, contract, and schema registry](./115-component-contract-and-schema-registry.md) | logical ownership, project boundaries, cross-system payloads, schemas, lifecycle | Core contract established |

The specification is complete enough for implementation planning: the demo work breakdown follows directly from each subsystem's required components, contracts, tests, and acceptance gates.

## Ledgers

- [Technical Decision Log](./decisions/README.md)
- [Technical Open Questions](./open-questions.md)
- The gameplay [Decision Log](../decisions/README.md) remains the rationale ledger for player-facing rules.
