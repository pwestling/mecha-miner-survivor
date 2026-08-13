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
# The Godot boundary is asserted TWICE, over two different reference sets, because
# neither set can see what the other catches - § 4 reads what each project
# DECLARES at evaluation time, § 4a reads what it ACTUALLY RESOLVES to on the
# compile line. § 4a's own header explains what each one catches alone; read it
# before deleting either as redundant.
#
# NO RESTORE IS REQUIRED to run this gate. § 4a cannot be measured without
# obj/project.assets.json, and on a checkout that has none it reports NOT MEASURED
# through skip() - a third outcome that is neither pass nor fail - so the gate
# still exits 0 and gate_summary reports the reduced coverage. A plain checkout
# must not fail this gate for lacking a restore, and "I could not measure this" is
# not the same finding as "this is violated".
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
#
# PRECONDITION: `dotnet restore` MUST have run against this checkout first.
#
# Section 6 reads the resolved reference set via `dotnet msbuild
# -getItem:ReferencePath -t:ResolveAssemblyReferences`, and
# ResolveAssemblyReferences cannot run without obj/project.assets.json, which
# only a restore produces. On a plain checkout with no obj/ this script
# therefore exits 4 with NINE failures - one per project in the solution,
# MechaMiner.Game included - all reading "the resolved compile-time reference
# set could not be evaluated". That is the gate failing closed rather than
# passing something it could not measure, and it is working as intended.
#
# Sequence it AFTER restore wherever it is wired. Measured on master 5805908 in
# one fresh worktree on a quiet box, single variable: with zero
# project.assets.json it exits 4 with 9 failures; after `dotnet restore
# MechaMiner.sln --locked-mode` produced 9 assets files, the same tree exits 0
# with 74 assertions.
#
# Known wording defect, recorded rather than silently changed: those nine
# assertions report the THIRD outcome ("I could not measure") when the actual
# condition is the SECOND ("a precondition was not met"). A reader who meets
# "could not be evaluated" on a loaded machine will reach for contention and be
# wrong - that misdiagnosis has already cost one round, twice. The message
# should say that no restore has been performed. The wording is left to the
# FND-001 gate owner because it changes assertion text that round evidence
# quotes verbatim.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

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
  # job and this is the only file that is it. Deleting or renaming the workflow
  # un-gates every gate at once, silently and with no red build anywhere, which is
  # the one failure mode no gate inside the workflow can catch.
  #
  # Listing it here tests the path and nothing else, which is less than an earlier
  # version of this comment claimed. § 8 is where the workflow's content is asserted;
  # this entry only reports the name when the file is gone.
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
  # Filters a list of reference identities, assembly identities or paths on stdin
  # down to the Godot assemblies among them, as a comma-separated list. Matches
  # the last path segment so a bare assembly name and a full HintPath are treated
  # alike. Tabs are treated as separators, so a caller may feed it more than one
  # column per reference.
  tr '\t' '\n' \
    | sed -E 's|.*[/\\]||; s|\.dll$||' \
    | grep -iE '^Godot([A-Za-z0-9.]*)?$' \
    | sort -u | paste -sd, - || true
}

resolved_assembly_identities() {
  # $1 project. Prints "<assembly identity>\t<file name>" for every resolved
  # compile-time reference. Returns nonzero on the same conditions as
  # msbuild_items.
  #
  # The identity is the simple name out of FusionName, which
  # ResolveAssemblyReferences reads from the assembly's own metadata. It therefore
  # does not change when the FILE is renamed, which is the whole point: copying
  # GodotSharp.dll to build/probe/Engine.dll and referencing it as
  # <Reference Include="Engine"> defeated every name-based check while putting the
  # real Godot assembly on a pure project's compile line.
  #
  # The file name is printed ALONGSIDE the identity, not instead of it, so a
  # reference whose identity metadata cannot be read is still matched by name
  # rather than reported as clean.
  local project="$1"
  local output
  output="$(dotnet msbuild "${REPO_ROOT}/${project}" -nologo \
    -getItem:ReferencePath -t:ResolveAssemblyReferences \
    -p:BuildProjectReferences=false 2>/dev/null)" || return 1
  printf '%s' "${output}" | python3 -c '
import json, sys
try:
    document = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
items = document.get("Items")
if not isinstance(items, dict) or "ReferencePath" not in items:
    sys.exit(1)
for entry in items["ReferencePath"]:
    path = (entry.get("Identity") or "").replace("\\", "/")
    name = path.rsplit("/", 1)[-1]
    if name.lower().endswith(".dll"):
        name = name[:-4]
    fusion = (entry.get("FusionName") or "").strip()
    identity = fusion.split(",")[0].strip() if fusion else ""
    sys.stdout.write("%s\t%s\n" % (identity, name))
'
}

compiled_source_files() {
  # $1 project. Prints the absolute path of every file in the project's evaluated
  # Compile item set - the actual compiler input, after default globs, explicit
  # includes, links and any <Import>ed contribution. FullPath rather than
  # Identity, because a linked file's Identity is relative to the project and can
  # point anywhere. Returns nonzero when the project cannot be evaluated or the
  # Compile item name is absent from the output; an EMPTY Compile set is valid
  # (several projects in this repository have no sources yet).
  local output
  output="$(dotnet msbuild "${REPO_ROOT}/$1" -nologo -getItem:Compile 2>/dev/null)" || return 1
  printf '%s' "${output}" | python3 -c '
import json, sys
try:
    document = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
items = document.get("Items")
if not isinstance(items, dict) or "Compile" not in items:
    sys.exit(1)
for entry in items["Compile"]:
    path = (entry.get("FullPath") or "").strip()
    if path:
        sys.stdout.write(path + "\n")
'
}

msbuild_property() {
  # $1 project, $2 property name. Prints the evaluated value.
  #
  # Returns nonzero when MSBuild cannot evaluate the project, or when the value is
  # empty, so a caller never compares an accepted name against a silently empty
  # string and calls the match a failure it can explain. An empty value and an
  # unevaluable project are both "no answer", and neither is an answer.
  local output
  output="$(dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getProperty:$2" 2>/dev/null)" || return 1
  output="${output//[$'\r\n']/}"
  [[ -n "${output}" ]] || return 1
  printf '%s' "${output}"
}

godot_assembly_names() {
  # Filters reference identities, assembly identities or paths on stdin down to the
  # Godot assemblies among them, as a comma-separated list. Matches the last path
  # segment so a bare assembly name and a full HintPath are treated alike, and
  # treats tabs as separators so a caller may feed more than one column per
  # reference.
  tr '\t' '\n' \
    | sed -E 's|.*[/\\]||; s|\.dll$||' \
    | grep -iE '^Godot([A-Za-z0-9.]*)?$' \
    | sort -u | paste -sd, - || true
}

