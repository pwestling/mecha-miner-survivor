#!/usr/bin/env bash
#
# Asserts that every gate script in this repository is either reached automatically
# from a workflow or explicitly exempted with a reason. A script that is neither fails
# this gate.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface ("CI calls these same wrappers instead of
#              recreating workflows")
#            docs/technical/91-verification-strategy.md § Fast pull-request suite
#            AGENTS.md § Standard workflow surface
# Requirements: TR-BLD-005, TR-QUA-001
# Verification: VER-FND-005-010, VER-FND-005-011
#
# Why this exists. Before it, this repository had nine build/verify-*.sh gate
# scripts and exactly three of them - verify-architecture.sh (from `build`),
# verify-godot.sh (from `godot-import`), and verify-policies.sh (from `test-fast`) -
# were invoked by anything. The other six ran only when a person remembered to type
# them. That is a different defect from a gate that cannot fail: these gates can
# fail, and they were never asked. Every "all gates green" report was therefore true
# about a subset nobody had stated, and the number of gates that existed was a
# question no artifact answered.
#
# The rule this file makes checkable is a partition, not a count:
#
#   every gate script is REACHED (a real call site inside a verb some workflow runs,
#   or inside a root wrapper or a workflow itself) or EXEMPT (listed below with the
#   failure that was observed when it was wired), and never both, and never neither.
#
# Four properties keep the rule from decaying into a formality, each stated because
# dropping it is how such a rule usually dies:
#
#  1. The candidate set is read from the filesystem, never from a list in this
#     file. A hardcoded roster would reproduce the original defect one level up:
#     a new script would be absent from the roster and so would pass by not being
#     looked at. `find` is the enumerator, and an empty enumeration fails.
#  2. Every exemption must name a file that exists, and must not name a script
#     that is in fact reached. A stale exemption for a deleted script, or for one
#     that has since been wired, turns the list into decoration.
#  3. "Reached" means a real call site, not a mention. Prose in a doc comment, a
#     diagnostic that names a script, an unused constant, an `echo` message and
#     here-doc text are all mentions. Otherwise documenting a script would be
#     indistinguishable from running it, which is the substitution this whole
#     repository keeps finding.
#  4. "Reached" also means reached from a workflow. A call site inside a verb that no
#     workflow invokes is not automation: verify-godot-runner.sh was called by
#     `test-main`, .github holds one workflow, and its steps do not include
#     `test-main`, so the gate ran exactly as often as it had before it was "wired" -
#     never. The partition now follows the edge from a workflow to a verb to a call
#     site, and rejects a chain that does not start at a workflow.
#
# Property 4 is why the analysis below is a program rather than a grep. Attributing a
# call site to a verb needs member granularity, not file granularity: TestVerb.cs
# holds both RunFastTier, which `test-fast` runs, and RunMainTier, which only
# `test-main` runs, and the whole point of the finding is that those two are not the
# same answer. The program is embedded rather than committed as build/*.py because
# .py is not an owned text extension (src/MechaMiner.Tools/Text/OwnedTextHygiene.cs),
# so a committed one would be the only file in the repository no formatter or policy
# gate looks at; build/verify-verbs.sh and build/verify-architecture.sh embed python3
# the same way.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

# --- Deliberately unwired gate scripts ---------------------------------------
# "<repo-relative path>|<the failure observed when it was wired, and what would
#  unblock it>"
#
# One reason per script, and each one is what a wiring attempt actually printed.
#
# The previous version of this list carried a single shared reason for five scripts:
# that ./build.sh rebuilds the verb host on every invocation, so a gate reached from
# inside a running verb makes MSBuild rewrite the assembly the calling verb is
# executing from, "observed" as verify-verbs.sh reading an empty verb table when wired
# into test-fast. That reason does not reproduce and is withdrawn. Each of the five was
# wired and its owning verb run end to end, and all five passed:
#
#   verify-wrapper-parity.sh  -> build       exit 0, verb 30 s  (baseline 20 s)
#   verify-verbs.sh           -> test-fast   exit 0, verb 122 s (baseline 28 s)
#   verify-configurations.sh  -> test-fast   exit 0, verb 109 s (baseline 28 s)
#   verify-test-harness.sh    -> build       exit 0, verb 142 s (baseline 20 s)
#   verify-format.sh          -> build       exit 0, verb 248 s (baseline 20 s)
#
# Three of them are now wired and are not on this list. What the wiring trials did find
# is a different constraint the old reason had obscured: several of these scripts invoke
# ./build.sh with a verb, so the verb that owns a script's subject usually cannot hold
# it. verify-format.sh in format-check, and verify-test-harness.sh in test-fast, each
# recursed until killed at 200 s. That is why verify-verbs.sh and verify-configurations.sh
# live in test-fast rather than in build, whose subject they are: both invoke
# ./build.sh build.
#
# This list is not a place to park a script that could simply be wired. Each entry
# below states what was measured or observed, and for verify-format.sh that is a
# runtime cost rather than a failure - said plainly, because "slow" is an honest reason
# and "re-entrancy" was not.

