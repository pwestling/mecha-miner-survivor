---
doc_id: DEC-009
title: Provide an Ore-Powered Directional Resource Radar
status: accepted
authoritative: false
---

# DEC-009 — Provide an Ore-Powered Directional Resource Radar

> **Completion note:** DEC-127 fixes category iconography, six-degree overlap fanning, clustering beyond three bearings, and exhausted-category presentation left open here.

## Decision

The resource radar is a run-local utility blueprint available from the beginning of the game and always offered in the fixed fabrication catalog. It costs only common basic ore; DEC-087 later fixes the amount at 300. This record originally used one selected specialized material; DEC-088 replaces selection and retargeting with simultaneous continuous guidance toward the nearest geode of every present specialized material, and DEC-089 expands that guidance to standard ore, rich ore, and super-resource sites.

The radar does not consume a weapon slot; under the later [DEC-018](./DEC-018-four-weapons-three-utilities.md), it consumes one of three utility slots. It clearly reports when no valid deposit remains. This record's original exclusion of rare special resources used for cross-run progression is superseded by DEC-089.

## Status

Accepted.

## Context

Randomized geology can leave a player with a viable intended recipe but no known route to its missing specialized material. That uncertainty supports exploration, but prolonged bad searching could trap the player in an underpowered state despite the required resource actually existing on the map.

## Considered options

### No recovery tool

This maximizes discovery pressure but allows navigation luck to invalidate otherwise informed build planning.

### Exact deposit waypoint

This reliably solves the search problem but reduces exploration to following a marker.

### Resource conversion or direct purchase

Converting basic ore into the missing material would bypass exploration and the positional risk of mining it.

### Directional resource radar

This improves navigation certainty while preserving traversal, combat, discovery, and mining extraction.

## Rationale

Charging only common ore ensures the recovery tool cannot require the same missing ingredient it is meant to find. Direction-only guidance removes potentially frustrating blind searching without exposing the exact route or deposit. Excluding a weapon-slot cost avoids worsening the combat deficit of a player already struggling to complete a build. Excluding rare cross-run resources preserves their exceptional discovery value.

The later 300-ore price makes this safety valve a substantial investment rather than a routine purchase.

DEC-088 later removes manual selection and retargeting and provides one continuous screen-edge direction for each present specialized material. DEC-089 adds one direction each for the nearest standard ore seam, rich ore seam, and super-resource site.

## Consequences

- The radar is a dependable option in every run, not a random offer or persistent unlock.
- A player must decide whether spending common ore on information is worth delaying combat power.
- The player must still travel to, survive near, and mine the located deposit.
- Radar feedback must communicate each tracked material, direction, overlapping bearing, and exhausted state without relying solely on color.
- DEC-087 fixes the price, DEC-088 fixes simultaneous active-play targeting, and DEC-089 expands coverage to all mining categories; exact indicator layout remains to be specified and balanced.
- If the radar becomes a near-universal purchase, playtesting should compare it with providing weaker resource guidance as a baseline navigation feature.

## Specification links

- [Maps, Resource Surveys, Exploration, and Navigation](../50-maps-resources-and-navigation.md)
- [Resources, Crafting, and Progression](../60-resources-crafting-progression.md)
- [OQ-019 — How does the resource radar work?](../open-questions.md#oq-019--how-does-the-resource-radar-work)

## Supersedes / superseded by

No earlier accepted decision is superseded. DEC-087 later fixes the price, DEC-088 supersedes the selected-material targeting behavior, and DEC-089 supersedes the special-resource exclusion.
