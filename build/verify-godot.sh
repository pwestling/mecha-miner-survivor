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
# The empty result of a FAILED `git status` is indistinguishable from the empty result of
# a clean tree, so the exit status is checked before the output is interpreted. Suppressing
# it with `2>/dev/null || true` meant that under a broken or absent git this assertion
# passed without having compared anything.
mutated=""
mutated_status=0
mutated="$(cd "${REPO_ROOT}" && git status --porcelain -- game 2>&1)" || mutated_status=$?
if [[ "${mutated_status}" -ne 0 ]]; then
  fail "could not read git status for game/ (exit ${mutated_status}), so mutation is unproved rather than absent: ${mutated}"
elif [[ -z "${mutated}" ]]; then
  pass "game/ has no unexpected tracked-file change"
else
  fail "import or launch mutated tracked files"
  printf '%s\n' "${mutated}" | sed 's/^/      /'
fi

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

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-godot" "${EXIT_VALIDATION}"
