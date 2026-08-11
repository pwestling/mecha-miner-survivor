using MechaMiner.Simulation.Geometry;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Approximate comparison of planar values, componentwise.
/// </summary>
/// <remarks>
/// <c>Is.EqualTo(planarVector).Within(tolerance)</c> is <c>NUnit2047</c>: NUnit cannot apply a
/// tolerance to a user-defined struct, and under this repository's warnings-as-errors policy that is
/// a build failure rather than a hint. Exact equality is the right assertion wherever the arithmetic
/// is exact - and most of it is, deliberately - but a value that has been through a normalization or
/// a trigonometric round trip needs a tolerance on each component, which is what this provides.
/// </remarks>
internal static class PlanarAssert
{
    /// <summary>Asserts that two planar values agree on both components within a tolerance.</summary>
    /// <param name="actual">The observed value.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="tolerance">The permitted absolute difference per component.</param>
    /// <param name="because">What the caller is asserting, reported on failure.</param>
    internal static void AreClose(
        PlanarVector actual,
        PlanarVector expected,
        double tolerance,
        string because)
    {
        Assert.That(
            actual.X,
            Is.EqualTo(expected.X).Within(tolerance),
            "eastward component: " + because + " (expected " + expected + ", observed " + actual + ")");
        Assert.That(
            actual.Y,
            Is.EqualTo(expected.Y).Within(tolerance),
            "northward component: " + because + " (expected " + expected + ", observed " + actual + ")");
    }
}
