---
doc_id: GDD-INITIAL-MECH-CATALOG
title: Initial Mech Catalog
status: active
authoritative: true
---

# Initial Mech Catalog

## Catalog status

This document defines the accepted six initial mech identities, silhouettes, signature pairings, traits, statistics, fresh-profile availability, and selection behavior. [DEC-117](./decisions/DEC-117-accept-initial-mech-catalog.md) accepts the gameplay catalog. Names remain presentation-working names and may receive a later presentation-only revision without reopening the chassis mechanics.

## Roster principles

- Every mech begins with exactly one accepted signature weapon and one concise inherent trait.
- Traits are simple permanent modifiers rather than triggered minigames, material dependencies, or signature-exclusive mechanics.
- Every trait remains useful on every valid resource profile. A trait may naturally complement its signature, but applies to all qualifying systems acquired later.
- Initial mechs have no numerical drawbacks. Their differences come from one positive specialization applied to the shared baseline.
- Trait strength is roughly comparable to one or two ranks of a run-local utility. It is meaningful at deployment but remains much smaller than a mature run build.
- Traits consume no weapon, utility, or relic slot; have no upgrade ranks; and never change during the run.
- Every mech uses the shared direct-movement, collision, camera, four-weapon, three-utility, one-relic, 100-Hull, zero-Armor, and zero-Recovery rules except for the one displayed trait.
- Skins and color variants are cosmetic. A visual variant never changes the signature, trait, statistics, collision, or unlock state.

## Shared comparison baseline

The selection interface compares each mech with this pre-PowerUp baseline:

| Statistic | Shared baseline |
| --- | ---: |
| Maximum Hull Integrity | 100 |
| Armor | 0 |
| Recovery | 0 Hull/s |
| Movement speed | 3.0M/s (100%) |
| Collision diameter | 1.0M circle |
| Mining extraction rate | 100% |
| Weapon damage | 100% |
| Weapon attack rate | 100% |
| Weapon area | 100% |

Account PowerUps modify the account-wide starting baseline first. The selected mech's inherent modifier then applies and the selection screen shows the resulting values. Percentage modifiers with the same named utility or PowerUp statistic add under the shared modifier rules.

## Catalog overview

| ID | Mech | Signature | Inherent trait | Selection role |
| --- | --- | --- | --- | --- |
| `MCH-01` | Kestrel | Pulse Repeater | Accelerated Feed: +15% weapon attack rate | Approachable rapid-fire skirmisher |
| `MCH-02` | Pike | Rail Lance | Heavy Calibration: +15% weapon damage | Deliberate high-impact line striker |
| `MCH-03` | Prospector | Missile Rack | Industrial Extractors: +15% mining extraction rate | Resource-routing expedition frame |
| `MCH-04` | Lodestar | Gravity Projector | Field Geometry: +15% weapon area | Crowd-shaping field controller |
| `MCH-05` | Bastion | Reactor Pulse | Reinforced Chassis: +25 maximum Hull Integrity | Durable close-pressure platform |
| `MCH-06` | Razorback | Ram Field | Overdrive Treads: +10% movement speed | Aggressive movement-driven breaker |

## MCH-01 — Kestrel

### Player-facing identity

Kestrel is the recommended first deployment. Its rapid nearest-target fire requires little facing expertise, while its trait makes every later automatic weapon activate more frequently where applicable.

### Signature and trait

- **Signature weapon:** Pulse Repeater.
- **Trait — Accelerated Feed:** `+15% weapon attack rate`.
- Attack rate means 1.15 times the ordinary activation frequency, not 15% cooldown subtraction.
- It uses the same affected-timing boundaries as Cycle Capacitor: primary weapon activations accelerate, while projectile travel, damage ticks, delayed echoes, arming delays, and non-weapon timers do not unless a weapon specification explicitly maps them to attack rate.

### Top-down silhouette

- Compact central chassis with two swept lateral weapon housings.
- Broad rear taper and short forward nose create a clear movement-facing axis.
- Low, agile proportions distinguish it from Bastion's square mass and Razorback's heavy wedge.
- Pulse Repeater emitters flash alternately from the two side housings without changing the weapon's mechanical firing sequence.

### Selection summary

> Rapid automatic targeting and faster weapon cycles make Kestrel the most immediately approachable chassis.

## MCH-02 — Pike

### Player-facing identity

Pike is a long-axis striker built around deliberate facing. Its slower Rail Lance rewards lining up crowds, while its general damage bonus ensures every later weapon benefits even if the run moves away from piercing attacks.

### Signature and trait

- **Signature weapon:** Rail Lance.
- **Trait — Heavy Calibration:** `+15% damage` to all equipped weapons.
- Uses the same weapon-attribution boundary as Harmonic Calibrator. It affects weapon-created mines, drones, turrets, delayed attacks, and persistent zones, but not environmental damage or non-weapon temporary effects.
- It does not alter Rail Lance cadence, projectile speed, width, range, or penetration allowance.

