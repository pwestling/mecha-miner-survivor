#!/usr/bin/env bash
#
# Proves the wrapper contract: every one of doc 100's eighteen verbs is registered,
# unimplemented verbs return a typed nonzero status naming their owning work package,
# invalid invocations exit 2 with usage, and a deliberately broken environment exits 3.
#
# It does NOT prove that the implemented verbs do their work. Of the eight implemented
# verbs, only two are ever executed here: doctor (§ 5 via the matrix, and § 6, § 6a,
# § 7, § 9, § 10) and build (§ 8, on its failure path only - nothing here asserts a
# successful build). The other six - bootstrap, format, format-check, godot-import,
# test-fast, test-main - are in SLOW_IMPLEMENTED below and are asserted for registration
# and classification only. Where each is actually driven: format and format-check by
# build/verify-format.sh, test-fast by build/verify-test-harness.sh, build's success path
# by build/verify-configurations.sh § 3. Three verbs are driven by no gate script at all -
# bootstrap, godot-import, and test-main - and their registry entries (VER-FND-002-005,
# VER-FND-002-014, VER-FND-003-012) carry a bare command selector rather than a script.
# Reading this script as the verb suite is the mistake worth naming here, because the
# matrix lists all eighteen and looks like one.
#
# Exit class 3 has two halves, and both are asserted separately, because asserting
# only one is how a misclassification survived. § 6 covers the *absent* half (a pinned
# tool that is not there) and § 10 covers the *mismatched* half (a pinned tool that is
# there in the wrong version). Doc 100 defines the class as "missing or mismatched
# pinned environment", so neither half is optional.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface
# Requirements: TR-BLD-005, TR-BLD-001, TR-BLD-002
# Verification: VER-FND-002-002, VER-FND-002-003, VER-FND-002-004,
#               VER-FND-002-006, VER-FND-002-007, VER-FND-002-009, VER-FND-002-013,
#               VER-FND-002-016, VER-FND-002-017
#
# Every environment assertion here carries a negative control that must be rejected by
# the same predicate. A gate exercised only against a good environment records an
# opinion, not a result.
#
# This script deliberately does NOT run implemented verbs that do slow real work as
# part of the matrix; each has its own gate and its own verification entry. What the
# matrix proves is registration, argument validation, and exit classification for all
# eighteen verbs.
#
# The matrix rows record the current implemented/awaiting-owner state and are updated
# by the task that implements each verb. At TASK-FND-003-002 the implemented verbs are
# doctor, bootstrap, format, format-check, build, godot-import, test-fast, and
# test-main. The ten remaining verbs name the work package that owns each.
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
  "format|0|MMT-0000|"
  "format-check|0|MMT-0000|"
  "build|0|MMT-0000|"
  "test-fast|0|MMT-0000|"
  "test-main|0|MMT-0000|"
  "test-nightly|2|MMT-2002|OPS-001"
  "content|2|MMT-2002|DAT-006"
  "godot-import|0|MMT-0000|"
  "run|2|MMT-2002|FND-006"
  "scenario M2-ARENA|2|MMT-2002|SIM-009"
  "map --seed 0|2|MMT-2002|MAP-009"
  "map-batch nightly-partition-1|2|MMT-2002|MAP-010"
  "benchmark WB-01|2|MMT-2002|QUA-005"
  "export linux release|2|MMT-2002|FND-006"
  "package-demo|2|MMT-2002|OPS-002"
  "release-validate|2|MMT-2002|OPS-002"
)

# Implemented verbs are slow and each owns its own gate, so the matrix asserts their
# registration and skips executing them here.
readonly SLOW_IMPLEMENTED=(
  "bootstrap"
  "format"
  "format-check"
  "build"
  "godot-import"
  "test-fast"
  "test-main"
)

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

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

section "1. the registered verb set is exactly doc 100's eighteen verbs (VER-FND-002-006)"
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

section "2. an empty invocation prints usage and exits 2 (VER-FND-002-004)"
output="$("${WRAPPER}" 2>&1)"
status=$?
if [[ "${status}" -eq 2 ]] && grep -q '^VERB TABLE' <<<"${output}"; then
  pass "no verb: exit 2 with the usage table"
