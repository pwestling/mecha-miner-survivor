#!/usr/bin/env bash
#
# Proves the Godot integration runner's process exit and report contract from outside
# the test host: pass, fail, timeout, and artifact, driven directly against the pinned
# engine.
#
# Authority: docs/technical/91-verification-strategy.md § Test project separation
#            docs/technical/110-implementation-plan-for-ai-agents.md
#              § Concrete M0 bootstrap queue - TASK-FND-003-002 close evidence is
#              "headless pass/fail/timeout/artifact fixtures"
# Requirements: TR-QUA-001, TR-QUA-003, TR-FND-001
# Verification: VER-FND-003-008 (fail), VER-FND-003-009 (timeout), and independent
#               corroboration of VER-FND-003-007 and VER-FND-003-010
#
# Why this exists alongside the NUnit fixture: a host test cannot prove that a broken
# engine case fails without failing its own suite, so the NUnit fixture asserts the
# failure as an expectation. This script asserts it the other way round - it requires a
# nonzero exit and a "failed" report - so neither proof depends on the other.
#
# The central claim being defended: a headless Godot launch exits 0 even when the C#
# script on a node fails to load, so the exit code alone is not a gate. Every case here
# asserts the report, and the "no script" case below proves the trap is real by showing
# a broken scene exiting 0 with no report.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure, 5 build/import failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly GAME_DIR="${REPO_ROOT}/game"
readonly RUNNER_SCENE="res://tests/GodotTestRunner.tscn"
readonly EVIDENCE_DIR="${REPO_ROOT}/artifacts/engine-tier/verify-godot-runner"
readonly GODOT="${MECHAMINER_GODOT:-godot}"
readonly HANG_TIMEOUT_SECONDS=15
readonly EXIT_VALIDATION=4
readonly EXIT_BUILD=5

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

report_field() {
  # $1 report path, $2 dotted field path
  python3 - "$1" "$2" <<'PY'
import json, sys
document = json.load(open(sys.argv[1]))
value = document
for part in sys.argv[2].split('.'):
    value = value[part]
sys.stdout.write(json.dumps(value))
PY
}

mkdir -p "${EVIDENCE_DIR}"

section "0. build and import so the runner scene is loadable"
if ! dotnet build "${GAME_DIR}/MechaMiner.Game.csproj" --nologo -v quiet; then
  fail "MechaMiner.Game must build before Godot can load the runner"
  exit "${EXIT_BUILD}"
fi
pass "MechaMiner.Game built"

if ! "${GODOT}" --headless --path "${GAME_DIR}" --import >"${EVIDENCE_DIR}/import.log" 2>&1; then
  fail "headless import failed; see artifacts/engine-tier/verify-godot-runner/import.log"
  exit "${EXIT_BUILD}"
fi
pass "headless import completed"

run_case() {
  # $1 case name, $2 timeout seconds. Prints "<exit code>|<report path>|<log path>".
  local case_name="$1"
  local timeout_seconds="$2"
  local case_dir="${EVIDENCE_DIR}/${case_name}"
  rm -rf "${case_dir}"
  mkdir -p "${case_dir}/case-artifacts"
  local report="${case_dir}/report.json"
  local log="${case_dir}/engine.log"

  local status=0
  MECHAMINER_TEST_CASE="${case_name}" \
  MECHAMINER_TEST_REPORT="${report}" \
  MECHAMINER_TEST_ARTIFACTS="${case_dir}/case-artifacts" \
    timeout --kill-after=5s "${timeout_seconds}s" \
      "${GODOT}" --headless --path "${GAME_DIR}" "${RUNNER_SCENE}" --audio-driver Dummy \
      >"${log}" 2>&1 || status=$?

  printf '%s|%s|%s' "${status}" "${report}" "${log}"
}

section "1. the pass case reports passed (VER-FND-003-007)"
IFS='|' read -r status report log <<<"$(run_case pass 90)"
if [[ "${status}" -eq 0 && -f "${report}" ]] \
    && [[ "$(report_field "${report}" outcome)" == '"passed"' ]] \
    && [[ "$(report_field "${report}" case)" == '"pass"' ]] \
    && [[ "$(report_field "${report}" schema)" == '"MMG-RUNNER-REPORT"' ]]; then
  pass "exit ${status}, report outcome passed, engine $(report_field "${report}" engine.version)"
  printf '      %s\n' "$(report_field "${report}" engine.rendering_method) renderer, headless=$(report_field "${report}" engine.headless)"
else
  fail "pass case: exit ${status}, report $(cat "${report}" 2>/dev/null || echo missing)"
  tail -5 "${log}" | sed 's/^/      /'
fi

section "2. the fail case reports failed and exits nonzero (VER-FND-003-008)"
IFS='|' read -r status report log <<<"$(run_case fail 90)"
problems=()
[[ "${status}" -ne 0 ]] || problems+=("exit was 0; a failing case must exit nonzero")
[[ -f "${report}" ]] || problems+=("no report written")
if [[ -f "${report}" ]]; then
  [[ "$(report_field "${report}" outcome)" == '"failed"' ]] \
    || problems+=("report outcome is $(report_field "${report}" outcome), expected \"failed\"")
  [[ "$(report_field "${report}" requested_exit_code)" == "4" ]] \
    || problems+=("requested exit code is $(report_field "${report}" requested_exit_code), expected 4")
  python3 - "${report}" <<'PY' || problems+=("no failing assertion is named in the report")
