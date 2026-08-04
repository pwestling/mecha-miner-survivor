---
doc_id: GDD-INITIAL-ALIEN-ROSTER
title: Initial Alien and Boss Roster
status: active
authoritative: true
---

# Initial Alien and Boss Roster

## Catalog status

This document defines the accepted initial standard-map roster of ten ordinary alien identities, the shared elite treatment, and four interval bosses. [DEC-119](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md) accepts this content as a coherent first balance baseline. Names, exact values, and presentation details remain adjustable through playtesting under the [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md), but the simple pursuer-first structure, silhouette relationships, single specialist, and one-mechanic boss identities are authoritative.

## Design relationship to the genre reference

The roster follows the broad pressure method of a normal *Vampire Survivors* stage rather than copying its creatures. Most danger comes from large numbers of simple contact pursuers whose fixed durability, speed, size, and density become threatening in different combinations. New visual identities, production-efficient variants, authored formations, elites, and four persistent bosses create escalation without turning every alien into an independent tactical puzzle.

The science-fiction reskin presents the horde as an invasive extraterrestrial biosphere responding to industrial mech activity. Organic plates, mineral inclusions, bioluminescent organs, and exaggerated fully top-down silhouettes distinguish enemies from low-poly machines and resource formations.

## Shared ordinary-enemy rules

- Nine identities continuously pursue the mech and threaten only through contact.
- Needler is the sole ordinary specialist. It retains pursuit and contact behavior and adds one telegraphed straight projectile.
- Every identity uses one fixed base profile throughout the standard run. Reappearance later does not invisibly scale it.
- Ordinary enemies have no Armor. Their listed control resistance reduces player-authored displacement magnitude and timed control duration through the exact rules in the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md).
- Ordinary enemies do not collide with the mech, one another, mining points, pickups, or other enemies. Solid world terrain still constrains their navigation and spawn positions.
- An overlapping enemy deals its listed contact damage immediately when eligible and then once every 0.75 seconds while that same overlap continues.
- After receiving contact damage, the mech has a 0.20-second global contact-damage grace period. Other enemies cannot deal another contact instance during that grace, but enemy projectiles and explicit hazards remain independently eligible.
- Damage does not make the mech flinch, move, stop mining, or lose control.
- Ordinary enemies and elites drop nothing. Their defeat creates space, statistics, and any explicitly authored relic or challenge interaction, but no XP, ore, materials, Hyper Gold, repair, or temporary pickup.
- Listed numbers are initial prototype values against a 100-Hull baseline and must be tuned together with weapon damage, camera scale, collision footprints, and population density. DEC-126 fixes the first-playable movement, footprint, damage-resolution, healing, and failure-margin assumptions used to evaluate them.

## Ordinary roster overview

Movement speed is shown as a percentage of the shared 3.0M/s unmodified mech movement speed. Body scale multiplies the Ripper's 0.80M contact diameter, not its decorative mesh. Exact derived values and boss circles appear in the [survivability baseline](./72-player-survivability-and-damage-baseline.md#collision-and-contact-footprints).

| ID | Identity | Family | Hull | Move | Contact | Body | Control resistance | Earliest minute |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `EN-01` | Skitterling | Six-legged swarm | 20 | 42% | 5 | 0.55× | 0% | 0 |
| `EN-02` | Ripper | Lean quadruped | 45 | 62% | 8 | 1.00× | 10% | 1 |
| `EN-03` | Shellback | Armored beetle | 150 | 34% | 14 | 1.30× | 45% | 4 |
| `EN-04` | Lurker | Tall tripod | 75 | 52% | 10 | 1.05× | 20% | 5 |
| `EN-05` | Gloomwing | Manta-like hoverer | 90 | 58% | 10 | 1.20× | 25% | 10 |
| `EN-06` | Needler | Ranged stalk | 80 | 40% | 8 | 1.00× | 25% | 16 |
| `EN-07` | Razorling | Skitterling variant | 60 | 85% | 10 | 0.62× | 15% | 13 |
| `EN-08` | Iron Ripper | Ripper variant | 220 | 55% | 16 | 1.10× | 55% | 18 |
| `EN-09` | Siegeback | Shellback variant | 650 | 28% | 24 | 1.65× | 80% | 22 |
| `EN-10` | Dreadwing | Gloomwing variant | 320 | 70% | 18 | 1.35× | 65% | 24 |

