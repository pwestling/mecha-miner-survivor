#!/usr/bin/env bash
#
# Proves the run slice evidence harness ran during THIS invocation and evaluated the
# assertions it claims to, rather than merely being named at a reachable call site.
#
# Authority: docs/technical/100-build-dependencies-and-release-operations.md
#              § Standard command surface (exit classes; "wrappers preserve the owning
#              tool's class rather than returning success after partial work")
#            docs/technical/91-verification-strategy.md § Verification registry,
#              § Negative control adequacy, § Determinism and fixture policy
#            docs/technical/delivery-waves.md § Decision 11 rule 4 (every gate carries a
#              negative control)
# Requirements: TR-PRE-001, TR-QUA-001, TR-BLD-005
# Verification: VER-PRE-001-003, VER-PRE-001-004, VER-PRE-001-005, VER-PRE-001-006,
#               VER-PRE-001-007
#
# Why this is a script and not a launch line in a verb:
#
#   game/tests/RunSliceEvidenceHarness.tscn is the only selector the camera, the input
#   adapter and the snapshot-to-transform path have. No test project may reference
#   MechaMiner.Game under the accepted project boundary (build/verify-architecture.sh
#   § 3), so there is no NUnit route to any of it, and until this script existed the
#   harness was run by hand and its transcript retained as evidence. Wiring it into a
#   verb without asserting anything about the run would be strictly worse than leaving
#   it unwired: a harness that passes when nothing works and is now invoked by a verb
#   manufactures a green where there was an honest silence (VER-PRE-001-004).
#
#   The traps this script encodes, each of which a bare launch line walks into:
#
#   1. THE ENGINE'S EXIT CODE IS NOT THE GATE. A headless Godot launch exits 0 even when
#      the C# script on a node fails to load - it logs "Cannot instantiate C# script" and
#      carries on. FND-001 hit this empirically; build/verify-godot-runner.sh § 5 keeps a
#      standing demonstration of it. So this gate reads the harness's own evidence.
#
#   2. A RETAINED ARTIFACT SATISFIES A LOG CHECK FOREVER. The harness writes its
#      transcript to the directory named by MECHAMINER_RUN_SLICE_OUTPUT
#      (RunSliceEvidenceHarness.OutputDirectoryVariable, declared at :58 as measured at
#      42a5c83), and a developer's hand-run leaves one behind. This gate therefore
#      creates an EMPTY output directory per invocation and requires the transcript to be
#      absent before the launch and present after (VER-PRE-001-003(c)). Without that, a
#      stale file answers every question the gate can ask about a run that never happened.
#
#   3. A COUNT NOBODY READS IS NOT A COUNT. The harness prints "assertions_run<TAB><n>"
#      into the transcript and exits on its failure count alone, so a run that evaluated
#      3 of 28 assertions passes exactly as happily as one that evaluated 28, and a run
#      whose every section returned early before its first check prints "assertions_run
#      0", "assertions_failed 0", "outcome passed" and exits 0. That vacuous green is
#      what VER-PRE-001-005 and VER-PRE-001-006 close, and closing it needs three
#      independent anchors rather than a pin - see § 4's own note.
#
#   4. NO TRANSCRIPT CONTENT REACHES STDOUT. Stdout carries the startup line, a one-line
#      summary and the transcript path, and nothing else. So §§ 3 and 4 read the file;
#      only § 1(b)'s startup-line assertion reads stdout, because that is the only place
#      the startup line appears.
#
# CITED BY ANCHOR TEXT, NOT BY OFFSET. Every reference to the harness below names a
# constant or a distinctive string - StartupLine, OutputDirectoryVariable,
# TranscriptFileName, "assertions_run" - because the harness is under repair around its
# interpolation section and everything below that point moves. Where a line number
# appears at all it is paired with the anchor text and with the sha it was measured at
# (42a5c83), so a decayed number is visibly a measurement and not a claim. NOTHING IN
# THIS SCRIPT'S RUNTIME BEHAVIOUR DEPENDS ON A LINE NUMBER: it greps for strings and
# never seeks to an offset.
#
# WHAT THIS GATE'S EXECUTION IS CONDITIONAL ON, said plainly because the wiring gate
# cannot say it:
#
#   This script is reached from GodotImportVerb - the godot-import verb - which
#   .github/workflows/fast.yml invokes as the LAST VERB-RUNNING STEP of its one job
#   (the two steps after it are `if: always()` artifact retention and exit-class
#   reporting). A GitHub Actions step without `if:` runs only when every earlier step in
#   the job succeeded. So on a run where format-check, build or test-fast goes red, this
#   gate DOES NOT RUN AT ALL, and its assertions are not merely unproved for that commit
#   - they are unattempted.
#
#   HOW THIS SCRIPT IS FILED, and the rule stated conditionally because the general form
#   is easy to get backwards: build/verify-gate-wiring.sh's § 4 is an exclusive-or between
#   REACHED and EXEMPT, not between its two arrays. This script gets one INVENTORY row,
#   kind `gate`, empty argument field, and NO EXEMPT row - because it is reached, and a
#   gate that is both reached and exempt is what § 4 reds. The pairing rule applies the
#   other way round: an UNREACHED gate needs an EXEMPT row, or § 4 reds it as "never
#   invoked and not on the deliberately-unwired list". Every EXEMPT path is also an
#   INVENTORY entry; the choice is only whether the EXEMPT half exists.
#
#   build/verify-gate-wiring.sh cannot distinguish those cases. Its § 4 reach analyzer is
#   a pure text scan: it resolves a verb name to a handler, finds this path inside a
#   member reachable from that handler, and prints "ok reached:" identically for a step
#   that always runs and for a step that never does. Being reached is not being run, and
#   VER-FND-005-010 says so of itself in capitals ("WHAT THIS ASSERTS IS REACH, NOT
#   EXECUTION"). What corroborates execution is the retained verb evidence under
#   artifacts/verbs/godot-import/, which fast.yml's "retain the verb evidence" step
#   uploads; nothing inside a gate can catch its own non-execution. That instrument gap
#   is the run manifest FND-005.json's notes scope and defer, and it is why
#   VER-PRE-001-007 carries deferredTo: FND-005. This header states the weaker accurate
#   claim rather than the stronger convenient one.
#
# ASSUMED, NOT ASSERTED, AND DELIBERATELY SO: that MechaMiner.Game has been built and the
# project imported. Both are done by build/verify-godot.sh, which GodotImportVerb runs
# immediately before this script in the same verb, from a cold cache by construction.
# This gate does not repeat them, so it has no class 5 and defines none. The cost is
# named rather than hidden: if the assembly were absent, the harness's script would not
# attach and this gate would report an absent startup line - a class 4 - for what is
# really a build failure. § 1's diagnostic therefore lists that cause explicitly instead
# of asserting one reading of it.
#
# Exit classes follow doc 100 § Standard command surface: 0 success,
# 2 invalid invocation, 3 environment, 4 validation failure.
#
# THIS SCRIPT PASSES THE HARNESS'S OWN CLASS THROUGH VERBATIM, AND THAT IS NOT WHAT
# build/verify-godot.sh DOES. Read the note above the final exit before tidying it.
# VER-PRE-001-003 asserts that "the harness's own exit code becomes the verb's exit class
# unchanged", and this script is the only thing standing between the two, so substituting
# a single EXIT_VALIDATION for every failure - which is exactly build/verify-godot.sh's
# shape - would break that claim while looking house-styled.
#
# The pass-through has one silent exception and it is not this script's to fix:
# GodotImportVerb's TimedOut branch returns class 5 before it reads any exit code, and
# CommandRunner sets the code to -1 on a timeout. So "unchanged" holds for a harness that
# finishes and not for one the verb's own 20-minute bound kills. This script's own,
# tighter bound is handled explicitly rather than inherited - see LAUNCH_TIMEOUT_SECONDS
# and § 2's 124/137 case - so a bound this script imposed is reported as this script's
# finding and never mistaken for a class the harness chose.

set -uo pipefail

readonly REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly GAME_DIR="${REPO_ROOT}/game"
readonly REGISTRY_DIR="${REPO_ROOT}/tests/verification"

# The scene, named. At 42a5c83 no other gate can reach it: build/verify-godot.sh launches
# bare and arrives at Boot.tscn only through game/project.godot's run/main_scene, and
# build/verify-godot-runner.sh names res://tests/GodotTestRunner.tscn and
# res://tests/DeliberatelyBrokenScene.tscn. Naming it here is VER-PRE-001-003(a).
readonly HARNESS_SCENE="res://tests/RunSliceEvidenceHarness.tscn"
readonly HARNESS_SCENE_FILE="${GAME_DIR}/tests/RunSliceEvidenceHarness.tscn"

# RunSliceEvidenceHarness.StartupLine, declared at :55 and printed by _Ready at :71 as
# measured at 42a5c83. It exists for exactly this assertion.
readonly STARTUP_LINE="MechaMiner: run slice evidence harness ready"

# RunSliceEvidenceHarness.OutputDirectoryVariable (:58) and TranscriptFileName.
#
# The env var is what this gate sets. Be aware that the harness's ReadSetting prefers a
# --mechaminer-run-slice-output= CLI user arg over the environment, so anything appearing
# after `--` on the launch line OVERRIDES what is set here. This gate passes no user args
# at all, which is the only way to keep the directory it created and the directory the
# harness writes to the same one.
readonly OUTPUT_VARIABLE="MECHAMINER_RUN_SLICE_OUTPUT"
readonly TRANSCRIPT_NAME="transcript.tsv"

# Pinned engine discovery, in build/toolchain.json's order: MECHAMINER_GODOT, then godot
# on PATH. The prefix is toolchain.json's expected_version_prefix for the godot entry.
readonly GODOT="${MECHAMINER_GODOT:-godot}"
readonly EXPECTED_VERSION_PREFIX="4.7.1.stable.mono.official"

readonly EVIDENCE_DIR="${REPO_ROOT}/artifacts/engine-tier/verify-run-slice"
readonly LAUNCH_TIMEOUT_SECONDS=180

# The pathspec the no-mutation measurement covers, and the words every verdict about it
# uses. game/, tests/ and build/ are what this gate reads, so they are what it must not
# change. artifacts/ and the controls' own mktemp -d are deliberately OUTSIDE this
# pathspec, which is why a control writing under either is not a mutation of the tree this
# gate checks - and also why the controls alone could never turn the measurement red, which
# is what § 6g exists to fix.
readonly MUTATION_PATHSPEC=(game tests build)
readonly MUTATION_SCOPE="game/, tests/ and build/"

# ANCHOR 2 of VER-PRE-001-006's three. A committed integer, here so that adding or
# removing a harness assertion is a two-line change that forces someone to state the new
# total by hand.
#
# WHAT THIS NUMBER IS A COUNT OF, AND AT WHICH REF - stated so the next reader
# re-derives it instead of trusting it:
#
#   Derived at 42a5c83 from the Check( call sites in game/tests/RunSliceEvidenceHarness.cs:
#   23 fixed sites, plus 5 iterations of the one movement loop over the five legs it
#   declares, is 28. Per section at that ref: camera 6, deadzone 6, facing 2, open-tick 3,
#   movement 8, interpolation 3.
#
#   `grep -c 'Check('` returns 26 on that file and is the wrong instrument twice over: it
#   counts the private Check definition, which is not a call site, and it counts the
#   movement loop's two mutually exclusive branch sites once each rather than five times
#   round the loop. That is why the derivation is written down here instead of a grep
#   being cited.
#
#   THIS NUMBER IS EXPECTED TO MOVE, AND SOON. A repair to the interpolation section is in
#   flight: the assertion VER-PRE-001-001 claims there compares a convex interpolant of
#   two endpoints against those same endpoints, which holds by arithmetic whatever the
#   production code does, and the value actually under test - pivot.Position - is only
#   logged. Replacing that with a real predicate need not be one-for-one: asserting
#   pivot.Position on more than one axis, or splitting the directional bound into a
#   non-directional pair, changes the interpolation section's 3. Re-derive the total from
#   the Check( sites when this gate goes red on the pin, and do not assume 28 is current.
#   A pin whose provenance is written down is repairable; a bare 28 is a mystery.
readonly EXPECTED_ASSERTIONS=28

