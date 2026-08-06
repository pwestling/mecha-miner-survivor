#!/usr/bin/env python3
"""Enumerate the authored derived values this pass removes, BEFORE removing them.

WHY THIS SCRIPT EXISTS AND WHY IT IS COMMITTED FIRST
====================================================
This pass deletes stored numbers from content/. A verifier written after the
deletion can be shaped to whatever the diff happens to contain, so it proves
nothing about whether the deletion was the intended one. So the order is fixed:

  commit 1  this script + the expectation file it writes, touching ZERO files
            under content/. `git show <commit 1> --stat` is the ordering proof:
            at that commit there is no diff to fit the expectation to.
  commit 2  the removals, plus A28/A29 in verify_content.py, which measure the
            numeric multiset the tree actually lost and assert SET EQUALITY with
            the committed expectation - per element, not per count.

EVERY CANDIDATE IS READ FROM A PINNED REF, NOT FROM `HEAD`
==========================================================
SWEEP_REF below is an explicit commit SHA, never `HEAD` and never a branch name.
The expectation file therefore regenerates byte-identically from this script at
any later commit, including after the removals land - which is what makes it
usable as a check rather than as a snapshot of whatever the tree currently says.

WHAT QUALIFIES AS A CANDIDATE
=============================
A stored number is a candidate only when all four hold:

  1. It reproduces EXACTLY from operands, computed in exact rational arithmetic
     (fractions.Fraction), never in binary floating point. A stored value that
     disagrees with its operands is a DEFECT, not a redundancy: this script
     exits non-zero and names both numbers rather than removing it.
  2. Every operand SURVIVES the removal. An operand is never removed. Where a
     value is both derived and an operand, the operand role wins and it stays -
     see RETAINED_BECAUSE_OPERAND.
  3. A line in docs/ assigns the derivation to the compiler. The citation is
     recorded per family and is part of the committed expectation.
  4. Removing it dangles no `source_refs` scope prefix (A22). Every family below
     removes leaves from inside an object that survives, or removes a whole
     object no prefix names.

Candidates that reproduce exactly but fail (3) are NOT removed. They are listed
in NOT_ASSIGNED_BY_DOCS and printed, because "the arithmetic works" is not
authority to delete an authored number.
"""

from __future__ import annotations

import argparse
import json
import math
import re
import subprocess
import sys
from fractions import Fraction
from pathlib import Path

# --------------------------------------------------------------------------
# The pinned sweep ref. NOT `HEAD`, NOT a branch name - a commit SHA, so this
# file regenerates byte-identically from this script forever.
#
# fefb7a359041c1275f6b9739437458443bdfecf1 is the merge of PR #8 into master,
# i.e. the tree this pass starts from.
# --------------------------------------------------------------------------
SWEEP_REF = "fefb7a359041c1275f6b9739437458443bdfecf1"

# Declared separately from what the builders produce, and asserted against it.
# `total_removed` and `family_count` used to be written here and read by nothing,
# which meant a family could be dropped, or fourteen values could stop being
# removed, with every number in the file updating itself to agree. These two are
# the independent statement of intent that the build must match.
#
# 115 across 6, not the 166 across 9 an earlier draft of this pass claimed:
# 166 - 14 (stat price curve, pulled) - 32 (damage-pressure block, pulled)
#     -  5 (resonant hit counts, pulled) = 115, and 9 - 3 = 6 families.
# See PULLED_FROM_THIS_PASS for why each was pulled.
DECLARED_FAMILY_COUNT = 6
DECLARED_TOTAL_REMOVED = 115

REPO = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent / "expected_derived_value_removals.json"

# --------------------------------------------------------------------------
# Shared authored constants that live elsewhere in the tree and must SURVIVE,
# because the removed values are derived from them.
# --------------------------------------------------------------------------
MECH_BASE_SPEED = "content/maps/standard-map-generation-contract.json::reference_mech_speed_m_per_s"
DAMAGE_PRESSURE_HULL = 100  # stated in every damage_pressure.assumptions string, which stays

RETAINED_BECAUSE_OPERAND = {
    "content/powerups/*::total_cost_hyper_gold": (
        "Derived (it is the last cumulative cost, and the sum of the rank prices) but RETAINED. "
        "It carries its own citation - `total_cost_hyper_gold: DEC-120#decision` - and it is the "
        "operand A14's second row checks when it sums the 13 per-entry totals to the doc-stated "
        "9,450. Removing it would delete an assertion's input to delete a redundancy."
    ),
    "content/enemies|bosses/*::resonant_damage_reference.resonant_damage": (
        "Reproduces for all 5 as ceil(base_damage x 1.20), and RETAINED because it is the operand "
        "the removed fresh_mech_hits_to_defeat_at_resonant_value derives from. CORRECTION to this "
        "note's earlier claim that 'no line in docs/ states the rounding': the rounding IS stated. "
        "docs/72 section 'Incoming Damage Resolution' step 3 says 'Round the result up to the next whole "
        "Hull point', immediately after step 2 'Multiply listed base damage by current "
        "attacker-side damage modifiers' - that is exactly ceil(base x modifier). The same section "
        "shows it "
        "worked (38 x 1.20 = 45.6, rounded up to 46) and docs/61 section 'Geode resonance behavior' "
        "states the 20% modifier. ceil "
        "is also the ONLY rounding that reproduces all five: 32.4 -> 33 and 43.2 -> 44 rule out "
        "both floor and round-half. So resonant_damage is fully documented arithmetic, and whether "
        "it is removable turns only on whether a docs/ line assigns ITS recomputation to the "
        "compiler - not on a missing rounding clause."
    ),
    "content/mining-sites/*::abundance_states[].geodes_on_map": (
        "Authored survey states (Scarce 8 / Moderate 9 / Rich 10) and the operand range the removed "
        "geodes_per_standard_map bounds are built from. Retained."
    ),
    "content/enemies|bosses/*::movement_speed.percent_of_mech_base_speed.percent": (
        "The authored half of the world speed. Retained - it is the operand."
    ),
    "content/utilities/*::acquisition.rank_ore_costs[]": (
        "The authored per-rank ore prices. Retained - they are the operands of the removed total."
    ),
    "content/powerups/*::ranks[].price_hyper_gold": (
        "The authored per-rank prices. Retained - they are the operands of both the removed "
        "cumulative costs and A14's first row."
    ),
}

# --------------------------------------------------------------------------
# Reproduces exactly, but NO line in docs/ assigns the derivation to the
# compiler - so these are NOT this pass's to remove. Printed, never deleted.
# --------------------------------------------------------------------------
NOT_ASSIGNED_BY_DOCS = [
    (
        "content/mining-sites/hyper-gold-sites.json",
        "beacon_thresholds[].at_uninterrupted_seconds_from_zero",
        "11.25 / 22.5 / 33.75 = progress percent x the authored 45 s extraction duration",
        "40:140 lists 'beacon thresholds' among the AUTHORED fields of a mining site and assigns "
        "the compiler only 'their totals'. Threshold timing is not a total and no other line "
        "claims it.",
    ),
    (
        "content/resources/common-ore.json",
        "sources[].depletion_seconds",
        "15 = installment_seconds x installment_count in both seam rows",
        "40:203 gives the compiler 'resource totals'; a depletion duration is not a total, and "
        "40:106's resource-field list does not mention it. The identically-derived "
        "total_depletion_seconds in content/mining-sites/ IS removed, because 40:140 names the "
        "site classes' totals explicitly.",
    ),
    (
        "content/enemies|bosses/*",
        "resonant_damage_reference.resonant_damage",
        "17 / 44 / 33 / 22 / 42 = ceil(base_damage x 1.20)",
        "The 1.20 comes from the geode resonance field's 20%, but no doc line documents the "
        "rounding, and 40:203 tolerates divergence only 'beyond documented rounding'. Also an "
        "operand - see RETAINED_BECAUSE_OPERAND.",
    ),
    (
        "content/bosses/BOSS-01.json",
        "ability.ordinary_contact_damage_replaced_during_charge",
        "18 = the definition's own top-level contact_damage",
        "It restates WHICH value the charge replaces rather than computing one, and no doc line "
        "assigns it. Left authored deliberately, unlike the boss damage_pressure.contact_damage "
        "duplicate, which sits inside the derived survivability block itself.",
    ),
    (
        "content/weapons/*",
        "damage_model.{burst_10_dps, sustained_30_dps, favorable_horde_dps}",
        "45 values; W-AB reproduces as 96/3.0 = 32.0, ceil(10/3.0) x 96/10 = 38.4, 32.0 x 4 pierce "
        "= 128",
        "40:203 DOES assign 'DPS estimates' to the compiler, so the doc test passes - but the "
        "burst/horde rule varies with each weapon's behaviour kind and this pass could not state "
        "ONE rule that reproduces all 45 exactly. Criterion 1 is therefore unmet and the family "
        "is deliberately out of scope, not cleared.",
    ),
    (
        "content/mining-sites/rich-ore-seams.json",
        "relative_to_standard_seam.{common_ore_per_second_multiplier, total_seam_payout_multiplier,"
        " exposure_per_secured_payout_multiplier}",
        "all three are 2; the first two reproduce against standard-ore-seams.json",
        "exposure_per_secured_payout_multiplier has no stated derivation, and the comparison "
        "operands live in a different file. Left authored.",
    ),
]


