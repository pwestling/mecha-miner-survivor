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
#   A tree that does not compile is exit class 5 with MMT-5001, and is reported before any
#   registry verdict is computed. See stage 0.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 5 build failure, 4 validation failure.
#
# On stage 0, and why it is not optional:
#
#   src/MechaMiner.Tools/Audit/TestInventory.cs asks the NUnit harness which tests exist by
#   running `dotnet test --list-tests --no-build` once per accepted test project, and the
#   registry's nunit selectors are resolved against that answer. `--no-build` is correct
#   there - discovery runs from inside a test process and must not rewrite the assembly it
#   is executing - but it means discovery reports what is in bin/, not what is in the tree.
#
#   This script used to run `dotnet test "${TOOLS_TESTS}"` without --no-build, which builds
#   exactly one of the six projects it then discovers against. Its verdict was therefore a
#   function of which assemblies happened to be built. Measured on one SHA with an identical
#   tree, only bin/ differing: nothing built -> 23 tests discovered -> exit 4 with 21
#   UnresolvedTestSelector findings blaming the registry; Diagnostics.Tests additionally
#   built -> 64 discovered -> exit 4 with a different 7; the whole solution built -> 99
#   discovered -> PASS. Worse in the green direction: after a passing run, deleting a cited
#   test from MechaMiner.Diagnostics.Tests source and not rebuilding still gave PASS with
#   "unresolved selectors: none", so the gate certified a citation to a test that no longer
#   existed - the exact defect selector resolution was written to close, reached through
#   build state instead of a typo.
#
#   So the gate builds first and then discovers against what it built. Building the solution
#   rather than a hand-written list of the six test projects is deliberate: a second roster
#   of test projects could drift from AcceptedArchitecture's, which is the roster
#   TestInventory iterates, and solution membership is itself asserted by
#   ArchitectureRuleTests.
#
#   Verifying assembly timestamps against sources instead was considered and rejected: it
#   would have to re-implement MSBuild's up-to-date rules, and its failure mode is a false
#   staleness alarm, which is the failure mode that gets a check deleted.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly SOLUTION="${REPO_ROOT}/MechaMiner.sln"
readonly TOOLS_TESTS="${REPO_ROOT}/tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj"
readonly ARCHITECTURE_EVIDENCE="${REPO_ROOT}/artifacts/architecture/architecture-forbidden-edges.txt"
readonly REGISTRY_DEFECTS="${REPO_ROOT}/artifacts/registry/registry-specification-defects.txt"
readonly FIXTURE_EVIDENCE="${REPO_ROOT}/artifacts/registry/registry-fixture-classes.txt"
readonly EXIT_VALIDATION=4
readonly EXIT_BUILD=5

# The registry failure classes TASK-FND-009-002's completion gate names, each of which must
# appear in the retained fixture-class inventory under its own heading.
readonly REQUIRED_FIXTURE_CLASSES=(missing duplicate dangling malformed)

# The floor stage 3 holds the controlled-forbidden-edge count to. The number is not chosen
# here: it is the same floor
# MechaMiner.Tools.Tests.Audit.ArchitectureRuleTests.EveryForbiddenReferenceEdgeIsRejected
# asserts with Assert.That(controls, Is.GreaterThanOrEqualTo(100)), which is what makes the
# complete ordered-pair matrix a matrix rather than a sample. Stage 3 asserts the same floor
# against the retained evidence so that a truncated, stubbed, or hand-edited inventory
# cannot be reported as a covered one.
readonly MINIMUM_FORBIDDEN_EDGE_CONTROLS=100

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

#
# The three evidence checks below are functions, not inline `if [[ -f ... ]]` blocks, for one
# reason: stage 5 runs each of them against deliberately defective evidence and requires it
# to reject it. An assertion nothing has ever been shown to fail is not an assertion, and
# every one of these three used to be exactly that - stage 3 read a count and then called
# `pass` unconditionally, and stage 4 called `pass` in both branches of its `if`. With the
# test run stubbed and all three files reduced to one comment line each, this script printed
# `ok 0 controlled forbidden edge(s)`, `ok  known specification-content defect(s)` with an
# empty number, and `verify-registry: PASS`, exit 0.
#
# Each function prints one line - the reason on rejection, the summary on acceptance - and
# returns 0 only when it accepts.
#

