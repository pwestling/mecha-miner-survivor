# `MechaMiner.Tools/ContentImport`

Durable content-import and validation code for the authored JSON catalog under `content/`. This
directory holds no `.csproj`; `MechaMiner.Tools.csproj` is owned by the integration stream.

## `verify_content.py`

A stdlib-only Python 3 checker for `content/`. Run it from anywhere — it locates the repository root
relative to its own path:

```sh
python3 src/MechaMiner.Tools/ContentImport/verify_content.py
```

It prints a summary table set either way, prints failures before warnings, and exits non-zero if any
failure is recorded. Warnings never affect the exit code.

**The assertion table at the top of `verify_content.py` is the authoritative list of what it claims**,
with the `docs/technical/40-content-data-and-validation.md` line behind each claim. In outline, it
checks:

- every `*.json` under `content/` parses as UTF-8 with no duplicate object properties;
- the common definition envelope on every definition — `id`, `schema_version`, `content_version`,
  `status` from exactly `development | enabled | disabled | retired`, `name_key`, `tags`, and a
  non-empty `source_refs`; `summary_key` is conditional and never required; `presentation_id` must be
  absent rather than null;
- `snake_case` property names at every depth, checked on keys only, so stable ID/enum/kind tokens in
  values keep their exact case;
- that no stale extraction metadata key (`_provenance`, `_source`, `notes`, `refs`, `lines`, `line`)
  survives anywhere at any depth;
- that every `source_refs` element resolves — the document ID against `doc_id` front matter under
  `docs/`, and any `#anchor` against a real heading slug in that document;
- `content/localization/en.json`: parses, flat, lexically sorted, duplicate-free, every referenced
  localization key present, and no orphaned string;
- per-catalog entry counts and aggregate row counts, from the `EXPECTATIONS` and `PROBES` tables where
  every row cites its own source doc and line;
- the two doc-stated grand totals recomputed from the JSON — PowerUp rank prices must sum to 9,450
  Hyper Gold and the six option-unlock costs to 2,150, with actual vs expected always printed;
- referential integrity for branch → weapon, encounter → enemy, and mech → signature weapon, with the
  reference property names discovered from the data rather than hardcoded; and
- one derived-value regression guard: the Sentry Pod deployment interval is the authored 6.0 s, and the
  derived 12 s must not appear as an authored deployment or ramp value in `content/weapons/`.

Two reconciliation heuristics report as warnings rather than failures, because no schema exists to
settle them: percentages carried on a property not named `*_percent` (`40:95`), and formulas held as
strings rather than a registered formula kind plus parameters (`40:99`). Both are grouped by property
name so the list stays actionable.

A definition whose `id` is present but `null` is a warning, not a failure. A **missing** `id` is always
a failure — there are no exceptions.

It performs no JSON Schema validation: `content/schemas/` does not exist yet, so domain field names
outside the envelope are unvalidated and will need one reconciliation pass when the schemas land.

When an expected count changes because a design document changed, edit the `EXPECTATIONS`/`PROBES`
table and update the `source` citation on that row in the same commit.
