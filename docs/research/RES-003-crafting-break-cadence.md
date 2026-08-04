---
doc_id: RES-003
title: Crafting-Break Cadence
status: complete
authoritative: false
---

# RES-003 — Crafting-Break Cadence

## Research question

Should paused crafting be opened on a schedule, whenever the player requests it, at specific map locations, or through a hybrid trigger?

## Retrieval date

2026-08-01.

## Sources

- [*Vampire Survivors* level-up mechanics](https://vampire.survivors.wiki/w/Level_up) — community-maintained description of an event-driven upgrade pause after reaching an XP threshold.
- [*Brotato* official Steam store page](https://store.steampowered.com/app/1942280/Brotato/) — developer-controlled description of collecting materials during combat and buying items in a shop between waves.
- [*Deep Rock Galactic: Survivor* mid-dive upgrades](https://deeprockgalactic.wiki.gg/wiki/Survivor%3AMid-dive_Upgrades) — official wiki description of spending mined Gold and Nitra on upgrades between dive stages.
- [*Deep Rock Galactic: Survivor* official Steam store page](https://store.steampowered.com/app/2321470/Deep_Rock_Galactic_Survivor/) — developer-controlled description of its auto-shooter, mining, staged mission, and extraction structure.

## Observed cadence patterns

### Resource-threshold event

*Vampire Survivors* pauses when XP reaches a level threshold, then presents upgrade choices. This keeps power growth close to the activity that earned it, but the frequency varies with kill and collection rate. This game has no XP, and immediately triggering crafting whenever a recipe becomes affordable could recreate a renamed level-up cadence rather than reward saving and recipe planning.

### Scheduled break between waves

*Brotato* separates combat into waves and opens its item shop between them. This produces predictable action and planning phases and prevents the shop from interrupting movement. It depends on a strongly segmented wave structure, whereas this game currently has continuous horde pressure punctuated by interval bosses.

### Location or stage-transition shop

*Deep Rock Galactic: Survivor* lets players spend mined Gold and Nitra between dive stages. This gives mined resources a clear delayed use and makes the shop a substantial reset point. Its cadence is much coarser than an in-level crafting system and relies on distinct stage transitions.

## Candidate models for this game

| Model | Agency | Pacing effect | Main benefit | Main risk |
| --- | --- | --- | --- | --- |
| Fixed-time automatic break | Low | Predictable full interruption | Simple, regular relief and upgrade curve | Can interrupt a boss, threat beacon, or other tense moment arbitrarily |
| Unrestricted on-demand pause | High | Player fragments action at will | Maximum control over spending and accessibility | Pause can become a panic tool; optimal play may involve frequent menu checks |
| Location-bound fabricator | Medium | Adds another route destination | Reinforces exploration and spatial planning | Competes with mining destinations and may starve unlucky routes of upgrades |
| Automatic post-boss break | Low to medium | Relief follows a threat peak | Strong dramatic punctuation and existing cadence | A weak build may need upgrades before it can defeat the boss |
| Limited milestone-unlocked window | High within a limit | Player chooses when to consume a finite break | Combines predictable availability with controlled agency | Adds a window charge/state and can encourage hoarding |

## Initial recommendation

Prototype a limited fabrication charge available before each interval boss, then invoked once by the player at a chosen moment. It keeps resources as the source of upgrades: the milestone grants access to the workshop, while mined materials determine what can be crafted.

This hybrid offers several advantages:

- The number of pauses is bounded, preventing unrestricted pause abuse.
- The player can delay the break until enough useful resources have been mined.
- The system does not add another mandatory map destination on top of mining points.
- Boss cadence and crafting cadence become mutually legible parts of the timer.
- A fabrication-window indicator gives the player advance planning information.
- One session can support multiple purchases, allowing the player to convert a meaningful stockpile into a boss-ready build.

The charge can be available from deployment for the first interval and replenished at a clearly signaled milestone before each later boss. Unlocking it only after boss defeat is no longer recommended because it can withhold the power needed for that boss.

## Resulting decision

The design owner selected unrestricted on-demand access for the initial implementation and playtest in [DEC-007](../decisions/DEC-007-unlimited-on-demand-fabrication.md). This differs from the initial limited-charge recommendation and deliberately tests whether resource costs alone are a sufficient access constraint. The limited-window analysis remains relevant as a fallback if playtesting shows excessive interruption or pause abuse.

## Remaining design tests

- Ensure the first crafting opportunity arrives before the initial weapon becomes inadequate.
- Decide exactly how far before each boss the charge becomes available and whether the first charge exists at deployment or after an early mining milestone.
- Decide whether unused windows stack or are replaced.
- The level timer and entire gameplay simulation are now decided to freeze during fabrication, preventing free survival or mining progress.
- Warn the player before the last opportunity to spend ordinary resources, which are lost at run end.
- Test whether saving resources for a later recipe is meaningfully different from buying upgrades at every opportunity.

## Resulting links

- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [DEC-006 — Pause combat for crafting and discard unspent ordinary resources](../decisions/DEC-006-paused-crafting-and-run-resource-reset.md)
- [DEC-007 — Allow unlimited on-demand fabrication access](../decisions/DEC-007-unlimited-on-demand-fabrication.md)
