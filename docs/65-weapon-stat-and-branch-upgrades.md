---
doc_id: GDD-WEAPON-STAT-AND-BRANCH-UPGRADES
title: Weapon Stat and Branch Upgrades
status: active
authoritative: true
---

# Weapon Stat and Branch Upgrades

## Purpose and player promise

Weapon upgrading gives the player direct control over both incremental performance and major play-pattern changes. Common basic ore supports frequent, precise stat decisions; less-common specialized ordinary resources support consequential weapon-specific branches shaped by the run's randomized geology.

## Upgrade layers

| Layer | Resource | Player choice | Effect scale |
| --- | --- | --- | --- |
| Individual stat upgrade | Common basic ore | Choose one stat from that weapon's fixed upgradeable bundle | A fixed linear increase to the displayed stat per rank |
| Weapon-specific branch | Specialized ordinary resource | Choose one of three mutually exclusive branches | Amplification, functional variation, or playstyle conversion |

Both layers are run-local and modify an already-equipped weapon within its existing weapon slot.

## Individual stat upgrades

Once a weapon is equipped, the fabrication interface exposes its fixed bundle of upgradeable stats separately. The bundle is authored for that weapon rather than selected or assembled by the player. The player spends common basic ore on the exact stat desired instead of buying a bundled weapon level.

Possible weapon-specific stats include:

- Damage.
- Fire rate or cooldown.
- Area or projectile size.
- Projectile speed.
- Projectile count.
- Pierce or chain count.
- Duration.
- Knockback or another control value.

This list is illustrative, not a requirement that every weapon use every stat. A weapon exposes only properties that are meaningful and understandable for its attack pattern. Different weapons may therefore have different stat bundles.

A weapon has at most three common-ore-upgradeable stats by default and may have fewer. Exceeding three requires an explicit exception. A property should remain fixed when making it upgradeable would be redundant, create a weak choice, or undermine the weapon fantasy.

All 15 weapon bundles are accepted and contain exactly three common-ore stats. The complete lookup and every branch-specific reinterpretation are recorded in the [Weapon Specification Index](./weapons/README.md) and [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md). No accepted branch adds a fourth track.

There is no explicit rank cap. Each successive rank adds the same fixed amount to the displayed stat, producing linear stat growth. Every weapon also has a shared **upgrade depth** equal to the total number of common-ore stat ranks purchased across all of its stats. The common-ore price rises nonlinearly with this shared depth rather than with the selected stat's individual rank. Available ore and the 35-minute run limit form the practical cap.

At any given depth, every stat on that weapon has the same next-purchase price. The player chooses which stat receives the rank, then the weapon's shared depth increases by one and raises the next price for all of its stats. For example, rank distributions `4 / 1 / 0` and `2 / 2 / 1` both represent depth 5 and pay the same price for purchase 6. Each weapon maintains an independent depth; buying ranks on one weapon does not increase another weapon's price.

For purchase number `n`, the shared next price is `5n(n + 1)` common ore. Because `n` is current depth plus one, a depth-`d` weapon pays `5(d + 1)(d + 2)`. The first ten prices are 10, 30, 60, 100, 150, 210, 280, 360, 450, and 550 ore. Cumulative cost reaches 100 after three purchases, 200 after four, 350 after five, and 2,200 after ten.

The interface should express cadence and other derived properties in units that can grow linearly without approaching an undefined endpoint. For example, attacks per second is preferable to an uncapped percentage cooldown reduction. The [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md) fixes every per-rank increment. Branches have no stat-rank prerequisites; any future cross-stat prerequisite would require a new decision.

An optional Easter egg is under consideration: extreme, economically inefficient investment in a stat could reveal or unlock a special upgrade. This is not yet accepted content; its trigger, visibility, effect, and repeatability remain open.

## Weapon branches

Specialized ordinary resources purchase larger weapon-specific upgrades. Every weapon has exactly three mutually exclusive branches arranged by distance from the base play pattern:

1. **Amplification — “samey but bigger and better.”** This reinforces the existing targeting, positioning, and attack pattern through a substantial increase or expansion rather than behaving like one more ordinary stat rank.
2. **Functional variant — “a bit different in function.”** This changes one important behavior or tactical emphasis while preserving the weapon's core identity.
3. **Playstyle conversion — “much different in play style.”** This substantially changes the positioning, movement, routing, targeting relationship, or build context that makes the weapon effective.

