# SIM negative-control transcripts

Every perturbation control this stream ran is recorded below, each with the perturbation, the exact
command and the exact failure text, so the claim that a gate can fail lives beside the entries it
substantiates rather than in a pull-request description or a reviewer's scratch directory.

The first fourteen sections were committed first. The rest arrived later, out of five session evidence
files that were outside the repository and so openable by nobody; § The rest of the transcripts says
what changed in bringing them in. § Which entry each section controls is the index: it lists every
entry in the seven `SIM-00*.json` files against the section that controls it, or against the reason
there is none.

This file is no longer scoped by an `evidenceKinds` value. It was, by `negative-control`, and that
value is gone from every `SIM-00*.json` entry: `docs/technical/91-verification-strategy.md` § What a
kind may name rules that a technique is not a kind and excludes it from the minted inventory. The
entries whose control is recorded here name this file in their own `fixtures`, qualified by section,
and each registry file's `notes` say which of its entries that is. An entry whose control is **not**
recorded here names this file nowhere, is listed in § Which entry each section controls with the
reason, and is substantiated by the committed test its selector names rather than by this file. No
transcript here should be read as covering an entry the table does not credit to it.

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

All eleven in this section were re-run at `a06bd18` with the rebuild forced and proved by a changed
assembly hash. Every one went red. None failed to reproduce. The sections after
§ Controls for the second review pass are the later batches, each of which states its own runner and its
own revert check.

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

## The rest of the transcripts

The fourteen sections above were the first batch committed. Everything below is the
remainder of the perturbation work, brought in from the session evidence files it used to
live in. Those files were outside the repository, so every claim resting on them was a
pointer rather than a claim; `docs/technical/91-verification-strategy.md` § Claim and
measurement discipline's fourth rule is why that is not good enough.

Three things about the form of what follows.

**The fenced blocks are verbatim.** Each one is copied byte-for-byte from the run that
produced it. Three normalizations are applied, uniformly and nowhere else. An absolute
repository or worktree path is rewritten to `<repo>/`, the session perturbation harness's
own path to `<session harness>/`, and a scratch results directory to `<tmpdir>/`, so that
no transcript carries a path only one machine has. Trailing whitespace is stripped, because
the repository's text rules forbid it on a tracked file and it occurs here only as the empty
tail of a rendered list. File names and line numbers inside stack traces are untouched,
because they are the part a reader checks. Nothing else in a block is edited, elided, or
re-typed. Where a block was already trimmed to its assertion lines by the run that recorded
it, it says so.

**The prose is not verbatim.** Section titles and the sentences around each block are
written here rather than carried over, so that each section states which registry entries
it controls in the vocabulary the registry actually uses now. Several of the original
headings named a gate by an entry ID that has since been retired or renumbered.

**An entry is credited with a control only when the transcript shows that entry's own
test failing.** Not the class, not a sibling assertion, not the perturbation's intent: the
method the entry's `selector` names has to appear in the recorded failure. That rule is
strict on purpose, because the alternative is the defect this file exists to prevent, and
applying it moved seven attributions. Five sections turned out to claim a gate the
transcript does not show failing, and two `fixtures` pointers already in the registry turned
out to name a section that does not show the entry's own gate going red, though both of those
pointers hold on a narrower reading that is now stated rather than assumed. All seven are
recorded under § Attributions that did not survive the rule rather than quietly fixed.

Two attributions rest on the command's `--filter` rather than on a named failing test,
because the run captured a golden diff instead of a `Failed <method>` line. They are
marked where they occur.

## SIM-001 and SIM-002: the fixed-step host and the pause contract

Twenty-four controls. Every one was applied to the tree, run, captured, and reverted from
a pre-perturbation snapshot, with the suite re-run green afterwards. The command shape for
all of them:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --nologo -v n --filter "FullyQualifiedName~<filter>"
```

Each section below gives the exact filter it used. Exit code with the perturbation applied
was `1` in every case; a `0` would mean the gate is not real.

State after the last revert:

```
$ ./build.sh build
ok    dotnet-restore-locked: exit 0
ok    dotnet-build: exit 0
ok    zero-warnings: reported 0 warning(s) and 0 error(s)
ok    verify-architecture: exit 0
OK [MMT-0000] build debug (MSBuild Debug) succeeded with 0 warnings, 0 errors, and an intact project boundary

$ ./build.sh format-check
ok    dotnet-format-whitespace: exit 0
ok    dotnet-format-style: exit 0
ok    owned-text-rules: every owned text file satisfies end_of_line, insert_final_newline, and trim_trailing_whitespace
OK [MMT-0000] format-check passed all three gates; nothing was written

$ ./build.sh test-fast
ok    test-MechaMiner.Simulation.Tests: total 60, passed 60, failed 0, skipped 0
OK [MMT-0000] test-fast: total 68, passed 68, failed 0, skipped 0
```

The tallies there are the batch's own baseline (68 fast-tier tests at the time), not the
current one. The current baseline is 147.

### Two probes that did not fail on the first attempt

Both are recorded because they found something rather than confirming something.

**`VER-SIM-001-001`, where the first probe was not a perturbation at all.** The first
attempt replaced the exact-rational `SecondsPerTick` with the literal
`0.0166666666666666666` and the gate stayed green. That literal rounds to exactly the same
`double` as `1.0 / 60.0`, so nothing had changed. No production or test code was altered
for this: the probe was wrong, not the gate. It was replaced by the three real
perturbations recorded below, and all three fail.

**`VER-SIM-002-009`, where the fixture was genuinely too weak.** Making the host bank
paused wall time into the accumulator left the gate green, for an arithmetic reason rather
than a lucky one: the fixture paused for four steps of 0.25 s, exactly 60 ticks, so
banking raised the accumulator's debt by a whole number of ticks, the catch-up bound
discarded exactly that surplus, and the retained remainder returned to its pre-pause
value. A whole-tick pause interval hides the defect. The blocked interval is now four
dyadic steps totalling `11/128 s`, which is 5.15625 ticks and deliberately not whole, so
banking shifts the remainder and the sequence diverges. The golden's data lines did not
change, because the unpaused run did not change. The control then fails, as recorded under
§ A pause banks wall time into the accumulator.

### The tick rate is changed to 50 Hz

**Entries controlled.** `VER-SIM-001-001`

**Perturbation** (`src/MechaMiner.Simulation/Time/TickRate.cs`). The numerator of the rate is changed from 60 to 50.

Filter: `TickRateTests`

Perturbed from:

```csharp
public const int TicksPerSecondNumerator = 60;
```

to:

```csharp
public const int TicksPerSecondNumerator = 50;
```

Verbatim failure:

```
  Failed FrequencyIsSixtyHertzAndConstantWithinARun [2 s]
  Error Message:
   Multiple failures or warnings in test:
  1)   tick rate numerator: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 60
  But was:  50
Total tests: 3
```

### The rate is declared a second time

**Entries controlled.** `VER-SIM-001-001`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/RunClock.cs`). A second, forkable statement of the rate is added to `RunClock`. Nothing behavioural changes, which is the point: doc 10 makes the rate architectural and it is stated once, so a second declaration is the defect even before it drifts.

Filter: `TickRateTests`

Perturbed from:

```csharp
    public const int FinalBoundaryMinutes = 35;
```

to:

```csharp
    public const int FinalBoundaryMinutes = 35;

    /// <summary>A second, forkable statement of the rate.</summary>
    public const int TicksPerSecond = 60;
```

Verbatim failure:

```
  Failed TheRateIsDeclaredInExactlyOnePlace [34 ms]
  Error Message:
     doc 10 § Clock domains makes the tick rate architectural; it is stated once, on TickRate, so a second declaration cannot drift from the first
Assert.That(elsewhere, Is.Empty)
  Expected: <empty>
  But was:  < "MechaMiner.Simulation.Runtime.RunClock.TicksPerSecond" >
Total tests: 3
```

### A tick-rate member becomes settable

**Entries controlled.** `VER-SIM-001-001`

**Perturbation** (`src/MechaMiner.Simulation/Time/TickRate.cs`). `TicksPerMinute` is turned from a constant into a settable static property.

Filter: `TickRateTests`

Perturbed from:

```csharp
    public const int TicksPerMinute = TicksPerSecond * 60;
```

to:

```csharp
    public static int TicksPerMinute { get; set; } = TicksPerSecond * 60;
```

Verbatim failure:

```
  Failed NoApiCanChangeTheRate [32 ms]
  Error Message:
     every tick-rate member is a constant or read-only, so the rate cannot change within a run (doc 10 § Clock domains: "It is constant within a run")
Assert.That(mutable, Is.Empty)
  Expected: <empty>
  But was:  < "TicksPerMinute", "<TicksPerMinute>k__BackingField" >
Total tests: 3
```

### The tick count becomes fractional

**Entries controlled.** `VER-SIM-001-002`

**Perturbation** (`src/MechaMiner.Simulation/Time/TickBudget.cs`). `TickBudget.TickCount` is changed from `int` to `double`. This one does not reach the test: it fails to compile, which is the strongest possible form of the same refusal and is recorded as what it is rather than as a test failure.

Filter: `FixedStepAccumulatorTests.OnlyWholeTicksAreEverYielded`

Perturbed from:

```csharp
public int TickCount { get; }
```

to:

```csharp
public double TickCount { get; }
```

Verbatim failure:

```
     2><repo>/src/MechaMiner.Simulation/Runtime/PerformanceDiagnostics.cs(102,41): error CS1503: Argument 2: cannot convert from 'double' to 'int' [<repo>/src/MechaMiner.Simulation/MechaMiner.Simulation.csproj]
         <repo>/src/MechaMiner.Simulation/Runtime/PerformanceDiagnostics.cs(102,41): error CS1503: Argument 2: cannot convert from 'double' to 'int' [<repo>/src/MechaMiner.Simulation/MechaMiner.Simulation.csproj]
```

### The retained fraction is exposed

**Entries controlled.** `VER-SIM-001-002`

**Perturbation** (`src/MechaMiner.Simulation/Time/FixedStepAccumulator.cs`). A member is added that exposes the retained fraction a tick target must never see.

Filter: `FixedStepAccumulatorTests.OnlyWholeTicksAreEverYielded`

Perturbed from:

```csharp
    public CatchUpPolicy Policy => _policy;
```

to:

```csharp
    public CatchUpPolicy Policy => _policy;

    /// <summary>Exposes the retained fraction a tick target must never see.</summary>
    public double RetainedFractionSeconds => _retainedSeconds;
```

Verbatim failure:

```
  Failed OnlyWholeTicksAreEverYielded [44 ms]
  Error Message:
     no member may expose the retained fraction: doc 10 § Clock domains says the host "never passes a variable delta to authoritative systems", which a readable remainder would immediately undo
Assert.That(exposed, Is.Empty)
  Expected: <empty>
  But was:  < "FixedStepAccumulator.get_RetainedFractionSeconds", "FixedStepAccumulator.get_RetainedFractionSeconds", "FixedStepAccumulator.RetainedFractionSeconds", "FixedStepAccumulator.RetainedFractionSeconds" >
Total tests: 1
```

### The accumulator ceilings instead of flooring

**Entries controlled.** `VER-SIM-001-002`

**Perturbation** (`src/MechaMiner.Simulation/Time/FixedStepAccumulator.cs`). The floor plus boundary snap is replaced by a ceiling, so a step yields one tick more than its elapsed time covers.

Filter: `FixedStepAccumulatorTests.OnlyWholeTicksAreEverYielded`

Perturbed from:

```csharp
long dueTicks = (long)Math.Floor(dueTicksExact + TickBoundarySnapTicks);
```

to:

```csharp
long dueTicks = (long)Math.Ceiling(dueTicksExact);
```

Verbatim failure:

```
  Failed OnlyWholeTicksAreEverYielded [37 ms]
  Error Message:
     the stream must yield exactly the whole ticks its total elapsed time covers, with no accumulated drift (doc 10 § Clock domains, VER-SIM-001-003)
Assert.That(total, Is.EqualTo((double)expectedTotalTicks))
  Expected: 33.0d
  But was:  34.0d
Total tests: 1
```

### Long-run drift by subtraction

**Entries controlled.** `VER-SIM-001-003`

**Perturbation** (`src/MechaMiner.Simulation/Time/FixedStepAccumulator.cs`). The boundary snap term is dropped from the floor, which is the shape accumulated floating-point drift takes. A full run of irregular deltas totalling exactly 2,100 s then yields 125,999 ticks instead of 126,000. One tick over 35 minutes is exactly the magnitude a gate without an exact-equality rule would tolerate.

Filter: `FixedStepAccumulatorTests.ZeroOneAndManyTicksPerStepWithoutLongRunDrift`

Perturbed from:

```csharp
long dueTicks = (long)Math.Floor(dueTicksExact + TickBoundarySnapTicks);
```

to:

```csharp
long dueTicks = (long)Math.Floor(dueTicksExact);
```

Verbatim failure:

```
  Failed ZeroOneAndManyTicksPerStepWithoutLongRunDrift [67 ms]
  Error Message:
     a full run of irregular deltas totalling exactly 2,100 s must yield exactly 126,000 ticks; anything else is accumulated drift (doc 10 § Clock domains): doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 126000
  But was:  125999
Total tests: 1
```

### Seconds accumulated instead of divided

**Entries controlled.** `VER-SIM-001-004`

**Perturbation** (`src/MechaMiner.Simulation/Time/TickRate.cs`). The single rational division is replaced by a loop that adds `1.0 / 60.0` per tick. The failure is read on the bit pattern of the double rather than on its printed value, so the two results differ by 1,917 and 242 units in the last place respectively and neither would have been visible at any sane print precision.

Filter: `SimulationTickTests`

Perturbed from:

```csharp
        return (double)(tickCount * TicksPerSecondDenominator) / TicksPerSecondNumerator;
```

to:

```csharp
        double accumulated = 0.0;
        for (long index = 0; index < tickCount; index++)
        {
            accumulated += 1.0 / TicksPerSecondNumerator;
        }

        return accumulated;
```

Verbatim failure:

```
  Failed DerivedSecondsComeFromTheTickIndexNotAccumulatedDeltas [304 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   126,000 ticks is exactly 2,100 seconds, because the rate is an exact rational
Assert.That(BitConverter.DoubleToInt64Bits(fromIrregular.Seconds), Is.EqualTo(BitConverter.DoubleToInt64Bits(FrameDeltaStreams.FullRunSeconds)))
  Expected: 4656836363910381568
  But was:  4656836363910379651
  Failed SecondsAreOneDivisionOfTheIndex [5 ms]
  Error Message:
     the derived value is the single quotient of the index and the rational rate
Assert.That(BitConverter.DoubleToInt64Bits(direct.Seconds), Is.EqualTo(BitConverter.DoubleToInt64Bits(5_000.0 / 60.0)))
  Expected: 4635564478951675221
  But was:  4635564478951674979
Total tests: 2
```

### The catch-up bound is removed

**Entries controlled.** `VER-SIM-001-005`

**Perturbation** (`src/MechaMiner.Simulation/Time/FixedStepAccumulator.cs`). The policy maximum is replaced by `int.MaxValue`, so a stall is caught up in full instead of being bounded and reported.

Filter: `CatchUpPolicyTests.SurplusBeyondTheBoundIsDiscardedAndReportedNotQueued`

Perturbed from:

```csharp
        int maximum = _policy.MaximumTicksPerStep;
        if (dueTicks > maximum)
```

to:

```csharp
        int maximum = int.MaxValue;
        if (dueTicks > maximum)
```

Verbatim failure:

```
  Failed SurplusBeyondTheBoundIsDiscardedAndReportedNotQueued [48 ms]
  Error Message:
     a stall worth 10 ticks must run exactly the bound's 4 ticks (doc 10 § Clock domains)
Assert.That(yielded, Is.EqualTo((double)bound))
  Expected: 4.0d
  But was:  10.0d
Total tests: 1
```

### The bound loses its headroom tick

**Entries controlled.** `VER-SIM-001-006`

**Perturbation** (`src/MechaMiner.Simulation/Time/CatchUpPolicy.cs`). The default headroom is changed from one tick to zero, which is what the bound drifting to three whole ticks would look like.

Filter: `CatchUpPolicyTests.BoundAbsorbsTheLargestToleratedStallAndNoMore`

Perturbed from:

```csharp
public const int HeadroomTicksDefault = 1;
```

to:

```csharp
public const int HeadroomTicksDefault = 0;
```

Verbatim failure:

```
  Failed BoundAbsorbsTheLargestToleratedStallAndNoMore [56 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   one tick of headroom, so a frame measured at the tolerance cannot trip the bound on a fractional remainder: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 1
  But was:  0
Total tests: 1
```

### One diagnostic per discarded tick instead of one per occurrence

