using System;
using System.Collections.Generic;
using Godot;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Simulation.World;

namespace MechaMiner.Game.Presentation;

/// <summary>
/// The run scene's root: composes the authoritative run, feeds it input, steps it, and renders the
/// snapshots it publishes.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-PRE-001</c> presentation bridge and <c>CMP-PRE-002</c> camera, per
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry.
/// </para>
/// <para>
/// <b>It contains no gameplay rule, and the division is worth stating precisely because it is the
/// whole point of the architecture.</b> This node decides when to sample the device, how to turn a
/// frame delta into a call on the host, and where to put a mesh. It does not decide how fast the
/// mech moves, which way it faces, where it may go, or what its Hull is. Every one of those is a
/// value it reads out of a published snapshot, and the only way it can influence any of them is by
/// submitting a command envelope that the admission gate may reject.
/// </para>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Architectural style makes the simulation the
/// sole authority and presentation a consumer that "may interpolate but never decide". The
/// interpolation here is deliberately confined to the transform of a child node: the authoritative
/// position is the ground-plane centre from the snapshot, and
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract says "decorative model pivots and animation root motion never modify it".
/// The visible meshes hang off a pivot that is offset upward for presentation, and that offset
/// never travels back.
/// </para>
/// <para>
/// The run session is a constant here rather than derived from a clock or a random source, because
/// this slice has no run-selection flow: <c>FND-006</c>'s <c>run</c> verb and the application
/// routes own that. A run session must be nonzero (doc 10 § Commands and mutations) and stable for
/// the process, which a constant satisfies.
/// </para>
/// </remarks>
public partial class RunSceneRoot : Node3D
{
    /// <summary>The stable line a host asserts to know the run scene reached managed code.</summary>
    internal const string StartupLine = "MechaMiner: run scene ready";

    /// <summary>
    /// The prefix of the line naming the replenishment row this run composed under.
    /// </summary>
    /// <remarks>
    /// Printed on every launch, including a plain one. A row's provenance travels in its own label, so a
    /// reader of any log can see whether the pressure came from doc 32:56 or from a harness row nothing
    /// authored, without having to know which argument was passed.
    /// </remarks>
    internal const string ReplenishmentRowLine = "MechaMiner: replenishment row ";

    /// <summary>
    /// The camera's vertical extent in gameplay meters: <c>24</c>.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/30-presentation-and-rendering.md</c>:53 - "The gameplay camera is
    /// orthographic, north-up, and non-rotating and shows <b>24 gameplay meters vertically</b>. At
    /// 16:9 this yields approximately 42.7 meters horizontally". The same line says "Agents retain
    /// this value through M4 without preference review", so this is retained rather than chosen, and
    /// changing it needs the paired reference-layout captures that line requires.
    /// </remarks>
    internal const float CameraVerticalExtentMetres = 24.0f;

    /// <summary>How far above the ground plane the camera sits, in meters.</summary>
    /// <remarks>
    /// Presentation only. An orthographic projection's scale does not depend on distance, so this
    /// affects nothing but near/far clipping; it is far enough to clear any plausible prop height.
    /// </remarks>
    internal const float CameraHeightMetres = 40.0f;

    /// <summary>The run session this scene's run uses.</summary>
    /// <remarks>Nonzero, per doc 10 § Commands and mutations. Stable for the process.</remarks>
    internal const ulong DevelopmentRunSession = 0x4D45_4348_4100_0001UL;

