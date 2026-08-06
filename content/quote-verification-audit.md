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
    382 of those leaves are named `text`  — the tree-wide count, stated in full in §10
  1,672 entered the candidate population
    305 of those candidates are named `text`
```

The two `text` figures are two different populations and are kept apart deliberately. 382
is every `text` string leaf in the tree; 305 is how many of them clear the candidate gate
above. The other 77 are shorter than three words or are all-caps tokens, so the gate never
admitted them — §10 tests all 382 anyway, because `text` is the one field name worth
measuring outside the gate.

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
55 uncheckable citation.** (`799 = 773 + 26`, and `799 + 378 + 55 = 1,232`.)

**On the two anchorless counts, 55 and 56.** Both are correct and they count different
things. **56** records in the full 1,672 carry a `source_refs` element with no `#anchor`;
**55** of those are decidable and appear in the table above, and the fifty-sixth is below
the decidability gate and is counted in the 406. §12 says 56 because it is talking about
whole records rather than about the decidable table.

### Which tree each figure is measured against

The buckets above are the measurement **as taken**, against the tree as it stood when the
prototype ran. Three findings recorded below have since been fixed in `content/` — §3's
`REL-09` drift, six `UNL-0*` `rules` entries citing the wrong section, and §6's two
trailing periods — and those fixes move **seven** records out of `genuine mismatch` and
two out of `match, only under a named rule`. Nothing else moved, and `docs/` is unchanged,
so the two states differ by exactly this:

| | as measured — §2, §4, §5 | current tree — §6, §12 |
| --- | ---: | ---: |
| match, exact | 773 | 782 |
| match, only under a named rule | 26 | 24 |
| genuine mismatch | 378 | 371 |
| citation too coarse to check | 55 | 55 |
| **decidable, in definition files** | **1,232** | **1,232** |
| **decidable matches** | **799** | **806** |
| matches below the decidability gate | 266 | 266 |
| **whole matched set** | **1,065** | **1,072** |

Every figure in this document names which column it comes from. §10 names neither, because
none of the nine moved records is a `text` leaf or changes a field name's class: its
`382 / 369 / 13` split and its `116 / 32 / 9` classification are identical in both columns
and were re-measured against both.

The mismatch figures are deliberately *not* restated to the current tree: §4's re-pointing
work and §5's anti-golden proof are claims about the 378 that were measured, and
`src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json` freezes exactly those
378 so the claims stay checkable after the tree moves again.

**§13's sixteen re-pointed citations do not move a single figure in this table, and the
reason is worth stating rather than leaving to be rechecked.** Every one of the 16 is
between **28 and 39 normalized characters**, so every one falls *below* the 40-character
decidability gate above and is therefore counted in the **406 undecidable**, not in the 799
matched or the 378 mismatched. A citation fix inside the 406 changes no bucket in this table;
what it changes is the tree, and §13 measures it there.

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

**The evidence artifact did not learn about that fix for two passes, and that is how §5's
recomputation was caught not testing the frozen string at all.** This record read
`located: nowhere` — the drifted string is absent from `docs/` everywhere — and still
re-derived `exact`, which is impossible if the frozen string is what gets tested. The
frozen string is now **refreshed explicitly** for this record, with the reason recorded in
the artifact itself: the live value is the corrected quotation and was verified
character-for-character against `docs/69-initial-relic-catalog.md:153`, inside the
`GDD-INITIAL-RELIC-CATALOG#rel-09--claim-jumper-core` section the record already cites. See
§5 for why a refresh is two hand-written fields rather than a code path, and Ruling 41.

One further case sits nearby and is not drift:
`content/mining-sites/specialized-material-geodes.json → progress_decay.rule` is authored
prose (9 % longest common run with its cited section) filed under a field name that means
quotation everywhere else. A mis-filed authored sentence, not a drifted quote.

## 4. The dominant finding was citation defects, not prose defects

This is the headline, and it is not the result anyone expected.

**248 of the 378 mismatches are verbatim quotations with a wrong citation** — 228 citing
the wrong section of the right document, 20 citing the wrong document, across **71 distinct
strings in 85 distinct string/bucket groups** (fourteen strings are mis-cited in two
different ways, so they appear in both the wrong-section and the wrong-document bucket).
The prose is correct. What is wrong is the pointer.