# Requires the fixture-class inventory to carry a heading for each of the failure classes.
check_fixture_classes() {
  local file="$1" headings missing=() class
  if [[ ! -f "${file}" ]]; then
    printf 'the fixture-class evidence was not written (%s)\n' "${file}"
    return 1
  fi

  for class in "${REQUIRED_FIXTURE_CLASSES[@]}"; do
    if ! grep -q "^## ${class} (expects " "${file}"; then
      missing+=("${class}")
    fi
  done
  if ((${#missing[@]} > 0)); then
    printf 'the fixture-class inventory has no "## <class> (expects <rule>)" heading for: %s\n' \
      "${missing[*]}"
    return 1
  fi

  headings="$(grep -c '^## ' "${file}")" || headings=0
  if ((headings < ${#REQUIRED_FIXTURE_CLASSES[@]})); then
    printf 'the fixture-class inventory carries %s class heading(s), fewer than the %s required\n' \
      "${headings}" "${#REQUIRED_FIXTURE_CLASSES[@]}"
    return 1
  fi

  printf '%s registry failure class(es), one fixture each (%s)\n' \
    "${headings}" "${REQUIRED_FIXTURE_CLASSES[*]}"
}

# Requires the forbidden-edge inventory to carry a self-consistent count at or above the
# floor. The count printed on a passing run is now the asserted one, rather than a number
# read out of a file and echoed with the typography of an assertion.
check_forbidden_edges() {
  local file="$1" rows footer
  if [[ ! -f "${file}" ]]; then
    printf 'the forbidden-edge evidence was not written (%s)\n' "${file}"
    return 1
  fi

  rows="$(grep -cv '^#' "${file}")" || rows=0
  footer="$(sed -n 's/^# \([0-9][0-9]*\) forbidden edges, each individually controlled\.$/\1/p' \
    "${file}" | tail -1)"
  if [[ -z "${footer}" ]]; then
    printf 'the inventory carries no "# <n> forbidden edges, each individually controlled." footer, so the %s row(s) it does carry are attributable to nothing\n' \
      "${rows}"
    return 1
  fi
  if [[ "${rows}" != "${footer}" ]]; then
    printf 'the inventory claims %s controlled edge(s) but carries %s control row(s)\n' \
      "${footer}" "${rows}"
    return 1
  fi
  if ((rows < MINIMUM_FORBIDDEN_EDGE_CONTROLS)); then
    printf 'the inventory carries %s controlled forbidden edge(s), below the floor of %s that ArchitectureRuleTests.EveryForbiddenReferenceEdgeIsRejected asserts\n' \
      "${rows}" "${MINIMUM_FORBIDDEN_EDGE_CONTROLS}"
    return 1
  fi

  printf '%s controlled forbidden edge(s), at or above the floor of %s; see artifacts/architecture/architecture-forbidden-edges.txt\n' \
    "${rows}" "${MINIMUM_FORBIDDEN_EDGE_CONTROLS}"
}

# Requires the specification-defect inventory to declare exactly one "# total: <n>", for an
# integer n, and to carry exactly n file:line rows. The empty string is not a count: the
# previous version accepted it and printed `ok  known specification-content defect(s)`.
check_defect_inventory() {
  local file="$1" declared total rows
  if [[ ! -f "${file}" ]]; then
    printf 'the specification-defect inventory was not written (%s)\n' "${file}"
    return 1
  fi

  declared="$(grep -c '^# total: ' "${file}")" || declared=0
  if [[ "${declared}" != "1" ]]; then
    printf 'the inventory carries %s "# total: <n>" line(s); exactly one is required\n' "${declared}"
    return 1
  fi

  total="$(sed -n 's/^# total: \(.*\)$/\1/p' "${file}")"
  if [[ ! "${total}" =~ ^(0|[1-9][0-9]*)$ ]]; then
    printf 'the "# total:" line reads %q, which is not an integer, so there is no known count to report\n' \
      "${total}"
    return 1
  fi

  rows="$(grep -cv '^#' "${file}")" || rows=0
  if ((total != rows)); then
    printf 'the inventory declares %s defect(s) but carries %s file:line row(s)\n' "${total}" "${rows}"
    return 1
  fi

  #
  # Whether the count is 0 or 3 is not this stage's verdict, and deliberately so. The
  # inventory is a ratchet asserted by
  # RegistryValidatorTests.TheSpecificationDefectInventoryIsRecorded, which stage 1 runs, so
  # a new defect fails there with its own file:line. What this stage owns is that the number
  # printed here is a real, self-consistent count rather than whatever the file happened to
  # say.
  #
  if [[ "${total}" == "0" ]]; then
    printf 'no specification-content defect, and the inventory says so in %s row(s)\n' "${rows}"
    return 0
  fi
  printf '%s known specification-content defect(s), each of the %s listed above with file:line [MMT-4002]\n' \
    "${total}" "${rows}"
}

# Runs one evidence check against deliberately defective evidence and requires rejection.
control() {
  local description="$1" checker="$2" file="$3" reason
  if reason="$("${checker}" "${file}" 2>&1)"; then
    fail "negative control did not fire: ${description} was accepted (${reason})"
  else
    pass "${description} -> rejected: ${reason}"
  fi
}

echo "=== 0. build every accepted test project, so discovery reads the tree and not stale assemblies"
build_output="$(cd "${REPO_ROOT}" && dotnet build "${SOLUTION}" --nologo -v minimal 2>&1)"
build_status=$?
printf '%s\n' "${build_output}" | tail -6 | sed 's/^/      /'
if [[ "${build_status}" -ne 0 ]]; then
  echo
  echo "verify-registry: FAIL (the solution does not build, so no registry verdict is available) [MMT-5001]"
  exit "${EXIT_BUILD}"
fi
pass "the solution builds, so every accepted test project's assembly was produced from these sources"

echo
echo "=== 1. the registry validator and the architecture rules, with their negative controls"
output="$(cd "${REPO_ROOT}" && dotnet test "${TOOLS_TESTS}" \
  --no-build --nologo -v minimal \
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
if summary="$(check_fixture_classes "${FIXTURE_EVIDENCE}")"; then
  pass "${summary}"
  sed 's/^/      /' "${FIXTURE_EVIDENCE}"
else
  fail "${summary}"
fi

echo
echo "=== 3. every forbidden project-reference edge, one negative control each"
if summary="$(check_forbidden_edges "${ARCHITECTURE_EVIDENCE}")"; then
  pass "${summary}"
else
  fail "${summary}"
fi

echo
echo "=== 4. the complete specification-content defect inventory (MMT-4002 class)"
if [[ -f "${REGISTRY_DEFECTS}" ]]; then
  sed 's/^/      /' "${REGISTRY_DEFECTS}"
fi
if summary="$(check_defect_inventory "${REGISTRY_DEFECTS}")"; then
  pass "${summary}"
else
  fail "${summary}"
fi

echo
echo "=== 5. the three evidence checks above, each shown to reject defective evidence"
control_root="$(mktemp -d)"
trap 'rm -rf "${control_root}"' EXIT

: >"${control_root}/empty.txt"
printf '# a single comment line and nothing else\n' >"${control_root}/one-comment.txt"

# Three of the four required class headings.
{
  printf '# three of four classes\n'
  printf '## missing (expects UndefinedIdentifier)\n'
  printf '## duplicate (expects DuplicateIdentifier)\n'
  printf '## dangling (expects BrokenLink)\n'
} >"${control_root}/three-classes.txt"

# 100 control rows with no footer, the same 100 with a footer that disagrees, and 99 rows
# with an honest footer just below the floor.
{
  printf '# rows but no footer\n'
  for edge in $(seq 1 100); do printf 'edge-%s\tForbiddenReference\tForbiddenReference\n' "${edge}"; done
} >"${control_root}/edges-no-footer.txt"
{
  cat "${control_root}/edges-no-footer.txt"
  printf '# 112 forbidden edges, each individually controlled.\n'
} >"${control_root}/edges-footer-disagrees.txt"
{
  printf '# an honest inventory, one row short of the floor\n'
  for edge in $(seq 1 99); do printf 'edge-%s\tForbiddenReference\tForbiddenReference\n' "${edge}"; done
  printf '# 99 forbidden edges, each individually controlled.\n'
} >"${control_root}/edges-below-floor.txt"

# A defect inventory with rows but no total, with an empty total, with a non-numeric total,
# and with a total that disagrees with the rows it carries.
{
  printf '# rows but no total\n'
  printf 'SpecificationDefect\tUndefinedIdentifier\tdocs/x.md:1\tTR-X-001\tdetail\n'
} >"${control_root}/defects-no-total.txt"
{
  cat "${control_root}/defects-no-total.txt"
  printf '# total: \n'
} >"${control_root}/defects-empty-total.txt"
{
  cat "${control_root}/defects-no-total.txt"
  printf '# total: three\n'
} >"${control_root}/defects-word-total.txt"
{
  cat "${control_root}/defects-no-total.txt"
  printf '# total: 5\n'
} >"${control_root}/defects-total-disagrees.txt"

control "stage 2: absent evidence" check_fixture_classes "${control_root}/absent.txt"
control "stage 2: empty evidence" check_fixture_classes "${control_root}/empty.txt"
control "stage 2: one comment line" check_fixture_classes "${control_root}/one-comment.txt"
control "stage 2: three of four class headings" check_fixture_classes "${control_root}/three-classes.txt"

control "stage 3: absent evidence" check_forbidden_edges "${control_root}/absent.txt"
control "stage 3: empty evidence" check_forbidden_edges "${control_root}/empty.txt"
control "stage 3: one comment line" check_forbidden_edges "${control_root}/one-comment.txt"
control "stage 3: 100 rows and no footer" check_forbidden_edges "${control_root}/edges-no-footer.txt"
control "stage 3: footer disagrees with rows" check_forbidden_edges "${control_root}/edges-footer-disagrees.txt"
control "stage 3: honest count one below the floor" check_forbidden_edges "${control_root}/edges-below-floor.txt"

control "stage 4: absent evidence" check_defect_inventory "${control_root}/absent.txt"
control "stage 4: empty evidence" check_defect_inventory "${control_root}/empty.txt"
control "stage 4: one comment line" check_defect_inventory "${control_root}/one-comment.txt"
control "stage 4: rows and no total" check_defect_inventory "${control_root}/defects-no-total.txt"
control "stage 4: an empty total" check_defect_inventory "${control_root}/defects-empty-total.txt"
control "stage 4: a non-numeric total" check_defect_inventory "${control_root}/defects-word-total.txt"
control "stage 4: a total that disagrees with its rows" check_defect_inventory "${control_root}/defects-total-disagrees.txt"

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-registry: PASS"
  exit 0
fi
echo "verify-registry: FAIL (${failures} assertion(s)) [MMT-4001]"
exit "${EXIT_VALIDATION}"
