using System;
using System.Collections.Generic;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The mapping between the three workflow configuration names of
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Build configurations and the three MSBuild configurations
/// <c>Godot.NET.Sdk</c> defines.
/// </summary>
/// <remarks>
/// <para>
/// Doc 100 names the configurations <c>Debug</c>, <c>Development</c>, and
/// <c>Release</c>. <c>Godot.NET.Sdk/4.7.1</c> declares
/// <c>Debug;ExportDebug;ExportRelease</c> in its own <c>Sdk.props</c> and its
/// export tooling only ever asks for those three names: an export preset's
/// "export with debug" flag selects <c>ExportDebug</c> or <c>ExportRelease</c>, and
/// the editor builds <c>Debug</c>. A fourth MSBuild configuration named
/// <c>Development</c> would therefore be a configuration no Godot export can ever
/// produce.
/// </para>
/// <para>
/// The resolution is a 1:1 mapping rather than a fourth configuration or a
/// dropped one. The workflow vocabulary stays exactly doc 100's three names; the
/// MSBuild configuration identity is Godot's. Doc 100 § Build configurations was
/// corrected in the same task to state both columns, as
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c>
/// § Specification maintenance autonomy requires when "the documented contract
/// cannot be implemented as written".
/// </para>
/// </remarks>
internal sealed class WorkflowConfiguration
{
    private WorkflowConfiguration(string workflowName, string msbuildName, string intent, bool optimized)
    {
        WorkflowName = workflowName;
        MsbuildName = msbuildName;
        Intent = intent;
        Optimized = optimized;
    }

    /// <summary>Local correctness development: low optimization, assertions, full logs.</summary>
    internal static WorkflowConfiguration Debug { get; } = new(
        "debug",
        "Debug",
        "local correctness development",
        optimized: false);

    /// <summary>Internal demo, balance, and performance diagnosis: optimized with diagnostics.</summary>
    internal static WorkflowConfiguration Development { get; } = new(
        "development",
        "ExportDebug",
        "internal demo, balance, performance diagnosis",
        optimized: true);

    /// <summary>External shipping candidate: optimized, bounded sanitized logs, no debug actions.</summary>
    internal static WorkflowConfiguration Release { get; } = new(
        "release",
        "ExportRelease",
        "external shipping candidate",
        optimized: true);

    /// <summary>The doc 100 workflow name, which is the wrapper argument value.</summary>
    internal string WorkflowName { get; }

    /// <summary>The MSBuild and Godot configuration identity.</summary>
    internal string MsbuildName { get; }

    /// <summary>The intended use, quoted from doc 100's configuration table.</summary>
    internal string Intent { get; }

    /// <summary>Whether the configuration compiles optimized code.</summary>
    internal bool Optimized { get; }

    /// <summary>The accepted workflow names in doc 100's table order.</summary>
    internal static IReadOnlyList<string> WorkflowNames { get; } = new[] { "debug", "development", "release" };

    /// <summary>All three configurations in doc 100's table order.</summary>
    internal static IReadOnlyList<WorkflowConfiguration> All { get; } = new[] { Debug, Development, Release };

    /// <summary>Resolves a workflow name, which the argument parser has already validated.</summary>
    internal static WorkflowConfiguration FromWorkflowName(string workflowName)
    {
        foreach (WorkflowConfiguration configuration in All)
        {
            if (string.Equals(configuration.WorkflowName, workflowName, StringComparison.Ordinal))
            {
                return configuration;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(workflowName),
            workflowName,
            "not one of the three accepted workflow configuration names");
    }
}
