# Content transcription notes

**This file is not an `OQ-###` open-question register.** The two registers are
`docs/open-questions.md` and `docs/technical/open-questions.md`. Nothing here carries or mints
an OQ ID. This file is the reviewable record of the per-definition transcription notes that
used to live in a `notes` array inside every JSON definition of the DAT-007 catalog import.
`notes` is extraction metadata, no schema will declare it, and unknown fields are errors
(`docs/technical/40-content-data-and-validation.md:26`, `:90`), so the arrays were deleted from
all 141 files and their contents moved here verbatim.

**Count assertion.** 945 note strings were removed from 141 `notes` arrays. This file
reproduces all 945 verbatim, partitioned between the two sections below — 3 in the contradictions section, 1 under “needs a ruling but not a two-passage contradiction”, 22 quoted inside the structural entries of the shape notes, and 919 in the per-definition listing (3 + 1 + 22 + 919 = 945).
No note text was edited, merged, summarised, or dropped. Four of the 141 files
(`content/mechs/shared-baseline.json`, `content/maps/world-props.json`,
`content/enemies/elite-modifier-profile.json`, `content/resources/geode-resonance-effects.json`)
no longer exist and a fifth (`content/utilities/radar-unassigned-id.json`, now
`content/utilities/UTL-R1.json`) has been renamed; their notes are reproduced here under their
former paths, and the entries in “Integration-owner rulings applied” below say where each value
went. Note text stays verbatim even where a ruling has since superseded it.

## Design-source contradictions requiring a ruling

Only cases where two design passages actually disagree about a value or a fact. A value stated
in one place and merely absent elsewhere is a gap and lives in the second section. Each entry
states which of two kinds it is, because the second kind is the one that quietly becomes a
wrong number in the data:

- **genuine source disagreement** — the same quantity or fact is given two different values;
- **two passages measuring different things** — the values differ because they are not the
  same quantity, so “picking one” authors a wrong number.

The Sentry Pod 6 s / 12 s case previously suspected here was the second kind and has been
resolved, not ruled on; see “Sentry Pod deployment interval” in the shape notes.

### C-1 — `EN-07` Ripper body scale: 0.496 M derived vs 0.50 M stated

- **Kind:** genuine source disagreement (a derived value and a stated value that do not match).
- **Value A — 0.496 M.** `docs/31-initial-alien-roster.md:45` gives `EN-07` (Razorling) a body-scale
  multiplier of `0.62x` against the `0.80 M` reference diameter the definition records;
  `0.62 x 0.80 = 0.496 M`.
- **Value B — 0.50 M.** `docs/72-player-survivability-and-damage-baseline.md:96` states Razorling's
  contact diameter directly as `0.50 M`, and derives its `0.75 M` contact-start distance from
  `0.50 M`.
- **What the JSON carries:** `contact_footprint.contact_diameter_m = 0.5` (Value B, the authoritative
  doc) *and* `body_scale_multiplier = 0.62` (the multiplier, verbatim). The two are therefore
  internally inconsistent in the same definition by 0.004 M.
- **Affected definitions:** `EN-07` (`content/enemies/EN-07.json`).
- **Ruling needed:** either the multiplier becomes `0.625`, or doc 72's stated diameter becomes
  `0.496 M`, or the multiplier is declared presentation-only and not a footprint input.
- **RULED — third option, and the disagreement dissolves.** See
  “Ruling 1 — enemies store a body scale, not a derived collision diameter” below. There was never
  a second authored value: `docs/72:96`'s `0.50 M` is `0.496 M` typeset to two decimals, exactly as
  every other row of that table is the body scale times `0.80 M` typeset to two decimals. The
  authored quantity is the `0.62×` scale at `docs/31:45`; the diameter is derived and is now the
  compiler's to produce (`docs/technical/40-content-data-and-validation.md:114`). The JSON no
  longer carries a diameter, so nothing is internally inconsistent. The **docs side is being
  corrected** — `docs/72:96` should read `0.496 M`, not `0.50 M`; that correction is not in this
  pass's scope (`docs/` is out of scope here) and is the one action still open on C-1.

- **Transcription note, verbatim** (`content/enemies/EN-07.json`):

  > bodyScaleMultiplier 0.62 x the Ripper 0.80M reference diameter is 0.496M, while docs/72-...md:96 lists 0.50M and derives the 0.75M contact-start distance from 0.50M. The authoritative 0.50M value is transcribed; the 0.62x scale is kept verbatim.

### C-2 — `UTL-A1` display name: “Harmonic Calibrator” vs “Harmonic Amplifier”

- **Kind:** genuine source disagreement (one entity, two names).
- **Value A — “Harmonic Calibrator”.** `docs/68-utility-catalog.md:37` (overview row) and `:52`
  (section heading). Doc 68 is `authoritative: true` for the utility catalog.
- **Value B — “Harmonic Amplifier”.** `docs/71-initial-weapon-numeric-catalog.md:517`, in the
  reference-build progression prose.
- **What the JSON carries:** `utility.UTL-A1.name = "Harmonic Calibrator"` in
  `content/localization/en.json`. The doc-71 wording survives verbatim inside
  `external_numerics[1].statement` and `external_numerics[1].quote` on the same definition, so the
  file now contains both names.
- **Affected definitions:** `UTL-A1` (`content/utilities/UTL-A1.json`).
- **Ruling needed:** this is now load-bearing rather than cosmetic — `name_key` resolves to exactly
  one English string, so one of the two doc spellings has to be corrected at source.

- **Transcription note, verbatim** (`content/utilities/UTL-A1.json`):

  > docs/71-initial-weapon-numeric-catalog.md:517 calls this utility 'Harmonic Amplifier'. docs/68-utility-catalog.md (authoritative: true) names it Harmonic Calibrator; the doc-68 name is used. Logged as a gap.

### C-3 — Resource radar display name: three capitalizations across four passages

- **Kind:** genuine source disagreement (one entity, three renderings of its name).
- **Value A — “resource radar”** (lower case, running prose):
  `docs/50-maps-resources-and-navigation.md:104` and `docs/68-utility-catalog.md:31`.
- **Value B — “Resource radar”** (sentence case, heading):
  `docs/50-maps-resources-and-navigation.md:102` and `docs/glossary.md:220`.
- **Value C — “Resource Radar”** (title case): `docs/71-initial-weapon-numeric-catalog.md:518`.
- **What the JSON carries:** `"Resource radar"` (Value B, the heading form) in
  `content/localization/en.json`. The key was `utility.radar-unassigned-id.name` when this entry was
  written and is now `utility.UTL-R1.name`; the string itself never changed.
- **Affected definitions:** the resource radar, `UTL-R1`
  (`content/utilities/UTL-R1.json`; the file was `radar-unassigned-id.json` with no stable ID when
  this entry was written — see “Ruling 2” and the shape notes).
- **Ruling needed:** same reason as C-2. A single canonical English string is now committed to the
  localization catalog; the docs should be reconciled to it.
- **RULED — Value B, “Resource radar” (sentence case), confirmed.** The authorities are
  `docs/glossary.md:220` and the fresh-profile catalog row at `docs/63:28`, where the neighbouring
  cell (`Universal utility` / the fully title-cased material-utility names in the row below) makes
  the lowercase `r` deliberate rather than a typo. The key is now `utility.UTL-R1.name`; see
  “Ruling 2 — the resource radar is a utility and gets `UTL-R1`” below. Value C
  (`docs/71:518`, title case) and the Value A prose spellings remain the docs-side reconciliation.

- **Transcription note, verbatim** (`content/utilities/radar-unassigned-id.json`):

  > The docs give no single canonical capitalization for the name: 'resource radar' in docs/50 prose and docs/68:31, 'Resource radar' as the docs/50:102 and docs/glossary.md:220 heading, 'Resource Radar' at docs/71-initial-weapon-numeric-catalog.md:518. The heading form is used. Logged as a gap.

### Needs a ruling, but not a two-passage contradiction

#### Minute 33 formation row does not resolve to event times

This is deliberately **not** filed as a contradiction: no second passage states a different
value for minute 33. One cell is unparseable, which is a gap, not a disagreement. It is kept here
rather than in the bulk notes because it leaves a `null` in shipped data and needs a ruling.

- **The cell:** minute 33 of `docs/32-standard-wave-and-beacon-schedule.md` reads
  “Streams rotate through four sectors at 33:15 intervals”. Every other formation row in the same
  table gives absolute `m:ss` event times.
- **What the JSON carries:** `minute_rows[33].formation_event.at = null`.
- **Affected definitions:** `WAV-01` (`content/encounters/standard-encounter-schedule.json`).
- **Ruling needed:** the absolute event times for minute 33, or an accepted interval grammar the
  schedule schema can validate.
- **RULED — four provisional times, flagged in the data as reconstructed.** See
  “Ruling 5 — minute 33 of the wave schedule” below. `at` is now
  `["33:00", "33:15", "33:30", "33:45"]` with `timestamps_reconstructed: true` on the same row.

- **Transcription note, verbatim** (`content/encounters/standard-encounter-schedule.json`):

  > minute 33 formationEvent has at=null: the cell reads 'Streams rotate through four sectors at 33:15 intervals', which does not resolve to explicit event times.

## Transcription and shape notes

Absent values, `null` fields, missing stable IDs, naming choices, doc omissions, and the
structural decisions taken in this pass. Nothing here is a disagreement between two sources.

### Structural changes and corrections made in this pass

#### Sentry Pod (`W-BE`) deployment interval — corrected to 6.0 s, derived 12 s removed

Not a source defect. `docs/71-initial-weapon-numeric-catalog.md:83` (the fixed-properties
cell of the Rank-Zero table) settles it: “One pod every 6 s, 24 s life, maximum three active;
oldest replaced at cap.” The authored deployment interval is **6.0 s**. The 12 s figure in the
base-behavior prose at `:305` is the arithmetic consequence for the *third* pod
(immediate, +6 s, +12 s) — a different quantity, not a competing interval. The two passages were
measuring different things.

Change made: `fixed_properties.ramp_to_maximum_pods_seconds: 12` was **deleted** from
`content/weapons/W-BE.json`. It was a derived value and should not have been authored.
`fixed_properties.deployment_cadence_seconds` remains `6.0` and
`fixed_properties.first_pod_deploys_immediately` remains `true` — the first-pod-immediate
behaviour is unchanged. `damage_model.primary_limitation` (“Six-second deployment ramp and
stationary coverage”, verbatim from `:59`) is unchanged.

Residue to be aware of: `base_behavior.text` still contains the phrase “maintains three after
its 12-second ramp”, because that field holds design-doc prose verbatim and verbatim prose was
not edited in this pass. If `:305` is reworded at source, that string should be re-transcribed.
The two notes below were written under the earlier reading that this was a live conflict; they
are reproduced verbatim rather than rewritten, and this entry supersedes them.

  > Internal conflict in docs/71-initial-weapon-numeric-catalog.md: the `Primary limitation` cell at :59 says "Six-second deployment ramp" while the base-behavior prose at :305 says the weapon "maintains three after its 12-second ramp" (first pod immediate, then one every 6.0 s). `rampToMaximumPodsSeconds` uses the 12 s figure from the prose, which is consistent with the stated cadence; the limitation string is transcribed verbatim.

  > `firstPodDeploysImmediately` and `rampToMaximumPodsSeconds` come from docs/71-initial-weapon-numeric-catalog.md:305; the Rank-Zero table at docs/71-initial-weapon-numeric-catalog.md:83 omits both.

#### `content/maps/world-props.json` folded into `MGC-01`, then deleted

The file held two prop definitions (destructible rock, health pack). Both are transcribed
values, not inferred ones: rock hull/armour/footprint from
`docs/72-player-survivability-and-damage-baseline.md:192` and health-pack repair and pickup
radius from `docs/51-standard-map-generation-contract.md:144`. Both now live inside
`content/maps/standard-map-generation-contract.json` (`MGC-01`). Every value and every
`source_refs` citation was carried over unchanged; the world-props `source_refs` entry is now
present twice, re-prefixed with the two new field paths.

Two placement decisions, recorded separately because they have different standing:

1. **Rock rules inside `MGC-01`: stated by the spec.**
   `docs/technical/40-content-data-and-validation.md:148` lists “rock rules” explicitly among
   the map-generation definition's fields. The rock's own property table therefore went into
   the existing `destructible_rock_rules` field, as `destructible_rock_rules.destructible_rock`,
   beside the spawn and replenishment rules already there.
2. **Health packs inside rock rules: a structural decision by the integration owner, not
   stated by `40:148`.** `40:148` says nothing about health packs. They were nested as
   `destructible_rock_rules.health_pack` on the reasoning that a pack is an outcome of rock
   destruction rather than an independent pickup class — the pack chance is a rock outcome and
   the 25 Hull / 0.25 M values exist only as its payload. If the schema stream later promotes
   packs to their own definition, that is a change of decision, not the correction of a
   transcription slip.

Also in this fold:

- `destructible_rock_rules.props_defined_in` (`"content/maps/world-props.json"`) was deleted.
  It was a pointer to the now-deleted file and its referent is the sibling field.
- The duplicated pair `valid_spawn_distance_from_mech_m` / `extra_visible_screen_margin_m`
  (rock property table) and `valid_position_distance_from_mech_m` /
  `min_meters_beyond_visible_camera_rectangle` (spawn rules) were **both kept**. They state the
  same 18–45 M and 2 M values from two different tables; de-duplicating them is a domain-shape
  decision for the map schema, not a transcription one.
- No landmark content was authored. `landmark_pools` stays `null` because the landmark pools are
  an open question (OQ-008, `docs/open-questions.md:42`; landmark content is named in its
  candidate-answers entry at `:47`). Rocks and health packs are not
  landmarks, so folding them in does not touch OQ-008.
- **Field naming needs confirming against `40:148` when the map schema lands.**
  `40:148` names the field group “rock rules”; the sub-field names `destructible_rock` and
  `health_pack`, and the nesting depth, were chosen here and are not stated anywhere.
