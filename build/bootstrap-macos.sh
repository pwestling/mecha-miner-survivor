#!/usr/bin/env bash
#
# Reproducible macOS toolchain bootstrap for a developer machine (arm64 or x86_64).
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#            (Toolchain pinning: "A bootstrap command verifies/downloads approved
#            public tools or prints exact manual installation instructions and
#            checks hashes. It never mutates global developer configuration
#            silently.")
#            docs/technical/00-technical-foundation.md § Platform boundary
#            ("Development host | macOS on Apple Silicon is supported for authoring
#            and local verification") and § Foundational verification gates gate 1
#            (".NET Godot builds run from a clean checkout on the macOS development
#            host").
# Requirements: TR-BLD-001, TR-BLD-002, TR-FND-001, TR-FND-003
#
# ============================================================================
# NOT RUN ON HARDWARE. Read this before trusting any runtime claim below.
# ============================================================================
# This script has never been executed on macOS. It was authored and checked on a
# Linux container, where it cannot run: every install path, every `shasum`
# invocation, and every Gatekeeper step is unexercised. What HAS been checked, and
# is all that has been checked:
#
#   * `bash -n` parses it clean.
#   * `shellcheck` reports no finding.
#   * build/verify-bootstrap-macos.sh asserts, statically and on Linux, that the
#     pins below agree with global.json and that the script keeps the properties
#     this header claims (no EUID gate, no GNU-only flags, hash check present).
#   * Every SHA-256 and SHA-512 constant below was obtained by downloading the
#     actual artifact on Linux and hashing it, and every one is cross-checked
#     against a hash the vendor publishes. Both vendors publish one: Microsoft in
#     releases.json, and Godot in a per-release SHA512-SUMS.txt. Those are
#     measured, not asserted. See build/verify-bootstrap-macos.sh for the
#     provenance table.
#   * The Godot/.NET hosting gate in verify() is exercised by
#     build/verify-bootstrap-macos-hosting.sh, which drives this script's own
#     functions against a real Godot 4.7.1 mono build - the version this script
#     pins - on Linux. With the subject binary's GodotSharp directory unreachable
#     the import run exits 134, prints "Unable to find the .NET assemblies
#     directory." and "ERROR: .NET: Assemblies not found" at gd_mono.cpp:650, and
#     the gate fails with exit 3; on a healthy build with a cold cache and the game
#     assembly not yet built it exits 0 with no complaint line, so the gate does not
#     fail a fresh clone. The engine code path is shared with macOS.
#   * What is macOS-specific and still unexecuted: a Godot_mono.app bundle reached
#     through a ~/.local/bin symlink. Do not read the symlink warning in
#     warn_if_symlink_worse() as evidence about that. On the Linux Godot available
#     here, a HEALTHY binary invoked through a bare symlink imports cleanly and
#     exits 0 - measured - so this container cannot produce the macOS failure
#     README § macOS describes, and the harness reaches that warning only by
#     pointing the link at a second, genuinely broken install. That exercises the
#     branch; it does not corroborate the mechanism.
#
# The first developer to run this on a Mac is its first execution. Treat an
# unexpected failure as a defect in this script, not in their machine.
# ============================================================================
#
# It is idempotent, and both halves revalidate rather than trusting that a path
# exists. The two halves do it differently, and only one of them self-heals:
#
#   * Godot: the installed executable is re-hashed against its pin on every run.
#     A corrupted, truncated or wrong-version app therefore does not survive a
#     re-run - it is detected and replaced. This is genuine self-healing, not
#     skip-if-present.
#   * .NET: the pinned SDK counts as present only when ${DOTNET_INSTALL_DIR}/dotnet
#     is executable AND reports that exact version in --list-sdks. It does NOT
#     re-hash the installed tree, so it is weaker than the Godot half: it proves
#     the SDK is complete enough to run, not that every file is the pinned bytes.
#     Bare directory existence was the earlier check and was not enough - a tar
#     that created sdk/<version>/ and then died left every later run skipping and
#     calling a half-extracted SDK present.
#
# ---------------------------------------------------------------------------
# Install locations, and why they are not free choice
# ---------------------------------------------------------------------------
# .NET goes to /usr/local/share/dotnet. This is the exact macOS analogue of the
# /usr/share/dotnet that build/bootstrap-linux.sh uses, and for the same reason:
# it is hostfxr's hardcoded default probe path, so Godot's GodotPlugins host
# resolves the runtime from there with no DOTNET_ROOT set and no `dotnet` on PATH.
# A custom directory plus DOTNET_ROOT is NOT equivalent - DOTNET_ROOT is read by
# the muxer and by apphost startup, but the headless game launch this repository
# gates on goes through GodotPlugins, which is why doc 100's Linux pin is a
# directory and not an environment variable.
#
# The path is established, not guessed:
#   * dotnet/runtime, src/native/corehost/hostmisc/pal.unix.cpp,
#     pal::get_default_installation_dir_for_arch() assigns
#     "/usr/local/share/dotnet" under __APPLE__ and "/usr/share/dotnet"
#     otherwise. That is the fallback probed when neither DOTNET_ROOT nor
#     /etc/dotnet/install_location is present.
#   * learn.microsoft.com/dotnet/core/install/macos § Arm-based Macs § Path
#     differences: "all Arm64 versions of .NET are installed to the normal
#     /usr/local/share/dotnet/ folder."
#   * dotnet/designs accepted/2021/install-location-per-architecture.md: "a
#     default install location (C:\Program Files\dotnet on Windows, /usr/share on
#     Linux, /usr/local/share on macOS)".
# This script installs only the SDK matching the host architecture, so
# /usr/local/share/dotnet is correct on both Apple silicon and Intel. The
# /usr/local/share/dotnet/x64/dotnet sub-path exists only for an x64 SDK
# installed ALONGSIDE an arm64 one, which this script never does.
#
# ---------------------------------------------------------------------------
# Root, and why this script does not demand it
# ---------------------------------------------------------------------------
# build/bootstrap-linux.sh hard-fails unless EUID is 0. That is right for a CI
# container and wrong for a Mac: a developer machine is normally not root, and a
# provisioning script that refuses to start is a provisioning script nobody runs.
# So this script runs unprivileged and escalates NARROWLY:
#
#   * Godot goes to ~/Applications and ~/.local/bin. Both are user-owned. No
#     privilege is needed, ever, for the whole Godot half.
#   * /usr/local/share/dotnet is under /usr/local, which on a stock macOS is
#     root:wheel. There is no user-writable directory that hostfxr probes by
#     default, so this cannot be avoided without giving up the probe path. The
#     script therefore uses `sudo` for exactly two operations - `mkdir -p` and
#     `chown` of the install directory - and then does the extraction itself as
#     the invoking user. Once the directory is user-owned, every later run needs
#     no privilege at all, so the sudo prompt is a one-time cost.
#   * If /usr/local/share/dotnet is already writable (common on Intel Macs where
#     Homebrew has chowned /usr/local), no sudo is invoked at all.
#   * It never edits a shell profile, a global NuGet config, or anything else
#     outside the two install roots and the ~/.local/bin symlink. Doc 100: a
#     bootstrap "never mutates global developer configuration silently."
#
# ---------------------------------------------------------------------------
# Differences from the Linux script that are macOS facts, not preferences
# ---------------------------------------------------------------------------
#   * There is no Vulkan ICD step. Godot on macOS renders through Metal via the
#     MoltenVK layer bundled inside Godot_mono.app; mesa-vulkan-drivers has no
#     macOS analogue and nothing needs installing for the Mobile renderer.
#   * `sha256sum`/`sha512sum` do not exist on macOS. `shasum -a 256|512` does.
#   * `mktemp --suffix=` is a GNU extension and fails on macOS. `mktemp -d` is
#     portable, so archives are named inside a temp directory instead.
#   * Godot ships ONE universal macOS archive. Godot_mono.app/Contents/MacOS/Godot
#     is a fat Mach-O with both an x86_64 and an arm64 slice, so arm64 and Intel
#     hosts download the same bytes and match the same hash. Only the .NET SDK
#     tarball differs by architecture.
#   * A downloaded archive carries com.apple.quarantine. Gatekeeper will refuse
#     to launch the extracted app until that attribute is cleared, and the
#     failure mode is a dialog rather than a useful exit code, so the script
#     clears it explicitly.
#   * The .NET SDK is installed from the pinned tarball with its SHA-512 checked,
#     not through dotnet-install.sh. bootstrap-linux.sh pipes dotnet-install.sh
#     and so verifies NO hash for the SDK it installs, which doc 100's "checks
#     hashes" requires. Extracting a pinned tarball is the same amount of work
#     and is actually verifiable.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 3 missing or mismatched pinned environment, 8 unexpected tool-internal failure.

