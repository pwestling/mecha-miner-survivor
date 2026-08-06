using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves the immutability and no-mutation gates can fail, by running the same assertions the real gates run
/// against payloads and rebuild passes that are deliberately wrong.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-007-011</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires evidence that a gate can
/// fail. The stubs are ordinary valid C# that behaves incorrectly, not a deliberately invalid fixture, which
/// <c>docs/technical/delivery-waves.md</c> forbids inside a compiled project.
/// </para>
/// <para>
/// The assertions come from <see cref="SnapshotContractAssertions"/>, the same code
/// <see cref="PresentationSnapshotTests"/> and <see cref="SnapshotReconstructionTests"/> use, so weakening
/// one turns the real gates and this control red together.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SnapshotNegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-007-011</c>.
    ///
    /// A stub snapshot that hands out a store's backing array fails the immutability assertion; a rebuild pass
    /// that writes through to simulation state fails the no-mutation assertion; and the real snapshot and a
    /// read-only rebuild pass pass both.
    /// </summary>
    [Test]
    public void ImmutabilityAndNoMutationAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        AssertArrayExposingSnapshotFailsTheImmutabilityGate();
        AssertSettablePropertyFailsTheImmutabilityGate();
        AssertWritingRebuildFailsTheNoMutationGate();
        AssertAVacuousRebuildFailsTheNoMutationGate();
        AssertTheRealSnapshotAndRebuildPassBothGates();
    }

    /// <summary>
    /// A stub payload that holds and returns a store's backing array: a consumer could write through it, so
    /// the structural assertion must reject it.
    /// </summary>
    private static void AssertArrayExposingSnapshotFailsTheImmutabilityGate()
    {
        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => SnapshotContractAssertions.PayloadTypesAreStructurallyImmutable(
                "a stub snapshot that hands out a store's backing array",
                typeof(ArrayExposingSnapshot)));

        Assert.That(
            failure.Message,
            Does.Contain("could write through the payload"),
            "the immutability gate must be the assertion that failed");
        Assert.That(
            failure.Message,
            Does.Contain("VisibleEntities"),
            "and it must name the offending member");
    }

    /// <summary>
    /// A stub payload whose members are immutably typed but settable is still mutable, and the gate must say
    /// so - this is the case a type-only check would miss.
    /// </summary>
    private static void AssertSettablePropertyFailsTheImmutabilityGate()
    {
        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => SnapshotContractAssertions.PayloadTypesAreStructurallyImmutable(
                "a stub snapshot with a public setter",
                typeof(SettableSnapshot)));

        Assert.That(
            failure.Message,
            Does.Contain("has a setter"),
            "a settable member must fail even when its type is immutable");
    }

    /// <summary>
    /// A rebuild pass that writes back into the authoritative store changes the world, so the whole-state
    /// comparison must reject it.
    /// </summary>
    private static void AssertWritingRebuildFailsTheNoMutationGate()
    {
        string control = RunFiveTicks(writeThroughAtTick: -1, out int controlFields, out string controlWorld);
        string mutated = RunFiveTicks(writeThroughAtTick: 2, out int mutatedFields, out string mutatedWorld);

        Assert.That(
            mutatedWorld,
            Is.Not.EqualTo(controlWorld),
            "the fixture must actually have mutated the world, or the control proves nothing");
        Assert.That(controlFields, Is.EqualTo(0));

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => SnapshotContractAssertions.RebuildMutatedNothing(
                "a rebuild pass that writes through to simulation state",
                control,
                mutated,
                mutatedFields));

        Assert.That(
            failure.Message,
            Does.Contain("bit-identical to a run in which no rebuild occurred"),
            "the no-mutation gate must be the assertion that failed");
    }

    /// <summary>
    /// A rebuild pass that read nothing would pass a naive comparison trivially, so the gate rejects it too.
    /// </summary>
    private static void AssertAVacuousRebuildFailsTheNoMutationGate()
    {
        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => SnapshotContractAssertions.RebuildMutatedNothing(
                "a rebuild pass that read nothing",
                "identical",
                "identical",
                rebuiltFieldCount: 0));

        Assert.That(
            failure.Message,
            Does.Contain("must have read something"),
            "the gate must refuse to be satisfied by a rebuild that did not happen");
    }

    /// <summary>Both assertions must pass against the real payload types and a read-only rebuild.</summary>
    private static void AssertTheRealSnapshotAndRebuildPassBothGates()
    {
        Expect.DoesNotThrow(() => SnapshotContractAssertions.PayloadTypesAreStructurallyImmutable(
            "the real CTR-SIM-003 payload types",
            typeof(PresentationSnapshot),
            typeof(SnapshotEntity),
            typeof(HudViewModel),
            typeof(SnapshotVersion),
            typeof(TickPublication),
            typeof(InterpolationSnapPolicy)));

        string control = RunFiveTicks(writeThroughAtTick: -1, out int _, out string controlWorld);
        string readOnly = RunFiveTicks(
            writeThroughAtTick: -1,
            out int readFields,
            out string readWorld,
            readOnlyRebuildAtTick: 2);

        Expect.DoesNotThrow(() => SnapshotContractAssertions.RebuildMutatedNothing(
            "a read-only rebuild pass",
            control,
            readOnly,
            readFields));

        Assert.That(readWorld, Is.EqualTo(controlWorld), "and the world must be unchanged too");
    }

    /// <summary>
    /// Runs five ticks, optionally writing back through the store during a rebuild, and returns the rendered
    /// authoritative result.
    /// </summary>
    private static string RunFiveTicks(
        long writeThroughAtTick,
        out int rebuiltFields,
        out string worldRendering,
        long readOnlyRebuildAtTick = -1)
    {
        SnapshotFixture fixture = new(enemyCount: 3);
        HudViewModel hud = HudViewModel.Unpublished;
        System.Text.StringBuilder rendering = new();
        rebuiltFields = 0;

        for (long tick = 0; tick < 5; tick++)
        {
            rendering.Append(fixture.RunTick(tick, hud, out hud));

            if (tick == writeThroughAtTick)
            {
                rebuiltFields = WriteThroughRebuild(fixture);
            }
            else if (tick == readOnlyRebuildAtTick)
            {
                rebuiltFields = ReadOnlyRebuild(fixture);
            }
        }

        worldRendering = fixture.RenderWorld();
        return rendering.ToString();
    }

    /// <summary>
    /// A deliberately broken rebuild pass: it reads the snapshot and then writes the values back into the
    /// authoritative store, which doc 20 § Scope and invariants forbids - "presentation cannot mutate
    /// simulation state".
    /// </summary>
    private static int WriteThroughRebuild(SnapshotFixture fixture)
    {
        PresentationSnapshot snapshot = fixture.Publisher.Latest!;
        ReadOnlySpan<SnapshotEntity> entities = snapshot.VisibleEntities.Span;
        int fieldsRead = 0;
        for (int index = 0; index < entities.Length; index++)
        {
            SnapshotEntity record = entities[index];
            fieldsRead += 3;
            fixture.Enemies.TryUpdate(
                record.Id,
                new EnemyState(record.PositionX + 100.0, record.PositionY + 100.0, 7));
        }

        return fieldsRead;
    }

    /// <summary>A correct rebuild pass: it reads the snapshot and writes nothing authoritative.</summary>
    private static int ReadOnlyRebuild(SnapshotFixture fixture)
    {
        PresentationSnapshot snapshot = fixture.Publisher.Latest!;
        ReadOnlySpan<SnapshotEntity> entities = snapshot.VisibleEntities.Span;
        List<SnapshotEntity> copies = new(entities.Length);
        int fieldsRead = 0;
        for (int index = 0; index < entities.Length; index++)
        {
            copies.Add(entities[index]);
            fieldsRead += 3;
        }

        return fieldsRead;
    }

    /// <summary>
    /// A deliberately broken payload that holds and returns a mutable array.
    /// </summary>
    /// <remarks>
    /// Valid code that models the wrong design. Nothing depends on it; it exists so the structural
    /// immutability assertion can be shown to catch an exposed mutable store.
    /// </remarks>
    private sealed class ArrayExposingSnapshot
    {
        private readonly SnapshotEntity[] _visibleEntities = Array.Empty<SnapshotEntity>();

        /// <summary>Hands out the backing array, which a consumer can write through.</summary>
        public SnapshotEntity[] VisibleEntities => _visibleEntities;
    }

    /// <summary>
    /// A deliberately broken payload whose member type is immutable but whose member is settable.
    /// </summary>
    private sealed class SettableSnapshot
    {
        /// <summary>Settable, so a consumer can replace it after publication.</summary>
        public long Tick { get; set; }
    }
}