# The section roster's expected size, reported rather than asserted - see § 4's third
# anchor, which derives the roster itself. Recorded here only so a reader knows what the
# derivation returned at 42a5c83: six entries, from PRE-001.json, PRE-002.json and
# UI-002.json, which are the entire engine-scene population of the corpus at that ref.
readonly ROSTER_SIZE_AT_42A5C83=6

# Every control case below is counted, and the total is asserted at the end. Emitted
# UNMARKED on purpose: that assertion is about the control set, not about anything a
# control's fixture produced.
#
#   The pin counts the in-band negative controls this script emits. It was 36 as counted at
#   42a5c83, 45 as counted at e1ce969 (which added nine: § 6g's three, § 6h's three, § 6c's
#   empty-roster case at both log sizes, and the metadata leg of the no-mutation
#   measurement, which used to be one finding and is now two), and is 46 as counted at THIS
#   commit, which added one - § 6d's chatty-stub row, the control that proves the version
#   read takes the LAST NON-EMPTY stdout line and not the first. Nothing derives it: it is
#   restated by hand whenever a control is added or removed, in the same commit
#   that adds or removes one. A mismatch therefore means one of two things - a control was
#   added and the total was not restated, which is bookkeeping, or a control that used to
#   run has stopped running, which is the failure this pin exists to catch and which nothing
#   else here would notice. Read a bare 46 with no ref stamp as a rumour rather than a
#   measurement; the stamp is what makes the next reader able to tell a stale count from a
#   regression.
#
#   HOW 46 IS REACHED AT RUNTIME, AND WHY NO GREP FINDS IT. There are 13 literal
#   control_pass/control_fail call sites and 7 literal `controls_run` increment sites in
#   this file, and they multiply out to 46 emissions because most of them sit inside a
#   table-driven loop and most of those loops run each row twice, once at fixture size and
#   once at ~300 KB. One of the 13 sites - § 6h's unavailability branch, which reports three
#   unproved controls if the throwaway repository cannot be created - does not run on a
#   healthy machine at all, so a green run reaches 12 of them. Per section, as counted at
#   this commit:
#
#     6a launch evidence       4 rows x 2 log sizes  =  8
#     6b assertions_run        4 rows x 2 sizes      =  8
#     6c registry roster       5 rows x 2 sizes      = 10
#     6d engine pin            4 rows               =  4
#     6e exit-code conduit     3 rows x 2 attempts   =  6
#     6f coherent scene        1                    =  1
#     6f absent scene          1                    =  1
#     6g mutation predicate    3 injected rows      =  3
#     6h mutation predicate    3 real-repo cases    =  3
#     6i the real-tree probe   2 legs               =  2
#                                                     --
#                                                     46
#
#   SO DO NOT "FIX" THIS CONSTANT FROM A GREP. `grep -c control_pass` over this file returns
#   9 - six emitter call sites plus three prose mentions, two of them inside this very
#   comment - and a reader who takes 9, or 13, or 7 for the runtime total will conclude the
#   pin is wrong and either edit it or file a bug against a gate that is working. The runtime
#   number and the static number differ BY DESIGN, because the loops are what make the
#   control set cheap to extend. If this pin goes red, count the emissions in a real run's
#   log - `grep -c '\[control-fixture\] control:'` returns exactly this number on a healthy
#   run - before touching the constant.
readonly EXPECTED_CONTROLS=46

# The classes this script can return, named from src/MechaMiner.Tools/Cli/ExitClass.cs so
# the shell side and the C# side cannot drift apart silently: InvalidInvocation = 2,
# Environment = 3, Validation = 4. The class the taxonomy deliberately omits is 1, not 2 -
# ExitClass's own remarks say so ("There is deliberately no 1 ... a wrapper that returns 1
# has leaked an unclassified failure").
#
# Class 3 has no live consumer in this repository yet: VerbOutcome.Environment and
# ExitClass.FromProcess are dead repo-wide at 42a5c83, and no gate script defines a
# class-3 constant (build/bootstrap-linux.sh does, and is a naming precedent only). The
# line this gate is on the far side of is the one BootstrapVerb argues at length for its
# locked restore: a committed lock file disagreeing with a committed props file is class 5
# because "the machine is correct, and the fix is a repository edit ... not an environment
# repair, which is the action a caller reading 3 would take". An absent or unpinned Godot
# is squarely the other side of that line - the repository is correct and the machine is
# not - so 3 is the class, and returning 4 for it would send a reader to edit the tree.
readonly EXIT_INVALID_INVOCATION=2
readonly EXIT_ENVIRONMENT=3
readonly EXIT_VALIDATION=4

# The shared emitters: pass/fail for findings about the subject under test,
# control_pass/control_fail for anything produced while a negative control's fixture is in
# place, section/gate_summary so a red run names the failing section. See build/gate-output.sh
# for why control output is marked and why that marking is enforced rather than conventional.
source "${REPO_ROOT}/build/gate-output.sh"

controls_run=0

# --- the predicates -----------------------------------------------------------
#
# Every predicate this gate decides on is a function taking text and printing one problem
# description per line, returning the problem count. That shape is what lets § 6's
# controls drive THE SAME FUNCTIONS the sections above drove, over injected fixtures,
# rather than re-implementing the reading and proving nothing about the gate.
#
# Two mechanical rules hold throughout, and both are measured rather than stylistic:
#
#   grep -q TAKES A HERE-STRING, NEVER A PIPE. `printf '%s' "$big" | grep -qE ...` under
#   `set -o pipefail` yielded a spurious exit 141 in 300 of 300 trials at 187 KB, because
#   grep exits on its first match while printf still has a write to do; the `<<<` form
#   gave 0 of 300 at every size. Godot logs are tens of kilobytes. This is the same defect
#   build/verify-godot.sh's startup-line read carried before it was changed to `<<<`.
#
#   control_detail IS FED BY REDIRECTION, NEVER FROM THE RIGHT OF A PIPE. On the right of
#   a pipe it runs in a subshell and the marked-line count it keeps is discarded, so
#   gate_summary makes a numeric claim that is quietly wrong. Use `control_detail
#   <<<"${text}"` or `control_detail < <(pipeline)`.

strip_ansi() {
  sed -e 's/\x1b\[[0-9;]*m//g'
}

# VER-PRE-001-003(b) and (c) in one function, so the controls exercise the real reading.
# $1 stdout text, $2 "present"|"absent" for the transcript after the launch.
evaluate_launch_evidence() {
  local stdout_text="$1"
  local transcript_state="$2"
  local problems=0

  if ! grep -qF -- "${STARTUP_LINE}" <<<"${stdout_text}"; then
    printf 'the harness never printed StartupLine ("%s"), so it did not reach managed code: either the scene loaded and its script did not attach, or _Ready threw before printing, or MechaMiner.Game was never built and imported by the preceding step\n' \
      "${STARTUP_LINE}"
    problems=$((problems + 1))
  fi

  if [[ "${transcript_state}" != "present" ]]; then
    printf 'no %s was produced in the per-invocation output directory, so there is no evidence of a run rather than evidence of a retained artifact\n' \
      "${TRANSCRIPT_NAME}"
    problems=$((problems + 1))
  fi

  return "${problems}"
}

# VER-PRE-001-005, all three sub-assertions. $1 transcript text.
#
# Prints one problem per line and returns the count. An ABSENT line and a ZERO are
# reported as different findings and the absent case is never read as zero: collapsing
# them is exactly the defect doc 91 § Negative control adequacy's third bullet names, and
# a non-numeric value is a failure rather than something to coerce - the coercion defect
# VER-FND-005-012 records for exit classes, where `[[ ${class} -ne 0 ]]` read FAILED, 4x
# and 00 all as zero.
classify_assertion_count() {
  local transcript_text="$1"
  local line raw

  if ! grep -qE "^assertions_run"$'\t' <<<"${transcript_text}"; then
    printf 'the transcript carries NO assertions_run line at all. This is not a count of zero: an absent value and a zero are different findings, and the harness prints that line unconditionally, so its absence means the transcript was not written by the harness this gate expects\n'
    return 1
  fi

  line="$(grep -m 1 -E "^assertions_run"$'\t' <<<"${transcript_text}")"
  raw="${line#*$'\t'}"

  if [[ ! "${raw}" =~ ^[0-9]+$ ]]; then
    printf 'the assertions_run value %q does not parse as a non-negative integer. It is NOT coerced to a number: a value nobody can read is a failure, not a zero and not a 28\n' \
      "${raw}"
    return 1
  fi

  if [[ "${raw}" -eq 0 ]]; then
    printf 'assertions_run is 0: the harness started, wrote a transcript, reported outcome passed and exited 0 while evaluating no comparison at all. This is the vacuous green VER-PRE-001-005 exists to close, and no exit-code check can see it\n'
    return 1
  fi

  printf 'assertions_run=%s\n' "${raw}"
  return 0
}

# ANCHOR 3 of VER-PRE-001-006. Derives the expected section roster from the registry -
# files this package does not own - so the pin is not a comparison between two lists one
# author edits in one commit.
#
# $1 registry directory. Prints one section name per line, sorted.
registry_section_roster() {
  python3 - "$1" <<'PY'
import glob, json, os, re, sys

pattern = re.compile(
    r"^game/tests/RunSliceEvidenceHarness\.tscn\s+§\s+(?P<section>\S+)\s*$")
sections = set()
for path in sorted(glob.glob(os.path.join(sys.argv[1], "*.json"))):
    with open(path, "r", encoding="utf-8") as handle:
        document = json.load(handle)
    for entry in document.get("entries", []):
        selector = entry.get("selector") or {}
        if selector.get("kind") != "engine-scene":
            continue
        match = pattern.match(str(selector.get("value", "")))
        if match is not None:
            sections.add(match.group("section"))
for section_name in sorted(sections):
    print(section_name)
PY
}

# The other side of anchor 3: the "## <section>" headings the transcript emits, each with
# the number of PASS/FAIL lines inside it. $1 transcript text. Prints "<name><TAB><count>".
transcript_section_assertions() {
  awk -F'\t' '
    /^## / { name = substr($0, 4); order[++n] = name; count[name] = 0; next }
    /^PASS\t/ || /^FAIL\t/ { if (n > 0) count[order[n]]++ }
    END { for (i = 1; i <= n; i++) printf "%s\t%s\n", order[i], count[order[i]] }
  ' <<<"$1"
}

# Set equality in BOTH directions, plus the requirement that each heading contain at
# least one PASS or FAIL line. Both directions matter: a transcript section no registry
# entry names must also be red, so a section added without a registry claim cannot arrive
# unnoticed. $1 roster text (one name per line), $2 transcript text.
compare_section_roster() {
  local roster_text="$1"
  local transcript_text="$2"
  local problems=0
  local emitted name count expected

  emitted="$(transcript_section_assertions "${transcript_text}")"

  while IFS= read -r expected; do
    [[ -n "${expected}" ]] || continue
    if ! grep -qxF -- "${expected}" <<<"$(cut -f1 <<<"${emitted}")"; then
      printf 'a registry entry names section %q, and the transcript emits no such "## " heading. Read this as a RENAME before reading it as a missing section: the headings the transcript does have are %s\n' \
        "${expected}" "$(cut -f1 <<<"${emitted}" | paste -sd, -)"
      problems=$((problems + 1))
    fi
  done <<<"${roster_text}"

  while IFS=$'\t' read -r name count; do
    [[ -n "${name}" ]] || continue
    if ! grep -qxF -- "${name}" <<<"${roster_text}"; then
      printf 'the transcript emits section %q, which no registry entry with an engine-scene selector names. A section added without a registry claim must be red, or coverage grows without anyone claiming it\n' \
        "${name}"
      problems=$((problems + 1))
      continue
    fi
    if [[ "${count}" -eq 0 ]]; then
      printf 'section %q is present as a heading with no PASS or FAIL line inside it, so it was entered and evaluated nothing. A heading is not an assertion\n' \
        "${name}"
      problems=$((problems + 1))
    fi
  done <<<"${emitted}"

  return "${problems}"
}

