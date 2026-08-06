using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using MechaMiner.Tools.Cli;

namespace MechaMiner.Tools;

/// <summary>
/// The typed verb dispatcher behind <c>./build.sh</c> and <c>./build.ps1</c>.
/// </summary>
/// <remarks>
/// <para>
/// Both root wrappers are thin launchers over this one process, so the verb table,
/// argument names, exit classes, and structured output cannot diverge between shell
/// languages. Doc 100 § Standard command surface: the wrappers "are thin launchers
/// for pinned tools and project-owned typed tooling; domain workflow logic is not
/// duplicated between shell languages."
/// </para>
/// <para>
/// The process contract is: argument one is the repository root the wrapper
/// resolved, argument two is the verb, and the rest are the verb's arguments. The
/// root is passed rather than discovered so nothing searches upward for a marker
/// file (<c>TR-BLD-006</c>).
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>Dispatches one verb and returns its doc 100 exit class.</summary>
    internal static int Main(string[] args)
    {
        TextWriter console = Console.Out;
        TextWriter errors = Console.Error;

        if (args.Length < 1)
        {
            errors.WriteLine(
                "MechaMiner.Tools is invoked by ./build.sh or ./build.ps1, which pass the repository root first.");
            return ExitClass.Internal;
        }

        RepositoryLayout layout;
        try
        {
            layout = RepositoryLayout.ForRoot(args[0]);
        }
        catch (InvalidOperationException exception)
        {
            errors.WriteLine("FAILED: " + exception.Message);
            return ExitClass.Internal;
        }

        string[] rest = args[1..];
        if (rest.Length == 0)
        {
            errors.Write(Usage.Render(VerbRegistry.All));
            errors.WriteLine();
            errors.WriteLine("FAILED [MMT-2003] no verb given. Exit class 2 (invalid verb or arguments).");
            return ExitClass.InvalidInvocation;
        }

        string verbName = rest[0];
        string[] verbArguments = rest[1..];

        VerbDescriptor? descriptor = VerbRegistry.Find(verbName);
        if (descriptor is null)
        {
            errors.Write(Usage.Render(VerbRegistry.All));
            errors.WriteLine();
            errors.WriteLine(
                "FAILED [" + DiagnosticCodes.UnknownVerb + "] unknown verb '" + verbName
                + "'. Exit class 2 (invalid verb or arguments).");
            return ExitClass.InvalidInvocation;
        }

        ParsedArguments parsed = ParsedArguments.Parse(descriptor, verbArguments);
        if (!parsed.IsValid)
        {
            errors.Write(Usage.Render(VerbRegistry.All));
            errors.WriteLine();
            errors.WriteLine(
                "FAILED [" + DiagnosticCodes.InvalidArgument + "] " + parsed.Error
                + ". Expected: " + descriptor.ToInvocationText()
                + ". Exit class 2 (invalid verb or arguments).");
            return ExitClass.InvalidInvocation;
        }

        return Dispatch(layout, descriptor, parsed, verbArguments, console, errors);
    }

    private static int Dispatch(
        RepositoryLayout layout,
        VerbDescriptor descriptor,
        ParsedArguments parsed,
        IReadOnlyList<string> verbArguments,
        TextWriter console,
        TextWriter errors)
    {
        DateTimeOffset started = DateTimeOffset.UtcNow;
        string invocationId = started.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)
            + "-" + System.Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        string artifactDirectory = Path.Combine(
            layout.ArtifactsDirectory,
            "verbs",
            descriptor.Name,
            invocationId);
        Directory.CreateDirectory(artifactDirectory);

        InvocationRecord record = new()
        {
            Verb = descriptor.Name,
            OwningWorkPackage = descriptor.OwningWorkPackage,
            InvocationId = invocationId,
            StartedUtc = started.ToString("O", CultureInfo.InvariantCulture),
        };
        record.Arguments.AddRange(verbArguments);

        VerbContext context = new(layout, descriptor, parsed, record, artifactDirectory, console);
        RecordSourceRevision(context);

        Stopwatch stopwatch = Stopwatch.StartNew();
        VerbOutcome outcome;
        try
        {
            outcome = descriptor.Handler is null
                ? VerbOutcome.AwaitingOwner(descriptor.Name, descriptor.OwningWorkPackage, descriptor.RequiredEffect)
                : descriptor.Handler(context);
        }
