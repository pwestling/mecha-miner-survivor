using MechaMiner.Diagnostics.Logging;

namespace MechaMiner.Diagnostics.Tests.Logging;

/// <summary>A sink that always fails, for the logging failure fixture.</summary>
/// <remarks>
/// Deliberately in the test project rather than beside the production sinks. A sink whose
/// only behaviour is to fail is test scaffolding, and the repository policy keeps
/// deliberately broken fixtures out of production assemblies.
/// </remarks>
internal sealed class FailingLogSink : ILogSink
{
    /// <summary>How many writes were attempted.</summary>
    internal int Attempts { get; private set; }

    /// <inheritdoc/>
    public bool TryWriteLine(string line)
    {
        Attempts++;
        return false;
    }
}
