#!/usr/bin/env python3
"""Re-run the anti-golden measurement behind content/quote-verification-audit.md §5.

Python 3, standard library only, no dependencies. Run from anywhere:

    python3 src/MechaMiner.Tools/ContentImport/check_quote_mismatch_evidence.py

Exit code is non-zero if any record moves, or if any frozen normalized form
fails to reproduce from its stored value.

================================================================================
WHY THIS FILE EXISTS
================================================================================

content/quote-verification-audit.md §5 makes a claim that is worth more than the
four normalization rules it defends:

    Every one of the 378 mismatches was re-tested against its cited section
    under MAXIMAL normalization - every optional rule at once, plus full case
    folding, plus dash, cell, markup, ellipsis and trailing-punctuation
    flattening. Zero cases move.

That claim is what makes "we adopted four rules" different from "we loosened
until it went green": if no amount of loosening rescues a single mismatch, the
four rules cannot be the first four steps of a slope.

A reviewer objected that the claim was unverifiable. Confirming it meant
rebuilding the matcher, and a second implementation that disagreed would not
establish which one was right. The objection is correct, and it generalises: an
unreproducible measurement is indistinguishable from one nobody made.

So the measurement is committed rather than described. `quote_mismatch_evidence.json`
beside this file carries all 378 mismatch records - the stored string, the
citation it failed against, and the maximally-normalized form of the string - and
this script re-derives every one of them from `docs/` and re-tests it. The
property is now checkable against an artifact rather than re-derived from a
description of one.

================================================================================
READ THIS BEFORE QUOTING `disagreements: 0`
================================================================================

`stored verdict_on_this_tree disagreements: 0` is the WEAKEST claim this script
makes, and on the commit that generates the artifact it is TRUE BY CONSTRUCTION:
the stored verdicts ARE this script's own output at that commit. It can detect
drift AFTER that commit. It can never establish that the 248 citations are
right. It was quoted as corroboration for the re-pointing pass on PR #8, and it
does not corroborate it.

Two further limits on the "reproduces master's 371 no-match / 7 exact" claim,
which sounds like 378 agreeing data points:

  - A DEGENERATE MATCHER that returns "no-match" unconditionally reproduces 371
    of master's 378 labels, because "no-match" is `verdict_now`'s default return
    and 371 of the stored labels are "no-match". Only SEVEN records discriminate
    on the positive side.
  - The one genuinely informative control is the SPECIFICITY rule: replacing
    "equally most specific" with "every covering citation" disagrees on exactly
    FOUR records, and they are exactly the four BOSS-01..BOSS-04
    persistence.reentry.behavior records the audit names. That is real evidence
    precisely because it is an external disagreement the artifact could not have
    been fitted to.

So 11 of 378 records carry information about a matcher that now asserts 248
positives, and that is the sole non-circular support for it. The load-bearing
evidence is those two controls, not the agreement count.

================================================================================
WHAT IS FROZEN AND WHAT IS RECOMPUTED
================================================================================

FROZEN in the JSON, because the tree has moved since the measurement:
  - every record's stored string AS MEASURED (`value`), and the citation
    covering it. Reading those strings back out of the tree today would silently
    drop the repaired ones from the population and turn a 378-record claim into
    a smaller one. `value` is what audit §5's claim is ABOUT, and it is the
    needle of the maximal-normalization test. It is never re-baselined.

RE-BASELINED, one record at a time, explicitly, and never by a code path:
  - `refreshed_value` + `refreshed_reason`, present on exactly the records whose
    live string has legitimately changed since the measurement. The expected
    live value is `refreshed_value` when present and `value` otherwise.

    This shape is deliberate. If a refresh were an automatic consequence of the
    live string verifying against its cited section, then any future change that
    happened to verify would silently re-baseline the artifact - and a drift
    detector whose baseline follows the tree never fires. The whole value of a
    frozen string is that it DISAGREES with the tree when something moved. So
    there is no such code path: a refresh is two fields written by hand, with the
    reason recorded, and the count of them is printed on every run.

RECOMPUTED on every run, so the artifact cannot rot into a transcript:
  - the maximal normalization of every stored string, checked against the
    frozen `maximal_normalized` field;
  - the cited section of every citation, read out of docs/**/*.md at its
    current content;
  - the containment test itself;
  - `verdict_on_this_tree`, per record - the value at that (file, pointer) as it
    now stands, against the source_refs that now cover that pointer - and a
    disagreement with the stored field is a FAILURE.

================================================================================
WHAT `verdict_on_this_tree` IS ANCHORED TO, AND WHY IT HAD TO BE
================================================================================

The recomputation used to read the live value at (file, pointer) and test
`raw in hay` WITHOUT ever comparing that value to the record's own frozen
string, and with no minimum-length guard on the containment. That made "248
exact" assert only that WHATEVER STRING SITS THERE NOW is a substring of the
cited section. Measured: replacing
content/utilities/UTL-R1.json :: catalog_wide_rules.modifier_and_timing_rules[0]
- a 22-word gameplay rule, stored `exact`, one of the 248 - with the single
character "a" still produced 248 exact / 129 no-match / 1 rule-match,
disagreements: 0, RESULT: ok, because a one-character string is a substring of
every section. The tell was content/relics/REL-09.json, which read
`located: nowhere` and still re-derived `exact` - impossible if the frozen string
were under test.

So the recomputation is now anchored twice:

  1. IDENTITY. The live value must equal the record's expected live value. A
     divergence is a failure that names both strings; it is never absorbed.
  2. LENGTH. The adopted-normalized live value must clear the record's
     population's containment gate (stored per population in the artifact, so
     the gate is data rather than a constant buried here). Containment of a short
     string is not evidence - that is the same reasoning as audit §2's
     decidability gate - so a record that falls under the gate is a failure
     rather than a quieter pass.

================================================================================
THE TWO POPULATIONS, COUNTED SEPARATELY
================================================================================

The printed "248 exact" used to conflate two different sets. Of the 248
re-pointing targets, 247 read `exact` and EN-06 :: hard_control_interaction
reads `match-under-a-named-rule`; the 248th `exact` in that line was REL-09, a
`located: nowhere` record from the OTHER set. The two happened to coincide, and
had they not, the off-by-one would have exposed the missing identity test. So
every verdict line now names the cohort it counts, and each cohort's expected
verdicts are stored and asserted separately.

docs/ is the half of the comparison this repository can still change, and it is
read live. If a design document is edited so that one of these strings becomes
findable under maximal normalization, this script fails and audit §5 needs
re-measuring - which is the correct outcome, not a false alarm.

================================================================================
WHAT "MAXIMAL" MEANS HERE
================================================================================

Strictly more permissive than the four adopted rules, on purpose. Both sides get
NFC, exotic-space folding, whitespace collapsing, curly->straight quotes, all
dash forms -> hyphen, inline-Markdown stripping, table-cell pipes -> space,
ellipsis -> "...", and FULL case folding. The needle additionally loses a leading
list marker and every trailing terminator.

Full case folding is the one that matters. The adopted R7a-initial-case rule
folds the FIRST CHARACTER ONLY, deliberately, because this tree treats case as
meaningful (verify_content.py A7) and folding everything would let "hyper gold"
pass for "Hyper Gold". Maximal normalization folds it all - it is not a proposal,
it is the ceiling that the proposals are measured against.
"""

