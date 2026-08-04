---
doc_id: GDD-STANDARD-WAVE-SCHEDULE
title: Standard Wave and Beacon Schedule
status: active
authoritative: true
---

# Standard Wave and Beacon Schedule

## Schedule status

This document defines the accepted first-pass 35-minute ordinary wave schedule, event formations, boss-phase pressure, final crescendo, and Hyper Gold threat-beacon response. [DEC-119](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md) accepts it as the deterministic standard-mode balance baseline. Exact counts and intervals are intentionally revisable through playtesting under the [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md); the phase structure, identity progression, event grammar, and boss/beacon relationships are the stable design.

## Director vocabulary

Each minute row specifies:

- **Composition:** the ordinary identities selected by weighted share for baseline replenishment. Shares are approximate and sum to 100%.
- **Minimum:** the desired baseline number of ordinary enemies active around the player. A living boss, scheduled elite, boss-spawned minion, or threat-beacon enemy does not reduce this minimum.
- **Pulse:** the batch size and active-simulation interval used while the baseline population is below its desired minimum. Once the minimum is met, baseline spawning waits until attrition creates room.
- **Event:** a deterministic authored formation layered over baseline replenishment at the listed time.

Ordinary baseline and event enemies spawn on valid navigable ground outside the active camera. Ordinary enemies far outside the pressure area may be recycled to another valid off-screen approach. Bosses and Hyper Gold beacon-tagged enemies are never discarded merely because the player retreats.

The initial performance ceiling is 450 baseline ordinary enemies plus up to 100 short-lived scheduled-event overflow and 150 persistent beacon-tagged enemies. Bosses are separate. If a ceiling is reached, pending authored enemies wait and enter as capacity opens rather than being canceled or converted into stronger invisible statistics.

## Formation grammar

- **Stream:** repeated narrow batches enter from one off-screen sector along parallel paths.
- **Wall:** a broad line advances from one side with at least one clearly readable traversal gap.
- **Swarm:** a dense but bounded cluster enters from one quadrant.
- **Twin flanks:** matched streams enter from two separated sectors, leaving two other escape directions.
- **Encirclement:** a loose off-screen ring converges while preserving a visible 70–90-degree escape arc.
- **Convergence:** four smaller groups enter from cardinal sectors with diagonal gaps.
- **Rolling ring:** two separated incomplete encirclements arrive sequentially rather than spawning simultaneously.

Formations reuse the listed identities and their ordinary fixed profiles. They do not grant new movement, damage, drops, or hidden buffs.

## Phase-level pressure curve

| Phase | Purpose | Reference-style pressure translation |
| --- | --- | --- |
| 0:00–1:00 | Survey orientation | A few fragile bodies demonstrate automatic combat without contesting survey reading |
| 1:00–7:00 | Establish the build | Population rises steadily; durable bodies and simple formations begin to constrain mining |
| 7:00–14:00 | Early escalation | Boss relief is brief; more mixed silhouettes test coverage and route choice |
| 14:00–21:00 | Mid-run complication | Fast variants, the sole projectile specialist, elites, and flank events appear |
| 21:00–28:00 | Mature-build test | Late durable identities combine with fast bodies and specialist pressure |
| 28:00–35:00 | Final crescendo | The last boss arrives with temporary density relief, followed by sustained screen saturation and frequent formations |

Boss-arrival minutes deliberately reduce the baseline minimum by roughly 10–20% from the preceding minute and contain no additional scheduled formation. This creates a readable boss entrance without pausing ordinary combat or turning the encounter into an empty arena. The next minute restores and then exceeds the previous pressure.

## Complete 35-minute schedule

