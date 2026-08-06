using System;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using MechaMiner.Game.Presentation;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;

namespace MechaMiner.Game.EngineTesting;

/// <summary>
/// Drives the real run scene with a scripted input sequence and writes a canonical transcript, so
/// that "the mech moves under player input" is a thing someone can read rather than infer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Development scaffolding.</b> It lives in <c>game/tests/</c>, which
/// <c>game/MechaMiner.Game.csproj</c> removes from compilation under the <c>ExportRelease</c>
/// configuration, so it is absent from the assembly in a Release build rather than merely filtered
/// out of an export. That is the same footing as <c>GodotTestRunner</c>, and the reasoning is doc 100
/// § Godot import and export.
/// </para>
/// <para>
/// It exists because no test project in this repository can reach presentation code:
/// <c>tests/MechaMiner.Game.Tests</c> references the three pure projects and not
/// <c>MechaMiner.Game</c>, and <c>build/verify-architecture.sh</c> § 3 asserts that reference set
/// exactly. So the camera, the input adapter, and the snapshot-to-transform path have no NUnit
/// selector available to them, and an engine-tier scene is what can assert them at all. The
/// verification entries in <c>tests/verification/PRE-001.json</c>, <c>PRE-002.json</c>, and
/// <c>UI-002.json</c> name the sections below.
/// </para>
/// <para>
/// <b>It does not synthesize device input.</b> Injecting events through the engine's input queue
/// would test the queue. Instead it drives the same adapter rule the shipping path uses
/// (<see cref="MovementInputAdapter.ApplyRadialDeadzone"/>), composes the same envelope through the
/// same <see cref="RunComposition"/>, and steps the same host - so what it proves about the
/// simulation path is exactly what the shipping path does. The one thing it therefore cannot prove
/// is that a physical key is bound to the right action; that is what the interactive launch is for,
/// and the transcript says so rather than implying otherwise.
/// </para>
/// <para>
/// The step is a fixed <see cref="TickRate.SecondsPerTick"/> rather than a real frame delta, because
/// a transcript compared against itself between runs has to be reproducible, and a wall-clock delta
/// is the one input to this path that is not.
/// </para>
/// </remarks>
public partial class RunSliceEvidenceHarness : Node
{
    /// <summary>The stable line a host asserts to know the harness reached managed code.</summary>
    internal const string StartupLine = "MechaMiner: run slice evidence harness ready";

    /// <summary>Environment variable naming the directory the transcript and captures go to.</summary>
    internal const string OutputDirectoryVariable = "MECHAMINER_RUN_SLICE_OUTPUT";

    private const string TranscriptFileName = "transcript.tsv";
    private const double Tolerance = 1e-9;

    private readonly StringBuilder _transcript = new();

    private int _assertionsRun;
    private int _assertionsFailed;

    /// <inheritdoc/>
    public override void _Ready()
    {
        GD.Print(StartupLine);

        string outputDirectory = ReadSetting(OutputDirectoryVariable);
        if (outputDirectory.Length == 0)
        {
            GD.PushError("the run slice evidence harness requires " + OutputDirectoryVariable);
            GetTree().Quit(2);
            return;
        }

        Line("# MechaMiner run slice evidence. Canonical, ordered, reviewable text");
        Line("# (doc 91 § Determinism and fixture policy). Every step is exactly one tick of");
        Line("# TickRate.SecondsPerTick, so this transcript is reproducible.");
        Line("engine_version\t" + Engine.GetVersionInfo()["string"].AsString());
        Line("rendering_method\t"
            + ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString());
        Line("display_server\t" + DisplayServer.GetName());
        Line("ticks_per_second\t" + TickRate.TicksPerSecond.ToString(CultureInfo.InvariantCulture));
        Line("base_speed_m_per_s\t" + Invariant(PlayerBaseline.BaseMovementSpeedMetersPerSecond));
        Line("displacement_per_tick_m\t" + Invariant(PlayerMovement.BaseDisplacementPerTickMeters));
        Line(string.Empty);

        RunCameraSection();
        RunDeadzoneSection();
        RunFacingSection();
        RunOpenTickSection();
        RunMovementSection();
        RunInterpolationSection();

        Line(string.Empty);
        Line("assertions_run\t" + _assertionsRun.ToString(CultureInfo.InvariantCulture));
        Line("assertions_failed\t" + _assertionsFailed.ToString(CultureInfo.InvariantCulture));
        Line("outcome\t" + (_assertionsFailed == 0 ? "passed" : "failed"));

        WriteTranscript(outputDirectory);

        GD.Print(
            "MechaMiner: run slice evidence "
            + (_assertionsFailed == 0 ? "passed" : "failed")
            + " with "
            + _assertionsRun.ToString(CultureInfo.InvariantCulture)
            + " assertions");

        GetTree().Quit(_assertionsFailed == 0 ? 0 : 4);
    }