# --------------------------------------------------------------------------
# reading the pinned tree
# --------------------------------------------------------------------------


def git_show(ref: str, path: str) -> str:
    return subprocess.run(
        ["git", "-C", str(REPO), "show", f"{ref}:{path}"],
        check=True,
        capture_output=True,
        text=True,
    ).stdout


def git_ls(ref: str, prefix: str) -> list[str]:
    out = subprocess.run(
        ["git", "-C", str(REPO), "ls-tree", "-r", "--name-only", ref, prefix],
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    return sorted(p for p in out.splitlines() if p.endswith(".json"))


class Tree:
    def __init__(self, ref: str) -> None:
        self.ref = ref
        self._cache: dict[str, object] = {}

    def load(self, path: str) -> object:
        if path not in self._cache:
            self._cache[path] = json.loads(git_show(self.ref, path))
        return self._cache[path]

    def files(self, prefix: str) -> list[str]:
        return git_ls(self.ref, prefix)


def exact(value) -> Fraction:
    """Exact rational value of a JSON number - via str(), never via binary float."""
    return Fraction(str(value))


def fmt(value: Fraction | int | float) -> str:
    if isinstance(value, Fraction):
        if value.denominator == 1:
            return str(value.numerator)
        return f"{float(value):g}"
    return f"{value:g}" if isinstance(value, float) else str(value)


def at(doc, pointer: str):
    """Resolve a dotted pointer with [i] indices."""
    node = doc
    for seg in re.findall(r"[^.\[\]]+|\[\d+\]", pointer):
        if seg.startswith("["):
            node = node[int(seg[1:-1])]
        else:
            node = node[seg]
    return node


def numeric_leaves(obj, prefix: str = ""):
    """Yield (pointer, value) for every numeric leaf, bools excluded."""
    if isinstance(obj, dict):
        for key, value in obj.items():
            child = f"{prefix}.{key}" if prefix else key
            if isinstance(value, bool):
                continue
            if isinstance(value, (int, float)):
                yield child, value
            else:
                yield from numeric_leaves(value, child)
    elif isinstance(obj, list):
        for index, value in enumerate(obj):
            child = f"{prefix}[{index}]"
            if isinstance(value, bool):
                continue
            if isinstance(value, (int, float)):
                yield child, value
            else:
                yield from numeric_leaves(value, child)


# --------------------------------------------------------------------------
# the families
#
# Each family returns records of
#   (file, pointer, value, operands, arithmetic, recomputed)
# and carries:
#   doc      - the docs/ line that assigns the derivation to the compiler
#   scopes   - the content/ directories A28's rule covers
#   segment  - the pointer-SEGMENT regex A28 matches on. It is matched against
#              every NAME in the pointer, not only the leaf key, because three
#              of these families store the number under a generic leaf name
#              (`amount`, `minimum`, `maximum`) inside a specifically-named
#              parent. Matching names, not values, is what makes a rename
#              unable to reintroduce the field under a new spelling.
# --------------------------------------------------------------------------


def enemy_and_boss_files(tree: Tree) -> list[str]:
    return [
        p
        for p in tree.files("content/enemies") + tree.files("content/bosses")
        if Path(p).name not in {"shared-elite-modifiers.json"}
    ]


def fam_world_speed(tree: Tree):
    base = exact(at(tree.load("content/maps/standard-map-generation-contract.json"),
                    "reference_mech_speed_m_per_s"))
    out = []
    for path in tree.files("content/enemies") + tree.files("content/bosses"):
        doc = tree.load(path)
        for pointer, value in numeric_leaves(doc):
            if not pointer.endswith("world_speed_m_per_s"):
                continue
            parent = pointer.rsplit(".", 1)[0]
            pct_pointer = f"{parent}.percent_of_mech_base_speed.percent"
            pct = exact(at(doc, pct_pointer))
            out.append(
                dict(
                    file=path,
                    pointer=pointer,
                    value=value,
                    operands=[
                        f"{path}::{pct_pointer} = {fmt(pct)}",
                        f"{MECH_BASE_SPEED} = {fmt(base)}",
                    ],
                    arithmetic=f"{fmt(pct)} / 100 x {fmt(base)} = {fmt(pct / 100 * base)}",
                    recomputed=pct / 100 * base,
                )
            )
    return out


def _damage_pressure_blocks(tree: Tree):
    for path in tree.files("content/enemies") + tree.files("content/bosses"):
        doc = tree.load(path)
        if isinstance(doc, dict) and isinstance(doc.get("damage_pressure"), dict):
            yield path, doc, doc["damage_pressure"]


def fam_damage_pressure_block(tree: Tree):
    """Every numeric leaf inside a damage_pressure block, in one family.

    The three field names are kept as three DERIVATIONS with three arithmetics,
    but as ONE assertion, because the assertion that actually holds is
    structural: `damage_pressure` is the survivability report's block and holds
    no authored number at all. That is rename-proof in a way a name list is not.
    """
    out = []
    for path, doc, block in _damage_pressure_blocks(tree):
        contact = exact(doc["contact_damage"])
        interval = exact(doc["contact_cadence"]["same_enemy_repeat_interval_seconds"])
        hits = math.ceil(Fraction(DAMAGE_PRESSURE_HULL) / contact)

        if "contact_damage" in block:
            out.append(
                dict(
                    file=path,
                    pointer="damage_pressure.contact_damage",
                    value=block["contact_damage"],
                    operands=[f"{path}::contact_damage = {fmt(contact)}"],
                    arithmetic=(
                        f"identity restatement of the definition's own contact_damage = "
                        f"{fmt(contact)}"
                    ),
                    recomputed=contact,
                )
            )
        if "hits_to_defeat_100_hull" in block:
            out.append(
                dict(
                    file=path,
                    pointer="damage_pressure.hits_to_defeat_100_hull",
                    value=block["hits_to_defeat_100_hull"],
                    operands=[
                        f"{path}::contact_damage = {fmt(contact)}",
                        f"{path}::damage_pressure.assumptions states the "
                        f"{DAMAGE_PRESSURE_HULL} Hull baseline (string, retained)",
                    ],
                    arithmetic=f"ceil({DAMAGE_PRESSURE_HULL} / {fmt(contact)}) = {hits}",
                    recomputed=Fraction(hits),
                )
            )
        key = "continuous_overlap_time_to_defeat_seconds"
        if key in block:
            value = (hits - 1) * interval
            out.append(
                dict(
                    file=path,
                    pointer=f"damage_pressure.{key}",
                    value=block[key],
                    operands=[
                        f"{path}::contact_damage = {fmt(contact)}",
                        f"{path}::contact_cadence.same_enemy_repeat_interval_seconds = "
                        f"{fmt(interval)}",
                        f"{path}::contact_cadence.first_hit = 'immediately when an eligible "
                        f"overlap begins' (string, retained: it is why the count is hits-1)",
                    ],
                    arithmetic=(
                        f"(ceil({DAMAGE_PRESSURE_HULL} / {fmt(contact)}) - 1) x {fmt(interval)} = "
                        f"({hits} - 1) x {fmt(interval)} = {fmt(value)}"
                    ),
                    recomputed=value,
                )
            )
    return out


def fam_resonant_hits(tree: Tree):
    key = "fresh_mech_hits_to_defeat_at_resonant_value"
    out = []
    for path in tree.files("content/enemies") + tree.files("content/bosses"):
        doc = tree.load(path)
        for pointer, value in numeric_leaves(doc):
            if not pointer.endswith(key):
                continue
            parent = pointer.rsplit(".", 1)[0]
            resonant = exact(at(doc, f"{parent}.resonant_damage"))
            hits = math.ceil(Fraction(DAMAGE_PRESSURE_HULL) / resonant)
            out.append(
                dict(
                    file=path,
                    pointer=pointer,
                    value=value,
                    operands=[f"{path}::{parent}.resonant_damage = {fmt(resonant)} (retained)"],
                    arithmetic=f"ceil({DAMAGE_PRESSURE_HULL} / {fmt(resonant)}) = {hits}",
                    recomputed=Fraction(hits),
                )
            )
    return out


def fam_powerup_cumulative(tree: Tree):
    out = []
    for path in tree.files("content/powerups"):
        doc = tree.load(path)
        running = Fraction(0)
        prices: list[str] = []
        for index, entry in enumerate(doc["ranks"]):
            price = exact(entry["price_hyper_gold"])
            running += price
            prices.append(fmt(price))
            out.append(
                dict(
                    file=path,
                    pointer=f"ranks[{index}].cumulative_cost_hyper_gold",
                    value=entry["cumulative_cost_hyper_gold"],
                    operands=[
                        f"{path}::ranks[0..{index}].price_hyper_gold = {', '.join(prices)}"
                    ],
                    arithmetic=f"{' + '.join(prices)} = {fmt(running)}",
                    recomputed=running,
                )
            )
    return out


def fam_utility_rank_total(tree: Tree):
    out = []
    for path in tree.files("content/utilities"):
        doc = tree.load(path)
        acq = doc.get("acquisition")
        if not isinstance(acq, dict) or "total_rank_ore_cost" not in acq:
            continue
        costs = [exact(c) for c in acq["rank_ore_costs"]]
        total = sum(costs, Fraction(0))
        listed = ", ".join(fmt(c) for c in costs) or "(empty)"
        out.append(
            dict(
                file=path,
                pointer="acquisition.total_rank_ore_cost",
                value=acq["total_rank_ore_cost"],
                operands=[f"{path}::acquisition.rank_ore_costs = [{listed}]"],
                arithmetic=(
                    f"{' + '.join(fmt(c) for c in costs)} = {fmt(total)}"
                    if costs
                    else "sum of the empty rank_ore_costs list = 0"
                ),
                recomputed=total,
            )
        )
    return out


def fam_stat_price_curve(tree: Tree):
    path = "content/weapons/stat-price-formula.json"
    doc = tree.load(path)
    formula = doc["formula"]
    out = []

    def price(n: int) -> Fraction:
        return Fraction(5 * n * (n + 1))

    for index, value in enumerate(doc["first_ten_prices"]):
        n = index + 1
        out.append(
            dict(
                file=path,
                pointer=f"first_ten_prices[{index}]",
                value=value,
                operands=[f"{path}::formula = '{formula}' (retained)"],
                arithmetic=f"5 x {n} x ({n} + 1) = {fmt(price(n))}",
                recomputed=price(n),
            )
        )
    for index, entry in enumerate(doc["cumulative_cost_checkpoints"]):
        purchases = entry["purchases"]
        total = sum((price(n) for n in range(1, purchases + 1)), Fraction(0))
        out.append(
            dict(
                file=path,
                pointer=f"cumulative_cost_checkpoints[{index}].cumulative_cost",
                value=entry["cumulative_cost"],
                operands=[
                    f"{path}::formula = '{formula}' (retained)",
                    f"{path}::cumulative_cost_checkpoints[{index}].purchases = {purchases} "
                    f"(retained)",
                ],
                arithmetic=(
                    "sum of 5n(n+1) for n = 1.."
                    f"{purchases} = {' + '.join(fmt(price(n)) for n in range(1, purchases + 1))}"
                    f" = {fmt(total)}"
                ),
                recomputed=total,
            )
        )
    return out


def fam_resource_totals(tree: Tree):
    """The one family whose totals are TIERED, so five of its records reproduce
    only TRANSITIVELY - from operands that are themselves in the removal set.

    That has to be stated, not left for a reader to notice: `operands` on those
    five names a value this same pass deletes, so "every operand survives the
    removal" is true only after the chain is unrolled to authored leaves.
    `chains_through` names the removed pointers, and `arithmetic` carries the
    fully unrolled form as well as the tiered one.
    """
    out = []

    def rec(path, pointer, value, operands, arithmetic, recomputed, chains=()):
        out.append(
            dict(
                file=path,
                pointer=pointer,
                value=value,
                operands=operands,
                arithmetic=arithmetic,
                recomputed=recomputed,
                chains_through=list(chains),
            )
        )

    ore = "content/resources/common-ore.json"
    doc = tree.load(ore)
    per_map: dict[str, Fraction] = {}
    for index, source in enumerate(doc["sources"]):
        base = f"sources[{index}]"
        if "ore_per_installment" in source:
            per_inst = exact(source["ore_per_installment"])
            count = exact(source["installment_count"])
            seams = exact(source["seams_per_map"])
            seam_total = per_inst * count
            rec(
                ore,
                f"{base}.total_per_seam",
                source["total_per_seam"],
                [
                    f"{ore}::{base}.ore_per_installment = {fmt(per_inst)}",
                    f"{ore}::{base}.installment_count = {fmt(count)}",
                ],
                f"{fmt(per_inst)} x {fmt(count)} = {fmt(seam_total)}",
                seam_total,
            )
            map_total = seam_total * seams
            rec(
                ore,
                f"{base}.total_per_map",
                source["total_per_map"],
                [
                    f"{ore}::{base}.ore_per_installment = {fmt(per_inst)}",
                    f"{ore}::{base}.installment_count = {fmt(count)}",
                    f"{ore}::{base}.seams_per_map = {fmt(seams)}",
                ],
                f"{fmt(per_inst)} x {fmt(count)} x {fmt(seams)} = {fmt(map_total)}",
                map_total,
            )
            per_map[source["source"]] = map_total
        elif "ore_per_completion" in source:
            per_completion = exact(source["ore_per_completion"])
            bounds = {}
            for bound in ("minimum", "maximum"):
                geodes = exact(source["geodes_per_map"][bound])
                total = per_completion * geodes
                bounds[bound] = total
                rec(
                    ore,
                    f"{base}.total_per_map.{bound}",
                    source["total_per_map"][bound],
                    [
                        f"{ore}::{base}.ore_per_completion = {fmt(per_completion)}",
                        f"{ore}::{base}.geodes_per_map.{bound} = {fmt(geodes)}",
                    ],
                    f"{fmt(per_completion)} x {fmt(geodes)} = {fmt(total)}",
                    total,
                )
            per_map["geode-min"] = bounds["minimum"]
            per_map["geode-max"] = bounds["maximum"]
        elif "ore_per_boss" in source:
            per_boss = exact(source["ore_per_boss"])
            bosses = exact(source["boss_count"])
            total = per_boss * bosses
            rec(
                ore,
                f"{base}.total_per_run",
                source["total_per_run"],
                [
                    f"{ore}::{base}.ore_per_boss = {fmt(per_boss)}",
                    f"{ore}::{base}.boss_count = {fmt(bosses)}",
                ],
                f"{fmt(per_boss)} x {fmt(bosses)} = {fmt(total)}",
                total,
            )
            per_map["interval boss defeat"] = total

    seam_sum = per_map["standard ore seam"] + per_map["rich ore seam"]
    rec(
        ore,
        "seam_total_per_map",
        doc["seam_total_per_map"],
        [
            f"{ore}::sources[0].total_per_map = {fmt(per_map['standard ore seam'])} "
            f"(ITSELF REMOVED BY THIS PASS - chained; unrolls to 10 x 10 x 20)",
            f"{ore}::sources[1].total_per_map = {fmt(per_map['rich ore seam'])} "
            f"(ITSELF REMOVED BY THIS PASS - chained; unrolls to 40 x 5 x 8)",
        ],
        f"tiered: {fmt(per_map['standard ore seam'])} + {fmt(per_map['rich ore seam'])} = "
        f"{fmt(seam_sum)}; unrolled to authored leaves: "
        f"(10 x 10 x 20) + (40 x 5 x 8) = {fmt(seam_sum)}",
        seam_sum,
        chains=["sources[0].total_per_map", "sources[1].total_per_map"],
    )
    for bound, geode_key in (("minimum", "geode-min"), ("maximum", "geode-max")):
        total = seam_sum + per_map[geode_key] + per_map["interval boss defeat"]
        rec(
            ore,
            f"complete_run_ceiling_before_relic_sales.{bound}",
            doc["complete_run_ceiling_before_relic_sales"][bound],
            [
                f"{ore}::seam_total_per_map = {fmt(seam_sum)} (ITSELF REMOVED BY THIS PASS - "
                f"chained, and itself chained through sources[0..1].total_per_map)",
                f"{ore}::sources[2].total_per_map.{bound} = {fmt(per_map[geode_key])} "
                f"(ITSELF REMOVED BY THIS PASS - chained)",
                f"{ore}::sources[3].total_per_run = "
                f"{fmt(per_map['interval boss defeat'])} (ITSELF REMOVED BY THIS PASS - chained)",
            ],
            f"tiered: {fmt(seam_sum)} + {fmt(per_map[geode_key])} + "
            f"{fmt(per_map['interval boss defeat'])} = {fmt(total)}; unrolled to authored leaves: "
            f"(10 x 10 x 20) + (40 x 5 x 8) + "
            f"({fmt(per_completion)} x {fmt(exact(doc['sources'][2]['geodes_per_map'][bound]))}) + "
            f"({fmt(per_boss)} x {fmt(bosses)}) = {fmt(total)}",
            total,
            chains=[
                "seam_total_per_map",
                f"sources[2].total_per_map.{bound}",
                "sources[3].total_per_run",
            ],
        )

    gold = "content/resources/hyper-gold.json"
    doc = tree.load(gold)
    site_total = boss_total = Fraction(0)
    for index, source in enumerate(doc["sources"]):
        base = f"sources[{index}]"
        if "award_per_completion" in source:
            award = exact(source["award_per_completion"])
            sites = exact(source["sites_per_map"])
            site_total = award * sites
            rec(
                gold,
                f"{base}.total_per_map",
                source["total_per_map"],
                [
                    f"{gold}::{base}.award_per_completion = {fmt(award)}",
                    f"{gold}::{base}.sites_per_map = {fmt(sites)}",
                ],
                f"{fmt(award)} x {fmt(sites)} = {fmt(site_total)}",
                site_total,
            )
        elif "award_per_boss" in source:
            award = exact(source["award_per_boss"])
            bosses = exact(source["boss_count"])
            boss_total = award * bosses
            rec(
                gold,
                f"{base}.total_per_run",
                source["total_per_run"],
                [
                    f"{gold}::{base}.award_per_boss = {fmt(award)}",
                    f"{gold}::{base}.boss_count = {fmt(bosses)}",
                ],
                f"{fmt(award)} x {fmt(bosses)} = {fmt(boss_total)}",
                boss_total,
            )
    ceiling = site_total + boss_total
    rec(
        gold,
        "run_ceiling",
        doc["run_ceiling"],
        [
            f"{gold}::sources[0].total_per_map = {fmt(site_total)} "
            f"(ITSELF REMOVED BY THIS PASS - chained; unrolls to 100 x 3)",
            f"{gold}::sources[1].total_per_run = {fmt(boss_total)} "
            f"(ITSELF REMOVED BY THIS PASS - chained; unrolls to 25 x 4)",
        ],
        f"tiered: {fmt(site_total)} + {fmt(boss_total)} = {fmt(ceiling)}; unrolled to authored "
        f"leaves: (100 x 3) + (25 x 4) = {fmt(ceiling)}",
        ceiling,
        chains=["sources[0].total_per_map", "sources[1].total_per_run"],
    )
    return out


def fam_mining_site_totals(tree: Tree):
    out = []

    def rec(path, pointer, value, operands, arithmetic, recomputed):
        out.append(
            dict(
                file=path,
                pointer=pointer,
                value=value,
                operands=operands,
                arithmetic=arithmetic,
                recomputed=recomputed,
            )
        )

    for path in tree.files("content/mining-sites"):
        doc = tree.load(path)
        count = exact(doc["count_per_standard_map"]) if "count_per_standard_map" in doc else None

        if "installment_count" in doc:
            per_inst = exact(doc["payout_per_installment"]["amount"])
            inst_seconds = exact(doc["installment_duration_seconds"])
            inst_count = exact(doc["installment_count"])
            depletion = inst_seconds * inst_count
            rec(
                path,
                "total_depletion_seconds",
                doc["total_depletion_seconds"],
                [
                    f"{path}::installment_duration_seconds = {fmt(inst_seconds)}",
                    f"{path}::installment_count = {fmt(inst_count)}",
                ],
                f"{fmt(inst_seconds)} x {fmt(inst_count)} = {fmt(depletion)}",
                depletion,
            )
            per_seam = per_inst * inst_count
            rec(
                path,
                "total_payout_per_seam.amount",
                doc["total_payout_per_seam"]["amount"],
                [
                    f"{path}::payout_per_installment.amount = {fmt(per_inst)}",
                    f"{path}::installment_count = {fmt(inst_count)}",
                ],
                f"{fmt(per_inst)} x {fmt(inst_count)} = {fmt(per_seam)}",
                per_seam,
            )
            per_map = per_seam * count
            rec(
                path,
                "total_payout_per_map.amount",
                doc["total_payout_per_map"]["amount"],
                [
                    f"{path}::payout_per_installment.amount = {fmt(per_inst)}",
                    f"{path}::installment_count = {fmt(inst_count)}",
                    f"{path}::count_per_standard_map = {fmt(count)}",
                ],
                f"{fmt(per_inst)} x {fmt(inst_count)} x {fmt(count)} = {fmt(per_map)}",
                per_map,
            )
            extraction = depletion * count
            rec(
                path,
                "total_uninterrupted_extraction_per_map_seconds",
                doc["total_uninterrupted_extraction_per_map_seconds"],
                [
                    f"{path}::installment_duration_seconds = {fmt(inst_seconds)}",
                    f"{path}::installment_count = {fmt(inst_count)}",
                    f"{path}::count_per_standard_map = {fmt(count)}",
                ],
                f"{fmt(inst_seconds)} x {fmt(inst_count)} x {fmt(count)} = {fmt(extraction)}",
                extraction,
            )
        elif "completion_payout" in doc and isinstance(doc["completion_payout"], dict):
            award = exact(doc["completion_payout"]["amount"])
            total = award * count
            rec(
                path,
                "total_payout_per_map.amount",
                doc["total_payout_per_map"]["amount"],
                [
                    f"{path}::completion_payout.amount = {fmt(award)}",
                    f"{path}::count_per_standard_map = {fmt(count)}",
                ],
                f"{fmt(award)} x {fmt(count)} = {fmt(total)}",
                total,
            )
        elif "geodes_per_present_material" in doc:
            materials = exact(doc["present_materials_per_run"])
            ore = next(
                exact(p["amount"])
                for p in doc["completion_payout"]
                if p["resource"] == "common ore"
            )
            for bound in ("minimum", "maximum"):
                per_material = exact(doc["geodes_per_present_material"][bound])
                geodes = per_material * materials
                rec(
                    path,
                    f"geodes_per_standard_map.{bound}",
                    doc["geodes_per_standard_map"][bound],
                    [
                        f"{path}::geodes_per_present_material.{bound} = {fmt(per_material)}",
                        f"{path}::present_materials_per_run = {fmt(materials)}",
                    ],
                    f"{fmt(per_material)} x {fmt(materials)} = {fmt(geodes)}",
                    geodes,
                )
                jackpot = ore * geodes
                rec(
                    path,
                    f"common_ore_from_completion_jackpots_per_map.{bound}",
                    doc["common_ore_from_completion_jackpots_per_map"][bound],
                    [
                        f"{path}::completion_payout[] common ore amount = {fmt(ore)}",
                        f"{path}::geodes_per_present_material.{bound} = {fmt(per_material)}",
                        f"{path}::present_materials_per_run = {fmt(materials)}",
                    ],
                    f"{fmt(ore)} x {fmt(per_material)} x {fmt(materials)} = {fmt(jackpot)}",
                    jackpot,
                )
    return out


def fam_map_site_hyper_gold(tree: Tree):
    path = "content/maps/standard-map-generation-contract.json"
    doc = tree.load(path)
    sites = exact(doc["site_placement"]["hyper_gold_sites"]["count"])
    award_path = "content/mining-sites/hyper-gold-sites.json"
    award = exact(at(tree.load(award_path), "completion_payout.amount"))
    total = sites * award
    return [
        dict(
            file=path,
            pointer="site_placement.hyper_gold_sites.total_site_based_hyper_gold",
            value=doc["site_placement"]["hyper_gold_sites"]["total_site_based_hyper_gold"],
            operands=[
                f"{path}::site_placement.hyper_gold_sites.count = {fmt(sites)}",
                f"{award_path}::completion_payout.amount = {fmt(award)}",
            ],
            arithmetic=f"{fmt(sites)} x {fmt(award)} = {fmt(total)}",
            recomputed=total,
        )
    ]


# Each family's ASSERTION is a rule over pointer SEGMENT NAMES, in the shape A20
# established: a broad semantic pattern plus a named allowlist. IT CATCHES A
# RENAME ONLY INTO A NAME THE PATTERN STILL MATCHES. An earlier revision of this
# comment said "so a rename inside the covered scope cannot reintroduce the field
# under a new spelling", which is false in the same way three other copies of that
# claim were, and this one survived the pass that corrected them - the correction
# grepped the files it had in hand rather than the claim. A rename to a name
# outside the word class passes; the VALUE layer is what covers that case, and it
# is the reason there are two layers.
#   scopes  - the content/ directories the rule covers. A20's two rules have two
#             different scopes for a reason, and the same applies here: three of
#             these patterns would flag legitimately authored fields in a
#             directory they do not cover.
#   parent  - optional. When set, the rule fires only for a leaf that sits under
#             a segment matching it, and `segment` is then matched against the
#             segments BELOW that parent.
#   allow   - segment names exempted by name, each with a reason, exactly as A20
#             allowlists reference_diameter_m.
#
# WHY THE AGGREGATE WORD CLASS IS SPELLED OUT ONCE AND THEN VARIED PER FAMILY,
# INSTEAD OF ONE GLOBAL RULE
# ==========================================================================
# Four families are the same semantic thing - "a stored sum" - and share
# AGGREGATE_WORDS below. They are still FOUR rules with FOUR scopes, and the
# reason is `content/powerups/`: that directory authors `total_` 71 times and
# every one of them SURVIVES this pass - 13 `total_cost_hyper_gold` (each
# carrying its own DEC-120#decision citation, and the operand of A14's second
# row) and 58 `total_effect` (44 `percent`, 5 `hull`, 5 `hull_per_second`,
# 3 `armor`, 1 `revival_charges_per_run`). Verify with:
#
#   grep -ho '"total_[a-z_]*"' content/powerups/*.json | sort | uniq -c
#
# A single global aggregate rule would therefore flag 71 authored fields and be
# unlandable. That is why PowerUp cumulative cost uses AGGREGATE_WORDS_NO_TOTAL
# and why the six rules are NOT consolidated into one. Consolidating them
# reintroduces that collision. The same per-scope reasoning is why the
# world-speed rule does not cover content/weapons/, and why the map rule spells
# `from_all` rather than `all` (content/maps/ authors
# `maximum_hyper_gold_sites_across_all_pockets` and
# `maximum_share_of_all_geodes_per_major_region`, which are authored BOUNDS on
# counts, not sums).
#
# Each class is a WORD CLASS, not a name list. Every one of the NINE rules of the
# earlier draft - six survive, three families were pulled - was first drafted as a
# short name list, and a negative-control probe (a field semantically inside the
# family but spelled with a word the list happened to omit) passed all nine. The
# lists below are what those probes forced. The nine-guard counts here and in the
# notes are HISTORY: they describe the draft the probes were run against.
AGGREGATE_WORDS = (
    r"total|sum|aggregate|combined|overall|grand|cumulative|accru|accumulat"
    r"|running_(total|sum|cost)|rolled?_up|to_date|so_far|thus_far|subtotal|tally"
    r"|net_|lifetime|entire|whole_|full_(run|map|seam|set)|from_all|all_sites$"
)
AGGREGATE_WORDS_NO_TOTAL = (
    r"sum|aggregate|combined|overall|grand|cumulative|accru|accumulat"
    r"|running_(total|sum|cost)|rolled?_up|to_date|so_far|thus_far|subtotal|tally"
    r"|net_|lifetime|entire|whole_|full_(run|map|seam|set)|spend|through_rank"
)

FAMILIES = [
    dict(
        name="enemy and boss world speed",
        builder=fam_world_speed,
        doc="docs/technical/40-content-data-and-validation.md:114 - 'Validation derives world "
        "speeds/footprints and compares them with the survivability report'",
        scopes=["enemies", "bosses"],
        parent=None,
        segment=r"(?i)world_speed|velocity|traverse|movement_rate|travel_rate"
        r"|(^|_)(m|metres?|meters?)_per_(s|sec|second)$|_mps$",
        allow={},
        note="17, not the 14 the reconciliation named: three are ability/projectile speeds "
        "(EN-06 specialist projectile 2.25, BOSS-01 charge 5.4, BOSS-03 ability projectile 2.25) "
        "built the same way from percent_of_mech_base_speed. The rule bans ANY absolute metres-"
        "per-second value in these two directories, not just the current spelling: an enemy or "
        "boss authors its speed as a percentage of the mech baseline, so an absolute one is "
        "always the compiler's. It does NOT cover content/weapons/, where projectile_speed_m_per_s "
        "is authored. The rule matches the UNIT rather than the field name: the first draft was "
        "`world_speed|_m_per_s$` and the probe `traverse_rate_metres_per_second` - the same "
        "quantity, spelled out - passed it.",
    ),
    dict(
        name="PowerUp cumulative cost",
        builder=fam_powerup_cumulative,
        doc="docs/technical/40-content-data-and-validation.md:136 - 'Validators recompute total "
        "catalog costs and maximum-account envelope'; with 40:203 'price curves, total costs'",
        scopes=["powerups"],
        parent=None,
        segment=r"(?i)" + AGGREGATE_WORDS_NO_TOTAL,
        allow={},
        note="total_cost_hyper_gold is RETAINED and the rule deliberately does not match it: it "
        "carries its own `total_cost_hyper_gold: DEC-120#decision` citation and is the operand of "
        "A14's second row, which sums the 13 per-entry totals to the doc-stated 9,450. This is the "
        "ONE family whose rule cannot use the total/sum/aggregate word class the four "
        "aggregate-total families use, because in content/powerups/ `total_` is authored twice "
        "over - total_cost_hyper_gold and every rank's total_effect - so the class would flag 71 "
        "surviving fields. Same reason A20 gives two rules two scopes, applied to patterns. It "
        "carries the rest of AGGREGATE_WORDS: the first draft was "
        "`cumulative|running_total|to_date|so_far` and the probe `accrued_cost_hyper_gold` passed "
        "it.",
    ),
    dict(
        name="utility total rank ore cost",
        builder=fam_utility_rank_total,
        doc="docs/technical/40-content-data-and-validation.md:203 - 'Recalculate ... price curves, "
        "total costs'",
        scopes=["utilities"],
        parent=None,
        segment=r"(?i)" + AGGREGATE_WORDS,
        allow={},
        note="13 of 13, including UTL-R1's 0, which is the sum of its empty rank_ore_costs list. "
        "The per-rank rank_ore_costs arrays are the operands and stay. The first draft was "
        "`total|sum|aggregate|combined|overall|grand` and the probe `accrued_rank_ore_cost` passed "
        "it, which is what added the cumulative half of AGGREGATE_WORDS.",
    ),
    dict(
        name="resource aggregate total",
        builder=fam_resource_totals,
        doc="docs/technical/40-content-data-and-validation.md:203 - 'Recalculate ... resource "
        "totals'",
        scopes=["resources"],
        parent=None,
        segment=r"(?i)" + AGGREGATE_WORDS + r"|ceiling|yield",
        allow={},
        note="sources[].depletion_seconds is NOT removed - a depletion duration is not a resource "
        "total; see reproduces_but_not_assigned_by_docs. `yield` is in the class because the probe "
        "`yield_per_seam` - a stored per-seam total spelled with no aggregate word at all - passed "
        "the `total|sum|aggregate|combined|overall|grand|ceiling` draft. content/resources/ authors "
        "no `yield` field; the per-installment operands are spelled ore_per_installment.",
    ),
    dict(
        name="mining-site aggregate total",
        builder=fam_mining_site_totals,
        doc="docs/technical/40-content-data-and-validation.md:140 - 'Standard mode validates "
        "exactly four accepted classes and their totals'; with 40:203",
        scopes=["mining-sites"],
        parent=None,
        segment=r"(?i)" + AGGREGATE_WORDS + r"|jackpot|geodes_per_standard_map",
        allow={
            "total_seam_payout_multiplier": "a comparison of the rich seam against the standard "
            "seam, whose operands live in another file and whose sibling "
            "exposure_per_secured_payout_multiplier has no stated derivation at all. Left "
            "authored, so allowlisted rather than removed."
        },
        note="count_per_standard_map is authored and does not match this rule. "
        "geodes_per_standard_map does, and both its bounds are removed - 8 x 4 and 10 x 4. `total|"
        "jackpot` was the original draft and `aggregate_payout_per_map` passed it; the probe that "
        "then passed `total|sum|aggregate|combined|overall|grand|jackpot` was "
        "`rolled_up_payout_per_map`, which is what added the cumulative half of AGGREGATE_WORDS. "
        "`payout` is deliberately NOT in the class: payout_per_installment, completion_payout and "
        "exposure_per_secured_payout_multiplier are authored here and stay.",
    ),
    dict(
        name="map-contract site-based Hyper Gold",
        builder=fam_map_site_hyper_gold,
        doc="docs/technical/40-content-data-and-validation.md:203 - 'Recalculate ... resource "
        "totals'",
        scopes=["maps"],
        parent=None,
        segment=r"(?i)" + AGGREGATE_WORDS,
        allow={},
        note="reference_mech_speed_m_per_s in the same file is RETAINED: it is the operand of all "
        "17 world speeds. The A13 world-prop values in this file do not match the rule. The probe "
        "that passed the `total|sum|aggregate|combined|overall|grand` draft was "
        "`hyper_gold_from_all_sites`, which is why AGGREGATE_WORDS carries `from_all` and "
        "`all_sites$` but NOT a bare `all`: content/maps/ authors "
        "maximum_hyper_gold_sites_across_all_pockets, maximum_relic_caches_across_all_pockets and "
        "maximum_share_of_all_geodes_per_major_region, which are authored bounds on counts and "
        "stay.",
    ),
]

# --------------------------------------------------------------------------
# PULLED FROM THIS PASS. These three families were built, verified to reproduce
# exactly, and then NOT removed. Their arithmetic is kept here because the
# reason each was pulled is a finding, and a reader who only sees six families
# cannot tell whether the other three were never found or were found and
# rejected. They are enumerated into the output file under
# `pulled_from_this_pass`, with counts, so the 115 can be reconciled to the 166
# an earlier draft claimed.
#
# The `damage-pressure survivability block` and `resonant-value hit count`
# families are pulled for ONE reason, and it is not reproducibility - both
# reproduce exactly.
#
# AN EARLIER DRAFT OF THIS COMMENT ARGUED THAT REMOVING THE REPORT WOULD MAKE
# THE COMPARISON IN docs/40 section 'Enemies and bosses' A TAUTOLOGY. THAT
# ARGUMENT IS WITHDRAWN. It was wrong on
# this tree: docs/data/contact-damage-pressure.csv carries the same report, so
# an independent comparand survives the removal either way, and A30 now asserts
# the two mirrors agree.
#
# THE REASON THAT IS ACTUALLY TRUE, and which holds whether or not another mirror
# happens to exist: there are THREE WRITERS on this report - the docs table, the
# content block, and the compiler that docs/40 sections 'Source-of-truth
# boundary' and 'Enemies and bosses' say derives it - and NO
# SETTLED AUTHORITY among them. Deleting any one copy silently DECIDES which of
# the survivors is authoritative. That is a decision nobody has made, and this
# pass would have taken it as a side effect of a commit whose stated purpose was
# deduplication. Removing a redundant copy and choosing an authority are different
# acts; only the first was authorised. So the copy stays until the authority
# question is answered on purpose.
#
# THE OPERAND/COMPARAND DISTINCTION IS ORTHOGONAL TO REPRODUCIBILITY. Every one
# of the 115 reproduces exactly; that says nothing about whether it is a
# comparand, and nothing about who owns it.
# --------------------------------------------------------------------------
PULLED_FROM_THIS_PASS = [
    dict(
        name="damage-pressure survivability block",
        builder=fam_damage_pressure_block,
        scopes=["enemies", "bosses"],
        pulled_because=(
            "THREE WRITERS ON ONE REPORT AND NO SETTLED AUTHORITY, so deleting a copy would "
            "silently decide which survivor is authoritative - a decision nobody has made, taken as "
            "a side effect of a deduplication pass. The three are docs/data/"
            "contact-damage-pressure.csv, the content block, and the compiler that docs/40 sections "
            "'Source-of-truth boundary' and 'Enemies and bosses' say derives the report. All 32 "
            "restored. Removing a redundant copy and choosing an "
            "authority are different acts and only the first was authorised. NOTE the earlier "
            "argument here - that removal would make the docs/40 section 'Enemies and bosses' "
            "comparison a tautology - is WITHDRAWN: "
            "the CSV supplies an independent comparand regardless, all 28 overlapping values "
            "agreeing exactly, and A30 now asserts that agreement. The withdrawn version depended on "
            "no other mirror existing; this one does not."
        ),
    ),
    dict(
        name="resonant-value hit count",
        builder=fam_resonant_hits,
        scopes=["enemies", "bosses"],
        pulled_because=(
            "Same reason as the damage-pressure block: these five are rows of the same "
            "survivability report, which docs/72 section 'Damage Pressure Reference' also states "
            "in full, so removing them would decide "
            "an unsettled authority question by side effect. All 5 restored."
        ),
    ),
    dict(
        name="stat upgrade price curve",
        builder=fam_stat_price_curve,
        scopes=["weapons"],
        pulled_because=(
            "MOVES CHECKABLE NUMBERS INTO AN UNCHECKED STRING, which is strictly worse than "
            "leaving them. All 14 values are still stated in the same file, in prose: "
            "`defining_prose` reads 'The first ten prices are 10, 30, 60, 100, 150, 210, 280, 360, "
            "450, and 550 ore. Cumulative cost reaches 100 after three purchases, 200 after four, "
            "350 after five, and 2,200 after ten.' A28 matches numeric leaves, so removing the "
            "leaves moved 14 checkable numbers somewhere no assertion looks. Editing the prose is "
            "NOT the fix and was not done: that sentence is a verified doc quotation in the "
            "quote-evidence artifact, so rewriting its numerals would falsify the citation while "
            "every validator stayed green. Whether the prose should stop restating derived numbers "
            "is a separate decision and not one to take inside a removal pass. All 14 restored."
        ),
    ),
]

# --------------------------------------------------------------------------
# Coincidental value recurrences the VALUE-KEYED gate finds and that are NOT
# second writers. Enumerated, not thresholded: the gate has no tolerance, no
# rounding and no scope shortcut that could shrink this list silently. Every
# entry is one line of justification.
#
# STALENESS IS CHECKED AGAINST THE CURRENT WORKTREE, NOT AGAINST SWEEP_REF. An
# earlier revision checked it against Tree(SWEEP_REF), where the colliding value
# collides by construction - so an entry here was "still colliding" forever no
# matter what the tree did, and the sentence claiming a stale exception fails was
# false. Changing content/utilities/UTL-R1.json acquisition.rank_count from 0 to 1
# ends the only declared collision and neither tool noticed. Both halves are now
# asserted: unused on the sweep ref (the gate never consulted it) AND no longer
# colliding on the current tree (the justification stopped being true).
# --------------------------------------------------------------------------
VALUE_COLLISION_EXCEPTIONS = {
    ("content/utilities/UTL-R1.json", "acquisition.total_rank_ore_cost",
     "acquisition.rank_count"): (
        "Coincidence of zero, and the only one in the tree. UTL-R1 is the ore-crafted radar: its "
        "rank_ore_costs list is empty, so its removed total is the sum of no terms = 0, and it "
        "independently has rank_count = 0 because it has no ranks. Two different quantities that "
        "are both zero, not one quantity written twice."
    ),
}


def pointer_segments(pointer: str) -> list[str]:
    return [s for s in re.split(r"\.|\[\d+\]", pointer) if s]


def rule_matches(family: dict, pointer: str) -> bool:
    """A28's matcher: a family's rule against a pointer's SEGMENT NAMES.

    The allowlist is consulted on the LEAF segment, which is A20's semantics
    (`if key in DERIVED_FOOTPRINT_FIELD_ALLOWED: continue`). An allowlisted leaf
    is exempt even when one of its ANCESTOR names matches the pattern - which is
    the only case that arises here, and the reason the allowlist is needed at
    all: `purchases` matches nothing itself, it inherits the match from
    `cumulative_cost_checkpoints` above it.
    """
    segments = pointer_segments(pointer)
    child_rx = re.compile(family["segment"])
    allow = family.get("allow") or {}
    if segments and segments[-1] in allow:
        return False
    if family.get("parent"):
        parent_rx = re.compile(family["parent"])
        for index, seg in enumerate(segments):
            if parent_rx.search(seg) and any(child_rx.search(s) for s in segments[index + 1 :]):
                return True
        return False
    return any(child_rx.search(seg) for seg in segments)


OPERAND_RE = re.compile(r"(content/[\w./-]+\.json)::([A-Za-z_][\w.\[\]]*)")


def operand_pointers(record: dict) -> list[str]:
    """The record's operands as machine-readable (file, pointer) strings.

    A28's value-keyed gate needs to know which leaves ARE the operands of a
    derived value, because an operand holding the same value as the thing derived
    from it is not a second writer. `ranks[0].cumulative_cost_hyper_gold` is the
    sum of exactly one price, so it equals `ranks[0].price_hyper_gold` by
    arithmetic necessity - a substring test on the operand prose misses that,
    because the prose writes the range form `ranks[0..0].price_hyper_gold`.
    """
    out: list[str] = []
    for text in record["operands"]:
        for path, pointer in OPERAND_RE.findall(text):
            rng = re.search(r"\[(\d+)\.\.(\d+)\]", pointer)
            if rng:
                lo, hi = int(rng.group(1)), int(rng.group(2))
                for index in range(lo, hi + 1):
                    out.append(f"{path}::{pointer[: rng.start()]}[{index}]"
                               f"{pointer[rng.end():]}")
            else:
                out.append(f"{path}::{pointer}")
    return out


def derivation_site(pointer: str) -> str:
    """The pointer of the object that HELD the removed leaf.

    A trailing `[i]` means the container is the list, so drop the index; otherwise
    drop the last dotted segment. A ROOT-LEVEL leaf has neither, and its container
    is the DOCUMENT ITSELF - returned as "" and read by is_in_site() as the whole
    document, not as "no site".

    That last case was a hole, not a subtlety: an earlier revision returned "" and
    then filtered with `if not (site and ...)`, so a falsy site skipped EVERY leaf
    and the value layer searched nothing at all for the six root-level records
    (common-ore seam_total_per_map, hyper-gold run_ceiling, and total_depletion_
    seconds / total_uninterrupted_extraction_per_map_seconds on both ore-seam
    files). They could not fire on anything, including on themselves reinjected.
    """
    if pointer.endswith("]"):
        return pointer[: pointer.rindex("[")]
    return pointer.rsplit(".", 1)[0] if "." in pointer else ""


def is_in_site(leaf: str, site: str) -> bool:
    """Is `leaf` the site itself or inside its subtree? "" is the document root."""
    if site == "":
        return True
    return leaf == site or leaf.startswith(site + ".") or leaf.startswith(site + "[")


def value_collision_hits(
    family: dict, records: list[dict], tree: Tree
) -> tuple[list[tuple], set]:
    """A28's VALUE-KEYED gate, measured on the sweep-ref tree.

    Asks the question the name rules cannot: is there a numeric leaf AT THE
    DERIVATION SITE carrying the derived value? The site is the object that held
    the removed leaf, and the walk descends into containers, so this survives a
    rename, a relocation within the site, a different unit suffix, and a change
    of arity (scalar -> [scalar]) - none of which changes the number.

    NO TOLERANCE AND NO SHORTCUT. Values compare exactly, as Fractions. The only
    exclusions are (a) the record's own declared operands and (b) the entries in
    VALUE_COLLISION_EXCEPTIONS, which are enumerated and individually justified.

    THE RADIUS IS THE LIMIT AND IT IS MEASURED, NOT ASSERTED - see
    measure_search_radii(), whose three counts go into the expectation file so the
    ratio a reader is shown reproduces from the artifact instead of from a
    docstring. Widening to file or scope radius costs coincidental recurrences by
    the dozen and by the hundred respectively, almost all magnitude coincidences
    between unrelated quantities (a 1.5 m/s world speed against a 1.5 s
    control-immunity window, a hit count of 4 against maximum_simultaneous_bosses).
    An exception list that size could not be justified entry by entry, so the
    radius is narrow AND SAID TO BE NARROW.

    Returns (hits, exceptions_used). The second value is what makes a declared
    exception the gate never consulted a failure rather than dead weight.
    """
    hits = []
    used: set = set()
    for record in records:
        pointer = record["pointer"]
        site = derivation_site(pointer)
        own_operands = {p.split("::", 1)[1] for p in record["operand_pointers"]
                        if p.split("::", 1)[0] == record["file"]}
        target = exact(record["value"])
        for leaf, value in numeric_leaves(tree.load(record["file"])):
            if leaf == pointer or exact(value) != target:
                continue
            if not is_in_site(leaf, site):
                continue
            if leaf in own_operands:
                continue
            if (record["file"], pointer, leaf) in VALUE_COLLISION_EXCEPTIONS:
                used.add((record["file"], pointer, leaf))
                continue
            hits.append((record["file"], pointer, leaf, value))
    return hits, used


def still_colliding_in_worktree(records: list[dict]) -> set:
    """Which declared exceptions STILL collide in the CURRENT worktree.

    This is the check the previous revision claimed to have and did not: it read
    the file out of Tree(SWEEP_REF), where the colliding leaf holds the colliding
    value by construction, so no tree state could ever make an entry stale. Read
    from disk instead, at the same radius the live gate uses.
    """
    by_key = {(r["file"], r["pointer"]): r for r in records}
    live: set = set()
    for key in VALUE_COLLISION_EXCEPTIONS:
        file_path, pointer, colliding = key
        record = by_key.get((file_path, pointer))
        if record is None:
            continue
        path = REPO / file_path
        if not path.exists():
            continue
        target = exact(record["value"])
        site = derivation_site(pointer)
        for leaf, value in numeric_leaves(json.loads(path.read_text())):
            if leaf == colliding and is_in_site(leaf, site) and exact(value) == target:
                live.add(key)
    return live


def measure_search_radii(families: list[dict], tree: Tree) -> dict:
    """Count the coincidental recurrences three candidate radii would flag.

    ONE DEFINITION, APPLIED THREE TIMES, so the three numbers are comparable and
    reproduce: a (removed value, numeric leaf) pair counts when the leaf holds the
    removed value exactly, is not the removed leaf itself, and is not one of that
    value's own operands in that file. Only the RADIUS varies - the derivation
    site, the whole file, or every file in the family's scopes.

    Earlier revisions printed 55 at file radius and 400 at scope radius. Neither
    reproduced: no code computed them, and a hand count varies with which
    exclusions it applies. These are computed on SWEEP_REF, carried in the
    expectation file, and printed from there.
    """
    counts = {"site": 0, "file": 0, "scope": 0}
    for family in families:
        scope_index: dict[Fraction, list[tuple[str, str]]] = {}
        for scope in family["scopes"]:
            for path in tree.files(scope.rstrip("/")):
                for leaf, value in numeric_leaves(tree.load(path)):
                    scope_index.setdefault(exact(value), []).append((path, leaf))
        for record in family["records"]:
            pointer = record["pointer"]
            site = derivation_site(pointer)
            own = {p.split("::", 1)[1] for p in record["operand_pointers"]
                   if p.split("::", 1)[0] == record["file"]}
            target = exact(record["value"])
            for path, leaf in scope_index.get(target, ()):
                if path == record["file"] and (leaf == pointer or leaf in own):
                    continue
                counts["scope"] += 1
                if path != record["file"]:
                    continue
                counts["file"] += 1
                if is_in_site(leaf, site):
                    counts["site"] += 1
    return counts


def build(sweep_ref: str) -> dict:
    tree = Tree(sweep_ref)
    failures: list[str] = []
    families = []
    all_records = []
    exceptions_used: set = set()

    for family in FAMILIES:
        records = family["builder"](tree)
        emitted = []
        for record in records:
            stored = exact(record["value"])
            if stored != record["recomputed"]:
                failures.append(
                    f"{record['file']} :: {record['pointer']} STORES {record['value']} but its "
                    f"operands give {fmt(record['recomputed'])} ({record['arithmetic']}). This is a "
                    f"VALUE DEFECT, not a redundancy - it is NOT removed."
                )
                continue
            if not rule_matches(family, record["pointer"]):
                failures.append(
                    f"{record['file']} :: {record['pointer']} is in family "
                    f"'{family['name']}' but does not match its own A28 segment rule "
                    f"/{family['segment']}/ - the assertion would not catch its return."
                )
                continue
            emitted.append(
                dict(
                    file=record["file"],
                    pointer=record["pointer"],
                    value=record["value"],
                    operands=record["operands"],
                    operand_pointers=operand_pointers(record),
                    chains_through=record.get("chains_through", []),
                    arithmetic=record["arithmetic"],
                )
            )

        # The rule must match the family's removal set and NOTHING ELSE in its
        # scope on the sweep ref. A rule that also matches a surviving authored
        # field would make A28 unlandable; a rule that matches fewer than the
        # removal set would let part of the family return.
        expected_pointers = {(r["file"], r["pointer"]) for r in emitted}
        matched = set()
        corpus = 0
        for scope in family["scopes"]:
            paths = tree.files(f"content/{scope}")
            if not paths:
                failures.append(
                    f"family '{family['name']}' scope content/{scope}/ contains NO json files on "
                    f"{sweep_ref[:12]}. A rule over an empty corpus passes vacuously."
                )
            for path in paths:
                for pointer, _ in numeric_leaves(tree.load(path)):
                    corpus += 1
                    if rule_matches(family, pointer):
                        matched.add((path, pointer))
        if corpus == 0:
            failures.append(
                f"family '{family['name']}' scopes {family['scopes']} hold NO numeric leaves on "
                f"{sweep_ref[:12]} - the rule would pass vacuously."
            )
        family["corpus_numeric_leaves"] = corpus

        # The VALUE-KEYED gate. Names can be renamed; the number cannot.
        collisions, used = value_collision_hits(family, emitted, tree)
        exceptions_used |= used
        if collisions:
            failures.append(
                f"family '{family['name']}' value-keyed gate: {len(collisions)} derived value(s) "
                f"also sit at a non-operand leaf inside their own derivation site on "
                f"{sweep_ref[:12]}. Each is either a second writer (fix the tree) or a coincidence "
                f"(add it to VALUE_COLLISION_EXCEPTIONS with a reason): {collisions[:6]}"
            )
        if matched != expected_pointers:
            over = sorted(matched - expected_pointers)
            under = sorted(expected_pointers - matched)
            failures.append(
                f"family '{family['name']}' A28 rule /{family['segment']}/ over "
                f"{family['scopes']} matches {len(matched)} numeric leaf/leaves on {sweep_ref[:12]} "
                f"but the removal set has {len(expected_pointers)}; "
                f"extra={over[:6]} missing={under[:6]}"
            )

        families.append(
            dict(
                name=family["name"],
                doc_assignment=family["doc"],
                scopes=[f"content/{s}/" for s in family["scopes"]],
                pointer_parent_rule=family["parent"],
                pointer_segment_rule=family["segment"],
                allowlisted_segments=family["allow"],
                note=family["note"],
                count=len(emitted),
                corpus_numeric_leaves=family["corpus_numeric_leaves"],
                records=sorted(emitted, key=lambda r: (r["file"], r["pointer"])),
            )
        )
        all_records.extend(emitted)

    # A stale exception is as much a defect as a missing one: if a declared
    # coincidence stops colliding, the justification is no longer true. TWO
    # HALVES, and the previous revision had neither working.
    #
    # (a) UNUSED ON THE SWEEP REF - the gate walked the site and never consulted
    #     this entry, so it is suppressing nothing. This is the pattern A30 uses
    #     (`set(CSV_MIRROR_ROUNDED) - declared_used`) and the reason it is worth
    #     copying is that it cannot be satisfied by an assumption: only an entry
    #     the scan actually reached counts as used.
    # (b) NO LONGER COLLIDING IN THE WORKTREE - read from disk, not from
    #     Tree(sweep_ref). Against the pinned ref the colliding value collides by
    #     construction, which is why the old check could not fail: it asserted a
    #     property of a frozen blob and called it a property of the tree.
    declared = set(VALUE_COLLISION_EXCEPTIONS)
    for key in sorted(declared - exceptions_used):
        failures.append(
            f"VALUE_COLLISION_EXCEPTIONS entry {key} was never consulted by the value gate on "
            f"{sweep_ref[:12]} - it suppresses nothing. Delete it."
        )
    live = still_colliding_in_worktree(all_records)
    for key in sorted(declared - live):
        failures.append(
            f"VALUE_COLLISION_EXCEPTIONS entry {key} no longer collides in the CURRENT worktree. "
            f"The justification recorded for it is no longer true, and a stale exception silently "
            f"widens the gate - delete it."
        )

    if not families:
        failures.append(
            "FAMILIES is empty. A28 would report '(nothing to report)' and PASS under a heading "
            "that promises the derived-value families, which is a vacuous pass."
        )
    if len(families) != DECLARED_FAMILY_COUNT:
        failures.append(
            f"{len(families)} family/families built but DECLARED_FAMILY_COUNT is "
            f"{DECLARED_FAMILY_COUNT}. The count is declared separately on purpose: it is what "
            f"stops a family being dropped without anyone noticing."
        )
    if len(all_records) != DECLARED_TOTAL_REMOVED:
        failures.append(
            f"{len(all_records)} value(s) built but DECLARED_TOTAL_REMOVED is "
            f"{DECLARED_TOTAL_REMOVED}."
        )

    if failures:
        for line in failures:
            print(f"DEFECT: {line}", file=sys.stderr)
        raise SystemExit(
            f"{len(failures)} candidate(s) failed the exact-reproduction or rule-coverage gate; "
            f"nothing was written"
        )

    radii = measure_search_radii(families, tree)

    multiset = sorted(
        [record["file"], record["pointer"], record["value"]] for record in all_records
    )
    return dict(
        schema="expected-derived-value-removals/1",
        sweep_ref=sweep_ref,
        generated_by="src/MechaMiner.Tools/ContentImport/derive_derived_value_expectations.py",
        claim=(
            f"Exactly these {len(all_records)} numeric leaves, and no others, are removed from "
            f"content/ by this pass. Each reproduces exactly from operands that survive the "
            f"removal, in exact rational arithmetic. A29 in verify_content.py asserts SET EQUALITY "
            f"between this list and the numeric multiset the tree actually lost relative to "
            f"{sweep_ref} - element by element over all {len(all_records)} elements, not by "
            f"matching totals."
        ),
        total_removed=len(all_records),
        family_count=len(families),
        declared_family_count=DECLARED_FAMILY_COUNT,
        declared_total_removed=DECLARED_TOTAL_REMOVED,
        families=families,
        pulled_from_this_pass=[
            dict(
                name=pulled["name"],
                scopes=[f"content/{s}/" for s in pulled["scopes"]],
                count_restored=len(pulled["builder"](tree)),
                pulled_because=pulled["pulled_because"],
            )
            for pulled in PULLED_FROM_THIS_PASS
        ],
        search_radius_measurement=dict(
            definition=(
                "A (removed value, numeric leaf) pair counts when the leaf holds the removed "
                "value EXACTLY, is not the removed leaf itself, and is not one of that value's "
                "own operands in that file. One definition, three radii, measured on "
                f"{sweep_ref[:12]} - so the three numbers are comparable and reproduce from this "
                "file. Earlier revisions quoted 55 at file radius and 400 at scope radius; "
                "neither reproduced, because no code computed them."
            ),
            site_radius_pairs=radii["site"],
            file_radius_pairs=radii["file"],
            scope_radius_pairs=radii["scope"],
            declared_exceptions=len(VALUE_COLLISION_EXCEPTIONS),
            conclusion=(
                f"Ratio {radii['site']} : {radii['file']} : {radii['scope']}. Widening the radius "
                f"to the file would need {radii['file'] - radii['site']} more hand-written "
                f"exceptions and to the scope {radii['scope'] - radii['site']} more, almost all "
                "magnitude coincidences between unrelated quantities. That is the reason the "
                "radius is the derivation site, and the reason relocation OUT of the site is a "
                "stated limit rather than a fixable one."
            ),
        ),
        value_collision_exceptions=[
            dict(file=k[0], derived_pointer=k[1], colliding_pointer=k[2], why_not_a_second_writer=v)
            for k, v in sorted(VALUE_COLLISION_EXCEPTIONS.items())
        ],
        removed_numeric_multiset=multiset,
        retained_because_operand=RETAINED_BECAUSE_OPERAND,
        reproduces_but_not_assigned_by_docs=[
            dict(file=f, pointer=p, arithmetic=a, why_retained=w)
            for f, p, a, w in NOT_ASSIGNED_BY_DOCS
        ],
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--sweep-ref",
        default=SWEEP_REF,
        help=f"commit to enumerate from (default the pinned {SWEEP_REF[:12]})",
    )
    parser.add_argument("--check", action="store_true", help="fail if the committed file differs")
    args = parser.parse_args()

    payload = build(args.sweep_ref)
    text = json.dumps(payload, indent=2, sort_keys=False) + "\n"

    if args.check:
        if not OUT.exists():
            print(f"MISSING: {OUT}", file=sys.stderr)
            return 1
        current = OUT.read_text()
        if current != text:
            print(
                f"STALE: {OUT} does not match what this script regenerates from "
                f"{args.sweep_ref[:12]}",
                file=sys.stderr,
            )
            return 1
        print(f"ok - {OUT.name} regenerates byte-identically from {args.sweep_ref[:12]}")
    else:
        OUT.write_text(text)
        print(f"wrote {OUT} ({payload['total_removed']} values, {payload['family_count']} families)")

    for family in payload["families"]:
        print(f"  {family['count']:4d}  {family['name']}  [{', '.join(family['scopes'])}]")
    print(f"  {payload['total_removed']:4d}  TOTAL")
    print("\nreproduces exactly but NO doc line assigns it to the compiler - NOT removed:")
    for item in payload["reproduces_but_not_assigned_by_docs"]:
        print(f"  - {item['file']} :: {item['pointer']}")
        print(f"      {item['arithmetic']}")
    print("\nretained because it is an operand (or an assertion's input):")
    for key in payload["retained_because_operand"]:
        print(f"  - {key}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
