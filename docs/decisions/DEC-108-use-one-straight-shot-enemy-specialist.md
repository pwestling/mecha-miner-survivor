---
doc_id: DEC-108
title: Use One Straight-Shot Enemy Specialist
status: accepted
authoritative: false
validation: prototype-and-playtest
---

# DEC-108 — Use One Straight-Shot Enemy Specialist

## Decision

Exactly one of the initial standard map's ten ordinary enemy identities is a behavioral specialist. The other nine are pure pursuit-and-contact enemies apart from their use in authored formations.

The specialist retains ordinary pursuit and contact damage but periodically performs one additional action:

1. present a conspicuous audiovisual firing windup;
2. aim toward the mech;
3. fire one straight projectile; and
4. resume its normal firing interval while the projectile travels without homing.

The shot does not lead the mech, split, explode, create a lasting hazard, apply a status effect, or change direction after release. Exact windup, cadence, projectile speed, lifetime, damage, collision size, and whether pursuit slows during the windup remain tuning work.

The specialist first enters the ordinary wave schedule during the 14:00–21:00 mid-run phase. Its exact debut minute and later combinations remain open.

Cinderglass resonance increases this projectile's damage by 20% while the firing enemy is inside the field, following the existing rule that field effects depend on the enemy's location. Eidolon Coral resonance increases its firing cadence. Other applicable outgoing-damage and movement modifiers work normally.

## Status

Accepted as the complete initial ordinary-specialist behavior budget: one straight-shot ranged identity and no second specialist. DEC-119 later names it Needler and supplies its initial presentation, statistics, timing, and minute-16 debut; exact values remain tuning work.

## Rationale

One readable projectile threat adds positional texture during mining without turning the ordinary roster into a set of tactical combatants. It also ensures Cinderglass's projectile-damage resonance has a recurring ordinary-enemy interaction rather than depending entirely on bosses.

Restricting the attack to one telegraphed non-homing shot makes its danger visible amid dense hordes and leaves dodging as the response. A second specialist is unnecessary for the first standard map because formations, elites, bosses, resonance fields, and mining commitments already create additional pressure.

## Consequences

- One of the ten ordinary identity concepts must have a clear ranged silhouette and firing tell.
- The standard map has nine pure pursuer identities and one pursuer-plus-projectile identity.
- Projectile contrast must remain readable against player weapons, resource indicators, and geode fields.
- Cinderglass and Eidolon Coral field tests must include this specialist.
- Adding a self-destructing, area-denial, support, or second ranged ordinary enemy requires a later explicit decision.

## Specification links

- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Mining and Extraction](../40-mining-and-extraction.md)
- [Specialized Resource Identities](../61-specialized-resource-identities.md)
- [Open Questions](../open-questions.md)

## Supersedes / superseded by

Narrows the zero-to-two specialist allowance in [DEC-105](./DEC-105-use-a-simple-pursuer-first-enemy-roster.md) and [DEC-106](./DEC-106-use-ten-ordinary-enemy-identities.md) to exactly one. It relies on the explicit field modifiers in [DEC-078](./DEC-078-geode-resonance-fields.md).
