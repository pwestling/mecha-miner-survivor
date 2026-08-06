using System;
using System.Collections.Generic;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The classified result of one verb invocation.
/// </summary>
/// <remarks>
/// Expected rejection is typed result data rather than an exception, per
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § C# and
/// domain defaults. Only a violated invariant or unrecoverable infrastructure
/// failure throws, and <see cref="Program"/> converts that into
/// <see cref="ExitClass.Internal"/>.
/// </remarks>
internal sealed class VerbOutcome
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _artifacts = new();

    private VerbOutcome(int exitClass, string diagnosticCode, string finalResult)
    {
        ExitClass = exitClass;
        DiagnosticCode = diagnosticCode;
        FinalResult = finalResult;
    }

    /// <summary>The doc 100 exit class this verb returns.</summary>
    internal int ExitClass { get; }

    /// <summary>The stable diagnostic code recorded in structured output.</summary>
    internal string DiagnosticCode { get; }

    /// <summary>The concise final result line printed to the console.</summary>
    internal string FinalResult { get; }

    /// <summary>The work package that owns an unavailable verb, when applicable.</summary>
    internal string? OwningWorkPackage { get; private init; }

    /// <summary>Warnings that did not change the exit class.</summary>
    internal IReadOnlyList<string> Warnings => _warnings;

    /// <summary>Repository-relative artifact paths this verb produced.</summary>
    internal IReadOnlyList<string> Artifacts => _artifacts;

    /// <summary>The verb reached its required effect.</summary>
    internal static VerbOutcome Success(string finalResult)
    {
        return new VerbOutcome(Cli.ExitClass.Success, DiagnosticCodes.Success, finalResult);
    }

    /// <summary>A pinned tool or version is missing or mismatched.</summary>
    internal static VerbOutcome Environment(string finalResult)
    {
        return new VerbOutcome(Cli.ExitClass.Environment, DiagnosticCodes.EnvironmentMismatch, finalResult);
    }

    /// <summary>A validation, content, or test gate failed.</summary>
    internal static VerbOutcome Validation(string finalResult)
    {
        return new VerbOutcome(Cli.ExitClass.Validation, DiagnosticCodes.ValidationFailed, finalResult);
    }

    /// <summary>A build, import, export, or package step failed.</summary>
    internal static VerbOutcome Build(string finalResult)
    {
        return new VerbOutcome(Cli.ExitClass.Build, DiagnosticCodes.BuildFailed, finalResult);
    }

    /// <summary>The invocation itself was invalid.</summary>
    internal static VerbOutcome InvalidInvocation(string diagnosticCode, string finalResult)
    {
        return new VerbOutcome(Cli.ExitClass.InvalidInvocation, diagnosticCode, finalResult);
    }

    /// <summary>
    /// The verb is registered but the work package owning its behavior has not
    /// landed. This is the typed nonzero status FND-002's completion gate requires.
    /// </summary>
    internal static VerbOutcome AwaitingOwner(string verb, string owningWorkPackage, string requiredEffect)
    {
        string finalResult = string.Concat(
            "verb '",
            verb,
            "' is registered but not implemented in this revision. Owning work package: ",
            owningWorkPackage,
            ". Required effect once ",
            owningWorkPackage,
            " lands: ",
            requiredEffect,
            ".");

        return new VerbOutcome(Cli.ExitClass.InvalidInvocation, DiagnosticCodes.VerbOwnerUnavailable, finalResult)
        {
            OwningWorkPackage = owningWorkPackage,
        };
    }

    /// <summary>Records a warning that does not change the exit class.</summary>
    internal VerbOutcome WithWarning(string warning)
    {
        _warnings.Add(warning);
        return this;
    }

    /// <summary>Records warnings that do not change the exit class.</summary>
    internal VerbOutcome WithWarnings(IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        _warnings.AddRange(warnings);
        return this;
    }

    /// <summary>Records a repository-relative artifact path this verb produced.</summary>
    internal VerbOutcome WithArtifact(string repositoryRelativePath)
    {
        _artifacts.Add(repositoryRelativePath);
        return this;
    }
}