resolved_assembly_identities() {
  # $1 project. Prints "<assembly identity>\t<file name>" for every entry in the
  # project's RESOLVED compile-time reference set. Returns nonzero when MSBuild
  # fails or its output is not the JSON shape expected.
  #
  # The identity is the simple name out of FusionName, which
  # ResolveAssemblyReferences reads from the assembly's own metadata. It therefore
  # does not change when the FILE is renamed, which is the whole point: copying
  # GodotSharp.dll to some other name and referencing it under that name defeats
  # every name-based check while putting the real Godot assembly on a pure
  # project's compile line.
  #
  # The file name is printed ALONGSIDE the identity rather than instead of it, so a
  # reference whose identity metadata cannot be read is still matched by name
  # rather than reported as clean.
  #
  # The absent-assets case is NOT handled here. Without obj/project.assets.json
  # this command prints NETSDK1004 and exits 1 - but it also prints a well-formed
  # document whose ReferencePath array is EMPTY, and an empty reference set is
  # exactly what a project with no Godot dependency looks like. A caller that
  # dropped the exit status would turn "nothing was measured" into nine passes.
  # The caller therefore tests the precondition explicitly before calling this at
  # all, and this function's nonzero return is reserved for a genuine failure.
  local project="$1"
  local output
  output="$(dotnet msbuild "${REPO_ROOT}/${project}" -nologo \
    -getItem:ReferencePath -t:ResolveAssemblyReferences \
    -p:BuildProjectReferences=false 2>/dev/null)" || return 1
  printf '%s' "${output}" | python3 -c '
import json, sys
try:
    document = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
items = document.get("Items")
if not isinstance(items, dict) or "ReferencePath" not in items:
    sys.exit(1)
for entry in items["ReferencePath"]:
    path = (entry.get("Identity") or "").replace("\\", "/")
    name = path.rsplit("/", 1)[-1]
    if name.lower().endswith(".dll"):
        name = name[:-4]
    fusion = (entry.get("FusionName") or "").strip()
    identity = fusion.split(",")[0].strip() if fusion else ""
    sys.stdout.write("%s\t%s\n" % (identity, name))
'
}

project_name() {
  local base="${1##*[/\\]}"
  printf '%s' "${base%.*proj}"
}

section "1. accepted repository layout (VER-FND-001-003)"
for path in "${EXPECTED_PATHS[@]}"; do
  if [[ -e "${REPO_ROOT}/${path}" ]]; then
    pass "exists: ${path}"
  else
    fail "missing prescribed path: ${path}"
  fi
done

section "2. solution contains exactly the accepted projects (VER-FND-001-003)"
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

section "2a. every accepted project builds in every solution configuration (VER-FND-001-003)"
#
# Solution membership is not the same as being built. Deleting a single
# "<GUID>.Debug|Any CPU.Build.0" line leaves `dotnet sln list` reporting nine
# projects while `dotnet build` compiles eight, so § 2 alone reports success
# for a solution that silently stops building a project. Assert the build flag
# per accepted project per solution configuration. Only the presence of the flag
# is asserted here, not which configuration it maps to.
#
# Numbered 2a rather than as a new § 3: § 3a and § 7a already establish the suffix
# as this file's idiom for a check inserted between two existing ones, and round
# evidence quotes this gate's section numbers verbatim.
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

section "3. project reference edges match the accepted boundary (VER-FND-001-004)"
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

section "3a. each accepted project keeps its accepted assembly identity (VER-FND-001-003)"
#
# A project renamed only via <AssemblyName> keeps its accepted file path, so § 1's
# layout check, § 2's project set and § 3's reference edges all still pass while the
# assembly the boundary is written about no longer exists under the name the boundary
# names. Every check in this file above this one reads PATHS; this is the only one that
# reads the identity the compiler actually emits.
#
# Numbered 3a rather than inserted as a new § 4 on purpose: round evidence quotes this
# gate's section numbers and assertion text verbatim, and § 7a already establishes the
# suffix as this file's idiom for a check added between two existing ones.
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs _godot <<<"${entry}"
  expected_name="$(project_name "${project}")"
  if ! actual_name="$(msbuild_property "${project}" AssemblyName)"; then
    fail "${expected_name}: AssemblyName could not be evaluated, so this project's assembly identity is unverified; an unread identity is not an accepted one"
  elif [[ "${actual_name}" == "${expected_name}" ]]; then
    pass "${expected_name} builds as assembly ${actual_name}"
  else
    fail "${expected_name} builds as assembly '${actual_name}'; the accepted boundary names '${expected_name}'"
  fi
done

section "4. only game/ may reference Godot (VER-FND-001-004)"
#
# Three independent signals, because each covers routes the others miss:
#
#   resolved  the ASSEMBLY IDENTITY of every ReferencePath entry after
#             ResolveAssemblyReferences - the actual compile line. This is the
#             authoritative signal: it covers PackageReference,
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
#
# The resolved signal reads the reference's ASSEMBLY IDENTITY, not its file name,
# because a file name is not an identity. Copying
# ~/.nuget/packages/godotsharp/4.7.1/lib/net8.0/GodotSharp.dll to
# build/probe/Engine.dll and referencing it as <Reference Include="Engine"> with a
# HintPath put the real Godot assembly on MechaMiner.Content's compile line, let it
# compile Godot.Vector2, and this section still reported "MechaMiner.Content has no
# Godot dependency". FusionName is what ResolveAssemblyReferences read out of that
# file's own metadata: "GodotSharp, Version=4.7.1.0, Culture=neutral,
# PublicKeyToken=null", regardless of what the file was called.
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  directory="${REPO_ROOT}/$(dirname "${project}")"
  name="$(project_name "${project}")"

  if ! resolved_references="$(resolved_assembly_identities "${project}")"; then
    fail "${name}: the resolved compile-time reference set could not be evaluated, so its Godot boundary is unverified"
    continue
  fi
  godot_resolved="$(printf '%s\n' "${resolved_references}" | godot_assembly_names)"

  if ! declared_references="$(msbuild_items "${project}" Reference,PackageReference)"; then
    fail "${name}: declared Reference/PackageReference items could not be evaluated"
    continue
  fi
  godot_declared="$(printf '%s\n' "${declared_references}" | godot_assembly_names)"

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
  # Deliberately unread: the decision below is the three-signal form (resolved, declared,
  # locked), so this filtered list has no consumer. The evaluation guard above it is a real
  # assertion - do not delete the probe along with this variable.

  # The lock file is half of this row's evidence, and an absent one used to be skipped:
  # `godot_locked` stayed empty, the row was decided on the MSBuild half alone, and the
  # "must not reference Godot" branch still printed `ok`. Deleting a project's
  # packages.lock.json therefore removed an assertion without failing anything, which is
  # Decision 11 rule 2 - an empty candidate set never satisfies a gate - in the mild
  # form. Every one of the nine accepted projects has a committed lock file today, so
  # this holds in fact; asserting it makes it hold by construction.
  #
  # verify-configurations.sh § 4 carries the sharper form of the same defect and the
  # set-level assertion that goes with it: that the lock-file set is non-empty and is
  # exactly the project set. This row is the per-project half.
  lock_file="${directory}/packages.lock.json"
  godot_locked=""
  if [[ ! -f "${lock_file}" ]]; then
    fail "$(project_name "${project}"): ${lock_file#"${REPO_ROOT}/"} is absent, so the locked half of the Godot boundary was not read; an unread half is not a satisfied one"
    continue
  fi
  godot_locked="$(grep -oE '"Godot[A-Za-z.]*"' "${lock_file}" | sort -u | tr -d '"' | paste -sd, - || true)"

  if [[ "${godot_allowed}" == "yes" ]]; then
    if [[ "${godot_resolved}" == *GodotSharp* && "${godot_locked}" == *GodotSharp* ]]; then
      pass "${name} references Godot as accepted (resolved: ${godot_resolved}, locked: ${godot_locked})"
    else
      fail "${name} must reference Godot but it is not on the resolved compile line and/or not locked (resolved: ${godot_resolved:-none}, locked: ${godot_locked:-none})"
    fi
  else
    if [[ -z "${godot_resolved}" && -z "${godot_declared}" && -z "${godot_locked}" ]]; then
      pass "${name} has no Godot dependency (assembly identity of every resolved compile-time reference checked)"
    else
      fail "${name} must not reference Godot (resolved: ${godot_resolved:-none}, declared: ${godot_declared:-none}, locked: ${godot_locked:-none})"
    fi
  fi
