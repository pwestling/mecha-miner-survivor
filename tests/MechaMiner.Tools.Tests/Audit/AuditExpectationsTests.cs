using System.Collections.Generic;
using System.IO;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Tools.Tests.Audit;

/// <summary>
/// The key-matching rule of <c>build/audit-expectations.env</c>, controlled over the same
/// spellings <c>build/verify-registry.sh</c> stage 6 drives through its own reader.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c>. Verification: <c>VER-FND-009-002</c>, <c>VER-FND-009-008</c> —
/// both entries depend on a value read out of that file, so the reading of it is part of
/// what they claim.
/// </para>
/// <para>
/// This existed nowhere. The expectations file is a single-owner mechanism whose whole
/// argument is that one value has one owner and two readers, and neither reader's key
/// matching had a control — so they disagreed without anything failing.
/// <c>FORBIDDEN_EDGE_CONTROLS =112</c>, one space before the <c>=</c>, read as <c>112</c>
/// in the shell (whose <c>sed</c> allowed <c>[[:space:]]*=</c>) and threw
/// <c>declares 0 value(s)</c> in C# (which compared <c>trimmed[..separator]</c>, that is
/// <c>"FORBIDDEN_EDGE_CONTROLS "</c>, raw). It failed closed, which is the right direction
/// and is not the point: two readers that disagree about whether the value exists are not
/// one owner.
/// </para>
/// <para>
/// <see cref="KeyVariants"/> is the same list, in the same order, as
/// <c>EXPECTATION_KEY_VARIANTS</c> in <c>build/verify-registry.sh</c>. Keeping one list in
/// two places is what makes a future divergence a failing control instead of a discovery;
/// if a spelling is added here, add it there.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class AuditExpectationsTests
{
    private const string Key = "PROBE_KEY";

    /// <summary>
    /// Each accepted spelling and each rejected one. <c>null</c> means the key must not be
    /// found at all, which <see cref="AuditExpectations.ReadFrom"/> signals by throwing.
    /// </summary>
    private static IEnumerable<TestCaseData> KeyVariants
    {
        get
        {
            yield return new TestCaseData("PROBE_KEY=112", "112");
            yield return new TestCaseData("PROBE_KEY =112", "112");
            yield return new TestCaseData("PROBE_KEY= 112", "112");
            yield return new TestCaseData("PROBE_KEY = 112", "112");
            yield return new TestCaseData("  PROBE_KEY=112", "112");
            yield return new TestCaseData("PROBE_KEY=112  ", "112");
            yield return new TestCaseData("PROBE_KEY\t=\t112", "112");
            yield return new TestCaseData("# PROBE_KEY=112", null);
            yield return new TestCaseData("#PROBE_KEY=112", null);
            yield return new TestCaseData("PREFIX_PROBE_KEY=112", null);
            yield return new TestCaseData("PROBE_KEY_SUFFIX=112", null);
            yield return new TestCaseData("PROBE KEY=112", null);
            yield return new TestCaseData("PROBE_KEY", null);
            yield return new TestCaseData("=112", null);
            yield return new TestCaseData(string.Empty, null);
        }
    }

    [TestCaseSource(nameof(KeyVariants))]
    public void TheKeyMatchingRuleReadsEachSpellingTheShellReaderReads(string line, string? expected)
    {
        if (expected is null)
        {
            // The line declares no value for the key, and build/verify-registry.sh's
            // reader agrees; this one must too.
            Expect.Throws<InvalidDataException>(
                () => AuditExpectations.ReadFrom(new[] { line }, Key, "variant"));
            return;
        }

        Assert.That(
            AuditExpectations.ReadFrom(new[] { line }, Key, "variant"),
            Is.EqualTo(expected),
            "the line [" + line + "] must read as " + expected
            + " here and in build/verify-registry.sh's expectation(); a spelling the two"
            + " readers disagree about means the value has two owners, not one");
    }

    /// <summary>Two declarations of one key have no single answer, so neither is returned.</summary>
    [Test]
    public void TwoDeclarationsOfOneKeyAreRejectedRatherThanResolved()
    {
        Expect.Throws<InvalidDataException>(
            () => AuditExpectations.ReadFrom(new[] { "PROBE_KEY=112", "PROBE_KEY=113" }, Key, "variant"));
    }

    /// <summary>
    /// The control on the controls above: the rejections mean nothing if the reader returns
    /// nothing for everything.
    /// </summary>
    [Test]
    public void ThePlainSpellingReadsSoTheRejectionsAreNotVacuous()
    {
        Assert.That(
            AuditExpectations.ReadFrom(new[] { "PROBE_KEY=112" }, Key, "variant"),
            Is.EqualTo("112"));
    }

    /// <summary>
    /// The committed file itself parses under this rule, so the variants are not being
    /// measured against a rule the real file does not satisfy.
    /// </summary>
    [Test]
    public void TheCommittedExpectationsFileParsesUnderTheSameRule()
    {
        Assert.That(AuditExpectations.ForbiddenEdgeControls, Is.GreaterThan(0));
        Assert.That(AuditExpectations.RegistryFixtureClasses, Is.Not.Empty);
    }
}
