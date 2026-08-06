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
# This is a real assertion, not a review aid: the Godot boundary is read from the
# RESOLVED compile-time reference set of every project - what the compiler is
# actually handed, after MSBuild has evaluated imports, central package
# management, SDK injection and assembly resolution - plus the committed NuGet
# lock files for transitive package routes. Every mismatch exits nonzero.
#
# The boundary is derived from how MSBuild can acquire a reference at all, not
# from the routes this script happened to check first. Reading PackageReference
# alone missed a raw <Reference> with a HintPath, an <Import> of a props file that
# adds one, and anything SDK- or central-package-injected; all of those land in
# ReferencePath, so ReferencePath is what is asserted.
#
# TASK-FND-009-001 replaces the reference-graph portion with an architecture test
# inside the pure test projects. This script remains the FND-001 gate until then.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure. There is deliberately no exit 1: a wrapper that returns
# it has leaked an unclassified failure from an underlying tool. `set -e` is
# therefore NOT used - it would abort on the first failing helper, return 1, and
# skip every remaining section - matching every other gate script in build/.
# Helpers that can fail return nonzero and their callers count a failure and
# continue, so one broken project cannot hide the checks that follow it.

set -uo pipefail

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
  # $1 project, $2 item name or comma-separated list of them, $3 optional target
  # to run first. Prints one Identity per line, sorted.
  #
  # Returns nonzero when MSBuild cannot evaluate the project, or its output is not
  # the JSON expected, or it does not contain every requested item name. Callers
  # MUST convert that into a counted failure: an unparseable evaluation used to
  # crash python under `set -e` and abort the whole script with exit 1, which both
  # leaked an unclassified exit class and skipped every later section.
  local project="$1" item="$2" target="${3:-}"
  local -a command=(dotnet msbuild "${REPO_ROOT}/${project}" -nologo "-getItem:${item}")
  if [[ -n "${target}" ]]; then
    # BuildProjectReferences=false keeps reference resolution from compiling the
    # referenced projects. Without it, any compile error anywhere in the graph makes
    # this evaluation fail for every project downstream of it, and the reference
    # boundary becomes unverifiable for reasons unrelated to the boundary. Godot
    # arrives as an assembly or package reference, never as a ProjectReference, so
    # nothing this section asserts depends on those projects having been built.
    command+=("-t:${target}" "-p:BuildProjectReferences=false")
  fi

  local output
  output="$("${command[@]}" 2>/dev/null)" || return 1
  printf '%s' "${output}" | python3 -c '
import json, sys
try:
    document = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
items = document.get("Items")
if not isinstance(items, dict):
    sys.exit(1)
for name in sys.argv[1].split(","):
    # A requested item name that is absent means the MSBuild contract changed;
    # that must fail loudly rather than read as "no such reference".
    if name not in items:
        sys.exit(1)
    for identity in sorted(entry.get("Identity", "") for entry in items[name]):
        if identity.strip():
            sys.stdout.write(identity + "\n")
' "${item}"
}

msbuild_property() {
  # $1 project, $2 property name. Prints the evaluated value.
  # Returns nonzero when MSBuild cannot evaluate the project or the value is
  # empty, so a caller never compares against a silently empty string.
  local output
  output="$(dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getProperty:$2" 2>/dev/null)" || return 1
  output="${output//[$'\r\n']/}"
  [[ -n "${output}" ]] || return 1
  printf '%s' "${output}"
}

godot_assembly_names() {
  # Filters a list of reference identities or paths on stdin down to the Godot
  # assemblies among them, as a comma-separated list. Matches the file name so a
  # bare assembly name and a full HintPath are treated alike.
  sed -E 's|.*[/\\]||; s|\.dll$||' \
    | grep -iE '^Godot([A-Za-z0-9.]*)?$' \
    | sort -u | paste -sd, - || true
}