The largest single instance repeats **182 times**: each of the thirteen utility files —
the radar `UTL-R1` included — stores the seven `shared_acquisition_and_rank_rules` and seven
`modifier_and_timing_rules` sentences verbatim from
`GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules` and `#modifier-and-timing-rules`,
while the only citation covering them is the file-level
`GDD-UTILITY-CATALOG#utl-XX--<name>`. Same shape for all 23 `top_down_silhouette`, 16
`trait_notes`, 6 `player_facing_identity` and 4 `core_tradeoff` values.

**Done for the wrong-citation half.** The re-pointing has landed:
`content/transcription-notes.md`, Ruling 36. Six of the 65 groups — the `UNL-01`…`UNL-06`
`rules[]` entries — were already correct on `master`, so **59 new scoped elements across 31
files** were added and no existing citation was deleted. Measured before and after under
§12's reading: **6 of 248 → 248 of 248** covered by a citation naming a section that
contains them (247 `exact` plus one match under R7a-initial-case, an adopted rule). The 6
are the `UNL-0*` `rules[]` entries and agree with the artifact's stored
`verdict_on_this_tree` on `master`.

**65 groups and 59 new elements, and this figure supersedes the 64 / 58 that an earlier
revision of this file carried in this paragraph and in §11 item 1.** 64 was a projection;
65 is an enumeration, and it reproduces. Both numbers are stated here because a reader
comparing this file against `master` will otherwise see an unexplained disagreement:
`master`'s copy says **64 across 37 files** at its `:137` and `:421`, which becomes 58 after
the same six-group subtraction. **65 / 59 is correct and 64 / 58 is withdrawn.** The
enumeration is corrected in place rather than annotated from elsewhere — a wrong figure left
where a reader meets it, with the correction in another section, means the merged tree
carries both.

**The grouping key, stated so the recipe reproduces.** It is
`(file, pointer with every array index collapsed)` — `[4]` → `[]`. Read literally,
`(file, pointer)` gives **248** and `(file, the existing citation's scope)` gives **38**;
only the index collapse gives 65. Three notations of that same collapse (`[4]` → `[]`,
`[4]` → nothing, `[4]` → `[*]`) induce the identical partition and all give **65 groups / 37
files / 6 already-correct / 59 new**, and both `all records in the group are exact` and `any
record in the group is exact` give 6, so there is no tuning room at the subtraction. The
derivation is committed as
`src/MechaMiner.Tools/ContentImport/derive_citation_pass_expectations.py`.

**Why the two `EN-06` groups do not collapse — the real reason.** They are
`specialist_attack.projectile.lifetime_description` and
`specialist_attack.resonance_interactions.flux_amber`, and they share a target section, so
collapsing them to one `specialist_attack:` element gives 64 groups and 58 new elements.
What that collapse breaks is **`specialist_attack.projectile.lifetime_description`**, which
a bare `specialist_attack:` prefix (specificity 1) leaves shadowed behind the pre-existing
`specialist_attack.projectile: TDD-ENCOUNTERS#needler` (specificity 2). Measured by
performing the collapse and running the checker: `disagreements: 1`,
`EN-06 :: specialist_attack.projectile.lifetime_description: stored 'exact', recomputed
'no-match'`, `RESULT: FAIL`. An earlier revision of this paragraph named
`hard_control_interaction` as the field at risk; that was wrong — it carries its own equally
specific element and is unaffected by the collapse.

**Still open:** the 21 anchors to add for the `external_numerics[].quote` values (the scoped
section was found for every one of the 21, and each then matched with zero normalization
rules). The check still cannot be turned on as a hard failure until that is done.

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

### The proof is committed, not described

A reviewer objected that this claim was the one measurement in the document that could not
be checked: confirming it meant rebuilding the matcher, and a second implementation that
disagreed would not establish which of the two was right. The objection is correct and it
generalises — **an unreproducible measurement is indistinguishable from one nobody made.**

So the measurement ships:

| file | what it is |
| --- | --- |
| `src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json` | **394** records in **two populations that are never added together** — the **378** audit §5 is a claim about, and the **16** of §13 that the frozen 378 cannot see. Per record: the stored string as measured, every citation it failed against, where the string *was* found under maximal normalization, and the maximally-normalized form |
| `src/MechaMiner.Tools/ContentImport/check_quote_mismatch_evidence.py` | re-derives every normalized form from the stored value, re-reads every cited section out of `docs/` at its current content, re-runs the containment test, re-derives `verdict_on_this_tree` per record from `content/` as it now stands, and exits non-zero if any case moves, any verdict disagrees, any live value diverges from its frozen string, or any live value falls under its population's containment gate |

