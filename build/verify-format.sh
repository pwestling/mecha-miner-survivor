#!/usr/bin/env bash
#
# Proves the format gate with a deliberately misformatted fixture: format-check
# fails and writes nothing, format repairs the tree, and format-check then passes.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface (format: "format owned text/code and fail
#              if the resulting tree still violates policy"; format-check: "validate
#              formatting without writes")
#            .editorconfig, which records that IDE0055 and IDE0005 stay at
#              suggestion severity at build time precisely so this verb is their
#              single owner
# Requirements: TR-BLD-001, TR-BLD-005
# Verification: VER-FND-002-010, VER-FND-002-011
#
# The fixture is transient: this script writes it, runs the gates, and removes it on
# every exit path, including failure. It is never committed. FND-001 established that
# deliberately invalid fixtures live under build/policy-fixtures/, outside
# MechaMiner.sln; this one cannot, because the gate under test formats the solution
# and would not see a project the solution excludes. It is therefore placed in a test
# project rather than in a shipping assembly, so no committed file inside
# MechaMiner.Tools can ever fight format, format-check, or warnings-as-errors.
#
# The fixture covers all three gates the verb owns in one file:
#   * C# whitespace (IDE0055): wrong indentation and stray spaces.
#   * unnecessary using (IDE0005): an import nothing needs.
#   * owned-text rules: a trailing-whitespace line and no final newline.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly FIXTURE="${REPO_ROOT}/tests/MechaMiner.Game.Tests/MisformattedFormatFixture.cs"
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

cleanup() {
  rm -f "${FIXTURE}"
}
trap cleanup EXIT

write_fixture() {
  # printf, not a heredoc: the fixture must end WITHOUT a final newline and must
  # carry literal trailing whitespace, and an editor-friendly heredoc would not
  # preserve either.
  printf '%s' 'using System;
using System.Text;

namespace MechaMiner.Game.Tests;

/// <summary>Deliberately misformatted fixture written by build/verify-format.sh.</summary>
internal static class MisformattedFormatFixture
{
        internal static int Value( )
    {
            return    1;
  }
}' >"${FIXTURE}"
}

echo "=== 1. a misformatted tracked file makes format-check fail (VER-FND-002-010)"
write_fixture
fixture_before="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"

output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 4 ]] && printf '%s' "${output}" | grep -q 'MMT-4001'; then
  pass "format-check exited 4 with MMT-4001"
else
  fail "format-check exited ${status} (expected 4 with MMT-4001)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

for expectation in \
    'C# whitespace' \
    'C# style diagnostics' \
    'owned-text rules'; do
  if printf '%s' "${output}" | grep -qF "${expectation}"; then
    pass "format-check named the failing gate: ${expectation}"
  else
    fail "format-check did not name the failing gate: ${expectation}"
  fi
done

echo
echo "=== 2. format-check wrote nothing (VER-FND-002-010)"
fixture_after_check="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"
if [[ "${fixture_before}" == "${fixture_after_check}" ]]; then
  pass "the fixture is byte-identical after format-check: sha256 ${fixture_after_check}"
else
  fail "format-check modified the fixture; it must validate without writes"
fi

echo
echo "=== 3. format repairs the tree (VER-FND-002-011)"
output="$("${WRAPPER}" format 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]]; then
  pass "format exited 0"
else
  fail "format exited ${status} (expected 0)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

fixture_after_format="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"
if [[ "${fixture_before}" != "${fixture_after_format}" ]]; then
  pass "format changed the fixture: sha256 ${fixture_before} -> ${fixture_after_format}"
else
  fail "format did not change the misformatted fixture"
fi

echo "      repaired fixture:"
sed -n '1,20p' "${FIXTURE}" | cat -A | sed 's/^/      /' | head -20

echo
echo "=== 4. format-check now passes (VER-FND-002-011)"
output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]] && printf '%s' "${output}" | grep -q 'MMT-0000'; then
  pass "format-check exited 0 with MMT-0000 after format"
else
  fail "format-check exited ${status} after format (expected 0)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

echo
echo "=== 5. the repository returns to a clean state"
cleanup
output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]]; then
  pass "format-check passes on the committed tree with the fixture removed"
else
  fail "format-check exited ${status} on the committed tree"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-format: PASS"
  exit 0
fi
echo "verify-format: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
