---
doc_id: GDD-WEAPON-CATALOG
title: Weapon Catalog and Resource Graph
status: active
authoritative: true
---

# Weapon Catalog and Resource Graph

## Purpose

This document defines the 15 normal base weapons, their placement on the six-resource pair graph, their three major branch paths, and their relationships to playable mechs. Every weapon's high-level automatic behavior, three-stat bundle, branch set, funding orientation, and off-color conversion are accepted. The [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md) supplies the authoritative first-playable values and weapon-specific edge rules using the [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md). Final presentation names and audiovisual production remain later work.

## Accepted catalog structure

- Stable graph codes `A`, `B`, `C`, `D`, `E`, and `F` correspond to Asterite, Barysteel, Cinderglass, Driftmetal, Eidolon Coral, and Flux Amber.
- Codes remain authoring shorthand and preserve weapon IDs. Player-facing interfaces use the accepted material names and redundant icons, silhouettes, motion, color, and audio rather than bare codes or color alone.
- Each unordered pair corresponds to exactly one normal base weapon, producing 15 weapons. "Unordered" here is a claim about the recipe; for how it relates to the order of the authored pair field, see [Content Data and Validation](./technical/40-content-data-and-validation.md#weapons).
- Every weapon has one major branch funded by each recipe resource and one funded by a fixed third resource distinct from both recipe resources.
- The three branches are mutually exclusive during a run.
- Every weapon's three branches follow the same transformation gradient: one amplification branch that is “samey but bigger and better,” one functional variant that is “a bit different in function,” and one playstyle conversion that is “much different in play style.”
- The transformation categories describe behavioral distance from the base weapon rather than power tiers.
- One recipe color funds amplification, the other funds the functional variant, and the fixed off-color resource always funds the playstyle conversion.
- Native amplification-versus-functional assignments are selected and locked through global catalog balancing rather than separate per-weapon approval.
- All 15 weapons occupy the same intended base power tier and must be useful before ore or branch investment.
- Each weapon in the initial catalog exposes exactly three common-ore stat tracks. Three remains the default maximum for later weapons unless an explicit decision establishes an exception.
- Every playable mech's signature weapon is one of these 15 weapons.
- Other mechs may fabricate that weapon under normal blueprint, profile, price, uniqueness, and slot rules.
- A mech cannot equip duplicate copies of a weapon.
- A damage-first automatic system can be a weapon regardless of whether it is a gun, beam, drone, turret, mine, contact aura, or ramming system.
- Weapon targeting and delivery may use enemy selection, movement or facing, radial patterns, orbits, automatic ground placement, or autonomous agents without manual combat inputs.
- Exactly six different catalog weapons serve as signatures for the six initial playable mechs.
- The 15 base concepts, pair positions, off-colors, native branch mappings, stat bundles, branch effects, six initial signature selections, and initial numerical values are fixed. Final names and audiovisual presentation remain open; numeric values remain playtest-tunable rather than underspecified.

## Accepted base catalog assignment

The 15 concepts, graph positions, stat bundles, branch sets, off-colors, native mappings, and signature selections below are accepted. Names are descriptive working labels rather than final presentation names. The last column preserves the catalog's status when DEC-075 was accepted; DEC-125 has since supplied the numerical baseline and moved all rows to **complete numerical baseline accepted; playtesting open**.

| ID | Recipe pair | Weapon concept | Base automatic attack | Ore-upgradeable stats | Amplification color / branch | Functional color / branch | Off-color / playstyle conversion | Signature mech | Status at DEC-075 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| W-AB | `A + B` | Rail Lance | Fires a slow, high-impact, very fast shot along persistent facing and pierces a fixed number of targets | Damage; width; range | `A` / Unbounded Bore | `B` / Fracture Lance | `C` / Kinetic Capacitor | Initial signature; mech TBD | complete high-level design accepted; tuning open |
| W-AC | `A + C` | Cluster Mortar | Fires at a telegraphed, committed ground position selected from the densest enemy concentration, then explodes there after a delay | Damage; blast radius; attack rate | `C` / Saturation Cascade | `A` / Interdiction Payload | `F` / Danger-Close Protocol | — | complete high-level design accepted; tuning open |
| W-AD | `A + D` | Gravity Projector | Creates a damaging pull pulse at an automatically selected ground location | Damage; field radius; field duration | `A` / Echo Well | `D` / Gravity Slingshot | `B` / Singularity Forge | Initial signature; mech TBD | complete high-level design accepted; tuning open |
| W-AE | `A + E` | Attack Drones | Maintains an indestructible autonomous squad that independently strafes nearby targets with short-range fire | Damage; attack rate; operational range | `E` / Replicator Swarm | `A` / Wolfpack Protocol | `D` / Containment Lattice | — | complete high-level design accepted; tuning open |
| W-AF | `A + F` | Tracking Laser | Maintains a focus-building beam on one target | Damage; range; focus rate | `A` / Coherence Memory | `F` / Target Designator | `B` / Cutting Vector | — | complete high-level design accepted; tuning open |
| W-BC | `B + C` | Pulse Repeater | Rapidly fires finite-speed projectiles at the nearest valid enemy | Damage; attack rate; range | `B` / Zero-Lag Emitter | `C` / Suppressive Sequencer | `E` / Broadside Oscillator | Initial signature; mech TBD | complete high-level design accepted; tuning open |
| W-BD | `B + D` | Mine Layer | Places mines at distance intervals along the mech's route | Damage; blast radius; active-mine capacity | `B` / Seed Charges | `D` / Selective Detonators | `F` / Hunter Mines | — | complete high-level design accepted; tuning open |
| W-BE | `B + E` | Sentry Pod | Deploys temporary stationary gun platforms at the mech's position | Damage; attack rate; range | `E` / Battery Overclock | `B` / Guardian Firmware | `A` / Forward Bastion | — | complete high-level design accepted; tuning open |
| W-BF | `B + F` | Orbital Cutters | Maintains contact-damage discs orbiting the mech | Damage; cutter size; orbit speed | `F` / Kinetic Flywheel | `B` / Deflection Ring | `E` / Tethered Reaper | — | complete high-level design accepted; tuning open |
| W-CD | `C + D` | Arc Emitter | Instantly chains damage through nearby unhit targets | Damage; attack rate; chain range | `C` / Total Conduction | `D` / Disruption Current | `B` / Ball-Lightning Projector | — | complete high-level design accepted; tuning open |
| W-CE | `C + E` | Reactor Pulse | Periodically damages every enemy around the mech | Damage; pulse radius; pulse rate | `E` / Critical-Mass Cycle | `C` / Kinetic Vent | `F` / Supernova Cycle | Initial signature; mech TBD | complete high-level design accepted; tuning open |
| W-CF | `C + F` | Wake Projector | Leaves temporary damaging trail segments behind movement | Damage; trail width; trail duration | `C` / Runaway Wake | `F` / Carrier Ignition | `D` / Circuit Closure | — | complete high-level design accepted; tuning open |
| W-DE | `D + E` | Scatter Array | Fires a fixed short-range projectile cone along persistent facing | Damage; attack rate; range | `D` / Saturation Choke | `E` / Concussive Fan | `C` / Focal Array | — | complete high-level design accepted; tuning open |
| W-DF | `D + F` | Ram Field | Maintains a damaging forward ram while the mech moves | Damage; ram width; knockback distance | `D` / Momentum Cascade | `F` / Impact Transfer | `A` / Siege Anchor | Initial signature; mech TBD | complete high-level design accepted; tuning open |
| W-EF | `E + F` | Missile Rack | Distributes a homing missile salvo among nearby targets | Damage; blast radius; launch rate | `F` / MIRV Saturation | `E` / Guardian Reserve | `D` / Spiral Barrage | Initial signature; mech TBD | complete high-level design accepted; tuning open |

### Assignment structure

The graph assignment deliberately distributes broad delivery families:

- Direct-fire or directly targeted weapons occupy the six-edge cycle `AB–BC–CD–DE–EF–FA`. Every resource is an endpoint for exactly two.
- Ground, route, or deployable weapons occupy `AC`, `AD`, `BD`, `BE`, and `CF`. Resources `A` through `D` touch two of these edges; `E` and `F` touch one.
- Body-centered or autonomous weapons occupy `AE`, `BF`, `CE`, and `DF`. Resources `A` through `D` touch one; `E` and `F` touch two.
- Initial signatures occupy the cycle `AB–BC–CE–EF–FD–DA`. Every resource appears in exactly two initial signature recipes. The six starting patterns are facing-based piercing, nearest-enemy projectiles, a radial pulse, homing missiles, movement-based ramming, and automatic ground control.

The off-color assignment has additional mathematical properties:

- `A`, `C`, and `E` each fund two playstyle conversions; `B`, `D`, and `F` each fund three.
- All 15 endpoint-plus-off-color signature sets are different.
- Across the 15 possible four-color profiles, 2 profiles expose two catalog playstyle conversions, 11 expose three, and 2 expose four. The average and median are both three.

These properties are balance aids, not reasons to preserve a weak weapon or branch concept. Weapon quality and coherent branches take precedence over perfect graph symmetry.

## Detailed batch 1

All high-level behaviors below are accepted. Exact numeric values and explicitly identified edge rules remain later tuning.

### W-AB — Rail Lance

**Base automatic behavior:** Whenever its cadence completes, Rail Lance fires a fast, finite-width projectile straight along the mech's persistent facing, whether or not an enemy is in that line. The lance damages each enemy at most once, passes through a fixed number of targets, and disappears when that target allowance is exhausted or it reaches maximum range. The target allowance is a fixed weapon property rather than an ore-upgradeable stat. The player aims it indirectly by moving to set facing before or between shots.

**Accepted common-ore stats:** damage, projectile width, and range. Rail Lance's slow firing cadence and very fast projectile speed are fixed weapon properties.

**Branches:**

- **`A` amplification — Unbounded Bore — accepted:** The lance no longer has a target-count limit. It pierces every enemy intersected before reaching maximum range, directly transcending the base weapon's defining horde-density limit without changing its facing-based line attack.
- **`B` functional variant — Fracture Lance — accepted:** Every pierced enemy emits a short perpendicular shockwave. The main attack remains a facing-based piercing line, but correctly threading dense groups now spreads damage sideways through the horde. Exact shockwave damage, reach, width, and repeated-hit rules remain tuning and edge-case work.
- **`C` playstyle conversion — Kinetic Capacitor — accepted:** Rail Lance becomes a slower charge weapon. It accumulates charge continuously and gains charge substantially faster while the mech moves; it automatically fires along persistent facing whenever fully charged. The resulting projectile has a much larger area and pierces every intersected target until maximum range. Charge timing is fixed branch behavior rather than an ore-upgradeable attack-rate track.

All three existing stat tracks continue to affect each form. Fracture shockwave damage and geometry derive visibly from the main lance's damage and width. Kinetic Capacitor inherits damage, range, and width while applying its large branch-specific area increase. Because target penetration, attack cadence, and projectile speed are not ore stats, the three branches do not invalidate prior investment.

### W-BC — Pulse Repeater

**Base automatic behavior:** Pulse Repeater selects the nearest enemy within targeting range and fires rapid projectiles toward its current position. A hit deals damage and applies a small outward impact force. It retargets between shots whenever another enemy becomes nearer or the current target becomes invalid. Projectiles continue on their fired trajectory rather than homing after launch.

**Accepted common-ore stats:** damage, attack rate, and range. Range controls enemy-acquisition distance in the targeted forms and lateral pulse travel distance for Broadside Oscillator. Base projectile speed and outward impact force are fixed weapon properties.

**Branches:**

- **`B` amplification — Zero-Lag Emitter — accepted:** Pulse travel becomes instantaneous. Each firing event hits the selected target immediately, eliminating misses caused by target movement and eliminating travel delay while preserving rapid nearest-enemy targeting.
- **`C` functional variant — Suppressive Sequencer — accepted:** Target selection favors nearby enemies not struck by the most recent pulses, distributing fire across the front of the horde. Hits briefly slow affected enemies, shifting the weapon from concentrated damage toward automatic suppression without abandoning rapid pulse fire. Exact recent-hit memory, target scoring, and slow values remain open.
- **`E` playstyle conversion — Broadside Oscillator — accepted:** The weapon stops selecting enemies. Each firing event launches rapid pulse fire in both directions perpendicular to persistent mech facing. The player circles or strafes alongside hordes to rake them laterally instead of relying on nearest-enemy aim. Exact pulse count, geometry, and per-pulse balance remain tuning.

All three stat tracks retain direct meanings in every branch. Broadside Oscillator applies attack rate to its firing events and range to lateral pulse travel. Because projectile speed is not an ore stat, Zero-Lag Emitter does not invalidate prior investment. Suppressive Sequencer's slow is fixed branch behavior rather than a fourth upgrade track.

### W-AD — Gravity Projector

**Base automatic behavior:** When ready and at least one enemy is in deployment range, Gravity Projector selects the densest nearby enemy concentration and creates a brief gravity pulse at its center. The pulse damages enemies and pulls them toward its center. It may create another pulse whenever its deployment cadence completes.

**Accepted common-ore stats:** damage, field radius, and field duration. Deployment cadence and placement range are fixed weapon properties. Duration provides the legible pull investment axis instead of a separate pull-force stat.

**Branches:**

- **`A` amplification — Echo Well — accepted:** Every deployed pulse repeats once at the same ground position after a fixed delay. The echo deals damage and applies the same pull again, rewarding successful placement without changing automatic cluster targeting.
- **`D` functional variant — Gravity Slingshot — accepted:** Each field first damages and pulls enemies together like the base weapon. At the end of the pulse, a second damaging burst hurls the gathered cluster away from the mech, retaining the grouping window before converting it into damage and player-relative space. Exact phase timing, second-hit damage, launch force, and direction-sampling rules remain open.
- **`B` playstyle conversion — Singularity Forge — accepted:** The weapon operates on a slow harvest-and-payoff cycle. A collection field pulls and damages aliens while accumulating mass from them, then compresses that mass into a micro-singularity round fired automatically at the strongest valid nearby enemy. Its impact creates an infrequent but devastating localized singularity. Denser and larger harvested groups increase the payoff; exact scaling, targeting, impact geometry, and edge rules remain open.

All three stat tracks retain visible effects in every branch. Echo Well applies them to both pulses. Gravity Slingshot applies duration to its gathering phase. Singularity Forge applies duration to its devastating impact singularity rather than its firing cadence, which remains slow.

## Detailed batch 2

### W-AC — Cluster Mortar

**Accepted base automatic behavior:** Whenever its firing cadence completes, Cluster Mortar identifies the densest enemy concentration within targeting range and launches an arcing shell toward that ground position. The impact position locks at launch and receives a visible warning marker throughout the travel delay. The shell does not track or retarget. On arrival, it explodes across an area; enemies that have left the marked area avoid the attack.

**Accepted common-ore stats:** damage, blast radius, and attack rate. Targeting range and travel delay are fixed weapon properties. Danger-Close Protocol ignores ordinary targeting range but retains the delay.

**Branches:**

- **`C` amplification — Saturation Cascade — accepted:** Every enemy damaged by the primary explosion seeds a smaller secondary explosion after a brief delay. Secondary blasts may overlap but cannot recursively seed further explosions. The mortar retains its automatic targeting, committed impact position, warning marker, and travel delay. Exact secondary delay, damage, area, repeated-hit rules, and behavior when a seeded enemy dies remain open.
- **`A` functional variant — Interdiction Payload — accepted:** The initial explosion remains, then its footprint becomes a temporary field that continues damaging and slowing enemies inside it. This converts a successful bombardment into area denial without sacrificing the base impact. Exact field duration, damage cadence, slow strength, stacking, and overlap rules remain open.
- **`F` playstyle conversion — Danger-Close Protocol — accepted:** The mortar stops selecting enemy concentrations and locks each impact marker beneath the mech at the firing moment. The delayed explosion is substantially larger and more devastating, so the player must remain near or circle the marker to lure pursuing aliens into it without being overwhelmed. The marker does not follow the mech, and its explosion does not damage the owning mech. Exact blast advantages remain tuning.

Exact concentration scoring, target tie-breaking, blast geometry, interactions with terrain, and numeric tuning remain open.

All three stat tracks remain meaningful in every branch. Saturation Cascade derives secondary damage and area from the primary blast. Interdiction Payload derives its field damage and footprint from the explosion. Danger-Close Protocol applies fixed branch multipliers to damage and radius while inheriting attack rate.

## Detailed batch 3

### W-AE — Attack Drones

**Accepted base automatic behavior:** The weapon maintains a small indestructible drone squadron. Each drone independently acquires a nearby valid enemy within operational range, flies out, and strafes it with short-range automatic fire. Drones may split across different targets. They retarget when necessary and return to a non-damaging orbit around the mech when no valid target remains. Enemies cannot target, damage, destroy, or physically block them.

**Accepted common-ore stats:** damage, attack rate, and operational range. Operational range becomes formation radius under Containment Lattice, and attack rate becomes lattice damage-tick rate. Squad size and movement speed remain fixed.

**Branches:**

- **`E` amplification — Replicator Swarm — accepted:** Whenever a permanent drone kills an enemy, it fabricates one temporary duplicate that fights like a normal drone before expiring. Temporary drones cannot replicate, and their simultaneous count is capped. Exact lifetime, cap, spawn behavior, and full-cap handling remain open.
- **`A` functional variant — Wolfpack Protocol — accepted:** The drones stop splitting targets and jointly designate one high-priority enemy, favoring bosses, elites, and then high-health threats. Every drone converges on it, and their combined effectiveness increases as more drones establish attack locks. The squad immediately designates another target when necessary. Exact priority, lock, bonus, and switching rules remain open.
- **`D` playstyle conversion — Containment Lattice — accepted:** The permanent drones stop seeking targets and firing. They hold a wide formation around the mech aligned to persistent facing, with damaging energy links between adjacent drones. Moving and turning carries the lattice edges through enemies; links do not physically block them. Exact formation geometry, rotation smoothing, beam thickness, and hit cooldown remain tuning.

All three stats retain direct meanings in every branch. Exact squad size, targeting, leash behavior, movement, firing, projectile, terrain, lattice, and numeric rules remain open.

## Detailed batch 4

### W-AF — Tracking Laser

**Base behavior:** The laser holds the nearest valid enemy until it becomes invalid and deals continuous damage. Maintaining the same target builds focus to a fixed cap and increases damage; changing targets normally resets focus.

**Common-ore stats:** damage, range, and focus rate. Beam width, tick cadence, and maximum focus are fixed.

**Branches:**

- **`A` amplification — Coherence Memory:** Focus transfers between targets and decays only while no target is held.
- **`F` functional — Target Designator:** Reaching a focus threshold exposes the target to increased damage from all player weapons for a brief duration.
- **`B` conversion — Cutting Vector:** The beam fires along persistent facing, pierces every enemy, and builds focus while the firing axis remains steady; turning beyond a tolerance resets focus.

Exact focus values, exposure rules, facing tolerance, and terrain interception remain tuning. See [DEC-065](./decisions/DEC-065-complete-tracking-laser.md).

### W-BD — Mine Layer

**Base behavior:** Traveling a fixed distance places an indestructible mine. After arming, it explodes when an enemy enters its trigger area. Parent mines have a fixed lifetime and an ore-upgradeable active capacity; placing above capacity removes the oldest without detonation.

**Common-ore stats:** damage, blast radius, and active-mine capacity. Placement interval, arming delay, and lifetime are fixed.

**Branches:**

- **`B` amplification — Seed Charges:** A parent explosion scatters short-lived non-recursive micro-mines.
- **`D` functional — Selective Detonators:** Mines wait for sufficient local density or an elite or boss, gain damage from targets and mass present, and may trigger normally near expiry.
- **`F` conversion — Hunter Mines:** Armed mines become mobile spider charges that pursue targets and explode on contact or pursuit expiry.

Exact triggers, seed layout, hunter movement, and terrain traversal remain tuning. See [DEC-066](./decisions/DEC-066-complete-mine-layer.md).

### W-BE — Sentry Pod

**Base behavior:** At a fixed cadence, an indestructible temporary pod deploys at the mech's current position, including when no enemy is present. Pods fire at their nearest targets. Capacity, lifetime, deployment cadence, and replacement of the oldest pod are fixed.

**Common-ore stats:** damage, attack rate, and range.

**Branches:**

- **`E` amplification — Battery Overclock:** Every additional active pod increases the attack rate of the entire active network up to a cap.
- **`B` functional — Guardian Firmware:** Pods prioritize enemies nearest the mech and push and stagger them away from it.
- **`A` conversion — Forward Bastion:** The network becomes one persistent, heavily multiplied bastion that deploys after the player holds an area and packs up when the player leaves its operating range.

Exact capacity, lifetime, setup, relocation, and multiplier rules remain tuning. See [DEC-067](./decisions/DEC-067-complete-sentry-pod.md).

### W-BF — Orbital Cutters

**Base behavior:** A fixed set of indestructible discs orbit the mech and deal contact damage under a short per-cutter, per-enemy hit cooldown.

**Common-ore stats:** damage, cutter size, and orbit speed. Cutter count, orbit radius, and contact cooldown are fixed.

**Branches:**

- **`F` amplification — Kinetic Flywheel:** Valid hits build temporary momentum that increases cutter damage and speed up to a cap.
- **`B` functional — Deflection Ring:** Cutters destroy interceptable enemy projectiles and emit a damaging outward shard burst on interception.
- **`E` conversion — Tethered Reaper:** All cutters fuse into one large tethered blade that lags behind movement and swings wide during turns; damage gains a speed-based multiplier.

Exact orbit, interception, momentum, and tether behavior remain tuning. See [DEC-068](./decisions/DEC-068-complete-orbital-cutters.md).

### W-CD — Arc Emitter

**Base behavior:** Each instant discharge strikes the nearest valid enemy, then chains through nearest unhit enemies within chain range until reaching a fixed target cap or losing connectivity.

**Common-ore stats:** damage, attack rate, and chain range. Initial acquisition range and base target cap are fixed.

**Branches:**

- **`C` amplification — Total Conduction:** The fixed target-count cap is removed; chaining ends only when no connected unhit enemy remains.
- **`D` functional — Disruption Current:** Every hit briefly stuns, with modestly longer stuns later in the chain and resistance scaling for strong enemies.
- **`B` conversion — Ball-Lightning Projector:** The instant chain becomes a slow orb launched along persistent facing that repeatedly arcs to nearby targets throughout its lifetime.

Exact routing, stun resistance, and orb behavior remain tuning. See [DEC-069](./decisions/DEC-069-complete-arc-emitter.md).

### W-CE — Reactor Pulse

**Base behavior:** At a regular cadence, an instantaneous radial pulse centered on the mech damages every enemy within its radius once and fires even in empty space.

**Common-ore stats:** damage, pulse radius, and pulse rate.

**Branches:**

- **`E` amplification — Critical-Mass Cycle:** Enemies hit contribute capped charge that increases the next pulse's damage and radius; charge is recalculated after each pulse.
- **`C` functional — Kinetic Vent:** Pulses also push enemies away from the mech and briefly slow them.
- **`F` conversion — Supernova Cycle:** Frequent pulses become one much slower, visibly charging, vastly larger and stronger supernova centered on the moving mech.

Exact charge, push, resistance, and supernova multipliers remain tuning. See [DEC-070](./decisions/DEC-070-complete-reactor-pulse.md).

### W-CF — Wake Projector

**Base behavior:** Movement lays contiguous temporary damaging wake segments at fixed distance intervals. Existing segments persist while stationary and expire individually.

**Common-ore stats:** damage, trail width, and trail duration. Placement interval and tick cadence are fixed.

**Branches:**

- **`C` amplification — Runaway Wake:** Uninterrupted movement builds capped momentum that increases the damage and width of newly laid trail.
- **`F` functional — Carrier Ignition:** Enemies damaged by the original wake temporarily leave their own non-recursive damaging trails.
- **`D` conversion — Circuit Closure:** The damaging trail becomes a conductive trace; crossing an active self-laid trace closes and consumes a loop, causing its enclosed area to erupt for high damage.

Exact momentum, ignition, self-intersection, enclosure, and minimum-loop rules remain tuning. See [DEC-071](./decisions/DEC-071-complete-wake-projector.md).

### W-DE — Scatter Array

**Base behavior:** Each shot launches a fixed number of fast, single-hit projectiles across a short fixed cone centered on persistent facing.

**Common-ore stats:** damage, attack rate, and range. Projectile count, cone angle, size, and speed are fixed.

**Branches:**

- **`D` amplification — Saturation Choke:** Discrete pellets become a continuous cone wave that hits every enemy in the cone once, removing gaps and first-target blocking.
- **`E` functional — Concussive Fan:** Pellets retain damage and add strong outward knockback plus brief stagger.
- **`C` conversion — Focal Array:** Pellets spread and then curve inward to converge at maximum range, rewarding placement of priority targets at the focal distance.

Exact cone geometry, multi-hit, displacement, and convergence rules remain tuning. See [DEC-072](./decisions/DEC-072-complete-scatter-array.md).

### W-DF — Ram Field

**Base behavior:** While the mech moves, a short field directly ahead along persistent facing damages and knocks back enemies under a brief per-enemy contact cooldown. It is inactive while stationary.

**Common-ore stats:** damage, ram width, and knockback distance. Forward reach and activation threshold are fixed.

**Branches:**

- **`D` amplification — Momentum Cascade:** Uninterrupted movement and impacts build capped momentum that increases damage and width.
- **`F` functional — Impact Transfer:** Rammed enemies become kinetic projectiles that damage and knock back other enemies they collide with.
- **`A` conversion — Siege Anchor:** Moving ram behavior is replaced by a circular barrier that arms after the mech remains stationary, damages and repels crossing enemies, and collapses on movement.

Exact momentum, collision chaining, resistance, setup, and barrier cooldown remain tuning. See [DEC-073](./decisions/DEC-073-complete-ram-field.md).

### W-EF — Missile Rack

**Base behavior:** Each fixed-size homing salvo distributes missiles among distinct nearby targets before assigning extras. Missiles retarget invalid targets and explode in a small area on contact.

**Common-ore stats:** damage, blast radius, and launch rate. Salvo size, targeting range, speed, turn rate, and lifetime are fixed.

**Branches:**

- **`F` amplification — MIRV Saturation:** Every missile splits once into non-recursive micro-missiles distributed among nearby targets.
- **`E` functional — Guardian Reserve:** Launched missiles orbit in a capped reserve and dive automatically at enemies entering a defensive radius.
- **`D` conversion — Spiral Barrage:** Homing is removed; every salvo launches in evenly spaced radial directions with a rotating offset and curves outward into a geometric spiral.

Exact distribution, split, reserve, and spiral rules remain tuning. See [DEC-074](./decisions/DEC-074-complete-missile-rack.md).

## Co-design method

Develop the catalog iteratively rather than finishing all weapon concepts and assigning them afterward:

1. Define a weapon's player-visible base behavior without reference to resource fiction.
2. Identify the fixed bundle of stats that common ore can improve.
3. Sketch one amplification branch, one functional variant, and one playstyle conversion; confirm that all three remain credible choices rather than ascending power tiers.
4. Place the weapon on a resource pair and assign its third color while considering relationships already created for those colors.
5. Revisit earlier assignments when a color becomes mechanically narrow, universally desirable, or consistently weak.
6. Test the resulting valid profiles for viability and pervasive bias, without forcing even role coverage.
7. Derive player-facing resource identities after the full set of recipe and branch relationships is visible.

## Soft profile-balance standard

Profiles are allowed to be asymmetrical. A run may lean toward short range, projectiles, persistent zones, burst damage, crowd control, or another pattern. Such skew is desirable when it changes play without predetermining failure.

Catalog review should intervene when:

- A profile offers no plausible combination for surviving the combined ordinary-horde and interval-boss schedule.
- A profile predictably creates an early survey state that informed players should abandon rather than play.
- A resource or weapon relationship causes the same deficiency across many mechs or profiles.
- One resource or pair becomes so broadly superior that its presence overwhelms the intended adaptation.
- Signature-aware generation repeatedly makes a mech's legal profiles worse than those of the rest of the roster.

These are playability and systemic-bias checks, not a requirement that every profile contain one weapon from every role taxonomy.

## Working assignment constraints

- Every third color differs from both recipe colors.
- Every row contains exactly one branch from each transformation category.
- Each row designates one recipe color for amplification and the other for the functional variant.
- The off-color always funds the playstyle conversion.
- Across 15 weapons and six possible third colors, perfect equality is impossible. A near-balanced assignment gives three colors two third-branch assignments and three colors three.
- The identities receiving two versus three assignments should be reconsidered after weapon and branch strength is understood; numeric balance alone does not guarantee gameplay balance.
- For each signature weapon, profile analysis uses only the 12 four-color profiles containing at least two of its three branch colors.
- Each valid profile supports six pair recipes. Because duplicates are forbidden, five or six of those represent legal additional weapons depending on whether the equipped signature's recipe pair is supported.

## Open tuning and content questions

- Which of the exact per-rank gains, base combat values, branch multipliers, control durations, caps, and weapon-specific edge rules in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md) survive playtesting unchanged?
- Which still-global targeting, terrain, overlap, collision, and simultaneous-event rules need system-wide standards beyond the weapon-specific answers in the numeric catalog?
- How should branch descriptions communicate transformations before purchase?