```
$ python3 src/MechaMiner.Tools/ContentImport/check_quote_mismatch_evidence.py
  mismatch records           : 394  = 378 audit-5-378 + 16 live-sweep-16
  citations re-tested        : 655
  records RE-BASELINED       : 2
  the 13 mis-citations the live sweep re-pointed (13):        13 exact
  the 130 absent from docs/ entirely (130):                  129 no-match, 1 exact
  the 248 re-pointing targets (248):                         247 exact, 1 match-under-a-named-rule
  the 3 the live sweep found are not mis-citations (3):        3 no-match
  stored verdict_on_this_tree disagreements: 0
  live value != frozen value: 0
  live values under their population's containment gate: 0
  normalized forms reproduced: 394/394
  FROZEN cited[] citations that did not resolve in docs/: 0
  LIVE source_refs anchors that did not resolve in docs/: 0
  CASES THAT MOVE under maximal normalization: 0
RESULT: ok - zero cases move, as §5 claims
```

### `disagreements: 0` is the weakest line in that output, and it is guaranteed

**Read this before quoting it, as this branch's own PR did.** `stored
verdict_on_this_tree disagreements: 0` is **true by construction on the commit that
generates the artifact**: the stored verdicts *are* the checker's output at that commit. It
can detect drift *after* that commit. It can never establish that the 248 citations are
right, and it is not corroboration that they are.

Two further limits on "the recomputation reproduces `master`'s 371 `no-match` / 7 `exact`",
which sounds like 378 agreeing data points:

- **A degenerate matcher that returns `no-match` unconditionally reproduces 371 of
  `master`'s 378 labels**, because `no-match` is the recomputation's default return and 371
  of the stored labels are `no-match`. Only **seven** records discriminate on the positive
  side. Measured, not reasoned: the control was run.
- **The one genuinely informative control is the specificity rule.** Replacing "equally most
  specific" with "every covering citation" disagrees on exactly **four** records, and they
  are exactly the four `BOSS-01`…`BOSS-04 :: persistence.reentry.behavior` records §12 names.
  That is real evidence *because* it is an external disagreement the artifact could not have
  been fitted to.

So **11 of 378 records carry information** about a matcher that now asserts 248 positives,
and those two controls are the sole non-circular support for it. The agreement count is not
the evidence.

**What is frozen and what is recomputed** is the load-bearing distinction. Each record's
`value` is frozen, because reading these strings back out of the tree today would silently
drop the repaired ones and shrink the claim. Everything else — the normalization, the cited
sections, the containment test — is recomputed on every run against live `docs/`, so the
artifact cannot decay into a transcript of a result. If a design document is ever edited so
that one of these becomes findable, the script fails and this section needs re-measuring,
which is the correct outcome rather than a false alarm.

### The recomputation now tests the frozen string, and did not before

`verdict_on_this_tree` is recomputed per record — the value at that `(file, pointer)` as it
now stands, against the `source_refs` that now cover it, under the four adopted rules — and
a disagreement with the stored field is a failure. Re-pointing a citation (§4) changes it,
which is exactly why it must not be stored prose.

**For two passes that recomputation never compared the live value to the record's own frozen
string, and put no minimum length on the containment.** So "248 `exact`" asserted only that
*whatever string sits at that pointer now* is a substring of the cited section. Measured:
replacing `content/utilities/UTL-R1.json → catalog_wide_rules.modifier_and_timing_rules[0]`
— a 22-word gameplay rule, stored `exact`, one of the 248 — with the single character `"a"`
still gave **248 `exact` / 129 `no-match` / 1 rule-match, `disagreements: 0`, `RESULT:
ok`**, because a one-character string is a substring of every section. §3's `REL-09` is the
tell that exposed it: `located: nowhere` and `exact` at the same time is impossible if the
frozen string is under test.

The recomputation is now anchored twice, and each anchor is a failure rather than a warning:

| anchor | what it asserts |
| --- | --- |
| **identity** | the live value must **equal** the record's expected live value. Divergence names both strings and fails |
| **length** | the adopted-normalized live value must clear its population's **containment gate**, stored in the artifact so the gate is data rather than a constant in the checker: **40 characters / 6 words** for the 378 (§2's decidability gate; the smallest of the 378 is 43/7) and **25 characters / 6 words** for the 16 (the smallest is 28/6) |

With the identity test in place, the same `"a"` sabotage gives `live value != frozen value:
1`, the record named with both strings, `recomputed 'value-diverged'`, `RESULT: FAIL`.

### Refreshing a frozen value is two hand-written fields, never a code path

Two of the 378 now diverge from the tree, and both are legitimate fixes the artifact never
learned about. Each is re-baselined by adding `refreshed_value` and `refreshed_reason` to
**that one record**, and the count of re-baselined records is printed on every run.

**This shape is the point.** If a refresh were an automatic consequence of the live string
verifying against its cited section, then any future change that happened to verify would
silently re-baseline the artifact — and a drift detector whose baseline follows the tree
never fires. The whole value of a frozen string is that it *disagrees* with the tree when
something moved. There is no code path that re-baselines anything.

| record | classification | what verifies the refresh |
| --- | --- | --- |
| `content/relics/REL-09.json → pause_behavior.rule` | the frozen string is the **pre-fix drifted text** of §3; the live string is the corrected quotation | **The cited section.** `docs/69-initial-relic-catalog.md:153`, inside `GDD-INITIAL-RELIC-CATALOG#rel-09--claim-jumper-core`, character-for-character |
| `content/encounters/standard-encounter-schedule.json → minute_rows[33].formation_events[0].reconstruction_basis` | **authored prose, not a quotation** — `no-match` before and after, and there is no cited section to verify it against | **`A27`'s sibling `A24`, plus Ruling 40.** The frozen string ends `See content/transcription-notes.md.`, embedding a repo path in a value, which `A24` forbids unconditionally — so the frozen string cannot legally exist in this tree. The live string is the A24-compliant replacement made by Rulings 25/26/31 |

