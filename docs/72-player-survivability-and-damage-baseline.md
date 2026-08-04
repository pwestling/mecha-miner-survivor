---
doc_id: GDD-PLAYER-SURVIVABILITY-BASELINE
title: Player Survivability and Damage Baseline
status: active
authoritative: true
---

# Player Survivability and Damage Baseline

Status: **authoritative first-playable baseline**. These values make player movement, collision, damage, recovery, control, and failure expectations implementable without claiming final balance before playtesting.

This document completes the numerical survival layer surrounding the [Initial Alien and Boss Roster](31-initial-alien-roster.md), [Standard Wave and Beacon Schedule](32-standard-wave-and-beacon-schedule.md), and [Initial Weapon Numeric Catalog](71-initial-weapon-numeric-catalog.md). [DEC-126](decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md) accepts it.

Machine-readable mirrors: [survivability baseline data](data/survivability-baseline.csv) and [contact damage-pressure data](data/contact-damage-pressure.csv).

If the CSV and this document disagree, this document is authoritative.

## Design Intent

Standard mode should punish repeated positional mistakes without killing a healthy fresh-account mech through one ordinary unreadable event.

- The player begins sturdy enough to learn from one mistake.
- Remaining overlapped by a crowd is intentionally fatal; enemies being non-solid is not permission to tank through them.
- Telegraphed boss attacks are serious but never one-shot the shared 100-Hull baseline.
- Two maximum-strength standard damage instances cannot kill a full fresh mech; a third may.
- Recovery is uncertain and exploration-driven unless the player invests in explicit passive Recovery.
- Armor, maximum Hull, Recovery, shields, and revival remain distinct defenses rather than interchangeable versions of one hidden durability score.
- No hidden adaptive damage, healing pity, or comeback modifier changes these values according to player health or performance.

## Shared Player Baseline

| Property | Initial value |
|---|---:|
| Maximum Hull Integrity | 100 |
| Starting Hull Integrity | Current maximum |
| Armor | 0 |
| Passive Recovery | 0 Hull/s |
| Revival charges | 0 |
| Base movement speed | 3.0M/s |
| Mech collision diameter | 1.0M |
| Mech collision shape | Circle |
| Same-enemy contact repeat interval | 0.75 s |
| Global contact grace after a resolved contact | 0.20 s |
| Universal post-hit invulnerability | None |
| Health-pack repair | 25 Hull |

`M` is one unmodified mech collision diameter. One base-travel second therefore equals 3.0M of shortest-path travel. Map-generation distance bands always use this unmodified speed even when the current mech has movement bonuses.

The collision circle is used for blocking terrain, enemy contact, pickups, and damage zones unless an explicit attack presents a different player-facing boundary. Decorative limbs, weapons, shadows, antennae, and effects never enlarge it.

## Movement and Speed Modifiers

- Input immediately moves the mech at its current full speed and release stops it immediately under the accepted direct-movement rules.
- Percentage movement bonuses add by percentage points and apply once to the 3.0M/s baseline.
- Razorback with its +10% trait moves at 3.30M/s.
- A non-Razorback with maximum Servo Overdrive and Rank-3 Vector Thrusters moves at 3.75M/s.
- Razorback with both moves at 4.05M/s: `3.0 × (1 + 0.10 + 0.10 + 0.15)`.
- Movement bonuses never change the fixed base-travel distances used by map generation, resource separation, or reference measurements.
- Enemies remain non-solid. Crossing through an enemy does not slow or redirect the mech, but its contact footprint can still deal damage during the overlap.

The base enemy percentages now correspond to these world speeds:

| Enemy | Move | World speed |
|---|---:|---:|
| Skitterling | 42% | 1.26M/s |
| Ripper | 62% | 1.86M/s |
| Shellback | 34% | 1.02M/s |
| Lurker | 52% | 1.56M/s |
| Gloomwing | 58% | 1.74M/s |
| Needler | 40% | 1.20M/s |
| Razorling | 85% | 2.55M/s |
| Iron Ripper | 55% | 1.65M/s |
| Siegeback | 28% | 0.84M/s |
| Dreadwing | 70% | 2.10M/s |
| Riftjaw pursuit | 42% | 1.26M/s |
| Brood Titan pursuit | 38% | 1.14M/s |
| Prism Crown pursuit | 45% | 1.35M/s |
| Skybreaker Apex pursuit | 50% | 1.50M/s |