The six signature pairings and initial mech traits are fixed by the [Initial Mech Catalog](./36-initial-mech-catalog.md), numerical structure is fixed by DEC-125, and browsing, comparison, purchase, and branch presentation are fixed by the [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#fabrication).

## Related documents

- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Specialized Resource Identities](./61-specialized-resource-identities.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [DEC-036 — Use six-color signature-aware resource profiles](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-037 — Use unique weapons and soft profile balance](./decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [DEC-038 — Use a broad automatic-weapon taxonomy](./decisions/DEC-038-broad-automatic-weapon-taxonomy.md)
- [DEC-039 — Target a six-mech initial roster](./decisions/DEC-039-six-mech-initial-roster.md)
- [DEC-040 — Use a three-level weapon-branch transformation gradient](./decisions/DEC-040-three-branch-transformation-gradient.md)
- [DEC-041 — Use an equal-tier base-weapon catalog](./decisions/DEC-041-equal-tier-base-weapon-catalog.md)
- [DEC-042 — Use movement-derived persistent mech facing](./decisions/DEC-042-movement-derived-persistent-facing.md)
- [DEC-043 — Assign the fifteen base weapons to the resource graph](./decisions/DEC-043-fifteen-weapon-graph-assignment.md)
- [DEC-045 — Define the first signature-weapon amplifications](./decisions/DEC-045-first-signature-amplification-branches.md)
- [DEC-046 — Define the Rail Lance branch set](./decisions/DEC-046-rail-lance-branch-set.md)
- [DEC-047 — Limit weapons to three common-ore stats](./decisions/DEC-047-three-stat-weapon-bundles.md)
- [DEC-048 — Give Pulse Repeater a suppressive functional branch](./decisions/DEC-048-pulse-repeater-suppressive-sequencer.md)
- [DEC-049 — Convert Pulse Repeater into a broadside weapon](./decisions/DEC-049-pulse-repeater-broadside-oscillator.md)
- [DEC-050 — Give Pulse Repeater damage, rate, and range stats](./decisions/DEC-050-pulse-repeater-stat-bundle.md)
- [DEC-051 — Give Gravity Projector a two-stage slingshot branch](./decisions/DEC-051-gravity-projector-slingshot.md)
- [DEC-052 — Convert Gravity Projector into a Singularity Forge](./decisions/DEC-052-gravity-projector-singularity-forge.md)
- [DEC-053 — Give Gravity Projector damage, radius, and duration stats](./decisions/DEC-053-gravity-projector-stat-bundle.md)
- [DEC-054 — Give Cluster Mortar delayed, committed area targeting](./decisions/DEC-054-cluster-mortar-base-behavior.md)
- [DEC-055 — Amplify Cluster Mortar with Saturation Cascade](./decisions/DEC-055-cluster-mortar-saturation-cascade.md)
- [DEC-056 — Give Cluster Mortar an Interdiction Payload](./decisions/DEC-056-cluster-mortar-interdiction-payload.md)
- [DEC-057 — Convert Cluster Mortar to Danger-Close Protocol](./decisions/DEC-057-cluster-mortar-danger-close-protocol.md)
- [DEC-058 — Make Danger-Close Protocol harmless to its owner](./decisions/DEC-058-danger-close-no-self-damage.md)
- [DEC-059 — Give Cluster Mortar damage, radius, and rate stats](./decisions/DEC-059-cluster-mortar-stat-bundle.md)
- [DEC-060 — Assign native branch funding for catalog balance](./decisions/DEC-060-balance-native-branch-funding.md)
- [DEC-061 — Use an autonomous, indestructible attack-drone squadron](./decisions/DEC-061-attack-drones-base-behavior.md)
- [DEC-062 — Amplify Attack Drones with a Replicator Swarm](./decisions/DEC-062-attack-drones-replicator-swarm.md)
- [DEC-063 — Give Attack Drones a Wolfpack Protocol](./decisions/DEC-063-attack-drones-wolfpack-protocol.md)
- [DEC-064 — Complete Attack Drones with Containment Lattice and three stats](./decisions/DEC-064-complete-attack-drones.md)
- [DEC-065 — Complete the Tracking Laser weapon](./decisions/DEC-065-complete-tracking-laser.md)
- [DEC-066 — Complete the Mine Layer weapon](./decisions/DEC-066-complete-mine-layer.md)
- [DEC-067 — Complete the Sentry Pod weapon](./decisions/DEC-067-complete-sentry-pod.md)
- [DEC-068 — Complete the Orbital Cutters weapon](./decisions/DEC-068-complete-orbital-cutters.md)
- [DEC-069 — Complete the Arc Emitter weapon](./decisions/DEC-069-complete-arc-emitter.md)
- [DEC-070 — Complete the Reactor Pulse weapon](./decisions/DEC-070-complete-reactor-pulse.md)
- [DEC-071 — Complete the Wake Projector weapon](./decisions/DEC-071-complete-wake-projector.md)
- [DEC-072 — Complete the Scatter Array weapon](./decisions/DEC-072-complete-scatter-array.md)
- [DEC-073 — Complete the Ram Field weapon](./decisions/DEC-073-complete-ram-field.md)
- [DEC-074 — Complete the Missile Rack weapon](./decisions/DEC-074-complete-missile-rack.md)
- [DEC-075 — Accept the complete initial weapon catalog for playtesting](./decisions/DEC-075-accept-complete-initial-weapon-catalog.md)
- [DEC-124 — Adopt a multi-metric weapon balance framework](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-076 — Give the six specialized resources strong non-exclusive identities](./decisions/DEC-076-specialized-resource-identities.md)
- [RES-006 — Resource-color graph for weapon availability](./research/RES-006-resource-color-weapon-graph.md)
