---
doc_id: GDD-CONVENTIONS
title: Documentation Conventions
status: active
authoritative: true
---

# Documentation Conventions

## Canonical format

Markdown in this directory is the source of truth for the player-visible game specification. Supporting diagrams should use Mermaid when practical so they remain reviewable as text. Structured data may use YAML or CSV when a large homogeneous catalog becomes harder to maintain as a Markdown table, but the relevant gameplay document must explain the fields and link to it.

## Authority and certainty

Every substantive statement must be distinguishable as one of the following:

- **Decided:** explicitly chosen by the game owner. It is canonical until superseded.
- **Provisional:** a working choice the game owner has allowed the specification to use, but which still needs validation.
- **Proposed:** an option suggested for consideration. It is not part of the game unless accepted.
- **Open:** unresolved. It must have an entry in [Open Questions](./open-questions.md).
- **Out of scope:** deliberately excluded from the current game or specification.

Ordinary declarative prose in a gameplay document is **Decided** unless the surrounding section is explicitly labeled otherwise. Do not silently turn an inference, genre convention, repository name, research result, or agent suggestion into a decided fact.

## Bounded reference precedent

[DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md) is the explicit exception to the general prohibition on silently importing a genre convention. When a standard single-player gameplay detail is genuinely absent:

1. Apply any explicit canonical rule or accepted decision first.
2. Within movement and collision feel, camera, ordinary combat pressure and spawning, boss pursuit, pause/run flow, active HUD, and results conventions, consult the simplest core normal-stage *Vampire Survivors* behavior.
3. Reject the reference behavior if it conflicts with mining and fabrication, procedural finite maps, the science-fiction mech theme, the 35-minute structure, or another project rule.
4. Do not import XP, level-up offerings, chests, static stages, reference weapon acquisition or evolution, run duration, loadout counts, economy, metaprogression prices, modes, multiplayer, platforms, art, DLC, exceptions, or secrets.
5. If the reference has multiple variants or no clear analogue, leave the detail open.

Reference-derived behavior must be stated explicitly in the relevant canonical gameplay document before agents depend on it. Verify non-obvious facts through [RES-001](./research/RES-001-vampire-survivors-reference.md) or a current source rather than relying on memory.

## Stable identifiers

Use stable IDs wherever another document may need to reference an item:

- Documents: `GDD-<DOMAIN>`
- Decisions: `DEC-###`
- Open questions: `OQ-###`
- Research notes: `RES-###`
- Player-visible rules or requirements, when individual traceability is useful: `<DOMAIN>-###`

Never reuse a retired ID. Preserve redirects or supersession notes when moving important content.

## Document shape

Gameplay documents should use the following sections when applicable, omitting only those that genuinely add no value:

1. **Purpose and player promise** — why the feature exists in the experience.
2. **Player-facing summary** — the simplest accurate description from the player's perspective.
3. **Rules and flow** — complete normative behavior, including prerequisites and outcomes.
4. **Inputs and controls** — how the player acts.
5. **Feedback and presentation** — how state and consequences are communicated.
6. **Content and variation** — authored instances, procedural ranges, or taxonomy.
7. **Progression and economy interactions** — unlocks, costs, rewards, and persistence.
8. **Difficulty and balance intent** — desired pressures, viable strategies, and guardrails without prematurely fixing every tuning value.
9. **Interactions and edge cases** — collisions with other rules, interruptions, invalid actions, disconnects, and boundary conditions.
10. **Onboarding and accessibility** — how the feature is taught and made usable.
11. **Open questions** — links to relevant entries in the central register; no duplicate question text as a second source of truth.
12. **Related documents** — bidirectional links to dependencies and consumers.

Use tables for true matrices and repeated fields, not as a substitute for explanatory prose. Prefer explicit examples for complicated rules, but label examples non-normative when they use illustrative tuning values.

## Language

- Describe observable behavior in present tense: “The player chooses one upgrade,” not “The player will choose one upgrade.”
- Name the actor. Avoid ambiguous phrases such as “it triggers” when several systems are in context.
- Use canonical glossary terms consistently; record important synonyms so search still works.
- State units, timing boundaries, rounding behavior, ordering, and persistence whenever they affect outcomes.
- Separate design intent (“encourages repositioning”) from the rule that is intended to produce it.
- Use exact quantities only when decided. Otherwise describe the required relationship and create an open question if a value blocks further design.
- Avoid implementation prescriptions such as class names, database schemas, engines, or algorithms unless they define observable behavior.

## Change discipline

- Update all affected documents when a decision changes; do not leave contradictions for readers to reconcile.
- Record consequential or non-obvious changes in the decision log, including what they supersede.
- Close, defer, or split related open questions when a decision resolves them.
- Add new glossary terms when a feature introduces specialized vocabulary.
- Cite external research near the claim and preserve full provenance in a research note.
- Explicitly flag deliberate exceptions to a general rule.

## Completeness tests

A feature is not comprehensively specified until a reader can answer:

- What can the player perceive and do?
- When is each action available, unavailable, interrupted, or forced?
- What state changes, for how long, and with what persistence?
- What does success, failure, cancellation, and repetition produce?
- How does the game communicate every relevant state and consequence?
- How does the feature interact with progression, economy, difficulty, content, UI, audio, narrative, accessibility, multiplayer, saving, and pausing where applicable?
- What happens at minimum, maximum, simultaneous, and otherwise unusual states?
- Which parts are decided, provisional, proposed, open, or out of scope?
