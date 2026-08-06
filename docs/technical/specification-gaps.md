---
doc_id: TDD-SPECIFICATION-GAPS
title: Specification Gaps
status: active
authoritative: false
---

# Specification Gaps

> **Non-normative.** This document adds no requirements, defines no behavior, and
> overrides nothing. It is a log of places where the specification read complete
> and was not. Nothing here is authority for an implementation choice; an entry
> becomes actionable only when it is triaged into a `TOQ`, an `OQ`, a `DEC`, or a
> documentation fix.

## Three sentences is a complete entry

**What you were implementing. What you expected to find. What you found instead.**

That is the whole format. There is no ID, no owner field, no candidate answers, and
no blocks list. Append your entry to the bottom of [Entries](#entries) and carry on
with your task.

Three properties of this register, in order of how much they matter:

1. **Three sentences is a complete entry, not a partial one.** A three-sentence
   observation with no analysis behind it is exactly the contribution this file
   wants. It is not a first draft of something better.
2. **You are not required to propose a resolution, and you are not expected to have
   investigated.** "I do not know what the right answer is" is a normal thing for an
   entry to say, and an entry that says it is complete.
3. **Recording a gap is not a complaint about the specification, and needs no
   justification.** You do not have to argue that the gap matters, estimate its
   impact, or explain why you are raising it. Write what you hit and stop.

The reason these are stated so bluntly is that the obstacle this register exists to
clear is not fear of being wrong. It is the finder's own judgement about what
deserves someone else's attention: a stream that sat on a real gap for a while did
so because raising it **felt like low-value noise unless the work had already been
done to make it actionable.** A register that merely *permits* an unresolved
observation does not clear that. So this file says it actively — an unanalyzed
three-sentence observation is a complete and welcome contribution.

**An entry that turns out to be a misreading rather than a gap costs nothing, and
should still be recorded.** "I might just be reading it wrong" is the other reason
people stay quiet, and it is a reason to write the entry rather than not to: the
misreading is itself evidence that the passage is easy to misread, which is the same
finding by a different route. Triage will say so, and that is a useful outcome, not
an embarrassing one.

## The defect class

This register is for **a specification that reads complete and is not.**

It is not for a missing section. A missing section announces itself — the heading is
absent, the table has no row, the document says "to be defined". Those get caught by
anyone reading the document.

This class is the opposite. The text is fluent, internally consistent, and passes
review, and it still cannot be implemented without guessing. Typical shapes:

- A value expressed only in units the simulation cannot hold — a distance in screen
  widths, a duration in "a moment", a size relative to something never sized.
- Two halves of one rule present in the same document with nothing saying they are
  two cases of one rule, so a reader following one half produces something the other
  half forbids.
- A quantity that every dependent constraint is expressed as a function of, and that
  no document ever states — visible only to whoever first tries to evaluate one of
  those constraints numerically.
- A derived column whose rounding is not declared, so a rounded presentation is read
  as a second authored value.

The common signature: **nothing reads as absent, so nothing prompts a question.**

## Triage

The integration owner triages entries. An entry is routed to exactly one of:

- **`TOQ-###`** in [Technical Open Questions](./open-questions.md) — an architectural
  or sequencing question with a real technical choice in it.
- **`OQ-###`** in the gameplay [Open Questions](../open-questions.md) — a
  player-visible design question.
- **A `DEC`** in the gameplay [Decision Log](../decisions/README.md) — the value or
  rule can simply be decided.
- **A documentation fix** — the specification already determines the answer and the
  text was merely unclear or incomplete. This is the most common outcome and the
  cheapest.

The outcome is linked back on the entry. **Until it is triaged, an entry is just an
observation** and carries no authority. Nobody blocks on an untriaged entry, and
nobody implements against one.

## This is not either open-question register

There are two open-question registers and this is neither of them.

| File | Holds | Requires |
| --- | --- | --- |
| [Open Questions](../open-questions.md) | `OQ-###` — unresolved *gameplay design* questions | A shaped question with why-it-matters, blocks, known constraints, and a status |
| [Technical Open Questions](./open-questions.md) | `TOQ-###` — unresolved *technical* choices | The same, plus a named owner, and only where different answers would materially affect architecture, delivery scope, or sequencing |

Both are authoritative registers whose entries are already shaped like a question
with a resolution path. Getting an entry into either one is work, and it is work the
finder is frequently not positioned to do.

This file exists for findings that **are not yet shaped like a question.** It is the
intake, not a third register. Every entry that turns out to be a genuine open
question leaves here and lands in one of the two above.

## Why the implementing stream catches this class

Every instance recorded below was found by someone building against the
specification who discovered they would have to guess a number or a name to
continue.

That is not a coincidence. A reviewer reading for correctness does not hit this
class, because the text *is* correct — self-consistent, well-formed, and free of
contradictions a reviewer could point at. The gap only becomes visible at the moment
someone has to produce a concrete artifact from the text: a JSON field that needs a
value, a store that needs a size, a validator that needs an operand. The implementing
stream is the only stream that reaches that moment, which makes it the only stream
positioned to notice.

The corollary is the whole reason for the low friction: if the implementing stream
does not write the finding down while it is holding it, nobody else will find it, and
the guess ships instead.

Streams may append here freely and without asking, under
[Parallel Delivery Waves and Stream Ownership § Rules every stream follows](./delivery-waves.md#rules-every-stream-follows).

## Entries

Append to the bottom. Entries are never edited except to add a triage outcome, and
never removed.

### A projectile range expressed only in screen widths

Sizing the enemy-projectile store for `COM-002` and authoring the `EN-06` and
`BOSS-03` content definitions. I expected an authored projectile range or lifetime in
simulation units, since doc 23 § Needler says speed, damage, and lifetime are
snapshotted at projectile creation. What exists is a speed (2.25M/s) and prose: the
Needler's "lifetime carries it slightly beyond one screen width", and a Prism Crown
projectile "disappears after crossing slightly more than one screen width". A screen
width is a camera property, and it is a different number at 16:9 than at 16:10, so
there is no simulation-domain value to author and no way to compute one that does not
make the simulation depend on presentation.

- **Triaged:** `TOQ-004` in [Technical Open Questions](./open-questions.md#toq-004--what-is-the-authored-projectile-range-in-metres-of-the-needler-and-prism-crown). Needs a design owner.

### Two halves of the field-naming rule, with the multiplicative case between them

Authoring enemy definitions against doc 40 and needing a name for an enemy's authored
body scale. I expected § Unit and numeric policy to say what to call it, since that
section is where field naming lives. It had a rule for percentage authoring
(`_percent`) and a rule for absolute units (`_m`, `_m_per_s`, `_seconds`), nothing for
a value that multiplies a reference dimension, and the names I would naturally have
reached for — `_factor`, `_scale`, a bare `scale` — were the vague scalar names the
same section forbids. Both halves of the rule were in the one file I was reading and
neither covered the case in front of me.

- **Triaged:** documentation fix. Doc 40 § Unit and numeric policy now names the
  `_multiplier` suffix and requires one spelling in every scope, so an enemy's
  authored body scale is `body_scale_multiplier` in source, bundle, report, schema,
  and code.

### An extraction-zone radius that every dependent constraint referenced and no document stated

Starting `MAP-003` spatial embedding and connectors. I expected to find the
extraction-zone radius stated somewhere in the mining or map documents, because the
constraints I had to implement are all expressed as functions of it: primary
connector width is defined in mining-zone diameters, deployment and mining-point
clearance likewise, and the Extraction Tether utility and Tether Amplifier PowerUp are
percentages of it. There was no radius anywhere — every consumer referenced a base
value that had never been authored, and `DEC-031` recorded it as still open under
`OQ-004` while later documents went on building percentages on top of it. I could not
evaluate a single one of those constraints numerically.

- **Triaged:** `DEC-128` set the extraction-zone radius to 3.0M and the resonance-field
  radius to 6.0M, both accepted. `MAP-003`, `MAP-006`, and `MIN-001` were blocked on
  the missing number and are not any more. **This entry is the example of the pipeline
  working:** a gap that read as complete text, recorded by the stream that hit it,
  triaged into a decision, three work packages unblocked.

### A derived column whose rounding was not declared

Reading the gameplay survivability baseline to transcribe enemy contact geometry. I
expected the derived diameter columns to be exact, and read the two-decimal figures as
authored values. One enemy's true derived diameter is 0.496M and the table displayed
`0.50M`, which reads as a second authored number rather than a rounded view of the
first — the table did not say which it was, so there was nothing in the text to warn
me.

- **Triaged:** documentation fix in the gameplay document. The affected rows now carry
  the exact derived values, and the section states that both columns are exact derived
  values rather than rounded presentations, carrying a third decimal where the
  derivation requires one.

## Related documents

- [Technical Open Questions](./open-questions.md) — `TOQ-###`, where technical entries go after triage
- [Open Questions](../open-questions.md) — `OQ-###`, where gameplay entries go after triage
- [Decision Log](../decisions/README.md) — `DEC-###`, where a decidable value goes
- [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md) — decision routing, escalation boundary, and specification maintenance autonomy
- [Parallel Delivery Waves and Stream Ownership](./delivery-waves.md) — integration ownership and the rules every stream follows
- [Technical Documentation Conventions](./conventions.md) — certainty labels and stable identifiers
