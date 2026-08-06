# SIM negative-control transcripts

Every `SIM-00*.json` entry in this directory whose `evidenceKinds` includes `negative-control`
asserts that its gate can fail. This file is that evidence, so the claim lives beside the entries it
substantiates rather than in a pull-request description or a reviewer's scratch directory.

Authority: `docs/technical/91-verification-strategy.md` § Acceptance evidence, which requires
evidence that a gate can fail; and its negative-control adequacy rules.

## Why a forced rebuild is part of the method

A negative control is a claim about a *build*, not about a source file. A restore that preserves a
file's mtime lets MSBuild treat the assembly as current, skip the recompile, and run the probe
against the **previous** build. The probe then reports on code that is no longer in the tree, and the
transcript is false while looking exactly like a true one. This happened once during review before it
was caught, so the rebuild is proved rather than assumed, two independent ways:

1. the perturbed source file's mtime is bumped after the edit, so the compiler input is
   unambiguously newer than the assembly; and
2. the **sha256 of the output assembly must differ** before versus after the build.

(2) is the load-bearing one. It is a direct observation that a new assembly exists, and unlike a
`CoreCompile` line in a build log it cannot be satisfied by some other project recompiling. The probe
then runs with `--no-build`, so the assembly that was proved new is the one measured. A control whose
assembly hash does not move aborts instead of reporting.

Each control also asserts that the perturbation changed the file at all (the snippet must match), and
restores it to its exact pre-edit blob afterwards, verified with `git hash-object`. No perturbed state
is ever committed.

## Reproducing one

```
git rev-parse HEAD                      # confirm the revision under test
<apply the perturbation named below>
touch <the perturbed file>
sha256sum src/MechaMiner.Simulation/bin/Debug/net8.0/MechaMiner.Simulation.dll
dotnet build tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj -v:n
sha256sum src/MechaMiner.Simulation/bin/Debug/net8.0/MechaMiner.Simulation.dll   # must differ
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --no-build --filter '<filter named below>'
git checkout -- <the perturbed file> && touch <the perturbed file>
```

## The controls

All eleven below were re-run at `a06bd18` with the rebuild forced and proved by a changed assembly
hash. Every one went red. None failed to reproduce.

### Randomness: the algorithm constants

Filter for all six: `FullyQualifiedName~MechaMiner.Simulation.Tests.Random`. Each is a one-bit or
one-step change to a constant that doc 20 § Authoritative random-number contract fixes.

| Perturbation | File | Result |
| --- | --- | --- |
| `Multiplier` `6364136223846793005UL` -> `...004UL` | `Random/Pcg32.cs` | red: `OutputSequenceMatchesTheCommittedVector`, golden `random-stream-initialization.txt` diverges from `primed-state` onward |
| `FirstXorShift` `18` -> `17` | `Random/Pcg32.cs` | red: 4+ tests including `OutputIsTheTransformationOfTheStateReadBeforeTheAdvance` |
| `SecondXorShift` `27` -> `26` | `Random/Pcg32.cs` | red: 4+ tests including `GoldenVectorsAreReproducedByAnIndependentImplementation` |
| `RotationShift` `59` -> `58` | `Random/Pcg32.cs` | red: 4+ tests including `CanonicalOrderIsEstablishedBeforeTheIndexIsDrawn` |
| increment low bit dropped, `(selector << 1) | 1UL` -> `selector << 1` | `Random/Pcg32.cs` | red: golden diff shows every `increment` losing its low bit, e.g. `0xE506F6059EE85593` -> `...92` |
| `Mix` `SecondShift` `27` -> `26` | `Random/SeedDerivation.cs` | red: 6+ tests including `AllRegisteredFamilyKeysArePresentUniqueAndClosed` |

The increment control is the interesting one: an even increment makes the LCG's period collapse, and
it is caught as a *recorded value* in the golden rather than only as a downstream output change.

### Randomness: rejection sampling replaced by modulo reduction

The substitution a future maintainer is most likely to make, so it has its own control. In
`Random/BoundedRandom.cs`, `NextBounded`'s loop is replaced by `return source.NextUInt32() % bound;`.

Red on two tests at once, which is the point: the results shift by one draw **and** the
draws-consumed column collapses from `2` to `1` wherever a draw should have been rejected.

```
  Failed BoundedIntegersUseRejectionSamplingNotModulo
  2)   two draws were rejected and redrawn
  Failed GoldenVectorsAreReproducedByAnIndependentImplementation
   golden random-bounded-conversion.txt does not match.
 184 - result	00	1611911799	2
 184 + result	00	72691162	1
 185 - result	01	1516283680	1
 185 + result	01	1611911799	1
```

The `bound 0xC0000000` block exists in that golden precisely so `draws > 1` is observable.

### Entities: the stale-reference gate refuses without counting

`Entities/PackedEntityStore.cs`, `ResolveDense`: the `RecordStaleReference()` call is removed from the
generation-mismatch branch, keeping the fail-closed `return -1`. Doc 20 § Entity identity requires
both the refusal and the diagnostic counter, so refusing without counting must still be red.

Filter: `FullyQualifiedName~MechaMiner.Simulation.Tests.Entities`. Red on three:

```
  Failed StaleGenerationFailsClosedAndCountsADiagnostic
Assert.That(afterStale - before, Is.EqualTo(1L))
  Expected: 1
  But was:  0
  Failed CapacityDiagnosticsReconcileWithTheOperationsPerformed
     every failed resolution must be counted exactly once
  Expected: 134
  But was:  0
  Failed IdentityAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs
```

The third is the control-of-the-control: the fixture that proves the store assertions can fail
notices that the real store now behaves like a broken stub.

