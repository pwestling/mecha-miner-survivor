using System;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Persistence.Tests.Support;

/// <summary>
/// The sample reference-model tier: an optimized implementation is compared against
/// deliberately simple slow reference logic over generated inputs.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-FND-003-006</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models: "For
/// algorithms where one implementation could repeat its own bug, maintain
/// deliberately simple slow reference logic in tests", and "Random/property tests
/// compare optimized results with the reference within declared numeric tolerance."
/// </para>
/// <para>
/// The pair below is compensated summation against naive left-to-right summation.
/// It is a genuine instance of the pattern - the optimized version is measurably more
/// accurate, so the comparison has to name a tolerance and justify it - and it is
/// self-contained, because at FND-003 the pure projects hold no domain code yet.
/// <c>PST-001</c>'s canonical save comparison and <c>COM-003</c>'s sequential damage
/// resolution are the first real reference models; they use this same shape.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ReferenceModelPropertyTests
{
    private const int SampleSeed = 5150;

    /// <summary>
    /// The comparison tolerance. Naive summation of n values of magnitude m
    /// accumulates at most about n * m * double.Epsilon-scale relative error; with
    /// n at most 256 and m at most 1e6 the observed spread is far below 1e-6, so a
    /// bound of 1e-6 catches a genuine algorithmic divergence while tolerating the
    /// float rounding the two orders legitimately differ by.
    /// </summary>
    private static readonly Tolerance SummationTolerance = Tolerance.Named(
        "compensated-vs-naive-summation",
        1e-6,
        "bounds accumulated float rounding for at most 256 terms of magnitude at most 1e6, which is "
            + "orders of magnitude smaller than any real algorithmic divergence");

    [Test]
    public void CompensatedSummationAgreesWithTheReferenceSumWithinANamedTolerance()
    {
        PropertyCase.ForAll(
            "compensated-summation-matches-reference",
            SampleSeed,
            caseCount: 128,
            generate: random =>
            {
                double[] values = new double[random.Next(0, 256)];
                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = (random.NextDouble() - 0.5) * 2_000_000.0;
                }

                return values;
            },
            shrink: ShrinkDoubleArray,
            render: values => "[" + string.Join(",", Array.ConvertAll(values, RenderExactly)) + "]",
            property: values => NumericAssert.AreEqualWithin(
                ReferenceSum(values),
                CompensatedSum(values),
                SummationTolerance,
                "sum of " + values.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " generated values"));
    }

    [Test]
    public void TheReferenceModelIsExactForIntegralValues()
    {
        // Where doc 91 requires exact equality - counts and integer currency - the
        // reference and the optimized implementation must agree exactly, not within a
        // tolerance.
        double[] values = { 1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0 };
        NumericAssert.AreExactlyEqual(
            (long)ReferenceSum(values),
            (long)CompensatedSum(values),
            "integral ledger sum");
    }

    [Test]
    public void TheOptimizedImplementationIsTheMoreAccurateOne()
    {
        // Without this, the property test above would pass just as happily if the
        // "optimized" implementation were a copy of the reference, and doc 91's
        // reason for keeping a reference model at all would be lost.
        double[] pathological = new double[10_001];
        pathological[0] = 1e16;
        for (int index = 1; index < pathological.Length; index++)
        {
            pathological[index] = 1.0;
        }

        double exact = 1e16 + 10_000.0;
        double naive = ReferenceSum(pathological);
        double compensated = CompensatedSum(pathological);

        Expect.Multiple(() =>
        {
            Assert.That(
                Math.Abs(compensated - exact),
                Is.LessThan(Math.Abs(naive - exact)),
                "compensated summation must be strictly closer to the exact value than the reference");
            Assert.That(compensated, Is.EqualTo(exact), "compensated summation recovers the exact value here");
        });
    }

    /// <summary>
    /// The reference model: the most obvious possible implementation, kept slow and
    /// simple on purpose so it cannot share a bug with the optimized one.
    /// </summary>
    private static double ReferenceSum(double[] values)
    {
        double total = 0.0;
        foreach (double value in values)
        {
            total += value;
        }

        return total;
    }

    /// <summary>The optimized implementation: Neumaier compensated summation.</summary>
    private static double CompensatedSum(double[] values)
    {
        double total = 0.0;
        double compensation = 0.0;
        foreach (double value in values)
        {
            double candidate = total + value;
            compensation += Math.Abs(total) >= Math.Abs(value)
                ? (total - candidate) + value
                : (value - candidate) + total;
            total = candidate;
        }

        return total + compensation;
    }

    private static System.Collections.Generic.IEnumerable<double[]> ShrinkDoubleArray(double[] values)
    {
        if (values.Length == 0)
        {
            yield break;
        }

        yield return Array.Empty<double>();
        if (values.Length > 2)
        {
            yield return values[..(values.Length / 2)];
        }

        for (int index = 0; index < values.Length; index++)
        {
            double[] without = new double[values.Length - 1];
            Array.Copy(values, 0, without, 0, index);
            Array.Copy(values, index + 1, without, index, values.Length - index - 1);
            yield return without;
        }
    }

    /// <summary>
    /// Round-trip formatting, so a preserved minimized input reproduces the exact
    /// double that failed rather than a rounded rendering of it.
    /// </summary>
    private static string RenderExactly(double value)
    {
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }
}
