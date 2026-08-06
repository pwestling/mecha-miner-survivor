using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MechaMiner.Tools.Cli;

/// <summary>The captured result of one external process invocation.</summary>
internal sealed class CommandResult
{
    internal CommandResult(string commandLine, int exitCode, string output, long durationMs, bool timedOut)
    {
        CommandLine = commandLine;
        ExitCode = exitCode;
        Output = output;
        DurationMs = durationMs;
        TimedOut = timedOut;
    }

    /// <summary>The exact command line that was executed.</summary>
    internal string CommandLine { get; }

    /// <summary>The raw process exit code. <c>-1</c> when the process was killed on timeout.</summary>
    internal int ExitCode { get; }

    /// <summary>Interleaved standard output and standard error.</summary>
    internal string Output { get; }

    /// <summary>Wall-clock duration in milliseconds.</summary>
    internal long DurationMs { get; }

    /// <summary>Whether the bounded timeout elapsed and the process was terminated.</summary>
    internal bool TimedOut { get; }

    /// <summary>Whether the process exited with code zero and did not time out.</summary>
    internal bool Succeeded => !TimedOut && ExitCode == 0;
}

/// <summary>
/// Runs pinned external tools noninteractively and captures their output.
/// </summary>
/// <remarks>
/// <para>
/// Every invocation redirects standard input from an empty stream, so nothing can
/// wait for a console answer: doc 100 § Standard command surface requires every
/// verb to be noninteractive.
/// </para>
/// <para>
/// Every invocation carries a bounded timeout. Doc 91 § Flake policy requires
/// bounded timeouts and explicit completion signals rather than wall-clock sleeps.
/// </para>
/// </remarks>
internal sealed class CommandRunner
{
    private readonly RepositoryLayout _layout;
    private readonly List<StepRecord> _steps;
    private readonly string _logDirectory;
    private readonly TextWriter _console;

    internal CommandRunner(RepositoryLayout layout, List<StepRecord> steps, string logDirectory, TextWriter console)
    {
        _layout = layout;
        _steps = steps;
        _logDirectory = logDirectory;
        _console = console;
    }

    /// <summary>
    /// Runs <paramref name="fileName"/> with <paramref name="arguments"/>, records a
    /// step, writes the captured output to a log file, and returns the result.
    /// </summary>
    internal CommandResult Run(
        string stepName,
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        IReadOnlyDictionary<string, string>? environment = null,
        bool quiet = false)
    {
        string directory = workingDirectory ?? _layout.Root;
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(20);

        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Noninteractive and locale-stable for every child tool.
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        startInfo.Environment["TERM"] = "dumb";
        startInfo.Environment["NO_COLOR"] = "1";
        if (environment is not null)
        {
            foreach (KeyValuePair<string, string> entry in environment)
            {
                startInfo.Environment[entry.Key] = entry.Value;
            }
        }

        string commandLine = FormatCommandLine(fileName, arguments);
        StringBuilder captured = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool timedOut = false;
        int exitCode;

        using (Process process = new() { StartInfo = startInfo })
        {
            process.OutputDataReceived += (_, e) => AppendLine(captured, e.Data);
            process.ErrorDataReceived += (_, e) => AppendLine(captured, e.Data);

            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                stopwatch.Stop();
                CommandResult missing = new(
                    commandLine,
                    exitCode: -1,
                    output: "could not start '" + fileName + "': " + exception.Message,
                    durationMs: stopwatch.ElapsedMilliseconds,
                    timedOut: false);
                RecordStep(stepName, missing, directory, quiet);
                return missing;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.Close();

            if (!process.WaitForExit((int)effectiveTimeout.TotalMilliseconds))
            {
                timedOut = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the timeout and the kill; nothing to do.
                }

                process.WaitForExit();
            }
            else
            {
                // Flush the asynchronous readers.
                process.WaitForExit();
            }

            exitCode = timedOut ? -1 : process.ExitCode;
        }

        stopwatch.Stop();
        CommandResult result = new(commandLine, exitCode, captured.ToString(), stopwatch.ElapsedMilliseconds, timedOut);
        RecordStep(stepName, result, directory, quiet);
        return result;
    }

    /// <summary>Records an in-process assertion as a step, so structured output is complete.</summary>
    internal void RecordAssertion(string stepName, bool succeeded, string detail, bool quiet = false)
    {
        StepRecord step = new()
        {
            Name = stepName,
            Command = string.Empty,
            WorkingDirectory = ".",
            ProcessExitCode = succeeded ? 0 : -1,
            DurationMs = 0,
            Succeeded = succeeded,
            Detail = detail,
        };
        step.Assertions.Add(detail);
        _steps.Add(step);
        if (!quiet)
        {
            _console.WriteLine((succeeded ? "ok    " : "FAIL  ") + stepName + ": " + detail);
        }
    }

    private static void AppendLine(StringBuilder target, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (target)
        {
            target.Append(line).Append('\n');
        }
    }

    private static string FormatCommandLine(string fileName, IReadOnlyList<string> arguments)
    {
        StringBuilder builder = new(fileName);
        foreach (string argument in arguments)
        {
            builder.Append(' ');
            builder.Append(argument.Contains(' ', StringComparison.Ordinal) ? "\"" + argument + "\"" : argument);
        }

        return builder.ToString();
    }

    private void RecordStep(string stepName, CommandResult result, string workingDirectory, bool quiet)
    {
        Directory.CreateDirectory(_logDirectory);
        string logFile = Path.Combine(_logDirectory, SanitizeStepName(stepName) + ".log");
        File.WriteAllText(logFile, result.Output);

        StepRecord step = new()
        {
            Name = stepName,
            Command = result.CommandLine,
            WorkingDirectory = _layout.Relative(workingDirectory),
            ProcessExitCode = result.ExitCode,
            DurationMs = result.DurationMs,
            Succeeded = result.Succeeded,
            LogPath = _layout.Relative(logFile),
            Detail = result.TimedOut
                ? "timed out after " + result.DurationMs.ToString(CultureInfo.InvariantCulture) + " ms"
                : "exit " + result.ExitCode.ToString(CultureInfo.InvariantCulture) + " in "
                    + result.DurationMs.ToString(CultureInfo.InvariantCulture) + " ms",
        };
        _steps.Add(step);

        if (!quiet)
        {
            _console.WriteLine((step.Succeeded ? "ok    " : "FAIL  ") + stepName + ": " + step.Detail);
        }
    }

    private static string SanitizeStepName(string stepName)
    {
        StringBuilder builder = new(stepName.Length);
        foreach (char character in stepName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString();
    }
}
