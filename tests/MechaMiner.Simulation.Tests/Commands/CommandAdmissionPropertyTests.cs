using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Compares the gate against a deliberately simple reference model over randomized envelope streams containing
/// out-of-order, duplicated, stale, foreign-run, and unnormalizable entries.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-004-011</c>.
///
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models;
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations. The seed and case count are
/// logged before any input is generated, by <see cref="PropertyCase.ForAll"/>.
/// </remarks>
[TestFixture]
internal sealed class CommandAdmissionPropertyTests
{
    private const int DeclaredSeed = 611_004;

    /// <summary>
    /// Verification: <c>VER-SIM-004-011</c>.
    ///
    /// Over randomized envelope streams the admitted sequence equals the reference model's, every refusal
    /// reports the reason the reference derived, and no refused envelope changed the gate's authoritative
    /// state.
    /// </summary>
    [Test]
    public void AdmittedSequenceMatchesTheReferenceModel()
    {
        PropertyCase.ForAll(
            "commands-admitted-sequence-matches-reference",
            DeclaredSeed,
            caseCount: 64,
            generate: random =>
            {
                // Each element encodes one submission: its remainder modulo seven selects the kind of
                // envelope, and the quotient supplies the sequence gap, so a crude integer array is enough
                // for Shrinkers.Int32Array to minimize a failure into something readable.
                int[] script = new int[random.Next(1, 40)];
                for (int index = 0; index < script.Length; index++)
                {
                    script[index] = random.Next(0, 210);
                }

                return script;
            },
            shrink: Shrinkers.Int32Array,
            render: script => "[" + string.Join(",", script) + "]",
            property: RunEnvelopeStream);
    }

    private static void RunEnvelopeStream(int[] script)
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;
        ReferenceAdmissionModel reference = new(CommandFixture.RunSession);
        List<Submission> admittedSoFar = new();
        List<long> appliedSequences = new();
        List<AdmittedCommandSet> gateFrozenTicks = new();

        long tickIndex = 0;
        long nextSequence = 0;
        gate.BeginTick(new SimulationTick(tickIndex));
        reference.BeginTick(tickIndex);

        for (int index = 0; index < script.Length; index++)
        {
            int element = script[index];
            int kind = element % 7;
            int gap = 1 + ((element / 7) % 3);

            if (kind == 0)
            {
                CompareFrozenTick(gate, reference, gateFrozenTicks, index);
                tickIndex++;
                gate.BeginTick(new SimulationTick(tickIndex));
                reference.BeginTick(tickIndex);
                continue;
            }

            Submission submission = BuildSubmission(
                kind,
                tickIndex,
                ref nextSequence,
                gap,
                admittedSoFar,
                reference.HighestAdmittedSequence);

            string before = gate.RenderAuthoritative();
            bool gateAdmitted = gate.TryAdmit(submission.Envelope, out CommandRejection rejection);
            string after = gate.RenderAuthoritative();

            bool referenceAdmitted = reference.TryAdmit(
                submission.RunSession,
                submission.TargetTick,
                submission.Sequence,
                submission.RawInputX,
                submission.RawInputY,
                out string referenceReason);

            string where = "submission "
                + index.ToString(CultureInfo.InvariantCulture)
                + " ("
                + submission.Describe()
                + ")";

            Assert.That(gateAdmitted, Is.EqualTo(referenceAdmitted), where + ": admitted flag");
            if (gateAdmitted)
            {
                admittedSoFar.Add(submission);
                appliedSequences.Add(submission.Sequence);
                Assert.That(
                    rejection.IsRejection,
                    Is.False,
                    where + ": an admitted envelope carries no rejection");
            }
            else
            {
                Assert.That(
                    rejection.Reason.ToString(),
                    Is.EqualTo(referenceReason).Using(StringComparer.Ordinal),
                    where + ": rejection reason");
                CommandContractAssertions.NothingAuthoritativeChanged(where, before, after);
            }
        }

