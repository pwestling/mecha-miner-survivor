# `content/` — prepared gameplay catalog JSON, pending `DAT-006`

**This tree does not deliver `DAT-007`.** It holds JSON transcribed by hand from the accepted gameplay
design documents in `docs/`, prepared *ahead of* work package `DAT-007` ("Import accepted gameplay
catalogs into initial JSON definitions", `docs/technical/110-implementation-plan-for-ai-agents.md`
`## Content and data work packages`, the `DAT-007` row).
That same row makes `DAT-006` `DAT-007`'s prerequisite; `DAT-006` depends on `DAT-005`, which depends
on `DAT-002`/`DAT-003`, which depend on `DAT-001` (the `DAT-001`…`DAT-006` rows of that same table);
and `DAT-001` has no implementation in this repository — `src/` contains one stdlib-only Python
verifier and no codec, schema, registry, or bundle-compiler code. `DAT-007` cannot be Done, so this
tree cannot be its completion, and `docs/technical/114-autonomous-agent-execution-protocol.md`
`## Work states and integration` ("Only Done dependencies satisfy downstream package prerequisites")
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

The layout is fixed by `docs/technical/40-content-data-and-validation.md` `## Accepted content repository layout`
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
> — `docs/technical/40-content-data-and-validation.md` `## Accepted content repository layout`

Corroborated by `docs/technical/100-build-dependencies-and-release-operations.md`
`## Related documents`, in its repository-tree block (`content/   source JSON and localization`). **No document specifies individual JSON file names**
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
| `localization/` (`en.json`) | localization stream (`DAT-009`, `docs/technical/110-implementation-plan-for-ai-agents.md` `## Content and data work packages`) | authored here, **not** a `DAT-009` delivery |
| `schemas/` | schema stream (`DAT-001`, `DAT-002`, `DAT-003`) | **not authored here** |
| `presentation/` | presentation/audio definitions, `SCH-CNT-003` (`docs/technical/115-component-contract-and-schema-registry.md` `## Schema registry`) | **not authored here** |
| `../generated/` | bundle compiler and report generators (`DAT-006`, `DAT-008`); "Generated files are changed through their generator" (`docs/technical/110-implementation-plan-for-ai-agents.md` `## Rules minimizing agent ambiguity`) | **not authored here** |

**"authored" in that table means the JSON exists and was transcribed by hand.** It does not mean
validated, compiled, hashed, or delivered, and no row of the table records a completed work package.
`DAT-007`, `DAT-008`, and `DAT-009` are all undelivered; the `DAT-*` IDs above name the stream that
will *own* each directory, not work this branch closed.

**138 definition files plus `content/localization/en.json`** — 139 `*.json` files under `content/` in
total. The two numbers are different things and are stated separately on purpose. The definition count
is what the per-directory rows above sum to, and **138 is what the verifier asserts** — no longer as a
literal, but as the number of rows in the `A28` manifest
(`src/MechaMiner.Tools/ContentImport/content-definition-manifest.txt`), which records the
`(path, id)` pair of every definition. `A21`'s inventory covers the population `load_definitions()`
loads, which is every `*.json` under `content/` except those beneath a `NON_DEFINITION_DIRS` directory
(`localization`, `schemas`). `A28` is what catches a definition being **renamed inside its own
directory** or having **its `id` edited**, neither of which changes any count: regenerate the manifest
with `MECHAMINER_GOLDEN_UPDATE=1`, which rewrites it and still fails, then review and commit the diff.
The manifest is an edit tax that makes such a change loud in review — it is not evidence that any path
or `id` is correct. The 139 total is asserted by nothing; it is the whole-tree number, and the
whole tree is what `A26` scans for `null`, which is why `en.json` leaves the definition count without
losing coverage. `content/` also holds three Markdown files (this one, `transcription-notes.md` and
`quote-verification-audit.md`) which are documentation, not content, and are counted in neither figure.
`content/schemas/`, `content/presentation/`, and `generated/` are absent from this tree because they
belong to other streams, not because they are optional.

Five files that used to be here are gone, and their absence is deliberate:

- **`mechs/shared-baseline.json`** held player baseline values. A mech definition carries *overrides*
  (`docs/technical/40-content-data-and-validation.md` `### Mechs`), and `content/` has no player or run
  category yet; the schema stream owns that category, with `PLY-001` as its consumer.
- **`maps/world-props.json`** held the destructible-rock and health-pack values
  (`docs/72-player-survivability-and-damage-baseline.md` `### Health pack` and `### Destructible
  rock`). Those are now fields of the
  `MGC-01` map-generation-contract definition.
- **`resources/geode-resonance-effects.json`** held the six geode resonance effects as one aggregate.
  Each effect now sits on the resource that owns it — `resources/A.json`…`F.json` each carry
  `resonance_effect_name` and a `resonance_behavior` block — and the resonance field lives on the geode
  site class (`resonance_field` in `mining-sites/specialized-material-geodes.json`), per the mining-site
  schema (`docs/technical/40-content-data-and-validation.md` `### Mining sites`). Its `radius_m` key is currently
  omitted: `DEC-128` sets it at 6.0 M, but that decision record is not reachable from this branch, so the
  number waits for a citation `source_refs` can carry (40 `## Common definition envelope`).
- **`enemies/elite-modifier-profile.json`** treated elite status as its own entity. It is not one:
  elite *eligibility* is a validated `elite_eligible` field on each of the ten enemies
  (`docs/technical/40-content-data-and-validation.md` `### Enemies and bosses` lists "elite eligibility" among the enemy
  fields), and the shared elite multipliers (`docs/31-initial-alien-roster.md` `## Elite treatment`) are now the
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
  (`docs/technical/40-content-data-and-validation.md` `## Stable ID policy`: "Reuse accepted gameplay IDs exactly").
- **Kebab-case file names for cohesive aggregates** — `shared-elite-modifiers.json`,
  `stat-price-formula.json` (`FORMULA-01`), `standard-encounter-schedule.json` (`WAV-01`),
  `standard-map-generation-contract.json` (`MGC-01`), and the four `*-seams`/`*-geodes`/`*-sites`
  mining-site files (`SITE-01`–`SITE-04`). A file carries a kebab-case name because no *document*
  assigns it an ID token; **minting an ID does not force a rename.** The four mining-site files and
  `enemies/shared-elite-modifiers.json` (`ELT-01`) keep their kebab-case names while carrying stable
  IDs, because the canonical bundle is ordered by category and stable ID and "hashes identically for
  identical semantic input regardless of source file enumeration order"
  (`docs/technical/40-content-data-and-validation.md` `## Compilation pipeline`) — the `id` field is load-bearing, the file
  stem is not. The resource radar *was* renamed to `UTL-R1.json`, but that was a choice about matching
  its twelve sibling utility files, not a rule.
- **Branch files** are named `<weapon-id>-<branch-name-kebab-case>.json` (e.g.
  `W-AD-singularity-forge.json`) because no doc assigns branch IDs.
- **Formatting:** 2-space indent, LF line endings, one trailing newline, UTF-8 without BOM.

### Property names are `snake_case`; values keep their exact case

`docs/technical/40-content-data-and-validation.md` `## JSON codec and schema baseline` is the single mandate behind both halves of this
rule:

> Property names use `snake_case`; stable enum/kind/ID tokens remain exact case-sensitive ASCII.

- **Every property name, at every depth, is `snake_case`** — lowercase, underscore-separated, and
  never `_`-prefixed. No key anywhere in this tree contains an uppercase letter.
- **Stable ID, enum, and kind tokens in *values* keep their exact case.** `"W-BE"`, `"EN-06"`,
  `"MCH-01"`, `"BOSS-02"`, `"UTL-C2"`, `"PU-S04"`, `"REL-07"`, `"UNL-03"`, `"WAV-01"`, `"MGC-01"`, and
  the resource letters `"A"`–`"F"` are transcribed verbatim. `docs/technical/40-content-data-and-validation.md` `## Stable ID policy`
  makes this explicit: "IDs are case-sensitive ASCII tokens ... and never localized."
- **Lower-kebab is not this tree's value convention, and nothing here should be read as saying it
  is.** Three measurements, all finding the same **43** lower-kebab value tokens and each dividing it
  by a different denominator, because each draws the boundary of a "closed-vocabulary field space"
  differently:
  - **43 of 1,115 token occurrences in 88 field spaces — 3.9%** — measured at `1d6a9d2` over every
    string leaf of every `*.json` under `content/`, restricted to the field spaces whose *every* value
    is a whitespace-free token. This is the predicate stated in full here because it is the one this
    file's author can re-derive.
  - **43 of 1,313 token occurrences in 74 field spaces — 3.3%** — an **earlier measurement by the same
    author as this pass's brief**, under a different predicate. Not an independent confirmation, and it
    is not offered as one.
  - **43 of 2,190 token-shaped values — 2.0%** — the integration owner's sweep at `origin/master`
    `e17b8b6`, by a route reusing neither of the scripts behind the two figures above. **This is the
    independent one**, and what it independently confirms is the **numerator**.
  The three shares are **not** to be averaged, reconciled, or reported as agreeing: they are counts
  over three different populations, and quoting a single "share of the corpus" would be picking one
  predicate and hiding it. What all three agree on is that lower-kebab is 43 tokens and a low
  single-digit fraction of value tokens on any of the three populations — so "the corpus is
  overwhelmingly kebab" is not a claim this tree supports, and it is not an argument available for or
  against re-casing anything.
