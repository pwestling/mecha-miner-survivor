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
# A1 asserts an absence, so on its own its `ok` line cannot distinguish "no wrapper
# dispatches on the verb" from "this check is incapable of noticing one". § A3 supplies
# the negative controls Decision 11 rule 4 requires: copies of build.sh that do dispatch,
# one at fixture size and one production-sized, plus two controls that keep the predicate
# from being unconditionally positive. They run in band on every invocation, against
# copies under a private temporary directory, and never touch the committed wrapper.
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

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, skip for a required check that did not run, and section/gate_summary so a red run
# names the failing section. See build/gate-output.sh for why control output is marked and
# why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

section "A1. neither wrapper branches on the verb (proof by construction)"
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

# Emits one description per dispatch construct present outside comments, one per line,
# and nothing at all for a wrapper that dispatches on nothing. Factored out of A1 so the
# negative controls in A3 drive the identical predicate rather than a paraphrase of it.
#
# The two greps write into shell variables and are never chained into a status-bearing
# pipeline. The construct this replaced was
#
#   grep -nE "${pattern}" "${wrapper}" | grep -vE '^[0-9]+:[[:space:]]*#' | grep -q .
#
# and under `set -o pipefail` it reported the opposite of the truth on any real
# violation. `grep -q` exits the instant it matches, the middle `grep -v` dies of
# SIGPIPE, and the PIPELINE's status is 141 - which on a negative assertion ("this
# construct must not appear") reads as "not present", so `found` stayed empty and A1
# printed `ok  build.sh contains no verb dispatch` for a wrapper that dispatched. The
# threshold is grep's own ~4 KB stdout buffer, so the failure mode is inverted with
# respect to severity: measured over 300 trials per size, 0 of 300 missed at 480 bytes
# of matched output, 37 of 300 at 4.3 KB, 188 of 300 at 73 KB, and 300 of 300 at
# 180 KB. The larger the violation the more reliably the gate reported success. A1's
# own fixtures were one-liners, which is why four review rounds did not see it.
dispatch_constructs() {
  local file="$1"
  local entry description pattern matched non_comment
  for entry in "${DISPATCH_PATTERNS[@]}"; do
    IFS='|' read -r description pattern <<<"${entry}"
    matched="$(grep -nE "${pattern}" "${file}" || true)"
    [[ -n "${matched}" ]] || continue
    non_comment="$(grep -vE '^[0-9]+:[[:space:]]*#' <<<"${matched}" || true)"
    [[ -n "${non_comment}" ]] || continue
    printf '%s\n' "${description}"
  done
}

for wrapper in "${SHELL_WRAPPER}" "${POWERSHELL_WRAPPER}"; do
  name="$(basename "${wrapper}")"
  mapfile -t found < <(dispatch_constructs "${wrapper}")

  if [[ "${#found[@]}" -eq 0 ]]; then
    pass "${name} contains no verb dispatch: no case, switch, \$1, shift, or args[0]"
  else
    fail "${name} branches on the verb: $(printf '%s; ' "${found[@]}")"
  fi
done

section "A2. both wrappers launch the same host project and forward all arguments"
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

section "A3. negative controls: A1 can see a wrapper that does dispatch (Decision 11 rule 4)"
#
# Until this section existed, A1 asserted an absence and nothing ever showed it
# detecting a presence, so its `ok` line carried no information: an A1 that could never
# fail and an A1 that passes look identical in the log. Decision 11 rule 4 requires the
# fixture that must fail to actually fail in the same run.
#
# Every control runs against a COPY under a private temporary directory. The committed
# build.sh is never modified and nothing is written inside the repository, so a control
# cannot leave litter or race another gate reading the tracked tree.
#
# Control 2 is deliberately production-sized. A one-line fixture is not a control for
# the SIGPIPE defect described over dispatch_constructs above - it is a control that
# structurally cannot fail on it, because the miss rate is zero below grep's ~4 KB
# buffer. The corpus below therefore includes a case with >170 KB of matched output,
# which the construct this replaced missed on 300 of 300 trials.
control_root="$(mktemp -d)"
remove_control_root() {
  rm -rf "${control_root}"
}
trap remove_control_root EXIT

# A coherent violation, not a broken state (doc 91 § Negative control adequacy): a
# complete, syntactically valid copy of build.sh that additionally dispatches on the
# verb exactly the way doc 100 forbids. `bash -n` is asserted below, so a red result
# cannot be the fixture failing to parse.
write_dispatching_copy() {
  local destination="$1"
  local repetitions="$2"
  local i
  cp "${SHELL_WRAPPER}" "${destination}"
  {
    printf '\n# --- injected by build/verify-wrapper-parity.sh A3; never committed ---\n'
    for ((i = 0; i < repetitions; i++)); do
      printf 'case "$1" in\n'
      printf '  build-%s) shift; exec dotnet "${HOST_ASSEMBLY}" "${REPO_ROOT}" build "$@" ;;\n' "${i}"
      printf '  test-%s)  shift; exec dotnet "${HOST_ASSEMBLY}" "${REPO_ROOT}" test-fast "$@" ;;\n' "${i}"
      printf 'esac\n'
    done
  } >>"${destination}"
}

