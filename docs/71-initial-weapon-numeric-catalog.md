---
doc_id: GDD-WEAPON-NUMERIC-CATALOG
title: Initial Weapon Numeric Catalog
status: active
authoritative: true
---

# Initial Weapon Numeric Catalog

Status: **authoritative first playable baseline**. These values are intended to make every weapon implementable and testable, not to claim final balance before playtesting.

This document assigns the first complete numerical catalog to the weapon concepts in [Weapon Catalog and Resource Graph](66-weapon-catalog-and-resource-graph.md). The balancing method, terminology, and benchmark scenes come from the [Combat and Economy Balance Framework](70-combat-and-economy-balance-framework.md).

Machine-readable mirrors:

- [Base weapon data](data/weapon-base-balance.csv)
- [Branch data](data/weapon-branch-balance.csv)

If a CSV and this document disagree, this document is authoritative.

## Measurement Conventions

- All time values use active simulation seconds. Fabrication pauses do not advance them.
- `M` means one unmodified mech collision diameter. It is a relative spatial unit so the catalog remains useful before final world-space scale is chosen.
- A `base-travel second` is the distance an unmodified mech covers in one second at full speed.
- `Damage` is damage before enemy mitigation, global modifiers, critical effects, or branch multipliers.
- An attack-rate stat is recorded as activations per second even if the UI presents the reciprocal cooldown.
- Continuous effects resolve mechanically at 10 ticks per second unless a weapon says otherwise. Tick rate is not itself an upgradeable stat and should not change total damage.
- Area attacks may damage every valid target in their area unless an explicit target or overlap cap says otherwise.
- A target may be damaged once per projectile, pulse, mine, or other discrete attack object unless a weapon defines a repeat interval.
- Branch multipliers apply after the weapon's base value plus ore-stat ranks. Global mech, utility, relic, and PowerUp modifiers then follow the modifier order in the balance framework.
- All base estimates assume rank zero, no branch, no utility, no relic, no PowerUps, and a target with zero mitigation.

## Catalog-Level Targets

| Measure | Catalog result | Intended use |
|---|---:|---|
| Mean ideal sustained single-target DPS | 31.7 | Close to the approximately 32-DPS catalog anchor |
| Lowest ideal sustained single-target DPS | 18.0, Reactor Pulse | Justified by exceptional unconditional area coverage |
| Highest ideal sustained single-target DPS | 45.0, Scatter Array | Justified by short range and the need to land all pellets |
| Ordinary rank size | Usually +8–10% of the named rank-zero stat | Makes early ranks legible without letting linear ranks outrun nonlinear prices |
| Branch target | Usually +35–70% in its favorable scene | Conversion branches may trade raw damage for a much larger role or geometry change |

The wide single-target range is intentional. A weapon's balance includes delivery, area, control, setup, and positional burden; equal dummy DPS would make the safest or broadest weapons categorically superior.

## Base Weapon Summary

`Burst 10` assumes the weapon begins ready to attack. `Sustained 30` is the steady ideal against one durable target. `Favorable horde DPS` is an analytic scene estimate, not a hard cap; it assumes a plausible number of enemies are simultaneously available to the weapon.

| ID | Weapon | Rank-zero damage model | Burst 10 | Sustained 30 | Favorable horde DPS | Primary limitation |
|---|---|---|---:|---:|---:|---|
| W-AB | Rail Lance | 96 every 3.0 s | 38.4 | 32.0 | 128 | Facing line and four-target pierce cap |
| W-AC | Cluster Mortar | 128 every 4.0 s | 38.4 | 32.0 | 160 | Delayed impact at a committed ground marker |
| W-AD | Gravity Projector | 36/s for 1.5 s every 2.5 s | 21.6 | 21.6 | 173 | Delayed field placement and partial uptime |
| W-AE | Attack Drones | 3 × 8 every 0.75 s | 32.0 | 32.0 | 32 | Total output is split rather than multiplied by crowds |
| W-AF | Tracking Laser | 18/s, focusing to 36/s | 31.5 | 36.0 | 36 | Requires uninterrupted focus on one target |
| W-BC | Pulse Repeater | 12 every 0.375 s | 32.4 | 32.0 | 32 | One automatically selected target per projectile |
| W-BD | Mine Layer | 27 per mine, at most one mine per base-travel second | 24.3 | 27.0 | 135 | Requires movement, arming, and enemies crossing the trail |
| W-BE | Sentry Pod | Up to 3 × 8 every 0.714 s | 15.7 | 33.6 | 33.6 | Six-second deployment ramp and stationary coverage |
| W-BF | Orbital Cutters | 4 × 8 per 1.0 s contact cycle | 32.0 | 32.0 | 128 | Requires close orbit contact |
| W-CD | Arc Emitter | 16 every 0.5 s, up to five targets | 32.0 | 32.0 | 160 | Needs chainable targets within link range |
| W-CE | Reactor Pulse | 27 every 1.5 s | 18.9 | 18.0 | 180 | Low boss output in exchange for unconditional radial coverage |
| W-CF | Wake Projector | 18/s per segment, two-segment target overlap cap | 34.2 | 36.0 | 144 | Requires movement and useful trail geometry |
| W-DE | Scatter Array | 5 × 9 every 1.0 s | 45.0 | 45.0 | 45 | All pellets land only at close range or on large targets |
| W-DF | Ram Field | 16 per target every 0.5 s | 32.0 | 32.0 | 128 | Requires movement directly through danger |
| W-EF | Missile Rack | 4 × 26 every 3.0 s | 41.6 | 34.7 | 69 | Slow missiles can waste travel on dead or obscured targets |