### Top-down silhouette

- Long narrow body with an unmistakable forward lance boom.
- Two small rear stabilizer fins counterbalance the extended nose.
- Minimal side bulk keeps the silhouette visually linear even when the weapon is not firing.
- The chassis rotates continuously with persistent facing so the player can read its firing line before the next lance.

### Selection summary

> A deliberate facing-based striker whose calibrated weapons hit harder across every build.

## MCH-03 — Prospector

### Player-facing identity

Prospector is an expedition and mining chassis protected by an automatic missile system. Its faster extraction shortens every ordinary-resource and Hyper Gold commitment without determining which weapons or branches the player should pursue.

### Signature and trait

- **Signature weapon:** Missile Rack.
- **Trait — Industrial Extractors:** `+15% mining extraction rate`.
- Applies to standard seams, rich seams, material geodes, and Hyper Gold sites.
- Mining decay remains four times Prospector's current forward extraction rate, preserving the normal leave-and-decay ratio.
- A nominal 15-second seam takes about 13.0 seconds, a 20-second geode about 17.4 seconds, and a 45-second Hyper Gold site about 39.1 seconds before interruption.
- Does not change payouts, beacon thresholds, resonance fields, grace, or completed checkpoints.

### Top-down silhouette

- Asymmetric industrial chassis with a visible sensor mast or survey dish on one shoulder and a block missile rack on the other.
- Rear equipment deck and external extraction arms create a work-platform profile rather than a military wedge.
- Missile doors and mining apparatus use separate silhouettes and animations so weapon readiness cannot be confused with active extraction.

### Selection summary

> A resource-focused expedition frame that completes every mining hold somewhat faster.

## MCH-04 — Lodestar

### Player-facing identity

Lodestar controls broad spaces. Its Gravity Projector immediately demonstrates field placement and grouping, while its trait expands every later weapon dimension explicitly classified as Area.

### Signature and trait

- **Signature weapon:** Gravity Projector.
- **Trait — Field Geometry:** `+15% weapon area`.
- Uses the same Area mappings and exclusions as Field Expander: scalable radii, widths, blast areas, projectile bodies, cones, and persistent damage zones qualify.
- It does not alter targeting range, placement range, travel distance, orbit radius, mining zones, pickup radius, or resonance fields unless an accepted weapon rule classifies that exact dimension as Area.
- Gravity Projector's field radius receives the bonus in every branch.

### Top-down silhouette

- Circular central body surrounded by four evenly spaced field vanes or emitter arms.
- Radial negative spaces remain visible when the model is small or surrounded by enemies.
- A luminous center identifies the gravity source without relying on its resource colors.
- The silhouette is orientation-neutral enough to suit automatic field placement while a small forward marker still communicates persistent facing.

### Selection summary

> A field-control platform whose weapons cover larger spaces.

## MCH-05 — Bastion

### Player-facing identity

Bastion is the durable close-pressure chassis. Reactor Pulse damages enemies around the mech, and extra Hull Integrity gives the player more room to survive positioning errors without providing Armor or passive Recovery.

### Signature and trait

- **Signature weapon:** Reactor Pulse.
- **Trait — Reinforced Chassis:** `+25 maximum Hull Integrity`.
- Bastion deploys at full current Hull Integrity, normally `125 / 125` before account PowerUps.
- The flat mech bonus applies after the shared account baseline. The selection screen displays the final current and maximum values.
- The trait does not reduce individual hits, repair automatically, improve health packs, or change contact-damage cadence.

### Top-down silhouette

- Broad square torso, heavy shoulder blocks, and a large visible circular reactor core.
- Short thick limbs keep its footprint visually dense without changing the shared collision rules.
- Four corner armor masses distinguish it from Lodestar's open radial vanes.
- Reactor Pulse originates from the central core and expands beyond the chassis outline.

### Selection summary

> A forgiving close-pressure platform with substantially more Hull Integrity.

## MCH-06 — Razorback

### Player-facing identity

Razorback is an aggressive movement chassis. Its wedge-like Ram Field turns forward travel into damage and space, while increased movement speed remains useful for exploration, mining defense, and every later weapon build.

### Signature and trait

- **Signature weapon:** Ram Field.
- **Trait — Overdrive Treads:** `+10% movement speed`.
- Applies equally in every direction without adding acceleration, momentum, drift, reverse penalties, or a turn radius.
- It does not change enemy movement, weapon projectile speed, Ram Field's activation threshold, or mining extraction rate.
- Combines with Vector Thrusters and other explicit movement modifiers under the shared additive percentage rule.

### Top-down silhouette

