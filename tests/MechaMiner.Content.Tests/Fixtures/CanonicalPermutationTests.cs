using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
/// <b>Determinism proved over nothing reads exactly like determinism proved over
/// everything.</b> Both of the "every permutation agrees" tests are <c>foreach</c> loops,
/// so emptying <see cref="Permutations"/> leaves them green while they assert nothing at
/// all - and this is the property DAT-006's permutation gate is built on top of, so a
/// vacuous pass here is a vacuous pass there. Two independent things stop that, and
/// neither is a literal sitting beside the array it counts.
/// </para>
/// <list type="number">
/// <item><description>The committed set is held against the fixture <em>directory</em>: <see cref="TheCommittedPermutationsAreExactlyTheFilesOnDisk"/> reads <c>canonical/permutation-*.json</c> and requires the array to name exactly those. Deleting a line from the array fails against a file that is still there, and deleting the line together with any count written next to it would fail the same way, because the number is not written down here at all.</description></item>
/// <item><description>A second family is <em>generated</em> rather than committed: <see cref="EveryGeneratedRotationCanonicalizesToTheSameBytesAndDigest"/> rotates the authored document's own root properties into every starting position and emits each rotation twice, indented and compact. Its size is <c>2 x</c> the number of root properties the document declares - read from the document, so it moves when the subject moves - and there is no file and no array to empty. A permutation family that cannot be shrunk without deleting the generator is the part of this fixture DAT-006 can stand on.</description></item>
/// </list>
/// <para>
/// Verification: <c>VER-DAT-001-010</c>, <c>VER-DAT-001-046</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalPermutationTests
{
    /// <summary>The fixture subdirectory the committed permutations live in.</summary>
    private const string PermutationDirectory = "canonical";

    /// <summary>The name every committed permutation file begins with.</summary>
    private const string PermutationPrefix = "permutation-";

    private static readonly string[] Permutations =
    {
        "canonical/permutation-authored-order.json",
        "canonical/permutation-reversed-order.json",
        "canonical/permutation-reindented.json",
        "canonical/permutation-compact.json",
    };

    /// <summary>
    /// The committed array names exactly the permutation files that exist on disk.
    /// </summary>
    /// <remarks>
    /// The directory is the independent basis. Every other test here ranges over
    /// <see cref="Permutations"/>, so the array decides how much they prove, and nothing
    /// derived from the array can notice it shrinking. A count literal written beside the
    /// array would not help: it is a second copy of the same fact and a deletion takes
    /// both. The directory is a third party - a permutation stops being asserted only
    /// when its file is genuinely deleted, which is a visible retirement rather than a
    /// tidy-up.
    /// </remarks>
    [Test]
    public void TheCommittedPermutationsAreExactlyTheFilesOnDisk()
    {
        List<string> onDisk = new();
        foreach (string path in Directory.GetFiles(
                     Path.Combine(FixtureCorpus.Root, PermutationDirectory),
                     PermutationPrefix + "*.json"))
        {
            onDisk.Add(PermutationDirectory + "/" + Path.GetFileName(path));
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                onDisk,
                Is.Not.Empty,
                "the permutation fixture directory is empty, so every loop over it proves "
                    + "nothing");
            Assert.That(
                Permutations,
                Is.EquivalentTo(onDisk),
                () => nameof(Permutations) + " must name exactly the "
                    + PermutationPrefix + "*.json files under Fixtures/"
                    + PermutationDirectory + "/. On disk: " + string.Join(", ", onDisk));
        });
    }

    [Test]
    public void ThePermutationsDifferAsRawBytesSoTheTestIsNotVacuous()
    {
        HashSet<string> distinct = new(StringComparer.Ordinal);
        foreach (string path in Permutations)
        {
            distinct.Add(Encoding.UTF8.GetString(FixtureCorpus.Read(path)));
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                Permutations,
                Is.Not.Empty,
                "there is nothing to compare; see "
                    + nameof(TheCommittedPermutationsAreExactlyTheFilesOnDisk));
            NumericAssert.AreExactlyEqual(
                Permutations.Length,
                distinct.Count,
                "every permutation must be a genuinely different file on disk");
            Assert.That(
                distinct,
                Is.Not.Empty,
                "no permutation was read at all");
        });
    }

    [Test]
    public void EveryPermutationProducesByteIdenticalCanonicalOutput()
    {
        Assert.That(
            Permutations,
            Is.Not.Empty,
            "an empty permutation set makes this loop assert nothing; see "
                + nameof(TheCommittedPermutationsAreExactlyTheFilesOnDisk));

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
        Assert.That(
            Permutations,
            Is.Not.Empty,
            "an empty permutation set makes this loop assert nothing; see "
                + nameof(TheCommittedPermutationsAreExactlyTheFilesOnDisk));

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
    /// The generated family: every cyclic rotation of the authored document's root
    /// property order, in both an indented and a compact spelling, canonicalizes to one
    /// byte sequence and one digest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Where the number comes from.</b> The size of this family is <c>2 x</c> the
    /// number of root properties the authored fixture declares: one rotation per starting
    /// property, each emitted in two whitespace spellings. It is read out of the document
    /// under test rather than written down here, so adding a field to the envelope makes
    /// the family larger without anybody editing this file, and no edit to this file can
    /// make it smaller while the document stays the same size.
    /// </para>
    /// <para>
    /// Rotations rather than all <c>n!</c> orderings: eight properties is 40320 envelope
    /// reads for a property that a rotation through every starting position already
    /// exercises, and the committed <c>permutation-reversed-order.json</c> covers the
    /// one ordering rotations cannot reach.
    /// </para>
    /// </remarks>
    [Test]
    public void EveryGeneratedRotationCanonicalizesToTheSameBytesAndDigest()
    {
        JsonObject authored = ReadAuthoredRoot();
        int rootProperties = authored.Count;
        IReadOnlyList<GeneratedPermutation> generated = RotationsOf(authored);

        byte[] expectedBytes = Canonicalize(Permutations[0]);
        string expectedDigest = CanonicalHash.Sha256Hex(expectedBytes);

        Expect.Multiple(() =>
        {
            Assert.That(
                rootProperties,
                Is.GreaterThan(1),
                "a document with fewer than two root properties has no distinct rotations, "
                    + "so this family would prove nothing");
            NumericAssert.AreExactlyEqual(
                2 * rootProperties,
                generated.Count,
                "the generated family is one rotation per root property in two whitespace "
                    + "spellings; its size is read from the document rather than written "
                    + "down here");

            HashSet<string> distinctSources = new(StringComparer.Ordinal);
            foreach (GeneratedPermutation permutation in generated)
            {
                distinctSources.Add(Encoding.UTF8.GetString(permutation.Utf8));

                Assert.That(
                    CanonicalizeBytes(permutation.Utf8),
                    Is.EqualTo(expectedBytes),
                    permutation.Name + " must canonicalize to the same bytes as "
                        + Permutations[0]);
                NumericAssert.AreExactlyEqual(
                    expectedDigest,
                    CanonicalHash.Sha256Hex(CanonicalizeBytes(permutation.Utf8)),
                    permutation.Name + " digest");
            }

            NumericAssert.AreExactlyEqual(
                generated.Count,
                distinctSources.Count,
                "every generated permutation must be a genuinely different byte sequence, "
                    + "or the family is smaller than it counts itself as");
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

    /// <summary>Reads the authored fixture's root object, property order preserved.</summary>
    private static JsonObject ReadAuthoredRoot()
    {
        JsonNode? root = JsonNode.Parse(FixtureCorpus.Read(Permutations[0]));
        Assert.That(root, Is.InstanceOf<JsonObject>(), Permutations[0] + " must be an object");
        return (JsonObject)root!;
    }

    /// <summary>
    /// Every cyclic rotation of <paramref name="authored"/>'s property order, each
    /// rendered indented and compact.
    /// </summary>
    private static IReadOnlyList<GeneratedPermutation> RotationsOf(JsonObject authored)
    {
        List<string> names = new(authored.Count);
        foreach (KeyValuePair<string, JsonNode?> property in authored)
        {
            names.Add(property.Key);
        }

        List<GeneratedPermutation> generated = new(2 * names.Count);
        for (int offset = 0; offset < names.Count; offset++)
        {
            JsonObject rotated = new();
            for (int index = 0; index < names.Count; index++)
            {
                string name = names[(offset + index) % names.Count];
                rotated[name] = authored[name]?.DeepClone();
            }

            foreach (bool indented in new[] { true, false })
            {
                generated.Add(new GeneratedPermutation(
                    "rotation from '" + names[offset] + "', "
                        + (indented ? "indented" : "compact"),
                    Encoding.UTF8.GetBytes(rotated.ToJsonString(
                        new JsonSerializerOptions { WriteIndented = indented }))));
            }
        }

        return generated;
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

    /// <summary>One generated source spelling of the authored document.</summary>
    /// <param name="Name">How it was generated, for a failure message.</param>
    /// <param name="Utf8">Its source bytes.</param>
    private sealed record GeneratedPermutation(string Name, byte[] Utf8);
}