The favorable-horde column uses approximately four simultaneous victims for line/contact weapons, five for the mortar, eight for Gravity Projector, ten for Reactor Pulse, and two victims per missile blast. It exists to catch order-of-magnitude errors; benchmark-scene captures supersede it once a playable build exists.

## Rank-Zero Values and Ore-Stat Increments

Each `+` value is the exact additive change bought by one rank of that stat. All three tracks remain uncapped and use the shared per-weapon purchase-depth price curve.

| ID | Stat 1 | Rank zero / increment | Stat 2 | Rank zero / increment | Stat 3 | Rank zero / increment | Fixed properties |
|---|---|---:|---|---:|---|---:|---|
| W-AB | Damage | 96 / +9.6 | Beam width | 0.60M / +0.06M | Range | 12M / +1.2M | 3.0 s cadence, 30M/s projectile, pierces four targets |
| W-AC | Damage | 128 / +12.8 | Blast radius | 2.40M / +0.20M | Attack rate | 0.250/s / +0.025/s | 12M targeting range, 1.2 s impact delay |
| W-AD | Field damage | 36/s / +3.6/s | Field radius | 2.50M / +0.20M | Field duration | 1.50 s / +0.15 s | 2.5 s deployment cadence, 10M targeting range, 2M/s inward pull |
| W-AE | Shot damage | 8 / +0.8 | Attack rate | 1.333/s / +0.133/s | Operational range | 8M / +0.8M | Three permanent drones, 6M/s repositioning speed |
| W-AF | Base damage rate | 18/s / +1.8/s | Range | 9M / +0.9M | Focus gain | 20 percentage points/s / +2 points/s | Focus multiplier rises linearly from 1× to 2× and resets on target loss |
| W-BC | Damage | 12 / +1.2 | Attack rate | 2.667/s / +0.267/s | Range | 8M / +0.8M | 16M/s projectile, 0.25M hit push |
| W-BD | Damage | 27 / +2.7 | Blast radius | 1.80M / +0.15M | Parent capacity | 6 / +1 | Places one mine per base-travel second, 0.5 s arm, 18 s life |
| W-BE | Shot damage | 8 / +0.8 | Attack rate | 1.400/s / +0.140/s | Range | 7M / +0.7M | One pod every 6 s, 24 s life, maximum three active; oldest replaced at cap |
| W-BF | Contact damage | 8 / +0.8 | Cutter radius | 0.40M / +0.04M | Orbit speed | 0.500 rev/s / +0.050 rev/s | Four cutters, 2.2M orbit radius, 1.0 s per-cutter/per-enemy repeat interval |
| W-CD | Arc damage | 16 / +1.6 | Attack rate | 2.000/s / +0.200/s | Chain range | 3M / +0.3M | 8M initial acquisition range, maximum five total targets |
| W-CE | Pulse damage | 27 / +2.7 | Pulse radius | 3M / +0.25M | Pulse rate | 0.667/s / +0.067/s | Damages every valid target in radius |
| W-CF | Segment damage | 18/s / +1.8/s | Wake width | 1.20M / +0.10M | Segment duration | 5.0 s / +0.5 s | Segment every 0.25 base-travel seconds, two-segment overlap cap per target |
| W-DE | Pellet damage | 9 / +0.9 | Attack rate | 1.000/s / +0.100/s | Range | 5M / +0.5M | Five pellets, 60° cone, 18M/s projectile |
| W-DF | Contact damage | 16 / +1.6 | Ram width | 1.50M / +0.12M | Knockback | 0.60M / +0.06M | 1.2M forward reach, 0.5 s per-target repeat interval |
| W-EF | Missile damage | 26 / +2.6 | Blast radius | 1M / +0.1M | Salvo rate | 0.333/s / +0.033/s | Four missiles per salvo, 12M acquire range, 8M/s speed, 5 s life, 180°/s turn rate |

## Shared Edge Rules

- When additive ranks produce a fractional capacity, target count, projectile count, or other discrete quantity, only the explicitly discrete stats in the table round. Parent mine capacity is the only base stat here that is discrete, and every rank adds exactly one.
- Attack-rate modifiers multiply activations per second, not cooldown duration. A +20% attack-rate result is therefore `base rate × 1.20`.
- Duration extensions create additional damage time at the existing damage rate. They do not squeeze a fixed damage budget over a longer interval.
- A branch-generated child object inherits current Damage and relevant Area values at its stated multiplier. It does not independently repeat the parent branch unless explicitly allowed.
- Branch-created damage counts as that weapon's damage for effects, statistics, and kill attribution.
- Bosses obey ordinary damage rules but use the global control-resistance rules for pull, push, slow, and stagger.
- Exact resistance, slow stacking, hard-control immunity, and boss-ability continuity are defined in the [Player Survivability and Damage Baseline](72-player-survivability-and-damage-baseline.md).

### Global weapon Attack Rate mapping

Kestrel's trait, Cycle Capacitor, PowerUps, and relics that say **weapon Attack Rate** modify the schedules below. A multiplier `R` divides the listed repeat interval by `R` or multiplies activations per second by `R`. It does not accelerate projectile travel, delayed echoes, field ticks, arm times, lifetimes, or movement unless explicitly stated.

