using System.Globalization;
using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>
/// Canonical numeric formatting. Verification: <c>VER-DAT-001-009</c>.
/// </summary>
[TestFixture]
internal sealed class CanonicalNumberTests
{
    /// <summary>
    /// The hard values: the classic decimal that has no exact binary form, a small
    /// exponent, the largest finite double, and both zeros.
    /// </summary>
    [TestCase(0.1)]
    [TestCase(1e-7)]
    [TestCase(1.7976931348623157E+308)]
    [TestCase(5e-324)]
    [TestCase(-0.0)]
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(-2.5)]
    [TestCase(1234567890123.0)]
    public void FormattingRoundTripsToTheSameBits(double value)
    {
        string text = CanonicalNumber.Format(value);
        double parsed = double.Parse(text, CultureInfo.InvariantCulture);

        // Exact bit equality, not a tolerance: doc 40 requires "invariant round-trip
        // representation", and a round trip that lost a bit would change the payload
        // hash of semantically identical content.
        NumericAssert.AreExactlyEqual(
            System.BitConverter.DoubleToInt64Bits(CanonicalNumber.NormalizeNegativeZero(value)),
            System.BitConverter.DoubleToInt64Bits(parsed),
            "round-tripping " + text + " must recover the same bits");
    }

    /// <summary>
    /// The brief for this codec says modern .NET's default double formatting is already
    /// shortest-round-trippable. That is true, and this asserts it rather than assuming
    /// it, because the whole canonical form depends on it.
    /// </summary>
    [TestCase(0.1)]
    [TestCase(1e-7)]
    [TestCase(1.7976931348623157E+308)]
    [TestCase(5e-324)]
    [TestCase(1.0)]
    public void TheRoundTripSpecifierAgreesWithInvariantDefaultFormatting(double value)
    {
        NumericAssert.AreExactlyEqual(
            value.ToString(CultureInfo.InvariantCulture),
            value.ToString("R", CultureInfo.InvariantCulture),
            "default invariant formatting is already shortest-round-trippable");
    }

    [Test]
    public void NegativeZeroIsNormalizedToZero()
    {
        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual("0", CanonicalNumber.Format(-0.0), "negative zero");
            NumericAssert.AreExactlyEqual("0", CanonicalNumber.Format(0.0), "positive zero");
            Assert.That(
                System.BitConverter.DoubleToInt64Bits(CanonicalNumber.NormalizeNegativeZero(-0.0)),
                Is.EqualTo(System.BitConverter.DoubleToInt64Bits(0.0)),
                "the two zeros must become bit-identical, not merely compare equal");
        });
    }

    [TestCase(0L, "0")]
    [TestCase(1L, "1")]
    [TestCase(-1L, "-1")]
    [TestCase(9007199254740993L, "9007199254740993")]
    [TestCase(long.MinValue, "-9223372036854775808")]
    public void IntegersAreWrittenWithoutPadding(long value, string expected)
    {
        NumericAssert.AreExactlyEqual(expected, CanonicalNumber.Format(value), "integer form");
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void ANonfiniteValueCannotBeWritten(double value)
    {
        // Writing one would produce a payload the codec itself rejects on the way back in.
        Expect.Throws<System.ArgumentOutOfRangeException>(() => CanonicalNumber.Format(value));
    }
}