else
  fail "no verb: exit ${status} (expected 2) and/or no usage table"
fi

section "3. an unknown verb prints usage and exits 2 (VER-FND-002-003)"
output="$("${WRAPPER}" definitely-not-a-verb 2>&1)"
status=$?
if [[ "${status}" -eq 2 ]] \
    && grep -q '^VERB TABLE' <<<"${output}" \
    && grep -q 'MMT-2001' <<<"${output}"; then
  pass "unknown verb: exit 2, usage table, diagnostic MMT-2001"
else
  fail "unknown verb: exit ${status} (expected 2) with MMT-2001 and usage"
  printf '%s\n' "${output}" | tail -3 | sed 's/^/      /'
fi

section "4. invalid arguments print usage and exit 2 (VER-FND-002-004)"
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
  if [[ "${status}" -eq 2 ]] && grep -q 'MMT-2003' <<<"${output}"; then
    pass "'${invocation}': exit 2 with MMT-2003"
  else
    fail "'${invocation}': exit ${status} (expected 2 with MMT-2003)"
    printf '%s\n' "${output}" | tail -2 | sed 's/^/      /'
  fi
done

section "5. every verb's classification (VER-FND-002-007, VER-FND-002-009)"
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
  grep -q "\[${expected_code}\]" <<<"${output}" \
    || problems+=("diagnostic code ${expected_code} not printed")
  if [[ -n "${expected_owner}" ]]; then
    grep -q "${expected_owner}" <<<"${output}" \
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

section "6. a deliberately broken environment exits 3 (VER-FND-002-002)"
output="$(MECHAMINER_GODOT=/nonexistent/godot "${WRAPPER}" doctor 2>&1)"
status=$?
if [[ "${status}" -eq 3 ]] \
    && grep -q 'MMT-3001' <<<"${output}" \
    && grep -q 'MISMATCH.*godot editor' <<<"${output}"; then
  pass "MECHAMINER_GODOT pointing at a nonexistent editor: exit 3 with MMT-3001"
else
  fail "broken environment: exit ${status} (expected 3 with MMT-3001)"
  printf '%s\n' "${output}" | tail -4 | sed 's/^/      /'
fi

section "6a. a SUBSTITUTED godot binary is detected by hash (VER-FND-002-017)"
#
# § 6 only points MECHAMINER_GODOT at a path that does not exist, where the version
# probe fails first and the hash probe is never reached on its interesting path. That
# is why the following survived: the hash probe resolved the pinned install path in
# preference to the command it was given, so a substituted binary produced
# "resolved as '<substitute>'" and "sha256 of /opt/godot/... matches the pin" on
# adjacent report lines, and doctor exited 0 with "0 mismatches" - a pin match reported
# for a file that was never opened.
#
# The fixture is a two-line shell script that prints a version string matching the
# pinned prefix, so the version probe passes and the hash probe is the only thing that
# can catch it. That is the point: this asserts the hash probe specifically.
#
readonly GODOT_FIXTURE_DIR="$(mktemp -d)"
readonly GODOT_SUBSTITUTE="${GODOT_FIXTURE_DIR}/godot"
readonly GODOT_SAME_CONTENT_LINK="${GODOT_FIXTURE_DIR}/godot-via-other-path"

remove_godot_fixtures() {
  rm -rf "${GODOT_FIXTURE_DIR}"
}

cleanup_fixtures_and_godot() {
  cleanup_fixtures
  remove_godot_fixtures
}
trap cleanup_fixtures_and_godot EXIT

pinned_version_prefix="$(python3 -c '
import json, sys
print(json.load(open(sys.argv[1]))["godot"]["expected_version_prefix"])
' "${REPO_ROOT}/build/toolchain.json")"

cat >"${GODOT_SUBSTITUTE}" <<SUBSTITUTE
#!/usr/bin/env bash
# Deliberately substituted godot, written by build/verify-verbs.sh. Prints a version
# string the version probe accepts, so only the hash probe can reject it.
echo "${pinned_version_prefix}.deadbeef"
SUBSTITUTE
chmod +x "${GODOT_SUBSTITUTE}"

