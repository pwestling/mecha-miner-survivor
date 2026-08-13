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
/// This is the harness's own identity, not the build identity. Product version,
/// build number, source commit, Godot and .NET versions, content bundle hash, and
/// the schema/map/random/save versions are owned by <c>FND-004</c>
/// (<c>TASK-FND-004-001</c>) and replace this text when that package lands. The
/// successor is recorded on <c>VER-FND-003-002</c>.
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