readonly EXEMPT=(
  "build/verify-format.sh|passes wired into build: ./build.sh build exits 0 in 248 s, of which verify-format is 230 s. Not wired because of that number and nothing else: 230 s is more than twice the whole fast job's current duration, and format-check, the verb whose subject it is, recurses (wired there, ./build.sh format-check did not terminate and was killed at 200 s). Runtime is the objection; whether to pay it on every pull request is a budget decision this file does not get to make"
  "build/verify-test-harness.sh|passes wired into build on its own: exit 0 in 142 s. Not wired because it invokes ./build.sh test-fast, so test-fast cannot hold it (wired there, killed at 200 s, exit 124, still nesting), and build cannot hold it either now that test-fast reaches verify-verbs.sh and verify-configurations.sh, which invoke ./build.sh build. Observed on this tree with it added to build's stages: ./build.sh build ran 452 s without terminating and was killed (exit 137), having recorded 13 nested build and 14 nested test-fast invocations under artifacts/verbs/ by then. Unblocked by a wrapper dispatch that does not re-enter the verb it was called from"
  "build/verify-godot-runner.sh|reached only from test-main, and no workflow invokes test-main: .github holds one workflow and its six steps are provisioning, bootstrap, format-check, build, test-fast, godot-import. It was called 'wired' on the strength of that call site while running exactly as often as before - never. Passes standalone. Unblocked by OPS-001's main-branch suite, which is the workflow that would run test-main"
)

# --- Where an invocation may live ---------------------------------------------
# The verb host (a verb reaching the script through RunRepositoryScript), the root
# wrappers, and the CI workflow. Anything else - a test's doc comment, a design
# document, another gate script's prose - is a mention, not a call site.

readonly -a SEARCH_ROOTS=(
  "src"
  "build.sh"
  "build.ps1"
  ".github"
)

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

echo "=== 1. enumerate gate scripts from the filesystem (VER-FND-005-010)"

# The glob is deliberately wider than build/verify-*.sh. A checker written in
# PowerShell, in Python, with no extension at all, or placed outside build/, is the
# same kind of artifact and must not escape the partition by being a different file
# name. Everything called verify-* or verify_* is a candidate; the four excluded
# suffixes are documents and records, which cannot be invoked. This repository ships
# build.ps1, so .ps1 in particular was a real hole rather than a hypothetical one.
# artifacts/ and .git are excluded because they are outputs, not sources, and
# generated/ because nothing there is authored.
mapfile -t scripts < <(
  cd "${REPO_ROOT}" && find . \
    -type d \( -name .git -o -name artifacts -o -name generated -o -name .godot \
      -o -name obj -o -name bin \) -prune -o \
    -type f \( -name 'verify-*' -o -name 'verify_*' \) \
    ! -name '*.md' ! -name '*.markdown' ! -name '*.txt' ! -name '*.json' -print \
    | sed 's|^\./||' | sort
)

# An empty candidate set never satisfies a gate: "no gate scripts found" is a
# broken enumerator, not a clean repository.
if [[ "${#scripts[@]}" -eq 0 ]]; then
  fail "found no gate scripts at all; the enumerator is broken, not the repository clean"
  echo
  echo "verify-gate-wiring: FAIL (${failures} assertion(s))"
  exit "${EXIT_VALIDATION}"
fi

pass "found ${#scripts[@]} gate script(s) by filesystem enumeration"
for path in "${scripts[@]}"; do
  printf '      %s\n' "${path}"
done

