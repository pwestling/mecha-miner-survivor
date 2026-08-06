# Negative controls for the threat-and-extraction slice

Every assertion added by the `ENC-001`, `MIN-001`, `COM-005`, `PRG-006`, `PLY-001` (entries 011-013) and
`GEO-001` (entry 005) verification entries has a control recorded here. A control injects the violation
the assertion claims to catch, the named test filter is run, and the verbatim failure is recorded. Each
one was run.

## What no automated gate does

**Nothing asserts this document's coverage.**
`tests/MechaMiner.Simulation.Tests/Support/NegativeControlCoverageTests.cs` asserts a partition between
`tests/verification/SIM-negative-controls.md` and `tests/verification/SIM-00*.json`, and its totals
arithmetic is over those seven files only. This file is not in that glob and neither are the registries
that point at it, so adding an entry here without a control is not currently a failing gate. That is
stated so a reader does not assume an enforcement that does not exist; extending the coverage gate to a
second corpus is `FND-009`'s registry-validator work under `TASK-FND-009-002`.

**Nothing runs the two engine-tier harnesses either.** `game/tests/RunSliceEvidenceHarness.tscn` and
`game/tests/RunSliceCaptureHarness.tscn` are invoked by no verb and no `build/verify-*.sh`. They run when
a person types the command. Their assertions are real and their exit codes are real, but they are not
gated coverage.

## The failure modes these controls are looking for

Four shapes, all of which this repository has shipped:

1. **A gate expressed in terms of the number it checks.** Found here: every loop in `MiningDecayTests`
   was written as `GrayboxExtraction.ExitGraceTicks`, so setting the documented 0.5-second grace to 0.0
   ran green — a hold loop of length zero holds nothing and every later assertion still held. Fixed by
   making the tick count a literal in the fixture, asserted against the production constant exactly once.
2. **An assertion over a population that cannot falsify it.** Found twice. Inverting phase 7's
   nearest-target comparison ran green under the authored minute-0 row, because that row staggers arrivals
   so usually exactly one pursuer is in range, and with one candidate the nearest and the first in stable
   order are the same enemy. Fixed by running the assertion under the lethal stress row and asserting
   separately that more than a hundred ticks had several pursuers in range at once.
3. **A claim that is unobservable, so nothing checks it.** Found twice. Moving dead-enemy removal from
   phase 12 into phase 10 ran green against every assertion in the loop fixture, because within one tick
   the two are indistinguishable to every later phase. Fixed by counting removals applied in phase 12 and
   asserting the count equals the deaths resolved.
4. **A control that goes red for the wrong reason.** Four of the first attempts here failed to *compile*
   rather than failing an assertion, which `docs/technical/91-verification-strategy.md` § Negative control
   adequacy rules out: a control must be "a coherent violation, not a broken state", or a red result is
   ambiguous between the gate catching it and the gate falling over. All four were redone with injections
   that compile.

## Which entry each section controls

| Entry | Section | Injection |
| --- | --- | --- |
| `VER-ENC-001-001` | EN-01 profile | contact damage 5 to 6 |
| `VER-ENC-001-002` | pursuit speed and non-solidity | per-tick displacement halved |
| `VER-ENC-001-003` | contact cadence | the global 0.20 s grace test disabled |
| `VER-ENC-001-004` | minute-0 pulse | desired minimum 8 to 10 |
| `VER-ENC-001-005` | same-seed determinism | entrance bearing drawn from an unseeded generator |
| `VER-ENC-001-006` | deferred death removal | removal moved from phase 12 into phase 10 |
| `VER-MIN-001-001` | seam payout profile | ore per installment 10 to 11 |
| `VER-MIN-001-002` | automatic occupancy | occupancy additionally requires a prior payout |
| `VER-MIN-001-003` | grace and four-times decay | exit grace 0.5 s to 0.0 s |
| `VER-MIN-001-004` | installment payout in phase 11 | the ore addition zeroed |
| `VER-COM-005-001` | Pulse Repeater catalog numbers | damage 12 to 13 |
| `VER-COM-005-002` | exact attack cadence | credit capped at exactly one activation |
| `VER-COM-005-003` | nearest-target selection | phase 7's comparison inverted to farthest |
| `VER-COM-005-004` | projectile flight and one hit per projectile | targeting range 8 m to 10 m |
| `VER-PRG-006-001` | terminal outcome | phase 13's death condition inverted |
| `VER-PRG-006-002` | settlement does not crash the run | the terminal-transition bridge never raises |
| `VER-PLY-001-011` | Hull damage application | damage displaces the mech one metre east |
| `VER-PLY-001-012` | no passive recovery | one Hull returned per damage instance |
| `VER-PLY-001-013` | the scheduled-event seam | the seam settles the run |
| `VER-GEO-001-005` | swept-segment overlap | the segment projection clamped to its endpoints |
| (seam placement half of `VER-ENC-001-005`) | seam placement is seeded | bearings taken from the loop index |

## How to run one

`artifacts/negative-controls/run-one.sh` takes a name, a file, an NUnit filter, and the two halves of a
text replacement. It reverts through `git checkout` rather than through a copy of the file, and it refuses
to start against a file that is already modified. Both properties are there because the first version
kept its backup in a scratch directory that was deleted underneath it mid-run, which left an injection
resident in `GameplayWorld.cs`.

## Proving this record can fail

Every control below was observed red. The verbatim output is in the pull request that added this file;
`artifacts/` is not committed, so the logs the runner writes are local and are not evidence a later reader
can reach. That is a limitation of this record rather than of the controls: what is committed is the
statement that each was run, the injection each used, and the failing test name.

<!-- M3-UNCOVERED-BEGIN -->
```text
uncovered-count: 0
```
<!-- M3-UNCOVERED-END -->

Zero entries in these six files lack a control. If that ever stops being true, the ID belongs in the
block above and its reason in the table, and the block is deliberately shaped like
`SIM-negative-controls.md`'s so that a future validator can read both.
