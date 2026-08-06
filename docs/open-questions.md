---
doc_id: GDD-OPEN-QUESTIONS
title: Open Questions
status: active
authoritative: true
---

# Open Questions

This is the single authoritative register of unresolved design questions. Gameplay documents link to entries here rather than maintaining duplicate question text.

## Question format

Each question records:

- **ID and question**
- **Why it matters**
- **Blocks** — documents, rules, content, or decisions that cannot be finalized
- **Known constraints**
- **Candidate answers** — optional and always non-canonical until decided
- **Status** — `open`, `researching`, `awaiting decision`, `deferred`, or `resolved`
- **Resolution** — the answer and link to a decision or canonical section

## Active questions

### OQ-004 — How does a mining point behave?

- **Why it matters:** The mining rules determine how much commitment, mobility, interruption, and defense the feature creates.
- **Blocks:** Mining, controls, HUD, map design, encounter design, and balance.
- **Known constraints:** Mining starts automatically inside a clearly visible circular zone and continues while the player remains there. Leaving grants a 0.5-second no-loss grace period, then unfinished progress decays linearly at four times that point's forward extraction rate; re-entry before zero resumes the remaining progress. Both finite ore-seam classes take 15 seconds to exhaust. A standard seam pays 10 ore every 1.5 seconds for ten installments and 100 total; each map has 20. A rich seam pays 40 ore every 3 seconds for five installments and 200 total; each map has 8. Completed installments remain secured. A 20-second material geode awards one specialized unit plus 50 common ore only at full completion. Each map's three 45-second Hyper Gold sites award 100 Hyper Gold apiece only at completion. The zone and decay values are initial playtest rules. See [Mining and Extraction](./40-mining-and-extraction.md).
- **Candidate answers:** Still to define: exact depleted-point presentation; interactions with explicitly authored forced movement or exact-boundary events; and whether any resource class should use a different zone size. The initial spatial distribution, clearance, and anti-overlap rules are fixed by [DEC-115](./decisions/DEC-115-adopt-standard-map-generation-contract.md). Ordinary damage does not interrupt extraction, modal pauses freeze progress and decay, and future multiplayer mining requires its own mode rules.
- **Status:** open
- **Resolution:** Partially resolved. The extraction zone has a 3.0M radius, a 6.0M diameter, for every mining-point class, and a geode resonance field has a 6.0M radius, under [DEC-128](./decisions/DEC-128-set-extraction-zone-and-resonance-field-radii.md). Depleted-point presentation, the forced-movement and exact-boundary interactions, and per-resource zone-size variation remain open.

### OQ-005 — What makes mining a push-your-luck system?

