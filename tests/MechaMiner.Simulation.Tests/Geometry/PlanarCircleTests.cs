using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Geometry;

/// <summary>
/// The circular footprint contract: inclusive containment and inclusive overlap, both decided from
/// squared distance.
/// </summary>
/// <remarks>
/// Verification: <c>VER-GEO-001-003</c>.
///
/// <c>docs/technical/21-world-geometry-navigation-and-spatial-queries.md</c> § Contact and overlap:
/// "Circle overlap uses squared distance and inclusive summed radii."
/// </remarks>
[TestFixture]
internal sealed class PlanarCircleTests
{
    private static PlanarCircle At(double x, double y, double radius)
    {
        return PlanarCircle.FromCentreAndRadius(PlanarVector.FromComponents(x, y), radius);
    }

    [Test]
    public void CentreAndRadiusAreCarriedVerbatim()
    {
        PlanarCircle circle = At(2.0, -3.0, 1.5);

        Expect.Multiple(() =>
        {
            Assert.That(circle.Centre, Is.EqualTo(PlanarVector.FromComponents(2.0, -3.0)));
            Assert.That(circle.Radius, Is.EqualTo(1.5));
        });
    }

    /// <summary>
    /// The boundary case the rule is actually about: exactly on the edge counts as inside.
    /// </summary>
    [Test]
    public void ContainmentIsInclusiveExactlyOnTheBoundary()
    {
        // Radius 5 with a 3-4-5 offset, so the boundary point is exact in binary floating point
        // and this is a test of the comparison rather than of a tolerance.
        PlanarCircle circle = At(0.0, 0.0, 5.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                circle.Contains(PlanarVector.FromComponents(3.0, 4.0)),
                Is.True,
                "a point exactly at the radius is contained; doc 21 § Contact and overlap makes the "
                    + "test inclusive, so there is no undefined gap at the boundary");
            Assert.That(circle.Contains(PlanarVector.Zero), Is.True, "the centre is contained");
            Assert.That(
                circle.Contains(PlanarVector.FromComponents(3.0, 4.000000001)),
                Is.False,
                "a point just outside is not contained");
        });
    }

    /// <summary>
    /// The boundary case the rule is actually about: tangency counts as contact.
    /// </summary>
    [Test]
    public void OverlapIsInclusiveAtExactlyTheSummedRadii()
    {
        PlanarCircle left = At(0.0, 0.0, 2.0);
        PlanarCircle tangent = At(5.0, 0.0, 3.0);
        PlanarCircle justApart = At(5.000000001, 0.0, 3.0);
        PlanarCircle overlapping = At(4.0, 0.0, 3.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                left.Overlaps(tangent),
                Is.True,
                "two footprints exactly at their summed radii overlap: doc 21 § Contact and overlap "
                    + "uses \"inclusive summed radii\", so a body that just touches a hazard is in it");
            Assert.That(left.Overlaps(justApart), Is.False, "just beyond the summed radii is no contact");
            Assert.That(left.Overlaps(overlapping), Is.True, "intersecting footprints overlap");
        });
    }

    [Test]
    public void OverlapIsSymmetricAndReflexive()
    {
        PlanarCircle left = At(1.0, 2.0, 1.0);
        PlanarCircle right = At(2.5, 2.0, 0.75);

        Expect.Multiple(() =>
        {
            Assert.That(left.Overlaps(right), Is.EqualTo(right.Overlaps(left)), "overlap is symmetric");
            Assert.That(left.Overlaps(left), Is.True, "a footprint overlaps itself");
        });
    }

    [Test]
    public void ADegenerateZeroRadiusFootprintBehavesAsAPoint()
    {
        PlanarCircle point = At(3.0, 0.0, 0.0);
        PlanarCircle circle = At(0.0, 0.0, 3.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                circle.Overlaps(point),
                Is.True,
                "a zero-radius footprint exactly on the boundary still contacts, because the test is "
                    + "inclusive; doc 21 § Collision primitives gives projectiles circles and a point "
                    + "projectile is the degenerate one");
            Assert.That(point.Contains(PlanarVector.FromComponents(3.0, 0.0)), Is.True);
            Assert.That(point.Contains(PlanarVector.FromComponents(3.0, 0.1)), Is.False);
        });
    }

    [Test]
    public void ANegativeRadiusIsRefusedBecauseItWouldInvertEveryOverlapTest()
    {
        ArgumentOutOfRangeException failure = Expect.Throws<ArgumentOutOfRangeException>(
            () => At(0.0, 0.0, -1.0));

        Expect.Multiple(() =>
        {
            Assert.That(failure.ParamName, Is.EqualTo("radius"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => At(0.0, 0.0, double.NaN)).ParamName,
                Is.EqualTo("radius"));
        });
    }

    [Test]
    public void MovingAFootprintKeepsItsRadius()
    {
        PlanarCircle moved = At(0.0, 0.0, 2.5).MovedTo(PlanarVector.FromComponents(-4.0, 1.0));

        Expect.Multiple(() =>
        {
            Assert.That(moved.Centre, Is.EqualTo(PlanarVector.FromComponents(-4.0, 1.0)));
            Assert.That(moved.Radius, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void EqualityCoversCentreAndRadius()
    {
        PlanarCircle circle = At(1.0, 1.0, 2.0);

        Expect.Multiple(() =>
        {
            Assert.That(circle == At(1.0, 1.0, 2.0), Is.True);
            Assert.That(circle != At(1.0, 1.0, 2.5), Is.True, "a different radius is a different circle");
            Assert.That(circle != At(1.0, 1.5, 2.0), Is.True, "a different centre is a different circle");
            Assert.That(circle.Equals((object)At(1.0, 1.0, 2.0)), Is.True);
            Assert.That(circle.Equals("not a circle"), Is.False);
            Assert.That(circle.GetHashCode(), Is.EqualTo(At(1.0, 1.0, 2.0).GetHashCode()));
        });
    }

    /// <summary>
    /// Overlap must agree with a slow independent reference over many random pairs, per doc 91
    /// § Test approach: "Generated-property tests ... compare validator results with independent
    /// slow reference queries."
    /// </summary>
    [Test]
    public void OverlapAgreesWithASlowReferenceOverManyPairs()
    {
        // A fixed sequence rather than a random one: this is an authoritative-adjacent property, and
        // a test that draws from an unseeded generator is a test whose failures cannot be reproduced.
        // The stride is chosen so pairs land on, just inside, and just outside tangency.
        int compared = 0;
        for (int xStep = -20; xStep <= 20; xStep++)
        {
            for (int yStep = -20; yStep <= 20; yStep++)
            {
                double x = xStep * 0.5;
                double y = yStep * 0.5;
                PlanarCircle left = At(0.0, 0.0, 3.0);
                PlanarCircle right = At(x, y, 2.0);

                // The reference uses a square root and a separate comparison, which is exactly the
                // formulation doc 21 forbids the production path from using.
                double distance = Math.Sqrt((x * x) + (y * y));
                bool referenceOverlaps = distance <= left.Radius + right.Radius;

                Assert.That(
                    left.Overlaps(right),
                    Is.EqualTo(referenceOverlaps),
                    "squared-distance overlap disagreed with the slow reference at (" + x + "," + y + ")");
                compared++;
            }
        }

        Assert.That(compared, Is.EqualTo(41 * 41), "every pair in the grid was compared");
    }

    /// <summary>
    /// <c>VER-GEO-001-005</c>: circle-segment overlap is inclusive, clamped to the segment, and squared.
    /// </summary>
    [Test]
    public void SegmentOverlapIsInclusiveAtExactTangency()
    {
        PlanarCircle circle = At(0.0, 0.0, 1.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                circle.OverlapsSegment(
                    PlanarVector.FromComponents(-5.0, 1.0),
                    PlanarVector.FromComponents(5.0, 1.0)),
                Is.True,
                "a segment grazing the circle at exactly one radius touches it. doc 21:103 makes this "
                    + "repository's containment and overlap tests inclusive");
            Assert.That(
                circle.OverlapsSegment(
                    PlanarVector.FromComponents(-5.0, 1.0000000001),
                    PlanarVector.FromComponents(5.0, 1.0000000001)),
                Is.False,
                "and a hair further out does not");
        });
    }

    [Test]
    public void SegmentOverlapIsMeasuredToTheSegmentAndNotToTheInfiniteLine()
    {
        // The line through these two points passes through the origin, so a test against the infinite
        // line would report an overlap. The segment itself stops well short.
        PlanarCircle circle = At(0.0, 0.0, 1.0);
        PlanarVector from = PlanarVector.FromComponents(5.0, 5.0);
        PlanarVector to = PlanarVector.FromComponents(10.0, 10.0);

        Assert.That(
            circle.OverlapsSegment(from, to),
            Is.False,
            "the nearest point of the SEGMENT is its own endpoint at (5,5), which is 7.07 m away. A "
                + "projection that was not clamped to [0,1] would have measured to the origin and "
                + "reported a hit for a projectile that never got near");
    }

    [Test]
    public void SegmentOverlapDetectsABodyEntirelyBetweenTheEndpoints()
    {
        // The tunnelling case. Neither endpoint is inside the circle; the segment passes straight through.
        PlanarCircle circle = At(0.0, 0.0, 0.22);
        PlanarVector from = PlanarVector.FromComponents(-1.0, 0.0);
        PlanarVector to = PlanarVector.FromComponents(1.0, 0.0);

        Expect.Multiple(() =>
        {
            Assert.That(circle.Contains(from), Is.False);
            Assert.That(circle.Contains(to), Is.False);
            Assert.That(
                circle.OverlapsSegment(from, to),
                Is.True,
                "a point test at either endpoint misses a body the step passed straight through");
        });
    }

    [Test]
    public void ADegenerateSegmentReducesToPointContainment()
    {
        PlanarCircle circle = At(2.0, 2.0, 1.0);
        PlanarVector inside = PlanarVector.FromComponents(2.5, 2.0);
        PlanarVector outside = PlanarVector.FromComponents(9.0, 9.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                circle.OverlapsSegment(inside, inside),
                Is.EqualTo(circle.Contains(inside)),
                "a zero-length segment has no direction to project along, so it must not divide by its own "
                    + "length; it answers the containment question instead");
            Assert.That(circle.OverlapsSegment(outside, outside), Is.EqualTo(circle.Contains(outside)));
        });
    }

    [Test]
    public void SegmentOverlapAgreesWithASlowSampledReferenceAcrossAGrid()
    {
        // The reference samples the segment densely and asks Contains at each sample. It is far too slow
        // for production and cannot be exactly right at tangency, so the comparison excludes a thin band
        // around the boundary where sampling resolution, not the implementation, decides the answer.
        PlanarCircle circle = At(0.0, 0.0, 1.0);
        const int samples = 4000;
        int compared = 0;

        for (int xStep = -8; xStep <= 8; xStep++)
        {
            for (int yStep = -8; yStep <= 8; yStep++)
            {
                PlanarVector from = PlanarVector.FromComponents(xStep * 0.5, yStep * 0.5);
                PlanarVector to = PlanarVector.FromComponents(-yStep * 0.4, xStep * 0.4);

                bool sampledHit = false;
                double nearest = double.PositiveInfinity;
                for (int sample = 0; sample <= samples; sample++)
                {
                    PlanarVector point = from + ((to - from) * (sample / (double)samples));
                    double distance = point.Magnitude;
                    nearest = Math.Min(nearest, distance);
                    sampledHit |= circle.Contains(point);
                }

                if (Math.Abs(nearest - circle.Radius) < 1e-3)
                {
                    // Within the band the dense sampling cannot resolve. Skipped rather than asserted, and
                    // the count below proves the skip did not swallow the whole grid.
                    continue;
                }

                Assert.That(
                    circle.OverlapsSegment(from, to),
                    Is.EqualTo(sampledHit),
                    "swept overlap disagreed with the sampled reference for the segment from "
                        + from.ToString() + " to " + to.ToString());
                compared++;
            }
        }

        Assert.That(
            compared,
            Is.GreaterThan(200),
            "the tangency band excluded some pairs; more than two hundred were still compared, so this is "
                + "an assertion over a population rather than an empty loop");
    }
}