| Weapon | Timing affected by global weapon Attack Rate | Explicitly unaffected timing |
|---|---|---|
| Rail Lance | Base firing cadence; Kinetic Capacitor charge accumulation | Projectile speed and Fracture delay |
| Cluster Mortar | Shell launch rate | 1.2 s travel delay, marker duration, and payload-field ticks |
| Gravity Projector | Base field deployment rate; Singularity Forge full-cycle rate | Echo delay, field ticks, and field duration |
| Attack Drones | Drone shot rate; Containment Lattice damage rate | Drone movement and temporary-drone lifetime |
| Tracking Laser | None | Continuous damage tick and focus gain |
| Pulse Repeater | Projectile or trace firing rate | Projectile travel and status durations |
| Mine Layer | None | Distance-based placement, arm time, pursuit speed, and mine life |
| Sentry Pod | Pod or bastion shot rate | Pod deployment cadence, setup hold, and pod life |
| Orbital Cutters | Per-cutter/per-enemy contact-repeat rate; Reaper contact-repeat rate | Physical orbit speed and blade movement |
| Arc Emitter | Direct discharge rate; Ball-Lightning orb launch rate | Orb movement, orb lifetime, and internal twice-per-second emissions |
| Reactor Pulse | Pulse or supernova activation rate | Charge tell and status durations |
| Wake Projector | None | Distance-based placement, damage ticks, and segment life |
| Scatter Array | Volley or cone-wave activation rate | Projectile travel |
| Ram Field | Per-enemy contact-repeat rate | Movement threshold and Siege Anchor setup |
| Missile Rack | Salvo production rate | Missile movement, reserve life, and targeting |

If a later modifier names an individual weapon's upgradeable Attack Rate, Pulse Rate, Salvo Rate, or Orbit Speed rather than global **weapon Attack Rate**, it changes only that named stat.

## W-AB — Rail Lance

Base behavior: fires down the mech's current facing line. The projectile damages the first four enemies it crosses and then ends; terrain does not block it unless a future map feature explicitly says so.

### Unbounded Bore — Amplification — 2 Asterite

- Removes the four-target pierce limit. The lance continues to its current maximum range and may damage every enemy it crosses.
- Damage, width, range, cadence, and projectile speed are otherwise unchanged.
- Expected effect: approximately +35–70% horde throughput in a dense aligned stream and no direct single-target increase.

### Fracture Lance — Functional — 2 Barysteel

- Every enemy struck emits one perpendicular shockwave after 0.10 s.
- Each shockwave deals 45% of current Rail Lance Damage, has the current beam width, and reaches 2.5M to each side of its source.
- The source enemy cannot be hit by its own shockwave. Any other enemy can take at most two shockwave hits from one Rail Lance shot.
- Expected effect: approximately +40–80% favorable-horde damage while preserving the four-target main-beam cap.

### Kinetic Capacitor — Conversion — 2 Cinderglass

- Replaces the regular shot with a charge tied to mech movement.
- The charge takes 9.0 s while stationary. At full unmodified movement speed it builds twice as fast and fires every 4.5 s; intermediate movement speeds interpolate linearly.
- The charged lance deals 175% current Damage, has 200% current width, and has unlimited pierce. It retains current range and 30M/s projectile speed. Even at full movement it fires less often than the 3.0 s base lance.
- The charge is retained while briefly changing direction and is lost only by firing; stopping merely slows the remaining charge.
- This creates a much stronger mobile lane-clearing weapon whose stationary boss DPS is substantially worse.

## W-AC — Cluster Mortar

Base behavior: selects the densest enemy concentration within 12M when ready and locks its current ground position. A warning marker remains at that committed point for the fixed 1.2 s shell delay; the explosion then damages every enemy in its current radius.

### Saturation Cascade — Amplification — 2 Cinderglass

- Each enemy hit by the primary blast seeds a secondary blast 0.40 s later at that enemy's original hit position.
- Each secondary blast deals 40% current Damage and has 60% current blast radius.
- Secondary blasts do not seed more blasts. A target can take no more than two secondary-blast hits from one primary shell.
- Expected effect: approximately +40–100% horde throughput, scaling with how well the primary impact catches a cluster.

### Interdiction Payload — Functional — 2 Asterite

- The primary explosion leaves a 3.0 s hazard field at its impact point.
- The field deals 12% current Damage every 0.5 s, for 72% additional damage to an enemy that remains for the full duration.
- Enemies inside are slowed by 30%; bosses receive the globally resisted form of the slow.
- Repeated fields may overlap and damage independently.

### Danger-Close Protocol — Conversion — 2 Flux Amber

- Mortar targeting is removed. Every shell lands centered on the mech's position sampled at the instant of firing.
- The shell deals 180% current Damage and has 160% current blast radius at the normal attack rate.
- The warning marker remains visible during the 1.2 s delay, allowing the player to drag a pursuing horde into the strike zone.
- The mech is immune to its own mortar. The conversion trades remote safety for an obvious close-range damage and coverage increase.

## W-AD — Gravity Projector

Base behavior: places a field at the current center of the densest enemy concentration within 10M. The field damages and pulls ordinary enemies toward its center for its current duration.

### Echo Well — Amplification — 2 Asterite

- Every field repeats once in the same location 1.25 s after the original ends.
- The echo uses the current field Damage, radius, duration, and pull values at full strength.
- Echoes cannot create more echoes. A new original field may coexist with an older echo.
- Expected effect: up to +100% damage when the player keeps enemies in the location, with materially less benefit against a moving boss.

### Gravity Slingshot — Functional — 2 Driftmetal

