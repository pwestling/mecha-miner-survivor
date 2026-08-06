#!/usr/bin/env bash
#
# Proves the wrapper contract: every one of doc 100's eighteen verbs is registered,
# implemented verbs behave, unimplemented verbs return a typed nonzero status naming
# their owning work package, invalid invocations exit 2 with usage, and a
# deliberately broken environment exits 3.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface
# Requirements: TR-BLD-005, TR-BLD-001, TR-BLD-002
# Verification: VER-FND-002-002, VER-FND-002-003, VER-FND-002-004,
#               VER-FND-002-006, VER-FND-002-007, VER-FND-002-009
#
# This script deliberately does NOT run implemented verbs that do slow real work as
# part of the matrix; each has its own gate and its own verification entry. What the
# matrix proves is registration, argument validation, and exit classification for all
# eighteen verbs.
#
# At TASK-FND-002-001 the implemented verbs are doctor and bootstrap. format,
# format-check, build, and godot-import are registered with their final argument
# contracts and return the typed unavailable-owner status naming FND-002 until
# TASK-FND-002-002 lands them; test-fast and test-main name FND-003. The matrix rows
# below record that state and are updated by the task that implements each verb.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly EXIT_VALIDATION=4

# The eighteen verbs of doc 100 § Standard command surface, in that document's
# table order, each with the exit class and diagnostic code the wrapper must return
# for a syntactically valid invocation, plus the owning work package for the ones
# whose owner has not landed.
#
#   <verb and arguments>|<expected exit class>|<expected diagnostic code>|<expected owner text>
readonly VERB_MATRIX=(
  "doctor|0|MMT-0000|"
  "bootstrap|0|MMT-0000|"
  "format|2|MMT-2002|FND-002"
  "format-check|2|MMT-2002|FND-002"
  "build|2|MMT-2002|FND-002"
  "test-fast|2|MMT-2002|FND-003"
  "test-main|2|MMT-2002|FND-003"
  "test-nightly|2|MMT-2002|OPS-001"
  "content|2|MMT-2002|DAT-006"
  "godot-import|2|MMT-2002|FND-002"
  "run|2|MMT-2002|FND-006"
  "scenario M2-ARENA|2|MMT-2002|SIM-009"
  "map --seed 0|2|MMT-2002|MAP-009"
  "map-batch nightly-partition-1|2|MMT-2002|MAP-010"
  "benchmark WB-01|2|MMT-2002|QUA-005"
  "export linux release|2|MMT-2002|FND-006"
  "package-demo|2|MMT-2002|OPS-002"
  "release-validate|2|MMT-2002|OPS-002"
)

# Implemented verbs that do slow real work own their own gate, so the matrix asserts
# their registration and does not execute them here.
readonly SLOW_IMPLEMENTED=(
  "bootstrap"
)

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

is_slow_implemented() {
  local verb="$1"
  local candidate
  for candidate in "${SLOW_IMPLEMENTED[@]}"; do
    [[ "${candidate}" == "${verb}" ]] && return 0
  done
  return 1
}

usage_table() {
  # The usage table is printed on every invalid invocation.
  "${WRAPPER}" 2>&1 || true
}

echo "=== 1. the registered verb set is exactly doc 100's eighteen verbs (VER-FND-002-006)"
mapfile -t registered < <(usage_table \
  | sed -n '/^VERB TABLE/,/^$/p' \
  | sed -n 's/^  \([a-z-]*\).*/\1/p' \
  | sed '/^$/d')
expected_names="$(printf '%s\n' "${VERB_MATRIX[@]}" | cut -d'|' -f1 | awk '{print $1}' | sort)"
registered_names="$(printf '%s\n' "${registered[@]}" | sort)"
if [[ "${registered_names}" == "${expected_names}" ]]; then
  pass "18 verbs registered and no others: $(printf '%s\n' "${registered[@]}" | wc -l | tr -d ' ') entries"
else
  fail "registered verb set differs from doc 100's table"
  diff <(printf '%s\n' "${expected_names}") <(printf '%s\n' "${registered_names}") || true
fi

echo
echo "=== 2. an empty invocation prints usage and exits 2 (VER-FND-002-004)"
output="$("${WRAPPER}" 2>&1)"
status=$?
if [[ "${status}" -eq 2 ]] && printf '%s' "${output}" | grep -q '^VERB TABLE'; then
  pass "no verb: exit 2 with the usage table"
else
  fail "no verb: exit ${status} (expected 2) and/or no usage table"
fi

echo
echo "=== 3. an unknown verb prints usage and exits 2 (VER-FND-002-003)"
output="$("${WRAPPER}" definitely-not-a-verb 2>&1)"
status=$?
if [[ "${status}" -eq 2 ]] \
    && printf '%s' "${output}" | grep -q '^VERB TABLE' \
    && printf '%s' "${output}" | grep -q 'MMT-2001'; then
  pass "unknown verb: exit 2, usage table, diagnostic MMT-2001"
