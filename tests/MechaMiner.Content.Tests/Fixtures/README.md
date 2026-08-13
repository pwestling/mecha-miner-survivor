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

## The `reach-*` schema fixtures

A numeric bound is not only found under `properties`. Draft 2020-12 lets one sit
at the root, in `$defs`, in `allOf`/`anyOf`/`oneOf`/`not`, in
`items`/`prefixItems`, in `propertyNames`/`additionalProperties`, behind a
`$ref`, and any number of those nested inside each other. `reach-*.schema.json`
is one file per position, each hiding a bare bound with no `x-authority`, so
that `SchemaAuthorityReachTests` can prove the walk arrives there rather than
assuming it. Writing them found a real hole: a `$defs` declared on a *subschema*
was skipped entirely by `JsonSchemaLoader`, and `reach-nested-defs.schema.json`
loaded clean with an unattributed bound in it.

Six of them — `reach-if`, `reach-then`, `reach-else`, `reach-contains`,
`reach-pattern-properties`, `reach-dependent-schemas` — sit on keywords this
evaluator does not implement, so the whole document is refused with `MMC-5001`
before the bound inside is ever considered. They are kept, and asserted to fail
for *that* reason and no other, because the day one of those keywords is
implemented the fixture stops failing that way and says so.

`no-bounds.schema.json` is the vacuous-pass control: a schema with zero bounds
produces an empty violation list, which is exactly what an empty
`content/schemas` produces. `SchemaAuthorityCoverageTests` asserts the walk saw
a nonzero number of documents and bounds, and this file proves the counter is a
count rather than a constant.

`keyword-named-properties.schema.json` is the control for the other way that
counter can lie. **Every schema keyword is a legal property name.** A schema
declaring properties called `maximum` and `x-authority` handed the structure-blind
walk an object with a bound keyword and an authority keyword side by side, so it
counted a bound that does not exist and suppressed its own finding on it — and
`BoundsSeen > 0`, the assertion that proves the gate looked at anything, was
satisfiable by property names alone. This file declares properties named
`maximum`, `$defs`, `$ref`, `type` and `x-authority`, plus one real unattributed
bound under `capacity` that must still be reported. `SchemaBoundWalkStructureTests`
generalises it: the walk now reads `properties`, `$defs`, `patternProperties` and
`dependentSchemas` as maps of subschemas, and every recognised keyword is asserted
harmless as a property name in each of those four positions, so a keyword
implemented later inherits the control instead of needing a new fixture. The
loader's structure-aware walk reads `properties` through `ReadSubschemaMap` and
never had the confusion; it is asserted on the same file.

`partly-attributed-bounds.schema.json` is the control none of the above was.
Every one of them puts **one** guarded thing in **one** position and proves the
gate reaches it; none of them asks how much one answer licenses. The answer was
"everything in the subschema": `x-authority` was read as a flag on the object,
so `presentation_id` declaring `minLength` and `maxLength` satisfied the whole
gate by attributing either one. This file is that shape — two bounds, one
authority — and both the loader and the corpus walk must name the unattributed
`maxLength`. When you harden a gate here, write the two-of-the-guarded-thing
control alongside the reach control; they are different questions.

`annotation-hiding-a-subschema.schema.json` carries `"title": {"if": {"maximum":
5}}`. An annotation keyword that accepted any JSON value was a hiding place with
a recognised keyword's name on it: the loader never parsed the value as a
subschema, so every parse-time rule stopped at its edge, while the
structure-blind corpus walk went straight in and found the bound. That
disagreement is the fixture's point.

`attributed-bound.schema.json` is the counterpart to `no-bounds.schema.json` for
the per-document coverage check: a document that unambiguously has a bound, so
that an exemption naming it is unambiguously a false claim about the corpus.

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

## What binds the table to the directory

Every one of those gates iterates the table, so the table decides which files are
tested and nothing checks the table against the disk.
`FixtureCorpusCoverageTests` is that check, in both directions:

- a file in `invalid/` with no row is an **orphan** — it runs no test, it looks
  like corpus to anyone reading the directory, and nothing would notice if it
  stopped being invalid. It now fails a test that names the path;
- a row naming a file that is no longer there fails as one sentence naming the
  row, rather than as three `FileNotFoundException`s out of
  `InvalidFixtureCorpusTests`.

The same class shows up per code. `MMC-1004`, `MMC-1005` and `MMC-2005` are each
proved by **two** fixtures. Delete one of a pair — the file, its table row, and
its `tests/verification/DAT-001.json` citation together — and the code is still
provoked by its sibling, so `ContentDiagnosticCodesTests`' provoked-equals-declared
comparison holds with half the coverage and nothing says so. That was verified,
not assumed: the suite went green at 601 tests with `MMC-1005` down to one
fixture. `TheFixturesProvokingEachCodeAreExactlyTheRosterStatedHere` states the
whole code-to-fixtures roster independently of the table, so a deletion fails a
test that names the code *and* the file.

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
