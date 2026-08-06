namespace MechaMiner.Simulation.Random;

/// <summary>
/// The SplitMix64 <c>Mix</c> function and the four-step
/// <c>d0</c> → <c>d1</c> → state-seed → selector derivation chain of doc 20 § Authoritative
/// random-number contract.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// derivation chain: "Define the unsigned-64-bit wrapping function <c>Mix(x)</c> by adding
/// <c>0x9E3779B97F4A7C15</c>, applying xor-shift 30 then multiplying by
/// <c>0xBF58476D1CE4E5B9</c>, applying xor-shift 27 then multiplying by
/// <c>0x94D049BB133111EB</c>, then applying xor-shift 31. Derive
/// <c>d0 = Mix(master seed XOR (schema version × 0xD1B54A32D192ED03))</c>,
/// <c>d1 = Mix(d0 XOR family key)</c>,
/// <c>state seed = Mix(d1 XOR (instance key × 0x9E3779B97F4A7C15))</c>, and
/// <c>selector = Mix(state seed XOR 0x94D049BB133111EB)</c>. All arithmetic wraps modulo
/// <c>2^64</c>."
/// </para>
/// <para>
/// Each step is exposed separately because
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-seed-derivation.txt</c> pins all four
/// intermediate values: a chain that only exposed its result could hide a compensating pair of
/// errors.
/// </para>
/// <para>
/// Every operation is <c>unchecked</c>. The contract is modulo-2^64 arithmetic, so a checked
/// overflow would be a runtime failure instead of the specified wrap.
/// </para>
/// </remarks>
public static class SeedDerivation
{
    /// <summary>The SplitMix64 gamma, the derivation chain of doc 20 § Authoritative
    /// random-number contract. Also the instance-key multiplier.</summary>
    internal const ulong Gamma = 0x9E3779B97F4A7C15UL;

    /// <summary>The first SplitMix64 multiplier, the derivation chain of doc 20 § Authoritative
    /// random-number contract.</summary>
    internal const ulong FirstMultiplier = 0xBF58476D1CE4E5B9UL;

    /// <summary>The second SplitMix64 multiplier, the derivation chain of doc 20 §
    /// Authoritative random-number contract. Also the selector salt.</summary>
    internal const ulong SecondMultiplier = 0x94D049BB133111EBUL;

    /// <summary>The schema-version multiplier of doc 20 § Authoritative random-number
    /// contract's <c>d0</c> step.</summary>
    internal const ulong SchemaVersionMultiplier = 0xD1B54A32D192ED03UL;

    /// <summary>The three xor-shift distances of <c>Mix</c>, the derivation chain of doc 20 §
    /// Authoritative random-number contract.</summary>
    internal const int FirstShift = 30;

    /// <summary>The second xor-shift distance of <c>Mix</c>, the derivation chain of doc 20 §
    /// Authoritative random-number contract.</summary>
    internal const int SecondShift = 27;

    /// <summary>The third xor-shift distance of <c>Mix</c>, the derivation chain of doc 20 §
    /// Authoritative random-number contract.</summary>
    internal const int ThirdShift = 31;

    /// <summary>
    /// The unsigned-64-bit wrapping mixing function of doc 20 § Authoritative random-number
    /// contract, which is exactly Vigna's SplitMix64 <c>next()</c>.
    /// </summary>
    /// <param name="value">The value to mix.</param>
    /// <returns>The mixed value.</returns>
    public static ulong Mix(ulong value)
    {
        ulong mixed = unchecked(value + Gamma);
        mixed ^= mixed >> FirstShift;
        mixed = unchecked(mixed * FirstMultiplier);
        mixed ^= mixed >> SecondShift;
        mixed = unchecked(mixed * SecondMultiplier);
        mixed ^= mixed >> ThirdShift;
        return mixed;
    }

    /// <summary>
    /// Step one: <c>d0 = Mix(master seed XOR (schema version × 0xD1B54A32D192ED03))</c>.
    /// </summary>
    /// <param name="schemaVersion">The random schema version (doc 20 § Authoritative
    /// random-number contract).</param>
    /// <param name="masterSeed">The deployment master seed, one unsigned 64-bit value.</param>
    /// <returns><c>d0</c>.</returns>
    public static ulong DeriveD0(RandomSchemaVersion schemaVersion, ulong masterSeed)
    {
        return Mix(masterSeed ^ unchecked((ulong)schemaVersion.Value * SchemaVersionMultiplier));
    }

    /// <summary>Step two: <c>d1 = Mix(d0 XOR family key)</c>.</summary>
    /// <param name="d0">The value returned by <see cref="DeriveD0"/>.</param>
    /// <param name="familyKey">A registered family key of doc 20 § Authoritative random-number
    /// contract.</param>
    /// <returns><c>d1</c>.</returns>
    public static ulong DeriveD1(ulong d0, ushort familyKey)
    {
        return Mix(d0 ^ familyKey);
    }

    /// <summary>
    /// Step three: <c>state seed = Mix(d1 XOR (instance key × 0x9E3779B97F4A7C15))</c>.
    /// </summary>
    /// <param name="d1">The value returned by <see cref="DeriveD1"/>.</param>
    /// <param name="instanceKey">The instance key of doc 20 § Authoritative random-number
    /// contract.</param>
    /// <returns>The state seed.</returns>
    public static ulong DeriveStateSeed(ulong d1, ulong instanceKey)
    {
        return Mix(d1 ^ unchecked(instanceKey * Gamma));
    }

    /// <summary>Step four: <c>selector = Mix(state seed XOR 0x94D049BB133111EB)</c>.</summary>
    /// <param name="stateSeed">The value returned by <see cref="DeriveStateSeed"/>.</param>
    /// <returns>The selector, which becomes the stream's odd increment.</returns>
    public static ulong DeriveSelector(ulong stateSeed)
    {
        return Mix(stateSeed ^ SecondMultiplier);
    }

    /// <summary>
    /// Runs the whole derivation for one registered stream and initializes it per the
    /// initialization rule of doc 20 § Authoritative random-number contract.
    /// </summary>
    /// <param name="schemaVersion">The random schema version.</param>
    /// <param name="masterSeed">The deployment master seed.</param>
    /// <param name="key">The validated stream key.</param>
    /// <returns>A primed stream whose next draw is its first caller-visible value.</returns>
    /// <remarks>
    /// Internal, and it must stay internal: it is the one function that produces a live stream
    /// value, and <see cref="Pcg32"/> is internal precisely so that no caller can hold one and
    /// fork it. Callers draw through <see cref="RandomStreamSet"/>.
    /// </remarks>
    internal static Pcg32 CreateStream(RandomSchemaVersion schemaVersion, ulong masterSeed, RandomStreamKey key)
    {
        ulong d0 = DeriveD0(schemaVersion, masterSeed);
        ulong d1 = DeriveD1(d0, key.FamilyKey);
        ulong stateSeed = DeriveStateSeed(d1, key.InstanceKey);
        ulong selector = DeriveSelector(stateSeed);
        return Pcg32.Initialize(stateSeed, selector);
    }
}
