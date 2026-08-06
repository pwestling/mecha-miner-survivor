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
# directory it sits in. The four policy-inheritance guards below close that gap;
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

# --- Policy inheritance: Directory.Build.* and .editorconfig shadowing -------
#
# MSBuild stops at the NEAREST Directory.Build.props / Directory.Build.targets
# found walking up from a project directory, and does NOT chain to the parent
# unless that file imports it explicitly. A project-local file therefore shadows
# the repository-root pair entirely rather than adding to it. Roslyn walks
# .editorconfig the same way and stops at the first one declaring `root = true`.
#
# That defeats every policy this script otherwise proves. Four concrete failures
# motivate the checks below:
#   * A project-local Directory.Build.targets makes the evaluated
#     TreatWarningsAsErrors 'false', so CS8600 and CS8602 compile as warnings and
#     the build succeeds - directly contradicting Directory.Build.targets' claim
#     that no work package can quietly opt its own project out.
#   * A shadowing build/policy-fixtures/unsafe/Directory.Build.props keeps the
#     unsafe fixture failing with CS0227 even when the real root policy has been
#     flipped to AllowUnsafeBlocks=true. The fixture then certifies a policy that
#     is switched off for the product, which is worse than having no fixture.
#   * A `root = true` .editorconfig copied into build/policy-fixtures/naming/
#     decouples the naming fixture from the root file's
#     dotnet_diagnostic.IDE1006.severity. Flipping that severity to `none` then
#     compiled a real IDE1006 violation clean while this gate printed PASS.
#   * ROOT Directory.Build.props RunAnalyzers=false plus
#     build/policy-fixtures/Directory.Build.props RunAnalyzers=true - two edits to
#     two tracked files, the second in the file guard 1 permits to exist. A CA2200
#     rethrow and an IDE1006 field name in src/ then built at 0 warnings while
#     VER-FND-001-007 and -008 both reported ok off fixtures that were measuring a
#     private copy of the policy. Root WarningLevel=0 with WarningLevel=9999 in the
#     same intermediate file does the same to CS8600, CS8602 and CA2200.
#
# Four guards, because no one of them is sufficient:
#   Guard 1 is structural - no file may shadow the root policy files.
#   Guard 1b restricts what the files guard 1 PERMITS to exist may say, because a
#   permitted file is the one place an override can legally sit.
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
# policy rather than the policy the product builds under. Guard 2 reads MSBuild's
# evaluated import graph, which neither of those two files appears in, so it
# catches both. It is NOT immune to a forged claim, though: see the caveat above
# guard 2 itself.
#
# Why guard 1b rather than a longer EVALUATED_POLICIES list. The suppression
# switches that decouple a fixture from the root policy cannot be enumerated -
# per-category AnalysisMode<Category>, per-rule severities, RunAnalyzers,
# RunAnalyzersDuringBuild, WarningLevel, CodeAnalysisTreatWarningsAsErrors, and
# whatever the next SDK adds. Adding names to guard 3's list leaves the next
# switch live. But the negative fixtures are already a detector for the whole
# family: a root RunAnalyzers=false with NO local override silences the CA2200 and
# IDE1006 fixtures, their expected diagnostics stop appearing, and this gate goes
# red without knowing that RunAnalyzers exists. What defeats the fixtures is only
# the SECOND edit, the local override. So guard 1b closes the override rather than
# enumerating the switches: the files below build/policy-fixtures/ that MSBuild
# reads may declare nothing outside a small allowlist of properties that provably
# cannot affect which diagnostics appear. Every future switch is then caught by
# the fixtures instead of by a list that has to predict it.

readonly ROOT_MSBUILD_FILES=(
  "Directory.Build.props"
  "Directory.Build.targets"
)

# The repository-root policy files guard 1 scans for. .editorconfig is here
# because half of VER-FND-001-008 lives in it, but it is NOT in
# ROOT_MSBUILD_FILES: it is not an MSBuild file, so guard 2 cannot assert it was
# imported and guard 3 cannot read its contents as a property.
readonly ROOT_POLICY_FILES=(
  "Directory.Build.props"
  "Directory.Build.targets"
  ".editorconfig"
)

