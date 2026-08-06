using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// The event kinds, identities, and factory shorthands the <c>SIM-006</c> tests share.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports every <c>VER-SIM-006-*</c> entry.
/// </para>
/// <para>
/// The kinds here are test kinds, declared the way
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events expects a real system to
/// declare its own: as static readonly members on the type that owns them, with stable numeric identities.
/// The real gameplay kinds belong to the packages that emit them, which is the whole reason
/// <c>EventKind</c> is not an enumeration.
/// </para>
/// </remarks>
internal static class EventFixture
{
    /// <summary>The run session the fixture's identities are fenced to.</summary>
    internal const ulong RunSession = 0xE7E0_0001UL;

    /// <summary>A second run session, for cross-run assertions.</summary>
    internal const ulong OtherRunSession = 0xE7E0_0002UL;

    /// <summary>A mining-site manifest count large enough for the fixture's identities.</summary>
    internal const int MiningSiteManifestCount = 8;

    /// <summary>A static-world-object manifest count large enough for the fixture's identities.</summary>
    internal const int StaticWorldObjectManifestCount = 4;

    /// <summary>An authoritative fact: an entity was defeated.</summary>
    internal static EventKind EntityDefeated { get; } = EventKind.Declare(1001, "entity-defeated");

    /// <summary>An authoritative fact: a resource was awarded.</summary>
    internal static EventKind ResourceAwarded { get; } = EventKind.Declare(1002, "resource-awarded");

    /// <summary>An authoritative fact: the run reached a terminal result.</summary>
    internal static EventKind RunTerminal { get; } = EventKind.Declare(1003, "run-terminal");

    /// <summary>A presentation instruction: a hit was confirmed. Coalescable by policy.</summary>
    internal static EventKind HitConfirmed { get; } = EventKind.Declare(2001, "hit-confirmed");

    /// <summary>A presentation instruction: a warning telegraph. Never coalesced.</summary>
    /// <remarks>
    /// doc 30 § Failure and fallback treats a critical telegraph differently from a cosmetic effect, so this
    /// kind exists to prove that a policy with a rule for one kind leaves another kind verbatim.
    /// </remarks>
    internal static EventKind Warning { get; } = EventKind.Declare(2002, "warning");

    /// <summary>An allocator for the fixture's run session.</summary>
    internal static EntityIdAllocator NewAllocator(ulong runSession)
    {
        return new EntityIdAllocator(runSession, MiningSiteManifestCount, StaticWorldObjectManifestCount);
    }

    /// <summary>A complete provenance with the given phase, sequence, and emitter.</summary>
    internal static EventProvenance Provenance(
        long tick,
        int systemPhase,
        long sequence,
        EntityId emitter,
        string sourceContentId = "E-FIXTURE")
    {
        return EventProvenance.Create(tick, systemPhase, sequence, emitter, sourceContentId);
    }

    /// <summary>A typed payload with a distinguishable quantity.</summary>
    internal static EventPayload Payload(long quantity)
    {
        return EventPayload.Typed(
            EventPayload.InitialSchemaVersion,
            quantity,
            quantity * 0.5,
            EventPayload.NoContentId);
    }

    /// <summary>A complete domain event.</summary>
    internal static DomainEvent Domain(
        EventKind kind,
        long tick,
        int systemPhase,
        long sequence,
        EntityId emitter,
        EntityId subject,
        long quantity)
    {
        return DomainEvent.Create(
            kind,
            Provenance(tick, systemPhase, sequence, emitter),
            subject,
            positionX: quantity * 1.5,
            positionY: quantity * -0.25,
            Payload(quantity));
    }

    /// <summary>A complete presentation event.</summary>
    internal static PresentationEvent Presentation(
        EventKind kind,
        long tick,
        int systemPhase,
        long sequence,
        EntityId emitter,
        EntityId subject,
        long quantity,
        string sourceContentId = "E-FIXTURE")
    {
        return PresentationEvent.Create(
            kind,
            EventProvenance.Create(tick, systemPhase, sequence, emitter, sourceContentId),
            subject,
            positionX: quantity * 1.5,
            positionY: quantity * -0.25,
            Payload(quantity));
    }
}
