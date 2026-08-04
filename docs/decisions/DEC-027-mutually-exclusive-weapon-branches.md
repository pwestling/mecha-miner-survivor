---
doc_id: DEC-027
title: Make Major Weapon Branches Mutually Exclusive
status: accepted
authoritative: false
---

# DEC-027 — Make Major Weapon Branches Mutually Exclusive

## Decision

A weapon can commit to only one mutually exclusive major branch during a run. Installing one branch prevents that weapon from also installing its alternative major branches.

## Status

Accepted.

## Context

Specialized-resource branches are intended to create a consequential weapon commitment, ranging from substantial amplification of its familiar pattern to a playstyle conversion. If all alternatives can accumulate, a sufficiently successful run can converge on the same fully upgraded weapon and branch selection stops defining the build.

## Considered options

### Stack every major branch

This offers a long upgrade runway but lets choice collapse into purchase order once enough resources are available.

### Partially compatible branch graph

This can produce intricate builds but greatly increases compatibility rules and UI burden before the weapon roster is defined.

### One mutually exclusive major branch

This makes the first commitment legible and preserves distinct versions of the same weapon across runs.

## Rationale

Mutual exclusivity turns specialized geology into a consequential build direction rather than a checklist. It also makes repeated runs with the same weapon meaningfully different and preserves clear weapon identities.

## Consequences

- The fabrication interface must show all excluded alternatives before branch confirmation.
- The amplification, functional-variant, and playstyle-conversion branches must remain credible alternatives rather than forming an obvious ascending power ladder.
- Each weapon has exactly three meaningful alternatives under DEC-040.
- DEC-044 makes branches immediately eligible and irreversible for the run. DEC-100 later prohibits weapon removal and dismantling entirely. Follow-on branch upgrades remain open in OQ-025.
- Mech relics remain a separate one-slot system and can alter the selected branch's effective behavior.

## Specification links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Mech Relics](../67-mech-relics.md)
- [OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?](../open-questions.md#oq-025--how-are-uncapped-stat-upgrades-priced-and-weapon-branches-structured)
- [DEC-040 — Use a three-level weapon-branch transformation gradient](./DEC-040-three-branch-transformation-gradient.md)
- [DEC-044 — Use immediate permanent branch commitment](./DEC-044-immediate-permanent-branch-commitment.md)

## Supersedes / superseded by

Narrows the branch-compatibility rules left open by [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md). [DEC-040](./DEC-040-three-branch-transformation-gradient.md) later fixes the count at three and defines their transformation categories. [DEC-044](./DEC-044-immediate-permanent-branch-commitment.md) later makes the choice immediately eligible and irreversible. Individual branch effects remain open.