# Non-root policy files permitted to exist. Listing a file here says it may exist
# and nothing more. Guard 1b bounds what it is allowed to say, and guard 2 still
# requires MSBuild to have imported the root pair when evaluating every project
# below it, so an entry here cannot be used to decouple a subtree from the root
# policy. Adding an entry must be a deliberate decision, not a side effect.
#
# There is no .editorconfig entry: this repository has exactly one .editorconfig,
# the root one, so the allowlist for that filename is empty.
readonly ALLOWED_INTERMEDIATE_MSBUILD_FILES=(
  "build/policy-fixtures/Directory.Build.props"
)

# Guard 1b's allowlist: "<file>|<required Sdk attribute>|<properties it may
# declare>". Every MSBuild file under build/policy-fixtures/ that MSBuild reads
# while evaluating a fixture appears here - the permitted intermediate file, and
# each enumerated fixture project, which is the other place a local override can
# sit. Anything not listed is a failure, including <ItemGroup>, <Target> and an
# <Import> of anything other than the root policy, because an item group can
# remove an analyzer just as effectively as a property can switch one off.
#
# Only two properties are allowed, and only in the intermediate file:
#   RestorePackagesWithLockFile  Consumed by NuGet restore to decide whether to
#                                WRITE packages.lock.json. It is load-bearing here:
#                                the root sets it true, so without this the gate
#                                leaves an untracked packages.lock.json in every
#                                fixture directory on every run. It reaches no
#                                compiler switch and no analyzer.
#   IsPackable                   Consumed only by the Pack target, which nothing
#                                here runs. It cannot reach a compiler switch.
#
# The fixture projects allow NONE. They declare no property today, and a fixture
# that needs one is a deliberate decision that belongs in this list with its own
# argument for why it cannot affect a diagnostic.
readonly INTERMEDIATE_ALLOWED_PROPERTIES="RestorePackagesWithLockFile,IsPackable"
readonly FIXTURE_PROJECT_ALLOWED_PROPERTIES=""

# "<property>|<required evaluated value>".
#
# WHAT THIS LIST IS: the properties whose evaluated value is asserted per project.
# It is a list of KNOWN suppression and policy switches, not a complete one. It
# does not and cannot enumerate every property that can stop a diagnostic from
# appearing - per-category AnalysisMode<Category>, per-rule
# dotnet_diagnostic.<ID>.severity, and whatever switch the next SDK ships are all
# outside it. Nothing here should be read as "these are all the properties that
# matter". Completeness for that family is guard 1b's job, not this list's; see
# the argument above ALLOWED_INTERMEDIATE_MSBUILD_FILES.
#
# Each entry is here because a specific escape was observed or is directly
# implied. The first group is what the fixtures below depend on:
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
# The last three are DEFENCE IN DEPTH for switches that were used to escape this
# gate, added on top of guard 1b rather than instead of it. Guard 1b is what makes
# the family closed; these three only make three specific members of it fail twice:
#   RunAnalyzers / RunAnalyzersDuringBuild  Empty is the value under which the
#     analyzers run - neither is set anywhere in this repository, and the required
#     value is that absence rather than "true" so that setting either one at all is
#     a deliberate act this gate reports.
#   WarningLevel  8 is what the pinned SDK evaluates for the pinned net8.0 /
#     LangVersion 12.0 pair; 0 turns CS8600, CS8602 and CA2200 off wholesale. This
#     is pinned to an observed value rather than declared in
#     Directory.Build.props, because declaring it there would cap the level and
#     silence warnings a later SDK adds. An SDK bump that changes the default will
#     fail here loudly; that is a review, not a number to adjust.
#
# KNOWN GAP, only partly closed: the naming fixture also depends on the root
# .editorconfig's `dotnet_diagnostic.IDE1006.severity = error`. That is not an
# MSBuild property and is not reachable through an evaluated item either - the
# EditorConfigFiles item holds only the SDK-generated file, because Roslyn, not
# MSBuild, walks the .editorconfig chain and applies `root = true`. Guard 1 now
# fails on any non-root .editorconfig, which converts the committed case from
# invisible to loud, but it does not see an untracked one and it cannot see
# `root = true` being removed from, or a severity being changed in, the root file
# itself. VER-FND-001-008 in tests/verification/FND-001.json records this.
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
  "RunAnalyzers|"
  "RunAnalyzersDuringBuild|"
  "WarningLevel|8"
  # These three are the forgery-resistant half of guard 2; see the caveat above
  # guard 2 for why guard 2 needs one. They are MSBuild's own answers about where
  # its upward search for the root policy landed and whether it was told to skip
  # the import, so no comment in any file can affect them. ImportDirectoryBuildProps
  # =false is the switch that suppresses the import without leaving a shadow file
  # for guard 1 to find.
  #
  # There is deliberately no DirectoryBuildPropsPath entry: it is the only one of
  # the four that is not uniform across projects, because the fixtures legitimately
  # resolve it to the guard-1-permitted build/policy-fixtures/Directory.Build.props.
  # The .targets path IS uniform - nothing is permitted to shadow it - so it is
  # asserted here.
  "ImportDirectoryBuildProps|true"
  "ImportDirectoryBuildTargets|true"
  "DirectoryBuildTargetsPath|${REPO_ROOT}/Directory.Build.targets"
)

