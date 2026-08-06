using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Every numeric bound in a project schema records where its number came from.
/// </summary>
/// <remarks>
/// <para>
/// The gate exists so that "which schema bounds need re-deriving now that a document's
/// capacity section changed" is a query rather than a recollection. Without it the
/// annotation rots into decoration: a few bounds carry it, new ones do not, and nothing
/// notices.
/// </para>
/// <para>
/// Provenance is a property of a <em>number</em>, so <c>x-authority</c> is a map keyed by
/// the bound each entry explains and a subschema asserting three numbers writes three
/// entries. It was once a single object on the subschema, which meant one authority
/// vouched for every bound beside it.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-025</c>, <c>VER-DAT-001-031</c>, <c>VER-DAT-001-032</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaAuthorityTests
{
    private static IEnumerable<string> ProjectSchemas
    {
        get
        {
            string directory = Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas");
            return Directory.GetFiles(directory, "*.schema.json", SearchOption.AllDirectories);
        }
    }

    [TestCaseSource(nameof(ProjectSchemas))]
    public void EveryNumericBoundInAProjectSchemaCarriesAnAdjacentAuthority(string path)
    {
        IReadOnlyList<string> unattributed = FindUnattributedBounds(File.ReadAllBytes(path));

        Assert.That(
            unattributed,
            Is.Empty,
            () => TestArtifacts.Relative(path) + " has numeric bounds with no adjacent '"
                + SchemaAuthority.Keyword + "': " + string.Join(", ", unattributed));
    }

    /// <summary>
    /// The source says where a number came from; the derivation says why it is that
    /// number. They go stale independently, so a sourced or derived bound needs both.
    /// </summary>
    [TestCaseSource(nameof(ProjectSchemas))]
    public void EverySourcedOrDerivedBoundStatesItsDerivation(string path)
    {
        IReadOnlyList<string> missing = FindMissingDerivations(File.ReadAllBytes(path));

        Assert.That(
            missing,
            Is.Empty,
            () => TestArtifacts.Relative(path) + " has sourced or derived bounds with no "
                + "'derivation': " + string.Join(", ", missing));
    }

    /// <summary>
    /// A structural bound has no citation to go stale, so the only thing standing between
    /// it and a limit chosen to make something pass is a stated reason.
    /// </summary>
    /// <remarks>
    /// The reason is asked of the entry, not of the enclosing subschema. It used to be the
    /// subschema's <c>description</c>, which is per subschema and therefore licensed every
    /// structural bound under it at once.
    /// </remarks>
    [TestCaseSource(nameof(ProjectSchemas))]
    public void EveryStructuralBoundStatesItsRationale(string path)
    {
        IReadOnlyList<string> missing = FindMissingRationales(File.ReadAllBytes(path));

        Assert.That(
            missing,
            Is.Empty,
            () => TestArtifacts.Relative(path) + " has structural bounds whose x-authority "
                + "entry states no 'rationale': " + string.Join(", ", missing));
    }

    /// <summary>
    /// The negative control. Without it, the gate above would pass just as happily if
    /// <see cref="FindUnattributedBounds"/> never found anything.
    /// </summary>
    [Test]
    public void TheGateFailsOnABareBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-bound.schema.json");

        IReadOnlyList<string> unattributed = FindUnattributedBounds(File.ReadAllBytes(path));

        Expect.Multiple(() =>
        {
            Assert.That(unattributed, Is.Not.Empty, "the gate must catch a bare bound");
            Assert.That(unattributed[0], Does.Contain("maximum"));
        });
    }

    /// <summary>
    /// The same control for an exclusive bound, which asserts the same number as its
    /// inclusive spelling and so needs the same provenance.
    /// </summary>
    [Test]
    public void TheGateFailsOnABareExclusiveBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-exclusive-bound.schema.json");
        byte[] bytes = File.ReadAllBytes(path);

        IReadOnlyList<string> unattributed = FindUnattributedBounds(bytes);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/unattributed-exclusive-bound.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(unattributed, Is.Not.Empty, "the gate must catch a bare exclusive bound");
            Assert.That(unattributed[0], Does.Contain("exclusiveMinimum"));
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain(SchemaAuthority.Keyword));
        });
    }

    /// <summary>
    /// The same control for a length bound. Nearly every <c>minLength</c> is structural,
    /// and an exemption for the obvious cases is what would let the one sourced length
    /// through unattributed.
    /// </summary>
    [Test]
    public void TheGateFailsOnABareLengthBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-length-bound.schema.json");
        byte[] bytes = File.ReadAllBytes(path);

        IReadOnlyList<string> unattributed = FindUnattributedBounds(bytes);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/unattributed-length-bound.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(unattributed, Is.Not.Empty, "the gate must catch a bare length bound");
            Assert.That(unattributed[0], Does.Contain("minLength"));
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain(SchemaAuthority.Keyword));
        });
    }

    [Test]
    public void TheDerivationGateFailsOnASourcedBoundWithNoDerivation()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "undelivered-derivation.schema.json");

        IReadOnlyList<string> missing = FindMissingDerivations(File.ReadAllBytes(path));

        Expect.Multiple(() =>
        {
            Assert.That(missing, Is.Not.Empty, "the gate must catch a bound with no derivation");
            Assert.That(missing[0], Does.Contain("maximum"));
        });
    }

    [Test]
    public void TheLoaderAlsoRejectsASourcedBoundWithNoDerivation()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "undelivered-derivation.schema.json");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            File.ReadAllBytes(path), "tests/fixtures/schema/undelivered-derivation.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain("derivation"));
        });
    }

    /// <summary>The loader rejects the same file, so the gate holds from both directions.</summary>
    [Test]
    public void TheLoaderAlsoRejectsABareBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-bound.schema.json");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            File.ReadAllBytes(path), "tests/fixtures/schema/unattributed-bound.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain(SchemaAuthority.Keyword));
        });
    }

    [Test]
    public void AProjectSchemaStillLoadsWithItsAuthorities()
    {
        foreach (string path in ProjectSchemas)
        {
            JsonSchemaLoadResult load = JsonSchemaLoader.Load(
                File.ReadAllBytes(path), TestArtifacts.Relative(path));
            Assert.That(
                load.IsValid,
                Is.True,
                () => TestArtifacts.Relative(path) + ": " + string.Join("; ", load.Diagnostics));
        }
    }

    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"Performance and capacity\",\"derivation\":\"worst case ~1010; x2 headroom; rounded to a power of two\"}}}", true)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"derived\",\"source\":\"GDD-MINING\",\"section\":\"Sites\",\"derivation\":\"four site classes times the per-class ceiling\"}}}", true)]
    // A sourced bound with no derivation says where the number came from but not why it
    // is that number.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\"}}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"  \"}}}", false)]
    // A sourced or derived bound states a derivation and not a rationale: two prose fields
    // asking one question mean neither is the one to read.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"d\",\"rationale\":\"r\"}}}", false)]
    // A structural bound has no derivation; it states a rationale of its own.
    [TestCase("{\"maximum\":1,\"description\":\"d\",\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\"derivation\":\"x\"}}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\"rationale\":\"why\"}}}", true)]
    // A structural bound with no rationale: the limit is unjustified, and a description
    // that says something does not answer for it.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\"}}}", false)]
    [TestCase("{\"maximum\":1,\"description\":\"why\",\"x-authority\":{\"maximum\":{\"kind\":\"structural\"}}}", false)]
    // A structural bound must not claim an authority it does not have.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\"rationale\":\"r\",\"source\":\"TDD-COMBAT\"}}}", false)]
    // A sourced bound needs both the document and the section.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"section\":\"S\",\"derivation\":\"d\"}}}", false)]
    // The source uses the source_refs vocabulary, validated by the same parser.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"docs/22.md\",\"section\":\"S\",\"derivation\":\"d\"}}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD_COMBAT\",\"section\":\"S\",\"derivation\":\"d\"}}}", false)]
    // A section is a heading, so an anchor slug belongs in the section field, not the ID.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT#capacity\",\"section\":\"S\",\"derivation\":\"d\"}}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"invented\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"d\"}}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"d\",\"extra\":1}}}", false)]
    // An authority with nothing to annotate is misplaced. It carries a rationale, so the
    // misplacement is the only thing left for it to fail on.
    [TestCase("{\"type\":\"integer\",\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    // The map is keyed by the bound it explains, so the flat pre-DAT-001 shape is a
    // failure that names the offending key rather than an authority for a bound called
    // "kind".
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"structural\"}}", false)]
    // An empty map records nothing while reading as though a bound had been attributed.
    [TestCase("{\"maximum\":1,\"x-authority\":{}}", false)]
    // The map is a map, not a subschema and not a list.
    [TestCase("{\"maximum\":1,\"x-authority\":[{\"kind\":\"structural\",\"rationale\":\"r\"}]}", false)]
    // An authority keyed by a keyword that is not a bound at all.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"type\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    public void TheAuthorityShapeIsValidated(string schemaText, bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");

        Assert.That(
            load.IsValid,
            Is.EqualTo(expected),
            () => schemaText + " -> " + string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// The negative control, parameterised over the whole keyword list rather than
    /// written out per keyword.
    /// </summary>
    /// <remarks>
    /// Written this way so that a keyword added to <see cref="SchemaAuthority.BoundKeywords"/>
    /// arrives with its control already in place. The per-keyword form drifts: the list
    /// grows, the controls do not, and the newest keyword is the one with nothing proving
    /// its gate can fail — which is exactly the keyword a reviewer would most want proven.
    /// </remarks>
    [TestCaseSource(typeof(SchemaAuthority), nameof(SchemaAuthority.BoundKeywords))]
    public void TheGateFailsOnABareBoundOfEveryMandatoryKeyword(string keyword)
    {
        // 1 is a legal value for all nine: a number where a number is wanted, and a
        // non-negative integer where a count or a length is.
        byte[] bare = Encoding.UTF8.GetBytes("{\"" + keyword + "\":1}");

        IReadOnlyList<string> unattributed = FindUnattributedBounds(bare);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(bare, "inline.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(unattributed, Is.Not.Empty, () => "the walk must catch a bare " + keyword);
            Assert.That(unattributed[0], Does.Contain(keyword));
            Assert.That(load.IsValid, Is.False, () => "the loader must reject a bare " + keyword);
            Assert.That(
                load.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed),
                () => "a bare " + keyword + " must fail for the stated reason, not merely fail");
            Assert.That(
                load.Diagnostics[0].ExpectedConstraint,
                Does.Contain(SchemaAuthority.Keyword));
        });
    }

    /// <summary>
    /// The control in the other direction: each mandatory keyword is satisfiable, so the
    /// gate above is a demand for attribution rather than a ban on the keyword.
    /// </summary>
    [TestCaseSource(typeof(SchemaAuthority), nameof(SchemaAuthority.BoundKeywords))]
    public void EveryMandatoryKeywordLoadsOnceAttributed(string keyword)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(
                "{\"" + keyword + "\":1,\"x-authority\":{\"" + keyword
                    + "\":{\"kind\":\"structural\",\"rationale\":\"why this number\"}}}"),
            "inline.schema.json");

        Assert.That(load.IsValid, Is.True, () => string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// One <c>x-authority</c> entry covers one bound. A second, unattributed bound beside
    /// it is reported, by name, and attributing the first does not licence the second.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control every earlier one in this fixture was missing. They all took the same
    /// shape - one guarded thing, in one position, unattributed - and each proved the gate
    /// reached a position. None of them asked <em>how much one answer licenses</em>, and
    /// the answer was "everything in the subschema": <c>x-authority</c> was read as a flag
    /// on the object, so a subschema declaring <c>minLength</c> and <c>maxLength</c>
    /// satisfied the whole gate by attributing either one. Adding a bare
    /// <c>"maxLength": 4096</c> to <c>content/schemas/envelope.schema.json</c> beside its
    /// attributed <c>minLength</c> left the suite entirely green.
    /// </para>
    /// <para>
    /// Parameterised over every pair of distinct bound keywords would be 72 cases proving
    /// one thing; the pair here is the one from the real file, and the general statement is
    /// carried by <see cref="TheGateFailsOnABareBoundOfEveryMandatoryKeyword"/> being about
    /// each keyword on its own.
    /// </para>
    /// </remarks>
    [Test]
    public void AnAuthorityOnOneBoundDoesNotCoverAnotherBoundBesideIt()
    {
        byte[] bytes = File.ReadAllBytes(
            Path.Combine(FixtureCorpus.Root, "schema", "partly-attributed-bounds.schema.json"));

        IReadOnlyList<string> unattributed = FindUnattributedBounds(bytes);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/partly-attributed-bounds.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(
                unattributed,
                Has.Exactly(1).Contains("maxLength"),
                () => "the walk must report the unattributed maxLength: "
                    + string.Join(", ", unattributed));
            Assert.That(
                unattributed,
                Has.None.Contains("minLength"),
                () => "the attributed minLength must not be reported: "
                    + string.Join(", ", unattributed));
            Assert.That(
                load.IsValid,
                Is.False,
                "the loader must reject a subschema that attributes one of its two bounds");
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("maxLength"),
                () => "the loader's diagnostic must name the unattributed keyword rather than "
                    + "merely fail: " + string.Join("; ", load.Diagnostics));
        });
    }

    /// <summary>
    /// The same statement inline and in both directions: two bounds need two entries, and
    /// an entry for a bound the subschema does not declare is rejected too.
    /// </summary>
    /// <remarks>
    /// The second half matters as much as the first. An authority for an absent bound is
    /// provenance for nothing today, and it silently becomes provenance for whatever bound
    /// is added under that key tomorrow - the same standing-waiver failure a stale
    /// exemption is.
    /// </remarks>
    // Two bounds, one authority: rejected. Every entry present carries a rationale, so the
    // arity of the attribution is the only thing left for these to fail on.
    [TestCase("{\"minLength\":1,\"maxLength\":9,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    [TestCase("{\"minLength\":1,\"maxLength\":9,\"x-authority\":{\"maxLength\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    // Two bounds, two authorities: accepted.
    [TestCase("{\"minLength\":1,\"maxLength\":9,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"r\"},\"maxLength\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", true)]
    // Three bounds, two authorities: rejected.
    [TestCase("{\"minimum\":1,\"maximum\":9,\"multipleOf\":3,\"x-authority\":{\"minimum\":{\"kind\":\"structural\",\"rationale\":\"r\"},\"maximum\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    // An authority for a bound the subschema does not declare.
    [TestCase("{\"minLength\":1,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"r\"},\"maxLength\":{\"kind\":\"structural\",\"rationale\":\"r\"}}}", false)]
    public void EveryBoundNeedsItsOwnAuthorityAndEveryAuthorityItsOwnBound(
        string schemaText,
        bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");

        Assert.That(
            load.IsValid,
            Is.EqualTo(expected),
            () => schemaText + " -> " + string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// The structure-blind walk agrees, keyword by keyword, on which of two bounds in one
    /// subschema is attributed.
    /// </summary>
    /// <remarks>
    /// The loader and the walk have different blind spots on purpose, and this was the
    /// defect they shared: both asked the per-subschema question. Asserting the pair here
    /// keeps them from drifting back to it together.
    /// </remarks>
    [TestCase("minLength", "maxLength")]
    [TestCase("maxLength", "minLength")]
    [TestCase("minimum", "maximum")]
    [TestCase("minItems", "maxItems")]
    public void TheWalkAttributesEachOfTwoBoundsSeparately(string attributed, string bare)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "{\"" + attributed + "\":1,\"" + bare + "\":9,\"x-authority\":{\"" + attributed
                + "\":{\"kind\":\"structural\",\"rationale\":\"why this number\"}}}");

        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.BoundsSeen,
                Is.EqualTo(2),
                "both bounds must be counted, and neither the annotation's own key nor its "
                    + "value may be counted as a third");
            Assert.That(
                walk.Unattributed,
                Is.EqualTo(new[] { "/" + bare }),
                () => "exactly the unattributed bound, named: "
                    + string.Join(", ", walk.Unattributed));
        });
    }

    /// <summary>
    /// The <c>x-authority</c> map's own keys are not bounds.
    /// </summary>
    /// <remarks>
    /// Keying the annotation by bound keyword puts the string <c>"minimum"</c> inside the
    /// annotation, one level below a real <c>minimum</c>. A structure-blind walk that
    /// descended into the annotation would count that key as a bound the schema asserts,
    /// report it unattributed at a pointer inside the annotation, and inflate
    /// <c>BoundsSeen</c> - which is the counter the whole coverage argument rests on. This
    /// is the same phantom the <c>properties</c> map produced, arriving through the fix for
    /// a different hole.
    /// </remarks>
    [TestCaseSource(typeof(SchemaAuthority), nameof(SchemaAuthority.BoundKeywords))]
    public void TheAuthorityMapsOwnKeysAreNotCountedAsBounds(string keyword)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "{\"" + keyword + "\":1,\"x-authority\":{\"" + keyword
                + "\":{\"kind\":\"structural\",\"rationale\":\"why this number\"}}}");

        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.BoundsSeen,
                Is.EqualTo(1),
                "the schema asserts one number; the annotation's key naming it is not a second");
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                () => "nothing here is unattributed: " + string.Join(", ", walk.Unattributed));
            Assert.That(
                walk.MissingRationales,
                Is.Empty,
                () => "the entry states a rationale: "
                    + string.Join(", ", walk.MissingRationales));
        });
    }

    /// <summary>
    /// A <c>rationale</c> whose text is a bound keyword is prose, not a bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The phantom check on this change itself. Moving the rationale out of the subschema's
    /// <c>description</c> and into the authority entry puts another author-written string one
    /// level inside the annotation, which is the position the structure-blind walk has been
    /// fooled in twice: once by <c>properties</c>, whose keys are author-chosen, and once by
    /// the authority map's own keys.
    /// </para>
    /// <para>
    /// The walk is safe here for a reason worth stating rather than assuming: it steps over
    /// <c>x-authority</c> wholesale, so nothing inside the annotation - key or value, at any
    /// depth - is read as a schema. The rationale text is asserted with a bound keyword in it
    /// to hold that in place, because a walk that descended one level for the rationale's
    /// sake would count this as a second bound and inflate <c>BoundsSeen</c>, the counter the
    /// coverage argument rests on.
    /// </para>
    /// </remarks>
    [TestCaseSource(typeof(SchemaAuthority), nameof(SchemaAuthority.BoundKeywords))]
    public void ARationaleNamingABoundKeywordIsNotCountedAsASecondBound(string keyword)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\","
                + "\"rationale\":\"chosen so that " + keyword + " stays a round number\"}}}");

        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(bytes, "inline.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.BoundsSeen,
                Is.EqualTo(1),
                "the schema asserts one number; a rationale mentioning '" + keyword
                    + "' is prose about that number, not a second one");
            Assert.That(walk.Unattributed, Is.Empty);
            Assert.That(walk.MissingRationales, Is.Empty);
            Assert.That(load.IsValid, Is.True, () => string.Join("; ", load.Diagnostics));
        });
    }

    /// <summary>
    /// No field of an authority entry may hold a subschema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same hole <c>title</c> and <c>description</c> were, one level further in, and
    /// found while adding a fifth string field to the entry. These fields were read as "a
    /// string if it is a string, otherwise absent", so
    /// <c>{"kind":"structural","source":{"if":{"maximum":5}}}</c> read as a structural entry
    /// that declares no source: the loader raised nothing, and the corpus walk steps over
    /// <c>x-authority</c> wholesale by design. Between the two of them the subschema parked
    /// under <c>source</c> was walked by nobody, which is strictly worse than the annotation
    /// case, where at least the blind walk still reached the bound.
    /// </para>
    /// <para>
    /// Asserted over every field rather than over <c>rationale</c> alone. A type rule applied
    /// to the newest field and not its neighbours is how this reopens.
    /// </para>
    /// </remarks>
    [TestCase("source")]
    [TestCase("section")]
    [TestCase("derivation")]
    [TestCase("rationale")]
    public void AnAuthorityFieldCannotHideASubschema(string field)
    {
        byte[] hiding = Encoding.UTF8.GetBytes(
            "{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\""
                + field + "\":{\"if\":{\"maximum\":5}}}}}");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(hiding, "inline.schema.json");
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(hiding);

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                () => "x-authority." + field + " must not accept an object: nothing walks a "
                    + "subschema parked inside the annotation");
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("'" + field + "'"),
                () => "the diagnostic must name the field: "
                    + string.Join("; ", load.Diagnostics));
            Assert.That(
                walk.BoundsSeen,
                Is.EqualTo(1),
                "the corpus walk cannot help here - it steps over x-authority by design - so "
                    + "the loader is the only reader of this position and the type check is "
                    + "the whole of the gate");
        });
    }

    /// <summary>
    /// An annotation keyword holds a string, so a subschema cannot be parked under one.
    /// </summary>
    /// <remarks>
    /// <c>title</c>, <c>description</c> and <c>$comment</c> carry no assertion, and the
    /// loader used to accept any JSON value in them. That made each one a hiding place with
    /// a recognised keyword's name on it: the value was never parsed as a subschema, so
    /// every rule the loader enforces at parse time - <c>x-authority</c> placement above
    /// all - stopped at its edge. <c>$schema</c> and <c>$id</c> are here for the same
    /// reason; they are strings by the specification and were accepted just as loosely.
    /// </remarks>
    [TestCase("title")]
    [TestCase("description")]
    [TestCase("$comment")]
    [TestCase("$schema")]
    [TestCase("$id")]
    public void AnAnnotationKeywordCannotHideASubschema(string keyword)
    {
        byte[] hiding = Encoding.UTF8.GetBytes(
            "{\"" + keyword + "\":{\"if\":{\"maximum\":5}}}");
        byte[] stringed = Encoding.UTF8.GetBytes("{\"" + keyword + "\":\"plain text\"}");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(hiding, "inline.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                () => keyword + " must not accept an object: an unevaluated subschema under an "
                    + "annotation is walked by nothing");
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("'" + keyword + "'"),
                () => "the diagnostic must name the annotation: "
                    + string.Join("; ", load.Diagnostics));
            Assert.That(
                JsonSchemaLoader.Load(stringed, "inline.schema.json").IsValid,
                Is.True,
                keyword + " must still accept a string, or this is a ban rather than a type");
        });
    }

    /// <summary>
    /// The committed fixture for the same hole, and the proof that the structure-blind
    /// walk saw the bound the loader was stepping over.
    /// </summary>
    [Test]
    public void ASubschemaHiddenUnderAnAnnotationIsRejectedAndItsBoundIsSeen()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(
            FixtureCorpus.Root, "schema", "annotation-hiding-a-subschema.schema.json"));

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/annotation-hiding-a-subschema.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False, "a subschema under 'title' must be rejected");
            Assert.That(
                load.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(
                FindUnattributedBounds(bytes),
                Has.Some.Contains("maximum"),
                "the structure-blind walk reached the bound the loader used to step over, "
                    + "which is why the two walks are kept as separate implementations");
        });
    }

    /// <summary>
    /// A structural bound's rationale lives in its own <c>x-authority</c> entry, and the
    /// subschema's <c>description</c> does not answer for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule this replaced asked the <em>subschema</em> for the rationale, which is the
    /// arity failure the <c>x-authority</c> map had just been reshaped to fix, sitting one
    /// field over. A <c>description</c> belongs to the subschema, so one sentence licensed
    /// every structural bound under it and nothing could check which clause covered which
    /// number.
    /// </para>
    /// <para>
    /// <b>The two checks do not coexist.</b> The <c>description</c> check is gone rather than
    /// kept as a second line of defence, because the weak check is the one people satisfy: a
    /// shared description would go on passing for two unrelated bounds while the strong check
    /// sat beside it looking like coverage. The final case here is that exact state - a
    /// description that plainly says something, and no rationale - and it must fail.
    /// </para>
    /// <para>
    /// The presence-only spelling of the old rule is retired with it. It accepted <c>""</c>,
    /// <c>"   "</c>, <c>0</c>, <c>false</c>, <c>{}</c> and <c>[]</c>; its replacement is
    /// asserted over the whitespace forms below, and the non-string forms are covered by
    /// <see cref="AnAuthorityFieldCannotHideASubschema"/>.
    /// </para>
    /// </remarks>
    // A rationale that says nothing is not a rationale.
    [TestCase("{\"minLength\":1,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"\"}}}", false)]
    [TestCase("{\"minLength\":1,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"   \"}}}", false)]
    [TestCase("{\"minLength\":1,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"\\t\\n\"}}}", false)]
    // A rationale that does is.
    [TestCase("{\"minLength\":1,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"the empty string is how an omitted field materializes\"}}}", true)]
    // The description no longer licenses anything, however much it says.
    [TestCase("{\"minLength\":1,\"description\":\"the empty string is how an omitted field materializes\",\"x-authority\":{\"minLength\":{\"kind\":\"structural\"}}}", false)]
    // And it is still prose the schema may carry, alongside a rationale.
    [TestCase("{\"minLength\":1,\"description\":\"prose for a reader\",\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"the empty string is how an omitted field materializes\"}}}", true)]
    public void AStructuralBoundNeedsARationaleOfItsOwnWithSomethingInIt(
        string schemaText,
        bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");

        Assert.That(
            load.IsValid,
            Is.EqualTo(expected),
            () => schemaText + " -> " + string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// The committed control: one structural bound, a subschema that says plenty, and no
    /// rationale on the entry.
    /// </summary>
    /// <remarks>
    /// The loader must name the keyword rather than merely fail, because the message a
    /// reviewer reads in a build log has to say which of the subschema's numbers went
    /// unjustified.
    /// </remarks>
    [Test]
    public void AStructuralBoundWithNoRationaleIsRejectedNamingTheKeyword()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(
            FixtureCorpus.Root, "schema", "structural-bound-without-rationale.schema.json"));

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/structural-bound-without-rationale.schema.json");
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                "a structural bound with no rationale is a limit nobody can justify, which is "
                    + "indistinguishable from one chosen to make something pass");
            Assert.That(
                load.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("minLength"),
                () => "the diagnostic must name the keyword: "
                    + string.Join("; ", load.Diagnostics));
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("rationale"),
                () => "the diagnostic must name what is missing: "
                    + string.Join("; ", load.Diagnostics));
            Assert.That(
                walk.MissingRationales,
                Is.EqualTo(new[] { "/properties/presentation_id/minLength" }),
                () => "the structure-blind walk must reach the same conclusion at the same "
                    + "pointer: " + string.Join(", ", walk.MissingRationales));
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                () => "the bound is attributed; what it lacks is a reason: "
                    + string.Join(", ", walk.Unattributed));
        });
    }

    /// <summary>
    /// Two structural bounds under one <c>description</c> fail for <em>both</em>, naming
    /// both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control with two of the guarded thing where only one answer was ever supplied -
    /// the shape <c>content/schemas/README.md</c> now requires of any change to this gate,
    /// and the one that was missing when the rationale rule was written. Under the rule this
    /// replaced, <c>"description": "the envelope is bounded"</c> satisfied the requirement
    /// for the 1 and the 4096 alike.
    /// </para>
    /// <para>
    /// Both halves have to be reported in one run. Reporting the first and stopping would
    /// make the diagnostic per annotation where the guarded thing is a bound: the reviewer
    /// repairs the named half, the other surfaces on the next run, and it reads as a second
    /// defect rather than the rest of the first.
    /// </para>
    /// </remarks>
    [Test]
    public void TwoStructuralBoundsSharingOneDescriptionAreBothReported()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(
            FixtureCorpus.Root, "schema", "shared-description-two-structural-bounds.schema.json"));

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            bytes, "tests/fixtures/schema/shared-description-two-structural-bounds.schema.json");
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                "one description cannot justify two numbers, and it used to be accepted as "
                    + "justifying both");
            Assert.That(
                ConstraintsOf(load),
                Has.Exactly(1).Contains("minLength"),
                () => "minLength must be reported once, by name: "
                    + string.Join("; ", load.Diagnostics));
            Assert.That(
                ConstraintsOf(load),
                Has.Exactly(1).Contains("maxLength"),
                () => "maxLength must be reported too - a check that stopped at the first "
                    + "unjustified bound would be per annotation where the guarded thing is a "
                    + "bound: " + string.Join("; ", load.Diagnostics));
            Assert.That(
                walk.MissingRationales,
                Is.EqualTo(new[]
                {
                    "/properties/presentation_id/minLength",
                    "/properties/presentation_id/maxLength",
                }),
                () => "the walk must report both, in the order the keywords are declared: "
                    + string.Join(", ", walk.MissingRationales));
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                () => "both bounds are attributed; neither is justified: "
                    + string.Join(", ", walk.Unattributed));
        });
    }

    private static IReadOnlyList<string> ConstraintsOf(JsonSchemaLoadResult load)
    {
        List<string> constraints = new();
        foreach (ContentDiagnostic diagnostic in load.Diagnostics)
        {
            constraints.Add(diagnostic.ExpectedConstraint);
        }

        return constraints;
    }

    /// <summary>
    /// Walks a schema document and returns the JSON Pointer of every mandatory bound that
    /// has no <c>x-authority</c> beside it.
    /// </summary>
    private static IReadOnlyList<string> FindUnattributedBounds(byte[] schemaBytes)
    {
        return SchemaBoundWalk.Of(schemaBytes).Unattributed;
    }

    /// <summary>
    /// Walks a schema document and returns the JSON Pointer of every sourced or derived
    /// bound whose authority states no derivation.
    /// </summary>
    private static IReadOnlyList<string> FindMissingDerivations(byte[] schemaBytes)
    {
        return SchemaBoundWalk.Of(schemaBytes).MissingDerivations;
    }

    /// <summary>
    /// Walks a schema document and returns the JSON Pointer of every structural bound whose
    /// authority states no rationale.
    /// </summary>
    private static IReadOnlyList<string> FindMissingRationales(byte[] schemaBytes)
    {
        return SchemaBoundWalk.Of(schemaBytes).MissingRationales;
    }
}
