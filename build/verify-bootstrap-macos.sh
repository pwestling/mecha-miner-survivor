#!/usr/bin/env bash
#
# Static gate for build/bootstrap-macos.sh. Runs on any platform, including CI
# Linux, and asserts the properties of that script which CAN be checked without a
# Mac.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Toolchain pinning ("checks hashes"), § Continuous integration
#            docs/technical/00-technical-foundation.md § Platform boundary
# Requirements: TR-BLD-001, TR-BLD-002, TR-FND-001, TR-FND-003
#
# WHY THIS FILE EXISTS
#
#   build/bootstrap-macos.sh cannot be executed by CI, because CI is Linux. A
#   provisioning script that no gate touches is a script that silently rots: the
#   pinned SDK version drifts away from global.json, a hash constant gets edited
#   to "fix" a failure, or a later edit reintroduces the root requirement or a
#   GNU-only flag that only fails on the machine of the one developer who runs it.
#   Everything below is checkable statically, so all of it is checked here.
#
#   The one thing this gate cannot do is prove the script provisions a Mac. It
#   makes no such claim, and check 11 exists to stop the script from making it
#   either.
#
# FORWARD-COMPATIBLE HALF
#
#   build/toolchain.json is NOT present on master; it belongs to the FND-002
#   branch chain (14 remote heads carried it when this was written, and master was
#   not one of them; the total head count moves as branches are pushed, so the
#   denominator is deliberately not recorded here). So
#   the macOS artifact hashes this repository now knows live in
#   build/bootstrap-macos.sh, and nothing else on master can hold them.
#
#   Section 10 handles the merge. If build/toolchain.json is absent it reports
#   SKIP, by name and counted. If it is present - which is what happens the
#   moment this branch meets the FND-002 chain - it becomes a hard assertion that
#   the pin file's osx-* entries agree with the bootstrap script's constants and
#   that unpinned_platform_policy no longer claims macOS is unrecorded. Two
#   records of the same hash that nothing compares is exactly the class of defect
#   this repository keeps shipping.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT
readonly SCRIPT="${REPO_ROOT}/build/bootstrap-macos.sh"
readonly LINUX_SCRIPT="${REPO_ROOT}/build/bootstrap-linux.sh"
readonly GLOBAL_JSON="${REPO_ROOT}/global.json"
readonly PIN_FILE="${REPO_ROOT}/build/toolchain.json"
readonly EXIT_VALIDATION=4

# hostfxr's hardcoded macOS default probe path. dotnet/runtime,
# src/native/corehost/hostmisc/pal.unix.cpp,
# pal::get_default_installation_dir_for_arch(): "/usr/local/share/dotnet" under
# __APPLE__. If this constant and the script ever disagree, one of them is wrong
# and the headless Godot launch is what breaks.
readonly MACOS_DOTNET_PROBE_PATH="/usr/local/share/dotnet"

failures=0
skipped=0
skipped_names=()

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

skip() {
  printf 'SKIP  %s\n' "$*"
  skipped=$((skipped + 1))
  skipped_names+=("$1")
}

# Prefixes each line of a captured block for reporting. A loop, not
# `sed 's/^/      /'`, so shellcheck stays silent on a variable substitution.
indent_lines() {
  local line
  while IFS= read -r line; do
    printf '      %s\n' "${line}"
  done <<<"$1"
}

# Reads a `readonly NAME="value"` constant out of the script by text, not by
# sourcing it. Sourcing would execute main() and try to provision this Linux box.
constant_of() {
  sed -n 's/^readonly '"$1"'="\([^"]*\)".*/\1/p' "${SCRIPT}" | head -1
}

if [[ ! -f "${SCRIPT}" ]]; then
  fail "build/bootstrap-macos.sh does not exist"
  echo "verify-bootstrap-macos: FAIL (1 assertion(s))"
  exit "${EXIT_VALIDATION}"
fi

echo "=== 1. the script parses"
if bash -n "${SCRIPT}" 2>/dev/null; then
  pass "bash -n reports no syntax error"
