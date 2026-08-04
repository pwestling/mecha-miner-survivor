---
doc_id: GDD-COMBAT-ECONOMY-BALANCE-FRAMEWORK
title: Combat and Economy Balance Framework
status: active
authoritative: true
---

# Combat and Economy Balance Framework

## Purpose

This document supplies one shared method for choosing and revising weapon, branch, enemy, boss, upgrade, and resource-economy values. [DEC-124](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md) accepts it as the initial tuning framework.

The framework creates comparable estimates rather than pretending that design can be solved by one formula. Single-target damage per second is the primary numerical anchor, but horde throughput, target coverage, reliability, setup, control, and positional risk remain separately visible. A weapon is balanced as a complete play pattern across the standard run, not by forcing every attack to produce the same dummy DPS.

All values are initial targets for prototyping. Measured play may revise the numbers while preserving the definitions, benchmark scenes, and adjustment order.

## Core principles

1. **Start with a fresh-account neutral baseline.** Base-weapon comparisons exclude mech traits, PowerUps, utilities, relics, branches, geode resonance, and temporary encounter effects unless the benchmark explicitly tests one.
2. **Measure realized results, not only theoretical output.** Travel time, misses, retargeting, overkill, facing, movement, setup, downtime, range, and target availability all affect what the player receives.
3. **Keep damage and safety legible as separate value.** Pull, knockback, slow, stun, projectile interception, and safe attack geometry are reported beside damage rather than silently converted through an arbitrary universal exchange rate.
4. **Balance weapons across a portfolio of situations.** An equal-tier weapon may lead one benchmark and trail another. It is unacceptable only when it is broadly dominated, has no meaningful favorable situation, or makes its normal recipe profile predictably undesirable.
5. **Treat geometry as conditional output.** Radius, width, range, duration, capacity, and movement patterns increase damage only when they create additional valid hits or uptime in an actual benchmark.
6. **Preserve authored progression advantage.** PowerUps, traits, utilities, branches, and relics genuinely increase capability. The director never secretly scales to cancel them.
7. **Use target ranges, not exact-point theater.** A value inside the target band can still be wrong if the weapon feels unclear, unreliable, or oppressive. A value slightly outside can be right when its distinct play pattern explains the difference.
8. **Tune the player and director together only in an explicit order.** Do not repeatedly raise weapon damage and enemy Hull to cancel one another without improving play.

## Shared reference state

Unless a benchmark says otherwise, use:

- the shared fresh-account mech baseline with 100 maximum Hull, zero Armor, zero Recovery, 3.0M/s movement, a 1.0M circular footprint, and no inherent trait;
- no PowerUps, utilities, relic, or specialized weapon branch;
- the weapon's base form with zero ore ranks;
- a ready weapon with all ordinary actors, charges, mines, pods, drones, or persistent components in their normal deployment state for the named window;
- active simulation time only; pauses contribute neither output nor elapsed time;
- unmodified enemies with the accepted fixed Hull, footprint, speed, and control resistance;
- no geode resonance field or Hyper Gold beacon response unless named; and
- normal automatic targeting, movement-derived facing, projectile collision, and terrain rules.

Signature-mech traits, account states, utilities, relics, and branches are tested as additional layers after the neutral base catalog is internally coherent.

## Damage metrics

### Ideal single-target DPS

Ideal single-target DPS estimates the sustained damage rate against one stationary, unarmored, boss-sized target held in the weapon's intended legal position with no other targets competing for selection.

For a simple discrete weapon:

`ideal DPS = damage per hit × damaging hits per activation × activations per second`

For persistent, continuous, autonomous, or movement-authored weapons, total damage dealt during the defined measurement window is divided by active-simulation seconds. Damage-over-time ticks that occur after the window count only when they land within it.

Ideal DPS exposes arithmetic mistakes and raw budget. It is not an expected run result.

### Opening burst and sustained DPS

Every weapon reports two single-target windows:

- **Burst-10:** total damage during the first 10 seconds from a normal ready state, divided by 10.
- **Sustained-30:** total damage during seconds 11–40 of uninterrupted operation, divided by 30.

