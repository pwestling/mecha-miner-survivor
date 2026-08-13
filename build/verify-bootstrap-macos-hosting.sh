#!/usr/bin/env bash
#
# DYNAMIC gate for the Godot/.NET hosting check inside build/bootstrap-macos.sh.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Toolchain pinning, § Continuous integration
# Requirements: TR-BLD-001, TR-BLD-002, TR-FND-001
#
# WHY THIS FILE EXISTS, AND HOW IT DIFFERS FROM ITS SIBLING
#
#   build/verify-bootstrap-macos.sh is a STATIC gate and says so: it reads
#   build/bootstrap-macos.sh as text and never executes any of it. That is the right
#   shape for pins, hashes and platform idioms, and nothing here changes it.
#
#   It is the wrong shape for a check whose whole value is what it does at runtime.
#   The hosting check in verify() was added as "a check that can fail", and the
#   static gate could not have told the difference between that and a check that
#   cannot: to a text scan, an import that adjudicates and an import that always
#   passes look the same. This file closes that gap. It EXECUTES the script's own
#   functions against Godot installs whose health it controls, so the answer to
#   "what perturbation of this code turns a gate red?" is a list, below, and not
#   "nothing beyond bash -n".
#
#   Nothing runs this automatically today - this repository has no CI workflow that
#   invokes either macOS gate. Run it by hand, or from whatever runs
#   build/verify-godot.sh, which has the same requirement: a real mono Godot on the
#   host. With no such Godot it reports SKIP and exits 0, so wiring it into a job
#   that runs on Godot-less hosts is safe.
#
# WHAT THIS CAN AND CANNOT ESTABLISH
#
#   Can: that assert_godot_hosts_dotnet() fails on a Godot that cannot reach its
#   .NET assemblies, by either arm - non-zero exit, or a complaint in the log with a
#   zero exit; that it passes on a healthy install with a cold cache and no game
#   assembly built; that MECHAMINER_GODOT is actually read; that a bad
#   ~/.local/bin/godot warns without aborting; and that the failure text does not
#   tell the developer to re-run the script to fix a symlink the script creates.
#
#   Cannot: anything about macOS. This host is Linux. Control 7 below MEASURES that
#   a healthy Godot invoked through a bare symlink imports cleanly and exits 0 here,
#   which means this container cannot reproduce the macOS failure README § macOS
#   describes. Control 5 reaches the symlink warning only by pointing the link at a
#   second, genuinely broken install. That exercises the branch. It is not evidence
#   for the invoked-path mechanism, and no control here claims to be.
#
#   The installs are built on the HOST's layout: the pinned Linux mono Godot's
#   binary hard-linked into place with its GodotSharp directory beside it, or
#   deliberately not beside it. It is not a Godot_mono.app emulation.
#
# Exit classes follow doc 100 § Standard command surface: 0 success (including
# SKIP), 4 validation failure.

set -uo pipefail

# HARNESS_REPO_ROOT, not REPO_ROOT: build/bootstrap-macos.sh declares its own
# REPO_ROOT `readonly`, and a readonly of that name in this shell survives into the
# subshells that source it - every case would die on "readonly variable" before
# reaching the code under test. Any name this harness holds must not collide with a
# constant that file declares.
#
# Declared and assigned separately: `readonly X="$(...)"` masks the subshell's exit
# status (shellcheck SC2155), the same note build/bootstrap-macos.sh carries.
HARNESS_REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly HARNESS_REPO_ROOT
readonly BOOTSTRAP="${HARNESS_REPO_ROOT}/build/bootstrap-macos.sh"
readonly EXIT_VALIDATION=4

failures=0
skipped=0

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
}

indent_lines() {
  local line
  while IFS= read -r line; do
    printf '      %s\n' "${line}"
  done <<<"$1"
}

# ---------------------------------------------------------------------------
# Is there a Godot here at all?
# ---------------------------------------------------------------------------
# SKIP, not FAIL: doc 100 § Continuous integration allows a job to run on a host
# without the game toolchain, and a harness that fails there would be turned off.
# The bar is a MONO build - a non-mono Godot cannot host .NET by construction and
# would make every control below red for a reason that is not a defect.

echo "=== locating a mono Godot to build controlled installs from"
harness_godot="${MECHAMINER_HARNESS_GODOT:-}"
if [[ -z "${harness_godot}" ]]; then
  harness_godot="$(command -v godot 2>/dev/null || true)"
fi

if [[ -z "${harness_godot}" || ! -x "${harness_godot}" ]]; then
  skip "no godot on PATH and MECHAMINER_HARNESS_GODOT unset - nothing to drive"
  echo
  echo "verify-bootstrap-macos-hosting: SKIPPED (${skipped}), nothing was executed"
  exit 0