readonly REQUIRED_CONSTRUCTS=(
  "a case statement"
  "a comparison against the first argument"
  "a positional-argument shift"
)

# "<label>|<repetitions>"
readonly DISPATCH_CONTROLS=(
  "one injected dispatch block (fixture size)|1"
  "a production-sized dispatch (>170 KB of matched output)|2400"
)

for control in "${DISPATCH_CONTROLS[@]}"; do
  IFS='|' read -r label repetitions <<<"${control}"
  fixture="${control_root}/build-dispatching-${repetitions}.sh"
  write_dispatching_copy "${fixture}" "${repetitions}"

  if ! bash -n "${fixture}" 2>/dev/null; then
    control_fail "A3 control '${label}': the fixture does not parse, so a red result would be ambiguous"
    continue
  fi

  # The size the control actually ran at, measured rather than asserted, so a reader
  # does not have to trust the label. This is the quantity the defect was sensitive to:
  # the bytes each pattern's `grep -nE` emits into the stage that used to be killed.
  matched_bytes=0
  for entry in "${DISPATCH_PATTERNS[@]}"; do
    IFS='|' read -r _description pattern <<<"${entry}"
    pattern_bytes="$(grep -nE "${pattern}" "${fixture}" | wc -c | tr -d ' ')"
    matched_bytes=$((matched_bytes + pattern_bytes))
  done

  mapfile -t detected < <(dispatch_constructs "${fixture}")
  missing=()
  for required in "${REQUIRED_CONSTRUCTS[@]}"; do
    found_required=0
    for entry in "${detected[@]-}"; do
      [[ "${entry}" == "${required}" ]] && found_required=1
    done
    [[ "${found_required}" -eq 1 ]] || missing+=("${required}")
  done

  if [[ "${#missing[@]}" -eq 0 ]]; then
    control_pass "A3 control: ${label} is detected at ${matched_bytes} bytes of matched output - $(printf '%s; ' "${detected[@]}")"
  else
    control_fail "A3 control: ${label} (${matched_bytes} bytes of matched output) was NOT detected; A1 cannot see this violation. Missed: $(printf '%s; ' "${missing[@]}")"
  fi
done

# Control 3: the predicate is not unconditionally positive. An untouched copy of the
# committed wrapper must report nothing, otherwise controls 1 and 2 would be satisfied
# by a predicate that reports dispatch for every input.
cp "${SHELL_WRAPPER}" "${control_root}/build-unmodified.sh"
mapfile -t clean_detected < <(dispatch_constructs "${control_root}/build-unmodified.sh")
if [[ "${#clean_detected[@]}" -eq 0 ]]; then
  control_pass "A3 control: an unmodified copy of build.sh reports no dispatch, so the predicate is not unconditionally positive"
else
  control_fail "A3 control: an unmodified copy of build.sh reported dispatch: $(printf '%s; ' "${clean_detected[@]}")"
fi

# Control 4: the comment filter still means something, at production size. The middle
# `grep -v` is the stage that used to be killed by SIGPIPE, so a fix that simply dropped
# it would pass controls 1-3 and start failing on ordinary prose. This control is the
# one that catches that: >170 KB of matched lines, every one of them a comment.
{
  printf '# --- injected by build/verify-wrapper-parity.sh A3; comments only ---\n'
  for ((i = 0; i < 2400; i++)); do
    printf '# case "$1" in build) shift ;; esac  -- prose about $1 and shift, line %s\n' "${i}"
  done
} >"${control_root}/build-comments-only.sh"
mapfile -t comment_detected < <(dispatch_constructs "${control_root}/build-comments-only.sh")
if [[ "${#comment_detected[@]}" -eq 0 ]]; then
  control_pass "A3 control: $(wc -c <"${control_root}/build-comments-only.sh" | tr -d ' ') bytes of commented-out dispatch reports nothing, so the comment filter survives at production size"
else
  control_fail "A3 control: commented-out dispatch was reported as a violation: $(printf '%s; ' "${comment_detected[@]}"); the comment filter is not being applied"
fi

remove_control_root
trap - EXIT

section "B. behavioral parity: identical usage tables from both wrappers (linux-x64 only)"
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

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-wrapper-parity" "${EXIT_VALIDATION}"
