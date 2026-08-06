namespace MechaMiner.Content.Envelope;

/// <summary>The four forms a <c>source_refs</c> scope step can take.</summary>
public enum SourceRefScopeStepKind
{
    /// <summary>A named object member, <c>segment</c>.</summary>
    Member = 0,

    /// <summary>Every element of an array, <c>[]</c>.</summary>
    AnyIndex = 1,

    /// <summary>One element of an array, <c>[n]</c>.</summary>
    Index = 2,

    /// <summary>A contiguous run of elements, <c>[low..high]</c>.</summary>
    Range = 3,
}
