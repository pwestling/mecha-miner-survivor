#!/usr/bin/env bash
#
# Asserts that every script in this repository is classified, and that every script
# classified as a gate is either reached automatically from a workflow or explicitly
# exempted with a reason. A script nothing classifies, and a gate that is neither
# reached nor exempt, both fail this gate.
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
#  1. Which files the rule is about is decided by a committed inventory that
#     classifies EVERY script in the repository, and the inventory is checked to
#     partition the filesystem exactly - see § THE INVENTORY, AND WHY IT REPLACED A
#     NAME GLOB below. An empty enumeration fails.
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
# THE INVENTORY, AND WHY IT REPLACED A NAME GLOB.
#
# Until this revision the candidate set came from `find -name 'verify-*' -o -name
# 'verify_*'`. That was defended as being read from the filesystem rather than from a
# list, and it is - but it answered the wrong question. It asked which files are named
# like a gate. A gate script named anything else was not accepted too broadly; it was
# not enumerated at all, so no strengthening of the rule could reach it. Three live
# instances, none hypothetical:
#
#   * src/MechaMiner.Tools/ContentImport/check_quote_mismatch_evidence.py (on master) is
#     a gate and has no other mode. Its name begins with "check".
#   * derive_citation_pass_expectations.py (on master) is a generator when invoked bare
#     and a gate when invoked with --verify. Its name begins with "derive".
#   * build/bootstrap-linux.sh is in this repository right now. The glob does not see it,
#     so it is outside the partition's extent entirely, and it happens to be reached only
#     because the workflow's provisioning step calls it. Nothing checked that.
#
# So the enumeration is inverted. The filesystem set is now every script in the
# repository, and INVENTORY below classifies each one. The check is set equality in both
# directions: a script no entry classifies fails, and an entry naming a file no
# enumerator found fails. "Is every script classified" becomes completely machine
# checkable, which the glob never asked at all.
#
# Two independent enumerators, and their disagreement is a failure. One reads extensions
# (SCRIPT_EXTENSIONS), one reads the first two bytes for `#!`. At this revision they
# coincide exactly on 13 files, which is why both are kept rather than one: a script with
# a shebang and no known extension is invisible to the first, and a script with a known
# extension and no shebang - a .ps1, say, since PowerShell needs none - is invisible to
# the second. Requiring them to agree catches either before it becomes a hole. The union
# is what gets classified, so a disagreement fails closed.
#
# DUAL-MODE SCRIPTS ARE CLASSIFIED BY INVOCATION, NOT BY FILE. An entry may name the
# arguments that make the script a gate (field 3). If an entry named only the path, a
# reachable bare invocation would satisfy the wiring rule while the gate mode ran
# nowhere - which is derive_citation_pass_expectations.py exactly. Where field 3 is set,
# a call site counts only if the arguments appear in the same C# member, or on the same
# shell command line, as the path.
#
#   Its limit, stated because the check is real and is not proof: "in the same member"
#   is not "at this call site". A member holding both `Run(script, "--verify")` and a
#   bare `Run(script)` satisfies it, and so does a member that passes --verify on a
#   branch it never takes. It is a genuine check on a real signal and it does not
#   establish that --verify is what runs. Narrowing it to argument-position adjacency is
#   possible and is not done here; nothing at this revision needs field 3 at all.
#
# NOTHING NOT ON THIS REF IS PRE-LISTED. master carries three ContentImport .py scripts
# (see the FOLLOW-UP below). They are named in this comment as evidence and are
# deliberately absent from INVENTORY, because classifying a file that is not here would
# be a stale inventory - the same defect as a stale exemption, which § 3 exists to catch.
# Whoever merges master into this chain classifies what arrives; the enumerators will
# fail the gate until they do, which is the rule meeting them rather than them having to
# derive it.
#
# THE RESIDUAL LIMIT. The inventory makes "is every script classified" machine checkable.
# It does not make "is this classification correct" checkable. A gate deliberately filed
# as `launcher` or `provisioning` still escapes §§ 3 and 4, and this file cannot tell the
# difference. What changed is where that judgement lives: it was a filename convention
# nobody signed, and it is now a line in a committed file with a note on it, which shows
# up in a diff and has an author. That is better and it is not a proof.
#
# Property 4 is why the analysis below is a program rather than a grep. Attributing a
# call site to a verb needs member granularity, not file granularity: TestVerb.cs
# holds both RunFastTier, which `test-fast` runs, and RunMainTier, which only
# `test-main` runs, and the whole point of the finding is that those two are not the
# same answer. The program is embedded rather than committed as build/*.py because .py
# is not in OwnedTextHygiene.OwnedExtensions (src/MechaMiner.Tools/Text/), so a
# committed one is inspected by no formatter and no policy gate;
# build/verify-verbs.sh and build/verify-architecture.sh embed python3 the same way.
#
# That is not a hypothetical about a file this commit might have added.
# src/MechaMiner.Tools/ContentImport/verify_content.py is on master right now, 132 KB
# of it, in exactly that position - along with check_quote_mismatch_evidence.py and
# derive_citation_pass_expectations.py.
#
# FOLLOW-UP (owner: FND-002, which owns format and OwnedTextHygiene): add ".py" to
# OwnedExtensions. Checked rather than assumed to be small - it is not free. All three
# of those files violate trim_trailing_whitespace today: 44 lines in
# verify_content.py, 22 in check_quote_mismatch_evidence.py, 8 in
# derive_citation_pass_expectations.py, 74 in total. All three already satisfy
# end_of_line and insert_final_newline. So adding the extension turns format-check red
# on master until `./build.sh format` repairs them, which it can do mechanically, and
# it rewrites 74 lines in files DAT-006's content-import work owns. What it unblocks is
# splitting the program below out of this file, which is the only reason it is inline.
# Note also that all three are enumerated by BOTH enumerators below - .py is in
# SCRIPT_EXTENSIONS and all three carry a shebang - so when master merges here the gate
# is red until someone classifies them, and classifying any of them as a gate then makes
# §§ 3 and 4 apply. That is the inverted enumeration working, not a problem with it, and
# it is the whole reason the glob had to go: check_quote_mismatch_evidence.py and
# derive_citation_pass_expectations.py match no glob spelled "verify".
#
# WHAT THIS GATE DOES NOT ESTABLISH. Three of these fail closed and two fail open, and
# they are separated on that line because only the latter can let something through.
#
#   Fails closed, so the worst case is a red gate someone has to look at:
#     * An inventory entry that classifies a file the enumerators do not see, and a
#       script no entry classifies, are both red. The extent of the rule is therefore
#       the whole repository and no longer the set of files named verify-*, which is the
#       inversion this revision made; the enumerators' own disagreement is red too.
#     * The C# member closure over-approximates. An unqualified identifier that names
#       members of several types adds an edge to all of them, so a member can be
#       attributed to more verbs than really reach it. That widens "reached", so it can
#       make this gate weaker in a way section 3's output shows by naming the verbs.
#     * A script path reached through a constant is not recognised. Only a literal in
#       argument position counts, so refactoring a call site to
#       `const string Script = "build/verify-x.sh";` makes this gate red rather than
#       quietly satisfied. Extending the matcher is the fix if that is ever wanted.
#
#   FAIL OPEN, and are the ones that can hide an unrun gate:
#     * A call site in build.sh or build.ps1 is counted as reached without proving that
#       the shell function containing it is ever called. The wrappers are launchers with
#       no functions today, so there is nothing to get wrong yet; the day one of them
#       grows a function, a gate invoked only from inside a function nothing calls would
#       pass this partition. Closing it needs shell reachability, which this gate does
#       not do. The C# side has no equivalent hole: there the member must be reachable
#       from a workflow verb's entry point.
#     * A HERE-DOC BODY LINE THAT LOOKS LIKE A COMMAND IS COUNTED AS A CALL SITE. The
#       shell matcher requires command position and does not track here-doc bodies, so an
#       indented bare path inside <<'WORD' or PowerShell's @'...'@ reads exactly like a
#       command. Two live instances, observed rather than imagined: build.sh:59 and
#       build.ps1:64 are `sudo build/bootstrap-linux.sh` inside the help text a wrapper
#       prints when the SDK is missing, and the analyzer reports both as
#       "(root wrapper ...) yes". VER-FND-005-011's twelve-form control set does include
#       here-doc prose, but the form it used is a sentence - "see build/verify-zzz.sh for
#       details" - which is not in command position; a line that a human is being told to
#       type is. Checked rather than assumed: no gate script's reached status depends on
#       this today - all seven reached gates are reached from C# call sites in
#       src/MechaMiner.Tools/Verbs/ - so the hole is live for provisioning and latent for
#       gates. It is also why § 4 is not extended to require that provisioning scripts be
#       reached from a workflow step: that check would be satisfied by help text, and a
#       check satisfied by help text is the substitution this file exists to catch.
#       FOLLOW-UP (FND-005, which owns the matcher through VER-FND-005-011): track
#       here-doc and PowerShell here-string bodies in strip_shell_comment's neighbourhood
#       and add the thirteenth control form - a bare indented path inside <<'WORD' - in
#       both directions. Then § 4 can cover provisioning.
#     * A GATE MISFILED AS `launcher` OR `provisioning` ESCAPES §§ 3 AND 4 ENTIRELY, and
#       so does a dual-mode gate whose entry names no arguments. The inventory makes
#       every script's classification visible and diffable; it cannot make the
#       classification true. See THE RESIDUAL LIMIT above, and the note on field 3's
#       "same member is not same call site" granularity.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

