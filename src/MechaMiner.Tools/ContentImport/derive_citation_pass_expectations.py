#!/usr/bin/env python3
"""Derive — before the change is made — what a citation pass is expected to change.

    python3 src/MechaMiner.Tools/ContentImport/derive_citation_pass_expectations.py --verify
    python3 src/MechaMiner.Tools/ContentImport/derive_citation_pass_expectations.py --write

A BARE INVOCATION DECIDES NOTHING AND WRITES NOTHING - it exits 2 with a usage
error naming both verbs. Writing is `--write` and only `--write`. Until then bare
WAS the generator: it rewrote expected_citation_deltas.json and exited 0, so a
reader who guessed this tool's checking verb was `--check` (which is its sibling's
verb, one file over, for the same job) spent their guess on a silent rewrite of
the committed artifact rather than on an error message. Measured before the change,
on a clean tree at 3b4703b: bare exited 0 and left the artifact 31 insertions and
731 deletions away from what was committed. Nothing about which mode CHECKS moved
here - `--verify` still measures the pinned range exactly as it did, and the
sibling's `--check` is still spelled `--check`; only the cost of guessing wrong did.

================================================================================
WHY THIS FILE EXISTS, AND WHY IT IS COMMITTED BEFORE THE CHANGE
================================================================================

The previous pass on this branch claimed its string differences were "enumerated
before they were measured". That claim was not supportable. `git log -S` for both
the phrase and the `63 added, 2 removed` figure returns only `b482304`, the
branch's last commit, fourteen minutes AFTER the change itself landed in
`9c1a4e3` — and `9c1a4e3`'s own message cannot serve as the record, because that
commit CONTAINS the 59 citations, so its message could have been read off its own
diff. A prediction that only exists after the measurement is not a prediction.

So the practice this file establishes is mechanical rather than narrative:

  1. This script and its committed output land in a commit that touches ZERO
     files under `content/`. `git show <that commit> --stat` is itself the
     ordering proof: at that commit there is no content diff to fit the
     expectation to.
  2. The change lands in a SECOND commit.
  3. `--verify` measures the second commit against the first and asserts the
     measured delta equals the committed expectation, element by element. The
     "second commit" is PINNED as `PASS_REF`, not read as HEAD: what is asserted
     is a one-shot claim about this pass's range, and asking it of a later HEAD
     turns it into "nothing has changed since". See PASS_REF for the measurement
     that made that concrete, and for the two standing assertions that remain.

The expectation is derived from the frozen evidence artifact and from a live
sweep of the tree at a named git ref — never from a diff.

================================================================================
WHAT IS DERIVED
================================================================================

`previous_pass_59_pairs`
    The 59 `(file, scope)` pairs of the 248-quotation re-pointing pass, derived
    from `origin/master`'s `quote_mismatch_evidence.json` ALONE, then compared as
    a SET against the pairs actually present in the branch diff. Set equality
    over 59 elements — nothing derived-but-not-measured, nothing
    measured-but-not-derived — is a far stronger statement than "63 added and 2
    removed agreed", which is two integers matching and which two wrong sets can
    produce.

    The grouping key is stated exactly, because the recipe has to reproduce:
    `(file, pointer with every array index collapsed)`. Read literally,
    `(file, pointer)` gives 248 and `(file, existing citation scope)` gives 38;
    only the index collapse gives 65. Three notations of that same collapse
    (`[4]` -> `[]`, `[4]` -> ``, `[4]` -> `[*]`) induce the identical partition
    and all give 65 / 37 files / 6 already-correct / 59 new, so there is no
    tuning room at the subtraction; all-exact and any-exact both give 6.

`sweep_16`
    The mis-citations the frozen 378-record artifact CANNOT see, found by a live
    sweep of every prose leaf in the tree at `sweep_ref`, and the target section
    proposed for each. See `content/quote-verification-audit.md` §13.

`expected_string_delta` / `expected_numeric_delta`
    The exact multiset difference over `content/**/*.json` string and numeric
    leaves that applying `sweep_16`'s targets must produce, and nothing else.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[2]
OUTPUT = HERE / "expected_citation_deltas.json"

sys.path.insert(0, str(HERE))
import check_quote_mismatch_evidence as Q  # noqa: E402

PREVIOUS_PASS_REF = "origin/master"

# THE COMMIT THAT LANDED THIS PASS - the "after" side of the range `--verify`
# measures, and PINNED for the reason below.
#
# WHY THIS EXISTS (it was HEAD, and that made the tool red at baseline). `--verify`
# used to measure `sweep_ref -> HEAD`, which is right on the day the pass lands and
# wrong forever after: every row here is a ONE-SHOT CLAIM ABOUT ONE PASS's RANGE -
# "applying sweep_16's 13 targets adds exactly these 13 source_refs elements and
# these 13 string leaves, and moves NO number at all". Pointed at a later HEAD it
# stops asserting that and starts asserting "nothing has changed since", which no
# branch can satisfy and which nothing here ever claimed. Measured on this branch:
# the numeric row is unmoved at 3b0a55a (the pass), fefb7a3 (the PR #8 merge) and
# fcde187, and moves first at 19dcf42 - the derived-value removal commit of the NEXT
# pull request, which removes 166 authored numeric leaves and later restores 51, net
# 115. So `--verify` exited 1 with "numeric multiset moved: ... removed {...}" at
# every head of that branch, for a diff it was never a claim about.
#
# This is the same defect that removed A29's rows 2 and 3 this round ("0 numeric
# leaves added", "0 surviving numeric leaves changed value") - a one-shot property of
# one commit range wired into a standing check - and it was left standing here, in a
# tool one pull request older, because a fix pass looks at what it is fixing.
#
# WHY PINNING RATHER THAN DELETING. A29's two rows had no range that made them true:
# they were properties of a diff, and re-pinning their ref would have made A29
# compare the tree against itself. This row does have one. The claim "the citation
# pass changed strings and no numbers" is TRUE, checkable, and worth keeping
# checkable: pinned to `sweep_ref -> PASS_REF` it re-derives the 13 pairs from the
# frozen artifact and measures the actual delta, so it still fails if the expectation
# file is edited, if the derivation drifts, or if the pass's commits leave HEAD's
# history (asserted below). What it is NOT, after the pin, is a statement about the
# current tree - and it never was. A29/A31 in verify_content.py are what police that.
PASS_REF = "3b0a55a0db57d47ed0ea14abebb5ba5dd702da28"

# The candidate gate of content/quote-verification-audit.md section 2, and the
# WORD half of its decidability gate. The 40-character half is deliberately
# dropped for this sweep and the choice is recorded rather than buried: at
# >= 6 words the sweep finds 145 leaves absent from their cited section (129 of
# them the frozen artifact's own no-match records, 16 outside it); at >= 40
# characters as well it finds 129 and NOTHING outside the artifact, which is
# exactly the blind spot being closed.
MIN_WORDS = 6
ALL_CAPS = re.compile(r"^[A-Z0-9][A-Z0-9 _\-./%+]*$")


def sh(*args: str) -> str:
    return subprocess.run(args, cwd=REPO_ROOT, capture_output=True, text=True,
                          check=True).stdout


def ancestor(older: str, newer: str) -> bool:
    return subprocess.run(
        ["git", "merge-base", "--is-ancestor", older, newer],
        cwd=REPO_ROOT, capture_output=True, text=True,
    ).returncode == 0


def resolve(ref: str) -> str:
    return sh("git", "rev-parse", ref).strip()


def materialize(ref: str, into: Path) -> Path:
    """Extract `ref`'s content/ and docs/ into `into`. Read-only, no worktree."""
    into.mkdir(parents=True, exist_ok=True)
    tar = subprocess.run(["git", "archive", ref, "content", "docs"],
                         cwd=REPO_ROOT, capture_output=True, check=True).stdout
    subprocess.run(["tar", "-x", "-C", str(into)], input=tar, check=True)
    return into


