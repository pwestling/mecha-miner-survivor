using System;

namespace MechaMiner.Content.Envelope;

/// <summary>One step of a <c>source_refs</c> scope path.</summary>
/// <remarks>
/// A step is either a named object member or one of three ways of selecting array
/// elements. The four forms are kept distinct rather than normalized to a single
/// "index" because a diagnostic that says "<c>rules[2..3]</c> selects elements 2 and 3,
/// and the array has 2 elements" is actionable, whereas one that says
/// "<c>/rules/3</c> is missing" is not obviously about a range.
/// </remarks>
public sealed class SourceRefScopeStep
{
    private SourceRefScopeStep(
        SourceRefScopeStepKind kind,
        string? name,
        int lowIndex,
        int highIndex)
    {
        Kind = kind;
        Name = name;
        LowIndex = lowIndex;
        HighIndex = highIndex;
    }

    /// <summary>Which of the four forms this step is.</summary>
    public SourceRefScopeStepKind Kind { get; }

    /// <summary>The member name, for <see cref="SourceRefScopeStepKind.Member"/>.</summary>
    public string? Name { get; }

    /// <summary>The lowest selected index, for an explicit index or range.</summary>
    public int LowIndex { get; }

    /// <summary>The highest selected index, for an explicit index or range.</summary>
    public int HighIndex { get; }

    /// <summary>A named object member, <c>segment</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public static SourceRefScopeStep Member(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new SourceRefScopeStep(SourceRefScopeStepKind.Member, name, 0, 0);
    }

    /// <summary>Every element of an array, <c>[]</c>.</summary>
    public static SourceRefScopeStep AnyIndex()
    {
        return new SourceRefScopeStep(SourceRefScopeStepKind.AnyIndex, null, 0, 0);
    }

    /// <summary>One element of an array, <c>[n]</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public static SourceRefScopeStep Index(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return new SourceRefScopeStep(SourceRefScopeStepKind.Index, null, index, index);
    }

    /// <summary>A contiguous run of elements, <c>[low..high]</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An endpoint is negative or <paramref name="high"/> precedes <paramref name="low"/>.
    /// </exception>
    public static SourceRefScopeStep Range(int low, int high)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(low);
        ArgumentOutOfRangeException.ThrowIfLessThan(high, low);
        return new SourceRefScopeStep(SourceRefScopeStepKind.Range, null, low, high);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind switch
        {
            SourceRefScopeStepKind.Member => Name ?? string.Empty,
            SourceRefScopeStepKind.AnyIndex => "[]",
            SourceRefScopeStepKind.Index => "[" + LowIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "]",
            _ => "[" + LowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".."
                + HighIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
        };
    }
}