substitute_sha="$(python3 -c '
import hashlib, sys
print(hashlib.sha256(open(sys.argv[1], "rb").read()).hexdigest())
' "${GODOT_SUBSTITUTE}")"
pinned_sha="$(python3 -c '
import json, sys
platform = json.load(open(sys.argv[1]))["godot"]["platforms"]
print(platform["linux-x64"]["executable_sha256"])
' "${REPO_ROOT}/build/toolchain.json")"

output="$(MECHAMINER_GODOT="${GODOT_SUBSTITUTE}" "${WRAPPER}" doctor 2>&1)"
status=$?

problems=()
[[ "${status}" -eq 3 ]] || problems+=("exit ${status}, expected 3")
grep -q 'MMT-3001' <<<"${output}" || problems+=("MMT-3001 not printed")
grep -qE 'MISMATCH.*godot executable hash' <<<"${output}" \
  || problems+=("the godot executable hash row is not MISMATCH")
# The decisive assertion: the report must show the SUBSTITUTE's hash as observed. The
# old defect showed the pinned hash as observed, which is how it read as a match.
grep -q "${substitute_sha}" <<<"${output}" \
  || problems+=("the report does not contain the substitute's own sha256 ${substitute_sha}")
grep -q "sha256 of ${GODOT_SUBSTITUTE}" <<<"${output}" \
  || problems+=("the report does not say it hashed ${GODOT_SUBSTITUTE}")

if [[ "${#problems[@]}" -eq 0 ]]; then
  pass "a substituted godot with an acceptable version string is rejected by hash: exit 3, MMT-3001, substitute's own sha256 reported"
else
  fail "substituted godot: $(printf '%s; ' "${problems[@]}")"
  printf '%s\n' "${output}" | grep -E 'godot' | sed 's/^/      /'
fi

# Negative control 1. Without this, § 6a could pass for the wrong reason: a probe that
# simply failed on any non-canonical path would satisfy every assertion above while
# still not hashing anything. A different path holding the SAME bytes must pass, which
# is only possible if the probe hashes what the command resolves to.
resolved_pinned="$(readlink -f "$(command -v godot 2>/dev/null || true)" 2>/dev/null || true)"
if [[ -n "${resolved_pinned}" && -f "${resolved_pinned}" ]]; then
  ln -s "${resolved_pinned}" "${GODOT_SAME_CONTENT_LINK}"
  output="$(MECHAMINER_GODOT="${GODOT_SAME_CONTENT_LINK}" "${WRAPPER}" doctor 2>&1)"
  status=$?
  if [[ "${status}" -eq 0 ]] && grep -q "${pinned_sha}" <<<"${output}"; then
    control_pass "negative control: a different path with the pinned bytes still passes, so § 6a rejected content and not merely an unusual path"
  else
    control_fail "negative control: the pinned binary reached through another path exited ${status} (expected 0); § 6a may be rejecting the path rather than the content"
    printf '%s\n' "${output}" | grep -E 'godot' | sed 's/^/      /'
  fi
else
  control_fail "negative control could not run: no godot on PATH to reach by a second path"
fi

# Negative control 2. The substitute and the pin must genuinely differ, or § 6a's
# central assertion would be trivially satisfiable.
if [[ "${substitute_sha}" != "${pinned_sha}" ]]; then
  control_pass "negative control: the substitute's sha256 differs from the pin, so the mismatch above was a real content difference"
else
  control_fail "negative control: the substitute hashes to the pinned value, which makes § 6a vacuous"
fi

remove_godot_fixtures
trap cleanup_fixtures EXIT

section "7. a correct environment exits 0 (VER-FND-002-001)"
output="$("${WRAPPER}" doctor 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]] && grep -q 'MMT-0000' <<<"${output}"; then
  pass "doctor: exit 0 with MMT-0000"
else
  fail "doctor: exit ${status} (expected 0)"
  printf '%s\n' "${output}" | tail -4 | sed 's/^/      /'
fi

section "8. build returns exit class 5 when solution compilation fails (VER-FND-002-013)"
#
# The fixture must break a project that is in MechaMiner.sln but is NOT the verb
# host. A broken verb host is a different, also-correct outcome: the launcher cannot
# reach any verb, so it returns 8. Both paths are asserted, in that order.
#
readonly SOLUTION_FIXTURE="${REPO_ROOT}/tests/MechaMiner.Game.Tests/DeliberatelyUncompilableFixture.cs"
readonly HOST_FIXTURE="${REPO_ROOT}/src/MechaMiner.Tools/DeliberatelyUncompilableFixture.cs"