The shared elite 1.10× movement multiplier applies after these values. Flux Amber resonance then multiplies current enemy movement by 1.20; Claim-Jumper Core multiplies it by 1.50 only while mining progress advances. Those distinct sources multiply because they describe different conditional transformations rather than additions to one displayed player stat.

Riftjaw's charge is a numerical exception to its ordinary pursuit: after its one-second warning it moves at 180% of base mech speed, or 5.40M/s, for 1.5 seconds. This replaces the weaker provisional twice-pursuit-speed value and makes the visible lane demand a lateral dodge.

## Collision and Contact Footprints

The Ripper's rank-zero contact diameter is 0.80M. Every ordinary body scale in the alien roster multiplies that diameter. Contact begins when the enemy contact circle and the mech's 0.50M-radius collision circle overlap.

| Enemy | Contact diameter | Center distance that begins contact |
|---|---:|---:|
| Skitterling | 0.44M | 0.72M |
| Ripper | 0.80M | 0.90M |
| Shellback | 1.04M | 1.02M |
| Lurker | 0.84M | 0.92M |
| Gloomwing | 0.96M | 0.98M |
| Needler | 0.80M | 0.90M |
| Razorling | 0.50M | 0.75M |
| Iron Ripper | 0.88M | 0.94M |
| Siegeback | 1.32M | 1.16M |
| Dreadwing | 1.08M | 1.04M |

Elite contact diameters are 1.25× their identity's value. Decorative animation may extend beyond the circle, but the persistent ground shadow and under-body contact tell must show the real gameplay footprint.

Bosses use simple circular gameplay footprints even when their meshes are elongated or irregular:

| Boss | Contact and weapon-hurt diameter | Center distance that begins contact |
|---|---:|---:|
| Riftjaw | 1.50M | 1.25M |
| Brood Titan | 2.00M | 1.50M |
| Prism Crown | 1.60M | 1.30M |
| Skybreaker Apex | 1.90M | 1.45M |

Boss appendages may pass over the mech harmlessly if they lie outside this footprint. Charge lanes, projectile shapes, and landing circles use their separately displayed attack geometry rather than the boss contact circle.

## Incoming Damage Resolution

Every eligible incoming attack resolves in this order:

1. Confirm that its per-attacker cooldown, global contact grace, or explicit attack hit rule permits a hit.
2. Multiply listed base damage by current attacker-side damage modifiers.
3. Round the result up to the next whole Hull point.
4. Subtract current Armor, to a minimum of one damage unless the effect ignores Armor.
5. Apply an eligible full-hit negation such as Capacitor Screen.
6. Remove the remaining value from current Hull.
7. If Hull would reach zero, resolve an eligible revival; otherwise the mech dies.

Example: Skybreaker Apex deals 38 contact damage. Asterite resonance makes this `38 × 1.20 = 45.6`, rounded up to 46. A fresh zero-Armor mech loses 46 Hull; three Armor reduces it to 43.

Damage is displayed and recorded as whole Hull points. Multiple hits at the same moment remain separate sequential damage instances rather than combining into one; a one-hit shield negates only one of them.

## Contact Cadence and Damage Grace

- An enemy deals contact damage immediately when an eligible overlap begins.
- That same enemy cannot attempt contact again for 0.75 active-simulation seconds, even if the overlap ends and resumes during the cooldown.
- A resolved contact attempt starts 0.20 seconds of global **contact grace**. No other enemy may resolve contact during it.
- A contact fully negated by a shield still starts that attacker's cooldown and the global contact grace. Otherwise a shield could be consumed invisibly and another overlapping body could deal damage on the same frame.
- Contact grace affects contact only. Projectiles, boss landing attacks, explicit hazards, and Armor-ignoring self-damage remain independently eligible.
- The current standard roster grants no universal post-hit invulnerability, hitstun, knockback, movement loss, mining interruption, or input suppression.
- Emergency Reboot's accepted two-second all-damage invulnerability is an explicit exception. Attacks ignored during Reboot invulnerability do not consume Capacitor Screen.

Global contact grace limits a dense crowd to at most five contact instances per second, but it does not make that exposure survivable. A player trapped among late enemies can still die in roughly one second and is expected to escape rather than exploit non-solid bodies.