project_name() {
  local base="${1##*[/\\]}"
  printf '%s' "${base%.*proj}"
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
#
# The project set is compared UNFILTERED. Selecting only *.csproj before comparing
# turned the C#-only rule into a filter instead of an assertion: a tenth project
# written in another language (a .vbproj referencing Godot through a bare
# <Reference>) was dropped from the actual set, so the comparison still reported
# "exactly the 9 accepted projects" while ten projects built. Every project the
# solution lists must be one of the accepted paths, and must be a .csproj.
actual_solution="$(cd "${REPO_ROOT}" && dotnet sln MechaMiner.sln list \
  | grep -E '\.[A-Za-z]+proj$' | tr '\\' '/' | sort)"
expected_solution="$(printf '%s\n' "${EXPECTED_PROJECTS[@]}" | cut -d'|' -f1 | sort)"
if [[ -z "${actual_solution}" ]]; then
  fail "could not read any project from MechaMiner.sln"
elif [[ "${actual_solution}" == "${expected_solution}" ]]; then
  pass "MechaMiner.sln references exactly the 9 accepted projects"
else
  fail "MechaMiner.sln project set differs from the accepted decomposition"
  diff <(printf '%s\n' "${expected_solution}") <(printf '%s\n' "${actual_solution}") || true
fi

non_csharp="$(printf '%s\n' "${actual_solution}" | grep -vE '\.csproj$' || true)"
if [[ -z "${non_csharp}" ]]; then
  pass "every project in MechaMiner.sln is a C# project (TR-FND-002)"
else
  fail "non-C# project in MechaMiner.sln (TR-FND-002 permits C# only): $(printf '%s' "${non_csharp}" | paste -sd' ' -)"
fi

echo
echo "=== 3. every accepted project builds in every solution configuration (VER-FND-001-003)"
#
# Solution membership is not the same as being built. Deleting a single
# "<GUID>.Debug|Any CPU.Build.0" line leaves `dotnet sln list` reporting nine
# projects while `dotnet build` compiles eight, so section 2 alone reports success
# for a solution that silently stops building a project. Assert the build flag
# per accepted project per solution configuration. Only the presence of the flag
# is asserted here, not which configuration it maps to.
build_flag_report="$(cd "${REPO_ROOT}" && python3 - MechaMiner.sln \
  "$(printf '%s\n' "${EXPECTED_PROJECTS[@]}" | cut -d'|' -f1)" <<'PY'
import re, sys

solution = open(sys.argv[1]).read()
expected = [line for line in sys.argv[2].splitlines() if line.strip()]


def section(name):
    # Skips the GlobalSection header line itself, so its "= preSolution" suffix is
    # not mistaken for a configuration entry.
    found = re.search(
        r"GlobalSection\(%s\)[^\n]*\n(.*?)EndGlobalSection" % name, solution, re.S)
    return found.group(1) if found else ""


configurations = [
    name for name in (
        line.split("=")[0].strip()
        for line in section("SolutionConfigurationPlatforms").splitlines()
        if "=" in line
    ) if name
]
project_configurations = section("ProjectConfigurationPlatforms")

# Path -> GUID for every project entry, normalised to forward slashes.
guids = {
    path.replace("\\", "/"): guid
    for _name, path, guid in re.findall(
        r'^Project\("\{[^}]+\}"\)\s*=\s*"([^"]+)",\s*"([^"]+)",\s*"\{([^}]+)\}"',
        solution, re.M)
}

if not configurations:
    print("ERROR|the solution declares no configuration, so this check would be vacuous")
    raise SystemExit(0)

for path in expected:
    guid = guids.get(path)
    if guid is None:
        print("FAIL|%s is not present in the solution at all" % path)
        continue
    missing = [
        configuration for configuration in configurations
        if "{%s}.%s.Build.0" % (guid, configuration) not in project_configurations
    ]
    if missing:
        print("FAIL|%s has no build flag for: %s" % (path, ", ".join(missing)))
    else:
        print("OK|%s builds in all %d configurations" % (path, len(configurations)))
PY
)" || build_flag_report="ERROR|the solution configuration section could not be parsed"

