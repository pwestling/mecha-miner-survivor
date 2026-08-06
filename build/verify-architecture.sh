#!/usr/bin/env bash
#
# Asserts the accepted repository layout and the accepted project boundary.
#
# Authority: docs/technical/115-component-contract-and-schema-registry.md
#              § Accepted project boundary
#            docs/technical/100-build-dependencies-and-release-operations.md
#              § Repository structure
#            docs/technical/00-technical-foundation.md § Language boundary
# Requirements: TR-CTR-001, TR-BLD-006, TR-FND-001, TR-FND-002
# Verification: VER-FND-001-003, VER-FND-001-004, VER-FND-001-005
#
# This is a real assertion, not a review aid: the reference graph is read from
# MSBuild's own evaluation of every project (so implicit SDK-injected package
# references are included), the Godot dependency is read from the committed
# NuGet lock files, and every mismatch exits nonzero.
#
# TASK-FND-009-001 added real architecture tests in tests/MechaMiner.Tools.Tests, with
# one negative control per forbidden edge. This script deliberately stays: CI and the
# build verb both call it, and it reads MSBuild's own evaluation of every project, which
# catches an SDK-injected package reference that no project file mentions. The two gates
# are independent on purpose - neither consumes the other's output - so one reader's
# defect cannot hide from both.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -euo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

# --- Accepted layout (doc 100 § Repository structure) ------------------------

readonly EXPECTED_PATHS=(
  "MechaMiner.sln"
  "global.json"
  "Directory.Build.props"
  "Directory.Packages.props"
  # doc 100 § Repository structure lists both root wrappers in the accepted tree.
  # They exist from FND-002 onward and are the only workflow entrypoint.
  "build.sh"
  "build.ps1"
  "game/project.godot"
  "game/MechaMiner.Game.csproj"
  "game/scenes"
  "game/shaders"
  "game/presentation"
  "src/MechaMiner.Simulation"
  "src/MechaMiner.Content"
  "src/MechaMiner.Diagnostics"
  "src/MechaMiner.Persistence"
  "src/MechaMiner.Tools"
  "tests/MechaMiner.Simulation.Tests"
  "tests/MechaMiner.Content.Tests"
  "tests/MechaMiner.Diagnostics.Tests"
  "tests/MechaMiner.Persistence.Tests"
  "tests/MechaMiner.Tools.Tests"
  "tests/MechaMiner.Game.Tests"
  "tests/verification"
  "content"
  # doc 40 § Accepted content repository layout. content/schemas was in that layout and
  # simply missing from this gate; content/player was added by the same doc change that
  # gave the shared player baseline somewhere to live, because a mech definition carries
  # Hull/Armor/Recovery/movement/footprint *overrides* and the overridden values are not
  # mech data. Both carry a .gitkeep, the way FND-001 seeded every other empty accepted
  # directory, so adding the path does not add a failure.
  "content/schemas"
  "content/player"
  "assets-source"
  "assets-runtime"
  "assets-manifest"
  "generated"
  "docs"
  "build"
)

# --- Accepted project boundary (doc 115) -------------------------------------
# "<project>|<comma separated allowed project references>|<godot: yes|no>"
# The reference list is exact: a project may not have an edge that is not listed.

readonly EXPECTED_PROJECTS=(
  "src/MechaMiner.Content/MechaMiner.Content.csproj||no"
  "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj||no"
  "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Tools/MechaMiner.Tools.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj|MechaMiner.Content|no"
  "tests/MechaMiner.Diagnostics.Tests/MechaMiner.Diagnostics.Tests.csproj|MechaMiner.Diagnostics|no"
  "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj|MechaMiner.Simulation|no"
  "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj|MechaMiner.Persistence|no"
  "tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj|MechaMiner.Tools|no"
  "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "game/MechaMiner.Game.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|yes"
)

# The one value that can never be a legitimate item identity, printed when MSBuild could
# not be asked. Without it, a failed evaluation is indistinguishable from a project that
# genuinely declares nothing: the exit status would be discarded by the pipeline, every
# comparison would run against an empty set, and a project with a forbidden edge would be
# reported as compliant. Emitting a value that matches no accepted set instead turns a
# discarded status into a visible failure.
readonly EVALUATION_FAILED="MSBUILD-EVALUATION-FAILED"

