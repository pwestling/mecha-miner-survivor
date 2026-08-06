#!/usr/bin/env bash
#
# Asserts that every gate script in this repository is either invoked automatically
# or explicitly exempted with a reason. A script that is neither fails this gate.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface ("CI calls these same wrappers instead of
#              recreating workflows")
#            docs/technical/91-verification-strategy.md § Fast pull-request suite
#            AGENTS.md § Standard workflow surface
# Requirements: TR-BLD-005, TR-QUA-001
# Verification: VER-FND-005-010
#
# Why this exists. Before it, this repository had nine build/verify-*.sh gate
# scripts and exactly three of them - verify-architecture.sh (from `build`),
# verify-godot.sh (from `godot-import`), and verify-policies.sh (from `test-fast`) -
# were invoked by anything. The other six ran only when a person remembered to type
# them. That is a different defect from a gate that cannot fail: these gates can
# fail, and they were never asked. Every "all gates green" report was therefore true
# about a subset nobody had stated, and the number of gates that existed was a
# question no artifact answered.
#
# The rule this file makes checkable is a partition, not a count:
#
#   every gate script is INVOKED (by the verb host, a root wrapper, or the CI
#   workflow) or EXEMPT (listed below with a reason), and never both, and never
#   neither.
#
# Three properties keep the rule from decaying into a formality, each stated
# because dropping it is how such a rule usually dies:
#
#  1. The candidate set is read from the filesystem, never from a list in this
#     file. A hardcoded roster would reproduce the original defect one level up:
#     a new script would be absent from the roster and so would pass by not being
#     looked at. `find` is the enumerator, and an empty enumeration fails.
#  2. Every exemption must name a file that exists, and must not name a script
#     that is in fact invoked. A stale exemption for a deleted script, or for one
#     that has since been wired, turns the list into decoration.
#  3. "Invoked" means a real call site, not a mention. The verb host names its
#     scripts as exact string literals ("build/<name>"); prose in a doc comment or
#     inside a longer message does not match. Otherwise documenting a script would
#     be indistinguishable from running it, which is the substitution this whole
#     repository keeps finding.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

# --- Deliberately unwired gate scripts ---------------------------------------
# "<repo-relative path>|<reason it is not invoked, and who would wire it>"
#
# This list is for genuine exceptions. It is not a place to park a script that could
# simply be wired, and every entry below shares one demonstrated structural reason
# rather than a preference:
#
#   ./build.sh rebuilds the verb host on every invocation ("Build the verb host
#   before dispatching"), so a script whose subject is the wrapper, reached from
#   inside a running verb, makes MSBuild rewrite the assembly the calling verb is
#   executing from. Observed rather than assumed: with verify-verbs.sh wired into
#   test-fast, its § 1 read an empty verb table and the gate failed, while the same
#   script passed standalone (exit 0, 89 s) minutes later. Two of them additionally
#   launch Godot on the way (doctor probes the editor; a Godot-project build writes
#   game/.godot), which trips test-fast's own pure-tier tripwire and fails doc 91
#   § Test project separation - the wiring itself would have turned the tier red.
#
# Every script below passes today when run directly; none is exempted because it is
# broken. What unblocks them is FND-002's: a way for the wrapper to dispatch without
# rebuilding a host that is already running, and a verb that passes it through. When
# that exists, these entries come off this list and section 3 starts requiring them.

readonly EXEMPT=(
  "build/verify-verbs.sh|re-entrant: invokes ./build.sh about thirty times, and its doctor probes launch Godot; passes standalone, exit 0 in 89 s; unblocked by FND-002 (host-rebuild skip)"
  "build/verify-configurations.sh|re-entrant: invokes ./build.sh build for all three configurations, whose Godot-project builds write game/.godot; passes standalone, exit 0 in 93 s; unblocked by FND-002"
  "build/verify-format.sh|re-entrant: its subject is ./build.sh format and format-check, so the verbs that own it cannot reach it; passes standalone, exit 0 in 212 s; unblocked by FND-002"
  "build/verify-wrapper-parity.sh|re-entrant: runs both root wrappers and compares their usage tables; passes standalone, exit 0 in 14 s; unblocked by FND-002"
  "build/verify-test-harness.sh|re-entrant: its subject is ./build.sh test-fast, so test-fast cannot reach it and any other verb hits the same host rebuild; passes standalone; unblocked by FND-002"
)

# --- Where an invocation may live ---------------------------------------------
# The verb host (a verb reaching the script through RunRepositoryScript), the root
# wrappers, and the CI workflow. Anything else - a test's doc comment, a design
# document, another gate script's prose - is a mention, not a call site.

readonly -a SEARCH_ROOTS=(
  "src"
  "build.sh"
  "build.ps1"
  ".github"
)

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

