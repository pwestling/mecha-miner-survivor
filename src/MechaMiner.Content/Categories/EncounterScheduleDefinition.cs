using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>The validated standard encounter schedule.</summary>
public sealed class EncounterScheduleDefinition : ContentDefinition
{
    internal EncounterScheduleDefinition(
        DefinitionEnvelope envelope,
        string mode,
        long durationMinutes,
        long bossArrivalWarningSeconds,
        IReadOnlyList<long> minutes)
        : base(envelope, DefinitionKind.EncounterSchedule)
    {
        Mode = mode;
        DurationMinutes = durationMinutes;
        BossArrivalWarningSeconds = bossArrivalWarningSeconds;
        Minutes = minutes;
    }

    /// <summary>The run mode this schedule governs.</summary>
    public string Mode { get; }

    /// <summary>How many minutes the run lasts.</summary>
    public long DurationMinutes { get; }

    /// <summary>
    /// How long before a boss arrives the warning fires. The schedule owns arrival
    /// timing, so this value lives here once.
    /// </summary>
    public long BossArrivalWarningSeconds { get; }

    /// <summary>The minute each row governs, in row order.</summary>
    public IReadOnlyList<long> Minutes { get; }
}

/// <summary>The wire shape of the encounter schedule's domain fields.</summary>
internal sealed class EncounterScheduleDto
{
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("duration_minutes")]
    public double? DurationMinutes { get; set; }

    [JsonPropertyName("boss_arrival_warning_seconds")]
    public double? BossArrivalWarningSeconds { get; set; }

    [JsonPropertyName("spawn_formations")]
    public List<FormationDefinitionDto>? SpawnFormations { get; set; }

    [JsonPropertyName("minute_rows")]
    public List<MinuteRowDto>? MinuteRows { get; set; }

    [JsonPropertyName("hyper_gold_beacon_response")]
    public BeaconResponseTableDto? HyperGoldBeaconResponse { get; set; }

    internal sealed class FormationDefinitionDto
    {
        [JsonPropertyName("formation")]
        public string? Formation { get; set; }
    }

    internal sealed class MinuteRowDto
    {
        [JsonPropertyName("minute")]
        public double? Minute { get; set; }

        [JsonPropertyName("minimum_count")]
        public double? MinimumCount { get; set; }

        [JsonPropertyName("pulse")]
        public PulseDto? Pulse { get; set; }

        [JsonPropertyName("composition")]
        public List<CompositionEntryDto>? Composition { get; set; }

        [JsonPropertyName("debut_enemy_ids")]
        public List<string>? DebutEnemyIds { get; set; }

        [JsonPropertyName("formation_events")]
        public List<FormationEventDto>? FormationEvents { get; set; }

        [JsonPropertyName("scheduled_elites")]
        public List<ScheduledEliteDto>? ScheduledElites { get; set; }

        [JsonPropertyName("boss_arrival_boss_id")]
        public string? BossArrivalBossId { get; set; }
    }

    internal sealed class PulseDto
    {
        [JsonPropertyName("batch_count")]
        public double? BatchCount { get; set; }

        [JsonPropertyName("interval_seconds")]
        public double? IntervalSeconds { get; set; }
    }

    internal sealed class CompositionEntryDto
    {
        [JsonPropertyName("enemy_id")]
        public string? EnemyId { get; set; }

        [JsonPropertyName("share_percent")]
        public double? SharePercent { get; set; }
    }

    internal sealed class FormationEventDto
    {
        [JsonPropertyName("enemy_ids")]
        public List<string>? EnemyIds { get; set; }

        [JsonPropertyName("formations")]
        public List<string>? Formations { get; set; }

        [JsonPropertyName("timestamp_provenance")]
        public string? TimestampProvenance { get; set; }
    }

    internal sealed class ScheduledEliteDto
    {
        [JsonPropertyName("enemy_id")]
        public string? EnemyId { get; set; }

        [JsonPropertyName("count")]
        public double? Count { get; set; }
    }

    internal sealed class BeaconResponseTableDto
    {
        [JsonPropertyName("elite_exclusion_enemy_id")]
        public string? EliteExclusionEnemyId { get; set; }

        [JsonPropertyName("responses")]
        public List<BeaconResponseDto>? Responses { get; set; }
    }

    internal sealed class BeaconResponseDto
    {
        [JsonPropertyName("trigger_kind")]
        public string? TriggerKind { get; set; }

        [JsonPropertyName("trigger_progress_percent")]
        public double? TriggerProgressPercent { get; set; }

        [JsonPropertyName("formation")]
        public string? Formation { get; set; }

        [JsonPropertyName("floor_count")]
        public double? FloorCount { get; set; }

        [JsonPropertyName("share_percent")]
        public double? SharePercent { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="EncounterScheduleDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(EncounterScheduleDto))]
internal sealed partial class EncounterScheduleJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates the standard encounter schedule.</summary>
public static class EncounterScheduleReader
{
    /// <summary>Reads the schedule.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope, out string? id,
                out DocumentOutline _, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        EncounterScheduleDto? dto = JsonSerializer.Deserialize(
            utf8, EncounterScheduleJsonContext.Default.EncounterScheduleDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        List<long> minutes = Validate(dto, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        EncounterScheduleDefinition definition = new(
            envelope,
            dto.Mode!,
            (long)dto.DurationMinutes!.Value,
            (long)dto.BossArrivalWarningSeconds!.Value,
            minutes);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static List<long> Validate(
        EncounterScheduleDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.Mode, EncounterScheduleSchema.Modes, root.AppendProperty("mode"), context, id, bag);

        SemanticCheck.Integer(
            dto.DurationMinutes, root.AppendProperty("duration_minutes"), context, id, bag,
            "duration_minutes");
        SemanticCheck.AtLeast(
            dto.DurationMinutes, 1, root.AppendProperty("duration_minutes"), context, id, bag,
            "duration_minutes is at least one");

        SemanticCheck.Integer(
            dto.BossArrivalWarningSeconds, root.AppendProperty("boss_arrival_warning_seconds"),
            context, id, bag, "boss_arrival_warning_seconds");
        SemanticCheck.AtLeast(
            dto.BossArrivalWarningSeconds, 0, root.AppendProperty("boss_arrival_warning_seconds"),
            context, id, bag,
            "boss_arrival_warning_seconds is a duration and durations are nonnegative");

        ValidateFormationDefinitions(dto, context, id, bag);
        List<long> minutes = ValidateMinuteRows(dto, context, id, bag);
        ValidateBeaconResponses(dto, context, id, bag);
        return minutes;
    }

    private static void ValidateFormationDefinitions(
        EncounterScheduleDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<EncounterScheduleDto.FormationDefinitionDto> formations =
            dto.SpawnFormations ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("spawn_formations");
        List<string> tokens = new(formations.Count);

        for (int index = 0; index < formations.Count; index++)
        {
            string token = formations[index].Formation ?? string.Empty;
            tokens.Add(token);
            SemanticCheck.Token(
                formations[index].Formation, EncounterScheduleSchema.Formations,
                pointer.AppendIndex(index).AppendProperty("formation"), context, id, bag);
        }

        SemanticCheck.Distinct(
            tokens, pointer, context, id, bag, "the formations the schedule defines");
        SemanticCheck.ExactCount(
            formations.Count, EncounterScheduleSchema.Formations.Tokens.Count, pointer, context,
            id, bag,
            "spawn_formations defines every formation in the closed vocabulary exactly once, so a "
                + "minute row can never name a formation the schedule has not defined");
    }

    private static List<long> ValidateMinuteRows(
        EncounterScheduleDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<EncounterScheduleDto.MinuteRowDto> rows = dto.MinuteRows ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("minute_rows");
        List<long> minutes = new(rows.Count);

        long duration = dto.DurationMinutes is null ? rows.Count : (long)dto.DurationMinutes.Value;
        SemanticCheck.ExactCount(
            rows.Count, (int)duration, pointer, context, id, bag,
            "minute_rows holds one row per minute of the run, so its length equals "
                + "duration_minutes; the two are checked against each other rather than both "
                + "against a constant, so raising the duration without adding rows fails");

        for (int index = 0; index < rows.Count; index++)
        {
            EncounterScheduleDto.MinuteRowDto row = rows[index];
            JsonPointer rowPointer = pointer.AppendIndex(index);

            minutes.Add(SemanticCheck.Integer(
                row.Minute, rowPointer.AppendProperty("minute"), context, id, bag, "minute"));

            SemanticCheck.Integer(
                row.MinimumCount, rowPointer.AppendProperty("minimum_count"), context, id, bag,
                "minimum_count");
            SemanticCheck.AtLeast(
                row.MinimumCount, 0, rowPointer.AppendProperty("minimum_count"), context, id, bag,
                "minimum_count is a desired live population and is nonnegative");

            ValidatePulse(row.Pulse, rowPointer, context, id, bag);
            ValidateComposition(row.Composition, rowPointer, context, id, bag);
            ValidateDebuts(row.DebutEnemyIds, rowPointer, context, id, bag);
            ValidateFormationEvents(row.FormationEvents, rowPointer, context, id, bag);
            ValidateScheduledElites(row.ScheduledElites, rowPointer, context, id, bag);

            if (row.BossArrivalBossId is not null)
            {
                SemanticCheck.ReferenceGrammar(
                    row.BossArrivalBossId, ContentCategory.Boss,
                    rowPointer.AppendProperty("boss_arrival_boss_id"), context, id, bag);
            }
        }

        SemanticCheck.Contiguous(
            minutes, 0, pointer, context, id, bag, "the schedule's minute numbers");
        return minutes;
    }

    private static void ValidatePulse(
        EncounterScheduleDto.PulseDto? pulse,
        JsonPointer rowPointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (pulse is null)
        {
            return;
        }

        JsonPointer pointer = rowPointer.AppendProperty("pulse");
        SemanticCheck.Integer(
            pulse.BatchCount, pointer.AppendProperty("batch_count"), context, id, bag,
            "batch_count");
        SemanticCheck.AtLeast(
            pulse.BatchCount, 1, pointer.AppendProperty("batch_count"), context, id, bag,
            "batch_count is the number of enemies one pulse places and is at least one");
        SemanticCheck.GreaterThan(
            pulse.IntervalSeconds, 0, pointer.AppendProperty("interval_seconds"), context, id, bag,
            "interval_seconds is strictly positive: it is the interval between pulses, and a zero "
                + "interval is a single instantaneous placement of the whole minute's population "
                + "rather than a pulse rate");
    }

    private static void ValidateComposition(
        List<EncounterScheduleDto.CompositionEntryDto>? composition,
        JsonPointer rowPointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<EncounterScheduleDto.CompositionEntryDto> entries = composition ?? new();
        JsonPointer pointer = rowPointer.AppendProperty("composition");

        long total = 0;
        List<string> enemyIds = new(entries.Count);
        for (int index = 0; index < entries.Count; index++)
        {
            EncounterScheduleDto.CompositionEntryDto entry = entries[index];
            JsonPointer entryPointer = pointer.AppendIndex(index);

            enemyIds.Add(entry.EnemyId ?? string.Empty);
            SemanticCheck.ReferenceGrammar(
                entry.EnemyId, ContentCategory.Enemy, entryPointer.AppendProperty("enemy_id"),
                context, id, bag);

            total += SemanticCheck.Integer(
                entry.SharePercent, entryPointer.AppendProperty("share_percent"), context, id, bag,
                "share_percent");
            SemanticCheck.Within(
                entry.SharePercent, 1, 100, entryPointer.AppendProperty("share_percent"), context,
                id, bag,
                "share_percent is a whole percentage point share of the minute's replenishment; "
                    + "an entry with a zero share is the absence of the entry");
        }

        SemanticCheck.Distinct(
            enemyIds, pointer, context, id, bag, "a minute row's composition enemy IDs");

        if (entries.Count > 0)
        {
            SemanticCheck.SumEquals(
                total, 100, pointer, context, id, bag,
                "a minute row's composition shares. The sum is over this row only and uses "
                    + "integer arithmetic, because shares are authored as whole percentage points "
                    + "and no tolerance is involved");
        }
    }

    private static void ValidateDebuts(
        List<string>? debuts,
        JsonPointer rowPointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<string> ids = debuts ?? new();
        JsonPointer pointer = rowPointer.AppendProperty("debut_enemy_ids");
        for (int index = 0; index < ids.Count; index++)
        {
            SemanticCheck.ReferenceGrammar(
                ids[index], ContentCategory.Enemy, pointer.AppendIndex(index), context, id, bag);
        }

        SemanticCheck.Distinct(ids, pointer, context, id, bag, "a minute row's debut enemy IDs");
    }

    private static void ValidateFormationEvents(
        List<EncounterScheduleDto.FormationEventDto>? events,
        JsonPointer rowPointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<EncounterScheduleDto.FormationEventDto> list = events ?? new();
        JsonPointer pointer = rowPointer.AppendProperty("formation_events");

        for (int index = 0; index < list.Count; index++)
        {
            EncounterScheduleDto.FormationEventDto entry = list[index];
            JsonPointer entryPointer = pointer.AppendIndex(index);

            List<string> formations = entry.Formations ?? new();
            for (int inner = 0; inner < formations.Count; inner++)
            {
                SemanticCheck.Token(
                    formations[inner], EncounterScheduleSchema.Formations,
                    entryPointer.AppendProperty("formations").AppendIndex(inner), context, id, bag);
            }

            List<string> enemyIds = entry.EnemyIds ?? new();
            for (int inner = 0; inner < enemyIds.Count; inner++)
            {
                SemanticCheck.ReferenceGrammar(
                    enemyIds[inner], ContentCategory.Enemy,
                    entryPointer.AppendProperty("enemy_ids").AppendIndex(inner), context, id, bag);
            }

            if (entry.TimestampProvenance is not null)
            {
                SemanticCheck.Token(
                    entry.TimestampProvenance, EncounterScheduleSchema.TimestampProvenances,
                    entryPointer.AppendProperty("timestamp_provenance"), context, id, bag);
            }
        }
    }

    private static void ValidateScheduledElites(
        List<EncounterScheduleDto.ScheduledEliteDto>? elites,
        JsonPointer rowPointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<EncounterScheduleDto.ScheduledEliteDto> list = elites ?? new();
        JsonPointer pointer = rowPointer.AppendProperty("scheduled_elites");

        for (int index = 0; index < list.Count; index++)
        {
            JsonPointer entryPointer = pointer.AppendIndex(index);
            SemanticCheck.ReferenceGrammar(
                list[index].EnemyId, ContentCategory.Enemy,
                entryPointer.AppendProperty("enemy_id"), context, id, bag);
            SemanticCheck.Integer(
                list[index].Count, entryPointer.AppendProperty("count"), context, id, bag, "count");
            SemanticCheck.AtLeast(
                list[index].Count, 1, entryPointer.AppendProperty("count"), context, id, bag,
                "a scheduled elite entry places at least one elite");
        }
    }

    private static void ValidateBeaconResponses(
        EncounterScheduleDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        EncounterScheduleDto.BeaconResponseTableDto? table = dto.HyperGoldBeaconResponse;
        if (table is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("hyper_gold_beacon_response");
        SemanticCheck.ReferenceGrammar(
            table.EliteExclusionEnemyId, ContentCategory.Enemy,
            pointer.AppendProperty("elite_exclusion_enemy_id"), context, id, bag);

        List<EncounterScheduleDto.BeaconResponseDto> responses = table.Responses ?? new();
        JsonPointer responsesPointer = pointer.AppendProperty("responses");
        for (int index = 0; index < responses.Count; index++)
        {
            EncounterScheduleDto.BeaconResponseDto response = responses[index];
            JsonPointer row = responsesPointer.AppendIndex(index);

            SemanticCheck.Token(
                response.TriggerKind, MiningSiteSchema.BeaconTriggerKinds,
                row.AppendProperty("trigger_kind"), context, id, bag);
            SemanticCheck.Token(
                response.Formation, EncounterScheduleSchema.Formations,
                row.AppendProperty("formation"), context, id, bag);

            SemanticCheck.Integer(
                response.FloorCount, row.AppendProperty("floor_count"), context, id, bag,
                "floor_count");
            SemanticCheck.AtLeast(
                response.FloorCount, 1, row.AppendProperty("floor_count"), context, id, bag,
                "floor_count is the minimum response size and is at least one; it and "
                    + "share_percent are the two operands of the response size, which is the "
                    + "larger of them and replaces the authored expression string");

            SemanticCheck.Integer(
                response.SharePercent, row.AppendProperty("share_percent"), context, id, bag,
                "share_percent");
            SemanticCheck.Within(
                response.SharePercent, 1, 100, row.AppendProperty("share_percent"), context, id,
                bag,
                "share_percent is a share of the current minute's desired minimum population");

            bool isProgress = string.Equals(
                response.TriggerKind, "progress-threshold", StringComparison.Ordinal);
            if (isProgress && response.TriggerProgressPercent is null)
            {
                SemanticCheck.RequiredBy(
                    row.AppendProperty("trigger_progress_percent"), context, id, bag,
                    "a progress-threshold response states the progress it fires at");
            }

            if (!isProgress && response.TriggerProgressPercent is not null)
            {
                SemanticCheck.ForbiddenBy(
                    row.AppendProperty("trigger_progress_percent"), context, id, bag,
                    "the activation response fires at the first progress of any amount and has no "
                        + "threshold");
            }
        }
    }
}