The gradient measures behavioral change, not power. All three branches should be credible choices, and the most transformative branch is not automatically the strongest. A focused projectile becoming more numerous and penetrating could be amplification, gaining a chaining behavior could be a functional variant, and becoming a rotating pattern could be a playstyle conversion. These examples illustrate classification rather than accepted weapon content.

Branch outcomes, requirements, and prices are fixed and visible. Opening fabrication never rerolls branches. The resource profile changes which branch materials are available and abundant, creating adaptation through mining rather than through random upgrade offers.

All three branches appear immediately after the weapon is equipped and require no common-ore rank, weapon level, elapsed time, or boss prerequisite. Installing one is a single deterministic purchase costing exactly two units of its assigned specialized material.

Major weapon branches are mutually exclusive and irreversible: once the player commits that weapon to one branch, the other two cannot be installed during the same run. Weapons themselves cannot be removed or reacquired during a run. Existing common-ore stat tracks continue to affect the branched form; a branch adds no new stat track unless its catalog entry explicitly establishes an exception. Follow-on branch upgrades remain open.

Each of the 15 normal base weapons has two specialized recipe resources. One recipe color funds amplification and the other funds the functional variant. The fixed distinct off-color resource always funds the playstyle conversion. All native and off-color assignments are fixed in the catalog. Amplification funding is distributed `A:3`, `B:2`, `C:3`, `D:2`, `E:3`, `F:2`, with functional funding as the five-edge complement. See the [Weapon Specification Index](./weapons/README.md) and [RES-006](./research/RES-006-resource-color-weapon-graph.md).

## Fabrication flow

1. Equip a signature or newly crafted weapon in one of four weapon slots.
2. Mine common basic ore and specialized ordinary resources.
3. Open fabrication, freezing the full gameplay simulation.
4. Select an equipped weapon.
5. Purchase any affordable individual stat rank at the weapon's shared next price, or purchase a compatible weapon branch.
6. Confirm an irreversible branch commitment when applicable.
7. Review the exact resulting stats and changed attack description before returning to play.

Upgrading a weapon never consumes another weapon slot. A full four-weapon loadout can continue improving every equipped weapon.

The mech cannot equip a duplicate weapon. Fabrication shows an already-equipped weapon as unavailable for another copy rather than allowing it to consume a second slot. Fabricating a new weapon permanently fills an empty weapon slot for the run; the preview and confirmation must make that commitment clear.

## Relationship to randomized geology

Common basic ore provides a universal improvement floor for every equipped weapon. Specialized-resource availability determines which additional base weapons can be fabricated and which major branches are practical. Because the mech is selected before the geological survey appears, generation guarantees that at least two of the signature weapon's three branch resources occur in the map's four-color profile. Its remaining branch may be unavailable, and its common-ore stat growth remains useful regardless of geology.

No one specialized resource should correspond to exactly one obviously mandatory weapon branch. Resources should support overlapping branches or roles so the geological survey creates planning choices instead of solving the build automatically.

## Mech relic relationship

The mech has one separate **relic slot** for an exploration-found modifier that can significantly change several weapons or another major gameplay rule at once. This system is distinct from:

- A weapon's fixed bundle of ore-upgradeable stats.
- Its weapon-specific specialized-resource branches.
- The mech's three utility slots.

Relics are found on the map and are not fabricated. They favor unusual rules, tradeoffs, or geometry changes over obvious unconditional stat increases. See [Mech Relics](./67-mech-relics.md) for the accepted discovery, sale, installation, and replacement rules and remaining content questions.

## Feedback requirements

The fabrication interface must communicate:

- Current value, fixed next-rank increase, and resulting value for every upgradeable stat.
- Shared weapon upgrade depth, common next-rank ore cost, price escalation, and remaining ore.
- Which branches are compatible, affordable, unavailable, or already excluded.
- Which branch commitment will exclude which alternatives before confirmation.
- The branch's transformation category, substantial output change, and any attack-pattern change—not merely its internal stat changes.
- Whether a choice is reversible before confirmation and whether it is reversible later.
- The weapon's complete current configuration after every purchase.

