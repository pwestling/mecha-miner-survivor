using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Support;

/// <summary>
/// The sample golden tier: proves that a golden is canonical, ordered, reviewable
/// text, and that it cannot be accepted merely to make a run green.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-FND-003-005</c>.
/// </para>
/// <para>
/// The subject is deliberately the harness's own canonical-writing rules rather than
/// content: at FND-003 there is no compiled content bundle, and
/// <c>docs/technical/40-content-data-and-validation.md</c> gives its canonical
/// ordering to <c>CMP-CNT-001</c>, owned by <c>DAT-006</c>. What this fixture proves
/// is the property every later golden depends on: dictionaries are written as
/// lexically sorted keys, so source enumeration order cannot change the text
/// (doc 40 § JSON codec and schema baseline).
/// </para>
/// </remarks>
[TestFixture]
internal sealed class GoldenTextTests
{
    private const string GoldenName = "canonical-ordering-sample.txt";

    /// <summary>
    /// A golden this fixture writes and deletes itself, so the mismatch, update, and
    /// missing-golden branches are covered without depending on execution order or on
    /// the committed golden.
    /// </summary>
    private const string TransientGoldenName = "transient-golden-contract.txt";

    [Test]
    public void TheCanonicalRenderingMatchesItsCommittedGolden()
    {
        GoldenText.Matches(GoldenName, RenderCanonicalSample(BuildSampleInSourceOrder()));
    }

    [Test]
    public void PermutingTheSourceOrderYieldsIdenticalCanonicalText()
    {
        string fromSourceOrder = RenderCanonicalSample(BuildSampleInSourceOrder());
        string fromReversedOrder = RenderCanonicalSample(BuildSampleInReversedOrder());

        Assert.That(
            fromReversedOrder,
            Is.EqualTo(fromSourceOrder),
            "canonical text must not depend on source enumeration order (doc 40 § JSON codec and schema baseline)");
    }

    [Test]
    public void AMismatchPreservesAReviewableLineDiffAndRefusesToAcceptTheNewValue()
    {
        // A transient golden, written by this test and removed in TearDown, so the
        // test is independent of execution order and of whether the committed golden
        // happens to exist. Doc 91 § Flake policy: a test may not depend on another
        // test having run first.
        WriteTransientGolden("committed value\n");

        string diffDirectory = TestArtifacts.GoldenDiffDirectory(TransientGoldenName);
        string diffPath = Path.Combine(diffDirectory, "diff.txt");
        File.Delete(diffPath);

        AssertionException failure = Expect.Throws<AssertionException>(
            () => GoldenText.Matches(TransientGoldenName, "a different value\n"));

        Expect.Multiple(() =>
        {
            Assert.That(File.Exists(diffPath), "a mismatch must preserve a reviewable diff");
            Assert.That(failure.Message, Does.Contain("does not match"));
            Assert.That(
                failure.Message,
                Does.Contain(GoldenText.UpdateVariable),
                "the message must say how a deliberate update is performed");
            Assert.That(
                failure.Message,
                Does.Contain("that run also fails by design"),
                "doc 114 forbids a golden update turning a run green by itself");
        });

        string diff = File.ReadAllText(diffPath);
        Expect.Multiple(() =>
        {
            Assert.That(diff, Does.Contain("--- expected (committed golden)"));
            Assert.That(diff, Does.Contain("+++ actual"));
            Assert.That(diff, Does.Contain("1 - committed value"));
            Assert.That(diff, Does.Contain("1 + a different value"));
        });

        // A mismatch without the update switch must leave the golden untouched.
        Assert.That(
            File.ReadAllText(TransientGoldenPath()),
            Is.EqualTo("committed value\n"),
            "a mismatch must not rewrite the golden");
    }

    [Test]
    public void AnUpdateRunRewritesTheGoldenAndStillFails()
    {
        WriteTransientGolden("stale value\n");

        string? previous = Environment.GetEnvironmentVariable(GoldenText.UpdateVariable);
        AssertionException failure;
        try
        {
            Environment.SetEnvironmentVariable(GoldenText.UpdateVariable, "1");
            failure = Expect.Throws<AssertionException>(
                () => GoldenText.Matches(TransientGoldenName, "reviewed new value\n"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(GoldenText.UpdateVariable, previous);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                File.ReadAllText(TransientGoldenPath()),
                Is.EqualTo("reviewed new value\n"),
                "the update switch must rewrite the golden");
            Assert.That(
                failure.Message,
                Does.Contain("still fails on purpose"),
                "doc 91: a golden may not be accepted merely to make a test pass, so the update run "
                    + "fails too and the new golden has to be reviewed and committed deliberately");
        });
    }

    [Test]
    public void AMissingGoldenIsWrittenAndTheTestStillFails()
    {
        File.Delete(TransientGoldenPath());

        AssertionException failure = Expect.Throws<AssertionException>(
            () => GoldenText.Matches(TransientGoldenName, "first observed value\n"));

        Expect.Multiple(() =>
        {
            Assert.That(File.Exists(TransientGoldenPath()), "a missing golden is written for review");
            Assert.That(
                File.ReadAllText(TransientGoldenPath()),
                Is.EqualTo("first observed value\n"));
            Assert.That(failure.Message, Does.Contain("did not exist and has been written"));
            Assert.That(failure.Message, Does.Contain("Review it against the authoritative source"));
        });
    }

    [TearDown]
    public void RemoveTransientGolden()
    {
        File.Delete(TransientGoldenPath());
    }

    private static string TransientGoldenPath()
    {
        return Path.Combine(TestArtifacts.TestProjectDirectory, "Goldens", TransientGoldenName);
    }

    private static void WriteTransientGolden(string content)
    {
        string path = TransientGoldenPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// A small sample whose only interesting property is that its keys arrive in a
    /// deliberately non-lexical order.
    /// </summary>
    private static IReadOnlyList<KeyValuePair<string, int>> BuildSampleInSourceOrder()
    {
        return new List<KeyValuePair<string, int>>
        {
            new("W-BD", 3),
            new("REL-01", 1),
            new("MCH-01", 2),
            new("EN-01", 10),
            new("W-AB", 4),
            new("BOSS-01", 1),
        };
    }

    private static IReadOnlyList<KeyValuePair<string, int>> BuildSampleInReversedOrder()
    {
        List<KeyValuePair<string, int>> reversed = new(BuildSampleInSourceOrder());
        reversed.Reverse();
        return reversed;
    }

    /// <summary>
    /// Renders the sample the way doc 40 § JSON codec and schema baseline requires a
    /// canonical writer to: "dictionaries as lexically sorted key entries", integers
    /// "without padding", and no dependence on the original property order.
    /// </summary>
    private static string RenderCanonicalSample(IReadOnlyList<KeyValuePair<string, int>> entries)
    {
        List<KeyValuePair<string, int>> sorted = new(entries);
        sorted.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));

        StringBuilder builder = new();
        builder.Append("# Canonical ordering sample, owned by tests/shared/GoldenText.cs.\n");
        builder.Append("# Keys are lexically sorted by ordinal comparison; integers carry no padding.\n");
        builder.Append("# entries=").Append(sorted.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (KeyValuePair<string, int> entry in sorted)
        {
            builder.Append(entry.Key).Append('\t')
                .Append(entry.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }
}
