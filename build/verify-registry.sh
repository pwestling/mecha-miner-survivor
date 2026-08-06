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

readonly EXPECTATIONS="${REPO_ROOT}/build/audit-expectations.env"

#
# The two expectations stages 2 and 3 compare evidence against are READ, not written down
# here. They used to be written down here as well as in tests/MechaMiner.Tools.Tests/Audit,
# and the comment above MINIMUM_FORBIDDEN_EDGE_CONTROLS said the number "is not chosen
# here: it is the same floor" the C# test asserts. That was a description of an intention,
# not of a mechanism: the two were separate literals that happened to be equal. Changing
# the C# assertion from 100 to 10 left this script at exit 0 still printing "at or above
# the floor of 100 that ArchitectureRuleTests.EveryForbiddenReferenceEdgeIsRejected
# asserts" - a claim about a number the named test no longer asserted.
#
# So both readers parse build/audit-expectations.env, and this one has no fallback: if a
# value is absent or unparseable, the stage that needs it fails rather than resuming the
# number it used to hardcode. Whatever this script now reports about these values, it read.
#
expectation() {
  # $1 key. Prints the value, or nothing when the file or the key is unusable.
  local value
  [[ -f "${EXPECTATIONS}" ]] || return 0
  value="$(sed -n -E "s/^[[:space:]]*$1[[:space:]]*=[[:space:]]*(.*[^[:space:]])[[:space:]]*\$/\1/p" \
    "${EXPECTATIONS}")"
  # Exactly one declaration, or none: two lines for one key has no single answer.
  [[ "$(printf '%s' "${value}" | grep -c .)" == "1" ]] || return 0
  printf '%s' "${value}"
}

# The registry failure classes TASK-FND-009-002's completion gate names, each paired with
# the rule its fixture must fail under. Both halves are checked: the heading has to name
# the class AND the rule, and the section under it has to carry a row the named rule
# produced.
read -r -a REQUIRED_FIXTURE_CLASSES <<<"$(expectation REGISTRY_FIXTURE_CLASSES)"
readonly REQUIRED_FIXTURE_CLASSES

# The exact size of the forbidden-edge matrix, asserted rather than used as a floor. A
# floor plus a footer/row self-consistency check - which is what stage 3 was - accepts a
# hand-written inventory of 150 rows claiming 150, and one of exactly 100, while the real
# matrix is 112. Neither is the matrix.
readonly EXPECTED_FORBIDDEN_EDGE_CONTROLS="$(expectation FORBIDDEN_EDGE_CONTROLS)"

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

