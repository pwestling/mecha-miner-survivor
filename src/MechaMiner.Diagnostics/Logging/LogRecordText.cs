namespace MechaMiner.Diagnostics.Logging;

/// <summary>Renders a log record as the one line that is written to a sink.</summary>
/// <remarks>
/// <para>
/// One record is one line of JSON. A rotating log file is therefore line-delimited: a crash
/// that truncates the tail costs the last record rather than making the whole file
/// unparseable, and rotation can count bytes without understanding the payload.
/// </para>
/// <para>
/// The line is canonical. Field order is declaration order, structured fields keep the order
/// the caller supplied, and no value contains a control character because
/// <see cref="Redaction"/> flattens them. Two runs with the same inputs therefore produce
/// byte-identical lines, which is what makes a log a reviewable artifact under doc 91
/// § Determinism and fixture policy.
/// </para>
/// </remarks>
internal static class LogRecordText
{
    /// <summary>Renders one record as a single canonical JSON line, without a trailing newline.</summary>
    internal static string Render(LogRecord record)
    {
        return DiagnosticsJsonContext.SerializeLine(record);
    }
}
