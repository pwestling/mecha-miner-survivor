---
doc_id: TDR-003
title: Require 60 FPS on Steam Deck
status: accepted
authoritative: false
validation: target-device-performance-benchmark
---

# TDR-003 — Require 60 FPS on Steam Deck

## Decision

Treat 60 rendered frames per second at 1280×800 on a retail Steam Deck as an initial-release correctness requirement, including the representative maximum-pressure gameplay benchmark. Require the same 60 FPS target at 1920×1080 on the eventual Windows minimum-spec machine.

A separately labeled 30 FPS frame-cap option may support hardware below minimum specification. It is not the target-device default and cannot be used to pass Steam Deck acceptance.

## Context

The player continuously reads gaps, contact footprints, mining-zone boundaries, telegraphed projectiles, and boss attacks while hundreds of enemies and automatic weapon effects move. Stable motion and input response materially affect fairness. Steam Deck is already a first-class target rather than a compatibility afterthought.

## Performance contract

- Target frame duration is 16.67 milliseconds.
- A ten-minute warmed representative benchmark must keep both CPU and GPU 95th-percentile frame time at or below 16.67 milliseconds.
- 99th-percentile frame time must remain at or below 22 milliseconds, excluding explicitly tagged loading transitions and shader-cache creation outside active play.
- No repeatable active-play stall may exceed 50 milliseconds.
- Simulation must never lower enemy counts, delay authored attacks, shorten effects, change mining behavior, or alter results to preserve frame rate.
- Presentation may use preapproved quality tiers that preserve gameplay readability and collision correspondence.

The canonical stress scene contains minute-34 baseline pressure, the allowed event overflow, a 75% Hyper Gold response at its capacity boundary, four mining/radar categories in view where feasible, representative weapon and relic VFX, dropped pickups, the HUD, and at least one active boss. Separate pathological tests cover all four surviving bosses and every hard population cap.

## Consequences

- Full Godot nodes, animated scene trees, navigation agents, or physics bodies per ordinary enemy are prohibited unless benchmarks overturn the architecture and a TDR records the evidence.
- CPU, GPU, allocation, memory, asset, draw-call, particle, audio-voice, and loading budgets are mandatory.
- Steam Deck captures are required throughout development, not only before release.
- Visual fidelity scales before gameplay density or simulation cadence.
- A fixed 60 Hz simulation remains the baseline. A later 30 Hz simulation with interpolation would require evidence that it preserves collision and telegraph feel and a superseding TDR.

## Reversal signals

Reconsider the target only if representative builds demonstrate that 60 FPS is infeasible after measured optimization and reasonable presentation scaling, or if the product direction explicitly chooses a different responsiveness standard. Development schedule pressure alone is not a reversal signal.

## Specification links

- [Technical Foundation](../00-technical-foundation.md)
- [Runtime Architecture](../10-runtime-architecture.md)
- [Standard Wave and Beacon Schedule](../../32-standard-wave-and-beacon-schedule.md)
- [DEC-113 — Target Windows PC and Steam Deck First](../../decisions/DEC-113-target-windows-pc-and-steam-deck-first.md)
- [DEC-114 — Use Native Low-Poly 3D Gameplay](../../decisions/DEC-114-use-native-low-poly-3d-gameplay.md)
