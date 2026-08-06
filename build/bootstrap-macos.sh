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
#     actual artifact on Linux and hashing it, cross-checked against the vendor's
#     own published hash where the vendor publishes one. Those are measured, not
#     asserted. See build/verify-bootstrap-macos.sh for the provenance table.
#
# The first developer to run this on a Mac is its first execution. Treat an
# unexpected failure as a defect in this script, not in their machine.
# ============================================================================
#
# It is idempotent: re-running it revalidates and skips already-correct
# installations.
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
#   Godot executable sha256: extracted from that archive and hashed.
#   .NET tarball sha512/size: taken from Microsoft's own release metadata at
#     https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
#     (release 10.0.10, 2026-07-14) AND independently reproduced by downloading
#     each tarball and running sha512sum. Published and measured agree.
# Retrieved 2026-08-06 UTC.
readonly GODOT_ARCHIVE_NAME="Godot_v${GODOT_VERSION}-stable_mono_macos.universal.zip"
readonly GODOT_ARCHIVE_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/${GODOT_ARCHIVE_NAME}"
readonly GODOT_ARCHIVE_SHA256="92cac516baa8ddc7756eeaa38a6d007778a968bfbf188db7c5d6e6ec21c5d52c"
readonly GODOT_ARCHIVE_SIZE_BYTES="197041155"
readonly GODOT_EXECUTABLE_SHA256="d11dc4a241ec29a347e13c8c7706e49433379ae1f9fc6a6e6819efb3891fce97"

readonly DOTNET_SHA512_ARM64="b2286dec9177e8b5543ff2fe95c84db358b87ec2a36a0d34a29033d70279940fd1134af56c4299648f8950db2d6ce35237698cf2818d9abc670c2c1664c92ac0"
readonly DOTNET_SHA512_X64="48d5861dc0d6c9c782c6d163d6b334ecac2ebd65a1ae59e9ce5b93dd080a31d7ecfc4e4d47e0e35b201ce63661218d641e154022266294a3a8b84593a019cfbc"

# --- user-scoped install locations ----------------------------------------
readonly GODOT_APP_DIR="${HOME}/Applications"
readonly GODOT_APP="${GODOT_APP_DIR}/Godot_mono.app"
readonly GODOT_BIN="${GODOT_APP}/Contents/MacOS/Godot"
readonly USER_BIN_DIR="${HOME}/.local/bin"
readonly GODOT_SYMLINK="${USER_BIN_DIR}/godot"

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

# Escalates for the two operations that genuinely need it, and only when the
# target is not already writable. Printing the command first is the doc 100
# "never mutates global developer configuration silently" requirement.
ensure_install_dir_writable() {
  local directory="$1"
  if [[ -d "${directory}" && -w "${directory}" ]]; then
    log "${directory} already exists and is writable; no privilege needed"
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
}

# ---------------------------------------------------------------------------
# .NET SDK
# ---------------------------------------------------------------------------

install_dotnet_sdk() {
  if [[ -d "${DOTNET_INSTALL_DIR}/sdk/${DOTNET_SDK_VERSION}" ]]; then
    log ".NET SDK ${DOTNET_SDK_VERSION} already present in ${DOTNET_INSTALL_DIR}"
    return
  fi

  local rid tarball url
  rid="$(dotnet_rid)"
  url="https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_SDK_VERSION}/dotnet-sdk-${DOTNET_SDK_VERSION}-${rid}.tar.gz"
  tarball="${WORK_DIR}/dotnet-sdk-${DOTNET_SDK_VERSION}-${rid}.tar.gz"

  log "installing .NET SDK ${DOTNET_SDK_VERSION} (${rid}) into ${DOTNET_INSTALL_DIR}"
  download "${url}" "${tarball}"
  verify_digest ".NET SDK ${rid} tarball" "${tarball}" 512 "$(dotnet_expected_sha512)"

  ensure_install_dir_writable "${DOTNET_INSTALL_DIR}"

  # The SDK tarball unpacks as the CONTENTS of the install root - dotnet, sdk/,
  # shared/, host/ at the top level - so it extracts directly into the install
  # directory with no strip-components. Extracting over an existing older SDK is
  # the supported side-by-side shape: sdk/<version>/ directories coexist.
  tar -xzf "${tarball}" -C "${DOTNET_INSTALL_DIR}" \
    || fail "could not extract the .NET SDK tarball into ${DOTNET_INSTALL_DIR}"

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

  if [[ -e "${GODOT_APP}" ]]; then
    log "replacing ${GODOT_APP}: present but not matching the pinned executable hash"
    rm -rf "${GODOT_APP}"
  fi

  local archive="${WORK_DIR}/${GODOT_ARCHIVE_NAME}"
  log "installing Godot ${GODOT_VERSION} .NET (universal) into ${GODOT_APP_DIR}"
  download "${GODOT_ARCHIVE_URL}" "${archive}"
  verify_size "Godot archive" "${archive}" "${GODOT_ARCHIVE_SIZE_BYTES}"
  verify_digest "Godot archive" "${archive}" 256 "${GODOT_ARCHIVE_SHA256}"

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
  ln -sfn "${GODOT_BIN}" "${GODOT_SYMLINK}"
  log "linked ${GODOT_SYMLINK} -> ${GODOT_BIN}"
}

# ---------------------------------------------------------------------------
# Verification
# ---------------------------------------------------------------------------

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

  local godot_version
  godot_version="$("${GODOT_SYMLINK}" --headless --version)"
  # Match the full pinned prefix, including flavor and channel. Checking only
  # "4.7.1" would accept a non-mono build, which cannot host GodotPlugins at all.
  [[ "${godot_version}" == "${GODOT_EXPECTED_VERSION_PREFIX}"* ]] \
    || fail "godot reported '${godot_version}', expected ${GODOT_EXPECTED_VERSION_PREFIX}*" "$EXIT_ENVIRONMENT"

  log "dotnet sdk: ${DOTNET_SDK_VERSION} (${DOTNET_INSTALL_DIR})"
  log "godot:      ${godot_version}"
}

report_path_guidance() {
  # Doc 100 forbids silently mutating developer configuration, so this prints
  # instead of editing a shell profile. The repository's documented Godot
  # discovery order is MECHAMINER_GODOT first, then `godot` on PATH, so either
  # line below is sufficient and neither requires privilege.
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
  log "OK"
}

main "$@"
