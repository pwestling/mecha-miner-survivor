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

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

msbuild_property() {
  # $1 project, $2 configuration, $3 property
  dotnet msbuild "${REPO_ROOT}/$1" -nologo "-p:Configuration=$2" "-getProperty:$3" 2>/dev/null | tr -d '\n\r '
}

echo "=== 1. the Godot SDK's own configuration set is what every project declares"
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

echo
echo "=== 2. every configuration carries doc 100's optimization and one diagnostic symbol"
for entry in "${CONFIGURATION_MAP[@]}"; do
  IFS='|' read -r workflow msbuild expected_optimize expected_symbol <<<"${entry}"
  for project in "${PROBED_PROJECTS[@]}"; do
    optimize="$(msbuild_property "${project}" "${msbuild}" Optimize)"
    constants="$(msbuild_property "${project}" "${msbuild}" DefineConstants)"

    problems=()
    [[ "${optimize}" == "${expected_optimize}" ]] \
      || problems+=("Optimize=${optimize}, expected ${expected_optimize}")
    printf '%s' "${constants}" | grep -q "${expected_symbol}" \
      || problems+=("${expected_symbol} not defined")

    # Exactly one MECHAMINER_* configuration symbol may be defined at a time,
    # otherwise conditional code could compile for two configurations at once.
    symbol_count="$(printf '%s' "${constants}" | tr ';' '\n' | grep -c '^MECHAMINER_' || true)"
    [[ "${symbol_count}" -eq 1 ]] \
      || problems+=("${symbol_count} MECHAMINER_* symbols defined, expected exactly 1")

    if [[ "${#problems[@]}" -eq 0 ]]; then
      pass "${workflow} -> ${msbuild}: $(basename "${project}") Optimize=${optimize}, ${expected_symbol}"
    else
      fail "${workflow} -> ${msbuild}: $(basename "${project}"): $(printf '%s; ' "${problems[@]}")"
    fi
  done
done

echo
echo "=== 3. the solution builds cleanly in all three configurations"
for entry in "${CONFIGURATION_MAP[@]}"; do
  IFS='|' read -r workflow msbuild _optimize _symbol <<<"${entry}"
  output="$("${WRAPPER}" build --configuration "${workflow}" 2>&1)"
  status=$?
  if [[ "${status}" -eq 0 ]] \
      && printf '%s' "${output}" | grep -q "MSBuild ${msbuild}" \
      && printf '%s' "${output}" | grep -q '0 warning(s) and 0 error(s)'; then
    pass "build --configuration ${workflow} built MSBuild ${msbuild} with 0 warnings, 0 errors"
  else
    fail "build --configuration ${workflow} exited ${status}"
    printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
  fi
done

echo
echo "=== 4. no committed lock file changed (restore stays configuration-independent)"
#
# The exit status is checked before the output is interpreted: a FAILED `git status`
# returns nothing, and nothing is exactly what an unchanged tree returns. Suppressing it
# with `2>/dev/null || true` made this assertion pass under a broken or absent git
# without any lock file having been compared.
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

echo
echo "=== 5. no configuration is silently dropped: three names in, three names out"
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

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-configurations: PASS"
  exit 0
fi
echo "verify-configurations: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
