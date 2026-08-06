# `MechaMiner.Tools/ContentImport`

Durable content-import and validation code for the authored JSON catalog under `content/`. This
directory holds no `.csproj`; `MechaMiner.Tools.csproj` is owned by the integration stream.

## `verify_content.py`

A stdlib-only Python 3 checker for `content/`. Run it from anywhere — it locates the repository root
relative to its own path:

```sh
python3 src/MechaMiner.Tools/ContentImport/verify_content.py
```

It prints a summary table set either way, prints failures before warnings, and exits non-zero if any
failure is recorded. Warnings never affect the exit code.

**The assertion table at the top of `verify_content.py` is the authoritative list of what it claims**,
with the `docs/technical/40-content-data-and-validation.md` line behind each claim. In outline, it
checks:

- every `*.json` under `content/` parses as UTF-8 with no duplicate object properties;
- the common definition envelope on every definition — `schema_version`, `content_version`,
  `status` from exactly `development | enabled | disabled | retired`, `tags`, and a non-empty
  `source_refs`. `name_key` and `summary_key` are conditional: required only where the definition has
  a player-facing name or summary, and never an error when omitted. `presentation_id` must be absent
  rather than null;
- that every definition carries a non-empty string `id`. A missing or null `id` is an unconditional
  failure: `ID_NULL_EXPECTED` is empty, because every definition now has a minted stable ID;
- the two exception sets, against `ID_NULL_EXPECTED` (empty) and `NAME_KEY_OMITTED` (three members)
  declared at the top of the script, so an undeclared null `id` or a new `name_key` omission is a
  failure rather than one more warning, and a member that no longer belongs is a warning to shrink the
  list;
- `snake_case` property names at every depth, checked on keys only, so stable ID/enum/kind tokens in
  values keep their exact case;
- that no property name abbreviates a bound as `cap`, `max` or `min`. A cap is a maximum, so the word is
  spelled out and the qualifier rather than the noun distinguishes two bounds on one quantity
  (`{target_minimum, target_maximum, hard_maximum}`). Where the name carries a unit suffix the unit stays
  terminal and the bound word moves to the front (`maximum_control_resistance_percent`). The exception
  list `BOUND_SPELLING_ESCALATED` is now **empty** — its two `W-BF-tethered-reaper` members were resolved
  rather than suppressed, since `docs/71:346` shows 200 bounds the speed-bonus component and 400 the
  total — and it is still asserted for drift like `ID_NULL_EXPECTED`, because a resolved escalation left
  in an exception list is worse than no list;
- that no stale extraction metadata key (`_provenance`, `_source`, `notes`, `note`, `refs`, `lines`,
  `line`, `shared_rule_refs`) survives anywhere at any depth. The singular `note` was added after three
  keys survived a blocklist that carried only the plural;
- that **no `null` appears anywhere under `content/`**, at any depth, in any of the 139 `*.json` files —
  `localization/en.json` included, which the definition loader skips — and with **no exception set at
  all**, because an exception set is a place for a null to hide. A `null` in a source definition is never
  legal: `40:90` materializes an explicit default for every absent optional field, so an absent field gets
  its default while a present-and-`null` one asks runtime to guess. 275 nulls across 101 of 138 definition
  files were disposed of in one pass — 246 keys omitted, 24 fields removed as fields no schema will declare
  (20 relic rarity/weighting, 4 boss `armor`), 3 `external_numerics[n].value` keys removed as shape
  defects, 2 nested `id` keys removed because their objects are parameters of `MGC-01` rather than
  addressable definitions. The two nested `id`s were briefly planned as declared exceptions; removing the
  key instead made the assertion unconditional;
- that **no line-number citation and no repository path** appears in any string value. This replaced
  `docs/.*\.md`, which pinned three incidental spellings of a path — the directory name, a forward slash,
  a lowercase `.md` — and let six forms through: no extension, a backslash separator, no `docs/` prefix,
  uppercase, `.markdown`. It is now two rules keyed on what is wrong: a `:<digits>` suffix after any
  path-like token, in either separator and any case with the extension optional; and any repository path
  at all (`docs`, `src`, `content`, `tools`, `assets` plus a separator), line number or not. A bare
  `#anchor` is **out of scope by design** — it is half of the sanctioned `doc_id#anchor` form, `A9` already
  resolves anchors against real heading slugs, and it carries neither a path nor a line number. The
  narrowness was not hypothetical: the new rules found two real defects the old pattern could not see, a
  `content/`-prefixed path in an encounter-schedule value and a bare extensionless `docs/68` in a `UTL-A1`
  statement, both the class `Ruling 25` removed 13 of;