cleanup_fixtures() {
  rm -f "${SOLUTION_FIXTURE}" "${HOST_FIXTURE}"
}
trap cleanup_fixtures EXIT

write_uncompilable() {
  cat >"$1" <<'BROKEN'
// Deliberately uncompilable fixture written and removed by build/verify-verbs.sh.
namespace MechaMiner.DeliberatelyUncompilable;

internal static class DeliberatelyUncompilableFixture
{
    internal static int Value()
    {
        return "not an int";
    }
}
BROKEN
}

write_uncompilable "${SOLUTION_FIXTURE}"
output="$("${WRAPPER}" build 2>&1)"
status=$?
rm -f "${SOLUTION_FIXTURE}"
if [[ "${status}" -eq 5 ]] && grep -q 'MMT-5001' <<<"${output}"; then
  pass "an uncompilable file in a solution project makes build exit 5 with MMT-5001"
else
  fail "uncompilable solution project: build exited ${status} (expected 5 with MMT-5001)"
  printf '%s\n' "${output}" | tail -6 | sed 's/^/      /'
fi

section "9. a broken verb host exits 8 rather than leaking the tool's own 1"
write_uncompilable "${HOST_FIXTURE}"
output="$("${WRAPPER}" doctor 2>&1)"
status=$?
rm -f "${HOST_FIXTURE}"
if [[ "${status}" -eq 8 ]] && grep -q 'MMT-8001' <<<"${output}"; then
  pass "a verb host that does not build makes the wrapper exit 8 with MMT-8001"
else
  fail "broken verb host: wrapper exited ${status} (expected 8 with MMT-8001)"
  printf '%s\n' "${output}" | tail -6 | sed 's/^/      /'
fi

if "${WRAPPER}" doctor >/dev/null 2>&1; then
  control_pass "the tree builds again after both fixtures were removed"
else
  control_fail "the repository did not return to a buildable state after the fixtures"
fi

section "10. a global.json pinning an uninstalled SDK exits 3, not 8 (VER-FND-002-016)"
#
# Section 6 above only ever proved the *absent* half of exit class 3: it points
# MECHAMINER_GODOT at a nonexistent editor. The mismatched half was unproved, and the
# wrapper got it wrong - it gated class 3 on `command -v dotnet` alone, so a
# global.json pinning an installed-but-absent SDK version fell through to the verb
# host's build failure and was reported as class 8 / MMT-8001 "unexpected
# tool-internal failure". Doc 100 § Standard command surface defines class 3 as a
# "missing or mismatched pinned environment", so a version mismatch is class 3 and
# blaming an internal tool bug is wrong.
#
# 9.9.999 is used because no such .NET SDK exists or will exist, so the fixture cannot
# accidentally resolve on a machine with more SDKs installed than this one.
#
readonly PINNED_SDK_FILE="${REPO_ROOT}/global.json"
readonly PINNED_SDK_BACKUP="${REPO_ROOT}/artifacts/wrapper/global.json.verify-verbs-backup"
readonly UNINSTALLABLE_SDK_VERSION="9.9.999"
readonly NEGATIVE_CONTROL_WRAPPER="${REPO_ROOT}/DeliberatelyUnprobedWrapperFixture.sh"

restore_pinned_sdk() {
  # Restoring the repository's real pin matters more than any assertion below, so it
  # is unconditional and idempotent.
  if [[ -f "${PINNED_SDK_BACKUP}" ]]; then
    cp -f "${PINNED_SDK_BACKUP}" "${PINNED_SDK_FILE}"
    rm -f "${PINNED_SDK_BACKUP}"
  fi
  rm -f "${NEGATIVE_CONTROL_WRAPPER}"
}

cleanup_fixtures_and_pin() {
  cleanup_fixtures
  restore_pinned_sdk
}
trap cleanup_fixtures_and_pin EXIT