# --- The call sites, resolved once --------------------------------------------
# Rows are "<script>\t<file>:<line>\t<verbs that reach it>\t<yes|no from a workflow>".

sites_table="$(mktemp)"
trap 'rm -f "${sites_table}" "${sites_table}.err"' EXIT

if ! python3 - "${REPO_ROOT}" --roots "${SEARCH_ROOTS[@]}" --scripts "${scripts[@]}" \
      >"${sites_table}" 2>"${sites_table}.err" <<'CALL_SITES'
"""Prints every real call site of every named gate script, with its reachability.

    <repo-root> --roots <root>... --scripts <path>...

One tab-separated row per accepted call site:

    <script>  <file>:<line>  <verbs that reach it>  <yes|no reachable from a workflow>

and nothing for a script with no call site. A mention - prose, a comment, a
diagnostic message, an unused constant - is not a call site and produces no row.
"""
from __future__ import annotations

import os
import re
import sys
from collections import defaultdict

# --- comment stripping -------------------------------------------------------
# A comment filter anchored at line start only is why "// see build/x.sh" was
# rejected while "code(); // build/x.sh" was accepted. Both are comments.

_URL_SCHEME = re.compile(r"([A-Za-z][A-Za-z0-9+.-]*)://")
_SCHEME_MARK = "\x00SCHEME\x00"
_SHELL_COMMENT = re.compile(r"(?:^|(?<=\s))#.*$")


def strip_cs_comment(line):
    """Removes a // comment anywhere on a C# line, protecting a URL scheme."""
    protected = _URL_SCHEME.sub(lambda m: m.group(1) + _SCHEME_MARK, line)
    return protected.split("//", 1)[0].replace(_SCHEME_MARK, "://")


def strip_shell_comment(line):
    """Removes a # comment from a shell/YAML line, keeping ${#x}, $# and $((...)).

    The # must start a word, which is the rule the shell itself applies.
    """
    return _SHELL_COMMENT.sub("", line)


# --- what counts as a call site ----------------------------------------------


def cs_pattern(script):
    """A C# call site: the complete string literal in argument position.

    The closing quote rejects a diagnostic that merely names the script inside a
    longer message. The following ',' or ')' rejects the two forms that survived the
    closing quote alone and were counted as invocations: a concatenation
    ("path" + " was never run") and a declaration (const string X = "path";).
    """
    return re.compile('"' + re.escape(script) + r'"\s*[,)]')


def shell_pattern(script):
    """A shell/YAML call site: the path in *command* position.

    Command position, not "surrounded by delimiters": the previous character class
    admitted a preceding '"', so echo "build/x.sh ..." and HINT="build/x.sh" both
    read as invocations, and so did here-doc prose, whose words are separated by the
    same spaces a command line uses. A command starts at the beginning of a line,
    after ';', '&&', '||', '|', '(' or a backtick, or after a YAML step's run: key,
    optionally behind an interpreter.
    """
    return re.compile(
        r"(?:^|[;&|(`]|&&|\|\|)\s*"
        r"(?:-\s+)?(?:run:\s*)?"
        r"(?:(?:sudo|bash|sh|pwsh|exec|source|\.)\s+)*"
        r"[\"']?(?:\./)?" + re.escape(script) + r"[\"']?"
        r"(?=$|[\s;&|)\"'])"
    )


# --- C# members --------------------------------------------------------------
# Attribution needs member granularity: TestVerb.cs holds RunFastTier, which
# test-fast runs, and RunMainTier, which only test-main runs, and those are not the
# same answer to "does a workflow reach this".

_TYPE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|internal|private|protected|static|sealed|abstract|partial|file"
    r"|readonly|ref|unsafe)\s+)*"
    r"(?:class|struct|interface|record|enum)\s+(\w+)"
)
_MEMBER = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|internal|private|protected|static|sealed|abstract|virtual|override"
    r"|partial|readonly|const|extern|async|new|volatile|unsafe|required|event"
    r"|implicit|explicit)\s+)*"
    r"[\w<>,\[\]\.\?\(\)\s]*?(\w+)\s*(?:<[^>()]*>)?\s*(?:\(|=>|=|\{|;)"
)
_IDENTIFIER = re.compile(r"\b([A-Za-z_]\w*)\b")
_QUALIFIED = re.compile(r"\b([A-Z]\w*)\.(\w+)\b")