if [[ -z "${build_flag_report}" ]]; then
  fail "the solution build-flag check produced no result for any accepted project"
fi
while IFS='|' read -r verdict detail; do
  [[ -n "${verdict}" ]] || continue
  case "${verdict}" in
    OK) pass "${detail}" ;;
    *) fail "${detail}" ;;
  esac
done <<<"${build_flag_report}"

echo
echo "=== 4. project reference edges match the accepted boundary (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project expected_refs _godot <<<"${entry}"
  if ! project_references="$(msbuild_items "${project}" ProjectReference)"; then
    fail "$(project_name "${project}") could not be evaluated by MSBuild (missing, malformed, or unrestored project)"
    continue
  fi
  actual_refs="$(printf '%s' "${project_references}" \
    | sed -E 's|.*[/\\]||; s|\.[A-Za-z]+proj$||' | sort | paste -sd, -)"
  if [[ "${actual_refs}" == "${expected_refs}" ]]; then
    pass "$(project_name "${project}") -> [${expected_refs}]"
  else
    fail "$(project_name "${project}") references [${actual_refs}], accepted set is [${expected_refs}]"
  fi
done

echo
echo "=== 5. each accepted project keeps its accepted assembly identity (VER-FND-001-003)"
#
# A project renamed only via <AssemblyName> keeps its accepted file path, so every
# path-based check above still passes while the assembly the boundary is written
# about no longer exists under that name.
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs _godot <<<"${entry}"
  expected_name="$(project_name "${project}")"
  if ! actual_name="$(msbuild_property "${project}" AssemblyName)"; then
    fail "${expected_name}: AssemblyName could not be evaluated"
  elif [[ "${actual_name}" == "${expected_name}" ]]; then
    pass "${expected_name} builds as assembly ${actual_name}"
  else
    fail "${expected_name} builds as assembly '${actual_name}'; the accepted boundary names '${expected_name}'"
  fi
done

echo
echo "=== 6. only game/ may reference Godot (VER-FND-001-004)"
#
# Three independent signals, because each covers routes the others miss:
#
#   resolved  ReferencePath after ResolveAssemblyReferences - the actual compile
#             line. This is the authoritative signal: it covers PackageReference,
#             a raw <Reference> with a HintPath, a <Reference> contributed by an
#             <Import>ed props/targets file, a Directory.Build.* contribution,
#             central package management including GlobalPackageReference, and
#             SDK-injected references, because all of them are resolved before the
#             compiler is invoked.
#   declared  Reference and PackageReference items, so a declared dependency is
#             still reported when resolution fails (a broken HintPath would
#             otherwise leave the intent invisible).
#   locked    the committed lock file, which covers transitive package routes that
#             no single project declares.
#
# Reading PackageReference alone was the original defect: a raw <Reference> is not
# a PackageReference and contributes nothing to packages.lock.json, so a project
# could expose a Godot type on its public API surface with this section reporting
# "has no Godot dependency".
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  directory="${REPO_ROOT}/$(dirname "${project}")"
  name="$(project_name "${project}")"

  if ! resolved_references="$(msbuild_items "${project}" ReferencePath ResolveAssemblyReferences)"; then
    fail "${name}: the resolved compile-time reference set could not be evaluated, so its Godot boundary is unverified"
    continue
  fi
  godot_resolved="$(printf '%s\n' "${resolved_references}" | godot_assembly_names)"

  if ! declared_references="$(msbuild_items "${project}" Reference,PackageReference)"; then
    fail "${name}: declared Reference/PackageReference items could not be evaluated"
    continue
  fi
  godot_declared="$(printf '%s\n' "${declared_references}" | godot_assembly_names)"

  lock_file="${directory}/packages.lock.json"
  godot_locked=""
  if [[ -f "${lock_file}" ]]; then
    godot_locked="$(grep -oE '"Godot[A-Za-z.]*"' "${lock_file}" | tr -d '"' | sort -u | paste -sd, - || true)"
  fi

  if [[ "${godot_allowed}" == "yes" ]]; then
    if [[ "${godot_resolved}" == *GodotSharp* && "${godot_locked}" == *GodotSharp* ]]; then
      pass "${name} references Godot as accepted (resolved: ${godot_resolved}, locked: ${godot_locked})"
    else
      fail "${name} must reference Godot but it is not on the resolved compile line and/or not locked (resolved: ${godot_resolved:-none}, locked: ${godot_locked:-none})"
    fi
  else
    if [[ -z "${godot_resolved}" && -z "${godot_declared}" && -z "${godot_locked}" ]]; then
      pass "${name} has no Godot dependency (resolved compile-time reference set checked)"
    else
      fail "${name} must not reference Godot (resolved: ${godot_resolved:-none}, declared: ${godot_declared:-none}, locked: ${godot_locked:-none})"
    fi
  fi
