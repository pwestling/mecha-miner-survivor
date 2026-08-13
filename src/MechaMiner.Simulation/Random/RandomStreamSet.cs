using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// Derives and owns every instantiated authoritative stream of one run, from a master seed and
/// a random schema version.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// derivation chain: "A deployment master seed is one unsigned 64-bit value"; the
/// key-registration rule: "A category retry or an added visual draw cannot consume another
/// family's sequence"; the recovery rule: "Stream state and odd increment are included in run
/// recovery for every instantiated authoritative stream"; the injection rule: "production
/// content cannot select an alternate algorithm".
/// </para>
/// <para>
/// <b>Streams are held in a <see cref="Pcg32"/>[] and mutated in place.</b> A
/// <see cref="Pcg32"/> is a mutable struct whose identity is its advancing state, so copying
/// one into a local forks the stream: the copy and the original then replay the same values,
/// and two families that should be independent would silently share a sequence. An array
/// element is a variable, so <c>_streams[index].NextUInt32()</c> advances the stored stream
/// rather than a copy. A <c>List&lt;Pcg32&gt;</c> would not do this — its indexer returns a
/// copy — and is deliberately not used.
/// <c>VER-SIM-005-011</c> exists to catch a fork.
/// </para>
/// <para>
/// This type is the only production holder of a <see cref="Pcg32"/>. Consumers receive an
/// <see cref="IRandomSource"/> from <see cref="Source"/> and therefore never hold a stream
/// value they could copy. There is no constructor, property, or method here that accepts a
/// generator, an algorithm, or a source: the injection rule of doc 20 § Authoritative
/// random-number contract's "production content cannot select an alternate algorithm" is a
/// property of this surface, asserted by
/// <c>VER-SIM-005-015</c>.
/// </para>
/// <para>
/// Streams are instantiated on first use, because doc 20 § Authoritative random-number contract
/// scopes recovery to every
/// <em>instantiated</em> stream. Instantiating all 23 families eagerly would put streams a run
/// never used into its recovery artifact.
/// </para>
/// </remarks>
public sealed class RandomStreamSet
{
    private readonly List<RandomStreamKey> _keys = new();
    private readonly Dictionary<RandomStreamKey, int> _indices = new();
    private readonly List<StreamCursor> _cursors = new();
    private Pcg32[] _streams = Array.Empty<Pcg32>();

    /// <summary>Creates an empty stream set for one run.</summary>
    /// <param name="schemaVersion">The random schema version to derive under.</param>
    /// <param name="masterSeed">The deployment master seed (doc 20 § Authoritative
    /// random-number contract).</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="schemaVersion"/> is the uninitialized default.
    /// </exception>
    public RandomStreamSet(RandomSchemaVersion schemaVersion, ulong masterSeed)
    {
        if (!schemaVersion.IsSpecified)
        {
            throw new ArgumentException(
                "a stream set derives under an explicit random schema version (doc 20 § Authoritative random-number contract); "
                    + "the uninitialized default is not one",
                nameof(schemaVersion));
        }

        this.SchemaVersion = schemaVersion;
        this.MasterSeed = masterSeed;
    }

    /// <summary>The random schema version every stream in this set was derived under.</summary>
    public RandomSchemaVersion SchemaVersion { get; }

    /// <summary>The deployment master seed every stream in this set was derived from.</summary>
    public ulong MasterSeed { get; }

    /// <summary>
    /// The keys of the streams instantiated so far, in the order they were first used.
    /// </summary>
    public IReadOnlyList<RandomStreamKey> InstantiatedKeys => this._keys;

