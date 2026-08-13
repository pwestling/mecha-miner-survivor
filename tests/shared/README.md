# Shared deterministic fixture utilities

Owner: `FND-003` (`TASK-FND-003-001`). Every later stream depends on these types, so
changing an observable behaviour here is an integration-owner change, not a
consumer-task edit (`docs/technical/delivery-waves.md` § Integration ownership).

These files are **linked into each test project** rather than compiled into a shared
assembly:

```xml
<Compile Include="../shared/**/*.cs" LinkBase="Support/Shared" />
```

`docs/technical/100-build-dependencies-and-release-operations.md` § Repository
structure enumerates the accepted test projects and prescribes no shared test
library, and
`docs/technical/115-component-contract-and-schema-registry.md` § Accepted project
boundary states that "Production projects never depend on test projects". Linking
keeps the accepted project set intact, keeps every type `internal`, and means no
cross-assembly contract is created for what is deliberately test-only scaffolding.

## What each file owns

| File | Owns |
| --- | --- |
| `HarnessIdentity.cs` | the version identity every randomized test logs before it runs |
| `Tolerance.cs` | a named float tolerance; an unnamed one cannot be constructed |
| `NumericAssert.cs` | exact equality for integers, named-tolerance equality for floats |
| `DeterministicCase.cs` | seed and identity logging before execution, reproduction on failure |
| `PropertyCase.cs` | generated-input property runs with shrinking and minimized-input preservation |
| `Shrinkers.cs` | the shrink strategies `PropertyCase` uses |
| `GoldenText.cs` | canonical ordered reviewable golden text comparison |
| `TestArtifacts.cs` | the `artifacts/` layout failures write into |

## The rules these types exist to enforce

From `docs/technical/91-verification-strategy.md`:

- § Determinism and fixture policy: "Every randomized test logs its seed and version
  identity before execution" and "Failures print a one-command/tool reproduction
  description and preserve the minimized input where possible."
- § Numeric tolerance: "each assertion names the tolerance", and
  "'Approximately equal' without a named tolerance is not an acceptable test."
  `NumericAssert` therefore has **no** overload that takes a bare epsilon.
- § Determinism and fixture policy: updating a golden "requires an authority-aware
  diff review of the underlying behavior change plus a regenerated evidence bundle"
  and an agent "may not accept snapshots merely to make a test pass". `GoldenText`'s
  update mode rewrites the golden **and still fails**, so a golden can never be
  accepted to turn a run green.
- § Flake policy: "Tests do not use wall-clock sleeps for simulation behavior" and
  async work uses "explicit completion signals and bounded timeouts". Nothing here
  sleeps.

## Randomness

`DeterministicCase` and `PropertyCase` seed `System.Random`. That is **test-harness
randomness only**. The authoritative gameplay stream contract - exact PCG32 with
SplitMix64 child derivation and registered stream families - is owned by `SIM-005`
and must never be replaced by, compared against, or confused with this. A test that
needs authoritative randomness takes it from `SIM-005`'s scripted test sources once
that package is Done.

## Version identity

`HarnessIdentity` reports the assembly identity and runtime it observed. The real
build identity - product version, build number, source commit, Godot and .NET
versions, content bundle hash, schema/map/random/save versions - is owned by
`FND-004` (`TASK-FND-004-001`). When that lands, `HarnessIdentity` reports it
instead, and `VER-FND-003-002` records `TASK-FND-004-001` as its successor.