## Six silhouette families

### EN-01 — Skitterling

A low, tiny six-legged scavenger with a triangular head and wide-spread legs. Skitterlings are fragile orientation enemies and later provide dense chain-reaction or piercing fuel. Their broad leg stance keeps a crowd readable as many separate bodies rather than one texture-colored mass.

### EN-02 — Ripper

A narrow predatory quadruped with a long forward jaw and raised blade-like shoulders. Rippers are the standard early pursuer: fast enough to require movement, durable enough to register weapon hits, and visually neutral enough to combine with every other family.

### EN-03 — Shellback

A slow beetle-like alien whose large rounded carapace and short legs produce a broad solid silhouette. Shellbacks create durable moving obstacles without physically blocking the mech and teach that some bodies should be routed around or concentrated under sustained fire.

### EN-04 — Lurker

A tall three-legged walker with a small central body and three long asymmetrical limbs. It is a middle-weight pursuer between Ripper speed and Shellback durability. Its rotating gait and open negative spaces provide a sixth production silhouette without a new attack behavior.

### EN-05 — Gloomwing

A manta-like organism hovering just above the ground with swept wings and a luminous central eye. It follows the same navigable-ground rules as walking enemies and cannot cross walls or voids. The hovering presentation varies animation and top-down shape without granting mechanical flight.

### EN-06 — Needler

A narrow stalk-like organism with a visibly swollen projectile sac and two rear stabilizing limbs. It continues pursuing, but every 4.5 seconds it begins a conspicuous 0.8-second charge, samples the mech's current position, and fires one straight non-homing needle along that line.

- The projectile does not lead, retarget, split, explode, leave a hazard, or apply a status.
- Its initial speed is 75% of the unmodified mech's movement speed, or 2.25M/s; its damage is 14, and its lifetime carries it slightly beyond one screen width.
- Needler continues moving at half speed while charging and returns to full pursuit immediately after firing.
- A bright body contraction, contrasting line flash, and unique audio chirp make the shot readable without relying on projectile color.
- Cinderglass resonance increases projectile damage; Eidolon Coral resonance increases firing frequency. Flux Amber affects Needler movement but not projectile speed.

## Four production-efficient variants

### EN-07 — Razorling

The Razorling uses the Skitterling rig and broad six-legged grammar but adds long forward leg blades, a raised abdomen, a larger footprint, a sharper gait, and a distinct two-tone value pattern. It is the fastest ordinary identity and converts familiar fodder into late-run gap-closing pressure.

### EN-08 — Iron Ripper

The Iron Ripper uses the Ripper rig with heavier shoulders, shortened jaws, mineral armor bands, louder footfalls, and a visibly denser body. It gives late waves a durable medium-speed body without hiding an upgraded Ripper behind color alone.

### EN-09 — Siegeback

The Siegeback enlarges the Shellback family into a slow late-run damage sink with a jagged crown, squared plate outline, heavy ground shadow, and distinct stagger feedback. Its fixed profile is highly resistant to control but it retains pure pursuit and contact behavior.

### EN-10 — Dreadwing

The Dreadwing is a larger, faster Gloomwing with forked wings, a hollow center, trailing tendrils, and stronger animation contrast. It supplies a durable fast late-run mass threat while remaining bound to ordinary navigation and contact rules.

## Elite treatment

An elite is a visibly enhanced instance of one of the nine pure pursuers. Needler does not become an elite in the initial standard schedule because combining its projectile with the shared elite multipliers reduces readability.

| Elite property | Modifier |
| --- | ---: |
| Maximum Hull | 4× |
| Movement speed | 1.10× |
| Contact damage | 1.50× |
| Body scale | 1.25× |
| Added control resistance | +25 percentage points, capped at 90% |

