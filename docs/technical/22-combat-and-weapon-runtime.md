---
doc_id: TDD-COMBAT-RUNTIME
title: Combat and Weapon Runtime
status: active
authoritative: true
---

# Combat and Weapon Runtime

## Purpose

This document defines automatic attack scheduling, target selection, weapon behavior registration, damage and control resolution, provenance, relic integration, rock targeting, pooling, and weapon verification. Exact player-facing values and edge rules remain authoritative in the [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md).

## Ownership boundary

Each occupied weapon slot owns one weapon runtime state keyed by stable weapon content ID, slot index, branch ID if installed, three stat ranks, shared upgrade depth, activation phase, and behavior-specific state. The mech-wide loadout owns cross-weapon modifiers and relic state.

- A slot begins with a ready state unless the weapon definition specifies authored setup.
- Fabricating a weapon creates one runtime and permanently occupies the slot for the run.
- Stat and branch transactions update loadout state atomically while paused.
- Existing finite actors retain creation-time values; future activations use the new derived loadout.
- Persistent autonomous actors read updated attack values for attacks begun after the transaction.
- Removing or duplicating a weapon is unsupported by the initial command surface.

## Behavior implementation strategy

Use a registry from stable weapon ID to a dedicated behavior implementation composed from reusable geometry, scheduling, target, actor, damage, and control services. Do not build a general visual scripting language or attempt to express every branch through reflection.

Content data owns numbers, asset IDs, stat labels, and compatible parameter switches. C# behavior owns mechanics whose state transitions or geometry are unique. A behavior and each branch must have explicit registration validation; unregistered content is a build error.

Shared primitives include:

- rate accumulator and cooldown schedule;
- nearest, priority, concentration, facing, radial, and fallback-rock targeting;
- finite projectile, homing projectile, hitscan trace, beam, sector volley, chain, circle pulse, delayed impact, persistent field, trail segment, orbit contact, deployable, drone, mine, and explosion;
- per-target repeat interval and attack-local hit set;
- control application, pull center, knockback, slow, hard control, and projectile interception;
- delayed repeat and stored-output queues; and
- capacity-limited actor collections with stable oldest-first replacement.

## Initial behavior registry

| Weapon | Primary runtime model | Persistent state and special obligations |
| --- | --- | --- |
| `W-AB` Rail Lance | fast finite piercing projectile/trace along selected direction | ordered hit list, fixed base pierce; branch shockwaves, charge-by-travel, unlimited pierce |
| `W-AC` Cluster Mortar | choose ground target, schedule delayed circular impact | pending shells; seeded secondary blasts, lingering field, danger-close center replacement |
| `W-AD` Gravity Projector | targeted persistent circle with damage ticks and inward pull | active fields; delayed echo, end burst/launch, collection mass and singularity cycle |
| `W-AE` Attack Drones | three persistent autonomous actors that reposition and fire | actor transforms and targets; temporary clone cap, shared focus, rotating containment links |
| `W-AF` Tracking Laser | continuous target lock and damage-rate accumulator | target identity and focus; memory decay, exposure debuff, facing beam hysteresis |
| `W-BC` Pulse Repeater | repeated nearest-target projectile | projectile actors; hitscan replacement, recent-hit preference/slow, fixed lateral pair |
| `W-BD` Mine Layer | distance-traveled production, arming, proximity detonation | parent and child capacities; arming/lifetime, selective population trigger, hunter state |
| `W-BE` Sentry Pod | timed deployment of capacity-limited persistent turrets | creation order, life, target/fire state; overclock count, guardian priority, anchored bastion packing |
| `W-BF` Orbital Cutters | four analytic orbit actors with per-target contact cadence | orbit phase and contact memory; flywheel stacks, projectile interception, delayed-path reaper |
| `W-CD` Arc Emitter | discrete chain chosen from spatial candidates | ordered no-repeat chain; unlimited dense chain, hard control, moving ball-lightning actor |
| `W-CE` Reactor Pulse | periodic mech-centered circle | pulse phase; victim charge, push/slow, long-charge supernova cycle |
| `W-CF` Wake Projector | distance-traveled trail-segment production | path sampling and overlap cap; movement stacks, enemy-carried trails, loop detection/consumption |
| `W-DE` Scatter Array | facing/targeted five-projectile sector volley | volley identity; all-target cone wave, once-per-volley control, focal convergence |
| `W-DF` Ram Field | persistent facing-aligned contact capsule/rectangle | per-target contact cadence; movement stacks, transferred launched-enemy collision, stationary ring |
| `W-EF` Missile Rack | four-missile homing salvo | missile target/turn state; split children, reserve queue, rotating radial spiral |

