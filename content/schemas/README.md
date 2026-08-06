# Content schemas

Owner: `DAT-001` (`TASK-DAT-001-001`). Registered as `SCH-CNT-001` in
[the component, contract, and schema registry](../../docs/technical/115-component-contract-and-schema-registry.md#schema-registry),
owned by `CMP-CNT-001`.

## What is authoritative

The typed structural validator in
`src/MechaMiner.Content/Envelope/EnvelopeReader.cs` is authoritative.
[Content Data and Validation](../../docs/technical/40-content-data-and-validation.md#json-codec-and-schema-baseline)
says so directly:

> JSON Schemas use draft 2020-12 for editor/tool interoperability. The
> project-owned typed structural/semantic validators remain authoritative; a
> fixture corpus proves the schema and typed validator accept/reject the same
> structural cases.

So the schemas here are a **mirror**, kept for editors, external tooling, and
review. They are not the gate. What makes the mirror trustworthy is
`MechaMiner.Content.Tests.Fixtures.SchemaAgreementTests`, which runs every
fixture through both and fails if the two verdicts differ.

## Layout

| File | Contents |
| --- | --- |
| `envelope.schema.json` | `SCH-CNT-001`, the nine-field common definition envelope |

Per-category schemas (`SCH-CNT-002`) and presentation schemas (`SCH-CNT-003`)
land with `DAT-002`, `DAT-003`, and their owning packages. Each one composes
the envelope with `"$ref": "envelope.schema.json"` rather than restating it.

## Versioning

A schema's version is the `schema_version` its definitions carry, not a version
on the schema file. Doc 40 § Content compatibility: changing field *meaning*
increments `schema_version`; changing numbers without changing behavior does
not. The initial `schema_version` of every first-authored definition is `1`.

## The evaluator these schemas are checked with

`src/MechaMiner.Content/Schema/` contains a small draft 2020-12 evaluator. It
exists only to run the agreement corpus, and it implements exactly the keywords
these schemas use — listed in `JsonSchemaKeywords`.

**An unrecognised keyword is a load failure, not a no-op.** The specification
says an implementation should ignore keywords it does not know, which is right
for interoperability and exactly wrong for a gate: a schema that silently loses
a constraint still reports "valid", and the gate has quietly stopped being one.
If you add a keyword to a schema here, implement it in the evaluator and test it
in the same change.

No JSON Schema package is used. `MechaMiner.Content` is required to have an
empty dependency edge list, which `build/verify-architecture.sh` asserts.

## Recorded deviation: the `source_refs` element grammar

Doc 40 § `source_refs` element grammar admits four element forms and bans a
file path, a line number, or any `path:line` pair. The accepted catalog uses
three shapes that doc 40 as written does not literally sanction. All three are
accepted, by decision of the integration owner, and doc 40 is expected to be
amended to match. The grammar as implemented:

```
element    := [ scope ": " ] reference
scope      := segment ( "." segment | index )*
segment    := [a-z][a-z0-9_]*
index      := "[]" | "[" digits "]" | "[" digits ".." digits "]"
reference  := ( docref | "DEC-" digits{3} | "TDR-" digits{3}
              | "TR-" [A-Z]+ "-" digits{3} ) [ "#" anchor ]
docref     := ( "GDD-" | "TDD-" ) [A-Z0-9-]+
anchor     := [a-z0-9-]+
```

**1. `TDD-<DOC>` document IDs.**
[Technical Documentation Conventions](../../docs/technical/conventions.md#stable-identifiers)
mints `TDD-<DOMAIN>` as a first-class stable ID, so its absence from doc 40's
list is an omission rather than a prohibition. 40 elements in the catalog use
it, for example `TDD-ENCOUNTERS#elite-construction`.

**2. An optional scope prefix.** A scope attributes one property of the
definition to a different source — `recipe_pair: GDD-WEAPON-CATALOG#...`,
`rules[2..3]: GDD-...`, `minute_rows[33].formation_events[]...: GDD-...`. This
makes traceability per-field instead of per-file.

A scope is *not* the thing doc 40 bans. The ban targets paths and line numbers
because they "move whenever a document is edited, so a reference built from them
decays silently". A scope is a selector into the definition's **own** JSON, and
the typed validator resolves it against that JSON: a scope naming a field the
definition does not have is `MMC-4003`, a build error. It cannot decay silently
because the build fails the moment it would.

**3. An optional `#anchor` on `DEC-###`, `TDR-###`, and `TR-<DOMAIN>-###`.**
Doc 40 allows an anchor only on the document forms. 15 elements in the catalog
anchor a decision — `DEC-120#decision`, `DEC-120#consequences`,
`DEC-121#decision`. A decision record is a Markdown document with stable
headings, so anchoring one is exactly as durable as `GDD-COMBAT#contact-damage`.
What doc 40's ban targets is a *line number*, which moves on every edit; a
heading slug does not.

**The ban itself does not loosen.** `MMC-4002` rejects a file path or a
`path:line` pair under its own diagnostic code, and
`tests/MechaMiner.Content.Tests/Fixtures/invalid/traceability-source-ref-path-line.json`
proves it stays rejected.

## Deliberately not schema'd yet

- **`presentation_id` grammar.** No accepted document mints one. The envelope
  requires a non-empty string; existence is a cross-reference check.
- **The `presentation` category.** Doc 40's layout lists
  `content/presentation/`, but the directory does not exist and no prefix has
  been granted. Minting one is an integration-owner decision.
- **`weapon-stat-price-formula` and the four mining-site aggregates.** See the
  DAT-001 handover notes; these carry IDs that no grammar currently admits.

## `x-authority`: where a numeric bound came from

Every numeric bound in a schema here carries an adjacent `x-authority`, and
`x-authority` is **a map keyed by the bound each entry explains**:

```json
{ "maximum": 2048,
  "x-authority": {
    "maximum": {
      "source": "TDD-COMBAT",
      "section": "Performance and capacity",
      "kind": "sourced",
      "derivation": "legal worst case ~1010; x2 headroom; rounded to a power of two" } } }
```

A subschema that asserts several numbers writes several entries, one per bound:

```json
{ "minLength": 1,
  "maxLength": 4096,
  "description": "the empty string is what an omitted field materializes as",
  "x-authority": {
    "minLength": { "kind": "structural" },
    "maxLength": {
      "source": "TDD-CONTENT-DATA",
      "section": "Limits",
      "kind": "sourced",
      "derivation": "the document limit divided by the worst-case field count" } } }
```

The key is the identity of what is attributed. Attribution used to be per
*subschema* — one `x-authority` beside any number of bounds — so a single entry
licensed every bound next to it, and adding a bare `"maxLength": 4096` beside an
attributed `minLength` was accepted by the loader and by the corpus walk alike.
Provenance is a property of a number, and a subschema can assert several.

Every key must be one of the nine bound keywords, every declared bound must have
an entry, and every entry must have a declared bound: an authority for a bound
that is not there is provenance for nothing, and it would silently cover that
bound the day someone adds it.

| Field of one entry | Meaning |
| --- | --- |
| `kind` | `sourced` (comes from a document), `derived` (follows from other content), or `structural` (an implementation limit with no external authority) |
| `source` | the document ID, in the **same vocabulary as `source_refs`** and validated by the same parser — no scope prefix, no anchor |
| `section` | a heading name, never a line number |
| `derivation` | how the number follows from its source |

`source`, `section`, and `derivation` are required for `sourced` and `derived`,
and must be **absent** for `structural`, whose rationale lives in `description`
instead.

Why both a source and a derivation: the source says *where* a number came from,
the derivation says *why it is that number*, and the two go stale independently.
A cited section can change in a way that invalidates the reasoning while leaving
the bound arithmetically defensible, and nothing catches that except a stated
derivation someone can re-check.

`section` is a heading rather than a line number for the same reason
`source_refs` rejects a `path:line` pair: a heading survives an edit.

A `structural` entry states its rationale in the subschema's `description`, and
`description` has to be a string with something in it. Presence alone was the
rule once, which made `""`, `"   "`, `0`, `false`, `{}` and `[]` all count as a
justification.

The annotation keywords — `title`, `description`, `$comment`, and likewise
`$schema` and `$id` — hold strings. A non-string annotation is a
subschema-shaped value that nothing walks: `{"title": {"if": {"maximum": 5}}}`
loaded clean and hid a bound behind a keyword's name.

**The gate.** `SchemaAuthorityTests` asserts that every `minimum`, `maximum`,
`exclusiveMinimum`, `exclusiveMaximum`, `minItems`, `maxItems`, `minLength`,
`maxLength`, and `multipleOf` under `content/schemas/**` has its own entry in
the adjacent `x-authority` map — per bound, not per subschema — and that every
sourced or derived entry states a derivation. Negative-control fixtures prove
the gate can fail: a bare bound, a bare exclusive bound, a bare length bound, a
bound with a source but no derivation, and a subschema with two bounds that
attributes one of them. The loader half of the control is parameterised over the
keyword list, so a tenth keyword arrives with its control already written. The
schema loader enforces the same rules, so the gate holds from both directions.

`SchemaAuthorityCoverageTests` additionally asserts, per document, that every
schema under `content/schemas/` either declares a bound or is named in an
enumerated list of documents declared bound-free.

**When adding or changing a gate here, write a negative control with two of the
guarded thing where only one satisfies the rule.** Checking that the walk
reaches a position is a different question from checking how much one answer
licenses, and this gate was hardened six times on the first question before the
second one turned up a hole: a single `x-authority` was licensing every bound in
its subschema.

**No exemption list.** The nine are one list: every bound that may be attributed
must be. (The per-document bound-free list above is a different kind of thing:
it enumerates *documents*, a factual claim about the corpus that is settled by
opening the file, rather than *keyword kinds*, which would widen the rule
everywhere at once including in schemas nobody has written yet.) Two earlier
candidates for exemption are worth recording, because both arguments will be
made again.

The exclusive bounds are gated because there is no principled line between "at
most 2048" and "strictly less than 2049" — the same number is being asserted,
and a rule that demanded provenance for one spelling would be asking about
syntax when the question is about the number.

`minLength` is gated for the case the obvious argument misses. It is nearly
always `structural`, and saying so costs one line; the line it buys is the one
place a genuinely sourced length — a localization key length, an ID length taken
from a document — could otherwise sit unattributed. An exemption list is where a
fail-open hides, and the argument for adding one is always that the cases are
obviously structural.
