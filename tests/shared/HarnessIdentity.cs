using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MechaMiner.Tests.Support;

/// <summary>
/// The version identity every randomized test logs before it executes.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Determinism and fixture
/// policy: "Every randomized test logs its seed and version identity before
/// execution." A seed alone is not reproducible - the same seed against different
/// code is a different experiment - so the identity is logged with it.
/// </para>
/// <para>
/// This is the harness's own identity, not the build identity. Product version, build
/// number, source commit, Godot and .NET versions, content bundle hash, and the
/// schema/map/random/save versions are owned by <c>CMP-OBS-001</c> in
/// <c>MechaMiner.Diagnostics</c>, which <c>FND-004</c> landed.
/// </para>
/// <para>
/// The build identity is nevertheless not concatenated here, and the trailing token
/// still names <c>TASK-FND-004-001</c>, for two reasons that are worth stating so the
/// next reader does not treat it as an oversight:
/// </para>
/// <list type="number">
///   <item><description>
///     These files are linked into every test project, and only a project that
///     references <c>MechaMiner.Diagnostics</c> can read the identity.
///     <c>MechaMiner.Diagnostics.Tests</c> and <c>MechaMiner.Game.Tests</c> do; adding
///     the reference to the remaining three is a one-line change to project files owned
///     by the wave-1 streams, not by <c>FND-004</c>.
///   </description></item>
///   <item><description>
///     <c>MechaMiner.Simulation.Tests.Support.DeterministicCaseTests</c> asserts this
///     exact token. Changing the token and that assertion is one atomic change, and
///     that test file is another stream's owned scope, so <c>FND-004</c> cannot make it.
///     Changing only the token would fail a gate; changing only the test would be an
///     edit outside this package's scope.
///   </description></item>
/// </list>
/// <para>
/// The identity itself is landed and proved: <c>VER-FND-004-004</c> asserts it is one
/// value across the tool process, the Godot process, and diagnostics. What remains is
/// wiring it into the three remaining test assemblies' log lines, which is the
/// follow-up recorded on <c>VER-FND-003-002</c>.
/// </para>
/// </remarks>
internal static class HarnessIdentity
{
    /// <summary>
    /// The version of the harness contract itself. Increment it when the seed-to-value
    /// mapping of <see cref="DeterministicCase"/> or <see cref="PropertyCase"/>
    /// changes, because every recorded seed in every evidence bundle stops meaning
    /// the same thing at that moment.
    /// </summary>
    internal const int HarnessVersion = 1;

    /// <summary>The single line a randomized case logs before it runs.</summary>
    internal static string Line { get; } = Build();

    private static string Build()
    {
        Assembly assembly = typeof(HarnessIdentity).Assembly;
        AssemblyName name = assembly.GetName();
        string informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? name.Version?.ToString()
            ?? "unknown";

        return string.Concat(
            "harness=",
            HarnessVersion.ToString(CultureInfo.InvariantCulture),
            " assembly=",
            name.Name ?? "unknown",
            " version=",
            informational,
            " framework=",
            RuntimeInformation.FrameworkDescription,
            " runtime-identifier=",
            RuntimeInformation.RuntimeIdentifier,
            " build-identity=pending:TASK-FND-004-001");
    }
}