- **polarity agreement**: where a structured polarity value (a `direction`, or any field valued from the
  closed vocabulary higher/lower, increase/decrease, more/less, faster/slower, longer/shorter,
  raise/reduce, gain/lose) sits beside prose stating the same fact, the two must agree in sign. Prose is
  read from the same object and from the enclosing one. It fires on strict contradiction only, so
  "20% faster without increasing movement speed" is not reported. This automates a check that had to be
  done by hand: six geode resonance directions were verified against `docs/40:104-109` by eye, and
  nothing would have caught a seventh;
- the **percentage-point policy** (`40:95`) on numbers and key names, not prose: every percent-named
  property resolves to at least one numeric leaf; no percent-named numeric value satisfies
  `0 < |v| < 1`, which would be the compiler's normalized factor stored where percentage points belong;
  and no name or object authors the normalized factor beside the points. A name "says `_percent`"
  wherever the token appears, so the 52 mid-name spellings such as `percent_of_mech_base_speed` are
  correct and are not flagged. **A fourth rule covers the case the other three cannot reach**: all three
  begin by asking whether the name says percent, so in the previous revision every rule sat behind
  `if not says_percent: … continue` and a bare number under a non-percent name was never examined —
  `sneaky_bonus: 25` and `damage_bonus: 150` both passed with zero failures, while the docstring
  advertised the rewrite as fixing exactly that. Rule 4 fails any **number** under a relative-magnitude
  name (`bonus`, `penalty`, `increase`, `decrease`, `reduction`, `boost`, `malus`, `discount`,
  `surcharge`, `uplift`) that says neither percent nor any unit-or-kind token: such a number is either
  percentage points or a multiplicative scale and the name does not say which, which `40:95` forbids for
  the first and `40:94` forbids for the second. A unit-or-kind token anywhere in the name excludes it, so
  `single_target_ceiling_multiplier_at_full_bonus` — head noun `multiplier`, `bonus` a mid-name qualifier
  — is not flagged. Rule 4 flags **nothing** authored in this tree: it is a regression guard, and its
  evidence is its negative control, the two injections above, each run and reverted individually;
- that every `source_refs` element resolves — the document ID against `doc_id` front matter under
  `docs/`, and any `#anchor` against a real heading slug in that document;
- that every `source_refs` **scope prefix** resolves to a field that exists in the definition it
  annotates. A citation pointing at a field a ruling removed, or at a name the field no longer has, is
  the same defect class as an anchor pointing at a missing heading;
- `content/localization/en.json`: parses, flat, lexically sorted, duplicate-free, every referenced
  localization key present, and no orphaned string;
- per-catalog entry counts and aggregate row counts, from the `EXPECTATIONS` and `PROBES` tables where
  every row cites its own source doc and line;
- the four authored world-prop **values** folded into the map contract, each against its own citation:
  destructible rock Hull 100 (`docs/72:194`), rock damage footprint diameter 0.80 M (`:196`), health
  pack repair 25 Hull (`:182`), health pack pickup radius 0.25 M (`:185`). This replaced a row *count*
  over key-name patterns, which counted patterns that matched at least once — so two names existing
  satisfied it and no value was ever compared. A missing field is now a failure, not a silent pass;
- the two doc-stated grand totals recomputed from the JSON — PowerUp rank prices must sum to 9,450
  Hyper Gold and the six option-unlock costs to 2,150, with actual vs expected always printed;
- referential integrity for branch → weapon, encounter → enemy, and mech → signature weapon, with the
  reference property names discovered from the data rather than hardcoded;
- one derived-value regression guard: the Sentry Pod deployment interval is the authored 6.0 s, and the
  derived 12 s must not appear as an authored deployment or ramp value in `content/weapons/`; and