**Entries controlled.** `VER-SIM-001-007`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/SimulationHost.cs`). The single diagnostic record is put inside a loop over the discarded tick count, so two stalls discarding 32 ticks between them emit 32 records instead of 2.

Filter: `SimulationHostCatchUpDiagnosticTests`

Perturbed from:

```csharp
            _diagnostics.RecordCatchUpBoundReached(_clock.CurrentTick, budget);
```

to:

```csharp
            for (int discarded = 0; discarded < budget.DiscardedTickCount; discarded++)
            {
                _diagnostics.RecordCatchUpBoundReached(_clock.CurrentTick, budget);
            }
```

Verbatim failure:

```
  Failed ReachingTheBoundEmitsOneDiagnosticCarryingCountAndDebt [38 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   exactly one diagnostic per occurrence: two stalls discarding 32 ticks between them produce two records, not 32: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 2
  But was:  32
Total tests: 2
```

### SIM-001's permanent negative control, with its stub repaired

**Entries controlled.** `VER-SIM-001-008`

**Perturbation** (`tests/MechaMiner.Simulation.Tests/Time/FractionalTickAccumulatorSubject.cs`). The deliberately broken stub is *repaired*, so it no longer yields fractional ticks. This is the control on the control: if the negative-control test still passed against a correct stub it would be asserting nothing. Note what the failure shows, which is the point of asserting on the message rather than on the fact of an exception. The stub still fails the whole-tick assertion, but for the wrong reason, so the control correctly reports that the failure it was looking for is not the failure it got.

Filter: `AccumulatorNegativeControlTests`

Perturbed from:

```csharp
        return elapsedSeconds * TickRate.TicksPerSecondNumerator / TickRate.TicksPerSecondDenominator;
```

to:

```csharp
        return System.Math.Floor(
            (elapsedSeconds * TickRate.TicksPerSecondNumerator) / TickRate.TicksPerSecondDenominator);
```

Verbatim failure:

```
  Failed WholeTickAndCatchUpAssertionsFailAgainstDeliberatelyBrokenStubs [64 ms]
  Error Message:
     the whole-tick assertion must fail for the reason it exists, not incidentally
Assert.That(fractionalFailure.Message, Does.Contain("fractional tick"))
  Expected: String containing "fractional tick"
  But was:  "  the stream must yield exactly the whole ticks its total elapsed time covers, with no accumulated drift (doc 10 § Clock domains, VER-SIM-001-003)
Assert.That(total, Is.EqualTo((double)expectedTotalTicks))
  Expected: 33.0d
  But was:  18.0d
"
Total tests: 1
```

### Resume catches up instead of discarding

**Entries controlled.** `VER-SIM-001-009`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/RunLifecycleHooks.cs`). The lifecycle discard is removed from the suspension-resume hook, so 15 minutes of wall time is caught up rather than discarded.

Filter: `SimulationHostTests.FocusLossAndSuspendResumeDiscardElapsedWallTime`

Perturbed from:

```csharp
        PauseTransitionResult result = _clock.Clear(PauseReason.OperatingSystemSuspension);
        _accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.OperatingSystemSuspension);
        return result;
```

to:

```csharp
        return _clock.Clear(PauseReason.OperatingSystemSuspension);
```

Verbatim failure:

```
  Failed FocusLossAndSuspendResumeDiscardElapsedWallTime(OperatingSystemSuspension,OperatingSystemSuspension) [32 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   the step after the resume runs zero ticks: 15 minutes of wall time is discarded, not caught up (doc 10 § Clock domains): doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 0
  But was:  4
Total tests: 2
```

### A tick is invoked twice

**Entries controlled.** `VER-SIM-001-010`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/SimulationHost.cs`). The tick-target invocation is duplicated.

Filter: `SimulationHostTests.TickTargetIsInvokedOncePerTickInAscendingOrder`

Perturbed from:

```csharp
                _world.AdvanceTick(tick);
```

to:

```csharp
                _world.AdvanceTick(tick);
                _world.AdvanceTick(tick);
```

Verbatim failure:

```
  Failed TickTargetIsInvokedOncePerTickInAscendingOrder [69 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   every tick is invoked exactly once, ascending, with no gap and no repeat
Assert.That(actual, Is.EqualTo(expectedSequence).AsCollection)
  Expected is <System.Collections.Generic.List`1[System.Int64]> with 33 elements, actual is <System.Collections.Immutable.ImmutableArray`1[System.Int64]> with 66 elements
  Values differ at index [1]
  Expected: 1
  But was:  0
Total tests: 1
```

### Run time accumulates instead of dividing the index

**Entries controlled.** `VER-SIM-001-011`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/RunClock.cs`). `RunSeconds` is changed from the tick index over the exact rational rate to the committed tick count times `SecondsPerTick`. Those agree to within one unit in the last place, and the gate is red on exactly that: the expected and actual bit patterns differ by one. A tolerance-based assertion here would have passed.

Filter: `RunClockTests.RunTimeAdvancesOnlyOnCommittedTicks`

Perturbed from:

```csharp
    public double RunSeconds => CurrentTick.Seconds;
```

to:

```csharp
    public double RunSeconds => CommittedTickCount * TickRate.SecondsPerTick;
```

Verbatim failure:

```
  Failed RunTimeAdvancesOnlyOnCommittedTicks [643 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   run time is the tick index over the exact rational rate, at tick 23
Assert.That(BitConverter.DoubleToInt64Bits(clock.RunSeconds), Is.EqualTo(BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(committed))))
  Expected: 4600577139346540681
  But was:  4600577139346540680
Total tests: 1
```

### A 35:00 event is admitted before the boundary is evaluated

**Entries controlled.** `VER-SIM-001-012`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/SimulationHost.cs`). The refusal of events scheduled at or after the final boundary is made conditional on the boundary already having been evaluated, which inverts the ordering the entry is about.

Filter: `FinalBoundaryOrderingTests`

Perturbed from:

```csharp
        if (scheduledTick >= RunClock.FinalBoundaryTick)
        {
            return false;
        }
```

to:

```csharp
        if (scheduledTick >= RunClock.FinalBoundaryTick && _clock.TerminalBoundaryEvaluated)
        {
            return false;
        }
```

Verbatim failure:

```
  Failed ExtractionBoundaryIsEvaluatedBeforeAnyEventAtOrAfterThirtyFiveMinutes [189 ms]
  Error Message:
     exactly the two pre-boundary events were begun; every 35:00-or-later event was refused, so the boundary evaluation precedes all of them: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 2
  But was:  5
Total tests: 1
```

### An eighth pause reason is defined

**Entries controlled.** `VER-SIM-002-001`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/PauseReason.cs`). An unregistered eighth reason is added to the enum.

Filter: `PauseReasonSetTests.ExactlyTheSevenBlockingReasonsAreDefined`

Perturbed from:

```csharp
    TerminalTransition = 64,
```

to:

```csharp
    TerminalTransition = 64,

    /// <summary>An unregistered eighth reason.</summary>
    NetworkStall = 128,
```

Verbatim failure:

```
  Failed ExactlyTheSevenBlockingReasonsAreDefined [66 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   the enum defines exactly seven reasons: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 7
  But was:  8
Total tests: 1
```

### The host blocks on one reason instead of any

**Entries controlled.** `VER-SIM-002-002`, `VER-SIM-002-003`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/SimulationHost.cs`). The host tests for `GeneralPause` specifically instead of asking whether any blocking reason is present, which is the single-toggle pause the matrix exists to forbid. Two entries are controlled here rather than one: both `NoTickExecutesWhileAnySingleReasonIsPresent` and `ResumesOnlyWhenEveryOverlappingReasonIsCleared` go red, and the exception names the reason that was ignored.

Filter: `PauseMatrixTests`

Perturbed from:

```csharp
        PauseReasonSet blocking = _clock.BlockingReasons;
        if (blocking.IsBlocking)
```

to:

```csharp
        PauseReasonSet blocking = _clock.BlockingReasons;
        if (blocking.Contains(PauseReason.GeneralPause))
```

Verbatim failure:

```
  Failed NoTickExecutesWhileAnySingleReasonIsPresent [35 ms]
  Error Message:
   System.InvalidOperationException : no tick commits while a blocking reason is present (doc 10 § Pause contract); present: Fabrication
  Failed ResumesOnlyWhenEveryOverlappingReasonIsCleared [7 ms]
  Error Message:
   System.InvalidOperationException : no tick commits while a blocking reason is present (doc 10 § Pause contract); present: Fabrication
Total tests: 4
```

### The reason set mutates in place

**Entries controlled.** `VER-SIM-002-004`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/PauseReasonSet.cs`). `With` also sets `GeneralPause`, so adding a reason double-counts.

Filter: `PauseReasonSetTests.SetIsImmutableAndIdempotent`

Perturbed from:

```csharp
        return new PauseReasonSet((byte)(_mask | MaskOf(reason)));
```

to:

```csharp
        return new PauseReasonSet((byte)(_mask | MaskOf(reason) | MaskOf(PauseReason.GeneralPause)));
```

Verbatim failure:

```
  Failed SetIsImmutableAndIdempotent [60 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   and does not double-count the reason: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 1
  But was:  2
Total tests: 1
```

### Focus recovery clears every reason

**Entries controlled.** `VER-SIM-002-005`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/RunLifecycleHooks.cs`). Focus recovery clears all reasons rather than only focus loss, which would dismiss a user-requested pause on regaining focus.

Filter: `FocusAndSuspendHookTests.FocusRecoveryDismissesOnlyTheFocusLossReason`

Perturbed from:

```csharp
        PauseTransitionResult result = _clock.Clear(PauseReason.FocusLoss);
        _accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.FocusLoss);
        return result;
```

to:

```csharp
        PauseTransitionResult result = _clock.Clear(PauseReason.FocusLoss);
        foreach (PauseReason other in PauseReasonSet.AllReasons)
        {
            result = _clock.Clear(other);
        }

        _accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.FocusLoss);
        return result;
```

Verbatim failure:

```
  Failed FocusRecoveryDismissesOnlyTheFocusLossReason [38 ms]
  Error Message:
     focus recovery never dismisses GeneralPause; doc 10 § Pause contract: "Focus recovery never dismisses a menu, tutorial, relic choice, or user-requested pause"
Assert.That(run.Contains(survivor), Is.True)
  Expected: True
  But was:  False
Total tests: 1
```

### The UI clock is frozen while paused

**Entries controlled.** `VER-SIM-002-007`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/HostStepResult.cs`). A blocked step reports zero elapsed UI seconds instead of its own, so pause presentation cannot animate.

Filter: `PauseMatrixTests.UiClockAdvancesWhileNoGameplayClockDoes`

Perturbed from:

```csharp
            blockingReasons,
            elapsedUiSeconds,
            0.0,
            AccumulatorDiscardReason.None,
            false);
```

to:

```csharp
            blockingReasons,
            0.0,
            0.0,
            AccumulatorDiscardReason.None,
            false);
```

Verbatim failure:

```
  Failed UiClockAdvancesWhileNoGameplayClockDoes [63 ms]
  Error Message:
     every blocked step reports its own elapsed UI seconds, so pause presentation can animate (doc 10 § Pause contract)
Assert.That(reportedUiSeconds, Is.EqualTo(blockedSteps).AsCollection)
  Expected is <System.Double[3]>, actual is <System.Collections.Generic.List`1[System.Double]> with 3 elements
  Values differ at index [0]
  Expected: 0.25d
  But was:  0.0d
Total tests: 1
```

### The terminal transition can be cleared

**Entries controlled.** `VER-SIM-002-008`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/RunClock.cs`). The one-way guard on the terminal transition is disabled, so clearing it is accepted rather than refused. Note that this is the one place a literal `false` was usable, because it guards a compound condition rather than a whole statement and so does not produce unreachable code.

Filter: `PauseReasonSetTests.TerminalTransitionCannotBeClearedBackIntoAnActiveRun`

Perturbed from:

```csharp
        if (reason == PauseReason.TerminalTransition && BlockingReasons.Contains(reason))
```

to:

```csharp
        if (false && reason == PauseReason.TerminalTransition && BlockingReasons.Contains(reason))
```

Verbatim failure:

```
  Failed TerminalTransitionCannotBeClearedBackIntoAnActiveRun [154 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   clearing it is rejected rather than silently ignored (VER-SIM-002-008)
Assert.That(refused.Outcome, Is.EqualTo(PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay))
  Expected: RefusedTerminalTransitionIsOneWay
  But was:  Cleared
Total tests: 1
```

### A pause banks wall time into the accumulator

**Entries controlled.** `VER-SIM-002-009`

**Perturbation** (`src/MechaMiner.Simulation/Runtime/SimulationHost.cs`). The blocked path advances the accumulator before returning, which is the defect doc 10 § Pause contract forbids. This is the control the strengthened fixture was built for: against the original whole-tick pause interval it left the gate green.

Filter: `PauseMatrixTests.PauseBoundaryConsumesNoGameplayTime`

Perturbed from:

```csharp
        if (blocking.IsBlocking)
        {
            return HostStepResult.Blocked(blocking, elapsedSeconds);
        }
```

to:

```csharp
        if (blocking.IsBlocking)
        {
            _accumulator.Advance(elapsedSeconds);
            return HostStepResult.Blocked(blocking, elapsedSeconds);
        }
```

Verbatim failure:

```
  Failed PauseBoundaryConsumesNoGameplayTime [61 ms]
  Error Message:
     a pause consumes no gameplay time, so the tick sequence is identical
Assert.That(paused.CanonicalText, Is.EqualTo(unpaused.CanonicalText).Using(StringComparer.Ordinal))
  Expected string length 1860 but was 1871. Strings differ at index 1612.
  Expected: "# authority: docs/technical/10-runtime-architecture.md § Pause contract\n#   "The simulation executes no ticks while any blocking reason is present."\n#   "Run time, AI, movement, spawning, projectiles, attacks, cooldowns, status effects,\n#   mining progress and decay, hazards, pickups, and gameplay physics remain unchanged."\n#   docs/technical/20-simulation-core.md § Verification.\ [...truncated for readability...]
  But was:  "# authority: docs/technical/10-runtime-architecture.md § Pause contract\n#   "The simulation executes no ticks while any blocking reason is present."\n#   "Run time, AI, movement, spawning, projectiles, attacks, cooldowns, status effects,\n#   mining progress and decay, hazards, pickups, and gameplay physics remain unchanged."\n#   docs/technical/20-simulation-core.md § Verification.\ [...truncated for readability...]
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- [...truncated for readability...]
Total tests: 1
```

### SIM-002's permanent negative control, with its stub repaired

**Entries controlled.** `VER-SIM-002-010`

**Perturbation** (`tests/MechaMiner.Simulation.Tests/Runtime/ClearEverythingOnFocusRecoveryRun.cs`). The stub that clears every reason on focus recovery is repaired to call the real recovery, so it no longer misbehaves and the control must notice.

Filter: `PauseNegativeControlTests`

Perturbed from:

```csharp
        foreach (PauseReason reason in PauseReasonSet.AllReasons)
        {
            _inner.ClearReason(reason);
        }
```

to:

```csharp
        _inner.RecoverFocus();
```

Verbatim failure:

```
  Failed PauseAssertionsFailAgainstDeliberatelyBrokenStubs [63 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <NUnit.Framework.AssertionException>
  But was:  null
Total tests: 1
```

## SIM-003, SIM-006 and SIM-007: identity, event buffers and snapshots

Twenty-two controls. Each was applied from a byte-for-byte backup taken immediately before
the edit and restored immediately after the run. Every section states its own command,
because the filters differ; the recorded output was trimmed to the assertion lines by the
run that captured it, and the tally line is kept wherever it was captured, because
`Failed: 1, Passed: 10` is what shows that the other ten did *not* go red.

Baseline before any perturbation in this batch:

```
OK [MMT-0000] test-fast: total 62, passed 62, failed 0, skipped 0
verb:    test-fast   exit class 0 (success)   owner FND-003
```

Two of this batch's perturbations are the same ones already recorded above at a later
revision with the forced rebuild proved, so they are not duplicated here: removing
`RecordStaleReference()` is § Entities: the stale-reference gate refuses without counting,
and the domain buffer's silent drop at its ceiling is § Events: the domain buffer drops at
its ceiling. What the earlier run of each adds is recorded with those sections in
§ Attributions that did not survive the rule.

### The store resolves a stale generation

**Entries controlled.** `VER-SIM-003-002`, `VER-SIM-003-005`, `VER-SIM-003-012`

**Perturbation** (`src/MechaMiner.Simulation/Entities/PackedEntityStore.cs`). The `_denseIds[dense] != id` check is dropped from `ResolveDense`, so a stale generation resolves to whatever now occupies the slot. This is the exact aliasing generations exist to prevent. Three entries go red, and the third is the control-of-the-control: the fixture that proves the store assertions can fail notices that the real store now behaves like a broken stub.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed StaleGenerationFailsClosedAndCountsADiagnostic [50 ms]
  Error Message:
  1)   the packed mining-site store: a generation-mismatched reference must fail closed, not resolve to the live record now occupying the slot
