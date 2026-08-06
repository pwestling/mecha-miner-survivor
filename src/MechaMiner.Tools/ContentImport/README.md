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
- that no property name abbreviates a bound as `cap`, `max` or `min`. A cap is a maximum, so the word is
  spelled out and the qualifier rather than the noun distinguishes two bounds on one quantity
  (`{target_minimum, target_maximum, hard_maximum}`). Where the name carries a unit suffix the unit stays
  terminal and the bound word moves to the front (`maximum_control_resistance_percent`). The exception
  list `BOUND_SPELLING_ESCALATED` is now **empty** — its two `W-BF-tethered-reaper` members were resolved
  rather than suppressed, since `docs/71:346` shows 200 bounds the speed-bonus component and 400 the
  total — and it is still asserted for drift like `ID_NULL_EXPECTED`, because a resolved escalation left
  in an exception list is worse than no list;
- that no stale extraction metadata key (`_provenance`, `_source`, `notes`, `note`, `refs`, `lines`,
  `line`, `shared_rule_refs`) survives anywhere at any depth. The singular `note` was added after three
  keys survived a blocklist that carried only the plural;
- that no string value anywhere under `content/` matches `docs/.*\.md`. `source_refs` was cleaned in an
  earlier pass, but the citations had moved next door into domain fields, where nothing looked: eleven
  `effect.stacking_classification` strings carried a parenthetical `(docs/68-…:253)`, and one field
  literally named `beacon_response_source` held a repo path. A line number is unstable wherever it
  hides, and `doc_id#anchor` in `source_refs` is the only citation form the envelope names;
- **polarity agreement**: where a structured polarity value (a `direction`, or any field valued from the
  closed vocabulary higher/lower, increase/decrease, more/less, faster/slower, longer/shorter,
  raise/reduce, gain/lose) sits beside prose stating the same fact, the two must agree in sign. Prose is
  read from the same object and from the enclosing one. It fires on strict contradiction only, so
  "20% faster without increasing movement speed" is not reported. This automates a check that had to be
  done by hand: six geode resonance directions were verified against `docs/40:104-109` by eye, and
  nothing would have caught a seventh;
- the **percentage-point policy** (`40:95`) on numbers and key names, not prose: every percent-named
  property resolves to at least one numeric leaf; no percent-named numeric value satisfies
  `0 < |v| < 1`, which would be the compiler's normalized factor stored where percentage points belong;
  and no name or object authors the normalized factor beside the points. A name "says `_percent`"
  wherever the token appears, so the 52 mid-name spellings such as `percent_of_mech_base_speed` are
  correct and are not flagged;
- that every `source_refs` element resolves — the document ID against `doc_id` front matter under
  `docs/`, and any `#anchor` against a real heading slug in that document;
- that every `source_refs` **scope prefix** resolves to a field that exists in the definition it
  annotates. A citation pointing at a field a ruling removed, or at a name the field no longer has, is
  the same defect class as an anchor pointing at a missing heading;
- `content/localization/en.json`: parses, flat, lexically sorted, duplicate-free, every referenced
  localization key present, and no orphaned string;
- per-catalog entry counts and aggregate row counts, from the `EXPECTATIONS` and `PROBES` tables where
  every row cites its own source doc and line;
- the four authored world-prop **values** folded into the map contract, each against its own citation:
  destructible rock Hull 100 (`docs/72:194`), rock damage footprint diameter 0.80 M (`:196`), health
  pack repair 25 Hull (`:182`), health pack pickup radius 0.25 M (`:185`). This replaced a row *count*
  over key-name patterns, which counted patterns that matched at least once — so two names existing
  satisfied it and no value was ever compared. A missing field is now a failure, not a silent pass;
- the two doc-stated grand totals recomputed from the JSON — PowerUp rank prices must sum to 9,450
  Hyper Gold and the six option-unlock costs to 2,150, with actual vs expected always printed;
- referential integrity for branch → weapon, encounter → enemy, and mech → signature weapon, with the
  reference property names discovered from the data rather than hardcoded;
- one derived-value regression guard: the Sentry Pod deployment interval is the authored 6.0 s, and the
  derived 12 s must not appear as an authored deployment or ramp value in `content/weapons/`; and
- the footprint second-writer guard, which is two rules with two different scopes. No definition under
  `content/enemies/` may carry a contact **diameter** — an enemy authors `body_scale_multiplier` and the
  diameter is `scale × 0.80 M` — and no definition under `content/enemies/`, `content/bosses/` **or**
  `content/maps/` may carry the **centre distance that begins contact**, which is the object's radius
  plus the player's `0.50 M` collision radius in all three. `content/maps/` joined the rule because the
  health pack stored `0.75` = its authored `0.25 M` pickup radius + `0.50 M` (`docs/72:185`), a third
  writer for one player-baseline constant. The diameter rule stops at enemies on purpose: a boss
  diameter is authored, because the boss roster gives bosses no body scale to derive one from
  (`docs/31:121-128` has no `Body` column, and `docs/72:86` scopes the derivation to "every **ordinary**
  body scale") and the survivability baseline states the four boss diameters flat (`docs/72:105-110`).
  `reference_diameter_m` is allowlisted, being the Ripper's authored rank-zero diameter rather than a
  per-enemy derived value.

**What the footprint guard does not do.** Both rules match specific key-name patterns in specific
directories. A derived value reintroduced under a name neither pattern matches, or in a directory
neither rule covers, passes — the guard raises the cost of the mistake, it does not make it impossible.
It is checked on key names rather than values so that a *rename* inside a covered directory cannot slip
past, which is a narrower claim than "fails the build if the field reappears under any name".

One reconciliation heuristic still reports as a warning rather than a failure, because no schema exists
to settle it: formulas held as strings rather than a registered formula kind plus parameters (`40:99`).
It is grouped by property name so the list stays actionable. The percentage heuristic that used to sit
beside it is gone: it matched a `%` glyph in prose, so it emitted 21 warnings about English sentences
while leaving the numeric rule it cited unchecked. A warning list a reader learns to ignore is worse
than no list.

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
