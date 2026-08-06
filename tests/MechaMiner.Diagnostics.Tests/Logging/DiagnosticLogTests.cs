using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Diagnostics.Identity;
using MechaMiner.Diagnostics.Logging;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Diagnostics.Tests.Logging;

/// <summary>
/// The bounded structured logging contract: schema, redaction, rate limiting, rotation, and
/// failure.
/// </summary>
/// <remarks>
/// Owner: <c>FND-007</c> (<c>TASK-FND-007-001</c>). Verification:
/// <c>VER-FND-007-001</c> through <c>VER-FND-007-009</c>. Requirements:
/// <c>TR-OBS-001</c>, <c>TR-OBS-002</c>, <c>TR-BLD-003</c>, <c>TR-PST-006</c>.
/// </remarks>
[TestFixture]
internal sealed class DiagnosticLogTests
{
    /// <summary>A clock a test drives explicitly, so no assertion depends on wall time.</summary>
    private sealed class TestClock
    {
        internal long Ticks { get; set; }

        internal DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMilliseconds(Ticks);

        internal void Advance(long ticks)
        {
            Ticks += ticks;
        }
    }

    /// <summary>A sink that records whether it was written to before <c>Drain</c> ran.</summary>
    private sealed class TouchRecordingSink : ILogSink
    {
        internal int Writes { get; private set; }

        internal List<string> Lines { get; } = new();

        public bool TryWriteLine(string line)
        {
            Writes++;
            Lines.Add(line);
            return true;
        }
    }

    private static (DiagnosticLog Log, MemoryLogSink Sink, TestClock Clock) NewLog(
        Redaction? redaction = null,
        int capacity = DiagnosticLog.DefaultCapacity,
        long windowTicks = DiagnosticLog.DefaultWindowTicks)
    {
        MemoryLogSink sink = new();
        TestClock clock = new();
        DiagnosticLog log = new(
            sink,
            redaction ?? Redaction.None(),
            () => clock.UtcNow,
            () => clock.Ticks,
            capacity,
            windowTicks);
        return (log, sink, clock);
    }

    /// <summary>
    /// Every field doc 90 § Structured logging requires is present on every record, and the
    /// line is canonical line-delimited JSON that round-trips and rejects unknown fields.
    /// </summary>
    [Test]
    public void EveryRecordCarriesEveryRequiredFieldAndRoundTrips()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog();
        log.Scope.RunId = "run-0001";
        log.Scope.ProfileId = "profile-0001";
        log.Scope.Tick = 1234;

        log.Write(
            DiagnosticCatalog.LoggingStarted,
            "bounded local logging opened",
            new LogField { Name = "capacity", Value = "512" });
        Assert.That(log.Drain(), Is.EqualTo(1));

        string line = sink.Lines[0];
        LogRecord record = DiagnosticsJsonContext.DeserializeLogLine(line);

