#!/usr/bin/env bash
#
# Proves the Godot project imports and launches headlessly from a cold cache.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Godot import and export, § Continuous integration ("CI jobs start
#              from clean checkouts and cannot depend on a developer's Godot import
#              cache")
#            docs/technical/00-technical-foundation.md § Renderer baseline
# Requirements: TR-FND-001, TR-FND-002, TR-BLD-002
# Verification: VER-FND-001-012 (headless import), VER-FND-001-013 (headless launch)
#
# Why this is a script and not just two commands:
#
#   Godot returns exit code 0 from a headless launch even when the C# script on the
#   boot node fails to load - it logs "Cannot instantiate C# script" and carries on.
#   Asserting only the exit code would pass a completely broken project. This script
#   asserts the exit code AND the composition root's stable startup line AND the
#   absence of engine ERROR lines.
#
# Build-order fact this script encodes: Godot.NET.Sdk puts both obj/ and bin/ for
# MechaMiner.Game inside game/.godot/mono/temp/, and .godot is gitignored. So a cold
# cache means no restore assets and no game assembly, and the order must be
# restore -> build -> import -> launch. FND-002's build and godot-import verbs
# inherit this ordering.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure, 5 build/import failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly GAME_DIR="${REPO_ROOT}/game"

# The scope of the no-mutation probe below. game/ and nothing else: this gate's subject is the
# Godot project, and .gitignore:15 ignores .godot/, so the cold-cache removal at the top of the
# run and every byte dotnet puts under game/.godot/mono/temp/ are invisible to both legs BY
# DESIGN. What is left in scope is what a Godot import can really rewrite - the committed
# import sidecars, `git ls-files game` naming game/project.godot plus one *.cs.uid per script -
# and that is the real coverage of this probe.
readonly MUTATION_PATHSPEC=(game)
readonly MUTATION_SCOPE="game/"
readonly STARTUP_LINE="MechaMiner: boot composition root ready"
readonly LAUNCH_FRAMES=60
readonly EXIT_VALIDATION=4
readonly EXIT_BUILD=5

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

strip_ansi() {
  sed -e 's/\x1b\[[0-9;]*m//g'
}

engine_problem_lines() {
  grep -E '(^|[[:space:]])(ERROR|WARNING|SCRIPT ERROR|USER ERROR):' || true
}

# --- the no-mutation probe's machinery ----------------------------------------------------
#
# WHY THIS IS TWO CAPTURES AND A SEPARATE PREDICATE, where the section below used to be a
# single `git status --porcelain -- game` read and an `[[ -z ... ]]` test.
#
# The section's claim is "import and launch changed nothing". The old read compared the tree
# against HEAD AND THE INDEX at one moment AFTER the work, which is the different claim
# "game/ is clean", and the difference was not academic. Anyone holding an unrelated
# uncommitted edit under game/ got `FAIL import or launch mutated tracked files` naming a
# cause that was not the cause; and a rerun after a red run that had left the tree dirty went
# red again for what the FIRST run did. The captures are therefore taken BEFORE the first
# line that touches the tree - the .godot removal immediately below - and again after the
# launch, so the window spans this gate's own work and nothing else.
#
# The captures and the predicate are IDENTICAL IN NAME AND SIGNATURE to
# build/verify-run-slice.sh's, which solved the same problem for a wider pathspec: a reader
# who knows that gate's § 6 knows this one. FOLLOW-UP: they are duplicated rather than
# shared, because hoisting them into build/gate-output.sh edits a file every gate sources.

# LEG 1, CONTENT. git's own summary of which paths in scope are modified, staged or untracked.
# It is CONTENT-DERIVED BY CONSTRUCTION and therefore blind to writing twice over: a
# deterministic byte-identical rewrite leaves every porcelain line exactly where it was, and
# so does a write followed by a cleanup. Godot's import sidecars are deterministic outputs of
# a pinned project, so a re-import that rewrites them byte for byte is exactly the write this
# leg cannot see - which is why leg 2 exists. § negative controls MEASURES that blindness
# over a real rewrite rather than asserting it here as prose.
capture_tree_content() {
  local root="$1"
  shift
  (cd -- "${root}" && git status --porcelain -- "$@") 2>&1
}