- **Why it matters:** A proximity constraint alone creates positional pressure, but push-your-luck normally requires a meaningful choice to continue accepting increasing or uncertain risk for additional reward.
- **Blocks:** Mining reward curve, threat response, exit rules, encounter design, and difficulty tuning.
- **Known constraints:** Remaining inside a mining area makes dodging harder, while leaving invokes the 0.5-second grace and four-times progress decay. Ore-seam installments secure partial value, but 20-second material geodes and 45-second Hyper Gold sites pay only at completion. Every unopened geode projects a larger resonance field that gives nearby enemies a material-specific 20% modifier; fields do not overlap or summon reinforcements. A Hyper Gold site separately activates its threat beacon with the first mining progress and escalates once each at 25%, 50%, and 75%. Each threshold has an accepted phase-scaled current-roster response, formation, two-second warning, and elite rule. Leaving stops further escalation while absent but does not remove summoned enemies; completion stops new beacon responses but also leaves survivors active. See the [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md#hyper-gold-threat-beacon-response).
- **Candidate answers:** Still to define for geodes: whether the six nominally equal modifiers need independent tuning. For Hyper Gold sites, playtesting may tune response percentages, floors, formation geometry, warning presentation, and the 150-enemy persistent-response ceiling without reopening the four-stage structure. See [RES-002](./research/RES-002-holdout-extraction-pressure-patterns.md).
- **Status:** open
- **Resolution:** Partially resolved. A geode resonance field has a 6.0M radius under [DEC-128](./decisions/DEC-128-set-extraction-zone-and-resonance-field-radii.md), twice the 3.0M extraction zone and larger than the 4.2M maximum expanded zone. Whether the six nominally equal modifiers need independent tuning, and the listed Hyper Gold response tuning, remain open.

### OQ-008 — How does exploration work?

- **Why it matters:** Exploration must be sufficiently informed and rewarding to feel strategic without removing discovery, navigation, or risk.
- **Blocks:** Maps, mining-point distribution, camera, navigation UI, procedural generation, pacing, and replayability.
- **Known constraints:** The player needs to explore the map to find mining resources and mech relics. Each level is a large, finite, bounded world rather than an infinite, repeating, or wrapping space. Every run significantly randomizes map layout and every player-relevant world location; even guaranteed elements appear at randomized positions. The standard generation contract fixes six to eight landmarked major regions, a 4:00–5:00 base-travel diameter, wide redundant routes, sparse 8–12% collision coverage, bounded optional pockets, randomized safe deployment, resource distance bands, site separation, and validation rules. The full world outline remains fogged until discovered. A compact active-play minimap reveals terrain through exploration fog and permanently records observed active or depleted deposits, landmarks, and opened or unopened relic caches. A larger paused Map destination is available through the run console; undiscovered content remains hidden.
- **Candidate answers:** Still to define: boundary fiction and exact art presentation; reveal radius and line-of-sight rules; resource world signals outside the radar; biome themes; and landmark content. The playfield and both maps are fixed north-up. The full map has three zoom levels, four marker filters, and one player-set waypoint on explored terrain; the waypoint appears on both maps and as a distinct active-play bearing. Relic caches use in-view world signaling and remain recorded once observed. See [Standard Map Generation Contract](./51-standard-map-generation-contract.md) and [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md).
- **Status:** open

### OQ-011 — What is the intended platform and presentation format?

- **Why it matters:** Camera, dimensionality, target devices, and input methods constrain readable enemy density, map navigation, controls, UI, performance expectations, and accessibility.
- **Blocks:** Camera, art direction, input model, UI, encounter scale, and platform features.
- **Known constraints:** The theme is science-fiction mechs versus alien monsters. Gameplay uses native low-poly 3D under a fully top-down, north-up orthographic camera with a wide field of view and fixed active-play world scale. Interfaces are 2D, and VFX may mix suitable techniques. The initial release targets Windows PC through Steam and Steam Deck in landscape. Desktop design uses 1920×1080 at 16:9; every screen fully supports 1280×800 at 16:10. Keyboard-and-mouse and gamepad are complete methods, every function works with gamepad alone, and glyphs switch automatically. Touch, mobile, portrait, console, native macOS, and non-Steam Linux requirements are outside the initial scope. The asset pipeline is CC0-first and must adapt mixed assets to a coherent shared style.
- **Candidate answers:** Still to define: ultrawide behavior; graphics and performance targets; fixed camera scale; palette, material, lighting, outline, animation, and effects rules; asset budgets; and the precise 2D/3D division for exceptional effects. DEC-127 fixes the first-playable keyboard, mouse, gamepad, and menu mappings. See [DEC-114](./decisions/DEC-114-use-native-low-poly-3d-gameplay.md), [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md), and [RES-005](./research/RES-005-free-asset-strategy.md).
- **Status:** open

### OQ-013 — What resource types exist, and what does each purchase?

- **Why it matters:** Multiple resource types create the routing, recipe, scarcity, and build-planning decisions that distinguish mining from a renamed XP bar.
- **Blocks:** Mining content, crafting recipes, weapon progression, economy, map distribution, UI, and balance.
- **Known constraints:** Common basic ore buys weapon-stat ranks, three capped ranks on installed non-radar utilities, and universal recipes. Mining supplies it through ore seams and 50-ore geode payouts; relic sales and boss loot are additional sources. Exactly four of six specialized materials occur per run: `A` Asterite, `B` Barysteel, `C` Cinderglass, `D` Driftmetal, `E` Eidolon Coral, and `F` Flux Amber. Each present material has eight, nine, or ten 20-second, completion-only, one-unit geodes, disclosed after deployment with count and abundance label but hidden locations. Each geode has a material-themed enemy resonance field. A pair-weapon costs one unit of each recipe material, a weapon branch costs two units of its assigned material, and each of twelve accepted non-radar utilities costs one unit of its single assigned material, with two utilities assigned per material. Thus every profile offers eight plus the ore-crafted radar. Bosses add six total random units drawn only from the four present materials if all loot is collected. Ordinary crafting materials remain run-local. Each map also contains three 45-second Hyper Gold sites worth 100 Hyper Gold apiece; four boss bursts add up to another 100. Hyper Gold funds cross-run upgrades and is banked only at mission extraction. See [Mining and Extraction](./40-mining-and-extraction.md), [Specialized Resource Identities](./61-specialized-resource-identities.md), and the [Utility Catalog](./68-utility-catalog.md).
- **Candidate answers:** Still to define: the distribution probabilities among eight, nine, and ten geodes; fungibility or conversion, if playtesting demonstrates a need despite the increased supply; and carrying limits. Every initial relic now sells for a fixed 150 common ore under DEC-127. Utility concepts are resolved by DEC-116, numerical PowerUps by DEC-120, and the initial permanent option catalog by DEC-121, with numeric playtesting still required. Later content expansions require their own decisions. Hyper Gold payouts are never modified by PowerUps. See [RES-006](./research/RES-006-resource-color-weapon-graph.md).
- **Status:** open

### OQ-023 — Which asset medium and visual style best fit the free-asset constraint?

- **Why it matters:** A coherent asset ecosystem affects the visible art style, animation quality, enemy variety, readability, and feasibility of producing enough content without bespoke art.
- **Blocks:** Asset sourcing, animation style, VFX, environment art, UI cohesion, content scope, and performance budgets.
- **Known constraints:** Gameplay uses native low-poly 3D with an orthographic fully top-down camera; HUD and interfaces use 2D; VFX may mix suitable techniques. The asset pipeline is CC0-first, every external asset retains a source-and-license ledger, and mixed packs receive common scale, material, palette, and readability treatment. Required gameplay state cannot depend on realistic lighting, subtle texture detail, or color alone. A representative prototype must prove six-mech coverage, horde readability, asset cohesion, and Steam Deck performance. See [DEC-114](./decisions/DEC-114-use-native-low-poly-3d-gameplay.md) and [RES-005](./research/RES-005-free-asset-strategy.md).
- **Candidate answers:** Still to define: the palette and material system; lighting, ground-contact, outline, animation, and effects rules; model, animation, draw-call, and effects budgets; the final candidate packs and adaptation workload; and the exact validation scenes and acceptance thresholds.
- **Status:** open

### OQ-034 — Should extreme weapon-stat investment reveal a hidden upgrade?

- **Why it matters:** An Easter egg can make the uncapped system surprising, but a meaningful hidden power breakpoint may turn intentionally inefficient spending into required metagame knowledge.
- **Blocks:** Only optional secret content and its achievement or discovery presentation; it does not block the first playable weapon catalog.
- **Known constraints:** Ordinary stat tracks remain uncapped, each rank is a fixed additive gain, and shared-depth prices become sharply nonlinear. The accepted initial catalog contains no hidden ranks, transformations, or stat-count prerequisites. Any secret must not make an otherwise poor allocation the dominant standard build.
- **Candidate answers:** A deliberately extravagant depth threshold could produce a comic visual, codex entry, achievement, or modest weapon-specific capstone. Prefer a discoverable flourish or sidegrade over a giant untelegraphed damage multiplier. Decide only after real runs establish what counts as extravagant rather than normal investment.
- **Status:** deferred

### OQ-032 — What onboarding, accessibility, and settings does standard mode require?

- **Why it matters:** Low-input combat is approachable, but the geological survey, automatic mining, fast decay, resonance fields, irreversible fabrication, and dense effects introduce rules that must be taught and remain readable.
- **Blocks:** First-run flow, tutorial timing, input remapping, difficulty assists, visual clarity, audio cues, photosensitivity support, text size, and player support.
- **Known constraints:** The opening survey cannot pause normal active play. The first mining interaction must teach automatic entry, the extraction zone, leaving grace, and decay. Resource identities, boundaries, radar directions, and danger states cannot rely on color alone. Required gameplay state must remain accessible even when the HUD is resized or restyled. Standard mode remains plausibly completable on a fresh account and uses no manual aiming or firing. The initial Windows Steam and Steam Deck target requires complete keyboard/mouse and gamepad access, automatic glyph switching, input remapping, 1280×800 legibility, and no required touch or free-text entry. Required text never falls below 9 pixels at 1280×800, with 12 pixels as the normal target.
- **Candidate answers:** Define first-run overlays or a separate tutorial; exact text, HUD-scale, contrast, shake, and VFX-intensity ranges; audio-caption vocabulary; game-speed or assist options if any; aim-pattern clarity; and how assists affect records or unlocks. DEC-127 already requires full remapping, manual glyph override, adjustable text and HUD scale, reduced-motion and reduced-flash paths, and independent shake and VFX controls.
- **Status:** open

### OQ-033 — What narrative and thematic frame connects the visible systems?

- **Why it matters:** The player repeatedly selects mechs, mines alien materials, fabricates equipment, fights bosses, gathers Hyper Gold, and performs mission extraction; names and presentation need a coherent reason and tone.
- **Blocks:** Faction and character naming, map identities, enemy families, mission framing, hangar presentation, unlock text, environmental storytelling, music, voice, and ending presentation.
- **Known constraints:** The fantasy is science-fiction mechs versus alien monsters. Six specialized materials already have non-exclusive identities. A successful 35-minute survival is framed as mission extraction; the between-run destination is a hangar. The game should remain readable and compatible with a free-asset-led visual production strategy.
- **Candidate answers:** Define the pilot or autonomous-mech premise, factions, why mining and Hyper Gold matter, why extraction occurs at 35:00, tone, naming conventions, biome stories, boss identities, how much text interrupts play, and the music and voice approach.
- **Status:** open

## Resolved questions

### OQ-031 — How are active play, fabrication, pause, and results presented?

- **Status:** resolved
- **Resolution:** Standard mode uses a sparse edge-anchored active HUD and one shared fully paused run console with Status, Fabrication, Map, Settings, and Controls. Direct shortcuts open the relevant paused destination. The specification fixes HUD regions, contextual mining and warning priority, controller grammar, confirmation tiers, fabrication comparisons, map filters and waypoint, relic resolution, four results pages, hangar flow, number formatting, and 1920×1080 / 1280×800 reflow. See [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md) and [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md).

### OQ-027 — How are mech relic discoveries presented and resolved?

- **Status:** resolved
- **Resolution:** Each run assigns three distinct relics without replacement from the currently unlocked pool. Caches have no dedicated guards or through-fog bearing; an in-view silhouette, emblem, and signal reveal them, after which the map records them. Discovery fully pauses on a mandatory comparison showing the one-sentence rule, exact values, weapon compatibility, installed relic, and outcomes. Every initial relic sells for 150 common ore; installing a replacement automatically sells the displaced relic for 150. See [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md) and [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#relic-cache-discovery-and-resolution).

### OQ-022 — How does the opening survey phase work?

- **Status:** resolved
- **Resolution:** The survey appears non-modally 0.5 active seconds after deployment beneath the minimap, lists four material identities with exact counts and abundance labels, marks signature branch availability, remains expanded for 12 active seconds, and then collapses into the wallet. It never captures movement. Opening Fabrication during its display pauses the remaining time and opens the complete Survey page, which stays available throughout the run. See [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md) and [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#opening-geological-survey).

### OQ-014 — How are weapons crafted and upgraded?

- **Status:** resolved
- **Resolution:** The accepted deterministic recipes, four weapon slots, three utility slots, shared-depth stat prices, three mutually exclusive branches, permanent run-slot commitments, and three utility ranks are presented through the fully paused Fabrication surface. It shows current-profile and all-blueprint views, affordability, automatic behavior, effective statistics, exact before/after values, branch comparisons, relic interactions, and permanent consequences. Ordinary ore ranks use one deliberate confirmation; weapon or utility installation and branch commitment use explicit consequence dialogs. Full slots disable further installation without hiding blueprints. See [DEC-100](./decisions/DEC-100-commit-installed-weapons-and-utilities.md), [DEC-109](./decisions/DEC-109-use-single-material-utilities-with-three-ore-ranks.md), [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md), and [Interface, Screen Flow, and Information Architecture](./73-interface-screen-flow-and-information-architecture.md#fabrication).

### OQ-016 — What rewards, if any, come directly from defeating monsters?

- **Status:** resolved
- **Resolution:** Ordinary enemies and elites drop nothing; defeating them yields immediate space and safety plus any explicitly authored counter credit. Standard mode instead maintains up to 16 active destructible rocks near the player's explored area. It makes one valid offscreen replenishment attempt per active-simulation second with 10% success, filling an empty slot or recycling the farthest eligible offscreen rock at the cap. A rock has 100 Hull, a non-solid 0.80M damage footprint, and a valid 18–45M spawn annulus at least 2M beyond the view. Automatic weapons can break rocks, and each has a fixed 20% chance to drop one persistent contact-collected health pack. A pack has a 0.25M pickup radius and immediately repairs 25 Hull without overhealing; it is consumed even at full Hull. Rocks never yield resources, equipment, permanent rewards, or temporary effects. Bosses remain the only defeated enemies that drop loot: their accepted physical resource bursts are unchanged. Presentation remains production work rather than unresolved gameplay. See the [Player Survivability and Damage Baseline](./72-player-survivability-and-damage-baseline.md), [DEC-123](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md), and [DEC-126](./decisions/DEC-126-adopt-the-initial-player-survivability-baseline.md).

### OQ-010 — What are the progression layers?

- **Status:** resolved
- **Resolution:** The foundational progression structure has three layers. Common ore and specialized materials build a strictly run-local arsenal; one exploration-found relic supplies a run-local transformative modifier; and extraction-secured Hyper Gold funds two between-run account systems. The thirteen-track numerical PowerUp catalog costs 9,450 Hyper Gold, applies account-wide, allows voluntary active ranks, and can be refunded completely. The six-purchase option catalog costs 2,150 Hyper Gold, permanently unlocks one balanced utility suite and five relic-pool entries, and cannot be refunded or disabled. A fresh profile already has all six initial mechs, all 15 base weapons, the radar, six basic utilities, five relics, the standard map, and standard mode. Later content may extend the breadth catalog, but that additive content is not an undefined foundational layer. See [Resources, Crafting, and Progression](./60-resources-crafting-progression.md), the [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md), the [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md), and [DEC-121](./decisions/DEC-121-accept-initial-option-unlock-catalog.md).

### OQ-030 — What enemies, bosses, and minute waves fill a standard run?

- **Status:** resolved
- **Resolution:** The initial standard encounter package contains ten fixed-profile ordinary identities built from six silhouettes and four readable variants, with Needler as the sole straight-shot specialist; a shared stat-only elite treatment; Riftjaw, Brood Titan, Prism Crown, and Skybreaker Apex as four persistent one-mechanic bosses; and a complete deterministic 35-row schedule of populations, replenishment pulses, compositions, and formations. Boss minutes briefly lower ordinary density, the final seven minutes escalate to sustained saturation without a fifth boss or end-state attacker, and Hyper Gold beacons add four phase-scaled persistent response packages drawn from the current roster. Exact numeric values remain playtest tuning. See the [Initial Alien and Boss Roster](./31-initial-alien-roster.md), [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md), and [DEC-119](./decisions/DEC-119-accept-initial-alien-encounter-baseline.md).

### OQ-020 — How do interval boss encounters resolve?

- **Status:** resolved
- **Resolution:** Exactly one boss arrives at each 7:00 threshold after a 15-second audiovisual and directional warning. Riftjaw charges, Brood Titan sheds an incomplete Skitterling ring, Prism Crown fires a radial burst, and Skybreaker Apex leaps to a locked marker. Each persists until killed, ordinary hordes continue at a temporarily reduced arrival-minute density, later bosses remain scheduled, and overlap is allowed. Bosses never despawn, cannot block extraction, and retain the accepted physical resource burst. Their initial Hull sequence is 6,000 / 14,000 / 30,000 / 45,000, validated against a legal no-relic reference build; those values remain playtest-tunable rather than structurally open. Audiovisual assets remain production work. See the [Initial Alien and Boss Roster](./31-initial-alien-roster.md), [Standard Wave and Beacon Schedule](./32-standard-wave-and-beacon-schedule.md), [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md), and [DEC-125](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md).

### OQ-029 — What are the six initial mechs and their traits?

- **Status:** resolved
- **Resolution:** The initial roster is Kestrel with Pulse Repeater and +15% Attack Rate; Pike with Rail Lance and +15% weapon Damage; Prospector with Missile Rack and +15% mining Extraction Rate; Lodestar with Gravity Projector and +15% weapon Area; Bastion with Reactor Pulse and +25 maximum Hull Integrity; and Razorback with Ram Field and +10% Movement Speed. Each has one positive, always-on, slotless trait and a distinct fully top-down silhouette. All six are available on a fresh profile, Kestrel is recommended but not forced, and Random Mech is available without revealing geology. Names remain presentation-working names and numeric values remain subject to balance testing. Later roster additions require separately specified extraction-secured or Hyper-Gold-based unlocks rather than failed-run unlocks. See the [Initial Mech Catalog](./36-initial-mech-catalog.md) and [DEC-117](./decisions/DEC-117-accept-initial-mech-catalog.md).

### OQ-024 — How is a highly randomized map constructed?

- **Status:** resolved
- **Resolution:** Standard maps contain six to eight broad landmarked regions, target a 4:00–5:00 base-travel diameter, and place every persistent important location within 2:30 of deployment. Major routes form redundant loops, connectors remain at least one mining-zone diameter wide, solid terrain targets 8–12% coverage, and optional pockets are few, shallow, and spacious. A randomized safe deployment receives defined Near-band mining opportunities. Every persistent resource class, Hyper Gold site, relic cache, and landmark obeys explicit distance-band, per-region, separation, clearance, and anti-clustering rules. Destructible rocks instead use validated dynamic offscreen positions under DEC-123. The boundary is solid, non-damaging, and discovered through fog. A map that violates counts, reachability, topology, clearance, distribution, or presentation rules is not a valid seed. Exact generation algorithms remain a future technical choice, while the numeric baselines remain playtest-tunable. See [DEC-115](./decisions/DEC-115-adopt-standard-map-generation-contract.md), [DEC-123](./decisions/DEC-123-replenish-destructible-rocks-around-the-player.md), and the [Standard Map Generation Contract](./51-standard-map-generation-contract.md).

### OQ-003 — What is the complete run or session structure?

- **Status:** resolved
- **Resolution:** A standard run is one 35-minute single-player deployment. Its visible timer counts upward only during active simulation; general pause, fabrication, relic resolution, blocking tutorials or modals, operating-system suspension, and focus loss freeze it. Death or confirmed abandonment fails the run; reaching 35:00 alive succeeds immediately, banks Hyper Gold, and discards the run build even if bosses remain. Every outcome proceeds through a defined results summary and returns to the hangar. Alternate modes and exact extraction/results presentation remain future content rather than gaps in the standard structure. See [DEC-099](./decisions/DEC-099-use-single-player-pause-and-results-flow.md) and [Run Structure and Timing](./20-run-structure-and-timing.md).

### OQ-006 — Where do resources, crafting, and upgrades persist?

- **Status:** resolved
- **Resolution:** Common ore, specialized materials, weapons, utilities, stat ranks, weapon branches, and relics are run-local and end with the deployment. Hyper Gold is unsecured when collected and becomes persistent only through successful mission extraction. Banked Hyper Gold purchases account-wide PowerUps or permanent option unlocks; PowerUps are freely refundable between runs, while option unlocks are permanent and nonrefundable. The initial numerical and option catalogs and prices are fixed by DEC-120 and DEC-121, while later content additions remain separate decisions. See [Resources, Crafting, and Progression](./60-resources-crafting-progression.md), the [Permanent PowerUp Catalog](./62-permanent-powerup-catalog.md), and the [Permanent Option-Unlock Catalog](./63-permanent-option-unlock-catalog.md).

### OQ-009 — How does the mech fight and move?

- **Status:** resolved
- **Resolution:** Combat input is movement only. Input immediately sets full movement direction and speed; digital diagonals are normalized, analog input supports the full circle, and release stops immediately. There is no inertia, turn radius, sprint, dash, dodge, stamina, reverse penalty, or strafe penalty. Facing begins east at deployment, follows movement, and retains the last nonzero direction. Weapons aim and fire automatically. Terrain and map boundaries are solid, enemies are non-solid, and ordinary damage does not cause hitstun, knockback, control loss, or mining interruption. See [DEC-097](./decisions/DEC-097-inherit-direct-movement-collision-and-camera.md) and [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md).

### OQ-012 — Who is the intended player, and what difficulty experience should the game provide?

- **Status:** resolved
- **Resolution:** Standard mode targets an approachable, low-input survivor-like challenge: a forgiving first minute, steadily escalating routing and build pressure, and a final phase where a successful build can feel overwhelmingly powerful. A fresh account must have a plausible path to extraction. A highly upgraded account substantially eases early play and improves consistency, but permanent power remains weaker than a mature run build and cannot universally eliminate late positioning or automate resource acquisition. The director neither weakens authored waves for a struggling player nor scales them up to cancel progression. Exact balance values, onboarding, accessibility settings, and alternate difficulty modes remain later design work. See [DEC-101](./decisions/DEC-101-target-an-approachable-escalating-standard-difficulty.md) and [DEC-112](./decisions/DEC-112-bound-permanent-power-below-run-build-power.md).

### OQ-028 — What are the 15 base weapons and their graph assignments?

- **Status:** resolved
- **Resolution:** DEC-043 fixes the 15 equal-tier concepts, graph positions, off-colors, and six initial signatures. DEC-045 through DEC-074 define every base behavior, three-stat bundle, and branch set; DEC-075 accepts the complete concept catalog. DEC-125 supplies exact first-playable base values, stat increments, branch values, caps, and weapon-specific edge rules. Later changes are playtest tuning rather than missing design. Mech identities are fixed separately, while audiovisual presentation remains content production work. See the [Weapon Catalog and Resource Graph](./66-weapon-catalog-and-resource-graph.md), [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md), and [Weapon Specification Index](./weapons/README.md).

### OQ-025 — How are uncapped stat upgrades priced and weapon branches structured?

- **Status:** resolved
- **Resolution:** Every initial weapon has three fixed uncapped stat tracks. Each rank adds the exact weapon-specific increment in the [Initial Weapon Numeric Catalog](./71-initial-weapon-numeric-catalog.md), while purchase number `n` on that weapon costs `5n(n + 1)` Ore using shared total weapon depth. Each weapon has three mutually exclusive, immediately eligible branches costing two assigned specialized units, with no stat prerequisite and no initial follow-on branch ranks. DEC-125 fixes all 45 base stat increments and all 45 branch multipliers, timings, caps, and weapon-specific edge rules. The optional extreme-investment Easter egg is isolated under OQ-034 and no longer keeps the core system open. See [DEC-085](./decisions/DEC-085-use-triangular-shared-depth-prices.md), [DEC-124](./decisions/DEC-124-adopt-a-multi-metric-weapon-balance-framework.md), and [DEC-125](./decisions/DEC-125-adopt-the-initial-numerical-weapon-catalog-and-feasible-boss-hull.md).

### OQ-026 — Should the game have a cross-weapon module system?

- **Status:** resolved
- **Resolution:** Yes, but as a single mech-wide **relic** rather than per-weapon modules. The mech has one run-local relic slot. Relics are found on the map rather than fabricated, can alter several weapons or another major gameplay rule at once, and should create significant behavioral twists or tradeoffs rather than functioning primarily as unconditional stat boosts. See [DEC-028](./decisions/DEC-028-one-exploration-found-mech-relic.md) and [Mech Relics](./67-mech-relics.md).

### OQ-001 — Which *Vampire Survivors* mechanics does this game inherit?

- **Status:** resolved
- **Resolution:** The simplest core single-player normal-stage *Vampire Survivors* behavior is the default precedent for direct movement and collision feel, camera tracking, automatic combat pressure, enemy spawning and recycling, boss pursuit, pause flow, and results conventions whenever no accepted decision replaces it. Explicit decisions always win. The rule does not import XP, chests, static stages, the reference weapon-acquisition/evolution system, run duration, loadout limits, economy, modes, multiplayer, platform, or art. See [DEC-096](./decisions/DEC-096-use-vampire-survivors-as-the-default-precedent.md) and [Combat, Weapons, Movement, and Camera](./30-combat-weapons-movement-camera.md).

### OQ-002 — What does crafting replace?

- **Status:** resolved
- **Resolution:** Mining and crafting replace XP and treasure-chest weapon progression. The game has no XP. Ordinary mined resources craft run-local weapons and upgrades; Hyper Gold found at three sites per map supports cross-run upgrades. See [DEC-002](./decisions/DEC-002-mining-replaces-xp-and-chests.md), [DEC-080](./decisions/DEC-080-twenty-second-geodes-forty-five-second-super-resources.md), [DEC-091](./decisions/DEC-091-name-and-quantify-hyper-gold.md), and [Resources, Crafting, and Progression](./60-resources-crafting-progression.md).

### OQ-007 — When and how does the player craft?

- **Status:** resolved
- **Resolution:** The player can open the fabrication menu anywhere and at any time during a run, without a charge, schedule, boss requirement, or location requirement. Access is unlimited, and one visit can include any number of affordable valid crafts. The entire gameplay simulation freezes while the menu is open. The rule will be reevaluated if playtesting shows excessive interruption or pause abuse. See [DEC-007](./decisions/DEC-007-unlimited-on-demand-fabrication.md) and [RES-003](./research/RES-003-crafting-break-cadence.md).

### OQ-015 — How are rare resources secured for cross-run use?

- **Status:** resolved
- **Resolution:** Completing a rare-resource mining operation collects the resource but does not bank it. The player permanently keeps it only by surviving until the level's time limit and completing mission extraction; death beforehand forfeits it. See [DEC-004](./decisions/DEC-004-mining-retention-threat-and-banking.md) and [DEC-005](./decisions/DEC-005-timed-survival-and-mission-extraction.md).

### OQ-017 — What game state continues while the fabrication menu is open?

- **Status:** resolved
- **Resolution:** The entire gameplay simulation freezes. The level timer, enemies, AI, spawning, projectiles, automatic attacks, cooldowns, mining progress and decay, threat-beacon events, hazards, status durations, pickups, and gameplay physics do not advance. Only the fabrication interface and its non-gameplay presentation continue. See [DEC-007](./decisions/DEC-007-unlimited-on-demand-fabrication.md).

### OQ-018 — How does each run randomize build availability without enabling fishing?

- **Status:** resolved
- **Resolution:** Unlocked blueprints, recipes, effects, and prices remain fixed. Exactly four of six specialized material families appear per run. Their complete pair graph defines 15 normal base weapons and exposes exactly six recipes per profile. The selected signature weapon belongs to that catalog, and generation restricts the roll to profiles containing at least two of its three branch materials. The four materials and their eight-to-ten geode counts remain hidden until deployment, then appear during the active opening while exact locations remain unknown until exploration. Reopening fabrication never rerolls anything. See [DEC-008](./decisions/DEC-008-fixed-blueprints-randomized-resource-profiles.md), [DEC-015](./decisions/DEC-015-in-run-opening-geological-survey.md), [DEC-034](./decisions/DEC-034-gate-base-weapons-by-resource-profile.md), [DEC-036](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md), [DEC-077](./decisions/DEC-077-ore-seams-and-material-geodes.md), [DEC-081](./decisions/DEC-081-eight-to-ten-geodes-per-material.md), [RES-004](./research/RES-004-run-randomization-and-build-agency.md), and [RES-006](./research/RES-006-resource-color-weapon-graph.md).

### OQ-019 — How does the resource radar work?

- **Status:** resolved
- **Resolution:** The resource radar is a run-local utility blueprint available from the beginning and always offered in the fixed catalog for 300 common ore. Once installed, it continuously shows up to seven active-play screen-edge directions: one toward the nearest remaining unopened geode of each of the four surveyed specialized materials, plus the nearest nondepleted standard ore seam, rich ore seam, and incomplete Hyper Gold site. It automatically retargets each category, requires no manual targeting or pause, shows neither exact waypoint nor distance, permanently commits one utility slot for the run, and reports exhausted categories without false directions. DEC-127 fixes category iconography, six-degree overlap fanning, clustering beyond three, and exhaustion feedback. See [DEC-009](./decisions/DEC-009-ore-powered-directional-resource-radar.md), [DEC-018](./decisions/DEC-018-four-weapons-three-utilities.md), [DEC-087](./decisions/DEC-087-price-resource-radar-at-three-hundred-ore.md), [DEC-088](./decisions/DEC-088-show-continuous-multi-material-radar-directions.md), [DEC-089](./decisions/DEC-089-expand-radar-to-all-mining-categories.md), [DEC-091](./decisions/DEC-091-name-and-quantify-hyper-gold.md), [DEC-100](./decisions/DEC-100-commit-installed-weapons-and-utilities.md), [DEC-127](./decisions/DEC-127-adopt-the-first-playable-interface-and-screen-flow.md), and [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md).

### OQ-021 — What is the pre-deployment selection order?

- **Status:** resolved
- **Resolution:** The player selects a mech and confirms deployment without access to the randomized resource profile. That signature weapon constrains generation to four-of-six profiles containing at least two of its three branch-resource colors, but the result remains hidden. The 35-minute timer, simulation, and one-minute minor-wave orientation phase then begin, and the geological survey becomes available during active play. See [DEC-015](./decisions/DEC-015-in-run-opening-geological-survey.md), [DEC-016](./decisions/DEC-016-one-minute-opening-orientation.md), [DEC-036](./decisions/DEC-036-six-color-signature-aware-resource-profiles.md), [DEC-079](./decisions/DEC-079-thirty-five-minute-seven-minute-boss-cycle.md), [Playable Mechs and Starting Loadouts](./35-playable-mechs.md), and [Maps, Resource Surveys, Exploration, and Navigation](./50-maps-resources-and-navigation.md).
