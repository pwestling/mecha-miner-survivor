---
doc_id: TDD-ASSETS
title: Asset Pipeline and Budgets
status: active
authoritative: true
---

# Asset Pipeline and Budgets

## Purpose

This document defines how freely available and original 3D, 2D, VFX, audio, font, and localization assets enter the repository, become normalized runtime assets, retain license provenance, satisfy gameplay readability, and remain inside Steam Deck budgets.

## Pipeline principles

- CC0-first acquisition remains the default.
- Runtime imports are reproducible from retained source and versioned settings.
- External assets are adapted to shared scale, palette, material, animation, pivot, naming, and readability conventions before use.
- Logical asset IDs isolate content from physical paths.
- Gameplay footprints and timing come from content/simulation, not mesh bounds or animation callbacks.
- Binary sources and derived assets have clear ownership and staleness checks.
- An attractive asset that fails top-down silhouette, license, performance, or adaptation requirements is not production-ready.

## Directory roles

Accepted layout:

```text
assets-source/
  external-originals/
  blender/
  audio-masters/
  vector-source/
assets-runtime/
  models/
  crowd/
  textures/
  materials/
  vfx/
  audio/
  fonts/
  ui/
assets-manifest/
  assets.json files
  licenses.json files
  attribution output
```

Large binary source/runtime files use Git LFS once implementation begins. Generated Godot `.godot/imported` cache files are not committed. Source-license records and import sidecars are committed.

## 3D interchange and tools

- Blender is the canonical adaptation tool, pinned to one major/minor version in repository tooling.
- Runtime model interchange uses glTF 2.0 binary `.glb`, which Godot supports as a first-class 3D import format. Direct `.blend` import is not the CI/release source because it implicitly depends on a local Blender installation and export behavior.
- One Blender meter equals one gameplay meter unless an asset manifest explicitly documents a presentation-only scale.
- Apply transforms before export. Runtime root scale is `(1,1,1)`.
- Model forward is Godot negative Z; up is positive Y; ground pivot is centered on the authoritative planar footprint.
- Object, bone, material, clip, socket, and shape names follow repository naming rules and contain no vendor-specific duplicates.
- Meshes exclude embedded cameras, lights, environments, and arbitrary collision unless the manifest explicitly expects them.

Godot's official importer supports glTF/FBX/Blender scenes and configurable animation sampling and skin influence settings; glTF is the project standard. [Godot scene importer](https://docs.godotengine.org/en/stable/classes/class_resourceimporterscene.html)

## 3D asset categories and initial enforceable budgets

Budgets are measured after triangulation for the actual gameplay LOD.

| Category | Gameplay triangle target | Material slots | Texture target | Animation path |
| --- | ---: | ---: | --- | --- |
| Ordinary enemy family | ≤4,000 LOD0; ≤1,500 gameplay crowd LOD; ≤600 low LOD | 1 preferred, 2 max | shared 512–1024 atlas/palette | VAT/baked GPU clips |
| Ordinary variant | reuse rig/atlas; geometry delta within family budget | same family | same family | shared clips where possible |
| Elite additions | ≤300 added triangles | no new material preferred | atlas region | instance flag/clip reuse |
| Player mech | ≤15,000 | ≤3 | one 1024–2048 set | skinned Godot scene |
| Boss | ≤18,000 | ≤3 | one 1024–2048 set | skinned Godot scene |
| Mining site/cache | ≤6,000 | ≤2 | 1024 shared family | limited loop/state animation |
| Major landmark | ≤30,000 visible chunk total | shared/atlas favored | 1024–2048 | static/limited |
| Small prop/obstacle | ≤2,000, with instanced low LOD | 1 | shared atlas | static |
| Weapon/deployable actor | ≤3,000 | 1–2 | shared 512–1024 | analytic or limited |

The crowd gameplay LOD is the default at the fixed camera; LOD0 exists for close menus/codex where needed. These are enforceable ceilings, not targets to consume. A measured task may tighten them autonomously; raising a ceiling requires target-device evidence that the aggregate presentation and memory budgets still pass with safety margin.

## Crowd animation bake

Ordinary enemy skeletal sources are baked into GPU-instanced animation data.

