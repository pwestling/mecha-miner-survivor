using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Tests.Support;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Geometry;

/// <summary>
/// The planar vector contract: gameplay meters, exact arithmetic, an explicit zero direction, and
/// no non-finite component.
/// </summary>
/// <remarks>
/// Verification: <c>VER-GEO-001-001</c>, <c>VER-GEO-001-002</c>, <c>VER-GEO-001-004</c>.
///
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract: "Simulation X increases east and simulation Y increases north."
/// <c>docs/technical/20-simulation-core.md</c> § Numeric and unit conventions: a direction is a
/// "normalized planar vector; zero direction is explicit".
/// </remarks>
[TestFixture]
internal sealed class PlanarVectorTests
{
    [Test]
    public void ComponentsAreCarriedVerbatim()
    {
        PlanarVector value = PlanarVector.FromComponents(3.25, -7.5);

        Expect.Multiple(() =>
        {
            Assert.That(value.X, Is.EqualTo(3.25), "the eastward component is carried exactly");
            Assert.That(value.Y, Is.EqualTo(-7.5), "the northward component is carried exactly");
        });
    }

    [Test]
    public void TheNamedAxesPointEastAndNorth()
    {
        Expect.Multiple(() =>
        {
            Assert.That(PlanarVector.East.X, Is.EqualTo(1.0), "east is +X per TDR-005");
            Assert.That(PlanarVector.East.Y, Is.EqualTo(0.0));
            Assert.That(PlanarVector.North.Y, Is.EqualTo(1.0), "north is +Y per TDR-005");
            Assert.That(PlanarVector.North.X, Is.EqualTo(0.0));
            Assert.That(PlanarVector.Zero.IsZero, Is.True);
        });
    }

    [Test]
    public void ArithmeticIsComponentwise()
    {
        PlanarVector left = PlanarVector.FromComponents(2.0, 5.0);
        PlanarVector right = PlanarVector.FromComponents(-1.5, 0.5);

        Expect.Multiple(() =>
        {
            Assert.That(left + right, Is.EqualTo(PlanarVector.FromComponents(0.5, 5.5)));
            Assert.That(left - right, Is.EqualTo(PlanarVector.FromComponents(3.5, 4.5)));
            Assert.That(-left, Is.EqualTo(PlanarVector.FromComponents(-2.0, -5.0)));
            Assert.That(left * 2.0, Is.EqualTo(PlanarVector.FromComponents(4.0, 10.0)));
            Assert.That(2.0 * left, Is.EqualTo(left * 2.0), "scaling commutes");
        });
    }

    [Test]
    public void TheNamedOperatorAlternativesAgreeWithTheOperators()
    {
        PlanarVector left = PlanarVector.FromComponents(2.0, 5.0);
        PlanarVector right = PlanarVector.FromComponents(-1.5, 0.5);

        Expect.Multiple(() =>
        {
            Assert.That(PlanarVector.Add(left, right), Is.EqualTo(left + right));
            Assert.That(PlanarVector.Subtract(left, right), Is.EqualTo(left - right));
            Assert.That(PlanarVector.Negate(left), Is.EqualTo(-left));
            Assert.That(PlanarVector.Multiply(left, 3.0), Is.EqualTo(left * 3.0));
        });
    }

    [Test]
    public void MagnitudeAndItsSquareAgreeOnAKnownTriple()
    {
        // 3-4-5, so both answers are exact in binary floating point and the test is not
        // measuring a tolerance.
        PlanarVector value = PlanarVector.FromComponents(3.0, 4.0);

        Expect.Multiple(() =>
        {
            Assert.That(value.MagnitudeSquared, Is.EqualTo(25.0));
            Assert.That(value.Magnitude, Is.EqualTo(5.0));
            Assert.That(value.IsZero, Is.False);
        });
    }

    [Test]
    public void DistanceIsSymmetricAndAgreesWithItsSquare()
    {
        PlanarVector from = PlanarVector.FromComponents(-1.0, 2.0);
        PlanarVector to = PlanarVector.FromComponents(2.0, 6.0);

        Expect.Multiple(() =>
        {
            Assert.That(from.DistanceTo(to), Is.EqualTo(5.0));
            Assert.That(to.DistanceTo(from), Is.EqualTo(5.0), "distance is symmetric");
            Assert.That(from.DistanceSquaredTo(to), Is.EqualTo(25.0));
            Assert.That(to.DistanceSquaredTo(from), Is.EqualTo(25.0));
        });
    }

    [Test]
    public void NormalizingANonzeroVectorYieldsUnitLengthAndKeepsDirection()
    {
        PlanarVector value = PlanarVector.FromComponents(3.0, 4.0);
        PlanarVector unit = value.Normalized();

        Expect.Multiple(() =>
        {
            Assert.That(unit.Magnitude, Is.EqualTo(1.0).Within(1e-15));
            Assert.That(unit.X, Is.EqualTo(0.6).Within(1e-15));
            Assert.That(unit.Y, Is.EqualTo(0.8).Within(1e-15));
            Assert.That(
                unit.BearingRadians(),
                Is.EqualTo(value.BearingRadians()).Within(1e-15),
                "normalizing changes length and not direction");
        });
    }

