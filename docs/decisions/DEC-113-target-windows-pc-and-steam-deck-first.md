---
doc_id: DEC-113
title: Target Windows PC and Steam Deck First
status: accepted
authoritative: false
validation: device-and-usability-test
---

# DEC-113 — Target Windows PC and Steam Deck First

> **Completion note:** DEC-127 fixes first-playable active-play and menu bindings, controller navigation grammar, focus behavior, and 1920×1080 / 1280×800 interface reflow. Ultrawide, performance, store features, and additional platforms remain open.

## Decision

The initial release target is **Windows PC through Steam**, with **Steam Deck treated as a first-class target** rather than a later compatibility exercise.

### Display baseline

- The game uses landscape presentation.
- `1920 × 1080` at `16:9` is the desktop reference canvas.
- Every gameplay and interface screen fully supports `1280 × 800` at `16:10` for Steam Deck without hiding, truncating, or removing required information.
- HUD, fabrication, pause map, relic resolution, hangar, progression, results, settings, and tutorial presentation all reflow or scale for both reference aspect ratios.
- At `1280 × 800`, the smallest required interface characters never fall below 9 pixels in height, with 12 pixels or more as the normal target. Text-size and contrast settings remain required accessibility work.
- Exact ultrawide behavior and additional desktop resolutions remain presentation work; they cannot invalidate the fixed-world-scale combat rule.

### Input baseline

- Keyboard-and-mouse and gamepad are both complete supported control methods.
- Every gameplay, menu, map, fabrication, settings, and results function is accessible with a gamepad alone using the default configuration.
- Active-play movement supports keyboard directional input and analog left-stick input under the established direct-movement rules.
- Prompts and glyphs switch automatically to match the most recently active supported input family.
- The standard flow avoids required free-text entry. If a later feature requires text, it must support controller-accessible entry on Steam Deck.

### Initial scope boundary

Native console, mobile, touch-first, portrait, macOS, and non-Steam Linux release requirements are outside the initial target. They may be added later through explicit platform and interface decisions. Steam Deck touchscreen input is optional and cannot be required.

## Status

Accepted as the initial platform, aspect-ratio, and input-support baseline. DEC-127 later defines the first-playable device mappings and UI layouts. Ultrawide behavior, graphics settings, performance targets, store features, and additional platforms remain later work.

## Rationale

The game already uses low-input movement-centered combat but contains fabrication, maps, resource surveys, and persistent HUD information that must be designed for both desk and handheld use from the beginning. Treating Steam Deck as a target forces controller-complete navigation and small-screen legibility before interface assumptions harden.

Windows covers the large majority of surveyed Steam systems, and 1920×1080 remains the most common primary display resolution. Valve's Steam Deck compatibility guidance requires complete access through the default controller configuration, support for the Deck's display resolution, and legible interface text at 1280×800.

## Consequences

- Every wireframe and usability test includes both 1920×1080 desktop and 1280×800 handheld captures.
- No hover-only, right-click-only, touch-only, or mouse-only action may be required.
- Fabrication comparisons and the pause map must remain useful without a cursor.
- Input-remapping and automatic glyph behavior are part of the initial settings scope.
- Mobile-style touch controls and portrait layouts do not constrain the initial HUD or camera.
- Platform expansion requires a separate review of pause, suspension, safe areas, text, input, performance, and store requirements.

## Research basis

- [Steam Hardware & Software Survey](https://store.steampowered.com/hwsurvey/) — current official survey used for Windows share and common desktop resolution.
- [Steam Deck and Steam Machine Compatibility Review](https://partner.steamgames.com/doc/steamhardware/compat?l=french&language=english) — official controller, resolution, and legibility requirements and recommendations.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Open Questions](../open-questions.md)
- [RES-005 — Free-asset strategy](../research/RES-005-free-asset-strategy.md)

## Supersedes / superseded by

Completes the initial target-platform and reference-display direction in OQ-011 without selecting the visual asset medium or art style.
