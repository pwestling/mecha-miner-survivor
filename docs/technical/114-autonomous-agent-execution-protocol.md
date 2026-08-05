---
doc_id: TDD-AUTONOMOUS-EXECUTION
title: Autonomous Agent Execution Protocol
status: active
authoritative: true
---

# Autonomous Agent Execution Protocol

## Purpose

This document defines how coding agents turn the specification into a working game with minimal human intervention. It removes preference-seeking from ordinary implementation, fixes the decision and escalation rules, defines task readiness and evidence, and gives agents deterministic responses to ambiguity, failure, performance pressure, and incomplete production assets.

This protocol governs implementation behavior. It does not authorize an agent to change the player-visible game, publish externally, use credentials, accept legal risk, or bypass a higher-authority requirement.

## Default mandate

An implementation agent is expected to proceed without asking for confirmation when all of the following are true:

- the requested work package and its prerequisites are clear;
- the change remains inside accepted gameplay and technical boundaries;
- the decision is local, reversible, and testable;
- no new third-party dependency, license exception, platform, service, or persistent incompatibility is introduced; and
- the agent can produce the required automated evidence.

Declarative subsystem prose is accepted under [Technical Documentation Conventions](./conventions.md). A **provisional** baseline is also authorization to implement that baseline and run its named validation gate; provisional does not mean “ask before starting.”

## Decision routing

Apply this table from top to bottom. The first matching row controls.

| Situation | Autonomous action | Required record |
| --- | --- | --- |
| Explicit gameplay or technical rule exists | Implement it exactly | cited requirement IDs and tests |
| Sources appear to conflict | Apply the documented precedence; if the higher source is unambiguous, correct the lower technical summary in the same task | corrected link/diff and regression evidence |
| A provisional baseline has a named proof gate | Implement the baseline, instrument it, and run the gate | proof result and keep/change conclusion |
| Player-visible detail is absent but covered by DEC-096 | Apply the closest *Vampire Survivors* precedent consistent with project overrides | source decision plus concise mapping note |
| Internal detail is unspecified and reversible | Use the local-choice defaults below | ordinary task handoff; no TDR |
| Two valid local choices remain | Select with the deterministic tie-breaker below | one-sentence rationale |
| An optional feature is not accepted | Do not implement it | list as non-goal only if relevant |
| A dependency or asset license is unclear | Reject that candidate and choose an allowed alternative | rejected candidate in acquisition/dependency log |
| A test exposes an implementation defect | Fix the owning layer and rerun affected gates | before/after evidence |
| A benchmark exceeds a sub-budget | Follow the performance response ladder | profile and measured comparison |
| The only fix changes accepted player-visible behavior or a foundational TDR | Stop only the affected slice and emit a blocking dossier | exact conflict, evidence, options, recommendation |

Lack of personal preference, uncertainty between equivalent names, or a desire for aesthetic confirmation is not a blocker.

## Deterministic local-choice defaults

When several implementations satisfy the same contract, prefer the first candidate that meets every higher row in this order:

1. Preserves correctness, determinism, pause, persistence, accessibility, and target-device budgets.
2. Uses an existing project mechanism without adding a dependency or architectural concept.
3. Keeps domain rules in a pure C# owner and Godot code at the presentation/integration edge.
4. Has the smallest public surface and fewest mutable owners.
5. Is directly testable without engine scenes, wall-clock sleeps, network access, or global machine state.
6. Is simpler to remove or replace and does not alter saves/content formats.
7. Produces stable ordering and diagnostics naturally.
8. Uses less allocation, C#↔Godot traffic, scene-tree population, or asset memory in a hot path.
9. Requires less code and fewer concepts after tests and failure handling are included.
10. If still tied, choose the lexically first stable ID or the first candidate in the authoritative catalog; never use incidental filesystem, hash-map, or discovery order.

### C# and domain defaults