The comparison reveals charge time, deployment buildup, actor capacity, delayed impacts, focus growth, mine setup, and other warmup behavior. Neither value replaces the other.

### Realized boss DPS

Realized boss DPS is weapon-attributed damage divided by boss lifetime during an actual standard-phase boss benchmark. It includes ordinary horde distraction, movement, misses, range loss, overkill, setup, and defensive repositioning.

The ratio `realized boss DPS / ideal DPS` is the weapon's **boss reliability** for that scene. A low ratio may be an intentional tradeoff, but it must correspond to meaningful horde, control, safety, or positional value.

### Horde damage throughput

Horde throughput is total weapon damage across all ordinary enemies divided by active-simulation time in a fixed crowd scene. It counts actual hits rather than multiplying single-target DPS by a theoretical unlimited target count.

Report alongside it:

- ordinary enemies defeated per second;
- unique enemies damaged per second;
- percentage of damage lost to overkill;
- percentage of the scene during which at least one valid target is being affected; and
- the active enemy count at the end of the window.

Horde throughput is allowed to greatly exceed single-target DPS for piercing, radial, area, chaining, trail, and persistent-zone weapons. The finite benchmark population prevents “hits everything” from becoming an infinite paper value.

## Non-damage metrics

Damage reports never hide the following:

| Metric | Meaning |
| --- | --- |
| Targeting uptime | Share of active time in which the weapon has a valid target or valid attack geometry |
| Hit reliability | Share of launched or scheduled damaging events that hit at least one intended target |
| Coverage | Unique ordinary enemies affected per second and spatial share of the benchmark horde reached |
| Setup time | Time from a normal ready state to the weapon's stable output pattern |
| Relocation recovery | Time required to regain stable output after the mech moves one camera width |
| Burst interval | Longest ordinary gap between meaningful damaging events |
| Control | Displacement, slow, stun, clustering, interception, or denial delivered per second, with resistance shown |
| Positional burden | Whether optimal output requires facing, continuous movement, remaining still, close range, route shaping, or luring |
| Safety contribution | Measured reduction in contact or projectile threats during matched scene comparisons |

Control and safety may justify lower damage, but the benefit must appear in matched playtests. A weapon does not receive a large damage discount merely because its description contains a control verb.

## Canonical weapon benchmark scenes

Every base weapon, branch, and meaningful stat-rank package is evaluated in the same scenes.

### WB-01 — Duel dummy

- One stationary boss-sized unarmored target.
- Target remains at the weapon's intended optimal legal range or geometry.
- Report Burst-10, Sustained-30, setup time, and ideal DPS.
- Purpose: arithmetic, warmup, and focused-output anchor.

### WB-02 — Advancing stream

- Twenty Rippers advance in a broad stream from one side with ordinary spacing.
- The mech may move and face normally inside an open area for 20 seconds.
- Report throughput, defeats, unique hits, overkill, hit reliability, and contact damage received.
- Purpose: piercing, facing, cone, retargeting, and line-control comparison.

### WB-03 — Dense mixed horde

- Fifty enemies use 50% Ripper, 30% Skitterling, and 20% Shellback composition around the mech.
- Replenish defeated enemies to preserve the composition for 30 seconds.
- Report horde throughput, defeats per second, active-count trend, coverage, and control.
- Purpose: area, chain, orbit, radial, autonomous, and persistent-zone comparison.

### WB-04 — Geode hold

- The mech completes one 20-second geode while the relevant standard-phase composition approaches.
- Movement must remain inside the normal extraction zone; leaving invalidates that trial.
- Report damage, defeats, contact damage received, minimum clear route, and extraction completion.
- Purpose: the game's defining constrained-position test.

### WB-05 — Relocation

- Begin at stable output, then travel one camera width through an ordinary horde and resume fighting.
- Report output during travel and seconds required to regain 90% of pre-move throughput.
- Purpose: compare pods, mines, wakes, stationary conversions, drones, range, and movement-authored attacks.

### WB-06 — Boss with live horde

- Use the boss's real arrival-minute ordinary composition and population.
- Run the complete fight with normal movement and no invulnerability.
- Report realized boss DPS, horde throughput, boss time to kill, damage received, targeting share, and any boss overlap.
- Purpose: validate that paper boss damage survives the actual standard encounter.

