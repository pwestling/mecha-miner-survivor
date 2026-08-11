using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// <see cref="RegistrySelectorTypes"/> resolves what is there and refuses what is not.
/// </summary>
/// <remarks>
/// A resolver that cannot fail turns every selector walk into a walk that proves nothing, and
/// this one is a hand-written source scan rather than the runtime's own loader, so the case for
/// trusting it has to be made here rather than assumed. Two claims are made: the scan agrees
/// with reflection on the one assembly where both answers are available, and it refuses names
/// that are close to real ones.
/// </remarks>
[TestFixture]
internal sealed class RegistrySelectorTypesTests
{
    /// <summary>The <c>`1</c> a runtime name carries for each generic type parameter count.</summary>
    private static readonly Regex GenericArity = new(
        @"`[0-9]+",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Every type reflection reports in this assembly is a type the source index found.
    /// </summary>
    /// <remarks>
    /// This is the calibration. The index has to answer for three sibling test assemblies this
    /// project cannot reference, and there is exactly one assembly where its answer can be
    /// checked against the loader's - this one. A parser that miscounts braces, misses a
    /// declaration form, or attributes members to the wrong type fails here first.
    /// </remarks>
    [Test]
    public void TheIndexFindsEveryTypeReflectionFindsInThisAssembly()
    {
        List<string> missing = [];
        int checked_ = 0;

        foreach (Type type in typeof(RegistrySelectorTypesTests).Assembly.GetTypes())
        {
            if (type.IsGenericParameter
                || type.Namespace is null
                || !type.Namespace.StartsWith("MechaMiner.", StringComparison.Ordinal)
                || type.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            {
                continue;
            }

            string full = type.FullName!.Replace('+', '.');
            if (full.Contains('<', StringComparison.Ordinal)
                || full.Contains('`', StringComparison.Ordinal))
            {
                continue;
            }

            checked_++;
            if (!RegistrySelectorTypes.Declares(full))
            {
                missing.Add(full);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                checked_,
                Is.GreaterThan(0),
                "no type was compared, so this walk held the parser to nothing");
            Assert.That(
                RegistrySelectorTypes.TypesIndexed,
                Is.GreaterThan(0),
                "the source index is empty, so every selector below would fail for one reason "
                    + "and it would not be the registry's");
            Assert.That(
                missing,
                Is.Empty,
                () => "reflection reports these types in this assembly and the source index did "
                    + "not find them, so the parser is wrong rather than the registry:"
                    + Environment.NewLine + string.Join(Environment.NewLine, missing));
        });
    }

    /// <summary>
    /// Every method reflection reports as declared on a type in this assembly is a member the
    /// source index recorded for that type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists alongside the type-level walk above.</b> That walk held the parser to
    /// the runtime loader for <em>types</em> and passed throughout, including while the
    /// <c>MethodDeclaration</c> regex forbade a space in the return type and so recorded no
    /// member whose return type was a generic with more than one argument, and none whose return
    /// type was a tuple. Nothing compared <em>members</em>, so a whole class of parser fault was
    /// invisible: 12 declarations under <c>tests/</c> were silently absent, and two members named
    /// <c>static</c> were recorded that no type has. A type-level calibration cannot see either,
    /// because the types those methods live in were all found. This walk named all 12; the three
    /// tuple-returning ones - <c>Shrink</c>, <c>Split</c> and <c>Resolve</c> - it found on its
    /// first run, after the generic-argument half of the regex fix was already in.
    /// </para>
    /// <para>
    /// <b>Which asymmetry is asserted, and why not the other one.</b> Only one direction is
    /// checked: reflection's methods must all be recorded. The reverse - that every recorded
    /// member is a method reflection finds - is deliberately <em>not</em> asserted, because the
    /// two answers legitimately differ in that direction and forcing them equal would make this
    /// test fail on correct behaviour. The index records a name per declaration line, so a set of
    /// overloads collapses to one name and reflection's several <see cref="MethodInfo"/> to one
    /// entry; it records property and event accessors, local functions and expression-bodied
    /// members that reflection either renames or does not surface as a plain method; it records
    /// declarations from <c>#if</c> branches this build excluded; and it errs long by design, as
    /// <see cref="RegistrySelectorTypes"/> says, because a recorded member that is not there only
    /// makes a method-granular selector pass wrongly.
    /// </para>
    /// <para>
    /// <b>Why that is the direction worth having.</b> The failure mode the resolver documents as
    /// harmless is over-recording. Under-recording is the opposite fault and the dangerous one: a
    /// selector naming a real method is reported absent, so a registry walk fails on a fixture
    /// that is genuinely there. That is what this direction catches, and it is the direction the
    /// regex defect broke.
    /// </para>
    /// <para>
    /// The reflection side is narrowed to declarations a source scan can be expected to see:
    /// members declared on the type rather than inherited, and not synthesised by the compiler -
    /// record equality and <c>Deconstruct</c>, property and event accessors, operators, lambda
    /// and local-function bodies. Those have no declaration line to find, so their absence is the
    /// index being right rather than wrong.
    /// </para>
    /// </remarks>
    [Test]
    public void TheIndexRecordsEveryMethodReflectionFindsInThisAssembly()
    {
        List<string> missing = [];
        List<string> unknownTypes = [];
        int compared = 0;

        foreach (Type type in typeof(RegistrySelectorTypesTests).Assembly.GetTypes())
        {
            if (!IsSourceDeclared(type))
            {
                continue;
            }

            string full = NameInSource(type.FullName!);
            IReadOnlyCollection<string>? recorded = RegistrySelectorTypes.MembersOf(full);

            MethodInfo[] declared = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in declared)
            {
                if (!IsSourceDeclared(method))
                {
                    continue;
                }

                if (recorded is null)
                {
                    unknownTypes.Add(full + " (declaring " + method.Name + ")");
                    continue;
                }

                compared++;
                if (!recorded.Contains(method.Name))
                {
                    missing.Add(full + "." + method.Name);
                }
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                compared,
                Is.GreaterThan(0),
                "no method was compared, so this walk held the parser to nothing");
            Assert.That(
                RegistrySelectorTypes.MembersIndexed,
                Is.GreaterThan(0),
                "the source index recorded no members at all, so every method-granular selector "
                    + "would fail for one reason and it would not be the registry's");
            Assert.That(
                unknownTypes,
                Is.Empty,
                () => "reflection reports methods on these types and the source index recorded no "
                    + "such type, so the parser lost the type rather than the member:"
                    + Environment.NewLine + string.Join(Environment.NewLine, unknownTypes));
            Assert.That(
                missing,
                Is.Empty,
                () => "reflection reports these methods as declared in this assembly and the "
                    + "source index did not record them, so a selector naming one would be "
                    + "reported absent when it is present - the resolver under-recording, which "
                    + "is the direction that makes a walk fail wrongly:"
                    + Environment.NewLine + string.Join(Environment.NewLine, missing));
        });
    }

    /// <summary>
    /// Whether <paramref name="type"/> is a type a scan of this repository's test sources could
    /// be expected to have a declaration line for.
    /// </summary>
    private static bool IsSourceDeclared(Type type)
    {
        return !type.IsGenericParameter
            && type.Namespace is not null
            && type.Namespace.StartsWith("MechaMiner.", StringComparison.Ordinal)
            && type.GetCustomAttribute<CompilerGeneratedAttribute>() is null
            && type.FullName is not null
            && !type.FullName.Contains('<', StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether <paramref name="method"/> has a declaration line of its own. Compiler-synthesised
    /// members and anything the runtime names with characters no C# identifier can carry do not.
    /// </summary>
    private static bool IsSourceDeclared(MethodInfo method)
    {
        return !method.IsSpecialName
            && method.GetCustomAttribute<CompilerGeneratedAttribute>() is null
            && method.Name.IndexOfAny(['<', '>', '.', '`']) < 0;
    }

    /// <summary>
    /// The name a source scan records for a runtime name: nested types are joined with a dot
    /// rather than a plus, and generic arity suffixes are not written in source at all.
    /// </summary>
    private static string NameInSource(string runtimeFullName)
    {
        return GenericArity.Replace(runtimeFullName.Replace('+', '.'), string.Empty);
    }

    /// <summary>The negative control: the resolver must be able to fail.</summary>
    [Test]
    public void TheResolverRefusesNamesThatAreNotThere()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                RegistrySelectorTypes.Unresolved(
                    "MechaMiner.Content.Tests.Fixtures.RegistrySelectorTypesTests"),
                Is.Null,
                "a type in this assembly must resolve");
            Assert.That(
                RegistrySelectorTypes.Unresolved(
                    "MechaMiner.Content.Tests.Fixtures.RegistrySelectorTypesTests"
                        + ".TheResolverRefusesNamesThatAreNotThere"),
                Is.Null,
                "a method-granular selector into this assembly must resolve");
            Assert.That(
                RegistrySelectorTypes.Unresolved(
                    "MechaMiner.Simulation.Tests.Geometry.PlanarVectorTests"),
                Is.Null,
                "a type in a sibling test project this assembly cannot reference must resolve "
                    + "through the source index");
            Assert.That(
                RegistrySelectorTypes.Unresolved("MechaMiner.Simulation.Tests.Geometry.NoSuchTests"),
                Is.Not.Null,
                "a fabricated sibling type must not resolve");
            Assert.That(
                RegistrySelectorTypes.Unresolved(
                    "MechaMiner.Simulation.Tests.Geometry.PlanarVectorTests.NoSuchTestMethod"),
                Is.Not.Null,
                "a real sibling type with a fabricated member must not resolve");
            Assert.That(
                RegistrySelectorTypes.Unresolved(
                    "MechaMiner.Content.Tests.Fixtures.RegistrySelectorTypesTests.NoSuchMember"),
                Is.Not.Null,
                "a real type in this assembly with a fabricated member must not resolve");
        });
    }
}
