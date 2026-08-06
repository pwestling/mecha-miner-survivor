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
      rule is about unit suffixes, so the mid-name spellings such as
      percent_of_mech_base_speed are correct and are not flagged. Measured
      with PERCENT_TOKEN_KEY, this tree holds 50 such occurrences across 33
      distinct property names; the figure here read 52 and matched neither
      quantity, so it is now stated with the unit it is counted in. Rule 4
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
      Checked on KEY NAMES in the covered directories. That catches a
      rename ONLY INTO A NAME THE PATTERN STILL MATCHES, which is a narrow
      thing and was previously written as though it were a general one: an
      earlier revision of this paragraph said "a rename inside one of them
      cannot slip past" and then, in the next sentence, that a value
      reappearing under an unrelated name is not caught. Those contradict,
      and the second is the true one. A20 has NO value layer, so a derived
      footprint value reintroduced under any name the pattern misses, or in
      an uncovered directory, passes - see the per-rule scopes above and
      README.md.
      Mandate: docs/technical/40-content-data-and-validation.md:114
      ("Validation derives world speeds/footprints and compares them with
      the survivability report")                                      FAILURE

  A21 content/ holds exactly as many DEFINITION *.json files as the A28
      manifest has pairs, so a definition in a directory no A12 row covers is
      still caught, AND the non-JSON files under content/ are
      exactly the three named in EXPECTED_CONTENT_NON_JSON (README.md,
      quote-verification-audit.md, transcription-notes.md).
      The count's expectation is len() of the A28 manifest, not a literal.
      There was a literal 138 here; A28 is the record of WHICH 138, and two
      independent literals asserted by a comment to be the same number is a
      defect this repository has already been burned by. The count row is
      therefore redundant BY CONSTRUCTION - it still reddens on an added or
      deleted definition, but it cannot disagree with the manifest. A21 no
      longer sees a RENAME at all; that is A28's row, and the count row
      staying green under a rename is exactly the hole A28 was added for.
      BOTH rows exclude every directory in NON_DEFINITION_DIRS, via
      in_non_definition_dir(), which is the population load_definitions()
      loads. The count row used to be a bare CONTENT.rglob("*.json") that
      never consulted NON_DEFINITION_DIRS, so content/localization/en.json
      was counted (139) and the first file under content/schemas/ - a
      directory this script itself declares is not a catalog of definitions
      - failed the row with "content/ holds 140 *.json file(s), expected
      139" on a branch that had added no definition. The expectation is
      rebased onto the definition population rather than raised to agree
      with the polluted one.
      The non-JSON row used to PRINT the file list next to a blank
      expectation and a hardcoded "ok" - it could not fail, so a stray file
      under content/ was reported and tolerated in the same breath. It now
      asserts the exact list.
      Negative control: an empty content/probe.txt -> FAIL, "content/ holds
      non-JSON files [... 'content/probe.txt' ...], expected exactly
      [...]". A file under content/schemas/ -> verdict UNCHANGED, which is
      the control that distinguishes this row from the one it replaced; a
      definition added under a real catalog directory still FAILS with the
      count.                                                          FAILURE

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
      pass that added this, and that tally was counted as that pass
      finished: 246 keys omitted, 20 relic rarity/weighting fields and 4
      boss armor fields REMOVED as fields no schema will declare, 3
      external_numerics[n].value keys removed as shape defects, and 2
      nested id keys removed because the objects holding them are not
      independently addressable. It is the record of that one disposal and
      no assertion recomputes it - what a green run asserts is zero nulls
      today, which says nothing about how many there once were. The two
      nested ids were briefly planned as declared exceptions; removing the
      key instead made the assertion unconditional.
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
      RE-MEASURED 2026-08-06, and only one of those figures held: "ver."
      inside 5 reproduces exactly across docs/**/*.md, its three named
      forms being the whole set. The "st." figures do not - unbounded
      matching finds it inside 100 words today, or 79 setting the
      hyphenated doc slugs aside, across 22 forms rather than 21 - and no
      counting rule tried reaches either 93 or 21, so the rule behind them
      is not recoverable from this text and neither is restated above as a
      new number. What the bound is FOR survives the discrepancy intact:
      every form either count matches is an ordinary word and none is an
      abbreviation, and no assertion recomputes any of it - what a green
      run asserts is that the bounded form finds zero today.
      THE FAILURE MESSAGE POINTS AT THE MATCHER, NOT AT A QUOTATION. The
      day someone writes "e.g." in a design document, nothing is wrong with
      any content string; what is wrong is that the quotation rule's
      premise no longer holds and the rule needs revisiting.
      Negative control: docs/ must not be modified, so the check runs
      against a scratch tree - a byte copy of docs/ with "e.g." inserted
      into one sentence -> FAIL naming that file and token.
      Mandate: content/quote-verification-audit.md (adopted rule and its
      stated corpus dependency)                                     FAILURE

  A31 RENAMED FROM A28. If you arrived here from a review comment, a commit
      message or a branch note that says "A28" and means the derived-value
      families, this row is that rule and A31 is its label. The number is out
      of sequence with its neighbours for that reason and not by accident.
      WHY IT MOVED: two streams independently claimed A28. This branch's
      derived-value-family rule (PR #10) and the definition (path, id)
      manifest (master, PR #12) both wrote "A28", and the merge that brought
      master in put both under one label. Master's shipped: it is on the trunk
      and referenced from its own A21 row, this file's manifest section, the
      tool README, content/README.md and .gitattributes. An identifier that
      has shipped does not move - the same principle content IDs follow. This
      branch's label had not shipped, so this is the side that moved. A28
      below is master's and keeps its number. Two rules under one label is a
      defect that compounds with every new reference, so it was fixed at the
      merge rather than deferred.
      ONE LABEL, NOT TWO, for the two layers below, and THE MAPPING IS THE
      REASON: two table numbers would make "the rule the review calls A28"
      ambiguous, which is the thing this note exists to prevent - a reader
      arriving from an un-editable comment must land on one row rather than
      choose between two. The layers also share one expectation file, one
      mandate set and one summary heading, which is consistent with keeping
      them together but is the weaker reason.
      (An earlier draft cited A24a/A24b here as precedent for naming a rule's
      internal parts. WITHDRAWN - that pair is two labels over one rule's two
      halves, so it is precedent for SPLITTING and argued the opposite of what
      it was cited for. See content/transcription-notes.md.)
      THE CAUSE IS NOT FIXED HERE. Labels are still allocated by whoever adds
      one, on their own branch, so the next two parallel additions collide the
      same way - see the minted-assertion-label-table open item in
      content/transcription-notes.md.
      No definition carries a compiler-derived value from any of the SIX
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
      "MEASURED" THERE MEANS HAND-RUN, NOT COMPUTED, and the distinction
      matters because every other figure in this row is one the tool just
      computed: the out-of-word-class reintroduction reach - caught 0 of 6 -
      is a HAND-RUN PROBE, six injections done by hand, one per family, and
      no assertion in any run recomputes it. The note this layer prints
      carries the same marker.
      Segments, not just the leaf key, because some families store the
      number under a generic leaf (`amount`, `minimum`, `maximum`) inside a
      specifically named parent - a leaf-key-only rule would miss
      total_payout_per_map.amount entirely.
      EACH NAME ROW NOW PRINTS ITS OWN DENOMINATOR - numeric leaves visited
      and files scanned, counted by the walk as it runs rather than restated
      from a hand-run figure - and one further row ASSERTS the walk:
      non-zero leaves over exactly DERIVED_NAME_WALK_SCOPE_FILES files. The
      six family rows cannot catch an emptied walk between them, because
      each reports "0 hits" and a rule that searched no file reports "0
      hits" too. Before this the six printed `0 / 0 / ok` with no coverage
      figure at all, the only assertion in this file reporting a result with
      no measure of what it looked at. Negative controls, each injected
      alone and reverted: emptying one family's `scopes` list, and
      misspelling a scope directory, each FAIL naming the collapse.
      (2) a VALUE layer, which is what a name rule cannot do: for each
      removed value, no non-operand numeric leaf inside its own derivation
      site may carry that value. Exact Fractions, no tolerance. This one
      survives a rename, a relocation within the site, a different unit
      suffix, and scalar -> [scalar]. THAT REINTRODUCTION REACH IS A
      HAND-RUN PROBE TOO, not a figure any run recomputes: rename, unit
      suffix and arity change caught 6 of 6 and a relocation OUT of the site
      caught 0 of 6, twelve injections done by hand, six per row. The three
      radii below are the computed figures in this paragraph; these two are
      not. Its RADIUS is the limit and is stated
      rather than hidden: the derivation site, not the file and not the
      scope. The three radii are COMPUTED, in the generator's
      measure_search_radii(), under one definition on the pinned sweep ref,
      and carried in search_radius_measurement in the expectation file so
      the ratio a reader is shown reproduces from the artifact: 1 : 40 : 668
      coincidental pairs at site, file and scope radius, almost all
      magnitude coincidences between unrelated quantities. An earlier
      revision quoted 55 and 400; no code computed either and neither
      reproduced. One exception is declared, enumerated and justified, and
      it FAILS if it stops colliding on the current tree - a claim that was
      false in both tools before this pass.
      A SECOND LIMIT, which the reported radius concealed: the guard is only
      as large as what survived inside the site. 13 of the 115 removed
      values sit in a container the removal left with no numeric leaves at
      all, so their guard searches nothing. That count is asserted
      (EMPTY_SITE_GUARD_RECORDS) and the per-record distribution prints
      beneath the table, because the earlier line - "299 numeric leaves
      across 115 removed values" - is a mean of 2.6 that reads as coverage
      of all 115 and is what hid the empty-site defect: for a root-level
      pointer the site was computed as "" and then filtered out as falsy, so
      six records searched zero leaves and could not fail on anything.
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
      as SET EQUALITY over all 115 elements - each (file, pointer, value)
      present in one side and the other - not as two totals that happen to
      agree. 115 == 115 would also hold if one value were removed by
      mistake and a different one kept by mistake; element-wise equality
      would not.
      Measured, not asserted: the sweep-ref tree is read out of git at the
      SHA the expectation file names, its numeric leaves are enumerated,
      and the worktree's are subtracted. The expectation file was committed
      BEFORE any content/ file changed (see that commit's --stat), so this
      check compares a prediction against a measurement rather than a diff
      against itself.
      It does NOT assert that the added side is empty. That row was
      deliberately deleted this pass: it is a property of one commit range,
      not an invariant. Adding an authored numeric leaf to EN-01 PASSES, by
      design.
      ONLY HALF OF THIS IS A STANDING INVARIANT, and the asymmetry is
      stated because it is not obvious. The `missing` half - every predicted
      removal must still be missing - holds for every future commit. The
      `unexpected` half - nothing else may be missing - does not: deleting
      any authored numeric leaf, for any reason, fails it. Controlled:
      deleting EN-01.earliest_minute FAILS with "1 removed-but-unpredicted",
      while retuning EN-01 hull 20 -> 25 in place passes. So a future commit
      that legitimately deletes a field will false-fail A29 and the fix is
      to re-derive the expectation from a newer sweep ref, deliberately.
      Mandate: docs/technical/40-content-data-and-validation.md:100
      ("Derived values include source operands and calculation version in
      reports"), which is what makes a stored operand-plus-result pair the
      compiler's to emit and not content's to author         FAILURE

  A30 docs/data/contact-damage-pressure.csv and content/ agree on every
      value they share - 98 comparisons, seven columns x 14 actors, exact
      Fraction arithmetic. Four columns compare against an authored content
      field; three against values derived from surviving operands, which is
      the comparison docs/40 section "Enemies and bosses" describes. Two
      unguarded mirrors of one report is the shape where a later edit to
      either produces a silent contradiction, so agreement is ASSERTED
      rather than observed (Ruling 45 observed it and nothing kept it).
      The comparison COUNT is asserted at 98, because a mirror check over
      zero values passes for free.
      NO TOLERANCE, including inside the declared exceptions: a declared
      lower-precision pair names the CSV's written value AND the single
      exact content-side value it covers, so the band an earlier revision
      allowed ([0.61875, 0.625) for EN-07's body_scale_multiplier) is gone.
      Two pairs are declared, both EN-07's, and both record an OPEN design
      question rather than settling it. A declared pair that stops
      diverging FAILS in either direction.
      Not settled by this rule: which mirror is authoritative. When that
      lands the loser becomes derived and A30 becomes redundant in the good
      way rather than wrong.
      Mandate: docs/technical/40-content-data-and-validation.md:114
      ("Validation derives world speeds/footprints and compares them with
      the survivability report") and :203 ("Reports compare with accepted
      gameplay tables")                                     FAILURE
  LABEL MAP: A28 (this branch, before the merge with master) -> A31. Two
     streams independently claimed A28 - this branch's six derived-value
     families and the definition (path, id) manifest immediately below, which
     came from master's PR #12. The A28 below is master's and is the one that
     keeps the number, because it has shipped on the trunk. If a review
     comment, commit message or note says "A28" and describes derived-value
     families, name layers, value layers or the 115 removed values, it means
     A31 above. Enumerated at the merge: master's PR added exactly {A28} and
     this branch added {A28, A29, A30}, so A28 was the only collision - A29
     and A30 are this branch's alone and did not move.
     The allocation problem behind it is recorded as an open item (a minted
     assertion-label table) in content/transcription-notes.md and is
     deliberately not built in this PR.

  A28 The definition population's (relative_path, id) PAIRS equal the
      committed manifest at content-definition-manifest.txt, compared in both
      directions: a path in the tree and not the manifest, a path in the
      manifest and not the tree, and a path in both whose id differs are each
      a separate failure naming the files. A fourth row compares the committed
      file's BYTES against the generator's output byte-for-byte, so the header,
      the line ORDER, whitespace padding and the line endings cannot drift
      either. That row is the ONLY guard for reordering and padding: the three
      pair rows compare two sets and a mapping, so a manifest whose lines are
      reordered or padded still holds the same pairs.
      The comparison reads with read_bytes() and the generator writes with
      write_bytes(), both deliberately. Path.read_text() applies universal
      newlines, so a manifest rewritten entirely in CRLF decoded to exactly the
      generator's LF text: it passed with 0 failures while this very row
      reported "identical", and every line of the file could be rewritten with
      the gate green. Path.write_text() has the mirror defect - it translates
      "\\n" to os.linesep, so the generator would emit CRLF on Windows and a byte
      comparison against its own output could never converge there. A byte
      comparison is the strict one: it keeps the reordering and padding guards
      and adds line-ending drift, whereas relabelling the row as a TEXT
      comparison would have kept the escape and merely described it.
      .gitattributes pins the manifest to eol=lf so a checkout cannot
      manufacture a false failure on a platform that would otherwise convert it.
      WHY PAIRS AND NOT NAMES. Two edits were invisible to every other
      assertion here. (1) Renaming a definition inside its own directory:
      A21's count row compared a NUMBER and was blind to which files those
      were, so `mv content/bosses/BOSS-01.json
      content/bosses/ZZZ-not-a-boss.json` exited 0 with zero failures while
      the non-JSON row beside it asserted an exact named tuple. (2) Editing
      the id inside a file: BOSS-01 -> BOSS-99 still matched A12's
      ^BOSS-\\d{2}$ selector, left the per-directory count at 4 and kept
      uniqueness, so it too exited 0. A name roster closes only the first; the
      pair closes both, and it also catches two files SWAPPING ids, which
      changes no count, no pattern and no uniqueness fact.
      NOT a relocation check - moving a file between directories was already
      caught by A12's per-directory counts and was never open. A28 does redden
      on a relocation too, because the path changed, but it is not what makes
      that case fail.
      WHY A MANIFEST AND NOT stem == id. That was measured and rejected: 130
      of the 138 definitions have stem == id byte-for-byte and 8 do not, and
      the mapping for those 8 is not a function of the string - four
      mining-site classes are SITE-01..04 in DOCUMENT order (alphabetically
      their stems give SITE-03, 02, 04, 01, so it is not even ordinal), plus
      WAV-01, MGC-01, ELT-01 and FORMULA-01. Exempting those directories would
      remove the check from precisely the eight files whose names are prose and
      are therefore the ones anyone would actually rename: nobody tidies
      BOSS-01.json, and standard-ore-seams.json is exactly the file someone
      would.
      THE MANIFEST IS AN EDIT TAX, NOT EVIDENCE, and its header says so.
      Regenerating it makes this check agree with the tree again, so someone
      who renames a file or edits an id and regenerates PASSES. What the
      manifest buys is that the change cannot happen without a reviewable diff
      in the same commit. It does not establish that any path or id is
      correct; the design documents and the A12 rows that cite them do that.
      REGENERATION follows the repository's existing convention rather than a
      new one - tests/shared/GoldenText.cs, where MECHAMINER_GOLDEN_UPDATE=1
      rewrites a golden AND THE TEST STILL FAILS. Same variable, same
      semantics, including failing when the switch is set but the manifest
      already matches. A regeneration can therefore never be the thing that
      turns a run green.
      The population is definition_paths(), which is in_non_definition_dir()
      - the same predicate A21's two rows and load_definitions() use. There is
      no second definition of what counts as a definition file.
      Negative controls, each run and reverted: the rename above -> FAIL (2
      failures, one per direction); the BOSS-01 -> BOSS-99 id edit -> FAIL (1
      failure naming `BOSS-01 -> BOSS-99`); swapping BOSS-01 and BOSS-02's ids
      -> FAIL (1 failure naming both), all three having been PASS/exit 0
      before this check existed.
      Mandate: docs/technical/40-content-data-and-validation.md:80 (`id` is an
      envelope field, "stable category-valid ID") with :185, where the
      canonical bundle "is ordered by category and stable ID" and hashes
      identically "regardless of source file enumeration order". That last
      clause is why the pair is the thing to record: the ID carries the
      bundle's identity and the FILENAME does not, so the file stem is a human
      handle that no compiled output would notice changing. Nothing downstream
      of the compiler can catch a rename, which is precisely why it has to be
      caught here.                                                   FAILURE

  A32 canonical_letter, five rows over content/resources/, each asserted and
      reported separately because each is blind to a different edit:
        1. EXACTLY the six letter definitions carry the key, and the carrier
           set is NAMED (A.json..F.json), not counted. A count of 6 passes
           when the key is deleted from D.json and added to common-ore.json
           in the same edit; the named set does not.
        2. In each of those six, canonical_letter == that file's own id. The
           six comparisons are NAMED on the passing run, not counted: a
           green "6 agree, 0 disagree" tells the reader how many files the
           row's argument rests on but not which, and the set is the half
           worth auditing. Predicate unchanged - only the display.
        3. The six values are six DISTINCT letters and cover exactly
           {A,B,C,D,E,F}. Redundant while rows 1 and 2 both hold, but what
           it is redundant WITH is asymmetric and the earlier wording here
           ("the row that survives if either is ever weakened") claimed
           more than it does. Row 3 is a predicate over the VALUE SET only
           - len(values), distinctness, and set equality with {A..F}. It
           would INHERIT row 1's population claim if row 1 were weakened,
           because six distinct letters cannot be present unless six
           carriers are. It is BLIND to row 2's placement claim and would
           inherit nothing from it: swapping B.json's and C.json's values
           satisfies all three of row 3's conditions, and placement is the
           defect A32 exists to catch.
        4. common-ore.json and hyper-gold.json do not carry the KEY AT ALL.
           A26 already forbids null repo-wide, so `"canonical_letter": null`
           in a currency file is caught with or without this row. Absence is
           nevertheless asserted here because A26 cannot see the defect this
           row exists for: `"canonical_letter": ""` and
           `"canonical_letter": "common-ore"` are both non-null, both pass
           A26, and both assert the thing 40:106 does not say - that a
           currency has a canonical letter. The omission is load-bearing
           content, so it is asserted as omission rather than inferred from
           the absence of a null.
        5. content/resources/ holds exactly 8 definition files, because
           rows 1-4 are all satisfied by a tree with a ninth resource in it.
      WHY A VALUE MULTISET WOULD HAVE PROVEN NOTHING. The six added values
      are the six ids, so they were already leaves of this tree before the
      field existed: a multiset over content/'s leaf values is by
      construction unchanged by this commit and would have reported "no
      values gained or lost" having checked nothing about the only thing
      that changed. Row 2 is the one that binds the new leaf to its
      neighbour, and row 1 is the one that binds the population.
      Negative controls, each injected alone, run, and reverted:
      delete D.json's key -> row 1 FAILs naming the carrier set; swap
      B.json's and C.json's values (multiset preserved) -> row 2 FAILs
      naming both files; A.json "A" -> "B" -> row 3 FAILs on distinctness;
      canonical_letter added to common-ore.json -> row 4 FAILs naming it;
      a ninth resources/*.json -> row 5 FAILs on the count. A.json's value
      set to null FAILs rows 2 and 3 here in addition to A26.
      Mandate: docs/technical/40-content-data-and-validation.md:106, blob
      4cded84 ("Resource definition fields include ID, canonical letter,
      localization keys ..."). NOT the RSC- ID grammar, which is not on this
      ref: no id value is asserted or changed by this row.       FAILURE

Not asserted here: no structural JSON Schema validation happens, because
content/schemas/ (40:36) does not exist yet. Domain field names outside the
envelope are therefore unvalidated and will need one reconciliation pass when
the schemas land. See content/transcription-notes.md.
"""

from __future__ import annotations

import json
import os
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

# A28 - the committed (path, id) manifest of the definition population.
#
# It lives beside this script rather than under content/ deliberately: a file
# under content/ would have to be either a definition (and count itself) or a
# non-JSON file (and join EXPECTED_CONTENT_NON_JSON), and a manifest is neither.
# The two sibling data files expected_citation_deltas.json and
# quote_mismatch_evidence.json already establish that committed tool data lives
# here.
#
# It is TEXT, tab-separated and sorted by path, for the reason
# docs/technical/91-verification-strategy.md gives for goldens generally -
# "canonical, ordered, and reviewable text". One line per definition means a
# rename is one removed line plus one added line and an edited id is a one-line
# change, which is what makes the diff the reviewable artifact.
CONTENT_DEFINITION_MANIFEST = (
    Path(__file__).resolve().parent / "content-definition-manifest.txt"
)

# The repository's convention for regenerating a committed expectation, taken
# from tests/shared/GoldenText.cs: the switch rewrites the file AND the check
# still fails, so a regeneration can never be the thing that makes a run green.
# The variable name is deliberately the same one, because this is the same act.
GOLDEN_UPDATE_VARIABLE = "MECHAMINER_GOLDEN_UPDATE"

# Placeholders for a definition whose id cannot be read. Neither can collide
# with a real id, and neither appears in the committed manifest today: every
# definition parses and every one carries an id (ID_NULL_EXPECTED is empty).
MANIFEST_ID_ABSENT = "<no-id>"
MANIFEST_ID_UNPARSEABLE = "<unparseable>"

# A21 - the DEFINITION *.json inventory under content/. This is the sum of the
# A12 rows, and it is asserted separately so that a definition file appearing in
# a directory A12 does not cover is still caught.
#
# The population is the same one load_definitions() loads: every *.json under
# content/ EXCEPT those beneath a NON_DEFINITION_DIRS directory. It used to be a
# bare CONTENT.rglob("*.json") that never consulted NON_DEFINITION_DIRS, so the
# count was definitions + localization/en.json = 139 and a directory this script
# already declares is not a catalog of definitions still counted toward the
# definition total. The first file authored under content/schemas/ - the
# directory DAT-006 owns, and the one the README notes "does not exist yet" -
# therefore reddened this row as "content/ holds 140 *.json file(s), expected
# 139" on a branch that had added no definition at all. That is a false failure
# charged to the wrong stream, and bumping 139 to 140 would only have laundered
# it by agreeing with the polluted population; the expectation is instead rebased
# onto the population the name now describes.
#
# THE EXPECTED COUNT IS NO LONGER A LITERAL. It is len() of the A28 manifest,
# which is the committed record of WHICH definitions exist. There was a literal
# 138 here, and the manifest would have been a second statement of the same
# number; two independent literals that a comment claims agree is a defect this
# repository has already been burned by, so the count is derived and the two
# cannot drift apart. The count row survives derivation because it is the
# readable summary of the population's size and it still reddens on an added or
# deleted definition - it is redundant BY CONSTRUCTION rather than
# independently, which is the difference that matters.
#
# content/localization/en.json is outside that population along with
# content/schemas/, because NON_DEFINITION_DIRS names them both. en.json loses no
# coverage by leaving - A10/A11 assert it parses, is flat, is lexically sorted,
# is duplicate-free, resolves every referenced key and orphans none, which is
# strictly more than membership in a total - and A26 still scans it for nulls,
# because "no null anywhere under content/" is deliberately a whole-tree claim
# rather than a definition-only one.
#
# content/ also holds three Markdown files - README.md, transcription-notes.md
# and quote-verification-audit.md - which are documentation, not content. So
# `find content -type f` reports 142 while the definition count is 138; that
# difference is correct and is not a discrepancy.
#
# The non-JSON files are NAMED rather than counted, and the A21 row asserts the
# exact list. It previously printed the list beside a blank expectation and a
# hardcoded "ok", which reported a stray file under content/ and tolerated it in
# the same breath. Adding a documentation file here is a deliberate act and
# updating this tuple is the record of it. That row is scoped to definition
# directories for the same reason the count is: a README beside a schema is the
# schema directory's business, not a stray file under a catalog.
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
        # constants block. It is not an eleventh enemy: it carries the id
        # ELT-01, which does not match this row's ^EN-\d{2}$ item selector, so
        # the id_regex selector buckets it as the directory's aggregate. What
        # makes it the aggregate is that its ID is not an item ID - NOT the
        # absence of an id. It used to have none, and this comment used to say
        # "it has no id and no name_key (see ID_NULL_EXPECTED)"; the integration
        # owner has since minted ELT-01 and ID_NULL_EXPECTED is now empty, so
        # only the name_key half of that was still true. It does still omit
        # name_key, as a constants block with no player-facing name, and is
        # listed in NAME_KEY_OMITTED for that. The former
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


def in_non_definition_dir(path: Path) -> bool:
    """True when path sits beneath one of the NON_DEFINITION_DIRS.

    The single place that decides whether a path under content/ belongs to the
    definition population. It exists because that decision was previously made
    inline here and NOT made at all in check_file_inventory(), which enumerated
    content/ bare and so counted a non-definition directory toward the definition
    total. Every enumeration that means "the definitions" goes through this.
    """
    parts = path.relative_to(CONTENT).parts
    return bool(parts) and parts[0] in NON_DEFINITION_DIRS


def load_definitions() -> dict[Path, object]:
    """All *.json under content/ except the non-definition directories."""
    docs: dict[Path, object] = {}
    if not CONTENT.is_dir():
        fail(f"content/ directory not found at {CONTENT}")
        return docs
    for path in sorted(CONTENT.rglob("*.json")):
        if in_non_definition_dir(path):
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


MANIFEST_HEADER = """\
# Committed manifest of the content/ definition population: one line per
# definition file, "<path>\\t<id>", sorted by path.
#
# WHAT THIS IS FOR. Three edits used to be invisible to every assertion in
# verify_content.py: renaming a definition file inside its own directory,
# editing the id inside one, and swapping two ids between files. The inventory
# assertion compared a COUNT, so a rename left the count unchanged and passed;
# the id assertions checked a REGEX, per-directory counts and uniqueness, all of
# which a plausible wrong id satisfies. Pairing the path with the id closes both
# halves: the path set catches the rename and the id beside it catches the edit.
#
# WHAT THIS IS NOT. This manifest is an EDIT TAX, not evidence. It records what
# the tree currently says, not what any document says it should say. Someone who
# renames a file or changes an id and then regenerates this manifest passes the
# check, and nothing here contradicts them - the manifest agrees with the tree
# again by construction. Its whole value is that the change becomes LOUD: it
# cannot happen without a diff to this file in the same commit, and that diff is
# what a reviewer reads. It does not establish that any path or id is correct.
# The authority for that is the design documents and the A12 per-directory rows
# that cite them.
#
# HOW TO REGENERATE. Run verify_content.py with MECHAMINER_GOLDEN_UPDATE=1. That
# rewrites this file AND STILL FAILS, deliberately: a regeneration can never be
# the thing that turns a run green. Review the diff, confirm the rename or the
# id change was intended, commit this file with it, then rerun without the
# switch. Do not hand-edit - the generator is the only writer.
#
# Two placeholders can appear in the id column and neither is a real id:
# <no-id> for a definition with no id (A5/A6 also fail) and <unparseable> for
# one that did not parse (A1 also fails).
"""


def definition_paths() -> list[Path]:
    """The definition population, as paths, in sorted order.

    The single enumeration behind both A21 rows and the A28 manifest, scoped by
    in_non_definition_dir() - the same rule load_definitions() uses. There is
    deliberately no second notion of what counts as a definition file.
    """
    return sorted(p for p in CONTENT.rglob("*.json") if not in_non_definition_dir(p))


def manifest_id(path: Path, docs: dict[Path, object]) -> str:
    """The id to record for path, or a placeholder saying why there is none."""
    if path not in docs:
        return MANIFEST_ID_UNPARSEABLE
    doc = docs[path]
    if not isinstance(doc, dict):
        return MANIFEST_ID_UNPARSEABLE
    value = doc.get("id")
    if isinstance(value, str) and value:
        return value
    return MANIFEST_ID_ABSENT


def actual_manifest(docs: dict[Path, object]) -> list[tuple[str, str]]:
    """The (path, id) pairs the tree currently holds, sorted by path."""
    return [(rel(p), manifest_id(p, docs)) for p in definition_paths()]


def render_manifest(pairs: list[tuple[str, str]]) -> str:
    """The manifest's canonical text: header, then one tab-separated pair a line."""
    lines = [f"{path}\t{identifier}" for path, identifier in sorted(pairs)]
    return MANIFEST_HEADER + "\n".join(lines) + "\n"


def parse_manifest(text: str) -> tuple[list[tuple[str, str]], list[str]]:
    """Parse manifest text into sorted pairs, plus a list of malformed lines."""
    pairs: list[tuple[str, str]] = []
    malformed: list[str] = []
    for number, raw in enumerate(text.splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        fields = raw.split("\t")
        if len(fields) != 2 or not fields[0].strip() or not fields[1].strip():
            malformed.append(f"line {number}: {raw!r}")
            continue
        pairs.append((fields[0].strip(), fields[1].strip()))
    return sorted(pairs), malformed


def write_manifest(pairs: list[tuple[str, str]]) -> bool:
    """Write the manifest as UTF-8 with LF endings. True when it was written.

    write_bytes, not write_text: Path.write_text opens in text mode with
    newline=None, which translates every "\\n" to os.linesep - so the generator
    would emit CRLF on Windows and LF elsewhere, and a byte comparison against
    its own output could never converge there. The manifest's bytes are the
    thing being asserted, so the writer pins them.
    """
    try:
        CONTENT_DEFINITION_MANIFEST.write_bytes(render_manifest(pairs).encode("utf-8"))
        return True
    except OSError as exc:
        fail(
            f"A28 could not write {rel(CONTENT_DEFINITION_MANIFEST)}: {exc}. The manifest path "
            f"must be a writable regular file."
        )
        return False


def _byte_difference(committed: bytes, expected: bytes) -> str:
    """Name the kind of byte difference, so the failure is a diagnosis.

    A byte comparison that only said "differs" would be a worse gate than the
    newline-normalising one it replaced: line endings are invisible in a terminal,
    and so is a trailing space. This says which it is.
    """
    crlf = b"\r\n"
    cr = b"\r"
    notes: list[str] = []
    if crlf in committed:
        notes.append(f"the committed file has {committed.count(crlf)} CRLF line ending(s)")
    elif cr in committed:
        notes.append(f"the committed file has {committed.count(cr)} lone CR(s)")
    if committed.replace(crlf, b"\n").replace(cr, b"\n") == expected:
        notes.append("line endings are the ONLY difference")
    elif sorted(committed.split(b"\n")) == sorted(expected.split(b"\n")):
        notes.append("the same lines are present but their ORDER differs")
    else:
        stripped = b"\n".join(line.rstrip() for line in committed.split(b"\n"))
        if stripped == expected:
            notes.append("trailing whitespace is the ONLY difference")
        for offset, (a, b) in enumerate(zip(committed, expected)):
            if a != b:
                notes.append(
                    f"first difference at byte {offset}: committed {bytes([a])!r} vs expected "
                    f"{bytes([b])!r}"
                )
                break
        else:
            notes.append(
                f"one is a prefix of the other: committed {len(committed)} byte(s) vs expected "
                f"{len(expected)}"
            )
    return ("; ".join(notes) + ".") if notes else ""


def check_definition_manifest(docs: dict[Path, object]) -> tuple[list[tuple], int | None]:
    """A28 - the tree's (path, id) pairs equal the committed manifest, both ways.

    Returns the rows and the manifest's length, which A21's count row uses as its
    expectation so that the size and the membership record cannot disagree.

    The comparison is deliberately three-sided rather than a set equality, so the
    failure text names the edit that happened: a path only in the tree is an
    added or renamed-to file, a path only in the manifest is a deleted or
    renamed-from file, and a path in both with a different id is an edited id.
    A rename inside one directory produces one of each of the first two, which is
    exactly the shape the old count-only row could not see.
    """
    actual_pairs = actual_manifest(docs)
    actual_by_path = dict(actual_pairs)
    update_requested = os.environ.get(GOLDEN_UPDATE_VARIABLE) == "1"

    if not CONTENT_DEFINITION_MANIFEST.is_file():
        # is_file() is False for a directory or a special file at this path too, not
        # only for an absent one. write_manifest() reports the OSError in that case
        # rather than raising, so absurd input fails with a diagnosis instead of a
        # traceback, and this message does not claim a write that did not happen.
        existed = CONTENT_DEFINITION_MANIFEST.exists()
        if write_manifest(actual_pairs):
            fail(
                f"A28 manifest {rel(CONTENT_DEFINITION_MANIFEST)} did not exist and has been "
                f"written with the {len(actual_pairs)} (path, id) pair(s) found in the tree. It "
                f"records what the tree says, not what the documents say - review it against the "
                f"A12 rows and the design documents before committing it, then rerun. This check "
                f"fails on a freshly written manifest on purpose."
            )
        return (
            [
                (
                    "committed (path, id) manifest",
                    f"{rel(CONTENT_DEFINITION_MANIFEST)} present, a regular file",
                    "exists but is not a regular file" if existed else "absent",
                    "FAIL",
                )
            ],
            None,
        )

    # read_bytes, not read_text: text mode applies universal newlines, so a
    # manifest rewritten with CRLF - all 168 lines of it - decoded to exactly the
    # generator's LF text and the comparison below passed while the row claimed
    # the bytes were identical. The bytes are read raw and decoded explicitly.
    try:
        committed_bytes = CONTENT_DEFINITION_MANIFEST.read_bytes()
    except OSError as exc:
        fail(
            f"A28 could not read {rel(CONTENT_DEFINITION_MANIFEST)}: {exc}. The manifest path "
            f"must be a readable regular file; a directory or a special file there is not a "
            f"manifest."
        )
        return (
            [
                (
                    "committed (path, id) manifest",
                    "readable regular file",
                    f"unreadable: {type(exc).__name__}",
                    "FAIL",
                )
            ],
            None,
        )
    try:
        committed_text = committed_bytes.decode("utf-8")
    except UnicodeDecodeError as exc:
        fail(
            f"A28 manifest {rel(CONTENT_DEFINITION_MANIFEST)} is not valid UTF-8: {exc}. "
            f"Regenerate it with {GOLDEN_UPDATE_VARIABLE}=1."
        )
        return (
            [
                (
                    "committed (path, id) manifest",
                    "valid UTF-8",
                    "undecodable",
                    "FAIL",
                )
            ],
            None,
        )
    manifest_pairs, malformed = parse_manifest(committed_text)
    manifest_by_path = dict(manifest_pairs)
    if malformed:
        fail(
            f"A28 manifest {rel(CONTENT_DEFINITION_MANIFEST)} has {len(malformed)} malformed "
            f"line(s); each data line must be '<path>\\t<id>': {malformed[:10]}"
        )
    if len(manifest_by_path) != len(manifest_pairs):
        counted: dict[str, int] = {}
        for path, _ in manifest_pairs:
            counted[path] = counted.get(path, 0) + 1
        duplicates = sorted(path for path, n in counted.items() if n > 1)
        fail(
            f"A28 manifest {rel(CONTENT_DEFINITION_MANIFEST)} lists a path more than once, so "
            f"one of its rows is unreachable: {len(manifest_pairs)} row(s) over "
            f"{len(manifest_by_path)} distinct path(s). Regenerate it. Paths: {duplicates[:10]}"
        )

    only_in_tree = sorted(set(actual_by_path) - set(manifest_by_path))
    only_in_manifest = sorted(set(manifest_by_path) - set(actual_by_path))
    changed_ids = sorted(
        (path, manifest_by_path[path], actual_by_path[path])
        for path in set(actual_by_path) & set(manifest_by_path)
        if manifest_by_path[path] != actual_by_path[path]
    )
    # The committed BYTES must equal what the generator produces, the way a
    # golden does. Pair equality alone would leave the header and the line format
    # uncompared, so a stale header - including this file's own statement of what
    # it does and does not prove - could sit there indefinitely, and
    # GOLDEN_UPDATE_VARIABLE would refuse to rewrite it on the grounds that the
    # pairs already agree.
    #
    # This row is also the ONLY guard for two edits the three pair rows cannot
    # see, because those are set and dict comparisons: the data lines REORDERED
    # while holding the same pairs, and whitespace PADDING before a tab. Both
    # survive a newline-normalising comparison, so the fix for the CRLF escape was
    # to compare more strictly rather than to relabel the row - a byte comparison
    # keeps both of those guards and adds line-ending drift on top.
    rendered_bytes = render_manifest(actual_pairs).encode("utf-8")
    bytes_match = committed_bytes == rendered_bytes
    pairs_match = not (only_in_tree or only_in_manifest or changed_ids or malformed)
    matches = bytes_match and pairs_match

    rows = [
        (
            "definition paths in the tree but not in the manifest",
            0,
            f"{len(only_in_tree)}{': ' + ', '.join(only_in_tree[:5]) if only_in_tree else ''}",
            "ok" if not only_in_tree else "FAIL",
        ),
        (
            "definition paths in the manifest but not in the tree",
            0,
            f"{len(only_in_manifest)}"
            f"{': ' + ', '.join(only_in_manifest[:5]) if only_in_manifest else ''}",
            "ok" if not only_in_manifest else "FAIL",
        ),
        (
            "paths whose id differs from the manifest",
            0,
            f"{len(changed_ids)}"
            + (
                ": " + ", ".join(f"{p} {was} -> {now}" for p, was, now in changed_ids[:5])
                if changed_ids
                else ""
            ),
            "ok" if not changed_ids else "FAIL",
        ),
        (
            "committed bytes byte-for-byte equal the generator's output "
            "(header, line order, padding, LF endings)",
            "identical",
            "identical" if bytes_match else "differs",
            "ok" if bytes_match else "FAIL",
        ),
        (
            "(path, id) pairs recorded [an edit tax, not evidence the ids are right]",
            len(manifest_by_path),
            len(actual_by_path),
            "ok" if matches else "FAIL",
        ),
    ]

    if only_in_tree:
        fail(
            f"A28 {len(only_in_tree)} definition file(s) are in the tree but not in "
            f"{rel(CONTENT_DEFINITION_MANIFEST)}: {only_in_tree[:10]}. A file was added, or "
            f"renamed to this name. If it was renamed, the matching 'in the manifest but not "
            f"in the tree' row names the old name - a rename inside one directory shows as both, "
            f"and used to show as nothing at all because the inventory compared only a count."
        )
    if only_in_manifest:
        fail(
            f"A28 {len(only_in_manifest)} definition file(s) are in "
            f"{rel(CONTENT_DEFINITION_MANIFEST)} but not in the tree: {only_in_manifest[:10]}. "
            f"A file was deleted, or renamed away from this name."
        )
    if changed_ids:
        fail(
            f"A28 {len(changed_ids)} definition file(s) carry an id that differs from "
            f"{rel(CONTENT_DEFINITION_MANIFEST)}: "
            f"{[f'{p}: {was} -> {now}' for p, was, now in changed_ids[:10]]}. Nothing else here "
            f"sees this: the A12 selectors match an id PATTERN, the per-directory count is "
            f"unchanged and uniqueness still holds, so a wrong-but-plausible id passed every "
            f"other assertion. If the new id is intended, regenerate the manifest with "
            f"{GOLDEN_UPDATE_VARIABLE}=1 and commit the diff."
        )
    if pairs_match and not bytes_match:
        fail(
            f"A28 {rel(CONTENT_DEFINITION_MANIFEST)} records the right (path, id) pairs but its "
            f"bytes are not what the generator produces - the header, the line ORDER, whitespace "
            f"padding or the line endings have drifted, or the file was hand-edited. This row is "
            f"the only one that sees any of those: the three rows above compare sets and a "
            f"mapping, so a reordered or padded manifest holds the same pairs. "
            f"{_byte_difference(committed_bytes, rendered_bytes)} Regenerate it with "
            f"{GOLDEN_UPDATE_VARIABLE}=1 and commit the result."
        )

    if update_requested:
        if matches:
            fail(
                f"{GOLDEN_UPDATE_VARIABLE} was set but {rel(CONTENT_DEFINITION_MANIFEST)} "
                f"already matches the tree. Unset it: regenerating the manifest is a deliberate "
                f"act with a reviewed rename or id change behind it, not a routine step."
            )
        elif write_manifest(actual_pairs):
            fail(
                f"{rel(CONTENT_DEFINITION_MANIFEST)} has been rewritten because "
                f"{GOLDEN_UPDATE_VARIABLE}=1, and this check STILL FAILS on purpose - a "
                f"regeneration may never be what turns a run green. Review the diff above "
                f"against the A12 rows and the design documents, confirm the change was "
                f"intended, commit the new manifest with it, then rerun without "
                f"{GOLDEN_UPDATE_VARIABLE}. Regenerating makes the check agree with the tree "
                f"again; it does not make the tree right."
            )

    return rows, len(manifest_by_path)


def check_file_inventory(manifest_size: int | None) -> list[tuple]:
    """A21 - the definition *.json inventory under content/.

    Both rows are scoped to the definition population by in_non_definition_dir(),
    the same rule load_definitions() uses, via definition_paths(). Neither may go
    back to enumerating content/ bare: NON_DEFINITION_DIRS names directories this
    script has already decided are not catalogs of definitions, and a count that
    ignores that decision charges the next branch to author content/schemas/ with
    a definition it never added.

    The count's expectation is manifest_size - len() of the A28 manifest - not a
    literal. Two literals asserted to be the same number is a defect, and the
    manifest is the one that says WHICH files those are. When the manifest is
    missing A28 has already failed, so the row reports that rather than comparing
    against a number it does not have.
    """
    definitions = definition_paths()
    other_files = sorted(
        p
        for p in CONTENT.rglob("*")
        if p.is_file() and p.suffix != ".json" and not in_non_definition_dir(p)
    )
    actual = len(definitions)
    count_ok = manifest_size is not None and actual == manifest_size
    rows = [
        (
            "definition *.json files under content/ (excluding "
            f"{', '.join(NON_DEFINITION_DIRS)})",
            "n/a - A28 manifest missing"
            if manifest_size is None
            else f"{manifest_size} (= len(A28 manifest))",
            actual,
            "ok" if count_ok else "FAIL",
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
    if manifest_size is not None and actual != manifest_size:
        fail(
            f"content/ holds {actual} definition *.json file(s), expected {manifest_size} - the "
            f"number of pairs in {rel(CONTENT_DEFINITION_MANIFEST)}. A definition was added or "
            f"removed without regenerating the manifest and updating the matching A12 row, or a "
            f"file is in a directory no A12 row covers. A28 names the files; this row only counts "
            f"them. Files under content/{{{','.join(NON_DEFINITION_DIRS)}}}/ are NOT in this count "
            f"and cannot cause this failure."
        )
    return rows


# --------------------------------------------------------------------------
# A32 - canonical_letter on exactly the six letter resources.
#
# 40:106 (blob 4cded84) lists "canonical letter" among the resource definition
# fields. It says that about the six-material set; it does not say it about
# common ore or Hyper Gold, which are the ordinary-crafting and cross-run
# currencies and have no letter to carry. Their omission is therefore authored
# content, not an oversight, and row 4 asserts it as such.
#
# NOTHING HERE READS OR ASSERTS AN id VALUE except by comparing a file's
# canonical_letter against its own id (row 2). The RSC- prefixed ID grammar is
# not on this ref and no row of this check anticipates it: if the ids later
# become RSC-A..RSC-F, row 2 is the row that will need re-stating, deliberately,
# and rows 1/3/4/5 are unaffected.
# --------------------------------------------------------------------------

RESOURCES_DIR = CONTENT / "resources"
CANONICAL_LETTER_KEY = "canonical_letter"
CANONICAL_LETTERS = ("A", "B", "C", "D", "E", "F")
CANONICAL_LETTER_CARRIERS = tuple(f"{letter}.json" for letter in CANONICAL_LETTERS)
# The two currencies, which must NOT carry the key in any form.
CANONICAL_LETTER_OMITTERS = ("common-ore.json", "hyper-gold.json")
RESOURCE_DEFINITION_COUNT = 8


def check_canonical_letters(docs: dict[Path, object]) -> list[tuple]:
    """A32 - canonical_letter is on exactly A-F and equals each file's own id."""
    paths = sorted(RESOURCES_DIR.glob("*.json")) if RESOURCES_DIR.is_dir() else []
    carried: dict[str, object] = {}
    ids: dict[str, object] = {}
    for path in paths:
        doc = docs.get(path)
        if not isinstance(doc, dict):
            continue
        ids[path.name] = doc.get("id")
        if CANONICAL_LETTER_KEY in doc:
            carried[path.name] = doc[CANONICAL_LETTER_KEY]

    # ---- row 1: the carrier set, NAMED. A count is blind to a correlated swap.
    carriers = sorted(carried)
    expected_carriers = sorted(CANONICAL_LETTER_CARRIERS)
    row1_ok = carriers == expected_carriers

    # ---- row 2: each of the six equals its OWN id. `agreed` is collected for
    # the PASSING display only: a green run used to print "6 agree, 0 disagree"
    # and name the files solely on failure, so the reader auditing a passing run
    # met a count and had to take the comparand set on trust. The predicate is
    # unchanged - `mismatches` alone still decides the status.
    mismatches: list[str] = []
    agreed: list[str] = []
    for name in CANONICAL_LETTER_CARRIERS:
        if name not in ids:
            mismatches.append(f"{name}: not a parsed definition")
        elif name not in carried:
            mismatches.append(f"{name}: {CANONICAL_LETTER_KEY} is absent, id is {ids[name]!r}")
        elif carried[name] != ids[name]:
            mismatches.append(
                f"{name}: {CANONICAL_LETTER_KEY} is {carried[name]!r}, id is {ids[name]!r}"
            )
        else:
            agreed.append(f"{name}={carried[name]!r}")

    # ---- row 3: six distinct letters covering exactly {A..F}. repr() so a
    # non-string value (null, a number, an object) cannot raise in sorted().
    values = [carried[name] for name in CANONICAL_LETTER_CARRIERS if name in carried]
    distinct = {repr(v) for v in values}
    row3_ok = (
        len(values) == len(CANONICAL_LETTERS)
        and len(distinct) == len(CANONICAL_LETTERS)
        and {v for v in values if isinstance(v, str)} == set(CANONICAL_LETTERS)
    )

    # ---- row 4: the currencies do not carry the KEY. Not "is not null" - see
    # the block comment above and the A32 docstring entry for why A26 is not
    # enough here.
    offenders = [name for name in CANONICAL_LETTER_OMITTERS if name in carried]

    # ---- row 5: the population itself, so rows 1-4 cannot pass on a tree that
    # has grown a ninth resource.
    row5_ok = len(paths) == RESOURCE_DEFINITION_COUNT

    rows = [
        (
            "row 1: files carrying canonical_letter are exactly A-F (named, not counted)",
            ", ".join(expected_carriers),
            ", ".join(carriers) or "none",
            "ok" if row1_ok else "FAIL",
        ),
        (
            "row 2: canonical_letter == that file's own id, in each of the six",
            "6 agree, named",
            (
                f"{len(agreed)} agree: " + ", ".join(agreed)
                if not mismatches
                else f"{len(agreed)} agree, {len(mismatches)} disagree: "
                + "; ".join(mismatches)
            ),
            "ok" if not mismatches else "FAIL",
        ),
        (
            "row 3: the six values are distinct and cover exactly {A,B,C,D,E,F}",
            "6 distinct: " + ", ".join(CANONICAL_LETTERS),
            f"{len(distinct)} distinct: " + (", ".join(sorted(distinct)) or "none"),
            "ok" if row3_ok else "FAIL",
        ),
        (
            "row 4: the two currencies do not carry the KEY AT ALL (absence, not null)",
            "0 of " + ", ".join(CANONICAL_LETTER_OMITTERS),
            f"{len(offenders)} carry it" + (f": {', '.join(offenders)}" if offenders else ""),
            "ok" if not offenders else "FAIL",
        ),
        (
            "row 5: content/resources/ holds exactly 8 definition files",
            RESOURCE_DEFINITION_COUNT,
            len(paths),
            "ok" if row5_ok else "FAIL",
        ),
    ]

    if not row1_ok:
        fail(
            f"A32 row 1: the files under content/resources/ carrying "
            f"{CANONICAL_LETTER_KEY!r} are {carriers}, expected exactly {expected_carriers}. "
            f"40:106 (blob 4cded84) gives the canonical letter to the six-material set and to "
            f"nothing else. This row NAMES the carriers rather than counting them because a "
            f"count of 6 also passes when the key is deleted from one letter file and added to "
            f"a currency in the same edit."
        )
    if mismatches:
        fail(
            f"A32 row 2: {len(mismatches)} resource(s) whose {CANONICAL_LETTER_KEY} is not that "
            f"file's own id: {mismatches}. The letter IS the identity of a specialized material, "
            f"so the two must agree in the same file. This row is the only one that catches two "
            f"letter files SWAPPING values: the set of values is unchanged by a swap, so rows 1 "
            f"and 3 both still pass."
        )
    if not row3_ok:
        fail(
            f"A32 row 3: the {len(values)} {CANONICAL_LETTER_KEY} value(s) present are "
            f"{sorted(distinct)}, expected 6 distinct letters covering exactly "
            f"{list(CANONICAL_LETTERS)}. Two materials cannot share a letter and no letter of "
            f"the accepted set may go unassigned (40:106, blob 4cded84)."
        )
    if offenders:
        fail(
            f"A32 row 4: {offenders} carry the key {CANONICAL_LETTER_KEY!r}. common ore and "
            f"Hyper Gold are the ordinary-crafting and cross-run currencies; 40:106 gives the "
            f"canonical letter to the six-material set only, so the right way to spell 'has no "
            f"letter' is to OMIT the key (40:90 materializes the default for an absent optional "
            f"field). A26 already rejects the null spelling repo-wide, but a non-null wrong "
            f"value - \"\" or \"common-ore\" - passes A26 and still asserts that a currency has a "
            f"canonical letter, which is what this row exists to catch."
        )
    if not row5_ok:
        fail(
            f"A32 row 5: content/resources/ holds {len(paths)} *.json file(s), expected "
            f"{RESOURCE_DEFINITION_COUNT} (6 specialized + common ore + Hyper Gold, the same "
            f"population the A12 resources row asserts). Rows 1-4 of A32 are all satisfied by a "
            f"tree that has grown a ninth resource, so the population is asserted beside them. A "
            f"new resource is added deliberately, with its A12 row, its A28 manifest line and a "
            f"decision recorded about whether it carries a canonical letter."
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
# A29 / A31 - the six derived-value families the compiler owns.
#
# The rules live in expected_derived_value_removals.json, which was committed in
# its own commit BEFORE any content/ file changed. Reading them from there rather
# than restating them here is deliberate: a rule restated in two places can drift,
# and the whole point of the prediction-first ordering is that the assertion and
# the prediction are the same artifact.
# --------------------------------------------------------------------------

DERIVED_EXPECTATION = Path(__file__).resolve().parent / "expected_derived_value_removals.json"

# The two counts the generator declares, restated here ON PURPOSE. This is the
# one place in the A29/A31 code where duplication is right: everything else is
# read from the expectation file so the assertion and the prediction cannot drift,
# but a count read only from the file it is meant to police cannot police it. An
# empty `families` list, a family with no records, and the counts overwritten with
# 9999/99 all passed verify_content.py before these existed - only
# `derive --check` caught them, by regenerating and byte-comparing, which is a
# file-integrity check and not an assertion inside the rule.
DECLARED_FAMILY_COUNT = 6
DECLARED_TOTAL_REMOVED = 115

# How many removed values sit in a container the removal left with no numeric
# leaves at all, so that A31's value layer searches nothing for them. Asserted,
# not remarked on: emptying a container empties its guard, and the number must not
# grow without someone deciding that it may. The 13 are the `{}` and
# `{"resource": "common ore"}` residues under content/resources/,
# content/mining-sites/ - NOT the six root-level records, whose site is the
# document itself.
EMPTY_SITE_GUARD_RECORDS = 13

# The three search-radius figures A31 PRINTS on a green run, declared here so that
# the tool which prints them also asserts them. They are computed by
# derive_derived_value_expectations.py on the pinned sweep ref and byte-checked by
# `derive --check`; that is a different tool, which nobody running verify_content.py
# has to run. Measured before this existed: editing file_radius_pairs back to the old
# unreproducible 55 made A31 print "1 : 55 : 668" at exit 0 with 0 failures. A figure
# a reader meets on a passing run and no assertion binds is indistinguishable from a
# figure that was made up, which is precisely what 55 and 400 were.
# (site, file, scope), as measured on the expectation's sweep_ref.
SEARCH_RADIUS_DECLARED = (1, 40, 668)

# The scope-file population A31's NAME layer walks, summed over the six families'
# scopes. Pinned here so a scope list that is emptied or misspelled cannot quietly
# shrink the walk to nothing while the six family rows keep printing `0 / ok` - a
# rule that searched no file finds no hit, and "0 hits" is the same string either
# way. It is 54 = enemies 11 + bosses 4 (family 1) + powerups 13 + utilities 13 +
# resources 8 + mining-sites 4 + maps 1, the same per-directory populations A12
# asserts; families overlap no directory, so the sum is also the distinct count.
#
# ONLY the file population is pinned. The LEAF count is asserted non-zero and
# printed as measured, never pinned: a leaf count moves with any authored value
# edit anywhere in seven directories, so pinning it would turn every ordinary
# content change into a failure here, while a file population moves only when a
# definition file is added or removed - which is deliberate, and which A12 and
# A28 already require a decision for.
DERIVED_NAME_WALK_SCOPE_FILES = 54


def load_derived_expectation() -> dict:
    if not DERIVED_EXPECTATION.exists():
        fail(
            f"A29/A31: {rel(DERIVED_EXPECTATION)} is missing. It is the committed prediction of "
            f"exactly which derived values this tree no longer authors, and both rules read their "
            f"scopes and patterns from it. Regenerate it with "
            f"derive_derived_value_expectations.py."
        )
        return {}
    return json.loads(DERIVED_EXPECTATION.read_text())


def check_derived_expectation_counts(docs: dict[Path, object]) -> list[tuple]:
    """A29/A31 vacuity guards - the four declared counts, asserted HERE.

    `total_removed`, `family_count`, `declared_family_count` and
    `declared_total_removed` were written by the generator and read by nothing in
    this file: `grep -n declared verify_content.py` returned no hit anywhere in the
    A29/A31 code. verify_content.py on its own therefore passed every vacuity
    injection - an empty `families` list, a scope pointed at a directory holding no
    definitions, every family's `records` emptied, an empty
    `removed_numeric_multiset` with `sweep_ref` repinned to HEAD, and the counts
    overwritten with 9999/99. Only `derive --check` caught those, and it caught
    them by regenerating the file and byte-comparing it: a file-integrity check,
    which fails for any edit at all and says nothing about whether the RULE has
    content. These rows are the rule saying it.

    The scope row is deliberately measured against the CURRENT tree rather than
    against the sweep ref, because a scope that resolves to nothing here is a rule
    searching nothing here, whatever it searched when the prediction was written.
    """
    expectation = load_derived_expectation()
    if not expectation:
        return []
    families = expectation.get("families") or []
    built_records = sum(len(f.get("records") or []) for f in families)
    multiset = expectation.get("removed_numeric_multiset") or []

    empty_families = sorted(f.get("name", "?") for f in families if not (f.get("records") or []))

    # The three figures A31 prints, plus the prose half of the same measurement. The
    # numbers and the sentence are two writers on one measurement, so an edit to
    # either alone has to fail: the row above pins the numbers, this pins the sentence
    # to them.
    measured_radii = expectation.get("search_radius_measurement") or {}
    site, file_r, scope_r = (measured_radii.get(k) for k in
                             ("site_radius_pairs", "file_radius_pairs", "scope_radius_pairs"))
    conclusion = measured_radii.get("conclusion") or ""
    if not all(isinstance(n, int) for n in (site, file_r, scope_r)):
        radius_conclusion_state = "radii missing or not integers"
    else:
        wanted = (
            f"Ratio {site} : {file_r} : {scope_r}",
            f"file would need {file_r - site} more",
            f"scope {scope_r - site} more",
        )
        absent = [w for w in wanted if w not in conclusion]
        radius_conclusion_state = "consistent" if not absent else f"text disagrees: {absent}"

    empty_scopes: list[str] = []
    for family in families:
        for scope in family.get("scopes") or []:
            directory = scope.strip("/").split("/", 1)[1] if "/" in scope.strip("/") else ""
            in_scope = files_in(directory, docs) if directory else {}
            leaves = sum(1 for path in in_scope for _ in numeric_pointer_leaves(in_scope[path]))
            if not in_scope or not leaves:
                empty_scopes.append(f"{family.get('name', '?')} -> {scope}")

    checks = [
        (
            "families in the expectation file",
            DECLARED_FAMILY_COUNT,
            len(families),
            len(families) == DECLARED_FAMILY_COUNT,
        ),
        (
            "family_count as written by the generator",
            DECLARED_FAMILY_COUNT,
            expectation.get("family_count"),
            expectation.get("family_count") == DECLARED_FAMILY_COUNT,
        ),
        (
            "declared_family_count as written by the generator",
            DECLARED_FAMILY_COUNT,
            expectation.get("declared_family_count"),
            expectation.get("declared_family_count") == DECLARED_FAMILY_COUNT,
        ),
        (
            "records summed over every family",
            DECLARED_TOTAL_REMOVED,
            built_records,
            built_records == DECLARED_TOTAL_REMOVED,
        ),
        (
            "total_removed as written by the generator",
            DECLARED_TOTAL_REMOVED,
            expectation.get("total_removed"),
            expectation.get("total_removed") == DECLARED_TOTAL_REMOVED,
        ),
        (
            "declared_total_removed as written by the generator",
            DECLARED_TOTAL_REMOVED,
            expectation.get("declared_total_removed"),
            expectation.get("declared_total_removed") == DECLARED_TOTAL_REMOVED,
        ),
        (
            "elements in removed_numeric_multiset (A29's comparand)",
            DECLARED_TOTAL_REMOVED,
            len(multiset),
            len(multiset) == DECLARED_TOTAL_REMOVED,
        ),
        (
            "families whose records list is EMPTY (a rule over no values passes free)",
            0,
            len(empty_families),
            not empty_families,
        ),
        (
            "family scopes holding no numeric leaves in the CURRENT tree",
            0,
            len(empty_scopes),
            not empty_scopes,
        ),
        (
            "search radii A31 PRINTS (site : file : scope), asserted by the tool that prints them",
            " : ".join(str(n) for n in SEARCH_RADIUS_DECLARED),
            " : ".join(str(measured_radii.get(k, "?"))
                       for k in ("site_radius_pairs", "file_radius_pairs", "scope_radius_pairs")),
            tuple(measured_radii.get(k) for k in
                  ("site_radius_pairs", "file_radius_pairs", "scope_radius_pairs"))
            == SEARCH_RADIUS_DECLARED,
        ),
        (
            "the printed radii and the conclusion sentence's own arithmetic agree",
            f"file-site={SEARCH_RADIUS_DECLARED[1] - SEARCH_RADIUS_DECLARED[0]}, "
            f"scope-site={SEARCH_RADIUS_DECLARED[2] - SEARCH_RADIUS_DECLARED[0]} in the text",
            radius_conclusion_state,
            radius_conclusion_state == "consistent",
        ),
        (
            "the pair definition is present, so the printed figures print with what they counted",
            "present",
            "present" if (measured_radii.get("definition") or "").strip() else "MISSING",
            bool((measured_radii.get("definition") or "").strip()),
        ),
    ]
    rows = [(label, expected, actual, "ok" if good else "FAIL")
            for label, expected, actual, good in checks]
    bad = [f"{label}: expected {expected}, got {actual!r}"
           for label, expected, actual, good in checks if not good]
    if bad:
        fail(
            f"A29/A31 {len(bad)} declared count(s)/vacuity guard(s) in "
            f"{rel(DERIVED_EXPECTATION)} do not hold. These are the rows that stop the rule from "
            f"passing over nothing, so a mismatch is a failure of the rule and not only of the "
            f"file: {bad}"
            + (f" empty families={empty_families}" if empty_families else "")
            + (f" empty scopes={empty_scopes}" if empty_scopes else "")
        )
    return rows


def pointer_segments(pointer: str) -> list[str]:
    return [s for s in re.split(r"\.|\[\d+\]", pointer) if s]


def derived_rule_matches(family: dict, pointer: str) -> bool:
    """A31's matcher: a family's rule against a pointer's SEGMENT NAMES.

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
    """A31 - none of the six removed families may reappear.

    Two layers. This is the NAME layer, over pointer segment names; it catches a
    rename only within its own word class and cannot make one impossible. The
    VALUE layer - no non-operand leaf inside a derivation site may carry the
    derived value - is asserted by the generator against the pinned sweep ref and
    recorded in the expectation file, because it is a property of the removal set
    rather than of the current tree.

    Every row carries the DENOMINATOR of its own search - the numeric leaves the
    walk visited and the files it scanned - counted by the walk itself as it runs,
    never restated from a hand-run figure. Without it these six rows printed
    `0 / 0 / ok` and were the only assertion in this file that reported a result
    with no measure of what it looked at (A26 prints files scanned, A27 files and
    tokens, A29 `115 of 115`, A30 comparisons made, A31's own VALUE layer
    `102 of 115` plus a distribution). A row reading `0` over 349 leaves and a row
    reading `0` over no leaves at all are the same six characters on the page.
    """
    expectation = load_derived_expectation()
    rows = []
    walked_leaves = 0
    walked_files = 0
    for family in expectation.get("families", []):
        hits: list[str] = []
        family_leaves = 0
        family_files = 0
        for scope in family["scopes"]:
            directory = scope.strip("/").split("/", 1)[1]
            for path, doc in sorted(files_in(directory, docs).items()):
                family_files += 1
                for pointer, value in numeric_pointer_leaves(doc):
                    family_leaves += 1
                    if derived_rule_matches(family, pointer):
                        hits.append(f"{rel(path)}.{pointer} = {value!r}")
        walked_leaves += family_leaves
        walked_files += family_files
        rows.append(
            (
                f"no {family['name']} value in {' + '.join(family['scopes'])}",
                0,
                f"{len(hits)} of {family_leaves} numeric leaf/leaves "
                f"in {family_files} file(s)",
                "ok" if not hits else "FAIL",
            )
        )
        if hits:
            fail(
                f"A31 {len(hits)} field(s) under {' + '.join(family['scopes'])} hold a "
                f"'{family['name']}' value, which the compiler owns per "
                f"{family['doc_assignment'].split(' - ')[0]}. Matched on pointer segment names "
                f"/{family['pointer_segment_rule']}/"
                + (f" under /{family['pointer_parent_rule']}/" if family.get("pointer_parent_rule")
                   else "")
                + f". This is the NAME layer, which catches a rename only within its own word "
                f"class - a value reintroduced under a name the class does not carry passes it, and "
                f"the value layer is what covers that: {hits[:10]}"
            )

    # The walk asserted, not only printed. The six rows above cannot fail on an
    # empty walk: every one of them is `0 hits`, which is exactly what a rule
    # searching nothing reports. This row is what a scope-list regression hits.
    coverage_ok = walked_leaves > 0 and walked_files == DERIVED_NAME_WALK_SCOPE_FILES
    rows.append(
        (
            "the walk itself: numeric leaves visited (non-zero) and scope files scanned",
            f"non-zero leaves, {DERIVED_NAME_WALK_SCOPE_FILES} file(s)",
            f"{walked_leaves} leaf/leaves, {walked_files} file(s)",
            "ok" if coverage_ok else "FAIL",
        )
    )
    if not coverage_ok:
        fail(
            f"A31 NAME layer walked {walked_leaves} numeric leaf/leaves across {walked_files} "
            f"file(s), expected a non-zero leaf count over exactly "
            f"{DERIVED_NAME_WALK_SCOPE_FILES} file(s). The six family rows above CANNOT catch "
            f"this: each reports `0 hits`, and a rule that searched no file reports `0 hits` too, "
            f"so an emptied or misspelled `scopes` entry in "
            f"{rel(DERIVED_EXPECTATION)} collapses the walk while six rows keep printing `ok`. "
            f"The leaf count is asserted non-zero rather than pinned because it moves with any "
            f"authored value edit; the file count is pinned because it moves only when a "
            f"definition file is added or removed, which A12 already requires a decision for."
        )

    return rows, (
        "NOT CAUGHT by this layer: a derived value reintroduced under a name outside the family's "
        "word class, or in a directory the family does not scope. Probed per family with a "
        "semantic-neighbour name - caught 0 of 6. That figure is a HAND-RUN PROBE, not a "
        "measurement this run made: six injections, one per family, each reintroducing the family's "
        "value under a name outside its word class, and no assertion here recomputes it. This "
        "layer catches a rename only WITHIN its own word class. Layer 2 below is the one that "
        "does not depend on the name at all.",
    )


def derivation_site(pointer: str) -> str:
    """The pointer of the object that HELD the removed leaf.

    A trailing `[i]` means the container is the list, so the index is dropped;
    otherwise the last dotted segment is dropped. A ROOT-LEVEL leaf has neither,
    and the object that held it is the DOCUMENT ITSELF - returned as "" and read
    by is_in_site() as the whole document, NOT as "no site".

    That last case was a hole rather than a subtlety. An earlier revision returned
    "" here and then filtered with `if not (site and ...): continue`, so a falsy
    site skipped EVERY leaf: six root-level records searched nothing at all and
    could not fail on any injection, including the removed value reinjected into
    the same file. The six were content/resources/common-ore.json
    seam_total_per_map, content/resources/hyper-gold.json run_ceiling, and
    total_depletion_seconds plus total_uninterrupted_extraction_per_map_seconds on
    both content/mining-sites/*-ore-seams.json.

    Kept as a shared helper with the same name and body in the generator, because
    `derive --check` regenerates the expectation from the same definition and the
    two must not drift.
    """
    if pointer.endswith("]"):
        return pointer[: pointer.rindex("[")]
    return pointer.rsplit(".", 1)[0] if "." in pointer else ""


def is_in_site(leaf: str, site: str) -> bool:
    """Is `leaf` the site itself or inside its subtree? "" is the document root."""
    if site == "":
        return True
    return leaf == site or leaf.startswith(site + ".") or leaf.startswith(site + "[")


def check_derived_family_values(docs: dict[Path, object]) -> list[tuple]:
    """A31's VALUE layer, over the CURRENT tree - the half a name rule cannot do.

    For every value this pass removed, no non-operand numeric leaf inside its own
    derivation site may carry that value. Compared exactly, as Fractions, with no
    tolerance: a stored 32.0 and a stored 32 are the same number and both fail.

    This is what makes the guard indifferent to spelling. A reintroduction
    survives a rename, a relocation inside the site, a different unit suffix and a
    change of arity (32.0 -> [32.0]) without changing the number, and all four are
    caught here while all four defeat the name layer.

    TWO THINGS LIMIT ITS REACH AND BOTH PRINT ON A GREEN RUN.

    (1) THE RADIUS: the derivation site, not the file and not the scope. A value
        relocated OUT of its site still passes. The choice is measured, not
        assumed - the generator's measure_search_radii() counts what each
        candidate radius would flag under one definition and writes the three
        numbers into search_radius_measurement in the expectation file, which is
        where the ratio printed below comes from.

    (2) THE GUARD IS ONLY AS BIG AS WHAT SURVIVED IN THE SITE. Emptying a
        container empties its guard: where the removal left the containing object
        with no numeric leaves at all, this layer searches ZERO leaves for that
        record and cannot fail on anything short of a leaf reappearing inside that
        same object. The count of such records is asserted and printed, because a
        mean ("299 leaves across 115 values") reads as coverage of all 115 and is
        exactly what hid the empty-site bug above.
    """
    expectation = load_derived_expectation()
    exceptions = {
        (e["file"], e["derived_pointer"], e["colliding_pointer"])
        for e in expectation.get("value_collision_exceptions", [])
    }
    exceptions_used: set = set()
    by_path = {rel(p): d for p, d in docs.items()}
    rows = []
    per_record: list[int] = []
    for family in expectation.get("families", []):
        hits: list[str] = []
        for record in family["records"]:
            doc = by_path.get(record["file"])
            if doc is None:
                continue
            pointer = record["pointer"]
            site = derivation_site(pointer)
            own = {p.split("::", 1)[1] for p in record.get("operand_pointers", [])
                   if p.split("::", 1)[0] == record["file"]}
            target = Fraction(str(record["value"]))
            searched_here = 0
            for leaf, value in numeric_pointer_leaves(doc):
                if not is_in_site(leaf, site):
                    continue
                searched_here += 1
                if Fraction(str(value)) != target:
                    continue
                if leaf in own:
                    continue
                if (record["file"], pointer, leaf) in exceptions:
                    exceptions_used.add((record["file"], pointer, leaf))
                    continue
                hits.append(f"{record['file']}.{leaf} = {value!r} (the removed {pointer})")
            per_record.append(searched_here)
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
                f"A31 value layer: {len(hits)} numeric leaf/leaves carry a value this pass removed "
                f"as a '{family['name']}', inside that value's own derivation site and not as one "
                f"of its operands. Matched on the NUMBER, so a rename, a relocation within the "
                f"site, a new unit suffix and a scalar-to-list change all fail it. If a hit is a "
                f"genuine coincidence, add it to VALUE_COLLISION_EXCEPTIONS with a reason: "
                f"{sorted(hits)[:10]}"
            )

    # A STALE DECLARED EXCEPTION FAILS, HERE AND NOT ONLY IN THE GENERATOR. This
    # is A30's pattern - `set(CSV_MIRROR_ROUNDED) - declared_used`, computed
    # against the tree in front of it - and it is here because the sentence "a
    # stale exception now fails too" was false in BOTH tools: this file had no
    # staleness check at all and consulted the set only as an exclusion, while the
    # generator checked it against the pinned sweep ref, where the colliding value
    # collides by construction. Control: content/utilities/UTL-R1.json
    # acquisition.rank_count 0 -> 1 ends the only declared collision, and before
    # this row both tools still exited 0.
    stale = sorted(exceptions - exceptions_used)
    rows.append(
        (
            "declared value-collision exceptions that no longer collide (stale exceptions)",
            0,
            len(stale),
            "ok" if not stale else "FAIL",
        )
    )
    if stale:
        fail(
            f"A31 value layer: {len(stale)} declared value-collision exception(s) no longer suppress "
            f"anything on this tree, so the justification recorded for each is no longer true. A "
            f"stale exception silently widens the gate - delete it from "
            f"VALUE_COLLISION_EXCEPTIONS in the generator and regenerate: {stale}"
        )

    # A record whose containing object lost all its numeric leaves has an empty
    # guard, and the count of those is an assertion rather than a remark: if a
    # later pass empties more containers, this row moves and someone has to look.
    blind = sum(1 for n in per_record if n == 0)
    rows.append(
        (
            "removed values whose site still holds numeric leaves to search "
            "(the rest have an EMPTY guard)",
            f"{len(per_record) - EMPTY_SITE_GUARD_RECORDS} of {len(per_record)}",
            f"{len(per_record) - blind} of {len(per_record)}",
            "ok" if blind == EMPTY_SITE_GUARD_RECORDS else "FAIL",
        )
    )
    if blind != EMPTY_SITE_GUARD_RECORDS:
        fail(
            f"A31 value layer: {blind} removed value(s) have a derivation site holding no numeric "
            f"leaves, so this layer searches nothing for them; "
            f"EMPTY_SITE_GUARD_RECORDS declares {EMPTY_SITE_GUARD_RECORDS}. This is not a pass/fail "
            f"about the tree's correctness - it is the SIZE OF THE BLIND SPOT, and it is asserted so "
            f"that emptying another container cannot shrink the guard quietly."
        )

    radii = expectation.get("search_radius_measurement") or {}
    distribution = ", ".join(
        f"{n} leaf/leaves x{per_record.count(n)}" for n in sorted(set(per_record))
    )
    # WHAT THIS CHECK SEARCHED AND WHAT IT CANNOT SEE, on the same table a passing
    # run prints. A limitation recorded only in a docstring or a notes file is not
    # disclosed to the person reading a green run. Reported as a DISTRIBUTION, not
    # as a total or a mean: the previous line said "299 numeric leaves across 115
    # removed values", which is a mean of 2.6 that reads as coverage of all 115
    # and concealed 19 records searching nothing.
    return rows, (
        f"RADIUS SEARCHED: the object that held each removed leaf, plus its subtree. Per-record "
        f"distribution over {len(per_record)} removed values - {distribution} - so the MINIMUM is "
        f"{min(per_record) if per_record else 0} and {blind} record(s) search nothing at all. "
        f"{len(exceptions)} declared exception(s), each asserted to still collide. Values compare "
        f"exactly as Fractions; there is no tolerance and no rounding.",
        f"NOT CAUGHT (1) - RADIUS: a removed value relocated OUT of its derivation site, elsewhere "
        f"in the same file or anywhere else in the scope. Probed per family - caught 0 of 6. Rename, "
        f"unit suffix and arity change (32.0 -> [32.0]) are caught 6 of 6. BOTH of those are "
        f"HAND-RUN PROBES - twelve injections done by hand, six per row - and no assertion in this "
        f"run recomputes either; unlike the three radii below, which this tool asserts. Wider radii "
        f"were measured "
        f"under one definition on the pinned sweep ref and are recorded in "
        f"search_radius_measurement: {radii.get('site_radius_pairs', '?')} coincidental pair(s) at "
        f"site radius, {radii.get('file_radius_pairs', '?')} at file radius and "
        f"{radii.get('scope_radius_pairs', '?')} at scope radius. Widening needs that many "
        f"hand-written exceptions, which is what makes it unlandable rather than merely unchosen. "
        f"WHAT A PAIR IS, stated here rather than pointed at, because a reader meeting "
        f"'{radii.get('site_radius_pairs', '?')} : {radii.get('file_radius_pairs', '?')} : "
        f"{radii.get('scope_radius_pairs', '?')}' cannot otherwise tell what was counted: "
        f"{(radii.get('definition') or 'DEFINITION MISSING from the expectation file').strip()} "
        f"These three figures are ASSERTED by this tool against SEARCH_RADIUS_DECLARED "
        f"({' : '.join(str(n) for n in SEARCH_RADIUS_DECLARED)}) in the A29/A31 declared-counts "
        f"rows above, not merely printed from the expectation file; the file's own byte-integrity is "
        f"a separate check (derive_derived_value_expectations.py --check).",
        f"NOT CAUGHT (2) - EMPTY GUARDS: {blind} of {len(per_record)} removed values sit in a "
        f"container the removal left with NO numeric leaves ({{}} and {{'resource': ...}} residues), "
        f"so their guard searches nothing and only a leaf reappearing inside that same object could "
        f"fail it. Emptying a container empties its guard. The 6 root-level records are NOT in this "
        f"set any more - their site is the document, which is the object that held them.",
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
# gameplay table docs/40 section 'Analytical' compares against). It is worth having
# either way, and when the question lands, the loser becomes derived and this check
# becomes redundant in the good way rather than wrong.
#
# NO TOLERANCE, AND THAT SENTENCE IS NOW TRUE FOR THE DECLARED PAIRS TOO. Values
# compare exactly as Fractions. Where the CSV states a value at lower precision
# than the derivation produces, the pair must be DECLARED below - enumerated with
# a reason, never absorbed by a threshold.
#
# A declared entry names BOTH numbers: the CSV's written value and the EXACT
# content-side value it is allowed to stand for. Both must hold. An earlier
# revision required only that the content value ROUND to the CSV value at the
# CSV's written precision, which is a tolerance of half the last decimal place
# hiding inside a rule whose comment said "never absorbed by a threshold":
# EN-07's body_scale_multiplier could sit anywhere in [0.61875, 0.625) undetected,
# a band disclosed nowhere. It is now pinned to the single value 0.496, so any
# retune of the scale fails, and the rounding clause is kept as a second condition
# so a declaration cannot silently cover a pair that is not even close.
# --------------------------------------------------------------------------
PRESSURE_CSV = REPO_ROOT / "docs/data/contact-damage-pressure.csv"

# (actor, column) -> (EXACT content-side value this declaration covers, reason).
# The exact value is what removes the tolerance: nothing else passes, in either
# direction.
CSV_MIRROR_ROUNDED = {
    ("EN-07", "contact_diameter_m"): (
        "0.496",
        "OPEN QUESTION, NOT A SETTLED ONE, and it is with the design owner: which contact diameter "
        "was the Razorling meant to have? This entry records the question and the evidence on both "
        "sides. It does not decide it, and an earlier version of this text did - it asserted the "
        "0.496 was 'not a content defect, it is the exact product of the authored scale', which "
        "reads as settled and is not supported. "
        "THE DIVERGENCE: docs/31 section 'Ordinary roster overview' states the Razorling body scale "
        "as 0.62x, so 0.62 x 0.80 = 0.496 M exactly; docs/72 section 'Collision and Contact "
        "Footprints' states its footprint as 0.50 M and this CSV mirrors 72. EN-07 is the ONLY "
        "actor whose derived diameter misses the CSV; the other 13 are exact. "
        "THE ARGUMENT WITH FORCE, and it points at 0.50 being the exact one: every other ordinary "
        "body scale is a multiple of 0.05 (0.55, 1.00, 1.30, 1.05, 1.20, 1.00, 1.10, 1.65, 1.35). "
        "0.62 is not - but NEITHER IS 0.625. The Razorling breaks the pattern under either "
        "hypothesis, so the pattern alone decides nothing; what the hypotheses differ on is whether "
        "the break is MOTIVATED. Under 0.625 it is: a designer targeting a clean 0.50 M contact "
        "diameter back-computes 0.50 / 0.80 = 0.625 and the scale is whatever falls out. Under 0.62 "
        "it is not: someone working in 0.05 steps who wanted a small variant picks 0.60 or 0.65. A "
        "motivated exception is better evidence than an unmotivated one, so this is the strongest "
        "single argument on the table and it points at 0.625 - which is why the evidence LEANS that "
        "way rather than sitting balanced. What it does not do is settle the question; see the "
        "closing paragraphs. "
        "THREE ARGUMENTS THAT DO NOT DISCRIMINATE, recorded as non-discriminating so a later reader "
        "does not weigh them. (1) 'Both docs/72 figures come out exact under 0.625' is ONE "
        "coincidence, not two: start distance is diameter / 2 + 0.50, so once the diameter is "
        "exactly 0.50 the start distance is exactly 0.75 automatically. The second figure carries no "
        "independent information. (2) 'docs/31 prints every scale at two decimals' is "
        "ZERO-discriminating: all nine other scales are multiples of 0.05, so two decimals always "
        "suffice for them - the column has never NEEDED a third decimal and therefore cannot "
        "distinguish 'authored at two decimals' from 'presented at two decimals'. It is a fit over a "
        "population incapable of falsifying it. (3) The delegation sentence at docs/31 section "
        "'Ordinary roster overview' ('Exact derived values and boss circles appear in the "
        "survivability baseline') obliges docs/72 to carry the exact value but says nothing about "
        "WHICH value is exact, so it is consistent with both readings - which is why each side has "
        "read it as supporting theirs. "
        "WHAT IS WRONG IN THE REPOSITORY EITHER WAY: the framing 'content/ follows the source it "
        "cites' is false for this field. EN-07's own source_refs scopes contact_footprint to "
        "GDD-PLAYER-SURVIVABILITY-BASELINE#collision-and-contact-footprints - docs/72, the 0.50 "
        "side - while the scale it stores comes from docs/31. Corrected in README.md too. "
        "WHERE THE EVIDENCE POINTS, AND WHY THAT IS STILL NOT A DECISION: it LEANS TOWARD 0.625. "
        "The motivated-exception argument above is the strongest single argument here and it points "
        "that way, and the three arguments below discriminate nothing, so nothing pulls the other "
        "way with comparable force. It is NOT DECISIVE, for two reasons that are not weak: it is an "
        "inference about a designer's intent, and it runs against the LITERAL TEXT of docs/31, "
        "which states 0.62x and is a document of record; and against the operand-home argument - "
        "docs/31's roster is where body scales live, so the scale column is the natural home of the "
        "authored quantity and docs/72's 0.50 M is the natural home of a presented consequence. A "
        "leaning inference does not overturn a stated number, so the question stays OPEN and it is "
        "the design owner's to close. "
        "WHY THE CURRENT STATE IS HELD, AND ON WHAT GROUNDS: on COST, not on evidence - the lean is "
        "recorded above and holding does not deny it. "
        "The current state is internally consistent under the 0.62 reading. If that reading wins "
        "there is nothing to do; if the other wins the work is one content value plus a document "
        "revert, travelling with the merges. The magnitude is 0.8% of a hitbox, so nothing is at "
        "risk in the interval, and the question belongs to the design owner as a DESIGN question "
        "rather than being inferred from typography. "
        "AND WHY HOLDING IS SAFE RATHER THAN MERELY CHEAP: A30 fails in BOTH directions while the "
        "question is open. Because this entry pins the exact content-side value, changing "
        "body_scale_multiplier fails; because a declared pair that stops diverging is a failure, "
        "correcting docs/72 to 0.496 fails too. Neither side can be taken quietly."
    ),
    ("EN-07", "contact_start_distance_m"): (
        "0.748",
        "The same 0.496 propagated: 0.496 / 2 + 0.50 = 0.748, which docs/72 section 'Collision and "
        "Contact Footprints' states as 0.75. ONE divergence, not two - and that is exactly why the "
        "'both figures are exact under 0.625' argument carries no independent weight; see the "
        "contact_diameter_m entry."
    ),
}

CSV_MIRROR_EXPECTED_COMPARISONS = 98

# How many definitions must author contact_footprint.reference_diameter_m, the
# operand A30's diameter column multiplies. Ten - the ordinary enemy roster. The
# four bosses author their diameters flat (docs/72:105-110) and have no reference,
# which is why this is 10 and not 14.
CSV_MIRROR_REFERENCE_DIAMETER_AUTHORS = 10


def _csv_decimals(text: str) -> int:
    return len(text.split(".", 1)[1]) if "." in text else 0


def check_csv_mirror_agreement(docs: dict[Path, object]) -> list[tuple]:
    """A30 - every value docs/data/contact-damage-pressure.csv shares with content/.

    Seven columns x 14 actors. Four columns compare against an AUTHORED content
    field; three compare against a value the compiler derives from surviving
    operands, which is the comparison docs/40 section "Enemies and bosses" actually
    describes ("derives world speeds/footprints and compares them with the
    survivability report").
    """
    if not PRESSURE_CSV.exists():
        fail(
            f"A30 {rel(PRESSURE_CSV)} is missing, so the CSV/content mirror is unmeasured. This "
            f"rule is not allowed to pass by being unable to run."
        )
        return [("pressure CSV readable", "present", "missing", "FAIL")], ()

    contract = None
    for path, doc in docs.items():
        if path.name == "standard-map-generation-contract.json":
            contract = doc
    if not isinstance(contract, dict) or "reference_mech_speed_m_per_s" not in contract:
        fail("A30 could not read reference_mech_speed_m_per_s, an operand of the speed column.")
        return [("mech base speed readable", "present", "missing", "FAIL")], ()
    base_speed = Fraction(str(contract["reference_mech_speed_m_per_s"]))

    # THE PLAYER'S COLLISION RADIUS IS THE ONE OPERAND WITH NO AUTHORED MIRROR, and
    # the asymmetry with reference_diameter_m below is deliberate rather than an
    # oversight. docs/72:86 states it: "Contact begins when the enemy contact circle
    # and the mech's 0.50M-radius collision circle overlap." It is a PLAYER-baseline
    # constant, and A20's centre-distance rule exists precisely to keep it OUT of
    # content/enemies/, content/bosses/ and content/maps/ - storing the sum there put
    # a second writer on it in fifteen files (Ruling 12, content/transcription-notes.md
    # sections on the centre distance). So there is nothing in the tree to read and
    # this literal is the repository's only copy of it.
    # SEARCHED BEFORE CONCLUDING: every numeric leaf equal to 0.5 under
    # content/**/*.json is 20 leaves, none of them a player/mech footprint field
    # (pulse intervals, arm/grace/decay seconds, per-rank increments, a charging
    # multiplier, an anchor collapse distance); no key anywhere under content/ matches
    # player_radius / collision_radius; and the only occurrences of the phrase are
    # prose in content/README.md and content/transcription-notes.md, which are
    # documentation of this derivation, not values it may read. If the mech baseline
    # ever becomes authored content, read it here the way ref_diameter is read.
    player_radius = Fraction("0.50")  # docs/72:86 - no authored mirror; see above

    by_id = {}
    for path, doc in docs.items():
        if isinstance(doc, dict) and isinstance(doc.get("id"), str):
            if path.parent.name in ("enemies", "bosses"):
                by_id[doc["id"]] = doc

    # THE OTHER OPERAND OF THE DIAMETER COLUMN IS AUTHORED, SO IT IS READ FROM THE
    # TREE. All ten enemy files store contact_footprint.reference_diameter_m = 0.8,
    # A20's DERIVED_FOOTPRINT_FIELD_ALLOWED allowlists it as authored and required to
    # stay, and this derivation multiplies it. It used to be hardcoded here as
    # Fraction("0.80"), which made the derivation agree with ITSELF rather than with
    # the tree: setting the field to 1.0 in all ten files left the whole suite green,
    # 0 failures, 10 of 10 escaped, and 0.9 and 0.8000001 likewise - while the sibling
    # operand body_scale_multiplier went red, so the field was stored, mirrored in the
    # CSV's derivation, allowlisted as authored, and read by nothing. THAT
    # 10-of-10 ESCAPE FIGURE IS A HAND-RUN PROBE against the old hardcoded code -
    # ten files rewritten per injection, three injections in all (1.0, 0.9,
    # 0.8000001), each done by hand and reverted - and no assertion recomputes it.
    # TWO ROWS, because reading it is not enough on its own. The per-actor read makes
    # an edit to ONE file fail that actor's diameter and start-distance comparisons;
    # the population and distinct-value rows make DELETING the field, or giving one
    # file a different reference from the other nine, fail as well - the shared
    # reference is one quantity with one owner, and a per-file reference would be a
    # second owner smuggled in one file at a time.
    authored_ref_diameters: dict[str, Fraction] = {}
    for actor_id, doc in by_id.items():
        footprint = doc.get("contact_footprint") or {}
        if "reference_diameter_m" in footprint:
            authored_ref_diameters[actor_id] = Fraction(str(footprint["reference_diameter_m"]))
    distinct_ref_diameters = sorted(set(authored_ref_diameters.values()))

    import csv as _csv

    compared = 0
    exact_hits = 0
    declared_used: set = set()
    mismatches: list[str] = []
    missing_actors: list[str] = []
    missing_ref_diameter: list[str] = []

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
            elif actor in authored_ref_diameters:
                ref_diameter = authored_ref_diameters[actor]
                diameter = Fraction(str(doc["body_scale_multiplier"])) * ref_diameter
                diameter_basis = (
                    f"body_scale_multiplier x authored contact_footprint."
                    f"reference_diameter_m {ref_diameter}"
                )
            else:
                # No authored diameter and no authored reference to derive one from.
                # The population row below is what reports this; skipping here also
                # drops the comparison count, so the vacuity guard fails too.
                missing_ref_diameter.append(actor)
                continue
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
                declared = CSV_MIRROR_ROUNDED.get(key)
                # BOTH conditions. The exact-value clause is what makes "no
                # tolerance" true: without it the declaration accepted any value
                # rounding to the CSV's figure, a half-last-place band nobody had
                # disclosed. The rounding clause stays so a declaration cannot
                # cover a pair that is not even close to the CSV.
                if declared is not None and got == Fraction(declared[0]) and rounded == want:
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
        (
            "enemy files authoring contact_footprint.reference_diameter_m (the operand "
            "this rule reads instead of hardcoding)",
            CSV_MIRROR_REFERENCE_DIAMETER_AUTHORS,
            len(authored_ref_diameters),
            "ok" if len(authored_ref_diameters) == CSV_MIRROR_REFERENCE_DIAMETER_AUTHORS
            else "FAIL",
        ),
        (
            "distinct authored reference diameters (one shared reference, one owner)",
            "1 (0.8)",
            f"{len(distinct_ref_diameters)} ({', '.join(str(float(d)) for d in distinct_ref_diameters) or 'none'})",
            "ok" if len(distinct_ref_diameters) == 1 else "FAIL",
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
    if len(authored_ref_diameters) != CSV_MIRROR_REFERENCE_DIAMETER_AUTHORS:
        fail(
            f"A30 {len(authored_ref_diameters)} enemy definition(s) author "
            f"contact_footprint.reference_diameter_m, expected "
            f"{CSV_MIRROR_REFERENCE_DIAMETER_AUTHORS}. It is the authored operand this rule "
            f"multiplies by body_scale_multiplier, and A20's DERIVED_FOOTPRINT_FIELD_ALLOWED "
            f"allowlists it on the basis that it stays. Missing from: "
            f"{sorted(set(by_id) - set(authored_ref_diameters))[:12]}"
        )
    if len(distinct_ref_diameters) != 1:
        fail(
            f"A30 the authored reference diameter is not one shared value: "
            f"{[str(d) for d in distinct_ref_diameters]}. docs/72:86 gives ONE reference (the "
            f"Ripper's 0.80 M rank-zero contact diameter) that every ordinary body scale "
            f"multiplies, so a per-file reference is a second owner for one quantity."
        )
    if missing_ref_diameter:
        fail(
            f"A30 {len(missing_ref_diameter)} actor(s) have neither an authored contact diameter "
            f"nor an authored reference to derive one from, so their diameter and start-distance "
            f"columns were not compared: {sorted(missing_ref_diameter)}"
        )
    # A30's own limits, on the output a green run prints. A30 caught 7 of 8
    # attacks when it was reviewed; the one that escaped went through the declared
    # exception, which is the note below. THAT 7 of 8 IS A HAND-RUN PROBE - eight
    # attacks tried by hand at review time - and no assertion recomputes it.
    return rows, (
        f"WHAT IS COMPARED: {compared} value(s) - the 7 CSV columns x 14 actors that both sides "
        f"carry. 4 columns compare against an AUTHORED content field; 3 against values derived from "
        f"surviving operands (diameter, start distance, world speed), which is the comparison docs/40 "
        f"section 'Enemies and bosses' describes. The count is asserted at "
        f"{CSV_MIRROR_EXPECTED_COMPARISONS} because a mirror check over zero values passes free.",
        f"WHERE THE DERIVED COLUMNS' OPERANDS COME FROM, named because a hardcoded operand makes a "
        f"derivation agree with itself: reference_mech_speed_m_per_s READ from "
        f"content/maps/standard-map-generation-contract.json; contact_footprint."
        f"reference_diameter_m READ per actor from the "
        f"{len(authored_ref_diameters)} enemy definition(s) that author it (asserted above, one "
        f"distinct value: {', '.join(str(float(d)) for d in distinct_ref_diameters) or 'none'}); "
        f"body_scale_multiplier and the authored boss diameters READ per actor. The player's "
        f"{float(player_radius):.2f} M collision radius is the ONLY hardcoded operand, from "
        f"docs/72:86, and it "
        f"is hardcoded because A20 keeps it out of content/ deliberately - it is a player-baseline "
        f"constant and storing it in an enemy, boss or map file put a second writer on it.",
        f"DECLARED EXCEPTIONS ARE EXACT PAIRS, NOT BANDS: {len(CSV_MIRROR_ROUNDED)} declared, each "
        f"naming the CSV value AND the single exact content-side value it covers ("
        + ", ".join(f"{a}.{c} = {v[0]}" for (a, c), v in sorted(CSV_MIRROR_ROUNDED.items()))
        + "). An earlier revision required only that the content value round to the CSV's written "
        "precision, which let EN-07's body_scale_multiplier sit anywhere in [0.61875, 0.625) "
        "undetected while this module's comment claimed NO TOLERANCE. There is now no band.",
        "NOT CAUGHT by A30: a value neither side carries, a column the CSV does not have, and an "
        "edit made to BOTH mirrors in the same commit - it asserts agreement, not correctness. It "
        "also does not settle which mirror is authoritative; when that lands the loser becomes "
        "derived and this rule becomes redundant rather than wrong. The EN-07 divergence is an OPEN "
        "design question, recorded with its evidence in CSV_MIRROR_ROUNDED, and A30 fails in both "
        "directions while it stays open.",
    )


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
    live in its body.

    ONLY HALF OF WHAT REMAINS IS A STANDING INVARIANT, and an earlier revision of
    this docstring said the whole of it was. Set equality has two halves and they
    have different futures:
      `missing`    - every predicted removal must STILL be missing. This does hold
                     for every future commit: nothing legitimately re-authors a
                     value the compiler owns.
      `unexpected` - nothing ELSE may be missing. This does NOT. Deleting any
                     authored numeric leaf, for any reason, fails it.
    Controlled individually: retuning EN-01 hull 20 -> 25 in place PASSES, adding
    an authored numeric leaf to EN-01 PASSES, and deleting EN-01.earliest_minute
    FAILS with "1 removed-but-unpredicted". So A29 WILL false-fail on a future
    commit that deletes a field, and the fix then is to re-derive the expectation
    from a newer sweep ref as a deliberate act - not to loosen this rule.
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

    # THE BANNED VALUE IS DERIVED FROM THE TREE, not hardcoded, for the reason A30's
    # reference diameter was: 12 is cadence x (pods - 1), and both operands are
    # AUTHORED in content/weapons/W-BE.json (deployment_cadence_seconds = 6.0,
    # maximum_active_pod_count = 3). Hardcoding the product made this ban agree with
    # itself: retuning the pod cap to 4 makes the derived total 18, and a ban on 12
    # would then police a figure nobody would author while the real derived value
    # walked in unchallenged - the ban narrows to nothing, silently. So the product is
    # computed from the two authored operands and DERIVED_DEPLOYMENT_SECONDS is the
    # DECLARED expectation it must equal, which turns a retune into a deliberate
    # re-declaration instead of a quiet loss of coverage.
    pod_props: dict = {}
    for _, doc in sorted(files_in("weapons", docs).items()):
        if isinstance(doc, dict) and doc.get("id") == SENTRY_POD_WEAPON_ID:
            pod_props = doc.get("fixed_properties") or {}
    cadence = pod_props.get("deployment_cadence_seconds")
    pods = pod_props.get("maximum_active_pod_count")
    if (
        isinstance(cadence, (int, float))
        and not isinstance(cadence, bool)
        and isinstance(pods, int)
        and not isinstance(pods, bool)
        and pods > 1
    ):
        derived_total = Fraction(str(cadence)) * (pods - 1)
        basis = f"{cadence} x ({pods} - 1)"
        rows.append(
            (
                "banned deployment total, derived from W-BE's authored operands "
                f"({basis}), == the declared {DERIVED_DEPLOYMENT_SECONDS}",
                DERIVED_DEPLOYMENT_SECONDS,
                str(float(derived_total)),
                "ok" if derived_total == DERIVED_DEPLOYMENT_SECONDS else "FAIL",
            )
        )
        if derived_total != DERIVED_DEPLOYMENT_SECONDS:
            fail(
                f"the Sentry Pod deployment total derived from content/weapons/ is "
                f"{float(derived_total)} s ({basis}), but DERIVED_DEPLOYMENT_SECONDS declares "
                f"{DERIVED_DEPLOYMENT_SECONDS}. The ban below is on the DERIVED value, so a retune "
                f"of the cadence or the pod cap has to re-declare it deliberately - otherwise this "
                f"rule keeps banning a stale figure and stops covering the live one."
            )
    else:
        derived_total = Fraction(DERIVED_DEPLOYMENT_SECONDS)
        basis = f"declared {DERIVED_DEPLOYMENT_SECONDS} (operands not readable)"
        warn(
            f"{SENTRY_POD_WEAPON_ID}: deployment_cadence_seconds / maximum_active_pod_count were "
            f"not both readable, so the banned deployment total falls back to the declared "
            f"{DERIVED_DEPLOYMENT_SECONDS} s instead of being derived from the tree (field names "
            f"are unvalidated until content/schemas/ exists)"
        )
        rows.append(
            (
                "banned deployment total, derived from W-BE's authored operands",
                DERIVED_DEPLOYMENT_SECONDS,
                "operands not readable - using the declared value",
                "WARN",
            )
        )

    banned_hits: list[str] = []
    for path, doc in sorted(files_in("weapons", docs).items()):
        for jpath, key, value in walk(doc):
            if (
                key
                and DEPLOYMENT_KEY.search(key)
                and not isinstance(value, bool)
                and isinstance(value, (int, float))
                and Fraction(str(value)) == derived_total
            ):
                banned_hits.append(f"{rel(path)}{jpath[1:]} = {value}")
    rows.append(
        (
            f"no authored {float(derived_total):g} s deployment/ramp value in content/weapons/ "
            f"({basis})",
            0,
            len(banned_hits),
            "ok" if not banned_hits else "FAIL",
        )
    )
    if banned_hits:
        fail(
            f"{len(banned_hits)} deployment/ramp field(s) in content/weapons/ hold "
            f"{float(derived_total):g}, which is DERIVED from W-BE's authored operands "
            f"({basis}), not authored (docs/71-initial-weapon-numeric-catalog.md:83, 40:100): "
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
# 275 nulls across 101 of the 138 definition files were disposed of in the pass
# that added this rule, and that tally was counted as that pass finished: 246 keys
# omitted, 20 relic rarity/weighting fields removed as fields no schema will
# declare, 4 boss armor fields removed for the same reason, 3
# external_numerics[n].value keys removed as shape defects, and 2 nested id keys
# removed because the objects they sat on are not independently addressable. It is
# the record of that one disposal and no assertion recomputes it; a green run
# asserts zero nulls today and says nothing about how many there once were.
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
    """A26 - no null anywhere under content/, with no declared exceptions.

    This rglob is deliberately NOT filtered by in_non_definition_dir(). "No null
    anywhere under content/" is a whole-tree claim, and localization/en.json - which
    the definition loader skips - is named in the README as covered here on purpose.
    Do not "fix" it to match A21: A21 counts a population, A26 scans a directory.

    Known consequence for DAT-006, recorded rather than pre-solved: JSON Schema
    authors nulls legally, so the first content/schemas/*.json carrying
    "default": null or null inside an enum will fail this assertion. Measured, not
    predicted - a probe file with {"properties":{"presentation_id":{"default":null}}}
    fails as `...probe.schema.json.properties.presentation_id.default`. The mandate
    behind A26 (40:90) is about absent optional fields in DEFINITIONS, so the
    resolution is a scope decision that belongs to whoever lands the schemas; it is
    not this assertion silently acquiring an exception set, which is the one thing
    A26's docstring rules out.
    """
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
    derived_counts_rows = check_derived_expectation_counts(docs)
    derived_family_rows, derived_family_notes = check_derived_family_absence(docs)
    derived_value_rows, derived_value_notes = check_derived_family_values(docs)
    csv_mirror_rows, csv_mirror_notes = check_csv_mirror_agreement(docs)
    removal_delta_rows = check_derived_removal_delta()
    prefix_rows = check_scope_prefixes(docs)
    bound_rows = check_bound_spelling(docs)
    manifest_rows, manifest_size = check_definition_manifest(docs)
    inventory_rows = check_file_inventory(manifest_size)
    canonical_letter_rows = check_canonical_letters(docs)
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
        "A29/A31 declared counts and vacuity guards (asserted here, not only by derive --check)",
        ("check", "expected", "actual", "status"),
        derived_counts_rows,
        (
            "These rows exist because total_removed, family_count, declared_family_count and "
            "declared_total_removed were written by the generator and read by nothing here. Without "
            "them this file passes an empty family list, empty records, an empty removal multiset "
            "and the counts overwritten with 9999/99. The last three rows are the same defect for "
            "the three search radii A31 PRINTS: editing file_radius_pairs back to the old "
            "unreproducible 55 used to make this tool print '1 : 55 : 668' at exit 0, with only "
            "derive --check objecting.",
        ),
    )
    table(
        "A31 layer 1 of 2 - NAME: no derived-value family reappears under a matching name "
        "(six rules, six scopes; catches a rename only within its own word class)",
        ("check", "expected", "actual", "status"),
        derived_family_rows,
        derived_family_notes,
    )
    table(
        "A31 layer 2 of 2 - VALUE: no removed value sits at a non-operand leaf inside its own "
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
        csv_mirror_notes,
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
    table(
        "A28 Definition (path, id) manifest "
        f"[{rel(CONTENT_DEFINITION_MANIFEST)}; regenerate with {GOLDEN_UPDATE_VARIABLE}=1, "
        "which rewrites it and still fails]",
        ("check", "expected", "actual", "status"),
        manifest_rows,
    )
    table("A21 File inventory", ("check", "expected", "actual", "status"), inventory_rows)
    table(
        "A32 canonical_letter on exactly the six letter resources (40:106, blob 4cded84); "
        "five rows, each blind to a different edit",
        ("check", "expected", "actual", "status"),
        canonical_letter_rows,
        (
            "A value multiset over content/ proves NOTHING about this field: the six added values "
            "are the six ids, so they were already leaves of this tree and a leaf-value comparison "
            "reports 'nothing gained or lost' having checked nothing that changed. Row 2 binds the "
            "new leaf to its own id; row 1 binds the carrier population by NAME, which is the only "
            "row that survives a correlated delete-here/add-there edit keeping the count at 6.",
        ),
    )

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