    /// <summary>
    /// <c>VER-PRE-002-001</c>: the camera shows exactly 24 gameplay metres vertically, orthographic,
    /// north-up, non-rotating.
    /// </summary>
    private void RunCameraSection()
    {
        Line("## camera-shows-24-metres-vertically");

        Camera3D camera = new();
        RunSceneRoot.ConfigureCamera(camera);

        Line("projection\t" + camera.Projection.ToString());
        Line("keep_aspect\t" + camera.KeepAspect.ToString());
        Line("size_metres\t" + Invariant(camera.Size));

        Check(
            "projection-is-orthographic",
            camera.Projection == Camera3D.ProjectionType.Orthogonal,
            "doc 30:53 \"The default is true orthographic\"");
        Check(
            "vertical-extent-is-24-metres",
            camera.KeepAspect == Camera3D.KeepAspectEnum.Height
                && Math.Abs(camera.Size - 24.0f) < Tolerance,
            "doc 30:53 \"shows 24 gameplay meters vertically\"; KeepAspect must be Height or the "
                + "number would be the horizontal extent instead");

        // The camera's basis after configuration. Its up vector must be world -Z, which TDR-005
        // makes simulation north, and its forward must be straight down.
        Vector3 up = camera.Transform.Basis.Y;
        Vector3 forward = -camera.Transform.Basis.Z;
        Line("camera_up\t" + Vector(up));
        Line("camera_forward\t" + Vector(forward));

        Check(
            "north-is-screen-up",
            Math.Abs(up.X) < 1e-5 && Math.Abs(up.Y) < 1e-5 && up.Z < -0.999f,
            "the camera's up direction is world -Z, which TDR-005 § Coordinate contract makes "
                + "simulation north, so north is screen-up by construction");
        Check(
            "camera-looks-straight-down",
            Math.Abs(forward.X) < 1e-5 && forward.Y < -0.999f && Math.Abs(forward.Z) < 1e-5,
            "doc 30 § Camera and visual format: \"looks fully top-down\"");
        Check(
            "camera-has-no-yaw",
            Math.Abs(camera.Rotation.Y) < 1e-6f && Math.Abs(camera.Rotation.Z) < 1e-6f,
            "doc 30:53 \"non-rotating\": there is no yaw term for anything to rotate");

        // Horizontal extent at 16:9, which doc 30:53 states as approximately 42.7 m.
        double horizontal = 24.0 * 16.0 / 9.0;
        Line("horizontal_extent_at_16_9_m\t" + Invariant(horizontal));
        Check(
            "horizontal-extent-matches-the-documented-figure",
            Math.Abs(horizontal - 42.7) < 0.05,
            "doc 30:53 \"At 16:9 this yields approximately 42.7 meters horizontally\"");

        camera.Free();
        Line(string.Empty);
    }