| Minute | Composition | Minimum | Pulse | Authored event or boundary |
| ---: | --- | ---: | --- | --- |
| 0 | Skitterling 100% | 8 | 2 / 1.50s | Survey orientation; no formation |
| 1 | Skitterling 80%, Ripper 20% | 18 | 3 / 1.20s | Standard escalation begins |
| 2 | Skitterling 65%, Ripper 35% | 24 | 4 / 1.00s | None |
| 3 | Skitterling 55%, Ripper 45% | 30 | 5 / 0.90s | 3:30 Skitterling stream from one side |
| 4 | Ripper 55%, Skitterling 25%, Shellback 20% | 36 | 5 / 0.80s | Shellback debut |
| 5 | Ripper 50%, Lurker 30%, Shellback 20% | 42 | 6 / 0.75s | Lurker debut |
| 6 | Ripper 45%, Lurker 30%, Shellback 25% | 50 | 7 / 0.70s | 6:30 loose encirclement; 6:45 boss warning |
| 7 | Ripper 55%, Shellback 45% | 44 | 6 / 0.75s | 7:00 Riftjaw arrives; no formation |
| 8 | Lurker 45%, Ripper 35%, Skitterling 20% | 58 | 7 / 0.65s | None |
| 9 | Lurker 45%, Shellback 30%, Ripper 25% | 66 | 8 / 0.60s | 9:30 Lurker wall with central gap |
| 10 | Gloomwing 45%, Skitterling 30%, Lurker 25% | 74 | 8 / 0.55s | Gloomwing debut |
| 11 | Gloomwing 40%, Ripper 35%, Lurker 25% | 82 | 9 / 0.50s | None |
| 12 | Shellback 40%, Gloomwing 35%, Skitterling 25% | 90 | 10 / 0.48s | 12:30 Skitterling quadrant swarm |
| 13 | Razorling 40%, Ripper 35%, Shellback 25% | 100 | 10 / 0.45s | Razorling debut; 13:30 one elite Ripper; 13:45 boss warning |
| 14 | Razorling 55%, Shellback 45% | 86 | 9 / 0.50s | 14:00 Brood Titan arrives; no formation |
| 15 | Razorling 45%, Gloomwing 35%, Lurker 20% | 108 | 10 / 0.45s | None |
| 16 | Ripper 40%, Razorling 40%, Needler 20% | 120 | 11 / 0.42s | Needler debut; no separate formation |
| 17 | Lurker 40%, Razorling 40%, Needler 20% | 132 | 12 / 0.40s | 17:30 Razorling stream |
| 18 | Skitterling 50%, Iron Ripper 30%, Needler 20% | 145 | 13 / 0.38s | Iron Ripper debut |
| 19 | Gloomwing 40%, Iron Ripper 35%, Needler 25% | 158 | 14 / 0.36s | 19:30 twin Gloomwing flanks |
| 20 | Iron Ripper 40%, Shellback 35%, Razorling 25% | 172 | 15 / 0.34s | 20:30 one elite Iron Ripper; 20:45 boss warning |
| 21 | Iron Ripper 55%, Needler 45% | 150 | 13 / 0.38s | 21:00 Prism Crown arrives; no formation |
| 22 | Skitterling 50%, Iron Ripper 30%, Siegeback 20% | 185 | 15 / 0.34s | Siegeback debut |
| 23 | Razorling 40%, Gloomwing 35%, Siegeback 25% | 200 | 16 / 0.32s | 23:30 Razorling wall with two gaps |
| 24 | Iron Ripper 40%, Dreadwing 35%, Skitterling 25% | 215 | 17 / 0.30s | Dreadwing debut |
| 25 | Razorling 45%, Dreadwing 35%, Needler 20% | 232 | 18 / 0.28s | 25:30 twin Razorling flanks |
| 26 | Iron Ripper 45%, Needler 30%, Siegeback 25% | 250 | 19 / 0.27s | None |
| 27 | Razorling 40%, Dreadwing 35%, Siegeback 25% | 270 | 20 / 0.26s | 27:20 one elite Dreadwing; 27:30 encirclement; 27:45 boss warning |
| 28 | Dreadwing 60%, Iron Ripper 40% | 230 | 18 / 0.28s | 28:00 Skybreaker Apex arrives; no formation |
| 29 | Dreadwing 40%, Razorling 35%, Siegeback 25% | 275 | 20 / 0.25s | 29:20 and 29:45 alternating walls |
| 30 | Dreadwing 40%, Razorling 35%, Needler 25% | 300 | 22 / 0.23s | 30:30 Needler-backed loose encirclement |
| 31 | Iron Ripper 40%, Gloomwing 35%, Siegeback 25% | 325 | 24 / 0.21s | 31:30 four-sector convergence |
| 32 | Dreadwing 40%, Siegeback 30%, Needler 30% | 350 | 26 / 0.19s | 32:30 one elite Siegeback plus encirclement |
| 33 | Razorling 35%, Iron Ripper 35%, Dreadwing 30% | 380 | 28 / 0.18s | Streams rotate through four sectors at 33:15 intervals |
| 34 | Dreadwing 40%, Siegeback 30%, Razorling 30% | 420 | 30 / 0.16s | Rolling rings at 34:10 and 34:40; extraction warning begins |

At 35:00 the living player extracts immediately. No fifth boss, invincible pursuer, final damage pulse, or post-timer survival check appears. Enemies and unresolved attacks cease with the run state.

## Hyper Gold threat-beacon response

A Hyper Gold site triggers four persistent response packages at activation and the first crossing of 25%, 50%, and 75% extraction progress. Each response uses the current minute's ordinary composition rather than a dedicated beacon-only alien. This keeps the pressure legible and lets a site attempted late remain proportionally dangerous.

Let `P` be the current minute's desired minimum ordinary population. Rounded counts use the nearest whole enemy, with the listed floor.