done

section "4a. the RESOLVED compile-time reference set respects the Godot boundary (VER-FND-001-004)"
#
# WHY THIS EXISTS ALONGSIDE § 4, AND WHY NEITHER IS REDUNDANT. Do not delete one of the
# two as a duplicate: they read different things, and each is blind to exactly what the
# other catches.
#
#   § 4 is EVALUATION-time. It reads what a project DECLARES - its PackageReference
#     items - cross-checked against its committed packages.lock.json. It needs no
#     restore, so it runs on any checkout, and it is the only one of the two that can
#     speak at all on an unrestored tree.
#
#   § 4a is RESOLUTION-time. It reads what the project ACTUALLY ENDS UP REFERENCING:
#     the assembly identity of every ReferencePath entry after
#     ResolveAssemblyReferences, which is the set the compiler is literally handed.
#
# What § 4a catches and § 4 cannot: a Godot reference that NO PROJECT DECLARES. A
# transitive engine reference arriving through another package's dependency graph is
# declared by nobody, appears in no project's PackageReference list, and still lands on
# the compile line. Likewise a raw <Reference> with a HintPath, a <Reference> contributed
# by an <Import>ed props/targets file, a Directory.Build.* contribution, and anything
# SDK- or central-package-injected - none of which is a PackageReference. And because the
# identity is read from FusionName, which ResolveAssemblyReferences takes from the
# assembly's own metadata, renaming the FILE defeats this check not at all.
#
# What § 4 catches and § 4a cannot: a declared dependency whose RESOLUTION FAILS. A
# <Reference> with a broken HintPath contributes nothing to ReferencePath, so the resolved
# set looks clean while the project's stated intent is to reference the engine. § 4 also
# reads the lock file, which covers transitive package routes, and it runs on trees where
# § 4a cannot run at all.
#
# THE THIRD OUTCOME, DELIBERATELY. ResolveAssemblyReferences cannot run without
# obj/project.assets.json, which only `dotnet restore` produces. "The property holds",
# "the property is violated" and "the measurement did not happen" are three different
# statements, and collapsing the third into the second is how a reader meets "could not be
# evaluated" on a plain checkout, reaches for build contention or a broken SDK, and spends
# an afternoon diagnosing the wrong thing. An absent assets file is therefore reported
# through skip() as NOT MEASURED - neither pass nor fail - which gate_summary reports as
# reduced coverage while the gate STILL EXITS 0. A plain checkout must not fail this gate
# for lacking a restore. A genuine MSBuild failure WITH the assets file present is still a
# fail, and so is an empty resolved set, which is a failed measurement rather than a
# project without dependencies.

resolved_absent=()
resolved_present=()
resolved_unevaluable=0

for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs _godot <<<"${entry}"
  # The assets path is read from MSBuild rather than assumed to be obj/project.assets.json:
  # Directory.Build.props may redirect the intermediate output path, and a guessed path
  # that is always absent would report "not measured" forever without anyone noticing.
  if ! assets_file="$(msbuild_property "${project}" ProjectAssetsFile)"; then
    fail "$(project_name "${project}"): ProjectAssetsFile could not be evaluated, so it is not even known whether the resolved reference set is measurable for this project"
    resolved_unevaluable=$((resolved_unevaluable + 1))
    continue
  fi
  if [[ -f "${assets_file}" ]]; then
    resolved_present+=("${entry}"$'\t'"${assets_file}")
  else
    resolved_absent+=("${entry}"$'\t'"${assets_file}")
  fi
done

# One skip rather than nine when nothing is measurable, because "no restore has been
# performed in this checkout" is a single fact about the tree, and nine restatements of
# one fact is the noise that made the original nine failures unreadable.
if [[ "${#resolved_present[@]}" -eq 0 && "${resolved_unevaluable}" -eq 0 ]]; then
  skip "4a. resolved compile-time reference set: NOT MEASURED for any of the ${#resolved_absent[@]} accepted projects - no restore has been performed in this checkout, so no project.assets.json exists and ResolveAssemblyReferences cannot run. This is not a failure and does not change this gate's exit code. § 4 asserted the declared and locked halves of the same boundary; run 'dotnet restore MechaMiner.sln' to measure this half too."
else
  if [[ "${#resolved_absent[@]}" -gt 0 ]]; then
    for record in "${resolved_absent[@]}"; do
      IFS=$'\t' read -r entry assets_file <<<"${record}"
      IFS='|' read -r project _expected_refs _godot <<<"${entry}"
      skip "$(project_name "${project}"): resolved compile-time reference set NOT MEASURED - ${assets_file#"${REPO_ROOT}/"} does not exist, so no restore has been performed for this project. Neither passed nor failed, and the gate's exit code is unchanged."
    done
  fi

  for record in "${resolved_present[@]}"; do
    IFS=$'\t' read -r entry assets_file <<<"${record}"
    IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
    name="$(project_name "${project}")"

    if ! resolved_references="$(resolved_assembly_identities "${project}")"; then
      fail "${name}: the resolved compile-time reference set could not be read even though ${assets_file#"${REPO_ROOT}/"} exists, so its Godot boundary is unverified at resolution time; this is a real failure and not an absent restore"
      continue
    fi

    # An empty resolved set is a failed measurement, not a project with no dependencies:
    # every .NET project resolves at least its framework references. Decision 11 rule 2 -
    # an empty candidate set never satisfies a gate - applies with force here, because an
    # empty ReferencePath is precisely what MSBuild prints alongside NETSDK1004.
    resolved_count="$(printf '%s\n' "${resolved_references}" | grep -c '[^[:space:]]' || true)"
    if [[ "${resolved_count}" -eq 0 ]]; then
      fail "${name}: the resolved compile-time reference set is EMPTY although its assets file exists; every .NET project resolves at least its framework references, so nothing was measured and an unmeasured boundary is not a satisfied one"
      continue
    fi

    godot_resolved="$(printf '%s\n' "${resolved_references}" | godot_assembly_names)"

    if [[ "${godot_allowed}" == "yes" ]]; then
      if [[ "${godot_resolved}" == *GodotSharp* ]]; then
        pass "${name} has Godot on its resolved compile line as accepted (resolved: ${godot_resolved}, of ${resolved_count} reference(s))"
      else
        fail "${name} must reference Godot but GodotSharp is not on its resolved compile line (resolved: ${godot_resolved:-none}, of ${resolved_count} reference(s))"
      fi
    else
      if [[ -z "${godot_resolved}" ]]; then
        pass "${name} has no Godot assembly on its resolved compile line (${resolved_count} reference(s) checked by assembly identity)"
      else
        fail "${name} must not reference Godot but Godot is on its RESOLVED compile line: ${godot_resolved} (of ${resolved_count} reference(s)). § 4 reads only what this project declares, so a reference arriving transitively or through a raw <Reference> passes there and is caught here."
      fi
    fi
  done