## Equal-tier base-weapon bands

The 15 base weapons share one intended tier, but they occupy different output shapes.

| Base archetype | Ideal single-target DPS | Favorable base horde throughput | Expected compensation |
| --- | ---: | ---: | --- |
| Focused direct damage | 36–45 | 40–90 damage/s | High boss reliability or priority targeting |
| Generalist | 28–38 | 70–130 damage/s | Useful in most scenes without leading all of them |
| Area or control specialist | 16–30 | 110–210 damage/s | Coverage, control, or constrained-position safety |
| Setup or movement specialist | 20–36 | 90–190 damage/s when used correctly | Higher favorable output offset by setup or positional burden |

These are zero-rank fresh-account starting bands, not hard caps. A weapon outside its nominal band needs a visible reason and must still satisfy the opening-survival and portfolio tests.

The complete catalog should average approximately **32 ideal single-target DPS per base weapon**. Direct weapons may exceed that average; radial and crowd-control signatures may fall below it. No zero-rank weapon should combine top-band single-target DPS, top-band horde throughput, strong control, and high reliability.

## Opening and ordinary-enemy anchors

- Every signature weapon must make minute zero stable without special resources, account progression, or a favorable relic.
- Against a Skitterling's 20 Hull, the base signature's ordinary successful attack should kill immediately or complete the kill within one normal follow-up event.
- In the minute-zero schedule, the signature alone must sustainably defeat at least the 1.33 Skitterlings per second supplied by the normal two-enemy/1.5-second replenishment pulse when the player moves reasonably.
- A focused base weapon should defeat a 45-Hull Ripper in roughly 1–2 seconds under favorable geometry. Area specialists may take longer against one Ripper but must clear several together.
- A base attack need not meaningfully damage every Shellback immediately. The 150-Hull enemy intentionally reveals the difference between coverage and concentrated damage.

## Build milestone boss-DPS budgets

The accepted boss Hull and time-to-kill targets imply the following **realized whole-build boss DPS** bands:

| Arrival | Boss | Hull | Target time to kill | Required realized build DPS |
| ---: | --- | ---: | ---: | ---: |
| 7:00 | Riftjaw | 6,000 | 45–75s | 80–133 |
| 14:00 | Brood Titan | 14,000 | 60–90s | 156–233 |
| 21:00 | Prism Crown | 30,000 | 75–105s | 286–400 |
| 28:00 | Skybreaker Apex | 45,000 | 90–120s | 375–500 |

These values are damage actually dealt to the boss while surviving its live arrival-minute horde. Isolated ideal DPS may need to be 20–35% higher depending on targeting competition and positional burden.

The targets assume the healthy-build states already established by the standard schedule:

- by 7:00, several signature ranks or one additional weapon;
- by 14:00, at least two weapons with meaningful stat, utility, or branch investment;
- by 21:00, three or four weapons and at least one branch;
- by 28:00, four weapons and a functionally mature mixture of ranks and branches.

The signature alone may defeat Riftjaw more slowly than the target without making the run impossible. Missing later bands should create dangerous boss overlap rather than an arbitrary enrage or despawn.

### Milestone feasibility check

Before accepting any boss Hull and weapon-value combination, divide the required realized build DPS by the ordinary healthy-build weapon count:

| Arrival | Reference equipped weapons | Required realized DPS per equipped weapon | Multiple of the 32-DPS base-catalog average |
| ---: | ---: | ---: | ---: |
| 7:00 | 2 | 40–67 | 1.25–2.08× |
| 14:00 | 3 | 52–78 | 1.63–2.44× |
| 21:00 | 4 | 71–100 | 2.23–3.13× |
| 28:00 | 4 | 94–125 | 2.93–3.91× |

This comparison deliberately ignores weapon specialization and is not itself a balance target. It exposes how much growth the combined ranks, branches, traits, utilities, PowerUps, and ordinary weapon interactions must supply.

The lower end of every boss band must be reachable by a coherent **fresh-account, no-relic reference build** using plausible ore and specialized-material collection by that minute. Relics are random and cannot be required; maximum PowerUps cannot define fresh-account viability. The upper end may describe a less focused build or one losing more output to survival.