# LEG 2, METADATA. mtime, inode and size of every TRACKED file in scope, in git's own sorted
# order. This is the leg whose subject is WRITING rather than content: a byte-identical
# rewrite in place moves %Y, a rewrite through a temp file and a rename moves %i, and neither
# moves anything leg 1 can see.
#
# WHAT IT DOES NOT COVER, said rather than implied: untracked files. A file created and
# deleted inside the window appears in neither leg, because `git ls-files` never named it.
# Leg 2's pass message says so.
#
# WHY A LOCAL RED HERE MAY NOT BE THIS GATE'S DOING, AND WHY THE FIX IS NEVER TO NARROW
# MUTATION_PATHSPEC. Any write to a tracked file under game/ inside the window moves an mtime
# and reds this leg, including a write this gate had nothing to do with.
#
#   IN CI THE HAZARD DOES NOT EXIST. The workflow runs on a clean checkout and nothing else
#   writes to the tree while the job runs, so the tree is STATIC across the window and a red
#   there means a write by this gate, by dotnet, or by the Godot import or launch it ran.
#   THE HAZARD IS DEVELOPER-LOCAL ONLY.
#
#   SO ON A DEVELOPER MACHINE THE QUESTION IS *WHAT CHANGED*, NOT *IS THE PATHSPEC TOO WIDE*.
#   A concurrent edit to a file under game/ by a person, an editor, a rebase or a sibling
#   agent reds this leg TRUTHFULLY - something in scope really was written - but confusingly,
#   because this gate is not what wrote it. The verdict names the paths and the mtimes; re-run
#   on a quiet tree and the leg goes green.
#
#   NARROWING THE PATHSPEC TO MAKE THIS QUIETER IS THE WRONG FIX. It buys a green by dropping
#   coverage of the very sidecars this probe exists to watch, and it is bought against a
#   hazard that does not exist in the only place the gate runs unattended.
capture_tree_metadata() {
  local root="$1"
  shift
  (
    cd -- "${root}" || exit 3
    # NUL-delimited, so a path containing a space or a newline cannot merge two entries into
    # one and hide a change inside the join. `-r` so an empty file set yields an empty capture
    # rather than a stat over the current directory. No `| head` and no `| sort`: the order is
    # git's and already deterministic, and a downstream command exiting first is the
    # pipefail-141 shape this gate refuses everywhere else.
    git ls-files -z -- "$@" | xargs -0 -r stat -c "%n"$'\t'"%Y %i %s" --
  ) 2>&1
}

# THE NO-MUTATION PREDICATE, EXTRACTED SO A CONTROL CAN ACTUALLY DRIVE IT.
#
# Inline, it was the one predicate in this gate no fixture could reach. The control at the
# foot of this file drove only the unreadable-git route; nothing anywhere demonstrated that
# the emptiness test could tell a mutated tree from an unmutated one, and a control BESIDE a
# predicate is not a control OF it. § negative controls now drives THIS function.
#
# $1 a phrase naming what was measured AND of what - printed verbatim into the verdict, so a
# control over a throwaway repository cannot claim to have measured game/. $2 the BEFORE
# capture, $3 the AFTER capture, $4 the BEFORE capture's exit status, $5 the AFTER capture's.
#
# Prints one problem per line and returns the count.
classify_tree_mutation() {
  local subject="$1" before="$2" after="$3" before_status="$4" after_status="$5"

  # STATUS BEFORE OUTPUT, on BOTH ends, and this ordering is why the statuses are parameters
  # at all. The empty output of a FAILED capture is indistinguishable from the empty output of
  # a clean one, so two failed captures compare EQUAL and would report a green built entirely
  # out of the failure. This preserves the hardening the single-read version already carried -
  # a read failure stays a failure and never becomes a pass - and extends it to the baseline,
  # because an unreadable baseline is exactly as unproved as an unreadable after-state.
  if [[ "${before_status}" -ne 0 || "${after_status}" -ne 0 ]]; then
    printf 'the %s could not be read (exit %s before, %s after), so mutation is unproved rather than absent: two failed reads compare equal, and a pass built on that comparison would be an artefact of the failure rather than a measurement. Capture text: %s\n' \
      "${subject}" "${before_status}" "${after_status}" "${after//$'\n'/ | }"
    return 1
  fi

  if [[ "${before}" != "${after}" ]]; then
    printf 'the %s changed across the window. Before: %s After: %s\n' \
      "${subject}" "${before//$'\n'/ | }" "${after//$'\n'/ | }"
    return 1
  fi

  return 0
}

