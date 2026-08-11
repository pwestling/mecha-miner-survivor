# DAT-006 census instrument, preserved as a program listing

This document holds the derivation program that produced the figures recorded in
`VER-DAT-006-007` of `tests/verification/DAT-006.json`: the ClosedVocabulary census over
declaration sites and their declared pointers, whose "direction 2" that entry withdraws as
never measured. Nothing in this repository runs this program. It is kept because an
instrument whose only record is a conversation cannot be re-run by a later reader, and the
entry's figures are checkable only to the extent that the program which produced them is.

## The pin

The instrument as it produced every figure in `VER-DAT-006-007` is **54,840 bytes**, SHA-256
**`c2cd5c115737783d63c1f2ce8cfc77786856fcce0fdd42f6e9a500b52b8e8564`**. The listing under
[The instrument](#the-instrument) below is those 54,840 bytes exactly, its `#!/usr/bin/env
python3` first line included, so this document carries the bytes the pin names rather than
only asserting a hash for bytes held somewhere else.

## How to run it

Extract the fenced block's contents to a `.py`, check it against the pin, and run it with
Python 3 from the repository root:

    awk '/^```python$/{f=1;next} /^```$/{f=0} f' \
      tests/verification/instruments/dat-006-derive.md > derive.py
    sha256sum derive.py
    python3 derive.py

Only the fence's own two lines match those patterns; the command above is inside an indented
block and matches neither. The listing carries its own shebang, so nothing has to be restored
or repaired after extraction; `sha256sum` must print the hash above, and if it prints anything
else then the extraction is wrong and its output is not this instrument's output.

## Why this is a document rather than a script

It is a document because nothing executes it: no verb, workflow or gate invokes this program,
and a gate has no more reason to enumerate a program listing inside a markdown file than it
has to enumerate a code block in a design document. This is deliberately **not** a `.py` with
its shebang removed. `build/verify-gate-wiring.sh` enumerates scripts twice over, by
extension against `SCRIPT_EXTENSIONS` (`sh bash ps1 psm1 py zsh ksh pl rb`) and by reading a
file's first two bytes for `#!`, and it refuses that manoeuvre by name: a file that "is not a
script at all and merely begins with those two bytes" is given no escape, because adding one
"is a deliberate change to what 'script' means here, not a workaround to reach for in
passing". A markdown file whose first two bytes are `# ` is outside both enumerators for the
honest reason - `md` is not in `SCRIPT_EXTENSIONS`, the first two bytes are not `#!`, and it
is not a script - rather than by wearing an extension chosen to defeat them.

The form also keeps the pin intact under `./build.sh format`. `.md` and `.txt` are both owned
extensions in `OwnedTextHygiene`, but `.md` and `.markdown` alone are exempt from
`trim_trailing_whitespace`, because Markdown gives trailing double spaces meaning. This
listing has 13 lines with trailing whitespace. Under any owned non-Markdown extension the
formatter would trim them, and a reformatted instrument no longer reproduces its own output;
under `.md` the bytes survive the gate untouched.

## When to delete this document

If this instrument is later promoted to a real `.py` classified under a new `instrument` kind
in `build/verify-gate-wiring.sh`'s taxonomy - that file's `KNOWN_KINDS` today are `gate`,
`launcher`, `provisioning` and `library`, and none of them fits a program that decides nothing
and that no verb invokes - then this document is redundant and should be deleted in that same
commit, with `VER-DAT-006-007`'s summary repointed to the script's committed path. Two copies
of one program, one of them a listing nothing checks against the other, is the condition this
document exists to end rather than to create.

## The instrument

```python
#!/usr/bin/env python3
"""
DAT-006 direction 2, re-derived by DECLARED JSON POINTER instead of by leaf property name.

Direction 2 of the DAT-006 census asks the corpus-first question: for each field position
that actually appears in the authored corpus, what (if anything) governs its value set?
The recorded direction-2 pass keyed corpus fields by LEAF PROPERTY NAME, which sums
unrelated JSON pointers that happen to end in the same word. This script keys every
occurrence by its full normalised JSON pointer, which is the same key direction 1
(VER-DAT-006-006) used, so the two directions become commensurable.

Fully deterministic: no timestamps, no wall-clock, no filesystem-order dependence, every
collection sorted before it is printed. Run with --reverse to reverse the input file list;
the output must be byte-identical (determinism control, section 9).

Usage:  python3 derive.py [--reverse]
"""

import collections
import glob
import json
import os
import re
import sys

# ---------------------------------------------------------------------------
# CONFIGURATION -- every knob is named here and printed in the output header.
# ---------------------------------------------------------------------------

ROOT = "/workspace/mecha-miner-survivor"

# Population. Recovered in Part 1: direction 1 reports "139 corpus files"; the glob below
# is the only one on this tree that yields exactly 139, and the leaf-name count over it is
# exactly the 503 that direction 2 reported as "examined".
CORPUS_GLOB = "content/**/*.json"
CORPUS_EXCLUDE_PREFIXES = ("content/schemas/",)

# Pointer normalisation. Direction 1's rule, verbatim from VER-DAT-006-006:
#   "producing a template with * for an array index"
# So an array index becomes the literal segment `*`. The task brief's example wrote `N`;
# `*` is used here because commensurability with direction 1 is the point, and direction
# 1's own recorded row spellings (/specialist_attack/projectile/snapshot_at_creation/*,
# /resonance_field/applies_to/*, /minute_rows/*/formation_events/*/formations/*) are `*`.
ARRAY_SEGMENT = "*"

# Arrays of scalars: a scalar sitting directly inside an array gets a pointer ENDING in
# `/*`. Decided this way because direction 1's declared pointers do exactly that -
# CombatShapes.SnapshotProperties is recorded at
# /specialist_attack/projectile/snapshot_at_creation/* and MiningSiteSchema.ResonanceTargets
# at /resonance_field/applies_to/*, both arrays of strings. Applied consistently everywhere.

# Sources searched for code declarations and grammars.
SRC_DIRS = ("src/MechaMiner.Content",)
SCHEMA_DIR = "content/schemas"

# Which schema governs which corpus directory. NOT recoverable from a single table in
# src/: AuthoredCorpus.cs carries only the two exception rows (NoCorpus, Excluded). This
# map is stated as an assumption and every governed-by-schema row prints the schema it
# came from so a reader can reject it.
SCHEMA_BINDING = {
    "boss.schema.json": ["content/bosses/"],
    "branch.schema.json": ["content/branches/"],
    "elite-modifiers.schema.json": ["content/enemies/shared-elite-modifiers.json"],
    "encounter-schedule.schema.json": ["content/encounters/"],
    "enemy.schema.json": ["content/enemies/"],
    "map-generation-contract.schema.json": ["content/maps/"],
    "mech.schema.json": ["content/mechs/"],
    "mining-site.schema.json": ["content/mining-sites/"],
    "player-baseline.schema.json": [],          # content/player/ does not exist
    "powerup.schema.json": ["content/powerups/"],
    "relic.schema.json": ["content/relics/"],
    "resource.schema.json": ["content/resources/"],
    "unlock.schema.json": ["content/unlocks/"],
    "utility.schema.json": ["content/utilities/"],
    "weapon.schema.json": ["content/weapons/"],
    "weapon-stat-price-formula.schema.json": ["content/weapons/"],
    "envelope.schema.json": [],                 # excluded by AuthoredCorpus.Excluded
}

# Admission knobs (section 6). Defaults are ONE point in the space; the sensitivity table
# sweeps them. No single setting is presented as the answer.
MIN_OCCURRENCES = 2
MAX_DISTINCT = 10
VALUE_SHAPE_FILTER = True

# The leaf names direction 2 relayed. NOT recoverable from the artifact (Part 1(d)); the
# three the independent reviewer quoted verbatim are listed first and marked, the rest of
# the split table is over every leaf name that splits at all.
REVIEWER_QUOTED_LEAVES = ("currency", "scope", "shape")


def rel(path):
    return os.path.relpath(path, ROOT)


# ---------------------------------------------------------------------------
# 1. POPULATION
# ---------------------------------------------------------------------------

def corpus_files():
    hits = glob.glob(os.path.join(ROOT, CORPUS_GLOB), recursive=True)
    keep = []
    for h in hits:
        r = rel(h)
        if any(r.startswith(p) for p in CORPUS_EXCLUDE_PREFIXES):
            continue
        keep.append(r)
    # Explicit sort. Never rely on filesystem order: a prior census was nondeterministic
    # because directory iteration order decided which of two same-named vocabularies
    # survived (UtilitySchema.PoolAvailabilities remark).
    return sorted(keep)


# ---------------------------------------------------------------------------
# 2/3. KEYING AND PER-POINTER FACTS
# ---------------------------------------------------------------------------

def jtype(v):
    if v is None:
        return "null"
    if isinstance(v, bool):
        return "bool"
    if isinstance(v, int):
        return "int"
    if isinstance(v, float):
        return "float"
    if isinstance(v, str):
        return "string"
    raise AssertionError("not a scalar: %r" % (v,))


class Occurrence:
    __slots__ = ("pointer", "leaf", "value", "file")

    def __init__(self, pointer, leaf, value, file):
        self.pointer = pointer
        self.leaf = leaf
        self.value = value
        self.file = file


def census(files):
    """Every scalar-valued leaf occurrence, keyed by normalised JSON pointer."""
    out = []
    unreadable = []

    def walk(node, pointer, leaf, path):
        if isinstance(node, dict):
            for key in sorted(node):                     # sorted: determinism
                walk(node[key], pointer + "/" + key, key, path)
        elif isinstance(node, list):
            for item in node:
                walk(item, pointer + "/" + ARRAY_SEGMENT, leaf, path)
        else:
            out.append(Occurrence(pointer, leaf, node, path))

    for f in files:
        try:
            with open(os.path.join(ROOT, f), encoding="utf-8") as fh:
                doc = json.load(fh)
        except Exception as exc:                          # pragma: no cover
            unreadable.append((f, type(exc).__name__ + ": " + str(exc)))
            continue
        walk(doc, "", None, f)

    return out, unreadable


class PointerFacts:
    def __init__(self, pointer):
        self.pointer = pointer
        self.leaf = pointer.rsplit("/", 1)[-1] if "/" in pointer else pointer
        self.count = 0
        self.values = collections.Counter()
        self.types = set()
        self.files = set()

    @property
    def distinct(self):
        return len(self.values)

    def value_list(self):
        return sorted(self.values)

    def type_list(self):
        return sorted(self.types)


def pointer_facts(occurrences):
    facts = {}
    for occ in occurrences:
        f = facts.get(occ.pointer)
        if f is None:
            f = facts[occ.pointer] = PointerFacts(occ.pointer)
        f.count += 1
        f.values[json.dumps(occ.value, sort_keys=True)] += 1
        f.types.add(jtype(occ.value))
        f.files.add(occ.file)
    return facts


def leaf_of_pointer(pointer):
    """The leaf PROPERTY name of a normalised pointer: the last segment that is not `*`."""
    segments = [s for s in pointer.split("/") if s and s != ARRAY_SEGMENT]
    return segments[-1] if segments else "(root)"


# ---------------------------------------------------------------------------
# 4. CODE DECLARATIONS AND THEIR DECLARED POINTERS
# ---------------------------------------------------------------------------

CS_FILES_CACHE = {}


def cs_files():
    if "v" not in CS_FILES_CACHE:
        found = []
        for d in SRC_DIRS:
            found.extend(glob.glob(os.path.join(ROOT, d, "**", "*.cs"), recursive=True))
        CS_FILES_CACHE["v"] = sorted(rel(p) for p in found)
    return CS_FILES_CACHE["v"]


def read_cs(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as fh:
        return fh.read()


DECL_RE = re.compile(
    r"public\s+static\s+ClosedVocabulary\s+(\w+)\s*\{\s*get;\s*\}\s*=\s*new\(",
    re.S)
TYPE_RE = re.compile(
    r"^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|partial\s+|abstract\s+)*class\s+(\w+)",
    re.M)


def balanced_args(text, open_index):
    """Split the argument list starting at the '(' at open_index into top-level args."""
    assert text[open_index] == "("
    depth = 0
    i = open_index
    args = []
    cur = []
    in_str = False
    in_verbatim = False
    while i < len(text):
        ch = text[i]
        if in_str:
            cur.append(ch)
            if in_verbatim:
                if ch == '"':
                    if i + 1 < len(text) and text[i + 1] == '"':
                        cur.append('"')
                        i += 2
                        continue
                    in_str = in_verbatim = False
            else:
                if ch == "\\":
                    cur.append(text[i + 1])
                    i += 2
                    continue
                if ch == '"':
                    in_str = False
            i += 1
            continue
        if ch == '"':
            in_str = True
            in_verbatim = i > 0 and text[i - 1] == "@"
            cur.append(ch)
            i += 1
            continue
        if ch in "([{":
            depth += 1
            if depth == 1 and ch == "(":
                i += 1
                continue
            cur.append(ch)
            i += 1
            continue
        if ch in ")]}":
            depth -= 1
            if depth == 0:
                args.append("".join(cur).strip())
                return [a for a in args if a != ""], i
            cur.append(ch)
            i += 1
            continue
        if ch == "," and depth == 1:
            args.append("".join(cur).strip())
            cur = []
            i += 1
            continue
        cur.append(ch)
        i += 1
    raise AssertionError("unbalanced argument list")


def enclosing_type(text, index):
    best = None
    for m in TYPE_RE.finditer(text):
        if m.start() < index:
            best = m.group(1)
        else:
            break
    return best or "(unknown)"


def line_of(text, index):
    return text.count("\n", 0, index) + 1


def find_declarations():
    """Every ClosedVocabulary declaration, keyed by DeclaringType.PropertyName."""
    decls = {}
    for path in cs_files():
        text = read_cs(path)
        for m in DECL_RE.finditer(text):
            prop = m.group(1)
            open_paren = m.end() - 1
            args, _ = balanced_args(text, open_paren)
            literals = []
            for a in args:
                lit = re.fullmatch(r'"((?:[^"\\]|\\.)*)"', a.strip())
                literals.append(json.loads('"' + lit.group(1) + '"') if lit else None)
            subject = literals[0] if literals else None
            source = literals[1] if len(literals) > 1 else None
            tokens = [t for t in literals[2:] if t is not None]
            owner = enclosing_type(text, m.start())
            key = owner + "." + prop
            decls[key] = {
                "key": key, "type": owner, "property": prop,
                "subject": subject, "source": source, "tokens": tokens,
                "file": path, "symbol": key,
                "nonliteral_tokens": len([t for t in literals[2:] if t is None]),
            }
    return decls


ASSIGN_RE = re.compile(r"^[ \t]*JsonPointer\s+(\w+)\s*=\s*(.*?);", re.M | re.S)
METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:private|public|internal|protected|static|async|override|sealed|virtual|partial)\s+)*"
    r"[\w<>,\[\]\?\.]+\s+(\w+)\s*\(", re.M)


def local_assignments(text):
    out = collections.defaultdict(list)
    for m in ASSIGN_RE.finditer(text):
        out[m.group(1)].append((m.start(), re.sub(r"\s+", " ", m.group(2)).strip()))
    return out


def method_at(text, index):
    """(name, start_index, params_text) of the method declaration enclosing index."""
    best = None
    for m in METHOD_RE.finditer(text):
        if m.start() >= index:
            break
        try:
            args, close = balanced_args(text, m.end() - 1)
        except AssertionError:
            continue
        best = (m.group(1), m.start(), args)
    return best


CHAIN_RE = re.compile(r"\.\s*(AppendProperty|AppendIndex)\s*\(")


def split_chain(expr):
    """Split 'a.AppendProperty("x").AppendIndex(i)' into base and [(op, arg), ...]."""
    m = CHAIN_RE.search(expr)
    if m is None:
        return expr.strip(), []
    base = expr[:m.start()].strip()
    ops = []
    pos = m.start()
    while pos < len(expr):
        m2 = CHAIN_RE.match(expr, pos)
        if m2 is None:
            break
        args, close = balanced_args(expr, m2.end() - 1)
        ops.append((m2.group(1), args[0] if args else ""))
        pos = close + 1
    return base, ops


MAX_DEPTH = 8


def resolve_pointer(expr, path, index, depth=0, seen=None):
    """Resolve a JsonPointer expression to a set of normalised pointer templates.

    The resolution rule is direction 1's, verbatim from VER-DAT-006-006: "resolved by
    tracing JsonPointer variable assignments to the nearest preceding assignment above
    the call - method-local in practice". Where the nearest preceding assignment is not
    method-local -- the base is a method PARAMETER -- this walks one hop out to the
    method's call sites in the same file and unions what they pass. Anything that cannot
    be resolved is returned as an UNRESOLVED marker rather than guessed.
    """
    if seen is None:
        seen = set()
    if depth > MAX_DEPTH:
        return {"<UNRESOLVED:depth>"}

    text = read_cs(path)
    base, ops = split_chain(expr)

    suffix = ""
    for op, arg in ops:
        if op == "AppendIndex":
            suffix += "/" + ARRAY_SEGMENT
        else:
            lit = re.fullmatch(r'"((?:[^"\\]|\\.)*)"', arg.strip())
            if lit is None:
                suffix += "/<UNRESOLVED:nonliteral-property>"
            else:
                suffix += "/" + json.loads('"' + lit.group(1) + '"')

    if base in ("JsonPointer.Root", "default"):
        return {suffix}

    if not re.fullmatch(r"\w+", base):
        return {"<UNRESOLVED:expr:" + base + ">" + suffix}

    key = (path, base, index)
    if key in seen:
        return {"<UNRESOLVED:cycle>" + suffix}
    seen = seen | {key}

    # nearest preceding method-local assignment
    assigns = local_assignments(text)
    candidates = [(pos, rhs) for pos, rhs in assigns.get(base, []) if pos < index]
    if candidates:
        pos, rhs = max(candidates, key=lambda t: t[0])
        return {p + suffix for p in resolve_pointer(rhs, path, pos, depth + 1, seen)}

    # method parameter: one hop out to this file's call sites of the enclosing method
    meth = method_at(text, index)
    if meth is not None:
        name, mstart, params = meth
        position = None
        for i, p in enumerate(params):
            pm = re.match(r"^\s*(?:this\s+)?JsonPointer\s+(\w+)", p)
            if pm and pm.group(1) == base:
                position = i
                break
        if position is not None:
            results = set()
            for m in re.finditer(r"\b" + re.escape(name) + r"\s*\(", text):
                try:
                    args, _ = balanced_args(text, m.end() - 1)
                except AssertionError:
                    continue
                if len(args) <= position:
                    continue
                cand = args[position]
                if not re.search(r"JsonPointer|Append|pointer|Pointer|root|Root", cand):
                    continue
                if re.match(r"^\s*JsonPointer\s+\w+", cand):     # the declaration
                    continue
                results |= resolve_pointer(cand, path, m.start(), depth + 1, seen)
            if results:
                return {p + suffix for p in results}
    return {"<UNRESOLVED:parameter:" + base + ">" + suffix}


CALL_SPECS = {
    # call text                       vocab arg  pointer arg
    "SemanticCheck.Token": (1, 2),
    "SemanticCheck.BehaviorToken": (None, 1),
    "SemanticCheck.ReferenceGrammar": (1, 2),
}


def find_call_sites(call, vocab_index, pointer_index):
    rows = []
    needle = re.compile(re.escape(call) + r"\s*\(")
    for path in cs_files():
        text = read_cs(path)
        for m in needle.finditer(text):
            try:
                args, _ = balanced_args(text, m.end() - 1)
            except AssertionError:
                continue
            if len(args) <= pointer_index:
                continue
            vocab = args[vocab_index].strip() if vocab_index is not None else None
            ptr_expr = args[pointer_index].strip()
            pointers = sorted(resolve_pointer(ptr_expr, path, m.start()))
            rows.append({
                "call": call, "file": path, "line": line_of(text, m.start()),
                "vocab": vocab, "expr": re.sub(r"\s+", " ", ptr_expr),
                "pointers": pointers,
            })
    rows.sort(key=lambda r: (r["file"], r["line"]))
    return rows


def canonical_letter_sites():
    """ResourceSchema.CanonicalLetterPattern: pointer from the nearest preceding
    JsonPointer assignment above the IsCanonicalLetter call, direction 1's own rule."""
    rows = []
    for path in cs_files():
        text = read_cs(path)
        for m in re.finditer(r"ResourceSchema\.IsCanonicalLetter\s*\(", text):
            assigns = local_assignments(text)
            best = None
            for name, entries in assigns.items():
                for pos, rhs in entries:
                    if pos < m.start() and (best is None or pos > best[0]):
                        best = (pos, name, rhs)
            if best is None:
                continue
            pos, name, rhs = best
            rows.append({
                "file": path, "line": line_of(text, m.start()), "variable": name,
                "pointers": sorted(resolve_pointer(rhs, path, pos)),
            })
    rows.sort(key=lambda r: (r["file"], r["line"]))
    return rows


def const_string(symbol_regex):
    for path in cs_files():
        text = read_cs(path)
        m = re.search(symbol_regex, text)
        if m:
            return m.group(1), path
    return None, None


# ---------------------------------------------------------------------------
# JSON Schema enum/pattern positions
# ---------------------------------------------------------------------------

def schema_constraints():
    """(schema file) -> {instance pointer template: [(keyword, detail), ...]}."""
    out = {}
    for p in sorted(glob.glob(os.path.join(ROOT, SCHEMA_DIR, "*.schema.json"))):
        name = os.path.basename(p)
        with open(p, encoding="utf-8") as fh:
            doc = json.load(fh)
        found = collections.defaultdict(list)

        def walk(node, pointer, seen):
            if not isinstance(node, dict):
                return
            ref = node.get("$ref")
            if isinstance(ref, str) and ref.startswith("#/"):
                if (ref, pointer) not in seen:
                    target = doc
                    for seg in ref[2:].split("/"):
                        seg = seg.replace("~1", "/").replace("~0", "~")
                        if isinstance(target, dict) and seg in target:
                            target = target[seg]
                        else:
                            target = None
                            break
                    if target is not None:
                        walk(target, pointer, seen | {(ref, pointer)})
            if "enum" in node and isinstance(node["enum"], list):
                found[pointer].append(("enum", sorted(json.dumps(v, sort_keys=True) for v in node["enum"])))
            if "const" in node:
                found[pointer].append(("const", [json.dumps(node["const"], sort_keys=True)]))
            if isinstance(node.get("pattern"), str):
                found[pointer].append(("pattern", [node["pattern"]]))
            props = node.get("properties")
            if isinstance(props, dict):
                for k in sorted(props):
                    walk(props[k], pointer + "/" + k, seen)
            items = node.get("items")
            if isinstance(items, dict):
                walk(items, pointer + "/" + ARRAY_SEGMENT, seen)
            pre = node.get("prefixItems")
            if isinstance(pre, list):
                for sub in pre:
                    walk(sub, pointer + "/" + ARRAY_SEGMENT, seen)
            for kw in ("allOf", "anyOf", "oneOf"):
                branch = node.get(kw)
                if isinstance(branch, list):
                    for sub in branch:
                        walk(sub, pointer, seen)
            for kw in ("if", "then", "else", "not"):
                if isinstance(node.get(kw), dict):
                    walk(node[kw], pointer, seen)

        walk(doc, "", frozenset())
        out[name] = {k: sorted(v) for k, v in sorted(found.items())}
    return out


def schemas_for_file(corpus_file):
    hits = []
    for schema, prefixes in sorted(SCHEMA_BINDING.items()):
        for pre in prefixes:
            if pre.endswith("/"):
                if corpus_file.startswith(pre):
                    # enemy.schema.json does not govern the shared elite-modifier file
                    if schema == "enemy.schema.json" and "shared-elite-modifiers" in corpus_file:
                        continue
                    hits.append(schema)
            elif corpus_file == pre:
                hits.append(schema)
    return sorted(set(hits))


# ---------------------------------------------------------------------------
# 5. GOVERNANCE CLASSIFICATION
# ---------------------------------------------------------------------------

TOKEN_PATTERN_FALLBACK = "^[a-z][a-z0-9]*(-[a-z0-9]+)*$"


class Governance:
    def __init__(self, klass, evidence):
        self.klass = klass
        self.evidence = evidence
        self.all_classes = []
        self.all_evidence = {}


def build_classifier(decls, token_rows, behavior_rows, reference_rows, letter_rows,
                     constraints, token_pattern, letter_pattern, category_grammars):
    # declared pointer -> list of declaration keys (governed-here)
    declared = collections.defaultdict(set)
    decl_pointers = collections.defaultdict(set)
    for row in token_rows:
        vocab = row["vocab"]
        key = None
        if vocab:
            parts = vocab.split(".")
            if len(parts) >= 2:
                key = parts[-2] + "." + parts[-1]
        for ptr in row["pointers"]:
            declared[ptr].add(key or vocab or "(unknown)")
            if key:
                decl_pointers[key].add(ptr)

    behavior_pointers = set()
    for row in behavior_rows:
        behavior_pointers.update(row["pointers"])
    reference_pointers = {}
    for row in reference_rows:
        cat = (row["vocab"] or "").split(".")[-1]
        for ptr in row["pointers"]:
            reference_pointers.setdefault(ptr, set()).add(cat)
    letter_pointers = set()
    for row in letter_rows:
        letter_pointers.update(row["pointers"])

    # value-set index for governed-elsewhere
    by_tokens = {}
    for key, d in sorted(decls.items()):
        by_tokens[key] = set(d["tokens"])

    def evidence_for(facts, corpus_files_of_pointer):
        """Every governance class with evidence for this pointer, independent of
        precedence, so the printed report cannot hide one class behind another."""
        ptr = facts.pointer
        found = {}

        if ptr in declared:
            found["governed-here"] = ("declaration(s) whose own declared pointer is %s: %s"
                                      % (ptr, ", ".join(sorted(declared[ptr]))))

        schema_hits = []
        for cf in sorted(corpus_files_of_pointer):
            for schema in schemas_for_file(cf):
                for kw, detail in constraints.get(schema, {}).get(ptr, []):
                    if kw in ("enum", "pattern", "const"):
                        schema_hits.append((schema, kw, tuple(detail)))
        if schema_hits:
            found["governed-by-schema"] = "; ".join(
                "%s keyword %s -> %s" % (s, k, ", ".join(d)[:200])
                for s, k, d in sorted(set(schema_hits)))

        grammar = []
        if ptr in letter_pointers:
            grammar.append("ResourceSchema.CanonicalLetterPattern = %r applied at %s"
                           % (letter_pattern, ptr))
        if ptr in behavior_pointers:
            grammar.append("TokenGrammar.Pattern = %r via SemanticCheck.BehaviorToken at %s"
                           % (token_pattern, ptr))
        if ptr in reference_pointers:
            cats = sorted(reference_pointers[ptr])
            pats = []
            for c in cats:
                pats.extend(category_grammars.get(c, []))
            grammar.append("SemanticCheck.ReferenceGrammar at %s, ContentCategory %s, ID grammar %s"
                           % (ptr, "/".join(cats),
                              ", ".join(sorted(set(pats))) or "(not recovered)"))
        if grammar:
            found["governed-by-grammar"] = " | ".join(grammar)

        # governed-elsewhere: a declaration's member set COVERS this pointer's value set
        # while the declaration's own declared pointer differs. Necessary, nowhere near
        # sufficient -- the full evidence is printed so a reader can reject it.
        values = set()
        ok = True
        for enc in facts.values:
            v = json.loads(enc)
            if not isinstance(v, str):
                ok = False
                break
            values.add(v)
        if ok and values:
            matches = [k for k in sorted(by_tokens)
                       if by_tokens[k] and values <= by_tokens[k]
                       and ptr not in decl_pointers.get(k, set())]
            if matches:
                ev = []
                for key in matches:
                    where = sorted(decl_pointers.get(key, [])) or ["(no resolved declared pointer)"]
                    ev.append("declaration %s (declared at %s) members {%s} covers this pointer's "
                              "values {%s}"
                              % (key, ", ".join(where),
                                 ", ".join(sorted(by_tokens[key])),
                                 ", ".join(sorted(values))))
                found["governed-elsewhere"] = " | ".join(ev)
        return found

    # Precedence, stated: a declaration's OWN declared pointer outranks everything; then a
    # bound schema keyword; then a code grammar; then a value-set match at a different
    # pointer. Every class with evidence is also recorded on the result as .all_classes,
    # because a class that always loses the precedence tie-break would otherwise print 0
    # and be read as "nothing found".
    precedence = ("governed-here", "governed-by-schema", "governed-by-grammar",
                  "governed-elsewhere")

    def classify(facts, corpus_files_of_pointer):
        found = evidence_for(facts, corpus_files_of_pointer)
        for k in precedence:
            if k in found:
                g = Governance(k, found[k])
                g.all_classes = sorted(found)
                g.all_evidence = found
                return g
        g = Governance("ungoverned", "no declared pointer, no bound schema enum/pattern, "
                                     "no grammar, no declaration member set covers its values")
        g.all_classes = []
        g.all_evidence = {}
        return g

    return classify, declared, decl_pointers


# ---------------------------------------------------------------------------
# 6. ADMISSION CRITERION
# ---------------------------------------------------------------------------

TOKENISH = re.compile(r"^[A-Za-z][A-Za-z0-9]*([-_ ][A-Za-z0-9]+)*$")


def value_shape_ok(values):
    """The value-shape filter, as three named rejections. Direction 2's actual filter is
    NOT recoverable from the artifact (Part 1(c)); this is a stated stand-in, swept on and
    off in the sensitivity table so its contribution is visible rather than assumed."""
    for enc in values:
        v = json.loads(enc)
        if not isinstance(v, str):
            return False                       # (i) not a string: not a token space
        if v.endswith(".") or ", " in v or len(v.split()) > 4:
            return False                       # (ii) prose sentence
        if not TOKENISH.match(v):
            return False                       # (iii) not a token spelling
    return True


def admitted(facts_by_ptr, min_occ, max_distinct, shape_filter):
    out = []
    for ptr in sorted(facts_by_ptr):
        f = facts_by_ptr[ptr]
        if f.count < min_occ:
            continue
        if max_distinct is not None and f.distinct > max_distinct:
            continue
        if shape_filter and not value_shape_ok(f.values):
            continue
        out.append(ptr)
    return out


# ---------------------------------------------------------------------------
# MAIN
# ---------------------------------------------------------------------------

def main():
    reverse = "--reverse" in sys.argv
    files = corpus_files()
    order = list(reversed(files)) if reverse else files

    w = sys.stdout.write

    w("=" * 78 + "\n")
    w("DAT-006 DIRECTION 2, RE-DERIVED BY DECLARED JSON POINTER\n")
    w("=" * 78 + "\n\n")
    w("HEADER -- every input and rule stated, so the run is reproducible by hand.\n\n")
    w("  repository root      : %s\n" % ROOT)
    w("  corpus glob          : %s\n" % CORPUS_GLOB)
    w("  corpus exclusions    : %s\n" % ", ".join(CORPUS_EXCLUDE_PREFIXES))
    w("  files enumerated     : %d\n" % len(files))
    w("  file order            : explicitly sorted; --reverse reverses the input list\n")
    w("  this run's order     : %s\n" % ("REVERSED" if reverse else "sorted ascending"))
    w("  pointer normalisation: an array index becomes the literal segment %r.\n" % ARRAY_SEGMENT)
    w("                         Direction 1's rule verbatim from VER-DAT-006-006:\n")
    w("                         \"producing a template with * for an array index\".\n")
    w("  arrays of scalars    : a scalar inside an array yields a pointer ENDING in '/%s'.\n"
      % ARRAY_SEGMENT)
    w("                         Consistent with direction 1's own row spellings, e.g.\n")
    w("                         /specialist_attack/projectile/snapshot_at_creation/* and\n")
    w("                         /resonance_field/applies_to/*.\n")
    w("  scalars counted      : string, int, float, bool, null (every JSON scalar)\n")
    w("  code sources         : %s\n" % ", ".join(SRC_DIRS))
    w("  schema sources       : %s\n" % SCHEMA_DIR)
    w("  admission defaults   : min-occurrences=%d, max-distinct=%s, value-shape-filter=%s\n"
      % (MIN_OCCURRENCES, MAX_DISTINCT, VALUE_SHAPE_FILTER))
    w("\n")

    w("  file list (%d):\n" % len(files))
    for f in files:
        w("    %s\n" % f)
    w("\n")

    occurrences, unreadable = census(order)
    facts = pointer_facts(occurrences)
    strings = [o for o in occurrences if isinstance(o.value, str)]

    w("-" * 78 + "\n")
    w("SECTION 1-3. CENSUS AND PER-POINTER FACTS\n")
    w("-" * 78 + "\n\n")
    if unreadable:
        w("  FILES SKIPPED (unreadable):\n")
        for f, why in sorted(unreadable):
            w("    %s -- %s\n" % (f, why))
    else:
        w("  files skipped: none; all %d parsed\n" % len(files))
    w("  scalar leaf occurrences        : %d\n" % len(occurrences))
    w("  string-valued occurrences      : %d\n" % len(strings))
    w("  distinct normalised pointers   : %d\n" % len(facts))
    w("  distinct pointers, string-only : %d\n"
      % len({o.pointer for o in strings}))
    w("  distinct LEAF PROPERTY NAMES (the key direction 2 used), string-valued : %d\n"
      % len({leaf_of_pointer(o.pointer) for o in strings}))
    w("  distinct leaf property names, all scalars                              : %d\n"
      % len({leaf_of_pointer(o.pointer) for o in occurrences}))
    w("\n")

    # ---- declarations, grammars, schemas
    decls = find_declarations()
    token_rows = find_call_sites("SemanticCheck.Token", 1, 2)
    behavior_rows = find_call_sites("SemanticCheck.BehaviorToken", None, 1)
    reference_rows = find_call_sites("SemanticCheck.ReferenceGrammar", 1, 2)
    letter_rows = canonical_letter_sites()
    constraints = schema_constraints()
    token_pattern, token_pattern_file = const_string(
        r'public\s+const\s+string\s+Pattern\s*=\s*"([^"]*)"')
    letter_pattern, letter_pattern_file = const_string(
        r'public\s+const\s+string\s+CanonicalLetterPattern\s*=\s*"([^"]*)"')
    category_grammars = collections.defaultdict(list)
    for path in cs_files():
        text = read_cs(path)
        for m in re.finditer(r'Declare\(ContentCategory\.(\w+),\s*"[^"]*",\s*((?:"[^"]*",?\s*)+)\)', text):
            for pm in re.finditer(r'"([^"]*)"', m.group(2)):
                category_grammars[m.group(1)].append(pm.group(1))

    w("-" * 78 + "\n")
    w("SECTION 4a. CODE DECLARATIONS AND THEIR DECLARED POINTERS\n")
    w("-" * 78 + "\n\n")
    w("  ClosedVocabulary declarations found (keyed on DECLARING TYPE + PROPERTY, never on\n")
    w("  the property name alone -- UtilitySchema.PoolAvailabilities' own remark says name\n")
    w("  keying gives 25 rows against 27 declaration sites): %d\n\n" % len(decls))
    for key in sorted(decls):
        d = decls[key]
        w("    %-46s %-11s %s\n" % (key, "(%d tokens)" % len(d["tokens"]), rel(d["file"])))
    w("\n")
    w("  SemanticCheck.Token call sites: %d\n" % len(token_rows))
    resolved = [r for r in token_rows if not any("UNRESOLVED" in p for p in r["pointers"])]
    w("  fully resolved: %d   with an unresolved segment: %d\n"
      % (len(resolved), len(token_rows) - len(resolved)))
    all_declared = sorted({p for r in token_rows for p in r["pointers"]})
    w("  distinct declared pointer templates: %d\n" % len(all_declared))
    decl_pointer_pairs = set()
    for r in token_rows:
        vocab = r["vocab"] or "(unknown)"
        parts = vocab.split(".")
        key = parts[-2] + "." + parts[-1] if len(parts) >= 2 else vocab
        for p in r["pointers"]:
            decl_pointer_pairs.add((key, p))
    w("  distinct (declaration, declared pointer) ROWS: %d\n\n" % len(decl_pointer_pairs))
    w("  INSTRUMENT CROSS-CHECK against direction 1. VER-DAT-006-006 records, verbatim,\n")
    w("  \"Measured at aa550fb on claude/hearth-thread-hrufl9-dat-006 over 139 corpus files\n")
    w("  and 27 declaration sites yielding 33 rows\". This script, built independently for\n")
    w("  direction 2, reaches:\n")
    w("      corpus files      : %d   (direction 1: 139)  %s\n"
      % (len(files), "MATCH" if len(files) == 139 else "DIFFERS"))
    w("      declaration sites : %d   (direction 1: 27)   %s\n"
      % (len(decls), "MATCH" if len(decls) == 27 else "DIFFERS"))
    w("      rows              : %d   (direction 1: 33)   %s\n"
      % (len(decl_pointer_pairs), "MATCH" if len(decl_pointer_pairs) == 33 else "DIFFERS"))
    w("  Declarations resolving to MORE THAN ONE declared pointer (direction 1 records\n")
    w("  EncounterScheduleSchema.Formations as three, not two):\n")
    by_decl = collections.defaultdict(list)
    for k, p in decl_pointer_pairs:
        by_decl[k].append(p)
    for k in sorted(by_decl):
        if len(by_decl[k]) > 1:
            w("      %-42s %d pointers: %s\n"
              % (k, len(by_decl[k]), ", ".join(sorted(by_decl[k]))))
    w("\n")
    for r in token_rows:
        w("    %s:%d\n" % (r["file"], r["line"]))
        w("        vocabulary : %s\n" % r["vocab"])
        w("        expression : %s\n" % r["expr"])
        for p in r["pointers"]:
            w("        pointer    : %s\n" % (p if p else "(root)"))
    w("\n")

    w("-" * 78 + "\n")
    w("SECTION 4b. GRAMMARS CHECKED (real names and locations, not assumed)\n")
    w("-" * 78 + "\n\n")
    w("  TokenGrammar.Pattern                    = %r   (%s)\n"
      % (token_pattern or TOKEN_PATTERN_FALLBACK, token_pattern_file))
    w("  ResourceSchema.CanonicalLetterPattern   = %r   (%s)\n"
      % (letter_pattern, letter_pattern_file))
    w("  SemanticCheck.ReferenceGrammar          = method at src/MechaMiner.Content/Categories/SemanticCheck.cs,\n")
    w("                                            defers to ContentCategoryDescriptor ID patterns\n")
    w("  NOTE: 'SemanticCheck.ReferenceGrammar' is a METHOD, not a pattern constant, and\n")
    w("  TokenGrammar is a static CLASS whose constant is TokenGrammar.Pattern. Both names\n")
    w("  in the brief were checked against the tree rather than assumed.\n\n")
    w("  ContentCategory ID grammars recovered: %d\n" % len(category_grammars))
    for cat in sorted(category_grammars):
        w("    %-12s %s\n" % (cat, ", ".join(category_grammars[cat])))
    w("\n  SemanticCheck.BehaviorToken call sites: %d, covering %d pointer template(s)\n"
      % (len(behavior_rows), len({p for r in behavior_rows for p in r["pointers"]})))
    for p in sorted({p for r in behavior_rows for p in r["pointers"]}):
        w("    %s\n" % p)
    w("\n  SemanticCheck.ReferenceGrammar call sites: %d, covering %d pointer template(s)\n"
      % (len(reference_rows), len({p for r in reference_rows for p in r["pointers"]})))
    for p in sorted({p for r in reference_rows for p in r["pointers"]}):
        w("    %s\n" % p)
    w("\n  CanonicalLetterPattern application sites: %d\n" % len(letter_rows))
    for r in letter_rows:
        w("    %s:%d via %s -> %s\n" % (r["file"], r["line"], r["variable"], ", ".join(r["pointers"])))
    w("\n")

    w("-" * 78 + "\n")
    w("SECTION 4c. JSON SCHEMA CONSTRAINT POSITIONS\n")
    w("-" * 78 + "\n\n")
    for schema in sorted(constraints):
        rows = constraints[schema]
        n = sum(len(v) for v in rows.values())
        w("  %-42s %3d constrained pointer(s), %3d keyword(s), binding: %s\n"
          % (schema, len(rows), n, ", ".join(SCHEMA_BINDING.get(schema, [])) or "(none)"))
    w("\n")

    classify, declared_index, decl_pointers = build_classifier(
        decls, token_rows, behavior_rows, reference_rows, letter_rows,
        constraints, token_pattern or TOKEN_PATTERN_FALLBACK, letter_pattern, category_grammars)

    # ---- per pointer facts print (admitted set at the defaults)
    default_admitted = admitted(facts, MIN_OCCURRENCES, MAX_DISTINCT, VALUE_SHAPE_FILTER)

    w("-" * 78 + "\n")
    w("SECTION 3. PER-POINTER FACTS, for the admitted set at the DEFAULT knobs\n")
    w("           (min-occ=%d, max-distinct=%s, shape-filter=%s) -- %d pointers\n"
      % (MIN_OCCURRENCES, MAX_DISTINCT, VALUE_SHAPE_FILTER, len(default_admitted)))
    w("-" * 78 + "\n\n")
    for ptr in default_admitted:
        f = facts[ptr]
        vals = f.value_list()
        shown = vals[:12]
        w("  %s\n" % ptr)
        w("      occurrences=%d  distinct=%d  types=%s  files=%d\n"
          % (f.count, f.distinct, "/".join(f.type_list()), len(f.files)))
        w("      values: %s%s\n"
          % (", ".join(shown), "   [TRUNCATED: %d of %d shown]" % (len(shown), len(vals))
             if len(vals) > len(shown) else ""))
    w("\n")

    # ---- SECTION 4. THE SPLIT TABLE
    w("-" * 78 + "\n")
    w("SECTION 4. THE SPLIT TABLE -- leaf property name -> the pointers it sums\n")
    w("-" * 78 + "\n\n")
    w("  Direction 2's 17 relayed leaf names are NOT recorded anywhere in\n")
    w("  tests/verification/DAT-006.json (see REPORT.md Part 1), so the set of 17 cannot be\n")
    w("  reproduced. Two things are printed instead, both checkable:\n")
    w("    (a) the three leaf names the independent reviewer quoted verbatim, and\n")
    w("    (b) EVERY leaf name in this corpus that resolves to more than one pointer.\n\n")

    by_leaf = collections.defaultdict(dict)
    for ptr in sorted(facts):
        if "string" not in facts[ptr].types:
            continue
        by_leaf[leaf_of_pointer(ptr)][ptr] = facts[ptr]

    def print_leaf(leaf, marker=""):
        rows = by_leaf.get(leaf)
        if not rows:
            w("    %s%s -- ABSENT from this corpus\n" % (leaf, marker))
            return None
        total = sum(f.count for f in rows.values())
        w("    leaf %r%s: total occurrences=%d across %d pointer(s)\n"
          % (leaf, marker, total, len(rows)))
        for ptr in sorted(rows):
            f = rows[ptr]
            vals = f.value_list()
            w("        %-58s occ=%-4d distinct=%-3d %s%s\n"
              % (ptr, f.count, f.distinct, ", ".join(vals[:12]),
                 "  [TRUNCATED %d of %d]" % (12, len(vals)) if len(vals) > 12 else ""))
        return len(rows)

    w("  (a) REVIEWER-QUOTED LEAF NAMES\n\n")
    for leaf in REVIEWER_QUOTED_LEAVES:
        print_leaf(leaf, "  <-- reviewer example")
        w("\n")

    multi = sorted(l for l in by_leaf if len(by_leaf[l]) > 1)
    w("  (b) EVERY MULTI-POINTER LEAF NAME IN THE CORPUS: %d of %d string-valued leaf names\n\n"
      % (len(multi), len(by_leaf)))
    for leaf in multi:
        print_leaf(leaf)
    w("\n")

    w("  SPLIT COUNT, at every knob setting, over the ADMITTED set:\n")
    w("  %-14s %-14s %-8s %-10s %-10s\n" % ("min-occ", "max-distinct", "shape", "admitted",
                                            "leaf names admitted / of those, split"))
    for shape in (True, False):
        for mo in (1, 2, 3):
            for md in (6, 10, 15, None):
                sel = admitted(facts, mo, md, shape)
                leaves = collections.defaultdict(set)
                for ptr in sel:
                    leaves[leaf_of_pointer(ptr)].add(ptr)
                split = sum(1 for l in leaves if len(leaves[l]) > 1)
                w("  %-14s %-14s %-8s %-10d %d / %d\n"
                  % (mo, "unbounded" if md is None else md, "on" if shape else "off",
                     len(sel), len(leaves), split))
    w("\n")
    w("  Reviewer's claim was 'nine of seventeen relayed rows sum two to six unrelated\n")
    w("  JSON pointers'. Over the whole corpus %d string-valued leaf names split at all,\n" % len(multi))
    w("  and %d of those split into 2..6 pointers. Whether nine of direction 2's\n"
      % len([l for l in multi if 2 <= len(by_leaf[l]) <= 6]))
    w("  particular seventeen split CANNOT be checked, because the seventeen are not recorded.\n\n")

    # ---- SECTION 5. CLASSIFICATION
    w("-" * 78 + "\n")
    w("SECTION 5. GOVERNANCE CLASSIFICATION\n")
    w("-" * 78 + "\n\n")

    files_by_pointer = collections.defaultdict(set)
    for o in occurrences:
        files_by_pointer[o.pointer].add(o.file)

    results = {}
    for ptr in sorted(facts):
        results[ptr] = classify(facts[ptr], files_by_pointer[ptr])

    w("  Each pointer is assigned to EXACTLY ONE class by this stated precedence:\n")
    w("    governed-here > governed-by-schema > governed-by-grammar > governed-elsewhere\n")
    w("    > ungoverned.\n")
    w("  Because a pointer can carry evidence for several classes, the ANY-EVIDENCE tally is\n")
    w("  printed beside the exactly-one tally. A class that always loses the tie-break would\n")
    w("  otherwise print 0 and be misread as 'nothing found'.\n\n")
    w("  Over ALL %d distinct pointers in the corpus:\n" % len(facts))
    counts = collections.Counter(g.klass for g in results.values())
    any_counts = collections.Counter()
    for g in results.values():
        for k in g.all_classes:
            any_counts[k] += 1
    w("    %-22s %-18s %s\n" % ("class", "exactly-one", "any-evidence"))
    for k in ("governed-here", "governed-by-schema", "governed-by-grammar",
              "governed-elsewhere", "ungoverned"):
        w("    %-22s %-18d %s\n"
          % (k, counts.get(k, 0),
             any_counts.get(k, 0) if k != "ungoverned" else "n/a"))
    w("\n")
    w("  Corpus pointers carrying grammar evidence, by grammar (any-evidence, so these are\n")
    w("  NOT hidden by the precedence rule):\n")
    gram = collections.Counter()
    for ptr in sorted(facts):
        ev = results[ptr].all_evidence.get("governed-by-grammar", "")
        if "CanonicalLetterPattern" in ev:
            gram["ResourceSchema.CanonicalLetterPattern"] += 1
        if "TokenGrammar.Pattern" in ev:
            gram["TokenGrammar.Pattern (via SemanticCheck.BehaviorToken)"] += 1
        if "ReferenceGrammar" in ev:
            gram["SemanticCheck.ReferenceGrammar (ContentCategory ID grammars)"] += 1
    if not gram:
        w("    none\n")
    for k in sorted(gram):
        w("    %-58s %d pointer(s) present in corpus\n" % (k, gram[k]))
    w("\n")
    w("  Over the ADMITTED set at the default knobs (%d pointers):\n" % len(default_admitted))
    ac = collections.Counter(results[p].klass for p in default_admitted)
    for k in ("governed-here", "governed-by-schema", "governed-by-grammar",
              "governed-elsewhere", "ungoverned"):
        w("    %-22s %d\n" % (k, ac.get(k, 0)))
    w("\n")

    for k in ("governed-here", "governed-by-schema", "governed-by-grammar",
              "governed-elsewhere", "ungoverned"):
        rows = [p for p in default_admitted if results[p].klass == k]
        w("  --- %s: %d admitted pointer(s) ---\n" % (k.upper(), len(rows)))
        for ptr in rows:
            f = facts[ptr]
            w("    %s   occ=%d distinct=%d\n" % (ptr, f.count, f.distinct))
            w("        evidence: %s\n" % results[ptr].evidence)
        w("\n")

    w("  GOVERNED-ELSEWHERE, full evidence over ALL pointers (not just admitted), so a\n")
    w("  reader can reject a coincidental value-set match. Value-set matching is necessary\n")
    w("  and nowhere near sufficient -- VER-DAT-006-006 says so in its own words.\n\n")
    ge = [p for p in sorted(facts) if results[p].klass == "governed-elsewhere"]
    if not ge:
        w("    none\n")
    for ptr in ge:
        f = facts[ptr]
        w("    %s  occ=%d distinct=%d\n" % (ptr, f.count, f.distinct))
        w("        %s\n" % results[ptr].evidence)
    w("\n")

    # ---- SECTION 6. CRITERION AND SENSITIVITY
    w("-" * 78 + "\n")
    w("SECTION 6. ADMISSION CRITERION, AS A RUNNABLE RULE\n")
    w("-" * 78 + "\n\n")
    w("  Direction 2's actual admission criterion is NOT recorded in\n")
    w("  tests/verification/DAT-006.json. The rule below is a STAND-IN with named knobs; it\n")
    w("  is not presented as a recovery of what direction 2 executed, and the sensitivity\n")
    w("  table below shows how much the headline depends on it. Deciding which pointers are\n")
    w("  candidate token spaces at all is the reviewer's judgement, not this script's.\n\n")
    w("  Executable by hand, in order:\n")
    w("    1. Enumerate every *.json under content/ except content/schemas/. Sort the list.\n")
    w("    2. In each file, visit every scalar leaf. Write its JSON pointer, replacing each\n")
    w("       array index with the segment '*'. A scalar directly inside an array therefore\n")
    w("       gets a pointer ending in '/*'.\n")
    w("    3. Group occurrences by that pointer. Record occurrence count, the distinct\n")
    w("       values, and the JSON types seen.\n")
    w("    4. ADMIT a pointer only if ALL of:\n")
    w("         4a. occurrence count >= MIN_OCCURRENCES        (default %d)\n" % MIN_OCCURRENCES)
    w("         4b. distinct value count <= MAX_DISTINCT       (default %s)\n" % MAX_DISTINCT)
    w("         4c. VALUE_SHAPE_FILTER passes, if enabled      (default %s), i.e. every\n"
      % VALUE_SHAPE_FILTER)
    w("             value is a string, AND is not a prose sentence (does not end in '.',\n")
    w("             contains no ', ', has at most 4 whitespace-separated words), AND matches\n")
    w("             the token-ish spelling %s.\n" % TOKENISH.pattern)
    w("    5. Everything not admitted is EXCLUDED. admitted + excluded = examined.\n\n")

    w("  SENSITIVITY TABLE. 'ungoverned' is over the admitted set at that setting.\n\n")
    w("  %-6s %-12s %-7s %-10s %-12s %-12s %-10s %-12s %-11s\n"
      % ("min", "max-distinct", "shape", "admitted", "excluded", "gov-here", "gov-else",
         "gov-schema/grammar", "UNGOVERNED"))
    reproduces_33 = []
    reproduces_27 = []
    examined_ptr = len(facts)
    for shape in (True, False):
        for mo in (1, 2, 3):
            for md in (6, 10, 15, None):
                sel = admitted(facts, mo, md, shape)
                c = collections.Counter(results[p].klass for p in sel)
                ung = c.get("ungoverned", 0)
                if len(sel) == 33:
                    reproduces_33.append((mo, md, shape, len(sel), ung))
                if ung == 27:
                    reproduces_27.append((mo, md, shape, len(sel), ung))
                w("  %-6s %-12s %-7s %-10d %-12d %-12d %-10d %-12d %-11d\n"
                  % (mo, "unbounded" if md is None else md, "on" if shape else "off",
                     len(sel), examined_ptr - len(sel), c.get("governed-here", 0),
                     c.get("governed-elsewhere", 0),
                     c.get("governed-by-schema", 0) + c.get("governed-by-grammar", 0), ung))
    w("\n")
    w("  Does ANY knob setting reproduce 33 ADMITTED?  %s\n"
      % ("YES: " + "; ".join("min-occ=%s max-distinct=%s shape=%s -> admitted=%d ungoverned=%d"
                             % (a, "unbounded" if b is None else b, "on" if c else "off", d, e)
                             for a, b, c, d, e in reproduces_33)
         if reproduces_33 else "NO -- not at any of the 24 settings swept."))
    w("  Does ANY knob setting reproduce 27 UNGOVERNED? %s\n"
      % ("YES: " + "; ".join("min-occ=%s max-distinct=%s shape=%s -> admitted=%d ungoverned=%d"
                             % (a, "unbounded" if b is None else b, "on" if c else "off", d, e)
                             for a, b, c, d, e in reproduces_27)
         if reproduces_27 else "NO -- not at any of the 24 settings swept."))
    w("\n")

    w("  LEAF-KEYED SENSITIVITY, for comparison with the reviewer's own three cuts\n")
    w("  (reviewer: >=2 occurrences with <=10 distinct gives 145; <=6 gives 124; <=15 gives 161):\n")
    leafnames = collections.defaultdict(collections.Counter)
    for o in strings:
        leafnames[leaf_of_pointer(o.pointer)][json.dumps(o.value, sort_keys=True)] += 1
    for md in (6, 10, 15):
        n = len([l for l, c in leafnames.items()
                 if sum(c.values()) >= 2 and len(c) <= md])
        w("    min-occ>=2, max-distinct<=%-2d -> %d leaf names\n" % (md, n))
    w("    (leaf names examined, string-valued: %d -- this IS direction 2's 'examined' 503)\n"
      % len(leafnames))
    w("    The reviewer's three cuts do NOT reproduce here: 135 vs 124, 159 vs 145, 176 vs 161,\n")
    w("    a consistent excess of 11/14/15. Four scalar-handling variants were swept to try to\n")
    w("    close it; none does. Reported as an unreconciled third-instrument disagreement\n")
    w("    rather than papered over:\n")
    for label, drop_arrays, only_strings in (
            ("arrays included, strings only   ", False, True),
            ("arrays SKIPPED, strings only    ", True, True),
            ("arrays included, all scalars    ", False, False),
            ("arrays SKIPPED, all scalars     ", True, False)):
        tally = collections.defaultdict(collections.Counter)
        for o in occurrences:
            if only_strings and not isinstance(o.value, str):
                continue
            if drop_arrays and o.pointer.endswith("/" + ARRAY_SEGMENT):
                continue
            tally[leaf_of_pointer(o.pointer)][json.dumps(o.value, sort_keys=True)] += 1
        cuts = tuple(len([l for l, c in tally.items()
                          if sum(c.values()) >= 2 and len(c) <= md]) for md in (6, 10, 15))
        w("      %s examined=%-5d cuts(6/10/15)=%d/%d/%d\n"
          % (label, len(tally), cuts[0], cuts[1], cuts[2]))
    w("\n")

    # ---- SECTION 8. INSTRUMENT CONTROL
    w("-" * 78 + "\n")
    w("SECTION 8. INSTRUMENT CONTROL -- the classifier must be able to say BOTH things\n")
    w("-" * 78 + "\n\n")

    control_decl = None
    for key in sorted(decls):
        if len(decls[key]["tokens"]) >= 2:
            control_decl = key
            break
    positive = PointerFacts("/control/synthetic_matches_a_declaration")
    for t in decls[control_decl]["tokens"]:
        positive.values[json.dumps(t)] += 1
        positive.count += 1
        positive.types.add("string")
    positive.files.add("content/CONTROL-synthetic.json")
    pos_result = classify(positive, {"content/CONTROL-synthetic.json"})

    negative = PointerFacts("/control/synthetic_matches_nothing")
    for t in ("zzz-control-alpha", "zzz-control-beta"):
        negative.values[json.dumps(t)] += 1
        negative.count += 1
        negative.types.add("string")
    negative.files.add("content/CONTROL-synthetic.json")
    neg_result = classify(negative, {"content/CONTROL-synthetic.json"})

    w("  (i) POSITIVE control -- synthetic pointer whose value set is exactly the members of\n")
    w("      %s: {%s}\n" % (control_decl, ", ".join(sorted(decls[control_decl]["tokens"]))))
    w("      classified: %s\n" % pos_result.klass)
    w("      evidence  : %s\n" % pos_result.evidence)
    w("      PASS: %s (must be a governed-* class)\n"
      % ("yes" if pos_result.klass.startswith("governed") else "NO -- CLASSIFIER IS BROKEN"))
    w("\n")
    w("  (ii) NEGATIVE control -- synthetic pointer whose values match no declaration:\n")
    w("      {zzz-control-alpha, zzz-control-beta}\n")
    w("      classified: %s\n" % neg_result.klass)
    w("      evidence  : %s\n" % neg_result.evidence)
    w("      PASS: %s (must be ungoverned)\n"
      % ("yes" if neg_result.klass == "ungoverned" else "NO -- CLASSIFIER IS BROKEN"))
    w("\n")
    w("  Both directions realised: %s\n"
      % ("yes -- the classifier is not a constant function"
         if pos_result.klass.startswith("governed") and neg_result.klass == "ungoverned"
         else "NO"))
    w("\n")

    w("-" * 78 + "\n")
    w("END OF RUN. No timestamps, no wall-clock, no filesystem-order dependence.\n")
    w("-" * 78 + "\n")


if __name__ == "__main__":
    main()
```