set -euo pipefail

# Declared and assigned separately: `readonly X="$(...)"` masks the subshell's
# exit status (shellcheck SC2155). build/verify-godot.sh line 32 uses the masking
# form; this is the same idiom written so `shellcheck` is silent.
REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT

# --- pinned versions -------------------------------------------------------
# DOTNET_SDK_VERSION duplicates global.json on purpose so the download URL and
# the hash constants sit next to the version they belong to. assert_sdk_pin()
# below fails if the duplicate ever drifts from global.json, so the duplication
# cannot rot silently. build/verify-bootstrap-macos.sh asserts the same thing
# without running the script.
readonly DOTNET_SDK_VERSION="10.0.302"
readonly DOTNET_INSTALL_DIR="/usr/local/share/dotnet"

readonly GODOT_VERSION="4.7.1"
readonly GODOT_EXPECTED_VERSION_PREFIX="4.7.1.stable.mono.official"

# --- pinned macOS artifacts ------------------------------------------------
# Provenance of every constant in this block:
#   Godot archive sha256/size: downloaded from the URL below and hashed. The same
#     method applied to the linux_x86_64 archive reproduces the archive_sha256
#     and archive_size_bytes already recorded for linux-x64 on the FND-002 branch
#     chain, byte for byte, which is what makes the method trustworthy here.
#   Godot archive, vendor cross-check: Godot publishes a per-release checksum file,
#     https://github.com/godotengine/godot-builds/releases/download/4.7.1-stable/SHA512-SUMS.txt
#     which lists for Godot_v4.7.1-stable_mono_macos.universal.zip
#       7708863cb3ed22000cda423a3b067c7b882f1434c7854242e2fab6cead45ae321b5004075adc76c32a73411ccc96b4fa655158d72cbbbb5ba58651c2a7c3763e
#     Fetching that file and sha512-ing the downloaded archive reproduces it
#     exactly, so the archive pin below is vendor-anchored and not merely
#     self-consistent. The SHA-256 pinned here is of those same cross-checked bytes.
#     (The archive is pinned by sha256 rather than the vendor's sha512 only
#     because GODOT_EXECUTABLE_SHA256 and the linux-x64 records are sha256; the
#     vendor sha512 is the cross-check, not a second pin.)
#   Godot executable sha256: extracted from that archive and hashed. The extracted
#     Contents/MacOS/Godot is a 2-slice universal Mach-O (x86_64 + arm64), which is
#     why one archive and one hash serve both host architectures.
#   .NET tarball sha512/size: taken from Microsoft's own release metadata at
#     https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
#     (release 10.0.10, 2026-07-14) AND independently reproduced by downloading
#     each tarball and running sha512sum. Published and measured agree. The two
#     size constants below are the same tarballs' byte counts, which Microsoft's
#     own Content-Length for each download URL also reports.
# Retrieved 2026-08-06 UTC.
readonly GODOT_ARCHIVE_NAME="Godot_v${GODOT_VERSION}-stable_mono_macos.universal.zip"
readonly GODOT_ARCHIVE_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/${GODOT_ARCHIVE_NAME}"
readonly GODOT_ARCHIVE_SHA256="92cac516baa8ddc7756eeaa38a6d007778a968bfbf188db7c5d6e6ec21c5d52c"
readonly GODOT_ARCHIVE_SIZE_BYTES="197041155"
readonly GODOT_EXECUTABLE_SHA256="d11dc4a241ec29a347e13c8c7706e49433379ae1f9fc6a6e6819efb3891fce97"

readonly DOTNET_SHA512_ARM64="b2286dec9177e8b5543ff2fe95c84db358b87ec2a36a0d34a29033d70279940fd1134af56c4299648f8950db2d6ce35237698cf2818d9abc670c2c1664c92ac0"
readonly DOTNET_SHA512_X64="48d5861dc0d6c9c782c6d163d6b334ecac2ebd65a1ae59e9ce5b93dd080a31d7ecfc4e4d47e0e35b201ce63661218d641e154022266294a3a8b84593a019cfbc"

