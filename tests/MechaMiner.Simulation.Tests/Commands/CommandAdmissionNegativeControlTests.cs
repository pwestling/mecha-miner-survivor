using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Proves the idempotence and atomicity gates can fail, by running the same assertions the real gates run
/// against stubs that are deliberately wrong.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-004-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires evidence that a gate can
/// fail. The stubs are ordinary valid C# that behaves incorrectly, not a deliberately invalid fixture, which
/// <c>docs/technical/delivery-waves.md</c> forbids inside a compiled project.
/// </para>
/// <para>
/// The assertions come from <see cref="CommandContractAssertions"/>, the same code
/// <see cref="CommandAdmissionGateTests"/>, <see cref="PausedTransactionTests"/>, and
/// <see cref="CommandAdmissionPropertyTests"/> use, so weakening one turns the real gates and this control red
/// together.
/// </para>
/// <para>
/// Note what a control here can and cannot be. The real gate has no path that admits a command without first
/// consulting the idempotency history and the monotonic high-water mark, and
/// <see cref="CommandRejection"/> and <see cref="PausedTransactionResult"/> hold nothing a caller could write
/// through, so there is nothing inside the real types to perturb. That is the stronger position; the stubs
/// here are separate types that <em>do</em> have those failures, so the assertions can be shown to catch them
/// while the real tests show the production types cannot produce them.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CommandAdmissionNegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-004-012</c>.
    ///
    /// A gate that keeps no deduplication history fails the at-most-once assertion; a transaction shell that
    /// mutates before validating fails the no-mutation assertion and the advance-exactly-once assertion; and
    /// the real gate passes all three.
    /// </summary>
    [Test]
    public void IdempotenceAndAtomicityAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        AssertHistorylessGateFailsTheAtMostOnceGate();
        AssertMutateBeforeValidateFailsTheNoMutationGate();
        AssertMutateTwiceFailsTheAdvanceExactlyOnceGate();
        AssertTheRealGatePassesEveryGate();
    }

    /// <summary>
    /// A stub that keeps no deduplication history applies one envelope twice, and the assertion names the
    /// sequence that came round again.
    /// </summary>
    private static void AssertHistorylessGateFailsTheAtMostOnceGate()
    {
        CommandEnvelope envelope = CommandFixture.Envelope(targetTick: 0, sequence: 0, rawInputX: 1.0, rawInputY: 0.0);
        HistorylessAdmissionGate broken = new(CommandFixture.RunSession);
        broken.BeginTick(0);
        broken.TryAdmit(envelope);
        broken.TryAdmit(envelope);

        // The fixture must actually reach the assertion. Without this the control would pass even if the stub
        // had refused the second submission, and it would then prove nothing about the assertion.
        Assert.That(
            broken.Applied.Count,
            Is.EqualTo(2),
            "the stub must genuinely apply the envelope twice, or the control below is vacuous");

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => CommandContractAssertions.ACommandWasAppliedAtMostOnce(
                "a stub gate that keeps no deduplication history",
                broken.Applied));

        Assert.That(
            failure.Message,
            Does.Contain("applied more than once"),
            "the at-most-once gate must be the assertion that failed, and it must name what came round again");
    }

    /// <summary>
    /// A stub transaction shell that advances its state version before checking the expected one leaves a
    /// changed rendering behind after refusing, which the no-mutation assertion catches.
    /// </summary>
    private static void AssertMutateBeforeValidateFailsTheNoMutationGate()
    {
        EagerTransactionShell broken = new();
        string before = broken.RenderAuthoritative();

        // A deliberately stale expected version, so the stub refuses - after it has already mutated.
        bool accepted = broken.Apply(expectedStateVersion: 99, clientCommandSequence: 0);
        string after = broken.RenderAuthoritative();

        Assert.That(accepted, Is.False, "the stub does refuse the stale request, it just mutates first");
        Assert.That(
            after,
            Is.Not.EqualTo(before).Using(StringComparer.Ordinal),
            "and the mutation must be visible in the rendering, or the control below is vacuous");

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => CommandContractAssertions.NothingAuthoritativeChanged(
                "a stub transaction shell that mutates before validating",
                before,
                after));

        Assert.That(
            failure.Message,
            Does.Contain("byte-identical"),
            "the no-mutation gate must be the assertion that failed");
    }

    /// <summary>
    /// A stub that applies a replay as a second commit advances its version twice, which the
    /// advance-exactly-once assertion catches.
    /// </summary>
    private static void AssertMutateTwiceFailsTheAdvanceExactlyOnceGate()
    {
        EagerTransactionShell broken = new();
        long before = broken.StateVersion;

        Assert.That(broken.Apply(expectedStateVersion: before, clientCommandSequence: 5), Is.True);
        Assert.That(
            broken.Apply(expectedStateVersion: broken.StateVersion, clientCommandSequence: 5),
            Is.True,
            "the stub applies the same idempotency key a second time, which is the failure being controlled");
        Assert.That(
            broken.StateVersion,
            Is.EqualTo(before + 2),
            "so its version has advanced twice, or the control below is vacuous");

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => CommandContractAssertions.StateVersionAdvancedExactlyOnce(
                "a stub transaction shell that applies a replay again",
                before,
                broken.StateVersion));

        Assert.That(
            failure.Message,
            Does.Contain("exactly one new state version"),
            "the advance-exactly-once gate must be the assertion that failed");
    }

    /// <summary>Every assertion must pass against the real gate, or the control is vacuous.</summary>
    private static void AssertTheRealGatePassesEveryGate()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        CommandEnvelope envelope = CommandFixture.Envelope(targetTick: 0, sequence: 0, rawInputX: 1.0, rawInputY: 0.0);
        List<long> applied = new();
        gate.BeginTick(SimulationTick.Zero);
        if (gate.TryAdmit(envelope, out CommandRejection _))
        {
            applied.Add(envelope.Sequence);
        }

        string beforeResubmission = gate.RenderAuthoritative();
        if (gate.TryAdmit(envelope, out CommandRejection _))
        {
            applied.Add(envelope.Sequence);
        }

        string afterResubmission = gate.RenderAuthoritative();
        gate.FreezeTick();
        fixture.PublishTick(0);
        fixture.Clock.CommitTick();
        fixture.Clock.Raise(PauseReason.Fabrication);

        long versionBefore = gate.TransactionStateVersion;
        PausedTransactionRequest request = CommandFixture.InstallRequest(versionBefore, clientCommandSequence: 3);
        PausedTransactionResult first = fixture.Apply(request);
        string afterFirstTransaction = gate.RenderAuthoritative();
        PausedTransactionResult replay = fixture.Apply(request);
        string afterReplay = gate.RenderAuthoritative();

        Expect.Multiple(() =>
        {
            Assert.That(first.IsAccepted, Is.True, "the real shell applies the first submission");
            Assert.That(
                replay.Reason,
                Is.EqualTo(TransactionRejectionReason.AlreadyApplied),
                "and answers the replay with the applied result");
        });

        Expect.DoesNotThrow(() => CommandContractAssertions.ACommandWasAppliedAtMostOnce(
            "the real admission gate",
            applied));
        Expect.DoesNotThrow(() => CommandContractAssertions.NothingAuthoritativeChanged(
            "the real gate refusing a duplicate envelope",
            beforeResubmission,
            afterResubmission));
        Expect.DoesNotThrow(() => CommandContractAssertions.NothingAuthoritativeChanged(
            "the real shell answering a replay",
            afterFirstTransaction,
            afterReplay));
        Expect.DoesNotThrow(() => CommandContractAssertions.StateVersionAdvancedExactlyOnce(
            "the real shell across two submissions of one key",
            versionBefore,
            gate.TransactionStateVersion));
    }

    /// <summary>
    /// A deliberately broken gate that admits any envelope for the open tick, keeping no deduplication history
    /// at all - which the real <see cref="CommandAdmissionGate"/> has no path to do.
    /// </summary>
    /// <remarks>
    /// Valid code that behaves incorrectly. It exists so the at-most-once assertion can be shown to catch a
    /// second application; nothing depends on it and it is never production behaviour.
    /// </remarks>
    private sealed class HistorylessAdmissionGate
    {
        private readonly ulong _runSession;
        private readonly List<long> _applied = new();
        private long _openTick = -1;

        internal HistorylessAdmissionGate(ulong runSession)
        {
            _runSession = runSession;
        }

        /// <summary>Every sequence this gate applied, in application order.</summary>
        internal IReadOnlyList<long> Applied => _applied;

        /// <summary>Opens the window for one tick.</summary>
        internal void BeginTick(long tick)
        {
            _openTick = tick;
        }

        /// <summary>Admits any envelope for the open tick, however many times it arrives.</summary>
        internal bool TryAdmit(CommandEnvelope envelope)
        {
            if (!envelope.BelongsTo(_runSession) || envelope.TargetTick.Index != _openTick)
            {
                return false;
            }

            _applied.Add(envelope.Sequence);
            return true;
        }
    }

    /// <summary>
    /// A deliberately broken paused-transaction shell that advances its state version and records its event
    /// before it validates anything, and that treats a replay as a fresh commit.
    /// </summary>
    /// <remarks>
    /// Valid code that behaves incorrectly. The real gate performs every check before the first statement of
    /// its commit block, so there is no ordering inside it to perturb; this type supplies the wrong ordering
    /// so the no-mutation and advance-exactly-once assertions can be shown to catch it.
    /// </remarks>
    private sealed class EagerTransactionShell
    {
        private readonly List<long> _emittedForClientSequence = new();
        private long _stateVersion = CommandAdmissionGate.InitialTransactionStateVersion;

        /// <summary>The shell's state version.</summary>
        internal long StateVersion => _stateVersion;

        /// <summary>Mutates, then validates - which is exactly the wrong order.</summary>
        internal bool Apply(long expectedStateVersion, long clientCommandSequence)
        {
            _stateVersion++;
            _emittedForClientSequence.Add(clientCommandSequence);
            return expectedStateVersion == _stateVersion - 1;
        }

        /// <summary>Renders the shell's whole state in the same spirit the real gate does.</summary>
        internal string RenderAuthoritative()
        {
            StringBuilder builder = new();
            builder
                .Append("stub stateVersion=")
                .Append(_stateVersion.ToString(CultureInfo.InvariantCulture))
                .Append(" emitted=")
                .Append(_emittedForClientSequence.Count.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < _emittedForClientSequence.Count; index++)
            {
                builder
                    .Append("\n  ")
                    .Append(_emittedForClientSequence[index].ToString(CultureInfo.InvariantCulture));
            }

            return builder.Append('\n').ToString();
        }
    }
}
