---
doc_id: TDD-BUILD-RELEASE
title: Build, Dependencies, and Release Operations
status: active
authoritative: true
---

# Build, Dependencies, and Release Operations

## Purpose

This document defines repository structure, pinned toolchain, dependency policy, build configurations, CI, Godot import/export, artifacts, Steam packaging, secrets, versioning, release gates, and reproducible agent commands.

## Repository structure

The repository uses this structure from FND-001 onward:

```text
MechaMiner.sln
global.json
Directory.Build.props
Directory.Packages.props
build.sh                      macOS/Linux workflow wrapper
build.ps1                     Windows workflow wrapper
game/                         Godot project and the only Godot-dependent C# project
  project.godot
  MechaMiner.Game.csproj
  scenes/
  shaders/
  presentation/
src/
  MechaMiner.Simulation/
  MechaMiner.Content/
  MechaMiner.Diagnostics/
  MechaMiner.Persistence/
  MechaMiner.Tools/
tests/
  MechaMiner.Simulation.Tests/
  MechaMiner.Content.Tests/
  MechaMiner.Diagnostics.Tests/
  MechaMiner.Persistence.Tests/
  MechaMiner.Game.Tests/
content/                      source JSON and localization
assets-source/                retained editable/original assets
assets-runtime/               Godot import sources and derived runtime assets
assets-manifest/              asset and license records
generated/                    reproducible bundles/reports, clearly marked
docs/                         gameplay and technical specifications
build/                        scripts/configuration, not build output
artifacts/                    ignored local outputs
```

The one solution references all C# projects. `game/MechaMiner.Game.csproj` is the Godot project; pure libraries and tools remain buildable/testable without engine assemblies. Changing these top-level ownership directories requires updating the [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md) and architecture tests in the same task.

## Toolchain pinning

Pin in version-controlled files:

- exact Godot 4.7.1 .NET editor and matching export templates;
- exact supported .NET SDK through `global.json` or equivalent;
- NuGet dependency graph and lock files;
- Blender major/minor/patch used by derivation scripts;
- content/schema/map/random/save generator versions;
- formatting/analyzer configuration;
- Steamworks.NET Standalone 2025.164.1 and matching Valve Steamworks SDK 1.64/native redistributables when PLT-001 begins;
- container/runner image identity where CI uses one.

A bootstrap command verifies/downloads approved public tools or prints exact manual installation instructions and checks hashes. It never mutates global developer configuration silently.

Minor/major tool upgrades are separate work items with clean import, full tests, representative saves, screenshots, performance, and export evidence. Godot patch updates follow the technical foundation policy.

## C# project standards

- Nullable reference checking enabled.
- Current supported C# language version pinned, not `preview`.
- Built-in .NET analyzers enabled; project warnings treated as errors in CI.
- Formatting and naming enforced through `.editorconfig` and one repository command.
- Deterministic builds enabled where supported.
- Release binaries do not include development cheats or arbitrary command execution.
- Unsafe code disabled by default; a measured hot-path exception requires isolated ownership, tests, and a TDR if it becomes architectural.
- Reflection-based gameplay registration and runtime assembly scanning are avoided; generated/explicit registries make missing behavior a build error.

Use NUnit for pure managed tests with only its required adapter/runner packages. Use a small project-owned Godot integration harness rather than adopting a large engine test plugin before the foundation spike proves a need. BenchmarkDotNet may support microbenchmarks, while target-device end-to-end metrics remain authoritative.

## Dependency policy

Prefer platform, .NET, and Godot capabilities before adding packages. A dependency request records:

- exact problem not reasonably solved by existing capabilities;
- package/source, owner activity, version, license, transitive graph, platform support;
- runtime/build/test-only scope;
- security history and update policy;
- binary/native code and Steam Deck implications;
- save/content/API reach and exit/replacement strategy; and
- bundle-size/performance effect.

Runtime dependencies receive stricter review than build/test tools. Do not add a generic ECS, dependency injection container, serializer, reactive framework, UI framework, scripting runtime, or logging stack unless measured complexity justifies it against project-owned narrow code.

