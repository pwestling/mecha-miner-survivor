---
doc_id: DEC-007
title: Allow Unlimited On-Demand Fabrication Access
status: accepted
authoritative: false
validation: playtest
---

# DEC-007 — Allow Unlimited On-Demand Fabrication Access

## Decision

The player can open the fabrication menu anywhere and at any time during a run, as often as desired. Opening it requires no charge, milestone, boss event, or fabrication location. The entire gameplay simulation freezes while the menu is open. Within one visit, the player may complete any number of crafts whose resource, recipe, and equipment requirements are satisfied.

This access model is accepted for playtesting and may be constrained later if observed player behavior shows that it harms the game.

## Status

Accepted for playtesting.

## Context

A limited fabrication-window model risked withholding power until an arbitrary cadence point and made pre-boss growth dependent on correctly tuning access milestones. Unrestricted access lets mined resources and recipes determine when the player can become stronger.

## Considered options

### Fixed-time or pre-boss windows

These bound menu frequency and make breaks predictable, but can force the player to hold usable resources or miss a needed preparation opportunity.

### Boss-unlocked windows

These create natural punctuation, but post-boss access cannot help the player prepare for that boss and defeat-gating can create a power deficit loop.

### Location-bound fabrication

This reinforces exploration but adds another mandatory destination and can make upgrade access depend too heavily on map routing.

### Unlimited on-demand access

This gives the player immediate control over when to convert mined resources into power and guarantees access before bosses.

## Rationale

The game already limits power through resource acquisition, recipe costs, and mining risk. Removing a separate access gate keeps the system easy to understand and allows the prototype to reveal whether unrestricted pausing is actually harmful rather than assuming that it will be.

## Consequences

- A player with affordable recipes can increase power immediately before an interval boss.
- Fabrication can occur directly after mining or at any other chosen moment.
- Players may interrupt action frequently or use the menu as a panic pause.
- The UI must make affordable crafts visible enough that players are not encouraged to check the menu constantly.
- The level timer, enemies, AI, spawning, projectiles, automatic attacks, cooldowns, mining progress and decay, threat-beacon events, hazards, status durations, pickups, and gameplay physics do not advance while the menu is open.
- Only the fabrication interface and its non-gameplay presentation continue during the pause.

## Playtest validation

Observe and measure:

- Menu openings per minute and their distribution around mining and bosses.
- Median and extreme time spent in the menu.
- Openings that result in no craft.
- Whether players open the menu reflexively when endangered.
- Whether frequent pauses weaken horde tension or mining commitment.
- Whether players understand what they can afford without opening the menu.
- Whether unrestricted access produces a smoother and more intentional power curve than limited windows.

If the access model fails, first test low-friction constraints such as requiring at least one affordable craft, a short reopen cooldown, or limited access during specific objective states before moving to a fully scheduled system.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [RES-003 — Crafting-break cadence](../research/RES-003-crafting-break-cadence.md)

## Supersedes / superseded by

Extends [DEC-006](./DEC-006-paused-crafting-and-run-resource-reset.md) by resolving its open access trigger. It supersedes the unaccepted limited-window proposal, not an earlier accepted decision.
