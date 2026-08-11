using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The ordering gate: every declared array in every valid fixture is emitted under the class its
/// declaration states, and the gate says how much it exercised.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline
/// grants two array treatments - "stable-ID sets in canonical ID order, and semantically ordered
/// arrays in their authored/explicit order" - and each has its own invariant, which is why this
/// fixture does not have one assertion applied to both.
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Stable-ID set.</b> The authored order carries nothing, so the invariant is that it cannot
/// be recovered from the output: every ordering of the array produces byte-identical canonical
/// bytes. That is doc 40:29's "original property order do[es] not affect ... payload hashes"
/// reaching inside an array.
/// </description></item>
/// <item><description>
/// <b>Semantically ordered array.</b> The invariant doc 40 states is order <em>preservation</em>:
/// <c>output_order == authored_order</c>, elementwise. That is asserted directly, as the primary
/// invariant, in <see cref="EveryOrderedArrayIsEmittedInItsAuthoredOrder"/>. "The output must
/// differ when the input is reordered" is a <em>consequence</em> of it and not the rule, and it
/// is the weaker statement in exactly the place it matters: it is vacuously true of an empty
/// array and of a one-element array, and this corpus is full of both.
/// </description></item>
/// </list>
/// <para>
/// <b>The arity condition is named, not assumed.</b> <c>at: ["3:30"]</c>,
/// <c>formations: ["Stream"]</c>, <c>debut_enemy_ids: []</c>, <c>scheduled_elites: []</c>,
/// <c>formation_events: []</c> and <c>cross_document_rules: []</c> all occur in the authored
/// tree, and a "reordering changes the output" assertion over any of them is true for no reason.
/// Every such assertion here runs only over occurrences
/// <see cref="CanonicalArrayOccurrence.IsReorderable"/> admits - at least two elements, at least
/// two of which differ - and <see cref="Census"/> pins how many of those each writer has, so a
/// run over none of them cannot report green. Doc 91 § Reach and arity: "Name the cardinality
/// the gate enforces, then write a control containing two of the guarded thing where only one
/// satisfies the rule."
/// </para>
/// <para>
/// <b>Where this gate is weak, stated rather than hidden.</b> Four writers of sixteen have a
/// reorderable stable-ID set in the corpus and ten have no declared ID set occurrence at all;
/// six have no reorderable semantically ordered array. Those are not gaps this fixture can close
/// by asserting harder - they are properties of what the twenty fixtures happen to author - so
/// they are written into <see cref="Census"/> as zeros and named in
/// <see cref="TheWritersWithNoReorderableOccurrenceAreNamed"/>. The set treatment <em>is</em>
/// exercised at arity three on every category through the shared envelope's <c>source_refs</c>,
/// in <c>EnvelopeArrayOrderEmissionTests</c>; that is one proof on a shared component, not
/// sixteen independent ones, and it is recorded that way.
/// </para>
/// <para>
/// <b>Which control catches which fault, because the two are not interchangeable.</b> Flipping
/// a field's declared class - <c>ArrayOrder.OrderedArray</c> to <c>ArrayOrder.IdSet</c> in a
/// field table - cannot be caught by any behavioural assertion here, and that is a property of
/// the design rather than a hole in the gate: the writer and this fixture read the same
/// declaration, so a flip moves the field into the other bucket in both at once and each side
/// then agrees with the other about the wrong answer. What catches it is
/// <see cref="Census"/> and the pinned assertion counts, which is the whole reason they are
/// literals. Twenty-one such flips were run, one or two per writer in both directions, and every
/// one was red on the census.
/// </para>
/// <para>
/// The fault the behavioural assertions do catch is a writer that stops reading the declaration
/// - the only place that could now happen is the single <c>ArrayOrderEmitter</c> call in
/// <c>DefinitionWriter.WriteArray</c>. Hardcoding <c>ArrayOrder.IdSet</c> there reddens
/// <see cref="EveryOrderedArrayIsEmittedInItsAuthoredOrder"/> naming eight writers - Mech,
/// Enemy, EliteModifiers, Boss, MiningSite, MapGenerationContract, Weapon, Utility - and
/// hardcoding <c>ArrayOrder.OrderedArray</c> reddens
/// <see cref="EveryStableIdSetIsEmittedInCanonicalIdOrder"/> and
/// <see cref="PermutingAStableIdSetDoesNotChangeTheCanonicalBytes"/> naming four - MiningSite,
/// Weapon, Branch, Unlock. Both directions are needed: the first catches a meaningful order
/// being sorted, the second catches a set failing to be sorted, and a suite that only ever ran
/// the first would leave the second fault green.
/// </para>
/// <para>
/// <b>Three writers are not independently proven by anything here.</b> <c>Resource</c>,
/// <c>PlayerBaseline</c> and <c>WeaponStatPriceFormula</c> declare no array field at all, so
/// their census rows are four zeros and no mutation of a declaration can redden them
/// individually. The only array they emit is the shared envelope's, so their only available
/// control is a mutation of the envelope writer, which reddens all sixteen at once and therefore
/// proves a shared component rather than any one of the three. <c>Relic</c> is a fourth partial
/// case: it has eight array occurrences and not one of them is reorderable, so it is proven by
/// the census and by the elementwise order comparison, and by no permutation.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalArrayOrderGateTests
{
    /// <summary>
    /// Declared array fields reachable from the sixteen field tables, counted once per category
    /// tree. Measured at <c>e655fbf</c>; the same 62 <c>DeclaredArrayOrderCoverageTests</c>
    /// counts, which is the 59 call sites plus three sub-shapes shared by two categories.
    /// </summary>
    private const int DeclaredArrayFields = 62;

    /// <summary>
    /// Declared array fields the valid fixture corpus actually authors a value for. Measured at
    /// <c>e655fbf</c>.
    /// </summary>
    private const int ReachedArrayFields = 55;

    /// <summary>
    /// The declared arrays no valid fixture authors, so no assertion here reaches them.
    /// </summary>
    /// <remarks>
    /// Committed as a list rather than as a count, and this is the ratchet: a field leaves this
    /// list only by a fixture authoring it, and a field joins it only by somebody writing its
    /// name into this file. A count alone would let one field slip out as another slipped in.
    /// Measured at <c>e655fbf</c>.
    /// </remarks>
    private static readonly string[] UnreachedArrayFields =
    {
        "Boss/ability/projectile/snapshot_at_creation",
        "Branch/global_attack_rate_mapping/affected_timings",
        "Branch/global_attack_rate_mapping/unaffected_timings",
        "Enemy/specialist_attack/rules",
        "MiningSite/beacon_rules",
        "MiningSite/beacon_thresholds",
        "MiningSite/spawn_exclusions",
    };

    /// <summary>
    /// What this gate exercises, per writer. Every number measured at <c>e655fbf</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One row per declared kind, present even when every number in it is zero, because a row
    /// that says "this writer has no reorderable ID set" is the honest reading of the gate's
    /// reach and an omitted row reads as "not applicable". <c>Resource</c>,
    /// <c>PlayerBaseline</c> and <c>WeaponStatPriceFormula</c> declare no array field at all, so
    /// all four of their numbers are zero and their rows exist to say so.
    /// </para>
    /// <para>
    /// The occurrence counts are of arrays the documents carry, not of declarations:
    /// <c>EncounterSchedule</c>'s one fixture accounts for 36 of the 42 set occurrences and 113
    /// of the 156 ordered ones, because a thirty-five-row schedule repeats the same handful of
    /// declarations once per row.
    /// </para>
    /// </remarks>
    private static readonly WriterCensus[] Census =
    {
        new(DefinitionKind.Resource, 2, 0, 0, 0, 0),
        new(DefinitionKind.Mech, 1, 0, 0, 2, 1),
        new(DefinitionKind.Enemy, 1, 0, 0, 2, 2),
        new(DefinitionKind.EliteModifiers, 1, 0, 0, 2, 2),
        new(DefinitionKind.Boss, 1, 0, 0, 2, 1),
        new(DefinitionKind.MiningSite, 2, 1, 1, 5, 3),
        new(DefinitionKind.EncounterSchedule, 1, 36, 0, 113, 39),
        new(DefinitionKind.MapGenerationContract, 1, 0, 0, 9, 2),
        new(DefinitionKind.PlayerBaseline, 1, 0, 0, 0, 0),
        new(DefinitionKind.Weapon, 1, 1, 1, 4, 2),
        new(DefinitionKind.WeaponStatPriceFormula, 1, 0, 0, 0, 0),
        new(DefinitionKind.Branch, 1, 1, 1, 1, 0),
        new(DefinitionKind.Utility, 2, 0, 0, 7, 3),
        new(DefinitionKind.Relic, 2, 2, 0, 6, 0),
        new(DefinitionKind.PowerUp, 1, 0, 0, 2, 1),
        new(DefinitionKind.Unlock, 1, 1, 1, 1, 0),
    };

    /// <summary>
    /// The writers with no reorderable stable-ID set occurrence in the corpus, so
    /// <see cref="PermutingAStableIdSetDoesNotChangeTheCanonicalBytes"/> asserts nothing about
    /// them. Measured at <c>e655fbf</c>.
    /// </summary>
    private static readonly DefinitionKind[] NoReorderableIdSet =
    {
        DefinitionKind.Resource,
        DefinitionKind.Mech,
        DefinitionKind.Enemy,
        DefinitionKind.EliteModifiers,
        DefinitionKind.Boss,
        DefinitionKind.EncounterSchedule,
        DefinitionKind.MapGenerationContract,
        DefinitionKind.PlayerBaseline,
        DefinitionKind.WeaponStatPriceFormula,
        DefinitionKind.Utility,
        DefinitionKind.Relic,
        DefinitionKind.PowerUp,
    };

    /// <summary>
    /// The writers with no reorderable semantically ordered array occurrence in the corpus, so
    /// <see cref="PermutingASemanticallyOrderedArrayChangesTheCanonicalBytes"/> asserts nothing
    /// about them. Measured at <c>e655fbf</c>.
    /// </summary>
    private static readonly DefinitionKind[] NoReorderableOrderedArray =
    {
        DefinitionKind.Resource,
        DefinitionKind.PlayerBaseline,
        DefinitionKind.WeaponStatPriceFormula,
        DefinitionKind.Branch,
        DefinitionKind.Relic,
        DefinitionKind.Unlock,
    };

    /// <summary>
    /// The census names every declared writer exactly once, so no writer can be dropped from
    /// the gate by deleting its row.
    /// </summary>
    [Test]
    public void TheCensusNamesEveryDeclaredWriterExactlyOnce()
    {
        List<DefinitionKind> censused = new();
        foreach (WriterCensus row in Census)
        {
            censused.Add(row.Kind);
        }

        List<DefinitionKind> declared = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            declared.Add(descriptor.Kind);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                censused,
                Is.EquivalentTo(declared),
                () => "the census must have one row per declared kind. Censused: "
                    + string.Join(", ", censused));
            Assert.That(
                new HashSet<DefinitionKind>(censused),
                Has.Count.EqualTo(Census.Length),
                "no kind may appear twice, or one row's numbers would answer for another's");
        });
    }

    /// <summary>
    /// What the gate walked equals what the census committed to, per writer and in total.
    /// </summary>
    /// <remarks>
    /// This is the anti-vacuity assertion the rest of the fixture stands on. Every other test
    /// here is a loop over occurrences, so all of them pass over an empty corpus; this one fails
    /// on an empty corpus, on a shrunken corpus, and on a corpus that grew without anybody
    /// saying so.
    /// </remarks>
    [Test]
    public void TheGateExercisesExactlyTheCensusItCommittedTo()
    {
        Dictionary<DefinitionKind, WriterCensus> measured = Measure();

        Expect.Multiple(() =>
        {
            int setOccurrences = 0;
            int setReorderable = 0;
            int orderedOccurrences = 0;
            int orderedReorderable = 0;

            foreach (WriterCensus expected in Census)
            {
                WriterCensus actual = measured[expected.Kind];
                Assert.That(
                    actual,
                    Is.EqualTo(expected),
                    () => expected.Kind + "'s exercised census must be the committed one. "
                        + "Committed " + expected + "; walked " + actual);

                setOccurrences += expected.SetOccurrences;
                setReorderable += expected.SetReorderable;
                orderedOccurrences += expected.OrderedOccurrences;
                orderedReorderable += expected.OrderedReorderable;
            }

            NumericAssert.AreExactlyEqual(
                CategoryFixtureCorpus.Valid.Count,
                CanonicalWriterSubject.All.Count,
                "subjects, one per valid fixture");
            NumericAssert.AreExactlyEqual(42, setOccurrences, "stable-ID set occurrences walked");
            NumericAssert.AreExactlyEqual(
                4, setReorderable, "of those, reorderable ones the set invariant can be tested on");
            NumericAssert.AreExactlyEqual(
                156, orderedOccurrences, "semantically ordered array occurrences walked");
            NumericAssert.AreExactlyEqual(
                56,
                orderedReorderable,
                "of those, reorderable ones the difference consequence can be tested on");
        });
    }

    /// <summary>
    /// A stable-ID set is emitted in canonical ID order, whatever order it was authored in.
    /// </summary>
    /// <remarks>
    /// Every element of every set-class array in this tree is a string - the set treatment has no
    /// meaning otherwise, and <c>ArrayOrderEmitter</c> refuses a non-string one - so the expected
    /// output is the ordinal sort of the authored elements, and it is compared elementwise rather
    /// than through a permutation. This holds at every arity, including one and zero, where it
    /// asserts that the elements are all there and unchanged.
    /// </remarks>
    [Test]
    public void EveryStableIdSetIsEmittedInCanonicalIdOrder()
    {
        int asserted = 0;

        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                JsonObject authored = subject.AuthoredRoot();
                JsonObject emitted = subject.CanonicalTreeOf(authored);

                foreach (CanonicalArrayOccurrence occurrence in
                    CanonicalArrayOccurrence.Of(subject.Shape, authored))
                {
                    if (occurrence.Order != ArrayOrder.IdSet)
                    {
                        continue;
                    }

                    asserted++;
                    List<string> expected = occurrence.ElementTexts();
                    expected.Sort(static (left, right) => string.CompareOrdinal(left, right));

                    Assert.That(
                        ElementTextsAt(emitted, occurrence.Path),
                        Is.EqualTo(expected),
                        () => subject + " " + occurrence
                            + " is a stable-ID set and must be emitted in canonical ID order");
                }
            }

            NumericAssert.AreExactlyEqual(42, asserted, "set-class occurrences asserted");
        });
    }

    /// <summary>
    /// A semantically ordered array is emitted in its authored order, elementwise. This is the
    /// primary invariant doc 40 states for the class.
    /// </summary>
    /// <remarks>
    /// The comparison is <c>output_order == authored_order</c> and not "the output differs when
    /// reordered", because the latter is a consequence and goes vacuous on the empty and
    /// one-element arrays this corpus is full of. Object elements are compared by their canonical
    /// emitted text against the emission of the same document with that one array reversed: if
    /// authored order is preserved, reversing the input reverses the output exactly, position for
    /// position. Text elements are compared directly against the authored strings, which needs no
    /// permutation at all and so holds at every arity.
    /// </remarks>
    [Test]
    public void EveryOrderedArrayIsEmittedInItsAuthoredOrder()
    {
        int assertedDirectly = 0;
        int assertedByReversal = 0;

        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                JsonObject authored = subject.AuthoredRoot();
                IReadOnlyList<CanonicalArrayOccurrence> occurrences =
                    CanonicalArrayOccurrence.Of(subject.Shape, authored);
                JsonObject baseline = subject.CanonicalTreeOf(authored);

                foreach (CanonicalArrayOccurrence occurrence in occurrences)
                {
                    if (occurrence.Order != ArrayOrder.OrderedArray)
                    {
                        continue;
                    }

                    if (occurrence.Field.Element!.Shape == FieldShape.Text)
                    {
                        assertedDirectly++;
                        Assert.That(
                            ElementTextsAt(baseline, occurrence.Path),
                            Is.EqualTo(occurrence.ElementTexts()),
                            () => subject + " " + occurrence
                                + " must be emitted in its authored order, unchanged");
                        continue;
                    }

                    List<string> emitted = ElementTextsAt(baseline, occurrence.Path);
                    occurrence.Reverse();
                    List<string> reversedEmission = ElementTextsAt(
                        subject.CanonicalTreeOf(authored), occurrence.Path);
                    occurrence.Reverse();

                    assertedByReversal++;
                    emitted.Reverse();
                    Assert.That(
                        reversedEmission,
                        Is.EqualTo(emitted),
                        () => subject + " " + occurrence
                            + " has object elements, and reversing the authored order must "
                            + "reverse the emitted order position for position");
                }
            }

            NumericAssert.AreExactlyEqual(
                156,
                assertedDirectly + assertedByReversal,
                "ordered-class occurrences asserted, every one of them");
            Assert.That(
                assertedDirectly,
                Is.GreaterThan(0),
                "no text-element ordered array was asserted directly");
            Assert.That(
                assertedByReversal,
                Is.GreaterThan(0),
                "no object-element ordered array was asserted by reversal");
        });
    }

    /// <summary>
    /// Reordering a stable-ID set does not change the canonical bytes at all.
    /// </summary>
    [Test]
    public void PermutingAStableIdSetDoesNotChangeTheCanonicalBytes()
    {
        int asserted = 0;

        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                JsonObject authored = subject.AuthoredRoot();
                IReadOnlyList<CanonicalArrayOccurrence> occurrences =
                    CanonicalArrayOccurrence.Of(subject.Shape, authored);
                byte[] baseline = subject.CanonicalOf(authored);

                foreach (CanonicalArrayOccurrence occurrence in occurrences)
                {
                    if (occurrence.Order != ArrayOrder.IdSet || !occurrence.IsReorderable)
                    {
                        continue;
                    }

                    asserted++;
                    occurrence.Reverse();
                    byte[] permuted = subject.CanonicalOf(authored);
                    occurrence.Reverse();

                    Assert.That(
                        permuted,
                        Is.EqualTo(baseline),
                        () => subject + " " + occurrence
                            + " is a stable-ID set, so reversing it must not change one byte");
                    Assert.That(
                        subject.CanonicalOf(authored),
                        Is.EqualTo(baseline),
                        () => subject + " " + occurrence
                            + ": reversing twice must restore the document exactly, or every "
                            + "later occurrence is measured against a tree nobody authored");
                }
            }

            NumericAssert.AreExactlyEqual(
                4,
                asserted,
                "reorderable set-class occurrences permuted. Four of sixteen writers have one; "
                    + "see NoReorderableIdSet for the twelve that do not");
        });
    }

    /// <summary>
    /// Reordering a semantically ordered array does change the canonical bytes - the consequence
    /// of order preservation, asserted only where the input really has two distinguishable
    /// orderings.
    /// </summary>
    [Test]
    public void PermutingASemanticallyOrderedArrayChangesTheCanonicalBytes()
    {
        int asserted = 0;

        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                JsonObject authored = subject.AuthoredRoot();
                IReadOnlyList<CanonicalArrayOccurrence> occurrences =
                    CanonicalArrayOccurrence.Of(subject.Shape, authored);
                byte[] baseline = subject.CanonicalOf(authored);

                foreach (CanonicalArrayOccurrence occurrence in occurrences)
                {
                    if (occurrence.Order != ArrayOrder.OrderedArray || !occurrence.IsReorderable)
                    {
                        continue;
                    }

                    asserted++;
                    Assert.That(
                        occurrence.Arity,
                        Is.GreaterThanOrEqualTo(2),
                        () => subject + " " + occurrence
                            + " was admitted as reorderable, so it must have at least two "
                            + "elements; one element has one ordering and proves nothing");

                    occurrence.Reverse();
                    byte[] permuted = subject.CanonicalOf(authored);
                    occurrence.Reverse();

                    Assert.That(
                        permuted,
                        Is.Not.EqualTo(baseline),
                        () => subject + " " + occurrence
                            + " is semantically ordered, so reversing it must change the bytes; "
                            + "identical bytes mean the writer sorted it");
                }
            }

            NumericAssert.AreExactlyEqual(
                56,
                asserted,
                "reorderable ordered-class occurrences permuted. Ten of sixteen writers have "
                    + "one; see NoReorderableOrderedArray for the six that do not");
        });
    }

    /// <summary>
    /// The writers each permutation assertion cannot reach are named, and the names agree with
    /// the census.
    /// </summary>
    /// <remarks>
    /// A gate's reach is part of its result. These two lists are what the census's zeros mean in
    /// words, and asserting that the lists and the numbers agree stops one of them being quietly
    /// corrected without the other: a writer removed from a list without gaining a fixture would
    /// contradict its own row.
    /// </remarks>
    [Test]
    public void TheWritersWithNoReorderableOccurrenceAreNamed()
    {
        List<DefinitionKind> setGaps = new();
        List<DefinitionKind> orderedGaps = new();
        foreach (WriterCensus row in Census)
        {
            if (row.SetReorderable == 0)
            {
                setGaps.Add(row.Kind);
            }

            if (row.OrderedReorderable == 0)
            {
                orderedGaps.Add(row.Kind);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                setGaps,
                Is.EquivalentTo(NoReorderableIdSet),
                () => "the named set-class gaps must be exactly the census's zeros. Census says: "
                    + string.Join(", ", setGaps));
            Assert.That(
                orderedGaps,
                Is.EquivalentTo(NoReorderableOrderedArray),
                () => "the named ordered-class gaps must be exactly the census's zeros. Census "
                    + "says: " + string.Join(", ", orderedGaps));
            Assert.That(
                setGaps,
                Has.Count.LessThan(Census.Length),
                "if every writer were a gap, both permutation tests would range over nothing");
            Assert.That(
                orderedGaps,
                Has.Count.LessThan(Census.Length),
                "if every writer were a gap, both permutation tests would range over nothing");
        });
    }

    /// <summary>
    /// Every declared array field is either authored by some valid fixture or named in the
    /// committed unreached list.
    /// </summary>
    /// <remarks>
    /// The partition is what stops the gate looking complete while a declaration nobody authors
    /// goes untested: a newly declared array is unreached, and an unreached array has to be
    /// written into <see cref="UnreachedArrayFields"/> by hand, which is a visible admission
    /// rather than a silent omission.
    /// </remarks>
    [Test]
    public void EveryDeclaredArrayIsEitherReachedByTheCorpusOrNamedAsUnreached()
    {
        List<string> declared = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            CollectDeclared(descriptor.Kind.ToString(), descriptor.Shape, declared);
        }

        HashSet<string> reached = new(StringComparer.Ordinal);
        foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
        {
            foreach (CanonicalArrayOccurrence occurrence in
                CanonicalArrayOccurrence.Of(subject.Shape, subject.AuthoredRoot()))
            {
                reached.Add(subject.Kind + WithoutIndices(occurrence.Path));
            }
        }

        List<string> unreached = new();
        foreach (string field in declared)
        {
            if (!reached.Contains(field))
            {
                unreached.Add(field);
            }
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                DeclaredArrayFields, declared.Count, "declared array fields walked");
            NumericAssert.AreExactlyEqual(
                ReachedArrayFields,
                declared.Count - unreached.Count,
                "declared array fields the corpus authors");
            Assert.That(
                unreached,
                Is.EquivalentTo(UnreachedArrayFields),
                () => "these declared arrays are authored by no valid fixture and must be named: "
                    + string.Join(", ", unreached));
        });
    }

    private static Dictionary<DefinitionKind, WriterCensus> Measure()
    {
        Dictionary<DefinitionKind, WriterCensus> measured = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            measured[descriptor.Kind] = new WriterCensus(descriptor.Kind, 0, 0, 0, 0, 0);
        }

        foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
        {
            WriterCensus row = measured[subject.Kind] with
            {
                Fixtures = measured[subject.Kind].Fixtures + 1,
            };

            foreach (CanonicalArrayOccurrence occurrence in
                CanonicalArrayOccurrence.Of(subject.Shape, subject.AuthoredRoot()))
            {
                row = occurrence.Order == ArrayOrder.IdSet
                    ? row with
                    {
                        SetOccurrences = row.SetOccurrences + 1,
                        SetReorderable = row.SetReorderable + (occurrence.IsReorderable ? 1 : 0),
                    }
                    : row with
                    {
                        OrderedOccurrences = row.OrderedOccurrences + 1,
                        OrderedReorderable =
                            row.OrderedReorderable + (occurrence.IsReorderable ? 1 : 0),
                    };
            }

            measured[subject.Kind] = row;
        }

        return measured;
    }

    private static void CollectDeclared(string path, DefinitionShape shape, List<string> into)
    {
        foreach (DefinitionField field in shape.Fields)
        {
            string fieldPath = path + "/" + field.Name;
            if (field.Shape == FieldShape.Array)
            {
                into.Add(fieldPath);
                if (field.Element?.Nested is not null)
                {
                    CollectDeclared(fieldPath + "[]", field.Element.Nested, into);
                }
            }
            else if (field.Nested is not null)
            {
                CollectDeclared(fieldPath, field.Nested, into);
            }
        }
    }

    /// <summary>
    /// An occurrence path with its array indices collapsed to <c>[]</c>, so the fifteen
    /// occurrences of one declaration count as one declaration reached.
    /// </summary>
    private static string WithoutIndices(string path)
    {
        System.Text.StringBuilder collapsed = new();
        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            if (int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                collapsed.Append("[]");
                continue;
            }

            collapsed.Append('/').Append(segment);
        }

        return collapsed.ToString();
    }

    /// <summary>The JSON text of each element of the array at <paramref name="path"/>.</summary>
    private static List<string> ElementTextsAt(JsonObject emitted, string path)
    {
        JsonNode? node = emitted;
        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            node = int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                ? ((JsonArray)node!)[index]
                : ((JsonObject)node!)[segment];
        }

        Assert.That(node, Is.InstanceOf<JsonArray>(), "no array was emitted at " + path);

        List<string> texts = new();
        foreach (JsonNode? element in (JsonArray)node!)
        {
            texts.Add(element?.ToJsonString() ?? "null");
        }

        return texts;
    }

    /// <summary>What the gate exercised for one writer.</summary>
    /// <param name="Kind">The writer's kind.</param>
    /// <param name="Fixtures">Valid fixtures of that kind in the corpus.</param>
    /// <param name="SetOccurrences">Stable-ID set arrays those fixtures carry.</param>
    /// <param name="SetReorderable">Of those, ones with two elements that differ.</param>
    /// <param name="OrderedOccurrences">Semantically ordered arrays those fixtures carry.</param>
    /// <param name="OrderedReorderable">Of those, ones with two elements that differ.</param>
    private sealed record WriterCensus(
        DefinitionKind Kind,
        int Fixtures,
        int SetOccurrences,
        int SetReorderable,
        int OrderedOccurrences,
        int OrderedReorderable);
}
