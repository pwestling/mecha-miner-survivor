using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MechaMiner.Tools.Text;

/// <summary>One owned-text file whose bytes do not match the repository text policy.</summary>
internal sealed class TextViolation
{
    internal TextViolation(string repositoryRelativePath, string rule)
    {
        Path = repositoryRelativePath;
        Rule = rule;
    }

    /// <summary>The repository-relative file path.</summary>
    internal string Path { get; }

    /// <summary>The <c>.editorconfig</c> rule the file violates.</summary>
    internal string Rule { get; }
}

/// <summary>
/// Enforces the whole-repository text rules that <c>.editorconfig</c> declares for
/// every owned text file, not only for C#.
/// </summary>
/// <remarks>
/// <para>
/// Doc 100 § Standard command surface defines <c>format</c> as "format owned
/// text/code and fail if the resulting tree still violates policy". <c>dotnet
/// format</c> covers C# only, so shell scripts, JSON, MSBuild files, scene files,
/// and Markdown would otherwise have no owner at all.
/// </para>
/// <para>
/// Exactly three rules are enforced, and each is read straight from the
/// <c>[*]</c> section of <c>.editorconfig</c>: <c>end_of_line = lf</c>,
/// <c>insert_final_newline = true</c>, and <c>trim_trailing_whitespace = true</c>
/// with the declared Markdown exception, because Markdown gives trailing double
/// spaces meaning. Indentation is deliberately not touched: <c>.editorconfig</c>
/// declares <c>indent_style = space</c> for <c>[*]</c>, but Visual Studio writes
/// <c>MechaMiner.sln</c> with tabs, and silently reindenting a generated solution
/// file would be a formatter changing a file's meaning.
/// </para>
/// </remarks>
internal static class OwnedTextHygiene
{
    private static readonly HashSet<string> OwnedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".props", ".targets", ".config",
        ".json", ".md", ".markdown", ".sh", ".ps1", ".yml", ".yaml",
        ".tscn", ".tres", ".godot", ".cfg", ".txt",
    };

    private static readonly HashSet<string> OwnedFileNames = new(StringComparer.Ordinal)
    {
        ".editorconfig", ".gitignore", ".gitattributes",
    };

    private static readonly HashSet<string> TrailingWhitespaceExempt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown",
    };

    /// <summary>
    /// Files another tool generates. <c>AGENTS.md</c>: "Never hand-edit generated
    /// output". A formatter that normalized these would fight their generator on
    /// every restore or import.
    /// </summary>
    private static readonly HashSet<string> GeneratedFileNames = new(StringComparer.Ordinal)
    {
        "packages.lock.json",
    };

    private static readonly HashSet<string> GeneratedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Godot writes import sidecars and script uid files itself.
        ".import", ".uid",
    };

    /// <summary>Returns whether the repository owns this file's text formatting.</summary>
    internal static bool IsOwned(string repositoryRelativePath)
    {
        string normalized = repositoryRelativePath.Replace('\\', '/');
        if (normalized.StartsWith("generated/", StringComparison.Ordinal))
        {
            return false;
        }

        string name = Path.GetFileName(normalized);
        if (GeneratedFileNames.Contains(name))
        {
            return false;
        }

        string extension = Path.GetExtension(normalized);
        if (GeneratedExtensions.Contains(extension))
        {
            return false;
        }

        if (OwnedFileNames.Contains(name))
        {
            return true;
        }

        return OwnedExtensions.Contains(extension);
    }

    /// <summary>
    /// Inspects <paramref name="absolutePath"/> and, when <paramref name="write"/> is
    /// true, rewrites it in place. Returns every rule the original bytes violated.
    /// </summary>
    internal static IReadOnlyList<TextViolation> Inspect(
        string absolutePath,
        string repositoryRelativePath,
        bool write)
    {
        List<TextViolation> violations = new();
        string original;
        try
        {
            original = File.ReadAllText(absolutePath);
        }
        catch (IOException)
        {
            return violations;
        }

        if (original.Length == 0)
        {
            return violations;
        }

        bool trimTrailing = !TrailingWhitespaceExempt.Contains(Path.GetExtension(repositoryRelativePath));
        string normalized = Normalize(original, trimTrailing, violations, repositoryRelativePath);

        if (write && !string.Equals(normalized, original, StringComparison.Ordinal))
        {
            File.WriteAllText(absolutePath, normalized);
        }

        return violations;
    }

    private static string Normalize(
        string original,
        bool trimTrailing,
        List<TextViolation> violations,
        string repositoryRelativePath)
    {
        string text = original;

        if (text.Contains('\r', StringComparison.Ordinal))
        {
            violations.Add(new TextViolation(repositoryRelativePath, "end_of_line = lf"));
            text = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);
        }

        if (trimTrailing)
        {
            string[] lines = text.Split('\n');
            bool trimmedAny = false;
            for (int index = 0; index < lines.Length; index++)
            {
                string trimmed = lines[index].TrimEnd(' ', '\t');
                if (!string.Equals(trimmed, lines[index], StringComparison.Ordinal))
                {
                    trimmedAny = true;
                    lines[index] = trimmed;
                }
            }

            if (trimmedAny)
            {
                violations.Add(new TextViolation(repositoryRelativePath, "trim_trailing_whitespace = true"));
                text = string.Join("\n", lines);
            }
        }

        if (!text.EndsWith('\n'))
        {
            violations.Add(new TextViolation(repositoryRelativePath, "insert_final_newline = true"));
            text += "\n";
        }

        return text;
    }

    /// <summary>Renders a reviewable, ordered violation report.</summary>
    internal static string RenderReport(IReadOnlyList<TextViolation> violations)
    {
        StringBuilder builder = new();
        builder.Append("owned-text policy violations: ").Append(violations.Count).Append('\n');
        foreach (TextViolation violation in violations)
        {
            builder.Append("  ").Append(violation.Path).Append("  ").Append(violation.Rule).Append('\n');
        }

        return builder.ToString();
    }
}
