using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// The <c>source_refs</c> element grammar, and the parser for it.
/// </summary>
/// <remarks>
/// <para>
/// The grammar, in the form <c>content/schemas/README.md</c> records and doc 40
/// § <c>source_refs</c> element grammar is the normative home for:
/// </para>
/// <code>
/// element    := [ scope ": " ] reference
/// scope      := segment ( "." segment | index )*
/// segment    := [a-z][a-z0-9_]*
/// index      := "[]" | "[" digits "]" | "[" digits ".." digits "]"
/// reference  := ( docref | "DEC-" digits{3} | "TDR-" digits{3}
///                | "TR-" [A-Z]+ "-" digits{3} ) [ "#" anchor ]
/// docref     := ( "GDD-" | "TDD-" ) [A-Z0-9-]+
/// anchor     := [a-z0-9-]+
/// </code>
/// <para>
/// <b>Three extensions beyond doc 40 as currently written,</b> all three accepted by
/// the integration owner and all three present in the accepted catalog:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <c>TDD-&lt;DOC&gt;</c> alongside <c>GDD-&lt;DOC&gt;</c>.
/// <c>docs/technical/conventions.md</c> § Stable identifiers mints
/// <c>TDD-&lt;DOMAIN&gt;</c> as a first-class stable ID, so its absence from doc 40's
/// list is an omission rather than a prohibition. Forty elements in the accepted
/// catalog use it.
/// </description>
/// </item>
/// <item>
/// <description>
/// An optional scope prefix attributing one property of the definition to a different
/// source. Doc 40 bans a file path or a <c>path:line</c> pair because both "move
/// whenever a document is edited, so a reference built from them decays silently". A
/// scope is a selector into the definition's <em>own</em> JSON, validated against that
/// JSON by <see cref="SourceRefScope.ResolvesIn"/>, so it cannot decay silently: if
/// the field it names goes away, the build fails.
/// </description>
/// </item>
/// <item>
/// <description>
/// An optional <c>#anchor</c> on <c>DEC-###</c>, <c>TDR-###</c>, and
/// <c>TR-&lt;DOMAIN&gt;-###</c>, not only on the two document forms. Fifteen elements
/// in the accepted catalog use it - <c>DEC-120#decision</c>,
/// <c>DEC-120#consequences</c>, <c>DEC-121#decision</c> - and a heading slug in a
/// decision record is as stable as one in a gameplay document. What doc 40's ban
/// targets is a <em>line number</em>, and that is still rejected.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Why the pattern is a constant.</b> <see cref="ElementPattern"/> is mirrored
/// verbatim into <c>content/schemas/envelope.schema.json</c> so that the schema
/// rejects a malformed element on its own, without the typed parser in the path. A
/// string field carrying a mini-language drifts unless its shape is pinned
/// structurally. Both the schema pattern and the parser are composed from the same
/// sub-pattern constants below, and a test asserts the two agree over a table of
/// elements, so there is one grammar rather than two that resemble each other.
/// </para>
/// </remarks>
public static class SourceRefGrammar
{
    private const string Segment = "[a-z][a-z0-9_]*";
    private const string IndexForms = "\\[\\]|\\[[0-9]+\\]|\\[[0-9]+\\.\\.[0-9]+\\]";
    private const string ScopePattern = Segment + "(?:\\." + Segment + "|" + IndexForms + ")*";
    private const string DocumentPrefix = "(?:GDD|TDD)-[A-Z0-9-]+";
    private const string AnchorPattern = "[a-z0-9-]+";

    /// <remarks>
    /// The anchor is factored out of all four alternatives rather than attached to the
    /// document forms alone. A decision record and a requirement index are Markdown
    /// documents with stable headings, so <c>DEC-120#decision</c> is exactly as durable
    /// as <c>GDD-COMBAT#contact-damage</c>. Doc 40's ban targets <em>line numbers</em>,
    /// which move on every edit; a heading slug does not, and
    /// <see cref="LooksLikePathLine"/> still rejects the form that does.
    /// </remarks>
    private const string ReferencePattern =
        "(?:" + DocumentPrefix
        + "|DEC-[0-9]{3}"
        + "|TDR-[0-9]{3}"
        + "|TR-[A-Z]+-[0-9]{3})"
        + "(?:#" + AnchorPattern + ")?";

    /// <summary>
    /// The whole element grammar as one anchored regular expression, using only
    /// constructs ECMA-262 and therefore JSON Schema <c>pattern</c> supports.
    /// </summary>
    /// <remarks>
    /// Anchored deliberately: JSON Schema's <c>pattern</c> is a <em>search</em>, not a
    /// full match, so an unanchored pattern would accept
    /// <c>docs/foo.md:12 GDD-MINING</c> because <c>GDD-MINING</c> occurs somewhere in
    /// it.
    /// </remarks>
    public const string ElementPattern = "^(?:" + ScopePattern + ": )?" + ReferencePattern + "$";

    private static readonly Regex CapturingElement = AnchoredPattern.Compile(
        "^(?:(?<scope>" + ScopePattern + "): )?(?<reference>" + ReferencePattern + ")$");

