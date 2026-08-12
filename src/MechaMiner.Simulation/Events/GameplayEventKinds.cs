namespace MechaMiner.Simulation.Events;

/// <summary>
/// The domain event kinds this slice's gameplay phases emit.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events makes an event's kind a
/// stable identifier, so the numbers below are assigned once and are not reordered. They are spaced by
/// phase - 1000s for spawning, 2000s for damage and death, 3000s for mining, 4000s for run
/// termination - so a later package adding an event to a phase does not have to renumber a neighbour.
/// </para>
/// <para>
/// <b>Every one of these is a domain event, not a presentation event.</b> doc 10 § Commands and
/// mutations: systems "append ordered domain or presentation events to tick-local buffers". A domain
/// event is a fact about authoritative state and is what a metrics or replay consumer reads; a
/// presentation event is a request for a visual, and requesting one would be this package deciding what
/// a hit looks like. <c>AVX-*</c> owns that, and coalescing policy exists precisely because
/// presentation events are droppable in a way these are not.
/// </para>
/// </remarks>
public static class GameplayEventKinds
{
    /// <summary>An ordinary enemy was materialized by phase 3.</summary>
    public static EventKind EnemyMaterialized => EventKind.Declare(1001, "enemy-materialized");

    /// <summary>An eligible contact instance reduced the mech's Hull in phase 10.</summary>
    public static EventKind PlayerContactDamaged => EventKind.Declare(2001, "player-contact-damaged");

    /// <summary>A weapon projectile damaged an ordinary enemy in phase 10.</summary>
    public static EventKind EnemyDamaged => EventKind.Declare(2002, "enemy-damaged");

    /// <summary>An ordinary enemy's Hull reached zero in phase 10.</summary>
    /// <remarks>
    /// Emitted where the death is decided, not where the record is removed. Phase 12 does the removal and
    /// emits nothing: doc 10 § System phase ordering defers structural change so systems do not
    /// invalidate collections, which makes phase 12 bookkeeping rather than an event about the world.
    /// </remarks>
    public static EventKind EnemyDestroyed => EventKind.Declare(2003, "enemy-destroyed");

    /// <summary>A mining installment completed and paid in phase 11.</summary>
    public static EventKind MiningInstallmentPaid => EventKind.Declare(3001, "mining-installment-paid");

    /// <summary>A mining site paid its last installment and is spent for the run.</summary>
    public static EventKind MiningSiteDepleted => EventKind.Declare(3002, "mining-site-depleted");

    /// <summary>A terminal result was assigned to the run.</summary>
    public static EventKind RunSettled => EventKind.Declare(4001, "run-settled");
}
