# Simulation golden vectors

This directory holds the canonical golden fixtures for
`MechaMiner.Simulation`. Three work-package groups share it:

| Group | Files | Work package |
| --- | --- | --- |
| Authoritative random-number contract | `random-*.txt` | SIM-005 |
| Fixed-step time and runtime | `time-*.txt`, `runtime-*.txt` | SIM-001, SIM-002 |
| Stable ordering | `entities-store-ordering.txt`, `events-simultaneous-ordering.txt` | SIM-003, SIM-006 |

Every file here is canonical, ordered, reviewable text per doc 91 § Determinism
and fixture policy: LF endings, no trailing whitespace, exactly one final
newline, and a `#` header naming the authority and the derivation. No file here
was produced by the C# implementation it checks, and § "A mismatch is
investigated, never regenerated" at the end of this document governs all of
them.

**How much "not produced by the implementation" means is not the same for every
file, and the difference is worth knowing before trusting one.** The six
`random-*.txt` files have *commit-order* independence: they were committed at
`a5a6929`, before any file under `src/MechaMiner.Simulation/Random/` existed, so
the implementation could not have shaped them even accidentally. That is the
strongest form of the property available and it is checkable from the history.
The other five files do not have it: they were committed alongside or after the
code they check. Their independence rests instead on a separate re-derivation
performed afterwards, restating the governing document sections and rebuilding
the expected values from that restatement rather than from observed output. That
is a real check and it caught real defects, but it is a weaker guarantee than
commit order, because the same person read the same document twice. Each group's
section below says which of the two it has.

## Files

| File | Pins | Authority |
| --- | --- | --- |
| `random-seed-derivation.txt` | four-step derivation; `d0`, `d1`, `stateSeed`, `selector` for 9 triples including master seed 0, master seed all-ones, instance key 0, and nonzero instance keys | doc 20:53 |
| `random-stream-initialization.txt` | increment and primed state plus the first 16 outputs for 5 streams; two of them differ only in instance key | doc 20:55 |
| `random-bounded-conversion.txt` | results **and draws consumed** for 10 bounds including 1, non-powers-of-two, and `0xC0000000` which rejects one draw in four | doc 20:86 |
| `random-unit-double-conversion.txt` | conversion, as mantissa, IEEE 754 bits, and decimal | doc 20:87 |
| `random-stream-independence.txt` | first output of all 23 registered families in canonical family-key order | doc 20:57-81 |
| `random-degenerate-selection.txt` | zero-draw rule with the stream state shown unchanged, plus multi-candidate controls that do advance | doc 20:89 |
| `time-tick-index-derived-seconds.txt` | derived seconds for a spread of tick indices as IEEE 754 bit patterns, each obtained by dividing the tick index once by the exact rational rate 60/1 rather than by accumulating a per-tick delta; includes tick 126000, whose derived seconds must be exactly 2100 | doc 10 § Clock domains; doc 20 § Numeric and unit conventions |
| `time-final-boundary-ordering.txt` | the 35:00 terminal boundary as tick 126000: 125999 is the last tick executed, 126000 never runs, and extraction is evaluated before any event scheduled at or after the boundary — an event at or after 126000 being refused at every point in the run, not only once the boundary has been reached | doc 20 § Boundary and tie ordering; doc 10 § System phase ordering, phase 2 |
| `runtime-pause-boundary-tick-sequence.txt` | a pause consumes no gameplay time: two runs fed the same 24 frame deltas, one of them blocked by `GeneralPause` for 11/128 s part-way through, render one identical tick sequence, because blocked steps produce no batch at all and blocked wall time is never banked into a later step | doc 10 § Pause contract; doc 20 § Verification |
| `entities-store-ordering.txt` | the entity comparator as authored priority key ascending, then storage index, then generation, in three cases that each leave exactly one of those three components as the sole discriminator: a live store at tied priority keys, retained records sharing one recycled slot at three generations, and retained records at one shared generation. Storage indices are rendered partition-relative and the partition base is computed from the capacity table by the fixture, never written down as a literal | doc 20 § Entity identity; doc 20 § Authoritative population categories |
| `events-simultaneous-ordering.txt` | the event comparator as tick, then explicit emission sequence, and nothing further: eight events across four system phases, appended in a scrambled order and again reversed, produce one identical batch. Records the two invariants that replaced the removed phase and entity-ID keys, namely that a sequence is unique within a tick and that phase does not decrease as the sequence rises | doc 10 § System phase ordering; doc 20 § Domain and presentation events |

