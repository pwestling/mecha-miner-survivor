---
doc_id: DEC-126
title: Adopt the Initial Player Survivability Baseline
status: accepted
authoritative: false
validation: analytic-model-and-playtest
---

# DEC-126 — Adopt the Initial Player Survivability Baseline

## Decision

Adopt the [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md) as the authoritative first-playable specification for movement scale, collision footprints, incoming damage resolution, contact grace, health-pack repair, destructible-rock durability, control resistance, and expected standard-mode failure margins.

The shared fresh-mech baseline is:

- 100 maximum Hull, zero Armor, zero Recovery, and no revival;
- 3.0M/s movement with a 1.0M circular collision footprint;
- 0.75 seconds between contact attempts from the same enemy;
- 0.20 seconds of global contact-only grace after a resolved contact;
- no universal post-hit invulnerability;
- 25 Hull restored by one health pack; and
- 100-Hull, non-solid destructible rocks with a 0.80M damage footprint.

Damage modifiers apply before upward whole-Hull rounding, Armor then subtracts to a minimum of one, and explicit hit negation resolves before Hull loss. A negated contact still starts its attacker cooldown and global contact grace.

The strongest initial standard damage instance is 46 Hull: Asterite-resonant Skybreaker contact. A full fresh mech must therefore survive any two eligible initial-catalog damage events, while a third maximum hit may kill it. Sustained late overlap remains intentionally lethal in approximately 1.5–3 seconds.

Adopt exact linear control-resistance scaling, multiplicative Driftmetal field reduction, non-stacking slows, one hard-control family, and 0.25 / 0.75 / 1.50-second post-control immunity for ordinary enemies, elites, and bosses respectively.

Adopt the listed milestone reach and extraction-rate bands as cohort-level validation targets. They never drive live adaptive difficulty.

Finally, revise Riftjaw's charge speed from twice its 42%-of-player pursuit speed to 180% of base mech speed. Its prior 84%-of-player charge could be outrun directly and did not fulfill the visible charge-lane threat.

## Status

Accepted as the initial implementation and playtest baseline. Values remain tunable through measured playtests, but they are no longer missing design inputs.

## Rationale

The existing documents already fixed 100 Hull, zero Armor and Recovery, enemy damage, a 0.75-second same-enemy contact interval, and 0.20-second contact grace. They did not establish the world-speed scale, contact footprints, damage rounding and shielding order, health-pack value, rock durability, control stacking, or a definition of fair failure.

The adopted values preserve a simple survivor-like survival model: movement prevents most damage; ordinary contact accumulates mistakes; telegraphed late attacks are severe but not single-hit deaths; exploration offers intermittent recovery; and explicit build choices improve durability. The two-hit guarantee provides a clear fairness invariant without making crowd overlap safe.

Twenty-five-Hull packs are large enough to matter after ordinary contact while remaining smaller than the worst late hit. At four to eight collected packs per successful run, active exploration can repair roughly one to two base health bars without guaranteeing recovery or replacing Repair Swarm.

Control resistance uses direct visible arithmetic and lockout windows rather than hidden per-enemy special cases. Boss warnings and attacks continue through control so automatic stagger cannot cancel their defining mechanic.

## Consequences

- `1M` now has a world-speed relationship: the base mech moves three diameters per second.
- Enemy percentage speeds, projectile speeds, map base-travel distances, and collision scales can be converted into concrete world units.
- Every existing contact value has a calculable hits-to-defeat and uninterrupted-overlap time.
- No initial standard attack may reach 50 damage against a zero-Armor fresh mech without a later explicit exception.
- Dynamic rocks remain non-solid support objects and cannot randomly obstruct movement routes.
- Target-selecting weapons attack rocks only when no enemy is in their acquisition range; geometric weapons can break them incidentally.
- Health-pack amount, rock durability, pickup radius, and spawn annulus are no longer open tuning inputs.
- OQ-016 remains resolved with a complete numerical healing route.
- Exact camera scale, audiovisual treatment, and alternate difficulty modes remain separate open work.

## Specification links

- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md)
- [Survivability baseline data](../data/survivability-baseline.csv)
- [Contact damage-pressure data](../data/contact-damage-pressure.csv)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Initial Alien and Boss Roster](../31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](../32-standard-wave-and-beacon-schedule.md)
- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md)
- [Utility Catalog](../68-utility-catalog.md)
- [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md)

## Supersedes / superseded by

This supplies numerical values left open by DEC-097, DEC-103, DEC-107, DEC-119, DEC-122, and DEC-123. It preserves their direct movement, non-solid enemies, fixed enemy profiles, 100-Hull baseline, dynamic 16-rock cap, 10%-per-second replenishment chance, and 20% pack-drop chance.

It supersedes only Riftjaw's provisional twice-pursuit-speed charge value. It does not change Riftjaw's warning, duration, direction lock, damage, collision rule, or ordinary pursuit speed.