echo "=== policy inheritance: repository-root policy files exist"
for file in "${ROOT_POLICY_FILES[@]}"; do
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

# MSBuild intermediate output has to be skipped, but `-not -path '*/obj/*'` skips
# ANY directory named obj or bin anywhere in the path, which turns the scan back
# into a floor: build/policy-fixtures/obj/hidden/hidden.csproj made the partition
# assertion below print "every one of the 6 project(s) ... is an enumerated
# fixture" and exit 0 while seven projects existed.
#
# A directory named obj or bin is an MSBuild intermediate only when the directory
# ABOVE it is itself a project directory. That prunes each project's own obj/ and
# bin/ and nothing else, so a project parked under a directory named obj is still
# counted. The rule is not circular: it asks whether the parent holds a project
# file, which is a fact on disk rather than a result of this scan.
prune_project_intermediates() {
  # Reads repository-relative project paths on stdin, writes the kept ones to
  # stdout. The script is passed with -c, not a heredoc, because a heredoc would
  # take over stdin and this filter needs it.
  python3 -c '
import fnmatch, os, sys

repo_root = sys.argv[1]

def is_project_directory(relative):
    directory = os.path.join(repo_root, relative) if relative else repo_root
    try:
        names = os.listdir(directory)
    except OSError:
        return False
    return any(fnmatch.fnmatch(name, "*.*proj") for name in names)

for line in sys.stdin:
    path = line.strip()
    if not path:
        continue
    parts = path.split("/")
    if not any(
            part in ("obj", "bin") and is_project_directory("/".join(parts[:index]))
            for index, part in enumerate(parts[:-1])):
        sys.stdout.write(path + "\n")
' "${REPO_ROOT}"
}

product_projects=()
while IFS= read -r project; do
  product_projects+=("${project}")
