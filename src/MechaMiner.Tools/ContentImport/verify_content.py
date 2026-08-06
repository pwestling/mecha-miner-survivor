#!/usr/bin/env python3
"""Verify the authored JSON content catalog under content/.

Python 3, standard library only, no dependencies. Run from anywhere:

    python3 src/MechaMiner.Tools/ContentImport/verify_content.py

Exit code is non-zero if any FAILURE is recorded. Warnings never change the
exit code. Failures print before warnings.

================================================================================
ASSERTION TABLE - what this script claims, and the mandate behind each claim
================================================================================

  A1  Every *.json under content/ parses as UTF-8 JSON with no duplicate
      object properties.
      Mandate: docs/technical/40-content-data-and-validation.md:26
      ("duplicate object properties ... are errors")                  FAILURE

  A2  Envelope: every definition file carries schema_version (int),
      content_version (int), status in {development, enabled, disabled,
      retired}, tags (array), and source_refs (non-empty array of strings).
      Mandate: docs/technical/40-content-data-and-validation.md:76-88
      (envelope table); status vocabulary at 40:83                    FAILURE

  A3  name_key and summary_key are both CONDITIONAL. summary_key is "where
      relevant" (40:85); name_key is required only where a definition has a
      genuinely player-facing name, with the compiler supplying the default
      otherwise (40:90, "Optional fields have explicit defaults materialized
      into the canonical bundle") - the same treatment presentation_id gets.
      When either is present it must be a non-empty string that resolves in
      en.json (see A11).                                              FAILURE
      Omission is warned about, and the set of omitting files is asserted
      against NAME_KEY_OMITTED below.                        WARNING + A19

  A4  presentation_id is ABSENT, not present-and-null. No presentation
      entry can exist until content/presentation/ is authored
      (40:88 declares the field; 40:52 declares the directory).        FAILURE

  A5  id must be present and a non-empty string, EXCEPT on the definitions
      listed in ID_NULL_EXPECTED below, where no design document assigns a
      stable ID and minting one would be inventing content. That list is now
      EMPTY - every definition in this tree carries a minted stable ID - so
      the exception has no members and A5 is unconditional.
      Mandate: docs/technical/40-content-data-and-validation.md:80     FAILURE

  A6  An absent or null id is a FAILURE, with its path, unless the definition
      is listed in ID_NULL_EXPECTED. With that list empty, a missing or null
      id always reddens the build; it used to be a warning only while IDs
      were genuinely unminted.                                         FAILURE

  A19 Expected-set guards, so a change in either set is visible rather than
      silently absorbed into the warning list: the set of definitions with a
      null/absent id must equal ID_NULL_EXPECTED, and the set omitting
      name_key must equal NAME_KEY_OMITTED. An unexpected member is a
      failure; a member that has since been fixed is a warning asking for
      the list to be shrunk.                             FAILURE + WARNING

  A7  Naming: no property name anywhere in a definition contains [A-Z] or
      starts with "_". Checked on KEYS ONLY - stable ID/enum/kind tokens in
      VALUES keep their exact case by the same mandate.
      Mandate: docs/technical/40-content-data-and-validation.md:26
      ("Property names use snake_case; stable enum/kind/ID tokens remain
      exact case-sensitive ASCII"), restated at 40:92-100              FAILURE

  A8  No stale extraction metadata: no key named _provenance, _source,
      notes, refs, lines, or line at any depth. Provenance now lives in
      source_refs (40:87); unknown fields are errors (40:90).          FAILURE

  A9  source_refs resolution: for every element, the document-ID portion
      (after any "json.path: " prefix, before any "#anchor") is a doc_id
      declared in the front matter of a file under docs/, and any #anchor
      resolves to a real heading slug in that document.
      Mandate: docs/technical/40-content-data-and-validation.md:87     FAILURE

  A10 Localization: content/localization/en.json parses, is flat (all values
      are strings), is lexically sorted, and has no duplicate keys.
      Mandate: 40:211 (dedicated string catalog), 40:28 (dictionaries
      emitted as lexically sorted key entries)                         FAILURE

  A11 Every name_key / summary_key that IS present (and any other
      *_key / *_keys reference) resolves to a key present in en.json, and no
      key in en.json is orphaned.
      Mandate: docs/technical/40-content-data-and-validation.md:216
      ("Missing release strings are build errors") - so these are
      failures, not warnings                                          FAILURE

  A12 Entry counts per catalog directory match the EXPECTATIONS table.
      Every row cites its own source doc:line.                        FAILURE

  A13 Aggregate row counts match the PROBES table (35 minute rows,
      4 beacon responses, 7 formations, 1 map contract file), AND the four
      authored world-prop VALUES folded into the map contract match the
      document: destructible rock Hull 100 (docs/72:194), rock damage
      footprint diameter 0.80 M (:196), health pack repair 25 Hull (:182),
      health pack pickup radius 0.25 M (:185). Every row cites its own
      source doc:line.
      The world-prop check used to be a row COUNT over key-name patterns,
      which counted patterns that matched at least once - so two names
      existing satisfied it and no value was ever compared. A missing field
      is now a failure rather than a silent pass.                     FAILURE

  A14 Doc-stated totals recompute from the JSON: PowerUp rank prices sum to
      9,450 Hyper Gold and the six option-unlock costs sum to 2,150.
      Actual vs expected is always printed.
      Mandate: 40:136 ("Validators recompute total catalog costs");
      sources docs/62-permanent-powerup-catalog.md:35 and
      docs/63-permanent-option-unlock-catalog.md:48                   FAILURE

  A15 Referential integrity: every branch's weapon reference resolves to a
      file in content/weapons/, every enemy ID referenced by the encounter
      schedule resolves in content/enemies/, and every mech's
      signature-weapon reference resolves. Reference key names are
      DISCOVERED from the data (snake_case), not hardcoded.
      Mandate: docs/technical/40-content-data-and-validation.md:199
      (relational layer: "References, uniqueness, graph coverage")     FAILURE

  A16 Percentage-point policy, checked on NUMBERS and KEY NAMES:
        1. every percent-named property resolves to at least one numeric
           leaf, so a percentage may not live only as prose under a name
           that promises a number;
        2. no percent-named numeric value satisfies 0 < |v| < 1, which would
           be the compiler's normalized factor stored where human-readable
           percentage points belong. Container leaves (minimum, maximum,
           percent, value, points) inherit percent-ness from the nearest
           ancestor key that says percent;
        3. the compiler's normalized factor is never authored: no property
           name combines a percent token with factor/multiplier/fraction/
           normalized, and no object holds both <stem>_percent and a
           same-stem factor sibling.
      A name "says _percent" wherever the token appears, not only at the
      end - 40:95 constrains what the name says and 40:96's terminal-unit
      rule is about unit suffixes, so the 52 mid-name spellings such as
      percent_of_mech_base_speed are correct and are not flagged.
      This REPLACES a prose scan that matched a literal "%" glyph in string
      values: it left a numeric 25 under a non-percent name unchecked while
      emitting 21 warnings about English sentences. None of the three rules
      needs content/schemas/, so all three are failures.              FAILURE

  A17 Formula policy: a player-facing formula must be a registered formula
      kind plus parameters, never a script string. String-valued formula
      expressions are grouped by key name.
      Mandate: docs/technical/40-content-data-and-validation.md:99
      Reported as a warning for the same reason as A16.                WARNING

  A18 Derived-vs-authored regression guard, special-cased to the one known
      transcription bug: the Sentry Pod (W-BE) deployment interval is 6.0
      seconds (docs/71-initial-weapon-numeric-catalog.md:83), and 12 must
      never appear as an authored deployment or ramp value anywhere in
      content/weapons/ - 12 s is the DERIVED time for three pods to exist at
      a 6 s cadence, not an authored number.
      Mandate: docs/technical/40-content-data-and-validation.md:100
      ("Derived values include source operands ... in reports")         FAILURE
      A missing deployment field is only a warning, because the field name
      is unvalidated until content/schemas/ exists.                    WARNING

  A20 No definition carries a compiler-derived footprint value. Two rules
      with two different scopes, because bosses and enemies author different
      halves of their footprint:
        - the contact DIAMETER rule covers content/enemies/ only. An enemy
          authors body_scale_multiplier and its diameter is scale x 0.80 M,
          so storing the diameter puts a second writer on it.
          reference_diameter_m is allowlisted: 0.80 M is the Ripper's
          authored rank-zero diameter, not a per-enemy derived value. A BOSS
          diameter is AUTHORED and must stay - the boss roster gives bosses
          no body-scale column (docs/31:121-128, unlike docs/31:37-48) and
          docs/72:105-110 states the four diameters flat.
        - the CENTRE DISTANCE rule covers content/enemies/,
          content/bosses/ AND content/maps/. It is the object's radius plus
          the player's 0.50 M collision radius in all three, so storing it
          hardcodes a player-baseline constant into a catalog that does not
          own it. content/maps/ joined the rule because the health pack
          stored 0.75 = its authored 0.25 M pickup radius + 0.50 M
          (docs/72:185).
      Checked on KEY NAMES in the covered directories, so a rename inside
      one of them cannot slip past. It does NOT catch the value reappearing
      under an unrelated name or in an uncovered directory - see the
      per-rule scopes above and README.md.
      Mandate: docs/technical/40-content-data-and-validation.md:114
      ("Validation derives world speeds/footprints and compares them with
      the survivability report")                                      FAILURE

  A21 content/ holds exactly EXPECTED_CONTENT_JSON_FILES (139) *.json
      files, so a file in a directory no A12 row covers is still caught.
      The two Markdown files under content/ are listed, not counted.   FAILURE

  A22 Every source_refs scope prefix resolves to a field that EXISTS in the
      definition it annotates. The optional "<json.path>: " prefix attributes
      one property to a document, so a prefix naming a field that no longer
      exists - removed by a ruling, renamed, or never present - is a dangling
      citation, the same defect class as an #anchor pointing at a missing
      heading (A9). The path grammar is dot-separated snake_case segments,
      each optionally suffixed with [] (every element), [N] (one element), or
      [N..M] (a range of elements).
      Mandate: docs/technical/40-content-data-and-validation.md:87
      (source_refs carries "gameplay document IDs/anchors ... implemented"),
      with 40:90 ("Unknown fields are errors") for why a prefix may not name
      a field the definition does not have                            FAILURE

  A24 No string value anywhere under content/ matches docs/.*\.md. A line
      number is unstable wherever it hides, and source_refs (40:87) names
      doc_id#anchor as the only citation form. source_refs itself was
      cleaned in an earlier pass, but 13 citations had moved next door into
      domain fields, where no assertion looked: eleven
      effect.stacking_classification strings, one note, and one field
      literally named beacon_response_source holding a repo path.
      Mandate: docs/technical/40-content-data-and-validation.md:87    FAILURE

  A25 Polarity agreement. Where a structured polarity value (a "direction",
      or any field whose value is drawn from the closed polarity vocabulary
      higher/lower, increase/decrease, more/less, faster/slower,
      longer/shorter, raise/reduce, gain/lose) sits beside prose encoding
      the same fact, the two must agree in sign. Prose is taken from the
      same object and from the enclosing one, because a direction commonly
      sits inside a structured modifier while the prose stays outside it.
      Fires on STRICT contradiction only: prose carrying both signs, as in
      "20% faster without increasing movement speed", is not a
      contradiction and is not reported.
      This automates a check that had to be done by hand - Ruling 22 in
      content/transcription-notes.md verified six geode resonance
      directions against docs/40:104-109 by eye, and nothing in the tree
      would have caught a seventh. Its value does not depend on catching
      anything today.                                                 FAILURE

  A23 One spelling for a bound. No property name abbreviates a bound as the
      token `cap`, `max`, or `min`; the word is spelled out as `maximum` or
      `minimum`, with the qualifier - not the noun - carrying the distinction
      between two bounds on one quantity (`target_minimum` vs `target_maximum`
      vs `hard_maximum`). A cap IS a maximum, so `_cap`, `_max` and `_maximum`
      were three spellings of one concept, twice inside a single object. Where
      a unit suffix must stay terminal (40:96) the bound word moves to the
      front instead: `maximum_control_resistance_percent`, not
      `control_resistance_maximum_percent`.
      Checked on KEY NAMES at any depth, so the abbreviation cannot return
      under a new stem. The only accepted members are the paths listed in
      BOUND_SPELLING_ESCALATED, which is asserted for drift the way A19
      asserts its two sets: an undeclared member is a failure, and a member
      that no longer applies is a warning asking for the list to shrink.
      Mandate: docs/technical/40-content-data-and-validation.md:26
      (snake_case property names) with 40:96 (the unit-suffix rule that fixes
      which end of the name the bound word may occupy)                FAILURE

Not asserted here: no structural JSON Schema validation happens, because
content/schemas/ (40:36) does not exist yet. Domain field names outside the
envelope are therefore unvalidated and will need one reconciliation pass when
the schemas land. See content/transcription-notes.md.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
CONTENT = REPO_ROOT / "content"
DOCS = REPO_ROOT / "docs"
LOCALIZATION = CONTENT / "localization" / "en.json"

# Directories under content/ that are not catalogs of definitions.
NON_DEFINITION_DIRS = ("localization", "schemas")

# A21 - total *.json inventory under content/. This is the sum of the A12 rows
# plus content/localization/en.json, and it is asserted separately so that a
# file appearing in a directory A12 does not cover is still caught.
#
# content/ also holds two Markdown files - README.md and transcription-notes.md
# - which are documentation, not content. So `find content -type f` reports 141
# while this count is 139; that difference is correct and is not a discrepancy.
EXPECTED_CONTENT_JSON_FILES = 139

# --------------------------------------------------------------------------
# A2/A4/A5 - envelope
# --------------------------------------------------------------------------

STATUS_VOCABULARY = ("development", "enabled", "disabled", "retired")
# docs/technical/40-content-data-and-validation.md:83

# A5/A6/A19 - definitions with no stable ID.
#
# Every other definition must carry a non-empty string id, including the
# cohesive aggregates that now have doc-assigned IDs (the standard encounter
# schedule is WAV-01, the standard map generation contract is MGC-01). Two files
# that used to have no ID are gone entirely:
#   - mechs/shared-baseline.json held player baseline values, not mech data. A
#     mech definition carries OVERRIDES (40:110), and content/ has no player or
#     run category yet; the schema stream owns that with PLY-001 as consumer.
#   - maps/world-props.json held the destructible-rock and health-pack values,
#     now fields of the MGC-01 definition. A18 asserts both prop families still
#     appear inside MGC-01.
#
# THIS LIST IS NOW EMPTY, and a missing or null id is therefore an
# unconditional failure (A5/A6). The integration owner minted the last five
# IDs, so nothing in this tree is waiting on one:
#   - the four prose-only mining-site classes
#     (docs/40-mining-and-extraction.md:58-132) are SITE-01..SITE-04, in
#     document order: standard ore seams, rich ore seams, Hyper Gold sites,
#     specialized-material geodes;
#   - content/enemies/shared-elite-modifiers.json is ELT-01. It was previously
#     ID-less on the reading that a constants block "has nothing to be
#     referenced BY". That is superseded: the canonical bundle is ordered by
#     category and stable ID, so a file with no ID has no slot in that
#     ordering. It is now an ordinary addressable definition. It keeps NO
#     name_key - name_key is conditional on a player-facing name (40:84, 40:90)
#     and this block has none - so it stays in NAME_KEY_OMITTED below. Its
#     FILENAME is deliberately unchanged: the bundle orders by the id field,
#     not by the file stem. It is still not an EXPECTATIONS item; it is the
#     enemies directory's one aggregate file, and its id does not match the
#     ^EN-\d{2}$ item selector.
#
# The list is kept, rather than deleted, as the declared place to record a
# future genuinely-unminted ID together with its reason. Adding a member is a
# deliberate act; A19 makes an undeclared one a failure either way.
#
# Three files that used to be listed here are gone from this list:
#   - enemies/elite-modifier-profile.json was DELETED, superseded by the
#     shared-elite-modifiers.json constants block plus the per-enemy
#     elite_eligible field.
#   - resources/geode-resonance-effects.json was DELETED: each resonance effect
#     moved onto the resource that owns it and the field radius onto the geode
#     site class, per the mining-site schema (40:140).
#   - utilities/radar-unassigned-id.json is now utilities/UTL-R1.json. The
#     radar is the thirteenth utility
#     (docs/50-maps-resources-and-navigation.md:106) and the rulings pass gave
#     it the stable ID UTL-R1 and the player-facing name "Resource radar", so
#     it is an ordinary item in the utilities count and belongs in neither list.
ID_NULL_EXPECTED: frozenset[str] = frozenset()

# A3/A19 - definitions that legitimately omit name_key.
#
# name_key is required only where a definition has a genuinely player-facing
# name. None of these three is player-facing: WAV-01 and MGC-01 are authoring
# contracts, and shared-elite-modifiers (now ELT-01) is a constants block, not
# an entity the UI ever names. Putting their titles in the localization catalog
# would imply a UI surface that does not exist, so the compiler supplies the
# default instead (40:90).
#
# Minting ELT-01 did not change this set. Having a stable ID and having a
# player-facing name are independent: the ID makes the block addressable and
# orderable in the canonical bundle, while name_key stays conditional on there
# being a name to localize. ELT-01 is the same path that was already listed
# here, so the set is unchanged at three members.
#
# The two files removed from this list are the same deletions and the same
# rename described above ID_NULL_EXPECTED: elite-modifier-profile.json and
# geode-resonance-effects.json no longer exist, and the radar is now UTL-R1 with
# the real name_key utility.UTL-R1.name.
NAME_KEY_OMITTED = frozenset(
    {
        "content/encounters/standard-encounter-schedule.json",
        "content/enemies/shared-elite-modifiers.json",
        "content/maps/standard-map-generation-contract.json",
    }
)

# --------------------------------------------------------------------------
# A23 - one spelling for a bound
#
# A cap is a maximum. Before this pass the same upper bound was spelled `_cap`,
# `_max` and `_maximum` - twice inside a single object in two files - and `_min`
# sat beside `_minimum` the same way. The word is now always spelled out, and
# the qualifier rather than the noun carries the distinction between two bounds
# on one quantity: `{target_minimum, target_maximum, hard_maximum}`, not
# `{target_min, target_max, hard_max}`.
#
# Where the name carries a unit suffix, the unit stays terminal (40:96) and the
# bound word moves to the front: `maximum_control_resistance_percent`,
# `maximum_pursuit_duration_seconds`, `minimum_percent`.
BOUND_ABBREVIATIONS = frozenset({"cap", "max", "min"})

# A23 - the accepted exceptions, as "<file>::<property name>".
#
# Both are in one object in one file, and they hold DIFFERENT values:
# effects.contact_damage_speed_bonus_percent_max is {percent: 200} while
# effects.contact_damage_percent_cap is {percent: 400}. That is either two real
# bounds wearing two spellings or one bound duplicated with a wrong number, and
# the two need opposite treatment - rename both, or delete one. Renaming them to
# a single spelling before that is decided would leave two identical stems
# holding different numbers, which reads as a duplicate with a typo and hides
# the collision the audit found. They are therefore left exactly as authored,
# escalated to the document owner, and declared here so the exception is visible
# rather than absorbed.
# Now EMPTY. The two W-BF-tethered-reaper members are resolved, not suppressed:
# docs/71:346 reads "Its contact Damage is `200% + up to 200%` of current Damage,
# scaling linearly with blade world speed ... and capped at 400%", so 200 bounds
# the speed-bonus COMPONENT (the "up to 200%" addend) and 400 bounds the TOTAL
# (200% base + 200% maximum bonus). They are two different bounds on two different
# quantities, so both values stay and the qualifier carries the distinction:
# maximum_speed_bonus_percent and maximum_total_contact_damage_percent. A stale
# exception is worse than none, so the list is emptied rather than left carrying
# a resolved escalation.
BOUND_SPELLING_ESCALATED = frozenset()

# --------------------------------------------------------------------------
# A8 - stale extraction metadata that must not survive anywhere
# --------------------------------------------------------------------------

FORBIDDEN_KEYS = (
    "_provenance",
    "_source",
    "notes",
    # The singular "note" was missing while "notes" was blocked, so three keys
    # survived - two on MCH-06 restating a movement speed docs/72:55,57 already
    # state, which is the same category deleted from all ten enemies.
    "note",
    "refs",
    "lines",
    "line",
    # shared_rule_refs was a second, parallel provenance carrier; its content
    # belongs in source_refs, which is the only carrier the envelope names.
    "shared_rule_refs",
)

# --------------------------------------------------------------------------
# A12 - per-directory entry counts
#
# selector  : how a file is classified as a catalog "item"
#             ("id_regex", pattern) - top-level "id" matches pattern
#             ("has_key", key)      - top-level object has that key
#             ("any_file",)         - every .json file in the directory
# items     : expected number of item files (None = report, do not assert)
# aggregates: expected number of non-item files (None = report only)
# --------------------------------------------------------------------------

EXPECTATIONS = [
    dict(
        dir="resources",
        selector=("id_regex", r"^(?:[A-F]|common-ore|hyper-gold)$"),
        items=8,
        aggregates=None,
        label="resources (6 specialized + common ore + Hyper Gold)",
        source="docs/61-specialized-resource-identities.md:20 + docs/60-resources-crafting-progression.md:18",
    ),
    dict(
        dir="mechs",
        selector=("id_regex", r"^MCH-\d{2}$"),
        items=6,
        aggregates=0,
        # No shared-baseline file: a mech definition carries overrides (40:110)
        # and the player baseline belongs to a player/run category the schema
        # stream owns (consumer PLY-001).
        label="mechs (no baseline aggregate)",
        source="docs/36-initial-mech-catalog.md:45",
    ),
    dict(
        dir="enemies",
        selector=("id_regex", r"^EN-\d{2}$"),
        items=10,
        aggregates=1,
        # The one aggregate is shared-elite-modifiers.json, the shared elite
        # constants block. It is not an eleventh enemy: it has no id and no
        # name_key (see ID_NULL_EXPECTED), so the id_regex selector correctly
        # buckets it as the directory's aggregate. The former
        # elite-modifier-profile.json definition it replaced is deleted.
        label="ordinary enemies (+ 1 shared elite modifier constants block)",
        source="docs/31-initial-alien-roster.md:37 + docs/31-initial-alien-roster.md:104",
    ),
    dict(
        dir="bosses",
        selector=("id_regex", r"^BOSS-\d{2}$"),
        items=4,
        aggregates=None,
        label="interval bosses",
        source="docs/31-initial-alien-roster.md:121",
    ),
    dict(
        dir="weapons",
        selector=("id_regex", r"^W-[A-F]{2}$"),
        items=15,
        aggregates=1,
        label="base weapons (+ 1 shared stat price formula aggregate)",
        source="docs/66-weapon-catalog-and-resource-graph.md:39 + docs/65-weapon-stat-and-branch-upgrades.md:44",
    ),
    dict(
        dir="branches",
        selector=("has_key", "weapon_id"),
        items=45,
        aggregates=0,
        label="weapon branches (15 weapons x 3)",
        source="docs/71-initial-weapon-numeric-catalog.md:130",
    ),
    dict(
        dir="utilities",
        # UTL-R1, the resource radar, is a utility with a stable ID like any
        # other, so the selector admits it. The old ^UTL-[A-F][12]$ pattern
        # matched only the twelve material utilities and therefore counted the
        # radar as an aggregate file, which it is not.
        selector=("id_regex", r"^UTL-(?:[A-F][12]|R1)$"),
        items=13,
        aggregates=0,
        label="utilities (12 material UTL-* + the resource radar UTL-R1)",
        source="docs/68-utility-catalog.md:35 + docs/50-maps-resources-and-navigation.md:106",
    ),
    dict(
        dir="relics",
        selector=("id_regex", r"^REL-\d{2}$"),
        items=10,
        aggregates=0,
        label="relics",
        source="docs/69-initial-relic-catalog.md:26",
    ),
    dict(
        dir="powerups",
        selector=("id_regex", r"^PU-[A-Z]\d{2}$"),
        items=13,
        aggregates=0,
        label="permanent PowerUps",
        source="docs/62-permanent-powerup-catalog.md:35",
    ),
    dict(
        dir="unlocks",
        selector=("id_regex", r"^UNL-\d{2}$"),
        items=6,
        aggregates=0,
        label="permanent option unlocks",
        source="docs/63-permanent-option-unlock-catalog.md:48",
    ),
    dict(
        dir="mining-sites",
        selector=("any_file",),
        items=4,
        aggregates=None,
        label="mining site classes (standard seam, rich seam, geode, Hyper Gold site)",
        source="docs/40-mining-and-extraction.md:58",
    ),
    dict(
        dir="encounters",
        selector=("any_file",),
        items=1,
        aggregates=None,
        label="standard encounter schedule (one cohesive aggregate)",
        source="docs/32-standard-wave-and-beacon-schedule.md:54",
    ),
    dict(
        dir="maps",
        selector=("any_file",),
        items=1,
        aggregates=None,
        # One MGC-01 definition; the destructible-rock and health-pack values
        # are fields of it, not a separate world-props file.
        label="standard map generation contract (world props folded in as fields)",
        source="docs/51-standard-map-generation-contract.md:1 + docs/72-player-survivability-and-damage-baseline.md:180",
    ),
]

# --------------------------------------------------------------------------
# A13 - row counts inside aggregate files.
#
# "array_at_path" sums the lengths of arrays whose JSON path matches the
# regex, so the key names are discovered from the data rather than assumed.
# --------------------------------------------------------------------------

PROBES = [
    dict(
        dir="encounters",
        label="35 minute rows (minutes 0-34)",
        expected=35,
        kind="array_at_path",
        pattern=r"\.minute_rows$",
        source="docs/32-standard-wave-and-beacon-schedule.md:54",
    ),
    dict(
        dir="encounters",
        label="Hyper Gold threat-beacon responses",
        expected=4,
        kind="array_at_path",
        pattern=r"beacon[^.]*\.responses$",
        source="docs/32-standard-wave-and-beacon-schedule.md:100",
    ),
    dict(
        dir="encounters",
        label="formation grammar entries",
        expected=7,
        kind="array_at_path",
        pattern=r"^\$\.(?:spawn_)?formations$",
        source="docs/32-standard-wave-and-beacon-schedule.md:27",
    ),
    dict(
        dir="maps",
        label="map generation contract file",
        expected=1,
        kind="files_matching",
        pattern=r"contract",
        source="docs/51-standard-map-generation-contract.md:1",
    ),
]

# --------------------------------------------------------------------------
# A13 - the two world-prop families, asserted on their VALUES.
#
# WHAT THIS REPLACED. The world-prop probe used to be kind="key_families" with
# expected=2, which counted PATTERNS THAT MATCHED AT LEAST ONCE. The existence of
# one key containing "rock" and one containing "health_pack" satisfied it, so
# emptying both objects and setting rock Hull to 1 and the footprint to 9.9 left
# the probe green. It asserted that two names existed, not that any value was
# right.
#
# The replacement names the four authored values and cites each one. The key is
# LOCATED by regex, discovered from the data like the rest of A13, but the value
# is compared - and a missing field is a failure, not a silent pass, which is the
# specific hole the old probe had.
# --------------------------------------------------------------------------

WORLD_PROP_VALUES = (
    (
        "destructible rock Hull",
        re.compile(r"(?i)destructible_rock(?:_rules)?"),
        re.compile(r"(?i)^hull$"),
        100,
        "docs/72-player-survivability-and-damage-baseline.md:194",
    ),
    (
        "destructible rock damage footprint diameter (M)",
        re.compile(r"(?i)destructible_rock(?:_rules)?"),
        re.compile(r"(?i)damage_footprint_diameter"),
        0.80,
        "docs/72-player-survivability-and-damage-baseline.md:196",
    ),
    (
        "health pack repair (Hull)",
        re.compile(r"(?i)health_pack"),
        re.compile(r"(?i)repair.*hull|hull.*repair"),
        25,
        "docs/72-player-survivability-and-damage-baseline.md:182",
    ),
    (
        "health pack pickup radius (M)",
        re.compile(r"(?i)health_pack"),
        re.compile(r"(?i)pickup_radius"),
        0.25,
        "docs/72-player-survivability-and-damage-baseline.md:185",
    ),
)

def check_world_prop_values(docs: dict[Path, object]) -> list[tuple]:
    """A13 - the four authored world-prop values, each against its own doc:line."""
    rows = []
    present = files_in("maps", docs)
    for label, family_rx, key_rx, expected, source in WORLD_PROP_VALUES:
        found: list[tuple[str, object]] = []
        for path, doc in sorted(present.items()):
            for jpath, key, value in walk(doc):
                if not key or not key_rx.search(key):
                    continue
                if not family_rx.search(jpath):
                    continue
                if isinstance(value, bool) or not isinstance(value, (int, float)):
                    continue
                found.append((f"{rel(path)}{jpath[1:]}", value))
        wrong = [f"{p} = {v}" for p, v in found if float(v) != float(expected)]
        if not found:
            status = "FAIL"
            actual = "no field found"
            fail(
                f"A13 {label}: no numeric field matching /{key_rx.pattern}/ inside a "
                f"/{family_rx.pattern}/ object exists in content/maps/, so the value "
                f"{expected} at {source} is unasserted"
            )
        else:
            status = "ok" if not wrong else "FAIL"
            actual = ", ".join(f"{v}" for _, v in found)
            if wrong:
                fail(f"A13 {label} must be {expected} ({source}): {wrong}")
        rows.append((f"{label} [{source.split(':')[-1]}]", expected, actual, status))
    return rows


# --------------------------------------------------------------------------
# A14 - doc-stated grand totals the JSON must reproduce
# --------------------------------------------------------------------------

POWERUP_TOTAL_HYPER_GOLD = 9450  # docs/62-permanent-powerup-catalog.md:35
UNLOCK_TOTAL_HYPER_GOLD = 2150  # docs/63-permanent-option-unlock-catalog.md:48

# --------------------------------------------------------------------------
# A16 / A17 - reconciliation heuristics
# --------------------------------------------------------------------------

# A16 - the NUMERIC percentage-point policy of
# docs/technical/40-content-data-and-validation.md:95:
#
#   "Percentages in authoring use human-readable percentage points only when the
#    property name says `_percent`; the compiler writes normalized factors into
#    the runtime bundle as a separate derived field."
#
# WHAT THIS USED TO BE, AND WHY IT WAS REPLACED. A16 previously matched a literal
# "%" glyph in STRING values and warned when the key was not named *_percent.
# That checks prose, not the rule: a numeric 25 under a non-percent name was not
# even warned, while 131 English sentences containing a percent sign were. A
# warning list a reader learns to ignore is worse than no list, so the prose scan
# is gone and the three rules below run on NUMBERS and KEY NAMES instead. None of
# them needs content/schemas/, so all three are failures rather than warnings.
#
# A NAME "SAYS _percent" WHEREVER THE TOKEN APPEARS, not only at the end.
# 40:95 constrains what the name says; 40:96's terminal-unit rule is about unit
# suffixes. 52 names in this tree put the token mid-name
# (percent_of_mech_base_speed, shockwave_damage_percent_of_current_damage, ...)
# and every one of them is correct, so a rule demanding a TERMINAL _percent would
# have forced 52 renames no document asks for.
PERCENT_TOKEN_KEY = re.compile(r"(?i)(?:^|_)percent(?:age)?(?:_points?)?(?:$|_)")
# Structural container keys: a numeric leaf under one of these inherits the
# percent-ness of the nearest ancestor key that says percent, so {"percent": 20}
# and {"minimum": 40, "maximum": 80} are checked as percentage points.
PERCENT_CONTAINER_KEY = frozenset({"minimum", "maximum", "percent", "value", "points"})
# The compiler-owned normalized factor. Authoring it puts a second writer on a
# derived field, which is the second half of 40:95.
NORMALIZED_FACTOR_TOKEN = re.compile(
    r"(?i)(?:^|_)(?:factor|multiplier|fraction|normalized|normalised)(?:$|_)"
)
FORMULA_KEY = re.compile(r"(?:^|_)(?:formula|formulas|expression|equation)s?$")
# A pure algebraic expression: operators present, no word longer than three
# letters (so prose containing a formula is not flagged), short.
FORMULA_EXPRESSION = re.compile(r"^[\s\w().,+*/^×·÷-]{3,60}$")
# Deliberately narrow, to keep the warning list actionable. A stable ID token
# ("EN-01"), a line range ("65-71"), a ratio ("8/10"), and a bare percentage
# ("+3%") are NOT formulas, so plain +, - and / never qualify on their own. A
# value qualifies only if it contains a digit plus an explicit multiplicative or
# exponent operator, or an implicit coefficient in front of a parenthesis as in
# "5n(n + 1)". Formulas written with only + and - under a key whose name does
# not say formula/expression/equation are therefore not detected here.
FORMULA_OPERATOR = re.compile(r"[*^×·÷]|\d\s*[A-Za-z]?\s*\(")
LONG_WORD = re.compile(r"[A-Za-z]{4,}")

failures: list[str] = []
warnings: list[str] = []


def fail(msg: str) -> None:
    failures.append(msg)


def warn(msg: str) -> None:
    warnings.append(msg)


def rel(p: Path) -> str:
    try:
        return str(p.relative_to(REPO_ROOT))
    except ValueError:
        return str(p)


# --------------------------------------------------------------------------
# A1 - parse everything, rejecting duplicate object properties
# --------------------------------------------------------------------------


def _no_duplicate_keys(pairs):
    seen: dict[str, int] = {}
    for key, _ in pairs:
        seen[key] = seen.get(key, 0) + 1
    dupes = sorted(k for k, n in seen.items() if n > 1)
    if dupes:
        raise ValueError(f"duplicate object properties: {dupes}")
    return dict(pairs)


def load_json(path: Path):
    """Return the parsed document, or None after recording a failure."""
    try:
        with path.open(encoding="utf-8") as fh:
            return json.load(fh, object_pairs_hook=_no_duplicate_keys)
    except (json.JSONDecodeError, UnicodeDecodeError, ValueError) as exc:
        fail(f"PARSE ERROR {rel(path)}: {exc}")
        return None


def load_definitions() -> dict[Path, object]:
    """All *.json under content/ except the non-definition directories."""
    docs: dict[Path, object] = {}
    if not CONTENT.is_dir():
        fail(f"content/ directory not found at {CONTENT}")
        return docs
    for path in sorted(CONTENT.rglob("*.json")):
        parts = path.relative_to(CONTENT).parts
        if parts and parts[0] in NON_DEFINITION_DIRS:
            continue
        doc = load_json(path)
        if doc is not None:
            docs[path] = doc
    return docs


# --------------------------------------------------------------------------
# traversal helpers
# --------------------------------------------------------------------------


def walk(obj, path="$"):
    """Yield (json_path, key_or_None, value) for every node."""
    if isinstance(obj, dict):
        for key, value in obj.items():
            yield f"{path}.{key}", key, value
            yield from walk(value, f"{path}.{key}")
    elif isinstance(obj, list):
        for index, value in enumerate(obj):
            yield f"{path}[{index}]", None, value
            yield from walk(value, f"{path}[{index}]")


def walk_with_ancestry(obj, path="$", ancestors=()):
    """Like walk(), plus the tuple of dict keys enclosing the node.

    A16 needs it: a numeric leaf named "minimum" inside "favorable_horde_damage_percent"
    is a percentage-point value, and only the ancestry says so.
    """
    if isinstance(obj, dict):
        for key, value in obj.items():
            yield f"{path}.{key}", key, value, ancestors
            yield from walk_with_ancestry(value, f"{path}.{key}", ancestors + (key,))
    elif isinstance(obj, list):
        for index, value in enumerate(obj):
            yield f"{path}[{index}]", None, value, ancestors
            yield from walk_with_ancestry(value, f"{path}[{index}]", ancestors)


def numeric_leaves(obj):
    """Yield every int/float leaf in a subtree, booleans excluded."""
    if isinstance(obj, bool):
        return
    if isinstance(obj, (int, float)):
        yield obj
    elif isinstance(obj, dict):
        for value in obj.values():
            yield from numeric_leaves(value)
    elif isinstance(obj, list):
        for value in obj:
            yield from numeric_leaves(value)


def files_in(directory: str, docs: dict[Path, object]) -> dict[Path, object]:
    base = CONTENT / directory
    return {p: d for p, d in docs.items() if p.parent == base or base in p.parents}


def is_item(doc, selector) -> bool:
    kind = selector[0]
    if kind == "any_file":
        return True
    if not isinstance(doc, dict):
        return False
    if kind == "id_regex":
        value = doc.get("id")
        return isinstance(value, str) and re.match(selector[1], value) is not None
    if kind == "has_key":
        return selector[1] in doc
    raise ValueError(f"unknown selector {selector!r}")


# --------------------------------------------------------------------------
# A9 - document ID and heading-anchor index built from docs/ front matter
# --------------------------------------------------------------------------

FENCE = re.compile(r"^\s*(?:```|~~~)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")


def slugify(heading: str) -> str:
    """GitHub-compatible heading slug.

    Formatting is stripped, the text is lowercased, punctuation other than
    hyphens/underscores is deleted, and each remaining whitespace character
    becomes one hyphen. Runs of whitespace are NOT collapsed: a heading such
    as "BOSS-01 - Riftjaw" written with an em dash slugs to
    "boss-01--riftjaw", because the dash is deleted and both surrounding
    spaces survive as hyphens.
    """
    text = re.sub(r"`([^`]*)`", r"\1", heading)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"[*~]", "", text)
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text)
    return re.sub(r"\s", "-", text)


def build_doc_index() -> dict[str, dict]:
    """Map doc_id -> {"path": ..., "anchors": {slug, ...}}."""
    index: dict[str, dict] = {}
    if not DOCS.is_dir():
        fail(f"docs/ directory not found at {DOCS}")
        return index
    for path in sorted(DOCS.rglob("*.md")):
        lines = path.read_text(encoding="utf-8").splitlines()
        if not lines or lines[0].strip() != "---":
            continue
        doc_id = None
        for line in lines[1:]:
            if line.strip() == "---":
                break
            match = re.match(r"\s*doc_id\s*:\s*(\S+)\s*$", line)
            if match:
                doc_id = match.group(1).strip("\"'")
        if not doc_id:
            continue
        anchors: dict[str, int] = {}
        in_fence = False
        for line in lines:
            if FENCE.match(line):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            match = HEADING.match(line)
            if not match:
                continue
            slug = slugify(match.group(2))
            if not slug:
                continue
            count = anchors.get(slug, 0)
            anchors[slug] = count + 1
        expanded = set()
        for slug, count in anchors.items():
            expanded.add(slug)
            for n in range(1, count):
                expanded.add(f"{slug}-{n}")
        if doc_id in index:
            fail(
                f"duplicate doc_id '{doc_id}' declared by {rel(index[doc_id]['path'])} "
                f"and {rel(path)}"
            )
            continue
        index[doc_id] = dict(path=path, anchors=expanded)
    return index


def split_source_ref(ref: str) -> tuple[str, str | None]:
    """'field.path: DOC-ID#anchor' -> ('DOC-ID', 'anchor')."""
    target = ref.split(": ", 1)[1] if ": " in ref else ref
    target = target.strip()
    if "#" in target:
        doc_id, anchor = target.split("#", 1)
        return doc_id.strip(), anchor.strip()
    return target, None


def source_ref_scope_prefix(ref: str) -> str | None:
    """'field.path: DOC-ID#anchor' -> 'field.path'; a bare ref -> None."""
    if ": " not in ref:
        return None
    prefix = ref.split(": ", 1)[0].strip()
    return prefix or None


# --------------------------------------------------------------------------
# A22 - a scope prefix must name a field that exists in the definition
#
# The prefix grammar is dot-separated snake_case segments, each optionally
# suffixed with one or more selectors:
#   name       a property
#   name[]     every element of an array property
#   name[3]    one element by index
#   name[2..3] a range of elements by index
# Descending through an array without a selector is also accepted, so
# "unlocks.utilities[].utility_id" and "unlocks.utilities.utility_id" both
# resolve: the segment then has to exist on at least one element.
# --------------------------------------------------------------------------

SCOPE_SEGMENT = re.compile(r"([a-z0-9_]+)((?:\[(?:\d+(?:\.\.\d+)?)?\])*)$")
SCOPE_SELECTOR = re.compile(r"\[(\d+)?(?:\.\.(\d+))?\]")


def split_scope_prefix(prefix: str) -> list[str]:
    """Split on '.' separators only, so the '..' inside 'rules[2..3]' survives."""
    segments: list[str] = [""]
    depth = 0
    for char in prefix:
        if char == "[":
            depth += 1
        elif char == "]":
            depth -= 1
        if char == "." and depth == 0:
            segments.append("")
            continue
        segments[-1] += char
    return segments


def resolve_scope_prefix(doc, prefix: str) -> tuple[bool, str]:
    """Does `prefix` name a field present in `doc`? -> (ok, reason)."""
    current = [doc]
    for segment in split_scope_prefix(prefix):
        match = SCOPE_SEGMENT.fullmatch(segment)
        if not match:
            return False, f"segment {segment!r} is not a snake_case path segment"
        name, selectors = match.group(1), match.group(2)
        holders = [n for n in current if isinstance(n, dict)]
        # An unselected array is transparent: look for the name on its elements.
        for node in current:
            if isinstance(node, list):
                holders.extend(e for e in node if isinstance(e, dict))
        if not holders:
            return False, f"nothing to hold {name!r} (reached a non-object)"
        found = [h[name] for h in holders if name in h]
        if not found:
            return False, f"{name!r} is not a field of the definition"
        for selector in SCOPE_SELECTOR.finditer(selectors):
            arrays = [n for n in found if isinstance(n, list)]
            if not arrays:
                return False, f"{name}{selectors} indexes {name!r}, which is not an array"
            start, end = selector.group(1), selector.group(2)
            if start is None:
                found = [e for a in arrays for e in a]
                if not found:
                    return False, f"{name}[] indexes an empty array"
                continue
            lo = int(start)
            hi = int(end) if end is not None else lo
            if hi < lo:
                return False, f"{name}[{lo}..{hi}] is an inverted range"
            picked = [a[i] for a in arrays for i in range(lo, hi + 1) if i < len(a)]
            if len(picked) != len(arrays) * (hi - lo + 1):
                return False, f"{name}[{lo}..{hi}] is out of range for {name!r}"
            found = picked
        current = found
    return True, "ok"


def check_scope_prefixes(docs: dict[Path, object]) -> list[tuple]:
    """A22 - every source_refs scope prefix resolves in its own definition."""
    prefixed = 0
    dangling: list[str] = []
    for path, doc in sorted(docs.items()):
        if not isinstance(doc, dict) or not isinstance(doc.get("source_refs"), list):
            continue
        for ref in doc["source_refs"]:
            if not isinstance(ref, str):
                continue
            prefix = source_ref_scope_prefix(ref)
            if prefix is None:
                continue
            prefixed += 1
            ok, reason = resolve_scope_prefix(doc, prefix)
            if not ok:
                dangling.append(f"{rel(path)}: {ref!r} - {reason}")
                fail(
                    f"{rel(path)}: source_refs {ref!r} has scope prefix {prefix!r}, which does "
                    f"not resolve in this definition ({reason}). A citation must annotate a field "
                    f"that exists: re-point it at the surviving field it documents, or drop the "
                    f"prefix and keep it file-level - never delete a citation that is the only "
                    f"support for a value still present (40:87, 40:90)"
                )
    return [
        (
            "source_refs scope prefixes resolve to an existing field",
            prefixed,
            f"{len(dangling)} dangling",
            "ok" if not dangling else "FAIL",
        )
    ]


# --------------------------------------------------------------------------
# A2-A9 - per-definition checks
# --------------------------------------------------------------------------


def check_bound_spelling(docs: dict[Path, object]) -> list[tuple]:
    """A23 - no property name abbreviates a bound as cap/max/min."""
    seen: set[str] = set()
    offenders: list[str] = []
    for path, doc in sorted(docs.items()):
        for jpath, key, _ in walk(doc):
            if not key or not (set(key.split("_")) & BOUND_ABBREVIATIONS):
                continue
            marker = f"{rel(path)}::{key}"
            seen.add(marker)
            if marker not in BOUND_SPELLING_ESCALATED:
                offenders.append(f"{rel(path)}{jpath[1:]}")
    resolved = sorted(BOUND_SPELLING_ESCALATED - seen)
    rows = [
        (
            "property names spell a bound out as maximum/minimum",
            0,
            f"{len(offenders)} abbreviated",
            "ok" if not offenders else "FAIL",
        ),
        (
            "declared BOUND_SPELLING_ESCALATED exceptions",
            len(BOUND_SPELLING_ESCALATED),
            f"{len(BOUND_SPELLING_ESCALATED) - len(resolved)} still present",
            "ok" if not resolved else "WARN",
        ),
    ]
    if offenders:
        fail(
            f"{len(offenders)} property name(s) abbreviate a bound as 'cap', 'max' or 'min'; a cap "
            f"is a maximum and the word is spelled out, with the unit suffix kept terminal (40:26, "
            f"40:96): {offenders[:15]}"
        )
    if resolved:
        warn(
            f"{len(resolved)} member(s) of BOUND_SPELLING_ESCALATED no longer exist, so the "
            f"escalation is settled: {resolved}. Shrink BOUND_SPELLING_ESCALATED."
        )
    return rows


def check_definitions(docs: dict[Path, object], doc_index: dict[str, dict]) -> dict:
    stats = dict(
        checked=0,
        missing_id=[],
        null_id=[],
        no_id=set(),
        no_name_key=set(),
        source_refs=0,
        formula_hits={},
        key_refs=set(),
        envelope_key_refs=set(),
    )
    for path, doc in sorted(docs.items()):
        name = rel(path)
        if not isinstance(doc, dict):
            fail(f"{name}: top-level JSON value is {type(doc).__name__}, expected an object")
            continue
        stats["checked"] += 1

        # ---- A5/A6 id: absent or null fails unless declared in
        #      ID_NULL_EXPECTED, which is now empty ----
        if "id" not in doc or doc["id"] is None:
            (stats["missing_id"] if "id" not in doc else stats["null_id"]).append(name)
            stats["no_id"].add(name)
            state = "no top-level 'id'" if "id" not in doc else "top-level 'id' is null"
            if name in ID_NULL_EXPECTED:
                warn(f"{name}: {state}; declared in ID_NULL_EXPECTED (40:80)")
            else:
                fail(
                    f"{name}: {state}; every definition must carry a stable category-valid ID "
                    f"(40:80). Add the minted ID, or list the file in ID_NULL_EXPECTED with the "
                    f"reason no document assigns one"
                )
        elif not isinstance(doc["id"], str) or not doc["id"].strip():
            fail(f"{name}: 'id' is {doc['id']!r}, expected a non-empty string (40:80)")

        # ---- A2 envelope ----
        for field in ("schema_version", "content_version"):
            value = doc.get(field)
            if field not in doc:
                fail(f"{name}: missing required '{field}' (40:81-82)")
            elif isinstance(value, bool) or not isinstance(value, int):
                fail(f"{name}: '{field}' is {value!r}, expected an integer (40:81-82)")

        if "status" not in doc:
            fail(f"{name}: missing required 'status' (40:83)")
        elif doc["status"] not in STATUS_VOCABULARY:
            fail(
                f"{name}: status {doc['status']!r} is not one of "
                f"{list(STATUS_VOCABULARY)} (40:83)"
            )

        if "tags" not in doc:
            fail(f"{name}: missing required 'tags' array (40:86)")
        elif not isinstance(doc["tags"], list):
            fail(f"{name}: 'tags' is {type(doc['tags']).__name__}, expected an array (40:86)")

        if "source_refs" not in doc:
            fail(f"{name}: missing required 'source_refs' array (40:87)")
        elif not isinstance(doc["source_refs"], list) or not doc["source_refs"]:
            fail(f"{name}: 'source_refs' must be a non-empty array (40:87)")
        elif not all(isinstance(r, str) and r.strip() for r in doc["source_refs"]):
            fail(f"{name}: every 'source_refs' element must be a non-empty string (40:87)")
        else:
            # ---- A9 resolution ----
            for ref in doc["source_refs"]:
                stats["source_refs"] += 1
                doc_id, anchor = split_source_ref(ref)
                entry = doc_index.get(doc_id)
                if entry is None:
                    fail(
                        f"{name}: source_refs {ref!r} names doc_id {doc_id!r}, which no file "
                        f"under docs/ declares in its front matter (40:87)"
                    )
                    continue
                if anchor and anchor not in entry["anchors"]:
                    fail(
                        f"{name}: source_refs {ref!r} anchor '#{anchor}' is not a heading in "
                        f"{rel(entry['path'])} (40:87)"
                    )

        # ---- A3 name_key is conditional on the definition being player-facing ----
        if "name_key" not in doc:
            stats["no_name_key"].add(name)
            warn(
                f"{name}: no 'name_key'; accepted only for a definition with no player-facing "
                f"name, with the compiler supplying the default (40:84, 40:90)"
            )
        elif not isinstance(doc["name_key"], str) or not doc["name_key"].strip():
            fail(
                f"{name}: 'name_key' is {doc['name_key']!r}; when present it must be a non-empty "
                f"string (40:84)"
            )

        # ---- A3 summary_key is conditional ----
        if "summary_key" in doc and (
            not isinstance(doc["summary_key"], str) or not doc["summary_key"].strip()
        ):
            fail(
                f"{name}: 'summary_key' is {doc['summary_key']!r}; when present it must be a "
                f"non-empty string (40:85)"
            )

        # ---- A4 presentation_id must be absent ----
        if "presentation_id" in doc:
            fail(
                f"{name}: 'presentation_id' is present ({doc['presentation_id']!r}); it must be "
                f"omitted entirely until content/presentation/ exists (40:52, 40:88)"
            )

        # ---- A7/A8/A11/A16/A17 - one traversal ----
        for jpath, key, value in walk(doc):
            if key is None:
                continue
            if re.search(r"[A-Z]", key):
                fail(f"{name}{jpath[1:]}: property name '{key}' contains uppercase (40:26)")
            if key.startswith("_"):
                fail(f"{name}{jpath[1:]}: property name '{key}' starts with '_' (40:26, 40:90)")
            if key in FORBIDDEN_KEYS:
                fail(
                    f"{name}{jpath[1:]}: stale extraction metadata key '{key}'; provenance "
                    f"belongs in source_refs (40:87, 40:90)"
                )
            if key.endswith("_key") and isinstance(value, str) and value.strip():
                stats["key_refs"].add(value)
                if key in ("name_key", "summary_key"):
                    stats["envelope_key_refs"].add(value)
            if key.endswith("_keys") and isinstance(value, list):
                for element in value:
                    if isinstance(element, str) and element.strip():
                        stats["key_refs"].add(element)
            if isinstance(value, str):
                if looks_like_formula(key, value):
                    stats["formula_hits"].setdefault(key, []).append(f"{name}{jpath[1:]}")
    return stats


def looks_like_formula(key: str, value: str) -> bool:
    text = value.strip()
    if not text:
        return False
    if FORMULA_KEY.search(key):
        return True
    if len(text) > 60 or LONG_WORD.search(text) or not re.search(r"\d", text):
        return False
    return bool(FORMULA_EXPRESSION.match(text) and FORMULA_OPERATOR.search(text))


def check_expected_sets(stats: dict) -> list[tuple]:
    """A19 - the exception sets must not drift silently."""
    rows = []
    for label, actual, expected, hint in (
        (
            "definitions with a null/absent id",
            stats["no_id"],
            ID_NULL_EXPECTED,
            "ID_NULL_EXPECTED",
        ),
        (
            "definitions omitting name_key",
            stats["no_name_key"],
            NAME_KEY_OMITTED,
            "NAME_KEY_OMITTED",
        ),
    ):
        unexpected = sorted(actual - expected)
        resolved = sorted(expected - actual)
        rows.append(
            (
                label,
                len(expected),
                f"{len(actual)} ({len(unexpected)} unexpected, {len(resolved)} resolved)",
                "ok" if not unexpected and not resolved else "FAIL" if unexpected else "WARN",
            )
        )
        if unexpected:
            fail(
                f"{len(unexpected)} definition(s) newly in '{label}' and not listed in {hint}: "
                f"{unexpected}. Either fix the definition or add it to {hint} with the reason."
            )
        if resolved:
            warn(
                f"{len(resolved)} definition(s) listed in {hint} no longer belong there "
                f"({label} no longer applies): {resolved}. Shrink {hint}."
            )
    return rows


def report_reconciliation(stats: dict) -> None:
    """A17 only. A16 is check_percentage_point_policy() - it asserts against
    numbers and key names and reports failures, not a warning list."""
    for key, hits in sorted(stats["formula_hits"].items()):
        warn(
            f"40:99 formula held as a string rather than a registered formula kind plus "
            f"parameters: '{key}' ({len(hits)} occurrence(s)) e.g. {', '.join(hits[:2])}"
        )


# --------------------------------------------------------------------------
# A10/A11 - localization
# --------------------------------------------------------------------------


def check_localization(stats: dict) -> list[tuple]:
    rows: list[tuple] = []
    if not LOCALIZATION.is_file():
        fail(
            f"{rel(LOCALIZATION)} does not exist; every name_key/summary_key is therefore "
            f"unresolvable and missing release strings are build errors (40:216)"
        )
        rows.append(("en.json present", "yes", "no", "FAIL"))
        return rows

    text = LOCALIZATION.read_text(encoding="utf-8")
    try:
        pairs_seen: list[str] = []

        def capture(pairs):
            pairs_seen.extend(k for k, _ in pairs)
            return _no_duplicate_keys(pairs)

        catalog = json.loads(text, object_pairs_hook=capture)
    except (json.JSONDecodeError, ValueError) as exc:
        fail(f"PARSE ERROR {rel(LOCALIZATION)}: {exc}")
        rows.append(("en.json parses", "yes", "no", "FAIL"))
        return rows
    rows.append(("en.json parses (no duplicate keys)", "yes", "yes", "ok"))

    if not isinstance(catalog, dict):
        fail(f"{rel(LOCALIZATION)}: top-level value is {type(catalog).__name__}, expected an object")
        return rows

    keys = list(catalog.keys())
    nested = [k for k, v in catalog.items() if not isinstance(v, str)]
    rows.append(("en.json is flat", 0, f"{len(nested)} non-string value(s)", "ok" if not nested else "FAIL"))
    if nested:
        fail(
            f"{rel(LOCALIZATION)}: {len(nested)} key(s) hold non-string values; the catalog is "
            f"flat key -> string (40:211): {nested[:10]}"
        )

    unsorted_at = [
        (keys[i], keys[i + 1]) for i in range(len(keys) - 1) if keys[i] > keys[i + 1]
    ]
    rows.append(
        ("en.json lexically sorted", 0, f"{len(unsorted_at)} inversion(s)", "ok" if not unsorted_at else "FAIL")
    )
    if unsorted_at:
        fail(
            f"{rel(LOCALIZATION)}: not lexically sorted, {len(unsorted_at)} inversion(s), "
            f"first at {unsorted_at[0][0]!r} before {unsorted_at[0][1]!r} (40:28)"
        )

    present = set(keys)
    envelope_missing = sorted(stats["envelope_key_refs"] - present)
    other_missing = sorted((stats["key_refs"] - stats["envelope_key_refs"]) - present)
    orphans = sorted(present - stats["key_refs"])

    rows.append(
        (
            "name_key/summary_key resolve",
            len(stats["envelope_key_refs"]),
            f"{len(stats['envelope_key_refs']) - len(envelope_missing)} resolved",
            "ok" if not envelope_missing else "FAIL",
        )
    )
    if envelope_missing:
        fail(
            f"{len(envelope_missing)} name_key/summary_key value(s) have no string in "
            f"{rel(LOCALIZATION)} (40:216): {envelope_missing[:15]}"
        )

    rows.append(
        (
            "other *_key references resolve",
            len(stats["key_refs"] - stats["envelope_key_refs"]),
            f"{len(other_missing)} unresolved",
            "ok" if not other_missing else "FAIL",
        )
    )
    if other_missing:
        fail(
            f"{len(other_missing)} other localization key reference(s) have no string in "
            f"{rel(LOCALIZATION)} (40:216): {other_missing[:15]}"
        )

    rows.append(
        ("no orphaned strings", 0, f"{len(orphans)} orphan(s)", "ok" if not orphans else "FAIL")
    )
    if orphans:
        fail(
            f"{len(orphans)} key(s) in {rel(LOCALIZATION)} are referenced by no definition "
            f"(40:212 keys are tied to content IDs and UI roles): {orphans[:15]}"
        )

    rows.append(("en.json total strings", "", len(keys), "ok"))
    return rows


# --------------------------------------------------------------------------
# A12 - per-directory counts
# --------------------------------------------------------------------------


def check_counts(docs: dict[Path, object]) -> list[tuple]:
    rows = []
    for spec in EXPECTATIONS:
        directory = spec["dir"]
        base = CONTENT / directory
        present = files_in(directory, docs)
        if not base.is_dir():
            fail(f"missing catalog directory content/{directory}/ ({spec['source']})")
            rows.append((directory, spec["label"], spec["items"], "DIR MISSING", "FAIL"))
            continue
        items = [p for p, d in present.items() if is_item(d, spec["selector"])]
        others = [p for p in present if p not in items]
        status = "ok"
        if spec["items"] is not None and len(items) != spec["items"]:
            status = "FAIL"
            fail(
                f"content/{directory}/: expected {spec['items']} entries, found {len(items)} "
                f"({spec['label']}; source {spec['source']})"
            )
        if spec["aggregates"] is not None and len(others) != spec["aggregates"]:
            status = "FAIL"
            fail(
                f"content/{directory}/: expected {spec['aggregates']} aggregate file(s), "
                f"found {len(others)}: {[rel(p) for p in sorted(others)]} "
                f"(source {spec['source']})"
            )
        rows.append(
            (
                directory,
                spec["label"],
                spec["items"],
                f"{len(items)} entries + {len(others)} aggregate(s)",
                status,
            )
        )
    return rows


# --------------------------------------------------------------------------
# A13 - aggregate row probes
# --------------------------------------------------------------------------


def probe_array_at_path(present: dict[Path, object], pattern: str) -> tuple[int, list[str]]:
    rx = re.compile(pattern)
    total = 0
    found: list[str] = []
    for path, doc in sorted(present.items()):
        for jpath, _, value in walk(doc):
            if isinstance(value, list) and rx.search(jpath):
                total += len(value)
                found.append(f"{rel(path)}{jpath[1:]} ({len(value)})")
    return total, found


def check_probes(docs: dict[Path, object]) -> list[tuple]:
    rows = []
    for spec in PROBES:
        present = files_in(spec["dir"], docs)
        if spec["kind"] == "files_matching":
            rx = re.compile(spec["pattern"])
            matched = [p for p in sorted(present) if rx.search(p.stem)]
            actual, found = len(matched), [rel(p) for p in matched]
        else:
            actual, found = probe_array_at_path(present, spec["pattern"])
        status = "ok" if actual == spec["expected"] else "FAIL"
        if status == "FAIL":
            fail(
                f"content/{spec['dir']}/: expected {spec['expected']} {spec['label']}, "
                f"found {actual} (source {spec['source']}; matched {found or 'nothing'})"
            )
        rows.append((spec["dir"], spec["label"], spec["expected"], actual, status))
    return rows


# --------------------------------------------------------------------------
# A14 - doc-stated totals recomputed from the JSON
# --------------------------------------------------------------------------


def numbers_under(obj, key_pattern: str) -> list[float]:
    rx = re.compile(key_pattern)
    out: list[float] = []
    for _, key, value in walk(obj):
        if key and rx.search(key) and not isinstance(value, bool) and isinstance(value, (int, float)):
            out.append(value)
    return out


def check_totals(docs: dict[Path, object]) -> list[tuple]:
    rows = []

    rank_prices: list[float] = []
    stated_totals: list[float] = []
    for path, doc in sorted(files_in("powerups", docs).items()):
        if not isinstance(doc, dict) or not isinstance(doc.get("id"), str):
            continue
        for _, key, value in walk(doc):
            if key and re.match(r"^ranks?$", key) and isinstance(value, list):
                for entry in value:
                    if isinstance(entry, dict):
                        rank_prices.extend(numbers_under(entry, r"^price(?:_|$)"))
        stated_totals.extend(
            value
            for key, value in doc.items()
            if re.match(r"^total_cost(?:_|$)", key)
            and not isinstance(value, bool)
            and isinstance(value, (int, float))
        )

    rank_sum = int(sum(rank_prices))
    stated_sum = int(sum(stated_totals))
    rows.append(
        (
            f"PowerUp rank prices ({len(rank_prices)} rank rows)",
            POWERUP_TOTAL_HYPER_GOLD,
            rank_sum,
            "ok" if rank_sum == POWERUP_TOTAL_HYPER_GOLD else "FAIL",
        )
    )
    if rank_sum != POWERUP_TOTAL_HYPER_GOLD:
        fail(
            f"PowerUp rank prices sum to {rank_sum} Hyper Gold across {len(rank_prices)} rank "
            f"rows, expected {POWERUP_TOTAL_HYPER_GOLD} (docs/62-permanent-powerup-catalog.md:35)"
        )
    rows.append(
        (
            f"PowerUp stated per-entry totals ({len(stated_totals)} entries)",
            POWERUP_TOTAL_HYPER_GOLD,
            stated_sum,
            "ok" if stated_sum == POWERUP_TOTAL_HYPER_GOLD else "FAIL",
        )
    )
    if stated_sum != POWERUP_TOTAL_HYPER_GOLD:
        fail(
            f"PowerUp per-entry total costs sum to {stated_sum} Hyper Gold, expected "
            f"{POWERUP_TOTAL_HYPER_GOLD} (docs/62-permanent-powerup-catalog.md:35)"
        )

    unlock_costs: list[float] = []
    for _, doc in sorted(files_in("unlocks", docs).items()):
        if not isinstance(doc, dict) or not isinstance(doc.get("id"), str):
            continue
        if not re.match(r"^UNL-\d{2}$", doc["id"]):
            continue
        found = [
            value
            for key, value in doc.items()
            if re.match(r"^(?:unlock_)?cost(?:_|$)", key)
            and not isinstance(value, bool)
            and isinstance(value, (int, float))
        ]
        if len(found) == 1:
            unlock_costs.append(found[0])
        elif not found:
            fail(f"unlock {doc['id']}: no numeric top-level cost field found")
        else:
            fail(f"unlock {doc['id']}: ambiguous top-level cost fields {found}")
    unlock_sum = int(sum(unlock_costs))
    rows.append(
        (
            f"Option unlock costs ({len(unlock_costs)} unlocks)",
            UNLOCK_TOTAL_HYPER_GOLD,
            unlock_sum,
            "ok" if unlock_sum == UNLOCK_TOTAL_HYPER_GOLD else "FAIL",
        )
    )
    if unlock_sum != UNLOCK_TOTAL_HYPER_GOLD:
        fail(
            f"option unlock costs sum to {unlock_sum} Hyper Gold across {len(unlock_costs)} "
            f"unlocks, expected {UNLOCK_TOTAL_HYPER_GOLD} "
            f"(docs/63-permanent-option-unlock-catalog.md:48)"
        )
    return rows


# --------------------------------------------------------------------------
# A18 - derived-vs-authored regression guard
#
# Only the one known transcription bug is special-cased. 12 s is the DERIVED
# time for three Sentry Pods to exist at a 6 s cadence with the first pod
# immediate; it is not an authored value, and docs/71:83 authors only "One pod
# every 6 s". A generic derived-value detector is not possible without schemas.
# --------------------------------------------------------------------------

# A20 - the compiler-derived footprint values, which no definition may carry.
# Both are derived under docs/technical/40-content-data-and-validation.md:114
# ("Validation derives world speeds/footprints and compares them with the
# survivability report"), so storing either puts a second writer on a
# compiler-owned value - exactly the 0.004 M disagreement that started this.
#
# The two rules have DIFFERENT scopes, because enemies and bosses author
# different halves of their footprint.
#
# Enemies author body_scale_multiplier, and both of these are products of it:
#   contact diameter  = body_scale_multiplier x 0.80 M   (docs/72:86)
#   centre distance   = contact diameter / 2 + 0.50 M    (docs/72:86)
#
# BOSS DIAMETERS ARE AUTHORED, so the diameter rule must NOT cover
# content/bosses/. The boss roster at docs/31:121-128 has no body-scale column
# at all - unlike the ordinary roster overview at docs/31:37-48, which is where
# the ten enemy scales come from - and the scales the four boss diameters would
# imply (1.875, 2.5, 2.0, 2.375) appear nowhere in docs/. docs/72:105 states the
# four diameters flat: Riftjaw 1.50M, Brood Titan 2.00M, Prism Crown 1.60M,
# Skybreaker Apex 1.90M. There is nothing to derive them from, so they are the
# authored quantity, exactly as body_scale_multiplier is for an enemy.
#
# THE CENTRE DISTANCE IS DERIVED FOR BOSSES TOO, so that rule covers
# content/bosses/ as well as content/enemies/. docs/72:86 gives one derivation
# for both: contact begins when the enemy circle and the mech's 0.50M-radius
# collision circle overlap. It reproduces exactly for all four bosses -
# 1.50/2+0.50=1.25, 2.00/2+0.50=1.50, 1.60/2+0.50=1.30, 1.90/2+0.50=1.45 - each
# matching the value content/bosses/ used to store. The 0.50 M term is the
# PLAYER's collision radius, so storing the sum in a boss or enemy catalog
# hardcodes a player-baseline constant into it: change the mech's collision
# radius and those files are silently wrong, with no validator to notice.
#
# THE CENTRE DISTANCE IS DERIVED IN content/maps/ TOO. The health pack authors
# pickup_radius_m = 0.25 M and docs/72:185 gives the sum as a consequence of it:
# "The pack has a 0.25M pickup radius. With the standard mech circle, collection
# occurs when centers come within 0.75M." 0.25 + 0.50 = 0.75, and the 0.50 M is
# again the PLAYER's collision radius - a third writer for one constant, after
# the ten enemies and the four bosses.
#
# reference_diameter_m is allowlisted and must stay: 0.80 M is the Ripper's
# authored rank-zero contact diameter (docs/72:86), the shared reference the
# scale multiplies. It is an authored constant, not a per-enemy derived value.
DERIVED_FOOTPRINT_FIELD_ALLOWED = frozenset({"reference_diameter_m"})
DERIVED_FOOTPRINT_RULES = (
    (
        "collision/contact diameter",
        ("enemies",),
        re.compile(r"(?i)(?:diameter|radius)"),
        "body_scale_multiplier x 0.80 M",
    ),
    (
        "centre distance that begins contact",
        ("enemies", "bosses", "maps"),
        re.compile(r"(?i)cent(?:er|re)_distance|distance_that_begins_contact"),
        "the object's radius + the player's 0.50 M collision radius",
    ),
)

SENTRY_POD_WEAPON_ID = "W-BE"
SENTRY_POD_DEPLOYMENT_SECONDS = 6.0  # docs/71-initial-weapon-numeric-catalog.md:83
DERIVED_DEPLOYMENT_SECONDS = 12  # derived from 6 s x (3 pods - 1), never authored
DEPLOYMENT_KEY = re.compile(r"(?i)deploy|ramp")
DEPLOYMENT_INTERVAL_KEY = re.compile(r"(?i)deploy.*(?:interval|cadence|seconds|period)")


def check_file_inventory() -> list[tuple]:
    """A21 - the total *.json inventory under content/."""
    json_files = sorted(CONTENT.rglob("*.json"))
    other_files = sorted(p for p in CONTENT.rglob("*") if p.is_file() and p.suffix != ".json")
    actual = len(json_files)
    rows = [
        (
            "*.json files under content/",
            EXPECTED_CONTENT_JSON_FILES,
            actual,
            "ok" if actual == EXPECTED_CONTENT_JSON_FILES else "FAIL",
        ),
        (
            "non-JSON files (documentation, not content)",
            "",
            f"{len(other_files)}: {', '.join(rel(p) for p in other_files) or 'none'}",
            "ok",
        ),
    ]
    if actual != EXPECTED_CONTENT_JSON_FILES:
        fail(
            f"content/ holds {actual} *.json file(s), expected "
            f"{EXPECTED_CONTENT_JSON_FILES}. Either a definition was added or removed without "
            f"updating EXPECTED_CONTENT_JSON_FILES and the matching A12 row, or a file is in a "
            f"directory no A12 row covers."
        )
    return rows


def check_derived_footprint_fields(docs: dict[Path, object]) -> list[tuple]:
    """A20 - no definition may carry a compiler-derived footprint value.

    The diameter rule is enemies-only, because boss diameters are authored; the
    centre-distance rule covers enemies and bosses, because it is derived for
    both. See DERIVED_FOOTPRINT_RULES for the citations.
    """
    rows = []
    for label, directories, rx, derivation in DERIVED_FOOTPRINT_RULES:
        hits: list[str] = []
        for directory in directories:
            for path, doc in sorted(files_in(directory, docs).items()):
                for jpath, key, value in walk(doc):
                    if not key or key in DERIVED_FOOTPRINT_FIELD_ALLOWED:
                        continue
                    if rx.search(key):
                        hits.append(f"{rel(path)}{jpath[1:]} = {value!r}")
        scope = " + ".join(f"content/{d}/" for d in directories)
        rows.append(
            (
                f"no {label} field in {scope}",
                0,
                len(hits),
                "ok" if not hits else "FAIL",
            )
        )
        if hits:
            fail(
                f"{len(hits)} field(s) under {scope} hold a {label}, which the compiler derives "
                f"as {derivation} (40:114, 40:100): {hits[:10]}"
            )
    return rows


def check_derived_values(docs: dict[Path, object]) -> list[tuple]:
    rows = []
    banned_hits: list[str] = []
    for path, doc in sorted(files_in("weapons", docs).items()):
        for jpath, key, value in walk(doc):
            if (
                key
                and DEPLOYMENT_KEY.search(key)
                and not isinstance(value, bool)
                and isinstance(value, (int, float))
                and value == DERIVED_DEPLOYMENT_SECONDS
            ):
                banned_hits.append(f"{rel(path)}{jpath[1:]} = {value}")
    rows.append(
        (
            "no authored 12 s deployment/ramp value in content/weapons/",
            0,
            len(banned_hits),
            "ok" if not banned_hits else "FAIL",
        )
    )
    if banned_hits:
        fail(
            f"{len(banned_hits)} deployment/ramp field(s) in content/weapons/ hold "
            f"{DERIVED_DEPLOYMENT_SECONDS}, which is DERIVED from the {SENTRY_POD_DEPLOYMENT_SECONDS} s "
            f"cadence, not authored (docs/71-initial-weapon-numeric-catalog.md:83, 40:100): "
            f"{banned_hits}"
        )

    intervals: list[tuple[str, object]] = []
    for path, doc in sorted(files_in("weapons", docs).items()):
        if not isinstance(doc, dict) or doc.get("id") != SENTRY_POD_WEAPON_ID:
            continue
        for jpath, key, value in walk(doc):
            if (
                key
                and DEPLOYMENT_INTERVAL_KEY.search(key)
                and not isinstance(value, bool)
                and isinstance(value, (int, float))
            ):
                intervals.append((f"{rel(path)}{jpath[1:]}", value))
    if not intervals:
        warn(
            f"{SENTRY_POD_WEAPON_ID}: no numeric deployment-interval property found, so the "
            f"{SENTRY_POD_DEPLOYMENT_SECONDS} s cadence at "
            f"docs/71-initial-weapon-numeric-catalog.md:83 could not be checked "
            f"(field names are unvalidated until content/schemas/ exists)"
        )
        rows.append(("W-BE deployment interval == 6.0 s", SENTRY_POD_DEPLOYMENT_SECONDS, "no field found", "WARN"))
        return rows
    wrong = [f"{p} = {v}" for p, v in intervals if float(v) != SENTRY_POD_DEPLOYMENT_SECONDS]
    rows.append(
        (
            "W-BE deployment interval == 6.0 s",
            SENTRY_POD_DEPLOYMENT_SECONDS,
            ", ".join(f"{v}" for _, v in intervals),
            "ok" if not wrong else "FAIL",
        )
    )
    if wrong:
        fail(
            f"{SENTRY_POD_WEAPON_ID} Sentry Pod deployment interval must be "
            f"{SENTRY_POD_DEPLOYMENT_SECONDS} s (docs/71-initial-weapon-numeric-catalog.md:83): "
            f"{wrong}"
        )
    return rows


# --------------------------------------------------------------------------
# A15 - referential integrity (reference key names discovered from the data)
# --------------------------------------------------------------------------


def ids_in(directory: str, docs: dict[Path, object], pattern: str) -> set[str]:
    rx = re.compile(pattern)
    out: set[str] = set()
    for _, doc in files_in(directory, docs).items():
        if isinstance(doc, dict):
            value = doc.get("id")
            if isinstance(value, str) and rx.match(value):
                out.add(value)
    return out


def check_references(docs: dict[Path, object]) -> list[tuple]:
    rows = []
    weapon_ids = ids_in("weapons", docs, r"^W-[A-F]{2}$")
    enemy_ids = ids_in("enemies", docs, r"^EN-\d{2}$")

    # branches -> weapons: any *weapon_id property, key name discovered
    dangling: list[str] = []
    refs = 0
    keys_used: set[str] = set()
    for path, doc in sorted(files_in("branches", docs).items()):
        for _, key, value in walk(doc):
            if key and re.search(r"(?:^|_)weapon_id$", key) and isinstance(value, str):
                keys_used.add(key)
                refs += 1
                if value not in weapon_ids:
                    dangling.append(f"{rel(path)}.{key} -> {value}")
    rows.append(
        (
            f"branches {sorted(keys_used) or '(no key found)'} -> content/weapons/",
            refs,
            len(dangling),
            "ok" if refs and not dangling else "FAIL" if dangling else "NO REFS",
        )
    )
    if dangling:
        fail(f"{len(dangling)} branch weapon reference(s) do not resolve: {dangling[:10]}")
    if not refs:
        fail("no branch -> weapon reference property found in content/branches/ (40:199)")

    # encounters -> enemies: structured *enemy_id(s) properties plus any EN-nn token
    dangling = []
    refs = 0
    keys_used = set()
    seen: set[str] = set()
    for path, doc in sorted(files_in("encounters", docs).items()):
        for _, key, value in walk(doc):
            tokens: list[str] = []
            if key and re.search(r"(?:^|_)enemy_ids?$", key):
                keys_used.add(key)
                if isinstance(value, str):
                    tokens = [value]
                elif isinstance(value, list):
                    tokens = [v for v in value if isinstance(v, str)]
            elif isinstance(value, str):
                tokens = re.findall(r"\bEN-\d{2}\b", value)
            for token in tokens:
                refs += 1
                if token not in enemy_ids and token not in seen:
                    seen.add(token)
                    dangling.append(f"{rel(path)} -> {token}")
    rows.append(
        (
            f"encounters {sorted(keys_used) or '(no key found)'} -> content/enemies/",
            refs,
            len(dangling),
            "ok" if refs and not dangling else "FAIL" if dangling else "NO REFS",
        )
    )
    if dangling:
        fail(f"encounter schedule references enemy IDs with no enemy file: {dangling[:15]}")
    if not refs:
        fail("no enemy reference found in content/encounters/ (40:199)")

    # mechs -> signature weapon
    dangling = []
    refs = 0
    keys_used = set()
    for path, doc in sorted(files_in("mechs", docs).items()):
        for _, key, value in walk(doc):
            if key and "signature" in key and "weapon" in key and isinstance(value, str):
                keys_used.add(key)
                refs += 1
                if value not in weapon_ids:
                    dangling.append(f"{rel(path)}.{key} -> {value}")
    rows.append(
        (
            f"mechs {sorted(keys_used) or '(no key found)'} -> content/weapons/",
            refs,
            len(dangling),
            "ok" if refs and not dangling else "FAIL" if dangling else "NO REFS",
        )
    )
    if dangling:
        fail(f"{len(dangling)} mech signature-weapon reference(s) do not resolve: {dangling}")
    if not refs:
        fail("no mech signature-weapon reference property found in content/mechs/ (40:199)")
    return rows


# --------------------------------------------------------------------------
# A16 - the numeric percentage-point policy (40:95). Three rules, all decidable
# from key names and numbers, so all three are failures. See the constants above
# for what this replaced and why.
# --------------------------------------------------------------------------


def check_percentage_point_policy(docs: dict[Path, object]) -> list[tuple]:
    """A16 - percentage points are numbers, are not normalized factors, and the
    compiler's normalized factor is never authored beside them."""
    no_number: list[str] = []
    factor_valued: list[str] = []
    hybrid_names: list[str] = []
    twins: list[str] = []
    checked = 0

    def check_twins(where: str, obj: dict) -> None:
        """Rule 3 - percentage points and a same-stem normalized factor in one object."""
        for sibling in obj:
            if not PERCENT_TOKEN_KEY.search(sibling):
                continue
            stem = re.sub(r"(?i)_?percent(?:age)?(?:_points?)?$", "", sibling)
            if not stem:
                continue
            for other in obj:
                if other != sibling and NORMALIZED_FACTOR_TOKEN.search(other) and stem in other:
                    twins.append(f"{where}: {sibling!r} + {other!r}")

    for path, doc in sorted(docs.items()):
        name = rel(path)
        # The document's own top level is an object too, and walk() never yields it.
        if isinstance(doc, dict):
            check_twins(name, doc)
        for jpath, key, value, ancestors in walk_with_ancestry(doc):
            if key is None:
                continue

            says_percent = bool(PERCENT_TOKEN_KEY.search(key))

            # Rule 3 - the compiler owns the normalized factor (40:95, 40:100).
            if says_percent and NORMALIZED_FACTOR_TOKEN.search(key):
                hybrid_names.append(f"{name}{jpath[1:]}")
            if isinstance(value, dict):
                check_twins(f"{name}{jpath[1:]}", value)

            if not says_percent:
                # Rule 2 reaches container leaves through the ancestry.
                if (
                    key in PERCENT_CONTAINER_KEY
                    and not isinstance(value, bool)
                    and isinstance(value, (int, float))
                    and any(PERCENT_TOKEN_KEY.search(a) for a in ancestors)
                ):
                    checked += 1
                    if value != 0 and abs(value) < 1:
                        factor_valued.append(f"{name}{jpath[1:]} = {value}")
                continue

            # Rule 1 - a percent-named property must resolve to a number.
            leaves = list(numeric_leaves(value))
            if not leaves:
                no_number.append(f"{name}{jpath[1:]} = {value!r}")
                continue
            checked += len(leaves)
            # Rule 2 - percentage points, never a normalized factor.
            for leaf in leaves:
                if leaf != 0 and abs(leaf) < 1:
                    factor_valued.append(f"{name}{jpath[1:]} = {leaf}")

    rows = [
        (
            "percent-named properties resolve to a number",
            0,
            f"{len(no_number)} prose-only",
            "ok" if not no_number else "FAIL",
        ),
        (
            f"percentage-point magnitudes, not normalized factors ({checked} numeric leaf/leaves)",
            0,
            f"{len(factor_valued)} with 0 < |v| < 1",
            "ok" if not factor_valued else "FAIL",
        ),
        (
            "compiler's normalized factor not authored",
            0,
            f"{len(hybrid_names)} hybrid name(s), {len(twins)} twin(s)",
            "ok" if not (hybrid_names or twins) else "FAIL",
        ),
    ]
    if no_number:
        fail(
            f"{len(no_number)} property name(s) say percent but hold no numeric value, so the "
            f"percentage exists only as prose (40:95): {no_number[:10]}"
        )
    if factor_valued:
        fail(
            f"{len(factor_valued)} percent-named numeric value(s) satisfy 0 < |v| < 1, which is a "
            f"normalized factor rather than human-readable percentage points (40:95): "
            f"{factor_valued[:10]}"
        )
    if hybrid_names:
        fail(
            f"{len(hybrid_names)} property name(s) combine a percent token with a normalized-factor "
            f"token; the compiler writes the normalized factor as a separate derived field (40:95): "
            f"{hybrid_names[:10]}"
        )
    if twins:
        fail(
            f"{len(twins)} object(s) author both percentage points and a same-stem normalized factor; "
            f"the factor is compiler-derived (40:95, 40:100): {twins[:10]}"
        )
    return rows


