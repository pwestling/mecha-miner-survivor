---
doc_id: DEC-104
title: Show a Compact Survivor-Like Active HUD
status: accepted
authoritative: false
validation: usability-and-playtest
---

# DEC-104 — Show a Compact Survivor-Like Active HUD

> **Completion note:** DEC-127 fixes HUD regions, contextual feedback, radar overlap, default inputs, paused detail, and reference-resolution behavior left open here. Final audiovisual styling and accessibility ranges remain separate work.

## Decision

Standard active play uses a compact persistent HUD that exposes:

- the upward-counting active timer, next scheduled boss threshold, and 35:00 extraction threshold;
- current Hull Integrity through a continuously readable gauge;
- carried common ore, each carried specialized material, and unsecured Hyper Gold;
- the four weapon slots, three utility slots, and active relic, including empty slots;
- total enemy defeats; and
- the compact north-up exploration minimap.

Mining progress, outside-zone grace and decay, geode resonance, Hyper Gold beacon state, repair gains, and temporary field effects appear contextually when relevant. The active HUD has no XP bar or player-level display because the game has no XP progression.

Pause provides the exact numeric and statistical detail defined by DEC-099 even if the active HUD uses compact gauges, icons, or abbreviated counts. Exact placement, visual grouping, scale, iconography, and audiovisual presentation remain open.

## Status

Accepted as the active-play information baseline.

## Rationale

The reference game's persistent run information reduces menu checking during dense automatic combat. This game needs the same immediacy, but replaces XP information with the resources, mining state, map knowledge, and extraction schedule that actually drive its decisions.

Showing empty and occupied loadout slots makes irreversible fabrication commitments and build maturity legible without opening the menu.

## Consequences

- A player can judge whether to route, mine, or fabricate without pausing only to inspect basic state.
- The HUD cannot reveal undiscovered terrain, deposits, relics, or exact radar targets.
- Non-color identity cues are required for specialized material counts and radar indicators.
- Accessibility settings may resize, reposition, simplify, or restyle information but cannot remove access to required state.

## Specification links

- [Core Gameplay Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Glossary](../glossary.md)

## Supersedes / superseded by

Applies the HUD portion of [DEC-096](./DEC-096-use-vampire-survivors-as-the-default-precedent.md) and complements the pause and results requirements in [DEC-099](./DEC-099-use-single-player-pause-and-results-flow.md).
