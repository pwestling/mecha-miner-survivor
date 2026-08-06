using Godot;

namespace MechaMiner.Game;

/// <summary>
/// The single Godot entry point of the application. Attached to the root node of
/// <c>res://scenes/Boot.tscn</c>, which <c>project.godot</c> names as the main
/// scene.
/// </summary>
/// <remarks>
/// <para>
/// At FND-001 this node deliberately does nothing except announce that the engine
/// reached managed code. It exists so that a headless import and a headless launch
/// are verifiable now (<c>VER-FND-001-012</c>, <c>VER-FND-001-013</c>) and so that
/// the real composition root has a single, already-wired replacement point.
/// </para>
/// <para>
/// This type is the placeholder for <c>CMP-APP-001</c> (application coordinator).
/// The successors that replace its body, in dependency order, are:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>FND-004</c> - build identity is verified first, per
///     <c>docs/technical/115</c> § Initialization order step 1.
///   </description></item>
///   <item><description>
///     <c>FND-007</c> - bounded local logging and crash breadcrumbs replace
///     <see cref="GD.Print(string)"/> here with stable diagnostic codes
///     (step 2, <c>CTR-OBS-001</c>).
///   </description></item>
///   <item><description>
///     <c>DAT-006</c> / <c>CMP-CNT-002</c> - load and validate the canonical
///     content bundle (step 3, <c>CTR-CNT-001</c>).
///   </description></item>
///   <item><description>
///     <c>PST-003</c> - load, migrate, or recover settings and profile (step 4).
///   </description></item>
///   <item><description>
///     <c>PLT-001</c> - initialize the platform adapter, selecting the local
///     unavailable result on failure rather than failing the game (step 5,
///     <c>CTR-PLT-001</c>).
///   </description></item>
///   <item><description>
///     <c>UI-001</c>, <c>PRE-001</c>, <c>AUD-001</c> - construct application
///     routes, UI services, presentation settings, and audio (step 6).
///   </description></item>
/// </list>
/// <para>
/// It must never contain gameplay rules. Every authoritative rule lives in the
/// pure projects; this node only composes and observes
/// (<c>docs/technical/10</c> § Architectural style, <c>TR-RUN-001</c>).
/// Dependencies will be passed by explicit construction from here - no
/// dependency-injection container, service locator, or mutable global registry
/// (<c>docs/technical/114</c> § C# and domain defaults).
/// </para>
/// </remarks>
public partial class BootCompositionRoot : Node
{
    /// <summary>
    /// The stable line a headless launch asserts on. Its text is part of
    /// <c>VER-FND-001-013</c>; changing it changes that verification entry.
    /// </summary>
    internal const string StartupLine = "MechaMiner: boot composition root ready";

    /// <inheritdoc/>
    public override void _Ready()
    {
        GD.Print(StartupLine);
    }
}
