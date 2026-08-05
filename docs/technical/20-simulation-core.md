---
doc_id: TDD-SIMULATION-CORE
title: Simulation Core
status: active
authoritative: true
---

# Simulation Core

## Purpose

This document turns the runtime boundary into implementable simulation contracts. It defines state representation, entity identity, numeric conventions, tick semantics, modifiers, events, snapshots, capacity behavior, and terminal ordering. Specialized systems build on these rules.

## Scope and invariants

The simulation is a pure C# library with no dependency on Godot, files, Steam, rendering, audio, wall time, or mutable global services.

The following invariants hold throughout an active run:

- exactly one player entity exists until terminal resolution;
- every live entity ID resolves to exactly one live record of the matching generation;
- all authoritative positions are valid planar positions or carry an explicit non-interacting transitional state;
- every content reference resolves through the immutable run content registry;
- no currency, equipment slot, stat rank, branch, relic, pickup, or persistent reward becomes negative or exceeds its content-defined representation;
- structural entity changes occur only during defined commit phases;
- simulation time advances only by complete fixed ticks;
- presentation cannot mutate simulation state; and
- a run terminal result is assigned once and is immutable.

Debug and test builds assert invariants at the end of every tick. Release builds retain inexpensive boundary validation and fail the run safely on corruption rather than continuing with untrusted state.

## Numeric and unit conventions

| Quantity | Authoritative representation |
| --- | --- |
| Run time | 64-bit integer simulation tick plus derived seconds |
| Position, distance, speed | floating-point values in gameplay meters on the simulation plane |
| Direction | normalized planar vector; zero direction is explicit |
| Player-facing bearing | degrees clockwise from north, normalized to `[0, 360)` only at display/content boundaries |
| Hull, Armor, resources, ranks, counts | signed or unsigned integers with checked conversion and validated nonnegative domain |
| Percentages and multipliers | normalized floating-point factor internally; player-facing percentage retained in content |
| Progress | normalized `[0,1]` value or exact accumulated work units owned by the relevant system |
| Random state | versioned integer state owned by a named stream |

The simulation uses double precision for accumulated schedules, cooldown phase, extraction work, and derived stat calculations; planar transforms may use single precision after tests confirm the accepted map scale remains safely within precision bounds. All conversion to Godot vectors occurs in presentation.

Content seconds convert to schedules that never complete earlier than authored. Rate-based systems use phase accumulation so values such as attacks per second or extraction multipliers preserve their long-run average without variable-delta updates. Thresholds crossed within one tick resolve in ascending threshold order.

## Authoritative random-number contract

Authoritative streams use the repository-owned **PCG-XSH-RR 64/32 (PCG32)** algorithm. Its 64-bit state advances modulo `2^64` with multiplier `6364136223846793005` and a per-stream odd increment. Output uses the PCG XSH-RR transformation: xorshift the prior state by 18 and 27 bits and rotate the resulting 32-bit value by the prior state's top five bits. Golden vectors pin initialization and output, so a runtime/library RNG is never an implicit substitute.

A deployment master seed is one unsigned 64-bit value. The current random schema version is `1`. Define the unsigned-64-bit wrapping function `Mix(x)` by adding `0x9E3779B97F4A7C15`, applying xor-shift 30 then multiplying by `0xBF58476D1CE4E5B9`, applying xor-shift 27 then multiplying by `0x94D049BB133111EB`, then applying xor-shift 31. Derive `d0 = Mix(master seed XOR (schema version × 0xD1B54A32D192ED03))`, `d1 = Mix(d0 XOR family key)`, `state seed = Mix(d1 XOR (instance key × 0x9E3779B97F4A7C15))`, and `selector = Mix(state seed XOR 0x94D049BB133111EB)`. All arithmetic wraps modulo `2^64`.

Initialize PCG32 with state zero and increment `(selector shifted left one bit) OR 1`; advance once, add `state seed` to state modulo `2^64`, and advance once again before returning the first caller-visible value. Golden fixtures include this derivation and initialization. Changing any operation increments the random schema version and invalidates incompatible recovery rather than silently changing a compatible run.

