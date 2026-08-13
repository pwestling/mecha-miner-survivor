using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>Where a drained log record is written.</summary>
/// <remarks>
/// An interface with exactly one method, because a sink is the one place diagnostics touch
/// I/O and the tick-safety design depends on that being a single, replaceable seam. A sink
/// returns <c>false</c> rather than throwing on a failure it handled: doc 114 § C# and domain
/// defaults reserves exceptions for violated invariants, and <c>CTR-OBS-001</c> requires that
/// diagnostics "never block or change authority".
/// </remarks>
internal interface ILogSink
{
    /// <summary>Writes one already-rendered line. Returns whether the write succeeded.</summary>
    bool TryWriteLine(string line);
}

/// <summary>A sink that keeps lines in memory, for tests and for the crash ring buffer.</summary>
internal sealed class MemoryLogSink : ILogSink
{
    private readonly List<string> _lines = new();

    /// <summary>Every line written, in order.</summary>
    internal IReadOnlyList<string> Lines => _lines;

    /// <inheritdoc/>
    public bool TryWriteLine(string line)
    {
        _lines.Add(line);
        return true;
    }
}

/// <summary>
/// A size-bounded, retention-bounded local log file.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-007</c> (<c>TASK-FND-007-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Structured
/// logging: "Logs rotate at 4 MiB and retain the five newest files. ... Cleanup uses
/// validated owned-directory entries and never follows links or deletes a user-exported
/// destination", and <c>docs/technical/70-persistence-and-platform-services.md</c> § Local
/// file layout and encoding. Requirements: <c>TR-PST-006</c>, <c>TR-OBS-002</c>.
/// </para>
/// <para>
/// The directory is passed in. Nothing here derives a location from a username, an
/// environment variable, or the current working directory, which is the rule doc 70 states
/// for every owned local artifact.
/// </para>
/// <para>
/// Rotation renames rather than truncating, so a reader holding the previous file keeps a
/// complete document. Retention deletes only entries that match the owned name pattern in
/// the owned directory and are ordinary files; a symbolic link or reparse point is left
/// alone and reported, because following one is how a cleanup routine deletes something a
/// user exported.
/// </para>
/// </remarks>
internal sealed class RotatingLogFile : ILogSink
{
    /// <summary>The rotation threshold doc 90 fixes.</summary>
    internal const long RotateAtBytes = 4L * 1024 * 1024;

    /// <summary>How many files doc 90 retains, including the active one.</summary>
    internal const int RetainedFiles = 5;

    private readonly string _directory;
    private readonly string _baseName;
    private readonly long _rotateAtBytes;
    private readonly int _retainedFiles;
    private readonly List<string> _notices = new();
    private long _bytesWritten;