The base/branch CSV mirrors are useful test inputs but subordinate to the full gameplay catalog. Every registry entry links its behavior fixtures to the corresponding catalog section and validates that all three branch IDs are implemented.

## Attack scheduling

Discrete weapons use a rate accumulator measured in activations per simulation second.

- A ready weapon begins according to its catalog readiness rule and does not receive a random phase.
- Each tick adds the final activation rate divided by simulation frequency.
- Every whole accumulated activation becomes an ordered attack request; the remainder is retained.
- If a multiplier creates more than one activation in one tick, requests resolve in stable sub-sequence without sharing mutable hit sets.
- A weapon with no valid target follows its catalog rule: wait ready, fire along facing, deploy from movement, or continue autonomous behavior. It never burns target-required activations invisibly unless specified.
- Primary activation rate, actor attack rate, damage tick rate, travel trigger, delayed echo, arming time, and lifetime are distinct schedules. A modifier changes only the schedules it names.

Continuous damage accumulates fractional damage budget and emits whole or content-authorized fractional damage events at its authored cadence. Player Hull damage remains rounded by the incoming-damage pipeline; weapon damage to enemies retains sufficient precision for balance measurements and HUD summary rounding.

## Target acquisition

Every target request declares:

- allowed categories and faction;
- range/shape and terrain occlusion policy;
- required visibility if any;
- priority policy;
- maximum results and no-repeat window;
- current target retention/hysteresis; and
- whether destructible rocks are fallback candidates.

Candidate collection uses the spatial index. Selection applies the authored priority keys, then distance squared, then entity ID unless a behavior defines another explicit stable ordering. No enumeration order, hash order, render visibility, or frame rate affects targeting.

Enemy-targeting weapons consider living enemies and bosses first. A rock becomes an eligible fallback only when no valid enemy lies in that weapon's acquisition domain. Geometric attacks may hit rocks incidentally. Rocks never consume enemy-only chain slots, kill triggers, focus, stacks, charge, exposure, replication, momentum, or statistics.

Target loss is evaluated after movement and before scheduled attacks. Beams and homing actors use behavior-specific retention hysteresis to prevent tick-level flicker; all thresholds are content-defined and covered by fixtures.

## Attack provenance

Every damaging or controlling event carries immutable provenance:

- run entity source and owning mech;
- weapon slot and base weapon ID;
- branch and relic IDs active at creation;
- actor/attack instance and chain generation;
- damage category: direct, persistent, secondary, relic, enemy, boss, hazard, or self-damage;
- whether the target is an enemy, boss, or rock; and
- flags controlling kill credit, proc eligibility, statistics, and recursive effects.

Provenance follows delayed actors after the source weapon rotates inactive or the relic changes. It prevents branch explosions, clones, trails, and fission chains from accidentally receiving recursive bonuses they exclude.

## Damage pipeline against enemies and rocks

For each damage event:

1. Reject dead, invalid, untargetable, already-hit, or geometry-ineligible targets.
2. Resolve source-side snapshot/live modifiers and relic reductions or amplification.
3. Apply target-state multipliers such as Barysteel resonance or Event-Horizon clustering.
4. Apply any content-defined per-target cap, falloff, or generation decay.
5. Subtract the nonnegative final damage from Hull.
6. Record attempted, effective, overkill, source, and target values.
7. Emit hit/control events.
8. Queue death once if Hull reaches zero.

Enemies currently have no Armor. Rocks ignore resonance, control, kill effects, and enemy-specific counters. Damage arithmetic remains floating point for enemies; death comparison uses nonpositive Hull with a small centrally defined numeric tolerance only to absorb calculation noise, never to change displayed tuning.

## Incoming player damage

