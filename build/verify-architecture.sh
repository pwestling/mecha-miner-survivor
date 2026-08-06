#!/usr/bin/env bash
#
# Asserts the accepted repository layout and the accepted project boundary.
#
# Authority: docs/technical/115-component-contract-and-schema-registry.md
#              § Accepted project boundary
#            docs/technical/100-build-dependencies-and-release-operations.md
#              § Repository structure
#            docs/technical/00-technical-foundation.md § Language boundary
# Requirements: TR-CTR-001, TR-BLD-006, TR-FND-001, TR-FND-002
# Verification: VER-FND-001-003, VER-FND-001-004, VER-FND-001-005
#
# This is a real assertion, not a review aid: the reference graph is read from
# MSBuild's own evaluation of every project (so implicit SDK-injected package
# references are included), the Godot dependency is read from the committed
# NuGet lock files, and every mismatch exits nonzero.
#
# TASK-FND-009-001 added real architecture tests in tests/MechaMiner.Tools.Tests, with
# one negative control per forbidden edge. This script deliberately stays: CI and the
# build verb both call it, and it reads MSBuild's own evaluation of every project, which
# catches an SDK-injected package reference that no project file mentions. The two gates
# are independent on purpose - neither consumes the other's output - so one reader's
# defect cannot hide from both.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 4 validation failure.

set -euo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly EXIT_VALIDATION=4

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

# --- Accepted layout (doc 100 § Repository structure) ------------------------

readonly EXPECTED_PATHS=(
  "MechaMiner.sln"
  "global.json"
  "Directory.Build.props"
  "Directory.Packages.props"
  # doc 100 § Repository structure lists both root wrappers in the accepted tree.
  # They exist from FND-002 onward and are the only workflow entrypoint.
  "build.sh"
  "build.ps1"
  # doc 100 § Continuous integration requires a pull-request job; FND-005 is that
  # job and this is the only file that is it. Deleting or renaming the workflow
  # un-gates every gate at once, silently and with no red build anywhere, which is
  # the one failure mode no gate inside the workflow can catch.
  #
  # Listing it here tests the path and nothing else, which is less than an earlier
  # version of this comment claimed. § 8 is where the workflow's content is asserted;
  # this entry only reports the name when the file is gone.
  ".github/workflows/fast.yml"
  "game/project.godot"
  "game/MechaMiner.Game.csproj"
  "game/scenes"
  "game/shaders"
  "game/presentation"
  "src/MechaMiner.Simulation"
  "src/MechaMiner.Content"
  "src/MechaMiner.Diagnostics"
  "src/MechaMiner.Persistence"
  "src/MechaMiner.Tools"
  "tests/MechaMiner.Simulation.Tests"
  "tests/MechaMiner.Content.Tests"
  "tests/MechaMiner.Diagnostics.Tests"
  "tests/MechaMiner.Persistence.Tests"
  "tests/MechaMiner.Tools.Tests"
  "tests/MechaMiner.Game.Tests"
  "tests/verification"
  "content"
  # doc 40 § Accepted content repository layout. content/schemas was in that layout and
  # simply missing from this gate; content/player was added by the same doc change that
  # gave the shared player baseline somewhere to live, because a mech definition carries
  # Hull/Armor/Recovery/movement/footprint *overrides* and the overridden values are not
  # mech data. Both carry a .gitkeep, the way FND-001 seeded every other empty accepted
  # directory, so adding the path does not add a failure.
  "content/schemas"
  "content/player"
  "assets-source"
  "assets-runtime"
  "assets-manifest"
  "generated"
  "docs"
  "build"
)

# --- Accepted project boundary (doc 115) -------------------------------------
# "<project>|<comma separated allowed project references>|<godot: yes|no>"
# The reference list is exact: a project may not have an edge that is not listed.

readonly EXPECTED_PROJECTS=(
  "src/MechaMiner.Content/MechaMiner.Content.csproj||no"
  "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj||no"
  "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj|MechaMiner.Content|no"
  "src/MechaMiner.Tools/MechaMiner.Tools.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj|MechaMiner.Content|no"
  "tests/MechaMiner.Diagnostics.Tests/MechaMiner.Diagnostics.Tests.csproj|MechaMiner.Diagnostics|no"
  "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj|MechaMiner.Simulation|no"
  "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj|MechaMiner.Persistence|no"
  "tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj|MechaMiner.Tools|no"
  "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|no"
  "game/MechaMiner.Game.csproj|MechaMiner.Content,MechaMiner.Diagnostics,MechaMiner.Persistence,MechaMiner.Simulation|yes"
)

# The one value that can never be a legitimate item identity, printed when MSBuild could
# not be asked. Without it, a failed evaluation is indistinguishable from a project that
# genuinely declares nothing: the exit status would be discarded by the pipeline, every
# comparison would run against an empty set, and a project with a forbidden edge would be
# reported as compliant. Emitting a value that matches no accepted set instead turns a
# discarded status into a visible failure.
readonly EVALUATION_FAILED="MSBUILD-EVALUATION-FAILED"

msbuild_items() {
  # $1 project, $2 item name. Prints one Identity per line, sorted.
  local document status
  document="$(dotnet msbuild "${REPO_ROOT}/$1" -nologo "-getItem:$2" 2>/dev/null)"
  status=$?
  if [[ "${status}" -ne 0 || -z "${document}" ]]; then
    printf '%s\n' "${EVALUATION_FAILED}"
    return 0
  fi

  if ! printf '%s' "${document}" | python3 -c '
import json, sys
document = json.load(sys.stdin)
for identity in sorted(item["Identity"] for item in document.get("Items", {}).get(sys.argv[1], [])):
    if identity.strip():
        sys.stdout.write(identity + "\n")
' "$2"; then
    printf '%s\n' "${EVALUATION_FAILED}"
  fi
}

project_name() {
  local base="${1##*[/\\]}"
  printf '%s' "${base%.csproj}"
}

# --- The two comparisons, as functions -------------------------------------------------
#
# Extracted so the negative controls in section 8 run the SAME comparison the real
# projects run, rather than a second implementation of it. A control that reimplemented
# the comparison would only prove the control works.

# $1 project path, $2 accepted comma-separated reference set.
# Sets EDGES_ACTUAL. Returns 0 when the evaluated set equals the accepted set.
EDGES_ACTUAL=""
edges_match() {
  EDGES_ACTUAL="$(msbuild_items "$1" ProjectReference \
    | sed -E 's|.*[/\\]||; s|\.csproj$||' | sort | paste -sd, -)"
  [[ "${EDGES_ACTUAL}" == "$2" ]]
}

