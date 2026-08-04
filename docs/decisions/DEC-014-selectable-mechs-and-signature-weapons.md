---
doc_id: DEC-014
title: Use a Selectable Mech Roster with Signature Starting Weapons
status: accepted
authoritative: false
---

# DEC-014 — Use a Selectable Mech Roster with Signature Starting Weapons

## Decision

Before each run, the player selects one playable mech from a roster. Each mech has one fixed signature automatic weapon equipped at deployment and a distinct gameplay trait expressed through a passive bonus, stat profile, or equivalent rule. Mining and fabrication provide the rest of the run's weapons and upgrades.

This adopts the high-level *Vampire Survivors* character concept without automatically inheriting its specific characters, stats, bonuses, unlock conditions, or XP-based scaling.

## Status

Accepted.

## Context

The player needs an immediately functional automatic attack before reaching the first mining point. The run also benefits from a stable starting identity that can coexist with randomized resource profiles and deliberate crafting.

## Considered options

### Choose any starting weapon independently

This maximizes loadout agency but makes it easier to force one favorite opening and gives the mech itself less mechanical identity.

### Start without a weapon

This would make the first mining point mandatory before combat can function and could create an unplayable opening under horde pressure.

### Pair each selectable mech with a signature weapon and trait

This guarantees a functional opening, differentiates the roster, and gives each run one known anchor before geology shapes later choices.

## Rationale

The structure is legible, established by the reference, and compatible with the game's intentional crafting goal. The selected mech determines how the player begins, while mining routes and resource availability still determine how the build develops.

## Consequences

- Every mech requires a signature automatic weapon and at least one meaningful gameplay trait.
- The signature weapon occupies one of the four weapon slots established by [DEC-018](./DEC-018-four-weapons-three-utilities.md); the inherent mech trait consumes no weapon or utility slot.
- Mech selection is a strategic pre-deployment choice.
- Every signature weapon needs a viable opening against enemies present before the first craft.
- DEC-036 guarantees at least two of the signature weapon's three branch-resource colors in every generated profile because selection occurs before the randomized geology is revealed.
- Signature weapons belong to the shared 15-weapon catalog and can be fabricated by other mechs under normal rules.
- DEC-039 sets the initial roster at six mechs with six different signature weapons from the normal catalog.
- DEC-117 fixes the six initial identities, signature assignments, simple inherent traits, silhouettes, fresh-profile availability, and selection rules. Final presentation names and numerical tuning remain open, as do identities and unlock requirements for later roster additions.
- XP-triggered character scaling is not assumed because the game has no XP.
- The later [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md) lets common ore improve individual signature-weapon stats and specialized ordinary resources purchase its larger branches.

## Specification links

- [Game Vision](../00-game-vision.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Initial Mech Catalog](../36-initial-mech-catalog.md)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [OQ-021 — What is the pre-deployment selection order?](../open-questions.md#oq-021--what-is-the-pre-deployment-selection-order)
- [DEC-015 — Reveal randomized geology during the active opening](./DEC-015-in-run-opening-geological-survey.md)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

No earlier accepted decision is superseded. This narrows the player-fantasy and starting-loadout questions left open by the initial vision. [DEC-015](./DEC-015-in-run-opening-geological-survey.md) later resolves selection order. [DEC-036](./DEC-036-six-color-signature-aware-resource-profiles.md) resolves signature weapons' relationship to the normal catalog and constrains resource generation around the selected signature. [DEC-039](./DEC-039-six-mech-initial-roster.md) resolves the initial roster size, and [DEC-117](./DEC-117-accept-initial-mech-catalog.md) resolves the initial identities, traits, signature pairings, availability, and selection behavior.
