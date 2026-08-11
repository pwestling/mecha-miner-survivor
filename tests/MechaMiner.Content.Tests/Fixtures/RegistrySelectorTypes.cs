using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Resolves an <c>nunit</c> selector - <c>Namespace.Fixture</c> or
/// <c>Namespace.Fixture.TestMethod</c> - to the thing it names.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is not just <see cref="Assembly.GetType(string)"/>.</b> Both registry walks
/// resolved selectors against <c>MechaMiner.Content.Tests</c> and only that assembly, so
/// every selector naming a fixture in <c>MechaMiner.Simulation.Tests</c>,
/// <c>MechaMiner.Persistence.Tests</c> or <c>MechaMiner.Game.Tests</c> was unresolvable by
/// construction. 110 of the 236 nunit selectors on disk name a type outside this assembly.
/// </para>
/// <para>
/// <b>Why a source index and not a reference.</b> This project references
/// <c>src/MechaMiner.Content</c> and nothing else; it references no other test project, and
/// <c>doc 100 § Repository structure</c> prescribes exactly four test projects with no shared
/// test library, so making the sibling assemblies reflectable is a project-reference decision
/// and not this fixture's to take. Loading the siblings' build output with
/// <see cref="Assembly.LoadFrom"/> was rejected for a different reason: it resolves only when
/// those projects happen to have been built, so running this project alone would either fail
/// spuriously or - far worse - pass by skipping. The route taken instead reads the sibling
/// test <em>sources</em>, which are in this repository and always present.
/// </para>
/// <para>
/// <b>What that route does and does not prove.</b> For a type in this assembly it proves what
/// it always did: the type is loadable and, for a method-granular selector, declares that
/// member. For a type in a sibling test project it proves a type of that fully qualified name,
/// and a member of that name inside it, are <em>declared in the repository's test sources</em>.
/// That is weaker than reflection - it does not prove the declaration compiles into that
/// assembly, carries <c>[Test]</c>, or is reachable - and it is strictly stronger than the
/// previous behaviour, which could not tell a real sibling fixture from a typo.
/// <see cref="RegistrySelectorTypesTests"/> is the negative control that this index can fail.
/// </para>
/// </remarks>
internal static class RegistrySelectorTypes
{
    /// <summary>
    /// The directories under <c>tests/</c> whose sources the index reads: the four test
    /// projects doc 100 prescribes, plus <c>tests/shared</c>, whose files are linked into all
    /// four and so declare types that are really in every test assembly.
    /// </summary>
    private static readonly string[] TestSourceDirectories =
    [
        "MechaMiner.Content.Tests",
        "MechaMiner.Game.Tests",
        "MechaMiner.Persistence.Tests",
        "MechaMiner.Simulation.Tests",
        "shared",
    ];