done < <(cd "${REPO_ROOT}" && find src tests game -name '*.csproj' -print 2>/dev/null \
  | prune_project_intermediates | sort)

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
done < <(cd "${REPO_ROOT}" && find build/policy-fixtures -name '*.*proj' -print 2>/dev/null \
  | prune_project_intermediates | sort)

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
echo "=== policy inheritance guard 1: no Directory.Build.* or .editorconfig file shadows the root policy"
#
# Scoped to this working tree's own files. A `git worktree` checkout of THIS
# repository nested inside the tree - which this repository creates under
# .claude/worktrees/, and which the .gitignore entry exists to accommodate -
# contains a complete second checkout, root Directory.Build.props and all. Those
# are that checkout's files: MSBuild cannot reach them from any project in this
# working tree, so they shadow nothing here. One session worktree made this guard
# exit 4 with three false failures (.claude/worktrees/<name>/Directory.Build.props,
# its .targets, and its build/policy-fixtures/Directory.Build.props), which turned
# the gate hostile to the very workflow the .gitignore change was made for.
#
# The exclusion admits exactly that case and nothing wider. It used to require only
# that the directory be its own git toplevel with the file untracked here, and a
# previous comment claimed that "cannot become a hiding place". It could: a plain
# `git init` in src/MechaMiner.Simulation/, and a real `git submodule add` under
# src/, each satisfy both facts, and each gave verify-policies: PASS with a
# shadowing Directory.Build.props sitting in the excluded directory. Neither can be
# delivered through a commit, so that was a working-tree-only hole, but the
# exclusion is now narrowed to the case it was written for:
#   * the directory must appear as a worktree of THIS repository in
#     `git worktree list --porcelain` run from the outer repository. A `git init`
#     tree is not in that list, and neither is a submodule, because neither is a
#     worktree of this repository, and
#   * the file must not be tracked by THIS repository. A tracked file is this
#     repository's file whatever sits above it, and is always checked.
repo_worktree_roots=()
while IFS= read -r line; do
  [[ "${line}" == "worktree "* ]] || continue
  worktree_path="${line#worktree }"
  worktree_real="$(cd "${worktree_path}" 2>/dev/null && pwd -P)" || continue
  repo_worktree_roots+=("${worktree_real}")
done < <(git -C "${REPO_ROOT}" worktree list --porcelain 2>/dev/null)

nested_checkout_roots=()
while IFS= read -r gitlink; do
  candidate="${gitlink%/.git}"
  [[ -n "${candidate}" && "${candidate}" != "${gitlink}" ]] || continue
  candidate_real="$(cd "${REPO_ROOT}/${candidate}" 2>/dev/null && pwd -P)" || continue
  is_worktree_of_this_repo=0
  for worktree_real in ${repo_worktree_roots[@]+"${repo_worktree_roots[@]}"}; do
    if [[ "${candidate_real}" == "${worktree_real}" ]]; then
      is_worktree_of_this_repo=1
      break
    fi
  done
  if [[ "${is_worktree_of_this_repo}" -eq 1 ]]; then
    nested_checkout_roots+=("${candidate}")
  fi
done < <(cd "${REPO_ROOT}" && find . -mindepth 2 -name '.git' -printf '%P\n' 2>/dev/null | sort)

nested_checkout_owner() {
  # $1 repository-relative path. Prints the nested worktree that owns it and
  # returns 0; returns nonzero when this working tree owns it.
  local file="$1" root
  for root in ${nested_checkout_roots[@]+"${nested_checkout_roots[@]}"}; do
    if [[ "${file}" == "${root}/"* ]]; then
      if git -C "${REPO_ROOT}" ls-files --error-unmatch -- "${file}" >/dev/null 2>&1; then
        return 1
      fi
      printf '%s' "${root}"
      return 0
    fi
  done
  return 1
}

found_policy_files=()
while IFS= read -r file; do
  if owner="$(nested_checkout_owner "${file}")"; then
    pass "not this working tree's file, it belongs to the nested worktree ${owner}/: ${file}"
    continue
  fi
  found_policy_files+=("${file}")
done < <(cd "${REPO_ROOT}" && find . \
  \( -name 'Directory.Build.props' -o -name 'Directory.Build.targets' \
     -o -name '.editorconfig' \) \
  -printf '%P\n' 2>/dev/null | sort)

# An empty result would mean the scan found nothing at all, including the root
# files, so it must not be reported as compliance. Counted AFTER the nested-worktree
# exclusion, so an exclusion that swallowed a root file fails here.
if [[ "${#found_policy_files[@]}" -lt "${#ROOT_POLICY_FILES[@]}" ]]; then
  fail "the policy-file scan retained ${#found_policy_files[@]} file(s) of this working tree, fewer than the ${#ROOT_POLICY_FILES[@]} root file(s); it cannot see even the root policy, so it proves nothing"
fi

