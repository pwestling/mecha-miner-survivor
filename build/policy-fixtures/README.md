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
`.editorconfig` half of `VER-FND-001-008` is recorded there as an open gap.