fi

harness_version="$("${harness_godot}" --headless --version 2>/dev/null || true)"
if [[ "${harness_version}" != *mono* ]]; then
  skip "godot at ${harness_godot} reports '${harness_version}', which is not a mono build"
  echo
  echo "verify-bootstrap-macos-hosting: SKIPPED (${skipped}), nothing was executed"
  exit 0
fi

# The real binary and the GodotSharp directory it needs beside it. `readlink -f`:
# this harness is Linux-only by construction, so the GNU flag build/bootstrap-macos.sh
# must avoid is fine here.
real_godot="$(readlink -f "${harness_godot}")"
real_godot_dir="$(dirname -- "${real_godot}")"
real_godotsharp="${real_godot_dir}/GodotSharp"
if [[ ! -d "${real_godotsharp}" ]]; then
  skip "no GodotSharp beside ${real_godot} - cannot build a 'healthy' install from it"
  echo
  echo "verify-bootstrap-macos-hosting: SKIPPED (${skipped}), nothing was executed"
  exit 0
fi
pass "driving controls from ${real_godot} (${harness_version})"

WORK=""
# shellcheck disable=SC2317  # reached only through the EXIT trap below
cleanup() {
  if [[ -n "${WORK}" && -d "${WORK}" ]]; then
    rm -rf "${WORK}"
  fi
}
trap cleanup EXIT
WORK="$(mktemp -d)"

# ---------------------------------------------------------------------------
# Building installs whose health is controlled
# ---------------------------------------------------------------------------
# A "healthy" install is the real binary with a GodotSharp beside it; a "broken"
# one is the same real binary with no GodotSharp beside it. Both are the genuine
# 145 MB engine - hard-linked, so building six of them costs no disk - and the
# broken one fails the way a real one does: SIGABRT out of
# modules/mono/mono_gd/gd_mono.cpp. Nothing here is a stub except control 4, which
# is labelled as one because a stub is the only way to produce that shape.

# install_godot_at <bin-path> healthy|broken
install_godot_at() {
  local bin_path="$1" health="$2"
  mkdir -p "$(dirname -- "${bin_path}")"
  ln "${real_godot}" "${bin_path}" 2>/dev/null || cp "${real_godot}" "${bin_path}"
  chmod +x "${bin_path}"
  if [[ "${health}" == "healthy" ]]; then
    ln -sfn "${real_godotsharp}" "$(dirname -- "${bin_path}")/GodotSharp"
  fi
}

# A fake HOME plus a fake REPO_ROOT, so the constants build/bootstrap-macos.sh
# derives from ${HOME} and from its own location point at things this harness owns.
# The script is COPIED, byte for byte, into the fake root's build/ so that its
# REPO_ROOT - and therefore GODOT_PROJECT_DIR - is the copied game project and this
# harness never imports into the real working tree.
#
# make_case <name> <bundle-health> <symlink-target-kind>
#   symlink-target-kind: bundle | broken-other | stub-complains | none
make_case() {
  local name="$1" bundle_health="$2" link_kind="$3"
  local case_root="${WORK}/${name}"
  mkdir -p "${case_root}/home/.local/bin" "${case_root}/root/build"

  cp "${BOOTSTRAP}" "${case_root}/root/build/bootstrap-macos.sh"
  chmod +x "${case_root}/root/build/bootstrap-macos.sh"
  cp -R "${HARNESS_REPO_ROOT}/game" "${case_root}/root/game"
  # Cold cache, every time: a control that only passes with a warm .godot would be
  # a control that passes for the wrong reason.
  rm -rf "${case_root}/root/game/.godot"

  local bundle_bin="${case_root}/home/Applications/Godot_mono.app/Contents/MacOS/Godot"
  install_godot_at "${bundle_bin}" "${bundle_health}"

  local link="${case_root}/home/.local/bin/godot"
  case "${link_kind}" in
    bundle) ln -sfn "${bundle_bin}" "${link}" ;;
    broken-other)
      install_godot_at "${case_root}/other-install/Godot" broken
      ln -sfn "${case_root}/other-install/Godot" "${link}"
      ;;
    stub-complains)
      # The one stub in this file. It reproduces the shape the replaced
      # `--headless --version` check passed: a Godot that prints the .NET
      # complaint and exits 0. No real engine build available here does that, so
      # the log arm cannot be exercised any other way.
      printf '%s\n' '#!/usr/bin/env bash' \
        'echo "Unable to find the .NET assemblies directory."' \
        'exit 0' >"${link}"
      chmod +x "${link}"
      ;;
    none) : ;;
  esac
  printf '%s\n' "${case_root}"
}

