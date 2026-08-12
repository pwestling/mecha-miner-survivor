---
doc_id: TDD-OPEN-QUESTIONS
title: Technical Open Questions
status: active
authoritative: true
---

# Technical Open Questions

This is the authoritative register of unresolved technical choices. A question appears here only when different answers would materially affect architecture, delivery scope, or implementation sequencing. Routine local coding choices do not belong here.

## Active

Two foundational technical questions currently require owner input; they are recorded below. Autonomous agents otherwise implement accepted/provisional defaults and use the decision and escalation rules in the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md); they do not convert ordinary local choices into new questions. Subsystem documents may record measured tuning work, production content work, or explicitly deferred product features without treating them as architectural ambiguity.

### TOQ-004 — What is the authored projectile range, in metres, of the Needler and Prism Crown?

- **Why it matters:** Three separate reasons, and each stands on its own.
  - **A screen width is a presentation value.** [Presentation and Rendering § Camera](./30-presentation-and-rendering.md#camera) fixes the gameplay camera at 24 gameplay metres vertically, "approximately 42.7 meters horizontally" at 16:9 and "approximately 38.4 meters" at 16:10. Both aspect ratios are in initial scope, so "one screen width" is not even one number: the same authored sentence yields a 42.7M reach on a 1920×1080 desktop and a 38.4M reach on a 1280×800 Steam Deck. Deriving a projectile lifetime from an aspect ratio would make the simulation depend on presentation, which [Simulation Core § Scope and invariants](./20-simulation-core.md#scope-and-invariants) forbids — the simulation is "a pure C# library with no dependency on Godot, files, Steam, rendering, audio, wall time, or mutable global services". It would do so invisibly, because the resulting figure would sit in a content definition looking exactly like an authored simulation constant.
  - **"Slightly beyond" has no magnitude.** No compiler can consume it. [Content Data and Validation § Unit and numeric policy](./40-content-data-and-validation.md#unit-and-numeric-policy) requires geometry to distinguish radius, diameter, width, range, and area and requires unit suffixes on ambiguous numeric names; there is no legal authoring of "slightly beyond one screen width" under that policy at all.
  - **It is load-bearing.** The approximately 19-second flight time in [Combat and Weapon Runtime § Enemy projectile ceiling](./22-combat-and-weapon-runtime.md#enemy-projectile-ceiling) is derived from that one sentence, and it is the figure the entire enemy-projectile ceiling analysis rests on. The ceiling was wrong by a factor of two — 512 against a legal peak near 1,010 — because nobody had converted the sentence into a number. `TOQ-003` is the remaining half of the same accident.
- **Blocks:** `COM-002` projectile resolution (a projectile store cannot be sized or its expiry implemented without the number); `ENC-004` Needler state and projectile behavior; `ENC-007` Prism Crown state machine; and the content definitions for `EN-06` and `BOSS-03`.
- **Known constraints:** The only authored facts are the speed and the prose. [Initial Alien and Boss Roster § EN-06 — Needler](../31-initial-alien-roster.md#en-06--needler) authors the Needler's speed as "75% of the unmodified mech's movement speed, or 2.25M/s" and its range only as "its lifetime carries it slightly beyond one screen width". [§ BOSS-03 — Prism Crown](../31-initial-alien-roster.md#boss-03--prism-crown) authors the same 2.25M/s and says each projectile "disappears after crossing slightly more than one screen width or hitting solid terrain". No technical document supplies the missing value: [Encounter Director and Enemy Runtime § Needler](./23-encounter-director-and-enemy-runtime.md#needler) states only that "projectile speed, damage, lifetime, terrain collision, and no-homing flags are snapshotted at creation", [Simulation Core § Authoritative population categories](./20-simulation-core.md#authoritative-population-categories) lists `lifetime` as required enemy-projectile state, and [Content Data and Validation § Enemies and bosses](./40-content-data-and-validation.md#enemies-and-bosses) declares the field group as "projectile or boss-ability parameters" without naming or valuing a range or lifetime field. Terrain collision already ends a Prism Crown projectile early and is unaffected by the answer.
- **Candidate answers:** Offered, not decided. The recommended shape is to **author a range in metres in the simulation domain** and demote the screen-width sentence to the design rationale for choosing that number, rather than leaving it as the source of the number. Lifetime then becomes **derived** — `range ÷ speed`, with both operands authored — which places it under the derived-value rule in [§ Enemies and bosses](./40-content-data-and-validation.md#enemies-and-bosses) — that section states "Derived geometry is never authored" and that an authored derived value "creates a second source of truth that silently disagrees with the first the moment either operand changes" — and it yields a checkable invariant afterwards instead of a second constant to keep in sync. On that shape the content definition for `EN-06` and `BOSS-03` carries a range and **no lifetime at all**. Whatever number is minted must state explicitly whether it covers both the Needler and the Prism Crown or whether they get separate ranges; the two currently share a speed and a range sentence, and an answer that silently fixes only one leaves the other unauthored.
- **General rule this argues for:** this instance is one case of a broader rule — **a simulation value must not derive from a presentation value.** A compiler could enforce it if content fields declared their domain, so that a presentation-domain operand feeding a simulation-domain field became a validation error rather than a review question. That machinery is not worth building for a single instance, and this is the only instance found so far. If a third appears, it is worth building then. Nothing in this question depends on that rule being written down; it is recorded here so a later reader does not have to rediscover the generalization.
- **Status:** open
- **Owner:** needs a design owner. Projectile reach is player-visible — it sets how far a Needler can safely stand off — so it is a gameplay choice under [Autonomous Agent Execution Protocol § Explicit escalation boundary](./114-autonomous-agent-execution-protocol.md#explicit-escalation-boundary), not an agent-local one. Recording the number is then ordinary specification maintenance.

### TOQ-003 — What bounds the alive per-identity enemy share?

- **Why it matters:** No finite enemy-projectile ceiling is provably safe until this is answered. Composition shares in the [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md#complete-35-minute-schedule) bound *replenishment*, not the alive mix. Under high churn the alive share of one identity can exceed its composition share, because an automatic-weapon build clears contact pursuers faster than it clears Needlers standing off at range. The 2,048 figure in [Combat and Weapon Runtime § Enemy projectile ceiling](./22-combat-and-weapon-runtime.md#enemy-projectile-ceiling) is therefore safe only against the replenishment-bounded worst case of approximately 1,010. In the pathological limit — the persistent legal pool of 450 baseline plus 150 beacon-tagged enemies alive as Needlers, at the resonance-shortened 3.75-second period — the count reaches approximately 3,200, above 2,048 as well.
- **Blocks:** `ENC-002` (director schedule compiler, pulses, weighted residual composition, ceilings/queues); the capacity behavior of `COM-001` and `COM-002`; and the extreme-kill stress fixture required by [Encounter Director and Enemy Runtime § Verification](./23-encounter-director-and-enemy-runtime.md#verification), which can drive exactly this condition.
- **Known constraints:** The 450 baseline, 100 scheduled-event, and 150 beacon ceilings bound total ordinary population and say nothing about its identity mix. Needler is the sole ordinary projectile specialist, so its alive share alone sets the ordinary enemy-projectile count. Doc 23 § Verification already requires an extreme-kill full-run fixture. An agent must not silently reduce enemies as a performance response, and `CMP-PRE-002` must not remove authoritative actors on saturation.
- **Candidate answers:** Three bounded options. The first two carry real costs, so this is not a free choice.
  - **(a) The director bounds per-identity alive share, not only replenishment share.** The only answer that holds for an unbounded real run. `CMP-ENC-001` forbids "adaptive difficulty from player strength", and an alive-share clamp does react to kill throughput; a fixed authored capacity invariant is arguably not difficulty adaptation, but the answer must name that distinction rather than assume it.
  - **(b) The extreme-kill stress fixture authors an explicit Needler-share bound.** Cheap, and it makes the ceiling provable — but only under a bounded fixture, leaving a pathological real run unguarded.
  - **(c) Enemy projectiles get a defined saturation behavior.** Reducing enemy population is forbidden as a performance technique and authoritative actors must not vanish on saturation, so any such behavior constrains several subsystems and needs a TDR rather than a local choice.
- **Status:** open
- **Owner:** the encounter director's owner. This is not an agent-local choice and may require a TDR.

## Resolved

### TOQ-002 — What frame-rate guarantee does the initial release make?

- **Resolution:** Steam Deck must sustain 60 FPS at 1280×800 during the accepted maximum-pressure benchmark. The Windows reference target is likewise 60 FPS at 1920×1080 on the eventual minimum-spec tier. Unsupported lower-power PCs may expose a separately labeled 30 FPS fallback, but it does not weaken target-device acceptance.
- **Record:** [TDR-003](./decisions/TDR-003-require-sixty-fps-on-steam-deck.md)

### TOQ-001 — What reproducibility guarantee does a run provide?

- **Resolution:** The simulation uses a fixed timestep and stable, seeded, independently owned random streams for reproducible generated content and diagnostic scenarios. Diagnostics retain the seed, build identity, relevant configuration, and major run events. The product does not promise bit-exact input replay across builds or platforms.
- **Record:** [TDR-002](./decisions/TDR-002-use-seeded-reproducibility-without-lockstep-replay.md)

### TOQ-000 — Which engine and runtime language does the project use?

- **Resolution:** Godot 4.7.1 with C#/.NET and the Mobile renderer. Runtime and tooling logic use C# rather than a mixed C#/GDScript codebase.
- **Record:** [TDR-001](./decisions/TDR-001-use-godot-csharp-and-mobile-renderer.md)
