---
doc_id: DEC-036
title: Use Six-Color Signature-Aware Resource Profiles
status: accepted
authoritative: false
---

# DEC-036 — Use Six-Color Signature-Aware Resource Profiles

## Decision

The specialized ordinary-resource system contains exactly six resource families. Every standard run selects exactly four of the six for its resource profile.

Every base weapon is associated with a unique unordered pair of specialized resources, producing a complete 15-weapon pair catalog. One recipe color funds its amplification branch and the other funds its functional variant. A fixed third color, distinct from both recipe colors, funds its playstyle conversion. At the time of this decision the exact near-balanced assignment remained content work; it was later completed by DEC-043 and DEC-060 through DEC-075.

Every selectable mech's signature weapon comes from this same 15-weapon catalog rather than from a separate signature-only category. The mech begins with it already equipped and does not pay its normal fabrication recipe. Other mechs may fabricate that weapon under the normal blueprint, profile, resource-cost, and slot rules.

After mech selection, resource-profile generation must include at least two of the selected signature weapon's three branch-resource colors. The profile remains hidden from the player until deployment.

## Status

Accepted.

## Context

The resource profile needs to disrupt repeatable favorite builds while offering meaningful choice for the three weapon slots left open by the signature weapon. Because the player commits to a mech before seeing geology, the selected signature weapon also needs useful specialized-resource development paths in every generated run.

## Considered options

### Five colors with four present

This requires only 10 pair-weapons and exposes six per run, but produces just five profiles, omits only one color, and makes every two-option utility available.

### Six colors with four present

This requires 15 pair-weapons, exposes six per run, produces 15 unconstrained profiles, and omits two colors strongly enough to alter weapon and branch possibilities.

### Seven colors with five present

This offers a perfectly balanced Fano-plane third-color assignment and near-half weapon availability, but requires 21 pair-weapons.

### Do not condition geology on the selected mech

This maximizes profile uniformity across the roster, but can leave the guaranteed starting weapon with only common-ore stat growth and no practical specialized branch.

### Require at least two of the signature weapon's three branch colors

This preserves some branch adaptation while guaranteeing at least two branch paths. It biases geology toward the selected mech without revealing the resulting profile before deployment.

## Rationale

Six colors with four present gives six theoretical pair-weapons per run—twice the number of normally empty weapon slots—without exposing most of the 15-weapon catalog. Conditioning the roll on the chosen signature weapon protects the player's only guaranteed weapon from an upgrade dead end while still withholding one of its three branch colors in most runs.

Keeping signature weapons inside the shared catalog avoids maintaining two parallel weapon classes. A mech's identity comes from guaranteed starting access and its trait, not from permanently exclusive weapon content.

## Consequences

- Every resource profile contains exactly four specialized resource families and supports exactly `C(4,2) = 6` pair-weapon recipes.
- The persistent complete pair catalog contains exactly `C(6,2) = 15` base weapons.
- For a fixed signature weapon, 12 of the 15 possible four-color profiles satisfy the two-of-three branch-color guarantee.
- Of those 12 valid profiles, 9 contain exactly two signature branch colors and 3 contain all three: 75% and 25% respectively under uniform selection among valid profiles.
- A particular signature branch color appears in 9 of 12 valid profiles, or 75%; a particular non-signature color appears in 7 of 12, or approximately 58.3%. Map generation is therefore intentionally biased by mech selection.
- The signature weapon's two recipe colors are both present in 6 of 12 valid profiles. Because DEC-037 forbids duplicates, the player has five other theoretically craftable weapons in those runs and six in the other runs.
- The selected mech and signature weapon must be known before the resource-profile roll can be finalized, but the result is not shown until active play begins.
- Broad abundance bands and deposit placement remain randomized after the four resource families are selected, subject to existing fairness requirements.
- The fixed third-color assignment must be generated and audited for near-balance because six colors cannot support a perfectly symmetric Steiner triple system.
- All valid mech/profile combinations must be checked for impossible or restart-worthy outcomes and pervasive graph bias. Equal tactical-role coverage is not required.

## Specification links

- [Playable Mechs and Starting Loadouts](../35-playable-mechs.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [RES-006 — Resource-color graph for weapon availability](../research/RES-006-resource-color-weapon-graph.md)

## Supersedes / superseded by

This resolves the numeric model and signature-weapon relationship left open by [DEC-034](./DEC-034-gate-base-weapons-by-resource-profile.md). It narrows the viability guarantees in [DEC-014](./DEC-014-selectable-mechs-and-signature-weapons.md) and [DEC-015](./DEC-015-in-run-opening-geological-survey.md) without changing the rule that geology remains hidden until deployment. [DEC-037](./DEC-037-unique-weapons-and-soft-profile-balance.md) later resolves duplicate equipment and narrows the profile-balance standard.
