using System;
using System.Collections.Generic;
using System.Reflection;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The injection seam: a test may script a source, and production cannot select an alternate
/// algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-015</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract): "Tests may inject a scripted
/// source, but production content cannot select an alternate algorithm", and the generator:
/// "Golden vectors pin initialization and output, so a runtime/library RNG is never an implicit
/// substitute." Also
/// <c>docs/technical/20-simulation-core.md</c> § Headless execution.
/// </para>
/// <para>
/// The second half of doc 20 § Authoritative random-number contract is asserted structurally
/// rather than by inspection, because a comment cannot stop a later work package from adding a
/// generator parameter "just for a test". Reflection over the public surface of
/// <see cref="RandomStreamSet"/> is what makes that addition a red test.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RandomSourceInjectionTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-015</c>. A scripted source drives every consumer,
    /// exhaustion fails loudly, and no production entry point accepts a generator.
    /// </summary>
    [Test]
    public void ScriptedSourceIsInjectableAndProductionHasNoAlgorithmChoice()
    {
        Expect.Multiple(() =>
        {
            // 1. A scripted source is injectable into every consumer of the seam.
            ScriptedRandomSource scripted = new(0x00000000U, 0xFFFFFFFFU, 0x12345678U, 0x9ABCDEF0U);
            Assert.That(scripted.NextUInt32(), Is.EqualTo(0x00000000U), "values arrive in order");
            Assert.That(scripted.NextUInt32(), Is.EqualTo(0xFFFFFFFFU));
            Assert.That(scripted.DrawCount, Is.EqualTo(2UL));

            ScriptedRandomSource forBounded = new(7U);
            Assert.That(BoundedRandom.NextBounded(forBounded, 10U), Is.EqualTo(7U));

            ScriptedRandomSource forDouble = new(0x80000000U, 0x00000000U);
            Assert.That(BoundedRandom.NextUnitDouble(forDouble), Is.EqualTo(0.5), "pinned by injection");

            ScriptedRandomSource forSelection = new(1U);
            Assert.That(
                CanonicalSelection.TrySelectFromCanonicalOrder(
                    forSelection,
                    new[] { "A", "B", "C" },
                    out string? selected),
                Is.True);
            Assert.That(selected, Is.EqualTo("B"), "a scripted draw of 1 selects the second candidate");

            // 2. Exhaustion is a loud failure, never a fallback to a real generator.
            ScriptedRandomSource exhausted = new(1U);
            _ = exhausted.NextUInt32();
            InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
                () => exhausted.NextUInt32());
            Assert.That(failure.Message, Does.Contain("exhausted"));
            Assert.That(
                failure.Message,
                Does.Contain("never falls back to a real generator"),
                "doc 20 § Authoritative random-number contract: a runtime/library RNG is never an implicit substitute");
            Assert.That(exhausted.RemainingCount, Is.Zero);

            // A scripted source is a snapshot of its values: mutating the caller's array afterwards
            // cannot change what a pinned test observes.
            uint[] mutable = { 42U };
            ScriptedRandomSource copied = new(mutable);
            mutable[0] = 43U;
            Assert.That(copied.NextUInt32(), Is.EqualTo(42U));

            // 3. Production cannot select an alternate algorithm because there is no code path
            // that accepts one - not a public one, and not a private one either. Reflection
            // covers every member of the stream set at every accessibility, so this is a
            // statement about the type rather than about the routes someone remembered to check.
            List<string> injectionPoints = new();
            foreach (MethodBase member in EveryMember(typeof(RandomStreamSet)))
            {
                foreach (ParameterInfo parameter in member.GetParameters())
                {
                    if (IsAlgorithmChoice(parameter.ParameterType))
                    {
                        injectionPoints.Add(member.Name + "(" + parameter.Name + ")");
                    }
                }
            }

            Assert.That(
                injectionPoints,
                Is.Empty,
                "doc 20 § Authoritative random-number contract: production content cannot select "
                    + "an alternate algorithm, so no member of RandomStreamSet at any accessibility "
                    + "may accept a source, generator, or delegate. A stream set derives every "
                    + "stream from its own master seed; there is nowhere to put a foreign one");

            // The only state in the set that is typed as the seam is its own private cursor type,
            // so even the set's internals cannot hold a foreign source.
            foreach (FieldInfo field in typeof(RandomStreamSet).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (Type mentioned in MentionedTypes(field.FieldType))
                {
                    if (!typeof(IRandomSource).IsAssignableFrom(mentioned))
                    {
                        continue;
                    }

                    Assert.That(
                        mentioned.DeclaringType,
                        Is.EqualTo(typeof(RandomStreamSet)),
                        "field " + field.Name + " may only hold RandomStreamSet's own source type");
                    Assert.That(
                        mentioned.IsVisible,
                        Is.False,
                        "field " + field.Name + " holds a type no other assembly can construct");
                }
            }

            // The set's only construction inputs are the schema version and the master seed.
            ConstructorInfo[] constructors = typeof(RandomStreamSet)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(constructors, Has.Length.EqualTo(1), "there is one way to create a stream set");
            ParameterInfo[] parameters = constructors[0].GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(RandomSchemaVersion)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(ulong)));

            // And nothing in the namespace can install a source into the set: the only settable
            // public state would be a property setter, and there is none.
            foreach (PropertyInfo property in typeof(RandomStreamSet).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Assert.That(
                    property.CanWrite,
                    Is.False,
                    "RandomStreamSet." + property.Name + " must not be settable");
            }

            // 4. The scripted source is the only injectable implementation of the seam, so a
            // production consumer that takes an IRandomSource is taking a real stream or a test's
            // script - never a third, unpinned algorithm.
            List<string> publicImplementations = new();
            foreach (Type type in typeof(IRandomSource).Assembly.GetExportedTypes())
            {
                if (type != typeof(IRandomSource) && typeof(IRandomSource).IsAssignableFrom(type))
                {
                    publicImplementations.Add(type.Name);
                }
            }

            publicImplementations.Sort(StringComparer.Ordinal);
            Assert.That(
                publicImplementations,
                Is.EqualTo(new[] { "ScriptedRandomSource" }),
                "the production implementation is private to RandomStreamSet; the only public one "
                    + "is the scripted test source");
        });
    }

    /// <summary>Every constructor and method of a type, at every accessibility.</summary>
    private static IReadOnlyList<MethodBase> EveryMember(Type type)
    {
        const BindingFlags everything = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        List<MethodBase> members = new();
        foreach (ConstructorInfo constructor in type.GetConstructors(everything))
        {
            members.Add(constructor);
        }

        foreach (MethodInfo method in type.GetMethods(everything))
        {
            members.Add(method);
        }

        return members;
    }

    /// <summary>A type and the types its generic arguments and element type mention.</summary>
    private static IReadOnlyList<Type> MentionedTypes(Type type)
    {
        List<Type> mentioned = new() { type };
        if (type.HasElementType)
        {
            Type? element = type.GetElementType();
            if (element is not null)
            {
                mentioned.Add(element);
            }
        }

        foreach (Type argument in type.GetGenericArguments())
        {
            mentioned.Add(argument);
        }

        return mentioned;
    }

    private static bool IsAlgorithmChoice(Type parameterType)
    {
        return typeof(IRandomSource).IsAssignableFrom(parameterType)
            || typeof(Delegate).IsAssignableFrom(parameterType);
    }
}