    /// <summary>
    /// Launch argument selecting a graybox pressure preset instead of the authored minute-0 row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A graybox development launch option, and the reason it exists is measured.</b> At the pressure
    /// <c>docs/32-standard-wave-and-beacon-schedule.md</c>:56 authors for minute 0, a run is very nearly
    /// unlosable: 35 minutes of kiting ends with 95 of 100 Hull, because eight Skitterlings are 160 Hull
    /// between them and <c>W-BC</c> clears 32 Hull per second. That is doc 32 § Phase-level pressure
    /// curve:43's intent for minute 0, not a defect. It does mean that a person launching this scene
    /// cannot see the mech destroyed, and neither can a screen capture, so the failure half of the run
    /// loop would be unobservable in the shipping scene.
    /// </para>
    /// <para>
    /// The absent argument is the authored row, so a plain launch is unaffected and no default changes.
    /// The only accepted value is <c>lethal</c>, which selects <c>HarnessStressRows.LethalSwarm</c> - a
    /// row whose own label says no document states it, and which the transcript and every capture print.
    /// Anything else is refused loudly rather than silently ignored, because an option that quietly does
    /// nothing when misspelled is an option a reader will believe took effect.
    /// </para>
    /// <para>
    /// This is deliberately a launch argument and not a test seam: <c>game/tests/</c> is removed from
    /// compilation under <c>ExportRelease</c>, but a member added to this class for a harness's benefit
    /// would ship. An argument is read the same way by a person, by <c>FND-006</c>'s eventual <c>run</c>
    /// verb, and by the capture harness, and it needs no mutable static and no per-instance state.
    /// </para>
    /// </remarks>
    internal const string PressureArgument = "--mechaminer-graybox-pressure=";

    /// <summary>The only accepted value of <see cref="PressureArgument"/>.</summary>
    internal const string LethalPressure = "lethal";

    private readonly MovementInputAdapter _input = new();

    /// <summary>
    /// The visible node for each snapshot identity currently drawn, keyed by that identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pooled by identity rather than by index. A snapshot's entity list is in the simulation's stable
    /// order, and that order changes when a record is removed - <c>PackedEntityStore</c> swaps the last
    /// dense slot into the vacated one - so a node bound to list position 3 would jump to whichever body
    /// happened to be moved there. Binding to <c>EntityId</c> instead means a node follows one body for
    /// that body's whole life and is freed when it ends.
    /// </para>
    /// <para>
    /// doc 30 § Snapshot consumption and interpolation and doc 10 § Entity and scene boundary both note
    /// that "presentation mappings tolerate simulation entities disappearing before their final visual
    /// event completes". This map is that tolerance: an identity absent from the newest snapshot has its
    /// node freed, and nothing here asks the simulation why.
    /// </para>
    /// </remarks>
    private readonly Dictionary<EntityId, Node3D> _visibleNodes = new();

    private readonly List<EntityId> _vanished = new();
    private readonly HashSet<EntityId> _stillPresent = new();

    private RunComposition? _run;
    private Node3D? _playerPivot;
    private Node3D? _entityRoot;
    private Camera3D? _camera;
    private long _nextCommandSequence = CommandEnvelope.FirstSequence;
    private long _renderedTick = -1;
    private double _secondsSinceLatestPublication;

    /// <summary>The assembled run, once <c>_Ready</c> has composed it.</summary>
    internal RunComposition? Run => _run;

    /// <summary>
    /// The adapter this scene samples input through.
    /// </summary>
    /// <remarks>
    /// Named <c>InputAdapter</c> rather than <c>Input</c> on purpose: a member called <c>Input</c>
    /// would shadow the engine's static input class throughout this type, so a later
    /// <c>Input.IsActionPressed</c> written here would not compile and the reason would not be
    /// obvious.
    /// </remarks>
    internal MovementInputAdapter InputAdapter => _input;

    /// <summary>The interpolation fraction most recently used, in <c>[0, 1]</c>.</summary>
    internal double InterpolationFraction { get; private set; }

    /// <summary>Whether the most recent frame snapped instead of interpolating, and why.</summary>
    internal InterpolationSnapReason LastSnapReason { get; private set; }

