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
#
# TWO CONSEQUENCES OF STAGE 0 THAT WILL SURPRISE SOMEONE, STATED HERE SO THE DEBUGGING
# STARTS IN THE RIGHT PLACE:
#
#   1. THIS GATE IS BLOCKED BY PROJECTS IT DOES NOT READ. Stage 0 builds MechaMiner.sln,
#      so breaking game/MechaMiner.Game.csproj alone gives:
#
#        verify-registry: FAIL (the solution does not build, so no registry verdict is
#        available) [MMT-5001]                                                  exit 5
#
#      even though game/ contributes nothing to registry discovery - the registry's
#      selectors resolve against the accepted TEST projects, and the Godot project is not
#      one of them. A registry typo and a broken Godot csproj therefore look the same from
#      the outside: no registry verdict at all. That is deliberate and it is the safe
#      direction. Narrowing stage 0 to the six test projects would replace one roster with
#      two - the sln's and this script's - and a project missing from the second roster is
#      a project whose citations go unresolved while the gate reports PASS, which is the
#      exact defect stage 0 exists to close. The cost is that a Godot-side break withholds
#      an unrelated verdict; the cost of the alternative is a false verdict. If exit 5
#      appears here, read stage 0's build output first and do not look for a registry
#      defect.
#
#   2. STAGE 0 SHOWS ONLY THE LAST 6 LINES of the build. `tail -6` is enough for the usual
#      case, where MSBuild's summary carries the one error, and it is NOT enough for a
#      build with many errors: the tail is then the warning/error counts and the elapsed
#      time, and every diagnostic has scrolled past. The full output is not retained
#      anywhere either. Re-run `dotnet build MechaMiner.sln` directly rather than reading
#      more into the six lines than they can carry.

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
# THE KEY-MATCHING RULE, which is now the same rule the C# reader uses. It is stated in
# build/audit-expectations.env's own header: a key matches when the text before the first
# `=`, trimmed at both ends, equals the key exactly; the value is the rest of the line,
# trimmed; a `#` first non-space character is a comment; a blank line is nothing.
#
# The two readers disagreed about it. This sed allowed `[[:space:]]*=`, so it read
# `FORBIDDEN_EDGE_CONTROLS =112` and printed 112. AuditExpectations compared
# `trimmed[..separator]`, which for that line is "FORBIDDEN_EDGE_CONTROLS " with the
# trailing space, so it matched nothing and threw "declares 0 value(s)". One space before
# the `=` and the two readers of a single-owner value disagreed about whether the value
# existed. That failed closed, which is the right direction and not the point: a mechanism
# whose entire purpose is that one value has one owner is not single-owner while its readers
# disagree about where the value is. `# KEY=value` is also excluded here now - the old
# pattern's leading `[[:space:]]*` did not exclude a comment marker, so a commented-out
# declaration was readable.
#
# $1 key, $2 optional file (defaults to EXPECTATIONS, so stage 6's variant controls can
# drive this exact function over spellings instead of a second implementation of it).
expectation() {
  local key="$1"
  local file="${2:-${EXPECTATIONS}}"
  local value
  [[ -f "${file}" ]] || return 0
  value="$(sed -n -E "s/^[[:space:]]*${key}[[:space:]]*=[[:space:]]*(.*[^[:space:]])[[:space:]]*\$/\1/p" \
    "${file}")"
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
echo "=== 6. the expectations file's key-matching rule, over the same spellings the C# reader uses"
#
# build/audit-expectations.env is a single-owner mechanism with two readers, and until this
# stage existed neither reader's key matching had a control. They disagreed: `KEY =value`
# read as 112 here and as "declares 0 value(s)" in AuditExpectations. The list below is the
# same list AuditExpectationsTests.KeyVariants drives through
# AuditExpectations.ReadFrom - one list in two places, so a divergence is a failing control
# rather than a discovery. Each row is `<expected>|<line>`; `-` means the key must NOT be
# found, and the value 112 is arbitrary.
#
# The accepted spellings are the ones the header commits to. The rejected ones are the
# shapes that must not be mistaken for a declaration: a comment, a longer key with this key
# as a suffix or a prefix, and a key with an interior space, which is not this key.

readonly EXPECTATION_KEY_VARIANTS=(
  "112|PROBE_KEY=112"
  "112|PROBE_KEY =112"
  "112|PROBE_KEY= 112"
  "112|PROBE_KEY = 112"
  "112|  PROBE_KEY=112"
  "112|PROBE_KEY=112  "
  "112|PROBE_KEY	=	112"
  "-|# PROBE_KEY=112"
  "-|#PROBE_KEY=112"
  "-|PREFIX_PROBE_KEY=112"
  "-|PROBE_KEY_SUFFIX=112"
  "-|PROBE KEY=112"
  "-|PROBE_KEY"
  "-|=112"
  "-|"
)

variant_root="$(mktemp -d)"
variant_failures=0
variant_index=0
for variant in "${EXPECTATION_KEY_VARIANTS[@]}"; do
  variant_index=$((variant_index + 1))
  variant_expected="${variant%%|*}"
  variant_line="${variant#*|}"
  variant_file="${variant_root}/variant-${variant_index}.env"
  printf '%s\n' "${variant_line}" >"${variant_file}"

  variant_actual="$(expectation PROBE_KEY "${variant_file}")"
  [[ -n "${variant_actual}" ]] || variant_actual="-"
  if [[ "${variant_actual}" != "${variant_expected}" ]]; then
    fail "stage 6: the line [${variant_line}] read as '${variant_actual}', expected '${variant_expected}'; this reader and AuditExpectations.ReadFrom must agree on every spelling"
    variant_failures=$((variant_failures + 1))
  fi
done

# Two declarations of one key has no single answer, and that is a separate rule from the
# spelling of one declaration.
printf 'PROBE_KEY=112\nPROBE_KEY=113\n' >"${variant_root}/duplicate.env"
if [[ -n "$(expectation PROBE_KEY "${variant_root}/duplicate.env")" ]]; then
  fail "stage 6: two declarations of one key produced a value; there is no single answer to give"
  variant_failures=$((variant_failures + 1))
fi

# The control on the control: the accepted spellings must not all be passing because the
# reader returns nothing for everything.
if [[ "$(expectation PROBE_KEY "${variant_root}/variant-1.env")" != "112" ]]; then
  fail "stage 6: the plain KEY=value spelling did not read, so the rejections above prove nothing"
  variant_failures=$((variant_failures + 1))
fi

rm -rf "${variant_root}"
if [[ "${variant_failures}" -eq 0 ]]; then
  pass "stage 6: ${#EXPECTATION_KEY_VARIANTS[@]} key spellings read the same here as in AuditExpectations.ReadFrom, plus the duplicate-declaration and plain-spelling controls"
fi

echo
echo "=== 7. doc 40's minted content-ID prefixes: the row set, and one minting authority each"
#
# THE READER build/doc40-minted-content-prefixes.expected DID NOT HAVE. That file was
# committed as merge evidence and documented itself, at its own lines 39-41, as wired into
# no gate - and a `git grep` over build/, .github/, scripts/, tests/ and src/ found nothing
# reading it by name. An expectation with no reader is a gate that cannot fail in the most
# literal sense: it could not have caught a row-set change, and it could not have caught the
# defect it was sitting next to.
#
# That defect: `WAV-01` was claimed as "minted here" by BOTH `### Encounter schedule` and
# the omnibus sentence under `### Map generation`, while its table row named only the first.
# A row-set check structurally cannot see it, because both sides of that disagreement leave
# the row set intact. So this stage asserts three things, not one.
#
# It reads the real document. A fixture would be the wrong control here for the reason
# measured elsewhere in this repository this round: a small input clears the very pipe that
# production input breaks, so a probe built at fixture size can pass while the real thing
# fails. doc 40 is ~400 lines and is the subject; there is no reason to substitute a smaller
# stand-in for it.
#
# WHICH ASSERTION CATCHES THE DEFECT THIS EXISTS FOR, checked before it was ever run,
# because a reader that cannot fail on the case it was written for is not the reader. At
# 3742aa2b - this branch's state before the merge - the WAV- ROW said
# `Minted in [Encounter schedule]`, `### Encounter schedule` made no minting claim at all,
# and the omnibus sentence under `### Map generation` claimed `WAV-01`. So there was
# exactly ONE claim, in the wrong section: assertion 1 is green (the row set is intact),
# assertion 2 is green (#encounter-schedule exists), and assertion 3's FIRST half is green
# too (no two sections claim it). Only assertion 3's second half - the section that claims
# a prefix must be the section its row names - goes red. Measured: "WAV-: its row names
# Minted in 'Encounter schedule' but the section claiming to mint it is 'Map generation'".
# That half is the whole reason this is three assertions and not one, and it is the half a
# row-set check cannot have.
#
# WHAT THIS CANNOT SEE. Stated because every gate on this repository that turned out to be
# a problem today had coverage narrower than its name.
#
#   Population: the lines of ONE file, docs/technical/40-content-data-and-validation.md.
#   It says nothing about any other document, and nothing about content/ - a prefix with a
#   correct row here and no definition on disk, or a definition whose id violates the
#   grammar in its own row's Grammar cell, is outside this stage entirely. The grammar cell
#   is read as text and never applied to anything.
#
#   Comparison: prefixes only, never IDs. `SITE-01` through `SITE-04` is one row and one
#   claim; a document that minted `SITE-02` twice, or numbered from `-02`, or contradicted
#   its own "the set is closed at four" prose, satisfies every assertion here.
#
#   Claim detection is textual and conservative. A minting claim is the phrase "minted
#   here" in a clause naming a `PFX-nn` token; the owning section is the nearest preceding
#   heading. A section that mints a prefix in ANY other wording - "this section grants",
#   "the authority for X is here" - is invisible, so assertion 3 can miss a second
#   authority phrased differently. It cannot invent one: the two redirect shapes are
#   handled, and that direction was a real bug rather than a hypothetical, found by running
#   the predicate against the base branch before wiring it. The base's own correct sentence
#   joins two clauses with a semicolon - "`MGC-01` is minted here; `WAV-01` is minted under
#   [Encounter schedule] above" - and a period-only split attributed the claim to both IDs,
#   so this check accused a document that was right. Clause-level splitting fixed it. If
#   the wording drifts again the failure direction is a false accusation, which is loud, but
#   do not read a green stage 7 as proof that no section anywhere claims a prefix twice.
readonly DOC40="${REPO_ROOT}/docs/technical/40-content-data-and-validation.md"
readonly DOC40_EXPECTED="${REPO_ROOT}/build/doc40-minted-content-prefixes.expected"

doc40_problems() {
  # $1 document, $2 expected-prefix file. One problem per line; nothing when all three
  # assertions hold. A missing input is a problem rather than an empty pass.
  local document="$1" expected_file="$2"
  [[ -f "${document}" ]] || { printf '%s\n' "the document is absent: ${document}"; return 0; }
  [[ -f "${expected_file}" ]] || { printf '%s\n' "the expected-prefix file is absent: ${expected_file}"; return 0; }
  python3 - "${document}" "${expected_file}" <<'DOC40PY'
import re, sys

document, expected_file = sys.argv[1], sys.argv[2]
lines = open(document, encoding="utf-8").read().split("\n")

# 1. The row set. `| `PFX-` | grammar | category | dir | Minted in |`
rows = {}
for number, line in enumerate(lines, 1):
    match = re.match(r"^\|\s*`([A-Z]+-)`\s*\|(.*)\|\s*$", line)
    if match:
        cells = [cell.strip() for cell in match.group(2).split("|")]
        rows[match.group(1)] = (number, cells)

expected = set()
for line in open(expected_file, encoding="utf-8"):
    line = line.strip()
    if line and not line.startswith("#"):
        expected.add(line)

for prefix in sorted(expected - set(rows)):
    print("%s is declared in the expected-prefix file and has no table row" % prefix)
for prefix in sorted(set(rows) - expected):
    print("%s has a table row and is not in the expected-prefix file" % prefix)

# Headings, by their GitHub anchor slug, so a "Minted in" link can be resolved.
def slug(text):
    text = re.sub(r"`", "", text).strip().lower()
    text = re.sub(r"[^a-z0-9 -]", "", text)
    return text.replace(" ", "-")

headings = {slug(l.lstrip("#").strip()): l.lstrip("#").strip()
            for l in lines if l.startswith("#")}

# 2. Each row's "Minted in" cell names a section that exists. The cell is either a link
#    `[Text](#anchor)` or the words "this section", which means the table's own section.
minted_in_section = {}
for prefix, (number, cells) in sorted(rows.items()):
    cell = cells[-1] if cells else ""
    link = re.match(r"^\[[^\]]*\]\(#([a-z0-9-]+)\)$", cell)
    if link:
        anchor = link.group(1)
        if anchor not in headings:
            print("%s (row at line %d) names Minted in '%s', and no heading in this document has that anchor"
                  % (prefix, number, anchor))
            continue
        minted_in_section[prefix] = headings[anchor]
    elif cell == "this section":
        minted_in_section[prefix] = "Minted content-ID grammars"
    else:
        print("%s (row at line %d) has a Minted in cell of '%s', which is neither a #anchor link nor 'this section'"
              % (prefix, number, cell))

# 3. No other section claims to mint the prefix. A claim is a sentence containing
#    "minted here"; the section it belongs to is the nearest preceding heading. A sentence
#    that says the prefix is explicitly NOT minted here is not a claim.
section_of_line, current = {}, None
for number, line in enumerate(lines, 1):
    if line.startswith("#"):
        current = line.lstrip("#").strip()
    section_of_line[number] = current

# Clauses, not sentences. Splitting on periods alone was a false-accusation bug found
# before this check ever ran: the base branch's own correct wording is one sentence
# joined by a semicolon - "`MGC-01` is minted here; `WAV-01` is minted under [Encounter
# schedule] above" - and a period-only split attributed the "minted here" to BOTH IDs,
# so the reader accused a document that was right. Semicolons and colons end a clause
# here for that reason.
claims = {}
for number, line in enumerate(lines, 1):
    if "minted here" not in line:
        continue
    for clause in re.split(r"(?<=[.;:])\s+", line):
        if "minted here" not in clause:
            continue
        for prefix in {m + "-" for m in re.findall(r"`([A-Z]+)-[0-9A-Z]", clause)}:
            token = re.escape("`" + prefix) + r"[0-9A-Z]*`"
            # Two shapes redirect rather than claim, and both appear in this document:
            #   "`WAV-01` is **not**: it is minted under ..."   an explicit disclaimer
            #   "`WAV-01` is minted under [Encounter schedule]"  a pointer to the owner
            # Either one names a DIFFERENT section as the authority, so neither is this
            # section claiming the prefix.
            if re.search(token + r"\s+is\s+\*\*not\*\*", clause):
                continue
            if re.search(token + r"\s+is\s+minted\s+(under|in)\s", clause):
                continue
            claims.setdefault(prefix, set()).add(section_of_line[number])

for prefix in sorted(rows):
    claiming = claims.get(prefix, set())
    if len(claiming) > 1:
        print("%s is claimed as minted by %d sections - %s - and a prefix has exactly one minting authority"
              % (prefix, len(claiming), "; ".join(sorted(claiming))))
        continue
    if not claiming:
        continue
    owner = minted_in_section.get(prefix)
    claimed = next(iter(claiming))
    if owner is not None and owner != claimed:
        print("%s: its row names Minted in '%s' but the section claiming to mint it is '%s'"
              % (prefix, owner, claimed))
DOC40PY
}

mapfile -t doc40_findings < <(doc40_problems "${DOC40}" "${DOC40_EXPECTED}")
if [[ "${#doc40_findings[@]}" -eq 0 ]]; then
  pass "stage 7: doc 40's $(grep -vc '^#\|^$' "${DOC40_EXPECTED}" || true) minted prefixes each have a table row in build/doc40-minted-content-prefixes.expected, a Minted in cell resolving to a heading that exists, and exactly one section claiming to mint them"
else
  fail "stage 7: doc 40's minted-prefix declarations are inconsistent: $(printf '%s; ' "${doc40_findings[@]}")"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-registry: PASS"
  exit 0
fi
echo "verify-registry: FAIL (${failures} assertion(s)) [MMT-4001]"
exit "${EXIT_VALIDATION}"
