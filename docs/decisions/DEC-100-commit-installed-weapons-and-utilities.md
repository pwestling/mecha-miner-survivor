---
doc_id: DEC-100
title: Commit Installed Weapons and Utilities
status: accepted
authoritative: false
validation: playtest
---

# DEC-100 — Commit Installed Weapons and Utilities

## Decision

Follow the core *Vampire Survivors* loadout-commitment model: once a weapon or utility is equipped during a run, it cannot be removed, replaced, dismantled, sold, or refunded during that run.

The signature weapon is permanently committed to its starting weapon slot. Fabricating a weapon or utility immediately and permanently occupies an empty corresponding slot. When all four weapon slots or all three utility slots are full, fabrication cannot install another item of that class.

The fabrication interface previews the resulting slot commitment and requires confirmation before spending resources. Resources remain available if a craft is blocked by capacity. The mech relic remains the explicit exception: later relic discoveries may replace the single installed relic under the established automatic-sale rule.

## Status

Accepted as the baseline run-local equipment lifecycle.

## Rationale

Irreversible slot choices make the build itself a run commitment and follow the reference game's unique-item loadout behavior. Deterministic fabrication already gives the player full knowledge of the selected item, so a clear confirmation is sufficient protection against accidental commitment.

Allowing dismantling would introduce refund valuation, stat-rank persistence, branch rollback, and repeated recipe cycling without a reference need. The relic replacement rule remains intentionally different because discovery is finite and relics are designed around direct comparison.

## Consequences

- Weapon stat ranks and branch state never require removal persistence rules in the baseline because the weapon cannot be removed.
- A branch remains irreversible as already decided.
- The radar, once installed, occupies its utility slot for the rest of the run.
- Utilities cannot be swapped to solve a temporary problem and then recovered later.
- Duplicate weapon prohibition remains unchanged.
- No ordinary-resource refund or salvage value exists for installed weapons or utilities.
- Any future replacement system requires an explicit new decision and must specify branch, rank, recipe, and refund behavior.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)

## Supersedes / superseded by

Resolves weapon and utility replacement, dismantling, refund, and removal-persistence questions left open by [DEC-018](./DEC-018-four-weapons-three-utilities.md), [DEC-023](./DEC-023-weapon-stat-and-branch-upgrades.md), and [DEC-087](./DEC-087-price-resource-radar-at-three-hundred-ore.md). It does not change relic replacement under DEC-029.