# The .NET half is size-checked as well as hash-checked, for symmetry with the
# Godot half. A size check is redundant against a matching sha512 and is kept
# anyway because it fails EARLIER and more legibly: a truncated download or an
# HTML error page served with a 200 reports a byte count a human can recognise,
# instead of a hash mismatch that reads identically to a tampered artifact.
readonly DOTNET_SIZE_BYTES_ARM64="226536510"
readonly DOTNET_SIZE_BYTES_X64="234313427"

# --- user-scoped install locations ----------------------------------------
readonly GODOT_APP_DIR="${HOME}/Applications"
readonly GODOT_APP="${GODOT_APP_DIR}/Godot_mono.app"
readonly GODOT_BIN="${GODOT_APP}/Contents/MacOS/Godot"
readonly USER_BIN_DIR="${HOME}/.local/bin"
readonly GODOT_SYMLINK="${USER_BIN_DIR}/godot"

# The repository's own Godot project. verify() imports it, because an import is
# the cheapest command that actually initialises .NET - see the comment on
# assert_godot_hosts_dotnet(). build/verify-godot.sh names the same directory.
readonly GODOT_PROJECT_DIR="${REPO_ROOT}/game"

readonly EXIT_ENVIRONMENT=3
readonly EXIT_INTERNAL=8

WORK_DIR=""

log() { printf '[bootstrap-macos] %s\n' "$*"; }
warn() { printf '[bootstrap-macos] WARNING: %s\n' "$*" >&2; }
fail() { printf '[bootstrap-macos] FAILED: %s\n' "$1" >&2; exit "${2:-$EXIT_INTERNAL}"; }

cleanup() {
  if [[ -n "${WORK_DIR}" && -d "${WORK_DIR}" ]]; then
    rm -rf "${WORK_DIR}"
  fi
}
trap cleanup EXIT

# ---------------------------------------------------------------------------
# Preconditions
# ---------------------------------------------------------------------------

require_macos() {
  local kernel
  kernel="$(uname -s)"
  if [[ "${kernel}" != "Darwin" ]]; then
    fail "this is the macOS bootstrap and the host is ${kernel}; on Linux run build/bootstrap-linux.sh" \
      "$EXIT_ENVIRONMENT"
  fi

  # `set -u` catches an UNSET variable; it does not catch a set-but-EMPTY one. With
  # HOME="" every ${HOME}-derived path in this script collapses to an absolute
  # system path: GODOT_APP becomes /Applications/Godot_mono.app, so the "replace a
  # Godot that does not match the pin" branch would rm -rf the SYSTEM-WIDE install
  # rather than this user's copy, and the symlink would target /.local/bin. Those
  # constants are assigned at file scope, before any function runs, so this check
  # cannot repair them - it exists to stop the script before anything is deleted.
  if [[ -z "${HOME:-}" ]]; then
    fail "HOME is empty or unset. Every path this script installs to is derived from
it, and with an empty HOME the Godot replacement step would target the system-wide
/Applications/Godot_mono.app instead of your own. Nothing was written. Set HOME to
your home directory and re-run." "$EXIT_ENVIRONMENT"
  fi
}

# Deliberately NOT require_root. See the header. Refusing to run as root as well
# would be gratuitous, but running as root would leave the extracted Godot app
# owned by root inside a user's home directory, so it is worth a warning.
warn_if_root() {
  if [[ "${EUID}" -eq 0 ]]; then
    warn "running as root. ${GODOT_APP} and ${USER_BIN_DIR} will end up root-owned"
    warn "inside \$HOME. Re-run as your normal user; this script escalates only where it must."
  fi
}

# The pinned SDK version is duplicated from global.json so it can sit beside its
# download URL and hashes. This is the check that keeps the duplicate honest.
assert_sdk_pin() {
  local global_json="${REPO_ROOT}/global.json"
  [[ -f "${global_json}" ]] || fail "missing ${global_json}" "$EXIT_ENVIRONMENT"

  local declared
  # No jq on a stock macOS. This is a fixed-shape file the repository owns.
  declared="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "${global_json}" | head -1)"
  if [[ "${declared}" != "${DOTNET_SDK_VERSION}" ]]; then
    fail "global.json pins SDK '${declared}' but this script pins '${DOTNET_SDK_VERSION}'. \
Update DOTNET_SDK_VERSION and the DOTNET_SHA512_* constants together - a version bump \
without new hashes would install one SDK and verify another." "$EXIT_ENVIRONMENT"
  fi
  log "pinned .NET SDK ${DOTNET_SDK_VERSION} agrees with global.json"
}

require_commands() {
  local missing=()
  local tool
  for tool in curl unzip shasum tar uname; do
    command -v "${tool}" >/dev/null 2>&1 || missing+=("${tool}")
  done
  if [[ "${#missing[@]}" -gt 0 ]]; then
    fail "missing required command(s): ${missing[*]}. All ship with macOS and the Command Line Tools; run 'xcode-select --install'" \
      "$EXIT_ENVIRONMENT"
  fi
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# Prefixes each line of a captured block so quoted engine output is legible inside
# a failure message. A loop, not `sed 's/^/    /'`, so shellcheck stays silent on a
# variable substitution; build/verify-bootstrap-macos.sh carries the same helper
# for the same reason.
indent_block() {
  local line
  while IFS= read -r line; do
    printf '    %s\n' "${line}"
  done <<<"$1"
}

# macOS has shasum, not sha256sum/sha512sum.
sha_of_file() {
  local algorithm="$1" path="$2"
  shasum -a "${algorithm}" "${path}" | awk '{print $1}'
}

verify_digest() {
  local label="$1" path="$2" algorithm="$3" expected="$4"
  local measured
  measured="$(sha_of_file "${algorithm}" "${path}")"
  if [[ "${measured}" != "${expected}" ]]; then
    fail "${label} sha${algorithm} mismatch.
  expected ${expected}
  measured ${measured}
A pinned artifact that does not match its hash is not installed. Nothing was written." \
      "$EXIT_ENVIRONMENT"
  fi
  log "${label} sha${algorithm} matches the pin"
}

verify_size() {
  local label="$1" path="$2" expected="$3"
  local measured
  # -f%z is the BSD/macOS stat spelling; GNU's is -c%s. This script only ever
  # runs on macOS, so BSD is the correct one and not a portability slip.
  measured="$(stat -f%z "${path}")"
  if [[ "${measured}" != "${expected}" ]]; then
    fail "${label} is ${measured} bytes, expected ${expected}" "$EXIT_ENVIRONMENT"
  fi
}

download() {
  local url="$1" destination="$2"
  log "downloading ${url}"
  curl -fsSL --retry 3 --retry-delay 2 -o "${destination}" "${url}" \
    || fail "could not download ${url}" "$EXIT_ENVIRONMENT"
}

dotnet_rid() {
  local machine
  machine="$(uname -m)"
  case "${machine}" in
    arm64) printf 'osx-arm64' ;;
    x86_64) printf 'osx-x64' ;;
    *) fail "unsupported macOS architecture '${machine}'; this script pins osx-arm64 and osx-x64 only" \
         "$EXIT_ENVIRONMENT" ;;
  esac
}

