namespace MechaMiner.Tools.Cli;

/// <summary>
/// The stable process exit classes of
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface.
/// </summary>
/// <remarks>
/// <para>
/// The set is closed. There is deliberately no <c>1</c>: doc 100 lists exactly
/// eight classes and omits it, so a wrapper that returns 1 has leaked an
/// unclassified failure from an underlying tool. Both root wrappers translate any
/// launcher failure into one of these classes before returning.
/// </para>
/// <para>
/// Wrappers preserve the owning tool's class rather than returning success after
/// partial work. Finer distinctions live in the stable diagnostic code of the
/// structured result document, not in a new exit class.
/// </para>
/// </remarks>
internal static class ExitClass
{
    /// <summary>The verb completed and every step it owns succeeded.</summary>
    internal const int Success = 0;

    /// <summary>Invalid verb or arguments, including a verb whose owning work package has not landed.</summary>
    internal const int InvalidInvocation = 2;

    /// <summary>A pinned tool, template, or version is missing or does not match its pin.</summary>
    internal const int Environment = 3;

    /// <summary>A validation, content, or test gate failed.</summary>
    internal const int Validation = 4;

    /// <summary>A build, import, export, or package step failed.</summary>
    internal const int Build = 5;

    /// <summary>A performance or budget gate failed.</summary>
    internal const int Budget = 6;

    /// <summary>An authorization, credential, or external-state action is required.</summary>
    internal const int Authorization = 7;

    /// <summary>An unexpected tool-internal failure; the verb could not reach a classified outcome.</summary>
    internal const int Internal = 8;

    /// <summary>Returns the stable short name doc 100 gives <paramref name="exitClass"/>.</summary>
    internal static string NameOf(int exitClass)
    {
        return exitClass switch
        {
            Success => "success",
            InvalidInvocation => "invalid-invocation",
            Environment => "environment",
            Validation => "validation",
            Build => "build",
            Budget => "budget",
            Authorization => "authorization",
            Internal => "internal",
            _ => "unclassified",
        };
    }

    /// <summary>
    /// Maps an exit code observed from an external process onto the class the
    /// caller declared it means, so an underlying tool's <c>1</c> can never
    /// escape as a wrapper exit code.
    /// </summary>
    internal static int FromProcess(int processExitCode, int failureClass)
    {
        return processExitCode == 0 ? Success : failureClass;
    }
}