# --- The script inventory ------------------------------------------------------
# "<repo-relative path>|<kind>|<arguments that make it a gate>|<why this kind>"
#
# Every script the enumerators find must appear here exactly once, and every entry must
# name a script an enumerator found. § 2 checks both directions. The rationale for
# inverting the enumeration, the invocation field and the limits of all of it are in the
# header under THE INVENTORY, AND WHY IT REPLACED A NAME GLOB.
#
# THREE KINDS, NOT TWO. A taxonomy with fewer kinds than the population forces a
# misfiling and then blames the filer:
#
#   gate         Decides something about the repository and can fail on its own account.
#                §§ 3 and 4 apply: it must be reached from a workflow or exempted.
#   launcher     The standard command surface itself. It decides nothing; it dispatches
#                to the verb host, and it is what workflows and gates invoke in order to
#                reach anything else. Asking whether it is "wired" is backwards.
#   provisioning Changes the machine, decides nothing about the repository, and cannot be
#                invoked from a verb. build/bootstrap-linux.sh installs the .NET SDK,
#                and a .NET process cannot install .NET, so no verb can hold it. It is
#                neither a gate nor a launcher and filing it as either would be a lie
#                told to satisfy a two-valued taxonomy.
#
# Field 3 is empty for every entry at this revision: no script here has a mode that only
# some arguments select. The mechanism exists because master's
# derive_citation_pass_expectations.py needs it on arrival, and because a mechanism with
# no control is a mechanism nobody has seen work - § 5 exercises it.

