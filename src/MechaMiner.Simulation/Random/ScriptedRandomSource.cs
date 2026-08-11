using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// A source that returns pinned 32-bit values in order and throws when they run out.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// injection rule: "Tests may inject a scripted source, but production content cannot select an
/// alternate algorithm."
/// </para>
/// <para>
/// Exhaustion is a loud failure, never a fallback. A scripted source that quietly switched to a
/// real generator would turn a test that <em>looks</em> pinned into one whose later draws
/// depend on an algorithm the test never named — the exact silent substitution the generator of
/// doc 20 § Authoritative random-number contract forbids ("a runtime/library RNG is never an
/// implicit substitute"). It also makes an under-scripted test a reviewable error rather than a
/// passing test that proves less than it claims, which is what
/// <c>VER-SIM-005-007</c> relies on to show that integer-ratio chance consumes exactly one
/// draw.
/// </para>
/// </remarks>
public sealed class ScriptedRandomSource : IRandomSource
{
    private readonly uint[] _values;
    private int _position;

    /// <summary>Creates a source over the given draws, in order.</summary>
    /// <param name="values">The 32-bit values this source returns, in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    public ScriptedRandomSource(params uint[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        this._values = (uint[])values.Clone();
    }

    /// <summary>Creates a source over the given draws, in order.</summary>
    /// <param name="values">The 32-bit values this source returns, in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    public ScriptedRandomSource(IReadOnlyList<uint> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        this._values = new uint[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            this._values[index] = values[index];
        }
    }

    /// <inheritdoc/>
    public ulong DrawCount => (ulong)this._position;

    /// <summary>How many scripted draws remain before exhaustion.</summary>
    public int RemainingCount => this._values.Length - this._position;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"> The scripted values are exhausted. A
    /// scripted source never falls back to a real generator.
    /// </exception>
    public uint NextUInt32()
    {
        if (this._position >= this._values.Length)
        {
            throw new InvalidOperationException(
                "scripted random source exhausted after "
                    + this._values.Length.ToString(CultureInfo.InvariantCulture)
                    + " draw(s). A scripted source never falls back to a real generator (doc 20 § Authoritative random-number contract); "
                    + "script the draws the code under test actually consumes");
        }

        uint value = this._values[this._position];
        this._position++;
        return value;
    }
}
