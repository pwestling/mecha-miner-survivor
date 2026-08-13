#!/usr/bin/env bash
#
# Proves the format gate with a deliberately misformatted fixture: format-check
# fails and writes nothing, format repairs the tree, and format-check then passes.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface (format: "format owned text/code and fail
#              if the resulting tree still violates policy"; format-check: "validate
#              formatting without writes")
#            .editorconfig, which records that IDE0055 and IDE0005 stay at
#              suggestion severity at build time precisely so this verb is their
#              single owner
# Requirements: TR-BLD-001, TR-BLD-005
# Verification: VER-FND-002-010, VER-FND-002-011
#
# The fixture is transient: this script writes it, runs the gates, and removes it on
# every exit path, including failure. It is never committed. FND-001 established that
# deliberately invalid fixtures live under build/policy-fixtures/, outside
# MechaMiner.sln; this one cannot, because the gate under test formats the solution
# and would not see a project the solution excludes. It is therefore placed in a test
# project rather than in a shipping assembly, so no committed file inside
# MechaMiner.Tools can ever fight format, format-check, or warnings-as-errors.
#
# The fixture covers all three gates the verb owns in one file:
#   * C# whitespace (IDE0055): wrong indentation and stray spaces.
#   * unnecessary using (IDE0005): an import nothing needs.
#   * owned-text rules: a trailing-whitespace line and no final newline.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly WRAPPER="${REPO_ROOT}/build.sh"
readonly FIXTURE="${REPO_ROOT}/tests/MechaMiner.Game.Tests/MisformattedFormatFixture.cs"
readonly EXIT_VALIDATION=4

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

cleanup() {
  rm -f "${FIXTURE}"
}
trap cleanup EXIT

write_fixture() {
  # printf, not a heredoc: the fixture must end WITHOUT a final newline and must
  # carry literal trailing whitespace, and an editor-friendly heredoc would not
  # preserve either.
  printf '%s' 'using System;
using System.Text;

namespace MechaMiner.Game.Tests;

/// <summary>Deliberately misformatted fixture written by build/verify-format.sh.</summary>
internal static class MisformattedFormatFixture
{
        internal static int Value( )
    {
            return    1;
  }
}' >"${FIXTURE}"
}

section "1. a misformatted tracked file makes format-check fail (VER-FND-002-010)"
write_fixture
fixture_before="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"

output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 4 ]] && grep -q 'MMT-4001' <<<"${output}"; then
  pass "format-check exited 4 with MMT-4001"
else
  fail "format-check exited ${status} (expected 4 with MMT-4001)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

for expectation in \
    'C# whitespace' \
    'C# style diagnostics' \
    'owned-text rules'; do
  if grep -qF "${expectation}" <<<"${output}"; then
    pass "format-check named the failing gate: ${expectation}"
  else
    fail "format-check did not name the failing gate: ${expectation}"
  fi
done

section "2. format-check wrote nothing (VER-FND-002-010)"
fixture_after_check="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"
if [[ "${fixture_before}" == "${fixture_after_check}" ]]; then
  pass "the fixture is byte-identical after format-check: sha256 ${fixture_after_check}"
else
  fail "format-check modified the fixture; it must validate without writes"
fi

section "3. format repairs the tree (VER-FND-002-011)"
output="$("${WRAPPER}" format 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]]; then
  pass "format exited 0"
else
  fail "format exited ${status} (expected 0)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

fixture_after_format="$(sha256sum "${FIXTURE}" | cut -d' ' -f1)"
if [[ "${fixture_before}" != "${fixture_after_format}" ]]; then
  pass "format changed the fixture: sha256 ${fixture_before} -> ${fixture_after_format}"
else
  fail "format did not change the misformatted fixture"
fi

# The fixture body is written by this script and dumped on every run, so it is marked:
# it contains the very whitespace/using text a reader might be grepping for. `head -20`
# after a `sed -n '1,20p'` can never close the pipe early, and its status is discarded
# anyway, so it stays a pipeline (Decision 13).
control_detail <<<'repaired fixture:'
control_detail < <(sed -n '1,20p' "${FIXTURE}" | cat -A | head -20)