- Both nested props keep `id: null` and a literal `prop` display name
  (“Destructible rock”, “Health pack”). Neither is an independently addressable definition any
  more, so neither received a localization key; if either becomes one, its `prop` string is the
  name to move into `en.json`.

Verbatim notes from the deleted file (`content/maps/world-props.json`):

1. Two entries in one aggregate: docs/technical/40-content-data-and-validation.md:148 lists "rock rules" under map generation, and the health pack exists only as the destructible rock's drop, so the pair is the smallest cohesive aggregate (docs/technical/40-content-data-and-validation.md:63).
2. Secondary source for both entries: docs/51-standard-map-generation-contract.md:144-156 ("## Destructible rocks"), which states the same 100 Hull, 0 Armor, 0.80M footprint, 20% health-pack chance, 0.25M pickup radius, and 25-Hull repair.
3. id is null for both entries: neither the rock nor the health pack receives an ID in any document, and docs/technical/40-content-data-and-validation.md:67 lists no ID scheme for world props. A stable-ID decision is required.
4. Rock field order follows the "Property | Initial value" table at docs/72-player-survivability-and-damage-baseline.md:192-201.
5. Spawn/replenishment behavior (16-rock cap, one attempt per second, 10% success) is transcribed in content/maps/standard-map-generation-contract.json under destructibleRockRules; the distance and camera-margin values are repeated here because they appear in the rock's own property table.
6. presentation is null: docs/51-standard-map-generation-contract.md:156 states "Audiovisual treatment remains production work".

#### `content/mechs/shared-baseline.json` — handoff to the player-baseline stream (`PLY-001`)

**This is the handoff entry. The file has been deleted; everything needed to re-home its
values without re-transcribing them is reproduced below.** The values are not mech data.
`docs/technical/40-content-data-and-validation.md:110` says a mech definition carries Hull,
Armor, Recovery, movement and footprint **overrides**, so the thing being overridden is not a
mech definition, and `content/` has no player or run category for it yet. `PLY-001` is the
intended consumer and the schema stream will mint the stable ID. The mech catalog is now six
files (`MCH-01`..`MCH-06`), with no baseline aggregate.

Complete former contents, verbatim (the `notes` array is reproduced separately below):

```json
{
  "schema_version": 1,
  "content_version": 1,
  "status": "enabled",
  "tags": [],
  "source_refs": [
    "GDD-INITIAL-MECH-CATALOG#shared-comparison-baseline",
    "survivability_baseline_extensions: GDD-PLAYER-SURVIVABILITY-BASELINE#shared-player-baseline"
  ],
  "maximum_hull_integrity": 100,
  "armor": 0,
  "recovery_hull_per_second": 0,
  "movement_speed_m_per_s": 3.0,
  "movement_speed_percent": 100,
  "collision_diameter_m": 1.0,
  "collision_shape": "circle",
  "mining_extraction_rate_percent": 100,
  "weapon_damage_percent": 100,
  "weapon_attack_rate_percent": 100,
  "weapon_area_percent": 100,
  "survivability_baseline_extensions": {
    "starting_hull_integrity": {
      "text": "Current maximum",
      "equals_current_maximum_hull_integrity": true
    },
    "passive_recovery_hull_per_second": 0,
    "revival_charges": 0,
    "same_enemy_contact_repeat_interval_seconds": 0.75,
    "global_contact_grace_after_resolved_contact_seconds": 0.2,
    "universal_post_hit_invulnerability": {
      "text": "None",
      "duration_seconds": null
    },
    "health_pack_repair_hull": 25
  }
}
```

Source citation for every value:

| Field | Value | Citation |
| --- | --- | --- |
| `maximum_hull_integrity` | 100 | `docs/36-initial-mech-catalog.md:31` (Shared comparison baseline, `:27-39`); `docs/72-player-survivability-and-damage-baseline.md:34` (Shared Player Baseline, `:30-45`, accepted by `DEC-126`) |
| `armor` | 0 | `docs/36:32`; `docs/72:36` |
| `recovery_hull_per_second` | 0 | `docs/36:33` “Recovery | 0 Hull/s”; `docs/72:37` “Passive Recovery | 0 Hull/s” |
| `movement_speed_m_per_s` | 3.0 | `docs/36:34` “Movement speed | 3.0M/s (100%)”; `docs/72:39` “Base movement speed | 3.0M/s” |
| `movement_speed_percent` | 100 | `docs/36:34` (the “(100%)” annotation only; doc 72 omits it) |
| `collision_diameter_m` | 1.0 | `docs/36:35` “Collision diameter | 1.0M circle”; `docs/72:40` |
| `collision_shape` | "circle" | `docs/36:35` (combined cell); `docs/72:41` (split into its own row) |
| `mining_extraction_rate_percent` | 100 | `docs/36:36` — doc 36 only |
| `weapon_damage_percent` | 100 | `docs/36:37` — doc 36 only |
| `weapon_attack_rate_percent` | 100 | `docs/36:38` — doc 36 only |
| `weapon_area_percent` | 100 | `docs/36:39` — doc 36 only |
| `survivability_baseline_extensions.starting_hull_integrity` | "Current maximum" | `docs/72:35` — doc 72 only |
| `survivability_baseline_extensions.passive_recovery_hull_per_second` | 0 | `docs/72:37` — the doc-72 row name for the same 0 Hull/s recorded above |
| `survivability_baseline_extensions.revival_charges` | 0 | `docs/72:38` — doc 72 only |
| `survivability_baseline_extensions.same_enemy_contact_repeat_interval_seconds` | 0.75 | `docs/72:42` — doc 72 only |
| `survivability_baseline_extensions.global_contact_grace_after_resolved_contact_seconds` | 0.2 | `docs/72:43` — doc 72 only |
| `survivability_baseline_extensions.universal_post_hit_invulnerability` | "None" / `null` | `docs/72:44` — doc 72 only; qualitative “None” with no duration |
| `survivability_baseline_extensions.health_pack_repair_hull` | 25 | `docs/72:45` — doc 72 only; the same 25 Hull now also carried by `MGC-01` |
| `source_refs[0]` | — | `GDD-INITIAL-MECH-CATALOG#shared-comparison-baseline` |
| `source_refs[1]` | — | `survivability_baseline_extensions: GDD-PLAYER-SURVIVABILITY-BASELINE#shared-player-baseline` |

Two things the receiving stream should know that this file did **not** carry:

- **Facing-starts-east is not in this file.** It is a movement rule at `docs/30:70` (`DEC-042`)
  and was never transcribed into `content/`. Nothing was lost by the deletion; it still needs
  authoring wherever the player/run definition lands.
- The unit `M` is one unmodified mech collision diameter (`docs/72:47`), not a metre. Every
  `_m` and `_m_per_s` suffix above uses that unit.

The 14 verbatim notes from the deleted file (`content/mechs/shared-baseline.json`), which
include the five documented doc-36 / doc-72 divergences — all of them naming, row-splitting or
coverage differences with zero conflicting values, which is why none is filed as a
contradiction:

1. No stable ID exists for the shared baseline in any doc, so this is a cohesive aggregate in a kebab-case file with no `id` field.
2. docs/36 is treated as PRIMARY for this file because it is the mech catalog and its 9-row table is the pre-PowerUp baseline the selection interface compares each mech against (line 27).
3. docs/72-player-survivability-and-damage-baseline.md 'Shared Player Baseline' (lines 30-46) is a 12-row table over the same subject. It contains ZERO conflicting values — every property present in both agrees numerically. The divergence is entirely one of row naming, row splitting, and coverage. All doc-72-only rows are preserved under `survivabilityBaselineExtensions` with their own `_source`, so no accepted value is dropped.
4. DIVERGENCE 1 (naming only): docs/36:33 'Recovery | 0 Hull/s' vs docs/72:37 'Passive Recovery | 0 Hull/s'. Same value 0; recorded once as recoveryHullPerSecond with the doc-72 label captured in the extensions block.
5. DIVERGENCE 2 (annotation only): docs/36:34 'Movement speed | 3.0M/s (100%)' vs docs/72:39 'Base movement speed | 3.0M/s'. Same value; docs/36's '(100%)' reference percentage is kept as movementSpeedPercent.
6. DIVERGENCE 3 (row splitting): docs/36:35 'Collision diameter | 1.0M circle' is one cell combining size and shape; docs/72:40-41 splits it into 'Mech collision diameter | 1.0M' and 'Mech collision shape | Circle'. Same values; recorded here as collisionDiameterM plus collisionShape following the docs/72 split.
7. DIVERGENCE 4 (docs/36-only rows, absent from docs/72): 'Mining extraction rate | 100%', 'Weapon damage | 100%', 'Weapon attack rate | 100%', 'Weapon area | 100%' (docs/36:36-39). These are the reference 100% denominators the mech traits modify and exist only in docs/36.
8. DIVERGENCE 5 (docs/72-only rows, absent from docs/36): 'Starting Hull Integrity | Current maximum', 'Revival charges | 0', 'Same-enemy contact repeat interval | 0.75 s', 'Global contact grace after a resolved contact | 0.20 s', 'Universal post-hit invulnerability | None', 'Health-pack repair | 25 Hull' (docs/72:35, 38, 42-45).
9. 'Universal post-hit invulnerability' has no numeric value in either doc: docs/72:44 states 'None'. Recorded as null with the verbatim text preserved; flagged in the gaps file.
10. Unit `M`: docs/72:47 defines M as one unmodified mech collision diameter, so one base-travel second equals 3.0M of shortest-path travel. Map-generation distance bands always use this unmodified speed even when the current mech has movement bonuses.
11. Modifier application order comes from docs/36:41: account PowerUps modify the account-wide starting baseline first, then the selected mech's inherent modifier applies, and the selection screen shows the resulting values. Percentage modifiers with the same named utility or PowerUp statistic add under the shared modifier rules.
12. passiveRecoveryHullPerSecond is the docs/72 label for the same 0 Hull/s value recorded above as recoveryHullPerSecond; it is repeated here only to preserve the docs/72 row name, not as a second statistic.
13. universalPostHitInvulnerability.durationSeconds is null because docs/72:44 gives the qualitative value 'None' and no duration.
14. docs/72:10 self-describes as the 'authoritative first-playable baseline' and docs/72:16 states that where the CSV mirrors disagree, docs/72 wins over docs/data/survivability-baseline.csv.

#### Stable IDs

- `content/encounters/standard-encounter-schedule.json` — `id` assigned this pass: **`WAV-01`**
  (ties to `GDD-STANDARD-WAVE-SCHEDULE`). Supersedes the withdrawn `SCHED-01`; that token was
  never written to a file.
- `content/maps/standard-map-generation-contract.json` — `id` assigned this pass: **`MGC-01`**
  (ties to `GDD-MAP-GENERATION`). Supersedes the withdrawn `MAPRULE-01`; likewise never written.
- `content/utilities/UTL-R1.json` — `id` assigned by the integration owner's ruling, **not** by
  this transcription pass: **`UTL-R1`**, with the file renamed from `radar-unassigned-id.json` and
  the localization keys rewritten from `utility.radar-unassigned-id.*` to `utility.UTL-R1.*`. See
  “Ruling 2” above. This is the entry the “BOUNDARY DECISION REQUIRED” note below asked for.
- Still `"id": null`, and still needing a decision — no document assigns these an ID, and
  `40:67` forbids inventing one. Their localization keys therefore use the **filename stem** as
  the `<stable_id>` segment, which is **provisional** and must be rewritten when IDs are minted:
  - `content/mining-sites/standard-ore-seams.json` → `mining_site.standard-ore-seams.name`
  - `content/mining-sites/rich-ore-seams.json` → `mining_site.rich-ore-seams.name`
  - `content/mining-sites/hyper-gold-sites.json` → `mining_site.hyper-gold-sites.name`
  - `content/mining-sites/specialized-material-geodes.json` →
    `mining_site.specialized-material-geodes.name`
- No `id` field **at all**, because the file is not a definition and therefore has nothing to
  identify: `content/enemies/shared-elite-modifiers.json` (Ruling 3). This is a different case from
  `"id": null` — null means “a definition whose ID has not been minted yet”, absent means “not a
  definition”, the same treatment the deleted `content/mechs/shared-baseline.json` had. It carries
  no localized string and no stem-based key.
- Gone entirely, so no ID was ever needed: `content/enemies/elite-modifier-profile.json`
  (Ruling 3) and `content/resources/geode-resonance-effects.json` (Ruling 4).

#### Localization keys: what was minted and what was not

Key grammar is `<category>.<stable_id>.<role>`, category `snake_case`, stable ID verbatim
(`40:67`), role from `name` / `summary`. English lives only in
`content/localization/en.json` (`40:209-217`); no `name_key` holds literal text (`40:82`).

`name_key` was **not** minted for four definitions, because no document authors a player-facing
display name for them and inventing one would be authoring, not transcription:

- `WAV-01` (`content/encounters/standard-encounter-schedule.json`). The reserved key is
  `encounter.WAV-01.name`; `mode: "standard"` is an enum token, not a display name.
- `MGC-01` (`content/maps/standard-map-generation-contract.json`). Reserved key
  `map.MGC-01.name`; same reason.
- `content/enemies/elite-modifier-profile.json` — a shared modifier profile, no name field.
- `content/resources/geode-resonance-effects.json` — an aggregate rules file, no name field.

Both of those last two files are now deleted (Rulings 3 and 4), and because neither ever held a
localization key, neither deletion removed anything from `en.json`. Their successor
`content/enemies/shared-elite-modifiers.json` likewise has no `name_key` and no reserved key: it is
a constants block, not a definition, so there is no name to reserve. The one localization change in
this pass was the radar's two keys moving from `utility.radar-unassigned-id.*` to
`utility.UTL-R1.*`, values unchanged (Ruling 2); `en.json` still holds 164 strings.

