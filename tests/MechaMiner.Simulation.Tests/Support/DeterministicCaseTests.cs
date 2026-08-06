using System;
using System.IO;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Proves the determinism contract of the shared harness: a seeded case is
/// reproducible, its seed and version identity are logged before it executes, and a
/// failure preserves a one-command reproduction and the minimized input.
/// </summary>
/// <remarks>
/// Verification: <c>VER-FND-003-002</c>, <c>VER-FND-003-003</c>.
///
/// Doc 91 § Determinism and fixture policy: "Every randomized test logs its seed and
/// version identity before execution" and "Failures print a one-command/tool
/// reproduction description and preserve the minimized input where possible."
/// </remarks>
[TestFixture]
internal sealed class DeterministicCaseTests
{
    private const int SampleSeed = 20260806;

    [Test]
    public void ASeededCaseProducesTheSameSequenceEveryRun()
    {
        int[] first = new int[8];
        int[] second = new int[8];

        DeterministicCase.Run(
            "seeded-sequence-first",
            SampleSeed,
            random =>
            {
                for (int index = 0; index < first.Length; index++)
                {
                    first[index] = random.Next(0, 1_000_000);
                }
            });

        DeterministicCase.Run(
            "seeded-sequence-second",
            SampleSeed,
            random =>
            {
                for (int index = 0; index < second.Length; index++)
                {
                    second[index] = random.Next(0, 1_000_000);
                }
            });

        Assert.That(second, Is.EqualTo(first), "the same seed must produce the same sequence");
    }

    [Test]
    public void TheVersionIdentityNamesTheHarnessVersionAndItsPendingSuccessor()
    {
        Expect.Multiple(() =>
        {
            Assert.That(HarnessIdentity.Line, Does.Contain("harness=1"));
            Assert.That(HarnessIdentity.Line, Does.Contain("assembly=MechaMiner.Simulation.Tests"));
            Assert.That(HarnessIdentity.Line, Does.Contain(".NET"));
            Assert.That(
                HarnessIdentity.Line,
                Does.Contain("build-identity=pending:TASK-FND-004-001"),
                "the harness must say that the real build identity is not wired yet");
        });
    }

    [Test]
    public void TheReproductionCommandCarriesTheSeedAndTheExactTestFilter()
    {
        string command = DeterministicCase.ReproductionCommand(SampleSeed);

        Expect.Multiple(() =>
        {
            Assert.That(command, Does.StartWith("MECHAMINER_TEST_SEED=20260806"));
            Assert.That(command, Does.Contain("dotnet test tests/MechaMiner.Simulation.Tests"));
            Assert.That(
                command,
                Does.Contain(
                    "--filter \"FullyQualifiedName="
                    + "MechaMiner.Simulation.Tests.Support.DeterministicCaseTests"
                    + ".TheReproductionCommandCarriesTheSeedAndTheExactTestFilter\""));
        });
    }

    [Test]
    public void TheSeedOverrideVariableReplacesTheDeclaredSeed()
    {
        string? previous = Environment.GetEnvironmentVariable(DeterministicCase.SeedOverrideVariable);
        try
        {
            Environment.SetEnvironmentVariable(DeterministicCase.SeedOverrideVariable, "4242");
            Assert.That(DeterministicCase.ResolveSeed(SampleSeed), Is.EqualTo(4242));
        }
        finally
        {
            Environment.SetEnvironmentVariable(DeterministicCase.SeedOverrideVariable, previous);
        }
    }

    [Test]
    public void AFailingSeededCasePreservesItsReproductionAndMinimizedInput()
    {
        const string caseName = "harness-failure-contract";
        string failureDirectory = TestArtifacts.FailureDirectory(caseName, SampleSeed);
        string reproduction = Path.Combine(failureDirectory, "reproduction.txt");
        string minimized = Path.Combine(failureDirectory, "minimized-input.txt");
        File.Delete(reproduction);
        File.Delete(minimized);

        InvalidOperationException thrown = Expect.Throws<InvalidOperationException>(() => DeterministicCase.Run(
                caseName,
                SampleSeed,
                _ => throw new InvalidOperationException("deliberate harness failure")));

        Assert.That(thrown.Message, Is.EqualTo("deliberate harness failure"));

        Expect.Multiple(() =>
        {
            Assert.That(File.Exists(reproduction), "the reproduction description must be preserved");
            Assert.That(File.Exists(minimized), "the minimized input must be preserved");
        });

        string reproductionText = File.ReadAllText(reproduction);
        Expect.Multiple(() =>
        {
            Assert.That(reproductionText, Does.Contain("MECHAMINER_TEST_SEED=20260806"));
            Assert.That(reproductionText, Does.Contain("reproduce with exactly one command"));
            Assert.That(reproductionText, Does.Contain("version identity: harness=1"));
            Assert.That(File.ReadAllText(minimized), Does.Contain("seed=20260806"));
        });
    }
}