# $1 project path, $2 "yes" when doc 115 allows the engine dependency.
# Sets GODOT_EVALUATED, GODOT_LOCKED, and GODOT_UNPROVED. Returns 0 when the project's
# engine dependency matches what doc 115 allows for it.
#
# GODOT_UNPROVED is the third outcome, and it is deliberately not folded into the
# mismatch return. The evaluated package list is obtained first and checked on its own
# before it is filtered, because a failed MSBuild evaluation yields an empty list and an
# empty list is exactly what a project with no Godot dependency looks like - every "must
# not reference Godot" row would otherwise pass without anything having been evaluated.
# Two independent mechanisms cover that, and both are kept on purpose:
#
#   - msbuild_items emits EVALUATION_FAILED rather than nothing, so a discarded exit
#     status still produces a value that matches no accepted set. This also covers a
#     zero-exit-but-empty document and a python parse failure, and it protects § 3.
#   - the sentinel is then detected here by name and reported as "unproved" rather than
#     as a Godot verdict, so § 4 says the boundary was not tested instead of implying it
#     was tested and passed.
#
# Only after the list is known good is it filtered for Godot. At that point grep's exit 1
# genuinely means "no Godot package", which is the outcome this row is testing for, so the
# `|| true` there is not masking anything.
GODOT_EVALUATED=""
GODOT_LOCKED=""
GODOT_UNPROVED="no"
godot_matches() {
  local project="$1" allowed="$2"
  local directory="${REPO_ROOT}/$(dirname "${project}")"
  local lock_file="${directory}/packages.lock.json"
  local evaluated_packages package_probe_status=0

  GODOT_UNPROVED="no"

  # msbuild_items returns 0 and reports failure in-band via the sentinel, but its status is
  # still checked: if a future edit gives it a nonzero exit, that must fail the gate rather
  # than silently become an empty package list again.
  evaluated_packages="$(msbuild_items "${project}" PackageReference)" || package_probe_status=$?

  # The sentinel is matched deliberately: a project whose items could not be evaluated must
  # not read as "has no Godot dependency".
  GODOT_EVALUATED="$(printf '%s\n' "${evaluated_packages}" \
    | grep -iE "^(Godot|${EVALUATION_FAILED})" || true)"

  if [[ "${package_probe_status}" -ne 0 ]]; then
    GODOT_UNPROVED="yes"
    GODOT_EVALUATED="msbuild_items exited ${package_probe_status}"
    return 2
  fi
  if [[ "${GODOT_EVALUATED}" == *"${EVALUATION_FAILED}"* ]]; then
    GODOT_UNPROVED="yes"
    return 2
  fi

  GODOT_LOCKED=""
  if [[ -f "${lock_file}" ]]; then
    GODOT_LOCKED="$(grep -oE '"Godot[A-Za-z.]*"' "${lock_file}" | sort -u | tr -d '"' | paste -sd, - || true)"
  fi

  if [[ "${allowed}" == "yes" ]]; then
    [[ -n "${GODOT_EVALUATED}" && "${GODOT_LOCKED}" == *GodotSharp* ]]
  else
    [[ -z "${GODOT_EVALUATED}" && -z "${GODOT_LOCKED}" ]]
  fi
}

# Reads one field of an EXPECTED_PROJECTS row, and fails loudly when the row is absent.
# A control that silently tested nothing because a path was renamed would be worse than
# no control, so the lookup is required to succeed.
accepted_field() {
  local wanted="$1" field="$2" entry project refs godot
  for entry in "${EXPECTED_PROJECTS[@]}"; do
    IFS='|' read -r project refs godot <<<"${entry}"
    if [[ "${project}" == "${wanted}" ]]; then
      case "${field}" in
        refs) printf '%s' "${refs}" ;;
        godot) printf '%s' "${godot}" ;;
      esac
      return 0
    fi
  done
  return 1
}

section "1. accepted repository layout (VER-FND-001-003)"
for path in "${EXPECTED_PATHS[@]}"; do
  if [[ -e "${REPO_ROOT}/${path}" ]]; then
    pass "exists: ${path}"
  else
    fail "missing prescribed path: ${path}"
  fi
done

section "2. solution contains exactly the accepted projects (VER-FND-001-003)"
actual_solution="$(cd "${REPO_ROOT}" && dotnet sln MechaMiner.sln list \
  | grep -E '\.csproj$' | tr '\\' '/' | sort)"
expected_solution="$(printf '%s\n' "${EXPECTED_PROJECTS[@]}" | cut -d'|' -f1 | sort)"
if [[ "${actual_solution}" == "${expected_solution}" ]]; then
  pass "MechaMiner.sln references exactly the ${#EXPECTED_PROJECTS[@]} accepted projects"
else
  fail "MechaMiner.sln project set differs from the accepted decomposition"
  diff <(printf '%s\n' "${expected_solution}") <(printf '%s\n' "${actual_solution}") || true
fi

section "3. project reference edges match the accepted boundary (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project expected_refs _godot <<<"${entry}"
  if edges_match "${project}" "${expected_refs}"; then
    pass "$(project_name "${project}") -> [${expected_refs}]"
  else
    fail "$(project_name "${project}") references [${EDGES_ACTUAL}], accepted set is [${expected_refs}]"
  fi
done

section "4. only game/ may reference Godot (VER-FND-001-004)"
for entry in "${EXPECTED_PROJECTS[@]}"; do
  IFS='|' read -r project _expected_refs godot_allowed <<<"${entry}"
  # From the base branch, kept verbatim in substance because this branch did not have it:
  # the lock file is half of this row's evidence, and an absent one used to be skipped.
  # `godot_locked` stayed empty, the row was decided on the MSBuild half alone, and the
  # "must not reference Godot" branch still printed `ok`. Deleting a project's
  # packages.lock.json therefore removed an assertion without failing anything, which is
  # Decision 11 rule 2 - an empty candidate set never satisfies a gate - in the mild form.
  # Every one of the accepted projects has a committed lock file today, so this holds in
  # fact; asserting it makes it hold by construction. verify-configurations.sh § 4 carries
  # the sharper set-level form; this row is the per-project half.
  #
  # It is here in the loop and NOT inside godot_matches() on purpose: § 8's fixture
  # projects under build/policy-fixtures/architecture/ carry no lock file, and they must
  # still be able to drive the same comparison the real projects drive.
  if [[ ! -f "${REPO_ROOT}/$(dirname "${project}")/packages.lock.json" ]]; then
    fail "$(project_name "${project}"): $(dirname "${project}")/packages.lock.json is absent, so the locked half of the Godot boundary was not read; an unread half is not a satisfied one"
    continue
  fi

  if godot_matches "${project}" "${godot_allowed}"; then
    if [[ "${godot_allowed}" == "yes" ]]; then
      pass "$(project_name "${project}") references Godot as accepted (locked: ${GODOT_LOCKED})"
    else
      pass "$(project_name "${project}") has no Godot dependency"
    fi
  elif [[ "${GODOT_UNPROVED}" == "yes" ]]; then
    # The evaluation itself did not happen, which is a third outcome and not a verdict on
    # this row either way. Reported before the accepted/forbidden branches below so that a
    # project whose items could not be read is never described as "must not reference
    # Godot ... (evaluated: none)" - "none" would read as an answer, and there was none.
    fail "$(project_name "${project}"): PackageReference evaluation failed (${GODOT_EVALUATED}); the Godot boundary is unproved for this project, which is not the same as satisfied"
    continue
  elif [[ "${godot_allowed}" == "yes" ]]; then
    fail "$(project_name "${project}") must reference Godot but no Godot package is evaluated or locked"
  else
    fail "$(project_name "${project}") must not reference Godot (evaluated: ${GODOT_EVALUATED:-none}, locked: ${GODOT_LOCKED:-none})"
  fi
done

# Runs a recursive grep whose absence-of-match is the passing outcome, and distinguishes
# grep's three exit classes instead of collapsing two of them.
#
#   0  matches found        -> the prohibition is violated
#   1  no matches           -> the prohibition holds
#   2+ grep itself failed   -> nothing was searched, so the prohibition is UNPROVED
#
# `grep ... 2>/dev/null || true` conflated 1 and 2+: a missing search root, an unreadable
# tree, or any other grep error produced an empty result, and the empty result took the
# pass branch. That is the same defect as the GDScript check below, in two more places.
# Also asserts that the search roots exist, because grep over a nonexistent directory is
# an error the gate must not absorb.
assert_absent_pattern() {
  local description="$1"
  local pattern="$2"
  local include="$3"
  shift 3
  local roots=("$@")

  local root
  for root in "${roots[@]}"; do
    if [[ ! -d "${REPO_ROOT}/${root}" ]]; then
      fail "${description}: search root '${root}' does not exist, so this prohibition was not searched for"
      return
    fi
  done

  local matches
  local grep_status=0
  matches="$(cd "${REPO_ROOT}" && grep -rlE "${pattern}" --include="${include}" "${roots[@]}" 2>&1)" \
    || grep_status=$?

  if [[ "${grep_status}" -eq 0 ]]; then
    fail "${description}: ${matches}"
  elif [[ "${grep_status}" -eq 1 ]]; then
    pass "${description}: no match in ${roots[*]}"
  else
    fail "${description}: grep exited ${grep_status}, so nothing was searched and the rule is unproved: ${matches}"
  fi
}

