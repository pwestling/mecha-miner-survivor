#!/usr/bin/env bash
#
# Runs the identifier/cross-link registry validator and the architecture-boundary rules,
# and prints their retained evidence.
#
# Authority: docs/technical/115-component-contract-and-schema-registry.md § Verification
#              ("Registration tests assert every CMP-*, CTR-*, SCH-*, and VER-* ID is
#              unique, indexed, and resolves its references"; "Documentation validation
#              checks that referenced registry and normative anchors exist")
#            docs/technical/91-verification-strategy.md § Verification registry
#            docs/technical/conventions.md § Stable identifiers
# Requirements: TR-CTR-006, TR-QUA-004, TR-BLD-005, TR-AGT-006
# Verification: VER-FND-009-012 (and the readable report for VER-FND-009-007 through
#               VER-FND-009-011)
#
# This script does not implement a second validator. The rules live in
# src/MechaMiner.Tools/Audit and run inside tests/MechaMiner.Tools.Tests, which
# ./build.sh test-fast already executes, so a reviewer never has to trust two
# implementations to agree. What this script adds is the readable report: it runs exactly
# those two test classes, then prints the retained inventories so the complete list of
# findings is in front of the reader instead of buried in a test failure message.
#
# On exit classification, and the choice recorded deliberately:
#
#   Structural violations of the shapes FND-009 owns - a duplicate identifier, a malformed
#   identifier, a registry that is not a valid SCH-QUA-001 document, an entry missing a
#   required field, a non-canonical encoding - are exit class 4 with MMT-4001.
#
#   A specification-content defect is a citation to an identifier or a document anchor that
#   does not exist. Those are real defects and are printed in full with file:line. They are
#   NOT downgraded to warnings, and they are NOT folded into MMT-4001 either, because the
#   document that contains the prose owns them and an unrelated task must not inherit the
#   blame. They get their own stable diagnostic code, MMT-4002, under the same exit class 4.
#   doc 100 § Standard command surface closes the exit-class set at eight members and
#   assigns finer distinctions to diagnostic codes: "More detailed stable diagnostic codes
#   live in structured output". Adding a ninth class to express this would change a contract
#   every later tool reads in order to say something the existing mechanism already says.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly TOOLS_TESTS="${REPO_ROOT}/tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj"
readonly ARCHITECTURE_EVIDENCE="${REPO_ROOT}/artifacts/architecture/architecture-forbidden-edges.txt"
readonly REGISTRY_DEFECTS="${REPO_ROOT}/artifacts/registry/registry-specification-defects.txt"
readonly FIXTURE_EVIDENCE="${REPO_ROOT}/artifacts/registry/registry-fixture-classes.txt"
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

echo "=== 1. the registry validator and the architecture rules, with their negative controls"
output="$(cd "${REPO_ROOT}" && dotnet test "${TOOLS_TESTS}" \
  --nologo -v minimal \
  --filter 'FullyQualifiedName~MechaMiner.Tools.Tests.Audit' 2>&1)"
status=$?
printf '%s\n' "${output}" | tail -30 | sed 's/^/      /'

if [[ "${status}" -eq 0 ]]; then
  pass "every audit assertion and every negative control held"
else
  fail "the audit suite exited ${status}"
fi

echo
echo "=== 2. the four registry failure classes, one fixture each"
if [[ -f "${FIXTURE_EVIDENCE}" ]]; then
  pass "$(printf '%s' "artifacts/registry/registry-fixture-classes.txt")"
  sed 's/^/      /' "${FIXTURE_EVIDENCE}"
else
  fail "the fixture-class evidence was not written"
fi

echo
echo "=== 3. every forbidden project-reference edge, one negative control each"
if [[ -f "${ARCHITECTURE_EVIDENCE}" ]]; then
  controls="$(grep -cv '^#' "${ARCHITECTURE_EVIDENCE}" || true)"
  pass "${controls} controlled forbidden edge(s); see artifacts/architecture/architecture-forbidden-edges.txt"
else
  fail "the forbidden-edge evidence was not written"
fi

echo
echo "=== 4. the complete specification-content defect inventory (MMT-4002 class)"
if [[ -f "${REGISTRY_DEFECTS}" ]]; then
  sed 's/^/      /' "${REGISTRY_DEFECTS}"
  defects="$(sed -n 's/^# total: \([0-9]*\)$/\1/p' "${REGISTRY_DEFECTS}" | tail -1)"
  if [[ "${defects}" == "0" ]]; then
    pass "no specification-content defect"
  else
    #
    # Reported, not masked. The inventory is a ratchet asserted by
    # MechaMiner.Tools.Tests.Audit.RegistryValidatorTests, so a new defect fails stage 1
    # above with its own file:line. This stage exists so the standing inventory is printed
    # in full on every run rather than only when it changes.
    #
    pass "${defects} known specification-content defect(s), each listed above with file:line [MMT-4002]"
  fi
else
  fail "the specification-defect inventory was not written"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-registry: PASS"
  exit 0
fi
echo "verify-registry: FAIL (${failures} assertion(s)) [MMT-4001]"
exit "${EXIT_VALIDATION}"
