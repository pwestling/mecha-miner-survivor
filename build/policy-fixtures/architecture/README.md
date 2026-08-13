# Architecture negative-control fixtures

Owner: `FND-009` (`TASK-FND-009-001`), extended by the `MechaMiner.Diagnostics` layout
change. Authority:
[Component, Contract, and Schema Registry](../../../docs/technical/115-component-contract-and-schema-registry.md)
§ Accepted project boundary,
[Build, Dependencies, and Release Operations](../../../docs/technical/100-build-dependencies-and-release-operations.md)
§ Repository structure. Verification: `VER-FND-009-013`.

## What these prove

`build/verify-architecture.sh` asserts the accepted project boundary by reading MSBuild's
own evaluation of every project. Until now nothing proved that assertion could fail. Its
§ 3 loop compared each real project against its accepted reference set, and every real
project was compliant, so the loop only ever printed `ok`. A comparison that is only ever
run against inputs that satisfy it is indistinguishable from a comparison that always
succeeds — a broken `sed` pipeline, a `msbuild_items` that returns nothing, or an
`EXPECTED_PROJECTS` row whose separator was mistyped would all have gone on printing `ok`.

`MechaMiner.Diagnostics` made that gap load-bearing. It is a **sixth** `src/` project, it
was added after the accepted boundary table was written, and its accepted row is the
strictest one in the repository: `.NET base libraries only`, Godot `No`, zero project
references, a dependency leaf so that every other project may reference it without a
cycle. That row is what keeps the leaf a leaf. If it were unenforced, the first consumer
that wanted a content type inside a log record would add the edge and nothing would say
no.

So each fixture here is a project file **named** `MechaMiner.Diagnostics.csproj` carrying
exactly one deliberate violation of that row. The script feeds each one through the same
`msbuild_items` evaluation and the same comparison function that § 3 and § 4 use on the
real projects — not a reimplementation of them — and requires the comparison to report a
difference against the accepted Diagnostics row. Sharing the code path is the point: a
control that used its own comparison would prove only that the control works.

## The fixtures

| Fixture | Violation | Accepted row it breaks |
| --- | --- | --- |
| `compliant/` | none | positive control: must **match** the accepted row |
| `edge-content/` | references `MechaMiner.Content` | "allowed project dependencies: .NET base libraries only" |
| `edge-simulation/` | references `MechaMiner.Simulation` | same, and would make the leaf depend on the run domain |
| `edge-game/` | references `MechaMiner.Game` | the reverse Godot edge: a pure project reaching the engine project |
| `godot/` | `PackageReference` on `GodotSharp` | "Godot types allowed: No" |

`compliant/` is not decoration. Every other fixture passes its control by producing a
**difference**, so a harness that had stopped evaluating anything at all — returning an
empty reference set for every input — would report a difference for none of them and the
negative controls would correctly fail. But a harness that returned garbage for every
input would report a difference for all of them and every negative control would pass.
`compliant/` is the input that must come back equal, so that failure mode is caught too.
One direction alone proves nothing.

These fixtures are **not** members of `MechaMiner.sln`, so `dotnet build` and `dotnet
test` of the product never see them, and `build/verify-architecture.sh` § 2 still requires
the solution to contain exactly the accepted projects. They inherit
`build/policy-fixtures/Directory.Build.props`, so what they are evaluated under is the
real repository policy rather than a local copy of it.

`edge-game/` and `godot/` are never restored or built — the script only asks MSBuild to
evaluate their items — so neither one pulls Godot assemblies into this repository's
restore graph.