- Types and members are non-public unless a documented cross-project contract requires visibility.
- Dependencies are passed explicitly through constructors or method inputs. Use a small manual composition root; do not add a dependency-injection container, service locator, or mutable global registry.
- Prefer immutable value records at boundaries and narrowly owned mutable stores inside hot systems.
- Use arrays or contiguous lists for ordered iteration; dictionaries are lookup indexes and never define authoritative order.
- Expected rejection uses typed result/reason data. Exceptions are reserved for violated invariants, invalid startup/build data, or unrecoverable infrastructure failure.
- Nullable state is explicit. Absence uses a defined optional/result representation; collections return empty rather than null.
- Asynchrony is limited to I/O or isolated immutable background jobs. It carries cancellation, has an owning lifetime, and commits through the main-thread boundary.
- Create a shared abstraction only after at least three concrete uses expose the same lifecycle and semantics, unless a specification already requires the abstraction.
- Optimize a hot path only with a benchmark or profiler trace; retain or add a simple reference implementation in tests when optimization obscures correctness.

### Godot and presentation defaults

- Scenes are composition and presentation artifacts, not alternate gameplay authorities.
- Favor explicit node references established during scene initialization; do not repeatedly search the scene tree in active frames.
- Repeated runtime visuals use pools, MultiMesh/server APIs, or batches according to the presentation contract; ordinary entities do not acquire one-off nodes for convenience.
- Missing or saturated noncritical presentation uses the documented fallback proxy and emits one rate-limited diagnostic.
- Animation follows simulation state. Animation callbacks may request presentation-only effects but never commit gameplay.
- UI derives from immutable view models and emits typed intents. Widget-local state is limited to focus, selection, scroll, transient animation, and uncommitted editing.

### Naming and file-placement defaults

- Use the stable gameplay/technical ID in fixtures, diagnostics, and generated artifacts; use descriptive English type names in code.
- Place a type with the project that owns its semantics, not the project that happens to call it first.
- One file contains one primary public/internal type unless tightly coupled private value types improve navigation.
- Test files mirror the production namespace/domain and name the behavior and condition being proved.
- Shared constants live with their owning rule or compiled content, never in an unrelated convenience file.

### External research and dependency defaults

- Verify time-sensitive engine, SDK, package, platform, and license facts against official documentation, first-party repositories, or the authoritative package registry at task time; do not rely on recalled versions or unsourced tutorials.
- When the specification fixes a version, use exactly that version. When it fixes only a compatible family, choose the newest non-prerelease version compatible with all pinned targets and immediately pin it and its transitive graph.
- Prefer .NET/Godot capabilities, then small project-owned code, then one narrow maintained package. “Less code initially” does not beat a new runtime dependency's license, platform, supply-chain, or exit cost.
- Record source URL, retrieval date, version/hash/signature, license, compatibility evidence, and rejection reason for alternatives in the dependency/tool/asset ledger.
- Never substitute a preview/nightly/unofficial binary because a stable download is inconvenient.
- If network access is unavailable, use verified repository caches. If the required artifact is absent, classify only that task as environment-blocked and continue independent offline work; do not silently change versions or fetch from an untrusted mirror.

## Work selection and decomposition

### Ready-work algorithm

An autonomous planner selects work using this order:

1. Exclude packages whose hard dependencies are not complete.
2. Prefer the earliest incomplete milestone.
3. Within that milestone, prefer contract/schema/harness work required by multiple consumers.
4. Then prefer a vertical path needed for the next executable milestone over catalog breadth.
5. Then prefer work retiring the highest open risk in the [Technical Risk Register](./113-technical-risk-register.md).
6. Break remaining ties by the numeric work-package ID.

Do not start a consumer against a guessed future contract merely to increase parallelism.

### Task-size rule

A work package is an epic. Concrete tasks use `TASK-<WORK-PACKAGE>-###` IDs that are never reused. Split a package until one task has:

- one observable behavior or contract outcome;
- one primary mutable owner;
- a nonoverlapping file scope;
- prerequisites that already exist;
- evidence runnable independently;
- no requirement for a temporary second implementation of domain truth; and
- a repository state that remains buildable when merged.

Split contract definition, consumer integration, catalog breadth, optimization, and production-asset replacement into separate tasks when each can close independently. Do not split a transaction across tasks if doing so would temporarily permit resource loss, duplication, or partial commit.

## Work states and integration

| State | Meaning | Exit condition |
| --- | --- | --- |
| Draft | objective exists but authority, scope, or evidence is incomplete | required task brief is complete |
| Ready | dependencies and contracts are present; file ownership is free | agent starts against a recorded base revision |
| Active | one agent owns the task scope | implementation and self-verification complete |
| Evidence review | change is complete but independent/rerun evidence is being checked | all gates pass with no unexplained delta |
| Done | behavior, evidence, generated artifacts, and documentation agree | integration base contains the change |
| Blocked | one of the explicit escalation conditions prevents meaningful progress | blocker resolved or task respecified |

