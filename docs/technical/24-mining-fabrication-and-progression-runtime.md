---
doc_id: TDD-MINING-PROGRESSION
title: Mining, Fabrication, and Progression Runtime
status: active
authoritative: true
---

# Mining, Fabrication, and Progression Runtime

## Purpose

This document defines automatic mining, progress/decay, payouts, resonance and beacon transitions, run inventory, fabrication transactions, relic caches, radar state, terminal settlement, and persistent progression boundaries.

## Mining site state

Every site owns:

- generated site ID, definition/class, position, extraction radius, and presentation IDs;
- total required work and current unfinished work;
- completed installment count and finite capacity for ore seams;
- inside/outside state and outside-grace remaining;
- complete/depleted flag;
- resonance material and field radius if a geode;
- beacon activation and crossed-threshold bitset if Hyper Gold;
- discovery/map state; and
- cumulative attempted, forward, decay, and completion metrics.

A site is immutable after completion except for discovery presentation. Completed sites never reactivate.

The baseline extraction radius is 3.0M for every site class and the geode resonance-field radius is 6.0M, accepted by [DEC-128](../decisions/DEC-128-set-extraction-zone-and-resonance-field-radii.md). Both are ordinary accepted content values: implement them exactly, with no proof gate owed. The occupancy test uses the baseline radius scaled by the run's current additive extraction-zone modifiers, which change when an Extraction Tether rank is installed mid-run. The resonance-field radius is never scaled, because no utility or PowerUp changes resonance-field size.

## Occupancy and progress

The mining phase samples the player's committed post-movement center against the inclusive extraction circle. Valid standard generation prevents extraction zones from overlapping, so at most one site can advance. A map with overlapping zones fails validation rather than relying on runtime arbitration.

### Inside

- Begin or continue forward work automatically.
- Clear outside grace.
- Advance by base work rate multiplied by additive extraction bonuses, then explicit relic transformation.
- Set the run-wide `actively mining` condition only when positive forward work is applied to an incomplete site.
- Resolve every crossed installment, beacon threshold, and completion in ascending work order.

### Outside

- If prior state was inside, start a 0.5-active-second grace period.
- Hold work unchanged while grace remains.
- After grace, subtract work at four times the site's current forward extraction rate.
- Clamp at zero and return to available state.
- Never reverse a completed ore installment or already-triggered beacon threshold.

Occupancy is evaluated once per tick after movement. A completion occurs only when the post-movement position is inside on that tick. Boundary equality counts as inside. Paused states execute no mining phase, so progress and grace do not advance.

Damage, shields, control applied to enemies, and player attack state do not affect mining. Any future forced player movement changes occupancy only through the same position test.

## Payout profiles

| Site | Work model | Payout commit |
| --- | --- | --- |
| Standard seam | ten 1.5-second-equivalent installments | 10 common ore at each installment; completed installments checkpoint |
| Rich seam | five 3-second-equivalent installments | 40 common ore at each installment; completed installments checkpoint |
| Material geode | one 20-second-equivalent completion | one assigned material plus 50 common ore atomically |
| Hyper Gold | one 45-second-equivalent completion | 100 unsecured Hyper Gold atomically |

“Second-equivalent” refers to work at unmodified extraction rate. Modifiers scale forward rate and proportional decay without rewriting thresholds.

Payout application checks integer overflow, appends a resource-ledger entry, updates HUD view state, records statistics, and emits presentation events as one commit. Presentation effects never grant currency.

## Hyper Gold beacon state

On the first positive forward-work tick, set activation permanently and request the activation response. On first crossing of 25%, 50%, and 75%, set the corresponding bit before requesting its response. Decay never clears bits.

- Each response request includes site ID, threshold, current active minute, composition row, response size, formation, warning, and elite additions.
- Leaving stops work and therefore new thresholds but does not change queued or living response enemies.
- Completion prevents later response requests but retains all living/queued prior responses.
- Duplicate requests for a set bit are rejected and logged.

## Geode resonance lifecycle

An unopened geode registers one active resonance circle with the spatial service at map creation. Completion unregisters it during the mining commit phase and emits a collapse event. It never exists without its site or overlaps another valid resonance field.

Enemy systems sample current membership as described in the encounter specification. The site itself does not iterate all enemies or modify their stored base definitions.

## Run resource ledger

The run inventory stores integer balances for common ore, each of six specialized materials, and unsecured Hyper Gold. Every change appends a typed ledger entry with tick/paused version, source, amount, resulting balance, and relevant site/boss/cache/purchase ID.

Sources are closed categories: seam installment, geode completion, Hyper Gold completion, boss pickup, relic sale, Ore Catalyzer bonus, fabrication spend, and terminal settlement. Ordinary enemy death is not a valid resource source.

The ledger is the basis for results totals and transaction diagnostics. It is not replayed to reconstruct state during normal play; current balances remain authoritative.

## Fabrication availability model

Opening fabrication freezes the simulation and builds an immutable catalog view from:

- present four-material geological profile and abundance;
- unlocked account options;
- current four weapon slots and three utility slots;
- installed branches, stat ranks, and shared depths;
- current run balances; and
- fixed content registry.

The view includes available, owned, incompatible, slot-full, absent-material, unaffordable, and permanently-excluded reasons. Reopening never rerolls or changes availability except through actual state changes.

### Weapon fabrication transaction

Validate:

1. recipe's two materials are both present and owned in required quantity;
2. weapon is unlocked and not already equipped;
3. a weapon slot is empty;
4. request state version remains current; and
5. confirmation names the exact weapon, slot, and permanent run commitment.

Atomically subtract one unit of each recipe material, create zero-rank/unbranched runtime state, fill the chosen stable lowest empty slot unless the UI explicitly selects one, and invalidate derived views.

