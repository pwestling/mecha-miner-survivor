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
# directory it sits in. The three policy-inheritance guards below close that gap;
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
# Three independent guards, because no one of them is sufficient:
#   Guard 1 is structural - no file may shadow the root pair.
#   Guard 2 asserts, per project, that MSBuild actually imported the root pair,
#   read out of MSBuild's own evaluated import graph.
#   Guard 3 measures the evaluated property value per project, so a policy that
#   is off fails even if the file layout looks correct (a flipped root value is
#   invisible to Guards 1 and 2, and a shadow file that happens to set today's
#   values is invisible to Guard 3).
#
# Guard 2 exists because Guard 1's allowlist entry for
# build/policy-fixtures/Directory.Build.props used to be checked by grepping that
# file for the TEXT of an <Import ... Directory.Build.props">. Two files passed
# that grep while importing nothing:
#   * <Import Project="nowhere/Directory.Build.props"
#            Condition="Exists('nowhere/Directory.Build.props')" /> - the text is
#     present, the condition is false, so no import happens.
#   * The real import inside an XML comment - grep does not parse XML.
# With either in place plus a local copy of the property values Guard 3 checks,
# the whole gate printed PASS while the fixtures measured a private copy of the
# policy rather than the policy the product builds under. Guard 2 therefore asks
# MSBuild what it imported instead of reading what a file says.

readonly ROOT_MSBUILD_FILES=(
  "Directory.Build.props"
  "Directory.Build.targets"
)

# Non-root Directory.Build.* files permitted to exist. Listing a file here only
# says it may exist; it grants no exemption from anything. Guard 2 still requires
# MSBuild to have imported the root pair when evaluating every project below it,
# so an entry here cannot be used to decouple a subtree from the root policy.
# Adding an entry must be a deliberate decision, not a side effect.
readonly ALLOWED_INTERMEDIATE_MSBUILD_FILES=(
  "build/policy-fixtures/Directory.Build.props"
)

