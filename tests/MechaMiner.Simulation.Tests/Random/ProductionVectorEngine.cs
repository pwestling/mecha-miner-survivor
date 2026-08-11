using System.Collections.Generic;
using MechaMiner.Simulation.Random;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The golden-vector engine backed by the production types under test.
/// </summary>
/// <remarks>
/// Every stream is opened through a real <see cref="RandomStreamSet"/> rather than by
/// constructing a <see cref="Pcg32"/> directly, so the vectors are produced by the code path
/// production actually uses - including the array-held stream and the
/// <see cref="IRandomSource"/> view that must not fork it (<c>VER-SIM-005-011</c>).
/// </remarks>
internal sealed class ProductionVectorEngine : IRandomVectorEngine
{
    /// <summary>The engine every golden test renders its expected text with.</summary>
    internal static readonly ProductionVectorEngine Instance = new();

    private static readonly KeyValuePair<ushort, string>[] FamilyRows = BuildFamilyRows();

    /// <inheritdoc/>
    public string Name => "production";

    /// <inheritdoc/>
    public IReadOnlyList<KeyValuePair<ushort, string>> Families => FamilyRows;

    /// <inheritdoc/>
    public RandomDerivationVector Derive(ulong masterSeed, ushort familyKey, ulong instanceKey)
    {
        ulong d0 = SeedDerivation.DeriveD0(RandomSchemaVersion.Current, masterSeed);
        ulong d1 = SeedDerivation.DeriveD1(d0, familyKey);
        ulong stateSeed = SeedDerivation.DeriveStateSeed(d1, instanceKey);
        ulong selector = SeedDerivation.DeriveSelector(stateSeed);
        return new RandomDerivationVector(d0, d1, stateSeed, selector);
    }

    /// <inheritdoc/>
    public IRandomVectorStream OpenStream(ulong masterSeed, ushort familyKey, ulong instanceKey)
    {
        return new ProductionVectorStream(masterSeed, familyKey, instanceKey);
    }

    private static KeyValuePair<ushort, string>[] BuildFamilyRows()
    {
        IReadOnlyList<RandomStreamFamily> registered = RandomStreamFamilies.All;
        KeyValuePair<ushort, string>[] rows = new KeyValuePair<ushort, string>[registered.Count];
        for (int index = 0; index < registered.Count; index++)
        {
            rows[index] = new KeyValuePair<ushort, string>(
                registered[index].Key,
                registered[index].Name ?? string.Empty);
        }

        return rows;
    }

    private sealed class ProductionVectorStream : IRandomVectorStream
    {
        private readonly RandomStreamSet _set;
        private readonly RandomStreamKey _key;
        private readonly IRandomSource _source;

        internal ProductionVectorStream(ulong masterSeed, ushort familyKey, ulong instanceKey)
        {
            this._set = new RandomStreamSet(RandomSchemaVersion.Current, masterSeed);
            this._key = RandomStreamKey.Create(familyKey, instanceKey);
            this._source = this._set.Source(this._key);
        }

        public ulong Increment => this._set.IncrementOf(this._key);

        public ulong State => this._set.StateOf(this._key);

        public ulong DrawCount => this._set.DrawCountOf(this._key);

        public uint NextUInt32()
        {
            return this._source.NextUInt32();
        }

        public uint NextBounded(uint bound)
        {
            return BoundedRandom.NextBounded(this._source, bound);
        }

        public double NextUnitDouble()
        {
            return BoundedRandom.NextUnitDouble(this._source);
        }

        public bool TrySelectIndex(int candidateCount, out int selectedIndex)
        {
            int[] canonicalIndices = new int[candidateCount];
            for (int index = 0; index < candidateCount; index++)
            {
                canonicalIndices[index] = index;
            }

            return CanonicalSelection.TrySelectFromCanonicalOrder(
                this._source,
                canonicalIndices,
                out selectedIndex);
        }
    }
}