- Wide triangular prow with two forward ramming prongs and a narrow rear body.
- Strong front-heavy wedge makes facing readable even without a separate arrow.
- Rear track or thruster housings flare outward behind the collision footprint.
- Ram Field projects just ahead of the prow so the weapon boundary does not disappear beneath the model.

### Selection summary

> A fast wedge-shaped breaker that rewards committed movement and aggressive routing.

## Trait comparison and stacking

| Mech | Changed baseline | With matching utility at Rank 3, before PowerUps |
| --- | ---: | ---: |
| Kestrel | 115% attack rate | 135% with Cycle Capacitor |
| Pike | 115% weapon damage | 135% with Harmonic Calibrator |
| Prospector | 115% extraction rate | 135% with Extraction Accelerator |
| Lodestar | 115% weapon area | 135% with Field Expander |
| Bastion | 125 maximum Hull | 170 with Reinforced Bulkhead |
| Razorback | 110% movement speed | 125% with Vector Thrusters |

These combinations are intentional specializations, not required pairings. The matching utility may be absent from the profile, consume a scarce material needed elsewhere, or lose to another utility under the three-slot limit. Every trait remains useful without it.

## Initial availability

All six initial mechs are selectable on a fresh profile. Kestrel is highlighted as the recommended first deployment, but the player may inspect and choose any chassis before the first run.

| Mech | Initial availability |
| --- | --- |
| Kestrel | Available; recommended default |
| Pike | Available |
| Prospector | Available |
| Lodestar | Available |
| Bastion | Available |
| Razorback | Available |

- Initial availability supports profile and signature testing immediately and avoids forcing repeated Kestrel attempts before the player has ever banked Hyper Gold.
- Later roster additions may use banked Hyper Gold, extraction-secured challenges, or both. No failed or abandoned run permanently unlocks a mech.
- The permanent progression catalog still uses Hyper Gold for PowerUps and other content or option unlocks; initial roster availability does not remove that progression layer.
- Any later locked mech shows its silhouette, signature, trait, and exact unlock requirement rather than a hidden question mark unless secrecy is the explicit reward.

## Selection behavior

- Kestrel is highlighted and initially focused for the first deployment, with confirmation still required before entering the map.
- The selection screen offers **Random Mech** among all currently available mechs. The result is shown before deployment confirmation and does not reveal geology.
- The interface remembers the most recently selected available mech between runs.
- Any future locked entries show their silhouette, signature weapon, trait, and exact unlock condition rather than hidden question marks.
- The player can inspect every unlocked or locked mech before confirming, using keyboard/mouse or gamepad alone.
- Selection compares changed statistics numerically and explains weapon-support mappings such as Attack Rate and Area.

## Presentation and asset constraints

- Every mech must remain identifiable as a solid-color silhouette at normal Steam Deck gameplay zoom.
- The six primary silhouettes are linear, swept, radial, square, industrial-asymmetric, and wedge-shaped respectively; paint alone cannot carry identity.
- Shared locomotion rigs and animations are allowed when chassis proportions and attachments remain distinct.
- Free models may be recolored, rescaled, and kitbashed under the accepted common material and palette treatment. Asset scarcity cannot collapse two mechs into the same top-down outline.
- Signature muzzle points and persistent-facing cues must align with the model so automatic attacks do not appear to originate from arbitrary space.
- Selection renders may use a more revealing angle than gameplay, but they cannot conceal the fully top-down silhouette the player actually controls.

## Balance and validation

- Test every mech across all twelve signature-valid resource profiles, not only profiles containing all three signature branch materials.
- Compare fresh, partial, and highly upgraded accounts so trait value is not accidentally erased or magnified by PowerUps.
- No mech should be a universal best selection after accounting for signature behavior, trait, and profile uncertainty.
- Kestrel must provide the easiest first-run targeting experience without being the strongest long-term choice by default.
- Bastion's flat Hull bonus must not make early contact harmless; Razorback's speed must not make all mining pressure optional.
- Pike, Kestrel, Prospector, and Lodestar stacking with their matching utilities must remain strong but below the power swing of a coherent three-utility and four-weapon build.
- First-run testing should confirm that highlighting Kestrel guides new players without making the other five choices feel forbidden or inexplicable.

## Related documents

- [Playable Mechs and Starting Loadouts](./35-playable-mechs.md)
- [Weapon Specification Index](./weapons/README.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#hangar-and-between-run-flow)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [DEC-039 — Target a six-mech initial roster](./decisions/DEC-039-six-mech-initial-roster.md)
- [DEC-043 — Assign the fifteen base weapons to the resource graph](./decisions/DEC-043-fifteen-weapon-graph-assignment.md)
- [DEC-117 — Accept the initial mech catalog](./decisions/DEC-117-accept-initial-mech-catalog.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
