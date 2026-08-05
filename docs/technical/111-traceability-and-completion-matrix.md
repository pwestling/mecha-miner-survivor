---
doc_id: TDD-TRACEABILITY
title: Traceability and Completion Matrix
status: active
authoritative: true
---

# Traceability and Completion Matrix

## Purpose

This matrix tells implementers where each gameplay domain is implemented, where it is verified, and which delivery work consumes it. It prevents an agent from treating a visible rule as belonging only to UI or only to simulation.

## Gameplay-to-technical matrix

| Gameplay source | Primary technical owners | Required verification/evidence |
| --- | --- | --- |
| [00 Game Vision](../00-game-vision.md) | foundation, runtime, implementation plan | M3/M4 player-loop acceptance and no-XP/content audit |
| [10 Core Game Loop](../10-core-game-loop.md) | runtime, simulation, encounters, mining/progression, UI | shortened and full end-to-end seeded runs |
| [20 Run Structure](../20-run-structure-and-timing.md) | runtime, simulation, encounters, persistence, UI | clock/pause/boss/terminal/settlement fixtures |
| [30 Combat, Weapons, Movement, Camera](../30-combat-weapons-movement-camera.md) | geometry, combat, presentation, UI | movement/automatic-attack/camera/slot tests and captures |
| [31 Alien and Boss Roster](../31-initial-alien-roster.md) | content, encounters, combat, assets/audio | every profile/ability/telegraph/performance fixture |
| [32 Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md) | content, encounters, observability | exact 35-row compile, population/formation/beacon full-run tests |
| [35–36 Playable Mechs](../35-playable-mechs.md) | content, modifiers, presentation, UI | six selection/trait/signature/profile-validity matrix |
| [40 Mining and Extraction](../40-mining-and-extraction.md) | mining/progression, geometry, encounters, UI/audio | all state, payout, resonance, beacon, pause, comprehension cases |
| [50 Maps and Navigation](../50-maps-resources-and-navigation.md) | generation, geometry, presentation, UI | discovery/fog/radar/map/waypoint/rock audits |
| [51 Map Generation Contract](../51-standard-map-generation-contract.md) | generation, geometry, content | hard validator/property/batch/performance reports |
| [60 Resources, Crafting, Progression](../60-resources-crafting-progression.md) | progression, content, persistence, UI | recipe/profile/transaction/settlement and end-to-end tests |
| [61 Resource Identities](../61-specialized-resource-identities.md) | content, assets, presentation, audio, UI | identity manifest, grayscale/color/audio/accessibility matrix |
| [62 PowerUp Catalog](../62-permanent-powerup-catalog.md) | content, modifiers, persistence, UI | costs/caps/stacking/refund/max-envelope tests |
| [63 Option Unlock Catalog](../63-permanent-option-unlock-catalog.md) | content, persistence, profile generation, UI | ownership/cost/pool expansion/nonrefundable tests |
| [65 Weapon Stat/Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md) | combat, progression, content, UI | formula/depth/branch/slot/preview fixtures |
| [66 Weapon Catalog/Graph](../66-weapon-catalog-and-resource-graph.md) | content, combat, generation | 15-edge graph and registered behavior coverage |
| [67 Mech Relics](../67-mech-relics.md) | progression, combat, UI | cache/install/sell/replace and compatibility tests |
| [68 Utility Catalog](../68-utility-catalog.md) | content, modifiers, progression, UI | every utility/rank/profile/interaction fixture |
| [69 Relic Catalog](../69-initial-relic-catalog.md) | content, combat, UI/presentation | ten hook policies, weapon matrix, live-state meters |
| [70 Balance Framework](../70-combat-and-economy-balance-framework.md) | combat, observability, verification | WB-01–WB-06 and tuning reports |
| [71 Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md) | content, combat | exact arithmetic, all base/branch benchmarks |
| [72 Survivability Baseline](../72-player-survivability-and-damage-baseline.md) | simulation, combat, encounters, observability | damage/control/recovery/rock/failure-margin capture |
| [73 Interface and Screen Flow](../73-interface-screen-flow-and-information-architecture.md) | UI/input, persistence, presentation/audio | route/focus/controller/responsive/usability matrices |

## Technical completion matrix

