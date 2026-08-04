---
doc_id: GDD-PLAYABLE-MECHS
title: Playable Mechs and Starting Loadouts
status: active
authoritative: true
---

# Playable Mechs and Starting Loadouts

## Purpose and player promise

Playable mechs provide distinct, recognizable starting identities within the shared mining-and-fabrication system. Selecting a mech gives the player one stable strategic anchor before the randomized geology is revealed and in-run crafting shapes the rest of the build.

## Adopted character structure

The game reuses the high-level playable-character concept from *Vampire Survivors*:

- The game has a roster of selectable playable mechs.
- The player selects one mech before each deployment.
- The player does not see the run's randomized resource profile before making that selection.
- Each mech has one fixed signature automatic weapon equipped at run start.
- Each mech also has a distinct gameplay trait expressed through a passive bonus, stat profile, or equivalent always-relevant rule.
- The selected mech, signature weapon, and trait persist for that run.
- Additional weapons and weapon upgrades are obtained through mining and fabrication rather than XP choices.

The reference defines the structure, not the content. No particular *Vampire Survivors* character, weapon, stat, progression curve, or unlock rule is inherited automatically.

## Initial roster target

The initial game contains six playable mechs:

| Mech | Signature weapon | Inherent trait |
| --- | --- | --- |
| Kestrel | Pulse Repeater | +15% weapon attack rate |
| Pike | Rail Lance | +15% weapon damage |
| Prospector | Missile Rack | +15% mining extraction rate |
| Lodestar | Gravity Projector | +15% weapon area |
| Bastion | Reactor Pulse | +25 maximum Hull Integrity |
| Razorback | Ram Field | +10% movement speed |

The other nine weapons remain ordinary craftable weapons without an initial signature assignment. Future roster additions may promote those weapons into signatures without requiring new weapon content.

All six signature weapons have accepted base behavior, stat bundles, and branch sets in the [Weapon Specification Index](./weapons/README.md). The authoritative [Initial Mech Catalog](./36-initial-mech-catalog.md) defines their identities, traits, distinct top-down silhouettes, fresh-profile availability, selection summaries, stacking rules, and validation requirements.

Signature status does not place a weapon in a higher power tier. All 15 catalog weapons are intended as viable build anchors; a signature mech's advantage is guaranteed free starting access, plus only those additional interactions explicitly supplied by its inherent trait.

## Signature starting weapon

The signature weapon lets combat function immediately, before the player reaches the first mining point. It attacks automatically under the same broad rules as other weapons and occupies the initial weapon loadout.

Once common basic ore is mined, the signature weapon's individual stats can be upgraded separately like those of any other equipped weapon. Its larger play-pattern branches use specialized ordinary resources.

Every signature weapon is one of the normal 15 base weapons defined by the complete pairings of six specialized resource families. It is not signature-exclusive: another mech may fabricate it when its blueprint is unlocked, both recipe resources are present, and the normal cost and slot rules are satisfied. The associated mech's special advantage is beginning the run with that weapon already equipped without paying its fabrication recipe.

A mech cannot equip a second copy of its signature weapon or duplicate any other equipped weapon.

Each weapon has three fixed branch-resource colors: its two base-recipe colors and one distinct assigned third color. After the player selects a mech, map generation selects four of the six resource families while guaranteeing at least two of the signature weapon's three branch colors. The player still does not see which four were selected until deployment.

The signature starts at its catalog base state with no purchased stat ranks and no major branch. Signature status alone grants no special scaling or exclusive branches; any interaction must be stated by the mech's general trait. Every standard mech starts with exactly one weapon. A future exception must be explicit and must still respect the four-slot limit.

## Mech-specific trait

Every mech has one gameplay distinction beyond presentation and starting weapon. The trait is selected implicitly with the mech rather than found randomly during the run.

Each initial trait is a concise, positive, always-on modifier using the shared stat vocabulary: Attack Rate, weapon Damage, mining Extraction Rate, weapon Area, maximum Hull Integrity, or Movement Speed. Traits create recognizable preferences but require no particular material combination. They consume no slot, have no ranks, and remain fixed throughout the run. Matching utility and PowerUp modifiers stack under the shared additive-percentage rules. Exact numeric values are accepted starting points for playtesting rather than promises that balance revision will never change them. Because this game has no XP, no trait uses level-based scaling.

## Shared build system

After deployment, the mech develops through the fixed unlocked blueprint catalog, the map's randomized resource profile, and relics discovered through exploration. Each mech has four weapon slots, three utility slots, and one separate relic slot. Its signature weapon occupies one weapon slot, leaving three for additional crafted weapons. Fabricated weapons and utilities permanently fill their chosen slots for that run and cannot be removed, replaced, dismantled, sold, or refunded. The utility and relic slots are separate; the mech's inherent trait consumes none of them. The relic slot begins empty and can hold one run-local relic whose significant mech-wide effect may alter several weapons or another gameplay rule.