    /// <summary>Opens or creates the active log file in an owned directory.</summary>
    internal RotatingLogFile(
        string directory,
        string baseName = "mechaminer.log",
        long rotateAtBytes = RotateAtBytes,
        int retainedFiles = RetainedFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        if (rotateAtBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rotateAtBytes), rotateAtBytes, "must be positive");
        }

        if (retainedFiles < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retainedFiles), retainedFiles, "must retain at least one");
        }

        _directory = directory;
        _baseName = baseName;
        _rotateAtBytes = rotateAtBytes;
        _retainedFiles = retainedFiles;

        Directory.CreateDirectory(_directory);
        ActivePath = Path.Combine(_directory, _baseName);
        _bytesWritten = File.Exists(ActivePath) ? new FileInfo(ActivePath).Length : 0;
    }

    /// <summary>The active file every new record is appended to.</summary>
    internal string ActivePath { get; }

    /// <summary>How many times this instance rotated.</summary>
    internal int Rotations { get; private set; }

    /// <summary>How many aged files retention deleted.</summary>
    internal int Deletions { get; private set; }

    /// <summary>How many writes failed and were dropped rather than propagated.</summary>
    internal int FailedWrites { get; private set; }

    /// <summary>
    /// Human-readable notices about entries retention refused to touch, so a refusal is
    /// visible rather than silent.
    /// </summary>
    internal IReadOnlyList<string> Notices => _notices;

    /// <inheritdoc/>
    public bool TryWriteLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        long lineBytes = Encoding.UTF8.GetByteCount(line) + 1;
        if (_bytesWritten > 0 && _bytesWritten + lineBytes > _rotateAtBytes)
        {
            Rotate();
        }

        try
        {
            File.AppendAllText(ActivePath, line + "\n", Encoding.UTF8);
            _bytesWritten += lineBytes;
            return true;
        }
        catch (IOException exception)
        {
            // A full disk, a removed directory, or a locked file. The record is dropped and
            // counted; diagnostics degrade instead of taking the caller down with them.
            FailedWrites++;
            _notices.Add("write failed: " + exception.GetType().Name);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            FailedWrites++;
            _notices.Add("write failed: " + exception.GetType().Name);
            return false;
        }
    }

    /// <summary>The retained files, newest generation first, that currently exist.</summary>
    internal IReadOnlyList<string> ExistingFiles()
    {
        List<string> found = new();
        if (File.Exists(ActivePath))
        {
            found.Add(ActivePath);
        }

        for (int generation = 1; generation < _retainedFiles + 4; generation++)
        {
            string path = GenerationPath(generation);
            if (File.Exists(path))
            {
                found.Add(path);
            }
        }

        return found;
    }

    private void Rotate()
    {
        // Delete the oldest generation that retention no longer keeps, then shift each
        // remaining generation down by one, then move the active file into generation 1.
        for (int generation = _retainedFiles - 1; generation >= 1; generation--)
        {
            string source = GenerationPath(generation);
            if (!File.Exists(source))
            {
                continue;
            }

            string destination = GenerationPath(generation + 1);
            if (generation + 1 >= _retainedFiles)
            {
                TryDeleteOwned(source);
                continue;
            }

            TryDeleteOwned(destination);
            try
            {
                File.Move(source, destination, overwrite: true);
            }
            catch (IOException exception)
            {
                _notices.Add("rotation could not move " + Path.GetFileName(source) + ": " + exception.GetType().Name);
            }
        }

        try
        {
            File.Move(ActivePath, GenerationPath(1), overwrite: true);
            Rotations++;
        }
        catch (IOException exception)
        {
            _notices.Add("rotation could not move the active file: " + exception.GetType().Name);
        }

        _bytesWritten = 0;
    }

    /// <summary>
    /// Deletes one file only when it is an ordinary file with the owned name pattern inside
    /// the owned directory.
    /// </summary>
    /// <remarks>
    /// Doc 90: cleanup "never follows links or deletes a user-exported destination". A
    /// symbolic link or reparse point is therefore refused and recorded rather than removed,
    /// because deleting through one is exactly how a cleanup routine reaches a destination it
    /// does not own.
    /// </remarks>
    private void TryDeleteOwned(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        string? parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.Equals(parent, Path.GetFullPath(_directory), StringComparison.Ordinal))
        {
            _notices.Add("refused to delete an entry outside the owned directory: " + Path.GetFileName(path));
            return;
        }

        string name = Path.GetFileName(path);
        if (!name.StartsWith(_baseName, StringComparison.Ordinal))
        {
            _notices.Add("refused to delete an entry that does not match the owned name pattern: " + name);
            return;
        }

        FileInfo info = new(path);
        if (info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            _notices.Add("refused to delete a link rather than following it: " + name);
            return;
        }

        try
        {
            info.Delete();
            Deletions++;
        }
        catch (IOException exception)
        {
            _notices.Add("could not delete " + name + ": " + exception.GetType().Name);
        }
        catch (UnauthorizedAccessException exception)
        {
            _notices.Add("could not delete " + name + ": " + exception.GetType().Name);
        }
    }

    private string GenerationPath(int generation)
    {
        return Path.Combine(
            _directory,
            _baseName + "." + generation.ToString(CultureInfo.InvariantCulture));
    }
}
