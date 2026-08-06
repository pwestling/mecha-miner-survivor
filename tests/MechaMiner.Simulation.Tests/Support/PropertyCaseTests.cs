using System;
using System.IO;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Proves that the property harness shrinks a failing generated input to a minimal
/// one and preserves it, without relying on a test that actually fails.
/// </summary>
/// <remarks>
/// Verification: <c>VER-FND-003-003</c>, <c>VER-FND-003-006</c>.
///
/// <c>SeedReproductionFixture</c> holds the deliberately failing counterpart, which
/// is <c>Explicit</c> so it never fails an ordinary run;
/// <c>build/verify-test-harness.sh</c> runs it on purpose and asserts its output.
/// These tests instead assert the shrinking contract from the inside, so the property
/// harness is covered by the always-on suite too.
/// </remarks>
[TestFixture]
internal sealed class PropertyCaseTests
{
    private const int SampleSeed = 913;

    [Test]
    public void APropertyThatHoldsForEveryGeneratedInputPasses()
    {
        PropertyCase.ForAll(
            "reversing-an-array-twice-is-identity",
            SampleSeed,
            caseCount: 64,
            generate: random =>
            {
                int[] values = new int[random.Next(0, 12)];
                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = random.Next(-1000, 1000);
                }

                return values;
            },
            shrink: Shrinkers.Int32Array,
            render: RenderArray,
            property: values =>
            {
                int[] reversed = (int[])values.Clone();
                Array.Reverse(reversed);
                Array.Reverse(reversed);
                Assert.That(reversed, Is.EqualTo(values));
            });
    }

    [Test]
    public void AFailingPropertyIsShrunkToAMinimalInputAndPreserved()
    {
        const string caseName = "shrink-to-single-large-element";
        string failureDirectory = TestArtifacts.FailureDirectory(caseName, SampleSeed);
        string minimizedPath = Path.Combine(failureDirectory, "minimized-input.txt");
        File.Delete(minimizedPath);

        // The property fails for any array containing an element above the threshold.
        // The minimal failing input is therefore a single-element array holding
        // exactly one such value, and the shrinker must find it from a longer,
        // noisier generated array.
        AssertionException failure = Expect.Throws<AssertionException>(
            () => PropertyCase.ForAll(
                caseName,
                SampleSeed,
                caseCount: 64,
                generate: random =>
                {
                    int[] values = new int[random.Next(4, 16)];
                    for (int index = 0; index < values.Length; index++)
                    {
                        values[index] = random.Next(0, 2000);
                    }

                    return values;
                },
                shrink: Shrinkers.Int32Array,
                render: RenderArray,
                property: values =>
                {
                    foreach (int value in values)
                    {
                        Assert.That(value, Is.LessThan(1000), "element must stay below the threshold");
                    }
                }));

        Expect.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("Minimized input after"));
            Assert.That(failure.Message, Does.Contain("MECHAMINER_TEST_SEED=913"));
            Assert.That(File.Exists(minimizedPath), "the minimized input must be preserved on disk");
        });

        string minimizedText = File.ReadAllText(minimizedPath);
        TestContext.Progress.WriteLine("preserved minimized input:");
        TestContext.Progress.WriteLine(minimizedText);

        string minimizedLine = ReadField(minimizedText, "minimized input:");
        int[] minimized = ParseArray(minimizedLine);

        Expect.Multiple(() =>
        {
            Assert.That(
                minimized,
                Has.Length.EqualTo(1),
                "shrinking must reduce the failing array to a single element");
            Assert.That(
                minimized[0],
                Is.GreaterThanOrEqualTo(1000),
                "the single remaining element must be the one that violates the property");
            Assert.That(minimizedText, Does.Contain("shrink steps:"));
            Assert.That(minimizedText, Does.Contain("original input:"));
        });
    }

    [Test]
    public void ShrinkingAnIntegerMovesItTowardsZeroAndTerminates()
    {
        Expect.Multiple(() =>
        {
            Assert.That(Shrinkers.Int32(0), Is.Empty, "zero cannot be shrunk further");
            Assert.That(Shrinkers.Int32(100), Does.Contain(0));
            Assert.That(Shrinkers.Int32(100), Does.Contain(50));
            Assert.That(Shrinkers.Int32(100), Does.Not.Contain(100), "a shrinker never returns its input");
            Assert.That(Shrinkers.Int32(-8), Does.Contain(-4));
        });
    }

    private static string RenderArray(int[] values)
    {
        return "[" + string.Join(",", values) + "]";
    }

    private static string ReadField(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            int index = line.IndexOf(label, StringComparison.Ordinal);
            if (index >= 0)
            {
                return line[(index + label.Length)..].Trim();
            }
        }

        throw new InvalidOperationException("field '" + label + "' not found in preserved input:\n" + text);
    }

    private static int[] ParseArray(string rendered)
    {
        string body = rendered.Trim().Trim('[', ']');
        if (body.Length == 0)
        {
            return Array.Empty<int>();
        }

        string[] parts = body.Split(',');
        int[] values = new int[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            values[index] = int.Parse(parts[index], System.Globalization.CultureInfo.InvariantCulture);
        }

        return values;
    }
}