    /// <inheritdoc/>
    public override void _Ready()
    {
        _playerPivot = GetNode<Node3D>("PlayerBody");
        _entityRoot = GetNode<Node3D>("SimulationEntities");
        _camera = GetNode<Camera3D>("GameplayCamera");

        ConfigureCamera(_camera);

        _run = RunComposition.CreateGraybox(DevelopmentRunSession, ReadReplenishmentRow());
        GD.Print(ReplenishmentRowLine + _run.World.BaselineRow.ToString());

        // Place the body at its authoritative position before the first frame, so the first thing
        // drawn is the deployment position rather than the scene file's placeholder transform.
        ApplyGroundTransform(_playerPivot, _run.World.Player.Position, _run.World.Player.FacingRadians);
        FollowWithCamera(_run.World.Player.Position);

        GD.Print(StartupLine);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The frame's three jobs, in this order and for these reasons. Sampling comes first so the
    /// newest input is available to the tick this frame is about to run, rather than arriving one
    /// tick late. Stepping comes second. Rendering comes last so it reads the snapshot the step just
    /// published instead of the previous frame's.
    /// </remarks>
    public override void _Process(double delta)
    {
        if (_run is null || _playerPivot is null || _camera is null)
        {
            return;
        }

        // A tick that threw ended the run through the technical-failure path, and every later Step
        // refuses by naming that failure (doc 20 § Tick transaction). Continuing to call it would turn
        // one recorded failure into an exception every frame, burying the original.
        if (_run.Host.HasEndedInTechnicalFailure)
        {
            return;
        }

        SubmitSampledInput(_run);
        _run.Host.Step(delta);

        // The run may have ended inside that step. Raising the terminal transition is the driver's, one
        // step out: RunClock refuses to commit a tick while a blocking reason is present, so raising it
        // from inside phase 13 would make the settling tick uncommittable and the host would file a
        // technical failure for a run that ended correctly. RunComposition.SettleTerminalTransition
        // records the whole reason.
        _ = _run.SettleTerminalTransition();

        Render(_run, _playerPivot, _camera, delta);
    }

    /// <summary>
    /// Reads the replenishment row this launch selects.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The pressure argument is present with a value that is not <see cref="LethalPressure"/>.
    /// </exception>
    /// <remarks>
    /// Refusing an unrecognized value rather than falling back is the point. A misspelled preset that
    /// silently produced the authored row would leave a capture labelled as one thing and rendered from
    /// another, which is the exact failure the label on <c>HarnessStressRows</c> exists to prevent.
    /// </remarks>
    private static BaselineReplenishmentRow ReadReplenishmentRow()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith(PressureArgument, StringComparison.Ordinal))
            {
                continue;
            }

            string value = argument[PressureArgument.Length..];
            if (string.Equals(value, LethalPressure, StringComparison.Ordinal))
            {
                return HarnessStressRows.LethalSwarm;
            }

