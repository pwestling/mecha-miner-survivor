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

### Entry status

`status` takes one of exactly three values, and the set is closed:

- `registered` is an entry written before the verification it describes exists. The entry is authored, cited, and numbered; nothing runs yet.
- `implemented` is an entry whose verification exists and runs.
- `retired` is an entry withdrawn from the registry. The tombstone-and-successor rule above is what governs it, and retirement is the only state that carries a companion obligation.

This is the status of an entry, not the state of a task. The [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md) § Work states and integration defines Draft, Ready, Active, Evidence review, Done, and Blocked; those are states of a task, and they deliberately share no word with the three above. The two vocabularies are separate axes and are not to be reconciled by widening either one. A task in Evidence review may own entries that are still `registered`, and an entry may be `implemented` while its task is Blocked on something unrelated. A value from one vocabulary appearing in the other field is an error rather than a synonym, and it is the plausible wrong answer: an author who knows one vocabulary and not the other writes `Done` where `implemented` belongs.

The three values are written down here because until now they were written down nowhere authoritative. They were stated in the registry validator's source, restated in prose `notes` inside individual registry files, and absent from every document. Three copies, none of them authoritative, one of them a comment. Copies agree until they do not, and when they disagree nothing decides which is right: the code and the data can each be internally consistent, agree with each other, and both drift, because there is no authority to drift from. Naming the vocabulary in this document is what makes disagreement with it a defect instead of an opinion.

### What `evidenceKinds` means

`evidenceKinds` is read against `status`, and its meaning changes with it. For a `registered` entry the list is the evidence the verification will produce once it is built, and no check may treat any of it as produced. For an `implemented` entry the list is the evidence that exists and runs, and a check may assert it. Nothing else in an entry distinguishes a record from a plan, and without this rule no check can assert the field in either direction: a check that reads it as a record is wrong about every `registered` entry, and a check that reads it as a plan can never assert anything at all.

The consequence is that promoting an entry from `registered` to `implemented` is a claim that the evidence it lists now exists. It is not bookkeeping that trails the work, it is the assertion the work has to earn, and it is the moment every kind in the list becomes checkable.

### What a kind may name

A kind names the sort of evidence and nothing else. It does not name where the evidence was written, which tool produced it, or the technique that produced it.

- A path is not a kind. A value such as `artifacts/architecture/architecture-forbidden-edges.txt` names a location, so it says nothing about what the evidence is: two entries retaining different files would share no kind, and two retaining the same file would be indistinguishable from two producing the same sort of evidence. A retained artifact belongs in a field that names the artifact, and no accepted entry field holds one today. Adding one, `retainedArtifacts` or a better name, is a proposal this document does not settle; `SCH-QUA-001` in the [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md) § Schema registry is where it would be accepted.
- A technique is not a kind. `negative-control` and `tripwire-sentinel` describe how a gate was built, not what it emitted. The evidence a negative control actually produces is a test count, an exit code, or a diagnostic ID, all of which are kinds already, so recording the technique in this field both omits the evidence and inflates the coverage the field appears to report. Negative-control adequacy is governed by its own section below, where it is a rule about the gate rather than a label in a coverage list.

The second bullet's two values are both in use, and both are excluded from the inventory below rather than grandfathered into it, which is what makes the correction owed rather than optional. The rule is therefore general: every entry whose `evidenceKinds` carries `negative-control` or `tripwire-sentinel` owes a correction that replaces the technique with the kinds the technique actually emits, and each correction is owned by the work package that owns the registry file carrying it.

Which entries those are is not this document's business to record. The registry corpus grows with every stream that merges, so a count written here is a measurement of one ref that the next merge falsifies, and a reader handed such a number cannot tell whether it was ever true or has merely gone stale. Ask the tree instead, at the ref in hand:

```
jq -r 'input_filename as $f | .entries[] | select(any(.evidenceKinds[]?; . == "negative-control" or . == "tripwire-sentinel")) | "\($f) \(.id)"' tests/verification/*.json
```

Every row it returns is a defect of this kind, and an empty result means the correction is complete. Once the registry validator required below enforces the `evidenceKinds` inventory, that check reports the same set as a gate rather than as a search someone has to remember to run.

The first bullet's path is an illustration and not a citation: the value `artifacts/architecture/architecture-forbidden-edges.txt` was invented for this document, so the first bullet states a rule that binds the next author rather than naming an entry to go fix. Whether any registry file has since acquired a path-shaped kind is likewise a question about the corpus at a ref, settled by the same search widened to any kind containing a slash. Where that returns nothing there is nothing to correct for the first bullet, and implying otherwise sends a reader looking for a defect that is not there.

