---
doc_id: DEC-039
title: Target a Six-Mech Initial Roster
status: accepted
authoritative: false
---

# DEC-039 — Target a Six-Mech Initial Roster

## Decision

The initial playable roster contains six mechs. Each has a distinct inherent trait and a different signature weapon selected from the normal 15-weapon catalog.

DEC-043 selects Rail Lance, Pulse Repeater, Gravity Projector, Reactor Pulse, Missile Rack, and Ram Field as the six initial signatures. The remaining nine weapons are normal craftable catalog weapons without an initial signature-mech assignment. Later mechs may use them as signatures, but the initial game does not require one mech per weapon.

## Status

Accepted initial content target.

## Context

Every mech needs a unique starting identity, trait, and signature weapon, but requiring all 15 weapons to anchor characters would greatly expand the initial character-content scope. Too small a roster would underuse the signature-aware resource-generation system and offer little pre-run variety.

## Considered options

### One initial mech per weapon

Fifteen mechs fully expose the catalog through starting loadouts but create a large trait, presentation, balance, and unlock burden.

### A very small prototype roster

Two or three mechs reduce scope but provide limited signature and profile variety.

### Six initial mechs

Six mechs can demonstrate meaningfully different starting patterns and traits while leaving room for future roster expansion.

## Rationale

Six is large enough to make mech selection consequential and exercise several signature-aware profile constraints, but small enough for each mech to have a legible identity. Signature selection can emphasize representative weapon patterns rather than treating the whole weapon catalog as a character checklist.

## Consequences

- Exactly six different weapons serve as signatures in the initial roster.
- No initial mech shares another initial mech's signature weapon.
- Signature weapons remain in the same base power tier as all nine non-signature catalog weapons under DEC-041.
- Nine normal weapons initially have no associated playable mech.
- Initial signature selection should demonstrate a range of automatic targeting and delivery patterns, but it need not satisfy a rigid role quota.
- DEC-117 fixes the six initial mech identities, signature assignments, inherent traits, silhouettes, fresh-profile availability, and selection behavior. Final presentation names and numerical balance remain open.
- Roster expansion may add mechs without adding weapons by assigning existing catalog weapons as signatures.
- Whether later mechs may share signature weapons remains open; uniqueness is required only within the initial six.

## Specification links

- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Initial Mech Catalog](../36-initial-mech-catalog.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-028 — What are the 15 base weapons and their graph assignments?](../open-questions.md#oq-028--what-are-the-15-base-weapons-and-their-graph-assignments)

## Supersedes / superseded by

This resolves the initial roster-size question left open by [DEC-014](./DEC-014-selectable-mechs-and-signature-weapons.md). [DEC-041](./DEC-041-equal-tier-base-weapon-catalog.md) later confirms that signature selection does not create a higher weapon tier. [DEC-043](./DEC-043-fifteen-weapon-graph-assignment.md) selects the six initial signature weapons. [DEC-117](./DEC-117-accept-initial-mech-catalog.md) later pairs them with accepted initial identities, traits, silhouettes, availability, and selection rules.