## Fixed-step time and runtime vectors (SIM-001, SIM-002)

`docs/technical/10-runtime-architecture.md` § Clock domains, § Pause contract,
and § System phase ordering, together with
`docs/technical/20-simulation-core.md` § Boundary and tie ordering and § Numeric
and unit conventions, are the normative sources for these three files. Each
file's own header quotes the sentences it depends on.

They were derived from those sections by independent Python references — a
fixed-step accumulator, and an exact rational tick-index-to-seconds division —
rather than by the C# under test, for the same reason the random vectors were: a
golden generated from the implementation proves only that the implementation
agrees with itself.

Two conventions in these files are worth knowing before reading them:

- **Seconds are IEEE 754 binary64 bit patterns, not decimal renderings.** A bit
  pattern has no formatting ambiguity, so a mismatch is always a disagreement
  about the value and never about how it was printed.
- **Frame deltas are exact binary fractions** (multiples of 1/128 s), so a tick
  sequence is a property of the accumulator rather than of decimal rounding in
  the fixture. The blocked interval in
  `runtime-pause-boundary-tick-sequence.txt` is deliberately not a whole number
  of ticks (11/128 s = 5.15625 ticks).

These two files have re-derivation independence, not commit-order independence:
they landed with the time and runtime implementation, and the Python references
were written from the document sections rather than from the C# output.

## Stable ordering vectors (SIM-003, SIM-006)

`docs/technical/20-simulation-core.md` § Entity identity and § Authoritative
population categories are the normative sources for
`entities-store-ordering.txt`. `docs/technical/10-runtime-architecture.md`
§ System phase ordering, together with doc 20 § Domain and presentation events
for the event-sequence contract, are the normative sources for
`events-simultaneous-ordering.txt`. Each file's own header quotes the sentences
it depends on and names the comparator it pins.

Both were derived by an independent Python reference rather than by the C# under
test, for the same reason the other groups were, and both were then re-derived a
second time from the documents during the hardening pass. Neither has
commit-order independence: both landed with the code they check, so the
re-derivation is what their independence rests on.

Two things about these two files specifically:

- **An ordering golden's weak point is its inputs, not its expected values.** A
  case whose records cannot distinguish two comparators proves nothing about the
  difference between them, and the file will still match. So each case in
  `entities-store-ordering.txt` names the one component it leaves as the sole
  discriminator, and `EntityStoreNegativeControlTests` asserts, per case, exactly
  which degraded comparators the case notices and which it is blind to. The
  blindness is asserted as well as the detection, because the fact that a live
  store cannot reach the generation key is the reason the two retained-record
  cases exist at all.
- **`events-simultaneous-ordering.txt` was rebuilt during the hardening pass, not
  merely extended.** Its original inputs paired a high system phase with a low
  emission sequence in order to make the sort observably not a no-op, which under
  the contract is a state the system cannot produce: the sequence is issued at
  emission and emission happens in phase order. The old file therefore pinned an
  impossible input. The current values reflect the two-key `(tick, sequence)`
  comparator, not the earlier three-key phase/sequence/entity-ID one, and the
  file reaches that same non-no-op property by scrambling the append order
  instead.

## Authoritative random-number vectors (SIM-005)

Work package SIM-005.

### Authority

`docs/technical/20-simulation-core.md` § Authoritative random-number contract
(lines 49-91) is the normative source for every number in the `random-*.txt`
files. **Random schema version: 1** (doc 20:53).

Doc 20:55 is explicit about the consequence of changing any of this: "Changing
any operation increments the random schema version and invalidates incompatible
recovery rather than silently changing a compatible run." These vectors are the
recorded behaviour of schema version 1.

### Why these vectors were not produced by the C# implementation

These six files are the ones with commit-order independence. The random vectors
were generated by an **independent pure-Python reference implementation of
PCG-XSH-RR 64/32 and SplitMix64**, written directly from doc 20:51-91 and
committed *before* the C# implementation existed.

This matters, and it is the entire point of the work package. If the goldens had
been generated by running the C# under test, they would prove only that the
implementation agrees with itself: a transcription error — a wrong shift, a
rotate in the wrong direction, an output taken from the state *after* the advance
instead of before — would be silently baked into the goldens, and every
downstream consumer (map generation, encounters, combat, progression) would agree
with the bug undetectably, because all of them inherit these numbers.