The complete numeric pass in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md) found the earlier 24,000 / 75,000 / 220,000 late-boss Hull values incompatible with a legal fresh-account build. DEC-125 therefore reduced the sequence to 14,000 / 30,000 / 45,000 while retaining the fight-duration targets. Its Kestrel reference progression produces approximately 81, 164, 328, and 391 realized DPS at the four milestones and lands inside the revised feasibility bands with plausible ore and specialized-material spending.

## Horde-clear milestone targets

Whole-build ordinary-enemy defeat rates provide a second phase anchor:

| Phase check | Approximate healthy-build defeats per second | Intended screen result |
| --- | ---: | --- |
| Minute 0 | 1.5–2.5 | Minor enemies remain readable and rarely threaten survey reading |
| Minute 6 | 2–4 | Mining space can be opened but not permanently emptied |
| Minute 13 | 4–7 | Mixed coverage begins to matter before Brood Titan |
| Minute 20 | 6–10 | Fast and durable bodies require a coherent multiweapon build |
| Minute 27 | 10–16 | A mature build clears routes through late mixed pressure |
| Minute 34 | 14–22 | Large masses die rapidly while the screen remains saturated |

These are measured against the authored composition at that minute. They are not spawn-rate commands and do not require every build to land in the center. Boss-specialized builds may trail them; horde-specialized builds may exceed them while remaining within boss-DPS bands.

## Common-ore stat-rank value

Every weapon stat rank adds one fixed linear amount while the shared nonlinear ore price rises with total weapon depth. The fixed increment should ordinarily produce:

- about **8–12% improvement over the zero-rank base value** of the named stat per rank;
- about **6–12% realized improvement** in at least one benchmark that clearly uses that stat;
- less percentage improvement relative to the weapon's current value at deeper ranks, because the same flat addition is applied to a larger total; and
- no more than roughly 15% realized improvement across multiple broad benchmarks from one ordinary rank.

This is a value band, not a rule that every stat reads “+10%.” Linear dimensions can change affected area nonlinearly; range may improve uptime rather than raw damage; duration may increase actor overlap; capacity uses discrete whole units; and control stats face resistance. Their fixed increments are selected through benchmark results while remaining truthful in the player-facing stat display.

If one stat rank produces less than 4% realized improvement even in its favorable benchmark, the increment or weapon interaction is too weak. If a stat repeatedly improves boss damage, horde throughput, control, and safety more than the weapon's Damage track, it is likely the automatic purchase and must be reduced or redefined.

The accepted shared price sequence remains `10, 30, 60, 100, 150, 210...` ore by total ranks purchased on that weapon. Constant gains plus rising prices intentionally create declining marginal value per ore without imposing a hard rank cap.

## Branch value and follow-on rule

A two-unit specialized-resource branch is a larger commitment than one ordinary rank. On the initial baseline:

- a well-used branch should create roughly **35–70% improvement** in its favorable combination of damage, reliability, coverage, control, or safety;
- it should feel comparable to approximately four to seven useful early common-ore ranks, while remaining qualitatively more noticeable;
- amplification may deliver the most consistent value because it preserves the base pattern;
- functional variants may exchange some raw output for control, priority, reliability, or area denial; and
- playstyle conversions may reach the highest favorable output only when the player adopts their new facing, movement, range, timing, or setup demand.

Every branch is a net improvement when used according to its disclosed play pattern. A conversion may perform worse than the base weapon when deliberately misplayed, but it cannot be a disguised downgrade under its intended conditions.

The initial weapon catalog has **no follow-on branch upgrades**. One irreversible two-unit branch choice is the complete specialized-material transformation for that weapon. Further branch ranks would add economy and menu complexity before the base 45 choices are validated and require a later explicit decision.

## Stacking and comparison order

Balance calculations use the accepted named-stat model:

1. Start from the weapon's base value.
2. Add the weapon's fixed ore-rank gains.
3. Apply additive percentage modifiers to the same named stat from mech traits, PowerUps, and utilities.
4. Apply branch-specific transformations and explicit multipliers.
5. Apply relic replacements or multipliers in the order defined by that relic.
6. Measure the resulting behavior rather than assuming the arithmetic multiplier equals realized output.

