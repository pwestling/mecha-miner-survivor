---
doc_id: GDD-PERMANENT-POWERUP-CATALOG
title: Permanent PowerUp Catalog
status: active
authoritative: true
---

# Permanent PowerUp Catalog

## Catalog status

This document defines the accepted initial account-wide PowerUp catalog, rank effects, caps, Hyper Gold prices, stacking, activation, refund behavior, and explicit exclusions. [DEC-120](./decisions/DEC-120-accept-permanent-powerup-catalog.md) accepts the catalog for progression and difficulty playtesting. Exact values may be revised as a complete economy pass, but the categories and their player-facing boundaries are the baseline.

## Design relationship to the genre reference

The catalog adopts the useful permanent-progression categories associated with *Vampire Survivors*: damage, attack cadence, area, effect duration, maximum health, armor, recovery, movement speed, and revival. It translates them into the mech vocabulary and adds discovery and mining improvements because exploration and extraction replace XP collection as the center of this game.

The catalog deliberately does not import Growth, Greed, Magnet, Luck, Curse, Reroll, Skip, Banish, projectile Amount, or a universal projectile-speed bonus. XP, coin farming, random level-up offerings, and chest fishing do not exist in the standard progression loop; contact collection, fixed weapon counts, and intentionally fixed projectile behavior are established gameplay constraints rather than missing permanent stats.

## Shared purchase rules

- PowerUps are purchased only between runs with banked Hyper Gold.
- Every category is visible and purchasable on a fresh profile. Categories have no prerequisite tree, account-level gate, random availability, or purchase-order requirement.
- Ranks within one category must be bought sequentially. Buying another category never raises this category's price.
- Every listed price is fixed. There is no global price inflation based on total ranks owned, mech selection, success count, or current bank balance.
- A purchased rank applies account-wide to every current and future mech.
- Purchasing a rank raises both the purchased maximum and the category's active rank to the new value.
- Between runs, the player may set each category's active rank to any value from zero through its purchased rank at no cost. This supports challenge play and testing without surrendering ownership.
- **Refund PowerUps** remains the full respec command: it resets every purchased and active PowerUp rank to zero and returns the exact Hyper Gold actually paid. It has no fee, cooldown, or usage limit.
- Permanent content and option unlocks are separate, nonrefundable purchases and are unaffected by active-rank changes or Refund PowerUps.
- PowerUps cannot be bought, activated, deactivated, ranked down, or refunded during an active run.

## Catalog overview

| ID | Domain | PowerUp | Per-rank effect | Cap | Maximum effect | Total cost |
| --- | --- | --- | --- | ---: | --- | ---: |
| `PU-C01` | Combat | Weapons Calibration | +3% weapon Damage | 5 | +15% | 675 |
| `PU-C02` | Combat | Cycle Optimizer | +2% weapon Attack Rate | 5 | +10% | 975 |
| `PU-C03` | Combat | Field Geometry | +3% weapon Area | 5 | +15% | 675 |
| `PU-C04` | Combat | Persistence Lattice | +4% weapon Duration | 4 | +16% | 425 |
| `PU-S01` | Survivability | Hull Reinforcement | +5 maximum Hull Integrity | 5 | +25 | 675 |
| `PU-S02` | Survivability | Ablative Armor | +1 Armor | 3 | +3 | 700 |
| `PU-S03` | Survivability | Repair Nanites | +0.03 Hull/s Recovery | 5 | +0.15 Hull/s | 825 |
| `PU-S04` | Survivability | Emergency Reboot | One revival per run | 1 | One revival | 500 |
| `PU-M01` | Mobility | Servo Overdrive | +2% movement speed | 5 | +10% | 975 |
| `PU-M02` | Mobility | Survey Optics | +5% discovery radius | 5 | +25% | 400 |
| `PU-E01` | Mining/economy | Extraction Tuning | +2% forward extraction rate | 5 | +10% | 975 |
| `PU-E02` | Mining/economy | Tether Amplifier | +3% extraction-zone radius | 5 | +15% | 675 |
| `PU-E03` | Mining/economy | Ore Assay | +3% mined common ore | 5 | +15% | 975 |

