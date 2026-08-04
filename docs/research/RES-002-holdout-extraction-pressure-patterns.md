---
doc_id: RES-002
title: Holdout and Extraction Pressure Patterns
status: complete
authoritative: false
---

# RES-002 — Holdout and Extraction Pressure Patterns

## Research question

What proven mechanics make remaining near a valuable objective dangerous, and which patterns could add distinct pressure to rare mining without obscuring this game's simple proximity rule?

## Retrieval date

2026-08-01.

## Sources

- [Teleporter — *Risk of Rain 2 Wiki*](https://riskofrain2.wiki.gg/wiki/Teleporter) — community-maintained description of a holdout zone, charge behavior, boss event, and optional risk/reward modifiers.
- [Focused Convergence — *Risk of Rain 2 Wiki*](https://riskofrain2.wiki.gg/wiki/Focused_Convergence) — community-maintained description of a direct trade between faster completion and a smaller holdout area.
- [Excavation — *Warframe Wiki*](https://wiki.warframe.com/w/Excavation) — official wiki description of defend-to-extract objectives, continuous and completion rewards, power requirements, and parallel excavators.
- [*Deep Rock Galactic: Rogue Core* official site](https://www.deeprockgalactic.com/roguecore) — developer-controlled description of time pressure, temporary mined progression, permanent unlocks, and side-objective opportunity cost.

## Observed patterns

### Area occupancy plus spawned combat event

In *Risk of Rain 2*, activating a teleporter creates a visible holdout area, spawns a boss and additional monsters, and charges only while players occupy the area. A separate risk/reward option can make charging faster while dramatically shrinking the valid zone. Other modifiers increase event difficulty in exchange for additional item rewards.

This pattern makes the objective itself an explicit combat event. Its advantages are clarity and tunability; its risk is making every rare point feel like the same arena encounter.

### Continuous base payout plus completion bonus

In *Warframe* Excavation, an active excavator produces a basic resource for each unit of completed work, while successfully finishing the excavation grants an additional reward. The objective must be defended and periodically supplied with energy carried by designated enemies. Carrying a cell also constrains weapon use.

This separates partial value from a completion prize in a way similar to this game's decided common-versus-rare payout profiles. It also shows how an extra task can pull attention away from pure defense, though adding fuel collection to this game would be a new mechanic rather than a necessary consequence.

### Opportunity cost across the larger run

The official *Rogue Core* description makes time a managed resource: mission timers and rising threat mean players may not finish every side objective. Mined material produces temporary mission upgrades, while mission accomplishments unlock persistent options between runs.

This pattern creates risk without changing enemy statistics at the node itself. A rare mining stop can be dangerous simply because it consumes time or route flexibility under a global escalation curve.

## Candidate rare-resource pressure patterns

These are proposals, not canonical design.

| Pattern | Player-visible pressure | Strength | Main risk |
| --- | --- | --- | --- |
| Threat beacon | Rare mining visibly attracts a curated wave, elite, or boss | Clear and easy to understand | Can make rare nodes repetitive combat arenas |
| Constricting field | The valid mining area shrinks or changes shape as completion approaches | Directly intensifies the decided positioning challenge | Can feel unfair if geometry or telegraphing is poor |
| Local hazard | The resource emits timed radiation, heat, gravity, or terrain pulses | Gives resource types distinct identities | Visual overload or unavoidable damage |
| Combat interference | Mining temporarily modifies weapon cadence, shields, cooling, sensors, or another capability | Reinforces the mech-and-mining theme | Removing core agency can feel frustrating |
| Global opportunity cost | Rare mining consumes meaningful time while the overall horde threat continues rising | Preserves simple local rules | Weak if the run has no strong global clock or escalation |
| Optional overcharge | The player can complete the normal extraction or remain for a harder bonus stage | Makes “push your luck” explicit and voluntary | Adds another reward layer and more UI complexity |

## Inferences for this game

- The existing combination of limited dodge space, rapid exit decay, and completion-only rare rewards already creates meaningful risk. An additional hazard is not required merely to justify the term “push-your-luck.”
- If rare points need more identity, a visible threat beacon is the clearest first prototype because it intensifies the game's existing combat vocabulary without disabling movement or weapons.
- A constricting field is the most direct extension of the positional premise, but it should probably trade constraint for a benefit such as faster completion rather than functioning as an unexplained punishment.
- Combat interference is thematically rich but higher-risk. Slowing the mech or disabling automatic weapons attacks the same agency the player needs to solve the mining challenge.
- Global opportunity cost depends on the still-open run timer and escalation rules, so it cannot yet carry the design by itself.

## Resulting decision

The threat-beacon pattern was selected for rare-resource mining in [DEC-004](../decisions/DEC-004-mining-retention-threat-and-banking.md). Exact wave, elite, escalation, and cleanup behavior remains design work; the other patterns in this note remain non-canonical comparisons.

## Research limitations

These comparators have different camera, control, multiplayer, and reward structures. Their mechanics demonstrate pressure patterns, not expected balance values. Community-maintained wiki details may change and are subordinate to direct testing and this game's own design goals.

## Resulting links

- [Mining and Extraction](../40-mining-and-extraction.md)
- [DEC-003 — Use automatic proximity mining with resource-specific payouts](../decisions/DEC-003-proximity-mining-and-resource-payouts.md)
- [DEC-004 — Use finite common deposits, rare threat beacons, and survival-gated banking](../decisions/DEC-004-mining-retention-threat-and-banking.md)
- [OQ-004 — How does a mining point behave?](../open-questions.md#oq-004--how-does-a-mining-point-behave)
- [OQ-005 — What makes mining a push-your-luck system?](../open-questions.md#oq-005--what-makes-mining-a-push-your-luck-system)
