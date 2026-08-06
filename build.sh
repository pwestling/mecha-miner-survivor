#!/usr/bin/env bash
#
# macOS/Linux workflow wrapper. The single entry point for every repository verb.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface
#            AGENTS.md § Standard workflow surface ("Do not create competing
#              workflow entrypoints.")
# Requirements: TR-BLD-005, TR-BLD-001, TR-BLD-002
# Verification: tests/verification/FND-002.json
#
# This file is deliberately a launcher and nothing else. Doc 100: the wrappers "are
# thin launchers for pinned tools and project-owned typed tooling; domain workflow
# logic is not duplicated between shell languages." The verb table, the argument
# names, the exit classes, and the structured artifacts all live in
# src/MechaMiner.Tools, which build.ps1 launches identically. That is why wrapper
# parity is a property of the design rather than something two scripts must be kept
# in agreement about: no verb name appears in this file.
#
# Exit classes, from doc 100 § Standard command surface (there is no class 1):
#   0 success                     4 validation/test failure
#   2 invalid verb/arguments      5 build/import/export/package failure
#   3 missing/mismatched pinned   6 performance/budget failure
#     environment                 7 authorization/credential/external state
#                                 8 unexpected tool-internal failure
#
# The launcher itself can produce only two of those: 3 when the pinned .NET SDK is
# not usable - absent from PATH, or present but pinned by global.json to a version no
# installed SDK satisfies - and 8 when the verb host does not build. Everything else
# is the owning tool's class, returned unchanged.
#
# Those two cases are deliberately separated below. "dotnet is on PATH" is not the
# same claim as "the pinned SDK resolves", and doc 100 defines class 3 as a "missing
# or mismatched pinned environment", so a mismatched pin is class 3 and never class 8.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly HOST_PROJECT="${REPO_ROOT}/src/MechaMiner.Tools/MechaMiner.Tools.csproj"
readonly HOST_CONFIGURATION="Debug"
readonly HOST_ASSEMBLY="${REPO_ROOT}/src/MechaMiner.Tools/bin/${HOST_CONFIGURATION}/net8.0/MechaMiner.Tools.dll"
readonly LAUNCHER_LOG_DIR="${REPO_ROOT}/artifacts/wrapper"

readonly EXIT_ENVIRONMENT=3
readonly EXIT_INTERNAL=8

# Noninteractive and locale-stable for every child tool.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_UI_LANGUAGE=en-US

if ! command -v dotnet >/dev/null 2>&1; then
  cat >&2 <<'INSTRUCTIONS'
FAILED [MMT-3001] the pinned .NET SDK is not on PATH, so the verb host cannot run.
Exit class 3 (missing or mismatched pinned environment).

A .NET process cannot bootstrap .NET. Install the pinned SDK first, exactly:

    sudo build/bootstrap-linux.sh

That script is idempotent, installs the version global.json pins into
/usr/share/dotnet (hostfxr's default probe path, which Godot's runtime host
requires), and then verifies it. Afterwards every other verb is reachable through
this wrapper:

    ./build.sh doctor
INSTRUCTIONS
  exit "${EXIT_ENVIRONMENT}"
fi

# MISMATCH-PROBE-BEGIN
#
# dotnet being on PATH does not mean the pinned SDK is usable. global.json pins an
# exact version, and hostfxr resolves that pin from the repository root rather than
# taking the newest install; a pin no installed SDK satisfies makes every SDK command
# fail, including the verb host's own build.
#
# `dotnet --version` is the cheapest invocation that performs exactly that
# resolution: it prints the resolved SDK version when the pin is satisfiable and
# fails with the SDK-resolution error when it is not. It is run from REPO_ROOT
# because that is the directory whose global.json governs the resolution.
#
# Without this probe the mismatch surfaced one step later, as a failed `dotnet build`
# of the verb host, and was therefore reported as class 8 / MMT-8001 "unexpected
# tool-internal failure". That is the wrong class and an actively misleading
# diagnosis: nothing in this repository is broken when an operator pins an SDK they
# have not installed. Doc 100 § Standard command surface defines class 3 as a
# "missing or mismatched pinned environment", so a mismatched pin is class 3, exactly
# like an absent SDK, and MMT-3001 is already documented as "a pinned tool or version
# is missing or mismatched".
#
# The gate for this classification is build/verify-verbs.sh § 10, whose negative
# control deletes the block between these two markers to prove the assertion fails
# when the wrapper misclassifies.
sdk_resolution=""
if ! sdk_resolution="$(cd -- "${REPO_ROOT}" && dotnet --version 2>&1)"; then
  {
    echo "FAILED [MMT-3001] global.json pins a .NET SDK version that is not installed here."
    echo "        Exit class 3 (missing or mismatched pinned environment)."
    echo "        dotnet is on PATH, so this is a version mismatch and not an absent SDK."
    echo "        It is not a repository fault: nothing in src/ is broken."
    echo
    echo "        Resolution attempt (cd \"${REPO_ROOT}\" && dotnet --version):"
    printf '%s\n' "${sdk_resolution}" | sed 's/^/          /'
    echo
    echo "        Installed SDKs:"
    dotnet --list-sdks 2>&1 | sed 's/^/          /'
    echo
    echo "        Either install the pinned SDK, exactly:"
    echo
    echo "            sudo build/bootstrap-linux.sh"
    echo
    echo "        (idempotent; installs the version global.json pins into"
    echo "        /usr/share/dotnet), or correct the pin in global.json. Do not"
    echo "        silently widen rollForward to make an unpinned SDK resolve:"
    echo "        doc 100 § Toolchain pinning requires the exact SDK to be pinned."
  } >&2
  exit "${EXIT_ENVIRONMENT}"
fi
# MISMATCH-PROBE-END

mkdir -p "${LAUNCHER_LOG_DIR}"

# Build the verb host before dispatching. `dotnet run` is deliberately not used: it
# returns 1 when the build fails, and 1 is not a class doc 100 defines.
if ! dotnet build "${HOST_PROJECT}" \
      --configuration "${HOST_CONFIGURATION}" \
      --nologo \
      --verbosity quiet \
      >"${LAUNCHER_LOG_DIR}/host-build.log" 2>&1; then
  echo "FAILED [MMT-8001] the verb host in src/MechaMiner.Tools did not build." >&2
  echo "        Exit class 8 (unexpected tool-internal failure)." >&2
  echo "        Build log: artifacts/wrapper/host-build.log" >&2
  tail -n 40 "${LAUNCHER_LOG_DIR}/host-build.log" >&2
  exit "${EXIT_INTERNAL}"
fi

if [[ ! -f "${HOST_ASSEMBLY}" ]]; then
  echo "FAILED [MMT-8001] the verb host built but ${HOST_ASSEMBLY} is missing." >&2
  echo "        Exit class 8 (unexpected tool-internal failure)." >&2
  exit "${EXIT_INTERNAL}"
fi

# The repository root is passed explicitly so the host never searches upward for a
# marker file (TR-BLD-006). exec makes the host's exit class this script's.
exec dotnet "${HOST_ASSEMBLY}" "${REPO_ROOT}" "$@"