def read_json_at(ref: str, path: str):
    return json.loads(sh("git", "show", f"{ref}:{path}"))


# --------------------------------------------------------------------------
# the previous pass: 59 (file, scope) pairs from master's artifact alone
# --------------------------------------------------------------------------

COLLAPSES = {
    "[n] -> []": lambda p: re.sub(r"\[\d+\]", "[]", p),
    "[n] -> (removed)": lambda p: re.sub(r"\[\d+\]", "", p),
    "[n] -> [*]": lambda p: re.sub(r"\[\d+\]", "[*]", p),
}


def derive_previous_pass(ref: str) -> dict:
    payload = read_json_at(ref, "src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json")
    located = [r for r in payload["records"] if r["located"] != "nowhere"]
    variants = {}
    for name, fn in COLLAPSES.items():
        groups: dict[tuple[str, str], list] = {}
        for rec in located:
            groups.setdefault((rec["file"], fn(rec["pointer"])), []).append(rec)
        all_exact = {k for k, v in groups.items()
                     if all(r["verdict_on_this_tree"] == "exact" for r in v)}
        any_exact = {k for k, v in groups.items()
                     if any(r["verdict_on_this_tree"] == "exact" for r in v)}
        variants[name] = dict(
            groups=len(groups), files=len({k[0] for k in groups}),
            already_correct_all_exact=len(all_exact),
            already_correct_any_exact=len(any_exact),
            new=len(groups) - len(all_exact),
            new_files=len({k[0] for k in groups if k not in all_exact}),
            pairs=sorted(f"{f} :: {s}" for f, s in groups if (f, s) not in all_exact),
        )
    canonical = variants["[n] -> []"]
    return dict(
        derived_from=ref,
        located_somewhere_records=len(located),
        grouping_key="(file, pointer with every array index collapsed)",
        literal_readings_that_do_not_reproduce={
            "(file, pointer)": 248,
            "(file, existing citation scope)": 38,
        },
        no_collapse_rule=(
            "The two content/enemies/EN-06.json groups "
            "(specialist_attack.projectile.lifetime_description and "
            "specialist_attack.resonance_interactions.flux_amber) share a target section and "
            "would collapse to one specialist_attack: element, giving 64 groups and 58 new. "
            "They are kept separate because that collapse SHADOWS "
            "specialist_attack.projectile.lifetime_description behind the pre-existing "
            "specialist_attack.projectile: TDD-ENCOUNTERS#needler element, which is more "
            "specific than a bare specialist_attack: prefix. Measured by performing the "
            "collapse and running check_quote_mismatch_evidence.py: disagreements: 1, "
            "'EN-06 :: specialist_attack.projectile.lifetime_description: stored exact, "
            "recomputed no-match', RESULT: FAIL. hard_control_interaction is NOT the field "
            "at risk - it carries its own equally specific element and is unaffected."
        ),
        variants=variants,
        pairs=canonical["pairs"],
    )


