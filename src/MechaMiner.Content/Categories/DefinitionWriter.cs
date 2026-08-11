using System;
using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>
/// One definition kind's canonical writer: the sixteen of them are the sixteen instances
/// <see cref="CategoryDescriptor.Writer"/> holds.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline
/// states the whole job: "The canonical writer emits fields in schema-declared order,
/// dictionaries as lexically sorted key entries, stable-ID sets in canonical ID order, and
/// semantically ordered arrays in their authored/explicit order." All four clauses are
/// implemented by <see cref="CanonicalJsonWriter"/>; this type's work is to walk a definition
/// and apply the right one at every position, at every depth.
/// </para>
/// <para>
/// <b>Why one type with sixteen instances rather than sixteen hand-written classes.</b> A
/// per-category writer that named a field and then named an operation for it would restate,
/// sixteen times over, the answer its field table already gives - and doc 40 classifies no
/// individual field, so those restatements would be the contract's only per-field record in a
/// second place, free to disagree with the first. The disagreement is undetectable by
/// inspection and invisible at run time, because both array operations emit well-formed JSON
/// of the declared kind: it shows up as a changed hash. So the walk is written once, and what
/// distinguishes one kind's writer from another's is entirely the
/// <see cref="DefinitionShape"/> it is bound to. The count is still sixteen, and
/// <see cref="CategorySchemas.All"/> is what makes it sixteen: a kind added without a field
/// table does not compile, and a kind added with one gets a writer without anybody writing a
/// writer.
/// </para>
/// <para>
/// <b>What decides each array's treatment.</b> Nothing here. Every array goes through
/// <see cref="ArrayOrderEmitter"/> carrying <see cref="DefinitionField.Order"/>, the class its
/// declaration states. There is no <c>switch</c> on a field name in this file and no list of
/// which fields are sets.
/// </para>
/// <para>
/// <b>Two things this writer does not do, deliberately.</b> It does not validate: it takes a
/// definition that a reader already accepted, because a writer that re-checked would be a
/// second validator with its own opinions. And it does not materialize defaults for absent
/// optional <em>domain</em> fields - it omits them. Doc 40 § Common definition envelope requires
/// materialized defaults and names the fields it means, all of them envelope fields, which
/// <see cref="DefinitionEnvelope.WriteCanonicalFields"/> already materializes; no accepted
/// document states a default for any optional domain field, and inventing one per field would
/// be manufacturing contract. That half of DAT-006 is a separate change and needs the grant
/// listed with it.
/// </para>
/// </remarks>
public sealed class DefinitionWriter
{
    private readonly DefinitionShape _shape;

    internal DefinitionWriter(DefinitionKind kind, DefinitionShape shape)
    {
        Kind = kind;
        _shape = shape;
        RootOrder = BuildRootOrder(kind, shape);
    }

    /// <summary>The kind this writer emits.</summary>
    public DefinitionKind Kind { get; }

    /// <summary>
    /// The canonical field order of a whole definition document: the envelope's nine names in
    /// <see cref="EnvelopeSchema.Order"/>'s order, then this kind's domain fields in the
    /// field table's order.
    /// </summary>
    /// <remarks>
    /// One order for one object, because a definition is one object. The envelope's nine come
    /// first because doc 40 tabulates them first and every category shares them; a category
    /// that interleaved its own fields among them would make the envelope's position a
    /// per-category fact.
    /// </remarks>
    public SchemaFieldOrder RootOrder { get; }

    /// <summary>The field table whose order and array classes this writer obeys.</summary>
    public DefinitionShape Shape => _shape;

