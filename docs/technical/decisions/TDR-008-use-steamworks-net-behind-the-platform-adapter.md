---
doc_id: TDR-008
title: Use Steamworks.NET Behind the Platform Adapter
status: accepted
authoritative: false
validation: windows-linux-steam-sandbox-and-offline-adapter-tests
---

# TDR-008 — Use Steamworks.NET Behind the Platform Adapter

## Decision

Use the non-Unity **Steamworks.NET Standalone 2025.164.1** release, which wraps Valve Steamworks SDK 1.64, for the initial C# Steam integration. Pin its source/binaries and license record exactly. Place every Steamworks.NET and Valve SDK type behind `CMP-PLT-001`; no simulation, content, persistence-domain, or UI contract exposes third-party types.

Use Valve's matching Windows `steam_api64.dll` and Linux `libsteam_api.so` redistributables in Steam-enabled packages. Development/internal builds default to the local adapter and remain fully playable without Steam.

## Context

Godot has no native Steamworks integration, while the project uses C# and needs only narrow initialization, cloud, language/controller hints, overlay/lifecycle, and future optional achievements. Steamworks.NET publishes a Standalone package for non-Unity projects, is MIT-licensed, and release 2025.164.1 carries the SDK 1.64 update/fixes. Valve documents that the matching native redistributable must ship beside the executable and that Steam initialization can legitimately fail when Steam or App ID context is absent.

## Considered alternatives

### Direct P/Invoke over Valve's C API

This minimizes wrapper code at runtime but transfers API marshalling, callbacks, platform binaries, SDK updates, and safety testing to the project. It provides no benefit for the narrow initial capability set.

### GodotSteam

GodotSteam is a credible Godot-focused integration and is listed by Valve as a third-party Godot option. It was not selected because this project already uses C#, wants a platform boundary independent of scene/GDScript conventions, and does not need engine-module ownership for its small service surface.

### Facepunch.Steamworks

This is another credible managed wrapper. Steamworks.NET was selected because it exposes a documented non-Unity Standalone distribution and maps closely to Valve's SDK/versioning. The adapter makes later replacement bounded if platform tests disprove the choice.

### No Steam API integration

The game can ship on Steam without most API features, and local play remains mandatory. However, the accepted cloud-sync/platform scope benefits from one managed wrapper rather than custom depot-only behavior.

## Consequences

- Steamworks.NET and SDK versions upgrade together through a dependency work item with Windows/Linux smoke, callback, cloud, offline, and package inventory evidence.
- Native binaries are treated as platform artifacts with Valve SDK provenance/redistribution records, not generic freely licensed assets.
- `steam_appid.txt` may be generated only for authorized local/sandbox development, is ignored by source control, and is excluded from every depot/release package.
- Steam callbacks are pumped on the application/UI frame while initialized, including during simulation pause; callbacks enqueue typed adapter results and never mutate gameplay directly.
- Initialization failure selects the unavailable/local path and does not produce a fatal boot error.
- Steam Cloud transfers complete allowed artifacts through the persistence/platform contracts; Steamworks.NET does not gain write access to simulation or profile state.

## Validation and reversal signals

- Prove Windows x86-64 and Steam Deck/Linux x86-64 initialization, shutdown, callbacks, overlay/lifecycle, and cloud conflict fixtures in an authorized sandbox.
- Prove the same Steam-enabled package launches, plays, and saves locally when Steam is unavailable.
- Validate matching native binary architecture/version and exact package contents.
- Replace the wrapper only if pinned Godot/.NET exports cannot load or reliably run it, a security/license issue is unresolved, or the adapter cannot express an accepted capability. Compare the replacement behind the same fake/contract suite and record a superseding TDR.

## Official references

- [Steamworks.NET releases](https://github.com/rlabrecque/Steamworks.NET/releases)
- [Steamworks.NET license](https://raw.githubusercontent.com/rlabrecque/Steamworks.NET/2025.164.1/LICENSE.txt)
- [Valve Steamworks API overview and native redistribution](https://partner.steamgames.com/doc/sdk/api?language=english)
- [Valve Steamworks SDK](https://partner.steamgames.com/doc/sdk?language=english)

## Specification links

- [Persistence and Platform Services](../70-persistence-and-platform-services.md)
- [Build, Dependencies, and Release Operations](../100-build-dependencies-and-release-operations.md)
- [Component, Contract, and Schema Registry](../115-component-contract-and-schema-registry.md)
- [TDR-004 — Offline-first client](./TDR-004-use-an-offline-first-client-without-a-game-backend.md)

## Supersedes / superseded by

- Supersedes: none.
- Superseded by: none.