    /// <summary>
    /// Restores a stream set from a recovery artifact (doc 20 § Authoritative random-number
    /// contract), refusing an artifact written under a different random schema version.
    /// </summary>
    /// <param name="schemaVersion">The schema version the running build derives under.</param>
    /// <param name="masterSeed">The run's master seed.</param>
    /// <param name="states">The per-stream recovery records.</param>
    /// <returns>A stream set whose streams continue the recovered sequences exactly.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="states"/> is null.</exception>
    /// <exception cref="InvalidOperationException"> A record carries a different schema
    /// version, names the presentation-only family, or repeats a stream key. Doc 20 §
    /// Authoritative random-number contract requires incompatible recovery to be invalidated
    /// "rather than silently changing a compatible run".
    /// </exception>
    public static RandomStreamSet Restore(
        RandomSchemaVersion schemaVersion,
        ulong masterSeed,
        IReadOnlyList<RandomStreamRecoveryState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        RandomStreamSet restored = new(schemaVersion, masterSeed);
        for (int index = 0; index < states.Count; index++)
        {
            RandomStreamRecoveryState state = states[index];
            if (state.SchemaVersion != schemaVersion)
            {
                throw new InvalidOperationException(
                    "recovery artifact was written under random schema version "
                        + state.SchemaVersion.ToString()
                        + " but this build derives under version " + schemaVersion.ToString()
                        + ". doc 20 § Authoritative random-number contract: changing any operation increments the random schema version and "
                        + "invalidates incompatible recovery rather than silently changing a compatible run");
            }

            RandomStreamKey key = RandomStreamKey.Create(state.FamilyKey, state.InstanceKey);
            if (!key.Family.IsAuthoritative)
            {
                throw new InvalidOperationException(
                    "recovery artifact contains the presentation-only family "
                        + key.Family.ToString()
                        + ". doc 20 § Authoritative random-number contract: presentation variation is never serialized into authoritative state, "
                        + "and doc 20 § Authoritative random-number contract: no presentation draw or state is read by simulation");
            }

            if (restored._indices.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    "recovery artifact contains stream " + key.ToString()
                        + " more than once; one instantiated stream has exactly one state");
            }

            _ = restored.Add(key, Pcg32.Restore(state.State, state.Increment));
        }

