using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
/// registries the walk read - and false of the other eighteen. Extending the walk to the whole
/// directory reported 135 strings as missing files. Not one of them was a missing file. They
/// were three other conventions the check had never been taught:
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
/// written to the run's output. The 22 prose references are reported as what they are -
/// unverifiable - rather than passing quietly among the 439 checkable ones: the repository-path,
/// path-and-section and path-and-case references, measured over <c>tests/verification/</c> at
/// <c>a45497c</c>.
/// </para>
/// <para>
/// <b>Neither figure in that sentence is pinned here, and both can be checked.</b> "Checkable" is
/// <see cref="IsVerifiable"/>, which excludes <see cref="Form.Prose"/> and nothing else, so 439 is
/// the sum of the census's three non-prose literals - 326 repository-path, 108 path-and-section, 5
/// path-and-case - and equally its 461 references less the 22 prose ones. Both readings are
/// asserted by the census named above, so a reader who doubts 439 can add those literals rather
/// than re-walk the directory - and a re-measurement that moves them dates this sentence too.
/// 432 is the superseded figure. It was correct at <c>d1a81c3</c>, which wrote it beside
/// <c>RepositoryPathReferences = 319</c>, and it was left behind by <c>3b5ed6e</c>, which re-pinned
/// that literal to 326 for the seven behavior-token fixtures <c>VER-DAT-002-037</c> names as its
/// evidence. The path-and-section, path-and-case and prose counts have not moved since
/// <c>d1a81c3</c>, so the whole 432-to-439 delta is that entry's +7.
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

        /// <summary>
        /// A type name rather than a path - <c>FailingLogSink</c>. Checkable: a source file in
        /// the scanned test roots must declare a type of that name.
        /// </summary>
        TypeName,

        /// <summary>
        /// A path under a build-output root that the entry's verification PRODUCES rather than
        /// reads - <c>artifacts/benchmark/sample-report.json</c>. Checkable, and the assertion
        /// INVERTS: the path must be under a declared output root, must be ignored by git, and
        /// must be absent from the committed tree. An expected output that has been committed is
        /// a real defect, so this form fails on presence.
        /// </summary>
        ExpectedOutput,

        /// <summary>
        /// A machine-private value an entry feeds to redaction - <c>/home/someone-else/…</c>.
        /// Checkable, and the strongest of the three: the value must appear in NO COMMITTED FILE
        /// OUTSIDE <c>tests/</c>, which makes the reference a leak detector rather than a path.
        /// The registry that declares the sample and the test that feeds it to redaction are
        /// where such a value legitimately lives, so the scope is "has not escaped the test
        /// material", not "is absent from the tree" - see <c>CommittedFilesOutsideTestsContaining</c>.
        /// </summary>
        RedactionSample,

        /// <summary>An English description of a manual perturbation. Not checkable at all.</summary>
        Prose,
    }

    /// <summary>Roots whose contents are build outputs rather than committed sources.</summary>
    /// <remarks>
    /// <para>
    /// Declared here so <see cref="Form.ExpectedOutput"/> means "under a root we have named as
    /// an output root" rather than "happens to start with a string". Adding a root is a
    /// deliberate act; a path outside all of them is not an expected output.
    /// </para>
    /// <para>
    /// A root qualifies only if it is WHOLLY ignored, which is why <c>generated/</c> is not
    /// here: <c>.gitignore</c> ignores only <c>generated/build-manifest.json</c> within it and
    /// <c>generated/.gitkeep</c> is committed, so "under generated/" would not imply "not
    /// committed" and the inverted assertion below would be unsound for it.
    /// </para>
    /// </remarks>
    private static readonly string[] OutputRoots = ["artifacts/"];

    /// <summary>A bare PascalCase identifier, which is a type name and not a path.</summary>
    private static readonly Regex TypeNameShape = new(
        @"^[A-Z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>
    /// An absolute path: POSIX <c>/…</c> or Windows <c>C:\…</c>. A repository-relative fixture
    /// path is never absolute, so an absolute one is naming a machine outside this checkout.
    /// </summary>
    private static readonly Regex AbsolutePathShape = new(
        @"^(/|[A-Za-z]:\\)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

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

        // These three precede RepositoryPath because each is a NON-PATH that a path-shaped test
        // would reject. The classifier's fault was never that it miscategorised them: it was
        // that RepositoryPath was the only bucket that checked anything, so the only escape from
        // a false accusation was Prose, which checks nothing. Each of these asserts something,
        // and two of them assert the OPPOSITE of "exists at this path".
        if (AbsolutePathShape.IsMatch(reference))
        {
            return Form.RedactionSample;
        }

        foreach (string root in OutputRoots)
        {
            if (reference.StartsWith(root, StringComparison.Ordinal))
            {
                return Form.ExpectedOutput;
            }
        }

        if (TypeNameShape.IsMatch(reference))
        {
            return Form.TypeName;
        }

        return AnyWhitespace.IsMatch(reference) ? Form.Prose : Form.RepositoryPath;
    }

    /// <summary>Whether a form is one this suite can resolve at all.</summary>
    /// <remarks>
    /// <see cref="Form.Prose"/> remains the only unverifiable form. The three forms added
    /// alongside it are all verifiable, which is the whole point of adding them rather than
    /// widening Prose: routing them to Prose would have traded 25 false accusations for three
    /// silently abandoned checks, and nothing afterwards would have marked the loss.
    /// </remarks>
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
        return Unresolved(reference, CommittedPaths);
    }

    /// <summary>
    /// <see cref="Unresolved(string)"/> against a supplied set of committed paths.
    /// </summary>
    /// <remarks>
    /// THE SEAM EXISTS BECAUSE THE CORRECT STATE OF THE TREE MAKES ONE LEG UNTESTABLE. The
    /// refusing leg of <see cref="Form.ExpectedOutput"/> fires when a build output has been
    /// COMMITTED, and nothing under <c>artifacts/</c> is committed - which is exactly the state
    /// the form asserts. So there is no natural probe for it, and the alternatives were to leave
    /// the leg unproved or to force-add a file and mutate the index during a test run. Injecting
    /// the set proves the branch on a tree that is correct, which is the only way this control
    /// can exist at all.
    /// </remarks>
    internal static string? Unresolved(string reference, IReadOnlySet<string> committedPaths)
    {
        Form form = FormOf(reference);
        if (form == Form.Prose)
        {
            return null;
        }

        if (form == Form.TypeName)
        {
            return RegistrySelectorTypes.DeclaresTypeNamed(reference)
                ? null
                : reference + " (TypeName: no source file in the scanned test roots declares a "
                    + "type named '" + reference + "')";
        }

        if (form == Form.ExpectedOutput)
        {
            // Inverted on purpose: this path is PRODUCED by the verification, so its absence
            // from the committed tree is the property, and its presence is the defect. Asserting
            // existence here is what made three FND-008 references look like dangling paths.
            if (!GitIgnores(reference))
            {
                return reference + " (ExpectedOutput: '" + reference + "' is under an output "
                    + "root but git does not ignore it, so a produced artifact would be "
                    + "committable and the tree could not tell evidence from source)";
            }

            return committedPaths.Contains(reference)
                ? reference + " (ExpectedOutput: '" + reference + "' is COMMITTED. An expected "
                    + "output that is in the tree is evidence that cannot have come from this "
                    + "run, which is the failure this form exists to catch)"
                : null;
        }

        if (form == Form.RedactionSample)
        {
            IReadOnlyCollection<string> leaks =
                CommittedFilesOutsideTestsContaining(reference);
            return leaks.Count == 0
                ? null
                : reference + " (RedactionSample: this machine-private value is meant to exist "
                    + "only as redaction input, and it appears in the committed tree in "
                    + string.Join(", ", leaks) + ")";
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

    /// <summary>
    /// Every path git tracks, relative to the repository root with forward slashes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One <c>git ls-files</c> for the whole suite, cached. It is the only way to ask "is this
    /// COMMITTED", which is a different question from "is this on disk" - and the difference is
    /// the entire point of <see cref="Form.ExpectedOutput"/>, whose paths routinely exist
    /// locally after a build and must never be committed.
    /// </para>
    /// <para>
    /// A failure to run git is a FAILURE and never a skip. The remarks on
    /// <see cref="RegistrySelectorTypes"/> reject <c>Assembly.LoadFrom</c> for precisely this
    /// reason - a check that passes when its input is unavailable is worse than one that fails -
    /// and the same rule applies here. Every gate in this repository already requires git.
    /// </para>
    /// </remarks>
    private static IReadOnlySet<string> CommittedPaths => TrackedPaths.Value;

    private static readonly Lazy<IReadOnlySet<string>> TrackedPaths = new(ReadTrackedPaths);

    private static IReadOnlySet<string> ReadTrackedPaths()
    {
        using Process git = new();
        git.StartInfo = new ProcessStartInfo("git", "ls-files -z")
        {
            WorkingDirectory = TestArtifacts.RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        git.Start();
        string output = git.StandardOutput.ReadToEnd();
        string error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "git ls-files exited " + git.ExitCode.ToString(CultureInfo.InvariantCulture)
                + ", so which files are committed is unknown. This is reported as a failure "
                + "rather than skipped, because a fixture check that passes when it cannot read "
                + "its input proves nothing: " + error);
        }

        HashSet<string> tracked = new(StringComparer.Ordinal);
        foreach (string path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            tracked.Add(path);
        }

        if (tracked.Count == 0)
        {
            throw new InvalidOperationException(
                "git ls-files returned no paths, so every ExpectedOutput would read as "
                + "not-committed and every RedactionSample as not-leaked. An empty answer is a "
                + "vacuous pass, not a clean tree.");
        }

        return tracked;
    }

    /// <summary>Whether <c>.gitignore</c> ignores the output root <paramref name="reference"/> sits under.</summary>
    private static bool GitIgnores(string reference)
    {
        string ignoreFile = Path.Combine(TestArtifacts.RepositoryRoot, ".gitignore");
        if (!File.Exists(ignoreFile))
        {
            return false;
        }

        HashSet<string> lines = new(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(ignoreFile))
        {
            lines.Add(line.Trim());
        }

        foreach (string root in OutputRoots)
        {
            if (reference.StartsWith(root, StringComparison.Ordinal)
                && (lines.Contains(root) || lines.Contains(root.TrimEnd('/'))))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether git tracks <paramref name="reference"/>.</summary>
    /// <remarks>
    /// Internal so <see cref="RegistrySelectorTypesTests"/> can assert its committed-output
    /// probe really is committed - a probe that quietly stopped being tracked would make that
    /// control's refusing leg pass vacuously.
    /// </remarks>
    internal static bool IsPathTrackedByGit(string reference)
    {
        return CommittedPaths.Contains(reference);
    }

    /// <summary>
    /// Committed files OUTSIDE <c>tests/</c> whose text contains <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SCOPED TO OUTSIDE <c>tests/</c>, AND THAT IS A CORRECTION TO THE RULE AS FIRST WRITTEN,
    /// NOT A WEAKENING OF IT. "Appears nowhere in the committed tree" is false of these values
    /// by construction: the registry that declares the sample contains it, and so does the test
    /// that feeds it to redaction - <c>tests/verification/FND-007.json</c> and
    /// <c>tests/MechaMiner.Diagnostics.Tests/Logging/DiagnosticLogTests.cs</c> are exactly where
    /// a redaction sample is supposed to live. An assertion that every declared sample is a leak
    /// would fail on every correct corpus.
    /// </para>
    /// <para>
    /// What is worth asserting is that the value has not escaped the test material into shipped
    /// or published text: <c>src/</c>, <c>docs/</c>, <c>game/</c>, <c>build/</c>, workflows or
    /// configuration. That is a real leak detector with a real control, and it is stated as a
    /// property rather than as an exemption list - no path is excused by name, the rule is about
    /// where such a value may legitimately appear at all.
    /// </para>
    /// </remarks>
    internal static IReadOnlyCollection<string> CommittedFilesOutsideTestsContaining(
        string value)
    {
        List<string> hits = [];
        foreach (string path in CommittedPaths)
        {
            if (path.StartsWith("tests/", StringComparison.Ordinal))
            {
                continue;
            }

            string absolute = Path.Combine(TestArtifacts.RepositoryRoot, path);
            if (!File.Exists(absolute))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(absolute);
            }
            catch (IOException)
            {
                continue;
            }

            if (text.Contains(value, StringComparison.Ordinal))
            {
                hits.Add(path);
            }
        }

        return hits;
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
