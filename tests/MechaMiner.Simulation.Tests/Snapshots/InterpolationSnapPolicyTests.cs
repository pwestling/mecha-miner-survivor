using System;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves the five snap triggers and that the distance threshold is the derived value rather than a chosen
/// constant.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-007-006</c>, <c>VER-SIM-007-007</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot;
/// <c>docs/technical/30-presentation-and-rendering.md</c> § Snapshot synchronization and § Camera;
/// <c>docs/72-player-survivability-and-damage-baseline.md</c> § Movement and Speed Modifiers.
/// </remarks>
[TestFixture]
internal sealed class InterpolationSnapPolicyTests
{
    /// <summary>
    /// The tolerance for the derived threshold's decimal value.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance: "'Approximately equal' without a
    /// named tolerance is not an acceptable test." The threshold is <c>5.40 / 60 x 2</c>, and neither 5.4 nor
    /// the quotient is exactly representable in binary, so comparing it to the decimal 0.18 needs a named
    /// tolerance. The <em>derivation</em> assertions below need none: they recompute from the same constants
    /// and compare bit-for-bit.
    /// </remarks>
    private static readonly Tolerance ThresholdDecimalTolerance = Tolerance.Named(
        "interpolation-snap-threshold-metres",
        1e-12,
        "1e-12 M is fifteen orders of magnitude below the 0.18 M threshold and eleven below the smallest "
            + "distance any accepted document states; it exists only to compare a binary double against the "
            + "decimal literal 0.18");

