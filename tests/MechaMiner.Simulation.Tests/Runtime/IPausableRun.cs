using MechaMiner.Simulation.Runtime;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The subject seam the pause assertions run against, so the same assertions can be pointed at
/// the real run session and at deliberately broken stubs.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>, <c>VER-SIM-002-005</c>, and the
/// negative control <c>VER-SIM-002-010</c>.
/// </para>
/// <para>
/// The seam deliberately exposes both "is the run blocked" and "is this reason present" as
/// separate questions. That is what lets a stub which "consults only a single boolean toggle
/// instead of the reason set" - the control <c>VER-SIM-002-010</c> names - be written at all: such
/// a stub can report a reason present while reporting the run unblocked, and the assertions catch
/// exactly that disagreement.
/// </para>
/// </remarks>
internal interface IPausableRun
{
    /// <summary>Whether the run is blocked and therefore executes no tick.</summary>
    bool IsBlocking { get; }

    /// <summary>Authoritative ticks committed so far.</summary>
    long CommittedTickCount { get; }

    /// <summary>Seconds of UI clock the run has been given, blocked or not.</summary>
    double UiClockSeconds { get; }

    /// <summary>Raises one blocking reason.</summary>
    /// <param name="reason">The reason to raise.</param>
    void Raise(PauseReason reason);

    /// <summary>Clears one blocking reason, if the run permits it.</summary>
    /// <param name="reason">The reason to clear.</param>
    void ClearReason(PauseReason reason);

    /// <summary>Whether the reason is currently present.</summary>
    /// <param name="reason">The reason to test.</param>
    bool Contains(PauseReason reason);

    /// <summary>The focus-recovery entry point.</summary>
    void RecoverFocus();

    /// <summary>Runs one host step and returns the whole ticks it executed.</summary>
    /// <param name="elapsedSeconds">Seconds since the previous step.</param>
    int Step(double elapsedSeconds);
}
