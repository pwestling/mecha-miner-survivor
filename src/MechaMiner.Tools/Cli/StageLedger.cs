using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The ordered stage list a multi-stage verb declares up front, so that a failure in one
/// stage can name the stages that were never entered.
/// </summary>
/// <remarks>
/// <para>
/// "Red at stage 1" is routinely read as "stages 2 and 3 passed", and it does not mean
/// that: those stages did not run, so they are unproved, and unproved is not the same
/// claim as satisfied. The distinction is the same one
/// <c>docs/technical/delivery-waves.md</c> § Decision 11 makes about candidate sets - a
/// step that did not happen has established nothing - and the same one
/// <c>build/verify-wrapper-parity.sh</c> makes about a skipped check, where "a skipped
/// required check that is only visible in the middle of a long log is indistinguishable
/// from a passed one at a glance".
/// </para>
/// <para>
/// A staged verb that returns early leaks exactly that ambiguity: the log simply stops,
/// and the reader supplies the rest. This puts the unrun stages in the final-result line,
/// which is the line a reader already looks at, instead of leaving them to be inferred
/// from where the output ends.
/// </para>
/// <para>
/// Only the prose is amended. <see cref="VerbOutcome.ExitClass"/> and
/// <see cref="VerbOutcome.DiagnosticCode"/> are carried through untouched, so a gate or a
/// caller matching on either sees no change.
/// </para>
/// </remarks>
internal sealed class StageLedger
{
    private readonly VerbContext _context;
    private readonly IReadOnlyList<string> _stages;
    private int _current = -1;

    internal StageLedger(VerbContext context, params string[] stages)
    {
        _context = context;
        _stages = stages;
    }

    /// <summary>The number of stages this verb declared.</summary>
    internal int Count => _stages.Count;

    /// <summary>Announces a stage and records it as the one now running.</summary>
    internal void Enter(int index)
    {
        Enter(index, _stages[index]);
    }

    /// <summary>
    /// Announces a stage under a heading that differs from the declared name, for a stage
    /// whose heading carries run-specific detail (a configuration name, an idempotent
    /// no-op). The declared name is what an unrun-stage report uses.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The index is not one of the declared stages.
    /// </exception>
    internal void Enter(int index, string heading)
    {
        // Throws rather than clamping, and this is the interesting line in the file.
        //
        // A verb that grows a stage and does not extend its declared list is the exact
        // failure this class exists to prevent, turned on itself: the headings would read
        // "stage 4 of 3", and - worse - a failure in stage 1 would report only the stages
        // the list happens to know about, silently under-reporting what did not run. That
        // is the same "unproved read as satisfied" conflation, reintroduced by the fix for
        // it. It happened during this change: `build` gained two stages in a merge while
        // the ledger still declared three, and nothing complained.
        //
        // An out-of-range stage is therefore a programming error and is loud. `build`
        // reaches this on every invocation, so the first run after a bad edit fails.
        if (index < 0 || index >= _stages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "this verb entered stage " + (index + 1).ToString(CultureInfo.InvariantCulture)
                + " but declared only " + _stages.Count.ToString(CultureInfo.InvariantCulture)
                + " stage(s). Add the new stage to the StageLedger constructor: an unrun-stage "
                + "report can only name stages the ledger was told about.");
        }

        _current = index;
        _context.Section(
            "stage " + (index + 1).ToString(CultureInfo.InvariantCulture) + " of "
            + _stages.Count.ToString(CultureInfo.InvariantCulture) + ": " + heading);
    }

    /// <summary>
    /// Records that the verb is abandoning the run inside the current stage, prints every
    /// stage that will therefore not run, and returns the outcome with those stages named
    /// in its final-result line. Returns the outcome unchanged when the failing stage was
    /// the last one, because then there is nothing left that did not run.
    /// </summary>
    internal VerbOutcome Abandon(VerbOutcome outcome)
    {
        List<string> notRun = new();
        for (int index = _current + 1; index < _stages.Count; index++)
        {
            notRun.Add(
                "stage " + (index + 1).ToString(CultureInfo.InvariantCulture) + " (" + _stages[index] + ")");
        }

        if (notRun.Count == 0)
        {
            return outcome;
        }

        _context.Console.WriteLine(
            "      DID NOT RUN, because stage " + (_current + 1).ToString(CultureInfo.InvariantCulture)
            + " failed: " + string.Join("; ", notRun));

        return outcome.WithFinalResult(
            outcome.FinalResult + ". Stage " + (_current + 1).ToString(CultureInfo.InvariantCulture)
            + " of " + _stages.Count.ToString(CultureInfo.InvariantCulture)
            + " failed, so these stages DID NOT RUN and are unproved rather than passing: "
            + string.Join("; ", notRun));
    }
}