# --------------------------------------------------------------------------
# the live sweep
# --------------------------------------------------------------------------

def walk_strings(node, path, name, out):
    if isinstance(node, dict):
        for key, value in node.items():
            walk_strings(value, f"{path}.{key}" if path else key, key, out)
    elif isinstance(node, list):
        for i, value in enumerate(node):
            walk_strings(value, f"{path}[{i}]", name, out)
    elif isinstance(node, str):
        out.append((path, name, node))


def is_candidate(name: str, value: str) -> bool:
    if name in ("source_refs", "tags") or name == "id":
        return False
    if name.endswith("_key") or name.endswith("_id"):
        return False
    return not ALL_CAPS.match(value.strip())


def sweep(tree: Path) -> list[dict]:
    """Every prose leaf whose equally-most-specific ANCHORED citation does not
    contain it, under the four adopted rules."""
    index = Q.build_doc_index(tree / "docs")
    cache: dict = {}

    def section(doc_id: str, anchor: str):
        key = (doc_id, anchor)
        if key not in cache:
            entry = index.get(doc_id)
            if entry is None or anchor not in entry["anchors"]:
                cache[key] = None
            else:
                start, end = entry["anchors"][anchor]
                cache[key] = Q.adopted("\n".join(entry["lines"][start:end]))
        return cache[key]

    absent = []
    for path in sorted((tree / "content").rglob("*.json")):
        rel = path.relative_to(tree).as_posix()
        doc = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(doc, dict) or "source_refs" not in doc:
            continue                                    # localization/en.json
        leaves: list = []
        walk_strings(doc, "", "", leaves)
        for pointer, name, value in leaves:
            if pointer.startswith("source_refs") or not is_candidate(name, value):
                continue
            needle = Q.adopted(value)
            if len(needle.split()) < MIN_WORDS:
                continue
            cites = [c for c in Q.covering_citations(doc.get("source_refs", []), pointer)
                     if "#" in c]
            if not cites:
                continue                                # citation too coarse to check
            forms = Q.adopted_variants(value)
            verdict, resolved = "no-match", False
            for cite in cites:
                hay = section(*cite.split("#", 1))
                if hay is None:
                    continue
                resolved = True
                if needle in hay:
                    verdict = "exact"
                    break
                if any(f in hay for f in forms):
                    verdict = "match-under-a-named-rule"
            if resolved and verdict == "no-match":
                absent.append(dict(file=rel, pointer=pointer, field=name,
                                   value=value, cited=cites))
    return absent