dotnet_expected_sha512() {
  case "$(dotnet_rid)" in
    osx-arm64) printf '%s' "${DOTNET_SHA512_ARM64}" ;;
    *) printf '%s' "${DOTNET_SHA512_X64}" ;;
  esac
}

dotnet_expected_size() {
  case "$(dotnet_rid)" in
    osx-arm64) printf '%s' "${DOTNET_SIZE_BYTES_ARM64}" ;;
    *) printf '%s' "${DOTNET_SIZE_BYTES_X64}" ;;
  esac
}

# Escalates for the two operations that genuinely need it, and only when the
# target is not already writable. Printing the command first is the doc 100
# "never mutates global developer configuration silently" requirement.
# `-w` on the install root says nothing about what is INSIDE it, and the `sudo
# chown` below is deliberately not recursive. Microsoft's .pkg leaves
# /usr/local/share/dotnet root-owned all the way down, so after chowning the top
# level the root is writable while every subdirectory a new SDK must write into is
# still not. Extracting into that state does not fail cleanly: tar creates the
# top-level subtrees it CAN, writes a large fraction of the new SDK, and only then
# fails on the ones it cannot - leaving a pre-existing install half-updated, with
# the ownership of a directory the developer never created permanently changed.
# Worse, a re-run cannot repair it: the root is writable by then, so the escalation
# branch is skipped and the identical partial extraction happens again. Refuse
# instead, before tar can run, and name the two ways out.
#
# A bash loop, NOT `find -writable`: that predicate is a GNU extension and macOS
# find does not have it, so the check would break on the only platform that runs
# this script. build/verify-bootstrap-macos.sh check 12 does not know that
# spelling either, so the breakage would ship green.
assert_install_dir_children_writable() {
  local directory="$1"
  local child
  # An unmatched glob stays literal, and `[[ -d ]]` rejects it, so an empty install
  # directory needs no nullglob and is correctly treated as fine.
  for child in "${directory}"/*; do
    [[ -d "${child}" ]] || continue
    [[ -w "${child}" ]] && continue
    fail "${directory} is writable but '${child}' inside it is not.
This is what a .NET SDK installed by Microsoft's .pkg looks like: root-owned
underneath a directory this script can chown only at the top level. Extracting the
pinned SDK over it would write part of the new SDK and then fail partway, leaving
the existing install half-updated and unrepairable by re-running.
Nothing was written. Choose one, then re-run:
    sudo chown -R '$(id -un)' '${directory}'
  to take ownership of the existing install and keep it, or
    sudo rm -rf '${directory}'
  to discard it and let this script lay down a clean SDK." "$EXIT_ENVIRONMENT"
  done
}

ensure_install_dir_writable() {
  local directory="$1"
  if [[ -d "${directory}" && -w "${directory}" ]]; then
    log "${directory} already exists and is writable; no privilege needed"
    assert_install_dir_children_writable "${directory}"
    return
  fi

  if ! command -v sudo >/dev/null 2>&1; then
    fail "${directory} is not writable and sudo is unavailable. Create it manually:
    sudo mkdir -p '${directory}' && sudo chown '$(id -un)' '${directory}'
then re-run this script." "$EXIT_ENVIRONMENT"
  fi

  log "${directory} is not writable. Escalating for exactly two operations:"
  log "    sudo mkdir -p ${directory}"
  log "    sudo chown $(id -un) ${directory}"
  log "Everything after this runs as $(id -un). Later runs need no privilege."
  sudo mkdir -p "${directory}" || fail "sudo mkdir -p ${directory} failed" "$EXIT_ENVIRONMENT"
  sudo chown "$(id -un)" "${directory}" || fail "sudo chown ${directory} failed" "$EXIT_ENVIRONMENT"
  [[ -w "${directory}" ]] || fail "${directory} still not writable after chown" "$EXIT_ENVIRONMENT"
  # The chown above is not recursive, so this is exactly the path on which a
  # pre-existing root-owned install surfaces. Same refusal as the already-writable
  # branch.
  assert_install_dir_children_writable "${directory}"
}

# ---------------------------------------------------------------------------
# .NET SDK
# ---------------------------------------------------------------------------

# Whether the pinned SDK is ALREADY INSTALLED AND USABLE. The earlier version of
# this test was `[[ -d "${DOTNET_INSTALL_DIR}/sdk/${DOTNET_SDK_VERSION}" ]]`, which
# revalidated nothing: a tar that created sdk/<version>/ and then died - exactly
# what a half-privileged extraction does - left that directory behind, so every
# later run skipped the install and reported the SDK present. Asking the muxer to
# enumerate its SDKs is a test a half-extracted tree fails.
dotnet_sdk_present() {
  local dotnet_bin="${DOTNET_INSTALL_DIR}/dotnet"
  [[ -x "${dotnet_bin}" ]] || return 1
  [[ -d "${DOTNET_INSTALL_DIR}/sdk/${DOTNET_SDK_VERSION}" ]] || return 1

  local sdks
  # A broken install makes this exit non-zero rather than printing; that is a
  # "not present" answer, not an error to abort on, hence the guard.
  sdks="$("${dotnet_bin}" --list-sdks 2>/dev/null)" || return 1
  # A here-string, not `printf | grep -q`: see the note in verify() for why piping
  # into grep -q under pipefail can report a found line as missing.
  grep -q "^${DOTNET_SDK_VERSION} " <<<"${sdks}"
}

install_dotnet_sdk() {
  if dotnet_sdk_present; then
    log ".NET SDK ${DOTNET_SDK_VERSION} already present in ${DOTNET_INSTALL_DIR} and reports itself runnable"
    return
  fi

  local rid tarball url
  rid="$(dotnet_rid)"
  url="https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_SDK_VERSION}/dotnet-sdk-${DOTNET_SDK_VERSION}-${rid}.tar.gz"
  tarball="${WORK_DIR}/dotnet-sdk-${DOTNET_SDK_VERSION}-${rid}.tar.gz"

  log "installing .NET SDK ${DOTNET_SDK_VERSION} (${rid}) into ${DOTNET_INSTALL_DIR}"
  download "${url}" "${tarball}"
  verify_size ".NET SDK ${rid} tarball" "${tarball}" "$(dotnet_expected_size)"
  verify_digest ".NET SDK ${rid} tarball" "${tarball}" 512 "$(dotnet_expected_sha512)"

  ensure_install_dir_writable "${DOTNET_INSTALL_DIR}"

  # The SDK tarball unpacks as the CONTENTS of the install root - dotnet, sdk/,
  # shared/, host/ at the top level - so it extracts directly into the install
  # directory with no strip-components. Extracting over an existing older SDK is
  # the supported side-by-side shape: sdk/<version>/ directories coexist.
  # Explicitly EXIT_ENVIRONMENT. With no exit class this defaulted to
  # EXIT_INTERNAL (8), reporting "the machine's install directory is not writable
  # by me" as an unexpected failure inside this tool, which sends the developer
  # looking in the wrong place.
  tar -xzf "${tarball}" -C "${DOTNET_INSTALL_DIR}" \
    || fail "could not extract the .NET SDK tarball into ${DOTNET_INSTALL_DIR}" "$EXIT_ENVIRONMENT"

  [[ -x "${DOTNET_INSTALL_DIR}/dotnet" ]] \
    || fail "expected ${DOTNET_INSTALL_DIR}/dotnet after extraction" "$EXIT_ENVIRONMENT"
  [[ -d "${DOTNET_INSTALL_DIR}/sdk/${DOTNET_SDK_VERSION}" ]] \
    || fail "extraction did not produce sdk/${DOTNET_SDK_VERSION}" "$EXIT_ENVIRONMENT"
}

# ---------------------------------------------------------------------------
# Godot
# ---------------------------------------------------------------------------

install_godot() {
  if [[ -x "${GODOT_BIN}" ]] \
    && [[ "$(sha_of_file 256 "${GODOT_BIN}")" == "${GODOT_EXECUTABLE_SHA256}" ]]; then
    log "Godot ${GODOT_VERSION} already present at ${GODOT_APP} and matches its pin"
    link_godot
    return
  fi

  local archive="${WORK_DIR}/${GODOT_ARCHIVE_NAME}"
  log "installing Godot ${GODOT_VERSION} .NET (universal) into ${GODOT_APP_DIR}"
  download "${GODOT_ARCHIVE_URL}" "${archive}"
  verify_size "Godot archive" "${archive}" "${GODOT_ARCHIVE_SIZE_BYTES}"
  verify_digest "Godot archive" "${archive}" 256 "${GODOT_ARCHIVE_SHA256}"

  # Deliberately AFTER the download and both verifications, never before. This
  # block used to sit above them, so a dropped connection, a hash mismatch or a
  # full disk during a 197 MB download destroyed the developer's working Godot and
  # installed nothing in its place. By here the replacement bytes are on disk and
  # have matched their pin, so the old app is only removed once there is a
  # hash-matched replacement in hand.
  if [[ -e "${GODOT_APP}" ]]; then
    log "replacing ${GODOT_APP}: present but not matching the pinned executable hash"
    rm -rf "${GODOT_APP}"
  fi

  mkdir -p "${GODOT_APP_DIR}"
  # The archive contains exactly one top-level entry, Godot_mono.app/, and no
  # symlink entries, so plain unzip reproduces the bundle faithfully. `ditto`
  # would also work and is more idiomatic on macOS, but unzip is what
  # bootstrap-linux.sh uses and keeping one extraction tool across both scripts
  # is worth more than idiom here.
  unzip -q -o "${archive}" -d "${GODOT_APP_DIR}" \
    || fail "could not unzip the Godot archive into ${GODOT_APP_DIR}"

  [[ -x "${GODOT_BIN}" ]] || fail "expected Godot binary missing at ${GODOT_BIN}" "$EXIT_ENVIRONMENT"
  verify_digest "Godot executable" "${GODOT_BIN}" 256 "${GODOT_EXECUTABLE_SHA256}"

  clear_quarantine
  link_godot
}

# A file downloaded by curl gets com.apple.quarantine, and Gatekeeper then blocks
# the extracted app with a GUI dialog rather than a diagnosable exit code. The
# official Godot macOS builds are signed and notarized, so clearing the attribute
# is not weakening a signature check - the signature is still enforced.
clear_quarantine() {
  if ! command -v xattr >/dev/null 2>&1; then
    warn "xattr not found; if macOS refuses to launch Godot, run: xattr -dr com.apple.quarantine '${GODOT_APP}'"
    return
  fi
  log "clearing com.apple.quarantine on ${GODOT_APP}"
  xattr -dr com.apple.quarantine "${GODOT_APP}" 2>/dev/null || true
}

link_godot() {
  mkdir -p "${USER_BIN_DIR}"

  # `ln -sfn` on its own replaces whatever occupies this path - including a wrapper
  # script a developer wrote by hand to pass extra flags, or a symlink to a
  # different engine build they are deliberately testing against. Doc 100's "never
  # mutates global developer configuration silently" applies: only ever replace a
  # symlink that already points where we are about to point it, and move anything
  # else aside, loudly, instead of deleting it.
  #
  # `readlink` with no -f: -f is a GNU extension that stock macOS readlink does not
  # accept. One level is all that is needed to recognise our own link.
  if [[ -L "${GODOT_SYMLINK}" || -e "${GODOT_SYMLINK}" ]]; then
    local current=""
    if [[ -L "${GODOT_SYMLINK}" ]]; then
      current="$(readlink "${GODOT_SYMLINK}")"
    fi
    if [[ "${current}" != "${GODOT_BIN}" ]]; then
      local moved
      moved="${GODOT_SYMLINK}.replaced-by-bootstrap.$(date -u +%Y%m%dT%H%M%SZ)"
      if [[ -L "${GODOT_SYMLINK}" ]]; then
        warn "${GODOT_SYMLINK} is a symlink to '${current}', not to the Godot this"
        warn "script installed."
      else
        warn "${GODOT_SYMLINK} already exists and is not a symlink - it may be a"
        warn "wrapper script you wrote."
      fi
      warn "Moving it to ${moved} rather than overwriting it. Delete it yourself if"
      warn "you do not want it back."
      mv "${GODOT_SYMLINK}" "${moved}" \
        || fail "could not move the existing ${GODOT_SYMLINK} aside" "$EXIT_ENVIRONMENT"
    fi
  fi

  ln -sfn "${GODOT_BIN}" "${GODOT_SYMLINK}"
  log "linked ${GODOT_SYMLINK} -> ${GODOT_BIN}"
}

# ---------------------------------------------------------------------------
# Verification
# ---------------------------------------------------------------------------

# OVERLAP, NAMED RATHER THAN LEFT TO BE FOUND: build/verify-godot.sh:71-83 already
# runs this same import against bare `godot` and, on top of the exit status, fails
# on any line matching (ERROR|WARNING|SCRIPT ERROR|USER ERROR):. That is the
# stronger assertion of the two and it stays the project's import gate. This one is
# deliberately narrower for one reason: verify-godot.sh:63 builds
# game/MechaMiner.Game.csproj first, so a clean log is a reasonable demand there.
# Bootstrap runs before any build - on a fresh clone the game assembly does not
# exist - and an unbuilt project imports with engine WARNING lines that say nothing
# about whether Godot can host .NET. So this narrows the log arm to the .NET
# assembly-resolution shape below. If the two ever need to agree, verify-godot.sh
# is the one to move toward, not this one.

# What a .NET assembly-resolution complaint LOOKS LIKE, matched case-insensitively
# and by shape rather than by one literal. Both word orders are real and both come
# out of the same failure: Godot prints "Unable to find the .NET assemblies
# directory." and then aborts with "ERROR: .NET: Assemblies not found", so the verb
# leads in one line and trails in the other. hostfxr and GodotSharp are named too
# because the same class of breakage surfaces through either. None of these strings
# is a stable interface, which is exactly why this matches a shape: pinning one
# literal would let the next wording through silently.
readonly GODOT_DOTNET_COMPLAINT_RE='(unable to find|could not find|cannot find|not found|missing|failed to load).*(\.net|assembl|godotsharp|hostfxr)|(\.net|assembl|godotsharp|hostfxr).*(unable to find|could not find|cannot find|not found|missing|failed to load)'

# Written by godot_import_probe(), read by its callers. Two values are needed out
# of one run and this script targets the bash 3.2 that ships with macOS, which has
# no namerefs to return them with.
GODOT_IMPORT_LOG=""
GODOT_IMPORT_STATUS=0

# Which executable the authoritative check adjudicates, and whether the ~/.local/bin
# symlink was found to be worse than it. Set by verify(), read by main() so the
# closing line cannot claim a clean run when it was not one.
GODOT_UNDER_TEST=""
GODOT_SYMLINK_DEGRADED=0

# MECHAMINER_GODOT is the repository's documented way to name the Godot executable
# (README § macOS). This script printed that name in two pieces of advice and never
# read the variable, so the escape hatch it offered a developer did not exist. It
# does now, and it wins over both the bundle binary and the symlink: a developer who
# has set it has told us which Godot they intend to use, and checking a different
# one answers a question they did not ask.
#
# Otherwise the subject is ${GODOT_BIN}, the binary inside the bundle this script
# installs and hashes - NOT ${GODOT_SYMLINK}. Everything downstream of provisioning
# is entitled to assume the pinned toolchain works when invoked directly; a
# convenience alias on PATH is a separate question, handled separately below.
godot_under_test() {
  if [[ -n "${MECHAMINER_GODOT:-}" ]]; then
    printf '%s\n' "${MECHAMINER_GODOT}"
  else
    printf '%s\n' "${GODOT_BIN}"
  fi
}

# Runs the import once through the executable it is given and reports whether that
# run was clean. Both signals are consulted, because neither alone is enough: the
# exit status alone misses an engine that complains and carries on, and the log
# match alone misses a failure that aborts without printing anything this pattern
# recognises.
#
# Output is captured, so Godot is not writing to a terminal and the lines this
# matches carry no colour escapes. The import populates ${GODOT_PROJECT_DIR}/.godot,
# which is gitignored and is the import cache the developer needs before their first
# launch anyway.
godot_import_probe() {
  local godot_executable="$1"
  GODOT_IMPORT_LOG=""
  GODOT_IMPORT_STATUS=0
  # `|| GODOT_IMPORT_STATUS=$?` rather than a bare assignment: under `set -e` a
  # failing command substitution in a plain assignment kills the script, and this
  # command is EXPECTED to fail on a broken install. That is the finding, not an
  # accident to abort on.
  GODOT_IMPORT_LOG="$("${godot_executable}" --headless --path "${GODOT_PROJECT_DIR}" --import 2>&1)" \
    || GODOT_IMPORT_STATUS=$?
  [[ "${GODOT_IMPORT_STATUS}" -eq 0 ]] || return 1
  # A here-string, not `printf | grep -q`: same SIGPIPE race the note in verify()
  # describes. Negated so a clean log is this function's success.
  ! grep -Eiq "${GODOT_DOTNET_COMPLAINT_RE}" <<<"${GODOT_IMPORT_LOG}"
}

# Failure path only, and it must never call fail(): a diagnostic that can itself
# abort would replace the real failure message with its own.
#
# The subject here was invoked by its own path with no symlink in the way, so a
# symlink cannot be the cause and this must not offer relinking as a cure. The one
# split still worth making is override-versus-install, and only when a
# MECHAMINER_GODOT override is what failed.
diagnose_godot_import() {
  local subject="$1"

  if [[ ! -x "${subject}" ]]; then
    printf '%s\n' \
      "  ${subject} is missing or not executable, so there was nothing here that" \
      "  could have hosted .NET."
    if [[ "${subject}" != "${GODOT_BIN}" ]]; then
      printf '%s\n' \
        "  It came from MECHAMINER_GODOT. Unset that to check ${GODOT_BIN}," \
        "  the Godot this script installs, instead."
    fi
    return 0
  fi

  if [[ "${subject}" != "${GODOT_BIN}" ]]; then
    # A MECHAMINER_GODOT override failed. Whether the override or the install is at
    # fault is answerable by running the same command against the install, so
    # answer it rather than making the developer guess.
    if [[ -x "${GODOT_BIN}" ]] && godot_import_probe "${GODOT_BIN}"; then
      printf '%s\n' \
        "  MECHAMINER_GODOT pointed this check at ${subject}." \
        "  The SAME command run against ${GODOT_BIN}," \
        "  the Godot this script installs, succeeds. The install is fine and the" \
        "  override is what fails: unset MECHAMINER_GODOT, or point it at" \
        "  ${GODOT_BIN}."
    else
      printf '%s\n' \
        "  MECHAMINER_GODOT pointed this check at ${subject}." \
        "  ${GODOT_BIN} is absent or fails the same way, so unsetting the override" \
        "  is not the fix. Remove ${GODOT_APP} and re-run this script to lay the" \
        "  pinned Godot down again from its archive."
    fi
    return 0
  fi

  printf '%s\n' \
    "  This is the binary inside the app bundle this script installed, invoked by" \
    "  its own path with no symlink involved, so no symlink can be the cause and" \
    "  relinking cannot be the cure. Its SHA-256 matched the pin earlier in this" \
    "  run, so the executable is the expected bytes; what is missing is something" \
    "  the bundle needs beside it at runtime - most often the GodotSharp directory" \
    "  that has to sit next to the executable. Remove ${GODOT_APP} and re-run this" \
    "  script to lay it down again from the pinned archive."
  return 0
}

# The symlink check, kept but demoted, and the demotion is the point.
#
# Once the bundle binary has been shown to host .NET, a symlink to it that does NOT
# is a real problem for every developer who types `godot` - build/verify-godot.sh
# calls bare `godot`, so it would fail for them. It is not, however, a reason to
# fail provisioning: both pinned tools are installed, hashed and shown to work by
# the invocation this script documents. Exit class 3 means "missing or mismatched
# pinned environment", and that is not this.
#
# It is also deliberately NOT a judgement about mechanism. README § macOS records
# the invoked-path assembly-resolution story as the coherent reading of a failure,
# not as something measured, and this script cannot measure it either: from here a
# symlink whose target is a second, broken Godot is indistinguishable from a good
# target reached by a bad path. So this reports the difference it observed and the
# invocation that works, and does not claim to know why.
warn_if_symlink_worse() {
  local subject="$1"

  # Nothing to compare when the symlink IS the subject, or when it is absent
  # because link_godot moved a developer's own file aside.
  [[ "${subject}" != "${GODOT_SYMLINK}" ]] || return 0
  if [[ ! -e "${GODOT_SYMLINK}" ]]; then
    warn "${GODOT_SYMLINK} is not present, so 'godot' will not resolve on PATH."
    return 0
  fi

  log "re-running the same import through ${GODOT_SYMLINK} - the PATH alias"
  if godot_import_probe "${GODOT_SYMLINK}"; then
    log "godot hosts .NET through ${GODOT_SYMLINK} too"
    return 0
  fi

  GODOT_SYMLINK_DEGRADED=1

  local complaints
  complaints="$(grep -Ei "${GODOT_DOTNET_COMPLAINT_RE}" <<<"${GODOT_IMPORT_LOG}" || true)"
  if [[ -z "${complaints}" ]]; then
    complaints="$(tail -n 5 <<<"${GODOT_IMPORT_LOG}")"
  fi

  local target="not a symlink"
  if [[ -L "${GODOT_SYMLINK}" ]]; then
    target="$(readlink "${GODOT_SYMLINK}")"
  fi

  warn ""
  warn "Provisioning succeeded, but 'godot' on PATH does not host .NET."
  warn ""
  warn "  works:  ${subject} --headless --path ${GODOT_PROJECT_DIR} --import"
  warn "  fails:  ${GODOT_SYMLINK} --headless --path ${GODOT_PROJECT_DIR} --import"
  warn "          exit ${GODOT_IMPORT_STATUS}"
  indent_block "${complaints}" >&2
  warn ""
  warn "What that establishes: this Godot hosts .NET when invoked as ${subject}"
  warn "and does not when invoked as ${GODOT_SYMLINK}."
  warn "What it does NOT establish: why. Two causes look identical from here - a mono"
  warn "Godot resolving its assemblies relative to the path it was invoked through"
  warn "(README § macOS, recorded there as the coherent reading of a failure rather"
  warn "than as a measurement), and the link simply pointing somewhere else. For that"
  warn "second one: ${GODOT_SYMLINK} -> ${target}"
  warn ""
  warn "Re-running this script will NOT fix it. This script created that symlink and"
  warn "would recreate the identical one pointing at the identical target."
  warn ""
  warn "Use the invocation this run showed working, either directly or by naming it:"
  warn "    export MECHAMINER_GODOT=\"${subject}\""
  warn "    ${subject} --headless --path ${GODOT_PROJECT_DIR} --import"
  warn ""
  warn "Anything that calls bare 'godot' - build/verify-godot.sh does - will fail"
  warn "until this is resolved."
  warn ""
}

# The part of verification that can actually fail on the way a real install breaks.
#
# `--headless --version` cannot. It answers before .NET is initialised, so it
# prints a version and exits 0 on an install whose .NET assemblies are unreachable
# - measured, on a Godot 4.7.1 mono build with its GodotSharp directory made
# unreachable: `--headless --version` printed 4.7.1.stable.mono.official and exited
# 0, while the import run below exited 134 with "ERROR: .NET: Assemblies not found"
# (gd_mono.cpp:650). A gate that passes the exact breakage it exists to catch is
# decoration.
#
# The import is what initialises .NET, so it is the run that adjudicates. On macOS
# some Godot failures present as a modal dialog and the process then sits there
# rather than exiting (README § macOS); this check inherits that, exactly as
# build/verify-godot.sh does, and the .NET failure it targets aborts instead.
#
# WHICH EXECUTABLE ADJUDICATES, and why it is not the symlink. An earlier revision
# of this function probed ${GODOT_SYMLINK}. If README § macOS is right that a mono
# Godot reached through such a link cannot find its assemblies, then that gate could
# never pass on the platform this script exists to provision - a correctly
# provisioned Mac would hard-fail every run, and the advice it printed was to re-run
# the script, which recreates the same link. Nobody here has a Mac, so that reading
# is unmeasured and must not be settled by asserting it. Probing ${GODOT_BIN}
# instead makes this gate answer a question with only one right answer: a pinned
# toolchain that cannot host .NET when invoked directly is broken on any platform,
# and still hard-fails with exit class 3.
assert_godot_hosts_dotnet() {
  local subject="$1"

  [[ -f "${GODOT_PROJECT_DIR}/project.godot" ]] \
    || fail "no Godot project at ${GODOT_PROJECT_DIR}. This check imports the repository's
own project, so it must be run from a checkout, not from a copy of this script alone." \
      "$EXIT_ENVIRONMENT"

  # Announced here rather than in verify(), so that anything which runs this check
  # says which Godot it is about. A gate that silently adjudicates an executable
  # other than the one the script installed is a gate whose result is misread.
  if [[ "${subject}" != "${GODOT_BIN}" ]]; then
    log "MECHAMINER_GODOT is set, so this adjudicates ${subject}"
    log "and NOT ${GODOT_BIN}, which this script installs."
  fi

  log "importing ${GODOT_PROJECT_DIR} through ${subject} - the run that initialises .NET"
  if godot_import_probe "${subject}"; then
    log "godot imported the project with no .NET complaint"
    return
  fi

  local failed_status="${GODOT_IMPORT_STATUS}"
  local complaints
  complaints="$(grep -Ei "${GODOT_DOTNET_COMPLAINT_RE}" <<<"${GODOT_IMPORT_LOG}" || true)"
  local evidence
  if [[ -n "${complaints}" ]]; then
    evidence="$(indent_block "${complaints}")"
  else
    # Non-zero exit with nothing this pattern recognises. The tail is the only
    # honest thing to show, and showing it beats claiming a cause.
    evidence="$(indent_block "$(tail -n 10 <<<"${GODOT_IMPORT_LOG}")")"
  fi
  local diagnosis
  diagnosis="$(diagnose_godot_import "${subject}")"

  fail "Godot cannot host .NET, so this toolchain cannot run the game.
  command  ${subject} --headless --path ${GODOT_PROJECT_DIR} --import
  exit     ${failed_status}
${evidence}
${diagnosis}
The install is left as it is; nothing was removed." "$EXIT_ENVIRONMENT"
}

verify() {
  log "verifying pinned versions"

  local dotnet_bin="${DOTNET_INSTALL_DIR}/dotnet"
  [[ -x "${dotnet_bin}" ]] || fail "no dotnet at ${dotnet_bin}" "$EXIT_ENVIRONMENT"

  local sdks
  sdks="$("${dotnet_bin}" --list-sdks)"
  # A here-string, not `printf | grep -q`: under `set -o pipefail` grep -q exits
  # on the first match, printf is killed by SIGPIPE, and the pipeline status
  # becomes 141 even though the line WAS found - which would report a correctly
  # pinned SDK as missing. build/bootstrap-linux.sh carries the same note; the
  # race is not platform-specific.
  grep -q "^${DOTNET_SDK_VERSION} " <<<"${sdks}" \
    || fail "SDK ${DOTNET_SDK_VERSION} not reported by ${dotnet_bin} --list-sdks" "$EXIT_ENVIRONMENT"

  GODOT_UNDER_TEST="$(godot_under_test)"
  [[ -x "${GODOT_UNDER_TEST}" ]] \
    || fail "no executable Godot at ${GODOT_UNDER_TEST}" "$EXIT_ENVIRONMENT"

  local godot_version
  godot_version="$("${GODOT_UNDER_TEST}" --headless --version)"
  # Match the full pinned prefix, including flavor and channel. Checking only
  # "4.7.1" would accept a non-mono build, which cannot host GodotPlugins at all.
  #
  # This says the executable is the right ENGINE. It says nothing about whether
  # that engine can reach its .NET assemblies, and it cannot: the version is
  # printed before .NET is initialised. assert_godot_hosts_dotnet() below is the
  # check that answers the question this one only looks like it answers.
  [[ "${godot_version}" == "${GODOT_EXPECTED_VERSION_PREFIX}"* ]] \
    || fail "godot reported '${godot_version}', expected ${GODOT_EXPECTED_VERSION_PREFIX}*" "$EXIT_ENVIRONMENT"

  assert_godot_hosts_dotnet "${GODOT_UNDER_TEST}"
  warn_if_symlink_worse "${GODOT_UNDER_TEST}"

  log "dotnet sdk: ${DOTNET_SDK_VERSION} (${DOTNET_INSTALL_DIR})"
  log "godot:      ${godot_version} (${GODOT_UNDER_TEST})"
  log "godot hosts .NET: import of ${GODOT_PROJECT_DIR} ran clean"
}

report_path_guidance() {
  # Doc 100 forbids silently mutating developer configuration, so this prints
  # instead of editing a shell profile. The repository's documented Godot
  # discovery order is MECHAMINER_GODOT first, then `godot` on PATH, so either
  # line below is sufficient and neither requires privilege. This script now reads
  # MECHAMINER_GODOT itself - see godot_under_test() - so the second line is advice
  # this script honours, not just advice it repeats.
  if [[ "${GODOT_SYMLINK_DEGRADED}" -eq 1 ]]; then
    log "Because of the warning above, prefer MECHAMINER_GODOT here; PATH alone will"
    log "give you a 'godot' that cannot host .NET."
  fi
  case ":${PATH}:" in
    *":${USER_BIN_DIR}:"*)
      log "${USER_BIN_DIR} is already on PATH; 'godot' resolves"
      ;;
    *)
      log ""
      log "${USER_BIN_DIR} is NOT on your PATH. Add one of these to your shell profile"
      log "(~/.zprofile on a default macOS shell). This script does not edit it for you:"
      log ""
      log "    export PATH=\"${USER_BIN_DIR}:\$PATH\""
      log "  or"
      log "    export MECHAMINER_GODOT=\"${GODOT_BIN}\""
      log ""
      ;;
  esac
  log "dotnet is at ${DOTNET_INSTALL_DIR}/dotnet. That is hostfxr's default probe"
  log "path on macOS, so DOTNET_ROOT is deliberately NOT set and must not be needed."
  log "Add ${DOTNET_INSTALL_DIR} to PATH if you want to invoke 'dotnet' directly."
}

main() {
  require_macos
  warn_if_root
  require_commands
  assert_sdk_pin

  WORK_DIR="$(mktemp -d)"

  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  export DOTNET_NOLOGO=1

  install_dotnet_sdk
  install_godot
  verify
  report_path_guidance
  # A warning forty lines up is a warning nobody reads. The closing line does not
  # get to say the run was clean when one half of it was not.
  if [[ "${GODOT_SYMLINK_DEGRADED}" -eq 1 ]]; then
    log "OK, with one WARNING above: the pinned toolchain works as"
    log "${GODOT_UNDER_TEST}, but not as ${GODOT_SYMLINK}."
  else
    log "OK"
  fi
}

# Executed: provision. SOURCED: define and stop.
#
# build/verify-bootstrap-macos-hosting.sh sources this file to drive
# godot_under_test(), assert_godot_hosts_dotnet() and warn_if_symlink_worse()
# against Godot installs whose health it controls, which is the only way anything
# in here can be made to go red on a Linux CI host. Without this guard that harness
# would provision the machine it runs on instead. `${BASH_SOURCE[0]}` equals `${0}`
# only when this file is the program being run.
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  main "$@"
fi
