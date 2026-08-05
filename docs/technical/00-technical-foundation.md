---
doc_id: TDD-FOUNDATION
title: Technical Foundation
status: active
authoritative: true
---

# Technical Foundation

## Purpose

This document fixes the project-wide technology boundary. Detailed runtime, simulation, rendering, content, persistence, tooling, and delivery contracts build on it.

## Accepted stack

| Concern | Decision |
| --- | --- |
| Engine | Godot 4.7.1, .NET-enabled distribution |
| Runtime language | C# using the .NET baseline supported by the pinned Godot release |
| Gameplay scripting | C# only; GDScript is not used in production gameplay or tooling |
| Renderer | Godot Mobile renderer as the production baseline |
| Gameplay medium | Native low-poly 3D under an orthographic top-down camera, with 2D UI and selective billboard effects |
| Initial release targets | Windows PC through Steam and Steam Deck as a first-class Linux target |
| Development host | macOS on Apple Silicon is supported for authoring and local verification; it is not currently a release target |
| Source format | Text-first, version-controlled project, code, data, shaders, and documentation |

The stack decision is recorded in [TDR-001](./decisions/TDR-001-use-godot-csharp-and-mobile-renderer.md). Player-facing platform and visual requirements come from [DEC-113](../decisions/DEC-113-target-windows-pc-and-steam-deck-first.md) and [DEC-114](../decisions/DEC-114-use-native-low-poly-3d-gameplay.md).

## Language boundary

- Runtime logic, Godot integration, editor utilities, import validation, and automated tests use C#.
- Engine shader files use the Godot shader language because that is the renderer contract, not an additional gameplay scripting language.
- The core simulation is written as ordinary C# types without inheriting from `GodotObject` unless an integration boundary requires it.
- Godot nodes own engine lifecycle and presentation concerns; they do not become the authoritative representation of every simulated enemy, projectile, pickup, or transient effect.
- Hot simulation loops minimize calls across the C#-to-Godot native boundary. Presentation receives compact batched state instead.
- Adding GDScript or another runtime language requires a TDR that identifies the ownership boundary, build impact, test strategy, and benefit that outweighs mixed-language complexity.

## Renderer baseline

The Mobile renderer is selected because the accepted art direction uses simple low-poly geometry and prioritizes stable performance on Steam Deck over advanced desktop-only lighting. Forward+ features are not assumed. A representative-art stress prototype must validate the renderer before production asset budgets are final.

Changing renderers is permitted only after a captured compatibility and performance comparison demonstrates that the Mobile renderer cannot satisfy an accepted presentation requirement or that another renderer materially improves the target-device result without breaking Steam Deck support.

## Version policy

- The repository pins the exact Godot editor and export-template version used by CI and release builds.
- Patch updates within the selected Godot minor line should be adopted after automated tests, representative save loading, rendering captures, and Windows/Steam Deck smoke tests pass.
- Minor or major Godot upgrades require a TDR because they can change serialization, rendering, physics, import results, or C# bindings.
- .NET SDK selection follows the pinned Godot version's supported baseline and is pinned in repository tooling rather than inferred from a developer machine.
- Third-party packages are exact-version locked. Dependencies with runtime or save-format reach require an ownership and exit strategy in the relevant subsystem document.

## Platform boundary

Initial production acceptance covers:

- Windows at the 1920×1080 reference layout with keyboard/mouse and gamepad;
- Steam Deck at 1280×800 with gamepad-only completion of every standard flow; and
- representative lower-power performance testing using the maximum-pressure scenario defined by the gameplay specification.

Native console, mobile, web, touch-first, macOS release, and non-Steam Linux distribution are outside the initial delivery contract. Architecture should avoid gratuitous platform coupling, but these platforms do not impose acceptance work until separately approved.

## Foundational verification gates

Before the internal demo architecture is treated as viable, a technical spike must demonstrate:

1. .NET Godot builds run from a clean checkout on the macOS development host.
2. Automated pure-C# tests run without launching the Godot editor.
3. Windows and Steam Deck export pipelines produce launchable artifacts.
4. The Mobile renderer displays representative low-poly models, animation, shadows or ground-contact treatment, particles, and 2D HUD at both reference layouts.
5. A data-oriented horde prototype sustains the accepted PERF-04 peak scenario without representing every ordinary enemy as a full Godot scene tree and physics body.
6. Whole-simulation pause freezes all gameplay clocks while UI remains responsive.

The enforceable performance, memory, entity, draw-call, and asset budgets are defined in the runtime, presentation, asset, and observability documents. Foundation work consumes those budgets rather than creating substitute thresholds.

## Official references

- [Godot 4.7.1 release archive](https://godotengine.org/download/archive/)
- [Godot license](https://godotengine.org/license/)
- [Godot system requirements](https://docs.godotengine.org/en/stable/about/system_requirements.html)
- [C# basics and interop considerations](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Using servers for performance](https://docs.godotengine.org/en/4.4/tutorials/performance/using_servers.html)
- [Using MultiMesh](https://docs.godotengine.org/en/4.3/tutorials/performance/using_multimesh.html)

## Related documents

- [Gameplay Specification](../README.md)
- [Technical Decision Log](./decisions/README.md)
- [Technical Open Questions](./open-questions.md)
