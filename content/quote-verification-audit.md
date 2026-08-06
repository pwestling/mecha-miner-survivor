# Quote verification audit

Can a prose field in `content/` be asserted against the design document it cites? This
records the answer, the measurements behind it, and — at least as importantly — the
plausible rules that measurement showed to be wrong.

The companion record for individual transcription decisions is
`content/transcription-notes.md`. This file is about the *checking*, not about the values.

---

## 1. What was checked

Every prose string under `content/**/*.json` was tested for containment in the design-doc
section its `source_refs` cites — **scoped to the cited section, not to the whole
document**. A citation is `DOC-ID#anchor`; the anchor resolves to a heading, and the
haystack is that heading line through the line before the next heading of the same or a
higher level. So a cited section includes its subsections. That is the permissive reading
and it is stated here because it widens the haystack and therefore *understates* the
mismatch count.

Scoping is not a detail. It is the thing that found the only real defect (§3).

The candidate population was deliberately wide, so that the narrowing would be a result
rather than an input: every string leaf that is not `source_refs`, not `tags`, not a
`*_key`/`*_id` reference, has three or more words, and is not an all-caps token.

## 2. The population and the buckets

```
139 content files, 5,201 string leaves
  1,672 entered the candidate population
    382 of those are `text`
```

| count | bucket |
| ---: | --- |
| **773** | **match, exact** — raw substring of the cited section, zero normalization rules |
| **26** | **match, only under a named rule** — R7a-initial-case 19, R8-period 4, R3-markup 2, R1-quotes 1 |
| **378** | **genuine mismatch** — not in the cited section (228 verbatim in a *different section* of the cited document, 20 verbatim in a *different document*, 130 absent everywhere) |
| **55** | **citation too coarse to check** — the covering `source_refs` element has no `#anchor`, so there is nothing to scope to (35 are verbatim elsewhere in the cited document, 20 are not in it at all) |
| **406** | **undecidable** — the string is too short for containment to be evidence |
| **34** | **out of scope** — `content/localization/en.json`, which has no envelope and no `source_refs` |

**The decidability gate is the most consequential conservative choice here, and it is a
property of the data rather than of the checker.** A case is only decided when its
normalized string is at least 40 characters and at least 6 words. `unit: "Damage per
second"` cannot be verified by containment no matter how the checker is written —
`"Extraction rate"` occurs in sixteen documents, so its absence from one section proves
nothing. 406 of 1,672 fall here and must not be bound by any assertion.

Restricted to the 1,232 decidable cases in definition files: **799 match / 378 mismatch /
55 uncheckable citation.**

## 3. The one genuine drift — found, and fixed

`content/relics/REL-09.json → pause_behavior.rule`. It stored:

> The enemy speed increase ends immediately when mining stops because the mech leaves, the point completes, or the simulation pauses.

`docs/69-initial-relic-catalog.md:153` says:

> The increase begins with forward progress and ends immediately when mining stops because the mech leaves, the point completes, or the simulation pauses.

82 % of the stored string is character-for-character identical. The head was rewritten, and
in rewriting it the clause stating **when the effect begins** was dropped. This is the
defect class the whole exercise exists to catch: a string that looks like a quotation, is
mostly a quotation, and is not one. **Now fixed** — the field holds the full sentence
(`content/transcription-notes.md`, Ruling 30).

One further case sits nearby and is not drift:
`content/mining-sites/specialized-material-geodes.json → progress_decay.rule` is authored
prose (9 % longest common run with its cited section) filed under a field name that means
quotation everywhere else. A mis-filed authored sentence, not a drifted quote.

## 4. The dominant finding was citation defects, not prose defects

This is the headline, and it is not the result anyone expected.

**248 of the 378 mismatches are verbatim quotations with a wrong citation** — 228 citing
the wrong section of the right document, 20 citing the wrong document, across 85 distinct
strings. The prose is correct. What is wrong is the pointer.

