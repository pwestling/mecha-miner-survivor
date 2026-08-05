---
doc_id: TDR-001
title: Use Godot 4.7.1, C#, and the Mobile Renderer
status: accepted
authoritative: false
validation: architecture-and-performance-spike
---

# TDR-001 — Use Godot 4.7.1, C#, and the Mobile Renderer

## Decision

Build the game with the .NET-enabled Godot 4.7.1 distribution. Use C# for runtime logic, engine integration, tools, and tests; do not use GDScript for production gameplay. Use Godot's Mobile renderer as the initial production renderer.

## Context

The game requires native low-poly 3D, a fixed orthographic camera, dense animated hordes, extensive 2D interfaces, automatic weapons with many transient effects, procedural finite maps, Windows and Steam Deck delivery, and a technical specification suitable for implementation by coding agents.

The most credible alternatives were Unity 6 with C#, Phaser 4 with TypeScript, and Godot with typed GDScript.

## Considered alternatives

### Unity 6 with C#

Unity offers mature profiling, a large asset ecosystem, and established data-oriented technologies. It was rejected because this project does not require its broader commercial ecosystem enough to justify its larger operational and licensing surface.

### Phaser 4 with TypeScript

Phaser provides extremely fast browser iteration, strong agent ergonomics, and excellent 2D sprite throughput. It was rejected because the accepted game uses native 3D and targets native desktop distribution. Phaser is explicitly 2D and web-first; adding a separate 3D renderer and desktop wrapper would erase much of its simplicity.

### Godot with typed GDScript

Typed GDScript offers the tightest editor integration, least setup, and fastest scene-script iteration. It remains capable of shipping this game. C# was selected because the project benefits more from a pure, independently testable simulation library; richer type-system contracts; mature automated testing and profiling tools; and efficient data-oriented hot loops.

### Mixed C# and GDScript

Mixing languages could place small scene behaviors in GDScript while retaining C# simulation. It was rejected because cross-language ownership, binding, debugging, and agent navigation costs outweigh the small reduction in presentation boilerplate.

## Rationale

Godot directly supports the accepted 3D/2D composition and native target platforms while remaining MIT-licensed and text-friendly. C# supports explicit system boundaries and automated verification for the unusually large catalog of interacting weapons, branches, utilities, relics, enemies, resources, and procedural rules.

The Mobile renderer is suited to visually simple scenes and lower-power targets. The design does not depend on advanced Forward+ features, while Steam Deck performance is a first-class constraint.

## Consequences and risks

- The project requires the .NET Godot build, a pinned .NET SDK, and an external C# editor.
- Hot loops must not repeatedly traverse Godot nodes or properties across the managed/native boundary.
- The simulation/presentation split must be deliberate; Godot nodes remain appropriate for durable presentation and UI composition.
- Godot 4 C# web export is unavailable, and mobile support is not an initial target.
- C# build time and binding differences add some friction relative to GDScript.
- Steam Deck performance still requires a representative prototype; engine selection alone does not guarantee it.

## Validation and reversal signals

Validate the decision with the foundational spike in [Technical Foundation](../00-technical-foundation.md). Reconsider part of the decision if:

- representative horde rendering cannot meet the eventual Steam Deck frame budget;
- required low-poly asset workflows fail in the Mobile renderer;
- C# iteration or deployment introduces sustained blocking friction that cannot be removed through tooling; or
- web delivery becomes an accepted product requirement.

Failure first triggers profiling and architecture correction rather than an automatic engine change.

## Specification links

- [Technical Foundation](../00-technical-foundation.md)
- [Gameplay Specification](../../README.md)
- [DEC-113 — Target Windows PC and Steam Deck First](../../decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
- [DEC-114 — Use Native Low-Poly 3D Gameplay](../../decisions/DEC-114-use-native-low-poly-3d-gameplay.md)

## Supersedes / superseded by

Initial technical stack decision; supersedes no prior technical record.
