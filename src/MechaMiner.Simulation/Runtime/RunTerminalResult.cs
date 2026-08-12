using System;
using System.Globalization;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// A run's terminal result: which of the two endings happened, on which tick, and what the run-local
/// ore total was when it did.
/// </summary>
/// <remarks>
/// <para>
/// <b>Assigned once and immutable</b>, per <c>docs/technical/20-simulation-core.md</c> § Scope and
/// invariants. That is why this is a value with no mutator: reassignment is refused by the world that
/// holds it (<c>GameplayWorld.Settle</c>), and the value itself cannot be edited into a different
/// ending after the fact.
/// </para>
/// <para>
/// <b>The ore total is recorded rather than banked.</b> Common ore is run-local: doc 40:90 - "The
/// specialized material and 50 common ore are run-local and are lost if unspent when the run ends" -
/// and doc 40:56 - "Death ends the run and discards all unspent ordinary resources under the normal
/// failure settlement." So the number here is what the run mined, not what a profile keeps, and it is
/// the same field on both outcomes because both discard it. Cross-run banking applies to Hyper Gold
/// (doc 40:127) and this slice mines none. The settlement itself is <c>PRG-006</c>'s; this is the
/// authoritative input to it.
/// </para>
/// </remarks>
public readonly struct RunTerminalResult : IEquatable<RunTerminalResult>
{
    private readonly RunOutcome _outcome;
    private readonly long _tick;
    private readonly long _runLocalCommonOre;

    private RunTerminalResult(RunOutcome outcome, long tick, long runLocalCommonOre)
    {
        _outcome = outcome;
        _tick = tick;
        _runLocalCommonOre = runLocalCommonOre;
    }

    /// <summary>A result meaning nothing has been assigned yet.</summary>
    public static RunTerminalResult Unassigned => default;

    /// <summary>Which ending happened.</summary>
    public RunOutcome Outcome => _outcome;

    /// <summary>The tick the ending was assigned on.</summary>
    public long Tick => _tick;

    /// <summary>The run-local common ore mined by the time the run ended.</summary>
    public long RunLocalCommonOre => _runLocalCommonOre;

    /// <summary>Whether a terminal result has been assigned.</summary>
    public bool IsAssigned => _outcome != RunOutcome.Undecided;

    /// <summary>
    /// Builds an assigned result.
    /// </summary>
    /// <param name="outcome">The ending. Must not be <see cref="RunOutcome.Undecided"/>.</param>
    /// <param name="tick">The tick it was assigned on.</param>
    /// <param name="runLocalCommonOre">The run-local ore total at that moment.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its domain.</exception>
    public static RunTerminalResult Assign(RunOutcome outcome, long tick, long runLocalCommonOre)
    {
        if (outcome == RunOutcome.Undecided)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "assigning the undecided outcome would produce a result that reads as no result, so a "
                    + "run could be settled and still look unsettled. Use RunTerminalResult.Unassigned "
                    + "for the not-ended state");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        ArgumentOutOfRangeException.ThrowIfNegative(runLocalCommonOre);

        return new RunTerminalResult(outcome, tick, runLocalCommonOre);
    }

    /// <inheritdoc/>
    public static bool operator ==(RunTerminalResult left, RunTerminalResult right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(RunTerminalResult left, RunTerminalResult right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(RunTerminalResult other)
    {
        return _outcome == other._outcome
            && _tick == other._tick
            && _runLocalCommonOre == other._runLocalCommonOre;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RunTerminalResult other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_outcome, _tick, _runLocalCommonOre);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsAssigned
            ? _outcome.ToString()
                + " at tick "
                + _tick.ToString(CultureInfo.InvariantCulture)
                + " with "
                + _runLocalCommonOre.ToString(CultureInfo.InvariantCulture)
                + " run-local common ore"
            : "undecided";
    }
}