section "4. format-check now passes (VER-FND-002-011)"
output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]] && grep -q 'MMT-0000' <<<"${output}"; then
  pass "format-check exited 0 with MMT-0000 after format"
else
  fail "format-check exited ${status} after format (expected 0)"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

section "5. the repository returns to a clean state"
cleanup
output="$("${WRAPPER}" format-check 2>&1)"
status=$?
if [[ "${status}" -eq 0 ]]; then
  pass "format-check passes on the committed tree with the fixture removed"
else
  fail "format-check exited ${status} on the committed tree"
  printf '%s\n' "${output}" | tail -8 | sed 's/^/      /'
fi

section "6. an unobtainable file set fails instead of passing vacuously (VER-FND-002-018)"
#
# format and format-check derive their owned-text candidate set from `git ls-files`. That
# enumeration used to return an empty list on failure and record no failure of its own,
# so violations.Count == 0 fired the success assertion: with a real trailing-whitespace
# violation on disk and nothing but a stale GIT_DIR, format-check printed
# "ok owned-text-rules: every owned text file satisfies ..." and exited 0, and format
# printed "the formatted tree satisfies every gate" while leaving the violation in place.
#
# Doc 100 requires format to "fail if the resulting tree still violates policy". A gate
# that inspected nothing has established nothing about the tree, so an unobtained set is
# now a failure and never an empty one.
#
# GIT_DIR is used rather than removing anything: it is nondestructive, it is exactly the
# route the defect was found through, and it leaves the real repository untouched.
#
# The two assertions below read `${output}` with a here-string and never through a pipe.
# They used to be `printf '%s' "${output}" | grep -qE ...`, and that construct is a
# SECOND, INDEPENDENTLY SUFFICIENT cause of this section's intermittent result. The
# earlier diagnosis - that the verbs could emit an empty `output` - is correct and
# incomplete; this cause fires on a perfectly good `output` and requires only that it be
# large. `grep -q` exits the instant it matches, bash's `printf` then takes SIGPIPE on a
# later write, and under `set -o pipefail` the PIPELINE reports 141:
#
#   * line 1 is `... || problems+=(...)`, so 141 fabricates a failure that is not there
#     and § 6 goes red naming "no owned-text-file-set failure was recorded";
#   * line 2 is `... && problems+=(...)`, so 141 silently DISCARDS the real violation
#     this section exists to catch.
#
# The two invert in opposite directions and flip together in the same run, so the visible
# symptom is a red gate naming the wrong thing while the finding it was written for is
# dropped. Measured over 300 trials per size on one `output` carrying both markers early:
# 0 of 300 at 476 bytes, 0 and 1 of 300 at 4.2 KB, 18 and 21 of 300 at 70 KB, and 300 of
# 300 for BOTH lines at 187 KB. A real `build.sh format-check` log is well past 4 KB. The
# here-string form measured 0 of 300 at every one of those sizes.
readonly STALE_GIT_DIR="/nonexistent/verify-format-stale.git"

# The two reads, factored out so § 6b's controls drive the identical predicate rather
# than a paraphrase of it. Prints one problem per line and nothing when the output has
# the shape an unobtainable file set must produce.
unobtained_set_problems() {
  local text="$1"
  local exit_status="$2"
  [[ "${exit_status}" -ne 0 ]] || printf '%s\n' "exit 0; an unreadable tree must not pass"
  grep -qE 'FAIL[[:space:]]+owned-text-file-set' <<<"${text}" \
    || printf '%s\n' "no owned-text-file-set failure was recorded"
  # The specific vacuous-success string that used to appear must not appear.
  grep -qE 'ok[[:space:]]+owned-text-rules' <<<"${text}" \
    && printf '%s\n' "owned-text-rules still reported ok on a set it never obtained"
  return 0
}

for verb in format-check format; do
  output="$(GIT_DIR="${STALE_GIT_DIR}" "${WRAPPER}" "${verb}" 2>&1)"
  status=$?

  mapfile -t problems < <(unobtained_set_problems "${output}" "${status}")

  if [[ "${#problems[@]}" -eq 0 ]]; then
    pass "${verb} with an unreadable git: exit ${status}, and the file set is reported as not obtained"
  else
    fail "${verb} with an unreadable git: $(printf '%s; ' "${problems[@]}")"
    printf '%s\n' "${output}" | grep -E 'owned-text|MMT-' | sed 's/^/      /'
  fi
