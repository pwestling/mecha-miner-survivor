---
doc_id: TDD-CONTENT-DATA
title: Content Data and Validation
status: active
authoritative: true
---

# Content Data and Validation

## Purpose

This document defines content authoring, schemas, stable IDs, compilation, cross-reference validation, derived data, localization, compatibility versions, and the workflow AI agents use to change content safely.

## Source-of-truth boundary

- Gameplay Markdown remains authoritative for accepted player-visible rules and design intent.
- Strict JSON is the authoritative machine-consumed representation used by builds.
- C# behavior implementations are authoritative for runtime mechanics that cannot be represented as parameters.
- Generated bundles, reports, CSVs, balance summaries, and imported Godot resources are derived artifacts.

A gameplay value change is incomplete until the gameplay document, JSON definition, generated reports, dependent estimates, and verification fixtures agree. CI detects mechanical disagreement where a comparison can be automated.

## JSON codec and schema baseline

- Use the built-in `System.Text.Json` reader/writer with explicit typed DTOs and source-generated serialization metadata; do not add Newtonsoft.Json, runtime contract reflection, or dynamic JSON objects to production paths.
- Source files and persisted JSON are UTF-8. Comments, trailing commas, duplicate object properties, nonfinite numbers, and unknown fields are errors. Property names use `snake_case`; stable enum/kind/ID tokens remain exact case-sensitive ASCII.
- JSON Schemas use draft 2020-12 for editor/tool interoperability. The project-owned typed structural/semantic validators remain authoritative; a fixture corpus proves the schema and typed validator accept/reject the same structural cases.
- The canonical writer emits fields in schema-declared order, dictionaries as lexically sorted key entries, stable-ID sets in canonical ID order, and semantically ordered arrays in their authored/explicit order. It writes integers without padding and finite floating-point values with invariant round-trip representation, normalizing negative zero to zero.
- File order, operating-system path order, locale, indentation, and original property order do not affect compiled bundle or payload hashes.
- SHA-256 from the .NET base class library hashes canonical UTF-8 payload bytes. Human-readable pretty JSON is a separate derived view and is never hashed or loaded as canonical state.

The same codec policy is reused by content, saves, recovery, manifests, diagnostics, and task evidence unless a schema explicitly requires a compact binary derived asset. Each domain owns its DTOs and validation; codec reuse does not merge domain ownership.

## Accepted content repository layout

```text
content/
  schemas/
  resources/
  mechs/
  enemies/
  bosses/
  weapons/
  branches/
  utilities/
  relics/
  powerups/
  unlocks/
  mining-sites/
  encounters/
  maps/
  presentation/
  localization/
assets-manifest/
  assets/
  licenses/
generated/
  content.bundle.json
  content.bundle.sha256
  reports/
```

Catalog directories are the authoring boundary. Definitions are grouped by stable item or the smallest cohesive aggregate such as the standard encounter schedule; generated/source separation is mandatory. A layout change must update build tooling, schemas, importers, documentation, and clean-checkout tests atomically rather than adding a second search path.

## Stable ID policy

- Reuse accepted gameplay IDs exactly for defined content: `MCH-01`, `EN-01`, `BOSS-01`, `W-AB`, `REL-01`, and equivalent utility/PowerUp/unlock IDs.
- Generated map instances append or separately store run-local generated IDs; they do not modify content IDs.
- IDs are case-sensitive ASCII tokens matching a schema pattern and never localized.
- Display names and localization keys may change without changing IDs.
- Removing shipped content retires its ID and leaves a migration/tombstone entry; IDs are never reassigned.
- Cross-references contain IDs plus schema-validated expected category where ambiguity is possible.

## Common definition envelope

Every independently addressable definition contains:

| Field | Requirement |
| --- | --- |
| `id` | stable category-valid ID |
| `schema_version` | integer version of its definition schema |
| `content_version` | monotonic revision used for diagnostics and migrations |
| `status` | development, enabled, disabled, or retired; release bundles exclude development/disabled unless configured |
| `name_key` | localization key; never literal player-facing text |
| `summary_key` | concise player-facing summary key where relevant |
| `tags` | closed or validated vocabulary for queries and tooling, never hidden behavior |
| `source_refs` | gameplay document IDs/anchors and decision IDs implemented |
| `presentation_id` | logical presentation entry where the content appears in-world |

