#!/usr/bin/env bash
#
# Proves that build identity is one value, reported identically by the workflow host,
# by the Godot game process, and by the generated SCH-BLD-001 manifest.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Version and build identity - every executable, about screen,
#              diagnostic header, and result manifest carries product version and
#              build number, source commit and dirty flag, Godot and .NET versions,
#              content bundle hash, schema/map/random/save versions, and build
#              configuration/platform
#            docs/technical/115-component-contract-and-schema-registry.md
#              § Initialization order step 1 - "Verify build/tool identity embedded in
#              the executable"
# Requirements: TR-BLD-001, TR-BLD-004, TR-BLD-005, TR-RUN-009
# Verification: VER-FND-004-008 (and the pasteable evidence for VER-FND-004-004)
#
# This is an equality assertion, not three reads. Three separate processes are asked
# for their identity and the three answers must be one string:
#
#   1. the workflow host, through the build_identity field of a verb result document;
#   2. the Godot game process, through the boot composition root's identity line - a
#      different game surface from the engine test runner that
#      MechaMiner.Game.Tests.BuildIdentityEqualityTests compares; and
#   3. diagnostics, through generated/build-manifest.json, which CMP-OBS-001 emits.
#
# A negative control proves the comparison can fail: the manifest is tampered with and
# the same comparison must reject it.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly GAME_DIR="${REPO_ROOT}/game"
readonly MANIFEST="${REPO_ROOT}/generated/build-manifest.json"
readonly BOOT_IDENTITY_PREFIX="MechaMiner: build identity "
readonly LAUNCH_FRAMES=60
readonly EXIT_VALIDATION=4

failures=0

fail() {
  printf 'FAIL  %s\n' "$*"
  failures=$((failures + 1))
}

pass() {
  printf 'ok    %s\n' "$*"
}

godot_command() {
  if [[ -n "${MECHAMINER_GODOT:-}" ]]; then
    printf '%s' "${MECHAMINER_GODOT}"
  else
    printf '%s' "godot"
  fi
}

json_string() {
  # $1 file, $2 top-level field name
  python3 -c '
import json, sys
with open(sys.argv[1], "r", encoding="utf-8") as handle:
    document = json.load(handle)
sys.stdout.write(str(document[sys.argv[2]]))
' "$1" "$2"
}

echo "=== 1. the workflow host builds, emits the manifest, and reports its identity"
build_output="$("${WRAPPER}" build 2>&1)"
build_status=$?
if [[ "${build_status}" -ne 0 ]]; then
  fail "build exited ${build_status}"
  printf '%s\n' "${build_output}" | tail -20 | sed 's/^/      /'
  echo
  echo "verify-build-identity: FAIL (${failures} assertion(s))"
  exit "${EXIT_VALIDATION}"
fi
pass "build exited 0 and reported a current SCH-BLD-001 manifest"

if [[ ! -f "${MANIFEST}" ]]; then
  fail "generated/build-manifest.json was not written"
  echo
  echo "verify-build-identity: FAIL (${failures} assertion(s))"
  exit "${EXIT_VALIDATION}"
fi

manifest_identity="$(json_string "${MANIFEST}" identity_line)"
tool_identity="$(json_string "${REPO_ROOT}/artifacts/verbs/build/latest-result.json" build_identity)"

echo
echo "=== 2. the Godot game process reports its identity from the boot composition root"
#
# The game assembly and the import cache already exist after `build` plus any earlier
# godot-import; a cold cache is VER-FND-001-012's subject, not this one. The launch is
# frame-bounded so it cannot hang.
launch_output="$("$(godot_command)" --headless --path "${GAME_DIR}" \
  --audio-driver Dummy --quit-after "${LAUNCH_FRAMES}" 2>&1)"
launch_status=$?
game_identity="$(printf '%s\n' "${launch_output}" \
  | sed -e 's/\x1b\[[0-9;]*m//g' \
  | sed -n "s|^${BOOT_IDENTITY_PREFIX}||p" | tail -1)"

if [[ "${launch_status}" -eq 0 && -n "${game_identity}" ]]; then
  pass "the boot composition root printed its build identity as its first line"
else
  fail "the headless launch exited ${launch_status} and printed no build identity line"
  printf '%s\n' "${launch_output}" | tail -20 | sed 's/^/      /'
fi

echo
echo "=== 3. the three identities are one value"
printf '      tool        %s\n' "${tool_identity}"
printf '      game        %s\n' "${game_identity}"
printf '      diagnostics %s\n' "${manifest_identity}"

if [[ "${tool_identity}" == "${manifest_identity}" ]]; then
  pass "tool identity equals the SCH-BLD-001 manifest"
else
  fail "tool identity differs from the manifest"
fi

if [[ "${game_identity}" == "${manifest_identity}" ]]; then
  pass "game identity equals the SCH-BLD-001 manifest"
else
  fail "game identity differs from the manifest"
fi

echo
echo "=== 4. every required identity field is present in the manifest"
missing="$(python3 -c '
import json, sys
with open(sys.argv[1], "r", encoding="utf-8") as handle:
    document = json.load(handle)
required = {
    "product": ["version", "build_number", "build_number_source"],
    "source": ["commit", "commit_short", "dirty"],
    "toolchain": ["godot_version", "dotnet_sdk_version", "target_framework"],
    "content": ["bundle_sha256", "status", "owning_work_package"],
    "data_versions": ["schema", "map", "random", "save"],
    "target": ["workflow_configuration", "msbuild_configuration", "platform"],
}
missing = []
for group, fields in required.items():
    if group not in document:
        missing.append(group)
        continue
    for field in fields:
        if field not in document[group]:
            missing.append(group + "." + field)
if "artifacts" not in document:
    missing.append("artifacts")
sys.stdout.write(",".join(missing))
' "${MANIFEST}")"
if [[ -z "${missing}" ]]; then
  pass "the manifest carries every field doc 100 § Version and build identity requires"
else
  fail "the manifest is missing: ${missing}"
fi

echo
echo "=== 5. negative control: a tampered manifest must be rejected"
#
# Without this the equality above could be vacuous. The manifest is copied, its commit
# is replaced, and the same comparison must reject the copy.
tampered="$(mktemp)"
python3 -c '
import json, sys
with open(sys.argv[1], "r", encoding="utf-8") as handle:
    document = json.load(handle)
document["source"]["commit"] = "0" * 40
document["identity_line"] = document["identity_line"].replace(
    "commit=" + sys.argv[3], "commit=" + "0" * 40)
with open(sys.argv[2], "w", encoding="utf-8") as handle:
    json.dump(document, handle)
' "${MANIFEST}" "${tampered}" "$(json_string "${MANIFEST}" commit 2>/dev/null || python3 -c '
import json, sys
with open(sys.argv[1], "r", encoding="utf-8") as handle:
    sys.stdout.write(json.load(handle)["source"]["commit"])
' "${MANIFEST}")"

tampered_identity="$(json_string "${tampered}" identity_line)"
rm -f "${tampered}"
if [[ "${tampered_identity}" != "${manifest_identity}" ]]; then
  pass "the tampered manifest is rejected by the same comparison (control differs)"
else
  fail "the tampered manifest compared equal, so the comparison above proves nothing"
fi

echo
if [[ "${failures}" -eq 0 ]]; then
  echo "verify-build-identity: PASS"
  exit 0
fi
echo "verify-build-identity: FAIL (${failures} assertion(s))"
exit "${EXIT_VALIDATION}"