`summary_key` was minted only where a definition already carried short player-facing prose:
mechs (`selection_summary`), relics (`discovery_sentence`, whose object held nothing but that
string once its line number was removed, so the key was deleted outright), and utilities
(`description`). It was **not** invented anywhere (`40:76`, “where relevant”).

Player-facing-looking strings deliberately left as data, because they are denormalized copies,
internal descriptors, or verbatim mechanical rule text, and because none of them has an
envelope key slot to point at:

- `weapon_name` on every branch, `signature` on every mech, and `branches.*.name` on every
  weapon — copies of a name whose canonical home is another definition's `name_key`.
- `inherent_trait.name` (mechs), `resonance_effect_name` (resources), `prop` (the two folded
  map props) — nested display names with no `*_key` slot.
- `identity`-adjacent art direction: `description` on enemies and bosses (sourced from
  “Six silhouette families” and the per-boss silhouette sentence), `material_character`,
  `primary_color`, `icon_and_silhouette_cue`, `audio_character` (resources).
- `player_facing_identity` and `top_down_silhouette` (mechs) — longer prose with no envelope
  slot; `selection_summary` is the concise summary the envelope asks for.
- Catalog-overview shorthand: `primary_transformation`, `core_tradeoff` (relics), `effect`
  (unlocks), `per_rank_effect.text` (PowerUps), `primary_role`, `installed_to_rank_3`
  (utilities), `base_automatic_attack.text`, `base_behavior.text` (weapons).
- Every `rules[].text`, `raw`, `quote`, and `rule` string — verbatim design-doc prose.

#### Second provenance channel removed

`refs` arrays holding `docs/<file>.md:<line>` strings (30 `availability.refs` + 30
`exclusivity.refs`, all in `content/branches/`), `lines` fields sitting beside `text`, and
`rules[].line` were all deleted. Line numbers are explicitly unstable (`40:70`, `TDR-006:25`).
Each was resolved to the enclosing heading of the cited document and merged into that file's
top-level `source_refs` in the existing `<snake_case json path>: <DOC-ID>#<anchor>` form, then
deduplicated against the entries already there. `text` values were kept: they are verbatim
design-doc prose and are data.

**Correction.** This paragraph previously read that `shared_rule_refs` on 15 branch files “is still
present and not touched, flagged for a decision”. **That is no longer true, and was already false
when written.** The field (spelled `sharedRuleRefs` at the time) held 2 raw
`docs/65-weapon-stat-and-branch-upgrades.md:<line>` strings on each of the 15 files — 30 in total,
all of them `:66` or `:68` — and it was removed in the same pass as the rest of this section,
commit `5becb39`. Both cited lines fall under `## Weapon branches` (`docs/65:54`), so both resolved
to `GDD-WEAPON-STAT-AND-BRANCH-UPGRADES#weapon-branches`, which the `availability:` and
`exclusivity:` prefixed entries already carried; the folded refs deduplicated against those and the
field was deleted. `shared_rule_refs` / `sharedRuleRefs` now appears nowhere under `content/**/*.json`.
No second provenance channel remains open.

### Integration-owner rulings applied

Five open items were ruled on by the integration owner and applied in this pass. Each entry
records the ruling, its citations, exactly what changed in `content/`, and — separately — the
choices the ruling did **not** determine, which are mine and are open to revision. No value was
removed except the ones named as removed below.

A **second integration-owner pass** then ruled on four of the choices Rulings 1, 3 and 4 had left
to me. Those are Rulings 6–9, in their own section after Ruling 5. Where a second-pass ruling
reverses a first-pass choice, the first-pass entry is left as written and carries a
**superseded-by** pointer — the record of what was decided when is part of the point.

#### Ruling 1 — enemies store a body scale, not a derived collision diameter

**Citations.** `docs/31:45` authors `EN-07` (Razorling) a `0.62×` body scale.
`docs/72:86` — “The Ripper's rank-zero contact diameter is 0.80M. Every ordinary body scale in the
alien roster multiplies that diameter.” So the diameter is a product, not an authored number.
`docs/technical/40-content-data-and-validation.md:114` assigns producing it to the compiler:
“Validation derives world speeds/footprints and compares them with the survivability report.”
Storing it alongside the scale would put a second writer on a compiler-owned value.

**Change.** In all ten ordinary enemy definitions (`content/enemies/EN-01.json` … `EN-10.json`):

- `body_scale_multiplier` is renamed **`body_scale_factor`** — a scale, so no unit suffix
  (`40:92-94` only requires suffixes on ambiguous *dimensional* names). The value is unchanged.
- `contact_footprint.contact_diameter_m` is **removed**. It was the derived collision diameter.

The ten scales were re-verified against the “Ordinary roster overview” table at
`docs/31:37-48` (`Body` column), not taken from the ruling's restatement, and the removed
diameters were re-verified against the “Collision and Contact Footprints” table at
`docs/72:88-99` before deletion:

| ID | Identity | `body_scale_factor` (`docs/31`) | Removed `contact_diameter_m` (`docs/72`) | scale × `0.80 M` |
| --- | --- | ---: | ---: | ---: |
| `EN-01` | Skitterling | 0.55 | 0.44 M | 0.440 |
| `EN-02` | Ripper | 1.00 | 0.80 M | 0.800 |
| `EN-03` | Shellback | 1.30 | 1.04 M | 1.040 |
| `EN-04` | Lurker | 1.05 | 0.84 M | 0.840 |
| `EN-05` | Gloomwing | 1.20 | 0.96 M | 0.960 |
| `EN-06` | Needler | 1.00 | 0.80 M | 0.800 |
| `EN-07` | Razorling | 0.62 | 0.50 M | **0.496** |
| `EN-08` | Iron Ripper | 1.10 | 0.88 M | 0.880 |
| `EN-09` | Siegeback | 1.65 | 1.32 M | 1.320 |
| `EN-10` | Dreadwing | 1.35 | 1.08 M | 1.080 |

Every row reproduces exactly, so nine of the ten removals lost nothing that the compiler cannot
reproduce from the surviving scale and the `0.80 M` reference.

**`docs/72:96`'s `0.50M` for Razorling is not a second authored value.** It is `0.496`
typeset to the two decimals the whole column uses; the same rounding is invisible on the other
nine rows only because their products happen to be exact at two decimals. Reading it as an
authored diameter is what created contradiction C-1. The **docs side is being corrected** to
`0.496 M`; that edit is outside this pass (`docs/` is out of scope for this worker).

**What the ruling did not determine, and I chose:**

1. **Which field is “the collision-diameter field”.** I removed only
   `contact_footprint.contact_diameter_m`. I **kept**
   `contact_footprint.center_distance_that_begins_contact_m`, `shape`, and
   `reference_diameter_m` — even though the centre distance is *also* derivable
   (`contact_diameter ÷ 2 + the mech's 0.50 M collision radius`, `docs/72:86`, which reproduces
   all ten values). The ruling named one field, singular, and “preserve every value you are not
   explicitly told to remove” governs the rest. If the compiler is to derive the centre distance
   too, that is a second removal and a second decision.
   **→ Superseded by Ruling 6.** That second decision was made: the centre distance is removed too.
   Also note the field rename in this entry (`body_scale_multiplier` → `body_scale_factor`) is
   **reverted by Ruling 7**, which puts the name back to `body_scale_multiplier`.
2. **No new `source_refs` entry for `body_scale_factor`.** The field is a rename of an existing
   field that carried no prefixed ref, its source did not change, and index 0 of every enemy's
   `source_refs` is already `GDD-INITIAL-ALIEN-ROSTER#ordinary-roster-overview` — the roster table
   the `Body` column lives in. The `contact_footprint:` ref to
   `GDD-PLAYER-SURVIVABILITY-BASELINE#collision-and-contact-footprints` is retained because the
   surviving footprint fields still come from there.

#### Ruling 2 — the resource radar is a utility and gets `UTL-R1`

**Citations.** `40:128` gives the Utilities schema a field for the “assigned material or ore-only
radar exception”, so the schema expects the radar *inside* the utility catalog with a marked
exception rather than outside it. `docs/68:31` — the radar “remains outside the material table: it
costs 300 common ore, has no ranks” — places it outside the *material table*, which is not the
catalog. `R` collides with none of the `A`–`F` material letters used by `UTL-A1`…`UTL-F2`.

**Change.**

- `"id"` is `"UTL-R1"` (was `null`).
- The file is renamed `content/utilities/radar-unassigned-id.json` →
  **`content/utilities/UTL-R1.json`**.
- `name_key` is `utility.UTL-R1.name` and `summary_key` is `utility.UTL-R1.summary` (were
  `utility.radar-unassigned-id.*`). Both keys were renamed in place in
  `content/localization/en.json`, which stays flat, lexically sorted, duplicate-free and
  orphan-free at 164 strings; the two English values are unchanged.
- Display name confirmed as **“Resource radar”**, sentence case — `docs/glossary.md:220` and the
  catalog row at `docs/63:28`, where the neighbouring `Universal utility` cell and the fully
  title-cased material-utility names in the row below make the lowercase `r` deliberate. This also
  closes C-3 above.
- The “no ID assigned” flag is gone: `"id": null` **was** the flag, and the per-definition note
  that opened “BOUNDARY DECISION REQUIRED — THIS FILE HAS NO ID” is superseded by this entry. That
  note is still reproduced verbatim below, under the file's former path, because note text is
  never edited.
- Four `source_refs` entries were added for the two things the file now asserts:
  `id: TDD-CONTENT-DATA#utilities`, `id: GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules`,
  `name_key: GDD-GLOSSARY#resource-radar`,
  `name_key: GDD-PERMANENT-OPTION-UNLOCK-CATALOG#fresh-profile-baseline`. Every pre-existing entry
  is retained.

Every other field of the definition — including `material: null`, the 300-ore cost, `ranks: null`,
the seven tracked categories, `effect_rules`, and all seven `external_numerics` — is byte-identical.

**What the ruling did not determine, and I chose:** the four added `source_refs` entries and their
`json.path:` prefixes. The ruling named `docs/glossary.md:220` and `docs/63:28` as name authorities
and `40:128` / `docs/68:31` as ID authorities but did not say to cite them in the file.

#### Ruling 3 — `elite-modifier-profile.json` is not a definition; decomposed and deleted

**Citations.** `40:114` puts “elite eligibility” in the *enemy* schema's field list.
`docs/technical/23:137` — an elite “snapshots the base enemy definition plus the shared elite
modifiers at spawn. It does not create a second duplicated enemy catalog row.” So there is no
elite definition to own an ID: there is a per-enemy flag and a block of shared constants.

**Change — eligibility onto the enemies.** `elite_eligible` (boolean) is on all ten enemy
definitions, `false` on `EN-06` and `true` on the other nine, and each now carries a
`elite_eligible: GDD-INITIAL-ALIEN-ROSTER#elite-treatment` ref (`EN-06` additionally carries
`elite_eligible: TDD-ENCOUNTERS#elite-construction`). The `EN-06` exclusion was confirmed at
source before writing it, in two places. `docs/31:102` states it twice in one sentence — an elite is “a visibly
enhanced instance of one of the nine pure pursuers” (nine of ten) and “Needler does not become an
elite in the initial standard schedule because combining its projectile with the shared elite
multipliers reduces readability”. And — decisively, because it is what makes this a validated field
rather than prose — `docs/technical/23:141`, “Needler is excluded by content validation.” The former `eligible_enemy_ids` / `excluded_enemy_ids` arrays are gone; their
content is exactly the ten booleans.

**Change — the five shared multipliers become a constants block.**
`content/enemies/shared-elite-modifiers.json`, with **no `id`** and **no `name_key`** because it is
not a definition and has no player-facing name. Envelope (`schema_version`, `content_version`,
`status`, `tags`, `source_refs`) first, as everywhere else. The five values, verified against
`docs/31:104-110` and `docs/technical/23:139-140`:

| Property | Value | Note |
| --- | ---: | --- |
| `maximum_hull_multiplier` | 4 | Hull ×4 |
| `movement_speed_multiplier` | 1.1 | |
| `contact_damage_multiplier` | 1.5 | |
| `body_scale_multiplier` | 1.25 | stays a **scale**, per the ruling and Ruling 1 |
| `added_control_resistance_percent` | 25 | **percentage points**, so the name ends `_percent` (`40:95`); the normalized factor is the compiler's |

Also renamed for the same `40:95` reason: `added_control_resistance.cap_percent` (90) is now the
top-level `control_resistance_cap_percent`. `added_control_resistance.percentage_points` was the
one property in the file that failed the `*_percent` policy.

`contact_diameter_multiplier` (`{"value": 1.25}`) was **removed** as the derived twin of
`body_scale_multiplier` — the same `1.25`, by the same argument as Ruling 1, so no value was lost.
Its `source_refs` entry was re-prefixed onto `body_scale_multiplier:` rather than dropped (the same
treatment the `world-props` fold used). Everything else from the old file is carried over
byte-identically: `adds_behavior`, `adds_attacks_phases_aura_or_support_ai`, `adds_loot`,
`retains_base_identity_behavior`, `exclusion_reason`, `max_scheduled_elites_at_once`,
`beacon_elites_additional`, `recycling`, `post_hard_control_immunity_seconds`,
`modifier_application_order`, `presentation_requirements`, `worked_examples`.
`content/enemies/elite-modifier-profile.json` is deleted.

> **FLAG — the placement of this constants block is my choice, not the spec's.**
> The ruling says the five multipliers are “a shared constants block, not a definition” and does
> **not** name a home for it. `content/` has no constants category: `40:34-63` lays out
> per-catalog definition directories and nothing else. I put it in `content/enemies/` because that
> is where its only consumer's definitions live and because it sits beside the `elite_eligible`
> flags it pairs with. Nothing in `docs/` states that. **It may move when the schemas land** — to a
> `content/constants/` or `content/rules/` directory, into the encounter-director contract, or into
> the schema stream's own shared-values file. Treat the path
> `content/enemies/shared-elite-modifiers.json` as provisional. The same caveat applies to the
> file's *stem*, which no document supplies either.