readonly INVENTORY=(
  "build.sh|launcher||the POSIX entry point of the standard command surface (doc 100 § Standard command surface). Parses no policy and decides nothing: it locates the verb host, builds it if needed, and forwards the verb. Every gate below is reached through it"
  "build.ps1|launcher||the PowerShell entry point of the same surface, held at parity with build.sh by build/verify-wrapper-parity.sh. Note that it carries a shebang (#!/usr/bin/env pwsh) and so is seen by both enumerators; a .ps1 without one would be seen by only the extension enumerator, which is why § 1 requires them to agree"
  "build/gate-output.sh|library||the shared output vocabulary every gate script sources: pass/fail for findings about the subject under test, control_pass/control_fail/control_detail for anything a negative control's fixture manufactured, section and gate_summary so a red run names the failing section. Sourced, never executed, and mode 644 so it cannot be a workflow step. Not a gate: it asserts nothing about the repository. Its own self-check, gate_assert_marking, runs inside each gate that sources it rather than here"
  "build/bootstrap-linux.sh|provisioning||installs and pins the .NET SDK, Godot and the Vulkan ICD, and verifies the pinned versions. Not a gate: the only thing it decides is whether the machine it is running on has the toolchain, and its failure means repair this machine, not repair this repository. Not reachable from a verb either - the verb host is a .NET process and this is what installs .NET - so the workflow's provisioning step is the only place it can be called from, and doc 100 § Standard command surface puts it there"
  "build/verify-architecture.sh|gate||asserts the project-reference graph, the Godot boundary and the no-GDScript rule"
  "build/verify-configurations.sh|gate||asserts the Godot project builds in all three configurations"
  "build/verify-format.sh|gate||asserts owned text files satisfy the .editorconfig rules"
  "build/verify-gate-wiring.sh|gate||this file: asserts the partition below. It is a gate about gates and is subject to its own rule, which is why it appears in its own inventory rather than being special-cased out of it"
  "build/verify-godot-runner.sh|gate||asserts the Godot integration-test runner emits the report the engine tier asserts"
  "build/verify-godot.sh|gate||asserts the Godot import step produced the expected artifacts"
  "build/verify-policies.sh|gate||asserts the compiler and analyzer policy fixtures fail as designed"
  "build/verify-test-harness.sh|gate||asserts the test tiers discover tests, separate pure from engine, and fail on violation"
  "build/verify-verbs.sh|gate||asserts the verb table matches doc 100's standard command surface"
  "build/verify-wrapper-parity.sh|gate||asserts build.sh and build.ps1 expose the same verbs and classes"
)

# "library" is new: build/gate-output.sh is sourced by every gate script and is not an
# entry point, so it is neither a gate (it decides nothing) nor a launcher (nothing
# invokes it). Only the "gate" kind is required to be reached-or-exempt by § 4, so a
# library classifies without claiming a call site it does not have.
readonly KNOWN_KINDS=("gate" "launcher" "provisioning" "library")

# The extension enumerator's alphabet. Deliberately wider than what is present: an
# extension nobody has used yet costs nothing here and closes the hole where the first
# .py or .rb gate arrives unenumerated. It is not the authority on what a script is -
# the shebang enumerator is the independent second opinion, and § 1 requires both.
readonly SCRIPT_EXTENSIONS=("sh" "bash" "ps1" "psm1" "py" "zsh" "ksh" "pl" "rb")

# Outputs and vendored trees, not authored sources.
readonly PRUNED_DIRS=(".git" "artifacts" "generated" ".godot" "obj" "bin" "node_modules")

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
#
# EXEMPT MUST NOT COME TO MEAN NEVER RUNS. Every script on this list runs today only
# when a person types it, which is the defect this whole file exists to name. The
# follow-up is a second, slower CI tier that runs the two expensive ones -
# FOLLOW-UP (OPS-001): a main-branch or nightly job that invokes build/verify-format.sh
# and build/verify-test-harness.sh, where 230 s and 142 s are affordable and where
# test-main already lives. OPS-001 owns the main and nightly suites (delivery-waves
# § Step 4), so it owns this. It is deliberately not built here: this work package's
# subject is the pull-request tier. Until that tier exists, these two and
# verify-godot-runner.sh are gates nothing asks for, and the pull request says so.

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

# The shared emitters. This file used to carry its own pass/fail pair, which was the
# fourth copy in build/; see build/gate-output.sh for why there is now one.
source "${REPO_ROOT}/build/gate-output.sh"

# The check functions below print their own ok/FAIL lines and RETURN their failure
# count, rather than adding to the global. That is what makes § 5's controls in band:
# a control runs the same function against an injected input and asserts the count is
# nonzero, so what the controls exercise is the code the gate itself just ran, not a
# re-implementation of it that could drift from it.
cfail() {
  printf 'FAIL  %s\n' "$*"
  check_failures=$((check_failures + 1))
}

# --- The two enumerators -------------------------------------------------------
# Independent on purpose; § 1 requires them to agree. Both prune the same output
# directories, so a disagreement is about the file, never about where it lives.

prune_expression() {
  local -a expression=()
  local name
  for name in "${PRUNED_DIRS[@]}"; do
    expression+=(-o -name "${name}")
  done
  printf '%s\n' "${expression[@]:1}"
}

# Every regular file under REPO_ROOT that is not in a pruned directory.
all_files() {
  local -a prune=()
  mapfile -t prune < <(prune_expression)
  (cd "${REPO_ROOT}" && find . -type d \( "${prune[@]}" \) -prune -o -type f -print) \
    | sed 's|^\./||' | LC_ALL=C sort
}

# Enumerator A: a known script extension.
enumerate_by_extension() {
  local -a suffixes=()
  local extension
  for extension in "${SCRIPT_EXTENSIONS[@]}"; do
    suffixes+=(-o -name "*.${extension}")
  done
  local -a prune=()
  mapfile -t prune < <(prune_expression)
  (cd "${REPO_ROOT}" && find . -type d \( "${prune[@]}" \) -prune -o \
    -type f \( "${suffixes[@]:1}" \) -print) | sed 's|^\./||' | LC_ALL=C sort
}

# Enumerator B: the first two bytes are `#!`. Reads the file rather than its name, so
# it sees an extensionless script and misses a .ps1 that (legitimately) has no shebang.
#
# One process reads every candidate rather than one `head` per file. That is not a
# micro-optimisation to note in passing: this gate is reached from `build`, and
# build/verify-configurations.sh makes a nested ./build.sh build per configuration, so
# whatever this costs is paid several times inside one test-fast.
enumerate_by_shebang() {
  all_files | python3 -c '
import sys, os
root = sys.argv[1]
for line in sys.stdin:
    path = line.rstrip("\n")
    if not path:
        continue
    try:
        with open(os.path.join(root, path), "rb") as handle:
            if handle.read(2) == b"#!":
                sys.stdout.write(path + "\n")
    except OSError:
        # A file that cannot be opened is not claimed to be a script by this
        # enumerator. If the extension enumerator claims it, § 1 reports the
        # disagreement, which is the fail-closed outcome.
        pass
' "${REPO_ROOT}" | LC_ALL=C sort
}

