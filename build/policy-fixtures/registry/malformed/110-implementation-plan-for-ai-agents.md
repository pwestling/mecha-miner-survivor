# Implementation Plan (fixture)

## Fixture work packages

| ID | Deliverable |
| --- | --- |
| FIX-001 | the fixture work package |

## Fixture task queue

| Task | Objective |
| --- | --- |
| `TASK-FIX-001-001` | the fixture task |

The fixture task cites `TR-FIX-001`, `CMP-FIX-001`, `CTR-FIX-001`, `SCH-FIX-001`,
`FIX-001`, and [the requirement index](./112-normative-requirement-index.md#fixture-requirements).

## Deliberate defect: malformed identifiers

These tokens are identifier-shaped but violate the grammar of
docs/technical/conventions.md, so the validator must report each one:
TR-FIX-1, CMP-FIX-0001, SCH-FIXTURE-001, and VER-FIX-001.