**What else the ruling did not determine, and I chose:**

1. **`exclusion_reason` stays in the constants block** (verbatim value, verbatim key). With the two
   ID arrays gone it has no `excluded_enemy_ids` sibling, but its value names Needler explicitly so
   it is self-describing, and enemy definitions have no prose slot for it. Deleting it would have
   lost an authored sentence.
   **→ Superseded by Ruling 8.** The field is dropped; the sentence was never lost, because it is
   authored prose in `docs/`, and the machine-readable form of the exclusion is `EN-06`'s
   `elite_eligible: false`.
2. **The asymmetric names `body_scale_factor` (enemy) vs `body_scale_multiplier` (elite).** The
   enemy stores its own scale; the elite block multiplies whatever the enemy stores. I read that as
   two different quantities that should not share a name, but the ruling only fixed the enemy one.
   **→ Superseded by Ruling 7.** They are the same kind of quantity and now share the name.
3. **`post_hard_control_immunity_seconds` keeps its `{"value": 0.75}` wrapper**, copied verbatim
   rather than flattened. Flattening is a shape decision for the schema stream.
4. **The two added `source_refs` entries** (`TDD-ENCOUNTERS#elite-construction`, and the
   `control_resistance_cap_percent:` prefix) and the re-prefix onto `body_scale_multiplier:`.

#### Ruling 4 — `geode-resonance-effects.json` is not a definition; decomposed and deleted

**Citations.** Doc 40 splits this deliberately. `40:106` gives the *resource* definition a
“resonance behavior registration if applicable”, and the authored table is keyed by Material
(`docs/61:90`, “Geode resonance behavior”), so the shape already matches. `40:140` gives the
*mining-site* definition the “zone/field dimensions”. Runtime agrees: `docs/technical/24:23` has
each site store “resonance material and field radius if a geode” — the site references the
material, the material owns the behavior.

**Change — the six effects onto the six resources.** `content/resources/A.json` … `F.json` each
gain a `resonance_behavior` object beside the `resonance_effect_name` they already had, carrying
that material's row byte-identically: `effect_name`, `resonance_effect`, `modifier`
(`{percent, direction}`), `short_modifier`, `edge_case_rule`. Each also gains
`resonance_behavior: GDD-MINING#geode-resonance-fields` and
`resonance_behavior.short_modifier: GDD-SPECIALIZED-RESOURCE-IDENTITIES#geode-resonance-behavior`,
matching the split the old file's own notes 2 and 3 describe (docs/40 authoritative, docs/61 for
the abbreviated `short_modifier` wording).

Two fields of each old row were **not** copied, because they were the row's own keys and are
already the receiving definition's identity: `material_id` (`"A"`) is the resource's `id`, and
`geode` (`"Asterite"`) is the resource's display name — `en.json`'s `resource.A.name` is exactly
`"Asterite"` for all six. Nothing is lost; asserted mechanically.

**Change — the field rules onto the geode class.** `content/mining-sites/specialized-material-geodes.json`
already carried a `resonance_field` block. The `field_rules` values the old file held are now all
present there, seven of them already matching under the site's own names:

| old `field_rules.*` | site `resonance_field.*` | value |
| --- | --- | ---: |
| `radius_m` | `radius_m` | `null` |
| `larger_than_extraction_zone` | *same* | `true` |
| `active_during_interruptions` | *same* | `true` |
| `collapses_when_geode_opens` | `collapses_on_open` | `true` |
| `modifier_retained_after_leaving_field` | `retained_after_leaving_field` | `false` |
| `applies_to` | *same* | 3 entries |
| `summons_enemies` | *same* | `false` |
| `uses_progress_thresholds` | *same* | `false` |
| `active_before_extraction_begins` | **added** | `true` |
| `fields_overlap_on_standard_maps` | **added** | `false` |
| `modifier_named_in_geode_label_or_contextual_hud` | **added** | `true` |

`modifier_magnitude_percent: 20` was already on the site as
`resonance_field.modifier_magnitude.percent: 20` and on each per-material `modifier.percent`.
Two `source_refs` entries were added to the site for the relocated rules.
`content/resources/geode-resonance-effects.json` is deleted. It had **no** localization keys —
none were ever minted for it (see “Localization keys” below) — so nothing was removed from
`en.json`.

**The field radius was never given a number** and stays `null`. `docs/40:100` says only that the
field is “larger than its extraction zone”; no doc gives a dimension. The existing gap flag is
kept — note 4 of `specialized-material-geodes` below, “resonanceField.radiusMeters is null:
docs/40-mining-and-extraction.md:100 states only that the field is ‘larger than its extraction
zone’; no dimension is given.”

> **FLAG — the six effects need `behavior_kind` registry tokens and do not have them.**
> `40:156` requires that “every content `behavior_kind` … has exactly one registered descriptor
> with a compatible parameter schema”, and no registry exists in this tree yet. So each
> `resonance_behavior` carries `"behavior_kind": null` with
> `"behavior_kind_registration_pending": true`, and **no token format was invented** — anything
> written now would look official and would be guessed. The six effect names needing tokens are:
> **Focused Assault** (`A`, Asterite), **Dense Plating** (`B`, Barysteel),
> **Charged Payloads** (`C`, Cinderglass), **Vector Lock** (`D`, Driftmetal),
> **Synchronized Aggression** (`E`, Eidolon Coral), **Overclocked Motion** (`F`, Flux Amber).
> All six are authored at `docs/40:104-109`. Whoever owns the registry must mint six tokens and
> replace the six nulls; the pending flag is the machine-readable marker to search for.
>
> **→ Superseded by Ruling 9.** The flag's *substance* stands — the six tokens are still unminted
> and still needed — but it must not live in `content/`. Both `behavior_kind: null` and
> `behavior_kind_registration_pending: true` are removed from the six resources, and the outstanding
> work is recorded in this document instead. See Ruling 9 for the list in its new home.

**What else the ruling did not determine, and I chose:**

1. **The three added `resonance_field` keys, and which seven I treated as already present.** The
   ruling said to move “the field radius”. The remaining ten `field_rules` values had to go
   somewhere or be lost, and the site's `resonance_field` is where the spec puts field dimensions
   (`40:140`). `active_before_extraction_begins: true` now sits beside the site's existing
   `active_while_unopened: true`, which is arguably the same fact under two names — I kept both
   rather than judge one redundant and drop an authored value. De-duplicating them is a schema
   decision, exactly as the `world-props` fold's duplicated distance pair was.
2. **The property names `resonance_behavior` and `behavior_kind_registration_pending`.** `40:106`
   says “resonance behavior registration”; the snake_case rendering and the pending flag's name are
   mine. **→ Partly superseded by Ruling 9:** `behavior_kind_registration_pending` is gone, so only
   `resonance_behavior` remains my naming choice.
3. **Keeping `resonance_effect_name` as well as `resonance_behavior.effect_name`.** The former was
   already there with its own note; the latter is part of the relocated row. They agree, and this
   is asserted.

#### Ruling 5 — minute 33 of the wave schedule

**Citations.** The markdown row is well-formed; only its timing token is not. “at 33:15 intervals”
parses neither as an `m:ss` timestamp (`33:15` would be one instant, not a set) nor as a period
(`33:15` is not a duration). The column's contract is “at the listed time”
(`docs/32:21`, “a deterministic authored formation layered over baseline replenishment at the
listed time”), and every other repeating row in the table enumerates its times explicitly —
`29:20 and 29:45`, `34:10 and 34:40`. Four sectors, minute 33, quarter-minute spacing is the only
reading that satisfies the column contract, the “four sectors” in the cell, and the `33:15` token.

**Change.** In `content/encounters/standard-encounter-schedule.json`, `minute_rows[33]`
(`minute: 33`), `formation_events[0]`:

- `at` is `["33:00", "33:15", "33:30", "33:45"]` (was `null`).
- `timing_unresolved` is `false` (was `true`) — the row now carries times.
- **`timestamps_reconstructed: true`** and `timestamp_provenance: "reconstructed"` are the explicit
  machine-readable flags marking those four times as reconstructed rather than authored, plus a
  `reconstruction_basis` sentence stating the reasoning inline. The integration owner's requirement
  is that this be legible in the data, not laundered into a hashed bundle as authoritative: a
  consumer that trusts `at` without reading `timestamps_reconstructed` is reading provisional
  numbers as accepted ones, and a bundle hash over this row must not be cited as evidence the
  times were authored.
- `formations`, `enemy_ids`, and the verbatim `text` are unchanged, so the original
  “Streams rotate through four sectors at 33:15 intervals” cell survives in the data — as does the
  row-level `authored_event_or_boundary`. Only the *timing token's role as the timing value* is
  gone; the token itself is still there, verbatim, twice.

**Proof gate — what discharges the flag.** The flag comes off when, and only when, one of these
lands, and until then the four times must not be treated as accepted content:

1. `docs/32:89` (the minute-33 row) is reauthored to enumerate absolute times, exactly as the
   minute-29 and minute-34 rows do. If those times are the four written here, the flag is deleted
   and `source_refs` gains nothing; if they differ, the data changes and only the flag's removal is
   shared with this pass. This is the expected resolution.
2. Or the schedule schema grows a validated interval grammar (`40:144` — “Aggregate validation
   compares 35 contiguous rows, totals, earliest appearance, boss cadence, formation grammar”), in
   which case the row should hold the interval, not four expanded timestamps, and this
   reconstruction is discarded rather than confirmed.

A playtest that “feels right” does **not** discharge it: the question is what was authored, not
what plays well.

**What the ruling did not determine, and I chose:** the flag's *spelling* — the property names
`timestamps_reconstructed`, `timestamp_provenance`, and `reconstruction_basis`, and the decision to
flip `timing_unresolved` to `false` rather than leave it `true` beside a populated `at`. The ruling
required “an explicit machine-readable flag on that row” and did not name it. I also added one
`source_refs` entry for the reconstructed field.

### Integration-owner rulings applied — second pass

Four further items were ruled on after the first pass and applied here. Three of them reverse a
choice I had made and recorded above; the fourth confirms two things stay as they are. As before,
each entry gives the citations, exactly what changed, and separately what the ruling left to me.

**These are deliberate removals, not transcription gaps.** Five fields are absent from `content/`
by decision rather than because no document supplied them:
`contact_footprint.contact_diameter_m` and `contact_footprint.center_distance_that_begins_contact_m`
on the ten enemies (Rulings 1 and 6 — the compiler owns both under `40:114`), and
`resonance_behavior.behavior_kind`, `resonance_behavior.behavior_kind_registration_pending` and
`exclusion_reason` (Rulings 8 and 9 — invented metadata no schema declares). A reviewer should not
read any of the five as missing data, and none should be re-added by a later transcription pass
finding the value in `docs/` and assuming it was overlooked.

#### Ruling 6 — the centre distance is derived too, and comes out

**Citations.** `docs/72:86` gives the whole derivation in one sentence: “The Ripper's rank-zero
contact diameter is 0.80M. Every ordinary body scale in the alien roster multiplies that diameter.
Contact begins when the enemy contact circle and the mech's 0.50M-radius collision circle overlap.”
So the centre distance is `enemy contact diameter ÷ 2 + 0.50 M`, and
`docs/technical/40-content-data-and-validation.md:114` assigns producing it to the compiler along
with the footprint: “Validation derives world speeds/footprints and compares them with the
survivability report.”

This is a worse coupling than the diameter was, which is why it did not survive the second look.
The `0.50 M` term is the **player's** collision radius, not anything about the enemy. Storing the
sum on an enemy definition copies a player-baseline constant into the enemy catalog, so a change to
the mech's collision radius would silently invalidate ten enemy files — two owners on one value, in
two different catalogs. That is the same defect as the duplicated diameter, one catalog boundary
further out.

**Change.** `contact_footprint.center_distance_that_begins_contact_m` is **removed** from all ten
ordinary enemy definitions (`content/enemies/EN-01.json` … `EN-10.json`). `contact_footprint` keeps
`shape` and `reference_diameter_m`, and its `source_refs` prefix is retained because those two
surviving fields still come from `docs/72`. Bosses are untouched: the ruling named the enemy
definitions, and a boss's circle is authored directly rather than scaled from the Ripper.

The removed values were re-verified against the “Center distance that begins contact” column at
`docs/72:88-99` before deletion, and every one reproduces from the surviving
`body_scale_multiplier`:

| ID | Identity | `body_scale_multiplier` | Removed centre distance (`docs/72`) | scale × `0.80 M` ÷ 2 + `0.50 M` |
| --- | --- | ---: | ---: | ---: |
| `EN-01` | Skitterling | 0.55 | 0.72 M | 0.720 |
| `EN-02` | Ripper | 1.00 | 0.90 M | 0.900 |
| `EN-03` | Shellback | 1.30 | 1.02 M | 1.020 |
| `EN-04` | Lurker | 1.05 | 0.92 M | 0.920 |
| `EN-05` | Gloomwing | 1.20 | 0.98 M | 0.980 |
| `EN-06` | Needler | 1.00 | 0.90 M | 0.900 |
| `EN-07` | Razorling | 0.62 | 0.75 M | **0.748** |
| `EN-08` | Iron Ripper | 1.10 | 0.94 M | 0.940 |
| `EN-09` | Siegeback | 1.65 | 1.16 M | 1.160 |
| `EN-10` | Dreadwing | 1.35 | 1.04 M | 1.040 |

Nine rows reproduce exactly. `EN-07` is off by `0.002` for the *same* reason it was off by `0.004`
on the diameter — it is `0.748` typeset to the two decimals the column uses — which is contradiction
C-1 showing up a second time in a second derived column, and is further evidence that C-1 is a
typesetting artefact rather than a competing authored value.