| Technical document | Implementation packages | Completion signal |
| --- | --- | --- |
| [00 Foundation](./00-technical-foundation.md) | FND-001–FND-006 | pinned clean builds/exports and spike gates |
| [10 Runtime](./10-runtime-architecture.md) | SIM-001–SIM-009 | headless lifecycle/pause/state runner |
| [20 Simulation Core](./20-simulation-core.md) | SIM and PLY packages | invariant/ordering/transaction/snapshot suites |
| [21 Geometry](./21-world-geometry-navigation-and-spatial-queries.md) | GEO-001–GEO-008 | reference differential tests and budgets |
| [22 Combat](./22-combat-and-weapon-runtime.md) | COM-001–COM-011 | complete catalog and modifier benchmark matrix |
| [23 Encounters](./23-encounter-director-and-enemy-runtime.md) | ENC-001–ENC-009 | full schedule/boss/population stress |
| [24 Mining/Progression](./24-mining-fabrication-and-progression-runtime.md) | MIN/PRG packages | end-to-end resource/transaction reconciliation |
| [30 Presentation](./30-presentation-and-rendering.md) | PRE packages | representative assets/readability/PERF-04 |
| [31 Audiovisual](./31-audiovisual-feedback.md) | AUD packages | critical redundancy/voice/pause/accessibility tests |
| [40 Content](./40-content-data-and-validation.md) | DAT packages | canonical validated bundle and reports |
| [50 Generation](./50-procedural-map-generation.md) | MAP packages | zero invalid published manifests across batch |
| [60 UI/Input](./60-ui-input-and-accessibility.md) | UI packages | complete controller/responsive/accessibility flow |
| [70 Persistence](./70-persistence-and-platform-services.md) | PST/PLT packages | fault/migration/recovery/cloud matrix |
| [80 Assets](./80-asset-pipeline-and-budgets.md) | AST packages | 100% license/import/budget/readability coverage |
| [90 Observability](./90-performance-diagnostics-and-observability.md) | FND-007/008, QUA packages | reproducible metrics/diagnostic/perf reports |
| [91 Verification](./91-verification-strategy.md) | all packages, OPS-001 | required suites stable at all CI tiers |
| [100 Build/Release](./100-build-dependencies-and-release-operations.md) | FND/OPS/PLT | immutable tested Windows/Linux/Steam artifacts |
| [110 Implementation Plan](./110-implementation-plan-for-ai-agents.md) | all packages | M0–M7 gates and evidence complete |
| [112 Requirement Index](./112-normative-requirement-index.md) | all packages | every task/test cites applicable stable requirements |
| [113 Risk Register](./113-technical-risk-register.md) | all milestone/risk-owning packages | each trigger checked and response evidence retained |
| [114 Autonomous Execution](./114-autonomous-agent-execution-protocol.md) | FND-010, all packages | Ready/Done/evidence/escalation protocol enforced |
| [115 Contract Registry](./115-component-contract-and-schema-registry.md) | FND-009, all contract-owning packages | unique registry, dependency, ownership, lifecycle tests pass |

## Explicitly deferred product/content decisions

The architecture supports but does not silently decide:

- final art palette, typography, models, icons, sound, music, animation style, and narrative frame;
- exact onboarding script and any difficulty/accessibility assists that change gameplay;
- shipped localization list;
- ultrawide gameplay framing beyond safe support;
- final Windows minimum hardware chosen from measured equivalents;
- achievements, leaderboards, workshops, daily challenges, remote content, mods, multiplayer, mobile, console, macOS release, or web;
- external analytics/crash upload; and
- post-initial catalogs, modes, maps, or biome themes.

These are not technical blockers for M4 or core implementation. Agents use specified representative defaults and automated acceptance gates, record later human observations without waiting, and do not expand the product scope. If a deferred feature is accepted later, it receives gameplay decisions and technical work packages before implementation.

## Specification readiness conclusion

The foundational architecture has no unresolved technical decision requiring owner input. A fresh repository agent can discover authority through `AGENTS.md`, select `TASK-FND-001-001`, determine sole component/contract ownership, make reversible local decisions through a fixed tie-breaker, close work with registered verification/evidence, and select successor work from the dependency graph without preference review.

Remaining changes should arise from measured prototype, performance, usability, balance, asset, or product-scope evidence and be recorded as tuning, content work, or a superseding TDR—not improvised during implementation. Human intervention is reserved for the explicit escalation boundary in document 114, not ordinary engineering or representative internal-demo production.
