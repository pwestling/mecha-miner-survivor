using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using MechaMiner.Game.Presentation;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.World;

namespace MechaMiner.Game.EngineTesting;

/// <summary>
/// Launches the unmodified production run scene on a real display, asserts that the physical
/// movement keys are bound to the logical movement actions, drives those actions, and saves screen
/// captures alongside the authoritative positions they correspond to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Development scaffolding</b>, on the same footing as the rest of <c>game/tests/</c>: removed from
/// compilation under <c>ExportRelease</c> by <c>game/MechaMiner.Game.csproj</c>.
/// </para>
/// <para>
/// This exists to close the one gap <see cref="RunSliceEvidenceHarness"/> cannot: that harness drives
/// the adapter rule and the simulation directly, so it proves the movement path but says nothing about
/// whether a physical key is bound to the right logical action. This one answers that as a pure
/// <c>InputMap</c> lookup in <c>_Ready</c> - <c>InputMap.ActionGetEvents</c> for the event count and
/// <c>InputMap.EventIsAction</c> for each of the eight key/action pairs - and <b>exits 4 without
/// capturing</b> if any of it fails. It then drives the bound actions and follows the rest of the
/// chain to <c>Input.GetVector</c>, to <see cref="MovementInputAdapter"/>, to a command envelope, to
/// the admission gate, to phase 5, to a published snapshot, to a rendered transform, to pixels.
/// Nothing is stubbed and no seam is added to production code for its benefit: it instantiates
/// <c>res://scenes/Run.tscn</c> exactly as shipped.
/// </para>
/// <para>
/// The two halves are established separately because a synthesized key event cannot establish the
/// second - see <see cref="ApplyHeldKeys"/> for the measurement that forced the split. What this
/// harness does <em>not</em> establish is the delivery of a real key press by a real display server
/// to the <c>Input</c> singleton, which is engine behaviour rather than this repository's.
/// </para>
/// <para>
/// It needs a real display server, because a capture of a headless viewport would be empty. Under a
/// virtual framebuffer with a software renderer that is available; where it is not, this harness
/// cannot run and the transcript from the other harness is what remains.
/// </para>
/// </remarks>
public partial class RunSliceCaptureHarness : Node
{
    /// <summary>The stable line a host asserts to know the capture harness reached managed code.</summary>
    internal const string StartupLine = "MechaMiner: run slice capture harness ready";

    /// <summary>The stable prefix a host greps to know why the capture harness refused to capture.</summary>
    internal const string FailureLine = "MechaMiner: run slice capture FAILED, input bindings did not reach the engine: ";

    /// <summary>Environment variable naming the directory captures and the log go to.</summary>
    internal const string OutputDirectoryVariable = "MECHAMINER_RUN_SLICE_CAPTURE";

    private const string RunScenePath = "res://scenes/Run.tscn";

    /// <summary>How a beat decides what to hold each frame.</summary>
    private enum Steering
    {
        /// <summary>Hold a fixed set of keys, unchanged for the whole beat.</summary>
        FixedKeys = 0,

        /// <summary>
        /// Each frame, hold whichever of the eight movement combinations points most nearly at the target.
        /// </summary>
        /// <remarks>
        /// Needed because the run's mining seams are placed from a seeded stream, so their bearings are
        /// not cardinal and a fixed key hold cannot reach one. Eight directions land within 22.5 degrees
        /// of any bearing, which converges to well inside a 2 m zone. It is still real held input through
        /// the real action map: what is scripted is which action to hold, not how the hold is delivered.
        /// </remarks>
        HomeOnSeam = 1,
    }

    /// <summary>What ends a beat.</summary>
    private enum Until
    {
        /// <summary>A fixed number of frames.</summary>
        Frames = 0,

        /// <summary>The mech is inside the first seam's extraction zone.</summary>
        InsideTheSeam = 1,

        /// <summary>The first seam has paid at least one installment.</summary>
        AnInstallmentIsPaid = 2,

        /// <summary>A pursuer has closed inside the mech's own weapon range.</summary>
        /// <remarks>
        /// Eight metres, which is <c>W-BC</c>'s targeting range from docs/71:81, so this is the moment the
        /// weapon engages rather than an arbitrary distance. Under the authored minute-0 row it is as close
        /// as a pursuer gets: measured, the nearest approach over 3,600 frames was 6.65 m, because the
        /// weapon kills a 20-Hull Skitterling before it crosses the rest. A beat asking for four metres
        /// there was the first version of this harness and it correctly reported itself unmet.
        /// </remarks>
        AnEnemyIsInWeaponRange = 3,