Package versions are exact locked. CI restores in locked mode and fails if lock files would change. Automated update proposals still run the complete affected gate.

## Build configurations

| Configuration | MSBuild identity | Optimization | Diagnostics | Intended use |
| --- | --- | --- | --- | --- |
| Debug | `Debug` | low | assertions, full logs, debug overlays/actions, symbols | local correctness development |
| Development | `ExportDebug` | optimized | metrics, profiler markers, debug tools, symbols | internal demo, balance, performance diagnosis |
| Release | `ExportRelease` | optimized | bounded sanitized logs, crash/recovery, no debug actions | external shipping candidate |

There are exactly three configurations, and the two columns name the same three things at two layers. The workflow name is the vocabulary of this document and of the wrapper's `configuration` argument. The MSBuild identity is the configuration name the compiler and the engine use, and it is not free choice: `Godot.NET.Sdk` declares `Debug;ExportDebug;ExportRelease` in its own SDK properties, and Godot's tooling only ever asks for those three — the editor builds `Debug`, and an export preset's export-with-debug flag selects `ExportDebug` or `ExportRelease`. A fourth MSBuild configuration named `Development` would therefore be one that no Godot export could produce, so `Development` maps onto `ExportDebug`, whose intent — an optimized build that still carries diagnostics and symbols — is exactly the `Development` row.

Every project in the solution declares the same three MSBuild configurations, including the pure libraries, because a project reference built from the Godot project inherits its configuration. `Microsoft.NET.Sdk` understands only `Debug` and `Release`, so optimization and the diagnostic symbol are set explicitly per configuration rather than inherited from the SDK's defaults. Project code selects behavior through the per-configuration diagnostic symbol, not through the engine's own `DEBUG` symbol, which `Godot.NET.Sdk` also defines for `ExportDebug`.

Restore is configuration-independent and produces one committed lock file per project. `Godot.NET.Sdk` references its editor assembly only under `Debug`, so restoring under `ExportRelease` would rewrite that project's lock file and break every later locked restore; a single restore at the default configuration produces the superset dependency graph that all three configurations build against.

All configurations consume the same gameplay content bundle unless an explicitly labeled test bundle is selected outside standard flow. `Development` must be representative enough for performance after accounting for known instrumentation overhead.

## Standard command surface

The root wrappers `./build.sh` and `./build.ps1` expose identical verbs and argument names. They are thin launchers for pinned tools and project-owned typed tooling; domain workflow logic is not duplicated between shell languages.

| Verb | Required effect |
| --- | --- |
| `doctor` | verify exact Godot/.NET/Blender/tool/template availability and hashes without mutating global state |
| `bootstrap` | restore/download allowed repository-local tools, then run `doctor` |
| `format` | format owned text/code and fail if the resulting tree still violates policy |
| `format-check` | validate formatting without writes |
| `build` | locked restore, analyzers, warnings-as-errors compilation |
| `test-fast` | pure bounded tests, content validation, representative headless fixtures |
| `test-main` | fast suite plus Godot integration, package smoke prerequisites, broader matrices |
| `test-nightly` | exhaustive seeds, full runs, soak/fuzz/screenshot/performance trend suites |
| `content` | compile/validate canonical content and emit generated reports/hash |
| `godot-import` | clean headless import/check with captured warnings |
| `run` | launch the normal local development build |
| `scenario <id>` | run a named deterministic development scenario, including M2/M3/M4/PERF/WB IDs |
| `map --seed <seed>` | generate, validate, visualize, and report one reproducible map |
| `map-batch <partition>` | run the named seed/profile/region audit partition |
| `benchmark <id>` | run a named WB/PERF scenario and emit its canonical report |
| `export <windows|linux> <development|release>` | headless import and named Godot export preset |
| `package-demo` | build and validate the Steam-independent M4 internal-demo artifacts |
| `release-validate` | run release gates and generate manifest/checksums/notices/SBOM without publishing |