msbuild_items() {
  # $1 project, $2 item name. Prints one Identity per line, sorted.
  local document status
  document="$(dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getItem:$2" 2>/dev/null)"
  status=$?
  if [[ "${status}" -ne 0 || -z "${document}" ]]; then
    printf '%s\n' "${EVALUATION_FAILED}"
    return 0
  fi

  if ! printf '%s' "${document}" | python3 -c '
import json, sys
document = json.load(sys.stdin)
for identity in sorted(item["Identity"] for item in document.get("Items", {}).get(sys.argv[1], [])):
    if identity.strip():
        sys.stdout.write(identity + "\n")
' "$2"; then
    printf '%s\n' "${EVALUATION_FAILED}"
  fi
}

project_name() {
  local base="${1##*[/\\]}"
  printf '%s' "${base%.csproj}"
}

# --- The two comparisons, as functions -------------------------------------------------
#
# Extracted so the negative controls in section 8 run the SAME comparison the real
# projects run, rather than a second implementation of it. A control that reimplemented
# the comparison would only prove the control works.

# $1 project path, $2 accepted comma-separated reference set.
# Sets EDGES_ACTUAL. Returns 0 when the evaluated set equals the accepted set.
EDGES_ACTUAL=""
edges_match() {
  EDGES_ACTUAL="$(msbuild_items "$1" ProjectReference \
    | sed -E 's|.*[/\\]||; s|\.csproj$||' | sort | paste -sd, -)"
  [[ "${EDGES_ACTUAL}" == "$2" ]]
}

# $1 project path, $2 "yes" when doc 115 allows the engine dependency.
# Sets GODOT_EVALUATED, GODOT_LOCKED, and GODOT_UNPROVED. Returns 0 when the project's
# engine dependency matches what doc 115 allows for it.
#
# GODOT_UNPROVED is the third outcome, and it is deliberately not folded into the
# mismatch return. The evaluated package list is obtained first and checked on its own
# before it is filtered, because a failed MSBuild evaluation yields an empty list and an
# empty list is exactly what a project with no Godot dependency looks like - every "must
# not reference Godot" row would otherwise pass without anything having been evaluated.
# Two independent mechanisms cover that, and both are kept on purpose:
#
#   - msbuild_items emits EVALUATION_FAILED rather than nothing, so a discarded exit
#     status still produces a value that matches no accepted set. This also covers a
#     zero-exit-but-empty document and a python parse failure, and it protects § 3.
#   - the sentinel is then detected here by name and reported as "unproved" rather than
#     as a Godot verdict, so § 4 says the boundary was not tested instead of implying it
#     was tested and passed.
#
# Only after the list is known good is it filtered for Godot. At that point grep's exit 1
# genuinely means "no Godot package", which is the outcome this row is testing for, so the
# `|| true` there is not masking anything.
GODOT_EVALUATED=""
GODOT_LOCKED=""
GODOT_UNPROVED="no"
godot_matches() {
  local project="$1" allowed="$2"
  local directory="${REPO_ROOT}/$(dirname "${project}")"
  local lock_file="${directory}/packages.lock.json"
  local evaluated_packages package_probe_status=0

  GODOT_UNPROVED="no"

  # msbuild_items returns 0 and reports failure in-band via the sentinel, but its status is
  # still checked: if a future edit gives it a nonzero exit, that must fail the gate rather
  # than silently become an empty package list again.
  evaluated_packages="$(msbuild_items "${project}" PackageReference)" || package_probe_status=$?

  # The sentinel is matched deliberately: a project whose items could not be evaluated must
  # not read as "has no Godot dependency".
  GODOT_EVALUATED="$(printf '%s\n' "${evaluated_packages}" \
    | grep -iE "^(Godot|${EVALUATION_FAILED})" || true)"

  if [[ "${package_probe_status}" -ne 0 ]]; then
    GODOT_UNPROVED="yes"
    GODOT_EVALUATED="msbuild_items exited ${package_probe_status}"
    return 2
  fi
  if [[ "${GODOT_EVALUATED}" == *"${EVALUATION_FAILED}"* ]]; then
    GODOT_UNPROVED="yes"
    return 2
  fi

  GODOT_LOCKED=""
  if [[ -f "${lock_file}" ]]; then
    GODOT_LOCKED="$(grep -oE '"Godot[A-Za-z.]*"' "${lock_file}" | sort -u | tr -d '"' | paste -sd, - || true)"
  fi

  if [[ "${allowed}" == "yes" ]]; then
    [[ -n "${GODOT_EVALUATED}" && "${GODOT_LOCKED}" == *GodotSharp* ]]
  else
    [[ -z "${GODOT_EVALUATED}" && -z "${GODOT_LOCKED}" ]]
  fi
}

