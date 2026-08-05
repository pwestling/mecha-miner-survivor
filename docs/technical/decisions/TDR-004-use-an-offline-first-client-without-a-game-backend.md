---
doc_id: TDR-004
title: Use an Offline-First Client Without a Game Backend
status: accepted
authoritative: false
validation: offline-and-platform-service-tests
---

# TDR-004 — Use an Offline-First Client Without a Game Backend

## Decision

Build the initial game as a fully offline-capable single-player client. Do not require a game server, player account service, remote configuration, online economy, authoritative telemetry service, or network connection for boot, progression, deployment, play, extraction, or saving.

Integrate Steam through a narrow platform-services adapter. Use Steam Cloud for save synchronization when available, while retaining a complete local-save path and understandable conflict recovery. Internal demos use local services and require no Steam client.

## Rationale

No accepted gameplay feature needs a backend. Offline-first behavior reduces failure modes, security surface, operational cost, build complexity, and implementation dependencies. It also makes deterministic fixtures and agent-run test environments straightforward.

## Consequences

- Local persistence is authoritative on a device; cloud synchronization transports complete versioned save artifacts rather than partial live state.
- Loss of Steam connectivity cannot block play or corrupt progression.
- Internal analytics and balance capture write local structured records. Any later external telemetry must be opt-in where required, privacy-reviewed, and accepted through a new TDR.
- Steam achievements, leaderboards, workshops, remote content, daily seeds, multiplayer, and cross-platform accounts are outside the initial technical contract.
- Platform calls use interfaces with local no-op or fake implementations so simulation and UI tests do not launch Steam.
- No secrets or service credentials are required in the game client or repository for the internal demo.

## Specification links

- [Technical Foundation](../00-technical-foundation.md)
- [Runtime Architecture](../10-runtime-architecture.md)
- [Resources, Crafting, and Progression](../../60-resources-crafting-progression.md)
- [DEC-113 — Target Windows PC and Steam Deck First](../../decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
