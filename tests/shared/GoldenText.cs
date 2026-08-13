using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Compares an actual value against a committed golden as canonical, ordered,
/// reviewable text.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Determinism and fixture
/// policy: "Golden outputs are canonical, ordered, and reviewable text or compact
/// images", and updating one "requires an authority-aware diff review of the
/// underlying behavior change plus a regenerated evidence bundle; the implementing
/// agent may perform that review ... but may not accept snapshots merely to make a
/// test pass."
/// </para>
/// <para>
/// The update switch honours that last clause literally: with
/// <c>MECHAMINER_GOLDEN_UPDATE=1</c> the golden is rewritten <b>and the test still
/// fails</b>. Updating a golden can therefore never turn a run green by itself; the
/// rewritten file has to be reviewed and committed, and the run has to be repeated
/// without the switch.
/// </para>
/// <para>
/// Goldens live in the test project's own <c>Goldens/</c> directory because they are
/// committed reviewable artifacts, not build output. Text is normalized to LF with a
/// single trailing newline so a golden diff is never a line-ending diff.
/// </para>
/// </remarks>
internal static class GoldenText
{
    /// <summary>The environment variable that rewrites a golden and still fails the test.</summary>
    internal const string UpdateVariable = "MECHAMINER_GOLDEN_UPDATE";

    /// <summary>
    /// Asserts that <paramref name="actual"/> equals the committed golden named
    /// <paramref name="goldenName"/>.
    /// </summary>
    internal static void Matches(string goldenName, string actual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goldenName);
        ArgumentNullException.ThrowIfNull(actual);

        string goldenPath = Path.Combine(TestArtifacts.TestProjectDirectory, "Goldens", goldenName);
        string normalizedActual = Normalize(actual);
        bool updateRequested = string.Equals(
            Environment.GetEnvironmentVariable(UpdateVariable),
            "1",
            StringComparison.Ordinal);

        if (!File.Exists(goldenPath))
        {
            WriteGolden(goldenPath, normalizedActual);
            Assert.Fail(
                "golden " + goldenName + " did not exist and has been written to "
                + TestArtifacts.Relative(goldenPath)
                + ". Review it against the authoritative source before committing it, then rerun. "
                + "doc 91 § Determinism and fixture policy: a golden is accepted through an "
                + "authority-aware review of the behaviour it records, never because a test needed "
                + "to pass.");
            return;
        }

        string normalizedGolden = Normalize(File.ReadAllText(goldenPath));
        if (string.Equals(normalizedGolden, normalizedActual, StringComparison.Ordinal))
        {
            if (updateRequested)
            {
                Assert.Fail(
                    UpdateVariable + " was set but golden " + goldenName
                    + " already matches. Unset it: a golden update run is a deliberate act with a "
                    + "reviewed behaviour change behind it.");
            }

            return;
        }

        string diff = RenderDiff(normalizedGolden, normalizedActual);
        string diffDirectory = TestArtifacts.GoldenDiffDirectory(goldenName);
        File.WriteAllText(Path.Combine(diffDirectory, "expected.txt"), normalizedGolden);
        File.WriteAllText(Path.Combine(diffDirectory, "actual.txt"), normalizedActual);
        File.WriteAllText(Path.Combine(diffDirectory, "diff.txt"), diff);

        TestContext.Progress.WriteLine("GOLDEN MISMATCH " + goldenName);
        TestContext.Progress.WriteLine(diff);
        TestContext.Progress.WriteLine("preserved: " + TestArtifacts.Relative(diffDirectory));

        if (updateRequested)
        {
            WriteGolden(goldenPath, normalizedActual);
            Assert.Fail(
                "golden " + goldenName + " has been rewritten because " + UpdateVariable
                + "=1, and this test still fails on purpose. Review the diff above against the "
                + "authoritative source, confirm the behaviour change is intended and evidenced, "
                + "commit the new golden, then rerun without " + UpdateVariable
                + ". doc 114 § Failure and retry policy forbids editing a golden to make a gate pass.");
            return;
        }

        Assert.Fail(
            "golden " + goldenName + " does not match. Diff preserved at "
            + TestArtifacts.Relative(diffDirectory)
            + ". If the behaviour change is intended, review it against its authoritative source and "
            + "rerun with " + UpdateVariable + "=1 to regenerate the golden; that run also fails by "
            + "design, so the new golden must be reviewed and committed deliberately.\n" + diff);
    }

    private static void WriteGolden(string goldenPath, string normalizedActual)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(goldenPath)!);
        File.WriteAllText(goldenPath, normalizedActual);
    }

    /// <summary>Normalizes to LF with exactly one trailing newline.</summary>
    private static string Normalize(string text)
    {
        string normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
        return normalized + "\n";
    }

    /// <summary>
    /// Renders a line-oriented diff. Every differing line is shown with its number,
    /// which is what makes a golden reviewable rather than merely unequal.
    /// </summary>
    private static string RenderDiff(string expected, string actual)
    {
        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        int lineCount = Math.Max(expectedLines.Length, actualLines.Length);

        List<string> lines = new()
        {
            "--- expected (committed golden): " + expectedLines.Length.ToString(CultureInfo.InvariantCulture)
                + " line(s)",
            "+++ actual:                      " + actualLines.Length.ToString(CultureInfo.InvariantCulture)
                + " line(s)",
        };

        int reported = 0;
        for (int index = 0; index < lineCount && reported < 40; index++)
        {
            string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<absent>";
            string actualLine = index < actualLines.Length ? actualLines[index] : "<absent>";
            if (string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                continue;
            }

            string number = (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(4);
            lines.Add(number + " - " + expectedLine);
            lines.Add(number + " + " + actualLine);
            reported++;
        }

        if (reported == 0)
        {
            lines.Add("(no line differs; the difference is trailing whitespace or line endings)");
        }

        StringBuilder builder = new();
        foreach (string line in lines)
        {
            builder.Append(line).Append('\n');
        }

        return builder.ToString();
    }
}
