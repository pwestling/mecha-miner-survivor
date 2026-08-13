using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Classifies and resolves one string from a registry entry's <c>fixtures</c> array.
/// </summary>
/// <remarks>
/// <para>
/// <b>The field carries four conventions, and only one of them used to be checked.</b> The
/// walk over <c>fixtures</c> treated every string as a repository-relative path and called
/// <c>File.Exists</c> on it, which is true of DAT-001, DAT-002 and DAT-003 - the three
/// registries the walk read - and false of every other registry on disk. How many that is
/// follows from <see cref="VerificationRegistry.RegistriesOnDisk"/>, which
/// <see cref="VerificationRegistryTests.EveryRegistryOnDiskIsDiscoveredAndWalked"/> asserts;
/// this sentence used to do the subtraction itself and was wrong by the time the next registry
/// landed. Extending the walk to the whole directory reported a batch of strings as missing
/// files - how large a batch does not reproduce, because the population has grown since, and
/// the finding does not need it: not one of them was a missing file. They were three other
/// conventions the check had never been taught:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="Form.RepositoryPath"/> - <c>content/relics/REL-01.json</c>. Must exist.
/// </description></item>
/// <item><description>
/// <see cref="Form.PathAndSection"/> - <c>a/b.md § Some heading</c>. Both the file and the
/// heading must exist. This one is documented by the registries themselves: SIM-001.json says
/// the entries whose control it records "name it in their own fixtures, qualified by section".
/// </description></item>
/// <item><description>
/// <see cref="Form.PathAndCase"/> - <c>game/tests/Runner.tscn case=fail</c>. The file must
/// exist; the variant after <c>case=</c> names a runtime case and is not checkable from here.
/// </description></item>
/// <item><description>
/// <see cref="Form.Prose"/> - <c>working-tree injection: content/relics/REL-11.json, a copy of
/// REL-01.json with id REL-11</c>. An English description of a perturbation to make by hand.
/// <b>Nothing here can verify it.</b>
/// </description></item>
/// </list>
/// <para>
/// <b>Prose is a declared outcome, not a tolerated one.</b> A field where four shapes are legal
/// and one is checked is a field that verifies nothing while looking verified, so the count of
/// each form is asserted against committed literals by
/// <see cref="VerificationRegistryTests.TheFixtureReferenceCensusIsWhatIsDeclared"/> and
/// written to the run's output. The prose references are reported as what they are -
/// unverifiable - rather than passing quietly among the ones that are checked. How many
/// there are of each is what that test's <c>ProseReferences</c>,
/// <c>RepositoryPathReferences</c>, <c>PathAndSectionReferences</c> and
/// <c>PathAndCaseReferences</c> literals say, and this paragraph deliberately restates
/// none of them: a figure repeated here is asserted by nothing and goes stale silently.
/// </para>
/// <para>
/// <b>Classification is by shape, never by existence.</b> A tempting reading of "is this a
/// path?" is "does this file exist?", and it is a fail-open: deleting a cited fixture would
/// reclassify its reference from <see cref="Form.RepositoryPath"/> to <see cref="Form.Prose"/>
/// and the check would stop looking for the file instead of reporting it gone. A reference with
/// no whitespace is a path and must exist, whatever is on disk.
/// </para>
/// </remarks>
internal static class RegistryFixtureReferences
{
    /// <summary>The conventions a <c>fixtures</c> string may follow.</summary>
    internal enum Form
    {
        /// <summary>A repository-relative path. Checkable: the file or directory must exist.</summary>
        RepositoryPath,

        /// <summary>A path, <c> § </c>, and a heading in it. Checkable: both must exist.</summary>
        PathAndSection,

        /// <summary>A path and <c> case=variant</c>. Partly checkable: the file must exist.</summary>
        PathAndCase,

        /// <summary>An English description of a manual perturbation. Not checkable at all.</summary>
        Prose,
    }

    private const string SectionSeparator = " § ";

    private const string CaseSeparator = " case=";

    private static readonly Regex AnyWhitespace = new(
        @"\s", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly Dictionary<string, HashSet<string>> HeadingCache =
        new(StringComparer.Ordinal);

    /// <summary>Which convention <paramref name="reference"/> follows, by its shape alone.</summary>
    internal static Form FormOf(string reference)
    {
        if (reference.Contains(SectionSeparator, StringComparison.Ordinal))
        {
            return Form.PathAndSection;
        }

        if (reference.Contains(CaseSeparator, StringComparison.Ordinal))
        {
            return Form.PathAndCase;
        }

        return AnyWhitespace.IsMatch(reference) ? Form.Prose : Form.RepositoryPath;
    }

    /// <summary>Whether a form is one this suite can resolve at all.</summary>
    internal static bool IsVerifiable(Form form)
    {
        return form != Form.Prose;
    }

    /// <summary>
    /// Why <paramref name="reference"/> does not resolve, or <see langword="null"/> when it
    /// does. A <see cref="Form.Prose"/> reference returns <see langword="null"/> because there
    /// is nothing to resolve - never treat that as evidence it was checked; the census is what
    /// says how many went unchecked.
    /// </summary>
    internal static string? Unresolved(string reference)
    {
        Form form = FormOf(reference);
        if (form == Form.Prose)
        {
            return null;
        }

        string path = form switch
        {
            Form.PathAndSection => Split(reference, SectionSeparator).Before,
            Form.PathAndCase => Split(reference, CaseSeparator).Before,
            _ => reference,
        };

        string absolute = Path.Combine(TestArtifacts.RepositoryRoot, path);
        if (!File.Exists(absolute) && !Directory.Exists(absolute))
        {
            return reference + " (" + form + ": '" + path + "' does not exist)";
        }

        if (form != Form.PathAndSection)
        {
            return null;
        }

        string section = Split(reference, SectionSeparator).After;
        return Headings(absolute).Contains(section)
            ? null
            : reference + " (PathAndSection: '" + path + "' exists but has no heading '"
                + section + "')";
    }

    /// <summary>Every heading's text, verbatim, in a Markdown file.</summary>
    /// <remarks>
    /// The section half of a <see cref="Form.PathAndSection"/> reference is the heading's text
    /// as written, not a GitHub anchor - unlike the <c>#anchor</c> half of a
    /// <c>technicalSources</c> citation, which is why this does not reuse the anchor helper.
    /// </remarks>
    private static HashSet<string> Headings(string absolutePath)
    {
        lock (HeadingCache)
        {
            if (HeadingCache.TryGetValue(absolutePath, out HashSet<string>? cached))
            {
                return cached;
            }

            HashSet<string> headings = new(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(absolutePath))
            {
                if (line.StartsWith('#'))
                {
                    headings.Add(line.TrimStart('#').Trim());
                }
            }

            HeadingCache[absolutePath] = headings;
            return headings;
        }
    }

    private static (string Before, string After) Split(string reference, string separator)
    {
        int at = reference.IndexOf(separator, StringComparison.Ordinal);
        return (reference[..at], reference[(at + separator.Length)..]);
    }
}