- Initial sample rate is 30 frames per second.
- Each family supplies normalized locomotion plus required charge/contact/hit/death clips.
- Root translation is removed; simulation owns motion.
- Clip bounds include all poses so culling is stable.
- Vertex animation output version, source mesh/rig hash, clip list, sample rate, texture format, bounds, and generator version enter the asset manifest.
- A visual comparison renders skeletal source beside VAT result through full clips and fails on unacceptable deformation or normal error.
- CPU skeleton assets may remain for previews but are excluded from ordinary runtime batches.

If VAT complexity fails the first performance/art spike, an alternative GPU deformation technique may replace it through a measured TDR; full per-enemy skeleton scenes remain outside budget.

## Rig and animation conventions

- One root bone at ground origin; no authoritative root motion.
- Maximum four bone influences per vertex for Mobile renderer compatibility unless a tested exception is documented.
- Clip names use stable semantic IDs, not Blender action names exposed directly to gameplay.
- Loop metadata, transition duration, and playback-rate range live in the presentation manifest.
- Gameplay telegraph durations come from simulation and may time-scale or blend the clip to fit.
- Sockets use explicit semantic names for muzzle, center, effect origin, and optional presentation anchors; missing required sockets fail import validation.
- Decorative animation cannot move the visible damage footprint so far that ground correspondence becomes misleading.

## Materials and textures

- Favor palette/gradient textures, vertex colors, and simple Mobile-compatible shaders over many unique PBR sets.
- Share atlas/material families across ordinary enemies, resources, terrain, and props while preserving their value identity.
- Ordinary horde assets avoid per-instance transparent materials and expensive screen-space effects.
- Opaque or alpha-scissored materials are preferred; blended transparency is limited to effects.
- Normal maps/tangents are included only when their top-down benefit is visible and measured.
- Texture dimensions are powers of two where practical, have mipmaps, and use platform-appropriate Godot compression.
- Source masters remain lossless; runtime textures use sized/compressed derivatives.
- Emissive elements remain readable without bloom. Bloom is embellishment, not state communication.

## 2D UI and icon assets

- Prefer SVG or high-resolution vector source for icons, resource letters/patterns, map markers, focus frames, and controller-independent symbols.
- Runtime rasterization or imported texture size must remain crisp at both reference layouts and 150% text/menu scale.
- Every six-material and site identity has shape/pattern/letter variants tested in grayscale and common color-vision simulations.
- Icons use a shared grid, stroke family, safe inset, silhouette test, and light/dark background variants.
- M2–M4 controller/keyboard glyphs use Kenney Input Prompts `1.5a` SVG sources, pinned by archive/file hash under its CC0 license. Import only the Windows/Xbox-style, Steam Deck, generic gamepad, keyboard, and mouse subsets required by the initial targets; map them through semantic logical actions rather than physical filenames.

