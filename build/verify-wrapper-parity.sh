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
# Parity is proved two ways. Only check A runs every time; check B runs only where
# pwsh is installed, and this repository does not pin pwsh, so on a clean checkout the
# only parity assertion that executes is A:
#
#   A. Structural, and platform-independent. Neither wrapper may inspect or branch
#      on the verb, and both must launch the same host project and forward every
#      argument verbatim. That is proof by construction: there is exactly one verb
#      table, in src/MechaMiner.Tools/Cli/VerbRegistry.cs, so there is nothing that
#      could drift between the two shell languages.
#
#   B. Behavioral, when PowerShell is available. Run both wrappers and require
#      byte-identical usage tables. PowerShell is not a pinned requirement on Linux
#      or macOS (build/toolchain.json lists pwsh under optional_tools), so on a clean
#      checkout of this repository check B does not run at all.
#
# What check B therefore does NOT establish, on any platform:
#
#   - It never proves parity on windows-x64 or osx-arm64. It executes build.ps1 on
#     whatever host it is running on, which in CI and in every container built from
#     build/bootstrap-linux.sh is linux-x64. VER-FND-002-008 records linux-x64 only,
#     and names the other two platforms as pending, for exactly this reason.
#   - When pwsh is absent it proves nothing at all, and a skipped required check that
#     is only visible in the middle of a long log is indistinguishable from a passed
#     one at a glance. So a skip is counted, echoed in the final summary line, and
#     restated after it - the summary never reads a bare "PASS" while a required
#     check did not run.
#
# A skip is not a failure: pwsh is unpinned by decision (delivery-waves.md § Decision
# 8), so its absence is expected and check A remains the binding proof on this
# platform. It is a reduction in coverage, and the summary says so out loud.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure. A skip does not change the class; it changes the summary.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly SHELL_WRAPPER="${REPO_ROOT}/build.sh"
readonly POWERSHELL_WRAPPER="${REPO_ROOT}/build.ps1"
readonly EXIT_VALIDATION=4

failures=0
skipped_checks=()

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

# A required check that did not run. Recorded rather than merely printed, so the
# summary at the bottom of this script cannot read as unqualified success.
skip() {
  printf 'SKIP  %s\n' "$*"
  skipped_checks+=("$*")
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
echo "=== B. behavioral parity: identical usage tables from both wrappers (linux-x64 only)"
if command -v pwsh >/dev/null 2>&1; then
  echo "      pwsh: $(pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>/dev/null || echo 'version unavailable')"
  echo "      Host platform for this run: $(uname -s)-$(uname -m). This check proves parity"
  echo "      on this platform only; windows-x64 and osx-arm64 remain unexercised."
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
  echo "      pwsh is not installed in this environment, so build.ps1 was never executed."
  echo "      PowerShell is not a pinned requirement on Linux or macOS (build/toolchain.json"
  echo "      lists it under optional_tools), so this is expected and is not a failure."
  echo "      Structural parity checks A1 and A2 above still apply and are the binding"
  echo "      proof on this platform. Behavioral parity is simply unproved in this run."
  skip "B. behavioral parity: build.ps1 never executed (pwsh absent); VER-FND-002-008 unproved in this run"
fi

echo
if [[ "${#skipped_checks[@]}" -gt 0 ]]; then
  echo "verify-wrapper-parity: ${#skipped_checks[@]} required check(s) DID NOT RUN:"
  for skipped in "${skipped_checks[@]}"; do
    printf '  SKIPPED  %s\n' "${skipped}"
  done
  echo
fi

if [[ "${failures}" -eq 0 ]]; then
  if [[ "${#skipped_checks[@]}" -gt 0 ]]; then
    # Deliberately not a bare "PASS". Every assertion that ran passed, and the reader
    # is told in the same line that coverage was reduced, so the result cannot be
    # quoted as "parity proved by execution" when build.ps1 never ran.
    echo "verify-wrapper-parity: PASS WITH ${#skipped_checks[@]} SKIPPED CHECK(S) - coverage reduced, see above"
    exit 0
  fi
  echo "verify-wrapper-parity: PASS (no skipped checks)"
  exit 0
fi
echo "verify-wrapper-parity: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
