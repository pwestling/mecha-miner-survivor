namespace MechaMiner.Tools.Toolchain;

/// <summary>How a probed tool relates to its pin.</summary>
internal enum ToolStatus
{
    /// <summary>Present and matching its pin.</summary>
    Ok,

    /// <summary>Not required yet; its owning work package has not landed.</summary>
    Deferred,

    /// <summary>Present and matching, with something worth reporting.</summary>
    Warning,

    /// <summary>Absent, or present with a version or hash that does not match its pin.</summary>
    Mismatched,
}

/// <summary>
/// One line of the toolchain report: what was pinned, what was found, and whether
/// that is acceptable.
/// </summary>
/// <remarks>
/// Doctor reads only. Doc 100 § Standard command surface requires it to "verify
/// exact Godot/.NET/Blender/tool/template availability and hashes without mutating
/// global state", so nothing in this type installs, downloads, or writes outside
/// <c>artifacts/</c>.
/// </remarks>
internal sealed class ToolProbe
{
    internal ToolProbe(
        string tool,
        ToolStatus status,
        string expected,
        string observed,
        string detail,
        string requiredBy)
    {
        Tool = tool;
        Status = status;
        Expected = expected;
        Observed = observed;
        Detail = detail;
        RequiredBy = requiredBy;
    }

    /// <summary>The tool name.</summary>
    internal string Tool { get; }

    /// <summary>The probe outcome.</summary>
    internal ToolStatus Status { get; }

    /// <summary>The pinned expectation.</summary>
    internal string Expected { get; }

    /// <summary>What was actually observed.</summary>
    internal string Observed { get; }

    /// <summary>A concise explanation, including how to repair a mismatch.</summary>
    internal string Detail { get; }

    /// <summary>The work package that requires the tool.</summary>
    internal string RequiredBy { get; }

    /// <summary>Whether this probe forces exit class 3.</summary>
    internal bool IsBlocking => Status == ToolStatus.Mismatched;

    /// <summary>Renders one fixed-width report row.</summary>
    internal string ToReportLine()
    {
        string marker = Status switch
        {
            ToolStatus.Ok => "ok      ",
            ToolStatus.Deferred => "deferred",
            ToolStatus.Warning => "warn    ",
            _ => "MISMATCH",
        };

        return marker + "  " + Tool.PadRight(22) + "  expected " + Expected
            + "  |  observed " + Observed + "  |  " + Detail;
    }
}
