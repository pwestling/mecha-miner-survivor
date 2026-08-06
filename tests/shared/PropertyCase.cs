using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Runs a property over generated inputs, shrinks the first failing input, and
/// preserves the minimized one.
/// </summary>
/// <remarks>
/// <para>
/// The shape is deliberately explicit rather than a generic property framework:
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § Deterministic
/// local-choice defaults prefers "an existing project mechanism without adding a
/// dependency or architectural concept", and doc 100 § C# project standards allows
/// "only its required adapter/runner packages" alongside NUnit.
/// </para>
/// <para>
/// Every run logs its seed and version identity before generating anything, and a
/// failure writes both the reproduction description and the shrunk input, satisfying
/// doc 91 § Determinism and fixture policy.
/// </para>
/// </remarks>
internal static class PropertyCase
{
    /// <summary>
    /// The bound on shrink steps. Doc 91 § Flake policy requires bounded work; an
    /// unbounded shrink loop on a pathological shrinker would hang the suite instead
    /// of reporting a failure.
    /// </summary>
    internal const int MaximumShrinkSteps = 500;

    /// <summary>
    /// Generates <paramref name="caseCount"/> inputs and asserts
    /// <paramref name="property"/> on each.
    /// </summary>
    /// <param name="caseName">Stable name used in logs and artifact paths.</param>
    /// <param name="declaredSeed">The seed, overridable by <c>MECHAMINER_TEST_SEED</c>.</param>
    /// <param name="caseCount">How many inputs to generate. Bounded by the caller.</param>
    /// <param name="generate">Produces one input from the seeded random source.</param>
    /// <param name="shrink">Produces strictly simpler candidates for a failing input.</param>
    /// <param name="render">Renders an input as canonical reviewable text.</param>
    /// <param name="property">Asserts the property. Throws to signal failure.</param>
    internal static void ForAll<TValue>(
        string caseName,
        int declaredSeed,
        int caseCount,
        Func<Random, TValue> generate,
        Func<TValue, IEnumerable<TValue>> shrink,
        Func<TValue, string> render,
        Action<TValue> property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);
        ArgumentOutOfRangeException.ThrowIfLessThan(caseCount, 1);
        ArgumentNullException.ThrowIfNull(generate);
        ArgumentNullException.ThrowIfNull(shrink);
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(property);

        int seed = DeterministicCase.ResolveSeed(declaredSeed);
        DeterministicCase.LogBeforeExecution(caseName, seed);
        TestContext.Progress.WriteLine(
            "CASES " + caseCount.ToString(CultureInfo.InvariantCulture)
            + " generated inputs, shrink bound " + MaximumShrinkSteps.ToString(CultureInfo.InvariantCulture));

        Random random = new(seed);
        for (int index = 0; index < caseCount; index++)
        {
            TValue value = generate(random);
            Exception? failure = Attempt(property, value);
            if (failure is null)
            {
                continue;
            }

            (TValue minimized, Exception minimizedFailure, int steps) = Shrink(value, failure, shrink, property);

            string minimizedText = string.Join(
                "\n",
                "generated case index: " + index.ToString(CultureInfo.InvariantCulture),
                "shrink steps:         " + steps.ToString(CultureInfo.InvariantCulture),
                "original input:       " + render(value),
                "minimized input:      " + render(minimized),
                "minimized failure:    " + minimizedFailure.Message);

            DeterministicCase.PreserveReproduction(caseName, seed, minimizedText, minimizedFailure);

            Assert.Fail(
                caseName + ": property failed on generated case "
                + index.ToString(CultureInfo.InvariantCulture) + " of "
                + caseCount.ToString(CultureInfo.InvariantCulture)
                + " at seed " + seed.ToString(CultureInfo.InvariantCulture)
                + ". Minimized input after " + steps.ToString(CultureInfo.InvariantCulture)
                + " shrink step(s): " + render(minimized)
                + ". Failure: " + minimizedFailure.Message
                + ". Reproduce: " + DeterministicCase.ReproductionCommand(seed));
        }
    }

    private static (TValue Value, Exception Failure, int Steps) Shrink<TValue>(
        TValue failing,
        Exception failure,
        Func<TValue, IEnumerable<TValue>> shrink,
        Action<TValue> property)
    {
        TValue current = failing;
        Exception currentFailure = failure;
        int steps = 0;

        bool improved = true;
        while (improved && steps < MaximumShrinkSteps)
        {
            improved = false;
            foreach (TValue candidate in shrink(current))
            {
                steps++;
                if (steps >= MaximumShrinkSteps)
                {
                    break;
                }

                Exception? candidateFailure = Attempt(property, candidate);
                if (candidateFailure is not null)
                {
                    current = candidate;
                    currentFailure = candidateFailure;
                    improved = true;
                    break;
                }
            }
        }

        return (current, currentFailure, steps);
    }

#pragma warning disable CA1031 // A property harness must classify any thrown failure, not only assertion failures.
    private static Exception? Attempt<TValue>(Action<TValue> property, TValue value)
    {
        try
        {
            property(value);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
#pragma warning restore CA1031
}
