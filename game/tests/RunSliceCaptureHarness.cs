using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using MechaMiner.Game.Presentation;

namespace MechaMiner.Game.EngineTesting;

/// <summary>
/// Launches the unmodified production run scene on a real display, presses real keys at it, and saves
/// screen captures alongside the authoritative positions they correspond to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Development scaffolding</b>, on the same footing as the rest of <c>game/tests/</c>: removed from
/// compilation under <c>ExportRelease</c> by <c>game/MechaMiner.Game.csproj</c>.
/// </para>
/// <para>
/// This exists to close the one gap <see cref="RunSliceEvidenceHarness"/> cannot: that harness drives
/// the adapter rule and the simulation directly, so it proves the movement path but says nothing about
/// whether a physical key is bound to the right logical action. This one injects
/// <c>InputEventKey</c> with a physical keycode through <c>Input.ParseInputEvent</c>, so the event
/// travels the whole real route - physical keycode, to the <c>[input]</c> action map in
/// <c>project.godot</c>, to <c>Input.GetVector</c>, to <see cref="MovementInputAdapter"/>, to a command
/// envelope, to the admission gate, to phase 5, to a published snapshot, to a rendered transform, to
/// pixels. Nothing is stubbed and no seam is added to production code for its benefit: it instantiates
/// <c>res://scenes/Run.tscn</c> exactly as shipped.
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

    /// <summary>Environment variable naming the directory captures and the log go to.</summary>
    internal const string OutputDirectoryVariable = "MECHAMINER_RUN_SLICE_CAPTURE";

    private const string RunScenePath = "res://scenes/Run.tscn";

    /// <summary>One scripted beat: hold a set of keys for a number of frames, then capture.</summary>
    private sealed record Beat(string Label, Key[] Held, int Frames);

    private readonly List<string> _log = new();

    private readonly Beat[] _beats =
    {
        new("01-at-rest", Array.Empty<Key>(), 20),
        new("02-holding-east", new[] { Key.D }, 90),
        new("03-holding-north", new[] { Key.W }, 90),
        new("04-holding-north-east", new[] { Key.W, Key.D }, 90),
        new("05-released", Array.Empty<Key>(), 30),
    };

    private string _outputDirectory = string.Empty;
    private RunSceneRoot? _runScene;
    private int _beatIndex;
    private int _framesInBeat;
    private Key[] _currentlyHeld = Array.Empty<Key>();
    private bool _finished;

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

        Log("# MechaMiner run slice capture. Each row is the authoritative state at the frame the");
        Log("# matching PNG was taken. The physical-keycode bindings are verified below as an");
        Log("# InputMap lookup; the held state is then driven through Input.ActionPress, because a");
        Log("# synthesized key event does not enter the Input singleton's held state. See");
        Log("# RunSliceCaptureHarness.ApplyHeldKeys for the measurement behind that.");
        Log("display_server\t" + DisplayServer.GetName());
        Log("rendering_method\t"
            + ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString());
        Log("video_adapter\t" + RenderingServer.GetVideoAdapterName());
        Log(string.Empty);
        // The action map as actually loaded. If these are absent the bindings never reached the
        // engine, which is a different failure from a binding that does not match.
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
        }

        // The physical-keycode bindings, checked as a pure InputMap lookup rather than through the
        // Input singleton's state machine. This is what proves that pressing D really is bound to
        // move_east: InputMap.EventIsAction answers the question the action map was written to answer,
        // and it does so without depending on how the event was delivered.
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
            Log(
                "physical_key_binds_action\t" + key + "\t" + action + "\t"
                + (InputMap.EventIsAction(probe, action) ? "yes" : "no"));
        }

        Log(string.Empty);
        Log("capture\tkeys_held\ttick\tsim_x\tsim_y\tfacing_rad\trendered_world_x\trendered_world_z"
            + "\taction_east\taction_north\tget_vector");

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
        if (_framesInBeat < _beats[_beatIndex].Frames)
        {
            return;
        }

        CaptureCurrentBeat();

        _beatIndex++;
        _framesInBeat = 0;

        if (_beatIndex >= _beats.Length)
        {
            _finished = true;
            ApplyHeldKeys(Array.Empty<Key>());
            WriteLog();
            GD.Print("MechaMiner: run slice capture complete");
            GetTree().Quit(0);
            return;
        }

        ApplyHeldKeys(_beats[_beatIndex].Held);
    }

    private void CaptureCurrentBeat()
    {
        if (_runScene?.Run is null)
        {
            return;
        }

        Beat beat = _beats[_beatIndex];
        var player = _runScene.Run.World.Player;
        Node3D pivot = _runScene.GetNode<Node3D>("PlayerBody");

        Vector2 composed = Input.GetVector(
            MovementInputAdapter.MoveWestAction,
            MovementInputAdapter.MoveEastAction,
            MovementInputAdapter.MoveSouthAction,
            MovementInputAdapter.MoveNorthAction,
            deadzone: 0.0f);

        Log(
            beat.Label + "\t"
            + (beat.Held.Length == 0 ? "none" : string.Join("+", beat.Held)) + "\t"
            + _runScene.Run.World.CommittedTickCount.ToString(CultureInfo.InvariantCulture) + "\t"
            + Invariant(player.Position.X) + "\t"
            + Invariant(player.Position.Y) + "\t"
            + Invariant(player.FacingRadians) + "\t"
            + Invariant(pivot.Position.X) + "\t"
            + Invariant(pivot.Position.Z) + "\t"
            + Invariant(Input.GetActionStrength(MovementInputAdapter.MoveEastAction)) + "\t"
            + Invariant(Input.GetActionStrength(MovementInputAdapter.MoveNorthAction)) + "\t"
            + "(" + Invariant(composed.X) + "," + Invariant(composed.Y) + ")");

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
