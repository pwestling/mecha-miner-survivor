#!/usr/bin/env bash
#
# Proves that the six build policies VER-FND-001-006 through VER-FND-001-011 name are
# enforced, by compiling a deliberately invalid fixture per policy and asserting the
# exact diagnostic.
#
# NOT every repository build policy. Doc 100 § C# project standards lists eight bullets
# and two of them have no fixture here at all: "Release binaries do not include
# development cheats or arbitrary command execution" and "Reflection-based gameplay
# registration and runtime assembly scanning are avoided". Neither has a subject yet -
# there are no release binaries and no gameplay registration - and they belong to
# FND-006 and DAT-006. Several Directory.Build.props properties are likewise declared
# and unfixtured: ImplicitUsings, RollForward, IsPackable, DebugType,
# RestorePackagesWithLockFile, and the deliberately empty WarningsNotAsErrors and
# NoWarn. Formatting (IDE0055/IDE0005) is build/verify-format.sh's, not this script's.
# A reader who takes this script as the repository's whole policy gate will believe
# eight bullets are covered when six are.
#
# Authority: docs/technical/110-implementation-plan-for-ai-agents.md
#              § Concrete M0 bootstrap queue - TASK-FND-001-002 close evidence is
#              "deliberately invalid fixture proves each policy"
#            docs/technical/100-build-dependencies-and-release-operations.md
#              § C# project standards
# Requirements: TR-BLD-001
# Verification: VER-FND-001-006 through VER-FND-001-011
#
# Each fixture must FAIL with an `error <ID>` line. What that `error` proves is NOT
# the same for every fixture, and the distinction matters because a fixture credited
# with proving a policy it does not exercise leaves that policy ungated:
#
#   - CS8600 (nullable) and CA2200 (analyzer) are warnings by default and carry no
#     severity override in .editorconfig. For those two, `error` rather than `warning`
#     is exactly what proves Directory.Build.targets' TreatWarningsAsErrors is in
#     force. Rebuild either fixture with -p:TreatWarningsAsErrors=false and the
#     diagnostic downgrades to `warning` and the build succeeds.
#   - IDE1006 (naming) does NOT prove that. .editorconfig sets
#     `dotnet_diagnostic.IDE1006.severity = error` explicitly, so it is an error on its
#     own authority: rebuild the naming fixture with -p:TreatWarningsAsErrors=false and
#     it still reports `error IDE1006`. What that fixture proves is the pair actually
#     under test - the .editorconfig naming rules plus EnforceCodeStyleInBuild, without
#     which an IDE-prefixed style rule is not evaluated during a command-line build at
#     any severity. That is what VER-FND-001-008 is for, and it is a real policy.
#   - CS9202 (langversion) and CS0227 (unsafe) are errors by default, so they prove the
#     property they name and say nothing about severity escalation either.
#   - CA2200 (analyzer) proves EnableNETAnalyzers and nothing about the AnalysisLevel
#     pin. Measured: rebuild the analyzer fixture with -p:AnalysisLevel=latest, =9.0, or
#     even =none and it still reports `error CA2200`; only
#     -p:EnableNETAnalyzers=false makes it compile. CA2200 is in the default rule set at
#     every analysis level, so it cannot distinguish one level from another. The row
#     below therefore names EnableNETAnalyzers alone. `AnalysisLevel=8.0` and
#     `AnalysisMode=Default` in Directory.Build.props are declared and unfixtured: no
#     gate would notice them floating with the SDK, which is exactly what pinning them
#     was for. A fixture that can tell the levels apart needs a diagnostic introduced
#     after 8.0.
#
# In short: TreatWarningsAsErrors is gated by VER-FND-001-006 and VER-FND-001-007 only.
# Attributing it to the naming fixture as well overstated the coverage of a policy that
# would otherwise have had no negative fixture at all.
#
# The fixtures are not part of MechaMiner.sln, so nothing here affects
# `dotnet build` or `dotnet test` of the product.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly FIXTURE_ROOT="${REPO_ROOT}/build/policy-fixtures"
readonly EXIT_VALIDATION=4