section "5. no pure project references MechaMiner.Game (VER-FND-001-004)"
assert_absent_pattern \
  "no project references MechaMiner.Game" \
  'ProjectReference[^>]*MechaMiner\.Game\.csproj' \
  '*.csproj' \
  src tests game

section "6. no Godot types outside game/ (VER-FND-001-004)"
#
# The Godot namespace as a token in any position, not only after `using`.
#
# The previous expression was `(^|[^A-Za-z.])using[[:space:]]+Godot([;.]|$)`, which
# required the literal token `using` followed by whitespace followed by `Godot`. It
# caught `using Godot;` and `global using Godot;` and missed `using static Godot.GD;`,
# `using GD = Godot.GD;`, `using GodotAlias = Godot;` and a fully-qualified
# `Godot.GD.Print` with no import at all.
#
# WHAT THIS SCAN IS NOT: A COMPLETE ACCOUNT OF THE WAYS C# CAN NAME A NAMESPACE. This
# comment used to call those five imports plus the qualified form "the six ways C# offers
# of naming a namespace". That is a completeness claim, and it is false. Measured
# counterexamples, each a real reference that resolves to `Godot`, none of them caught
# here:
#
#   - a reference split across two physical lines: `global using` newline `    Godot;`,
#     `using` newline `    Godot;`, and `Godot` newline `    .GD.Print("y");`. Both
#     readers decide per line, so neither sees any of the three. `using static` newline
#     `    Godot.GD;` IS caught, because its second line still contains `Godot.` - a
#     line-splitting hole with a narrow, non-obvious edge, which is the shape a
#     hand-written scan produces.
#   - an identifier written with a Unicode escape: `using \u0047odot;` and
#     `\u0047odot.GD.Print()` both bind to `Godot`, and after stripping the text still
#     reads `\u0047odot`.
#
# So this scan covers the naming forms its corpus names and no more. The corpus below
# records the uncovered ones as the `k*` class - k1 `global using` / `    Godot;`, k2
# `using` / `    Godot;`, k3 `Godot` / `    .GD.Print("y");`, k4 `using \u0047odot;`, k5
# `\u0047odot.GD.Print("x");` - asserted as missed, so the gap is measured rather than
# merely admitted.
#
# CLOSING THEM IS RULED OUT, and this is the ruling rather than a deferral. It needs a
# parser: read the reference graph from a C# syntax tree (Microsoft.CodeAnalysis.CSharp)
# and comments, literals, line splits and identifier escapes stop being cases at all. Four
# independent constraints forbid adding it - doc 114 § Default mandate withholds agent
# autonomy for a new third-party dependency, doc 114 § Explicit escalation boundary makes a
# new foundational dependency human-only, doc 100 § Dependency policy requires a recorded
# dependency request and this repository has no dependency ledger, and
# Directory.Packages.props says in as many words not to add a package for build
# convenience. The five stay open, named, and asserted. ProjectGraph.NamesGodotNamespace
# carries the same list and the same ruling.
#
# DO NOT CLOSE THEM BY ADDING ANOTHER SUBSTITUTION PASS, and do not hand-roll a tokenizer
# either. Both of the two most recent defects in this scan were introduced by adding a
# pass: one closed the character-literal hole and opened a commoner apostrophe-prose hole,
# the other rewrote a safe `grep -lE` into a `printf | grep -q` pipe whose SIGPIPE status
# reported a real violation as `ok`.
#
# GODOT_TOKEN below is character-for-character the same expression as
# ProjectGraph.GodotNamespaceToken in src/MechaMiner.Tools/Audit/ProjectGraph.cs. The two
# are two readers of one rule, and the design intent is real: a rule enforced in one place
# can be changed in one place and silently diverge from the documented boundary. But be
# accurate about what the pair currently buys, because the previous wording oversold it.
# Over the 46-file corpus described below the two readers DISAGREE on nine files - the C#
# reader catches four positives this one misses, and clears four lookalikes this one
# flags. This is not two views of one rule; it is one precise reader and one approximate
# one. The approximate one keeps the property that matters for a prohibition, in that a
# violation it cannot see is one the C# reader can - but it is not authoritative, and
# § 6's `ok` is not an independent confirmation of test-fast's. ONE exact reader would
# serve the single-owner intent better than two inexact ones that disagree; the escalation
# above is why there is not one yet. Until there is, if the expression changes, change it
# in both files and keep both controls green.
#
# Matching `Godot[.]` alone would not be sufficient: `using GodotAlias = Godot;` has
# no dot after the token. The trailing class is therefore "any non-identifier
# character or end of line", and the leading class excludes `.` and identifier
# characters so `MechaMiner.GodotLike` and `NotGodotish` do not match.
readonly GODOT_TOKEN='(^|[^A-Za-z0-9_.])Godot([^A-Za-z0-9_]|$)'

# Where the token may legitimately appear if the file names the namespace: on a
# `using` directive line (which covers all five import spellings), or in
# namespace-qualifier position anywhere (which covers the fully-qualified form).
readonly GODOT_USING_LINE='^[[:space:]]*(global[[:space:]]+)?using[[:space:]]'
readonly GODOT_QUALIFIER='(^|[^A-Za-z0-9_.])Godot[[:space:]]*\.'

# Comments and string literals are removed before the scan, for the same reason the
# C# reader removes them: 96 lines under src/ and tests/ spell `Godot` as a bare
# English word in a comment or a diagnostic message, and most of them are prose
# explaining why this boundary exists. Rewording those to satisfy a text scan would
# make the code worse and prove nothing about the boundary.
#
# WHAT THIS STRIPPER IS AND IS NOT. Sequential regular-expression substitutions cannot
# decide C# lexical context: each pass is blind to what the others did, so a construct
# only one of them understands can be cut in half by another. It therefore errs in BOTH
# directions, and the previously stated claim that its "error direction is a false
# negative ... never a false accusation" was measured false. Over a 46-file corpus, each
# member compiled against a Godot shim so every positive is a resolvable reference and
# every lookalike genuinely is not, this stripper causes four false accusations that the
# C# reader correctly clears:
#
#   - a single-line block comment, `/* Godot.GD is engine-only */`;
#   - the same text inside a multi-line block comment;
#   - a multi-line `@"` string containing `Godot.GD.Print(x);`; and
#   - a multi-line `"""` raw string containing `Godot.`.
#
# `sed` strips `//` to end of line and never `/* */` in any form, and it has no state
# across lines, so a literal that spans lines is not tracked. The block-comment cases are
# not covered by any "constructs spanning lines" caveat: the first is on one line. Those
# four are the reason this pass must not be the authority on this rule; they are recorded
# in the corpus below as `k*` probes, which assert the CURRENT verdict rather than the
# correct one so that a change here shows up rather than passing unremarked.
#
# The three substitutions run in this order for three separate reasons, two of which
# were holes:
#
#   1. Character literals go first, and the content is exactly one character or one
#      escape - `'([^'\\]|\\.)'` and not `'([^'\\]|\\.)*'`. Without this pass at all,
#      the `"` inside `'"'` was read as a string opener, so the scan ran on to the next
#      `"` and deleted everything between - which on `char q = '"'; Godot.GD.Print("y");`
#      is the reference itself. But the unbounded `*` form that closed that hole opened a
#      commoner one: run before the string pass, it happily pairs apostrophes belonging to
#      two DIFFERENT string literals and deletes everything between them, so
#      `var s = "don't"; Godot.GD.Print("won't");` became `var s =  );` and
#      `/* it's */ Godot.GD.Print("won't");` became `/* it t");`. English prose in
#      diagnostic strings is the commonest shape in this repository, and both of those
#      lines are references this gate reported `ok` on. A C# character literal holds one
#      character or one escape, so requiring exactly that costs nothing and cannot span
#      two literals. This is a bound on the damage the pass can do, not a claim that
#      substitution passes can decide lexical context - they cannot, and the false
#      accusations above are the standing proof.
#   2. Strings go before comments. The previous order deleted from the first `//` to end
#      of line first, so `string u = "http://example.invalid"; Godot.GD.Print(x);` lost
#      its reference to a `//` that was inside a literal. A URL beside a Godot call is
#      ordinary code, and the C# reader already got this case right.
#   3. Comments go last, over what is left, so a `//` outside any literal still takes
#      the rest of its line.
#
# Each removal leaves a space rather than nothing, so deleting a literal cannot splice
# two identifiers into a token nobody wrote.
godot_strip_comments_and_strings() {
  sed -E \
    -e "s/'([^'\\\\]|\\\\.)'/ /g" \
    -e 's/@?"([^"\\]|\\.)*"/ /g' \
    -e 's,//.*,,' \
    "$1"
}