- When a field ends, it emits a burst dealing 75% of that field's full-duration damage budget: `current Damage per second × current duration × 0.75`.
- Ordinary survivors are launched 4M directly away from the mech's position sampled when the field ends; bosses receive resisted displacement.
- The burst has the current field radius and can damage each target once.
- Expected full-stay damage is 175% of the base field, with the launch turning grouping into deliberate dispersal.

### Singularity Forge — Conversion — 2 Barysteel

- Replaces the normal cadence with a fixed 7.0 s cycle: a 4.0 s collection field followed by a fired singularity round.
- The collection field uses current radius and pull but deals only 60% current field Damage per second.
- It gains one mass point per 50 points of non-boss enemy Hull drawn into the inner half of the field, rounded up per enemy; elites contribute at least two and bosses contribute four. Mass caps at 12.
- An enemy contributes only once per collection cycle, the first time it enters the inner half; the calculation uses its maximum Hull rather than remaining Hull.
- At the end of collection, a round targets the strongest enemy in 12M and creates a singularity lasting current field duration with 150% current radius.
- The singularity deals `current field Damage × (4 + 0.25 × mass)` per second. At maximum mass this is 7× current field Damage per second.
- If no target exists when collection ends, the round waits up to 2.0 s and then detonates at the collection point.
- This is deliberately infrequent and devastating: even a low-mass shot improves boss output, while a well-fed shot can erase a dense zone.

## W-AE — Attack Drones

Base behavior: three permanent drones stay within operational range, acquire targets independently, and reposition as needed. Several drones may choose the same target.

### Replicator Swarm — Amplification — 2 Eidolon Coral

- A permanent-drone kill creates a temporary exact clone of that drone for 8.0 s.
- Temporary drones inherit current Damage, attack rate, and operational range but cannot create more clones.
- At most six temporary drones may exist. Creating a seventh replaces the temporary drone with the least remaining life.
- Expected effect: no benefit against a lone durable target but up to 3× total squad size while kills arrive quickly.

### Wolfpack Protocol — Functional — 2 Asterite

- All three drones focus the same target, prioritizing the strongest valid enemy and switching only when it dies or leaves operational range.
- Each drone gains +15% damage for every other permanent drone currently locked to that target. With all three locked, each deals +30% damage.
- The bonus does not count temporary drones from any external effect.
- Expected effect: +30% single-target output and much less target waste, paid for with poor crowd distribution.

### Containment Lattice — Conversion — 2 Driftmetal

- The three drones stop firing projectiles and form a rotating triangle around the mech.
- Each of the three links deals damage per second equal to 200% of one drone's current shot DPS: `current shot Damage × current attack rate × 2`.
- An enemy may be damaged by at most two links at once. Link length and formation size scale with current operational range.
- Drones continuously adjust the triangle so one point follows mech facing; the lattice therefore rewards sweeping and enclosing crowds rather than target selection.

## W-AF — Tracking Laser

Base behavior: locks to one target in range and deals continuous damage. Its multiplier rises linearly from 1× to 2× as the focus meter fills, taking 5.0 s at rank zero. Losing the target normally resets focus.

### Coherence Memory — Amplification — 2 Asterite

- Focus no longer resets immediately when the target dies or breaks line of range.
- While no valid target is locked, the focus meter decays by 10 percentage points per second; locking any new target resumes from the remaining value.
- This changes reliability rather than the 2× cap and is expected to add roughly 35–55% realized damage in target-rich movement scenes.

### Target Designator — Functional — 2 Flux Amber

- At 75% focus or higher, the locked target becomes Exposed.
- An Exposed target takes +25% damage from every player weapon, including Tracking Laser, for 4.0 s. The duration refreshes while focus remains at or above the threshold.
- Only one target may be Exposed by this weapon at once; changing targets lets the old exposure expire normally.
- Exposure is a weapon-specific damage-taken multiplier and does not stack with a second copy, which duplicate-weapon rules already forbid.

### Cutting Vector — Conversion — 2 Barysteel

- Removes automatic target lock. The laser becomes an unlimited-pierce beam along mech facing and damages every enemy it crosses at the full current damage rate.
- Focus builds while the player's facing remains within 12° of its previous direction. It decays at twice the current focus-gain rate between 12° and 25° and resets beyond 25°.
- Range remains the upgradeable laser range and beam width is fixed at 0.35M.
- This preserves maximum focused DPS but turns a safe tracker into a manually aimed horde cutter.

## W-BC — Pulse Repeater

Base behavior: selects the nearest enemy within current range and fires a rapid projectile toward that enemy's current position. The projectile continues on its fired trajectory without homing, damages the first enemy it reaches, and then ends. Target selection updates between shots.

### Zero-Lag Emitter — Amplification — 2 Barysteel

- Projectiles become instantaneous hitscan traces with otherwise unchanged range and cadence.
- Each trace deals current Damage.
- The branch removes travel-time misses and travel delay; its realized gain should be largest against fast lateral targets even though ideal stationary-target DPS is unchanged.

### Suppressive Sequencer — Functional — 2 Cinderglass

- The weapon remembers enemies it hit during the previous 1.5 s and prefers any valid enemy not in that memory set.
- Every hit slows its target by 25% for 1.0 s. Bosses receive the globally resisted form.
- If every valid target is already marked, it selects normally rather than withholding fire.
- Damage is unchanged; the branch converts repeated single-target fire into broad crowd suppression.

