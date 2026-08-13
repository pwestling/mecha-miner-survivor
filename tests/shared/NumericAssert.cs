using System;
using System.Globalization;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Numeric comparisons that satisfy
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance.
/// </summary>
/// <remarks>
/// There is deliberately no method that compares two floating-point values with a
/// bare epsilon or with a default tolerance. Every float comparison takes a
/// <see cref="Tolerance"/>, which cannot exist without a name and a rationale. Every
/// quantity doc 91 lists as exact - "Integer currency, ranks, ticks, counts, schedule
/// boundaries, and IDs" - has an exact-equality method instead.
/// </remarks>
internal static class NumericAssert
{
    /// <summary>
    /// Asserts that two floating-point values differ by no more than an explicitly
    /// named tolerance.
    /// </summary>
    internal static void AreEqualWithin(double expected, double actual, Tolerance tolerance, string subject)
    {
        ArgumentNullException.ThrowIfNull(tolerance);

        double difference = Math.Abs(expected - actual);
        Assert.That(
            difference,
            Is.LessThanOrEqualTo(tolerance.Absolute),
            () => string.Concat(
                subject,
                ": expected ",
                expected.ToString("R", CultureInfo.InvariantCulture),
                ", actual ",
                actual.ToString("R", CultureInfo.InvariantCulture),
                ", difference ",
                difference.ToString("R", CultureInfo.InvariantCulture),
                " exceeds tolerance ",
                tolerance.ToString()));
    }

    /// <summary>
    /// Asserts exact equality for a quantity doc 91 requires to be exact: integer
    /// currency, ranks, ticks, counts, and schedule boundaries.
    /// </summary>
    internal static void AreExactlyEqual(long expected, long actual, string subject)
    {
        Assert.That(
            actual,
            Is.EqualTo(expected),
            () => subject + ": doc 91 § Numeric tolerance requires exact equality for this quantity");
    }

    /// <summary>
    /// Asserts exact equality for a stable ID. Doc 91 requires IDs to match exactly,
    /// and <c>docs/technical/40-content-data-and-validation.md</c> § Stable ID policy
    /// makes them case-sensitive ASCII tokens, so the comparison is ordinal.
    /// </summary>
    internal static void AreExactlyEqual(string expected, string actual, string subject)
    {
        Assert.That(
            actual,
            Is.EqualTo(expected).Using(StringComparer.Ordinal),
            () => subject + ": stable IDs are case-sensitive ASCII tokens and compare ordinally");
    }
}
