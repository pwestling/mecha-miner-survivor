using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using MechaMiner.Tests.Support;
using MechaMiner.Tools.Audit;

namespace MechaMiner.Tools.Tests.Audit;

/// <summary>
/// The committed expectations in <c>build/audit-expectations.env</c>, read by the audit
/// tests and by <c>build/verify-registry.sh</c>.
/// </summary>
/// <remarks>
/// <para>
/// One owner for values two readers assert. Before this existed, the forbidden-edge floor
/// was written down twice — <c>MINIMUM_FORBIDDEN_EDGE_CONTROLS=100</c> in
/// <c>build/verify-registry.sh</c> and <c>Is.GreaterThanOrEqualTo(100)</c> in
/// <see cref="ArchitectureRuleTests"/> — with a comment claiming they were "the same
/// floor". They were two equal literals, not one number: changing the C# assertion to 10
/// left the script at exit 0 still reporting the floor as 100 and still attributing it to
/// the test by name. A comment cannot hold two literals in step, so there is now one.
/// </para>
/// <para>
/// Nothing here falls back to a default. A missing file, a missing key or an unparseable
/// value throws, because the alternative is a reader that silently resumes asserting the
/// number it used to hardcode — which is the failure this class was added to end.
/// </para>
/// </remarks>
internal static class AuditExpectations
{
    /// <summary>The absolute path of the expectations file.</summary>
    internal static string FilePath { get; } =
        Path.Combine(TestArtifacts.RepositoryRoot, "build", "audit-expectations.env");

    /// <summary>
    /// The exact number of forbidden ordered project pairs, each of which
    /// <see cref="ArchitectureRuleTests.EveryForbiddenReferenceEdgeIsRejected"/> controls.
    /// </summary>
    internal static int ForbiddenEdgeControls => ReadInt("FORBIDDEN_EDGE_CONTROLS");

    /// <summary>
    /// The registry failure classes, each paired with the rule its fixture must fail
    /// under, in the order the file lists them.
    /// </summary>
    internal static ImmutableArray<(string Fixture, RegistryRule Rule)> RegistryFixtureClasses
    {
        get
        {
            var classes = ImmutableArray.CreateBuilder<(string, RegistryRule)>();
            foreach (string pair in Read("REGISTRY_FIXTURE_CLASSES")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = pair.Split(':');
                if (parts.Length != 2)
                {
                    throw new InvalidDataException(
                        FilePath + ": REGISTRY_FIXTURE_CLASSES entry '" + pair
                        + "' is not a '<class>:<rule>' pair");
                }

                if (!Enum.TryParse(parts[1], out RegistryRule rule))
                {
                    throw new InvalidDataException(
                        FilePath + ": REGISTRY_FIXTURE_CLASSES entry '" + pair
                        + "' names '" + parts[1] + "', which is not a RegistryRule member");
                }

                classes.Add((parts[0], rule));
            }

            if (classes.Count == 0)
            {
                throw new InvalidDataException(
                    FilePath + ": REGISTRY_FIXTURE_CLASSES lists no classes, and an empty list of"
                    + " failure classes is not a satisfied requirement");
            }

            return classes.ToImmutable();
        }
    }

    /// <summary>Reads one key's value, or throws.</summary>
    private static string Read(string key)
    {
        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException(
                "the audit expectations file is missing, so the value it owns cannot be"
                + " asserted against anything: " + FilePath,
                FilePath);
        }

        List<string> matches = new();
        foreach (string line in File.ReadAllLines(FilePath))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            int separator = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(trimmed[..separator], key, StringComparison.Ordinal))
            {
                matches.Add(trimmed[(separator + 1)..].Trim());
            }
        }

        if (matches.Count != 1)
        {
            throw new InvalidDataException(
                FilePath + " declares " + matches.Count.ToString(CultureInfo.InvariantCulture)
                + " value(s) for '" + key + "'; exactly one is required");
        }

        return matches[0];
    }

    /// <summary>Reads one key's value as a non-negative integer, or throws.</summary>
    private static int ReadInt(string key)
    {
        string value = Read(key);
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new InvalidDataException(
                FilePath + ": '" + key + "' reads '" + value
                + "', which is not a non-negative integer");
        }

        return parsed;
    }
}
