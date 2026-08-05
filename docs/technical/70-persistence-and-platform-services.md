---
doc_id: TDD-PERSISTENCE
title: Persistence and Platform Services
status: active
authoritative: true
---

# Persistence and Platform Services

## Purpose

This document defines the local profile, settings, run history, pending settlements, recovery snapshots, atomic writes, migrations, corruption recovery, Steam Cloud, platform abstraction, and failure behavior.

## Persistence scopes

| Artifact | Purpose | Cloud | Lifetime |
| --- | --- | --- | --- |
| Profile | banked Hyper Gold, purchases, owned/active ranks, unlocks, records, history, tutorial/notification flags | yes | permanent |
| Settings | display, audio, accessibility, controls, glyph preference | normally yes, with device-local exceptions | permanent |
| Pending settlement | idempotent extracted reward awaiting confirmed profile persistence | no; local recovery-critical | until committed |
| Run recovery | resumable active run after crash/termination | no | until terminal/abandon/incompatibility |
| Diagnostic logs | structured local technical and balance records | no | bounded retention |
| Crash package | failure metadata and optional dump | no unless user explicitly shares | bounded retention |

Initial release supports one active local profile. Reset Progress is a destructive confirmed operation that archives the old profile before creating a fresh one. Multiple named profiles are outside initial scope but the envelope includes a profile ID so future expansion does not reuse identity.

## Local file layout and encoding

The Godot integration resolves one platform-appropriate writable user-data root and passes its absolute path into Persistence. Persistence never derives paths from usernames, environment expansion, current working directory, or content IDs.

```text
profile.json
profile.backup.1.json
profile.backup.2.json
profile.backup.3.json
settings.portable.json
settings.portable.backup.json
settings.device.json
settings.device.backup.json
pending-settlement.json
recovery.json.br
archives/
logs/
diagnostics/
```

Profile, settings, settlement, and migration archives are uncompressed canonical UTF-8 JSON. Recovery is the complete canonical JSON envelope compressed with the .NET built-in Brotli encoder at quality `4`, format version `brotli-1`; its checksum covers the uncompressed canonical payload. Compression runs only from an immutable captured buffer off the authoritative tick. A future codec change is a recovery-schema migration/rejection change, not an implementation-local substitution.

Temporary writes use an unpredictable file name in the same directory and never match a cloud allowlist. Archive names contain artifact kind, schema version, UTC timestamp, revision, and a collision-safe suffix. Retention and cleanup operate only on validated files inside these exact owned directories.

## Save envelope

