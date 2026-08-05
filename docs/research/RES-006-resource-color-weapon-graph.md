---
doc_id: RES-006
title: Resource-Color Graph for Weapon Availability
status: complete
authoritative: false
---

# RES-006 — Resource-Color Graph for Weapon Availability

## Research question

Can a well-understood graph or combinatorial-design structure select a random subset of specialized resource types, control how much of the base-weapon catalog is craftable, and assign two native plus one off-color major branch to every weapon in a balanced way? Approximately 50% availability was an initial estimate, not a requirement.

This note retains “color” and `A`–`F` as compact mathematical terminology. [DEC-076](../decisions/DEC-076-specialized-resource-identities.md) later maps those codes to the player-facing Asterite, Barysteel, Cinderglass, Driftmetal, Eidolon Coral, and Flux Amber identities without changing the graph.

## Retrieval date

2026-08-02.

## Sources

- [Extensions of Steiner Triple Systems](https://doi.org/10.1002/jcd.21964) — primary research paper defining a Steiner triple system as triples in which every resource pair occurs together exactly once and stating the standard existence condition.
- [Orthogonal representations of Steiner triple system incidence graphs](https://arxiv.org/abs/1708.07741) — primary research paper identifying the Fano plane as the unique Steiner triple system of order seven.
- [Total colorings of k-regular graphs of girths 2k and k](https://doi.org/10.61091/jcmcc127-07) — primary research paper giving an explicit cyclic representation of the seven triples in `STS(7)`.

## Graph formulation

Let each specialized resource color be a vertex. Let each base weapon be an undirected edge connecting the resource types required by its recipe. A run selects `X` of the `Y` resource vertices. The theoretically craftable base weapons are precisely the edges induced by that selected vertex set.

If every unordered pair defines one weapon, the persistent arsenal is the complete graph `K_Y`:

- Total base weapons: `C(Y, 2)`.
- Base weapons available when `X` colors are present: `C(X, 2)`.
- Available fraction: `X(X - 1) / [Y(Y - 1)]`.

This count is exact for every resource profile of the same size because every selected `X`-vertex set induces a complete graph `K_X`. This arithmetic is a direct derivation, not a claim taken from the cited sources.

## Candidate parameter sets

| Total colors `Y` | Colors per run `X` | Total pair-weapons | Weapons per run | Available fraction | Comment |
| ---: | ---: | ---: | ---: | ---: | --- |
| 4 | 3 | 6 | 3 | 50.0% | Exact half, but little geological variety and only three choices |
| 5 | 3 | 10 | 3 | 30.0% | Very restrictive; filling three empty weapon slots would consume every available option |
| 5 | 4 | 10 | 6 | 60.0% | Modest catalog and six choices, but only one color is missing per run |
| 6 | 3 | 15 | 3 | 20.0% | Too restrictive unless the intended build begins nearly complete |
| 6 | 4 | 15 | 6 | 40.0% | Strong candidate: six choices for three slots and two colors missing per run |
| 6 | 5 | 15 | 10 | 66.7% | Many choices, but likely makes preferred-loadout repetition easier |
| 7 | 5 | 21 | 10 | 47.6% | Closest to the initial half estimate and supports the Fano-plane construction |
| 8 | 6 | 28 | 15 | 53.6% | Near half, but a much larger arsenal |

No complete pair graph using exactly five or six total colors produces exactly 50% availability. This is not a defect: the useful design target is enough choices to improvise while withholding enough of the catalog to disrupt a solved favorite build.

## Detailed five- and six-color comparison

For a weapon whose recipe colors are already present, assume its third major branch is assigned to one of the other `Y - 2` colors in a balanced way. That off-color resource is present with probability:

```text
(X - 2) / (Y - 2)
```

For the originally proposed utility purchasable with either of two assigned colors, its availability is the `A OR B` fraction derived in [Utility recipes on the same resource graph](#utility-recipes-on-the-same-resource-graph--superseded-proposal). DEC-109 later rejects this model because its availability is too broad.

| Total / present | Distinct resource profiles | Pair-weapons total | Pair-weapons available | Catalog available | Off-color branch available for a craftable weapon | `A OR B` utility available |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `5 / 3` | 10 | 10 | 3 | 30.0% | 33.3% | 90.0% |
| `5 / 4` | 5 | 10 | 6 | 60.0% | 66.7% | 100.0% |
| `6 / 3` | 20 | 15 | 3 | 20.0% | 25.0% | 80.0% |
| `6 / 4` | 15 | 15 | 6 | 40.0% | 50.0% | 93.3% |
| `6 / 5` | 6 | 15 | 10 | 66.7% | 75.0% | 100.0% |
| `7 / 5` comparison | 21 | 21 | 10 | 47.6% | 60.0% | 95.2% |

These columns expose different kinds of run variation:

- `5 / 4` keeps the long-term content requirement low, but every run is missing only one color. All two-option utilities are available and two-thirds of off-color branches remain accessible, so profiles may feel less disruptive than the 60% weapon figure suggests.
- `6 / 4` offers the same six base-weapon choices while excluding two of six colors. It makes 40% of weapons, half of assigned off-color branches, and approximately 93.3% of two-option utilities available. This became the accepted weapon-profile model, although DEC-109 later replaced the overly broad utility rule.
- `6 / 5` presents 10 of 15 weapons and never excludes an `A OR B` utility. It is generous enough that players may often reconstruct a favorite loadout.
- `7 / 5` provides elegant balance, but increases the complete arsenal from 15 to 21 weapons merely to move base availability from 40% to 47.6%.

The branch and utility percentages are exact probabilities for any individual balanced assignment under uniformly selected profiles. Unlike the complete pair-weapon count, the number actually available in a particular run may vary unless the label assignment or utility graph has additional combinatorial structure.

Five colors can divide 10 third-color edge labels evenly: each resource can label two weapons. Six colors cannot divide 15 labels evenly, so the best possible whole-number balance gives three colors two off-color assignments each and three colors three assignments each. This small asymmetry can rotate between maps or be assigned to compensate for the actual strength and scarcity of each resource.

## Signature-aware six-color profiles

The accepted model selects four of six colors but requires at least two of the chosen mech's three signature branch colors. Let those three colors be the signature set and the other three be the non-signature set.

There are `C(6,4) = 15` unconstrained profiles. A profile violates the guarantee only when it contains one signature color and all three non-signature colors, which can happen in `C(3,1) × C(3,3) = 3` ways. Therefore 12 profiles remain valid for a given signature weapon.

The valid-profile distribution is:

- Exactly two signature colors: `C(3,2) × C(3,2) = 9` profiles, or 75%.
- All three signature colors: `C(3,3) × C(3,1) = 3` profiles, or 25%.
- Any particular signature color is present in 9 of 12 profiles, or 75%.
- Any particular non-signature color is present in 7 of 12 profiles, or approximately 58.3%.
- Both base-recipe colors of the signature weapon are present in `C(4,2) = 6` of the 12 profiles, or 50%.

The last point matters because the already-equipped signature belongs to the normal pair catalog and duplicate weapons are forbidden. Every profile still supports six pair recipes, but when the signature recipe itself is among them, only five of those recipes represent different additional weapons. When its recipe is absent, all six supported recipes are different from the equipped signature.

This conditioning intentionally makes the selected mech affect profile probabilities without disclosing the result before deployment. Validation must therefore enumerate the 12 legal profiles separately for every distinct signature three-color set; analyzing only the 15 unconstrained profiles is insufficient. Equal tactical-role coverage is not required, but impossible, predictably abandoned, and pervasively biased combinations must be identified.

## Third-branch assignment through a Steiner triple system

The two recipe colors on a weapon edge can naturally fund two mutually exclusive major branches. Assigning a unique third, non-endpoint color to every pair is an edge-labeling problem. A Steiner triple system solves a stronger balanced version: every pair belongs to exactly one three-color block.

The cited existence condition says a Steiner triple system of order `Y` exists exactly when `Y` is congruent to `1` or `3` modulo `6`. Therefore neither five nor six colors supports a perfect Steiner triple system. Their edges can still receive deliberately balanced off-color labels, but they cannot reproduce every symmetry of the seven-color construction.

For seven resource colors, the unique system is the Fano plane. One cyclic labeling uses these seven triples:

```text
124  235  346  457  561  672  713
```

For weapon edge `{1,2}`, the unique containing triple is `{1,2,4}`, so colors `1` and `2` are its native branch resources and color `4` is its off-color branch resource. The same rule assigns one third color to every one of the 21 weapon pairs.

The design is balanced:

- Every color is a base-recipe endpoint for six weapons.
- Every color is the off-color branch resource for three weapons.
- No weapon's off-color branch duplicates either base-recipe resource.
- Every pair receives exactly one off-color assignment.

## Seven-color comparison: behavior when five colors are selected

A run contains exactly `C(5,2) = 10` of the `C(7,2) = 21` base weapons.

For every available weapon, its two native branch resources are necessarily present because those resources were required to make the weapon available. The off-color branch is present only when the third point in its Fano triple is also one of the five selected resources.

Every five-point subset of the Fano plane contains exactly two complete triples. Each complete triple contributes three weapon edges whose off-color is present. Therefore, under this construction:

- All 10 available weapons have their two native branch paths available in principle.
- Exactly 6 of those weapons also have their off-color third path available.
- The remaining 4 weapons cannot take their third path during that run.

This constant `6/10` split is a useful property of the seven-color design, not merely an average.

The explicit seven triples were enumerated locally across all 21 five-color profiles. The check confirmed 21 unique resource pairs, 10 available pair-weapons in every profile, and exactly 6 available off-color branches in every profile.

## Content-scope alternatives

### Use all 21 edges as the long-term arsenal

This preserves exact counts and perfect balance. It may be appropriate as a mature content target even if an initial prototype implements only a subset.

### Use a curated subgraph

A smaller arsenal can use only selected edges. If the run still chooses five of seven colors uniformly, every individual two-color weapon remains available with probability `10/21`, so approximately 47.6% of a balanced roster is available in expectation.

The count will no longer be identical for every profile. A near-regular graph and automated enumeration of all 21 possible five-color profiles can minimize variation and verify role coverage.

### Prototype with four colors and three present

This produces exactly three of six weapons per run. It is cheap to build and proves the induced-subgraph idea, but it provides less resource-profile variety and lacks the especially balanced seven-color third-branch structure.

### Use six colors with four present

This produces exactly six of 15 pair-weapons. It has no perfect Steiner triple system, so assign third colors with a balanced edge-labeling and enumerate all 15 profiles to measure branch availability and tactical-role coverage. It offers substantially less content scope than the 21-weapon Fano model while giving players twice as many candidates as the three empty weapon slots they normally need to fill.

## Utility recipes on the same resource graph — superseded proposal

Utilities can use an edge with the opposite availability rule from weapons:

- A weapon on edge `{A,B}` requires `A AND B`.
- A utility on edge `{A,B}` can be purchased with `A OR B` through two fixed alternative recipes.

For `Y` total colors and `X` selected colors, an `A OR B` utility is unavailable only if both endpoints are absent. Its availability fraction is:

```text
1 - (Y - X)(Y - X - 1) / [Y(Y - 1)]
```

With six colors and four present, a particular utility is available in `14/15`, or approximately 93.3%, of profiles. With seven colors and five present, it is available in `20/21`, or approximately 95.2%, of profiles. That broad availability is a feature if the goal is to prevent utilities from being welded to the same weapon pair. The run-specific decision becomes which of its alternative resource costs is present or economical, rather than whether the utility usually exists at all.

A balanced six-color example assigns six non-radar utilities to the edges of a six-color cycle. Every color then pays for exactly two utilities. With exactly two colors omitted:

- If the omitted colors are adjacent on the cycle, the utility on that edge is unavailable and the other five remain available. This occurs for 6 of the 15 resource profiles.
- If the omitted colors are not adjacent, all six remain available. This occurs for 9 of the 15 profiles.
- The common-ore radar remains available in every profile independently of this graph.

This proposal was not adopted. DEC-109 uses the stricter recipe rule anticipated here: twelve non-radar utilities, two assigned to each material, with one assigned-material recipe apiece. A given utility is therefore available whenever its one material is among the four selected colors: `4/6`, or two thirds, of unconditioned profiles. Every profile contains four materials and therefore offers exactly eight of the twelve utilities, plus the common-ore radar.

## Design risks

- Twenty-one distinct base weapons may be beyond the desired content scope.
- A technically available weapon may still be practically unavailable if its two deposits are too scarce or distant.
- Pair recipes may become hard to remember without strong color, icon, name, and recipe UI.
- Color cannot be the only distinguishing channel; each resource needs a non-color identity for accessibility.
- If weapon roles cluster around particular vertices, some profiles may become impossible or predictably restart-worthy despite having the correct weapon count.
- The third branch should not always dominate the two native branches merely because it is less frequently available.
- Signature weapons need an explicit relationship to the graph: included edges, separate guaranteed frames, or another rule.
- Broad `OR` utility availability may create too little utility variation if changing the payable resource is not strategically meaningful.

## Accepted outcome

The accepted numeric model is `6 colors / 4 per run / complete pair recipes`. It gives exactly six of 15 supported base recipes per run without making most of the persistent arsenal accessible. It also withholds two colors and makes an assigned off-color branch available half the time for an otherwise craftable weapon before signature conditioning. Under later DEC-109, those four colors also expose exactly eight of twelve single-material non-radar utilities.

Signature weapons belong inside the pair graph. Profile generation is conditioned to include at least two of the selected signature weapon's three branch colors, and duplicate weapons are forbidden. One recipe color funds amplification, the other funds functional variation, and the assigned off-color always funds the playstyle conversion. The weapon catalog and near-balanced off-color edge-labeling were subsequently completed. Utility access is fixed at eight non-radar choices per four-color profile, and DEC-116 subsequently completes the twelve concepts and two-per-material assignments in the [Utility Catalog](../68-utility-catalog.md).

## Resulting links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [Weapon Stat and Branch Upgrades](../65-weapon-stat-and-branch-upgrades.md)
- [DEC-034 — Gate base weapons through the specialized-resource profile](../decisions/DEC-034-gate-base-weapons-by-resource-profile.md)
- [DEC-035 — Integrate utilities without fixed weapon pairing](../decisions/DEC-035-integrate-utilities-without-fixed-weapon-pairing.md)
- [DEC-036 — Use six-color signature-aware resource profiles](../decisions/DEC-036-six-color-signature-aware-resource-profiles.md)
- [DEC-109 — Use single-material utilities with three ore ranks](../decisions/DEC-109-use-single-material-utilities-with-three-ore-ranks.md)
- [DEC-116 — Accept the initial utility catalog](../decisions/DEC-116-accept-initial-utility-catalog.md)
- [DEC-076 — Give the six specialized resources strong non-exclusive identities](../decisions/DEC-076-specialized-resource-identities.md)
- [DEC-037 — Use unique weapons and soft profile balance](../decisions/DEC-037-unique-weapons-and-soft-profile-balance.md)
- [Weapon Catalog and Resource Graph](../66-weapon-catalog-and-resource-graph.md)
- [OQ-013 — What resource types exist, and what does each purchase?](../open-questions.md#oq-013--what-resource-types-exist-and-what-does-each-purchase)
- [OQ-014 — How are weapons crafted and upgraded?](../open-questions.md#oq-014--how-are-weapons-crafted-and-upgraded)
