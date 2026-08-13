using System;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The strict-codec rules applied to one class of document.
/// </summary>
/// <remarks>
/// <para>
/// Everything doc 40 § JSON codec and schema baseline forbids outright - comments,
/// trailing commas, duplicate properties, nonfinite numbers, JSON <c>null</c> - is
/// unconditional and has no switch here. Only two rules vary by document class, and
/// both vary because doc 40 itself states two different requirements:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Property-name style.</b> § JSON codec and schema baseline says "Property names
/// use <c>snake_case</c>", but § Source catalog format and key pattern requires the
/// localization catalog to be "a flat object of key to string" whose keys are
/// <c>&lt;category&gt;.&lt;stable_id&gt;.&lt;role&gt;</c> with the stable ID
/// "verbatim, in its own case". Those two rules cannot both hold for the same
/// object, and they do not have to: in a definition a property name is a
/// schema-declared field, whereas in a catalog it is data. The distinction is which
/// document is being read, so it is a policy of the read and not a guess made
/// per-object.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Object root.</b> A definition is always one object. A future payload class may
/// not be, so the requirement is stated rather than assumed.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class StrictJsonPolicy
{
    private StrictJsonPolicy(
        StrictJsonLimits limits,
        bool requireSnakeCasePropertyNames,
        bool requireObjectRoot)
    {
        Limits = limits;
        RequireSnakeCasePropertyNames = requireSnakeCasePropertyNames;
        RequireObjectRoot = requireObjectRoot;
    }

    /// <summary>
    /// The policy for a source definition: every property name is a schema-declared
    /// field in <c>snake_case</c> and the root is an object.
    /// </summary>
    public static StrictJsonPolicy Definitions { get; } = new(StrictJsonLimits.Default, true, true);

    /// <summary>
    /// The policy for a flat keyed catalog such as
    /// <c>content/localization/&lt;locale&gt;.json</c>, whose property names are
    /// data rather than fields.
    /// </summary>
    public static StrictJsonPolicy KeyedCatalog { get; } = new(StrictJsonLimits.Default, false, true);

    /// <summary>The size, depth, and count ceilings applied to the document.</summary>
    public StrictJsonLimits Limits { get; }

    /// <summary>Whether every property name must match <c>^[a-z][a-z0-9_]*$</c>.</summary>
    public bool RequireSnakeCasePropertyNames { get; }

    /// <summary>Whether the root value must be a JSON object.</summary>
    public bool RequireObjectRoot { get; }

    /// <summary>Returns this policy with different limits, for boundary testing.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is null.</exception>
    public StrictJsonPolicy WithLimits(StrictJsonLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return new StrictJsonPolicy(limits, RequireSnakeCasePropertyNames, RequireObjectRoot);
    }
}
