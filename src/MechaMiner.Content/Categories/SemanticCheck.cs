using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Ids;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The value-level checks category readers share: tokens, integrality, bounds,
/// contiguity, reference grammar, and within-definition uniqueness.
/// </summary>
/// <remarks>
/// <para>
/// These are doc 40 § Semantic's "rules within a definition". Each takes the pointer
/// it is checking so that the diagnostic names the exact field, and each states the
/// constraint rather than the failure, because "cadence_seconds is a positive number
/// of seconds" tells an author what to write and "invalid cadence" does not.
/// </para>
/// <para>
/// Every method returns the checked value or a fallback and reports at most one
/// diagnostic, so a reader can run all of them and an author sees every fault in one
/// pass rather than one per build.
/// </para>
/// </remarks>
public static class SemanticCheck
{
    /// <summary>Requires a token to be in a closed vocabulary.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool Token(
        string? value,
        ClosedVocabulary vocabulary,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null)
        {
            return false;
        }

        if (vocabulary.Accepts(value))
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.TokenOutsideVocabulary,
            context.SourcePath,
            pointer,
            contentId,
            vocabulary.Describe()));
        return false;
    }

    /// <summary>Requires a value to be a well-formed <c>lower-kebab-case</c> token.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool BehaviorToken(
        string? value,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null)
        {
            return false;
        }

        if (TokenGrammar.IsWellFormed(value))
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.BehaviorTokenMalformed,
            context.SourcePath,
            pointer,
            contentId,
            TokenGrammar.Describe()));
        return false;
    }

    /// <summary>Requires a JSON number to be integral, and returns it.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static long Integer(
        double? value,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null)
        {
            return 0;
        }

        double raw = value.Value;
        if (Math.Floor(raw) == raw && raw is >= long.MinValue and <= long.MaxValue)
        {
            return (long)raw;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ValueOutOfRange,
            context.SourcePath,
            pointer,
            contentId,
            subject + " is an integer in source; doc 40 § Unit and numeric policy makes "
                + "currency and rank values integral so that a formula over them cannot "
                + "accumulate a rounding difference the reports would then have to explain"));
        return 0;
    }

    /// <summary>Requires a number to be at least <paramref name="minimum"/>.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool AtLeast(
        double? value,
        double minimum,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null || value.Value >= minimum)
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ValueOutOfRange,
            context.SourcePath,
            pointer,
            contentId,
            subject + " is at least " + Format(minimum) + "; found " + Format(value.Value)));
        return false;
    }

    /// <summary>Requires a number to be strictly greater than <paramref name="floorValue"/>.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool GreaterThan(
        double? value,
        double floorValue,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null || value.Value > floorValue)
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ValueOutOfRange,
            context.SourcePath,
            pointer,
            contentId,
            subject + " is strictly greater than " + Format(floorValue) + "; found "
                + Format(value.Value)));
        return false;
    }

    /// <summary>Requires a number to lie within an inclusive band.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool Within(
        double? value,
        double minimum,
        double maximum,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null || (value.Value >= minimum && value.Value <= maximum))
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ValueOutOfRange,
            context.SourcePath,
            pointer,
            contentId,
            subject + " is between " + Format(minimum) + " and " + Format(maximum)
                + " inclusive; found " + Format(value.Value)));
        return false;
    }

    /// <summary>Requires a range's bounds to be ordered, and any target to lie between them.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool FeasibleRange(
        double? minimum,
        double? maximum,
        double? target,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (minimum is not null && maximum is not null && minimum.Value > maximum.Value)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.RangeInfeasible,
                context.SourcePath,
                pointer,
                contentId,
                subject + "'s lower bound is no greater than its upper bound; found "
                    + Format(minimum.Value) + " and " + Format(maximum.Value)
                    + ". Feasibility is checked before any sampling, so an impossible "
                    + "contract fails at compile time rather than after a generator has "
                    + "exhausted its retry budget"));
            return false;
        }

        if (target is null || minimum is null || maximum is null)
        {
            return true;
        }

        if (target.Value >= minimum.Value && target.Value <= maximum.Value)
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.RangeInfeasible,
            context.SourcePath,
            pointer,
            contentId,
            subject + "'s target lies within its bounds; found target " + Format(target.Value)
                + " outside " + Format(minimum.Value) + " to " + Format(maximum.Value)));
        return false;
    }

    /// <summary>Requires an array to hold exactly <paramref name="expected"/> elements.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool ExactCount(
        int actual,
        int expected,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (actual == expected)
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ArrayCardinalityWrong,
            context.SourcePath,
            pointer,
            contentId,
            subject + " holds exactly " + expected.ToString(CultureInfo.InvariantCulture)
                + " elements; found " + actual.ToString(CultureInfo.InvariantCulture)));
        return false;
    }

    /// <summary>
    /// Requires the ordinals of a sequence to be the contiguous integers from
    /// <paramref name="first"/>, in array order.
    /// </summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool Contiguous(
        IReadOnlyList<long> ordinals,
        long first,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(ordinals);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        for (int index = 0; index < ordinals.Count; index++)
        {
            long expected = first + index;
            if (ordinals[index] == expected)
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.SequenceNotContiguous,
                context.SourcePath,
                pointer.AppendIndex(index),
                contentId,
                subject + " runs as the contiguous integers from "
                    + first.ToString(CultureInfo.InvariantCulture)
                    + " in array order; element " + index.ToString(CultureInfo.InvariantCulture)
                    + " is " + ordinals[index].ToString(CultureInfo.InvariantCulture)
                    + " where " + expected.ToString(CultureInfo.InvariantCulture)
                    + " was required. The comparison is against the array index, so a "
                    + "correct element count with a repeated or reordered ordinal still fails"));
            return false;
        }

        return true;
    }

    /// <summary>Requires a cross-reference to match a category's ID grammar.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool ReferenceGrammar(
        string? value,
        ContentCategory category,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (value is null)
        {
            return false;
        }

        ContentCategoryDescriptor descriptor = ContentCategories.Describe(category);
        if (descriptor.Accepts(value))
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ReferenceGrammarMismatch,
            context.SourcePath,
            pointer,
            contentId,
            "a cross-reference holds a stable ID, never a display name: "
                + descriptor.DescribeAcceptedGrammar()
                + ". Whether a definition with that ID exists is a relational check and is "
                + "not asserted here",
            new[] { value }));
        return false;
    }

    /// <summary>Requires the values of a sequence to be distinct within one definition.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool Distinct(
        IReadOnlyList<string> values,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        HashSet<string> seen = new(values.Count, StringComparer.Ordinal);
        bool distinct = true;
        for (int index = 0; index < values.Count; index++)
        {
            if (seen.Add(values[index]))
            {
                continue;
            }

            distinct = false;
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.DuplicateValueInDefinition,
                context.SourcePath,
                pointer.AppendIndex(index),
                contentId,
                subject + " are distinct within one definition; '" + values[index]
                    + "' appears more than once"));
        }

        return distinct;
    }

    /// <summary>Requires a recomputed sum to equal an accepted total.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool SumEquals(
        long actual,
        long expected,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string subject)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        if (actual == expected)
        {
            return true;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.SumMismatch,
            context.SourcePath,
            pointer,
            contentId,
            subject + " sums to " + expected.ToString(CultureInfo.InvariantCulture)
                + "; the parts sum to " + actual.ToString(CultureInfo.InvariantCulture)
                + ". The total is recomputed from the parts rather than compared against an "
                + "authored copy of itself, which would prove nothing"));
        return false;
    }

    /// <summary>Reports a field a discriminator makes required but that is absent.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static void RequiredBy(
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string because)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ConditionalFieldMissing,
            context.SourcePath,
            pointer,
            contentId,
            because));
    }

    /// <summary>Reports a field a discriminator makes illegal but that is present.</summary>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static void ForbiddenBy(
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        string because)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ConditionalFieldForbidden,
            context.SourcePath,
            pointer,
            contentId,
            because));
    }

    private static string Format(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
