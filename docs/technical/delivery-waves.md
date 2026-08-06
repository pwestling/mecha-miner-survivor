---
doc_id: TDD-DELIVERY-WAVES
title: Parallel Delivery Waves and Stream Ownership
status: active
authoritative: false
---

# Parallel Delivery Waves and Stream Ownership

> **Non-normative coordination material.** This document adds no requirements and
> overrides nothing. It carves the already-accepted work packages into
> self-contained streams so that parallel agent sessions in separate threads can
> pick up work without colliding on files or contracts.
>
> The normative decomposition remains
> [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md).
> Every package ID, deliverable, dependency, and completion gate below is quoted
> from doc 110; where this document and doc 110 differ, **doc 110 controls and
> this document is wrong and must be corrected**. Ownership, boundaries, and
> contract IDs come from
> [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md).
> Decision authority, evidence, and escalation come from
> [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md).
>
> Nothing here relaxes the milestone gates. In particular doc 110's M0 rule still
> holds: **"No one creates gameplay systems during M0."**

## Integration ownership

Doc 114 § Work states and integration:

> For parallel work, one integration owner controls shared project/solution files,
> generated registries, and contract changes for that wave. Consumer agents return
> focused changes; the integration owner resolves cross-task conflicts according to
> ownership rather than merging both behaviors.

**This session is the integration owner.** It landed `FND-001` and owns the shared
surface for wave 0 and the wave boundaries after it.

### Integration-owner-only files

A consumer stream must not edit any of these. It **requests** the change from the
integration owner, which lands it as its own change with its own evidence, then
the consumer rebases.

| Shared surface | Why the integration owner holds it |
| --- | --- |
| `MechaMiner.sln` | every stream would otherwise add projects concurrently and conflict on one file |
| `Directory.Build.props`, `Directory.Build.targets` | repository-wide compiler, analyzer, nullable, warnings-as-errors, and determinism policy |
| `Directory.Packages.props`, `NuGet.config`, every `packages.lock.json` | exact locked dependency graph; doc 100 § Dependency policy requires the recorded dependency request before a package appears |
| `global.json` | pinned .NET SDK |
| `.editorconfig` | formatting and naming policy |
| `game/project.godot` | one engine configuration file that every presentation and UI stream would otherwise touch |
| `game/MechaMiner.Game.csproj`, every other `*.csproj` | project reference edges are the architecture boundary of doc 115 |
| generated registries and any generated artifact under `generated/` | doc 110: "Generated files are changed through their generator" |
| `tests/verification/*.json` for a package a stream does not own | `VER-*` IDs are never renumbered or reused |
| `docs/technical/**` normative documents | doc 114 § Specification maintenance autonomy still applies for a genuine contract correction, but a shared-contract edit is coordinated, "never an incidental edit hidden in a consumer task" (doc 110) |
| `build.sh`, `build.ps1`, `build/` scripts (`toolchain.json`, `bootstrap-*`, `verify-*`) | one workflow entrypoint; AGENTS.md: "Do not create competing workflow entrypoints" |
| `src/MechaMiner.Tools/Cli/`, `src/MechaMiner.Tools/Verbs/`, `src/MechaMiner.Tools/Toolchain/` | the verb table, exit classes, and diagnostic codes are the workflow contract every stream and every CI job reads. A stream adds its own tool code elsewhere under `src/MechaMiner.Tools/` and requests the verb registration |
| `tests/shared/` | the deterministic fixture utilities every test project links. Changing an observable behaviour here changes every stream's tests at once |
| `game/tests/` and the `MMG-RUNNER-REPORT` schema | the engine tier's runner and report contract. `W2-SHELL` owns `tests/MechaMiner.Game.Tests/` from wave 2, but the runner scene and the report shape stay coordinated |

Adding a top-level ownership directory is always an integration-owner change:
doc 100 § Repository structure requires the registry and the architecture tests to
be updated in the same task.

## Rules every stream follows

1. **Write the verification registry first.** Add the stream's
   `tests/verification/<work-package-id>.json` entries **before** implementing, per
   doc 91 § Verification registry. Entries are never renumbered; retired ones keep a
   tombstone and successor. At least one non-compilation verification per
   implementation task.
2. **Emit evidence.** Every task produces a validating `SCH-OBS-003` summary at
   `artifacts/evidence/<task-id>/<build-id>/evidence.json` (doc 114 § Required
   evidence bundle). Until `FND-010` lands the canonical emitter, produce the same
   field set by hand and record its path and checksum in the handoff. Compilation
   alone is never completion.
3. **One PR per task or tight task group.** Small, reviewable, and independently
   rerunnable from a clean checkout.
4. **Never edit another stream's owned scope.** Return a focused change and request
   the cross-scope edit.
5. **Never start a consumer against a contract that is not Done.** Doc 114: only
   Done dependencies satisfy downstream prerequisites, and "Do not start a consumer
   against a guessed future contract merely to increase parallelism." A stream may
   do read-only analysis while it waits.
6. **Leave the repository buildable** and the stream's own tests passing at every
   merge.
7. **Fix the smallest owning layer.** Do not compensate in an unrelated layer, and
   do not disable a gate, loosen a tolerance, or accept a golden to go green
   (doc 114 § Failure and retry policy).

### Branch and PR convention

The accepted documents impose no branch naming. **Local choice for this program,
not a requirement:**

- one branch per stream, named `claude/<stream-id>-<work-package>` - for example
  `claude/w1-sim-SIM-003`, `claude/w2-cat-DAT-007`;
- small PRs, one task or one tight task group each;
- on conflict the integration owner rebases consumers rather than merging both
  behaviors, which is the `RSK-017` first response in
  [Technical Risk Register](./113-technical-risk-register.md): "freeze consumer
  work; land one owner/contract and rebase consumers."

## Wave overview

| Wave | Theme | Parallelism | Starts when |
| --- | --- | --- | --- |
| 0 | Foundation and toolchain | integration owner only, sequential | now |
| 1 | Pure contracts | 2 streams | `FND-003` test harness is Done |
| 2 | Cores on top of contracts | 4-5 streams | the wave 1 contract it consumes is Done |
| 3 | Gameplay vertical slice | many streams | wave 2 cores are Done |
| 4 | Catalog breadth | widest | the wave 3 primitive it extends is Done |
| 5 | M4 integration, then M5 breadth, then M6/M7 | integration owner, then breadth | wave 4 |

The "Starts when" column states doc 110's formal gates verbatim and is not softened
anywhere in this document. What a later section reports is whether the surface behind a
gate is **landed and usable**, which is a different and weaker claim; § Two different
claims: landed, and Done below defines both and says which one each status line is
making. No package in this repository is Done yet.

---

## Wave 0 - foundation

**Integration owner only. Sequential. Blocks every other wave.** Owned scope is
the whole shared surface listed above, so no consumer stream can run concurrently
with it.

### Two different claims: landed, and Done

This document tracks two states that are easy to conflate. Conflating them is how it
previously came to assert completion for which no evidence exists, so they are defined
once here and used precisely below.

- **Landed** is an engineering claim about this branch. The code and its gates exist,
  they run from a clean checkout, and a consumer can build against them right now. It
  says nothing about merge status and nothing about evidence.
- **Done** is doc 114's formal work state, and it is stricter in two independent ways
  that wave 0 does not currently satisfy.
  1. Doc 114 § Work states gives Done the exit condition "integration base contains
     the change". Every `FND-001`, `FND-002`, and `FND-003` commit is still on an
     **open draft pull request**, so the integration base does not contain them.
  2. AGENTS.md § Completion and doc 114 § Required evidence bundle require, per task,
     "a validating `SCH-OBS-003` evidence bundle containing authority,
     commands/results, seeds, artifacts, warnings, budget deltas, risks, and successor
     work". `SCH-OBS-003` and its emitter/validator are `FND-010`'s deliverable, and
     `FND-010` has not started. No `artifacts/evidence/` path exists anywhere in this
     repository. So **no M0 task can be Done yet, however good its implementation
     is** - not for want of work, but because the instrument that measures completion
     has not been built.

The honest reading of wave 0 is therefore: **landed pending merge; engineering-ready
for consumers now; formally Done at `TASK-FND-005-002`**, the M0 close gate, once
`FND-010` can emit the bundle that gate's "M0 evidence bundle with no unexplained
warning or manual repair" refers to.

Where a section below says a wave or a step is "open" or "Ready", it means the surface
it consumes exists and is usable. That is an engineering-readiness judgement, which
this explicitly non-normative document is allowed to record. It does **not** mean
doc 110's completion gate has been passed, and it does not relax doc 114 § Work
states, which still controls: "Only Done dependencies satisfy downstream package
prerequisites." A stream that starts on readiness accepts that its base can still be
revised, and `RSK-017`'s first response above - "freeze consumer work; land one
owner/contract and rebase consumers" - is what covers that when it happens.