Unknown fields are errors rather than silently ignored. Optional fields have explicit defaults materialized into the canonical bundle so runtime never guesses.

### Declared-optional envelope fields

Two envelope fields are declared optional, and authors express absence the same way for both: **omit the key**. A JSON `null` is never legal anywhere in a source definition, because the codec rejects it as a type error rather than reading it as absence. The compiler materializes the documented default into the canonical bundle, so runtime always reads a value.

- `presentation_id` is omitted when a definition never appears in-world. Aggregates, schedules, and other non-embodied definitions omit it.
- `name_key` is required only where a definition has a player-facing name. A definition players never see named — an aggregate schedule or a generation contract — omits it. The localization catalog holds strings players read; internal aggregate titles do not belong in it.

`summary_key` follows the same rule its row already states: present where a concise player-facing summary is relevant, omitted otherwise.

### Initial versions

The initial `schema_version` is `1` and the initial `content_version` is `1` for every first-authored definition. `schema_version` then increments when its schema changes field meaning, and `content_version` increments on each subsequent revision of that definition, both as [Content compatibility](#content-compatibility) below describes. The [Component, Contract, and Schema Registry](./115-component-contract-and-schema-registry.md#schema-registry) delegates version assignment to the implementation; this records the assignment.

### `tags` vocabulary

`tags` accepts an empty array, and an empty array is the expected value for most definitions. The closed vocabulary starts **empty** and gains a term only when a concrete query or tooling need requires it; the term is added to the vocabulary in the same change that first uses it. A tag never carries behavior, never selects an implementation, and never gates a rule: a definition's behavior comes from its registered `behavior_kind` and parameters, never from the presence of a tag.

### `source_refs` element grammar

`source_refs` is an array of stable-ID strings. Each element is one of:

- a gameplay document ID with an optional anchor, for example `GDD-COMBAT` or `GDD-COMBAT#contact-damage`;
- a gameplay decision ID, `DEC-###`;
- a technical decision ID, `TDR-###`; or
- a technical requirement ID, `TR-<DOMAIN>-###`.

The grammar for each is the one declared in [Documentation Conventions](../conventions.md#stable-identifiers) and [Technical Documentation Conventions](./conventions.md#stable-identifiers).

A file path, a line number, or any `path:line` pair is **not** a legal element. Paths and line numbers move whenever a document is edited, so a reference built from them decays silently; [Stable ID policy](#stable-id-policy) above and [TDR-006](./decisions/TDR-006-author-validated-content-as-strict-json.md) establish that IDs, not filenames, connect definitions to their sources. A source that has no stable ID gets one before it can be referenced.

## Unit and numeric policy

- Ambiguous numeric names carry suffixes such as `_m`, `_m_per_s`, `_seconds`, `_per_second`, `_hull`, `_degrees`, `_fraction`, or `_count`.
- Percentages in authoring use human-readable percentage points only when the property name says `_percent`; the compiler writes normalized factors into the runtime bundle as a separate derived field.
- Durations are nonnegative and bounded by schema; rates cannot be negative.
- Integer currency and rank values are integral in source and checked for formula overflow.
- Geometry dimensions distinguish radius, diameter, width, range, and area; `area` is never used as a vague scalar name.
- A multiplicative scale carries the `_multiplier` suffix and keeps one name in every scope it appears in: an enemy's authored body scale is `body_scale_multiplier` in the source definition, in the canonical bundle, in generated reports, and in any code or schema that reads it. `_multiplier` says the value multiplies a reference dimension, which `_factor`, `_scale`, and a bare `scale` do not; and a single spelling everywhere is what lets a derived-value report be traced back to its operand by name.
- Formulas allowed to players, such as weapon upgrade price, are represented by a registered formula kind plus parameters, not arbitrary script strings.
- Derived values include source operands and calculation version in reports for auditability.

## Content catalogs

### Resources

Resource definition fields include ID, canonical letter, localization keys, icon/pattern/audio identity, inventory scope, persistence class, maximum safe count, and resonance behavior registration if applicable. The six-material set and common ore/Hyper Gold pass graph-specific validators.

### Mechs

Fields include signature weapon ID, trait behavior kind/parameters, base Hull/Armor/Recovery/movement/footprint overrides, availability, presentation, selection order, and comparison text. Validation ensures every signature is an initial weapon, every trait behavior is registered, and every mech remains compatible with profile generation.

### Enemies and bosses

The fields an enemy definition **authors** are Hull, the movement percentage, contact damage, contact cadence, `body_scale_multiplier`, control resistance, behavior registration, projectile or boss-ability parameters, elite eligibility, presentation, spawn classification, and telemetry tags. Contact diameter and contact-begin center distance are deliberately absent from that list, because they are derived rather than authored; the next paragraph is the same rule stated in full, not an additional one. Validation derives world speeds and contact footprints from the authored operands above and compares them with the survivability report.

Derived geometry is never authored, which is why the authored-field list above stops at the multiplier. An enemy definition stores its authored `body_scale_multiplier`; the compiler derives the contact diameter from that multiplier and the reference diameter, and derives the contact-begin center distance from the result. This is not a second rule. It is the rule [Unit and numeric policy](#unit-and-numeric-policy) already states, applied to geometry: a `_multiplier` "says the value multiplies a reference dimension", and the compiler is what performs that multiplication and writes the product into the runtime bundle. Authored movement is the same shape in the same policy — a percentage is authored as percentage points because its name says `_percent`, and the compiler "writes normalized factors into the runtime bundle as a separate derived field", which is where world speed comes from. One rule, two operands: the author supplies the multiplier or the percentage, and the compiler supplies every value computed from it.

An author who types a derived value into a definition creates a second source of truth that silently disagrees with the first the moment either operand changes, which is exactly how a gameplay table and a technical table came to disagree by 0.004 M on one enemy. Derived values appear in generated reports with their source operands and calculation version, as [Unit and numeric policy](#unit-and-numeric-policy) requires; they do not appear in source JSON.

Everything above states the rule for **ordinary enemy identities** — the ones the accepted roster gives a body scale, and therefore the ones that have an authored operand to derive geometry from. Boss contact geometry is governed separately and is deliberately not decided here; the paragraphs above must not be read as settling it in either direction, and the boss definition schema that `DAT-002` owns must not be written against an inference drawn from them.

### Weapons

Fields include recipe material pair, behavior kind, targeting policy, fixed properties, three stat-track definitions, rank-zero values, increments, snapshot/live classifications, all branch IDs, analytical-model registration, presentation/audio references, and rock-targeting behavior.

The compiler verifies exactly 15 unordered material-pair recipes, no duplicate pair, exactly three stats, one amplification/functional/conversion branch, unique branch materials according to the graph, and behavior registration.

### Branches

Fields include parent weapon, transformation class, two-unit material cost, behavior modifier kind/parameters, affected snapshot/live properties, exclusions/recursion flags, summary/detail keys, and compatibility notes. A branch cannot register against multiple weapons or add an unrecognized fourth stat.

### Utilities

Fields include assigned material or ore-only radar exception, unlock ownership, one-unit fabrication cost, slot behavior, behavior kind, base value, three rank values/prices where applicable, affected named stats, stacking classification, and presentation. Validators enforce no duplicate installed identity, allowed rank count, and exactly the accepted fresh/unlocked distribution.

The resource radar is a utility definition with the stable ID `UTL-R1`, and it is the one definition that uses the ore-only exception field named above rather than an assigned material. No accepted gameplay document minted an ID for it, because the accepted catalog identifies material utilities by their material letter; `UTL-R1` is minted here so cross-references to the radar are stable IDs like every other utility reference.

### Relics

Fields include pool availability/unlock, discovery sentence key, sale value, behavior registration, benefit/tradeoff parameters, hook points, affected weapon categories, live-state meter, and presentation. Validation requires one sentence summary, explicit tradeoff, compatibility results for all weapons, and no hidden unsupported behavior.

### PowerUps and option unlocks

PowerUps include rank cap, fixed costs/values by rank, active-rank policy, refundable flag, named-stat contribution, and UI grouping. Unlocks include exact Hyper Gold cost, nonrefundable flag, owned content additions, and whether ownership may be disabled. Validators recompute total catalog costs and maximum-account envelope.

### Mining sites

Fields include site class, count rule, zone/field dimensions, base work seconds, installment thresholds/payouts, decay/grace, resource result, beacon thresholds, presentation, map marker, and spawn exclusions. Standard mode validates exactly four accepted classes and their totals.

### Encounter schedule

One aggregate standard schedule file contains mode ID, duration, minute rows, composition weights, minimums, pulses, formations, boss warnings/arrivals, beacon response table, and population ceilings. Aggregate validation compares 35 contiguous rows, totals, earliest appearance, boss cadence, formation grammar, and accepted enemy IDs.

The standard encounter schedule has the stable ID `WAV-01`. It is an aggregate: it is not embodied in the world and players never read its name, so it omits `presentation_id` and `name_key` under [Declared-optional envelope fields](#declared-optional-envelope-fields).

### Map generation

The fields a map generation contract **authors** are mode/map ID, region/topology/scale ranges, static obstacle targets, distance bands, site counts, distribution constraints, candidate clearances, retry budgets, discovery settings, rock rules, and landmark pools. Semantic validation checks internal feasibility before sampling maps.

The map-generation version is deliberately absent from that list. [Content compatibility](#content-compatibility) below makes it part of build identity, which the build records and increments when generation semantics change; a contract that also declared it would be a second source of truth for the same value, disagreeing with build identity the moment either side moved. This is not an additional rule. It is the rule [Enemies and bosses](#enemies-and-bosses) above states for a derived field — derived values "do not appear in source JSON" — applied one layer up, to a whole contract rather than to one field: authoring is where operands live, and a version the build owns is not one of them.

The standard map generation contract has the stable ID `MGC-01`, and it is an aggregate on the same terms as `WAV-01`.

`WAV-01` and `MGC-01` are minted here. No accepted document previously granted a content-ID grammar for either aggregate, and both need one because every schema in this document references other definitions by stable ID. They follow [Stable ID policy](#stable-id-policy) above: case-sensitive ASCII, never localized, never reassigned.

### Presentation and audio

Presentation definitions map logical IDs to models, materials, animation sets, VFX recipes, UI icons, map markers, and fallback proxies. Audio definitions follow the event contract. These definitions never contain damage or other authoritative outcomes.

## Behavior registries

Each owning pure project exposes a manually composed immutable registration table through a narrow contract. `MechaMiner.Tools` combines the pure tables and presentation-recipe descriptors owned by Content, then emits the canonical registry manifest. `MechaMiner.Game` owns a separate explicit implementation table for those presentation recipe IDs; a Godot integration test requires exact descriptor/implementation set equality and compatible parameters without making Tools depend on Game. The manifest is derived and checked for staleness; runtime assembly scanning, reflection discovery, source-generator magic, and a separately hand-edited manifest are forbidden. The content compiler verifies every content `behavior_kind`, targeting policy, formula, modifier hook, formation, effect, and presentation recipe has exactly one registered descriptor with a compatible parameter schema.

An implementation agent adding a new kind must provide:

- stable kind ID and parameter schema;
- domain ownership and lifecycle;
- content validation;
- unit and integration fixtures;
- debug visualization/metrics where applicable; and
- at least one definition using it or an explicit infrastructure-only rationale.

Do not accept a raw type name from JSON and instantiate it through reflection.

## Compilation pipeline

```mermaid
flowchart LR
    Source["JSON source definitions"] --> Schema["Structural schema validation"]
    Schema --> Semantic["Semantic and range validation"]
    Semantic --> Refs["Cross-reference and graph validation"]
    Refs --> Registry["Behavior, asset, and localization registration"]
    Registry --> Derived["Derived values and normalized defaults"]
    Derived --> Bundle["Canonical immutable bundle"]
    Derived --> Reports["Balance, coverage, and traceability reports"]
    Bundle --> Hash["Content hash and build metadata"]
```

Every stage emits stable diagnostic codes, exact source path/field, content ID, expected constraint, and relevant related IDs. CI fails on errors. Warnings have an owner and expiration; release builds treat unresolved content warnings as errors unless allowlisted with rationale.

The canonical bundle is ordered by category and stable ID, uses normalized numeric formatting, includes schema/generation versions, and hashes identically for identical semantic input regardless of source file enumeration order.

## Validation layers

### Structural

Required fields, types, allowed properties, enum vocabulary, ID syntax, numeric bounds, and array cardinality.

### Semantic

Rules within a definition: positive cadence, branch class, three stats, increasing rank costs, valid geometry, exact reward totals, compatible behavior parameters.

### Relational

References, uniqueness, graph coverage, signature/profile feasibility, unlock ownership, material distribution, schedule availability, asset and localization existence.

### Analytical

Recalculate DPS estimates, price curves, total costs, enemy derived speeds/footprints, boss feasibility reference builds, and resource totals. Reports compare with accepted gameplay tables and fail on unexplained divergence beyond documented rounding.

### Runtime smoke

Instantiate every behavior in a tiny headless fixture, execute at least one activation/state transition, serialize its presentation view, and dispose it without error.

## Localization contract

- Source language is English stored in a dedicated string catalog, not definition files.
- Keys are stable semantic paths tied to content IDs and UI roles.
- Parameterized text uses named placeholders validated against each locale.
- UI definitions declare expected expansion class; pseudo-localization expands text and adds accented/directional stress characters.
- Player-facing numbers use locale-aware formatting while content formulas and saves remain invariant culture.
- Missing release strings are build errors; development builds show the key visibly.
- Final release locale list is product scope; infrastructure supports adding locales without content-schema changes.

### Source catalog format and key pattern

- Source catalogs are strict JSON under the same codec policy as every other source file: UTF-8, no comments, no trailing commas, no duplicate properties, unknown fields rejected.
- There is one file per locale at `content/localization/<locale>.json`.
- Each file is a flat object of key to string. There is no nesting, no metadata wrapper, and no array.
- Keys are lexically sorted, so a diff shows only the strings that changed and two authors adding different keys do not conflict on ordering.
- The key pattern is `<category>.<stable_id>.<role>`. The category is `snake_case`. The stable ID appears **verbatim**, in its own case, so `weapon.W-AB.name` and not `weapon.w_ab.name`: a localization key that transforms an ID is no longer traceable to it, and [Stable ID policy](#stable-id-policy) makes IDs case-sensitive.
- The role comes from a small set, beginning with `name` and `summary`, matching the `name_key` and `summary_key` envelope fields. The set grows with the same discipline as the `tags` vocabulary: a role is added when a definition or a UI surface needs it, not in advance.

## Asset manifest contract

Logical asset entries contain ID, type, source provenance/license record, source file, imported resource path, expected import settings, variants/LODs/animations, budget metadata, and fallback. Content definitions refer only to the logical ID.

The compiler verifies type compatibility and asset budget metadata; the Godot import audit verifies the actual imported resource matches the manifest.

## Content compatibility

Build identity records:

- product version;
- Godot and .NET versions;
- content bundle hash;
- per-schema versions;
- map-generation version;
- random-stream derivation version; and
- save-format version.

Changing numbers without changing behavior increments content revision/hash but not necessarily schema. Changing field meaning increments schema. Changing generation semantics increments map-generation version. Run recovery requires compatible versions declared by migrations; diagnostic seeds require the original version identities.

## Agent content-change workflow

1. Read the authoritative gameplay section, relevant technical behavior contract, and current definition.
2. Change the smallest source JSON set and any approved gameplay Markdown in the same work item.
3. Run structural, semantic, relational, analytical, asset, localization, and behavior-registration validation.
4. Regenerate canonical reports and CSV mirrors using the repository tool; never edit generated files to fix source errors.
5. Run affected headless benchmarks and golden fixtures.
6. Review diffs for unrelated key reordering or generated churn.
7. Record tuning evidence when values change materially.

An agent must not infer a new behavior from a field name, add an unvalidated optional field, encode logic in localization text, or bypass a validator to make a build pass.

## Verification

- Invalid-fixture suites cover every diagnostic code and schema boundary.
- Canonicalization tests permute source order and require identical bundle/hash.
- All cross-reference graphs have reachability/orphan reports.
- Gameplay catalog totals and pair mappings are asserted.
- A clean checkout can compile content without launching the Godot editor.
- Release packaging proves no development/disabled definitions or unlicensed assets enter the bundle.

## Related documents

- [Technical Documentation Conventions](./conventions.md)
- [Simulation Core](./20-simulation-core.md)
- [Combat and Weapon Runtime](./22-combat-and-weapon-runtime.md)
- [Procedural Map Generation](./50-procedural-map-generation.md)
- [Asset Pipeline and Budgets](./80-asset-pipeline-and-budgets.md)
- [Machine-Readable Gameplay Data Index](../data/README.md)
