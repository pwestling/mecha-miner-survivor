#!/usr/bin/env bash
#
# Reproducible Linux x86-64 toolchain bootstrap for a clean machine or CI runner.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#            (Toolchain pinning: "A bootstrap command verifies/downloads approved
#            public tools or prints exact manual installation instructions and
#            checks hashes. It never mutates global developer configuration
#            silently.")
# Requirements: TR-BLD-001, TR-BLD-002, TR-FND-001
#
# This script is the reproducible record of the pinned environment. It is wired
# into the `bootstrap` verb of ./build.sh by TASK-FND-002-001; until then it is
# invoked directly. It is idempotent: re-running it revalidates and skips
# already-correct installations.
#
# It installs to shared system locations on purpose:
#   * .NET must live at /usr/share/dotnet because that is hostfxr's default probe
#     path. Godot's GodotPlugins host resolves the runtime from there without
#     DOTNET_ROOT or `dotnet` on PATH; a custom install directory breaks the
#     headless game launch.
#   * Godot lives at /opt/godot with a /usr/local/bin/godot symlink.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 3 missing or mismatched pinned environment, 8 unexpected tool-internal failure.

set -euo pipefail

readonly DOTNET_SDK_VERSION="10.0.302"
readonly DOTNET_INSTALL_DIR="/usr/share/dotnet"

readonly GODOT_VERSION="4.7.1"
readonly GODOT_ARCHIVE="Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64"
readonly GODOT_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_VERSION}-stable/${GODOT_ARCHIVE}.zip"
readonly GODOT_ROOT="/opt/godot"
readonly GODOT_BIN="${GODOT_ROOT}/${GODOT_ARCHIVE}/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64"

readonly EXIT_ENVIRONMENT=3
readonly EXIT_INTERNAL=8

log() { printf '[bootstrap] %s\n' "$*"; }
fail() { printf '[bootstrap] FAILED: %s\n' "$*" >&2; exit "${2:-$EXIT_INTERNAL}"; }

require_root() {
  if [[ "${EUID}" -ne 0 ]]; then
    fail "must run as root to write ${DOTNET_INSTALL_DIR} and ${GODOT_ROOT}" "$EXIT_ENVIRONMENT"
  fi
}

install_dotnet_sdk() {
  if [[ -d "${DOTNET_INSTALL_DIR}/sdk/${DOTNET_SDK_VERSION}" ]]; then
    log ".NET SDK ${DOTNET_SDK_VERSION} already present"
  else
    log "installing .NET SDK ${DOTNET_SDK_VERSION} into ${DOTNET_INSTALL_DIR}"
    local script
    script="$(mktemp)"
    curl -fsSL -o "${script}" https://dot.net/v1/dotnet-install.sh \
      || fail "could not download dotnet-install.sh" "$EXIT_ENVIRONMENT"
    bash "${script}" --version "${DOTNET_SDK_VERSION}" --install-dir "${DOTNET_INSTALL_DIR}" \
      || fail "dotnet-install.sh failed"
    rm -f "${script}"
  fi
  ln -sfn "${DOTNET_INSTALL_DIR}/dotnet" /usr/local/bin/dotnet
}

install_godot() {
  if [[ -x "${GODOT_BIN}" ]]; then
    log "Godot ${GODOT_VERSION} already present"
  else
    log "installing Godot ${GODOT_VERSION} .NET into ${GODOT_ROOT}"
    mkdir -p "${GODOT_ROOT}"
    local archive
    archive="$(mktemp --suffix=.zip)"
    curl -fsSL -o "${archive}" "${GODOT_URL}" \
      || fail "could not download ${GODOT_URL}" "$EXIT_ENVIRONMENT"
    unzip -q -o "${archive}" -d "${GODOT_ROOT}" || fail "could not unzip Godot archive"
    rm -f "${archive}"
  fi
  [[ -x "${GODOT_BIN}" ]] || fail "expected Godot binary missing at ${GODOT_BIN}" "$EXIT_ENVIRONMENT"
  ln -sfn "${GODOT_BIN}" /usr/local/bin/godot
}

install_software_vulkan() {
  # Without a Vulkan ICD, Godot silently falls back to OpenGL 3 and the mandated
  # Mobile renderer is never exercised. lavapipe (mesa-vulkan-drivers) provides a
  # software ICD. Only needed for the windowed/Xvfb rendering tier, not
  # --headless, but installing it keeps one bootstrap for all tiers.
  if [[ -d /usr/share/vulkan/icd.d ]] && compgen -G '/usr/share/vulkan/icd.d/*.json' >/dev/null; then
    log "Vulkan ICD already present"
    return
  fi
  if ! command -v apt-get >/dev/null 2>&1; then
    log "WARNING: no apt-get; install a Vulkan ICD manually for windowed render checks"
    return
  fi
  log "installing mesa-vulkan-drivers (the preloaded apt index is stale, so update first)"
  apt-get update -q || fail "apt-get update failed" "$EXIT_ENVIRONMENT"
  apt-get install -y -q mesa-vulkan-drivers || fail "apt-get install failed" "$EXIT_ENVIRONMENT"
}

verify() {
  log "verifying pinned versions"
  local sdks
  sdks="$(/usr/local/bin/dotnet --list-sdks)"
  # A here-string, not `printf | grep -q`: under `set -o pipefail` grep -q exits on
  # the first match, printf is killed by SIGPIPE, and the pipeline status becomes
  # 141 even though the line was found - which would report a correctly pinned SDK
  # as missing. build/verify-policies.sh hit exactly that race.
  grep -q "^${DOTNET_SDK_VERSION} " <<<"${sdks}" \
    || fail "SDK ${DOTNET_SDK_VERSION} not reported by dotnet --list-sdks" "$EXIT_ENVIRONMENT"
  local godot_version
  godot_version="$(/usr/local/bin/godot --headless --version)"
  [[ "${godot_version}" == "${GODOT_VERSION}"* ]] \
    || fail "godot reported '${godot_version}', expected ${GODOT_VERSION}" "$EXIT_ENVIRONMENT"
  log "dotnet sdk: ${DOTNET_SDK_VERSION}"
  log "godot:      ${godot_version}"
}

main() {
  require_root
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  export DOTNET_NOLOGO=1
  install_dotnet_sdk
  install_godot
  install_software_vulkan
  verify
  log "OK"
}

main "$@"