else
  fail "bash -n reports a syntax error"
  bash -n "${SCRIPT}" 2>&1 | sed 's/^/      /'
fi

echo
echo "=== 2. shellcheck"
if command -v shellcheck >/dev/null 2>&1; then
  shellcheck_output="$(shellcheck "${SCRIPT}" 2>&1)"
  if [[ -z "${shellcheck_output}" ]]; then
    pass "shellcheck reports no finding"
  else
    fail "shellcheck reports finding(s)"
    printf '%s\n' "${shellcheck_output}" | sed 's/^/      /'
  fi
else
  # Counted and named, not silently passed: a run with a skip must not be
  # readable as a run in which shellcheck was clean.
  skip "shellcheck (not installed on this host)"
fi

echo
echo "=== 3. it is executable"
if [[ -x "${SCRIPT}" ]]; then
  pass "build/bootstrap-macos.sh has its executable bit"
else
  fail "build/bootstrap-macos.sh is not executable"
fi

echo
echo "=== 4. the pinned SDK version agrees with global.json"
declared_sdk="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "${GLOBAL_JSON}" | head -1)"
script_sdk="$(constant_of DOTNET_SDK_VERSION)"
if [[ -z "${script_sdk}" ]]; then
  fail "cannot find DOTNET_SDK_VERSION in the script"
elif [[ "${script_sdk}" == "${declared_sdk}" ]]; then
  pass "DOTNET_SDK_VERSION=${script_sdk} matches global.json"
else
  fail "DOTNET_SDK_VERSION=${script_sdk} but global.json pins ${declared_sdk}"
fi

