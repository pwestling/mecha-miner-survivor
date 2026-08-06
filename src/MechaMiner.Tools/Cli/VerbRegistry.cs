using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MechaMiner.Tools.Verbs;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The explicit registration table for the standard command surface.
/// </summary>
/// <remarks>
/// <para>
/// Every verb of
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface is registered here, in that document's table order,
/// with its required effect quoted. Registration is a literal array: doc 100
/// § C# project standards requires that "generated/explicit registries make missing
/// behavior a build error", and doc 114 forbids reflection-based registration.
/// </para>
/// <para>
/// A verb whose behavior belongs to a work package that has not landed is
/// registered with its argument contract and its owner, and returns a typed nonzero
/// status. That is FND-002's completion gate: "implemented verbs run
/// noninteractively and unavailable owner verbs return a typed nonzero status until
/// their package lands".
/// </para>
/// </remarks>
internal static class VerbRegistry
{
    private static readonly ImmutableArray<VerbDescriptor> Verbs = ImmutableArray.Create(
        VerbDescriptor.Implemented(
            "doctor",
            "verify exact Godot/.NET/Blender/tool/template availability and hashes without mutating global state",
            "FND-002",
            DoctorVerb.Execute),
        VerbDescriptor.Implemented(
            "bootstrap",
            "restore/download allowed repository-local tools, then run doctor",
            "FND-002",
            BootstrapVerb.Execute),
        VerbDescriptor.Implemented(
            "format",
            "format owned text/code and fail if the resulting tree still violates policy",
            "FND-002",
            FormatVerb.Format),
        VerbDescriptor.Implemented(
            "format-check",
            "validate formatting without writes",
            "FND-002",
            FormatVerb.Check),
        VerbDescriptor.Implemented(
            "build",
            "locked restore, analyzers, warnings-as-errors compilation",
            "FND-002",
            BuildVerb.Execute,
            VerbArgument.OptionalChoice("configuration", "debug", "debug", "development", "release")),
        VerbDescriptor.Implemented(
            "test-fast",
            "pure bounded tests, content validation, representative headless fixtures",
            "FND-003",
            TestVerb.RunFastTier),
        VerbDescriptor.AwaitingOwner(
            "test-main",
            "fast suite plus Godot integration, package smoke prerequisites, broader matrices",
            "FND-003"),
        VerbDescriptor.AwaitingOwner(
            "test-nightly",
            "exhaustive seeds, full runs, soak/fuzz/screenshot/performance trend suites",
            "OPS-001"),
        VerbDescriptor.AwaitingOwner(
            "content",
            "compile/validate canonical content and emit generated reports/hash",
            "DAT-006"),
        VerbDescriptor.Implemented(
            "godot-import",
            "clean headless import/check with captured warnings",
            "FND-002",
            GodotImportVerb.Execute),
        VerbDescriptor.AwaitingOwner(
            "run",
            "launch the normal local development build",
            "FND-006"),
        VerbDescriptor.AwaitingOwner(
            "scenario",
            "run a named deterministic development scenario, including M2/M3/M4/PERF/WB IDs",
            "SIM-009",
            VerbArgument.Positional("id")),
        VerbDescriptor.AwaitingOwner(
            "map",
            "generate, validate, visualize, and report one reproducible map",
            "MAP-009",
            VerbArgument.RequiredOption("seed")),
        VerbDescriptor.AwaitingOwner(
            "map-batch",
            "run the named seed/profile/region audit partition",
            "MAP-010",
            VerbArgument.Positional("partition")),
        VerbDescriptor.AwaitingOwner(
            "benchmark",
            "run a named WB/PERF scenario and emit its canonical report",
            "QUA-005",
            VerbArgument.Positional("id")),
        VerbDescriptor.AwaitingOwner(
            "export",
            "headless import and named Godot export preset",
            "FND-006",
            VerbArgument.PositionalChoice("platform", "windows", "linux"),
            VerbArgument.PositionalChoice("configuration", "development", "release")),
        VerbDescriptor.AwaitingOwner(
            "package-demo",
            "build and validate the Steam-independent M4 internal-demo artifacts",
            "OPS-002"),
        VerbDescriptor.AwaitingOwner(
            "release-validate",
            "run release gates and generate manifest/checksums/notices/SBOM without publishing",
            "OPS-002"));

    /// <summary>Every registered verb, in doc 100's table order.</summary>
    internal static IReadOnlyList<VerbDescriptor> All => Verbs;

    /// <summary>Finds a verb by exact name, or null when the name is not registered.</summary>
    internal static VerbDescriptor? Find(string name)
    {
        foreach (VerbDescriptor verb in Verbs)
        {
            if (string.Equals(verb.Name, name, StringComparison.Ordinal))
            {
                return verb;
            }
        }

        return null;
    }
}
