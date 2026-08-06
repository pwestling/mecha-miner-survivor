using System;
using System.Globalization;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// <c>CTR-RUN-002</c>: one unit of authoritative external intent in transit - run session, target tick,
/// monotonic sequence, and a payload that can only be read as a normalized value and only by the run it
/// names.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary contract registry,
/// <c>CTR-RUN-002</c>: produced by the "input adapter", consumed by <c>CMP-SIM-002</c>, carrying "run ID,
/// target tick, monotonic sequence, normalized payload", and on failure "stale/duplicate/invalid commands
/// return typed rejection/no change".
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations: "All authoritative external
/// intent crosses into the run through typed commands or paused transactions", "Active-play movement input
/// is sampled by the input adapter and converted into the command for the next simulation tick", and
/// "Commands that can cross an asynchronous boundary carry a run-session identity and monotonic command
/// sequence."
/// </para>
/// <para>
/// <b>The run fence is structural, not a check the gate remembers to make.</b> The raw payload is private
/// and there is no member that returns it, in any form, without being handed the expected run session:
/// <see cref="TryNormalizePayload"/> is the only way out and it refuses a session that does not match.
/// <see cref="ToString"/> renders identity alone. So "a foreign envelope's payload was never normalized or
/// inspected" (<c>VER-SIM-004-004</c>) is not an ordering the gate has to preserve - there is no call the
/// gate could make that would reach the payload first.
/// </para>
/// <para>
/// <b>The payload is raw in transit and normalized on admission.</b> doc 10 § Commands and mutations says
/// commands "are validated at application boundaries and again against authoritative state when applied",
/// so this type carries what the producer actually sent - including a non-finite component - and
/// <see cref="CommandAdmissionGate"/> turns that into
/// <see cref="CommandRejectionReason.InvalidPayload"/>. Validating the payload in
/// <see cref="Create"/> would move that refusal to the wrong side of the asynchronous boundary, where it
/// would be an exception in the producer rather than a typed rejection to the run.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the input adapter in <c>game/</c> constructs
/// these; <c>MechaMiner.Tools</c> replays scenario command streams built from them; and
/// <c>MechaMiner.Game.Tests</c> asserts on them. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct CommandEnvelope : IEquatable<CommandEnvelope>
{
    /// <summary>The first sequence a run's command stream may carry.</summary>
    /// <remarks>
    /// Zero rather than one, so the "highest admitted sequence" high-water mark starts at
    /// <c>-1</c> and the first envelope of a run is strictly greater than it without a special case.
    /// </remarks>
    public const long FirstSequence = 0;

    private readonly ulong _runSession;
    private readonly SimulationTick _targetTick;
    private readonly long _sequence;
    private readonly double _rawInputX;
    private readonly double _rawInputY;

    private CommandEnvelope(
        ulong runSession,
        SimulationTick targetTick,
        long sequence,
        double rawInputX,
        double rawInputY)
    {
        _runSession = runSession;
        _targetTick = targetTick;
        _sequence = sequence;
        _rawInputX = rawInputX;
        _rawInputY = rawInputY;
    }

    /// <summary>The run session this envelope was produced for.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The tick this envelope's intent is for.</summary>
    /// <remarks>
    /// doc 10 § Commands and mutations: input "is converted into the command for the next simulation
    /// tick", so the producer names the tick rather than letting arrival order decide it. That is what
    /// makes a late arrival <see cref="CommandRejectionReason.Stale"/> rather than silently applying to
    /// whichever tick happened to be running.
    /// </remarks>
    public SimulationTick TargetTick => _targetTick;

    /// <summary>The producer's monotonic command sequence within the run.</summary>
    public long Sequence => _sequence;

    /// <summary>
    /// Whether this envelope was constructed rather than defaulted.
    /// </summary>
    /// <remarks>
    /// Run session zero is reserved to mean "no run" throughout this assembly - see
    /// <c>EntityId.IsUnset</c> - so the default value is recognizably not an envelope and can never pass
    /// the run fence of any real run.
    /// </remarks>
    public bool IsPresent => _runSession != 0;

    /// <summary>Creates an envelope. The payload is carried as sampled and is not validated here.</summary>
    /// <param name="runSession">The run session. Must not be zero.</param>
    /// <param name="targetTick">The tick the intent is for.</param>
    /// <param name="sequence">The producer's monotonic sequence. Must not be below <see cref="FirstSequence"/>.</param>
    /// <param name="rawInputX">The raw planar X component as sampled. Not validated; see the type remarks.</param>
    /// <param name="rawInputY">The raw planar Y component as sampled. Not validated; see the type remarks.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="runSession"/> is zero, or <paramref name="sequence"/> is below
    /// <see cref="FirstSequence"/>.
    /// </exception>
    /// <remarks>
    /// The two arguments that <em>are</em> validated are the ones a producer cannot get wrong by
    /// accident: a zero run session would be an envelope belonging to no run, and a negative sequence
    /// could not participate in a monotonic order. Both are defects in the producer rather than input a
    /// player can supply, so they throw instead of becoming a rejection.
    /// </remarks>
    public static CommandEnvelope Create(
        ulong runSession,
        SimulationTick targetTick,
        long sequence,
        double rawInputX,
        double rawInputY)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'; doc 10 § Commands and mutations requires "
                    + "every command that can cross an asynchronous boundary to carry a run-session "
                    + "identity");
        }

        if (sequence < FirstSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "a command sequence is monotonic within a run and starts at "
                    + FirstSequence.ToString(CultureInfo.InvariantCulture));
        }

        return new CommandEnvelope(runSession, targetTick, sequence, rawInputX, rawInputY);
    }

    /// <summary>Whether this envelope belongs to <paramref name="runSession"/>.</summary>
    /// <param name="runSession">The run session the reader speaks for.</param>
    /// <remarks>
    /// A defaulted envelope belongs to no run, so this is false for it whatever
    /// <paramref name="runSession"/> is.
    /// </remarks>
    public bool BelongsTo(ulong runSession)
    {
        return IsPresent && _runSession == runSession;
    }

    /// <summary>
    /// Normalizes the payload for the run that owns it, and refuses any other reader.
    /// </summary>
    /// <param name="expectedRunSession">The run session the reader speaks for.</param>
    /// <param name="intent">The normalized intent, or <see cref="MovementIntent.Stop"/> on refusal.</param>
    /// <returns>
    /// <see langword="false"/> when the envelope belongs to a different run session, or when its raw
    /// payload has no normalized meaning.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The only route to the payload, which is what makes <c>VER-SIM-004-004</c> structural rather than an
    /// ordering to preserve. The two failures are deliberately not distinguished here: an envelope has no
    /// business explaining which of the caller's checks it failed, and
    /// <see cref="CommandAdmissionGate"/> assigns
    /// <see cref="CommandRejectionReason.ForeignRunSession"/> or
    /// <see cref="CommandRejectionReason.InvalidPayload"/> from checks it made itself, in that order.
    /// </para>
    /// <para>
    /// The fence is re-tested here even though the gate has already tested it. That is not redundancy to
    /// remove: it is the reason no future caller can reach the payload by forgetting to fence.
    /// </para>
    /// </remarks>
    public bool TryNormalizePayload(ulong expectedRunSession, out MovementIntent intent)
    {
        if (!BelongsTo(expectedRunSession))
        {
            intent = MovementIntent.Stop;
            return false;
        }

        return MovementIntent.TryNormalize(_rawInputX, _rawInputY, out intent);
    }

    /// <summary>Compares two envelopes for exact equality of identity and raw payload.</summary>
    public static bool operator ==(CommandEnvelope left, CommandEnvelope right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two envelopes for inequality.</summary>
    public static bool operator !=(CommandEnvelope left, CommandEnvelope right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(CommandEnvelope other)
    {
        return _runSession == other._runSession
            && _targetTick == other._targetTick
            && _sequence == other._sequence
            && _rawInputX.Equals(other._rawInputX)
            && _rawInputY.Equals(other._rawInputY);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is CommandEnvelope other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_runSession, _targetTick, _sequence, _rawInputX, _rawInputY);
    }

    /// <summary>
    /// Renders the envelope's identity - and only its identity - as canonical invariant text.
    /// </summary>
    /// <remarks>
    /// The payload is deliberately absent. If diagnostics could print it, "the payload is unreachable
    /// without the run fence" would be true of one method and false of the type, and a later reader would
    /// have to work out which of the two claims held.
    /// </remarks>
    public override string ToString()
    {
        if (!IsPresent)
        {
            return "envelope(none)";
        }

        return "envelope run="
            + _runSession.ToString("X16", CultureInfo.InvariantCulture)
            + " tick="
            + _targetTick.ToString()
            + " seq="
            + _sequence.ToString(CultureInfo.InvariantCulture);
    }
}
