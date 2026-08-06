---
doc_id: TDD-VERIFICATION
title: Verification Strategy
status: active
authoritative: true
---

# Verification Strategy

## Purpose

This document defines how agents prove implementation correctness, integration, performance, readability, persistence safety, and gameplay conformance. Verification is part of each work item, not a separate cleanup phase.

## Test layers

| Layer | Runs | Primary use |
| --- | --- | --- |
| Pure unit | every change | formulas, geometry, clocks, state transitions, validators, migrations |
| Property/fuzz | every change for bounded suites; nightly exhaustive | invariants across generated inputs and malformed data |
| Golden fixture | every change | accepted schedule, catalogs, derivations, saves, map manifests, edge ordering |
| Headless simulation integration | every change | multi-system run behavior without Godot |
| Godot engine integration | every change where relevant | scene bindings, imports, input, UI routes, presentation synchronization |
| Screenshot/render comparison | affected changes plus scheduled matrix | layout, geometry correspondence, accessibility, asset presentation |
| Export smoke | main branch/release candidates | packaged Windows/Linux launch and basic flow |
| Performance | local affected scenario, scheduled target-device suite | budget and regression evidence |
| Soak | nightly/release candidate | leaks, handle reuse, repeated runs, long queues |
| Usability/manual acceptance | milestone/release candidate | player comprehension and device behavior |

## Test project separation

- Pure simulation/content/persistence tests do not launch Godot.
- Engine integration uses a dedicated minimal Godot test runner or scenes, not production front-end navigation unless that is the subject.
- Test fixtures and reference assets are isolated from release content by export filters and manifest status.
- Tools call the same libraries and validators as the game; they do not reimplement rules for tests.
- Test-only access occurs through explicit diagnostic APIs/assemblies, not reflection into private state.

## Verification registry

Each work package owns `tests/verification/<work-package-id>.json`. Every entry contains a stable `VER-<WORK-PACKAGE>-###` ID, summary, cited `TR-*` requirements and gameplay sources, automated test selectors or manual/device procedure ID, fixture/seed/scenario IDs, evidence artifact kinds, applicable platforms/tier, and current status. Entries are added before implementation and never renumbered; retired verification retains a tombstone and successor.

The registry validator enforces unique IDs, existing requirements/sources/selectors/scenarios, at least one non-compilation verification for every implementation task, and no accepted behavior registration/content definition without coverage. Test runners emit executed verification IDs into the task evidence bundle. `PERF-*` and `WB-*` remain scenario IDs referenced by one or more `VER-*` entries.

## Determinism and fixture policy

- Every randomized test logs its seed and version identity before execution.
- Failures print a one-command/tool reproduction description and preserve the minimized input where possible.
- Golden outputs are canonical, ordered, and reviewable text or compact images.
- Updating a golden requires an authority-aware diff review of the underlying behavior change plus a regenerated evidence bundle; the implementing agent may perform that review under the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md), but may not accept snapshots merely to make a test pass.
- Fixtures declare compatibility scope. A deliberate algorithm-version change creates a new fixture set while retaining at least one migration/rejection test for the old identity.

## Domain coverage requirements

### Runtime and clocks

Fixed tick accumulation, zero/multiple ticks per render, catch-up cap, all pause reasons and overlaps, focus/suspension, command priority, final tick/extraction, technical failure, and scene disposal.

### Geometry and navigation

Every primitive, tangent/boundary case, swept collision, slide/corner, terrain tie, path/reachability, boss clearance, camera edge, spawn validity, fog reveal, waypoint, and offscreen recycle rule.

### Combat

Every base weapon, stat, branch, targeting policy, relic, utility, PowerUp, mech trait, resonance, control, projectile, actor capacity, recursion exclusion, rock fallback, damage order, shield, revival, Armor, contact grace, and statistics attribution.

Use pairwise combinatorial generation across the broad modifier matrix plus explicit exhaustive tests for interactions named in gameplay. Every weapon/relic pairing runs at least one behavior fixture and every branch runs all six benchmark scenes.

### Encounters

All 35 schedule rows, formations, population ceilings/queues, composition allocation, first-playable substitutions, Needler, elite construction, four boss state machines, overlap, re-entry, resonance, physical loot, and extraction with living bosses.

### Mining and economy

Every site class, installment/completion payout, grace/decay, modifiers, beacon threshold history, profile graph, recipes, prices, slots, irreversible branches, utilities/ranks, radar categories, relic transactions, PowerUp purchase/refund, unlock, ledger, and terminal settlement.

