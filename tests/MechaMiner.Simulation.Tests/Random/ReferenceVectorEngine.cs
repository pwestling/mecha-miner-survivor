using System.Collections.Generic;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// A second, deliberately simple implementation of doc 20 § Authoritative random-number
/// contract, written from the document rather than from the production code.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models: a reference model is
/// "deliberately simple independent logic" for cases where one implementation could repeat its
/// own bug. That is exactly the risk here - every downstream system inherits these numbers, so
/// a wrong shift agreed on by the implementation and its own golden would be invisible.
/// </para>
/// <para>
/// It is independent in the ways that matter and says so explicitly: it computes the rejection
/// threshold as <c>(2^32 - bound) mod bound</c> in 64-bit arithmetic rather than by unsigned
/// negation, rotates by hand rather than through
/// <c>System.Numerics.BitOperations</c>, divides by <c>2^53</c> rather than multiplying by its
/// reciprocal, carries its own transcription of the 23-row family table, and writes the schema
/// version as the literal <c>1</c> of doc 20 § Authoritative random-number contract rather than
/// reading
/// <c>RandomSchemaVersion.Current</c>. It shares no code with
/// <c>MechaMiner.Simulation.Random</c>.
/// </para>
/// <para>
/// Its constants are data (<see cref="ReferenceRandomConstants"/>) so
/// <c>VER-SIM-005-016</c> can run one-bit mutations of it and prove the golden gates fail.
/// </para>
/// </remarks>
internal sealed class ReferenceVectorEngine : IRandomVectorEngine
{
    /// <summary>The random schema version of doc 20 § Authoritative random-number contract,
    /// transcribed as a literal.</summary>
    private const ulong SchemaVersion = 1UL;

    /// <summary><c>2^32</c>, for the rejection threshold of doc 20 § Authoritative
    /// random-number contract.</summary>
    private const ulong TwoToThe32 = 4294967296UL;

    /// <summary><c>2^53</c>, the divisor of the unit-interval conversion of doc 20 §
    /// Authoritative random-number contract.</summary>
    private const double TwoToThe53 = 9007199254740992.0;

    /// <summary>The reference exactly as the presentation-isolation rule of doc 20 §
    /// Authoritative random-number contract specifies it.</summary>
    internal static readonly ReferenceVectorEngine Canonical = new(ReferenceRandomConstants.Canonical);

    /// <summary>
    /// The 23 registered families of doc 20 § Authoritative random-number contract, transcribed
    /// independently of
    /// <c>RandomStreamFamilies</c>.
    /// </summary>
    private static readonly KeyValuePair<ushort, string>[] FamilyRows =
    {
        new(0x0100, "resource-profile selection"),
        new(0x0200, "major topology"),
        new(0x0201, "spatial embedding"),
        new(0x0202, "region recipes"),
        new(0x0203, "landmarks"),
        new(0x0204, "obstacle/dressing placement"),
        new(0x0205, "deployment selection"),
        new(0x0210, "standard-seam placement"),
        new(0x0211, "rich-seam placement"),
        new(0x0220, "material-geode placement"),
        new(0x0230, "Hyper Gold placement"),
        new(0x0240, "relic-cache placement"),
        new(0x0241, "relic assignment"),
        new(0x0250, "dynamic rocks/drop rolls"),
        new(0x0260, "release fallback-manifest selection"),
        new(0x0300, "baseline encounter sectors/composition"),
        new(0x0301, "authored event formations"),
        new(0x0302, "beacon response selection"),
        new(0x0303, "boss entry/ability randomness"),
        new(0x0400, "player weapon combat randomness"),
        new(0x0410, "enemy combat randomness"),
        new(0x0500, "boss/other authorized loot"),
        new(0xF000, "presentation-only variation"),
    };

    private readonly ReferenceRandomConstants _constants;

    /// <summary>Creates a reference engine over one constant set.</summary>
    /// <param name="constants">The constants to compute with.</param>
    internal ReferenceVectorEngine(ReferenceRandomConstants constants)
    {
        this._constants = constants;
    }

    /// <inheritdoc/>
    public string Name => "reference[" + this._constants.Description + "]";

    /// <inheritdoc/>
    public IReadOnlyList<KeyValuePair<ushort, string>> Families => FamilyRows;

    /// <inheritdoc/>
    public RandomDerivationVector Derive(ulong masterSeed, ushort familyKey, ulong instanceKey)
    {
        ulong d0 = this.Mix(masterSeed ^ unchecked(SchemaVersion * this._constants.SchemaVersionMultiplier));
        ulong d1 = this.Mix(d0 ^ (this._constants.IgnoreFamilyKey ? 0UL : familyKey));
        ulong stateSeed = this.Mix(d1 ^ unchecked(instanceKey * this._constants.MixGamma));
        ulong selector = this.Mix(stateSeed ^ this._constants.MixSecondMultiplier);
        return new RandomDerivationVector(d0, d1, stateSeed, selector);
    }