    /// <summary>A file-scoped or block namespace declaration.</summary>
    private static readonly Regex NamespaceDeclaration = new(
        @"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>A type declaration, with its modifiers.</summary>
    /// <remarks>
    /// <c>record struct</c> and <c>record class</c> have to be spelled out, or the name
    /// captured from <c>readonly record struct Enclosing(...)</c> is <c>struct</c>. That is not
    /// hypothetical: it is what this regex did on its first run, and
    /// <see cref="RegistrySelectorTypesTests.TheIndexFindsEveryTypeReflectionFindsInThisAssembly"/>
    /// is what reported it.
    /// </remarks>
    private static readonly Regex TypeDeclaration = new(
        @"^\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly"
            + @"|ref|file|new|unsafe)\s+)*"
            + @"(?:record\s+(?:struct|class)|class|struct|record|interface|enum)\s+"
            + @"([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// A method declaration: modifiers, a return type, then the name and its paren.
    /// </summary>
    /// <remarks>
    /// The leading exclusion keeps statements out. <c>return Helper(x);</c> is two identifiers
    /// and a paren, which is the shape of a declaration, and without the guard it would record
    /// <c>Helper</c> as a member of the enclosing type. Recording a member that is not there
    /// only ever makes a method-granular selector pass that should have failed, so this list
    /// errs long.
    /// </remarks>
    private static readonly Regex MethodDeclaration = new(
        @"^\s*(?!(?:return|throw|yield|await|else|case|new|using|lock|fixed|foreach|for|while"
            + @"|if|switch|do|catch|var|base|this|default)\b)"
            + @"(?:(?:public|internal|private|protected|static|async|override|sealed|virtual"
            + @"|partial|extern|unsafe|new|abstract)\s+)*"
            + @"[A-Za-z_][A-Za-z0-9_.<>,\[\]\?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*"
            + @"(?:<[^>()]*>)?\s*\(",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Lazy<SourceIndex> Sources = new(BuildSourceIndex);

    private static Assembly ThisAssembly => typeof(RegistrySelectorTypes).Assembly;

    /// <summary>
    /// Why <paramref name="selector"/> names nothing, or <see langword="null"/> when it
    /// resolves.
    /// </summary>
    internal static string? Unresolved(string selector)
    {
        // A nested type is spelled Outer+Nested in a selector and Outer.Nested in source.
        string value = selector.Replace('+', '.');

        if (ThisAssembly.GetType(selector) is not null)
        {
            return null;
        }

        int lastDot = value.LastIndexOf('.');
        string? owner = lastDot > 0 ? value[..lastDot] : null;
        string? member = lastDot > 0 ? value[(lastDot + 1)..] : null;

        if (owner is not null)
        {
            Type? declaring = ThisAssembly.GetType(owner)
                ?? ThisAssembly.GetType(owner.Replace('.', '+'));
            if (declaring is not null)
            {
                bool declared = declaring.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.Static)
                    .Any(method => string.Equals(method.Name, member, StringComparison.Ordinal));

                return declared
                    ? null
                    : "'" + owner + "' is a type in " + ThisAssembly.GetName().Name
                        + " but declares no member '" + member + "'";
            }
        }

        SourceIndex index = Sources.Value;

        if (index.Members.ContainsKey(value))
        {
            return null;
        }

        if (owner is not null && index.Members.TryGetValue(owner, out HashSet<string>? members))
        {
            return members.Contains(member!)
                ? null
                : "'" + owner + "' is declared in " + index.FileOf[owner]
                    + " but no member '" + member + "' is declared in it";
        }

        return "'" + selector + "' is not a type in " + ThisAssembly.GetName().Name
            + ", and neither it nor its declaring type is declared anywhere under tests/";
    }

    /// <summary>How many types the source index found. A count, so an emptied index is loud.</summary>
    internal static int TypesIndexed => Sources.Value.Members.Count;

    /// <summary>The fully qualified names the source index found, for the negative control.</summary>
    internal static bool Declares(string fullyQualifiedType)
    {
        return Sources.Value.Members.ContainsKey(fullyQualifiedType);
    }

    private static SourceIndex BuildSourceIndex()
    {
        SourceIndex index = new();

        foreach (string project in TestSourceDirectories)
        {
            string root = Path.Combine(TestArtifacts.RepositoryRoot, "tests", project);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string relative = TestArtifacts.Relative(file);
                if (relative.Contains("/obj/", StringComparison.Ordinal)
                    || relative.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                IndexFile(index, file, relative);
            }
        }

        return index;
    }

    /// <summary>
    /// Records every type declared in one file, and the members declared directly in each.
    /// </summary>
    /// <remarks>
    /// Nesting is tracked by brace depth so a nested type is recorded under its enclosing
    /// type's name rather than beside it - otherwise a selector naming <c>Namespace.Nested</c>,
    /// a name no type has, would resolve. Braces inside strings and comments are not
    /// discounted; the only cost of miscounting is a type recorded at the wrong depth, which
    /// makes a selector fail rather than pass, and
    /// <see cref="RegistrySelectorTypesTests.TheIndexFindsEveryTypeReflectionFindsInThisAssembly"/>
    /// holds the parser to reflection's answer on the one assembly where both are available.
    /// </remarks>
    private static void IndexFile(SourceIndex index, string file, string relative)
    {
        string? containingNamespace = null;
        List<Enclosing> enclosing = [];
        int depth = 0;

        foreach (string line in File.ReadAllLines(file))
        {
            string trimmed = line.TrimStart();
            bool ignorable = trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith('*')
                || trimmed.StartsWith("/*", StringComparison.Ordinal);

            if (!ignorable)
            {
                Match space = NamespaceDeclaration.Match(line);
                if (space.Success)
                {
                    containingNamespace = space.Groups[1].Value;
                }
                else if (containingNamespace is not null)
                {
                    Match type = TypeDeclaration.Match(line);
                    if (type.Success)
                    {
                        string qualified = containingNamespace + "."
                            + string.Join('.', enclosing.Select(level => level.Name).Append(
                                type.Groups[1].Value));
                        index.Members.TryAdd(qualified, new HashSet<string>(StringComparer.Ordinal));
                        index.FileOf.TryAdd(qualified, relative);

                        // A bodyless declaration - `record Point(int X, int Y);` - encloses
                        // nothing, so it must not go on the stack or every later declaration in
                        // the file is recorded as nested inside it.
                        if (!trimmed.EndsWith(';'))
                        {
                            enclosing.Add(new Enclosing(type.Groups[1].Value, depth));
                        }
                    }
                    else if (enclosing.Count > 0)
                    {
                        Match method = MethodDeclaration.Match(line);
                        if (method.Success)
                        {
                            string qualified = containingNamespace + "."
                                + string.Join('.', enclosing.Select(level => level.Name));
                            index.Members[qualified].Add(method.Groups[1].Value);
                        }
                    }
                }
            }

            depth += line.Count(character => character == '{');
            depth -= line.Count(character => character == '}');

            // A type declared on one line and braced on the next sits at the depth in force
            // before its body opened, so it is only closed once its body has actually been
            // entered - popping on the declaration line itself would leave every member
            // attributed to the namespace.
            for (int level = 0; level < enclosing.Count; level++)
            {
                if (depth > enclosing[level].OpenedAt)
                {
                    enclosing[level] = enclosing[level] with { Entered = true };
                }
            }

            while (enclosing.Count > 0
                && enclosing[^1].Entered
                && depth <= enclosing[^1].OpenedAt)
            {
                enclosing.RemoveAt(enclosing.Count - 1);
            }
        }
    }

    private readonly record struct Enclosing(string Name, int OpenedAt, bool Entered = false);

    private sealed class SourceIndex
    {
        internal Dictionary<string, HashSet<string>> Members { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, string> FileOf { get; } = new(StringComparer.Ordinal);
    }
}