Assert.That(staleResolved, Is.False)
  Expected: False
  But was:  True
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   the packed mining-site store: exactly one diagnostic counter increment per failed resolution
Assert.That(afterStale - before, Is.EqualTo(1L))
  Expected: 1
  But was:  0
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.StoreContractAssertions.<>c__DisplayClass0_0.<GenerationMismatchFailsClosed>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreContractAssertions.cs:line 69
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.StoreContractAssertions.<>c__DisplayClass0_0.<GenerationMismatchFailsClosed>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreContractAssertions.cs:line 79
   at NUnit.Framework.Assert.Multiple(Action action)
  Failed IdentityAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs [37 ms]
  Error Message:
     Assert.That(code, new ThrowsNothingConstraint())
  Expected: No Exception to be thrown
  But was:  <NUnit.Framework.MultipleAssertException: Multiple failures or warnings in test:
  1)   the real packed store: a generation-mismatched reference must fail closed, not resolve to the live record now occupying the slot
Assert.That(staleResolved, Is.False)
  Expected: False
  But was:  True
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   the real packed store: exactly one diagnostic counter increment per failed resolution
Assert.That(afterStale - before, Is.EqualTo(1L))
  Expected: 1
  But was:  0
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.AssertionScope.Dispose()
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Tests.Support.Expect.DoesNotThrow(Action code) in <repo>/tests/shared/Expect.cs:line 37
  Failed LiveAndFreedIdentitiesMatchTheReferenceModel [36 ms]
  Error Message:
  1)   step 6: every freed or superseded identity must fail closed: entity:3822/g1@run2290614273
Assert.That(store.TryGet(id, out long _), Is.False)
  Expected: False
```

### The store iterates in storage order

**Entries controlled.** `VER-SIM-003-010`, `VER-SIM-003-012`

**Perturbation** (`src/MechaMiner.Simulation/Entities/PackedEntityStore.cs`). `CompareDense` is replaced by a comparison of dense storage indices, so iteration order becomes insertion order and both the authored priority key and the full entity ID are ignored. The first failure is the negative control refusing to proceed on a fixture where insertion order and key order no longer differ, which is a vacuity check firing rather than the ordering assertion itself.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed IdentityAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs [52 ms]
  Error Message:
     the fixture must be one where insertion order and key order genuinely differ, or the control proves nothing
Assert.That(insertionRendering, Is.Not.EqualTo(correctRendering))
  Expected: not equal to "  0  priority=    30  entity:3830/g1@run3735879681
  But was:  "  0  priority=    30  entity:3830/g1@run3735879681
1)    at MechaMiner.Simulation.Tests.Entities.EntityStoreNegativeControlTests.AssertInsertionOrderedStoreFailsTheOrderingGate() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/EntityStoreNegativeControlTests.cs:line 136
  Failed IterationOrderIsPriorityKeysThenFullEntityId [7 ms]
  Error Message:
  1)   the packed pickup store, distinct priority keys: two stores holding the same members inserted in different orders must iterate identically, so no observable order comes from insertion or collection enumeration
Assert.That(secondRendering, Is.EqualTo(firstRendering))
  Expected: "30\n10\n20\n40\n50\n15\n25\n35\n"
  But was:  "35\n25\n15\n50\n40\n20\n10\n30\n"
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   the packed pickup store, distinct priority keys: order must be authored priority key ascending, then the full entity ID
Assert.That(firstRendering, Is.EqualTo(expectedRendering))
  Expected: "10\n15\n20\n25\n30\n35\n40\n50\n"
  But was:  "30\n10\n20\n40\n50\n15\n25\n35\n"
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.StoreContractAssertions.<>c__DisplayClass1_0.<IterationOrderMatchesTheDocumentedComparison>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreContractAssertions.cs:line 112
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.StoreContractAssertions.<>c__DisplayClass1_0.<IterationOrderMatchesTheDocumentedComparison>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreContractAssertions.cs:line 118
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     2, Passed:    11, Skipped:     0, Total:    13, Duration: 201 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### An exhausted generation wraps instead of retiring the slot

**Entries controlled.** `VER-SIM-003-006`

**Perturbation** (`src/MechaMiner.Simulation/Entities/EntityIdAllocator.cs`). The retirement branch is removed from `TryFree`, so a slot at the generation ceiling re-enters the free list and the next allocation wraps its generation. Six assertions fail, covering retirement, its diagnostic, the refusal to re-issue, the unset result, the counted rejection, and the live count.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities.EntityIdAllocatorTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed GenerationExhaustionRetiresTheSlotRatherThanAliasing [151 ms]
  Error Message:
  1)   the slot must be retired once its generation is exhausted
Assert.That(allocator.IsRetired(lastIssued), Is.True)
  Expected: True
  But was:  False
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   retirement must be a counted diagnostic, not a silent state
Assert.That(diagnostics.RetiredSlotCount, Is.EqualTo(1))
  Expected: 1
  But was:  0
   at NUnit.Framework.Assert.Multiple(Action action)
  3)   a retired slot must not be re-issued; the partition is now exhausted
Assert.That(allocator.TryAllocate(PopulationCategory.MiningSite, out EntityId afterRetirement), Is.False)
  Expected: False
  But was:  True
   at NUnit.Framework.Assert.Multiple(Action action)
  4)   Assert.That(afterRetirement.IsUnset, Is.True)
  Expected: True
  But was:  False
   at NUnit.Framework.Assert.Multiple(Action action)
  5)   the refused allocation must be counted rather than served by wrapping
Assert.That(diagnostics.RejectedRequests, Is.EqualTo(1L))
  Expected: 1
  But was:  0
   at NUnit.Framework.Assert.Multiple(Action action)
  6)   Assert.That(allocator.LiveCount(PopulationCategory.MiningSite), Is.EqualTo(0))
  Expected: 0
  But was:  1
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.EntityIdAllocatorTests.<>c__DisplayClass6_0.<GenerationExhaustionRetiresTheSlotRatherThanAliasing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/EntityIdAllocatorTests.cs:line 221
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.EntityIdAllocatorTests.<>c__DisplayClass6_0.<GenerationExhaustionRetiresTheSlotRatherThanAliasing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/EntityIdAllocatorTests.cs:line 225
   at NUnit.Framework.Assert.Multiple(Action action)
3)    at MechaMiner.Simulation.Tests.Entities.EntityIdAllocatorTests.<>c__DisplayClass6_0.<GenerationExhaustionRetiresTheSlotRatherThanAliasing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/EntityIdAllocatorTests.cs:line 229
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 282 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The hard capacity ignores the margin

**Entries controlled.** `VER-SIM-003-008`

**Perturbation** (`src/MechaMiner.Simulation/Entities/StoreCapacity.cs`). `HardCapacity` is made to ignore the margin, which is what a hand-edited literal looks like: the derivation `hard == soft + margin` no longer holds for any row with a margin.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities.StoreCapacityTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed SoftTargetHardCapacityAndOverflowBehaviourAreEnforcedPerStore [103 ms]
  Error Message:
  1)   OrdinaryEnemy: the hard capacity must be the soft target plus the documented margin, never a hand-edited number
Assert.That(capacity.HardCapacity, Is.EqualTo(capacity.SoftTarget + capacity.Margin))
  Expected: 730
  But was:  700
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   Elite: the hard capacity must be the soft target plus the documented margin, never a hand-edited number
Assert.That(capacity.HardCapacity, Is.EqualTo(capacity.SoftTarget + capacity.Margin))
  Expected: 15
  But was:  13
   at NUnit.Framework.Assert.Multiple(Action action)
  3)   Pickup: the hard capacity must be the soft target plus the documented margin, never a hand-edited number
Assert.That(capacity.HardCapacity, Is.EqualTo(capacity.SoftTarget + capacity.Margin))
  Expected: 87
  But was:  75
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.StoreCapacityTests.<>c.<SoftTargetHardCapacityAndOverflowBehaviourAreEnforcedPerStore>b__4_0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreCapacityTests.cs:line 51
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.StoreCapacityTests.<>c.<SoftTargetHardCapacityAndOverflowBehaviourAreEnforcedPerStore>b__4_0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreCapacityTests.cs:line 51
   at NUnit.Framework.Assert.Multiple(Action action)
3)    at MechaMiner.Simulation.Tests.Entities.StoreCapacityTests.<>c.<SoftTargetHardCapacityAndOverflowBehaviourAreEnforcedPerStore>b__4_0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/StoreCapacityTests.cs:line 51
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 147 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The domain buffer releases with records unconsumed

**Entries controlled.** `VER-SIM-006-006`

**Perturbation** (`src/MechaMiner.Simulation/Events/DomainEventBuffer.cs`). The consume-before-release predicate is made never true, letting `Release` discard records statistics never consumed.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Events.DomainEventBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed BuffersAreNotReleasedWithUnconsumedRecords [40 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.InvalidOperationException>
  But was:  null
1)    at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in <repo>/tests/shared/Expect.cs:line 31
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 88 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### Event ordering drops the system-phase key

**Entries controlled.** `VER-SIM-006-009`, `VER-SIM-006-010`

**Perturbation** (`src/MechaMiner.Simulation/Events/EventProvenance.cs`). The system-phase key is removed from `EventProvenance.Compare`, so a batch orders by emission sequence alone and a later phase's outcome can sort before an earlier phase's. This is the section `VER-SIM-006-010` is controlled by: its selector, `LossAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs`, is the first failure recorded here. The property test over the reference ordering fails alongside it, seven generated cases deep.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Events"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed LossAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs [51 ms]
  Error Message:
     the fixture must be one where hash enumeration order genuinely differs from the documented order, or the control proves nothing
Assert.That(hashRendering, Is.Not.EqualTo(correctRendering))
  Expected: not equal to "  0  domain tick=2 phase=11 seq=   0 from=entity:1/g1@run3890216961 source=E-FIXTURE kind=entity-defeated#1001 subject=entity:3830/g1@run3890216961 at=(1.5,-0.25) payload=v1 quantity=1 magnitude=0.5 content=none
  3  domain tick=2 phase=10 seq=   3 from=entity:1/g1@run3890216961 source=E-FIXTURE kind=resource-awarded#1002 subject=entity:3830/g1@run3890216961 at=(6,-1) payload=v1 quantity=4 magnitude=2 content=none
  7  domain tick=2 phase=10 seq=   7 from=entity:2/g1@run3890216961 source=E-FIXTURE kind=resource-awarded#1002 subject=entity:3830/g1@run3890216961 at=(12,-2) payload=v1 quantity=8 magnitude=4 content=none
  But was:  "  0  domain tick=2 phase=11 seq=   0 from=entity:1/g1@run3890216961 source=E-FIXTURE kind=entity-defeated#1001 subject=entity:3830/g1@run3890216961 at=(1.5,-0.25) payload=v1 quantity=1 magnitude=0.5 content=none
  3  domain tick=2 phase=10 seq=   3 from=entity:1/g1@run3890216961 source=E-FIXTURE kind=resource-awarded#1002 subject=entity:3830/g1@run3890216961 at=(6,-1) payload=v1 quantity=4 magnitude=2 content=none
  7  domain tick=2 phase=10 seq=   7 from=entity:2/g1@run3890216961 source=E-FIXTURE kind=resource-awarded#1002 subject=entity:3830/g1@run3890216961 at=(12,-2) payload=v1 quantity=8 magnitude=4 content=none
1)    at MechaMiner.Simulation.Tests.Events.EventBufferNegativeControlTests.AssertHashOrderedBufferFailsTheOrderingGate() in <repo>/tests/MechaMiner.Simulation.Tests/Events/EventBufferNegativeControlTests.cs:line 93
  Failed OrderedBatchMatchesTheReferenceOrdering [44 ms]
  Error Message:
  1)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 1 seq=   3 from=entity:4/g1@run3890..."
  But was:  "  0  domain tick=3 phase= 8 seq=   0 from=entity:4/g1@run3890..."
  2)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 1 seq=   3 from=entity:4/g1@run3890..."
  But was:  "  0  domain tick=3 phase= 8 seq=   0 from=entity:4/g1@run3890..."
  3)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 1 seq=   3 from=entity:4/g1@run3890..."
  But was:  "  0  domain tick=3 phase= 8 seq=   0 from=entity:4/g1@run3890..."
  4)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 4 seq=   1 from=entity:3/g1@run3890..."
  But was:  "  0  domain tick=3 phase= 8 seq=   0 from=entity:4/g1@run3890..."
  5)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 4 seq=   1 from=entity:3/g1@run3890..."
  But was:  "  0  domain tick=3 phase= 8 seq=   0 from=entity:4/g1@run3890..."
  6)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 4 seq=   1 from=entity:3/g1@run3890..."
  But was:  "  0  domain tick=3 phase=11 seq=   0 from=entity:2/g1@run3890..."
  7)   the published batch must equal the reference ordering
Assert.That(EventContractAssertions.RenderDomainBatch(published), Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)))
  Expected: "  0  domain tick=3 phase= 4 seq=   1 from=entity:3/g1@run3890..."
```

### The coalescing policy merges every kind

**Entries controlled.** `VER-SIM-006-002`

**Perturbation** (`src/MechaMiner.Simulation/Events/PresentationCoalescingPolicy.cs`). `TryGetMergeRule` is made to report a rule for every kind, so coalescing happens by omission rather than under an explicit named policy.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Events.PresentationEventBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed CoalescingHappensOnlyUnderAnExplicitNamedPolicy [33 ms]
  Error Message:
1)    at MechaMiner.Simulation.Events.PresentationCoalescingPolicy.WithMerge(EventKind kind, String ruleName) in <repo>/src/MechaMiner.Simulation/Events/PresentationCoalescingPolicy.cs:line 90
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 87 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The tick-locality check is removed

**Entries controlled.** `VER-SIM-006-008`

**Perturbation** (`src/MechaMiner.Simulation/Events/DomainEventBuffer.cs`). The tick-locality check is removed from `Append`, so an event belonging to another tick can enter this tick's buffer.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Events.EventOrderingTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed BuffersAreTickLocalAndStartEmpty [66 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.ArgumentException>
  But was:  null
1)    at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in <repo>/tests/shared/Expect.cs:line 31
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 91 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The snapshot gains a public member of a mutable type

**Entries controlled.** `VER-SIM-007-001`, `VER-SIM-007-011`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/PresentationSnapshot.cs`). A public member whose type is `SnapshotEntity[]` is added. It returns a *copy*, so no behavioural test could catch it, which is exactly why the gate is structural: it walks every member's type rather than probing the members someone remembered. This is the section `VER-SIM-007-011` is controlled by.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed SnapshotIsImmutableAndExposesNoMutableStore [31 ms]
  Error Message:
Assert.That(violations, Is.Empty)
  Expected: <empty>
  But was:  < "PresentationSnapshot.LeakedEntities returns MechaMiner.Simulation.Snapshots.SnapshotEntity[]" >
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotContractAssertions.<>c__DisplayClass1_0.<PayloadTypesAreStructurallyImmutable>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotContractAssertions.cs:line 87
   at NUnit.Framework.Assert.Multiple(Action action)
  Failed ImmutabilityAndNoMutationAssertionsFailAgainstDeliberatelyBrokenStubs [13 ms]
  Error Message:
     Assert.That(code, new ThrowsNothingConstraint())
  Expected: No Exception to be thrown
  But was:  <NUnit.Framework.MultipleAssertException:   the real CTR-SIM-003 payload types: these members are mutable or hand out something mutable, so a consumer could write through the payload:
Assert.That(violations, Is.Empty)
  Expected: <empty>
  But was:  < "PresentationSnapshot.LeakedEntities returns MechaMiner.Simulation.Snapshots.SnapshotEntity[]" >
   at NUnit.Framework.Assert.AssertionScope.Dispose()
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Tests.Support.Expect.DoesNotThrow(Action code) in <repo>/tests/shared/Expect.cs:line 37
Failed!  - Failed:     2, Passed:     9, Skipped:     0, Total:    11, Duration: 188 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The snapshot keeps a private mutable array field

**Entries controlled.** `VER-SIM-007-001`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/PresentationSnapshot.cs`). The payload is given a private `SnapshotEntity[]` field, the page storage the real design keeps in `SnapshotDoubleBuffer`. Private, so nothing public changes and no consumer-facing behaviour changes, but the producer could still rewrite it under a held payload. This is the case a public-members-only check would miss.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.PresentationSnapshotTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed SnapshotIsImmutableAndExposesNoMutableStore [39 ms]
  Error Message:
Assert.That(violations, Is.Empty)
  Expected: <empty>
  But was:  < "PresentationSnapshot._pageStorage : MechaMiner.Simulation.Snapshots.SnapshotEntity[] (mutable field type)" >
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotContractAssertions.<>c__DisplayClass1_0.<PayloadTypesAreStructurallyImmutable>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotContractAssertions.cs:line 87
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 164 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### Publication writes the page a consumer already holds

**Entries controlled.** `VER-SIM-007-003`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotDoubleBuffer.cs`). `Publish` writes the front page instead of the back one, collapsing the double buffer to a single page so publishing tick N+1 rewrites the snapshot a consumer holds for tick N.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed PublishingDoesNotMutateAHeldSnapshot [70 ms]
  Error Message:
  1)   the snapshot held for tick 0 must be byte-identical after tick 1 is published
Assert.That(held.Render(), Is.EqualTo(heldRendering))
  Expected string length 360 but was 384. Strings differ at index 29.
  Expected: "snapshot run=1517289473 tick=0 v1 terminal=no player=(0,-0) f..."
  But was:  "snapshot run=1517289473 tick=1 v2 terminal=no player=(0.05,-0..."
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   including its version
Assert.That(held.Version, Is.EqualTo(heldVersion))
  Expected: v1
  But was:  v2
   at NUnit.Framework.Assert.Multiple(Action action)
  3)   and its tick
Assert.That(held.Tick, Is.EqualTo(0L))
  Expected: 0
  But was:  1
   at NUnit.Framework.Assert.Multiple(Action action)
  4)   the next publication must have written the other page, not the held one
Assert.That(fixture.Publisher.Latest, Is.Not.SameAs(held))
  Expected: not same as <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
  But was:  <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
   at NUnit.Framework.Assert.Multiple(Action action)
Assert.That(fixture.Publisher.Previous, Is.SameAs(held))
  Expected: same as <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
  But was:  <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
   at NUnit.Framework.Assert.Multiple(Action action)
Assert.That(fixture.Publisher.Latest!.Version, Is.GreaterThan(held.Version))
  Expected: greater than v2
  But was:  v2
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass0_0.<PublishingDoesNotMutateAHeldSnapshot>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 43
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass0_0.<PublishingDoesNotMutateAHeldSnapshot>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 47
   at NUnit.Framework.Assert.Multiple(Action action)
3)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass0_0.<PublishingDoesNotMutateAHeldSnapshot>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 48
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
   at NUnit.Framework.Assert.Multiple(Action action)
```

### An invalidated tick publishes a partial snapshot

**Entries controlled.** `VER-SIM-007-005`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotPublisher.cs`). `InvalidateTick` writes the staged state into the double buffer before returning its unpublished result, so a tick that failed before commit leaks a partial state into the latest snapshot.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.PresentationSnapshotTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed AnInvalidatedTickPublishesNoSnapshot [84 ms]
  Error Message:
  1)   the version must not advance for a tick that did not commit
Assert.That(fixture.Publisher.LatestVersion, Is.EqualTo(lastGoodVersion))
  Expected: v1
  But was:  v2
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   the latest complete snapshot must still be the last committed one
Assert.That(fixture.Publisher.Latest, Is.SameAs(lastGood))
  Expected: same as <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
  But was:  <MechaMiner.Simulation.Snapshots.PresentationSnapshot>
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Snapshots.PresentationSnapshotTests.<>c__DisplayClass2_0.<AnInvalidatedTickPublishesNoSnapshot>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/PresentationSnapshotTests.cs:line 205
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Snapshots.PresentationSnapshotTests.<>c__DisplayClass2_0.<AnInvalidatedTickPublishesNoSnapshot>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/PresentationSnapshotTests.cs:line 209
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 105 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The interpolation-snap threshold tolerates a third interval

**Entries controlled.** `VER-SIM-007-007`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/InterpolationSnapPolicy.cs`). The tolerated interval count is changed from 2 to 3, the shape a hand-tuned threshold takes. Only `ThresholdMatchesItsDocumentedDerivation` goes red; the tally shows `Failed: 1, Passed: 1` over a filter that includes both of the class's tests, so the spawn/teleport/distance gate passed under this perturbation and is not controlled here. It is controlled by § The snap policy stops evaluating the distance backstop.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.InterpolationSnapPolicyTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed ThresholdMatchesItsDocumentedDerivation [26 ms]
  Error Message:
Assert.That(InterpolationSnapPolicy.ToleratedSnapshotIntervals, Is.EqualTo(2))
  Expected: 2
  But was:  3
1)    at MechaMiner.Simulation.Tests.Snapshots.InterpolationSnapPolicyTests.<>c__DisplayClass2_0.<ThresholdMatchesItsDocumentedDerivation>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/InterpolationSnapPolicyTests.cs:line 142
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 52 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The HUD rounds to nearest instead of truncating

**Entries controlled.** `VER-SIM-007-008`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/HudViewModel.cs`). The documented truncation is replaced by round-to-nearest, so a derived Hull of 0.5 displays as 1 and the HUD overstates the player's survivability. Three cases fail: the midpoint, just under one, and just under a hundred.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.HudViewModelTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed DisplayedWholeValuesEqualAuthoritativeValuesAfterDocumentedRounding [50 ms]
  Error Message:
  1)   and a half value truncates too, so the rule has no midpoint case to disagree about: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 0
  But was:  1
     at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   just under one displays as zero: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 0
  But was:  1
     at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
  3)   just under a hundred displays as ninety-nine: doc 91 § Numeric tolerance requires exact equality for this quantity
Assert.That(actual, Is.EqualTo(expected))
  Expected: 99
  But was:  100
     at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
3)    at MechaMiner.Tests.Support.NumericAssert.AreExactlyEqual(Int64 expected, Int64 actual, String subject) in <repo>/tests/shared/NumericAssert.cs:line 50
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 50 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The snap policy stops evaluating the distance backstop

**Entries controlled.** `VER-SIM-007-006`, `VER-SIM-007-007`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/InterpolationSnapPolicy.cs`). The distance backstop is removed, so an entity displaced further than any legal movement is interpolated. Both of the class's tests go red, which is why this section and not the threshold one is what controls `VER-SIM-007-006`.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.InterpolationSnapPolicyTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed SnapsOnSpawnTeleportReEntryTerminalAndAboveTheDistanceThreshold [50 ms]
  Error Message:
     Assert.That(policy.Evaluate(false, false, false, false, threshold * 1.5), Is.EqualTo(InterpolationSnapReason.DistanceThresholdExceeded))
  Expected: DistanceThresholdExceeded
  But was:  None
1)    at MechaMiner.Simulation.Tests.Snapshots.InterpolationSnapPolicyTests.<>c__DisplayClass1_0.<SnapsOnSpawnTeleportReEntryTerminalAndAboveTheDistanceThreshold>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/InterpolationSnapPolicyTests.cs:line 67
   at NUnit.Framework.Assert.Multiple(Action action)
  Failed ThresholdMatchesItsDocumentedDerivation [3 ms]
  Error Message:
Assert.That(policy.Evaluate(false, false, false, false, oneIntervalBeyond), Is.EqualTo(InterpolationSnapReason.DistanceThresholdExceeded))
  Expected: DistanceThresholdExceeded
  But was:  None
1)    at MechaMiner.Simulation.Tests.Snapshots.InterpolationSnapPolicyTests.<>c__DisplayClass2_0.<ThresholdMatchesItsDocumentedDerivation>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/InterpolationSnapPolicyTests.cs:line 161
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2, Duration: 55 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The packed store allocates during churn

**Entries controlled.** `VER-SIM-003-011`

**Perturbation** (`src/MechaMiner.Simulation/Entities/PackedEntityStore.cs`). `CopyOrderedTo` allocates a scratch array per call, the shape a reflection-driven or LINQ-based store would have. Re-run after the warm-up was lengthened to 1,024 iterations, to confirm the longer warm-up had not made the gate vacuous. Note that the per-iteration assertion reports 64 allocating cycles as well as the 17,920-byte total, which is the distinction the two false positives below turned on.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed ChurnCycleAllocatesNothingAfterWarmUp [99 ms]
  Error Message:
  1)   64 admit-mutate-resolve-order-remove cycles must each allocate nothing; 64 cycle(s) allocated, the largest 280 byte(s)
Assert.That(allocatingCycles, Is.EqualTo(0))
  Expected: 0
  But was:  64
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   and the whole measured window must allocate nothing; measured 17920 byte(s)
Assert.That(allocated, Is.EqualTo(0L))
  Expected: 0
  But was:  17920
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests.<>c__DisplayClass9_0.<ChurnCycleAllocatesNothingAfterWarmUp>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/PackedEntityStoreTests.cs:line 289
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests.<>c__DisplayClass9_0.<ChurnCycleAllocatesNothingAfterWarmUp>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/PackedEntityStoreTests.cs:line 297
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 140 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### Snapshot publication allocates per tick

**Entries controlled.** `VER-SIM-007-009`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotDoubleBuffer.cs`). Publication allocates a fresh page array every tick, the copy-the-world-each-tick shape double buffering exists to avoid. Run against the final form of the gate: 1,024-iteration warm-up, per-iteration assertion, and a consumed publication result.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed PublishingAChurnFreeTickAllocatesNothing [54 ms]
  Error Message:
  1)   64 churn-free publications must each allocate nothing; 64 iteration(s) allocated, the largest 920 byte(s)
Assert.That(allocatingIterations, Is.EqualTo(0))
  Expected: 0
  But was:  64
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   and the whole measured window must allocate nothing; measured 58880 byte(s)
Assert.That(allocated, Is.EqualTo(0L))
  Expected: 0
  But was:  58880
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass3_0.<PublishingAChurnFreeTickAllocatesNothing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 188
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass3_0.<PublishingAChurnFreeTickAllocatesNothing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 197
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 80 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### A new snapshot instance is created per publication

**Entries controlled.** `VER-SIM-007-009`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotDoubleBuffer.cs`). Each publication constructs a fresh `PresentationSnapshot` instead of rewriting the back page. The identity invariant sees 64 instances rather than exactly two.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed PublishingAChurnFreeTickAllocatesNothing [67 ms]
  Error Message:
     64 publications must cycle through exactly 2 snapshot instances; a third instance means a snapshot was allocated per tick rather than a page reused
Assert.That(distinctPages, Has.Count.EqualTo(SnapshotDoubleBuffer.PageCount))
  Expected: property Count equal to 2
  But was:  64
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass2_0.<PublishingAChurnFreeTickAllocatesNothing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 189
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 89 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### A page's backing storage is replaced per publication

**Entries controlled.** `VER-SIM-007-009`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotDoubleBuffer.cs`). Each publication allocates a fresh page array. The array field stays `readonly` and only its element is replaced, so this is exactly the case a readonly-field check alone would miss; the `ReadOnlyMemory` identity comparison is what catches it, and it names the tick each page changed storage at.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed PublishingAChurnFreeTickAllocatesNothing [66 ms]
  Error Message:
     each page must keep the same backing storage for every publication that writes it: page 0 changed backing storage at tick 2; page 1 changed backing storage at tick 3; page 0 changed backing storage at tick 4; page 1 changed backing storage at tick 5; page 0 changed backing storage at tick 6; page 1 changed backing storage at tick 7; page 0 changed backing storage at tick 8; page 1 changed backing storage at tick 9; page 0 changed backing storage at tick 10; page 1 changed backing storage at tick 11; page 0 changed backing storage at tick 12; page 1 changed backing storage at tick 13; page 0 changed backing storage at tick 14; page 1 changed backing storage at tick 15; page 0 changed backing storage at tick 16; page 1 changed backing storage at tick 17; page 0 changed backing storage at tick 18; page 1 changed backing storage at tick 19; page 0 changed backing storage at tick 20; page 1 changed backing storage at tick 21; page 0 changed backing storage at tick 22; page 1 changed backing storage at tick 23; page 0 changed backing storage at tick 24; page 1 changed backing storage at tick 25; page 0 changed backing storage at tick 26; page 1 changed backing storage at tick 27; page 0 changed backing storage at tick 28; page 1 changed backing storage at tick 29; page 0 changed backing storage at tick 30; page 1 changed backing storage at tick 31; page 0 changed backing storage at tick 32; page 1 changed backing storage at tick 33; page 0 changed backing storage at tick 34; page 1 changed backing storage at tick 35; page 0 changed backing storage at tick 36; page 1 changed backing storage at tick 37; page 0 changed backing storage at tick 38; page 1 changed backing storage at tick 39; page 0 changed backing storage at tick 40; page 1 changed backing storage at tick 41; page 0 changed backing storage at tick 42; page 1 changed backing storage at tick 43; page 0 changed backing storage at tick 44; page 1 changed backing storage at tick 45; page 0 changed backing storage at tick 46; page 1 changed backing storage at tick 47; page 0 changed backing storage at tick 48; page 1 changed backing storage at tick 49; page 0 changed backing storage at tick 50; page 1 changed backing storage at tick 51; page 0 changed backing storage at tick 52; page 1 changed backing storage at tick 53; page 0 changed backing storage at tick 54; page 1 changed backing storage at tick 55; page 0 changed backing storage at tick 56; page 1 changed backing storage at tick 57; page 0 changed backing storage at tick 58; page 1 changed backing storage at tick 59; page 0 changed backing storage at tick 60; page 1 changed backing storage at tick 61; page 0 changed backing storage at tick 62; page 1 changed backing storage at tick 63
Assert.That(storageDrift, Is.Empty)
  Expected: <empty>
  But was:  < "page 0 changed backing storage at tick 2", "page 1 changed backing storage at tick 3", "page 0 changed backing storage at tick 4", "page 1 changed backing storage at tick 5", "page 0 changed backing storage at tick 6", "page 1 changed backing storage at tick 7", "page 0 changed backing storage at tick 8", "page 1 changed backing storage at tick 9", "page 0 changed backing storage at tick 10", "page 1 changed backing storage at tick 11"... >
1)    at MechaMiner.Simulation.Tests.Snapshots.SnapshotDoubleBufferTests.<>c__DisplayClass2_0.<PublishingAChurnFreeTickAllocatesNothing>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotDoubleBufferTests.cs:line 197
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 84 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The dense record region becomes replaceable

**Entries controlled.** `VER-SIM-003-011`

**Perturbation** (`src/MechaMiner.Simulation/Entities/PackedEntityStore.cs`). `readonly` is dropped from the dense record array, so an operation could replace it and allocate per churn. Nothing observable changes at runtime, which is the point: the gate is structural and catches the possibility rather than waiting for a sample.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed ChurnCycleAllocatesNothingAfterWarmUp [47 ms]
  Error Message:
  1)   the dense region and the ordering scratch are readonly arrays, so no operation can replace one; a new array field here is new per-churn storage and needs its own registry entry
Assert.That(readonlyArrays, Is.EqualTo(new[] { "_denseIds", "_densePriorityKeys", "_denseStates", "_order", "_slotToDense" }))
  Expected is <System.String[5]>, actual is <System.Collections.Generic.List`1[System.String]> with 4 elements
  Expected string length 12 but was 6. Strings differ at index 1.
  Expected: < "_denseIds", "_densePriorityKeys", "_denseStates", "_order", "_slotToDense" >
  But was:  < "_denseIds", "_densePriorityKeys", "_order", "_slotToDense" >
   at NUnit.Framework.Assert.Multiple(Action action)
  2)   the authored-spawn queue is the only growable storage, because doc 20 § Capacity and overload behavior says a queued authored enemy later enters and the queue must never lose it
Assert.That(growableArrays, Is.EqualTo(new[] { "_queuedPriorityKeys", "_queuedStates" }))
  Expected is <System.String[2]>, actual is <System.Collections.Generic.List`1[System.String]> with 3 elements
  Expected string length 19 but was 12. Strings differ at index 1.
  Expected: < "_queuedPriorityKeys", "_queuedStates" >
  But was:  < "_denseStates", "_queuedPriorityKeys", "_queuedStates" >
   at NUnit.Framework.Assert.Multiple(Action action)
1)    at MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests.<>c__DisplayClass9_0.<AssertDenseStorageFieldsAreReadonlyPlainArrays>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/PackedEntityStoreTests.cs:line 335
   at NUnit.Framework.Assert.Multiple(Action action)
2)    at MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests.<>c__DisplayClass9_0.<AssertDenseStorageFieldsAreReadonlyPlainArrays>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/PackedEntityStoreTests.cs:line 340
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 88 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### The growth counter is never recorded

**Entries controlled.** `VER-SIM-003-011`

**Perturbation** (`src/MechaMiner.Simulation/Entities/PackedEntityStore.cs`). The `RecordStoreGrowth()` call is removed, so the store enlarges the queue arrays without saying so. The churn cycle's zero would still hold, which is why the test also drives a store past its ceiling and requires the counter to rise there; that half is what fails. See § A control that proved nothing, and what it exposed for why that second half exists.

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --nologo --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests"
```

