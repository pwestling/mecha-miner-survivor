---
doc_id: DEC-112
title: Bound Permanent Power Below Run-Build Power
status: accepted
authoritative: false
validation: progression-and-difficulty-playtest
---

# DEC-112 — Bound Permanent Power Below Run-Build Power

## Decision

Account-wide numerical PowerUps provide a substantial, permanent advantage without replacing the much larger power development created inside each run.

At a highly upgraded or fully upgraded account:

- early standard-map phases should feel noticeably easier and faster than on a fresh profile;
- the player should have greater tolerance for inefficient routes, interrupted mining, unlucky relics, and imperfect purchases;
- a coherent fabricated weapon-and-utility build is still necessary for the late run;
- movement, mining commitments, boss pressure, and the final crescendo cannot be ignored universally; and
- starting equipment plus permanent stats alone does not constitute a complete 35-minute build.

A fresh account retains a plausible path to standard mission extraction. No permanent purchase is a hidden prerequisite for the initial standard map, and the director does not increase enemy statistics to cancel purchased PowerUps. The earned advantage is real.

Individual PowerUps use explicit rank caps. Their combined multipliers must remain bounded rather than producing order-of-magnitude stat growth, automatic survival, automatic resource acquisition, or a bypass around exploration and fabrication. Exact effects, caps, prices, prerequisites, and the final composite advantage remain catalog and playtest work.

Later maps, Hyper-style variants, or challenge modes may be designed as outlets for accumulated account strength. Any mode that assumes a particular progression level must disclose that expectation rather than silently changing the standard-map baseline.

## Status

Accepted as the permanent-power ceiling and standard-mode balance invariant. DEC-120 later supplies the individual PowerUp catalog, caps, values, and prices.

## Rationale

Permanent progression should feel valuable: weakening it until upgrades are barely perceptible would make Hyper Gold unexciting. Conversely, if account stats replace routing, mining, fabrication, and movement, the game's defining run decisions disappear and early content becomes an idle grind.

Allowing substantial early ease while preserving late-run build requirements gives account growth a visible payoff without making progression compulsory. Separate challenge content can absorb accumulated strength without secretly scaling the base stage against the player.

## Consequences

- Balance testing requires fresh, partial, and fully upgraded account profiles.
- Early-wave time-to-kill, safe mining tolerance, boss kill timing, and final-crescendo survival must be compared across those profiles.
- A fully upgraded account may make the standard map substantially more consistent, but cannot make every valid loadout or route automatically successful.
- PowerUp ranks can strengthen combat, survivability, mobility, and mining/economy while still requiring the corresponding gameplay interaction.
- Content unlocks expand options and are not counted as numerical power for this ceiling.
- Optional harder content should provide the long-term mastery outlet rather than hidden standard-mode counter-scaling.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](../20-run-structure-and-timing.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Open Questions](../open-questions.md)

## Supersedes / superseded by

Completes the broad progression-power envelope left open by [DEC-092](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md), [DEC-093](./DEC-093-make-permanent-power-account-wide.md), [DEC-095](./DEC-095-include-mining-and-economy-powerups.md), and [DEC-101](./DEC-101-target-an-approachable-escalating-standard-difficulty.md). [DEC-120](./DEC-120-accept-permanent-powerup-catalog.md) later selects the individual PowerUps within that envelope.