The second one is refreshed on **weaker and different** evidence than the first, and that is
stated rather than blurred: it is verified against an assertion and a ruling, not against a
source section. A reader should judge the two separately, which is why the reason is stored
per record rather than summarised once.

**`value` is never re-baselined.** It stays the string as measured, because §5's claim is
about the strings as measured; the refresh lands in `refreshed_value` and only the *live*
comparison uses it. Refreshing `value` itself would make `REL-09`'s corrected quotation
findable in its cited section, which would fire `CASES THAT MOVE` and quietly convert an
anti-golden proof over 378 mismatches into a proof over 377.

### The printed counts name the population they count

The old output merged two sets. Of the 248 re-pointing targets, 247 read `exact` and
`EN-06 :: specialist_attack.hard_control_interaction` reads `match-under-a-named-rule`; the
248th `exact` in the printed line was **`REL-09`, a `located: nowhere` record from the other
set**. The two happened to coincide at 248, and had they not, the off-by-one would have
exposed the missing identity test years earlier than a reviewer did. Every verdict line now
names its cohort and every cohort's expected verdicts are stored and asserted separately.

Likewise `citations that did not resolve in docs/: 0` was computed over the frozen `cited[]`
array and said nothing about the 59 live elements this branch added — a line whose plain
reading was broader than its computation. It is now two lines, each naming what it measures:
**`FROZEN cited[] citations`** and **`LIVE source_refs anchors`**.

**Negative controls, each run and reverted individually — never in aggregate.** Every
changed assertion has one, and the results are reported per assertion in
`content/transcription-notes.md`, Ruling 42. Summarised: the identity test catches the `"a"`
sabotage; the containment gate catches a one-character value even when the frozen string is
sabotaged to match it; a flipped stored cohort count, a `refreshed_value` with no
`refreshed_reason`, a removed refresh, a deleted record, a corrupted frozen anchor and a live
anchor re-pointed at a non-existent section each produce a named failure and `RESULT: FAIL`.

## 6. The truncation rule: measured against the corpus, then narrowed

Containment tolerates truncation. A stored string can be a perfect substring and still have
dropped a qualifier. The obvious fix is a sentence-boundary rule — require the quotation to
end where the source sentence ends. **Measuring it first is what saved it from being
adopted.**

