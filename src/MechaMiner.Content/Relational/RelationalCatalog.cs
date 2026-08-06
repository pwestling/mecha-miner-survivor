using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Categories;

namespace MechaMiner.Content.Relational;

/// <summary>
/// Every definition loaded so far, indexed by stable ID, for the relational stage to
/// read operands out of.
/// </summary>
/// <remarks>
/// <para>
/// The catalog is built once and then read; a constraint never triggers a load. That is
/// what keeps the relational stage's verdicts independent of the order the source tree
/// was enumerated in: every constraint sees the same complete catalog, or reports that
/// it does not.
/// </para>
/// <para>
/// <b>What this type is not.</b> It is not the compiled bundle and it does not order,
/// hash, or serialize anything. Building the bundle is <c>DAT-006</c>'s work and
/// evaluating the constraints over a real content tree is <c>DAT-005</c>'s; what lands
/// here is the shape those two consume, plus the two constraints themselves so that
/// their operands and their stage are declared where the field tables that produce
/// them live.
/// </para>
/// </remarks>
public sealed class RelationalCatalog
{
    private readonly Dictionary<string, ContentDefinition> _byId;

    /// <summary>Builds a catalog from every loaded definition.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Two definitions claim one stable ID.</exception>
    public RelationalCatalog(IReadOnlyList<ContentDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        Definitions =
            new ReadOnlyCollection<ContentDefinition>(new List<ContentDefinition>(definitions));
        _byId = new Dictionary<string, ContentDefinition>(definitions.Count, StringComparer.Ordinal);
        foreach (ContentDefinition definition in definitions)
        {
            if (!_byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    "two definitions claim the stable ID '" + definition.Id
                        + "'; IDs are never reassigned, so this is a build fault rather than a "
                        + "content diagnostic");
            }
        }
    }

    /// <summary>An empty catalog, for a constraint asked to evaluate before any load.</summary>
    public static RelationalCatalog Empty { get; } = new(Array.Empty<ContentDefinition>());

    /// <summary>Every loaded definition, in load order.</summary>
    public IReadOnlyList<ContentDefinition> Definitions { get; }

    /// <summary>Looks up a definition by stable ID.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null.</exception>
    public bool TryGet(string id, out ContentDefinition? definition)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.TryGetValue(id, out definition);
    }

    /// <summary>Looks up a definition of a given type by stable ID.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null.</exception>
    public TDefinition? Find<TDefinition>(string id)
        where TDefinition : ContentDefinition
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.TryGetValue(id, out ContentDefinition? definition)
            ? definition as TDefinition
            : null;
    }

    /// <summary>Every loaded definition of one kind, in load order.</summary>
    public IReadOnlyList<TDefinition> OfKind<TDefinition>()
        where TDefinition : ContentDefinition
    {
        List<TDefinition> matches = new();
        foreach (ContentDefinition definition in Definitions)
        {
            if (definition is TDefinition typed)
            {
                matches.Add(typed);
            }
        }

        return new ReadOnlyCollection<TDefinition>(matches);
    }
}