Verbatim failure, trimmed to the assertion lines by the run that recorded it:

```
  Failed ChurnCycleAllocatesNothingAfterWarmUp [60 ms]
  Error Message:
     queueing past the preallocated queue size must enlarge it and be counted, which is what makes the churn cycle's zero meaningful
Assert.That(store.Diagnostics.StoreGrowthCount, Is.GreaterThan(0))
  Expected: greater than 0
  But was:  0
1)    at MechaMiner.Simulation.Tests.Entities.PackedEntityStoreTests.<>c__DisplayClass9_0.<AssertGrowthCounterRisesWhenTheQueueGrows>b__0() in <repo>/tests/MechaMiner.Simulation.Tests/Entities/PackedEntityStoreTests.cs:line 327
   at NUnit.Framework.Assert.Multiple(Action action)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3, Duration: 91 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

### A control that proved nothing, and what it exposed

The first form of the growth-counter control perturbed the queue-growth branch so every
enqueue enlarged the arrays, and expected `VER-SIM-003-011` to fail. **It still passed.**
The churn cycle never reaches hard capacity and therefore never enqueues, so
`StoreGrowthCount == 0` held whether or not the counter was wired up at all. The assertion
was unconditionally true and proved nothing.

This is recorded rather than deleted because it is exactly the failure the negative-control
discipline exists to catch, and it was caught by the control rather than by review. The fix
was to make the counter demonstrably live: `AssertGrowthCounterRisesWhenTheQueueGrows`
drives an authored-enemy store past its ceiling by more than one queue's worth, asserts the
counter rises, and asserts no resident record was displaced. The churn cycle's zero is now
evidence by contrast with a case where the same counter is nonzero.

### Two allocation false positives, and why the gates changed shape

Both are recorded because in both cases the production code was correct the whole time and
the measurement was wrong, and because the fix in each case changed a gate rather than the
code it guards.

**The tiered-JIT promotion.** Between one green run and the next,
`PublishingAChurnFreeTickAllocatesNothing` began failing at 58,880 and then 77,024 bytes
with nothing in production having changed. Instrumenting the measured loop over 20,000
consecutive publications gave:

```
NONZERO count=1 lastTick=40 total=928
```

Exactly one iteration in twenty thousand allocated, 928 bytes, at the 36th iteration after
the warm-up, and zero for the other 19,999. That is the signature of .NET tiered-JIT
promotion, which allocates once per method on the calling thread when a method crosses its
call-count threshold. A four-iteration warm-up put that promotion inside the measured
window, and whether it landed there depended on codegen. Two things changed, both in tests:
the warm-up became 1,024 counted iterations rather than four, with this measurement recorded
in the constant's XML docs as the justification; and both allocation gates now assert
per-iteration deltas as well as the total, because a total-only assertion cannot distinguish
"every iteration allocates a little" from "one iteration allocated once", and those are a
defect and a JIT artifact respectively. The suite was then run three times end to end before
the controls were re-run against the new warm-up, because a longer warm-up must not be
allowed to make a gate vacuous:

```
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
```

**The discarded large-struct return.** After that fix the same gate passed under a plain
`dotnet test` and failed under `./build.sh test-fast`, reporting all 64 iterations
allocating 920 bytes each. The only difference is that the verb passes `--logger trx`.
Reproduced directly:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --nologo -v minimal --logger trx --results-directory <tmpdir>/trx1
```

```
  1)   64 churn-free publications must each allocate nothing; 64 iteration(s) allocated, the largest 920 byte(s)
Assert.That(allocatingIterations, Is.EqualTo(0))
  Expected: 0
  But was:  64
```

Segment instrumentation put all of it inside `SnapshotPublisher.Publish`; instrumentation
*inside* `Publish` attributed it to no statement, which is the signature of a codegen
artifact rather than of a statement that allocates. The cause was the discarded
`TickPublication` return value at the test's call site: a large struct return nobody reads
is materialized differently depending on codegen. Assigning the result and reading two
fields from it removed the 920 bytes entirely under both loggers. That is not a workaround
but the realistic shape, because doc 20 § Tick transaction step 6 makes the publication the
tick result, so no real caller discards it and a benchmark modelling a caller nobody writes
measures a cost nobody pays. Production code was not touched for either false positive.
Stability after both fixes, six consecutive full runs:

```
--- with --logger trx (what ./build.sh test-fast uses) ---
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
--- with the console logger ---
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
Passed!  - Failed:     0, Passed:    54, Skipped:     0, Total:    54
```

The byte-measurement form of both allocation gates was later removed from the fast tier on
the coordinator's decision and replaced with object-identity invariants that need no
measurement. The four controls above named for identity rather than bytes, § A new snapshot
instance is created per publication, § A page's backing storage is replaced per publication,
§ The dense record region becomes replaceable, and § The growth counter is never recorded,
are the ones that confirm the replacements are not vacuous. A real allocation budget in
bytes is deferred to `QUA-005` in the main tier; the two entries state that deferral in
their own summaries, not in a registry field.

## SIM-004: command admission and paused transactions

Twelve controls, one for every entry in `tests/verification/SIM-004.json` that existed when
this batch ran. Two harnesses were used, both stated here because they are what makes the
transcripts checkable rather than anecdotal:

- `perturb.py replace <relpath> <old> <new>` refuses unless the pattern occurs **exactly
  once**, so no perturbation can silently hit two sites; `perturb.py restore <relpath>`
  copies the file back from a byte-exact backup.
- `run-control.sh <FullyQualifiedName-substring>` runs
  `dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj -c Debug
  --no-restore --nologo --filter "FullyQualifiedName~<substring>"` and prints from the first
  `Failed` line onward.

After the last restore, `diff -r` against the backup reported no difference:

```
$ diff -r src/MechaMiner.Simulation/Commands   <session harness>/backup/src-Commands
$ diff -r tests/MechaMiner.Simulation.Tests/Commands <session harness>/backup/test-Commands
ALL RESTORED CLEAN
```

and the gate run was re-executed green:

```
OK [MMT-0000] build debug (MSBuild Debug) succeeded with 0 warnings, 0 errors, and an intact project boundary
OK [MMT-0000] format-check passed all three gates; nothing was written
      tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj: total 129, passed 129, failed 0, skipped 0
OK [MMT-0000] test-fast: total 137, passed 137, failed 0, skipped 0
verify-architecture: PASS
```

The one intentional difference added *after* the controls is an extra `<para>` in
`CommandAdmissionGate`'s remarks recording the `Dictionary.Add` third barrier that the
at-most-once control uncovered. It was in place for that green run.

**Three perturbations had to be retried, and all three retries are recorded below.** A
literal `if (false)` or `if (true)` produces `error CS0162: Unreachable code detected`,
which is a build error here because `TreatWarningsAsErrors=true` with an empty `NoWarn`.
Each was replaced by an always-false comparison on a real value instead. This is worth
knowing before writing another control in this repository: the obvious way to disable a
branch does not compile.

### A command is applied more than once

**Entries controlled.** `VER-SIM-004-001`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`), three
edits, because at-most-once is guarded three independent ways and removing one is not
enough to break it. The idempotency-history check is disabled, the monotonic high-water
mark is weakened to `envelope.Sequence < 0`, and the history write is relaxed from
`Dictionary.Add` to an indexer assignment.

**The first attempt, with only the first two edits, was still caught**, by the
`Dictionary.Add` on the history write. That is a third structural barrier that had not been
named anywhere, and finding it is the reason the control was worth running:

```
  Failed ACommandIsAppliedAtMostOnce [28 ms]
  Error Message:
   System.ArgumentException : An item with the same key has already been added. Key: 0
  ParamName: <null>
  Stack Trace:
     at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior)
   at System.Collections.Generic.Dictionary`2.Add(TKey key, TValue value)
   at MechaMiner.Simulation.Commands.CommandAdmissionGate.TryAdmit(CommandEnvelope& envelope, CommandRejection& rejection) in .../CommandAdmissionGate.cs:line 401
```

That finding was written back into `CommandAdmissionGate`'s type remarks, with the
instruction not to relax the write to an indexer. With all three edits:

```
  Failed ACommandIsAppliedAtMostOnce [51 ms]
  Error Message:
     Assert.That(gate.TryAdmit(first, out CommandRejection immediate), Is.False)
  Expected: False
  But was:  True

  Stack Trace:
     at MechaMiner.Simulation.Tests.Commands.CommandAdmissionGateTests.ACommandIsAppliedAtMostOnce() in .../CommandAdmissionGateTests.cs:line 47
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1
```

### A refusal touches authoritative state

**Entries controlled.** `VER-SIM-004-002`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). The
private `Reject` helper is made to touch authoritative state on the way out:

```
        rejection = CommandRejection.Of(reason, envelope, detail);
+       _admittedInRun++;
        _rejectedInRun++;
```

The shared `CommandContractAssertions.NothingAuthoritativeChanged` fires on the first
refusal in the test. The message carries the whole authoritative rendering before and
after, which is what makes the failure readable rather than a bare inequality:

```
  Failed StaleDuplicateAndInvalidEnvelopesRejectWithoutMutation [83 ms]
  Error Message:
     a stale envelope naming an already-frozen tick: a refused submission returns a typed rejection with no mutation, so the whole authoritative state rendering must be byte-identical before and after it. Before:
gate run=000000005A700004 open=1 lastFrozen=0 highestSeq=1 admitted=2 stateVersion=1 appliedTransactions=0
open-tick
  1 intent(1,0)
admitted run=000000005A700004 tick=0 count=1
  0 intent(0.5,0.5)
history
  0->0
  1->1
transactions

After:
gate run=000000005A700004 open=1 lastFrozen=0 highestSeq=1 admitted=3 stateVersion=1 appliedTransactions=0
...
Assert.That(after, Is.EqualTo(before).Using(StringComparer.Ordinal))
  String lengths are both 233. Strings differ at index 68.
  -------------------------------------------------------------------------------^
```

**Vacuity guard in the test itself.** Before any of the five refusals, the test admits one
envelope and asserts the rendering *changed*:

```csharp
        Assert.That(
            afterAdmission,
            Is.Not.EqualTo(beforeAdmission).Using(StringComparer.Ordinal),
            "an admission must change the authoritative rendering, or the no-mutation comparison below would be vacuous");
```

Without that, "byte-identical after a rejection" would hold even if `RenderAuthoritative()`
returned a constant.

### A sequence regression is admitted

**Entries controlled.** `VER-SIM-004-003`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). The
monotonic check `if (envelope.Sequence <= _highestAdmittedSequence)` becomes
`if (envelope.Sequence < 0)`.

```
  Failed SequenceIsMonotonicAndNeverReordered [52 ms]
  Error Message:
     Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 3, 1.0, 0.0), out CommandRejection regression), Is.False)
  Expected: False
  But was:  True

  Stack Trace:
     at ...CommandAdmissionGateTests.SequenceIsMonotonicAndNeverReordered() in .../CommandAdmissionGateTests.cs:line 209
```

Sequence 3 was never admitted, only 0 and 5 were, so this is a pure regression rather than
a duplicate. That is what makes the two rejection reasons distinguishable.

### The foreign-run fence is checked second

**Entries controlled.** `VER-SIM-004-004`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). The
`AdmissionClosed` window check is moved above the `ForeignRunSession` fence in `TryAdmit`.

```
  Failed AForeignRunIdentityIsRejectedBeforePayloadInspection [50 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   a foreign run identity is refused on identity alone, ahead of the closed window and the unnormalizable payload it also carries
Assert.That(foreignRejection.Reason, Is.EqualTo(CommandRejectionReason.ForeignRunSession))
  Expected: ForeignRunSession
  But was:  AdmissionClosed

  2)   exactly one foreign-run refusal was counted
Assert.That(fixture.Gate.RejectionCount(CommandRejectionReason.ForeignRunSession), Is.EqualTo(1L))
  Expected: 1
  But was:  0
```

**Vacuity guard.** The test submits one payload, `NaN`, in three run and window
combinations and requires three *different* reasons. An implementation that always answered
`ForeignRunSession` would fail the other two, so the fence assertion cannot pass by accident.

### Movement normalizes by the wrong divisor

**Entries controlled.** `VER-SIM-004-005`

**Perturbation** (`src/MechaMiner.Simulation/Commands/MovementIntent.cs`).
`TryNormalize`'s clamp divides by 2 instead of by the magnitude:

```
-            scaledX / scaledMagnitude * MaximumMagnitude,
-            scaledY / scaledMagnitude * MaximumMagnitude);
+            scaledX / 2.0 * MaximumMagnitude,
+            scaledY / 2.0 * MaximumMagnitude);
```

```
  Failed MagnitudeClampsAndDigitalDiagonalsNormalizeToUnitLength [45 ms]
  Error Message:
     digital (1,1) normalizes to unit length: expected 1, actual 0.7071067811865476, difference 0.2928932188134524 exceeds tolerance movement-intent-unit-length (+/-1E-15, one normalizing division, so a few ulps at magnitude 1 (1 ulp = 2.22e-16))
Assert.That(difference, Is.LessThanOrEqualTo(tolerance.Absolute))
  Expected: less than or equal to 1.0000000000000001E-15d
  But was:  0.29289321881345243d
```

This one also measures the gate's tolerance rather than only its direction: the wrong answer
is 0.29 away and the named tolerance is 1e-15, so the tolerance is tight enough to catch a
wrong divisor by 14 orders of magnitude.

### The tick's admitted set is not frozen

**Entries controlled.** `VER-SIM-004-006`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). `BeginTick`
does not clear its working lists:

```
-        _openTickSequences.Clear();
-        _openTickIntents.Clear();
+        // perturbed: the working lists are not cleared
```

```
  Failed AdmittedCommandsAreFrozenForTheTickTheyTarget [62 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   and tick 1 does not hold tick 0's
Assert.That(tickOne.ContainsSequence(0), Is.False)
  Expected: False
  But was:  True

  2)   with its one command and no more
Assert.That(tickOne.Count, Is.EqualTo(1))
  Expected: 1
  But was:  2
```

### Commit mutates before its last validation

**Entries controlled.** `VER-SIM-004-007`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). `Apply`
advances the applied-transaction count before the domain validator has answered:

```
+       _appliedTransactionCount++;
        if (!action.DomainValidator(request))
```

```
  Failed CommitIsAllOrNothingBetweenTicks [93 ms]
  Error Message:
     a request the owning domain component refused: a refused submission returns a typed rejection with no mutation, so the whole authoritative state rendering must be byte-identical before and after it. Before:
gate run=000000005A700004 open=none lastFrozen=1 highestSeq=0 admitted=1 stateVersion=1 appliedTransactions=0
...
After:
gate run=000000005A700004 open=none lastFrozen=1 highestSeq=0 admitted=1 stateVersion=1 appliedTransactions=1
...
  String lengths are both 193. Strings differ at index 108.
```

**Vacuity guard.** `AssertRefusalChangesNothing` also asserts whether
`DomainValidatorInvocations` moved, per case, and the accepted case asserts it moved by
exactly one. Asserting that a counter stays at zero in a scenario that could never raise it
is the vacuity this avoids, and the same test drives one case that does raise it.

### A stale expected state version is accepted

**Entries controlled.** `VER-SIM-004-008`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). **First
attempt**: the expected-state-version check becomes `if (false)`. It did not compile:

```
.../CommandAdmissionGate.cs(647,13): error CS0162: Unreachable code detected [.../MechaMiner.Simulation.csproj]
```

**Retry**: `if (request.ExpectedStateVersion < 0)`, which can never fire because the request
type already rejects a non-positive expected version.

```
  Failed AStaleExpectedStateVersionChangesNothing [71 ms]
  Error Message:
     a request carrying a superseded expected state version: a refused submission returns a typed rejection with no mutation, so the whole authoritative state rendering must be byte-identical before and after it. Before:
gate run=000000005A700004 open=none lastFrozen=0 highestSeq=0 admitted=1 stateVersion=2 appliedTransactions=1
...
transactions
  transaction-result accepted run=000000005A700004 action=A-INSTALL-WEAPON clientSeq=0 version=2 events=1 snapshot=v2

After:
gate run=000000005A700004 open=none lastFrozen=0 highestSeq=0 admitted=1 stateVersion=3 appliedTransactions=2
...
transactions
  transaction-result accepted run=000000005A700004 action=A-INSTALL-WEAPON clientSeq=0 version=2 events=1 snapshot=v2
  transaction-result accepted run=000000005A700004 action=A-INSTALL-WEAPON clientSeq=1 version=3 events=1 snapshot=v3
```

**Vacuity guard.** The test then re-raises the identical action with
`WithExpectedStateVersion(current)` and asserts it is accepted, so the refusal above is
attributable to the version rather than to the request being unusable.

### A replay is refused without observing the applied result