        Expect.Multiple(() =>
        {
            Assert.That(line, Does.Not.Contain("\n"), "one record is one line");
            Assert.That(record.Schema, Is.EqualTo("MMD-LOG-RECORD"));
            Assert.That(record.SchemaVersion, Is.EqualTo(1));
            Assert.That(
                DateTimeOffset.TryParse(
                    record.TimestampUtc,
                    CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out _),
                Is.True,
                "the timestamp is a round-trip UTC value");
            Assert.That(record.Sequence, Is.EqualTo(1), "the sequence is monotonic from one");
            Assert.That(record.Severity, Is.EqualTo("information"));
            Assert.That(record.Category, Is.EqualTo("bootstrap"));
            Assert.That(record.Code, Is.EqualTo(DiagnosticCatalog.LoggingStarted));
            Assert.That(record.BuildIdentity, Is.EqualTo(BuildIdentity.IdentityLine));
            Assert.That(record.ContentIdentity, Is.Not.Empty);
            Assert.That(record.RunId, Is.EqualTo("run-0001"));
            Assert.That(record.ProfileId, Is.EqualTo("profile-0001"));
            Assert.That(record.Tick, Is.EqualTo(1234));
            Assert.That(record.Message, Is.EqualTo("bounded local logging opened"));
            Assert.That(record.Fields, Has.Count.EqualTo(1));
            Assert.That(record.Fields[0].Name, Is.EqualTo("capacity"));
            Assert.That(LogRecordText.Render(record), Is.EqualTo(line), "rendering is canonical");
        });
    }

    /// <summary>An unknown field in a log line is rejected rather than ignored.</summary>
    [Test]
    public void AnUnknownLogFieldIsRejected()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog();
        log.Write(DiagnosticCatalog.LoggingStarted, "opened");
        log.Drain();

        string tampered = sink.Lines[0].Replace(
            "{\"schema\"",
            "{\"unexpected\":1,\"schema\"",
            StringComparison.Ordinal);

        Expect.Throws<System.Text.Json.JsonException>(
            () => DiagnosticsJsonContext.DeserializeLogLine(tampered));
    }

    /// <summary>The sequence is monotonic and strictly increasing across records.</summary>
    [Test]
    public void TheSequenceIsMonotonicEvenWhenTheTimestampDoesNotAdvance()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog();
        for (int index = 0; index < 5; index++)
        {
            log.Write(DiagnosticCatalog.PersistenceWriteCommitted, "committed");
        }

        log.Drain();

        List<long> sequences = new();
        HashSet<string> timestamps = new(StringComparer.Ordinal);
        foreach (string line in sink.Lines)
        {
            LogRecord record = DiagnosticsJsonContext.DeserializeLogLine(line);
            sequences.Add(record.Sequence);
            timestamps.Add(record.TimestampUtc);
        }

        Expect.Multiple(() =>
        {
            Assert.That(sequences, Is.Ordered.Ascending.And.Unique);
            Assert.That(
                timestamps,
                Has.Count.EqualTo(1),
                "the clock did not advance, so the sequence is the only thing ordering these records");
        });
    }

    /// <summary>
    /// Redaction removes real machine-private values. The fixture uses this machine's own
    /// home directory and account name, so the assertion is that a value that genuinely
    /// identifies this machine is absent from the output.
    /// </summary>
    [Test]
    public void RedactionRemovesMachinePrivateValuesFromEveryPartOfARecord()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            home = Path.GetTempPath();
        }

        string userName = Environment.UserName;
        string userData = Path.Combine(home, ".local", "share", "MechaMiner");
        string steamId = "76561198000000001";

        Redaction redaction = Redaction.For(home, userData, userName);
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog(redaction);

        log.Scope.RunId = "run-for-" + userName;
        log.Scope.ProfileId = Path.Combine(userData, "profile.json");
        log.Write(
            DiagnosticCatalog.PersistenceWriteCommitted,
            "wrote " + Path.Combine(userData, "profile.json") + " for " + userName,
            new LogField { Name = "home", Value = home },
            new LogField { Name = "steam_id", Value = "steamid=" + steamId },
            new LogField { Name = "other_user_home", Value = "/home/someone-else/notes.txt" },
            new LogField { Name = "windows_profile", Value = @"C:\Users\someone-else\AppData\game.log" },
            new LogField
            {
                Name = "uncontrolled_content_text",
                Value = new string('x', Redaction.MaximumFieldLength * 2) + "\nforged: second line",
            });

        log.Drain();
        string output = string.Join("\n", sink.Lines);

        Expect.Multiple(() =>
        {
            Assert.That(output, Does.Not.Contain(userData), "the owned user-data root must not appear");
            Assert.That(output, Does.Not.Contain(home), "the home directory must not appear");
            Assert.That(
                output,
                Does.Not.Contain(userName),
                "the account name must not appear anywhere, including inside the run ID");
            Assert.That(output, Does.Not.Contain(steamId), "a raw Steam identifier must not appear");
            Assert.That(output, Does.Not.Contain("someone-else"), "another user's path must not appear");

            Assert.That(output, Does.Contain(Redaction.UserDataToken));
            Assert.That(output, Does.Contain(Redaction.UserToken));
            Assert.That(output, Does.Contain(Redaction.SteamIdToken));
            Assert.That(output, Does.Contain(Redaction.PathToken));
            Assert.That(output, Does.Contain(Redaction.TruncationToken));

            Assert.That(
                sink.Lines,
                Has.Count.EqualTo(1),
                "uncontrolled text carrying a newline must not forge a second record");
        });

        TestContext.Progress.WriteLine("redacted output: " + output);
    }

    /// <summary>A rate-limited code emits its burst, then one summary count for the window.</summary>
    [Test]
    public void ARateLimitedCodeEmitsItsBurstThenOneSummaryCount()
    {
        (DiagnosticLog log, MemoryLogSink sink, TestClock clock) = NewLog(windowTicks: 1000);
        DiagnosticCode code = DiagnosticCatalog.Require(DiagnosticCatalog.InterfaceActionRejected);

        for (int index = 0; index < code.Burst + 20; index++)
        {
            log.Write(DiagnosticCatalog.InterfaceActionRejected, "unaffordable purchase");
        }

        log.Drain();
        Assert.That(
            sink.Lines,
            Has.Count.EqualTo(code.Burst),
            "only the declared burst is emitted inside one window");

        // Closing the window is what produces the summary; the count is never discarded.
        clock.Advance(1000);
        log.Write(DiagnosticCatalog.InterfaceActionRejected, "unaffordable purchase");
        log.Drain();

        LogRecord? summary = null;
        int emitted = 0;
        foreach (string line in sink.Lines)
        {
            LogRecord record = DiagnosticsJsonContext.DeserializeLogLine(line);
            if (string.Equals(record.Code, DiagnosticCatalog.LogRateLimitSummary, StringComparison.Ordinal))
            {
                summary = record;
            }
            else
            {
                emitted++;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(summary, Is.Not.Null, "a closed window must report its suppressed count");
            Assert.That(FieldValue(summary!, "suppressed_code"), Is.EqualTo(DiagnosticCatalog.InterfaceActionRejected));
            Assert.That(FieldValue(summary!, "suppressed_count"), Is.EqualTo("20"));
            Assert.That(log.Suppressed, Is.EqualTo(20));
            Assert.That(emitted, Is.EqualTo(code.Burst + 1), "the new window emits again");
        });
    }

    /// <summary>A code that declares no burst is never suppressed, however often it repeats.</summary>
    [Test]
    public void ACodeWithoutADeclaredBurstIsNeverSuppressed()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog();
        for (int index = 0; index < 200; index++)
        {
            log.Write(DiagnosticCatalog.SimulationInvariantViolated, "invariant diverged");
        }

        log.Drain();

        Expect.Multiple(() =>
        {
            Assert.That(sink.Lines, Has.Count.EqualTo(200));
            Assert.That(
                log.Suppressed,
                Is.Zero,
                "CTR-OBS-001 permits rate-limiting only declared diagnostics");
        });
    }

    /// <summary>Writing performs no I/O; every sink write happens in <c>Drain</c>.</summary>
    [Test]
    public void WritingTouchesNoSinkSoDiagnosticsCannotBlockTheAuthoritativeTick()
    {
        TouchRecordingSink sink = new();
        TestClock clock = new();
        DiagnosticLog log = new(sink, Redaction.None(), () => clock.UtcNow, () => clock.Ticks);

        for (int index = 0; index < 50; index++)
        {
            log.Write(DiagnosticCatalog.EncounterSpawnQueued, "queued");
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                sink.Writes,
                Is.Zero,
                "doc 115 states CMP-OBS-001 never blocks the authoritative tick on I/O, so Write must "
                + "not reach the sink at all");
            Assert.That(log.Pending, Is.GreaterThan(0));
        });

        log.Drain();
        Assert.That(sink.Writes, Is.GreaterThan(0), "Drain is where the I/O happens");
    }

    /// <summary>A full ring drops the oldest record, counts the loss, and keeps the newest.</summary>
    [Test]
    public void AFullRingDropsTheOldestRecordAndCountsTheLoss()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog(capacity: 4);
        for (int index = 0; index < 10; index++)
        {
            log.Write(
                DiagnosticCatalog.PersistenceWriteCommitted,
                "record " + index.ToString(CultureInfo.InvariantCulture));
        }

        log.Drain();

        Expect.Multiple(() =>
        {
            Assert.That(sink.Lines, Has.Count.EqualTo(4));
            Assert.That(log.Overflowed, Is.EqualTo(6));
            Assert.That(
                DiagnosticsJsonContext.DeserializeLogLine(sink.Lines[^1]).Message,
                Is.EqualTo("record 9"),
                "a crash breadcrumb buffer keeps the records nearest the failure");
        });
    }

    /// <summary>
    /// A sink that always fails does not propagate the failure, counts every dropped record,
    /// and leaves the caller running.
    /// </summary>
    [Test]
    public void ASinkFailureIsCountedAndNeverPropagated()
    {
        FailingLogSink sink = new();
        TestClock clock = new();
        DiagnosticLog log = new(sink, Redaction.None(), () => clock.UtcNow, () => clock.Ticks);

        for (int index = 0; index < 5; index++)
        {
            log.Write(DiagnosticCatalog.PersistenceWriteCommitted, "committed");
        }

        int written = 0;
        Expect.DoesNotThrow(() => written = log.Drain());

        Expect.Multiple(() =>
        {
            Assert.That(written, Is.Zero);
            Assert.That(sink.Attempts, Is.EqualTo(5));
            Assert.That(log.Dropped, Is.EqualTo(5));
        });
    }

    /// <summary>An unregistered code is a build invariant violation, not a log line.</summary>
    [Test]
    public void AnUnregisteredCodeThrows()
    {
        (DiagnosticLog log, _, _) = NewLog();
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => log.Write("MMD-9999", "no such code"));
        Assert.That(failure.Message, Does.Contain("MMD-9999"));
    }

    /// <summary>
    /// Every category doc 90 lists has at least one registered code, every code's prefix and
    /// shape is stable, and no code is registered twice.
    /// </summary>
    [Test]
    public void TheCodeRegistryCoversEveryCategoryAndHasNoDuplicates()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<string> duplicates = new();
        List<string> malformed = new();

        foreach (DiagnosticCode code in DiagnosticCatalog.All)
        {
            if (!seen.Add(code.Code))
            {
                duplicates.Add(code.Code);
            }

            if (!code.Code.StartsWith(DiagnosticCatalog.CodePrefix, StringComparison.Ordinal)
                || code.Code.Length != DiagnosticCatalog.CodePrefix.Length + 4
                || code.Meaning.Length == 0)
            {
                malformed.Add(code.Code);
            }
        }

        IReadOnlySet<DiagnosticCategory> covered = DiagnosticCatalog.CoveredCategories();
        List<string> uncovered = new();
        foreach (DiagnosticCategory category in Enum.GetValues<DiagnosticCategory>())
        {
            if (!covered.Contains(category))
            {
                uncovered.Add(DiagnosticCatalog.NameOf(category));
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(duplicates, Is.Empty);
            Assert.That(malformed, Is.Empty);
            Assert.That(
                uncovered,
                Is.Empty,
                "doc 90 § Structured logging lists ten initial categories; each needs a registered code");
        });
    }

    /// <summary>
    /// A player-facing expected rejection is not an error log, and an unexpected invariant
    /// divergence is. Doc 90 states both, and the severity is fixed by the code rather than
    /// chosen at the call site, so this is a property of the registry.
    /// </summary>
    [Test]
    public void ExpectedPlayerFacingRejectionIsNotAnErrorButInvariantDivergenceIs()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                DiagnosticCatalog.Require(DiagnosticCatalog.InterfaceActionRejected).Severity,
                Is.EqualTo(DiagnosticSeverity.Information));
            Assert.That(
                DiagnosticCatalog.Require(DiagnosticCatalog.SimulationInvariantViolated).Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
        });
    }

    /// <summary>The <c>SCH-OBS-001</c> run record carries build identity and round-trips canonically.</summary>
    [Test]
    public void TheDiagnosticRunRecordCarriesIdentityAndRoundTrips()
    {
        BuildManifest identity = BuildIdentity.Current;
        DiagnosticRunRecord record = new()
        {
            DiagnosticId = "diag-0001",
            BuildIdentity = identity.IdentityLine,
            ContentIdentity = identity.Content.Status + ":" + identity.Content.OwningWorkPackage,
            DataVersions = new DiagnosticDataVersions
            {
                Schema = identity.DataVersions.Schema,
                Map = identity.DataVersions.Map,
                Random = identity.DataVersions.Random,
                Save = identity.DataVersions.Save,
            },
            Environment = new DiagnosticEnvironment { Platform = identity.Target.Platform },
        };
        record.Breadcrumbs.Add(new DiagnosticBreadcrumb
        {
            Tick = -1,
            Code = DiagnosticCatalog.BuildIdentityVerified,
            Detail = "identity verified before content loaded",
        });

        string first = DiagnosticsJsonContext.Serialize(record);
        string second = DiagnosticsJsonContext.Serialize(
            DiagnosticsJsonContext.DeserializeRunRecord(first));

        Expect.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first), "the run record round-trips byte-exactly");
            Assert.That(record.Schema, Is.EqualTo("SCH-OBS-001"));
            Assert.That(first, Does.Contain(identity.IdentityLine));
            Assert.That(
                first.IndexOf("\"diagnostic_id\"", StringComparison.Ordinal),
                Is.LessThan(first.IndexOf("\"breadcrumbs\"", StringComparison.Ordinal)),
                "field order is declaration order");
        });
    }

    /// <summary>A flush closes an open window so a suppression count survives shutdown.</summary>
    [Test]
    public void FlushReportsASuppressionCountWhoseWindowNeverElapsed()
    {
        (DiagnosticLog log, MemoryLogSink sink, _) = NewLog();
        DiagnosticCode code = DiagnosticCatalog.Require(DiagnosticCatalog.PresentationFallbackUsed);
        for (int index = 0; index < code.Burst + 7; index++)
        {
            log.Write(DiagnosticCatalog.PresentationFallbackUsed, "fallback proxy used");
        }

        log.Flush();

        bool sawSummary = false;
        foreach (string line in sink.Lines)
        {
            if (DiagnosticsJsonContext.DeserializeLogLine(line).Code
                == DiagnosticCatalog.LogRateLimitSummary)
            {
                sawSummary = true;
            }
        }

        Assert.That(sawSummary, Is.True, "shutdown must not lose a suppression count");
    }

    private static string FieldValue(LogRecord record, string name)
    {
        foreach (LogField field in record.Fields)
        {
            if (string.Equals(field.Name, name, StringComparison.Ordinal))
            {
                return field.Value;
            }
        }

        return string.Empty;
    }
}