section "1. enumerate every script two independent ways, and require them to agree"

mapfile -t by_extension < <(enumerate_by_extension)
mapfile -t by_shebang < <(enumerate_by_shebang)

# The union is the candidate set. A file either enumerator claims is a script is
# treated as one, so a disagreement fails closed: the extra file still has to be
# classified, and § 1 still reports the disagreement as a failure of its own.
mapfile -t scripts < <(
  printf '%s\n' "${by_extension[@]}" "${by_shebang[@]}" | grep -v '^$' | LC_ALL=C sort -u
)

# An empty candidate set never satisfies a gate: "no scripts found" is a broken
# enumerator, not a clean repository.
if [[ "${#scripts[@]}" -eq 0 ]]; then
  fail "found no scripts at all; the enumerators are broken, not the repository clean"
  gate_summary "verify-gate-wiring" "${EXIT_VALIDATION}"
  exit "${EXIT_VALIDATION}"
fi

# Given two sorted lists by name, reports every file only one of them found.
# Returns its failure count.
check_enumerators() {
  local -n left="$1"
  local -n right="$2"
  local check_failures=0
  local path

  local only_extension only_shebang
  only_extension="$(LC_ALL=C comm -23 \
    <(printf '%s\n' "${left[@]}" | grep -v '^$' | LC_ALL=C sort -u) \
    <(printf '%s\n' "${right[@]}" | grep -v '^$' | LC_ALL=C sort -u))"
  only_shebang="$(LC_ALL=C comm -13 \
    <(printf '%s\n' "${left[@]}" | grep -v '^$' | LC_ALL=C sort -u) \
    <(printf '%s\n' "${right[@]}" | grep -v '^$' | LC_ALL=C sort -u))"

  while IFS= read -r path; do
    [[ -z "${path}" ]] && continue
    cfail "${path} has a known script extension but no '#!' first line, so only one of the two enumerators sees it. Add a shebang, or add its extension case to the header's account of why the two are kept."
  done <<<"${only_extension}"

  while IFS= read -r path; do
    [[ -z "${path}" ]] && continue
    cfail "${path} starts with '#!' but has no extension in SCRIPT_EXTENSIONS, so only one of the two enumerators sees it. Give it a known extension, or add the extension to SCRIPT_EXTENSIONS. If it is not a script at all and merely begins with those two bytes, this gate has no escape for that and adding one is a deliberate change to what 'script' means here, not a workaround to reach for in passing."
  done <<<"${only_shebang}"

  if [[ "${check_failures}" -eq 0 ]]; then
    pass "the extension enumerator and the shebang enumerator agree exactly, on $(printf '%s\n' "${left[@]}" | grep -c -v '^$') file(s)"
  fi
  return "${check_failures}"
}

check_failures=0
enumerator_report="$(check_enumerators by_extension by_shebang)"
enumerator_failures=$?
printf '%s\n' "${enumerator_report}"
gate_add_failures "${enumerator_failures}"

section "2. the inventory classifies every enumerated script, and nothing else (VER-FND-005-010)"

# Set equality in both directions between INVENTORY's paths and the enumerated set,
# plus a known kind on every entry. This is the check the name glob never made: under
# the glob, a script named anything other than verify-* was not accepted too broadly,
# it was not looked at.
check_inventory() {
  local -n inventory="$1"
  local -n enumerated="$2"
  local check_failures=0
  local entry path kind invocation note found seen_kinds candidate hits

  local -a inventory_paths=()
  for entry in "${inventory[@]}"; do
    inventory_paths+=("${entry%%|*}")
  done

  # Direction 1: every entry names a file an enumerator found.
  for entry in "${inventory[@]}"; do
    path="${entry%%|*}"
    kind="$(printf '%s' "${entry}" | cut -d'|' -f2)"
    invocation="$(printf '%s' "${entry}" | cut -d'|' -f3)"
    note="$(printf '%s' "${entry}" | cut -d'|' -f4-)"

    found=no
    for candidate in "${enumerated[@]}"; do
      [[ "${candidate}" == "${path}" ]] && found=yes && break
    done
    if [[ "${found}" == "no" ]]; then
      if [[ -e "${REPO_ROOT}/${path}" ]]; then
        cfail "the inventory classifies ${path}, which exists but which neither enumerator recognises as a script. Either it is not a script and does not belong here, or the enumerators cannot see it - which is the hole this section exists to catch."
      else
        cfail "stale classification: the inventory classifies ${path}, which does not exist. An inventory entry for a deleted file is the same defect as a stale exemption."
      fi
      continue
    fi

    seen_kinds=no
    for candidate in "${KNOWN_KINDS[@]}"; do
      [[ "${candidate}" == "${kind}" ]] && seen_kinds=yes && break
    done
    if [[ "${seen_kinds}" == "no" ]]; then
      cfail "the inventory gives ${path} the kind '${kind}', which is not one of: ${KNOWN_KINDS[*]}. A kind this file does not know is a classification nothing acts on."
      continue
    fi

    if [[ -z "${note}" ]]; then
      cfail "the inventory classifies ${path} as '${kind}' and states no reason. The classification is the judgement this file cannot check, so it has to be written down."
      continue
    fi

    if [[ -n "${invocation}" ]]; then
      pass "classified: ${path} -> ${kind} (only as '${path} ${invocation}')"
    else
      pass "classified: ${path} -> ${kind}"
    fi
  done

  # Direction 2: every enumerated script is classified, exactly once.
  for path in "${enumerated[@]}"; do
    hits=0
    for candidate in "${inventory_paths[@]}"; do
      [[ "${candidate}" == "${path}" ]] && hits=$((hits + 1))
    done
    if [[ "${hits}" -eq 0 ]]; then
      cfail "${path} is a script that the inventory does not classify. Add it to INVENTORY in build/verify-gate-wiring.sh as gate, launcher or provisioning, with the reason. If it is a gate, §§ 3 and 4 then apply to it; if it is not, saying so in a committed file is the point."
    elif [[ "${hits}" -gt 1 ]]; then
      cfail "${path} is classified ${hits} times in the inventory. Two classifications of one file is not a partition, and which one §§ 3 and 4 would use is undefined."
    fi
  done

  return "${check_failures}"
}