fi

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

section "5. no pure project references MechaMiner.Game (VER-FND-001-004)"
assert_absent_pattern \
  "no project references MechaMiner.Game" \
  'ProjectReference[^>]*MechaMiner\.Game\.csproj' \
  '*.csproj' \
  src tests game

section "6. no Godot types in C# source outside game/ (VER-FND-001-004)"
#
# THE RULE, AND WHY IT IS NEITHER OF THE TWO IT REPLACES.
#
# Two expressions have held this section, and both were wrong in opposite
# directions. Neither is restored here.
#
#   `using[[:space:]]+Godot([;.]|$)` tested one import spelling while the section
#   title claimed to check for types. A fully-qualified `Godot.Vector2` with no
#   import at all, `using static Godot.Mathf;`, `using GD2 = Godot;` and
#   `using Alias = Godot;` all passed it. Note what the defect actually was: each
#   of those is a POSITION the pattern failed to describe - `Godot.Vector2` has no
#   using, `using static Godot.Mathf` has a word between `using` and `Godot`, and
#   `using GD2 = Godot` has the name on the far side of an `=`. The pattern was too
#   specific about position; position was not the wrong thing to look at.
#
#   The bare token in ANY position, `(^|[^A-Za-z0-9_.])Godot([^A-Za-z0-9_]|$)`
#   against raw file text, fixed those false negatives and bought false positives.
#   Measured on the merged tree: it fires on 65 lines across 19 files, and 64 of
#   those lines are doc comments, `//` comments and string literals - mostly prose
#   explaining why this boundary exists, e.g. an invariant quoted as "has no
#   dependency on Godot, files, Steam". The 65th is
#   src/MechaMiner.Tools/Toolchain/ToolchainPins.cs, where the line is
#   `public GodotPin Godot { get; set; } = new();` - a property named `Godot` whose
#   type `GodotPin` is ours, mapping the `godot` key of build/toolchain.json. That
#   is C# code and not prose, so no amount of comment stripping reaches it, and it
#   names no Godot type. The old rule's answer was that prose "should be reworded",
#   which makes the writing serve the checker and proves nothing about the boundary.
#
# So: the token is unchanged and still matches in any position, and it is required
# to sit in a position where a Godot TYPE reference actually puts it. Two branches,
# applied to text with comments and string literals already removed:
#
#   A. QUALIFIER - the token followed by optional whitespace and a `.`. Covers
#      `Godot.Vector2`, `Godot.GD.Print`, `global::Godot.Node`, `[Godot.Export]`,
#      `List<Godot.Node>`, `using static Godot.Mathf`, `using GD2 = Godot.X`.
#   B. USING DIRECTIVE - the token anywhere inside a using directive, in any of its
#      spellings: `using Godot;`, `using static Godot...`, `using Alias = Godot...`,
#      and `global using Godot;`.
#
# WHY THAT LOSES NO COVERAGE. A Godot type cannot be named from C# without one or
# the other. An unqualified `Vector2` compiles only if some using directive imports
# the namespace; an alias is established by a using directive; a global using is a
# using directive. The two branches are therefore not a sample of the ways to reach
# the engine, they are the closure of them. Three consequences were checked rather
# than assumed, and each is the reason for a control in § 6a:
#
#   * A using directive MAY SPAN LINES. `using` NEWLINE `Godot;` is one directive
#     and compiles - verified by building it, not by reading the grammar - as does
#     `global using` NEWLINE `Godot;`. Branch B therefore matches a DIRECTIVE SPAN,
#     from `using` to its `;`, across newlines, and not "a line that looks like a
#     using". A per-line test is the obvious simplification here and it is a hole;
#     § 6a holds it open.
#   * A global using in ANOTHER FILE of the same project lets a file name a Godot
#     type while containing no `Godot` text at all. That file is not the finding;
#     the file holding the directive is, and both readers below scan every file of
#     the project, so the project is still reported.
#   * A `<Using Include="Godot" />` in the .csproj puts the directive in a
#     GENERATED file, obj/<project>.GlobalUsings.g.cs, which reader 1 prunes. This
#     is precisely why reader 2 exists and why it deliberately includes obj/.
#     Verified by building a project with `<Using Include>` and reading the
#     generated file: it contains `global using <namespace>;`, which branch B
#     matches.
#
# WHAT STILL DEFEATS IT is unchanged by this work and is not claimed to be fixed:
# the unicode-escaped identifier and the renamed GodotSharp assembly, both written
# up in full in the note on the second reader below. Neither attacks the position
# branches; both attack the TOKEN, and they defeat this rule exactly as they defeat
# the two it replaces.
#
# WHAT THE STRIPPER HANDLES, AND WHAT IT DOES NOT. It is a single-pass scanner over
# the file, not a sequence of substitutions, so a construct one pass understands
# cannot be cut in half by another: `//` and `///` to end of line, `/* */`
# INCLUDING multi-line, `"..."` with backslash escapes, verbatim `@"..."` with `""`
# as the escaped quote and newlines inside it, interpolated `$"..."`, the combined
# `$@"` and `@$"`, raw `"""..."""` fences, and single-character literals `'x'` and
# `'\n'`. Every removal leaves a space, so deleting a literal cannot splice two
# identifiers into a token nobody wrote. It does NOT: decode `\uXXXX` identifier
# escapes (defeat 1 above); understand nested raw-string fences longer than the one
# that opened them; or track `#if` regions, so text inside a disabled `#if` block is
# scanned as if it were live, which is fail-closed rather than fail-open. It KEEPS
# the code inside an interpolation hole - `$"{Godot.GD.X()}"` still matches -
# because that text is code, and stripping it would be a false negative.
readonly GODOT_SCAN_ROOTS=(src tests build/policy-fixtures)

