#!/usr/bin/env bash
#
# The shared output vocabulary for this repository's gate scripts. Sourced, never run,
# and so mode 644 rather than 755: it is a library and not an entry point, and nothing
# should be able to invoke it as a workflow step.
#
# NOTE FOR WHOEVER MERGES FND-005 INTO THIS BRANCH: this file is a new script under
# build/, seen by both of verify-gate-wiring.sh's enumerators (it has a .sh extension and
# a shebang), and that gate requires set equality between the enumerated scripts and its
# INVENTORY. It therefore needs an INVENTORY entry, and none of that file's current
# KNOWN_KINDS - gate, launcher, provisioning - describes a sourced library. Adding a
# 'library' kind plus one entry is the merge fix; leaving it out fails that gate closed,
# which is the gate working correctly.
#
# Authority: docs/technical/delivery-waves.md § Decision 11 (a gate never passes on an
#              input set it did not successfully obtain; rule 4, every gate carries a
#              negative control)
#            docs/technical/91-verification-strategy.md § Negative control adequacy
#
# ONE COPY, ON PURPOSE. The two preceding rounds on this branch each found a correction
# that had reached one file and not its copy - 9f7c8fe's misattribution surviving in
# build/policy-fixtures/README.md is the worst case, and the commit that fixed it called
# copy drift "the half-life this sweep exists to shorten". Every gate defining its own
# pass/fail pair is that same shape waiting to happen, so the emitters live here.
#
# ---------------------------------------------------------------------------------------
# Why control output is marked
# ---------------------------------------------------------------------------------------
#
# Running negative controls in band is the right design and this repository has committed
# to it: a control that only runs on a scratch branch proves nothing about the gate a
# maintainer actually invokes. It has one cost, and the cost grows with the thoroughness
# of the control set.
#
# A gate whose controls run in band PRINTS ITS OWN FAILURE VOCABULARY ON EVERY RUN. Its
# log therefore contains a plausible-looking answer to almost any question a reader
# brings to it. The failure mode is confirmation, not confusion: a reader who predicts a
# cause, greps the log for the string that cause would produce, and finds it - inside a
# control's fixture - stops looking, and ships a fix for the wrong section. That happened
# on this project: a session grepped for its expected string, found it twice inside
# in-band control fixtures, and nearly repaired the wrong check while the real failure was
# in a different section covering three scripts rather than one.
#
# So every line produced while a control's fixture is in place carries ${CONTROL_MARKER},
# and no line about the subject under test ever does. `grep -v` on that one token leaves
# only genuine findings.
#
# This is a PROPERTY rather than a convention, which is the point: `pass` and `fail`
# REFUSE a message that already contains the marker, and `control_pass`/`control_fail`
# always add it. A future author cannot add an unmarked control line, or a marked genuine
# one, without the gate saying so. `gate_assert_marking` proves that enforcement works on
# every invocation, so the marking cannot decay into a naming habit.
#
# A control that FAILS is still marked. It is a genuine finding - about the gate rather
# than about the tree - so it is counted separately and named in the summary, where it
# cannot be missed, instead of being made greppable alongside findings about the
# repository. The summary distinguishes the two.
#
# ---------------------------------------------------------------------------------------
# What gets marked, and what deliberately does not
# ---------------------------------------------------------------------------------------
#
# WHAT IS MARKED: output from any section whose purpose is to prove the gate can fail -
# every section named "negative control(s)" or "control", including the assertions that
# clean up after its fixture. Those sections print failure vocabulary on a GREEN run,
# which is exactly the text a reader searching for a cause will find and stop at.
#
# WHAT IS NOT MARKED, on purpose: a gate's primary assertions, even where they are driven
# by a deliberately invalid fixture. verify-format.sh §§ 1-5 write a misformatted file and
# require format-check to reject it; verify-policies.sh requires each policy fixture to
# fail to compile; verify-test-harness.sh § 2 runs a deliberately failing test to prove a
# reproduction is printed. Those failures ARE the finding the gate exists to report, and
# marking them would leave `grep -v` on the marker showing nothing, which is a marker that
# carries no information rather than a useful one.
#
# The boundary is therefore "is a failure here news about the repository, or is it the
# fixture doing its job so the gate can check itself" - and it is written down here rather
# than left to each author's judgement, because the next person to add a control needs to
# know which side of it they are on. If a primary section's fixture ever starts producing
# text that collides with a real diagnostic a reader would search for, mark that section
# too and extend this note; do not widen the marker silently.