mkdir -p "${REPO_ROOT}/artifacts/wrapper"
cp -f "${PINNED_SDK_FILE}" "${PINNED_SDK_BACKUP}"

# The negative control is the pre-fix wrapper: build.sh with its pinned-SDK
# resolution probe deleted. It must misclassify, and the assertion below must reject
# it. Without this control, section 10 would still pass against a wrapper that had
# never gained the probe, because a mismatched pin is nonzero either way.
if ! sed '/^# MISMATCH-PROBE-BEGIN$/,/^# MISMATCH-PROBE-END$/d' "${WRAPPER}" \
      >"${NEGATIVE_CONTROL_WRAPPER}"; then
  control_fail "could not write the negative-control wrapper"
fi
chmod +x "${NEGATIVE_CONTROL_WRAPPER}"

if ! grep -q '^# MISMATCH-PROBE-BEGIN$' "${WRAPPER}"; then
  control_fail "build.sh has no MISMATCH-PROBE markers, so the negative control below is vacuous"
elif grep -q 'MISMATCH-PROBE' "${NEGATIVE_CONTROL_WRAPPER}"; then
  control_fail "the negative-control wrapper still contains the probe it was supposed to lose"
else
  control_pass "negative control built: build.sh with the pinned-SDK resolution probe removed"
fi

python3 - "${PINNED_SDK_FILE}" "${UNINSTALLABLE_SDK_VERSION}" <<'PY'
import json, sys
path, version = sys.argv[1:3]
with open(path, encoding="utf-8") as handle:
    document = json.load(handle)
document["sdk"]["version"] = version
with open(path, "w", encoding="utf-8") as handle:
    json.dump(document, handle, indent=2)
    handle.write("\n")
PY

assert_mismatch_classification() {
  # "<wrapper path>|<human name>"
  local wrapper_path="$1"
  local wrapper_name="$2"
  local expect_correct="$3"
  local wrapper_output wrapper_status

  wrapper_output="$("${wrapper_path}" doctor 2>&1)"
  wrapper_status=$?

  local classified_correctly=0
  if [[ "${wrapper_status}" -eq 3 ]] \
      && grep -q 'MMT-3001' <<<"${wrapper_output}" \
      && grep -qi 'mismatch' <<<"${wrapper_output}"; then
    classified_correctly=1
  fi

  if [[ "${expect_correct}" -eq 1 ]]; then
    if [[ "${classified_correctly}" -eq 1 ]]; then
      pass "${wrapper_name}: a pin on SDK ${UNINSTALLABLE_SDK_VERSION} exits 3 with MMT-3001 and says mismatch"
    else
      fail "${wrapper_name}: a pin on SDK ${UNINSTALLABLE_SDK_VERSION} exited ${wrapper_status} (expected 3 with MMT-3001)"
      printf '%s\n' "${wrapper_output}" | tail -6 | sed 's/^/      /'
    fi
    return
  fi

  # Negative control: the misclassifying wrapper must be rejected by the very same
  # predicate, and must specifically be the class-8 answer this fix replaced.
  if [[ "${classified_correctly}" -eq 1 ]]; then
    fail "${wrapper_name}: the probe-less wrapper still classified correctly, so section 10 proves nothing"
  elif [[ "${wrapper_status}" -eq 8 ]] && grep -q 'MMT-8001' <<<"${wrapper_output}"; then
    pass "${wrapper_name}: rejected, and reproduces the original defect exactly (exit 8, MMT-8001)"
  else
    pass "${wrapper_name}: rejected by the same predicate (exit ${wrapper_status}, not 3/MMT-3001)"
  fi
}

assert_mismatch_classification "${NEGATIVE_CONTROL_WRAPPER}" "negative control (probe removed)" 0
assert_mismatch_classification "${WRAPPER}" "build.sh" 1

restore_pinned_sdk
trap cleanup_fixtures EXIT

if [[ "$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["sdk"]["version"])' "${PINNED_SDK_FILE}")" \
      != "${UNINSTALLABLE_SDK_VERSION}" ]] && "${WRAPPER}" doctor >/dev/null 2>&1; then
  pass "global.json was restored and doctor exits 0 again"
else
  fail "global.json was not restored to its real pin"
fi

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-verbs" "${EXIT_VALIDATION}"
