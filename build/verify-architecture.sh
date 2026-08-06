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
  "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj||no"
  "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Tools/MechaMiner.Tools.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj|MechaMiner.Content|no"
  "tests/MechaMiner.Diagnostics.Tests/MechaMiner.Diagnostics.Tests.csproj|MechaMiner.Diagnostics|no"
  "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj|MechaMiner.Simulation|no"
  "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj|MechaMiner.Persistence|no"
  "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "game/MechaMiner.Game.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|yes"
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
  pass "MechaMiner.sln references exactly the ${#EXPECTED_PROJECTS[@]} accepted projects"
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

  godot_packages="$(msbuild_items "${project}" PackageReference | grep -i '^Godot' || true)"
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

echo
echo "=== 5. no pure project references MechaMiner.Game (VER-FND-001-004)"
reverse_edges="$(cd "${REPO_ROOT}" && grep -rlE 'ProjectReference[^>]*MechaMiner\.Game\.csproj' \
  --include='*.csproj' src tests game 2>/dev/null || true)"
if [[ -z "${reverse_edges}" ]]; then
  pass "no project references MechaMiner.Game"
else
  fail "reverse Godot edge: ${reverse_edges}"
fi

echo
echo "=== 6. no Godot types outside game/ (VER-FND-001-004)"
stray_godot="$(cd "${REPO_ROOT}" && grep -rlE '(^|[^A-Za-z.])using[[:space:]]+Godot([;.]|$)' \
  --include='*.cs' src tests 2>/dev/null || true)"
if [[ -z "${stray_godot}" ]]; then
  pass "no C# file under src/ or tests/ imports Godot"
else
  fail "Godot import outside game/: ${stray_godot}"
fi

echo
echo "=== 7. no GDScript in the repository (VER-FND-001-005)"
gdscript="$(cd "${REPO_ROOT}" && git ls-files '*.gd' '*.gdshaderinc.gd' 2>/dev/null || true)"
if [[ -z "${gdscript}" ]]; then
  pass "no .gd source file is tracked"
else
  fail "GDScript is not permitted (TR-FND-002): ${gdscript}"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-architecture: PASS"
  exit 0
fi
echo "verify-architecture: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