**Entries controlled.** `VER-SIM-004-009`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). Two edits
in `Apply`: the idempotency lookup is disabled and the applied-result write is relaxed from
`Add` to an indexer assignment, for the same third-barrier reason as the at-most-once
control.

```
  Failed ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult [83 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   it is refused as already applied
Assert.That(replay.Reason, Is.EqualTo(TransactionRejectionReason.AlreadyApplied))
  Expected: AlreadyApplied
  But was:  StaleExpectedStateVersion

  2)   but it still reports that the action happened, which is what makes it observable rather than merely refused
Assert.That(replay.WasApplied, Is.True)
  Expected: True
  But was:  False
```

Note what the control also shows. With the idempotency history gone the replay is still
refused, by the expected-state-version check, which is a second barrier against double
application. It is refused with the *wrong reason*, and the entry's real requirement, that
the replay observe the applied result, is what breaks. This observation is what the later
one-control-per-guard pass under § One control per replay guard was built to settle.

**Vacuity guard.** The test then submits a different client command sequence against the
refreshed version and asserts it is accepted, so the deduplication is keyed on the sequence
rather than being a blanket refusal after the first transaction.

### A refused transaction publishes

**Entries controlled.** `VER-SIM-004-010`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). The whole
publication block, `BeginTick`, the staging callback, the event append, `Publish` and
`ReleaseTick`, is moved above the domain-refusal check.

```
  Failed ReplacementSnapshotIsPublishedBeforeResumption [100 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   a refused transaction publishes no replacement snapshot
Assert.That(versionAfterRefusal, Is.EqualTo(versionBeforeTransaction))
  Expected: v1
  But was:  v2

  2)   the snapshot the pause opened over is still readable as the previous page, so presentation holding it is not left without a state across the transaction
Assert.That(fixture.Publisher.Previous!.Render(), Is.EqualTo(preTransactionRendering).Using(StringComparer.Ordinal))
  Expected string length 143 but was 141. Strings differ at index 32.
  Expected: "snapshot run=1517289476 tick=0 v1 terminal=no player=(0.25,-0.25) facing=0 hud v1 hull=100 armor=5 ore=101 hypergold=25 clock=1 extraction=10%\n"
  But was:  "snapshot run=1517289476 tick=0 v2 terminal=no player=(0.5,-0.5) facing=0 hud v2 hull=100 armor=5 ore=102 hypergold=25 clock=1 extraction=10%\n"
```

**Vacuity guard.** The test runs a refused transaction first and asserts the published
version did not move, then an accepted one and asserts it did. Asserting only that the
accepted one published would pass against an implementation that published on every path.

### The reference model disagrees about a rejection reason

**Entries controlled.** `VER-SIM-004-011`

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`). **First
attempt**: `if (alreadyAdmittedTick == envelope.TargetTick.Index)` becomes `if (true)`. It
did not compile, `error CS0162: Unreachable code detected` at the following `return
Reject(...)`. **Retry**: `if (alreadyAdmittedTick >= 0)`, so any history hit is reported as
`Duplicate` regardless of whether the tick matches.

```
  Failed AdmittedSequenceMatchesTheReferenceModel [142 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   submission 7 (run=000000005A700004 tick=1 seq=3 raw=(1,0)): rejection reason
Assert.That(rejection.Reason.ToString(), Is.EqualTo(referenceReason).Using(StringComparer.Ordinal))
  Expected string length 18 but was 9. Strings differ at index 0.
  Expected: "SequenceRegression"
  But was:  "Duplicate"
  -----------^

  2)   submission 6 (run=000000005A700004 tick=1 seq=0 raw=(1,0)): rejection reason
  Expected: "SequenceRegression"
  But was:  "Duplicate"
```

Both the original generated case and the shrunk candidate failed, so the shrinker ran and
reported a minimized input.

### SIM-004's permanent negative control, with its shared assertion weakened

**Entries controlled.** `VER-SIM-004-012`

**Perturbation** (`tests/MechaMiner.Simulation.Tests/Commands/CommandContractAssertions.cs`).
The shared assertion is made unable to report a repeat:

```
-            if (!seen.Add(appliedSequences[index]))
+            if (!seen.Add(appliedSequences[index]) && false)
```

```
  Failed IdempotenceAndAtomicityAssertionsFailAgainstDeliberatelyBrokenStubs [38 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <NUnit.Framework.MultipleAssertException>
  But was:  null

  Stack Trace:
     at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in .../tests/shared/Expect.cs:line 31
   at ...CommandAdmissionNegativeControlTests.AssertHistorylessGateFailsTheAtMostOnceGate() in .../CommandAdmissionNegativeControlTests.cs:line 79
```

This is the control on the control: a weakened shared assertion turns `VER-SIM-004-012`
red, which is why the assertions are shared rather than duplicated inline.

**Vacuity guards inside the control itself.** Each of the three broken-stub sections
asserts, before invoking the shared assertion, that the stub genuinely produced the failure
being controlled, and a fourth section asserts the real gate passes all four shared
assertions, so the control cannot be satisfied by assertions that always throw.

## SIM-005: the random-number contract

Three of this batch's five controls are the ones already recorded above under § Randomness:
the algorithm constants and § Randomness: rejection sampling replaced by modulo reduction.
The two that are not, plus one externally anchored transcript that belongs with them, are
below.

`MECHAMINER_GOLDEN_UPDATE` was never set at any point. After each revert,
`git status --short tests/MechaMiner.Simulation.Tests/Goldens/` reported 0 modified:

```
git diff --stat                                          # (empty)
git status --short tests/MechaMiner.Simulation.Tests/Goldens/   # (empty) - 0 modified
dotnet test ... --filter "FullyQualifiedName~MechaMiner.Simulation.Tests.Random"
Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22
```

No golden file was written, edited, or regenerated at any point.

### The Mix shift, against a published external value

**Entries controlled.** `VER-SIM-005-002`

**Perturbation** (`src/MechaMiner.Simulation/Random/SeedDerivation.cs`). `SecondShift` is
changed from 27 to 26:

```
-    internal const int SecondShift = 27;
+    internal const int SecondShift = 26;
```

**Attribution.** The captured output is a golden diff rather than a `Failed <method>` line,
so this section's credit rests on the command's filter naming the class, which carries
exactly one registry selector. Command:

```
dotnet test ... --filter "FullyQualifiedName~SeedDerivationGoldenVectorTests"
```

```
golden random-seed-derivation.txt does not match. Diff preserved at artifacts/goldens/random-seed-derivation-txt. ...
  18 - 0x0000000000000000	0x0100	0x0000000000000000	0x2D0F28C7E7E786B2	0xC8CDFFD247457D49	0x7B911AB30C19A532	0x49182B4742FDA4D0
  18 + 0x0000000000000000	0x0100	0x0000000000000000	0x322C13E23CEB62AB	0x4847F1CC310463D1	0x946F831AA174028F	0x635E3E374B709104
  19 - 0x0000000000000000	0x0220	0x0000000000000005	0x2D0F28C7E7E786B2	0x6FFE8DBCE920FA29	0xF2479005E7AA098F	0x283FEFFBCDCAEFAB
  19 + 0x0000000000000000	0x0220	0x0000000000000005	0x322C13E23CEB62AB	0x28C14C5EF2F4840E	0x5D59C25DCD70766A	0xC80CAC29FF60438C
```

**The half of this that depends on no file in this repository.** The same perturbation also
fires an assertion anchored on SplitMix64's published output for state 0, which is not a
golden, not a fixture, and not anything this repository could have derived wrongly in a way
the assertion would accept:

```
dotnet test ... --filter "FullyQualifiedName~MixIsSplitMix64AtItsPublishedSeedZeroValue"

  Failed MixIsSplitMix64AtItsPublishedSeedZeroValue [37 ms]
     SplitMix64 next() for state 0
  Expected: 16294208416658607535
  But was:  14870713931807527012
```

`16294208416658607535` is `0xE220A8397B1DCDAF`, the published SplitMix64 output for state 0.
That test is not named by a registry selector, so it is recorded here as supporting evidence
for the derivation entry rather than as a control for an entry of its own.

### The family key is dropped from the derivation

**Entries controlled.** `VER-SIM-005-011`

**Perturbation** (`src/MechaMiner.Simulation/Random/SeedDerivation.cs`, `DeriveD1`). The
family key is masked to zero, so every family shares one stream:

```
-        return Mix(d0 ^ familyKey);
+        return Mix(d0 ^ (ushort)(familyKey & 0x0000));
```

Command:

```
dotnet test ... --filter "FullyQualifiedName~AnExtraDrawInOneFamilyShiftsNoOtherFamily"
```

```
  Failed AnExtraDrawInOneFamilyShiftsNoOtherFamily [45 ms]
   golden random-stream-independence.txt does not match. Diff preserved at artifacts/goldens/random-stream-independence-txt. ...
--- expected (committed golden): 43 line(s)
+++ actual:                      43 line(s)
  20 - 0x0100	0x539E37C12C509BCD	0xF2837B02CF742AC9	0xE506F6059EE85593	0xD340BE7594896373	0x04552DDA	resource-profile selection
  20 + 0x0100	0x5169706E0E622EE8	0xAABB6D65591E325D	0x5576DACAB23C64BB	0x5096BB1A8B393562	0xF904B579	resource-profile selection
  21 - 0x0200	0x728CE2DDBEC363A8	0x0FC8C06453FF4164	0x1F9180C8A7FE82C9	0xEA2E2724952B13A6	0x2E1DAF1A	major topology
  21 + 0x0200	0x5169706E0E622EE8	0xAABB6D65591E325D	0x5576DACAB23C64BB	0x5096BB1A8B393562	0xF904B579	major topology
  22 - 0x0201	0x03679B296608E812	0xB332EA611830092F	0x6665D4C23060125F	0x93B6D01A04D7273C	0xA7BD9DB7	spatial embedding
  22 + 0x0201	0x5169706E0E622EE8	0xAABB6D65591E325D	0x5576DACAB23C64BB	0x5096BB1A8B393562	0xF904B579	spatial embedding
```

Every one of the 23 rows collapses to the same state seed, selector, increment, primed state
and first output. That is the collision signature the ascending-key ordering of the fixture
exists to make visible, and six other tests fail alongside it.

### Recovery re-derives the stream instead of carrying its live state

**Entries controlled.** `VER-SIM-005-013`

**Perturbation** (`src/MechaMiner.Simulation/Random/RandomStreamSet.cs`,
`CaptureRecoveryState`). The record is built from a freshly derived stream rather than from
the stream's current state, which is the plausible "the state is derivable, why store it"
simplification:

```
-                this._streams[index].State,
-                this._streams[index].Increment));
+            Pcg32 rederived = SeedDerivation.CreateStream(this.SchemaVersion, this.MasterSeed, key);
+                rederived.State,
+                rederived.Increment));
```

Command:

```
dotnet test ... --filter "FullyQualifiedName~RandomStreamRecoveryTests"
```

```
  Failed StateAndIncrementRoundTripAndContinueTheSequence [55 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   0x0202/0x0000000000000007: state round-trips exactly
Assert.That(restored.StateOf(key), Is.EqualTo(original.StateOf(key)))
  Expected: 16935879855380838511
  But was:  5575767882096134064

  2)   0x0220/0x0000000000000005: state round-trips exactly
```

The failure is on the resulting state, not on the calls: a stream that had consumed draws is
restored to its primed state, so every stream with a nonzero draw count is caught. The
stream with zero draws is legitimately unaffected, which is why the fixture uses six streams
at six different draw counts.

## The final batch: run-session fences, comparator keys, and one control per guard

Twelve controls run on this branch rather than in a worktree. Each perturbed run was
preceded and followed by a full `./build.sh test-fast` at `skipped 0`. The runner is
`dotnet test` with a `--filter` rather than the verb, because a control has to show one
named test failing for one named reason and the verb reports a tally that would hide which
assertion fired; where the verb's output is shown as well it is shown for the exit class,
not for the attribution.

State after every perturbation in this batch was reverted:

```
$ ./build.sh build
OK [MMT-0000] build debug (MSBuild Debug) succeeded with 0 warnings, 0 errors, and an intact project boundary
verb:    build   exit class 0 (success)   owner FND-002

$ ./build.sh format-check
OK [MMT-0000] format-check passed all three gates; nothing was written
verb:    format-check   exit class 0 (success)   owner FND-002

$ ./build.sh test-fast
OK [MMT-0000] test-fast: total 147, passed 147, failed 0, skipped 0
verb:    test-fast   exit class 0 (success)   owner FND-003

$ bash build/verify-architecture.sh
verify-architecture: PASS
arch exit: 0
```

### The staging run-session fence

**Entries controlled.** `VER-SIM-007-012`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotPublisher.cs`).
`StageVisibleEntity`'s guard condition is replaced by a constant false, so the refusal
becomes unreachable while everything else about the method stays as written:

```
-        if (entity.Id.RunSession != _runSession)
+        if (false)
         {
```

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --filter 'FullyQualifiedName~SnapshotRunSessionFenceTests.AForeignRunSessionEntityCannotBeStaged'
```

```
  Failed AForeignRunSessionEntityCannotBeStaged [39 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.ArgumentException>
  But was:  null

  Stack Trace:
     at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in <repo>/tests/shared/Expect.cs:line 31
   at MechaMiner.Simulation.Tests.Snapshots.SnapshotRunSessionFenceTests.AForeignRunSessionEntityCannotBeStaged() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotRunSessionFenceTests.cs:line 60

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 39 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

**What this establishes.** The foreign identity is minted by a second `EntityIdAllocator` on
run session `0x5A700002`, and the test asserts in the same block that it collides with the
fixture's first live enemy on both storage index and generation. So with the fence removed
the record is accepted, and the only thing separating a leaked reference from a live one was
the guard. The pre-existing `IsPresent` check does not notice, because a well-formed foreign
identity is present.

### The assembled-batch run-session fence

**Entries controlled.** `VER-SIM-006-012`

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotPublisher.cs`). Both batch
checks are removed from `Publish`, leaving the copy into the batch arrays and the existing
ordering invariant untouched:

```
-        RequireDomainBatchIsOwnRunSession(domainCount);
-        RequirePresentationBatchIsOwnRunSession(presentationCount);
+        // negative-control perturbation: batch fence disabled
```

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --filter 'FullyQualifiedName~SnapshotRunSessionFenceTests'
```

```
  Failed AForeignRunSessionEventCannotBePublished [32 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.InvalidOperationException>
  But was:  null

  Stack Trace:
     at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in <repo>/tests/shared/Expect.cs:line 31
   at MechaMiner.Simulation.Tests.Snapshots.SnapshotRunSessionFenceTests.AssertAForeignEmitterFailsTheBatchInvariant() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotRunSessionFenceTests.cs:line 133
   at MechaMiner.Simulation.Tests.Snapshots.SnapshotRunSessionFenceTests.AForeignRunSessionEventCannotBePublished() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotRunSessionFenceTests.cs:line 113

Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 59 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

Note the second line of the tally. `Passed: 1` is `AForeignRunSessionEntityCannotBeStaged`,
whose guard had been restored before this run, so the two guards are shown to be separately
effective rather than jointly.

### The subject half of the batch fence

**Entries controlled.** `VER-SIM-006-012`

The batch guard reads two identity fields per record, on two different types: the emitting
entity on `EventProvenance` and the subject on the record itself. A guard that checked one
and not the other would leave half the gap open, and the control above would not notice, so
the subject half gets its own perturbation.

**Perturbation** (`src/MechaMiner.Simulation/Snapshots/SnapshotPublisher.cs`). Only the
domain-batch subject check is made unreachable. The emitting-entity check, the
presentation-batch checks, and both call sites in `Publish` are left as written:

```
             EntityId subjectId = _domainBatch[index].SubjectId;
-            if (subjectId.RunSession != _runSession)
+            if (false)
```

Command:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --filter 'FullyQualifiedName~SnapshotRunSessionFenceTests'
```

```
  Failed AForeignRunSessionEventCannotBePublished [36 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.InvalidOperationException>
  But was:  null

  Stack Trace:
     at MechaMiner.Tests.Support.Expect.Throws[TException](Action code) in <repo>/tests/shared/Expect.cs:line 31
   at MechaMiner.Simulation.Tests.Snapshots.SnapshotRunSessionFenceTests.AssertAForeignSubjectFailsTheBatchInvariant() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotRunSessionFenceTests.cs:line 172
   at MechaMiner.Simulation.Tests.Snapshots.SnapshotRunSessionFenceTests.AForeignRunSessionEventCannotBePublished() in <repo>/tests/MechaMiner.Simulation.Tests/Snapshots/SnapshotRunSessionFenceTests.cs:line 114

Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2, Duration: 69 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

**What this establishes.** The failing assertion moved from
`AssertAForeignEmitterFailsTheBatchInvariant` at line 133 in the previous control to
`AssertAForeignSubjectFailsTheBatchInvariant` at line 172 here. Each half of the guard is
separately load-bearing, and the test covers both fields rather than one of them twice.

### One control per replay guard

**Entries controlled.** `VER-SIM-004-009`

This is the control that changed a conclusion rather than confirming one, so it is recorded
in full, including the pass results, because a control that passes is the finding here.

Three things in `CommandAdmissionGate.Apply` could refuse a replay, and the earlier control
under § A replay is refused without observing the applied result only showed that some
combination of them did. Guard A is the intended one, the idempotency-history lookup that
returns before any validation. Guard B is the other intended one, the expected-state-version
check, which fires because the first application advanced the version. Guard C was
unplanned: the trailing `_appliedByClientCommandSequence.Add`, which throws
`ArgumentException` on a duplicate key.

Each guard was disabled in turn with the other two also disabled. `_runSession == 0UL` is
used as the always-false condition throughout, because the constructor throws on run session
zero and a literal `false` is `error CS0162` under warnings-as-errors. Command for all
three:

```
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  --filter 'FullyQualifiedName~PausedTransactionTests.ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult'
```

**Guard A alone.**

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 56 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

Independently sufficient, and sufficient for the whole contract. A is first in the method,
so with B and C removed nothing observable changes.

**Guard B alone.**

```
  Failed ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult [81 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   it is refused as already applied
Assert.That(replay.Reason, Is.EqualTo(TransactionRejectionReason.AlreadyApplied))
  Expected: AlreadyApplied
  But was:  StaleExpectedStateVersion

  2)   but it still reports that the action happened, which is what makes it observable rather than merely refused
Assert.That(replay.WasApplied, Is.True)
  Expected: True
  But was:  False

  3)   and it reports the first result: same version, same event, same snapshot