echo
echo "=== 5. the Godot version agrees with build/bootstrap-linux.sh"
# The two hosts must provision the SAME engine. A macOS-only Godot bump is a
# split-brain toolchain, and doc 100 pins one editor version for the repository.
linux_godot="$(sed -n 's/^readonly GODOT_VERSION="\([^"]*\)".*/\1/p' "${LINUX_SCRIPT}" | head -1)"
macos_godot="$(constant_of GODOT_VERSION)"
if [[ -n "${linux_godot}" && "${linux_godot}" == "${macos_godot}" ]]; then
  pass "GODOT_VERSION=${macos_godot} matches bootstrap-linux.sh"
else
  fail "GODOT_VERSION=${macos_godot} but bootstrap-linux.sh pins '${linux_godot}'"
fi

echo
echo "=== 6. .NET installs to hostfxr's default macOS probe path"
script_dir="$(constant_of DOTNET_INSTALL_DIR)"
if [[ "${script_dir}" == "${MACOS_DOTNET_PROBE_PATH}" ]]; then
  pass "DOTNET_INSTALL_DIR=${script_dir}"
else
  fail "DOTNET_INSTALL_DIR=${script_dir}, expected ${MACOS_DOTNET_PROBE_PATH}"
  printf '      %s\n' \
    "Godot's GodotPlugins host resolves the runtime from the default probe path" \
    "with no DOTNET_ROOT set. A different directory breaks the headless launch."
fi

# Asserting the DECLARATION alone was not enough, and this is the single failure
# this section's comment says it exists to prevent. constant_of() reads
# `^readonly DOTNET_INSTALL_DIR="..."` by text, so leaving that line untouched and
# pointing the install code at a second variable passed this check green: the
# declaration still said /usr/local/share/dotnet while the SDK was extracted to
# $HOME/.dotnet, off hostfxr's probe path. `bash -n` was clean, `shellcheck` was
# clean, the gate reported zero failures, and the symptom on the developer's Mac is
# a game that will not launch and looks like a Godot bug.
#
# So assert that the code which actually creates and fills the install directory
# names ${DOTNET_INSTALL_DIR} literally. A shadow variable now fails here.
assert_targets_install_dir() {
  local label="$1" pattern="$2" lines
  # Comment lines are dropped so the script may still discuss these operations in
  # prose; `|| true` because no match is a distinct, separately-reported failure.
  lines="$(grep -nE "${pattern}" "${SCRIPT}" | grep -vE '^[0-9]+:[[:space:]]*#' || true)"
  if [[ -z "${lines}" ]]; then
    fail "${label} is not present in the script at all; this check cannot confirm the install target"
    printf '      %s\n' "searched for: ${pattern}"
    return
  fi
  # A here-string, deliberately: `grep -qv` fed by a pipe from a file-reading grep
  # is the exact SIGPIPE race that section 8 documents.
  if grep -qvF "\${DOTNET_INSTALL_DIR}" <<<"${lines}"; then
    fail "${label} does not target \${DOTNET_INSTALL_DIR}"
    indent_lines "${lines}"
    printf '      %s\n' \
      "The declaration above can be correct while the install goes somewhere else." \
      "An SDK outside ${MACOS_DOTNET_PROBE_PATH} is not on hostfxr's probe path."
  else
    pass "${label} targets \${DOTNET_INSTALL_DIR}"
  fi
}
assert_targets_install_dir "the privilege-escalation call" 'ensure_install_dir_writable[[:space:]]+"'
assert_targets_install_dir "the tar extraction" 'tar[[:space:]]+-xzf'

echo
echo "=== 7. DOTNET_ROOT is not substituted for the probe path"
# Setting DOTNET_ROOT instead of using the probe path is the specific wrong fix
# this check exists to prevent. Mentioning it in a comment is fine; exporting it
# is not.
if grep -nE '^[[:space:]]*(export[[:space:]]+)?DOTNET_ROOT=' "${SCRIPT}" >/dev/null; then
  fail "the script assigns DOTNET_ROOT"
  grep -nE '^[[:space:]]*(export[[:space:]]+)?DOTNET_ROOT=' "${SCRIPT}" | sed 's/^/      /'
else
  pass "no DOTNET_ROOT assignment"
fi

echo
echo "=== 8. it does not demand root"
# bootstrap-linux.sh legitimately hard-fails on EUID != 0. This script must not:
# a Mac developer is not root, and a bootstrap that refuses to start is one
# nobody runs. Narrowly-scoped sudo for mkdir/chown is the intended shape.
#
# This check was `grep -nE 'EUID.*-ne[[:space:]]+0' "${SCRIPT}" | grep -qv 'warn'`,
# and it had two independent holes.
#
#   1. SIGPIPE. Under `set -o pipefail` the right-hand `grep -qv` exits at its
#      first non-matching line; the left-hand grep is then killed by SIGPIPE and
#      the pipeline yields 141, which `if` reads as FALSE and falls through to
#      pass. On a script carrying 1500 hard-fail-on-non-root lines this printed
#      "ok no root requirement" in 10 runs out of 12 - and FAILed in the other 2.
#      It was a race, not a check. The single-line negative control passed only
#      because one line is too little output to make the left grep block.
#      So: capture into variables and match with here-strings. No pipe, no signal,
#      no race.
#   2. One spelling. `-ne 0` was the only form recognised, so the equally
#      idiomatic `[[ "${EUID}" -eq 0 ]] || fail ...` walked straight through, as
#      did anything phrased with `$(id -u)`.
#
# The warn_if_root exclusion is kept, but by ENCLOSING FUNCTION rather than by
# looking for the word "warn" on the matched line. That text test only ever worked
# by accident: warn_if_root's condition line is `if [[ "${EUID}" -eq 0 ]]; then`,
# which contains no "warn" at all, and it escaped the old check purely because the
# old pattern could not see `-eq 0`. Widening the pattern without fixing the
# exclusion would have turned this check into a permanent false FAIL.
root_gate_hits="$(awk '
  /^[[:space:]]*#/ { next }
  /^[a-zA-Z_][a-zA-Z0-9_]*\(\)[[:space:]]*\{/ { fn = $0; sub(/\(\).*/, "", fn); next }
  /(EUID|id -u)/ &&
  /(-ne[[:space:]]+0|-eq[[:space:]]+0|!=[[:space:]]*"?0|==[[:space:]]*"?0)/ {
    if (fn != "warn_if_root") { printf "%d:%s\n", NR, $0 }
  }
' "${SCRIPT}")"
if [[ -n "${root_gate_hits}" ]]; then
  fail "the script appears to hard-fail on a non-root EUID"
  indent_lines "${root_gate_hits}"
else
  pass "no root requirement"
fi

echo
echo "=== 9. hash constants are present and well-formed"
# A pinned artifact with no hash, or a hash of the wrong width, is the failure
# this repository's unpinned_platform_policy was written about.
check_hex() {
  local name="$1" width="$2" value
  value="$(constant_of "${name}")"
  if [[ -z "${value}" ]]; then
    fail "${name} is missing"
  elif [[ ! "${value}" =~ ^[0-9a-f]{${width}}$ ]]; then
    fail "${name} is not ${width} lowercase hex characters: '${value}'"
  else
    pass "${name} is a well-formed sha (${width} hex)"
  fi
}
check_hex GODOT_ARCHIVE_SHA256 64
check_hex GODOT_EXECUTABLE_SHA256 64
check_hex DOTNET_SHA512_ARM64 128
check_hex DOTNET_SHA512_X64 128

# A copied Linux hash masquerading as a macOS one is worse than a missing hash,
# because it looks recorded. bootstrap-linux.sh has no hash constants at all
# today, so this compares against the values on the FND-002 pin file when that
# file is present; here it just insists the two macOS SDK hashes differ from each
# other, which a copy-paste would violate.
if [[ "$(constant_of DOTNET_SHA512_ARM64)" == "$(constant_of DOTNET_SHA512_X64)" ]]; then
  fail "the arm64 and x64 .NET SDK hashes are identical; one is a copy of the other"
else
  pass "the arm64 and x64 .NET SDK hashes differ, as two different tarballs must"
fi

echo
echo "=== 10. build/toolchain.json agreement (skips until the FND-002 chain merges)"
if [[ ! -f "${PIN_FILE}" ]]; then
  skip "build/toolchain.json osx-* agreement (the file is not on this branch)"
  printf '      %s\n' \
    "build/toolchain.json is owned by the FND-002 chain and is absent from master." \
    "The macOS hashes live only in build/bootstrap-macos.sh until that file arrives;" \
    "this section becomes a hard assertion the moment it does."
else
  pin_report="$(python3 - "${PIN_FILE}" "$(constant_of GODOT_ARCHIVE_SHA256)" \
    "$(constant_of GODOT_EXECUTABLE_SHA256)" <<'PY'
import json, sys, re
path, want_archive, want_exe = sys.argv[1], sys.argv[2], sys.argv[3]
try:
    doc = json.load(open(path))
except Exception as exc:
    print("FAIL cannot parse %s: %s" % (path, exc)); sys.exit(0)
godot = doc.get("godot", {})
platforms = godot.get("platforms", {})
osx = {k: v for k, v in platforms.items() if k.startswith("osx-")}
if not osx:
    print("FAIL build/toolchain.json exists but records no godot.platforms.osx-* entry")
else:
    for key, entry in sorted(osx.items()):
        if entry.get("archive_sha256") != want_archive:
            print("FAIL %s archive_sha256 disagrees with bootstrap-macos.sh" % key)
            print("INFO   pin file %s" % entry.get("archive_sha256"))
            print("INFO   script   %s" % want_archive)
        else:
            print("PASS %s archive_sha256 agrees with bootstrap-macos.sh" % key)
        if entry.get("executable_sha256") != want_exe:
            print("FAIL %s executable_sha256 disagrees with bootstrap-macos.sh" % key)
        else:
            print("PASS %s executable_sha256 agrees with bootstrap-macos.sh" % key)
policy = godot.get("unpinned_platform_policy", "")
# The contradiction guard. Recording macOS hashes while the policy still says
# they are unrecorded is precisely the shape of defect this gate is for.
if osx and re.search(r"(macos|osx)", policy, re.I) and re.search(
        r"(only linux-x64|unrecorded|has been bootstrapped so far)", policy, re.I):
    print("FAIL unpinned_platform_policy still describes macOS as unrecorded while osx-* entries exist")
    print("INFO   policy: %s" % policy)
else:
    print("PASS unpinned_platform_policy does not contradict the recorded osx-* entries")
PY
)"
  while IFS= read -r line; do
    case "${line}" in
      PASS\ *) pass "${line#PASS }" ;;
      FAIL\ *) fail "${line#FAIL }" ;;
      INFO\ *) printf '      %s\n' "${line#INFO }" ;;
    esac
  done <<<"${pin_report}"
