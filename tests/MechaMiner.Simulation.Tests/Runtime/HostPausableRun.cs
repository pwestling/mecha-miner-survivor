using MechaMiner.Simulation.Runtime;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The real subject: a production <see cref="SimulationHost"/> over a recording world, behind the
/// pause test seam.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>, <c>VER-SIM-002-005</c>.
///
/// The adapter adds no behaviour: every member forwards to the host, its run clock, or its
/// lifecycle hooks. Every assertion the negative control <c>VER-SIM-002-010</c> proves can fail is
/// therefore run against the production types here.
/// </remarks>
internal sealed class HostPausableRun : IPausableRun
{
    private readonly SimulationHost _host;

    /// <summary>Creates a run over a fresh host, clock, accumulator, and recording world.</summary>
    internal HostPausableRun()
    {
        World = new RecordingWorld();
        _host = new SimulationHost(World);
    }

    /// <summary>The recording world the host drives.</summary>
    internal RecordingWorld World { get; }

    /// <summary>The host under test, for assertions the seam does not expose.</summary>
    internal SimulationHost Host => _host;

    /// <inheritdoc />
    public bool IsBlocking => _host.Clock.IsBlocking;

    /// <inheritdoc />
    public long CommittedTickCount => _host.Clock.CommittedTickCount;

    /// <inheritdoc />
    public double UiClockSeconds => _host.UiClockSeconds;

    /// <inheritdoc />
    public void Raise(PauseReason reason)
    {
        _host.Clock.Raise(reason);
    }

    /// <inheritdoc />
    public void ClearReason(PauseReason reason)
    {
        _host.Clock.Clear(reason);
    }

    /// <inheritdoc />
    public bool Contains(PauseReason reason)
    {
        return _host.Clock.BlockingReasons.Contains(reason);
    }

    /// <inheritdoc />
    public void RecoverFocus()
    {
        _host.Lifecycle.OnFocusRegained();
    }

    /// <inheritdoc />
    public int Step(double elapsedSeconds)
    {
        return _host.Step(elapsedSeconds).TickCount;
    }
}
