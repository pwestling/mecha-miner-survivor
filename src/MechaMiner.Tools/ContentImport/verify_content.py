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
      4 beacon responses, 7 formations, 1 map contract file), AND the six
      authored world-prop VALUES folded into the map contract match the
      document: destructible rock Hull 100 (docs/72:194), rock damage
      footprint diameter 0.80 M (:196), health pack repair 25 Hull (:182),
      health pack pickup radius 0.25 M (:185), rock active population cap
      16 and rock initial count 16 (both docs/51:146, the active cap
      corroborated at docs/72:203). Every row cites its own source
      doc:line.
      The two 16-rock rows were ADDED as a coverage gap: both values were
      transcribed and NEITHER was asserted, and they sit between two values
      that were - rock Hull at 72:194 and the footprint at 72:196 bracket
      the population rules in the same section. A value whose neighbours
      are asserted reads as covered. That is a gap that LOOKS FILLED, which
      is a different failure from a gate that cannot fail.
      Negative control, per value, each reverted after: active_maximum
      16 -> 15 FAILs "A13 destructible rock active population cap must be
      16"; initial_count 16 -> 12 FAILs "A13 destructible rock initial
      count must be 16".
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
           same-stem factor sibling;
        4. no NUMBER sits under a relative-magnitude name - one whose
           segments include bonus/bonuses, penalty/penalties, increase,
           increased, decrease, reduction, boost, malus, discount,
           surcharge or uplift - when that name says neither percent nor
           any unit-or-kind token. Such a number is either percentage
           points or a multiplicative scale and the name does not say
           which; 40:95 permits percentage points only under a name that
           says percent, and 40:94 requires an ambiguous numeric name to
           carry a unit suffix.
      A name "says _percent" wherever the token appears, not only at the
      end - 40:95 constrains what the name says and 40:96's terminal-unit
      rule is about unit suffixes, so the 52 mid-name spellings such as
      percent_of_mech_base_speed are correct and are not flagged. Rule 4
      excludes a unit-or-kind token wherever it appears too, for the
      mirror-image reason: the tree's
      single_target_ceiling_multiplier_at_full_bonus has `multiplier` as
      its head noun and `bonus` as a mid-name qualifier, and Ruling 14
      makes `_multiplier` the unit declaration for a multiplicative scale.
      WHAT RULE 4 EXISTS FOR, and what it was verified to do. Rules 1-3 all
      begin by asking whether the name says percent, so in the previous
      revision every A16 rule was gated behind `if not says_percent: ...
      continue` and a bare number under a non-percent name was never
      examined. That revision's docstring nevertheless advertised the
      rewrite as fixing exactly that case; it did not.
      Rule 4 flags nothing in this tree as authored - it is a regression
      guard, not a cleanup - so the claim above rests on its negative
      control, not on a count. Two injections on content/enemies/EN-01.json
      were each run and reverted individually:
        sneaky_bonus: 25  -> FAIL, "1 numeric value(s) sit under a
          relative-magnitude name ... ['content/enemies/EN-01.json.
          sneaky_bonus = 25']", RESULT: FAIL (1 failure)
        damage_bonus: 150 -> FAIL, the same message reporting
          ['content/enemies/EN-01.json.damage_bonus = 150'],
          RESULT: FAIL (1 failure)
      Nothing beyond those two forms is claimed here.
      A16 REPLACED a prose scan that matched a literal "%" glyph in string
      values: it left a numeric 25 under a non-percent name unchecked while
      emitting 21 warnings about English sentences. None of the four rules
      needs content/schemas/, so all four are failures.              FAILURE

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
      files, so a file in a directory no A12 row covers is still caught,
      AND the non-JSON files under content/ are exactly the three named in
      EXPECTED_CONTENT_NON_JSON (README.md, quote-verification-audit.md,
      transcription-notes.md).
      The non-JSON row used to PRINT the file list next to a blank
      expectation and a hardcoded "ok" - it could not fail, so a stray file
      under content/ was reported and tolerated in the same breath. It now
      asserts the exact list.
      Negative control: an empty content/probe.txt -> FAIL, "content/ holds
      non-JSON files [... 'content/probe.txt' ...], expected exactly
      [...]".                                                         FAILURE

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

  A24 Two rules over every string value under content/, each matching the
      thing that is actually wrong rather than one spelling of a path:
        a. no path-like token carries a `:<digits>` line number, in ANY
           spelling - either slash separator, any case, extension optional
           when a separator is present, and no `docs/` prefix required;
        b. no repository path (docs, src, content, tools, assets followed by
           a separator) appears at all, line number or not, because 40:87
           names doc_id#anchor as the citation form and a path is not one.
      A bare `#anchor` is OUT OF SCOPE by design: it is half of the
      sanctioned citation form, A9 already resolves anchors against real
      heading slugs, and it carries neither a path nor a line number.
      REPLACED `docs/.*\.md`, which pinned three incidental spellings and
      let six forms through. Each of seven forms was tested individually
      against the new rules; the six defective ones are caught and the
      anchor-only form is confirmed not matching.
      The narrowness was not hypothetical: the new rules found TWO real
      defects in this tree that the old pattern could not see - a
      `content/transcription-notes.md` path in an encounter-schedule
      reconstruction_basis, and a bare extensionless `docs/68` in a UTL-A1
      statement. Both are the class Ruling 25 removed 13 of, and both were
      rewritten in this pass.
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

  A26 No `null` appears anywhere under content/, at any depth, in any file -
      including content/localization/en.json, which the definition loader
      skips. THERE IS NO EXCEPTION SET, and that is deliberate: an
      exception set is a place for a null to hide. A null in a source
      definition is never legal, because 40:90 materializes an explicit
      default for every absent optional field ("Optional fields have
      explicit defaults materialized into the canonical bundle so runtime
      never guesses") - so an absent field gets its default and a
      present-and-null field asks runtime to guess. Absence is spelled by
      omitting the key.
      275 nulls across 101 of 138 definition files were disposed of in the
      pass that added this: 246 keys omitted, 20 relic rarity/weighting
      fields and 4 boss armor fields REMOVED as fields no schema will
      declare, 3 external_numerics[n].value keys removed as shape defects,
      and 2 nested id keys removed because the objects holding them are not
      independently addressable. The two nested ids were briefly planned as
      declared exceptions; removing the key instead made the assertion
      unconditional.
      Negative control: `"probe_null": null` injected at the top level of
      content/enemies/EN-01.json -> FAIL, "1 null(s) under content/ ...
      ['content/enemies/EN-01.json.probe_null']".
      Mandate: docs/technical/40-content-data-and-validation.md:90  FAILURE

  A27 No sentence-internal abbreviation period appears anywhere under
      docs/**/*.md. This asserts a property of the CORPUS on behalf of a
      MATCHER, and it is the only assertion here that does.
      content/quote-verification-audit.md adopts a quotation rule that
      fires when a stored string begins at a sentence boundary, carries its
      own terminator, and the source sentence continues past it. That rule
      measured 2 hits with ZERO false positives across the audit's whole
      matched set - 1,072 records, which is its 806 decidable matches plus
      the 266 matches below its decidability gate (audit §2, §6) - but
      only because `.`/`!`/`?` is an unambiguous sentence terminator in
      THIS corpus. The moment a design document writes "e.g." the terminator
      stops being unambiguous and the rule starts misfiring on innocent
      quotations. Documenting that as an assumption would be a fail-open
      with a footnote, so it is asserted instead.
      The list is the abbreviations that carry a period INSIDE a sentence
      and are plausible in this project's prose: e.g. i.e. etc. approx.
      cf. vs. viz. resp. no. fig. eq. sec. p. pp. ca. al. esp. incl.
      Chosen on one criterion - the period is not a sentence end - so
      units ("0.80M", "1.5 s") and decimals are NOT in scope: a decimal
      point is not followed by a sentence-initial capital and the audit
      measured zero decimal misfires across all 1,072 records.
      Matched case-insensitively at a word boundary, because unbounded
      substring matching finds "st." inside 93 ordinary words ending a
      sentence ("first.", "specialist.", "cost.", ... 21 forms) and "ver."
      inside 5 ("forever.", "solver.", "hover."). None of those 98 is an
      abbreviation. The bounded form finds zero today.
      THE FAILURE MESSAGE POINTS AT THE MATCHER, NOT AT A QUOTATION. The
      day someone writes "e.g." in a design document, nothing is wrong with
      any content string; what is wrong is that the quotation rule's
      premise no longer holds and the rule needs revisiting.
      Negative control: docs/ must not be modified, so the check runs
      against a scratch tree - a byte copy of docs/ with "e.g." inserted
      into one sentence -> FAIL naming that file and token.
      Mandate: content/quote-verification-audit.md (adopted rule and its
      stated corpus dependency)                                     FAILURE

  A28 No definition carries a compiler-derived value from any of the SIX
      families removed by the derived-value pass. SIX RULES WITH SIX
      DIFFERENT SCOPES, for the same reason A20 is two rules with two
      scopes: some of these patterns flag legitimately AUTHORED fields in
      a directory they do not cover. An absolute metres-per-second value is
      always derived in content/enemies/ and content/bosses/, where a speed
      is authored as a percentage of the mech baseline - and always
      authored in content/weapons/, where projectile_speed_m_per_s is the
      real number. So the world-speed rule covers the first two and not the
      third.
      SIX, not the nine an earlier draft asserted: the damage-pressure
      block (32 values) and the resonant hit counts (5) are the COMPARAND
      40:114 has the compiler compare its derivation against, not derived
      duplicates, and the stat price curve (14) would have moved fourteen
      checkable numbers into an unchecked prose string. All 51 restored.
      See pulled_from_this_pass in the expectation file.
      TWO LAYERS, and neither is a complete guard on its own:
      (1) a NAME layer over pointer SEGMENT NAMES. It catches a rename only
      within its own word class. It does NOT make a rename impossible: a
      value reintroduced under a name the class does not carry passes, and
      that was measured, not assumed - a semantic-neighbour probe defeated
      all nine drafts of these rules before they were widened, and a probe
      chosen against the widened classes would defeat some of them too.
      Segments, not just the leaf key, because some families store the
      number under a generic leaf (`amount`, `minimum`, `maximum`) inside a
      specifically named parent - a leaf-key-only rule would miss
      total_payout_per_map.amount entirely.
      (2) a VALUE layer, which is what a name rule cannot do: for each
      removed value, no non-operand numeric leaf inside its own derivation
      site may carry that value. Exact Fractions, no tolerance. This one
      survives a rename, a relocation within the site, a different unit
      suffix, and scalar -> [scalar]. Its RADIUS is the limit and is stated
      rather than hidden: the derivation site, not the file and not the
      scope, because at file radius this tree has 55 coincidental
      recurrences and at scope radius 400 - magnitude coincidences between
      unrelated quantities, which no honest exception list can absorb. One
      exception is declared, enumerated and justified.
      Two segment names are ALLOWLISTED, in the shape A20 allowlists
      reference_diameter_m: `purchases` (the authored checkpoint index the
      removed cumulative cost derives FROM, which matches only by
      inheriting its parent's name) and total_seam_payout_multiplier (left
      authored; its sibling exposure_per_secured_payout_multiplier has no
      stated derivation at all).
      The rules, scopes and allowlists are READ FROM
      expected_derived_value_removals.json rather than duplicated here, so
      the assertion and the prediction cannot drift apart.
      Mandate: per family, the docs/ line recorded in that file -
      40:114 (world speeds; the survivability report), 40:136 ("Validators
      recompute total catalog costs"), 40:140 ("their totals"), 40:203
      ("Recalculate ... price curves, total costs ... resource totals")
                                                                    FAILURE

  A29 The numeric multiset the tree LOST equals the committed expectation,
      as SET EQUALITY over all 166 elements - each (file, pointer, value)
      present in one side and the other - not as two totals that happen to
      agree. 166 == 166 would also hold if one value were removed by
      mistake and a different one kept by mistake; element-wise equality
      would not.
      Measured, not asserted: the sweep-ref tree is read out of git at the
      SHA the expectation file names, its numeric leaves are enumerated,
      and the worktree's are subtracted. The expectation file was committed
      BEFORE any content/ file changed (see that commit's --stat), so this
      check compares a prediction against a measurement rather than a diff
      against itself.
      It also asserts the ADDED side is empty. A removal pass that quietly
      introduced a number would otherwise pass every other rule here.
      Mandate: docs/technical/40-content-data-and-validation.md:100
      ("Derived values include source operands and calculation version in
      reports"), which is what makes a stored operand-plus-result pair the
      compiler's to emit and not content's to author         FAILURE

Not asserted here: no structural JSON Schema validation happens, because
content/schemas/ (40:36) does not exist yet. Domain field names outside the
envelope are therefore unvalidated and will need one reconciliation pass when
the schemas land. See content/transcription-notes.md.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
import textwrap
from fractions import Fraction
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
# content/ also holds three Markdown files - README.md, transcription-notes.md
# and quote-verification-audit.md - which are documentation, not content. So
# `find content -type f` reports 142 while this count is 139; that difference is
# correct and is not a discrepancy.
#
# The non-JSON files are NAMED rather than counted, and the A21 row asserts the
# exact list. It previously printed the list beside a blank expectation and a
# hardcoded "ok", which reported a stray file under content/ and tolerated it in
# the same breath. Adding a documentation file here is a deliberate act and
# updating this tuple is the record of it.
EXPECTED_CONTENT_JSON_FILES = 139
EXPECTED_CONTENT_NON_JSON = (
    "content/README.md",
    "content/quote-verification-audit.md",
    "content/transcription-notes.md",
)

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
    # ADDED as a coverage gap, not as a new policy. The rock population cap was
    # transcribed and never asserted, and the reason it was missed generalises:
    # A VALUE WHOSE NEIGHBOURS ARE ASSERTED READS AS COVERED. rock Hull (72:194)
    # and the footprint diameter (72:196) are both checked, and they bracket this
    # value in the same document section, which is exactly the situation in which
    # nobody looks. That is a distinct failure shape from a gate that cannot fail
    # - it is a gap that looks filled.
    (
        "destructible rock active population cap",
        re.compile(r"(?i)destructible_rock(?:_rules)?"),
        re.compile(r"(?i)^active_maximum$"),
        16,
        "docs/51-standard-map-generation-contract.md:146",
    ),
    (
        "destructible rock initial count",
        re.compile(r"(?i)destructible_rock(?:_rules)?"),
        re.compile(r"(?i)^initial_count$"),
        16,
        "docs/51-standard-map-generation-contract.md:146",
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
# is gone and the FOUR rules below run on NUMBERS and KEY NAMES instead. None of
# them needs content/schemas/, so all four are failures rather than warnings.
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
# A16 rule 4 - the OTHER half of 40:95, which the previous rewrite claimed to fix
# and did not.
#
# 40:95 reads "Percentages in authoring use human-readable percentage points ONLY
# WHEN the property name says `_percent`". Rules 1-3 all begin by asking whether
# the name says percent, so every one of them is gated behind that question and a
# bare number under a name that does NOT say percent was never examined at all.
# `sneaky_bonus: 25` and `damage_bonus: 150` both passed with zero failures.
#
# What is decidable here. Whether an arbitrary number "is a percentage" is not
# decidable from the number. But a name whose head noun is a RELATIVE magnitude -
# a bonus, a penalty, an increase, a reduction - names a quantity that is
# necessarily proportional to something else, so it is either percentage points or
# a multiplicative scale, and both 40:95 (percentage points say `_percent`) and
# 40:94 (ambiguous numeric names carry a unit suffix) require the name to say
# which. A relative-magnitude name carrying neither is a number whose unit the
# reader cannot recover: 25 could be 25 percentage points or a 25x scale.
#
# A name that already declares its quantity kind ANYWHERE in the name is excluded,
# not just terminally. `single_target_ceiling_multiplier_at_full_bonus` is the
# tree's one such name: its head noun is `multiplier`, the `bonus` token is a
# mid-name qualifier, and Ruling 14 makes `_multiplier` the unit declaration for a
# multiplicative scale. Requiring the token to be terminal would have flagged it.
#
# This rule flags NOTHING in the tree as authored. That is the point: it is a
# regression guard over a CLOSED VOCABULARY of relative-magnitude segments -
# exactly the tokens in RELATIVE_MAGNITUDE_TOKEN below - and its negative control,
# the two injections named above, is what demonstrates it bites.
#
# It does NOT catch "a percentage arriving under a name that hides its unit" in
# general. Whether an arbitrary number is a percentage is not decidable from the
# number, and `sneaky_value: 25` sails past this rule exactly as it sails past
# rules 1-3. Nothing beyond the two enumerated forms - a number under a
# relative-magnitude name carrying neither a percent token nor a unit-or-kind
# token - is claimed here.
RELATIVE_MAGNITUDE_TOKEN = re.compile(
    r"(?i)(?:^|_)(?:bonus|bonuses|penalty|penalties|increase|increased|decrease"
    r"|reduction|boost|malus|discount|surcharge|uplift)(?:$|_)"
)
# Tokens that declare what kind of quantity the number is, so the name is not
# ambiguous and 40:94 is satisfied. Matched as whole underscore-delimited segments
# anywhere in the name.
UNIT_OR_KIND_TOKEN = re.compile(
    r"(?i)(?:^|_)(?:m|m_per_s|meters?|seconds?|milliseconds?|per_second|hull|armor"
    r"|degrees|fraction|count|multiplier|scale|ore|hyper_gold|units?|ranks?|hits?"
    r"|diameters?|tier|weight)(?:$|_)"
)
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
            f"{len(EXPECTED_CONTENT_NON_JSON)}: {', '.join(EXPECTED_CONTENT_NON_JSON)}",
            f"{len(other_files)}: {', '.join(rel(p) for p in other_files) or 'none'}",
            "ok" if [rel(p) for p in other_files] == list(EXPECTED_CONTENT_NON_JSON) else "FAIL",
        ),
    ]
    if [rel(p) for p in other_files] != list(EXPECTED_CONTENT_NON_JSON):
        fail(
            f"content/ holds non-JSON files {[rel(p) for p in other_files]}, expected exactly "
            f"{list(EXPECTED_CONTENT_NON_JSON)}. This row used to LIST the non-JSON files with a "
            f"blank expectation and a hardcoded 'ok', so it could never fail - a stray file under "
            f"content/ was reported and tolerated in the same breath. It is now an expectation: a "
            f"new documentation file is added here deliberately, and anything else is a finding."
        )
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


# --------------------------------------------------------------------------
# A28 / A29 - the nine derived-value families the compiler owns.
#
# The rules live in expected_derived_value_removals.json, which was committed in
# its own commit BEFORE any content/ file changed. Reading them from there rather
# than restating them here is deliberate: a rule restated in two places can drift,
# and the whole point of the prediction-first ordering is that the assertion and
# the prediction are the same artifact.
# --------------------------------------------------------------------------

DERIVED_EXPECTATION = Path(__file__).resolve().parent / "expected_derived_value_removals.json"


def load_derived_expectation() -> dict:
    if not DERIVED_EXPECTATION.exists():
        fail(
            f"A28/A29: {rel(DERIVED_EXPECTATION)} is missing. It is the committed prediction of "
            f"exactly which derived values this tree no longer authors, and both rules read their "
            f"scopes and patterns from it. Regenerate it with "
            f"derive_derived_value_expectations.py."
        )
        return {}
    return json.loads(DERIVED_EXPECTATION.read_text())


def pointer_segments(pointer: str) -> list[str]:
    return [s for s in re.split(r"\.|\[\d+\]", pointer) if s]


def derived_rule_matches(family: dict, pointer: str) -> bool:
    """A28's matcher: a family's rule against a pointer's SEGMENT NAMES.

    The allowlist is consulted on the LEAF segment, which is A20's semantics
    (`if key in DERIVED_FOOTPRINT_FIELD_ALLOWED: continue`) - an allowlisted leaf
    is exempt even when an ANCESTOR name matches, which is the only case that
    arises: `purchases` matches nothing itself, it inherits the match from
    `cumulative_cost_checkpoints` above it.
    """
    segments = pointer_segments(pointer)
    if not segments:
        return False
    allow = family.get("allowlisted_segments") or {}
    if segments[-1] in allow:
        return False
    child_rx = re.compile(family["pointer_segment_rule"])
    parent = family.get("pointer_parent_rule")
    if parent:
        parent_rx = re.compile(parent)
        for index, seg in enumerate(segments):
            if parent_rx.search(seg) and any(child_rx.search(s) for s in segments[index + 1 :]):
                return True
        return False
    return any(child_rx.search(seg) for seg in segments)


def numeric_pointer_leaves(obj, prefix: str = ""):
    """Yield (pointer, value) for every numeric leaf. Bools are not numbers."""
    if isinstance(obj, dict):
        items = obj.items()
    elif isinstance(obj, list):
        items = ((f"[{i}]", v) for i, v in enumerate(obj))
    else:
        return
    for key, value in items:
        child = key if (isinstance(obj, dict) and not prefix) else (
            f"{prefix}.{key}" if isinstance(obj, dict) else f"{prefix}{key}"
        )
        if isinstance(value, bool):
            continue
        if isinstance(value, (int, float)):
            yield child, value
        else:
            yield from numeric_pointer_leaves(value, child)


def check_derived_family_absence(docs: dict[Path, object]) -> list[tuple]:
    """A28 - none of the six removed families may reappear.

    Two layers. This is the NAME layer, over pointer segment names; it catches a
    rename only within its own word class and cannot make one impossible. The
    VALUE layer - no non-operand leaf inside a derivation site may carry the
    derived value - is asserted by the generator against the pinned sweep ref and
    recorded in the expectation file, because it is a property of the removal set
    rather than of the current tree.
    """
    expectation = load_derived_expectation()
    rows = []
    for family in expectation.get("families", []):
        hits: list[str] = []
        for scope in family["scopes"]:
            directory = scope.strip("/").split("/", 1)[1]
            for path, doc in sorted(files_in(directory, docs).items()):
                for pointer, value in numeric_pointer_leaves(doc):
                    if derived_rule_matches(family, pointer):
                        hits.append(f"{rel(path)}.{pointer} = {value!r}")
        rows.append(
            (
                f"no {family['name']} value in {' + '.join(family['scopes'])}",
                0,
                len(hits),
                "ok" if not hits else "FAIL",
            )
        )
        if hits:
            fail(
                f"A28 {len(hits)} field(s) under {' + '.join(family['scopes'])} hold a "
                f"'{family['name']}' value, which the compiler owns per "
                f"{family['doc_assignment'].split(' - ')[0]}. Matched on pointer segment names "
                f"/{family['pointer_segment_rule']}/"
                + (f" under /{family['pointer_parent_rule']}/" if family.get("pointer_parent_rule")
                   else "")
                + f". This is the NAME layer, which catches a rename only within its own word "
                f"class - a value reintroduced under a name the class does not carry passes it, and "
                f"the value layer is what covers that: {hits[:10]}"
            )
    return rows, (
        "NOT CAUGHT by this layer: a derived value reintroduced under a name outside the family's "
        "word class, or in a directory the family does not scope. Probed per family with a "
        "semantic-neighbour name - caught 0 of 6. This layer catches a rename only WITHIN its own "
        "word class. Layer 2 below is the one that does not depend on the name at all.",
    )


def check_derived_family_values(docs: dict[Path, object]) -> list[tuple]:
    """A28's VALUE layer, over the CURRENT tree - the half a name rule cannot do.

    For every value this pass removed, no non-operand numeric leaf inside its own
    derivation site may carry that value. Compared exactly, as Fractions, with no
    tolerance: a stored 32.0 and a stored 32 are the same number and both fail.

    This is what makes the guard indifferent to spelling. A reintroduction
    survives a rename, a relocation inside the site, a different unit suffix and a
    change of arity (32.0 -> [32.0]) without changing the number, and all four are
    caught here while all four defeat the name layer.

    Its RADIUS IS ITS LIMIT AND IS STATED: the derivation site, not the file and
    not the scope. A value relocated OUT of its site still passes. That choice was
    measured rather than assumed - see VALUE_COLLISION_EXCEPTIONS in the generator,
    and the 55/400 coincidence counts at file and scope radius that make a wider
    radius unlandable without a hand-written exception list nobody can audit.
    """
    expectation = load_derived_expectation()
    exceptions = {
        (e["file"], e["derived_pointer"], e["colliding_pointer"])
        for e in expectation.get("value_collision_exceptions", [])
    }
    by_path = {rel(p): d for p, d in docs.items()}
    rows = []
    searched = 0
    records_checked = 0
    for family in expectation.get("families", []):
        hits: list[str] = []
        for record in family["records"]:
            doc = by_path.get(record["file"])
            if doc is None:
                continue
            records_checked += 1
            pointer = record["pointer"]
            site = (pointer[: pointer.rindex("[")] if pointer.endswith("]")
                    else (pointer.rsplit(".", 1)[0] if "." in pointer else ""))
            own = {p.split("::", 1)[1] for p in record.get("operand_pointers", [])
                   if p.split("::", 1)[0] == record["file"]}
            target = Fraction(str(record["value"]))
            for leaf, value in numeric_pointer_leaves(doc):
                if not (site and (leaf == site or leaf.startswith(site + ".")
                                  or leaf.startswith(site + "["))):
                    continue
                searched += 1
                if Fraction(str(value)) != target:
                    continue
                if leaf in own or (record["file"], pointer, leaf) in exceptions:
                    continue
                hits.append(f"{record['file']}.{leaf} = {value!r} (the removed {pointer})")
        rows.append(
            (
                f"no {family['name']} VALUE at its derivation site",
                0,
                len(hits),
                "ok" if not hits else "FAIL",
            )
        )
        if hits:
            fail(
                f"A28 value layer: {len(hits)} numeric leaf/leaves carry a value this pass removed "
                f"as a '{family['name']}', inside that value's own derivation site and not as one "
                f"of its operands. Matched on the NUMBER, so a rename, a relocation within the "
                f"site, a new unit suffix and a scalar-to-list change all fail it. If a hit is a "
                f"genuine coincidence, add it to VALUE_COLLISION_EXCEPTIONS with a reason: "
                f"{sorted(hits)[:10]}"
            )
    # WHAT THIS CHECK SEARCHED AND WHAT IT CANNOT SEE, on the same table a passing
    # run prints. A limitation recorded only in a docstring or a notes file is not
    # disclosed to the person reading a green run.
    return rows, (
        f"RADIUS SEARCHED: the object that held each removed leaf, plus its subtree - "
        f"{searched} numeric leaves across {records_checked} removed values, with "
        f"{len(exceptions)} declared exception(s). Values compare exactly as Fractions; there is "
        f"no tolerance and no rounding.",
        "NOT CAUGHT by this layer: a removed value relocated OUT of its derivation site, elsewhere "
        "in the same file or anywhere else in the scope. Probed per family - caught 0 of 6. Rename, "
        "unit suffix and arity change (32.0 -> [32.0]) are caught 6 of 6. Wider radii were measured "
        "and rejected: file radius has 55 coincidental recurrences on this tree and scope radius "
        "400, which no exception list can justify entry by entry.",
    )


# --------------------------------------------------------------------------
# A30 - the docs CSV mirror and content/ must agree, value by value.
#
# docs/data/contact-damage-pressure.csv and content/ both carry the survivability
# report. Ruling 45 found all overlapping values agreeing and said so; nothing
# KEPT them agreeing. Two unguarded mirrors is the exact shape where a later edit
# to one produces a silent contradiction, so agreement is asserted rather than
# observed.
#
# This does NOT wait on the authority question (which of the two is the accepted
# gameplay table 40:203 compares against). It is worth having either way, and when
# the question lands, the loser becomes derived and this check becomes redundant in
# the good way rather than wrong.
#
# NO TOLERANCE. Values compare exactly as Fractions. Where the CSV states a value
# at lower precision than the derivation produces, the pair must be DECLARED
# below - enumerated with a reason, never absorbed by a threshold - and the content
# value must round to the CSV value at the CSV's own written precision. A new
# inexact pair therefore fails instead of hiding behind the declared one.
# --------------------------------------------------------------------------
PRESSURE_CSV = REPO_ROOT / "docs/data/contact-damage-pressure.csv"

CSV_MIRROR_ROUNDED = {
    ("EN-07", "contact_diameter_m"): (
        "Two docs/ sources disagree and content/ follows the one it cites. docs/31 section "
     "'Ordinary roster overview' states the "
        "Razorling body scale as 0.62x, so the derived diameter is 0.62 x 0.80 = 0.496 M exactly. "
        "docs/72 section 'Collision and Contact Footprints' states its footprint as 0.50 M, and "
        "this CSV mirrors 72. The 0.496 is not a "
        "content defect - it is the exact product of the authored scale - and docs/40 section "
        "'Analytical' fails only on "
        "divergence 'beyond documented rounding'. EN-07 is the ONLY actor whose derived diameter "
        "does not land on the CSV's two decimal places; every other one is exact."
    ),
    ("EN-07", "contact_start_distance_m"): (
        "The same 0.496 propagated: 0.496/2 + 0.50 = 0.748, which docs/72 section 'Collision and "
        "Contact Footprints' states as 0.75. One "
        "divergence, not two."
    ),
}

CSV_MIRROR_EXPECTED_COMPARISONS = 98


def _csv_decimals(text: str) -> int:
    return len(text.split(".", 1)[1]) if "." in text else 0


def check_csv_mirror_agreement(docs: dict[Path, object]) -> list[tuple]:
    """A30 - every value docs/data/contact-damage-pressure.csv shares with content/.

    Seven columns x 14 actors. Four columns compare against an AUTHORED content
    field; three compare against a value the compiler derives from surviving
    operands, which is the comparison 40:114 actually describes ("derives world
    speeds/footprints and compares them with the survivability report").
    """
    if not PRESSURE_CSV.exists():
        fail(
            f"A30 {rel(PRESSURE_CSV)} is missing, so the CSV/content mirror is unmeasured. This "
            f"rule is not allowed to pass by being unable to run."
        )
        return [("pressure CSV readable", "present", "missing", "FAIL")]

    contract = None
    for path, doc in docs.items():
        if path.name == "standard-map-generation-contract.json":
            contract = doc
    if not isinstance(contract, dict) or "reference_mech_speed_m_per_s" not in contract:
        fail("A30 could not read reference_mech_speed_m_per_s, an operand of the speed column.")
        return [("mech base speed readable", "present", "missing", "FAIL")]
    base_speed = Fraction(str(contract["reference_mech_speed_m_per_s"]))
    ref_diameter = Fraction("0.80")   # docs/72 "Collision and Contact Footprints"
    player_radius = Fraction("0.50")  # docs/72 "Collision and Contact Footprints"

    by_id = {}
    for path, doc in docs.items():
        if isinstance(doc, dict) and isinstance(doc.get("id"), str):
            if path.parent.name in ("enemies", "bosses"):
                by_id[doc["id"]] = doc

    import csv as _csv

    compared = 0
    exact_hits = 0
    declared_used: set = set()
    mismatches: list[str] = []
    missing_actors: list[str] = []

    with PRESSURE_CSV.open() as handle:
        for row in _csv.DictReader(handle):
            actor = row["actor_id"]
            doc = by_id.get(actor)
            if doc is None:
                missing_actors.append(actor)
                continue
            footprint = doc.get("contact_footprint") or {}
            if "contact_and_weapon_hurt_diameter_m" in footprint:
                diameter = Fraction(str(footprint["contact_and_weapon_hurt_diameter_m"]))
                diameter_basis = "authored contact_and_weapon_hurt_diameter_m"
            else:
                diameter = Fraction(str(doc["body_scale_multiplier"])) * ref_diameter
                diameter_basis = f"body_scale_multiplier x {ref_diameter}"
            percent = Fraction(
                str(doc["movement_speed"]["percent_of_mech_base_speed"]["percent"])
            )
            block = doc.get("damage_pressure") or {}

            candidates = [
                ("contact_diameter_m", diameter, diameter_basis),
                ("contact_start_distance_m", diameter / 2 + player_radius,
                 f"{diameter_basis} / 2 + {player_radius}"),
                ("move_speed_mps", percent / 100 * base_speed,
                 f"percent_of_mech_base_speed / 100 x {base_speed}"),
                ("contact_damage", Fraction(str(doc["contact_damage"])), "authored contact_damage"),
                ("control_resistance", Fraction(str(doc["control_resistance"]["percent"])) / 100,
                 "authored control_resistance.percent / 100"),
            ]
            if "hits_to_defeat_100_hull" in block:
                candidates.append(
                    ("hits_to_defeat_100", Fraction(str(block["hits_to_defeat_100_hull"])),
                     "authored damage_pressure.hits_to_defeat_100_hull")
                )
            if "continuous_overlap_time_to_defeat_seconds" in block:
                candidates.append(
                    ("continuous_overlap_ttd_s",
                     Fraction(str(block["continuous_overlap_time_to_defeat_seconds"])),
                     "authored damage_pressure.continuous_overlap_time_to_defeat_seconds")
                )

            for column, got, basis in candidates:
                raw = row.get(column)
                if raw is None or raw == "":
                    continue
                compared += 1
                want = Fraction(raw)
                if want == got:
                    exact_hits += 1
                    continue
                key = (actor, column)
                places = _csv_decimals(raw)
                quantum = Fraction(10) ** places
                rounded = Fraction(int(got * quantum + Fraction(1, 2)), quantum)
                if key in CSV_MIRROR_ROUNDED and rounded == want:
                    declared_used.add(key)
                    continue
                mismatches.append(
                    f"{actor}.{column}: CSV {raw} vs content {got} (= {float(got)!r}, from "
                    f"{basis})"
                )

    stale = sorted(set(CSV_MIRROR_ROUNDED) - declared_used)
    rows = [
        (
            f"docs CSV vs content/: every shared value agrees ({compared} compared, "
            f"{exact_hits} exactly, {len(declared_used)} at the CSV's stated precision)",
            0,
            len(mismatches),
            "ok" if not mismatches else "FAIL",
        ),
        (
            f"comparisons made (vacuity guard; a mirror check over 0 values passes for free)",
            CSV_MIRROR_EXPECTED_COMPARISONS,
            compared,
            "ok" if compared == CSV_MIRROR_EXPECTED_COMPARISONS else "FAIL",
        ),
        (
            "declared lower-precision pairs that no longer diverge (stale exceptions)",
            0,
            len(stale),
            "ok" if not stale else "FAIL",
        ),
    ]
    if mismatches:
        fail(
            f"A30 {len(mismatches)} value(s) disagree between {rel(PRESSURE_CSV)} and content/. "
            f"Both carry the survivability report and neither is derived from the other, so a "
            f"divergence is a silent contradiction between two mirrors: {sorted(mismatches)[:10]}"
        )
    if compared != CSV_MIRROR_EXPECTED_COMPARISONS:
        fail(
            f"A30 compared {compared} value(s), expected {CSV_MIRROR_EXPECTED_COMPARISONS}. A "
            f"mirror-agreement rule that compares nothing passes vacuously, so the count is "
            f"asserted. If a column or an actor was legitimately added or removed, update "
            f"CSV_MIRROR_EXPECTED_COMPARISONS deliberately."
        )
    if missing_actors:
        fail(
            f"A30 {len(missing_actors)} CSV actor(s) have no definition under content/enemies/ or "
            f"content/bosses/: {missing_actors}"
        )
    if stale:
        fail(
            f"A30 {len(stale)} declared lower-precision pair(s) now agree exactly. A stale "
            f"exception silently widens the rule - delete it: {stale}"
        )
    return rows


def _numeric_multiset_at_ref(ref: str, paths: list[str]) -> dict[tuple[str, str], object]:
    out: dict[tuple[str, str], object] = {}
    for path in paths:
        blob = subprocess.run(
            ["git", "-C", str(REPO_ROOT), "show", f"{ref}:{path}"],
            capture_output=True,
            text=True,
        )
        if blob.returncode != 0:
            continue
        for pointer, value in numeric_pointer_leaves(json.loads(blob.stdout)):
            out[(path, pointer)] = value
    return out


def check_derived_removal_delta() -> list[tuple]:
    """A29 - the measured numeric delta IS the committed expectation, per element.

    ONE ROW, AND DELIBERATELY ONE. Earlier drafts also asserted "0 numeric leaves
    added" and "0 surviving numeric leaves changed value" against the sweep ref.
    Both are true of THIS diff and neither belongs in a standing validator: they
    are one-shot properties of one commit range, so the first ordinary tuning
    commit after merge - EN-01 hull 20 -> 25, an authored non-derived value -
    would fail a rule about derived values. Worse, the only way to clear that
    failure is to re-pin sweep_ref to a newer commit, which makes A29 compare the
    tree against itself and destroys the prediction-first property that is the
    whole point. Those two measurements are evidence for this pull request and
    live in its body. Set equality over the removal set is the invariant that
    holds for every future commit, so it is the one that stays here.
    """
    expectation = load_derived_expectation()
    if not expectation:
        return []
    ref = expectation["sweep_ref"]

    listing = subprocess.run(
        ["git", "-C", str(REPO_ROOT), "ls-tree", "-r", "--name-only", ref, "content/"],
        capture_output=True,
        text=True,
    )
    if listing.returncode != 0:
        fail(
            f"A29 could not read the sweep ref {ref[:12]} out of git, so the removal delta is "
            f"unmeasured. This rule is not allowed to pass by being unable to run: "
            f"{listing.stderr.strip()}"
        )
        return [("sweep ref readable", ref[:12], "unreadable", "FAIL")]
    sweep_paths = sorted(p for p in listing.stdout.splitlines() if p.endswith(".json"))

    before = _numeric_multiset_at_ref(ref, sweep_paths)
    after: dict[tuple[str, str], object] = {}
    for path in sorted(CONTENT.rglob("*.json")):
        for pointer, value in numeric_pointer_leaves(json.loads(path.read_text())):
            after[(rel(path), pointer)] = value

    measured_removed = {key: value for key, value in before.items() if key not in after}

    predicted = {(f, p): v for f, p, v in expectation["removed_numeric_multiset"]}
    n = len(predicted)

    missing = sorted(key for key in predicted if key not in measured_removed)
    unexpected = sorted(key for key in measured_removed if key not in predicted)
    wrong_value = sorted(
        f"{f}.{p}: predicted {predicted[(f, p)]!r}, tree had {measured_removed[(f, p)]!r}"
        for (f, p) in predicted.keys() & measured_removed.keys()
        if predicted[(f, p)] != measured_removed[(f, p)]
    )

    equal = not missing and not unexpected and not wrong_value
    rows = [
        (
            f"set equality over {n} element(s): predicted removals == measured removals",
            f"{n} of {n}",
            f"{n - len(missing)} matched, {len(unexpected)} unpredicted, "
            f"{len(wrong_value)} value mismatch(es)",
            "ok" if equal else "FAIL",
        ),
    ]
    if not equal:
        fail(
            f"A29 the removal set measured against {ref[:12]} is NOT the committed expectation. "
            f"This is set equality over {n} elements, not a total: "
            f"{len(missing)} predicted-but-still-present {missing[:6]}, "
            f"{len(unexpected)} removed-but-unpredicted {unexpected[:6]}, "
            f"{len(wrong_value)} predicted with the wrong value {wrong_value[:6]}"
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
# A16 - the numeric percentage-point policy (40:95). Four rules, all decidable
# from key names and numbers, so all four are failures. See the constants above
# for what this replaced and why.
# --------------------------------------------------------------------------


def check_percentage_point_policy(docs: dict[Path, object]) -> list[tuple]:
    """A16 - percentage points are numbers, are not normalized factors, the
    compiler's normalized factor is never authored beside them, and no number
    sits under a relative-magnitude name that says neither percent nor a unit.

    Four rules. The fourth covers what the first three cannot reach, because
    each of them begins by asking whether the name says percent. It is a closed
    vocabulary (RELATIVE_MAGNITUDE_TOKEN), and nothing beyond that vocabulary is
    claimed.
    """
    no_number: list[str] = []
    factor_valued: list[str] = []
    hybrid_names: list[str] = []
    twins: list[str] = []
    unnamed_magnitude: list[str] = []
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
                inherits_percent = any(PERCENT_TOKEN_KEY.search(a) for a in ancestors)
                is_number = not isinstance(value, bool) and isinstance(value, (int, float))
                # Rule 2 reaches container leaves through the ancestry.
                if key in PERCENT_CONTAINER_KEY and is_number and inherits_percent:
                    checked += 1
                    if value != 0 and abs(value) < 1:
                        factor_valued.append(f"{name}{jpath[1:]} = {value}")
                # Rule 4 - a NUMBER under a name that does not say percent. This is
                # the case rules 1-3 never reach, because all three ask "does the
                # name say percent?" first. A relative-magnitude name that declares
                # no unit and no kind cannot state whether its number is percentage
                # points or a scale, and 40:95 permits percentage points only under
                # a name that says percent.
                elif (
                    is_number
                    and not inherits_percent
                    and RELATIVE_MAGNITUDE_TOKEN.search(key)
                    and not UNIT_OR_KIND_TOKEN.search(key)
                ):
                    unnamed_magnitude.append(f"{name}{jpath[1:]} = {value}")
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
        (
            "relative magnitude under a name that declares no unit",
            0,
            f"{len(unnamed_magnitude)} bare number(s)",
            "ok" if not unnamed_magnitude else "FAIL",
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
    if unnamed_magnitude:
        fail(
            f"{len(unnamed_magnitude)} numeric value(s) sit under a relative-magnitude name (bonus, "
            f"penalty, increase, reduction, ...) that says neither percent nor a unit, so the number "
            f"could be percentage points or a multiplicative scale and the name does not say which. "
            f"40:95 allows human-readable percentage points only under a name that says percent, and "
            f"40:94 requires an ambiguous numeric name to carry a unit suffix: rename to "
            f"<stem>_percent, or to <stem>_multiplier if it is a scale, or add the unit suffix: "
            f"{unnamed_magnitude[:10]}"
        )
    return rows


# --------------------------------------------------------------------------
# A24 - no repo path, and no line number, may hide in a domain value.
#
# source_refs was cleaned in an earlier pass, but the citations had moved next
# door: eleven effect.stacking_classification strings carried a parenthetical
# "(docs/68-utility-catalog.md:253)", a weapons note carried one, and
# hyper-gold-sites.json held a repo path in a field named beacon_response_source.
# A line number is unstable wherever it hides, and the doc_id#anchor form
# (40:87) is the only citation form the envelope names, so the rule is scoped to
# the value, not to one field.
#
# WHAT THIS USED TO BE, AND WHY IT WAS REPLACED. The pattern was `docs/.*\.md`,
# which pins three incidental spellings of one path - the literal directory name,
# a forward slash, and a lowercase `.md` - and matches on none of them being the
# unstable thing. Six citation forms walked straight through it: no extension
# (`docs/40-mining-and-extraction`), a backslash separator, no `docs/` prefix
# (`40-mining-and-extraction.md:104`), uppercase (`DOCS/...MD:104`), and the
# `.markdown` extension. It also missed two defects PRESENT IN THIS TREE, which is
# how the narrowness was confirmed rather than assumed: a `content/`-prefixed repo
# path in an encounter-schedule value, and a bare extensionless `docs/68` in a
# UTL-A1 statement. Both are the class Ruling 25 removed 13 of; the old pattern
# simply could not see them.
#
# The replacement is two rules, each matching what is actually wrong:
#
#   A24a THE LINE NUMBER. Any path-like token followed by `:<digits>`, in any
#        spelling: either slash separator, any case, extension optional when a
#        separator is present, and no directory name required. A line number is
#        the unstable thing - it moves whenever the cited document is edited - so
#        this rule keys on it rather than on how the path in front of it is spelled.
#
#   A24b THE REPO PATH. A repository directory (docs, src, content, tools, assets)
#        followed by a separator and a path character, in any case and with either
#        separator. A repo path is a defect even with no line number on it, because
#        40:87 names doc_id#anchor as the citation form and a path is not one.
#
# A BARE `#anchor` IS DELIBERATELY OUT OF SCOPE, and the test below records it as
# not matching. `#hyper-gold-sites` is not a defective citation form: it is HALF OF
# THE SANCTIONED ONE. 40:87 names doc_id#anchor, A9 already resolves every anchor
# that appears in source_refs against the real heading slugs of the cited document,
# and a bare `#slug` carries neither a path nor a line number, so nothing about it
# is unstable in the way this assertion is about. Flagging it would fire on the
# very spelling the envelope endorses.
# --------------------------------------------------------------------------

# A24a - a path-like token carrying a line number, in any spelling.
LINE_NUMBER_IN_VALUE = re.compile(
    r"""(?ix)
    (?:
        [\w.\-]+ (?: [/\\] [\w.\-]+ )+          # any path with >=1 separator, extension optional
      | [\w\-]+ \. (?: md | markdown | mdown | rst | txt | json | ya?ml | cs | py )
                                                # or a bare filename with a document extension
    )
    \s* : \s* \d+
    """
)
# A24b - a repository path in a domain value, line number or not.
REPO_PATH_IN_VALUE = re.compile(
    r"(?i)(?:^|[^\w/\\])(?:docs|src|content|tools|assets)[/\\][\w.\-]"
)

A24_RULES = (
    (
        "no line-number citation in any string value",
        LINE_NUMBER_IN_VALUE,
        "a line number moves whenever the cited document is edited, so it is unstable wherever "
        "it hides; source_refs carries doc_id#anchor instead",
    ),
    (
        "no repository path in any string value",
        REPO_PATH_IN_VALUE,
        "a repo path is not a citation form - 40:87 names doc_id#anchor - and it is a defect with "
        "or without a line number attached",
    ),
)


def check_no_doc_paths_in_values(docs: dict[Path, object]) -> list[tuple]:
    """A24 - no line-number citation and no repository path in any string value.

    Matched on the unstable thing rather than on one spelling of a path: either
    slash separator, any case, extension optional. A bare `#anchor` is out of
    scope by design - see the comment above.
    """
    rows = []
    for label, rx, why in A24_RULES:
        hits: list[str] = []
        for path, doc in sorted(docs.items()):
            for jpath, _, value in walk(doc):
                if isinstance(value, str) and rx.search(value):
                    hits.append(f"{rel(path)}{jpath[1:]} = {value!r}")
        rows.append((label, 0, len(hits), "ok" if not hits else "FAIL"))
        if hits:
            fail(
                f"{len(hits)} string value(s) under content/ violate A24 ({label}): {why} "
                f"(40:87): {hits[:10]}"
            )
    return rows


# --------------------------------------------------------------------------
# A26 - no `null` anywhere under content/, with NO declared exceptions.
#
# THE RULING. A null in a source definition is never legal. content/README.md
# used to define a null as "the document states no value", which made absence
# expressible two ways - an omitted key and a nulled key - for one meaning. 40:90
# settles it: "Optional fields have explicit defaults materialized into the
# canonical bundle so runtime never guesses." An absent optional field gets its
# default; a present-and-null one asks runtime to guess, which is what that line
# forbids. So absence is spelled by omitting the key.
#
# 275 nulls across 101 of the 138 definition files were disposed of in one pass:
# 246 keys omitted, 20 relic rarity/weighting fields removed as fields no schema
# will declare, 4 boss armor fields removed for the same reason, 3
# external_numerics[n].value keys removed as shape defects, and 2 nested id keys
# removed because the objects they sat on are not independently addressable.
#
# THERE IS NO EXCEPTION SET, deliberately. An earlier plan declared the two nested
# `id` nulls in content/maps/standard-map-generation-contract.json as tolerated
# exceptions pending minted IDs. That is superseded: destructible_rock and
# health_pack are nested objects inside MGC-01, which already carries an ID, and
# neither is reachable except through MGC-01 plus a JSON pointer - nothing
# references either by ID. They are parameters of the map contract, not
# definitions, so the `id` key was removed rather than minted or tolerated. With
# it gone the assertion is unconditional, which is stronger than one carrying two
# permanent exemptions: an exception set is a place for a null to hide.
#
# This scans EVERY *.json under content/, including content/localization/en.json,
# which load_definitions() skips. A24-style value rules only see definition files;
# a null is illegal in the localization catalog too.
# --------------------------------------------------------------------------


def null_paths(obj, path="$"):
    """Yield the JSON path of every null in a document."""
    if obj is None:
        yield path
    elif isinstance(obj, dict):
        for key, value in obj.items():
            yield from null_paths(value, f"{path}.{key}")
    elif isinstance(obj, list):
        for index, value in enumerate(obj):
            yield from null_paths(value, f"{path}[{index}]")


def check_no_nulls() -> list[tuple]:
    """A26 - no null anywhere under content/, with no declared exceptions."""
    hits: list[str] = []
    scanned = 0
    for path in sorted(CONTENT.rglob("*.json")):
        doc = load_json(path)
        if doc is None:
            continue
        scanned += 1
        hits.extend(f"{rel(path)}{p[1:] or ' (whole document)'}" for p in null_paths(doc))
    rows = [
        (
            f"no null anywhere under content/ ({scanned} file(s) scanned, 0 exceptions declared)",
            0,
            len(hits),
            "ok" if not hits else "FAIL",
        )
    ]
    if hits:
        fail(
            f"{len(hits)} null(s) under content/. A null in a source definition is never legal: "
            f"40:90 materializes an explicit default for every absent optional field, so absence is "
            f"spelled by OMITTING the key and a present-and-null field asks runtime to guess. Omit "
            f"the key; if the field should not exist at all, remove it and record the removal. There "
            f"is no exception set to add it to: {sorted(hits)[:15]}"
        )
    return rows


# --------------------------------------------------------------------------
# A27 - the quotation matcher's corpus premise, asserted rather than documented.
#
# WHY THIS ASSERTION LOOKS BACKWARDS. Every other check here asserts something
# about content/ against docs/. This one asserts something about docs/ on behalf
# of a matcher described in content/quote-verification-audit.md. That matcher's
# adopted rule - fail when a stored string begins at a sentence boundary, carries
# its own trailing terminator, and the source sentence continues past it - was
# measured at 2 hits with zero false positives across the audit's whole matched set.
# That set is 1,072 records: the 806 decidable matches of the audit's §2 tree-state
# table (782 exact + 24 matching under a named rule) plus the 266 matches that sit
# below its decidability gate. Every number in this comment derives from that table;
# none of them is quoted from prose. The measurement
# is only valid while `.` reliably means "end of sentence" in docs/. It does today.
# Nothing keeps it true tomorrow, and "it can stop being true silently" is exactly
# the property a documented assumption cannot address: a documented assumption is
# a fail-open with a footnote.
#
# WHAT IS IN THE LIST AND WHY. One criterion: the period is not a sentence end.
# That admits ordinary prose abbreviations and excludes decimals and unit
# suffixes ("0.80M", "1.5 s", "45.6"), which are not sentence-terminator
# candidates in the first place - a decimal point is not followed by a
# sentence-initial capital, and the audit measured zero decimal-point misfires
# across all 1,072 records in both directions.
#
# WORD-BOUNDARY MATCHING IS LOAD-BEARING, not tidiness. Unbounded case-insensitive
# substring matching for "st." hits 93 places in this corpus across 21 distinct word
# forms - "first." 24, "specialist." 15, "cost." 10, "test." 9, "manifest." 6,
# "burst." 4, and fifteen more - and "ver." hits 5, being "forever.", "solver." and
# "hover.". Every one of them is a sentence end, not an abbreviation, and the word
# boundary below removes all 98. A check that fires on those would be turned off
# within a day, which would leave no check.
#
# THE MESSAGE IS THE POINT. When this fails, no content string is wrong. What is
# wrong is that the quotation rule's premise has lapsed. The message must send the
# reader to the matcher, because a message that blames a quotation sends them to
# innocent data and teaches them the check is noise.
# --------------------------------------------------------------------------

SENTENCE_INTERNAL_ABBREVIATIONS = (
    "e.g.", "i.e.", "etc.", "approx.", "cf.", "vs.", "viz.", "resp.",
    "no.", "fig.", "eq.", "sec.", "p.", "pp.", "ca.", "al.", "esp.", "incl.",
)

ABBREVIATION_RX = tuple(
    (abbr, re.compile(r"(?<![A-Za-z0-9])" + re.escape(abbr), re.IGNORECASE))
    for abbr in SENTENCE_INTERNAL_ABBREVIATIONS
)


def check_no_abbreviation_periods(docs_root: Path = DOCS) -> list[tuple]:
    """A27 - docs/ carries no sentence-internal abbreviation period."""
    hits: list[str] = []
    scanned = 0
    for path in sorted(docs_root.rglob("*.md")):
        scanned += 1
        text = path.read_text(encoding="utf-8")
        for lineno, line in enumerate(text.splitlines(), 1):
            for abbr, rx in ABBREVIATION_RX:
                for m in rx.finditer(line):
                    hits.append(f"{rel(path)}:{lineno} '{m.group(0)}' (matched '{abbr}')")
    rows = [
        (
            f"sentence-internal abbreviation periods under docs/ "
            f"({scanned} file(s), {len(SENTENCE_INTERNAL_ABBREVIATIONS)} token(s) searched)",
            0,
            len(hits),
            "ok" if not hits else "FAIL",
        )
    ]
    if hits:
        fail(
            f"THE QUOTATION MATCHER'S ASSUMPTIONS NO LONGER HOLD - the matcher needs "
            f"revisiting, and no content string is implicated by this failure. "
            f"content/quote-verification-audit.md adopts a quotation rule that treats "
            f"'.' as an unambiguous sentence terminator in docs/. It was measured safe "
            f"(2 hits, zero false positives, across that document's whole matched set of "
            f"1,072 records = its 806 decidable matches plus the 266 matches below its "
            f"decidability gate; both figures are in the tree-state table in its §2) "
            f"against a corpus containing no abbreviation periods. docs/ now contains "
            f"{len(hits)}, so the rule can misfire on complete, honest quotations that "
            f"happen to end just before one. Re-measure the rule against the corpus and "
            f"either teach it this abbreviation or narrow it; do NOT edit the quotation "
            f"that the rule flags: {sorted(hits)[:15]}"
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


def table(title: str, headers: tuple, rows: list[tuple], notes: tuple = ()) -> None:
    """Print an assertion table, then any `notes` beneath it.

    `notes` exists so a rule can disclose WHAT IT CANNOT SEE on the same output a
    passing run produces. A limitation that lives only in a docstring or in
    content/transcription-notes.md is not disclosed to the person reading a green
    run, which is the only person who needs to be told.
    """
    print(f"\n{title}")
    if rows:
        cols = [str(h) for h in headers]
        body = [[("" if c is None else str(c)) for c in row] for row in rows]
        widths = [max(len(cols[i]), *(len(r[i]) for r in body)) for i in range(len(cols))]
        print("  " + "  ".join(c.ljust(widths[i]) for i, c in enumerate(cols)))
        print("  " + "  ".join("-" * widths[i] for i in range(len(cols))))
        for row in body:
            print("  " + "  ".join(row[i].ljust(widths[i]) for i in range(len(cols))))
    else:
        print("  (nothing to report)")
    for note in notes:
        for i, line in enumerate(textwrap.wrap(note, 104)):
            print(("  ! " if i == 0 else "    ") + line)


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
    derived_family_rows, derived_family_notes = check_derived_family_absence(docs)
    derived_value_rows, derived_value_notes = check_derived_family_values(docs)
    csv_mirror_rows = check_csv_mirror_agreement(docs)
    removal_delta_rows = check_derived_removal_delta()
    prefix_rows = check_scope_prefixes(docs)
    bound_rows = check_bound_spelling(docs)
    inventory_rows = check_file_inventory()
    percent_rows = check_percentage_point_policy(docs)
    doc_path_rows = check_no_doc_paths_in_values(docs)
    polarity_rows = check_polarity_agreement(docs)
    null_rows = check_no_nulls()
    abbreviation_rows = check_no_abbreviation_periods()
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
        "A28 layer 1 of 2 - NAME: no derived-value family reappears under a matching name "
        "(six rules, six scopes; catches a rename only within its own word class)",
        ("check", "expected", "actual", "status"),
        derived_family_rows,
        derived_family_notes,
    )
    table(
        "A28 layer 2 of 2 - VALUE: no removed value sits at a non-operand leaf inside its own "
        "derivation site (exact Fractions; indifferent to name, unit suffix and arity)",
        ("check", "expected", "actual", "status"),
        derived_value_rows,
        derived_value_notes,
    )
    table(
        "A30 docs/data/contact-damage-pressure.csv and content/ agree on every shared value "
        "(two unguarded mirrors of one report; exact Fractions, declared exceptions only)",
        ("check", "expected", "actual", "status"),
        csv_mirror_rows,
    )
    table(
        "A29 Removal delta == the expectation committed before the removals",
        ("check", "expected", "actual", "status"),
        removal_delta_rows,
    )
    table(
        "A24 No line-number citation and no repository path in any string value",
        ("check", "expected", "actual", "status"),
        doc_path_rows,
    )
    table(
        "A26 No null anywhere under content/ (no declared exceptions)",
        ("check", "expected", "actual", "status"),
        null_rows,
    )
    table(
        "A27 Quotation-matcher corpus premise (no abbreviation periods in docs/)",
        ("check", "expected", "actual", "status"),
        abbreviation_rows,
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
