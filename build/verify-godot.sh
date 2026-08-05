#!/usr/bin/env bash
#
# Proves the Godot project imports and launches headlessly from a cold cache.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Godot import and export, § Continuous integration ("CI jobs start
#              from clean checkouts and cannot depend on a developer's Godot import
#              cache")
#            docs/technical/00-technical-foundation.md § Renderer baseline
# Requirements: TR-FND-001, TR-FND-002, TR-BLD-002
# Verification: VER-FND-001-012 (headless import), VER-FND-001-013 (headless launch)
#
# Why this is a script and not just two commands:
#
#   Godot returns exit code 0 from a headless launch even when the C# script on the
#   boot node fails to load - it logs "Cannot instantiate C# script" and carries on.
#   Asserting only the exit code would pass a completely broken project. This script
#   asserts the exit code AND the composition root's stable startup line AND the
#   absence of engine ERROR lines.
#
# Build-order fact this script encodes: Godot.NET.Sdk puts both obj/ and bin/ for
# MechaMiner.Game inside game/.godot/mono/temp/, and .godot is gitignored. So a cold
# cache means no restore assets and no game assembly, and the order must be
# restore -> build -> import -> launch. FND-002's build and godot-import verbs
# inherit this ordering.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure, 5 build/import failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly GAME_DIR="${REPO_ROOT}/game"
readonly STARTUP_LINE="MechaMiner: boot composition root ready"
readonly LAUNCH_FRAMES=60
readonly EXIT_VALIDATION=4
readonly EXIT_BUILD=5

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

strip_ansi() {
  sed -e 's/\x1b\[[0-9;]*m//g'
}

engine_problem_lines() {
  grep -E '(^|[[:space:]])(ERROR|WARNING|SCRIPT ERROR|USER ERROR):' || true
}

echo "=== cold cache: removing game/.godot"
rm -rf "${GAME_DIR}/.godot"

echo
echo "=== restore and build the game assembly (its obj/bin live inside .godot)"
if ! dotnet build "${GAME_DIR}/MechaMiner.Game.csproj" --nologo -v q; then
  fail "the game project must build before Godot can load its assembly"
  exit "${EXIT_BUILD}"
fi
pass "MechaMiner.Game built"

echo
echo "=== VER-FND-001-012: godot headless import"
import_log="$(godot --headless --path "${GAME_DIR}" --import 2>&1 | strip_ansi)"
import_status="${PIPESTATUS[0]}"
if [[ "${import_status}" -ne 0 ]]; then
  fail "headless import exited ${import_status}, expected 0"
else
  pass "headless import exited 0"
fi
import_problems="$(printf '%s\n' "${import_log}" | engine_problem_lines)"
if [[ -n "${import_problems}" ]]; then
  fail "headless import reported engine problems"
  printf '%s\n' "${import_problems}" | sed 's/^/      /'
else
  pass "headless import reported no ERROR or WARNING line"
fi

echo
echo "=== VER-FND-001-013: godot headless launch"
launch_log="$(godot --headless --path "${GAME_DIR}" --quit-after "${LAUNCH_FRAMES}" 2>&1 | strip_ansi)"
launch_status="${PIPESTATUS[0]}"
if [[ "${launch_status}" -ne 0 ]]; then
  fail "headless launch exited ${launch_status}, expected 0"
else
  pass "headless launch exited 0"
fi
if printf '%s\n' "${launch_log}" | grep -qF "${STARTUP_LINE}"; then
  pass "composition root printed its stable startup line"
else
  fail "startup line not found; the boot scene did not reach managed code"
  printf '%s\n' "${launch_log}" | sed 's/^/      /'
fi
launch_problems="$(printf '%s\n' "${launch_log}" | engine_problem_lines)"
if [[ -n "${launch_problems}" ]]; then
  fail "headless launch reported engine problems (Godot exits 0 even for these)"
  printf '%s\n' "${launch_problems}" | sed 's/^/      /'
else
  pass "headless launch reported no ERROR or WARNING line"
fi

echo
echo "=== no tracked file may be mutated by import or launch"
mutated="$(cd "${REPO_ROOT}" && git status --porcelain -- game 2>/dev/null || true)"
if [[ -z "${mutated}" ]]; then
  pass "game/ has no unexpected tracked-file change"
else
  fail "import or launch mutated tracked files"
  printf '%s\n' "${mutated}" | sed 's/^/      /'
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-godot: PASS"
  exit 0
fi
echo "verify-godot: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