    /// <summary>
    /// <c>VER-UI-002-001</c>: the radial deadzone is 0.18 and remaps the remainder to <c>[0,1]</c>.
    /// </summary>
    private void RunDeadzoneSection()
    {
        Line("## deadzone-remaps-radially");

        MovementInputAdapter adapter = new();
        Line("deadzone\t" + Invariant(adapter.RadialDeadzone));
        Check(
            "deadzone-is-0.18",
            Math.Abs(adapter.RadialDeadzone - 0.18) < Tolerance,
            "doc 60:65 \"a configurable radial deadzone, initially 0.18\"");

        Line("sampled_x\tsampled_y\tnonzero\traw_x\traw_y\tmagnitude");
        (double X, double Y)[] samples =
        {
            (0.0, 0.0),
            (0.10, 0.0),
            (0.18, 0.0),
            (0.19, 0.0),
            (0.5, 0.0),
            (1.0, 0.0),
            (0.13, 0.13),
            (1.0, 1.0),
        };

        foreach ((double x, double y) in samples)
        {
            bool nonzero = MovementInputAdapter.ApplyRadialDeadzone(
                x,
                y,
                adapter.RadialDeadzone,
                out double rawX,
                out double rawY);

            Line(
                Invariant(x) + "\t" + Invariant(y) + "\t" + (nonzero ? "yes" : "no") + "\t"
                + Invariant(rawX) + "\t" + Invariant(rawY) + "\t"
                + Invariant(double.Hypot(rawX, rawY)));
        }

        MovementInputAdapter.ApplyRadialDeadzone(0.18, 0.0, 0.18, out double atX, out double atY);
        Check(
            "exactly-at-the-deadzone-is-a-stop",
            atX == 0.0 && atY == 0.0,
            "the deadzone test is inclusive, so a sample exactly at it is an exact stop");

        MovementInputAdapter.ApplyRadialDeadzone(0.19, 0.0, 0.18, out double justX, out _);
        Check(
            "just-outside-the-deadzone-is-a-small-magnitude",
            justX > 0.0 && justX < 0.02,
            "doc 60:65 remaps the REMAINING magnitude, so just outside the deadzone is near zero "
                + "rather than jumping to 0.18, which is the discontinuity a bare threshold gives");

        MovementInputAdapter.ApplyRadialDeadzone(1.0, 0.0, 0.18, out double fullX, out _);
        Check(
            "full-deflection-remaps-to-one",
            Math.Abs(fullX - 1.0) < Tolerance,
            "doc 60:65 remaps to [0,1], so full deflection is exactly 1");

        MovementInputAdapter.ApplyRadialDeadzone(1.0, 1.0, 0.18, out double cornerX, out double cornerY);
        Check(
            "a-corner-sample-is-clamped-to-unit-magnitude",
            double.Hypot(cornerX, cornerY) <= 1.0 + Tolerance,
            "doc 20 § Active commands defines the intent domain as [0,1]; a device corner sample must "
                + "not leave it");

        MovementInputAdapter.ApplyRadialDeadzone(0.13, 0.13, 0.18, out double diagX, out double diagY);
        Check(
            "the-deadzone-is-radial-not-per-axis",
            diagX != 0.0 && diagY != 0.0,
            "0.13 on each axis is radial magnitude 0.1838, which EXCEEDS a radial deadzone of 0.18 and "
                + "so passes it, while each axis on its own is BELOW 0.18 and a per-axis deadzone would "
                + "zero both. This is the sample that tells the two implementations apart, and the "
                + "radial one must report nonzero here");

        Line(string.Empty);
    }

    /// <summary>
    /// <c>VER-UI-002-002</c>: a sample below the deadzone does not change persistent facing.
    /// </summary>
    private void RunFacingSection()
    {
        Line("## subdeadzone-input-preserves-facing");

        RunComposition run = RunComposition.CreateGraybox(0xE71DE_0000_0001UL);
        MovementInputAdapter adapter = new();
        long sequence = CommandEnvelope.FirstSequence;

        // Hold north for ten ticks, so there is a facing worth preserving.
        for (int tick = 0; tick < 10; tick++)
        {
            SubmitThroughAdapter(run, adapter, 0.0, 1.0, ref sequence);
            run.Host.Step(TickRate.SecondsPerTick);
        }

        double facingAfterHold = run.World.Player.FacingRadians;
        Line("facing_after_holding_north_rad\t" + Invariant(facingAfterHold));

        // Now a drifting stick at rest, well inside the deadzone.
        for (int tick = 0; tick < 10; tick++)
        {
            SubmitThroughAdapter(run, adapter, 0.04, -0.03, ref sequence);
            run.Host.Step(TickRate.SecondsPerTick);
        }

        double facingAfterDrift = run.World.Player.FacingRadians;
        Line("facing_after_stick_drift_rad\t" + Invariant(facingAfterDrift));

        Check(
            "facing-survives-stick-drift",
            Math.Abs(facingAfterDrift - facingAfterHold) < Tolerance,
            "doc 60:66 \"Tiny input below deadzone does not change persistent facing\"");
        Check(
            "facing-after-holding-north-is-a-quarter-turn",
            Math.Abs(facingAfterHold - (Math.PI / 2.0)) < 1e-9,
            "north is a quarter turn counterclockwise from east");

        Line(string.Empty);
    }