# The predicate, in one place. Prints "<path>\t<reason>" for each file that names a
# Godot type and nothing for the others, so an empty result means "none of these
# files", never "the reader failed".
#
# Both readers below call this function, and so do the controls in § 6a. That is
# what makes those controls in band: they drive this program against injected
# fixtures rather than re-implementing the rule beside it, so a control cannot pass
# against logic the gate does not use.
godot_offenders() {
  [[ "$#" -gt 0 ]] || return 0
  python3 - "$@" <<'GODOTSCAN'
import re
import sys

TOKEN = re.compile(r'(^|[^A-Za-z0-9_.])Godot([^A-Za-z0-9_]|$)')
QUALIFIER = re.compile(r'(^|[^A-Za-z0-9_.])Godot\s*\.')
# A using DIRECTIVE SPAN: `using` (optionally `global using`) through its `;`,
# newlines included. Anchored to the start of a line or to the end of a previous
# statement or block so that the word `using` inside an expression is not a span.
USING_DIRECTIVE = re.compile(
    r'(?:^|(?<=[;{}]))[ \t\r\n]*(?:global[ \t\r\n]+)?using(?=[ \t\r\n])[^;]*;',
    re.MULTILINE)


def strip_comments_and_strings(text):
    out = []
    i, n = 0, len(text)
    while i < n:
        char = text[i]
        pair = text[i:i + 2]
        if pair == '//':
            end = text.find('\n', i)
            end = n if end < 0 else end
            out.append(' ')
            i = end
            continue
        if pair == '/*':
            end = text.find('*/', i + 2)
            end = n if end < 0 else end + 2
            out.append(' ' + '\n' * text.count('\n', i, end))
            i = end
            continue
        fence = re.match(r'"{3,}', text[i:])
        if fence:
            mark = fence.group(0)
            end = text.find(mark, i + len(mark))
            end = n if end < 0 else end + len(mark)
            out.append(' ' + '\n' * text.count('\n', i, end))
            i = end
            continue
        opener = re.match(r'(\$@|@\$|@|\$)"', text[i:])
        if opener or char == '"':
            prefix = opener.group(1) if opener else ''
            verbatim = '@' in prefix
            interpolated = '$' in prefix
            i += len(prefix) + 1
            out.append(' ')
            depth = 0
            while i < n:
                char = text[i]
                if interpolated and depth == 0 and char == '{':
                    if text[i:i + 2] == '{{':
                        i += 2
                        continue
                    depth = 1
                    out.append(' ')
                    i += 1
                    continue
                if interpolated and depth > 0:
                    # Inside an interpolation hole this is code, so keep it.
                    if char == '{':
                        depth += 1
                    elif char == '}':
                        depth -= 1
                        if depth == 0:
                            out.append(' ')
                            i += 1
                            continue
                    out.append(char)
                    i += 1
                    continue
                if verbatim:
                    if char == '"':
                        if text[i:i + 2] == '""':
                            i += 2
                            continue
                        i += 1
                        break
                    if char == '\n':
                        out.append('\n')
                    i += 1
                    continue
                if char == '\\':
                    i += 2
                    continue
                if char == '"':
                    i += 1
                    break
                if char == '\n':      # an unterminated non-verbatim literal
                    break
                i += 1
            out.append(' ')
            continue
        literal = re.match(r"'([^'\\\n]|\\.)'", text[i:])
        if literal:
            out.append(' ')
            i += literal.end()
            continue
        out.append(char)
        i += 1
    return ''.join(out)


def reason(text):
    stripped = strip_comments_and_strings(text)
    if QUALIFIER.search(stripped):
        return 'names Godot in qualifier position'
    for span in USING_DIRECTIVE.finditer(stripped):
        if TOKEN.search(span.group(0)):
            return 'imports Godot on a using directive'
    return ''


for path in sys.argv[1:]:
    try:
        with open(path, encoding='utf-8', errors='replace') as handle:
            body = handle.read()
    except OSError as error:
        print('%s\tcould not be read (%s)' % (path, error.strerror))
        continue
    why = reason(body)
    if why:
        print('%s\t%s' % (path, why))
GODOTSCAN
}

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
  godot_scan_files=()
  mapfile -d '' -t godot_scan_files < <(cd "${REPO_ROOT}" && find "${GODOT_SCAN_ROOTS[@]}" \
    -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print0 2>/dev/null | sort -z)

  # An empty file set is a broken search, not a clean repository. The scan-root
  # check above catches a moved directory; this catches a find that matched nothing
  # in directories that do exist.
  if [[ "${#godot_scan_files[@]}" -eq 0 ]]; then
    fail "the Godot source scan matched no .cs file at all under $(printf '%s/ ' "${GODOT_SCAN_ROOTS[@]}")- the search is broken, not the tree clean"
  # The status is checked rather than the output trusted: a scanner that died prints
  # nothing, and nothing must never read as "no violations".
  elif ! godot_scan_raw="$(cd "${REPO_ROOT}" && godot_offenders "${godot_scan_files[@]}")"; then
    fail "the Godot source scan failed to run over ${#godot_scan_files[@]} file(s), so this assertion is unverified rather than clean"
  else
    stray_godot="$(printf '%s' "${godot_scan_raw}" | cut -f1 | sort)"
    if [[ -z "${stray_godot}" ]]; then
      pass "none of the ${#godot_scan_files[@]} C# file(s) under $(printf '%s/ ' "${GODOT_SCAN_ROOTS[@]}")names a Godot type"
    else
      fail "Godot type named outside game/: $(printf '%s' "${stray_godot}" | paste -sd' ' -)"
    fi
  fi
fi