# Prints the files under the given roots that name the Godot namespace.
#
# NEVER PUT AN EARLY-EXITING READER ON THE RIGHT OF A PIPE IN THIS FILE. `grep -q` and
# `head` stop reading the moment they have their answer; the writer on the left then dies
# of SIGPIPE, and under `set -o pipefail` the pipeline's status is 141. Inside an `if`
# condition 141 reads as "no match", so the file is not emitted, `stray_godot` comes back
# empty and § 6 prints `ok` over a real violation.
#
# That was live at 4859a90, in both branches below, written as
# `printf '%s\n' "${stripped}" | grep -qE ...`, and its miss rate rises with file size,
# because the writer only dies once it has more to write than the 64 KiB pipe buffer
# holds. Measured against a file that names `Godot.GD` early with the bulk after it:
#
#     88 B (the size the f1-f9 probe fixtures used to be)     0 of 100 missed
#     26 KB                                                   0 of 100 missed
#     84 KB                                                  78 of 100 missed
#    371 KB                                                 100 of 100 missed
#
# and through the using-line branch: 13 of 100 at 21 KB, 49 of 100 at 70 KB, 100 of 100
# at 306 KB. Every probe fixture was a single short line, so the control could not fail:
# the stripped text fitted inside the pipe buffer, the writer never blocked, and no
# SIGPIPE was reachable. A one-line fixture is not a control for this class of defect, it
# is a control that structurally cannot see it - which is why several rounds of attacking
# this scan did not find it. The corpus below therefore carries two production-sized
# probes, one per branch.
#
# Note the direction of the size dependency: a reference LATE in a large file survives,
# because grep has to read to the end before it can match and the writer finishes first.
# The broken case is an early reference in a large file, which is where `using` directives
# and a type's first statements actually live - so the failing shape is the ordinary one.
#
# The fix is a here-string, not a pipe: `<<<` has no left-hand process to signal. The
# using-line branch captures its intermediate result in a variable rather than piping
# grep into grep, so neither reader can be killed. This prohibition was safe when it was a
# `grep -lE` over file arguments; it broke when that was rewritten into a `printf | grep -q`
# pipe. Two rewrites of this scan have now introduced a false pass.
godot_namespace_hits() {
  local root file stripped using_lines
  for root in "$@"; do
    while IFS= read -r file; do
      stripped="$(godot_strip_comments_and_strings "${file}")"
      if grep -qE "${GODOT_QUALIFIER}" <<<"${stripped}"; then
        printf '%s\n' "${file}"
        continue
      fi

      # grep's exit 1 here means "this file declares no using directives", which is an
      # ordinary outcome rather than a failure, so this is the one place `|| true` is
      # right. The result is held in a variable so the token test below reads a
      # here-string instead of the far end of a pipe.
      using_lines="$(grep -E "${GODOT_USING_LINE}" <<<"${stripped}" || true)"
      if [[ -n "${using_lines}" ]] && grep -qE "${GODOT_TOKEN}" <<<"${using_lines}"; then
        printf '%s\n' "${file}"
      fi
    done < <(find "${root}" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print | sort)
  done
}

godot_scan_roots_present=1
for root in src tests; do
  if [[ ! -d "${REPO_ROOT}/${root}" ]]; then
    # A moved scan root would make the search cover nothing, and an empty search is
    # not a satisfied prohibition.
    fail "Godot source scan root is missing: ${root}/, so this prohibition was not searched for"
    godot_scan_roots_present=0
  fi
done

# THIS PASS IS A SCREEN, AND ITS OUTPUT ABOUT src/ AND tests/ IS NOT A VERDICT.
#
# It used to fail the gate on its own reading, which made an approximate reader
# authoritative over a prohibition. Measured over the 46-file corpus below, it makes four
# false accusations the C# reader correctly clears - x1 a single-line
# `/* Godot.GD is engine-only */`, x2 the same in a multi-line block comment, x3 a
# multi-line `@"` string containing `Godot.GD.Print(x);`, x4 a multi-line `"""` string
# containing `Godot.` - because its `sed` strips `//` and never `/* */` in any form and has
# no state across lines. Any of those four is ordinary prose about this very boundary, and
# writing one would have turned `./build.sh build` red over code that is correct.
#
# THE AUTHORITATIVE READER IS ArchitectureRuleTests.TheGodotImportRuleCatchesEveryWayOfNaming-
# TheNamespace, over ProjectGraph.NamesGodotNamespace, and it runs in test-fast. On the same
# corpus it catches the same 31 of 36 references this screen catches and makes ZERO false
# accusations. Nothing is lost by not deciding here: every reference this screen can see, the
# C# reader can see too - its five escapes are a subset of this one's, verified probe by
# probe - so the repository-level guarantee is unchanged while four ways to fail the build
# over correct code are removed. CI runs both verbs, so both readers still run on every pull
# request.
#
# What that costs, stated rather than implied: `./build.sh build` alone no longer enforces
# the Godot prohibition. A `using Godot;` added under src/ is reported here as a candidate
# and does not fail this gate; test-fast fails on it. If you want the prohibition inside
# `build`, the way to get it is the C# reader, not this one.
#
# The control below is a different matter and DOES fail the gate. It asserts what this
# screen does - which forms it catches, which lookalikes it clears, which references it
# cannot see, which non-references it accuses - and that is a claim about this script rather
# than about the repository, so drift in the screen is still a hard failure.
if [[ "${godot_scan_roots_present}" -eq 1 ]]; then
  stray_godot="$(cd "${REPO_ROOT}" && godot_namespace_hits src tests)"
  if [[ -z "${stray_godot}" ]]; then
    pass "screen: no C# file under src/ or tests/ names the Godot namespace by this scan's reading. Not a verdict - see the note above; ArchitectureRuleTests over ProjectGraph.NamesGodotNamespace decides this rule, in test-fast"
  else
    # Deliberately not `fail`. A candidate is a thing to look at, and this reader is known
    # to accuse four shapes of correct code.
    pass "screen: $(printf '%s' "${stray_godot}" | grep -c . || true) candidate(s) to look at, which this approximate reader believes name the Godot namespace: $(printf '%s' "${stray_godot}" | paste -sd' ' -). NOT A FINDING. This scan makes four known false accusations (see the x* class below); ArchitectureRuleTests in test-fast is what decides, and its verdict is the one to act on"
  fi
