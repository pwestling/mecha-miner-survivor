using System;
using System.Numerics;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// The repository-owned PCG-XSH-RR 64/32 generator: a 64-bit state, a per-stream odd increment,
/// and a 32-bit output taken from the state <em>before</em> each advance.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// generator: "Its 64-bit state advances modulo <c>2^64</c> with multiplier
/// <c>6364136223846793005</c> and a per-stream odd increment. Output uses the PCG XSH-RR
/// transformation: xorshift the prior state by 18 and 27 bits and rotate the resulting 32-bit
/// value by the prior state's top five bits."
/// </para>
/// <para>
/// Doc 20 § Authoritative random-number contract fixes initialization: "Initialize PCG32 with
/// state zero and increment
/// <c>(selector shifted left one bit) OR 1</c>; advance once, add <c>state seed</c> to state
/// modulo <c>2^64</c>, and advance once again before returning the first caller-visible value."
/// Both priming advances are excluded from
/// <see cref="DrawCount"/> because doc 20 § Authoritative random-number contract scopes them to
/// before the first
/// <em>caller-visible</em> value.
/// </para>
/// <para>
/// <b>This struct is mutable by design, and it is <c>internal</c> so that a fork is impossible
/// rather than merely forbidden.</b> A stream's identity <em>is</em> its advancing state, so
/// copying one into a local silently forks it: both halves then replay the same values, and a
/// forked authoritative stream produces plausible numbers forever. No public member of this
/// assembly accepts, returns, or exposes a
/// <see cref="Pcg32"/> - the compiler enforces that, because a public signature naming an
/// internal type does not compile. <see cref="RandomStreamSet"/> is the only holder; it keeps
/// streams in a <see cref="Pcg32"/>[] and mutates array elements in place, and it exposes only
/// operations keyed by <see cref="RandomStreamKey"/>. Callers therefore have nothing to copy.
/// <c>VER-SIM-005-011</c> confirms the property and locks the accessibility that guarantees it.
/// </para>
/// <para>
/// Every multiply and add is <c>unchecked</c>: the whole contract is modulo-2^64 arithmetic, so
/// a checked overflow would be a runtime failure instead of the specified wrap. The rotation
/// uses <see cref="BitOperations.RotateRight(uint, int)"/> because
/// <c>AllowUnsafeBlocks</c> is <see langword="false"/> repository-wide.
/// </para>
/// </remarks>
internal struct Pcg32
{
    /// <summary>The LCG multiplier of doc 20 § Authoritative random-number contract, identical
    /// to <c>pcg32_random_r</c>.</summary>
    internal const ulong Multiplier = 6364136223846793005UL;

    /// <summary>The first xorshift distance of the XSH-RR output transformation (doc 20 §
    /// Authoritative random-number contract).</summary>
    internal const int FirstXorShift = 18;

    /// <summary>The second xorshift distance of the XSH-RR output transformation (doc 20 §
    /// Authoritative random-number contract).</summary>
    internal const int SecondXorShift = 27;

    /// <summary>The shift that selects the prior state's top five bits as the rotation (doc 20
    /// § Authoritative random-number contract).</summary>
    internal const int RotationShift = 59;

    private readonly ulong _increment;
    private ulong _state;
    private ulong _drawCount;

    private Pcg32(ulong state, ulong increment, ulong drawCount)
    {
        this._state = state;
        this._increment = increment;
        this._drawCount = drawCount;
    }

    /// <summary>The current 64-bit state: the value the next draw's output is taken
    /// from.</summary>
    public readonly ulong State => this._state;

    /// <summary>The per-stream odd increment.</summary>
    public readonly ulong Increment => this._increment;

    /// <summary>
    /// How many caller-visible draws this stream has produced. The two priming advances of doc
    /// 20 § Authoritative random-number contract are not counted.
    /// </summary>
    /// <remarks>
    /// A diagnostic counter, not part of the run-recovery contract: the recovery rule of doc 20
    /// § Authoritative random-number contract includes "stream state and odd increment" in
    /// recovery and nothing else. It exists because
    /// <c>VER-SIM-005-008</c> asserts that a degenerate selection leaves "the stream's state,
    /// increment, and consumed-draw count" identical, which is not observable from the state
    /// alone.
    /// </remarks>
    public readonly ulong DrawCount => this._drawCount;

    /// <summary>
    /// Initializes a stream from a derived state seed and selector exactly as the
    /// initialization rule of doc 20 § Authoritative random-number contract specifies.
    /// </summary>
    /// <param name="stateSeed">The derived state seed (doc 20 § Authoritative random-number
    /// contract).</param>
    /// <param name="selector">The derived selector, which becomes the odd increment.</param>
    /// <returns>A primed stream whose next draw is the first caller-visible value.</returns>
    public static Pcg32 Initialize(ulong stateSeed, ulong selector)
    {
        Pcg32 stream = new(0UL, unchecked((selector << 1) | 1UL), 0UL);
        stream.Prime();
        stream._state = unchecked(stream._state + stateSeed);
        stream.Prime();
        return stream;
    }

    /// <summary>
    /// Restores a stream from a recovered state and odd increment (doc 20 § Authoritative
    /// random-number contract).
    /// </summary>
    /// <param name="state">The recovered 64-bit state.</param>
    /// <param name="increment">The recovered odd increment.</param>
    /// <returns>A stream that continues the recovered sequence exactly.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="increment"/> is even. Doc 20 § Authoritative random-number contract
    /// requires a per-stream
    /// <em>odd</em> increment; an even one halves the period and is never produced by
    /// <see cref="Initialize"/>, so accepting it would resume a stream this contract cannot
    /// have created.
    /// </exception>
    public static Pcg32 Restore(ulong state, ulong increment)
    {
        if ((increment & 1UL) == 0UL)
        {
            throw new ArgumentException(
                "a PCG32 increment is odd (doc 20 § Authoritative random-number contract); an even increment is not a stream this contract can produce",
                nameof(increment));
        }

        return new Pcg32(state, increment, 0UL);
    }

    /// <summary>Produces the next caller-visible 32-bit output and advances the
    /// state.</summary>
    /// <returns>The XSH-RR transformation of the state before the advance.</returns>
    public uint NextUInt32()
    {
        ulong prior = this._state;
        this._state = unchecked((prior * Multiplier) + this._increment);
        this._drawCount = unchecked(this._drawCount + 1UL);
        return Transform(prior);
    }

    /// <summary>
    /// The XSH-RR output transformation of doc 20 § Authoritative random-number contract,
    /// applied to the state before the advance.
    /// </summary>
    /// <param name="priorState">The state the output is taken from.</param>
    /// <returns>The 32-bit output.</returns>
    internal static uint Transform(ulong priorState)
    {
        uint xorshifted = (uint)(((priorState >> FirstXorShift) ^ priorState) >> SecondXorShift);
        int rotation = (int)(priorState >> RotationShift);
        return BitOperations.RotateRight(xorshifted, rotation);
    }

    /// <summary>
    /// Advances the state once without counting a caller-visible draw and discards the output,
    /// which is what doc 20 § Authoritative random-number contract's two priming advances are.
    /// </summary>
    private void Prime()
    {
        this._state = unchecked((this._state * Multiplier) + this._increment);
    }
}
