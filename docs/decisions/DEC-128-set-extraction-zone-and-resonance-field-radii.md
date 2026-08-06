---
doc_id: DEC-128
title: Set the Extraction-Zone and Resonance-Field Radii
status: accepted
authoritative: false
---

# DEC-128 — Set the Extraction-Zone and Resonance-Field Radii

## Decision

Every mining point's circular extraction zone has a **3.0M radius**, a 6.0M diameter. Every unopened material geode's resonance field has a **6.0M radius**.

The zone radius applies to all four accepted mining-point classes: standard ore seams, rich ore seams, material geodes, and Hyper Gold sites. Whether any class should later use a different size is a separate question and stays open.

Both values are accepted rather than provisional. Dependent work implements them directly and does not owe a proof gate before doing so. They remain revisable as ordinary playtest tuning under the [Combat and Economy Balance Framework](../70-combat-and-economy-balance-framework.md), on the same terms as every other accepted numeric baseline.

The two radii satisfy every constraint the specification already places on them:

- **Primary connector width.** The [Standard Map Generation Contract](../51-standard-map-generation-contract.md#redundant-major-routes) requires primary connectors never narrower than one mining-zone diameter, targeting one and a half. Connectors are therefore never narrower than 6.0M and target 9.0M, against a 720–900M traversable map diameter targeting 810M. Connector width is derived from the unmodified baseline zone, so Extraction Tether and Tether Amplifier never change it.
- **Mining-point clearance.** A mining point's cleared radius becomes 4.0M: the 3.0M circle plus the one-mech-width maneuvering band, at the 1.0M mech collision diameter.
- **Resonance field larger than the maximum expanded zone.** The [Utility Catalog](../68-utility-catalog.md#utl-d2--extraction-tether) requires a material geode's resonance field to stay larger than the maximum expanded extraction zone. Extraction Tether at rank 3 contributes +25% and `PU-E02` Tether Amplifier at rank 5 contributes +15%; the two stack additively on the 3.0M base for a 4.2M maximum. The 6.0M field exceeds it, with margin at every intermediate rank combination.
- **Circling room.** A major region must hold a complete mining circle without forcing the mech into a connector. A 6.0M circle inside a region whose shortest cross-map route targets 810M leaves the region free to supply the surrounding open ground the horde schedule needs.

These are the values OQ-004 and OQ-005 listed as still to define.

## Status

Accepted. The extraction-zone and resonance-field radii are no longer missing design inputs.

## Rationale

No extraction-zone radius existed anywhere in the specification, and it was the single largest remaining gap: several accepted rules are expressed as functions of it and could not be evaluated at all. Primary connector width is defined in mining-zone diameters. Deployment clearance is defined in mining-zone diameters. The Extraction Tether utility and the Tether Amplifier PowerUp are percentages of it. Map validation cannot check zone or resonance-field non-overlap without both numbers.

A 3.0M radius makes the zone six mech widths across and two seconds of base movement across at 3.0M/s. That is small enough that standing in it is a real positional commitment — the player cannot simply outrun contact inside the circle — and large enough to permit the circling and weaving that [DEC-031](./DEC-031-circular-mining-zone-and-fast-decay.md) chose a circle to support. It also keeps the derived quantities sane: 6.0M and 9.0M connectors read as broad routes rather than corridors at an 810M map diameter, and a 4.0M cleared radius is a modest local demand on obstacle placement at 8–12% coverage.

A 6.0M resonance field is twice the zone radius and four times its area. It satisfies the utility catalog's larger-than-maximum-expansion constraint with margin, so no Tether or Amplifier combination can ever shrink the geode's danger role to nothing. Doubling rather than a smaller increment also means the player meets the field noticeably before reaching the circle, which is the pressure the field exists to create: the commitment starts before extraction does.

Fixing both numbers now is preferable to leaving them provisional. Every consumer needs a concrete value, and a provisional label would only add a proof gate to work whose correctness does not depend on the exact figure — the constraint relationships, not the specific radii, are what dependent code enforces.

## Consequences

- OQ-004's zone radius and OQ-005's resonance-field radius are resolved. Depleted-point presentation, forced-movement and exact-boundary edge cases, and independent tuning of the six resonance modifiers remain open in those questions.
- `MAP-003` spatial embedding and connectors, `MAP-006` the site constraint solver, and `MIN-001` mining site state are no longer blocked on a missing radius. Each has a concrete number to embed, validate, and store.
- Map validation can now evaluate connector width, deployment clearance, mining-point clearance, extraction-circle non-overlap, and resonance-field non-overlap numerically rather than symbolically.
- Resonance-field separation consumes noticeably more room than extraction-circle separation, because the field is twice the radius. Geode distribution rules are the binding placement constraint on a standard map, not seam distribution.
- Content authoring gains two absolute geometry values; quantities defined relative to them stay relative in source and are derived by the compiler.
- Resource-specific zone sizes, if playtesting later wants them, are an addition to this decision rather than a reversal of it.

## Specification links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [Core Game Loop](../10-core-game-loop.md)
- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [Utility Catalog](../68-utility-catalog.md)
- [Permanent PowerUp Catalog](../62-permanent-powerup-catalog.md)
- [Player Survivability and Damage Baseline](../72-player-survivability-and-damage-baseline.md)
- [Glossary](../glossary.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)
- [OQ-005 — What makes mining a push-your-luck system?](../open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system)
- [DEC-031 — Use visible circular mining zones with fast exit decay](./DEC-031-circular-mining-zone-and-fast-decay.md)
- [DEC-078 — Give material geodes thematic enemy resonance fields](./DEC-078-geode-resonance-fields.md)
- [DEC-115 — Adopt the standard map-generation contract](./DEC-115-adopt-standard-map-generation-contract.md)
- [DEC-126 — Adopt the initial player survivability baseline](./DEC-126-adopt-the-initial-player-survivability-baseline.md)

## Supersedes / superseded by

This supplies the two numeric values left open by DEC-031, DEC-078, DEC-115, and DEC-119. It preserves their circular zone, 0.5-second grace, four-times decay, larger-than-zone resonance field, 20% material modifiers, non-overlap rules, and connector-width relationship without changing any of them.

It changes no extraction duration, payout, decay rate, utility or PowerUp percentage, map count, or separation time.