# VER-PRE-001-007(b) as a function, so § 6 can drive it over injected candidates without
# breaking this machine's PATH. $1 candidate executable. Prints one problem per line and
# returns the count; on success prints the version string it observed.
#
# THIS GATE IS NOT THE FIRST OR ONLY CHECK OF THE GODOT PIN, said plainly because the
# opposite is an easy thing to assume from a gate that opens by checking it:
# ToolchainInspector.ProbeGodot compares the same prefix, DoctorVerb reports it,
# BootstrapVerb runs doctor after installing (BootstrapVerb.cs:113, "DoctorVerb.Report"),
# and build/verify-verbs.sh drives that path over a deliberately substituted godot. What
# this gate uniquely does is check the pin INSIDE THE GATE, before § 1 launches anything -
# which is still worth doing, because every section below reads evidence a run produces and
# a gate that skips the pin turns "this machine lacks the pinned toolchain" into a list of
# validation failures about the repository. It is an independent, early, in-band check; it
# is not the sole one, and a reader who deletes doctor's check because this exists has
# removed the one a developer actually reads.
classify_engine() {
  local candidate="$1"
  local version status=0 chatter="" stderr_file line trimmed last='(no output)'

  if ! command -v "${candidate}" >/dev/null 2>&1; then
    printf 'the pinned Godot executable %q was not found. Discovery order is MECHAMINER_GODOT then godot on PATH (build/toolchain.json). This is class %s, missing or mismatched pinned environment, and NOT class %s: a gate that returns a validation class for a broken toolchain has told the reader to fix the repository when the machine is what is broken\n' \
      "${candidate}" "${EXIT_ENVIRONMENT}" "${EXIT_VALIDATION}"
    return 1
  fi

  # THIS INVOCATION DELIBERATELY MATCHES ToolchainInspector.ProbeGodot's, ARGUMENT FOR
  # ARGUMENT AND LINE-PICK FOR LINE-PICK, and the match is the point rather than a
  # coincidence of style. ProbeGodot runs `--headless --version` and then takes the LAST
  # NON-EMPTY LINE, with its own comment saying why: "A headless Godot process can print
  # engine chatter before the version, so the last nonempty line is the version string."
  # build/bootstrap-linux.sh:119 reads the same `--headless --version`. Both of those ran
  # green in CI at 7c18787 on a GitHub ubuntu-24.04 runner, which is the evidence that a
  # Godot on a display-less runner reports a stream whose last non-empty line begins with
  # the pin - and it is the only evidence anything here has, since this gate had never run
  # in CI when it was written.
  #
  # The three ways this read used to diverge from the proven one, each of which reddens a
  # PR on an environment verdict while every other gate passes because none of them looks
  # at the version at all:
  #
  #   NO --headless. Display-init chatter is not suppressed on a runner with no display,
  #   so the stream acquires lines nobody predicted.
  #   2>&1 ON THE VERSION READ. A driver or Wayland/X warning on stderr becomes a
  #   candidate line for the prefix test.
  #   THE FIRST LINE INSTEAD OF THE LAST NON-EMPTY ONE. Any preamble line then becomes
  #   `version`, fails the prefix test, and exits class 3.
  #
  # Stderr is captured SEPARATELY and used only in the diagnostics below: it must be able
  # to explain a failure without being able to cause one.
  stderr_file="$(mktemp "${TMPDIR:-/tmp}/verify-run-slice-godot-stderr.XXXXXX")" \
    || stderr_file="/dev/null"
  version="$("${candidate}" --headless --version 2>"${stderr_file}")" || status=$?
  chatter="$(strip_ansi <"${stderr_file}" | tr -d '\r')"
  [[ "${stderr_file}" == "/dev/null" ]] || rm -f -- "${stderr_file}"
  # Flattened by parameter expansion, so a multi-line warning cannot turn one problem
  # description into several and break every caller's one-problem-per-line contract.
  chatter="${chatter//$'\n'/ | }"
  version="$(strip_ansi <<<"${version}" | tr -d '\r')"

  # LAST NON-EMPTY LINE, BY A READ LOOP. Not `| tail -1` and not `| head -1`: under
  # pipefail a downstream command that exits first turns a healthy read into 141, which is
  # the shape this gate refuses everywhere else. A here-string feeds the loop, so the
  # assignments below happen in THIS shell rather than in a pipeline's subshell. Each line
  # is trimmed before being judged non-empty, matching ProbeGodot's LastNonEmptyLine, so a
  # whitespace-only chatter line cannot become the version.
  while IFS= read -r line; do
    trimmed="${line#"${line%%[![:space:]]*}"}"
    trimmed="${trimmed%"${trimmed##*[![:space:]]}"}"
    [[ -n "${trimmed}" ]] && last="${trimmed}"
  done <<<"${version}"
  version="${last}"

  if [[ "${status}" -ne 0 ]]; then
    printf 'the pinned Godot executable %q would not report its version (exit %s), so the pin is unproved rather than satisfied. Class %s. Its stderr said: %s\n' \
      "${candidate}" "${status}" "${EXIT_ENVIRONMENT}" "${chatter:-(nothing)}"
    return 1
  fi

  if [[ "${version}" != "${EXPECTED_VERSION_PREFIX}"* ]]; then
    # The quoted string is the LAST NON-EMPTY line of stdout. If it looks like engine
    # chatter rather than a version, the executable is printing something after its version
    # and the read above is what needs revisiting - not the pin. Stderr is reported beside
    # it, and took no part in the comparison.
    printf 'the executable %q reports %q, which is not the pinned %q from build/toolchain.json. Class %s, not class %s. Its stderr said: %s\n' \
      "${candidate}" "${version}" "${EXPECTED_VERSION_PREFIX}" "${EXIT_ENVIRONMENT}" "${EXIT_VALIDATION}" \
      "${chatter:-(nothing)}"
    return 1
  fi

  printf '%s\n' "${version}"
  return 0
}

# --- the no-mutation measurement's two legs, and its predicate -----------------
#
# TWO LEGS BECAUSE THEY ANSWER DIFFERENT QUESTIONS. "The tracked content did not change"
# and "nothing was written" are not the same claim, and only the second is about writing. A
# clean diff is not evidence of no write, so the gate emits the two as SEPARATE FINDINGS
# rather than folding them into one verdict a reader would over-read.
#
# Both take $1 a repository root and $2.. a pathspec, parameterised rather than closed over
# REPO_ROOT and MUTATION_PATHSPEC on purpose: § 6h drives these same two captures over a
# throwaway git repository under CONTROL_ROOT, so the predicate below can be shown going
# red on a REAL repository difference without this gate writing into the tree it checks.
# VER-PRE-001-007(ii) forbids the latter outright, and a control that broke the rule its own
# gate asserts would be worth less than no control.

# LEG 1, CONTENT. git's own summary of which tracked paths in scope are modified, staged or
# untracked. It is CONTENT-DERIVED BY CONSTRUCTION and therefore blind to writing twice
# over: a deterministic byte-identical rewrite leaves every porcelain line exactly where it
# was, and so does a write followed by a cleanup. It is also a summary of STATUS rather than
# of bytes - a path already modified before the window and modified differently inside it
# prints the same ' M path' at both ends - so this leg's pass message below claims status
# identity and never byte identity. § 6h measures both blindnesses rather than asserting
# them here as prose.
capture_tree_content() {
  local root="$1"
  shift
  (cd -- "${root}" && git status --porcelain -- "$@") 2>&1
}

# LEG 2, METADATA. mtime, inode and size of every TRACKED file in scope, in git's own sorted
# order. This is the leg whose subject is WRITING rather than content: a byte-identical
# rewrite in place moves %Y, and a rewrite through a temp file and a rename moves %i, and
# neither moves anything leg 1 can see. § 6h drives both legs over a real byte-identical
# rewrite and requires exactly that split - leg 1 silent, leg 2 red - so the claim that
# these legs differ is measured on every run instead of being asserted in this comment.
#
# WHAT IT DOES NOT COVER, said rather than implied: untracked files. A file created and
# deleted inside the window appears in neither leg, because git ls-files never named it.
# The pass message says so; widening the leg to the untracked set would make it report every
# build output the launch legitimately produces.
#
# WHY A LOCAL RED HERE MAY NOT BE THIS GATE'S DOING, and why the fix is never to narrow
# MUTATION_PATHSPEC. The pathspec covers tests/verification/*.json and game/tests/*, which
# are files people and other agents edit. The window this leg spans includes § 1's launch,
# so it can be open for up to LAUNCH_TIMEOUT_SECONDS (180). ANY write to a tracked file in
# scope inside that window moves an mtime and reds this leg - including a write this gate
# had nothing to do with.
#
#   IN CI THE HAZARD DOES NOT EXIST. The workflow runs on a clean checkout and nothing else
#   writes to the tree while the job runs, so the tree is static for the whole window and a
#   red in CI means a write by this gate or by the harness it launched. THE HAZARD IS
#   DEVELOPER-LOCAL ONLY.
#
#   SO ON A DEVELOPER MACHINE, THE QUESTION TO ASK IS *WHAT CHANGED*, NOT *IS THE PATHSPEC
#   TOO WIDE*. The verdict names the paths; `git status` and the reported mtimes say whether
#   an editor, a rebase or a sibling agent touched one of them mid-run. Re-run on a quiet
#   tree and the leg goes green.
#
#   NARROWING THE PATHSPEC TO MAKE THIS QUIETER IS THE WRONG FIX. A red about a real write
#   is exactly what this leg exists to produce; a red is truthful even when its cause is a
#   concurrent edit rather than this gate. Dropping paths out of scope trades real coverage
#   of the files this gate reads for the comfort of a green, and the write it was built to
#   catch would then land in a path nobody is watching.
capture_tree_metadata() {
  local root="$1"
  shift
  (
    cd -- "${root}" || exit 3
    # NUL-delimited, so a path containing a space or a newline cannot merge two entries into
    # one and hide a change inside the join. `-r` so an empty file set yields an empty
    # capture rather than a stat over the current directory. No `| head`, no `| sort`: the
    # order is git's and is already deterministic, and a downstream command exiting first is
    # the pipefail-141 shape this gate refuses everywhere else.
    git ls-files -z -- "$@" | xargs -0 -r stat -c "%n"$'\t'"%Y %i %s" --
  ) 2>&1
}

# THE NO-MUTATION PREDICATE, EXTRACTED SO A CONTROL CAN ACTUALLY DRIVE IT.
#
# This used to be an inline `[[ "${before}" == "${after}" ]]` at the end of § 6, which made
# it the one predicate in this gate no fixture could reach: every control writes under
# mktemp -d or artifacts/, both outside MUTATION_PATHSPEC, so the control set was
# STRUCTURALLY INCAPABLE of turning it red, and its unreadable-git branch had never been
# taken at all. Extracting it is the same move evaluate_launch_evidence,
# classify_assertion_count, compare_section_roster and classify_engine already are, and for
# the same reason: § 6 must drive THE SAME FUNCTION the real measurement drives, or the
# control proves something about a copy of the logic instead of about the logic.
#
# $1 a phrase naming what was measured AND of what - it is printed verbatim into the
# verdict, so the subject travels with the finding and a control over a throwaway repository
# cannot claim to have measured game/. $2 the BEFORE capture, $3 the AFTER capture, $4 the
# BEFORE capture's exit status, $5 the AFTER capture's exit status.
#
# Prints one problem per line and returns the count.
classify_tree_mutation() {
  local subject="$1" before="$2" after="$3" before_status="$4" after_status="$5"

  # STATUS BEFORE OUTPUT, and this ordering is the whole reason the statuses are parameters.
  # The empty output of a FAILED capture is indistinguishable from the empty output of a
  # clean one, so two failed captures compare EQUAL and would report a green built entirely
  # out of the failure. Same trap build/verify-godot.sh's own mutation probe had to learn.
  if [[ "${before_status}" -ne 0 || "${after_status}" -ne 0 ]]; then
    printf 'the %s could not be captured (exit %s before, %s after), so "nothing changed" is unproved rather than true: two failed captures compare equal, and a pass built on that comparison would be an artefact of the failure rather than a measurement. Capture text: %s\n' \
      "${subject}" "${before_status}" "${after_status}" "${after//$'\n'/ | }"
    return 1
  fi

  if [[ "${before}" != "${after}" ]]; then
    printf 'the %s changed across the window. Before: %s After: %s\n' \
      "${subject}" "${before//$'\n'/ | }" "${after//$'\n'/ | }"
    return 1
  fi

  return 0
}

# The launch, and the whole of VER-PRE-001-004's conduit.
#
# THE HARNESS'S EXIT CODE IS CAPTURED DIRECTLY AND NOTHING ELSE CAN BECOME IT. There is
# no pipeline here, no command substitution around the engine, no `|| true`, and no grep
# whose own status could be read as the harness's: stdout and stderr go to files by
# redirection and the status arrives through `|| LAUNCH_STATUS=$?` on the launch itself.
# Results come back in globals rather than on stdout for the same reason - a `$(...)`
# wrapper would run the launch in a subshell, and a status captured in a subshell is a
# status that can be lost.
#
# $1 engine executable, $2 project dir, $3 scene, $4 output dir.
LAUNCH_STATUS=0
LAUNCH_STDOUT=""
LAUNCH_TRANSCRIPT_STATE="absent"
launch_harness() {
  local engine="$1" project_dir="$2" scene="$3" output_dir="$4"
  local stdout_file="${output_dir}.stdout.log"

  LAUNCH_STATUS=0
  LAUNCH_STDOUT=""
  LAUNCH_TRANSCRIPT_STATE="absent"

  # Empty, per invocation. rm before mkdir so a directory left by an earlier run of this
  # gate cannot contribute a transcript to this one.
  rm -rf -- "${output_dir}" "${stdout_file}"
  mkdir -p -- "${output_dir}"

  env "${OUTPUT_VARIABLE}=${output_dir}" \
    timeout --kill-after=10s "${LAUNCH_TIMEOUT_SECONDS}s" \
    "${engine}" --headless --path "${project_dir}" "${scene}" --audio-driver Dummy \
    >"${stdout_file}" 2>&1 || LAUNCH_STATUS=$?

  LAUNCH_STDOUT="$(strip_ansi <"${stdout_file}")"
  [[ -f "${output_dir}/${TRANSCRIPT_NAME}" ]] && LAUNCH_TRANSCRIPT_STATE="present"
  return 0
}

# --- § 0: the pinned environment ----------------------------------------------

section "0. the pinned engine is present, and a machine problem is class ${EXIT_ENVIRONMENT} rather than class ${EXIT_VALIDATION} (VER-PRE-001-007(b))"
#
# Early and directly, before any assertion about the repository is attempted. The
# precedent for an early direct exit in this family is build/verify-godot.sh:59-63 (class
# 5, as measured at 42a5c83); no gate defined a class-3 constant before this one, and
# build/bootstrap-linux.sh:49 is the naming precedent only. The reason it must be early:
# every section below reads evidence a run produces, and with no engine there is no run,
# so continuing would turn "this machine lacks the pinned toolchain" into a list of
# validation failures about the repository.
engine_problems=""
engine_version=""
engine_problem_count=0
engine_problems="$(classify_engine "${GODOT}")" || engine_problem_count=$?
if [[ "${engine_problem_count}" -ne 0 ]]; then
  fail "${engine_problems}"
  # No gate_summary: the summary's job is to report findings about the repository, and
  # this is not one. The class is what carries the meaning.
  exit "${EXIT_ENVIRONMENT}"
fi
engine_version="${engine_problems}"
pass "pinned engine ${GODOT} reports ${engine_version}"

if ! command -v python3 >/dev/null 2>&1; then
  fail "python3 is absent, and § 4's third anchor derives the expected section roster from ${REGISTRY_DIR#"${REPO_ROOT}/"}/*.json with it. Without it this gate could still assert the pin, which is precisely the blind two-anchor comparison VER-PRE-001-006 refuses to ship. Class ${EXIT_ENVIRONMENT}"
  exit "${EXIT_ENVIRONMENT}"
fi
pass "python3 is present, so the registry-derived roster anchor can be computed"

mkdir -p -- "${EVIDENCE_DIR}"

# --- the no-mutation window OPENS HERE, before § 1's launch --------------------
#
# THE PLACEMENT IS THE POINT, and getting it wrong is the largest defect this commit
# repairs. The BEFORE capture used to be taken at the top of § 6, AFTER § 1's real launch of
# the harness. The window therefore spanned § 6's controls AND NOTHING ELSE, so the gate
# made no claim whatever about the question that actually matters: whether launching the
# harness mutated the tree this gate checks. It asserted that its own fixtures were tidy
# while saying nothing about the subject under test.
#
# Taken here, the window spans § 1'S REAL LAUNCH OF THE HARNESS AND EVERY CONTROL IN § 6,
# and the verdicts at the end of § 6 say so in those words rather than describing the
# controls alone.
#
# MEASURED AS BEFORE-AGAINST-AFTER AND NOT AGAINST THE COMMITTED TREE, which is a correction
# rather than a preference: a status compared against HEAD reports whatever the person
# running the gate has in their working tree - this file, while it was being written, was
# itself enough to turn that comparison red - and a check that fails for a reason
# unconnected to the window is worse than no check. What is asserted is that the launch and
# the controls changed nothing, so the comparison has to be against what the tree looked
# like before the launch started. A DIRTY-BUT-UNCHANGED TREE IS NOT A FAILURE HERE, and
# § 6g's second case is the control that proves it.
#
# If the launch ever does dirty something in scope, that is a TRUE FINDING about the harness
# and this gate should go red for it. Do not widen MUTATION_PATHSPEC and do not add an
# exclusion to quiet it: the exclusion would be permanent and the finding is the point.
control_tree_content_before=""
control_tree_content_before_status=0
control_tree_content_before="$(capture_tree_content "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || control_tree_content_before_status=$?
control_tree_metadata_before=""
control_tree_metadata_before_status=0
control_tree_metadata_before="$(capture_tree_metadata "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || control_tree_metadata_before_status=$?

# --- § 1: the launch -----------------------------------------------------------

section "1. the gate launches the run-slice evidence scene by name, the harness reaches managed code, and the transcript is this run's (VER-PRE-001-003)"

if [[ -f "${HARNESS_SCENE_FILE}" ]]; then
  pass "the scene this gate names, ${HARNESS_SCENE}, resolves to ${HARNESS_SCENE_FILE#"${REPO_ROOT}/"}"
else
  # VER-PRE-001-007(a): the unresolvable path is NAMED, and the class is validation.
  fail "the scene this gate names, ${HARNESS_SCENE}, is unresolvable: ${HARNESS_SCENE_FILE#"${REPO_ROOT}/"} does not exist. This is a missing scene and not a harness that produced no output, and the two must not be reported alike"
fi

readonly PRIMARY_OUTPUT_DIR="${EVIDENCE_DIR}/run"

# THE DELETION IS THE GUARANTEE, AND THERE IS DELIBERATELY NO ASSERTION HERE.
#
# What used to sit on these lines was a pass/fail pair asserting that transcript.tsv was
# ABSENT - two lines after this script had itself deleted and recreated the directory. NO
# INJECTION COULD HAVE FAILED IT: it re-read a state it had just established, so the `fail`
# branch was unreachable and the `pass` was free. That is the same shape as the
# interpolation assertion removed from RunSliceEvidenceHarness.cs, which compared a convex
# interpolant against its own two endpoints and held by arithmetic whatever the production
# code did. AN UNFAILABLE ASSERTION SITTING IN A LIST OF ASSERTIONS IS WORSE THAN NO
# ASSERTION, because it inflates the count and reads as coverage. So the pair is gone and
# the rm -rf carries the claim by itself.
#
# THE CLAIM IS UNDIMINISHED BY THAT REMOVAL, and it remains the strongest test in this file.
# Because the directory is emptied HERE and the transcript is required to be PRESENT after
# the launch (evaluate_launch_evidence, reported by § 1's finding below), "present
# afterwards" can only mean THIS INVOCATION WROTE IT. That is an EVENT claim rather than a
# state claim, and it is the thing that stops a developer's retained hand-run transcript
# from answering every question this gate asks - trap 2 in the header. Deleting the
# unfailable check removes a miscounted guarantee, not a real one.
#
# If the rm -rf or the mkdir were to fail, the launch would find no directory to write into,
# no transcript would appear, and § 1's transcript finding below goes red. The failure is
# still reported; it is simply reported by the check that can actually observe it.
rm -rf -- "${PRIMARY_OUTPUT_DIR}"
mkdir -p -- "${PRIMARY_OUTPUT_DIR}"
printf '      note: %s was emptied and recreated immediately before the launch, so "%s present afterwards" means this invocation wrote it. That guarantee comes from the deletion, not from a comparison, and is deliberately not emitted as an assertion.\n' \
  "${PRIMARY_OUTPUT_DIR#"${REPO_ROOT}/"}" "${TRANSCRIPT_NAME}"

launch_harness "${GODOT}" "${GAME_DIR}" "${HARNESS_SCENE}" "${PRIMARY_OUTPUT_DIR}"
readonly PRIMARY_TRANSCRIPT="${PRIMARY_OUTPUT_DIR}/${TRANSCRIPT_NAME}"

# CAPTURED OUT OF THE GLOBAL IMMEDIATELY, before anything else can touch it. § 6's
# controls drive the same launch_harness and therefore overwrite LAUNCH_STATUS; the class
# this script propagates is the one THIS launch returned, and it is read from here on.
readonly HARNESS_STATUS="${LAUNCH_STATUS}"
readonly HARNESS_STDOUT="${LAUNCH_STDOUT}"
readonly HARNESS_TRANSCRIPT_STATE="${LAUNCH_TRANSCRIPT_STATE}"

launch_problems=""
launch_problem_count=0
launch_problems="$(evaluate_launch_evidence "${HARNESS_STDOUT}" "${HARNESS_TRANSCRIPT_STATE}")" \
  || launch_problem_count=$?
if [[ "${launch_problem_count}" -eq 0 ]]; then
  pass "the harness printed StartupLine and wrote a fresh ${TRANSCRIPT_NAME} this invocation"
else
  fail "the launch of ${HARNESS_SCENE} produced no usable evidence of a run: ${launch_problems//$'\n'/; }"
  printf '%s\n' "${HARNESS_STDOUT}" | tail -20 | sed 's/^/      /'
fi

# --- § 2: the conduit ----------------------------------------------------------

section "2. the harness's own exit code reaches the caller unchanged (VER-PRE-001-004)"
#
# The harness exits 0 when its failure count is zero, 4 on assertion failure
# (RunSliceEvidenceHarness.cs:114 at 42a5c83) and 2 when OutputDirectoryVariable names
# nothing (:77). What is asserted here is that the number arrives: § 6's stub-engine
# controls drive this same launch_harness over engines that exit 0, 2 and 4 with large
# logs, and require the captured status to be exactly that.
if [[ "${HARNESS_STATUS}" =~ ^[0-9]+$ ]]; then
  pass "the launch's exit status was captured as an integer (${HARNESS_STATUS}) rather than lost to a pipeline or a subshell"
else
  fail "the launch's exit status did not arrive as an integer (${HARNESS_STATUS}); the conduit this gate exists to prove is broken"
fi

# PROPAGATED_CLASS is what this script will exit with when the HARNESS is what failed,
# and it is deliberately not always EXIT_VALIDATION. Empty means "the harness chose no
# failing class", in which case the gate's own findings decide the exit through
# gate_summary.
propagated_class=""
case "${HARNESS_STATUS}" in
  0)
    pass "the harness exited 0, so its own verdict is that every assertion it evaluated held"
    ;;
  "${EXIT_INVALID_INVOCATION}")
    fail "the harness exited ${EXIT_INVALID_INVOCATION}, its refusal when ${OUTPUT_VARIABLE} names no directory. This gate set that variable, so either it did not survive to the process or a --mechaminer-run-slice-output= user arg overrode it. That class is passed through unchanged rather than translated to ${EXIT_VALIDATION}: a caller reading ${EXIT_VALIDATION} would look for a failing assertion, and there is none"
    propagated_class="${EXIT_INVALID_INVOCATION}"
    ;;
  "${EXIT_VALIDATION}")
    fail "the harness exited ${EXIT_VALIDATION}: an assertion inside it failed, and that class reaches this gate's caller unchanged rather than being swallowed. The failing assertion names follow"
    propagated_class="${EXIT_VALIDATION}"
    printf '%s\n' "$(grep -E '^FAIL'$'\t' -- "${PRIMARY_TRANSCRIPT}" 2>/dev/null || true)" | sed 's/^/      /'
    ;;
  124 | 137)
    # This bound is THIS SCRIPT'S, not the harness's, so the class is this script's
    # finding and is not presented as a class the harness chose. GodotImportVerb's own
    # 20-minute TimedOut branch pre-empts every exit code with class 5 and is a separate
    # exception, named in the header.
    fail "the harness was terminated at the ${LAUNCH_TIMEOUT_SECONDS}s bound THIS SCRIPT imposed (exit ${HARNESS_STATUS}), so it chose no class of its own; an atomically written transcript means it left none rather than a truncated one"
    ;;
  *)
    fail "the harness exited ${HARNESS_STATUS}, which is none of its declared classes (0, ${EXIT_INVALID_INVOCATION}, ${EXIT_VALIDATION}) and none of a bounded termination (124, 137). An unclassified code is not passed through: doc 100's set is closed, and forwarding an unknown number would leak an unclassified failure into the verb"
    ;;