| Trigger | Response size | Formation | Elite addition |
| --- | ---: | --- | --- |
| Activation | max(12, 15% of P) | One concentrated stream from the site's outward side | None |
| 25% | max(16, 20% of P) | One dense quadrant swarm | None |
| 50% | max(20, 25% of P) | Twin flanks with open retreat directions | One elite of the most common eligible pure pursuer |
| 75% | max(28, 35% of P) | Loose encirclement with a 70-degree escape arc | Two elites chosen from eligible pure pursuers in the minute |

- Each trigger produces a site pulse, HUD callout, and two-second warning before enemies begin entering from valid off-screen ground.
- The response draws ordinary identities using the current composition weights. Needler may appear as an ordinary responder but is never selected for the elite addition.
- Beacon enemies are tagged to that site and remain active until killed or run end. Leaving stops new extraction thresholds but does not remove or recycle the response.
- If the 150-enemy beacon ceiling is occupied, a triggered response remains queued and enters as tagged enemies die. The player never cancels a response by forcing the ceiling.
- Completion prevents further beacon responses but does not dismiss survivors. Every responder follows its fixed identity profile and drops nothing.
- A boss may be selected by the ordinary profile's movement or resonance modifiers but never counts as part of a beacon response.
- Multiple activated sites keep separate threshold histories and tags. Their surviving forces may overlap if the player deliberately activates more than one.

## Difficulty and build-readiness targets

- **0:00–1:00:** A player reading the survey while moving casually should rarely take damage. The signature weapon alone clears enough space to demonstrate targeting.
- **By 7:00:** A reasonable route permits several signature stat ranks or one additional weapon before Riftjaw. The boss is survivable with the signature alone but should take long enough to make that choice unattractive.
- **By 14:00:** A coherent run normally has at least two weapons plus meaningful stat or utility investment. Brood Titan punishes exclusively single-target development without requiring a particular weapon.
- **By 21:00:** Needlers and mixed fast bodies require deliberate movement inside mining zones. A healthy run is approaching three or four weapons and has made at least one major branch choice.
- **By 28:00:** The build should be functionally mature. Failing to defeat Prism Crown before Skybreaker arrives is possible but represents a meaningful damage deficit.
- **28:00–35:00:** Population and formation frequency, rather than additional ordinary mechanics, create the final test. A coherent build should destroy large masses and feel powerful while still needing to move around fast bodies, Needler shots, and the final boss.

## Playtest adjustment order

When a phase is too hard or too easy, adjust in this order:

1. Desired population and pulse replenishment.
2. Event batch size, timing, or escape geometry.
3. Ordinary identity mixture.
4. Boss Hull or added-behavior cadence.
5. Ordinary fixed identity profiles only when that identity is problematic across several appearances.

Do not add hidden elapsed-time scaling, adaptive director logic, more specialist attacks, or resource-profile counters to repair a schedule problem.

## Instrumentation and validation

- Record active ordinary count, event overflow, beacon-tagged count, spawn throughput, kill throughput, frame time, and contact-damage frequency by minute.
- Record first damage, first mining interruption, boss arrival-to-death time, boss overlap duration, and deaths by source.
- Compare milestone reach, stacked-hit sequences, pack collection, low-Hull time, and extraction rate with the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md).
- Compare the same authored schedule across all mechs, all signature-valid resource profiles, and fresh, partial, and upgraded accounts.
- Specifically test 45-second Hyper Gold holds begun in every seven-minute phase and during a living boss overlap.
- Verify that formations preserve a readable route at the fixed camera scale and do not spawn into solid geometry, mining circles, or unavoidable contact.
- Steam Deck stress tests use minute 34 plus the 75% beacon response and one living boss as the representative maximum-pressure case.

## Related documents

- [Initial Alien and Boss Roster](./31-initial-alien-roster.md)
- [Run Structure, Timing, Bosses, and Mission Extraction](./20-run-structure-and-timing.md)
- [Mining and Extraction](./40-mining-and-extraction.md)
- [Standard Map Generation Contract](./51-standard-map-generation-contract.md)
- [Combat and Economy Balance Framework](./70-combat-and-economy-balance-framework.md)
- [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md)
- [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md)
- [DEC-098 — Use minute-authored horde waves](./decisions/DEC-098-use-minute-authored-horde-waves.md)
- [DEC-119 — Accept the initial alien encounter baseline](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md)
- [DEC-124 — Adopt a multi-metric weapon balance framework](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md)
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md)
- [DEC-126 — Adopt the initial player survivability baseline](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md)
- [RES-001 — Vampire Survivors reference mechanics](./research/RES-001-vampire-survivors-reference.md)
- [RES-002 — Holdout and extraction pressure patterns](./research/RES-002-holdout-extraction-pressure-patterns.md)