    /// <summary>
    /// A file name followed by a line number, which is the <c>path:line</c> form doc 40
    /// rejects even when it contains no directory separator.
    /// </summary>
    private static readonly Regex FileAndLine = AnchoredPattern.Compile(
        "^[^\\s:]+\\.[A-Za-z0-9]{1,6}:[0-9]+$");

    private static readonly Regex ScopeStep = AnchoredPattern.Compile(
        "\\G(?:\\.(?<member>" + Segment + ")"
        + "|(?<any>\\[\\])"
        + "|\\[(?<index>[0-9]+)\\]"
        + "|\\[(?<low>[0-9]+)\\.\\.(?<high>[0-9]+)\\])");

    private static readonly Regex LeadingSegment = AnchoredPattern.Compile("^" + Segment);

    /// <summary>Parses one <c>source_refs</c> element.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    public static SourceRefParseOutcome Parse(string element, out SourceRef? parsed)
    {
        ArgumentNullException.ThrowIfNull(element);

        parsed = null;

        Match match = CapturingElement.Match(element);
        if (!match.Success)
        {
            return LooksLikePathLine(element)
                ? SourceRefParseOutcome.PathLine
                : SourceRefParseOutcome.Malformed;
        }

        SourceRefScope? scope = null;
        Group scopeGroup = match.Groups["scope"];
        if (scopeGroup.Success && !TryParseScope(scopeGroup.Value, out scope))
        {
            // The scope matched the shape but not its semantics: a descending or
            // implausibly wide range. It is still a scope, so it is malformed rather
            // than a path.
            return SourceRefParseOutcome.Malformed;
        }

        string reference = match.Groups["reference"].Value;
        int hash = reference.IndexOf('#', StringComparison.Ordinal);
        string documentId = hash < 0 ? reference : reference[..hash];
        string? anchor = hash < 0 ? null : reference[(hash + 1)..];

        parsed = new SourceRef(element, scope, KindOf(documentId), reference, documentId, anchor);
        return SourceRefParseOutcome.Parsed;
    }

    /// <summary>
    /// True when <paramref name="element"/> is a file path or a <c>path:line</c> pair.
    /// </summary>
    /// <remarks>
    /// A directory separator is decisive on its own: no form the grammar admits
    /// contains one, and every path does. The second test catches a bare file name with
    /// a line number, which has no separator but is the same mistake.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    public static bool LooksLikePathLine(string element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element.Contains('/', StringComparison.Ordinal)
            || element.Contains('\\', StringComparison.Ordinal))
        {
            return true;
        }

        return FileAndLine.IsMatch(element);
    }

    /// <summary>True when <paramref name="element"/> matches <see cref="ElementPattern"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    public static bool MatchesElementPattern(string element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return CapturingElement.IsMatch(element);
    }

    private static SourceRefKind KindOf(string documentId)
    {
        if (documentId.StartsWith("GDD-", StringComparison.Ordinal))
        {
            return SourceRefKind.GameplayDocument;
        }

        if (documentId.StartsWith("TDD-", StringComparison.Ordinal))
        {
            return SourceRefKind.TechnicalDocument;
        }

        if (documentId.StartsWith("DEC-", StringComparison.Ordinal))
        {
            return SourceRefKind.GameplayDecision;
        }

        if (documentId.StartsWith("TDR-", StringComparison.Ordinal))
        {
            return SourceRefKind.TechnicalDecision;
        }

        if (documentId.StartsWith("TR-", StringComparison.Ordinal))
        {
            return SourceRefKind.TechnicalRequirement;
        }

        throw new InvalidOperationException(
            "reference '" + documentId + "' matched the element grammar but has no kind; the "
                + "grammar and KindOf must list the same prefixes");
    }

    private static bool TryParseScope(string text, out SourceRefScope? scope)
    {
        scope = null;

        Match leading = LeadingSegment.Match(text);
        if (!leading.Success)
        {
            return false;
        }

        List<SourceRefScopeStep> steps = new() { SourceRefScopeStep.Member(leading.Value) };

        int position = leading.Length;
        while (position < text.Length)
        {
            Match step = ScopeStep.Match(text, position);
            if (!step.Success)
            {
                return false;
            }

            if (step.Groups["member"].Success)
            {
                steps.Add(SourceRefScopeStep.Member(step.Groups["member"].Value));
            }
            else if (step.Groups["any"].Success)
            {
                steps.Add(SourceRefScopeStep.AnyIndex());
            }
            else if (step.Groups["index"].Success)
            {
                if (!TryParseIndex(step.Groups["index"].Value, out int index))
                {
                    return false;
                }

                steps.Add(SourceRefScopeStep.Index(index));
            }
            else
            {
                if (!TryParseIndex(step.Groups["low"].Value, out int low)
                    || !TryParseIndex(step.Groups["high"].Value, out int high)
                    || high < low
                    || (high - low) + 1 > SourceRefScope.MaximumRangeSpan)
                {
                    return false;
                }

                steps.Add(SourceRefScopeStep.Range(low, high));
            }

            position += step.Length;
        }

        scope = new SourceRefScope(text, steps);
        return true;
    }

    private static bool TryParseIndex(string digits, out int value)
    {
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