    /// <summary>
    /// <c>VER-UI-002-003</c>: every sample targets the tick the admission window is open for.
    /// </summary>
    private void RunOpenTickSection()
    {
        Line("## samples-target-the-open-tick");
        Line("before_step_open_tick\tenvelope_target_tick\tadmitted\tafter_step_open_tick");

        RunComposition run = RunComposition.CreateGraybox(0x0FEED_0000_0002UL);
        long sequence = CommandEnvelope.FirstSequence;
        bool everyEnvelopeTargetedTheOpenTick = true;
        bool everySampleWasAdmitted = true;

        for (int tick = 0; tick < 5; tick++)
        {
            long openBefore = run.OpenTick.Index;
            CommandEnvelope envelope = run.ComposeEnvelope(sequence++, 1.0, 0.0);
            bool admitted = run.CommandGate.TryAdmit(envelope, out _);
            run.Host.Step(TickRate.SecondsPerTick);

            everyEnvelopeTargetedTheOpenTick &= envelope.TargetTick.Index == openBefore;
            everySampleWasAdmitted &= admitted;

            Line(
                openBefore.ToString(CultureInfo.InvariantCulture) + "\t"
                + envelope.TargetTick.Index.ToString(CultureInfo.InvariantCulture) + "\t"
                + (admitted ? "yes" : "no") + "\t"
                + run.OpenTick.Index.ToString(CultureInfo.InvariantCulture));
        }

        Check(
            "every-envelope-targets-the-open-tick",
            everyEnvelopeTargetedTheOpenTick,
            "the target tick is read from the gate rather than counted by the producer, so a producer "
                + "cannot address a frozen tick by miscounting");
        Check("every-sample-was-admitted", everySampleWasAdmitted, "no sample was late");

        // A stale envelope: composed for a tick, submitted after that tick froze.
        CommandEnvelope stale = run.ComposeEnvelope(sequence++, 1.0, 0.0);
        run.Host.Step(TickRate.SecondsPerTick);
        bool staleAdmitted = run.CommandGate.TryAdmit(stale, out _);
        Line("stale_envelope_admitted\t" + (staleAdmitted ? "yes" : "no"));
        Check(
            "a-sample-for-a-frozen-tick-is-rejected",
            !staleAdmitted,
            "applying it a tick late would be a command applied outside the tick it was normalized for");

        Line(string.Empty);
    }