# Runs the two functions under test inside one subshell with the case's HOME, and
# prints their combined output followed by two machine-readable trailer lines. A
# subshell because build/bootstrap-macos.sh's fail() calls exit, which is the
# behaviour being asserted.
#
# run_case <case-root> <mechaminer-godot-or-empty>
run_case() {
  local case_root="$1" override="$2"
  (
    export HOME="${case_root}/home"
    if [[ -n "${override}" ]]; then
      export MECHAMINER_GODOT="${override}"
    else
      unset MECHAMINER_GODOT
    fi
    # shellcheck source=/dev/null
    source "${case_root}/root/build/bootstrap-macos.sh"
    subject="$(godot_under_test)"
    assert_godot_hosts_dotnet "${subject}"
    warn_if_symlink_worse "${subject}"
    printf 'HARNESS_DEGRADED=%s\n' "${GODOT_SYMLINK_DEGRADED}"
  ) 2>&1
  printf 'HARNESS_STATUS=%s\n' "$?"
}

# assert_contains <label> <text> <needle>
assert_contains() {
  if grep -qF -- "$3" <<<"$2"; then
    pass "$1"
  else
    fail "$1 - expected to find: $3"
  fi
}

# assert_absent <label> <text> <needle>
assert_absent() {
  if grep -qF -- "$3" <<<"$2"; then
    fail "$1 - must NOT contain: $3"
  else
    pass "$1"
  fi
}

# assert_status <label> <text> <expected>
assert_status() {
  local got
  got="$(grep -o 'HARNESS_STATUS=[0-9]*' <<<"$2" | tail -n 1 | cut -d= -f2)"
  if [[ "${got}" == "$3" ]]; then
    pass "$1 (exit ${got})"
  else
    fail "$1 - expected exit $3, got '${got}'"
  fi
}

# ---------------------------------------------------------------------------
echo
echo "=== 1. POSITIVE CONTROL: healthy bundle, link to it, cold cache, no assembly built"
c1="$(make_case healthy healthy bundle)"
o1="$(run_case "${c1}" "")"
indent_lines "${o1}"
assert_status "healthy install passes" "${o1}" 0
assert_contains "reports the import ran with no .NET complaint" "${o1}" \
  "godot imported the project with no .NET complaint"
assert_contains "degraded flag stays clear" "${o1}" "HARNESS_DEGRADED=0"

# ---------------------------------------------------------------------------
echo
echo "=== 2. NEGATIVE CONTROL: the installed bundle binary cannot reach its assemblies"
echo "===    (this is the fault the gate exists to catch; no symlink is involved)"
c2="$(make_case broken-bundle broken bundle)"
o2="$(run_case "${c2}" "")"
indent_lines "${o2}"
assert_status "broken install hard-fails with the environment class" "${o2}" 3
assert_contains "names the failure for what it is" "${o2}" "Godot cannot host .NET"
assert_contains "quotes the engine's own complaint" "${o2}" \
  "Unable to find the .NET assemblies directory."
assert_contains "blames the bundle, not a link" "${o2}" \
  "no symlink can be the cause"
assert_absent "does not prescribe relinking for a directly-invoked binary" "${o2}" \
  "Re-run this script to relink"

# ---------------------------------------------------------------------------
echo
echo "=== 3. NEGATIVE CONTROL: the old check's blind spot - version probe on the same install"
version_out="$("${c2}/home/Applications/Godot_mono.app/Contents/MacOS/Godot" --headless --version 2>&1)"
version_status=$?
indent_lines "${version_out}"
printf '      exit %s\n' "${version_status}"
if [[ "${version_status}" -eq 0 && "${version_out}" == 4.7.1* ]]; then
  pass "\`--headless --version\` prints a matching version and exits 0 on the install control 2 rejected"
else
  fail "the replaced check no longer behaves as recorded: '${version_out}' exit ${version_status}"
fi

# ---------------------------------------------------------------------------
echo
echo "=== 4. NEGATIVE CONTROL: zero exit, complaint in the log - the log arm alone"
c4="$(make_case log-arm healthy stub-complains)"
# The stub is the SYMLINK, so point MECHAMINER_GODOT at it to make it the subject.
o4="$(run_case "${c4}" "${c4}/home/.local/bin/godot")"
indent_lines "${o4}"
assert_status "a complaint with a zero exit still fails" "${o4}" 3
assert_contains "the recorded exit really was 0" "${o4}" "exit     0"

