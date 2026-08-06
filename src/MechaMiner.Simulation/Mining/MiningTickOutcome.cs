using System;
using System.Globalization;

namespace MechaMiner.Simulation.Mining;

/// <summary>What one tick of phase 11 did to one mining site.</summary>
/// <remarks>
/// The ore is returned rather than added to a ledger inside the advance rule, so the rule stays a pure
/// function of a state and a position. That is what lets the installment arithmetic be tested without
/// a run, and it is what keeps the single writer of the run's resource total in one place - doc 10
/// § Commands and mutations' one-writer rule.
/// </remarks>
public readonly struct MiningTickOutcome : IEquatable<MiningTickOutcome>
{
    private readonly MiningSiteState _state;
    private readonly int _oreAwarded;

    internal MiningTickOutcome(MiningSiteState state, int oreAwarded)
    {
        _state = state;
        _oreAwarded = oreAwarded;
    }

    /// <summary>The site's state after the tick.</summary>
    public MiningSiteState State => _state;

    /// <summary>
    /// Common ore this tick secured, which is zero or one installment's worth.
    /// </summary>
    /// <remarks>
    /// At most one installment can complete per tick: forward progress is one tick of credit per tick
    /// and an installment costs 90, so two completions in one tick would need 90 ticks of credit to
    /// arrive at once. A caller that wants to assert that reads this field and not a count.
    /// </remarks>
    public int OreAwarded => _oreAwarded;

    /// <summary>Whether this tick completed an installment.</summary>
    public bool CompletedInstallment => _oreAwarded > 0;

    /// <inheritdoc/>
    public static bool operator ==(MiningTickOutcome left, MiningTickOutcome right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(MiningTickOutcome left, MiningTickOutcome right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(MiningTickOutcome other)
    {
        return _state.Equals(other._state) && _oreAwarded == other._oreAwarded;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MiningTickOutcome other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_state, _oreAwarded);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _state.ToString()
            + " ore+"
            + _oreAwarded.ToString(CultureInfo.InvariantCulture);
    }
}
