using System.Text;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// A scope must resolve to a path that exists in the definition it annotates.
/// </summary>
/// <remarks>
/// <para>
/// This is the rule that earns the scope extension. A scope is accepted, where doc 40
/// bans a <c>path:line</c>, precisely because it cannot decay silently: the build fails
/// the moment the field it names goes away. These tests are that guarantee.
/// </para>
/// <para>
/// The documents here are nested beyond the envelope's own shape on purpose. Nested
/// member and wildcard steps cannot be exercised through an envelope fixture, because
/// the envelope has no nested objects and unknown fields are rejected.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-019</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SourceRefScopeResolutionTests
{
    private const string Document = """
        {
          "id": "W-AB",
          "recipe_pair": "AB",
          "resonance_behavior": { "short_modifier": "focused", "percent": 20 },
          "rules": ["a", "b", "c", "d"],
          "unlocks": {
            "utilities": [
              { "utility_id": "UTL-A1" },
              { "utility_id": "UTL-B2" }
            ]
          },
          "minute_rows": [
            { "formation_events": [ { "timestamps_reconstructed": true } ] }
          ]
        }
        """;

    [TestCase("id")]
    [TestCase("recipe_pair")]
    [TestCase("resonance_behavior")]
    [TestCase("resonance_behavior.short_modifier")]
    [TestCase("rules[]")]
    [TestCase("rules[0]")]
    [TestCase("rules[3]")]
    [TestCase("rules[1..2]")]
    [TestCase("rules[2..3]")]
    [TestCase("unlocks.utilities[].utility_id")]
    [TestCase("minute_rows[0].formation_events[].timestamps_reconstructed")]
    public void AScopeNamingAnExistingPathResolves(string scope)
    {
        Assert.That(Resolves(scope), Is.True, () => "'" + scope + "' should resolve");
    }

    [TestCase("recipe_pairs")]
    [TestCase("resonance_behaviour")]
    [TestCase("resonance_behavior.long_modifier")]
    [TestCase("resonance_behavior.short_modifier.deeper")]
    [TestCase("rules[4]")]
    [TestCase("rules[4..6]")]
    [TestCase("missing[]")]
    [TestCase("unlocks.utilities[].utility_name")]
    [TestCase("minute_rows[1].formation_events[].timestamps_reconstructed")]
    [TestCase("id[0]")]
    public void AScopeNamingAPathThatIsNotThereDoesNotResolve(string scope)
    {
        Assert.That(Resolves(scope), Is.False, () => "'" + scope + "' should not resolve");
    }

    /// <summary>
    /// A partially satisfiable range resolves: <c>rules[2..9]</c> annotates the rules
    /// that exist. A range that selects nothing at all is the failure.
    /// </summary>
    [Test]
    public void ARangeResolvesWhenAtLeastOneSelectedElementExists()
    {
        Expect.Multiple(() =>
        {
            Assert.That(Resolves("rules[2..9]"), Is.True, "elements 2 and 3 exist");
            Assert.That(Resolves("rules[8..9]"), Is.False, "neither element exists");
        });
    }

    private static bool Resolves(string scope)
    {
        Assert.That(
            SourceRefGrammar.Parse(scope + ": GDD-WEAPON-CATALOG#x", out SourceRef? parsed),
            Is.EqualTo(SourceRefParseOutcome.Parsed),
            () => "the test's own scope '" + scope + "' must be syntactically valid");

        StrictJsonScanResult scan = StrictJsonReader.Scan(
            Encoding.UTF8.GetBytes(Document), StrictJsonPolicy.Definitions);
        Assert.That(scan.IsValid, Is.True, "the test document must scan cleanly");

        return parsed!.Scope!.ResolvesIn(scan.Structure);
    }
}