- Elites use a luminous crown organ, thicker outline, persistent minimap pip, distinct entrance cry, and larger death effect. Scale or palette alone is insufficient.
- Elites retain the base identity's pure pursuit and contact behavior and add no attacks, phases, aura, support AI, or loot.
- Standard minute events introduce at most two scheduled elites at once. Hyper Gold beacons may temporarily add their separately specified elite counts.
- Elites may be recycled only under ordinary off-screen pressure rules; beacon-tagged elites remain active until killed or run end.

## Interval boss overview

Each boss is a persistent giant pursuer with exactly one additional behavior. Bosses never despawn, do not count toward ordinary population, and make a readable off-screen re-entry if left behind. Their ordinary contact rules match the shared cadence, while their added attack defines its own damage event.

| ID | Boss | Arrival | Initial Hull | Move | Contact | Control resistance | Defining behavior |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `BOSS-01` | Riftjaw | 7:00 | 6,000 | 42% | 18 | 85% | Telegraphs and performs a straight charge |
| `BOSS-02` | Brood Titan | 14:00 | 14,000 | 38% | 24 | 90% | Periodically sheds a Skitterling ring |
| `BOSS-03` | Prism Crown | 21:00 | 30,000 | 45% | 30 | 92% | Fires a radial needle burst |
| `BOSS-04` | Skybreaker Apex | 28:00 | 45,000 | 50% | 38 | 95% | Leaps onto a locked ground marker |

Boss Hull values are initial anchors rather than a substitute for time-to-kill validation. Against an appropriately developed fresh-account build, target roughly 45–75 seconds for Riftjaw, 60–90 seconds for Brood Titan, 75–105 seconds for Prism Crown, and 90–120 seconds for Skybreaker Apex. The values are validated against the legal no-relic progression in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md). A weak or poorly matched build may take longer and risk scheduled overlap.

## BOSS-01 — Riftjaw

Riftjaw is a long burrowing predator with an armored wedge head and a segmented body that remains fully visible above ground during ordinary pursuit.

- Every 8 seconds, Riftjaw stops for 1 second, displays a wide straight charge lane toward the mech's sampled position, then charges without turning for 1.5 seconds at 180% of unmodified mech speed, or 5.40M/s.
- Charge contact deals 27 damage instead of 18. Missing carries Riftjaw past the player and creates a brief damage opportunity before pursuit resumes.
- Terrain stops the charge without stunning or damaging the boss. The lane never knowingly selects a route that immediately collides with terrain before covering one body length.
- This first boss checks movement, facing awareness, and early single-target damage without adding projectiles or summons.

## BOSS-02 — Brood Titan

Brood Titan is a massive low-slung carrier whose split dorsal shell visibly contains smaller organisms.

- Every 10 seconds, it pauses for 0.8 seconds and releases 16 Skitterlings in an incomplete ring just outside its body.
- The ring preserves a clearly visible 90-degree opening oriented generally away from the boss's direction of approach, ensuring the spawn is pressure rather than an unavoidable enclosure.
- Spawned Skitterlings are ordinary EN-01 instances, count as event overflow, drop nothing, and follow ordinary recycling rules.
- The boss tests area coverage and prevents pure single-target focus without introducing a new minion identity.

## BOSS-03 — Prism Crown

Prism Crown is a tall radial organism whose twelve mineral spines flare before discharging.

- Every 7 seconds, it stops for a 1.2-second conspicuous charge and fires twelve evenly spaced straight projectiles in every direction.
- Each projectile deals 18 damage, travels at 75% of base mech movement speed or 2.25M/s, does not home, and disappears after crossing slightly more than one screen width or hitting solid terrain.
- The radial offset alternates by 15 degrees on each burst so static lanes do not remain permanently safe.
- Cinderglass resonance increases projectile damage and Eidolon Coral resonance increases burst frequency while the boss is inside the relevant field.
- This is one radial-burst behavior, not twelve independently targeted attacks.

## BOSS-04 — Skybreaker Apex

Skybreaker Apex is a towering four-limbed predator with folded dorsal membranes that open only for its leap.

- Every 9 seconds, it marks a circular area centered on the mech's sampled position, crouches for 1.5 seconds, becomes non-damaging while airborne, and lands at that fixed marker.
- Landing deals 35 damage inside the circle. The marker does not track after appearing, and its boundary plus countdown remain visible beneath player and enemy effects.
- The landing cannot place the boss in solid terrain; an invalid point moves to the nearest valid ground without moving the marker closer to the mech.
- Ordinary pursuit and contact resume immediately after landing. The leap provides the boss's anti-distance pressure without adding a second ability.

