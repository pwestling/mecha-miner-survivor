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
/// construction. Most of the nunit selectors on disk name a type outside this assembly, and
/// the split is not restated as a number here: it is pinned as
/// <c>NunitSelectorsReflected</c> and <c>NunitSelectorsSourceDeclared</c> by
/// <see cref="VerificationRegistryTests.TheSelectorCensusIsWhatIsDeclared"/>. A count written
/// into a comment is exactly the thing that goes stale without anything turning red, which is
/// what this one did.
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
    /// The return type in a declaration: either a tuple type, or a dotted chain of identifiers
    /// each able to carry a generic argument list; then any array-rank and nullable suffixes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spelling the generic argument list out as a bracketed group, rather than folding
    /// <c>&lt;</c>, <c>&gt;</c> and <c>,</c> into one flat character class of return-type
    /// characters, is the point of this fragment. A generic argument list of more than one
    /// argument contains a space - <c>Dictionary&lt;string, JsonSchemaNode&gt;?</c> is the
    /// shape - and a flat class has only two options, both wrong. Forbidding the space is what
    /// the flat class did, and it dropped 9 declarations from the sources this index reads, 18
    /// across <c>src/</c> and <c>tests/</c> together. Both figures re-measured at <c>455d22f</c>
    /// and unchanged. <c>f16b553</c>'s message puts the second at 17, one commit earlier, and that
    /// is not a figure that moved: the three files changed between <c>f16b553</c> and
    /// <c>455d22f</c> declare none of the 18, so the population was identical and the 17 was an
    /// undercount when it was written. Recorded here because the branch carries both numbers for
    /// one measurement and neither commit says which is right. Permitting the space everywhere is
    /// the other option, and it lets the return type span two identifiers instead of one, so
    /// <c>owners ?? new List&lt;string&gt;()));</c> reads as the return type <c>owners ??</c>
    /// declaring a member named <c>List</c>.
    /// </para>
    /// <para>
    /// A tuple return type has to be spelled out for the same reason and one more: it contains a
    /// space, and it also begins with a character no chain of identifiers can begin with. Without
    /// the alternation, <c>private static (string Before, string After) Split(</c> did not merely
    /// go unrecorded - the paren the regex found was the tuple's rather than the method's, so it
    /// recorded a member named <c>static</c>. Admitting the tuple records the <b>four</b> real
    /// declarations of that shape and withdraws the <b>three</b> phantom <c>static</c> members
    /// those lines had produced. Those two figures were three and two when they were measured at
    /// <c>ebbc38a</c>; <c>455d22f</c> added a fourth tuple-returning declaration -
    /// <see cref="Resolve"/> - and moved both, which is what a figure written into a comment does.
    /// </para>
    /// <para>
    /// Nothing outside the bracketed groups consumes <c>=</c>, and that is a narrower guarantee
    /// than it was first written as. It does keep an initialiser whose only paren follows the
    /// <c>=</c> out - <c>private static readonly Regex Foo = new(</c> does not match. It does not
    /// keep every initialiser out, because a tuple type carries a paren of its own <em>before</em>
    /// the <c>=</c>: <c>private static readonly (int A, int B) Pair = default;</c> matches, with
    /// the tuple's paren standing in for the name's. What it records in that case is whichever
    /// modifier the alternation declined to consume, and discarding those is
    /// <see cref="ReservedKeywords"/>'s job rather than this fragment's.
    /// </para>
    /// </remarks>
    private const string ReturnTypePattern =
        @"(?:\([^()]*\)|[A-Za-z_][A-Za-z0-9_]*(?:<[^()]*>)?"
        + @"(?:\.[A-Za-z_][A-Za-z0-9_]*(?:<[^()]*>)?)*)[\[\],\?]*";

    /// <summary>
    /// A method declaration: modifiers, a return type, then the name and its paren.
    /// </summary>
    /// <remarks>
    /// The leading exclusion keeps statements out. <c>return Helper(x);</c> is two identifiers
    /// and a paren, which is the shape of a declaration, and without the guard it would record
    /// <c>Helper</c> as a member of the enclosing type. Recording a member that is not there
    /// only ever makes a method-granular selector pass that should have failed, so this list
    /// errs long - and it has to actually err in that direction, which is a claim about the
    /// return type as much as about the guard, and the claim
    /// <see cref="RegistrySelectorTypesTests.TheIndexRecordsEveryMethodReflectionFindsInThisAssembly"/>
    /// now holds it to.
    /// </remarks>
    private static readonly Regex MethodDeclaration = new(
        @"^\s*(?!(?:return|throw|yield|await|else|case|new|using|lock|fixed|foreach|for|while"
            + @"|if|switch|do|catch|var|base|this|default)\b)"
            + @"(?:(?:public|internal|private|protected|static|async|override|sealed|virtual"
            + @"|partial|extern|unsafe|new|abstract)\s+)*"
            + ReturnTypePattern + @"\s+([A-Za-z_][A-Za-z0-9_]*)\s*"
            + @"(?:<[^>()]*>)?\s*\(",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Every C# reserved keyword. A captured member name that is one of these is never a member:
    /// a reserved keyword cannot be an identifier in source without an <c>@</c> prefix, and a
    /// name written <c>@readonly</c> does not begin with a character
    /// <see cref="MethodDeclaration"/>'s name group accepts. So rejecting the whole set can only
    /// discard a phantom, never a real declaration - which matters because under-recording is the
    /// direction that makes a walk fail on a fixture that is genuinely there. Contextual keywords
    /// - <c>partial</c>, <c>required</c>, <c>init</c>, <c>file</c>, <c>value</c>, <c>record</c> -
    /// are deliberately absent: those are legal identifiers, so rejecting them could drop a real
    /// member.
    /// </summary>
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };

    private static readonly Lazy<SourceIndex> Sources = new(BuildSourceIndex);

    private static Assembly ThisAssembly => typeof(RegistrySelectorTypes).Assembly;

    /// <summary>
    /// Which of the two routes answered a selector, and so how strong the answer is.
    /// </summary>
    /// <remarks>
    /// The routes are not equally strong - see the class remarks - so a census that counted
    /// both as one number would claim more than it had. This is what lets
    /// <see cref="VerificationRegistryTests.TheSelectorCensusIsWhatIsDeclared"/> pin them apart.
    /// </remarks>
    internal enum Route
    {
        /// <summary>Neither route found what the selector names.</summary>
        None,

        /// <summary>
        /// The runtime loader answered: the type is in this assembly and, for a method-granular
        /// selector, reflection found a member of that name declared on it. "Declared on it" is
        /// literal - <see cref="Resolve"/> passes <see cref="BindingFlags.DeclaredOnly"/>, so an
        /// inherited member does not answer, and this route agrees with the narrowing the member
        /// calibration already made on the other side of the comparison.
        /// </summary>
        Reflection,

        /// <summary>
        /// The source index answered: a declaration of that name is in the repository's test
        /// sources. Strictly weaker than <see cref="Reflection"/>.
        /// </summary>
        SourceIndex,
    }

    /// <summary>
    /// Why <paramref name="selector"/> names nothing, or <see langword="null"/> when it
    /// resolves.
    /// </summary>
    internal static string? Unresolved(string selector)
    {
        return Resolve(selector).Reason;
    }

    /// <summary>
    /// Which route resolved <paramref name="selector"/>, or <see cref="Route.None"/> when
    /// neither did.
    /// </summary>
    internal static Route RouteOf(string selector)
    {
        return Resolve(selector).Route;
    }

    /// <summary>
    /// Resolves a selector once, reporting both the route that answered and - when neither
    /// did - why. One implementation deliberately: a second one that only computed the route
    /// could disagree with this one about which selectors resolve at all, and the census would
    /// then be a census of something other than what the walk checks.
    /// </summary>
    private static (Route Route, string? Reason) Resolve(string selector)
    {
        // A nested type is spelled Outer+Nested in a selector and Outer.Nested in source.
        string value = selector.Replace('+', '.');

        if (ThisAssembly.GetType(selector) is not null)
        {
            return (Route.Reflection, null);
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
                // DeclaredOnly, because Route.Reflection says "declared on it" and without this
                // flag every fixture answered for Equals, GetHashCode and ToString as well - a
                // selector naming a member it inherits from object rather than one it declares.
                // It is also the narrowing the member calibration passes on the reflection side,
                // so the two agree about what a declaration is. Adding it moved no selector: all
                // 126 nunit selectors that reflection answers at 455d22f still resolve.
                bool declared = declaring.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.Instance | BindingFlags.Static
                        | BindingFlags.DeclaredOnly)
                    .Any(method => string.Equals(method.Name, member, StringComparison.Ordinal));

                return declared
                    ? (Route.Reflection, null)
                    : (Route.None, "'" + owner + "' is a type in " + ThisAssembly.GetName().Name
                        + " but declares no member '" + member + "'");
            }
        }

        SourceIndex index = Sources.Value;

        if (index.Members.ContainsKey(value))
        {
            return (Route.SourceIndex, null);
        }

        if (owner is not null && index.Members.TryGetValue(owner, out HashSet<string>? members))
        {
            return members.Contains(member!)
                ? (Route.SourceIndex, null)
                : (Route.None, "'" + owner + "' is declared in " + index.FileOf[owner]
                    + " but no member '" + member + "' is declared in it");
        }

        return (
            Route.None,
            "'" + selector + "' is not a type in " + ThisAssembly.GetName().Name
                + ", and neither it nor its declaring type is declared anywhere under tests/");
    }

    /// <summary>
    /// How many types the source index found. Pinned to a literal by
    /// <see cref="RegistrySelectorTypesTests.TheIndexFindsEveryTypeReflectionFindsInThisAssembly"/>,
    /// so an emptied index is loud and so is one that lost a declaration form.
    /// </summary>
    internal static int TypesIndexed => Sources.Value.Members.Count;

    /// <summary>
    /// How many members the source index found, across every type. Pinned to a literal by
    /// <see cref="RegistrySelectorTypesTests.TheIndexRecordsEveryMethodReflectionFindsInThisAssembly"/>,
    /// which is what makes a return type the <see cref="MethodDeclaration"/> regex stops matching
    /// loud rather than silent. Until that literal existed the only assertion on this number was
    /// that it exceeded zero, so a regex change could drop ten or a hundred declarations of the
    /// 1195 recorded at <c>455d22f</c> without anything turning red - which is what the flat
    /// return-type character class did.
    /// </summary>
    internal static int MembersIndexed => Sources.Value.Members.Values.Sum(members => members.Count);

    /// <summary>
    /// Every member the index recorded whose name is a C# reserved keyword, as
    /// <c>Type.member</c>. Always empty when the parser is right, because no such member can be
    /// declared; anything here is the <see cref="MethodDeclaration"/> regex having captured a
    /// modifier in the name group.
    /// </summary>
    internal static IReadOnlyCollection<string> MembersNamedWithAKeyword
    {
        get
        {
            return Sources.Value.Members
                .SelectMany(type => type.Value
                    .Where(member => ReservedKeywords.Contains(member))
                    .Select(member => type.Key + "." + member))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }
    }

    /// <summary>The fully qualified names the source index found, for the negative control.</summary>
    internal static bool Declares(string fullyQualifiedType)
    {
        return Sources.Value.Members.ContainsKey(fullyQualifiedType);
    }

    /// <summary>
    /// The members the source index recorded directly in <paramref name="fullyQualifiedType"/>, or
    /// <see langword="null"/> when it recorded no such type at all - a distinction the member-level
    /// calibration needs, because an unknown type and a known type with a missing member are
    /// different parser faults.
    /// </summary>
    internal static IReadOnlyCollection<string>? MembersOf(string fullyQualifiedType)
    {
        return Sources.Value.Members.TryGetValue(
            fullyQualifiedType,
            out HashSet<string>? members)
            ? members
            : null;
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
    /// <para>
    /// Nesting is tracked by brace depth so a nested type is recorded under its enclosing
    /// type's name rather than beside it - otherwise a selector naming <c>Namespace.Nested</c>,
    /// a name no type has, would resolve.
    /// </para>
    /// <para>
    /// Braces inside strings and comments are not discounted, and miscounting them is not
    /// one-directional the way this remark used to claim. An unbalanced closing brace inside a
    /// string pops the enclosing stack early, so the very case the paragraph above worries about
    /// is the case a miscount produces: <c>Namespace.Nested</c> is recorded instead of
    /// <c>Namespace.Outer.Nested</c>, which makes the correct selector fail <em>and</em> makes a
    /// selector naming <c>Namespace.Nested</c> - a name no type has - pass. No live instance was
    /// found: the member calibration reports any type it recorded no entry for, and it reports
    /// none across <c>MechaMiner.Content.Tests</c>, <c>MechaMiner.Simulation.Tests</c> and
    /// <c>MechaMiner.Persistence.Tests</c>, measured at <c>455d22f</c>. What holds the parser to
    /// the loader's answer on the one assembly where both are available is
    /// <see cref="RegistrySelectorTypesTests.TheIndexFindsEveryTypeReflectionFindsInThisAssembly"/>.
    /// </para>
    /// <para>
    /// A comment counts too, and that is worth stating because it is easy to miss: a comment line
    /// is skipped for <em>declaration</em> matching and not for brace counting, so an unbalanced
    /// brace written inside one shifts the depth exactly as a string's would. Writing the closing
    /// brace of the paragraph above literally rather than in words is what proved it - the two
    /// calibrations went red on <c>Enclosing</c>, <c>SourceIndex</c> and <c>IndexFile</c>, which
    /// is this class's own nested types and its own method being popped out from under it.
    /// Discounting braces on comment lines would be a real narrowing of the fault and is not done
    /// here; a block comment's interior lines would still count, so it is a change with its own
    /// measurement to make.
    /// </para>
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

                        // A captured name that is a reserved keyword is not a member: it is a
                        // modifier the alternation declined to consume, taken as the name because
                        // a paren followed - the tuple's own, on a tuple-typed field or property.
                        // See ReservedKeywords for why discarding the whole set is safe.
                        if (method.Success
                            && !ReservedKeywords.Contains(method.Groups[1].Value))
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

            // A positional record whose parameter list spans lines - `record struct Fixture(`
            // over five lines, closing `);` - is bodyless, but the `;` is not on the declaration
            // line, so the guard above pushed it. Nothing then opens a brace, so it is never
            // entered and never popped, and every later declaration in the file is filed inside
            // it. Popping an un-entered enclosing at the first line ending in `;` closes that:
            // the only line that can end in `;` between a type's declaration and its opening
            // brace is the `;` that says there is no body. Entered is already true by then for a
            // type that has one, because its opening brace is processed before any member line.
            // (Written in words rather than as the character, for the reason the remarks give.)
            if (enclosing.Count > 0 && !enclosing[^1].Entered && line.TrimEnd().EndsWith(';'))
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
