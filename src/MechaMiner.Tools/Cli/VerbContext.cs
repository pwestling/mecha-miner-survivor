using System;
using System.Collections.Generic;
using System.IO;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// Everything one verb invocation needs, passed explicitly by the dispatcher.
/// </summary>
/// <remarks>
/// Dependencies are passed through the constructor rather than resolved from a
/// container, service locator, or mutable global registry, per
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § C# and domain
/// defaults.
/// </remarks>
internal sealed class VerbContext
{
    internal VerbContext(
        RepositoryLayout layout,
        VerbDescriptor descriptor,
        ParsedArguments arguments,
        InvocationRecord record,
        string artifactDirectory,
        TextWriter console)
    {
        Layout = layout;
        Descriptor = descriptor;
        Arguments = arguments;
        Record = record;
        ArtifactDirectory = artifactDirectory;
        Console = console;
        Runner = new CommandRunner(layout, record.Steps, Path.Combine(artifactDirectory, "logs"), console);
    }

    /// <summary>The accepted repository paths.</summary>
    internal RepositoryLayout Layout { get; }

    /// <summary>The verb being executed.</summary>
    internal VerbDescriptor Descriptor { get; }

    /// <summary>The validated argument values.</summary>
    internal ParsedArguments Arguments { get; }

    /// <summary>The structured result document being assembled.</summary>
    internal InvocationRecord Record { get; }

    /// <summary>The absolute directory this invocation writes its evidence into.</summary>
    internal string ArtifactDirectory { get; }

    /// <summary>Where progress lines go.</summary>
    internal TextWriter Console { get; }

    /// <summary>The external-process runner, which records every command as a step.</summary>
    internal CommandRunner Runner { get; }

    /// <summary>Resolves the workflow configuration this invocation targets.</summary>
    internal WorkflowConfiguration Configuration()
    {
        string name = Arguments.Value("configuration");
        WorkflowConfiguration configuration = name.Length == 0
            ? WorkflowConfiguration.Debug
            : WorkflowConfiguration.FromWorkflowName(name);
        Record.Configuration = configuration.WorkflowName;
        Record.MsbuildConfiguration = configuration.MsbuildName;
        return configuration;
    }

    /// <summary>Writes a text artifact into this invocation's directory and returns its repository-relative path.</summary>
    internal string WriteArtifact(string fileName, string content)
    {
        Directory.CreateDirectory(ArtifactDirectory);
        string absolute = Path.Combine(ArtifactDirectory, fileName);
        File.WriteAllText(absolute, content);
        return Layout.Relative(absolute);
    }

    /// <summary>Prints a section heading so console output stays scannable.</summary>
    internal void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine("=== " + title);
    }

    /// <summary>Runs a repository script beneath <c>build/</c> through the platform shell.</summary>
    internal CommandResult RunRepositoryScript(
        string stepName,
        string scriptRelativePath,
        IReadOnlyList<string>? scriptArguments = null,
        TimeSpan? timeout = null)
    {
        List<string> arguments = new() { Layout.Absolute(scriptRelativePath) };
        if (scriptArguments is not null)
        {
            arguments.AddRange(scriptArguments);
        }

        return Runner.Run(stepName, "bash", arguments, Layout.Root, timeout);
    }
}