# --------------------------------------------------------------------------
# A24 - no repo path with a line number may hide in a domain value.
#
# source_refs was cleaned in an earlier pass, but the citations had moved next
# door: eleven effect.stacking_classification strings carried a parenthetical
# "(docs/68-utility-catalog.md:253)", a weapons note carried one, and
# hyper-gold-sites.json held a repo path in a field named beacon_response_source.
# A line number is unstable wherever it hides, and the doc_id#anchor form
# (40:87) is the only citation form the envelope names, so the rule is scoped to
# the value, not to one field.
# --------------------------------------------------------------------------

DOC_PATH_IN_VALUE = re.compile(r"docs/.*\.md")


def check_no_doc_paths_in_values(docs: dict[Path, object]) -> list[tuple]:
    """A24 - no string value anywhere under content/ matches docs/.*\\.md."""
    hits: list[str] = []
    for path, doc in sorted(docs.items()):
        for jpath, _, value in walk(doc):
            if isinstance(value, str) and DOC_PATH_IN_VALUE.search(value):
                hits.append(f"{rel(path)}{jpath[1:]} = {value!r}")
    rows = [
        (
            "no string value matches docs/.*\\.md",
            0,
            len(hits),
            "ok" if not hits else "FAIL",
        )
    ]
    if hits:
        fail(
            f"{len(hits)} string value(s) under content/ embed a docs/ path; citations use the "
            f"doc_id#anchor form in source_refs, and a path with a line number is unstable "
            f"wherever it hides (40:87): {hits[:10]}"
        )
    return rows