# ---------------------------------------------------------------------------
echo
echo "=== 5. NEGATIVE CONTROL: ~/.local/bin/godot points at a SECOND, broken install"
echo "===    (the bundle is healthy; this is a bad link TARGET, not a claim about"
echo "===     invocation paths - see control 7)"
c5="$(make_case bad-link healthy broken-other)"
o5="$(run_case "${c5}" "")"
indent_lines "${o5}"
assert_status "provisioning is NOT aborted by a bad PATH alias" "${o5}" 0
assert_contains "the alias problem is reported" "${o5}" \
  "'godot' on PATH does not host .NET"
assert_contains "the degraded flag is raised" "${o5}" "HARNESS_DEGRADED=1"
assert_contains "the working invocation is given" "${o5}" "export MECHAMINER_GODOT="
assert_contains "the report refuses to claim a cause" "${o5}" \
  "What it does NOT establish: why"
assert_contains "re-running is ruled out explicitly" "${o5}" \
  "Re-running this script will NOT fix it"
# Asserting the good sentence is present is not the same as asserting the bad
# advice is gone, and the bad advice is the specific defect here: this script
# creates ${GODOT_SYMLINK}, so telling a developer to re-run it to repair that
# symlink sends them round a loop that reproduces the same state.
assert_absent "does not offer relinking as the cure for a link it created" "${o5}" \
  "relink"

# ---------------------------------------------------------------------------
echo
echo "=== 6. MECHAMINER_GODOT is READ, not just mentioned"
echo "=== 6a. override names a healthy Godot while the installed bundle is broken"
c6="$(make_case override broken none)"
install_godot_at "${c6}/override-install/Godot" healthy
o6a="$(run_case "${c6}" "${c6}/override-install/Godot")"
indent_lines "${o6a}"
assert_status "the override is what gets checked, so this passes" "${o6a}" 0
assert_contains "the displacement is stated, not silent" "${o6a}" \
  "MECHAMINER_GODOT is set"

echo
echo "=== 6b. override names a broken Godot while the installed bundle is healthy"
c6b="$(make_case override-broken healthy none)"
install_godot_at "${c6b}/override-install/Godot" broken
o6b="$(run_case "${c6b}" "${c6b}/override-install/Godot")"
indent_lines "${o6b}"
assert_status "the override is what gets checked, so this fails" "${o6b}" 3
assert_contains "the diagnosis points at the override, not the install" "${o6b}" \
  "override is what fails"

# ---------------------------------------------------------------------------
echo
echo "=== 7. MEASUREMENT, not an assertion about macOS: a HEALTHY Godot reached"
echo "===    through a bare symlink, on THIS host"
c7="$(make_case symlink-mechanism healthy bundle)"
mkdir -p "${c7}/bare"
ln -sfn "${c7}/home/Applications/Godot_mono.app/Contents/MacOS/Godot" "${c7}/bare/godot"
rm -rf "${c7}/root/game/.godot"
mech_out="$("${c7}/bare/godot" --headless --path "${c7}/root/game" --import 2>&1)"
mech_status=$?
printf '      exit %s\n' "${mech_status}"
mech_complaints="$(grep -Ei 'assembl|\.net|godotsharp|hostfxr' <<<"${mech_out}" || true)"
if [[ -n "${mech_complaints}" ]]; then
  indent_lines "${mech_complaints}"
else
  printf '      (no .NET or assembly complaint in the log)\n'
fi
if [[ "${mech_status}" -eq 0 && -z "${mech_complaints}" ]]; then
  pass "on this host a symlink to a healthy Godot imports clean - so no control here reproduces the macOS symlink failure README § macOS describes, and none claims to"
else
  # Not a failure of the code under test. It would mean this host's engine behaves
  # like the one README describes, which is worth knowing and worth saying loudly.
  fail "UNEXPECTED AND INTERESTING: a symlink to a healthy Godot did NOT import clean here (exit ${mech_status}). If this reproduces, README § macOS's invoked-path reading has a measurement behind it for the first time - report it before changing this file."
fi

# ---------------------------------------------------------------------------
echo
if [[ "${skipped}" -gt 0 ]]; then
  echo "verify-bootstrap-macos-hosting: ${skipped} check(s) skipped"
fi
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-bootstrap-macos-hosting: PASS"
  echo "NOTE: these controls executed build/bootstrap-macos.sh's verification functions"
  echo "      on Linux against installs this harness built. They say nothing about the"
  echo "      macOS-specific half of that script, which remains unexecuted."
  exit 0
fi
echo "verify-bootstrap-macos-hosting: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