done

echo
echo "=== 7. no pure project references MechaMiner.Game (VER-FND-001-004)"
reverse_edges="$(cd "${REPO_ROOT}" && grep -rlE 'ProjectReference[^>]*MechaMiner\.Game\.csproj' \
  --include='*.csproj' src tests game 2>/dev/null || true)"
if [[ -z "${reverse_edges}" ]]; then
  pass "no project references MechaMiner.Game"
else
  fail "reverse Godot edge: ${reverse_edges}"
fi

echo
echo "=== 8. no Godot types in C# source outside game/ (VER-FND-001-004)"
#
# Matches the Godot namespace as a token in ANY position, not only after `using`.
# The previous expression was `using[[:space:]]+Godot([;.]|$)`, which tested for
# one import spelling while the section title claimed to check for types. Every
# other spelling passed: a fully-qualified `Godot.Vector2` with no import at all,
# `using static Godot.Mathf;`, `using static Godot.GD;`, `using GD2 = Godot;` and
# `using Alias = Godot;`.
#
# Note that matching `Godot[.]` is NOT sufficient: the alias forms
# `using GD2 = Godot;` and `using Alias = Godot;` have no dot after the token. The
# trailing class below is therefore "any non-identifier character or end of line".
# The leading class excludes `.` and identifier characters so that
# `MechaMiner.GodotLike` and `NotGodotish` do not match.
#
# This is a deliberately conservative rule for a boundary whose whole point is
# that these projects never name the engine: a comment or string in a pure project
# that spells `Godot` as a bare token also fails, and should be reworded.
readonly GODOT_TOKEN='(^|[^A-Za-z0-9_.])Godot([^A-Za-z0-9_]|$)'
readonly GODOT_SCAN_ROOTS=(src tests build/policy-fixtures)

# A scan root that has moved would make the search silently cover nothing, so its
# absence is a failure rather than an empty pass.
scan_roots_present=1
for root in "${GODOT_SCAN_ROOTS[@]}"; do
  if [[ ! -d "${REPO_ROOT}/${root}" ]]; then
    fail "Godot source scan root is missing: ${root}/"
    scan_roots_present=0
  fi
done

if [[ "${scan_roots_present}" -eq 1 ]]; then
  # bin/ and obj/ are build outputs regenerated from the sources scanned here, and
  # a stale one must not decide this gate either way.
  stray_godot="$(cd "${REPO_ROOT}" && find "${GODOT_SCAN_ROOTS[@]}" \
    -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print0 2>/dev/null \
    | xargs -0 -r grep -lE "${GODOT_TOKEN}" 2>/dev/null | sort || true)"
  if [[ -z "${stray_godot}" ]]; then
    pass "no C# file under $(printf '%s/ ' "${GODOT_SCAN_ROOTS[@]}")names a Godot type"
  else
    fail "Godot type named outside game/: $(printf '%s' "${stray_godot}" | paste -sd' ' -)"
  fi
fi

echo
echo "=== 9. no GDScript in the repository (VER-FND-001-005)"
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
