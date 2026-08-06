using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MechaMiner.Game.Tests;

/// <summary>Host-side view of one assertion an engine case made.</summary>
internal sealed class EngineRunnerAssertion
{
    /// <summary>Stable assertion name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether it held.</summary>
    public bool Passed { get; set; }

    /// <summary>What was expected and what was observed.</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>Host-side view of the engine identity the runner observed.</summary>
internal sealed class EngineRunnerIdentity
{
    /// <summary>The engine version string.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The configured rendering method.</summary>
    public string RenderingMethod { get; set; } = string.Empty;

    /// <summary>The rendering driver actually in use.</summary>
    public string RenderingDriver { get; set; } = string.Empty;

    /// <summary>Whether the process was headless.</summary>
    public bool Headless { get; set; }
}

/// <summary>
/// Host-side view of the <c>MMG-RUNNER-REPORT</c> document the engine runner writes.
/// </summary>
/// <remarks>
/// This type is deliberately a separate declaration from the runner's own, even though
/// the shapes match. <c>docs/technical/115-component-contract-and-schema-registry.md</c>
/// § Accepted project boundary keeps <c>MechaMiner.Game.Tests</c> free of Godot, so it
/// cannot reference the runner's types; the JSON document is the contract, and a
/// divergence between the two declarations shows up as a deserialization failure rather
/// than as a silent mismatch. Unknown fields are rejected, per doc 40 § JSON codec and
/// schema baseline.
/// </remarks>
internal sealed class EngineRunnerReport
{
    /// <summary>Stable schema identity. Must be <c>MMG-RUNNER-REPORT</c>.</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Version of the document's shape.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>The case that ran.</summary>
    public string Case { get; set; } = string.Empty;

    /// <summary><c>passed</c> or <c>failed</c>.</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>The engine identity observed at run time.</summary>
    public EngineRunnerIdentity Engine { get; set; } = new();

    /// <summary>The canonical <c>SCH-BLD-001</c> build identity line the engine process reported.</summary>
    public string BuildIdentity { get; set; } = string.Empty;

    /// <summary>UTC start time in round-trip format.</summary>
    public string StartedUtc { get; set; } = string.Empty;

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>The exit code the runner asked the engine for.</summary>
    public int RequestedExitCode { get; set; }

    /// <summary>Every assertion the case made, in order.</summary>
    public List<EngineRunnerAssertion> Assertions { get; set; } = new();

    /// <summary>Absolute paths of artifacts the case wrote.</summary>
    public List<string> Artifacts { get; set; } = new();
}

/// <summary>Source-generated JSON metadata for the host-side report view.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(EngineRunnerReport))]
internal sealed partial class EngineRunnerJsonContext : JsonSerializerContext
{
    /// <summary>Reads a report document, rejecting unknown fields.</summary>
    internal static EngineRunnerReport Deserialize(string json)
    {
        return JsonSerializer.Deserialize(json, Default.EngineRunnerReport)
            ?? throw new JsonException("the engine runner report deserialized to null");
    }
}
