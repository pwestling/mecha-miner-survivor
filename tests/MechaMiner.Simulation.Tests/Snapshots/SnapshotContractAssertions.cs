using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// The immutability and no-mutation assertions of <c>SIM-007</c>, written once so the real snapshot and the
/// deliberately broken stubs are judged by literally the same code.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-007-001</c>, <c>VER-SIM-007-004</c>, and <c>VER-SIM-007-011</c>.
/// </para>
/// <para>
/// <b>The immutability assertion is structural, not an inspection of remembered members.</b> It walks every
/// field of a payload type - public and non-public, and recursively into any nested type declared in the
/// simulation assembly - and requires each one's <em>type</em> to be immutable. A member added later by
/// someone who never read this test still has to satisfy it, which a test that pokes at today's members
/// cannot promise. doc 115 § Cross-boundary contract registry: "Cross-boundary payloads never expose
/// mutable collections."
/// </para>
/// </remarks>
internal static class SnapshotContractAssertions
{
    /// <summary>
    /// Type names that are immutable by construction, so the walk can stop at them.
    /// </summary>
    /// <remarks>
    /// <see cref="ReadOnlyMemory{T}"/> is here and <c>Memory{T}</c> deliberately is not: the first has no
    /// member that writes, the second does. An array type is never here, at any depth.
    /// </remarks>
    private static readonly HashSet<string> ImmutableFrameworkTypes = new(StringComparer.Ordinal)
    {
        "System.Boolean",
        "System.Byte",
        "System.SByte",
        "System.Int16",
        "System.UInt16",
        "System.Int32",
        "System.UInt32",
        "System.Int64",
        "System.UInt64",
        "System.Single",
        "System.Double",
        "System.Decimal",
        "System.Char",
        "System.String",
        "System.Guid",
        "System.DateTimeOffset",
        "System.TimeSpan",
        "System.ReadOnlyMemory`1",
    };

    /// <summary>
    /// Asserts that every field of every named payload type has an immutable type, transitively, and that no
    /// public member offers a setter or hands out a mutable collection.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="payloadTypes">The types that cross the contract boundary.</param>
    /// <remarks>
    /// Fields rather than only properties, and non-public as well as public: a private array field on a
    /// payload type is exactly the "mutable store" doc 20 § Presentation snapshot forbids exposing, because
    /// the page's own producer can still rewrite it while a consumer holds the payload. Keeping the arrays in
    /// the double buffer - which is not a payload type - is what lets this assertion have no exemptions.
    /// </remarks>
    internal static void PayloadTypesAreStructurallyImmutable(string subject, params Type[] payloadTypes)
    {
        ArgumentNullException.ThrowIfNull(payloadTypes);

        List<string> violations = new();
        foreach (Type payloadType in payloadTypes)
        {
            InspectType(payloadType, payloadType.Name, violations, new HashSet<Type>(), depth: 0);
            InspectPublicSurface(payloadType, violations);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                payloadTypes,
                Is.Not.Empty,
                subject + ": the assertion must be given types, or it is vacuous");
            Assert.That(
                violations,
                Is.Empty,
                subject + ": these members are mutable or hand out something mutable, so a consumer could "
                    + "write through the payload:\n  " + string.Join("\n  ", violations));
        });
    }

    /// <summary>
    /// Asserts that a rebuild pass mutated nothing: the authoritative rendering before and after are
    /// identical, and so is the next tick's.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="controlRendering">The rendering of a run in which no rebuild occurred.</param>
    /// <param name="rebuiltRendering">The rendering of a run in which a full rebuild pass ran.</param>
    /// <param name="rebuiltFieldCount">How many fields the rebuild pass actually read.</param>
    /// <remarks>
    /// An invariant over the resulting state rather than an assertion about the rebuild call: it compares the
    /// whole committed rendering across both runs, including the tick after the rebuild, so a write-through
    /// by any route shows up. doc 20 § Scope and invariants: "presentation cannot mutate simulation state."
    /// </remarks>
    internal static void RebuildMutatedNothing(
        string subject,
        string controlRendering,
        string rebuiltRendering,
        int rebuiltFieldCount)
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                rebuiltFieldCount,
                Is.GreaterThan(0),
                subject + ": the rebuild pass must have read something, or the comparison is vacuous");
            Assert.That(
                rebuiltRendering,
                Is.EqualTo(controlRendering),
                subject + ": a full rebuild from a snapshot must leave the committed state and the next "
                    + "tick's behaviour bit-identical to a run in which no rebuild occurred");
        });
    }

    private static void InspectType(
        Type type,
        string path,
        List<string> violations,
        HashSet<Type> visited,
        int depth)
    {
        if (depth > 8 || !visited.Add(type))
        {
            return;
        }

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (FieldInfo field in fields)
        {
            string fieldPath = path + "." + field.Name + " : " + Describe(field.FieldType);
            if (!IsImmutableType(field.FieldType))
            {
                violations.Add(fieldPath + " (mutable field type)");
                continue;
            }

            if (IsSimulationValueType(field.FieldType))
            {
                InspectType(field.FieldType, path + "." + field.Name, violations, visited, depth + 1);
            }

            Type[] arguments = field.FieldType.IsGenericType
                ? field.FieldType.GetGenericArguments()
                : Array.Empty<Type>();
            foreach (Type argument in arguments)
            {
                if (!IsImmutableType(argument))
                {
                    violations.Add(fieldPath + " (mutable generic argument " + Describe(argument) + ")");
                }
                else if (IsSimulationValueType(argument))
                {
                    InspectType(argument, path + "." + field.Name + "<>", violations, visited, depth + 1);
                }
            }
        }
    }

    private static void InspectPublicSurface(Type type, List<string> violations)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            violations.Add(type.Name + "." + field.Name + " is a public field");
        }

        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (property.CanWrite)
            {
                violations.Add(type.Name + "." + property.Name + " has a setter");
            }

            if (!IsImmutableType(property.PropertyType))
            {
                violations.Add(
                    type.Name + "." + property.Name + " returns " + Describe(property.PropertyType));
            }
        }
    }

    private static bool IsImmutableType(Type type)
    {
        if (type.IsArray || type.IsPointer || type.IsByRef)
        {
            return false;
        }

        if (type.IsEnum || type.IsPrimitive)
        {
            return true;
        }

        if (ImmutableFrameworkTypes.Contains(NameWithoutArity(type)))
        {
            return true;
        }

        if (IsSimulationValueType(type))
        {
            return true;
        }

        // A reference type other than string is only immutable if it is one of this package's own sealed
        // payload classes; anything else - a collection, a builder, an unknown class - is treated as mutable.
        return type == typeof(MechaMiner.Simulation.Snapshots.PresentationSnapshot);
    }

    private static bool IsSimulationValueType(Type type)
    {
        return type.IsValueType
            && !type.IsEnum
            && !type.IsPrimitive
            && type.Assembly == typeof(MechaMiner.Simulation.Snapshots.PresentationSnapshot).Assembly;
    }

    private static string NameWithoutArity(Type type)
    {
        Type target = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return target.FullName ?? target.Name;
    }

    private static string Describe(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        Type[] arguments = type.GetGenericArguments();
        string[] names = new string[arguments.Length];
        for (int index = 0; index < arguments.Length; index++)
        {
            names[index] = Describe(arguments[index]);
        }

        return NameWithoutArity(type)
            + "<"
            + string.Join(",", names)
            + "> (arity "
            + arguments.Length.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