esac

# --- § 3: the count exists, parses, and is non-vacuous -------------------------

section "3. the transcript's assertions_run line exists, parses as a non-negative integer, and is greater than zero (VER-PRE-001-005)"
#
# Read from the TRANSCRIPT and not from stdout, because no transcript content reaches
# stdout: the harness prints the startup line, a one-line summary and the transcript path.
transcript_text=""
transcript_read_status=0
if [[ -f "${PRIMARY_TRANSCRIPT}" ]]; then
  transcript_text="$(cat -- "${PRIMARY_TRANSCRIPT}")" || transcript_read_status=$?
else
  transcript_read_status=1
fi

assertions_run=""
if [[ "${transcript_read_status}" -ne 0 ]]; then
  fail "there is no readable ${TRANSCRIPT_NAME} to read assertions_run from, so the count is unproved rather than zero"
  not_reached "3. the assertions_run line's existence, parse and non-vacuity"
else
  count_report=""
  count_problem=0
  count_report="$(classify_assertion_count "${transcript_text}")" || count_problem=$?
  if [[ "${count_problem}" -eq 0 ]]; then
    assertions_run="${count_report#assertions_run=}"
    pass "assertions_run exists, parses as a non-negative integer, and is ${assertions_run} which is greater than zero"
  else
    fail "${count_report}"
  fi
