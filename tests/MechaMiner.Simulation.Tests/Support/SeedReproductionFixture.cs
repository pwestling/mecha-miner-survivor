using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// A deliberately failing randomized case, used to prove that a real failure prints a
/// one-command reproduction and preserves the minimized input.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-FND-003-003</c>. Driven by
/// <c>build/verify-test-harness.sh</c>, which runs this fixture on purpose, asserts
/// that the run fails, and asserts the reproduction and minimized-input artifacts.
/// </para>
/// <para>
/// It is <c>Explicit</c>, so <c>./build.sh test-fast</c> never executes it and it can
/// never make an ordinary run red. That is not a quarantine: doc 91 § Flake policy
/// governs a flaky <em>required</em> test, and this is a fixture whose expected
/// outcome is failure, exercised by its own gate. <c>PropertyCaseTests</c> covers the
/// same shrinking contract from the inside in the always-on suite, so nothing here is
/// the sole evidence for anything.
/// </para>
/// <para>
/// Unmistakably diagnostic and excluded from Release by construction: it is a test
/// project, and doc 100 § Godot import and export filters test fixtures out of
/// release packaging.
/// </para>
/// </remarks>
[TestFixture]
[Explicit("Deliberately fails. Driven by build/verify-test-harness.sh to prove the failure contract.")]
[Category(HarnessFailureDemonstration)]
internal sealed class SeedReproductionFixture
{
    /// <summary>The category name <c>build/verify-test-harness.sh</c> filters on.</summary>
    internal const string HarnessFailureDemonstration = "HarnessFailureDemonstration";

    /// <summary>
    /// The seed this fixture declares. The gate script asserts that this exact number
    /// appears in the printed reproduction command and in the preserved artifacts.
    /// </summary>
    internal const int DeclaredSeed = 77001;

    [Test]
    public void APropertyThatCannotHoldFailsWithAReproducibleMinimizedInput()
    {
        PropertyCase.ForAll(
            "deliberate-harness-failure",
            DeclaredSeed,
            caseCount: 32,
            generate: random =>
            {
                int[] values = new int[random.Next(6, 20)];
                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = random.Next(0, 500);
                }

                return values;
            },
            shrink: Shrinkers.Int32Array,
            render: values => "[" + string.Join(",", values) + "]",
            property: values => Assert.That(
                values,
                Is.Empty,
                "deliberate failure: this property cannot hold for a generated non-empty array"));
    }
}