**Verifier.** `A20` in `src/MechaMiner.Tools/ContentImport/verify_content.py` now asserts that no
enemy definition carries either derived field, matching on key names across `content/enemies/` so a
rename cannot slip past it. `reference_diameter_m` is explicitly allowlisted there, because `0.80 M`
is the Ripper's authored rank-zero diameter and not a per-enemy derived value.

**What the ruling did not determine, and I chose:** leaving `contact_footprint` as an object with
two remaining fields rather than flattening `shape` and `reference_diameter_m` onto the definition.
Flattening is a shape decision for the schema stream.

#### Ruling 7 — `body_scale_factor` goes back to `body_scale_multiplier`

**Citations.** Both documents call it a multiplication. `docs/31:35` — “Body scale multiplies the
Ripper's 0.80M contact diameter, not its decorative mesh.” `docs/technical/23:139` — “Hull ×4,
movement ×1.10, contact damage ×1.50, contact diameter ×1.25.”

**Change.** `body_scale_factor` is renamed **`body_scale_multiplier`** on all ten ordinary enemy
definitions. **All ten values are unchanged.** This reverts the rename Ruling 1 made and restores
the name the files carried before this pass began.

The point is the composition chain, which now reads in one vocabulary:
`0.80 M` Ripper reference × the enemy's `body_scale_multiplier` × the elite block's
`body_scale_multiplier` (`1.25`). My first-pass reasoning — that the enemy's own scale and the
elite's scaling-of-a-scale are different quantities deserving different names — was wrong: both are
multipliers applied to the same reference, and naming them differently made a two-step product look
like two unrelated ideas. `content/enemies/shared-elite-modifiers.json` was already
`body_scale_multiplier` and is unchanged.

#### Ruling 8 — `exclusion_reason` comes out of the shared elite modifiers

**Citations.** `40:249` — an agent “must not … add an unvalidated optional field”. The exclusion
already has a machine-readable carrier: `docs/technical/23:141` says “Needler is excluded by content
validation”, and that validation reads `EN-06`'s `elite_eligible: false`, which is the field
`40:114` puts in the enemy schema. The *reason* is authored prose at `docs/31:102` (“Needler does not
become an elite in the initial standard schedule because combining its projectile with the shared
elite multipliers reduces readability”) and restated at `docs/technical/23:141`.

**Change.** `exclusion_reason` is **removed** from `content/enemies/shared-elite-modifiers.json`.
Nothing else in the file changed.

No authored sentence was lost, which is where my first-pass reasoning went wrong: the sentence is
still at `docs/31:102`, where it is owned and maintained. Copying it into `content/` gave the same
prose a second writer with no validator to keep the two in step, so the copy could drift from the
source and nothing would notice. The exclusion **is** `elite_eligible: false` on `EN-06`; that is
the assertion a validator can act on, and it is already in the data.

#### Ruling 9 — `behavior_kind: null` and the pending flag come out of the six resources

**Citations.** `40:249` again — no unvalidated optional fields, and `40:90` — “Unknown fields are
errors rather than silently ignored.” `behavior_kind_registration_pending` is a field no schema will
ever declare: it describes the state of this transcription, not the state of the game. And
`behavior_kind: null` is a *nulled* optional field where the envelope's own rule for an
unavailable optional value is omission — the same treatment `presentation_id` gets throughout this
tree (`A4` in the verifier asserts it).

**Change.** In `content/resources/A.json` … `F.json`, both keys are removed from
`resonance_behavior`. The key is **omitted**, not set to `null`. Everything else in each
`resonance_behavior` block — `effect_name`, `resonance_effect`, `modifier`, `short_modifier`,
`edge_case_rule` — and both `source_refs` entries are unchanged.

**No tokens were minted, deliberately.** `DAT-004` owns the behavior registry manifest; it is
generated and staleness-checked, so a token grammar invented in a content PR would look official,
would not match the generated manifest, and would have to be re-minted — with the invented spelling
already referenced from six files by then. Guessing here costs more than waiting.

**Outstanding work, recorded here because it does not belong in `content/`.** Six resonance
behaviors need registry tokens and do not have them. `DAT-004` should mint against these six names,
all authored in the “Geode resonance fields” table at **`docs/40-mining-and-extraction.md:98-109`**:

| Resource | Geode | Effect name | Authored effect |
| --- | --- | --- | --- |
| `A` | Asterite | **Focused Assault** | outgoing enemy damage is 20% higher |
| `B` | Barysteel | **Dense Plating** | enemies take 20% less damage |
| `C` | Cinderglass | **Charged Payloads** | enemy projectile damage is 20% higher |
| `D` | Driftmetal | **Vector Lock** | player-imposed displacement magnitude and control-effect duration are 20% lower |
| `E` | Eidolon Coral | **Synchronized Aggression** | enemy attack cadence is 20% faster without increasing movement speed |
| `F` | Flux Amber | **Overclocked Motion** | enemy movement speed is 20% higher without increasing attack cadence |

`40:156` requires that every content `behavior_kind` have exactly one registered descriptor with a
compatible parameter schema. When the registry exists, each of the six resources gains a
`behavior_kind` holding its minted token. Until then the six `resonance_behavior` blocks carry the
authored effect and its `modifier` and no registration — which is accurate, because there is no
registration to carry. Each material's `effect_name` is the string to search on; that is authored
content, not a flag.

#### Ruling 10 — two things confirmed unchanged

Neither of these is a change; both were reviewed in the second pass and left as they are.

1. **`content/enemies/shared-elite-modifiers.json` keeps its location.** The FLAG under Ruling 3
   calls the path my choice and provisional; the ruling confirms `content/enemies/` is right for now.
   Elite modifiers are enemy values used by the enemy catalog — same domain — so the
   “unrelated convenience file” objection at `114:90`, which is what moved the player baseline out
   of the mech catalog (see the `shared-baseline.json` handoff above), does not apply here. The
   file's *stem* is still unsourced, and the path may still move when the schemas land.
2. **The geode resonance field radius stays `null`.** The integration owner is registering
   provisional values in `docs/technical/24`; no number goes into
   `content/mining-sites/specialized-material-geodes.json` before a citation exists for
   `source_refs` to point at. Ruling 4's gap flag on `resonance_field.radius_m` therefore stands
   unchanged.

### Per-definition notes, by catalog

#### Weapons (`content/weapons/`)

##### `W-AB` — `content/weapons/W-AB.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-AC` — `content/weapons/W-AC.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-AD` — `content/weapons/W-AD.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-AE` — `content/weapons/W-AE.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-AF` — `content/weapons/W-AF.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. `focusFillSecondsAtRankZero` (5.0 s) comes from docs/71-initial-weapon-numeric-catalog.md:232; the Rank-Zero table at docs/71-initial-weapon-numeric-catalog.md:80 states only the 1×-to-2× range.
8. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-BC` — `content/weapons/W-BC.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-BD` — `content/weapons/W-BD.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. A `base-travel second` is the distance an unmodified mech covers in one second at full speed (docs/71-initial-weapon-numeric-catalog.md:25), so `placementIntervalBaseTravelSeconds` is a distance, not a time.
8. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-BE` — `content/weapons/W-BE.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-BF` — `content/weapons/W-BF.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-CD` — `content/weapons/W-CD.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-CE` — `content/weapons/W-CE.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Reactor Pulse is the only weapon whose `Fixed properties` cell (docs/71-initial-weapon-numeric-catalog.md:86) states no numeric value; `targetCap` is null because the weapon explicitly has no target or overlap cap.
8. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-CF` — `content/weapons/W-CF.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. A `base-travel second` is the distance an unmodified mech covers in one second at full speed (docs/71-initial-weapon-numeric-catalog.md:25), so `placementIntervalBaseTravelSeconds` is a distance, not a time.
8. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-DE` — `content/weapons/W-DE.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-DF` — `content/weapons/W-DF.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. `activationMovementSpeedThresholdOfBase` (20% of base speed) comes from docs/71-initial-weapon-numeric-catalog.md:450; the Rank-Zero table at docs/71-initial-weapon-numeric-catalog.md:89 calls the threshold fixed without giving its value.
8. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `W-EF` — `content/weapons/W-EF.json`

1. `M` is one unmodified mech collision diameter, a relative spatial unit, not a metric distance (docs/71-initial-weapon-numeric-catalog.md:24); numeric keys therefore carry an `M` suffix rather than `Meters`.
2. Ore-stat units differ per stat, so each entry in `oreUpgradeableStats` carries an explicit `unit` field instead of encoding the unit in the key name.
3. `signatureMech` comes from docs/36-initial-mech-catalog.md:45, which is authoritative for the assignment. The `Signature mech` column of docs/66-weapon-catalog-and-resource-graph.md:39 still reads "Initial signature; mech TBD" and docs/weapons/README.md:20 only records Yes/No; both agree on signature membership but neither names the mech.
4. The `Status at DEC-075` column of docs/66-weapon-catalog-and-resource-graph.md:39 is deliberately not transcribed: docs/66-weapon-catalog-and-resource-graph.md:37 states DEC-125 moved every row to a different status, so the column is stale.
5. `favorableHordeDps` is an analytic scene estimate, not a cap; docs/71-initial-weapon-numeric-catalog.md:68 states the assumed simultaneous-victim counts and that benchmark captures supersede it.
6. The content envelope required by docs/technical/40-content-data-and-validation.md:76-89 (`schema_version`, `content_version`, `status`, `name_key`, `summary_key`, `tags`, `source_refs`, `presentation_id`) is not populated: no design document supplies values for those fields, and this transcription follows the DAT-007 field conventions.
7. Branch mechanics are transcribed separately into content/branches/; only the branch identity, transformation class, funding material, and specialized-material price are recorded here.

##### `weapon-stat-price-formula` — `content/weapons/stat-price-formula.json`

1. The price depends on the weapon's shared upgrade depth (total ranks bought across all three of its stats), not on the individual stat's rank; each weapon tracks its own depth.
2. There is no explicit rank cap; available ore and the 35-minute run limit form the practical cap (docs/65-weapon-stat-and-branch-upgrades.md:44).

#### Weapon branches (`content/branches/`)

##### `W-AB-fracture-lance` — `content/branches/W-AB-fracture-lance.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AB-kinetic-capacitor` — `content/branches/W-AB-kinetic-capacitor.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AB-unbounded-bore` — `content/branches/W-AB-unbounded-bore.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AC-danger-close-protocol` — `content/branches/W-AC-danger-close-protocol.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AC-interdiction-payload` — `content/branches/W-AC-interdiction-payload.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.
4. The section states no explicit expected-effect estimate, so `expectedEffect` is null.

##### `W-AC-saturation-cascade` — `content/branches/W-AC-saturation-cascade.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AD-echo-well` — `content/branches/W-AD-echo-well.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AD-gravity-slingshot` — `content/branches/W-AD-gravity-slingshot.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AD-singularity-forge` — `content/branches/W-AD-singularity-forge.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AE-containment-lattice` — `content/branches/W-AE-containment-lattice.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AE-replicator-swarm` — `content/branches/W-AE-replicator-swarm.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AE-wolfpack-protocol` — `content/branches/W-AE-wolfpack-protocol.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AF-coherence-memory` — `content/branches/W-AF-coherence-memory.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AF-cutting-vector` — `content/branches/W-AF-cutting-vector.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.

##### `W-AF-target-designator` — `content/branches/W-AF-target-designator.json`

1. No document in the repository assigns a stable branch ID; `id` and the filename are minted as <weaponId>-<branch-name-kebab-case> per the transcription convention.
2. Prerequisites, exclusivity, and the two-unit cost model are stated in docs/65-weapon-stat-and-branch-upgrades.md lines 66 and 68; the cost resource and amount for this branch come from this section's heading.
3. `rules` holds every bullet of the section verbatim with its source line; `effects` is the structured transcription of those same bullets.
4. The section states no explicit expected-effect estimate, so `expectedEffect` is null.

##### `W-BC-broadside-oscillator` — `content/branches/W-BC-broadside-oscillator.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.

##### `W-BC-suppressive-sequencer` — `content/branches/W-BC-suppressive-sequencer.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Damage is explicitly unchanged and no numeric value is given for the suppression benefit (line 269), so favorableSceneEffect.magnitude is null; only the class-level Functional range at docs/71-initial-weapon-numeric-catalog.md:503 applies.

##### `W-BC-zero-lag-emitter` — `content/branches/W-BC-zero-lag-emitter.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. No numeric favorable-scene magnitude is stated for this branch; the section only says realized gain is largest against fast lateral targets while ideal stationary-target DPS is unchanged (line 262). favorableSceneEffect.magnitude is therefore null. The only applicable numeric target is the class-level Amplification range at docs/71-initial-weapon-numeric-catalog.md:502.

##### `W-BD-hunter-mines` — `content/branches/W-BD-hunter-mines.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.

##### `W-BD-seed-charges` — `content/branches/W-BD-seed-charges.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Internally consistent: 4 micro-mines × 35% current Damage = the stated 140% parent damage for a fully used cluster.

##### `W-BD-selective-detonators` — `content/branches/W-BD-selective-detonators.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.

##### `W-BE-battery-overclock` — `content/branches/W-BE-battery-overclock.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Internally consistent: two pods beyond the first × +25% = the stated +50% cap at three active pods.

##### `W-BE-forward-bastion` — `content/branches/W-BE-forward-bastion.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.

##### `W-BE-guardian-firmware` — `content/branches/W-BE-guardian-firmware.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Damage and attack rate are explicitly unchanged and the defensive value is not quantified (line 317), so favorableSceneEffect.magnitude is null; only the class-level Functional range at docs/71-initial-weapon-numeric-catalog.md:503 applies.

##### `W-BF-deflection-ring` — `content/branches/W-BF-deflection-ring.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. No numeric favorable-scene magnitude is stated; realized value depends on incoming interceptable projectile volume, which the section does not quantify (line 342). favorableSceneEffect.magnitude is therefore null; only the class-level Functional range at docs/71-initial-weapon-numeric-catalog.md:503 applies.

