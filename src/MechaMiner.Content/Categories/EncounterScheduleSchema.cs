using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-encounter-schedule</c>: the field table of <c>WAV-01</c>, the
/// standard encounter schedule.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Encounter schedule: "One
/// aggregate standard schedule file contains mode ID, duration, minute rows,
/// composition weights, minimums, pulses, formations, boss warnings/arrivals, beacon
/// response table, and population ceilings. Aggregate validation compares 35 contiguous
/// rows, totals, earliest appearance, boss cadence, formation grammar, and accepted
/// enemy IDs."
/// </para>
/// <para>
/// It is an aggregate: players never read its name and it is not embodied in the world,
/// so it omits <c>name_key</c> and <c>presentation_id</c> under § Declared-optional
/// envelope fields.
/// </para>
/// <para>
/// <b>The schedule is the single writer on arrival timing.</b> The fifteen-second boss
/// warning appeared four times - once here, once per boss, once per boss as a
/// presentation string, and once per minute row as a pre-subtracted warning timecode.
/// It lives here once; the boss keeps its arrival timecode, and the warning timecode is
/// derived.
/// </para>
/// </remarks>
public static class EncounterScheduleSchema
{
    /// <summary>The accepted mode tokens.</summary>
    public static ClosedVocabulary Modes { get; } = new(
        "a run mode", "GDD-WAVE-SCHEDULE", "standard");

    /// <summary>The seven accepted spawn formations.</summary>
    /// <remarks>
    /// <c>docs/32-standard-wave-and-beacon-schedule.md</c> § Formation grammar. The
    /// tokens are the authored names retokenized; the schedule's own
    /// <c>spawn_formations</c> block defines each one, and every formation a minute row
    /// names must be one of them.
    /// </remarks>
    public static ClosedVocabulary Formations { get; } = new(
        "a spawn formation",
        "GDD-WAVE-SCHEDULE",
        "stream",
        "wall",
        "swarm",
        "twin-flanks",
        "encirclement",
        "convergence",
        "rolling-ring");

    /// <summary>Whether a formation event's timestamps are authored or reconstructed.</summary>
    /// <remarks>
    /// One minute row's timestamps were reconstructed rather than transcribed, and the
    /// integration owner requires that to stay legible in the data: a consumer that
    /// trusts the timestamps without reading this field is reading provisional numbers
    /// as accepted ones. The authored form carried the same fact twice, as a boolean and
    /// as this token; the token stays because it has room for a third state and the
    /// boolean does not.
    /// </remarks>
    public static ClosedVocabulary TimestampProvenances { get; } = new(
        "a formation event's timestamp provenance",
        "TDD-CONTENT-DATA",
        "authored",
        "reconstructed");

    /// <summary>One minute row's composition entry.</summary>
    public static DefinitionShape CompositionEntry { get; } = DefinitionShape.Of(
        "a composition entry",
        DefinitionField.Text("enemy_id"),
        DefinitionField.Integer("share_percent"));

