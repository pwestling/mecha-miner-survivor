using System;
using System.Collections.Generic;
using System.Text;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Source property order, indentation, and line endings do not reach the hash.
/// </summary>
/// <remarks>
/// <para>
/// Doc 40 § JSON codec and schema baseline: "File order, operating-system path order,
/// locale, indentation, and original property order do not affect compiled bundle or
/// payload hashes." This is that property at the single-payload level;
/// <c>DAT-006</c> extends it to the compiled bundle.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-010</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalPermutationTests
{
    private static readonly string[] Permutations =
    {
        "canonical/permutation-authored-order.json",
        "canonical/permutation-reversed-order.json",
        "canonical/permutation-reindented.json",
        "canonical/permutation-compact.json",
    };

    [Test]
    public void ThePermutationsDifferAsRawBytesSoTheTestIsNotVacuous()
    {
        HashSet<string> distinct = new(StringComparer.Ordinal);
        foreach (string path in Permutations)
        {
            distinct.Add(Encoding.UTF8.GetString(FixtureCorpus.Read(path)));
        }

        NumericAssert.AreExactlyEqual(
            Permutations.Length,
            distinct.Count,
            "every permutation must be a genuinely different file on disk");
    }

    [Test]
    public void EveryPermutationProducesByteIdenticalCanonicalOutput()
    {
        byte[] expected = Canonicalize(Permutations[0]);

        Expect.Multiple(() =>
        {
            foreach (string path in Permutations)
            {
                Assert.That(
                    Canonicalize(path),
                    Is.EqualTo(expected),
                    path + " must canonicalize to the same bytes as "
                        + Permutations[0]);
            }
        });
    }

    [Test]
    public void EveryPermutationProducesTheIdenticalSha256Digest()
    {
        string expected = CanonicalHash.Sha256Hex(Canonicalize(Permutations[0]));

        Expect.Multiple(() =>
        {
            Assert.That(expected, Does.Match("^[0-9a-f]{64}$"));
            foreach (string path in Permutations)
            {
                NumericAssert.AreExactlyEqual(
                    expected,
                    CanonicalHash.Sha256Hex(Canonicalize(path)),
                    path + " digest");
            }
        });
    }

    /// <summary>
    /// Line endings are a property of the file, not of the content. A CRLF copy is built
    /// in memory rather than committed, because .editorconfig fixes the repository to LF
    /// and a committed CRLF fixture would fight the format gate.
    /// </summary>
    [Test]
    public void LineEndingsDoNotReachTheHash()
    {
        byte[] lf = FixtureCorpus.Read(Permutations[0]);
        byte[] crlf = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(lf).Replace("\n", "\r\n", StringComparison.Ordinal));

        Expect.Multiple(() =>
        {
            Assert.That(crlf, Is.Not.EqualTo(lf), "the two byte streams must really differ");
            NumericAssert.AreExactlyEqual(
                CanonicalHash.Sha256Hex(CanonicalizeBytes(lf)),
                CanonicalHash.Sha256Hex(CanonicalizeBytes(crlf)),
                "a CRLF copy must hash identically");
        });
    }

    /// <summary>
    /// The negative control. If a semantic change did not change the hash, the property
    /// above would be worthless.
    /// </summary>
    [Test]
    public void ASemanticChangeDoesChangeTheHash()
    {
        byte[] original = FixtureCorpus.Read(Permutations[0]);
        byte[] changed = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(original)
                .Replace("\"content_version\": 1", "\"content_version\": 2", StringComparison.Ordinal)
                .Replace("\"content_version\":1", "\"content_version\":2", StringComparison.Ordinal));

        Assert.That(changed, Is.Not.EqualTo(original), "the fixture edit must have applied");

        Assert.That(
            CanonicalHash.Sha256Hex(CanonicalizeBytes(changed)),
            Is.Not.EqualTo(CanonicalHash.Sha256Hex(CanonicalizeBytes(original))),
            "changing a value must change the hash");
    }

    private static byte[] Canonicalize(string path)
    {
        return CanonicalizeBytes(FixtureCorpus.Read(path));
    }

    private static byte[] CanonicalizeBytes(byte[] utf8)
    {
        EnvelopeReadResult result = EnvelopeReader.Read(
            utf8,
            new EnvelopeReadContext("tests/canonical.json", ContentCategory.Weapon));

        Assert.That(
            result.IsValid,
            Is.True,
            () => "a canonical permutation fixture must validate: "
                + string.Join("; ", result.Diagnostics));

        return result.Envelope!.ToCanonicalUtf8();
    }
}
