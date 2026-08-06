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
# 4 validation failure.

set -euo pipefail

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
  # $1 project, $2 item name. Prints one Identity per line, sorted.
  dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getItem:$2" 2>/dev/null | python3 -c '
import json, sys
document = json.load(sys.stdin)
for identity in sorted(item["Identity"] for item in document.get("Items", {}).get(sys.argv[1], [])):
    if identity.strip():
        sys.stdout.write(identity + "\n")
' "$2"
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
  printf '%s' "${base%.csproj}"
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
actual_solution="$(cd "${REPO_ROOT}" && dotnet sln MechaMiner.sln list \
  | grep -E '\.csproj$' | tr '\\' '/' | sort)"
expected_solution="$(printf '%s\n' "${EXPECTED_PROJECTS[@]}" | cut -d'|' -f1 | sort)"
if [[ "${actual_solution}" == "${expected_solution}" ]]; then
  pass "MechaMiner.sln references exactly the 9 accepted projects"
else
  fail "MechaMiner.sln project set differs from the accepted decomposition"
  diff <(printf '%s\n' "${expected_solution}") <(printf '%s\n' "${actual_solution}") || true
fi

section "3. project reference edges match the accepted boundary (VER-FND-001-004)"
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

section "6. no Godot types outside game/ (VER-FND-001-004)"
assert_absent_pattern \
  "no C# file under src/ or tests/ imports Godot" \
  '(^|[^A-Za-z.])using[[:space:]]+Godot([;.]|$)' \
  '*.cs' \
  src tests

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