class Member:
    def __init__(self, type_name, name):
        self.type_name = type_name
        self.name = name
        self.lines = []

    @property
    def key(self):
        return (self.type_name, self.name)

    @property
    def text(self):
        return "\n".join(self.lines)


def parse_cs(text):
    """Returns line number -> member key, and member key -> member, for one file."""
    owner = {}
    members = {}
    type_stack = []          # (type name, brace depth of its body)
    pending_type = None
    current = None
    depth = 0
    parens = 0

    for number, raw in enumerate(text.splitlines(), start=1):
        line = strip_cs_comment(raw)
        stripped = line.strip()
        inside = type_stack[-1] if type_stack else None
        at_rest = (
            inside is not None
            and current is None
            and depth == inside[1]
            and parens == 0
            and bool(stripped)
            and stripped[0] not in "{}"
        )

        type_here = _TYPE.match(line) if stripped else None
        if type_here is not None:
            pending_type = type_here.group(1)
            current = None
        elif at_rest:
            # A declaration may only begin where the previous one ended. A
            # continuation line of a multi-line initializer looks exactly like a
            # declaration on its own, which is how the verb registry's whole table
            # was once read as one member per row.
            member_here = _MEMBER.match(line)
            if member_here is not None:
                current = members.setdefault(
                    (inside[0], member_here.group(1)),
                    Member(inside[0], member_here.group(1)),
                )

        if current is not None:
            current.lines.append(line)
            owner[number] = current.key

        for character in line:
            if character == "(":
                parens += 1
            elif character == ")":
                parens = max(0, parens - 1)
            elif character == "{":
                depth += 1
                if pending_type is not None:
                    type_stack.append((pending_type, depth))
                    pending_type = None
            elif character == "}":
                if type_stack and type_stack[-1][1] == depth:
                    type_stack.pop()
                    current = None
                depth -= 1

        inside = type_stack[-1] if type_stack else None
        if (
            current is not None
            and inside is not None
            and depth == inside[1]
            and parens == 0
            and (stripped.endswith(";") or stripped.endswith("}"))
        ):
            current = None

    return owner, members


# --- the analysis ------------------------------------------------------------

_WORKFLOW_VERB = re.compile(
    r"(?:^|[;&|(`]|&&|\|\|)\s*(?:-\s+)?(?:run:\s*)?"
    r"(?:bash\s+|sh\s+|pwsh\s+(?:-\w+\s+)*)?"
    r"(?:\./)?build\.(?:sh|ps1)\s+([a-z][a-z0-9-]*)"
)
_ENTRY = re.compile(r"VerbDescriptor\.Implemented\(")


def read(path):
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        return handle.read()


def files_under(root_dir, roots, suffixes):
    found = []
    for root in roots:
        absolute = os.path.join(root_dir, root)
        if os.path.isfile(absolute):
            if absolute.endswith(suffixes):
                found.append(os.path.relpath(absolute, root_dir))
            continue
        for directory, subdirectories, names in os.walk(absolute):
            subdirectories[:] = [
                name for name in subdirectories
                if name not in ("obj", "bin", ".git", "artifacts", ".godot", "generated")
            ]
            for name in names:
                if name.endswith(suffixes):
                    found.append(os.path.relpath(os.path.join(directory, name), root_dir))
    return sorted(set(found))


def workflow_verbs(root_dir):
    """The verbs some workflow actually invokes through a root wrapper."""
    verbs = set()
    workflows = os.path.join(root_dir, ".github", "workflows")
    if not os.path.isdir(workflows):
        return verbs
    for name in sorted(os.listdir(workflows)):
        if name.endswith((".yml", ".yaml")):
            for raw in read(os.path.join(workflows, name)).splitlines():
                match = _WORKFLOW_VERB.search(strip_shell_comment(raw))
                if match is not None:
                    verbs.add(match.group(1))
    return verbs


def entry_points(registry_text, members):
    """verb name -> the registered member that implements it.

    The registration order is (name, effect, owner, handler, arguments...), so the
    first qualified reference that resolves to a member is the handler and a
    VerbArgument factory after it is not an entry point.
    """
    entries = defaultdict(set)
    text = "\n".join(strip_cs_comment(line) for line in registry_text.splitlines())
    for match in _ENTRY.finditer(text):
        window = text[match.end():].split("VerbDescriptor.")[0]
        verb = re.search(r'"([a-z][a-z0-9-]*)"', window)
        if verb is None:
            continue
        for type_name, member_name in _QUALIFIED.findall(window):
            if (type_name, member_name) in members:
                entries[verb.group(1)].add((type_name, member_name))
                break
    return entries


