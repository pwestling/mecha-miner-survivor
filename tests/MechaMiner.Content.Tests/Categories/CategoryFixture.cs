using MechaMiner.Content.Categories;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>One fixture in the category corpus.</summary>
/// <remarks>
/// A fixture with no expected code must produce no diagnostics; one with an expected
/// code must produce that code. Carrying both cases in one type keeps the two lists
/// from drifting into different shapes.
/// </remarks>
internal sealed class CategoryFixture
{
    internal CategoryFixture(string path, DefinitionKind kind, string? expectedCode)
    {
        Path = path;
        Kind = kind;
        ExpectedCode = expectedCode;
    }

    /// <summary>The path beneath the category fixture directory.</summary>
    internal string Path { get; }

    /// <summary>The definition kind whose field table the fixture is read against.</summary>
    internal DefinitionKind Kind { get; }

    /// <summary>The one code this fixture must provoke, or null when it must be clean.</summary>
    internal string? ExpectedCode { get; }

    /// <summary>True when this fixture must validate cleanly.</summary>
    internal bool MustBeValid => ExpectedCode is null;

    /// <summary>Builds the read context for this fixture.</summary>
    internal CategoryReadContext Context()
    {
        return new CategoryReadContext(CategoryFixtureCorpus.SourcePathOf(Path), Kind);
    }

    public override string ToString()
    {
        return ExpectedCode is null ? Path : Path + " -> " + ExpectedCode;
    }
}