Assert.That(replay.ReportsTheSameApplicationAs(first), Is.True)
  Expected: True
  But was:  False

  4) System.InvalidOperationException : this result applied nothing, so it carries no domain event; check HasAppliedEvent first
     at MechaMiner.Simulation.Commands.PausedTransactionResult.get_AppliedEvent() in <repo>/src/MechaMiner.Simulation/Commands/PausedTransactionResult.cs:line 141

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 82 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

Independently sufficient to refuse, but not to satisfy the contract. The important detail is
what did *not* fail: `StateVersionAdvancedExactlyOnce` and `NothingAuthoritativeChanged` both
run before the block above and both passed, so the replay really was refused and nothing was
applied twice. All four failures are about the refusal not being *observable*, which only A
can make it, because only A has the result in hand.

**Guard C alone.**

```
  Failed ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult [48 ms]
  Error Message:
   System.ArgumentException : An item with the same key has already been added. Key: 7
  ParamName: <null>
  Stack Trace:
     at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior)
   at System.Collections.Generic.Dictionary`2.Add(TKey key, TValue value)
   at MechaMiner.Simulation.Commands.CommandAdmissionGate.Apply(PausedTransactionRequest& request, PauseReasonSet blockingReasons, Action`1 stageReplacementState, SnapshotPublisher publisher, DomainEventBuffer domainEvents, PresentationEventBuffer presentationEvents, PresentationCoalescingPolicy coalescingPolicy) in <repo>/src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs:line 716
   at MechaMiner.Simulation.Tests.Commands.CommandFixture.Apply(PausedTransactionRequest& request) in <repo>/tests/MechaMiner.Simulation.Tests/Commands/CommandFixture.cs:line 229
   at MechaMiner.Simulation.Tests.Commands.PausedTransactionTests.ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult() in <repo>/tests/MechaMiner.Simulation.Tests/Commands/PausedTransactionTests.cs:line 165

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 48 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

**Not a guard, and this is the finding.** `Dictionary.Add` throws, so the method does not
*return* a second success, and that is the whole of what it does. Read the line numbers: the
commit block opens at 674, appends the domain event at 699, publishes, writes the state
version at 707 and the applied count at 708, and only reaches the `Add` at 722. By the time
the throw happens the transaction has been applied a second time. Guard C converts a
completed double-apply into an unhandled exception after the fact. It refuses nothing.

### The commit precondition, and the contrast that proves it is the guard

**Entries controlled.** `VER-SIM-004-009`

The finding above was fixed rather than recorded and left. The mutating tail of `Apply`
moved verbatim into a new private `CommitApplied` with exactly one call site, and a
precondition was added immediately before that call site and after the last validation:
`if (_appliedByClientCommandSequence.ContainsKey(request.ClientCommandSequence)) throw new
InvalidOperationException(...)`. The trailing `Add` is kept as a last-resort invariant
behind the precondition, with a comment saying it is not a defence, and the class remark's
paragraph about it was rewritten to say what the first probe established.

The old control asserted only that *an exception occurred*, which is why C looked like a
guard for so long. This pass used a temporary probe test that snapshots the state version,
the appended-event count, the published snapshot version, the applied count and the whole
authoritative rendering, submits the identical request, and asserts every one is unchanged
*and* that the exception is an `InvalidOperationException` rather than an
`ArgumentException`. It used `Assert.Catch` rather than the repository's `Expect.Throws<T>`
on purpose, because `Expect.Throws` asserts the exact type and aborts, which would discard
the state assertions in exactly the run where they matter. The probe was never committed.
Command:

```
cd <repo>
dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj \
  -c Debug --nologo --filter "FullyQualifiedName~<selector>"
```

**Guard A alone**, unchanged from the first pass:

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 58 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

**Guard B alone**, also unchanged, including what did not fail:

```
  Failed ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult [94 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   it is refused as already applied
Assert.That(replay.Reason, Is.EqualTo(TransactionRejectionReason.AlreadyApplied))
  Expected: AlreadyApplied
  But was:  StaleExpectedStateVersion

  2)   but it still reports that the action happened, which is what makes it observable rather than merely refused
Assert.That(replay.WasApplied, Is.True)
  Expected: True
  But was:  False

  3)   and it reports the first result: same version, same event, same snapshot
Assert.That(replay.ReportsTheSameApplicationAs(first), Is.True)
  Expected: True
  But was:  False

  4) System.InvalidOperationException : this result applied nothing, so it carries no domain event; check HasAppliedEvent first
     at MechaMiner.Simulation.Commands.PausedTransactionResult.get_AppliedEvent() in <repo>/src/MechaMiner.Simulation/Commands/PausedTransactionResult.cs:line 141