def locate(tree: Path, rec: dict, own_docs: set[str]) -> list[dict]:
    """Every docs/ section containing the value, ranked by Ruling 36's method:
    a document the file already cites, then the deepest heading, then the
    smallest span."""
    index = Q.build_doc_index(tree / "docs")
    needle, forms = Q.adopted(rec["value"]), Q.adopted_variants(rec["value"])
    hits = []
    for doc_id, entry in index.items():
        for anchor, (start, end) in entry["anchors"].items():
            hay = Q.adopted("\n".join(entry["lines"][start:end]))
            if needle in hay:
                kind = "exact"
            elif any(f in hay for f in forms):
                kind = "match-under-a-named-rule"
            else:
                continue
            level = entry["lines"][start].split(" ", 1)[0].count("#")
            hits.append(dict(cited_document=doc_id in own_docs, heading_level=level,
                             span=end - start, target=f"{doc_id}#{anchor}", kind=kind,
                             where=f"{entry['path'].name}:{start + 1}-{end}"))
    hits.sort(key=lambda h: (not h["cited_document"], -h["heading_level"], h["span"]))
    return hits


# --------------------------------------------------------------------------
# leaf multisets, for the value-preservation proofs
# --------------------------------------------------------------------------

def leaf_multisets(tree: Path) -> tuple[Counter, Counter]:
    strings: Counter = Counter()
    numbers: Counter = Counter()
    for path in sorted((tree / "content").rglob("*.json")):
        rel = path.relative_to(tree).as_posix()
        doc = json.loads(path.read_text(encoding="utf-8"))

        def rec(node, ptr):
            if isinstance(node, dict):
                for k, v in node.items():
                    rec(v, f"{ptr}.{k}" if ptr else k)
            elif isinstance(node, list):
                for i, v in enumerate(node):
                    rec(v, f"{ptr}[{i}]")
            elif isinstance(node, bool):
                pass
            elif isinstance(node, str):
                strings[node] += 1
            elif isinstance(node, (int, float)):
                numbers[node] += 1
        rec(doc, "")
        del rel
    return strings, numbers


def multiset_delta(before: Counter, after: Counter) -> dict:
    """Multiset difference, kept as {value: multiplicity} in BOTH directions.

    Multiplicity is retained on purpose: six of this pass's thirteen new elements
    are the identical string, so collapsing to distinct values would turn a
    13-element expectation into a 5-element one and stop discriminating.
    """
    return dict(before_total=sum(before.values()), after_total=sum(after.values()),
                added=dict(after - before), removed=dict(before - after))


# --------------------------------------------------------------------------

def build(sweep_ref: str, previous_ref: str) -> dict:
    sweep_sha, prev_sha = resolve(sweep_ref), resolve(previous_ref)
    with tempfile.TemporaryDirectory() as tmp:
        tree = materialize(sweep_sha, Path(tmp) / "sweep")
        found = sweep(tree)
        frozen = json.loads(
            (REPO_ROOT / "src/MechaMiner.Tools/ContentImport/quote_mismatch_evidence.json")
            .read_text(encoding="utf-8"))
        known = {(r["file"], r["pointer"]) for r in frozen["records"]}
        outside = [r for r in found if (r["file"], r["pointer"]) not in known]
        for rec in outside:
            doc = json.loads((tree / rec["file"]).read_text(encoding="utf-8"))
            own = {ref.split(": ", 1)[-1].split("#", 1)[0] for ref in doc["source_refs"]}
            hits = locate(tree, rec, own)
            rec["sections_containing_the_value"] = hits[:4]
            if hits:
                rec["classification"] = "mis-citation - re-point"
                rec["target"] = hits[0]["target"]
                scope = rec["pointer"]
                rec["expected_new_source_refs_element"] = f"{scope}: {hits[0]['target']}"
            else:
                rec["classification"] = "NOT a mis-citation - value is not a verbatim quotation"
                rec["target"] = None
                rec["expected_new_source_refs_element"] = None
        expected_added = sorted(r["expected_new_source_refs_element"]
                                for r in outside if r["target"])
        return dict(
            schema="citation-pass-expectations/1",
            sweep_ref=sweep_sha,
            previous_pass_ref=prev_sha,
            min_words=MIN_WORDS,
            sweep_totals=dict(
                absent_from_cited_section=len(found),
                inside_the_frozen_378=len(found) - len(outside),
                outside_the_frozen_378=len(outside),
            ),
            previous_pass_59_pairs=derive_previous_pass(prev_sha),
            sweep_16=outside,
            expected_string_delta=dict(added=expected_added, removed=[]),
            expected_numeric_delta=dict(added=[], removed=[]),
        )