### Broadside Oscillator — Conversion — 2 Eidolon Coral

- Automatic target selection is removed.
- Every attack event fires two full-damage pulses simultaneously, one directly left and one directly right relative to mech facing.
- Both pulses use current range and may hit separate targets; a sufficiently large target intersecting both traces may take both hits.
- The result has up to 2× raw output but demands continuous facing and lateral positioning.

## W-BD — Mine Layer

Base behavior: while the mech moves, it places a mine after each base-travel-second of distance. Mines arm after 0.5 s, trigger on the first valid enemy entering their radius, and expire after 18 s. Placing above current parent capacity removes the oldest parent mine without detonating it.

### Seed Charges — Amplification — 2 Barysteel

- A parent-mine explosion throws four micro-mines evenly around the blast edge.
- Each micro-mine deals 35% current Damage, has 50% current blast radius, arms in 0.25 s, and expires after 4.0 s.
- Micro-mines do not create more mines. Their separate capacity is four times current parent capacity; the oldest expires when exceeded.
- A fully used cluster adds 140% parent damage, but the delayed spread makes that ceiling situational.

### Selective Detonators — Functional — 2 Driftmetal

- A mine waits until at least three ordinary-equivalent enemies are in radius, or until an elite or boss enters.
- Its damage multiplier is `1 + 0.15 × (ordinary-equivalent count − 1)`, capped at 2×. A boss counts as three and an elite as four for this calculation.
- An expiring mine detonates with a minimum 1.30× multiplier if any enemy is in radius; otherwise it disappears harmlessly.
- This improves payload efficiency but can leave holes against scattered weak enemies.

### Hunter Mines — Conversion — 2 Flux Amber

- Armed mines acquire the nearest enemy within 6M and pursue at 3M/s for up to 6.0 s.
- They deal 150% current Damage with 85% current blast radius.
- A hunter may retarget once if its target dies. It detonates on contact, at pursuit timeout if a target is inside its radius, or at normal lifetime expiry.
- The branch turns a route-defense tool into slow autonomous ordnance with better single-target reliability but less crowd coverage.

## W-BE — Sentry Pod

Base behavior: deploys its first temporary pod immediately when acquired and another at the mech's position every 6.0 s, including when no enemy is present. Pods last 24 s, fire independently, and cannot be damaged. Deploying while three are active replaces the oldest pod, so a continuously owned weapon maintains three after its 12-second ramp.

### Battery Overclock — Amplification — 2 Eidolon Coral

- Every active pod beyond the first gives all active pods +25% attack rate, capped at +50% with three pods.
- The bonus updates immediately as pods deploy or expire.
- At full setup this is a direct +50% damage-throughput increase without changing each pod's Damage value.

### Guardian Firmware — Functional — 2 Barysteel

- Pods prioritize enemies within 4M of the mech, then fall back to normal nearest-target behavior.
- Every hit pushes an ordinary enemy 0.8M away from the mech and staggers it for 0.25 s. Bosses and elites use global control resistance.
- Damage and attack rate are unchanged; the branch converts distributed fire into a mobile defensive screen.

### Forward Bastion — Conversion — 2 Asterite

- Ordinary timed pod deployment is removed. Holding within 0.25M of one position for 1.5 s erects one persistent bastion there.
- The bastion has 300% current shot Damage, 200% current attack rate, and 125% current range.
- It packs instantly if the mech moves more than 6M from it, after which another 1.5 s hold is required to redeploy. It remains indestructible.
- One active bastion has 2× the total steady DPS of three ordinary pods, but only while the player repeatedly anchors near it.

## W-BF — Orbital Cutters

Base behavior: four cutters orbit the mech at a fixed 2.2M radius. Each cutter may damage a given enemy once per second while their collision shapes overlap.

### Kinetic Flywheel — Amplification — 2 Flux Amber

- Every valid cutter hit grants one Flywheel stack, giving +4% cutter Damage and +4% orbit speed per stack.
- Stacks cap at 10, for +40% Damage and +40% orbit speed.
- After 2.0 s without any cutter hit, one stack decays every 0.5 s until another valid hit lands.
- The branch rewards remaining close to a crowd without requiring the mech to remain still.

### Deflection Ring — Functional — 2 Barysteel

- Cutters destroy ordinary interceptable enemy projectiles on contact.
- Each interception emits six outward shards. A shard deals 50% current cutter Damage, travels 4M, and ends on its first enemy.
- One enemy can take at most two shards from a single interception burst. Each cutter has a 0.5 s interception cooldown.
- Undeflectable boss attacks remain visually distinct. The branch converts projectile pressure into retaliatory offense rather than simply deleting every threat.

### Tethered Reaper — Conversion — 2 Eidolon Coral

- The four cutters combine into one blade with 200% current cutter radius. Its contact Damage is `200% + up to 200%` of current Damage, scaling linearly with blade world speed from stationary to one base mech full-speed and capped at 400%.
- The blade follows the mech's recent path with a 0.5 s delay at the current orbit distance instead of orbiting.
- It may damage a given enemy every 0.5 s, giving the same single-target ceiling as four ordinary cutters at zero blade speed and a 2× ceiling at full bonus.
- The player must drag the delayed blade through enemies; abrupt turns and reversals become the core aiming method.

## W-CD — Arc Emitter

Base behavior: strikes the nearest target within 8M and chains to up to four additional enemies. A chain never hits the same enemy twice and each jump must be within current chain range.

### Total Conduction — Amplification — 2 Cinderglass