**Nothing gates any of this, including the correction that wrote it.** No script reads a
status line in this document and compares it with doc 114's exit conditions; there is no
`artifacts/evidence/` path to check a claim of Done against, which is the point item 2
above makes. Before this section existed the document asserted "`FND-001` is landed and
Done", "`FND-002` and `FND-003` are landed and Done", and "**Wave 1 is therefore open**"
on that basis, and every one of those sentences was false in the same way: they used
doc 114's word for a state whose instrument had not been built. That correction is
recorded in commit `a494f09` as "item 1" of a list whose numbering resolves to nothing -
see § Decision 12 - and it has no exit class before or after, because a document is
verified by reading it. The protection is that the two words are now defined in one place
and every status line below says which one it means; a status line that says "Done"
without satisfying both conditions above is a defect a reader has to catch.

### Step 1

| Package | Deliverable (doc 110) | Depends on | Completion gate | Owned file scope |
| --- | --- | --- | --- | --- |
| `FND-001` | Pin Godot/.NET versions, solution/project skeleton, repository layout, editor/analyzer settings | none | clean restore/build and version report | `MechaMiner.sln`, `global.json`, `Directory.Build.*`, `Directory.Packages.props`, `NuGet.config`, `.editorconfig`, `.gitignore`, every `*.csproj`, `game/project.godot`, `game/scenes/Boot.tscn`, `game/BootCompositionRoot.cs`, `build/bootstrap-linux.sh`, `build/verify-architecture.sh`, `build/verify-policies.sh`, `build/policy-fixtures/`, `tests/verification/FND-001.json` |

**`FND-001` is landed, not Done.** `TASK-FND-001-001`, `TASK-FND-001-002`, and
`TASK-FND-001-003` are committed on an open draft pull request, with
`tests/verification/FND-001.json` carrying thirteen implemented entries. The pinned
toolchain, solution skeleton, repository layout, and analyzer settings are what
`FND-002`, `FND-003`, and `FND-004` actually consume from it, and all of that is
present and usable, so those packages are unblocked on readiness. Their formal
prerequisite closes with the rest of M0 at `TASK-FND-005-002`.

### Step 2

| Package | Deliverable | Depends on | Completion gate | Owned file scope |
| --- | --- | --- | --- | --- |
| `FND-002` | Root wrapper/typed command host, doctor/bootstrap/format/build base verbs, and stable registration surface for later content/import/run/export owners | `FND-001` | implemented verbs run noninteractively and unavailable owner verbs return a typed nonzero status until their package lands | `build.sh`, `build.ps1`, `build/toolchain.json`, `build/verify-verbs.sh`, `build/verify-wrapper-parity.sh`, `build/verify-format.sh`, `build/verify-configurations.sh`, `src/MechaMiner.Tools/` command host, `Directory.Build.props` and `MechaMiner.sln` configuration sections, `tests/verification/FND-002.json` |
| `FND-003` | Pure NUnit test projects and Godot integration-test harness | `FND-001` | sample pure and engine tests pass headlessly | `tests/shared/`, the `Support/` and `Goldens/` subtrees of each pure test project, `tests/MechaMiner.Game.Tests/`, `game/tests/`, `build/verify-test-harness.sh`, `build/verify-godot-runner.sh`, `tests/verification/FND-003.json` |

`FND-002` and `FND-003` are independent of each other and may be two sessions, but
both are integration-owner scope because they touch the shared workflow entrypoint
and every test project. `FND-003` is the gate that opens wave 1.

**`FND-002` and `FND-003` are landed, not Done**, in one open draft pull request of
four task commits: `TASK-FND-002-001`, `TASK-FND-002-002`, `TASK-FND-003-001`,
`TASK-FND-003-002`. `tests/verification/FND-002.json` carries eighteen implemented
entries and `tests/verification/FND-003.json` twelve. Three of `FND-002`'s eighteen
(`VER-FND-002-016` through `018`) and the strengthened `VER-FND-001-005` exist because
an independent review found gates that could not fail; see Decision 11.

**Wave 1 is therefore open on engineering readiness.** The harness `W1-DAT` and
`W1-SIM` consume exists, runs headlessly from a clean checkout, and its contracts are
stable enough to build against, so `W1-DAT` (`DAT-001`) and `W1-SIM` (`SIM-001`,
`SIM-003`, `SIM-005`) may start. It is open because the harness exists - not because
`FND-003` passed its completion gate, which it has not. Read that sentence with
§ Two different claims above: a wave 1 stream is starting against a landed base on the
integration owner's judgement, and accepts a rebase if that base is revised before
`TASK-FND-005-002` closes M0.

What every stream now gets, and must use rather than reinvent:

| Surface | Where | Notes for consumers |
| --- | --- | --- |
| the eighteen verbs | `./build.sh`, `./build.ps1` | one entrypoint; neither wrapper branches on the verb, so parity is structural |
| the verb table | `src/MechaMiner.Tools/Cli/VerbRegistry.cs` | integration-owner scope. A stream that needs a verb implemented requests it; ten verbs are registered and return a typed nonzero status naming their owner |
| exit classes and diagnostic codes | `src/MechaMiner.Tools/Cli/ExitClass.cs`, `DiagnosticCodes.cs` | doc 100's eight classes, no `1`. New codes are appended, never renumbered |
| structured verb evidence | `artifacts/verbs/<verb>/<invocation>/result.json` | `MMT-VERB-RESULT`. `TASK-FND-010-002` maps this onto `SCH-OBS-003` |
| deterministic fixture utilities | `tests/shared/` | linked into all four test projects. Seed and identity logging, named tolerances, shrinking, goldens. See `tests/shared/README.md` |
| the engine tier | `game/tests/`, `tests/MechaMiner.Game.Tests/` | one runner scene, a JSON report contract. `W2-SHELL` owns `tests/MechaMiner.Game.Tests/` from wave 2; the runner and its report schema stay integration-owner scope |
| toolchain pins | `build/toolchain.json` | read by `doctor` and `bootstrap`. Adding a tool needs doc 100's dependency request |

Two conventions every stream must follow because the harness enforces them:

1. **Name every float tolerance.** `NumericAssert` has no overload that takes a bare
   epsilon, and `Tolerance.Named` rejects a blank name, a blank rationale, and a
   nonpositive magnitude. Doc 91: "'Approximately equal' without a named tolerance is
   not an acceptable test." `GEO-001` and `COM-003` own the central world-scale
   tolerance catalogue; until then each test names and justifies its own.
2. **A golden is never accepted to make a run green.** `GoldenText`'s update switch
   rewrites the golden and still fails. Review the diff against its authoritative
   source, commit the new golden deliberately, then rerun without the switch.

Two things the harness deliberately does not provide, so nobody builds on a guess:

- **Authoritative randomness.** `DeterministicCase` and `PropertyCase` seed
  `System.Random`. That is test-harness randomness. The exact PCG32 and SplitMix64
  stream contract is `SIM-005`'s, and its scripted test sources are what a gameplay
  test uses.
- **Build identity.** `HarnessIdentity` reports the harness identity and says
  `build-identity=pending:TASK-FND-004-001`. `FND-004` replaces it.

### Step 3

| Package | Deliverable | Depends on | Completion gate | Owned file scope |
| --- | --- | --- | --- | --- |
| `FND-004` | Build identity/version service and generated build manifest | `FND-001` | identity visible in tool/game test and diagnostics | build identity owner, `generated/` build manifest, `SCH-BLD-001` |
| `FND-007` | Structured logging, stable diagnostic codes, redaction, rotating local files | `FND-004` | schema/redaction/rate-limit tests pass | diagnostics logging owner (`CMP-OBS-001`), `CTR-OBS-001` |
| `FND-008` | Profiler marker/metric registry and benchmark report format | `FND-004` | sample CPU/count/allocation report produced | diagnostics metric owner, `SCH-OBS-002` |
| `FND-009` | Architecture dependency tests plus complete documentation/requirement/component/contract/schema/verification/work ID registry validator | `FND-001`, `FND-003` | forbidden project edges and missing/duplicate/dangling registry IDs/links fail fixtures | architecture tests, registry/document validation tooling, `SCH-QUA-001` validator |

**Step 3 is Ready on the same readiness basis.** `FND-004` depends only on `FND-001`,
and `FND-009` depends on `FND-001` and `FND-003`. All three are landed and usable, and
none of them is formally Done; see § Two different claims. Doc 110 makes `FND-007` and `FND-008`
depend on `FND-004`, so inside this step `FND-004` lands first and then
`FND-007`/`FND-008` can run in parallel with `FND-009`. `FND-004` also has two waiting
consumers already recorded in code: `HarnessIdentity` in `tests/shared/` says
`build-identity=pending:TASK-FND-004-001`, and `game/BootCompositionRoot.cs` names
`FND-004` as its first successor. `FND-009` replaces `build/verify-architecture.sh`'s reference-graph
assertions with real architecture tests (`TASK-FND-009-001`) and takes over
validating `tests/verification/*.json` (`TASK-FND-009-002`).

