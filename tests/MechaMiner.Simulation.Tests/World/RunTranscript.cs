using System;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;

namespace MechaMiner.Simulation.Tests.World;

/// <summary>
/// Drives a run under a scripted policy and renders every authoritative quantity it produces as
/// canonical text.
/// </summary>
/// <remarks>
/// <para>
/// This exists for the determinism claim. Comparing two runs' final Hull would pass for two runs that
/// diverged and reconverged; comparing a rendered transcript of every enemy position, every seam's
/// progress, and every counter, at every sampled tick, will not. <c>docs/technical/91-verification-strategy.md</c>
/// § Determinism and fixture policy wants canonical, ordered, reviewable text, and this is that.
/// </para>
/// <para>
/// Positions render with <c>R</c> round-trip formatting, so two doubles that differ in the last bit
/// render differently. A fixed-decimal format would hide exactly the divergence this is built to catch.
/// </para>
/// </remarks>
internal static class RunTranscript
{
    /// <summary>A scripted player policy: what to hold on a given tick, given the world's state.</summary>
    internal delegate PlanarVector Policy(GameplayWorld world, long tick);

    /// <summary>Hold nothing at all. The mech stands where it deployed.</summary>
    internal static PlanarVector Stand(GameplayWorld world, long tick)
    {
        return PlanarVector.Zero;
    }

    /// <summary>Walk to the first seam and stop on it.</summary>
    internal static PlanarVector DrillFirstSeam(GameplayWorld world, long tick)
    {
        PlanarVector toSeam = world.MiningSiteAt(0).Centre - world.Player.Position;
        return toSeam.Magnitude > 0.05 ? toSeam.Normalized() : PlanarVector.Zero;
    }

    /// <summary>
    /// Kite anticlockwise on a ring, correcting toward it, which outruns a 1.26 m/s pursuer.
    /// </summary>
    internal static Policy KiteOnRing(double radiusMeters)
    {
        return (world, tick) =>
        {
            PlanarVector position = world.Player.Position;
            double magnitude = position.Magnitude;
            PlanarVector radial = magnitude < 1e-6
                ? PlanarVector.East
                : PlanarVector.FromComponents(position.X / magnitude, position.Y / magnitude);
            PlanarVector tangent = PlanarVector.FromComponents(-radial.Y, radial.X);
            double error = Math.Clamp(radiusMeters - magnitude, -1.0, 1.0);
            return (tangent + (radial * error)).Normalized();
        };
    }

    /// <summary>
    /// Runs a composition for a bounded number of ticks and renders a transcript.
    /// </summary>
    /// <param name="run">The composition to drive.</param>
    /// <param name="ticks">How many ticks to attempt.</param>
    /// <param name="policy">The scripted player policy.</param>
    /// <param name="sampleEvery">How often to render a sample row.</param>
    /// <returns>The transcript.</returns>
    /// <remarks>
    /// Each step is exactly <see cref="TickRate.SecondsPerTick"/> rather than a real frame delta, because
    /// a transcript compared against itself has to be reproducible and a wall-clock delta is the one input
    /// to this path that is not.
    /// </remarks>
    internal static string Drive(
        RunComposition run,
        int ticks,
        Policy policy,
        int sampleEvery)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(policy);

        StringBuilder text = new();
        text.Append("row\t").Append(run.World.BaselineRow.ToString()).Append('\n');
        text.Append("seams");
        for (int index = 0; index < run.World.MiningSiteCount; index++)
        {
            text.Append('\t').Append(Render(run.World.MiningSiteAt(index).Centre));
        }

        text.Append('\n');

        long sequence = CommandEnvelope.FirstSequence;
        for (int tick = 0; tick < ticks; tick++)
        {
            PlanarVector intent = policy(run.World, tick);
            _ = run.CommandGate.TryAdmit(run.ComposeEnvelope(sequence, intent.X, intent.Y), out _);
            sequence++;

            run.Host.Step(TickRate.SecondsPerTick);
            _ = run.SettleTerminalTransition();

            bool sample = run.World.CommittedTickCount % sampleEvery == 0;
            if (sample || run.World.HasEnded)
            {
                AppendSample(text, run);
            }

            if (run.World.HasEnded)
            {
                break;
            }
        }

        text.Append("terminal\t").Append(run.World.Terminal.ToString()).Append('\n');
        return text.ToString();
    }

    private static void AppendSample(StringBuilder text, RunComposition run)
    {
        GameplayWorld world = run.World;
        text.Append("t=").Append(world.CommittedTickCount.ToString(CultureInfo.InvariantCulture))
            .Append("\thull=").Append(world.Player.Hull.ToString(CultureInfo.InvariantCulture))
            .Append("\tat=").Append(Render(world.Player.Position))
            .Append("\tore=").Append(world.RunLocalCommonOre.ToString(CultureInfo.InvariantCulture))
            .Append("\tenemies=").Append(world.LiveEnemyCount.ToString(CultureInfo.InvariantCulture))
            .Append("\tshots=").Append(world.ProjectilesFired.ToString(CultureInfo.InvariantCulture))
            .Append("\tspawned=").Append(world.EnemiesMaterialized.ToString(CultureInfo.InvariantCulture))
            .Append("\tkilled=").Append(world.EnemiesDestroyed.ToString(CultureInfo.InvariantCulture))
            .Append("\tcontacts=")
            .Append(world.ContactInstancesResolved.ToString(CultureInfo.InvariantCulture))
            .Append("\tinstallments=")
            .Append(world.InstallmentsPaid.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        for (int index = 0; index < world.MiningSiteCount; index++)
        {
            MiningSiteState seam = world.MiningSiteAt(index);
            text.Append("  seam").Append(index.ToString(CultureInfo.InvariantCulture))
                .Append('\t').Append(seam.Phase.ToString())
                .Append("\tprogress=")
                .Append(seam.InstallmentProgressTicks.ToString(CultureInfo.InvariantCulture))
                .Append("\tpaid=").Append(seam.InstallmentsPaid.ToString(CultureInfo.InvariantCulture))
                .Append("\toutside=").Append(seam.TicksOutside.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        for (int index = 0; index < world.LiveEnemyCount; index++)
        {
            EnemyState enemy = world.EnemyAt(index);
            text.Append("  enemy").Append(index.ToString(CultureInfo.InvariantCulture))
                .Append('\t').Append(Render(enemy.Position))
                .Append("\thull=").Append(enemy.Hull.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }
    }

    private static string Render(PlanarVector value)
    {
        return "("
            + value.X.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + value.Y.ToString("R", CultureInfo.InvariantCulture)
            + ")";
    }
}
