namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Every constant and structural decision of doc 20 § Authoritative random-number contract, as
/// data the independent reference implementation reads instead of hard-coding.
/// </summary>
/// <remarks>
/// <para>
/// The canonical instance is the specification. The point of making the constants data is the
/// negative control: <c>VER-SIM-005-016</c> has to prove that the golden gates actually fail
/// when a single constant is wrong, and it can only do that by running a deliberately wrong
/// implementation. Mutating the <em>reference</em> keeps that proof inside a passing test suite
/// - no gate is ever disabled, and no production constant is touched, which doc 114 § Failure
/// and retry policy would forbid.
/// </para>
/// <para>
/// Each mutation flag corresponds to a transcription error a reader of doc 20 § Authoritative
/// random-number contract could plausibly make: the multiplier off by one bit, a shift off by
/// one, the rotation in the wrong direction, the output taken after the advance instead of
/// before, the increment's mandatory low bit dropped, modulo reduction in place of rejection
/// sampling, the two halves of the 53-bit pair swapped, a draw consumed by a singleton
/// selection, or the family key left out of the derivation so every family shares one sequence.
/// </para>
/// </remarks>
internal sealed class ReferenceRandomConstants
{
    /// <summary>The specification exactly as the presentation-isolation rule of doc 20 §
    /// Authoritative random-number contract states it.</summary>
    internal static readonly ReferenceRandomConstants Canonical = new();

    /// <summary>What this constant set is, for assertion messages.</summary>
    internal string Description { get; init; } = "canonical";

    /// <summary>The LCG multiplier of doc 20 § Authoritative random-number contract.</summary>
    internal ulong LcgMultiplier { get; init; } = 6364136223846793005UL;

    /// <summary>The first output xorshift distance of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal int FirstOutputXorShift { get; init; } = 18;

    /// <summary>The second output xorshift distance of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal int SecondOutputXorShift { get; init; } = 27;

    /// <summary>The shift that selects the prior state's top five bits (doc 20 § Authoritative
    /// random-number contract).</summary>
    internal int RotationSelectorShift { get; init; } = 59;

    /// <summary>Rotates left instead of right, inverting the generator of doc 20 §
    /// Authoritative random-number contract's XSH-RR rotation.</summary>
    internal bool RotateLeftInsteadOfRight { get; init; }

    /// <summary>Takes the output from the state after the advance instead of before
    /// it.</summary>
    internal bool OutputAfterAdvance { get; init; }

    /// <summary>Drops the mandatory low bit of the increment (doc 20 § Authoritative
    /// random-number contract).</summary>
    internal bool ForceEvenIncrement { get; init; }

    /// <summary>The SplitMix64 gamma of doc 20 § Authoritative random-number contract, also the
    /// instance-key multiplier.</summary>
    internal ulong MixGamma { get; init; } = 0x9E3779B97F4A7C15UL;

    /// <summary>The first SplitMix64 multiplier of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal ulong MixFirstMultiplier { get; init; } = 0xBF58476D1CE4E5B9UL;

    /// <summary>The second SplitMix64 multiplier of doc 20 § Authoritative random-number
    /// contract, also the selector salt.</summary>
    internal ulong MixSecondMultiplier { get; init; } = 0x94D049BB133111EBUL;

    /// <summary>The first <c>Mix</c> xor-shift distance of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal int MixFirstShift { get; init; } = 30;

    /// <summary>The second <c>Mix</c> xor-shift distance of doc 20 § Authoritative
    /// random-number contract.</summary>
    internal int MixSecondShift { get; init; } = 27;

    /// <summary>The third <c>Mix</c> xor-shift distance of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal int MixThirdShift { get; init; } = 31;

    /// <summary>The schema-version multiplier of doc 20 § Authoritative random-number
    /// contract.</summary>
    internal ulong SchemaVersionMultiplier { get; init; } = 0xD1B54A32D192ED03UL;

    /// <summary>Leaves the family key out of the derivation, so all families collide.</summary>
    internal bool IgnoreFamilyKey { get; init; }

    /// <summary>Reduces by modulo instead of rejection sampling, which the bounded-integer rule
    /// of doc 20 § Authoritative random-number contract forbids.</summary>
    internal bool UseModuloInsteadOfRejection { get; init; }

    /// <summary>Puts the second draw in the high half of the 53-bit pair.</summary>
    internal bool SecondDrawIsHighHalf { get; init; }

    /// <summary>Consumes a draw for a singleton selection, which the selection rules of doc 20
    /// § Authoritative random-number contract forbids.</summary>
    internal bool SingletonSelectionDrawsAnIndex { get; init; }
}