from __future__ import annotations

import json
import re
import sys
import unicodedata
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[2]
DOCS = REPO_ROOT / "docs"
CONTENT = REPO_ROOT / "content"
EVIDENCE = HERE / "quote_mismatch_evidence.json"

# Asserted per population, not inferred from the file's length, so a truncated or
# padded artifact is a failure rather than a quieter claim. The two populations
# are separate claims about separate things and are never added together:
#
#   audit-5-378   the 378 mismatches audit §5's anti-golden proof is ABOUT.
#   live-sweep-16 the 16 mis-cited leaves the frozen 378 CANNOT SEE, found by the
#                 live sweep of audit §13. Adding them to the 378 would change
#                 what §5 is a claim over; they are counted alongside it instead.
EXPECTED_POPULATION_COUNTS = {"audit-5-378": 378, "live-sweep-16": 16}


# --------------------------------------------------------------------------
# maximal normalization
# --------------------------------------------------------------------------

SMART_QUOTES = {
    "‘": "'", "’": "'", "‚": "'", "‛": "'",
    "“": '"', "”": '"', "„": '"', "‟": '"',
    "′": "'", "″": '"', "´": "'",
}
DASHES = {"–": "-", "—": "-", "−": "-", "‒": "-", "‐": "-"}
EXOTIC_SPACES = (
    "           "
    "    　﻿"
)