- **Kebab and camelCase value-token counts, each stamped at the ref it was measured at.** Measured over
  every string leaf of every `*.json` under `content/`:
  - **37 kebab-case value tokens across five token spaces at `b482304`** — `id` 2, `inventory_scope`
    8, `pool_availability` 10, `site_class` 4, `value_kind` 13. This figure is kept with its sha
    rather than overwritten, because it was exactly right when written; an unstamped number is what
    let it go stale unnoticed here, and replacing it with a bare new number would rebuild the same
    defect one migration later.
  - **43 across five token spaces at `origin/master` `e17b8b6` and at `1d6a9d2`** — `inventory_scope`
    8, `pool_availability` 10, `resource_class` 8, `site_class` 4, `value_kind` 13, with `id`
    contributing **zero**. This 43 was measured twice by different parties: once here, and once by the
    integration owner at `e17b8b6` by a route reusing neither script.
  - **The two fives are not the same five, and the figures reconcile exactly** — so the arithmetic is
    given rather than two bare numbers left side by side, because "37 … 43 … five spaces" otherwise
    reads as one population that grew:

    | | in the 37 at `b482304` | in the 43 at `e17b8b6`/`1d6a9d2` |
    | --- | --- | --- |
    | `id` | **2** | — |
    | `inventory_scope` | 8 | 8 |
    | `pool_availability` | 10 | 10 |
    | `resource_class` | — | **8** |
    | `site_class` | 4 | 4 |
    | `value_kind` | 13 | 13 |
    | **total** | **37** | **43** |

    **37 − 2 + 8 = 43**: minus the two retired `id` slugs (`common-ore` and `hyper-gold`, now `RSC-07`
    and `RSC-08`), plus the 8 `resource_class` tokens the 37 never counted because that field held
    prose then. Three of the five spaces are common to both and did not move at all.
  - **The resource `id`s are `RSC-01`…`RSC-08`** — `common-ore.json` carries `RSC-07`,
    `hyper-gold.json` carries `RSC-08` — and **no `id` in this tree is lower-kebab.** `common-ore` and
    `hyper-gold` were the two `id` values when the 37 was counted at `b482304`; they are
    `resource_class` values now. This one is a **correction, not a stamp**: the claim that the tree's
    kebab `id`s are `common-ore` and `hyper-gold` is false at both refs above rather than true-then, and
    dating a false claim would read as provenance while functioning as cover.
  - **12 occurrences of 8 camelCase tokens across four spaces**, unchanged at all three refs — `kind`
    on `UNL-01`…`UNL-06`, `snapshot_at_creation` on `EN-06`, `unchanged_stats` on `W-AB`,
    `clone_inherits_current` on `W-AE`.
  - **The camelCase eight are deliberately left as they are**, on **two open preconditions**, neither
    discharged at `e17b8b6` or `1d6a9d2`: a **provenance answer** — no *exact-string* occurrence of any
    of the eight exists in `docs/`, in `src/`, or in a `content/` file other than the nine that carry
    them, though same-meaning counterparts in other spellings do exist and are recorded with the ruling
    — and a **declared token grammar**, which would live in `content/schemas/` and would have to land
    on a **merged** ref; that directory exists on neither ref, and a grammar declared only on an
    unmerged branch does not discharge the precondition. The grammar is a precondition rather than a
    preference. Cited by heading rather than by number, because a bare ruling number is not a checkable
    citation: see `content/transcription-notes.md`, the ruling headed **"Ruling 39 — the eight
    camelCase value tokens are measured and left alone, pending a provenance answer"**, which states
    what would discharge each precondition so a later pass can check the two conditions instead of
    re-opening the question.
  - These bullets describe **values**, not property names.
- **Units live in key-name suffixes**, per
  `docs/technical/40-content-data-and-validation.md` `## Unit and numeric policy` (`_m`, `_m_per_s`, `_seconds`, `_per_second`,
  `_hull`, `_degrees`, `_fraction`, `_count`): `movement_speed_m_per_s`,
  `extraction_duration_seconds`, `reference_diameter_m`, `recovery_hull_per_second`,
  `cost_hyper_gold`, `warning_seconds`.
- **Percentage points belong only on a property whose name says `_percent`**
  (`docs/technical/40-content-data-and-validation.md` `## Unit and numeric policy`). The normalized factor is *not* authored
  here — the compiler writes it into the runtime bundle as a separate derived field.
- **Geometry names distinguish radius, diameter, width, and range**; `area` is never a vague scalar
  (`docs/technical/40-content-data-and-validation.md` `## Unit and numeric policy`). **`Area` the stat is exempt**, because it is
  an established stat classification rather than a geometric measurement: `docs/36-initial-mech-catalog.md`
  `## MCH-04 — Lodestar` `### Signature and trait`
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
  unit stays terminal (40 `## Unit and numeric policy`) and the bound word moves to the front instead:
  `maximum_control_resistance_percent`, `maximum_pursuit_duration_seconds`.
- **Formulas are not script strings.** A player-facing formula such as the weapon upgrade price must
  become a registered formula kind plus parameters
  (`docs/technical/40-content-data-and-validation.md` `## Unit and numeric policy`).
- **Ranges are `{minimum, maximum}` objects**, never a string like `"8-10"`.
- **Per-rank values are rank-ordered arrays** (`ranks[0]` is rank 1), variable length: PowerUp rank
  arrays have 1, 3, 4, or 5 entries matching each entry's `maximum`, and `PU-S04` has exactly one rank.
- **Values are transcribed, not derived.** No value is computed, rounded, or filled in.
- **Absence is spelled by omitting the key. `null` appears nowhere in this tree.** Where the docs state
  no fact, the property is **absent**, not present-and-`null`.
  `docs/technical/40-content-data-and-validation.md` `## Common definition envelope` is the mandate: "Optional fields have explicit
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
  design document mentions and 40 `### Relics`'s relic field list omits, and the 4 boss `armor` fields, which
  40 `### Enemies and bosses`'s enemies-and-bosses field list omits while 40 `### Mechs`'s mech list includes Armor); 3
  `external_numerics[n].value` keys were removed as shape defects; and 2 nested `id` keys were removed
  because the objects holding them are parameters of `MGC-01` rather than independently addressable
  definitions. `A26` in the verifier now fails on any `null` anywhere under `content/`, **with no
  exception set** — an exception set is a place for a null to hide. See Ruling 29 in
  `content/transcription-notes.md` for the full bucket accounting.