- Removes the five-target cap. The arc continues jumping until no new valid enemy is within current chain range.
- Damage does not diminish across jumps and no enemy may be hit twice by one activation.
- Expected effect: no direct boss increase and approximately +40–150% horde throughput when chain density supports more than five victims.

### Disruption Current — Functional — 2 Driftmetal

- The first target is staggered for 0.25 s and each successive chain step adds 0.05 s, capped at 0.60 s.
- Elites and bosses receive globally resisted stagger duration and immunity-window behavior.
- Damage and target cap are unchanged. The later, harder-to-reach targets receive the strongest disruption.

### Ball-Lightning Projector — Conversion — 2 Barysteel

- Replaces direct arcs with a slow orb launched along persistent mech facing. Rank-zero orb launch rate is 0.4/s, exactly one fifth of the base Arc Emitter attack rate; attack-rate ranks and global modifiers scale it normally.
- An orb moves at 3M/s for 4.0 s. Twice per second it emits current-Damage arcs to as many as three distinct enemies within current chain range.
- The same enemy may be struck once per emission, including bosses. Orbs pass through enemies and terrain that does not explicitly block player projectiles.
- A single orb can deal eight hits to one durable target over its life, yielding a rank-zero ideal of 51.2 single-target DPS at continuous launch cadence, while moving targets or poor placement lower the result.

## W-CE — Reactor Pulse

Base behavior: emits a radial pulse centered on the mech and damages every valid enemy in current radius.

### Critical-Mass Cycle — Amplification — 2 Eidolon Coral

- Every enemy hit by a pulse gives one Critical Mass charge for the next pulse, capped at 20 charges.
- Each charge gives the next pulse +2% Damage and +1% radius, for maximum bonuses of +40% Damage and +20% radius.
- Charges are consumed by the next pulse and replaced by the count that pulse hits, allowing sustained crowd contact to maintain the bonus.
- A boss alone supplies one charge; the large benefit requires horde pressure around it.

### Kinetic Vent — Functional — 2 Cinderglass

- Every pulse pushes ordinary enemies 1.0M radially outward and slows them by 25% for 1.0 s.
- Bosses and elites receive globally resisted displacement and slow.
- Damage, radius, and pulse rate are unchanged. The weapon becomes a repeating breathing-space tool for mining holds.

### Supernova Cycle — Conversion — 2 Flux Amber

- Pulse rate is multiplied by 0.25; rank zero therefore changes from one pulse every 1.5 s to one every 6.0 s.
- Each supernova deals 500% current Damage and has 250% current pulse radius.
- A visible and audible charge builds during the final 2.0 s, but movement remains unrestricted.
- Rank-zero steady single-target DPS rises from 18 to 22.5 while each event becomes a map-clearing spike rather than constant protection.

## W-CF — Wake Projector

Base behavior: movement leaves damaging trail segments. An enemy may take damage from at most two segments simultaneously, preventing stationary turns from creating unbounded overlap.

### Runaway Wake — Amplification — 2 Cinderglass

- After 2.0 s of continuous movement, the wake gains +10% Damage and +5% width for each additional second of movement.
- Bonuses cap at +50% Damage and +25% width after 7.0 total seconds of continuous movement. Each newly laid segment snapshots the current bonuses until that segment expires.
- Dropping below 20% of base movement speed for 1.0 s clears the bonus; turning does not.
- Segment duration is unchanged, so the branch rewards sustained traversal without encouraging a stationary hold.

### Carrier Ignition — Functional — 2 Flux Amber

- An enemy damaged by the mech's wake leaves its own trail for 3.0 s as it moves.
- Enemy trails deal 50% current segment Damage per second, have 50% current wake width, and cannot ignite more carriers.
- One carrier trail per enemy may exist at once, and the ordinary two-segment overlap cap includes both mech and carrier segments.
- The branch turns enemies into moving area denial and is strongest when fast aliens cross through slower crowds.

### Circuit Closure — Conversion — 2 Driftmetal

- When the current wake intersects a segment from the same continuous path and encloses at least 4M², the loop closes and all enclosed enemies take an eruption hit equal to six seconds of current segment Damage.
- A loop may claim at most 40M² of interior area; a larger loop still erupts but only enemies in the 40M² nearest the closure point are hit.
- The segments forming the loop are consumed. The same continuous path cannot close another loop for 1.0 s.
- This turns the wake from passive pursuit damage into deliberate lasso-like route drawing.

## W-DE — Scatter Array

Base behavior: fires five pellets across a 60° facing cone. Each pellet ends on its first target, so all five hit one enemy only at close range or when the target is large enough to cover the spread.

### Saturation Choke — Amplification — 2 Driftmetal

- Replaces the five pellets with one continuous-looking cone wave per normal attack activation.
- Every enemy in the current 60° cone and range is hit once for 400% current pellet Damage.
- Against a target that would take all five base pellets, rank-zero shot damage changes from 45 to 36; against two or more enemies the branch is an immediate throughput increase.
- The wave is instantaneous and does not pierce terrain that explicitly blocks player attacks.

### Concussive Fan — Functional — 2 Eidolon Coral

- Pellets retain normal damage and add 1.2M knockback plus 0.35 s stagger.
- Multiple pellets from the same attack combine damage normally but apply control only once to a target.
- Bosses and elites receive globally resisted control.
- The branch makes close-range volleys create obvious space instead of merely killing faster.

### Focal Array — Conversion — 2 Cinderglass