def _collapse(s: str) -> str:
    s = unicodedata.normalize("NFC", s)
    for ch in EXOTIC_SPACES:
        s = s.replace(ch, " ")
    return re.sub(r"\s+", " ", s).strip()


def _strip_markup(s: str) -> str:
    s = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", s)
    s = s.replace("`", "")
    s = re.sub(r"(\*{1,3})(?=\S)(.+?)(?<=\S)\1", r"\2", s)
    s = re.sub(r"(?<![A-Za-z0-9_])_{1,2}(?=\S)(.+?)(?<=\S)_{1,2}(?![A-Za-z0-9_])", r"\1", s)
    return s


def _maximal_common(s: str) -> str:
    """Everything applied to BOTH sides of the containment test."""
    for a, b in SMART_QUOTES.items():
        s = s.replace(a, b)
    for a, b in DASHES.items():
        s = s.replace(a, b)
    s = _strip_markup(s)
    s = s.replace("|", " ")
    s = s.replace("…", "...")
    return _collapse(s).casefold()


def maximal_haystack(s: str) -> str:
    return _maximal_common(s)


def maximal_needle(s: str) -> str:
    """Common folding, plus the two needle-only rules.

    A leading list marker goes because a bullet lifted out of docs/ keeps its
    "- "; every trailing terminator goes because R8-period's maximal form is
    "any trailing punctuation may differ", not just ".".
    """
    s = re.sub(r"^\s*(?:[-*+]|\d+\.)\s+", "", s)
    s = _maximal_common(s)
    return s.rstrip(".!?;:, ").strip()


# --------------------------------------------------------------------------
# ADOPTED normalization - the four rules of audit section 5, and nothing else.
#
# This is the OTHER matcher in this file and the two must not be confused. The
# maximal one above is a ceiling that nothing is allowed to pass under; this one
# is the real rule, and it is what `verdict_on_this_tree` is measured with.
#
#   R1-quotes        curly quote characters -> straight
#   R3-markup        inline Markdown stripped (links, backticks, emphasis)
#   R7a-initial-case the FIRST character only may differ in case
#   R8-period        a trailing "." on the stored string need not be in the source
#
# Deliberately absent: whitespace collapsing, dash folding, cell-pipe folding,
# ellipsis folding and full case folding. Audit section 5 measured each of those
# to have zero motivating cases, and three of them to have counter-examples in
# this tree. Adding one here would quietly re-open the slope that section closes.
# --------------------------------------------------------------------------


def adopted(s: str) -> str:
    """R1 and R3, applied to both sides."""
    s = unicodedata.normalize("NFC", s)
    for a, b in SMART_QUOTES.items():
        s = s.replace(a, b)
    return _strip_markup(s)


def adopted_variants(value: str) -> list[str]:
    """The needle under R7a and R8, which apply to the stored string only."""
    base = adopted(value)
    out = {base}
    if base:
        out.add(base[0].swapcase() + base[1:])
    for form in list(out):
        if form.endswith("."):
            out.add(form[:-1])
    return [f for f in out if f]


# --------------------------------------------------------------------------
# Which source_refs elements cover a JSON pointer, and which of those count.
#
# Audit section 12: "Multi-citation fields are checked disjunctively. When
# several equally specific source_refs elements cover one field, a match against
# any counts." EQUALLY SPECIFIC is the load-bearing half. A file-level citation
# has specificity 0 and a scope prefix's specificity is its segment count, so
# `persistence.reentry: X` (2) hides `persistence: Y` (1) for a field under
# `persistence.reentry`. Checking every covering element instead - which is the
# obvious implementation and the wrong one - makes four BOSS-0* records read as
# matches on the tree this artifact was measured against, and the artifact says
# they are not. That disagreement is how this rule was pinned down.
# --------------------------------------------------------------------------