        CompareFrozenTick(gate, reference, gateFrozenTicks, script.Length);
        CommandContractAssertions.ACommandWasAppliedAtMostOnce("the randomized envelope stream", appliedSequences);
        CommandContractAssertions.AdmittedSequenceMatchesTheReferenceModel(
            "the randomized envelope stream",
            reference.RenderAllFrozenTicks(),
            RenderAllFrozenTicks(gateFrozenTicks, reference.FrozenTickCount));
    }

    /// <summary>
    /// Freezes the open tick on both the gate and the reference and compares the two renderings.
    /// </summary>
    /// <remarks>
    /// The gate keeps only its most recent frozen set, which is the right shape for production - a set is a
    /// value handed to the tick that uses it - so the run's whole history is accumulated here instead.
    /// </remarks>
    private static void CompareFrozenTick(
        CommandAdmissionGate gate,
        ReferenceAdmissionModel reference,
        List<AdmittedCommandSet> gateFrozenTicks,
        int submissionIndex)
    {
        AdmittedCommandSet frozen = gate.FreezeTick();
        string referenceRendering = reference.FreezeTick();
        gateFrozenTicks.Add(frozen);

        Assert.That(
            RenderFrozenSet(frozen),
            Is.EqualTo(referenceRendering).Using(StringComparer.Ordinal),
            "the tick frozen at submission "
                + submissionIndex.ToString(CultureInfo.InvariantCulture)
                + " must match the reference model");
    }

    /// <summary>Renders every frozen tick, in tick order.</summary>
    private static string RenderAllFrozenTicks(List<AdmittedCommandSet> frozen, int expectedTickCount)
    {
        Assert.That(
            frozen.Count,
            Is.EqualTo(expectedTickCount),
            "the gate and the reference must have frozen the same number of ticks");

        StringBuilder builder = new();
        for (int index = 0; index < frozen.Count; index++)
        {
            builder.Append(RenderFrozenSet(frozen[index])).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders one frozen set in the canonical form the reference model also produces.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>AdmittedCommandSet.Render</c>: the comparison must not be between one type's
    /// rendering and itself. This form carries the tick and the admitted sequences with their normalized
    /// intents, which is everything the entry asserts about.
    /// </remarks>
    private static string RenderFrozenSet(AdmittedCommandSet frozen)
    {
        StringBuilder builder = new();
        builder.Append("tick ").Append(frozen.TargetTick.Index.ToString(CultureInfo.InvariantCulture));
        for (int index = 0; index < frozen.Count; index++)
        {
            builder
                .Append(' ')
                .Append(frozen.SequenceAt(index).ToString(CultureInfo.InvariantCulture))
                .Append('=')
                .Append(frozen.IntentAt(index).ToString());
        }

        return builder.ToString();
    }

    /// <summary>Builds one submission of the requested kind, falling back when the kind is not available yet.</summary>
    private static Submission BuildSubmission(
        int kind,
        long tickIndex,
        ref long nextSequence,
        int gap,
        List<Submission> admittedSoFar,
        long highestAdmittedSequence)
    {
        switch (kind)
        {
            case 2 when admittedSoFar.Count > 0:
                // A duplicate: the exact identity of an envelope already admitted.
                return admittedSoFar[admittedSoFar.Count - 1];

            case 3 when tickIndex > 0:
                // Stale: a fresh sequence naming a tick that has already been frozen.
                return Fresh(CommandFixture.RunSession, tickIndex - 1, ref nextSequence, gap, finite: true);

            case 4:
                // A foreign run session, otherwise well formed.
                return Fresh(CommandFixture.ForeignRunSession, tickIndex, ref nextSequence, gap, finite: true);

            case 5 when highestAdmittedSequence >= 1:
                // A sequence regression: at or below the high-water mark.
                return new Submission(
                    CommandFixture.RunSession,
                    tickIndex,
                    highestAdmittedSequence - 1,
                    rawInputX: 1.0,
                    rawInputY: 0.0);

            case 6:
                // Well formed except that the payload cannot be normalized.
                return Fresh(CommandFixture.RunSession, tickIndex, ref nextSequence, gap, finite: false);

            default:
                // Case 1, and the fallback for any kind whose precondition is not met yet: a fresh in-order
                // envelope. The fallback keeps every generated script legal rather than skipping elements,
                // which would make the shrinker's candidates behave differently from the original.
                return Fresh(CommandFixture.RunSession, tickIndex, ref nextSequence, gap, finite: true);
        }
    }

    private static Submission Fresh(
        ulong runSession,
        long targetTick,
        ref long nextSequence,
        int gap,
        bool finite)
    {
        long sequence = nextSequence;
        nextSequence += gap;
        return new Submission(
            runSession,
            targetTick,
            sequence,
            finite ? 1.0 + (sequence % 3) : double.NaN,
            finite ? -1.0 : 0.0);
    }

    /// <summary>One generated submission: the identity, the raw payload, and the envelope built from them.</summary>
    /// <remarks>
    /// The raw payload is carried separately because <c>CommandEnvelope</c> exposes it to nobody, so the
    /// reference model has to be told what was sampled rather than reading it back out of the type under
    /// test.
    /// </remarks>
    private readonly struct Submission
    {
        internal Submission(ulong runSession, long targetTick, long sequence, double rawInputX, double rawInputY)
        {
            RunSession = runSession;
            TargetTick = targetTick;
            Sequence = sequence;
            RawInputX = rawInputX;
            RawInputY = rawInputY;
            Envelope = CommandEnvelope.Create(
                runSession,
                new SimulationTick(targetTick),
                sequence,
                rawInputX,
                rawInputY);
        }

        /// <summary>The run session the envelope names.</summary>
        internal ulong RunSession { get; }

        /// <summary>The tick the envelope targets.</summary>
        internal long TargetTick { get; }

        /// <summary>The sequence the envelope carries.</summary>
        internal long Sequence { get; }

        /// <summary>The raw planar X component as sampled.</summary>
        internal double RawInputX { get; }

        /// <summary>The raw planar Y component as sampled.</summary>
        internal double RawInputY { get; }

        /// <summary>The envelope handed to the gate.</summary>
        internal CommandEnvelope Envelope { get; }

        /// <summary>Describes the submission for a failure message.</summary>
        internal string Describe()
        {
            return "run="
                + RunSession.ToString("X16", CultureInfo.InvariantCulture)
                + " tick="
                + TargetTick.ToString(CultureInfo.InvariantCulture)
                + " seq="
                + Sequence.ToString(CultureInfo.InvariantCulture)
                + " raw=("
                + RawInputX.ToString("R", CultureInfo.InvariantCulture)
                + ","
                + RawInputY.ToString("R", CultureInfo.InvariantCulture)
                + ")";
        }
    }

    /// <summary>
    /// A deliberately simple model of the documented admission rules, written against doc 10 § Commands and
    /// mutations rather than against <c>CommandAdmissionGate</c>.
    /// </summary>
    /// <remarks>
    /// It keeps its history as a flat list and rescans it for the high-water mark on every submission, so it
    /// shares no data structure and no incremental bookkeeping with the implementation. It normalizes nothing:
    /// the raw payload's only relevance to admission is whether both components are finite, which is the
    /// documented condition, and the intents in the rendering come from the same public
    /// <c>MovementIntent.TryNormalize</c> the production path uses because normalization is not what this
    /// entry is a model of.
    /// </remarks>
    private sealed class ReferenceAdmissionModel
    {
        private readonly ulong _runSession;
        private readonly List<long> _appliedTicks = new();
        private readonly List<long> _appliedSequences = new();
        private readonly List<long> _openSequences = new();
        private readonly List<MovementIntent> _openIntents = new();
        private readonly List<string> _frozenTicks = new();

        private long _openTick = -1;
        private bool _isOpen;
        private long _lastFrozenTick = -1;

        internal ReferenceAdmissionModel(ulong runSession)
        {
            _runSession = runSession;
        }

        /// <summary>How many ticks the model has frozen.</summary>
        internal int FrozenTickCount => _frozenTicks.Count;

        /// <summary>The highest sequence the model has admitted, found by rescanning its history.</summary>
        internal long HighestAdmittedSequence
        {
            get
            {
                long highest = -1;
                for (int index = 0; index < _appliedSequences.Count; index++)
                {
                    if (_appliedSequences[index] > highest)
                    {
                        highest = _appliedSequences[index];
                    }
                }

                return highest;
            }
        }

        /// <summary>Opens the model's admission window for one tick.</summary>
        internal void BeginTick(long tick)
        {
            _openTick = tick;
            _isOpen = true;
            _openSequences.Clear();
            _openIntents.Clear();
        }

        /// <summary>Applies the documented rules in the documented order.</summary>
        internal bool TryAdmit(
            ulong runSession,
            long targetTick,
            long sequence,
            double rawInputX,
            double rawInputY,
            out string reason)
        {
            if (runSession != _runSession)
            {
                reason = nameof(CommandRejectionReason.ForeignRunSession);
                return false;
            }

            for (int index = 0; index < _appliedSequences.Count; index++)
            {
                if (_appliedSequences[index] != sequence)
                {
                    continue;
                }

                reason = _appliedTicks[index] == targetTick
                    ? nameof(CommandRejectionReason.Duplicate)
                    : nameof(CommandRejectionReason.SequenceRegression);
                return false;
            }

            if (sequence <= HighestAdmittedSequence)
            {
                reason = nameof(CommandRejectionReason.SequenceRegression);
                return false;
            }

            if (targetTick <= _lastFrozenTick)
            {
                reason = nameof(CommandRejectionReason.Stale);
                return false;
            }

            if (!_isOpen || targetTick != _openTick)
            {
                reason = nameof(CommandRejectionReason.AdmissionClosed);
                return false;
            }

            if (!double.IsFinite(rawInputX) || !double.IsFinite(rawInputY))
            {
                reason = nameof(CommandRejectionReason.InvalidPayload);
                return false;
            }

            _appliedTicks.Add(targetTick);
            _appliedSequences.Add(sequence);
            _openSequences.Add(sequence);
            _openIntents.Add(MovementIntent.Normalize(rawInputX, rawInputY));
            reason = string.Empty;
            return true;
        }

        /// <summary>Freezes the model's open tick and returns its canonical rendering.</summary>
        internal string FreezeTick()
        {
            StringBuilder builder = new();
            builder.Append("tick ").Append(_openTick.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < _openSequences.Count; index++)
            {
                builder
                    .Append(' ')
                    .Append(_openSequences[index].ToString(CultureInfo.InvariantCulture))
                    .Append('=')
                    .Append(_openIntents[index].ToString());
            }

            string rendering = builder.ToString();
            _frozenTicks.Add(rendering);
            _lastFrozenTick = _openTick;
            _isOpen = false;
            return rendering;
        }

        /// <summary>Renders every frozen tick, in tick order.</summary>
        internal string RenderAllFrozenTicks()
        {
            StringBuilder builder = new();
            for (int index = 0; index < _frozenTicks.Count; index++)
            {
                builder.Append(_frozenTicks[index]).Append('\n');
            }

            return builder.ToString();
        }
    }
}
