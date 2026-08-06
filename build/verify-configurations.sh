#!/usr/bin/env bash
#
# Proves the resolution of the build-configuration conflict between doc 100 and
# Godot.NET.Sdk.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Build configurations
#            Godot.NET.Sdk/4.7.1 Sdk.props, which declares
#              Configurations=Debug;ExportDebug;ExportRelease unconditionally
# Requirements: TR-BLD-001, TR-BLD-003
# Verification: VER-FND-002-015
#
# The conflict: doc 100 names three configurations - Debug, Development, Release -
# and Godot.NET.Sdk defines its own three. Godot's export tooling only ever asks for
# its own three names (the editor builds Debug; an export preset's "export with
# debug" flag selects ExportDebug or ExportRelease), so a fourth MSBuild
# configuration named Development would be one that no Godot export could produce.
#
# The resolution is a 1:1 mapping. This script asserts every part of it:
#
#   doc 100 name  MSBuild identity  Optimize  diagnostic symbol
#   Debug         Debug             false     MECHAMINER_DEBUG
#   Development   ExportDebug       true      MECHAMINER_DEVELOPMENT
#   Release       ExportRelease     true      MECHAMINER_RELEASE
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly EXIT_VALIDATION=4

# "<doc 100 name>|<MSBuild identity>|<expected Optimize>|<expected symbol>"
readonly CONFIGURATION_MAP=(
  "debug|Debug|false|MECHAMINER_DEBUG"
  "development|ExportDebug|true|MECHAMINER_DEVELOPMENT"
  "release|ExportRelease|true|MECHAMINER_RELEASE"
)

readonly PROBED_PROJECTS=(
  "src/MechaMiner.Content/MechaMiner.Content.csproj"
  "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj"
  "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj"
  "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj"
  "src/MechaMiner.Tools/MechaMiner.Tools.csproj"
  "game/MechaMiner.Game.csproj"
)

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

msbuild_property() {
  # $1 project, $2 configuration, $3 property
  dotnet msbuild "${REPO_ROOT}/$1" -nologo "-p:Configuration=$2" "-getProperty:$3" 2>/dev/null | tr -d '\n\r '
}

section "1. the Godot SDK's own configuration set is what every project declares"
godot_sdk_set="$(grep -oE '<Configurations>[^<]+</Configurations>' \
  "${HOME}/.nuget/packages/godot.net.sdk/4.7.1/Sdk/Sdk.props" 2>/dev/null \
  | sed -E 's|</?Configurations>||g' || true)"
if [[ -z "${godot_sdk_set}" ]]; then
  godot_sdk_set="$(msbuild_property game/MechaMiner.Game.csproj Debug Configurations)"
  echo "      (read from the evaluated project; the SDK package is not in the local cache)"
fi
echo "      Godot.NET.Sdk declares: ${godot_sdk_set}"

for project in "${PROBED_PROJECTS[@]}"; do
  declared="$(msbuild_property "${project}" Debug Configurations)"
  if [[ "${declared}" == "Debug;ExportDebug;ExportRelease" ]]; then
    pass "$(basename "${project}") declares ${declared}"
  else
    fail "$(basename "${project}") declares '${declared}', expected Debug;ExportDebug;ExportRelease"
  fi
done

section "2. every configuration carries doc 100's optimization and one diagnostic symbol"
#
# Emits one problem per line for a given (Optimize, DefineConstants) reading. Factored out
# so § 2b's controls drive the identical predicate over injected values rather than a
# paraphrase of it, and so the arity rule below has something a control can attack.
configuration_problems() {
  local optimize="$1" constants="$2" expected_optimize="$3" expected_symbol="$4"
  local symbol_count

  [[ "${optimize}" == "${expected_optimize}" ]] \
    || printf '%s\n' "Optimize=${optimize}, expected ${expected_optimize}"
  grep -q "${expected_symbol}" <<<"${constants}" \
    || printf '%s\n' "${expected_symbol} not defined"

  # Exactly one MECHAMINER_* configuration symbol may be defined at a time,
  # otherwise conditional code could compile for two configurations at once. `grep -c`
  # reads to EOF, so this pipeline has no early-exit reader and no SIGPIPE to take.
  symbol_count="$(printf '%s' "${constants}" | tr ';' '\n' | grep -c '^MECHAMINER_' || true)"
  [[ "${symbol_count}" -eq 1 ]] \
    || printf '%s\n' "${symbol_count} MECHAMINER_* symbols defined, expected exactly 1"

  return 0
}

