---
doc_id: DEC-123
title: Replenish Destructible Rocks Around the Player
status: accepted
authoritative: false
validation: playtest
---

# DEC-123 — Replenish Destructible Rocks Around the Player

> **Completion note:** DEC-126 supplies the 100-Hull durability, 0.80M non-solid footprint, 18–45M spawn annulus, 0.25M pickup radius, and 25-Hull repair value left open here. This decision's population and probability rules remain unchanged.

## Decision

Standard mode maintains a dynamic population of up to **16 active destructible rocks** rather than placing only 16 one-shot rocks for the whole run.

The map begins with 16 valid rocks around the deployment region but outside the initial camera view. During active simulation, the game makes one replenishment attempt every second. Each attempt has a fixed **10% success chance**. A successful attempt creates one rock at a valid offscreen location near the player:

- If fewer than 16 rocks currently exist, the new rock fills an empty population slot.
- At the 16-rock cap, the new rock replaces the farthest eligible offscreen rock so recovery opportunities continue to follow exploration.
- A rock never appears or disappears inside the visible camera area.
- A rock cannot spawn inside a mining or relic interaction zone, on blocking terrain, in a required connector, or where its presentation would hide another important object.
- If no valid offscreen position and eligible replacement exist, the attempt produces nothing and leaves every current rock unchanged.

Destroying a rock has a fixed **20% chance** to drop one health pack and an 80% chance to drop nothing. The chance is independent for every destroyed rock and is not modified by PowerUps, resources, progression, the current Hull value, or hidden difficulty adjustment.

Health packs continue to persist until collected or run end, restore Hull immediately on contact without overhealing, and are consumed even at full Hull. Dropped health packs do not count against the 16-rock cap and are never removed when rocks recycle.

Rock durability, health restored per pack, valid spawn annulus, pickup radius, and audiovisual presentation remain playtest or presentation variables. The one-second attempt interval, 10% rock-spawn chance, 16-rock active cap, and 20% health-pack chance are the initial numeric baseline.

## Status

Accepted as the standard replenishing recovery-object baseline. Supersedes DEC-122's interpretation of 16 as a whole-map lifetime count.

## Rationale

At a 20% health-pack chance, 16 lifetime rocks would average only 3.2 packs across a 35-minute run and could strand a player after early exploration. Replenishment makes healing an ongoing but uncertain opportunity that the player can seek by moving and breaking nearby rocks.

The reference pattern in *Vampire Survivors* continually attempts to spawn offscreen breakable light sources while maintaining a small active cap. The adopted values create approximately one successful rock spawn every ten active-simulation seconds before cap and visibility constraints. If all spawned rocks were destroyed, the theoretical expectation would be one health pack per 50 active seconds; actual recovery is lower because the player will not find and break every rock.

Keeping drop chance fixed avoids a hidden comeback system. A damaged player benefits by actively hunting rocks, not because the game secretly changes the odds.

## Consequences

- Sixteen becomes a simultaneous population cap, not a total standard-map content count.
- Destructible rocks are dynamic support objects rather than persistent authored map locations and are excluded from the geological survey, resource radar, exploration completion, and fixed-site seed validation.
- The minimap does not persistently record rocks or health packs. Their recovery value is local and visual.
- Survey Optics and Survey Aperture may reveal rocks through their normal world-discovery radius but do not create durable map markers.
- Tests must measure rocks destroyed, packs produced, packs collected, wasted full-Hull pickups, time since last visible rock, and deaths occurring after long healing droughts.

## Specification links

- [Core Game Loop](../10-core-game-loop.md)
- [Combat, Weapons, Movement, and Camera](../30-combat-weapons-movement-camera.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Standard Map Generation Contract](../51-standard-map-generation-contract.md)
- [RES-001 — Vampire Survivors reference mechanics](../research/RES-001-vampire-survivors-reference.md)

## Supersedes / superseded by

Supersedes only DEC-122's fixed 16-per-map interpretation. It preserves destructible rocks as the sole ordinary health-pack source, the absence of temporary-effect pickups, contact collection, and the no-resource-drop rule. It also revises DEC-115's fixed field-object count into a dynamic active-population cap without changing any mining, relic, Hyper Gold, or landmark placement rule.