    /// <inheritdoc/>
    public IRandomVectorStream OpenStream(ulong masterSeed, ushort familyKey, ulong instanceKey)
    {
        RandomDerivationVector derived = this.Derive(masterSeed, familyKey, instanceKey);
        return new ReferenceVectorStream(this._constants, derived.StateSeed, derived.Selector);
    }

    /// <summary>
    /// Initializes a stream from an explicit state seed and selector, which is what
    /// <c>pcg32_srandom_r</c>'s <c>initstate</c> and <c>initseq</c> are.
    /// </summary>
    /// <param name="stateSeed">The state seed / <c>initstate</c>.</param>
    /// <param name="selector">The selector / <c>initseq</c>.</param>
    /// <returns>A primed stream.</returns>
    /// <remarks>
    /// Only the reference offers this. Production initializes exclusively from a master seed
    /// and a registered stream key, because a public "seed a stream from these two numbers"
    /// entry point would be an unregistered-randomness path around the family table. The
    /// published 42/54 demo vector needs an arbitrary seed pair, so it is reached here and
    /// production is then driven from the resulting state through run recovery.
    /// </remarks>
    internal IRandomVectorStream OpenFromSeeds(ulong stateSeed, ulong selector)
    {
        return new ReferenceVectorStream(this._constants, stateSeed, selector);
    }

    /// <summary>
    /// The mixing function of the derivation chain: add the gamma, xor-shift 30 and multiply,
    /// xor-shift 27 and multiply, xor-shift 31.
    /// </summary>
    private ulong Mix(ulong value)
    {
        ulong mixed = unchecked(value + this._constants.MixGamma);
        mixed ^= mixed >> this._constants.MixFirstShift;
        mixed = unchecked(mixed * this._constants.MixFirstMultiplier);
        mixed ^= mixed >> this._constants.MixSecondShift;
        mixed = unchecked(mixed * this._constants.MixSecondMultiplier);
        mixed ^= mixed >> this._constants.MixThirdShift;
        return mixed;
    }

    private sealed class ReferenceVectorStream : IRandomVectorStream
    {
        private readonly ReferenceRandomConstants _constants;
        private readonly ulong _increment;
        private ulong _state;
        private ulong _drawCount;

        internal ReferenceVectorStream(ReferenceRandomConstants constants, ulong stateSeed, ulong selector)
        {
            this._constants = constants;
            ulong increment = unchecked((selector << 1) | 1UL);
            this._increment = constants.ForceEvenIncrement ? increment & 0xFFFFFFFFFFFFFFFEUL : increment;
            this._state = 0UL;
            this.Step();
            this._state = unchecked(this._state + stateSeed);
            this.Step();
        }

        public ulong Increment => this._increment;

        public ulong State => this._state;

        public ulong DrawCount => this._drawCount;

        public uint NextUInt32()
        {
            this._drawCount++;
            return this.Step();
        }

        public uint NextBounded(uint bound)
        {
            if (this._constants.UseModuloInsteadOfRejection)
            {
                return this.NextUInt32() % bound;
            }

            uint threshold = (uint)((TwoToThe32 - bound) % bound);
            while (true)
            {
                uint draw = this.NextUInt32();
                if (draw >= threshold)
                {
                    return draw % bound;
                }
            }
        }

        public double NextUnitDouble()
        {
            uint firstDraw = this.NextUInt32();
            uint secondDraw = this.NextUInt32();
            ulong bits = this._constants.SecondDrawIsHighHalf
                ? ((ulong)secondDraw << 32) | firstDraw
                : ((ulong)firstDraw << 32) | secondDraw;
            return (bits >> 11) / TwoToThe53;
        }

        public bool TrySelectIndex(int candidateCount, out int selectedIndex)
        {
            if (candidateCount == 0)
            {
                selectedIndex = -1;
                return false;
            }

            if (candidateCount == 1)
            {
                if (this._constants.SingletonSelectionDrawsAnIndex)
                {
                    this.NextBounded(1U);
                }

                selectedIndex = 0;
                return true;
            }

            selectedIndex = (int)this.NextBounded((uint)candidateCount);
            return true;
        }

        /// <summary>
        /// Advances the state once and returns the output the transformation of doc 20 §
        /// Authoritative random-number contract produces from the state read before the
        /// advance.
        /// </summary>
        private uint Step()
        {
            ulong prior = this._state;
            this._state = unchecked((prior * this._constants.LcgMultiplier) + this._increment);
            ulong outputSource = this._constants.OutputAfterAdvance ? this._state : prior;

            uint xorshifted = (uint)(((outputSource >> this._constants.FirstOutputXorShift) ^ outputSource)
                >> this._constants.SecondOutputXorShift);
            int rotation = (int)(outputSource >> this._constants.RotationSelectorShift) & 31;
            if (rotation == 0)
            {
                return xorshifted;
            }

            return this._constants.RotateLeftInsteadOfRight
                ? (xorshifted << rotation) | (xorshifted >> (32 - rotation))
                : (xorshifted >> rotation) | (xorshifted << (32 - rotation));
        }
    }
}
