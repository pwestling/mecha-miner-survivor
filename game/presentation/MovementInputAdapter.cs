using System;
using Godot;

namespace MechaMiner.Game.Presentation;

/// <summary>
/// Samples the logical move-vector action and turns it into a raw planar pair the simulation can
/// normalize.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-UI-001</c> / <c>UI-002</c>. <c>docs/technical/60-ui-input-and-accessibility.md</c>
/// § Logical input actions: "Gameplay and UI consume logical actions rather than physical
/// key/button checks", and the initial active-play action set begins with a "move vector". This
/// type reads that action and nothing else - no key code, no button index, no device query -
/// which is what makes remapping <c>UI-009</c>'s job rather than a change here.
/// </para>
/// <para>
/// It decides nothing about gameplay. It converts a device sample into two numbers and hands
/// them over; the direction rule, the speed rule, and the facing rule all live in the
/// simulation. In particular it does <b>not</b> convert magnitude to speed: doc 60 § Movement
/// says the adapter "remaps remaining magnitude to <c>[0,1]</c>, <em>then</em> the gameplay rule
/// converts any nonzero magnitude to full movement speed", so the remap is here and the speed
/// rule is emphatically not.
/// </para>
/// <para>
/// The deadzone is radial rather than per-axis. A per-axis deadzone is the common wrong
/// implementation and it is visible in play: it clips the diagonals into a square, so a stick
/// pushed diagonally reports a different magnitude than one pushed cardinally, and the mech
/// appears to hesitate near the axes.
/// </para>
/// </remarks>
internal sealed class MovementInputAdapter
{
    /// <summary>
    /// The logical action name for the active-play move vector, as declared in
    /// <c>project.godot</c>'s <c>[input]</c> section.
    /// </summary>
    /// <remarks>
    /// Four one-directional actions rather than one two-dimensional one, because the engine's
    /// action system is one-dimensional and <c>Input.GetVector</c> composes exactly this shape.
    /// The logical action doc 60 names is the composed vector, which is what
    /// <see cref="Sample"/> returns.
    /// </remarks>
    internal const string MoveEastAction = "move_east";

    /// <inheritdoc cref="MoveEastAction"/>
    internal const string MoveWestAction = "move_west";

    /// <inheritdoc cref="MoveEastAction"/>
    internal const string MoveNorthAction = "move_north";

    /// <inheritdoc cref="MoveEastAction"/>
    internal const string MoveSouthAction = "move_south";

    /// <summary>
    /// The initial radial deadzone: <c>0.18</c>.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/60-ui-input-and-accessibility.md</c>:65 - "Analog movement uses a
    /// configurable radial deadzone, initially 0.18, remaps remaining magnitude to
    /// <c>[0,1]</c>". Configurable is <c>UI-009</c>'s settings work; the same document at line
    /// 220 gives the settings range as 0.10 to 0.35. This constant is the initial value, and the
    /// field below is what a setting would write.
    /// </remarks>
    internal const double InitialRadialDeadzone = 0.18;

    private readonly double _radialDeadzone;

    /// <summary>Creates an adapter at the initial deadzone.</summary>
    internal MovementInputAdapter()
        : this(InitialRadialDeadzone)
    {
    }

    /// <summary>Creates an adapter at an explicit deadzone.</summary>
    /// <param name="radialDeadzone">The radial deadzone, in <c>[0, 1)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The deadzone is outside <c>[0, 1)</c>.</exception>
    internal MovementInputAdapter(double radialDeadzone)
    {
        if (!double.IsFinite(radialDeadzone) || radialDeadzone < 0.0 || radialDeadzone >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radialDeadzone),
                radialDeadzone,
                "a radial deadzone lies in [0,1); at 1 the control could never report input at all");
        }

        _radialDeadzone = radialDeadzone;
    }

    /// <summary>The radial deadzone in force.</summary>
    internal double RadialDeadzone => _radialDeadzone;

    /// <summary>
    /// Reads the logical move vector and returns it deadzoned and remapped.
    /// </summary>
    /// <param name="rawX">Receives the eastward component, in <c>[-1, 1]</c>.</param>
    /// <param name="rawY">Receives the northward component, in <c>[-1, 1]</c>.</param>
    /// <returns>Whether the sample is nonzero after the deadzone.</returns>
    /// <remarks>
    /// The engine's own action deadzone is bypassed by passing zero, so that this type's radial
    /// rule is the only deadzone in the path. Two deadzones in series would compose into
    /// something neither doc 60's number nor the engine's default describes.
    /// </remarks>
    internal bool Sample(out double rawX, out double rawY)
    {
        Vector2 sampled = Input.GetVector(
            MoveWestAction,
            MoveEastAction,
            MoveSouthAction,
            MoveNorthAction,
            deadzone: 0.0f);

        return ApplyRadialDeadzone(sampled.X, sampled.Y, _radialDeadzone, out rawX, out rawY);
    }

    /// <summary>
    /// Applies a radial deadzone and remaps the remaining magnitude to <c>[0, 1]</c>.
    /// </summary>
    /// <param name="sampledX">The eastward device sample.</param>
    /// <param name="sampledY">The northward device sample.</param>
    /// <param name="deadzone">The radial deadzone.</param>
    /// <param name="rawX">Receives the remapped eastward component.</param>
    /// <param name="rawY">Receives the remapped northward component.</param>
    /// <returns>Whether the result is nonzero.</returns>
    /// <remarks>
    /// <para>
    /// Static and pure so the rule can be exercised without a device. doc 60:65's remap is
    /// <c>(magnitude - deadzone) / (1 - deadzone)</c> applied along the sampled direction: a
    /// sample just outside the deadzone becomes a small nonzero magnitude rather than jumping to
    /// 0.18, which is the discontinuity a bare threshold produces and which feels like the stick
    /// snapping.
    /// </para>
    /// <para>
    /// The remapped magnitude is clamped to 1. A device that reports a corner sample can exceed
    /// unit magnitude, and passing that through would hand the simulation a magnitude outside
    /// the <c>[0,1]</c> domain doc 20 § Active commands defines.
    /// </para>
    /// </remarks>
    internal static bool ApplyRadialDeadzone(
        double sampledX,
        double sampledY,
        double deadzone,
        out double rawX,
        out double rawY)
    {
        if (!double.IsFinite(sampledX) || !double.IsFinite(sampledY))
        {
            // A device that reports a non-finite axis is a broken device, not a direction. Reporting
            // a stop is the safe reading: the alternative is handing NaN to the simulation, which
            // MovementIntent.Normalize would refuse by throwing on the authoritative thread.
            rawX = 0.0;
            rawY = 0.0;
            return false;
        }

        double magnitude = double.Hypot(sampledX, sampledY);
        if (magnitude <= deadzone)
        {
            // doc 60:66 "Tiny input below deadzone does not change persistent facing." An exact stop
            // is what carries that: the simulation preserves the last nonzero facing on a stop, so
            // reporting zero here is what leaves the facing alone.
            rawX = 0.0;
            rawY = 0.0;
            return false;
        }

        double remapped = Math.Min((magnitude - deadzone) / (1.0 - deadzone), 1.0);
        rawX = sampledX / magnitude * remapped;
        rawY = sampledY / magnitude * remapped;
        return true;
    }
}
