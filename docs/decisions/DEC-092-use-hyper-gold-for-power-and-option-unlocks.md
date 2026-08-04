---
doc_id: DEC-092
title: Use Hyper Gold for Power and Option Unlocks
status: accepted
authoritative: false
validation: playtest
---

# DEC-092 — Use Hyper Gold for Power and Option Unlocks

## Decision

Hyper Gold supports both major forms of permanent progression associated with *Vampire Survivors*:

1. **Permanent numerical power upgrades** that improve future runs.
2. **Permanent content and option unlocks** that expand the choices available in future runs.

Neither category replaces the other. The player must eventually have meaningful Hyper Gold purchases in both. DEC-093 later makes numerical PowerUps account-wide, DEC-094 makes their between-run refund free and complete, and DEC-120 supplies their thirteen effects, prices, caps, and active-rank rules. DEC-121 supplies the initial six-purchase option catalog, fresh-profile baseline, and permanent ownership rules. Later catalog expansion and the final interface remain open.

## Status

Accepted as the cross-run progression structure. Content and tuning remain open.

## Rationale

Permanent numerical power creates a direct sense of account growth and helps players overcome earlier difficulty. Option unlocks expand experimentation and replayability without reducing all progress to stat accumulation. Offering both lets a player choose between becoming stronger now and broadening future build possibilities.

This structure follows the intended *Vampire Survivors* reference without requiring its exact upgrade catalog or economy. Run-local mining and crafting remain the primary source of power within a deployment; Hyper Gold changes the starting progression context across deployments.

## Consequences

- The between-run progression interface must visibly separate power purchases from option unlocks or otherwise make their different consequences unmistakable.
- Permanent PowerUps apply account-wide under DEC-093, span combat, survivability, mobility, and mining/economy under DEC-095, and require explicit caps and displayed per-rank effects; they are not an uncapped substitute for run-local stat crafting.
- The initial option catalog contains one six-blueprint utility bundle and five relic-pool additions. Later option unlocks may include mechs, weapons, utilities, relics, maps, modes, cosmetics, or other player-facing choices through separate decisions.
- Unlocking an option does not guarantee acquiring it during a run; normal mech selection, resource-profile, exploration, slot, and crafting rules still apply unless a specific unlock states otherwise.
- Hyper Gold prices must be calibrated against 100 per completed site, 25 per defeated-and-looted boss, and 0–400 bankable Hyper Gold per successful standard run.
- DEC-112 later requires fresh-account viability and substantial but bounded permanent power that eases early play without replacing late-run build development.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Gameplay Loop](../10-core-game-loop.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Permanent Option-Unlock Catalog](../63-permanent-option-unlock-catalog.md)
- [OQ-006 — Where do resources, crafting, and upgrades persist?](../open-questions.md#oq-006--where-do-resources-crafting-and-upgrades-persist)
- [OQ-010 — What are the progression layers?](../open-questions.md#oq-010--what-are-the-progression-layers)

## Supersedes / superseded by

Resolves whether Hyper Gold provides permanent power, permanent options, or a mixture: it provides both. It narrows the exact-purchase questions left open by [DEC-091](./DEC-091-name-and-quantify-hyper-gold.md). [DEC-093](./DEC-093-make-permanent-power-account-wide.md) later resolves the numerical upgrades' scope, [DEC-112](./DEC-112-bound-permanent-power-below-run-build-power.md) resolves their broad power ceiling, [DEC-120](./DEC-120-accept-permanent-powerup-catalog.md) defines their initial numerical catalog, and [DEC-121](./DEC-121-accept-initial-option-unlock-catalog.md) defines their initial option catalog.
