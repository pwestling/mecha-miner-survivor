---
doc_id: GDD-MECH-RELICS
title: Mech Relics
status: active
authoritative: true
---

# Mech Relics

## Purpose and player promise

Relics make exploration rewarding even when the player is not searching for a specific crafting material. Finding one presents a run-defining temptation: install a strange new rule that may demand a different play style, or convert the discovery into resources that advance the existing build.

## Loadout rule

The mech has exactly one relic slot. It is separate from:

- The four weapon slots.
- Each weapon's common-ore stat upgrades.
- Each weapon's mutually exclusive specialized-resource branch.
- The mech's three utility slots.
- The mech's inherent trait.

An installed relic applies at the mech or whole-build level. It may affect several weapons, mining, movement, enemy interactions, or another major gameplay rule. It is not attached separately to each weapon.

Relics and their effects are run-local. The installed relic never persists after the run under the standard rules.

## Acquisition

Every standard map contains exactly three relic caches at randomized locations. A cache is a clearly recognizable world object that opens automatically when the mech touches it. Relics are not fabricated, bought from the fabrication catalog, produced by mining, or listed in the geological resource survey.

A fresh profile's cache pool contains Retrograde Engine, Colossus Governor, Event-Horizon Coupler, Fission Seed, and Claim-Jumper Core. Five permanent, nonrefundable Hyper Gold purchases add Ghostline Chassis, Dead-Reckoning Array, War-Drum Oscillator, Redline Crucible, and Sequential Reactor individually. An owned relic cannot be disabled, and unlocking one adds a possible random result rather than guaranteeing it. Exact prices and ownership rules are defined in the [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md).

The three caches are guaranteed to exist but are not guaranteed to be discovered or reached before the run ends. Each run assigns three distinct relics without replacement from the currently unlocked pool. Caches receive no dedicated guards or global through-fog bearing; their tall silhouette, ground emblem, and intermittent vertical signal make them recognizable whenever they enter the gameplay view. Once observed, an unopened cache remains recorded on both maps.

## Install-or-sell choice

When the player finds a relic, they have two established uses:

1. **Install it** in the mech's relic slot and activate its effect.
2. **Sell it** for run resources instead of installing it.

If a relic is already installed, the newly found relic can replace it. Choosing sale converts the new relic into common basic ore and retains the currently installed relic. Installing the new relic ends the previous relic's effect, activates the replacement, and automatically sells the displaced relic for its common-ore value.

Every relic discovery freezes the entire gameplay simulation while this choice is open. Every initial relic has a fixed 150-common-ore sale value. Selling the new relic retains the installed relic and awards 150 ore; installing over an active relic automatically sells the displaced relic for 150 ore. Both outcomes are shown before confirmation. The player must install or sell before active play resumes; there is no relic inventory or deferred decision.

This pause behavior is defined for standard single-player play. Any future multiplayer mode requires a separate relic-resolution rule.

This is an explicit exception to the otherwise mining-driven ordinary crafting economy: common ore may enter the inventory through mining or relic sale. Relics never sell for specialized ordinary resources, so selling one cannot directly bypass the map's specialized-resource profile.

## Effect design

Relics should change gameplay significantly. Their identity comes from an unusual rule, altered geometry, constraint, danger, or tradeoff rather than an ordinary unconditional stat increase. Relics are deliberately tuned to feel extremely powerful at first; the player should nevertheless need to reconsider weapon selection, positioning, routing, or another important behavior to exploit them.

Relic designs may affect only a tagged subset of systems when that restriction is part of the effect. The interface must identify which equipped weapons and mechanics are affected before installation.

### Accepted initial catalog

The [Initial Relic Catalog](./69-initial-relic-catalog.md) defines ten accepted effects: nine broadly transform weapon behavior, while Claim-Jumper Core transforms mining commitment and enemy movement. The set covers reversed geometry, delayed duplication, facing-based aim, giant slow attacks, synchronized beats, enemy clustering, kill chains, stationary heat, accelerated dangerous mining, and rotating single-weapon phases.

Every relic has a one-sentence discovery summary containing its essential benefit and tradeoff. The discovery choice presents that sentence before expanded rules. A player may inspect details, current weapon compatibility, and exact numbers, but must not need those details merely to avoid being surprised by the relic's basic behavior.

## Replacement and interaction rules

- Only one relic effect can be active on the mech at a time.
- Installing a new relic replaces the active relic rather than stacking with it.
- A relic may affect all equipped weapons, only a clearly defined weapon family, or a non-weapon gameplay system.
- A relic does not alter the fixed recipe or base description of the affected weapon; the current effective behavior must nevertheless be shown to the player.
- A relic's effect combines with stat ranks and the weapon's chosen branch unless that relic explicitly states an exception.

The initial catalog defines shared modifier ordering and the primary interaction boundaries. Per-weapon edge mappings revealed by prototyping remain tuning work, but they must preserve the accepted one-sentence concept.

## Feedback requirements

Before the player installs or sells a discovered relic, the game must communicate:

- The relic's complete player-facing rule, including its upside and twist or tradeoff.
- Which equipped weapons or other systems it currently affects.
- The common-ore payout for selling it.
- The currently installed relic, if any.
- That installation will replace the current effect.
- The common-ore payout automatically received for the displaced relic if replacement is chosen.
- That the complete gameplay simulation is paused and installation or sale is required to resume.

During play, the HUD must identify the installed relic and make its altered behavior readable through effects, audio, animation, or concise text. The player should not need to infer that a weapon is behaving differently because of an unseen modifier.

## Balance intent

- A relic discovery should be exciting even when the relic does not suit the current build because selling it still advances the run.
- Installing a relic should create a meaningful build or play-pattern decision, not a routine power pickup.
- Relics should invite adaptation and experimentation without making the original weapon descriptions misleading.
- The one-slot limit should make later discoveries genuine replacement decisions.
- Three guaranteed caches should make relics a dependable run feature without guaranteeing that the player finds every one.
- Sale value should be useful without making relic discovery feel like a disguised ore cache.
- Relic placement should reward exploration without making a run's viability depend on finding one.

## Open questions

- [OQ-008 — How does exploration work?](./open-questions.md#oq-008--how-does-exploration-work)

## Related documents

- [Core Game Loop](./10-core-game-loop.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#relic-cache-discovery-and-resolution)
- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md)
- [Initial Relic Catalog](./69-initial-relic-catalog.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./decisions/DEC-029-pause-and-resolve-relic-discoveries.md)
- [DEC-030 — Place three automatic relic caches on each standard map](./decisions/DEC-030-three-automatic-relic-caches.md)
- [DEC-099 — Use single-player pause and results flow](./decisions/DEC-099-use-single-player-pause-and-results-flow.md)
- [DEC-118 — Accept the initial relic catalog](./decisions/DEC-118-accept-initial-relic-catalog.md)
- [DEC-121 — Accept the initial option-unlock catalog](./decisions/DEC-121-accept-initial-option-unlock-catalog.md)