POINTER_SEGMENT = re.compile(r"([a-z0-9_]+)((?:\[\d+\])*)")
PREFIX_SELECTOR = re.compile(r"\[(\d+)?(?:\.\.(\d+))?\]")


def _segments(path: str) -> list[str]:
    """Split on '.' outside brackets, so 'rules[2..3]' survives intact."""
    out, depth, cur = [], 0, ""
    for char in path:
        if char == "[":
            depth += 1
        elif char == "]":
            depth -= 1
        if char == "." and depth == 0:
            out.append(cur)
            cur = ""
            continue
        cur += char
    out.append(cur)
    return out


def prefix_covers(prefix: str, pointer: str) -> bool:
    """Does scope `prefix` cover the JSON pointer `pointer`?"""
    pseg, qseg = _segments(prefix), _segments(pointer)
    if len(pseg) > len(qseg):
        return False
    for p, q in zip(pseg, qseg):
        if p.split("[", 1)[0] != q.split("[", 1)[0]:
            return False
        psel = PREFIX_SELECTOR.findall(p)
        qsel = PREFIX_SELECTOR.findall(q)
        if psel and not qsel:
            return False
        for (lo, hi), (index, _) in zip(psel, qsel):
            if lo == "" and hi == "":
                continue          # [] - every element
            low = int(lo)
            high = int(hi) if hi else low
            if not low <= int(index) <= high:
                return False
    return True


def covering_citations(refs, pointer: str) -> list[str]:
    """The equally-most-specific citations covering `pointer`."""
    best, chosen = -1, []
    for ref in refs:
        if not isinstance(ref, str):
            continue
        if ": " in ref:
            prefix, cite = ref.split(": ", 1)
            prefix = prefix.strip()
            if not prefix_covers(prefix, pointer):
                continue
            specificity = len(_segments(prefix))
        else:
            cite, specificity = ref, 0
        if specificity > best:
            best, chosen = specificity, [cite]
        elif specificity == best:
            chosen.append(cite)
    return chosen


def value_at(doc, pointer: str):
    """Resolve a dotted/indexed JSON pointer, or raise KeyError/IndexError."""
    current = doc
    for segment in _segments(pointer):
        match = POINTER_SEGMENT.fullmatch(segment)
        if match is None:
            raise KeyError(segment)
        current = current[match.group(1)]
        for index in re.findall(r"\[(\d+)\]", match.group(2)):
            current = current[int(index)]
    return current


# --------------------------------------------------------------------------
# doc index - doc_id -> section spans, matching verify_content.py's A9 slugs
# --------------------------------------------------------------------------

FENCE = re.compile(r"^\s*(?:```|~~~)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")


def slugify(heading: str) -> str:
    text = re.sub(r"`([^`]*)`", r"\1", heading)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"[*~]", "", text)
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text)
    return re.sub(r"\s", "-", text)


def build_doc_index(docs_root: Path) -> dict:
    """doc_id -> {lines, anchors: {slug: (start, end)}}.

    A section's span runs from its heading line to the line before the next
    heading of the same or a higher level, so a cited section INCLUDES its
    subsections. That is the permissive reading, and it is the one the audit
    used: it widens the haystack, so it can only make a mismatch MORE likely
    to move, never less.
    """
    index: dict[str, dict] = {}
    for path in sorted(docs_root.rglob("*.md")):
        lines = path.read_text(encoding="utf-8").splitlines()
        if not lines or lines[0].strip() != "---":
            continue
        doc_id = None
        for line in lines[1:]:
            if line.strip() == "---":
                break
            m = re.match(r"\s*doc_id\s*:\s*(\S+)\s*$", line)
            if m:
                doc_id = m.group(1).strip("\"'")
        if not doc_id:
            continue
        headings = []
        in_fence = False
        for i, line in enumerate(lines):
            if FENCE.match(line):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            m = HEADING.match(line)
            if not m:
                continue
            slug = slugify(m.group(2))
            if slug:
                headings.append((i, len(m.group(1)), slug))
        seen: Counter = Counter()
        anchors: dict[str, tuple[int, int]] = {}
        for n, (lineno, level, slug) in enumerate(headings):
            key = slug if seen[slug] == 0 else f"{slug}-{seen[slug]}"
            seen[slug] += 1
            end = len(lines)
            for lineno2, level2, _ in headings[n + 1:]:
                if level2 <= level:
                    end = lineno2
                    break
            anchors[key] = (lineno, end)
        index[doc_id] = dict(path=path, lines=lines, anchors=anchors)
    return index