# --------------------------------------------------------------------------
# A25 - polarity agreement between a structured direction and its sibling prose.
#
# This automates a check that had to be done by hand. Ruling 22 in
# content/transcription-notes.md verified all six
# resonance_behavior.modifier.direction values against docs/40:104-109 by eye
# after one was reported wrong; nothing in the tree would have caught a seventh.
#
# The vocabulary is a CLOSED set of opposed pairs and +1 means "more of the
# quantity": higher/lower, increase/decrease, more/less, faster/slower,
# longer/shorter, raise/reduce, gain/lose.
#
# It fires on STRICT contradiction only - every polarity word in the prose has
# the opposite sign to the structured value. Prose carrying both signs, as in
# "enemy attack cadence is 20% faster without increasing movement speed", is not
# a contradiction and is not reported; a heuristic that guessed which clause
# governed would produce exactly the kind of confident wrong answer the
# pre-clear audit appendix warns about.
# --------------------------------------------------------------------------

POLARITY_WORDS = {
    "higher": 1, "lower": -1,
    "increase": 1, "decrease": -1,
    "more": 1, "less": -1,
    "faster": 1, "slower": -1,
    "longer": 1, "shorter": -1,
    "raise": 1, "reduce": -1,
    "gain": 1, "lose": -1,
}
# Inflections of the verbs above, so "increases"/"increasing"/"reduced" count.
POLARITY_INFLECTIONS = ("", "s", "d", "es", "ed", "ing")
POLARITY_LOOKUP: dict[str, int] = {}
for _stem, _sign in POLARITY_WORDS.items():
    for _suffix in POLARITY_INFLECTIONS:
        _base = _stem[:-1] if _suffix in ("ing", "ed", "es") and _stem.endswith("e") else _stem
        POLARITY_LOOKUP.setdefault(_base + _suffix, _sign)
        POLARITY_LOOKUP.setdefault(_stem + _suffix, _sign)