# Second reader of the same rule, over a file set the directory scan cannot have.
#
# A directory scan only sees the directories it names. A pure project that
# <Compile Include=>s a file from anywhere else compiles source this section never
# looks at: pulling generated/HiddenGodotSource.cs into MechaMiner.Content let it
# compile Godot.Vector2 while this section reported "no C# file under src/ tests/
# build/policy-fixtures/ names a Godot type" and the gate exited 0. The file set is
# therefore also taken from each pure project's EVALUATED Compile item set - the
# real compiler input, after default globs, explicit includes, links, and any
# <Import>ed contribution - wherever those files live, including under obj/ and
# outside the repository's source directories entirely.
#
# The two readers deliberately share one rule: both call godot_offenders, the single
# program defined above, so the stripping and the two position branches exist in one
# place and cannot drift apart. Only the file set differs.
#
# THE SHARED RULE HAS NOW CHANGED, and this note records it because the change is the
# one this paragraph used to anticipate. It previously read "the SHARED RULE CHANGES
# AT FND-004", describing a rule that strips comments and string literals before
# applying the token and requires it in `using` or qualifier position. That change is
# implemented above, taken by BOTH readers together in the same commit as the note
# always required, and its controls are in § 6a. It was NOT taken as FND-004 spells
# it: FND-004 applies the token per line, and a using directive may span lines, so
# branch B matches a directive span instead. Two things did not change - the token
# itself is character-for-character what it was, and defeats 1 and 2 below are still
# open:
#
#   1. A using directive written with a UNICODE-ESCAPED IDENTIFIER. C# allows
#      \uXXXX escapes inside identifiers, so `using \u0047odot;` is the very same
#      using directive as `using Godot;` to the compiler, while the file contains
#      no occurrence of the token either reader matches. The stripper above does not
#      decode identifier escapes, so this is open exactly as it was.
#   2. Source that names no Godot type at all because the type comes from a
#      RENAMED ASSEMBLY. GodotSharp.dll's identity lives in its `#Strings` heap,
#      and it ships with PublicKeyToken=null, so a 10-byte patch renames the
#      assembly with nothing to re-sign; the reference then resolves under a name
#      the assembly-identity reader does not recognise and the source names types
#      from a namespace the token does not match. This is an attack on the shared
#      rule's premise - "Godot arrives under a name we can recognise" - not on
#      either file set.
#   3. The two combined, which is how the escape was actually demonstrated.
#
# Whoever performs the FND-004 merge owns all three. They are named here, rather than
# quietly inherited, because the position branches above narrow WHERE the token has to
# sit and therefore do nothing about a token that is not in the file to begin with:
# both defeats survive this change untouched, and a rule that strips prose and reads
# position is not thereby a rule that cannot be evaded.
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  [[ "${godot_allowed}" == "no" ]] || continue
  name="$(project_name "${project}")"

  if ! compiled_sources="$(compiled_source_files "${project}")"; then
    fail "${name}: the evaluated Compile item set could not be read, so the source side of its Godot boundary is unverified"
    continue
  fi

  compiled_count=0
  compiled_offenders=()
  compiled_existing=()
  while IFS= read -r source; do
    [[ -n "${source}" ]] || continue
    compiled_count=$((compiled_count + 1))
    if [[ ! -f "${source}" ]]; then
      # A compile input that does not exist would fail the build, but it must not
      # read here as a file that was scanned and found clean.
      compiled_offenders+=("${source#"${REPO_ROOT}/"} (missing)")
      continue
    fi
    compiled_existing+=("${source}")
  done <<<"${compiled_sources}"

  # The same predicate reader 1 used, over this project's real compiler input.
  if [[ "${#compiled_existing[@]}" -gt 0 ]]; then
    if ! compiled_report="$(godot_offenders "${compiled_existing[@]}")"; then
      fail "${name}: the Godot scan failed to run over its ${#compiled_existing[@]} compile input(s), so the source side of its Godot boundary is unverified"
      continue
    fi
    while IFS= read -r offender; do
      [[ -n "${offender}" ]] || continue
      compiled_offenders+=("${offender#"${REPO_ROOT}/"}")
    done < <(printf '%s' "${compiled_report}" | cut -f1)
  fi

  if [[ "${#compiled_offenders[@]}" -eq 0 ]]; then
    pass "${name} compiles ${compiled_count} source file(s), none naming a Godot type"
  else
    fail "${name} compiles source that names a Godot type: $(printf '%s ' "${compiled_offenders[@]}")"
  fi
done

section "6a. negative controls: the Godot boundary rule can actually fail, in both directions (VER-FND-001-004)"
#
# § 6 had no control at all, which is how it carried two successive rules that were
# each wrong in a different direction without either being caught by a run. Both
# directions are asserted here, because a rule that only ever gets stricter passes a
# one-directional control forever:
#
#   MUST RED - the four spellings that defeated `using[[:space:]]+Godot([;.]|$)`, so
#   that expression cannot come back unnoticed, plus the line-spanning directive that
#   defeats a per-line reading of branch B.
#   MUST NOT RED - the prose and identifier cases the bare any-position token
#   falsely accused, each drawn from a shape that is actually in this repository.
#
# The fixtures live under build/policy-fixtures/, which is one of § 6's own scan
# roots, and they are written and removed inside this section. They CANNOT be left in
# the tree: the four must-red fixtures would be found by reader 1 on the next run and
# would redden § 6 for real. That is why they are created after § 6 has scanned,
# removed here, and their removal asserted below against reader 1's own verdict.
#
# The controls call godot_offenders - the same program §§ 6's two readers just ran,
# not a copy of the expression - so a control cannot pass against logic the gate does
# not use, and changing the rule without changing these is not possible.
readonly GODOT_CONTROL_DIR="${REPO_ROOT}/build/policy-fixtures/godot-boundary-controls"

remove_godot_controls() {
  rm -rf "${GODOT_CONTROL_DIR}"
}

remove_godot_controls
trap remove_godot_controls EXIT
mkdir -p "${GODOT_CONTROL_DIR}"

# "<file>|<must-red: yes|no>|<what it is>"
readonly GODOT_CONTROL_CASES=(
  "QualifiedNoUsing.cs|yes|a fully-qualified Godot.Vector2 with no using directive at all"
  "UsingStatic.cs|yes|using static Godot.Mathf"
  "UsingAlias.cs|yes|using GD2 = Godot"
  "PlainUsing.cs|yes|a plain using Godot;"
  "UsingSpanningLines.cs|yes|a using directive split across a newline, which compiles and which a per-line branch B would miss"
  "PropertyNamedGodot.cs|no|a property named Godot whose type GodotPin is ours - the ToolchainPins.cs shape"
  "LineComment.cs|no|Godot named in a // comment"
  "DocComment.cs|no|Godot named in a /// doc comment"
  "StringLiteral.cs|no|Godot named inside a string literal"
  "BlockComment.cs|no|Godot named inside a multi-line /* */ block comment"
  "VerbatimString.cs|no|Godot named inside a multi-line verbatim @\" string"
)

cat >"${GODOT_CONTROL_DIR}/QualifiedNoUsing.cs" <<'FIXTURE'
// Control fixture, written and removed by build/verify-architecture.sh.
namespace Fixture;
internal sealed class QualifiedNoUsing
{
    private Godot.Vector2 position;
}
FIXTURE

cat >"${GODOT_CONTROL_DIR}/UsingStatic.cs" <<'FIXTURE'
using static Godot.Mathf;
namespace Fixture;
internal sealed class UsingStatic { }
FIXTURE

cat >"${GODOT_CONTROL_DIR}/UsingAlias.cs" <<'FIXTURE'
using GD2 = Godot;
namespace Fixture;
internal sealed class UsingAlias { }
FIXTURE

cat >"${GODOT_CONTROL_DIR}/PlainUsing.cs" <<'FIXTURE'
using Godot;
namespace Fixture;
internal sealed class PlainUsing { }
FIXTURE

# `using` and the namespace on separate lines is ONE directive and compiles; this was
# verified by building it, not inferred from the grammar.
cat >"${GODOT_CONTROL_DIR}/UsingSpanningLines.cs" <<'FIXTURE'
using
Godot;
namespace Fixture;
internal sealed class UsingSpanningLines { }
FIXTURE

