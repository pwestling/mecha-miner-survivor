---
doc_id: GDD-WEAPON-INDEX
title: Weapon Specification Index
status: active
authoritative: true
---

# Weapon Specification Index

## Purpose

This is the fast lookup layer for the accepted 15-weapon catalog. The authoritative behavior summaries live under each linked heading in [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md), while exact first-playable values and edge rules live in the [Initial Weapon Numeric Catalog](../71-initial-weapon-numeric-catalog.md). Accepted decision records preserve rationale and tuning boundaries.

All names are presentation-working names. Every listed base behavior, three-stat bundle, branch category, branch effect, native funding orientation, and off-color assignment is accepted for prototyping and playtesting. Exact values and explicitly deferred edge rules remain adjustable.

Resource codes are compact authoring labels: `A` Asterite, `B` Barysteel, `C` Cinderglass, `D` Driftmetal, `E` Eidolon Coral, and `F` Flux Amber. Player-facing recipes use the full material names and icons defined in [Specialized Resource Identities](../61-specialized-resource-identities.md).

## Catalog lookup

| ID | Weapon | Recipe | Common-ore stats | Amplification | Functional variant | Off-color conversion | Initial signature |
| --- | --- | --- | --- | --- | --- | --- | --- |
| W-AB | [Rail Lance](../66-weapon-catalog-and-resource-graph.md#w-ab--rail-lance) | `A + B` | Damage; width; range | `A` Unbounded Bore | `B` Fracture Lance | `C` Kinetic Capacitor | Yes |
| W-AC | [Cluster Mortar](../66-weapon-catalog-and-resource-graph.md#w-ac--cluster-mortar) | `A + C` | Damage; blast radius; attack rate | `C` Saturation Cascade | `A` Interdiction Payload | `F` Danger-Close Protocol | No |
| W-AD | [Gravity Projector](../66-weapon-catalog-and-resource-graph.md#w-ad--gravity-projector) | `A + D` | Damage; field radius; field duration | `A` Echo Well | `D` Gravity Slingshot | `B` Singularity Forge | Yes |
| W-AE | [Attack Drones](../66-weapon-catalog-and-resource-graph.md#w-ae--attack-drones) | `A + E` | Damage; attack rate; operational range | `E` Replicator Swarm | `A` Wolfpack Protocol | `D` Containment Lattice | No |
| W-AF | [Tracking Laser](../66-weapon-catalog-and-resource-graph.md#w-af--tracking-laser) | `A + F` | Damage; range; focus rate | `A` Coherence Memory | `F` Target Designator | `B` Cutting Vector | No |
| W-BC | [Pulse Repeater](../66-weapon-catalog-and-resource-graph.md#w-bc--pulse-repeater) | `B + C` | Damage; attack rate; range | `B` Zero-Lag Emitter | `C` Suppressive Sequencer | `E` Broadside Oscillator | Yes |
| W-BD | [Mine Layer](../66-weapon-catalog-and-resource-graph.md#w-bd--mine-layer) | `B + D` | Damage; blast radius; active-mine capacity | `B` Seed Charges | `D` Selective Detonators | `F` Hunter Mines | No |
| W-BE | [Sentry Pod](../66-weapon-catalog-and-resource-graph.md#w-be--sentry-pod) | `B + E` | Damage; attack rate; range | `E` Battery Overclock | `B` Guardian Firmware | `A` Forward Bastion | No |
| W-BF | [Orbital Cutters](../66-weapon-catalog-and-resource-graph.md#w-bf--orbital-cutters) | `B + F` | Damage; cutter size; orbit speed | `F` Kinetic Flywheel | `B` Deflection Ring | `E` Tethered Reaper | No |
| W-CD | [Arc Emitter](../66-weapon-catalog-and-resource-graph.md#w-cd--arc-emitter) | `C + D` | Damage; attack rate; chain range | `C` Total Conduction | `D` Disruption Current | `B` Ball-Lightning Projector | No |
| W-CE | [Reactor Pulse](../66-weapon-catalog-and-resource-graph.md#w-ce--reactor-pulse) | `C + E` | Damage; pulse radius; pulse rate | `E` Critical-Mass Cycle | `C` Kinetic Vent | `F` Supernova Cycle | Yes |
| W-CF | [Wake Projector](../66-weapon-catalog-and-resource-graph.md#w-cf--wake-projector) | `C + F` | Damage; trail width; trail duration | `C` Runaway Wake | `F` Carrier Ignition | `D` Circuit Closure | No |
| W-DE | [Scatter Array](../66-weapon-catalog-and-resource-graph.md#w-de--scatter-array) | `D + E` | Damage; attack rate; range | `D` Saturation Choke | `E` Concussive Fan | `C` Focal Array | No |
| W-DF | [Ram Field](../66-weapon-catalog-and-resource-graph.md#w-df--ram-field) | `D + F` | Damage; ram width; knockback distance | `D` Momentum Cascade | `F` Impact Transfer | `A` Siege Anchor | Yes |
| W-EF | [Missile Rack](../66-weapon-catalog-and-resource-graph.md#w-ef--missile-rack) | `E + F` | Damage; blast radius; launch rate | `F` MIRV Saturation | `E` Guardian Reserve | `D` Spiral Barrage | Yes |

## Assignment balance

Every resource is native to five weapons. Amplification funding is distributed `A:3`, `B:2`, `C:3`, `D:2`, `E:3`, `F:2`; functional funding is the complement `A:2`, `B:3`, `C:2`, `D:3`, `E:2`, `F:3`. No resource exclusively signals one branch category.

Off-color conversions retain the accepted distribution: `A`, `C`, and `E` fund two each; `B`, `D`, and `F` fund three each.

## Design state

- **Accepted:** base targeting and delivery, fixed versus ore-upgradeable properties, all three common-ore stats, all branches, resource mappings, signature membership, and owner self-damage exceptions explicitly stated in the catalog.
- **Accepted first-playable numeric baseline:** exact per-rank gains, base combat values, branch multipliers, durations, caps, and weapon-specific edge rules in [DEC-125](../decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md). These remain playtest-tunable, not underspecified.
- **Still tuning or systemic work:** global control-resistance behavior, final visual tolerances, system-wide terrain rules, measured benchmark results, and any catalog revisions supported by playtesting. The shared common-ore price curve is fixed globally by DEC-085, and the common DPS, throughput, benchmark, and value-band method is fixed by [DEC-124](../decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md).
- **Separate content work:** final presentation names, effects and audio, mech identities and traits, resource identities, enemy roster interactions, and prototype order.

Numeric weapon entries use the [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md). The [base-weapon](../data/weapon-base-balance.csv) and [branch](../data/weapon-branch-balance.csv) CSVs mirror their authoritative Markdown values for comparison and tooling.
