#!/usr/bin/env bash
#
# Proves the two determinism obligations doc 91 places on every randomized test, and
# proves that the pure test tier launches no Godot process.
#
# Authority: docs/technical/91-verification-strategy.md
#              § Determinism and fixture policy ("Every randomized test logs its seed
#              and version identity before execution"; "Failures print a
#              one-command/tool reproduction description and preserve the minimized
#              input where possible")
#              § Test project separation ("Pure simulation/content/persistence tests
#              do not launch Godot")
# Requirements: TR-QUA-001, TR-QUA-002, TR-BLD-005
# Verification: VER-FND-003-003, VER-FND-003-011
#
# The failure obligations cannot be proved by a passing test alone, so this script
# runs the Explicit SeedReproductionFixture on purpose, requires the run to fail, and
# then asserts the printed reproduction command and the preserved artifacts.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly SIMULATION_TESTS="${REPO_ROOT}/tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj"
readonly FAILURE_CASE="deliberate-harness-failure"
readonly DECLARED_SEED=77001
readonly FAILURE_DIR="${REPO_ROOT}/artifacts/test-failures/${FAILURE_CASE}/seed-${DECLARED_SEED}"
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

echo "=== 1. the pure tier runs, discovers a nonzero test count, and its tests launch no Godot"
#
# This section used to remove game/.godot first and then assert that it was still
# absent, as a corroborating signal for "the pure tier launched no Godot". That
# assertion is now false for a legitimate reason and has been removed rather than
# weakened: test-fast reaches build/verify-configurations.sh, which builds the Godot
# project in all three configurations, and a Godot.NET.Sdk build writes game/.godot.
# Observed on this tree - game/.godot exists after a green ./build.sh test-fast.
#
# The binding assertion was always the tripwire, and the tripwire's scope is the pure
# NUnit test processes, not everything the verb does; see GodotTripwire's remarks in
# src/MechaMiner.Tools/Verbs/TestVerb.cs. So this section requires the tripwire to have
# been reported armed and untripped, and claims nothing about the gate scripts.
output="$("${WRAPPER}" test-fast 2>&1)"
status=$?
printf '%s\n' "${output}" | sed 's/^/      /'

if [[ "${status}" -eq 0 ]]; then
  pass "test-fast exited 0"
else
  fail "test-fast exited ${status} (expected 0)"
fi

total="$(printf '%s' "${output}" | sed -n 's/.*test-fast: total \([0-9]*\),.*/\1/p' | tail -1)"
if [[ -n "${total}" && "${total}" -gt 0 ]]; then
  pass "the pure tier executed ${total} test(s)"
else
  fail "the pure tier reported no test count; 0 discovered tests is a harness failure"
fi

# The tripwire is the assertion, and it has to have been reported: a verb that dropped
# the stage entirely would otherwise look identical to one that armed it and saw nothing.
if printf '%s' "${output}" | grep -q 'ok    no-godot-launched: .*shim was first on PATH'; then
  pass "test-fast reported its no-godot-launched tripwire as armed and untripped"
else
  fail "test-fast did not report an armed no-godot-launched tripwire"
fi

if printf '%s' "${output}" | grep -q 'skipped 0'; then
  pass "no test was skipped (doc 91 § Flake policy: a skipped required test is a defect)"
else
  fail "the pure tier reported skipped tests"
fi

echo
echo "=== 2. a randomized failure prints a one-command reproduction and preserves the minimized input"
rm -rf "${FAILURE_DIR}"
run_output="$(dotnet test "${SIMULATION_TESTS}" \
  --nologo -v normal \
  --filter "TestCategory=HarnessFailureDemonstration" 2>&1)"
run_status=$?

if [[ "${run_status}" -ne 0 ]]; then
  pass "the deliberately failing fixture failed, exit ${run_status}"
else
  fail "the deliberately failing fixture passed; it must fail for this gate to mean anything"
fi

