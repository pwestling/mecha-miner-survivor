using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// An immutable, overlap-aware set of blocking reasons. The run is blocking if and only if
/// the set is non-empty.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: "Pause is represented
/// as a set of reasons rather than a single toggle", "Multiple reasons may overlap", and
/// "Simulation resumes only when all blocking reasons are cleared."
/// </para>
/// <para>
/// The set is a value over a 7-bit mask, so an exhaustive sweep of all 128 subsets - which
/// is what <c>VER-SIM-002-003</c> requires and what doc 10 § Verification requirements means
/// by "every pause source in overlapping combinations" - is a loop over
/// <c>0..127</c> rather than 128 hand-written cases.
/// </para>
/// <para>
/// <b>This type does not enforce the one-way terminal rule.</b> A set that sometimes
/// refuses a member is not a set. doc 115 § Mutable-state ownership matrix gives run
/// terminal state exactly one writer, so the refusal lives in that writer,
/// <see cref="RunClock"/>, and reaches the caller as a
/// <see cref="PauseTransitionResult"/>.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> and
/// <c>CMP-UIX-001</c> in <c>game/</c> read the set to choose pause presentation, and
/// <c>MechaMiner.Game.Tests</c> asserts on it across the engine boundary. Because it is a
/// value type with no reference to the run, handing one out cannot let a consumer mutate
/// the run's own set - the property <c>VER-SIM-002-004</c> asserts.
/// </para>
/// </remarks>
public readonly struct PauseReasonSet : IEquatable<PauseReasonSet>
{
    /// <summary>
    /// The seven reasons in the order doc 10 § Pause contract lists them.
    /// </summary>
    /// <remarks>
    /// An immutable array, not a dictionary: <c>docs/technical/114-autonomous-agent-execution-protocol.md</c>
    /// § C# and domain defaults makes dictionaries "lookup indexes only" that "never define
    /// authoritative order", and this order is authoritative - it is the order every
    /// diagnostic and golden renders reasons in.
    /// </remarks>
    private static readonly ImmutableArray<PauseReason> OrderedReasons = ImmutableArray.Create(
        PauseReason.GeneralPause,
        PauseReason.Fabrication,
        PauseReason.RelicResolution,
        PauseReason.BlockingTutorialOrModal,
        PauseReason.FocusLoss,
        PauseReason.OperatingSystemSuspension,
        PauseReason.TerminalTransition);

    private readonly byte _mask;

    private PauseReasonSet(byte mask)
    {
        _mask = mask;
    }

    /// <summary>The set with no reason present: the only state in which ticks execute.</summary>
    public static PauseReasonSet Empty => default;

    /// <summary>
    /// The number of distinct blocking reasons doc 10 § Pause contract defines.
    /// </summary>
    public static int ReasonCount => OrderedReasons.Length;

    /// <summary>
    /// The mask value one greater than the largest representable set, which is the number of
    /// subsets an exhaustive sweep must cover.
    /// </summary>
    /// <remarks>
    /// <c>2^7 = 128</c>. Exposed so <c>VER-SIM-002-003</c> derives its sweep bound from the
    /// reason count instead of hard-coding 128, which would silently stop being exhaustive
    /// if an eighth reason were ever registered.
    /// </remarks>
    public static int SubsetCount => 1 << OrderedReasons.Length;

    /// <summary>The seven reasons, in the order doc 10 § Pause contract lists them.</summary>
    public static ImmutableArray<PauseReason> AllReasons => OrderedReasons;

    /// <summary>The raw bit mask, so an exhaustive sweep can enumerate every subset.</summary>
    public byte Mask => _mask;

    /// <summary>Whether the run is blocked, which is true exactly when the set is non-empty.</summary>
    /// <remarks>
    /// doc 10 § Pause contract: "The simulation executes no ticks while any blocking reason
    /// is present" and "Simulation resumes only when all blocking reasons are cleared."
    /// </remarks>
    public bool IsBlocking => _mask != 0;

    /// <summary>Whether no reason is present.</summary>
    public bool IsEmpty => _mask == 0;

    /// <summary>How many reasons are present.</summary>
    public int Count => BitOperations.PopCount(_mask);

    /// <summary>Whether <paramref name="reason"/> is present.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a registered reason.</exception>
    public bool Contains(PauseReason reason)
    {
        return (_mask & MaskOf(reason)) != 0;
    }

    /// <summary>
    /// Returns the set with <paramref name="reason"/> present. Idempotent: adding a reason
    /// already present returns an equal set and is not an error.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a registered reason.</exception>
    public PauseReasonSet With(PauseReason reason)
    {
        return new PauseReasonSet((byte)(_mask | MaskOf(reason)));
    }

    /// <summary>
    /// Returns the set with <paramref name="reason"/> absent. Idempotent: clearing a reason
    /// that is absent returns an equal set and is not an error.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a registered reason.</exception>
    public PauseReasonSet Without(PauseReason reason)
    {
        return new PauseReasonSet((byte)(_mask & ~MaskOf(reason)));
    }

    /// <summary>The reasons present, in doc 10's order.</summary>
    /// <remarks>
    /// An ordered list rather than a set enumeration, because a diagnostic line and a golden
    /// must render the same reasons in the same order every time.
    /// </remarks>
    public ImmutableArray<PauseReason> ToOrderedArray()
    {
        ImmutableArray<PauseReason>.Builder present = ImmutableArray.CreateBuilder<PauseReason>(Count);
        foreach (PauseReason reason in OrderedReasons)
        {
            if (Contains(reason))
            {
                present.Add(reason);
            }
        }

        return present.ToImmutable();
    }

    /// <summary>Builds a set from zero or more reasons.</summary>
    /// <param name="reasons">The reasons to include; repeats are ignored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="reasons"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A member of <paramref name="reasons"/> is unregistered.</exception>
    public static PauseReasonSet Of(params PauseReason[] reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);

        byte mask = 0;
        foreach (PauseReason reason in reasons)
        {
            mask |= MaskOf(reason);
        }

        return new PauseReasonSet(mask);
    }

    /// <summary>Builds a set from a raw subset mask, for an exhaustive sweep.</summary>
    /// <param name="mask">A value in <c>[0, SubsetCount)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="mask"/> has a bit set that is not a registered reason.
    /// </exception>
    public static PauseReasonSet FromMask(int mask)
    {
        if (mask < 0 || mask >= SubsetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mask),
                mask,
                "a pause-reason mask covers exactly the seven registered reasons of doc 10 § Pause "
                + "contract, so it lies in [0, 128)");
        }

        return new PauseReasonSet((byte)mask);
    }

    /// <inheritdoc />
    public bool Equals(PauseReasonSet other)
    {
        return _mask == other._mask;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PauseReasonSet other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _mask.GetHashCode();
    }

    /// <summary>
    /// Renders the present reasons in doc 10's order, comma separated, or <c>none</c>.
    /// </summary>
    /// <remarks>
    /// Canonical text for a diagnostic or a golden line: ordered, and independent of how the
    /// set was built.
    /// </remarks>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return "none";
        }

        StringBuilder rendered = new();
        foreach (PauseReason reason in OrderedReasons)
        {
            if (!Contains(reason))
            {
                continue;
            }

            if (rendered.Length > 0)
            {
                rendered.Append(',');
            }

            rendered.Append(reason.ToString());
        }

        return rendered.ToString();
    }

    /// <summary>Compares two sets for equal membership.</summary>
    public static bool operator ==(PauseReasonSet left, PauseReasonSet right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two sets for differing membership.</summary>
    public static bool operator !=(PauseReasonSet left, PauseReasonSet right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Validates a reason and returns its single bit.
    /// </summary>
    /// <remarks>
    /// An unregistered value fails closed rather than being silently ignored: doc 20 § Entity
    /// identity and references makes invalid references "fail closed", and a pause reason the
    /// set does not know would otherwise resume a run that should stay blocked.
    /// </remarks>
    private static byte MaskOf(PauseReason reason)
    {
        if (!OrderedReasons.Contains(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "doc 10 § Pause contract defines exactly seven blocking reasons; this value is not one "
                + "of them");
        }

        return (byte)(int)reason;
    }
}