for entry in "${CONFIGURATION_MAP[@]}"; do
  IFS='|' read -r workflow msbuild expected_optimize expected_symbol <<<"${entry}"
  for project in "${PROBED_PROJECTS[@]}"; do
    optimize="$(msbuild_property "${project}" "${msbuild}" Optimize)"
    constants="$(msbuild_property "${project}" "${msbuild}" DefineConstants)"

    mapfile -t problems < <(configuration_problems \
      "${optimize}" "${constants}" "${expected_optimize}" "${expected_symbol}")

    if [[ "${#problems[@]}" -eq 0 ]]; then
      pass "${workflow} -> ${msbuild}: $(basename "${project}") Optimize=${optimize}, ${expected_symbol}"
    else
      fail "${workflow} -> ${msbuild}: $(basename "${project}"): $(printf '%s; ' "${problems[@]}")"
    fi
  done
done

section "2b. negative controls: § 2 can fail, including on arity (Decision 11 rule 4)"
#
# § 2 asserts three things about a reading it takes from MSBuild, and until now nothing
# showed any of them failing. The third is an ARITY rule - exactly one MECHAMINER_* symbol
# - and doc 91 § Negative control adequacy is explicit that arity survives every reach
# attack, so it needs "a control containing two of the guarded thing where only one
# satisfies the rule". Control 3 below is that control.
#
# The readings are injected directly. Nothing is written, no project is edited and no
# MSBuild run is perturbed, so a red result cannot be a broken environment.
#
# "<label>|<Optimize>|<DefineConstants>|<expected problem count>"
readonly CONFIGURATION_CONTROLS=(
  "the real Debug reading|false|TRACE;DEBUG;MECHAMINER_DEBUG;GODOT|0"
  "wrong Optimize|true|TRACE;DEBUG;MECHAMINER_DEBUG;GODOT|1"
  "two configuration symbols at once (arity)|false|TRACE;MECHAMINER_DEBUG;MECHAMINER_RELEASE;GODOT|1"
  "no configuration symbol at all|false|TRACE;DEBUG;GODOT|2"
)

for control in "${CONFIGURATION_CONTROLS[@]}"; do
  IFS='|' read -r label control_optimize control_constants expected_count <<<"${control}"
  mapfile -t control_problems < <(configuration_problems \
    "${control_optimize}" "${control_constants}" false MECHAMINER_DEBUG)
  if [[ "${#control_problems[@]}" -eq "${expected_count}" ]]; then
    control_pass "§ 2b control: ${label} -> ${#control_problems[@]} problem(s), as required${control_problems[0]:+ (${control_problems[*]})}"
  else
    control_fail "§ 2b control: ${label} -> ${#control_problems[@]} problem(s) (${control_problems[*]-none}), expected ${expected_count}"
  fi
done

section "3. the solution builds cleanly in all three configurations"
for entry in "${CONFIGURATION_MAP[@]}"; do
  IFS='|' read -r workflow msbuild _optimize _symbol <<<"${entry}"
  output="$("${WRAPPER}" build --configuration "${workflow}" 2>&1)"
  status=$?
  if [[ "${status}" -eq 0 ]] \
      && grep -q "MSBuild ${msbuild}" <<<"${output}" \
      && grep -q '0 warning(s) and 0 error(s)' <<<"${output}"; then
    pass "build --configuration ${workflow} built MSBuild ${msbuild} with 0 warnings, 0 errors"
  else
    fail "build --configuration ${workflow} exited ${status}"
    printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
  fi
done

section "4. no committed lock file changed (restore stays configuration-independent)"
#
# The exit status is checked before the output is interpreted: a FAILED `git status`
# returns nothing, and nothing is exactly what an unchanged tree returns. Suppressing it
# with `2>/dev/null || true` made this assertion pass under a broken or absent git
# without any lock file having been compared.
#
# The status check alone was still not enough. This section asserted only that
# `git status --porcelain -- '*packages.lock.json'` reports nothing changed, and never
# that any lock file EXISTS. An empty pathspec reports nothing changed, so deleting all
# nine lock files satisfied it - the strongest possible violation of
# configuration-independent restore passed the assertion written to catch drift in it.
# All nine are committed today, so the section held in fact and not by construction,
# which is Decision 11 rule 2: an empty candidate set never satisfies a gate.
#
# The fix needs three anchors, not two, because doc 91 § Negative control adequacy notes
# that "an invariant asserting that two sets match is blind to a correlated deletion from
# both sides": deleting a project AND its lock file together keeps the two sets equal. So
# the count is asserted against EXPECTED_LOCK_FILE_COUNT, a literal independent of both.
# Twelve, not nine, from the FND-004/FND-009 merge: src/MechaMiner.Diagnostics,
# tests/MechaMiner.Diagnostics.Tests and tests/MechaMiner.Tools.Tests are three new
# accepted projects and each carries a committed lock file. The literal fired on all three
# by name, which is the third anchor doing exactly its job - a set-equality check alone
# would have been blind to it. Adding a project means editing this number and the list
# below in the same change; that is the cost of the anchor and it is the point of it.
readonly EXPECTED_LOCK_FILE_COUNT=12

