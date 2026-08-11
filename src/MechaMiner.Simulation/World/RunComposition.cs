using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.World;

/// <summary>
/// One assembled run: its world, the host that ticks it, and the seams a caller must reach.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that presentation and the tests build the <em>same</em> run. A composition
/// duplicated between the two is a composition that will differ, and it would differ exactly
/// where it matters: a test that wires its own gate to its own publisher proves the world works
/// when correctly wired, and says nothing about whether the shipping wiring is correct.
/// </para>
/// <para>
/// It composes by explicit construction and holds no container, service locator, or mutable
/// global, per <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § C# and domain
/// defaults. It is a record of what was built, not a registry to look things up in.
/// </para>
/// </remarks>
public sealed class RunComposition
{
    private readonly SimulationHost _host;
    private readonly GameplayWorld _world;
    private readonly CommandAdmissionGate _commandGate;
    private readonly SnapshotDoubleBuffer _snapshots;

    private RunComposition(
        SimulationHost host,
        GameplayWorld world,
        CommandAdmissionGate commandGate,
        SnapshotDoubleBuffer snapshots)
    {
        _host = host;
        _world = world;
        _commandGate = commandGate;
        _snapshots = snapshots;
    }

    /// <summary>The host that paces ticks. Drive the run by calling its step method.</summary>
    public SimulationHost Host => _host;

    /// <summary>The authoritative world.</summary>
    public GameplayWorld World => _world;

    /// <summary>The gate a producer submits command envelopes to.</summary>
    public CommandAdmissionGate CommandGate => _commandGate;

    /// <summary>The double buffer a consumer reads the two latest published snapshots from.</summary>
    public SnapshotDoubleBuffer Snapshots => _snapshots;

    /// <summary>The run session every envelope for this run must carry.</summary>
    public ulong RunSession => _commandGate.RunSession;

    /// <summary>The tick the admission window is currently open for.</summary>
    /// <remarks>
    /// A producer targets this tick. It advances as ticks execute, so it is read per sample
    /// rather than cached.
    /// </remarks>
    public SimulationTick OpenTick => _commandGate.OpenTick;

    /// <summary>
    /// Assembles a run over the graybox arena with the player deployed at its centre.
    /// </summary>
    /// <param name="runSession">The run session identity. Must not be zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    /// <remarks>
    /// The arena is <see cref="GrayboxArenaBounds.Default"/>, which is graybox scaffolding that
    /// <c>MAP-007</c> replaces; the origin is its centre. Deployment position selection is
    /// <c>MAP-005</c>'s and the centre is a placeholder for it, not a rule.
    /// </remarks>
    public static RunComposition CreateGraybox(ulong runSession)
    {
        return Create(runSession, GrayboxArenaBounds.Default, PlanarVector.Zero);
    }

    /// <summary>
    /// Assembles a run.
    /// </summary>
    /// <param name="runSession">The run session identity. Must not be zero.</param>
    /// <param name="bounds">The world constraint phase 5 enforces.</param>
    /// <param name="deploymentPosition">Where the player's body begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bounds"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    public static RunComposition Create(
        ulong runSession,
        IPlanarBounds bounds,
        PlanarVector deploymentPosition)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        // Capacities. Every one of these is a bound on this slice's content, not a tuned budget:
        // there is one body, so one visible-entity slot is more than is used, and no phase appends
        // an event, so the event batches are sized to the smallest the publisher accepts rather
        // than to a measured peak. The packages that add entities and events resize them against
        // their own populations.
        const int visibleEntityCapacity = 1;
        const int eventBatchCapacity = 1;
        const int eventBufferHardMaximum = 64;

        CommandAdmissionGate commandGate = new(runSession);
        SnapshotPublisher publisher = new(
            runSession,
            visibleEntityCapacity,
            eventBatchCapacity,
            eventBatchCapacity);
        DomainEventBuffer domainEvents = new(eventBatchCapacity, eventBufferHardMaximum);
        PresentationEventBuffer presentationEvents = new(eventBatchCapacity, eventBufferHardMaximum);

        GameplayWorld world = new(
            commandGate,
            publisher,
            domainEvents,
            presentationEvents,
            PresentationCoalescingPolicy.Verbatim,
            bounds,
            deploymentPosition);

        return new RunComposition(new SimulationHost(world), world, commandGate, publisher.Buffer);
    }

    /// <summary>
    /// Builds a command envelope for the tick the admission window is currently open for.
    /// </summary>
    /// <param name="sequence">The producer's monotonic sequence.</param>
    /// <param name="rawInputX">The raw eastward input component as sampled.</param>
    /// <param name="rawInputY">The raw northward input component as sampled.</param>
    /// <returns>The envelope to submit.</returns>
    /// <remarks>
    /// The target tick is read from the gate rather than chosen by the producer, so a producer
    /// cannot address a tick that has already frozen by miscounting. Submitting the result is a
    /// separate call, because admission can reject and the producer has to see the rejection.
    /// </remarks>
    public CommandEnvelope ComposeEnvelope(long sequence, double rawInputX, double rawInputY)
    {
        return CommandEnvelope.Create(
            _commandGate.RunSession,
            _commandGate.OpenTick,
            sequence,
            rawInputX,
            rawInputY);
    }
}