The vocabulary is open, and a kind is minted by adding it to the inventory below before it may appear in a registry file. The inventory as of this revision, including `compilation`, which is minted because the non-compilation rule below presupposes it and not because any entry was found reaching for it: `absent-value-assertion`, `artifact-sha256`, `assembly-metadata`, `assembly-sha256`, `benchmark-report`, `benchmark-report-json`, `build-manifest-json`, `canonical-ordering`, `captured-artifact`, `changed-file-list`, `command-exit-code`, `command-output`, `compilation`, `diagnostic-run-record-json`, `drop-counters`, `engine-log`, `expected-diagnostic-id`, `file-hash`, `frame-budget-table`, `golden-text`, `identity-equality`, `import-log`, `log-line`, `minimized-input-artifact`, `msbuild-property-values`, `pin-comparison`, `registry-report`, `rotated-file-set`, `runner-report-json`, `seed-identity`, `sink-touch-count`, `stdout-line`, `surviving-file-assertion`, `test-counts`, `test-output`, `text-diff`, `timeout-observation`, `verb-result-json`, `warning-count`. The inventory carries near-duplicates that a later revision should consolidate, among them `stdout-line` against `command-output` and the three hash kinds against one another; consolidating them edits registry files owned by their packages and is not done here.

`benchmark-report` is minted by this revision. It passes the test the bullets above state, because a benchmark scenario emits a canonical report and that report is a file a reader opens. The entry reaching for it is `VER-SIM-001-013`, the `PERF-04` proof gate for the tick catch-up bound, whose status is `registered` and whose gate is not built; that is correct use rather than an unused kind, because on a `registered` entry the list is the evidence the verification will produce, so minting a kind before its artifact exists is exactly what `registered` is for, and the defect would be the unminted kind rather than the unbuilt gate. That entry arrives with `tests/verification/SIM-001.json`, so on a branch without `SIM-001` the pointer does not yet resolve; a reader there should treat it as naming where the kind is reached for rather than as a broken citation.

Whether `benchmark-report` and `benchmark-report-json` are two kinds or one is an open question this revision does not settle, and it is stated here rather than left for a reader to notice. Nothing distinguishes them today. The distinction turns on the form the report actually takes, which is a design decision belonging to whoever builds `PERF-04` and not a measurement anyone can take now: if that report is the `SCH-OBS-002` performance report, the two kinds name one artifact, `benchmark-report` is withdrawn from the inventory above, and every entry reaching for it is repointed at `benchmark-report-json`. If instead the scenario emits a report in some other form, this paragraph is replaced by a sentence saying what separates the two. Either way the resolution is owed by that work package, and until it lands a reader choosing between the two values should read the pair as unresolved rather than as a distinction they have failed to grasp. Minting the kind is what makes the question visible; leaving it unminted while an entry used it would have hidden the same overlap in a registry file.

Two values found in registry files are deliberately not minted. `diagnostic-counter` and `reference-identity` name a counter's observed value and object identity between two references, and both are measurements rather than forms of artifact; what a reader opens in either case is the test output, which is a kind already. `reference-identity` had itself replaced `allocation-count`, which is the shape this test is most useful against: a wrong value swapped for another value of the same wrong category, with nobody asking whether the field wanted an artifact form at all.

The list is not closed, and closing it would be a mistake dressed as rigor. Every kind above was minted by toolchain, wrapper, harness, architecture, diagnostics, and benchmark work, because those are the packages that have landed. Simulation, content, persistence, UI, performance, and device verification will need kinds nobody has written yet, such as image goldens, timing percentiles, save-migration outcomes, and replay divergence. A set closed against whichever packages happened to arrive first is widened on first contact with the next one, and a vocabulary widened whenever it is inconvenient was never closed. Requiring an edit to this document gives the field an authority without pretending the list is finished.

### What `successor` means

`successor` names the `VER-*` entry that replaces a retired one. It is a property of retirement: a `retired` entry must carry it, and no other status has anything to name. It does not name a task.

The field is misused in the registry in a single recognizable shape, which is two defects at once: a task or work-package ID where a verification ID belongs, and a `successor` on an entry whose status is not `retired`. What such an entry means is that a later task is expected to replace the verification, and that is not a successor, it is planned work. Record it in the entry summary or the registry file's notes, and set `successor` only when the entry is actually retired and its replacement has a `VER-*` ID.

How many entries are in that state, and which, is a property of the corpus at a ref rather than of this document, for the same reason a kind census is. The test is mechanical, so apply it rather than trusting a number:

```
jq -r 'input_filename as $f | .entries[] | select(has("successor")) | "\($f) \(.id) \(.status) -> \(.successor)"' tests/verification/*.json
```

A row is correct only when its status is `retired` and its target is a `VER-*` ID. Every other row is a defect owed by the work package that owns the file.