fi

# --- § 4: three independent anchors on the assertion set -----------------------

section "4. the evaluated-assertion count holds against a committed pin AND against a section roster derived from the registry (VER-PRE-001-006)"
#
# THREE ANCHORS, AND THE THIRD IS THE POINT. A pin both sides of which come from the same
# run is blind, and so is a set equality between two lists one author edits in one commit
# (doc 91 § Negative control adequacy, third bullet: such an invariant "needs a third
# anchor that names the expected members or their count independently of either side").
#
#   Anchor 1 is this run: the transcript's assertions_run value, read in § 3.
#   Anchor 2 is EXPECTED_ASSERTIONS above, restated by hand in this repository.
#   Anchor 3 is independent of both and lives in files this package does not own: the
#   expected section roster derived from tests/verification/*.json.
#
# Anchors 1 and 2 alone are the blind pair - they catch a shrunk set and miss a renamed,
# relocated or substituted assertion, and they miss a section dropped in the same commit
# that decrements the pin. What anchor 3 buys, said plainly rather than overclaimed: it is
# not tamper-proof, because one author can edit the harness, the pin and another package's
# registry in one commit. It buys that doing so is a diff touching PRE-002.json or
# UI-002.json, which lands in another package's review, where a two-line pin bump would
# not.
if [[ -z "${assertions_run}" ]]; then
  not_reached "4. anchor 1 against anchor 2 (no usable assertions_run value)"
elif [[ "${assertions_run}" -eq "${EXPECTED_ASSERTIONS}" ]]; then
  pass "anchor 1 (${assertions_run} evaluated) equals anchor 2 (EXPECTED_ASSERTIONS=${EXPECTED_ASSERTIONS})"
else
  fail "anchor 1 says ${assertions_run} assertions were evaluated and anchor 2 pins ${EXPECTED_ASSERTIONS}. Re-derive the total from the Check( call sites rather than editing the pin to match: the pin's own comment records that a repair to the interpolation section is expected to move it, and it records what the 28 was a count of and at which sha"
fi

roster_text=""
roster_status=0
roster_text="$(registry_section_roster "${REGISTRY_DIR}" 2>&1)" || roster_status=$?
roster_count=0
[[ -n "${roster_text}" ]] && roster_count="$(grep -c . <<<"${roster_text}")"

if [[ "${roster_status}" -ne 0 ]]; then
  # A failed derivation is not an empty roster. The empty result of a FAILED enumeration
  # is indistinguishable from the empty result of a clean one, so the status is checked
  # before the output is interpreted - the same trap build/verify-godot.sh's mutation
  # probe had to learn.
  fail "the registry-derived roster could not be computed (exit ${roster_status}), so anchor 3 is unproved rather than satisfied: ${roster_text}"
  not_reached "4. anchor 3, set equality between the registry roster and the transcript's headings"
elif [[ "${roster_count}" -eq 0 ]]; then
  # WHAT THIS GUARD IS AND IS NOT. It is a belt: it refuses to proceed on an empty
  # expectation rather than reporting whatever an empty expectation happens to produce. It
  # is NOT the only thing standing between an empty roster and a vacuous green - § 6c's last
  # control drives compare_section_roster with an empty roster and requires it to red, so the
  # predicate's own behaviour on the empty case is now measured rather than assumed. The
  # guard stays because an empty DERIVATION is a distinct finding from an empty comparison:
  # it means the registry stopped naming this harness at all, and that deserves its own
  # sentence rather than three per-heading complaints.
  fail "the registry-derived roster is EMPTY: no entry under ${REGISTRY_DIR#"${REPO_ROOT}/"} has an engine-scene selector naming '${HARNESS_SCENE_FILE#"${REPO_ROOT}/"} § <section>'. An empty expectation would make the set equality below hold vacuously, which is the blindness anchor 3 exists to remove"
  not_reached "4. anchor 3, set equality between the registry roster and the transcript's headings"
elif [[ "${transcript_read_status}" -ne 0 ]]; then
  not_reached "4. anchor 3 (no readable transcript to compare the roster against)"
else
  pass "anchor 3's expectation was derived independently: ${roster_count} section(s) from the registry's engine-scene selectors ($(paste -sd, - <<<"${roster_text}"))"
  if [[ "${roster_count}" -ne "${ROSTER_SIZE_AT_42A5C83}" ]]; then
    # Reported, never asserted. The roster is derived on purpose; a committed count of it
    # would be a fourth thing to edit and would defeat the independence.
    printf '      note: %s section(s) derived, where %s was measured at 42a5c83. Not a failure - the roster is derived rather than pinned - but worth a reader knowing the corpus moved.\n' \
      "${roster_count}" "${ROSTER_SIZE_AT_42A5C83}"
  fi

  roster_problems=""
  roster_problem_count=0
  roster_problems="$(compare_section_roster "${roster_text}" "${transcript_text}")" \
    || roster_problem_count=$?
  if [[ "${roster_problem_count}" -eq 0 ]]; then
    pass "set equality holds in both directions between the registry roster and the transcript's '## ' headings, and every heading contains at least one PASS or FAIL line"
  else
    fail "anchor 3 is red in ${roster_problem_count} place(s)"
    printf '%s\n' "${roster_problems}" | sed 's/^/      /'
  fi
fi

# --- § 5: the run's evidence is retained --------------------------------------

section "5. the transcript this run produced is retained where the workflow uploads it"
if [[ -f "${PRIMARY_TRANSCRIPT}" ]]; then
  pass "transcript retained at ${PRIMARY_TRANSCRIPT#"${REPO_ROOT}/"} ($(wc -l <"${PRIMARY_TRANSCRIPT}" | tr -d ' ') lines), under artifacts/ which fast.yml's 'retain the verb evidence' step uploads"
else
  fail "no transcript was retained, so nothing corroborates this gate's own execution and the run-manifest gap deferred to FND-005 is the only instrument left"
fi

# --- § 6: negative controls ----------------------------------------------------

section "6. negative controls: every predicate above can actually fail (Decision 11 rule 4)"
#
# In band, on every run, driving THE SAME FUNCTIONS §§ 0-4 drove over injected fixtures.
# Table-driven from readonly pipe-delimited arrays, and every log-reading case runs twice:
# once at fixture size and once at ~300 KB with the marker late in the stream. The
# production-sized case is the point rather than thoroughness for its own sake - the
# `printf | grep -q` shape this gate refuses reports 141 once the log is large enough that
# printf still has a write to do when grep exits, so a one-line fixture is a control that
# structurally cannot fail here, and a real harness log is tens of kilobytes.
#
# NO CONTROL MUTATES THE TREE THIS GATE CHECKS. VER-PRE-001-007(ii) forbids it outright
# and build/verify-gate-wiring.sh's renamed-call-site control (:1532-1552 as measured at
# 42a5c83) is the pattern: copy the roots into a temp dir, mutate the copy, point the same
# analyser at the copy. § 6h extends that pattern to the no-mutation predicate itself, with a
# throwaway git repository standing in for the tree.
#
# THE BEFORE HALF OF THE NO-MUTATION MEASUREMENT IS NOT TAKEN HERE. It is taken before § 1's
# launch, so the window covers the launch as well as these controls; see the block above
# § 1. Taking it here is the defect this commit repairs, not the design.
readonly CONTROL_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/verify-run-slice-controls.XXXXXX")"
cleanup_controls() {
  rm -rf -- "${CONTROL_ROOT}"
}
trap cleanup_controls EXIT

readonly LOG_FILLER='  --- Debug adapter server started on port 6006 ---   at Godot.NativeInterop.NativeFuncs.godotsharp_internal_object_disposed'

pad_to() {
  # $1 target bytes, $2 seed text. Prints the seed grown with filler lines.
  local target="$1" text="$2"
  while [[ "${#text}" -lt "${target}" ]]; do
    text+=$'\n'"${LOG_FILLER}"
  done
  printf '%s' "${text}"
}

expect_control_red() {
  # $1 label, $2 expected problem count, $3 observed count, $4 report text
  local label="$1" want="$2" got="$3" report="$4"
  controls_run=$((controls_run + 1))
  if [[ "${got}" -eq "${want}" ]]; then
    control_pass "control: ${label} -> ${got} problem(s), as designed"
    [[ -n "${report}" ]] && control_detail <<<"${report}"
  else
    control_fail "control: ${label} produced ${got} problem(s) where ${want} was designed, so the check it exercises does not read what this gate believes it reads"
    [[ -n "${report}" ]] && control_detail <<<"${report}"
  fi
}

# --- 6a. the launch-evidence read (VER-PRE-001-003(b) and (c)) -----------------
# "<label>|<startup line: exact|near-miss|absent>|<transcript: present|absent>|<expected problems>"
readonly LAUNCH_EVIDENCE_CONTROLS=(
  "a healthy run: StartupLine present and a fresh transcript|exact|present|0"
  "the coherent VER-PRE-001-007(c) case: the harness started and never reached managed code|absent|absent|2"
  "managed code reached but no transcript written|exact|absent|1"
  "a near-miss startup line, which must NOT satisfy the read|near-miss|present|1"
)

