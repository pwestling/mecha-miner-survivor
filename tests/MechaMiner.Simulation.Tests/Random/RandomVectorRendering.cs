using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Renders the data body of each committed golden vector file from any
/// <see cref="IRandomVectorEngine"/>.
/// </summary>
/// <remarks>
/// <para>
/// The text layout lives here once so the production engine and the independent reference
/// engine cannot differ over formatting instead of arithmetic. Every number in every line comes
/// from the engine.
/// </para>
/// <para>
/// The inputs - which master seeds, which families, which instance keys, which bounds, which
/// selection cases - are the fixtures' own inputs, stated in the golden headers. They are
/// transcribed here as literals rather than read out of the golden, because reading the inputs
/// from the file under test would make the comparison partly self-satisfying.
/// </para>
/// <para>
/// Canonical candidate <em>order</em> for the selection fixture is applied here rather than by
/// an engine. Ordering is a domain rule (doc 20 § Authoritative random-number contract:
/// canonical manifest/order rules), not generator arithmetic; what each engine supplies is the
/// zero-draw rule and the index draw. Production's own ordering is asserted separately by
/// <c>VER-SIM-005-009</c>.
/// </para>
/// </remarks>
internal static class RandomVectorRendering
{
    /// <summary>The golden that pins the derivation chain of doc 20 § Authoritative
    /// random-number contract.</summary>
    internal const string SeedDerivationGolden = "random-seed-derivation.txt";

    /// <summary>The golden that pins initialization and the first outputs (doc 20 §
    /// Authoritative random-number contract, :55).</summary>
    internal const string StreamInitializationGolden = "random-stream-initialization.txt";

    /// <summary>The golden that pins bounded conversion and its consumed draws (doc 20 §
    /// Authoritative random-number contract).</summary>
    internal const string BoundedConversionGolden = "random-bounded-conversion.txt";

    /// <summary>The golden that pins the 53-bit unit-interval conversion (doc 20 §
    /// Authoritative random-number contract).</summary>
    internal const string UnitDoubleConversionGolden = "random-unit-double-conversion.txt";

    /// <summary>The golden that pins all 23 registered families (doc 20 § Authoritative
    /// random-number contract).</summary>
    internal const string StreamIndependenceGolden = "random-stream-independence.txt";

    /// <summary>The golden that pins the zero-draw selection rule (doc 20 § Authoritative
    /// random-number contract).</summary>
    internal const string DegenerateSelectionGolden = "random-degenerate-selection.txt";

    /// <summary>The master seed the independence, bounded, double, and selection fixtures
    /// use.</summary>
    internal const ulong FixtureMasterSeed = 0x0123456789ABCDEFUL;

    /// <summary>How many values each fixture block records.</summary>
    private const int BlockLength = 16;

    private static readonly (ulong Master, ushort Family, ulong Instance)[] DerivationTriples =
    {
        (0x0000000000000000UL, 0x0100, 0x0000000000000000UL),
        (0x0000000000000000UL, 0x0220, 0x0000000000000005UL),
        (0xFFFFFFFFFFFFFFFFUL, 0x0100, 0x0000000000000000UL),
        (0xFFFFFFFFFFFFFFFFUL, 0x0410, 0xFFFFFFFFFFFFFFFFUL),
        (0x000000000000002AUL, 0xF000, 0x0000000000000000UL),
        (0x0123456789ABCDEFUL, 0x0100, 0x0000000000000000UL),
        (0x0123456789ABCDEFUL, 0x0202, 0x0000000000000001UL),
        (0x0123456789ABCDEFUL, 0x0303, 0x0000000000000003UL),
        (0x0123456789ABCDEFUL, 0x0250, 0x00000000DEADBEEFUL),
    };

    private static readonly (ulong Master, ushort Family, ulong Instance)[] InitializationStreams =
    {
        (0x0123456789ABCDEFUL, 0x0100, 0x0000000000000000UL),
        (0x0000000000000000UL, 0x0200, 0x0000000000000000UL),
        (0xFFFFFFFFFFFFFFFFUL, 0x0300, 0x0000000000000000UL),
        (0x0123456789ABCDEFUL, 0x0220, 0x0000000000000003UL),
        (0x0123456789ABCDEFUL, 0x0220, 0x0000000000000004UL),
    };

    private static readonly uint[] ConversionBounds =
    {
        1U, 2U, 3U, 6U, 10U, 52U, 100U, 1000U, 3221225472U, 4294967295U,
    };

    private static readonly SelectionCase[] SelectionCases =
    {
        new("empty", Array.Empty<string>(), StringComparer.Ordinal),
        new("singleton", new[] { "MCH-01" }, StringComparer.Ordinal),
        new("pair, authored order reversed", new[] { "W-BD", "W-AB" }, StringComparer.Ordinal),
        new(
            "triple, authored order unsorted",
            new[] { "REL-01", "BOSS-01", "EN-01" },
            StringComparer.Ordinal),
        new("singleton integer", new[] { "7" }, AscendingIntegerComparer.Instance),
        new(
            "four integers, authored order unsorted",
            new[] { "9", "3", "5", "1" },
            AscendingIntegerComparer.Instance),
    };