for file in ${found_policy_files[@]+"${found_policy_files[@]}"}; do
  is_root=0
  for root_file in "${ROOT_POLICY_FILES[@]}"; do
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
    fail "shadowing policy file ${file}: it replaces the repository-root policy for every project at or below $(dirname "${file}")/"
  else
    # What this file is allowed to SAY is guard 1b, below. Whether the root policy
    # survives it is asserted by guard 2, from MSBuild's evaluated import graph for
    # the projects below it.
    pass "permitted intermediate file, contents bounded by guard 1b and root policy asserted by guard 2: ${file}"
  fi
done

echo
echo "=== policy inheritance guard 1b: the permitted files below build/policy-fixtures/ override nothing"
#
# Guard 1 permits build/policy-fixtures/Directory.Build.props to exist, because the
# root sets RestorePackagesWithLockFile=true and the fixtures must not leave an
# untracked packages.lock.json behind on every run. That permission was the whole
# escape: root RunAnalyzers=false plus RunAnalyzers=true here, or root
# WarningLevel=0 plus WarningLevel=9999 here, and the fixtures keep failing with
# their expected diagnostics while the product compiles violations at 0 warnings.
# A fixture .csproj is the same lever one directory deeper, so it is checked too.
#
# The check is a whitelist of what these files may CONTAIN, not a blacklist of
# switches, because the switch family is open-ended and a blacklist would always be
# one SDK behind. <ItemGroup> is rejected along with unlisted properties: removing
# an Analyzer item switches an analyzer off just as effectively as a property does.
policy_fixture_file_violations() {
  # $1 repository-relative file. $2 the Sdk attribute <Project> must carry, empty
  # for none. $3 comma-separated property names the file may declare, empty for
  # none. Prints one violation per line; prints nothing when the file is compliant.
  python3 - "${REPO_ROOT}" "$1" "$2" "$3" <<'PY'
import os, sys
import xml.etree.ElementTree as ET

repo_root, relative, required_sdk, allowed_csv = sys.argv[1:5]
allowed = {name for name in allowed_csv.split(',') if name}
allowed_text = ', '.join(sorted(allowed)) or 'none'
absolute = os.path.join(repo_root, relative)
root_policy = {
    os.path.join(repo_root, name)
    for name in ('Directory.Build.props', 'Directory.Build.targets')
}

def local(tag):
    return tag.split('}', 1)[1] if '}' in tag else tag

def report(message):
    print('%s %s' % (relative, message))

try:
    project = ET.parse(absolute).getroot()
except (ET.ParseError, OSError) as error:
    report('could not be parsed as XML (%s); a file this gate cannot read is a '
           'file it cannot vouch for' % error)
    raise SystemExit(0)

if local(project.tag) != 'Project':
    report('has root element <%s>, not <Project>' % local(project.tag))
    raise SystemExit(0)

for name, value in sorted(project.attrib.items()):
    if name == 'Sdk':
        if value != required_sdk:
            report('declares Sdk="%s" on <Project>; this gate proves policies under '
                   'Sdk="%s" and nothing else' % (value, required_sdk))
    else:
        report('declares the attribute %s="%s" on <Project>; only Sdk may appear '
               'there, because attributes such as TreatAsLocalProperty change how '
               'the root policy is evaluated' % (name, value))
if required_sdk and 'Sdk' not in project.attrib:
    report('does not declare Sdk="%s" on <Project>' % required_sdk)

def check_attributes(node, permitted, where):
    for name, value in sorted(node.attrib.items()):
        if name not in permitted:
            report('sets %s="%s" on %s; only %s may appear there'
                   % (name, value, where, ' and '.join(sorted(permitted))))

for child in project:
    if child.tag is ET.Comment or child.tag is ET.PI:
        continue
    name = local(child.tag)
    if name == 'PropertyGroup':
        check_attributes(child, {'Condition', 'Label'}, '<PropertyGroup>')
        for prop in child:
            if prop.tag is ET.Comment or prop.tag is ET.PI:
                continue
            declared = local(prop.tag)
            check_attributes(prop, {'Condition'}, '<%s>' % declared)
            if declared not in allowed:
                report('declares <%s>, which is not among the properties this file '
                       'may declare (%s). A property set here overrides the '
                       'repository-root policy for every project below it, and the '
                       'negative fixtures cannot detect a suppression switch that '
                       'has been switched back on locally.' % (declared, allowed_text))
    elif name == 'Import':
        check_attributes(child, {'Project', 'Condition'}, '<Import>')
        target = child.get('Project') or ''
        resolved = os.path.normpath(
            os.path.join(os.path.dirname(absolute), target))
        if resolved not in root_policy:
            report('imports "%s", which is not a repository-root policy file; only '
                   'the root Directory.Build.props or Directory.Build.targets may '
                   'be imported here' % target)
    else:
        report('contains <%s>; only <PropertyGroup> with allowlisted properties and '
               'an <Import> of the root policy may appear in this file, because an '
               'item group or a target can switch an analyzer off just as '
               'effectively as a property can' % name)
PY
}

