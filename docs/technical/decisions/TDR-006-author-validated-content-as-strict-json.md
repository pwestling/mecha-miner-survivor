---
doc_id: TDR-006
title: Author Validated Content as Strict JSON
status: accepted
authoritative: false
validation: content-compiler-and-agent-editing-trial
---

# TDR-006 — Author Validated Content as Strict JSON

## Decision

Author gameplay content definitions in strict, formatted JSON files governed by versioned schemas and semantic validators. Use one file per independently reviewable content item and dedicated aggregate files for schedules or matrices that must be validated as a whole.

A build-time content compiler resolves references, calculates derived values, validates registrations/assets/localization, writes a canonical immutable bundle and content hash, and rejects invalid content before the game launches.

## Context

The game contains many homogeneous definitions and cross-references. AI agents need formats that are text-diffable, mechanically validated, searchable, and editable without opening Godot. Godot Resources would couple content validation to engine startup; YAML would add ambiguous scalar behavior and another parser dependency; CSV is unsuitable for nested behavior and branch data.

## Consequences

- Strict JSON disallows comments. Design rationale stays in Markdown and descriptions use localization keys.
- Field names include units where ambiguity exists.
- Stable IDs, not filenames or display names, connect definitions.
- The generated bundle is never hand-edited or reviewed as source.
- Existing gameplay CSV mirrors become generated/reporting artifacts once the content compiler is implemented.
- Asset references use logical IDs resolved through an asset manifest, not scattered `res://` strings.
- New behavior kinds still require C# registry implementations and tests; JSON cannot inject executable logic.

## Specification links

- [Content Data and Validation](../40-content-data-and-validation.md)
- [Technical Documentation Conventions](../conventions.md)
- [Machine-Readable Gameplay Data Index](../../data/README.md)
