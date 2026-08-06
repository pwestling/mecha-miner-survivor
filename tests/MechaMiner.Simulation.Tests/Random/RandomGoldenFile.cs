using System;
using System.IO;
using System.Text;
using MechaMiner.Tests.Support;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Reads a committed golden vector file and splits it into its <c>#</c> header block and its
/// data body.
/// </summary>
/// <remarks>
/// <para>
/// The split is purely positional: the header is the maximal leading run of lines beginning
/// with <c>#</c>, and the body is everything after it, including the blank line several files
/// put between the header and the first data block. Header plus body is therefore the file,
/// byte for byte.
/// </para>
/// <para>
/// The body is the unit <c>VER-SIM-005-004</c> compares three ways - committed golden,
/// production, and independent reference - because it is exactly the part of the file that is
/// computed rather than authored.
/// </para>
/// <para>
/// Nothing here writes. Doc 91 § Determinism and fixture policy and
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/README.md</c> both require a mismatch to be
/// investigated rather than regenerated, so these vectors are read-only to the test suite; only
/// <c>GoldenText</c>'s deliberate update path may write a golden, and that path fails the run.
/// </para>
/// </remarks>
internal static class RandomGoldenFile
{
    /// <summary>The absolute path of a golden in the test project's own source tree.</summary>
    /// <param name="goldenName">The golden's file name.</param>
    /// <returns>The absolute path.</returns>
    internal static string PathOf(string goldenName)
    {
        return Path.Combine(TestArtifacts.TestProjectDirectory, "Goldens", goldenName);
    }

    /// <summary>Reads a golden, normalized to LF with exactly one trailing newline.</summary>
    /// <param name="goldenName">The golden's file name.</param>
    /// <returns>The whole file.</returns>
    internal static string ReadAll(string goldenName)
    {
        string path = PathOf(goldenName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "golden " + goldenName + " is missing. These vectors are committed artifacts derived from "
                    + "doc 20 § Authoritative random-number contract by an independent reference; a test may "
                    + "never recreate one (Goldens/README.md)",
                path);
        }

        return Normalize(File.ReadAllText(path));
    }

    /// <summary>The maximal leading run of <c>#</c> comment lines.</summary>
    /// <param name="goldenName">The golden's file name.</param>
    /// <returns>The header, ending in a newline.</returns>
    internal static string Header(string goldenName)
    {
        string[] lines = ReadAll(goldenName).Split('\n');
        StringBuilder header = new();
        for (int index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith('#'))
            {
                break;
            }

            header.Append(lines[index]).Append('\n');
        }

        return header.ToString();
    }

    /// <summary>Everything after the header: the computed data the vectors pin.</summary>
    /// <param name="goldenName">The golden's file name.</param>
    /// <returns>The body, normalized to LF with exactly one trailing newline.</returns>
    internal static string Body(string goldenName)
    {
        string all = ReadAll(goldenName);
        string header = Header(goldenName);
        if (!all.StartsWith(header, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("golden " + goldenName + " header split failed");
        }

        return Normalize(all.Substring(header.Length));
    }

    /// <summary>Normalizes to LF with exactly one trailing newline, as <c>GoldenText</c>
    /// does.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    internal static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .TrimEnd('\n') + "\n";
    }
}