Player damage implements the exact sequence from the [survivability baseline](../72-player-survivability-and-damage-baseline.md#incoming-damage-resolution): attacker eligibility, attacker-side multiplier, ceiling to whole Hull, Armor with minimum one unless ignored, full-hit negation, Hull subtraction, then revival/death.

- Same-attacker contact repeat is 0.75 active seconds before modifiers.
- Resolved or shield-negated contact starts the 0.20-second global contact grace.
- Contact grace does not block projectiles, boss landings, hazards, or relic self-damage.
- Emergency Reboot invulnerability precedes hit-negation consumption as specified.
- Every sequential instance records Armor prevented, shield result, source, resonance, prior Hull, and resulting Hull.

## Control and status runtime

Each enemy stores current strongest slow, hard-control remaining time, post-control immunity, and any continuous displacement contributors.

- Resistance scales displacement magnitude and timed-control duration exactly as the gameplay baseline states.
- Driftmetal resonance applies its separate 0.80 factor after inherent resistance.
- Positive resolved timed control below 0.05 seconds becomes the minimum visible duration.
- Slows select strongest magnitude and longest remaining duration; they do not add.
- Stun and stagger share the hard-control family and select longest duration.
- Post-control immunity is 0.25 seconds ordinary, 0.75 elite, and 1.50 boss.
- Hard control never cancels boss warning, charge, burst, leap, or cooldown state machines.
- Pull and knockback remain separate from hard-control immunity and obey geometry/terrain correction.

Control events carry authored and resolved values for balance telemetry.

## Relic integration

Relics are cross-cutting policies registered by stable relic ID. They modify well-defined hook points rather than scattering ID checks through every weapon.

| Hook | Relics initially using it |
| --- | --- |
| activation-rate transformation and opposite geometry | Retrograde Engine |
| delayed transform history and attack duplication | Ghostline Chassis |
| targeting replacement and facing conversion | Dead-Reckoning Array |
| cadence/damage/area/duration transformation | Colossus Governor |
| output capture and global beat release | War-Drum Oscillator |
| per-hit pull and clustered target multiplier | Event-Horizon Coupler |
| direct-damage reduction and generational death explosion | Fission Seed |
| position-history heat and conditional modifiers/self-damage | Redline Crucible |
| mining-rate and conditional enemy-speed transformation | Claim-Jumper Core |
| weapon-slot activation gate and rotating phase | Sequential Reactor |

Each weapon has a relic-compatibility manifest produced from tests, not a manually maintained player-facing claim. It states fully affected, partially affected with disclosed reason, or invalid. The initial design expects no invalid pairing; any exception requires a gameplay-spec update.

The simulation retains at least two seconds of player position/facing history at tick resolution for Ghostline and path-based effects. History is a fixed-capacity ring and freezes with simulation time.

## Effect recursion and caps

- Secondary effects state whether they may trigger kill effects, hit effects, or further secondary effects.
- Fission explosions may recurse only through their explicit generation counter and decay; they never affect rocks or the player.
- Mine children, MIRV children, mortar seeded blasts, carried wakes, and impact-transfer collisions are nonrecursive unless explicitly stated.
- Target and actor caps are authoritative content values. When full, behavior uses its specified reject, replace-oldest, queue, or consume rule.
- A cap applies to simulation actors even if presentation has degraded their visuals.

## Projectile interception

Interceptable enemy projectiles declare that flag at creation. Deflection Ring queries swept contact against cutter paths, consumes the projectile once, emits its configured shards, and starts the per-cutter cooldown. Boss landing zones, contact, beams, and non-projectile hazards are not interceptable.

## Presentation contract

For each attack, presentation receives enough state to render telegraphs and effect geometry without recomputing mechanics:

- origin, facing, target/impact, start tick, impact tick, expiry tick;
- authoritative width/radius/range and hit timing;
- branch/relic visual variant IDs;
- actor transforms for long-lived objects;
- hit, control, intercept, death, and expiration events; and
- priority/importance for visual and audio budgets.

Damage geometry must remain visible at reduced quality. Cosmetic trails and particles may be shortened or pooled; the primary telegraph, boundary, projectile core, and impact remain.

## Performance and capacity

Provisional authoritative hard capacities for stress validation are intentionally above current gameplay maxima:

- 2,048 player weapon projectiles/actors combined;
- 512 persistent damage zones/trail segments after behavior-specific compaction;
- 2,048 enemy projectiles;
- 256 pending delayed attacks;
- 128 deployable/autonomous actors; and
- 8,192 damage candidates in one tick before deterministic chunked processing.

These are safety ceilings, not design allowances. Profiling and legal maximum-output analysis must tighten or expand them before content complete. Reaching 80% emits a diagnostic warning; reaching a hard cap in a legal build fails the stress gate.

### Enemy projectile ceiling

The enemy projectile ceiling is the expansion this section authorizes: the legal maximum-output analysis puts a legal peak above the earlier 512, so 512 was the wrong ceiling rather than the build being illegal. It is 2,048 and remains provisional under the same warning and stress-gate rules.

Only two identities create enemy projectiles, and both count against this one ceiling: Needler, the sole ordinary projectile specialist, and `BOSS-03` Prism Crown.

- Needler fires one non-homing needle per 4.5-second active cadence at 2.25M/s, slower than the 3.0M/s unmodified mech, with a lifetime carrying it slightly beyond one screen width. One screen width is approximately 42.7M at 16:9 against the fixed 24M vertical camera, so flight time is approximately 19 seconds, not the one or two seconds a projectile normally implies. That derivation is why 512 was set too low.
- Maximum legal Needler population is 180, at minute 32:30: a 30% composition share of that row's 350 authored minimum, plus 30 scheduled-event overflow and 45 beacon-tagged responders drawn at the same share. Minute 21's larger 45% share is not the worst row, because its authored minimum is 150 and it is a boss-arrival minute with no formation.
- Eidolon Coral resonance advances cadence at 1.20×, shortening the fire period to as little as 3.75 seconds. The field is a local non-overlapping circle around an unopened geode, so applying the multiplier to the whole population is an upper bound rather than a reachable state.
- Prism Crown fires twelve projectiles every 7 seconds at the same 2.25M/s and the same range, and lives from 21:00 until killed, so it can be alive at minute 32. It adds 28 to 48 projectiles.
- Legal worst case is therefore approximately 1,010 simultaneous enemy projectiles, and approximately 673 on the most conservative reading of the same numbers. Both exceed 512, and the old 80% warning line of 410 was already breached by baseline population plus a single beacon response at any late Needler minute.

Neither elites nor player builds change the arithmetic. Needler is excluded from elite selection by content validation, and no elite modifier touches attack cadence or projectile count. No player build raises enemy fire rate: Claim-Jumper Core accelerates enemy movement and explicitly does not accelerate attack cadence, so Coral resonance is the only enemy-cadence multiplier in the game.

2,048 rather than 1,024: 1,024 would place a legal peak at approximately 99% of cap, spamming the 80% warning and effectively failing the stress gate. 2,048 places the legal worst case at approximately 49%, keeps the warning line at 1,638, matches the power-of-two vocabulary already used above, and equals the accepted player projectile ceiling, so no new budget shape is introduced. Enemy projectile state is small, so the added headroom is cheap.

2,048 is justified against the *replenishment*-bounded worst case only. Composition shares govern replenishment, not the alive mix, and nothing currently bounds the alive per-identity share; under extreme churn the alive Needler share can exceed its authored composition share and the count can exceed 2,048. That question is unresolved and recorded as [TOQ-003](./open-questions.md#toq-003--what-bounds-the-alive-per-identity-enemy-share). Until it is answered no finite enemy projectile ceiling is provably safe, and this ceiling must not be treated as one.

At Steam Deck peak, combat scheduling, weapon actor simulation, target queries, hit generation, damage, control, and death processing together target at most 2.5 ms CPU at 95th percentile with zero steady-state managed allocation.

## Verification

- Every base weapon, stat rank, and branch has arithmetic fixtures matching the authoritative catalog.
- Every weapon runs WB-01 through WB-06 headlessly with standard metrics.
- Golden geometry fixtures cover targeting ties, pierce, chain no-repeat, per-target cadence, delayed impacts, path loops, actor replacement, and rock fallback.
- A modifier matrix covers each weapon with all three branches, ten relics, relevant utilities, all mech traits, and fresh/max PowerUps; pairwise generation may replace exhaustive rank combinations.
- Damage fixtures cover contact grace, simultaneous hits, Armor minimum, shield/revival order, resonance creation-time rules, overkill, and recursive exclusions.
- Stress fixtures construct maximum legal rates, areas, durations, capacities, and relic combinations and assert no authoritative loss or unbounded allocation.
- Presentation correspondence captures debug geometry over representative effects at 1920×1080 and 1280×800.

## Related documents

- [Simulation Core](./20-simulation-core.md)
- [World Geometry, Navigation, and Spatial Queries](./21-world-geometry-navigation-and-spatial-queries.md)
- [Encounter Director and Enemy Runtime](./23-encounter-director-and-enemy-runtime.md)
- [Content Data and Validation](./40-content-data-and-validation.md)
- [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md)
- [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md)