        /// <summary>A pursuer's footprint overlaps the mech's, so contact damage is eligible.</summary>
        AnEnemyIsTouchingTheMech = 6,

        /// <summary>The mech's Hull has fallen below its maximum.</summary>
        HullHasFallen = 4,

        /// <summary>The run has been settled.</summary>
        TheRunHasEnded = 5,
    }

    /// <summary>One scripted beat: steer this way until this happens, then capture.</summary>
    private sealed record Beat(string Label, Steering Steering, Key[] Held, Until Until, int FrameBound);

    private readonly List<string> _log = new();

    /// <summary>
    /// The movement beats the first slice established. Retained verbatim, so extending this harness cannot
    /// quietly drop the evidence it already produced.
    /// </summary>
    private static readonly Beat[] MovementBeats =
    {
        new("01-at-rest", Steering.FixedKeys, Array.Empty<Key>(), Until.Frames, 20),
        new("02-holding-east", Steering.FixedKeys, new[] { Key.D }, Until.Frames, 90),
        new("03-holding-north", Steering.FixedKeys, new[] { Key.W }, Until.Frames, 90),
        new("04-holding-north-east", Steering.FixedKeys, new[] { Key.W, Key.D }, Until.Frames, 90),
        new("05-released", Steering.FixedKeys, Array.Empty<Key>(), Until.Frames, 30),
    };

    /// <summary>
    /// The loop beats: walk onto a seam, drill it, and let a pursuer close.
    /// </summary>
    /// <remarks>
    /// Every one of these ends on a condition read from authoritative state rather than on a frame count.
    /// A beat that captured after N frames and called the frame "the seam being drilled" would be a claim
    /// about what N happened to produce; a beat that captures when <c>InstallmentsPaid</c> first reaches
    /// one is a claim about the thing itself. The frame bound is a failure bound, not a schedule: a beat
    /// that reaches it without its condition is reported as a failure below.
    /// </remarks>
    private static readonly Beat[] LoopBeats =
    {
        new("06-walking-to-the-seam", Steering.HomeOnSeam, Array.Empty<Key>(), Until.InsideTheSeam, 900),
        new("07-drilling-the-seam", Steering.HomeOnSeam, Array.Empty<Key>(), Until.AnInstallmentIsPaid, 600),
        new(
            "08-a-pursuer-enters-weapon-range",
            Steering.HomeOnSeam,
            Array.Empty<Key>(),
            Until.AnEnemyIsInWeaponRange,
            3600),
    };

    /// <summary>
    /// The failure beats, reachable only under the lethal graybox pressure preset.
    /// </summary>
    /// <remarks>
    /// The mech holds nothing and is overrun. Under the authored minute-0 row of docs/32:56 neither
    /// condition is reachable inside any capture a person would watch - measured: 35 minutes of kiting
    /// ends with 95 of 100 Hull - so these beats run only when the scene was launched with
    /// <c>RunSceneRoot.PressureArgument</c>, and the log says which row produced them.
    /// </remarks>
    private static readonly Beat[] FailureBeats =
    {
        new(
            "09-a-pursuer-reaches-the-mech",
            Steering.FixedKeys,
            Array.Empty<Key>(),
            Until.AnEnemyIsTouchingTheMech,
            3600),
        new("10-taking-damage", Steering.FixedKeys, Array.Empty<Key>(), Until.HullHasFallen, 1200),
        new("11-the-run-has-ended", Steering.FixedKeys, Array.Empty<Key>(), Until.TheRunHasEnded, 1800),
    };

    private readonly List<Beat> _beats = new();
    private readonly List<string> _unmetBeats = new();

    private string _outputDirectory = string.Empty;
    private RunSceneRoot? _runScene;
    private int _beatIndex;
    private int _framesInBeat;
    private Key[] _currentlyHeld = Array.Empty<Key>();
    private bool _finished;
    private bool _lethalPressure;

