using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Diagnostics.Logging;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Diagnostics.Tests.Logging;

/// <summary>
/// Rotation, retention, and cleanup safety for the bounded local log file.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-007</c> (<c>TASK-FND-007-001</c>). Verification:
/// <c>VER-FND-007-006</c>, <c>VER-FND-007-007</c>, <c>VER-FND-007-008</c>.
/// Requirements: <c>TR-OBS-002</c>, <c>TR-PST-006</c>.
/// </para>
/// <para>
/// The thresholds under test are a small multiple of a line rather than doc 90's literal
/// 4 MiB, because writing 20 MiB per assertion would make the fast tier slow without
/// testing anything the small bound does not. The production constants are asserted
/// separately against the document's numbers, so both the policy and the mechanism are
/// covered.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RotatingLogFileTests
{
    private string _directory = string.Empty;

    /// <summary>Creates an isolated owned directory per test.</summary>
    [SetUp]
    public void CreateDirectory()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "mechaminer-logs-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Removes the owned directory.</summary>
    [TearDown]
    public void DeleteDirectory()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>The production constants are the numbers doc 90 fixes.</summary>
    [Test]
    public void TheRotationAndRetentionConstantsAreTheOnesTheDocumentFixes()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                RotatingLogFile.RotateAtBytes,
                Is.EqualTo(4L * 1024 * 1024),
                "doc 90 § Structured logging: 'Logs rotate at 4 MiB'");
            Assert.That(
                RotatingLogFile.RetainedFiles,
                Is.EqualTo(5),
                "doc 90 § Structured logging: 'retain the five newest files'");
        });
    }

    /// <summary>Crossing the size bound rotates rather than truncating, and no line is lost.</summary>
    [Test]
    public void CrossingTheSizeBoundRotatesWithoutLosingALine()
    {
        RotatingLogFile file = new(_directory, "test.log", rotateAtBytes: 64, retainedFiles: 5);

        for (int index = 0; index < 10; index++)
        {
            Assert.That(
                file.TryWriteLine("line-" + index.ToString(CultureInfo.InvariantCulture) + "-padding-padding"),
                Is.True);
        }

        List<string> everyLine = new();
        foreach (string path in file.ExistingFiles())
        {
            everyLine.AddRange(File.ReadAllLines(path));
        }

        Expect.Multiple(() =>
        {
            Assert.That(file.Rotations, Is.GreaterThan(0), "the bound must actually have been crossed");
            Assert.That(everyLine, Has.Count.EqualTo(10), "rotation renames; it never truncates");
            Assert.That(file.Notices, Is.Empty);
        });
    }

    /// <summary>Retention keeps exactly the declared number of files and no more.</summary>
    [Test]
    public void RetentionKeepsExactlyTheDeclaredNumberOfFiles()
    {
        RotatingLogFile file = new(_directory, "test.log", rotateAtBytes: 48, retainedFiles: 3);

        for (int index = 0; index < 40; index++)
        {
            file.TryWriteLine("line-" + index.ToString(CultureInfo.InvariantCulture) + "-padding-padding");
        }

        string[] onDisk = Directory.GetFiles(_directory);

        Expect.Multiple(() =>
        {
            Assert.That(onDisk, Has.Length.EqualTo(3), "retaining three means three files exist, not three plus history");
            Assert.That(file.Deletions, Is.GreaterThan(0), "aged generations were deleted, not left behind");
        });
    }

    /// <summary>
    /// Cleanup refuses to delete a symbolic link, so it cannot reach a user-exported
    /// destination by following one.
    /// </summary>
    /// <remarks>
    /// Doc 90 § Structured logging: cleanup "never follows links or deletes a user-exported
    /// destination". The control is a link placed at a generation path that rotation would
    /// otherwise remove, pointing at a file outside the owned directory that must survive.
    /// </remarks>
    [Test]
    public void CleanupRefusesToDeleteALinkRatherThanFollowingIt()
    {
        string exportedDirectory = Path.Combine(
            Path.GetTempPath(),
            "mechaminer-exported-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(exportedDirectory);
        string exported = Path.Combine(exportedDirectory, "user-exported.log");
        File.WriteAllText(exported, "a destination the user exported and owns\n");

        try
        {
            // Retaining three files means the active file plus generations 1 and 2, so
            // generation 2 is exactly the entry retention deletes on the next rotation.
            RotatingLogFile file = new(_directory, "test.log", rotateAtBytes: 48, retainedFiles: 3);
            string linkPath = Path.Combine(_directory, "test.log.2");
            File.CreateSymbolicLink(linkPath, exported);

            for (int index = 0; index < 24; index++)
            {
                file.TryWriteLine("line-" + index.ToString(CultureInfo.InvariantCulture) + "-padding-padding");
            }

            Expect.Multiple(() =>
            {
                Assert.That(
                    File.Exists(exported),
                    Is.True,
                    "the user-exported destination must survive log cleanup");
                Assert.That(
                    File.ReadAllText(exported),
                    Is.EqualTo("a destination the user exported and owns\n"),
                    "and must not be truncated through the link either");
                Assert.That(
                    string.Join("; ", file.Notices),
                    Does.Contain("refused to delete a link"),
                    "the refusal is recorded rather than silent");
            });
        }
        finally
        {
            if (Directory.Exists(exportedDirectory))
            {
                Directory.Delete(exportedDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Cleanup refuses an entry that does not match the owned name pattern, so an unrelated
    /// file that happens to sit in the directory is never removed.
    /// </summary>
    [Test]
    public void CleanupRefusesAnEntryOutsideTheOwnedNamePattern()
    {
        string unrelated = Path.Combine(_directory, "unrelated-notes.txt");
        File.WriteAllText(unrelated, "not a log file\n");

        RotatingLogFile file = new(_directory, "test.log", rotateAtBytes: 48, retainedFiles: 2);
        for (int index = 0; index < 12; index++)
        {
            file.TryWriteLine("line-" + index.ToString(CultureInfo.InvariantCulture) + "-padding-padding");
        }

        Assert.That(File.Exists(unrelated), Is.True, "only the owned name pattern is subject to retention");
    }

    /// <summary>
    /// A write into a directory that has been removed is reported as a failed write rather
    /// than thrown, so a diagnostic failure cannot take its caller down.
    /// </summary>
    [Test]
    public void AWriteIntoARemovedDirectoryFailsWithoutThrowing()
    {
        RotatingLogFile file = new(_directory, "test.log");
        Assert.That(file.TryWriteLine("first"), Is.True);

        Directory.Delete(_directory, recursive: true);

        bool succeeded = true;
        Expect.DoesNotThrow(() => succeeded = file.TryWriteLine("second"));

        Expect.Multiple(() =>
        {
            Assert.That(succeeded, Is.False, "the failure is reported as a result, not raised");
            Assert.That(file.FailedWrites, Is.EqualTo(1));
            Assert.That(string.Join("; ", file.Notices), Does.Contain("write failed"));
        });
    }

    /// <summary>
    /// The log and the file cooperate end to end: records written through the log land in the
    /// rotating file as parseable lines, and a sink failure is counted on both sides.
    /// </summary>
    [Test]
    public void RecordsWrittenThroughTheLogLandInTheRotatingFileAsParseableLines()
    {
        RotatingLogFile file = new(_directory, "test.log", rotateAtBytes: 4096, retainedFiles: 5);
        long ticks = 0;
        DiagnosticLog log = new(
            file,
            Redaction.For("/home/nobody", "/home/nobody/.local/share/MechaMiner", "nobody"),
            () => DateTimeOffset.UnixEpoch.AddMilliseconds(ticks),
            () => ticks);

        log.Write(DiagnosticCatalog.BuildIdentityVerified, "identity verified");
        log.Write(DiagnosticCatalog.LoggingStarted, "logging opened");
        Assert.That(log.Drain(), Is.EqualTo(2));

        string[] lines = File.ReadAllLines(file.ActivePath);
        List<string> codes = new();
        foreach (string line in lines)
        {
            codes.Add(MechaMiner.Diagnostics.DiagnosticsJsonContext.DeserializeLogLine(line).Code);
        }

        Expect.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(2));
            Assert.That(
                codes,
                Is.EqualTo(new[] { DiagnosticCatalog.BuildIdentityVerified, DiagnosticCatalog.LoggingStarted }));
            Assert.That(log.Dropped, Is.Zero);
        });
    }
}
