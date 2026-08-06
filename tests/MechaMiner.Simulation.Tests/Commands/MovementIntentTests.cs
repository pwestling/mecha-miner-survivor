using System;
using System.Globalization;
using MechaMiner.Simulation.Commands;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Pins the movement-intent normalization of doc 20 § Active commands.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-004-005</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Active commands: "Movement intent is normalized to a planar
/// vector with magnitude <c>[0,1]</c>; digital diagonals normalize to unit length. The simulation applies
/// immediate direction and full current speed for nonzero input and stops on zero input."
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance requires every approximate
/// comparison to name its tolerance.
/// </remarks>
[TestFixture]
internal sealed class MovementIntentTests
{
    /// <summary>
    /// The tolerance for a normalized magnitude.
    /// </summary>
    /// <remarks>
    /// Normalization is one division of two <c>double</c>s by a <c>double</c> magnitude, so the result is
    /// within a few units in the last place of unit length. One ulp at magnitude 1 is
    /// <c>2^-52 = 2.22e-16</c>, so <c>1e-15</c> is about four and a half ulps: loose enough that the
    /// rounding of one division cannot fail it, tight enough that a wrong divisor - dividing a diagonal by 2
    /// instead of by <c>sqrt(2)</c>, say, which lands at 0.707 rather than 1 - cannot pass it.
    /// </remarks>
    private static readonly Tolerance UnitLength = Tolerance.Named(
        "movement-intent-unit-length",
        1e-15,
        "one normalizing division, so a few ulps at magnitude 1 (1 ulp = 2.22e-16)");

    /// <summary>
    /// Verification: <c>VER-SIM-004-005</c>.
    ///
    /// An over-unit analog magnitude clamps, each of the eight digital directions normalizes to unit length
    /// within a named tolerance, a sub-unit analog magnitude is preserved rather than scaled up, and zero
    /// input is the explicit stop rather than a tiny direction.
    /// </summary>
    [Test]
    public void MagnitudeClampsAndDigitalDiagonalsNormalizeToUnitLength()
    {
        AssertZeroInputIsAnExplicitStop();
        AssertEveryDigitalDirectionNormalizesToUnitLength();
        AssertOverUnitAnalogMagnitudeClamps();
        AssertSubUnitAnalogMagnitudeIsPreserved();
        AssertNonFiniteInputHasNoNormalizedValue();
    }

    /// <summary>
    /// Zero is a command, not the absence of one: it is <see cref="MovementIntent.Stop"/> exactly, and a
    /// merely tiny input is not.
    /// </summary>
    private static void AssertZeroInputIsAnExplicitStop()
    {
        Assert.That(MovementIntent.TryNormalize(0.0, 0.0, out MovementIntent stop), Is.True);

        // The contrast that keeps the stop assertion from being vacuous: an input small enough to look like
        // nothing is still a direction, so IsStop is reporting the exact zero rather than a threshold.
        MovementIntent tiny = MovementIntent.Normalize(1e-300, 0.0);

        Expect.Multiple(() =>
        {
            Assert.That(stop.IsStop, Is.True, "zero input is an explicit stop");
            Assert.That(stop, Is.EqualTo(MovementIntent.Stop), "and it is the Stop value itself");
            Assert.That(stop.X, Is.Zero, "with no residual X direction");
            Assert.That(stop.Y, Is.Zero, "and no residual Y direction");
            Assert.That(stop.Magnitude, Is.Zero, "and exactly zero magnitude");
            Assert.That(
                tiny.IsStop,
                Is.False,
                "but a tiny nonzero input is a direction, not a stop; doc 20 § Active commands gives no "
                    + "deadzone and this type must not invent one");
            Assert.That(tiny.X, Is.EqualTo(1e-300), "and it is preserved rather than flushed to zero");
        });
    }

    /// <summary>
    /// The eight digital directions. All four diagonals are over-unit before normalization, so the clamp is
    /// what makes them unit length; the four cardinals are already exactly unit length.
    /// </summary>
    private static void AssertEveryDigitalDirectionNormalizesToUnitLength()
    {
        double[,] digital =
        {
            { 1.0, 0.0 },
            { -1.0, 0.0 },
            { 0.0, 1.0 },
            { 0.0, -1.0 },
            { 1.0, 1.0 },
            { 1.0, -1.0 },
            { -1.0, 1.0 },
            { -1.0, -1.0 },
        };

        Assert.That(digital.GetLength(0), Is.EqualTo(8), "there are eight digital directions");

        for (int index = 0; index < digital.GetLength(0); index++)
        {
            double rawX = digital[index, 0];
            double rawY = digital[index, 1];
            MovementIntent intent = MovementIntent.Normalize(rawX, rawY);
            string description = "digital ("
                + rawX.ToString("R", CultureInfo.InvariantCulture)
                + ","
                + rawY.ToString("R", CultureInfo.InvariantCulture)
                + ")";

            NumericAssert.AreEqualWithin(
                MovementIntent.MaximumMagnitude,
                intent.Magnitude,
                UnitLength,
                description + " normalizes to unit length");

            Expect.Multiple(() =>
            {
                Assert.That(
                    intent.IsStop,
                    Is.False,
                    description + " is nonzero input, so it applies full current speed rather than stopping");
                Assert.That(
                    Math.Sign(intent.X),
                    Is.EqualTo(Math.Sign(rawX)),
                    description + " keeps its X direction");
                Assert.That(
                    Math.Sign(intent.Y),
                    Is.EqualTo(Math.Sign(rawY)),
                    description + " keeps its Y direction");
            });
        }

        // The diagonal components are the one value a wrong divisor would get wrong while still yielding a
        // plausible-looking vector, so they are pinned directly as well as through the magnitude.
        MovementIntent upRight = MovementIntent.Normalize(1.0, 1.0);
        double expectedComponent = 1.0 / Math.Sqrt(2.0);
        NumericAssert.AreEqualWithin(
            expectedComponent,
            upRight.X,
            UnitLength,
            "a digital diagonal's X component is 1/sqrt(2), not 1/2");
        NumericAssert.AreEqualWithin(
            expectedComponent,
            upRight.Y,
            UnitLength,
            "a digital diagonal's Y component is 1/sqrt(2), not 1/2");
    }