Base-catalog tuning uses the fresh neutral reference first. Then test every signature with its inherent mech trait, fresh and maximum PowerUps, relevant utility combinations, all three branches, and all ten relics. A strong combination is allowed; one combination that trivializes every benchmark or one weapon that becomes unusable under many broad relics requires review.

## Economy and power-curve checks

- Compare ore earned and spent by each boss threshold, not only whole-map totals.
- For every boss threshold, publish one legal fresh-account no-relic reference build, its exact ore and specialized-material cost, and its measured realized DPS. A boss target without such a build is unsupported.
- Record how many ranks are bought on each weapon and which stat receives them. A consistently ignored stat fails even if the weapon's total output is acceptable.
- Record branch timing, unused specialized units, and whether informed players delay a branch for ore ranks or always buy it immediately.
- A player following a reasonable route should be able to meet boss readiness without mining every available point or receiving perfect relic and boss-material outcomes.
- The 300-ore radar, 300-ore fully ranked utility, weapon ranks, and fixed 150-ore relic sales all compete in one common-ore economy and must be evaluated together.
- Permanent PowerUps may make readiness more consistent but are not required to reach the fresh-account lower bound.

## Adjustment order

When a weapon or phase misses its targets, change the narrowest responsible value first.

### Weapon adjustment order

1. Broken targeting, collision, or stat attribution.
2. Reliability: projectile speed, valid targeting, range, setup, or cadence gaps.
3. Base damage or attack cadence.
4. Area, width, duration, capacity, or target count.
5. Control strength and resistance interaction.
6. Ore-rank increment.
7. Branch multiplier or special rule.
8. Weapon concept only if numeric repair cannot produce a distinct useful role.

### Encounter adjustment order

Use the accepted director order: population and replenishment, event geometry, composition, boss Hull or cadence, then ordinary fixed enemy profiles. Do not use hidden adaptive scaling or raise all enemy Hull merely because one weapon overperforms.

### Economy adjustment order

1. Verify actual collection and unspent-resource data.
2. Adjust one reward or price with a clearly identified timing problem.
3. Recheck weapon-rank, utility, radar, and branch competition.
4. Change mapwide ore supply only when the problem appears across many builds and routes.

## Required tuning record

Each weapon's numeric specification records:

- base damage and every fixed timing, count, range, area, duration, capacity, control, and delivery value;
- the fixed per-rank increment for all three stats;
- Burst-10, Sustained-30, boss reliability, and favorable horde throughput estimates;
- the branch's key numeric changes and favorable benchmark delta;
- which assumptions are analytic estimates and which are measured playtest results; and
- the game-data revision or document decision that changed a value.

Markdown remains the authoritative explanation. The [base weapon](./data/weapon-base-balance.csv) and [branch](./data/weapon-branch-balance.csv) machine-readable tables mirror the initial catalog so agents and balancing tools can compare all 15 weapons without scraping prose. The tables must never introduce a value absent from an authoritative gameplay specification.

## Related documents

- [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md)
- [Weapon Specification Index](./weapons/README.md)
- [Weapon Stat and Branch Upgrades](./65-weapon-stat-and-branch-upgrades.md)
- [Initial Alien and Boss Roster](./31-initial-alien-roster.md)
- [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md)
- [Resources, Crafting, and Progression](./60-resources-crafting-progression.md)
- [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md)
- [Utility Catalog](./68-utility-catalog.md)
- [Initial Relic Catalog](./69-initial-relic-catalog.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [DEC-084 — Price stat upgrades by total weapon depth](./decisions/DEC-084-price-stat-upgrades-by-weapon-depth.md)
- [DEC-085 — Use a triangular shared-depth price curve](./decisions/DEC-085-use-triangular-shared-depth-prices.md)
- [DEC-112 — Bound permanent power below run-build power](./decisions/DEC-112-bound-permanent-power-below-run-build-power.md)
- [DEC-119 — Accept the initial alien encounter baseline](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [DEC-124 — Adopt a multi-metric weapon balance framework](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