# THE BASELINE, taken before the .godot removal below, which is this gate's first
# tree-touching line. Nothing between this point and the after-read at § no tracked file may
# be mutated is anything but the gate's own work, which is what makes the comparison mean
# "import and launch changed nothing".
tree_content_before=""
tree_content_before_status=0
tree_content_before="$(capture_tree_content "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || tree_content_before_status=$?
tree_metadata_before=""
tree_metadata_before_status=0
tree_metadata_before="$(capture_tree_metadata "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || tree_metadata_before_status=$?

section "cold cache: removing game/.godot"
rm -rf "${GAME_DIR}/.godot"

section "restore and build the game assembly (its obj/bin live inside .godot)"
if ! dotnet build "${GAME_DIR}/MechaMiner.Game.csproj" --nologo -v q; then
  fail "the game project must build before Godot can load its assembly"
  exit "${EXIT_BUILD}"
fi
pass "MechaMiner.Game built"

section "VER-FND-001-012: godot headless import"
import_log="$(godot --headless --path "${GAME_DIR}" --import 2>&1 | strip_ansi)"
import_status="${PIPESTATUS[0]}"
if [[ "${import_status}" -ne 0 ]]; then
  fail "headless import exited ${import_status}, expected 0"
else
  pass "headless import exited 0"
fi
import_problems="$(printf '%s\n' "${import_log}" | engine_problem_lines)"
if [[ -n "${import_problems}" ]]; then
  fail "headless import reported engine problems"
  printf '%s\n' "${import_problems}" | sed 's/^/      /'
else
  pass "headless import reported no ERROR or WARNING line"
fi

section "VER-FND-001-013: godot headless launch"
launch_log="$(godot --headless --path "${GAME_DIR}" --quit-after "${LAUNCH_FRAMES}" 2>&1 | strip_ansi)"
launch_status="${PIPESTATUS[0]}"
if [[ "${launch_status}" -ne 0 ]]; then
  fail "headless launch exited ${launch_status}, expected 0"
else
  pass "headless launch exited 0"
fi
# A here-string, not `printf | grep -q`: under `set -o pipefail` grep -q exits on
# the first match, printf is killed by SIGPIPE, and the pipeline status becomes 141
# even though the line was found - which would report a launch that DID print the
# startup line as one that did not. The longer the log the likelier the race.
if grep -qF "${STARTUP_LINE}" <<<"${launch_log}"; then
  pass "composition root printed its stable startup line"
else
  fail "startup line not found; the boot scene did not reach managed code"
  printf '%s\n' "${launch_log}" | sed 's/^/      /'
fi
launch_problems="$(printf '%s\n' "${launch_log}" | engine_problem_lines)"
if [[ -n "${launch_problems}" ]]; then
  fail "headless launch reported engine problems (Godot exits 0 even for these)"
  printf '%s\n' "${launch_problems}" | sed 's/^/      /'
else
  pass "headless launch reported no ERROR or WARNING line"
fi

section "no tracked file may be mutated by import or launch"
#
# TWO LEGS, TWO FINDINGS. "The tracked content did not change" and "nothing was written" are
# DIFFERENT CLAIMS, and only the second is about writing. A single verdict would let a reader
# take the weaker measurement for the stronger claim - which is precisely what one
# content-derived read did here for as long as this section had one leg. Each pass message
# claims what its own leg measured and says nothing about the other's subject.
#
# WHAT THIS SECTION NO LONGER DETECTS, stated because a green line here is read as "the tree
# is intact". A before/after comparison cannot see a mutation that was ALREADY PRESENT when
# this gate started - including a tracked file a PREVIOUS run of this gate rewrote and left
# dirty. Under the old HEAD comparison that stayed red on every subsequent run until someone
# reverted it; now run 1 reds and run 2 goes green, because run 2's baseline already contains
# run 1's damage. That trade is accepted deliberately - the alternative reds every developer
# holding uncommitted work under game/, which is the symptom being repaired - and it is
# DISCLOSED rather than absorbed: leg 1's pass line carries the number of pre-existing changes
# it held out of the comparison, so a green run that stepped over dirt says how much.
mutated=""
mutated_status=0
mutated="$(capture_tree_content "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" || mutated_status=$?
mutated_metadata=""
mutated_metadata_status=0
mutated_metadata="$(capture_tree_metadata "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || mutated_metadata_status=$?