def scoped_elements(ref: str) -> dict[str, Counter]:
    """Every source_refs element in the tree at `ref`, keyed by file."""
    out: dict[str, Counter] = {}
    for rel in sh("git", "ls-tree", "-r", "--name-only", ref, "--", "content").split():
        if not rel.endswith(".json"):
            continue
        doc = read_json_at(ref, rel)
        if isinstance(doc, dict) and "source_refs" in doc:
            out[rel] = Counter(doc["source_refs"])
    return out


def element_delta(before_ref: str, after_ref: str) -> tuple[set, set]:
    """`(file, scope)` pairs added, and whole elements removed, between two refs."""
    before, after = scoped_elements(before_ref), scoped_elements(after_ref)
    added, removed = set(), set()
    for rel in set(before) | set(after):
        b, a = before.get(rel, Counter()), after.get(rel, Counter())
        for element in (a - b).elements():
            scope = element.split(": ", 1)[0] if ": " in element else "<file-level>"
            added.add(f"{rel} :: {scope}")
        for element in (b - a).elements():
            removed.add(f"{rel} :: {element}")
    return added, removed


def report_set_equality(label: str, derived: set, measured: set,
                        removed: set, failures: list) -> None:
    print(f"  {label}")
    print(f"    derived (from the artifact / the sweep, never from a diff): {len(derived)}")
    print(f"    measured (present in the diff)                            : {len(measured)}")
    for name, gap in (("derived but NOT measured", derived - measured),
                      ("measured but NOT derived", measured - derived)):
        print(f"    {name}: {len(gap)}")
        for item in sorted(gap):
            failures.append(f"{label}: {name}: {item}")
    print(f"    citations DELETED: {len(removed)}")
    for item in sorted(removed):
        failures.append(f"{label}: citation deleted: {item}")
    print(f"    SET EQUALITY over {len(derived)} elements: "
          f"{'YES' if derived == measured else 'NO'}")


