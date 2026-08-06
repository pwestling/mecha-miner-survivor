# Policy fixtures

Deliberately invalid C# that proves the six build policies `VER-FND-001-006` through
`VER-FND-001-011` name are actually enforced rather than merely declared.

Not every repository build policy. Doc 100 § C# project standards has eight bullets and
two have no fixture here — release-binary content and the no-reflection-registration
rule, both of which have no subject in the repository yet — and several
`Directory.Build.props` properties are declared and unfixtured, including
`AnalysisLevel`, `AnalysisMode`, `ImplicitUsings`, `RollForward`, `IsPackable`,
`DebugType`, and `RestorePackagesWithLockFile`. Formatting is `build/verify-format.sh`'s.
See the header of `build/verify-policies.sh` for the full accounting.

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
| `analyzer/` | `EnableNETAnalyzers` (the `AnalysisLevel` pin is unfixtured) | `error CA2200` | `VER-FND-001-007` |
| `naming/` | `.editorconfig` naming rules with `EnforceCodeStyleInBuild` | `error IDE1006` | `VER-FND-001-008` |
| `langversion/` | `LangVersion` pinned to `12.0`, not `preview`/`latest` | `error CS9202` | `VER-FND-001-009` |
| `unsafe/` | `AllowUnsafeBlocks=false` | `error CS0227` | `VER-FND-001-010` |
| `deterministic/` | `Deterministic=true` | byte-identical rebuild | `VER-FND-001-011` |

Five of the six fixtures assert an **error**, not a warning; `deterministic/` asserts a
byte-identical rebuild instead and says nothing about severity.

`TreatWarningsAsErrors` is proved by `nullable/` and `analyzer/` only. `CS8600` and
`CA2200` are warnings by default and carry no severity override in `.editorconfig`, so
for those two an `error` is exactly what shows `Directory.Build.targets` is in force —
measured: rebuild either with `-p:TreatWarningsAsErrors=false` and the diagnostic
downgrades to a warning and the build succeeds.

`IDE1006` does **not** prove it. `.editorconfig` sets
`dotnet_diagnostic.IDE1006.severity = error` explicitly, so it is an error on its own
authority — measured: rebuild `naming/` with `-p:TreatWarningsAsErrors=false` and it
still reports `error IDE1006`. What that fixture proves is the pair actually under test,
the `.editorconfig` naming rules plus `EnforceCodeStyleInBuild`. `CS9202` and `CS0227`
are errors by default and likewise prove only the property they name. Crediting the
naming fixture with `TreatWarningsAsErrors` overstated the coverage of a policy that
would otherwise have had no negative fixture at all; `build/verify-policies.sh`'s header
carries the same accounting.

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
`build/verify-policies.sh`. A policy without a failing fixture is not *proved*: MSBuild
may well be applying it, and nothing would tell you if it stopped.