Eidolon Coral resonance divides an enemy's contact repeat interval and named attack cooldown by 1.20. It does not shorten telegraphs, projectile life, or the mech's 0.20-second global contact grace.

## Damage Pressure Reference

The following table assumes 100 Hull, zero Armor, no healing, no resonance, and uninterrupted overlap with one attacker. Time to death includes the immediate first hit and then 0.75 seconds between that enemy's later hits.

| Attacker | Damage | Hits to defeat | Continuous-overlap time to defeat |
|---|---:|---:|---:|
| Skitterling | 5 | 20 | 14.25 s |
| Ripper | 8 | 13 | 9.00 s |
| Shellback | 14 | 8 | 5.25 s |
| Lurker | 10 | 10 | 6.75 s |
| Gloomwing | 10 | 10 | 6.75 s |
| Needler contact | 8 | 13 | 9.00 s |
| Razorling | 10 | 10 | 6.75 s |
| Iron Ripper | 16 | 7 | 4.50 s |
| Siegeback | 24 | 5 | 3.00 s |
| Dreadwing | 18 | 6 | 3.75 s |
| Riftjaw pursuit | 18 | 6 | 3.75 s |
| Brood Titan pursuit | 24 | 5 | 3.00 s |
| Prism Crown pursuit | 30 | 4 | 2.25 s |
| Skybreaker Apex pursuit | 38 | 3 | 1.50 s |

Discrete specialist and boss attacks use these margins:

| Attack | Base damage | Relevant 20% resonance result | Fresh-mech hits to defeat at resonant value |
|---|---:|---:|---:|
| Needler projectile | 14 | 17 under Cinderglass | 6 |
| Riftjaw charge | 27 | 33 under Asterite | 4 |
| Prism Crown projectile | 18 | 22 under Cinderglass | 5 |
| Skybreaker landing | 35 | 42 under Asterite | 3 |
| Skybreaker contact | 38 | 46 under Asterite | 3 |
| Elite Siegeback contact | 36 | 44 under Asterite | 3 |

The strongest initial standard damage instance is therefore 46 Hull. A full 100-Hull fresh mech always survives any two standard incoming instances, including two worst-case resonant Skybreaker contacts, but may die to a third. Armor-ignoring relic self-damage and future explicitly exceptional attacks are outside this guarantee.

## Health Packs and Destructible Rocks

### Health pack

- One pack repairs 25 Hull immediately and cannot exceed current maximum Hull.
- A pack is consumed at full Hull and wastes all excess repair above maximum.
- Repair amount is unaffected by Armor, Recovery, maximum-Hull modifiers, PowerUps, utilities, relics, current health, elapsed time, or hidden difficulty adjustment.
- The pack has a 0.25M pickup radius. With the standard mech circle, collection occurs when centers come within 0.75M.
- Packs remain non-solid, persist until collected or run end, have no attraction beyond contact, and receive no persistent map or radar marker.

Twenty-five Hull repairs five Skitterling hits, three Ripper hits, one unmodified late heavy hit, or slightly more than half of the worst 46-Hull resonant hit. Four fully effective packs repair one complete fresh-mech health bar.

### Destructible rock

| Property | Initial value |
|---|---:|
| Hull | 100 |
| Armor | 0 |
| Damage footprint diameter | 0.80M |
| Movement collision | Non-solid to mech and enemies |
| Control response | Immune; displacement and status are ignored |
| Health-pack chance | 20% independently per destroyed rock |
| Valid spawn distance | 18–45M from the mech |
| Extra visible-screen margin | 2M beyond the current camera rectangle |

- The existing one-attempt-per-second, 10% success chance, and 16-rock active cap remain unchanged.
- A valid position must satisfy both the 18–45M annulus and the offscreen-plus-2M condition. If no position does, the successful attempt produces nothing.
- Enemy-selecting weapons consider a rock only when no valid enemy is inside that weapon's acquisition range. Facing, radial, trail, field, orbit, contact, and explosion attacks may damage rocks incidentally under their ordinary geometry.
- A weapon attack may damage enemies and a rock in the same event if its normal area or pierce permits. Rocks do not count toward enemy target caps or chain routing unless a branch explicitly names world objects.
- A non-piercing projectile that directly hits a rock deals damage and ends normally; piercing attacks may continue according to their ordinary rule. “Non-solid” refers to movement, not immunity to weapon collision.
- Rocks never receive enemy damage, resonance modifiers, elite or boss multipliers, control, kill effects, or ordinary-enemy defeat credit.
- Damaging or destroying a rock never builds weapon focus, momentum, mass, charge, replication, exposure, or another enemy-hit or enemy-kill counter. It may still satisfy the basic requirement for an automatic weapon to launch an attack when no enemy is available.
- A destroyed rock disappears after a brief break effect and rolls exactly once for its health pack.