check_failures=0
inventory_report="$(check_inventory INVENTORY scripts)"
inventory_failures=$?
printf '%s\n' "${inventory_report}"
gate_add_failures "${inventory_failures}"

# The gate subset, and the arguments each gate needs, taken from the inventory. §§ 3
# and 4 are about these and not about launchers or provisioning.
#
# gates_of and invocations_of read an inventory array by name so that § 5 can derive
# them from an injected inventory the same way this run derives them from the real one.
gates_of() {
  local -n source_inventory="$1"
  local entry
  for entry in "${source_inventory[@]}"; do
    if [[ "$(printf '%s' "${entry}" | cut -d'|' -f2)" == "gate" ]]; then
      printf '%s\n' "${entry%%|*}"
    fi
  done
}

# "<path>|<arguments>" for every gate whose gate mode needs arguments. Empty output
# when no entry sets field 3, which is the case at this revision.
invocations_of() {
  local -n source_inventory="$1"
  local entry invocation
  for entry in "${source_inventory[@]}"; do
    [[ "$(printf '%s' "${entry}" | cut -d'|' -f2)" == "gate" ]] || continue
    invocation="$(printf '%s' "${entry}" | cut -d'|' -f3)"
    [[ -n "${invocation}" ]] && printf '%s|%s\n' "${entry%%|*}" "${invocation}"
  done
}

mapfile -t gates < <(gates_of INVENTORY)
mapfile -t invocations < <(invocations_of INVENTORY)

echo
printf '      %s of %s classified script(s) are gates:\n' "${#gates[@]}" "${#scripts[@]}"
for path in "${gates[@]}"; do
  printf '      %s\n' "${path}"
done

# --- The call sites, resolved once --------------------------------------------
# Rows are
#   "<script>\t<file>:<line>\t<verbs>\t<yes|no from a workflow>\t<bare|args-ok|args-missing>".
#
# The program is written to a file rather than piped, because § 5 runs it a second time
# against a copy of the search roots with a call site renamed. That control cannot
# mutate a tracked file - a gate that edits the repository it is checking is not a gate -
# so it copies, mutates the copy, and points the same analyzer at it.

program="$(mktemp)"
sites_table="$(mktemp)"
control_root="$(mktemp -d)"
CONTROL_FIXTURES=()

cleanup() {
  rm -f "${program}" "${sites_table}" "${sites_table}.err"
  rm -rf "${control_root}"
  local fixture
  for fixture in "${CONTROL_FIXTURES[@]-}"; do
    [[ -n "${fixture}" ]] && rm -f "${REPO_ROOT}/${fixture}"
  done
}
trap cleanup EXIT

