using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Categories;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The <c>branch_class</c> field name is justified by what the value classifies, and by
/// nothing else, in both artifacts that justify it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What was retracted.</b> Both <c>content/schemas/branch.schema.json</c> and
/// <see cref="BranchSchema"/>'s remarks argued the name on two grounds: that
/// <c>branch_class</c> says what the value classifies, and that the shorter spelling is a
/// C# keyword a typed model would have to escape. The second was decorative and wrong.
/// <see cref="BranchDto"/> annotates every property with
/// <see cref="JsonPropertyNameAttribute"/>, so the wire name never has to be a C#
/// identifier at all; and the property behind the shorter wire spelling would be
/// <c>Class</c>, which is an ordinary identifier that needs no escape and no attribute of
/// its own. The first ground stands on its own and is the whole argument now.
/// </para>
/// <para>
/// <b>Why a test and not a commit message.</b> The claim lived in two places. Correcting
/// one and leaving the other is the ordinary way a retracted argument survives: the
/// surviving copy is later read as independent corroboration, and the pair is restored on
/// the grounds that they agreed. These assertions hold both copies at once, and the
/// reflection assertion below states the fact the retraction rests on, so that the day
/// <see cref="BranchDto"/> stops annotating its properties the retraction is re-examined
/// rather than assumed.
/// </para>
/// <para>
/// This says nothing about whether the corpus under <c>content/branches/</c> satisfies the
/// branch schema; the schema and the corpus disagree on several points and reconciling
/// them is the catalog stream's work.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-003-036</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class BranchClassNamingRationaleTests
{
    /// <summary>
    /// The words the withdrawn argument was made of. A description that reaches for any of
    /// them is appealing to the language rather than to the content again.
    /// </summary>
    private static readonly string[] TheWithdrawnAppeal =
    {
        "escape",
        "keyword",
        "reserved word",
    };

    private static string SchemaDescription()
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", "branch.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(path));
        return schema.RootElement
            .GetProperty("properties").GetProperty("branch_class")
            .GetProperty("description").GetString()!;
    }

    [Test]
    public void TheSchemaJustifiesBranchClassByWhatItClassifiesAndNotByCsharpNaming()
    {
        string description = SchemaDescription();

        Expect.Multiple(() =>
        {
            Assert.That(
                description,
                Does.Contain("says what the value classifies"),
                () => "content/schemas/branch.schema.json must keep the ground that holds - "
                    + "the qualified name says what the value classifies. Deleting the whole "
                    + "sentence rather than the withdrawn half leaves the field name "
                    + "unjustified: " + description);

            foreach (string appeal in TheWithdrawnAppeal)
            {
                Assert.That(
                    description,
                    Does.Not.Contain(appeal).IgnoreCase,
                    () => "the branch_class description appeals to '" + appeal
                        + "'. That argument was withdrawn as false: BranchDto carries "
                        + "JsonPropertyName on every property, so the wire name is not a C# "
                        + "identifier, and the property behind a wire name of 'class' would "
                        + "be 'Class', which needs no escape. Justify the name by what it "
                        + "classifies: " + description);
            }
        });
    }

    /// <summary>
    /// The fact the retraction rests on: the wire name and the property name are already
    /// independent, so no wire spelling can force an escape.
    /// </summary>
    /// <remarks>
    /// If a property here ever loses its annotation, the DTO starts depending on member
    /// names matching wire names and the withdrawn argument acquires a grain of truth. This
    /// assertion is what makes that a failure rather than a quiet reversal.
    /// </remarks>
    [Test]
    public void EveryBranchDtoPropertyNamesItsWireFieldExplicitly()
    {
        System.Type dto = typeof(BranchSchema).Assembly.GetType(
            "MechaMiner.Content.Categories.BranchDto")!;

        PropertyInfo[] properties = dto.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        List<string> unannotated = new();
        foreach (PropertyInfo property in properties)
        {
            if (property.GetCustomAttribute<JsonPropertyNameAttribute>() is null)
            {
                unannotated.Add(property.Name);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                properties,
                Is.Not.Empty,
                "BranchDto declares no properties, so this assertion would hold over nothing");
            Assert.That(
                unannotated,
                Is.Empty,
                () => "these BranchDto properties carry no JsonPropertyName: "
                    + string.Join(", ", unannotated)
                    + ". The branch_class naming rationale in BranchDefinition.cs and in "
                    + "branch.schema.json rests on the wire name and the property name being "
                    + "independent; if that stops being true, re-examine the retraction "
                    + "rather than leaving it asserted");
        });
    }
}