# "<file>|<required Sdk>|<properties it may declare>"
policy_fixture_files=()
for allowed_file in "${ALLOWED_INTERMEDIATE_MSBUILD_FILES[@]}"; do
  policy_fixture_files+=("${allowed_file}||${INTERMEDIATE_ALLOWED_PROPERTIES}")
done
for project in "${fixture_projects[@]}"; do
  policy_fixture_files+=("${project}|Microsoft.NET.Sdk|${FIXTURE_PROJECT_ALLOWED_PROPERTIES}")
done

for entry in "${policy_fixture_files[@]}"; do
  IFS='|' read -r file required_sdk allowed_properties <<<"${entry}"
  if [[ ! -f "${REPO_ROOT}/${file}" ]]; then
    fail "guard 1b: no file at ${file}, so what it declares is unknown"
    continue
  fi
  violations="$(policy_fixture_file_violations "${file}" "${required_sdk}" "${allowed_properties}")"
  if [[ -z "${violations}" ]]; then
    pass "${file} declares nothing that can change which diagnostics appear"
  else
    while IFS= read -r violation; do
      [[ -n "${violation}" ]] && fail "guard 1b: ${violation}"
    done <<<"${violations}"
  fi
done

echo
echo "=== policy inheritance guard 2: MSBuild actually imports the root policy pair"
#
# Reads MSBuild's own -preprocess output, which inlines the fully evaluated import
# graph and stamps each inlined file with a banner comment carrying its absolute
# path. An <Import> whose Condition is false, whose Project resolves to nothing,
# or that sits inside an XML comment produces no banner, because it contributed
# nothing to the evaluation. That is what defeats the two files described above
# guard 1: both claim an import in text that MSBuild never acts on, and neither
# appears here.
#
# CAVEAT, and the reason this guard is not read as proof on its own. MSBuild copies
# the comments of a source file into its preprocess output verbatim, and this guard
# recognises a banner by its shape. A comment shaped like a banner, placed in a
# project file, is therefore indistinguishable here from a banner MSBuild emitted:
# adding one to all six fixture projects removed every guard-2 failure while no
# fixture imported anything. Three things bound that:
#   * the forgery must hard-code the absolute path of the checkout it runs in,
#     because the banner carries an absolute path and this guard compares against
#     ${REPO_ROOT}. A forged file is therefore machine-specific and breaks in CI, in
#     a clone, and in a worktree.
#   * guard 3 is the backstop. It reads evaluated property values, which no comment
#     can affect, so the eleven policy properties still have to hold even when this
#     guard has been lied to.
#   * guard 3 also asserts ImportDirectoryBuildProps, ImportDirectoryBuildTargets
#     and DirectoryBuildTargetsPath, which are MSBuild's own answers about where its
#     upward search landed and whether it was told to skip the import. Those cover
#     the specific fact this guard exists for - "was the root pair imported at all" -
#     without going through any comment.
# What is NOT bounded is a switch outside guard 3's list, in a project whose forged
# banner is written for the machine the gate runs on. Closing that needs a source of
# import truth that is not the preprocess text - a binary log, or MSBuild's
# ProjectImportedEventArgs - and it is recorded rather than closed here.

# Set by imported_msbuild_files when it returns nonzero: why the import graph could
# not be read. Always an INFRASTRUCTURE reason - "MSBuild could not tell us" - never
# "MSBuild told us the root policy was not imported", which is the caller's finding
# and has its own message. The two used to share one exit code and one message, and
# a single non-reproducing failure in roughly 600 evaluations could not be
# attributed to either. Fail-closed either way; no retry.
import_graph_failure=""