##### `W-BF-kinetic-flywheel` — `content/branches/W-BF-kinetic-flywheel.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Internally consistent: 10 stacks × +4% = the stated +40% Damage and +40% orbit speed.

##### `W-BF-tethered-reaper` — `content/branches/W-BF-tethered-reaper.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Internally consistent: 200% base + up to 200% speed bonus = the stated 400% cap.

##### `W-CD-ball-lightning-projector` — `content/branches/W-CD-ball-lightning-projector.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Internally consistent: 8 hits × 16 rank-zero Arc damage = 128 per orb × 0.4 launches/s = the stated 51.2 rank-zero ideal single-target DPS.
5. Launch rate is stated as 0.4/s and "exactly one fifth of the base Arc Emitter attack rate" (2.000/s at rank zero); launchRateFractionOfBaseArcEmitterAttackRate records that one fifth as 0.2.

##### `W-CD-disruption-current` — `content/branches/W-CD-disruption-current.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
4. Damage and target cap are explicitly unchanged and the control benefit is not quantified (line 365), so favorableSceneEffect.magnitude is null; only the class-level Functional range at docs/71-initial-weapon-numeric-catalog.md:503 applies.

##### `W-CD-total-conduction` — `content/branches/W-CD-total-conduction.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id is composed as <weaponId>-<branch-name-kebab-case>.
2. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
3. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.

##### `W-CE-critical-mass-cycle` — `content/branches/W-CE-critical-mass-cycle.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CE — Reactor Pulse' heading (line 374).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-CE-kinetic-vent` — `content/branches/W-CE-kinetic-vent.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CE — Reactor Pulse' heading (line 374).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-CE-supernova-cycle` — `content/branches/W-CE-supernova-cycle.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CE — Reactor Pulse' heading (line 374).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-CF-carrier-ignition` — `content/branches/W-CF-carrier-ignition.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CF — Wake Projector' heading (line 398).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-CF-circuit-closure` — `content/branches/W-CF-circuit-closure.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CF — Wake Projector' heading (line 398).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-CF-runaway-wake` — `content/branches/W-CF-runaway-wake.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-CF — Wake Projector' heading (line 398).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DE-concussive-fan` — `content/branches/W-DE-concussive-fan.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DE — Scatter Array' heading (line 423).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DE-focal-array` — `content/branches/W-DE-focal-array.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DE — Scatter Array' heading (line 423).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DE-saturation-choke` — `content/branches/W-DE-saturation-choke.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DE — Scatter Array' heading (line 423).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DF-impact-transfer` — `content/branches/W-DF-impact-transfer.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DF — Ram Field' heading (line 448).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DF-momentum-cascade` — `content/branches/W-DF-momentum-cascade.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DF — Ram Field' heading (line 448).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-DF-siege-anchor` — `content/branches/W-DF-siege-anchor.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-DF — Ram Field' heading (line 448).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-EF-guardian-reserve` — `content/branches/W-EF-guardian-reserve.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-EF — Missile Rack' heading (line 473).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-EF-mirv-saturation` — `content/branches/W-EF-mirv-saturation.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-EF — Missile Rack' heading (line 473).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

##### `W-EF-spiral-barrage` — `content/branches/W-EF-spiral-barrage.json`

1. Branch sections in this document carry no standalone branch ID token; the heading supplies only name, class, and cost. The id and filename are composed as <weaponId>-<branch-name-kebab-case>.
2. weaponId and weaponName come from the enclosing '## W-EF — Missile Rack' heading (line 473).
3. `rules` holds every bullet of this section verbatim with its source line; `effects` is a structured restatement of those same bullets and introduces no value that is not written in them.
4. Section prose states no prerequisite. prerequisites is empty per docs/65-weapon-stat-and-branch-upgrades.md:50,66 ("Branches have no stat-rank prerequisites"; no rank, level, time, or boss prerequisite).
5. Exclusivity and irreversibility are not restated in this section; they come from docs/65-weapon-stat-and-branch-upgrades.md:56,68 and apply to all 45 branches.
6. expectedEffect is null: no bullet in this section is phrased as an 'Expected effect' estimate, unlike some earlier branch sections in this document. The section's own numeric outcomes are transcribed in `effects` and every bullet is preserved verbatim in `rules`, so nothing is lost.

#### Mechs (`content/mechs/`)

##### `MCH-01` — `content/mechs/MCH-01.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 47); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect.
4. inherentTrait.modifier is a structured restatement of the verbatim inherentTrait.effect string; no value is introduced.
5. traitStacking values come from the 'Trait comparison and stacking' table; the doc states them as percentages of the shared baseline, not as deltas.
6. crossDocNotes is present on every mech for a uniform shape and is empty when docs outside 36 add no numeric fact about this mech.
7. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.

##### `MCH-02` — `content/mechs/MCH-02.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 48); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. The overview table (line 48) writes the trait effect as '+15% weapon damage'; the detail section (line 87) writes '+15% damage to all equipped weapons'. Both are recorded; they agree numerically.
4. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect.
5. crossDocNotes is present on every mech for a uniform shape and is empty when docs outside 36 add no numeric fact about this mech.
6. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.

##### `MCH-03` — `content/mechs/MCH-03.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 49); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect. The extraction-time examples in those bullets are the doc's own stated numbers and are left as prose rather than restructured, because the doc gives them as approximations ('about').
4. crossDocNotes is present on every mech for a uniform shape and is empty when docs outside 36 add no numeric fact about this mech.
5. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.

##### `MCH-04` — `content/mechs/MCH-04.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 50); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect.
4. crossDocNotes is present on every mech for a uniform shape and is empty when docs outside 36 add no numeric fact about this mech.
5. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.

##### `MCH-05` — `content/mechs/MCH-05.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 51); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. This is the only mech whose trait is a flat rather than percentage modifier, so inherentTrait.modifier uses flatHull instead of percent.
4. resolvedBaseline records the doc's own explicit deployment values (line 162, '125 / 125 before account PowerUps').
5. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect, and minus the 125/125 bullet captured as resolvedBaseline.
6. crossDocNotes is present on every mech for a uniform shape and is empty when docs outside 36 add no numeric fact about this mech.
7. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.
8. Bastion's stacking row is expressed in absolute Hull points, not percentages, so this row uses 'value' where the other five mechs use 'percent'.

##### `MCH-06` — `content/mechs/MCH-06.json`

1. Row fields id/mech/signature/inherentTrait/selectionRole come from the Catalog overview table (line 52); field order follows that table's column order.
2. signatureWeaponId is not present in docs/36; it is resolved by weapon name against the Accepted base catalog assignment table in docs/66-weapon-catalog-and-resource-graph.md and carries its own _source.
3. traitNotes are the verbatim bullets of '### Signature and trait' minus the two bullets already captured as signature and inherentTrait.effect.
4. crossDocNotes carries the two resolved movement speeds docs/72 states for Razorback; docs/36 states no absolute M/s value for this mech.
5. Only the ID-to-weapon-name mapping is taken from docs/66. Its 'Signature mech' column reads 'Initial signature; mech TBD' and is stale; docs/36 is authoritative for the assignment.

#### Enemies (`content/enemies/`)

##### `EN-01` — `content/enemies/EN-01.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 0 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:56.
6. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
7. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-02` — `content/enemies/EN-02.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 1 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:57.
6. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
7. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-03` — `content/enemies/EN-03.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 4 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:60.
6. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
7. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-04` — `content/enemies/EN-04.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 5 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:61.
6. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
7. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-05` — `content/enemies/EN-05.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 10 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:66.
6. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
7. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-06` — `content/enemies/EN-06.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 16 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:72.
6. Needler is the sole ordinary specialist and the only ordinary identity with no elite form (docs/31-...md:102; enforced by content validation per docs/technical/23-encounter-director-and-enemy-runtime.md:141).
7. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-07` — `content/enemies/EN-07.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 13 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:69.
6. `variantOf` is read from the Family column value "Skitterling variant".
7. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-08` — `content/enemies/EN-08.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 18 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:74.
6. `variantOf` is read from the Family column value "Ripper variant".
7. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-09` — `content/enemies/EN-09.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 22 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:78.
6. `variantOf` is read from the Family column value "Shellback variant".
7. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `EN-10` — `content/enemies/EN-10.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. Earliest minute 24 re-verified against the debut row in docs/32-standard-wave-and-beacon-schedule.md:80.
6. `variantOf` is read from the Family column value "Gloomwing variant".
7. Per-minute spawn weights are authored in the 35-minute schedule (docs/32-standard-wave-and-beacon-schedule.md:54-90), not on the enemy definition, and spawn source class is a per-instance director tag (docs/technical/23-encounter-director-and-enemy-runtime.md:31-43).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted rather than invented.

##### `elite-modifier-profile` — `content/enemies/elite-modifier-profile.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. No document assigns a stable ID to the elite modifier profile, so `id` is null; docs/technical/40-content-data-and-validation.md:67 requires reusing accepted gameplay IDs exactly and none exists. Minting one is a boundary decision, not a transcription choice.
6. This is a single shared modifier profile applied at spawn to a base ordinary enemy definition; it is not a second enemy catalog row (docs/technical/23-encounter-director-and-enemy-runtime.md:154-160).
7. `addedControlResistance` is additive in percentage points, not a multiplier (docs/31-...md:110, docs/72-...md:236).

#### Bosses (`content/bosses/`)

##### `BOSS-01` — `content/bosses/BOSS-01.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. `arrival.activeSecondsIntoRun` is the doc's 7:00 timecode converted to active simulation seconds; the timecode is preserved verbatim.
6. No document states a boss Armor value; ordinary enemies are explicitly 0 Armor (docs/31-...md:25) but bosses are not covered, so `armor` is null.
7. Boss Hull values are initial anchors validated against the no-relic progression in docs/71-initial-weapon-numeric-catalog.md, not a substitute for time-to-kill validation (docs/31-...md:128).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted.

##### `BOSS-02` — `content/bosses/BOSS-02.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. `arrival.activeSecondsIntoRun` is the doc's 14:00 timecode converted to active simulation seconds; the timecode is preserved verbatim.
6. No document states a boss Armor value; ordinary enemies are explicitly 0 Armor (docs/31-...md:25) but bosses are not covered, so `armor` is null.
7. Boss Hull values are initial anchors validated against the no-relic progression in docs/71-initial-weapon-numeric-catalog.md, not a substitute for time-to-kill validation (docs/31-...md:128).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted.

##### `BOSS-03` — `content/bosses/BOSS-03.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. `arrival.activeSecondsIntoRun` is the doc's 21:00 timecode converted to active simulation seconds; the timecode is preserved verbatim.
6. No document states a boss Armor value; ordinary enemies are explicitly 0 Armor (docs/31-...md:25) but bosses are not covered, so `armor` is null.
7. Boss Hull values are initial anchors validated against the no-relic progression in docs/71-initial-weapon-numeric-catalog.md, not a substitute for time-to-kill validation (docs/31-...md:128).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted.

##### `BOSS-04` — `content/bosses/BOSS-04.json`

1. Field names are camelCase per the CAT-stream transcription convention. docs/technical/40-content-data-and-validation.md:92-100 instead mandates snake_case property names with _m/_m_per_s/_seconds/_hull/_percent suffixes and the common definition envelope at lines 76-89 (id, schema_version, content_version, status, name_key, summary_key, tags, source_refs, presentation_id). Only `id` is emitted here; the remaining envelope fields have no authored values in docs/ and are left to the schema stream (DAT-001..003).
2. `M` is one unmodified mech collision diameter (1.0 M), per docs/72-player-survivability-and-damage-baseline.md:47. Keys suffixed `M` or `MPerSecond` use that unit, not metres.
3. Percentages are transcribed as {"percent": N} objects; ranges as {"min": X, "max": Y}.
4. docs/data/contact-damage-pressure.csv and docs/data/survivability-baseline.csv are non-authoritative mirrors (docs/data/README.md:5,10) and were used only to cross-check. Every overlapping value agrees with the Markdown.
5. `arrival.activeSecondsIntoRun` is the doc's 28:00 timecode converted to active simulation seconds; the timecode is preserved verbatim.
6. No document states a boss Armor value; ordinary enemies are explicitly 0 Armor (docs/31-...md:25) but bosses are not covered, so `armor` is null.
7. Boss Hull values are initial anchors validated against the no-relic progression in docs/71-initial-weapon-numeric-catalog.md, not a substitute for time-to-kill validation (docs/31-...md:128).
8. No presentation entry ID exists anywhere in docs/, so the presentation reference required by docs/technical/40-content-data-and-validation.md:114 is omitted.
9. Making the airborne boss untargetable would be a player-visible gameplay change, not a visual choice (docs/technical/23-encounter-director-and-enemy-runtime.md:197).

#### Relics (`content/relics/`)

##### `REL-01` — `content/relics/REL-01.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:28; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-02` — `content/relics/REL-02.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:29; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-03` — `content/relics/REL-03.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:30; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-04` — `content/relics/REL-04.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:31; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-05` — `content/relics/REL-05.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:32; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-06` — `content/relics/REL-06.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:33; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-07` — `content/relics/REL-07.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:34; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-08` — `content/relics/REL-08.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:35; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-09` — `content/relics/REL-09.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:36; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

##### `REL-10` — `content/relics/REL-10.json`

1. Field order follows the doc 69 `## Catalog overview` column order (ID, Relic, Primary transformation, Core tradeoff), then the per-relic prose fields.
2. Catalog-overview table row for this relic is docs/69-initial-relic-catalog.md:37; the fresh/unlocked availability rules are docs/69-initial-relic-catalog.md:39-59.
3. `rules` holds the relic's prose bullets verbatim with their source line numbers; numbers stated in that prose are also structured under `effects`.
4. `_source` marks every value or rule folded in from a document other than docs/69-initial-relic-catalog.md.

#### Utilities (`content/utilities/`)

##### `UTL-A1` — `content/utilities/UTL-A1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-A2` — `content/utilities/UTL-A2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.