    /// <summary>
    /// <c>VER-PRE-001-002</c>: the mech moves under input, and the rendered transform is the
    /// authoritative position through the documented mapping and nothing else.
    /// </summary>
    private void RunMovementSection()
    {
        Line("## presentation-holds-no-rule");
        Line("# The four cardinals in turn, 30 ticks each (half a second). sim_x and sim_y are the");
        Line("# authoritative pair from the published snapshot; world_x/world_y/world_z are the");
        Line("# rendered transform of the PlayerBody pivot. TDR-005 requires world_x == sim_x,");
        Line("# world_z == -sim_y, and world_y == 0 for the ground-plane pivot.");
        Line("phase\ttick\tsim_x\tsim_y\tfacing_rad\tworld_x\tworld_y\tworld_z");

        RunComposition run = RunComposition.CreateGraybox(0x0A0DE_0000_0003UL);
        MovementInputAdapter adapter = new();
        long sequence = CommandEnvelope.FirstSequence;
        Node3D pivot = new();

        (string Name, double X, double Y)[] legs =
        {
            ("east", 1.0, 0.0),
            ("north", 0.0, 1.0),
            ("west", -1.0, 0.0),
            ("south", 0.0, -1.0),
            ("release", 0.0, 0.0),
        };

        bool mappingHeldEverywhere = true;
        PlanarVector startOfLeg = run.World.Player.Position;

        foreach ((string name, double x, double y) in legs)
        {
            startOfLeg = run.World.Player.Position;

            for (int tick = 0; tick < 30; tick++)
            {
                SubmitThroughAdapter(run, adapter, x, y, ref sequence);
                run.Host.Step(TickRate.SecondsPerTick);

                PresentationSnapshot published = run.Snapshots.Latest!;
                PlanarVector authoritative = PlanarVector.FromComponents(
                    published.PlayerPositionX,
                    published.PlayerPositionY);

                // Exactly what the shipping path does with a snapshot.
                RunSceneRoot.ApplyGroundTransform(pivot, authoritative, published.PlayerFacingRadians);

                mappingHeldEverywhere &=
                    Math.Abs(pivot.Position.X - authoritative.X) < 1e-5
                    && Math.Abs(pivot.Position.Y) < 1e-6
                    && Math.Abs(pivot.Position.Z - (-authoritative.Y)) < 1e-5;

                // Log the first and last tick of each leg; the middle is uniform by construction.
                if (tick == 0 || tick == 29)
                {
                    Line(
                        name + "\t" + published.Tick.ToString(CultureInfo.InvariantCulture) + "\t"
                        + Invariant(authoritative.X) + "\t" + Invariant(authoritative.Y) + "\t"
                        + Invariant(published.PlayerFacingRadians) + "\t"
                        + Invariant(pivot.Position.X) + "\t" + Invariant(pivot.Position.Y) + "\t"
                        + Invariant(pivot.Position.Z));
                }
            }

            PlanarVector endOfLeg = run.World.Player.Position;
            double travelled = startOfLeg.DistanceTo(endOfLeg);
            Line("# leg " + name + " travelled " + Invariant(travelled) + " m in 30 ticks");

            if (name == "release")
            {
                Check(
                    "releasing-input-stops-the-mech",
                    travelled < Tolerance,
                    "docs/30:64 \"Releasing input stops the mech immediately\"");
            }
            else
            {
                Check(
                    "holding-" + name + "-moves-the-mech-at-base-speed",
                    Math.Abs(travelled - (30.0 * PlayerMovement.BaseDisplacementPerTickMeters)) < 1e-9,
                    "30 ticks at 0.05 m is 1.5 m, which is half a second at the 3.0 m/s base speed");
            }
        }

        Check(
            "the-rendered-transform-is-the-authoritative-pair-through-the-mapping",
            mappingHeldEverywhere,
            "TDR-005 § Coordinate contract: east to world +X, north to world -Z, and the pivot's own "
                + "height is zero so the model's resting elevation is never part of the position");

        pivot.Free();
        Line(string.Empty);
    }