### Step 4

| Package | Deliverable | Depends on | Completion gate | Owned file scope |
| --- | --- | --- | --- | --- |
| `FND-005` | Initial CI fast suite with locked restore and artifact summaries | `FND-002`, `FND-003` | pull-request-equivalent job passes cleanly | CI configuration only |
| `FND-006` | Windows/Linux Godot export presets and local-platform adapter | `FND-001`, `FND-002` | packaged empty builds launch without Steam | `game/export_presets.cfg`, platform boundary |
| `FND-010` | Task evidence schema, deterministic emitter/validator, and CI artifact integration | `FND-004`, `FND-005`, `FND-007`, `FND-009` | complete/incomplete/redaction/reproducibility fixtures and sample retained artifact | evidence tooling/tests, `SCH-OBS-003` |

`FND-006` creates exactly the four preset names doc 100 § Godot import and export
requires: `Windows Development x86-64`, `Windows Release x86-64`,
`Linux/Steam Deck Development x86-64`, `Linux/Steam Deck Release x86-64`.

**`FND-005` and `FND-006` are both unblocked on readiness**: `FND-005` needs `FND-002`
and `FND-003`, `FND-006` needs `FND-001` and `FND-002`, and all three are landed.
`FND-005` also matters to the formal gate rather than only to CI: `FND-010` depends on
it, and `FND-010` owns the `SCH-OBS-003` bundle without which no M0 task can reach
Done. Three things they inherit rather than invent:

- `FND-005`'s CI job calls `./build.sh` verbs, never a script directly. The fast
  pull-request path is `bootstrap`, `format-check`, `build`, `test-fast`, `godot-import`;
  the main path adds `test-main`. This list previously omitted `godot-import`, which
  contradicted doc 91 § Fast pull-request suite ("Godot headless import and focused
  integration tests"); doc 91 is the authority and this document is non-normative
  coordination, so the omission was a defect here and is corrected rather than being
  read as narrowing the tier. The `content` verb belongs in the fast path as soon as
  `DAT-006` lands, and until then it exits 2 naming `DAT-006`, which is a legible CI
  failure rather than a silent gap. The focused integration tests doc 91 names alongside
  the import are still main-path only, because `test-main` is the verb that runs them.
- `FND-006` implements `export` and `run`, and inherits Decision 4's mapping: `development`
  builds `ExportDebug` and `release` builds `ExportRelease`. It also owns the export-preset
  exclusion for `game/tests/`, whose compile-time exclusion under `ExportRelease` already
  exists.
- `FND-006` records the Godot export-template hash in `build/toolchain.json` and moves it
  out of `deferred`, per Decision 7.

Then the M0 close gate, `TASK-FND-005-002`: "run and close the complete M0
clean-checkout/import/launch/export gate", hard dependency "all prior M0 tasks",
owned scope "integration configuration/evidence only", close evidence "M0 evidence
bundle with no unexplained warning or manual repair".

---

## Wave 1 - pure contracts

**The `FND-003` harness is landed and usable, so this wave is open on engineering
readiness now** - see § Two different claims; `FND-003` is not formally Done. Two
streams, no file overlap. These are the contract-first packages that every later wave
consumes, so nothing downstream may begin against them until each package is Done, and
that rule is not weakened by the readiness judgement that opened this wave: a wave 1
stream may start on a landed harness, but a wave 2 stream may not start on a wave 1
contract that is still moving.

### `W1-DAT` - content contracts

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `DAT-001` | Common definition envelope, stable-ID rules, schema infrastructure, diagnostic format | `FND-001`, `FND-003` | invalid/valid fixture suite |
| `DAT-002` | Resource, mech, enemy, boss, mining, encounter, map schemas and typed models | `DAT-001` | accepted initial definitions parse/validate |
| `DAT-003` | Weapon, branch, utility, relic, PowerUp, unlock schemas and typed models | `DAT-001` | graph/cardinality/price validators pass |
| `DAT-004` | Behavior/target/formula/modifier registry manifest and registration validator | `DAT-002`, `DAT-003` | unknown/duplicate/mismatched registrations fail |
| `DAT-005` | Cross-reference, semantic, analytical, localization, asset, and source-trace validators | `DAT-002`, `DAT-003` | complete invalid-fixture coverage |
| `DAT-006` | Canonical bundle compiler, hash, normalized defaults, deterministic ordering | `DAT-005` | source-order permutation yields identical hash |

- **Owned file scope:** `src/MechaMiner.Content/`, `tests/MechaMiner.Content.Tests/`,
  `content/schemas/`. Compiler/report entry points it needs inside
  `src/MechaMiner.Tools/` are requested from the integration owner while `FND-002`
  owns the command host.
- **Contracts it owns:** `CMP-CNT-001`, `CMP-CNT-002`; `CTR-CNT-001`,
  `CTR-CNT-002`; `SCH-CNT-001`, `SCH-CNT-002`, `SCH-CNT-003`, `SCH-CNT-004`.
- **Requirements:** `TR-DAT-001` through `TR-DAT-006`, applicable `TR-AST-*`.
- `TASK-DAT-001-001` is already decomposed in doc 110's M0 queue: "establish strict
  JSON codec, schema diagnostics, common envelope, and valid/invalid sample
  definition", owned scope "Content project and content schema/sample fixture only".
- Do not transcribe catalogs here. Content breadth is `W2-CAT`.

### `W1-SIM` - simulation contracts

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `SIM-001` | Fixed 60 Hz host, accumulator, catch-up limit, clock domains | `FND-003` | clock/edge/final-boundary fixtures |
| `SIM-002` | Pause-reason set, lifecycle, focus/suspend hooks | `SIM-001` | overlapping pause matrix |
| `SIM-003` | Generational entity IDs and packed category stores | `FND-003` | reuse/stale/capacity/property tests |
| `SIM-004` | Command admission, sequence/idempotency, paused transaction shell | `SIM-001`, `SIM-003` | command/atomic rejection fixtures |
| `SIM-005` | Exact PCG32 implementation, SplitMix64 child derivation, registered stream families, recovery state, scripted test sources | `FND-003` | golden vectors, bounded conversion, stable sequences, serialization, and independence tests |
| `SIM-006` | Domain/presentation event buffers, provenance, stable ordering | `SIM-003` | simultaneous/order/event-loss fixtures |
| `SIM-007` | Immutable/double-buffered presentation snapshot and view-model primitives | `SIM-003`, `SIM-006` | reconstruction/no-mutation tests |

- **Owned file scope:** `src/MechaMiner.Simulation/` runtime, clock, entity,
  command, event, snapshot, and RNG subtrees, plus their mirrors in
  `tests/MechaMiner.Simulation.Tests/`. Geometry inside
  `src/MechaMiner.Simulation/` belongs to `W2-GEO`; the two streams must agree the
  subdirectory split with the integration owner before both are active.
- **Contracts it owns:** `CMP-RUN-001`, `CMP-SIM-001`, `CMP-SIM-002`,
  `CMP-SIM-003`; `CTR-RUN-001`, `CTR-RUN-002`, `CTR-RUN-003`, `CTR-SIM-001`,
  `CTR-SIM-002`, `CTR-SIM-003`; `SCH-RUN-001`.
- **Requirements:** `TR-RUN-001` through `TR-RUN-010`, `TR-SIM-001`, `TR-SIM-002`,
  `TR-SIM-005`, `TR-SIM-006`, `TR-CTR-002`, `TR-CTR-004`.

### The wave 1/2 boundary

Two SIM packages are deliberately outside `W1-SIM`:

| Package | Deliverable | Depends on | Why it is a boundary item |
| --- | --- | --- | --- |
| `SIM-008` | Modifier graph, versions, flat/additive/branch/relic layers, snapshot/live fields | `DAT-003`, `SIM-003` | needs `W1-DAT`'s weapon/branch/relic models, so it cannot land inside `W1-SIM` |
| `SIM-009` | Headless simulation runner, step/advance/script/checksum/report | `SIM-001`-`SIM-008` | needs all of `W1-SIM` plus `SIM-008` |

They land together at the wave 1/2 boundary, owned by whichever of the two streams
finishes second, coordinated by the integration owner. `SIM-008` carries
`TR-SIM-003` and `TR-SIM-004`; `SIM-009` closes M1 with `ENC-002` and `PST-005`
downstream of it.

---

## Wave 2 - cores on top of contracts

Four to five parallel streams. Each starts only when the wave 1 package it
consumes is Done.

### `W2-GEO` - geometry, navigation, spatial queries

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `GEO-001` | Planar math, primitives, inclusive overlap, swept queries, terrain collision | `SIM-003` | brute-force reference comparison |
| `GEO-002` | Static geometry manifest and raster construction | `GEO-001`, `DAT-002` | connectivity/clearance fixtures |
| `GEO-003` | Player swept movement/slide and coordinate presentation adapter contract | `GEO-001` | movement/corner/boundary tests |
| `GEO-004` | Uniform spatial hash and allocation-free query API | `GEO-001` | randomized differential tests and budget |
| `GEO-005` | Flow-field navigation and ordinary movement integration | `GEO-002`, `GEO-004` | route/stuck/boundary/performance fixtures |
| `GEO-006` | Boss-clearance routing and re-entry candidate service | `GEO-002`, `GEO-005` | all boss footprints/routes pass |
| `GEO-007` | Camera footprint, spawn sectors, offscreen validation/recycle candidates | `GEO-002` | all map-edge/camera orientations pass |
| `GEO-008` | Exploration raster, discovery, marker/waypoint model | `GEO-002` | visible-is-discovered and fog fixtures |

- **Owned file scope:** the geometry subtree of `src/MechaMiner.Simulation/` and its
  mirror in `tests/MechaMiner.Simulation.Tests/`.
- **Contracts:** `CMP-GEO-001`, plus `CTR-MAP-002` as a consumer.
- **Requirements:** `TR-GEO-001` through `TR-GEO-006`.
- Retires `RSK-005` (flow-navigation clumping) at `GEO-005`.

### `W2-CAT` - gameplay catalog transcription and reports

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `DAT-007` | Import accepted gameplay catalogs into initial JSON definitions | `DAT-006` | totals/mappings/numbers match GDD/CSV reports |
| `DAT-008` | Generate CSV/balance/coverage/traceability reports | `DAT-006` | generated artifacts stable and stale detection works |
| `DAT-009` | Localization catalogs, named-placeholder validation, pseudo-localization | `DAT-005` | missing/mismatched/expansion fixtures pass |

This is the largest pure-volume item in the program: transcribing the accepted
gameplay catalogs from the `docs/` Markdown into `content/` JSON. It is almost
perfectly parallel with everything else because it touches data, not code.

- **Authority direction matters.** `docs/data/*.csv` carries
  `authoritative: false`; its own index says the files are "intentionally
  subordinate to the linked authoritative Markdown specification: when values
  disagree, update the data mirror to match the Markdown". **The Markdown wins.**
  A CSV/Markdown disagreement is a data-mirror defect to correct, never a new
  design decision, and doc 114 already authorizes an agent to "fix formulas, units,
  rounding, or data transcription to match the gameplay source without asking".
- **Owned file scope:** `content/` catalog directories (all of doc 40's layout
  except `content/schemas/`, which stays with `W1-DAT`), `content/localization/`,
  `generated/reports/`, and the report generators the stream adds under
  `src/MechaMiner.Tools/`.
- **Contracts:** consumes `SCH-CNT-001`; populates `SCH-CNT-002`, `SCH-CNT-003`;
  produces through `CMP-CNT-001` into `CTR-CNT-001` / `SCH-CNT-004`.
- **Requirements:** `TR-DAT-002`, `TR-DAT-004`, `TR-DAT-005`, `TR-DAT-006`.
- Retires `RSK-012` (content JSON and gameplay docs drift) - its first response is
  "fail build; update authoritative gameplay + JSON + generated reports together".
- Split by catalog file so several sessions can run concurrently inside this
  stream: resources, mechs, enemies, bosses, mining sites, encounters, maps,
  weapons, branches, utilities, relics, PowerUps, unlocks.

### `W2-PST` - persistence

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `PST-001` | Save envelopes, canonical JSON, checksum, schema validation | `FND-003`, `DAT-001` | valid/corrupt/limit fixtures |
| `PST-002` | Atomic write/backups/corruption recovery | `PST-001` | fault injection at every write step |
| `PST-003` | Profile/settings/history domain and transactions | `PST-002`, `PRG-005` | load/save/refund/unlock/history fixtures |
| `PST-004` | Pending extraction settlement idempotency | `PST-002`, `PRG-006` | crash-before/after-commit tests |
| `PST-005` | Run recovery capture/restore/rebuild | `PST-002`, `SIM-009`, `MAP-007` | checksum round trip and resume-paused |
| `PST-006` | Sequential migration framework and initial fixtures | `PST-003` | old/current/future-version behavior |

- `PST-001` and `PST-002` start in wave 2 on `DAT-001` alone. `PST-003`-`PST-006`
  wait for their `PRG`, `SIM-009`, and `MAP-007` dependencies and therefore
  actually land in wave 3/4; they are listed here so one stream owns the whole
  persistence surface end to end.
- **Owned file scope:** `src/MechaMiner.Persistence/`,
  `tests/MechaMiner.Persistence.Tests/`.
- **Contracts:** `CMP-PST-001`; `CTR-PST-001`, `CTR-PST-002`, `CTR-PST-003`;
  `SCH-PST-001` through `SCH-PST-004`, and `SCH-RUN-003` serialization.
- **Requirements:** `TR-PST-001` through `TR-PST-006`.
- **Boundary note:** doc 115 permits `MechaMiner.Persistence` to reference "narrow
  immutable types from `MechaMiner.Simulation`". `FND-001` deliberately did not add
  that project reference because no such type exists yet. `PST-005` is the package
  that requests it from the integration owner if `SCH-RUN-003` genuinely needs it;
  the architecture check's allowed-edge table is updated in the same change.
- Retires `RSK-010` (recovery snapshot cost) at `PST-005`.

### `W2-SHELL` - Godot shell, camera, UI frame, input

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `PRE-001` | Godot run scene, snapshot bridge, entity-handle lifecycle | `FND-003`, `SIM-007` | rebuild/dispose/missed-event tests |
| `PRE-002` | Orthographic camera, world conversion, clamp, footprint publication | `PRE-001`, `GEO-003` | both aspect ratios and boundary captures |
| `UI-001` | Route coordinator, immutable view models, shared widgets, focus IDs | `FND-003`, `SIM-007` | route/focus harness |
| `UI-002` | Logical input, movement adapter, glyph detection, controller disconnect | `UI-001`, `PLY-001` | deadzone/remap/disconnect tests |

- **Owned file scope:** `game/presentation/`, `game/scenes/` (except the
  integration-owner-held `Boot.tscn`), `game/shaders/`,
  `tests/MechaMiner.Game.Tests/`. A new `game/` subdirectory is an
  integration-owner change.
- **Contracts:** `CMP-PRE-001`, `CMP-PRE-002`, `CMP-UI-001`, `CMP-APP-001` as it
  replaces `BootCompositionRoot`; `CTR-PRE-001`, `CTR-UI-001`, `CTR-UI-002`,
  consuming `CTR-SIM-002` and `CTR-SIM-003`.
- **Requirements:** `TR-PRE-001` through `TR-PRE-004`, `TR-UI-001` through
  `TR-UI-005`, `TR-RUN-001`, `TR-SIM-005`.
- `UI-002` needs `PLY-001` (wave 3), so it lands at the wave 2/3 boundary.
- Retires `RSK-013` (C#/Godot interop cost) at `PRE-001` and `RSK-009`
  (1280x800 gamepad UI) through the `UI` screenshot/focus matrices.

### `W2-AST` - asset and license ledger

| Package | Deliverable | Depends on | Completion gate |
| --- | --- | --- | --- |
| `AST-001` | Asset/license manifest, allowlist, credits/notices generator | `DAT-005` | 100% packaged license coverage fixture |

- **Owned file scope:** `assets-manifest/assets/`, `assets-manifest/licenses/`, the
  notices generator under `src/MechaMiner.Tools/`.
- **Contracts:** `SCH-AST-001`.
- **Requirements:** `TR-AST-001`.
- Unblocks `AST-002`-`AST-006` and `OPS-002`, so landing it early in wave 2 keeps
  the whole asset chain off the critical path.

---

## Wave 3 - gameplay vertical slice

The route doc 110 § Vertical-slice sequencing calls "the fastest safe route to M4".
Many streams, but they are more coupled than wave 2, so each PR must name the
`CMP-*` it modifies.

| Stream | Packages | Owned file scope | Contracts |
| --- | --- | --- | --- |
| `W3-PLY` | `PLY-001` (player movement, facing, Hull/Armor/Recovery/contact grace, damage order; depends `SIM-008`, `GEO-003`) | player subtree of `src/MechaMiner.Simulation/` | `CMP-SIM-001`, `CMP-GEO-001`, player snapshot fields; `TR-SIM-*`, `TR-GEO-*`, `TR-COM-004` |
| `W3-COM` | `COM-001` scheduler/targeting/provenance (`SIM-006`, `SIM-008`, `GEO-004`, `DAT-004`), `COM-002` projectile/hitscan/beam/zone/explosion (`COM-001`, `GEO-001`), `COM-003` damage pipelines/death/attribution (`COM-002`, `PLY-001`), `COM-004` control/status runtime (`COM-003`) | combat subtree of `src/MechaMiner.Simulation/` | `CMP-COM-001`, combat portions of `CTR-SIM-*`; `TR-COM-001` through `TR-COM-008` |
| `W3-ENC` | `ENC-001` pure pursuer store/contact/profiles (`DAT-002`, `GEO-005`, `PLY-001`), `ENC-002` director schedule compiler (`DAT-002`, `SIM-009`) | encounter subtree of `src/MechaMiner.Simulation/` | `CMP-ENC-001`, spawn/world-query contracts; `TR-ENC-001` through `TR-ENC-004` |
| `W3-MIN` | `MIN-001` site store/occupancy/grace/decay/installments (`SIM-003`, `GEO-001`, `DAT-002`), `MIN-002` geode resonance and Hyper Gold threshold history (`MIN-001`, `ENC-001`) | mining subtree of `src/MechaMiner.Simulation/` | `CMP-MIN-001`, mining events/intents; `TR-MIN-001` through `TR-MIN-005` |
| `W3-MAP` | `MAP-001` profile selector (`DAT-002`, `SIM-005`), `MAP-002` bridgeless region graph (`GEO-002`, `SIM-005`), `MAP-003` spatial embedding (`MAP-002`), `MAP-004` landmarks (`MAP-003`, `DAT-005`), `MAP-005` deployment selection (`MAP-003`, `GEO-007`), `MAP-006` constraint solver (`MAP-005`), `MAP-007` manifest/checksum/retry (`MAP-001`-`MAP-006`) | map-generation subtree of `src/MechaMiner.Simulation/` (or its own generator area agreed with the integration owner) | `CMP-MAP-001`, `CTR-MAP-001`, `CTR-MAP-002`, `SCH-MAP-001`, `SCH-MAP-002`; `TR-MAP-001` through `TR-MAP-006` |
| `W3-PRG` | `PRG-001` ledger (`SIM-006`, `DAT-002`), `PRG-002` fabrication (`SIM-004`, `SIM-008`, `PRG-001`, `DAT-007`), `PRG-003` utility/radar (`PRG-002`, `GEO-004`), `PRG-004` relic cache transactions (`SIM-004`, `COM-010`, `PRG-001`), `PRG-005` PowerUp/unlock (`DAT-007`, `SIM-004`), `PRG-006` result manifest/settlement (`PRG-001`, `ENC-008`) | progression subtree of `src/MechaMiner.Simulation/` | `CMP-PRG-001`, `CTR-RUN-003`, `CTR-SIM-004`, `SCH-RUN-002`; `TR-PRG-001` through `TR-PRG-005` |
| `W3-UI` | `UI-003` HUD shell (`UI-001`, `PRE-002`), `UI-004` mining panel/survey/radar (`UI-003`, `MIN-002`, `PRG-003`), `UI-005` minimap/full map (`UI-001`, `GEO-008`), `UI-006` run console/fabrication (`UI-001`, `PRG-002`), `UI-007` relic modal (`UI-006`, `PRG-004`) | `game/presentation/` UI subtree, `game/scenes/` UI scenes | `CMP-UI-001`, `CTR-UI-001`, `CTR-UI-002`; `TR-UI-001` through `TR-UI-005` |
| `W3-PRE` | `PRE-003` static world instantiation (`PRE-001`, `MAP-007`), `PRE-004` crowd VAT/instancing spike (`PRE-001`, `AST-003`), `PRE-005` durable adapters (`PRE-001`, `DAT-007`), `PRE-006` transient pools/priority (`PRE-001`, `COM-002`), `PRE-007` materials/lighting/quality/warm-up (`PRE-003`-`PRE-006`) | `game/presentation/`, `game/shaders/` | `CMP-PRE-001`, `CMP-PRE-002`, `CTR-PRE-001`; `TR-PRE-001` through `TR-PRE-004` |
| `W3-AUD` | `AUD-001` mixer buses, audio event registry/service, voice priority/aggregation (`FND-007`, `SIM-006`) | audio subtree of `game/presentation/` | `CMP-AUD-001`, presentation events; `TR-AUD-001`, `TR-AUD-002` |

`PRE-004` is the `RSK-001` and `RSK-002` proof gate (900-instance crowd
performance and readability) and doc 110 wants it early: "PRE-001/002/004,
UI-001 through UI-003: establish M2 and performance direction early."

---

## Wave 4 - catalog breadth

Widest parallelism in the program. Each stream extends a wave 3 primitive that is
already Done, so streams rarely block each other.

| Stream | Packages | Split suggestion | Contracts |
| --- | --- | --- | --- |
| `W4-WEAPONS` | `COM-005` Pulse Repeater + Rail Lance direct primitives (`COM-001`-`COM-004`, `DAT-007`), `COM-006` Cluster Mortar + Gravity Projector area/persistent (`COM-001`-`COM-004`), `COM-007` Attack Drones/Sentry Pod autonomous actors (`COM-001`-`COM-004`), `COM-008` remaining base weapon behaviors - 15 base weapons (`COM-005`-`COM-007`), `COM-009` all 45 branch modifiers (`COM-008`), `COM-010` ten relic hook policies and the weapon compatibility matrix (`COM-009`, `PRG-005`), `COM-011` utilities/PowerUps/mech traits (`COM-010`, `DAT-007`) | **one session per weapon family**, then one for branches per family, then relics, then utilities/PowerUps/traits | `CMP-COM-001`; `TR-COM-001` through `TR-COM-008` |
| `W4-ENC` | `ENC-003` formations/recycling (`ENC-002`, `GEO-007`), `ENC-004` Needler (`ENC-001`, `COM-002`), `ENC-005` elites (`ENC-001`, `ENC-002`), `ENC-006` Riftjaw + Brood Titan (`ENC-003`, `COM-002`), `ENC-007` Prism Crown + Skybreaker Apex (`ENC-006`), `ENC-008` boss warning/re-entry/death/loot/overlap (`ENC-006`, `ENC-007`, `PRG-003`), `ENC-009` Hyper Gold response packages (`ENC-002`, `MIN-002`) | one session per boss state machine | `CMP-ENC-001`; `TR-ENC-001` through `TR-ENC-006` |
| `W4-MAP` | `MAP-008` dynamic rocks (`MAP-007`, `GEO-007`), `MAP-009` map audit CLI/images/reports/batch runner (`MAP-007`), `MAP-010` nightly profile/signature seed matrix (`MAP-009`, `FND-005`) | sequential inside the stream | `CMP-MAP-001`, `SCH-MAP-002`; `TR-MAP-004`, `TR-MAP-007`. `MAP-009`/`MAP-010` are the `RSK-004` proof gate |
| `W4-UI` | `UI-008` results/unlocks/Hangar/Mechs/PowerUps/Blueprints/Records (`UI-001`, `PRG-005`, `PRG-006`, `PST-003`), `UI-009` settings/remapping/accessibility (`UI-001`, `UI-002`), `UI-010` onboarding coordinator (`UI-003`-`UI-009`) | one session per screen group | `CMP-UI-001`, `CTR-UI-001`, `CTR-UI-002`; `TR-UI-002`, `TR-UI-003`, `TR-UI-005` |
| `W4-AST` | `AST-002` pinned Blender/glTF pipeline (`AST-001`, `FND-002`), `AST-003` crowd rig to LOD/VAT (`AST-002`), `AST-004` UI/icon/material identity + accessibility contact sheets (`AST-001`, `DAT-009`), `AST-005` audio/font/VFX validators and budgets (`AST-001`), `AST-006` acquire/adapt the representative CC0 set for M4 (`AST-001`-`AST-005`) | `AST-003` before `PRE-004` needs it; `AST-006` last | `SCH-AST-001`, import/build contracts; `TR-AST-001` through `TR-AST-005`. `AST-003` is an `RSK-001` gate, `AST-006` an `RSK-008` gate |
| `W4-PLT` | `PLT-001` Steam/local platform adapter (`FND-006`), `PLT-002` Steam Cloud sync and conflict model (`PLT-001`, `PST-003`) | sequential | `CMP-PLT-001`, `CTR-PLT-001`; `TR-PLT-001` through `TR-PLT-003`. See decision 3 below before starting |
| `W4-PST` | `PST-003` through `PST-006` once their `PRG` dependencies are Done | same owner as `W2-PST` | as `W2-PST` |
| `W4-QUA` | `QUA-001` WB-01-WB-06 balance harness (`SIM-009`, `COM-008`), `QUA-002` capture metrics/reconciliation (`FND-008`, `PRG-006`), `QUA-003` debug overlay/dev command palette (`FND-008` + "gameplay systems"), `QUA-004` diagnostic package/breadcrumbs (`FND-007`, `FND-008`, `PST-005`), `QUA-005` PERF-01-PERF-08 runner ("presentation/gameplay complete per scenario") | one session per harness | `CMP-OBS-001`, `CTR-OBS-001`; `TR-QUA-001` through `TR-QUA-004`, `TR-OBS-001`, `TR-OBS-002` |
| `W4-OPS` | `OPS-001` main/nightly/release CI suites (`FND-005`, `QUA-005`), `OPS-002` release packaging/checksums/SBOM (`FND-006`, `AST-001`, `OPS-001`), `OPS-003` Steam staging/depot/rollback (`PLT-002`, `OPS-002`), `OPS-004` retail Steam Deck RC gate (`OPS-003`, M6) | sequential; integration owner for `OPS-002`+ | build/release manifests, `SCH-BLD-001`; `TR-BLD-001` through `TR-BLD-006` |

Three wave 4 packages have dependencies that are **not** package IDs and cannot be
scheduled from the graph alone: `QUA-003` ("`FND-008`, gameplay systems"),
`QUA-005` ("presentation/gameplay complete per scenario"), and `OPS-004`
("`OPS-003`, M6" - a milestone). Their readiness is an integration-owner judgement,
not a lookup.

`OPS-003` and beyond touch credentials, depots, and external state. Doc 114
§ Explicit escalation boundary reserves those for a human: "using credentials,
creating external accounts, publishing a build, changing a storefront/depot".

---

## Wave 5 - M4 integration, then breadth, then release

1. **M4 internal demo integration - integration owner.** Assemble the diagnostic
   scenario against doc 110 § M4 diagnostic scenario contract and close doc 110
   § Internal demo acceptance checklist. Doc 110: "Package and validate M4 before
   completing catalog breadth."
2. **M5 - full standard-run feature completeness.** Remaining catalog breadth
   across the wave 4 streams.
3. **M6 - content/performance production readiness**, then **M7 - release
   candidate**, driven by `W4-QUA` and `W4-OPS` with the integration owner holding
   the release gate in doc 100 § Release gate.

Doc 113 requires a risk review at each M0-M7 gate.

---

## Decisions already made

Recorded here so no stream re-litigates them. **They were not all established the same
way, and the list has grown past the sentence that used to cover it.** An earlier
revision said "each was verified empirically in the FND-001 container, not recalled",
which was true of Decisions 1-4 and is not true of the rest:

- Decisions 1-4 were measured in the FND-001 container: versions installed, projects
  built, exit codes observed. Each names the fixture or gate that holds it today.
- Decisions 5-7 are design and routing choices, not measurements. There was nothing to
  measure; what they record is which class, code, and owning package was chosen and why
  a ninth exit class was rejected.
- Decision 8's `build.ps1` half is a hand measurement on a host that had `pwsh`, not a
  container result and not a committed gate. That bullet says so itself.
- Decisions 9-11 are rules distilled from defects found in this repository's own gates.
  Each cites the sites it came from; the rules themselves are obligations on future
  gates and no gate asserts them.
- Decision 12 is a claim about a commit message, two pull-request reviews, and the
  absence of a file on any ref. It is established by search, and its own text records
  that three of the nine items it tables have no gate at all.

So read each decision for how it says it was established. Do not quote this section as
a block of empirical results.

### Decision 1 - .NET SDK 10.0.302 with target framework `net8.0`

- **What:** `global.json` pins SDK `10.0.302` (`rollForward: latestPatch`).
  Every project targets `net8.0` with `LangVersion` pinned to `12.0`.
- **Why:** doc 00 § Version policy says ".NET SDK selection follows the pinned Godot
  version's supported baseline". `GodotSharp` 4.7.1 declares `net8.0` and ships only
  `lib/net8.0`, so `net8.0` is that baseline. 10.0.302 is the SDK actually
  installable in this environment and is current LTS. `net8.0` assemblies run on the
  installed .NET 10 runtime because Godot's `GodotPlugins` runtimeconfig rolls
  forward; `RollForward=LatestMajor` in `Directory.Build.props` does the same for
  pure test hosts, so no separate .NET 8 runtime install is required.
- **Consequence:** `LangVersion` is `12.0`, not `latest`. A C# 13+ feature is a
  build error, proved by `build/policy-fixtures/langversion` (`error CS9202`).
  Raising it requires raising the target framework first, which is an
  integration-owner change.

### Decision 2 - Godot 4.7.1 confirmed real and current stable; the pin stands

- **What:** `Godot.NET.Sdk/4.7.1`, editor and export templates at `4.7.1-stable`
  from the `godot-builds` release assets.
- **Why:** verified against the official download page at task time, not from
  memory: 4.7.1 is the current latest stable, released 14 July 2026. It was
  downloaded, it builds a C# project, and it executes C# under `--headless` with
  correct exit codes. `4.7.2-rc.1` and `4.8.0-dev.2` exist on nuget.org but doc 114
  forbids substituting a preview or nightly binary.
- **Consequence:** no change to doc 00 or `TR-FND-001`. Doc 00 § Version policy
  keeps Godot minor/major upgrades behind a TDR, and `RSK-014` keeps the project
  pinned until full compatibility evidence exists.
- **CI note beyond the pin:** without a Vulkan ICD, Godot silently degrades to
  OpenGL 3 and the mandated Mobile renderer is never exercised. `mesa-vulkan-drivers`
  is therefore part of `build/bootstrap-linux.sh`. `--headless` uses the dummy
  driver and cannot capture screenshots, so visual verification needs
  `xvfb-run` plus lavapipe. Two distinct CI tiers, not one.

### Decision 3 - `Steamworks.NET 2025.164.1` must be vendored, not restored from NuGet

- **What:** `TR-PLT-001` and doc 110 `PLT-001` pin "Steamworks.NET Standalone
  2025.164.1/SDK 1.64". **The pin stands.** But the package is not on nuget.org:
  the newest version published there is `2024.8.0` (Steamworks SDK 1.60).
  `dotnet add package Steamworks.NET --version 2025.164.1` cannot work. Verified
  against the registry index and search API at task time.
- **How PLT-001 obtains it:** `git clone` at tag `2025.164.1` (verified to succeed
  through the proxy) and build `Standalone2.0/Steamworks.NET.Standard.csproj` from
  source. The clone contains the CodeGen SDK 1.64 headers and the prebuilt natives
  including `Plugins/libsteam_api.so`.
- **Deferred to `PLT-001`.** No vendoring happens before that package is Ready. The
  integration owner adds the vendored project to `MechaMiner.sln` and records the
  dependency per doc 100 § Dependency policy.
- **Steam stays untestable in CI, and the architecture already handles that.** The
  Valve partner SDK 1.64 download is login-gated, so real Steam behavior cannot be
  exercised here. That is not a new problem: `CTR-PLT-001` already specifies
  "unavailable platform returns supported typed local fallback, never blocks play",
  `TR-PLT-002` requires initialization/callback/shutdown failure to degrade to the
  local path, and `TR-PST-001` requires the game to be fully playable and savable
  offline. `PLT-001` is built and verified against that seam plus a fake; real Steam
  verification happens on a developer machine.
- **Specification correction owed.** Doc 100 § Toolchain pinning lists
  "Steamworks.NET Standalone 2025.164.1 and matching Valve Steamworks SDK 1.64/native
  redistributables" among things to "Pin in version-controlled files", in a list
  whose neighbouring bullet is "NuGet dependency graph and lock files". That implies
  a NuGet reference, which is not achievable at the pinned version. `PLT-001` must
  correct that bullet in the same task to say the managed binding is vendored from
  the tagged source rather than restored from nuget.org - doc 114 § Specification
  maintenance autonomy both permits and requires this, because it is the case where
  "the documented contract cannot be implemented as written".

### Decision 4 - doc 100's three configurations map onto Godot's three, and doc 100 was corrected

- **The conflict was real.** Doc 100 § Build configurations prescribed
  `Debug` / `Development` / `Release`. `Godot.NET.Sdk/4.7.1` sets
  `<Configurations>Debug;ExportDebug;ExportRelease</Configurations>` unconditionally in
  its own `Sdk.props`, before `Directory.Build.props` is even evaluated, and Godot's
  tooling only ever asks for those three names: the editor builds `Debug`, and an export
  preset's export-with-debug flag selects `ExportDebug` or `ExportRelease`.
- **Why a fourth configuration is not the answer.** An MSBuild configuration named
  `Development` builds, but nothing can ever produce it as a Godot export, so
  `export <platform> development` would still have to emit an `ExportDebug` build. The
  configuration would exist only as a name.
- **What was implemented.** A 1:1 mapping. `Debug` -> `Debug`,
  `Development` -> `ExportDebug`, `Release` -> `ExportRelease`. Nothing dropped, nothing
  invented. The workflow vocabulary stays doc 100's three names; the wrapper's
  `configuration` argument accepts exactly `debug`, `development`, `release` and
  translates. Every project declares the same three MSBuild configurations, because a
  project reference built from `MechaMiner.Game` under `ExportRelease` inherits that
  configuration and `Microsoft.NET.Sdk` knows only `Debug` and `Release`; optimization,
  symbols, and one `MECHAMINER_*` diagnostic symbol per configuration are therefore set
  explicitly in `Directory.Build.props`. `MechaMiner.sln` carries exactly these three
  solution configurations on `Any CPU`.
- **Project code gates on `MECHAMINER_DEBUG`, `MECHAMINER_DEVELOPMENT`, or
  `MECHAMINER_RELEASE`**, never on `DEBUG`, which `Godot.NET.Sdk` also defines for
  `ExportDebug`. `build/verify-configurations.sh` asserts that exactly one
  `MECHAMINER_*` symbol is defined per configuration across five projects.
- **A second defect the resolution had to absorb.** `Godot.NET.Sdk` references
  `GodotSharpEditor` only under `Debug`, so restoring under `ExportRelease` rewrites
  `game/packages.lock.json` and every later `--locked-mode` restore fails. Restore is
  therefore configuration-independent: the `build` verb restores once at the default
  configuration, which yields the superset graph all three configurations build against,
  and then builds the requested configuration with `--no-restore`. The gate asserts that
  no lock file changes after building all three.
- **Doc 100 was corrected in the same PR**, per doc 114 § Specification maintenance
  autonomy, which requires it when "the documented contract cannot be implemented as
  written". § Build configurations now carries an `MSBuild identity` column, the reason
  the identity is not free choice, the per-project declaration rule, and the restore
  rule. The next reader of doc 100 sees the mapping rather than an apparent
  contradiction.
- **Successor.** `FND-006` creates the four export presets and is the package that
  proves the mapping end to end by producing a `development` and a `release` package per
  platform.

### Decision 5 - a registered verb whose owner has not landed returns exit class 2 with a distinct diagnostic code

- **The gap was real.** Doc 110's `FND-002` completion gate requires that "unavailable
  owner verbs return a typed nonzero status until their package lands". Doc 100 fixes
  exactly eight exit classes and **none of them means "not implemented yet"**.
- **What was implemented.** Exit class `2` (invalid verb or arguments) with the stable
  diagnostic code `MMT-2002`, the owning work-package ID, and the verb's required effect,
  in both the printed final result and the structured result document. An unknown verb is
  `MMT-2001` and an invalid argument is `MMT-2003`, so the three cases are
  distinguishable in structured output while sharing one class.
- **Why not a ninth class.** Doc 100's own sentence assigns finer distinctions to
  structured output: "More detailed stable diagnostic codes live in structured output".
  Adding a class would change a contract every later tool and CI job reads, to express
  something the existing mechanism already expresses.
- **Doc 100 § Standard command surface now states this**, plus the fact that there is
  deliberately no class `1`.
- **Consequence for CI.** `FND-005` can invoke any verb and get a loud, classified
  failure with the package to chase. `./build.sh content` before `DAT-006` lands exits 2
  naming `DAT-006`.

### Decision 6 - the ten unimplemented verbs and their owning packages

Recorded so no stream re-derives the routing. These are registration decisions, not new
scope: each names the package doc 110 already makes responsible for the behavior.

| Verb | Owner | Why that package |
| --- | --- | --- |
| `test-nightly` | `OPS-001` | doc 110: "main/nightly/release CI suites" |
| `content` | `DAT-006` | canonical bundle compiler, hash, and reports |
| `run` | `FND-006` | owns the local launch and platform adapter path |
| `scenario <id>` | `SIM-009` | headless simulation runner, step/advance/script/checksum/report |
| `map --seed <seed>` | `MAP-009` | map audit CLI, images/layers/reports |
| `map-batch <partition>` | `MAP-010` | the nightly profile/signature seed matrix defines the partitions |
| `benchmark <id>` | `QUA-005` | the `PERF-01`-`PERF-08` runner; `QUA-001` supplies the `WB-*` scenarios and `FND-008` the report format |
| `export <platform> <configuration>` | `FND-006` | named Windows/Linux export presets |
| `package-demo` | `OPS-002` | release packaging, checksums, SBOM |
| `release-validate` | `OPS-002` | release gates and manifest generation |

The implementing package flips its own row from `AwaitingOwner` to `Implemented` in
`src/MechaMiner.Tools/Cli/VerbRegistry.cs` and updates
`build/verify-verbs.sh`'s matrix in the same change. That file is integration-owner
scope, so the request goes through the integration owner.

### Decision 7 - doctor reports a tool whose owning package has not landed as deferred, not missing

- Doc 100 § Toolchain pinning lists Blender and the export templates among the pinned
  tools. The derivation scripts that need Blender are `AST-002`'s, and the export
  presets that need the 1.2 GB templates are `FND-006`'s.
- Failing `doctor` on their absence would make the verb unusable in every environment
  until those packages land, which defeats the gate rather than strengthening it. So
  `build/toolchain.json` records each tool with the package that will require it, and
  `doctor` prints it as `deferred` with that package named. `doctor` fails, with exit
  class 3, only on a tool that is required now.
- The owning package moves its tool from `optional_tools` to required, and pins its exact
  version, in the same change that first needs it.

### Decision 8 - PowerShell is not a pinned requirement on Linux or macOS

- `build.ps1` is the Windows wrapper. `pwsh` is listed under `optional_tools` in
  `build/toolchain.json`.
- `build/verify-wrapper-parity.sh` therefore proves parity two ways. Structurally, and
  on every platform: neither wrapper contains a `case`, a `switch`, a `$1`, a `shift`,
  or an indexed read of the argument vector, and both build the same host project and
  forward every argument verbatim - so there is one verb table and nothing that can
  drift. Behaviorally, when `pwsh` is present: run both wrappers and require
  byte-identical usage tables. When `pwsh` is absent the behavioral check reports
  itself as **skipped**, by name, it is counted, and the script's final summary line
  says how many required checks did not run - so a run with a skip cannot be read, or
  quoted elsewhere, as a run in which parity was proved by execution.
- Two consequences of leaving `pwsh` unpinned, both recorded rather than glossed:
  `VER-FND-002-008` lists **`linux-x64` only**, because the behavioral half executes
  `build.ps1` on whichever host invokes it and no Windows or macOS host ever has;
  behavioral parity on `windows-x64` and `osx-arm64` is **pending** and needs runners
  on those platforms, which `FND-005` owns. Adding PowerShell to the pinned toolchain
  instead was considered and rejected: it would invent a toolchain dependency in order
  to make a coverage claim true, and doc 100 § Toolchain pinning would then require an
  exact version and per-platform hashes for a tool that no repository verb needs on
  Linux or macOS.
- Both wrappers do share one behavior that is not merely structural: an absent `dotnet`
  and a `global.json` pinning an uninstalled SDK version each exit class 3 with
  `MMT-3001`, never class 8. See Decision 10. **It is asserted by execution on
  `build.sh` only.** `build/verify-verbs.sh` § 10 invokes `${WRAPPER}`, which is
  `build.sh`, and `build/verify-wrapper-parity.sh`'s behavioral half does not run without
  `pwsh`, so no committed gate ever runs `build.ps1 doctor` against a mismatched pin.
  `build.ps1` does carry the same `MISMATCH-PROBE` block, and the reviewer who reported
  the defect installed `pwsh` and observed the same 8 -> 3 transition by hand, so the
  claim is true as fact. It is not evidence a clean checkout can produce, which is the
  same distinction the `pwsh` bullet above draws for the usage tables. An earlier revision
  of this bullet said the behavior "is asserted on both"; that was an overclaim and is
  corrected here.

### Decision 9 - deliberately invalid fixtures are never committed inside a compiled project

- `FND-001` established the pattern: invalid fixtures live under
  `build/policy-fixtures/`, outside `MechaMiner.sln`, and a gate script drives them in
  isolation.
- Three `FND-002`/`FND-003` gates need a bad fixture that the verb under test can
  actually see, and the verbs under test operate on `MechaMiner.sln`, so a project the
  solution excludes would be invisible to them. Those fixtures are therefore
  **transient**: `build/verify-format.sh` and `build/verify-verbs.sh` write them, run
  the gate, and remove them on every exit path including failure. Nothing is committed,
  and they are written into a test project rather than a shipping assembly, so no
  committed file inside `MechaMiner.Tools` can fight `format`, `format-check`, or
  warnings-as-errors.
- The one fixture that is committed is the deliberately failing NUnit case
  (`SeedReproductionFixture`), which is marked `Explicit` so no ordinary run executes
  it, and whose contract is independently covered by always-on tests.

### Decision 10 - exit class 3 has two halves, and a gate that proves one does not prove the other

- Doc 100 § Standard command surface defines class 3 as a "missing **or mismatched**
  pinned environment". Those are two different environment faults and they reach the
  wrappers by two different routes.
- The wrappers originally gated class 3 on `command -v dotnet` alone. A `global.json`
  pinning an SDK version that is not installed therefore passed that check, failed
  later inside the verb host's own `dotnet build`, and was reported as class 8
  `MMT-8001` "unexpected tool-internal failure" - blaming the repository for an
  operator's environment. Both wrappers now probe pin resolution explicitly and return
  3 with `MMT-3001` for a mismatch, while a genuinely uncompilable verb host still
  returns 8.
- The gate gap that let it survive is the general lesson: `build/verify-verbs.sh` § 6
  asserted only the *absent* half, and nothing asserted the *mismatched* half, so half
  of a documented exit class had no gate at all. § 10 now asserts the other half.
  **When a contract enumerates alternatives, each alternative needs its own
  assertion**; a gate that covers one member of a documented set and is named after
  the whole set is worse than no gate, because it reads as coverage.

### Decision 11 - a gate never passes on an input set it did not successfully obtain

- Several gates in this repository were found to succeed vacuously: they derived a
  candidate set (files to check, paths to hash, projects to scan), the derivation
  failed or returned nothing, and the "no violations found" branch then reported
  success. A gate that cannot see anything must not conclude that everything is fine.
- The rule, applied to every gate the integration owner owns:
  1. **A failed subprocess is a gate failure.** Never `|| true`, never a discarded
     exit status, never an empty result substituted for an error. If `git ls-files`
     fails, the gate fails; it does not check zero files.
  2. **An empty candidate set never satisfies a gate.** Zero matches is a distinct
     outcome from zero violations. A gate whose set is legitimately empty says so
     explicitly and names why; a gate whose set is unexpectedly empty fails.
  3. **Verify the artifact you resolved, not the one you assumed.** A probe that
     resolves a path and then validates a canonical path instead is not checking the
     thing that will be used. Hash, version-check, and compare the resolved artifact.
  4. **Every gate carries a negative control.** The fixture that must fail has to
     actually fail, asserted in the same run, or the positive result proves nothing.
- These are not new requirements. AGENTS.md § Task execution already forbids masking a
  failure and loosening a threshold to pass, and doc 91 already requires a gate to have
  observable meaning. Decision 11 records them as a checklist because of how widely the
  same defect had spread: a review reported three instances, and auditing the
  neighbouring gates for the same shape found five more, across eight sites in
  `format`/`format-check`, `build`, `doctor`, `verify-architecture.sh`,
  `verify-godot.sh`, and `verify-configurations.sh`. Every one of them reported success
  on something it had not examined. Assume the shape is present until the negative
  control proves otherwise.

### Decision 12 - a message never cites a numbered list that is not an artifact

- Commit `a494f09` on this branch carries the message `WIP: inherited uncommitted
  review-fix work (items 1-8, 11), unverified` and enumerates nine items in its body. It
  is the parent of most of the wrapper and gate work described above, it is not on
  `origin/master` - the common ancestor of `master` and this whole stack is `739bf29` -
  and it has reached four of the six pull requests stacked on this branch, three of them
  by base merge (`a319afb`, `ea88ea8`, `c5f1378`) and one by linear descent.
- **The numbered list it indexes into exists in no artifact.** It is not in any review or
  comment on any pull request in this repository, and not in any file on any ref at any
  commit. It was a triage list an agent assembled by merging findings from two independent
  reviews, each of which numbers its own findings 1 through 9, and it lived only in that
  agent's context. So the numbers survive in permanent history and the list does not, and
  no reader of `git log` can resolve "items 1-8, 11" - which makes every claim that
  message makes unauditable by the reader it was written for.
- **Where the traceable items came from.** Six of the nine map onto the two reviews, and
  two of those six map onto unnumbered "Corrections to the PR body" bullets rather than
  onto numbered findings, so even the traceable half does not resolve as numbers:

  | Item | Subject | Source |
  | --- | --- | --- |
  | 1 | this document's false "Done" / "Wave 1 is therefore open" wording | nothing |
  | 2 | mismatched SDK pin returns class 3, not class 8 | [PR #3 review](https://github.com/pwestling/mecha-miner-survivor/pull/3#pullrequestreview-4870741930) finding 7 |
  | 3 | `VER-FND-002-008` narrowed; the counted, named `SKIPPED` path | PR #3 review finding 8, plus its "`build.ps1` parity - proved by execution" correction bullet |
  | 4 | doc 40 derived-geometry self-contradiction (contact diameter) | nothing |
  | 5 | `format`/`format-check` fail open when `git ls-files` fails | PR #3 review finding 3 |
  | 6 | `verify-architecture.sh` GDScript check `\|\| true` and untracked `.gd` | PR #3 review finding 6 and [PR #1 review](https://github.com/pwestling/mecha-miner-survivor/pull/1#pullrequestreview-4870772506) finding 6 |
  | 7 | `doctor`'s Godot hash probe hashes a file it did not resolve | PR #3 review finding 4 |
  | 8 | `FND-001.json` platform overclaims | PR #1 review, "Corrections to the PR body and registry" |
  | 11 | delivery-waves.md "no public type in `src/`" claim | nothing - see below |

- **Item 11 had no referent, and the claim behind it was withdrawn.** No text matching
  "no public type in `src/`" appears in this document, or in any file on any ref, at any
  commit. Another session originated that claim in conversation, relayed it as a defect,
  and later retracted it; the sentence was never in any file. So item 11 was an item to
  fix something that did not exist, there was nothing to fix, and the retraction never
  reached the commit message - which is why it still reads as outstanding work. This is a
  withdrawn claim, not an open question.
- **Item 8 is not in `a494f09` at all.** That commit's only change to
  `tests/verification/FND-001.json` is item 6's registry counterpart; `VER-FND-001-001`
  and `-002` still carried three platforms there. The narrowing landed thirteen minutes
  later in `a494f09`'s immediate child, `bc28ae2`.
- **"unverified" is stale, and three of the nine items can never stop being unverified by
  a gate.** Every gate `a494f09` touched has since been run red-then-green by injection;
  the records live on the entries the gates belong to, in
  `tests/verification/FND-002.json` (`VER-FND-002-008`, `-016`, `-017`, `-018`) and
  `tests/verification/FND-001.json` (`VER-FND-001-005`), each naming the injected
  violation and the exit class at the parent `2e6d717` and at every commit carrying the
  fix. Items 1, 4 and 8 have no gate: items 1 and 4 are document edits, verified by
  reading, and nothing anywhere reads the registry `platforms` field, so item 8's
  correctness is a claim no check can hold. Item 3 is exit 0 on both sides by design -
  it converts a silent pass into `PASS WITH 1 SKIPPED CHECK(S) - coverage reduced`, so
  it is a reporting fix and its evidence is that line rather than an exit class.
- **The rule.** In a commit message, pull request body, or review, carry the claim rather
  than a pointer to it: one line per item saying what the item is costs a few hundred
  bytes and stays true forever. If a numbered list must be referenced, cite the artifact
  by URL or path plus sha and say which of several numbered lists it is - two of this
  project's reviews each number findings 1 through 9, so a bare "item 6" is ambiguous
  between them even for someone holding both. Never write "items N-M" where the list is
  only in your context. And when a message says "unverified", plan the commit that says
  otherwise: history is append-only, so a message on a branch others have already merged
  cannot be corrected in place. Doc 91 § Claim and measurement discipline states this as a
  rule about authors; this decision is the instance that produced it.

---

## Note on the four test projects

Doc 100 § Repository structure lists **four** test projects:
`MechaMiner.Simulation.Tests`, `MechaMiner.Content.Tests`,
`MechaMiner.Persistence.Tests`, `MechaMiner.Game.Tests`. Doc 10 § Accepted project
decomposition tables only three, omitting `MechaMiner.Content.Tests`.

**All four are implemented.** Doc 100's layout is the authority for repository
structure and `TR-BLD-006` ("Repository/project directories follow the accepted
ownership layout"), and doc 115 supports it directly: "Tests mirror those
projects", where the projects include `MechaMiner.Content`. Doc 10's table is a
responsibility summary, not a layout contract, and its omission is an editorial
gap rather than a decision to leave content untested.

No document was changed for this: the two sources are reconciled by precedence,
not by editing either. If `FND-009`'s documentation validator later wants the two
tables to agree literally, adding the missing row to doc 10 is the editorial
correction, never removing the project.

## Related documents

- [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md) - normative decomposition
- [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md) - integration ownership, evidence, escalation
- [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md) - project boundary and contract IDs
- [Verification Strategy](./91-verification-strategy.md) - verification registry rules
- [Technical Risk Register](./113-technical-risk-register.md) - `RSK-017` parallel-agent conflict response
- [Build, Dependencies, and Release Operations](./100-build-dependencies-and-release-operations.md) - repository structure and command surface