##### `UTL-B1` — `content/utilities/UTL-B1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.

##### `UTL-B2` — `content/utilities/UTL-B2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-C1` — `content/utilities/UTL-C1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-C2` — `content/utilities/UTL-C2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.
7. effect.stackingClassification is null: docs/68-utility-catalog.md:253-255 classifies flat statistic changes, additive percentages, and Recovery, but says nothing about how a recharging one-hit negation stacks with another negation source. Logged as a gap.

##### `UTL-D1` — `content/utilities/UTL-D1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-D2` — `content/utilities/UTL-D2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.

##### `UTL-E1` — `content/utilities/UTL-E1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-E2` — `content/utilities/UTL-E2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.

##### `UTL-F1` — `content/utilities/UTL-F1.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole comes from the '## Fresh-profile and unlocked availability' table (docs/68-utility-catalog.md:265-272), which lists only the six fresh-profile utilities.

##### `UTL-F2` — `content/utilities/UTL-F2.json`

1. Overview-table row and per-utility rank table are both from docs/68-utility-catalog.md; the rank table is authoritative for the four tier values and the overview cell restates Installed and Rank 3.
2. acquisition mirrors the catalog-wide '## Shared acquisition and rank rules' at docs/68-utility-catalog.md:25-31; the 300-ore rank total is restated at docs/60-resources-crafting-progression.md:158.
3. catalogWideRules duplicates the two catalog-wide rule lists verbatim in every utility file because the mandated content layout (docs/technical/40-content-data-and-validation.md:34-63) provides no shared-rules file inside content/utilities/.
4. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
5. effect.stackingClassification is derived from the catalog-wide modifier rules at docs/68-utility-catalog.md:253-255, not from a dedicated doc field.
6. availability.coverageRole is null: the coverage-role column at docs/68-utility-catalog.md:265-272 exists only for the six fresh-profile utilities, and no doc assigns a coverage role to the six Advanced Utility Suite utilities. Logged as a gap.

##### `radar-unassigned-id` — `content/utilities/radar-unassigned-id.json`

1. BOUNDARY DECISION REQUIRED — THIS FILE HAS NO ID. The resource radar is the thirteenth utility (docs/technical/110-implementation-plan-for-ai-agents.md:181 'All 15 weapons/45 branches, 12 utilities plus radar'; docs/60-resources-crafting-progression.md:141 'twelve non-radar utilities'), but no document in the repository ever assigns it a UTL-* identifier. docs/technical/40-content-data-and-validation.md:67 requires 'Reuse accepted gameplay IDs exactly for defined content', so minting an ID such as UTL-R1 or UTL-00 here would fabricate an accepted ID that no decision record contains. id is therefore null and the filename is radar-unassigned-id.json. The design owner must accept an ID (and its file name) in a decision record before this definition can pass any schema that requires an id; the compiler assertion of '12 utilities plus radar' cannot be satisfied by an ID pattern alone.
2. material is null by specification, not omission: docs/68-utility-catalog.md:31 places the radar 'outside the material table' and docs/technical/40-content-data-and-validation.md:128 calls it the 'ore-only radar exception'. It costs 300 common ore instead of one specialized material unit.
3. effectRules holds the twelve '### Decided behavior' bullets verbatim; the slot-commitment/presentation paragraph (docs/50:121) and the '### Design role' paragraph (docs/50:125) are kept in their own fields.
4. primaryRole and installedToRank3 are null: both columns belong to the docs/68-utility-catalog.md:35 overview table, which deliberately excludes the radar. No doc supplies an equivalent one-line role label; the verbatim design-role prose is kept in effectRules instead. Logged as a gap.
5. ranks is null and rankCount is 0 because the radar has no rank track (docs/68-utility-catalog.md:31 'has no ranks'; docs/60-resources-crafting-progression.md:158 'The radar has no initial upgrade track'). This is specified, not missing.
6. catalogWideRules.sharedAcquisitionAndRankRules is reproduced for reference only; its first two bullets apply to non-radar utilities by their own wording.
7. Markdown emphasis, code markers, and link syntax were removed from transcribed prose; wording is otherwise verbatim.
8. Bearing presentation numerics (six-degree fan, cluster above three) come from the interface specification that docs/50:125 defers to, not from docs/50 itself.

#### PowerUps (`content/powerups/`)

##### `PU-C01` — `content/powerups/PU-C01.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:37; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-C01 — Weapons Calibration' section; one object per doc row in rank order, columns Rank | Total weapon Damage | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-C01 — Weapons Calibration' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-C02` — `content/powerups/PU-C02.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:38; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-C02 — Cycle Optimizer' section; one object per doc row in rank order, columns Rank | Total weapon Attack Rate | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-C02 — Cycle Optimizer' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-C03` — `content/powerups/PU-C03.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:39; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-C03 — Field Geometry' section; one object per doc row in rank order, columns Rank | Total weapon Area | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-C03 — Field Geometry' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-C04` — `content/powerups/PU-C04.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:40; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-C04 — Persistence Lattice' section; one object per doc row in rank order, columns Rank | Total weapon Duration | Price | Cumulative cost. The array has 4 entries because the cap is 4; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-C04 — Persistence Lattice' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-E01` — `content/powerups/PU-E01.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:47; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-E01 — Extraction Tuning' section; one object per doc row in rank order, columns Rank | Total forward extraction rate | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-E01 — Extraction Tuning' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-E02` — `content/powerups/PU-E02.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:48; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-E02 — Tether Amplifier' section; one object per doc row in rank order, columns Rank | Total extraction-zone radius | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-E02 — Tether Amplifier' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-E03` — `content/powerups/PU-E03.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:49; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-E03 — Ore Assay' section; one object per doc row in rank order, columns Rank | Total mined common ore | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-E03 — Ore Assay' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-M01` — `content/powerups/PU-M01.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:45; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-M01 — Servo Overdrive' section; one object per doc row in rank order, columns Rank | Total movement speed | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-M01 — Servo Overdrive' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-M02` — `content/powerups/PU-M02.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:46; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-M02 — Survey Optics' section; one object per doc row in rank order, columns Rank | Total discovery radius | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-M02 — Survey Optics' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-S01` — `content/powerups/PU-S01.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:41; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-S01 — Hull Reinforcement' section; one object per doc row in rank order, columns Rank | Total maximum Hull | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-S01 — Hull Reinforcement' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-S02` — `content/powerups/PU-S02.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:42; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-S02 — Ablative Armor' section; one object per doc row in rank order, columns Rank | Total Armor | Price | Cumulative cost. The array has 3 entries because the cap is 3; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-S02 — Ablative Armor' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-S03` — `content/powerups/PU-S03.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:43; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-S03 — Repair Nanites' section; one object per doc row in rank order, columns Rank | Total Recovery | Price | Cumulative cost. The array has 5 entries because the cap is 5; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. rules are the verbatim bullets of the 'PU-S03 — Repair Nanites' section, in document order.
5. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
6. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
8. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
9. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
10. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
11. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.

##### `PU-S04` — `content/powerups/PU-S04.json`

1. id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold come from the 'Catalog overview' table row at docs/62-permanent-powerup-catalog.md:44; field order follows that table's column order (ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost).
2. ranks is the per-PowerUp rank/price table in the 'PU-S04 — Emergency Reboot' section; one object per doc row in rank order, columns Rank | Revival charges | Price | Cumulative cost. The array has 1 entries because the cap is 1; rank-array lengths are deliberately not normalized across PowerUps.
3. The 'text' member of each effect object is the doc cell verbatim; the sibling numeric member is the same value typed as a number. No value is introduced.
4. revival is a structured restatement of the first bullet of the 'PU-S04 — Emergency Reboot' section (40% of current maximum Hull, two active-simulation seconds of invulnerability) plus the automatic/no-pause/no-recharge bullets. No value is introduced.
5. rules are the verbatim bullets of the 'PU-S04 — Emergency Reboot' section, in document order.
6. No localization key, presentation identifier, icon, sort order, or content/schema version exists for PowerUps anywhere in docs/; those fields are therefore absent rather than guessed.
7. Source of id, domain, powerUp, perRankEffect, cap, maximumEffect and totalCostHyperGold.
8. These fields are the catalog-wide shared purchase rules and are identical on every PowerUp file.
9. prerequisite is null because line 23 states categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
10. uiGrouping repeats the Domain column; docs/technical/40-content-data-and-validation.md:136 requires PowerUps to carry rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping.
11. Corroborates the 9,450 Hyper Gold catalog total that the 13 totalCostHyperGold values sum to. DEC-120 is status: accepted, authoritative: false; docs/62 remains the authoritative source for the per-entry number.
12. Restates the fully active account envelope, corroborating this entry's maximumEffect. Also restated verbatim in docs/62-permanent-powerup-catalog.md 'Fully upgraded account envelope' lines 251-256.
13. 'Emergency Reboot provides one automatic 40%-Hull revival and two seconds of invulnerability per run at its sole rank.' Corroborates revival.hullRestoredFractionOfCurrentMaximum, revival.invulnerabilityActiveSimulationSeconds and cap 1.

#### Option unlocks (`content/unlocks/`)

##### `UNL-01` — `content/unlocks/UNL-01.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:50; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. unlocks.utilities is the 'Utility added by the suite' table at lines 74-81; replacesNothing is explicit — freshProfileUtility on each row is the 'Fresh-profile utility' table at lines 63-70 and is retained, not replaced.
3. utilityId values are not present in docs/63; they are resolved by utility name against the 'Catalog overview' table of docs/68-utility-catalog.md and carry their own _source.
4. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
5. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
6. Source of id, unlock, category, effect and costHyperGold.
7. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
8. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
9. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
10. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
11. Only the ID-to-utility-name-and-material mapping is taken from docs/68. docs/68 lines 271-273 independently restate the 600-Hyper-Gold suite and the same six unlocked utilities.
12. DEC-121's own catalog table restates this unlock's cost and the 2,150 Hyper Gold total. DEC-121 is status: accepted, authoritative: false; docs/63 remains the authoritative source.
13. 'Buying the 600-Hyper-Gold Advanced Utility Suite permanently unlocks Survey Aperture, Reinforced Bulkhead, Capacitor Screen, Extraction Tether, Priority Uplink, and Field Expander together.' Matches this file exactly.

##### `UNL-02` — `content/unlocks/UNL-02.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:51; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. relicId is not present in docs/63; it is resolved by relic name against the 'Catalog overview' table of docs/69-initial-relic-catalog.md and carries its own _source.
3. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
4. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
5. Source of id, unlock, category, effect and costHyperGold.
6. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
7. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
8. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
9. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
10. Only the ID-to-relic-name mapping is taken from docs/69.
11. docs/69's 'Relic | Unlock cost' table independently states 250 for Ghostline Chassis, matching docs/63. DEC-121 lines 19-26 also restate it.
12. Verbatim pool-behavior and run-local retention rules that apply to every relic unlock.

##### `UNL-03` — `content/unlocks/UNL-03.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:52; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. relicId is not present in docs/63; it is resolved by relic name against the 'Catalog overview' table of docs/69-initial-relic-catalog.md and carries its own _source.
3. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
4. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
5. Source of id, unlock, category, effect and costHyperGold.
6. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
7. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
8. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
9. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
10. Only the ID-to-relic-name mapping is taken from docs/69.
11. docs/69's 'Relic | Unlock cost' table independently states 250 for Dead-Reckoning Array, matching docs/63. DEC-121 lines 19-26 also restate it.
12. Verbatim pool-behavior and run-local retention rules that apply to every relic unlock.

##### `UNL-04` — `content/unlocks/UNL-04.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:53; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. relicId is not present in docs/63; it is resolved by relic name against the 'Catalog overview' table of docs/69-initial-relic-catalog.md and carries its own _source.
3. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
4. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
5. Source of id, unlock, category, effect and costHyperGold.
6. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
7. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
8. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
9. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
10. Only the ID-to-relic-name mapping is taken from docs/69.
11. docs/69's 'Relic | Unlock cost' table independently states 300 for War-Drum Oscillator, matching docs/63. DEC-121 lines 19-26 also restate it.
12. Verbatim pool-behavior and run-local retention rules that apply to every relic unlock.

##### `UNL-05` — `content/unlocks/UNL-05.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:54; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. relicId is not present in docs/63; it is resolved by relic name against the 'Catalog overview' table of docs/69-initial-relic-catalog.md and carries its own _source.
3. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
4. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
5. Source of id, unlock, category, effect and costHyperGold.
6. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
7. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
8. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
9. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
10. Only the ID-to-relic-name mapping is taken from docs/69.
11. docs/69's 'Relic | Unlock cost' table independently states 350 for Redline Crucible, matching docs/63. DEC-121 lines 19-26 also restate it.
12. Verbatim pool-behavior and run-local retention rules that apply to every relic unlock.

##### `UNL-06` — `content/unlocks/UNL-06.json`

1. id, unlock, category, effect and costHyperGold come from the 'Catalog overview' table row at docs/63-permanent-option-unlock-catalog.md:55; field order follows that table's column order (ID | Unlock | Category | Effect | Cost).
2. relicId is not present in docs/63; it is resolved by relic name against the 'Catalog overview' table of docs/69-initial-relic-catalog.md and carries its own _source.
3. prerequisite is null: line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
4. No localization key, presentation identifier, icon, or sort order exists for option unlocks anywhere in docs/; those fields are absent rather than guessed.
5. Source of id, unlock, category, effect and costHyperGold.
6. These fields are the catalog-wide shared purchase rules and are identical on every unlock file.
7. prerequisite is null because line 38 states there is no prerequisite tree, challenge prerequisite, account-level gate, random shop, or purchase-order requirement.
8. grantsAccountAccessOnly comes from the 'Purpose' section, line 18: an option unlock grants account access only.
9. docs/technical/40-content-data-and-validation.md:136 requires unlocks to carry exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled.
10. Only the ID-to-relic-name mapping is taken from docs/69.
11. docs/69's 'Relic | Unlock cost' table independently states 400 for Sequential Reactor, matching docs/63. DEC-121 lines 19-26 also restate it.
12. Verbatim pool-behavior and run-local retention rules that apply to every relic unlock.