# The project set this section requires a lock file for: every project probed above, plus
# the six test projects, which restore under the same three configurations.
readonly LOCK_FILE_PROJECT_DIRECTORIES=(
  "src/MechaMiner.Content"
  "src/MechaMiner.Diagnostics"
  "src/MechaMiner.Simulation"
  "src/MechaMiner.Persistence"
  "src/MechaMiner.Tools"
  "game"
  "tests/MechaMiner.Content.Tests"
  "tests/MechaMiner.Diagnostics.Tests"
  "tests/MechaMiner.Simulation.Tests"
  "tests/MechaMiner.Persistence.Tests"
  "tests/MechaMiner.Tools.Tests"
  "tests/MechaMiner.Game.Tests"
)

# Emits one problem per line; nothing when the lock-file set is exactly the project set
# and carries the expected number of members. Factored out so § 4b drives the identical
# predicate over an injected removal.
lock_file_set_problems() {
  local -a directories=("$@")
  local tracked tracked_status=0 directory expected actual_count

  tracked="$(cd "${REPO_ROOT}" && git ls-files -- '*packages.lock.json' 2>&1)" \
    || tracked_status=$?
  if [[ "${tracked_status}" -ne 0 ]]; then
    printf '%s\n' "could not enumerate lock files (git ls-files exit ${tracked_status}): ${tracked}"
    return 0
  fi

  actual_count=0
  [[ -z "${tracked}" ]] || actual_count="$(grep -c . <<<"${tracked}")"

  # Anchor 1: non-empty, and the number the repository is supposed to have.
  if [[ "${actual_count}" -ne "${EXPECTED_LOCK_FILE_COUNT}" ]]; then
    printf '%s\n' "${actual_count} tracked packages.lock.json file(s), expected ${EXPECTED_LOCK_FILE_COUNT}"
  fi

  # Anchor 2: every project in the set has one, tracked AND on disk. Both are required:
  # a tracked-but-deleted file is what `git status` would report as a change, and a
  # present-but-untracked one is not committed and so is not what restore is pinned by.
  for directory in "${directories[@]}"; do
    expected="${directory}/packages.lock.json"
    grep -qxF "${expected}" <<<"${tracked}" \
      || printf '%s\n' "${expected} is not tracked"
    [[ -f "${REPO_ROOT}/${expected}" ]] \
      || printf '%s\n' "${expected} is not present on disk"
  done

  # Anchor 3: nothing tracked outside the named set, so a lock file for a project that is
  # not in the accepted decomposition is a finding rather than silent extra coverage.
  while IFS= read -r tracked_path; do
    [[ -n "${tracked_path}" ]] || continue
    local known=0
    for directory in "${directories[@]}"; do
      [[ "${tracked_path}" == "${directory}/packages.lock.json" ]] && known=1
    done
    [[ "${known}" -eq 1 ]] || printf '%s\n' "${tracked_path} is tracked but its project is not in the accepted set"
  done <<<"${tracked}"

  return 0
}

# The literal and the list encode the same fact, and that is a drift risk - so it is
# asserted rather than left to memory. What it is NOT is a reason to derive the literal
# from the list: the list is one of the two sets anchors 2 and 3 compare, so a count taken
# from it agrees with it by construction and anchor 1 stops being an independent anchor.
# That would reopen exactly the hole the comment above names from doc 91 § Negative control
# adequacy - deleting a project, its lock file and its list entry together keeps both sets
# equal and would then satisfy all three checks. The independent third artifact here is the
# literal, which is the role a committed manifest plays in the content gate; the list is
# the analogue of that gate's scan, not of its manifest.
#
# So both stay, and disagreement between them is a named failure instead of a silent one.
# Adding a project now costs three edits - the list, this literal, and the lock file - and
# the ceiling is stated: three consistent edits still pass, which makes an ACCIDENTAL
# change loud and is not evidence the set is the right size.
if [[ "${EXPECTED_LOCK_FILE_COUNT}" -ne "${#LOCK_FILE_PROJECT_DIRECTORIES[@]}" ]]; then
  fail "EXPECTED_LOCK_FILE_COUNT is ${EXPECTED_LOCK_FILE_COUNT} but LOCK_FILE_PROJECT_DIRECTORIES lists ${#LOCK_FILE_PROJECT_DIRECTORIES[@]}; the two encode one fact and have drifted, so § 4's count anchor is measuring a set nobody declared"
else
  pass "the count anchor (${EXPECTED_LOCK_FILE_COUNT}) and the accepted project list agree, so anchor 1 is independent of the two sets without being stale"
fi

mapfile -t lock_set_problems < <(lock_file_set_problems "${LOCK_FILE_PROJECT_DIRECTORIES[@]}")
if [[ "${#lock_set_problems[@]}" -eq 0 ]]; then
  pass "the lock-file set is exactly the ${EXPECTED_LOCK_FILE_COUNT} accepted projects, so § 4's drift check has something to compare"