**This section is measured against the current tree, not against §2's snapshot**, because
a boundary rule has to be judged on what it would do to the tree it would run in. The
population is the **806 decidable matches** of §2's right-hand column: `782` exact plus
`24` matching under a named rule. That is §2's 799 plus the seven records the §3 and §4
fixes moved from mismatch to match. It is *not* an 800-ish approximation of 799 and it does
not include `content/localization/en.json`, which contributes no matches at all.

The rule as proposed would **newly fail 137 of those 806 decidable currently-passing
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

Across the whole matched set — **1,072 records = the 806 decidable matches above plus the
266 matches that sit below the decidability gate** — this conjunction fires on **exactly 2
cases, with zero false positives**. The undecidable 266 are included here deliberately and
they are the only place in this document where they are bound by anything: the decidability
gate exists because *containment* over a short string is not evidence, and this rule does
not test containment. It tests where a string that already matched sits relative to the
source sentence, which a four-word quotation answers as clearly as a forty-word one. It
isolates precisely the "fragment dressed as a
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
- **Matching is word-bounded.** Unbounded substring matching for `st.` hits **93** places
  in this corpus and `ver.` hits **5** — and not one of them is an abbreviation. They are
  the tails of ordinary words ending a sentence: `first.` 24, `specialist.` 15, `cost.` 10,
  `test.` 9, `manifest.` 6, `burst.` 4 across 21 distinct word forms for `st.`; `forever.`, `solver.` and
  `hover.` for `ver.`. Word-bounded, all 93 and all 5 disappear. A check that fired on those
  would be disabled within a day.

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
than only the 305 that entered the candidate population or the 265 that are decidable. The
word-count gate is lifted for this one measurement, deliberately: every `text` leaf is put
through the containment test regardless of length, because the question here is whether the
*field name* means quotation, and answering it by testing only the long values would beg it.

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
| 1 | ~~**Re-point the 248 wrong-citation quotations.**~~ **Done** — 59 new scoped `source_refs` elements across 31 files (`content/transcription-notes.md`, Ruling 36); enumeration gave 65 groups across 37 files, of which 6 were already correct on `master`. Coverage went 6/248 → 248/248. Item 2 still blocks turning the check on as a hard failure | 31 files | content/integration |
| 2 | **Add the 21 missing anchors** on `external_numerics[].quote`, after which the highest-confidence quotation field in the tree becomes checkable | 21 refs | content/integration |
| 3 | **Fix the 9 MIXED-USE field names**, and rename `behavior_kind` in one of its uses regardless (§10) | 9 names | schema |
| 4 | **Rename `maximum_effect.text` on the 13 `PU-*` files** — bare numeric tokens under a name that promises prose | 13 files | schema |
| 5 | **Re-file `specialized-material-geodes.json → progress_decay.rule`** — authored prose under a quotation field name (§3) | 1 field | content/integration |
| 6 | **Implement the §6 rule in `verify_content.py`** as a refinement of R8-period, keeping R8's legitimate use passing. Currently the rule is measured and its 2 hits are fixed, but nothing enforces it | 1 assertion | tooling |
| 7 | **Stop duplicating catalog-wide rules into every member.** Editing one sentence in `GDD-UTILITY-CATALOG#shared-acquisition-and-rank-rules` will break all 13 utility files at once; same for the `damage_pressure.assumptions` block (14 files — 4 bosses and 10 enemies, not bosses alone) and the relic `acquisition.*` block (10 files, 55 string leaves). This is a content-shape change, not a checker change | content shape | content/integration |
| 8 | **Replace the frozen record list with a live sweep** (§14). The gate as it stands iterates a fixed list of 394 records and *cannot see* a mis-citation added tomorrow in a file it already covers. This is the next design step and the largest remaining hole | 1 tool | tooling |
| 9 | **The three leaves §13 found that are not mis-citations.** `W-BC-suppressive-sequencer → effects.target_preference` (`the` for `that`, a one-word drift in the §3 class), `W-BF-tethered-reaper → effects.single_target_ceiling_at_zero_blade_speed` (a mid-sentence elision), `specialized-material-geodes → partial_payout` (an authored `none; ` prefix on a verbatim tail). Each cites the right section; each needs a **value** ruling, not a citation | 3 fields | content/integration |
| 10 | **The 123 leaves below §13's six-word gate** that are also absent from their cited section (§13). Mostly `unit`, `arrival_warning_presentation`, `combines_with`, `behavior_kind`, `primary_transformation`, `affected_scope`. Whether containment is evidence at 3–5 words is a measurement decision this pass did not take | 123 fields | content/integration |

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
- **§6 covers only the currently-*matching* records** — all 1,072 of them, decidable and
  short alike. Its complement is the 510 `no-match` records and the **56** records whose
  citation carries no `#anchor`. Both counts here are whole records rather than decidable
  ones, which is why 56 appears rather than §2's 55: §2's table counts only the 55 that are
  decidable, and the fifty-sixth anchorless record is short enough to fall in the 406.
  Those records may contain truncations too; they already fail, so a boundary rule can be
  neither credited nor blamed for them.

