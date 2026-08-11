using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Ids;
using MechaMiner.Content.Schema;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The gate over <see cref="SchemaCorpusDivergence"/>: the disagreement between
/// <c>content/schemas/</c> and the definitions under <c>content/</c> is pinned as a set of
/// positions, not as a count, and both directions of the pin are controlled.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a set and not a count.</b> A scalar over a set is blind to substitution. If the
/// figure for one schema is 177, then 177 different errors passes, a violation moving from
/// one file to another passes, and a field's error moving from <c>required</c> to
/// <c>type</c> - a change in what is actually wrong - passes. A monotone bound on the
/// cardinality of a set says nothing about its membership, and membership is what a
/// reviewer needs in order to decide whether a change was progress. So the whole
/// normalised set is committed, as
/// <c>Goldens/schema-corpus-divergence.txt</c>, and a substitution shows up as one line
/// removed and one line added instead of as silence. The full set is committed rather than
/// a hash of it for the same reason: a hash reddens without saying what moved, and the
/// next person's first question is what moved.
/// </para>
/// <para>
/// <b>Both directions are controlled, because a ratchet only ever watched while the number
/// falls has demonstrated that it tolerates improvement, which is not what it exists to
/// catch.</b> <see cref="InjectingOneUndeclaredFieldAddsExactlyThatPositionAndBreaksThePin"/>
/// grows the set and confirms the pin breaks and names the new member.
/// <see cref="SubstitutingOneViolationForAnotherKeepsTheErrorCountAndBreaksThePin"/> swaps
/// one violation for a different one so the total is unchanged, and confirms the pin still
/// breaks - which is the control that distinguishes a pinned set from a count wearing a
/// set's name. If that test ever passes green, the pin has become a count somewhere and
/// the point has been lost.
/// </para>
/// <para>
/// <b>Both controls mutate a copy, never <c>content/</c>.</b> The corpus belongs to the
/// catalog stream and this package may read it and not write it, so
/// <see cref="MutatedCorpus"/> copies the tree into <c>artifacts/</c> and mutates the copy.
/// That is also the stronger arrangement: an in-place control is a control whose green run
/// depends on somebody remembering to revert.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-006-001</c> through <c>VER-DAT-006-004</c> in
/// <c>tests/verification/DAT-006.json</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaCorpusDivergenceTests
{
    /// <summary>The committed baseline.</summary>
    private const string GoldenName = "schema-corpus-divergence.txt";

    /// <summary>The header the position lines follow in the golden.</summary>
    private const string PositionHeader =
        "## positions: schema | corpus file | pointer | keyword | occurrences";

    /// <summary>
    /// The eight resource definitions whose id and <c>resource_class</c> the catalog
    /// stream's rename replaces.
    /// </summary>
    private static readonly string[] RenamedResourceFiles =
    {
        "A.json", "B.json", "C.json", "D.json", "E.json", "F.json",
        "common-ore.json", "hyper-gold.json",
    };

    /// <summary>
    /// The three positions each renamed resource contributes, which the rename removes.
    /// </summary>
    /// <remarks>
    /// Stated as a rule over the eight files rather than as twenty-four pasted strings, so
    /// the prediction is derived and a file leaving the list takes its three positions with
    /// it. <c>/id</c> fails <c>pattern</c> because the authored id is a letter or a word
    /// where the schema states <c>^RSC-[0-9]{2}$</c>; <c>/resource_class</c> fails
    /// <c>enum</c> because the authored value is the document's prose class rather than the
    /// token; and the root fails <c>oneOf</c> because both discriminator arms are keyed on
    /// that token.
    /// </remarks>
    private static readonly (string Pointer, string Keyword)[] RenamePredictedPositions =
    {
        ("(root)", "oneOf"),
        ("/id", "pattern"),
        ("/resource_class", "enum"),
    };

    /// <summary>
    /// The measurement over the real tree, computed once: sixteen schema loads and 138
    /// evaluations is not work to repeat per test case.
    /// </summary>
    private static SchemaCorpusDivergence Measured => Lazy.Value;

    private static readonly Lazy<SchemaCorpusDivergence> Lazy =
        new(() => SchemaCorpusDivergence.Measure(TestArtifacts.RepositoryRoot));

    /// <summary>
    /// The measured divergence is exactly the committed baseline, position by position.
    /// </summary>
    /// <remarks>
    /// This is the pin. It fails when a position appears, when one disappears, when one
    /// moves file, pointer or keyword, and when the number of times a position is reported
    /// changes. It does not fail when the evaluator's prose changes, because no message is
    /// in the normal form.
    /// </remarks>
    [Test]
    public void TheDivergenceIsExactlyThePinnedBaseline()
    {
        GoldenText.Matches(GoldenName, Measured.Render());
    }

    /// <summary>
    /// Every <c>*.schema.json</c> on disk is either exercised against a corpus, named as
    /// having no corpus, or named as excluded - and nothing is in two of those states.
    /// </summary>
    /// <remarks>
    /// The anti-vacuity assertion this gate needs most. A measurement that iterates corpus
    /// directories cannot report a schema whose corpus does not exist, so the enumeration
    /// runs over schemas and the schemas on disk are the independent anchor. Without this,
    /// <c>player-baseline.schema.json</c> - which governs zero definitions - would be
    /// absent from the report and its absence would be indistinguishable from agreement.
    /// </remarks>
    [Test]
    public void TheSchemasExercisedPlusTheNamedExceptionsAreExactlyTheSchemasOnDisk()
    {
        List<string> onDisk = new();
        foreach (string path in Directory.GetFiles(
                     Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas"),
                     "*.schema.json",
                     SearchOption.AllDirectories))
        {
            onDisk.Add(TestArtifacts.Relative(path));
        }

        List<string> accounted = new();
        foreach (string name in Measured.Exercised)
        {
            accounted.Add("content/schemas/" + name);
        }

        accounted.AddRange(Measured.NoCorpus);
        accounted.AddRange(Measured.Excluded);

        Expect.Multiple(() =>
        {
            Assert.That(
                accounted,
                Is.EquivalentTo(onDisk),
                "every schema under content/schemas/ must be exercised, named as having no "
                    + "authored corpus, or named as excluded. A schema in none of the three is "
                    + "one this gate silently says nothing about");
            Assert.That(
                accounted,
                Is.Unique,
                "a schema in two of the three states would be reported and exempted at once");
            Assert.That(
                onDisk,
                Is.Not.Empty,
                "content/schemas/ holds no schema, so every walk over it proves nothing");
        });
    }

    /// <summary>
    /// Every exercised schema has at least one definition, and every authored definition is
    /// governed by exactly one schema.
    /// </summary>
    /// <remarks>
    /// The second half is the check that survives a new category directory. The bindings say
    /// which files a schema governs; <see cref="AuthoredCorpus.EveryAuthoredDefinition"/>
    /// walks the tree and says which files exist. A definition nothing is bound to appears
    /// in the second and in neither corpus, and a definition bound twice would be counted
    /// twice, so both failures are named rather than absorbed.
    /// </remarks>
    [Test]
    public void EveryExercisedSchemaHasACorpusAndEveryDefinitionIsGovernedExactlyOnce()
    {
        List<string> governed = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            if (AuthoredCorpus.NoCorpus.ContainsKey(descriptor.Kind))
            {
                continue;
            }

            IReadOnlyList<string> files = AuthoredCorpus.FilesOf(
                descriptor.Kind, TestArtifacts.RepositoryRoot);
            Assert.That(
                files,
                Is.Not.Empty,
                descriptor.SchemaPath + " is exercised against no definition, so its row in the "
                    + "baseline would be an empty measurement reading as agreement. A kind with "
                    + "no corpus belongs in AuthoredCorpus.NoCorpus with the reason");
            governed.AddRange(files);
        }

        IReadOnlyList<string> authored = AuthoredCorpus.EveryAuthoredDefinition(
            TestArtifacts.RepositoryRoot);

        Expect.Multiple(() =>
        {
            Assert.That(
                governed,
                Is.Unique,
                "a definition governed by two schemas would be measured twice");
            Assert.That(
                governed,
                Is.EquivalentTo(authored),
                "the definitions this gate measures are not the definitions on disk. A file in "
                    + "content/ that no schema is bound to is a file no schema is checked "
                    + "against");
            Assert.That(
                authored,
                Is.Not.Empty,
                "there are no authored definitions, so the whole measurement is vacuous");
        });
    }

    /// <summary>
    /// A schema named as having no authored corpus really has none - checked against the
    /// directory, not taken from the list.
    /// </summary>
    [Test]
    public void TheNoCorpusSchemasReallyHaveNoAuthoredDefinition()
    {
        Assert.That(
            AuthoredCorpus.NoCorpus,
            Is.Not.Empty,
            "the no-corpus list is empty, so either every schema now governs a definition - in "
                + "which case the previous entry moved into the measured rows and this "
                + "expectation should be inverted - or the list stopped being maintained");

        Expect.Multiple(() =>
        {
            foreach (KeyValuePair<DefinitionKind, string> entry in AuthoredCorpus.NoCorpus)
            {
                CategoryDescriptor descriptor = CategorySchemas.Describe(entry.Key);
                string directory = Path.Combine(
                    TestArtifacts.RepositoryRoot,
                    "content",
                    ContentCategories.Describe(descriptor.Category).DirectoryName);

                Assert.That(
                    AuthoredCorpus.FilesOf(entry.Key, TestArtifacts.RepositoryRoot),
                    Is.Empty,
                    descriptor.SchemaPath + " is named as having no authored corpus but "
                        + TestArtifacts.Relative(directory) + " holds definitions, so the schema "
                        + "is being exempted from a measurement it could take");
                Assert.That(
                    entry.Value.Length,
                    Is.GreaterThan(40),
                    descriptor.SchemaPath + " must say why it has no corpus in terms a reviewer "
                        + "can check, not carry a label");
            }
        });
    }

    /// <summary>
    /// The envelope's exclusion is measured rather than asserted: pointed at whole
    /// definitions it rejects every one of them for its category fields, which is an
    /// artefact of the pairing and not a finding.
    /// </summary>
    /// <remarks>
    /// An exclusion with a reason nobody rechecks is how a real divergence gets parked. This
    /// runs the excluded schema over the whole corpus and holds it to the claim the
    /// exclusion makes: no file passes, and every file's rejection includes at least one
    /// undeclared-property error. If the envelope ever stopped rejecting everything, the
    /// exclusion's premise would be gone and this would say so.
    /// </remarks>
    [Test]
    public void TheEnvelopeExclusionIsTheArtefactItClaimsToBe()
    {
        Assert.That(
            AuthoredCorpus.Excluded.ContainsKey("envelope.schema.json"),
            Is.True,
            "the envelope is the exclusion this test is about");

        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", "envelope.schema.json");
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            File.ReadAllBytes(path), "content/schemas/envelope.schema.json");
        Assert.That(load.IsValid, Is.True, "the envelope schema must load");

        List<string> clean = new();
        List<string> withoutUndeclaredField = new();
        IReadOnlyList<string> authored = AuthoredCorpus.EveryAuthoredDefinition(
            TestArtifacts.RepositoryRoot);

        foreach (string file in authored)
        {
            IReadOnlyList<JsonSchemaError> errors = SchemaCorpusDivergence.Evaluate(
                load.Schema!, TestArtifacts.RepositoryRoot, file);
            if (errors.Count == 0)
            {
                clean.Add(file);
                continue;
            }

            bool undeclared = false;
            foreach (JsonSchemaError error in errors)
            {
                if (string.Equals(error.Keyword, "additionalProperties", StringComparison.Ordinal))
                {
                    undeclared = true;
                    break;
                }
            }

            if (!undeclared)
            {
                withoutUndeclaredField.Add(file);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                authored.Count,
                Is.GreaterThan(0),
                "the envelope was run over nothing, so the exclusion's premise was not measured");
            Assert.That(
                clean,
                Is.Empty,
                () => "the envelope now accepts " + clean.Count + " definition(s), so it no "
                    + "longer rejects everything and its exclusion needs rereading: "
                    + string.Join(", ", clean));
            Assert.That(
                withoutUndeclaredField,
                Is.Empty,
                () => "the exclusion says every rejection is an undeclared category field, but "
                    + "these are rejected for something else and would be real findings: "
                    + string.Join(", ", withoutUndeclaredField));
        });
    }

    /// <summary>
    /// The upward control: one injected undeclared field adds exactly one position, and the
    /// pinned baseline no longer matches.
    /// </summary>
    /// <remarks>
    /// The violation is the smallest well-formed thing the schema forbids - a field the
    /// schema does not declare, under <c>additionalProperties: false</c>. The file stays
    /// parseable, so a red result cannot be the environment falling over rather than the
    /// gate working, which is doc 91 § Negative control adequacy's requirement.
    /// </remarks>
    [Test]
    public void InjectingOneUndeclaredFieldAddsExactlyThatPositionAndBreaksThePin()
    {
        const string target = "content/relics/REL-01.json";
        string root = MutatedCorpus("injected-undeclared-field", target, document =>
        {
            document["control_injected_field"] = "an undeclared field";
        });

        SchemaCorpusDivergence mutated = SchemaCorpusDivergence.Measure(root);
        HashSet<string> added = new(mutated.Lines(), StringComparer.Ordinal);
        added.ExceptWith(Measured.Lines());
        HashSet<string> removed = new(Measured.Lines(), StringComparer.Ordinal);
        removed.ExceptWith(mutated.Lines());

        string expected =
            "relic.schema.json | " + target + " | /control_injected_field | additionalProperties | 1";

        Expect.Multiple(() =>
        {
            Assert.That(
                added,
                Is.EquivalentTo(new[] { expected }),
                "the injected field must add exactly its own position and nothing else");
            Assert.That(removed, Is.Empty, "the injection must remove nothing");
            Assert.That(
                mutated.ErrorCount,
                Is.EqualTo(Measured.ErrorCount + 1),
                "one injected violation is one more error");
            Assert.That(
                mutated.Render(),
                Is.Not.EqualTo(CommittedBaselineText()),
                "the pinned baseline must not match a corpus with a new violation in it. A "
                    + "green pin here is a pin that tolerates the direction it exists to catch");
        });
    }

    /// <summary>
    /// The substitution control: one violation is replaced by a different one, the error
    /// count does not move, and the pinned baseline still breaks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deleting <c>affected_scope</c> from a relic removes the <c>type</c> failure at
    /// <c>/affected_scope</c> - the authored value is a string where the schema states an
    /// array - and adds a <c>required</c> failure at the root, where seven already sit. The
    /// cardinality is identical, so a per-schema count, a whole-corpus total, and a per-file
    /// count are all green on it. This is the case the count-based design missed, and it is
    /// the reason the pin is a set.
    /// </para>
    /// <para>
    /// It is also the substitution most likely to happen for real: a field is renamed or
    /// moved, and its old error is replaced by a new one somewhere else in the same file.
    /// </para>
    /// </remarks>
    [Test]
    public void SubstitutingOneViolationForAnotherKeepsTheErrorCountAndBreaksThePin()
    {
        const string target = "content/relics/REL-01.json";
        string root = MutatedCorpus("substituted-violation", target, document =>
        {
            Assert.That(
                document.Remove("affected_scope"),
                Is.True,
                "the substitution control needs the field it removes to be there");
        });

        SchemaCorpusDivergence mutated = SchemaCorpusDivergence.Measure(root);
        HashSet<string> added = new(mutated.Lines(), StringComparer.Ordinal);
        added.ExceptWith(Measured.Lines());
        HashSet<string> removed = new(Measured.Lines(), StringComparer.Ordinal);
        removed.ExceptWith(mutated.Lines());

        Expect.Multiple(() =>
        {
            Assert.That(
                mutated.ErrorCount,
                Is.EqualTo(Measured.ErrorCount),
                "the substitution must not change the error count, or it is not a substitution "
                    + "and this test would prove nothing a count could not");
            Assert.That(
                removed,
                Is.EquivalentTo(new[]
                {
                    "relic.schema.json | " + target + " | /affected_scope | type | 1",
                    "relic.schema.json | " + target + " | (root) | required | 7",
                }),
                "the type failure at the deleted field leaves, and the root required position "
                    + "leaves at its old count");
            Assert.That(
                added,
                Is.EquivalentTo(new[]
                {
                    "relic.schema.json | " + target + " | (root) | required | 8",
                }),
                "the root required position returns one occurrence heavier");
            Assert.That(
                mutated.Render(),
                Is.Not.EqualTo(CommittedBaselineText()),
                "THE control. A same-cardinality substitution must break the pin. If this "
                    + "passes, the baseline is a count in disguise and the substitution "
                    + "blindness the set exists to remove has come back");
        });
    }

    /// <summary>
    /// What the pin cannot see, asserted rather than described: two missing required
    /// properties trading places at one object does not move the measurement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The normal form ends at (file, pointer, keyword, count), and <c>required</c> reports
    /// every missing property at the object that lacks it. So satisfying one required field
    /// while breaking another at the same object is one position with the same count, and
    /// the baseline is silent. This test performs that swap and asserts the silence, so the
    /// limit is a committed measurement that fails the day somebody narrows the normal form,
    /// rather than a caveat in prose that outlives its truth.
    /// </para>
    /// <para>
    /// Closing it needs a discriminator naming the missing property. Today that name exists
    /// only inside <c>JsonSchemaError.Message</c>, and a normal form built on message text
    /// reddens the whole baseline when the evaluator's wording is edited - which is how a
    /// golden stops being read. The gap is recorded instead.
    /// </para>
    /// </remarks>
    [Test]
    public void TheBaselineIsBlindToASwapWithinOnePosition()
    {
        const string target = "content/relics/REL-01.json";
        string root = MutatedCorpus("swap-within-one-position", target, document =>
        {
            // sale_value_common_ore is required and absent, and its declaration is a
            // non-negative integer, so a zero satisfies it without adding any other failure.
            document["sale_value_common_ore"] = 0;

            // status is required and present, and its authored value satisfies its
            // declaration, so removing it adds exactly one required failure and removes none.
            Assert.That(
                document.Remove("status"),
                Is.True,
                "the swap needs the field it removes to be there");
        });

        SchemaCorpusDivergence mutated = SchemaCorpusDivergence.Measure(root);

        Expect.Multiple(() =>
        {
            Assert.That(
                mutated.ErrorCount,
                Is.EqualTo(Measured.ErrorCount),
                "the swap is one required failure for another, so the count cannot move");
            Assert.That(
                mutated.Render(),
                Is.EqualTo(Measured.Render()),
                "this is the documented blind spot: the baseline does not distinguish which "
                    + "required properties are missing at one object. If this test fails, the "
                    + "normal form has been narrowed - which is an improvement, and both this "
                    + "test and SchemaCorpusDivergence's remarks need rewriting to match");
        });
    }

    /// <summary>
    /// The predicted rows name positions the baseline actually has, so the prediction cannot
    /// point at rows that are not there.
    /// </summary>
    /// <remarks>
    /// The catalog stream's resource rename is expected to have landed on
    /// <c>origin/master</c> already: a third derivation reports 1,891 errors against that
    /// ref's corpus at <c>e17b8b6</c> where this branch's corpus gives 1,927 under the same
    /// library and traversal. Measured independently here by reading that ref's tree
    /// read-only out of this clone's object store, the whole 36-error gap is 24 resource
    /// positions plus 13 in <c>utility</c> and one fewer in
    /// <c>map-generation-contract</c>. Rather than a note saying "24 rows will drop", which
    /// is unfalsifiable at the moment it matters, the 24 are named here and
    /// <see cref="AnyPositionThatLeftTheBaselineIsExactlyTheOnesPredictedToLeave"/> asserts
    /// the relationship when the drop happens. A prediction with a committed subset is a
    /// control; a prediction without one is a caveat that fits whatever occurred.
    /// </remarks>
    [Test]
    public void ThePredictedResourceRenameRowsAreRowsTheBaselineActuallyHas()
    {
        IReadOnlyCollection<string> predicted = PredictedRenamePositions();
        HashSet<string> present = new(StringComparer.Ordinal);
        foreach (DivergencePosition position in Measured.Positions)
        {
            present.Add(position.Key);
        }

        List<string> missing = new();
        foreach (string key in predicted)
        {
            if (!present.Contains(key))
            {
                missing.Add(key);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                predicted,
                Is.Not.Empty,
                "an empty prediction is satisfied by every outcome");
            Assert.That(
                missing,
                Is.Empty,
                () => "the prediction names positions the baseline does not have, so it is "
                    + "predicting the disappearance of rows that already went: "
                    + string.Join("; ", missing));
        });
    }

    /// <summary>
    /// The prediction, cashed today: applying the rename to a copy of the corpus removes
    /// exactly the predicted positions and adds none.
    /// </summary>
    /// <remarks>
    /// Without this the prediction would be a promise that can only be tested once, on
    /// somebody else's merge. The mutation is the three authored changes the rename makes -
    /// the id becomes <c>RSC-0n</c>, <c>resource_class</c> becomes its token, and a
    /// specialized material carries the <c>canonical_letter</c> its arm requires - and the
    /// resulting resource rows are byte-identical to the rows <c>origin/master</c>'s corpus
    /// produces at <c>e17b8b6</c>, measured out of this clone's object store at
    /// <c>59a4986</c>. That is what licenses calling this a simulation of the rename rather
    /// than merely a mutation that happens to remove the right rows.
    /// </remarks>
    [Test]
    public void TheResourceRenameSimulationRemovesExactlyThePredictedRowsAndAddsNone()
    {
        string root = CopyCorpus("resource-rename-simulation");
        string[] tokens =
        {
            "RSC-01", "RSC-02", "RSC-03", "RSC-04", "RSC-05", "RSC-06", "RSC-07", "RSC-08",
        };

        for (int index = 0; index < RenamedResourceFiles.Length; index++)
        {
            string file = "content/resources/" + RenamedResourceFiles[index];
            string letter = Path.GetFileNameWithoutExtension(RenamedResourceFiles[index]);
            Mutate(root, file, document =>
            {
                document["id"] = tokens[index];
                string authored = (string?)document["resource_class"] ?? string.Empty;
                switch (authored)
                {
                    case "specialized ordinary resource":
                        document["resource_class"] = "specialized-material";
                        document["canonical_letter"] = letter;
                        break;
                    case "ordinary crafting resource":
                        document["resource_class"] = "common-ore";
                        break;
                    case "cross-run progression resource":
                        document["resource_class"] = "hyper-gold";
                        break;
                    default:
                        Assert.Fail(
                            file + " authors resource_class '" + authored + "', which this "
                                + "simulation does not know how to rename. The rename this "
                                + "prediction is about has changed shape");
                        break;
                }
            });
        }

        SchemaCorpusDivergence mutated = SchemaCorpusDivergence.Measure(root);
        HashSet<string> removed = new(StringComparer.Ordinal);
        foreach (DivergencePosition position in Measured.Positions)
        {
            removed.Add(position.Key);
        }

        HashSet<string> after = new(StringComparer.Ordinal);
        foreach (DivergencePosition position in mutated.Positions)
        {
            after.Add(position.Key);
        }

        HashSet<string> added = new(after, StringComparer.Ordinal);
        added.ExceptWith(removed);
        removed.ExceptWith(after);

        Expect.Multiple(() =>
        {
            Assert.That(
                removed,
                Is.EquivalentTo(PredictedRenamePositions()),
                "the rename must remove exactly the positions the prediction names");
            Assert.That(
                added,
                Is.Empty,
                () => "the rename must not add a position: " + string.Join("; ", added));
        });
    }

    /// <summary>
    /// When a position leaves the baseline, it is exactly the set predicted to leave.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The committed golden is the previous baseline, so the removal set is computable against
    /// it without anybody recording a number by hand.
    /// </para>
    /// <para>
    /// <b>This assertion is vacuous at this sha, and says so where it is read rather than only
    /// in a registry entry.</b> The removal set is empty - the measured positions are exactly
    /// the pinned ones - so the equality below is never evaluated and a green result here is
    /// the absence of a removal, not a verified prediction. What carries the prediction today
    /// is elsewhere and is named here so a reader does not have to find it:
    /// <see cref="ThePredictedResourceRenameRowsAreRowsTheBaselineActuallyHas"/> requires all
    /// twenty-four predicted positions to be present in the current baseline, so the
    /// prediction cannot quietly become a list of rows that already went, and
    /// <see cref="TheResourceRenameSimulationRemovesExactlyThePredictedRowsAndAddsNone"/>
    /// applies the rename to a copy and exercises this very equality against a non-empty
    /// removal set. This test becomes load-bearing the moment a row leaves, which is the
    /// moment it exists for.
    /// </para>
    /// </remarks>
    [Test]
    public void AnyPositionThatLeftTheBaselineIsExactlyTheOnesPredictedToLeave()
    {
        HashSet<string> pinned = new(StringComparer.Ordinal);
        foreach (string line in CommittedBaselinePositionLines())
        {
            int lastSeparator = line.LastIndexOf(" | ", StringComparison.Ordinal);
            pinned.Add(lastSeparator < 0 ? line : line[..lastSeparator]);
        }

        HashSet<string> measured = new(StringComparer.Ordinal);
        foreach (DivergencePosition position in Measured.Positions)
        {
            measured.Add(position.Key);
        }

        HashSet<string> removed = new(pinned, StringComparer.Ordinal);
        removed.ExceptWith(measured);

        Assert.That(
            pinned,
            Is.Not.Empty,
            "the committed baseline holds no position, so this comparison is against nothing");

        if (removed.Count == 0)
        {
            // Vacuous, deliberately and visibly: nothing has left the baseline, so there is no
            // equality to check. The two tests named in the remarks are what hold the
            // prediction until something does leave.
            Assert.Pass(
                "no position has left the baseline at this sha, so the predicted-removal "
                + "equality was not evaluated. The prediction is carried meanwhile by "
                + nameof(ThePredictedResourceRenameRowsAreRowsTheBaselineActuallyHas)
                + ", which requires all " + PredictedRenamePositions().Count
                + " predicted positions to be present, and by "
                + nameof(TheResourceRenameSimulationRemovesExactlyThePredictedRowsAndAddsNone)
                + ", which exercises this equality on a mutated copy.");
        }

        Assert.That(
            removed,
            Is.EquivalentTo(PredictedRenamePositions()),
            "positions left the baseline that were not predicted to leave, or the predicted "
                + "ones left alongside others. Either way the drop is not the one that was "
                + "expected and the difference is what to look at rather than the total");
    }

    /// <summary>The twenty-four predicted positions, as position keys.</summary>
    private static IReadOnlyCollection<string> PredictedRenamePositions()
    {
        List<string> keys = new();
        foreach (string file in RenamedResourceFiles)
        {
            foreach ((string Pointer, string Keyword) position in RenamePredictedPositions)
            {
                keys.Add(
                    "resource.schema.json | content/resources/" + file + " | "
                    + position.Pointer + " | " + position.Keyword);
            }
        }

        return keys;
    }

    /// <summary>The committed baseline's text, normalized the way the golden comparison does.</summary>
    /// <remarks>
    /// An absent golden fails with its own message rather than an <c>IOException</c> from
    /// somewhere inside the assertion. The two states a reader must be able to tell apart are
    /// "the pinned property is violated" and "the measurement never happened", and an
    /// unhandled file-not-found collapses the second into the first: the run is red either
    /// way, and the stack trace names an assertion that never ran. That collapse is the one
    /// this project has been bitten by before, so the baseline's absence is reported as the
    /// absence of the baseline.
    /// </remarks>
    private static string CommittedBaselineText()
    {
        string path = Path.Combine(TestArtifacts.TestProjectDirectory, "Goldens", GoldenName);
        if (!File.Exists(path))
        {
            // Thrown rather than Assert.Fail'd: inside Expect.Multiple a recorded failure does
            // not abort, so the read below would still throw and the run would report the
            // absence twice - once in these words and once as an IOException with a stack in
            // the assertion that never ran. One clear failure is the whole point here.
            throw new InvalidOperationException(
                "the committed baseline " + TestArtifacts.Relative(path) + " is not on disk, so "
                + "nothing was compared. This is not a violation of the pinned property - it is "
                + "the property never having been evaluated. Restore the golden from version "
                + "control, or regenerate it deliberately with "
                + GoldenText.UpdateVariable + "=1 and review it before committing.");
        }

        return File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <summary>The committed baseline's position lines.</summary>
    private static IReadOnlyList<string> CommittedBaselinePositionLines()
    {
        List<string> lines = new();
        bool inPositions = false;
        foreach (string line in CommittedBaselineText().Split('\n'))
        {
            if (!inPositions)
            {
                inPositions = string.Equals(line, PositionHeader, StringComparison.Ordinal);
                continue;
            }

            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>Copies the content tree into <c>artifacts/</c> and returns the copy's root.</summary>
    private static string CopyCorpus(string controlName)
    {
        string root = Path.Combine(
            TestArtifacts.RepositoryRoot, "artifacts", "schema-corpus-controls", controlName);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        string source = Path.Combine(TestArtifacts.RepositoryRoot, "content");
        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(
                Path.Combine(root, "content", Path.GetRelativePath(source, directory)));
        }

        Directory.CreateDirectory(Path.Combine(root, "content"));
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(root, "content", Path.GetRelativePath(source, file)));
        }

        return root;
    }

    /// <summary>Copies the corpus and applies one mutation to one definition.</summary>
    private static string MutatedCorpus(
        string controlName,
        string corpusFile,
        Action<JsonObject> mutate)
    {
        string root = CopyCorpus(controlName);
        Mutate(root, corpusFile, mutate);
        return root;
    }

    /// <summary>Rewrites one definition beneath a copied corpus.</summary>
    private static void Mutate(string root, string corpusFile, Action<JsonObject> mutate)
    {
        string path = Path.Combine(root, corpusFile.Replace('/', Path.DirectorySeparatorChar));
        JsonNode? node = JsonNode.Parse(File.ReadAllText(path));
        Assert.That(node, Is.Not.Null, corpusFile + " must parse for a control to mutate it");
        JsonObject document = node!.AsObject();
        mutate(document);
        File.WriteAllText(
            path,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