The largest single instance repeats **168 times**: each of the twelve non-radar utility
files stores the seven `shared_acquisition_and_rank_rules` and seven
`modifier_and_timing_rules` sentences verbatim from
`GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules` and `#modifier-and-timing-rules`,
while the only citation covering them is the file-level
`GDD-UTILITY-CATALOG#utl-XX--<name>`. Same shape for all 23 `top_down_silhouette`, 16
`trait_notes`, 6 `player_facing_identity` and 4 `core_tradeoff` values.

**Still open.** The fix is **64 new scoped `source_refs` elements across 37 files**, plus
21 anchors to add for the `external_numerics[].quote` values (the scoped section was found
for every one of the 21, and each then matched with zero normalization rules). This is
mechanical work, not a negotiation — but the check cannot be turned on as a hard failure
before it is done, because it would redden the build on 248 strings that are *correct
quotations of the wrong citation*.

## 5. Normalization: four rules adopted, six built and dropped

Each adopted rule is pinned to the specific case in the tree that motivates it. A rule with
no motivating case is not a rule, it is a tolerance.

### Adopted (4)

| rule | what it does | cases | motivating case |
| --- | --- | ---: | --- |
| **R1-quotes** | curly `" " ' ' ′ ″` → straight | 1 | `content/maps/standard-map-generation-contract.json → destructible_rock_rules.destructible_rock.rules[3]` stores `"Non-solid"` straight; `docs/72:207` writes it curly |
| **R3-markup** | strip inline Markdown: `[text](url)` → `text`, backticks, `*`/`_` emphasis | 2 | `content/unlocks/UNL-01.json → rules[3]` ends `… specified in the Utility Catalog.`; `docs/63:44` writes it as an inline link. **Mandatory, not cosmetic:** A24 forbids a `docs/*.md` path in any content string, so the author *cannot* transcribe the link |
| **R7a-initial-case** | the **first character only** may differ in case | 19 | `content/bosses/BOSS-01.json → ability.terrain_interaction` de-capitalises `Terrain ends…` so it reads as a field value. Deliberately *not* full case-folding: A7 treats case as meaningful, and folding would let `hyper gold` pass for `Hyper Gold`. Full case-insensitivity was implemented first and then tightened; nothing was lost |
| **R8-period** | a trailing `.` on the stored string need not be in the source | 4 | `content/unlocks/UNL-01.json → rules[0]` ends `…per specialized resource.` where `docs/63:37` ends with a colon because a table follows |

### Built, tested, and DROPPED (6) — each for lack of a motivating case

**These are as important as the four above.** They are the record of which plausible
tolerances are wrong, and that is precisely the knowledge that gets lost and then
rediscovered by someone who assumes a matcher must obviously need them.

| dropped rule | why |
| --- | --- |
| **whitespace / newline collapsing** | **0 motivating cases.** Every exact match is a raw substring with no whitespace normalization. The design documents keep one paragraph per line, so no quote in this tree spans a hard wrap |
| **non-breaking space** | **0 occurrences of U+00A0** in `content/` or `docs/` |
| **en dash / em dash → hyphen** | **0 motivating cases, and 12 counter-examples.** Twelve matched strings carry an en dash and match exactly — e.g. `content/branches/W-AB-fracture-lance.json → rules[3].text`. The tree transcribes dashes faithfully; a dash rule would only hide a future defect |
| **table-cell pipes (`\|` → space)** | **0 motivating cases, 3 counter-examples.** Strings that cross a cell boundary keep the pipe and match raw: `standard-map-generation-contract.json → distance_bands[0].source_text` = `Up to 45 seconds \| Up to 135M`. All 21 `external_numerics[].quote` values likewise keep their pipes and match with zero rules |
| **ellipsis `…` → `...`** | **0 occurrences** |
| **leading bullet/list-marker stripping** | **0 motivating cases.** Where a bullet is quoted the `- ` is already excluded from the stored string, or (in `external_numerics[].quote`) deliberately included and matched |