- the footprint second-writer guard, which is two rules with two different scopes. No definition under
  `content/enemies/` may carry a contact **diameter** — an enemy authors `body_scale_multiplier` and the
  diameter is `scale × 0.80 M` — and no definition under `content/enemies/`, `content/bosses/` **or**
  `content/maps/` may carry the **centre distance that begins contact**, which is the object's radius
  plus the player's `0.50 M` collision radius in all three. `content/maps/` joined the rule because the
  health pack stored `0.75` = its authored `0.25 M` pickup radius + `0.50 M` (`docs/72:185`), a third
  writer for one player-baseline constant. The diameter rule stops at enemies on purpose: a boss
  diameter is authored, because the boss roster gives bosses no body scale to derive one from
  (`docs/31:121-128` has no `Body` column, and `docs/72:86` scopes the derivation to "every **ordinary**
  body scale") and the survivability baseline states the four boss diameters flat (`docs/72:105-110`).
  `reference_diameter_m` is allowlisted, being the Ripper's authored rank-zero diameter rather than a
  per-enemy derived value.

**What the footprint guard does not do.** Both rules match specific key-name patterns in specific
directories. A derived value reintroduced under a name neither pattern matches, or in a directory
neither rule covers, passes — the guard raises the cost of the mistake, it does not make it impossible.
It is checked on key names rather than values, which catches a *rename inside the rule's own key-name
pattern* and nothing beyond it. **It does not catch a rename generally** — an earlier draft of this
paragraph said it did, and that was false: a rename to a name the pattern does not match is exactly the
case that passes, which is the sentence immediately above. Both claims are narrower than "fails the
build if the field reappears under any name". A28's value layer is the shape that closes this for the
families it covers; A20 has no value layer yet, and adding one is the next design step.

**A28 — the six derived-value families, six rules, six scopes, two layers.** A20 generalised: **115**
stored numbers across **six** families were removed because the compiler owns them, and A28 asserts each
family cannot return.

**Six, not the nine an earlier draft of this pass claimed, and the arithmetic is 166 − 51 = 115.** Three
families were built, verified to reproduce exactly, and then *pulled*:

| Pulled family | Values | Why |
| --- | ---: | --- |
| damage-pressure survivability block | 32 | **Comparand, not a derived duplicate.** `40:114` has the compiler derive world speeds and footprints and "compare them with the survivability report". The report is the independent side of that comparison; authoring it in the compiler makes validation compare a derivation against itself — always agreeing, catching nothing, printing like a working cross-check. |
| resonant-value hit count | 5 | Same reading of `40:114`: these five are rows of that same survivability report (`72:167`). |
| stat upgrade price curve | 14 | **Would move checkable numbers into an unchecked string.** All 14 are restated in prose in the same file (`defining_prose`), and A28 only matches numeric leaves. Editing the prose is *not* the fix and was not done — it is a verified doc quotation, so rewriting its numerals would falsify the citation while every validator stayed green. |

Reproducibility was never the issue for any of the three: all 51 reproduce exactly. **The
operand/comparand distinction is orthogonal to reproducibility**, and the generator now records the same
test applied to all six surviving families.

It has the same shape as A20 and the same limits, with these differences:

- rules are matched against every **name segment** of a pointer, not only the leaf key, because three
  families store their number under a generic leaf (`amount`, `minimum`, `maximum`) inside a
  specifically named parent — `total_payout_per_map.amount` is invisible to a leaf-key-only rule;
- each family's pattern is a **word class**, not a name, and every one of the nine was widened after a
  negative control defeated it. Two rounds of controls were run. The first round *renamed* each removed
  field and all nine guards of that draft fired. The second round injected, per guard, a field **semantically inside
  the family but lexically outside the pattern** — the shape that had already walked
  `aggregate_payout_per_map` past the mining-site draft `total|jackpot` — and **all nine guards missed**:
  `traverse_rate_metres_per_second`, `survivability_pressure.hits_to_defeat_100_hull`,
  `impacts_to_destroy_fresh_mech`, `accrued_cost_hyper_gold`, `accrued_rank_ore_cost`, `price_ladder`,
  `yield_per_seam`, `rolled_up_payout_per_map`, `hyper_gold_from_all_sites`. Nine word lists written by
  listing the spellings already in the tree had the same hole. The rules now name what the family is
  *about*: the world-speed rule matches the **unit** (`m_per_s|metres_per_second|velocity|traverse`), the
  damage-pressure **parent** is the class `pressure|survivability` rather than the one name
  `damage_pressure`, the price-curve rule is a price-or-cost word crossed with a series word, and the four
  aggregate families share `AGGREGATE_WORDS`, which carries the cumulative half
  (`cumulative|accru|accumulat|rolled_up|to_date|so_far|subtotal|tally|…`) as well as the summation half.
  Widening changed **no `content/` value**: each rule was re-controlled at the pinned `sweep_ref` and still
  matches its own removal set and nothing else, so the 166-element prediction is byte-identical;
- the scopes differ per rule for the same reason A20's two do, and so do two exclusions from the word
  classes. An absolute metres-per-second value is *always* derived under `content/enemies/` and
  `content/bosses/`, where a speed is authored as a percentage of the mech baseline, and *always* authored
  under `content/weapons/`, where `projectile_speed_m_per_s` is the real number — so the world-speed rule
  covers the first two only. `content/powerups/` is the one family that cannot use the summation half of
  the aggregate class at all: `total_` is authored **71** times there and every one survives
  (13 `total_cost_hyper_gold` + 58 `total_effect`; check with
  `grep -ho '"total_[a-z_]*"' content/powerups/*.json | sort | uniq -c`), so the class would flag 71
  surviving fields. Its rule is `AGGREGATE_WORDS_NO_TOTAL`. For the same reason `payout` is kept out of the
  mining-site class (`payout_per_installment`, `completion_payout`,
  `exposure_per_secured_payout_multiplier` are authored there) and a bare `all` is kept out of the
  aggregate class (`content/maps/` authors `maximum_hyper_gold_sites_across_all_pockets` and
  `maximum_share_of_all_geodes_per_major_region`, authored bounds on counts rather than sums — hence
  `from_all` and `all_sites$`). **This is why the rules are per-family and not one.** Consolidating them
  reintroduces the `total_` collision; the reason is stated in the code beside `AGGREGATE_WORDS`;
- **a second, VALUE-KEYED layer, which is the one that does not depend on names at all.** For each
  removed value, no non-operand numeric leaf inside its own derivation site may carry that value —
  compared exactly as `Fraction`, with no tolerance. It survives a rename, a relocation within the site,
  a different unit suffix, and a change of arity (`32.0` → `[32.0]`), because none of those change the
  number. Its **radius is the limit, and it is stated rather than hidden**: the derivation site, not the
  file and not the scope. That choice was measured — at file radius this tree has **55** coincidental
  recurrences and at scope radius **400**, almost all magnitude coincidences between unrelated
  quantities (a `1.5` m/s world speed against a `1.5` s control-immunity window; a hit count of `4`
  against `maximum_simultaneous_bosses`). An exception list that size could not be justified entry by
  entry, so the radius is narrow *and said to be narrow*. **One** exception is declared, with its
  reason: `UTL-R1`'s removed `acquisition.total_rank_ore_cost` is `0` (the sum of an empty list) and its
  `acquisition.rank_count` is independently `0`. A declared exception that stops colliding also fails,
  because a stale justification is as much a defect as a missing one.

**What A28 still does not do, after the widening.** A word class is still a word class. The nine guards
moved from "catches renames" to "catches renames and the obvious semantic neighbours"; a tenth probe chosen
adversarially against the *new* lists would pass some of them. Only the damage-pressure family is asserted
structurally — no numeric leaf of **any** name may sit under a `pressure`/`survivability` block — and
generalising that form to the other eight is the next design step, not a claim this rule set makes.

Two segment names are allowlisted, exactly as `reference_diameter_m` is: `purchases` (the authored
checkpoint index the removed cumulative cost derives *from*, which matches only by inheriting its
parent's name) and `total_seam_payout_multiplier` (left authored, because its sibling
`exposure_per_secured_payout_multiplier` has no stated derivation at all).

The rules, scopes and allowlists are **read from `expected_derived_value_removals.json`** rather than
restated in `verify_content.py`, so the assertion and the prediction cannot drift apart.

**A29 — the removal delta is the committed prediction, as set equality, and it is now ONE row.** `115 == 115` would also hold
if one value were removed by mistake and a different one kept by mistake. A29 therefore reads the
sweep-ref tree out of git at the SHA the expectation file names, enumerates its numeric leaves,
subtracts the worktree's, and compares **element by element**: every `(file, pointer, value)` predicted
must be missing, and nothing else may be. It additionally asserts that the added side is empty and that
no surviving numeric leaf changed value — a removal pass introduces no numbers and retunes no operand.
If the sweep ref cannot be read out of git the rule **fails**; it is not allowed to pass by being unable
to run.

One reconciliation heuristic still reports as a warning rather than a failure, because no schema exists
to settle it: formulas held as strings rather than a registered formula kind plus parameters (`40:99`).
It is grouped by property name so the list stays actionable. The percentage heuristic that used to sit
beside it is gone: it matched a `%` glyph in prose, so it emitted 21 warnings about English sentences
while leaving the numeric rule it cited unchecked. A warning list a reader learns to ignore is worse
than no list.

A definition whose `id` is absent or `null` is a **failure**: no definition in `content/` is waiting on
an ID any more. The integration owner minted the last five — the four prose-only mining-site classes
are `SITE-01`…`SITE-04` and the shared elite modifiers are `ELT-01` — so `ID_NULL_EXPECTED` is empty and
the check is unconditional. The list is kept as the declared place to record a future genuinely
unminted ID together with its reason, which is what stops that from becoming a place to hide a real
mistake.

Five kebab-case file names now carry a stable ID without being renamed to it
(`enemies/shared-elite-modifiers.json` is `ELT-01`, and the four `mining-sites/*.json` are
`SITE-01`…`SITE-04`). That is deliberate: the canonical bundle orders by the `id` field, not by the file
stem, so the file name is not load-bearing. See `content/transcription-notes.md`.

It performs no JSON Schema validation: `content/schemas/` does not exist yet, so domain field names
outside the envelope are unvalidated and will need one reconciliation pass when the schemas land.

When an expected count changes because a design document changed, edit the `EXPECTATIONS`/`PROBES`
table and update the `source` citation on that row in the same commit.

## `check_quote_mismatch_evidence.py` and `quote_mismatch_evidence.json`

A second stdlib-only script, run the same way and independent of `verify_content.py`:

```sh
python3 src/MechaMiner.Tools/ContentImport/check_quote_mismatch_evidence.py
```

It exists to make one claim in `content/quote-verification-audit.md` checkable rather than merely
stated. That document's §5 reports that all **378** of its genuine mismatches were re-tested against
their cited sections under *maximal* normalization — every optional rule at once, plus full case
folding — and that **zero** of them move. That result is what separates "four rules were adopted" from
"the matcher was loosened until the tree went green": if no amount of loosening rescues a single
mismatch, four rules is a finite list rather than the first four steps of a slope.

Verifying it by re-implementation does not work, because a second matcher that disagreed would not
establish which of the two was right. So the measurement is committed instead.
`quote_mismatch_evidence.json` holds **394 records in two populations that are never added together** —
the **378** audit §5 is a claim about, and the **16** of audit §13 that the frozen 378 cannot see. Per
record: the stored string as measured, every citation it failed against, where the string *was* found
under maximal normalization, and its maximally-normalized form. The script re-derives every normalized
form from the stored value, re-reads every cited section out of `docs/` at its current content, re-runs
the containment test, and exits non-zero if any case moves.

### `disagreements: 0` is the weakest claim in this output — read this before quoting it

`stored verdict_on_this_tree disagreements: 0` is **true by construction on the commit that generates
the artifact**: the stored verdicts *are* this script's own output at that commit. It detects drift
*after* that commit. **It can never establish that a citation is correct**, and it must not be cited as
corroboration that one is. It was quoted that way on PR #8, and it did not corroborate what it was
offered for.

The same care applies to "the recomputation reproduces `master`'s 371 `no-match` / 7 `exact`", which
sounds like 378 agreeing data points:

- a **degenerate matcher** returning `no-match` unconditionally reproduces **371 of the 378** labels,
  because `no-match` is the recomputation's default return and 371 of the stored labels are
  `no-match` — so only **7** records discriminate on the positive side;
- the one informative control is the **specificity rule**: replacing "equally most specific" with
  "every covering citation" disagrees on exactly **4** records, and they are exactly the four
  `BOSS-01`…`BOSS-04 :: persistence.reentry.behavior` records the audit names. That is real evidence
  because it is an external disagreement the artifact could not have been fitted to.

**11 of 378 records carry information** about a matcher now asserting 248 positives. Those two controls
are the load-bearing evidence; the agreement count is not.

### What is frozen, what is re-baselined by hand, what is recomputed

**Each record's `value` is frozen** — reading these strings back out of the tree today would silently
shrink the claim — and it is the needle of the maximal-normalization test. `docs/` is the half of the
comparison this repository can still change, so it is read live: if a design document is ever edited
such that one of these becomes findable, the script fails and audit §5 needs re-measuring. That is the
correct outcome, not a false alarm.

**`refreshed_value` + `refreshed_reason` re-baseline one record at a time, by hand.** They are present
on exactly the records whose live string has legitimately changed since the measurement (two, today),
and the count is printed on every run. There is deliberately **no code path** that re-baselines: if a
refresh were an automatic consequence of the live string verifying, any future change that happened to
verify would silently move the baseline, and a drift detector whose baseline follows the tree never
fires.

**Everything else is recomputed**, including `verdict_on_this_tree` per record — and that recomputation
is anchored to the record twice, because for two passes it was anchored to nothing: the live value must
**equal** the record's expected live value, and its normalized form must clear the population's stored
**containment gate**. Without the first, `exact` says only that whatever string sits at that pointer now
is a substring of the cited section — which a single character satisfies, as it did.

It is not wired into `verify_content.py`, and deliberately so. `verify_content.py` asserts properties of
`content/` against `docs/`; this asserts a property of a *measurement* the audit reports. Its negative
controls are recorded per assertion in `content/transcription-notes.md`, Ruling 42, each run and
reverted individually.

## `derive_citation_pass_expectations.py` and `expected_citation_deltas.json`

```sh
python3 src/MechaMiner.Tools/ContentImport/derive_citation_pass_expectations.py           # derive
python3 src/MechaMiner.Tools/ContentImport/derive_citation_pass_expectations.py --verify  # measure
```

Derives, from the frozen evidence artifact and a live sweep at a named git ref — **never from a diff** —
what a citation pass is expected to change: the `(file, scope)` pairs, and the exact string and numeric
multiset deltas with multiplicity kept. The derivation and its committed output go in a commit that
touches **zero files under `content/`**, so `git show <that commit> --stat` is itself the ordering
proof; the change lands in a second commit and `--verify` fails unless the measured delta equals the
committed expectation element by element. `content/transcription-notes.md`, Ruling 43 records why this
replaced a narrated "enumerated before it was measured" claim that no commit supported.

It also carries the live sweep that found the 16 mis-citations of audit §13 — the ones a frozen
394-record list structurally cannot see. Audit §14 records that limitation as the next design step.

## `derive_derived_value_expectations.py` and `expected_derived_value_removals.json`

```sh
python3 src/MechaMiner.Tools/ContentImport/derive_derived_value_expectations.py          # derive
python3 src/MechaMiner.Tools/ContentImport/derive_derived_value_expectations.py --check  # regenerates?
```

Enumerates, from a pinned commit SHA rather than `HEAD`, the stored numbers `content/` no longer authors
because the compiler derives them, and records for each one its operands, its arithmetic, and the
`docs/` line that assigns the derivation. Its output is the input to A28 and A29 — the rules are not
restated in `verify_content.py`, so the assertion and the prediction are one artifact.

A candidate qualifies only if it reproduces **exactly** in `fractions.Fraction`, never in binary float;
every operand survives the removal; a document assigns the derivation; and no `source_refs` scope prefix
is left dangling. A stored value that disagrees with its operands is a defect, not a redundancy: the
script names both numbers and exits non-zero rather than removing it. It also refuses to write unless
each family's A28 rule matches that family's removal set **and nothing else** in its scope at the sweep
ref — a rule that also flagged a surviving authored field would be unlandable, and one that flagged
fewer would let part of the family return.

Six things reproduce exactly and are still retained, listed with their arithmetic in the file, because
no `docs/` line assigns them: the beacon threshold times, `sources[].depletion_seconds`,
`resonant_damage`, `ordinary_contact_damage_replaced_during_charge`, the three
`relative_to_standard_seam` multipliers, and the 45 weapon DPS estimates. The DPS family is the
interesting one — `40:203` *does* assign "DPS estimates" to the compiler, so it passes the document test
and fails the arithmetic one: the burst and horde rules vary with each weapon's behaviour kind and no
single rule reproduces all 45. It is out of scope, not cleared.

Six further values are derived **and** operands; the file records why each stays, including
`total_cost_hyper_gold`, which is the operand A14's second row sums to the doc-stated 9,450.