# One pass site and one fail site rather than four, so both legs are worded to the same
# discipline. Emitted through pass/fail, NOT control_pass/control_fail: this is an ordinary
# assertion about the repository, and the window it spans holds only the gate's real work.
#
# $1 subject phrase for the predicate, $2 before, $3 after, $4 before status, $5 after status,
# $6 the pass message, which must claim ONLY what this leg measured.
report_mutation_leg() {
  local subject="$1" before="$2" after="$3" before_status="$4" after_status="$5"
  local pass_message="$6"
  local leg_report=""
  local leg_count=0
  leg_report="$(classify_tree_mutation "${subject}" "${before}" "${after}" \
    "${before_status}" "${after_status}")" || leg_count=$?
  if [[ "${leg_count}" -eq 0 ]]; then
    pass "${pass_message}"
  else
    fail "${leg_report}"
  fi
}

# The held-out count: how many changes under game/ were already there before the window
# opened. Counted from the BEFORE capture, because those are exactly the lines this comparison
# declines to attribute to import or launch.
held_out=0
[[ -n "${tree_content_before}" ]] && held_out="$(grep -c '' <<<"${tree_content_before}")"

report_mutation_leg \
  "tracked-content status of ${MUTATION_SCOPE}" \
  "${tree_content_before}" "${mutated}" \
  "${tree_content_before_status}" "${mutated_status}" \
  "content leg: game/ has no unexpected tracked-file change - git reports the same modified/staged/untracked set for ${MUTATION_SCOPE} after the import and launch as it did before the cold-cache removal, holding out ${held_out} change(s) that were already present when this gate started. This is a STATUS summary, not byte identity, and it is NOT evidence that nothing was written - see the mtime leg for that claim"

report_mutation_leg \
  "tracked-file metadata (mtime, inode, size) of ${MUTATION_SCOPE}" \
  "${tree_metadata_before}" "${mutated_metadata}" \
  "${tree_metadata_before_status}" "${mutated_metadata_status}" \
  "mtime leg: the mtime, inode and size of every TRACKED file in ${MUTATION_SCOPE} are unchanged across the import and launch, so no tracked file in scope was written - including the byte-identical rewrite of a deterministic import sidecar that the content leg cannot see. Covers tracked files only: an untracked file created and deleted inside the window is outside both legs. If this leg ever goes RED, note that in CI the tree is static - clean checkout, no other writer during the run - so a red there is a write by this gate or by the engine it ran; on a developer machine the same red may instead be a concurrent edit under game/ by a person or a sibling agent, which is a TRUTHFUL red about a real write even though this gate did not cause it. Either way the check to run is WHAT CHANGED, from the paths and mtimes the failure prints. Narrowing MUTATION_PATHSPEC is the WRONG fix"

section "negative controls: every predicate above can actually fail (Decision 11 rule 4)"
#
# This gate had no negative control at all, and its assertions are the kind that most
# needs one: three of the four read a Godot log for the presence or absence of a string,
# and Godot exits 0 even when the boot script fails to load, so "the log did not say
# ERROR" is exactly the shape that reads as success when nothing was read.
#
# The controls drive the identical predicates - the same `engine_problem_lines` function
# and the same startup-line read - over injected logs. They cannot drive a real
# misbehaving engine: making Godot fail on purpose means breaking the project or the
# binary, and doc 91 § Negative control adequacy rules that out ("a coherent violation,
# not a broken state"), because a red result would then be ambiguous between the gate
# catching it and the gate falling over. What is controlled here is therefore the log
# analysis, which is the part of this gate that could silently stop working; the engine's
# own exit status is read directly from PIPESTATUS above and is not a predicate.
#
# Each control runs twice: once at fixture size and once at ~300 KB with the marker late
# in the stream. A real headless Godot launch log is tens of kilobytes, and the read at
# line 95 used to be `printf '%s\n' "${launch_log}" | grep -qF ...`, which under
# `set -o pipefail` reports 141 - "startup line not found" - once the log is large enough
# that printf has a write left to do when grep exits. So a one-line fixture would be a
# control that structurally cannot fail here, and the production-sized case is the point.
readonly GODOT_LOG_FILLER='  --- Debug adapter server started on port 6006 ---   at Godot.NativeInterop.NativeFuncs.godotsharp_internal_object_disposed'