# The exact shape of src/MechaMiner.Tools/Toolchain/ToolchainPins.cs: a property named
# Godot, of a type of ours, bound to the `godot` key of build/toolchain.json.
cat >"${GODOT_CONTROL_DIR}/PropertyNamedGodot.cs" <<'FIXTURE'
namespace Fixture;
internal sealed class GodotPin { }
internal sealed class PropertyNamedGodot
{
    public GodotPin Godot { get; set; } = new();
}
FIXTURE

# Every must-not-red fixture below carries a QUALIFIER-SHAPED occurrence, `Godot.`, and
# not merely a bare `Godot`. That is deliberate and it is what makes them controls rather
# than decoration: bare undotted prose - "no dependency on Godot, files, Steam" - is
# already cleared by the position branches whether the stripper runs or not, so a fixture
# spelled that way would stay green with the stripper deleted and would prove nothing
# about it. `Godot.NET.Sdk` in a doc comment is not a hypothetical shape either; it is
# what src/MechaMiner.Tools/Cli/WorkflowConfiguration.cs, Verbs/BuildVerb.cs and
# Verbs/GodotImportVerb.cs actually say, and `Godot.Collections` is what
# tests/.../SimulationAssemblyDeterminismTests.cs says. Without stripping, those real
# files red.
cat >"${GODOT_CONTROL_DIR}/LineComment.cs" <<'FIXTURE'
namespace Fixture;
internal sealed class LineComment
{
    // Restore is deliberately not given a configuration. Godot.NET.Sdk decides it,
    // and this simulation has no dependency on Godot, files or Steam.
    private int step;
}
FIXTURE

cat >"${GODOT_CONTROL_DIR}/DocComment.cs" <<'FIXTURE'
namespace Fixture;
/// <summary>Pure logic.</summary>
/// <remarks><c>Godot.NET.Sdk</c> defines the configuration; <c>Godot.Collections</c>
/// is forbidden here, and this type names neither.</remarks>
internal sealed class DocComment { }
FIXTURE

cat >"${GODOT_CONTROL_DIR}/StringLiteral.cs" <<'FIXTURE'
namespace Fixture;
internal sealed class StringLiteral
{
    public const string Forbidden = "Godot";
    public const string Hint = "a pure test must not call Godot.GD.Print or launch Godot";
}
FIXTURE

cat >"${GODOT_CONTROL_DIR}/BlockComment.cs" <<'FIXTURE'
namespace Fixture;
/*
 * Godot.GD is engine-only and must not appear here.
 * Godot.Vector2 likewise.
 */
internal sealed class BlockComment { }
FIXTURE

cat >"${GODOT_CONTROL_DIR}/VerbatimString.cs" <<'FIXTURE'
namespace Fixture;
internal sealed class VerbatimString
{
    public const string Report = @"line one
Godot.GD.Print(x);
""quoted"" and still inside the literal";
}
FIXTURE

godot_control_failures=0
for entry in "${GODOT_CONTROL_CASES[@]}"; do
  IFS='|' read -r control_file control_must_red control_what <<<"${entry}"
  control_path="${GODOT_CONTROL_DIR}/${control_file}"

  if [[ ! -f "${control_path}" ]]; then
    control_fail "control fixture ${control_file} was not written, so its case was not tested"
    godot_control_failures=$((godot_control_failures + 1))
    continue
  fi

  if ! control_report="$(godot_offenders "${control_path}")"; then
    control_fail "the rule failed to run against ${control_file}, so ${control_what} is untested"
    godot_control_failures=$((godot_control_failures + 1))
    continue
  fi

  # Parameter expansion rather than `| head -n 1 | cut -f2`: this file forbids an
  # early-exiting reader on the right of a pipe, and that prohibition does not get an
  # exception for a control.
  control_reason="${control_report%%$'\n'*}"
  control_reason="${control_reason#*$'\t'}"

  if [[ "${control_must_red}" == "yes" ]]; then
    if [[ -n "${control_reason}" ]]; then
      control_pass "reds as it must: ${control_what} (${control_reason})"
    else
      control_fail "DID NOT RED: ${control_what}. The rule cannot see a real Godot reference in this position, so § 6's green means nothing for it"
      godot_control_failures=$((godot_control_failures + 1))
    fi
  else
    if [[ -z "${control_reason}" ]]; then
      control_pass "stays green as it must: ${control_what}"
    else
      control_fail "FALSELY ACCUSED: ${control_what} was reported as '${control_reason}'. Rewording prose to satisfy this gate is not the fix; the rule is wrong"
      godot_control_failures=$((godot_control_failures + 1))
    fi
  fi
done

# Each misbehaving case already emitted control_fail, which counts itself as a failing
# negative control and names this section. The aggregate is therefore stated without
# counting again, and never as a finding about the repository: a control that malfunctions
# is a defect in this gate, not in the tree it is scanning.
if [[ "${godot_control_failures}" -eq 0 ]]; then
  control_pass "all ${#GODOT_CONTROL_CASES[@]} Godot-boundary controls behaved as designed, in both directions"
else
  control_detail <<<"${godot_control_failures} of ${#GODOT_CONTROL_CASES[@]} Godot-boundary controls did not behave as designed; § 6's verdict is not trustworthy on this run"
fi

remove_godot_controls
trap - EXIT

# The fixtures must be gone, or this section has littered a scan root with files that
# would redden § 6 on the next run. Asserted by re-running reader 1's own file search,
# not by testing for the directory, so a fixture left anywhere under the roots counts.
godot_leftovers="$(cd "${REPO_ROOT}" && find "${GODOT_SCAN_ROOTS[@]}" -path '*godot-boundary-controls*' -name '*.cs' 2>/dev/null | sort)"
if [[ -z "${godot_leftovers}" ]]; then
  control_pass "every control fixture was removed from the scan roots"
else
  control_fail "control fixtures were left behind and will redden § 6 on the next run: $(printf '%s' "${godot_leftovers}" | paste -sd' ' -)"
fi

section "7. no GDScript in the repository (VER-FND-001-005)"
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

# The first line of a probe's verdict, without a pipe. `probe | head -1` makes head exit
# after one line, the probe take SIGPIPE, and `set -o pipefail` surface 141; `set -e` then
# ABORTS THE WHOLE GATE mid-run - after § 1-6 have printed `ok` lines and before § 7 or
# § 7a print anything, which reads as a truncated log rather than as a failure. It was
# intermittent and it fired during § 8's negative controls.
#
# Measured over 300 trials on a probe emitting one path per offending file: 0 of 300 at
# 428 bytes and at 4.1 KB, 136 of 300 at 70 KB, 300 of 300 at 326 KB. A tree with a few
# hundred stray .gd files is exactly the tree this section exists to fail on, so the abort
# was reachable precisely when the gate mattered. See delivery-waves § Decision 13.
#
# `tail -n +2` below is left as a pipeline on purpose: `tail` must read to EOF to know
# where the end is, so it never closes the pipe early and there is no SIGPIPE to take.
first_line() {
  local text="$1"
  printf '%s' "${text%%$'\n'*}"
}