else
  fail "unknown verb: exit ${status} (expected 2) with MMT-2001 and usage"
  printf '%s\n' "${output}" | tail -3 | sed 's/^/      /'
fi

echo
echo "=== 4. invalid arguments print usage and exit 2 (VER-FND-002-004)"
declare -a INVALID_INVOCATIONS=(
  "build --configuration nope"
  "build --unknown-argument x"
  "scenario"
  "map"
  "map --seed"
  "export linux"
  "export solaris release"
  "doctor extra-positional"
)
for invocation in "${INVALID_INVOCATIONS[@]}"; do
  # shellcheck disable=SC2086
  output="$("${WRAPPER}" ${invocation} 2>&1)"
  status=$?
  if [[ "${status}" -eq 2 ]] && printf '%s' "${output}" | grep -q 'MMT-2003'; then
    pass "'${invocation}': exit 2 with MMT-2003"
  else
    fail "'${invocation}': exit ${status} (expected 2 with MMT-2003)"
    printf '%s\n' "${output}" | tail -2 | sed 's/^/      /'
  fi
done

echo
echo "=== 5. every verb's classification (VER-FND-002-007, VER-FND-002-009)"
for entry in "${VERB_MATRIX[@]}"; do
  IFS='|' read -r invocation expected_class expected_code expected_owner <<<"${entry}"
  verb="${invocation%% *}"

  if is_slow_implemented "${verb}"; then
    pass "${invocation}: registered as implemented; exercised by its own gate, not by this matrix"
    continue
  fi

  # shellcheck disable=SC2086
  output="$("${WRAPPER}" ${invocation} 2>&1)"
  status=$?

  problems=()
  [[ "${status}" -eq "${expected_class}" ]] || problems+=("exit ${status}, expected ${expected_class}")
  printf '%s' "${output}" | grep -q "\[${expected_code}\]" \
    || problems+=("diagnostic code ${expected_code} not printed")
  if [[ -n "${expected_owner}" ]]; then
    printf '%s' "${output}" | grep -q "${expected_owner}" \
      || problems+=("owning work package ${expected_owner} not named")
  fi

  # Every verb writes structured evidence beneath artifacts/ and prints its path.
  result_path="$(printf '%s\n' "${output}" | sed -n 's/^result: *//p' | tail -1)"
  if [[ -z "${result_path}" ]]; then
    problems+=("no result artifact path printed")
  elif [[ ! -f "${REPO_ROOT}/${result_path}" ]]; then
    problems+=("result artifact ${result_path} does not exist")
  else
    python3 - "${REPO_ROOT}/${result_path}" "${expected_class}" "${expected_code}" "${expected_owner}" <<'PY' \
      || problems+=("structured result document does not match the printed classification")
import json, sys
path, expected_class, expected_code, expected_owner = sys.argv[1:5]
document = json.load(open(path))
assert document["schema"] == "MMT-VERB-RESULT", document["schema"]
assert document["exit_class"] == int(expected_class), document["exit_class"]
assert document["diagnostic_code"] == expected_code, document["diagnostic_code"]
if expected_owner:
    assert document["owning_work_package"] == expected_owner, document["owning_work_package"]
PY
  fi

  if [[ "${#problems[@]}" -eq 0 ]]; then
    pass "${invocation}: exit ${expected_class}, ${expected_code}${expected_owner:+, owner ${expected_owner}}, structured result verified"
  else
    fail "${invocation}: $(printf '%s; ' "${problems[@]}")"
    printf '%s\n' "${output}" | tail -4 | sed 's/^/      /'
  fi
done

echo
echo "=== 6. a deliberately broken environment exits 3 (VER-FND-002-002)"
output="$(MECHAMINER_GODOT=/nonexistent/godot "${WRAPPER}" doctor 2>&1)"
status=$?
if [[ "${status}" -eq 3 ]] \
    && printf '%s' "${output}" | grep -q 'MMT-3001' \
    && printf '%s' "${output}" | grep -q 'MISMATCH.*godot editor'; then
  pass "MECHAMINER_GODOT pointing at a nonexistent editor: exit 3 with MMT-3001"
else
  fail "broken environment: exit ${status} (expected 3 with MMT-3001)"
  printf '%s\n' "${output}" | tail -4 | sed 's/^/      /'
fi

echo
echo "=== 7. a correct environment exits 0 (VER-FND-002-001)"
output="$("${WRAPPER}" doctor 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]] && printf '%s' "${output}" | grep -q 'MMT-0000'; then
  pass "doctor: exit 0 with MMT-0000"
else
  fail "doctor: exit ${status} (expected 0)"
  printf '%s\n' "${output}" | tail -4 | sed 's/^/      /'
fi

echo
echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-verbs: PASS"
  exit 0
fi
echo "verify-verbs: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
