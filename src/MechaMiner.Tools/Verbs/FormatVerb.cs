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
        OwnedTextInspection inspection = InspectOwnedText(context, write);
        List<TextViolation> violations = inspection.Violations;
        string violationReport = OwnedTextHygiene.RenderReport(violations);
        string reportPath = context.WriteArtifact("owned-text-violations.txt", violationReport);
        context.Console.Write(violationReport);

        // The candidate set is asserted before its contents are judged. Without this
        // the gate passed whenever the set came back empty, which included the case
        // where `git ls-files` failed outright: zero files inspected produced zero
        // violations, and zero violations read as compliance. Doc 100 requires format
        // to "fail if the resulting tree still violates policy", and a gate that
        // inspected nothing has not established anything about the tree.
        context.Runner.RecordAssertion(
            "owned-text-file-set",
            inspection.SetIsUsable,
            inspection.SetDetail);
        if (!inspection.SetIsUsable)
        {
            failures.Add("owned-text file enumeration (" + inspection.SetDetail + ")");
        }

        bool ownedTextClean = inspection.SetIsUsable && violations.Count == 0;
        context.Runner.RecordAssertion(
            "owned-text-rules",
            ownedTextClean,
            ownedTextClean
                ? inspection.Files.Count.ToString(CultureInfo.InvariantCulture)
                    + " owned text file(s) inspected; every one satisfies end_of_line,"
                    + " insert_final_newline, and trim_trailing_whitespace"
                : !inspection.SetIsUsable
                    ? "not evaluated: the owned text file set was not obtained, so no"
                        + " conclusion about the tree is available"
                    : violations.Count.ToString(CultureInfo.InvariantCulture)
                        + " violation(s) across " + inspection.Files.Count.ToString(CultureInfo.InvariantCulture)
                        + " inspected file(s); see " + reportPath);
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
        OwnedTextInspection recheck = InspectOwnedText(context, write: false);
        List<TextViolation> remaining = recheck.Violations;

        List<string> unrepaired = new();
        if (!verifyWhitespace.Succeeded)
        {
            unrepaired.Add("C# whitespace");
        }

        if (!verifyStyle.Succeeded)
        {
            unrepaired.Add("C# style diagnostics");
        }

        // Gate 4 is the "fail if the resulting tree still violates policy" re-verify, so
        // it must not conclude "repaired" from a re-check that could not read the tree.
        context.Runner.RecordAssertion(
            "owned-text-file-set-recheck",
            recheck.SetIsUsable,
            recheck.SetDetail);
        if (!recheck.SetIsUsable)
        {
            unrepaired.Add("the owned-text re-verify could not obtain its file set ("
                + recheck.SetDetail + "), so the formatted tree is unverified");
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

    /// <summary>
    /// The result of one owned-text pass, keeping "the set could not be obtained"
    /// distinguishable from "the set was obtained and held no violations".
    /// </summary>
    /// <remarks>
    /// Collapsing those two into an empty violation list is what made this gate pass
    /// on a tree it had never read. A gate reports on the input set it actually got,
    /// and says so when it got none.
    /// </remarks>
    private sealed class OwnedTextInspection
    {
        /// <summary>Owned text files that were enumerated and inspected.</summary>
        internal List<string> Files { get; init; } = new();

        /// <summary>Rules the inspected files violated.</summary>
        internal List<TextViolation> Violations { get; init; } = new();

        /// <summary>
        /// Whether the candidate set is fit to draw a conclusion from: the enumeration
        /// succeeded and returned at least one owned text file.
        /// </summary>
        internal bool SetIsUsable { get; init; }

        /// <summary>Human-readable statement of what the set was, or why there is none.</summary>
        internal string SetDetail { get; init; } = string.Empty;
    }

    private static OwnedTextInspection InspectOwnedText(VerbContext context, bool write)
    {
        CommandResult listed = context.Runner.Run(
            "list-owned-text-files",
            "git",
            new[] { "ls-files", "--cached", "--others", "--exclude-standard" },
            context.Layout.Root,
            TimeSpan.FromMinutes(2));

        if (!listed.Succeeded)
        {
            // A failed enumeration is a gate failure, never an empty set. `git ls-files`
            // fails on a broken or absent repository, a stale GIT_DIR, or a timeout, and
            // in every one of those cases the tree is unexamined rather than clean.
            return new OwnedTextInspection
            {
                SetIsUsable = false,
                SetDetail = "git ls-files failed with exit "
                    + listed.ExitCode.ToString(CultureInfo.InvariantCulture)
                    + (listed.TimedOut ? " (timed out)" : string.Empty)
                    + "; the owned text file set is unknown, so this gate cannot pass",
            };
        }

        List<string> files = new();
        foreach (string line in listed.Output.Split('\n'))
        {
            string path = line.Trim();
            if (path.Length > 0 && OwnedTextHygiene.IsOwned(path))
            {
                files.Add(path);
            }
        }

        // Lexical order keeps the report stable, and a brand new file is covered before
        // it is committed because --others includes untracked, non-ignored paths.
        files.Sort(StringComparer.Ordinal);

        if (files.Count == 0)
        {
            // An empty candidate set never satisfies a gate. This repository always
            // contains owned text files - .editorconfig and this file among them - so
            // zero matches means the enumeration or the ownership filter is broken, not
            // that the tree is compliant.
            return new OwnedTextInspection
            {
                SetIsUsable = false,
                SetDetail = "git ls-files succeeded but matched zero owned text files, which"
                    + " cannot be true of this repository; the enumeration or the"
                    + " ownership filter is broken",
            };
        }

        List<TextViolation> violations = new();
        int inspected = 0;
        List<string> unreadable = new();
        foreach (string relative in files)
        {
            string absolute = context.Layout.Absolute(relative);
            if (!File.Exists(absolute))
            {
                // git listed it and the filesystem does not have it. That is a real
                // inconsistency, not a file to skip quietly.
                unreadable.Add(relative);
                continue;
            }

            inspected++;
            violations.AddRange(OwnedTextHygiene.Inspect(absolute, relative, write));
        }

        if (unreadable.Count > 0)
        {
            return new OwnedTextInspection
            {
                Files = files,
                Violations = violations,
                SetIsUsable = false,
                SetDetail = unreadable.Count.ToString(CultureInfo.InvariantCulture)
                    + " path(s) that git listed are missing from the working tree, so the"
                    + " set was inspected only in part; first: " + unreadable[0],
            };
        }

        return new OwnedTextInspection
        {
            Files = files,
            Violations = violations,
            SetIsUsable = true,
            SetDetail = inspected.ToString(CultureInfo.InvariantCulture)
                + " owned text file(s) enumerated and inspected",
        };
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