### Generation

Every hard map contract plus statistical distribution/independence, retry limits, deterministic manifest, fallback pool, dynamic rocks, and presentation footprint. Nightly coverage spans every signature-valid profile and region count.

### UI/input/accessibility

Every route/direct shortcut/Back path, focus state, controller-only flow, confirmation, rejection, responsive composition, map control, bearing cluster, four-boss HUD, pause, disconnect, remapping, localization expansion, settings preview/revert, reduced-effects modes, and screen resolution.

### Persistence/platform

Every schema migration, atomic failure point, corruption fallback, settlement idempotency, recovery round trip, cloud relation/conflict, offline operation, device-local settings, reset/archive, and platform adapter fake.

### Assets/build

Every manifest, license, import, budget, name, clip, socket, LOD/VAT, localization, export include/exclude, dependency license, and clean-checkout build.

## Reference models

For algorithms where one implementation could repeat its own bug, maintain deliberately simple slow reference logic in tests:

- brute-force spatial queries and collision candidates;
- exhaustive small-graph route/bridge checks;
- direct price/resource/DPS calculations;
- unoptimized site-constraint validation;
- sequential damage/control resolution; and
- canonical save/profile comparison.

Random/property tests compare optimized results with the reference within declared numeric tolerance.

## Numeric tolerance

- Integer currency, ranks, ticks, counts, schedule boundaries, and IDs require exact equality.
- Derived displayed whole Hull values require exact equality after documented rounding.
- Floating geometry uses central absolute/relative tolerances based on world scale and operation; each assertion names the tolerance.
- DPS/throughput comparisons use exact fixture inputs and a documented tolerance small enough to catch one missed tick/activation.
- Screenshot comparison uses region masks and perceptual thresholds but critical geometry overlay pixels have tighter explicit checks.

“Approximately equal” without a named tolerance is not an acceptable test.

## Negative control adequacy

A negative control counts only when it fails for the reason the gate exists; the rules below name the failures that a control passing on its own terms still lets through.

- Reach and arity are different axes. A negative control must exercise the cardinality a gate claims to enforce, not only whether the check visits every position. Reach asks whether the check reaches each position; arity asks how much one answer licenses. Reach failures live in the enumeration and get attacked first, because enumerating positions is the natural way to attack a walker. Arity failures live in the aggregation, in a `break`, an `Any()`, or a flag hoisted one scope too high, and they survive every reach attack. Name the cardinality the gate enforces, then write a control containing two of the guarded thing where only one satisfies the rule. The instance that produced this rule: one `x-authority` annotation licensed every numeric bound in the same subschema, on a gate that already had a parameterized control across all nine bound keywords plus a coverage assertion.
- A negative control must be a coherent violation, not a broken state. If the injected violation also breaks the environment, through an unparseable file, an absent binary, or a failed subprocess, a red result is ambiguous between the gate catching the violation and the gate falling over for an unrelated reason, and a green result is worse still. Inject the smallest well-formed thing that the rule forbids.
- An invariant asserting that two sets match is blind to a correlated deletion from both sides. Removing a member from each side keeps them equal and the assertion passes. Such an invariant needs a third anchor that names the expected members or their count independently of either side.

## Claim and measurement discipline

The rules below govern claims an author makes about data and about measurements. Each defends against a failure that survives review of the implementation, because the implementation is faithful to the claim and the claim is what is wrong.

- Check a stated rule against the corpus before implementing it. State the rule, name the corpus that would falsify it, have someone run it over that corpus, and only then write the code. This is not the same as review. Two artifacts produced from the same source by the same process, hours apart, read the same ten prose strings oppositely, and the one that reached code was the wrong one. Reviewing that implementation would have found a schema that was internally consistent and confidently wrong, because the code faithfully implemented a misreading; there was nothing in the code to catch. Only checking the claim against the data separated the two readings, and a reviewer cannot do it for you because the reviewer is reading the same artifact. Pre-clear only where being wrong is expensive to unwind, such as a rule touching many files, a rename, or a grammar other work will be written against; if the rule statement would be longer than the implementation, build the thing. The instance that produced this rule: a proposed grammar's own stated precedent thinned from 38-to-2 to 13-to-6 to 10-to-2 under measurement, its mandatory prefix produced zero collisions on the corpus it was designed to protect, and 25 of the strings it would have migrated turned out to be the sole carrier of player-facing text. All four were empirical claims in a design document that would otherwise have become code.
- An expected-difference set is only evidence if it is visible before the result. For any pass that asserts that the only differences are an enumerated set, commit the enumerated expected differences and the derivation that produced them in their own commit, before running the measurement. Otherwise a reader cannot distinguish an expectation determined in advance from one fitted to the outcome, because in both cases the only evidence is that the two agree. Two properties are at stake, they defend against different failures, and a proof can have the second without the first:
  - pre-registration, a committed record fixing the expectation before the result, protects against fitting the expectation to the outcome; and
  - re-derivability from a prior artifact, where the expected figure is recomputable from the state that predates the change without reference to the diff, protects against the expectation being arbitrary and lets a third party check it without trusting anyone's account of when it was computed.