# Reads one field of an EXPECTED_PROJECTS row, and fails loudly when the row is absent.
# A control that silently tested nothing because a path was renamed would be worse than
# no control, so the lookup is required to succeed.
accepted_field() {
  local wanted="$1" field="$2" entry project refs godot
  for entry in "${EXPECTED_PROJECTS[@]}"; do
    IFS='|' read -r project refs godot <<<"${entry}"
    if [[ "${project}" == "${wanted}" ]]; then
      case "${field}" in
        refs) printf '%s' "${refs}" ;;
        godot) printf '%s' "${godot}" ;;
      esac
      return 0
    fi
  done
  return 1
}

echo "=== 1. accepted repository layout (VER-FND-001-003)"
for path in "${EXPECTED_PATHS[@]}"; do
  if [[ -e "${REPO_ROOT}/${path}" ]]; then
    pass "exists: ${path}"
  else
    fail "missing prescribed path: ${path}"
  fi
done

echo
echo "=== 2. solution contains exactly the accepted projects (VER-FND-001-003)"
actual_solution="$(cd "${REPO_ROOT}" && dotnet sln MechaMiner.sln list \
  | grep -E '\.csproj$' | tr '\\' '/' | sort)"
expected_solution="$(printf '%s\n' "${EXPECTED_PROJECTS[@]}" | cut -d'|' -f1 | sort)"
if [[ "${actual_solution}" == "${expected_solution}" ]]; then
  pass "MechaMiner.sln references exactly the ${#EXPECTED_PROJECTS[@]} accepted projects"
else
  fail "MechaMiner.sln project set differs from the accepted decomposition"
  diff <(printf '%s\n' "${expected_solution}") <(printf '%s\n' "${actual_solution}") || true
fi

echo
echo "=== 3. project reference edges match the accepted boundary (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project expected_refs _godot <<<"${entry}"
  if edges_match "${project}" "${expected_refs}"; then
    pass "$(project_name "${project}") -> [${expected_refs}]"
  else
    fail "$(project_name "${project}") references [${EDGES_ACTUAL}], accepted set is [${expected_refs}]"
  fi
done

echo
echo "=== 4. only game/ may reference Godot (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  if godot_matches "${project}" "${godot_allowed}"; then
    if [[ "${godot_allowed}" == "yes" ]]; then
      pass "$(project_name "${project}") references Godot as accepted (locked: ${GODOT_LOCKED})"
    else
      pass "$(project_name "${project}") has no Godot dependency"
    fi
  elif [[ "${GODOT_UNPROVED}" == "yes" ]]; then
    # The evaluation itself did not happen, which is a third outcome and not a verdict on
    # this row either way. Reported before the accepted/forbidden branches below so that a
    # project whose items could not be read is never described as "must not reference
    # Godot ... (evaluated: none)" - "none" would read as an answer, and there was none.
    fail "$(project_name "${project}"): PackageReference evaluation failed (${GODOT_EVALUATED}); the Godot boundary is unproved for this project, which is not the same as satisfied"
    continue
  elif [[ "${godot_allowed}" == "yes" ]]; then
    fail "$(project_name "${project}") must reference Godot but no Godot package is evaluated or locked"
  else
    fail "$(project_name "${project}") must not reference Godot (evaluated: ${GODOT_EVALUATED:-none}, locked: ${GODOT_LOCKED:-none})"
  fi
done