# "<label>|<startup line: yes|no>|<engine problem: yes|no>"
readonly GODOT_LOG_CONTROLS=(
  "a healthy launch log|yes|no"
  "a log with no startup line (the boot scene never reached managed code)|no|no"
  "a log carrying an engine ERROR line (Godot still exits 0)|yes|yes"
)

for control in "${GODOT_LOG_CONTROLS[@]}"; do
  IFS='|' read -r label has_startup has_problem <<<"${control}"
  for target_bytes in 0 300000; do
    synthetic="Godot Engine v4.7.1.stable.mono.official - https://godotengine.org"
    while [[ "${#synthetic}" -lt "${target_bytes}" ]]; do
      synthetic+=$'\n'"${GODOT_LOG_FILLER}"
    done
    # Markers last, so grep must read the whole log to answer.
    [[ "${has_problem}" == "yes" ]] \
      && synthetic+=$'\n''ERROR: Cannot instantiate C# script at res://scenes/boot.tscn::Boot'
    [[ "${has_startup}" == "yes" ]] && synthetic+=$'\n'"${STARTUP_LINE}"

    control_problems=()
    if grep -qF "${STARTUP_LINE}" <<<"${synthetic}"; then
      [[ "${has_startup}" == "yes" ]] || control_problems+=("the startup-line read found a line that is not there")
    else
      [[ "${has_startup}" == "no" ]] || control_problems+=("the startup-line read missed a line that IS there")
    fi

    engine_problems="$(engine_problem_lines <<<"${synthetic}")"
    if [[ -n "${engine_problems}" ]]; then
      [[ "${has_problem}" == "yes" ]] || control_problems+=("engine_problem_lines reported a problem in a clean log")
    else
      [[ "${has_problem}" == "no" ]] || control_problems+=("engine_problem_lines missed a real ERROR line")
    fi

    if [[ "${#control_problems[@]}" -eq 0 ]]; then
      control_pass "control (${#synthetic} bytes of log): ${label} is read correctly"
    else
      control_fail "control (${#synthetic} bytes of log): ${label}: $(printf '%s; ' "${control_problems[@]}")"
    fi
  done
done

# The mutation predicate's own control: with git unable to answer, the gate must report
# that it could not tell and must never report an unmutated tree. This is the same
# coherent-violation route verify-format.sh § 6 and verify-architecture.sh § 7a use.
control_mutated_status=0
control_mutated="$(cd "${REPO_ROOT}" \
  && GIT_DIR=/nonexistent/verify-godot-broken.git git status --porcelain -- game 2>&1)" \
  || control_mutated_status=$?
if [[ "${control_mutated_status}" -ne 0 ]]; then
  control_pass "control: with an unreadable git the mutation probe fails rather than reporting a clean game/ (exit ${control_mutated_status})"
else
  control_fail "control: with an unreadable git the mutation probe exited 0; a failed enumeration must not read as an unmutated tree"
fi

# --- the no-mutation predicate itself, over INJECTED STRINGS ------------------------------
# THE CONTROL THE SECTION ABOVE NEVER HAD. The unreadable-git control immediately above
# drives only the read; the comparison that actually decides the verdict was never observed
# going red in either direction. These drive classify_tree_mutation, the SAME function both
# legs above just called, so the control is OF the predicate rather than adjacent to it.
#
# The three cases are the three answers the predicate can give:
#   (a) two captures that DIFFER must be red - the mutation it exists to catch;
#   (b) two EQUAL NON-EMPTY captures must PASS - a dirty-but-unchanged tree is not a failure,
#       and this is the repaired symptom in predicate form: a developer's uncommitted edit
#       appears at BOTH ends and must not red;
#   (c) a nonzero status at EITHER end must be red as UNPROVED RATHER THAN TRUE, because two
#       failed reads compare equal and would otherwise manufacture the green.
#
# The VERDICT TEXT is asserted and not only the count, for (c) especially: an unreadable read
# and a genuine mutation are different findings, and a reader handed the wrong one goes
# looking for a mutation that never happened.
# "<label>|<before>|<after>|<before status>|<after status>|<expected problems>|<required phrase>"
readonly MUTATION_CONTROLS=(
  "two captures that differ, which must be red| M game/project.godot| M game/project.godot?? game/scenes/written-by-import.tscn|0|0|1|changed across the window"
  "two equal NON-EMPTY captures, which must PASS: a developer's uncommitted edit under game/ is present at both ends and is not a mutation by this gate| M game/project.godot| M game/project.godot|0|0|0|"
  "a nonzero status at both ends, which must be red as unproved rather than absent|||3|3|1|unproved rather than absent"
)