- A measurement's scope travels with it. A figure measured over one population and described over another is a true measurement carrying a false sentence, and nothing about checking the figure can detect it. Name the population in the same sentence as the number. Two instances worth citing: a duplication count of “168 across 12 files” was arithmetically consistent under a method nobody intended, since 14 times 12 and 14 times 13 minus 1 are the same number, so a reader checking the arithmetic confirmed it; and a value-preservation proof reported as “no gameplay number changed” was measured over numeric leaves in a change where every altered value was a string.

None of the three is enforced by a gate. They constrain what an author may write, not what a runner may check.

## Flake policy

- Tests do not use wall-clock sleeps for simulation behavior.
- Randomness is seeded and logged.
- Async persistence/build tests use explicit completion signals and bounded timeouts.
- Platform-dependent expectations declare platform tags.
- A flaky required test is a defect. Quarantine requires owner, issue, reason, expiration, and equivalent protective gate; repeated retries do not convert failure into success.

## CI suites

### Fast pull-request suite

- format/analyzer/build with warnings as errors;
- JSON schema/content/localization/asset-manifest/license validation;
- pure unit/property bounded/golden tests;
- headless representative simulation and map seeds;
- Godot headless import and focused integration tests;
- changed UI/render captures where applicable; and
- generated-artifact staleness/diff check.

### Main-branch suite

Adds Windows/Linux debug exports, packaged smoke flows, broader simulation/modifier/map matrices, migrations, recovery, and performance smoke.

### Nightly suite

Adds 10,000+ map seeds per defined partition, full 35-minute sweeps, long soak, pairwise content matrix, all screenshot/accessibility layouts, fuzzed saves/content, and benchmark trend reports.

### Release-candidate suite

Adds signed/final-like packages, clean machines, Steam sandbox/cloud, retail Steam Deck performance, controller families, resolution/window modes, upgrade from every shipped save schema, license/notices/SBOM, crash/recovery, and manual usability checklist.

## Acceptance evidence

Every work item identifies registered verification IDs before implementation. Completion evidence includes:

- commands/suites run and results;
- generated reports/captures/benchmarks where relevant;
- requirement and gameplay links;
- remaining risks or deliberately deferred validation; and
- no unexplained warnings, skipped tests, or changed goldens.

An agent may not declare completion based solely on compilation or visual inspection.

## Coverage policy

Line/branch coverage is diagnostic, not the acceptance target. Pure domain libraries should maintain high coverage, but required behavior/edge matrices and mutation-sensitive tests matter more. CI reports untested public behavior registrations, content definitions without fixtures, and requirements without verification IDs.

## Manual and usability testing

Automated tests cannot establish combat feel, readability, balance, or comprehension. Each milestone provides deterministic scenarios and capture forms for:

- mining boundary/decay understanding;
- weapon/relic effect readability;
- map and radar navigation;
- fabrication/irreversibility comprehension;
- controller flow and handheld text;
- threat/telegraph visibility under peak effects; and
- progression/settlement clarity.

Engineering tasks and M0–M4 technical gates do not wait for a human session when all deterministic, screenshot, input-script, readability, and benchmark evidence passes. Agents record the scenario build/seed and an empty standardized observation form so a later human session can add subjective findings without reconstructing context. Human findings change gameplay numbers/content through the design workflow, not hidden technical compensation.

## Related documents

- [Performance, Diagnostics, and Observability](./90-performance-diagnostics-and-observability.md)
- [Content Data and Validation](./40-content-data-and-validation.md)
- [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md)
- [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md)