## 13. The mis-citations the frozen artifact cannot see — a live sweep, and the 16 it found

§4 and §5 are claims about a **fixed list** of records. That list was measured once, and it
is the population of every figure above. So it has a blind spot with a precise shape: **a
mis-citation in a file the artifact already covers, at a pointer the artifact does not hold,
is invisible to it.** Fixing 248 records while a sibling field two lines away stays mis-cited
is the partial-pass trap, and it is not hypothetical.

**The instrument.** An independent sweep of every prose leaf in the live tree — the candidate
gate of §1, plus an **anchored equally-most-specific citation**, plus **six or more words** —
tested under the four adopted rules of §5 and nothing else. It finds **145 leaves absent from
their cited section: 129 of them the artifact's own `no-match` records, and 16 outside it.**

**The six-word gate is a choice and it is the whole result.** Add §2's 40-character half and
the sweep finds 129 and *nothing at all* outside the artifact — the 16 are all between 28 and
39 characters. The 40-character gate is right for asking "is containment evidence *that this
string is a quotation*"; it is wrong for asking "does this citation point at the right
section", because a 30-character six-word sentence fragment is quite long enough for its
absence from a named section to mean something. Below six words the sweep finds **123 more**
(§11 item 10), and whether containment is evidence there is a question this pass did not
answer either way.

**The 16, and what each turned out to be.** Eleven are siblings of fields this branch had
already re-pointed, which is exactly the shape the blind spot predicts:

| n | leaves | was cited | now also cited | sibling already fixed by this branch |
| ---: | --- | --- | --- | --- |
| **4** | `BOSS-01`…`BOSS-04 → persistence.reentry.trigger` | `TDD-ENCOUNTERS#boss-re-entry` | `GDD-INITIAL-ALIEN-ROSTER#boss-arrival-persistence-and-reward` | yes — `persistence.reentry.behavior`, **the same source sentence**, `docs/31-initial-alien-roster.md:172` |
| **6** | `UTL-B1 B2 C1 C2 E1 F1 → installed_to_rank_3` | the file's own `#utl-XX--<name>` section | `GDD-UTILITY-CATALOG#catalog-overview` | yes — `UTL-E2`, the same column of the same table |
| **1** | `REL-09 → core_tradeoff` | `#rel-09--claim-jumper-core` | `GDD-INITIAL-RELIC-CATALOG#catalog-overview` | yes — `REL-01`, `REL-06`, `REL-08`, `REL-10`, **the same table** |
| **1** | `MCH-02 → inherent_trait.effect_detail` | `#catalog-overview` | `GDD-INITIAL-MECH-CATALOG#signature-and-trait-1` | `trait_notes[]` already cites that section |
| **1** | `standard-map-generation-contract → destructible_rock_rules.health_pack.persistence` | `GDD-PLAYER-SURVIVABILITY-BASELINE#health-packs-and-destructible-rocks` | `GDD-CORE-LOOP#combat-pressure` | — |
| **3** | see below | — | **not re-pointed** | — |

**Thirteen were re-pointed and now read `exact`.** Target sections were chosen by Ruling 36's
method — a document the file already cites, then the deepest heading, then the smallest span
— under the four adopted rules and nothing else. No existing citation was deleted.

The one judgement call is the map contract's `health_pack.persistence`, `"persists until
collected or run end"`. Its cited section *does* contain that clause, but inflected:
`docs/72:186` writes "Packs **persist** until collected or run end". The stored string is a
verbatim quotation of a *different* document — `docs/10-core-game-loop.md:129` and
`docs/30-combat-weapons-movement-camera.md:102` both write "The pack **persists** until
collected or run end" — so this is the wrong-document shape of §4's finding, and the deepest
heading of the two candidates wins. Both the alternative and the reason are recorded because
a reader could reasonably prefer keeping only the survivability citation.