        return restored;
    }

    /// <summary>
    /// Returns the draw source for one registered stream, instantiating the stream on first
    /// use.
    /// </summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns> The stream's source. Repeated calls for the same key return the same source,
    /// because there is exactly one stream per key and two sources over one stream would be two
    /// views of the same advancing state, not two streams.
    /// </returns>
    public IRandomSource Source(RandomStreamKey key)
    {
        int index = this.IndexOf(key);
        return this._cursors[index];
    }

    /// <summary>Draws the next raw 32-bit value from one stream.</summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns>The draw.</returns>
    public uint NextUInt32(RandomStreamKey key)
    {
        int index = this.IndexOf(key);
        return this.DrawAt(index);
    }

    /// <summary>Draws an unbiased bounded integer from one stream (doc 20 § Authoritative
    /// random-number contract).</summary>
    /// <param name="key">The validated stream key.</param>
    /// <param name="bound">The exclusive upper bound, at least one.</param>
    /// <returns>A value in <c>[0, bound)</c>.</returns>
    public uint NextBounded(RandomStreamKey key, uint bound)
    {
        return BoundedRandom.NextBounded(this.Source(key), bound);
    }

    /// <summary>Draws a <c>[0,1)</c> double from one stream (doc 20 § Authoritative
    /// random-number contract).</summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns>A double in <c>[0,1)</c>.</returns>
    public double NextUnitDouble(RandomStreamKey key)
    {
        return BoundedRandom.NextUnitDouble(this.Source(key));
    }

    /// <summary>Resolves an integer-ratio chance from one stream (doc 20 § Authoritative
    /// random-number contract).</summary>
    /// <param name="key">The validated stream key.</param>
    /// <param name="numerator">The favourable count.</param>
    /// <param name="denominator">The total count.</param>
    /// <returns><see langword="true"/> when the chance succeeds.</returns>
    public bool NextChance(RandomStreamKey key, uint numerator, uint denominator)
    {
        return BoundedRandom.NextChance(this.Source(key), numerator, denominator);
    }

    /// <summary>The current state of one stream, instantiating it on first use.</summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns>The 64-bit state the next draw will read.</returns>
    public ulong StateOf(RandomStreamKey key)
    {
        // The index is resolved into a local first, deliberately. In `_streams[IndexOf(key)]`
        // C# evaluates the array reference before the index, so instantiating a stream inside
        // IndexOf - which may grow the array - would read the pre-growth array.
        int index = this.IndexOf(key);
        return this._streams[index].State;
    }

    /// <summary>The odd increment of one stream, instantiating it on first use.</summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns>The odd increment.</returns>
    public ulong IncrementOf(RandomStreamKey key)
    {
        int index = this.IndexOf(key);
        return this._streams[index].Increment;
    }

    /// <summary>How many caller-visible draws one stream has produced.</summary>
    /// <param name="key">The validated stream key.</param>
    /// <returns>The consumed-draw count.</returns>
    public ulong DrawCountOf(RandomStreamKey key)
    {
        int index = this.IndexOf(key);
        return this._streams[index].DrawCount;
    }

    /// <summary>Whether a stream has been instantiated for <paramref name="key"/>.</summary>
    /// <param name="key">The stream key.</param>
    /// <returns><see langword="true"/> when the stream exists in this set.</returns>
    public bool IsInstantiated(RandomStreamKey key)
    {
        return this._indices.ContainsKey(key);
    }

    /// <summary>
    /// Captures the recovery state of every instantiated authoritative stream (doc 20 §
    /// Authoritative random-number contract).
    /// </summary>
    /// <returns> One record per instantiated authoritative stream, ordered by family key then
    /// instance key so the artifact is canonical and reviewable.
    /// </returns>
    /// <remarks>
    /// The presentation-only family is excluded: the family table of doc 20 § Authoritative
    /// random-number contract says presentation variation is "never serialized into
    /// authoritative state". It is skipped here rather than filtered by the caller, so no
    /// caller can serialize it by forgetting to.
    /// </remarks>
    public IReadOnlyList<RandomStreamRecoveryState> CaptureRecoveryState()
    {
        List<RandomStreamRecoveryState> captured = new(this._keys.Count);
        for (int index = 0; index < this._keys.Count; index++)
        {
            RandomStreamKey key = this._keys[index];
            if (!key.Family.IsAuthoritative)
            {
                continue;
            }

            captured.Add(new RandomStreamRecoveryState(
                this.SchemaVersion,
                key.FamilyKey,
                key.InstanceKey,
                this._streams[index].State,
                this._streams[index].Increment));
        }

        captured.Sort(CompareRecoveryOrder);
        return captured;
    }

    private static int CompareRecoveryOrder(RandomStreamRecoveryState left, RandomStreamRecoveryState right)
    {
        int byFamily = left.FamilyKey.CompareTo(right.FamilyKey);
        return byFamily != 0 ? byFamily : left.InstanceKey.CompareTo(right.InstanceKey);
    }

    private int IndexOf(RandomStreamKey key)
    {
        if (this._indices.TryGetValue(key, out int existing))
        {
            return existing;
        }

        if (!key.Family.IsRegistered)
        {
            throw new ArgumentOutOfRangeException(
                nameof(key),
                key.FamilyKey,
                "a stream is only derived for a registered family (doc 20 § Authoritative random-number contract)");
        }

        return this.Add(key, SeedDerivation.CreateStream(this.SchemaVersion, this.MasterSeed, key));
    }

    private int Add(RandomStreamKey key, Pcg32 stream)
    {
        int index = this._keys.Count;
        if (index >= this._streams.Length)
        {
            int capacity = this._streams.Length == 0 ? 8 : this._streams.Length * 2;
            Array.Resize(ref this._streams, capacity);
        }

        this._streams[index] = stream;
        this._keys.Add(key);
        this._indices.Add(key, index);
        this._cursors.Add(new StreamCursor(this, index));
        return index;
    }

    private uint DrawAt(int index)
    {
        ref Pcg32 stream = ref this._streams[index];
        return stream.NextUInt32();
    }

    /// <summary>
    /// The one production <see cref="IRandomSource"/>: a view onto one array element of the
    /// owning set, never a copy of it.
    /// </summary>
    /// <remarks>
    /// It holds an index rather than the stream, so the set may grow its backing array without
    /// any live source becoming a stale copy. That is the whole reason this type exists instead
    /// of handing out the struct.
    /// </remarks>
    private sealed class StreamCursor : IRandomSource
    {
        private readonly RandomStreamSet _owner;
        private readonly int _index;

        internal StreamCursor(RandomStreamSet owner, int index)
        {
            this._owner = owner;
            this._index = index;
        }

        public ulong DrawCount => this._owner._streams[this._index].DrawCount;

        public uint NextUInt32()
        {
            return this._owner.DrawAt(this._index);
        }

        public override string ToString()
        {
            return "stream " + this._owner._keys[this._index].ToString()
                + " index " + this._index.ToString(CultureInfo.InvariantCulture);
        }
    }
}