    /// <summary>
    /// Normalization returns a unit vector across the whole representable range, not only the
    /// middle of it.
    /// </summary>
    /// <remarks>
    /// The subnormal case is a regression test for a real defect in this type. Dividing directly by
    /// the magnitude gave <c>(1, 1)</c> for <c>(epsilon, epsilon)</c> - length sqrt 2 - because
    /// <c>double.Hypot</c> of two subnormals rounds to a subnormal. Movement integration multiplies a
    /// direction by a speed, so a direction longer than one is a body moving faster than its stat
    /// allows, which is why this is asserted rather than dismissed as unreachable.
    /// </remarks>
    [Test]
    public void NormalizationIsUnitLengthEvenAtTheExtremesOfTheRange()
    {
        (double X, double Y, string Case)[] samples =
        {
            (double.Epsilon, double.Epsilon, "two subnormals"),
            (double.Epsilon, 0.0, "one subnormal, one zero"),
            (1e-320, 3e-320, "unequal subnormals"),
            (1e300, 1e300, "two very large components"),
            (1e300, 1.0, "one very large, one ordinary"),
            (1e-200, 1e200, "components 400 orders of magnitude apart"),
            (3.0, 4.0, "an ordinary pair"),
        };

        foreach ((double x, double y, string label) in samples)
        {
            PlanarVector unit = PlanarVector.FromComponents(x, y).Normalized();

            Assert.That(
                unit.Magnitude,
                Is.EqualTo(1.0).Within(1e-15),
                "normalizing " + label + " must give a unit vector; a longer one would make the body "
                    + "move faster than its speed stat");
        }
    }

    /// <summary>
    /// <c>VER-GEO-001-002</c>: the zero direction is explicit, not an arbitrary axis.
    /// </summary>
    [Test]
    public void NormalizingZeroYieldsAnExplicitZeroDirection()
    {
        PlanarVector unit = PlanarVector.Zero.Normalized();

        Expect.Multiple(() =>
        {
            Assert.That(
                unit.IsZero,
                Is.True,
                "doc 20 § Numeric and unit conventions: \"zero direction is explicit\". Returning east "
                    + "for a stopped body would make a released control indistinguishable from one held "
                    + "east");
            Assert.That(unit, Is.EqualTo(PlanarVector.Zero));
        });
    }

    /// <summary>
    /// <c>VER-GEO-001-004</c>: a bearing round-trips through a direction, and east is zero.
    /// </summary>
    [Test]
    public void BearingRoundTripsThroughDirection()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                PlanarVector.East.BearingRadians(),
                Is.EqualTo(0.0),
                "east is exactly zero radians under the counterclockwise-from-east convention");
            Assert.That(
                PlanarVector.North.BearingRadians(),
                Is.EqualTo(Math.PI / 2.0).Within(1e-15),
                "north is a quarter turn counterclockwise from east");
            Assert.That(
                PlanarVector.Zero.BearingRadians(),
                Is.EqualTo(0.0),
                "the zero vector has no direction and reports zero rather than throwing; a caller that "
                    + "must tell \"facing east\" from \"no direction\" reads IsZero");
        });

        // Every eighth of a turn, including the diagonals a keyboard produces.
        for (int step = 0; step < 8; step++)
        {
            double radians = step * Math.PI / 4.0;
            PlanarVector direction = PlanarVector.FromBearing(radians);

            Assert.That(
                direction.Magnitude,
                Is.EqualTo(1.0).Within(1e-15),
                "FromBearing produces a unit vector at step " + step);
            PlanarAssert.AreClose(
                PlanarVector.FromBearing(direction.BearingRadians()),
                direction,
                1e-15,
                "the bearing of a direction reconstructs that direction at step " + step);
        }
    }

    /// <summary>
    /// <c>VER-GEO-001-001</c>: a non-finite component is refused at the statement that produced it.
    /// </summary>
    [Test]
    public void ANonFiniteComponentIsRefusedAtConstruction()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlanarVector.FromComponents(double.NaN, 0.0)).ParamName,
                Is.EqualTo("x"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlanarVector.FromComponents(0.0, double.NaN)).ParamName,
                Is.EqualTo("y"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlanarVector.FromComponents(double.PositiveInfinity, 0.0)).ParamName,
                Is.EqualTo("x"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlanarVector.FromComponents(0.0, double.NegativeInfinity)).ParamName,
                Is.EqualTo("y"));
        });
    }

    [Test]
    public void ANonFiniteScaleOrBearingIsRefused()
    {
        PlanarVector value = PlanarVector.East;

        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(() => _ = value * double.NaN).ParamName,
                Is.EqualTo("factor"));
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PlanarVector.FromBearing(double.NaN)).ParamName,
                Is.EqualTo("radians"));
        });
    }

    [Test]
    public void EqualityIsComponentwiseAndHashesAgree()
    {
        PlanarVector value = PlanarVector.FromComponents(1.5, -2.5);
        PlanarVector same = PlanarVector.FromComponents(1.5, -2.5);
        PlanarVector different = PlanarVector.FromComponents(1.5, 2.5);

        Expect.Multiple(() =>
        {
            Assert.That(value == same, Is.True);
            Assert.That(value != different, Is.True);
            Assert.That(value.Equals((object)same), Is.True);
            Assert.That(value.Equals("not a vector"), Is.False);
            Assert.That(value.GetHashCode(), Is.EqualTo(same.GetHashCode()));
        });
    }

    [Test]
    public void TheRenderingIsRoundTrippableAndCultureInvariant()
    {
        // "R" and the invariant culture, so a decimal comma cannot appear in a transcript that a
        // golden comparison reads.
        Assert.That(
            PlanarVector.FromComponents(1.5, -2.25).ToString(),
            Is.EqualTo("(1.5,-2.25)m"));
    }
}