### Validation and reporting

The registry validator must enforce unique IDs, existing requirements/sources/selectors/scenarios, the closed `status` vocabulary and the `evidenceKinds` inventory above, at least one non-compilation verification for every implementation task, and no accepted behavior registration/content definition without coverage. `FND-009` owns it, under `TASK-FND-009-002`. This paragraph states what the validator is required to enforce, not what runs in a given revision; which rules are live is recorded by the `VER-FND-009-*` entries and their statuses, which is the only place that question is answered. Describing a gate in the present indicative reads as a report that it runs, and leaves a reader unable to tell an enforced rule from an intended one.

Test runners emit executed verification IDs into the task evidence bundle. `PERF-*` and `WB-*` remain scenario IDs referenced by one or more `VER-*` entries.

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

- A claim travels; a pointer to a claim does not. A commit message, pull request body, or review that cites "items 1-8, 11" into a list held only in its author's context makes its own claims permanently unauditable: the numbers survive in history and the list does not, so the next reader can neither resolve an item nor tell a finished one from an outstanding one. Carry the claim instead — one line per item saying what the item is costs a few hundred bytes and stays true. Where a numbered list must be referenced, cite the artifact by URL or path plus sha and say which numbered list it is, because a project with two reviews that each number findings 1 through 9 makes a bare "item 6" ambiguous even for someone holding both. The same applies to a message that describes its own work as unverified: that is a statement with a shelf life, so plan the commit that supersedes it, write the verification where a reader of the registry will meet it, and name the earlier commit in the later message so `git log --grep` connects them. History is append-only, and a message on a branch others have merged cannot be corrected in place; a `git note` on the original is the only mechanism that reaches a reader of `git log` without rewriting anything. The instance that produced this rule: a commit enumerating nine review-fix items reached four of six stacked pull requests, its list existed in no artifact on any ref, two of its nine items resolved to unnumbered correction bullets rather than to findings, and one referenced a sentence that had never existed in any file and whose originating claim had been retracted in conversation — a retraction that never reached the message. The non-normative `docs/technical/delivery-waves.md` § Decision 12 records that instance in full.

- A prose count over a corpus that grows as branches merge is a measurement of one ref, and the next merge falsifies it. Before writing a number, ask whether merging a branch could change it. A set is closed if merging cannot: the `status` vocabulary and the `evidenceKinds` inventory above change only by editing this document, so a count over them counts this document, which is why the closing line of this section may count its own bullets. Everything over `tests/verification/*.json` is open, because that directory gains a file per work package, and so is any count over `content/`, `docs/`, or the test suite. A historical figure recording a past measurement is fixed at the time it was taken and is not a claim about the current tree, provided the sentence says so. Where the set is open, state the rule in prose and delegate the enumeration to a search the reader can run, giving the command, so that a reader on any ref gets the answer for the ref in hand; where a count is genuinely load-bearing, name the ref and the sha it was measured at in the same sentence as the number; and where the number's whole job is to settle a choice being made now, take the count, use it, and write it down nowhere. There are three options and not two, and the third is more often the right one than it looks. The test that selects it is whether a reader of this document later needs the number or whether only the person choosing needed it: a decision is consumed at a moment and a document is read at every later moment, so a count whose job is finished when the choice is made becomes a claim with no remaining purpose and a guaranteed expiry — as in this rule's own commit, where resolving the overlap between `benchmark-report` and `benchmark-report-json` turned on `benchmark-report-json` having zero current users, a fact that made one of the two resolutions cheap and was genuinely load-bearing in choosing it, and which is a count over `tests/verification/*.json`, the open corpus this rule is about, so writing it into the text would have been this rule failing on its first use; the number was worth having in the decision, was used there, and was deliberately left out. That a count is currently true is not a defence and is in fact the failure mode, because the author verifies on the branch in hand and the reader arrives from a different one. Two instances worth citing, both of them claims this document made about itself: it said `negative-control` and `tripwire-sentinel` appeared once each, on `VER-FND-003-011`, and that this was the whole of it, which held at `5589ad1` and was 13 entries against 1 at the sibling ref `claude/hearth-thread-3aamx2` (`e17ccf6`), where seven `SIM-*` registry files exist that `5589ad1` does not have; and it said that every entry carrying `successor`, three of them, carried a task ID while its own status was `implemented`, which at that same sibling was wrong three ways — seven entries rather than three, two of them naming `QUA-005`, which is neither a task ID nor a `VER-*` ID, and one, `VER-SIM-006-003`, being a correct use in which a `retired` entry names a `VER-*` successor and so falsifies the `implemented` half outright.

None of the five is enforced by a gate. They constrain what an author may write, not what a runner may check.

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