    /// <summary>The stream key the bounded, double, and independence-row fixtures draw
    /// from.</summary>
    internal static (ulong Master, ushort Family, ulong Instance) ConversionStream =>
        (FixtureMasterSeed, 0x0100, 0x0000000000000000UL);

    /// <summary>The stream key the selection fixture draws from.</summary>
    internal static (ulong Master, ushort Family, ulong Instance) SelectionStream =>
        (FixtureMasterSeed, 0x0205, 0x0000000000000000UL);

    /// <summary>Renders the body of <c>random-seed-derivation.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string SeedDerivationBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        StringBuilder text = new();
        foreach ((ulong master, ushort family, ulong instance) in DerivationTriples)
        {
            RandomDerivationVector derived = engine.Derive(master, family, instance);
            text.Append(Hex16(master)).Append('\t')
                .Append(Hex4(family)).Append('\t')
                .Append(Hex16(instance)).Append('\t')
                .Append(Hex16(derived.D0)).Append('\t')
                .Append(Hex16(derived.D1)).Append('\t')
                .Append(Hex16(derived.StateSeed)).Append('\t')
                .Append(Hex16(derived.Selector)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Renders the body of <c>random-stream-initialization.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string StreamInitializationBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        StringBuilder text = new();
        foreach ((ulong master, ushort family, ulong instance) in InitializationStreams)
        {
            RandomDerivationVector derived = engine.Derive(master, family, instance);
            IRandomVectorStream stream = engine.OpenStream(master, family, instance);

            text.Append('\n');
            text.Append("stream\tmaster=").Append(Hex16(master))
                .Append("\tfamily=").Append(Hex4(family))
                .Append("\tinstance=").Append(Hex16(instance)).Append('\n');
            text.Append("state-seed\t").Append(Hex16(derived.StateSeed)).Append('\n');
            text.Append("selector\t").Append(Hex16(derived.Selector)).Append('\n');
            text.Append("increment\t").Append(Hex16(stream.Increment)).Append('\n');
            text.Append("primed-state\t").Append(Hex16(stream.State)).Append('\n');
            for (int index = 0; index < BlockLength; index++)
            {
                text.Append("output\t").Append(Index2(index)).Append('\t')
                    .Append(Hex8(stream.NextUInt32())).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>Renders the body of <c>random-bounded-conversion.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string BoundedConversionBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        (ulong master, ushort family, ulong instance) = ConversionStream;
        StringBuilder text = new();
        foreach (uint bound in ConversionBounds)
        {
            IRandomVectorStream stream = engine.OpenStream(master, family, instance);
            uint threshold = RejectionThreshold(bound);

            text.Append('\n');
            text.Append("bound\t").Append(DecimalText(bound)).Append('\t').Append(Hex8(bound))
                .Append("\tthreshold\t").Append(DecimalText(threshold)).Append('\t').Append(Hex8(threshold))
                .Append('\n');
            for (int index = 0; index < BlockLength; index++)
            {
                ulong before = stream.DrawCount;
                uint result = stream.NextBounded(bound);
                ulong consumed = stream.DrawCount - before;
                text.Append("result\t").Append(Index2(index)).Append('\t')
                    .Append(DecimalText(result)).Append('\t')
                    .Append(consumed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            text.Append("total-draws\t")
                .Append(stream.DrawCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Renders the body of <c>random-unit-double-conversion.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string UnitDoubleConversionBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        (ulong master, ushort family, ulong instance) = ConversionStream;
        IRandomVectorStream stream = engine.OpenStream(master, family, instance);
        StringBuilder text = new();
        for (int index = 0; index < BlockLength; index++)
        {
            double value = stream.NextUnitDouble();
            text.Append(Index2(index)).Append('\t')
                .Append(Hex14(MantissaOf(value))).Append('\t')
                .Append(Hex16(BitConverter.DoubleToUInt64Bits(value))).Append('\t')
                .Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Renders the body of <c>random-stream-independence.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string StreamIndependenceBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        StringBuilder text = new();
        foreach (KeyValuePair<ushort, string> family in engine.Families)
        {
            RandomDerivationVector derived = engine.Derive(FixtureMasterSeed, family.Key, 0UL);
            IRandomVectorStream stream = engine.OpenStream(FixtureMasterSeed, family.Key, 0UL);
            text.Append(Hex4(family.Key)).Append('\t')
                .Append(Hex16(derived.StateSeed)).Append('\t')
                .Append(Hex16(derived.Selector)).Append('\t')
                .Append(Hex16(stream.Increment)).Append('\t')
                .Append(Hex16(stream.State)).Append('\t')
                .Append(Hex8(stream.NextUInt32())).Append('\t')
                .Append(family.Value).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Renders the body of <c>random-degenerate-selection.txt</c>.</summary>
    /// <param name="engine">The implementation to compute with.</param>
    /// <returns>The body text.</returns>
    internal static string DegenerateSelectionBody(IRandomVectorEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        (ulong master, ushort family, ulong instance) = SelectionStream;
        StringBuilder text = new();
        foreach (SelectionCase selectionCase in SelectionCases)
        {
            IRandomVectorStream stream = engine.OpenStream(master, family, instance);
            string[] ordered = selectionCase.CanonicalOrder();
            ulong stateBefore = stream.State;
            ulong drawsBefore = stream.DrawCount;
            bool selected = stream.TrySelectIndex(ordered.Length, out int selectedIndex);
            ulong stateAfter = stream.State;
            ulong consumed = stream.DrawCount - drawsBefore;

            text.Append('\n');
            text.Append("case\t").Append(selectionCase.Name).Append('\n');
            text.Append("candidates\t").Append(DecimalText((uint)selectionCase.Authored.Count)).Append('\t')
                .Append(RenderList(selectionCase.Authored)).Append('\n');
            text.Append("ordered\t").Append(RenderList(ordered)).Append('\n');
            text.Append("draws\t").Append(consumed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("state-before\t").Append(Hex16(stateBefore)).Append('\n');
            text.Append("state-after\t").Append(Hex16(stateAfter)).Append('\t')
                .Append(stateAfter == stateBefore ? "unchanged" : "advanced").Append('\n');
            text.Append("selected\t").Append(selected ? ordered[selectedIndex] : "<none>").Append('\n');
        }

        return text.ToString();
    }

    /// <summary>The rejection threshold of doc 20 § Authoritative random-number contract, for
    /// the fixture's own header column.</summary>
    /// <param name="bound">The exclusive upper bound.</param>
    /// <returns>The threshold.</returns>
    internal static uint RejectionThreshold(uint bound)
    {
        return (uint)((4294967296UL - bound) % bound);
    }

    /// <summary>Recovers the 53-bit mantissa a unit-interval double encodes.</summary>
    /// <param name="value">A value produced by the conversion of doc 20 § Authoritative
    /// random-number contract.</param>
    /// <returns>The mantissa.</returns>
    /// <remarks>
    /// Exact: the value is <c>m53 / 2^53</c>, so scaling by <c>2^53</c> is a power-of-two
    /// scaling with no rounding. Recovering the mantissa from the double, rather than taking it
    /// from the conversion, is what makes the fixture's mantissa column evidence that the
    /// double really carries those 53 bits.
    /// </remarks>
    internal static ulong MantissaOf(double value)
    {
        return (ulong)(value * 9007199254740992.0);
    }

    private static string RenderList(IReadOnlyList<string> items)
    {
        StringBuilder text = new();
        text.Append('[');
        for (int index = 0; index < items.Count; index++)
        {
            if (index > 0)
            {
                text.Append(", ");
            }

            text.Append(items[index]);
        }

        return text.Append(']').ToString();
    }

    private static string Hex4(ushort value)
    {
        return "0x" + value.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static string Hex8(uint value)
    {
        return "0x" + value.ToString("X8", CultureInfo.InvariantCulture);
    }

    private static string Hex14(ulong value)
    {
        return "0x" + value.ToString("X14", CultureInfo.InvariantCulture);
    }

    private static string Hex16(ulong value)
    {
        return "0x" + value.ToString("X16", CultureInfo.InvariantCulture);
    }

    private static string Index2(int index)
    {
        return index.ToString("D2", CultureInfo.InvariantCulture);
    }

    private static string DecimalText(uint value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>One selection case of the degenerate-selection fixture.</summary>
    private sealed class SelectionCase
    {
        internal SelectionCase(string name, string[] authored, IComparer<string> canonicalOrder)
        {
            this.Name = name;
            this.Authored = authored;
            this.Comparer = canonicalOrder;
        }

        internal string Name { get; }

        internal IReadOnlyList<string> Authored { get; }

        private IComparer<string> Comparer { get; }

        /// <summary>The candidates in canonical order, which is never authored order
        /// here.</summary>
        internal string[] CanonicalOrder()
        {
            string[] ordered = new string[this.Authored.Count];
            for (int index = 0; index < this.Authored.Count; index++)
            {
                ordered[index] = this.Authored[index];
            }

            Array.Sort(ordered, this.Comparer);
            return ordered;
        }
    }

    /// <summary>
    /// Canonical order for the fixture's integer cases: numeric ascending, so
    /// <c>9</c> sorts after <c>10</c> would and lexical order cannot be mistaken for it.
    /// </summary>
    private sealed class AscendingIntegerComparer : IComparer<string>
    {
        internal static readonly AscendingIntegerComparer Instance = new();

        public int Compare(string? left, string? right)
        {
            int leftValue = int.Parse(left!, CultureInfo.InvariantCulture);
            int rightValue = int.Parse(right!, CultureInfo.InvariantCulture);
            return leftValue.CompareTo(rightValue);
        }
    }
}