for control in "${MUTATION_CONTROLS[@]}"; do
  IFS='|' read -r label before after before_status after_status want phrase <<<"${control}"
  report=""
  got=0
  report="$(classify_tree_mutation "injected capture under control" \
    "${before}" "${after}" "${before_status}" "${after_status}")" || got=$?
  mutation_problems=()
  [[ "${got}" -eq "${want}" ]] \
    || mutation_problems+=("the predicate reported ${got} problem(s) where ${want} was designed")
  if [[ -n "${phrase}" ]] && ! grep -qF -- "${phrase}" <<<"${report}"; then
    mutation_problems+=("the verdict does not say '${phrase}', so the reader cannot tell which of the predicate's findings this is")
  fi
  if [[ "${#mutation_problems[@]}" -eq 0 ]]; then
    control_pass "control: ${label} -> ${got} problem(s), as designed"
  else
    control_fail "control: ${label}: $(printf '%s; ' "${mutation_problems[@]}")"
  fi
done

# --- the same predicate over a REAL repository difference ----------------------------------
# The injected strings prove the predicate reads what it is handed. What they cannot prove is
# that THE CAPTURES MOVE when a repository really changes - an injected string is a claim
# about the comparison, not about capture_tree_content and capture_tree_metadata. So the same
# two captures and the same predicate run over a THROWAWAY GIT REPOSITORY under a mktemp root,
# created for this control and deleted with it.
#
# THIS IS WHY THE CAPTURES TAKE A ROOT. A control that wrote under game/ would trip the real
# assertion above - it would be a gate dirtying the tree it checks - so nothing in
# ${REPO_ROOT} is touched. A throwaway repo gets a genuine `git status` difference and a
# genuine inode change with the real tree untouched, so the assertion the gate makes and the
# evidence the gate offers do not have to trade off. Nothing is committed: the index alone is
# enough for `git status --porcelain` to have an opinion and for `git ls-files` to name the
# file, which keeps the control fast and needs no committer identity.
#
# The three cases MEASURE THE TWO LEGS' DIFFERENT REACH rather than asserting it in a comment,
# which is the substance of the content-only defect this repairs:
#   (a) a new file appears              -> the content leg is RED, as any diff-based check is;
#   (b) a byte-identical rewrite        -> the content leg is SILENT, and that is not a bug;
#   (c) that SAME byte-identical rewrite -> the mtime leg is RED, which is why leg 2 exists.
control_root="$(mktemp -d "${TMPDIR:-/tmp}/verify-godot-controls.XXXXXX")"
trap 'rm -rf -- "${control_root}"' EXIT

mutation_repo="${control_root}/mutation-repo"
mutation_repo_ready="yes"
mkdir -p -- "${mutation_repo}/game" 2>/dev/null || mutation_repo_ready="no"
git init -q -- "${mutation_repo}" >/dev/null 2>&1 || mutation_repo_ready="no"
readonly MUTATION_FIXTURE_BYTES='a tracked file created by a negative control in a throwaway repository'
printf '%s\n' "${MUTATION_FIXTURE_BYTES}" >"${mutation_repo}/game/tracked.txt" 2>/dev/null \
  || mutation_repo_ready="no"
(cd -- "${mutation_repo}" && git add -- game/tracked.txt) >/dev/null 2>&1 \
  || mutation_repo_ready="no"

# $1 label, $2 expected problem count, $3 observed count, $4 report text.
expect_control_red() {
  local label="$1" want="$2" got="$3" report="$4"
  if [[ "${got}" -eq "${want}" ]]; then
    control_pass "control: ${label} -> ${got} problem(s), as designed"
  else
    control_fail "control: ${label} produced ${got} problem(s) where ${want} was designed, so the check it exercises does not read what this gate believes it reads"
  fi
}