| Family key | Stream family | Instance key |
| ---: | --- | --- |
| `0x0100` | resource-profile selection | zero |
| `0x0200` | major topology | zero |
| `0x0201` | spatial embedding | zero |
| `0x0202` | region recipes | stable generated region ID |
| `0x0203` | landmarks | stable generated region ID |
| `0x0204` | obstacle/dressing placement | stable generated region ID |
| `0x0205` | deployment selection | zero |
| `0x0210` | standard-seam placement | zero |
| `0x0211` | rich-seam placement | zero |
| `0x0220` | material-geode placement | canonical material ordinal `0–5` |
| `0x0230` | Hyper Gold placement | zero |
| `0x0240` | relic-cache placement | zero |
| `0x0241` | relic assignment | zero |
| `0x0250` | dynamic rocks/drop rolls | zero for placement; stable rock ID for its one drop roll |
| `0x0260` | release fallback-manifest selection | selected profile ordinal plus region-count ordinal |
| `0x0300` | baseline encounter sectors/composition | zero |
| `0x0301` | authored event formations | schedule-row/minute index |
| `0x0302` | beacon response selection | stable Hyper Gold site ID |
| `0x0303` | boss entry/ability randomness | scheduled boss index `0–3` |
| `0x0400` | player weapon combat randomness | weapon slot ordinal `0–3` |
| `0x0410` | enemy combat randomness | stable spawning source plus entity generation encoded as instance key |
| `0x0500` | boss/other authorized loot | stable reward-source ID |
| `0xF000` | presentation-only variation | presentation binding identity; never serialized into authoritative state |

New authoritative randomness receives a unique registered family key in this table; keys are never repurposed. A category retry or an added visual draw cannot consume another family's sequence. Stable generated IDs and ordinals come from canonical manifest/order rules, never dictionary or scene enumeration.

- Stream state and odd increment are included in run recovery for every instantiated authoritative stream.
- Unbiased bounded integers use rejection sampling rather than modulo reduction.
- A `[0,1)` double is built from 53 random bits under one golden-tested conversion; a chance that can be represented as an integer ratio compares integers instead.
- Selection from a collection first establishes canonical candidate order, then draws an index.
- An empty/singleton selection consumes no draw; this convention is fixture-pinned.
- Tests may inject a scripted source, but production content cannot select an alternate algorithm.
- Presentation may use the presentation family or a separate visual generator, but no presentation draw or state is read by simulation.

## Entity identity

An entity ID contains a reusable storage index and a generation. Reusing a slot increments its generation, making stale references invalid.

- IDs are unique only within one run session.
- The player has a stable reserved ID.
- IDs are not content IDs and are not persisted between runs.
- Cross-system references store entity IDs, never direct mutable object references.
- Invalid, expired, or generation-mismatched references fail closed and produce a diagnostic counter.
- Stable ordering uses the full entity ID after a system's authored priority keys.

The implementation uses purpose-built packed stores by population category, not a general reflection-driven ECS framework. A new generic abstraction is justified only when at least three concrete systems require the same lifecycle and query semantics.

## Authoritative population categories

| Category | Required state | Typical lifecycle |
| --- | --- | --- |
| Player | transform, facing, movement, Hull, modifiers, loadout, run inventory, contact grace | run lifetime |
| Ordinary enemy | definition, transform, motion, Hull, contact cooldown, control state, spawn tags | spawn to death/recycle/run end |
| Elite | ordinary state plus elite modifiers and marker | event/beacon spawn to death/run end |
| Boss | definition, transform, Hull, behavior state machine, contact state, re-entry state | scheduled arrival to death/run end |
| Enemy projectile | origin identity, transform, velocity, damage snapshot, lifetime, collision flags | fire to impact/terrain/expiry |
| Weapon actor | weapon provenance, owner slot, transform, timing, branch/relic snapshot or live modifier policy | attack-specific |
| Damage zone | geometry, provenance, tick policy, affected-target memory, expiry | attack-specific |
| Mining site | class, position, zone, progress, checkpoint state, completion, beacon thresholds | map lifetime |
| Pickup | resource kind, amount, position, collection radius, provenance | spawn to collection/run end |
| Destructible rock | position, Hull, footprint, drop-roll state | spawn to destruction/recycle |
| Relic cache | position, assigned relic, discovery/open state | map lifetime |
| Static world object | stable map ID, geometry and presentation references | map lifetime |

