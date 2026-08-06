# DAT-001 fixture corpus

Owner: `DAT-001` (`TASK-DAT-001-001`). The completion gate for DAT-001 is
"invalid/valid fixture suite" (`docs/technical/110-implementation-plan-for-ai-agents.md`
§ Work packages), and this is that suite.

## These files are deliberately invalid, and that is the point

Everything under `invalid/` is broken on purpose: comments, duplicate
properties, `NaN`, JSON `null`, `camelCase` field names, bad IDs, malformed
`source_refs`. **Do not "fix" them.** Each one exists to prove that a specific
diagnostic fires, and a fixture that stopped being invalid would turn its gate
into a test that passes for the wrong reason.

`build/policy-fixtures/` is the precedent for deliberately-invalid *code* being
kept outside the solution so it cannot fight the build. These are *data*, not
compiled sources, so they cannot break a build by existing — but the same
reasoning applies to a reviewer's expectations, which is why this note is here.

Two of them are worth calling out because they look like ordinary mistakes:

- `codec-malformed.json` is missing its closing brace.
- `codec-root-not-object.json` has an array at the root.

## Layout

| Directory | Contents |
| --- | --- |
| `valid/` | Definitions that must produce **zero** diagnostics |
| `invalid/` | One file per diagnostic code, each proving exactly one failure |
| `canonical/` | Files differing only in property order, indentation, and whitespace |
| `schema/` | Broken schema documents, and the negative controls for the `x-authority` gate |

## What binds a fixture to its expectation

`FixtureCorpus.cs` holds the table mapping each invalid fixture to the one
diagnostic code it must provoke. The table is C# rather than a manifest file so
that every expected code is the `ContentDiagnosticCodes` constant itself:
renaming a constant becomes a compile error instead of a corpus that silently
stops asserting anything.

Three properties are asserted for every invalid fixture
(`InvalidFixtureCorpusTests`):

1. it is rejected;
2. it reports the code it is named for — not merely *some* error, because a test
   that passes on the wrong error is not a gate;
3. it reports **only** that code, so a fixture cannot drift into testing
   something other than its name.

And the control in the other direction (`ValidFixtureCorpusTests`): the valid
fixtures produce zero diagnostics of any severity, so an over-strict validator
fails as loudly as an under-strict one.

## Why some fixtures carry reduced limits

The `limit-*` fixtures are evaluated under a `StrictJsonLimits` with exactly one
ceiling lowered, declared beside them in the corpus table. A fixture that had to
exceed the shipped one-megabyte ceiling would be a megabyte of generated JSON
that no reviewer would read. The shipped defaults are asserted separately, and
the at-limit/one-past boundary is exercised in `StrictJsonLimitsTests` with
documents built in code.

## Codes not provoked by a file here

Two kinds of case cannot be a committed `.json` fixture, and are provoked in
`ContentDiagnosticCodesTests` instead:

- `MMC-1008` (invalid UTF-8) — a committed file with invalid UTF-8 would fight
  the repository's encoding rules.
- `MMC-5001`/`MMC-5002`/`MMC-5003` — these come from schema documents rather than
  from definitions, so they live in `schema/`.

That test asserts the set of codes the whole suite provokes **equals** the set
declared in `ContentDiagnosticCodes`, in both directions.