WORD = re.compile(r"[A-Za-z]+")


def polarity_of(text: str) -> set[int]:
    """The set of polarity signs the words of `text` carry."""
    return {POLARITY_LOOKUP[w] for w in (m.group(0).lower() for m in WORD.finditer(text))
            if w in POLARITY_LOOKUP}


def prose_siblings(obj: dict, skip_key: str):
    """String values of `obj` that are prose, not another structured token."""
    for key, value in obj.items():
        if key == skip_key or not isinstance(value, str):
            continue
        # A bare vocabulary word is a second structured value, not prose about one.
        if value.strip().lower().rstrip(".") in POLARITY_LOOKUP:
            continue
        yield key, value


def check_polarity_agreement(docs: dict[Path, object]) -> list[tuple]:
    """A25 - a structured polarity value may not contradict its sibling prose."""
    contradictions: list[str] = []
    pairs = 0

    def visit(node, jpath: str, name: str, parent: dict | None, parent_key: str | None) -> None:
        if isinstance(node, list):
            for index, element in enumerate(node):
                visit(element, f"{jpath}[{index}]", name, parent, parent_key)
            return
        if not isinstance(node, dict):
            return
        for field, structured in node.items():
            if not isinstance(structured, str):
                continue
            sign = POLARITY_LOOKUP.get(structured.strip().lower().rstrip("."))
            if sign is None:
                continue
            candidates = [(key, value) for key, value in prose_siblings(node, field)]
            if parent is not None and parent_key is not None:
                # A "direction" commonly sits one level in, inside a structured
                # modifier, while the prose stating the same fact stays outside it.
                candidates += [
                    (f"../{key}", value) for key, value in prose_siblings(parent, parent_key)
                ]
            for prose_key, prose in candidates:
                signs = polarity_of(prose)
                if not signs:
                    continue
                nonlocal pairs
                pairs += 1
                if sign not in signs:
                    contradictions.append(
                        f"{name}{jpath[1:]}.{field} = {structured!r} contradicts "
                        f"{prose_key} = {prose!r}"
                    )
        for key, value in node.items():
            visit(value, f"{jpath}.{key}", name, node, key)

    for path, doc in sorted(docs.items()):
        visit(doc, "$", rel(path), None, None)
    rows = [
        (
            f"structured polarity agrees with sibling prose ({pairs} pair(s) compared)",
            0,
            len(contradictions),
            "ok" if not contradictions else "FAIL",
        )
    ]
    if contradictions:
        fail(
            f"{len(contradictions)} structured polarity value(s) contradict the prose beside them; "
            f"a sign inversion is invisible to every other assertion here: {contradictions[:10]}"
        )
    return rows


