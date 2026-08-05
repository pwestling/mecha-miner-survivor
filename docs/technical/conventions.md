---
doc_id: TDD-CONVENTIONS
title: Technical Documentation Conventions
status: active
authoritative: true
---

# Technical Documentation Conventions

## Canonical format

Markdown is the specification source of truth. Mermaid diagrams are preferred for architecture and lifecycle diagrams because they remain text-reviewable. Runtime game content uses the strict JSON pipeline defined in [Content Data and Validation](./40-content-data-and-validation.md); CSV is limited to generated analysis/reporting, and Godot resources are integration artifacts rather than an alternate authoring source. Tool configuration may use the format required by its owning tool.

## Requirement sources and precedence

Technical requirements use this precedence order:

1. Explicit player-visible rules in the [Gameplay Specification](../README.md).
2. Accepted technical decision records.
3. Normative subsystem contracts in this directory.
4. Provisional baselines clearly labeled for prototype validation.
5. Local implementation choices that do not contradict the above.

When two requirements conflict, implementation stops until the conflict is resolved in the higher-authority source. Code behavior is never treated as the specification merely because it already exists.

## Certainty

- **Accepted:** explicitly approved or mechanically required by an accepted product decision.
- **Provisional:** the working technical baseline; implementation may depend on it, but a named validation gate can revise it.
- **Proposed:** not yet authorized for implementation.
- **Open:** unresolved and recorded in [Technical Open Questions](./open-questions.md).
- **Out of scope:** deliberately excluded from the current architecture or delivery target.

Ordinary declarative prose and numerical baselines in a technical subsystem document are accepted and enforceable unless their section is explicitly labeled provisional. A provisional section names its validation gate and authorizes implementation of the stated baseline before that gate runs.

## Stable identifiers

- Technical documents: `TDD-<DOMAIN>`
- Technical decisions: `TDR-###`
- Technical open questions: `TOQ-###`
- Technical requirements: `TR-<DOMAIN>-###`
- Implementation work packages: `<DOMAIN>-###` as registered in document 110
- Concrete implementation tasks: `TASK-<WORK-PACKAGE>-###`, for example `TASK-FND-001-001`
- Architecture components: `CMP-<DOMAIN>-###`
- Cross-boundary contracts: `CTR-<DOMAIN>-###`
- Data schemas: `SCH-<DOMAIN>-###`
- Verification cases: `VER-<WORK-PACKAGE>-###`, for example `VER-SIM-005-001`

Identifiers are never reused. Renames preserve redirects or supersession notes.

Component, contract, and schema IDs are registered in the [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md). Package verification IDs are registered under `tests/verification/`; suites retain accepted scenario IDs such as `PERF-*` and `WB-*`, which verification entries reference instead of duplicating. Every stable verification ID remains unique and traceable to requirements.

## Normative language

- **Must / must not:** required for correctness or acceptance.
- **Should / should not:** expected default; deviation requires documented rationale.
- **May:** optional within the stated boundary.

Every important contract states ownership, input, output, timing, failure behavior, thread or frame affinity, persistence, and verification method. Units, coordinate spaces, clock domains, rounding, ordering, and allocation behavior are explicit wherever ambiguity could cause divergent implementations.

## Traceability

Each subsystem document links to the gameplay documents and decisions it implements. Each technical requirement links to at least one verification method: automated test, schema validation, generated-map audit, performance benchmark, visual capture, device test, or manual acceptance scenario.

The technical specification does not duplicate large gameplay catalogs. It references their stable identifiers or source tables and defines how they are represented, validated, and consumed.

## Decision discipline

A TDR is required when a choice:

- constrains several subsystems;
- is expensive to reverse;
- selects a foundational dependency, platform, persistence format, threading model, or delivery mechanism;
- deliberately rejects a credible alternative; or
- introduces a non-obvious performance or reliability tradeoff.

Routine class decomposition does not need a TDR when it follows an accepted subsystem contract.

Autonomous agents use the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md) to distinguish authorized local choices from genuine escalation. Provisional baselines authorize implementation and their named validation; they are not unresolved questions.

## Completeness test

A technical area is complete when an implementation agent can determine:

- what components exist and which component owns each state;
- which APIs, events, schemas, and lifecycle transitions connect them;
- what runs on which clock, frame phase, process, and thread;
- how pause, failure, cancellation, shutdown, and invalid data behave;
- what is persisted and how versions migrate;
- what performance and memory budget applies;
- how the area is tested, observed, debugged, and accepted;
- which dependencies must exist first; and
- which details remain intentionally local implementation choices.