Every verb is noninteractive, returns nonzero on failure, writes structured evidence beneath `artifacts/`, and prints a concise final result plus artifact paths. Unknown verbs/arguments fail with usage. CI calls these same wrappers instead of recreating workflows. Publishing, Steam upload, signing, and credential use remain separate explicitly authorized operations and are never side effects of `release-validate`.

Stable process exit classes are: `0` success, `2` invalid verb/arguments, `3` missing or mismatched pinned environment, `4` validation/test failure, `5` build/import/export/package failure, `6` performance/budget failure, `7` authorization/credential/external-state action required, and `8` unexpected tool-internal failure. There is deliberately no `1`: a wrapper that returns it has leaked an unclassified failure from an underlying tool. More detailed stable diagnostic codes live in structured output; wrappers preserve the owning tool's class rather than returning success after partial work.

Every verb in the table above is registered from the moment the command surface exists, with its final argument names, even when the work package that owns its behavior has not landed. Invoking such a verb validates its arguments and then returns class `2` with a distinct stable diagnostic code and the owning work-package ID, so it is nonzero, typed, and distinguishable in structured output from an unknown verb. This class is closed at eight members and none of them denotes "not implemented yet"; the diagnostic code carries that distinction, which is what this section already assigns structured output to do.

## Godot import and export

Commit `project.godot`, import sidecars, and nonsecret `export_presets.cfg`. Do not commit `.godot` cache or `export_credentials.cfg`; Godot documents export credentials as confidential and supports headless command-line import/export with committed presets. [Godot export documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_projects.html)

Named presets:

- Windows Development x86-64
- Windows Release x86-64
- Linux/Steam Deck Development x86-64
- Linux/Steam Deck Release x86-64

Internal demo artifacts need no Steam client and use the local platform adapter. Release-like Steam artifacts include the platform adapter but must still launch gracefully with Steam unavailable according to policy.

The build runs headless import before export, captures importer errors/warnings, and uses the exact matching templates. Release filtering excludes source assets, test fixtures, debug scenes, diagnostics commands, development content, temporary captures, and credentials.

## Version and build identity

Use semantic product version plus monotonically increasing CI build number and source commit. Every executable/about screen/diagnostic header/result manifest includes:

- product version and build number;
- source commit and dirty flag for local builds;
- Godot and .NET versions;
- content bundle hash;
- schema/map/random/save versions; and
- build configuration/platform.

Development builds show identity unobtrusively in debug surfaces. Release builds expose it in Settings/Support.

Identity has exactly one owner, `CMP-OBS-001` in `MechaMiner.Diagnostics`, and exactly one representation, the `SCH-BLD-001` manifest. The values are baked into that assembly's metadata at compile time from `build/version-identity.props`, `global.json`, and the source working tree, and every surface reads them back from the loaded assembly rather than probing its own environment. That is what makes "the tool, the game, and diagnostics report the same identity" a testable equality rather than three independent derivations that could disagree about platform, configuration, or working-tree state.

Every field of identity is stable across two clean builds of the same source, so identity cannot break the deterministic-build requirement above. Nothing in it is a timestamp. A build tree that is not a git working tree records the source commit as `unavailable` and the dirty flag as `unknown` instead of reporting a clean build it cannot observe.