else
  fail "the lock-file set is not the accepted project set: $(printf '%s; ' "${lock_set_problems[@]}")"
fi

changed=""
changed_status=0
changed="$(cd "${REPO_ROOT}" && git status --porcelain -- '*packages.lock.json' 2>&1)" \
  || changed_status=$?
if [[ "${changed_status}" -ne 0 ]]; then
  fail "could not read git status for lock files (exit ${changed_status}), so drift is unproved rather than absent: ${changed}"
elif [[ -z "${changed}" ]]; then
  pass "every packages.lock.json is unchanged after building all three configurations"
else
  fail "building the three configurations rewrote lock file(s):"
  printf '%s\n' "${changed}" | sed 's/^/      /'
fi

section "4b. negative control: § 4 notices a removed lock file (Decision 11 rules 2 and 4)"
#
# The control drives the identical predicate with one member removed from the project set
# it is asked about, which is what a deleted lock file looks like to the assertion. It is
# a coherent violation and not a broken state: nothing is deleted from the working tree,
# no subprocess is made to fail, and the repository is not touched, so a red result here
# can only mean the predicate saw the removal.
# Control 1 attacks anchor 3, the "tracked but not in the accepted set" direction: the
# project set is shortened while the tree keeps all nine files.
control_directories=("${LOCK_FILE_PROJECT_DIRECTORIES[@]:1}")
mapfile -t removed_problems < <(lock_file_set_problems "${control_directories[@]}")
if [[ "${#removed_problems[@]}" -gt 0 ]]; then
  control_pass "§ 4b control: a project set that drops ${LOCK_FILE_PROJECT_DIRECTORIES[0]} is reported: $(printf '%s; ' "${removed_problems[@]}")"
else
  control_fail "§ 4b control: a project set that drops ${LOCK_FILE_PROJECT_DIRECTORIES[0]} was accepted; § 4 still passes on a mismatched set"
fi

# Control 2 attacks anchor 2, the "a project in the set has no lock file" direction -
# which is the shape a deletion actually takes, and the one the old § 4 passed on. A
# directory that carries no packages.lock.json stands in for a deleted one; nothing is
# removed from the working tree.
mapfile -t absent_problems < <(lock_file_set_problems \
  "${LOCK_FILE_PROJECT_DIRECTORIES[@]}" "src/MechaMiner.NoSuchProject")
absent_reported=0
for problem in "${absent_problems[@]-}"; do
  [[ "${problem}" == *"src/MechaMiner.NoSuchProject/packages.lock.json is not tracked"* ]] && absent_reported=1
  [[ "${problem}" == *"src/MechaMiner.NoSuchProject/packages.lock.json is not present on disk"* ]] && absent_reported=1
done
if [[ "${absent_reported}" -eq 1 ]]; then
  control_pass "§ 4b control: a project with no lock file is reported as missing one, not skipped: $(printf '%s; ' "${absent_problems[@]}")"
else
  control_fail "§ 4b control: a project with no lock file was skipped rather than reported; § 4 would still pass on a deletion. Reported: $(printf '%s; ' "${absent_problems[@]-none}")"
fi

# The count anchor on its own, so a correlated deletion from both sides is covered: if the
# project set and the tracked set were both shortened, anchors 2 and 3 would agree and
# only the literal count would object.
control_count_problems=0
if [[ "$(cd "${REPO_ROOT}" && git ls-files -- '*packages.lock.json' | grep -c .)" -ne "${EXPECTED_LOCK_FILE_COUNT}" ]]; then
  control_count_problems=1
fi
if [[ "${control_count_problems}" -eq 0 ]]; then
  control_pass "the count anchor is a literal ${EXPECTED_LOCK_FILE_COUNT}, independent of both sets, so a correlated deletion cannot satisfy § 4"
else
  control_fail "the count anchor disagrees with the tracked set; § 4's third anchor is wrong, not the tree"
fi

section "5. no configuration is silently dropped: three names in, three names out"
mapfile -t allowed < <("${WRAPPER}" build --configuration invalid 2>&1 \
  | sed -n 's/.*must be one of \[\([^]]*\)\].*/\1/p' \
  | tr ',' '\n' | tr -d ' ')
if [[ "${#allowed[@]}" -eq 3 ]] \
    && [[ "${allowed[0]}" == "debug" ]] \
    && [[ "${allowed[1]}" == "development" ]] \
    && [[ "${allowed[2]}" == "release" ]]; then
  pass "the wrapper accepts exactly doc 100's three configuration names: ${allowed[*]}"
else
  fail "the wrapper's configuration vocabulary is not doc 100's three names: ${allowed[*]:-none}"
fi

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-configurations" "${EXIT_VALIDATION}"
