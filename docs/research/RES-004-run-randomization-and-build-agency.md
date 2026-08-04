---
doc_id: RES-004
title: Run Randomization and Build Agency
status: complete
authoritative: false
---

# RES-004 — Run Randomization and Build Agency

## Research question

How can fabrication preserve intentional player choice while preventing every run from converging on a favorite or solved weapon loadout?

## Retrieval date

2026-08-01.

## Sources

- [Blueprints — *Against the Storm* Official Wiki](https://wiki.hoodedhorse.com/Against_the_Storm/Blueprint) — describes curated blueprint pools that provide early necessities before later specialized buildings, with random selections from staged pools.
- [Buildings — *Against the Storm* Official Wiki](https://wiki.hoodedhorse.com/Against_the_Storm/Buildings) — describes run-local building availability after a blueprint is chosen.
- [Card rewards — *Slay the Spire* Wiki](https://slaythespire.wiki.gg/wiki/Card) — mechanics reference for building from a larger unlocked pool through small random rewards and optional skipping.
- [Fated Persuasion — *Hades* Wiki](https://hadeswiki.com/wiki/mirror-talents/fated-persuasion-green) — community reference for strictly limited per-run rerolls of randomized boon choices.
- [Seal — *Vampire Survivors* Wiki](https://vampire-survivors.fandom.com/wiki/Seal) — community reference for removing items from the pre-run choice pool, illustrating how accumulated control can make specific builds increasingly forceable.

## Design problem

The game needs two properties that pull against each other:

- **Agency:** Mining and crafting should let the player intentionally build weapons rather than accept arbitrary XP or chest rewards.
- **Adaptation:** A run should not always permit the same known-best or favorite loadout with the same upgrade path.

Pure catalog access maximizes agency but is solvable. A random shop that refreshes whenever fabrication opens creates variation but turns unrestricted menu access into free rerolls. Generous sealing, banishing, or repeated rerolls can gradually restore the deterministic catalog problem.

## Relevant patterns

### Curated pools instead of unrestricted random draws

*Against the Storm* groups blueprints into pools so early selections can cover foundational needs before more specialized options enter. This demonstrates that “random” does not need to mean every unlocked item has equal eligibility at every moment. Coverage guarantees can protect run viability without prescribing the build.

### Small drafts with permission to decline

*Slay the Spire* commonly asks the player to choose from a small random subset rather than a full catalog, and card rewards can be skipped. The run is shaped by accumulated decisions under uncertainty. The important property is that the player adapts to a fixed offer rather than repeatedly refreshing until a desired card appears.

### Strictly limited correction tools

*Hades* provides a limited number of per-attempt rerolls. This can rescue an unusable offer, but the finite supply forces the player to decide when correction is worth spending. Such tools become fishing mechanisms when they are abundant enough to force a favorite build.

### Pre-run pool pruning

*Vampire Survivors* allows unlocked systems to remove items from future selection pools. This gives long-term control but contributes to the user's identified problem: mature accounts can increasingly force a narrow preferred build.

## Adopted initial model

Start with fixed unlocked blueprints, recipes, effects, and prices. Randomize the map's specialized-resource profile instead: which specialized resources exist and their broad abundance. Reveal that profile immediately after deployment so the player can form an early-run build plan during the one-minute minor-wave orientation phase, but leave exact deposit locations unknown so exploration remains necessary.

This is the cleanest first test because:

- Randomness changes what can be economically built, not what a known recipe unexpectedly produces.
- Players can make informed early-run plans before committing to a resource route.
- Exact node locations and quantities still require in-run adaptation.
- Unlimited fabrication access cannot reroll geology or prices.
- The distinctive mining system, rather than a generic reward draft, drives run variety.

The model requires careful recipe design. Each resource profile must support several coherent weapon roles, and specialized resources should participate in multiple recipes so one geological roll does not map to one obvious build. Basic ore should provide a reliable early progression floor even when specialized resources have not yet been found.

The user accepted this as the initial model in [DEC-008](../decisions/DEC-008-fixed-blueprints-randomized-resource-profiles.md), then moved the survey from pre-deployment planning into the active opening in [DEC-015](../decisions/DEC-015-in-run-opening-geological-survey.md). The main residual risk is that players solve one best build for each resource-profile combination. Track repeated build rates in playtesting before adding another random layer.

### Later refinement: gate base weapons as well as upgrades

On 2026-08-02, the design moved beyond treating geology as only an economic influence. [DEC-034](../decisions/DEC-034-gate-base-weapons-by-resource-profile.md) establishes that base weapons require specialized resources. [DEC-036](../decisions/DEC-036-six-color-signature-aware-resource-profiles.md) fixes six resource families, four present per run, and a complete 15-weapon pair graph with exactly six supported recipes per profile. Recipes and outcomes remain fixed, so the player adapts to a stable run-specific feasibility set rather than a rerolling offer screen.

[RES-006](./RES-006-resource-color-weapon-graph.md) develops the leading combinatorial model: resource types as vertices, two-resource weapon recipes as edges, and a possible Steiner-triple-system assignment for each weapon's third branch resource.

## Additional layers if resource ecology is insufficient

### Fixed run-specific fabrication manifest

At run start, select a limited subset of unlocked weapon frames and upgrade families for that run. This **fabrication manifest** remains fixed and visible. Opening or closing the unlimited fabrication menu never redraws it.

The manifest should contain more viable options than the player can equip, but substantially fewer than the permanent unlocked catalog. Exact counts depend on weapon-slot and content decisions.

Use curated pools and guarantees rather than an unrestricted uniform draw:

- Include functional coverage such as focused damage, area control, and a survivability or control option.
- Ensure the starting equipment has at least one valid improvement route.
- Avoid duplicate options that do not create a real choice.
- Ensure recipes can be supported by resources that exist in the level.
- Keep all manifest entries visible so the player can plan rather than guess.

### Random threat context

In addition to resource ecology, vary deposit locations, rare-resource opportunities, enemy composition, and threat-beacon encounters. The known resource profile defines what is broadly practical; the discovered map changes what is economical and urgent.

This layer is especially important to the game's identity. A resource-rich route can make an otherwise secondary weapon practical, while enemy or terrain pressures can change which roles are valuable. Randomizing the problem reduces reliance on arbitrary random rewards.

Safeguards should prevent resource luck from invalidating every manifest option. The level can guarantee minimum access to essential materials while allowing meaningful abundance and scarcity above that floor.

### Stable drafts at major weapon branches

Minor numeric or predictable upgrades can remain deterministic. When a weapon reaches a major transformation point, generate a small set of compatible branches from its run-eligible module pool.

- Show exact effects and costs.
- Keep the branch offer fixed if the player closes and reopens fabrication.
- Allow postponing the choice.
- Make every offered branch viable and meaningfully distinct.
- Lock or materially alter incompatible branches after selection so the choice shapes the run.

This places randomness at high-impact divergence points without making every purchase a slot-machine pull.

## Player-control guardrails

The following preserve agency without making a favorite build guaranteed:

- Let a chosen mech or starting kit provide one stable strategic anchor, while the remaining manifest varies.
- If correction is necessary, use a small run-limited redraw resource rather than free or escalating-cost rerolls available forever.
- Prefer choosing a broad doctrine or tag bias over guaranteeing one exact weapon.
- Allow declining or postponing a branch rather than forcing a harmful choice.
- Do not let reopening fabrication, saving and loading, or delaying a choice change the seeded options.

## Anti-frustration requirements

- A run must never lack a viable early damage path.
- Resource requirements and manifest generation must be compatible.
- New permanent unlocks must not simply dilute the pool with unusable options.
- A manifest should contain tactical contrast, not several cosmetic versions of the same role.
- The game should disclose enough of the manifest and known resource ecology for informed routing.
- Losing because the player adapted poorly is acceptable; losing because the run offered no coherent path is not.

## Alternatives considered

| Model | Agency | Variety | Fishing risk | Fit for this game |
| --- | --- | --- | --- | --- |
| Full deterministic catalog | Very high | Low after mastery | None | Conflicts with replayability goal |
| Fresh random shop on every open | Low to medium | Superficially high | Extreme with unlimited menu access | Poor |
| Random three-choice offer after every craft | Medium | High | High if rerolls exist | Too close to renamed XP choices |
| Fixed catalog plus randomized resource ecology | High | Medium to high | None | Adopted initial model; strongest thematic fit |
| Fixed manifest only | High within bounds | High | Low | Strong, but context may still repeat |
| Fixed manifest plus ecology plus major branch drafts | High within bounds | Very high | Low | Fallback if the simpler model is insufficient |

## Playtest questions

- How often does the same weapon or weapon combination appear in successful runs?
- How often do identical resource profiles produce identical builds?
- Do players change their intended build after seeing map information or only after finding deposits?
- How often do players choose the same weapon when it is available?
- What fraction of manifest entries are never considered viable?
- Can players infer the best build immediately from the manifest, or does exploration meaningfully update the plan?
- How often does resource scarcity make a desired recipe impossible versus merely expensive?
- Do major branch drafts produce adaptation or obvious best choices?
- Does enlarging the persistent unlock catalog improve variety or dilute build coherence?

## Resulting links

- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [DEC-008 — Use fixed fabrication rules with surveyed randomized resource profiles](../decisions/DEC-008-fixed-blueprints-randomized-resource-profiles.md)
- [DEC-015 — Reveal randomized geology during the active opening](../decisions/DEC-015-in-run-opening-geological-survey.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
- [OQ-018 — How does each run randomize build availability without enabling fishing?](../open-questions.md#oq-018--how-does-each-run-randomize-build-availability-without-enabling-fishing)
- [DEC-034 — Gate base weapons through the specialized-resource profile](../decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [RES-006 — Resource-color graph for weapon availability](./RES-006-resource-color-weapon-graph.md)