for control in "${LAUNCH_EVIDENCE_CONTROLS[@]}"; do
  IFS='|' read -r label startup_kind transcript_state want <<<"${control}"
  for target_bytes in 0 300000; do
    synthetic="$(pad_to "${target_bytes}" \
      "Godot Engine v${EXPECTED_VERSION_PREFIX}.a13da4feb - https://godotengine.org")"
    # Markers last, so the read must traverse the whole log to answer.
    case "${startup_kind}" in
      exact) synthetic+=$'\n'"${STARTUP_LINE}" ;;
      # A near miss that does NOT contain StartupLine as a substring. "readyish" would,
      # and would make this control structurally unable to fail.
      near-miss) synthetic+=$'\n'"MechaMiner: run slice evidence harness is ready" ;;
      absent) : ;;
    esac
    report=""
    got=0
    report="$(evaluate_launch_evidence "${synthetic}" "${transcript_state}")" || got=$?
    expect_control_red "${label} (${#synthetic} bytes of log)" "${want}" "${got}" "${report}"
  done
done

# --- 6b. the assertions_run classifier (VER-PRE-001-005) ----------------------
# VER-PRE-001-005's three controls, plus the healthy case. (ii) and (iii) are what make
# the existence and parse assertions real rather than defensive prose: without them the
# gate has a branch nobody has seen taken.
# "<label>|<the assertions_run value, or the literal ABSENT>|<expected problems>"
readonly COUNT_CONTROLS=(
  "a healthy transcript carrying the pinned count|${EXPECTED_ASSERTIONS}|0"
  "control (i), non-vacuity: every section returned before its first check|0|1"
  "control (ii), the assertions_run line deleted, which must NOT be read as a count of 0|ABSENT|1"
  "control (iii), an unparseable value, which must NOT be coerced|n/a|1"
)

for control in "${COUNT_CONTROLS[@]}"; do
  IFS='|' read -r label value want <<<"${control}"
  for target_bytes in 0 300000; do
    # Tabs are real tabs: the transcript is TSV and every read below is anchored on one,
    # so a fixture written with a literal backslash-t would be a control that cannot fail.
    synthetic="$(pad_to "${target_bytes}" "# MechaMiner run slice evidence")"
    synthetic+=$'\n## camera-shows-24-metres-vertically'
    synthetic+=$'\nPASS\tprojection-is-orthographic\tobserved'
    [[ "${value}" != "ABSENT" ]] && synthetic+=$'\n'"assertions_run"$'\t'"${value}"
    synthetic+=$'\nassertions_failed\t0'
    synthetic+=$'\noutcome\tpassed'
    report=""
    got=0
    report="$(classify_assertion_count "${synthetic}")" || got=$?
    expect_control_red "${label} (${#synthetic} bytes of transcript)" "${want}" "${got}" "${report}"
  done
done

# --- 6c. the registry roster comparison (VER-PRE-001-006 anchor 3) ------------
# The controls VER-PRE-001-006 names: a silent shrink shows up as a heading with no
# assertion line, and a roster drift shows up as a rename in both directions at once.
#
# THE EMPTY ROSTER IS NOW ONE OF THEM, and it was not before. § 4's guard reds an empty
# registry-derived roster because "an empty expectation would make the set equality below
# hold vacuously", and every roster fixture here was a hardcoded non-empty literal - so the
# empty case was the one input the predicate was never handed. The last row drives
# compare_section_roster with an EMPTY roster and requires 3 problems, one per transcript
# heading no registry entry now names. Note what that measures and what it does not: it
# proves the PREDICATE does not go vacuously quiet on an empty expectation, which is the
# substance of § 4's concern. § 4's own `roster_count -eq 0` branch is still an inline guard
# in the main body and is still not driven by a fixture; it is a cheap belt to the predicate's
# braces, and this control is why the braces are now known to hold.
# "<label>|<mutation>|<roster: standard|empty>|<expected problems>"
readonly ROSTER_CONTROLS=(
  "the roster and the headings agree, every heading carrying an assertion|none|standard|0"
  "control (iii), roster drift: a heading renamed without touching the registry|rename|standard|2"
  "control (i), silent shrink: a section entered whose checks all returned early|empty-section|standard|1"
  "a transcript section no registry entry names, which must also be red|extra-section|standard|1"
  "an EMPTY roster, which must NOT hold vacuously: every heading becomes one no registry entry names|none|empty|3"
)

readonly CONTROL_ROSTER=$'camera-shows-24-metres-vertically\ndeadzone-remaps-radially\ninterpolation-is-presentation-only'

for control in "${ROSTER_CONTROLS[@]}"; do
  IFS='|' read -r label mutation roster_kind want <<<"${control}"
  control_roster="${CONTROL_ROSTER}"
  [[ "${roster_kind}" == "empty" ]] && control_roster=""
  for target_bytes in 0 300000; do
    synthetic="$(pad_to "${target_bytes}" "# MechaMiner run slice evidence")"
    first_heading='## camera-shows-24-metres-vertically'
    [[ "${mutation}" == "rename" ]] \
      && first_heading='## camera-shows-24-metres-vertically-renamed-by-a-control'
    synthetic+=$'\n'"${first_heading}"
    synthetic+=$'\nPASS\tprojection-is-orthographic\tobserved'
    synthetic+=$'\n## deadzone-remaps-radially'
    [[ "${mutation}" == "empty-section" ]] \
      || synthetic+=$'\nPASS\tdeadzone-is-0.18\tobserved'
    synthetic+=$'\n## interpolation-is-presentation-only'
    synthetic+=$'\nPASS\tinterpolation-stays-between-the-two-committed-anchors\tobserved'
    [[ "${mutation}" == "extra-section" ]] \
      && synthetic+=$'\n## a-section-no-registry-entry-names\nPASS\tsomething\tobserved'
    report=""
    got=0
    report="$(compare_section_roster "${control_roster}" "${synthetic}")" || got=$?
    expect_control_red "${label} (${#synthetic} bytes of transcript)" "${want}" "${got}" "${report}"
  done
done

# --- 6d. the engine pin (VER-PRE-001-007(b)) ----------------------------------
# Control (iii) without breaking this machine's PATH: the same classify_engine that § 0
# ran, over injected candidates. A stub reporting the pinned string proves the predicate
# accepts as well as refuses, which is what stops "everything is class 3" passing here.
#
# AND ONE STUB THAT PRINTS CHATTER BEFORE ITS VERSION, which is the control the accept
# case alone cannot be: a stub whose ONLY stdout line is the version is satisfied by a
# first-line read and by a last-non-empty-line read alike, so it proves nothing about which
# line classify_engine takes. The chatty stub reproduces what ProbeGodot's comment says a
# headless Godot does on a display-less runner - engine chatter, then the version - so a
# regression back to `${version%%$'\n'*}` turns this control red instead of turning a CI
# run red. Its stderr line is there for the other half: a warning on stderr must not become
# a candidate for the prefix test, and the accept has to survive a noisy stderr.
cat >"${CONTROL_ROOT}/godot-pinned" <<PINNED
#!/usr/bin/env bash
# A stub reporting the pinned version. Written and removed by build/verify-run-slice.sh.
printf '%s\n' "${EXPECTED_VERSION_PREFIX}.a13da4feb"
PINNED
cat >"${CONTROL_ROOT}/godot-chatty" <<CHATTY
#!/usr/bin/env bash
# A stub reporting the pinned version AFTER engine chatter, with a driver warning on
# stderr. Written and removed by build/verify-run-slice.sh. The blank line is deliberate:
# the read must skip it rather than take it as the last line.
printf 'Godot Engine v%s.a13da4feb - https://godotengine.org\n' "${EXPECTED_VERSION_PREFIX}"
printf 'OpenGL API 3.3.0 NVIDIA 550.54.14 - Compatibility - Using Device: NVIDIA\n'
printf 'WARNING: This method is deprecated\n' >&2
printf 'ERROR: Cannot open X display, DISPLAY is not set\n' >&2
printf '4.6.0.stale.mono.official.notthepin\n' >&2
printf '\n'
printf '%s\n' "${EXPECTED_VERSION_PREFIX}.a13da4feb"
CHATTY
cat >"${CONTROL_ROOT}/godot-unpinned" <<'UNPINNED'
#!/usr/bin/env bash
# A stub reporting a version that is not the pin. Written by build/verify-run-slice.sh.
printf '%s\n' "4.6.0.stable.mono.official.deadbeef"
UNPINNED
chmod +x "${CONTROL_ROOT}/godot-pinned" "${CONTROL_ROOT}/godot-chatty" \
  "${CONTROL_ROOT}/godot-unpinned"

# "<label>|<candidate>|<expected problems>"
readonly ENGINE_CONTROLS=(
  "a stub reporting the pinned version is accepted|${CONTROL_ROOT}/godot-pinned|0"
  "a stub printing engine chatter, a blank line and a stderr warning BEFORE the pinned version is accepted, so the read takes the last non-empty stdout line and not the first|${CONTROL_ROOT}/godot-chatty|0"
  "control (iii), an absent executable|${CONTROL_ROOT}/godot-does-not-exist|1"
  "control (iii), an executable that is not the pinned Godot|${CONTROL_ROOT}/godot-unpinned|1"
)

for control in "${ENGINE_CONTROLS[@]}"; do
  IFS='|' read -r label candidate want <<<"${control}"
  report=""
  got=0
  report="$(classify_engine "${candidate}")" || got=$?
  expect_control_red "${label}" "${want}" "${got}" "${report}"
done

# --- 6e. the exit-code conduit (VER-PRE-001-004) ------------------------------
# THE CONTROL FOR THE CONDUIT, and the one this gate would be worthless without. The
# perturbation VER-PRE-001-004 names for its own control - moving MovementInputAdapter's
# RadialDeadzone and rebuilding - is a production-code change with a forced rebuild, and
# it is not something a gate may do to the tree it is checking. What is controllable in
# band, and what is actually at risk here, is the conduit: whether a harness exit code
# survives the launch wrapper. So the SAME launch_harness runs against stub engines that
# exit 0, 2 and 4 after writing ~300 KB of log, and the captured status must be exactly
# theirs. A `|| true`, a pipeline, or a grep's own status would show up here as 0.
#
# WHAT THIS CONTROL DOES NOT PROVE, stated so nobody reads more into it: that a real
# failing harness assertion produces exit 4. That is the harness's own contract
# (RunSliceEvidenceHarness.cs:114 at 42a5c83, "Quit(_assertionsFailed == 0 ? 0 : 4)"),
# and VER-PRE-001-004's expected-red is the way to observe it end to end through the verb.
cat >"${CONTROL_ROOT}/stub-engine" <<'STUB'
#!/usr/bin/env bash
# A stub engine, written and removed by build/verify-run-slice.sh. It ignores every
# argument, writes a production-sized log, optionally writes a transcript, and exits with
# the class named by STUB_EXIT.
for ((i = 0; i < 2600; i++)); do
  printf '  --- stub engine log line %s ---\n' "$i"
done
printf 'MechaMiner: run slice evidence harness ready\n'
if [[ -n "${MECHAMINER_RUN_SLICE_OUTPUT:-}" && "${STUB_WRITE_TRANSCRIPT:-no}" == "yes" ]]; then
  printf 'assertions_run\t28\nassertions_failed\t0\noutcome\tpassed\n' \
    >"${MECHAMINER_RUN_SLICE_OUTPUT}/transcript.tsv"
fi
exit "${STUB_EXIT:-0}"
STUB
chmod +x "${CONTROL_ROOT}/stub-engine"

# "<label>|<stub exit class>|<writes a transcript: yes|no>"
readonly CONDUIT_CONTROLS=(
  "a harness exiting 0 through the launch wrapper|0|yes"
  "a harness exiting 2, its ${OUTPUT_VARIABLE} refusal|2|no"
  "a harness exiting 4, its assertion-failure class|4|yes"
)