if [[ -n "${GATE_OUTPUT_SOURCED:-}" ]]; then
  return 0
fi
readonly GATE_OUTPUT_SOURCED=1

# The one token. Chosen to be a word no diagnostic, path, MSBuild property or Godot log
# line in this repository contains, so `grep -v` on it cannot remove a genuine finding.
readonly CONTROL_MARKER='[control-fixture]'

gate_failures=0
gate_control_failures=0
gate_sections_failed=()
gate_not_run=()
gate_controls_emitted=0
gate_current_section='(no section declared yet)'

# Declares the section every subsequent finding belongs to, and prints its heading. The
# summary reads this back, so a red run names the section instead of only a count.
section() {
  gate_current_section="$*"
  printf '\n=== %s\n' "$*"
}

_gate_record_failing_section() {
  local existing
  for existing in ${gate_sections_failed[@]+"${gate_sections_failed[@]}"}; do
    [[ "${existing}" == "${gate_current_section}" ]] && return 0
  done
  gate_sections_failed+=("${gate_current_section}")
}

# A finding about the subject under test. Never carries the marker.
pass() {
  if [[ "$*" == *"${CONTROL_MARKER}"* ]]; then
    _gate_marker_misuse "pass" "$*"
    return 0
  fi
  printf 'ok    %s\n' "$*"
}

fail() {
  if [[ "$*" == *"${CONTROL_MARKER}"* ]]; then
    _gate_marker_misuse "fail" "$*"
    return 0
  fi
  printf 'FAIL  %s\n' "$*"
  gate_failures=$((gate_failures + 1))
  _gate_record_failing_section
}

# A finding produced while a negative control's fixture was in place. Always marked, so a
# reader can exclude every line of it with one `grep -v`.
control_pass() {
  gate_controls_emitted=$((gate_controls_emitted + 1))
  printf 'ok    %s %s\n' "${CONTROL_MARKER}" "$*"
}

control_fail() {
  gate_controls_emitted=$((gate_controls_emitted + 1))
  printf 'FAIL  %s %s\n' "${CONTROL_MARKER}" "$*"
  gate_control_failures=$((gate_control_failures + 1))
  _gate_record_failing_section
}

# Multi-line detail belonging to a control run. Every line is marked, because a reader
# excluding control output must not be left holding half of it.
#
# CALL IT WITH A REDIRECTION, never as the right-hand side of a pipe:
#
#   control_detail <<<"${text}"            control_detail < <(some | pipeline)
#
# On the right of a pipe it runs in a subshell, the marked-line count it keeps is
# discarded when that subshell exits, and gate_summary then understates how much of the
# log is control output - a summary making a numeric claim that is quietly wrong is worse
# than one making none.
control_detail() {
  local line
  while IFS= read -r line; do
    gate_controls_emitted=$((gate_controls_emitted + 1))
    printf '      %s %s\n' "${CONTROL_MARKER}" "${line}"
  done
}

# A required check that did not run. Recorded rather than merely printed, so no summary
# can read as unqualified success while coverage was reduced.
skip() {
  printf 'SKIP  %s\n' "$*"
  gate_not_run+=("$*")
}

# A stage or section that was not reached because an earlier one failed. "Red at stage 4"
# invites a reader to assume stages 5 and 6 passed; they did not run, and that is a
# different statement. Recorded so the summary can say which.
not_reached() {
  printf 'NOT RUN  %s\n' "$*"
  gate_not_run+=("${*} (not reached; an earlier section failed)")
}

_gate_marker_misuse() {
  local emitter="$1"
  shift
  # Deliberately describes the token instead of printing it. A genuine finding that
  # embedded the marker would be removed by the very `grep -v` this marking exists to
  # make safe, so the report of the misuse must not commit the misuse.
  printf 'FAIL  %s() was given a message already carrying the control marker, so control output and genuine findings would no longer be separable. Offending message with the marker elided: %s\n' \
    "${emitter}" "${*//"${CONTROL_MARKER}"/<marker>}"
  gate_failures=$((gate_failures + 1))
  _gate_record_failing_section
}

