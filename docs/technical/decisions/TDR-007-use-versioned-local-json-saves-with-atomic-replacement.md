---
doc_id: TDR-007
title: Use Versioned Local JSON Saves With Atomic Replacement
status: accepted
authoritative: false
validation: migration-crash-and-cloud-conflict-tests
---

# TDR-007 — Use Versioned Local JSON Saves With Atomic Replacement

## Decision

Persist the initial single profile and settings as explicit versioned JSON envelopes using atomic temporary-write and replace semantics with rotating backups. Persist optional in-progress recovery snapshots as compressed versioned JSON because they are larger and ephemeral.

Use Steam Cloud to synchronize complete profile artifacts when available. Do not merge arbitrary divergent progression fields automatically.

## Rationale

JSON is inspectable, recoverable, schema-validatable, agent-friendly, and adequate for the small profile. Explicit envelopes and migrations avoid coupling long-lived progress to C# type names or Godot resource serialization. Atomic replacement and backups protect extraction rewards and purchases from interruption.

## Consequences

- Saves never serialize engine objects, CLR type metadata, or direct scene/resource paths.
- The save schema is distinct from content JSON schemas and has sequential migrations.
- Checksums detect corruption or incomplete transport but are not anti-cheat security.
- Players may edit local saves; the initial single-player game does not add encryption or invasive tamper prevention.
- A future cross-platform account or authoritative online economy requires a new persistence/backend decision.

## Specification links

- [Persistence and Platform Services](../70-persistence-and-platform-services.md)
- [TDR-004 — Use an Offline-First Client Without a Game Backend](./TDR-004-use-an-offline-first-client-without-a-game-backend.md)
