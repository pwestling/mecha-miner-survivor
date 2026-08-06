using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Diagnostics;

/// <summary>
/// The diagnostic code registry: unique, described, banded, and fully exercised.
/// </summary>
/// <remarks>
/// <para>
/// The last of those is the one with teeth. A registry of codes nothing emits is a
/// wish list, and a validator that emits a code the registry does not declare cannot be
/// enumerated or reported on. This asserts the two sets are equal.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-012</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ContentDiagnosticCodesTests
{
    [Test]
    public void NoTwoCodesCollide()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        Expect.Multiple(() =>
        {
            foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
            {
                Assert.That(
                    seen.Add(descriptor.Code),
                    Is.True,
                    descriptor.Code + " is declared more than once");
            }
        });
    }

    [Test]
    public void EveryCodeHasANameAndAHumanDescription()
    {
        Expect.Multiple(() =>
        {
            foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
            {
                Assert.That(descriptor.Name, Is.Not.Empty, descriptor.Code + " needs a name");
                Assert.That(
                    descriptor.Description, Is.Not.Empty, descriptor.Code + " needs a description");
            }
        });
    }

    /// <summary>
    /// The convention: <c>MMC-</c> for content, four digits, leading digit is the
    /// validation stage. It mirrors the <c>MMT-####</c> grammar
    /// <c>src/MechaMiner.Tools/Cli/DiagnosticCodes.cs</c> established, differing only in
    /// what the leading digit encodes.
    /// </summary>
    [Test]
    public void EveryCodeFollowsTheMmcConventionAndSitsInItsStageBand()
    {
        Expect.Multiple(() =>
        {
            foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
            {
                Assert.That(
                    descriptor.Code,
                    Does.Match("^MMC-[0-9]{4}$"),
                    descriptor.Code + " must be MMC- followed by four digits");

                int number = int.Parse(
                    descriptor.Code.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture);

                NumericAssert.AreExactlyEqual(
                    (int)descriptor.Stage,
                    number / 1000,
                    descriptor.Code + ": the leading digit is the validation stage");
            }
        });
    }

    [Test]
    public void LookupFindsADeclaredCodeAndRefusesAnUndeclaredOne()
    {
        Expect.Multiple(() =>
        {
            Assert.That(ContentDiagnosticCodes.IsDeclared(ContentDiagnosticCodes.NullValue), Is.True);
            Assert.That(ContentDiagnosticCodes.IsDeclared("MMC-9999"), Is.False);
            Assert.That(ContentDiagnosticCodes.IsDeclared("MMT-2001"), Is.False);
            Expect.Throws<ArgumentException>(() => ContentDiagnosticCodes.Describe("MMC-9999"));
        });
    }

    /// <summary>
    /// Every codec violation kind maps to a declared code, so adding a kind without a
    /// code is a test failure rather than a runtime exception in a build.
    /// </summary>
    [Test]
    public void EveryCodecViolationKindMapsToADeclaredCode()
    {
        Expect.Multiple(() =>
        {
            foreach (StrictJsonViolationKind kind in Enum.GetValues<StrictJsonViolationKind>())
            {
                if (kind == StrictJsonViolationKind.None)
                {
                    continue;
                }

                string code = StrictJsonDiagnostics.CodeFor(kind);
                Assert.That(
                    ContentDiagnosticCodes.IsDeclared(code),
                    Is.True,
                    kind + " maps to " + code + ", which must be declared");
            }
        });
    }

    /// <summary>
    /// The set of codes the suite actually provokes equals the declared set.
    /// </summary>
    /// <remarks>
    /// Both directions matter. An undeclared code that a validator can emit is
    /// unenumerable; a declared code nothing provokes is untested, and the first time it
    /// fires it will be in a build nobody expected it in.
    /// </remarks>
    [Test]
    public void TheCodesTheSuiteProvokesAreExactlyTheCodesDeclared()
    {
        HashSet<string> provoked = new(StringComparer.Ordinal);

        foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
        {
            EnvelopeReadResult result =
                EnvelopeReader.Read(FixtureCorpus.Read(fixture.Path), fixture.Context());
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
            {
                provoked.Add(diagnostic.Code);
            }
        }

        foreach (string code in ProvokedOutsideTheFileCorpus())
        {
            provoked.Add(code);
        }

        // The semantic and relational bands are provoked by the category corpus and by
        // the catalog and relational checks, which no envelope fixture can reach.
        foreach (string code in Categories.CategoryDiagnosticProbe.Provoked())
        {
            provoked.Add(code);
        }

        List<string> declared = new();
        foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
        {
            declared.Add(descriptor.Code);
        }

        Assert.That(
            provoked,
            Is.EquivalentTo(declared),
            "every declared code must be provoked by the suite - the DAT-001 envelope corpus, "
                + "the DAT-002 and DAT-003 category corpus, and the catalog and relational "
                + "checks between them - and the suite must provoke nothing undeclared");
    }

    /// <summary>
    /// The declared codes, written out here rather than read from the registry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TheCodesTheSuiteProvokesAreExactlyTheCodesDeclared"/> compares two sets
    /// that both shrink: delete a descriptor <em>and</em> the fixture that provokes it and
    /// the equality still holds, with one code fewer on each side and nothing naming what
    /// went. Codes are "never reused and never renumbered", and a code that quietly stops
    /// existing is the same loss of history as one reused.
    /// </para>
    /// <para>
    /// <b>Changing this list is a deliberate change to the registry.</b> A retired code
    /// stays declared - retirement is recorded on the descriptor, not performed with a
    /// delete key - so a code should only ever be added here.
    /// </para>
    /// </remarks>
    private static readonly string[] DeclaredCodeRoster =
    {
        // Codec, band 1xxx.
        "MMC-1001", "MMC-1002", "MMC-1003", "MMC-1004", "MMC-1005", "MMC-1006", "MMC-1007",
        "MMC-1008", "MMC-1009", "MMC-1010", "MMC-1011", "MMC-1012", "MMC-1013", "MMC-1014",
        "MMC-1015",

        // Structural, band 2xxx.
        "MMC-2001", "MMC-2002", "MMC-2003", "MMC-2004", "MMC-2005", "MMC-2006", "MMC-2007",
        "MMC-2008", "MMC-2009",

        // Identity, band 3xxx.
        "MMC-3001", "MMC-3002",

        // Traceability, band 4xxx.
        "MMC-4001", "MMC-4002", "MMC-4003",

        // Schema infrastructure, band 5xxx.
        "MMC-5001", "MMC-5002", "MMC-5003",
    };

    /// <summary>
    /// The registry contains exactly the codes this test names, so a code cannot leave it
    /// silently.
    /// </summary>
    [Test]
    public void TheDeclaredCodesAreExactlyTheRosterStatedHere()
    {
        List<string> declared = new();
        foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
        {
            declared.Add(descriptor.Code);
        }

        Expect.Multiple(() =>
        {
            foreach (string code in DeclaredCodeRoster)
            {
                Assert.That(
                    declared,
                    Does.Contain(code),
                    code + " is no longer declared in ContentDiagnosticCodes. Deleting a "
                        + "descriptor and the fixture that provokes it shrinks both sides of "
                        + "the provoked-equals-declared check together, so that test would "
                        + "stay green; this one names the code that went");
            }

            Assert.That(
                declared,
                Is.EquivalentTo(DeclaredCodeRoster),
                "a new diagnostic code must be added to this roster in the same change, so "
                    + "that the registry and its independent statement cannot drift");
        });
    }

    /// <summary>
    /// The codes that cannot come from a committed <c>.json</c> fixture, each provoked
    /// here instead.
    /// </summary>
    /// <remarks>
    /// Invalid UTF-8 cannot be a committed text fixture without fighting the repository's
    /// encoding rules, and the three schema-infrastructure codes are provoked by schema
    /// documents rather than by definitions.
    /// </remarks>
    private static IEnumerable<string> ProvokedOutsideTheFileCorpus()
    {
        List<string> codes = new();

        // Invalid UTF-8: 0xFF is not a legal byte in any position.
        byte[] invalidUtf8 = { (byte)'{', (byte)'"', 0xFF, (byte)'"', (byte)':', (byte)'1', (byte)'}' };
        EnvelopeReadResult result = EnvelopeReader.Read(
            invalidUtf8,
            new EnvelopeReadContext("tests/invalid-utf8.json", Content.Ids.ContentCategory.Weapon));
        foreach (ContentDiagnostic diagnostic in result.Diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        foreach (string name in new[]
                 {
                     "unsupported-keyword.schema.json",
                     "unresolvable-ref.schema.json",
                     "malformed.schema.json",
                 })
        {
            string path = Path.Combine(FixtureCorpus.Root, "schema", name);
            JsonSchemaLoadResult load =
                JsonSchemaLoader.Load(File.ReadAllBytes(path), "tests/fixtures/schema/" + name);
            foreach (ContentDiagnostic diagnostic in load.Diagnostics)
            {
                codes.Add(diagnostic.Code);
            }
        }

        return codes;
    }

    /// <summary>
    /// The content codes must not collide with the verb host's, because both appear in
    /// the same structured build output.
    /// </summary>
    [Test]
    public void ContentCodesUseADifferentPrefixFromTheVerbHost()
    {
        Expect.Multiple(() =>
        {
            foreach (ContentDiagnosticDescriptor descriptor in ContentDiagnosticCodes.All)
            {
                Assert.That(
                    descriptor.Code.StartsWith("MMT-", StringComparison.Ordinal),
                    Is.False,
                    "MMT- belongs to src/MechaMiner.Tools/Cli/DiagnosticCodes.cs");
            }
        });
    }
}