## Boss arrival, persistence, and reward

- A 15-second HUD warning, directional boss icon, distinctive distant cry, and edge treatment precede each scheduled arrival.
- At the exact threshold, one boss enters from valid navigable ground outside the camera. It never spawns on the mech, inside an active mining zone, or in an unavoidable collision path.
- Each boss persists until killed. Later bosses arrive on schedule and may overlap it.
- If a boss remains far outside the combat area, it re-enters from a telegraphed valid off-screen approach. Its added-behavior cooldown restarts after re-entry so it cannot attack invisibly.
- Bosses receive the same explicit geode resonance modifiers as ordinary enemies. Control resistance combines with Driftmetal resonance without reaching complete immunity unless a later rule says so.
- Boss death uses the accepted non-modal physical burst: 300 common ore, 25 unsecured Hyper Gold, and one random present-profile material from the first two bosses or two from the final two.
- Bosses do not need to die for mission extraction. Any living boss disappears with the rest of the run state when extraction triggers at 35:00.

## Presentation and asset constraints

- All ten ordinary identities and four bosses must remain distinguishable as solid top-down silhouettes at normal Steam Deck gameplay zoom.
- Variant pairs share a production rig but not an easily confused combat outline, scale, motion, and value structure.
- Enemy value range and material treatment remain distinct from the six resource families; no alien can masquerade as a geode, pickup, player projectile, or Hyper Gold marker.
- Contact bodies use grounded shadows and readable hurt flashes. Decorative limbs and wings do not silently enlarge the damage footprint.
- Needler and boss telegraphs remain legible during the maximum authored ordinary population and common player VFX, with reduced-flash and high-contrast alternatives.

## First-playable subset

A six-identity first playable uses Skitterling, Ripper, Shellback, Lurker, Needler, and Razorling, plus Riftjaw and Brood Titan. When testing later schedule minutes before the remaining content exists, Iron Ripper substitutes with an elite Ripper, Siegeback with an elite Shellback, and both wing identities with Lurkers at equivalent population weight. These substitutions are temporary production scaffolding and not content-complete balance.

## Tuning and validation

- Test ordinary profiles separately from schedule density: changing minute population must not disguise an identity whose fixed profile is unreadable or unfair.
- Validate all 15 four-material profiles and all six mechs across fresh, partial, and highly upgraded accounts.
- Confirm that every major weapon geometry has useful horde targets without creating a roster hard-counter requirement.
- Contact damage should punish sustained overlap without producing unexplained instantaneous death from simultaneous bodies.
- Needler projectiles and boss attacks must remain dodgeable during active mining when the player uses the extraction zone's available space well.
- Boss overlap should be a serious consequence of low damage, not a normal expectation for a reasonably coherent build.
- Final values may change after instrumentation, but ordinary identities never receive hidden elapsed-time or player-responsive scaling.

## Related documents

- [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](./20-run-structure-and-timing.md)
- [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Initial Relic Catalog](./69-initial-relic-catalog.md)
- [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [DEC-105 — Use a simple pursuer-first enemy roster](./decisions/DEC-105-use-a-simple-pursuer-first-enemy-roster.md)
- [DEC-106 — Use ten ordinary enemy identities](./decisions/DEC-106-use-ten-ordinary-enemy-identities.md)
- [DEC-107 — Use fixed ordinary enemy stat profiles](./decisions/DEC-107-use-fixed-ordinary-enemy-stat-profiles.md)
- [DEC-108 — Use one straight-shot enemy specialist](./decisions/DEC-108-use-one-straight-shot-enemy-specialist.md)
- [DEC-111 — Make bosses explode into collectible resources](./decisions/DEC-111-make-bosses-explode-into-resources.md)
- [DEC-119 — Accept the initial alien encounter baseline](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [DEC-124 — Adopt a multi-metric weapon balance framework](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
- [RES-001 — Vampire Survivors reference mechanics](./research/RES-001-vampire-survivors-reference.md)
