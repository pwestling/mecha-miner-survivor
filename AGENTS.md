# Mecha Miner Survivor — Agent Instructions

This repository is designed for autonomous AI implementation. Do not ask for routine preferences or invent player-visible behavior.

## Required reading order

Before changing implementation, read:

1. [`docs/README.md`](docs/README.md) for player-visible authority.
2. [`docs/technical/README.md`](docs/technical/README.md) for the technical map.
3. [`docs/technical/114-autonomous-agent-execution-protocol.md`](docs/technical/114-autonomous-agent-execution-protocol.md) for proceed/escalate, task, evidence, retry, and completion rules.
4. [`docs/technical/115-component-contract-and-schema-registry.md`](docs/technical/115-component-contract-and-schema-registry.md) for project boundaries and sole state ownership.
5. [`docs/technical/110-implementation-plan-for-ai-agents.md`](docs/technical/110-implementation-plan-for-ai-agents.md) for dependency-ordered packages and the concrete starting queue.
6. The exact gameplay, technical, TDR, `TR-*`, component, contract, schema, and verification sources cited by the assigned task.

If the repository has no implementation yet, the first task is `TASK-FND-001-001`. Select it without asking what to build first.

## Authority

Player-visible gameplay Markdown outranks technical decisions, which outrank subsystem contracts, provisional baselines, and local implementation choices. Apply the documented precedence when it resolves a conflict. Stop only the affected slice when a genuine higher-authority conflict remains.

Ordinary technical prose and numerical baselines are enforceable unless explicitly labeled provisional. A provisional baseline is permission to implement it and run its named proof gate.

## Nonnegotiable architecture

- Godot 4.7.1 .NET, C#, and Mobile renderer. No production GDScript.
- Pure `Content`, `Simulation`, and `Persistence` projects contain no Godot types.
- The simulation is the sole active-run authority, fixed at 60 Hz and serial initially.
- Godot scenes, UI, audio, and rendering consume immutable snapshots/events and never own gameplay rules.
- Every mutable state has one registered writer. Cross-boundary mutation uses typed commands or atomic transactions.
- Content and durable JSON are strict, typed, validated, canonical, versioned, and unknown-field rejecting.
- Stable ordering and the exact PCG32 stream contract govern authoritative randomness.
- The game remains fully playable and savable offline; Steam is an optional platform adapter.
- Do not add a dependency, language, framework, serializer, ECS, service locator, runtime reflection registry, backend, or platform without the specified decision process.

## Task execution

- Work only from a Ready `TASK-<WORK-PACKAGE>-###` brief with Done dependencies and explicit file/component ownership.
- Use the autonomous decision tie-breaker; reversible internal choices do not need approval.
- Do not start consumers against guessed contracts or edit another active task's scope.
- Update source, schemas, fixtures, generated reports, diagnostics, and technical documentation together when their contract changes.
- Never hand-edit generated output, blindly accept a golden, mask a failure with retries, loosen a threshold to pass, or reduce legal gameplay pressure for performance.
- Keep development scaffolding unmistakable, excluded from Release, and tied to a successor package.
- Preserve unrelated user changes and avoid destructive repository or filesystem operations.

## Standard workflow surface

Once FND-002 exists, use the root `build.sh`/`build.ps1` verbs defined in [`docs/technical/100-build-dependencies-and-release-operations.md`](docs/technical/100-build-dependencies-and-release-operations.md). CI uses the same verbs. Do not create competing workflow entrypoints.

## Completion

A task is Done only with registered `VER-*` coverage and a validating `SCH-OBS-003` evidence bundle containing authority, commands/results, seeds, artifacts, warnings, budget deltas, risks, and successor work. Compilation alone is never completion.

Human input is reserved for the explicit escalation boundary: genuinely unresolved player-visible choices, TDR reversal after evidence, legal exceptions, destructive persistent-data policy, credentials/publication/external state, or reserved subjective production scope. Continue independent Ready work when one slice is blocked.
