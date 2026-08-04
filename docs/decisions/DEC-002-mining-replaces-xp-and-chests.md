---
doc_id: DEC-002
title: Replace XP and Treasure Chests with Mining and Crafting
status: accepted
authoritative: false
---

# DEC-002 — Replace XP and Treasure Chests with Mining and Crafting

## Decision

The game has no experience-point system. Mining multiple resource types and crafting with those resources replace XP level-up progression and treasure-chest weapon progression. Ordinary resources primarily create run-local weapons and upgrades; rare special resources found on maps support cross-run upgrades.

## Status

Accepted.

## Context

In the reference progression loop, defeating enemies produces XP that leads to weapon or passive choices, while treasure chests provide random upgrades and weapon evolutions. The new game's identity requires the player to explore for power, accept positional risk while extracting it, and exercise more intention over the resulting build.

## Considered options

### Retain XP and chests alongside mining

This would preserve the familiar progression cadence, but it would compete with mining as the source of run power and could let players ignore exploration.

### Replace both with mining and crafting

This makes the new feature structurally necessary and connects exploration, risk, resources, and build development in one loop.

### Make all mined progression persistent

This would emphasize long-term advancement but weaken the repeated build arc within each run.

### Use ordinary run resources plus rare persistent resources

This preserves a strong run-local build while giving exceptional exploration discoveries lasting value.

## Rationale

Replacing rather than merely supplementing XP and chests prevents the reference game's progression loop from overshadowing mining. Multiple resource types can make route choice and recipe planning meaningful. Separating ordinary and rare resources supports both within-run growth and cross-run motivation.

## Consequences

- Monsters do not drop XP gems.
- XP thresholds do not trigger ordinary weapon choices.
- Treasure chests do not supply ordinary random weapon progression.
- Mining-point availability and crafting cadence must support the full run power curve.
- The player must be able to understand which resource types enable which weapons or upgrades.
- Failure rules must distinguish ordinary run-local resources from rare cross-run resources.
- [DEC-020](./DEC-020-mining-exclusive-ordinary-materials.md) later excluded crafting-material drops from enemies. [DEC-029](./DEC-029-pause-and-resolve-relic-discoveries.md) preserves that prohibition while making relic sale the sole non-mining source of common ore; specialized ordinary resources remain mining-exclusive.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [RES-001 — *Vampire Survivors* reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

None.