At the accepted spawn and drop probabilities, destroying every generated rock would theoretically produce one pack per 50 active seconds. Actual recovery should be far lower because rocks must be found, reached, validly targeted, destroyed, and collected.

The initial natural-play target is:

- 0.75–1.25 rocks destroyed per active minute during purposeful exploration;
- four to eight health packs collected during a successful 35-minute fresh-account run;
- 100–200 total potential Hull repaired before overheal waste; and
- fewer than 10% of otherwise healthy runs experiencing a six-minute interval with no collectible pack after destroying at least eleven rocks during that interval.

These are capture targets, not hidden spawn correction. The drop roll always remains 20%.

## Control Resistance and Status Stacking

Enemy control resistance `R` is expressed as a percentage from 0% to 95%.

- Discrete knockback, launch, and other player-authored displacement becomes `authored magnitude × (1 − R)`.
- Continuous pull or push applies the same multiplier to displacement rate. Resistance never shortens the lifetime of the source field itself.
- Stun and stagger duration becomes `authored duration × (1 − R)`.
- Slow magnitude remains the authored percentage, while slow duration becomes `authored duration × (1 − R)`.
- Any positive timed control result below 0.05 s resolves as a 0.05-second minimum tell so a successful effect is visible without providing meaningful boss lockdown.
- Resistance does not reduce damage, exposure, targeting priority, projectile interception, or a weapon's own lifetime.

Driftmetal resonance applies after inherent resistance: multiply the already resisted displacement magnitude or timed-control duration by 0.80. It does not add 20 resistance points and therefore never creates complete immunity.

Elites add 25 resistance percentage points to their identity up to the accepted 90% elite cap. Bosses use their listed 85%, 90%, 92%, and 95% values.

Status combination rules:

- Multiple slows do not add. Use the strongest current magnitude and the longest current remaining duration.
- Reapplying the same slow refreshes duration to the greater of current remaining time or the newly resolved duration.
- Stun and stagger are one **hard-control** family for stacking and immunity purposes; simultaneous effects use the longest resolved duration rather than adding.
- Hard control applied while hard control is already active replaces the remaining duration only when its newly resolved duration is longer. During the post-control immunity window it deals normal damage but applies no hard control.
- After hard control ends, ordinary enemies receive 0.25 s of hard-control immunity, elites 0.75 s, and bosses 1.50 s.
- Knockback and continuous pull do not trigger hard-control immunity and remain eligible whenever their attack rules permit.
- Boss ability warnings, charges, bursts, leaps, and cooldown clocks are never canceled, delayed, or reset by player hard control. Hard control may pause ordinary pursuit movement while the boss's authored ability sequence continues.
- The current enemy catalog applies no slows, stuns, knockback, or forced movement to the player. Damage alone never creates player control loss.

## Expected Failure Margins

These are acceptance targets for standard mode rather than invisible live difficulty scaling.

### Moment-to-moment margins

- No non-relic standard damage event may remove 50 or more Hull from a zero-Armor fresh mech without a later explicit exception and unmistakable warning.
- A full fresh mech must survive any two eligible initial-catalog damage instances.
- A player who immediately corrects one early collision should normally lose 5–16 Hull, not a life.
- One late heavy collision should remove 18–46 Hull depending on source and resonance, making the next mistake urgent without ending the run alone.
- Sustained overlap is intentionally lethal: a late boss or alternating late crowd may destroy a fresh mech in approximately 1.5–3 seconds.
- A 20-second material-geode hold is never balanced around face-tanking. The player must keep moving inside the zone, clear space, leave temporarily, or invest in defense.

### Run-level margins

For players who understand movement and fabrication but have no permanent PowerUps:

| Milestone | Target probability of reaching it | Interpretation |
|---|---:|---|
| Riftjaw at 7:00 | 85–95% | The opening teaches rather than frequently ending runs |
| Brood Titan at 14:00 | 70–85% | Early routing and first build decisions begin separating runs |
| Prism Crown at 21:00 | 55–75% | An incoherent or under-mined build is now at serious risk |
| Skybreaker Apex at 28:00 | 40–65% | Mature build, exploration, and survival execution all matter |
| Mission extraction at 35:00 | 30–50% | Fresh standard mode is approachable but not an automatic first clear |

An experienced player intentionally testing a fresh account should extract in roughly 65–85% of valid runs. An experienced account with maximum PowerUps should extract in roughly 85–97%, retaining failure risk from poor routing, bad relic choices, greed, or repeated positioning mistakes.

These bands are cohort targets, not per-run manipulation. The director, drops, damage, and enemies do not inspect player skill or adjust themselves to force the percentages.

Among failed runs after the controls are understood:

- fewer than 25% of failed runs should end before Riftjaw;
- most deaths should follow either at least two damage instances within three seconds or a prolonged period without restoring accumulated damage;
- fewer than 5% should be attributable to an attack whose warning or collision boundary was hidden by effects;
- no ordinary spawn, boss re-entry, or dynamic rock placement should create unavoidable immediate damage; and
- mining deaths should read as accepted greed or failed space management, not as mining progress itself directly damaging the mech.

## Playtest Capture Requirements

Record at minimum:

- damage taken by source, phase, and resonance state;
- all hits occurring within 0.25, 0.50, 1.0, and 3.0 seconds of the prior hit;
- time spent below 50%, 30%, and 15% Hull;
- contact attempts prevented by same-enemy cooldown or global grace;
- shields consumed, revival activations, Armor prevented, and passive Recovery delivered;
- rocks seen, damaged, destroyed, recycled, and used as weapon targets;
- packs rolled, seen, collected, wasted at full Hull, and effective repair delivered;
- longest rock and health-pack droughts;
- damage taken during each mining attempt and immediately after leaving;
- death minute, source, recent damage sequence, current build, and unsecured Hyper Gold lost; and
- milestone reach and extraction rates segmented by fresh account, account power, mech, resource profile, and player experience.

Review values when any of these occur:

- a healthy fresh mech dies to one initial-catalog event or two correctly resolved events;
- early crowd contact routinely produces more than 30 damage before a visible escape response is possible;
- late overlap is harmless for more than three seconds without explicit defense;
- natural successful runs collect fewer than four or more than eight packs at the median;
- health packs are strategically irrelevant beside passive Recovery, or make Repair Swarm irrelevant;
- a control branch permanently locks an elite or boss;
- a fast enemy plus movement modifiers creates unavoidable contact from outside the readable view; or
- failure rates miss a milestone band by more than ten percentage points across a meaningful cohort.

## Adjustment Order

When survival feels unfair, inspect in this order:

1. attack warning, effect occlusion, and displayed collision boundary;
2. invalid spawn, re-entry, navigation, or overlap state;
3. contact cooldown and grace resolution;
4. player and enemy footprint scale;
5. movement-speed relationship;
6. attack damage;
7. health-pack access and repair value; and
8. permanent or run-build defenses.

When survival feels trivial, reverse the diagnosis but do not immediately inflate every enemy's damage. Horde geometry, pursuit speed, resource-route pressure, boss overlap, and mining commitments can raise danger while preserving the two-hit readability guarantee.

## Related Decisions

- [DEC-097: Inherit direct movement, collision, and camera](decisions/DEC-097-inherit-direct-movement-collision-and-camera.md)
- [DEC-103: Use Hull Integrity and contact-collected field pickups](decisions/DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md)
- [DEC-107: Use fixed ordinary enemy stat profiles](decisions/DEC-107-use-fixed-ordinary-enemy-stat-profiles.md)
- [DEC-112: Bound permanent power below run-build power](decisions/DEC-112-bound-permanent-power-below-run-build-power.md)
- [DEC-119: Accept the initial alien encounter baseline](decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [DEC-123: Replenish destructible rocks around the player](decisions/DEC-123-replenish-destructible-rocks-around-the-player.md)
- [DEC-126: Adopt the initial player survivability baseline](decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