    /// <summary>
    /// Writes one definition's canonical payload: <paramref name="envelope"/>'s nine fields
    /// followed by the domain fields <paramref name="document"/> carries, in declared order.
    /// </summary>
    /// <param name="writer">The canonical writer to emit into.</param>
    /// <param name="envelope">The validated envelope, which supplies the nine shared fields.</param>
    /// <param name="document">
    /// The authored document's root object. Only its declared domain properties are read; the
    /// nine envelope properties in it are ignored, because the validated
    /// <paramref name="envelope"/> is the normalized form of those and the authored text is not.
    /// </param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="document"/> is not an object.</exception>
    public void WriteCanonical(
        CanonicalJsonWriter writer,
        DefinitionEnvelope envelope,
        JsonElement document)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(envelope);
        if (document.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "a definition document is a JSON object, and this one is "
                    + document.ValueKind,
                nameof(document));
        }

        writer.BeginObject(RootOrder);
        envelope.WriteCanonicalFields(writer);
        WriteFields(writer, _shape, document);
        writer.EndObject();
    }

    /// <summary>The canonical payload bytes of one definition.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is null.</exception>
    public byte[] ToCanonicalUtf8(DefinitionEnvelope envelope, JsonElement document)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return CanonicalJson.Serialize(writer => WriteCanonical(writer, envelope, document));
    }

    /// <summary>The SHA-256 hex digest of one definition's canonical payload.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is null.</exception>
    public string CanonicalSha256Hex(DefinitionEnvelope envelope, JsonElement document)
    {
        return CanonicalHash.Sha256Hex(ToCanonicalUtf8(envelope, document));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind + " canonical writer over " + _shape.Subject;
    }

    private static SchemaFieldOrder BuildRootOrder(DefinitionKind kind, DefinitionShape shape)
    {
        List<string> names = new(EnvelopeSchema.Fields.Count + shape.Fields.Count);
        names.AddRange(EnvelopeSchema.Fields);
        names.AddRange(shape.FieldNames());

        // SchemaFieldOrder rejects a repeated name, which is the check that matters here: a
        // category field table that redeclared an envelope field would give the document two
        // canonical positions for one property. DefinitionShapeValidator accepts the nine at
        // the root without a table restating them, so the two declarations agreeing is a
        // property worth failing loudly on rather than resolving by precedence.
        return new SchemaFieldOrder(kind + " definition document", names);
    }

    /// <summary>Emits the declared fields <paramref name="value"/> carries, in declared order.</summary>
    private static void WriteFields(
        CanonicalJsonWriter writer,
        DefinitionShape shape,
        JsonElement value)
    {
        foreach (DefinitionField field in shape.Fields)
        {
            if (value.TryGetProperty(field.Name, out JsonElement property))
            {
                WriteField(writer, field, property);
            }
        }
    }

    private static void WriteField(
        CanonicalJsonWriter writer,
        DefinitionField field,
        JsonElement value)
    {
        switch (field.Shape)
        {
            case FieldShape.Text:
                writer.WriteString(field.Name, value.GetString()!);
                return;

            case FieldShape.Integer:
                writer.WriteInteger(field.Name, value.GetInt64());
                return;

            case FieldShape.Number:
                writer.WriteNumber(field.Name, value.GetDouble());
                return;

            case FieldShape.Flag:
                writer.WriteBoolean(field.Name, value.GetBoolean());
                return;

            case FieldShape.Object:
                writer.BeginObjectField(field.Name, field.Nested!.Order());
                WriteFields(writer, field.Nested, value);
                writer.EndObject();
                return;

            case FieldShape.ParameterMap:
                writer.WriteSortedDictionary(
                    field.Name,
                    EntriesOf(value),
                    static (target, entry) => WriteUndeclaredValue(target, entry));
                return;

            case FieldShape.Array:
                WriteArray(writer, field, value);
                return;

            default:
                throw new InvalidOperationException(
                    "field '" + field.Name + "' has shape " + field.Shape
                        + ", which has no canonical form");
        }
    }

    /// <summary>
    /// Emits a declared array under the class its declaration states, and never under a class
    /// chosen here.
    /// </summary>
    private static void WriteArray(
        CanonicalJsonWriter writer,
        DefinitionField field,
        JsonElement value)
    {
        DefinitionField element = field.Element!;

        if (element.Shape == FieldShape.Text)
        {
            List<string> texts = new();
            foreach (JsonElement item in value.EnumerateArray())
            {
                texts.Add(item.GetString()!);
            }

            ArrayOrderEmitter.WriteStrings(writer, field.Name, field.Order, texts);
            return;
        }

        List<JsonElement> items = new();
        foreach (JsonElement item in value.EnumerateArray())
        {
            items.Add(item);
        }

        ArrayOrderEmitter.WriteItems(
            writer,
            field.Name,
            field.Order,
            items,
            (target, item) => WriteElement(target, element, item));
    }

    private static void WriteElement(
        CanonicalJsonWriter writer,
        DefinitionField element,
        JsonElement value)
    {
        switch (element.Shape)
        {
            case FieldShape.Text:
                writer.WriteStringValue(value.GetString()!);
                return;

            case FieldShape.Integer:
                writer.WriteIntegerValue(value.GetInt64());
                return;

            case FieldShape.Number:
                writer.WriteNumberValue(value.GetDouble());
                return;

            case FieldShape.Flag:
                writer.WriteBooleanValue(value.GetBoolean());
                return;

            case FieldShape.Object:
                writer.BeginObjectValue(element.Nested!.Order());
                WriteFields(writer, element.Nested, value);
                writer.EndObject();
                return;

            default:
                throw new InvalidOperationException(
                    "an array element of shape " + element.Shape + " has no canonical form; "
                        + "DefinitionField.ElementOf and ElementObject declare scalar and "
                        + "object elements, and nothing else has a declared element table");
        }
    }

    /// <summary>
    /// Emits a value whose shape no field table declares: the value of a
    /// <see cref="FieldShape.ParameterMap"/> entry, or anything nested inside one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A parameter map's keys are owned by a registered descriptor rather than by a field
    /// table, so its values arrive with no declared shape, no declared field order, and - for a
    /// nested array - no declared <see cref="ArrayOrder"/>. Each of doc 40's rules is applied
    /// in the only way available without inventing a declaration: an object has no declared
    /// field order, so it is a dictionary and its keys are sorted; a number is normalized by
    /// <see cref="CanonicalNumber"/>; and an array is emitted in its authored order.
    /// </para>
    /// <para>
    /// <b>That last one is a gap, recorded rather than papered over.</b> Doc 40 grants two
    /// array treatments and both are properties of a declared array field, so neither is
    /// granted to a descriptor-owned array. Authored order is the treatment that asserts
    /// nothing about the elements, which is why it is the one chosen here; sorting would assert
    /// they are stable IDs. Six parameter maps are declared - <c>effects</c> and
    /// <c>magnitude</c> on branch, <c>effects</c> and <c>parameters</c> on relic,
    /// <c>parameters</c> on the weapon price formula, <c>fixed_properties</c> on weapon. They
    /// occur 123 times across the authored tree and the fixture corpus, and 20 of those
    /// occurrences contain a nested array - all 20 of them <c>effects</c> - measured at
    /// <c>8e45064</c>. So the gap is reached, not hypothetical.
    /// </para>
    /// </remarks>
    private static void WriteUndeclaredValue(CanonicalJsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString()!);
                return;

            case JsonValueKind.Number:
                WriteUndeclaredNumber(writer, value);
                return;

            case JsonValueKind.True:
            case JsonValueKind.False:
                writer.WriteBooleanValue(value.GetBoolean());
                return;

            case JsonValueKind.Object:
                writer.WriteSortedDictionaryValue(
                    "a descriptor-owned object",
                    EntriesOf(value),
                    static (target, entry) => WriteUndeclaredValue(target, entry));
                return;

            case JsonValueKind.Array:
                writer.WriteOrderedArrayValue(
                    ElementsOf(value),
                    static (target, item) => WriteUndeclaredValue(target, item));
                return;

            default:
                throw new InvalidOperationException(
                    "a descriptor-owned value of kind " + value.ValueKind + " has no canonical "
                        + "form; doc 40 makes a JSON null a codec error, so a validated "
                        + "definition never carries one");
        }
    }

    /// <summary>
    /// Writes an undeclared number as an integer when the source text is one, and as a
    /// round-trip double otherwise.
    /// </summary>
    /// <remarks>
    /// The source text decides, not the value. Doc 40 asks for "integers without padding" and
    /// "finite floating-point values with invariant round-trip representation", and with no
    /// declared shape to consult the only evidence of which the author meant is how they wrote
    /// it: <c>3</c> is an integer and <c>3.0</c> is a double whose round-trip form is
    /// <c>3</c>. Both reach the payload as <c>3</c>, which is normalization and not a loss -
    /// a canonical payload records the value, and the two are the same value.
    /// </remarks>
    private static void WriteUndeclaredNumber(CanonicalJsonWriter writer, JsonElement value)
    {
        string text = value.GetRawText();
        bool isIntegerText = text.IndexOfAny(['.', 'e', 'E']) < 0;

        if (isIntegerText && value.TryGetInt64(out long integer))
        {
            writer.WriteIntegerValue(integer);
            return;
        }

        writer.WriteNumberValue(value.GetDouble());
    }

    private static IEnumerable<KeyValuePair<string, JsonElement>> EntriesOf(JsonElement value)
    {
        List<KeyValuePair<string, JsonElement>> entries = new();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            entries.Add(new KeyValuePair<string, JsonElement>(property.Name, property.Value));
        }

        return entries;
    }

    private static IEnumerable<JsonElement> ElementsOf(JsonElement value)
    {
        List<JsonElement> items = new();
        foreach (JsonElement item in value.EnumerateArray())
        {
            items.Add(item);
        }

        return items;
    }
}
