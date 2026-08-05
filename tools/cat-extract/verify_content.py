#!/usr/bin/env python3
"""Verify the CAT-stream gameplay catalog transcription under content/.

Standard library only. Run from anywhere:

    python3 tools/cat-extract/verify_content.py

Checks performed (see tools/cat-extract/README.md):
  1. every .json under content/ parses (hard failure on any parse error)
  2. per-directory entry counts match the EXPECTATIONS table below
  3. every file carries a well-formed _provenance block whose doc path exists
  4. the two doc-stated totals recompute (PowerUp ranks 9,450; unlocks 2,150)
  5. referential integrity for branch -> weapon, encounter -> enemy, mech -> weapon
  6. entries with a null id are reported as warnings, not failures

Exit code is non-zero if any check fails. Warnings never change the exit code.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CONTENT = REPO_ROOT / "content"

# --------------------------------------------------------------------------
# EXPECTATIONS
#
# Every row cites the design-doc line the count comes from. The aggregate
# totals are restated at docs/technical/110-implementation-plan-for-ai-agents.md:181
# (M5 gate) and docs/technical/40-content-data-and-validation.md:120.
#
# selector  : how a file is classified as a catalog "item"
#             ("id_regex", pattern) - top-level "id" matches pattern
#             ("has_key", key)      - top-level object has that key
#             ("any_file",)         - every .json file in the directory
# items     : expected number of item files (None = do not assert)
# aggregates: expected number of non-item files (None = do not assert)
# --------------------------------------------------------------------------

EXPECTATIONS = [
    dict(
        dir="resources",
        selector=("id_regex", r"(?i)^(?:[A-F]|common[-_ ]?ore|hyper[-_ ]?gold)$"),
        items=8,
        aggregates=None,
        label="resources (6 specialized + common ore + Hyper Gold)",
        # 6 specialized materials: docs/61-specialized-resource-identities.md:20
        # common ore + Hyper Gold: docs/60-resources-crafting-progression.md:18
        # "8 resources" restated: docs/technical/110-implementation-plan-for-ai-agents.md:181
        source="docs/61-specialized-resource-identities.md:20 + docs/60-resources-crafting-progression.md:18",
    ),
    dict(
        dir="mechs",
        selector=("id_regex", r"^MCH-\d{2}$"),
        items=6,
        aggregates=None,
        label="mechs",
        # docs/36-initial-mech-catalog.md:45 (Catalog overview table, 6 rows)
        source="docs/36-initial-mech-catalog.md:45",
    ),
    dict(
        dir="enemies",
        selector=("id_regex", r"^EN-\d{2}$"),
        items=10,
        aggregates=1,
        label="ordinary enemies (+ 1 elite modifier profile aggregate)",
        # 10 ordinary enemies: docs/31-initial-alien-roster.md:37
        # elite modifier profile: docs/31-initial-alien-roster.md:104
        source="docs/31-initial-alien-roster.md:37 + docs/31-initial-alien-roster.md:104",
    ),
    dict(
        dir="bosses",
        selector=("id_regex", r"^BOSS-\d{2}$"),
        items=4,
        aggregates=None,
        label="interval bosses",
        # docs/31-initial-alien-roster.md:121 (Interval boss overview table, 4 rows)
        source="docs/31-initial-alien-roster.md:121",
    ),
    dict(
        dir="weapons",
        selector=("id_regex", r"^W-[A-F]{2}$"),
        items=15,
        aggregates=None,
        label="base weapons",
        # docs/66-weapon-catalog-and-resource-graph.md:39 (15 rows = C(6,2))
        source="docs/66-weapon-catalog-and-resource-graph.md:39",
    ),
    dict(
        dir="branches",
        selector=("has_key", "weaponId"),
        items=45,
        aggregates=None,
        label="weapon branches (15 weapons x 3)",
        # docs/71-initial-weapon-numeric-catalog.md:130-491 (45 branch sections)
        source="docs/71-initial-weapon-numeric-catalog.md:130",
    ),
    dict(
        dir="utilities",
        selector=("id_regex", r"^UTL-"),
        items=12,
        aggregates=1,
        label="utilities (12 with UTL-* IDs + resource radar, which has no ID)",
        # 12 material utilities: docs/68-utility-catalog.md:35
        # resource radar as 13th utility: docs/50-maps-resources-and-navigation.md:106
        # "12 utilities plus radar": docs/technical/110-implementation-plan-for-ai-agents.md:181
        source="docs/68-utility-catalog.md:35 + docs/50-maps-resources-and-navigation.md:106",
    ),
    dict(
        dir="relics",
        selector=("id_regex", r"^REL-\d{2}$"),
        items=10,
        aggregates=None,
        label="relics",
        # docs/69-initial-relic-catalog.md:26 (Catalog overview table, 10 rows)
        source="docs/69-initial-relic-catalog.md:26",
    ),
    dict(
        dir="powerups",
        selector=("id_regex", r"^PU-"),
        items=13,
        aggregates=None,
        label="permanent PowerUps",
        # docs/62-permanent-powerup-catalog.md:35 (Catalog overview table, 13 rows)
        source="docs/62-permanent-powerup-catalog.md:35",
    ),
    dict(
        dir="unlocks",
        selector=("id_regex", r"^UNL-\d{2}$"),
        items=6,
        aggregates=None,
        label="permanent option unlocks",
        # docs/63-permanent-option-unlock-catalog.md:48 (Catalog overview table, 6 rows)
        source="docs/63-permanent-option-unlock-catalog.md:48",
    ),
    dict(
        dir="mining-sites",
        selector=("any_file",),
        items=4,
        aggregates=None,
        label="mining site classes (prose-only, no IDs in any doc)",
        # docs/40-mining-and-extraction.md:58-132: standard seam, rich seam,
        # specialized-material geode, Hyper Gold site
        source="docs/40-mining-and-extraction.md:58",
    ),
    dict(
        dir="encounters",
        selector=("any_file",),
        items=None,
        aggregates=None,
        label="encounter schedule aggregates (counted by probe, not by file)",
        # docs/32-standard-wave-and-beacon-schedule.md - see PROBES below
        source="docs/32-standard-wave-and-beacon-schedule.md:54",
    ),
    dict(
        dir="maps",
        selector=("any_file",),
        items=None,
        aggregates=None,
        label="map contract + 2 world props (counted by probe: props may be one aggregate file)",
        # map contract: docs/51-standard-map-generation-contract.md (whole doc)
        # health pack: docs/72-player-survivability-and-damage-baseline.md:180
        # destructible rock: docs/72-player-survivability-and-damage-baseline.md:190
        source="docs/51-standard-map-generation-contract.md:1 + docs/72-player-survivability-and-damage-baseline.md:180",
    ),
]

# Row-level probes for aggregate files where the entry count lives inside an array.
PROBES = [
    dict(
        dir="encounters",
        label="35-minute schedule rows (minutes 0-34)",
        expected=35,
        kind="dicts_with_key",
        key="minute",
        # docs/32-standard-wave-and-beacon-schedule.md:54 (35 table rows)
        source="docs/32-standard-wave-and-beacon-schedule.md:54",
    ),
    dict(
        dir="encounters",
        label="Hyper Gold threat-beacon responses",
        expected=4,
        kind="dicts_with_key",
        key="trigger",
        # docs/32-standard-wave-and-beacon-schedule.md:100 (4 table rows)
        source="docs/32-standard-wave-and-beacon-schedule.md:100",
    ),
    dict(
        dir="encounters",
        label="formation grammar entries",
        expected=7,
        kind="array_at_key",
        pattern=r"(?i)^(?:spawn)?formations$",
        # docs/32-standard-wave-and-beacon-schedule.md:27 (7 bullets)
        source="docs/32-standard-wave-and-beacon-schedule.md:27",
    ),
    dict(
        dir="powerups",
        label="PowerUp rank rows across all 13 PowerUps",
        expected=58,
        kind="array_at_key",
        pattern=r"(?i)^ranks?$",
        # docs/62-permanent-powerup-catalog.md rank tables at L57, 71, 85, 98,
        # 113, 127, 139, 153, 167, 181, 197, 212, 226 -> 5+5+5+4+5+3+5+1+5+5+5+5+5
        source="docs/62-permanent-powerup-catalog.md:57",
    ),
    dict(
        dir="maps",
        label="map generation contract document",
        expected=1,
        kind="files_matching",
        pattern=r"(?i)contract",
        # docs/51-standard-map-generation-contract.md (whole doc, one aggregate)
        source="docs/51-standard-map-generation-contract.md:1",
    ),
    dict(
        dir="maps",
        label="world prop entries (destructible rock, health pack)",
        expected=2,
        kind="array_at_key",
        pattern=r"(?i)^(?:world)?props$",
        # docs/72-player-survivability-and-damage-baseline.md:180 (health pack) and :190 (rock)
        source="docs/72-player-survivability-and-damage-baseline.md:180",
    ),
]

# Doc-stated grand totals the transcription must reproduce.
POWERUP_TOTAL_HYPER_GOLD = 9450  # docs/62-permanent-powerup-catalog.md:35 (sum of Total cost)
UNLOCK_TOTAL_HYPER_GOLD = 2150  # docs/63-permanent-option-unlock-catalog.md:48 (sum of Cost)

PROVENANCE_REQUIRED = ("doc", "section", "lines", "extractedFor")

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
# 1. parse everything
# --------------------------------------------------------------------------


def load_all() -> dict[Path, object]:
    docs: dict[Path, object] = {}
    if not CONTENT.is_dir():
        fail(f"content/ directory not found at {CONTENT}")
        return docs
    for path in sorted(CONTENT.rglob("*.json")):
        try:
            with path.open(encoding="utf-8") as fh:
                docs[path] = json.load(fh)
        except (json.JSONDecodeError, UnicodeDecodeError) as exc:
            fail(f"PARSE ERROR {rel(path)}: {exc}")
    return docs


# --------------------------------------------------------------------------
# generic traversal helpers
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


def strings(obj):
    for _, _, value in walk(obj):
        if isinstance(value, str):
            yield value


def files_in(directory: str, docs: dict[Path, object]) -> dict[Path, object]:
    base = CONTENT / directory
    return {p: d for p, d in docs.items() if base in p.parents or p.parent == base}


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
# 2. per-directory counts
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
# probes
# --------------------------------------------------------------------------


def probe_dicts_with_key(present: dict[Path, object], key: str) -> tuple[int, list[str]]:
    total = 0
    found: list[str] = []
    for path, doc in sorted(present.items()):
        for jpath, _, value in walk(doc):
            if (
                isinstance(value, list)
                and value
                and all(isinstance(e, dict) and key in e for e in value)
            ):
                total += len(value)
                found.append(f"{rel(path)}{jpath[1:]} ({len(value)})")
    return total, found


def probe_array_at_key(present: dict[Path, object], pattern: str) -> tuple[int, list[str]]:
    """Sum the lengths of top-level arrays whose key matches `pattern`.

    Only depth-1 keys are considered: a catalog of N things is a top-level array
    in its aggregate file, not a nested per-row field of the same name.
    """
    rx = re.compile(pattern)
    total = 0
    found: list[str] = []
    for path, doc in sorted(present.items()):
        hits = []
        if isinstance(doc, dict):
            hits = [
                (f"$.{key}", value)
                for key, value in doc.items()
                if rx.search(key) and isinstance(value, list) and value
            ]
        if not hits and rx.search(path.stem):
            # aggregate file named for the concept: take its largest list of dicts
            lists = [
                (jpath, value)
                for jpath, _, value in walk(doc)
                if isinstance(value, list) and value and all(isinstance(e, dict) for e in value)
            ]
            if lists:
                hits = [max(lists, key=lambda pair: len(pair[1]))]
        for jpath, value in hits:
            total += len(value)
            found.append(f"{rel(path)}{jpath[1:]} ({len(value)})")
    return total, found


def check_probes(docs: dict[Path, object]) -> list[tuple]:
    rows = []
    for spec in PROBES:
        present = files_in(spec["dir"], docs)
        if spec["kind"] == "dicts_with_key":
            actual, found = probe_dicts_with_key(present, spec["key"])
        elif spec["kind"] == "files_matching":
            rx = re.compile(spec["pattern"])
            matched = [p for p in sorted(present) if rx.search(p.stem)]
            actual, found = len(matched), [rel(p) for p in matched]
        else:
            actual, found = probe_array_at_key(present, spec["pattern"])
        status = "ok" if actual == spec["expected"] else "FAIL"
        if status == "FAIL":
            fail(
                f"content/{spec['dir']}/: expected {spec['expected']} {spec['label']}, "
                f"found {actual} (source {spec['source']}; matched {found or 'nothing'})"
            )
        rows.append((spec["dir"], spec["label"], spec["expected"], actual, status))
    return rows


# --------------------------------------------------------------------------
# 3. provenance
# --------------------------------------------------------------------------


def check_provenance(docs: dict[Path, object]) -> int:
    checked = 0
    extracted_for: dict[str, int] = {}
    for path, doc in sorted(docs.items()):
        if not isinstance(doc, dict):
            fail(f"{rel(path)}: top-level JSON value is {type(doc).__name__}, expected an object")
            continue
        prov = doc.get("_provenance")
        if prov is None:
            fail(f"{rel(path)}: missing _provenance block")
            continue
        if not isinstance(prov, dict):
            fail(f"{rel(path)}: _provenance is {type(prov).__name__}, expected an object")
            continue
        checked += 1
        for field in PROVENANCE_REQUIRED:
            if not prov.get(field):
                fail(f"{rel(path)}: _provenance.{field} missing or empty")
        if "notes" not in prov:
            warn(f"{rel(path)}: _provenance has no notes array")
        tag = prov.get("extractedFor")
        if isinstance(tag, str):
            extracted_for[tag] = extracted_for.get(tag, 0) + 1
        doc_ref = prov.get("doc")
        if isinstance(doc_ref, str) and doc_ref:
            target = REPO_ROOT / doc_ref.split("#")[0].split(":")[0]
            if not target.is_file():
                fail(f"{rel(path)}: _provenance.doc '{doc_ref}' does not exist on disk")
        # nested _source blocks must name a real doc too
        for jpath, key, value in walk(doc):
            if key == "_source" and isinstance(value, dict):
                blocks = [value] if "doc" in value else [v for v in value.values() if isinstance(v, dict)]
                for block in blocks:
                    ref = block.get("doc")
                    if not isinstance(ref, str) or not ref:
                        fail(f"{rel(path)}{jpath[1:]}: _source block has no doc reference")
                        continue
                    if not (REPO_ROOT / ref.split("#")[0].split(":")[0]).is_file():
                        fail(f"{rel(path)}{jpath[1:]}: _source doc '{ref}' does not exist on disk")
    if len(extracted_for) > 1:
        warn(
            "_provenance.extractedFor is not consistent across content/: "
            + ", ".join(f"{k}={v} file(s)" for k, v in sorted(extracted_for.items()))
            + " (catalog transcription is DAT-007; DAT-008 is report generation, "
            "docs/technical/110-implementation-plan-for-ai-agents.md:216-217)"
        )
    return checked


# --------------------------------------------------------------------------
# 4. doc-stated totals
# --------------------------------------------------------------------------


def numeric_under(obj, key_pattern: str) -> list[float]:
    rx = re.compile(key_pattern)
    out: list[float] = []
    for _, key, value in walk(obj):
        if not key or not rx.search(key):
            continue
        if isinstance(value, bool):
            continue
        if isinstance(value, (int, float)):
            out.append(value)
        elif isinstance(value, dict):
            for _, subkey, subvalue in walk(value):
                if (
                    subkey
                    and not isinstance(subvalue, bool)
                    and isinstance(subvalue, (int, float))
                    and re.search(r"(?i)hyper|gold|amount|value|ore|cost", subkey)
                ):
                    out.append(subvalue)
                    break
    return out


def check_totals(docs: dict[Path, object]) -> list[tuple]:
    rows = []

    # PowerUps: sum of every rank price must equal the doc's grand total.
    powerups = files_in("powerups", docs)
    rank_prices: list[float] = []
    stated_totals: list[float] = []
    for _, doc in sorted(powerups.items()):
        if not isinstance(doc, dict) or not isinstance(doc.get("id"), str):
            continue
        for _, key, value in walk(doc):
            if key and re.match(r"(?i)^ranks?$", key) and isinstance(value, list):
                for entry in value:
                    if isinstance(entry, dict):
                        rank_prices.extend(numeric_under(entry, r"(?i)^price"))
        stated_totals.extend(numeric_under(doc, r"(?i)^total.*cost"))
    rank_sum = int(sum(rank_prices))
    stated_sum = int(sum(stated_totals))
    rows.append(
        ("PowerUp rank prices", POWERUP_TOTAL_HYPER_GOLD, rank_sum, "ok" if rank_sum == POWERUP_TOTAL_HYPER_GOLD else "FAIL")
    )
    rows.append(
        ("PowerUp stated totalCost", POWERUP_TOTAL_HYPER_GOLD, stated_sum, "ok" if stated_sum == POWERUP_TOTAL_HYPER_GOLD else "FAIL")
    )
    if rank_sum != POWERUP_TOTAL_HYPER_GOLD:
        fail(
            f"PowerUp rank prices sum to {rank_sum} Hyper Gold across {len(rank_prices)} rank rows, "
            f"expected {POWERUP_TOTAL_HYPER_GOLD} (docs/62-permanent-powerup-catalog.md:35)"
        )
    if stated_sum != POWERUP_TOTAL_HYPER_GOLD:
        fail(
            f"PowerUp per-entry total costs sum to {stated_sum} Hyper Gold, "
            f"expected {POWERUP_TOTAL_HYPER_GOLD} (docs/62-permanent-powerup-catalog.md:35)"
        )

    # Unlocks: sum of the six unlock costs.
    unlocks = files_in("unlocks", docs)
    unlock_costs: list[float] = []
    for _, doc in sorted(unlocks.items()):
        if not isinstance(doc, dict) or not isinstance(doc.get("id"), str):
            continue
        if not re.match(r"^UNL-\d{2}$", doc["id"]):
            continue
        # cost is a top-level field; nested costs (if any) must not be summed
        found = [
            value
            for key, value in doc.items()
            if re.match(r"(?i)^(?:unlock)?cost", key)
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
        ("Unlock costs", UNLOCK_TOTAL_HYPER_GOLD, unlock_sum, "ok" if unlock_sum == UNLOCK_TOTAL_HYPER_GOLD else "FAIL")
    )
    if unlock_sum != UNLOCK_TOTAL_HYPER_GOLD:
        fail(
            f"option unlock costs sum to {unlock_sum} Hyper Gold across {len(unlock_costs)} unlocks, "
            f"expected {UNLOCK_TOTAL_HYPER_GOLD} (docs/63-permanent-option-unlock-catalog.md:48)"
        )
    return rows


# --------------------------------------------------------------------------
# 5. referential integrity
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

    # branches -> weapons
    dangling = []
    refs = 0
    for path, doc in sorted(files_in("branches", docs).items()):
        if not isinstance(doc, dict):
            continue
        for _, key, value in walk(doc):
            if key == "weaponId" and isinstance(value, str):
                refs += 1
                if value not in weapon_ids:
                    dangling.append(f"{rel(path)} -> {value}")
    rows.append(("branches.weaponId -> content/weapons/", refs, len(dangling), "ok" if not dangling else "FAIL"))
    if dangling:
        fail(f"{len(dangling)} branch weaponId reference(s) do not resolve to a weapon file: {dangling[:10]}")

    # encounters -> enemies
    dangling = []
    refs = 0
    seen: set[str] = set()
    for path, doc in sorted(files_in("encounters", docs).items()):
        for value in strings(doc):
            for token in re.findall(r"\bEN-\d{2}\b", value):
                refs += 1
                if token not in enemy_ids and token not in seen:
                    seen.add(token)
                    dangling.append(f"{rel(path)} -> {token}")
    rows.append(("encounters -> content/enemies/", refs, len(dangling), "ok" if not dangling else "FAIL"))
    if dangling:
        fail(f"encounter schedule references enemy IDs with no enemy file: {dangling[:15]}")

    # mechs -> signature weapons
    dangling = []
    refs = 0
    for path, doc in sorted(files_in("mechs", docs).items()):
        if not isinstance(doc, dict):
            continue
        for _, key, value in walk(doc):
            if key and re.search(r"(?i)signature.*weapon", key) and isinstance(value, str):
                if re.match(r"^W-[A-F]{2}$", value):
                    refs += 1
                    if value not in weapon_ids:
                        dangling.append(f"{rel(path)} -> {value}")
    rows.append(("mechs signature weapon -> content/weapons/", refs, len(dangling), "ok" if not dangling else "FAIL"))
    if dangling:
        fail(f"{len(dangling)} mech signature-weapon reference(s) do not resolve: {dangling}")
    return rows


# --------------------------------------------------------------------------
# 6. null ids (known missing-stable-ID cases -> warnings)
# --------------------------------------------------------------------------


def check_null_ids(docs: dict[Path, object]) -> list[str]:
    hits = []
    for path, doc in sorted(docs.items()):
        if isinstance(doc, dict) and "id" in doc and doc["id"] is None:
            hits.append(f"{rel(path)} (top-level id is null)")
        for jpath, key, value in walk(doc):
            if key == "id" and value is None and jpath != "$.id":
                hits.append(f"{rel(path)}{jpath[1:]} is null")
    for hit in hits:
        warn(f"null id: {hit}")
    return hits


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
    print("CAT content verification")
    print(f"repo:    {REPO_ROOT}")
    print(f"content: {rel(CONTENT)}")

    docs = load_all()
    print(f"parsed:  {len(docs)} JSON file(s)")

    count_rows = check_counts(docs)
    probe_rows = check_probes(docs)
    provenance_checked = check_provenance(docs)
    total_rows = check_totals(docs)
    ref_rows = check_references(docs)
    null_ids = check_null_ids(docs)

    table(
        "Per-directory entry counts",
        ("directory", "catalog", "expected", "actual", "status"),
        count_rows,
    )
    table("Aggregate row probes", ("directory", "rows", "expected", "actual", "status"), probe_rows)
    table("Doc-stated totals (Hyper Gold)", ("total", "expected", "actual", "status"), total_rows)
    table("Referential integrity", ("check", "refs", "dangling", "status"), ref_rows)

    print(f"\n_provenance blocks validated: {provenance_checked}")
    print(f"null ids (known missing-stable-ID cases): {len(null_ids)}")
    for hit in null_ids:
        print(f"  - {hit}")

    if warnings:
        print(f"\nWARNINGS ({len(warnings)}):")
        for message in warnings:
            print(f"  ! {message}")

    if failures:
        print(f"\nFAILURES ({len(failures)}):")
        for message in failures:
            print(f"  x {message}")
        print("\nRESULT: FAIL")
        return 1

    print("\nRESULT: PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
