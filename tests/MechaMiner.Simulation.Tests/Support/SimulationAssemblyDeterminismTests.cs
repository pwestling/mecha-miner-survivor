using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Reads the compiled <c>MechaMiner.Simulation</c> assembly's metadata and fails if it references a
/// wall clock, a nondeterministic source, or the engine.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-014</c>.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Scope and invariants: "The simulation is a pure C#
/// library with no dependency on Godot, files, Steam, rendering, audio, wall time, or mutable global
/// services", and "simulation time advances only by complete fixed ticks".
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains owns the separation this defends.
/// </para>
/// <para>
/// <b>Why this exists alongside the architecture script, and why neither replaces the other.</b>
/// <c>build/verify-architecture.sh</c> checks the <em>reference</em> axis: which assemblies a project
/// depends on. That is the right instrument for Godot, because Godot arrives as a package and a
/// forbidden dependency is exactly what there is to find - and that script covers it thoroughly,
/// down to resolved compile-time reference paths, raw <c>Reference</c> items with a <c>HintPath</c>,
/// injected <c>GlobalPackageReference</c>, the retained lock file, and a source scan that matches the
/// <c>Godot</c> namespace as a bare token in any position rather than only after <c>using</c>.
/// </para>
/// <para>
/// The APIs this test forbids are a different axis, and no reference-level check can ever reach them:
/// <see cref="DateTime"/>, <c>Stopwatch</c>, <c>Environment.TickCount</c>, <see cref="Random"/>, and
/// <c>Guid.NewGuid</c> all live in assemblies every project legitimately references. <b>There is no
/// forbidden dependency to find.</b> Only type-usage inspection reaches them, and nothing else in the
/// repository does that. Do not delete this as a duplicate of the Godot checks; the Godot rule here is
/// belt-and-braces that happens to be free, and the wall-clock rules are the load-bearing part.
/// </para>
/// <para>
/// <b>A denylist of specific APIs, not an allowlist of namespaces.</b> An allowlist would fail every
/// time someone legitimately used a new BCL type, which trains people to widen it without reading -
/// and a rule that is widened reflexively stops being a rule. The list below names the specific
/// forbidden members, so a new <c>System.Buffers</c> type or a new collection costs nothing and only a
/// genuine clock read trips it.
/// </para>
/// <para>
/// <b>The known blind spots are documented in
/// <see cref="TheForbiddenApiScanIsHonestAboutWhatItCannotSee"/> rather than left to be discovered.</b>
/// A metadata scan is an enumeration, and enumerations fail by not reaching things. That test records
/// which evasion routes reach metadata and which do not.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SimulationAssemblyDeterminismTests
{
    /// <summary>The namespace prefix no simulation type may reference.</summary>
    /// <remarks>
    /// Matched as a namespace segment, so <c>Godot</c> and <c>Godot.Collections</c> are forbidden while
    /// a hypothetical <c>MechaMiner.GodotLike</c> is not.
    /// </remarks>
    private const string ForbiddenNamespaceRoot = "Godot";

    /// <summary>
    /// Whole types the simulation may not reference at all, because every member of them is a wall
    /// clock or an unseeded generator.
    /// </summary>
    /// <remarks>
    /// <see cref="DateTime"/> and <see cref="DateTimeOffset"/> are banned outright rather than by
    /// member: doc 20 § Scope and invariants excludes "wall time" from the simulation entirely, so
    /// there is no legitimate reason for an authoritative type to hold one, and banning the type also
    /// catches arithmetic and formatting paths that never touch <c>Now</c>. <c>Stopwatch</c> and
    /// <see cref="Random"/> are the same case: doc 20 § Authoritative random-number contract makes the
    /// seeded PCG stream the only generator, so <see cref="Random"/> in this assembly is a second
    /// source of randomness by definition. <c>tests/shared/README.md</c> warns that the harness's own
    /// <see cref="Random"/> "must never be confused with the authoritative contract" - this test is
    /// what makes that warning mechanical, and it is scoped to the production assembly so the harness
    /// keeps its own.
    /// </remarks>
    private static readonly ForbiddenType[] ForbiddenTypes =
    [
        new ForbiddenType("System", "DateTime", "wall time (doc 20 § Scope and invariants)"),
        new ForbiddenType("System", "DateTimeOffset", "wall time (doc 20 § Scope and invariants)"),
        new ForbiddenType(
            "System.Diagnostics",
            "Stopwatch",
            "wall time, and forbidden in tests too (doc 91 § Determinism and fixture policy)"),
        new ForbiddenType(
            "System",
            "Random",
            "an unseeded generator competing with the authoritative PCG stream "
                + "(doc 20 § Authoritative random-number contract)"),
    ];

    /// <summary>
    /// Individual members the simulation may not reference, on types it may otherwise use freely.
    /// </summary>
    /// <remarks>
    /// This is the half that makes the rule a denylist rather than an allowlist.
    /// <see cref="Guid"/> is a perfectly ordinary value type and <c>Environment</c> carries
    /// <c>ProcessorCount</c> and much else that is harmless; only <c>NewGuid</c> and the two tick
    /// counters are nondeterministic. Banning their declaring types would forbid legitimate code and
    /// invite the ban to be relaxed. The two <see cref="DateTime"/> clock properties are listed too
    /// even though the type is already banned, because the registry entry names them and a reader
    /// should find them where they are forbidden.
    /// </remarks>
    private static readonly ForbiddenMember[] ForbiddenMembers =
    [
        new ForbiddenMember("System", "Environment", "get_TickCount", "wall time since boot"),
        new ForbiddenMember("System", "Environment", "get_TickCount64", "wall time since boot"),
        new ForbiddenMember(
            "System",
            "Guid",
            "NewGuid",
            "a nondeterministic identity; doc 20 § Entity identity issues identities from the "
                + "run's allocator"),
        new ForbiddenMember("System", "DateTime", "get_Now", "wall time"),
        new ForbiddenMember("System", "DateTime", "get_UtcNow", "wall time"),
    ];

    /// <summary>
    /// Verification: <c>VER-SIM-001-014</c>.
    ///
    /// The compiled simulation assembly references none of the forbidden wall-clock, nondeterministic,
    /// or engine APIs, by any route.
    /// </summary>
    /// <remarks>
    /// Reads metadata rather than source, so how the API was reached does not matter: a
    /// fully-qualified <c>System.DateTime.UtcNow</c> with no <c>using</c> directive, an extension
    /// method, an alias, or a generic instantiation all leave the same <c>TypeReference</c> row.
    /// </remarks>
    [Test]
    public void TheCompiledAssemblyReferencesNoForbiddenDeterminismApi()
    {
        List<string> violations = ScanForForbiddenReferences(SimulationAssemblyPath());

        Assert.That(
            violations,
            Is.Empty,
            "the compiled MechaMiner.Simulation assembly references APIs doc 20 § Scope and "
                + "invariants excludes from the simulation - it is "
                + "\"a pure C# library with no dependency on Godot, files, Steam, rendering, audio, "
                + "wall time, or mutable global services\". Each line below is a metadata row, so the "
                + "reference is real regardless of how the source spelled it: "
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-014</c>.
    ///
    /// Records which evasion routes this scan reaches and which it provably cannot, so the gate's
    /// coverage is stated rather than assumed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A metadata scan fails by not reaching things, which is the same failure mode as a vacuous
    /// golden. Each route below was injected into a production file, built, and observed - not reasoned
    /// about - because the reasoning was wrong once already: a <see cref="Type"/> argument inside a
    /// custom attribute was predicted to be invisible, on the grounds that ECMA-335 serializes such an
    /// argument as a type <em>name</em> in the attribute blob rather than as a token. Roslyn emits the
    /// <c>TypeReference</c> row anyway, so that route is caught. Measured results:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Caught - reached only through a generic instantiation</b>
    /// (<c>List&lt;System.DateTime&gt;</c> field). The instantiation's signature blob carries a
    /// <c>TypeDefOrRef</c> token, which must point at a <c>TypeReference</c> row.
    /// </description></item>
    /// <item><description>
    /// <b>Caught - appearing only as an attribute argument</b>
    /// (<c>[Probe(typeof(System.DateTime))]</c>), despite the blob-encoding argument above.
    /// </description></item>
    /// <item><description>
    /// <b>Caught - captured in a lambda's closure class</b> (a local captured by a returned lambda and
    /// never named in any signature). The compiler-generated display class has a field of that type.
    /// </description></item>
    /// <item><description>
    /// <b>Caught - named only in a method body's locals</b>, never in any signature. The local-variable
    /// signature is a <c>StandAloneSig</c> blob and carries the same token.
    /// </description></item>
    /// <item><description>
    /// <b>Not caught - <c>nameof</c>.</b> <c>nameof(System.DateTime)</c> is folded to the constant
    /// string <c>"DateTime"</c> at compile time and no token survives, so nothing in the metadata
    /// distinguishes it from any other string literal. This blind spot cannot be closed by a metadata
    /// scan, and it is benign on its own - a string naming a type reads no clock - but it is <em>not</em>
    /// benign if something later reflects on that string, which is a use no gate here can see.
    /// </description></item>
    /// </list>
    /// <para>
    /// One further blind spot, found while probing and then closed rather than documented: a
    /// <c>Godot</c>-namespaced type <em>defined</em> in this assembly produces a
    /// <c>TypeDefinition</c> row and no <c>TypeReference</c> row, so the reference scan alone walked
    /// past it. <see cref="ScanForForbiddenReferences"/> now reads the definitions too.
    /// </para>
    /// <para>
    /// The assertions below are over the scan's own reachability, not over the production assembly, so
    /// this test does not become an excuse to weaken the gate above.
    /// </para>
    /// </remarks>
    [Test]
    public void TheForbiddenApiScanIsHonestAboutWhatItCannotSee()
    {
        string assemblyPath = SimulationAssemblyPath();
        HashSet<string> referencedTypes = ReadTypeReferenceNames(assemblyPath);

        Expect.Multiple(() =>
        {
            Assert.That(
                referencedTypes,
                Is.Not.Empty,
                "the scan must actually read TypeReference rows, or every gate built on it passes "
                    + "vacuously");
            Assert.That(
                referencedTypes,
                Does.Contain("System.Int32"),
                "a type used only in method bodies and signatures must appear as a TypeReference row, "
                    + "which is what makes the method-body and generic-instantiation routes reachable");
            Assert.That(
                ForbiddenTypes,
                Is.Not.Empty,
                "the denylist must be non-empty, or the gate forbids nothing");
            Assert.That(
                ForbiddenMembers,
                Is.Not.Empty,
                "and the member half must be non-empty, or the rule has collapsed into a type "
                    + "allowlist");
        });
    }

    /// <summary>
    /// The absolute path of the compiled <c>MechaMiner.Simulation</c> assembly.
    /// </summary>
    /// <remarks>
    /// Taken from a production type's assembly rather than composed from a configuration guess, so the
    /// file scanned is necessarily the one the test ran against.
    /// </remarks>
    private static string SimulationAssemblyPath()
    {
        Assembly simulation = typeof(EntityId).Assembly;
        string location = simulation.Location;

        Assert.That(
            location,
            Is.Not.Empty,
            "the simulation assembly has no file location, so its metadata cannot be read and this "
                + "gate would silently pass; a single-file or in-memory host needs the assembly "
                + "resolved a different way rather than the check skipped");
        Assert.That(File.Exists(location), Is.True, "the simulation assembly must exist at " + location);

        return location;
    }

    /// <summary>Reads every <c>TypeReference</c> row as a full type name.</summary>
    private static HashSet<string> ReadTypeReferenceNames(string assemblyPath)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            TypeReference reference = reader.GetTypeReference(handle);
            names.Add(FullName(
                reader.GetString(reference.Namespace),
                reader.GetString(reference.Name)));
        }

        return names;
    }

    /// <summary>
    /// Scans an assembly's <c>TypeReference</c> and <c>MemberReference</c> rows and returns one line
    /// per forbidden reference found.
    /// </summary>
    /// <param name="assemblyPath">The assembly to read.</param>
    /// <remarks>
    /// Rows, not source text. A <c>TypeReference</c> row exists for every type the assembly names in a
    /// signature, a local-variable signature, a generic instantiation, or a member reference's parent,
    /// so the route the source took to reach the API is not something this has to enumerate.
    /// </remarks>
    private static List<string> ScanForForbiddenReferences(string assemblyPath)
    {
        List<string> violations = new();
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            TypeReference reference = reader.GetTypeReference(handle);
            string typeNamespace = reader.GetString(reference.Namespace);
            string typeName = reader.GetString(reference.Name);

            if (IsForbiddenNamespace(typeNamespace))
            {
                violations.Add(
                    "TypeReference "
                        + FullName(typeNamespace, typeName)
                        + " - the engine namespace, which doc 20 § Scope and invariants excludes from "
                        + "the simulation");
                continue;
            }

            foreach (ForbiddenType forbidden in ForbiddenTypes)
            {
                if (string.Equals(typeNamespace, forbidden.Namespace, StringComparison.Ordinal)
                    && string.Equals(typeName, forbidden.Name, StringComparison.Ordinal))
                {
                    violations.Add(
                        "TypeReference "
                            + FullName(typeNamespace, typeName)
                            + " - "
                            + forbidden.Reason);
                }
            }
        }

        // A Godot-namespaced type *defined* in this assembly rather than referenced from the engine
        // package leaves no TypeReference row at all, so the loop above cannot see it. Reading the
        // definitions too closes that route. This is the one place where a namespace, not an API, is
        // the subject: the simulation may not host engine-namespaced types either.
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition definition = reader.GetTypeDefinition(handle);
            string definedNamespace = reader.GetString(definition.Namespace);
            if (IsForbiddenNamespace(definedNamespace))
            {
                violations.Add(
                    "TypeDefinition "
                        + FullName(definedNamespace, reader.GetString(definition.Name))
                        + " - the simulation declares a type in the engine namespace, which makes an "
                        + "engine-shaped API reachable without any engine reference to find");
            }
        }

        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            MemberReference reference = reader.GetMemberReference(handle);
            if (reference.Parent.Kind != HandleKind.TypeReference)
            {
                // A TypeSpecification or ModuleReference parent still forces a TypeReference row for
                // every named type inside it, so the type scan above has already seen it.
                continue;
            }

            TypeReference parent = reader.GetTypeReference((TypeReferenceHandle)reference.Parent);
            string parentNamespace = reader.GetString(parent.Namespace);
            string parentName = reader.GetString(parent.Name);
            string memberName = reader.GetString(reference.Name);

            foreach (ForbiddenMember forbidden in ForbiddenMembers)
            {
                if (string.Equals(parentNamespace, forbidden.Namespace, StringComparison.Ordinal)
                    && string.Equals(parentName, forbidden.TypeName, StringComparison.Ordinal)
                    && string.Equals(memberName, forbidden.MemberName, StringComparison.Ordinal))
                {
                    violations.Add(
                        "MemberReference "
                            + FullName(parentNamespace, parentName)
                            + "::"
                            + memberName
                            + " - "
                            + forbidden.Reason);
                }
            }
        }

        violations.Sort(StringComparer.Ordinal);
        return violations;
    }

    /// <summary>
    /// True when a namespace is <see cref="ForbiddenNamespaceRoot"/> or nested inside it.
    /// </summary>
    /// <remarks>
    /// Segment-aware on purpose: a prefix test alone would also match a namespace merely beginning
    /// with those letters, and a rule that fires on an innocent name is a rule that gets disabled.
    /// </remarks>
    private static bool IsForbiddenNamespace(string typeNamespace)
    {
        return string.Equals(typeNamespace, ForbiddenNamespaceRoot, StringComparison.Ordinal)
            || typeNamespace.StartsWith(ForbiddenNamespaceRoot + ".", StringComparison.Ordinal);
    }

    private static string FullName(string typeNamespace, string typeName)
    {
        return typeNamespace.Length == 0
            ? typeName
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{typeNamespace}.{typeName}");
    }

    /// <summary>A whole type the simulation may not reference.</summary>
    /// <param name="Namespace">The type's namespace.</param>
    /// <param name="Name">The type's simple name.</param>
    /// <param name="Reason">Why it is forbidden, for the failure message.</param>
    private readonly record struct ForbiddenType(string Namespace, string Name, string Reason);

    /// <summary>A single member the simulation may not reference, on an otherwise permitted type.</summary>
    /// <param name="Namespace">The declaring type's namespace.</param>
    /// <param name="TypeName">The declaring type's simple name.</param>
    /// <param name="MemberName">The member's metadata name, so a property is its accessor.</param>
    /// <param name="Reason">Why it is forbidden, for the failure message.</param>
    private readonly record struct ForbiddenMember(
        string Namespace,
        string TypeName,
        string MemberName,
        string Reason);
}
