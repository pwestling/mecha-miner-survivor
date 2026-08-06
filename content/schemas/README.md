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
  "description": "prose about this field, for a reader",
  "x-authority": {
    "minLength": {
      "kind": "structural",
      "rationale": "the empty string is what an omitted field materializes as, so an authored empty string would be a second way to say 'absent'" },
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
| `rationale` | why a structural bound is this number |

**The `kind` decides the field set exactly, in both directions.** `source`,
`section`, and `derivation` are required for `sourced` and `derived` and must be
**absent** for `structural`; `rationale` is required for `structural` and must be
**absent** for the other two. Every one of the five is a string wherever it
appears.

`rationale` is absent rather than optional on a sourced or derived bound because
those already state a `derivation`, which answers the same question — *why is it
this number*. Two fields asking one question mean neither is the one to read, and
the redundant one is the one that fills with filler. Which field carries the
justification is settled by the `kind` rather than by whoever wrote the entry.

Why both a source and a derivation: the source says *where* a number came from,
the derivation says *why it is that number*, and the two go stale independently.
A cited section can change in a way that invalidates the reasoning while leaving
the bound arithmetically defensible, and nothing catches that except a stated
derivation someone can re-check.

`section` is a heading rather than a line number for the same reason
`source_refs` rejects a `path:line` pair: a heading survives an edit.

**The rationale is a field of the entry, and `description` has no checking role.**
It used to be the rule that a `structural` bound's rationale lived in the
subschema's `description`, which is the same arity failure the `x-authority` map
had just been reshaped to fix, sitting one field over: a `description` belongs to
the *subschema*, so one sentence licensed every structural bound under it. A
subschema asserting two structural bounds beneath `"description": "the envelope is
bounded"` satisfied the rule for both, and nothing could check which clause
covered which number.

The `description` check was **deleted** rather than kept alongside the new one.
Two checks for one thing is worse than either alone: the weak check is the one
people satisfy, so a shared `description` would go on passing for two unrelated
bounds while the strong check sat beside it looking like coverage. `description`
is now prose for a reader and nothing more. The presence-only spelling of the old
rule — which accepted `""`, `"   "`, `0`, `false`, `{}` and `[]` as a
justification — retires with it; `rationale` is a string with non-whitespace
content in it.

The annotation keywords — `title`, `description`, `$comment`, and likewise
`$schema` and `$id` — hold strings. A non-string annotation is a
subschema-shaped value that nothing walks: `{"title": {"if": {"maximum": 5}}}`
loaded clean and hid a bound behind a keyword's name.

**The same is true of every field inside `x-authority`, and there it is worse.**
The five were read as "a string if it is a string, otherwise absent", so
`{"kind": "structural", "source": {"if": {"maximum": 5}}}` read as a structural
entry declaring no source: the loader raised nothing, and the corpus walk steps
over `x-authority` wholesale by design, precisely so that the annotation's own
keys are not counted as bounds. Between them the subschema parked under `source`
was reached by neither — strictly worse than the `title` case, where the blind
walk at least still found the bound. Each field is now string-typed where it
appears.

**The gate.** `SchemaAuthorityTests` asserts that every `minimum`, `maximum`,
`exclusiveMinimum`, `exclusiveMaximum`, `minItems`, `maxItems`, `minLength`,
`maxLength`, and `multipleOf` under `content/schemas/**` has its own entry in
the adjacent `x-authority` map — per bound, not per subschema — that every
sourced or derived entry states a derivation, and that every structural entry
states a rationale. Negative-control fixtures prove the gate can fail: a bare
bound, a bare exclusive bound, a bare length bound, a bound with a source but no
derivation, a subschema with two bounds that attributes one of them, a structural
bound whose entry states no rationale, and two structural bounds sharing one
subschema `description` — which must be reported **twice, naming both**, since the
guarded thing is a bound and a check that stopped at the first would be counting
annotations. The loader half of the control is parameterised over the keyword
list, so a tenth keyword arrives with its control already written. The schema
loader enforces the same rules, so the gate holds from both directions.

`SchemaAuthorityCoverageTests` additionally asserts, per document, that every
schema under `content/schemas/` either declares a bound or is named in an
enumerated list of documents declared bound-free. That list names
**repository-relative paths**, not file names. The glob is recursive, so
`content/schemas/a/x.schema.json` and `content/schemas/b/x.schema.json` would be
one entry to a list keyed by name, and one exemption would waive both — the same
arity failure as a shared `x-authority`, one level out from the bound. It is
latent while the corpus is a single flat file, which makes the control the whole
of the guarantee: two documents with colliding names, one exempted, and the other
still reported by name.

**A position no reader visits is worse than a rule that accepts too much.** Every
hole above was a check that said yes when it should have said no, and each one
could be repaired by tightening the reader that was already there. Two were not
of that kind. The five fields inside an `x-authority` entry could park a subschema
that the loader never parsed and the corpus walk stepped over by design; and a
`$ref` inside a `$defs` declared on a *subschema* was resolution-checked by
nobody, because the attribution walk parses those nodes and drops them while
reference resolution runs afterwards over what survived —
`{"properties":{"a":{"$defs":{"x":{"$ref":"#/$defs/nope"}}}}}` loaded clean. There
is no rule to tighten in that situation, because there is no reader whose rule was
wrong. The question that finds this class is not *what does this check accept* but
*which reader visits this position at all*, and the answer "the one that discards
what it reads" is the same as "none".

**When adding or changing a gate here, write a negative control with two of the
guarded thing where only one satisfies the rule.** Checking that the walk
reaches a position is a different question from checking how much one answer
licenses, and this gate was hardened six times on the first question before the
second one turned up a hole: a single `x-authority` was licensing every bound in
its subschema.

**Then ask the same question of the field next door.** The arity fix above
reshaped `x-authority` into a per-bound map and left the rationale rule keyed to
the subschema's `description` — the identical failure, one field over, in the same
commit. Before that, the phantom-bound class was closed for `properties` and
reintroduced one level inside the annotation by the fix for a different hole. Each
time, the fix and the remaining hole were the same shape in the adjacent position.
So the second question to ask of a check is not only *how much does one answer
license* but *what else here is keyed per subschema, per document, or per file
that should be keyed per bound, per field, or per item*.

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