Because the goldens were committed first, the C# test can only pass by
reproducing numbers the implementation did not produce.

`GoldenText` (`tests/shared/GoldenText.cs`) reinforces this: when a golden is
**absent** it writes the file *and still fails the test*. A missing golden can
therefore never be silently created by a green run.

### External reference check

The doc's constants were checked against published primary sources rather than
taken on trust, because a transcription error in the specification itself would
otherwise be invisible. Every constant is standard:

| Element | Doc 20 value | Published source | Match |
| --- | --- | --- | --- |
| LCG multiplier | `6364136223846793005` | `pcg32_random_r` in `pcg_basic.c` | yes |
| Output xorshifts | 18 then 27 | `((oldstate >> 18u) ^ oldstate) >> 27u` | yes |
| Rotate amount | prior state's top five bits | `rot = oldstate >> 59u` | yes |
| Output source | state *before* the advance | `oldstate` captured before advancing | yes |
| Init sequence | state 0; inc `(sel<<1)\|1`; advance; `+= stateSeed`; advance | `pcg32_srandom_r` verbatim | yes |
| Mix gamma | `0x9E3779B97F4A7C15` | SplitMix64 `next()` increment | yes |
| Mix multiplier 1 | `0xBF58476D1CE4E5B9` | SplitMix64 first multiplier | yes |
| Mix multiplier 2 | `0x94D049BB133111EB` | SplitMix64 second multiplier | yes |
| Mix shifts | 30, 27, 31 | SplitMix64 shift sequence | yes |

Sources:

- PCG reference C implementation, `pcg32_random_r` and `pcg32_srandom_r`:
  <https://github.com/imneme/pcg-c-basic/blob/master/pcg_basic.c>
- Published `pcg32-demo` output ("Round 1", 32bit):
  <https://www.pcg-random.org/using-pcg-c-basic.html>
- `pcg32-demo` seeding, `pcg32_srandom_r(&rng, 42u, 54u)`:
  <https://github.com/imneme/pcg-c-basic/blob/master/pcg32-demo.c>
- SplitMix64 (Vigna), constants and shifts:
  <https://prng.di.unimi.it/splitmix64.c>

`Mix` is therefore exactly SplitMix64's `next()`, and doc 20:55's initialization
is exactly canonical `pcg32_srandom_r` with `stateSeed` as `initstate` and
`selector` as `initseq`. That equivalence yields a genuine external check:
feeding `initstate = 42`, `initseq = 54` into the same core must reproduce the
published demo stream, and it does, exactly.

```
published : 0xa15c02b7 0x7b47f409 0xba1d3330 0x83d2f293 0xbfa4784b 0xcbed606e
reference : 0xa15c02b7 0x7b47f409 0xba1d3330 0x83d2f293 0xbfa4784b 0xcbed606e
```

The C# implementation should carry this same 42/54 assertion as a unit test. It
is the only check here anchored to a value published outside this repository, so
it is the one that catches a shared misreading of the spec rather than a
disagreement between our own two implementations.

### Details fixed by decision

Doc 20:86-87 constrains these two points but does not fully determine them. They
are settled here because four downstream streams depend on them, so the choice
has to be written down rather than left implicit in whichever implementation
landed first.

#### 1. `[0,1)` double bit layout

Doc 20:87 requires "53 random bits under one golden-tested conversion" without
stating the layout. Fixed as: two 32-bit draws, **first draw as the high half**,
mantissa from the **top 53 bits** of the resulting 64-bit value.

```
hi    = NextUInt32();               // first draw
lo    = NextUInt32();               // second draw
bits  = ((ulong)hi << 32) | lo;
m53   = bits >> 11;                 // low 11 bits discarded
value = m53 * (1.0 / 9007199254740992.0);   // 2^53
```

Chosen over the MT19937 `genrand_res53` 27/26 split because `(x >> 11) * 2^-53`
is the dominant published construction for a `[0,1)` double from a 64-bit
source, it is a single obvious C# expression with no room for an off-by-one, and
"the first draw is the more significant half" is the natural reading of draw
order. Each value consumes exactly two draws.

`random-unit-double-conversion.txt` pins the raw 53-bit mantissa **and** the
IEEE 754 bit pattern, so the conversion is verified independently of any
double-to-text formatting.