gdscript_verdict="$(gdscript_probe)"
gdscript_kind="$(first_line "${gdscript_verdict}")"
gdscript_detail="$(printf '%s\n' "${gdscript_verdict}" | tail -n +2)"

if [[ "${gdscript_kind}" == "unreadable" ]]; then
  fail "could not enumerate GDScript candidates, so the no-GDScript rule is unproved: ${gdscript_detail}"
elif [[ "${gdscript_kind}" == "violation" ]]; then
  fail "GDScript is not permitted (TR-FND-002), tracked or untracked: ${gdscript_detail}"
else
  pass "no .gd file is tracked, and none is present untracked in the working tree"
fi

section "7a. negative controls: the no-GDScript gate can actually fail"
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

  control_kind="$(first_line "$(gdscript_probe)")"
  if [[ "${control_kind}" == "violation" ]]; then
    control_pass "an untracked .gd file is detected as a violation"
  else
    control_fail "an untracked .gd file was reported as '${control_kind}'; the gate cannot see untracked GDScript"
  fi

  # Control 2: the same fixture, with git unable to answer. The gate must report that
  # it could not tell, and must never report a clean tree.
  control_kind="$(first_line "$(GIT_DIR=/nonexistent/verify-architecture-broken.git gdscript_probe)")"
  if [[ "${control_kind}" == "unreadable" ]]; then
    control_pass "a git failure is reported as unreadable, not as a clean tree"
  else
    control_fail "with a broken git the probe reported '${control_kind}'; a failed enumeration must not pass"
  fi

  remove_gdscript_fixture
  trap - EXIT

  # The fixture must be gone. The comparison is against § 7's own verdict rather than
  # against "clean", so that a pre-existing .gd file in the tree - which § 7 already
  # failed on - is not reported a second time as a fixture-cleanup failure.
  control_kind="$(first_line "$(gdscript_probe)")"
  if [[ "${control_kind}" == "${gdscript_kind}" ]]; then
    control_pass "the fixture was removed; the probe reports '${control_kind}' again, as it did in § 7"
  else
    control_fail "the GDScript fixture was not removed: probe reports '${control_kind}', § 7 saw '${gdscript_kind}'"
  fi
fi

section "8. the CI workflow still gates the repository (VER-FND-005-009)"
#
# Section 1 lists the workflow among EXPECTED_PATHS, which is a test of the path and
# nothing more. `[[ -e ]]` accepts a zero-byte fast.yml, and it accepts a workflow with
# no jobs and no pull_request or push trigger. Either of those un-gates every gate in
# this repository exactly as silently as deleting the file, and `./build.sh build` was
# green for both. What follows asserts the content the suite depends on.
#
# The required verbs are a list of requirements, not a roster of what the file happens
# to contain: delivery-waves § Step 4 says "The fast pull-request path is bootstrap,
# format-check, build, test-fast, godot-import". Deriving them from the workflow would
# assert only that the workflow agrees with itself.

readonly CI_WORKFLOW=".github/workflows/fast.yml"
readonly REQUIRED_TRIGGERS=("pull_request" "push")
readonly REQUIRED_FAST_VERBS=("bootstrap" "format-check" "build" "test-fast" "godot-import")

# The child keys of a top-level `key:` block, in either the block or the inline-list
# form, so `on: [push, pull_request]` reads the same as the block this file uses.
yaml_block_keys() {
  awk -v want="$1" '
    index($0, want ":") == 1 {
      rest = substr($0, length(want) + 2)
      sub(/^[[:space:]]*/, "", rest)
      if (rest ~ /^\[/) {
        gsub(/[][]/, "", rest)
        n = split(rest, parts, /,/)
        for (i = 1; i <= n; i++) {
          gsub(/[[:space:]]/, "", parts[i])
          if (parts[i] != "") { print parts[i] }
        }
      } else if (rest == "" || rest ~ /^#/) {
        block = 1
      }
      next
    }
    block && /^[^[:space:]#]/ { block = 0 }
    block && /^  [A-Za-z_][A-Za-z0-9_-]*:/ {
      key = $0
      sub(/:.*/, "", key)
      gsub(/[[:space:]]/, "", key)
      print key
    }
  ' "$2"
}

workflow_path="${REPO_ROOT}/${CI_WORKFLOW}"

if [[ ! -f "${workflow_path}" ]]; then
  fail "${CI_WORKFLOW} does not exist, so nothing in this repository is gated by anything"
elif [[ ! -s "${workflow_path}" ]]; then
  fail "${CI_WORKFLOW} exists but is empty, so it runs nothing; § 1's path test cannot tell those apart"
else
  pass "${CI_WORKFLOW} exists and is not empty"

  # Here-strings rather than pipes into `grep -q`: grep exits on its first match and
  # closes the pipe, printf takes SIGPIPE, and `set -o pipefail` then aborts the whole
  # script with 141 instead of reporting an assertion. That happened, nondeterministically,
  # on the control that deletes one step.
  mapfile -t workflow_triggers < <(yaml_block_keys "on" "${workflow_path}")
  workflow_trigger_list="$(printf '%s\n' "${workflow_triggers[@]-}")"
  for trigger in "${REQUIRED_TRIGGERS[@]}"; do
    if grep -qxF "${trigger}" <<<"${workflow_trigger_list}"; then
      pass "${CI_WORKFLOW} triggers on ${trigger}"
    else
      fail "${CI_WORKFLOW} declares no ${trigger} trigger, so the suite never runs for that event"
    fi
  done

  mapfile -t workflow_jobs < <(yaml_block_keys "jobs" "${workflow_path}")
  if [[ "${#workflow_jobs[@]}" -eq 0 ]]; then
    fail "${CI_WORKFLOW} declares no job, so nothing in it can run"
  else
    pass "${CI_WORKFLOW} declares ${#workflow_jobs[@]} job(s): ${workflow_jobs[*]}"
    if grep -qE '^[[:space:]]+steps:[[:space:]]*$' "${workflow_path}"; then
      pass "${CI_WORKFLOW} declares a steps: block"
    else
      fail "${CI_WORKFLOW} declares a job with no steps: block"
    fi
  fi

  workflow_body="$(sed 's/#.*$//' "${workflow_path}")"
  for verb in "${REQUIRED_FAST_VERBS[@]}"; do
    if grep -qE "(^|[[:space:];&|])(\./)?build\.(sh|ps1)[[:space:]]+${verb}([[:space:]]|\$)" \
        <<<"${workflow_body}"; then
      pass "${CI_WORKFLOW} invokes ./build.sh ${verb}"
    else
      fail "${CI_WORKFLOW} never invokes ./build.sh ${verb}, which delivery-waves § Step 4 puts on the fast path"
    fi
  done
fi

# This gate runs negative controls in band (§ 7a), so its log contains failure-shaped text
# on a green run. Prove the marking that separates that text from genuine findings holds.
gate_assert_marking

gate_summary "verify-architecture" "${EXIT_VALIDATION}"