    /// <summary>An analog magnitude above one clamps to unit length while keeping its direction.</summary>
    private static void AssertOverUnitAnalogMagnitudeClamps()
    {
        // A 3-4-5 triangle, so the clamped components are the exact 0.6 and 0.8 of the unit direction and a
        // failure names a recognizable number rather than a rounding artifact.
        MovementIntent clamped = MovementIntent.Normalize(3.0, 4.0);
        NumericAssert.AreEqualWithin(0.6, clamped.X, UnitLength, "an over-unit (3,4) clamps its X to 0.6");
        NumericAssert.AreEqualWithin(0.8, clamped.Y, UnitLength, "an over-unit (3,4) clamps its Y to 0.8");
        NumericAssert.AreEqualWithin(
            MovementIntent.MaximumMagnitude,
            clamped.Magnitude,
            UnitLength,
            "an over-unit analog magnitude clamps to 1");

        // The same clamp at the far end of the double range. A normalization that squared its components
        // would produce an infinite magnitude here and then a NaN direction, so this case distinguishes a
        // correct implementation from one that merely passes at ordinary scales.
        MovementIntent enormous = MovementIntent.Normalize(1e308, 1e308);
        NumericAssert.AreEqualWithin(
            MovementIntent.MaximumMagnitude,
            enormous.Magnitude,
            UnitLength,
            "an enormous but finite over-unit input clamps rather than overflowing to NaN");
        Expect.Multiple(() =>
        {
            Assert.That(
                enormous,
                Is.EqualTo(MovementIntent.Normalize(1.0, 1.0)),
                "and it clamps to the same unit diagonal a digital (1,1) does");
            Assert.That(double.IsFinite(enormous.X), Is.True, "with a finite X");
            Assert.That(double.IsFinite(enormous.Y), Is.True, "and a finite Y");
        });
    }

    /// <summary>An analog magnitude below one is preserved: the clamp is a ceiling, not a normalization.</summary>
    private static void AssertSubUnitAnalogMagnitudeIsPreserved()
    {
        MovementIntent half = MovementIntent.Normalize(0.3, 0.4);

        Expect.Multiple(() =>
        {
            Assert.That(half.X, Is.EqualTo(0.3), "a sub-unit analog X is carried through unchanged");
            Assert.That(half.Y, Is.EqualTo(0.4), "and so is its Y");
        });

        NumericAssert.AreEqualWithin(
            0.5,
            half.Magnitude,
            UnitLength,
            "a sub-unit analog magnitude is preserved rather than scaled up to 1");
    }

    /// <summary>A non-finite component has no normalized value, and that is reported rather than absorbed.</summary>
    private static void AssertNonFiniteInputHasNoNormalizedValue()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                MovementIntent.TryNormalize(double.NaN, 0.0, out MovementIntent fromNaN),
                Is.False,
                "NaN has no normalized magnitude in [0,1]");
            Assert.That(fromNaN, Is.EqualTo(MovementIntent.Stop), "and the out value is the stop, not a NaN");
            Assert.That(
                MovementIntent.TryNormalize(0.0, double.PositiveInfinity, out MovementIntent fromInfinity),
                Is.False,
                "nor does an infinity");
            Assert.That(fromInfinity, Is.EqualTo(MovementIntent.Stop), "and it too yields the stop");
        });

        ArgumentOutOfRangeException fromX = Expect.Throws<ArgumentOutOfRangeException>(
            () => MovementIntent.Normalize(double.NaN, 1.0));
        ArgumentOutOfRangeException fromY = Expect.Throws<ArgumentOutOfRangeException>(
            () => MovementIntent.Normalize(1.0, double.NegativeInfinity));

        Expect.Multiple(() =>
        {
            Assert.That(fromX.ParamName, Is.EqualTo("rawX"), "the throwing overload names the bad component");
            Assert.That(fromY.ParamName, Is.EqualTo("rawY"), "whichever one it was");
        });
    }
}
