using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every category fixture reaches the verdict the corpus declares, and every invalid
/// one reaches it through the diagnostic code it is named for.
/// </summary>
/// <remarks>
/// Verification: <c>VER-DAT-002-008</c> through <c>VER-DAT-002-014</c>,
/// <c>VER-DAT-002-022</c>, <c>VER-DAT-002-027</c>, <c>VER-DAT-002-030</c>,
/// <c>VER-DAT-003-011</c>, <c>VER-DAT-003-026</c>.
/// </remarks>
[TestFixture]
internal sealed class CategoryFixtureCorpusTests
{
    private static IEnumerable<CategoryFixture> ValidCases => CategoryFixtureCorpus.Valid;

    private static IEnumerable<CategoryFixture> CatalogOnlyCases =>
        CategoryFixtureCorpus.CatalogOnly;

    private static IEnumerable<CategoryFixture> InvalidCases => CategoryFixtureCorpus.Invalid;

    /// <summary>
    /// The over-strictness control. Without it the invalid corpus would be satisfied by
    /// a validator that rejects everything, and a field table could quietly require a
    /// field no document asks for.
    /// </summary>
    [TestCaseSource(nameof(ValidCases))]
    public void AValidFixtureProducesNoDiagnostics(CategoryFixture fixture)
    {
        DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);

        Expect.Multiple(() =>
        {
            Assert.That(
                result.Diagnostics,
                Is.Empty,
                () => fixture.Path + " must validate cleanly: "
                    + string.Join("; ", result.Diagnostics));
            Assert.That(result.IsValid, Is.True, fixture.Path + " must produce a typed model");
        });
    }

    /// <summary>
    /// A catalog-only fixture is individually valid. It is what makes its catalog wrong,
    /// which is a different stage; asserting cleanliness here is what proves the two
    /// stages are actually separate rather than one pass that happens to catch both.
    /// </summary>
    [TestCaseSource(nameof(CatalogOnlyCases))]
    public void ACatalogOnlyFixtureIsIndividuallyValid(CategoryFixture fixture)
    {
        DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);

        Assert.That(
            result.Diagnostics,
            Is.Empty,
            () => fixture.Path + " is invalid only against a catalog, so the per-file pass must "
                + "accept it: " + string.Join("; ", result.Diagnostics));
    }

    [TestCaseSource(nameof(InvalidCases))]
    public void AnInvalidFixtureFailsWithTheCodeItIsNamedFor(CategoryFixture fixture)
    {
        DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);
        IReadOnlyList<string> codes = Codes(result);

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False, fixture.Path + " must be rejected");
            Assert.That(
                codes,
                Does.Contain(fixture.ExpectedCode),
                () => fixture.Path + " must fail with " + fixture.ExpectedCode
                    + "; a gate that passes on the wrong error is not a gate. Produced: "
                    + string.Join("; ", result.Diagnostics));
        });
    }

    /// <summary>
    /// Every fixture the corpus names exists on disk, so a renamed file fails the suite
    /// rather than quietly dropping out of it.
    /// </summary>
    [Test]
    public void EveryNamedFixtureExists()
    {
        List<string> missing = new();
        foreach (CategoryFixture fixture in AllFixtures())
        {
            if (!File.Exists(CategoryFixtureCorpus.Absolute(fixture.Path)))
            {
                missing.Add(fixture.Path);
            }
        }

        Assert.That(missing, Is.Empty, () => "named but absent: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Every fixture on disk is named by the corpus. A file nobody reads is a fixture
    /// that looks like coverage and is not.
    /// </summary>
    [Test]
    public void EveryFixtureOnDiskIsNamedByTheCorpus()
    {
        HashSet<string> named = new(System.StringComparer.Ordinal);
        foreach (CategoryFixture fixture in AllFixtures())
        {
            named.Add(CategoryFixtureCorpus.Absolute(fixture.Path));
        }

        List<string> orphans = new();
        foreach (string path in Directory.GetFiles(
                     CategoryFixtureCorpus.Root, "*.json", SearchOption.AllDirectories))
        {
            if (!named.Contains(path))
            {
                orphans.Add(TestArtifacts.Relative(path));
            }
        }

        Assert.That(
            orphans,
            Is.Empty,
            () => "fixtures on disk that no test reads: " + string.Join(", ", orphans));
    }

    /// <summary>
    /// Every definition kind has at least one valid fixture, so a kind cannot be added
    /// with a field table nothing ever exercises.
    /// </summary>
    [Test]
    public void EveryDefinitionKindHasAValidFixture()
    {
        HashSet<DefinitionKind> covered = new();
        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Valid)
        {
            covered.Add(fixture.Kind);
        }

        List<DefinitionKind> uncovered = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            if (!covered.Contains(descriptor.Kind))
            {
                uncovered.Add(descriptor.Kind);
            }
        }

        Assert.That(
            uncovered,
            Is.Empty,
            () => "definition kinds with no valid fixture: " + string.Join(", ", uncovered));
    }

    private static IEnumerable<CategoryFixture> AllFixtures()
    {
        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Valid)
        {
            yield return fixture;
        }

        foreach (CategoryFixture fixture in CategoryFixtureCorpus.CatalogOnly)
        {
            yield return fixture;
        }

        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Invalid)
        {
            yield return fixture;
        }
    }

    private static IReadOnlyList<string> Codes(DefinitionReadResult result)
    {
        List<string> codes = new(result.Diagnostics.Count);
        foreach (ContentDiagnostic diagnostic in result.Diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        return codes;
    }
}
