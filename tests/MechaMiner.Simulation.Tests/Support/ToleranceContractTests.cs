using System;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// The sample unit tier: proves the numeric-tolerance contract of
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance.
/// </summary>
/// <remarks>
/// Verification: <c>VER-FND-003-004</c>.
///
/// Doc 91 states that "'Approximately equal' without a named tolerance is not an
/// acceptable test." These tests prove that the harness makes that structurally true
/// rather than a convention: a tolerance cannot be constructed without a name, a
/// rationale, and a finite positive magnitude, and there is no float comparison that
/// takes a bare epsilon.
/// </remarks>
[TestFixture]
internal sealed class ToleranceContractTests
{
    /// <summary>
    /// A worked tolerance, named and justified, used by the comparison tests below.
    /// The real world-scale catalogue belongs to <c>GEO-001</c>; this one exists to
    /// exercise the mechanism.
    /// </summary>
    private static readonly Tolerance HarnessTolerance = Tolerance.Named(
        "harness-sample-metres",
        1e-9,
        "1e-9 m is far below any world-scale quantity and exists only to exercise the mechanism; "
            + "GEO-001 owns the real geometry tolerances");

    [Test]
    public void ANamedToleranceRecordsItsNameMagnitudeAndRationale()
    {
        Expect.Multiple(() =>
        {
            Assert.That(HarnessTolerance.Name, Is.EqualTo("harness-sample-metres"));
            Assert.That(HarnessTolerance.Absolute, Is.EqualTo(1e-9));
            Assert.That(HarnessTolerance.Rationale, Does.Contain("GEO-001"));
            Assert.That(HarnessTolerance.ToString(), Does.Contain("harness-sample-metres"));
        });
    }

    [Test]
    public void AToleranceWithoutANameIsRejected()
    {
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(() => Tolerance.Named(string.Empty, 1e-6, "rationale"));
            Expect.Throws<ArgumentException>(() => Tolerance.Named("   ", 1e-6, "rationale"));
        });
    }

    [Test]
    public void AToleranceWithoutARationaleIsRejected()
    {
        Expect.Throws<ArgumentException>(
            () => Tolerance.Named("named-but-unjustified", 1e-6, string.Empty));
    }

    [TestCase(0.0)]
    [TestCase(-1e-6)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void AToleranceMagnitudeMustBeFiniteAndPositive(double magnitude)
    {
        Expect.Throws<ArgumentOutOfRangeException>(
            () => Tolerance.Named("bad-magnitude", magnitude, "rationale"));
    }

    [Test]
    public void ValuesInsideTheNamedToleranceCompareEqual()
    {
        NumericAssert.AreEqualWithin(1.0, 1.0 + 5e-10, HarnessTolerance, "sample position");
    }

    [Test]
    public void ValuesOutsideTheNamedToleranceFailAndTheMessageNamesTheTolerance()
    {
        AssertionException failure = Expect.Throws<AssertionException>(
            () => NumericAssert.AreEqualWithin(1.0, 1.0 + 1e-6, HarnessTolerance, "sample position"));

        Expect.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("harness-sample-metres"));
            Assert.That(failure.Message, Does.Contain("sample position"));
            Assert.That(failure.Message, Does.Contain("exceeds tolerance"));
        });
    }

    [Test]
    public void IntegerQuantitiesCompareExactly()
    {
        NumericAssert.AreExactlyEqual(2100L, 2100L, "banked Hyper Gold");
        Expect.Throws<AssertionException>(
            () => NumericAssert.AreExactlyEqual(2100L, 2101L, "banked Hyper Gold"));
    }

    [Test]
    public void StableIdentifiersCompareCaseSensitively()
    {
        NumericAssert.AreExactlyEqual("W-AB", "W-AB", "weapon ID");
        Expect.Throws<AssertionException>(
            () => NumericAssert.AreExactlyEqual("W-AB", "w-ab", "weapon ID"));
    }
}
