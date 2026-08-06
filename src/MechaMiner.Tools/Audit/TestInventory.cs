using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MechaMiner.Tools.Audit;

/// <summary>
/// The set of tests the NUnit harness actually discovers, as a value.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>). Authority:
/// <c>docs/technical/91-verification-strategy.md</c> § Verification registry
/// ("automated selector"). Requirements: <c>TR-QUA-004</c>, <c>TR-AGT-003</c>.
/// </para>
/// <para>
/// A verification entry with an <c>nunit</c> selector claims that a named test proves the
/// entry. Checking only that the selector begins with the namespace of a known test
/// project accepts any class and any method name after that prefix, so an entry could
/// carry <c>status: implemented</c> while nothing anywhere ran. Resolving the selector
/// against the harness's own discovery output is what makes the claim falsifiable.
/// </para>
/// <para>
/// Discovery deliberately calls the real discoverer -
/// <c>dotnet test --list-tests</c> per test project - rather than re-deriving the test
/// list from assembly metadata. NUnit decides what a test is: inherited fixtures,
/// <c>TestCase</c> expansion, <c>TestCaseSource</c>, and fixture-level parameterisation
/// all change the answer. A second implementation of those rules would be one more thing
/// a reviewer has to trust, and the failure it would produce - a selector rejected
/// because our reimplementation disagreed with NUnit - is exactly the false alarm that
/// gets a check deleted. <c>NUnit.DisplayName=FullName</c> is passed through to the
/// adapter because the default display name is the bare method name, which cannot be
/// matched against a namespace-qualified selector.
/// </para>
/// <para>
/// <see cref="IsEmpty"/> is the empty-set guard. A build failure, a moved project, or a
/// changed CLI output format all produce zero discovered tests, and "no selector
/// contradicted an empty list" is not evidence of anything. The validator reports an
/// empty inventory as its own failure rather than resolving selectors against nothing,
/// and <see cref="DiscoveryReport"/> carries the per-project counts and the child
/// process's own words so the cause is in front of the reader.
/// </para>
/// </remarks>
internal sealed class TestInventory
{
    /// <summary>
    /// The NUnit adapter setting that makes <c>--list-tests</c> print namespace-qualified
    /// names instead of bare method names.
    /// </summary>
    internal const string FullNameSetting = "NUnit.DisplayName=FullName";

    private readonly ImmutableArray<string> _names;

    private TestInventory(ImmutableArray<string> names, string discoveryReport)
    {
        _names = names;
        DiscoveryReport = discoveryReport;
    }

    /// <summary>Every discovered test name, namespace-qualified, sorted and deduplicated.</summary>
    internal ImmutableArray<string> Names => _names;

    /// <summary>How many tests were discovered.</summary>
    internal int Count => _names.Length;

    /// <summary>
    /// Whether discovery produced nothing, which is a harness failure rather than a
    /// registry that happens to cite no test.
    /// </summary>
    internal bool IsEmpty => _names.IsEmpty;

    /// <summary>Reviewable text describing how the inventory was obtained.</summary>
    internal string DiscoveryReport { get; }

    /// <summary>An inventory of exactly the given names, for a test that supplies its own.</summary>
    internal static TestInventory Of(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        return Of((IEnumerable<string>)names);
    }

    /// <summary>An inventory of exactly the given names, for a test or a fixture.</summary>
    internal static TestInventory Of(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        SortedSet<string> unique = new(StringComparer.Ordinal);
        foreach (string name in names)
        {
            string trimmed = name.Trim();
            if (trimmed.Length > 0)
            {
                unique.Add(trimmed);
            }
        }

        return new TestInventory(
            ImmutableArray.CreateRange(unique),
            "supplied directly: " + unique.Count.ToString(CultureInfo.InvariantCulture) + " test name(s)");
    }

    /// <summary>An inventory that discovered nothing, and why.</summary>
    internal static TestInventory Nothing(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new TestInventory(ImmutableArray<string>.Empty, reason);
    }

    /// <summary>
    /// Whether <paramref name="selector"/> names at least one discovered test.
    /// </summary>
    /// <remarks>
    /// The three accepted shapes are the three a registry uses: the exact full name of a
    /// test, a namespace or class that contains tests (<c>selector + "."</c>), and a
    /// parameterised method whose discovered names carry their arguments
    /// (<c>selector + "("</c>). Nothing else matches, so a truncated or misspelled
    /// segment is a failure rather than a lucky prefix.
    /// </remarks>
    internal bool Resolves(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return false;
        }

