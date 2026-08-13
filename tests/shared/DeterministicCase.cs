using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Runs a randomized test body with the seed and version identity logged before it
/// executes, and a one-command reproduction preserved when it fails.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Determinism and fixture
/// policy requires exactly two things of every randomized test, and this type is how
/// both are obtained rather than remembered:
/// </para>
/// <list type="number">
///   <item><description>
///     "Every randomized test logs its seed and version identity before execution."
///     The lines go to <see cref="TestContext.Progress"/>, which is unbuffered, so a
///     case that hangs or kills the process has still named its seed.
///   </description></item>
///   <item><description>
///     "Failures print a one-command/tool reproduction description and preserve the
///     minimized input where possible." For a seeded case the seed is the minimized
///     input, and it is written to a file as well as printed.
///   </description></item>
/// </list>
/// <para>
/// The <see cref="Random"/> handed to the body is test-harness randomness. The
/// authoritative gameplay stream contract - exact PCG32 with SplitMix64 child
/// derivation - is owned by <c>SIM-005</c> and is not this.
/// </para>
/// </remarks>
internal static class DeterministicCase
{
    /// <summary>The environment variable that overrides a case's declared seed.</summary>
    internal const string SeedOverrideVariable = "MECHAMINER_TEST_SEED";

    /// <summary>
    /// Logs identity, then runs <paramref name="body"/> with a seeded
    /// <see cref="Random"/>. On failure, preserves the reproduction and rethrows.
    /// </summary>
    internal static void Run(string caseName, int declaredSeed, Action<Random> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);
        ArgumentNullException.ThrowIfNull(body);

        int seed = ResolveSeed(declaredSeed);
        LogBeforeExecution(caseName, seed);

        try
        {
            body(new Random(seed));
        }
        catch (Exception exception)
        {
            PreserveReproduction(caseName, seed, minimizedInput: null, exception);
            throw;
        }
    }

    /// <summary>
    /// Returns the seed this run must use: the declared seed, unless
    /// <c>MECHAMINER_TEST_SEED</c> overrides it, which is how the printed
    /// reproduction command works.
    /// </summary>
    internal static int ResolveSeed(int declaredSeed)
    {
        string? overrideText = Environment.GetEnvironmentVariable(SeedOverrideVariable);
        if (!string.IsNullOrWhiteSpace(overrideText)
            && int.TryParse(overrideText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return declaredSeed;
    }

    /// <summary>
    /// Writes the seed and version identity to the unbuffered progress stream before
    /// any randomized work happens.
    /// </summary>
    internal static void LogBeforeExecution(string caseName, int seed)
    {
        TestContext.Progress.WriteLine(
            "SEED case=" + caseName + " seed=" + seed.ToString(CultureInfo.InvariantCulture));
        TestContext.Progress.WriteLine("VERSION-IDENTITY " + HarnessIdentity.Line);
        TestContext.Progress.WriteLine("REPRODUCE " + ReproductionCommand(seed));
    }

    /// <summary>
    /// The one command that reruns exactly this case at exactly this seed, through the
    /// standard command surface where possible.
    /// </summary>
    internal static string ReproductionCommand(int seed)
    {
        string project = TestArtifacts.Relative(TestArtifacts.TestProjectDirectory);
        string filter = TestContext.CurrentContext.Test.FullName;
        return SeedOverrideVariable + "=" + seed.ToString(CultureInfo.InvariantCulture)
            + " dotnet test " + project + " --filter \"FullyQualifiedName=" + filter + "\"";
    }

    /// <summary>
    /// Writes the reproduction description and the minimized input, and prints both.
    /// </summary>
    internal static void PreserveReproduction(
        string caseName,
        int seed,
        string? minimizedInput,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string directory = TestArtifacts.FailureDirectory(caseName, seed);
        string command = ReproductionCommand(seed);

        string description = string.Join(
            "\n",
            "case:             " + caseName,
            "seed:             " + seed.ToString(CultureInfo.InvariantCulture),
            "version identity: " + HarnessIdentity.Line,
            "test:             " + TestContext.CurrentContext.Test.FullName,
            "failure:          " + exception.GetType().FullName + ": " + exception.Message,
            string.Empty,
            "reproduce with exactly one command:",
            string.Empty,
            "    " + command,
            string.Empty,
            "doc 91 § Determinism and fixture policy: a randomized failure must print a",
            "one-command reproduction description and preserve the minimized input.",
            "doc 114 § Failure and retry policy: a random or property failure is preserved",
            "with its seed and shrunk into a fixed regression case. It is never retried",
            "unchanged and the tolerance is never loosened to make it pass.");

        File.WriteAllText(Path.Combine(directory, "reproduction.txt"), description + "\n");
        File.WriteAllText(
            Path.Combine(directory, "minimized-input.txt"),
            (minimizedInput ?? "seed=" + seed.ToString(CultureInfo.InvariantCulture)) + "\n");

        TestContext.Progress.WriteLine();
        TestContext.Progress.WriteLine("RANDOMIZED FAILURE " + caseName);
        TestContext.Progress.WriteLine(description);
        TestContext.Progress.WriteLine(
            "preserved: " + TestArtifacts.Relative(directory) + "/reproduction.txt");
        TestContext.Progress.WriteLine(
            "preserved: " + TestArtifacts.Relative(directory) + "/minimized-input.txt");
    }
}