def verify(expected: dict, after_ref: str = PASS_REF) -> int:
    """Measure the pass's own range and assert the delta equals the prediction.

    The range is `sweep_ref -> after_ref`, and `after_ref` defaults to the PINNED
    PASS_REF rather than to HEAD. See PASS_REF's comment for why: every row here is a
    one-shot claim about one pass, so measuring to HEAD turns it into "nothing has
    changed since" and makes this tool red at every later head.
    """
    failures = []
    after_sha = resolve(after_ref)
    pinned = after_sha == resolve(PASS_REF)

    # STANDING, unlike the delta rows: the pass's commits must still be in HEAD's
    # history. Pinning a range is only honest while the range is still reachable -
    # if the pass is rebased away or dropped, this measurement is of history that
    # the branch no longer contains and has to fail rather than keep passing.
    if not ancestor(expected["sweep_ref"], after_sha):
        failures.append(
            f"{expected['sweep_ref'][:7]} is not an ancestor of the measured after-ref "
            f"{after_sha[:7]}, so the range is not a range"
        )
    if not ancestor(after_sha, resolve("HEAD")):
        failures.append(
            f"the measured after-ref {after_sha[:7]} is not an ancestor of HEAD: the citation "
            f"pass this file predicts is no longer in this branch's history, so the pinned range "
            f"measures history the tree no longer contains. Re-pin PASS_REF deliberately, or "
            f"delete this expectation with the pass it belonged to."
        )

    with tempfile.TemporaryDirectory() as tmp:
        before = materialize(expected["sweep_ref"], Path(tmp) / "before")
        after = materialize(after_sha, Path(tmp) / "after")
        bs, bn = leaf_multisets(before)
        as_, an = leaf_multisets(after)
        sd = multiset_delta(bs, as_)
        nd = multiset_delta(bn, an)

    print("=" * 78)
    print("CITATION PASS EXPECTATION vs MEASUREMENT")
    print("=" * 78)
    print(f"  expectation committed at   : {expected['sweep_ref'][:7]} "
          f"(zero content/ files in that commit)")
    print(f"  measured                   : {expected['sweep_ref'][:7]} -> {after_sha[:7]}"
          f"{'  [PINNED to the pass that landed it]' if pinned else '  [OVERRIDDEN via --after-ref]'}")
    print(f"  HEAD is                    : {resolve('HEAD')[:7]}"
          f"{'' if pinned else ' - rows below are NOT claims about it'}")
    print("  WHAT THIS VERB IS: a reproduction of ONE citation pass over ITS OWN range - the 13")
    print("  (file, scope) re-pointings, the 13 string leaves they add, and the zero numbers they")
    print("  move. It is NOT a check on the current tree, and it never was: pointed at HEAD it")
    print("  reads as 'nothing has changed since', which the next pull request's 115 numeric")
    print("  removals falsify by design. A29/A31 in verify_content.py police the current tree.")
    print()
    this_derived = {f"{r['file']} :: {r['pointer']}"
                    for r in expected["sweep_16"] if r["target"]}
    this_added, this_removed = element_delta(expected["sweep_ref"], after_sha)
    report_set_equality("THIS pass's new (file, scope) pairs",
                        this_derived, this_added, this_removed, failures)
    print()
    prev_derived = set(expected["previous_pass_59_pairs"]["pairs"])
    prev_added, prev_removed = element_delta(expected["previous_pass_ref"],
                                             expected["sweep_ref"])
    report_set_equality("the PREVIOUS pass's 59 pairs, re-derived from "
                        f"{expected['previous_pass_ref'][:7]}'s frozen artifact alone",
                        prev_derived, prev_added, prev_removed, failures)
    print()
    exp_add = Counter(expected["expected_string_delta"]["added"])
    exp_rm = Counter(expected["expected_string_delta"]["removed"])
    got_add, got_rm = Counter(sd["added"]), Counter(sd["removed"])
    print("  STRING multiset over every string leaf of content/**/*.json")
    print(f"    leaves    : {sd['before_total']} -> {sd['after_total']}")
    print(f"    added     : {sum(got_add.values())} (expected {sum(exp_add.values())})")
    print(f"    removed   : {sum(got_rm.values())} (expected {sum(exp_rm.values())})")
    for name, gap in (("derived but NOT measured", exp_add - got_add),
                      ("measured but NOT derived", got_add - exp_add),
                      ("expected removal not measured", exp_rm - got_rm),
                      ("measured removal not expected", got_rm - exp_rm)):
        for value, n in sorted(gap.items()):
            failures.append(f"string {name}: {value!r} x{n}")
    print(f"    SET EQUALITY over {sum(exp_add.values())} added elements "
          f"(multiplicity kept): {'YES' if exp_add == got_add else 'NO'}")
    print()
    print("  NUMERIC multiset over every int/float leaf (bool excluded)")
    print(f"    leaves    : {nd['before_total']} -> {nd['after_total']}")
    print(f"    added     : {nd['added'] or '{}'}")
    print(f"    removed   : {nd['removed'] or '{}'}")
    if nd["added"] or nd["removed"]:
        failures.append(
            f"numeric multiset moved over {expected['sweep_ref'][:7]} -> {after_sha[:7]}: "
            f"added {nd['added']} removed {nd['removed']}"
            + (
                ""
                if pinned
                else f". WHY THIS FAILED, and it is very likely not a defect in the tree: this row "
                f"is a ONE-SHOT claim about the citation pass's own range (sweep_ref -> PASS_REF "
                f"{PASS_REF}), namely that the pass moved no numbers. You have pointed it at "
                f"{after_sha[:7]} instead, so it is now asserting that NOTHING has moved a number "
                f"since - which any later pass legitimately falsifies (the derived-value removal "
                f"pass moves 115). Drop --after-ref to measure the pinned range."
            )
        )
    print()
    print("  Scope of each proof, named: the numeric multiset covers int/float leaves and")
    print("  nothing else; the string multiset covers string leaves and nothing else. The")
    print("  three Markdown files under content/ are prose and are in neither. The evidence")
    print("  artifact under src/ is a measurement, not a value store, and is deliberately")
    print("  excluded - its own bookkeeping strings change by construction on every pass.")
    print(f"  RANGE, which is the other half of every scope above: {expected['sweep_ref'][:7]} ->")
    print(f"  {after_sha[:7]}, four rows, all four ONE-SHOT claims about that range and none of")
    print("  them standing claims about HEAD. The two STANDING assertions here are the ancestry")
    print("  ones: the range must be a range, and the pass must still be in HEAD's history.")
    print("  NOT CHECKED, and this tool has no verb for it: that expected_citation_deltas.json")
    print("  still REGENERATES from its inputs. It cannot, and that is a property of the inputs")
    print("  rather than a missing flag - `build` reads previous_pass_ref as the moving")
    print("  `origin/master` and reads quote_mismatch_evidence.json from the WORKTREE, and both")
    print("  have moved since (regenerating today gives outside_the_frozen_378 = 0, because the")
    print("  artifact now contains the 16). The prediction's integrity therefore rests on git")
    print(f"  history - the file is committed with ZERO content/ files in it - not on a byte")
    print("  compare. `derive_derived_value_expectations.py --check` has that verb because its")
    print("  inputs are a pinned ref only.")
    print()
    for f in failures:
        print(f"  FAIL: {f}")
    print("RESULT:", "FAIL" if failures else "ok - measured delta equals the committed expectation")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--ref", default="HEAD",
                    help="tree the live sweep runs against (the pre-change tree)")
    ap.add_argument("--previous-ref", default=PREVIOUS_PASS_REF)
    ap.add_argument("--verify", action="store_true",
                    help="measure the pass's own pinned range against the committed expectation")
    ap.add_argument("--write", action="store_true",
                    help=f"derive the expectation afresh and REWRITE {OUTPUT.name}. The only mode "
                         f"that touches the worktree, and it must be asked for by name: this is "
                         f"what a bare invocation used to do silently, at exit 0")
    ap.add_argument("--after-ref", default=PASS_REF,
                    help=f"the 'after' side of the measured range (default the pinned {PASS_REF}, "
                         f"the commit that landed the pass; passing HEAD asks a one-shot claim to "
                         f"hold over every commit since, which it will not)")
    args = ap.parse_args()

    if args.verify:
        if not OUTPUT.exists():
            print(f"FAIL: no committed expectation at {OUTPUT}")
            return 1
        return verify(json.loads(OUTPUT.read_text(encoding="utf-8")), args.after_ref)

    if not args.write:
        # Checked BEFORE build() so the usage error costs nothing but the message.
        # `--verify` is handled above and is untouched by this guard; what used to
        # fall through to here was a bare invocation, which rewrote OUTPUT.
        ap.error(
            f"no mode given. This tool does not default to writing: pass --verify to measure the "
            f"pinned range against the committed {OUTPUT.name}, or --write to regenerate it. "
            f"Note that --verify is this tool's checking verb; "
            f"derive_derived_value_expectations.py spells the same job --check."
        )

    payload = build(args.ref, args.previous_ref)
    OUTPUT.write_text(json.dumps(payload, indent=1, ensure_ascii=False) + "\n",
                      encoding="utf-8")
    prev = payload["previous_pass_59_pairs"]
    print(f"sweep_ref              : {payload['sweep_ref'][:7]}")
    print(f"absent from cited      : {payload['sweep_totals']}")
    print(f"previous pass grouping : {prev['grouping_key']}")
    for name, v in prev["variants"].items():
        print(f"   {name:20s} groups {v['groups']} files {v['files']} "
              f"already {v['already_correct_all_exact']} new {v['new']} "
              f"files {v['new_files']}")
    fixable = [r for r in payload["sweep_16"] if r["target"]]
    print(f"sweep_16               : {len(payload['sweep_16'])} found, "
          f"{len(fixable)} re-pointable, {len(payload['sweep_16']) - len(fixable)} not")
    print(f"expected string delta  : {len(payload['expected_string_delta']['added'])} added, "
          f"{len(payload['expected_string_delta']['removed'])} removed")
    print(f"written                : {OUTPUT.relative_to(REPO_ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