# --------------------------------------------------------------------------
# the measurement
# --------------------------------------------------------------------------

def main() -> int:
    if not EVIDENCE.exists():
        print(f"FAIL: missing evidence artifact {EVIDENCE}")
        return 1
    payload = json.loads(EVIDENCE.read_text(encoding="utf-8"))
    records = payload["records"]
    index = build_doc_index(DOCS)

    failures: list[str] = []
    moved: list[str] = []
    normalization_drift: list[str] = []
    frozen_unresolved: list[str] = []
    live_unresolved: list[str] = []
    verdict_drift: list[str] = []
    value_divergence: list[str] = []
    gate_violation: list[str] = []
    verdicts: dict[str, Counter] = {}
    citations_tested = 0
    refreshed = 0
    adopted_haystacks: dict[tuple[str, str], str] = {}
    populations = payload["populations"]

    def adopted_section(doc_id: str, anchor: str) -> str | None:
        key = (doc_id, anchor)
        if key not in adopted_haystacks:
            entry = index.get(doc_id)
            if entry is None or anchor not in entry["anchors"]:
                return None
            start, end = entry["anchors"][anchor]
            adopted_haystacks[key] = adopted("\n".join(entry["lines"][start:end]))
        return adopted_haystacks.get(key)

    def expected_live_value(rec) -> str:
        """The string this record asserts is at (file, pointer) TODAY.

        `refreshed_value` when the record has been explicitly re-baselined, and
        the frozen as-measured `value` otherwise. There is no third case and no
        code path that computes one.
        """
        return rec.get("refreshed_value", rec["value"])

    def verdict_now(rec) -> str:
        """Re-derive verdict_on_this_tree from content/ as it stands.

        Anchored to the record: the live value must BE the expected live value,
        and its adopted-normalized form must clear the population's containment
        gate. Neither is a warning - both are failures, recorded by the caller.
        """
        where = f"{rec['file']} :: {rec['pointer']}"
        path = REPO_ROOT / rec["file"]
        if not path.exists():
            return "file-gone"
        doc = json.loads(path.read_text(encoding="utf-8"))
        try:
            value = value_at(doc, rec["pointer"])
        except (KeyError, IndexError, TypeError):
            return "field-gone"
        if not isinstance(value, str):
            return "field-gone"

        wanted = expected_live_value(rec)
        if value != wanted:
            value_divergence.append(
                f"{where}:\n"
                f"      frozen{' (refreshed)' if 'refreshed_value' in rec else ''}: "
                f"{wanted!r}\n"
                f"      live  : {value!r}"
            )
            return "value-diverged"

        raw = adopted(value)
        gate = populations[rec["population"]]["containment_gate"]
        if len(raw) < gate["min_characters"] or len(raw.split()) < gate["min_words"]:
            gate_violation.append(
                f"{where}: adopted-normalized live value is {len(raw)} character(s) / "
                f"{len(raw.split())} word(s), under this population's containment gate "
                f"of {gate['min_characters']}/{gate['min_words']}. Containment of a "
                f"string this short is not evidence."
            )
            return "under-the-containment-gate"

        forms = adopted_variants(value)
        best = "no-match"
        for cite in covering_citations(doc.get("source_refs", []), rec["pointer"]):
            if "#" not in cite:
                continue
            hay = adopted_section(*cite.split("#", 1))
            if hay is None:
                live_unresolved.append(f"{where}: live source_refs cites {cite}")
                continue
            if raw in hay:
                return "exact"
            if any(form in hay for form in forms):
                best = "match-under-a-named-rule"
        return best

    seen_counts = Counter(rec["population"] for rec in records)
    if dict(seen_counts) != EXPECTED_POPULATION_COUNTS:
        failures.append(
            f"evidence artifact holds {dict(seen_counts)}, expected "
            f"{EXPECTED_POPULATION_COUNTS}. content/quote-verification-audit.md §5 "
            f"claims its property over exactly 378 mismatches and §13 over exactly "
            f"16; a different population is a different claim."
        )
    for name, spec in populations.items():
        if spec["record_count"] != EXPECTED_POPULATION_COUNTS.get(name):
            failures.append(f"population {name!r} declares record_count "
                            f"{spec['record_count']}, expected "
                            f"{EXPECTED_POPULATION_COUNTS.get(name)}")

    for rec in records:
        where = f"{rec['file']} :: {rec['pointer']}"
        cohort = rec["cohort"]
        if "refreshed_value" in rec:
            refreshed += 1
            if not rec.get("refreshed_reason"):
                failures.append(f"{where}: refreshed_value with no refreshed_reason. "
                                f"A re-baselined string without a recorded reason is "
                                f"exactly the silent baseline this artifact forbids.")
        now = verdict_now(rec)
        verdicts.setdefault(cohort, Counter())[now] += 1
        if now != rec.get("verdict_on_this_tree"):
            verdict_drift.append(
                f"{where}: stored {rec.get('verdict_on_this_tree')!r}, "
                f"recomputed {now!r}"
            )
        needle = maximal_needle(rec["value"])
        if needle != rec["maximal_normalized"]:
            normalization_drift.append(
                f"{where}: frozen {rec['maximal_normalized']!r} != recomputed {needle!r}"
            )
            continue
        if not needle:
            failures.append(f"{where}: normalizes to the empty string")
            continue
        for cite in rec["cited"]:
            doc_id, anchor = cite["doc_id"], cite["anchor"]
            entry = index.get(doc_id)
            if entry is None or anchor not in entry["anchors"]:
                frozen_unresolved.append(f"{where}: {doc_id}#{anchor}")
                continue
            start, end = entry["anchors"][anchor]
            hay = maximal_haystack("\n".join(entry["lines"][start:end]))
            citations_tested += 1
            if needle in hay:
                moved.append(f"{where}: now contained in {doc_id}#{anchor}")

    print("=" * 78)
    print("ANTI-GOLDEN RE-MEASUREMENT - content/quote-verification-audit.md §5 and §13")
    print("=" * 78)
    print(f"  evidence artifact          : {EVIDENCE.relative_to(REPO_ROOT)}")
    print(f"  mismatch records           : {len(records)}"
          f"  = " + " + ".join(f"{n} {k}" for k, n in sorted(seen_counts.items())))
    print(f"  citations re-tested        : {citations_tested}")
    print(f"  docs/ sections indexed     : {sum(len(e['anchors']) for e in index.values())}"
          f" across {len(index)} document(s)")
    print(f"  records RE-BASELINED       : {refreshed}  (records carrying an explicit "
          f"refreshed_value + refreshed_reason)")
    for rec in records:
        if "refreshed_value" in rec:
            print(f"      re-baselined: {rec['file']} :: {rec['pointer']}")
            print(f"                    {rec.get('refreshed_reason', '*** NO REASON RECORDED ***')}")
    print()
    for name, spec in sorted(populations.items()):
        gate = spec["containment_gate"]
        print(f"  POPULATION {name} - {spec['record_count']} record(s): {spec['what_it_is']}")
        print(f"    containment gate: >= {gate['min_characters']} characters and "
              f">= {gate['min_words']} words")
        print("    where the string WAS found under maximal normalization (frozen):")
        for k, n in sorted(spec["located_breakdown"].items(), key=lambda kv: -kv[1]):
            print(f"      {n:5d}  {k}")
    print()
    print("  RE-DERIVED against content/ as it stands today, BY COHORT (adopted rules")
    print("  only, disjunctive over the equally-most-specific citations). Each line names")
    print("  the population it counts: the printed total used to merge the 248 re-pointing")
    print("  targets with a `nowhere` record that happened to read `exact`, and the")
    print("  coincidence masked a missing identity test.")
    for cohort in sorted(verdicts):
        print(f"    {cohort} ({sum(verdicts[cohort].values())} record(s)):")
        for k, n in sorted(verdicts[cohort].items(), key=lambda kv: -kv[1]):
            print(f"      {n:5d}  {k}")
        note = payload["cohort_notes"].get(cohort)
        if note:
            print(f"        {note}")
    print(f"  stored verdict_on_this_tree disagreements: {len(verdict_drift)}"
          f"   <- the WEAKEST line here: on the commit that generates the artifact this")
    print("       is 0 by construction, because the stored verdicts ARE this script's")
    print("       output at that commit. It detects drift after the fact; it is not")
    print("       evidence that any citation is correct. See the header.")
    print(f"  live value != frozen value: {len(value_divergence)}")
    print(f"  live values under their population's containment gate: {len(gate_violation)}")
    print()
    print(f"  normalized forms reproduced: {len(records) - len(normalization_drift)}"
          f"/{len(records)}")
    print(f"  FROZEN cited[] citations that did not resolve in docs/: "
          f"{len(frozen_unresolved)}")
    print("      (this line measures the artifact's frozen cited[] array ONLY - it says")
    print("       nothing about the live source_refs elements, which are gated by A9 in")
    print("       verify_content.py and counted on the next line)")
    print(f"  LIVE source_refs anchors that did not resolve in docs/: "
          f"{len(live_unresolved)}")
    print(f"  CASES THAT MOVE under maximal normalization: {len(moved)}")
    print()

    for bucket, label in (
        (normalization_drift, "NORMALIZED FORM DID NOT REPRODUCE"),
        (frozen_unresolved, "FROZEN cited[] CITATION DID NOT RESOLVE IN docs/"),
        (live_unresolved, "LIVE source_refs ANCHOR DID NOT RESOLVE IN docs/"),
        (value_divergence, "LIVE VALUE != FROZEN VALUE"),
        (gate_violation, "LIVE VALUE UNDER THE CONTAINMENT GATE"),
        (verdict_drift, "VERDICT DRIFTED - stored != re-derived from content/"),
        (moved, "CASE MOVED - a mismatch became a match"),
    ):
        for item in bucket[:20]:
            print(f"  {label}: {item}")
        if len(bucket) > 20:
            print(f"  ... and {len(bucket) - 20} more")

    for cohort, stored in payload["verdict_on_this_tree_by_cohort"].items():
        if dict(verdicts.get(cohort, Counter())) != stored:
            failures.append(
                f"cohort {cohort!r}: stored summary {stored} != re-derived "
                f"{dict(verdicts.get(cohort, Counter()))}"
            )
    for cohort in verdicts:
        if cohort not in payload["verdict_on_this_tree_by_cohort"]:
            failures.append(f"cohort {cohort!r} has no stored summary to check against")
    if refreshed != payload["refreshed_record_count"]:
        failures.append(
            f"{refreshed} record(s) carry refreshed_value, artifact declares "
            f"{payload['refreshed_record_count']}. Re-baselining is the one thing here "
            f"that must never happen quietly."
        )

    if value_divergence:
        print()
        print(
            "A LIVE VALUE DIVERGED FROM ITS FROZEN STRING. This is the test that makes "
            "the verdict mean anything: without it, `exact` says only that whatever "
            "string sits at that pointer now is a substring of the cited section, which "
            "a single character satisfies. Either the tree changed under the record - in "
            "which case verify the live string against its cited section and, if it is "
            "right, add an explicit refreshed_value and refreshed_reason for THAT record "
            "- or the tree is wrong. Do not widen the comparison."
        )
    bad = (failures + normalization_drift + frozen_unresolved + live_unresolved
           + value_divergence + gate_violation + verdict_drift + moved)
    if verdict_drift:
        print()
        print(
            "verdict_on_this_tree is the artifact's one statement about content/ TODAY, "
            "so it is re-derived rather than trusted. A disagreement means the tree moved "
            "under it - a string was edited, a field was renamed, or a citation was "
            "re-pointed - and the field must be regenerated, not reasoned about."
        )
    if moved:
        print()
        print(
            "AUDIT §5 NO LONGER HOLDS. The anti-golden claim is that no amount of "
            "loosening rescues any of these mismatches - that is what makes the four "
            "adopted rules a finite list rather than the first four steps of a slope. "
            "A case that moves means either a design document changed under one of "
            "these quotations, or the maximal normalization here has drifted. "
            "Re-measure §5; do not relax a rule to absorb it."
        )
    for f in failures:
        print(f"  FAIL: {f}")

    print()
    print("RESULT:", "FAIL" if bad else "ok - zero cases move, as §5 claims")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
