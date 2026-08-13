using System.Collections.Generic;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// One recorded step of a verb: an external command, or an assertion the verb made
/// about a captured report.
/// </summary>
/// <remarks>
/// Doc 114 § Required evidence bundle requires "exact command invocations and exit
/// results". A step therefore keeps the command exactly as it was executed rather
/// than a prose summary.
/// </remarks>
internal sealed class StepRecord
{
    /// <summary>A short stable step name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The exact command line, or an empty string for an in-process assertion.</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>The working directory the command ran in, repository-relative.</summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>The raw process exit code, before it was mapped onto an exit class.</summary>
    public int ProcessExitCode { get; set; }

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Whether the step met its own expectation.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Repository-relative path of the captured output log, when one was written.</summary>
    public string? LogPath { get; set; }

    /// <summary>Assertions this step made about a captured report rather than about an exit code.</summary>
    public List<string> Assertions { get; set; } = new();

    /// <summary>A concise human-readable detail line.</summary>
    public string Detail { get; set; } = string.Empty;
}
