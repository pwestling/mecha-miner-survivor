using System.Collections.Generic;
using System.Text;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// Renders the usage table.
/// </summary>
/// <remarks>
/// <para>
/// The usage table is the machine-comparable declaration of the wrapper contract.
/// <c>build/verify-wrapper-parity.sh</c> compares the table emitted through
/// <c>build.sh</c> with the table emitted through <c>build.ps1</c>; because both
/// wrappers are thin launchers over this one process, identical output is proof of
/// identical verbs and argument names, which is what doc 100 § Standard command
/// surface requires.
/// </para>
/// <para>
/// There is deliberately no <c>help</c> verb. Doc 100 fixes exactly eighteen verbs,
/// and doc 100 also says "Unknown verbs/arguments fail with usage", so usage is
/// reached by an invalid invocation and exits <c>2</c>.
/// </para>
/// </remarks>
internal static class Usage
{
    /// <summary>The marker line the parity fixture anchors on.</summary>
    internal const string TableHeader = "VERB TABLE (18 verbs, docs/technical/100 § Standard command surface)";

    /// <summary>Renders the full usage text, including the verb and exit-class tables.</summary>
    internal static string Render(IReadOnlyList<VerbDescriptor> verbs)
    {
        StringBuilder builder = new();
        builder.Append("usage: build.sh <verb> [arguments]\n");
        builder.Append("       build.ps1 <verb> [arguments]\n");
        builder.Append('\n');
        builder.Append("Both wrappers are thin launchers for the same typed verb host in\n");
        builder.Append("src/MechaMiner.Tools; they expose identical verbs and argument names.\n");
        builder.Append('\n');
        builder.Append(TableHeader);
        builder.Append('\n');

        int width = 0;
        foreach (VerbDescriptor verb in verbs)
        {
            int length = verb.ToInvocationText().Length;
            if (length > width)
            {
                width = length;
            }
        }

        foreach (VerbDescriptor verb in verbs)
        {
            builder.Append("  ");
            builder.Append(verb.ToInvocationText().PadRight(width));
            builder.Append("  ");
            builder.Append(verb.IsImplemented ? "implemented   " : "awaiting owner");
            builder.Append("  ");
            builder.Append(verb.OwningWorkPackage);
            builder.Append('\n');
        }

        builder.Append('\n');
        builder.Append("EXIT CLASSES (docs/technical/100 § Standard command surface; there is no 1)\n");
        builder.Append("  0  success\n");
        builder.Append("  2  invalid verb or arguments, including a verb whose owning work package has not landed\n");
        builder.Append("  3  missing or mismatched pinned environment\n");
        builder.Append("  4  validation or test failure\n");
        builder.Append("  5  build, import, export, or package failure\n");
        builder.Append("  6  performance or budget failure\n");
        builder.Append("  7  authorization, credential, or external-state action required\n");
        builder.Append("  8  unexpected tool-internal failure\n");
        builder.Append('\n');
        builder.Append("Every verb is noninteractive, writes structured evidence beneath artifacts/,\n");
        builder.Append("and prints a concise final result plus artifact paths.\n");
        return builder.ToString();
    }
}
