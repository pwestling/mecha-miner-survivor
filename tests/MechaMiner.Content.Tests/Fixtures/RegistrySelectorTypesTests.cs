using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