    /// <summary>One authored formation event within a minute.</summary>
    public static DefinitionShape FormationEvent { get; } = DefinitionShape.Of(
        "a formation event",
        DefinitionField.Text("authored_cell_text"),
        DefinitionField.ArrayOf("at", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf("enemy_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf("formations", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalText("timestamp_provenance"),
        DefinitionField.OptionalText("reconstruction_basis"));

    /// <summary>One scheduled elite within a minute.</summary>
    public static DefinitionShape ScheduledElite { get; } = DefinitionShape.Of(
        "a scheduled elite",
        DefinitionField.Text("at"),
        DefinitionField.Text("enemy_id"),
        DefinitionField.Integer("count"));

    /// <summary>The pulse sub-shape.</summary>
    public static DefinitionShape Pulse { get; } = DefinitionShape.Of(
        "a minute's replenishment pulse",
        DefinitionField.Integer("batch_count"),
        DefinitionField.Number("interval_seconds"));

    /// <summary>One minute row.</summary>
    public static DefinitionShape MinuteRow { get; } = DefinitionShape.Of(
        "a minute row",
        DefinitionField.Integer("minute"),
        DefinitionField.Text("authored_event_or_boundary"),
        DefinitionField.Integer("minimum_count"),
        DefinitionField.Object("pulse", Pulse),
        DefinitionField.ArrayOf("composition", DefinitionField.ElementObject(CompositionEntry)),
        DefinitionField.ArrayOf("debut_enemy_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf(
            "formation_events", DefinitionField.ElementObject(FormationEvent)),
        DefinitionField.ArrayOf(
            "scheduled_elites", DefinitionField.ElementObject(ScheduledElite)),
        DefinitionField.OptionalText("boss_arrival_boss_id"));

    /// <summary>One beacon response package.</summary>
    public static DefinitionShape BeaconResponse { get; } = DefinitionShape.Of(
        "a beacon response package",
        DefinitionField.Text("trigger_kind"),
        DefinitionField.OptionalInteger("trigger_progress_percent"),
        DefinitionField.Text("formation"),
        DefinitionField.Integer("floor_count"),
        DefinitionField.Integer("share_percent"),
        DefinitionField.OptionalText("elite_addition"));

    /// <summary>The Hyper Gold beacon response table.</summary>
    public static DefinitionShape BeaconResponseTable { get; } = DefinitionShape.Of(
        "the Hyper Gold beacon response table",
        DefinitionField.Text("trigger_rule"),
        DefinitionField.Integer("warning_seconds"),
        DefinitionField.Text("population_symbol"),
        DefinitionField.Text("elite_exclusion_enemy_id"),
        DefinitionField.ArrayOf("responses", DefinitionField.ElementObject(BeaconResponse)),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The population ceiling sub-shape.</summary>
    public static DefinitionShape PopulationCeilings { get; } = DefinitionShape.Of(
        "the population ceilings",
        DefinitionField.Integer("baseline_ordinary_count"),
        DefinitionField.Integer("persistent_beacon_tagged_count"),
        DefinitionField.Integer("scheduled_event_overflow_count"),
        DefinitionField.Flag("bosses_count_toward_ceilings"),
        DefinitionField.Text("overflow_rule"));

    /// <summary>The boss-arrival minute rule sub-shape.</summary>
    public static DefinitionShape BossArrivalMinuteRule { get; } = DefinitionShape.Of(
        "how a boss arrival changes its minute",
        DefinitionField.Flag("additional_scheduled_formation"),
        DefinitionField.Object(
            "minimum_reduction_from_preceding_minute_percent",
            DefinitionShape.Of(
                "the reduction band",
                DefinitionField.Integer("min"),
                DefinitionField.Integer("max"))),
        DefinitionField.Text("statement"));

    /// <summary>One phase of the authored pressure curve.</summary>
    public static DefinitionShape PhasePressure { get; } = DefinitionShape.Of(
        "a pressure-curve phase",
        DefinitionField.Text("start_at"),
        DefinitionField.Text("end_at"),
        DefinitionField.Text("purpose"),
        DefinitionField.Text("reference_style_pressure_translation"));

    /// <summary>One formation's definition.</summary>
    public static DefinitionShape FormationDefinition { get; } = DefinitionShape.Of(
        "a formation definition",
        DefinitionField.Text("formation"),
        DefinitionField.Text("definition"));

    /// <summary>The schedule field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "the standard encounter schedule",
        DefinitionField.Text("mode"),
        DefinitionField.Integer("duration_minutes"),
        DefinitionField.Text("extraction_at"),
        DefinitionField.Text("extraction_rule"),
        DefinitionField.Integer("boss_arrival_warning_seconds"),
        DefinitionField.Object("population_ceilings", PopulationCeilings),
        DefinitionField.Object("boss_arrival_minute_rule", BossArrivalMinuteRule),
        DefinitionField.ArrayOf(
            "spawn_formations", DefinitionField.ElementObject(FormationDefinition)),
        DefinitionField.ArrayOf("formation_constraints", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf(
            "phase_pressure_curve", DefinitionField.ElementObject(PhasePressure)),
        DefinitionField.ArrayOf("minute_rows", DefinitionField.ElementObject(MinuteRow)),
        DefinitionField.Object("hyper_gold_beacon_response", BeaconResponseTable));

    /// <summary>The values the compiler derives for the schedule.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "mode_id",
            "the schedule's mode is carried by 'mode'; no accepted document mints a separate mode "
                + "ID, and a second identifier for one concept is a second writer on it",
            "/mode"),
        DerivedField.At(
            "director_vocabulary",
            "the glossary defines this schema's own field names, so it belongs in the schema's "
                + "documentation rather than in an instance of it"),
    });
}