- All five pellets spread and then curve inward to converge on a focal point centered on persistent mech facing at current maximum range.
- Each pellet deals 140% current Damage. A target at the focal point can take all five, for 63 rank-zero damage per attack.
- Targets much closer than the focal point are likely to receive only part of the volley; the weapon does not rotate the focal point toward an enemy independently of mech facing.
- This converts a close crowd fan into a precise medium-range execution weapon.

## W-DF — Ram Field

Base behavior: while movement speed is at least 20% of base speed, projects a short damaging field in front of the mech. An enemy overlapping it may be hit every 0.5 s and is pushed along the mech's facing direction. The field is inactive below that threshold.

### Momentum Cascade — Amplification — 2 Driftmetal

- Each full second above 80% of base movement speed gives one Momentum stack. A valid impact also grants one stack, but no more than one impact-granted stack per second.
- Each stack gives +10% Damage and +5% ram width. Six stacks cap the bonuses at +60% Damage and +30% width.
- Falling below 20% base speed for 1.0 s clears all stacks; collisions and turns do not.
- The branch makes long committed charges visibly stronger without requiring a straight line.

### Impact Transfer — Functional — 2 Flux Amber

- An ordinary enemy struck by the ram becomes a projectile for 1.0 s or until it has traveled 5M.
- It deals 75% of the triggering ram hit to each of up to three new enemies it collides with and transfers current knockback.
- The launched enemy and each collision target can participate only once per original ram hit, preventing recursive collision chains.
- Bosses cannot be launched but can be damaged by launched enemies.

### Siege Anchor — Conversion — 2 Asterite

- Moving less than 0.25M for 1.25 s replaces the forward ram with a circular barrier at 2.2M radius around the mech.
- Crossing the barrier deals 150% current Damage, applies current knockback radially, and may affect a given enemy every 0.5 s.
- Current ram width becomes barrier thickness. Moving 0.5M from the anchor point collapses it and another 1.25 s hold is required.
- The branch converts an aggressive collision weapon into a high-output mining perimeter with a strict anchoring requirement.

## W-EF — Missile Rack

Base behavior: each salvo launches four homing missiles, dividing them across valid targets when possible. A missile can retarget if its target dies and explodes on contact or at the end of its five-second life.

### MIRV Saturation — Amplification — 2 Flux Amber

- Each missile splits once into three micro-missiles at half of its remaining travel distance or 1.0 s before predicted impact, whichever comes first.
- Each micro-missile deals 45% current Damage and has 50% current blast radius. They select distinct nearby targets before assigning extras, so all three may hit one durable target only when no alternatives are available.
- Micro-missiles inherit current speed and turn rate and cannot split again.
- Total direct payload per original missile is 135% current Damage, with more target distribution and less area per impact.

### Guardian Reserve — Functional — 2 Eidolon Coral

- Missiles enter an orbiting reserve around the mech instead of immediately choosing distant targets, up to eight stored missiles.
- A reserve missile dives at the nearest enemy entering 6M, dealing 125% current Damage. If the reserve is full, newly produced missiles launch normally at any target in acquisition range.
- Stored missiles persist until fired and retarget normally. They do not block enemy projectiles.
- The branch banks damage during quiet traversal and releases it automatically when danger reaches the player.

### Spiral Barrage — Conversion — 2 Driftmetal

- Automatic homing is removed. Every salvo launches its four missiles in evenly spaced radial directions from the mech.
- Each missile deals 125% current Damage and has 125% current blast radius.
- The four-way launch offset advances 45° clockwise with each salvo. Missiles also curve clockwise at 45° per second, retain current speed and lifetime, and may damage every enemy in their explosions.
- The conversion produces predictable sweeping geometry and more raw area output, but no guarantee that any missile follows a priority target.

## Branch Numeric Summary

| Class | Typical raw effect in favorable scene | Main balancing burden |
|---|---:|---|
| Amplification | +35–70%; some horde-only branches exceed this in unusually dense formations | Usually preserves targeting and core handling |
| Functional | Roughly +25–75% effective value through control, reliability, exposure, or delayed damage | Value varies sharply by enemy and map scene |
| Conversion | May range from a small dummy-DPS loss to more than 2× favorable output | Requires materially different movement, facing, range, or setup |

Several branches intentionally exceed a simple 70% raw ceiling when the required geometry is fragile: Echo Well requires enemies to remain in place, Forward Bastion requires anchoring, Broadside Oscillator needs both lateral traces to connect, and Tethered Reaper must be steered by path history. These are benchmark priorities, not automatic nerf candidates.

## Legal Reference Build and Boss Feasibility

The first numerical pass uses one deliberately ordinary no-relic progression to verify that boss Hull is compatible with legal weapon access. It is not intended as the best build.

Reference conditions:

- Mech: Kestrel, whose signature Pulse Repeater receives the locked +15% weapon attack-rate trait.
- Resource profile: Asterite, Barysteel, Cinderglass, and Eidolon Coral. It satisfies the signature guarantee and makes six weapons available.
- Chosen weapons: Pulse Repeater, Rail Lance, Sentry Pod, and Attack Drones.
- Utilities by minute 14: Harmonic Amplifier and Cycle Capacitor, at their installed +8% values. Both reach Rank 2 (+16%) by minute 21 and Rank 3 (+20%) by minute 28.
- Third utility: Resource Radar. Its 300-Ore purchase is included at minute 28 but contributes no combat output.
- Branches: Battery Overclock on Sentry Pod and Wolfpack Protocol on Attack Drones. Zero-Lag Emitter is included by minute 21 for reliability and contributes no ideal stationary-target DPS multiplier.
- No relic, no permanent PowerUps, no boss-drop combat pickup, and no favorable claim about rare random support.
- `Realized DPS` applies explicit delivery allowances for facing, deployment uptime, target switches, and boss movement. It is lower than analytic dummy DPS.