# Runs a recursive grep whose absence-of-match is the passing outcome, and distinguishes
# grep's three exit classes instead of collapsing two of them.
#
#   0  matches found        -> the prohibition is violated
#   1  no matches           -> the prohibition holds
#   2+ grep itself failed   -> nothing was searched, so the prohibition is UNPROVED
#
# `grep ... 2>/dev/null || true` conflated 1 and 2+: a missing search root, an unreadable
# tree, or any other grep error produced an empty result, and the empty result took the
# pass branch. That is the same defect as the GDScript check below, in two more places.
# Also asserts that the search roots exist, because grep over a nonexistent directory is
# an error the gate must not absorb.
assert_absent_pattern() {
  local description="$1"
  local pattern="$2"
  local include="$3"
  shift 3
  local roots=("$@")

  local root
  for root in "${roots[@]}"; do
    if [[ ! -d "${REPO_ROOT}/${root}" ]]; then
      fail "${description}: search root '${root}' does not exist, so this prohibition was not searched for"
      return
    fi
  done

  local matches
  local grep_status=0
  matches="$(cd "${REPO_ROOT}" && grep -rlE "${pattern}" --include="${include}" "${roots[@]}" 2>&1)" \
    || grep_status=$?

  if [[ "${grep_status}" -eq 0 ]]; then
    fail "${description}: ${matches}"
  elif [[ "${grep_status}" -eq 1 ]]; then
    pass "${description}: no match in ${roots[*]}"
  else
    fail "${description}: grep exited ${grep_status}, so nothing was searched and the rule is unproved: ${matches}"
  fi
}

echo
echo "=== 5. no pure project references MechaMiner.Game (VER-FND-001-004)"
assert_absent_pattern \
  "no project references MechaMiner.Game" \
  'ProjectReference[^>]*MechaMiner\.Game\.csproj' \
  '*.csproj' \
  src tests game

echo
echo "=== 6. no Godot types outside game/ (VER-FND-001-004)"
assert_absent_pattern \
  "no C# file under src/ or tests/ imports Godot" \
  '(^|[^A-Za-z.])using[[:space:]]+Godot([;.]|$)' \
  '*.cs' \
  src tests

echo
echo "=== 7. no GDScript in the repository (VER-FND-001-005)"
#
# "No production GDScript" is one of AGENTS.md § Nonnegotiable architecture's hard
# prohibitions (TR-FND-002), and this gate could not enforce it. Two separate defects,
# both of which made a violation pass:
#
#   - `|| true` discarded git's exit status, so a git failure produced an empty
#     result, and the empty result took the "pass" branch. Under a broken or absent
#     git the prohibition was silently unenforceable.
#   - only tracked paths were consulted, so an untracked .gd file passed - and
#     untracked is precisely the state a newly written file is in.
#
# The candidate set is now tracked plus untracked-but-not-ignored, which is the same
# set format/format-check inspects, and a nonzero git status fails the gate instead of
# emptying it. Ignored paths stay out of scope on purpose: game/.godot/ is an engine
# cache, and a gitignored file is not production content.
#
# The probe is a function so that the negative controls below can drive the identical
# predicate. A gate asserted only against a clean tree proves nothing about its ability
# to fail.

# Emits a verdict word on line 1 - clean, violation, or unreadable - and detail after.
gdscript_probe() {
  local probe_status=0
  local probe_output
  probe_output="$(cd "${REPO_ROOT}" && git ls-files --cached --others --exclude-standard \
    -- '*.gd' '*.gdshaderinc.gd' 2>&1)" || probe_status=$?

  if [[ "${probe_status}" -ne 0 ]]; then
    printf 'unreadable\ngit ls-files exited %s: %s\n' "${probe_status}" "${probe_output}"
  elif [[ -n "${probe_output}" ]]; then
    printf 'violation\n%s\n' "${probe_output}"
  else
    printf 'clean\n'
  fi
}

gdscript_verdict="$(gdscript_probe)"
gdscript_kind="$(printf '%s\n' "${gdscript_verdict}" | head -1)"
gdscript_detail="$(printf '%s\n' "${gdscript_verdict}" | tail -n +2)"

if [[ "${gdscript_kind}" == "unreadable" ]]; then
  fail "could not enumerate GDScript candidates, so the no-GDScript rule is unproved: ${gdscript_detail}"
elif [[ "${gdscript_kind}" == "violation" ]]; then
  fail "GDScript is not permitted (TR-FND-002), tracked or untracked: ${gdscript_detail}"
else
  pass "no .gd file is tracked, and none is present untracked in the working tree"
fi

echo
echo "=== 7a. negative controls: the no-GDScript gate can actually fail"
readonly GDSCRIPT_FIXTURE="${REPO_ROOT}/game/DeliberatelyForbiddenGdscriptFixture.gd"

