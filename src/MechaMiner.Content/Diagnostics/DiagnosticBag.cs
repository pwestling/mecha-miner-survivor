using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// Collects the diagnostics produced while validating one source document.
/// </summary>
/// <remarks>
/// A validator collects rather than throws so that an author sees every problem in one
/// pass. Doc 40 § Agent content-change workflow expects an agent to "Change the
/// smallest source JSON set" and re-run validation; a validator that stopped at the
/// first fault would turn one edit into as many round trips as there are faults.
/// </remarks>
public sealed class DiagnosticBag
{
    private readonly List<ContentDiagnostic> _diagnostics = new();

    /// <summary>Every diagnostic added, in the order the validators produced them.</summary>
    public IReadOnlyList<ContentDiagnostic> Diagnostics =>
        new ReadOnlyCollection<ContentDiagnostic>(_diagnostics);

    /// <summary>True when no diagnostic of any severity has been added.</summary>
    public bool IsEmpty => _diagnostics.Count == 0;

    /// <summary>True when at least one diagnostic is an error.</summary>
    public bool HasErrors
    {
        get
        {
            foreach (ContentDiagnostic diagnostic in _diagnostics)
            {
                if (diagnostic.Severity == ContentDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Adds a diagnostic.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is null.</exception>
    public void Add(ContentDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _diagnostics.Add(diagnostic);
    }

    /// <summary>The distinct codes present, in first-seen order.</summary>
    public IReadOnlyList<string> Codes()
    {
        List<string> codes = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (ContentDiagnostic diagnostic in _diagnostics)
        {
            if (seen.Add(diagnostic.Code))
            {
                codes.Add(diagnostic.Code);
            }
        }

        return codes;
    }

    /// <summary>Renders every diagnostic, one per line, for an assertion message.</summary>
    public override string ToString()
    {
        if (_diagnostics.Count == 0)
        {
            return "(no diagnostics)";
        }

        return string.Join(Environment.NewLine, _diagnostics);
    }
}