Weapon actors cover projectiles, beams, mines, pods, drones, orbiters, trails, delayed echoes, and other attack state. They use specialized packed stores when their update pattern differs materially; they are not forced into one sparse universal component table.

## Tick transaction

Each active tick is a transaction over one prior world state.

1. Read the prior committed state and commands admitted for this tick.
2. Execute the fixed system phase order in [Runtime Architecture](./10-runtime-architecture.md#system-phase-ordering).
3. Append damage, spawn, removal, payout, domain, presentation, and metric records to tick-local buffers.
4. Apply each buffer only in its documented commit phase and stable ordering.
5. Evaluate terminal state.
6. Publish the committed state, snapshot, events, and diagnostics as one tick result.

An exception or invariant failure before commit invalidates the tick and ends the run through the safe technical-failure path; it never publishes a partial state.

## Boundary and tie ordering

The following rules eliminate frame-dependent ambiguity:

- A circle boundary is inclusive: a center distance equal to the summed radii counts as inside or overlapping.
- When entering and leaving are both plausible due to one swept move, continuous crossing time determines the state at tick end and emits crossings in chronological order.
- An attack scheduled for a tick uses the transform committed by the movement phase of that same tick.
- Damage instances remain separate and resolve by system phase, explicit attack sequence, target ID, source ID, then insertion sequence.
- A target reduced to zero Hull ignores later damage instances in the same tick unless a weapon rule explicitly consumes overkill.
- Death consequences are emitted once during the death commit phase.
- Mining installments or thresholds crossed during a tick resolve before the site completion event when their threshold is lower.
- Active ticks cover times strictly before 35:00. After the tick covering the final pre-boundary interval commits, the clock reaches 35:00 and successful extraction is evaluated before any attack, spawn, hazard, or other event scheduled for 35:00 or later can begin.
- The player must have positive Hull after the final pre-boundary tick. A death resolved in that tick is failure; otherwise extraction wins immediately and no later simulation step can deal damage.

## Derived statistics and modifiers

The content registry supplies immutable base definitions. A run owns mutable loadout state: ranks, branch, utilities, relic, mech trait, and account PowerUps.

Every derived-stat calculation records its contributing layers:

1. base content value;
2. fixed flat changes such as ore-rank increments;
3. additive percentages to the same named statistic from mech trait, PowerUps, and utilities;
4. branch-specific transformations and multipliers;
5. relic replacement rules and multipliers;
6. conditional runtime modifiers such as heat or mining state; and
7. target-side modifiers such as resonance, Armor, resistance, or clustering.

The order follows the gameplay [balance framework](../70-combat-and-economy-balance-framework.md#stacking-and-comparison-order). A derived stat is recomputed only when a contributing version changes, not by traversing the entire modifier graph every tick.

Each cached result carries a monotonically increasing loadout version. Already-created finite weapon instances retain the creation-time values required by the gameplay specification; persistent actors query the defined live values only for future attacks. The effect catalog explicitly classifies every field as snapshotted-at-creation or live-read-at-action.

## Commands and paused transactions

### Active commands

The initial active command surface contains movement intent and non-authoritative presentation shortcuts. Movement intent is normalized to a planar vector with magnitude `[0,1]`; digital diagonals normalize to unit length. The simulation applies immediate direction and full current speed for nonzero input and stops on zero input.

### Paused transactions

Fabrication, relic resolution, PowerUp purchases/refunds, option unlocks, deployment confirmation, abandonment, and profile reset use typed transactions outside active ticks.

Every transaction carries:

- application or run-session identity;
- expected state version;
- action identity and typed selection;
- client command sequence for deduplication; and
- optional confirmation token for irreversible actions.

Validation returns either a new complete state/version plus domain events or a typed rejection with no mutation. Purchases check ownership, availability, slot capacity, duplication, cost, prerequisites, branch exclusivity, and integer overflow atomically.

## Domain and presentation events

Domain events are immutable facts used by other authoritative or application systems: entity defeated, boss defeated, resource awarded, item installed, threshold crossed, run terminal, and similar outcomes. Presentation events are disposable instructions such as attack fired, hit confirmed, mining installment, warning, or loot burst.

- Events carry tick, sequence, stable event kind, relevant entity/content IDs, position, and typed payload.
- Consumers never infer authoritative state solely from presentation events.
- Presentation events may be coalesced by an explicit visual policy; domain events may not be dropped.
- Statistics consume domain/damage records before their buffers are released.
- Event schemas are versioned when written to diagnostic artifacts.

## Presentation snapshot

At the end of each tick the simulation publishes a read-only snapshot optimized for presentation synchronization. It includes:

- tick and interpolation anchor;
- player transform, facing, Hull, mining state, loadout summary, resources, and relevant meters;
- visible or potentially visible entity transforms and presentation-state flags;
- mining sites, pickups, caches, discovered markers, and boss warnings;
- run clock, schedule phase, pause-independent display state, and terminal state; and
- versioned HUD view models whose numbers already reflect authoritative calculation and rounding.

Snapshots do not expose mutable stores. Double buffering or immutable pooled pages avoids copying untouched static map state. Presentation interpolates transforms between the two most recent complete snapshots but snaps on spawn, teleport, re-entry, terminal transition, or a distance threshold that would make interpolation misleading.

## Capacity and overload behavior

Every dynamic store has a documented soft target, hard capacity, and overflow behavior.

- Authored enemies that reach a gameplay ceiling queue and later enter; they are not silently canceled or converted.
- Visual-only particles, decals, trails, hit sparks, and audio voices may use priority-based degradation without affecting simulation.
- Authoritative projectiles and persistent weapon actors may not disappear because a visual pool is full. Their presentation can use a simplified fallback.
- A hard authoritative capacity breach is a failed invariant caught by content validation or stress testing, not a runtime balancing tool.
- Capacity, high-water mark, queue depth, reuse count, and rejected visual requests are diagnostic metrics.

Initial capacities are derived from the encounter schedule plus a documented margin in each specialized system rather than selected as arbitrary powers of two.

## Headless execution

The simulation host supports:

- step one tick;
- advance until a tick, terminal result, event, or predicate;
- inject scripted commands and random sources;
- load a compiled content registry and generated map manifest;
- emit canonical summaries and checksums; and
- run faster than real time without presentation.

Headless execution is the basis for unit tests, map audits, weapon benchmarks, economy simulations, balance sweeps, and regression reproduction. It is a library/tool mode, not a second gameplay implementation.

## Verification

- Property tests exercise entity allocation/reuse, nonnegative resources, modifier invalidation, capacity queues, and transaction atomicity.
- Golden fixtures cover phase order, final-tick death versus extraction, simultaneous damage, branch commitment, relic replacement/sale, boss loot, and pause boundaries.
- Long-run soak tests execute repeated 35-minute simulations and assert invariant stability and bounded memory.
- Differential tests compare displayed derived values with measured weapon and mining behavior.
- Every content definition used in simulation has at least one load/validation test and one behavior registration test.

## Related documents

- [Runtime Architecture](./10-runtime-architecture.md)
- [World Geometry, Navigation, and Spatial Queries](./21-world-geometry-navigation-and-spatial-queries.md)
- [Combat and Weapon Runtime](./22-combat-and-weapon-runtime.md)
- [Encounter Director and Enemy Runtime](./23-encounter-director-and-enemy-runtime.md)
- [Mining, Fabrication, and Progression Runtime](./24-mining-fabrication-and-progression-runtime.md)
- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md)
