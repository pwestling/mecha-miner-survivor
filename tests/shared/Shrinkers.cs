using System;
using System.Collections.Generic;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Shrink strategies for <see cref="PropertyCase"/>.
/// </summary>
/// <remarks>
/// A shrinker returns candidate values that are strictly simpler than its input and
/// never returns the input itself, so the shrink loop in
/// <see cref="PropertyCase"/> terminates. "Simpler" means closer to zero, shorter,
/// or with fewer distinct elements: the point is that the preserved minimized input
/// is small enough for a human to read, which is what
/// <c>docs/technical/91-verification-strategy.md</c> § Determinism and fixture policy
/// asks for when it says a failure must "preserve the minimized input where
/// possible".
/// </remarks>
internal static class Shrinkers
{
    /// <summary>Shrinks an integer towards zero by halving, then by stepping.</summary>
    internal static IEnumerable<int> Int32(int value)
    {
        if (value == 0)
        {
            yield break;
        }

        yield return 0;

        int halved = value / 2;
        if (halved != 0 && halved != value)
        {
            yield return halved;
        }

        int stepped = value > 0 ? value - 1 : value + 1;
        if (stepped != value && stepped != halved)
        {
            yield return stepped;
        }
    }

    /// <summary>
    /// Shrinks an array by dropping elements first, then by shrinking the remaining
    /// elements one at a time.
    /// </summary>
    internal static IEnumerable<int[]> Int32Array(int[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            yield break;
        }

        // Whole-array removals, largest first: an empty or halved array is the most
        // valuable shrink when it still fails.
        yield return Array.Empty<int>();
        if (value.Length > 2)
        {
            yield return value[..(value.Length / 2)];
            yield return value[(value.Length / 2)..];
        }

        // One element removed, in index order for stable results.
        for (int index = 0; index < value.Length; index++)
        {
            int[] without = new int[value.Length - 1];
            Array.Copy(value, 0, without, 0, index);
            Array.Copy(value, index + 1, without, index, value.Length - index - 1);
            yield return without;
        }

        // Element values shrunk towards zero, in index order.
        for (int index = 0; index < value.Length; index++)
        {
            foreach (int smaller in Int32(value[index]))
            {
                int[] candidate = (int[])value.Clone();
                candidate[index] = smaller;
                yield return candidate;
            }
        }
    }
}