#pragma warning disable CA1031 // The verb host is the last boundary; an unclassified failure must become exit class 8, not an unhandled crash.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            outcome = VerbOutcome.InvalidInvocation(DiagnosticCodes.UnexpectedFailure, exception.ToString());
            record.ExitClass = ExitClass.Internal;
            stopwatch.Stop();
            record.DurationMs = stopwatch.ElapsedMilliseconds;
            record.ExitClassName = ExitClass.NameOf(ExitClass.Internal);
            record.DiagnosticCode = DiagnosticCodes.UnexpectedFailure;
            record.FinalResult = "unexpected tool-internal failure: " + exception.Message;
            string crashPath = WriteRecord(context, record);
            errors.WriteLine();
            errors.WriteLine("FAILED [" + DiagnosticCodes.UnexpectedFailure + "] " + record.FinalResult);
            errors.WriteLine("        " + exception);
            errors.WriteLine("result: " + crashPath);
            return ExitClass.Internal;
        }

        stopwatch.Stop();
        record.DurationMs = stopwatch.ElapsedMilliseconds;
        record.ExitClass = outcome.ExitClass;
        record.ExitClassName = ExitClass.NameOf(outcome.ExitClass);
        record.DiagnosticCode = outcome.DiagnosticCode;
        record.FinalResult = outcome.FinalResult;
        if (outcome.OwningWorkPackage is not null)
        {
            record.OwningWorkPackage = outcome.OwningWorkPackage;
        }

        record.Warnings.AddRange(outcome.Warnings);
        record.Artifacts.AddRange(outcome.Artifacts);
        string resultPath = WriteRecord(context, record);

        TextWriter destination = outcome.ExitClass == ExitClass.Success ? console : errors;
        destination.WriteLine();
        destination.WriteLine(
            (outcome.ExitClass == ExitClass.Success ? "OK" : "FAILED")
            + " [" + outcome.DiagnosticCode + "] " + outcome.FinalResult);
        foreach (string warning in outcome.Warnings)
        {
            destination.WriteLine("warning: " + warning);
        }

        destination.WriteLine("verb:    " + descriptor.Name
            + "   exit class " + outcome.ExitClass.ToString(CultureInfo.InvariantCulture)
            + " (" + record.ExitClassName + ")"
            + "   owner " + record.OwningWorkPackage);
        destination.WriteLine("result:  " + resultPath);
        foreach (string artifact in outcome.Artifacts)
        {
            destination.WriteLine("artifact: " + artifact);
        }

        return outcome.ExitClass;
    }

    private static string WriteRecord(VerbContext context, InvocationRecord record)
    {
        string json = ToolsJsonContext.Serialize(record);
        string absolute = Path.Combine(context.ArtifactDirectory, "result.json");
        File.WriteAllText(absolute, json);

        // A stable path a script can read without knowing the invocation id.
        string latest = Path.Combine(
            context.Layout.ArtifactsDirectory,
            "verbs",
            record.Verb,
            "latest-result.json");
        File.WriteAllText(latest, json);

        return context.Layout.Relative(absolute);
    }

    private static void RecordSourceRevision(VerbContext context)
    {
        CommandResult revision = context.Runner.Run(
            "source-revision",
            "git",
            new[] { "rev-parse", "HEAD" },
            context.Layout.Root,
            TimeSpan.FromMinutes(1),
            quiet: true);
        if (revision.Succeeded)
        {
            context.Record.SourceRevision = revision.Output.Trim();
        }

        CommandResult status = context.Runner.Run(
            "source-status",
            "git",
            new[] { "status", "--porcelain" },
            context.Layout.Root,
            TimeSpan.FromMinutes(1),
            quiet: true);
        context.Record.SourceTreeDirty = status.Succeeded && status.Output.Trim().Length > 0;
    }
}