if [[ "${mutation_repo_ready}" != "yes" ]]; then
  # Not silently skipped: three controls did not run, and a green must not imply they passed.
  for missing in "a new file is visible to the content leg" \
    "a byte-identical rewrite is invisible to the content leg" \
    "a byte-identical rewrite IS visible to the mtime leg"; do
    control_fail "control: ${missing}: the throwaway git repository could not be created, so the predicate was never driven over a real repository difference and this control is unproved rather than passing"
  done
else
  # (a) a real new file: the content leg must see it.
  real_content_before=""
  real_content_before_status=0
  real_content_before="$(capture_tree_content "${mutation_repo}" game)" \
    || real_content_before_status=$?
  printf 'written by a negative control\n' >"${mutation_repo}/game/written-by-a-control.txt"
  real_content_after=""
  real_content_after_status=0
  real_content_after="$(capture_tree_content "${mutation_repo}" game)" \
    || real_content_after_status=$?
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-content status of the throwaway repository" \
    "${real_content_before}" "${real_content_after}" \
    "${real_content_before_status}" "${real_content_after_status}")" || got=$?
  expect_control_red "a REAL new file in a throwaway repo turns the content leg red" 1 "${got}" "${report}"
  rm -f -- "${mutation_repo}/game/written-by-a-control.txt"

  # (b) and (c) the byte-identical rewrite, through a temp file and a rename so the change is
  # guaranteed rather than clock-dependent: %Y has one-second granularity and a fast rewrite
  # could land in the same second, but the rename always yields a different %i. The temp file
  # is created inside the window and removed by the rename, so this fixture is a
  # write-then-cleanup as well as a byte-identical rewrite - both of the writes the content
  # leg cannot see, in one control.
  rewrite_content_before=""
  rewrite_content_before_status=0
  rewrite_content_before="$(capture_tree_content "${mutation_repo}" game)" \
    || rewrite_content_before_status=$?
  rewrite_metadata_before=""
  rewrite_metadata_before_status=0
  rewrite_metadata_before="$(capture_tree_metadata "${mutation_repo}" game)" \
    || rewrite_metadata_before_status=$?

  printf '%s\n' "${MUTATION_FIXTURE_BYTES}" >"${mutation_repo}/game/.rewrite.tmp"
  mv -f -- "${mutation_repo}/game/.rewrite.tmp" "${mutation_repo}/game/tracked.txt"

  rewrite_content_after=""
  rewrite_content_after_status=0
  rewrite_content_after="$(capture_tree_content "${mutation_repo}" game)" \
    || rewrite_content_after_status=$?
  rewrite_metadata_after=""
  rewrite_metadata_after_status=0
  rewrite_metadata_after="$(capture_tree_metadata "${mutation_repo}" game)" \
    || rewrite_metadata_after_status=$?

  # (b) EXPECTED TO PASS WITH ZERO PROBLEMS, and that zero IS the finding. It is the measured
  # form of "git status --porcelain is a content-derived summary": the file was rewritten and a
  # temp file came and went, and the content leg reports nothing. A gate carrying only this leg
  # would call that tree unwritten - which is what this gate did before the mtime leg existed.
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-content status of the throwaway repository" \
    "${rewrite_content_before}" "${rewrite_content_after}" \
    "${rewrite_content_before_status}" "${rewrite_content_after_status}")" || got=$?
  expect_control_red "a REAL byte-identical rewrite is INVISIBLE to the content leg, which is why a second leg exists" \
    0 "${got}" "${report}"

  # (c) the same rewrite, read by the mtime leg, which must be red.
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-file metadata of the throwaway repository" \
    "${rewrite_metadata_before}" "${rewrite_metadata_after}" \
    "${rewrite_metadata_before_status}" "${rewrite_metadata_after_status}")" || got=$?
  expect_control_red "the SAME byte-identical rewrite IS visible to the mtime leg (mtime, inode, size)" \
    1 "${got}" "${report}"
fi

rm -rf -- "${control_root}"
trap - EXIT

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-godot" "${EXIT_VALIDATION}"
