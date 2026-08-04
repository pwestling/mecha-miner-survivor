---
doc_id: DEC-080
title: Use Twenty-Second Geodes and Forty-Five-Second Super-Resource Sites
status: accepted
authoritative: false
---

# DEC-080 — Use Twenty-Second Geodes and Forty-Five-Second Super-Resource Sites

## Decision

A material geode requires 20 seconds of uninterrupted forward extraction to open from zero progress. A super-resource site requires 45 seconds to complete from zero.

**Super resource** was the working name for the rare resource used in cross-run progression. [DEC-091](./DEC-091-name-and-quantify-hyper-gold.md) later names it Hyper Gold and fixes three 100-unit sites per map. Its 45-second site uses the existing progress-threshold threat beacon and remains unsecured until mission extraction. Material geodes retain their non-escalating resonance fields and award run-local specialized materials.

Both point classes use the standard 0.5-second exit grace and four-times progress decay. Consequently, progress equivalent to a full geode bar would decay in five seconds outside after grace, while the equivalent super-resource progress would decay in 11.25 seconds; in play, completion triggers before a literally full bar can decay.

At uninterrupted pace, the current specialized-material build budgets consume:

| Build state | Geodes | Extraction time | Share of 35-minute run |
| --- | ---: | ---: | ---: |
| Three additional weapons | 6 | 2:00 | 5.7% |
| Four weapons fully branched | 14 | 4:40 | 13.3% |
| Full build with radar | 16 | 5:20 | 15.2% |
| Full build without radar | 17 | 5:40 | 16.2% |

Travel, combat, retreats, failed attempts, ore seams, fabrication pauses, and super-resource sites add to those minimums.

## Status

Accepted as initial playtest timing.

## Rationale

Twenty seconds gives a geode resonance field enough time to create a meaningful local encounter without consuming the excessive share of the newly extended run that a 30-second geode would require. Forty-five seconds makes a cross-run reward a substantially larger commitment whose escalating beacon has time to express all four response stages.

## Consequences

- Geode duration is uniform across all six materials; their resonance modifiers create the variation.
- Super-resource beacon thresholds occur at 11.25, 22.5, and 33.75 seconds of uninterrupted forward progress, although threshold events remain one-time when progress later decays.
- UI must distinguish a 20-second run-local geode commitment from a 45-second cross-run super-resource commitment.
- DEC-083 fixes the seam denomination, DEC-086 fixes the geode jackpot, DEC-090 fixes ore-seam map counts, and DEC-091 fixes Hyper Gold site counts and payouts.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Run Structure and Timing](../20-run-structure-and-timing.md)
- [DEC-077 — Use ore seams and completion-only material geodes](./DEC-077-ore-seams-and-material-geodes.md)
- [DEC-078 — Give material geodes thematic enemy resonance fields](./DEC-078-geode-resonance-fields.md)

## Supersedes / superseded by

Resolves the geode-completion-time variable left open by DEC-077 and fixes the initial extraction duration for the sites governed by DEC-032. DEC-091 later replaces the working name with Hyper Gold and fixes site count and payout.
