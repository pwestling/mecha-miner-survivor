using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// The frozen, immutable set of normalized commands admitted for exactly one tick.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering, phase 1: "Admit and normalize
/// commands for the tick." <c>docs/technical/20-simulation-core.md</c> § Tick transaction step 1: "Read the
/// prior committed state and commands admitted for this tick."
/// </para>
/// <para>
/// <b>Immutable after admission is a property of the type, not a discipline.</b> This is a readonly struct
/// over two <see cref="ImmutableArray{T}"/> values whose elements are readonly structs of scalars, and it
/// declares no member that adds, removes, replaces, or reorders anything. Phases 2 to 14 receive a
/// <em>copy</em> of the value, so even a phase that wanted to append has nothing to append to. That is
/// what <c>VER-SIM-004-006</c> asks for: the gate does not have to refuse a late append, because no
/// late append is expressible.
/// </para>
/// <para>
/// <b>Why a fresh immutable pair per tick rather than a pooled buffer.</b> A pooled mutable buffer handed
/// out under a read-only interface is still the same storage the gate keeps writing to, so the previous
/// tick's set would change under a holder as soon as the next tick admitted anything - and
/// <c>VER-SIM-004-006</c> asserts precisely that it does not. Two small immutable arrays per tick is the
/// price of the guarantee. doc 90 § CPU initial enforceable allocation budgets the simulation phase as a
/// whole and does not measure admission, and doc 20 § Active commands gives the initial surface as one
/// movement intent per tick, so the arrays are tiny.
/// </para>
/// <para>
/// <b>No collection is exposed.</b> doc 115 § Cross-boundary contract registry: "Cross-boundary payloads
/// never expose mutable collections." Rather than exposing an immutable collection and arguing that it is
/// safe, this type exposes <see cref="Count"/> and indexed accessors only, so there is no collection
/// object to hand on at all.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-SIM-001</c>, the simulation world, reads
/// "normalized commands" as its input, and <c>MechaMiner.Tools</c> renders admitted sets into scenario
/// reports. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct AdmittedCommandSet : IEquatable<AdmittedCommandSet>
{
    private readonly ulong _runSession;
    private readonly SimulationTick _targetTick;
    private readonly ImmutableArray<long> _sequences;
    private readonly ImmutableArray<MovementIntent> _intents;

    private AdmittedCommandSet(
        ulong runSession,
        SimulationTick targetTick,
        ImmutableArray<long> sequences,
        ImmutableArray<MovementIntent> intents)
    {
        _runSession = runSession;
        _targetTick = targetTick;
        _sequences = sequences;
        _intents = intents;
    }

    /// <summary>The value that is not a set: no run, no tick, no commands.</summary>
    /// <remarks>
    /// Distinguishable from an empty tick's set, which <see cref="IsFrozen"/> reports as frozen: a tick
    /// that admitted nothing is a real answer, and "no tick has been frozen yet" is not.
    /// </remarks>
    public static AdmittedCommandSet Unfrozen => default;

    /// <summary>Whether this value is a frozen set rather than the default.</summary>
    public bool IsFrozen => !_sequences.IsDefault;

    /// <summary>The run session every admitted command in this set carries.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The one tick this set was admitted for.</summary>
    public SimulationTick TargetTick => _targetTick;

    /// <summary>How many commands were admitted for the tick.</summary>
    public int Count => _sequences.IsDefault ? 0 : _sequences.Length;

    /// <summary>Whether the tick admitted no command at all, which is an ordinary outcome.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// The highest sequence in the set, or <c>-1</c> when the set is empty.
    /// </summary>
    /// <remarks>
    /// The set is stored in admission order and admission is monotonic in sequence, so this is the last
    /// element. It is exposed as a named fact rather than left to the caller to index, because
    /// <c>VER-SIM-004-003</c>'s "never reordered" is asserted against the whole ordering and this is the
    /// cheap consistency check on it.
    /// </remarks>
    public long HighestSequence => Count == 0 ? -1L : _sequences[Count - 1];

    /// <summary>
    /// The intent the tick's movement phase applies: the last one admitted, or
    /// <see cref="MovementIntent.Stop"/> when nothing was admitted.
    /// </summary>
    /// <remarks>
    /// doc 20 § Active commands: "The simulation applies immediate direction and full current speed for
    /// nonzero input and stops on zero input." Immediate means the tick uses the newest intent it was
    /// given, so a tick that received two samples applies the later one; a tick that received none holds
    /// no intent, and doc 20's stop-on-zero makes <see cref="MovementIntent.Stop"/> the only defensible
    /// answer there rather than the previous tick's direction carried over invisibly.
    /// </remarks>
    public MovementIntent LatestIntent => Count == 0 ? MovementIntent.Stop : _intents[Count - 1];

    /// <summary>Freezes one tick's admitted commands into an immutable set.</summary>
    /// <param name="runSession">The run session. Must not be zero.</param>
    /// <param name="targetTick">The tick these commands were admitted for.</param>
    /// <param name="sequences">The admitted sequences, in admission order.</param>
    /// <param name="intents">The normalized intents, index-aligned with <paramref name="sequences"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    /// <exception cref="ArgumentNullException">A list argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// The two lists differ in length, or the sequences are not strictly increasing.
    /// </exception>
    /// <remarks>
    /// The strictly-increasing check is not defensive duplication of the gate's monotonic rule: it is the
    /// assertion that freezing preserved it. <c>VER-SIM-004-003</c> requires that "no admitted envelope is
    /// ever reordered relative to another", and the one place that could go wrong is the copy out of the
    /// gate's working lists, which is here.
    /// </remarks>
    public static AdmittedCommandSet Freeze(
        ulong runSession,
        SimulationTick targetTick,
        IReadOnlyList<long> sequences,
        IReadOnlyList<MovementIntent> intents)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'");
        }

        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(intents);

        if (sequences.Count != intents.Count)
        {
            throw new ArgumentException(
                "an admitted set holds one normalized intent per admitted sequence: "
                    + sequences.Count.ToString(CultureInfo.InvariantCulture)
                    + " sequence(s) against "
                    + intents.Count.ToString(CultureInfo.InvariantCulture)
                    + " intent(s)",
                nameof(intents));
        }

        for (int index = 1; index < sequences.Count; index++)
        {
            if (sequences[index] <= sequences[index - 1])
            {
                throw new ArgumentException(
                    "admitted sequences are strictly increasing in admission order, but element "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " is "
                        + sequences[index].ToString(CultureInfo.InvariantCulture)
                        + " after "
                        + sequences[index - 1].ToString(CultureInfo.InvariantCulture)
                        + "; doc 10 § Commands and mutations requires a monotonic command sequence",
                    nameof(sequences));
            }
        }

        return new AdmittedCommandSet(
            runSession,
            targetTick,
            ImmutableArray.CreateRange(sequences),
            ImmutableArray.CreateRange(intents));
    }

    /// <summary>The sequence of the command at <paramref name="index"/> in admission order.</summary>
    /// <param name="index">A position in <c>[0, <see cref="Count"/>)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the set.</exception>
    public long SequenceAt(int index)
    {
        RequireIndex(index);
        return _sequences[index];
    }

    /// <summary>The normalized intent of the command at <paramref name="index"/> in admission order.</summary>
    /// <param name="index">A position in <c>[0, <see cref="Count"/>)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the set.</exception>
    public MovementIntent IntentAt(int index)
    {
        RequireIndex(index);
        return _intents[index];
    }

    /// <summary>Whether <paramref name="sequence"/> was admitted for this tick.</summary>
    /// <param name="sequence">The sequence to look for.</param>
    /// <remarks>
    /// A linear scan over the admission order rather than a hash lookup. The set holds a handful of
    /// elements, and a dictionary here would be a second representation of the same ordering - which the
    /// house rule "dictionaries are lookup indexes only, never authoritative order" exists to prevent.
    /// </remarks>
    public bool ContainsSequence(long sequence)
    {
        for (int index = 0; index < Count; index++)
        {
            if (_sequences[index] == sequence)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Compares two sets element by element, not by backing-array identity.</summary>
    public static bool operator ==(AdmittedCommandSet left, AdmittedCommandSet right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two sets for inequality.</summary>
    public static bool operator !=(AdmittedCommandSet left, AdmittedCommandSet right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Element-wise, because <see cref="ImmutableArray{T}"/>'s own equality compares the backing array
    /// reference: two sets frozen from the same commands would otherwise compare unequal, and
    /// <c>VER-SIM-004-006</c> compares a held set against a re-derived one.
    /// </remarks>
    public bool Equals(AdmittedCommandSet other)
    {
        if (_runSession != other._runSession
            || _targetTick != other._targetTick
            || IsFrozen != other.IsFrozen
            || Count != other.Count)
        {
            return false;
        }

        for (int index = 0; index < Count; index++)
        {
            if (_sequences[index] != other._sequences[index] || _intents[index] != other._intents[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is AdmittedCommandSet other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(_runSession);
        hash.Add(_targetTick);
        hash.Add(IsFrozen);
        for (int index = 0; index < Count; index++)
        {
            hash.Add(_sequences[index]);
            hash.Add(_intents[index]);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Render();
    }

    /// <summary>Renders the whole set as canonical invariant text, in admission order.</summary>
    /// <remarks>
    /// Whole-set rather than field-by-field, because "the admitted set is byte-identical" is the assertion
    /// <c>VER-SIM-004-002</c> makes and a rendering of everything cannot omit the field someone forgot.
    /// </remarks>
    public string Render()
    {
        if (!IsFrozen)
        {
            return "admitted(unfrozen)";
        }

        StringBuilder builder = new();
        builder
            .Append("admitted run=")
            .Append(_runSession.ToString("X16", CultureInfo.InvariantCulture))
            .Append(" tick=")
            .Append(_targetTick.ToString())
            .Append(" count=")
            .Append(Count.ToString(CultureInfo.InvariantCulture));

        for (int index = 0; index < Count; index++)
        {
            builder
                .Append("\n  ")
                .Append(_sequences[index].ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(_intents[index].ToString());
        }

        return builder.ToString();
    }

    private void RequireIndex(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "the admitted set holds "
                    + Count.ToString(CultureInfo.InvariantCulture)
                    + " command(s)");
        }
    }
}
