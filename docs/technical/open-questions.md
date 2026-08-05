---
doc_id: TDD-OPEN-QUESTIONS
title: Technical Open Questions
status: active
authoritative: true
---

# Technical Open Questions

This is the authoritative register of unresolved technical choices. A question appears here only when different answers would materially affect architecture, delivery scope, or implementation sequencing. Routine local coding choices do not belong here.

## Active

There are no foundational technical questions that currently require owner input. Autonomous agents implement accepted/provisional defaults and use the decision and escalation rules in the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md); they do not convert ordinary local choices into new questions. Subsystem documents may record measured tuning work, production content work, or explicitly deferred product features without treating them as architectural ambiguity.

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
