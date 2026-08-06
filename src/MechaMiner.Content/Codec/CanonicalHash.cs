using System;
using System.Security.Cryptography;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The content hash function.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "SHA-256 from the .NET base class library hashes canonical UTF-8 payload
/// bytes. Human-readable pretty JSON is a separate derived view and is never hashed
/// or loaded as canonical state."
/// </para>
/// <para>
/// The input is therefore always bytes produced by
/// <see cref="CanonicalJsonWriter"/>, never a re-serialized or re-indented view. This
/// type takes a byte span and nothing else precisely so that no caller can hand it a
/// pretty-printed string by accident.
/// </para>
/// </remarks>
public static class CanonicalHash
{
    private const string HexDigits = "0123456789abcdef";

    /// <summary>The number of characters in a digest produced by <see cref="Sha256Hex"/>.</summary>
    public const int HexLength = 64;

    /// <summary>
    /// Returns the lowercase hexadecimal SHA-256 digest of a canonical UTF-8 payload.
    /// </summary>
    public static string Sha256Hex(ReadOnlySpan<byte> canonicalUtf8)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(canonicalUtf8, digest);

        // Written by hand rather than through Convert.ToHexString(...).ToLowerInvariant():
        // lowercase is the form every hash artifact in this repository uses, and doing
        // it directly avoids a case conversion whose culture behaviour has to be
        // reasoned about at all.
        Span<char> hex = stackalloc char[HexLength];
        for (int index = 0; index < digest.Length; index++)
        {
            hex[index * 2] = HexDigits[digest[index] >> 4];
            hex[(index * 2) + 1] = HexDigits[digest[index] & 0x0F];
        }

        return new string(hex);
    }
}
