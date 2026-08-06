using System;
using System.Globalization;

namespace MechaMiner.Tests.Support;

/// <summary>
/// A named floating-point tolerance.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance: "each
/// assertion names the tolerance", and "'Approximately equal' without a named
/// tolerance is not an acceptable test."
/// </para>
/// <para>
/// The only way to obtain a tolerance is <see cref="Named"/>, which rejects a blank
/// name and a magnitude that is not finite and positive. There is deliberately no
/// implicit conversion from <see cref="double"/> and no default value, so a bare
/// epsilon cannot reach <see cref="NumericAssert"/> at all.
/// </para>
/// <para>
/// The central catalogue of world-scale tolerances is not defined here. Doc 91
/// requires them to be "central absolute/relative tolerances based on world scale and
/// operation", which makes them the property of the owner of that scale:
/// <c>GEO-001</c> for planar geometry, <c>COM-003</c> for damage and throughput.
/// Until those land, each test names its own tolerance and states why it is that
/// size.
/// </para>
/// </remarks>
internal sealed class Tolerance
{
    private Tolerance(string name, double absolute, string rationale)
    {
        Name = name;
        Absolute = absolute;
        Rationale = rationale;
    }

    /// <summary>The tolerance's stable name, printed in every assertion message.</summary>
    internal string Name { get; }

    /// <summary>The absolute magnitude two values may differ by.</summary>
    internal double Absolute { get; }

    /// <summary>Why the magnitude is this size, printed with the name.</summary>
    internal string Rationale { get; }

    /// <summary>
    /// Declares a tolerance. The name and rationale are required: a tolerance whose
    /// size nobody can justify is the failure mode doc 91 forbids.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The name or rationale is blank, or the magnitude is not finite and positive.
    /// </exception>
    internal static Tolerance Named(string name, double absolute, string rationale)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "a tolerance must be named; doc 91 § Numeric tolerance: \"each assertion names the "
                + "tolerance\"",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException(
                "a tolerance must state why it is this size, so a later reader can tell a measured "
                + "bound from a number chosen to make a test pass",
                nameof(rationale));
        }

        if (!double.IsFinite(absolute) || absolute <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absolute),
                absolute,
                "a tolerance magnitude must be finite and greater than zero; use NumericAssert's exact "
                + "comparisons for values that must match exactly");
        }

        return new Tolerance(name, absolute, rationale);
    }

    /// <summary>Renders the tolerance for an assertion message.</summary>
    public override string ToString()
    {
        return Name + " (+/-" + Absolute.ToString("R", CultureInfo.InvariantCulture) + ", " + Rationale + ")";
    }
}