Stat names and comparisons must remain understandable without requiring knowledge of hidden formulas.

## Balance intent

- All numeric assignment and revision follows the [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md). Ideal single-target DPS anchors arithmetic, while horde throughput, boss reliability, coverage, setup, control, safety, and positional burden remain separately measured.
- The [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md) is the authoritative first-playable assignment of base values, fixed properties, stat increments, branch numbers, caps, and weapon-specific edge rules.
- Common-ore ranks should create frequent useful spending without making cheap early ranks in every stat the automatic allocation.
- Nonlinear shared-depth prices should make extreme investment in one weapon increasingly inefficient without forbidding either specialization or balanced allocation.
- Stat labels and units must make the promised fixed linear gain truthful and understandable.
- Every branch must be a larger upgrade than an ordinary stat rank. The amplification branch may preserve positioning and routing, while the functional variant and especially the playstyle conversion should change how the weapon is used.
- A player should be able to improve power before each interval boss without completing a full branch.
- The fixed two-unit branch cost should make geode routing meaningful without making a supported build depend on unusually scarce map supply.
- Fabrication should allow several purchases in one visit without forcing repeated menu exits.
- The initial catalog has no follow-on branch upgrades. One two-unit irreversible branch is the complete specialized-material transformation for that weapon; common ore supplies further depth.

## Open questions

- [OQ-013 — What resource types exist, and what does each purchase?](./open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)

## Related documents

- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#fabrication)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Specialized Resource Identities](./61-specialized-resource-identities.md)
- [Mech Relics](./67-mech-relics.md)
- [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [DEC-008 — Use fixed fabrication rules with surveyed randomized resource profiles](./decisions/DEC-008-fixed-blueprints-randomized-resource-profiles.md)
- [DEC-023 — Use per-stat ore upgrades and specialized-resource weapon branches](./decisions/DEC-023-weapon-stat-and-branch-upgrades.md)
- [DEC-025 — Use uncapped linear stat ranks with nonlinear prices](./decisions/DEC-025-uncapped-linear-stat-ranks.md)
- [DEC-084 — Price stat upgrades by total weapon upgrade depth](./decisions/DEC-084-price-stat-upgrades-by-weapon-depth.md)
- [DEC-085 — Use a triangular shared-depth price curve](./decisions/DEC-085-use-triangular-shared-depth-prices.md)
- [DEC-027 — Make major weapon branches mutually exclusive](./decisions/DEC-027-mutually-exclusive-weapon-branches.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](./decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [DEC-036 — Use six-color signature-aware resource profiles](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-037 — Use unique weapons and soft profile balance](./decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](./weapons/README.md)
- [DEC-040 — Use a three-level weapon-branch transformation gradient](./decisions/DEC-040-three-branch-transformation-gradient.md)
- [DEC-041 — Use an equal-tier base-weapon catalog](./decisions/DEC-041-equal-tier-base-weapon-catalog.md)
- [DEC-044 — Use immediate permanent branch commitment](./decisions/DEC-044-immediate-permanent-branch-commitment.md)
- [DEC-047 — Limit weapons to three common-ore stats](./decisions/DEC-047-three-stat-weapon-bundles.md)
- [DEC-050 — Give Pulse Repeater damage, rate, and range stats](./decisions/DEC-050-pulse-repeater-stat-bundle.md)
- [DEC-053 — Give Gravity Projector damage, radius, and duration stats](./decisions/DEC-053-gravity-projector-stat-bundle.md)
- [DEC-059 — Give Cluster Mortar damage, radius, and rate stats](./decisions/DEC-059-cluster-mortar-stat-bundle.md)
- [DEC-060 — Assign native branch funding for catalog balance](./decisions/DEC-060-balance-native-branch-funding.md)
- [DEC-075 — Accept the complete initial weapon catalog for playtesting](./decisions/DEC-075-accept-complete-initial-weapon-catalog.md)
- [DEC-077 — Use ore seams and completion-only material geodes](./decisions/DEC-077-ore-seams-and-material-geodes.md)
- [DEC-100 — Commit installed weapons and utilities](./decisions/DEC-100-commit-installed-weapons-and-utilities.md)
- [DEC-124 — Adopt a multi-metric weapon balance framework](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