The complete initial numerical catalog costs **9,450 Hyper Gold**. A perfect standard run can bank at most 400, so fully maximizing it requires at least 24 perfect collections and ordinarily more. Early ranks cost 25–125 Hyper Gold, allowing a player who extracts with only part of the map's available currency to make a useful purchase.

## Combat PowerUps

### PU-C01 — Weapons Calibration

| Rank | Total weapon Damage | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +3% | 50 | 50 |
| 2 | +6% | 75 | 125 |
| 3 | +9% | 125 | 250 |
| 4 | +12% | 175 | 425 |
| 5 | +15% | 250 | 675 |

- Applies to damage attributed to every equipped weapon, including weapon-created mines, drones, pods, delayed attacks, persistent zones, and branches.
- Uses the same weapon-attribution boundary as Harmonic Calibrator and Pike's Heavy Calibration.
- Does not increase relic-only explosions, enemy damage, environmental hazards, mining effects, or temporary field effects not attributed to a weapon.

### PU-C02 — Cycle Optimizer

| Rank | Total weapon Attack Rate | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +2% | 75 | 75 |
| 2 | +4% | 125 | 200 |
| 3 | +6% | 175 | 375 |
| 4 | +8% | 250 | 625 |
| 5 | +10% | 350 | 975 |

- Applies to primary weapon activation frequency using the same boundaries as Cycle Capacitor and Kestrel's Accelerated Feed.
- Does not accelerate projectile travel, damage ticks inside an existing effect, delayed echoes, mine arming, autonomous movement, relic cycles, mining, Recovery, or enemy timers unless a weapon explicitly classifies that schedule as Attack Rate.
- The displayed value is increased attacks per second, not percentage cooldown subtraction.

### PU-C03 — Field Geometry

| Rank | Total weapon Area | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +3% | 50 | 50 |
| 2 | +6% | 75 | 125 |
| 3 | +9% | 125 | 250 |
| 4 | +12% | 175 | 425 |
| 5 | +15% | 250 | 675 |

- Applies to scalable weapon radii, widths, blast areas, projectile bodies, cones, and persistent damage zones using the same mapping as Field Expander and Lodestar's Field Geometry trait.
- Does not change targeting range, placement range, travel distance, orbit radius, discovery radius, pickup radius, mining zones, resonance fields, or warning markers unless a weapon explicitly classifies that dimension as Area.

### PU-C04 — Persistence Lattice

| Rank | Total weapon Duration | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +4% | 50 | 50 |
| 2 | +8% | 75 | 125 |
| 3 | +12% | 125 | 250 |
| 4 | +16% | 175 | 425 |

- Applies only to finite lifetimes explicitly classified as weapon Duration: persistent fields, wakes, deployable pods, mines, temporary drones, branch-created zones, and similar instances.
- Does not alter attack cadence, projectile travel time, targeting delay, control-effect duration, mining grace or progress, relic timing, utility recharge, boss warnings, or run time.
- Some equipped weapons may have no Duration-bearing component. Mech selection and fabrication display affected current systems rather than implying universal benefit.

## Survivability PowerUps

### PU-S01 — Hull Reinforcement

| Rank | Total maximum Hull | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +5 | 50 | 50 |
| 2 | +10 | 75 | 125 |
| 3 | +15 | 125 | 250 |
| 4 | +20 | 175 | 425 |
| 5 | +25 | 250 | 675 |

- Adds flat maximum Hull Integrity to the account baseline. A run begins at the resulting full Hull value.
- Bastion's flat +25 trait and Reinforced Bulkhead apply afterward under their accepted flat-stat rules.
- It does not supply Armor, Recovery, repair amplification, or damage immunity.

### PU-S02 — Ablative Armor

| Rank | Total Armor | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +1 | 125 | 125 |
| 2 | +2 | 225 | 350 |
| 3 | +3 | 350 | 700 |

- Each Armor point subtracts one Hull from every eligible incoming contact, projectile, or hazard instance, to a minimum of one damage.
- Explicit Armor-ignoring effects remain unaffected. Redline Crucible's self-damage remains Armor-ignoring under its relic rule.
- The low cap prevents dense low-damage hordes from becoming harmless.

### PU-S03 — Repair Nanites

| Rank | Total Recovery | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +0.03 Hull/s | 50 | 50 |
| 2 | +0.06 Hull/s | 100 | 150 |
| 3 | +0.09 Hull/s | 150 | 300 |
| 4 | +0.12 Hull/s | 225 | 525 |
| 5 | +0.15 Hull/s | 300 | 825 |