echo "      --- captured output of the deliberate failure ---"
printf '%s\n' "${run_output}" \
  | grep -E 'SEED |VERSION-IDENTITY |REPRODUCE |CASES |RANDOMIZED FAILURE|minimized|preserved:|Failed!|Error Message|property failed' \
  | sed 's/^/      /'

# Doc 91 requires the seed and the version identity to be logged BEFORE execution.
# The unbuffered progress stream is what makes that observable, so assert the order.
seed_line="$(printf '%s\n' "${run_output}" | grep -n "SEED case=${FAILURE_CASE} seed=${DECLARED_SEED}" | head -1 | cut -d: -f1)"
identity_line="$(printf '%s\n' "${run_output}" | grep -n 'VERSION-IDENTITY harness=' | head -1 | cut -d: -f1)"
failure_line="$(printf '%s\n' "${run_output}" | grep -n 'RANDOMIZED FAILURE' | head -1 | cut -d: -f1)"

if [[ -n "${seed_line}" ]]; then
  pass "the seed was logged: seed=${DECLARED_SEED}"
else
  fail "the seed was not logged"
fi

if [[ -n "${identity_line}" ]]; then
  pass "the version identity was logged"
else
  fail "the version identity was not logged"
fi

if [[ -n "${seed_line}" && -n "${identity_line}" && -n "${failure_line}" ]] \
    && [[ "${seed_line}" -lt "${failure_line}" ]] \
    && [[ "${identity_line}" -lt "${failure_line}" ]]; then
  pass "both were logged before the failure, so an aborted or hanging run still names its seed"
else
  fail "the seed and identity were not logged before execution"
fi

if printf '%s' "${run_output}" | grep -q "MECHAMINER_TEST_SEED=${DECLARED_SEED} dotnet test tests/MechaMiner.Simulation.Tests"; then
  pass "a one-command reproduction was printed"
else
  fail "no one-command reproduction was printed"
fi

for artifact in reproduction.txt minimized-input.txt; do
  if [[ -f "${FAILURE_DIR}/${artifact}" ]]; then
    pass "preserved artifacts/test-failures/${FAILURE_CASE}/seed-${DECLARED_SEED}/${artifact}"
  else
    fail "missing ${FAILURE_DIR}/${artifact}"
  fi
done

if [[ -f "${FAILURE_DIR}/minimized-input.txt" ]]; then
  echo "      --- preserved minimized input ---"
  sed 's/^/      /' "${FAILURE_DIR}/minimized-input.txt"

  original_length="$(sed -n 's/.*original input: *\[\(.*\)\]/\1/p' "${FAILURE_DIR}/minimized-input.txt" \
    | tr ',' '\n' | sed '/^$/d' | wc -l | tr -d ' ')"
  minimized_length="$(sed -n 's/.*minimized input: *\[\(.*\)\]/\1/p' "${FAILURE_DIR}/minimized-input.txt" \
    | tr ',' '\n' | sed '/^$/d' | wc -l | tr -d ' ')"
  if [[ -n "${original_length}" && -n "${minimized_length}" && "${minimized_length}" -lt "${original_length}" ]]; then
    pass "the preserved input was minimized: ${original_length} element(s) shrunk to ${minimized_length}"
  else
    fail "the preserved input was not minimized (original ${original_length:-?}, minimized ${minimized_length:-?})"
  fi
fi

echo
echo "=== 3. the reproduction command actually reproduces the failure at the same seed"
#
# Capture the first run's minimized input, then delete the artifact, then rerun with
# the printed override. Comparing a value captured before the rerun against one read
# after it is what makes this assertion real rather than a file compared with itself.
first_minimized="$(sed -n 's/.*minimized input: *//p' "${FAILURE_DIR}/minimized-input.txt" 2>/dev/null || true)"
rm -rf "${FAILURE_DIR}"

reproduced="$(MECHAMINER_TEST_SEED="${DECLARED_SEED}" dotnet test "${SIMULATION_TESTS}" \
  --nologo -v normal \
  --filter "TestCategory=HarnessFailureDemonstration" 2>&1)"