remove_gdscript_fixture() {
  rm -f "${GDSCRIPT_FIXTURE}"
}

if [[ "${gdscript_kind}" == "unreadable" ]]; then
  # The controls drive the same enumeration § 7 just failed to perform, so running them
  # would only restate that failure in three more places. § 7 already failed the gate.
  echo "      NOT RUN: § 7 could not enumerate at all, so these controls cannot mean"
  echo "      anything in this environment. The gate is already failing above."
else
  trap remove_gdscript_fixture EXIT

  # Control 1: an untracked .gd file. This is exactly the case the old gate passed.
  cat >"${GDSCRIPT_FIXTURE}" <<'GDFIXTURE'
# Deliberately forbidden GDScript, written and removed by build/verify-architecture.sh.
extends Node
GDFIXTURE

  control_kind="$(gdscript_probe | head -1)"
  if [[ "${control_kind}" == "violation" ]]; then
    pass "an untracked .gd file is detected as a violation"
  else
    fail "an untracked .gd file was reported as '${control_kind}'; the gate cannot see untracked GDScript"
  fi

  # Control 2: the same fixture, with git unable to answer. The gate must report that
  # it could not tell, and must never report a clean tree.
  control_kind="$(GIT_DIR=/nonexistent/verify-architecture-broken.git gdscript_probe | head -1)"
  if [[ "${control_kind}" == "unreadable" ]]; then
    pass "a git failure is reported as unreadable, not as a clean tree"
  else
    fail "with a broken git the probe reported '${control_kind}'; a failed enumeration must not pass"
  fi

  remove_gdscript_fixture
  trap - EXIT

  # The fixture must be gone. The comparison is against § 7's own verdict rather than
  # against "clean", so that a pre-existing .gd file in the tree - which § 7 already
  # failed on - is not reported a second time as a fixture-cleanup failure.
  control_kind="$(gdscript_probe | head -1)"
  if [[ "${control_kind}" == "${gdscript_kind}" ]]; then
    pass "the fixture was removed; the probe reports '${control_kind}' again, as it did in § 7"
  else
    fail "the GDScript fixture was not removed: probe reports '${control_kind}', § 7 saw '${gdscript_kind}'"
  fi
fi

echo
echo "=== 8. the boundary comparisons above can actually fail (VER-FND-009-013)"
#
# Sections 3 and 4 only ever ran against compliant input, so nothing showed they were
# capable of reporting a violation. MechaMiner.Diagnostics made that gap matter: it is a
# sixth src/ project whose accepted row is the strictest in the repository - ".NET base
# libraries only", Godot "No", zero references, a dependency leaf every other project may
# reference without a cycle - and that row is the only thing keeping the leaf a leaf.
#
# Each fixture under build/policy-fixtures/architecture/ is a project file named
# MechaMiner.Diagnostics.csproj carrying exactly one violation of that row. They are fed
# through edges_match and godot_matches, the same functions sections 3 and 4 call, and each
# must report a difference. The accepted row is read out of EXPECTED_PROJECTS rather than
# hardcoded here, so a future task that legitimately gives Diagnostics an edge updates one
# place and these controls keep testing the row that is actually accepted.

readonly DIAGNOSTICS_PROJECT="src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj"
readonly CONTROL_ROOT="build/policy-fixtures/architecture"

# "<fixture directory>|<the project the fixture references>|<what it injects>"
#
# The middle field is the evaluated reference set the fixture must produce. Asserting it
# closes a both-sides-absent comparison: the compliant control below compares "" against
# an accepted set that is also "" today, so it would pass against an msbuild_items that
# had broken into returning nothing for every input. Requiring each edge fixture to come
# back naming the project it references proves the evaluation actually happened.
readonly EDGE_CONTROLS=(
  "edge-content|MechaMiner.Content|a reference to MechaMiner.Content"
  "edge-simulation|MechaMiner.Simulation|a reference to MechaMiner.Simulation"
  "edge-game|MechaMiner.Game|a reference to MechaMiner.Game (the reverse Godot edge)"
)
# Guards against an empty control set silently proving nothing, the way an unquoted or
# mistyped array expansion would. The loop must run this many times.
readonly EXPECTED_EDGE_CONTROLS=3

