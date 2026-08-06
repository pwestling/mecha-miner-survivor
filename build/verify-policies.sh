#!/usr/bin/env bash
#
# Proves that every repository build policy is enforced, by compiling a
# deliberately invalid fixture per policy and asserting the exact diagnostic.
#
# Authority: docs/technical/110-implementation-plan-for-ai-agents.md
#              § Concrete M0 bootstrap queue - TASK-FND-001-002 close evidence is
#              "deliberately invalid fixture proves each policy"
#            docs/technical/100-build-dependencies-and-release-operations.md
#              § C# project standards
# Requirements: TR-BLD-001
# Verification: VER-FND-001-006 through VER-FND-001-011
#
# Each fixture must FAIL with an `error <ID>` line. For CS8600 and CA2200,
# asserting `error` rather than `warning` is what proves Directory.Build.targets'
# TreatWarningsAsErrors is in force, because both are warnings by default.
# IDE1006 is NOT evidence of that: .editorconfig sets
# `dotnet_diagnostic.IDE1006.severity = error` directly, so the naming fixture
# fails with `error IDE1006` whether or not warnings are treated as errors. What
# the naming fixture proves is the .editorconfig naming policy itself plus
# EnforceCodeStyleInBuild, which is what VER-FND-001-008 claims.
#
# The fixtures are not part of MechaMiner.sln, so nothing here affects
# `dotnet build` or `dotnet test` of the product.
#
# Fixtures alone cannot prove a policy is on, because a fixture only measures the
# directory it sits in. The two policy-inheritance sections below close that gap;
# see the comment above them.
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
  "analyzer|CA2200|EnableNETAnalyzers at pinned AnalysisLevel|VER-FND-001-007"
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

# --- Policy inheritance: Directory.Build.* shadowing -------------------------
#
# MSBuild stops at the NEAREST Directory.Build.props / Directory.Build.targets
# found walking up from a project directory, and does NOT chain to the parent
# unless that file imports it explicitly. A project-local file therefore shadows
# the repository-root pair entirely rather than adding to it.
#
# That defeats every policy this script otherwise proves. Two concrete failures
# motivate the checks below:
#   * A project-local Directory.Build.targets makes the evaluated
#     TreatWarningsAsErrors 'false', so CS8600 and CS8602 compile as warnings and
#     the build succeeds - directly contradicting Directory.Build.targets' claim
#     that no work package can quietly opt its own project out.
#   * A shadowing build/policy-fixtures/unsafe/Directory.Build.props keeps the
#     unsafe fixture failing with CS0227 even when the real root policy has been
#     flipped to AllowUnsafeBlocks=true. The fixture then certifies a policy that
#     is switched off for the product, which is worse than having no fixture.
#
# Two independent guards, because either one alone can be evaded:
#   Guard 1 is structural - no file may shadow the root pair.
#   Guard 2 measures the evaluated property value per project, so a policy that
#   is off fails even if the file layout looks correct (a flipped root value is
#   invisible to Guard 1, and a shadow file that happens to set today's values is
#   invisible to Guard 2).

readonly ROOT_MSBUILD_FILES=(
  "Directory.Build.props"
  "Directory.Build.targets"
)

# Non-root Directory.Build.* files permitted to exist. Each must explicitly
# import the file it would otherwise shadow, and is verified to do so below.
# Adding an entry here must be a deliberate decision, not a side effect.
readonly ALLOWED_INTERMEDIATE_MSBUILD_FILES=(
  "build/policy-fixtures/Directory.Build.props"
)

# "<property>|<required evaluated value>" - the compiler policies doc 100
# § C# project standards requires, as they must evaluate for every project.
readonly EVALUATED_POLICIES=(
  "TreatWarningsAsErrors|true"
  "Nullable|enable"
  "AllowUnsafeBlocks|false"
  "LangVersion|12.0"
)

echo "=== policy inheritance: repository-root policy files exist"
for file in "${ROOT_MSBUILD_FILES[@]}"; do
  if [[ -f "${REPO_ROOT}/${file}" ]]; then
    pass "root policy file present: ${file}"
  else
    fail "root policy file missing: ${file}; every policy below depends on it"
  fi
done

echo
echo "=== policy inheritance: no Directory.Build.* file shadows the root pair"
found_msbuild_files=()
while IFS= read -r file; do
  found_msbuild_files+=("${file}")
done < <(cd "${REPO_ROOT}" && find . \
  \( -name 'Directory.Build.props' -o -name 'Directory.Build.targets' \) \
  -printf '%P\n' 2>/dev/null | sort)

# An empty result would mean the scan found nothing at all, including the root
# pair, so it must not be reported as compliance.
if [[ "${#found_msbuild_files[@]}" -lt "${#ROOT_MSBUILD_FILES[@]}" ]]; then
  fail "the Directory.Build.* scan found ${#found_msbuild_files[@]} file(s); it cannot see even the root pair, so it proves nothing"