| Boss time | Weapons and ranks available | Ore spent on weapon ranks | Utility/radar Ore | Estimated realized boss DPS | Hull supported at target TTK |
|---:|---|---:|---:|---:|---:|
| 7:00 | Pulse depth 4; Rail depth 2 | 240 | 0 | 81 | 3,645–6,075 |
| 14:00 | Pulse, Rail, and Sentry each depth 4; Battery Overclock | 600 | 0 | 164 | 9,840–14,760 |
| 21:00 | Four weapons each depth 6; both combat utilities Rank 2; Battery, Wolfpack, Zero-Lag | 2,240 | 300 | 328 | 24,600–34,440 |
| 28:00 | Four weapons each depth 7; both combat utilities Rank 3; same branches | 3,360 | 900 | 391 | 35,190–46,920 |

The utility/radar column uses cumulative Ore: 150 per combat utility to reach Rank 2, 300 per combat utility to reach Rank 3, and 300 for Resource Radar. Weapon depth uses the shared nonlinear price curve.

The reference estimate is reproducible from these allocations and delivery allowances:

| Time | Pulse ranks `Damage / Rate / Range` | Rail ranks `Damage / Width / Range` | Sentry ranks `Damage / Rate / Range` | Drone ranks `Damage / Rate / Range` | Realized-output factors after arithmetic DPS |
|---:|---:|---:|---:|---:|---|
| 7:00 | `2 / 2 / 0` | `2 / 0 / 0` | — | — | Pulse 90%; Rail 75% |
| 14:00 | `2 / 2 / 0` | `2 / 1 / 1` | `2 / 2 / 0` | — | Pulse 88%; Rail 80%; Sentry 72% |
| 21:00 | `3 / 3 / 0` | `3 / 1 / 2` | `3 / 3 / 0` | `3 / 3 / 0` | Pulse 95%; Rail 80%; Sentry 80%; Drones 90% |
| 28:00 | `4 / 3 / 0` | `4 / 1 / 2` | `4 / 3 / 0` | `4 / 3 / 0` | Pulse 95%; Rail 80%; Sentry 85%; Drones 95% |

For this calculation, add ore ranks to the weapon value first; multiply by Harmonic Calibrator's current damage value; multiply activation rate by Kestrel plus Cycle Capacitor's additive combined attack-rate value; apply Battery Overclock or Wolfpack Protocol; then apply the listed realized-output factor. Zero-Lag changes Pulse's factor rather than ideal DPS. Area, width, and range ranks do not receive fictitious boss-DPS credit.

The prior boss Hull sequence of 6,000 / 24,000 / 75,000 / 220,000 cannot be supported by this legal progression without making ordinary weapons wildly overpowered against the rest of the game. The initial playable boss Hull sequence is therefore:

| Boss | Arrival | Initial Hull | Target defeat time | Required realized build DPS |
|---|---:|---:|---:|---:|
| Riftjaw | 7:00 | 6,000 | 45–75 s | 80–133 |
| Brood Titan | 14:00 | 14,000 | 60–90 s | 156–233 |
| Prism Crown | 21:00 | 30,000 | 75–105 s | 286–400 |
| Skybreaker Apex | 28:00 | 45,000 | 90–120 s | 375–500 |

This change reduces late boss health, not their spectacle, control pressure, adds, attack damage, or resource explosion. Those other levers can preserve escalation without invalidating the weapon economy.

## First Playtest Capture Requirements

For every weapon, capture at least:

1. rank-zero base behavior in all six benchmark scenes;
2. depth 3, depth 6, and depth 9 with ranks distributed as a player would plausibly choose;
3. each branch at depth 6;
4. one intentionally favorable and one unfavorable geometry for every conversion;
5. boss DPS at the weapon's expected arrival boss and at the final boss;
6. horde kills per second, overkill fraction, missed or wasted attacks, and player hit rate during a geode hold.

Review a weapon when any of these occur:

- it misses its archetype single-target band by more than 20% in neutral conditions;
- one branch dominates both of its siblings in every benchmark scene;
- a conversion's new play pattern is not visible within five seconds of use;
- a safety/control weapon also matches the best focused weapon's boss damage without a meaningful burden;
- a branch relies on uncapped child-object multiplication or creates illegible screen coverage;
- the reference build misses a boss target by more than 20% after player execution is accounted for.

## Related Decisions

- [DEC-025: Use uncapped linear stat ranks with nonlinear prices](decisions/DEC-025-uncapped-linear-stat-ranks.md)
- [DEC-027: Make major weapon branches mutually exclusive](decisions/DEC-027-mutually-exclusive-weapon-branches.md)
- [DEC-043: Assign the fifteen base weapons to the resource graph](decisions/DEC-043-fifteen-weapon-graph-assignment.md)
- [DEC-085: Use a triangular shared-depth price curve](decisions/DEC-085-use-triangular-shared-depth-prices.md)
- [DEC-124: Adopt a multi-metric weapon balance framework](decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125: Adopt the initial numerical weapon catalog and feasible boss Hull](decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-126: Adopt the initial player survivability baseline](decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
