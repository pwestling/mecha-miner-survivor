using System;
using System.Globalization;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// The typed refusal of an active command envelope: a reason plus the identity that was refused, and
/// nothing that could change anything.
/// </summary>
/// <remarks>
/// <para>
/// <c>CTR-RUN-002</c> in <c>docs/technical/115-component-contract-and-schema-registry.md</c> §
/// Cross-boundary contract registry: "stale/duplicate/invalid commands return typed rejection/no change".
/// <c>docs/technical/115-...</c> § Component registry gives <c>CMP-SIM-002</c> "accepted normalized
/// commands or typed rejection" as its output and names "partial mutations" as its forbidden
/// responsibility.
/// </para>
/// <para>
/// <b>"No mutation on rejection" needs no enumeration of mutation sites.</b> This is a readonly struct of
/// scalars and one string. It holds no reference to the gate, no delegate, no collection, no buffer and no
/// entity handle, so there is nothing a holder of a rejection could write through and nothing the gate
/// gave away by returning one. That is a stronger statement than "every mutation site was checked",
/// because it does not depend on having found every site.
/// </para>
/// <para>
/// <b>A reason cannot be read where there is none.</b> <c>default</c> is not a rejection, and
/// <see cref="Reason"/> throws rather than reporting the zero member of
/// <see cref="CommandRejectionReason"/>. So a caller that ignores
/// <see cref="CommandAdmissionGate.TryAdmit"/>'s return value cannot mistake an admission for a stale
/// rejection.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the input adapter in <c>game/</c> - the
/// producer of <c>CTR-RUN-002</c> - and <c>CMP-OBS-001</c> read the refusal, and
/// <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct CommandRejection : IEquatable<CommandRejection>
{
    private readonly int _encodedReason;
    private readonly ulong _runSession;
    private readonly SimulationTick _targetTick;
    private readonly long _sequence;
    private readonly string? _detail;

    private CommandRejection(
        int encodedReason,
        ulong runSession,
        SimulationTick targetTick,
        long sequence,
        string detail)
    {
        _encodedReason = encodedReason;
        _runSession = runSession;
        _targetTick = targetTick;
        _sequence = sequence;
        _detail = detail;
    }

    /// <summary>The value that is not a rejection, returned alongside a successful admission.</summary>
    public static CommandRejection None => default;

    /// <summary>Whether this value is a rejection at all.</summary>
    public bool IsRejection => _encodedReason != 0;

    /// <summary>The typed reason the envelope was refused.</summary>
    /// <exception cref="InvalidOperationException">This value is not a rejection.</exception>
    /// <remarks>
    /// Stored offset by one so that <c>default</c> carries no reason rather than the zero member. Throwing
    /// is the point: there is no reason to report for an admitted command, so no value is invented for it.
    /// </remarks>
    public CommandRejectionReason Reason => IsRejection
        ? (CommandRejectionReason)(_encodedReason - 1)
        : throw new InvalidOperationException(
            "this value is not a rejection, so it carries no reason; check IsRejection, or the bool "
            + "returned by CommandAdmissionGate.TryAdmit");

    /// <summary>The run session the refused envelope claimed.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The tick the refused envelope targeted.</summary>
    public SimulationTick TargetTick => _targetTick;

    /// <summary>The sequence the refused envelope carried.</summary>
    public long Sequence => _sequence;

    /// <summary>Why this envelope in particular was refused, in words, for a diagnostic or the UI.</summary>
    public string Detail => _detail ?? string.Empty;

    /// <summary>Builds a rejection for one envelope.</summary>
    /// <param name="reason">The typed reason.</param>
    /// <param name="envelope">The envelope that was refused; only its identity is carried.</param>
    /// <param name="detail">Why this envelope was refused. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a defined member.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is blank.</exception>
    public static CommandRejection Of(CommandRejectionReason reason, in CommandEnvelope envelope, string detail)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "a rejection reason must be a defined member of CommandRejectionReason; an undefined "
                    + "value would be an untyped refusal, which CTR-RUN-002 forbids");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        return new CommandRejection(
            (int)reason + 1,
            envelope.RunSession,
            envelope.TargetTick,
            envelope.Sequence,
            detail);
    }

    /// <summary>Compares two rejections for exact equality of every field.</summary>
    public static bool operator ==(CommandRejection left, CommandRejection right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two rejections for inequality.</summary>
    public static bool operator !=(CommandRejection left, CommandRejection right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(CommandRejection other)
    {
        return _encodedReason == other._encodedReason
            && _runSession == other._runSession
            && _targetTick == other._targetTick
            && _sequence == other._sequence
            && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is CommandRejection other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _encodedReason,
            _runSession,
            _targetTick,
            _sequence,
            StringComparer.Ordinal.GetHashCode(Detail));
    }

    /// <summary>Renders the refusal as canonical invariant text for a diagnostic or a golden.</summary>
    public override string ToString()
    {
        if (!IsRejection)
        {
            return "admitted";
        }

        return "rejected "
            + Reason.ToString()
            + " run="
            + _runSession.ToString("X16", CultureInfo.InvariantCulture)
            + " tick="
            + _targetTick.ToString()
            + " seq="
            + _sequence.ToString(CultureInfo.InvariantCulture)
            + ": "
            + Detail;
    }
}