*(Full case-insensitivity was also built and then superseded by R7a — a seventh dropped
candidate, folded into R7a's row above rather than counted separately.)*

### The anti-golden proof: there is no slippery slope

"Loosen until green" is the failure mode this exercise most needed to avoid, so it was
measured directly. **Every one of the 378 mismatches was re-tested against its cited
section under maximal normalization** — every optional rule at once, plus full case
folding, plus dash, cell, markup, ellipsis and trailing-punctuation flattening.

**Zero cases move.** Not one mismatch becomes a match. Every case in this tree is either a
raw substring, a substring under one of the four named rules, or absent from the cited
section entirely — with **nothing in between**. So "is there a normalization that makes the
tree pass?" has a clean answer: no amount of loosening rescues the 378, and the four rules
are a short finite list rather than the first four steps of a slope.

That also gives a test for any fifth rule ever proposed: if a new rule is needed to make a
case pass, the zero-cases-move property has been broken, which means the case is a
paraphrase and belongs in the data fix, not in the ladder.

## 6. The truncation rule: measured against the corpus, then narrowed

Containment tolerates truncation. A stored string can be a perfect substring and still have
dropped a qualifier. The obvious fix is a sentence-boundary rule — require the quotation to
end where the source sentence ends. **Measuring it first is what saved it from being
adopted.**

The rule as proposed would **newly fail 137 of the 806 decidable currently-passing
quotations (17 %)**, and of those 137:

| n | what actually follows the quotation in the source | verdict |
| ---: | --- | --- |
| **103** | a Markdown **table-cell boundary** (` \|`) | **misfire** |
| **16** | a **semicolon** ending a list item | **misfire** |
| **6** | a **colon** introducing a table | **misfire** |
| **2** | a sentence terminator **one backtick away** | **misfire** |
| 8 | a comma; the sentence continues | legitimate fragment |
| 2 | prose continues without punctuation | legitimate fragment |
| **0** | — | **genuine truncation** |

**127 misfires, zero genuine truncations.** The table-cell bucket is proven rather than
judged: for 100 of the 103 the stored string is the *entire* cell, and for the other 3 it
is the cell minus a leading bold label that a sibling field holds verbatim. A Markdown
table cell simply has no sentence terminator, and this tree quotes cells whole. Worse for
the proposal: two of the six colon cases are `content/unlocks/UNL-01.json → rules[0]` and
`rules[1]`, complete sentences the document terminates with `:` because a table follows —
so the proposed rule would fail the very cases R8-period exists to let pass.

### The rule that was adopted instead

> **Fail when a stored string (a) begins at a sentence boundary in the source, (b) ends
> with its own `.`/`!`/`?`, and (c) the source sentence continues past that point.**

Across the whole matched set — **1,072 records** — this conjunction fires on **exactly 2
cases, with zero false positives**. It isolates precisely the "fragment dressed as a
complete sentence" defect. **Exception list: zero**, because the fix is not a marker, it is
deleting one character:

- `content/branches/W-DE-focal-array.json → effects.pellet_path`
- `content/branches/W-CE-critical-mass-cycle.json → effects.charge_consumption`

Both trailing periods are now deleted (recorded in `content/transcription-notes.md`). Both
strings are fragments and now read as fragments; the omitted tails were never lost — in
each file a sibling field or a `rules[].text` entry holds them verbatim.

Stated precisely, the finding is a *split* of R8-period rather than a new rule: a stored
trailing terminator is legitimate when the source's own terminator is `:`, `;` or a cell
boundary, and is a defect when the source continues in prose.

The unrefined variants and the head rule are worth keeping as a **warning and a one-time
census** — they are the right instrument for populating a schema's fragment declaration —
but not as a failure.

## 7. Three failure shapes a gate can have, and the two proofs that catch them

This generalises past this check, so it is recorded here beside the results rather than in
a conclusion. The next proposed rule will also be plausible.

**A negative control proves a rule *can* fail.** Every gate fixed on this branch needed
one, and two of them turned out to be structurally incapable of failing. Absent a negative
control, a green check and a broken check are indistinguishable.

**Measuring against the corpus before building proves a rule *doesn't fail wrongly*.** The
dropped sentence-boundary rule is the first time that discipline caught anything on this
project, and what it caught was worse than a no-op: 137 of 806 passing quotations newly
failing, 127 of them honest. A false-positive gate gets switched off or worked around
within a week, which leaves no gate at all *plus* an institutional story about why gates
are annoying.

**This is the third instance on this project of a plausible validator rule that would have
confidently enforced the wrong thing** — and the only reason it is known is that it was
measured against the corpus before it was built. The measurement is cheap; the plausibility
is not evidence.

**A third shape, found separately: a gap that looks filled.** `active_maximum: 16` and
`initial_count: 16` in `content/maps/standard-map-generation-contract.json` were
transcribed and asserted by nothing, while rock Hull 100 (`docs/72:194`) and the 0.80 M
footprint (`docs/72:196`) — which *bracket* them in the same document section — were both
asserted. **A value whose neighbours are asserted reads as covered.** This is not a gate
that cannot fail and not a gate that fires wrongly; it is a gate nobody thought to write,
and its signature is being surrounded by coverage. Both values are now asserted as A13
rows against `docs/51:146`, with the active cap corroborated at `docs/72:203`.

## 8. Head-end cases: settled, and not worth building

A head-boundary rule (does the quotation start at a sentence boundary?) was implemented to
answer whether the defect class exists. It does, and it is benign: **54 of the 56 decidable
head-only cases are noun-phrase or verb-phrase field values whose elided subject is
supplied by the field name.** The clearest instance repeats ten times —
`content/enemies/EN-01…EN-10.json → contact_cadence.first_hit` =
`"immediately when an eligible overlap begins"`, lifted from "The first hit lands
immediately when an eligible overlap begins." The field name *is* the missing subject.
The other two are single-word elisions (a determiner `their`, a subject `Driftmetal`) whose
referent is restored by the JSON path or the file identity.

**The decisive point, recorded explicitly: `REL-09` was `no-match`, and a head rule would
never have reached it.** Its rewritten head means the string is not a substring of the
cited section at all, so the plain containment test already fails it. What caught the only
genuine drift in the tree was **scoping the containment test to the cited section** — not
any boundary refinement. A head rule would have added a census and no findings.

## 9. Caveat: the adopted rule's safety is a property of the corpus, not of the matcher

**This one is flagged rather than buried, because it can stop being true silently.**

The adopted rule's zero-false-positive result depends on `.` being an unambiguous
sentence terminator in `docs/`. Abbreviation-period and decimal-point misfires measured
**zero in both directions** — but only because `docs/` today contains no `e.g.`, `i.e.`,
`etc.`, `approx.`, `vs.`, `No.` or `cf.` anywhere, and no `<digit> s.` followed by a
lowercase word. Nothing about the matcher makes that so. The day a design document writes
"e.g.", the rule begins misfiring on complete, honest quotations that happen to end just
before one, and nothing in the quotation itself will be wrong.

**So it is asserted rather than documented**, because a documented assumption is a
fail-open with a footnote. `A27` in
`src/MechaMiner.Tools/ContentImport/verify_content.py` scans `docs/**/*.md` for a named
list of sentence-internal abbreviations and fails if any appears. Two properties of that
assertion are deliberate:

- **The failure message points at the matcher, not at a quotation.** When it fires, no
  content string is implicated; the rule's premise has lapsed and the rule needs
  revisiting. A message that blamed a quotation would send the reader to innocent data and
  teach them the check is noise.
- **Matching is word-bounded.** Unbounded substring matching for `st.` finds `burst.` (93
  times in this corpus) and `ver.` finds `over.` (5 times) — both sentence ends, neither an
  abbreviation. A check that fired on those would be disabled within a day.

Decimals and unit suffixes (`0.80M`, `1.5 s`, `45.6`) are deliberately **out** of the
list: a decimal point is not a sentence-terminator candidate, since it is not followed by a
sentence-initial capital.

## 10. Field-name classification — the input to schema marking

A field name is **QUOTATION** when ≥ 90 % of its decidable occurrences are provably lifted
from some design document (matched, or matched in the wrong section/document, or blocked
only by a coarse citation), **AUTHORED** when ≤ 10 % are, and **MIXED-USE** in between.

| class | names |
| ---: | --- |
| **116** | **QUOTATION** — declare as quotation-bearing (`text`, `quote`, `rules`, `rule`, `effect_rules`, `description`, `first_hit`, `unaffected_timing`, `primary_limitation`, `raw`, …) |
| **32** | **AUTHORED** — declare as authored description (`assumptions`, `method`, `resolution`, `selection_model`, `cache_opening`, …) |
| **9** | **MIXED-USE** — a naming defect; fix before declaring either way |

The nine MIXED-USE names: `qualitative`, `persistence`, `effect`, `behavior_kind`,
`depleted_state`, `meaning`, `edge_case_rule`, `targeting`, `detail`. MIXED-USE is a defect
in its own right — the same name carries a quotation in one file and an authored sentence
in another, so no schema declaration can bind it correctly as it stands.

**`behavior_kind` needs renaming in one of its two uses regardless of how the quotation
question is settled**, because a registry is minting it as a token: it holds registered
behavior identifiers on enemies and bosses (`"pure contact pursuer"`, `"persistent giant
pursuer with exactly one additional behavior"`) while elsewhere carrying prose. A field
that is simultaneously a registry key and a sentence cannot be declared as either.

`text` is the decisive row and deserves its own statement, over all 382 occurrences rather
than only the decidable ones:

```
382  `text` string leaves in the tree
369  are EXACT raw substrings of their cited section — zero normalization rules
 13  are not quotations at all: content/powerups/PU-*.json → maximum_effect.text
     holds bare numeric tokens ('+15%', '+25', '+0.15 Hull/s', 'One revival')
  0  are drifted, paraphrased, or missing
```

Those 13 are the only non-quotation `text` values in the tree, and the fix is to rename the
field rather than to except it from a declaration.

## 11. What remains open, and who owns it

| # | Open item | Size | Owner |
| --: | --- | --- | --- |
| 1 | **Re-point the 248 wrong-citation quotations.** 64 new scoped `source_refs` elements across 37 files; the correct `doc_id#anchor` is known for each. Blocks turning the quotation check on as a hard failure | 37 files | content/integration |
| 2 | **Add the 21 missing anchors** on `external_numerics[].quote`, after which the highest-confidence quotation field in the tree becomes checkable | 21 refs | content/integration |
| 3 | **Fix the 9 MIXED-USE field names**, and rename `behavior_kind` in one of its uses regardless (§10) | 9 names | schema |
| 4 | **Rename `maximum_effect.text` on the 13 `PU-*` files** — bare numeric tokens under a name that promises prose | 13 files | schema |
| 5 | **Re-file `specialized-material-geodes.json → progress_decay.rule`** — authored prose under a quotation field name (§3) | 1 field | content/integration |
| 6 | **Implement the §6 rule in `verify_content.py`** as a refinement of R8-period, keeping R8's legitimate use passing. Currently the rule is measured and its 2 hits are fixed, but nothing enforces it | 1 assertion | tooling |
| 7 | **Stop duplicating catalog-wide rules into every member.** Editing one sentence in `GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules` will break 12 files at once; same for the boss `assumptions` block (14 files) and the relic `acquisition.*` block (10). This is a content-shape change, not a checker change | content shape | content/integration |

**Not open, and deliberately so:** the 406 undecidable cases. They are short field values,
short values are not quotations, and binding them would produce either false confidence or
false failures. They are a property of the data, not a checker limitation to engineer
around.

## 12. Method caveats

- **The section span includes subsections.** A citation to a `##` heading is checked against
  everything down to the next `##`. Tightening it would move some matches into the mismatch
  bucket; it was not tightened because the wrong-section findings already dominate and
  tightening would inflate them without new information.
- **The locator that explains a non-match uses maximal normalization including full case
  folding**, and is used only to *explain*. No case is promoted to "match" on the strength
  of a locate hit — which is what makes §5's zero-cases-move result a strong statement
  rather than a tautology.
- **Multi-citation fields are checked disjunctively.** When several equally specific
  `source_refs` elements cover one field, a match against any counts. That is the right
  reading, but it means a field with four citations is weakly bound.
- **"Any occurrence may be clean" is the permissive reading** in §6, so every failure count
  there is a floor. In practice it changes nothing: 1,069 of the 1,072 records have a
  single occurrence in their cited section.
- **§6 covers only the currently-*matching* records.** The `no-match` records and the 56
  anchorless citations may contain truncations too; they already fail, so a boundary rule
  can be neither credited nor blamed for them.
