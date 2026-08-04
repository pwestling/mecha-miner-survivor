---
doc_id: GDD-DECISIONS
title: Decision Log
status: active
authoritative: true
---

# Decision Log

Decision records preserve why consequential gameplay choices were made. The gameplay specification remains the canonical statement of current behavior; this log provides history and rationale.

## Decision record format

Each `DEC-###-short-name.md` file contains:

1. **Decision** — the chosen direction in one concise statement.
2. **Status** — `accepted`, `superseded`, or `reversed`.
3. **Context** — the player-experience problem and known constraints.
4. **Considered options** — credible alternatives, including their tradeoffs.
5. **Rationale** — why the decision best supports the game.
6. **Consequences** — player-visible gains, costs, risks, and follow-on work.
7. **Specification links** — every canonical document affected.
8. **Supersedes / superseded by** — links when applicable.

## Decisions

- [DEC-001 — Use a *Vampire Survivors*-inspired combat reference](./DEC-001-vampire-survivors-combat-reference.md) — narrow inheritance boundary superseded by DEC-096; explicit combat reference retained
- [DEC-002 — Replace XP and treasure chests with mining and crafting](./DEC-002-mining-replaces-xp-and-chests.md) — accepted
- [DEC-003 — Use automatic proximity mining with resource-specific payouts](./DEC-003-proximity-mining-and-resource-payouts.md) — accepted
- [DEC-004 — Use finite common deposits, rare threat beacons, and survival-gated banking](./DEC-004-mining-retention-threat-and-banking.md) — accepted
- [DEC-005 — Use timed survival, interval bosses, and mission extraction](./DEC-005-timed-survival-and-mission-extraction.md) — accepted
- [DEC-006 — Pause combat for crafting and discard unspent ordinary resources](./DEC-006-paused-crafting-and-run-resource-reset.md) — accepted
- [DEC-007 — Allow unlimited on-demand fabrication access](./DEC-007-unlimited-on-demand-fabrication.md) — accepted for playtesting
- [DEC-008 — Use fixed fabrication rules with surveyed randomized resource profiles](./DEC-008-fixed-blueprints-randomized-resource-profiles.md) — accepted
- [DEC-009 — Provide an ore-powered directional resource radar](./DEC-009-ore-powered-directional-resource-radar.md) — accepted
- [DEC-010 — Make one timed deployment one complete run](./DEC-010-one-deployment-per-run.md) — accepted
- [DEC-011 — Start with a 25-minute standard run timer](./DEC-011-twenty-five-minute-run-timer.md) — superseded by DEC-079
- [DEC-012 — Schedule four bosses before the final horde crescendo](./DEC-012-four-boss-five-minute-cadence.md) — cadence superseded by DEC-079
- [DEC-013 — Keep bosses active and allow scheduled overlap](./DEC-013-persistent-overlapping-bosses.md) — accepted for playtesting
- [DEC-014 — Use a selectable mech roster with signature starting weapons](./DEC-014-selectable-mechs-and-signature-weapons.md) — accepted
- [DEC-015 — Reveal randomized geology during the active opening](./DEC-015-in-run-opening-geological-survey.md) — accepted
- [DEC-016 — Use a one-minute minor-wave orientation phase](./DEC-016-one-minute-opening-orientation.md) — accepted for playtesting
- [DEC-017 — Keep the survey reviewable through fabrication](./DEC-017-persistent-survey-review.md) — accepted
- [DEC-018 — Use four weapon slots and three utility slots](./DEC-018-four-weapons-three-utilities.md) — accepted
- [DEC-019 — Use movement-only baseline combat controls](./DEC-019-movement-only-combat-controls.md) — accepted
- [DEC-020 — Keep ordinary crafting materials exclusive to mining](./DEC-020-mining-exclusive-ordinary-materials.md) — source exclusivity superseded by DEC-029; enemy-drop rule completed by DEC-102
- [DEC-021 — Use a wide fully top-down camera](./DEC-021-wide-fully-top-down-camera.md) — accepted
- [DEC-022 — Randomize all player-relevant map locations](./DEC-022-randomized-map-locations.md) — accepted
- [DEC-023 — Use per-stat ore upgrades and specialized-resource weapon branches](./DEC-023-weapon-stat-and-branch-upgrades.md) — accepted
- [DEC-024 — Use a large finite map](./DEC-024-large-finite-map.md) — accepted
- [DEC-025 — Use uncapped linear stat ranks with nonlinear prices](./DEC-025-uncapped-linear-stat-ranks.md) — accepted
- [DEC-026 — Recombine recurring authored map chunks](./DEC-026-recombined-authored-map-chunks.md) — accepted
- [DEC-027 — Make major weapon branches mutually exclusive](./DEC-027-mutually-exclusive-weapon-branches.md) — accepted
- [DEC-028 — Use one exploration-found mech relic](./DEC-028-one-exploration-found-mech-relic.md) — accepted
- [DEC-029 — Pause and resolve relic discoveries through installation or common-ore sale](./DEC-029-pause-and-resolve-relic-discoveries.md) — accepted
- [DEC-030 — Place three automatic relic caches on each standard map](./DEC-030-three-automatic-relic-caches.md) — accepted
- [DEC-031 — Use visible circular mining zones with fast exit decay](./DEC-031-circular-mining-zone-and-fast-decay.md) — accepted for playtesting
- [DEC-032 — Escalate rare threat beacons at progress thresholds](./DEC-032-progress-threshold-threat-beacons.md) — accepted for playtesting
- [DEC-033 — Use a fogged exploration map with persistent discovery markers](./DEC-033-fogged-exploration-map.md) — accepted
- [DEC-034 — Gate base weapons through the specialized-resource profile](./DEC-034-gate-base-weapons-by-resource-profile.md) — accepted
- [DEC-035 — Integrate utilities without fixed weapon pairing](./DEC-035-integrate-utilities-without-fixed-weapon-pairing.md) — accepted direction
- [DEC-036 — Use six-color signature-aware resource profiles](./DEC-036-six-color-signature-aware-resource-profiles.md) — accepted
- [DEC-037 — Use unique weapons and soft profile balance](./DEC-037-unique-weapons-and-soft-profile-balance.md) — accepted
- [DEC-038 — Use a broad automatic-weapon taxonomy](./DEC-038-broad-automatic-weapon-taxonomy.md) — accepted
- [DEC-039 — Target a six-mech initial roster](./DEC-039-six-mech-initial-roster.md) — accepted initial content target
- [DEC-040 — Use a three-level weapon-branch transformation gradient](./DEC-040-three-branch-transformation-gradient.md) — accepted
- [DEC-041 — Use an equal-tier base-weapon catalog](./DEC-041-equal-tier-base-weapon-catalog.md) — accepted
- [DEC-042 — Use movement-derived persistent mech facing](./DEC-042-movement-derived-persistent-facing.md) — accepted
- [DEC-043 — Assign the fifteen base weapons to the resource graph](./DEC-043-fifteen-weapon-graph-assignment.md) — accepted catalog structure
- [DEC-044 — Use immediate permanent branch commitment](./DEC-044-immediate-permanent-branch-commitment.md) — accepted
- [DEC-045 — Define the first signature-weapon amplifications](./DEC-045-first-signature-amplification-branches.md) — accepted behavior; tuning open
- [DEC-046 — Define the Rail Lance branch set](./DEC-046-rail-lance-branch-set.md) — accepted behavior; tuning open
- [DEC-047 — Limit weapons to three common-ore stats](./DEC-047-three-stat-weapon-bundles.md) — accepted default
- [DEC-048 — Give Pulse Repeater a suppressive functional branch](./DEC-048-pulse-repeater-suppressive-sequencer.md) — accepted behavior; targeting details and tuning open
- [DEC-049 — Convert Pulse Repeater into a broadside weapon](./DEC-049-pulse-repeater-broadside-oscillator.md) — accepted behavior; geometry and tuning open
- [DEC-050 — Give Pulse Repeater damage, rate, and range stats](./DEC-050-pulse-repeater-stat-bundle.md) — accepted bundle; numeric tuning open
- [DEC-051 — Give Gravity Projector a two-stage slingshot branch](./DEC-051-gravity-projector-slingshot.md) — accepted behavior; edge rules and tuning open
- [DEC-052 — Convert Gravity Projector into a Singularity Forge](./DEC-052-gravity-projector-singularity-forge.md) — accepted behavior; scaling and tuning open
- [DEC-053 — Give Gravity Projector damage, radius, and duration stats](./DEC-053-gravity-projector-stat-bundle.md) — accepted bundle; scheduling and tuning open
- [DEC-054 — Give Cluster Mortar delayed, committed area targeting](./DEC-054-cluster-mortar-base-behavior.md) — accepted base behavior; completed by DEC-055 through DEC-060
- [DEC-055 — Amplify Cluster Mortar with Saturation Cascade](./DEC-055-cluster-mortar-saturation-cascade.md) — accepted `C` branch; tuning open
- [DEC-056 — Give Cluster Mortar an Interdiction Payload](./DEC-056-cluster-mortar-interdiction-payload.md) — accepted `A` branch; tuning open
- [DEC-057 — Convert Cluster Mortar to Danger-Close Protocol](./DEC-057-cluster-mortar-danger-close-protocol.md) — accepted behavior; blast advantages and tuning open
- [DEC-058 — Make Danger-Close Protocol harmless to its owner](./DEC-058-danger-close-no-self-damage.md) — accepted branch rule
- [DEC-059 — Give Cluster Mortar damage, radius, and rate stats](./DEC-059-cluster-mortar-stat-bundle.md) — accepted bundle; numeric tuning open
- [DEC-060 — Assign native branch funding for catalog balance](./DEC-060-balance-native-branch-funding.md) — accepted method and Cluster Mortar mapping
- [DEC-061 — Use an autonomous, indestructible attack-drone squadron](./DEC-061-attack-drones-base-behavior.md) — accepted base behavior; completed by DEC-062 through DEC-064
- [DEC-062 — Amplify Attack Drones with a Replicator Swarm](./DEC-062-attack-drones-replicator-swarm.md) — accepted `E` branch; tuning open
- [DEC-063 — Give Attack Drones a Wolfpack Protocol](./DEC-063-attack-drones-wolfpack-protocol.md) — accepted `A` branch and native mapping; tuning open
- [DEC-064 — Complete Attack Drones with Containment Lattice and three stats](./DEC-064-complete-attack-drones.md) — accepted complete high-level design; tuning open
- [DEC-065 — Complete the Tracking Laser weapon](./DEC-065-complete-tracking-laser.md) — accepted complete high-level design; tuning open
- [DEC-066 — Complete the Mine Layer weapon](./DEC-066-complete-mine-layer.md) — accepted complete high-level design; tuning open
- [DEC-067 — Complete the Sentry Pod weapon](./DEC-067-complete-sentry-pod.md) — accepted complete high-level design; tuning open
- [DEC-068 — Complete the Orbital Cutters weapon](./DEC-068-complete-orbital-cutters.md) — accepted complete high-level design; tuning open
- [DEC-069 — Complete the Arc Emitter weapon](./DEC-069-complete-arc-emitter.md) — accepted complete high-level design; tuning open
- [DEC-070 — Complete the Reactor Pulse weapon](./DEC-070-complete-reactor-pulse.md) — accepted complete high-level design; tuning open
- [DEC-071 — Complete the Wake Projector weapon](./DEC-071-complete-wake-projector.md) — accepted complete high-level design; tuning open
- [DEC-072 — Complete the Scatter Array weapon](./DEC-072-complete-scatter-array.md) — accepted complete high-level design; tuning open
- [DEC-073 — Complete the Ram Field weapon](./DEC-073-complete-ram-field.md) — accepted complete high-level design; tuning open
- [DEC-074 — Complete the Missile Rack weapon](./DEC-074-complete-missile-rack.md) — accepted complete high-level design; tuning open
- [DEC-075 — Accept the complete initial weapon catalog for playtesting](./DEC-075-accept-complete-initial-weapon-catalog.md) — accepted catalog baseline; playtesting open
- [DEC-076 — Give the six specialized resources strong non-exclusive identities](./DEC-076-specialized-resource-identities.md) — accepted names, associations, and recognition channels
- [DEC-077 — Use ore seams and completion-only material geodes](./DEC-077-ore-seams-and-material-geodes.md) — accepted payout models, geode counts, and initial unit costs
- [DEC-078 — Give material geodes thematic enemy resonance fields](./DEC-078-geode-resonance-fields.md) — accepted initial 20% local enemy modifiers
- [DEC-079 — Use a 35-minute run with a seven-minute boss cycle](./DEC-079-thirty-five-minute-seven-minute-boss-cycle.md) — accepted standard timing baseline; playtesting open
- [DEC-080 — Use 20-second geodes and 45-second super-resource sites](./DEC-080-twenty-second-geodes-forty-five-second-super-resources.md) — accepted extraction durations; playtesting open
- [DEC-081 — Place eight to ten geodes for each present material](./DEC-081-eight-to-ten-geodes-per-material.md) — accepted material-supply baseline; playtesting open
- [DEC-082 — Deplete both ore-seam classes in fifteen seconds](./DEC-082-fifteen-second-ore-seams.md) — accepted ore-seam cadence and capacity; playtesting open
- [DEC-083 — Set the common-ore installment unit to ten](./DEC-083-set-common-ore-unit-to-ten.md) — accepted initial currency scale; playtesting open
- [DEC-084 — Price stat upgrades by total weapon upgrade depth](./DEC-084-price-stat-upgrades-by-weapon-depth.md) — accepted shared pricing structure; completed by DEC-085
- [DEC-085 — Use a triangular shared-depth price curve](./DEC-085-use-triangular-shared-depth-prices.md) — accepted initial numeric curve; playtesting open
- [DEC-086 — Award fifty common ore from each material geode](./DEC-086-fifty-ore-geode-jackpot.md) — accepted initial jackpot; playtesting open
- [DEC-087 — Price the resource radar at three hundred ore](./DEC-087-price-resource-radar-at-three-hundred-ore.md) — accepted initial price; playtesting open
- [DEC-088 — Show continuous multi-material radar directions](./DEC-088-show-continuous-multi-material-radar-directions.md) — target maximum and exclusions superseded by DEC-089
- [DEC-089 — Expand the radar to all mining categories](./DEC-089-expand-radar-to-all-mining-categories.md) — accepted seven-category active-play coverage; layout completed by DEC-127
- [DEC-090 — Place twenty standard and eight rich ore seams](./DEC-090-place-twenty-standard-and-eight-rich-ore-seams.md) — accepted initial map counts; placement and economy tuning open
- [DEC-091 — Name and quantify Hyper Gold](./DEC-091-name-and-quantify-hyper-gold.md) — accepted name, three sites per map, and 100-unit site payout
- [DEC-092 — Use Hyper Gold for power and option unlocks](./DEC-092-use-hyper-gold-for-power-and-option-unlocks.md) — accepted dual metaprogression structure; initial catalogs later supplied by DEC-120 and DEC-121
- [DEC-093 — Make permanent power account-wide](./DEC-093-make-permanent-power-account-wide.md) — accepted global PowerUp scope; catalog later supplied by DEC-120
- [DEC-094 — Allow free PowerUp refunds](./DEC-094-allow-free-powerup-refunds.md) — accepted full lossless between-run respec
- [DEC-095 — Include mining and economy PowerUps](./DEC-095-include-mining-and-economy-powerups.md) — accepted four-domain catalog breadth; individual tracks later supplied by DEC-120
- [DEC-096 — Use Vampire Survivors as the default precedent](./DEC-096-use-vampire-survivors-as-the-default-precedent.md) — accepted bounded inheritance rule
- [DEC-097 — Inherit direct movement, collision, and camera](./DEC-097-inherit-direct-movement-collision-and-camera.md) — accepted baseline feel; numeric tuning open
- [DEC-098 — Use minute-authored horde waves](./DEC-098-use-minute-authored-horde-waves.md) — accepted horde director and boss anti-avoidance model
- [DEC-099 — Use single-player pause and results flow](./DEC-099-use-single-player-pause-and-results-flow.md) — accepted standard run-flow baseline; screen organization completed by DEC-127
- [DEC-100 — Commit installed weapons and utilities](./DEC-100-commit-installed-weapons-and-utilities.md) — accepted irreversible run loadout lifecycle
- [DEC-101 — Target an approachable escalating standard difficulty](./DEC-101-target-an-approachable-escalating-standard-difficulty.md) — accepted standard difficulty intent
- [DEC-102 — Separate enemy kills from field pickups](./DEC-102-separate-enemy-kills-from-field-pickups.md) — accepted ordinary-enemy no-drop rule; boss rewards supplied by DEC-111 and recovery objects finalized by DEC-122/123
- [DEC-103 — Use Hull Integrity and contact-collected field pickups](./DEC-103-use-hull-integrity-and-contact-collected-field-pickups.md) — accepted survivability and contact-collection baseline; temporary pickups removed and rocks replenished by DEC-122/123
- [DEC-104 — Show a compact survivor-like active HUD](./DEC-104-show-a-compact-survivor-like-active-hud.md) — accepted persistent information baseline; composition completed by DEC-127
- [DEC-105 — Use a simple pursuer-first enemy roster](./DEC-105-use-a-simple-pursuer-first-enemy-roster.md) — accepted initial enemy-complexity constraint
- [DEC-106 — Use ten ordinary enemy identities](./DEC-106-use-ten-ordinary-enemy-identities.md) — accepted standard-map roster size and simultaneous-variety limit
- [DEC-107 — Use fixed ordinary enemy stat profiles](./DEC-107-use-fixed-ordinary-enemy-stat-profiles.md) — accepted visible composition-driven escalation rule
- [DEC-108 — Use one straight-shot enemy specialist](./DEC-108-use-one-straight-shot-enemy-specialist.md) — accepted sole ordinary specialist behavior
- [DEC-109 — Use single-material utilities with three ore ranks](./DEC-109-use-single-material-utilities-with-three-ore-ranks.md) — accepted utility availability and upgrade structure
- [DEC-110 — Use an open multi-route map topology](./DEC-110-use-open-multi-route-map-topology.md) — accepted standard-map traversal invariant
- [DEC-111 — Make bosses explode into collectible resources](./DEC-111-make-bosses-explode-into-resources.md) — accepted physical boss-loot reward model
- [DEC-112 — Bound permanent power below run-build power](./DEC-112-bound-permanent-power-below-run-build-power.md) — accepted metaprogression strength envelope
- [DEC-113 — Target Windows PC and Steam Deck first](./DEC-113-target-windows-pc-and-steam-deck-first.md) — accepted initial platform baseline; default mappings and responsive flow completed by DEC-127
- [DEC-114 — Use native low-poly 3D gameplay](./DEC-114-use-native-low-poly-3d-gameplay.md) — accepted visual-medium target; representative art and performance validation required
- [DEC-115 — Adopt the standard map-generation contract](./DEC-115-adopt-standard-map-generation-contract.md) — accepted first-pass scale, topology, placement, fairness, and seed-validity baseline
- [DEC-116 — Accept the initial utility catalog](./DEC-116-accept-initial-utility-catalog.md) — accepted twelve-item catalog and six-item first-playable subset; numeric tuning open
- [DEC-117 — Accept the initial mech catalog](./DEC-117-accept-initial-mech-catalog.md) — accepted six-mech identities, traits, silhouettes, fresh-profile availability, and selection rules; numeric tuning open
- [DEC-118 — Accept the initial relic catalog](./DEC-118-accept-initial-relic-catalog.md) — accepted ten transformative relics; pool membership supplied by DEC-121 and cache selection, sale, and presentation by DEC-127; effect tuning open
- [DEC-119 — Accept the initial alien encounter baseline](./DEC-119-accept-initial-alien-encounter-baseline.md) — accepted ten ordinary aliens, four bosses, full 35-minute schedule, and Hyper Gold beacon responses; tuning open
- [DEC-120 — Accept the permanent PowerUp catalog](./DEC-120-accept-permanent-powerup-catalog.md) — accepted thirteen account-wide tracks, fixed prices and caps, active ranks, stacking, and 9,450-Hyper-Gold total; tuning open
- [DEC-121 — Accept the initial permanent option-unlock catalog](./DEC-121-accept-initial-option-unlock-catalog.md) — accepted fresh-profile baseline, six nonrefundable purchases, no prerequisites or disabling, and 2,150-Hyper-Gold total
- [DEC-122 — Use destructible rocks as the health-pack source](./DEC-122-use-destructible-rocks-for-health-packs.md) — superseded fixed-count baseline; rock identity and health-pack-only catalog retained by DEC-123
- [DEC-123 — Replenish destructible rocks around the player](./DEC-123-replenish-destructible-rocks-around-the-player.md) — accepted 16-rock active cap, 10%-per-second offscreen replenishment, and fixed 20% health-pack chance
- [DEC-124 — Adopt a multi-metric weapon balance framework](./DEC-124-adopt-a-multi-metric-weapon-balance-framework.md) — accepted DPS and throughput anchors, six shared benchmarks, rank and branch value bands, and no initial follow-on branches
- [DEC-125 — Adopt the initial numerical weapon catalog and feasible boss Hull](./DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md) — accepted exact values for 15 weapons and 45 branches, machine-readable mirrors, legal reference build, and revised 6,000 / 14,000 / 30,000 / 45,000 boss Hull
- [DEC-126 — Adopt the initial player survivability baseline](./DEC-126-adopt-the-initial-player-survivability-baseline.md) — accepted 3.0M/s movement, collision footprints, damage order and two-hit fairness invariant, 25-Hull packs, 100-Hull rocks, control rules, and failure-rate bands
- [DEC-127 — Adopt the first-playable interface and screen flow](./DEC-127-adopt-the-first-playable-interface-and-screen-flow.md) — accepted active HUD, paused run console, fabrication, map and waypoint, relic resolution and 150-ore sales, results, hangar, default inputs, and reference-resolution reflow