done

# Negative control: the same two verbs, same tree, healthy git. They must pass. Without
# this, § 6 would also be satisfied by a verb that had simply been made to fail always.
for verb in format-check format; do
  output="$("${WRAPPER}" "${verb}" 2>&1)"
  status=$?
  if [[ "${status}" -eq 0 ]] && grep -qE 'ok[[:space:]]+owned-text-file-set' <<<"${output}"; then
    control_pass "negative control: ${verb} passes on the same tree with a healthy git, so § 6 failed on the unreadable set and not unconditionally"
  else
    control_fail "negative control: ${verb} exited ${status} with a healthy git (expected 0); § 6 may be failing unconditionally"
    printf '%s\n' "${output}" | grep -E 'owned-text|MMT-' | sed 's/^/      /'
  fi
done

section "6b. controls on § 6's own two reads, at production input size"
#
# § 6's controls above vary the ENVIRONMENT (stale git vs healthy git) and leave the
# reads themselves unexercised: both drive `${output}` values that happen to be small
# and correctly shaped, so neither could ever have caught the SIGPIPE defect described
# over § 6. The controls below vary the OUTPUT instead, holding everything else fixed,
# and each shape is driven twice - once at fixture size and once at ~200 KB with the
# marker late in the stream, which is the size and position the defect needed.
#
# This corpus therefore includes a production-sized case by construction. A one-line
# fixture is not a control for this class; it is a control that structurally cannot fail,
# because the miss rate is zero below grep's ~4 KB buffer. That is precisely why four
# review rounds passed over it.
readonly OWNED_TEXT_FILLER='      at MechaMiner.Tools.Format.OwnedText.Scan(String path) in /repo/src/MechaMiner.Tools/Format/OwnedText.cs:line 118'

# "<label>|<markers: fail-only|vacuous-ok|neither>|<expected problem count>|<must contain>"
readonly SECTION6_CONTROLS=(
  "an unobtainable set, correctly reported|fail-only|0|"
  "the vacuous success this section exists to catch|vacuous-ok|1|owned-text-rules still reported ok"
  "a run that recorded no file-set failure at all|neither|1|no owned-text-file-set failure was recorded"
)

for control in "${SECTION6_CONTROLS[@]}"; do
  IFS='|' read -r label markers expected_count must_contain <<<"${control}"
  for target_bytes in 0 200000; do
    synthetic="FAIL  owned-text-file-set: git ls-files exited 128"
    [[ "${markers}" == "neither" ]] && synthetic="MMT-4001 owned text policy failed for an unrelated reason"
    # Filler first, marker last: `grep -q` must read the whole stream to answer, which is
    # the arrangement under which the discarded-violation half of the defect appears.
    while [[ "${#synthetic}" -lt "${target_bytes}" ]]; do
      synthetic+=$'\n'"${OWNED_TEXT_FILLER}"
    done
    [[ "${markers}" == "vacuous-ok" ]] \
      && synthetic+=$'\n''ok    owned-text-rules: every owned text file satisfies the rules'

    mapfile -t control_problems < <(unobtained_set_problems "${synthetic}" 4)
    joined="${control_problems[*]-}"

    control_problem_count="${#control_problems[@]}"
    if [[ "${control_problem_count}" -eq "${expected_count}" ]] \
        && { [[ -z "${must_contain}" ]] || [[ "${joined}" == *"${must_contain}"* ]]; }; then
      control_pass "§ 6b control (${#synthetic} bytes of output): ${label} -> ${control_problem_count} problem(s), as required"
    else
      control_fail "§ 6b control (${#synthetic} bytes of output): ${label} -> ${control_problem_count} problem(s) [${joined}], expected ${expected_count}${must_contain:+ including '${must_contain}'}"
    fi
  done
done

# This gate runs negative controls in band, so its log contains failure-shaped text on a
# green run. Prove the marking that separates that text from genuine findings still holds.
gate_assert_marking

gate_summary "verify-format" "${EXIT_VALIDATION}"
