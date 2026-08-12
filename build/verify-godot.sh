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

# The documented discovery order, which build/toolchain.json states as "MECHAMINER_GODOT
# environment variable" then "godot on PATH". ToolchainInspector.ResolveGodotCommand
# implements it for doctor and build/verify-godot-runner.sh already spells it exactly this
# way; this script used to call bare `godot` twice and so honoured only the second half.
#
# That gap is platform-specific in the worst way. On macOS `godot` MUST NOT be on PATH:
# Godot resolves its GodotSharp assemblies relative to the path it was invoked by, so
# reaching it through a symlink such as /usr/local/bin/godot makes it search
# /usr/local/bin/GodotSharp/Api/Debug and die with "Unable to find .NET assemblies
# directories". The documented macOS setup therefore gives the full path inside the app
# bundle and deliberately leaves PATH alone - which left this script with nothing to find,
# and MECHAMINER_GODOT, the variable that exists precisely to say where the engine is,
# was the one thing it did not read.
readonly GODOT="${MECHAMINER_GODOT:-godot}"

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

# Resolves the discovery order to something runnable, or fails naming discovery. Printing
# "headless import exited 127" instead sends a reader into the engine log for a defect that
# is really "this machine never told the gate where Godot is" - the same shape as the
# swallowed `readlink -f` in build/verify-verbs.sh, one layer further out.
godot_discovery_problem() {
  local candidate="$1"
  if [[ -z "${candidate}" ]]; then
    printf '%s\n' "neither MECHAMINER_GODOT nor a godot on PATH names an engine"
    return 0
  fi
  if [[ "${candidate}" == */* ]]; then
    # An explicit path, from MECHAMINER_GODOT or a relative spelling.
    [[ -e "${candidate}" ]] || { printf '%s\n' "${candidate} does not exist"; return 0; }
    [[ -x "${candidate}" ]] || { printf '%s\n' "${candidate} is not executable"; return 0; }
    return 1
  fi
  command -v -- "${candidate}" >/dev/null 2>&1 \
    || { printf '%s\n' "no '${candidate}' on PATH"; return 0; }
  return 1
}

section "0. the documented Godot discovery order resolves to a runnable engine"
discovery_problem="$(godot_discovery_problem "${GODOT}" || true)"
if [[ -n "${discovery_problem}" ]]; then
  fail "Godot discovery failed: ${discovery_problem}"
  printf '%s\n' \
    "      Discovery order (build/toolchain.json): MECHAMINER_GODOT, then godot on PATH." \
    "      MECHAMINER_GODOT is currently ${MECHAMINER_GODOT:-unset}." \
    "      On macOS do NOT put godot on PATH - invoked through a symlink the engine looks" \
    "      for GodotSharp beside the symlink and fails with 'Unable to find .NET assemblies" \
    "      directories'. Point MECHAMINER_GODOT at the binary inside the app bundle:" \
    "        export MECHAMINER_GODOT=\"/Applications/Godot_mono.app/Contents/MacOS/Godot\"" \
    "      Then './build.sh doctor' confirms the engine before any gate runs." >&2
  gate_summary "verify-godot" "${EXIT_VALIDATION}"
  exit "${EXIT_VALIDATION}"
fi
pass "Godot discovery resolved '${GODOT}' before any engine command ran"

# Negative control for § 0. Without it the check above could never fail and would be
# decoration: a discovery probe that accepts everything reports success on the very machine
# it exists to diagnose. Each arm asserts the reported problem NAMES its cause, because a
# discovery failure that does not say which half of the order came up empty is the defect
# this section was added to remove.
control_absent="$(godot_discovery_problem "" || true)"
if [[ "${control_absent}" == *"neither MECHAMINER_GODOT nor a godot on PATH"* ]]; then
  control_pass "negative control: an empty discovery result is reported as a discovery failure naming both halves of the order"
else
  control_fail "negative control: an empty discovery result produced '${control_absent}', which does not name the discovery order; § 0 cannot be trusted to fire"
fi

control_missing_path="$(godot_discovery_problem "${REPO_ROOT}/build/verify-godot-no-such-engine" || true)"
if [[ "${control_missing_path}" == *"does not exist"* ]]; then
  control_pass "negative control: an explicit MECHAMINER_GODOT path that does not exist is reported as such, not as an engine crash"
else
  control_fail "negative control: a nonexistent explicit engine path produced '${control_missing_path}'; § 0 would let it through to the import step"
fi

control_not_executable="$(mktemp)"
control_not_executable_report="$(godot_discovery_problem "${control_not_executable}" || true)"
if [[ "${control_not_executable_report}" == *"is not executable"* ]]; then
  control_pass "negative control: an explicit engine path that is not executable is reported as such"
else
  control_fail "negative control: a non-executable engine path produced '${control_not_executable_report}'; § 0 would let it through to the import step"
fi
rm -f -- "${control_not_executable}"

section "cold cache: removing game/.godot"
rm -rf "${GAME_DIR}/.godot"

section "restore and build the game assembly (its obj/bin live inside .godot)"
if ! dotnet build "${GAME_DIR}/MechaMiner.Game.csproj" --nologo -v q; then
  fail "the game project must build before Godot can load its assembly"
  exit "${EXIT_BUILD}"
fi
pass "MechaMiner.Game built"

section "VER-FND-001-012: godot headless import"
import_log="$("${GODOT}" --headless --path "${GAME_DIR}" --import 2>&1 | strip_ansi)"
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
launch_log="$("${GODOT}" --headless --path "${GAME_DIR}" --quit-after "${LAUNCH_FRAMES}" 2>&1 | strip_ansi)"
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
