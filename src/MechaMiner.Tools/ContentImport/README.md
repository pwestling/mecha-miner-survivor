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
- the common definition envelope on every definition — `schema_version`, `content_version`,
  `status` from exactly `development | enabled | disabled | retired`, `tags`, and a non-empty
  `source_refs`. `name_key` and `summary_key` are conditional: required only where the definition has
  a player-facing name or summary, and never an error when omitted. `presentation_id` must be absent
  rather than null;
- that every definition carries a non-empty string `id`. A missing or null `id` is an unconditional
  failure: `ID_NULL_EXPECTED` is empty, because every definition now has a minted stable ID;
- the two exception sets, against `ID_NULL_EXPECTED` (empty) and `NAME_KEY_OMITTED` (three members)
  declared at the top of the script, so an undeclared null `id` or a new `name_key` omission is a
  failure rather than one more warning, and a member that no longer belongs is a warning to shrink the
  list;
- `snake_case` property names at every depth, checked on keys only, so stable ID/enum/kind tokens in
  values keep their exact case;
- that no stale extraction metadata key (`_provenance`, `_source`, `notes`, `refs`, `lines`, `line`,
  `shared_rule_refs`) survives anywhere at any depth;
- that every `source_refs` element resolves — the document ID against `doc_id` front matter under
  `docs/`, and any `#anchor` against a real heading slug in that document;
- that every `source_refs` **scope prefix** resolves to a field that exists in the definition it
  annotates. A citation pointing at a field a ruling removed, or at a name the field no longer has, is
  the same defect class as an anchor pointing at a missing heading;
- `content/localization/en.json`: parses, flat, lexically sorted, duplicate-free, every referenced
  localization key present, and no orphaned string;
- per-catalog entry counts and aggregate row counts, from the `EXPECTATIONS` and `PROBES` tables where
  every row cites its own source doc and line;
- the two doc-stated grand totals recomputed from the JSON — PowerUp rank prices must sum to 9,450
  Hyper Gold and the six option-unlock costs to 2,150, with actual vs expected always printed;
- referential integrity for branch → weapon, encounter → enemy, and mech → signature weapon, with the
  reference property names discovered from the data rather than hardcoded;
- one derived-value regression guard: the Sentry Pod deployment interval is the authored 6.0 s, and the
  derived 12 s must not appear as an authored deployment or ramp value in `content/weapons/`; and
- the footprint second-writer guard, which is two rules with two different scopes. No definition under
  `content/enemies/` may carry a contact **diameter** — an enemy authors `body_scale_multiplier` and the
  diameter is `scale × 0.80 M` — and no definition under `content/enemies/` **or** `content/bosses/` may
  carry the **centre distance that begins contact**, which is `diameter ÷ 2 + the player's 0.50 M
  collision radius` for both. The diameter rule stops at enemies on purpose: a boss diameter is
  authored, because the boss roster gives bosses no body scale to derive one from and the survivability
  baseline states the four boss diameters flat. `reference_diameter_m` is allowlisted, being the
  Ripper's authored rank-zero diameter rather than a per-enemy derived value.

Two reconciliation heuristics report as warnings rather than failures, because no schema exists to
settle them: percentages carried on a property not named `*_percent` (`40:95`), and formulas held as
strings rather than a registered formula kind plus parameters (`40:99`). Both are grouped by property
name so the list stays actionable.

A definition whose `id` is absent or `null` is a **failure**: no definition in `content/` is waiting on
an ID any more. The integration owner minted the last five — the four prose-only mining-site classes
are `SITE-01`…`SITE-04` and the shared elite modifiers are `ELT-01` — so `ID_NULL_EXPECTED` is empty and
the check is unconditional. The list is kept as the declared place to record a future genuinely
unminted ID together with its reason, which is what stops that from becoming a place to hide a real
mistake.

Five kebab-case file names now carry a stable ID without being renamed to it
(`enemies/shared-elite-modifiers.json` is `ELT-01`, and the four `mining-sites/*.json` are
`SITE-01`…`SITE-04`). That is deliberate: the canonical bundle orders by the `id` field, not by the file
stem, so the file name is not load-bearing. See `content/transcription-notes.md`.

It performs no JSON Schema validation: `content/schemas/` does not exist yet, so domain field names
outside the envelope are unvalidated and will need one reconciliation pass when the schemas land.

When an expected count changes because a design document changed, edit the `EXPECTATIONS`/`PROBES`
table and update the `source` citation on that row in the same commit.
