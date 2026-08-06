# Policy fixtures

Deliberately invalid C# that proves each repository build policy is actually
enforced rather than merely declared.

`TASK-FND-001-002`'s completion gate is "deliberately invalid fixture proves each
policy" (`docs/technical/110-implementation-plan-for-ai-agents.md`
§ Concrete M0 bootstrap queue). Each subdirectory here is a tiny project that
inherits the repository's `Directory.Build.props` / `Directory.Build.targets` /
`.editorconfig` policy and **must fail to compile** with a specific diagnostic
ID.

Run them with:

```sh
./build/verify-policies.sh
```

| Fixture | Policy proved | Expected diagnostic | Verification |
| --- | --- | --- | --- |
| `nullable/` | `Nullable=enable` plus warnings-as-errors | `error CS8600` | `VER-FND-001-006` |
| `analyzer/` | `EnableNETAnalyzers` at the pinned `AnalysisLevel` | `error CA2200` | `VER-FND-001-007` |
| `naming/` | `.editorconfig` naming rules with `EnforceCodeStyleInBuild` | `error IDE1006` | `VER-FND-001-008` |
| `langversion/` | `LangVersion` pinned to `12.0`, not `preview`/`latest` | `error CS9202` | `VER-FND-001-009` |
| `unsafe/` | `AllowUnsafeBlocks=false` | `error CS0227` | `VER-FND-001-010` |
| `deterministic/` | `Deterministic=true` | byte-identical rebuild | `VER-FND-001-011` |

Every fixture asserts an **error**, not a warning. That is what proves
`Directory.Build.targets`'s `TreatWarningsAsErrors` is in force: `CS8600`,
`CA2200`, and `IDE1006` are warnings by default and only appear as errors under
the repository policy.

## These fixtures are not part of the product build

- None of them is listed in `MechaMiner.sln`, so `dotnet build MechaMiner.sln`
  and `dotnet test` never see them.
- `build/policy-fixtures/Directory.Build.props` imports the repository policy
  explicitly and turns lock files off, so the fixtures never contribute a
  committed NuGet lock file.
- They reference no package and no project in `src/`, `tests/`, or `game/`.

## Adding a policy

Add the policy to `Directory.Build.props` or `Directory.Build.targets`, add a
fixture directory that violates exactly that policy, register a `VER-FND-001-###`
entry in `tests/verification/FND-001.json`, and add the row to the table in
`build/verify-policies.sh`. A policy without a failing fixture is not enforced.

The last step is not optional bookkeeping. `build/verify-policies.sh` asserts that
the fixtures it enumerates are exactly the projects present under this directory,
in both directions: a project here that the script does not enumerate fails the
gate, and an enumerated fixture whose directory is gone fails it too. A fixture
directory nobody enumerated used to build silently under whatever policy its own
`.csproj` declared, which is how `seventh/` compiled unsafe pointer code with both
gates green.

If a policy the fixtures rely on is not expressible as a `.csproj` property, say
so in `EVALUATED_POLICIES`' comment instead of leaving it implied - the
`.editorconfig` half of `VER-FND-001-008` is recorded there, and in
`tests/verification/FND-001.json`, as a partly open gap.

## Nothing here may declare a build property

Neither `build/policy-fixtures/Directory.Build.props` nor any fixture `.csproj`
may set a property, an `ItemGroup`, a `Target`, or an `Import` of anything but the
repository-root policy. `build/verify-policies.sh` guard 1b asserts that against a
whitelist, and today that whitelist is two properties in the intermediate file
(`RestorePackagesWithLockFile`, which is load-bearing because the root sets it
`true` and the fixtures must not leave lock files behind, and `IsPackable`) and
none at all in a fixture project.

This is not tidiness. Root `RunAnalyzers=false` plus `RunAnalyzers=true` here
compiled a `CA2200` rethrow and an `IDE1006` field name at zero warnings while the
whole gate printed `PASS`. Adding a property here therefore needs an argument,
written into guard 1b's comment, for why it cannot change which diagnostics
appear.

Guard 1b is **not** why the suppression-switch family is closed, and an earlier
version of this section claimed it was. The claim was that a switch takes two
edits to hide - switch it off at the root, switch it back on locally - so removing
the local half closes the family. It does not: the switch does not need switching
back on, it needs never to have applied to the fixtures. A single `Condition` on a
root `PropertyGroup`, such as
`Condition="!$(MSBuildProjectDirectory.Contains('policy-fixtures'))"` over
`<AnalysisModeUsage>None</AnalysisModeUsage>`, compiles a real `CA2200` at zero
warnings with no file here touched at all. Three sibling files do the same and none
of them is a file guard 1b reads: `Directory.Packages.props` here (found by
MSBuild's own upward search and imported *after* `Directory.Build.props`, so it
wins), `Directory.Build.rsp` here, and a fixture's `.csproj.user`. The first two
are not gitignored and are therefore committable.

What closes the family is **guard 4**: it compares each fixture's whole evaluated
property set against `src/MechaMiner.Content`'s and requires equality outside a
measured allowlist of structural differences. Every one of those escapes leaves the
fixture evaluating a different configuration from the product, which is observable
without knowing which mechanism produced it. The fixtures themselves remain the
detector for a root flip applied *uniformly*: their expected diagnostics simply
stop appearing.

There is also no non-root `.editorconfig` anywhere in this repository, and guard 1
fails on one. A copy of the root file placed in `naming/` decoupled that fixture
from the root `dotnet_diagnostic.IDE1006.severity`, which could then be flipped to
`none` over a real violation with the gate still green.
