using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Which authored definitions each schema under <c>content/schemas/</c> describes.
/// </summary>
/// <remarks>
/// <para>
/// The population is enumerated over <b>schemas</b> and never over directories. A walk
/// that starts from the directories cannot report a schema whose corpus is missing,
/// because there is no directory to arrive at: it skips it, and the schema with zero
/// coverage becomes the one the gate never mentions. So every declared kind is either
/// bound to a non-empty corpus here or named in <see cref="NoCorpus"/> with the reason,
/// and <c>SchemaCorpusDivergenceTests</c> holds the union of the two - plus
/// <see cref="Excluded"/> - against the <c>*.schema.json</c> files on disk.
/// </para>
/// <para>
/// <b>Two directories hold two kinds each</b>, which doc 40 § Minted content-ID grammars
/// causes by placing an aggregate "in the catalog directory it serves":
/// <c>content/enemies/</c> holds ten enemies plus the shared elite-modifier aggregate,
/// and <c>content/weapons/</c> holds fifteen weapons plus the stat price formula. The two
/// aggregates are named in <see cref="Aggregates"/> by path rather than inferred, because
/// every route to inferring them reads a field the corpus authors differently from the
/// schema - the divergence being measured - and a splitter built on the divergence would
/// move when the divergence moved. Naming them costs two lines and is checked from both
/// sides: the named file must exist, and the union of every corpus must be exactly the
/// authored definitions on disk, so a file assigned to the wrong kind or to none is loud.
/// </para>
/// </remarks>
internal static class AuthoredCorpus
{
    /// <summary>
    /// The aggregate definitions that share a directory with a different kind, by the
    /// kind that owns them.
    /// </summary>
    private static readonly Dictionary<DefinitionKind, string> Aggregates =
        new()
        {
            [DefinitionKind.EliteModifiers] = "shared-elite-modifiers.json",
            [DefinitionKind.WeaponStatPriceFormula] = "stat-price-formula.json",
        };

    /// <summary>
    /// Schemas with no authored definition anywhere, each with the reason, carried as a
    /// named list rather than as an omission.
    /// </summary>
    /// <remarks>
    /// A schema with zero instances can never fail: it is the one row a divergence
    /// measurement is structurally unable to say anything about, so it is stated instead.
    /// <c>SchemaCorpusDivergenceTests.TheNoCorpusSchemasReallyHaveNoAuthoredDefinition</c>
    /// checks the claim against the directory rather than trusting this list.
    /// </remarks>
    internal static IReadOnlyDictionary<DefinitionKind, string> NoCorpus { get; } =
        new ReadOnlyDictionary<DefinitionKind, string>(new Dictionary<DefinitionKind, string>
        {
            [DefinitionKind.PlayerBaseline] =
                "no definition has been authored for the player baseline: content/player/ "
                + "does not exist, so player-baseline.schema.json governs zero instances and "
                + "cannot produce a row here in either direction",
        });