### Events: the domain buffer drops at its ceiling

`Events/DomainEventBuffer.cs`, `Grow`: a silent `return` is added at the hard ceiling, so a domain
event is discarded instead of failing the tick invariant. This is the drop branch the real type
deliberately does not have.

Filter: `FullyQualifiedName~MechaMiner.Simulation.Tests.Events`.

```
  Failed DomainEventsAreNeverDropped
     Assert.That(caughtException, expression)
  Expected: <System.InvalidOperationException>
  But was:  null
```

### Commands: the two mid-commit recovery controls

Both target `Commands/CommandAdmissionGate.cs` and the same gate,
`AFailedCommitInvalidatesTheTickInsteadOfWedgingTheRun` (`VER-SIM-004-013`). They are separate
controls because they break different halves of doc 20 § Mid-commit invalidation.

**A, recovery removed.** `Apply`'s call to `AbandonPartialCommit` is put behind the
runtime-impossible condition `_runSession == 0UL` (the constructor refuses run session zero), so the
commit still rethrows but recovers nothing. A literal `if (false)` is not usable here: it is
`error CS0162`, unreachable code, under this repository's warnings-as-errors settings.

```
  Failed AFailedCommitInvalidatesTheTickInsteadOfWedgingTheRun
  1)   the tick the commit opened must not be left open, or no later tick and no retry can run
Assert.That(fixture.Publisher.IsTickOpen, Is.False)
  Expected: False
  But was:  True
  2)   it must be invalidated and counted
Assert.That(fixture.Publisher.InvalidatedTickCount, Is.EqualTo(invalidatedBefore + 1))
  Expected: 1
  But was:  0
```

The assertions about nothing being published and no version advancing still **pass** under this
perturbation, which is why a test asserting only those would not have caught the original defect.

**B, releasing a buffer the commit did not open.** Inside `AbandonPartialCommit`, the
`!domainBufferWasOpen` condition is neutralised so the recovery releases a domain buffer belonging to
another tick. Doc 20 § Mid-commit invalidation carves this out explicitly: a buffer the commit did not
open, or that holds an unconsumed record, is left exactly as it is, because `CTR-SIM-001` forbids
dropping an authoritative event. Red on the same gate.

## Controls for the second review pass

These three accompany fixes made in the same pass and are recorded here for the same reason.

**The recovery artifact's canonical sort** (`VER-SIM-005-013`). Before the fixture was rebuilt,
deleting `captured.Sort(CompareRecoveryOrder)` in `Random/RandomStreamSet.cs` left the suite green,
and so did reducing `CompareRecoveryOrder` to `return byFamily;`: the fixture instantiated its
streams in strictly ascending family-key order with one instance per family, so insertion order and
sorted order were the same sequence. The fixture now instantiates out of order and gives family
`0x0220` two instances with the higher instance key first. Both perturbations are now red:

```
Assert.That(ascending, Is.True)
  Expected: True
  But was:  False
Assert.That(CaptureInInstantiationOrder(reversedOrder), Is.EqualTo(artifact))
  Expected: v1 0x0410/0xFFFFFFFFFFFFFFFF ...
  But was:  v1 0x0230/0x0000000000000000 ...
```

**The two idempotent pause outcomes** (`VER-SIM-002-004`). `PauseTransitionOutcome.AlreadyPresent`
and `AlreadyAbsent` had no test-side reference, so deleting either branch in `Runtime/RunClock.cs`
left the suite green. Both are now red:

```
delete the AlreadyPresent branch -> Expected: AlreadyPresent  But was: Raised
delete the AlreadyAbsent branch  -> Expected: AlreadyAbsent   But was: Cleared
```

**The forked stream** (`VER-SIM-005-011`). In `Random/RandomStreamSet.cs`, `DrawAt` is changed from
`ref Pcg32 stream = ref this._streams[index];` to `Pcg32 stream = this._streams[index];`, so every
draw of a key is identical.

Before the fix this control was reported by the run never finishing: the whole test project hung past
a 150-second cap, exit 124, because `BoundedRandom.NextBounded`'s `while (true)` rejection loop cannot
terminate against a source that does not advance. Note that `VER-SIM-005-011` *in isolation* did
report, in 73 ms; what hung was the suite. That is why the fix is in the loop and not only in the
test. After it, the same perturbation gives exit 1 in 3 seconds with 12 reported failures:

```
  Failed AnExtraDrawInOneFamilyShiftsNoOtherFamily [35 ms]
     drawing through a source must advance the stored stream, not a copy of it; a stream that
     does not advance is forked, and every later draw is a copy of 04552DDA
```

and the loop's own bound names the divergence where it is reached:

```
rejection sampling rejected 256 consecutive draws for bound 3221225472 against threshold
1073741824; the last rejected draw was 0x04552DDA after 0 draws from stream
0x0100/0x0000000000000000 index 0. More than half of all draws are accepted for every bound, so
this is a source that is not advancing rather than an unlucky run
```

## What these transcripts do not establish

A negative control shows a gate can fail for the reason named. It does not show the gate is
sufficient, and three specific limits are worth stating rather than leaving to be discovered.

- A control that turns several tests red at once has not shown those tests to be independently
  effective. Where independence matters it was probed separately, one guard at a time.
- The committed allowed-call lists in `PostPublicationRegionTests` are an edit tax rather than
  evidence: adding a call to the region and to the list passes. See doc 20 § Mid-commit invalidation.
- Six guards in the simulation cannot fail from any public entry point, so no control exists for them
  and none is claimed. Each is labelled where it is registered, with its own reason, in the notes of
  `SIM-003.json`, `SIM-004.json`, `SIM-005.json`, and `SIM-007.json`.