# Requires the fixture-class inventory to carry, for each failure class, a heading that
# names the class AND the rule its fixture must fail under, and at least one row beneath
# that heading which the named rule produced.
#
# Both halves were holes. The heading test was `grep -q "^## ${class} (expects "`, which
# read the prefix and stopped: `## malformed (expects Whatever)` was accepted, so the
# heading's own claim about which rule the fixture proves went unread. And nothing looked
# below the headings at all, so four correct headings with zero rows under them - which is
# what a stubbed or truncated evidence writer produces - was accepted and reported as
# "4 registry failure class(es), one fixture each".
#
# The expected rule names come from build/audit-expectations.env, the same line
# RegistryValidatorTests.EachFixtureClassFailsUnderItsOwnRule builds its class table from.
# Hardcoding them here would have made this check a second literal disagreeing with the
# writer of the file it checks, which is the defect it is closing.
check_fixture_classes() {
  local file="$1" headings pair class rule rows missing=() unproved=()
  if [[ ! -f "${file}" ]]; then
    printf 'the fixture-class evidence was not written (%s)\n' "${file}"
    return 1
  fi
  if ((${#REQUIRED_FIXTURE_CLASSES[@]} == 0)); then
    printf 'REGISTRY_FIXTURE_CLASSES could not be read from %s, so there is no list of failure classes to hold the inventory to\n' \
      "${EXPECTATIONS}"
    return 1
  fi

  for pair in "${REQUIRED_FIXTURE_CLASSES[@]}"; do
    class="${pair%%:*}"
    rule="${pair#*:}"
    if [[ -z "${class}" || -z "${rule}" || "${pair}" != *:* ]]; then
      printf 'REGISTRY_FIXTURE_CLASSES in %s carries %q, which is not a <class>:<rule> pair\n' \
        "${EXPECTATIONS}" "${pair}"
      return 1
    fi

    if ! grep -qxF "## ${class} (expects ${rule})" "${file}"; then
      missing+=("## ${class} (expects ${rule})")
      continue
    fi

    # Rows between this heading and the next one whose rule column is the named rule. A
    # heading is a claim about what the fixture proved; the row is the proof.
    rows="$(awk -F'\t' \
      -v heading="## ${class} (expects ${rule})" -v rule="${rule}" '
        $0 == heading { inside = 1; next }
        substr($0, 1, 3) == "## " { inside = 0 }
        inside && substr($0, 1, 1) != "#" && $2 == rule { found++ }
        END { print found + 0 }' "${file}")"
    if ((rows == 0)); then
      unproved+=("${class} (no ${rule} row under its heading)")
    fi
  done

  if ((${#missing[@]} > 0)); then
    printf 'the fixture-class inventory carries no heading reading exactly: %s\n' "${missing[*]}"
    return 1
  fi
  if ((${#unproved[@]} > 0)); then
    printf 'the fixture-class inventory has the heading but not the finding for: %s\n' \
      "${unproved[*]}"
    return 1
  fi

  headings="$(grep -c '^## ' "${file}")" || headings=0
  if ((headings < ${#REQUIRED_FIXTURE_CLASSES[@]})); then
    printf 'the fixture-class inventory carries %s class heading(s), fewer than the %s required\n' \
      "${headings}" "${#REQUIRED_FIXTURE_CLASSES[@]}"
    return 1
  fi

  printf '%s registry failure class(es), each with a heading naming its rule and at least one row that rule produced (%s)\n' \
    "${#REQUIRED_FIXTURE_CLASSES[@]}" "${REQUIRED_FIXTURE_CLASSES[*]}"
}

# Requires the forbidden-edge inventory to carry a self-consistent count that EQUALS the
# committed size of the matrix, taking the expected count as $2 so the negative controls
# below drive the identical predicate with the identical expectation.
#
# This was a floor plus a self-consistency check, not a comparison: a hand-written
# inventory of 150 rows with a footer claiming 150 was accepted, and so was one of exactly
# 100 rows claiming 100, while the real matrix is 112. A self-consistent inventory is
# consistent with itself and says nothing about the matrix. The count cannot be derived
# here either - it falls out of the accepted-boundary table in
# src/MechaMiner.Tools/Audit/AcceptedArchitecture.cs, and re-deriving that in shell would
# be a second implementation of what is being checked - so it is committed once in
# build/audit-expectations.env and both readers compare against that.
check_forbidden_edges() {
  local file="$1" expected="$2" rows footer
  if [[ -z "${expected}" ]]; then
    printf 'FORBIDDEN_EDGE_CONTROLS could not be read from %s, so the inventory has nothing to be compared against and its row count means nothing\n' \
      "${EXPECTATIONS}"
    return 1
  fi
  if [[ ! "${expected}" =~ ^(0|[1-9][0-9]*)$ ]]; then
    printf 'FORBIDDEN_EDGE_CONTROLS in %s reads %q, which is not a count\n' \
      "${EXPECTATIONS}" "${expected}"
    return 1
  fi
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
  if ((rows != expected)); then
    printf 'the inventory carries %s controlled forbidden edge(s) and the matrix is %s (FORBIDDEN_EDGE_CONTROLS in %s); a self-consistent inventory of the wrong size is not the matrix\n' \
      "${rows}" "${expected}" "${EXPECTATIONS}"
    return 1
  fi

  printf '%s controlled forbidden edge(s), exactly the %s the committed matrix size declares; see artifacts/architecture/architecture-forbidden-edges.txt\n' \
    "${rows}" "${expected}"
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
# Everything after the checker name is passed through, so a control gives the checker the
# same arguments the real stage gives it and differs only in the evidence.
control() {
  local description="$1" checker="$2" reason
  shift 2
  if reason="$("${checker}" "$@" 2>&1)"; then
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
echo "=== 2. every registry failure class, one fixture each, each proving its own rule"
if summary="$(check_fixture_classes "${FIXTURE_EVIDENCE}")"; then
  pass "${summary}"
  sed 's/^/      /' "${FIXTURE_EVIDENCE}"
else
  fail "${summary}"
fi

echo
echo "=== 3. every forbidden project-reference edge, one negative control each"
if summary="$(check_forbidden_edges "${ARCHITECTURE_EVIDENCE}" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}")"; then
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
echo "=== 5. the three evidence checks above, each shown to accept sound evidence and reject defective evidence"
control_root="$(mktemp -d)"
trap 'rm -rf "${control_root}"' EXIT

: >"${control_root}/empty.txt"
printf '# a single comment line and nothing else\n' >"${control_root}/one-comment.txt"

# A fixture-class inventory that carries every heading and every row it should, so the
# defective variants below differ from an acceptable file in exactly one way each. Written
# from the same class:rule list stage 2 checks against, because a control that hardcoded
# the headings would stop tracking the list.
fixture_class_section() {
  # $1 class, $2 rule, $3 rule to spell in the row (so a "heading but no finding" variant
  # can be built by naming a different rule).
  printf '## %s (expects %s)\n' "$1" "$2"
  printf 'Error\t%s\tdocs/x.md:1\tTR-X-001\tdetail\n' "$3"
}
{
  printf '# every heading, every row\n'
  for pair in "${REQUIRED_FIXTURE_CLASSES[@]}"; do
    printf '\n'
    fixture_class_section "${pair%%:*}" "${pair#*:}" "${pair#*:}"
  done
} >"${control_root}/classes-complete.txt"

# One heading short.
{
  printf '# one class short\n'
  for pair in "${REQUIRED_FIXTURE_CLASSES[@]:1}"; do
    printf '\n'
    fixture_class_section "${pair%%:*}" "${pair#*:}" "${pair#*:}"
  done
} >"${control_root}/classes-one-short.txt"

# Every heading present, every rule name in the headings replaced. This is the variant the
# old prefix grep accepted: `## malformed (expects Whatever)` matched `^## malformed
# (expects ` and the rest of the heading was never read.
{
  printf '# headings that name no real rule\n'
  for pair in "${REQUIRED_FIXTURE_CLASSES[@]}"; do
    printf '\n'
    fixture_class_section "${pair%%:*}" "Whatever" "${pair#*:}"
  done
} >"${control_root}/classes-bogus-rule.txt"

# Every heading correct and not one row beneath any of them, which is what a stubbed or
# truncated evidence writer produces. Also accepted before.
{
  printf '# correct headings, no findings\n'
  for pair in "${REQUIRED_FIXTURE_CLASSES[@]}"; do
    printf '\n## %s (expects %s)\n' "${pair%%:*}" "${pair#*:}"
  done
} >"${control_root}/classes-no-rows.txt"

# Every heading correct, rows present, but the last class's rows are all some other rule -
# the heading claims a proof the section does not carry.
{
  printf '# a heading whose section proves a different rule\n'
  for pair in "${REQUIRED_FIXTURE_CLASSES[@]::${#REQUIRED_FIXTURE_CLASSES[@]}-1}"; do
    printf '\n'
    fixture_class_section "${pair%%:*}" "${pair#*:}" "${pair#*:}"
  done
  printf '\n'
  last="${REQUIRED_FIXTURE_CLASSES[*]: -1}"
  fixture_class_section "${last%%:*}" "${last#*:}" "SomeOtherRule"
} >"${control_root}/classes-wrong-rule-rows.txt"

# Rows with no footer; the same rows with a footer that disagrees; an honest inventory one
# row short of the matrix; and the two self-consistent-but-wrong inventories a floor plus a
# self-consistency check accepted - one padded well above the matrix and one sitting exactly
# on the old floor of 100.
edge_inventory() {
  # $1 row count, $2 footer count, or "none" for no footer.
  local edge
  printf '# a synthetic inventory\n'
  for edge in $(seq 1 "$1"); do
    printf 'edge-%s\tForbiddenReference\tForbiddenReference\n' "${edge}"
  done
  [[ "$2" == "none" ]] || printf '# %s forbidden edges, each individually controlled.\n' "$2"
}
edge_inventory "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}" none >"${control_root}/edges-no-footer.txt"
edge_inventory "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}" \
  "$((EXPECTED_FORBIDDEN_EDGE_CONTROLS + 1))" >"${control_root}/edges-footer-disagrees.txt"
edge_inventory "$((EXPECTED_FORBIDDEN_EDGE_CONTROLS - 1))" \
  "$((EXPECTED_FORBIDDEN_EDGE_CONTROLS - 1))" >"${control_root}/edges-one-short.txt"
edge_inventory 150 150 >"${control_root}/edges-self-consistent-150.txt"
edge_inventory 100 100 >"${control_root}/edges-self-consistent-100.txt"

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

# The complete fixture-class inventory must be ACCEPTED, or the four rejections below
# would also be produced by a check that rejects everything.
if summary="$(check_fixture_classes "${control_root}/classes-complete.txt")"; then
  pass "stage 2: a complete synthetic inventory -> accepted: ${summary}"
else
  fail "stage 2: a complete synthetic inventory was rejected (${summary}), so the rejections below prove nothing"
fi
control "stage 2: absent evidence" check_fixture_classes "${control_root}/absent.txt"
control "stage 2: empty evidence" check_fixture_classes "${control_root}/empty.txt"
control "stage 2: one comment line" check_fixture_classes "${control_root}/one-comment.txt"
control "stage 2: one class heading short" check_fixture_classes "${control_root}/classes-one-short.txt"
control "stage 2: headings that name no real rule" check_fixture_classes "${control_root}/classes-bogus-rule.txt"
control "stage 2: correct headings with no findings beneath them" check_fixture_classes "${control_root}/classes-no-rows.txt"
control "stage 2: a heading whose section proves a different rule" check_fixture_classes "${control_root}/classes-wrong-rule-rows.txt"

control "stage 3: absent evidence" check_forbidden_edges "${control_root}/absent.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: empty evidence" check_forbidden_edges "${control_root}/empty.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: one comment line" check_forbidden_edges "${control_root}/one-comment.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: the right number of rows and no footer" check_forbidden_edges "${control_root}/edges-no-footer.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: footer disagrees with rows" check_forbidden_edges "${control_root}/edges-footer-disagrees.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: an honest inventory one row short of the matrix" check_forbidden_edges "${control_root}/edges-one-short.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: self-consistent at 150, which is not the matrix" check_forbidden_edges "${control_root}/edges-self-consistent-150.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: self-consistent at exactly the old floor of 100" check_forbidden_edges "${control_root}/edges-self-consistent-100.txt" "${EXPECTED_FORBIDDEN_EDGE_CONTROLS}"
control "stage 3: an unreadable expected count" check_forbidden_edges "${ARCHITECTURE_EVIDENCE}" ""

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
