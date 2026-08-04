---
doc_id: GDD-INITIAL-RELIC-CATALOG
title: Initial Relic Catalog
status: active
authoritative: true
---

# Initial Relic Catalog

## Catalog status

This document defines the ten accepted initial mech relic effects and their player-facing concept summaries. [DEC-118](./decisions/DEC-118-accept-initial-relic-catalog.md) accepts the catalog for prototyping and playtesting. Names and exact numerical values are working content subject to presentation and balance revision; the central behavior and tradeoff of each relic are authoritative until deliberately revised.

## Catalog principles

- Relics are deliberately powerful. Initial tuning should err toward an exciting install choice that can be reduced after testing rather than a subtle bonus the player cannot feel.
- Every relic creates a meaningful change in positioning, targeting, cadence, routing, risk, or build evaluation.
- Nine initial relics alter most or all equipped weapons. Claim-Jumper Core is the intentional mining-system exception.
- Every relic has a complete concept-level description that fits in one sentence. That sentence appears first in the discovery choice and must prepare the player for both the benefit and the tradeoff.
- Expanded details may explain affected weapons, numerical boundaries, and unusual interactions, but cannot reveal a major drawback absent from the one-sentence description.
- The discovery interface identifies which currently equipped weapons are affected and how before installation.
- Relics combine with weapon branches, stat ranks, utilities, mech traits, and PowerUps unless an entry explicitly overrides a rule.

## Catalog overview

| ID | Relic | Primary transformation | Core tradeoff |
| --- | --- | --- | --- |
| `REL-01` | Retrograde Engine | Triple weapon tempo | Directional and targeted attacks operate oppositely |
| `REL-02` | Ghostline Chassis | Delayed duplicate arsenal | Original weapon damage is reduced |
| `REL-03` | Dead-Reckoning Array | Facing-directed arsenal | Automatic aiming is removed |
| `REL-04` | Colossus Governor | Huge, long-lived attacks | Weapons activate much less often |
| `REL-05` | War-Drum Oscillator | Synchronized arsenal bursts | No weapon output between beats |
| `REL-06` | Event-Horizon Coupler | Pull and cluster amplification | Attacks compact the horde around their geometry |
| `REL-07` | Fission Seed | Kill-driven chain explosions | Direct weapon damage is reduced |
| `REL-08` | Redline Crucible | Stationary heat overclock | Redline heat burns Hull until vented through movement |
| `REL-09` | Claim-Jumper Core | Double extraction speed | Enemies move 50% faster during mining |
| `REL-10` | Sequential Reactor | Extreme rotating weapon phases | Only one equipped weapon operates at a time |

## Fresh-profile and unlocked availability

The fresh profile's random relic-cache pool contains five effects:

- Retrograde Engine
- Colossus Governor
- Event-Horizon Coupler
- Fission Seed
- Claim-Jumper Core

The other five enter the pool through permanent, nonrefundable Hyper Gold purchases:

| Relic | Unlock cost |
| --- | ---: |
| Ghostline Chassis | 250 |
| Dead-Reckoning Array | 250 |
| War-Drum Oscillator | 300 |
| Redline Crucible | 350 |
| Sequential Reactor | 400 |

Each purchase permanently adds the named relic to future random cache selection; it neither equips nor guarantees that relic. Owned relics cannot be disabled to narrow the pool. Standard maps still contain three caches, and every discovery retains the normal immediate install-or-sell choice. See the [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md).

## REL-01 — Retrograde Engine

**Discovery sentence:** “All weapons attack three times as fast, but directional and targeted attacks operate on the opposite side of the mech.”

- Multiplies each weapon's primary activation frequency by three after ordinary Attack Rate modifiers. This includes fixed-cadence weapons that do not normally expose Attack Rate as an ore statistic.
- Targeted shots and ground deployments first select normally, then mirror the chosen direction or target point through the mech at equal distance.
- Persistent-facing attacks fire backward. Forward Ram Field geometry moves behind the mech, movement-laid Wake geometry appears ahead of the moving mech, and orbital travel reverses direction.
- Autonomous weapons retain their ordinary target-selection priorities but approach or attack through the opposite side or orientation defined by their weapon-specific geometry.
- Mech-centered, radial, and otherwise directionless attacks remain centered and receive the tempo increase without inventing a meaningless opposite position.
- Distance-triggered attacks such as mines and wake segments use one-third their ordinary travel interval so the relic increases their production tempo rather than silently excluding them.
- Delayed echoes, projectile travel, finite effect durations, arming delays, and damage ticks do not accelerate unless they are explicitly the weapon's primary activation schedule.

## REL-02 — Ghostline Chassis

**Discovery sentence:** “Your weapons deal 80% damage, but a spectral mech following 1.5 seconds behind repeats all their attacks at 60% damage.”

