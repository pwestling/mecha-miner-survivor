using System.Text;
using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>SHA-256 over canonical UTF-8 bytes. Verification: <c>VER-DAT-001-010</c>.</summary>
[TestFixture]
internal sealed class CanonicalHashTests
{
    /// <summary>
    /// The published SHA-256 of the empty input and of "abc". Pinning against the
    /// published vectors rather than against our own output means a change to the
    /// hashing path cannot be absorbed by regenerating an expectation.
    /// </summary>
    [TestCase("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [TestCase("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public void TheDigestMatchesThePublishedVector(string input, string expected)
    {
        NumericAssert.AreExactlyEqual(
            expected,
            CanonicalHash.Sha256Hex(Encoding.UTF8.GetBytes(input)),
            "SHA-256 of " + (input.Length == 0 ? "the empty input" : "'" + input + "'"));
    }

    [Test]
    public void TheDigestIsLowercaseHexOfTheDeclaredLength()
    {
        string digest = CanonicalHash.Sha256Hex(Encoding.UTF8.GetBytes("mecha"));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(CanonicalHash.HexLength, digest.Length, "digest length");
            Assert.That(digest, Does.Match("^[0-9a-f]{64}$"), "lowercase hexadecimal only");
        });
    }

    [Test]
    public void DifferentBytesProduceDifferentDigests()
    {
        Assert.That(
            CanonicalHash.Sha256Hex(Encoding.UTF8.GetBytes("{\"a\":1}")),
            Is.Not.EqualTo(CanonicalHash.Sha256Hex(Encoding.UTF8.GetBytes("{\"a\":2}"))));
    }
}