- Adds continuous passive Recovery to the account baseline and stacks additively with Repair Swarm and any future explicit Recovery source.
- Cannot exceed maximum Hull and freezes during full-simulation pauses.
- Does not increase the value of health packs or restore Hull after run end.

### PU-S04 — Emergency Reboot

| Rank | Revival charges | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | 1 per run | 500 | 500 |

- The first time the mech would reach zero Hull Integrity, it instead restores 40% of its current maximum Hull and gains two active-simulation seconds of invulnerability.
- Reboot emits a non-damaging radial displacement that clears ordinary enemies from immediate overlap. Elites and bosses receive their normal displacement resistance.
- It activates automatically, consumes its one charge, does not pause the timer or simulation, and preserves current mining, weapons, resources, bosses, beacon thresholds, and run state.
- It cannot restore a charge during the run and does not bank Hyper Gold by itself. A second lethal event causes ordinary death unless another explicitly equipped revival exists.
- The HUD displays the unused or spent charge. If several revival effects ever coexist, the pre-run and active details show their fixed consumption order.

## Mobility and exploration PowerUps

### PU-M01 — Servo Overdrive

| Rank | Total movement speed | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +2% | 75 | 75 |
| 2 | +4% | 125 | 200 |
| 3 | +6% | 175 | 375 |
| 4 | +8% | 250 | 625 |
| 5 | +10% | 350 | 975 |

- Applies equally in every direction without adding acceleration, momentum, a turn radius, reverse penalties, sprint, dash, or stamina.
- Adds with Razorback's Overdrive Treads, Vector Thrusters, and other explicit movement-speed percentages.
- Does not alter weapon projectile speed, enemy movement, mining speed, or the base-travel distances used by map generation.

### PU-M02 — Survey Optics

| Rank | Total discovery radius | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +5% | 25 | 25 |
| 2 | +10% | 50 | 75 |
| 3 | +15% | 75 | 150 |
| 4 | +20% | 100 | 250 |
| 5 | +25% | 150 | 400 |

- Expands ordinary terrain-fog reveal and discovery of landmarks, mining points, relic caches, and destructible rocks using the same boundary as Survey Aperture.
- Does not reveal through blocked terrain when ordinary visibility would not, disclose exact hidden locations, add radar directions, reveal absent materials, or mark undiscovered objects globally.
- Previously revealed terrain and markers remain recorded normally after leaving the enlarged radius.

## Mining and run-economy PowerUps

### PU-E01 — Extraction Tuning

| Rank | Total forward extraction rate | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +2% | 75 | 75 |
| 2 | +4% | 125 | 200 |
| 3 | +6% | 175 | 375 |
| 4 | +8% | 250 | 625 |
| 5 | +10% | 350 | 975 |

- Applies to standard seams, rich seams, material geodes, and Hyper Gold sites.
- Mining decay remains four times the current forward extraction rate, preserving the normal proportional retreat penalty.
- Stacks additively with Prospector and Extraction Accelerator. Claim-Jumper Core's doubling applies after their combined additive extraction modifier.
- Does not change payout, installment checkpoints, leaving grace, beacon thresholds, resonance fields, or enemies already summoned.

### PU-E02 — Tether Amplifier

| Rank | Total extraction-zone radius | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +3% | 50 | 50 |
| 2 | +6% | 75 | 125 |
| 3 | +9% | 125 | 250 |
| 4 | +12% | 175 | 425 |
| 5 | +15% | 250 | 675 |

- Expands the visible automatic-mining boundary for every mining-point class and stacks additively with Extraction Tether.
- Does not change extraction speed, payouts, discovery, resource radar behavior, threat-beacon reach, or resonance-field size.
- The map-generation separation and base-travel rules continue using the unmodified baseline mining-zone size.

### PU-E03 — Ore Assay

| Rank | Total mined common ore | Price | Cumulative cost |
| ---: | ---: | ---: | ---: |
| 1 | +3% | 75 | 75 |
| 2 | +6% | 125 | 200 |
| 3 | +9% | 175 | 375 |
| 4 | +12% | 250 | 625 |
| 5 | +15% | 350 | 975 |