# "<fixture directory>|<expected diagnostic id>|<policy>|<verification id>"
readonly NEGATIVE_FIXTURES=(
  "nullable|CS8600|Nullable=enable plus warnings-as-errors|VER-FND-001-006"
  "analyzer|CA2200|EnableNETAnalyzers (the AnalysisLevel pin is unfixtured)|VER-FND-001-007"
  "naming|IDE1006|.editorconfig naming with EnforceCodeStyleInBuild|VER-FND-001-008"
  "langversion|CS9202|LangVersion pinned to 12.0|VER-FND-001-009"
  "unsafe|CS0227|AllowUnsafeBlocks=false|VER-FND-001-010"
)

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

clean_fixture() {
  rm -rf "${FIXTURE_ROOT}/$1/obj" "${FIXTURE_ROOT}/$1/bin"
}

for entry in "${NEGATIVE_FIXTURES[@]}"; do
  IFS='|' read -r fixture diagnostic policy verification <<<"${entry}"
  project="${FIXTURE_ROOT}/${fixture}/${fixture}.csproj"

  echo "=== ${verification}: ${policy}"
  if [[ ! -f "${project}" ]]; then
    fail "${verification}: fixture project missing at ${project}"
    echo
    continue
  fi

  clean_fixture "${fixture}"
  output="$(dotnet build "${project}" --nologo -v m 2>&1)"
  status=$?

  if [[ "${status}" -eq 0 ]]; then
    fail "${verification}: ${fixture} compiled successfully; the policy is NOT enforced"
    echo
    continue
  fi

  matched="$(printf '%s\n' "${output}" | grep -oE ": error ${diagnostic}:.*" | head -1)"
  if [[ -n "${matched}" ]]; then
    pass "${fixture} failed with exit ${status} and the expected diagnostic"
    printf '      %s\n' "${matched}"
  else
    fail "${verification}: ${fixture} failed with exit ${status} but not with error ${diagnostic}"
    printf '%s\n' "${output}" | grep -E ': (error|warning) ' | sort -u | sed 's/^/      /'
  fi
  echo
done

# --- VER-FND-001-011: deterministic build -----------------------------------
#
# The positive case (policy value Deterministic=true) must produce byte-identical
# assemblies across two independent clean builds. The negative control
# (Deterministic=false) must NOT, otherwise the assertion above would be
# vacuously true regardless of the policy.

echo "=== VER-FND-001-011: Deterministic=true"
readonly DETERMINISTIC_ASSEMBLY="${FIXTURE_ROOT}/deterministic/bin/Debug/net8.0/deterministic.dll"

hash_deterministic_build() {
  # $1 = value for the Deterministic property. Prints the SHA-256 of the output.
  clean_fixture deterministic
  if ! dotnet build "${FIXTURE_ROOT}/deterministic/deterministic.csproj" \
      --nologo -v q "-p:Deterministic=$1" >/dev/null 2>&1; then
    printf 'BUILD-FAILED'
    return
  fi
  sha256sum "${DETERMINISTIC_ASSEMBLY}" | cut -d' ' -f1
}

first_hash="$(hash_deterministic_build true)"
second_hash="$(hash_deterministic_build true)"
if [[ "${first_hash}" == "BUILD-FAILED" || "${second_hash}" == "BUILD-FAILED" ]]; then
  fail "VER-FND-001-011: the deterministic fixture must compile but did not"
elif [[ "${first_hash}" == "${second_hash}" ]]; then
  pass "two clean builds are byte-identical: sha256 ${first_hash}"
else
  fail "VER-FND-001-011: rebuild differed (${first_hash} vs ${second_hash})"
fi

echo
echo "=== VER-FND-001-011 negative control: Deterministic=false"
third_hash="$(hash_deterministic_build false)"
fourth_hash="$(hash_deterministic_build false)"
if [[ "${third_hash}" == "BUILD-FAILED" || "${fourth_hash}" == "BUILD-FAILED" ]]; then
  fail "VER-FND-001-011: negative-control build failed"
elif [[ "${third_hash}" != "${fourth_hash}" ]]; then
  pass "nondeterministic builds differ as expected (${third_hash:0:16}... vs ${fourth_hash:0:16}...)"
else
  fail "VER-FND-001-011: Deterministic=false still produced identical output; the check proves nothing"
fi
clean_fixture deterministic

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-policies: PASS"
  exit 0
fi
echo "verify-policies: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