- **The authoritative source wins over its mirrors.** The Markdown design docs are authoritative; the
  CSVs under `docs/data/` are mirrors (`docs/data/README.md`, its `authoritative: false` front-matter
  key and the preamble under `# Machine-Readable Data Index` — "when values disagree, update the
  data mirror to match the Markdown"). Where a mirror and a doc disagree, the doc value is transcribed
  and the divergence is recorded in `content/transcription-notes.md`.

### The common definition envelope

`docs/technical/40-content-data-and-validation.md` `## Common definition envelope` requires the following on every
independently addressable definition. The literal values this tree carries today:

| Field | Mandate | In this tree |
| --- | --- | --- |
| `id` | 40 `## Common definition envelope` — stable category-valid ID | present as a non-empty string on **all 138** definitions, including the aggregates (`WAV-01`, `MGC-01`) and the six IDs the integration owner minted rather than transcribed: the four prose-only mining-site classes (`docs/40-mining-and-extraction.md` `## Resource payout profiles` — its `### Standard ore seams`, `### Rich ore seams`, `### Specialized-material geodes` and `### Hyper Gold sites` subsections, closed by `### Other resource profiles`: "Standard ore seams, rich ore seams, specialized-material geodes, and Hyper Gold sites are the accepted initial mining-point classes") are `SITE-01`–`SITE-04` in document order, `enemies/shared-elite-modifiers.json` is `ELT-01`, and `weapons/stat-price-formula.json` is `FORMULA-01` (`content/transcription-notes.md`, the ruling headed "Ruling 38 — `FORMULA-01`, and the summary that says what the definition is" — it previously carried `weapon-stat-price-formula`, the only *minted* ID that was not `<PREFIX>-<NN>`). **No `id` in this tree is lower-kebab.** This row previously ended "lower-kebab `id`s do exist here, but only as the two transcribed resource IDs `common-ore` and `hyper-gold`"; that was true until the `RSC-01`–`RSC-08` migration and is **false at `origin/master` `e17b8b6` and at `1d6a9d2`**, where the eight resource definitions carry `RSC-01`…`RSC-08` (`common-ore.json` → `RSC-07`, `hyper-gold.json` → `RSC-08`) and the two slugs survive only as `resource_class` values. Corrected rather than dated, because this row is a live description of the tree rather than a record of a past measurement. Nothing here carries `"id": null` or omits the field, so the verifier treats a missing or null `id` as an unconditional failure |
| `schema_version` | 40 `## Common definition envelope` — integer version of its definition schema | `1` everywhere; no schema exists to version yet |
| `content_version` | 40 `## Common definition envelope` — monotonic revision | `1` everywhere; this is the first authored revision |
| `status` | 40 `## Common definition envelope` — exactly one of `development`, `enabled`, `disabled`, `retired` | `"enabled"` everywhere; nothing here is gated or retired |
| `name_key` | 40 `## Common definition envelope` — localization key, never literal player-facing text | **conditional**, like `presentation_id`: required only where a definition has a genuinely player-facing name, with the compiler supplying the default otherwise (40 `## Common definition envelope`). Present on 135 of the 138 definitions, always resolving into `content/localization/en.json`. The three omissions — `WAV-01`, `MGC-01`, and `ELT-01` — are authoring contracts and a constants block; naming them in the localization catalog would imply a UI surface that does not exist. Having a stable ID and having a player-facing name are independent: `ELT-01` is addressable without being named |
| `summary_key` | 40 `## Common definition envelope` — "concise player-facing summary key **where relevant**" | conditional, so it is present only where a summary exists (29 definitions); its absence is never an error |
| `tags` | 40 `## Common definition envelope` — closed or validated vocabulary, never hidden behavior | present as an array on every definition, currently empty: no tag vocabulary has been minted, and inventing one here would be hidden behavior |
| `source_refs` | 40 `## Common definition envelope` — gameplay document IDs/anchors and decision IDs implemented | present and non-empty on every definition; see below |
| `presentation_id` | 40 `## Common definition envelope` — logical presentation entry | **omitted entirely**, not set to `null`. `content/presentation/` (40 `## Accepted content repository layout`) does not exist, so there is no logical presentation entry to name; the presentation stream adds the field with its value |

### `source_refs` is the provenance carrier

Provenance no longer lives in a `_provenance` block, and no `_`-prefixed key exists anywhere in this
tree. It lives in the required `source_refs` envelope field
(`docs/technical/40-content-data-and-validation.md` `## Common definition envelope`), in this shape:

```json
"source_refs": [
  "GDD-WEAPON-NUMERIC-CATALOG#base-weapon-summary",
  "recipe_pair: GDD-WEAPON-CATALOG#accepted-base-catalog-assignment"
]
```

- The reference is a **stable document ID plus a heading anchor** — the `doc_id` from that document's
  front matter, never a repo-relative path and never a line number. Line numbers were the old
  convention; they churn on every doc edit, and 40 `## Common definition envelope` asks for "document IDs/anchors".
- An optional `<json.path>: ` prefix attributes a **single property** to a different document than the
  rest of the file, replacing the old per-field `_source` blocks.
- Every document ID must resolve to a real `doc_id` declared under `docs/`, and every `#anchor` to a
  real heading in that document. The verifier checks both.

### Localization

Player-facing text is not authored in definition files
(`docs/technical/40-content-data-and-validation.md` `## Localization contract`). Every `name_key` and `summary_key` that is
present resolves into `content/localization/en.json` — 164 strings today — a flat, lexically sorted,
duplicate-free map of key to English string. Missing release strings are build errors
(`docs/technical/40-content-data-and-validation.md` `## Localization contract`), so an unresolved key and an orphaned string
are both failures, not warnings. An *omitted* conditional key is not an unresolved key: the compiler
materializes its default (`docs/technical/40-content-data-and-validation.md` `## Common definition envelope`).

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

- every file parses, with no duplicate object properties (40 `## JSON codec and schema baseline`);
- the full envelope above, including `status` from exactly the four accepted literals (40 `## Common definition envelope`) and
  `presentation_id` being absent rather than null;
- a non-empty string `id` on every definition — a missing or null `id` is an unconditional failure,
  because the exception list is now empty;
- that **no `null` appears anywhere under `content/`**, at any depth, in any of the 139 `*.json` files
  including `localization/en.json`, with **no exception set at all** (40 `## Common definition envelope`);
- that no number sits under a relative-magnitude name (`bonus`, `penalty`, `increase`, `reduction`, ...)
  which says neither percent nor any unit, so a percentage cannot arrive under a name that hides whether
  it is percentage points or a scale (40 `## Unit and numeric policy`, 40 `## Unit and numeric policy`);
- that no string value carries a `:<digits>` line number after any path-like token, in either slash
  direction and any case with the extension optional, and that no repository path appears in a value at
  all — matched on the unstable thing rather than on one spelling of a path (40 `## Common definition envelope`);
- the two exception sets, so drift stays visible: the definitions carrying a null *or absent* `id`
  (currently none) and the definitions omitting `name_key` (currently three) must each match a list
  declared at the top of the verifier. A new member is a failure; a member that no longer belongs is a
  warning to shrink the list;
- `snake_case` property names everywhere, keys only, so ID/enum tokens in values keep their case
  (40 `## JSON codec and schema baseline`);
- that no property name abbreviates a bound as `cap`, `max` or `min` at any depth, so the spelled-out
  `maximum`/`minimum` cannot drift back into three spellings. The only accepted exceptions are the two
  fields declared in the verifier's `BOUND_SPELLING_ESCALATED`, which is now **empty**: its two
  `W-BF-tethered-reaper` members are resolved rather than suppressed, since `docs/71`
  `### Tethered Reaper — Conversion — 2 Eidolon Coral` shows 200
  bounds the speed-bonus component and 400 bounds the total, so both values stay as
  `maximum_speed_bonus_percent` and `maximum_total_contact_damage_percent`;
- no stale extraction metadata keys survive anywhere at any depth, including the retired
  `shared_rule_refs`, whose content now lives in `source_refs`;
- every `source_refs` document ID and `#anchor` resolves against `docs/` front matter (40 `## Common definition envelope`), and
  every `source_refs` scope prefix names a field that actually exists in the definition it annotates —
  a citation pointing at a removed or renamed field is as dangling as an anchor pointing at a missing
  heading;
- `content/localization/en.json` is flat, sorted, duplicate-free, fully referenced, and has no
  orphaned strings (40 `## Localization contract`);
- per-catalog entry counts and aggregate row counts, each row citing its own source `doc:line`;
- the two doc-stated grand totals recomputed from the JSON (PowerUp ranks = 9,450 Hyper Gold;
  option unlocks = 2,150);
- branch→weapon, encounter→enemy, and mech→signature-weapon referential integrity (40 `### Relational`);
- two derived-value guards. The first is a regression guard on the one known transcription bug: the
  Sentry Pod deployment interval is the authored 6.0 s
  (`docs/71-initial-weapon-numeric-catalog.md` `## Rank-Zero Values and Ore-Stat Increments`, the
  `W-BE` row: "One pod every 6 s, 24 s life, maximum three"), and the derived 12 s must not appear as an
  authored deployment or ramp value. The second is a second-writer guard on footprints, with two
  scopes. No definition under `enemies/` may carry a contact **diameter**, because an enemy authors
  `body_scale_multiplier` and the diameter is `scale × 0.80 M`; and no definition under `enemies/`
  **or** `bosses/` may carry the **centre distance that begins contact**, which for both is
  `diameter ÷ 2 + the player's 0.50 M collision radius` (40 `### Enemies and bosses`). The diameter rule deliberately
  stops at enemies: a boss diameter is *authored*, since the boss roster gives bosses no body-scale
  column to derive one from (`docs/31-initial-alien-roster.md` `## Interval boss overview`) and
  `docs/72-player-survivability-and-damage-baseline.md` `## Collision and Contact Footprints` states
  the four boss diameters flat in its `| Boss | Contact and weapon-hurt diameter |` table.
  `reference_diameter_m` is allowlisted because 0.80 M is the Ripper's authored rank-zero diameter,
  the shared reference the scale multiplies; and
- the total `*.json` inventory under `content/`, so a file in a directory no per-catalog row covers is
  still caught.

## Reconciling with the canonical schemas

**The envelope is settled; the domain field names outside it are not.** Every property name in this
tree is `snake_case` with unit suffixes, so the naming mandate at 40 `## JSON codec and schema baseline` and 40 `## Unit and numeric policy` is satisfied
mechanically — but *which* domain fields exist, what they are called, and how they nest has never been
checked against a schema, because none exists. **Expect exactly one reconciliation pass over the
non-envelope field names when `content/schemas/` lands.** The verifier cannot anticipate it; it checks
naming form, not the field vocabulary.

Two shapes are known to be most likely wrong at that point, and the verifier warns about both rather
than failing, since no schema can settle them yet:

- properties that carry a percentage in prose or in a `text`-style field instead of a `*_percent`
  numeric with the compiler-derived factor (40 `## Unit and numeric policy`); and
- formulas held as strings rather than a registered formula kind plus parameters (40 `## Unit and numeric policy`).

What to run once the schemas land, in this order:

1. `DAT-001` envelope validation over every file. The envelope fields above should pass; expect
   `additionalProperties: false` to reject unrecognized domain fields.
2. `DAT-002`/`DAT-003` per-category schema validation, including the cardinality and price validators
   (15 unordered material-pair recipes with no duplicate pair and exactly three stats each; one
   amplification, functional, and conversion branch per weapon —
   `docs/technical/40-content-data-and-validation.md` `### Weapons`).
3. `DAT-004` behavior-registry validation — every `behavior_kind`, targeting policy, formula, modifier
   hook, formation, and effect must resolve to exactly one registered descriptor
   (`CTR-CNT-002`, `docs/technical/115-component-contract-and-schema-registry.md` `## Cross-boundary contract registry`). **No registry
   token has been minted for a `behavior_kind`, but the field is not greenfield: 14 sites already
   hold a prose sentence where a token will go**, so this is a 14-site migration rather than a fresh
   authoring job. All 14 are `behavior_kind` at the top level of an enemy or boss: the ten ordinary
   enemies (`EN-01`–`EN-10`) and the four bosses (`BOSS-01`–`BOSS-04`). Nine read
   `"pure contact pursuer"`, `EN-06` reads
   `"pursuit and contact plus one telegraphed straight projectile"`, and all four bosses read
   `"persistent giant pursuer with exactly one additional behavior"`. Counting the other five spaces
   the same sentence names, **65 value positions across the six spaces hold prose and 24 hold a
   single token** — the 24 being `effect.value_kind` on all 13 utilities, `unlocks.kind` on the six
   option unlocks, and five of the seven `spawn_formations[].formation` names. That 65 is an upper
   bound on sites to annotate, not a settled classification: the registry vocabulary is being
   re-keyed by field space *and* token together, because a flat namespace has real collisions in this
   tree. Two, measured here rather than assumed: `UTL-E1` carries the identical string
   `"Recovery sources add in Hull Integrity per second."` at both `effect.stacking_classification`
   and `catalog_wide_rules.modifier_and_timing_rules[2]`, so one string is simultaneously an effect
   classification and a catalog-wide rule; and the bare value `"damage"` appears at **36 sites under
   7 distinct leaf names** (`ore_upgradeable_stats[].name`, `.unit`,
   `ore_upgradeable_stat_labels.labels[]`, `unchanged_stats[]`, `clone_inherits_current[]`,
   `snapshot_at_creation[]`, `echo_uses_full_strength_current_values[]`), meaning a stat name, a
   unit, a display label and a snapshot-field enumerator. **None of the 14 values, and none of the
   65, are changed here.** Annotating before the field spaces are settled would be annotate-twice
   churn; this entry is the record of the size, not a migration.
4. `DAT-005` cross-reference, semantic, analytical, localization, asset, and source-trace validators.
   Asset and presentation checks will fail until `content/presentation/` exists.
5. `DAT-006` bundle compile and hash — `generated/content.bundle.json` plus
   `generated/content.bundle.sha256`; a source-order permutation must yield an identical hash.
6. `DAT-008` report generation, then diff the generated balance report against
   `docs/data/*.csv` and `docs/70-combat-and-economy-balance-framework.md`.

Two classes of blocker survive that sequence: the missing tag vocabulary (40 `## Common definition envelope`), and every
contradiction in section 1 of `content/transcription-notes.md`, which a document owner must decide
before the numbers here can be called final.

**Until step 5 has actually run, this tree stays prepared material and nothing more.** `DAT-007` is
claimable only once `DAT-006` is Done and the bundle compiler has accepted these files; a reviewer
looking for the completion evidence of `DAT-007` will not find it here, and should not accept this
`README`, the verifier's output, or `content/transcription-notes.md` as a substitute for it.

## The line-citation release, and the citation surfaces this tree cannot gate

A line-number citation such as `docs/72-…:96` breaks silently the moment its target document gains a
line above the cited one: the coordinate still resolves, but to the wrong sentence, and nothing fails.
Three commits — `c05aee9`, `e24a52a`, `85d0ced` — removed every **live** such citation from the files
this branch owns (everything under `src/MechaMiner.Tools/ContentImport/`, plus `content/README.md`,
`content/transcription-notes.md` and `content/quote-verification-audit.md`) and re-pointed each to a
section heading (`<doc>:<n>` removed, `` <doc> `## Heading` `` added). This section is the durable
record of what that released, what it deliberately did **not**, and the citation surfaces no
working-tree gate can walk. The reasoning below lived only in inter-session messages until now; it is
written here so a later pass can act on it without them.

**Amended 2026-08-11, at `3db56c5`. The word "live" in the paragraph above is the amendment, and it
carries the rest of it.** This section previously read as though the release had removed *every*
citation; **1251 line citations of 31 documents survive in those files**, deliberately, as frozen note
text. (That 1251 is the population this section measures: `<token>:<digits>` occurrences resolving to a
`docs/**` document. It is not `A35`'s 1312, which counts a wider set of seven spellings over the same
twelve files; the two figures are not reconciled and should not be.) Three things changed here as a
result: the definition of *released* has been replaced with the
property that was actually proved, the released membership has moved from **33 to 32**, and every
surviving-citation figure is now labelled a **floor** rather than a count. The earlier wording is kept
in place, marked superseded, so a reader who saw the first version can see what changed and why.

**Amended again 2026-08-11 — the two lists in this section do not partition the cited documents, and
this section no longer reads as though they do.** Both lists were derived from citation enumeration,
while the criterion for *released* has since been replaced by the property that was actually proved —
no runtime or gate behaviour in this tree depends on a `docs/**` line coordinate. On that criterion
**there is no "not released" set at all**: the shift experiment covered all 36 cited documents, every
gate returned byte-identical output, and the result was reproduced on a forced failure path, so **every
document is free to re-flow, and that is the entire content of "released" now**. The five-document list
below survives as a **non-exclusive annotation** answering a different question — which documents have
line citations *pinned inside frozen artifacts*, so that re-flowing them strands those citations — and
under the proved property a stranded pinned citation is a **documentation defect, not a breakage**. A
document may therefore appear in both lists, and `docs/technical/22` and `docs/technical/24` appearing
in both is **not a contradiction**. Both subsections below carry this ruling with their earlier framing
kept in place and marked superseded, as the amendment above did.

### Released documents — their line counts may change freely

**AMENDMENT, 2026-08-11 — the criterion this subsection published has been replaced, and the
membership figure has moved from 33 to 32.** What a document owner was told is unchanged and was
always true. What was wrong is the grounds this record gave for it. The superseded paragraphs are kept
below rather than deleted, marked as superseded, the same way the retired 37 is kept.

**The criterion as it now stands: a document is released when no runtime or gate behaviour in this
tree depends on a `docs/**` line coordinate of it.** That is measured directly, by two probes:

- **The shift probe.** All 36 cited documents were shifted at once — 108 inserted lines — and all four
  gates re-run. The **complete stdout** of every gate came back byte-identical, not merely its
  `RESULT:` line.
- **The forced-failure probe.** The same comparison was repeated with a gate driven down a
  deliberately failing path, so the identity is not an artefact of the passing path printing the same
  summary whatever the tree looks like.

Nothing in this tree resolves a `docs/**` line position at runtime, and those two probes are what
establish that rather than assert it.

**Why the replacement is the primary finding here and not a tidier wording of the old definition.**
The shift probe does not enumerate citations at all: it moves line geometry and observes behaviour.
The criterion it replaces — *no file in this tree cites the document by line* — can be no more
reliable than the citation enumeration it rests on, and citation enumeration in this repository is
measured to miss spellings (below). Of the two candidate definitions, **only one survives the
instrument finding**, and it is the one now published.

**~~SUPERSEDED — the definition this subsection used to publish, and the figure that went with it.~~**
It read: *"A released document is one no file in this tree cites by line, so its owner may add or
remove lines without breaking anything here. **The membership figure is 33** — the documents
positively confirmed released by full-path diff evidence from the diffs of `c05aee9`, `e24a52a` and
`85d0ced`, enumerated below."* **That definition is false of 26 of the 33 documents it was applied
to.** The three commits removed the **live** citations and deliberately preserved the **frozen** ones,
so citation-free-ness is not what they delivered and never was. The definition is retired, not
softened. The diff evidence behind the list is unaffected: it establishes which documents the release
commits actually touched, which is a different claim and a sound one.

**The two properties, kept apart, because conflating them is exactly what produced the defect:**

- **(a) No live consumer resolves a line position in these documents.** **Proved**, by the two probes
  above. This is the property a document owner cares about, and it is the one owners were told:
  *change your line count freely* — true, and true for all of them.
- **(b) No file in this tree contains a line citation of the document.** **False for 26 of the 33.**
  It is strictly stronger than (a) and was only ever a proxy for it.

**Once the criterion is (a), the surviving frozen citations stop being a defect in the release.** They
become what they were always meant to be — deliberately preserved note text. The per-document counts
below are kept because they are useful to anyone editing the files that hold them, not because their
subjects fail a test.

**Every surviving-citation figure below is a FLOOR, not a count, and the direction is the unhelpful
one.** Citation spellings vary in this repository and a pattern aimed at one spelling misses the
others. Two measured instances, each carrying its population:

- A `\.md:[0-9]+` pattern found **1 of 26** citations of one document, the other 25 being written in
  the abbreviated extensionless form.
- The re-point pass reported **311** live citations where an exhaustive pass measured **351** genuine
  ones at that pass's own base ref. `verify_content.py` records this under `A35`: "311 collapsed a
  range to one occurrence and came from an instrument that could not see the extensionless or
  bare-short spellings; 351 counts each coordinate."

A third instance was measured for this amendment, inside this tree's own accounting, and it is the
same shape. `content/transcription-notes.md` states **419** frozen citations over **twenty**
documents in its per-definition-notes section; an exhaustive sweep of that same region measures **430
over 21 documents**, and the file's own table directly beneath the claim has **21 rows summing to
419**. Every per-document row agrees with the sweep except two, and the entire 11-citation gap is one
spelling — the elided-slug form `docs/NN-...md` — 10 citations of
`docs/31-initial-alien-roster.md` and 1 of `docs/72-player-survivability-and-damage-baseline.md`. The
claim here is about spellings and stays that size: it is **not** a claim that every citation
instrument in this project sees only one spelling.

**The remedy, recorded because it is the one that worked when it was tried: sweep exhaustively over
the general shape and enumerate the permitted forms, rather than assembling a union of patterns each
aimed at a spelling somebody remembered.** A union of remembered spellings is not a corpus. Every
figure below comes from such a sweep, run at `3db56c5` over the twelve files this tool owns: every
`<token>:<digits>` occurrence extracted first, then each occurrence resolved to a document through an
enumerated form vocabulary — full path with extension, abbreviated path without extension, bare
filename without the `docs/` prefix, elided slug, bare document number — with clock timecodes,
`content/` paths and one uppercase illustrative exhibit separated out and named.

**So the confirmed count is "at most 7 of the 33", not 7.** A document for which the sweep found no
surviving citation may hold one in a spelling the sweep does not enumerate, and the instrument that
produced these figures is from the same family as the two above. `A35`'s own recall measurement is the
calibration: it catches 308 of 351 genuine citations, missing 43 of a single shape.

**The commits' own declarations assert 37; that number is recorded here as a declaration, not as a
measurement.** `c05aee9` and `e24a52a` removed every line citation of
`docs/technical/40-content-data-and-validation.md` ("so no line in that doc is load-bearing"), and
`85d0ced` re-pointed the line citations of **36 further documents** and states plainly, "The owners of
all 36 documents can change their line counts" — 37 in total. That prose **names no members**: set
against the 33 confirmed by diff, **four members are unaccounted for**, and no pass has produced them.
Publishing 37 as a measured release count would repeat the exact defect this section exists to
document — a count nobody enumerated. (The likeliest explanation of the gap is documents cited only in
bare or verbatim-note form, which a path-prefixed scan does not enumerate; that is a conjecture, not an
enumeration, and it does not name the four either.)

**Why this correction exists** — it is this section's own subject turned on itself: two independent
methods agreed on a count of 32 while omitting *different* documents, so their agreement manufactured a
confidence neither had earned, which is why this record publishes membership rather than cardinality.

**Membership of this list is not a boundary, because on the published criterion it has no complement.**
Amended 2026-08-11: the criterion above holds of every cited document, so this list is not the set of
documents permitted to re-flow — all 36 are permitted, and the five annotated further down are not the
remainder of it. What the list records is which documents the release commits demonstrably touched, and
nothing more.

The 33 as the list stood, sorted — read now as *the documents the release commits demonstrably
touched*, and with `docs/weapons/README.md` **struck** from it by the verdict recorded below, leaving
membership at **32**:

- `docs/10-core-game-loop.md`
- `docs/30-combat-weapons-movement-camera.md`
- `docs/31-initial-alien-roster.md`
- `docs/32-standard-wave-and-beacon-schedule.md`
- `docs/35-playable-mechs.md`
- `docs/36-initial-mech-catalog.md`
- `docs/40-mining-and-extraction.md`
- `docs/50-maps-resources-and-navigation.md`
- `docs/51-standard-map-generation-contract.md`
- `docs/60-resources-crafting-progression.md`
- `docs/61-specialized-resource-identities.md`
- `docs/62-permanent-powerup-catalog.md`
- `docs/63-permanent-option-unlock-catalog.md`
- `docs/65-weapon-stat-and-branch-upgrades.md`
- `docs/66-weapon-catalog-and-resource-graph.md`
- `docs/68-utility-catalog.md`
- `docs/69-initial-relic-catalog.md`
- `docs/71-initial-weapon-numeric-catalog.md`
- `docs/72-player-survivability-and-damage-baseline.md`
- `docs/glossary.md`
- `docs/open-questions.md`
- `docs/data/README.md`
- `docs/data/survivability-baseline.csv`
- `docs/data/weapon-base-balance.csv`
- ~~`docs/weapons/README.md`~~ — struck; see the verdict below
- `docs/technical/22-combat-and-weapon-runtime.md`
- `docs/technical/23-encounter-director-and-enemy-runtime.md`
- `docs/technical/24-mining-fabrication-and-progression-runtime.md`
- `docs/technical/40-content-data-and-validation.md`
- `docs/technical/100-build-dependencies-and-release-operations.md`
- `docs/technical/110-implementation-plan-for-ai-agents.md`
- `docs/technical/114-autonomous-agent-execution-protocol.md`
- `docs/technical/115-component-contract-and-schema-registry.md`

**The 7 confirmed under the retired criterion (b), re-measured at `3db56c5`** — zero surviving line
citations in any enumerated spelling across the twelve owned files, and each of them cited at
`origin/master`, so it is the release that removed them. The count in brackets is the `origin/master`
figure falling to zero:

- `docs/35-playable-mechs.md` (2 → 0)
- `docs/open-questions.md` (2 → 0)
- `docs/data/survivability-baseline.csv` (1 → 0)
- `docs/data/weapon-base-balance.csv` (1 → 0)
- `docs/technical/100-build-dependencies-and-release-operations.md` (1 → 0)
- `docs/technical/114-autonomous-agent-execution-protocol.md` (2 → 0)
- `docs/technical/115-component-contract-and-schema-registry.md` (2 → 0)

**The other 26 still carry surviving line citations at `3db56c5`. Every figure in the right column is
a floor over the twelve owned files, not a count**, for the reason given above:

| document | surviving citations at `3db56c5` (floor) |
| --- | --- |
| `docs/68-utility-catalog.md` | 232 |
| `docs/technical/40-content-data-and-validation.md` | 113 |
| `docs/69-initial-relic-catalog.md` | 92 |
| `docs/36-initial-mech-catalog.md` | 64 |
| `docs/65-weapon-stat-and-branch-upgrades.md` | 64 |
| `docs/71-initial-weapon-numeric-catalog.md` | 60 |
| `docs/60-resources-crafting-progression.md` | 57 |
| `docs/50-maps-resources-and-navigation.md` | 50 |
| `docs/66-weapon-catalog-and-resource-graph.md` | 47 |
| `docs/40-mining-and-extraction.md` | 43 |
| `docs/61-specialized-resource-identities.md` | 42 |
| `docs/72-player-survivability-and-damage-baseline.md` | 36 |
| `docs/51-standard-map-generation-contract.md` | 33 |
| `docs/63-permanent-option-unlock-catalog.md` | 33 |
| `docs/31-initial-alien-roster.md` | 31 |
| `docs/technical/24-mining-fabrication-and-progression-runtime.md` | 30 |
| `docs/technical/23-encounter-director-and-enemy-runtime.md` | 25 |
| `docs/32-standard-wave-and-beacon-schedule.md` | 23 |
| ~~`docs/weapons/README.md`~~ (struck from the list) | 20 |
| `docs/data/README.md` | 15 |
| `docs/62-permanent-powerup-catalog.md` | 13 |
| `docs/technical/22-combat-and-weapon-runtime.md` | 10 |
| `docs/10-core-game-loop.md` | 2 |
| `docs/30-combat-weapons-movement-camera.md` | 2 |
| `docs/glossary.md` | 2 |
| `docs/technical/110-implementation-plan-for-ai-agents.md` | 1 |

**The arithmetic, so the two figures in this subsection do not read as a conflict:** 33 was 7 + 26 over
the list as published. Striking `docs/weapons/README.md` — one of the 26 — leaves membership at **32 =
7 confirmed citation-free + 25 carrying frozen citations**. The row above is kept in the table, struck,
because its measured figure is part of the evidence for striking it.

**Three of those figures disagree with the ones this amendment was briefed with, and all three
disagreements are the instrument, not the arithmetic.** `docs/31-initial-alien-roster.md` measures 31
against a reported 21 — the 10 extra are the elided-slug spelling, the same 10 the tree's own frozen
table misses. `docs/72-player-survivability-and-damage-baseline.md` measures 36 against a reported 34
— 2 extra, elided-slug again. `docs/technical/40-content-data-and-validation.md` measures 113 here and
agrees, but was **118** one commit earlier at `39b88b3`: five bare-number citations invisible to a
path-prefixed instrument, which `3db56c5` has since re-pointed. A fourth, smaller disagreement: the
brief's "five at 1–2" is four in this population — `docs/10-core-game-loop.md`,
`docs/30-combat-weapons-movement-camera.md`, `docs/glossary.md` and
`docs/technical/110-implementation-plan-for-ai-agents.md`. The fifth was
`docs/00-game-vision.md`, which is not a member of the 33 and is recorded separately below as a
regrowth case.

**The structural reason the 26 still carry citations, quoted from the tree rather than paraphrased.**
`content/transcription-notes.md` says of its doc-40 citations that they are "reproduced verbatim from
the deleted `notes` arrays, under the rule stated at the top of this file ('Note text stays verbatim
even where a ruling has since superseded it'), so they are **not** rewritten here." And of the
remainder: "**The other 419 `<doc>:<n>` citations below are frozen note text too, and every one of
them still resolves** … Same rule, same reason: they are reproduced verbatim from the deleted `notes`
arrays and are **not** rewritten." Measured at `3db56c5`: **558 citations survive in
`content/transcription-notes.md`** — that figure is over the whole file, and the file's own two notes
account for 530 of them over the subsections those notes head — and **656 survive in
`src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json`**, spread across 22 documents.
Preservation was the instruction those files were following, so the survivors are compliance with it,
not drift from it.

**~~SUPERSEDED — `docs/weapons/README.md`'s status was published as unresolved.~~** The paragraph read:
*"One member's status is unresolved: `docs/weapons/README.md`. Two passes disagree about it — one
reports its citation removed and re-added, which means the claim was withdrawn in place and therefore
never re-pointed; the other reports its `docs/weapons/README.md:48` re-pointed to `` `## Design state` ``
and `` `## Catalog lookup` ``. A full-path diff is being run to settle it. Neither reading is adopted
here: the membership figure stands at 33 with this one document's status pending, and a second
amendment to this section will follow when the verdict lands."*

**RESOLVED: `docs/weapons/README.md` is NOT released, and it is struck from the list above.** The full-
path diff settles it four ways, and its citations **increased** rather than falling — 18 at
`origin/master`, 20 at `3db56c5`:

- The coordinate the second reading called re-pointed appears **verbatim on both sides** of
  `85d0ced`'s diff. Only the sentence around it was withdrawn — "is worth recording as unresolvable"
  became "was recorded here as unresolvable". That is a line rewritten around a surviving citation,
  not a re-point.
- `85d0ced` **added** a further occurrence of that same coordinate, as a resolution aid stating which
  heading it means. A commit in the release increased this document's citation count.
- Its fifteen citations of a second coordinate were never touched at all: fifteen at `origin/master`,
  fifteen at `3db56c5`, absent from every release diff.
- The remaining occurrence is in this file, in the pull-request paragraphs below.

**The coordinates themselves are not reproduced in this paragraph, deliberately**, for the reason
given in the enforcement subsection: naming one here would add a citation `A35` exists to forbid, and
would redden the gate.

**Where the contrary reading came from: a resolution table read as a re-pointing table.**
`content/transcription-notes.md` carries a table headed "heading each `:<n>` below resolves to", whose
purpose is to say what frozen coordinates *mean* so a reader need not resolve them. Read as a record
of re-pointing, it turns preserved citations into removed ones. It is the second table in that file to
be read that way, and both readings inverted the same distinction this subsection now leads with —
live citations removed, frozen citations kept.

**The verdict was right by the wrong route, and that is worth recording separately.** The peer ruling
that `docs/weapons/README.md` was a spurious member is **upheld** — but it was reached by reconciling
counts, and it is upheld here by a full-path diff. The derivation changed even though the verdict did
not. A conclusion being correct does not retroactively license the route that produced it, and a
record that quietly keeps the right answer teaches the wrong method.

**`docs/00-game-vision.md` is a regrowth case, and the regrowth happened in the gate file itself.**
Zero line citations at `origin/master`. **Two** at `39b88b3`, both of one coordinate in
`src/MechaMiner.Tools/ContentImport/verify_content.py`, introduced by commit `29a9b8d` — *after* all
three release commits. The regrowth this section warned was ungated had already happened once, in the
file that does the gating. It is now repaired: `3db56c5` re-pointed both to `docs/00-game-vision.md`
`## Player fantasy`, and the figure at `3db56c5` is **zero**.

**The 2 and the 3 on that document are two populations, not a discrepancy.** The **2** counts line
citations *of* `docs/00-game-vision.md` across the twelve owned files at `39b88b3`. A separate
stream's **3** counts `docs/technical/40-content-data-and-validation.md`'s own citations of
`docs/00-game-vision.md`, three occurrences among that document's 26. Different denominators; neither
figure corrects the other. The third occurrence stays named as unaccounted-for in this record rather
than guessed at.

**`docs/technical/10-runtime-architecture.md` was never cited by line at all, and the coordinate
credited to it belongs to a different document.** That path occurs zero times in any enumerated
spelling across the twelve owned files at `origin/master` and at `3db56c5`. The single bare-number
coordinate read as its citation sat in `content/transcription-notes.md` at `origin/master`, and
`85d0ced`'s replacement line **in the same hunk** names what it meant: the bare pair became
`docs/50-maps-resources-and-navigation.md` `` `## Randomized resource profile` `` and
`docs/10-core-game-loop.md` `` `### Crafting and upgrades` ``. So the coordinate was
`docs/10-core-game-loop.md`'s. **And that document is not released either** — 3 citations at
`origin/master`, **2 surviving** at `3db56c5`, both in
`src/MechaMiner.Tools/ContentImport/expected_citation_deltas.json`. The reassignment is recorded
because a zero credited to the wrong document is worse than no measurement: it retires a question
nobody answered.

**The count is method-sensitive, and a path-prefixed scan undercounts.** A scan that only matches
`docs/…:<n>`-prefixed citations in the diffs surfaces fewer documents than were actually released,
because it is blind to the bare `<NN>:<n>` spelling — `docs/technical/22` alone carried **58** citations
in that form, re-pointed to its own headings and gone at HEAD, and was invisible to that instrument
entirely — and it also misses citations spread across the other owned files. Two such scans each produced
32, each dropping a *different* single document (one `docs/weapons/README.md`, the other
`docs/technical/22`). Do not treat "32" as the release count.

**That paragraph stands, and the exhaustive sweep confirms its exhibit:** the bare spelling of
`docs/technical/22-combat-and-weapon-runtime.md` measures exactly **58** occurrences at
`origin/master` and zero at `3db56c5`. What has changed is its status — it was written as a caution
about one instrument, and the amendment above generalises it: spelling blindness is the reason every
surviving-citation figure in this section is a floor.

### Five documents whose line citations are pinned inside a frozen artifact — a non-exclusive annotation, not a not-released set

**~~SUPERSEDED — this subsection was headed "Five documents are NOT released — the inverse of a
release, not a smaller one", and it published these five as the complement of the released list.~~**
Amended 2026-08-11, the second amendment to this subsection, and the heading above is the amendment.
**On the criterion this section now publishes there is no not-released set for this list to be.** The
shift experiment covered all 36 cited documents, every gate returned byte-identical output, and it was
reproduced on a forced failure path: nothing in this tree — no runtime path and no gate — depends on a
`docs/**` line coordinate. So **every document is free to re-flow, these five included**, and that is
the entire content of "released" now. An inverse list has nothing left to hold.

**What this list does hold is the question the retired criterion was actually tracking: which documents
have line citations *pinned inside frozen artifacts*, so that re-flowing them leaves those pinned
citations stale.** Under the proved property that is a **documentation defect, not a breakage** —
nothing reads the stored numbers, so nobody will be stopped by a stale one, and that is precisely why
it needs recording rather than leaving to be noticed.

**This annotation is explicitly NON-EXCLUSIVE, and overlap with the released list is expected rather
than tolerated.** The two lists answer different questions — *may this document re-flow* (yes, of every
one of them) and *does re-flowing it strand a pinned citation* — so a document can sit in both at once
with both entries correct. `docs/technical/22` and `docs/technical/24` do sit in both, and that is
**not a contradiction**; neither entry is struck for it, and a later pass should not reconcile them by
removing one.

**AMENDMENT, 2026-08-11 — this list carries the same defect as the released list, and gets the same
fix.** Its stated criterion is *surviving citations that live only inside the frozen measurement
artifact* — and that artifact,
`src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json`, carries **656 citations across 22
documents** at `3db56c5`, verified by the sweep described above. So the criterion as published does not
pick out five documents, on either reading of it:

- **Read strictly — cited by line, and only inside that artifact — TWO further documents qualify, for
  seven in place of five.** The two are `docs/technical/22-combat-and-weapon-runtime.md` (10 citations,
  all in that file) and `docs/technical/24-mining-fabrication-and-progression-runtime.md` (30
  citations, all in that file). **Both are also listed as released by this same section**, which the
  ruling above settles: the two lists answer different questions, so appearing in both is expected and
  is not a contradiction.
- **Read loosely — has surviving citations inside that artifact — SEVENTEEN further documents qualify,
  for twenty-two in place of five.** The other fifteen of the 22 carry citations in that artifact *and*
  in `content/transcription-notes.md`, which is why they fail the strict reading.
- **The pairing is strict → two further, loose → seventeen further, in that order.** It is stated twice
  here because an earlier brief carried the two figures the other way round; the order above is the
  corrected one and a pass that finds them reversed anywhere is looking at the uncorrected brief.

**The property that actually distinguishes these five, stated so the list has a criterion it
satisfies: the three release commits touched no citation of them, because they carried no live
citation to re-point.** That is a fact about the release, not about runtime. Under the criterion now
published — no runtime or gate behaviour depending on a `docs/**` line coordinate — these five are
released like everything else, and this subsection's own next paragraph is the evidence:
`check_quote_mismatch_evidence.py` rebuilds every span from the live documents by anchor slug and never
reads the stored numbers. What their owners owe is not a runtime break but a re-derivation of a frozen
measurement, at exactly one path.

The five, unchanged as a list:

- `docs/67-mech-relics.md`
- `docs/73-interface-screen-flow-and-information-architecture.md`
- `docs/20-run-structure-and-timing.md`
- `docs/70-combat-and-economy-balance-framework.md`
- `docs/decisions/DEC-120-accept-permanent-powerup-catalog.md`

**The constraint, re-measured at `3db56c5` and unchanged: 111 surviving line citations *of these five
documents*, all of them in `src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json`, which
holds 656 citations of 22 documents in total** — `67` 63, `73` 40, `20` 4, `70` 3, `DEC-120` 1. **That
111 is one of two different 111s in this section, and each needs its population attached whenever it
appears:** this one is *the five annotated documents' citations inside
`quote_mismatch_evidence.json`* — annotated, not "not released", per the ruling at the head of this
subsection; the other is *`content/transcription-notes.md`'s own count of its
doc-40 citations in one subsection of that file*, which the sweep confirms at 111 and which is a
population of one document in one file. Same digits, unrelated denominators. The sibling artifact `src/MechaMiner.Tools/ContentImport/expected_citation_deltas.json`
carries **zero** citation of any of the five in any form, so the constraint lives wholly in the first
file. Those 111 are the `(path.md:<a>-<b>)` tail of a `doc_id#anchor` reference, frozen at the sweep
ref; `check_quote_mismatch_evidence.py` rebuilds every span from the live docs by anchor slug and never
reads the stored numbers, so they do not resolve at runtime — but they are the surviving textual line
citations, and rewriting them would corrupt a frozen measurement. **~~SUPERSEDED — the actionable fact
as this paragraph used to state it.~~** It read: *"**The one actionable fact for an owner:** these five
may not be treated as freely re-flowable the way **the 32 documents the release commits re-pointed**
are — the released set as this section now lists it, not the retired declaration figure of 37; before
their line geometry is changed, the frozen artifact must be re-derived, and the change lives at exactly
one path, `quote_mismatch_evidence.json`."* That is the partition reading, and the ruling at the head of
this subsection replaces it: **these five are as freely re-flowable as every other document**, because
no runtime or gate behaviour depends on a `docs/**` line coordinate of any of them. **The actionable
fact as it now stands:** re-flowing one of these five leaves its pinned spans stale inside
`quote_mismatch_evidence.json` — a documentation defect at exactly one path, not a break and not a
precondition — so the owner may re-flow first and re-derive the artifact after, and the re-derivation is
owed either way because nothing will fail to remind them.

### Two documents were frozen at a fixed line count by their authors — both freezes are now lifted

- **`docs/66-weapon-catalog-and-resource-graph.md`.** Commit `0648186` appended a pointer *in place* —
  "the pointer is appended in place so docs/66 keeps its line count, because docs/66:37 and docs/66:39
  are cited by line from verify_content.py and content/transcription-notes.md." That freeze is lifted:
  `85d0ced` re-pointed those citations to `` `## Accepted base catalog assignment` ``, and doc 66 is in
  the released set above.
- **`docs/technical/40-content-data-and-validation.md`, held at 431 lines** (still 431 at HEAD). Its
  line citations were the subject of `c05aee9` and `e24a52a`; with them gone, the fixed line count no
  longer protects anything and the freeze is lifted.

### Ownership — three relationships, and "owner unknown" is a measurement, not an omission

- **No document has a recorded owner.** `CODEOWNERS` is absent from the working tree and from every
  reachable git object (no ref and no history touches it), so "owner unknown" for a document is a
  measured fact about this repository, not a gap in this record.
- **Authored by the human owner.** The ten `docs/technical/*` documents and `docs/data/README.md` and
  `docs/weapons/README.md` were authored by Porter Westling (`pwestling@gmail.com`) in commits
  `21c555f` ("Add comprehensive gameplay design specification") and `739bf29` ("Add autonomous
  implementation technical specification"). For those the record says *authored by the human owner*.
  Every other document is owner-unknown.
- **Reviewer-authored replacement text resides in documents the reviewer does not own**, including
  `docs/technical/40-content-data-and-validation.md`. This is a third relationship, distinct from
  authorship and from ownership.
- **Git authorship cannot distinguish sessions.** Reachable commits are near-uniformly
  `Claude <noreply@anthropic.com>` (only Porter's two commits and a handful of `claude[bot]` commits
  differ), so who did what rests on the `Claude-Session` commit trailer, not on the author field.

### A GitHub-side citation population no working-tree gate can reach

**Recorded 2026-08-11 in `8c2e643` ("content: record the line-citation release and the surfaces this
tree cannot gate"); the sweep has not been re-run since.** Every figure below — the 506, the 113-artifact
corpus, and each of the four dispositions — is a measurement taken for that commit and of no later tree.
This stamp was added by a later pass that re-measured nothing. **What the ref the sweep compared against
can and cannot be established from the local ref set:** `origin/master` in this clone was
`e17b8b666476b118da86250e90eec062cabf0ede` when the census was recorded — its ref last moved at
2026-08-11 01:05:07 +0000, ten hours earlier, and `git rev-parse origin/master` still returns that sha —
so `e17b8b6` is what `master` resolved to locally at that moment. No fetch happened between that update
and the recording, so this record does **not** assert that the remote `master` was `e17b8b6` when the
sweep ran; that is not checkable from here.

A gate walks the working tree; it cannot see a pull-request body, a review, a review comment, an issue
body, a commit message, or a cross-session message. A sweep of the GitHub artifacts found **506
positional citations across 27 of 36 PRs**. Corpus: **113 text artifacts** — 36 PR bodies, 34 review
bodies, 43 issue comments, and 0 inline review comments (a cross-checked empty set, not an unchecked
one). Of the 506:

- **79 are wrong against `master` as of that 2026-08-11 recording**, of which **26 were valid at their
  own PR head** — the coordinates drifted after merge; the claim never changed;
- **166 are uncheckable by construction**, because they cite an ambiguous bare document number — **149
  of them `40`**, which names two documents (`docs/40-mining-and-extraction.md` and
  `docs/technical/40-content-data-and-validation.md`) differing by 207 lines;
- **26 are verified correct**;
- **239 are in range but unverified** — 213 of them make their claim in prose rather than in a quotable
  fragment.

**This surface is not swept and mostly cannot be:** a merged PR body is a historical record with no
routine edit path. So for this population the record itself is the delivery, not a fix.

### Three GitHub-side citations that resolve to a plausible-but-wrong line — three different dispositions

Calling all three "wrong" would be false; each is a distinct thing.

- **PR #16 — `docs/72:236`, an INVERTED citation (repair drafted; applied pending verification).** On
  `master`, line 236 is the elite/boss resistance sentence, while the claim it is cited for — "multiply
  the already resisted displacement magnitude or timed-control duration by 0.80" — sits at **line 234**,
  under the h2 at line 223. A reader who checks 236 lands on a sentence about resistance, believes the
  claim verified, and stops; that inversion is what distinguishes it from a merely stale coordinate. The
  drafted replacement is
  `` docs/72-player-survivability-and-damage-baseline.md `## Control Resistance and Status Stacking` ``.
- **PR #14 — `docs/technical/40-content-data-and-validation.md:106`, DRIFTED COORDINATES, not an
  error (repair drafted; applied pending verification).** It was correct against blob `4cded84`, and the
  surrounding prose pins it to that blob explicitly; `master` later re-flowed the sentence to line 231.
  "The citation was wrong" would be false — the coordinates moved, the claim never changed. The drafted
  replacement is `` docs/technical/40-content-data-and-validation.md `### Resources` `` — an **h3, three
  hashes**, not an h2.
- **PR #4 — `docs/51:161`, STRUCTURALLY UNREPAIRABLE.** It sits in a submitted **review** body, and
  GitHub exposes no edit for a submitted review; neither a new review nor a dismissal alters the
  original text. No follow-up review comment was posted, deliberately: it would add a second artifact to
  the record without fixing the first.

**The durable lesson, stated at the reader rather than the writer:** a read-modify-write through a
lossy reader is corrupted *before* the write happens. The MCP reader of a PR body drops the
`<!-- ccr-projects-attribution -->` marker that every body in this repository must open with and
HTML-escapes quotes (measured: 19,392 raw characters against 19,359 through the reader, the 33-byte gap
being exactly the marker), so the safe repair path reads the raw body by plain unauthenticated request
(the repo is public), edits that string, and submits it — never round-trips through the lossy reader.

### One frozen wrong claim, named because it cannot be fixed

Commit `85d0ced`'s message asserts that doc 22's owner "chose NOT to cite its table cells by line."
**That is a misreading.** Commit `e77fa0d` ("docs(40): grant the behavior-token vocabularies for all
twelve call sites") is a change to `docs/technical/40` that restrains **its own** citations of doc 22's
closed-list cells — it classifies against doc 22's lists rather than citing their cells by line. It is
not doc 22's owner acting; like nearly every commit here it is authored `Claude <noreply@anthropic.com>`.
History is not rewritten, so the false claim survives in `85d0ced`'s message forever; this entry is the
only place a reader who finds that commit can learn the correct reading.

### Trailer and attribution census

Measured over **35 remote refs on 2026-08-11**: one git author across essentially all commits, six
distinct `Claude-Session` trailer values, and roughly **82%** of commits attributable from the commit
object alone. The figure carries its ref count and its moment because it is unauditable without them —
a later fetch changes the ref set. No internal sum is offered here as a cross-check: an identity that
closes within one enumeration verifies the addition, not the population.

**The trailer-less residual — 78 commits that carry no `Claude-Session` trailer — is unmeasured.** (This
78 is a count of *trailer-less* commits; it is not the unrelated "78" some measurements use as a floor on
*trailered* commits over a 230-commit union — opposite property, incompatible denominator, same digits.)
It is known to be **two populations**: some share is compliance with a since-superseded prohibition on
model identifiers in commit footers — not carelessness — and the split between that and the rest has not
been measured. It is not derived by subtraction here, because a subtraction from a total is not a
measurement of the remainder.

### Enforcement of the release — gated as of `3db56c5`, amended 2026-08-11

**~~SUPERSEDED — this subsection used to read as follows.~~** *"No citation-prohibition assertion has
landed in the verifier: the highest assertion at HEAD is `A34` (the `resource_class` coupling). `A24`
already forbids a `:<digits>` line number inside a `content/` domain **value**, but that is the
content-JSON surface, not the design-document citations in this tree's prose, comments, and failure
messages. So **regrowth of `<doc>:<n>` citations in the owned files is not yet gated** — an assertion
that would gate it is forthcoming, and this record should not be read as claiming a check already
prevents the defect from returning."* The `A24` sentence remains true and its scope distinction still
holds. The rest is out of date: commit `3db56c5` landed `A35` and `A36`, and the highest assertion at
HEAD is now `A36`.

**The instance worth more than any count in this subsection: a repair reintroduced the very defect it
was repairing, and the new gate caught it on its first live use.** The pass that removed the bare-number
citations from `content/transcription-notes.md` wrote a dated note recording what it had removed — and
that note *named the coordinate it was removing*, so the record of the removal carried the form the
removal existed to delete. The repaired note now states the rule in its own words: *"The coordinate
itself is not reproduced here: naming it would reintroduce the very form this repair removes."* The
same commit repaired the same shape one layer up, in an unqualified negative about a phrase: *"a
negative claim about a phrase falsifies itself the moment it is written down, because the record of the
claim contains the phrase."* Both are one failure — **a record of an absence that quotes what it says
is absent restores it** — and it is the failure most likely to recur here, because this section is
nothing but records of removals. **This amendment follows the rule rather than describing it:** every
coordinate discussed above is named by document and heading, never reproduced. The `docs/weapons/README.md`
paragraph says so where it applies.

`A35` — **no positional citation of a design document in the twelve files this tool owns.** Seven
spellings are matched: a `docs/` path with the extension present or absent, a bare document filename, a
bare short document name, ranges in any punctuation, a bare coordinate inheriting its document from an
antecedent, the `line`/`lines` form, and the `L` / `#L` forms. The short-name and filename sets are read
from a recursive walk of `docs/` on every run rather than listed in the file. At `3db56c5` it reports
**0 files carrying a non-exempt citation** — and the assertion says so in its own output, so it is
recorded here too: **that zero is a count over files, not over citations**, and the six citation
surfaces no working-tree gate can reach are printed beside it on every run.

**The exemptions are an exact declared set, not a pattern — 13 buckets holding 1312 citations, each
with its own reason and a pinned count.** Adding a citation to an already-exempt bucket **fails** the
gate, which is the designed difference between an exact set and a pattern: the exemption covers the
citations that were audited, not the location they happen to sit in. **`content/README.md` is one of
those buckets, with five entries covering six citations** — so this file can no longer record a
coordinate in passing, and an amendment to this section must use the heading form. Recall is measured
rather than assumed: against the file contents at `e24a52a`, `A35` catches **308 of 351** genuine
citations, and the **43** it misses are one undecidable shape — a bare short name followed by a
two-digit number in clock range, a string indistinguishable from the schedule timecodes this
repository's notes transcribe verbatim.

**The pinned count also enforces supersede-not-delete mechanically, and this paragraph is addressed to
whoever arrives later wanting to tidy a superseded paragraph out of this file.** A bucket's count is
pinned in **both** directions — `A35` fails when the population it finds differs from the declared one
either way — so **deleting a superseded paragraph that contains one of `content/README.md`'s six exempt
citations drops that bucket below its pinned count and fails the gate**, exactly as adding an
unaudited citation would. That is a property of the design rather than a coincidence of these
particular counts: the exemption covers the citations that were **audited**, so removing one changes the
audited set as surely as adding one does. The consequence, stated plainly so it is learned here instead
of by reddening a gate: the superseded paragraphs above cannot be deleted, only superseded — marked,
quoted, and explained by what replaced them, the way every amendment in this section does it. If a
deletion is genuinely correct, it takes an edit to that bucket's declared count and a reason recorded in
the entry, never a quiet removal.

`A36` — **an ambiguous short-name reference must carry its full path.** The collision set is computed
from a recursive walk of `docs/**/*.md` on every run and printed in full: **201 documents, 12 colliding
short names** (9 numeric, plus `README` shared by 7 documents and `conventions` and `open-questions`
each shared by 2). **253 short-name references all resolve to exactly one document, and 0 carry a line
number.** The recursive walk is load-bearing: non-recursively there is exactly one document numbered
40, the collision set computes empty, and the row would pass forever while the ambiguity sat one
directory below.

**What is gated and what is not.** Regrowth of a `<doc>:<n>` citation in the twelve owned files is now
gated, and the `docs/00-game-vision.md` case above is the proof that it needed to be — that regrowth
had already happened, in the gate file itself, after the release. What remains ungated is everything
outside those twelve files, including the whole GitHub-side population recorded above, and the 43-case
shape `A35` records as beyond a text scan.