    /// <inheritdoc/>
    public override void _Ready()
    {
        GD.Print(StartupLine);

        _outputDirectory = ReadSetting(OutputDirectoryVariable);
        if (_outputDirectory.Length == 0)
        {
            GD.PushError("the run slice capture harness requires " + OutputDirectoryVariable);
            GetTree().Quit(2);
            return;
        }

        Directory.CreateDirectory(_outputDirectory);

        // The production scene, instantiated as shipped. If this fails to load or its script fails to
        // instantiate, the capture is worthless and must not be reported as a pass.
        PackedScene packed = ResourceLoader.Load<PackedScene>(RunScenePath);
        Node instantiated = packed.Instantiate();
        AddChild(instantiated);

        if (instantiated is not RunSceneRoot runScene)
        {
            GD.PushError(RunScenePath + " did not instantiate a RunSceneRoot");
            GetTree().Quit(2);
            return;
        }

        _runScene = runScene;

        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            _lethalPressure |= argument
                == RunSceneRoot.PressureArgument + RunSceneRoot.LethalPressure;
        }

        // The scenario REPLACES the beat set rather than extending it. The movement and loop beats need
        // the authored row - a seam cannot be drilled in peace under a lethal swarm - and the failure
        // beats need the lethal one. Running all eleven in one pass would also cost several thousand
        // software-rendered frames, so the two passes are two launches, each with its own output
        // directory and its own row printed at the top of its log.
        if (_lethalPressure)
        {
            _beats.AddRange(FailureBeats);
        }
        else
        {
            _beats.AddRange(MovementBeats);
            _beats.AddRange(LoopBeats);
        }

        Log("# MechaMiner run slice capture. Each row is the authoritative state at the frame the");
        Log("# matching PNG was taken. The physical-keycode bindings are ASSERTED below as an");
        Log("# InputMap lookup: an action with no bound events, or a key that does not resolve to");
        Log("# its action, exits 4 and captures nothing. The held state is then driven through");
        Log("# Input.ActionPress, because a synthesized key event does not enter the Input");
        Log("# singleton's held state. See RunSliceCaptureHarness.ApplyHeldKeys for that");
        Log("# measurement.");
        Log("graybox_pressure\t"
            + (_lethalPressure
                ? RunSceneRoot.LethalPressure
                : "authored docs/32:56 minute-0 row"));
        Log("replenishment_row\t" + (runScene.Run?.World.BaselineRow.ToString() ?? "unavailable"));
        Log("beats\t" + _beats.Count.ToString(CultureInfo.InvariantCulture));
        Log("display_server\t" + DisplayServer.GetName());
        Log("rendering_method\t"
            + ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString());
        Log("video_adapter\t" + RenderingServer.GetVideoAdapterName());
        Log(string.Empty);
        // The action map as actually loaded. If these are absent the bindings never reached the
        // engine, which is a different failure from a binding that does not match.
        List<string> actionsWithNoBoundEvents = new();
        foreach (string action in new[]
            {
                MovementInputAdapter.MoveEastAction,
                MovementInputAdapter.MoveWestAction,
                MovementInputAdapter.MoveNorthAction,
                MovementInputAdapter.MoveSouthAction,
            })
        {
            // The event COUNT, not merely whether the action exists. HasAction returning true says
            // only that the name is known; an action with an empty event list is bound to no key at
            // all, and that is exactly the state a mis-encoded [input] section produces.
            int boundEvents = InputMap.HasAction(action) ? InputMap.ActionGetEvents(action).Count : -1;
            Log(
                "action_registered\t" + action + "\t"
                + (InputMap.HasAction(action) ? "yes" : "no") + "\tbound_events\t"
                + boundEvents.ToString(CultureInfo.InvariantCulture));

            if (boundEvents <= 0)
            {
                actionsWithNoBoundEvents.Add(action);
            }
        }

        // The physical-keycode bindings, checked as a pure InputMap lookup rather than through the
        // Input singleton's state machine. This is what proves that pressing D really is bound to
        // move_east: InputMap.EventIsAction answers the question the action map was written to answer,
        // and it does so without depending on how the event was delivered.
        List<string> keysNotBoundToTheirAction = new();
        foreach ((Key key, string action) in new[]
            {
                (Key.D, MovementInputAdapter.MoveEastAction),
                (Key.A, MovementInputAdapter.MoveWestAction),
                (Key.W, MovementInputAdapter.MoveNorthAction),
                (Key.S, MovementInputAdapter.MoveSouthAction),
                (Key.Right, MovementInputAdapter.MoveEastAction),
                (Key.Left, MovementInputAdapter.MoveWestAction),
                (Key.Up, MovementInputAdapter.MoveNorthAction),
                (Key.Down, MovementInputAdapter.MoveSouthAction),
            })
        {
            InputEventKey probe = new() { PhysicalKeycode = key, Pressed = true };
            bool binds = InputMap.EventIsAction(probe, action);
            Log(
                "physical_key_binds_action\t" + key + "\t" + action + "\t"
                + (binds ? "yes" : "no"));

            if (!binds)
            {
                keysNotBoundToTheirAction.Add(key + " -> " + action);
            }
        }