def closure(seeds, members, by_name):
    """Members reachable from seeds. A qualified T.N is an edge to T.N; a bare N is
    an edge to this type's N when it has one, and otherwise to every N there is.
    Over-approximating widens what counts as reached, which can only make this gate
    weaker in a way a reader can see, never redder than the truth."""
    reached = set()
    queue = [seed for seed in seeds if seed in members]
    while queue:
        key = queue.pop()
        if key in reached:
            continue
        reached.add(key)
        member = members[key]
        for type_name, member_name in _QUALIFIED.findall(member.text):
            candidate = (type_name, member_name)
            if candidate in members and candidate not in reached:
                queue.append(candidate)
        for name in set(_IDENTIFIER.findall(member.text)):
            same_type = (member.type_name, name)
            if same_type in members:
                if same_type not in reached:
                    queue.append(same_type)
                continue
            for candidate in by_name.get(name, ()):
                if candidate not in reached:
                    queue.append(candidate)
    return reached


def main():
    argv = sys.argv[1:]
    root_dir = argv[0]
    roots, scripts, bucket = [], [], None
    for token in argv[1:]:
        if token == "--roots":
            bucket = roots
        elif token == "--scripts":
            bucket = scripts
        elif bucket is not None:
            bucket.append(token)
    roots = [root for root in roots if os.path.exists(os.path.join(root_dir, root))]

    owners, stripped_cs, members = {}, {}, {}
    for relative in files_under(root_dir, roots, (".cs",)):
        text = read(os.path.join(root_dir, relative))
        owner, file_members = parse_cs(text)
        owners[relative] = owner
        stripped_cs[relative] = [strip_cs_comment(line) for line in text.splitlines()]
        for key, member in file_members.items():
            members.setdefault(key, Member(*key)).lines.extend(member.lines)

    by_name = defaultdict(set)
    for key in members:
        by_name[key[1]].add(key)

    registry = os.path.join(root_dir, "src", "MechaMiner.Tools", "Cli", "VerbRegistry.cs")
    entries = entry_points(read(registry), members) if os.path.isfile(registry) else {}

    from_workflow = workflow_verbs(root_dir)
    verbs_of = defaultdict(set)
    for verb, seeds in entries.items():
        for key in closure(seeds, members, by_name):
            verbs_of[key].add(verb)

    # A member "runs scripts" when it calls RunRepositoryScript, or when a member
    # that references it does. The second half is what lets a table of gates live in
    # a field the running method iterates over; without it a real call site in a
    # static array would be rejected. Nothing further away is accepted: a string
    # literal in a list nothing runs is a mention, not a call.
    referrers = defaultdict(set)
    for key, member in members.items():
        for name in set(_IDENTIFIER.findall(member.text)):
            for candidate in by_name.get(name, ()):
                if candidate != key:
                    referrers[candidate].add(key)

    def runs_scripts(key):
        if "RunRepositoryScript" in members[key].text:
            return True
        return any("RunRepositoryScript" in members[other].text for other in referrers[key])

    shell_files = files_under(root_dir, roots, (".sh", ".ps1", ".yml", ".yaml", ".bash"))
    rows = set()
    for script in scripts:
        cs_regex = cs_pattern(script)
        shell_regex = shell_pattern(script)

        for relative, lines in stripped_cs.items():
            for number, line in enumerate(lines, start=1):
                if cs_regex.search(line) is None:
                    continue
                key = owners[relative].get(number)
                if key is None or not runs_scripts(key):
                    continue
                verbs = sorted(verbs_of.get(key, ()))
                reached = any(verb in from_workflow for verb in verbs)
                rows.add((
                    script,
                    relative + ":" + str(number),
                    ",".join(verbs) if verbs else "(no verb)",
                    "yes" if reached else "no",
                ))

        for relative in shell_files:
            in_workflow = relative.startswith(".github/workflows/")
            is_wrapper = relative in ("build.sh", "build.ps1")
            for number, raw in enumerate(read(os.path.join(root_dir, relative)).splitlines(), 1):
                if shell_regex.search(strip_shell_comment(raw)) is None:
                    continue
                if in_workflow:
                    where, reached = "(workflow " + os.path.basename(relative) + ")", "yes"
                elif is_wrapper:
                    where, reached = "(root wrapper " + relative + ")", "yes"
                else:
                    where, reached = "(" + relative + ", neither a workflow nor a wrapper)", "no"
                rows.add((script, relative + ":" + str(number), where, reached))

    for row in sorted(rows):
        sys.stdout.write("\t".join(row) + "\n")
    return 0


