---
doc_id: TDD-OPEN-QUESTIONS
title: Technical Open Questions
status: active
authoritative: true
---

# Technical Open Questions

This is the authoritative register of unresolved technical choices. A question appears here only when different answers would materially affect architecture, delivery scope, or implementation sequencing. Routine local coding choices do not belong here.

## Active

One foundational technical question currently requires owner input; it is recorded below. Autonomous agents otherwise implement accepted/provisional defaults and use the decision and escalation rules in the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md); they do not convert ordinary local choices into new questions. Subsystem documents may record measured tuning work, production content work, or explicitly deferred product features without treating them as architectural ambiguity.

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
