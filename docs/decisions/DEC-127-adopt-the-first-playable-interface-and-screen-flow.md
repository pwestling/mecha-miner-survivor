---
doc_id: DEC-127
title: Adopt the First-Playable Interface and Screen Flow
status: accepted
authoritative: false
validation: prototype-device-and-usability-test
---

# DEC-127 — Adopt the First-Playable Interface and Screen Flow

## Decision

Adopt the [Interface, Screen Flow, and Information Architecture](../73-interface-screen-flow-and-information-architecture.md) as the authoritative first-playable specification for the active HUD, opening survey, paused run console, fabrication, full map, relic resolution, results, hangar, default inputs, controller navigation, confirmation hierarchy, and reference-resolution behavior.

The accepted structure is:

- a sparse edge-anchored combat HUD with contextual mining and warning information;
- one shared fully paused run-console shell with direct entry to Status, Fabrication, and Map;
- a three-column desktop fabrication layout that reflows to a controller-friendly detail drawer at 1280×800;
- one user waypoint on explored terrain, shown on maps and through a distinct active-play bearing;
- a blocking side-by-side relic comparison that communicates behavior, weapon compatibility, replacement, and sale outcomes;
- four results pages covering Summary, Build and Combat, Mining and Economy, and Exploration; and
- a Hangar flow separating deployment, refundable PowerUps, nonrefundable option unlocks, records, and settings.

Use `WASD` or arrows and the left stick or directional pad for movement. Use `Esc` / Menu for Status, `Tab` / face-north for Fabrication, and `M` / View for Map. Every binding remains remappable, and there is no required attack, aim, interact, mining, dodge, or utility button.

Every standard run selects three distinct relics without replacement from the currently unlocked pool. Relics have no dedicated cache defenders and no global through-fog signal. Every initial relic has a fixed 150-common-ore sale value; replacing an installed relic automatically sells the displaced relic for the same amount after confirmation.

## Status

Accepted as the initial interaction and information baseline. Final graphic styling, animation, audio, accessibility ranges, localization, ultrawide behavior, and camera world scale remain separate presentation work.

## Rationale

The game asks the player to track danger, seven possible radar bearings, irreversible equipment slots, four run materials, common ore, unsecured Hyper Gold, mining state, bosses, and exploration while moving continuously. Persistently displaying every exact value would obscure the battlefield, while hiding them would produce avoidable menu checking and accidental build commitments.

The adopted split keeps immediate state compact and moves arithmetic into a complete pause. Direct shortcuts preserve unlimited fabrication and make the map practical without creating multiple inconsistent pause models. A stable controller grammar and 1280×800 reflow make Steam Deck a real first target rather than an eventual scaling test.

Relic selection without replacement ensures the map's three caches offer three different decisions. A flat 150-ore sale is large enough to fund meaningful development but remains below the 300-ore radar or a boss's 300-ore burst; it is also simple enough to understand during a replacement decision.

## Consequences

- OQ-031 is resolved for the first playable; visual production and accessibility tuning continue in their dedicated questions.
- OQ-022 is resolved: the opening survey is non-modal, appears after 0.5 seconds, stays expanded for 12 active seconds, and remains reviewable through pause.
- OQ-027 is resolved: cache selection, duplicates, signaling, guarding, sale values, compatibility previews, and replacement presentation are fixed.
- OQ-014 is resolved at the gameplay-specification level because fabrication now has complete browsing, comparison, purchase, branch, full-slot, and confirmation behavior.
- The full map gains one player waypoint without revealing undiscovered content or providing pathfinding.
- Active radar overlap has a deterministic fanning and clustering rule.
- Results exposes settlement before unlock notifications, so banked, forfeited, spent, and discarded resources cannot be confused.
- Interface implementation must support full gamepad operation and responsive composition rather than relying on a cursor-scaled desktop layout.

## Specification Links

- [Interface, Screen Flow, and Information Architecture](../73-interface-screen-flow-and-information-architecture.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Mech Relics](../67-mech-relics.md)
- [Initial Relic Catalog](../69-initial-relic-catalog.md)
- [Open Questions](../open-questions.md)

## Supersedes / Superseded By

This completes presentation and navigation details left open by DEC-015, DEC-017, DEC-029, DEC-089, DEC-099, DEC-100, DEC-104, DEC-113, and DEC-118 while preserving their underlying rules.

It supplies a fixed 150-ore value and without-replacement selection for the initial relic pool. It does not change any relic effect, weapon recipe, resource payout, PowerUp price, extraction rule, or pause behavior.