    /// <summary>
    /// <c>VER-PRE-001-001</c>: interpolation is presentation only and never changes the
    /// authoritative position.
    /// </summary>
    private void RunInterpolationSection()
    {
        Line("## interpolation-is-presentation-only");

        RunComposition run = RunComposition.CreateGraybox(0x1EDEF_000_0004UL);
        MovementInputAdapter adapter = new();
        long sequence = CommandEnvelope.FirstSequence;

        for (int tick = 0; tick < 12; tick++)
        {
            SubmitThroughAdapter(run, adapter, 1.0, 0.0, ref sequence);
            run.Host.Step(TickRate.SecondsPerTick);
        }

        PresentationSnapshot previous = run.Snapshots.Previous!;
        PresentationSnapshot latest = run.Snapshots.Latest!;
        PlanarVector from = PlanarVector.FromComponents(previous.PlayerPositionX, previous.PlayerPositionY);
        PlanarVector to = PlanarVector.FromComponents(latest.PlayerPositionX, latest.PlayerPositionY);
        PlanarVector authoritativeBefore = run.World.Player.Position;

        Line("previous_tick\t" + previous.Tick.ToString(CultureInfo.InvariantCulture));
        Line("latest_tick\t" + latest.Tick.ToString(CultureInfo.InvariantCulture));
        Line("fraction\trendered_x\trendered_z");

        Node3D pivot = new();
        bool everyRenderedPositionIsBetweenTheAnchors = true;

        foreach (double fraction in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            PlanarVector rendered = from + ((to - from) * fraction);
            RunSceneRoot.ApplyGroundTransform(pivot, rendered, latest.PlayerFacingRadians);
            Line(
                Invariant(fraction) + "\t" + Invariant(pivot.Position.X) + "\t"
                + Invariant(pivot.Position.Z));

            everyRenderedPositionIsBetweenTheAnchors &=
                rendered.X >= from.X - Tolerance && rendered.X <= to.X + Tolerance;
        }

        Check(
            "interpolation-stays-between-the-two-committed-anchors",
            everyRenderedPositionIsBetweenTheAnchors,
            "doc 30 § Snapshot consumption: presentation \"consumes the two most recent committed "
                + "simulation snapshots\" and does not extrapolate beyond them");
        Check(
            "interpolating-does-not-move-the-authoritative-body",
            run.World.Player.Position == authoritativeBefore,
            "doc 10 § Architectural style: presentation \"may interpolate but never decide\". Five "
                + "renders at five fractions left the authoritative position untouched");

        // The shortest angular path, which is the case a naive lerp gets wrong.
        double justNorthOfWest = (Math.PI * 7.0) / 8.0;
        double justSouthOfWest = -(Math.PI * 7.0) / 8.0;
        double halfway = RunSceneRoot.InterpolateAngle(justNorthOfWest, justSouthOfWest, 0.5);
        Line("wrap_from_rad\t" + Invariant(justNorthOfWest));
        Line("wrap_to_rad\t" + Invariant(justSouthOfWest));
        Line("wrap_halfway_rad\t" + Invariant(halfway));

        Check(
            "facing-interpolates-the-short-way-across-the-wrap",
            Math.Abs(Math.Abs(halfway) - Math.PI) < 1e-9,
            "doc 30 § Snapshot consumption: facing interpolates \"along the shortest valid "
                + "planar/angle path\". Turning from just north of west to just south of west is a "
                + "quarter turn through west, not most of a full turn the other way; a naive lerp "
                + "would report 0 radians, which is due east");

        pivot.Free();
        Line(string.Empty);
    }

    private static void SubmitThroughAdapter(
        RunComposition run,
        MovementInputAdapter adapter,
        double sampledX,
        double sampledY,
        ref long sequence)
    {
        MovementInputAdapter.ApplyRadialDeadzone(
            sampledX,
            sampledY,
            adapter.RadialDeadzone,
            out double rawX,
            out double rawY);

        run.CommandGate.TryAdmit(run.ComposeEnvelope(sequence, rawX, rawY), out _);
        sequence++;
    }

    private static string ReadSetting(string name)
    {
        string prefix = "--" + name.ToLowerInvariant().Replace('_', '-') + "=";
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return argument[prefix.Length..];
            }
        }

        return OS.GetEnvironment(name);
    }

    private static string Invariant(double value)
    {
        return value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private static string Vector(Vector3 value)
    {
        return "(" + Invariant(value.X) + "," + Invariant(value.Y) + "," + Invariant(value.Z) + ")";
    }

    private void Line(string text)
    {
        _transcript.Append(text).Append('\n');
    }

    private void Check(string name, bool held, string because)
    {
        _assertionsRun++;
        if (!held)
        {
            _assertionsFailed++;
        }

        Line((held ? "PASS\t" : "FAIL\t") + name + "\t" + because);
    }

    private void WriteTranscript(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        string path = Path.Combine(outputDirectory, TranscriptFileName);

        // Written atomically, so a harness that is killed leaves no transcript rather than a
        // truncated one - the same reason GodotTestRunner writes its report that way.
        string temporary = path + ".partial";
        File.WriteAllText(temporary, _transcript.ToString());
        File.Move(temporary, path, overwrite: true);

        GD.Print("MechaMiner: run slice transcript " + path);
    }
}