```

**Guard P alone**, with both intended guards disabled. This is the picture that changed:

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 59 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

The replay is refused before any state moves. The state version did not advance a second
time, no second domain event was appended, no second snapshot exists, the applied count did
not advance, and the authoritative rendering is byte-identical. The refusal is an
`InvalidOperationException`, so it is P and not C. The same perturbation run against the
real entry's test, which cannot survive an exception where it expects a returned result:

```
  Failed ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult [47 ms]
  Error Message:
   System.InvalidOperationException : client command sequence 7 is already in the applied-transaction history, so the idempotency check above should have answered this submission with the applied result. Refusing here, before the commit has moved anything: doc 20 § Tick transaction ends the run through the safe technical-failure path on an invariant failure before commit, and never publishes a partial state
  Stack Trace:
     at MechaMiner.Simulation.Commands.CommandAdmissionGate.Apply(PausedTransactionRequest& request, PauseReasonSet blockingReasons, Action`1 stageReplacementState, SnapshotPublisher publisher, DomainEventBuffer domainEvents, PresentationEventBuffer presentationEvents, PresentationCoalescingPolicy coalescingPolicy) in <repo>/src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs:line 727
   at MechaMiner.Simulation.Tests.Commands.CommandFixture.Apply(PausedTransactionRequest& request) in <repo>/tests/MechaMiner.Simulation.Tests/Commands/CommandFixture.cs:line 229
   at MechaMiner.Simulation.Tests.Commands.PausedTransactionTests.ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult() in <repo>/tests/MechaMiner.Simulation.Tests/Commands/PausedTransactionTests.cs:line 165

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 48 ms - MechaMiner.Simulation.Tests.dll (net8.0)
```

The deepest production frame is `CommandAdmissionGate.Apply`, not `CommitApplied`. The
commit was never entered.

**Guard C alone**, the contrast. Identical probe, identical command, only P differs:

```
  Failed ProbeTheCommitPreconditionRefusesBeforeAnyStateMoves [83 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   by the commit precondition rather than by the trailing Dictionary.Add
Assert.That(thrown!.GetType().Name, Is.EqualTo("InvalidOperationException"))
  Expected string length 25 but was 17. Strings differ at index 0.
  Expected: "InvalidOperationException"
  But was:  "ArgumentException"
  -----------^

  2)   the state version did not advance a second time
Assert.That(fixture.Gate.TransactionStateVersion, Is.EqualTo(versionAfterFirst))
  Expected: 2
  But was:  3

  3)   no second domain event was published
Assert.That(fixture.DomainEvents.AppendedInRun, Is.EqualTo(appendedAfterFirst))
  Expected: 1
  But was:  2

  4)   no second snapshot exists
Assert.That(fixture.Publisher.LatestVersion, Is.EqualTo(snapshotAfterFirst))
  Expected: v2
  But was:  v3

  5)   the applied-transaction count did not advance
Assert.That(fixture.Gate.AppliedTransactionCount, Is.EqualTo(appliedAfterFirst))
  Expected: 1
  But was:  2

  6)   and the whole authoritative rendering is byte-identical
Assert.That(fixture.Gate.RenderAuthoritative(), Is.EqualTo(renderAfterFirst).Using(StringComparer.Ordinal))
  String lengths are both 343. Strings differ at index 86.
  Expected: "gate run=000000005A700004 open=none lastFrozen=0 highestSeq=0 admitted=1 stateVersion=2 appliedTransactions=1\nopen-tick\n  0 intent(1,0)\nadmitted run=000000005A700004 tick=0 count=1\n  0 intent(1,0)\nhistory\n  0->0\ntransactions\n  transaction-result accepted run=000000005A700004 action=A-INSTALL-WEAPON clientSeq=7 version=2 events=1 snapshot=v2\n"
  But was:  "gate run=000000005A700004 open=none lastFrozen=0 highestSeq=0 admitted=1 stateVersion=3 appliedTransactions=2\nopen-tick\n  0 intent(1,0)\nadmitted run=000000005A700004 tick=0 count=1\n  0 intent(1,0)\nhistory\n  0->0\ntransactions\n  transaction-result accepted run=000000005A700004 action=A-INSTALL-WEAPON clientSeq=7 version=2 events=1 snapshot=v2\n"
```

Six failures, and every one is the finding stated as data. With only the trailing `Add`, the
second application completed: version 2 to 3, one appended event to two, snapshot v2 to v3,
applied count 1 to 2, and the rendering differs at index 86, which is where `stateVersion=`
begins. The `ArgumentException` arrives after all of that. Runs 3 and 4 differ by one
perturbation and produce a refusal-before-anything-moved versus a completed double-apply.

Note also that the `transactions` line in the last rendering still shows `clientSeq=7
version=2`: the indexer wrote the second result over the first and `Render` is showing the
second, identical in text here because the same request produced it. That is the other
reason the `Add` must not be relaxed to an indexer, and it is still not a reason to call it
a guard.

**No new registry entry was added for the precondition**, and the reasoning is recorded
because it is a decision rather than an omission. The new precondition is unreachable
through the public surface, because guard A intercepts every submission whose client command
sequence is in the history and that is precisely the condition P tests. A selector naming it
would be a vacuous gate, which is what doc 91 § Acceptance evidence is against. The
observable property P protects, that a replay moves nothing, is already `VER-SIM-004-009`,
whose test asserts the same five facts the probe asserts. What changed is which mechanism
makes them hold.

### The event comparator loses its tick key

**Entries controlled.** `VER-SIM-006-011`

**Perturbation** (`src/MechaMiner.Simulation/Events/EventProvenance.cs`, `Compare`). The
tick key is made unreachable by a runtime-impossible guard, `left._sequence ==
long.MinValue`, since an emission sequence is always non-negative, so the comparator is
emission sequence only.

Before, as committed:

```csharp
        int byTick = left._tick.CompareTo(right._tick);
        return byTick != 0 ? byTick : left._sequence.CompareTo(right._sequence);
```

Perturbed:

```csharp
        int byTick = left._tick.CompareTo(right._tick);
        return byTick != 0 && left._sequence == long.MinValue
            ? byTick
            : left._sequence.CompareTo(right._sequence);
```

```
$ ./build.sh test-fast
=== stage 2: pure NUnit tiers (no Godot)
FAIL  test-MechaMiner.Simulation.Tests: exit 1 in 4198 ms
      tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj: total 139, passed 138, failed 1, skipped 0
FAILED [MMT-4001] test-fast: total 147, passed 146, failed 1, skipped 0. Failing project(s): tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj
verb:    test-fast   exit class 4 (validation)   owner FND-003
```

```
$ dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --filter "FullyQualifiedName~EventOrderingTests"
  Failed SimultaneousEventsUseDocumentedStableOrdering [26 ms]
  Error Message:
   System.InvalidOperationException : two domain events share tick 7 and emission sequence 4, so their relative order would be decided by collection timing. doc 10 § System phase ordering forbids that; CMP-SIM-003 owns the sequence and must issue each one once.
```

**Why this control exists at all, which is the point of it.** Before the
`retained-multi-tick-records` case existed, the identical perturbation left the whole suite
green at 138 of 138, because every event-ordering fixture and every golden row held the tick
constant. The comparator had a key no input reached. That is why the entry this section
controls is `VER-SIM-006-011` and not the entry that came before it: `VER-SIM-006-003` was
retired precisely because its gate could not see this, and its successor moved the negative
control inside the gate so it runs on every invocation rather than existing only as a
transcript of a perturbation someone once applied. This transcript is the record of the hole,
not a substitute for the in-gate control.

### EntityId.Compare loses its run-session key

**Entries controlled.** `VER-SIM-003-010`, `VER-SIM-003-012`

**Perturbation** (`src/MechaMiner.Simulation/Entities/EntityId.cs`, `Compare`). The leading
run-session key is made unreachable by a runtime-impossible guard, `left._index ==
int.MinValue`, since a storage index is never below `NoEntityIndex` of -1, so the comparator
is storage index then generation.

Before, as committed:

```csharp
        int bySession = left._runSession.CompareTo(right._runSession);
        if (bySession != 0)
        {
            return bySession;
        }
```

Perturbed:

```csharp
        int bySession = left._runSession.CompareTo(right._runSession);
        if (bySession != 0 && left._index == int.MinValue)
        {
            return bySession;
        }
```

```
$ ./build.sh test-fast
=== stage 2: pure NUnit tiers (no Godot)
FAIL  test-MechaMiner.Simulation.Tests: exit 1 in 4292 ms
      tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj: total 139, passed 137, failed 2, skipped 0
FAILED [MMT-4001] test-fast: total 147, passed 145, failed 2, skipped 0. Failing project(s): tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj
verb:    test-fast   exit class 4 (validation)   owner FND-003
```

```
$ dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --filter "FullyQualifiedName~Entities"
  Failed IdentityAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs [60 ms]
  Error Message:
     retained-cross-session-records: these are the degradations the case must notice, and the ones it must not. A case that notices fewer is vacuous for the component it was added for; a case that notices more means the fixture shape changed and the golden's header no longer describes it. Blind here:
  Failed IterationOrderIsPriorityKeysThenFullEntityId [14 ms]
  Error Message:
     retained-cross-session-records: the documented comparison must be a total order over this set, so a reversed arrival order produces the identical result
```

Both failures come from the new `retained-cross-session-records` case. The ordering test
fails because the two records that differ only on run session tie, so a reversed arrival
order no longer renders identically. The negative control fails because the degraded
comparators can no longer be told apart from the production one for that case. Before this
case existed the identical perturbation left the suite fully green, because every ordering
case held one run session. This is the same shape of defect as the event comparator's tick
key, found in the same pass.

### The mid-commit invalidation path is disabled

**Entries controlled.** `VER-SIM-004-013`

This is the same perturbation as § Commands: the two mid-commit recovery controls, control
A, recorded here with the verb's exit class and the full assertion list because this is the
run that was made against the reviewer's report.

**Perturbation** (`src/MechaMiner.Simulation/Commands/CommandAdmissionGate.cs`, `Apply`).
The call to `AbandonPartialCommit` is made unreachable by `_runSession == 0UL`, so the
commit still rethrows but recovers nothing, which is exactly the state the reviewer found.

Before, as committed:

```csharp
        catch (Exception failure)
        {
            AbandonPartialCommit(
                request,
                failure,
                ...);
            throw;
        }
```

Perturbed:

```csharp
        catch (Exception failure)
        {
            if (_runSession == 0UL)
            {
                AbandonPartialCommit(
                    request,
                    failure,
                    ...);
            }

            throw;
        }
```

```
$ ./build.sh test-fast
FAILED [MMT-4001] test-fast: total 147, passed 146, failed 1, skipped 0. Failing project(s): tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj
verb:    test-fast   exit class 4 (validation)   owner FND-003
```

```
$ dotnet test tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj --filter "FullyQualifiedName~AFailedCommitInvalidatesTheTickInsteadOfWedgingTheRun"
  Failed AFailedCommitInvalidatesTheTickInsteadOfWedgingTheRun [78 ms]
  Error Message:
   Multiple failures or warnings in test:
  1)   the tick the commit opened must not be left open, or no later tick and no retry can run
Assert.That(fixture.Publisher.IsTickOpen, Is.False)
  Expected: False
  But was:  True

  2)   it must be invalidated and counted, which is how the run ends through the technical-failure path rather than wedging
Assert.That(fixture.Publisher.InvalidatedTickCount, Is.EqualTo(invalidatedBefore + 1))
  Expected: 1
  But was:  0

  3)   and the gate must record the abandoned commit as its own diagnostic
Assert.That(fixture.Gate.AbandonedCommitCount, Is.EqualTo(1L))
  Expected: 1
  But was:  0

  4)   observable to CMP-OBS-001, not only to this test
Assert.That(fixture.Gate.Render(), Does.Contain("abandonedCommits=1"))
```

The four assertions that fail are precisely the ones whose absence let the blocker through:
the publisher's tick is left open and `InvalidatedTickCount` stays at zero. The assertions
about nothing being published and no version advancing still pass under the perturbation,
which is why a test asserting only those would not have caught it.

## Attributions that did not survive the rule

Crediting a section with an entry only when the entry's own test is shown failing moved
seven attributions. Every one is recorded here rather than silently corrected, because the
pattern is worth more than any of the individual fixes: **a perturbation's intent is not the
same fact as a gate's redness**, and the original records were written from the intent.

**Two pointers already in the registry are kept but relabelled.** `VER-SIM-005-016` names
§ Randomness: the algorithm constants and `VER-SIM-006-010` names § Events: the domain buffer
drops at its ceiling. Neither section shows the entry's own test failing. In the first case
the tests shown red are four other SIM-005 entries' and in the second the tally reads
`Failed: 1, Passed: 10` over a filter covering all of `...Tests.Events`, which positively
establishes that `VER-SIM-006-010`'s test passed there.

Both pointers were nonetheless placed deliberately and reasoned for in their registry files'
`notes`, on a basis that holds: each of those entries is a stub-based gate, and the section it
names applies **the same perturbation to production code** that the entry applies to a
test-side subject. A reader following the pointer finds the real one-bit change, or the real
ceiling drop, rather than nothing. That is a useful pointer and a different claim from "this
section shows this gate failing", so it is labelled as the cross-reference it is in
§ Which entry each section controls rather than removed. What was wrong was the absence of the
distinction, not the pointer.

`VER-SIM-006-010` also gains the section that does show its own test failing,
§ Event ordering drops the system-phase key, where its selector is the first failure recorded.
`VER-SIM-005-016` gains nothing, because no section anywhere shows its test failing: the
one-bit mutations it carries are applied to the test-side independent reference, so the battery
lives inside a green suite with no gate disabled, and nobody ran the repair-the-stub control
that the other six packages' permanent negative-control entries have. Six of seven, not seven
of seven, and § Which entry each section controls is where that shows.

**Five section-level gate claims were narrowed.** In each case the original record named a
gate the transcript does not show failing. Two of the five are provable over-claims rather
than merely unproven ones, because the tally line accounts for every test under the filter:

| Section | Originally claimed | Actually shown | Why it moved |
| --- | --- | --- | --- |
| § The store resolves a stale generation | also `VER-SIM-003-003` | `VER-SIM-003-002`, `-005`, `-012` | The output was trimmed to the assertion lines with no tally, so this is unproven rather than disproven. Not credited either way |
| § Events: the domain buffer drops at its ceiling, and the earlier run of the same perturbation | also `VER-SIM-006-009` and `VER-SIM-006-010` | `VER-SIM-006-001` | **Provable.** `Failed: 1, Passed: 10` over a filter covering all of `...Tests.Events` |
| § Event ordering drops the system-phase key | also the entry since retired as `VER-SIM-006-003` | `VER-SIM-006-009`, `VER-SIM-006-010` | `SimultaneousEventsUseDocumentedStableOrdering` does not appear in the failure text. This perturbation removed the phase key; that test's own hole was the *tick* key, which is § The event comparator loses its tick key |
| § The snapshot keeps a private mutable array field | also `VER-SIM-007-011` | `VER-SIM-007-001` | The filter was class-scoped to `PresentationSnapshotTests` and the negative-control test is in another class, so it could not have run. It is credited to § The snapshot gains a public member of a mutable type, whose broader filter did run it |
| § The interpolation-snap threshold tolerates a third interval | also `VER-SIM-007-006` | `VER-SIM-007-007` | **Provable.** `Failed: 1, Passed: 1` over a filter covering both of the class's tests, so the spawn, teleport and distance gate passed under this perturbation |

**Two attributions were widened**, in the other direction, because a transcript showed a test
failing that its record had not claimed. The stale-reference control, § Entities: the
stale-reference gate refuses without counting, also fails `VER-SIM-003-009`; and § The host
blocks on one reason instead of any fails both `VER-SIM-002-002` and `VER-SIM-002-003` rather
than the one it named.

## Which entry each section controls

Every entry in the seven `SIM-00*.json` files appears in exactly one row. An entry whose
`fixtures` name this file names the section or sections in its row and no others. An entry
recorded as having no control does **not** name this file, which is the point of the table: a
pointer to a transcript that does not mention the entry is worse than no pointer.

Two kinds of qualified credit are marked inline. *Same perturbation against production* means
the section applies to production code the perturbation the entry's gate applies to a stub, and
does not show the entry's own test failing. *By filter* means the captured output is a golden
diff rather than a `Failed <method>` line, and the credit rests on the command naming that test.

| Entry | Status | Section of this file, or the reason there is none |
| --- | --- | --- |
| `VER-SIM-001-001` | implemented | § The tick rate is changed to 50 Hz; § The rate is declared a second time; § A tick-rate member becomes settable |
| `VER-SIM-001-002` | implemented | § The tick count becomes fractional; § The retained fraction is exposed; § The accumulator ceilings instead of flooring |
| `VER-SIM-001-003` | implemented | § Long-run drift by subtraction |
| `VER-SIM-001-004` | implemented | § Seconds accumulated instead of divided |
| `VER-SIM-001-005` | implemented | § The catch-up bound is removed |
| `VER-SIM-001-006` | implemented | § The bound loses its headroom tick |
| `VER-SIM-001-007` | implemented | § One diagnostic per discarded tick instead of one per occurrence |
| `VER-SIM-001-008` | implemented | § SIM-001's permanent negative control, with its stub repaired |
| `VER-SIM-001-009` | implemented | § Resume catches up instead of discarding |
| `VER-SIM-001-010` | implemented | § A tick is invoked twice |
| `VER-SIM-001-011` | implemented | § Run time accumulates instead of dividing the index |
| `VER-SIM-001-012` | implemented | § A 35:00 event is admitted before the boundary is evaluated |
| `VER-SIM-001-013` | registered | Status is `registered`. Its selector is `./build.sh benchmark PERF-04`, a command that no runner in this repository can execute yet, so there is nothing to perturb and nothing to record. Its evidence will be an SCH-OBS-002 performance report on target hardware. |
| `VER-SIM-001-014` | implemented | No control transcript. The gate scans the compiled assembly for forbidden determinism APIs, so a perturbation would mean adding a forbidden call to production code. Its five injected evasion routes, including the one a metadata scan cannot see, are recorded in the test's own remarks, which is the artifact a reader should open. |
| `VER-SIM-002-001` | implemented | § An eighth pause reason is defined |
| `VER-SIM-002-002` | implemented | § The host blocks on one reason instead of any |
| `VER-SIM-002-003` | implemented | § The host blocks on one reason instead of any |
| `VER-SIM-002-004` | implemented | § Controls for the second review pass; § The reason set mutates in place |
| `VER-SIM-002-005` | implemented | § Focus recovery clears every reason |
| `VER-SIM-002-006` | implemented | No control transcript. The neighbouring hook control, § Resume catches up instead of discarding, perturbs the same discard mechanism but is credited to `VER-SIM-001-009`, whose test is what went red. The suspension-only half was never perturbed on its own. |
| `VER-SIM-002-007` | implemented | § The UI clock is frozen while paused |
| `VER-SIM-002-008` | implemented | § The terminal transition can be cleared |
| `VER-SIM-002-009` | implemented | § A pause banks wall time into the accumulator |
| `VER-SIM-002-010` | implemented | § SIM-002's permanent negative control, with its stub repaired |
| `VER-SIM-003-001` | implemented | No control transcript. Generation increment on slot reuse was never perturbed on its own; the exhaustion path was, under § An exhausted generation wraps instead of retiring the slot. |
| `VER-SIM-003-002` | implemented | § Entities: the stale-reference gate refuses without counting; § The store resolves a stale generation |
| `VER-SIM-003-003` | implemented | No control transcript. § The store resolves a stale generation drops the identity check that this entry's cross-run case also depends on, and the original record of that run named this entry as being under control, but the captured failure text does not show this entry's test failing. Recorded as uncovered rather than credited on the strength of the perturbation's intent. |
| `VER-SIM-003-004` | implemented | No control transcript. The reserved player identity was never perturbed. |
| `VER-SIM-003-005` | implemented | § The store resolves a stale generation |
| `VER-SIM-003-006` | implemented | § An exhausted generation wraps instead of retiring the slot |
| `VER-SIM-003-007` | implemented | No control transcript. One store per authoritative population category is a structural enumeration; it was never perturbed. |
| `VER-SIM-003-008` | implemented | § The hard capacity ignores the margin |
| `VER-SIM-003-009` | implemented | § Entities: the stale-reference gate refuses without counting |
| `VER-SIM-003-010` | implemented | § The store iterates in storage order; § EntityId.Compare loses its run-session key |
| `VER-SIM-003-011` | implemented | § The packed store allocates during churn; § The dense record region becomes replaceable; § The growth counter is never recorded |
| `VER-SIM-003-012` | implemented | § Entities: the stale-reference gate refuses without counting; § The store resolves a stale generation; § The store iterates in storage order; § EntityId.Compare loses its run-session key |
| `VER-SIM-004-001` | implemented | § A command is applied more than once |
| `VER-SIM-004-002` | implemented | § A refusal touches authoritative state |
| `VER-SIM-004-003` | implemented | § A sequence regression is admitted |
| `VER-SIM-004-004` | implemented | § The foreign-run fence is checked second |
| `VER-SIM-004-005` | implemented | § Movement normalizes by the wrong divisor |
| `VER-SIM-004-006` | implemented | § The tick's admitted set is not frozen |
| `VER-SIM-004-007` | implemented | § Commit mutates before its last validation |
| `VER-SIM-004-008` | implemented | § A stale expected state version is accepted |
| `VER-SIM-004-009` | implemented | § A replay is refused without observing the applied result; § One control per replay guard; § The commit precondition, and the contrast that proves it is the guard |
| `VER-SIM-004-010` | implemented | § A refused transaction publishes |
| `VER-SIM-004-011` | implemented | § The reference model disagrees about a rejection reason |
| `VER-SIM-004-012` | implemented | § SIM-004's permanent negative control, with its shared assertion weakened |
| `VER-SIM-004-013` | implemented | § Commands: the two mid-commit recovery controls; § The mid-commit invalidation path is disabled |
| `VER-SIM-005-001` | implemented | § Randomness: the algorithm constants *(by filter)* |
| `VER-SIM-005-002` | implemented | § The Mix shift, against a published external value *(by filter)* |
| `VER-SIM-005-003` | implemented | No control transcript. The initialization golden was never perturbed on its own. § Randomness: the algorithm constants perturbs the constants that feed it and shows `random-stream-initialization.txt` diverging, but under a filter that names a different test, so the credit goes there and not here. |
| `VER-SIM-005-004` | implemented | § Randomness: the algorithm constants; § Randomness: rejection sampling replaced by modulo reduction |
| `VER-SIM-005-005` | implemented | § Randomness: rejection sampling replaced by modulo reduction |
| `VER-SIM-005-006` | implemented | No control transcript. The 53-bit unit conversion was never perturbed on its own. |
| `VER-SIM-005-007` | implemented | No control transcript. Integer-ratio chance was never perturbed. |
| `VER-SIM-005-008` | implemented | No control transcript. The empty and singleton no-draw rule was never perturbed. |
| `VER-SIM-005-009` | implemented | § Randomness: the algorithm constants |
| `VER-SIM-005-010` | implemented | § Randomness: the algorithm constants |
| `VER-SIM-005-011` | implemented | § Controls for the second review pass; § The family key is dropped from the derivation |
| `VER-SIM-005-012` | implemented | No control transcript. Instance-key separation within a family was never perturbed on its own; § The family key is dropped from the derivation perturbs the family key, which is the other axis. |
| `VER-SIM-005-013` | implemented | § Controls for the second review pass; § Recovery re-derives the stream instead of carrying its live state |
| `VER-SIM-005-014` | implemented | No control transcript. Schema-version rejection on recovery was never perturbed. |
| `VER-SIM-005-015` | implemented | No control transcript. Source injectability is a structural assertion about production having no algorithm choice; it was never perturbed. |
| `VER-SIM-005-016` | implemented | § Randomness: the algorithm constants *(same perturbation against production)* |
| `VER-SIM-006-001` | implemented | § Events: the domain buffer drops at its ceiling |
| `VER-SIM-006-002` | implemented | § The coalescing policy merges every kind |
| `VER-SIM-006-003` | retired | retired, and deliberately not pointed at this file. Its selector is now `VER-SIM-006-011`'s, and § The event comparator loses its tick key is the transcript of why this entry was retired rather than a control for what it claimed |
| `VER-SIM-006-004` | implemented | No control transcript. Provenance completeness was never perturbed. |
| `VER-SIM-006-005` | implemented | No control transcript for this entry's own test. § The event comparator loses its tick key perturbs the comparator that this entry asserts forms a total order, and `TickAndSequenceFormATotalOrder` did not fail under it. |
| `VER-SIM-006-006` | implemented | § The domain buffer releases with records unconsumed |
| `VER-SIM-006-007` | implemented | No control transcript. Discarding the presentation batch was never perturbed. |
| `VER-SIM-006-008` | implemented | § The tick-locality check is removed |
| `VER-SIM-006-009` | implemented | § Event ordering drops the system-phase key |
| `VER-SIM-006-010` | implemented | § Events: the domain buffer drops at its ceiling *(same perturbation against production)*; § Event ordering drops the system-phase key |
| `VER-SIM-006-011` | implemented | § The event comparator loses its tick key |
| `VER-SIM-006-012` | implemented | § The assembled-batch run-session fence; § The subject half of the batch fence |
| `VER-SIM-007-001` | implemented | § The snapshot gains a public member of a mutable type; § The snapshot keeps a private mutable array field |
| `VER-SIM-007-002` | implemented | No control transcript. Run, tick and version identity was never perturbed on its own. |
| `VER-SIM-007-003` | implemented | § Publication writes the page a consumer already holds |
| `VER-SIM-007-004` | implemented | No control transcript. Reconstruction from a snapshot was never perturbed. |
| `VER-SIM-007-005` | implemented | § An invalidated tick publishes a partial snapshot |
| `VER-SIM-007-006` | implemented | § The snap policy stops evaluating the distance backstop |
| `VER-SIM-007-007` | implemented | § The interpolation-snap threshold tolerates a third interval; § The snap policy stops evaluating the distance backstop |
| `VER-SIM-007-008` | implemented | § The HUD rounds to nearest instead of truncating |
| `VER-SIM-007-009` | implemented | § Snapshot publication allocates per tick; § A new snapshot instance is created per publication; § A page's backing storage is replaced per publication |
| `VER-SIM-007-010` | implemented | No control transcript. Reconstruction across skipped snapshots was never perturbed. |
| `VER-SIM-007-011` | implemented | § The snapshot gains a public member of a mutable type |
| `VER-SIM-007-012` | implemented | § The staging run-session fence |

68 entries name this file. 20 are recorded above as having no control transcript
anywhere and name it nowhere. One is retired and points at nothing. That accounts for all
89 entries across the seven files.

The 20 without a control are not a backlog with a plan attached, and this file does not
pretend otherwise. They are what the perturbation work did not reach. Seven of them are in
SIM-005, the package whose gates lean hardest on committed goldens and an independent
reference implementation, so the shape of its evidence is agreement with an external oracle
rather than a demonstration that a gate can fail. That is a different kind of assurance, not a
substitute for this one, and doc 91 § Acceptance evidence asks for this one.

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