- The ghost continuously retraces the mech's recorded position and persistent facing from 1.5 active-simulation seconds earlier.
- Every equipped weapon's ordinary output is attributed either to the mech at 80% damage or to its ghost copy at 60% damage. Both inherit the original weapon's stats, branch, targeting rules, cadence, geometry, and non-damage effects.
- The ghost supplies a delayed second origin for projectiles, fields, pulses, orbiting or contact weapons, movement paths, and autonomous attack events. It does not occupy a weapon slot, collide with enemies, mine, collect pickups, open caches, or take damage.
- Movement-shaped attacks reproduce the ghost's delayed route, making loops, reversals, and passes strategically different from standing near the same location.
- The maximum overlapping damage is intentionally greater than the original arsenal, but delayed spatial separation makes that power conditional on pathing and enemy movement.
- The 1.5-second delay and 80%/60% damage factors are initial tuning values.

## REL-03 — Dead-Reckoning Array

**Discovery sentence:** “Weapons stop automatically aiming and instead attack along your facing direction with greatly increased damage and Area.”

- Aimable weapons no longer select enemies or enemy concentrations. They fire, launch, or deploy along persistent mech facing using their ordinary range or placement distance.
- Homing attacks lose homing after launch. Autonomous weapon units orient their attacks along the mech's sampled facing rather than choosing their own targets.
- Mech-centered, orbiting, movement-laid, and other geometry that already lacks automatic aim keeps its normal origin and behavior.
- All weapon damage increases by 75%, and every dimension classified as weapon Area increases by 40%, including weapons whose geometry was already non-aimed.
- The relic adds no aim stick, cursor, fire button, or turn-in-place control. The player aims indirectly through the accepted movement-derived facing rule.

## REL-04 — Colossus Governor

**Discovery sentence:** “Weapons attack 60% less often, but every attack deals 2.5 times the damage and becomes dramatically larger and longer-lasting.”

- Each weapon produces primary activations at 40% of its otherwise final frequency.
- All weapon damage is multiplied by 2.5, weapon Area is doubled, and finite weapon-created durations are doubled.
- Continuous, contact, and movement-authored weapons accumulate their ordinary output and release it in slower, visibly emphatic cycles rather than bypassing the cadence tradeoff.
- Projectile travel speed, targeting range, placement range, knockback, control strength, actor capacity, and other properties do not increase unless their weapon specification derives them from Damage, Area, or Duration.
- The relic is intended to preserve roughly comparable raw sustained damage before the coverage and duration advantage while making misses, poor facing, and badly placed attacks much more costly.

## REL-05 — War-Drum Oscillator

**Discovery sentence:** “All weapon output is stored and unleashed together every three seconds with 35% bonus power.”

- Weapons do not deal damage or apply control between global three-second beats.
- Each weapon continues accumulating the attacks, movement triggers, or continuous output it would ordinarily have produced, up to two beats of stored output.
- On each beat, every stored weapon releases its accumulated output in a synchronized burst from the mech's current position and facing, using the current valid target state.
- Released damage and other scalable weapon output receive a 35% bonus. The beat does not erase ordinary targeting, branch behavior, capacity rules, or attack geometry.
- The HUD, audio, and mech effect telegraph the next beat clearly enough for the player to approach, face, or retreat around it.
- Fabrication and other full-simulation pauses freeze the beat and stored-output state.

## REL-06 — Event-Horizon Coupler

**Discovery sentence:** “Every weapon pulls enemies into its attacks, and enemies clustered together take 50% more weapon damage.”

- Every damaging weapon hit applies an inward displacement toward the nearest meaningful centerline, impact point, field center, projectile path, or other visible center of that attack's geometry.
- Existing weapon-authored knockback reverses into pull instead of applying both directions. Non-displacement control such as slow or stun remains unchanged.
- An enemy within the displayed clustering distance of at least one other living enemy takes 50% more weapon damage.
- Pull resistance continues to scale for elites and bosses, but the clustered-damage rule applies whenever their spatial condition is satisfied.
- The relic makes piercing, area, and persistent attacks exceptionally effective while intentionally compacting pursuers into more dangerous concentrations.

## REL-07 — Fission Seed

**Discovery sentence:** “Weapons deal 35% less direct damage, but enemies they kill explode and can trigger diminishing chain reactions.”

- All direct and persistent damage attributed to equipped weapons is multiplied by 0.65.
- An enemy defeated by weapon damage explodes after a short readable delay. Explosion strength and Area scale from that enemy's maximum Hull, subject to a cap for elites and bosses.
- A fission explosion can defeat other enemies and create further explosions. Each generation deals less damage and has smaller Area than the generation that caused it until the chain ends.
- Explosions are attributed to the relic for statistics but count as weapon-caused defeats for compatible run rules. They cannot damage the player, mining points, caches, pickups, or destructible rocks.
- The relic is intentionally powerful against dense ordinary hordes and comparatively poor against an isolated boss without nearby chain fuel.
- Exact explosion scaling, generational decay, delay, and boss cap remain numerical tuning.

