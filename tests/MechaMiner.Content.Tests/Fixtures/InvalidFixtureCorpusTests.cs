using System.Collections.Generic;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Every invalid fixture fails, and fails with the specific code it is named for.
/// </summary>
/// <remarks>
/// <para>
/// Asserting the <em>specific</em> code is the whole point. A test that passed whenever
/// something failed would go green on a validator that rejected every document, and a
/// corpus that cannot tell a correct rejection from an accidental one is not a gate.
/// The over-strict direction is covered by <see cref="ValidFixtureCorpusTests"/>.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-001</c> through <c>VER-DAT-001-007</c>,
/// <c>VER-DAT-001-014</c> through <c>VER-DAT-001-019</c>, <c>VER-DAT-001-024</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class InvalidFixtureCorpusTests
{
    private static IEnumerable<FixtureCorpus.InvalidFixture> Cases => FixtureCorpus.Invalid;

    [TestCaseSource(nameof(Cases))]
    public void TheFixtureIsRejectedWithExactlyItsNamedDiagnosticCode(
        FixtureCorpus.InvalidFixture fixture)
    {
        EnvelopeReadResult result = EnvelopeReader.Read(
            FixtureCorpus.Read(fixture.Path),
            fixture.Context());

        Expect.Multiple(() =>
        {
            Assert.That(
                result.IsValid,
                Is.False,
                () => fixture.Path + " must be rejected, but it validated");

            Assert.That(
                Codes(result),
                Does.Contain(fixture.ExpectedCode),
                () => fixture.Path + " must report " + fixture.ExpectedCode + " ("
                    + ContentDiagnosticCodes.Describe(fixture.ExpectedCode).Description
                    + "), but reported: " + Describe(result));
        });
    }

    /// <summary>
    /// A fixture proves one rule, so it reports one code. A fixture that tripped several
    /// would still pass the assertion above while quietly testing something other than
    /// what it is named for.
    /// </summary>
    [TestCaseSource(nameof(Cases))]
    public void TheFixtureProvesExactlyOneRule(FixtureCorpus.InvalidFixture fixture)
    {
        EnvelopeReadResult result = EnvelopeReader.Read(
            FixtureCorpus.Read(fixture.Path),
            fixture.Context());

        Assert.That(
            Codes(result),
            Is.EquivalentTo(new[] { fixture.ExpectedCode }),
            () => fixture.Path + " must isolate one failure, but reported: " + Describe(result));
    }

    /// <summary>
    /// Every diagnostic carries the five elements doc 40 requires, on a real rejection
    /// rather than on a hand-built instance.
    /// </summary>
    [TestCaseSource(nameof(Cases))]
    public void EveryDiagnosticNamesItsSourcePathAndExpectedConstraint(
        FixtureCorpus.InvalidFixture fixture)
    {
        EnvelopeReadResult result = EnvelopeReader.Read(
            FixtureCorpus.Read(fixture.Path),
            fixture.Context());

        Expect.Multiple(() =>
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
            {
                Assert.That(
                    diagnostic.SourcePath,
                    Is.EqualTo(FixtureCorpus.SourcePathOf(fixture.Path)),
                    "a diagnostic reports the repository-relative path of the file at fault");
                Assert.That(
                    diagnostic.ExpectedConstraint,
                    Is.Not.Empty,
                    "a diagnostic states the constraint that was expected");
                Assert.That(
                    ContentDiagnosticCodes.IsDeclared(diagnostic.Code),
                    Is.True,
                    "every emitted code is declared in ContentDiagnosticCodes");
                Assert.That(
                    diagnostic.Severity,
                    Is.EqualTo(ContentDiagnosticSeverity.Error),
                    "an invalid definition is an error, not a warning");
            }
        });
    }

    private static IReadOnlyList<string> Codes(EnvelopeReadResult result)
    {
        List<string> codes = new();
        HashSet<string> seen = new(System.StringComparer.Ordinal);
        foreach (ContentDiagnostic diagnostic in result.Diagnostics)
        {
            if (seen.Add(diagnostic.Code))
            {
                codes.Add(diagnostic.Code);
            }
        }

        return codes;
    }

    private static string Describe(EnvelopeReadResult result)
    {
        return result.Diagnostics.Count == 0
            ? "(no diagnostics)"
            : string.Join("; ", result.Diagnostics);
    }
}