    /// <summary>
    /// Schemas deliberately outside the pin, each with the reason.
    /// </summary>
    /// <remarks>
    /// One entry, and it is not a category schema. <c>envelope.schema.json</c> closes its
    /// field set to the nine envelope fields, so pointing it at whole definitions rejects
    /// every one of them for carrying category fields - one row per undeclared property
    /// per file, which is an artefact of the pairing rather than a disagreement anybody
    /// could act on. Excluding it silently would be worse than including it, so the
    /// exclusion is named here and
    /// <c>SchemaCorpusDivergenceTests.TheEnvelopeExclusionIsTheArtefactItClaimsToBe</c>
    /// measures the artefact rather than asserting it.
    /// </remarks>
    internal static IReadOnlyDictionary<string, string> Excluded { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["envelope.schema.json"] =
                "the nine-field envelope is closed, so every one of the 138 authored "
                + "definitions is rejected for its category fields; the rows would be one "
                + "per undeclared property per file and would say nothing about the envelope",
        });

    /// <summary>
    /// The repository-relative directory the definitions of <paramref name="kind"/> are
    /// authored in, with a trailing slash.
    /// </summary>
    internal static string DirectoryOf(DefinitionKind kind)
    {
        CategoryDescriptor descriptor = CategorySchemas.Describe(kind);
        return "content/" + ContentCategories.Describe(descriptor.Category).DirectoryName + "/";
    }

    /// <summary>
    /// How this kind's corpus is selected, for the report: the directory, or the one
    /// aggregate file, or the directory minus the aggregates that share it.
    /// </summary>
    internal static string DescribeSelection(DefinitionKind kind)
    {
        string directory = DirectoryOf(kind);
        if (Aggregates.TryGetValue(kind, out string? aggregate))
        {
            return directory + aggregate;
        }

        List<string> shared = new();
        foreach (KeyValuePair<DefinitionKind, string> entry in Aggregates)
        {
            if (string.Equals(DirectoryOf(entry.Key), directory, StringComparison.Ordinal))
            {
                shared.Add(entry.Value);
            }
        }

        shared.Sort(StringComparer.Ordinal);
        return shared.Count == 0 ? directory : directory + " excluding " + string.Join(", ", shared);
    }

    /// <summary>
    /// The repository-relative paths of <paramref name="kind"/>'s authored definitions
    /// beneath <paramref name="root"/>, in ordinal path order.
    /// </summary>
    /// <remarks>
    /// <paramref name="root"/> is a parameter rather than
    /// <c>TestArtifacts.RepositoryRoot</c> so a negative control can measure a mutated
    /// copy of the real corpus. A control that had to mutate <c>content/</c> in place
    /// would be a control that writes to a path this package does not own.
    /// </remarks>
    internal static IReadOnlyList<string> FilesOf(DefinitionKind kind, string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);

        string relativeDirectory = DirectoryOf(kind);
        string absoluteDirectory = Path.Combine(root, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        List<string> files = new();
        if (!Directory.Exists(absoluteDirectory))
        {
            return files;
        }

        if (Aggregates.TryGetValue(kind, out string? aggregate))
        {
            string aggregatePath = Path.Combine(absoluteDirectory, aggregate);
            if (File.Exists(aggregatePath))
            {
                files.Add(relativeDirectory + aggregate);
            }

            return files;
        }

        HashSet<string> excludedNames = new(StringComparer.Ordinal);
        foreach (KeyValuePair<DefinitionKind, string> entry in Aggregates)
        {
            if (string.Equals(DirectoryOf(entry.Key), relativeDirectory, StringComparison.Ordinal))
            {
                excludedNames.Add(entry.Value);
            }
        }

        foreach (string path in Directory.GetFiles(absoluteDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(path);
            if (!excludedNames.Contains(name))
            {
                files.Add(relativeDirectory + name);
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    /// <summary>
    /// Every authored definition beneath <paramref name="root"/>, independently of the
    /// bindings above: every <c>*.json</c> under <c>content/</c> that is neither a schema
    /// nor a localization catalog.
    /// </summary>
    /// <remarks>
    /// The second anchor for the population. The bindings answer "which files does this
    /// schema govern"; this answers "which files are there", by walking the tree instead
    /// of consulting the table. A definition no schema is bound to appears in this set and
    /// in no corpus, which is what makes an unbound directory a failure rather than a
    /// silence.
    /// </remarks>
    internal static IReadOnlyList<string> EveryAuthoredDefinition(string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);

        string contentRoot = Path.Combine(root, "content");
        List<string> files = new();
        foreach (string path in Directory.GetFiles(contentRoot, "*.json", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (relative.StartsWith("content/schemas/", StringComparison.Ordinal)
                || relative.StartsWith("content/localization/", StringComparison.Ordinal))
            {
                continue;
            }

            files.Add(relative);
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }
}