Only Done dependencies satisfy downstream package prerequisites. A task may prepare read-only analysis while waiting, but it must not commit consumer code against Draft or Active contracts.

For parallel work, one integration owner controls shared project/solution files, generated registries, and contract changes for that wave. Consumer agents return focused changes; the integration owner resolves cross-task conflicts according to ownership rather than merging both behaviors.

## Required evidence bundle

Each implementation task produces a machine-readable evidence summary at `artifacts/evidence/<task-id>/<build-id>/evidence.json` during local/CI execution. The artifact need not be committed, but CI retains it and the handoff reports its path and checksum.

The summary contains:

- task and parent work-package IDs;
- base and result source revisions;
- cited gameplay documents, TDRs, and `TR-*` requirements;
- changed ownership areas and cross-boundary contracts;
- exact command invocations and exit results;
- tests/fixtures run, counts, skips, and seed identities;
- generated reports, screenshots, maps, profiles, benchmarks, or packages with hashes;
- before/after performance values where relevant;
- warnings, known limitations, deferred successor IDs, and triggered risks; and
- an explicit statement that no unexplained warning, retry-masked failure, or unauthorized visible behavior change remains.

Evidence generators sort fields and paths, redact machine-private values, and return nonzero when required evidence is missing. Compilation alone never satisfies an evidence bundle.

## Self-review sequence

Before handoff, the implementing agent performs these passes in order:

1. **Authority:** map every changed behavior to its source and confirm no higher-authority conflict.
2. **Ownership:** confirm there is one mutable owner and no duplicated rule in UI/presentation/tools.
3. **Failure:** exercise invalid input, capacity, pause, cancellation, disposal, and partial-failure paths that apply.
4. **Determinism:** inspect iteration/order/RNG/tie behavior and record seeds.
5. **Persistence:** check versioning, atomicity, migration, and idempotency whenever durable or recoverable state is touched.
6. **Performance:** check allocations, counts, boundary traffic, and the relevant budget even when the task was not labeled optimization.
7. **Accessibility/presentation:** verify redundant cues and both reference layouts for any visible change.
8. **Repository:** run formatting, analyzers, affected tests, generated-file checks, and clean import/build as required by the task tier.
9. **Diff:** remove dead scaffolding, unrelated edits, stale comments, hidden TODOs, and accidental generated/binary changes.
10. **Handoff:** produce the evidence bundle and name the next Ready package(s).

## Failure and retry policy

Classify a failure before retrying:

| Class | Response |
| --- | --- |
| Deterministic test/content/build failure | Do not retry unchanged; diagnose and fix the owner |
| Random/property failure | Preserve seed and shrink/reproduce as a fixed regression case |
| Environmental/tool mismatch | Run the toolchain doctor; correct the pinned environment, not project behavior |
| External transient download/service failure | Retry once with bounded backoff; then preserve diagnostics and continue offline-capable work |
| Flaky timing/order failure | Treat as a defect; remove wall-clock/order dependence rather than increasing retries |
| Performance regression | Reproduce after warm-up, capture owner timings, then use the response ladder |
| Visual/screenshot delta | Compare authoritative geometry and intentional content first; never accept a new golden solely to make CI green |

An agent must not disable a gate, loosen a tolerance, edit a golden, suppress a warning, or reduce scenario pressure unless the underlying accepted behavior changed and the same task records why.

## Performance response ladder

When an accepted benchmark misses budget, apply these responses in order and remeasure after each relevant change:

1. Confirm the correct build, device, warm-up, scenario, content hash, settings, and instrumentation overhead.
2. Attribute the regression to per-system timings, allocations, counts, GPU passes, or boundary traffic.
3. Remove accidental work, allocation, duplicate queries, repeated conversions, or unnecessary C#↔Godot calls.
4. Improve locality, reuse, query filtering, pooling, batching, instancing, and update frequency where semantics permit.
5. Reduce or aggregate noncritical presentation according to its priority and quality policy.
6. Use documented LOD, VFX, shadow, material, and audio degradation without changing authoritative populations or outcomes.
7. Rebalance subsystem sub-budgets only if the total target passes with stable safety margin and no starvation risk.
8. If the target still fails, produce a TDR dossier comparing the smallest architectural alternatives. Do not silently reduce enemies, attacks, mining pressure, simulation rate, or accepted readability.

