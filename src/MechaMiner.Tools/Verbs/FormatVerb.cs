using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Tools.Cli;
using MechaMiner.Tools.Text;

namespace MechaMiner.Tools.Verbs;

/// <summary>
/// <c>format</c> and <c>format-check</c>: the single owner of whitespace and
/// formatting enforcement.
/// </summary>
/// <remarks>
/// <para>
/// Doc 100 § C# project standards: "Formatting and naming enforced through
/// <c>.editorconfig</c> and one repository command." Naming is a build diagnostic
/// (<c>IDE1006</c> is <c>error</c> in <c>.editorconfig</c> and
/// <c>EnforceCodeStyleInBuild</c> is on), and FND-001 deliberately left
/// <c>IDE0055</c> and <c>IDE0005</c> at <c>suggestion</c> so that whitespace and
/// unnecessary-using enforcement has exactly one owner: this verb. Do not move
/// either diagnostic back into the build.
/// </para>
/// <para>
/// The verb has three gates, in this order: C# whitespace, the two style
/// diagnostics the build deliberately leaves at suggestion severity, and the
/// repository-wide owned-text rules that <c>dotnet format</c> does not cover at all.
/// <c>format-check</c> runs the identical gates with writes disabled.
/// </para>
/// </remarks>
internal static class FormatVerb
{
    /// <summary>Formats the tree and fails if it still violates policy afterwards.</summary>
    internal static VerbOutcome Format(VerbContext context)
    {
        return Run(context, write: true);
    }

    /// <summary>Validates formatting without writing anything.</summary>
    internal static VerbOutcome Check(VerbContext context)
    {
        return Run(context, write: false);
    }

    private static VerbOutcome Run(VerbContext context, bool write)
    {
        List<string> failures = new();

        context.Section(write
            ? "gate 1: C# whitespace (dotnet format whitespace)"
            : "gate 1: C# whitespace (dotnet format whitespace --verify-no-changes)");

        CommandResult whitespace = context.Runner.Run(
            "dotnet-format-whitespace",
            "dotnet",
            BuildArguments("whitespace", context.Layout.Solution, write, diagnostics: null),
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        if (!whitespace.Succeeded)
        {
            failures.Add("C# whitespace (IDE0055)");
        }

        context.Section("gate 2: C# style diagnostics this verb owns (IDE0055, IDE0005)");
        CommandResult style = context.Runner.Run(
            "dotnet-format-style",
            "dotnet",
            BuildArguments("style", context.Layout.Solution, write, diagnostics: new[] { "IDE0055", "IDE0005" }),
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        if (!style.Succeeded)
        {
            failures.Add("C# style diagnostics (IDE0055, IDE0005)");
        }

        context.Section("gate 3: repository-wide owned-text rules from .editorconfig [*]");
        List<TextViolation> violations = InspectOwnedText(context, write);
        string violationReport = OwnedTextHygiene.RenderReport(violations);
        string reportPath = context.WriteArtifact("owned-text-violations.txt", violationReport);
        context.Console.Write(violationReport);
        context.Runner.RecordAssertion(
            "owned-text-rules",
            violations.Count == 0,
            violations.Count == 0
                ? "every owned text file satisfies end_of_line, insert_final_newline, and trim_trailing_whitespace"
                : violations.Count.ToString(CultureInfo.InvariantCulture) + " violation(s); see " + reportPath);
        if (violations.Count > 0)
        {
            failures.Add("owned-text rules");
        }

        if (!write)
        {
            if (failures.Count == 0)
            {
                return VerbOutcome
                    .Success("format-check passed all three gates; nothing was written")
                    .WithArtifact(reportPath);
            }

            return VerbOutcome
                .Validation("format-check failed: " + string.Join("; ", failures)
                    + ". Run ./build.sh format to repair, then review the diff.")
                .WithArtifact(reportPath);
        }

        // "format ... fail if the resulting tree still violates policy": re-verify
        // rather than trusting that the writing pass fixed everything, because
        // dotnet format reports but cannot repair every diagnostic.
        context.Section("gate 4: re-verify that the formatted tree satisfies policy");
        CommandResult verifyWhitespace = context.Runner.Run(
            "verify-format-whitespace",
            "dotnet",
            BuildArguments("whitespace", context.Layout.Solution, write: false, diagnostics: null),
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        CommandResult verifyStyle = context.Runner.Run(
            "verify-format-style",
            "dotnet",
            BuildArguments("style", context.Layout.Solution, write: false, diagnostics: new[] { "IDE0055", "IDE0005" }),
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        List<TextViolation> remaining = InspectOwnedText(context, write: false);

        List<string> unrepaired = new();
        if (!verifyWhitespace.Succeeded)
        {
            unrepaired.Add("C# whitespace");
        }

        if (!verifyStyle.Succeeded)
        {
            unrepaired.Add("C# style diagnostics");
        }

        if (remaining.Count > 0)
        {
            unrepaired.Add(remaining.Count.ToString(CultureInfo.InvariantCulture) + " owned-text violation(s)");
        }

        if (unrepaired.Count > 0)
        {
            return VerbOutcome
                .Validation("format wrote changes but the tree still violates policy: "
                    + string.Join("; ", unrepaired))
                .WithArtifact(reportPath);
        }

        int changed = violations.Count;
        return VerbOutcome
            .Success("format completed; the formatted tree satisfies every gate ("
                + changed.ToString(CultureInfo.InvariantCulture) + " owned-text violation(s) repaired)")
            .WithArtifact(reportPath);
    }

    private static List<TextViolation> InspectOwnedText(VerbContext context, bool write)
    {
        List<TextViolation> violations = new();
        foreach (string relative in EnumerateOwnedTextFiles(context))
        {
            string absolute = context.Layout.Absolute(relative);
            if (!File.Exists(absolute))
            {
                continue;
            }

            violations.AddRange(OwnedTextHygiene.Inspect(absolute, relative, write));
        }

        return violations;
    }

    /// <summary>
    /// Enumerates tracked and untracked-but-not-ignored owned text files in
    /// lexical order, so the report is stable and a brand new file is covered
    /// before it is committed.
    /// </summary>
    private static List<string> EnumerateOwnedTextFiles(VerbContext context)
    {
        CommandResult listed = context.Runner.Run(
            "list-owned-text-files",
            "git",
            new[] { "ls-files", "--cached", "--others", "--exclude-standard" },
            context.Layout.Root,
            TimeSpan.FromMinutes(2));

        List<string> files = new();
        if (!listed.Succeeded)
        {
            return files;
        }

        foreach (string line in listed.Output.Split('\n'))
        {
            string path = line.Trim();
            if (path.Length > 0 && OwnedTextHygiene.IsOwned(path))
            {
                files.Add(path);
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private static List<string> BuildArguments(
        string subcommand,
        string solution,
        bool write,
        IReadOnlyList<string>? diagnostics)
    {
        List<string> arguments = new() { "format", subcommand, solution, "--verbosity", "minimal" };
        if (diagnostics is not null)
        {
            arguments.Add("--diagnostics");
            arguments.AddRange(diagnostics);
            // The build deliberately leaves these at suggestion severity, so the
            // formatter has to be told to act on informational diagnostics.
            arguments.Add("--severity");
            arguments.Add("info");
        }

        if (!write)
        {
            arguments.Add("--verify-no-changes");
        }

        return arguments;
    }
}
