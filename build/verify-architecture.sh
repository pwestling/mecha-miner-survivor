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
# TASK-FND-009-001 replaces the reference-graph portion with an architecture test
# inside the pure test projects. This script remains the FND-001 gate until then.
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
  # doc 100 § Continuous integration requires a pull-request job; FND-005 is that
  # job and this is the only file that is it. Listing it here is what makes "CI
  # exists" a checked fact rather than a claim: deleting or renaming the workflow
  # un-gates every gate at once, silently and with no red build anywhere, which is
  # the one failure mode no gate inside the workflow can catch.
  ".github/workflows/fast.yml"
  "game/project.godot"
  "game/MechaMiner.Game.csproj"
  "game/scenes"
  "game/shaders"
  "game/presentation"
  "src/MechaMiner.Simulation"
  "src/MechaMiner.Content"
  "src/MechaMiner.Persistence"
  "src/MechaMiner.Tools"
  "tests/MechaMiner.Simulation.Tests"
  "tests/MechaMiner.Content.Tests"
  "tests/MechaMiner.Persistence.Tests"
  "tests/MechaMiner.Game.Tests"
  "tests/verification"
  "content"
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
  "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Tools/MechaMiner.Tools.csproj|MechaMiner.Content,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj|MechaMiner.Content|no"
  "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj|MechaMiner.Simulation|no"
  "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj|MechaMiner.Persistence|no"
  "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj|MechaMiner.Content,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "game/MechaMiner.Game.csproj|MechaMiner.Content,MechaMiner.Persistence,MechaMiner.Simulation|yes"
)

msbuild_items() {
  # $1 project, $2 item name. Prints one Identity per line, sorted.
  dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getItem:$2" 2>/dev/null | python3 -c '
import json, sys
document = json.load(sys.stdin)
for identity in sorted(item["Identity"] for item in document.get("Items", {}).get(sys.argv[1], [])):
    if identity.strip():
        sys.stdout.write(identity + "\n")
' "$2"
}

project_name() {
  local base="${1##*[/\\]}"
  printf '%s' "${base%.csproj}"
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
  pass "MechaMiner.sln references exactly the 9 accepted projects"
else
  fail "MechaMiner.sln project set differs from the accepted decomposition"
  diff <(printf '%s\n' "${expected_solution}") <(printf '%s\n' "${actual_solution}") || true
fi

echo
echo "=== 3. project reference edges match the accepted boundary (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project expected_refs _godot <<<"${entry}"
  actual_refs="$(msbuild_items "${project}" ProjectReference \
    | sed -E 's|.*[/\\]||; s|\.csproj$||' | sort | paste -sd, -)"
  if [[ "${actual_refs}" == "${expected_refs}" ]]; then
    pass "$(project_name "${project}") -> [${expected_refs}]"
  else
    fail "$(project_name "${project}") references [${actual_refs}], accepted set is [${expected_refs}]"
  fi
done

echo
echo "=== 4. only game/ may reference Godot (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  directory="${REPO_ROOT}/$(dirname "${project}")"

  # `msbuild_items ... | grep -i '^Godot' || true` used to cover the whole pipeline, so a
  # failed MSBuild evaluation produced an empty package list, and an empty package list
  # is exactly what a project with no Godot dependency looks like. Every "must not
  # reference Godot" row below would then pass without anything having been evaluated.
  # The evaluation is now checked on its own before its output is filtered.
  evaluated_packages=""
  package_probe_status=0
  evaluated_packages="$(msbuild_items "${project}" PackageReference)" || package_probe_status=$?
  if [[ "${package_probe_status}" -ne 0 ]]; then
    fail "$(project_name "${project}"): PackageReference evaluation failed (exit ${package_probe_status}); the Godot boundary is unproved for this project, which is not the same as satisfied"
    continue
  fi

  # Filtering an already-validated in-memory list: here grep's exit 1 genuinely means
  # "no Godot package", which is the outcome this row is testing for.
  godot_packages="$(printf '%s\n' "${evaluated_packages}" | grep -i '^Godot' || true)"

  lock_file="${directory}/packages.lock.json"
  godot_locked=""
  if [[ -f "${lock_file}" ]]; then
    godot_locked="$(grep -oE '"Godot[A-Za-z.]*"' "${lock_file}" | sort -u | tr -d '"' | paste -sd, - || true)"
  fi

  if [[ "${godot_allowed}" == "yes" ]]; then
    if [[ -n "${godot_packages}" && "${godot_locked}" == *GodotSharp* ]]; then
      pass "$(project_name "${project}") references Godot as accepted (locked: ${godot_locked})"
    else
      fail "$(project_name "${project}") must reference Godot but no Godot package is evaluated or locked"
    fi
  else
    if [[ -z "${godot_packages}" && -z "${godot_locked}" ]]; then
      pass "$(project_name "${project}") has no Godot dependency"
    else
      fail "$(project_name "${project}") must not reference Godot (evaluated: ${godot_packages:-none}, locked: ${godot_locked:-none})"
    fi
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
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-architecture: PASS"
  exit 0
fi
echo "verify-architecture: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