## REL-08 — Redline Crucible

**Discovery sentence:** “Remaining near the same position heats and strengthens every weapon, but maximum heat burns Hull until movement vents it.”

- The relic adds a visible heat meter. Remaining within a small tolerance around the same world position builds heat; sustained travel outside that tolerance vents it.
- Heat scales continuously from no bonus to a maximum of +100% weapon Damage, +50% Attack Rate, and +50% weapon Area.
- At maximum heat, the mech loses 3 Hull Integrity per active-simulation second and receives no passive Recovery until heat falls below redline.
- Redline damage cannot be negated by Armor or a hit-negation utility and can kill the mech. Health packs can still restore Hull.
- Mining does not itself generate heat; the player's limited movement inside an extraction zone determines whether heat builds or vents.
- Heat, damage, and venting freeze during full-simulation pauses. Exact positional tolerance and heating and venting rates remain tuning.

## REL-09 — Claim-Jumper Core

**Discovery sentence:** “Mining is twice as fast, but all enemies move 50% faster while you are actively mining.”

- Doubles forward extraction rate for standard seams, rich seams, material geodes, and Hyper Gold sites after ordinary extraction modifiers.
- Mining decay remains four times the mech's current forward extraction rate, preserving the normal proportional leave-and-decay rule.
- Every living enemy receives +50% movement speed while extraction progress is actively advancing. The increase begins with forward progress and ends immediately when mining stops because the mech leaves, the point completes, or the simulation pauses.
- The speed increase affects ordinary enemies, specialists, elites, bosses, and already-summoned threat-beacon enemies. It does not accelerate their attack cadence, projectile speed, status durations, or spawn schedule.
- Completed installments and payouts remain unchanged. The relic compresses positional exposure rather than increasing resource yield.

## REL-10 — Sequential Reactor

**Discovery sentence:** “Only one weapon operates at a time, rotating every four seconds, but the active weapon receives five times its normal output.”

- The active weapon advances through occupied weapon slots in stable loadout order every four active-simulation seconds. Empty slots are skipped.
- Only the active weapon may begin new attacks, deal continuous or contact damage, apply new control, or create movement-triggered output.
- The active weapon produces five times its otherwise final output rate. For discrete attacks this ordinarily means five times the activation frequency; continuous, contact, and distance-triggered weapons receive an equivalent visible output-rate mapping.
- Finite projectiles, mines, fields, trails, and autonomous attacks created during an active phase remain present and finish their already-authored behavior after the source weapon rotates inactive, but they cannot begin new autonomous attack events while inactive.
- The HUD shows the active weapon, remaining phase time, and next weapon. Fabrication and other full-simulation pauses freeze phase time.
- The five-times factor intentionally makes the relic a raw-power increase with a severe coverage and timing constraint, including when fewer than four weapon slots are occupied.

## Shared interaction and preview rules

- Relic-specific multipliers apply after the additive percentage modifiers supplied by mech traits, utilities, and PowerUps unless the relic explicitly replaces targeting or cadence.
- A relic's damage multiplier applies uniformly to every damaging component derived from an equipped weapon, including branches, mines, drones, pods, delayed attacks, and persistent zones.
- Already-created finite weapon instances retain the relic state and values with which they were created unless an entry explicitly describes ongoing activity that can become inactive.
- Installing or replacing a relic during the paused discovery screen updates all future weapon behavior after play resumes; no gameplay timer advances during the transition.
- The discovery screen shows the one-sentence concept first, followed by exact values, affected equipped weapons, important branch interactions, and any weapon that receives only part of the effect.
- Current effective weapon details identify relic overrides so a player never has to compare hidden rules against the base catalog from memory.

## Content and tuning still open

- Final names, models, icons, color language, audio, and activation effects.
- Exact numeric tuning explicitly identified above.
- Per-weapon effect mappings that prototype testing reveals cannot use the shared interaction rule cleanly, provided those mappings preserve each accepted discovery sentence.

DEC-127 fixes cache selection without replacement, duplicate exclusion, no dedicated guards, in-view signaling, the discovery comparison, and a 150-common-ore sale value independent of installed duration.

## Related documents

- [Mech Relics](./67-mech-relics.md)
- [Weapon Specification Index](./weapons/README.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md)
- [DEC-028 — Use one exploration-found mech relic](./decisions/DEC-028-one-exploration-found-mech-relic.md)
- [DEC-118 — Accept the initial relic catalog](./decisions/DEC-118-accept-initial-relic-catalog.md)
- [DEC-121 — Accept the initial option-unlock catalog](./decisions/DEC-121-accept-initial-option-unlock-catalog.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#relic-cache-discovery-and-resolution)