fi

# --- § 6 control corpus ----------------------------------------------------------------
#
# The scan above has only ever run against a compliant tree, so on its own it is
# indistinguishable from a scan that matches nothing. This corpus is the same 46 files
# ArchitectureRuleTests feeds the C# reader, in the same four classes, so a divergence
# between the two readers is a failing control rather than a silent disagreement. Every
# member has been compiled against a Godot shim: each `f*` and `k*` file fails to compile
# without the shim, so every positive is a reference that really resolves to `Godot`, and
# each `n*` and `x*` file compiles without it, so no lookalike is secretly a reference.
#
#   f*  a real reference this reader MUST catch.
#   n*  not a reference; this reader must NOT flag it.
#   k*  a real reference this reader CANNOT see. Asserted as missed, so the gap is
#       measured instead of unmeasured. If one starts being caught, this control fails and
#       tells you to move it to `f*` - a gap closing is a visible edit in the diff, which
#       is the same ratchet the audit-expectations census uses.
#   x*  not a reference, and this reader flags it anyway. Asserted as flagged, for the same
#       reason. The C# reader clears all four; see the header above.
#
# WHY THE PROBES ARE NOT ALL ONE SHORT LINE, which is what they used to be. Two whole
# classes of defect are invisible to a single-line fixture:
#
#   - a stripper defect that swallows the rest of a line cannot be seen by a probe whose
#     line holds nothing else. All six original spellings stayed green while
#     `char q = '"'; Godot.GD.Print("y");` passed the real scan. f07-f12 and f14-f17
#     are therefore same-line probes: a decoy the stripper has to get right, followed on
#     the same line by a reference it has to keep. None is contrived - a URL, a quote
#     character, or an apostrophe in English prose beside a Godot call is ordinary code.
#   - a SIGPIPE defect in the scan pipeline only fires once the writer has more to write
#     than the 64 KiB pipe buffer holds. Every fixture was ~75 bytes, so the writer never
#     blocked and the false pass documented on godot_namespace_hits was structurally
#     unreachable by this control. f30 and f31 are therefore PRODUCTION-SIZED - a few
#     hundred KiB each, one per scan branch, with the reference near the top and the bulk
#     after it, because that is the shape that breaks and it is also the shape a real
#     source file has. Do not shrink them: at fixture size they prove nothing.

readonly PROBE_AP="'"

godot_probe_dir="$(mktemp -d)"
godot_probe="${godot_probe_dir}/probe"
mkdir -p "${godot_probe}"

# $1 file name, $2.. the lines of the file.
probe_file() {
  local name="$1"
  shift
  printf '%s\n' "$@" >"${godot_probe}/${name}"
}

# --- f*: real references this reader must catch ---
probe_file 'f01-using.cs' \
  'using Godot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f02-global-using.cs' \
  'global using Godot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f03-using-static.cs' \
  'using static Godot.GD;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f04-alias-type.cs' \
  'using GD = Godot.GD;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f05-alias-namespace.cs' \
  'using GodotAlias = Godot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f06-fully-qualified.cs' \
  'internal static class P { internal static void R() {' \
  'Godot.GD.Print("x");' \
  '} }'
probe_file 'f07-char-literal-quote.cs' \
  'internal static class P { internal static void R() {' \
  "char q = ${PROBE_AP}\"${PROBE_AP}; Godot.GD.Print(q);" \
  '} }'
probe_file 'f08-comment-in-string.cs' \
  'internal static class P { internal static void R() {' \
  'string s = "// not a comment"; Godot.GD.Print(s);' \
  '} }'
probe_file 'f09-url-in-string.cs' \
  'internal static class P { internal static void R() {' \
  'string u = "http://example.invalid/x"; Godot.GD.Print(u);' \
  '} }'
probe_file 'f10-two-apostrophes.cs' \
  'internal static class P { internal static void R() {' \
  "string s = \"don${PROBE_AP}t\"; Godot.GD.Print(\"won${PROBE_AP}t\");" \
  '} }'
probe_file 'f11-apostrophe-in-block-comment.cs' \
  'internal static class P { internal static void R() {' \
  "/* it${PROBE_AP}s fine */ Godot.GD.Print(\"won${PROBE_AP}t\");" \
  '} }'
probe_file 'f12-apostrophe-in-line-comment.cs' \
  'internal static class P { internal static void R() {' \
  "// it${PROBE_AP}s fine" \
  "Godot.GD.Print(\"won${PROBE_AP}t\");" \
  '} }'
probe_file 'f13-linesplit-using-static.cs' \
  'using static' \
  '    Godot.GD;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f14-escaped-quote-in-string.cs' \
  'internal static class P { internal static void R() {' \
  'string s = "a\"b"; Godot.GD.Print(s);' \
  '} }'
probe_file 'f15-verbatim-doubled-quote.cs' \
  'internal static class P { internal static void R() {' \
  'string s = @"a""b"; Godot.GD.Print(s);' \
  '} }'
probe_file 'f16-raw-string.cs' \
  'internal static class P { internal static void R() {' \
  'string s = """x"""; Godot.GD.Print(s);' \
  '} }'
probe_file 'f17-interpolated-apostrophe.cs' \
  'internal static class P { internal static void R() {' \
  "int x = 1; string s = $\"it${PROBE_AP}s {x}\"; Godot.GD.Print(s);" \
  '} }'
probe_file 'f18-attribute.cs' \
  '[Godot.GlobalClass]' \
  'internal sealed class P { internal int R() => 1; }'
probe_file 'f19-typeof.cs' \
  'internal static class P { internal static void R() {' \
  'System.Type t = typeof(Godot.Node); Godot.GD.Print(t);' \
  '} }'
probe_file 'f20-field-type.cs' \
  'internal sealed class P { private Godot.Node? _n; internal object? R() => _n; }'
probe_file 'f21-base-type.cs' \
  'internal sealed class P : Godot.Node { internal int R() => 1; }'
probe_file 'f22-using-subnamespace.cs' \
  'using Godot.Collections;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f23-generic-argument.cs' \
  'internal static class P { internal static void R() {' \
  'var l = new System.Collections.Generic.List<Godot.Node>(); Godot.GD.Print(l.Count);' \
  '} }'
probe_file 'f24-space-before-semicolon.cs' \
  'using Godot ;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'f25-space-around-dot.cs' \
  'internal static class P { internal static void R() {' \
  'Godot . GD.Print("x");' \
  '} }'
probe_file 'f26-block-comment-close-then-reference.cs' \
  'internal static class P { internal static void R() {' \
  '/* a comment that' \
  '   spans lines and ends here */ Godot.GD.Print("y");' \
  '} }'
probe_file 'f27-escaped-apostrophe-char.cs' \
  'internal static class P { internal static void R() {' \
  "char q = ${PROBE_AP}\\${PROBE_AP}${PROBE_AP}; Godot.GD.Print(q);" \
  '} }'
probe_file 'f28-conditional-compilation.cs' \
  'internal static class P { internal static void R() {' \
  '#if DEBUG' \
  'Godot.GD.Print("d");' \
  '#else' \
  'Godot.GD.Print("r");' \
  '#endif' \
  '} }'
probe_file 'f29-nested-namespace-type.cs' \
  'internal static class P { internal static void R() {' \
  'var a = new Godot.Collections.Array(); Godot.GD.Print(a.Count);' \
  '} }'