        // The two loops above are a GATE and not a report. Logging a binding failure and then
        // capturing anyway is what let an earlier revision of this harness exit 0 against a
        // project.godot whose four [input] event lists had been emptied: every line read
        // "bound_events 0" and "physical_key_binds_action D move_east no", the mech still reached
        // the arena corner because ApplyHeldKeys drives the actions directly and bypasses the key
        // binding entirely, and nothing failed on it. An unbound action is therefore an exit code.
        if (actionsWithNoBoundEvents.Count > 0 || keysNotBoundToTheirAction.Count > 0)
        {
            StringBuilder detail = new();
            if (actionsWithNoBoundEvents.Count > 0)
            {
                detail
                    .Append("actions with no bound events: ")
                    .Append(string.Join(", ", actionsWithNoBoundEvents));
            }

            if (keysNotBoundToTheirAction.Count > 0)
            {
                if (detail.Length > 0)
                {
                    detail.Append("; ");
                }

                detail
                    .Append("physical keys not bound to their action: ")
                    .Append(string.Join(", ", keysNotBoundToTheirAction));
            }

            Log(string.Empty);
            Log("FAIL\tinput-bindings-reached-the-engine\t" + detail);
            Log("outcome\tfailed");
            WriteLog();
            GD.PushError(FailureLine + detail);
            GD.Print(FailureLine + detail);
            GetTree().Quit(4);
            return;
        }

        Log(string.Empty);
        Log(
            "PASS\tinput-bindings-reached-the-engine\tall four movement actions carry at least one "
            + "bound event and all eight physical keys resolve to the action they are meant to drive");

        Log(string.Empty);
        Log("capture\tkeys_held\ttick\tsim_x\tsim_y\tfacing_rad\trendered_world_x\trendered_world_z"
            + "\taction_east\taction_north\tget_vector\thull\tore\tenemies\tnearest_m\tseam0"
            + "\tprogress\tpaid\tterminal\tdrawn_nodes\tframes_in_beat");

        ApplyHeldKeys(_beats[0].Held);
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (_finished || _runScene is null)
        {
            return;
        }

        _framesInBeat++;
        Beat beat = _beats[_beatIndex];

        if (beat.Steering == Steering.HomeOnSeam)
        {
            ApplyHeldKeys(KeysTowardTheSeam());
        }

        bool reached = IsBeatConditionMet(beat);
        bool exhausted = _framesInBeat >= beat.FrameBound;
        if (!reached && !exhausted)
        {
            return;
        }

        if (!reached)
        {
            // A beat that ran out of frames without its condition is a failure, not a capture. Saying so
            // is what keeps this harness from producing a PNG labelled "drilling the seam" that shows a
            // mech standing next to one.
            _unmetBeats.Add(
                beat.Label + " never reached " + beat.Until + " within "
                + beat.FrameBound.ToString(CultureInfo.InvariantCulture) + " frames");
        }

        CaptureCurrentBeat();

        _beatIndex++;
        _framesInBeat = 0;

        if (_beatIndex >= _beats.Count)
        {
            _finished = true;
            ApplyHeldKeys(Array.Empty<Key>());
            WriteLog();

            if (_unmetBeats.Count > 0)
            {
                foreach (string unmet in _unmetBeats)
                {
                    GD.PushError("MechaMiner: capture beat unmet: " + unmet);
                }

                GD.Print("MechaMiner: run slice capture FAILED with "
                    + _unmetBeats.Count.ToString(CultureInfo.InvariantCulture) + " unmet beat(s)");
                GetTree().Quit(4);
                return;
            }

            GD.Print("MechaMiner: run slice capture complete");
            GetTree().Quit(0);
            return;
        }