**Three of the 16 are not mis-citations at all, and were not forced into that bucket.** Each
already cites the section its sentence comes from; what is wrong is the **value**, so
re-pointing them would be citation-shopping and each needs a ruling instead (§11 item 9):

| leaf | stored | the cited section says | class |
| --- | --- | --- | --- |
| `W-BC-suppressive-sequencer → effects.target_preference` | `any valid enemy not in the memory set` | `…prefers any valid enemy not in **that** memory set.` | a one-word drift — §3's class |
| `W-BF-tethered-reaper → effects.single_target_ceiling_at_zero_blade_speed` | `the same as four ordinary cutters` | `…giving the same single-target ceiling as four ordinary cutters at zero blade speed…` | a mid-sentence elision |
| `specialized-material-geodes → partial_payout` | `none; no partial material or ore payout` | `…provides no partial material or ore payout.` | an authored prefix on a verbatim tail |

All 16 are added to the evidence artifact as a **second population**, `live-sweep-16`, kept
separate from the 378 rather than added to it: §5's anti-golden claim is a claim over exactly
378 mismatches, and a 394-record claim is a different claim. All 16 were re-tested under
maximal normalization with the rest, and **none moves**.

## 14. The limitation this pass did not close: the gate iterates a frozen list

**This is the next design step and it is recorded here rather than attempted, because it is a
different piece of work from fixing a citation.**

The gate as it now stands reads 394 records out of a JSON file and re-tests each one. That
makes it a strong **drift detector** — it fails if a design document moves under a quotation,
if a live value diverges from its frozen string, if a citation is re-pointed without the
artifact learning about it. It is not a **coverage** check, and the difference is not a
nuance:

> **A mis-citation added tomorrow, in a file the artifact already covers, at a pointer the
> artifact does not hold, will not fail this gate.** Nor will one that exists today below the
> six-word sweep gate. The artifact's population is fixed; the tree's is not.

§13's sweep is the shape of the fix: enumerate the population **from the tree on every run**
rather than from a stored list, and let the frozen artifact do only the job a frozen artifact
can do — hold the strings as measured, so §5's claim stays checkable after the tree moves.
The two are complements, not alternatives. What blocks turning a live sweep on as a hard
failure is §11 item 2 (the 21 `external_numerics[].quote` anchors) and item 10 (whether
containment is evidence below six words), in that order.

**Recorded as an open item (§11 item 8) rather than fixed here**, and stated at this length
because "the check passes" and "the tree is checked" have been confused once already on this
branch, and a green run of a frozen-list gate is exactly what makes the confusion easy.

## 15. The practice adopted for value-preservation proofs, after one failed on ordering

An earlier revision of this branch claimed its string differences were "enumerated before
they were measured". **That claim was not supportable and is withdrawn.** `git log -S` for
both the phrase and the `63 added, 2 removed` figure returns only `b482304`, the branch's
last commit, fourteen minutes *after* the change landed in `9c1a4e3` — and `9c1a4e3`'s own
message cannot serve as the record, because that commit *contains* the 59 citations it
describes, so its message could have been read off its own diff. A prediction that exists
only after the measurement is not a prediction.

What is true and defensible in its place: the 59 `(file, scope)` pairs are **independently
re-derivable from the frozen evidence artifact on `origin/master`, and were re-derived** —
see §4 for the grouping key. The derivation yields the 59 pairs **as a set**, and comparing
that set element-wise against the pairs extracted from the diff gives **exact set equality:
nothing derived-but-not-measured, nothing measured-but-not-derived, one element per group,
zero deletions.** That is an agreement between two 59-element sets, not between two integers,
and two wrong sets can agree on counts.

**The mechanism now adopted, so ordering is structural rather than narrated.** One extra
commit, placed **first**, touching **no `content/` file**: a script that re-derives the
expected `(file, scope)` pairs and the expected string and numeric multiset deltas, plus its
committed output. Then a second commit making the change. `git show <first-commit> --stat`
showing zero `content/` files **is** the ordering proof, because at that commit there is no
diff to fit the expectation to. `derive_citation_pass_expectations.py --verify` then measures
the second commit against the first and fails unless the measured delta equals the committed
expectation, multiplicity kept.