Save/profile/settings/recovery JSON uses the canonical built-in codec defined in [Content Data and Validation](./40-content-data-and-validation.md#json-codec-and-schema-baseline). Persistence owns the durable DTOs and migration adapters; it does not serialize live Godot objects, arbitrary runtime types, or unvalidated dictionaries.

Every persisted artifact begins with fields that can be read before payload migration:

- artifact kind and stable profile ID where applicable;
- save schema version;
- product/build version;
- content bundle and compatible-system versions where relevant;
- monotonic profile revision;
- globally unique mutation/settlement identity;
- device installation ID;
- UTC write time plus monotonic session sequence for local ordering;
- payload byte length; and
- checksum of canonical payload bytes.

Unknown future versions are never deserialized into older models. The original file is preserved and the user receives an actionable incompatibility message.

## Profile payload

The profile contains only stable IDs and primitive structured values:

- banked Hyper Gold;
- owned and active rank for each PowerUp plus actual fixed purchase records needed for exact refund audit;
- owned option unlock IDs;
- default selected mech and interface preferences that are account-like rather than device-like;
- permanent unlock/seen/codex flags accepted by gameplay content;
- aggregate records and achievements earned locally;
- bounded run-history summaries;
- pending unlock-notification acknowledgement IDs;
- tutorial steps seen/completed and reset generation; and
- migration/audit metadata.

Derived values, display strings, weapon definitions, prices, unlocked-pool expansions, and asset paths are not stored. They resolve from the current content registry, with migration/tombstone behavior for retired IDs.

Run history retains the most recent 100 manifests by default plus aggregate lifetime records. History entries contain the results fields required by the interface, seed/version identity, and final loadout; they do not contain entity snapshots or input replay.

## Settings split

Settings are divided by sync behavior:

- **Portable:** volumes, subtitles, accessibility presentation, bindings expressed in portable logical input form, HUD/menu scale, glyph preference, tutorial preference.
- **Device-local:** monitor, window position, selected resolution, render scale, quality override, output-device identifiers, diagnostic/developer flags.

Cloud synchronization transports only portable settings or ignores unavailable bindings/devices safely. A settings parse failure loads defaults, preserves the corrupt file, and never prevents profile loading.

## Atomic write protocol

All critical writes follow this order:

1. serialize and validate a complete new envelope in memory or a dedicated temporary stream;
2. write to a uniquely named temporary file in the same filesystem/directory;
3. flush file contents and, where supported, directory metadata;
4. reread/verify header, length, checksum, and schema parse;
5. rotate the prior primary into backup history without deleting the last known-good artifact first;
6. atomically replace/rename temporary to primary;
7. confirm the primary can be reopened; and
8. only then acknowledge the transaction to UI or clear a pending settlement.

Keep three rotating profile backups and one pre-migration archive per schema upgrade. Settings keep one backup. Cleanup never removes the only valid file.

Writes are serialized through one persistence coordinator. Concurrent purchase, result, Steam callback, and shutdown requests queue and coalesce only when their mutation identities remain distinct and acknowledged.

## Persistent transaction model

Profile-changing commands create a new immutable profile snapshot with:

- expected prior revision;
- unique mutation ID;
- exact debit/credit and ownership changes;
- source action/result ID; and
- resulting revision.

The persistence coordinator rejects stale expected revisions and recognizes already-committed mutation IDs. UI success occurs after durable local commit, not when the in-memory value changes.

PowerUp refund returns the exact accepted fixed values represented by current catalog/validated purchase audit. Option unlocks remain nonrefundable. Checked arithmetic prevents overflow or negative balances.

## Extraction settlement

Successful extraction creates a pending settlement before mutating the profile.

1. Write pending record containing settlement ID, profile ID/revision, run result hash, and exact Hyper Gold credit.
2. Create and atomically persist the new profile revision containing the settlement ID in a bounded applied-settlement ledger.
3. Verify the profile.
4. Delete or mark committed the pending record.

On startup:

- if the settlement ID appears in profile, clear the pending record without crediting again;
- if it does not and the prior revision/result validate, retry exactly once through the same idempotent path;
- if data conflicts, preserve everything and present recovery rather than guessing.

Failed/abandoned runs produce history but no credit settlement.

## Run recovery

The initial release supports automatic crash/suspension recovery, not manual save slots or player-triggered mid-run saving.

### Capture cadence

- every 30 active-simulation seconds;
- after generation and initial deployment state is committed;
- after a fabrication or relic transaction before resuming;
- on general pause, focus loss, or operating-system suspension; and
- before orderly application shutdown if a run is active.

The main thread captures a consistent immutable simulation/application snapshot at a tick boundary. Serialization/compression and file writing may run in the background from that immutable capture. At most one write is active and one newer capture waits; intermediate periodic captures may be superseded, while transaction/focus-loss captures request a flush.

### Recovery payload

Include run/session identity, seed and generated manifest/checksum, all authoritative entity stores, static/mutable site state, spatially reconstructable state, player/loadout/inventory, director schedule/queues, boss/weapon behavior states, RNG streams, tick, pause reasons safe to restore, discovery/map/waypoint, ledger/statistics, and required content/build versions.

Do not serialize presentation nodes, pooled handles, particles, audio voices, or UI animation. Presentation rebuilds from the restored snapshot.

### Restore policy

- Offer Continue Deployment only when schema/content/system compatibility declares the snapshot safe.
- Restore paused to Status with zero wall-time catch-up and a clear recovery notice.
- Do not bank or alter unsecured Hyper Gold during capture/restore.
- Delete recovery after terminal settlement, confirmed abandonment, or explicit discard.
- Incompatible/corrupt recovery may be archived and discarded; it never blocks the permanent profile.
- Recovery does not synchronize through Steam Cloud in the initial release, so a run cannot move between devices.

## Migrations

- Every save schema version has a one-way migration to the next; loading applies a contiguous sequence.
- Migrations are pure transformations over validated prior payloads and do not query mutable online state.
- Each step records prior/new version and migration ID, then validates the new schema and semantic invariants.
- Migration writes a new primary only after preserving the original archive.
- Released migrations are immutable. Fixes add a new version rather than silently changing historical behavior.
- Content ID retirement uses explicit mappings or safe removal semantics per field; unknown owned progression content is preserved in an `unresolved` ledger when loss would be harmful.

Downgrading a save is unsupported. A newer-version save remains untouched.

## Corruption recovery

Load candidates in order: primary, newest valid backup, older backups, pre-migration archive. Validate header, length, checksum, JSON schema, references, nonnegative currencies/ranks, catalog caps, and transaction consistency.

If a backup loads:

- preserve the corrupt primary with timestamp;
- enter a blocking local recovery screen before Hangar rather than silently using the backup;
- explain that a backup is available and state its revision/write time and any potentially missing recent progress; and
- offer **Continue with backup**, **Export diagnostics**, and **Exit**. Continue archives the corrupt primary and atomically writes the validated backup as the new primary before play.

If none load, offer archive-and-create-fresh or exit; never silently erase files.

## Steam platform adapter

[TDR-008](./decisions/TDR-008-use-steamworks-net-behind-the-platform-adapter.md) fixes Steamworks.NET Standalone 2025.164.1 with Steamworks SDK 1.64 as the initial provider. Only `CMP-PLT-001` references wrapper/native SDK types. The provider initializes after coherent local persistence is available, pumps callbacks on the application/UI frame even while gameplay is paused, converts them to typed adapter results, and shuts down best-effort after pending local work is safe. Failure to initialize reports unavailable and selects the local path.

Expose narrow capabilities:

- initialization and availability;
- user/language/controller/glyph hints where supported;
- cloud read/write/status/conflict callbacks;
- achievements if later enabled;
- overlay and graceful shutdown events; and
- diagnostic platform build identity.

The local adapter implements identical contracts without Steam. Simulation and content assemblies do not reference Steam types.

## Steam Cloud conflict policy

The cloud artifact includes profile ID, revision, mutation ancestry tail, device ID, and write time.

- Identical hash: no action.
- One revision is a known descendant of the other: keep the descendant and preserve the older local backup.
- Divergent ancestry: do not field-merge currencies, refunds, unlocks, or history automatically. Present both summaries with write time, banked Hyper Gold, major unlock counts, run count, and device; let the player choose while archiving the unselected file.
- Offline play continues locally. Upload retries do not block save acknowledgement.

Cloud failures never roll back a durable local transaction.

## Privacy and security

- No personally identifying account data is required in the save.
- Installation/profile IDs are random local identifiers.
- Paths, usernames, Steam identifiers, and machine details are redacted from shareable diagnostic packages by default.
- Checksums detect accidental corruption, not malicious editing.
- Save parsing uses size/depth/count limits and rejects path/type injection.
- Steam credentials and secrets are never stored in saves or source.

## Performance budgets

- Profile/settings load and migration: under 250 ms p95 on Steam Deck for normal files.
- Critical profile atomic write: under 250 ms p95 without blocking active simulation; UI may show a short committing state.
- Recovery capture main-thread work: under 2 ms p95; background serialization may take longer.
- Recovery artifact target: under 16 MiB compressed for the accepted peak run.
- No unbounded history, diagnostic, backup, or transaction ledger growth.

## Verification

- Golden save fixtures exist for every released schema version and migration path.
- Fault injection interrupts every atomic-write step and proves a prior or new valid artifact remains.
- Settlement tests prove no loss/double credit under crash/retry.
- Recovery round trips compare canonical simulation checksums immediately and after additional ticks.
- Cloud tests cover identical, ancestor, descendant, divergent, offline, interrupted upload, and user-choice flows.
- Fuzz/limit tests feed malformed, huge, deeply nested, unknown-field, invalid-ID, and corrupt-checksum artifacts.
- Windows and Steam Deck verify actual filesystem replace/flush behavior and cloud sandbox operation.

## Related documents

- [Runtime Architecture](./10-runtime-architecture.md)
- [Mining, Fabrication, and Progression Runtime](./24-mining-fabrication-and-progression-runtime.md)
- [UI, Input, and Accessibility](./60-ui-input-and-accessibility.md)
- [Build, Dependencies, and Release Operations](./100-build-dependencies-and-release-operations.md)