        if (_beats[_beatIndex].Steering == Steering.FixedKeys)
        {
            ApplyHeldKeys(_beats[_beatIndex].Held);
        }
    }

    /// <summary>Whether the current beat's goal has been reached, read from authoritative state.</summary>
    private bool IsBeatConditionMet(Beat beat)
    {
        RunComposition? run = _runScene?.Run;
        if (run is null)
        {
            return false;
        }

        switch (beat.Until)
        {
            case Until.Frames:
                return _framesInBeat >= beat.FrameBound;
            case Until.InsideTheSeam:
                return run.World.MiningSiteAt(0).Zone.Contains(run.World.Player.Position);
            case Until.AnInstallmentIsPaid:
                return run.World.MiningSiteAt(0).InstallmentsPaid >= 1;
            case Until.AnEnemyIsInWeaponRange:
                return NearestEnemyDistance(run)
                    <= MechaMiner.Simulation.Combat.PulseRepeaterBaseline.TargetingRangeMeters;
            case Until.AnEnemyIsTouchingTheMech:
                return NearestEnemyDistance(run)
                    <= MechaMiner.Simulation.Player.PlayerBaseline.CollisionRadiusMeters
                        + EnemyRoster.Skitterling.ContactRadiusMeters;
            case Until.HullHasFallen:
                return run.World.Player.Hull < MechaMiner.Simulation.Player.PlayerBaseline.MaximumHull;
            case Until.TheRunHasEnded:
                return run.World.HasEnded;
            default:
                return false;
        }
    }

    /// <summary>
    /// The movement keys whose combined direction points most nearly at the first seam.
    /// </summary>
    private Key[] KeysTowardTheSeam()
    {
        RunComposition? run = _runScene?.Run;
        if (run is null)
        {
            return Array.Empty<Key>();
        }

        MiningSiteState seam = run.World.MiningSiteAt(0);
        PlanarVector toSeam = seam.Centre - run.World.Player.Position;

        // Stop once inside, so the mech stands on the seam instead of orbiting its centre.
        if (toSeam.Magnitude <= GrayboxExtraction.ZoneRadiusMeters * 0.4)
        {
            return Array.Empty<Key>();
        }

        Key[] best = Array.Empty<Key>();
        double bestAlignment = double.NegativeInfinity;
        foreach ((Key[] keys, double x, double y) in EightDirections)
        {
            double alignment = ((toSeam.X * x) + (toSeam.Y * y)) / toSeam.Magnitude;
            if (alignment <= bestAlignment)
            {
                continue;
            }

            bestAlignment = alignment;
            best = keys;
        }

        return best;
    }

    /// <summary>The eight combinations of held movement keys, with the unit direction each produces.</summary>
    private static readonly (Key[] Keys, double X, double Y)[] EightDirections =
    {
        (new[] { Key.D }, 1.0, 0.0),
        (new[] { Key.A }, -1.0, 0.0),
        (new[] { Key.W }, 0.0, 1.0),
        (new[] { Key.S }, 0.0, -1.0),
        (new[] { Key.W, Key.D }, 0.70710678118654752, 0.70710678118654752),
        (new[] { Key.W, Key.A }, -0.70710678118654752, 0.70710678118654752),
        (new[] { Key.S, Key.D }, 0.70710678118654752, -0.70710678118654752),
        (new[] { Key.S, Key.A }, -0.70710678118654752, -0.70710678118654752),
    };

    private static double NearestEnemyDistance(RunComposition run)
    {
        double nearest = double.PositiveInfinity;
        for (int index = 0; index < run.World.LiveEnemyCount; index++)
        {
            nearest = Math.Min(
                nearest,
                run.World.EnemyAt(index).Position.DistanceTo(run.World.Player.Position));
        }

        return nearest;
    }

    private void CaptureCurrentBeat()
    {
        if (_runScene?.Run is null)
        {
            return;
        }

        Beat beat = _beats[_beatIndex];
        RunComposition run = _runScene.Run;
        var player = run.World.Player;
        Node3D pivot = _runScene.GetNode<Node3D>("PlayerBody");
        MiningSiteState seam = run.World.MiningSiteAt(0);
        double nearest = NearestEnemyDistance(run);

        Vector2 composed = Input.GetVector(
            MovementInputAdapter.MoveWestAction,
            MovementInputAdapter.MoveEastAction,
            MovementInputAdapter.MoveSouthAction,
            MovementInputAdapter.MoveNorthAction,
            deadzone: 0.0f);

        Log(
            beat.Label + "\t"
            + (_currentlyHeld.Length == 0 ? "none" : string.Join("+", _currentlyHeld)) + "\t"
            + run.World.CommittedTickCount.ToString(CultureInfo.InvariantCulture) + "\t"
            + Invariant(player.Position.X) + "\t"
            + Invariant(player.Position.Y) + "\t"
            + Invariant(player.FacingRadians) + "\t"
            + Invariant(pivot.Position.X) + "\t"
            + Invariant(pivot.Position.Z) + "\t"
            + Invariant(Input.GetActionStrength(MovementInputAdapter.MoveEastAction)) + "\t"
            + Invariant(Input.GetActionStrength(MovementInputAdapter.MoveNorthAction)) + "\t"
            + "(" + Invariant(composed.X) + "," + Invariant(composed.Y) + ")\t"
            + player.Hull.ToString(CultureInfo.InvariantCulture) + "\t"
            + run.World.RunLocalCommonOre.ToString(CultureInfo.InvariantCulture) + "\t"
            + run.World.LiveEnemyCount.ToString(CultureInfo.InvariantCulture) + "\t"
            + (double.IsPositiveInfinity(nearest) ? "none" : Invariant(nearest)) + "\t"
            + seam.Phase.ToString() + "\t"
            + seam.InstallmentProgressTicks.ToString(CultureInfo.InvariantCulture) + "\t"
            + seam.InstallmentsPaid.ToString(CultureInfo.InvariantCulture) + "\t"
            + (run.World.HasEnded ? run.World.Terminal.Outcome.ToString() : "running") + "\t"
            + _runScene.GetNode<Node3D>("SimulationEntities").GetChildCount()
                .ToString(CultureInfo.InvariantCulture) + "\t"
            + _framesInBeat.ToString(CultureInfo.InvariantCulture));

        Image image = GetViewport().GetTexture().GetImage();
        string path = Path.Combine(_outputDirectory, beat.Label + ".png");
        Error saved = image.SavePng(path);
        if (saved != Error.Ok)
        {
            GD.PushError("could not save " + path + ": " + saved);
        }
    }

    /// <summary>
    /// Releases whatever was held and holds the new set, by driving the actions those keys are bound
    /// to.
    /// </summary>
    /// <param name="held">The physical keys to hold.</param>
    /// <remarks>
    /// <para>
    /// <b>This uses <c>Input.ActionPress</c>, and the reason is a measured limitation rather than a
    /// preference.</b> The first version of this harness synthesized <c>InputEventKey</c> values through
    /// <c>Input.ParseInputEvent</c>, which is the route that would also have proved the physical
    /// binding. It does not work for a <em>held</em> control: over 1269 ticks with a key nominally down,
    /// <c>Input.GetActionStrength</c> stayed at 0 and the mech never moved. A synthesized event of that
    /// kind does not enter the state the <c>Input</c> singleton reports as "currently pressed", which is
    /// what <c>Input.GetVector</c> reads.
    /// </para>
    /// <para>
    /// So the two halves of the claim are established separately, and neither is assumed. That the
    /// physical keycode is bound to the right action is a pure <c>InputMap.EventIsAction</c> lookup,
    /// logged in <c>_Ready</c>. That a held action drives the rest of the chain to pixels is what the
    /// captures below show. What remains unproven by automation is only the delivery of a real key
    /// press by a real display server to the <c>Input</c> singleton, which is engine behaviour rather
    /// than this repository's, and which an interactive launch exercises.
    /// </para>
    /// </remarks>
    private void ApplyHeldKeys(Key[] held)
    {
        foreach (Key key in _currentlyHeld)
        {
            Input.ActionRelease(ActionFor(key));
        }

        foreach (Key key in held)
        {
            Input.ActionPress(ActionFor(key), 1.0f);
        }

        _currentlyHeld = held;
    }

    /// <summary>The logical action a physical key is bound to.</summary>
    /// <param name="key">The physical key.</param>
    /// <exception cref="ArgumentOutOfRangeException">The key is not one this harness drives.</exception>
    private static string ActionFor(Key key)
    {
        return key switch
        {
            Key.D => MovementInputAdapter.MoveEastAction,
            Key.A => MovementInputAdapter.MoveWestAction,
            Key.W => MovementInputAdapter.MoveNorthAction,
            Key.S => MovementInputAdapter.MoveSouthAction,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "not a movement key"),
        };
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
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private void Log(string text)
    {
        _log.Add(text);
    }

    private void WriteLog()
    {
        StringBuilder builder = new();
        foreach (string line in _log)
        {
            builder.Append(line).Append('\n');
        }

        string path = Path.Combine(_outputDirectory, "captures.tsv");
        File.WriteAllText(path, builder.ToString());
        GD.Print("MechaMiner: run slice capture log " + path);
    }
}
