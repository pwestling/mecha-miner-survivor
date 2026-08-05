# `content/` — source gameplay catalog JSON

This tree holds the initial JSON definitions transcribed from the accepted gameplay design documents
in `docs/`, per work package **DAT-007** ("Import accepted gameplay catalogs into initial JSON
definitions", `docs/technical/110-implementation-plan-for-ai-agents.md:216`).

Nothing here has been schema-validated. The canonical schemas (`DAT-001`..`DAT-006`) do not exist
yet; see [Reconciling with the canonical schemas](#reconciling-with-the-canonical-schemas).

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
| `resources/` (9 files: `A`–`F`, `common-ore`, `hyper-gold`, `geode-resonance-effects`) | catalog transcription (CAT) | authored |
| `mechs/` (7: `MCH-01`–`MCH-06`, `shared-baseline`) | CAT | authored |
| `enemies/` (11: `EN-01`–`EN-10`, `elite-modifier-profile`) | CAT | authored |
| `bosses/` (4: `BOSS-01`–`BOSS-04`) | CAT | authored |
| `weapons/` (16: `W-AB`…`W-EF`, `stat-price-formula`) | CAT | authored |
| `branches/` (45: `<weaponId>-<branch-name>`) | CAT | authored |
| `utilities/` (13: `UTL-A1`…`UTL-F2`, `radar-unassigned-id`) | CAT | authored |
| `relics/` (10: `REL-01`–`REL-10`) | CAT | authored |
| `powerups/` (13: `PU-*`) | CAT | authored |
| `unlocks/` (6: `UNL-01`–`UNL-06`) | CAT | authored |
| `mining-sites/` (4 prose-derived site classes) | CAT | authored |
| `encounters/` (1: `standard-encounter-schedule`) | CAT | authored |
| `maps/` (2: `standard-map-generation-contract`, `world-props`) | CAT | authored |
| `schemas/` | schema stream (`DAT-001`, `DAT-002`, `DAT-003`) | **not authored here** |
| `presentation/` | presentation/audio definitions, `SCH-CNT-003` (`docs/technical/115-component-contract-and-schema-registry.md:91`) | **not authored here** |
| `localization/` | localization stream (`DAT-009`, `docs/technical/110-implementation-plan-for-ai-agents.md:218`) | **not authored here** |
| `../generated/` | bundle compiler and report generators (`DAT-006`, `DAT-008`); "Generated files are changed through their generator" (`docs/technical/110-implementation-plan-for-ai-agents.md:92`) | **not authored here** |

`content/schemas/`, `content/presentation/`, `content/localization/`, and `generated/` are absent from
this tree because they belong to other streams, not because they are optional.

## Authoring conventions

These are the conventions the transcription actually followed. A reviewer can check compliance
against this list.

- **One JSON file per stable catalog item, named by its exact doc ID.** `MCH-01.json`, `EN-07.json`,
  `BOSS-03.json`, `W-BE.json`, `REL-10.json`, `UTL-C2.json`, `UNL-04.json`, `PU-*.json`. IDs are
  copied verbatim from the design docs and never re-cased or re-numbered
  (`docs/technical/40-content-data-and-validation.md:66`: "Reuse accepted gameplay IDs exactly").
- **Kebab-case file names for cohesive aggregates** — the units that no doc gives an ID and that are
  not per-item catalogs: `shared-baseline.json`, `elite-modifier-profile.json`,
  `geode-resonance-effects.json`, `stat-price-formula.json`, `standard-encounter-schedule.json`,
  `standard-map-generation-contract.json`, `world-props.json`, the four `*-seams`/`*-geodes`/`*-sites`
  mining-site files, and `radar-unassigned-id.json`.
- **Branch files** are named `<weaponId>-<branch-name-kebab-case>.json` (e.g.
  `W-AD-singularity-forge.json`) because no doc assigns branch IDs.
- **Formatting:** 2-space indent, LF line endings, one trailing newline, UTF-8 without BOM.
- **Field names are camelCase, derived from the doc's own column headers** or, for prose-only content,
  from the doc's own noun phrase. Nothing is renamed for taste.
- **Units live in key names**, not in values: `movementSpeedMPerSecond`, `baseWorkSeconds`,
  `collisionDiameterM`, `recoveryHullPerSecond`, `costHyperGold`, `warningSeconds`.
- **Ranges are `{min, max}` objects**, never a string like `"8-10"`.
- **Percentages are `{percent: N}` objects** with human-readable percentage points, matching
  `docs/technical/40-content-data-and-validation.md:95`.
- **Per-rank values are rank-ordered arrays** (`ranks[0]` is rank 1), variable length: PowerUp rank
  arrays have 1, 3, 4, or 5 entries matching each entry's cap, and `PU-S04` has exactly one rank.
- **Values are transcribed, not computed.** Where a doc gives prose plus a number, both are kept: the
  verbatim string and the structured restatement side by side. No value is derived, rounded, or
  filled in. Facts the docs do not state are `null`, with the reason recorded in `_provenance.notes`.
- **The authoritative source wins over its mirrors.** The Markdown design docs are authoritative; the
  CSVs under `docs/data/` are mirrors (`docs/data/README.md:5,10` — "when values disagree, update the
  data mirror to match the Markdown"). Where a mirror and a doc disagree, the doc value is
  transcribed and the divergence is recorded in `_provenance.notes`.

## Provenance convention

Every file carries a top-level `_provenance` object:

| Key | Meaning |
| --- | --- |
| `doc` | repo-relative path of the authoritative source document |
| `section` | that document's heading the content came from |
| `lines` | line or line range within that document |
| `extractedFor` | work-package ID the extraction serves |
| `notes` | array of transcription decisions, divergences, and reasons for `null` values |

Individual fields and nested objects may additionally carry a `_source` block with the same `doc` /
`section` / `lines` / `notes` shape, used when one field comes from a different document than the rest
of the file (for example `signatureWeaponId` in the mech files).

**`_`-prefixed keys are extraction metadata, not gameplay data.** No runtime behavior may read them.

Known reconciliation item: if the `DAT-001`..`DAT-006` schemas land with
`"additionalProperties": false` — which `docs/technical/40-content-data-and-validation.md:90`
mandates in spirit ("Unknown fields are errors rather than silently ignored") — then `_provenance`
and `_source` must either be explicitly allowed by the envelope schema or moved out of the definition
files into sidecars (for example `content/<catalog>/<id>.provenance.json`). The provenance itself is
not optional: `source_refs` is a required envelope field
(`docs/technical/40-content-data-and-validation.md:87`) and `DAT-005` includes a source-trace
validator (`docs/technical/110-implementation-plan-for-ai-agents.md:214`). Only its carrier is open.

## Known gaps and required design decisions

93 gaps were recorded during transcription: 66 flagged as genuinely unresolvable from the documents,
27 resolved by choosing the authoritative source of two divergent statements. Grouped by kind:

### 1. Missing stable IDs (boundary decisions, not local choices)

`docs/technical/40-content-data-and-validation.md:66` requires reusing accepted gameplay IDs exactly,
but no document assigns one to:

- the six specialized materials — only letter codes `A`–`F`
  (`docs/61-specialized-resource-identities.md:18`); `RES-001`..`RES-006` are already taken by
  `docs/research/`. Files use the letter code verbatim as `id`.
- common ore and Hyper Gold — no doc token of any kind. Files use the slugs `common-ore` and
  `hyper-gold`.
- the resource radar, which is the thirteenth utility (`docs/50-maps-resources-and-navigation.md:106`;
  "12 utilities plus radar", `docs/technical/110-implementation-plan-for-ai-agents.md:181`) but never
  receives a `UTL-*` ID. Filed as `radar-unassigned-id.json` with `"id": null`.
- all 45 weapon branches — headings carry only `<Branch Name> — <Class> — 2 <Material>`.
- the elite modifier profile (`docs/31-initial-alien-roster.md:104`), `"id": null`.
- all four mining-site classes (`docs/40-mining-and-extraction.md:58-132`, prose-only, no table),
  `"id": null` — yet `docs/technical/40-content-data-and-validation.md:140` requires validating
  exactly four accepted classes.
- the shared mech baseline aggregate.
- both world props, destructible rock and health pack
  (`docs/72-player-survivability-and-damage-baseline.md:180,190`), `"id": null`.
- the standard encounter schedule's mode ID, required by
  `docs/technical/40-content-data-and-validation.md:144`; no `MODE-*` token exists anywhere.
- the map contract's mode/map ID and generation version, required by
  `docs/technical/40-content-data-and-validation.md:148,237`.

### 2. Envelope and naming-convention conflict with the technical spec

- The envelope fields required of every definition by
  `docs/technical/40-content-data-and-validation.md:76-89` — `schema_version`, `content_version`,
  `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id` — have **no source
  values anywhere in `docs/`**. None are authored here. They must be minted by the schema and
  localization streams.
- The technical spec's property names are `snake_case` with unit suffixes (`_m`, `_seconds`,
  `_percent`, …; `docs/technical/40-content-data-and-validation.md:92-100`), while this tree is
  camelCase with unit words in the key name (`movementSpeedMPerSecond`). One of the two must give
  when the schemas land; a mechanical rename is possible in either direction.
- No `presentation_id` values can exist until `content/presentation/` is authored, so no
  enemy/boss/weapon/relic definition can satisfy its presentation reference today.

### 3. Numbers the design docs explicitly leave open

Transcribed as `null` with the reason in `_provenance.notes`:

- geode resonance field radius (only "larger than its extraction zone") and the mining extraction zone
  radius itself.
- universal post-hit invulnerability duration (stated as "None", no duration).
- maximum safe count for all 8 resources, required by
  `docs/technical/40-content-data-and-validation.md:106`.
- Needler (`EN-06`) projectile lifetime and hitbox size.
- boss Armor for all four bosses; Riftjaw charge-lane width; Brood Titan minion-ring radius; Prism
  Crown projectile lifetime; Skybreaker Apex landing-marker diameter.
- `REL-06` clustering distance; `REL-07` explosion delay, Hull-scaling, generational decay,
  chain limit, and elite/boss cap; `REL-08` positional tolerance and heat build/vent rates — all
  explicitly left as tuning.
- relic rarity tier and relic-cache selection weight (do not exist anywhere in `docs/`).
- favorable-scene effect magnitude for 5 branches; `expectedEffect` for 17 branches whose sections
  state no estimate.
- Hyper Gold's icon, silhouette cue, and audio character (an explicit open question, unlike the six
  specialized materials).
- coverage roles for the six Advanced Utility Suite utilities (the doc's coverage-role column covers
  only the six fresh-profile utilities); the radar's primary role and Installed→Rank 3 summary.
- how `UTL-C2`'s recharging one-hit negation stacks with other negation/invulnerability sources.
- presentation, map marker, and spawn exclusions for every mining site, required by
  `docs/technical/40-content-data-and-validation.md:140`.
- map retry budgets and enumerated landmark pools, required by
  `docs/technical/40-content-data-and-validation.md:148`.

### 4. Live contradictions the docs do not resolve

- **`W-BE` Sentry Pod deployment ramp: 6 s vs 12 s**, stated both ways inside
  `docs/71-initial-weapon-numeric-catalog.md`. Needs an owner decision.
- Minute 33 of the schedule reads "Streams rotate through four sectors at 33:15 intervals", which does
  not resolve to explicit event times; every other formation row lists absolute `m:ss` times.
- `docs/data/weapon-branch-balance.csv:42` gives `W-DF` impact transfer "up to +225% secondary crowd
  damage per launched enemy"; `225` appears nowhere in the authoritative section.
- `EN-07` body scale: `0.62 × 0.80 M` = `0.496 M`, but `docs/data/survivability-baseline.csv` lists
  `0.50 M`.
- `_provenance.extractedFor` is inconsistent across this tree (`DAT-008` on 94 files, `DAT-007` on
  47). Catalog transcription is `DAT-007`; `DAT-008` is report generation
  (`docs/technical/110-implementation-plan-for-ai-agents.md:216-217`). One value must be chosen and
  applied.

### 5. Divergences resolved by choosing the authoritative document

Recorded in `_provenance.notes` on the affected files rather than silently normalized:

- Signature-mech assignment is stated three ways —
  `docs/66-weapon-catalog-and-resource-graph.md:39` says "Initial signature; mech TBD",
  `docs/weapons/README.md:20` gives only Yes/No, `docs/36-initial-mech-catalog.md:45` names the mech.
  Doc 36 is used; doc 66's column is treated as stale.
- The `Status at DEC-075` column of `docs/66-weapon-catalog-and-resource-graph.md:39` is knowingly
  stale (its own line 37 says so) and was not transcribed.
- Two different shared mech baselines exist: `docs/36-initial-mech-catalog.md:29` (9 rows, selection
  framing) and `docs/72-player-survivability-and-damage-baseline.md:32` (12 rows, simulation framing). They agree on
  every shared value but neither is a superset; `shared-baseline.json` carries the union with
  per-field `_source`.
- Duplicate geode resonance tables: `docs/40-mining-and-extraction.md:102` is used;
  `docs/61-specialized-resource-identities.md:94` omits the six effect names and two coupling
  qualifiers.
- Utility `UTL-A1` is spelled "Harmonic Calibrator" in the catalog and "Harmonic Amplifier" outside
  it; the catalog spelling is used.
- Several CSV mirrors drop numeric values present in the Markdown (`W-BF` and `W-DF` repeat
  intervals, `W-BE` "first pod immediate"); the Markdown values are transcribed.
- `MCH-05` trait stacking is stated in absolute Hull (125 / 170) while the other five mechs are
  percentages, so that field's shape is not uniform across the six files.
- `PU-S04`'s single rank costs 500 Hyper Gold, outside the prose's "early ranks cost 25-125" range.
- `REL-01`'s "one-third their ordinary travel interval" has no exact scalar representation and is kept
  as the verbatim ratio.
- Weapon ore-stat units vary per stat, so they cannot be encoded uniformly in key names.

## Reconciling with the canonical schemas

`content/schemas/` is empty and `DAT-001`..`DAT-006` are unimplemented, so **nothing in this tree has
been validated against a schema, compiled into a bundle, or hashed**. The counts and totals were
checked mechanically against the design docs only.

What to run today:

```sh
python3 tools/cat-extract/verify_content.py
```

That checks parseability, per-directory entry counts, `_provenance` well-formedness, the two
doc-stated grand totals (PowerUp ranks = 9,450 Hyper Gold; option unlocks = 2,150), and
branch→weapon / encounter→enemy / mech→weapon referential integrity. See
`tools/cat-extract/README.md`.

What to run once the schemas land, in this order:

1. `DAT-001` envelope validation over every file — expect failures for the missing envelope fields in
   gap group 2 and for the `_provenance`/`_source` keys under `additionalProperties: false`.
2. `DAT-002`/`DAT-003` per-category schema validation, including the cardinality and price validators
   (15 unordered material-pair recipes with no duplicate pair and exactly three stats each; one
   amplification, functional, and conversion branch per weapon —
   `docs/technical/40-content-data-and-validation.md:120`).
3. `DAT-004` behavior-registry validation — every `behavior_kind`, targeting policy, formula, modifier
   hook, formation, and effect must resolve to exactly one registered descriptor
   (`CTR-CNT-002`, `docs/technical/115-component-contract-and-schema-registry.md:62`). No behavior
   kinds are named in this tree yet; they must be assigned with the registry.
4. `DAT-005` cross-reference, semantic, analytical, localization, asset, and source-trace validators.
   Localization and asset checks will fail until `content/localization/` (`DAT-009`) and
   `content/presentation/` exist.
5. `DAT-006` bundle compile and hash — `generated/content.bundle.json` plus
   `generated/content.bundle.sha256`; a source-order permutation must yield an identical hash.
6. `DAT-008` report generation, then diff the generated balance report against
   `docs/data/*.csv` and `docs/70-combat-and-economy-balance-framework.md`.

Every gap in group 1 (missing stable IDs) blocks step 2, and every gap in group 4 (live
contradictions) must be decided by a document owner before the numbers here can be called final.