#### Resources (`content/resources/`)

##### `A` — `content/resources/A.json`

1. The table row for this material is line 22.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `B` — `content/resources/B.json`

1. The table row for this material is line 23.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `C` — `content/resources/C.json`

1. The table row for this material is line 24.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `D` — `content/resources/D.json`

1. The table row for this material is line 25.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `E` — `content/resources/E.json`

1. The table row for this material is line 26.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `F` — `content/resources/F.json`

1. The table row for this material is line 27.
2. Resources carry no RES-* stable ID in any design doc; only the letter codes A-F (docs/61-specialized-resource-identities.md:18). RES-001..RES-006 are already taken by docs/research/, so no new RES-* ID was minted: `id` is the doc's own letter code verbatim.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count` on every resource definition; no gameplay doc states one, so maximumSafeCount is null.
4. Localization keys (name_key / summary_key per docs/technical/40-content-data-and-validation.md:76-89) are not authored here; content/localization/ is owned by a separate stream.
5. primaryColor is a presentation default, not part of the resource identity (docs/61-specialized-resource-identities.md:29): lighting, biome palettes, and visual effects may shift the rendered value while the other recognition channels are preserved.
6. docs/60-resources-crafting-progression.md:124-131 restates looseAssociation as a `Loose personality` column with the same content plus a serial `and` (e.g. 'Precision, focus, stable fields, and anchoring'); the authoritative docs/61 wording is used here.
7. The per-material prose sections (docs/61-specialized-resource-identities.md:31-77) and the `Accessibility and recognition standard` table (docs/61-specialized-resource-identities.md:109-116) are not transcribed here; those recognition cues belong to content/presentation/.
8. sharedEconomyTier is from docs/61-specialized-resource-identities.md:87 - specialized materials share the same economy tier and establish no global rarity or power hierarchy.
9. persistence is from docs/40-mining-and-extraction.md:90 - 'The specialized material and 50 common ore are run-local and are lost if unspent when the run ends.'
10. resonanceEffectName references the matching entry in geode-resonance-effects.json (resonance behavior registration per docs/technical/40-content-data-and-validation.md:106).

##### `common-ore` — `content/resources/common-ore.json`

1. Common ore has no RES-* or other stable ID in any design doc; the docs name it only as 'common ore' / 'common basic ore'. `id` is a kebab-case slug of that name because no doc identifier exists to copy verbatim, and RES-001..RES-006 are already taken by docs/research/.
2. scope / availability / primaryPurpose / persistence are the four columns of the docs/60-resources-crafting-progression.md:18-21 table. That table's first row covers the whole 'Ordinary crafting resources' scope, which includes the six specialized materials as well as common ore; there is no common-ore-only row anywhere in the docs.
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count`; no gameplay doc states one, so maximumSafeCount is null.
4. Every numeric payout carries its own `_source` because those values come from docs/40-mining-and-extraction.md rather than from this file's main source section.
5. Mining-point class definitions (zone dimensions, installment thresholds, decay, grace, beacons) live in content/mining-sites/; only the resource payout numbers are repeated here.
6. Localization keys are not authored here; content/localization/ is owned by a separate stream.
7. The 1,600-2,000 per-map ore range is stated at docs/60-resources-crafting-progression.md:49 and :135.

##### `geode-resonance-effects` — `content/resources/geode-resonance-effects.json`

1. Cohesive aggregate: the six resonance effects have no individual IDs, so they are one file keyed by the geode's material.
2. `shortModifier` on each entry is transcribed from the duplicate table at docs/61-specialized-resource-identities.md:94-101 (section 'Geode resonance behavior'), which is an abbreviated restatement of the same six 20% modifiers.
3. Divergence between the two tables: docs/61:94-101 omits every named effect (Focused Assault, Dense Plating, Charged Payloads, Vector Lock, Synchronized Aggression, Overclocked Motion) and omits the two non-coupling qualifiers docs/40 states - Eidolon Coral is '20% faster without increasing movement speed' and Flux Amber is '20% higher without increasing attack cadence'. No numeric value diverges; both tables state 20% for all six. docs/40 is used as authoritative because docs/61:103 points back to it.
4. materialId is the letter code from the identity map at docs/61-specialized-resource-identities.md:20-27; docs/40's table names materials only.
5. modifier.direction is derived from the doc's own wording ('higher', 'lower', 'less', 'faster').
6. fieldRules.radiusMeters is null: docs/40-mining-and-extraction.md:48 leaves the exact mining-zone radius and any resource-specific size variation open, and no doc gives the resonance field radius beyond 'larger than its extraction zone'.
7. The 20% modifiers are explicitly initial playtest values (docs/40-mining-and-extraction.md:111); their common magnitude does not assert equal practical difficulty.

##### `hyper-gold` — `content/resources/hyper-gold.json`

1. Hyper Gold has no RES-* or other stable ID in any design doc. `id` is a kebab-case slug of the player-facing name because no doc identifier exists to copy verbatim, and RES-001..RES-006 are already taken by docs/research/.
2. scope / availability / primaryPurpose / persistence are the four columns of the docs/60-resources-crafting-progression.md:18-21 table (Hyper Gold row, line 21).
3. docs/technical/40-content-data-and-validation.md:106 expects a `maximum safe count`; no gameplay doc states one, so maximumSafeCount is null.
4. The Hyper Gold site's threat-beacon mechanics (first-progress activation and the 25% / 50% / 75% escalation thresholds at 11.25 / 22.5 / 33.75 seconds, docs/40-mining-and-extraction.md:123) are the mining-site class definition and belong to content/mining-sites/, not to this resource entry.
5. docs/61-specialized-resource-identities.md:126 records that Hyper Gold's appearance and audio identity are explicitly undecided, so no icon or audio identity is authored here.
6. Localization keys are not authored here; content/localization/ is owned by a separate stream.
7. runCeiling 400 and increasedByPowerUps=false are from docs/60-resources-crafting-progression.md:80.

#### Mining sites (`content/mining-sites/`)

##### `hyper-gold-sites` — `content/mining-sites/hyper-gold-sites.json`

1. id is null: this mining-site class has no ID and no table anywhere in docs/. A stable-ID decision is required before docs/technical/40-content-data-and-validation.md:67 ("Reuse accepted gameplay IDs exactly") can be satisfied; minting one is a boundary decision, not a transcription choice.
2. Field names are derived from prose because the source has no column headers.
3. extractionZoneRadiusMeters is null: docs/40-mining-and-extraction.md:48 states "Exact radius and any resource-specific size variation remain open."
4. Arithmetic check: 3 sites x 100 Hyper Gold = 300 Hyper Gold, matching "The sites contain 300 Hyper Gold in total" (docs/40-mining-and-extraction.md:121).
5. The 25/50/75% threshold seconds (11.25 / 22.5 / 33.75) are stated verbatim at docs/40-mining-and-extraction.md:123 and equal the stated fractions of the 45-second extraction.
6. Beacon response sizes, formations, and elite additions are owned by the encounter schedule (content/encounters/standard-encounter-schedule.json, from docs/32-standard-wave-and-beacon-schedule.md:100-113) and are referenced rather than duplicated.
7. Per-boss Hyper Gold drops (25 each, docs/40-mining-and-extraction.md:121) are boss loot, not a mining-site payout, and belong to content/bosses/.
8. presentation, map marker, and spawn exclusions (required by docs/technical/40-content-data-and-validation.md:140) are not specified for this class; placement guarantees are transcribed in content/maps/standard-map-generation-contract.json.

##### `rich-ore-seams` — `content/mining-sites/rich-ore-seams.json`

1. id is null: this mining-site class has no ID and no table anywhere in docs/. A stable-ID decision is required before docs/technical/40-content-data-and-validation.md:67 ("Reuse accepted gameplay IDs exactly") can be satisfied; minting one is a boundary decision, not a transcription choice.
2. Field names are derived from prose because the source has no column headers.
3. extractionZoneRadiusMeters is null: docs/40-mining-and-extraction.md:48 states "Exact radius and any resource-specific size variation remain open."
4. totalUninterruptedExtractionPerMapSeconds 120 transcribes "two minutes of uninterrupted extraction if all are depleted" (docs/40-mining-and-extraction.md:72).
5. Arithmetic check: 8 seams x 200 ore = 1,600 common ore, matching the stated per-map total. Combined with the 20 standard seams (2,000), the map total is 3,600 common ore from ore seams.
6. "Rich ore" is a high-yield source of ordinary common ore; it is not Hyper Gold and does not persist between runs (docs/40-mining-and-extraction.md:74).
7. presentation, map marker, and spawn exclusions (required by docs/technical/40-content-data-and-validation.md:140) are not specified for this class in the gameplay docs and are therefore absent rather than guessed.

##### `specialized-material-geodes` — `content/mining-sites/specialized-material-geodes.json`

1. id is null: this mining-site class has no ID and no table of its own anywhere in docs/. A stable-ID decision is required before docs/technical/40-content-data-and-validation.md:67 ("Reuse accepted gameplay IDs exactly") can be satisfied; minting one is a boundary decision, not a transcription choice.
2. Field names other than the survey-state table (docs/40-mining-and-extraction.md:82-86) are derived from prose.
3. extractionZoneRadiusMeters is null: docs/40-mining-and-extraction.md:48 states "Exact radius and any resource-specific size variation remain open."
4. resonanceField.radiusMeters is null: docs/40-mining-and-extraction.md:100 states only that the field is "larger than its extraction zone"; no dimension is given.
5. The per-material resonance effect names and texts live in the resource catalog (docs/40-mining-and-extraction.md:102-109 duplicated at docs/61-specialized-resource-identities.md:94) and are not duplicated here.
6. Arithmetic check: 4 present materials x 8-10 geodes = 32-40 geodes per map, matching the stated "A standard map therefore contains 32–40 material geodes" (docs/40-mining-and-extraction.md:88).
7. commonOreFromCompletionJackpotsPerMap 1600-2000 is stated at docs/50-maps-resources-and-navigation.md:22 and docs/60-resources-crafting-progression.md:49 and equals 32-40 geodes x 50 ore.
8. presentation, map marker, and spawn exclusions (required by docs/technical/40-content-data-and-validation.md:140) are not specified for this class in the gameplay docs; placement guarantees are transcribed in content/maps/standard-map-generation-contract.json.

##### `standard-ore-seams` — `content/mining-sites/standard-ore-seams.json`

1. id is null: this mining-site class has no ID and no table anywhere in docs/. A stable-ID decision is required before docs/technical/40-content-data-and-validation.md:67 ("Reuse accepted gameplay IDs exactly") can be satisfied; minting one is a boundary decision, not a transcription choice.
2. Field names are derived from prose because the source has no column headers (docs/40-mining-and-extraction.md:58-132 is entirely prose).
3. extractionZoneRadiusMeters is null: docs/40-mining-and-extraction.md:48 states "Exact radius and any resource-specific size variation remain open."
4. totalUninterruptedExtractionPerMapSeconds 300 transcribes "five minutes of uninterrupted extraction if all are depleted" (docs/40-mining-and-extraction.md:66).
5. Arithmetic check: 20 seams x 100 ore = 2,000 common ore, matching the stated per-map total.
6. Cross-checked against docs/decisions/DEC-082, DEC-083, DEC-090 as listed at docs/40-mining-and-extraction.md:215-218.
7. presentation, map marker, and spawn exclusions (required by docs/technical/40-content-data-and-validation.md:140) are not specified for this class in the gameplay docs and are therefore absent rather than guessed.

#### Encounter schedule (`content/encounters/`)

##### `WAV-01` — `content/encounters/standard-encounter-schedule.json`

1. One aggregate file per docs/technical/40-content-data-and-validation.md:144, which requires 'One aggregate standard schedule file' containing mode ID, duration, minute rows, composition weights, minimums, pulses, formations, boss warnings/arrivals, beacon response table, and population ceilings.
2. modeId is null: no mode identifier is stated anywhere in the source document.
3. Enemy and boss references use the exact IDs from docs/31-initial-alien-roster.md:39-48 and :123-126.
4. authoredEventOrBoundary holds the source cell verbatim; debutEnemyIds, bossWarningAt, bossArrival, scheduledElites, and formationEvents are structured restatements of that same cell and add no information.
5. DEC-011 (25-minute timer) and DEC-012 (five-minute boss cadence) are status: superseded and were not used.

#### Map generation (`content/maps/`)

##### `MGC-01` — `content/maps/standard-map-generation-contract.json`

1. One aggregate file: docs/technical/40-content-data-and-validation.md:148 describes map generation as a single field set (mode/map ID, generation version, region/topology/scale ranges, static obstacle targets, distance bands, site counts, distribution constraints, candidate clearances, retry budgets, discovery settings, rock rules, landmark pools) and docs/technical/40-content-data-and-validation.md:63 allows the smallest cohesive aggregate.
2. modeId, mapId, and generationVersion are null: no such identifiers or version appear in the source document.
3. retryBudgets is null: docs/51-standard-map-generation-contract.md defines validity rules (lines 184-200) but states no retry budget.
4. landmarkPools is null: the document requires one prominent landmark per region and caps repetition (lines 35, 158-166) but enumerates no landmark pool.
5. Values labeled "initial baseline" in the source are transcribed as-is; docs/51-standard-map-generation-contract.md:12 states they are accepted starting points for playtesting rather than final balance.
6. Distances use the document's "M" unit (metres) and base-travel time at the 3.0M/s unmodified mech speed.
7. Destructible-rock and health-pack property definitions live in content/maps/world-props.json; the dynamic rock population rule is kept here because docs/technical/40-content-data-and-validation.md:148 lists "rock rules" under map generation.
8. Mining-site payout profiles live in content/mining-sites/; only their placement and distribution constraints are transcribed here.