# f30/f31: production-sized, one per branch of godot_namespace_hits, reference first and
# bulk after. See the SIGPIPE note on that function for the measured miss rates by size.
{
  printf '%s\n' 'internal static class P { internal static void R() {' 'Godot.GD.Print("early");'
  seq 1 12000 | sed 's/.*/    System.GC.KeepAlive(&);/'
  printf '%s\n' '} }'
} >"${godot_probe}/f30-production-size-qualifier.cs"
{
  printf '%s\n' 'using Godot;'
  seq 1 14000 | sed 's/.*/using Filler.N&;/'
  printf '%s\n' 'internal static class P { internal static int R() => 1; }'
} >"${godot_probe}/f31-production-size-using.cs"

# --- n*: not references; must not be flagged ---
probe_file 'n1-qualified-lookalike.cs' \
  'using MechaMiner.GodotLike;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'n2-embedded-token.cs' \
  'internal static class N { internal static string R() => "NotGodotish"; }'
probe_file 'n3-bare-lookalike.cs' \
  'using MechaMiner.GodotLike;' \
  'internal static class N { internal static void R() => GodotLike.Do(); }'
probe_file 'n4-member-named-godot.cs' \
  '// Only game/ may reference Godot.' \
  'internal sealed class N { public int Godot { get; set; } }'
probe_file 'n5-comment-and-diagnostic.cs' \
  '// Only game/ may reference Godot, which is why this project does not.' \
  'internal static class N { internal static string R() => "launched no Godot process"; }'
probe_file 'n6-qualifier-in-string.cs' \
  'internal static class N { internal static string R() => "call Godot.GD.Print here"; }'

# --- k*: real references beyond this reader, asserted as missed ---
probe_file 'k1-linesplit-global-using.cs' \
  'global using' \
  '    Godot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'k2-linesplit-using.cs' \
  'using' \
  '    Godot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'k3-linesplit-qualifier.cs' \
  'internal static class P { internal static void R() {' \
  'Godot' \
  '    .GD.Print("y");' \
  '} }'
probe_file 'k4-unicode-escape-using.cs' \
  'using \u0047odot;' \
  'internal static class P { internal static int R() => 1; }'
probe_file 'k5-unicode-escape-qualifier.cs' \
  'internal static class P { internal static void R() {' \
  '\u0047odot.GD.Print("x");' \
  '} }'

# --- x*: non-references this reader accuses, asserted as flagged ---
probe_file 'x1-block-comment-one-line.cs' \
  'internal static class N {' \
  '    /* Godot.GD is engine-only, so nothing here calls it. */' \
  '    internal static int R() => 1; }'
probe_file 'x2-block-comment-multi-line.cs' \
  'internal static class N {' \
  '    /*' \
  '     * Godot.GD is engine-only.' \
  '     * game/ owns every call to Godot.GD.Print.' \
  '     */' \
  '    internal static int R() => 1; }'
probe_file 'x3-verbatim-string-multi-line.cs' \
  'internal static class N {' \
  '    private const string S = @"' \
  'Godot.GD.Print(x);' \
  '";' \
  '    internal static string R() => S; }'
probe_file 'x4-raw-string-multi-line.cs' \
  'internal static class N {' \
  '    private const string S = """' \
  '        Godot.GD.Print(x);' \
  '        """;' \
  '    internal static string R() => S; }'

godot_probe_hits="$(godot_namespace_hits "${godot_probe}")"

# Exact whole-line match through a here-string. Not `printf ... | grep -q`: that is the
# pipeline whose SIGPIPE status silently reported caught probes as missed inside the very
# message that listed them.
probe_hit() {
  grep -qxF "${godot_probe}/$1" <<<"${godot_probe_hits}"
}

# The four classes, asserted per file rather than by a count. A count can agree with a
# corpus that lost a member; a per-file assertion cannot.
probe_missed=()
probe_falsely_flagged=()
probe_gap_closed=()
probe_accusation_gone=()
for probe_name in $(cd "${godot_probe}" && ls *.cs | sort); do
  case "${probe_name}" in
    f*) probe_hit "${probe_name}" || probe_missed+=("${probe_name}") ;;
    n*) ! probe_hit "${probe_name}" || probe_falsely_flagged+=("${probe_name}") ;;
    k*) ! probe_hit "${probe_name}" || probe_gap_closed+=("${probe_name}") ;;
    x*) probe_hit "${probe_name}" || probe_accusation_gone+=("${probe_name}") ;;
  esac
done

probe_count() {
  # $1 class prefix. Counts files, not hits.
  local n
  n="$(cd "${godot_probe}" && ls -1 "$1"*.cs)"
  grep -c . <<<"${n}"
}

if [[ "${#probe_missed[@]}" -ne 0 ]]; then
  fail "control: the Godot scan missed real references it must catch: ${probe_missed[*]}"
elif [[ "${#probe_falsely_flagged[@]}" -ne 0 ]]; then
  fail "control: the Godot scan flagged ${probe_falsely_flagged[*]}, which do not name the Godot namespace"
elif [[ "${#probe_gap_closed[@]}" -ne 0 ]]; then
  fail "control: ${probe_gap_closed[*]} is recorded as beyond this reader and was caught; that is an improvement, so move it from the k* class to f* in this file AND in ArchitectureRuleTests, and update the two readers' scores in both headers"
elif [[ "${#probe_accusation_gone[@]}" -ne 0 ]]; then
  fail "control: ${probe_accusation_gone[*]} is recorded as a false accusation this reader makes and it no longer makes it; that is an improvement, so move it from the x* class to n* in this file AND in ArchitectureRuleTests"
else
  # Deliberately not "every way C# can name the namespace". The k* class is the measured
  # list of ways it cannot, and the x* class is the measured list of non-references it
  # accuses; both are stated in the header with why.
  pass "control: $(probe_count f) real references are caught (including two production-sized files, several hundred KiB each, one per scan branch - at fixture size the pipeline defect this scan carried was unreachable), $(probe_count n) lookalikes are cleared, $(probe_count k) references are recorded as beyond a text scan and confirmed missed, and $(probe_count x) non-references are recorded as accused by this reader and cleared by the C# one"
fi

rm -rf "${godot_probe_dir}"

section "7. no GDScript in the repository (VER-FND-001-005)"
#
# "No production GDScript" is one of AGENTS.md § Nonnegotiable architecture's hard
# prohibitions (TR-FND-002), and this gate could not enforce it. Two separate defects,
# both of which made a violation pass:
#
#   - `|| true` discarded git's exit status, so a git failure produced an empty
#     result, and the empty result took the "pass" branch. Under a broken or absent
#     git the prohibition was silently unenforceable.
#   - only tracked paths were consulted, so an untracked .gd file passed - and
#     untracked is precisely the state a newly written file is in.
#
# The candidate set is now tracked plus untracked-but-not-ignored, which is the same
# set format/format-check inspects, and a nonzero git status fails the gate instead of
# emptying it. Ignored paths stay out of scope on purpose: game/.godot/ is an engine
# cache, and a gitignored file is not production content.
#
# The probe is a function so that the negative controls below can drive the identical
# predicate. A gate asserted only against a clean tree proves nothing about its ability
# to fail.

# Emits a verdict word on line 1 - clean, violation, or unreadable - and detail after.
gdscript_probe() {
  local probe_status=0
  local probe_output
  probe_output="$(cd "${REPO_ROOT}" && git ls-files --cached --others --exclude-standard \
    -- '*.gd' '*.gdshaderinc.gd' 2>&1)" || probe_status=$?

  if [[ "${probe_status}" -ne 0 ]]; then
    printf 'unreadable\ngit ls-files exited %s: %s\n' "${probe_status}" "${probe_output}"
  elif [[ -n "${probe_output}" ]]; then
    printf 'violation\n%s\n' "${probe_output}"
  else
    printf 'clean\n'
  fi
}