if ! diagnostics_accepted_refs="$(accepted_field "${DIAGNOSTICS_PROJECT}" refs)"; then
  fail "negative control cannot run: ${DIAGNOSTICS_PROJECT} has no EXPECTED_PROJECTS row"
elif ! diagnostics_accepted_godot="$(accepted_field "${DIAGNOSTICS_PROJECT}" godot)"; then
  fail "negative control cannot run: ${DIAGNOSTICS_PROJECT} has no EXPECTED_PROJECTS row"
else
  # Positive control first. Every control below passes by producing a DIFFERENCE, so a
  # comparison that had broken into reporting a difference for every input would pass all
  # of them. This is the one input that must come back equal.
  control="${CONTROL_ROOT}/compliant/MechaMiner.Diagnostics.csproj"
  if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
    fail "negative-control fixture missing: ${control}"
  elif edges_match "${control}" "${diagnostics_accepted_refs}"; then
    pass "control: a compliant Diagnostics project compares equal to [${diagnostics_accepted_refs}]"
  else
    fail "control: a compliant Diagnostics project reported [${EDGES_ACTUAL}]; the comparison reports a difference for compliant input, so every negative control below is meaningless"
  fi

  edge_controls_run=0
  for entry in "${EDGE_CONTROLS[@]}"; do
    IFS='|' read -r fixture referenced injected <<<"${entry}"
    control="${CONTROL_ROOT}/${fixture}/MechaMiner.Diagnostics.csproj"
    if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
      fail "negative-control fixture missing: ${control}"
      continue
    fi
    edge_controls_run=$((edge_controls_run + 1))
    if edges_match "${control}" "${diagnostics_accepted_refs}"; then
      fail "control: Diagnostics with ${injected} was NOT rejected; § 3 accepted [${EDGES_ACTUAL}] against [${diagnostics_accepted_refs}]"
    elif [[ "${EDGES_ACTUAL}" != "${referenced}" ]]; then
      # It was rejected, but not for the reason the control exists to prove. An empty
      # evaluated set would also be "rejected", and would mean MSBuild evaluated nothing.
      fail "control: Diagnostics with ${injected} was rejected, but § 3 evaluated [${EDGES_ACTUAL}] instead of [${referenced}]; the rejection does not prove the injected edge was seen"
    else
      pass "control: Diagnostics with ${injected} is rejected (§ 3 saw [${EDGES_ACTUAL}])"
    fi
  done

  if [[ "${edge_controls_run}" -eq "${EXPECTED_EDGE_CONTROLS}" ]]; then
    pass "control: all ${EXPECTED_EDGE_CONTROLS} forbidden-edge controls ran"
  else
    fail "control: ${edge_controls_run} of ${EXPECTED_EDGE_CONTROLS} forbidden-edge controls ran; a control set that shrank proves less than it claims"
  fi

  control="${CONTROL_ROOT}/godot/MechaMiner.Diagnostics.csproj"
  if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
    fail "negative-control fixture missing: ${control}"
  elif godot_matches "${control}" "${diagnostics_accepted_godot}"; then
    fail "control: Diagnostics with a GodotSharp PackageReference was NOT rejected by § 4"
  elif [[ "${GODOT_UNPROVED}" == "yes" ]]; then
    # Rejected, but as "unproved" rather than as "has Godot". An unevaluable fixture would
    # also be "not accepted", and would prove only that MSBuild could not read it.
    fail "control: Diagnostics with a GodotSharp PackageReference was rejected as unproved (${GODOT_EVALUATED}), so § 4's Godot detection was never exercised"
  elif [[ "${GODOT_EVALUATED}" != *[Gg]odot* ]]; then
    # Rejected, but not for the reason the control exists to prove, mirroring the
    # edge-control check above: § 4 must have actually seen the injected Godot package.
    fail "control: Diagnostics with a GodotSharp PackageReference was rejected, but § 4 evaluated [${GODOT_EVALUATED}] and saw no Godot package; the rejection does not prove the injected dependency was seen"
  else
    pass "control: Diagnostics with a GodotSharp PackageReference is rejected (§ 4 saw [${GODOT_EVALUATED}])"
  fi
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-architecture: PASS"
  exit 0
fi
echo "verify-architecture: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