imported_msbuild_files() {
  # $1 project (repository-relative). Prints the absolute path of every file
  # MSBuild imported while evaluating it, one per line.
  #
  # Returns 0 when a graph was read, and 1 when it could not be - MSBuild failed,
  # its output would not parse, or it contained no import banner at all, which is
  # equally an infrastructure failure because every SDK-style project imports at
  # least Microsoft.Common.props. An empty graph is never read as compliance.
  # On 1, import_graph_failure carries the reason, with MSBuild's exit code and
  # captured stderr when MSBuild is what failed.
  local project="$1"
  import_graph_failure=""

  local preprocessed
  if ! preprocessed="$(mktemp)"; then
    import_graph_failure="mktemp could not create a temporary file for MSBuild's -preprocess output"
    return 1
  fi
  local captured_stderr
  if ! captured_stderr="$(mktemp)"; then
    rm -f "${preprocessed}"
    import_graph_failure="mktemp could not create a temporary file for MSBuild's stderr"
    return 1
  fi

  dotnet msbuild "${REPO_ROOT}/${project}" -nologo \
    "-preprocess:${preprocessed}" >/dev/null 2>"${captured_stderr}"
  local msbuild_status=$?
  if [[ "${msbuild_status}" -ne 0 ]]; then
    import_graph_failure="dotnet msbuild -preprocess exited ${msbuild_status}; stderr: $(tr '\n' '|' <"${captured_stderr}" | cut -c 1-1500)"
    rm -f "${preprocessed}" "${captured_stderr}"
    return 1
  fi

  local parsed parse_status preprocessed_size
  parsed="$(python3 - "${preprocessed}" 2>"${captured_stderr}" <<'PY'
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
except (ET.ParseError, OSError) as error:
    sys.stderr.write("the -preprocess output is not readable XML: %s\n" % error)
    raise SystemExit(2)

found = 0
for node in tree.iter():
    if node.tag is not ET.Comment:
        continue
    match = BANNER.match(node.text or "")
    if match:
        found += 1
        sys.stdout.write(match.group("path").strip() + "\n")
if not found:
    sys.stderr.write(
        "the -preprocess output parsed but carries no import banner at all, "
        "which cannot happen for an SDK-style project\n")
    raise SystemExit(3)
raise SystemExit(0)
PY
)"
  parse_status=$?
  preprocessed_size="$(wc -c <"${preprocessed}" 2>/dev/null)"
  if [[ "${parse_status}" -ne 0 ]]; then
    import_graph_failure="dotnet msbuild -preprocess exited 0 but its output could not be read as an import graph (reader exit ${parse_status}: $(tr '\n' '|' <"${captured_stderr}" | cut -c 1-1500)); the output file was ${preprocessed_size:-unmeasurable} byte(s)"
    rm -f "${preprocessed}" "${captured_stderr}"
    return 1
  fi
  rm -f "${preprocessed}" "${captured_stderr}"
  printf '%s\n' "${parsed}"
  return 0
}