for control in "${CONDUIT_CONTROLS[@]}"; do
  IFS='|' read -r label stub_exit writes <<<"${control}"
  expected_state="absent"
  [[ "${writes}" == "yes" ]] && expected_state="present"
  for attempt in first second; do
    # Exported explicitly rather than prefixed onto the function call: launch_harness runs
    # the engine through `env`, which forwards the environment it was given, so the stub's
    # settings have to be in it.
    export STUB_EXIT="${stub_exit}" STUB_WRITE_TRANSCRIPT="${writes}"
    launch_harness "${CONTROL_ROOT}/stub-engine" "${GAME_DIR}" "${HARNESS_SCENE}" \
      "${CONTROL_ROOT}/conduit-${stub_exit}-${attempt}"
    unset STUB_EXIT STUB_WRITE_TRANSCRIPT
    conduit_problems=()
    [[ "${LAUNCH_STATUS}" -eq "${stub_exit}" ]] \
      || conduit_problems+=("the captured status is ${LAUNCH_STATUS} and the engine exited ${stub_exit}, so the conduit loses or rewrites the class")
    grep -qF -- "${STARTUP_LINE}" <<<"${LAUNCH_STDOUT}" \
      || conduit_problems+=("the startup line was not read out of a $((${#LAUNCH_STDOUT} / 1024)) KB log, which is the pipefail-141 shape this gate refuses")
    [[ "${LAUNCH_TRANSCRIPT_STATE}" == "${expected_state}" ]] \
      || conduit_problems+=("the transcript state was read as ${LAUNCH_TRANSCRIPT_STATE} where ${expected_state} was designed")
    controls_run=$((controls_run + 1))
    if [[ "${#conduit_problems[@]}" -eq 0 ]]; then
      control_pass "control: ${label} (${attempt} attempt, $((${#LAUNCH_STDOUT} / 1024)) KB of log) -> captured exactly ${LAUNCH_STATUS}"
    else
      control_fail "control: ${label} (${attempt} attempt): $(printf '%s; ' "${conduit_problems[@]}")"
    fi
  done
done

# --- 6f. the coherent primary control, and the absent scene -------------------
# VER-PRE-001-007(i), the coherent injection and the primary control: launch a scene that
# EXISTS, loads, runs and exits 0, so nothing about the environment is broken and the
# subprocess succeeds. The gate must still go red, naming the absent startup line and the
# absent transcript. THE GUARDED EFFECT IS ASSERTED AND NOT ONLY THE GUARD: the output
# directory must be EMPTY afterwards, because a gate that goes red while a stale transcript
# sits in the directory has proved only that a guard fired.
#
# EMPTINESS, NOT ONE FILENAME. This used to test `! -f .../transcript.tsv` while the sentence
# above it claimed the directory contained no transcript - a state claim narrower than its
# own comment, and one that cannot distinguish WROTE-THEN-CLEANED from NEVER-WROTE for any
# name but that one. A scene that wrote transcript.tsv.tmp, or a partial file under any other
# name, was invisible to it. Both checks now run: the directory must hold no entry at all,
# and the transcript name is still called out separately because its presence specifically
# would mean § 1's read had been answered by an artifact a control left behind. Note that
# launch_harness puts the stdout log at "${output_dir}.stdout.log" - a SIBLING of the
# directory rather than a child - so the gate's own bookkeeping cannot make this emptiness
# check fail.
readonly COHERENT_SCENE="res://tests/GodotTestRunner.tscn"
launch_harness "${GODOT}" "${GAME_DIR}" "${COHERENT_SCENE}" "${CONTROL_ROOT}/coherent"
coherent_problems=()
coherent_report=""
coherent_count=0
coherent_report="$(evaluate_launch_evidence "${LAUNCH_STDOUT}" "${LAUNCH_TRANSCRIPT_STATE}")" \
  || coherent_count=$?
[[ "${coherent_count}" -eq 2 ]] \
  || coherent_problems+=("the launch-evidence read reported ${coherent_count} problem(s) where 2 were designed")
[[ ! -f "${CONTROL_ROOT}/coherent/${TRANSCRIPT_NAME}" ]] \
  || coherent_problems+=("a ${TRANSCRIPT_NAME} is sitting in the output directory, so a red here would prove only that a guard fired")
# -A rather than a glob, because a glob skips dotfiles and a partial write named
# .transcript.swp is exactly the case this widening exists to catch. The status is read
# before the output is interpreted: an unlistable directory is not an empty one, and the
# empty output of a failed ls is indistinguishable from the empty output of a clean one.
coherent_entries=""
coherent_entries_status=0
coherent_entries="$(ls -A -- "${CONTROL_ROOT}/coherent" 2>/dev/null)" \
  || coherent_entries_status=$?
if [[ "${coherent_entries_status}" -ne 0 ]]; then
  coherent_problems+=("the output directory could not be listed (exit ${coherent_entries_status}), so 'the scene wrote nothing' is unproved rather than true")
elif [[ -n "${coherent_entries}" ]]; then
  coherent_problems+=("the output directory is NOT EMPTY - it holds ${coherent_entries//$'\n'/, } - so this control cannot tell a scene that wrote nothing from one that wrote under a name this check does not happen to know")
fi
controls_run=$((controls_run + 1))
if [[ "${#coherent_problems[@]}" -eq 0 ]]; then
  control_pass "control: launching ${COHERENT_SCENE} - a scene that exists and exits ${LAUNCH_STATUS} - is red on the startup line and the transcript, and the output directory holds no entry of any name afterwards"
  control_detail <<<"${coherent_report}"
else
  control_fail "control: the coherent VER-PRE-001-007(i) case: $(printf '%s; ' "${coherent_problems[@]}")"
  control_detail <<<"${coherent_report}"
fi

# VER-PRE-001-007(ii), the absent scene, IN A COPY. The scene is renamed in a copy of the
# game directory and the same launch_harness is pointed at the copy; the tree this gate
# checks is never touched.
scene_control_ok="yes"
scene_control_note=""
copy_status=0
mkdir -p -- "${CONTROL_ROOT}/game-copy"
cp -R -- "${GAME_DIR}/." "${CONTROL_ROOT}/game-copy/" 2>/dev/null || copy_status=$?
if [[ ! -f "${CONTROL_ROOT}/game-copy/tests/RunSliceEvidenceHarness.tscn" ]]; then
  scene_control_ok="no"
  scene_control_note="the game directory could not be copied (exit ${copy_status}), so the absent-scene control could not run without mutating the tree, and it was not run"
else
  mv -- "${CONTROL_ROOT}/game-copy/tests/RunSliceEvidenceHarness.tscn" \
    "${CONTROL_ROOT}/game-copy/tests/RenamedByAControl.tscn"
  launch_harness "${GODOT}" "${CONTROL_ROOT}/game-copy" "${HARNESS_SCENE}" \
    "${CONTROL_ROOT}/absent-scene"
  absent_count=0
  absent_report=""
  absent_report="$(evaluate_launch_evidence "${LAUNCH_STDOUT}" "${LAUNCH_TRANSCRIPT_STATE}")" \
    || absent_count=$?
  [[ "${absent_count}" -eq 2 ]] || {
    scene_control_ok="no"
    scene_control_note="the launch-evidence read reported ${absent_count} problem(s) where 2 were designed"
  }
  if ! grep -qiE 'RunSliceEvidenceHarness\.tscn' <<<"${LAUNCH_STDOUT}"; then
    scene_control_ok="no"
    scene_control_note="${scene_control_note}${scene_control_note:+; }the engine's own diagnostic does not name RunSliceEvidenceHarness.tscn, so a reader cannot tell an unresolvable scene from a harness that produced no output"
  fi
fi
controls_run=$((controls_run + 1))
if [[ "${scene_control_ok}" == "yes" ]]; then
  control_pass "control: with ${HARNESS_SCENE} renamed IN A COPY of game/, the launch is red and the engine's diagnostic names the unresolvable scene (class ${EXIT_VALIDATION}, not a generic 'produced no output')"
  control_detail < <(grep -iE 'error|cannot|scene' <<<"${LAUNCH_STDOUT}" | head -3)
else
  control_fail "control: the absent-scene case: ${scene_control_note}"
fi

# --- 6g. the no-mutation predicate, over INJECTED STRINGS ---------------------
# THE PRIMARY CONTROL FOR classify_tree_mutation, and the reason it is a function at all.
# Before this existed the comparison was inline, and no fixture could reach it: every control
# in §§ 6a-6f writes under mktemp -d or artifacts/, both outside MUTATION_PATHSPEC, so the
# control set could not move the captured strings even in principle. The predicate was the
# one thing in this gate that had never been observed going red.
#
# STRINGS, NOT A MUTATED TREE. VER-PRE-001-007(ii) forbids a gate editing the tree it checks,
# so the inputs are injected exactly as 6a-6e inject theirs. Driving the same function with
# differing inputs is what makes this a control OF the predicate rather than one adjacent to
# it; § 6h then corroborates it against a real repository difference.
#
# The three cases are the three answers the predicate can give:
#   (a) two captures that DIFFER must be red - the mutation it exists to catch;
#   (b) two EQUAL NON-EMPTY captures must PASS - a dirty working tree that the window did not
#       change is not a failure, and a predicate that reddened on any non-empty status would
#       be unusable for everyone who runs this gate with work in progress;
#   (c) a nonzero status at EITHER end must be red as UNPROVED RATHER THAN TRUE, because two
#       failed captures compare equal and would otherwise manufacture the green.
# "<label>|<before>|<after>|<before status>|<after status>|<expected problems>|<required phrase>"
readonly MUTATION_CONTROLS=(
  "two captures that differ, which must be red| M game/tests/X.tscn| M game/tests/X.tscn?? game/tests/Y.tscn|0|0|1|changed across the window"
  "two equal NON-EMPTY captures, which must PASS: a dirty-but-unchanged tree is not a failure| M game/tests/X.tscn| M game/tests/X.tscn|0|0|0|"
  "a nonzero status at both ends, which must be red as unproved rather than true|||3|3|1|unproved rather than true"
)

for control in "${MUTATION_CONTROLS[@]}"; do
  IFS='|' read -r label before after before_status after_status want phrase <<<"${control}"
  report=""
  got=0
  report="$(classify_tree_mutation "injected capture under control" \
    "${before}" "${after}" "${before_status}" "${after_status}")" || got=$?
  mutation_problems=()
  [[ "${got}" -eq "${want}" ]] \
    || mutation_problems+=("the predicate reported ${got} problem(s) where ${want} was designed")
  # The VERDICT is asserted and not only the count, for case (c) especially: an unreadable
  # capture and a genuine mutation are different findings, and a reader handed the wrong one
  # goes looking for a mutation that never happened.
  if [[ -n "${phrase}" ]] && ! grep -qF -- "${phrase}" <<<"${report}"; then
    mutation_problems+=("the verdict does not say '${phrase}', so the reader cannot tell which of the predicate's findings this is")
  fi
  controls_run=$((controls_run + 1))
  if [[ "${#mutation_problems[@]}" -eq 0 ]]; then
    control_pass "control: ${label} -> ${got} problem(s), as designed"
    [[ -n "${report}" ]] && control_detail <<<"${report}"
  else
    control_fail "control: ${label}: $(printf '%s; ' "${mutation_problems[@]}")"
    [[ -n "${report}" ]] && control_detail <<<"${report}"
  fi
done

# --- 6h. the same predicate over a REAL repository difference ------------------
# 6g proves the predicate reads the strings it is handed. What it cannot prove is that the
# CAPTURES move when a repository really changes - an injected string is a claim about the
# comparison, not about capture_tree_content and capture_tree_metadata. So the same two
# captures and the same predicate run over a THROWAWAY GIT REPOSITORY under CONTROL_ROOT,
# created for this control and deleted with it.
#
# THIS IS WHY THE CAPTURES TAKE A ROOT. VER-PRE-001-007(ii) forbids this gate writing into
# the tree it checks, and build/verify-gate-wiring.sh's habit of writing fixtures into the
# real tree under a trap is that gate's licence and not this one's. A throwaway repo gets a
# genuine `git status` difference and a genuine inode change with nothing in
# ${REPO_ROOT} touched, so the rule the gate asserts and the evidence the gate offers do not
# have to trade off.
#
# The three cases are chosen to MEASURE THE TWO LEGS' DIFFERENT REACH rather than to assert it
# in a comment - which is the substance of the content-only defect this commit repairs:
#   (a) a new file appears           -> the content leg is red, as any diff-based check would be;
#   (b) a byte-identical rewrite     -> the content leg is SILENT, and that is not a bug in it;
#   (c) the same byte-identical rewrite -> the metadata leg is RED, which is the whole reason
#                                          the second leg exists.
mutation_repo="${CONTROL_ROOT}/mutation-repo"
mutation_repo_ready="yes"
mkdir -p -- "${mutation_repo}/game" 2>/dev/null || mutation_repo_ready="no"
# -q and a scoped config: no committer identity is needed because nothing is committed. The
# index alone is enough for `git status --porcelain` to have an opinion and for
# `git ls-files` to name the file, and not committing keeps the control fast and hermetic.
git init -q -- "${mutation_repo}" >/dev/null 2>&1 || mutation_repo_ready="no"
readonly MUTATION_FIXTURE_BYTES='a tracked file created by a negative control in a throwaway repository'
printf '%s\n' "${MUTATION_FIXTURE_BYTES}" >"${mutation_repo}/game/tracked.txt" 2>/dev/null \
  || mutation_repo_ready="no"
