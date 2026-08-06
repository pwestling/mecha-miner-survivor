# Content transcription notes

**This file is not an `OQ-###` open-question register.** The two registers are
`docs/open-questions.md` and `docs/technical/open-questions.md`. Nothing here carries or mints
an OQ ID. This file is the reviewable record of the per-definition transcription notes that
used to live in a `notes` array inside every JSON definition of the catalog transcription under
`content/`. **That transcription does not deliver `DAT-007`** — `DAT-007`'s prerequisite `DAT-006` is
not Done and `DAT-001` has no code, so what is here is material prepared ahead of `DAT-007` under
`docs/technical/114-autonomous-agent-execution-protocol.md:141` ("A task may prepare read-only
analysis while waiting"), and it is neither validated nor validatable until `content/schemas/` exists.
Where a note below says "the DAT-007 catalog" or "the DAT-007 field conventions", it is naming the
package the work anticipates, not a package this branch closed; note text is reproduced verbatim and
was not edited to say so.
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

  **→ Superseded by Ruling 11.** The four IDs were minted (`SITE-01`–`SITE-04`) and the four
  provisional stem keys were rewritten, exactly as this entry said they would have to be.
- No `id` field **at all**, because the file is not a definition and therefore has nothing to
  identify: `content/enemies/shared-elite-modifiers.json` (Ruling 3). This is a different case from
  `"id": null` — null means “a definition whose ID has not been minted yet”, absent means “not a
  definition”, the same treatment the deleted `content/mechs/shared-baseline.json` had. It carries
  no localized string and no stem-based key.

  **→ Superseded by Ruling 11.** The file is `ELT-01` and is an ordinary addressable definition.
  It still carries no localized string and no `name_key`; that half of this entry stands.
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
to me. Those are Rulings 6–9, in their own section after Ruling 5. A **third pass** followed, minting
the last five stable IDs and reversing the scope of Ruling 6; those are Rulings 11–13, in their own
section after Ruling 10. Where a later ruling reverses an earlier choice, the earlier entry is left as
written and carries a **superseded-by** pointer — the record of what was decided when is part of the
point.

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
>
> **→ Superseded in part by Ruling 11, on the “no `id`” half only.** The FLAG's subject — where the
> file lives — is untouched: it stays exactly where it is, in `content/enemies/`, with the same stem
> (Ruling 10 already confirmed the path, and Ruling 11 does not revisit it). What Ruling 11 reverses
> is this entry's ruling that the file is “not a definition” and therefore needs no ID. It has one:
> **`ELT-01`**. **The reason is the bundle ordering.** `40:185` — “The canonical bundle is ordered by
> category and stable ID” — leaves no slot in that ordering for a file without a stable ID, so an
> ID-less file cannot be placed deterministically in the artifact every consumer reads. That is also
> what stops the kebab-case file name looking wrong now that the file has an ID: the ordering keys on
> the `id` field, and `40:185` requires the bundle to hash “identically for identical semantic input
> regardless of source file enumeration order”, so the stem is not load-bearing and no rename is
> owed. The FLAG text above is left verbatim, including the “not a definition” framing that Ruling 11
> reverses — the reasoning trail is the point, and the argument it makes about *placement* is still
> the live one.

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

**→ The last sentence is superseded in half by Ruling 12.** A boss *diameter* is authored, exactly as
written here, and stays. The boss *centre distance* is not: it is the same
`diameter ÷ 2 + 0.50 M` derivation, it reproduces exactly for all four bosses, and it has now been
removed from `content/bosses/` as well. This entry's scope — “the ruling named the enemy definitions”
— is what left the defect in place one catalog over.

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

### Integration-owner rulings applied — third pass

Two further items were ruled on after the second pass, plus one audit this pass ran on its own
initiative and fixed. As before, each entry gives the citations, exactly what changed, and separately
what the ruling left to me. Rulings 11 and 12 each reverse a scope decision recorded above, and both
earlier entries carry a **superseded-by** pointer rather than being rewritten.

**The deliberate-removal list grows by one.** The paragraph above Ruling 6 names five fields that are
absent from `content/` by decision rather than for want of a source. A sixth joins them:
`contact_footprint.center_distance_that_begins_contact_m` on the **four bosses** (Ruling 12). As with
the other five, a reviewer should not read it as missing data, and a later transcription pass that
finds the four values at `docs/72:105-110` must not re-add them.

#### Ruling 11 — the last five stable IDs are minted: `SITE-01`–`SITE-04` and `ELT-01`

**Citations.** `40:80` requires a stable category-valid `id` on every independently addressable
definition. `40:185` is what makes an ID-less file untenable rather than merely untidy: “The canonical
bundle is ordered by category and stable ID, uses normalized numeric formatting, includes
schema/generation versions, and hashes identically for identical semantic input regardless of source
file enumeration order.” There is no slot in that ordering for a file with no stable ID, so an ID-less
file cannot be deterministically placed in the artifact every consumer reads. `40:67` (“Reuse accepted
gameplay IDs exactly”) still forbids *inventing* IDs during transcription, which is why this pass did
not mint them and the integration owner did.

**Change — the four mining-site classes.** `"id": null` becomes a minted token, in the document order
of `docs/40-mining-and-extraction.md:58-132`, and each definition's `name_key` moves off the
provisional filename stem:

| File | `id` | `name_key` before | `name_key` after |
| --- | --- | --- | --- |
| `content/mining-sites/standard-ore-seams.json` | `SITE-01` | `mining_site.standard-ore-seams.name` | `mining_site.SITE-01.name` |
| `content/mining-sites/rich-ore-seams.json` | `SITE-02` | `mining_site.rich-ore-seams.name` | `mining_site.SITE-02.name` |
| `content/mining-sites/hyper-gold-sites.json` | `SITE-03` | `mining_site.hyper-gold-sites.name` | `mining_site.SITE-03.name` |
| `content/mining-sites/specialized-material-geodes.json` | `SITE-04` | `mining_site.specialized-material-geodes.name` | `mining_site.SITE-04.name` |

The four keys were renamed in place in `content/localization/en.json`, which stays flat, lexically
sorted, duplicate-free and orphan-free at 164 strings. **The four English values are unchanged** —
“Standard ore seams”, “Rich ore seams”, “Hyper Gold sites”, “Specialized-material geodes” — and no
string was added or removed. The key grammar `<category>.<stable_id>.<role>` is unchanged; only the
`<stable_id>` segment stopped being provisional. This discharges the “must be rewritten when IDs are
minted” obligation the Stable IDs section above recorded.

**Change — the shared elite modifiers.** `content/enemies/shared-elite-modifiers.json` gains
`"id": "ELT-01"` as the first envelope field, and becomes an ordinary addressable definition rather
than an ID-less constants block. Three things it does **not** gain, each for its own reason:

- **No `name_key`.** `name_key` is conditional on a definition having a genuinely player-facing name
  (`40:84` with `40:90`), and this one has none — it is a block of multipliers the UI never names.
  Having a stable ID and having a name are independent properties, so `ELT-01` stays in the verifier's
  `NAME_KEY_OMITTED` list beside `WAV-01` and `MGC-01`, and the list is unchanged at three members.
  Nothing was added to `en.json` for it.
- **No rename.** The file stays at `content/enemies/shared-elite-modifiers.json`, the path Ruling 10
  confirmed. The bundle orders by the `id` field and hashes independently of source file enumeration
  order (`40:185`), so the stem is not load-bearing; renaming would churn every `content/README.md`
  and verifier reference for no gain. The file's stem remains unsourced, as Rulings 3 and 10 noted.
- **No new `source_refs` entry.** No document assigns either `ELT-01` or `SITE-01`–`SITE-04`, so there
  is nothing to cite: a `id: <DOC>#<anchor>` prefix here would attribute a minted token to a document
  that does not contain it. (`UTL-R1` got such refs under Ruling 2 because `40:128` and `docs/68:31`
  genuinely argue for putting the radar in the utility catalog; nothing comparable exists here.)

**What the ruling did not determine, and I chose:**

1. **Which mining-site file gets which number.** The ruling supplied the five tokens and the mapping;
   the ordering rationale recorded above — document order in `docs/40:58-132` — is the reading I
   applied when writing them, and it matches the mapping given.
2. **Placing `id` first in the elite block's envelope**, ahead of `schema_version`, matching every
   other definition in the tree.

#### Ruling 12 — the boss centre distance is derived too, and comes out

**Citations.** `docs/72:86` states one derivation for every contact circle in the game: “Contact begins
when the enemy contact circle and the mech's 0.50M-radius collision circle overlap.” So the centre
distance is `contact diameter ÷ 2 + 0.50 M` for a boss exactly as for an enemy, and
`docs/technical/40-content-data-and-validation.md:114` assigns producing it to the compiler
(“Validation derives world speeds/footprints and compares them with the survivability report”).
Ruling 6 removed it from the ten enemies and stopped there, because the ruling it applied “named the
enemy definitions”. The defect it was removing did not stop there.

**The two halves of a boss footprint have opposite provenance, and the investigation established the
split before anything was deleted:**

- **Boss diameters are AUTHORED and stay.** The interval-boss overview table at
  `docs/31-initial-alien-roster.md:121-128` has **no body-scale column at all** — unlike the ordinary
  roster overview at `docs/31:37-48`, which is where the ten enemy `body_scale_multiplier` values come
  from. The scales the four boss diameters would imply against the `0.80 M` Ripper reference
  (`1.875×`, `2.5×`, `2.0×`, `2.375×`) appear **nowhere** in `docs/`; a search for them returns
  nothing. And `docs/72-player-survivability-and-damage-baseline.md:105` states the four diameters
  flat (rows at `:107-110`), in its own table introduced by “Bosses use simple circular gameplay footprints even when their
  meshes are elongated or irregular”. There is no operand to derive them from, so the diameter *is*
  the authored quantity for a boss — the role `body_scale_multiplier` plays for an enemy.
  `contact_footprint.contact_and_weapon_hurt_diameter_m` therefore **stays** on all four bosses.
- **Boss centre distances are DERIVED and come out.** Re-verified against the “Center distance that
  begins contact” column of the boss table at `docs/72:105-110` before deletion, arithmetic recomputed
  from the authored diameter rather than taken from the ruling's restatement:

| ID | Boss | Authored diameter (`docs/72:105`) | `diameter ÷ 2 + 0.50 M` | Removed stored value | Reproduces |
| --- | --- | ---: | ---: | ---: | --- |
| `BOSS-01` | Riftjaw | 1.50 M | 1.50 ÷ 2 + 0.50 = **1.25** | 1.25 M | exactly |
| `BOSS-02` | Brood Titan | 2.00 M | 2.00 ÷ 2 + 0.50 = **1.50** | 1.50 M | exactly |
| `BOSS-03` | Prism Crown | 1.60 M | 1.60 ÷ 2 + 0.50 = **1.30** | 1.30 M | exactly |
| `BOSS-04` | Skybreaker Apex | 1.90 M | 1.90 ÷ 2 + 0.50 = **1.45** | 1.45 M | exactly |

All four reproduce exactly — no rounding artefact of the C-1 kind anywhere in this column — so nothing
was lost that the compiler cannot reproduce from the surviving authored diameter and the player's
collision radius.

**Change.** `contact_footprint.center_distance_that_begins_contact_m` is **removed** from
`content/bosses/BOSS-01.json` … `BOSS-04.json`. Each `contact_footprint` keeps
`contact_and_weapon_hurt_diameter_m`, `shape`, `appendages_outside_footprint` and
`attack_geometry_uses_separate_display`, and its `contact_footprint:` `source_refs` prefix is retained
because those four surviving fields still come from `docs/72`. Nothing else in any boss file changed.

**Why this is the same defect A20 already existed to prevent.** The `0.50 M` term is the **player's**
collision radius. Storing the sum in `content/bosses/` hardcoded a player-baseline constant into the
boss catalog, so a change to the mech's collision radius would silently invalidate four boss files with
no validator to notice — two owners on one value, in two different catalogs. That is word for word the
argument Ruling 6 made about the ten enemies. A20 was simply under-scoped to enemies.

**Verifier.** `A20` in `src/MechaMiner.Tools/ContentImport/verify_content.py` is now **two rules with
two scopes**, not one rule over one directory:

- the contact-**diameter** rule stays `content/enemies/` **only**, because a boss diameter is authored.
  Widening it would fail the four authored diameters, and `ring_radius_m` on `BOSS-02`'s minion ring
  with them;
- the **centre-distance** rule covers `content/enemies/` **and** `content/bosses/`.

Both still match on key names, so a rename cannot slip past either, and `reference_diameter_m` stays
allowlisted for the reason Ruling 6 gave.

**What the ruling did not determine, and I chose:** leaving `contact_footprint` on the bosses as an
object with four remaining fields rather than flattening it, matching the choice Ruling 6 recorded for
the enemies.

#### Ruling 13 — `source_refs` scope prefixes are audited, and a dangling one is now a failure

Not an integration-owner ruling: an audit this pass ran and fixed, recorded here with the rest.

**The defect class.** A `source_refs` element may carry an optional `<json.path>: ` prefix attributing
a single property to a document (`40:87`, and the shape documented in `content/README.md`). A prefix
naming a field that does not exist in the definition is a **dangling citation** — it claims to
document something that is not there. It is the same defect class as an `#anchor` pointing at a heading
that does not exist, which `A9` has always failed on, and it had no check at all.

**The audit.** Every one of the **1,131** prefixed `source_refs` elements in `content/` was parsed and
resolved against its own definition. **50 did not resolve.** None of them turned out to be caused by
the removed fields this pass expected to find — the rulings that removed
`contact_diameter_m`, `center_distance_that_begins_contact_m`, `behavior_kind`,
`behavior_kind_registration_pending` and `exclusion_reason`, and the
`body_scale_factor` → `body_scale_multiplier` rename, all correctly updated or dropped their own
prefixes at the time. Every dangling prefix instead came from the provenance fold in commit `5becb39`,
which converted `_provenance` blocks into prefixed `source_refs` and, in five places, minted a prefix
that names something other than a field of the definition. Each fix below either follows the field or
drops the prefix; **no citation was deleted.**

| Prefix | Count | Where | Why it dangles | Fix and reason |
| --- | ---: | --- | --- | --- |
| `catalog_overview_row:` | 19 | 13 PowerUps, 6 unlocks | Never a JSON field. It names a **row of a table in the source document** — the `Catalog overview` table — not a property of the definition | **Prefix dropped, citation kept file-level.** The row is the source of `id`, `domain`/`category`, the per-rank or unlock effect, `cap`, `maximum_effect` and the cost — six or seven fields, not one, so no single-property prefix is correct. It supports the definition generally, which is exactly what a bare ref means. It is the only citation for several of those values and must not be deleted |
| `availability:` | 15 | the 15 `W-AB`/`W-AC`/`W-AD`/`W-AE`/`W-AF` branches | These 15 branch files have **no `availability` object**; the other 30 do. The prefix was applied uniformly to all 45 when the refs were folded, so on 15 of them it names a field that was never transcribed | **Re-pointed at `prerequisites:`.** The cited passage is the branch-availability rule — branches “appear immediately after the weapon is equipped and require no common-ore rank, weapon level, elapsed time, or boss prerequisite”. On these 15 files the surviving field that records that fact is `prerequisites: []`. The 30 files that *do* have an `availability` object keep their `availability:` prefix untouched |
| `discovery_sentence:` | 10 | all 10 relics | The field was **deleted**: once its line number was stripped the object held nothing but its string, so the string became the relic's `summary_key` and the wrapper went (see “Localization keys” above) | **Re-pointed at `summary_key:`.** The citation is the only support for the English summary now living in `en.json` as `relic.REL-nn.summary`, so dropping it would strip provenance from a value still shipped. `summary_key` is precisely the surviving field it documents |
| `corroboration:` | 1 | `UNL-01` | Never a JSON field. It is a **role word** describing what the citation does, not a path | **Prefix dropped, citation kept file-level.** `docs/68` independently restates the 600-Hyper-Gold suite *and* the same six unlocked utilities, so it corroborates two fields at once and cannot name one. It stays as a bare corroborating ref |
| `rules[2..3]:` | 5 | `UNL-02`…`UNL-06` | **Not dangling.** `rules` exists with four elements, so indices 2 and 3 both resolve; only the `[2..3]` **range** notation was unsupported by the first draft of the checker | **Content unchanged.** The range form is now part of the asserted path grammar. Rewriting it to `rules[]:` would have been a real loss of precision: the citation covers the two shared pool-behavior bullets, not all four |

**Change in `content/`.** 45 `source_refs` elements across 45 files: 15 branches, 13 PowerUps, 10
relics, 6 unlocks (`UNL-01` twice). **No element was removed and no document ID or `#anchor` was
altered** — only the prefix, and only in the direction of naming a field that exists. Every array
stays the same length; the `rules[2..3]:` files are untouched.

**Verifier.** New assertion **`A22`**: every `source_refs` scope prefix must resolve to a field that
exists in the definition it annotates. The path grammar it asserts is dot-separated `snake_case`
segments, each optionally suffixed with `[]` (every element), `[N]` (one element) or `[N..M]` (a
range), which is the union of the forms already in use — `rules[]`,
`minute_rows[33].formation_events[].timestamps_reconstructed`, `unlocks.utilities[].utility_id`,
`rules[2..3]`. An unindexed array is transparent, so `unlocks.utilities.utility_id` would resolve too.
The failure message names the surviving-field and drop-the-prefix options explicitly, and says not to
delete a citation that is the only support for a value still present.

**What no ruling determined, and I chose:** the five fixes above, one per row, with the reason in the
table. Two are judgement calls worth flagging for a reviewer: re-pointing the 15 branch refs at
`prerequisites` rather than adding the missing `availability` object to those 15 files (adding it would
be authoring content this pass has no ruling for), and keeping `catalog_overview_row:` /
`corroboration:` as **file-level** refs rather than picking one of the several fields each supports.

### Integration-owner rulings applied — fourth pass

One naming pass, driven by the schema stream's rulings on how a multiplicative scale and an upper bound
are spelled. Everything here is a **rename, a collapse of two names onto one value, or an omission**; no
number changed. The pass is recorded as Rulings 14–18 plus the audit that produced them, which is
preserved verbatim in the [pre-clear audit appendix](#appendix--pre-clear-audit-three-independent-checks)
at the end of this file.

#### Ruling 14 — a multiplicative scale is spelled `_multiplier`, and nothing else

**Citation.** `docs/technical/40-content-data-and-validation.md:26` ("Property names use `snake_case`")
with `:96` (the unit-suffix list) is the whole of the naming mandate; neither names a spelling for a
multiplicative scale, so the tree had drifted into four — `_multiplier` (43 names, 52 leaves),
`_scaling` (4 names), `_multiple_of_` (1 name), and a `_scale` grouping key over non-factors. The ruling
picks `_multiplier`.

**Changes.**

| File | Before | After | Why |
| --- | --- | --- | --- |
| `content/relics/REL-07.json` | `effects.explosion_area_scaling` | `effects.explosion_area_multiplier` | one spelling; the `area` half of the name is exempt, see Ruling 17 |
| `content/relics/REL-09.json` | `effects.mining_decay_multiple_of_current_forward_extraction_rate` | `effects.decay_rate_multiplier_of_forward_rate` | the same quantity is already `progress_decay.decay_rate_multiplier_of_forward_rate` in all four `content/mining-sites/*.json`, at the same value `4`. One concept, one name |

**Left alone deliberately:** `content/branches/W-BF-tethered-reaper.json :: effects.speed_scaling`. The
ruling asked whether it was null; it is not. It holds a prose curve — "linear with blade world speed
from stationary to one base mech full-speed" — which is a *shape*, not a scale, so `_multiplier` would
be the wrong name for it and no rename applies. It stays as authored.

#### Ruling 15 — three fields whose multiplicativity was inferred from prose are omitted, not renamed

**The ruling.** Inferring multiplicativity from prose in order to choose a field name is inventing a
semantic from a name, which `40:90` ("Unknown fields are errors") and the transcribe-don't-derive rule
both forbid. **A declared optional field whose semantics we guessed is worse than an absent one**, because
the guess survives into the schema as if a document had stated it. So a `_scaling` field whose value is
`null` is omitted entirely rather than renamed to `_multiplier`.

**Two fields are omitted from `content/relics/REL-07.json :: effects`:**

| Omitted field | What the prose actually says | Why no field was declared |
| --- | --- | --- |
| `explosion_strength_scaling` (was `null`) | `docs/69-initial-relic-catalog.md:130` — "Explosion strength and Area scale from that enemy's maximum Hull, subject to a cap for elites and bosses." | The sentence says explosion strength *scales from* maximum Hull. It does not say the relationship is multiplicative, nor give a coefficient, an exponent, or a curve. Naming the field `explosion_strength_multiplier` would assert a multiplication the document never states; naming it `_scaling` keeps a second spelling alive for a value that does not exist. `REL-07 :: rules[5]` records the same gap in the relic's own words: "Exact explosion scaling, generational decay, delay, and boss cap remain numerical tuning." |
| `elite_and_boss_scaling_cap` (was `null`) | the same line 130 — "subject to a cap for elites and bosses" — and `rules[5]`'s "boss cap remain numerical tuning" | The cap's *existence* is stated; its magnitude, and even whether it bounds a multiplier, an absolute damage figure, or a fraction of Hull, are not. The field would have had to declare both a bound spelling and the kind of quantity bounded, and the document supports neither. |

The surviving prose is not lost: `effects.explosion_scales_from = "the defeated enemy's maximum Hull"`
still carries the relationship as a string, `rules[1]` carries the full doc sentence, and `rules[5]`
carries the "remains numerical tuning" admission. **A later pass that finds real numbers for these two
must add them as `_multiplier` and `maximum_`-spelled fields, and must not resurrect the `_scaling`
names.** `effects.explosion_delay_seconds`, `effects.generational_decay` and
`effects.chain_generation_limit` stay as `null`: their names state a unit or a plain quantity and assert
nothing about a scale, so `null` there means "the document states no value", which is exactly what
`content/README.md` says a `null` means.

**One open tension a reviewer should see.** `effects.explosion_area_multiplier` is still `null` after
Ruling 14 renamed it, and it comes from the *same sentence* as `explosion_strength_scaling` — so by
Ruling 15's own reasoning it is also a guessed semantic and arguably belongs in the omitted column.
Ruling 14 named it explicitly for rename, and Ruling 15 enumerated three other fields, so the rename was
applied as instructed and the tension is recorded here rather than resolved locally. **This is an open
question for the integration owner: omit `explosion_area_multiplier` too, or keep it.**

#### Ruling 16 — an upper bound is spelled `maximum`, a lower bound `minimum`

**Citation.** `40:26` again for property-name form, and `40:94` for why the bound word cannot always take
the suffix slot: units live in key-name suffixes, so a name carrying `_percent` or `_seconds` must keep
that terminal.

**The ruling.** A cap *is* a maximum. `_cap`, `_max` and `_maximum` were three spellings of one concept —
in two files, two of them inside a single object — and `_min` sat beside `_minimum` the same way. One
spelling now: the word is spelled out, and **the qualifier rather than the noun carries the distinction
between two bounds on one quantity**, so `{target_min, target_max, hard_max}` becomes
`{target_minimum, target_maximum, hard_maximum}`. Where a unit suffix must stay terminal the bound word
moves to the front instead.

**Scope: 88 distinct property names, 194 leaf occurrences, across 57 files.** Every property name in
`content/**/*.json` whose underscore-delimited tokens included `cap`, `max` or `min` was rewritten,
including the bare `{min, max}` range objects (32 + 32 leaves) that `content/README.md` used to mandate —
that line now reads `{minimum, maximum}`. Representative cases, one per shape:

| Shape | Before | After |
| --- | --- | --- |
| bare range members | `target_time_to_kill_seconds.{min, max}` | `.{minimum, maximum}` |
| two bounds, one quantity | `visible_mining_opportunities_in_normal_view.{target_min, target_max, hard_max}` | `.{target_minimum, target_maximum, hard_maximum}` |
| unit suffix must stay terminal | `control_resistance_cap_percent` | `maximum_control_resistance_percent` |
| unit suffix must stay terminal | `pursuit_duration_seconds_max` | `maximum_pursuit_duration_seconds` |
| unit suffix must stay terminal | `stagger_cap_seconds` | `maximum_stagger_seconds` |
| no unit, suffix slot free | `raw_output_multiplier_max` | `raw_output_multiplier_maximum` |
| `cap` mid-name | `focus_cap_multiplier` | `focus_maximum_multiplier` |
| `cap` as the whole name | `cap` (13 PowerUp rank caps) | `maximum` |
| prefix form | `max_simultaneous_bosses`, `min_far_band_caches` | `maximum_simultaneous_bosses`, `minimum_far_band_caches` |

**One `source_refs` citation was re-pointed, not deleted:**
`content/enemies/shared-elite-modifiers.json` carried the scope prefix
`control_resistance_cap_percent: GDD-PLAYER-SURVIVABILITY-BASELINE#control-resistance-and-status-stacking`,
which A22 correctly reported as dangling the moment the field was renamed. It now reads
`maximum_control_resistance_percent: …`, same document, same anchor. No other citation in the tree names
a renamed field.

**`content/branches/W-BD-selective-detonators.json` needed no deletion.** `effects.damage_multiplier_cap`
and `favorable_scene_effect.magnitude.damage_multiplier_max` both held `2`, but they sit in *different*
objects, and restating an effect value inside `favorable_scene_effect.magnitude` is a shape every branch
file uses (compare `W-BC-broadside-oscillator.json` and `W-BF-tethered-reaper.json`). So both were spelled
`damage_multiplier_maximum` and both stayed; unifying the spelling was the whole defect.

**Superseded by [Ruling 28](#ruling-28--the-two-w-bf-tethered-reaper-bounds-are-two-bounds-both-stay-both-are-renamed):
the document owner confirmed these are two different bounds, both values stay, and both fields are now
renamed. The escalation below is kept verbatim as the record of why the rename waited.**

**`content/branches/W-BF-tethered-reaper.json` STOPS, and is escalated.** One object holds
`effects.contact_damage_speed_bonus_percent_max = {percent: 200}` and
`effects.contact_damage_percent_cap = {percent: 400}` — two spellings holding **different** values. That is
either two real bounds wearing two spellings or one bound duplicated with a wrong number, and the two need
opposite treatment: rename both, or delete one. Renaming them now would leave two identical stems holding
different numbers, which reads as a duplicate with a typo and hides the collision. **Both are left exactly
as authored** and declared in the verifier's `BOUND_SPELLING_ESCALATED`, so the exception is visible instead
of absorbed. **This needs a document owner's answer: are 200 and 400 two bounds, or one bound and a
mistake?**

#### Ruling 17 — `Area` the stat keeps its name; the geometry rule does not bind it

**Citation.** `40:98` — "Geometry dimensions distinguish radius, diameter, width, range, and area; `area`
is never used as a vague scalar name" — read against `docs/35-playable-mechs.md:65`, which lists "weapon
Area" in "the shared stat vocabulary", and `docs/36-initial-mech-catalog.md:137`, which defines its
membership: "scalable radii, widths, blast areas, projectile bodies, cones, and persistent damage zones
qualify".

**The ruling.** `content/relics/REL-04.json :: effects.weapon_area_multiplier` is **not renamed**. It
deliberately scales a *set* of dimensions, so forcing a specific dimension into the name would encode a
falsehood — `REL-04 :: rules[1]` restates `docs/69-initial-relic-catalog.md:99` verbatim ("weapon Area is
doubled"). `40:98` binds a field naming a measured dimension of a specific shape, not a field naming the
Area stat. `content/README.md`'s geometry bullet now records the exemption.

This is one of two rules in the planned validator set that **would have produced a wrong answer** if
applied mechanically; the other is Ruling 18. Both are called out in the audit appendix.

#### Ruling 18 — `obstacle_free_radius_in_mining_zone_diameters` is not touched

`content/maps/standard-map-generation-contract.json ::
deployment_and_opening_fairness.obstacle_free_radius_in_mining_zone_diameters = 1` names a **radius**
measured in **diameters**, and `docs/51-standard-map-generation-contract.md:70` ("obstacle-free space at
least one mining-zone diameter around the mech") does not say whether the clear envelope's radius or its
diameter equals one mining-zone diameter. That is a **factor-of-two design ambiguity**, not a naming
defect: renaming the field either way would silently pick one reading. The field and its flag stay
exactly as they are, escalated for a document correction.

#### Ruling 19 — REL-09's duplicate movement multipliers collapse to one field each

`content/relics/REL-09.json` held one value under two names, twice.

**Pair 1 — the Claim-Jumper factor, `1.5` under two names inside one file.**
`effects.enemy_movement_speed_multiplier_while_mining` and
`cross_document_rules[0].enemy_movement_multiplier` were the same number.
`cross_document_rules[0].enemy_movement_multiplier` is **removed**, and the longer name is kept, because
that is the name the source wording supports: `docs/72-player-survivability-and-damage-baseline.md:80` —
which is the sentence `cross_document_rules[0].rule` transcribes — says "Claim-Jumper Core multiplies it by
1.50 **only while mining progress advances**", and `docs/69-initial-relic-catalog.md:153` says "+50%
movement **speed** while extraction progress is actively advancing". The short name dropped both the
`speed` the docs name and the `while_mining` condition the rule text restates.

**Pair 2 — the elite movement factor, `1.1`, and a correction to the ruling's premise.** The ruling
described this as a pair internal to REL-09 between `movement_speed_multiplier` and
`multiplies_with_elite_movement_multiplier`. **REL-09 has no `movement_speed_multiplier` field**; the pair is
*cross-file*. `content/enemies/shared-elite-modifiers.json (ELT-01) :: movement_speed_multiplier = 1.1` is
the authored value — `docs/31-initial-alien-roster.md:107` states it as a multiplier in the shared elite
modifier table, "| Movement speed | 1.10× |" — and `REL-09 :: cross_document_rules[0].multiplies_with_elite_movement_multiplier = 1.1`
was a second writer on it, under a name that reads as a boolean predicate while holding a number. So the
collapse keeps ELT-01's field and **removes the copy in REL-09**. Nothing about the ordering is lost:
`cross_document_rules[1].enemy_movement_multiplier_order = [base, elite, resonance, Claim-Jumper Core]`
still states that the factors multiply and in what order, and `cross_document_rules[0].rule` still carries
the doc sentence in full.

**`cross_document_rules[0].multiplies_with_flux_amber_resonance_multiplier = 1.2` is left in place.** It is
the same *name* shape, but not the same defect: `1.20` is authored nowhere else in `content/` — no resource
definition carries it — so this field is the tree's only carrier of the Flux Amber movement factor from
`docs/72:80`, and removing it would delete a value rather than a duplicate. Flagged as the asymmetry it is.

**No `source_refs` citation was orphaned.** REL-09's four `cross_document_rules[]:` prefixes name the
array, which still exists with element 0 intact; no prefix named either removed field.

#### Ruling 20 — counts carry `_count`, not a bare `size`

`40:94` lists `_count` among the required suffixes. `rarity_and_weighting.fresh_profile_pool_size` and
`rarity_and_weighting.fully_unlocked_pool_size` are counts of relics in a pool (5 and 10), so across
`REL-01`–`REL-10` — 20 occurrences — they become `fresh_profile_pool_count` and
`fully_unlocked_pool_count`. Values unchanged.

`content/branches/W-AE-replicator-swarm.json :: expected_effect.maximum_total_squad_size_multiplier` keeps
`size`: it is a *multiplier on* a squad size, not a count, and no ruling covered it.

#### Ruling 21 — the factor-and-percentage twins are audited, and none is removed

`40:95` says the compiler writes the normalized factor into the runtime bundle "as a separate derived
field", which argues a hand-authored factor sitting beside its percentage is a second writer. The ruling
set two mandatory conditions: the factor must actually agree with the percentage, and the factor may be
removed **only where the design document states the value as a percentage**. Where the source states a
multiplier, the multiplier is what is authored and nothing is derived.

**Agreement check — all three pairs agree, so there is no data defect:**

| File | Percentage | Factor | Relationship | Agrees? |
| --- | --- | --- | --- | --- |
| `REL-04` | `primary_activation_frequency_reduction = {percent: 60}` and `primary_activation_frequency_of_final = {percent: 40}` | *none* | `60 + 40 = 100`; two percentages, no factor | yes |
| `REL-07` | `direct_damage_reduction = {percent: 35}` | `direct_and_persistent_weapon_damage_multiplier = 0.65` | `0.65 = 1 − 35/100` | yes |
| `REL-09` | `enemy_movement_speed_increase_while_mining = {percent: 50}` | `enemy_movement_speed_multiplier_while_mining = 1.5` | `1.5 = 1 + 50/100` | yes |

**Removal check — the doc line relied on, per relic. Nothing is removed:**

- **`REL-04`** — `docs/69-initial-relic-catalog.md:99`: "All weapon damage is multiplied by 2.5, weapon Area
  is doubled, and finite weapon-created durations are doubled." The document states *multipliers*, so
  `weapon_damage_multiplier = 2.5`, `weapon_area_multiplier = 2` and
  `finite_weapon_created_duration_multiplier = 2` are the authored form and stay. There is no
  factor-and-percentage pair here at all: the cadence numbers are two percentages, both doc-stated
  (`:98` "Each weapon produces primary activations at 40% of its otherwise final frequency", `:96`
  "Weapons attack 60% less often"), and `60` is the complement of `40` rather than a normalized factor —
  a different redundancy class, outside this ruling.
- **`REL-07`** — `docs/69-initial-relic-catalog.md:129`: "All direct and persistent damage attributed to
  equipped weapons is multiplied by 0.65." The normative rule line states the **multiplier**, so the
  multiplier is authored and is not removed. The percentage sibling is doc-stated too, at `:127`: "Weapons
  deal 35% less direct damage." Both forms appear in the source, so removing either would drop a
  doc-stated value.
- **`REL-09`** — `docs/69-initial-relic-catalog.md:153` states the **percentage**: "Every living enemy
  receives +50% movement speed while extraction progress is actively advancing." That alone would make
  `enemy_movement_speed_multiplier_while_mining = 1.5` the derived member and remove it. It is **not**
  removed, because a second accepted document states the multiplier directly —
  `docs/72-player-survivability-and-damage-baseline.md:80`: "Claim-Jumper Core multiplies it by 1.50 only
  while mining progress advances." Both forms are authored by documents this definition already cites, so
  the ruling's own test ("whichever the source states is authored") keeps both.

**What this leaves open.** Three definitions now carry a percentage and its factor side by side with doc
support for each. That is not a defect under this ruling, but it *will* collide with the compiler's derived
field when `DAT-006` lands, and the collision needs a decision then: either the compiler's derived factor is
suppressed where an authored one exists, or the authored factor is dropped and the doc line that states it
is treated as prose. **Not decided here.**

#### Ruling 22 — two confirmed findings stay out until a citation exists

Neither is a naming matter; both are recorded so a reviewer does not read them as oversights.

- **The extraction-zone and resonance-field radii (3.0 M / 6.0 M) are not added.** The values are confirmed,
  but `source_refs` has nothing real to point at until a decision record exists, and `40:87` wants
  "gameplay document IDs/anchors and decision IDs implemented". An uncitable value is not transcribed.
- **Minute 33's `timestamps_reconstructed`, `timestamp_provenance` and `reconstruction_basis` markers stay,
  and the preserved malformed token stays.** They record that the row was reconstructed rather than read.
  The doc correction that would retire them has not landed; stripping the markers first would erase the
  only evidence that the row is not a straight transcription.

#### Corrected transcription errors — the geode resonance directions, audited and found correct

Distinct from the design-source contradictions in section 1 of this file: those are the documents
disagreeing with themselves, whereas this heading is for **our own** transcription mistakes, found and
fixed. The distinction matters for judging how far to trust the rest of the tree.

One was reported and investigated this pass, and **it is not a defect**. The report was that
`content/resources/F.json` (Flux Amber) carried `resonance_behavior.modifier.direction = "decrease"` while
`docs/40-mining-and-extraction.md:109` states the opposite. The doc line is:

> | Flux Amber | **Overclocked Motion:** enemy movement speed is 20% higher |

`F.json` already carries `"direction": "increase"`, matching it. All six were then checked against
`docs/40-mining-and-extraction.md:104-109` in full, and all six are correct:

| Resource | Geode | Doc wording (`docs/40:104-109`) | `direction` | Correct? |
| --- | --- | --- | --- | --- |
| `A` | Asterite | "outgoing enemy damage is 20% **higher**" | `increase` | yes |
| `B` | Barysteel | "enemies take 20% **less** damage" | `decrease` | yes |
| `C` | Cinderglass | "enemy projectile damage is 20% **higher**" | `increase` | yes |
| `D` | Driftmetal | "displacement magnitude and control-effect duration are 20% **lower**" | `decrease` | yes |
| `E` | Eidolon Coral | "enemy attack cadence is 20% **faster**" | `increase` | yes |
| `F` | Flux Amber | "enemy movement speed is 20% **higher**" | `increase` | yes |

`git log` confirms `F.json` has never held `"decrease"` in any commit, and neither did the deleted
`resources/geode-resonance-effects.json` aggregate it was decomposed from (Ruling 4) — the only two
`"decrease"` values in that file were Barysteel's and Driftmetal's, both correct. **No value was changed,
and no value-level defect exists in the six resonance directions.** The heading is kept for the next real
one.

### Integration-owner rulings applied — fifth pass

An adversarial review of the pull request preparing this content ahead of `DAT-007`. Six items were
raised; **three of the six were
defects in the *assertions*, not in the data** — a guard that read as covering something and did not.
The pass is recorded as Rulings 23–27. No authored number changed. One review instruction was
**declined on evidence** and is recorded as Ruling 23 so the refusal is auditable.

#### Ruling 23 — the four boss contact diameters are AUTHORED and were not deleted

**The instruction.** Delete eight "derived footprint" fields from `content/bosses/` — the four
`contact_footprint.center_distance_that_begins_contact_m` values *and* the four
`contact_footprint.contact_and_weapon_hurt_diameter_m` values — on the ground that both are
`diameter ÷ 2 + 0.50`.

**Finding 1: four of the eight no longer exist.** The four centre distances were removed in commit
`4e12659`, and the A20 centre-distance rule was widened to `content/bosses/` in the same commit. The
review ran against a tree at or before `21f1734`. Nothing to do.

**Finding 2: the other four are not derived, and the arithmetic in the instruction cannot apply to
them.** `diameter ÷ 2 + 0.50` is the *centre distance*; a diameter cannot equal a function of itself.
A third independent attempt to refute the authored finding also failed:

| Refutation attempt | Result |
| --- | --- |
| Is there a boss body-scale column? | No. `docs/31:121-128` carries ID, Boss, Arrival, Initial Hull, Move, Contact, Control resistance, Defining behavior. The ordinary roster at `docs/31:37-48` *does* carry a `Body` column, which is where the ten enemy scales come from |
| Does the derivation sentence cover bosses? | No. `docs/72:86` reads "Every **ordinary** body scale in the alien roster multiplies that diameter" — the qualifier is in the source |
| Do the implied scales (1.875, 2.5, 2.0, 2.375) appear anywhere in `docs/`? | No. `grep` over all of `docs/` returns nothing for any of the four |
| Is the elite `1.25×` body scale (`docs/31:109`) an operand? | No. It scales "an enhanced instance of one of the nine pure pursuers"; bosses are explicitly separate |
| Are the diameters stated flat? | Yes. `docs/72:105-110` states Riftjaw 1.50M, Brood Titan 2.00M, Prism Crown 1.60M, Skybreaker Apex 1.90M |

**Ruling.** The boss diameter is the authored quantity, exactly as `body_scale_multiplier` is for an
ordinary enemy. It stays. Deleting it would destroy the only statement of a boss footprint in the tree
in order to satisfy a review instruction. **No boss file was modified this pass.**

#### Ruling 24 — the health pack's collection centre distance is removed (the same defect, third writer)

**Change.** `destructible_rock_rules.health_pack.collection_center_distance_with_standard_mech_circle_m`
is **removed** from `content/maps/standard-map-generation-contract.json`.

**Why.** It held `0.75`, and `docs/72:185` gives it as a consequence, not an operand: "The pack has a
0.25M pickup radius. With the standard mech circle, collection occurs when centers come within 0.75M."
`0.25 + 0.50 = 0.75`, where `0.50` is the *player's* collision radius (`docs/72:86`). The authored
operand `pickup_radius_m = 0.25` stays. This is the third writer for the same player-baseline constant,
after the ten enemies (Ruling 12's precursor) and the four bosses (`4e12659`): change the mech's
collision radius and this file is silently wrong with no validator to notice. A20's centre-distance rule
now covers `content/maps/` as well as `content/enemies/` and `content/bosses/`.

#### Ruling 25 — a `path:line` citation is not a domain value (13 occurrences)

`source_refs` was cleaned in an earlier pass, but the unstable citations had moved next door into
domain fields, where no assertion looked. Line numbers are unstable wherever they hide.

| Where | Before | After |
| --- | --- | --- |
| 11 × `content/utilities/UTL-*.json :: effect.stacking_classification` | `"… authoritative. (docs/68-utility-catalog.md:253)"` | `"… authoritative."`, with `effect.stacking_classification: GDD-UTILITY-CATALOG#modifier-and-timing-rules` added to that file's `source_refs` |
| `content/mining-sites/hyper-gold-sites.json :: beacon_response_source` | `"docs/32-standard-wave-and-beacon-schedule.md#hyper-gold-threat-beacon-response"` | `"GDD-STANDARD-WAVE-SCHEDULE#hyper-gold-threat-beacon-response"`, with the same reference added under a `beacon_response_source:` scope prefix |
| `content/weapons/stat-price-formula.json :: price_curve_decision.note` | `"The shared common-ore price curve is fixed globally by DEC-085 (docs/weapons/README.md:48)."` | key removed under Ruling 26; the surviving `price_curve_decision.id = "DEC-085"` is the whole of the fact |

Two corrections to the review's own framing, both verified before writing:

- The claim that `hyper-gold-sites.json` "already uses the stable form correctly in its own
  `source_refs`" was **false** — that array held only `GDD-MINING#hyper-gold-sites`. The stable form is
  nonetheless correct and was verified independently: `docs/32` declares
  `doc_id: GDD-STANDARD-WAVE-SCHEDULE` and carries `## Hyper Gold threat-beacon response` at `:94`,
  whose slug is `hyper-gold-threat-beacon-response`. A9 and A22 now both resolve it.
- `docs/68:253` and `:255` both sit under `## Modifier and timing rules` (`docs/68:251`), so all eleven
  utilities cite one anchor.

**New assertion (A24).** No string value anywhere under `content/` may match `docs/.*\.md`.

#### Ruling 26 — the singular `note` was not on the blocklist

The A8 blocklist forbade `notes` and missed `note`. Three singular keys survived, two of them spelling
out a derived movement speed in prose — the same category deleted from all ten enemies in an earlier
pass. The keys are removed and `note` is added to `FORBIDDEN_KEYS`. Text is reproduced verbatim:

| Former path | Note text |
| --- | --- |
| `content/mechs/MCH-06.json :: cross_doc_notes[0].note` | "Razorback with its +10% trait moves at 3.30M/s." |
| `content/mechs/MCH-06.json :: cross_doc_notes[1].note` | "Razorback with both maximum Servo Overdrive and Rank-3 Vector Thrusters moves at 4.05M/s: `3.0 × (1 + 0.10 + 0.10 + 0.15)`." |
| `content/weapons/stat-price-formula.json :: price_curve_decision.note` | "The shared common-ore price curve is fixed globally by DEC-085 (docs/weapons/README.md:48)." |

The two `movement_speed_m_per_s` values **stay**: `docs/72:55` states 3.30M/s and `docs/72:57` states
4.05M/s flat, so they are transcribed, not derived here. Only the prose restating them is gone, and
`cross_doc_notes[]: GDD-PLAYER-SURVIVABILITY-BASELINE#movement-and-speed-modifiers` still carries the
citation. The third note's `docs/weapons/README.md:48` pointer is worth recording as unresolvable —
there is no `docs/weapons/` directory in this repo — which is precisely why a `path:line` string in a
domain field is a defect rather than a convenience.

#### Ruling 27 — three assertions were rewritten because they did not check what they cited

This is the pass's most important output. In each case the guard was green, and green meant nothing.

**A16 — checked prose, not the numeric rule it cited.** A16 cited `40:95` ("Percentages in authoring
use human-readable percentage points only when the property name says `_percent`; the compiler writes
normalized factors into the runtime bundle as a separate derived field") but ran only on *string* values
and matched a literal `%` glyph. A numeric `25` under a non-`_percent` name was not even warned, while
131 English sentences containing a percent sign were. A warning list a reader learns to ignore is worse
than none. A16 is now **four** rules on **numbers and key names**, and a FAILURE rather than a warning
(none of the four needs `content/schemas/`):

1. a percent-named property resolves to at least one numeric leaf, so a percentage may not live only in
   prose under a name that promises a number;
2. a percentage-point magnitude is never a normalized factor — no percent-named numeric leaf (including
   its `minimum`/`maximum`/`percent` container leaves) may satisfy `0 < |v| < 1`;
3. the compiler's normalized factor is never authored — no property name combines a percent token with a
   `factor`/`multiplier`/`fraction`/`normalized` token, and no object holds both `<stem>_percent` and a
   same-stem factor sibling;
4. no **number** sits under a relative-magnitude name that says neither percent nor any unit-or-kind
   token.

**Rule 4 is the one this ruling nearly shipped without, and the reason it exists is the ruling's own
finding turned on itself.** Rules 1–3 each *begin* by asking whether the name says percent, so in the
revision this ruling describes every A16 rule sat behind `if not says_percent: … continue` and a bare
number under a non-percent name was never examined at all — while the docstring advertised the rewrite
as fixing exactly that case. `sneaky_bonus: 25` and `damage_bonus: 150` both passed with zero failures.
That is the same defect as the one this ruling opens with: a guard that was green because it did not
look, described as though it had.

**What rule 4 claims, and what it does not.** It is a **closed vocabulary**, not a judgement about
names in general. The relative-magnitude segments are exactly `bonus`, `bonuses`, `penalty`,
`penalties`, `increase`, `increased`, `decrease`, `reduction`, `boost`, `malus`, `discount`,
`surcharge`, `uplift`; a number under one of those, where the name carries no percent token and no
unit-or-kind token, is a failure. It does **not** claim to detect a percentage arriving under any name
that hides its unit — whether an arbitrary number "is a percentage" is not decidable from the number,
and a percentage stored as `sneaky_value: 25` sails past. What is decidable is that a *relative*
magnitude is necessarily proportional to something else, so it is either percentage points or a
multiplicative scale, and `40:95` (percentage points say `_percent`) and `40:94` (an ambiguous numeric
name carries a unit suffix) both require the name to say which. A unit-or-kind token **anywhere** in
the name excludes it, not merely terminally: `single_target_ceiling_multiplier_at_full_bonus` is the
tree's one such name — head noun `multiplier`, `bonus` a mid-name qualifier, and Ruling 14 makes
`_multiplier` the unit declaration for a multiplicative scale. A terminal-token rule would have flagged
it wrongly.

**Rule 4 flags nothing in this tree**, so its evidence is its negative control rather than a count. Two
injections on `content/enemies/EN-01.json`, each run and reverted individually: `sneaky_bonus: 25` →
FAIL, `1 numeric value(s) sit under a relative-magnitude name … ['content/enemies/EN-01.json.sneaky_bonus = 25']`;
`damage_bonus: 150` → FAIL, the same message reporting `['content/enemies/EN-01.json.damage_bonus = 150']`.
Nothing beyond those two forms is claimed.

**The 52 names that are *not* violations.** `percent_of_mech_base_speed`,
`shockwave_damage_percent_of_current_damage` and 50 others put the percent token mid-name. `40:95`
requires that the name *says* `_percent`, not that it *ends* in it, and `40:96`'s terminal-unit rule is
about unit suffixes. A rule demanding a terminal `_percent` would have condemned all 52 and forced a
rename that no document asks for — the same trap Checks 2 and 3 of the pre-clear audit describe.

**A13's world-prop probe — vacuous.** It counted *patterns that matched at least once*, so
`expected = 2` was satisfied by the existence of one key containing `rock` and one containing
`health_pack`. Emptying both objects and setting Hull to `1` and the footprint to `9.9` left it green.
It is replaced by four value assertions, each carrying its own citation:

| Assertion | Value | Citation |
| --- | --- | --- |
| destructible rock Hull | `100` | `docs/72:194` |
| destructible rock damage footprint diameter | `0.80` M | `docs/72:196` |
| health pack repair | `25` Hull | `docs/72:182` |
| health pack pickup radius | `0.25` M | `docs/72:185` |

All four verified against the document before the assertion was written, and all four reproduce.

**A25 (new) — polarity agreement, the automation of a hand check.** Ruling 22's Flux Amber
investigation had to verify six `resonance_behavior.modifier.direction` values by reading
`docs/40:104-109` by eye. Nothing stopped a seventh from being wrong. A25 draws a closed polarity
vocabulary of opposed pairs — higher/lower, increase/decrease, more/less, faster/slower,
shorter/longer, raise/reduce, gain/lose — and fails when a structured polarity value contradicts the
polarity words in the prose beside it. It fires on strict contradiction only: prose carrying words of
both signs ("20% faster without increasing movement speed") is not a contradiction. Its value does not
depend on catching anything today; it catches the *next* one.

**Also corrected in the pull-request body, not the tree:** the body claimed A20 "fails the build if
either field reappears under any name". It does not — A20 matches specific key-name patterns in
specific directories, and a value injected under an unmatched name passes. The body now says what A20
does. `src/MechaMiner.Tools/ContentImport/README.md` already described it accurately.

#### Ruling 28 — the two `W-BF-tethered-reaper` bounds are two bounds; both stay, both are renamed

**The escalation is resolved, not suppressed.** Ruling 16 stopped on
`content/branches/W-BF-tethered-reaper.json` because one object held
`effects.contact_damage_speed_bonus_percent_max = {percent: 200}` and
`effects.contact_damage_percent_cap = {percent: 400}` — two spellings of a bound holding *different*
values, which could have been two bounds or one bound plus a transcription mistake. Renaming without
knowing which would have destroyed a value. The document owner has now answered, and the sentence was
re-read here before writing anything. `docs/71-initial-weapon-numeric-catalog.md:346`:

> The four cutters combine into one blade with 200% current cutter radius. Its contact Damage is
> `200% + up to 200%` of current Damage, scaling linearly with blade world speed from stationary to one
> base mech full-speed and **capped at 400%**.

So `200` bounds the speed-bonus **component** — the "up to 200%" addend — and `400` bounds the
**total**, which is the 200% base plus the 200% maximum bonus. Two different bounds on two different
quantities. Nothing is redundant and no value changed.

**Changes.** Both are renamed under Ruling 16's spelling, with the qualifier rather than the noun
carrying the distinction:

| Before | After |
| --- | --- |
| `effects.contact_damage_speed_bonus_percent_max` | `effects.maximum_speed_bonus_percent` |
| `effects.contact_damage_percent_cap` | `effects.maximum_total_contact_damage_percent` |

`BOUND_SPELLING_ESCALATED` is now **empty**, and it is still asserted for drift. A resolved escalation
left in an exception list is worse than no list, which is the same failure mode as the 21 percent-sign
warnings Ruling 27 removed. Zero `_cap`, `_max` or `_min` bound suffixes remain anywhere under
`content/`.

**A prose over-claim, recorded rather than rewritten.** Commit `75310ed`'s message describes this exact
object — "an upper bound as `_cap`, `_max` or `_maximum`, twice within a single object" — as part of what
that commit normalized. `git show 75310ed -- content/branches/W-BF-tethered-reaper.json` is empty: the
file was deliberately skipped and escalated, which was the right call, and the message described it as
done anyway. The commit is pushed and history is not being rewritten; the over-claim is recorded here and
in this pass's commit message instead. Together with the A20 over-claim in the pull-request body, that is
**twice on this branch that prose asserted work the code did not do**, both in the same direction: the
claim was written from the intent rather than from the diff. Every claim in a commit message or body on
this branch is now checked against the actual diff before it is written.

### Integration-owner rulings applied — sixth pass

#### Ruling 29 — `null` is never legal in a source definition

**The ruling.** A `null` in a source definition is never legal. `content/README.md` used to define a
`null` as "the document states no value", which made absence expressible two ways — an omitted key and
a nulled key — for one meaning. `docs/technical/40-content-data-and-validation.md:90` settles it:
"Optional fields have explicit defaults materialized into the canonical bundle so runtime never
guesses." An optional field that is absent gets its default; an optional field that is present and
`null` asks runtime to guess, which is the thing that line forbids. So absence is spelled by omitting
the key, and `null` is spelled nowhere.

**The inventory, re-derived rather than inherited.** Walking every `*.json` under `content/` and
counting `null` leaves at any depth: **275 nulls across 101 of the 138 definition files**
(`content/localization/en.json` holds none, and the two Markdown files are not definitions). Every
figure below was recomputed from the tree in this pass, not carried forward.

| Bucket | Nulls | Disposition |
| --- | ---: | --- |
| (a) pure absence — no document states a value, and the per-definition note already says so | 178 | key omitted |
| (b) the document states a value exists but supplies no number, or states it varies per instance | 52 | key omitted; the ten genuine gaps are recorded below first |
| (c) fields no schema will declare — relic rarity/weighting, boss Armor | 24 | **field removed**, not converted |
| (d) shape defects — a scalar slot that can never hold a scalar | 3 | **key removed**, shape defect recorded |
| (e) nested `id` on two non-addressable objects | 2 | **key removed** |
| (f) deliberate deferrals and pending citations | 16 | key omitted; see the two subsections below |
| Total | 275 | no `null` remains anywhere under `content/` |

**Order.** The gap entries below were written **before** any key was omitted. A `null` plus a
per-definition note is a two-part record of a hole in the design spec; converting the `null` away
first would leave the note without the thing it annotates, and for ten of them there was no note
either. That is why this subsection precedes the conversion.

#### Value gaps recorded before conversion

These are holes in the **design specification**, not transcription defects: a document requires or
implies a value and supplies none. They are recorded here because the `null` that used to mark them is
gone. Grouped by gap, with the affected definitions listed inside; a field appearing on eight
definitions for one reason is one gap, not eight.

**Gap 1 — boss ability geometry is stated as a shape and never dimensioned (3 definitions).**
`docs/31-initial-alien-roster.md` describes each boss ability's affected area in words and gives no
extent for any of them. The three share one cause, so they are one gap:

- `BOSS-01 :: ability.lane_width_m`. Being transcribed: the width of Riftjaw's charge lane. Expected: a
  width in metres, because the ability's hit test is an area and the roster gives every other parameter
  of the ability numerically — 8 s cadence, 1 s telegraph, 1.5 s charge, 180% / 5.40 M/s, 27 damage.
  What the document actually says: `docs/31:134` — "displays a **wide** straight charge lane toward the
  mech's sampled position". "Wide" is the only extent given, and `docs/31:136`'s "before covering one
  body length" dimensions the lane's *length* test, not its width.
- `BOSS-02 :: ability.ring_radius_m`. Being transcribed: the radius of the Skitterling ring Brood Titan
  sheds. Expected: a radius in metres, since the sibling `ring_opening_degrees` is authored as 90 and a
  ring needs both an angular opening and a radius to be placed. What the document actually says:
  `docs/31:143` — "releases 16 Skitterlings in an incomplete ring **just outside its body**". The
  placement is given relative to the body with no offset, and the boss's 2.00 M contact diameter is the
  only nearby length. Deriving a radius from it would author a number `docs/31` does not state.
- `BOSS-04 :: ability.marker_diameter_m`. Being transcribed: the diameter of Skybreaker Apex's locked
  landing marker. Expected: a diameter in metres — the sibling `marker_shape` is authored as "Circle"
  and landing damage of 35 applies "inside the circle", so the circle's size decides the ability's
  entire threat area. What the document actually says: `docs/31:162` — "marks a **circular area**
  centered on the mech's sampled position" — and `docs/31:163` — "Landing deals 35 damage inside the
  circle." Neither gives a size, and the boss circles `docs/31:35` defers to
  `docs/72-player-survivability-and-damage-baseline.md#collision-and-contact-footprints` are the
  bosses' own contact diameters, not their ability markers.

**Gap 2 — `REL-06 :: effects.clustering_distance_m`.** Being transcribed: the distance within which two
living enemies count as clustered, which gates a +50% weapon damage bonus. Expected: a distance in
metres; it is the sole spatial condition of the relic's headline effect, its sibling
`minimum_other_living_enemies_for_clustering` is authored as 1, and the damage figure itself is authored
as 50 percentage points. What the document actually says: `docs/69-initial-relic-catalog.md:121` — "An
enemy within the **displayed** clustering distance of at least one other living enemy takes 50% more
weapon damage." The distance is described as displayed to the player and is never stated, and unlike
`REL-08` below the section does not say the value remains tuning — so the relic's central threshold is
simply missing.

**Gap 3 — `UTL-R1 :: availability.coverage_role`.** Being transcribed: the resource radar's
coverage-role label. Expected: one of the role labels the utility catalog assigns every other
fresh-profile utility, because the radar is a fresh-profile utility — `docs/50:106` and
`docs/68:272` both have it offered in every profile. What the document actually says: the "Coverage
role" column at `docs/68:265-272` has exactly six rows, one per material (Direct offense, Mining speed,
Weapon tempo, Mobility, Recovery, Economy), and the radar is not one of them; `docs/68:272` mentions it
only as "plus the resource radar", outside the table. **What existed before this entry, precisely.** `content/README.md` listed this
field's *path* among ten it named as gaps, but nowhere in the repository recorded the *substance* — what
was being transcribed, what value was expected and why, or what the document says instead — and
`content/transcription-notes.md` did not mention `coverage_role` for the radar in any spelling. A path in
a list is not a record of the gap; the same is true of Gaps 1 and 2. The six Advanced-Utility-Suite
utilities that omit the same field for the same reason
(`UTL-A2`, `UTL-B1`, `UTL-C2`, `UTL-D2`, `UTL-E2`, `UTL-F2`) each already carry a per-definition note
saying so — the radar's note block records `primaryRole` and `installedToRank3` as gaps and never
mentioned `coverageRole`, so converting its `null` without this entry would have destroyed the only
trace.

**Five more, recorded briefly because a sibling field preserves the fact.** These are gaps in the same
sense, but nothing is lost by the conversion: each nulled numeric has a sibling in the same object that
carries the document's own words, so the hole stays visible in the data.

- `BOSS-03 :: ability.projectile.lifetime_seconds` and `EN-06 ::
  specialist_attack.projectile.lifetime_seconds`. Both retain a sibling `lifetime_description` holding
  the document's qualitative extent — "disappears after crossing slightly more than one screen width or
  hitting solid terrain" and "carries it slightly beyond one screen width". A screen width is not a
  world distance, so no duration is derivable; the prose says exactly that.
- `REL-08 :: effects.positional_tolerance_m`, `effects.heat_build_rate_per_second`,
  `effects.heat_vent_rate_per_second`. All three are named as unquantified by the relic's own
  `rules[5]`, transcribed verbatim: "Exact positional tolerance and heating and venting rates remain
  tuning." `rules[0]` additionally carries the qualitative behavior ("Remaining within a small
  tolerance around the same world position builds heat; sustained travel outside that tolerance vents
  it"). The document declares these tuning values rather than omitting them by accident.

**Four gap families found by re-derivation, with no record anywhere.** The brief named five unrecorded
gaps; re-deriving the inventory rather than trusting it surfaced four more field families whose name
appears nowhere in this file, in any spelling. They are recorded here for the same reason as Gap 3 — the
`null` was the only trace — and briefly, because none of them is a missing *number* whose magnitude a
reader would need:

- `live_state_meter` on eight relics (`REL-01`–`REL-07`, `REL-09`). `docs/technical/40:132`'s relic
  field list includes "live-state meter", and no relic section in `docs/69` describes one for these
  eight. One gap: the field list requires a meter description the catalog never writes.
- `pause_behavior` on six relics (`REL-01`–`REL-04`, `REL-06`, `REL-07`). `REL-05` and `REL-08`–`REL-10`
  do state pause behavior; these six sections say nothing about what happens across a
  full-simulation pause. One gap, six pointers.
- `first_playable_subset.temporary_substitute` on six enemies (`EN-01`–`EN-04`, `EN-06`, `EN-07`). The
  sibling `included` is authored `true` on all six, so no substitute is needed and the field is
  vestigial rather than missing — recorded so it is not later read as a hole.
- `resonance_behavior.edge_case_rule` on three resources (`A`, `E`, `F`). `docs/40:110-116` states
  edge-case rules for Cinderglass, Barysteel and Driftmetal and states none for Asterite, Eidolon Coral
  or Flux Amber. One gap: three of the six resonance behaviors have no stated edge case.

**Not gaps, and deliberately not written up as such.** 22 nulls sit under fields whose document says
the value varies per instance or is decided at generation time, and two more (`presentation` on the
destructible rock and the health pack) sit under `docs/51-standard-map-generation-contract.md:156`'s
"Audiovisual treatment remains production work". A specification that deliberately defers a value is a
specification, not a hole; writing register-style entries for them would dilute the gaps above. Their
keys are simply omitted.

#### Removals, not conversions — 29 keys leave the tree

Data leaving the tree must not be described as a conversion. These fields were deleted, not omitted:

**(c1) The 20 relic rarity-and-weighting fields.** `rarity_and_weighting.rarity_tier` and
`rarity_and_weighting.cache_selection_weight` on all ten of `REL-01`–`REL-10`. **Ruled: these are fields
that should not exist.** No design document mentions relic rarity or relic weighting anywhere;
`docs/technical/40-content-data-and-validation.md:132`'s relic field list — pool availability/unlock,
discovery sentence key, sale value, behavior registration, benefit/tradeoff parameters, hook points,
affected weapon categories, live-state meter, presentation — omits both; and `DEC-127` fixes cache
selection by drawing without replacement from the unlocked pool rather than by weight, so a per-relic
weight has nothing to weight. **Measured before acting: no relic's `rarity_and_weighting` object is
emptied by the removal.** All ten hold the same five surviving populated fields — `selection_model`,
`fresh_profile_pool_count`, `fully_unlocked_pool_count`, `in_fresh_profile_pool`,
`guaranteed_to_appear` — so in every one of the ten only the two keys are removed and the object stays.
The whole-object removal contemplated for the empty case therefore applies to nothing, and each relic's
three `rarity_and_weighting:` scoped `source_refs` prefixes still resolve, so no citation had to move
(Ruling 13, `A22`).

**(c2) Boss `armor`, on all four bosses.** **Ruled: the field should not exist on a boss.**
`docs/technical/40-content-data-and-validation.md:114`'s enemies-and-bosses field list — Hull, movement,
contact damage/diameter/cadence, control resistance, behavior registration, projectile or boss-ability
parameters, elite eligibility, presentation, spawn classification, telemetry tags — omits Armor
entirely, while `:110`'s mech list includes it. Armor is a mech stat. This is the same class as (c1): an
invented field, not a missing value.

**Reported, not acted on at the time: the ten enemies also carry `armor`.** Every one of
`EN-01`–`EN-10` holds a top-level `armor: 0` — a value, not a null, so it is outside Ruling 29's scope,
and the `40:114` argument above would apply to it identically. The 0 comes from one prose sentence at
`docs/31:25` ("Ordinary enemies have no Armor"), not from a roster column. The ruling named bosses only,
so the ten enemy fields were **left exactly as authored** and referred back to the integration owner
rather than removed under an extended reading of a ruling that did not mention them.
**SUPERSEDED by Ruling 32** — the integration owner has now ruled on the ten directly, and they are
removed. The referral is what produced the ruling; the ten were not removed by analogy.

**(d) Three `external_numerics[n].value` shape defects.** `UTL-A1 :: external_numerics[1].value`,
`UTL-C1 :: external_numerics[1].value`, `UTL-R1 :: external_numerics[6].value`. In each, the sibling
`statement` and `quote` carry prose or several numbers at once — `UTL-A1`'s reference-build line states
+8% by minute 14, +16% by minute 21 and +20% by minute 28 in one sentence — so a single scalar slot can
never be filled, whatever value were chosen. The `null` is removed because `null` is illegal. **The
surrounding shape is deliberately left alone.** Whether `value` should become an array, whether the
prose should be the sole carrier, or whether the entry should split into three is a shape question about
a field name this transcription invented, and deciding it now would guess what `content/schemas/` will
pick. Recorded here as a **shape defect for the schema-reconciliation pass**, alongside the other
defects that need the schemas to land.

**(e) Two nested `id` keys removed.** `content/maps/standard-map-generation-contract.json` under
`destructible_rock_rules.destructible_rock` and `.health_pack`. **Ruled: the key should not exist, and
no ID is to be minted.** Both are nested objects inside `MGC-01`, which already carries a stable ID, and
neither is independently addressable — they are reached through `MGC-01` plus a JSON pointer, and nothing
in the tree references a rock rule or a pack rule by ID. They are parameters of the map contract, not
definitions. Minting IDs would create addressable entities the canonical bundle has no reason to
address; if presentation later needs to bind visuals to them, that is `presentation_id`'s job, not
`id`'s. The earlier plan to declare them as tolerated exceptions in the verifier is superseded: with the
keys gone, **no `null` remains anywhere under `content/` and the new A26 assertion needs no exception
set at all**, which is a stronger assertion than one carrying two permanent exemptions.

#### Deliberate deferrals and pending citations

- **`landmark_pools` (`MGC-01`)** — key omitted, and **no gap entry written**. It is already tracked as
  `OQ-008` at `docs/open-questions.md:42`; duplicating a live open question into a gap register would
  create two places to resolve one thing.
- **`extraction_zone_radius_m` (4 mining-site classes) and `resonance_field.radius_m`
  (specialized-material geodes)** — keys omitted, and these are **not gaps**: `DEC-128` sets them at
  3.0 M and 6.0 M respectively, so a document does now supply the values. They are **resolved values
  awaiting a reachable citation**. The numbers are deliberately not written in: the decision record is
  not reachable from this branch, and `source_refs` must not point at nothing (40:87, and Ruling 13's
  dangling-citation rule). This is a distinct state from "no document supplies this" and is recorded as
  such so the next pass adds the numbers with their citation rather than re-deriving the gap.
- **`presentation` on the destructible rock and the health pack** — keys omitted, no gap entry, per
  `docs/51:156` above.

#### A consequence the conversion produced, and `A22` caught: 19 citations de-scoped

Omitting `prerequisite` on 13 PowerUps and 6 option unlocks left each of those 19 files with a
`source_refs` entry scoped to a field that no longer exists — `prerequisite:
GDD-PERMANENT-POWERUP-CATALOG#shared-purchase-rules` and its unlocks equivalent. `A22` failed the run
with all 19 named, which is exactly the defect class Ruling 13 made a failure, arriving from a direction
nobody planned for: a legitimate conversion created dangling citations as a side effect.

Fixed as `A22`'s own message prescribes — "drop the prefix and keep it file-level; never delete a
citation that is the only support for a value still present". The 19 prefixes are removed and the
citations kept at file level, unchanged in target: `GDD-PERMANENT-POWERUP-CATALOG#shared-purchase-rules`
now annotates the whole definition instead of one absent field. No citation was deleted and none was
re-pointed at a different document; 0 of the 19 collided with an existing file-level entry, so 19
citations remain 19. The shared purchase rules still apply to every PowerUp and unlock whether or not any
of them has a prerequisite, so the file-level scope is the honest one.

#### Ruling 30 — a silently rewritten quotation on `REL-09`, restored

**The defect.** `content/relics/REL-09.json :: pause_behavior.rule` stored:

> "The enemy speed increase ends immediately when mining stops because the mech leaves, the point
> completes, or the simulation pauses."

`docs/69-initial-relic-catalog.md:153` states:

> "Every living enemy receives +50% movement speed while extraction progress is actively advancing. The
> increase begins with forward progress and ends immediately when mining stops because the mech leaves,
> the point completes, or the simulation pauses."

The tail matched closely enough to read as a quotation; the head had been rewritten, and the rewrite
**dropped the clause stating when the effect begins**. The lost clause is "The increase begins with
forward progress" — the effect's **start condition**, which is the half of the sentence a reader
consulting a field named `pause_behavior.rule` would most need, since a rule about when something stops
is incomplete without the condition that starts it. The field presented itself as verbatim quotation, so
nothing structural could see the change: it is not a null, not a naming defect, not a value, and no
value-preservation proof over numeric leaves would ever touch it.

**The fix.** The verbatim second sentence of `docs/69:153` is restored. This is **our** transcription
error, not a design-source contradiction, so it belongs here and not in the contradictions section.

**Second case, judged rather than fixed.** `content/mining-sites/specialized-material-geodes.json ::
progress_decay.rule` reads "The reward is withheld until the Complete transition, so an incomplete
attempt pays nothing", which overlaps its cited section (`GDD-MINING#specialized-material-geodes`) by
very little and uses `Complete`-transition vocabulary from the technical state diagram rather than the
geode section's wording. Reading the cited text: `docs/40:78` says the geode "awards that unit and 50
common ore **only at completion** and provides **no partial material or ore payout**", and `docs/40:42`,
in a different section, says "Material geodes and Hyper Gold sites **withhold their primary reward until
the `Complete` transition**." **Conclusion: this field is our own description, not a quotation, and the
text is left alone.** There is no single passage it could be a verbatim quote of — it synthesises two
sentences from two different sections, and unlike `REL-09` nothing is lost in the synthesis: both source
facts (reward only at completion, no partial payout) survive intact in it. It is recorded here as an
**authored-description field** so that a later reader does not mistake it for a quotation and "restore"
a passage that never existed. It also lacks a scoped `source_ref` for `progress_decay`, which is part of
the wider citation-coverage finding deliberately left to a follow-up pass (below).

**Deliberately out of scope.** The prototype that found these two also reported 248 further prose/citation
mismatches needing roughly 64 new scoped `source_refs` entries across 37 files. Those are **citation**
defects, not prose defects — the prose is right and the citation is too coarse to check it — and folding
37 files into this pass would bury the fixes under review. That is a follow-up pull request.

#### Ruling 31 — A24 was pinned to one spelling of a path, and two real defects were hiding behind it

`A24` matched `docs/.*\.md`, which pins three incidental spellings of a path — the literal directory
name, a forward slash, and a lowercase `.md` — and none of them is the unstable thing. Six citation
forms walked through it: no extension, a backslash separator, no `docs/` prefix, uppercase, and
`.markdown`. It is now two rules keyed on what is actually wrong: **a `:<digits>` line number after any
path-like token**, in either separator and any case with the extension optional; and **any repository
path at all** (`docs`, `src`, `content`, `tools`, `assets` followed by a separator), because
`40:87` names `doc_id#anchor` as the citation form and a path is not one, line number or no line number.

A bare `#anchor` is **out of scope by design**: it is half of the sanctioned form, `A9` already resolves
every anchor in `source_refs` against real heading slugs, and it carries neither a path nor a line
number. Flagging it would fire on the spelling the envelope endorses.

**Two occurrences of Ruling 25's class were hiding behind the old pattern**, found by the new rules
against the existing tree rather than by injection:

- `standard-encounter-schedule.json :: minute_rows[33].formation_events[0].reconstruction_basis` ended
  "See `content/transcription-notes.md`." — a repo path the old pattern could not match because the
  prefix was `content/`, not `docs/`. Reworded to "The reconstruction is recorded in the transcription
  notes beside this catalog."; the pointer's meaning is unchanged.
- `UTL-A1 :: external_numerics[1].statement` said "`docs/68` calls it Harmonic Calibrator" — a `docs/`
  path with no extension and no line number, which `docs/.*\.md` could not match. Rewritten to
  "`GDD-UTILITY-CATALOG` calls it Harmonic Calibrator", which is the citation form `40:87` names. The
  `UTL-A1` naming contradiction itself is unchanged and is still recorded as `C-2` above.

#### Value-preservation record, corrected

The earlier record on this branch stated the multiset difference of numeric leaves as `{0.75: 1}` and
presented it as covering `21f1734 → 159b9c4`. **That figure is true of the final commit alone.**
Recomputed: `75310ed → 159b9c4` removes `{0.75: 1}` and adds nothing, but across the full range
`21f1734 → 159b9c4` the tree goes from **2,320 numeric leaves to 2,313** — **seven** left, not one, and
the earlier report never named six of them. A measurement must be stated with the range it was taken
over; this one was not.

**All seven, enumerated.** Value multiset difference `21f1734 → 159b9c4`:
removed `{0.75: 1, 1.1: 1, 1.25: 1, 1.3: 1, 1.45: 1, 1.5: 2}`, added `{}`.

| # | Path | Value | Why it left |
| --: | --- | ---: | --- |
| 1 | `BOSS-01 :: contact_footprint.center_distance_that_begins_contact_m` | 1.25 | Ruling 12 — derived as radius + the player's 0.50 M collision radius; a second writer on a player-baseline constant |
| 2 | `BOSS-02 :: contact_footprint.center_distance_that_begins_contact_m` | 1.5 | Ruling 12, same derivation |
| 3 | `BOSS-03 :: contact_footprint.center_distance_that_begins_contact_m` | 1.3 | Ruling 12, same derivation |
| 4 | `BOSS-04 :: contact_footprint.center_distance_that_begins_contact_m` | 1.45 | Ruling 12, same derivation |
| 5 | `MGC-01 :: destructible_rock_rules.health_pack.collection_center_distance_with_standard_mech_circle_m` | 0.75 | Ruling 24 — the same derivation, third writer (0.25 M pickup radius + 0.50 M) |
| 6 | `REL-09 :: cross_document_rules[0].enemy_movement_multiplier` | 1.5 | Ruling 19 — **a deletion.** Duplicate writer removed |
| 7 | `REL-09 :: cross_document_rules[0].multiplies_with_elite_movement_multiplier` | 1.1 | Ruling 19 — **a deletion.** Duplicate writer removed |

**Rows 6 and 7 are deletions and were never disclosed as removals.** A duplicate-writer removal is a
deletion even when the value survives somewhere else, and both do survive: the 1.5 remains at
`REL-09 :: effects.enemy_movement_speed_multiplier_while_mining`, and the 1.1 remains at
`content/enemies/shared-elite-modifiers.json :: movement_speed_multiplier`, which is the catalog that
owns the elite movement modifier. Surviving elsewhere is the *justification* for the deletion, not a
reason to omit it from the record — the earlier report described Ruling 19 as collapsing duplicates and
never said that two numeric leaves left the tree as a result.

**This pass's own multiset difference, with its range named.** Range: `bb10612 → this pass's commit`,
`content/**/*.json` only.

- **Numeric leaves: 2,313 → 2,313.** Value multiset difference: **empty in both directions.** Stronger
  than that, and measured: the multiset of `(file, JSON path, value)` triples is also unchanged — **0
  gone, 0 new** — so no number moved to a different path either, which the value multiset alone would not
  have shown. Every one of the 275 `null` dispositions replaces a nulled key with an absent key, and a
  `null` is not a numeric leaf, so the conversions cannot move the count by construction; that is exactly
  why the empty difference is a real check on the 29 removals below rather than a restatement of the
  conversions.
- **29 keys removed, carrying 0 numeric leaves.** The 20 relic `rarity_and_weighting` fields, the 4 boss
  `armor` fields, the 3 `external_numerics[n].value` fields and the 2 nested `id` keys were every one of
  them `null` at `bb10612`, so removing them deletes keys and no numbers. Stated explicitly because "29
  keys removed" and "no numeric leaf moved" are both true and either one alone would mislead: the first
  reads as data loss it is not, the second reads as nothing having been removed.
- **1 string changed**, not a removal: `REL-09 :: pause_behavior.rule` gained back the dropped start
  condition (Ruling 30). Two further strings were reworded to remove repository paths (Ruling 31), with
  no change of meaning in either.
- **`null` count: 275 → 0**, with **no declared exceptions**.

### Integration-owner rulings applied — seventh pass

#### Corrected transcription errors — two trailing periods that turned a fragment into a sentence

**Both are our errors, not design-source contradictions.** Nothing in the documents disagrees with
anything else; a character was added that the source does not have, and each stored string is now a
character-exact substring of the section it cites. Verified against the source before editing and
after.

| # | Field | Was | Now | Source |
| --: | --- | --- | --- | --- |
| 1 | `content/branches/W-DE-focal-array.json :: effects.pellet_path` | `…converge on a focal point.` | `…converge on a focal point` | `docs/71-initial-weapon-numeric-catalog.md:443` |
| 2 | `content/branches/W-CE-critical-mass-cycle.json :: effects.charge_consumption` | `…the count that pulse hits.` | `…the count that pulse hits` | `docs/71-initial-weapon-numeric-catalog.md:382` |

The two source lines, quoted in full, are the whole of the evidence:

- `:443` — `- All five pellets spread and then curve inward to converge on a focal point centered on
  persistent mech facing at current maximum range.` The sentence **continues** past `focal point`; there
  is no period there.
- `:382` — `- Charges are consumed by the next pulse and replaced by the count that pulse hits, allowing
  sustained crowd contact to maintain the bonus.` The sentence continues with a comma.

**Why the period mattered, and why it is the specific defect worth recording.** Neither field lost
information. The omitted tails are stored verbatim elsewhere in the same files — for `W-DE-focal-array`
the sibling `effects.focal_point_location` holds `centered on persistent mech facing at current maximum
range` word-for-word, and `rules[0].text` holds the complete sentence; for `W-CE-critical-mass-cycle`
`rules[2].text` holds the complete sentence. **None of those four fields was touched.** What was wrong
was narrower and more corrosive: the trailing period dressed a *fragment* as a *complete sentence*, and
that is exactly what made it **indistinguishable from a truncated quotation**. A reader — or a
checker — seeing a capital-to-period string has no way to tell "this is a deliberately extracted clause"
from "this quotation lost its second half". Delete the period and the string reads as what it is, a
clause extracted under a field name that supplies its context.

These two are the *only* two such cases in the tree: they are the complete output of the quotation rule
adopted in `content/quote-verification-audit.md` §6, which fires when a stored string begins at a
sentence boundary, carries its own terminator, and the source sentence continues past it — 2 hits across
1,072 quotations, zero false positives. The exception list is empty because the fix is one character
each rather than a marker.

#### Ruling 32 — the ten enemy `armor: 0` fields are removed

**Ruled: enemies do not have an armor stat, so the field should not exist.** This closes the referral
recorded under Ruling 29's `(c2)` above; the ten were **not** removed by extending the boss ruling.

Confirmed at the source before editing, as required:

- **`docs/31-initial-alien-roster.md:37` — the ordinary-enemy table columns are `ID | Identity | Family
  | Hull | Move | Contact | Body | Control resistance | Earliest minute`. There is no armor column.**
- `docs/31:25` states it outright: "Ordinary enemies have no Armor. Their listed control resistance
  reduces player-authored displacement magnitude and timed control duration…" — an **absence
  statement**, which is the crux (see the rock contrast below).
- Armor is a player-side stat: `docs/72-player-survivability-and-damage-baseline.md:36` gives the mech
  Armor 0 in the Shared Player Baseline, and `docs/72:121` applies it to *incoming* damage ("Subtract
  current Armor, to a minimum of one damage unless the effect ignores Armor").
- `docs/technical/40-content-data-and-validation.md:114`'s enemies-and-bosses field list omits Armor,
  while `:110`'s mech list includes it.

So `armor: 0` on an enemy is **an invented field holding a value the document denies exists** — the same
class as the relic `rarity_and_weighting` fields and the four boss `armor` fields, and **not** the same
class as a missing value. It is therefore recorded here as a **removal with its reasoning**, alongside
the boss `armor` removal it matches, and **not** as a value gap: a concept that does not apply to an
entity is not a gap in that entity's data.

**No citation dangled.** `A22` catches a `source_refs` scope prefix naming a field that no longer
exists — it caught 19 such orphans when Ruling 29's conversions removed fields. Checked before editing:
none of the ten files carries an `armor:`-prefixed prefix, or any prefix whose path descends through
`armor`. Every one of their citations is file-level or scoped to `movement_speed`, `contact_footprint`,
`contact_cadence`, `damage_pressure`, `applies_to_player`, `elite_eligible`, `first_playable_subset`,
`description`, or (on `EN-06`) `specialist_attack.*`. `A22` reports **zero dangling** after the removal;
no citation was deleted or re-pointed because none needed to be.

#### Ruling 33 — the rock `armor: 0` STAYS, and the asymmetry with enemies is structural

`content/maps/standard-map-generation-contract.json :: destructible_rock_rules.destructible_rock.armor`
holds `0` beside `hull: 100`. It was investigated on the question of whether Ruling 32 extends to it.
**It does not, and the evidence is decisive in the opposite direction.** Recorded because "the same
reasoning probably applies" is precisely how a ruling gets over-extended — earlier on this branch that
reasoning nearly deleted four authored boss diameters (Ruling 23).

A destructible rock's Armor is **stated, as a value, in a property table**:

- `docs/72-player-survivability-and-damage-baseline.md:190` opens `### Destructible rock`, and its
  property table gives `| Hull | 100 |` at `:194`, **`| Armor | 0 |` at `:195`**, and
  `| Damage footprint diameter | 0.80M |` at `:196`. The Armor row sits *between* two values this tree
  already asserts.
- `docs/51-standard-map-generation-contract.md:156` — "Every rock has 100 Hull, zero Armor, a non-solid
  0.80M weapon-damage footprint, and no response to control."
- Corroborated in the same enumerated form at `docs/50-maps-resources-and-navigation.md:94`,
  `docs/30-combat-weapons-movement-camera.md:102`, `docs/glossary.md:102`, and as a tagged row in
  `docs/data/survivability-baseline.csv:25` (`rock,hull,100,Hull,Zero Armor`).

**The asymmetry is structural, not a judgement call.** For an enemy the document says the stat *does not
exist* ("have no Armor"); for a rock the document *assigns the stat a value of zero*, in the same
sentence and the same table row sequence as its Hull. A rock has Hull and takes weapon damage, so Armor
applies to it for the same reason it applies to a mech. `armor: 0` on a rock is a faithful transcription
of an authored value; `armor: 0` on an enemy invented a field. **No change made.**

#### Ruling 34 — the 16-rock cap was transcribed and asserted by nothing; two A13 rows added

A coverage gap, not a value change. `destructible_rock_rules.active_maximum: 16` and
`initial_count: 16` were transcribed correctly and **no assertion covered either**. Both are now `A13`
world-prop value rows.

Verified at the source: `docs/51-standard-map-generation-contract.md:146` — "Standard mode maintains a
dynamic population capped at **16 active destructible rocks** … The run begins with 16 rocks at valid
offscreen positions around deployment." That one line authors both values, which is why both rows cite
it. `docs/72:203` corroborates the cap only ("The existing one-attempt-per-second, 10% success chance,
and 16-rock active cap remain unchanged"), so it is recorded as corroboration rather than as the
citation for `initial_count`.

**Why it was missed generalises, and that is the reason it is written down.** Rock Hull 100 (`docs/72:194`)
and the 0.80 M footprint (`docs/72:196`) were both already asserted, and they **bracket** the population
rules in the same document section. **A value whose neighbours are asserted reads as covered.** That is a
distinct failure shape from the two this branch has already fixed: it is not a gate that *cannot fail*
(the A21 non-JSON row, the old world-prop key-family probe) and not a gate that *fires wrongly* (the
sentence-boundary rule measured and dropped in the quote audit) — it is a gate nobody thought to write,
and its signature is being surrounded by coverage. Recorded in
`content/quote-verification-audit.md` §7 beside the other two shapes.

Negative-controlled individually, each value reverted afterwards: `active_maximum` 16 → 15 fails
"A13 destructible rock active population cap must be 16"; `initial_count` 16 → 12 fails "A13
destructible rock initial count must be 16".

#### Ruling 35 — the quotation matcher's corpus premise is asserted, not documented (`A27`)

The quotation rule adopted in `content/quote-verification-audit.md` §6 measured **zero** false
positives across 1,072 quotations — that audit's whole matched set, being its 806 decidable matches
plus the 266 matches below its decidability gate, both derived in its §2 tree-state table — but only
because `.` is an unambiguous sentence terminator in this
corpus, and that is true only because `docs/` contains no `e.g.`, `i.e.`, `etc.` or `approx.` anywhere.
**"It can stop being true silently" is the whole problem, and a documented assumption is a fail-open
with a footnote.** So `A27` scans `docs/**/*.md` for eighteen sentence-internal abbreviations and fails
if any appears. Its failure message names the *matcher* as the thing to revisit and states that no
content string is implicated — because the day someone writes "e.g." in a design document, the build
must point at the rule, not at an innocent quotation. Confirmed passing today: zero occurrences of any
listed token under `docs/`. Negative control run against a scratch copy of `docs/` (this pass must not
modify `docs/`), reported in the pass summary.

#### Value-preservation record — seventh pass

**Range: `bb10612 → this pass's commit`, `content/**/*.json` only.** Measured, not carried forward.

- **Numeric leaves: 2,313 → 2,303.** Value multiset difference: **removed `{0: 10}`, added `{}`**. The
  earlier line on this branch — "2,313 → 2,313, empty in both directions" — was true of the sixth pass
  and is **not** true of this one, because Ruling 32 removes ten numeric leaves. It has been corrected
  rather than carried, since a multiset statement is only meaningful with its range named.
- **Across the full branch, `21f1734 → this pass's commit`: 2,320 → 2,303, seventeen leaves removed.**
  Value multiset difference: removed `{0: 10, 0.75: 1, 1.1: 1, 1.25: 1, 1.3: 1, 1.45: 1, 1.5: 2}`,
  added `{}`. The seven non-zero removals are the ones already enumerated in the corrected sixth-pass
  record above (Rulings 12, 19 and 24); the ten zeros are this pass.

**The ten removals, named, with the reason each left:**

| # | Path | Value | Why it left |
| --: | --- | ---: | --- |
| 1 | `EN-01 :: armor` | 0 | Ruling 32 — a field for a stat `docs/31:25` says enemies do not have |
| 2 | `EN-02 :: armor` | 0 | Ruling 32, same reason |
| 3 | `EN-03 :: armor` | 0 | Ruling 32, same reason |
| 4 | `EN-04 :: armor` | 0 | Ruling 32, same reason |
| 5 | `EN-05 :: armor` | 0 | Ruling 32, same reason |
| 6 | `EN-06 :: armor` | 0 | Ruling 32, same reason |
| 7 | `EN-07 :: armor` | 0 | Ruling 32, same reason |
| 8 | `EN-08 :: armor` | 0 | Ruling 32, same reason |
| 9 | `EN-09 :: armor` | 0 | Ruling 32, same reason |
| 10 | `EN-10 :: armor` | 0 | Ruling 32, same reason |

- **2 characters removed from 2 strings**, not removals of values: the two trailing periods above. No
  other string changed.
- **No number was added, and no number moved to a different path.** The removals are the only value
  change; every remaining `(file, JSON path, value)` triple is unchanged.
- **`null` count: 0 → 0**, still with no declared exceptions.

#### Ruling 36 — the 248 wrong-section quotations are re-pointed with 59 scoped citations, and the file list came from the directory

`content/quote-verification-audit.md` §4 records the dominant finding: **248 of the 378 prose
mismatches are verbatim quotations whose covering `source_refs` element names the wrong section
(228) or the wrong document (20)**. The prose was right and the pointer was wrong. This pass fixes
the pointer, and **no existing citation was deleted** — every new element is an addition.

**The affected set was enumerated from the tree, not from a count in a design document.** Grouping
the 248 records of `src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json` by
`(file, scope)` yields **65 groups across 37 files**. Six of them — the `rules[]` entries of
`UNL-01`…`UNL-06` — are **already correct on `master`**: the merged PR added the file-level
`GDD-PERMANENT-OPTION-UNLOCK-CATALOG#shared-purchase-rules` citation, and the evidence artifact
records all six as `exact` rather than `no-match`. So **59 new scoped elements across 31 files**
close the remaining 242.

**On the audit's "64 new scoped `source_refs` elements across 37 files".** Enumeration gives 65, not
64. The one-group difference is reproducible: `content/enemies/EN-06.json` has two groups —
`specialist_attack.projectile.lifetime_description` and
`specialist_attack.resonance_interactions.flux_amber` — whose correct section is the *same*
(`GDD-INITIAL-ALIEN-ROSTER#en-06--needler`), so they collapse to one element under the shallower
`specialist_attack:` prefix and the total becomes 64. They are kept separate here, because a prefix
that also covers `specialist_attack.hard_control_interaction` — which is quoted from
`TDD-ENCOUNTERS#needler`, a different document — would attribute that field to a section it does
not come from. The audit's 37-file figure counts the six `UNL-0*` files this pass does not need to
touch.

**The largest single instance, and the count that had to be taken from the filesystem.** 182 of the
248 are the shared utility rules block: seven `catalog_wide_rules.shared_acquisition_and_rank_rules`
and seven `catalog_wide_rules.modifier_and_timing_rules` sentences in **thirteen** files, quoted
verbatim from `GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules` and
`#modifier-and-timing-rules` while the only covering citation was the file-level
`GDD-UTILITY-CATALOG#utl-XX--<name>` (and, for the radar, `GDD-MAPS#resource-radar`).

**`UTL-R1` is the thirteenth and it is the one a careless pass drops.** The design documents say
"twelve non-radar utilities" in many places; that statement is true and it is *not* the file count.
The radar is an entity the documents treat as an exception while still giving it its own definition,
so a pass that reads "twelve" and enumerates twelve members produces a set that is internally
consistent, passes a value-preservation check, and silently leaves 14 mis-cited quotations in
`content/utilities/UTL-R1.json`. The set was therefore enumerated with `glob('content/utilities/*.json')`
and **asserted to be 13** before any file was written, with the thirteen IDs checked against
`UTL-A1 A2 B1 B2 C1 C2 D1 D2 E1 E2 F1 F2 R1`. `verify_content.py`'s own A12 row already carries the
same correction in its selector comment.

**How each target section was chosen.** For every group, the sections of `docs/` containing *every*
record in that group were computed under the four adopted normalization rules of audit §5 — R1-quotes,
R3-markup, R7a-initial-case, R8-period, and nothing else. Among those, preference went to a document
the file already cites, then to the deepest heading level, then to the smallest span. All 248 had at
least one hit under the adopted rules; none needed a fifth rule.

**Proof, measured rather than asserted.** Before this pass, **6 of the 248** were covered by a
citation naming a section that contains them — the six `UNL-0*` `rules[]` entries — and after it,
**248 of 248** (247 `exact`, and `EN-06 :: specialist_attack.hard_control_interaction` under
R7a-initial-case, which is one of the four adopted rules, not a fifth). The test uses the audit's own
reading from §12 — disjunctive over the *equally most specific* covering elements, where a file-level
citation has specificity 0 and a scope prefix's specificity is its segment count. That qualifier is
load-bearing and was pinned down by disagreement rather than assumed: checking every covering element
instead makes the four `BOSS-0*` `persistence.reentry.behavior` records read as matches on the tree
the artifact was measured against, and the artifact says they are not.

**An earlier revision of this ruling said "10 of the 248", and that figure was wrong.** 10 is
6 + those same four `BOSS-0*` records — i.e. it was measured under the all-covering-elements reading
that this very paragraph rejects. Under the adopted reading, and per the artifact's own stored
`verdict_on_this_tree` on `master` (`no-match` 371 / `exact` 7, of which 6 fall in the 248 and 1 —
`REL-09 :: pause_behavior.rule` — falls in the 130 located nowhere), the before-figure is **6**.

#### Ruling 37 — `verdict_on_this_tree` is re-derived by the checker, not stored prose

Re-pointing 242 citations changes what `quote_mismatch_evidence.json` should say in
`verdict_on_this_tree`, which is the artifact's one field that describes `content/` **today** rather
than the frozen measurement. Hand-editing a summary count is precisely the defect this pair of files
exists to prevent, so instead `check_quote_mismatch_evidence.py` now **recomputes the field per
record** from the live tree — the value at that `(file, pointer)` as it now stands, against the
`source_refs` that now cover that pointer, under the four adopted rules — and **fails** if the stored
field disagrees.

The recomputation was validated before it was trusted: run against `master` it reproduces the stored
figures exactly, **371 `no-match` / 7 `exact`**. On this pass's tree it gives **248 `exact`, 1
`match-under-a-named-rule`, 129 `no-match`**, and the artifact now stores that. The single
rule-matched record is `content/enemies/EN-06.json :: specialist_attack.hard_control_interaction`,
which needs R7a-initial-case because the document begins the sentence "Hard control may pause…".
The frozen halves — the 378 stored strings, their citations as measured, `located_breakdown`, and
`maximal_normalized` — are untouched, and `CASES THAT MOVE under maximal normalization` is still 0,
so audit §5's anti-golden claim is unaffected.

Negative controls, run and reverted individually:

- **Flipping one record's stored `verdict_on_this_tree` from `no-match` to `exact`** →
  `stored verdict_on_this_tree disagreements: 1`, the drifting record named, plus
  `FAIL: payload verdict_on_this_tree summary … != re-derived …`, `RESULT: FAIL`.
- **Deleting one of this pass's new citations from `content/utilities/UTL-R1.json`** (the
  `catalog_wide_rules.modifier_and_timing_rules[]` element) →
  `stored verdict_on_this_tree disagreements: 7`, each of the seven sentences named as
  `stored 'exact', recomputed 'no-match'`, `RESULT: FAIL`.

Both reverted; the checker returns `RESULT: ok` on the committed tree.

#### Ruling 38 — `FORMULA-01`, and the summary that says what the definition is

`content/weapons/stat-price-formula.json` carried `"id": "weapon-stat-price-formula"`, which matches
no ID grammar in the tree — every other minted ID is `<PREFIX>-<NN>`. It is now **`FORMULA-01`**. The
prefix was confirmed unused first: `grep -rn "FORMULA-" docs/ content/ src/` returned nothing.

Its localization keys migrate to `weapon.FORMULA-01.*`, and `content/localization/en.json` stays
flat, lexically sorted, duplicate-free and orphan-free (A10/A11 pass; 164 → 165 strings, the one
addition being the new summary).

A `summary_key` is added because the definition needed a summary that **disambiguates a definition
from a formula kind**. A registry separately mints formula kinds as `snake_case` tokens, and
`FORMULA-01` is not one of them: it is the definition that carries a kind together with that kind's
parameters. The disambiguation belongs where a reader meets the definition rather than in a note
elsewhere. The summary also states, truthfully, that this definition still holds its rule as the
literal expression `5n(n + 1)` rather than as a registered kind — which is what `verify_content.py`
already warns about at 40:99 for `stat-price-formula.json.formula` and
`.equivalent_by_depth.formula`. Claiming the file already carries a registered kind would have been
prose contradicted by the artifact.

`FORMULA-01` does not match A12's weapons selector `^W-[A-F]{2}$`, so the file remains the weapons
directory's one aggregate and the row still reads 15 items + 1 aggregate.

#### Ruling 39 — the eight camelCase value tokens are measured and left alone, pending a provenance answer

An earlier revision of this pass converted eight camelCase **value** tokens to lower-kebab-case, 12
occurrences across 9 files. **That conversion has been removed and the original camelCase strings
restored.** The measurement it rested on stands and is recorded here; the change does not.

Measured over every string leaf of every `*.json` under `content/`, kebab-case value tokens occur
**37 times across five token spaces** — `id` (`common-ore`, `hyper-gold`), `inventory_scope`,
`pool_availability`, `site_class`, `value_kind` — against camelCase's **12 occurrences of 8 distinct
tokens across four spaces**. (The brief for the earlier revision said "six token spaces … against
camelCase twice"; neither figure reproduces. That revision's own prose said 38 kebab occurrences;
enumerating them gives **37**, and the enumeration is in `content/README.md` under
"Property names are `snake_case`; values keep their exact case".)

The eight, found by scanning for the camelCase shape rather than by trusting a list:

| token | occurrences | where |
| --- | --: | --- |
| `relicCachePoolEntry` | 5 | `UNL-02`…`UNL-06` `unlocks.kind` |
| `utilityBlueprints` | 1 | `UNL-01` `unlocks.kind` |
| `terrainCollision` | 1 | `EN-06` `specialist_attack.projectile.snapshot_at_creation[3]` |
| `noHoming` | 1 | `EN-06` `specialist_attack.projectile.snapshot_at_creation[4]` |
| `beamWidth` | 1 | `W-AB-unbounded-bore` `effects.unchanged_stats[1]` |
| `projectileSpeed` | 1 | `W-AB-unbounded-bore` `effects.unchanged_stats[4]` |
| `attackRate` | 1 | `W-AE-replicator-swarm` `effects.clone_inherits_current[1]` |
| `operationalRange` | 1 | `W-AE-replicator-swarm` `effects.clone_inherits_current[2]` |

**Why nothing is converted.** The argument for converting was that these are minted tokens that
missed a convention, resting on the record above that a transcription pass re-cased property *names*
to `snake_case` and left values alone. Read literally, that record says stable ID, enum and kind
tokens **in values** keep their exact case — which protects these eight as readily as it marks them
as residue. Nobody has established which they are: none of the eight appears in `docs/`, `src/`, or
elsewhere in `content/`, so there is no call site to settle it either way, and the absence of a
document occurrence is equally consistent with both stories. The one piece of evidence either way is
suggestive rather than decisive — many per-definition notes below record "Field names are camelCase
per the CAT-stream transcription convention", so the source these files came from was camelCase-native
for *names*, which makes camelCase-native *values* plausible without establishing it. Meanwhile the schema stream has not
fixed the token grammar a converted value would have to satisfy. Converting twice would be worse
than converting once, late, so these wait on a provenance answer and a grammar. The resource IDs,
`canonical_letter` and `recipe_pair` are untouched for the separate reason that an `RSC-01`–`08`
migration is pending and must land as one pass.

#### Ruling 40 — the repo-`path:line`-in-a-value item is already closed, and the count is recorded

The brief for this pass asked for **four fields embedding a repo `path:line` string inside a value**
to be converted to `DOC-ID#anchor`. Measured against the tree rather than assumed: **there are
none.** `A24`'s two regexes (`LINE_NUMBER_IN_VALUE`, `REPO_PATH_IN_VALUE`) applied to every string
leaf of every `*.json` under `content/` — including `content/localization/en.json`, which
`load_definitions()` skips and which A24 therefore never sees — return **0 hits**, and so does a
deliberately wider net (`.md` anywhere in the value, or any of `docs|src|content|tools|assets`
followed by a separator).

Traced through history so the figure is checkable rather than asserted: `21f1734` held **15
occurrences across 5 field paths** (`effect.stacking_classification` ×11,
`minute_rows[].formation_events[].reconstruction_basis`, `beacon_response_source`,
`external_numerics[].statement`, `price_curve_decision.note`); Rulings 25, 26 and 31 of the merged PR
took that to 2 by `159b9c4` and to **0 by `4291cb0`**, which is on `master`. Nothing remains to
convert. The only `path:line` strings left anywhere are inside
`src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json`, in the `cited[].span` and `file`
fields of the measurement artifact, where a repository path is the subject rather than a citation.

#### Value-preservation record — eighth pass

**Two multisets, over two different value types, with their scopes named.** This pass changes
**strings** and no numbers, so the numeric multiset — the one every earlier pass reported — is not
the proof that covers this diff. Both were run; neither stands in for the other.

**Range for both: `origin/master` (`4eda0c5`) → this pass's commit.** Measured, not carried forward.
Each was run at two scopes: the **33 touched `content/**/*.json` files**, and, so that nothing can
hide outside the touched set, **all 139 `*.json` files under `content/`**. Both scopes give the same
difference.

- **Numeric multiset — unchanged. Scope: numeric leaves (`int`/`float`, `bool` excluded).**
  508 → 508 over the 33 touched files, 2,303 → 2,303 over all 139. Value multiset difference:
  **removed `{}`, added `{}`** at both scopes. **No gameplay number changed in this pass.** This
  proof says nothing about the strings.
- **String multiset — changed, and every difference was enumerated before it was measured. Scope:
  string leaves.** 1,366 → 1,427 over the 33 touched files, 5,201 → 5,262 over all 139; both are
  **+61 net = 63 added, 2 removed**, and the 63/2 sets are identical at both scopes. The expected
  difference was written down first — from the frozen artifact, not from the diff — and the measured
  set equals it exactly:
  - **59 added:** one new scoped `source_refs` element per `(file, scope)` group. The 59 groups were
    re-derived independently by grouping the 248 located-somewhere records of
    `quote_mismatch_evidence.json` **as it stands on `master`** by `(file, pointer)` → 65 groups
    across 37 files, less the 6 `UNL-0*` `rules` groups already stored `exact` → **59 groups across
    31 files covering 242 records**. Measured: 59 new elements, one per group, `(file, scope)`
    multiset equal to the derived one, each of the form `<scope>: <DOC-ID>#<anchor>` and each at a
    `source_refs[...]` path. *Anchor* correctness is a separate proof, re-derived against `docs/` by
    `check_quote_mismatch_evidence.py`.
  - **4 added, 2 removed:** `FORMULA-01`, `weapon.FORMULA-01.name`, `weapon.FORMULA-01.summary` and
    the summary string itself, against `weapon-stat-price-formula` and
    `weapon.weapon-stat-price-formula.name` (Ruling 38).
  - **Nothing else.** No other string was added, removed, or changed at either scope.
- **2 `(file, JSON path)` pairs changed value**, both in `stat-price-formula.json`: `id` and
  `name_key` (Ruling 38). No other pair moved. The earlier revision of this pass had 14, the extra 12
  being the camelCase token conversion that Ruling 39 has since removed.
- **1 pair removed, 62 added.** The single removal is `en.json ::
  weapon.weapon-stat-price-formula.name`, re-added at `weapon.FORMULA-01.name` **with the string
  value unchanged** — a key rename, which is why `Weapon common-ore stat upgrade price` appears in
  neither direction of the string multiset and its absence there is asserted rather than assumed.
- **`null` count: 0 → 0**, still with no declared exceptions.
- **No citation was deleted.** Every `source_refs` element present on `master` is present here.
- **Outside `content/`, reported not asserted.** `quote_mismatch_evidence.json` is a measurement
  artifact, not a value store, and its string multiset does change: 242 `no-match` verdicts become
  241 `exact` plus 1 `match-under-a-named-rule` (its string-leaf total is unchanged at 5,176, and its
  numeric leaves go 6 → 7 as the verdict summary gains a third category). That is the recomputation
  Ruling 37 exists to perform. The three Markdown files under `content/` are prose and are excluded
  from both multisets.

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

---

## Appendix — pre-clear audit: three independent checks

This appendix is the durable record of the read-only audit that produced Rulings 14–22 above. It ran
before those rulings existed, wrote nothing into the repository, and mutated no git state. It is kept
here rather than discarded because **two of the rules it checked would have produced a wrong answer if a
validator had applied them mechanically** — see [What the audit changed about the validator
set](#what-the-audit-changed-about-the-validator-set) — and because the findings it left open still need
owners.

**Scope.** 139 JSON files under `content/**`, 8,827 scalar leaves walked, plus the full text of
`docs/technical/40-content-data-and-validation.md` and the design documents cited below.

**The rules checked.**

1. `40:106` and `40:67` — whether the resource-field enumeration and the stable-ID reuse policy support
   the premise behind a downstream ID ruling.
2. `40:96` (`_m`, `_m_per_s`, `_seconds`, `_per_second`, `_hull`, `_degrees`, `_fraction`, `_count`) and
   `40:97` (percentage points only on a `_percent` name, the normalized factor left to the compiler) —
   the one-spelling rule for a multiplicative scale.
3. `40:98` — "Geometry dimensions distinguish radius, diameter, width, range, and area; `area` is never
   used as a vague scalar name."

### Check 1 — the premise behind a downstream ID ruling

- **`docs/technical/40-content-data-and-validation.md:106` enumerates resource fields — CONFIRMED
  verbatim.** "Resource definition fields include ID, canonical letter, localization keys,
  icon/pattern/audio identity, inventory scope, persistence class, maximum safe count, and resonance
  behavior registration if applicable." So the prose does list "ID" and "canonical letter" as two
  separate entries in one enumeration.
- **The ID-reuse rule omits resources — CONFIRMED.** `40:67` reads "Reuse accepted gameplay IDs exactly
  for defined content: `MCH-01`, `EN-01`, `BOSS-01`, `W-AB`, `REL-01`, and equivalent utility/PowerUp/unlock
  IDs." Resources are absent, confirmed by reading all of `## Stable ID policy` (lines 65–72).
- **`canonical_letter` is prose only, never a field name.** The token is absent from the entire
  repository (`grep -rn "canonical_letter" docs/ src/ content/` → no matches). "canonical letter" appears
  three times, all in running prose: `40:106`, `docs/73-…:156`, `docs/73-…:189`. By contrast the doc names
  real fields in backticks in the `## Common definition envelope` table at `40:74-88`. **So
  `canonical_letter` as a field name would be a choice, not a mandate.**
- **Counter-evidence the ruling should have seen.** The inference "the letters `A`–`F` were never the
  resource ID" is contradicted by the shipped content: `content/resources/A.json` carries `"id": "A"` and
  `"name_key": "resource.A.name"`, and the same shape runs through `F.json`. No resource file carries a
  canonical-letter field of any spelling. Today the letter *is* the ID.
- **Resources are not uniquely omitted from the reuse bullet.** `branches` (eleven-plus ID-bearing files,
  its own doc section) and `mining-sites` are omitted too, and the bullet says "and equivalent … IDs",
  so its list is illustrative rather than exhaustive. Any argument built on "resources were deliberately
  excluded" is weakened by that.

**Status: informational, no content change.** Nothing in Rulings 14–22 depends on it; it is recorded so
the ID decision is not re-argued from the same unsupported premise.

### Check 2 — the `_multiplier` one-spelling rule

**Spellings found for a multiplicative scale.**

| Spelling | Names | Leaves | Verdict |
| --- | --- | --- | --- |
| `_multiplier` | 43 | 52 | dominant and canonical |
| `_scaling` | 4 | 4 | a second live spelling — `REL-07 :: explosion_strength_scaling`, `explosion_area_scaling`, `elite_and_boss_scaling_cap` (all `null`), and `W-BF-tethered-reaper :: effects.speed_scaling` (a prose curve) |
| `_multiple_of_` | 1 | 1 | `REL-09 :: mining_decay_multiple_of_current_forward_extraction_rate = 4`, colliding with a `_multiplier_of_` twin |
| `_factor` / `_scale` / `_ratio` / `_times` / `_x` as a suffix | 0 | 0 | **confirmed clean** — a regex over every leaf key returned no matches, so the `body_scale_factor` regression is gone from the JSON and `body_scale_multiplier` is uniform across all eleven files that carry it |
| `_scale` as a grouping key | 1 | — | `maps/standard-map-generation-contract.json :: world_scale` is an object of *absolute* quantities (`major_region_count`, `traversable_diameter_m`, …), not factors — a `_scale` name over a container of non-factors |

Non-suffix `scale`/`scales` predicates and reference strings (booleans and strings, not scalars) were
listed for completeness and are not violations: `scales_with_elapsed_time_or_player_state` (10 enemy
files), `scales_with_attack_rate_ranks_and_global_modifiers`, `scales_with`,
`link_length_and_formation_size_scale_with`, `pull_resistance_scales_for_elites_and_bosses`,
`explosion_scales_from`.

**Same concept under two different names — seven findings.**

| # | Finding | Status |
| --- | --- | --- |
| A | Mining progress-decay rate, value `4`, under `decay_rate_multiplier_of_forward_rate` in four `mining-sites/*.json` and `mining_decay_multiple_of_current_forward_extraction_rate` in `REL-09` | **fixed** — Ruling 14 |
| B | Claim-Jumper enemy movement, value `1.5`, under `effects.enemy_movement_speed_multiplier_while_mining` and `cross_document_rules[0].enemy_movement_multiplier` in one file; the short name drops the `while_mining` condition its own `rule` text restates | **fixed** — Ruling 19, pair 1 |
| C | Elite movement factor, value `1.1`, under `shared-elite-modifiers :: movement_speed_multiplier` and `REL-09 :: cross_document_rules[0].multiplies_with_elite_movement_multiplier`; the second reads as a boolean predicate while holding a number | **fixed** — Ruling 19, pair 2 (and the audit's "internal to REL-09" framing corrected there: the pair is cross-file) |
| D | Damage-multiplier ceiling, value `2`, as `_cap` and `_max` in one file (`W-BD-selective-detonators`) | **fixed** — Ruling 16, spelling unified in both objects, neither removed |
| E | Focus-multiplier ceiling, value `2`, as `W-AF :: fixed_properties.focus_multiplier_maximum` and `W-AF-coherence-memory :: effects.focus_cap_multiplier` — the same ceiling with the two words transposed, plus `changes_focus_cap_multiplier = false`, a boolean carrying `_multiplier` | **partly fixed, partly open** — Ruling 16 spelled the branch fields `focus_maximum_multiplier` / `changes_focus_maximum_multiplier`, but the weapon and its branch still name one ceiling two ways (`focus_multiplier_maximum` vs `focus_maximum_multiplier`) and the boolean still carries `_multiplier`. **Open: no ruling picked a winner across the two files.** |
| F | Upper-bound suffix inconsistent repo-wide across `_cap`, `_max` and `_maximum`, with `W-BF-tethered-reaper` carrying two spellings in one object | **fixed** — Ruling 16, and the one escalation is now closed by Ruling 28: the document owner confirmed 200 bounds the speed-bonus component and 400 bounds the total, so both values stay and both fields are renamed. `BOUND_SPELLING_ESCALATED` is empty. |
| G | Factor and its complementary percentage stored side by side, on `REL-04`, `REL-07` and `REL-09` | **audited, nothing removed** — Ruling 21. All three pairs agree arithmetically, and each factor is stated as a multiplier by a document the definition already cites, so removing one would drop a doc-stated value. **Open: collides with the compiler's derived field when `DAT-006` lands.** |

**Multiplicative values whose property name does not say so.**

1. `REL-09 :: mining_decay_multiple_of_current_forward_extraction_rate` — same as finding A. **Fixed.**
2. `REL-07 :: explosion_strength_scaling` and `explosion_area_scaling`, both `null`, multiplicativity
   inferred from `docs/69-initial-relic-catalog.md:130` rather than from data. **Resolved by Ruling 15
   (omitted) and Ruling 14 (renamed) respectively — see the open tension recorded under Ruling 15.**
3. `REL-07 :: elite_and_boss_scaling_cap`, `null`, a bound on a scaling whose name states neither a
   multiplier nor a unit. **Fixed — omitted, Ruling 15.**
4. `maps/standard-map-generation-contract.json :: world_scale` — a `_scale` name over non-multiplicative
   contents. **Open, unruled.**
5. `W-CF-circuit-closure :: effects.eruption_damage_as_seconds_of_current_segment_damage = 6` —
   functionally a multiplier on current DPS, named as a duration, and doc-faithful
   (`docs/71-initial-weapon-numeric-catalog.md:418`: "an eruption hit equal to six seconds of current
   segment Damage"). **Open judgement call; arguably intended.**
6. The `_percent_of_current_*` family (`charged_shot_width_percent_of_current_width = 200`,
   `secondary_blast_radius_percent_of_current_radius = 60`, `shell_blast_radius_percent_of_current_radius = 160`,
   `singularity_radius_percent_of_current_radius = 150`, `blade_radius_percent_of_current_cutter_radius`,
   `burst_damage_percent_of_full_duration_damage_budget`) — relative scales in percentage points.
   **Compliant** under `40:97`, which permits percentage points when the name says `_percent`. Listed so
   they are not miscounted as violations.
7. Formula strings that encode multipliers instead of naming them —
   `W-BD-selective-detonators :: damage_multiplier_formula`, `W-AE-containment-lattice :: link_damage_per_second_formula`,
   `W-AD-singularity-forge :: singularity_damage_per_second_formula`, `W-AD-gravity-slingshot :: burst_damage_formula`.
   `40:99` requires "a registered formula kind plus parameters, not arbitrary script strings".
   **Open — a separate rule, already reported by the verifier as an A17 warning.**

**`body_scale_factor` in this file is deliberately not scrubbed.** The audit found the dead spelling
surviving at `content/transcription-notes.md:442`, `:451`, `:484-486`, `:597`, `:784`, `:811`, `:817` —
both spellings in adjacent tables of one document. The ruling: **naming rules bind definitions, not
documentation.** Ruling 7 is recorded as superseding the rename, and scrubbing the old spelling out of an
audit trail stops it being a record. The verifier walks `content/**/*.json` definitions only. **Not a
defect; no change.**

**Check 2 tally.** Rename-level violations of the one-spelling rule: 6. Bound-suffix violations of the
same principle: 1 cluster (88 property names once swept). Redundant twin representations: 3. Confirmed
clean: no `_factor`/`_scale`/`_ratio`/`_times`/`_x` suffix anywhere in the JSON.

### Check 3 — the `area` dimension rule

**Every property whose name contains `area` — 6 total.**

| # | Property | Value | Doc support for a specific dimension | Verdict |
| --- | --- | --- | --- | --- |
| 1 | `W-CF-circuit-closure :: effects.minimum_enclosed_area_m2` | 4 | yes — `docs/71-…:418` "encloses at least 4M², the loop closes" | compliant |
| 2 | `W-CF-circuit-closure :: effects.maximum_claimed_interior_area_m2` | 40 | yes — `docs/71-…:419` "A loop may claim at most 40M² of interior area" | compliant |
| 3 | `REL-04 :: effects.weapon_area_multiplier` | 2 | no single dimension — it scales a set | **exempt, Ruling 17** |
| 4 | `REL-07 :: effects.explosion_area_multiplier` (was `explosion_area_scaling`) | `null` | no — `docs/69-…:130` says only "Explosion strength and Area scale from that enemy's maximum Hull" | **renamed under Ruling 14; exemption reasoning of Ruling 17 applies to the `area` half; the `null` is the open tension under Ruling 15** |
| 5 | `maps/… :: topology.optional_pockets.terminal_area_rule` | prose | n/a — region noun | compliant |
| 6 | `maps/… :: topology.optional_pockets.exit_readable_from_terminal_area` | `true` | n/a — region noun | compliant |

**Geometry names that do not state their dimension — five judgement calls.**

| Property | Value | Problem | Status |
| --- | --- | --- | --- |
| `maps/… :: deployment_and_opening_fairness.obstacle_free_radius_in_mining_zone_diameters` | 1 | a **radius** measured in **diameters**; `docs/51-…:70` ("obstacle-free space at least one mining-zone diameter around the mech") does not say whether the radius or the diameter of the clear envelope is meant. `docs/technical/50-…:99` and `docs/51-…:47` use "diameters" as a unit of *width* elsewhere. Factor-of-two risk. | **not touched — Ruling 18**, escalated for a doc correction |
| `W-DF :: fixed_properties.forward_reach_m` | 1.2 | "reach" is not one of `40:98`'s five words; `docs/71-…:89` and `docs/data/weapon-base-balance.csv:15` both say "1.2M forward reach" and neither says whether it is measured from mech centre or hull front | **open** |
| `W-AF-cutting-vector :: effects.range` | `"the upgradeable laser range"` | a bare `range` — an allowed dimension word, but not *which* range | **open** |
| `W-DF-siege-anchor :: effects.barrier_thickness_source` | `"current ram width"` | dimension-word mismatch: key says *thickness*, value says *width*; "thickness" is outside `40:98`'s five words | **open** |
| `W-AB-fracture-lance :: effects.shockwave_reach_per_side_m` | 2.5 | "reach" again; `per_side` resolves the half-vs-full ambiguity, but no doc line states the dimension or the value | **open** |

**Dimension-by-reference strings are compliant** — the dimension word is present and no unit is expected
because the value is a pointer: `burst_radius`, `collection_field_radius`, `emission_range`,
`follow_distance`, `shockwave_width`.

**No ambiguous `_m` radius-or-diameter value exists.** Every metric geometry leaf states its dimension in
the key; the audit enumerated all of them across `radius`, `diameter`, `width`, `range`, `distance` and
`area` and found zero cases where an `_m` value could be either.

**Bare `size`.** `REL-01`–`REL-10 :: rarity_and_weighting.fresh_profile_pool_size` and
`fully_unlocked_pool_size` — 20 occurrences, counts wearing a bare `size`, a `40:96` unit-suffix defect
rather than a geometry one. **Fixed — Ruling 20.** `W-AE-replicator-swarm :: max_total_squad_size_multiplier`
and `W-AE-containment-lattice :: link_length_and_formation_size_scale_with` were also flagged; the first
keeps `size` deliberately (it multiplies a size, it is not a count) and the second, which bundles a length
and a size into one key with no dimension on either, is **open**.

**Check 3 tally.** Properties containing `area`: 6 — 4 compliant, 2 needing a ruling (both now ruled).
Geometry names lacking a stated dimension: 5 judgement calls, 1 ruled and 4 open. Bare `size` on counts:
20 occurrences, fixed. Ambiguous `_m` values: 0.

### What the audit changed about the validator set

Two rules in the planned validator set **would have produced a wrong answer** had they been implemented
as written and run unattended. This is the audit's most important output, and the reason it is preserved
rather than summarized away.

1. **`40:98` applied to the `Area` stat.** Read literally, "`area` is never used as a vague scalar name"
   condemns `REL-04 :: effects.weapon_area_multiplier` — and a validator enforcing it would have demanded
   a specific dimension in the name. That would have been **wrong**: `Area` is an established stat
   classification (`docs/35-playable-mechs.md:65`), whose membership `docs/36-initial-mech-catalog.md:137`
   defines as "scalable radii, widths, blast areas, projectile bodies, cones, and persistent damage zones",
   with explicit exclusions at `:138`. Naming one dimension would encode a falsehood about a scalar that
   deliberately spans several. The rule binds a field naming a measured dimension of a specific shape, not
   a field naming the Area stat (Ruling 17), and `content/README.md` now records the exemption.
2. **A geometry-naming validator applied to
   `obstacle_free_radius_in_mining_zone_diameters`.** The name is internally odd — a radius in diameters —
   and any mechanical fix (rename the key, or normalize the unit) would have silently chosen one of two
   readings that differ by a factor of two, because `docs/51-…:70` does not say which is meant. The correct
   output is *no change plus an escalation* (Ruling 18). A validator that "fixes" this produces a map
   contract that is quietly wrong in one direction.

The general lesson both cases share: a naming rule that fires on a *token* cannot distinguish a name that
misdescribes a measurement from a name that correctly describes a classification, and it cannot tell a
naming defect from a design ambiguity wearing a naming defect's clothes. Both need a citation check before
a rename, which is why every ruling above quotes the line it relied on.

### Findings still open after this pass

| Finding | Owner needed |
| --- | --- |
| `obstacle_free_radius_in_mining_zone_diameters` — radius or diameter? Factor of two. | document owner (`docs/51`) |
| `REL-07 :: effects.explosion_area_multiplier` is still `null` from the same sentence whose sibling was omitted | integration owner |
| One focus ceiling under two names across `W-AF.json` and `W-AF-coherence-memory.json`, plus a boolean named `changes_focus_maximum_multiplier` | schema stream |
| Percentage-and-factor twins on `REL-04`, `REL-07`, `REL-09` versus the compiler's derived field (`40:95`) | schema stream, at `DAT-006` |
| `world_scale`, `eruption_damage_as_seconds_of_current_segment_damage`, `forward_reach_m`, bare `range`, `barrier_thickness_source`, `shockwave_reach_per_side_m`, `link_length_and_formation_size_scale_with` | schema stream / document owners |
| Formula strings versus a registered formula kind (`40:99`) — already an A17 warning | schema stream |
| Extraction-zone / resonance-field radii (3.0 M / 6.0 M) awaiting a decision-record citation; minute 33's reconstruction markers awaiting a doc correction | document owner (Ruling 22) |