# Proves the marking is a property and not a habit, on every invocation. Without this the
# separation decays the first time someone adds a control that prints with `pass`.
gate_assert_marking() {
  section "output marking: control output is separable from genuine findings"

  local marked genuine misuse
  marked="$(control_pass 'a synthetic control finding' 2>&1)"
  genuine="$(pass 'a synthetic genuine finding' 2>&1)"

  if [[ "${marked}" == *"${CONTROL_MARKER}"* ]]; then
    pass "control output carries the control marker"
  else
    fail "control output does not carry the control marker; control fixtures are indistinguishable from findings"
  fi

  if [[ "${genuine}" != *"${CONTROL_MARKER}"* ]]; then
    pass "a genuine finding carries no marker, so excluding control output cannot discard one"
  else
    fail "a genuine finding carried the control marker; excluding control output would discard real findings"
  fi

  # The enforcement itself: pass() must refuse a pre-marked message rather than print it.
  #
  # Asserted on the CAPTURED OUTPUT and not on gate_failures. Command substitution runs
  # pass() in a subshell, so the counter it increments there is discarded when the
  # subshell exits - which is convenient rather than awkward: the deliberate misuse this
  # control provokes cannot leak into the gate's own failure count, so there is no counter
  # to repair afterwards and no window in which a repair could erase a real finding.
  # The provoking message below is built from the token on purpose; every assertion about
  # it describes the token rather than repeating it.
  misuse="$(pass "smuggled ${CONTROL_MARKER} through a genuine emitter" 2>&1)"
  if [[ "${misuse}" == FAIL*"already carrying the control marker"* ]] \
      && [[ "${misuse}" != *"ok    smuggled"* ]]; then
    control_pass "a marked message passed to pass() is rejected, so the separation is enforced and not merely conventional"
  else
    fail "pass() accepted a message carrying the control marker; the marking is a convention that will decay. It emitted: ${misuse//"${CONTROL_MARKER}"/<marker>}"
  fi
}

# The single summary every gate ends with. $1 gate name, $2 exit code for failure.
#
# Names the failing SECTIONS, not only a count, because "read the section, do not grep the
# log" is only usable advice when the section is easy to read off. Names control failures
# separately from findings about the tree, and names everything that did not run.
gate_summary() {
  local name="$1"
  local failure_exit="$2"
  local total=$((gate_failures + gate_control_failures))
  local entry

  echo
  if [[ "${gate_controls_emitted}" -gt 0 ]]; then
    printf '%s: %s marked line(s) of this log were produced by in-band negative controls, verdicts and detail alike, and every one carries %s.\n' \
      "${name}" "${gate_controls_emitted}" "${CONTROL_MARKER}"
    printf "        To read only findings about the repository: %s ... | grep -v '%s'\n" \
      "${name}" "${CONTROL_MARKER}"
  fi

  if [[ "${#gate_not_run[@]}" -gt 0 ]]; then
    printf '%s: %s required check(s)/stage(s) DID NOT RUN:\n' "${name}" "${#gate_not_run[@]}"
    for entry in "${gate_not_run[@]}"; do
      printf '  DID NOT RUN  %s\n' "${entry}"
    done
  fi

  if [[ "${total}" -eq 0 ]]; then
    if [[ "${#gate_not_run[@]}" -gt 0 ]]; then
      # Deliberately not a bare PASS: every assertion that ran passed, and the reader is
      # told in the same line that coverage was reduced.
      printf '%s: PASS WITH %s CHECK(S) THAT DID NOT RUN - coverage reduced, see above\n' \
        "${name}" "${#gate_not_run[@]}"
      return 0
    fi
    printf '%s: PASS\n' "${name}"
    return 0
  fi

  printf '%s: FAIL (%s finding(s) about the repository, %s failing negative control(s))\n' \
    "${name}" "${gate_failures}" "${gate_control_failures}"
  printf '%s: FAILING SECTION(S) - read these, do not grep the log:\n' "${name}"
  for entry in ${gate_sections_failed[@]+"${gate_sections_failed[@]}"}; do
    printf '  FAILING SECTION  %s\n' "${entry}"
  done
  if [[ "${gate_control_failures}" -gt 0 ]]; then
    printf '%s: a failing negative control means THE GATE cannot detect the violation it was written for.\n' \
      "${name}"
  fi
  return "${failure_exit}"
}