#### 2. Bounded-integer rejection threshold

Doc 20:86 mandates rejection sampling "rather than modulo reduction" without
stating the threshold. Fixed as the canonical PCG one:

```
threshold = (2^32 - bound) % bound;      // unchecked((uint)-bound) % bound
do { r = NextUInt32(); } while (r < threshold);
return r % bound;
```

Unbiased because exactly `2^32 - threshold` values are accepted and that count
is an exact multiple of `bound`, so every residue is reachable from an equal
number of accepted draws. This matches `pcg32_boundedrand_r` in the PCG
reference implementation.

`bound == 1` gives `threshold == 0`, so the first draw is always accepted and
**one draw is consumed**. The no-draw rule of doc 20:89 is a property of
*selection*, not of this primitive — see the note on ambiguities below.

### Ambiguities in doc 20:49-91 worth a second reading

- **Draw accounting during priming.** Doc 20:55 says to advance twice "before
  returning the first caller-visible value". The phrase "caller-visible" is read
  here as excluding the two priming advances from any draw count, so
  `random-stream-initialization.txt` output index `00` is the first draw a caller
  ever sees. The priming outputs are computed and discarded.
- **Whether `bounded(1)` consumes a draw.** Doc 20:89 puts the no-draw rule on
  *selection* ("An empty/singleton selection consumes no draw"), while doc 20:88
  defines selection as establishing canonical order and then drawing an index.
  Read literally, `Select` must short-circuit before reaching the generator for
  0 or 1 candidates, but `bounded(1)` called directly is not covered. Resolved as
  above: `Select` short-circuits (zero draws), `bounded(1)` follows canonical PCG
  and consumes one. Both behaviours are pinned by fixtures so the split cannot
  drift.
- **"Canonical candidate order" is not defined here.** Doc 20:88 requires it and
  doc 20:83 says stable IDs and ordinals "come from canonical manifest/order
  rules, never dictionary or scene enumeration", but the ordering key belongs to
  each calling system. `random-degenerate-selection.txt` uses ordinal-ascending
  order on simple keys purely to demonstrate that canonical order is applied
  before the index draw; it does not define canonical order for any real domain
  collection.
- **Instance keys are domain-defined.** Several families take a "stable generated
  region ID" or similar (doc 20:62-80). `random-stream-independence.txt`
  deliberately uses instance key 0 for all 23 families so the family key is the
  only varying input; it does not assert anything about real instance-key values.

## A mismatch is investigated, never regenerated

If a test in this directory fails, the default assumption is that **the
implementation is wrong**, not that the golden is stale. These numbers were
derived from the authoritative documents — and, for the random vectors,
independently cross-checked against published reference vectors; they are not a
snapshot of observed behaviour.

`GoldenText`'s update mode is deliberately not an escape hatch:
`MECHAMINER_GOLDEN_UPDATE=1` rewrites the golden **and the test still fails**.
Updating a golden can therefore never turn a run green by itself. Per doc 91,
accepting a new golden requires an authority-aware review of the underlying
behaviour change; per doc 114 § Failure and retry policy, editing a golden to
make a gate pass is forbidden.

Concretely, a mismatch in a `random-*.txt` file means one of:

1. the C# implementation has a bug (overwhelmingly the most likely);
2. the random schema version is being changed, which per doc 20:55 must be an
   explicit version increment with recovery invalidation, not a golden edit; or
3. doc 20:49-91 itself changed, in which case the Python reference is rewritten
   from the new text and the vectors are regenerated as a reviewed act.

A mismatch in a `time-*.txt` or `runtime-*.txt` file means either the first of
those, or that the governing section of doc 10 or doc 20 changed — in which case
the tick rate, boundary, or pause rule is restated from the new text and the
vectors are regenerated as a reviewed act. Neither is a licence to edit the
golden to match observed output.

A mismatch in `entities-store-ordering.txt` or `events-simultaneous-ordering.txt`
means the first of those, or that the documented comparator itself changed. A
comparator change is not a golden edit: doc 10 § System phase ordering requires a
regression test for any observable ordering change, so the new key order is
restated from the document, the per-case detection matrix in the negative control
is updated to say which cases now reach which key, and only then are the values
rebuilt. A case that stops noticing a degradation it used to notice is the same
kind of defect as a mismatch, because it means the file is no longer evidence
about the component it was added for.
