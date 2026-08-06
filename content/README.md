# `content/` — prepared gameplay catalog JSON, pending `DAT-006`

**This tree does not deliver `DAT-007`.** It holds JSON transcribed by hand from the accepted gameplay
design documents in `docs/`, prepared *ahead of* work package `DAT-007` ("Import accepted gameplay
catalogs into initial JSON definitions", `docs/technical/110-implementation-plan-for-ai-agents.md:216`).
That same line makes `DAT-006` `DAT-007`'s prerequisite; `DAT-006` depends on `DAT-005`, which depends
on `DAT-002`/`DAT-003`, which depend on `DAT-001` (`docs/technical/110-implementation-plan-for-ai-agents.md:210-215`);
and `DAT-001` has no implementation in this repository — `src/` contains one stdlib-only Python
verifier and no codec, schema, registry, or bundle-compiler code. `DAT-007` cannot be Done, so this
tree cannot be its completion, and `docs/technical/114-autonomous-agent-execution-protocol.md:141`
admits no data-versus-code exemption.

**The basis for preparing it now** is that same line, quoted verbatim:

> Only Done dependencies satisfy downstream package prerequisites. A task may prepare read-only
> analysis while waiting, but it must not commit consumer code against Draft or Active contracts.

This tree is that read-only preparation, and nothing more: transcribed source data plus the notes that
explain each transcription decision. No consumer code is committed here against a `DAT-001`–`DAT-006`
contract, because none of those contracts exists yet.

**Nothing here is validated, and no schema exists to validate it against.** `content/schemas/` does not
exist, so no definition in this tree has been checked against a structural schema, compiled into a
bundle, or hashed. Which domain fields exist, what they are called, and how they nest have never been
machine-checked against anything. What *has* been checked is a set of local assertions this branch
declared for itself, described under
[What is actually verified today](#what-is-actually-verified-today); that is not schema validation and
must not be reported as it.

**Downstream work must not treat this content as validated until the compiler exists.** Until the
`DAT-001`–`DAT-006` chain is Done and `generated/content.bundle.json` has been produced from these
files, no consumer should assume any field name, nesting, or number here will survive. Expect renames,
re-nesting, and outright rejection of fields at that point.

## Mandated directory layout

The layout is fixed by `docs/technical/40-content-data-and-validation.md:34-63`
(`## Accepted content repository layout`), quoted verbatim:

```text
content/
  schemas/
  resources/
  mechs/
  enemies/
  bosses/
  weapons/
  branches/
  utilities/
  relics/
  powerups/
  unlocks/
  mining-sites/
  encounters/
  maps/
  presentation/
  localization/
assets-manifest/
  assets/
  licenses/
generated/
  content.bundle.json
  content.bundle.sha256
  reports/
```

> Catalog directories are the authoring boundary. Definitions are grouped by stable item or the
> smallest cohesive aggregate such as the standard encounter schedule; generated/source separation is
> mandatory. A layout change must update build tooling, schemas, importers, documentation, and
> clean-checkout tests atomically rather than adding a second search path.
> — `docs/technical/40-content-data-and-validation.md:63`

Corroborated by `docs/technical/100-build-dependencies-and-release-operations.md:41`
(`content/   source JSON and localization`). **No document specifies individual JSON file names**
inside these directories; grouping follows the "stable item or smallest cohesive aggregate" rule
above.

### Ownership

| Directory | Owner | State |
| --- | --- | --- |
| `resources/` (8 files: `A`–`F`, `common-ore`, `hyper-gold`) | catalog transcription (CAT) | authored |
| `mechs/` (6: `MCH-01`–`MCH-06`) | CAT | authored |
| `enemies/` (11: `EN-01`–`EN-10`, `shared-elite-modifiers` = `ELT-01`) | CAT | authored |
| `bosses/` (4: `BOSS-01`–`BOSS-04`) | CAT | authored |
| `weapons/` (16: `W-AB`…`W-EF`, `stat-price-formula`) | CAT | authored |
| `branches/` (45: `<weapon-id>-<branch-name>`) | CAT | authored |
| `utilities/` (13: `UTL-A1`…`UTL-F2` plus the resource radar `UTL-R1`) | CAT | authored |
| `relics/` (10: `REL-01`–`REL-10`) | CAT | authored |
| `powerups/` (13: `PU-*`) | CAT | authored |
| `unlocks/` (6: `UNL-01`–`UNL-06`) | CAT | authored |
| `mining-sites/` (4 prose-derived site classes, `SITE-01`–`SITE-04`) | CAT | authored |
| `encounters/` (1: `standard-encounter-schedule`, `WAV-01`) | CAT | authored |
| `maps/` (1: `standard-map-generation-contract`, `MGC-01`) | CAT | authored |
| `localization/` (`en.json`) | localization stream (`DAT-009`, `docs/technical/110-implementation-plan-for-ai-agents.md:218`) | authored here, **not** a `DAT-009` delivery |
| `schemas/` | schema stream (`DAT-001`, `DAT-002`, `DAT-003`) | **not authored here** |
| `presentation/` | presentation/audio definitions, `SCH-CNT-003` (`docs/technical/115-component-contract-and-schema-registry.md:91`) | **not authored here** |
| `../generated/` | bundle compiler and report generators (`DAT-006`, `DAT-008`); "Generated files are changed through their generator" (`docs/technical/110-implementation-plan-for-ai-agents.md:92`) | **not authored here** |

**"authored" in that table means the JSON exists and was transcribed by hand.** It does not mean
validated, compiled, hashed, or delivered, and no row of the table records a completed work package.
`DAT-007`, `DAT-008`, and `DAT-009` are all undelivered; the `DAT-*` IDs above name the stream that
will *own* each directory, not work this branch closed.

**138 definition files plus `content/localization/en.json`** — 139 `*.json` files under `content/` in
total. The two numbers are different things and are stated separately on purpose: the definition count
is what the per-directory rows above sum to, and the 139 total is what the verifier asserts as
`EXPECTED_CONTENT_JSON_FILES`. `content/` also holds two Markdown files (this one and
`transcription-notes.md`) which are documentation, not content, and are counted in neither figure.
`content/schemas/`, `content/presentation/`, and `generated/` are absent from this tree because they
belong to other streams, not because they are optional.

Five files that used to be here are gone, and their absence is deliberate:

- **`mechs/shared-baseline.json`** held player baseline values. A mech definition carries *overrides*
  (`docs/technical/40-content-data-and-validation.md:110`), and `content/` has no player or run
  category yet; the schema stream owns that category, with `PLY-001` as its consumer.
- **`maps/world-props.json`** held the destructible-rock and health-pack values
  (`docs/72-player-survivability-and-damage-baseline.md:180,190`). Those are now fields of the
  `MGC-01` map-generation-contract definition.
- **`resources/geode-resonance-effects.json`** held the six geode resonance effects as one aggregate.
  Each effect now sits on the resource that owns it — `resources/A.json`…`F.json` each carry
  `resonance_effect_name` and a `resonance_behavior` block — and the resonance field lives on the geode
  site class (`resonance_field` in `mining-sites/specialized-material-geodes.json`), per the mining-site
  schema (`docs/technical/40-content-data-and-validation.md:140`). Its `radius_m` key is currently
  omitted: `DEC-128` sets it at 6.0 M, but that decision record is not reachable from this branch, so the
  number waits for a citation `source_refs` can carry (`40:87`).
- **`enemies/elite-modifier-profile.json`** treated elite status as its own entity. It is not one:
  elite *eligibility* is a validated `elite_eligible` field on each of the ten enemies
  (`docs/technical/40-content-data-and-validation.md:114` lists "elite eligibility" among the enemy
  fields), and the shared elite multipliers (`docs/31-initial-alien-roster.md:104`) are now the
  constants block `enemies/shared-elite-modifiers.json`, which the enemies read.
- **`utilities/radar-unassigned-id.json`** is now **`utilities/UTL-R1.json`**. The rulings pass
  assigned the resource radar the stable ID `UTL-R1` and a player-facing name, so it is an ordinary
  utility item like the other twelve rather than an aggregate with no ID.

## Authoring conventions

These are the conventions this tree actually follows. A reviewer can check compliance against this
list, and the verifier under
[What is actually verified today](#what-is-actually-verified-today) enforces the mechanical ones.

### File and directory naming

- **One JSON file per stable catalog item, named by its exact doc ID.** `MCH-01.json`, `EN-07.json`,
  `BOSS-03.json`, `W-BE.json`, `REL-10.json`, `UTL-C2.json`, `UNL-04.json`, `PU-*.json`. IDs are
  copied verbatim from the design docs and never re-cased or re-numbered
  (`docs/technical/40-content-data-and-validation.md:67`: "Reuse accepted gameplay IDs exactly").
- **Kebab-case file names for cohesive aggregates** — `shared-elite-modifiers.json`,
  `stat-price-formula.json` (`FORMULA-01`), `standard-encounter-schedule.json` (`WAV-01`),
  `standard-map-generation-contract.json` (`MGC-01`), and the four `*-seams`/`*-geodes`/`*-sites`
  mining-site files (`SITE-01`–`SITE-04`). A file carries a kebab-case name because no *document*
  assigns it an ID token; **minting an ID does not force a rename.** The four mining-site files and
  `enemies/shared-elite-modifiers.json` (`ELT-01`) keep their kebab-case names while carrying stable
  IDs, because the canonical bundle is ordered by category and stable ID and "hashes identically for
  identical semantic input regardless of source file enumeration order"
  (`docs/technical/40-content-data-and-validation.md:185`) — the `id` field is load-bearing, the file
  stem is not. The resource radar *was* renamed to `UTL-R1.json`, but that was a choice about matching
  its twelve sibling utility files, not a rule.
- **Branch files** are named `<weapon-id>-<branch-name-kebab-case>.json` (e.g.
  `W-AD-singularity-forge.json`) because no doc assigns branch IDs.
- **Formatting:** 2-space indent, LF line endings, one trailing newline, UTF-8 without BOM.

### Property names are `snake_case`; values keep their exact case

`docs/technical/40-content-data-and-validation.md:26` is the single mandate behind both halves of this
rule:

> Property names use `snake_case`; stable enum/kind/ID tokens remain exact case-sensitive ASCII.

- **Every property name, at every depth, is `snake_case`** — lowercase, underscore-separated, and
  never `_`-prefixed. No key anywhere in this tree contains an uppercase letter.
- **Stable ID, enum, and kind tokens in *values* keep their exact case.** `"W-BE"`, `"EN-06"`,
  `"MCH-01"`, `"BOSS-02"`, `"UTL-C2"`, `"PU-S04"`, `"REL-07"`, `"UNL-03"`, `"WAV-01"`, `"MGC-01"`, and
  the resource letters `"A"`–`"F"` are transcribed verbatim. `docs/technical/40-content-data-and-validation.md:69`
  makes this explicit: "IDs are case-sensitive ASCII tokens ... and never localized."
- **Most value tokens this tree mints are lower-kebab-case, and eight camelCase tokens are a known
  unresolved exception.** Measured over every string leaf of every `*.json` under `content/`:
  **37 kebab-case value tokens across five token spaces** — `inventory_scope` (`run-local`,
  `cross-run`), `pool_availability`, `site_class`, `value_kind`, and the two resource `id`s
  `common-ore` and `hyper-gold` — against **12 occurrences of 8 camelCase tokens across four spaces**
  (`kind` on `UNL-01`…`UNL-06`, `snapshot_at_creation` on `EN-06`, `unchanged_stats` on `W-AB`,
  `clone_inherits_current` on `W-AE`). **The camelCase eight are deliberately left as they are**
  (`content/transcription-notes.md`, Ruling 39): whether they were transcribed from a document — in
  which case the bullet above protects them — or minted here, has not been established, and the
  schema stream has not yet fixed the token grammar a converted value would have to satisfy. This
  bullet describes **values**, not property names.
- **Units live in key-name suffixes**, per
  `docs/technical/40-content-data-and-validation.md:94` (`_m`, `_m_per_s`, `_seconds`, `_per_second`,
  `_hull`, `_degrees`, `_fraction`, `_count`): `movement_speed_m_per_s`,
  `extraction_duration_seconds`, `reference_diameter_m`, `recovery_hull_per_second`,
  `cost_hyper_gold`, `warning_seconds`.
- **Percentage points belong only on a property whose name says `_percent`**
  (`docs/technical/40-content-data-and-validation.md:95`). The normalized factor is *not* authored
  here — the compiler writes it into the runtime bundle as a separate derived field.
- **Geometry names distinguish radius, diameter, width, and range**; `area` is never a vague scalar
  (`docs/technical/40-content-data-and-validation.md:98`). **`Area` the stat is exempt**, because it is
  an established stat classification rather than a geometric measurement: `docs/36-initial-mech-catalog.md:137`
  defines its membership as "scalable radii, widths, blast areas, projectile bodies, cones, and
  persistent damage zones", so `weapon_area_multiplier` deliberately scales a *set* of dimensions and
  naming one of them would encode a falsehood. The rule binds a field naming a measured dimension of a
  specific shape, not a field naming the Area stat.
- **A multiplicative scale is always `_multiplier`.** Not `_scaling`, not `_multiple_of_`, not
  `_factor`. `content/mining-sites/*.json` and `content/relics/REL-09.json` name the same mining
  progress-decay quantity `decay_rate_multiplier_of_forward_rate`, because one concept has one name.
  A field is *not* named `_multiplier` on the strength of prose that says a value "scales": where no
  document states the multiplicativity, the property is omitted rather than declared under a guessed
  semantic (see `content/transcription-notes.md`).
- **A bound is always `maximum` or `minimum`, spelled out** — never `_cap`, `_max` or `_min`. A cap is
  a maximum, and the qualifier rather than the noun carries the distinction between two bounds on one
  quantity: `{target_minimum, target_maximum, hard_maximum}`. Where the name carries a unit suffix the
  unit stays terminal (`40:94`) and the bound word moves to the front instead:
  `maximum_control_resistance_percent`, `maximum_pursuit_duration_seconds`.
- **Formulas are not script strings.** A player-facing formula such as the weapon upgrade price must
  become a registered formula kind plus parameters
  (`docs/technical/40-content-data-and-validation.md:99`).
- **Ranges are `{minimum, maximum}` objects**, never a string like `"8-10"`.
- **Per-rank values are rank-ordered arrays** (`ranks[0]` is rank 1), variable length: PowerUp rank
  arrays have 1, 3, 4, or 5 entries matching each entry's `maximum`, and `PU-S04` has exactly one rank.
- **Values are transcribed, not derived.** No value is computed, rounded, or filled in.
- **Absence is spelled by omitting the key. `null` appears nowhere in this tree.** Where the docs state
  no fact, the property is **absent**, not present-and-`null`.
  `docs/technical/40-content-data-and-validation.md:90` is the mandate: "Optional fields have explicit
  defaults materialized into the canonical bundle so runtime never guesses." An absent optional field
  gets its materialized default; a present-and-`null` one asks runtime to guess, which is what that line
  forbids. So absence has exactly one spelling.
  **This reverses what this file used to say.** It previously defined a `null` as "the docs state no
  fact" and argued that for ten fields the `null` was the only machine-readable record that a value is
  undecided, so omitting the key would delete the record. The premise was right and the conclusion was
  wrong: the fix is to write the record down, not to leave an illegal value in the data as a marker. A
  full inventory found **275 nulls across 101 of the 138 definition files**; all 275 are gone.
  Ten gap entries were written into `content/transcription-notes.md` **before** any key was omitted —
  what was being transcribed, what value was expected and why, and what the cited document actually says
  instead — and four further gap families found by re-deriving the inventory were recorded with them.
  Of the 275: 246 keys were omitted; 24 fields were **removed** as fields no schema will declare (the 20
  `relics/REL-01`–`REL-10 :: rarity_and_weighting.{rarity_tier, cache_selection_weight}` fields, which no
  design document mentions and `40:132`'s relic field list omits, and the 4 boss `armor` fields, which
  `40:114`'s enemies-and-bosses field list omits while `40:110`'s mech list includes Armor); 3
  `external_numerics[n].value` keys were removed as shape defects; and 2 nested `id` keys were removed
  because the objects holding them are parameters of `MGC-01` rather than independently addressable
  definitions. `A26` in the verifier now fails on any `null` anywhere under `content/`, **with no
  exception set** — an exception set is a place for a null to hide. See Ruling 29 in
  `content/transcription-notes.md` for the full bucket accounting.
- **The authoritative source wins over its mirrors.** The Markdown design docs are authoritative; the
  CSVs under `docs/data/` are mirrors (`docs/data/README.md:5,10` — "when values disagree, update the
  data mirror to match the Markdown"). Where a mirror and a doc disagree, the doc value is transcribed
  and the divergence is recorded in `content/transcription-notes.md`.

### The common definition envelope

`docs/technical/40-content-data-and-validation.md:76-88` requires the following on every
independently addressable definition. The literal values this tree carries today:

| Field | Mandate | In this tree |
| --- | --- | --- |
| `id` | `40:80` — stable category-valid ID | present as a non-empty string on **all 138** definitions, including the aggregates (`WAV-01`, `MGC-01`) and the six IDs the integration owner minted rather than transcribed: the four prose-only mining-site classes (`docs/40-mining-and-extraction.md:58-132`) are `SITE-01`–`SITE-04` in document order, `enemies/shared-elite-modifiers.json` is `ELT-01`, and `weapons/stat-price-formula.json` is `FORMULA-01` (`content/transcription-notes.md`, Ruling 38 — it previously carried `weapon-stat-price-formula`, which matched no ID grammar here). Nothing here carries `"id": null` or omits the field, so the verifier treats a missing or null `id` as an unconditional failure |
| `schema_version` | `40:81` — integer version of its definition schema | `1` everywhere; no schema exists to version yet |
| `content_version` | `40:82` — monotonic revision | `1` everywhere; this is the first authored revision |
| `status` | `40:83` — exactly one of `development`, `enabled`, `disabled`, `retired` | `"enabled"` everywhere; nothing here is gated or retired |
| `name_key` | `40:84` — localization key, never literal player-facing text | **conditional**, like `presentation_id`: required only where a definition has a genuinely player-facing name, with the compiler supplying the default otherwise (`40:90`). Present on 135 of the 138 definitions, always resolving into `content/localization/en.json`. The three omissions — `WAV-01`, `MGC-01`, and `ELT-01` — are authoring contracts and a constants block; naming them in the localization catalog would imply a UI surface that does not exist. Having a stable ID and having a player-facing name are independent: `ELT-01` is addressable without being named |
| `summary_key` | `40:85` — "concise player-facing summary key **where relevant**" | conditional, so it is present only where a summary exists (29 definitions); its absence is never an error |
| `tags` | `40:86` — closed or validated vocabulary, never hidden behavior | present as an array on every definition, currently empty: no tag vocabulary has been minted, and inventing one here would be hidden behavior |
| `source_refs` | `40:87` — gameplay document IDs/anchors and decision IDs implemented | present and non-empty on every definition; see below |
| `presentation_id` | `40:88` — logical presentation entry | **omitted entirely**, not set to `null`. `content/presentation/` (`40:52`) does not exist, so there is no logical presentation entry to name; the presentation stream adds the field with its value |

### `source_refs` is the provenance carrier

Provenance no longer lives in a `_provenance` block, and no `_`-prefixed key exists anywhere in this
tree. It lives in the required `source_refs` envelope field
(`docs/technical/40-content-data-and-validation.md:87`), in this shape:

```json
"source_refs": [
  "GDD-WEAPON-NUMERIC-CATALOG#base-weapon-summary",
  "recipe_pair: GDD-WEAPON-CATALOG#accepted-base-catalog-assignment"
]
```

- The reference is a **stable document ID plus a heading anchor** — the `doc_id` from that document's
  front matter, never a repo-relative path and never a line number. Line numbers were the old
  convention; they churn on every doc edit, and `40:87` asks for "document IDs/anchors".
- An optional `<json.path>: ` prefix attributes a **single property** to a different document than the
  rest of the file, replacing the old per-field `_source` blocks.
- Every document ID must resolve to a real `doc_id` declared under `docs/`, and every `#anchor` to a
  real heading in that document. The verifier checks both.

### Localization

Player-facing text is not authored in definition files
(`docs/technical/40-content-data-and-validation.md:211`). Every `name_key` and `summary_key` that is
present resolves into `content/localization/en.json` — 164 strings today — a flat, lexically sorted,
duplicate-free map of key to English string. Missing release strings are build errors
(`docs/technical/40-content-data-and-validation.md:216`), so an unresolved key and an orphaned string
are both failures, not warnings. An *omitted* conditional key is not an unresolved key: the compiler
materializes its default (`docs/technical/40-content-data-and-validation.md:90`).

## Known gaps, contradictions, and transcription decisions

These live in **[`content/transcription-notes.md`](./transcription-notes.md)**, in two sections:

1. **design-source contradictions needing a ruling** — places where the design documents disagree with
   themselves or with their CSV mirrors, and no local choice can settle it; and
2. **transcription and shape notes** — values the docs leave open (the key is omitted, and the gap is
   recorded there rather than marked with a `null`), aggregates whose file shape is provisional, and divergences resolved by choosing the authoritative document.

That file is deliberately *not* named `open-questions.md`: two open-question registers already exist
(`docs/open-questions.md` and `docs/technical/open-questions.md`), and a third file with that name
would be mistaken for one of them. Genuine open questions for a document owner belong in those two
registers; `content/transcription-notes.md` records what this tree did and why.

## What is actually verified today

`content/schemas/` does not exist and `DAT-001`..`DAT-006` are unimplemented, so **nothing in this tree
has been validated against a schema, compiled into a bundle, or hashed.** What exists is a standalone
stdlib-only verifier. **A green run of it is not evidence toward `DAT-007`**: it checks this tree
against assertions this branch wrote for itself, not against a contract any other package owns, and it
cannot close a package whose prerequisite is not Done.

```sh
python3 src/MechaMiner.Tools/ContentImport/verify_content.py
```

It asserts, with every claim citing the mandate behind it (see the assertion table at the top of that
file, and `src/MechaMiner.Tools/ContentImport/README.md`):

- every file parses, with no duplicate object properties (`40:26`);
- the full envelope above, including `status` from exactly the four accepted literals (`40:83`) and
  `presentation_id` being absent rather than null;
- a non-empty string `id` on every definition — a missing or null `id` is an unconditional failure,
  because the exception list is now empty;
- that **no `null` appears anywhere under `content/`**, at any depth, in any of the 139 `*.json` files
  including `localization/en.json`, with **no exception set at all** (`40:90`);
- that no number sits under a relative-magnitude name (`bonus`, `penalty`, `increase`, `reduction`, ...)
  which says neither percent nor any unit, so a percentage cannot arrive under a name that hides whether
  it is percentage points or a scale (`40:95`, `40:94`);
- that no string value carries a `:<digits>` line number after any path-like token, in either slash
  direction and any case with the extension optional, and that no repository path appears in a value at
  all — matched on the unstable thing rather than on one spelling of a path (`40:87`);
- the two exception sets, so drift stays visible: the definitions carrying a null *or absent* `id`
  (currently none) and the definitions omitting `name_key` (currently three) must each match a list
  declared at the top of the verifier. A new member is a failure; a member that no longer belongs is a
  warning to shrink the list;
- `snake_case` property names everywhere, keys only, so ID/enum tokens in values keep their case
  (`40:26`);
- that no property name abbreviates a bound as `cap`, `max` or `min` at any depth, so the spelled-out
  `maximum`/`minimum` cannot drift back into three spellings. The only accepted exceptions are the two
  fields declared in the verifier's `BOUND_SPELLING_ESCALATED`, which is now **empty**: its two
  `W-BF-tethered-reaper` members are resolved rather than suppressed, since `docs/71:346` shows 200
  bounds the speed-bonus component and 400 bounds the total, so both values stay as
  `maximum_speed_bonus_percent` and `maximum_total_contact_damage_percent`;
- no stale extraction metadata keys survive anywhere at any depth, including the retired
  `shared_rule_refs`, whose content now lives in `source_refs`;
- every `source_refs` document ID and `#anchor` resolves against `docs/` front matter (`40:87`), and
  every `source_refs` scope prefix names a field that actually exists in the definition it annotates —
  a citation pointing at a removed or renamed field is as dangling as an anchor pointing at a missing
  heading;
- `content/localization/en.json` is flat, sorted, duplicate-free, fully referenced, and has no
  orphaned strings (`40:216`);
- per-catalog entry counts and aggregate row counts, each row citing its own source `doc:line`;
- the two doc-stated grand totals recomputed from the JSON (PowerUp ranks = 9,450 Hyper Gold;
  option unlocks = 2,150);
- branch→weapon, encounter→enemy, and mech→signature-weapon referential integrity (`40:199`);
- two derived-value guards. The first is a regression guard on the one known transcription bug: the
  Sentry Pod deployment interval is the authored 6.0 s
  (`docs/71-initial-weapon-numeric-catalog.md:83`), and the derived 12 s must not appear as an
  authored deployment or ramp value. The second is a second-writer guard on footprints, with two
  scopes. No definition under `enemies/` may carry a contact **diameter**, because an enemy authors
  `body_scale_multiplier` and the diameter is `scale × 0.80 M`; and no definition under `enemies/`
  **or** `bosses/` may carry the **centre distance that begins contact**, which for both is
  `diameter ÷ 2 + the player's 0.50 M collision radius` (`40:114`). The diameter rule deliberately
  stops at enemies: a boss diameter is *authored*, since the boss roster gives bosses no body-scale
  column to derive one from (`docs/31-initial-alien-roster.md:121-128`) and
  `docs/72-player-survivability-and-damage-baseline.md:105-110` states the four boss diameters flat.
  `reference_diameter_m` is allowlisted because 0.80 M is the Ripper's authored rank-zero diameter,
  the shared reference the scale multiplies; and
- the total `*.json` inventory under `content/`, so a file in a directory no per-catalog row covers is
  still caught.

## Reconciling with the canonical schemas

**The envelope is settled; the domain field names outside it are not.** Every property name in this
tree is `snake_case` with unit suffixes, so the naming mandate at `40:26` and `40:92-100` is satisfied
mechanically — but *which* domain fields exist, what they are called, and how they nest has never been
checked against a schema, because none exists. **Expect exactly one reconciliation pass over the
non-envelope field names when `content/schemas/` lands.** The verifier cannot anticipate it; it checks
naming form, not the field vocabulary.

Two shapes are known to be most likely wrong at that point, and the verifier warns about both rather
than failing, since no schema can settle them yet:

- properties that carry a percentage in prose or in a `text`-style field instead of a `*_percent`
  numeric with the compiler-derived factor (`40:95`); and
- formulas held as strings rather than a registered formula kind plus parameters (`40:99`).

What to run once the schemas land, in this order:

1. `DAT-001` envelope validation over every file. The envelope fields above should pass; expect
   `additionalProperties: false` to reject unrecognized domain fields.
2. `DAT-002`/`DAT-003` per-category schema validation, including the cardinality and price validators
   (15 unordered material-pair recipes with no duplicate pair and exactly three stats each; one
   amplification, functional, and conversion branch per weapon —
   `docs/technical/40-content-data-and-validation.md:120`).
3. `DAT-004` behavior-registry validation — every `behavior_kind`, targeting policy, formula, modifier
   hook, formation, and effect must resolve to exactly one registered descriptor
   (`CTR-CNT-002`, `docs/technical/115-component-contract-and-schema-registry.md:62`). No behavior
   kinds are named in this tree yet; they must be assigned with the registry.
4. `DAT-005` cross-reference, semantic, analytical, localization, asset, and source-trace validators.
   Asset and presentation checks will fail until `content/presentation/` exists.
5. `DAT-006` bundle compile and hash — `generated/content.bundle.json` plus
   `generated/content.bundle.sha256`; a source-order permutation must yield an identical hash.
6. `DAT-008` report generation, then diff the generated balance report against
   `docs/data/*.csv` and `docs/70-combat-and-economy-balance-framework.md`.

Two classes of blocker survive that sequence: the missing tag vocabulary (`40:86`), and every
contradiction in section 1 of `content/transcription-notes.md`, which a document owner must decide
before the numbers here can be called final.

**Until step 5 has actually run, this tree stays prepared material and nothing more.** `DAT-007` is
claimable only once `DAT-006` is Done and the bundle compiler has accepted these files; a reviewer
looking for the completion evidence of `DAT-007` will not find it here, and should not accept this
`README`, the verifier's output, or `content/transcription-notes.md` as a substitute for it.