### Stat-rank transaction

Validate weapon ownership and ore balance. Compute next price from shared depth using `5(d + 1)(d + 2)` with checked integer arithmetic. Subtract ore, increment only the selected stat rank and shared depth, invalidate derived statistics, and publish old/increment/new values.

There is no cap, but the transaction rejects arithmetic overflow or a resulting value outside the schema's safe numeric domain. Content validation proves a generous maximum test depth even though normal economy is the practical cap.

### Branch transaction

Validate weapon ownership, no current branch, branch association, two units of the assigned present material, and irreversible confirmation. Subtract material, install the branch, invalidate derived behavior, and exclude the alternatives atomically. No rank prerequisite or follow-on branch exists.

### Utility transaction

Validate blueprint unlock, assigned present material, one-unit cost, no duplicate, and empty utility slot. Installation permanently occupies the slot. Utility ore ranks use the utility catalog's fixed rank caps, prices, and increments and share no price depth with weapons.

### Resource radar

Radar is a special always-unlocked utility costing 300 common ore. Installation fills a utility slot and creates no active targeting choice. Each active tick or relevant site transition queries the nearest incomplete standard seam, rich seam, Hyper Gold site, and unopened geode of each present material. Results contain bearing and category only; distance and map location remain undisclosed.

Nearest ties use route-independent planar distance, then generated site ID. A category immediately retargets after completion and enters exhausted state when no valid site remains. Presentation performs bearing fanning/clustering without changing targets.

## Relic cache transaction

Touching an unopened cache during movement queues a cache-open boundary after the current tick and adds the relic pause reason. The cache becomes opened exactly once before presenting the choice.

The paused choice view contains assigned relic, installed relic if any, affected-weapon compatibility results, exact current values, and the 150-ore sale outcome.

- **Sell new relic:** add 150 common ore; installed relic unchanged.
- **Install into empty slot:** install new relic; no ore.
- **Replace installed relic:** install new relic and add 150 common ore for the displaced relic atomically.

The decision is mandatory and cannot be deferred. Duplicate commands are idempotent through cache state and command sequence.

## PowerUp and option-unlock transactions

Between runs, the profile exposes the accepted PowerUp tracks and option purchases.

- PowerUp ranks validate cap and Hyper Gold balance, spend fixed price, and permit an active rank from zero through owned rank.
- A full or individual refund returns exactly the historical fixed purchase cost of refunded ranks; no fee, loss, or price drift applies.
- Option unlocks validate one-time ownership and spend their nonrefundable fixed price.
- Purchased option unlocks cannot be disabled to narrow random pools.
- All profile mutations use atomic persistence before the UI reports success.

The runtime compiles active PowerUps into an immutable deployment modifier snapshot. Later profile changes cannot alter a run already in progress.

## Deployment and geological profile

Deployment confirmation snapshots selected mech, active PowerUps, unlocked options, content versions, and a new master run seed. The profile generator chooses exactly four of six materials from signature-valid profiles containing at least two of the signature weapon's three branch resources.

The resulting material identities and abundance counts enter the generation manifest. They remain hidden until the active 0.5-second survey reveal but are not secret from the simulation or validation tools.

The signature weapon starts in its stable assigned slot with zero run ranks and no branch. It does not consume its recipe materials.

## Terminal settlement

Terminal settlement is a two-stage transaction.

1. Freeze the run and produce an immutable result manifest containing outcome, balances, ledger totals, build, statistics, map/exploration summary, content/build identity, and diagnostic ID.
2. If outcome is successful extraction, atomically add collected unsecured Hyper Gold to banked profile Hyper Gold and write the profile. On failure/abandonment add none.

In every outcome, ordinary resources, run equipment, relic, and unsecured Hyper Gold disappear with run disposal after the result manifest is retained for the results screen.

If profile saving fails after success, do not discard the result manifest or falsely confirm banking. Retain a pending settlement record locally and retry idempotently at next safe startup. Settlement identity prevents double banking after a crash.

## Results data

The result manifest contains all gameplay-required fields and enough provenance to explain them:

- outcome, active ticks, wall duration, mech, seed, build/content versions;
- kills and boss defeats by identity;
- final weapons, ranks, branches, utilities/ranks, relic, and active PowerUps;
- attempted/effective/overkill damage by weapon/branch/relic;
- mining attempts, completions, forward/decay work, and payouts by site class;
- resources collected, spent, sold, discarded, banked, and forfeited;
- exploration cells/regions/sites/caches/loot discovery; and
- records/unlocks awarded once persistence confirms them.

## Verification

- Tick fixtures cover inclusive boundary, 0.5-second grace, four-times decay, pause, re-entry, installment checkpointing, exact threshold order, and completion.
- Modifier matrices cover Prospector, PowerUps, Extraction Accelerator, Extraction Tether, Claim-Jumper, and their specified additive/multiplicative order.
- Economy fixtures prove every price, recipe, slot, duplicate, absent profile, branch exclusion, utility cap, refund, and overflow rejection.
- Relic-cache tests cover install, sell, replace, stale/double command, and pause overlap.
- Settlement crash tests interrupt before write, during temporary write, after replacement, and before acknowledgement and prove no loss or double bank.
- Radar fixtures cover all seven categories, ties, completion retargeting, exhaustion, undiscovered targets, and no distance leakage.
- Full-run ledger reconciliation must equal every displayed results total.

## Related documents

- [Simulation Core](./20-simulation-core.md)
- [Encounter Director and Enemy Runtime](./23-encounter-director-and-enemy-runtime.md)
- [Persistence and Platform Services](./70-persistence-and-platform-services.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [Interface, Screen Flow, and Information Architecture](../73-interface-screen-flow-and-information-architecture.md)