            throw new ArgumentException(
                PressureArgument
                    + value
                    + " is not a graybox pressure preset. The only accepted value is '"
                    + LethalPressure
                    + "'; omit the argument for the authored minute-0 row of docs/32:56");
        }

        return MinuteZeroBaseline.Row;
    }

    /// <summary>
    /// Configures the gameplay camera: orthographic, north-up, non-rotating, 24 metres vertically.
    /// </summary>
    /// <param name="camera">The camera to configure.</param>
    /// <remarks>
    /// <para>
    /// Set in code as well as in the scene file. The scene file is what the editor shows and what a
    /// person would edit; this is what makes the value a contract rather than a saved property
    /// somebody can nudge in the inspector without noticing. doc 30:53 makes the extent retained
    /// through M4.
    /// </para>
    /// <para>
    /// The rotation is a quarter turn about X and nothing else. That makes the camera look straight
    /// down its own vertical, and it puts the camera's up direction along world negative Z - which
    /// TDR-005 § Coordinate contract makes simulation north. So north is screen-up, by construction
    /// rather than by adjustment, and there is no yaw term for anything to rotate.
    /// </para>
    /// </remarks>
    internal static void ConfigureCamera(Camera3D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        camera.Projection = Camera3D.ProjectionType.Orthogonal;

        // KeepHeight makes Size the VERTICAL extent, which is the axis doc 30:53 specifies. Under
        // KeepWidth the same number would be the horizontal extent and the view would be about 1.8
        // times too tall at 16:9.
        camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
        camera.Size = CameraVerticalExtentMetres;
        camera.Rotation = new Vector3(-Mathf.Pi / 2.0f, 0.0f, 0.0f);
        camera.Near = 0.05f;
        camera.Far = CameraHeightMetres * 2.0f;
    }

    /// <summary>
    /// Samples the device and submits the sample as a command envelope for the open tick.
    /// </summary>
    /// <param name="run">The run to submit to.</param>
    /// <remarks>
    /// A rejection is not an error to recover from here. The gate rejects a sample whose tick has
    /// already frozen, and the correct response is to let it go: the next frame samples again, and
    /// applying a stale sample a tick late would be worse than dropping it. doc 20 § Active commands
    /// normalizes an intent for a particular tick, so a sample that missed its tick is not input for
    /// another one.
    /// </remarks>
    private void SubmitSampledInput(RunComposition run)
    {
        // A sample below the deadzone reports false and yields (0, 0), and it is submitted anyway.
        // That is deliberate: an explicit zero is a stop, and doc 30:68 requires releasing input to
        // stop the mech immediately. Submitting nothing would instead leave the previous intent held,
        // which is what a dropped frame means and not what a released control means.
        _input.Sample(out double rawX, out double rawY);

        CommandEnvelope envelope = run.ComposeEnvelope(_nextCommandSequence, rawX, rawY);
        if (run.CommandGate.TryAdmit(envelope, out CommandRejection rejection))
        {
            _nextCommandSequence++;
            return;
        }

        // The sequence still advances. doc 10 § Commands and mutations makes the sequence monotonic
        // per run, and reusing a rejected one would make a later envelope indistinguishable from a
        // replay of this one to the gate's idempotency history.
        _nextCommandSequence++;
        _ = rejection;
    }

    private void Render(RunComposition run, Node3D playerPivot, Camera3D camera, double delta)
    {
        PresentationSnapshot? latest = run.Snapshots.Latest;
        if (latest is null)
        {
            // Nothing has been published yet, which happens for the first few frames while the
            // accumulator is still short of one tick. Keep drawing what is already drawn: inventing a
            // position would be presentation deciding.
            return;
        }

        if (latest.Tick != _renderedTick)
        {
            _renderedTick = latest.Tick;
            _secondsSinceLatestPublication = 0.0;
        }
        else
        {
            _secondsSinceLatestPublication += delta;
        }

        PresentationSnapshot? previous = run.Snapshots.Previous;
        if (previous is null)
        {
            // One snapshot is not enough to interpolate between, so show it exactly.
            InterpolationFraction = 1.0;
            LastSnapReason = InterpolationSnapReason.None;
            ApplyGroundTransform(
                playerPivot,
                PlanarVector.FromComponents(latest.PlayerPositionX, latest.PlayerPositionY),
                latest.PlayerFacingRadians);
            FollowWithCamera(PlanarVector.FromComponents(latest.PlayerPositionX, latest.PlayerPositionY));
            return;
        }

        PlanarVector from = PlanarVector.FromComponents(previous.PlayerPositionX, previous.PlayerPositionY);
        PlanarVector to = PlanarVector.FromComponents(latest.PlayerPositionX, latest.PlayerPositionY);

        // doc 30 § Snapshot consumption and interpolation: a large correction snaps rather than
        // sliding the body across the gap. The threshold is derived, not tuned - see
        // InterpolationSnapPolicy - and this slice can only ever trigger the distance case, since
        // nothing here spawns, teleports, re-enters, or terminates.
        LastSnapReason = InterpolationSnapPolicy.Documented.Evaluate(
            spawnedSinceOlderSnapshot: false,
            teleported: false,
            bossReEntered: false,
            terminalTransition: latest.IsTerminal,
            displacementMetres: from.DistanceTo(to));

        double fraction = LastSnapReason == InterpolationSnapReason.None
            ? Math.Clamp(_secondsSinceLatestPublication / TickRate.SecondsPerTick, 0.0, 1.0)
            : 1.0;
        InterpolationFraction = fraction;

        PlanarVector rendered = from + ((to - from) * fraction);
        double renderedFacing = InterpolateAngle(
            previous.PlayerFacingRadians,
            latest.PlayerFacingRadians,
            fraction);

        ApplyGroundTransform(playerPivot, rendered, renderedFacing);
        FollowWithCamera(rendered);
        RenderVisibleEntities(latest);
    }

    /// <summary>
    /// Places, creates, and frees one node per visible simulation entity in the newest snapshot.
    /// </summary>
    /// <param name="latest">The newest published snapshot.</param>
    /// <remarks>
    /// <para>
    /// <b>Positions are the snapshot's, uninterpolated.</b> The player body interpolates between the two
    /// latest snapshots because the camera follows it and its motion is the one a person watches closely.
    /// These do not, and that is a deliberate limit rather than an omission: interpolating a body that
    /// might have been removed between the two snapshots needs a rule for what to show when only one of
    /// the pair holds it, and doc 30 § Snapshot consumption and interpolation gives that rule to the
    /// snap policy, whose spawn and death cases <c>PRE-003</c> owns. Showing the authoritative position
    /// exactly is the honest degenerate case; it cannot be wrong, only stepped.
    /// </para>
    /// <para>
    /// <b>Every visual property here is graybox.</b> The three meshes are boxes and a flat disc, sized
    /// from the authoritative radii they represent, and <c>AST-006</c> replaces them along with the rest
    /// of the representative asset set. What is not graybox is the mapping: every node's position comes
    /// from <c>PresentationGroundMapping</c>, the same term of TDR-005's coordinate contract the player
    /// body uses.
    /// </para>
    /// </remarks>
    private void RenderVisibleEntities(PresentationSnapshot latest)
    {
        if (_entityRoot is null)
        {
            return;
        }

        _stillPresent.Clear();
        ReadOnlySpan<SnapshotEntity> entities = latest.VisibleEntities.Span;
        for (int index = 0; index < entities.Length; index++)
        {
            SnapshotEntity entity = entities[index];
            _stillPresent.Add(entity.Id);

            if (!_visibleNodes.TryGetValue(entity.Id, out Node3D? node))
            {
                node = CreateVisual(entity.Category);
                _entityRoot.AddChild(node);
                _visibleNodes.Add(entity.Id, node);
            }

            ApplyGroundTransform(
                node,
                PlanarVector.FromComponents(entity.PositionX, entity.PositionY),
                entity.FacingRadians);
        }

        _vanished.Clear();
        foreach (KeyValuePair<EntityId, Node3D> drawn in _visibleNodes)
        {
            if (!_stillPresent.Contains(drawn.Key))
            {
                _vanished.Add(drawn.Key);
            }
        }

        // Collected first, then removed. Freeing a node while enumerating the dictionary that holds it
        // would invalidate the enumerator - the presentation-side version of the deferral rule doc 10
        // § System phase ordering applies to the simulation's own stores.
        for (int index = 0; index < _vanished.Count; index++)
        {
            EntityId gone = _vanished[index];
            _visibleNodes[gone].QueueFree();
            _visibleNodes.Remove(gone);
        }
    }

    /// <summary>
    /// Builds the graybox visual for one population category.
    /// </summary>
    /// <param name="category">The category the snapshot entity carries.</param>
    /// <remarks>
    /// The sizes are the authoritative radii, doubled into diameters, so a person looking at a frame is
    /// looking at the real footprint rather than at an artist's guess about it: a Skitterling's box is
    /// its 0.44 m contact diameter (docs/31:39 against docs/31:35's 0.80M Ripper reference), a seam's
    /// disc is twice the extraction-zone radius, and a projectile is a small marker because doc 71:81
    /// gives Pulse Repeater no width at all.
    /// </remarks>
    private static Node3D CreateVisual(PopulationCategory category)
    {
        MeshInstance3D node = new();
        switch (category)
        {
            case PopulationCategory.OrdinaryEnemy:
                {
                    float diameter = (float)(EnemyRoster.Skitterling.ContactRadiusMeters * 2.0);
                    node.Mesh = new BoxMesh
                    {
                        Size = new Vector3(diameter, diameter * 0.6f, diameter),
                        Material = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.72f, 0.24f, 0.30f),
                            Roughness = 0.65f,
                        },
                    };
                    node.Position = new Vector3(0.0f, diameter * 0.3f, 0.0f);
                    break;
                }

            case PopulationCategory.MiningSite:
                {
                    float diameter = (float)(GrayboxExtraction.ZoneRadiusMeters * 2.0);
                    node.Mesh = new CylinderMesh
                    {
                        TopRadius = diameter / 2.0f,
                        BottomRadius = diameter / 2.0f,
                        Height = 0.04f,
                        Material = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.24f, 0.58f, 0.42f),
                            Roughness = 0.8f,
                        },
                    };
                    node.Position = new Vector3(0.0f, 0.02f, 0.0f);
                    break;
                }

            default:
                {
                    node.Mesh = new BoxMesh
                    {
                        Size = new Vector3(0.16f, 0.08f, 0.16f),
                        Material = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.95f, 0.88f, 0.45f),
                            Roughness = 0.3f,
                        },
                    };
                    node.Position = new Vector3(0.0f, 0.35f, 0.0f);
                    break;
                }
        }

        // The mesh hangs at its own height on a pivot whose own position is the authoritative
        // ground-plane centre, exactly as PlayerBody does, so a model's resting elevation is never part
        // of a position (TDR-005 § Coordinate contract).
        Node3D pivot = new();
        pivot.AddChild(node);
        return pivot;
    }

    /// <summary>
    /// Interpolates an angle along the shortest path, so a turn across the wrap point does not spin
    /// the long way round.
    /// </summary>
    /// <param name="from">The earlier angle in radians.</param>
    /// <param name="to">The later angle in radians.</param>
    /// <param name="fraction">The interpolation fraction in <c>[0, 1]</c>.</param>
    /// <remarks>
    /// doc 30 § Snapshot consumption and interpolation: "Position and facing interpolate along the
    /// shortest valid planar/angle path." Facing is in <c>(-pi, pi]</c>, so a body turning from just
    /// north of west to just south of west crosses the wrap and a naive lerp would rotate it almost
    /// all the way round instead of a few degrees.
    /// </remarks>
    internal static double InterpolateAngle(double from, double to, double fraction)
    {
        double difference = Math.IEEERemainder(to - from, Math.Tau);
        if (double.IsNaN(difference))
        {
            return to;
        }

        return from + (difference * fraction);
    }

    /// <summary>
    /// Places a node at an authoritative planar position with an authoritative facing.
    /// </summary>
    /// <param name="node">The node to place.</param>
    /// <param name="position">The authoritative ground-plane centre.</param>
    /// <param name="facingRadians">The authoritative facing, radians counterclockwise from east.</param>
    /// <remarks>
    /// <para>
    /// The mapping is <c>PresentationGroundMapping</c>'s and is not restated here. That type lives in
    /// the simulation assembly because the mapping is a term of TDR-005's coordinate contract, and a
    /// second copy of a sign convention is a second thing to get wrong.
    /// </para>
    /// <para>
    /// The height passed is zero: this node is the ground-plane pivot, and every visible mesh is a
    /// child of it with its own upward offset. That is what keeps a model's resting elevation out of
    /// the authoritative position.
    /// </para>
    /// <para>
    /// Facing maps to a rotation about world up. A rotation of <c>theta</c> about world +Y sends
    /// world +X to <c>(cos theta, 0, -sin theta)</c>, and under TDR-005 that is exactly the planar
    /// direction at <c>theta</c> counterclockwise from east - east being +X and north being -Z. So
    /// the authoritative angle is the engine angle with no conversion, provided the model's nose is
    /// modelled along +X, which the scene does.
    /// </para>
    /// </remarks>
    internal static void ApplyGroundTransform(Node3D node, PlanarVector position, double facingRadians)
    {
        ArgumentNullException.ThrowIfNull(node);

        PresentationGroundMapping.ToPresentationWorld(
            position,
            0.0,
            out double worldX,
            out double worldY,
            out double worldZ);

        node.Position = new Vector3((float)worldX, (float)worldY, (float)worldZ);
        node.Rotation = new Vector3(0.0f, (float)facingRadians, 0.0f);
    }

    /// <summary>
    /// Puts the camera over a ground position, at the fixed height, without changing its orientation.
    /// </summary>
    /// <param name="groundPosition">The ground position to centre on.</param>
    /// <remarks>
    /// <para>
    /// doc 30 § Camera: the camera "follows the authoritative player ground point". This slice
    /// follows it exactly, with no smoothing. The same section calls for "critically damped visual
    /// smoothing limited so the player never visibly leaves the position assumed by warnings and HUD
    /// bearings", and boundary clamping against the authored world - both of which are the rest of
    /// <c>PRE-002</c>, and neither of which is invented here. Following exactly is the honest
    /// degenerate case: it cannot violate the limit that smoothing has to respect.
    /// </para>
    /// <para>
    /// The rotation is never touched, which is what makes "non-rotating" structural rather than
    /// maintained.
    /// </para>
    /// </remarks>
    private void FollowWithCamera(PlanarVector groundPosition)
    {
        if (_camera is null)
        {
            return;
        }

        PresentationGroundMapping.ToPresentationWorld(
            groundPosition,
            CameraHeightMetres,
            out double worldX,
            out double worldY,
            out double worldZ);

        _camera.Position = new Vector3((float)worldX, (float)worldY, (float)worldZ);
    }
}