for project in "${policy_projects[@]}"; do
  if [[ ! -f "${REPO_ROOT}/${project}" ]]; then
    fail "import-graph guard: no project file at ${project}"
    continue
  fi
  if ! imported="$(imported_msbuild_files "${project}")"; then
    fail "${project}: INFRASTRUCTURE - MSBuild's import graph could not be read at all, so whether the root policy applies to this project is unproven rather than disproven: ${import_graph_failure}"
    continue
  fi

  imported_count="$(printf '%s\n' "${imported}" | grep -c .)"
  missing=()
  for root_file in "${ROOT_MSBUILD_FILES[@]}"; do
    if ! printf '%s\n' "${imported}" | grep -qxF "${REPO_ROOT}/${root_file}"; then
      missing+=("${root_file}")
    fi
  done

  if [[ "${#missing[@]}" -eq 0 ]]; then
    pass "$(basename "${project}" .csproj) imports the root policy pair"
  else
    fail "${project}: MSBuild's import graph was READ (${imported_count} imported file(s)) and does not contain $(printf '%s ' "${missing[@]}")- the policy this gate proves is not the policy this project builds under"
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
# Scope: the enumerated policy_projects set - every product project (so a newly
# added one is covered automatically) plus the six policy fixtures by name. The
# fixtures are named rather than globbed so that fixture trees owned by other work
# packages, which may deliberately declare unrestorable references, are not
# evaluated here; the naming is asserted to be a partition of build/policy-fixtures/
# above, so it cannot rot into an exemption.

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
# The positive case must produce byte-identical assemblies across two independent
# clean builds USING THE REPOSITORY'S OWN Deterministic VALUE. It previously
# passed "-p:Deterministic=true" on the command line, which made the assertion
# blind to the thing it claims to verify: with Deterministic=false in the root
# Directory.Build.props, this section still reported "two clean builds are
# byte-identical" because the command line had overridden the repository. So the
# positive case now passes no override at all - that is the whole point of it.
#
# The negative control is the only place an override appears, and it must NOT
# produce identical output, otherwise the positive assertion would be vacuously
# true regardless of the policy.
#
# Both hashes are required to look like SHA-256 before they are compared. The
# comparison used to be between two `sha256sum | cut` results, which are both the
# empty string when the assembly path is wrong - two absent values compared equal
# and the positive assertion passed, printing "byte-identical: sha256 " with
# nothing after it. A check whose two sides can both be absent must reject absence
# before comparing.

echo "=== VER-FND-001-011: Deterministic=true (the repository's own value)"
readonly DETERMINISTIC_ASSEMBLY="${FIXTURE_ROOT}/deterministic/bin/Debug/net8.0/deterministic.dll"
readonly SHA256_TEXT='^[0-9a-f]{64}$'

hash_deterministic_build() {
  # "$@" = extra MSBuild arguments, deliberately empty for the positive case so
  # the Deterministic value under test is the repository's. Prints the SHA-256 of
  # the produced assembly, or a BUILD-FAILED / NO-OUTPUT / NO-HASH marker that
  # will not satisfy SHA256_TEXT.
  clean_fixture deterministic
  if ! dotnet build "${FIXTURE_ROOT}/deterministic/deterministic.csproj" \
      --nologo -v q "$@" >/dev/null 2>&1; then
    printf 'BUILD-FAILED'
    return
  fi
  if [[ ! -f "${DETERMINISTIC_ASSEMBLY}" ]]; then
    printf 'NO-OUTPUT'
    return
  fi
  local hash
  hash="$(sha256sum "${DETERMINISTIC_ASSEMBLY}" | cut -d' ' -f1)"
  if [[ -z "${hash}" ]]; then
    printf 'NO-HASH'
    return
  fi
  printf '%s' "${hash}"
}

first_hash="$(hash_deterministic_build)"
second_hash="$(hash_deterministic_build)"
if [[ ! "${first_hash}" =~ ${SHA256_TEXT} || ! "${second_hash}" =~ ${SHA256_TEXT} ]]; then
  fail "VER-FND-001-011: the deterministic fixture must compile to a hashable assembly; got '${first_hash}' and '${second_hash}'"
elif [[ "${first_hash}" == "${second_hash}" ]]; then
  pass "two clean builds at the repository's Deterministic value are byte-identical: sha256 ${first_hash}"
else
  fail "VER-FND-001-011: rebuild differed at the repository's Deterministic value (${first_hash} vs ${second_hash}); either the policy is not true or something else is nondeterministic"
fi

echo
echo "=== VER-FND-001-011 negative control: -p:Deterministic=false forced"
third_hash="$(hash_deterministic_build -p:Deterministic=false)"
fourth_hash="$(hash_deterministic_build -p:Deterministic=false)"
if [[ ! "${third_hash}" =~ ${SHA256_TEXT} || ! "${fourth_hash}" =~ ${SHA256_TEXT} ]]; then
  fail "VER-FND-001-011: negative-control build produced no hashable assembly; got '${third_hash}' and '${fourth_hash}'"
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
