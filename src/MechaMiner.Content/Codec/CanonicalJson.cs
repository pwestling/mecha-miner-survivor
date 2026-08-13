using System;
using System.Buffers;
using System.Text.Json;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The entry point for producing canonical payload bytes and their hash.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "File order, operating-system path order, locale, indentation, and
/// original property order do not affect compiled bundle or payload hashes."
/// </para>
/// <para>
/// This type is the only supported way to obtain those bytes. Everything that could
/// make the output machine-dependent is fixed here and cannot be overridden by a
/// caller: no indentation, no locale-sensitive formatting anywhere on the path, and
/// writer validation left on so a malformed payload fails at the point it is written
/// rather than at the point something tries to read it.
/// </para>
/// <para>
/// Doc 70 § Persistent transaction model reuses this codec for saves, profile,
/// settings, and recovery. Nothing in this namespace names a content type, so that
/// reuse costs a <c>using</c> and no dependency. Codec reuse does not merge domain
/// ownership: each domain owns its own DTOs, its own
/// <see cref="SchemaFieldOrder"/> declarations, and its own validation.
/// </para>
/// </remarks>
public static class CanonicalJson
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        // Canonical bytes are never the human-readable view.
        Indented = false,

        // Left on deliberately: a canonical payload that is not well-formed JSON is
        // worse than a write failure, because its hash would look perfectly valid.
        SkipValidation = false,
    };

    /// <summary>Runs <paramref name="write"/> against a fresh writer and returns the bytes.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="write"/> is null.</exception>
    public static byte[] Serialize(Action<CanonicalJsonWriter> write)
    {
        ArgumentNullException.ThrowIfNull(write);

        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer, WriterOptions))
        {
            write(new CanonicalJsonWriter(writer));
            writer.Flush();
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes one object whose fields are emitted in <paramref name="order"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static byte[] SerializeObject(SchemaFieldOrder order, Action<CanonicalJsonWriter> writeFields)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(writeFields);

        return Serialize(writer =>
        {
            writer.BeginObject(order);
            writeFields(writer);
            writer.EndObject();
        });
    }

    /// <summary>
    /// Serializes and returns the SHA-256 hex digest of the canonical bytes, without
    /// exposing an intermediate form that could be hashed instead.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="write"/> is null.</exception>
    public static string Sha256HexOf(Action<CanonicalJsonWriter> write)
    {
        return CanonicalHash.Sha256Hex(Serialize(write));
    }
}