- Applies to common ore installments from standard and rich seams and to the 50-ore material-geode completion payout.
- Stacks additively with Ore Catalyzer. Fractional bonus ore carries forward and pays one whole ore whenever the accumulated fraction reaches one.
- Does not duplicate specialized materials, increase Hyper Gold, modify boss loot, improve relic sale value, affect refunds, or change other non-mining awards.
- It strengthens run-local fabrication but cannot produce resources without exploration and active mining.

## Modifier order and displayed values

1. Begin from the shared account baseline.
2. Apply active flat PowerUps such as Hull Reinforcement and Ablative Armor.
3. Apply the selected mech's explicit flat differences.
4. Add percentage modifiers with the same named statistic across active PowerUps, mech traits, utilities, and other ordinary sources.
5. Apply explicit multiplicative relic replacements or transformations after the additive total.

The hangar, mech selection, fabrication, pause, and expanded statistics surfaces show the final effective values relevant to their context. A percentage is never silently applied to a system excluded by its detailed definition.

## Fully upgraded account envelope

With every PowerUp active, before selecting a mech or acquiring run-local equipment, the account receives:

- +15% weapon Damage, +10% Attack Rate, +15% Area, and +16% applicable weapon Duration;
- +25 maximum Hull, +3 Armor, +0.15 Hull/s Recovery, and one Emergency Reboot;
- +10% movement speed and +25% discovery radius; and
- +10% extraction rate, +15% extraction-zone radius, and +15% mined common ore.

This is intentionally a substantial early-run advantage. It still supplies no additional starting weapon, utility, relic, specialized material, common ore, Hyper Gold, map knowledge, automatic mining, automatic navigation, or immunity to late positioning. A mature four-weapon, three-utility, branched and ore-upgraded run build remains the larger source of power.

## Interface requirements

- Each card shows domain, current active rank, purchased rank, cap, total effect, next-rank effect, exact price, and post-purchase bank balance.
- The detail view lists affected and excluded systems in player language.
- Maxed categories are visibly complete and cannot consume more Hyper Gold.
- Active-rank controls are distinct from purchasing and refunding so reducing an active rank cannot accidentally surrender ownership.
- Refund PowerUps shows the exact returned total and requires confirmation; it cannot affect permanent option unlocks.
- The hangar summarizes active PowerUps before deployment, and the results screen records the active ranks used for the completed run.

## Balance validation

- Compare first-boss time-to-kill, damage taken, mining completion rate, ore spent, boss overlap, and extraction success on fresh, partial, and fully upgraded profiles.
- Confirm that the full catalog substantially eases minutes 0–14 without making minutes 28–35 safe while standing still or using only the signature weapon.
- Test each mech and all 15 resource profiles with every PowerUp active; the director never counter-scales against the account.
- Confirm that extraction and ore upgrades improve routing tolerance without making Hyper Gold sites, geodes, or exploration optional.
- Emergency Reboot should rescue a plausible run and feel valuable without becoming a required standard completion prerequisite.
- Price testing must include competition with the accepted 2,150-Hyper-Gold [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md) and the realistic banked Hyper Gold distribution, not only the 400-unit theoretical maximum.

## Related documents

- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Initial Relic Catalog](./69-initial-relic-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#hangar-and-between-run-flow)
- [DEC-092 — Use Hyper Gold for power and option unlocks](./decisions/DEC-092-use-hyper-gold-for-power-and-option-unlocks.md)
- [DEC-093 — Make permanent power account-wide](./decisions/DEC-093-make-permanent-power-account-wide.md)
- [DEC-094 — Allow free PowerUp refunds](./decisions/DEC-094-allow-free-powerup-refunds.md)
- [DEC-095 — Include mining and economy PowerUps](./decisions/DEC-095-include-mining-and-economy-powerups.md)
- [DEC-112 — Bound permanent power below run-build power](./decisions/DEC-112-bound-permanent-power-below-run-build-power.md)
- [DEC-120 — Accept the permanent PowerUp catalog](./decisions/DEC-120-accept-permanent-powerup-catalog.md)
- [DEC-121 — Accept the initial option-unlock catalog](./decisions/DEC-121-accept-initial-option-unlock-catalog.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
- [RES-001 — Vampire Survivors reference mechanics](./research/RES-001-vampire-survivors-reference.md)