(cd -- "${mutation_repo}" && git add -- game/tracked.txt) >/dev/null 2>&1 \
  || mutation_repo_ready="no"

if [[ "${mutation_repo_ready}" != "yes" ]]; then
  # Not silently skipped: three controls did not run, and the summary must say so rather
  # than a green implying they passed.
  for missing in "a new file is visible to the content leg" \
    "a byte-identical rewrite is invisible to the content leg" \
    "a byte-identical rewrite IS visible to the metadata leg"; do
    controls_run=$((controls_run + 1))
    control_fail "control: ${missing}: the throwaway git repository under CONTROL_ROOT could not be created, so the predicate was never driven over a real repository difference and this control is unproved rather than passing"
  done
else
  # (a) a real new file: the content leg must see it.
  real_content_before=""
  real_content_before_status=0
  real_content_before="$(capture_tree_content "${mutation_repo}" game)" \
    || real_content_before_status=$?
  printf 'written by a negative control\n' >"${mutation_repo}/game/written-by-a-control.txt"
  real_content_after=""
  real_content_after_status=0
  real_content_after="$(capture_tree_content "${mutation_repo}" game)" \
    || real_content_after_status=$?
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-content status of the throwaway repository" \
    "${real_content_before}" "${real_content_after}" \
    "${real_content_before_status}" "${real_content_after_status}")" || got=$?
  expect_control_red "a REAL new file in a throwaway repo turns the content leg red" 1 "${got}" "${report}"
  rm -f -- "${mutation_repo}/game/written-by-a-control.txt"

  # (b) and (c) the byte-identical rewrite, through a temp file and a rename so the change is
  # guaranteed rather than clock-dependent: %Y has one-second granularity and a fast rewrite
  # could land in the same second, but the rename always yields a different %i. The temp file
  # is created inside the window and removed by the rename, so this fixture is a
  # write-then-cleanup as well as a byte-identical rewrite - both of the writes the content
  # leg cannot see, in one control.
  rewrite_content_before=""
  rewrite_content_before_status=0
  rewrite_content_before="$(capture_tree_content "${mutation_repo}" game)" \
    || rewrite_content_before_status=$?
  rewrite_metadata_before=""
  rewrite_metadata_before_status=0
  rewrite_metadata_before="$(capture_tree_metadata "${mutation_repo}" game)" \
    || rewrite_metadata_before_status=$?

  printf '%s\n' "${MUTATION_FIXTURE_BYTES}" >"${mutation_repo}/game/.rewrite.tmp"
  mv -f -- "${mutation_repo}/game/.rewrite.tmp" "${mutation_repo}/game/tracked.txt"

  rewrite_content_after=""
  rewrite_content_after_status=0
  rewrite_content_after="$(capture_tree_content "${mutation_repo}" game)" \
    || rewrite_content_after_status=$?
  rewrite_metadata_after=""
  rewrite_metadata_after_status=0
  rewrite_metadata_after="$(capture_tree_metadata "${mutation_repo}" game)" \
    || rewrite_metadata_after_status=$?

  # (b) EXPECTED TO PASS WITH ZERO PROBLEMS, and that zero is the finding. It is the measured
  # form of "git status --porcelain is a content-derived summary": the file was rewritten and
  # a temp file came and went, and the content leg reports nothing. A gate carrying only this
  # leg would call that tree unwritten.
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-content status of the throwaway repository" \
    "${rewrite_content_before}" "${rewrite_content_after}" \
    "${rewrite_content_before_status}" "${rewrite_content_after_status}")" || got=$?
  expect_control_red "a REAL byte-identical rewrite is INVISIBLE to the content leg, which is why a second leg exists" \
    0 "${got}" "${report}"

  # (c) the same rewrite, read by the metadata leg, which must be red.
  report=""
  got=0
  report="$(classify_tree_mutation "tracked-file metadata of the throwaway repository" \
    "${rewrite_metadata_before}" "${rewrite_metadata_after}" \
    "${rewrite_metadata_before_status}" "${rewrite_metadata_after_status}")" || got=$?
  expect_control_red "the SAME byte-identical rewrite IS visible to the metadata leg (mtime, inode, size)" \
    1 "${got}" "${report}"
fi

# --- 6i. the real measurement: did the launch or the controls change the tree? --
#
# TWO LEGS, TWO FINDINGS, because "the tracked content did not change" and "nothing was
# written" are different claims and a single verdict would let a reader take the weaker
# measurement for the stronger claim. The window spans § 1's REAL LAUNCH and every control
# above; see the block before § 1 for why it is opened there rather than here.
#
# Emitted through one helper so there is one pass site and one fail site rather than four,
# and so both legs are worded to the same discipline: each says what its own leg measured and
# nothing about the other's subject.
#
# READING A RED FROM THE METADATA LEG. In CI it means this gate or the harness it launched
# wrote to a tracked file in ${MUTATION_SCOPE}: the checkout is clean and no other writer
# touches the tree while the job runs, so the tree is STATIC across the window. On a
# developer machine the tree is not static - tests/verification/*.json and game/tests/* are
# under active edit, and the window is open across § 1's launch for up to
# ${LAUNCH_TIMEOUT_SECONDS}s - so a red there may be a concurrent edit by a person or
# another agent. It is still a TRUTHFUL red: something in scope was written. The check to
# run is WHAT CHANGED, from the paths and mtimes the verdict prints. The check NOT to run is
# whether MUTATION_PATHSPEC is too wide: narrowing it to silence this is trading the
# coverage the leg exists to give for a green, and the developer-local hazard it would be
# bought with does not exist in CI in the first place.
report_mutation_leg() {
  # $1 subject phrase for the predicate, $2 before, $3 after, $4 before status,
  # $5 after status, $6 the pass message - which must claim ONLY what this leg measured.
  local subject="$1" before="$2" after="$3" before_status="$4" after_status="$5"
  local pass_message="$6"
  local leg_report=""
  local leg_count=0
  leg_report="$(classify_tree_mutation "${subject}" "${before}" "${after}" \
    "${before_status}" "${after_status}")" || leg_count=$?
  controls_run=$((controls_run + 1))
  if [[ "${leg_count}" -eq 0 ]]; then
    control_pass "control: ${pass_message}"
  else
    control_fail "control: ${leg_report}"
  fi
}

control_tree_content_after=""
control_tree_content_after_status=0
control_tree_content_after="$(capture_tree_content "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || control_tree_content_after_status=$?
control_tree_metadata_after=""
control_tree_metadata_after_status=0
control_tree_metadata_after="$(capture_tree_metadata "${REPO_ROOT}" "${MUTATION_PATHSPEC[@]}")" \
  || control_tree_metadata_after_status=$?

# LEG 1's pass message claims STATUS identity, not byte identity and not absence of writes.
# The previous wording said the tree was "exactly as the controls found them", which asserted
# byte-and-metadata identity off the back of a content-derived summary and over a window that
# excluded the launch. Both halves of that overclaim are corrected here.
report_mutation_leg \
  "tracked-content status of ${MUTATION_SCOPE}" \
  "${control_tree_content_before}" "${control_tree_content_after}" \
  "${control_tree_content_before_status}" "${control_tree_content_after_status}" \
  "content leg: git reports the same modified/staged/untracked set for ${MUTATION_SCOPE} after § 1's launch and every control above as it did before the launch. This is a STATUS summary and not byte identity, and it is NOT evidence that nothing was written - see the metadata leg for that claim"

# LEG 2's pass message is the one about writing, and it names its own blind spot rather than
# leaving a reader to assume there is none.
report_mutation_leg \
  "tracked-file metadata (mtime, inode, size) of ${MUTATION_SCOPE}" \
  "${control_tree_metadata_before}" "${control_tree_metadata_after}" \
  "${control_tree_metadata_before_status}" "${control_tree_metadata_after_status}" \
  "metadata leg: the mtime, inode and size of every TRACKED file in ${MUTATION_SCOPE} are unchanged across § 1's launch and every control above, so no tracked file in scope was written - including the byte-identical rewrite and the write-then-cleanup the content leg cannot see. Covers tracked files only: an untracked file created and deleted inside the window is outside both legs' reach. If this leg ever goes RED, note that in CI the tree is static - clean checkout, no other writer during the run - so a red there is a write by this gate or the harness; on a developer machine the same red may instead be a concurrent edit to tests/verification/*.json or game/tests/* inside a window open for up to ${LAUNCH_TIMEOUT_SECONDS}s, which is a truthful red about a real write. Either way the check to run is WHAT CHANGED, from the paths and mtimes the failure prints. Narrowing MUTATION_PATHSPEC is the WRONG fix: it buys a green by dropping coverage of files this gate actually reads"

cleanup_controls
trap - EXIT

# The control-set assertion, UNMARKED: it is a finding about this gate's control set and
# not something a control's fixture produced. A control that silently stops running is a
# gate whose green means less than it did, and nothing else here would notice.
if [[ "${controls_run}" -eq "${EXPECTED_CONTROLS}" ]]; then
  pass "all ${controls_run} negative controls ran"
else
  fail "${controls_run} negative control(s) ran where EXPECTED_CONTROLS=${EXPECTED_CONTROLS}. Either a control stopped running or one was added without restating the total"
fi

# This gate runs negative controls in band, so its log contains failure vocabulary and
# engine error text on a green run. Prove the marking that separates that text from
# genuine findings still holds.
gate_assert_marking

# --- the exit ------------------------------------------------------------------
#
# WHY THE SINGLE gate_summary FUNNEL IS NOT SUFFICIENT HERE, so a later reader does not
# tidy this back into one line.
#
# Every other gate in build/ ends `gate_summary "<name>" "${EXIT_VALIDATION}"`, which
# collapses every failure into one class. That is right for them: their subject is the
# repository and validation is the only class their findings can have. It is WRONG here.
# VER-PRE-001-003 asserts that the harness's own exit code becomes the verb's exit class
# UNCHANGED - doc 100 § Standard command surface, "wrappers preserve the owning tool's
# class rather than returning success after partial work" - and this script is the only
# thing between the harness and GodotImportVerb. The harness emits 0, 2 or 4, and 2 is a
# real class here (ExitClass.InvalidInvocation) rather than a rounding error: a caller
# reading 4 goes looking for a failing assertion, and the harness's 2 means it never got
# an output directory and evaluated nothing.
#
# So gate_summary still prints - the summary, the failing sections and the marked-line
# accounting are all still wanted - and its status is then OVERRIDDEN by the harness's
# class when the harness is what failed. The two cases stay distinguishable in the log as
# well as in the class, because § 2 named which one happened.
summary_class=0
gate_summary "verify-run-slice" "${EXIT_VALIDATION}" || summary_class=$?

if [[ -n "${propagated_class}" ]]; then
  printf 'verify-run-slice: exiting %s, the class THE HARNESS chose, passed through unchanged (VER-PRE-001-003). This gate found %s finding(s) of its own.\n' \
    "${propagated_class}" "${gate_failures}"
  exit "${propagated_class}"
fi

if [[ "${summary_class}" -ne 0 ]]; then
  printf 'verify-run-slice: exiting %s, THIS GATE\x27s class: the harness exited %s and the findings above are this gate\x27s own.\n' \
    "${summary_class}" "${HARNESS_STATUS}"
fi
exit "${summary_class}"
