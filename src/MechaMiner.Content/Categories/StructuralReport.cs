using System;
using System.Collections.Generic;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Categories;

/// <summary>
/// Which pointers the structural stage has already reported as missing or mistyped, so
/// that a semantic check reading one of them can stay silent instead of asserting
/// something about a value nobody authored.
/// </summary>
/// <remarks>
/// <para>
/// <b>The failure this exists to stop.</b> A typed DTO cannot distinguish an absent
/// field from an authored one: both arrive as null, and every reader turns null into a
/// default before it computes anything. A semantic check downstream of that default is
/// then comparing against a value the document does not contain, and its message
/// describes a document that does not exist. A PowerUp whose <c>cap</c> was renamed gets
/// "the rank ladder holds exactly 0 elements; found 5" over a file with five plainly
/// visible rows; a schedule whose <c>share_percent</c> was renamed gets "sums to 100; the
/// parts sum to 0"; a utility whose <c>ore_only_exception</c> was renamed is told three
/// times that it is not the ore-only radar, by a check reading a boolean that defaulted
/// to false.
/// </para>
/// <para>
/// None of those is a second finding. Each is a restatement of the structural
/// diagnostic that has already been emitted at the operand's own pointer, dressed as an
/// independent one, and an author who fixes the field watches all of them disappear at
/// once. Counting them makes the gap between a catalog and its compiler look larger
/// than it is, and reading them sends an author to a rule that is not broken.
/// </para>
/// <para>
/// <b>Why the operands are named at the call site.</b> Nothing in the compiler can
/// infer which fields a check reads: by the time it runs, the defaulted value is
/// indistinguishable from an authored one. So the check is told, pointer by pointer, and
/// suppression is exact rather than a subtree sweep. That matters in both directions -
/// a missing <c>/ranks/0/price_hyper_gold</c> must not silence a genuine cardinality
/// fault on <c>/ranks</c>, and a missing <c>/cap</c> must silence the check that reads
/// it. A check whose operands are all present behaves exactly as it did before.
/// </para>
/// <para>
/// The report is a snapshot of the bag taken after the prelude, which is the moment the
/// structural stage has finished and no semantic diagnostic has been added yet. Reading
/// the bag rather than keeping a parallel list is what stops the two from disagreeing:
/// a pointer is suppressible exactly when a diagnostic for it was actually emitted.
/// </para>
/// </remarks>
public sealed class StructuralReport
{
    private static readonly HashSet<JsonPointer> None = new();

    private readonly HashSet<JsonPointer> _pointers;

    private StructuralReport(HashSet<JsonPointer> pointers)
    {
        _pointers = pointers;
    }

    /// <summary>A report in which nothing was reported.</summary>
    public static StructuralReport Empty { get; } = new(None);

    /// <summary>
    /// Snapshots the missing and mistyped fields <paramref name="bag"/> holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only <see cref="ContentDiagnosticCodes.RequiredFieldMissing"/> and
    /// <see cref="ContentDiagnosticCodes.FieldTypeMismatch"/> count. Those are the two
    /// ways the field table says a value is not there to be read. An unknown field is
    /// not one of them: it reports a property the schema does not declare, which no
    /// typed check reads and so cannot have defaulted.
    /// </para>
    /// <para>
    /// Of those two, only the missing half can be provoked as the pipeline stands: a
    /// kind mismatch makes the shape unsound and an unsound shape stops the read before
    /// the semantic stage, so a mistyped operand and a semantic diagnostic never appear
    /// in the same document. The mistyped half is kept because it is what the rule says,
    /// and <c>DependentSemanticSuppressionTests</c> pins the ordering that currently
    /// makes it unreachable rather than leaving the claim in prose.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="bag"/> is null.</exception>
    public static StructuralReport Of(DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        HashSet<JsonPointer> pointers = new();
        foreach (ContentDiagnostic diagnostic in bag.Diagnostics)
        {
            if (diagnostic.Code is ContentDiagnosticCodes.RequiredFieldMissing
                or ContentDiagnosticCodes.FieldTypeMismatch)
            {
                pointers.Add(diagnostic.Location);
            }
        }

        return pointers.Count == 0 ? Empty : new StructuralReport(pointers);
    }

    /// <summary>True when the structural stage has already reported this exact pointer.</summary>
    public bool Reported(JsonPointer operand)
    {
        return _pointers.Contains(operand);
    }

    /// <summary>True when any of these operands has already been reported.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is null.</exception>
    public bool ReportedAny(IReadOnlyList<JsonPointer> operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        for (int index = 0; index < operands.Count; index++)
        {
            if (_pointers.Contains(operands[index]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when either operand has already been reported.</summary>
    public bool ReportedEither(JsonPointer first, JsonPointer second)
    {
        return _pointers.Contains(first) || _pointers.Contains(second);
    }
}
