---
doc_id: TDD-RISKS
title: Technical Risk Register
status: active
authoritative: true
---

# Technical Risk Register

## Purpose

This register identifies the assumptions most likely to invalidate cost, schedule, performance, or architecture. Each risk has an early proof, trigger, and bounded response so agents do not discover it only after content investment.

| ID | Risk | Early proof/gate | Trigger | First response |
| --- | --- | --- | --- | --- |
| RSK-001 | GPU-instanced VAT crowds do not preserve appealing/readable low-poly animation | PRE-004/AST-003 before catalog art | unacceptable deformation/silhouette or pipeline fragility | test equivalent GPU deformation; simplify clips/rig before node actors |
| RSK-002 | Steam Deck misses 60 FPS under legal horde/VFX | M2 proxy test, then PERF-04 each milestone | p95 CPU/GPU over budget or stalls | profile owner; batch/LOD/VFX/queries first, never population |
| RSK-003 | Mobile renderer lacks a required presentation feature | representative materials/light/VFX in M2/M3 | critical readable effect cannot be reproduced | simpler shader/presentation; compare renderer only with measured TDR |
| RSK-004 | Constraint generation has high failure/latency or repetitive maps | MAP-009/010 before art dressing breadth | >1% exhausted retries, p95 >2s, distribution bias | improve candidates/backtracking/staged constraints; keep valid fallback pool |
| RSK-005 | Flow navigation produces visible clumping/stuck enemies near sparse terrain | GEO-005 representative regions | frequent stuck recovery or unfair path collapse | refine raster/clearance/local steering; do not add per-agent A* broadly |
| RSK-006 | 15 weapons × branches × relics create unmaintainable special-case code | COM-005–010 and registry coverage | scattered ID checks, recursive bugs, missing pairings | strengthen shared hook/primitive only from proven repetitions; retain dedicated behaviors |
| RSK-007 | Unlimited fabrication pause encourages pathological interruption | M3/M4 usage metrics/playtest | excessive opens/panic-pause behavior | gameplay review; technical system already supports access-policy change |
| RSK-008 | Free asset families cannot form a coherent readable whole | AST-006 representative composition | adaptation still looks mismatched or identities fail top-down | palette/material/shape pass, replace weakest families, reduce breadth before medium change |
| RSK-009 | UI cannot show complete information at 1280×800/gamepad | UI screenshot/focus matrices before full catalog | <9px text, trapped focus, hidden required values | reflow/drawer/paging/short labels, never mouse-only or silent omission |
| RSK-010 | Recovery snapshots stall or cannot restore complex behavior state | PST-005 after representative actors | capture >2ms, artifact >16MiB, checksum divergence | snapshot pages/delta-safe compaction; reduce cadence, not correctness fields |
| RSK-011 | Cloud divergence risks progression loss/duplication | PLT-002 sandbox | ancestry unavailable or conflicting mutations | preserve both and require choice; no automatic currency merge |
| RSK-012 | Content JSON and gameplay docs drift | DAT-007/008 CI gate | analytical/catalog mismatch | fail build; update authoritative gameplay + JSON + generated reports together |
| RSK-013 | C#↔Godot interop dominates hot frames | PRE-001/004 profiling | per-entity property calls or >snapshot budget | batch buffers/server API, cache values, keep simulation pure |
| RSK-014 | Godot/.NET/export upgrade breaks bindings/imports/saves | version upgrade gate | regression in clean build, render, migration, platform | stay pinned; adopt only after full compatibility evidence |
| RSK-015 | Health/resource/economy tuning invalidates reference builds | QUA-001/002 milestone reports | boss/economy targets miss accepted bands | adjust in gameplay framework order with linked data/spec changes |
| RSK-016 | Autonomous tasks appear complete while skipping requirements or irreproducible checks | FND-010 and first package in every prefix | missing evidence fields, unexplained warnings/skips, clean rerun differs | fail evidence validation; reopen owning task and add fixture/gate |
| RSK-017 | Parallel agents create incompatible contracts or two mutable owners | FND-009 plus each integration wave | reverse project edge, duplicate type/rule, conflicting generated registry | freeze consumer work; land one owner/contract and rebase consumers |
| RSK-018 | Agents stall on preferences or provisional choices that are already authorized | first three autonomous work waves | repeated clarification without an explicit escalation condition | apply deterministic choice/default mandate; improve the routing spec if ambiguity recurs |

## Review cadence

- Review at each M0–M7 gate.
- Add a risk when a workaround would cross subsystem ownership or alter a milestone.
- Close only with durable evidence and reference its artifact/test.
- A triggered architectural risk may require a TDR; a balance/content risk follows the gameplay decision workflow.

## Related documents

- [Implementation Plan for AI Agents](./110-implementation-plan-for-ai-agents.md)
- [Performance, Diagnostics, and Observability](./90-performance-diagnostics-and-observability.md)
- [Verification Strategy](./91-verification-strategy.md)
