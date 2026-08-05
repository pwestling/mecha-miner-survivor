---
doc_id: TDD-AUDIOVISUAL-FEEDBACK
title: Audiovisual Feedback
status: active
authoritative: true
---

# Audiovisual Feedback

## Purpose

This document defines the event-driven audio, music, haptics, captions, prioritization, and accessibility implementation supporting combat and mining readability. Final creative assets remain production content; event semantics and budgets are technical contracts.

## Audio architecture

Use one audio presentation service consuming simulation/presentation events. Gameplay systems never instantiate audio players directly.

Initial mixer buses are:

- Master
- Music
- Ambience
- Player Weapons
- Enemy and Boss
- Mining and Resources
- UI
- Critical Warnings

Each bus exposes user volume; Critical Warnings also respects Master but is not ducked below audibility by ordinary combat. UI preview can audition every bus from Settings.

## Event definitions

An audio event definition contains stable ID, asset variants, bus, priority, spatial mode, concurrency group, per-group limit, minimum replay interval, volume/pitch variation bounds, ducking policy, caption key, and haptic recipe if any.

- Simulation events identify what happened and where.
- The audio service chooses a presentation variant without affecting authoritative randomness.
- Repeated events use deterministic or presentation-random variation independent of gameplay streams.
- An absent asset triggers a diagnostic and optional generic fallback; it never throws into simulation.

## Spatial policy

- World attacks, impacts, mining sites, caches, enemies, and bosses use top-down stereo spatialization with conservative distance falloff.
- Player weapon layers stay readable near center without becoming exhausting at high rates.
- Boss warnings, low-Hull warnings, extraction, UI confirmation, and other critical state use nonspatial or hybrid cues so camera-edge placement cannot make them inaudible.
- Offscreen boss and radar-related direction cues preserve left/right bearing but never communicate a false exact distance.
- Ambience and music remain nonauthoritative and pause/duck according to application state.

## Horde aggregation

Do not emit locomotion or contact ambience independently for hundreds of enemies.

- The audio service derives one or more horde-bed layers from nearby population, mass, speed, and directional distribution.
- Only selected close pass-bys, elite entries, Needler charges, boss actions, and resolved contacts receive discrete voices.
- Ordinary hit and death sounds are sampled or aggregated by a short time/space bucket; damage statistics remain unsampled.
- Aggregation thresholds are presentation settings and do not alter event recording.

## Priority and voice budget

Highest to lowest priority:

1. low-Hull, revival, lethal boss/Needler/beacon warnings;
2. relic/fabrication confirmations and extraction outcome;
3. player damage, health-pack collection, mining state transitions;
4. boss action and elite entrance;
5. major weapon activation and resource payout;
6. ordinary attacks, hits, and deaths;
7. ambience and decorative detail.

Initial total voice limit is 64, with at least eight slots reserved for critical/UI voices and eight for music/ambience layers. Concurrency groups prevent one rapid weapon or horde event from monopolizing the pool. Voice stealing selects lowest priority, farthest/quietest, then oldest.

## Pause and lifecycle

- Full simulation pause freezes gameplay-looping audio, scheduled beat cues, charge loops, and haptic patterns at their logical phase.
- One-shot UI and pause transition sounds continue on the UI clock.
- Music and ambience either pause or apply a low-pass/ducked pause state consistently; they do not imply active combat timing.
- Resuming reconciles long-lived loops from the current snapshot and does not replay every event that occurred before pause.
- Focus loss mutes or ducks according to setting and never advances simulation audio schedules.

## Music state

Music uses authored layers or cues selected by active run phase: orientation, early, mid, mature, final crescendo, boss presence, pause, success, and failure. Boss overlap raises intensity but does not create one music instance per boss.

Transitions use simulation boundary events and freeze with the run where rhythmic timing communicates gameplay. Music does not inspect player health or build power to alter difficulty perception except for the explicit low-Hull warning layer.

## Haptics

Haptic recipes are short, bounded, and priority managed. Initial supported events include player damage, shield negation, revival, mining installment/completion, beacon threshold, boss arrival/major attack, relic decision, and extraction.

- Haptics can be disabled and have an intensity slider.
- Repetitive weapon and ordinary kill events are heavily rate-limited or omitted.
- Critical warnings use distinguishable patterns without requiring haptics for comprehension.
- Device disconnect cancels active patterns safely.

## Captions and visual redundancy

Every gameplay-relevant non-UI sound has an optional caption definition with category, concise localized label, directional indicator when meaningful, and minimum display time. Captions are required for distant boss cries, Needler charge, beacon response, extraction countdown, revival readiness/activation, and any future spoken line.

No rule is communicated only by sound. Conversely, audio should reinforce material/resource identity, warning class, and success/failure without relying on pitch discrimination alone.

## Settings

Provide:

- independent volume sliders for all buses plus mute;
- output-device behavior delegated to the operating system unless Godot exposes safe selection;
- subtitles/captions Off, Critical, or All, defaulting to Critical;
- caption size following UI text scale and a high-contrast background option;
- haptics toggle and 0–100% intensity; and
- background-audio toggle.

## Verification

- Event registry validation proves every referenced audio/caption/haptic ID exists and every critical event has visual redundancy.
- A voice-budget stress test emits maximum weapon, horde, mining, boss, and UI events and proves reserved warnings remain audible.
- Pause fixtures verify loops, War-Drum beats, charges, and music resume at consistent phases.
- Controller tests verify haptic cancellation and intensity.
- Accessibility review verifies caption timing, direction, localization expansion, and no audio-only information.

## Related documents

- [Presentation and Rendering](./30-presentation-and-rendering.md)
- [UI, Input, and Accessibility](./60-ui-input-and-accessibility.md)
- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)