reproduced_status=$?
if [[ "${reproduced_status}" -ne 0 ]] \
    && printf '%s' "${reproduced}" | grep -q "seed=${DECLARED_SEED}"; then
  pass "rerunning with MECHAMINER_TEST_SEED=${DECLARED_SEED} failed again at the same seed"
else
  fail "the printed reproduction did not reproduce the failure (exit ${reproduced_status})"
fi

second_minimized="$(sed -n 's/.*minimized input: *//p' "${FAILURE_DIR}/minimized-input.txt" 2>/dev/null || true)"
if [[ -n "${first_minimized}" && -n "${second_minimized}" \
      && "${first_minimized}" == "${second_minimized}" ]]; then
  pass "two independent runs at seed ${DECLARED_SEED} shrink to the same minimized input: ${first_minimized}"
else
  fail "the minimized input is not stable across runs at the same seed: first '${first_minimized:-none}', second '${second_minimized:-none}'"
fi

echo
echo "=== 4. the Explicit fixture never runs in an ordinary suite"
output="$("${WRAPPER}" test-fast 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]]; then
  pass "test-fast still exits 0, so the deliberately failing fixture is not part of it"
else
  fail "test-fast exited ${status}; the Explicit fixture must not affect an ordinary run"
fi

echo
echo "=== 5. negative control: a pure test that launches Godot fails the tier"
#
# An assertion that cannot fail is not a gate. This writes a pure test that launches
# `godot` from PATH, ignores the result, and passes; the tier must still fail,
# reporting the recorded invocation. The tripwire's shim answers instead of the real
# editor, so the violation is caught rather than performed.
#
cleanup_tripwire_fixture() {
  rm -f "${TRIPWIRE_FIXTURE}"
}
readonly TRIPWIRE_FIXTURE="${REPO_ROOT}/tests/MechaMiner.Simulation.Tests/DeliberateGodotLaunchFixture.cs"
trap cleanup_tripwire_fixture EXIT

cat >"${TRIPWIRE_FIXTURE}" <<'LAUNCHES_GODOT'
// Deliberately violating fixture written and removed by build/verify-test-harness.sh.
using System.Diagnostics;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests;

/// <summary>Launches Godot from a pure test, which doc 91 forbids.</summary>
[TestFixture]
internal sealed class DeliberateGodotLaunchFixture
{
    /// <summary>Passes on purpose: the tier's tripwire, not this test, is the gate.</summary>
    [Test]
    public void LaunchingGodotFromThePureTierIsRejectedByTheTier()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "godot",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--version");

        using Process process = Process.Start(startInfo)!;
        process.WaitForExit();
        Assert.Pass("the launch happened; the tier's tripwire is the gate");
    }
}
LAUNCHES_GODOT

violation_output="$("${WRAPPER}" test-fast 2>&1)"
violation_status=$?
cleanup_tripwire_fixture

printf '%s\n' "${violation_output}" | grep -E 'no-godot-launched|FAILED|total ' | sed 's/^/      /'

if [[ "${violation_status}" -eq 4 ]]; then
  pass "the tier exited 4 even though every test passed, so the tripwire is the gate"
else
  fail "the tier exited ${violation_status}; a pure test that launches Godot must fail it with 4"
fi

if printf '%s' "${violation_output}" \
    | grep -q 'FAIL  no-godot-launched: a pure NUnit test process tried to launch Godot'; then
  pass "the tripwire named the violation and recorded the invocation"
else
  fail "the tripwire did not report the launch it was armed to catch"
fi

echo
echo "=== 6. the tree is clean again after the negative control"
if "${WRAPPER}" test-fast >/dev/null 2>&1; then
  pass "test-fast passes again with the violating fixture removed"
else
  fail "test-fast does not pass after the violating fixture was removed"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-test-harness: PASS"
  exit 0
fi
echo "verify-test-harness: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