# The first line of a probe's verdict, without a pipe. `probe | head -1` makes head exit
# after one line, the probe take SIGPIPE, and `set -o pipefail` surface 141; `set -e` then
# ABORTS THE WHOLE GATE mid-run - after § 1-6 have printed `ok` lines and before § 7 or
# § 7a print anything, which reads as a truncated log rather than as a failure. It was
# intermittent and it fired during § 8's negative controls.
#
# Measured over 300 trials on a probe emitting one path per offending file: 0 of 300 at
# 428 bytes and at 4.1 KB, 136 of 300 at 70 KB, 300 of 300 at 326 KB. A tree with a few
# hundred stray .gd files is exactly the tree this section exists to fail on, so the abort
# was reachable precisely when the gate mattered. See delivery-waves § Decision 13.
#
# `tail -n +2` below is left as a pipeline on purpose: `tail` must read to EOF to know
# where the end is, so it never closes the pipe early and there is no SIGPIPE to take.
first_line() {
  local text="$1"
  printf '%s' "${text%%$'\n'*}"
}

gdscript_verdict="$(gdscript_probe)"
gdscript_kind="$(first_line "${gdscript_verdict}")"
gdscript_detail="$(printf '%s\n' "${gdscript_verdict}" | tail -n +2)"

if [[ "${gdscript_kind}" == "unreadable" ]]; then
  fail "could not enumerate GDScript candidates, so the no-GDScript rule is unproved: ${gdscript_detail}"
elif [[ "${gdscript_kind}" == "violation" ]]; then
  fail "GDScript is not permitted (TR-FND-002), tracked or untracked: ${gdscript_detail}"
else
  pass "no .gd file is tracked, and none is present untracked in the working tree"
fi

section "7a. negative controls: the no-GDScript gate can actually fail"
readonly GDSCRIPT_FIXTURE="${REPO_ROOT}/game/DeliberatelyForbiddenGdscriptFixture.gd"

remove_gdscript_fixture() {
  rm -f "${GDSCRIPT_FIXTURE}"
}

if [[ "${gdscript_kind}" == "unreadable" ]]; then
  # The controls drive the same enumeration § 7 just failed to perform, so running them
  # would only restate that failure in three more places. § 7 already failed the gate.
  echo "      NOT RUN: § 7 could not enumerate at all, so these controls cannot mean"
  echo "      anything in this environment. The gate is already failing above."
else
  trap remove_gdscript_fixture EXIT

  # Control 1: an untracked .gd file. This is exactly the case the old gate passed.
  cat >"${GDSCRIPT_FIXTURE}" <<'GDFIXTURE'
# Deliberately forbidden GDScript, written and removed by build/verify-architecture.sh.
extends Node
GDFIXTURE

  control_kind="$(first_line "$(gdscript_probe)")"
  if [[ "${control_kind}" == "violation" ]]; then
    control_pass "an untracked .gd file is detected as a violation"
  else
    control_fail "an untracked .gd file was reported as '${control_kind}'; the gate cannot see untracked GDScript"
  fi

  # Control 2: the same fixture, with git unable to answer. The gate must report that
  # it could not tell, and must never report a clean tree.
  control_kind="$(first_line "$(GIT_DIR=/nonexistent/verify-architecture-broken.git gdscript_probe)")"
  if [[ "${control_kind}" == "unreadable" ]]; then
    control_pass "a git failure is reported as unreadable, not as a clean tree"
  else
    control_fail "with a broken git the probe reported '${control_kind}'; a failed enumeration must not pass"
  fi

  remove_gdscript_fixture
  trap - EXIT

  # The fixture must be gone. The comparison is against § 7's own verdict rather than
  # against "clean", so that a pre-existing .gd file in the tree - which § 7 already
  # failed on - is not reported a second time as a fixture-cleanup failure.
  control_kind="$(first_line "$(gdscript_probe)")"
  if [[ "${control_kind}" == "${gdscript_kind}" ]]; then
    control_pass "the fixture was removed; the probe reports '${control_kind}' again, as it did in § 7"
  else
    control_fail "the GDScript fixture was not removed: probe reports '${control_kind}', § 7 saw '${gdscript_kind}'"
  fi
fi

section "8. the boundary comparisons above can actually fail (VER-FND-009-013)"
#
# Sections 3 and 4 only ever ran against compliant input, so nothing showed they were
# capable of reporting a violation. MechaMiner.Diagnostics made that gap matter: it is a
# sixth src/ project whose accepted row is the strictest in the repository - ".NET base
# libraries only", Godot "No", zero references, a dependency leaf every other project may
# reference without a cycle - and that row is the only thing keeping the leaf a leaf.
#
# Each fixture under build/policy-fixtures/architecture/ is a project file named
# MechaMiner.Diagnostics.csproj carrying exactly one violation of that row. They are fed
# through edges_match and godot_matches, the same functions sections 3 and 4 call, and each
# must report a difference. The accepted row is read out of EXPECTED_PROJECTS rather than
# hardcoded here, so a future task that legitimately gives Diagnostics an edge updates one
# place and these controls keep testing the row that is actually accepted.

readonly DIAGNOSTICS_PROJECT="src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj"
readonly CONTROL_ROOT="build/policy-fixtures/architecture"

# "<fixture directory>|<the project the fixture references>|<what it injects>"
#
# The middle field is the evaluated reference set the fixture must produce. Asserting it
# closes a both-sides-absent comparison: the compliant control below compares "" against
# an accepted set that is also "" today, so it would pass against an msbuild_items that
# had broken into returning nothing for every input. Requiring each edge fixture to come
# back naming the project it references proves the evaluation actually happened.
readonly EDGE_CONTROLS=(
  "edge-content|MechaMiner.Content|a reference to MechaMiner.Content"
  "edge-simulation|MechaMiner.Simulation|a reference to MechaMiner.Simulation"
  "edge-game|MechaMiner.Game|a reference to MechaMiner.Game (the reverse Godot edge)"
)
# Guards against an empty control set silently proving nothing, the way an unquoted or
# mistyped array expansion would. The loop must run this many times.
readonly EXPECTED_EDGE_CONTROLS=3

if ! diagnostics_accepted_refs="$(accepted_field "${DIAGNOSTICS_PROJECT}" refs)"; then
  fail "negative control cannot run: ${DIAGNOSTICS_PROJECT} has no EXPECTED_PROJECTS row"
elif ! diagnostics_accepted_godot="$(accepted_field "${DIAGNOSTICS_PROJECT}" godot)"; then
  fail "negative control cannot run: ${DIAGNOSTICS_PROJECT} has no EXPECTED_PROJECTS row"