cat >"${program}" <<'CALL_SITES'
"""Prints every real call site of every named gate script, with its reachability.

    <repo-root> --roots <root>... --scripts <path>... [--invocations <path>|<args>...]

One tab-separated row per accepted call site:

    <script>  <file>:<line>  <verbs that reach it>  <yes|no reachable from a workflow>
    <bare|args-ok|args-missing>

and nothing for a script with no call site. A mention - prose, a comment, a
diagnostic message, an unused constant - is not a call site and produces no row.

--invocations names the arguments that make a dual-mode script a gate. The fifth
column is 'bare' when no arguments are required, 'args-ok' when every required token
appears in the same C# member or on the same shell command line as the path, and
'args-missing' otherwise. See the header's note on that check's limit: same member is
not same call site.
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
    roots, scripts, invocations, bucket = [], [], [], None
    for token in argv[1:]:
        if token == "--roots":
            bucket = roots
        elif token == "--scripts":
            bucket = scripts
        elif token == "--invocations":
            bucket = invocations
        elif bucket is not None:
            bucket.append(token)
    roots = [root for root in roots if os.path.exists(os.path.join(root_dir, root))]

    # path -> the argument tokens that select the gate mode.
    required = {}
    for entry in invocations:
        path, _, arguments = entry.partition("|")
        tokens = arguments.split()
        if tokens:
            required[path] = tokens

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

    def invocation_verdict(script, haystack):
        """'bare', 'args-ok' or 'args-missing' for one call site.

        The haystack is the enclosing C# member's text, or the one shell line. That is
        the honest granularity available here and it is not proof that the arguments are
        what this call site passes; the header says so where a reader will meet it.
        """
        tokens = required.get(script)
        if not tokens:
            return "bare"
        return "args-ok" if all(token in haystack for token in tokens) else "args-missing"

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
                    invocation_verdict(script, members[key].text),
                ))

        for relative in shell_files:
            in_workflow = relative.startswith(".github/workflows/")
            is_wrapper = relative in ("build.sh", "build.ps1")
            for number, raw in enumerate(read(os.path.join(root_dir, relative)).splitlines(), 1):
                stripped = strip_shell_comment(raw)
                if shell_regex.search(stripped) is None:
                    continue
                if in_workflow:
                    where, reached = "(workflow " + os.path.basename(relative) + ")", "yes"
                elif is_wrapper:
                    where, reached = "(root wrapper " + relative + ")", "yes"
                else:
                    where, reached = "(" + relative + ", neither a workflow nor a wrapper)", "no"
                rows.add((
                    script,
                    relative + ":" + str(number),
                    where,
                    reached,
                    invocation_verdict(script, stripped),
                ))

    for row in sorted(rows):
        sys.stdout.write("\t".join(row) + "\n")
    return 0


raise SystemExit(main())
CALL_SITES

# resolve_sites <root-dir> <output-file> <invocations-array-name> <script>...
# Runs the analyzer above. § 5 calls it a second time with a different root.
resolve_sites() {
  local root="$1" out="$2" invocation_array="$3"
  shift 3
  local -n requirements="${invocation_array}"
  local -a arguments=("${root}" --roots "${SEARCH_ROOTS[@]}" --scripts "$@")
  if [[ "${#requirements[@]}" -gt 0 ]]; then
    arguments+=(--invocations "${requirements[@]}")
  fi
  python3 "${program}" "${arguments[@]}" >"${out}" 2>"${out}.err"
}

if ! resolve_sites "${REPO_ROOT}" "${sites_table}" invocations "${gates[@]}"; then
  fail "the call-site analysis did not run; this gate cannot report a partition it did not compute"
  sed 's/^/      /' "${sites_table}.err"
  gate_summary "verify-gate-wiring" "${EXIT_VALIDATION}"
  exit "${EXIT_VALIDATION}"
fi

# Every call site of one script, reachable from a workflow or not.
all_sites() {
  awk -F'\t' -v want="$1" '$1 == want { print }' "$2"
}

# Only the call sites a workflow reaches AND whose required arguments are present.
# This is what "invoked" means. A call site with args-missing is a reachable bare
# invocation of a dual-mode script, which is precisely the case the invocation field
# exists to reject: it runs the script and it does not run the gate.
reached_sites() {
  awk -F'\t' -v want="$1" '$1 == want && $4 == "yes" && $5 != "args-missing" { print }' "$2"
}

is_exempt() {
  local path="$1" array_name="$2" entry
  local -n exemptions="${array_name}"
  for entry in "${exemptions[@]}"; do
    [[ "${entry%%|*}" == "${path}" ]] && return 0
  done
  return 1
}

# check_exemptions <exempt-array> <sites-table>
check_exemptions() {
  local -n exemptions="$1"
  local table="$2"
  local check_failures=0
  local entry exempt_path reason

  if [[ "${#exemptions[@]}" -eq 0 ]]; then
    pass "the exemption list is empty, so every gate script must be reached"
    return 0
  fi

  for entry in "${exemptions[@]}"; do
    exempt_path="${entry%%|*}"
    reason="${entry#*|}"

    if [[ ! -f "${REPO_ROOT}/${exempt_path}" ]]; then
      cfail "stale exemption: ${exempt_path} does not exist"
      continue
    fi

    if [[ -z "${reason}" || "${reason}" == "${entry}" ]]; then
      cfail "exemption for ${exempt_path} states no reason"
      continue
    fi

    if [[ -n "$(reached_sites "${exempt_path}" "${table}")" ]]; then
      cfail "${exempt_path} is exempted but is in fact reached from a workflow; remove the exemption"
      continue
    fi

    pass "exempt: ${exempt_path} (${reason})"
  done
  return "${check_failures}"
}

# check_partition <gates-array> <exempt-array> <sites-table>
check_partition() {
  local -n gate_paths="$1"
  local exempt_array="$2"
  local table="$3"
  local check_failures=0
  local path reached first unreached

  for path in "${gate_paths[@]}"; do
    reached="$(reached_sites "${path}" "${table}")"

    if [[ -n "${reached}" ]]; then
      if is_exempt "${path}" "${exempt_array}"; then
        # Reported by the exemption section as well; repeated here so the partition's
        # own statement is complete in one place.
        cfail "${path} is both reached and exempt"
        continue
      fi
      first="$(printf '%s\n' "${reached}" | head -n 1)"
      pass "reached: ${path}  <-  $(printf '%s' "${first}" | cut -f2) (verb $(printf '%s' "${first}" | cut -f3), $(printf '%s' "${first}" | cut -f5))"
      continue
    fi

    if is_exempt "${path}" "${exempt_array}"; then
      continue
    fi

    unreached="$(all_sites "${path}" "${table}")"
    if [[ -n "${unreached}" ]]; then
      cfail "${path} has a call site, but no workflow reaches it with the invocation the inventory requires."
      printf '%s\n' "${unreached}" | while IFS=$'\t' read -r _ where verbs from_workflow arguments; do
        printf '      %s: verbs %s, from a workflow %s, arguments %s\n' \
          "${where}" "${verbs}" "${from_workflow}" "${arguments}"
      done
      printf '      A gate invoked by a verb no workflow runs is a gate nobody runs, and a\n'
      printf '      dual-mode gate invoked without the arguments that select its gate mode is\n'
      printf '      a script that runs and a gate that does not. Either move the call site into\n'
      printf '      a verb the workflow invokes, pass the arguments the inventory names, add the\n'
      printf '      verb to a workflow, or exempt it in build/verify-gate-wiring.sh with the reason.\n'
      continue
    fi

    cfail "${path} is never invoked and is not on the deliberately-unwired list."
    printf '      It runs only when a person remembers to type it, so any report of\n'
    printf '      "all gates green" silently excludes it. Wire it into a verb some\n'
    printf '      workflow runs, or add it to EXEMPT in build/verify-gate-wiring.sh\n'
    printf '      with the reason.\n'
  done
  return "${check_failures}"
}

section "3. every exemption names a script that exists and is not reached"

check_failures=0
exemption_report="$(check_exemptions EXEMPT "${sites_table}")"
exemption_failures=$?
printf '%s\n' "${exemption_report}"
gate_add_failures "${exemption_failures}"

section "4. every gate script is reached from a workflow or exempt, and never both"

check_failures=0
partition_report="$(check_partition gates EXEMPT "${sites_table}")"
partition_failures=$?
printf '%s\n' "${partition_report}"
gate_add_failures "${partition_failures}"

# --- § 5: the negative controls ------------------------------------------------
# In band, in this script, on every run, for the same reason FND-004 carries its
# seventeen here rather than in a note: a control that lives in a registry summary is a
# claim about a control, and the only thing that makes a red observation a fact is
# running it. VER-FND-005-010 claimed three of these and committed none; two of the
# three had never been run at all.
#
# Each control states the injected violation and the class it expects. They inject
# inputs into the very functions §§ 1-4 just ran - not copies of them - so a control
# cannot pass against logic the gate does not use.

section "5. negative controls: each check above can actually fail (VER-FND-005-010)"

controls_run=0
readonly EXPECTED_CONTROLS=8

# expect_red <name> <expected-failure-count-at-least> <report> <count>
# Every line this prints is manufactured by a control's fixture, INCLUDING the FAIL lines
# it quotes out of the report, so all of it goes through the marked emitters.
#
# That quoting is the confirmation trap this marking exists for. § 5 runs eight controls
# whose fixtures produce real-looking failure text - a synthetic
# verify-zzz-unclassified-control.sh being unclassified, a deliberately broken
# verify-godot.sh call site - and every one of those lines is printed on a GREEN run. A
# reader who predicted a cause, grepped this log for the string that cause would produce,
# and found it here would stop looking. That happened: a session found its expected string
# twice inside these fixtures and nearly shipped a fix for the wrong section, when the real
# failure was § 2's classification check covering three scripts rather than § 4's wiring
# check covering one. `grep -v '[control-fixture]'` now leaves only genuine findings, and
# the summary names the failing section so the log does not have to be grepped at all.
expect_red() {
  local name="$1" want="$2" report="$3" count="$4"
  controls_run=$((controls_run + 1))
  if [[ "${count}" -ge "${want}" ]]; then
    control_pass "control: ${name} -> ${count} failure(s), as designed"
    control_detail < <(grep '^FAIL' <<<"${report}")
  else
    control_fail "control: ${name} produced ${count} failure(s); the check it exercises cannot fail, so its green means nothing"
    control_detail <<<"${report}"
  fi
}

# --- 5a. a new script that nothing classifies ---------------------------------
# The injected violation is a real file, so both enumerators have to see it and § 2 has
# to notice it is unclassified. This is the control VER-FND-005-010 described as
# "a new build/verify-zzz-probe.sh that is neither wired nor listed".
readonly UNCLASSIFIED_FIXTURE="build/verify-zzz-unclassified-control.sh"
CONTROL_FIXTURES+=("${UNCLASSIFIED_FIXTURE}")
cat >"${REPO_ROOT}/${UNCLASSIFIED_FIXTURE}" <<'FIXTURE'
#!/usr/bin/env bash
# Deliberately unclassified script, written and removed by build/verify-gate-wiring.sh.
exit 0
FIXTURE

mapfile -t control_extension < <(enumerate_by_extension)
mapfile -t control_shebang < <(enumerate_by_shebang)
mapfile -t control_scripts < <(
  printf '%s\n' "${control_extension[@]}" "${control_shebang[@]}" \
    | grep -v '^$' | LC_ALL=C sort -u
)

check_failures=0
report="$(check_inventory INVENTORY control_scripts)"
count=$?
expect_red "an unclassified script in the tree" 1 "${report}" "${count}"

# The same fixture, now classified as a gate that nothing calls. Two distinct defects
# hide behind one file: not being classified, and being classified and never run.
control_inventory=("${INVENTORY[@]}" "${UNCLASSIFIED_FIXTURE}|gate||a control fixture, classified so that the partition rather than the inventory is what has to reject it")
mapfile -t control_gates < <(gates_of control_inventory)

check_failures=0
report="$(check_partition control_gates EXEMPT "${sites_table}")"
count=$?
expect_red "a gate that is classified, unwired and unexempted" 1 "${report}" "${count}"

rm -f "${REPO_ROOT}/${UNCLASSIFIED_FIXTURE}"

# --- 5b. the two enumerators disagreeing --------------------------------------
# Two real files, one each way. A shebang with no known extension is the hole the
# extension enumerator has; a known extension with no shebang is the hole the shebang
# enumerator has. Neither is hypothetical - .ps1 needs no shebang.
readonly SHEBANG_ONLY_FIXTURE="build/zzz-shebang-only-control"
readonly EXTENSION_ONLY_FIXTURE="build/zzz-extension-only-control.sh"
CONTROL_FIXTURES+=("${SHEBANG_ONLY_FIXTURE}" "${EXTENSION_ONLY_FIXTURE}")

printf '#!/usr/bin/env bash\n# control fixture, removed by build/verify-gate-wiring.sh\nexit 0\n' \
  >"${REPO_ROOT}/${SHEBANG_ONLY_FIXTURE}"
printf '# control fixture with no shebang, removed by build/verify-gate-wiring.sh\nexit 0\n' \
  >"${REPO_ROOT}/${EXTENSION_ONLY_FIXTURE}"

mapfile -t control_extension < <(enumerate_by_extension)
mapfile -t control_shebang < <(enumerate_by_shebang)

check_failures=0
report="$(check_enumerators control_extension control_shebang)"
count=$?
expect_red "a shebang with no known extension, and a known extension with no shebang" 2 \
  "${report}" "${count}"

rm -f "${REPO_ROOT}/${SHEBANG_ONLY_FIXTURE}" "${REPO_ROOT}/${EXTENSION_ONLY_FIXTURE}"

# The fixtures must be gone, or the gate has littered the tree it is checking. Compared
# against § 1's own verdict rather than against zero, so a pre-existing disagreement -
# which § 1 already failed on - is not reported a second time here.
mapfile -t control_extension < <(enumerate_by_extension)
mapfile -t control_shebang < <(enumerate_by_shebang)
check_failures=0
report="$(check_enumerators control_extension control_shebang)"
count=$?
if [[ "${count}" -eq "${enumerator_failures}" ]]; then
  control_pass "control fixtures removed: the enumerators report what they reported in § 1"
else
  control_fail "control fixtures were not cleaned up: the enumerators now report ${count} failure(s), § 1 saw ${enumerator_failures}"
fi

# --- 5c. a classification that names a file that is not there -----------------
control_inventory=("${INVENTORY[@]}" "build/verify-does-not-exist.sh|gate||a control entry for a file that was never here")
check_failures=0
report="$(check_inventory control_inventory scripts)"
count=$?
expect_red "an inventory entry naming a nonexistent file" 1 "${report}" "${count}"

# An unknown kind, which is the other way an entry can be wrong without being stale.
control_inventory=("${INVENTORY[@]/build\/verify-godot.sh|gate|/build\/verify-godot.sh|probably-a-gate|}")
check_failures=0
report="$(check_inventory control_inventory scripts)"
count=$?
expect_red "an inventory entry with a kind this file does not know" 1 "${report}" "${count}"

# --- 5d. a stale exemption ----------------------------------------------------
# VER-FND-005-010's second claimed control. It had never been run.
control_exempt=("${EXEMPT[@]}" "build/verify-was-deleted.sh|a control exemption for a script that does not exist")
check_failures=0
report="$(check_exemptions control_exempt "${sites_table}")"
count=$?
expect_red "an exemption naming a script that does not exist" 1 "${report}" "${count}"

# --- 5e. a renamed call site --------------------------------------------------
# VER-FND-005-010's third claimed control, and the only one that needs the analyzer
# rather than the shell logic: "renaming the godot-import verb's call site away from
# build/verify-godot.sh fails the check by name". It cannot mutate a tracked file -
# a gate that edits the repository it is checking is not a gate - so it copies the
# search roots, renames the call site in the COPY, and points the same analyzer at it.
readonly RENAME_TARGET="build/verify-godot.sh"
readonly RENAME_REPLACEMENT="build/verify-godot-renamed-by-a-control.sh"

for root in "${SEARCH_ROOTS[@]}"; do
  [[ -e "${REPO_ROOT}/${root}" ]] || continue
  mkdir -p "${control_root}/$(dirname -- "${root}")"
  cp -R "${REPO_ROOT}/${root}" "${control_root}/${root}"
done

# Only the call sites move. The script itself is not copied and not renamed: the
# defect being injected is "the verb now calls something else", which is exactly what a
# rename that forgets a call site produces.
while IFS= read -r file; do
  LC_ALL=C sed -i "s|${RENAME_TARGET}|${RENAME_REPLACEMENT}|g" "${file}"
done < <(grep -rl --binary-files=without-match -F "${RENAME_TARGET}" "${control_root}" 2>/dev/null)

control_sites="$(mktemp "${control_root}/sites.XXXXXX")"
if ! resolve_sites "${control_root}" "${control_sites}" invocations "${gates[@]}"; then
  control_fail "control: the renamed-call-site control could not run the analyzer at all"
  control_detail < <(sed 's/^/        /' "${control_sites}.err")
  controls_run=$((controls_run + 1))
else
  check_failures=0
  report="$(check_partition gates EXEMPT "${control_sites}")"
  count=$?
  expect_red "the godot-import call site renamed away from ${RENAME_TARGET}" 1 "${report}" "${count}"
  # Here-string, not a pipe: `grep -q` exits on its first match, printf takes SIGPIPE, and
  # `set -o pipefail` reports 141 - which on this negated test reads as "the report does not
  # name it" and would fabricate a control failure. See delivery-waves § Decision 13.
  if ! grep -q "${RENAME_TARGET}" <<<"${report}"; then
    control_fail "control: the renamed-call-site control went red without naming ${RENAME_TARGET}; a failure that does not say which gate is unrun is not the one this control is for"
  fi
fi

# --- 5f. a dual-mode gate reached only bare -----------------------------------
# The invocation field's own control. No entry sets field 3 at this revision, so the
# control supplies one: verify-godot.sh is really reached from godot-import, and no call
# site passes --verify, so an entry requiring --verify must turn that reach into a
# failure. Without this the mechanism would be committed and unobserved, which is the
# defect this section exists to stop repeating.
control_inventory=("${INVENTORY[@]/build\/verify-godot.sh|gate||/build\/verify-godot.sh|gate|--verify|}")
mapfile -t control_invocations < <(invocations_of control_inventory)
control_dual_sites="$(mktemp "${control_root}/dual.XXXXXX")"
if ! resolve_sites "${REPO_ROOT}" "${control_dual_sites}" control_invocations "${gates[@]}"; then
  control_fail "control: the dual-mode control could not run the analyzer at all"
  control_detail < <(sed 's/^/        /' "${control_dual_sites}.err")
  controls_run=$((controls_run + 1))
else
  check_failures=0
  report="$(check_partition gates EXEMPT "${control_dual_sites}")"
  count=$?
  expect_red "a gate whose inventory entry requires --verify, reached only bare" 1 \
    "${report}" "${count}"
fi

# A control set that quietly shrinks proves less than it claims, so the count is
# asserted rather than assumed - FND-004's reason for doing the same.
echo
# Deliberately unmarked, unlike everything else in § 5: this is an assertion about the
# control SET rather than output manufactured by a control, it quotes no fixture text, and
# "the control set shrank" is precisely a finding a reader excluding control output still
# needs to see.
if [[ "${controls_run}" -eq "${EXPECTED_CONTROLS}" ]]; then
  pass "all ${EXPECTED_CONTROLS} negative controls ran"
else
  fail "${controls_run} of ${EXPECTED_CONTROLS} negative controls ran; a control set that shrank proves less than it claims"
fi

# This gate's own § 5 is eight in-band controls, so it is the strongest instance of the
# problem gate_assert_marking guards. Prove the separation still holds before summarising.
gate_assert_marking

gate_summary "verify-gate-wiring" "${EXIT_VALIDATION}"