raise SystemExit(main())
CALL_SITES
then
  fail "the call-site analysis did not run; this gate cannot report a partition it did not compute"
  sed 's/^/      /' "${sites_table}.err"
  rm -f "${sites_table}.err"
  echo
  echo "verify-gate-wiring: FAIL (${failures} assertion(s))"
  exit "${EXIT_VALIDATION}"
fi
rm -f "${sites_table}.err"

# Every call site of one script, reachable from a workflow or not.
all_sites() {
  awk -F'\t' -v want="$1" '$1 == want { print }' "${sites_table}"
}

# Only the call sites a workflow reaches. This is what "invoked" means.
reached_sites() {
  awk -F'\t' -v want="$1" '$1 == want && $4 == "yes" { print }' "${sites_table}"
}

is_exempt() {
  local path="$1"
  local entry
  for entry in "${EXEMPT[@]}"; do
    [[ "${entry%%|*}" == "${path}" ]] && return 0
  done
  return 1
}

echo
echo "=== 2. every exemption names a script that exists and is not reached"

if [[ "${#EXEMPT[@]}" -eq 0 ]]; then
  pass "the exemption list is empty, so every gate script must be reached"
else
  for entry in "${EXEMPT[@]}"; do
    exempt_path="${entry%%|*}"
    reason="${entry#*|}"

    if [[ ! -f "${REPO_ROOT}/${exempt_path}" ]]; then
      fail "stale exemption: ${exempt_path} does not exist"
      continue
    fi

    if [[ -z "${reason}" || "${reason}" == "${entry}" ]]; then
      fail "exemption for ${exempt_path} states no reason"
      continue
    fi

    if [[ -n "$(reached_sites "${exempt_path}")" ]]; then
      fail "${exempt_path} is exempted but is in fact reached from a workflow; remove the exemption"
      continue
    fi

    pass "exempt: ${exempt_path} (${reason})"
  done
fi

echo
echo "=== 3. every gate script is reached from a workflow or exempt, and never both"

for path in "${scripts[@]}"; do
  reached="$(reached_sites "${path}")"

  if [[ -n "${reached}" ]]; then
    if is_exempt "${path}"; then
      # Reported by section 2 as well; repeated here so the partition's own
      # statement is complete in one place.
      fail "${path} is both reached and exempt"
      continue
    fi
    first="$(printf '%s\n' "${reached}" | head -n 1)"
    pass "reached: ${path}  <-  $(printf '%s' "${first}" | cut -f2) (verb $(printf '%s' "${first}" | cut -f3))"
    continue
  fi

  if is_exempt "${path}"; then
    continue
  fi

  unreached="$(all_sites "${path}")"
  if [[ -n "${unreached}" ]]; then
    fail "${path} has a call site, but no workflow reaches the verb that holds it."
    printf '%s\n' "${unreached}" | while IFS=$'\t' read -r _ where verbs _; do
      printf '      %s is reached only from: %s\n' "${where}" "${verbs}"
    done
    printf '      A gate invoked by a verb no workflow runs is a gate nobody runs. Either\n'
    printf '      move the call site into a verb the workflow invokes, add the verb to a\n'
    printf '      workflow, or exempt it in build/verify-gate-wiring.sh with the reason.\n'
    continue
  fi

  fail "${path} is never invoked and is not on the deliberately-unwired list."
  printf '      It runs only when a person remembers to type it, so any report of\n'
  printf '      "all gates green" silently excludes it. Wire it into a verb some\n'
  printf '      workflow runs, or add it to EXEMPT in build/verify-gate-wiring.sh\n'
  printf '      with the reason.\n'
done

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-gate-wiring: PASS"
  exit 0
fi

echo "verify-gate-wiring: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
