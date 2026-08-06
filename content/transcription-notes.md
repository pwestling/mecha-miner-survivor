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
No note text was edited, merged, summarised, or dropped. Two of the 141 files
(`content/mechs/shared-baseline.json`, `content/maps/world-props.json`) no longer exist; their
notes are reproduced here under their former paths.

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
- **What the JSON carries:** `utility.radar-unassigned-id.name = "Resource radar"` (Value B, the
  heading form) in `content/localization/en.json`.
- **Affected definitions:** the resource radar
  (`content/utilities/radar-unassigned-id.json`, no stable ID assigned — see the shape notes).
- **Ruling needed:** same reason as C-2. A single canonical English string is now committed to the
  localization catalog; the docs should be reconciled to it.

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
- Still `"id": null`, and still needing a decision — no document assigns these an ID, and
  `40:67` forbids inventing one. Their localization keys therefore use the **filename stem** as
  the `<stable_id>` segment, which is **provisional** and must be rewritten when IDs are minted:
  - `content/utilities/radar-unassigned-id.json` → `utility.radar-unassigned-id.*`
  - `content/mining-sites/standard-ore-seams.json` → `mining_site.standard-ore-seams.name`
  - `content/mining-sites/rich-ore-seams.json` → `mining_site.rich-ore-seams.name`
  - `content/mining-sites/hyper-gold-sites.json` → `mining_site.hyper-gold-sites.name`
  - `content/mining-sites/specialized-material-geodes.json` →
    `mining_site.specialized-material-geodes.name`
  - `content/enemies/elite-modifier-profile.json` — no localized string at all, so no key was
    minted and no stem was used.

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

Still present and **not** touched, flagged for a decision: `shared_rule_refs` on 15 branch
files still holds 30 raw `docs/65-weapon-stat-and-branch-upgrades.md:<line>` strings. It is the
same unstable-line-number channel under a domain-looking field name, and it was outside the
stated scope of this pass.

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