The signature weapon anchors the build but should not determine every later choice. Base weapons beyond that signature require specialized resources. Every profile exposes exactly six of the 15 pair-weapon recipes. When one is the equipped signature, five different additional weapons remain; otherwise all six are additional choices. Profile generation guarantees at least two of the signature weapon's three branch colors, then relies on the remaining colors and the mech's trait to create build variation.

All mechs share the standard direct-movement and collision model: immediate 3.0M/s base movement, a 1.0M circular player footprint, normalized digital diagonals, no inertia or turn radius, non-solid enemies, and a fixed north-up camera. A mech may alter a displayed base statistic such as movement speed, armor, or mining rate, but does not silently change the input model.

Every playable mech must be viable on every valid randomized resource profile. Mech traits and signature weapons may create preferences, efficiencies, and different strategies, but cannot make a level nonviable solely because of its randomized specialized resources.

## Selection and information

All six initial mechs are available on a fresh profile. Kestrel is initially focused and identified as the recommended first deployment, but confirmation is required and the player may choose any mech. The interface also offers Random Mech among all currently available mechs, reveals its result before confirmation, and remembers the most recently selected available mech.

The player confirms deployment without seeing the randomized resource profile. The geological survey appears 0.5 active seconds after play begins, during a one-minute orientation phase with deliberately minor enemy waves. Its non-modal card remains expanded for 12 active seconds and stays reviewable through Fabrication under the [interface specification](./73-interface-screen-flow-and-information-architecture.md#opening-geological-survey).

The selection interface must eventually communicate:

- The mech's signature weapon and automatic attack behavior.
- Its passive bonus or stat differences.
- Numerical differences from the shared baseline, with positive and negative modifiers both visible before confirmation.
- Its resulting maximum Hull Integrity, Armor, and Recovery when its trait changes any of them.
- Any mech-specific equipment or crafting rules.
- Whether the mech is available, locked, newly unlocked, or selected.

Permanent numerical PowerUps purchased with Hyper Gold apply account-wide to every mech, including mechs unlocked after the purchase. A mech's signature weapon, inherent trait, and explicit mech-specific stat differences modify that shared upgraded baseline; changing mechs never requires repurchasing the same PowerUps.

## Content still required

- Final presentation names; the six accepted names are working names.
- Final models, animations, skins, and selection renders within the accepted silhouette and cosmetic-only variant rules.
- Numeric trait tuning after representative runs across all signature-valid profiles and account-progression states.
- Identities and extraction-gated or Hyper-Gold-based unlock requirements for any later roster additions.
- Final selection-screen styling, mech renders, and transition animation; layout and comparison information are fixed by DEC-127.

## Related documents

- [Game Vision](./00-game-vision.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md)
- [Core Game Loop](./10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Mech Relics](./67-mech-relics.md)
- [Initial Mech Catalog](./36-initial-mech-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [DEC-014 — Use a selectable mech roster with signature starting weapons](./decisions/DEC-014-selectable-mechs-and-signature-weapons.md)
- [DEC-015 — Reveal randomized geology during the active opening](./decisions/DEC-015-in-run-opening-geological-survey.md)
- [DEC-018 — Use four weapon slots and three utility slots](./decisions/DEC-018-four-weapons-three-utilities.md)
- [DEC-023 — Use per-stat ore upgrades and specialized-resource weapon branches](./decisions/DEC-023-weapon-stat-and-branch-upgrades.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](./decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [DEC-036 — Use six-color signature-aware resource profiles](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-037 — Use unique weapons and soft profile balance](./decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [DEC-038 — Use a broad automatic-weapon taxonomy](./decisions/DEC-038-broad-automatic-weapon-taxonomy.md)
- [DEC-039 — Target a six-mech initial roster](./decisions/DEC-039-six-mech-initial-roster.md)
- [DEC-041 — Use an equal-tier base-weapon catalog](./decisions/DEC-041-equal-tier-base-weapon-catalog.md)
- [DEC-043 — Assign the fifteen base weapons to the resource graph](./decisions/DEC-043-fifteen-weapon-graph-assignment.md)
- [DEC-093 — Make permanent power account-wide](./decisions/DEC-093-make-permanent-power-account-wide.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](./weapons/README.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
- [RES-001 — Vampire Survivors reference mechanics](./research/RES-001-vampire-survivors-reference.md)
- [DEC-097 — Inherit direct movement, collision, and camera](./decisions/DEC-097-inherit-direct-movement-collision-and-camera.md)
- [DEC-100 — Commit installed weapons and utilities](./decisions/DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](./decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-117 — Accept the initial mech catalog](./decisions/DEC-117-accept-initial-mech-catalog.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