    /// <summary>
    /// Verification: <c>VER-SIM-007-006</c>.
    ///
    /// Presentation snaps on spawn, teleport, boss re-entry, terminal transition, and a displacement above the
    /// threshold, and interpolates for every displacement below it.
    /// </summary>
    [Test]
    public void SnapsOnSpawnTeleportReEntryTerminalAndAboveTheDistanceThreshold()
    {
        InterpolationSnapPolicy policy = InterpolationSnapPolicy.Documented;
        double threshold = InterpolationSnapPolicy.DistanceThresholdMetres;

        Expect.Multiple(() =>
        {
            Assert.That(
                policy.Evaluate(true, false, false, false, 0.0),
                Is.EqualTo(InterpolationSnapReason.Spawn),
                "doc 30 § Snapshot synchronization: a spawned actor appears at the newest transform without "
                    + "extrapolating backward");
            Assert.That(
                policy.Evaluate(false, true, false, false, 0.0),
                Is.EqualTo(InterpolationSnapReason.Teleport));
            Assert.That(
                policy.Evaluate(false, false, true, false, 0.0),
                Is.EqualTo(InterpolationSnapReason.BossReEntry));
            Assert.That(
                policy.Evaluate(false, false, false, true, 0.0),
                Is.EqualTo(InterpolationSnapReason.TerminalTransition));
            Assert.That(
                policy.Evaluate(false, false, false, false, threshold * 1.5),
                Is.EqualTo(InterpolationSnapReason.DistanceThresholdExceeded));

            // Interpolates for every displacement below the threshold, including at it.
            Assert.That(
                policy.Evaluate(false, false, false, false, 0.0),
                Is.EqualTo(InterpolationSnapReason.None),
                "a stationary entity interpolates");
            Assert.That(
                policy.Evaluate(false, false, false, false, threshold * 0.5),
                Is.EqualTo(InterpolationSnapReason.None));
            Assert.That(
                policy.Evaluate(false, false, false, false, threshold),
                Is.EqualTo(InterpolationSnapReason.None),
                "the comparison is strictly greater, so movement at exactly the fastest legal rate across "
                    + "exactly the tolerated intervals does not snap");

            // Precedence: the enumerated cause wins over its own consequence.
            Assert.That(
                policy.Evaluate(true, true, true, true, threshold * 100.0),
                Is.EqualTo(InterpolationSnapReason.Spawn),
                "an entity that both spawned and moved far reports the cause, not the displacement");
            Assert.That(
                policy.Evaluate(false, true, true, true, threshold * 100.0),
                Is.EqualTo(InterpolationSnapReason.Teleport));
            Assert.That(
                policy.Evaluate(false, false, true, true, threshold * 100.0),
                Is.EqualTo(InterpolationSnapReason.BossReEntry));
            Assert.That(
                policy.Evaluate(false, false, false, true, threshold * 100.0),
                Is.EqualTo(InterpolationSnapReason.TerminalTransition));

            // All five reasons of doc 20 § Presentation snapshot exist, plus None, and no others.
            Assert.That(
                Enum.GetValues<InterpolationSnapReason>(),
                Has.Length.EqualTo(6),
                "doc 20 § Presentation snapshot names five triggers - spawn, teleport, re-entry, terminal "
                    + "transition, distance threshold - plus the no-snap case");

            Expect.Throws<InvalidOperationException>(
                () => default(InterpolationSnapPolicy).Evaluate(false, false, false, false, 0.0));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => policy.Evaluate(false, false, false, false, -1.0));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => policy.Evaluate(false, false, false, false, double.NaN));
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-007-007</c>.
    ///
    /// The threshold is not reached by the fastest documented authoritative movement across the tolerated
    /// number of snapshot intervals, and is exceeded by the first interval beyond that.
    /// </summary>
    [Test]
    public void ThresholdMatchesItsDocumentedDerivation()
    {
        InterpolationSnapPolicy policy = InterpolationSnapPolicy.Documented;
        double perTick = InterpolationSnapPolicy.LargestLegalSingleTickDisplacementMetres;
        double atTolerance = perTick * InterpolationSnapPolicy.ToleratedSnapshotIntervals;
        double oneIntervalBeyond = perTick * (InterpolationSnapPolicy.ToleratedSnapshotIntervals + 1);

        Expect.Multiple(() =>
        {
            // The inputs are the documented ones, so a document change is what moves the number.
            Assert.That(
                InterpolationSnapPolicy.FastestAuthoritativeMetresPerSecond,
                Is.EqualTo(5.40),
                "doc 72 § Movement and Speed Modifiers: Riftjaw's charge moves at 5.40M/s, the fastest "
                    + "documented authoritative continuous movement");
            Assert.That(
                InterpolationSnapPolicy.AuthoritativeTicksPerSecond,
                Is.EqualTo(60),
                "doc 10 § Clock domains: the authoritative tick rate is 60 Hz");
            Assert.That(
                InterpolationSnapPolicy.ToleratedSnapshotIntervals,
                Is.EqualTo(2),
                "one ordinary interval between the two most recent complete snapshots, plus one more because "
                    + "CTR-SIM-003 permits a consumer to drop a stale snapshot");

            // The threshold is the product, bit-for-bit: no tolerance, because both sides are computed from
            // the same constants by the same operations.
            Assert.That(
                InterpolationSnapPolicy.DistanceThresholdMetres,
                Is.EqualTo(atTolerance),
                "the threshold must be exactly the derivation, not a constant that happens to be near it");

            // Not reached at the tolerance, exceeded one interval beyond.
            Assert.That(
                policy.Evaluate(false, false, false, false, atTolerance),
                Is.EqualTo(InterpolationSnapReason.None),
                "the fastest documented movement across the tolerated number of intervals must not snap, or "
                    + "legal movement after a dropped snapshot would pop");
            Assert.That(
                policy.Evaluate(false, false, false, false, oneIntervalBeyond),
                Is.EqualTo(InterpolationSnapReason.DistanceThresholdExceeded),
                "one interval beyond the tolerance cannot be legal continuous movement, so interpolating it "
                    + "would draw motion that never happened");
        });

        NumericAssert.AreEqualWithin(
            0.18,
            InterpolationSnapPolicy.DistanceThresholdMetres,
            ThresholdDecimalTolerance,
            "the derived interpolation-snap distance threshold in gameplay meters");

        NumericAssert.AreEqualWithin(
            0.09,
            perTick,
            ThresholdDecimalTolerance,
            "the largest legal single-tick displacement in gameplay meters");

        // Cross-checks that the value is not merely arithmetic, from the accepted documents.
        const double cameraHeightMetres = 24.0;
        const double mechCollisionDiameterMetres = 1.0;
        Expect.Multiple(() =>
        {
            Assert.That(
                InterpolationSnapPolicy.DistanceThresholdMetres / cameraHeightMetres,
                Is.LessThan(0.01),
                "a snap at the threshold is under 1% of the 24 m camera height (doc 30 § Camera), so the "
                    + "threshold does not itself cause a visible pop");
            Assert.That(
                InterpolationSnapPolicy.DistanceThresholdMetres / mechCollisionDiameterMetres,
                Is.LessThan(0.25),
                "and well under the mech collision diameter, so a real teleport exceeds it by orders of "
                    + "magnitude and the distance test is a backstop rather than the primary trigger");
            Assert.That(
                InterpolationSnapPolicy.Documented.ToString(),
                Does.Contain("threshold="),
                "the policy renders its derived threshold, so a diagnostic can show the number it used");
        });
    }
}