fi

echo
echo "=== 11. the script does not overclaim"
# The script has never run on macOS. It must not say otherwise, and it must say
# so plainly. This gate is the reason the claim stays true after later edits.
if grep -inE '\bverified\b' "${SCRIPT}" | grep -vE 'sha|hash|matches the pin|agree' >/dev/null; then
  fail "the script uses the word 'verified' outside a hash context"
  grep -inE '\bverified\b' "${SCRIPT}" | grep -vE 'sha|hash|matches the pin|agree' | sed 's/^/      /'
else
  pass "no bare 'verified' claim"
fi

if grep -qF 'NOT RUN ON HARDWARE' "${SCRIPT}"; then
  pass "the unrun-on-hardware banner is present"
else
  fail "the unrun-on-hardware banner has been removed"
fi

echo
echo "=== 12. no GNU-only or Linux-only idiom outside comments"
# Each of these parses fine on Linux and fails at runtime on macOS, which is the
# worst possible place to find out. Comments are excluded so the script can
# explain the difference.
#
# WHOLE-LINE comments only. The previous `sed 's/[[:space:]]*#.*$//'` cut the line
# at any '#' whatsoever, including the '#' of a ${#array[@]} parameter expansion:
# on bootstrap-macos.sh's own `if [[ "${#missing[@]}" -gt 0 ]]; then` it left
# `  if [[ "${` and threw the rest away. Any GNU-only idiom sharing a line with a
# ${#...} was therefore invisible, and `sha256sum` - which does not exist on macOS
# - was demonstrated shipping past this check with the gate fully green.
#
# Dropping only whole-line comments also fails SAFE. A trailing comment that
# happens to name one of the idioms below now produces a FALSE FAILURE, which is
# loud and gets fixed, rather than a false pass, which is silent and does not.
# NOTE, because of that: trailing comments in build/bootstrap-macos.sh must not
# name any idiom in the list below. Put such a mention on its own comment line.
stripped="$(grep -vE '^[[:space:]]*#' "${SCRIPT}")"
gnuisms=0
for pattern in 'sha256sum' 'sha512sum' 'mktemp --suffix' 'stat -c' 'apt-get' 'readlink -f' '/opt/godot'; do
  if grep -qF -- "${pattern}" <<<"${stripped}"; then
    fail "GNU/Linux-only idiom in executable code: ${pattern}"
    gnuisms=$((gnuisms + 1))
  fi
done
if [[ "${gnuisms}" -eq 0 ]]; then
  pass "no GNU-only or Linux-only idiom in executable code"
fi

echo
echo "=== 13. both macOS architectures are handled"
if grep -qE '\barm64\)' <<<"${stripped}" && grep -qE '\bx86_64\)' <<<"${stripped}"; then
  pass "uname -m dispatch covers arm64 and x86_64"
else
  fail "the script does not dispatch on both arm64 and x86_64"
fi

echo
if [[ "${skipped}" -gt 0 ]]; then
  echo "verify-bootstrap-macos: ${skipped} required check(s) DID NOT RUN:"
  for name in "${skipped_names[@]}"; do
    printf '      skipped: %s\n' "${name}"
  done
  echo
fi

if [[ "${failures}" -eq 0 ]]; then
  echo "verify-bootstrap-macos: PASS (${skipped} skipped)"
  echo "NOTE: every check above is static. None of them executed build/bootstrap-macos.sh,"
  echo "      which cannot run on this host. This gate passing is not evidence that the"
  echo "      script provisions a Mac."
  exit 0
fi
echo "verify-bootstrap-macos: FAIL (${failures} assertion(s), ${skipped} skipped)"
exit "${EXIT_VALIDATION}"