## Autonomous asset and presentation selection

Agents may acquire and select assets without aesthetic approval when all hard gates pass. Apply this ranking:

1. CC0/public domain before attribution licenses; reject unclear or prohibited licenses.
2. Existing coherent family before a visually stronger isolated asset.
3. Distinct top-down silhouette and gameplay readability before detail.
4. Existing rig/material/palette compatibility before adaptation cost.
5. Lower measured runtime/import cost before unused fidelity.
6. More complete provenance/source/editability before convenience.
7. If still tied, use the first stable logical asset ID and record alternatives in the contact sheet.

Canonical top-down contact sheets, footprint overlays, grayscale/color-vision variants, animation comparisons, and Steam Deck captures are the default autonomous review artifact. A candidate that fails is replaced or simplified; it is not escalated merely because a different aesthetic might also work.

## Provisional values and tuning

- Implement accepted and provisional numeric baselines exactly before tuning.
- A failed correctness test is not permission to rebalance content.
- Automated balance harnesses may identify outliers and produce a proposed data-only tuning patch when the gameplay adjustment order specifies the response.
- Technical agents may fix formulas, units, rounding, or data transcription to match the gameplay source without asking.
- Player-visible tuning changes outside an explicitly assigned tuning task remain proposals with before/after reports; they do not enter the implementation incidentally.
- Human playtesting remains valuable for feel, but lack of a playtest result does not block completion of a technically specified milestone or its deterministic scenarios.

## Specification maintenance autonomy

An agent updates technical documents in the same task when it:

- corrects a contradiction against a higher-authority source;
- makes a local implementation choice into a cross-component contract;
- changes a schema, lifecycle, failure mode, ordering, budget allocation, or verification method;
- adds a stable ID, work package, risk, or evidence gate; or
- discovers that the documented contract cannot be implemented as written.

Editorial clarification and mechanically necessary technical detail may be made autonomously when they preserve behavior. A new or reversed TDR, new player-visible behavior, destructive save policy, new service/platform, legal exception, or release action requires the corresponding authority described below.

## Explicit escalation boundary

Human input is required only for:

- a genuinely unresolved player-visible choice not covered by an accepted rule or DEC-096;
- changing an accepted player-visible rule for feel, balance, scope, monetization, privacy, or accessibility policy;
- accepting a new foundational dependency/engine/platform/backend or reversing a TDR after the response ladder is exhausted;
- accepting a license or legal exception outside the allowlist;
- using credentials, creating external accounts, publishing a build, changing a storefront/depot, or making another irreversible external-state change;
- intentionally discarding or incompatibly transforming user-owned persistent data; or
- a subjective production-art/narrative choice explicitly reserved in the gameplay open-question register.

When escalation is required, stop only the affected slice. Continue independent Ready work. The blocking dossier contains the exact authority conflict, reproduction/evidence, consequences of doing nothing, two or three bounded options, the recommended default, and the next work unlocked by the decision. Do not ask an open-ended preference question.

## Autonomous completion standard

An implementation task is complete without human confirmation when:

- its brief was Ready and all prerequisites were Done;
- every cited requirement and acceptance case passes;
- its evidence bundle is complete and reproducible from a clean checkout;
- contracts, generated data, tests, diagnostics, and documentation agree;
- relevant budgets pass or a nonexpired exception already authorized by this specification applies;
- no hidden TODO, skipped required test, unexplained warning, unowned generated change, or silent fallback remains; and
- downstream Ready work can be named from the dependency graph.

Subjective playtest findings may later reopen gameplay tuning, but do not retroactively make correctly evidenced engineering incomplete.

## Related documents

- [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md)
- [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md)
- [Normative Requirement Index](./112-normative-requirement-index.md)
- [Technical Risk Register](./113-technical-risk-register.md)
- [Verification Strategy](./91-verification-strategy.md)
- [Build, Dependencies, and Release Operations](./100-build-dependencies-and-release-operations.md)
