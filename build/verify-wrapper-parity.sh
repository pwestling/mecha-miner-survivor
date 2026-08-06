#!/usr/bin/env bash
#
# Proves that build.sh and build.ps1 expose an identical verb and argument table.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface ("The root wrappers ./build.sh and
#              ./build.ps1 expose identical verbs and argument names.")
# Requirements: TR-BLD-005
# Verification: VER-FND-002-008
#
# Parity is proved two ways, and both run every time:
#
#   A. Structural, and platform-independent. Neither wrapper may inspect or branch
#      on the verb, and both must launch the same host project and forward every
#      argument verbatim. That is proof by construction: there is exactly one verb
#      table, in src/MechaMiner.Tools/Cli/VerbRegistry.cs, so there is nothing that
#      could drift between the two shell languages.
#
#   B. Behavioral, when PowerShell is available. Run both wrappers and require
#      byte-identical usage tables. PowerShell is not a pinned requirement on Linux
#      or macOS, so when pwsh is absent this check reports that it was skipped, by
#      name, and check A still has to pass. It never silently passes.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly SHELL_WRAPPER="${REPO_ROOT}/build.sh"
readonly POWERSHELL_WRAPPER="${REPO_ROOT}/build.ps1"
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

echo "=== A1. neither wrapper branches on the verb (proof by construction)"
#
# The binding structural property is not "the word never appears" - "dotnet build"
# and "re-run" are ordinary English and shell. It is that neither wrapper inspects
# or dispatches on the verb argument at all: there is no case, no switch, no
# comparison against $1, and no shift. Every argument is forwarded verbatim to the
# one verb table in src/MechaMiner.Tools/Cli/VerbRegistry.cs, so the two wrappers
# cannot expose different verbs or different argument names.
#
mapfile -t verbs < <("${SHELL_WRAPPER}" 2>&1 \
  | sed -n '/^VERB TABLE/,/^$/p' \
  | sed -n 's/^  \([a-z-]*\).*/\1/p' \
  | sed '/^$/d')

if [[ "${#verbs[@]}" -eq 18 ]]; then
  pass "read exactly 18 verb names from the one shared verb table"
else
  fail "expected 18 verbs in the shared table, read ${#verbs[@]}"
fi

# "<description>|<extended regex that must not match outside comments>"
readonly DISPATCH_PATTERNS=(
  "a case statement|(^|;|then|do)[[:space:]]*case[[:space:]]"
  "a switch statement|(^|[^[:alnum:]_])switch[[:space:]]*[(\{]"
  "a comparison against the first argument|\\$\\{?1\\}?"
  "a positional-argument shift|(^|[^[:alnum:]_])shift([^[:alnum:]_]|$)"
  "an indexed read of the argument vector|(args|Args|argv)\\[0\\]"
)

for wrapper in "${SHELL_WRAPPER}" "${POWERSHELL_WRAPPER}"; do
  name="$(basename "${wrapper}")"
  found=()
  for entry in "${DISPATCH_PATTERNS[@]}"; do
    IFS='|' read -r description pattern <<<"${entry}"
    if grep -nE "${pattern}" "${wrapper}" | grep -vE '^[0-9]+:[[:space:]]*#' | grep -q .; then
      found+=("${description}")
    fi
  done

  if [[ "${#found[@]}" -eq 0 ]]; then
    pass "${name} contains no verb dispatch: no case, switch, \$1, shift, or args[0]"
  else
    fail "${name} branches on the verb: $(printf '%s; ' "${found[@]}")"
  fi
done

echo
echo "=== A2. both wrappers launch the same host project and forward all arguments"
readonly HOST_PATH='src/MechaMiner.Tools/MechaMiner.Tools.csproj'
readonly HOST_ASSEMBLY_PATH='src/MechaMiner.Tools/bin/'

for wrapper in "${SHELL_WRAPPER}" "${POWERSHELL_WRAPPER}"; do
  name="$(basename "${wrapper}")"
  if grep -qF "${HOST_PATH}" "${wrapper}"; then
    pass "${name} builds ${HOST_PATH}"
  else
    fail "${name} does not reference ${HOST_PATH}"
  fi

  if grep -qF "${HOST_ASSEMBLY_PATH}" "${wrapper}"; then
    pass "${name} runs the assembly under ${HOST_ASSEMBLY_PATH}"
  else
    fail "${name} does not run the host assembly from the expected output path"
  fi
done

if grep -qF 'exec dotnet "${HOST_ASSEMBLY}" "${REPO_ROOT}" "$@"' "${SHELL_WRAPPER}"; then
  pass "build.sh forwards every argument verbatim with \"\$@\""
else
  fail "build.sh does not forward every argument verbatim"
fi

if grep -qF '& dotnet $hostAssembly $repoRoot @args' "${POWERSHELL_WRAPPER}"; then
  pass "build.ps1 forwards every argument verbatim with @args"
else
  fail "build.ps1 does not forward every argument verbatim"
fi

echo
echo "=== B. behavioral parity: identical usage tables from both wrappers"
if command -v pwsh >/dev/null 2>&1; then
  shell_table="$("${SHELL_WRAPPER}" 2>&1 | sed -n '/^VERB TABLE/,$p')"
  powershell_table="$(pwsh -NoLogo -NoProfile -File "${POWERSHELL_WRAPPER}" 2>&1 \
    | sed -n '/^VERB TABLE/,$p')"

  if [[ -z "${powershell_table}" ]]; then
    fail "pwsh is present but build.ps1 emitted no verb table"
  elif [[ "${shell_table}" == "${powershell_table}" ]]; then
    pass "build.sh and build.ps1 emitted byte-identical verb and exit-class tables"
    printf '%s\n' "${shell_table}" | sed 's/^/      /'
  else
    fail "the two wrappers emitted different tables"
    diff <(printf '%s\n' "${shell_table}") <(printf '%s\n' "${powershell_table}") || true
  fi

  shell_status=0
  powershell_status=0
  "${SHELL_WRAPPER}" definitely-not-a-verb >/dev/null 2>&1 || shell_status=$?
  pwsh -NoLogo -NoProfile -File "${POWERSHELL_WRAPPER}" definitely-not-a-verb >/dev/null 2>&1 \
    || powershell_status=$?
  if [[ "${shell_status}" -eq "${powershell_status}" && "${shell_status}" -eq 2 ]]; then
    pass "both wrappers return exit class 2 for the same unknown verb"
  else
    fail "unknown-verb exit class differs: build.sh ${shell_status}, build.ps1 ${powershell_status}"
  fi
else
  echo "      SKIPPED: pwsh is not installed in this environment."
  echo "      PowerShell is not a pinned requirement on Linux or macOS (build/toolchain.json"
  echo "      lists it under optional_tools). Structural parity checks A1 and A2 above still"
  echo "      apply and are the binding proof on this platform."
  pass "behavioral parity check reported as skipped rather than passed"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-wrapper-parity: PASS"
  exit 0
fi
echo "verify-wrapper-parity: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
