namespace MechaMiner.Simulation.Entities;

/// <summary>
/// What a store does when a request arrives at its hard capacity.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior defines
/// three distinct behaviours, and they are three rather than a boolean because their
/// consequences differ in kind:
/// </para>
/// <list type="bullet">
///   <item><description>
///     "Authored enemies that reach a gameplay ceiling queue and later enter; they are
///     not silently canceled or converted."
///   </description></item>
///   <item><description>
///     "Visual-only particles, decals, trails, hit sparks, and audio voices may use
///     priority-based degradation without affecting simulation."
///   </description></item>
///   <item><description>
///     "A hard authoritative capacity breach is a failed invariant caught by content
///     validation or stress testing, not a runtime balancing tool."
///   </description></item>
/// </list>
/// <para>
/// Public because it is part of the store contract a tool or the game may read through
/// <c>EntityDiagnostics</c>; doc 115 § Component registry lists <c>CMP-OBS-001</c> as a
/// consumer of the diagnostic metrics doc 20 § Capacity and overload behavior
/// enumerates.
/// </para>
/// </remarks>
public enum OverflowBehaviour
{
    /// <summary>
    /// The request is retained and materializes later; nothing is cancelled or
    /// converted.
    /// </summary>
    /// <remarks>
    /// The behaviour of the authored-enemy stores. doc 10 § System phase ordering
    /// phase 3 "materialize queued spawns that have capacity and valid positions" and
    /// phase 12 "apply deferred entity creation/removal and capacity queues" are where
    /// the queue drains.
    /// </remarks>
    QueueAuthored = 0,

    /// <summary>
    /// The request is refused and counted, without affecting authoritative state.
    /// </summary>
    /// <remarks>
    /// No authoritative population category declares this: doc 20 § Capacity and
    /// overload behavior confines degradation to "visual-only particles, decals, trails,
    /// hit sparks, and audio voices", none of which is one of the twelve categories, and
    /// says outright that "authoritative projectiles and persistent weapon actors may
    /// not disappear because a visual pool is full". The member exists because the
    /// enumeration is doc 20's, and the presentation pools that use it are owned by the
    /// presentation stream; <c>StoreCapacityTests</c> asserts that no authoritative
    /// category declares it, so the exclusion is a gate rather than a comment.
    /// </remarks>
    DegradePresentation = 1,

    /// <summary>
    /// The breach fails the tick invariant, ending the run through the safe
    /// technical-failure path.
    /// </summary>
    /// <remarks>
    /// doc 20 § Tick transaction: "An exception or invariant failure before commit
    /// invalidates the tick and ends the run through the safe technical-failure path; it
    /// never publishes a partial state." The offending batch is still resident when the
    /// invariant fires, which is what makes the failure inspectable rather than masked
    /// by a silently rejected spawn.
    /// </remarks>
    FailInvariant = 2,
}