# "<property>|<required evaluated value>" - every property a fixture below relies
# on, because a fixture proves only that the policy holds where the fixture sits.
# The list is derived by asking, per fixture, "which evaluated properties must
# hold for this diagnostic to appear at all", not by picking the properties that
# felt important:
#
#   nullable      CS8600 as error   Nullable, TreatWarningsAsErrors
#   analyzer      CA2200 as error   EnableNETAnalyzers, AnalysisLevel,
#                                   AnalysisMode, TreatWarningsAsErrors
#   naming        IDE1006 as error  EnforceCodeStyleInBuild (+ .editorconfig,
#                                   see the gap noted below)
#   langversion   CS9202            LangVersion
#   unsafe        CS0227            AllowUnsafeBlocks
#   deterministic byte-identical    Deterministic
#   all of them   "error", not      WarningsNotAsErrors and NoWarn must stay
#                 "warning"         empty, as Directory.Build.targets declares
#
# EnableNETAnalyzers, AnalysisLevel, AnalysisMode, EnforceCodeStyleInBuild and
# Deterministic were previously absent, and that reopened the exact defect the
# inheritance guards exist to close: the permitted intermediate file imported the
# root policy honestly and then set EnableNETAnalyzers and EnforceCodeStyleInBuild
# back to true locally, while the root file had both switched off. All three
# guards were green, VER-FND-001-007 and -008 reported ok off the fixtures, and a
# product file with a CA2200 rethrow and an IDE1006 field name compiled at
# 0 warnings. WarningsNotAsErrors and NoWarn are here for the same reason: a root
# NoWarn covering CS8600/CA2200/IDE1006 is invisible to a fixture that has a local
# empty NoWarn.
#
# KNOWN GAP, not closed here: the naming fixture also depends on the root
# .editorconfig's `dotnet_diagnostic.IDE1006.severity = error`. That is not an
# MSBuild property and is not reachable through an evaluated item either - the
# EditorConfigFiles item holds only the SDK-generated file, because Roslyn, not
# MSBuild, walks the .editorconfig chain and applies `root = true`. A project-local
# `.editorconfig` with `root = true` therefore still decouples that half of
# VER-FND-001-008. Closing it needs an .editorconfig shadowing guard of its own.
readonly EVALUATED_POLICIES=(
  "TreatWarningsAsErrors|true"
  "WarningsNotAsErrors|"
  "NoWarn|"
  "Nullable|enable"
  "AllowUnsafeBlocks|false"
  "LangVersion|12.0"
  "EnableNETAnalyzers|true"
  "AnalysisLevel|8.0"
  "AnalysisMode|Default"
  "EnforceCodeStyleInBuild|true"
  "Deterministic|true"
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
echo "=== policy scope: the projects every policy below must cover"
#
# Enumerated once, here, because all three inheritance guards and the evaluated
# policy check must cover the same set. Scope: every product project (so a newly
# added one is covered automatically) plus the policy fixtures. The fixtures are
# enumerated rather than globbed so that fixture trees owned by other work
# packages, which may deliberately declare unrestorable references, are not
# evaluated here - and the enumeration is asserted to be a partition below, so an
# unenumerated fixture is a failure rather than an exemption.

product_projects=()
while IFS= read -r project; do
  product_projects+=("${project}")
done < <(cd "${REPO_ROOT}" && find src tests game -name '*.csproj' \
  -not -path '*/obj/*' -not -path '*/bin/*' -print 2>/dev/null | sort)

fixture_projects=()
for entry in "${NEGATIVE_FIXTURES[@]}"; do
  fixture="${entry%%|*}"
  fixture_projects+=("build/policy-fixtures/${fixture}/${fixture}.csproj")
done
fixture_projects+=("build/policy-fixtures/deterministic/deterministic.csproj")

policy_projects=("${product_projects[@]}" "${fixture_projects[@]}")

# The fixture enumeration must be a PARTITION of build/policy-fixtures/, in both
# directions. A floor ("at least N projects in scope") is not one:
# build/policy-fixtures/seventh/seventh.csproj with AllowUnsafeBlocks=true,
# Nullable=disable and LangVersion=latest compiled pointer-dereferencing unsafe
# code and both verify-policies.sh and verify-architecture.sh exited 0, because
# nothing asserted that the enumerated fixtures are ALL the fixtures.
#
#   found but not enumerated  -> a project builds under a policy nothing asserts
#   enumerated but not found  -> the enumeration has rotted into an exemption for
#                                a fixture that no longer exists, and the policy
#                                it used to prove is silently unproven
#
# Any project extension is scanned, not only *.csproj: a fixture written in
# another language would otherwise be dropped by the very filter that is supposed
# to be an assertion (the same defect verify-architecture.sh section 2 records).
found_fixture_projects=()
while IFS= read -r project; do
  found_fixture_projects+=("${project}")
done < <(cd "${REPO_ROOT}" && find build/policy-fixtures -name '*.*proj' \
  -not -path '*/obj/*' -not -path '*/bin/*' -print 2>/dev/null | sort)

if [[ "${#found_fixture_projects[@]}" -eq 0 ]]; then
  fail "the fixture scan found no project under build/policy-fixtures/; it cannot see even the enumerated fixtures, so it proves nothing"
else
  unenumerated_fixtures="$(comm -13 \
    <(printf '%s\n' "${fixture_projects[@]}" | sort) \
    <(printf '%s\n' "${found_fixture_projects[@]}" | sort))"
  if [[ -z "${unenumerated_fixtures}" ]]; then
    pass "every one of the ${#found_fixture_projects[@]} project(s) under build/policy-fixtures/ is an enumerated fixture"
  else
    fail "unenumerated project under build/policy-fixtures/: $(printf '%s' "${unenumerated_fixtures}" | paste -sd' ' -); it is not a fixture this gate proves anything about, it is code building under a policy nothing here asserts"
  fi
fi

absent_fixtures=()
for project in "${fixture_projects[@]}"; do
  [[ -f "${REPO_ROOT}/${project}" ]] || absent_fixtures+=("${project}")
done
if [[ "${#absent_fixtures[@]}" -eq 0 ]]; then
  pass "every enumerated fixture exists on disk"
else
  fail "enumerated fixture does not exist: $(printf '%s ' "${absent_fixtures[@]}"); the enumeration has become an exemption for a fixture that is gone, and the policy it proved is unproven"
fi

readonly MINIMUM_PRODUCT_PROJECTS=9
if [[ "${#product_projects[@]}" -lt "${MINIMUM_PRODUCT_PROJECTS}" ]]; then
  fail "policy scope found only ${#product_projects[@]} product project(s) under src/ tests/ game/, fewer than the ${MINIMUM_PRODUCT_PROJECTS} accepted; the scan is not covering what it claims (verify-architecture.sh asserts the exact set)"
else
  pass "policy scope: ${#product_projects[@]} product project(s) plus ${#fixture_projects[@]} fixture project(s)"
fi

echo
echo "=== policy inheritance guard 1: no Directory.Build.* file shadows the root pair"
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
  else
    # Deliberately NOT verified by reading this file. Whether the root policy
    # survives it is asserted by guard 2, from MSBuild's evaluated import graph
    # for the projects below it.
    pass "permitted intermediate file, root policy in effect below it asserted by guard 2: ${file}"
  fi
done

echo
echo "=== policy inheritance guard 2: MSBuild actually imports the root policy pair"
#
# Reads MSBuild's own -preprocess output, which inlines the fully evaluated import
# graph and stamps each inlined file with a banner comment carrying its absolute
# path. An <Import> whose Condition is false, whose Project resolves to nothing,
# or that sits inside an XML comment produces no banner, because it contributed
# nothing to the evaluation. This is why the check is "what did MSBuild import",
# not "what does this file say".

imported_msbuild_files() {
  # $1 project (repository-relative). Prints the absolute path of every file
  # MSBuild imported while evaluating it, one per line. Returns nonzero when the
  # project cannot be preprocessed or the output contains no import banner at
  # all, so an empty graph is never read as compliance: every SDK-style project
  # imports at least Microsoft.Common.props.
  local project="$1"
  local preprocessed
  preprocessed="$(mktemp)" || return 1
  if ! dotnet msbuild "${REPO_ROOT}/${project}" -nologo \
      "-preprocess:${preprocessed}" >/dev/null 2>&1; then
    rm -f "${preprocessed}"
    return 1
  fi
  python3 - "${preprocessed}" <<'PY'
import re, sys
import xml.etree.ElementTree as ET

# The banner MSBuild's preprocessor writes before each inlined file:
#   <!--
#   =========...=========
#     <Import ... />
#
#   /absolute/path/of/the/imported/file
#   =========...=========
#   -->
BANNER = re.compile(
    r"\A\s*={20,}\s*\n\s*<Import\b.*?\n\s*\n(?P<path>[^\n]+)\n\s*={20,}\s*\n?\Z",
    re.S)

try:
    tree = ET.parse(
        sys.argv[1], ET.XMLParser(target=ET.TreeBuilder(insert_comments=True)))
except (ET.ParseError, OSError):
    raise SystemExit(1)

found = 0
for node in tree.iter():
    if node.tag is not ET.Comment:
        continue
    match = BANNER.match(node.text or "")
    if match:
        found += 1
        sys.stdout.write(match.group("path").strip() + "\n")
raise SystemExit(0 if found else 1)
PY
  local status=$?
  rm -f "${preprocessed}"
  return "${status}"
}

for project in "${policy_projects[@]}"; do
  if [[ ! -f "${REPO_ROOT}/${project}" ]]; then
    fail "import-graph guard: no project file at ${project}"
    continue
  fi
  if ! imported="$(imported_msbuild_files "${project}")"; then
    fail "${project}: MSBuild's import graph could not be read, so it is unproven that the root policy applies to it"
    continue
  fi

  missing=()
  for root_file in "${ROOT_MSBUILD_FILES[@]}"; do
    if ! printf '%s\n' "${imported}" | grep -qxF "${REPO_ROOT}/${root_file}"; then
      missing+=("${root_file}")
    fi
  done

  if [[ "${#missing[@]}" -eq 0 ]]; then
    pass "$(basename "${project}" .csproj) imports the root policy pair"
  else
    fail "${project}: MSBuild never imported $(printf '%s ' "${missing[@]}")- the policy this gate proves is not the policy this project builds under"
  fi
done

echo
echo "=== policy inheritance guard 3: evaluated compiler policy per project (VER-FND-001-006 .. VER-FND-001-010)"
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