fi

for file in "${found_msbuild_files[@]}"; do
  is_root=0
  for root_file in "${ROOT_MSBUILD_FILES[@]}"; do
    if [[ "${file}" == "${root_file}" ]]; then
      is_root=1
      break
    fi
  done
  if [[ "${is_root}" -eq 1 ]]; then
    continue
  fi

  is_allowed=0
  for allowed_file in "${ALLOWED_INTERMEDIATE_MSBUILD_FILES[@]}"; do
    if [[ "${file}" == "${allowed_file}" ]]; then
      is_allowed=1
      break
    fi
  done

  if [[ "${is_allowed}" -eq 0 ]]; then
    fail "shadowing MSBuild file ${file}: it replaces the repository-root policy for every project at or below $(dirname "${file}")/"
  elif grep -qE '<Import[[:space:]][^>]*Project="[^"]*Directory\.Build\.(props|targets)"' \
      "${REPO_ROOT}/${file}"; then
    pass "permitted intermediate file imports the root policy explicitly: ${file}"
  else
    fail "${file} is a permitted intermediate file but does not import the root policy file it shadows"
  fi
done

echo
echo "=== evaluated compiler policy per project (VER-FND-001-006 .. VER-FND-001-010)"
#
# Asserts the value MSBuild actually evaluates for each project, rather than
# trusting that the root file exists and therefore applies. This is the same
# shift as reading the resolved reference set instead of PackageReference: check
# the evaluated state, not the declaration believed to produce it.
#
# Scope: every product project (so a newly added one is covered automatically)
# plus the six policy fixtures by name. The fixtures are named rather than
# globbed so that fixture trees owned by other work packages, which may
# deliberately declare unrestorable references, are not evaluated here.

policy_projects=()
while IFS= read -r project; do
  policy_projects+=("${project}")
done < <(cd "${REPO_ROOT}" && find src tests game -name '*.csproj' \
  -not -path '*/obj/*' -not -path '*/bin/*' -print 2>/dev/null | sort)

for entry in "${NEGATIVE_FIXTURES[@]}"; do
  fixture="${entry%%|*}"
  policy_projects+=("build/policy-fixtures/${fixture}/${fixture}.csproj")
done
policy_projects+=("build/policy-fixtures/deterministic/deterministic.csproj")

readonly MINIMUM_POLICY_PROJECTS=15
if [[ "${#policy_projects[@]}" -lt "${MINIMUM_POLICY_PROJECTS}" ]]; then
  fail "evaluated-policy scan enumerated only ${#policy_projects[@]} project(s), fewer than the ${MINIMUM_POLICY_PROJECTS} accepted; the scan is not covering what it claims"
fi

evaluated_policies() {
  # $1 project. Prints "<property>=<value>" per line for EVALUATED_POLICIES.
  # Returns nonzero when MSBuild cannot evaluate the project or its output is not
  # the expected JSON, so the caller counts a validation failure instead of
  # comparing against an empty string (doc 100 reserves no exit class 1).
  local project="$1"
  local -a command=(dotnet msbuild "${REPO_ROOT}/${project}" -nologo)
  local policy
  for policy in "${EVALUATED_POLICIES[@]}"; do
    command+=("-getProperty:${policy%%|*}")
  done

  local output
  output="$("${command[@]}" 2>/dev/null)" || return 1
  printf '%s' "${output}" | python3 -c '
import json, sys
try:
    document = json.load(sys.stdin)
except ValueError:
    sys.exit(1)
properties = document.get("Properties")
if not isinstance(properties, dict) or not properties:
    sys.exit(1)
for name in sys.argv[1:]:
    if name not in properties:
        sys.exit(1)
    sys.stdout.write("%s=%s\n" % (name, properties[name]))
' "${EVALUATED_POLICIES[@]%%|*}"
}

for project in "${policy_projects[@]}"; do
  if [[ ! -f "${REPO_ROOT}/${project}" ]]; then
    fail "evaluated-policy scan: no project file at ${project}"
    continue
  fi

  if ! evaluated="$(evaluated_policies "${project}")"; then
    fail "${project}: MSBuild could not evaluate the compiler policy properties"
    continue
  fi

  project_failures=0
  for policy in "${EVALUATED_POLICIES[@]}"; do
    IFS='|' read -r property required <<<"${policy}"
    actual="$(printf '%s\n' "${evaluated}" | sed -n "s/^${property}=//p")"
    if [[ "${actual}" != "${required}" ]]; then
      fail "${project}: ${property} evaluates to '${actual}' but policy requires '${required}'"
      project_failures=$((project_failures + 1))
    fi
  done

  if [[ "${project_failures}" -eq 0 ]]; then
    pass "$(basename "${project}" .csproj): $(printf '%s' "${evaluated}" | paste -sd' ' -)"
  fi
done

echo
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