The `build` verb writes the manifest to `generated/build-manifest.json` and reads it back. That file is a build output and is not committed: it names the source commit of the build that produced it, so a committed copy could never be current at the commit that contained it. The staleness relation that matters — does the manifest match the assembly just built — is asserted at emission and classified as current, stale, missing, or unreadable. Per-artifact release manifests, one for each packaged platform and configuration with its checksums, are the [Artifacts](#artifacts) obligation and belong to the release-packaging package; `SCH-BLD-001` already carries the artifact list so its shape does not change when they land.

## Continuous integration

### Pull request

Run the fast suite from [Verification Strategy](./91-verification-strategy.md), including clean locked restore, format/analyzers/build, pure tests, content/assets/licenses/localization, representative headless simulations/maps, Godot headless import/integration, and generated-file staleness.

### Main branch

Produce Windows and Linux development exports and run packaged smoke scripts. Publish test/content/map summaries and symbols as bounded artifacts.

### Nightly

Run exhaustive seed/simulation/fuzz/soak/screenshot matrices and performance trend jobs available on controlled runners. Target-device Steam Deck benchmarks may be manually or remotely triggered but use the same manifest/report format.

### Release candidate

Run the complete release-candidate suite, package final-like Windows/Linux builds, validate Steam sandbox/cloud on a staging app/depot, and produce release manifest, checksums, notices, SBOM, symbols, and rollback artifact.

CI jobs start from clean checkouts and cannot depend on a developer's Godot import cache, global NuGet cache contents, uncommitted generated files, or editor state.

## Packaged smoke flow

Automated or scripted smoke must:

1. launch packaged executable with a temporary user-data directory and local platform adapter where supported;
2. reach Hangar with a fresh profile;
3. confirm deployment on a pinned seed;
4. move, mine an installment/geode through a shortened diagnostic scenario, fabricate through real transactions, and pause/resume;
5. spawn/defeat representative enemies and boss through a development scenario manifest;
6. reach success/failure result and persist expected profile state;
7. restart and verify load/history/settings; and
8. exit cleanly with no error/fatal logs.

Release binaries use separately built test hooks only where safe; otherwise run the closest manual/automation path without development commands.

## Artifacts

Each build artifact set contains:

- packaged game;
- SHA-256 checksums;
- release/build manifest and content hash;
- third-party notices and asset attribution;
- software bill of materials for dependencies;
- relevant export/import logs;
- test and content validation summary;
- symbols/debug mapping in a restricted separate artifact; and
- known-issues/release-note draft.

Artifacts have retention by type. Release/rollback builds are retained durably; routine PR builds expire.

## Steam packaging

- Steam App/depot IDs, credentials, and scripts with secrets are stored in CI secret management, never source.
- Depot layout separates Windows and Linux executables where required and shares compatible data only when measured useful.
- Steamworks/native plugin version is pinned and wrapped by the platform adapter.
- Staging branch receives automated build first; production promotion references an already tested immutable artifact rather than rebuilding.
- Steam Cloud file allowlist includes profile and portable settings only.
- Controller support and 1280×800 operation are validated against Steam Deck compatibility expectations.
- Rollback retains the prior depot build and save compatibility analysis.

## Secrets and signing

- No secrets in repository, generated logs, diagnostic packages, export presets, or client content.
- CI injects signing/store credentials only into release jobs with least privilege.
- Logs redact command environment and credential paths.
- Developer/internal demo builds remain unsigned unless platform distribution requires signing; release signing is reproducible as a post-build provenance step.
- Credential rotation does not require source changes.

## Supply-chain and security gates

- Dependency and tool downloads verify expected publisher/hash/signature where available.
- License allowlist and vulnerability scan run in CI; exceptions have owner and expiry.
- Release includes an SBOM and notices.
- Save/content parsers enforce size/depth/count limits and reject unknown fields per schema policy.
- No runtime loading of unsigned arbitrary code, mods, remote scripts, or user-provided assemblies in initial scope.
- Development command surfaces and test content are absent from Release.

## Release gate

No release candidate promotes unless:

- all required tests/content/assets/licenses/localization pass from clean checkout;
- Windows and Linux packaged smoke pass;
- retail Steam Deck PERF-04 satisfies TDR-003;
- save migration/recovery/cloud conflict tests pass from all shipped versions;
- gamepad-only 1280×800 standard flow passes;
- crash/diagnostic/rollback artifacts exist;
- no expired exceptions, unresolved critical warnings, or unowned high-severity defects remain; and
- gameplay/content version changes have corresponding spec and tuning records.

## Related documents

- [Technical Foundation](./00-technical-foundation.md)
- [Verification Strategy](./91-verification-strategy.md)
- [Performance, Diagnostics, and Observability](./90-performance-diagnostics-and-observability.md)
- [Persistence and Platform Services](./70-persistence-and-platform-services.md)
- [Asset Pipeline and Budgets](./80-asset-pipeline-and-budgets.md)