        foreach (string name in _names)
        {
            if (string.Equals(name, selector, StringComparison.Ordinal)
                || name.StartsWith(selector + ".", StringComparison.Ordinal)
                || name.StartsWith(selector + "(", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Asks the NUnit harness which tests exist, one accepted test project at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The project list is <see cref="AcceptedArchitecture"/>'s own <c>tests/</c> rows, so
    /// this does not introduce a second roster of test projects that could drift from the
    /// accepted decomposition. Only the test list itself is discovered; which projects
    /// exist is already an asserted property.
    /// </para>
    /// <para>
    /// <c>--no-build</c> is deliberate. Discovery runs from inside a test process, and a
    /// build launched there could rewrite the assembly currently executing.
    /// </para>
    /// <para>
    /// The consequence, stated plainly because a comment here used to state the opposite:
    /// this method reports what is in <c>bin/</c>, not what is in the tree, and it is the
    /// caller's job to have built first. A partly built tree does not discover nothing. It
    /// discovers the subset of projects that happen to be built, and the missing ones surface
    /// as <c>UnresolvedTestSelector</c> findings that blame the registry entries citing them
    /// rather than naming the build state that caused it. The empty-set guard
    /// (<c>EmptyTestInventory</c>) only fires when <i>no</i> project discovers anything, which
    /// is the fully unbuilt case and the explicitly-supplied-empty-inventory case that
    /// <c>RegistryValidatorTests.AnEmptyTestInventoryFailsRatherThanPassingWithNothingToCompare</c>
    /// controls. It is a backstop, not the path a partly stale tree takes.
    /// </para>
    /// <para>
    /// <c>build/verify-registry.sh</c> is therefore responsible for building every accepted
    /// test project before it reaches this method, and maps a build failure to exit class 5
    /// with <c>MMT-5001</c> instead of computing a registry verdict. Its stage 0 comment
    /// records the measurement that made this necessary: the same tree with different
    /// <c>bin/</c> contents produced three different verdicts, including a PASS that certified
    /// a citation to a test already deleted from source.
    /// </para>
    /// </remarks>
    internal static TestInventory Discover(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        SortedSet<string> names = new(StringComparer.Ordinal);
        StringBuilder report = new();
        report.Append("dotnet test --list-tests -- ")
            .Append(FullNameSetting)
            .Append(", per accepted test project:")
            .Append('\n');

        foreach (AcceptedProject project in AcceptedArchitecture.Projects)
        {
            if (!project.ProjectPath.StartsWith("tests/", StringComparison.Ordinal))
            {
                continue;
            }

            string absolute = Path.Combine(
                repositoryRoot,
                project.ProjectPath.Replace('/', Path.DirectorySeparatorChar));

            (int discovered, string diagnostic) = ListProject(repositoryRoot, absolute, project.Name, names);
            report.Append("  ")
                .Append(project.ProjectPath)
                .Append(": ")
                .Append(discovered.ToString(CultureInfo.InvariantCulture))
                .Append(" test(s)");
            if (diagnostic.Length > 0)
            {
                report.Append(" -- ").Append(diagnostic);
            }

            report.Append('\n');
        }

        report.Append("  total: ").Append(names.Count.ToString(CultureInfo.InvariantCulture)).Append(" test(s)");
        return new TestInventory(ImmutableArray.CreateRange(names), report.ToString());
    }

    private static (int Discovered, string Diagnostic) ListProject(
        string repositoryRoot,
        string projectPath,
        string assemblyName,
        SortedSet<string> into)
    {
        if (!File.Exists(projectPath))
        {
            return (0, "the project file does not exist");
        }

        ProcessStartInfo start = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in new[]
        {
            "test",
            projectPath,
            "--no-build",
            "--nologo",
            "-v",
            "quiet",
            "--list-tests",
            "--",
            FullNameSetting,
        })
        {
            start.ArgumentList.Add(argument);
        }

        // Noninteractive and locale-stable, so the output this parses does not depend on
        // the caller's environment.
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";

        string output;
        int exitCode;
        try
        {
            using Process process = Process.Start(start)
                ?? throw new InvalidOperationException("dotnet did not start");
            output = process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
            if (!process.WaitForExit((int)TimeSpan.FromMinutes(10).TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return (0, "discovery timed out after 10 minutes");
            }

            exitCode = process.ExitCode;
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (0, "dotnet could not be launched: " + error.Message);
        }

        int discovered = 0;
        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.Trim();

            // Anchored on our own assembly name rather than on the CLI's headings, so a
            // reworded header cannot be mistaken for a test and a test cannot be missed
            // because the heading changed.
            if (trimmed.StartsWith(assemblyName + ".", StringComparison.Ordinal))
            {
                into.Add(trimmed);
                discovered++;
            }
        }

        if (discovered == 0)
        {
            return (0, "exit " + exitCode.ToString(CultureInfo.InvariantCulture) + "; last output: " + Tail(output));
        }

        return (discovered, string.Empty);
    }

    private static string Tail(string output)
    {
        string[] lines = output.Split('\n');
        List<string> kept = new();
        for (int index = lines.Length - 1; index >= 0 && kept.Count < 5; index--)
        {
            string trimmed = lines[index].Trim();
            if (trimmed.Length > 0)
            {
                kept.Insert(0, trimmed);
            }
        }

        return string.Join(" | ", kept);
    }
}