# Prints every call site of one gate script, or nothing. A C# call site names the
# script as a complete string literal; a shell or YAML call site runs the path.
call_sites() {
  local path="$1"
  local -a roots=()
  local root

  for root in "${SEARCH_ROOTS[@]}"; do
    [[ -e "${REPO_ROOT}/${root}" ]] && roots+=("${root}")
  done

  if [[ "${#roots[@]}" -eq 0 ]]; then
    return 0
  fi

  # Two accepted forms, each only in the language where it is a call, and each
  # anchored so a mention cannot pass as one:
  #
  #   C#     "build/verify-x.sh"   the complete string literal a verb passes to
  #                                RunRepositoryScript. The closing quote is
  #                                required: without it, a diagnostic message that
  #                                merely names the script - "build/verify-x.sh
  #                                returned an unclassified exit code" - counts as
  #                                running it, and the negative control that removes
  #                                the real call site then still passes. That is not
  #                                hypothetical; it is what the first version of this
  #                                function did.
  #
  #   shell  build/verify-x.sh     executed as a command, optionally ./-prefixed or
  #   pwsh                         quoted, in a wrapper or a workflow step.
  #   YAML
  ( cd "${REPO_ROOT}" && grep -rnE \
    -e "\"${path}\"" \
    --include='*.cs' \
    "${roots[@]}" 2>/dev/null
    cd "${REPO_ROOT}" && grep -rnE \
    -e "(^|[[:space:]\"'\$({|&;])(\./)?${path}([[:space:]\"')}|&;]|\$)" \
    --include='*.sh' \
    --include='*.ps1' \
    --include='*.yml' \
    --include='*.yaml' \
    "${roots[@]}" 2>/dev/null ) \
    | grep -vE '^[^:]+:[0-9]+:[[:space:]]*(#|//|///|<!--)'
}

is_exempt() {
  local path="$1"
  local entry
  for entry in "${EXEMPT[@]}"; do
    [[ "${entry%%|*}" == "${path}" ]] && return 0
  done
  return 1
}

echo "=== 1. enumerate gate scripts from the filesystem (VER-FND-005-010)"

# The glob is deliberately wider than build/verify-*.sh. A checker written in
# Python, or placed outside build/, is the same kind of artifact and must not
# escape the partition by being a different file extension or living somewhere
# else. artifacts/ and .git are excluded because they are outputs, not sources,
# and generated/ because nothing there is authored.
mapfile -t scripts < <(
  cd "${REPO_ROOT}" && find . \
    -type d \( -name .git -o -name artifacts -o -name generated -o -name .godot \
      -o -name obj -o -name bin \) -prune -o \
    -type f \( -name 'verify-*.sh' -o -name 'verify_*.sh' \
      -o -name 'verify-*.py' -o -name 'verify_*.py' \) -print \
    | sed 's|^\./||' | sort
)

# An empty candidate set never satisfies a gate: "no gate scripts found" is a
# broken enumerator, not a clean repository.
if [[ "${#scripts[@]}" -eq 0 ]]; then
  fail "found no gate scripts at all; the enumerator is broken, not the repository clean"
  echo
  echo "verify-gate-wiring: FAIL (${failures} assertion(s))"
  exit "${EXIT_VALIDATION}"
fi

pass "found ${#scripts[@]} gate script(s) by filesystem enumeration"
for path in "${scripts[@]}"; do
  printf '      %s\n' "${path}"
done

echo
echo "=== 2. every exemption names a script that exists and is not invoked"

if [[ "${#EXEMPT[@]}" -eq 0 ]]; then
  pass "the exemption list is empty, so every gate script must be invoked"
else
  for entry in "${EXEMPT[@]}"; do
    exempt_path="${entry%%|*}"
    reason="${entry#*|}"

    if [[ ! -f "${REPO_ROOT}/${exempt_path}" ]]; then
      fail "stale exemption: ${exempt_path} does not exist"
      continue
    fi

    if [[ -z "${reason}" || "${reason}" == "${entry}" ]]; then
      fail "exemption for ${exempt_path} states no reason"
      continue
    fi

    if [[ -n "$(call_sites "${exempt_path}")" ]]; then
      fail "${exempt_path} is exempted but is in fact invoked; remove the exemption"
      continue
    fi

    pass "exempt: ${exempt_path} (${reason})"
  done
fi

echo
echo "=== 3. every gate script is invoked or exempt, and never both"

for path in "${scripts[@]}"; do
  sites="$(call_sites "${path}")"

  if [[ -n "${sites}" ]]; then
    if is_exempt "${path}"; then
      # Reported by section 2 as well; repeated here so the partition's own
      # statement is complete in one place.
      fail "${path} is both invoked and exempt"
      continue
    fi
    first="$(printf '%s\n' "${sites}" | head -n 1)"
    pass "invoked: ${path}  <-  $(printf '%s' "${first}" | cut -d: -f1,2)"
    continue
  fi

  if is_exempt "${path}"; then
    continue
  fi

  fail "${path} is never invoked and is not on the deliberately-unwired list."
  printf '      It runs only when a person remembers to type it, so any report of\n'
  printf '      "all gates green" silently excludes it. Wire it into the verb that\n'
  printf '      owns it, or add it to EXEMPT in build/verify-gate-wiring.sh with a\n'
  printf '      reason.\n'
done

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-gate-wiring: PASS"
  exit 0
fi

echo "verify-gate-wiring: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