# --------------------------------------------------------------------------
# reporting
# --------------------------------------------------------------------------


def table(title: str, headers: tuple, rows: list[tuple]) -> None:
    print(f"\n{title}")
    if not rows:
        print("  (nothing to report)")
        return
    cols = [str(h) for h in headers]
    body = [[("" if c is None else str(c)) for c in row] for row in rows]
    widths = [max(len(cols[i]), *(len(r[i]) for r in body)) for i in range(len(cols))]
    print("  " + "  ".join(c.ljust(widths[i]) for i, c in enumerate(cols)))
    print("  " + "  ".join("-" * widths[i] for i in range(len(cols))))
    for row in body:
        print("  " + "  ".join(row[i].ljust(widths[i]) for i in range(len(cols))))


def main() -> int:
    print("Content verification (envelope, naming, references, totals, localization)")
    print(f"repo:    {REPO_ROOT}")
    print(f"content: {rel(CONTENT)}")

    doc_index = build_doc_index()
    print(f"docs:    {len(doc_index)} doc_id(s) indexed from {rel(DOCS)}")

    docs = load_definitions()
    print(f"parsed:  {len(docs)} definition file(s)")

    stats = check_definitions(docs, doc_index)
    set_rows = check_expected_sets(stats)
    report_reconciliation(stats)
    count_rows = check_counts(docs)
    probe_rows = check_probes(docs)
    world_prop_rows = check_world_prop_values(docs)
    total_rows = check_totals(docs)
    ref_rows = check_references(docs)
    derived_rows = check_derived_values(docs)
    footprint_rows = check_derived_footprint_fields(docs)
    prefix_rows = check_scope_prefixes(docs)
    bound_rows = check_bound_spelling(docs)
    inventory_rows = check_file_inventory()
    percent_rows = check_percentage_point_policy(docs)
    doc_path_rows = check_no_doc_paths_in_values(docs)
    polarity_rows = check_polarity_agreement(docs)
    loc_rows = check_localization(stats)

    table(
        "A12 Per-directory entry counts",
        ("directory", "catalog", "expected", "actual", "status"),
        count_rows,
    )
    table("A13 Aggregate row probes", ("directory", "rows", "expected", "actual", "status"), probe_rows)
    table(
        "A13 World-prop values (asserted individually, each with its own citation)",
        ("value [doc line]", "expected", "actual", "status"),
        world_prop_rows,
    )
    table(
        "A16 Percentage-point policy (numbers and key names, 40:95)",
        ("check", "expected", "actual", "status"),
        percent_rows,
    )
    table("A14 Doc-stated totals (Hyper Gold)", ("total", "expected", "actual", "status"), total_rows)
    table("A15 Referential integrity", ("check", "refs", "dangling", "status"), ref_rows)
    table("A18 Derived-vs-authored guard", ("check", "expected", "actual", "status"), derived_rows)
    table(
        "A20 Footprint fields the compiler owns",
        ("check", "expected", "actual", "status"),
        footprint_rows,
    )
    table(
        "A24 No docs/*.md path in any string value",
        ("check", "expected", "actual", "status"),
        doc_path_rows,
    )
    table(
        "A25 Polarity agreement (structured direction vs sibling prose)",
        ("check", "expected", "actual", "status"),
        polarity_rows,
    )
    table(
        "A22 source_refs scope prefixes",
        ("check", "prefixed refs", "dangling", "status"),
        prefix_rows,
    )
    table(
        "A23 One spelling for a bound (maximum/minimum, spelled out)",
        ("check", "expected", "actual", "status"),
        bound_rows,
    )
    table("A10/A11 Localization", ("check", "expected", "actual", "status"), loc_rows)
    table("A19 Expected exception sets", ("set", "expected", "actual", "status"), set_rows)
    table("A21 File inventory", ("check", "expected", "actual", "status"), inventory_rows)

    print(f"\nA2-A9 envelope/naming: {stats['checked']} definition(s) checked, "
          f"{stats['source_refs']} source_refs resolved against docs/")
    print(f"A5/A6 definitions with no stable id ({len(stats['no_id'])}):")
    for name in sorted(stats["no_id"]):
        kind = "absent" if name in stats["missing_id"] else "null"
        print(f"  - {name} ({kind})")
    print(f"A3 definitions omitting name_key ({len(stats['no_name_key'])}):")
    for name in sorted(stats["no_name_key"]):
        print(f"  - {name}")

    if failures:
        print(f"\nFAILURES ({len(failures)}):")
        for message in failures:
            print(f"  x {message}")

    if warnings:
        print(f"\nWARNINGS ({len(warnings)}):")
        for message in warnings:
            print(f"  ! {message}")

    print(f"\nRESULT: {'FAIL' if failures else 'PASS'} "
          f"({len(failures)} failure(s), {len(warnings)} warning(s))")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