else
  # Positive control first. Every control below passes by producing a DIFFERENCE, so a
  # comparison that had broken into reporting a difference for every input would pass all
  # of them. This is the one input that must come back equal.
  control="${CONTROL_ROOT}/compliant/MechaMiner.Diagnostics.csproj"
  if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
    fail "negative-control fixture missing: ${control}"
  elif edges_match "${control}" "${diagnostics_accepted_refs}"; then
    pass "control: a compliant Diagnostics project compares equal to [${diagnostics_accepted_refs}]"
  else
    fail "control: a compliant Diagnostics project reported [${EDGES_ACTUAL}]; the comparison reports a difference for compliant input, so every negative control below is meaningless"
  fi

  edge_controls_run=0
  for entry in "${EDGE_CONTROLS[@]}"; do
    IFS='|' read -r fixture referenced injected <<<"${entry}"
    control="${CONTROL_ROOT}/${fixture}/MechaMiner.Diagnostics.csproj"
    if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
      fail "negative-control fixture missing: ${control}"
      continue
    fi
    edge_controls_run=$((edge_controls_run + 1))
    if edges_match "${control}" "${diagnostics_accepted_refs}"; then
      fail "control: Diagnostics with ${injected} was NOT rejected; § 3 accepted [${EDGES_ACTUAL}] against [${diagnostics_accepted_refs}]"
    elif [[ "${EDGES_ACTUAL}" != "${referenced}" ]]; then
      # It was rejected, but not for the reason the control exists to prove. An empty
      # evaluated set would also be "rejected", and would mean MSBuild evaluated nothing.
      fail "control: Diagnostics with ${injected} was rejected, but § 3 evaluated [${EDGES_ACTUAL}] instead of [${referenced}]; the rejection does not prove the injected edge was seen"
    else
      pass "control: Diagnostics with ${injected} is rejected (§ 3 saw [${EDGES_ACTUAL}])"
    fi
  done

  if [[ "${edge_controls_run}" -eq "${EXPECTED_EDGE_CONTROLS}" ]]; then
    pass "control: all ${EXPECTED_EDGE_CONTROLS} forbidden-edge controls ran"
  else
    fail "control: ${edge_controls_run} of ${EXPECTED_EDGE_CONTROLS} forbidden-edge controls ran; a control set that shrank proves less than it claims"
  fi

  control="${CONTROL_ROOT}/godot/MechaMiner.Diagnostics.csproj"
  if [[ ! -f "${REPO_ROOT}/${control}" ]]; then
    fail "negative-control fixture missing: ${control}"
  elif godot_matches "${control}" "${diagnostics_accepted_godot}"; then
    fail "control: Diagnostics with a GodotSharp PackageReference was NOT rejected by § 4"
  elif [[ "${GODOT_UNPROVED}" == "yes" ]]; then
    # Rejected, but as "unproved" rather than as "has Godot". An unevaluable fixture would
    # also be "not accepted", and would prove only that MSBuild could not read it.
    fail "control: Diagnostics with a GodotSharp PackageReference was rejected as unproved (${GODOT_EVALUATED}), so § 4's Godot detection was never exercised"
  elif [[ "${GODOT_EVALUATED}" != *[Gg]odot* ]]; then
    # Rejected, but not for the reason the control exists to prove, mirroring the
    # edge-control check above: § 4 must have actually seen the injected Godot package.
    fail "control: Diagnostics with a GodotSharp PackageReference was rejected, but § 4 evaluated [${GODOT_EVALUATED}] and saw no Godot package; the rejection does not prove the injected dependency was seen"
  else
    pass "control: Diagnostics with a GodotSharp PackageReference is rejected (§ 4 saw [${GODOT_EVALUATED}])"
  fi
fi

section "9. the CI workflow still gates the repository (VER-FND-005-009)"
#
# NUMBERED 9, NOT 8, AND ONLY FOR THAT REASON. FND-005 wrote this section as § 8 on
# claude/hearth-thread-2vmaro-fnd-002 while FND-009 wrote a different § 8 above on
# claude/hearth-thread-2vmaro-fnd-004, and the two arrived in one file at merge. BOTH ARE
# KEPT: they assert unrelated things and dropping either loses a control. § 8 keeps its
# number because VER-FND-009-013 and PR #7's description cite it by it; this section keeps
# VER-FND-005-009 and every assertion FND-005 wrote for it, unchanged.
#
# Section 1 lists the workflow among EXPECTED_PATHS, which is a test of the path and
# nothing more. `[[ -e ]]` accepts a zero-byte fast.yml, and it accepts a workflow with
# no jobs and no pull_request or push trigger. Either of those un-gates every gate in
# this repository exactly as silently as deleting the file, and `./build.sh build` was
# green for both. What follows asserts the content the suite depends on.
#
# The required verbs are a list of requirements, not a roster of what the file happens
# to contain: delivery-waves § Step 4 says "The fast pull-request path is bootstrap,
# format-check, build, test-fast, godot-import". Deriving them from the workflow would
# assert only that the workflow agrees with itself.

readonly CI_WORKFLOW=".github/workflows/fast.yml"
readonly REQUIRED_TRIGGERS=("pull_request" "push")
readonly REQUIRED_FAST_VERBS=("bootstrap" "format-check" "build" "test-fast" "godot-import")

# The child keys of a top-level `key:` block, in either the block or the inline-list
# form, so `on: [push, pull_request]` reads the same as the block this file uses.
yaml_block_keys() {
  awk -v want="$1" '
    index($0, want ":") == 1 {
      rest = substr($0, length(want) + 2)
      sub(/^[[:space:]]*/, "", rest)
      if (rest ~ /^\[/) {
        gsub(/[][]/, "", rest)
        n = split(rest, parts, /,/)
        for (i = 1; i <= n; i++) {
          gsub(/[[:space:]]/, "", parts[i])
          if (parts[i] != "") { print parts[i] }
        }
      } else if (rest == "" || rest ~ /^#/) {
        block = 1
      }
      next
    }
    block && /^[^[:space:]#]/ { block = 0 }
    block && /^  [A-Za-z_][A-Za-z0-9_-]*:/ {
      key = $0
      sub(/:.*/, "", key)
      gsub(/[[:space:]]/, "", key)
      print key
    }
  ' "$2"
}

workflow_path="${REPO_ROOT}/${CI_WORKFLOW}"

if [[ ! -f "${workflow_path}" ]]; then
  fail "${CI_WORKFLOW} does not exist, so nothing in this repository is gated by anything"
elif [[ ! -s "${workflow_path}" ]]; then
  fail "${CI_WORKFLOW} exists but is empty, so it runs nothing; § 1's path test cannot tell those apart"
else
  pass "${CI_WORKFLOW} exists and is not empty"

  # Here-strings rather than pipes into `grep -q`: grep exits on its first match and
  # closes the pipe, printf takes SIGPIPE, and `set -o pipefail` then aborts the whole
  # script with 141 instead of reporting an assertion. That happened, nondeterministically,
  # on the control that deletes one step.
  mapfile -t workflow_triggers < <(yaml_block_keys "on" "${workflow_path}")
  workflow_trigger_list="$(printf '%s\n' "${workflow_triggers[@]-}")"
  for trigger in "${REQUIRED_TRIGGERS[@]}"; do
    if grep -qxF "${trigger}" <<<"${workflow_trigger_list}"; then
      pass "${CI_WORKFLOW} triggers on ${trigger}"
    else
      fail "${CI_WORKFLOW} declares no ${trigger} trigger, so the suite never runs for that event"
    fi
  done

  mapfile -t workflow_jobs < <(yaml_block_keys "jobs" "${workflow_path}")
  if [[ "${#workflow_jobs[@]}" -eq 0 ]]; then
    fail "${CI_WORKFLOW} declares no job, so nothing in it can run"
  else
    pass "${CI_WORKFLOW} declares ${#workflow_jobs[@]} job(s): ${workflow_jobs[*]}"
    if grep -qE '^[[:space:]]+steps:[[:space:]]*$' "${workflow_path}"; then
      pass "${CI_WORKFLOW} declares a steps: block"
    else
      fail "${CI_WORKFLOW} declares a job with no steps: block"
    fi
  fi

  workflow_body="$(sed 's/#.*$//' "${workflow_path}")"
  for verb in "${REQUIRED_FAST_VERBS[@]}"; do
    if grep -qE "(^|[[:space:];&|])(\./)?build\.(sh|ps1)[[:space:]]+${verb}([[:space:]]|\$)" \
        <<<"${workflow_body}"; then
      pass "${CI_WORKFLOW} invokes ./build.sh ${verb}"
    else
      fail "${CI_WORKFLOW} never invokes ./build.sh ${verb}, which delivery-waves § Step 4 puts on the fast path"
    fi
  done
fi

# This gate runs negative controls in band (§ 7a), so its log contains failure-shaped text
# on a green run. Prove the marking that separates that text from genuine findings holds.
gate_assert_marking

gate_summary "verify-architecture" "${EXIT_VALIDATION}"