Official source: [Kenney Input Prompts](https://kenney.nl/assets/input-prompts).

## VFX assets

VFX recipes separate critical core geometry from optional embellishment.

- Core meshes/textures support solid high-contrast boundaries in reduced-VFX/reduced-flash modes.
- Particle textures share atlases and avoid unique material proliferation.
- Trails have simulation-aligned core width; decorative fringe may extend without implying damage.
- Telegraph assets include color-independent pattern/shape and exact ground-size validation.
- Every recipe declares Low/Medium/High counts and a hard concurrency/pool limit.
- Shader source is text-reviewed; variants and expected uniforms are manifest-validated.

## Audio assets

- Masters use lossless WAV/FLAC; runtime import chooses compressed streaming for music/long ambience and decompressed or efficient compressed samples for short effects according to measured memory/CPU.
- Loudness targets are normalized by category, with true-peak headroom and no clipping after common concurrency.
- Loops have validated seamless boundaries and explicit loop metadata.
- Critical cues retain recognizable midrange content on Steam Deck speakers and common headphones.
- Variant sets avoid audible repetition without unbounded memory.
- Every gameplay-relevant cue has caption/localization metadata and visual redundancy.

## Font policy

- Atkinson Hyperlegible Next is the accepted M2–M4 UI and heading family. Acquire the static Regular, Medium, and Bold weights from the Braille Institute/Google Fonts source, pin their hashes, retain the SIL Open Font License record, and use Bold rather than a second display family for headings.
- Use redistributable open-source fonts with complete license files and required glyph coverage.
- Bundle exact font versions; do not depend on system fonts for layout.
- A later stylistic heading family is a production-art change, not an implementation prerequisite; it must preserve the same layout/accessibility fixtures.
- Generate font/subset assets only when shipped locales are known; never omit required accessibility or name glyphs.
- Validate numerals, material letters, symbols, and controller prompts at the nine-pixel absolute minimum.

Official source: [Braille Institute Atkinson Hyperlegible font family](https://www.brailleinstitute.org/freefont/).

## License and provenance ledger

Every external asset record includes:

- logical asset ID and category;
- original title, author/organization, source page and direct download reference;
- acquisition date and original package/hash;
- exact license/SPDX-style identity and retained license text;
- attribution requirement and generated credit line;
- modification summary and responsible source file;
- whether redistribution of source/derivatives is permitted; and
- verifying task/agent identity and status.

Allowed by default: CC0/public domain and project-original assets. CC BY 4.0 and permissive software-style licenses are allowed when the ledger, retained license, attribution, and redistribution checks pass; this verification does not require preference approval. Noncommercial, no-derivatives, unclear, ripped, proprietary-without-grant, or share-alike assets are excluded unless the owner explicitly approves a legal exception. “Free to download” is not a license.

The build generates a third-party notices/credits artifact from the ledger and fails if a packaged logical asset lacks an accepted license record.

## Import manifest

Each logical runtime asset declares:

- source and derived path/hash;
- expected Godot resource type;
- import preset and relevant options;
- mesh/material/texture/animation budgets;
- required names/clips/sockets;
- LOD/VAT relationships;
- content consumers and fallback;
- license record ID; and
- validation captures where applicable.

Agents never fix a broken reference by inserting a raw path into gameplay content. They update the asset manifest and rerun import/validation.

## Import and derivation pipeline

1. Acquire original into quarantined source area with license/hash.
2. Review license and mark allowed before adaptation.
3. Normalize/adapt in pinned tools; retain editable source.
4. Export deterministic `.glb`, textures, audio, vectors, or font assets.
5. Generate LODs/VAT/atlases/derived assets through repository scripts.
6. Import with committed Godot sidecar configuration.
7. Run structural, budget, name, material, animation, asset-manifest, and license validators.
8. Render canonical top-down previews and representative composition captures.
9. Mark the logical asset accepted for content use when every automated and capture gate passes.

When multiple candidates pass, agents apply the deterministic acquisition and selection ranking in the [Autonomous Agent Execution Protocol](./114-autonomous-agent-execution-protocol.md#autonomous-asset-and-presentation-selection). Human taste is not a prerequisite for representative M2–M4 assets.

Generated outputs carry source/generator hashes. CI detects stale output; it does not silently regenerate large binaries in a validation-only job.

## Asset acceptance gates

- Correct scale, pivot, facing, clips, sockets, and no unexpected nodes.
- Meets category triangle/material/texture/memory budgets.
- Top-down silhouette remains distinct at Steam Deck scale amid representative density.
- Gameplay footprint overlay corresponds visibly.
- Materials remain distinguishable under lighting and accessibility modes.
- Animation/VAT has no visible popping or root drift.
- License and attribution are complete.
- Asset has a fallback and every content reference resolves.
- Representative scene remains within GPU/frame/memory budgets.

## Build and repository behavior

- `assets-runtime` and manifests are included according to committed export presets; source masters are excluded from release packages.
- Godot import is run headlessly in CI before export; official Godot supports a headless import command. [Godot command-line documentation](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)
- Release build fails on import warnings promoted by project policy, missing resources, stale derivatives, unapproved licenses, or budget errors.
- No export credential, store secret, or signing key lives beside asset sources.

## Verification

- Manifest/actual import audit on every change.
- Top-down contact-sheet generation for every category and variation.
- Animation comparison fixtures for every crowd clip and boss/mech state.
- Grayscale/color-vision/icon-size matrices for resource and threat identities.
- Audio loudness, clipping, loop, and missing-caption scans.
- License coverage report must be 100% for packaged assets.
- Steam Deck representative composition validates actual imported assets, not proxies, before art direction is production-proven.

## Related documents

- [Presentation and Rendering](./30-presentation-and-rendering.md)
- [Audiovisual Feedback](./31-audiovisual-feedback.md)
- [Content Data and Validation](./40-content-data-and-validation.md)
- [Build, Dependencies, and Release Operations](./100-build-dependencies-and-release-operations.md)
- [RES-005 — Free Asset Strategy](../research/RES-005-free-asset-strategy.md)