import json, sys
document = json.load(open(sys.argv[1]))
failing = [a for a in document["assertions"] if not a["passed"]]
assert failing, "no failing assertion"
assert failing[0]["name"] == "deliberate-failure", failing[0]["name"]
PY
fi

if [[ "${#problems[@]}" -eq 0 ]]; then
  pass "exit ${status}, report outcome failed, failing assertion named 'deliberate-failure'"
  printf '      %s\n' "$(python3 -c "
import json,sys
d=json.load(open('${report}'))
print([a['detail'] for a in d['assertions'] if not a['passed']][0][:100])")"
else
  fail "fail case: $(printf '%s; ' "${problems[@]}")"
  tail -5 "${log}" | sed 's/^/      /'
fi

section "3. the hang case is terminated at its bound and leaves no report (VER-FND-003-009)"
start_seconds="${SECONDS}"
IFS='|' read -r status report log <<<"$(run_case hang "${HANG_TIMEOUT_SECONDS}")"
elapsed=$((SECONDS - start_seconds))
problems=()
# GNU timeout returns 124 when it terminates the child.
[[ "${status}" -eq 124 || "${status}" -eq 137 ]] \
  || problems+=("exit ${status}, expected 124 or 137 from a terminated process")
[[ ! -f "${report}" ]] || problems+=("a report exists; an atomically written report must be absent")
[[ "${elapsed}" -ge "${HANG_TIMEOUT_SECONDS}" ]] \
  || problems+=("only ${elapsed}s elapsed; the case exited before its bound")
[[ "${elapsed}" -lt $((HANG_TIMEOUT_SECONDS + 30)) ]] \
  || problems+=("${elapsed}s elapsed; the bound did not terminate the process")
grep -q 'will never quit' "${log}" || problems+=("the deliberate hang line was not printed")

if [[ "${#problems[@]}" -eq 0 ]]; then
  pass "terminated after ${elapsed}s with exit ${status}, no report written"
else
  fail "hang case: $(printf '%s; ' "${problems[@]}")"
  tail -5 "${log}" | sed 's/^/      /'
fi

section "4. the artifact case writes and references an artifact (VER-FND-003-010)"
IFS='|' read -r status report log <<<"$(run_case artifact 90)"
problems=()
[[ "${status}" -eq 0 ]] || problems+=("exit ${status}, expected 0")
[[ -f "${report}" ]] || problems+=("no report written")
if [[ -f "${report}" ]]; then
  referenced="$(python3 -c "
import json,sys
d=json.load(open('${report}'))
print(d['artifacts'][0] if d['artifacts'] else '')")"
  [[ -n "${referenced}" ]] || problems+=("the report references no artifact")
  [[ -f "${referenced}" ]] || problems+=("referenced artifact ${referenced} does not exist")
fi

if [[ "${#problems[@]}" -eq 0 ]]; then
  pass "exit ${status}, report references ${referenced#"${REPO_ROOT}/"}"
  sed 's/^/      /' "${referenced}"
else
  fail "artifact case: $(printf '%s; ' "${problems[@]}")"
  tail -5 "${log}" | sed 's/^/      /'
fi

section "5. the exit code alone is not a gate: a broken scene exits 0 with no report"
#
# This is the trap FND-001 recorded. A scene whose script cannot be instantiated logs
# an error and the engine still exits 0. If the runner were gated on the exit code, this
# case would pass.
readonly BROKEN_SCENE_SOURCE="${GAME_DIR}/tests/DeliberatelyBrokenScene.tscn"
cleanup_broken_scene() {
  rm -f "${BROKEN_SCENE_SOURCE}" "${BROKEN_SCENE_SOURCE}.uid"
}
trap cleanup_broken_scene EXIT

cat >"${BROKEN_SCENE_SOURCE}" <<'BROKEN'
[gd_scene load_steps=2 format=3]

; Written and removed by build/verify-godot-runner.sh. Its script path does not exist,
; so the C# script cannot be instantiated.

[ext_resource type="Script" path="res://tests/NoSuchRunnerScript.cs" id="1_missing"]

[node name="BrokenRunner" type="Node"]
script = ExtResource("1_missing")
BROKEN

broken_log="${EVIDENCE_DIR}/broken-scene.log"
broken_status=0
"${GODOT}" --headless --path "${GAME_DIR}" res://tests/DeliberatelyBrokenScene.tscn \
  --audio-driver Dummy --quit-after 30 >"${broken_log}" 2>&1 || broken_status=$?
cleanup_broken_scene

broken_report="${EVIDENCE_DIR}/broken-scene/report.json"
# The engine text quoted below is manufactured by this section's own broken fixture, so it
# goes through control_detail: a reader hunting a real "cannot instantiate" must not find
# this one first and stop. `head -3` here is a display truncation whose status is discarded
# and whose value is correct, so it stays a pipeline (Decision 13).
if [[ "${broken_status}" -eq 0 ]]; then
  control_pass "a broken scene exited 0, which is exactly why the report is the gate"
else
  control_pass "a broken scene exited ${broken_status} in this engine build; the report is still the gate"
fi
control_detail < <(grep -iE 'error|cannot|failed' "${broken_log}" | head -3)

if [[ ! -f "${broken_report}" ]]; then
  control_pass "no report was written, so a report-based gate rejects it regardless of exit code"
else
  control_fail "a broken scene somehow produced a report"
fi

# This gate runs a negative control in band (§ 5's deliberately broken scene), so its log
# contains engine error text on a green run. Prove the marking that separates it still holds.
gate_assert_marking

gate_summary "verify-godot-runner" "${EXIT_VALIDATION}"
