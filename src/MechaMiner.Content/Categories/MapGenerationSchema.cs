namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-map-generation-contract</c>: the field table of <c>MGC-01</c>, the
/// standard map generation contract.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Map generation: "Fields
/// include mode/map ID, generation version, region/topology/scale ranges, static
/// obstacle targets, distance bands, site counts, distribution constraints, candidate
/// clearances, retry budgets, discovery settings, rock rules, and landmark pools.
/// Semantic validation checks internal feasibility before sampling maps."
/// </para>
/// <para>
/// <b><c>generation_version</c> is deliberately not declared, against doc 40's list.</b>
/// § Content compatibility makes the map-generation version part of build identity,
/// which is generated. Authoring it here would put a second writer on a value the
/// generator owns, and the authored copy would go stale the first time generation
/// semantics changed without anyone editing this file. This is the one requirement in
/// § Map generation that the schema declines, and it declines it in favour of another
/// section of the same document.
/// </para>
/// <para>
/// <b>An absent lower bound on a half-open band means unbounded below, not unknown.</b>
/// The Near band starts at the deployment point and has no floor. Doc 40 § Declared-
/// optional envelope fields makes absence the only way to say "no value", so the two
/// readings would collide if both were possible here - they are not, because a band
/// whose floor was unknown could not be sampled at all and would fail feasibility.
/// </para>
/// </remarks>
public static class MapGenerationSchema
{
    /// <summary>A three-valued scale range: two bounds and an authored target.</summary>
    public static DefinitionShape TargetRange { get; } = DefinitionShape.Of(
        "a target range",
        DefinitionField.Number("min"),
        DefinitionField.Number("max"),
        DefinitionField.Number("target"));

    /// <summary>A two-bound integer range.</summary>
    public static DefinitionShape IntegerRange { get; } = DefinitionShape.Of(
        "an integer range",
        DefinitionField.Integer("min"),
        DefinitionField.Integer("max"));

    /// <summary>A half-open band, exclusive below and inclusive above.</summary>
    /// <remarks>
    /// The exclusivity is in the key names because losing it loses a fact: two adjacent
    /// bands that were both inclusive would overlap at their shared boundary, and a
    /// point on that boundary would belong to both.
    /// </remarks>
    public static DefinitionShape HalfOpenMetreBand { get; } = DefinitionShape.Of(
        "a half-open distance band in mech collision diameters",
        DefinitionField.OptionalNumber("min_exclusive"),
        DefinitionField.Number("max_inclusive"));

    /// <summary>A half-open band measured in base travel seconds.</summary>
    public static DefinitionShape HalfOpenSecondBand { get; } = DefinitionShape.Of(
        "a half-open band in base travel seconds",
        DefinitionField.OptionalNumber("min_seconds_exclusive"),
        DefinitionField.Number("max_seconds_inclusive"));

    /// <summary>One distance band.</summary>
    public static DefinitionShape DistanceBand { get; } = DefinitionShape.Of(
        "a distance band",
        DefinitionField.Text("band"),
        DefinitionField.Object("route_distance_m", HalfOpenMetreBand),
        DefinitionField.Object("base_travel_time_from_deployment", HalfOpenSecondBand));

    /// <summary>The world-scale sub-shape.</summary>
    public static DefinitionShape WorldScale { get; } = DefinitionShape.Of(
        "the world scale",
        DefinitionField.Object("major_region_count", DefinitionShape.Of(
            "the major region count",
            DefinitionField.Integer("min"),
            DefinitionField.Integer("max"),
            DefinitionField.Integer("initial_target"))),
        DefinitionField.Object("traversable_diameter_m", TargetRange),
        DefinitionField.Object("traversable_diameter_base_travel_seconds", TargetRange),
        DefinitionField.Number("maximum_base_travel_from_deployment_to_important_location_seconds"),
        DefinitionField.Text("deployment_placement"),
        DefinitionField.Text("region_boundary_rule"),
        DefinitionField.Text("region_size_rule"),
        DefinitionField.Text("landmark_per_region"));

    /// <summary>The topology sub-shape.</summary>
    public static DefinitionShape Topology { get; } = DefinitionShape.Of(
        "the topology contract",
        DefinitionField.Object("redundant_major_routes", DefinitionShape.Of(
            "the redundant-route contract",
            DefinitionField.Integer("min_connected_major_regions_per_region"),
            DefinitionField.Flag("multiple_loops_required"),
            DefinitionField.Flag("single_connector_removal_cannot_isolate"),
            DefinitionField.Flag("compulsory_narrow_bridge_allowed"),
            DefinitionField.Object(
                "primary_connector_width_in_mining_zone_diameters",
                DefinitionShape.Of(
                    "the connector width",
                    DefinitionField.Number("min"),
                    DefinitionField.Number("target"))))),
        DefinitionField.Object("open_combat_ground", DefinitionShape.Of(
            "the open-combat-ground contract",
            DefinitionField.Object(
                "solid_obstacle_coverage_per_major_region_percent",
                DefinitionShape.Of(
                    "the obstacle coverage share",
                    DefinitionField.Object("initial_target", IntegerRange),
                    DefinitionField.Integer("maximum"),
                    DefinitionField.Text("excludes"))),
            DefinitionField.Number("obstacle_detour_target_seconds"),
            DefinitionField.Text("clustering_rule"),
            DefinitionField.Text("long_barrier_rule"),
            DefinitionField.Text("weapon_pattern_rule"))),
        DefinitionField.Object("optional_pockets", DefinitionShape.Of(
            "the optional-pocket contract",
            DefinitionField.Object("count_per_map", IntegerRange),
            DefinitionField.Number("maximum_one_way_depth_seconds"),
            DefinitionField.Integer("maximum_hyper_gold_sites_across_all_pockets"),
            DefinitionField.Integer("maximum_relic_caches_across_all_pockets"),
            DefinitionField.Flag("exit_readable_from_terminal_area"),
            DefinitionField.Flag("near_band_guarantees_depend_on_pockets"),
            DefinitionField.Text("terminal_area_rule"))));

    /// <summary>The deployment-fairness sub-shape.</summary>
    public static DefinitionShape DeploymentFairness { get; } = DefinitionShape.Of(
        "the deployment and opening fairness contract",
        DefinitionField.Number("obstacle_free_radius_in_mining_zone_diameters"),
        DefinitionField.Integer("min_distinct_broad_departure_routes"),
        DefinitionField.Integer("min_offscreen_enemy_entry_directions"),
        DefinitionField.Object(
            "nearest_standard_ore_seam_base_travel_seconds", IntegerRange),
        DefinitionField.Object("near_band_guarantees", DefinitionShape.Of(
            "the near-band guarantees",
            DefinitionField.Integer("standard_ore_seams_min"),
            DefinitionField.Integer("rich_ore_seams_min"),
            DefinitionField.Integer("geodes_per_present_material_min"))),
        DefinitionField.Flag("not_against_world_boundary"),
        DefinitionField.Flag("not_inside_narrow_connector"),
        DefinitionField.Flag("not_inside_spur_pocket"),
        DefinitionField.Flag("changes_every_run"),
        DefinitionField.Flag("directions_revealed_to_player"),
        DefinitionField.ArrayOf(
            "excluded_from_initial_camera_view", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The site-placement sub-shape.</summary>
    public static DefinitionShape SitePlacement { get; } = DefinitionShape.Of(
        "the site placement contract",
        DefinitionField.Object("specialized_material_geodes", DefinitionShape.Of(
            "geode placement",
            DefinitionField.Integer("present_materials"),
            DefinitionField.Object("geodes_per_present_material", IntegerRange),
            DefinitionField.Integer("min_in_near_band_per_material"),
            DefinitionField.Integer("min_major_regions_represented_per_material"),
            DefinitionField.Integer("maximum_same_material_geodes_per_major_region"),
            DefinitionField.Integer("maximum_share_of_all_geodes_per_major_region_percent"),
            DefinitionField.Flag("single_directional_cluster_allowed"),
            DefinitionField.Text("resonance_field_separation_evaluated_on"))),
        DefinitionField.Object("common_ore_seams", DefinitionShape.Of(
            "ore seam placement",
            DefinitionField.Integer("standard_seam_count"),
            DefinitionField.Integer("rich_seam_count"),
            DefinitionField.Integer("min_standard_seams_per_major_region"),
            DefinitionField.Integer("min_major_regions_with_rich_seams"),
            DefinitionField.Integer("maximum_rich_seams_per_major_region"),
            DefinitionField.Integer("maximum_share_of_all_ore_seams_per_major_region_percent"),
            DefinitionField.Integer("near_band_min_standard_seams"),
            DefinitionField.Integer("near_band_min_rich_seams"),
            DefinitionField.Flag("dominant_cluster_allowed"))),
        DefinitionField.Object("hyper_gold_sites", DefinitionShape.Of(
            "Hyper Gold site placement",
            DefinitionField.Integer("count"),
            DefinitionField.Integer("distinct_major_regions"),
            DefinitionField.Integer("min_middle_band_sites"),
            DefinitionField.Integer("min_far_band_sites"),
            DefinitionField.Integer("maximum_in_optional_spur_pocket"),
            DefinitionField.Number("min_separation_base_travel_seconds"),
            DefinitionField.Integer("total_site_based_hyper_gold"),
            DefinitionField.Flag("allowed_in_initial_camera_view"))),
        DefinitionField.Object("relic_caches", DefinitionShape.Of(
            "relic cache placement",
            DefinitionField.Integer("count"),
            DefinitionField.Integer("distinct_major_regions"),
            DefinitionField.Integer("min_middle_band_caches"),
            DefinitionField.Integer("min_far_band_caches"),
            DefinitionField.Integer("maximum_in_optional_spur_pocket"),
            DefinitionField.Number("min_separation_base_travel_seconds"),
            DefinitionField.Flag("allowed_in_initial_camera_view"),
            DefinitionField.Flag("dedicated_guard_package"),
            DefinitionField.Flag("global_through_fog_bearing"),
            DefinitionField.Text("discovery_rule"),
            DefinitionField.Text("relic_assignment"))));

    /// <summary>The destructible-rock sub-shape.</summary>
    public static DefinitionShape DestructibleRockRules { get; } = DefinitionShape.Of(
        "the destructible rock contract",
        DefinitionField.Integer("initial_count"),
        DefinitionField.Integer("active_maximum"),
        DefinitionField.Text("initial_placement"),
        DefinitionField.Object("valid_position_distance_from_mech_m", IntegerRange),
        DefinitionField.Number("min_m_beyond_visible_camera_rectangle_m"),
        DefinitionField.Number("replenishment_attempt_interval_seconds"),
        DefinitionField.Integer("replenishment_success_chance_percent"),
        DefinitionField.Text("success_behavior"),
        DefinitionField.ArrayOf("position_constraints", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Object("destructible_rock", DefinitionShape.Of(
            "the destructible rock prop",
            DefinitionField.Text("prop"),
            DefinitionField.Integer("hull"),
            DefinitionField.Integer("armor"),
            DefinitionField.Number("damage_footprint_diameter_m"),
            DefinitionField.Text("control_response"),
            DefinitionField.Text("movement_collision"),
            DefinitionField.Integer("health_pack_chance_percent"),
            DefinitionField.Flag("health_pack_chance_is_independent_per_rock"),
            DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)))),
        DefinitionField.Object("health_pack", DefinitionShape.Of(
            "the health pack prop",
            DefinitionField.Text("prop"),
            DefinitionField.Integer("repair_hull"),
            DefinitionField.Number("pickup_radius_m"),
            DefinitionField.Flag("can_exceed_maximum_hull"),
            DefinitionField.Flag("map_or_radar_marker"),
            DefinitionField.Text("attraction"),
            DefinitionField.Text("movement_collision"),
            DefinitionField.Text("persistence"),
            DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)))));

    /// <summary>The map generation field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "the standard map generation contract",
        DefinitionField.Text("mode"),
        DefinitionField.Text("distance_language"),
        DefinitionField.ArrayOf("distance_bands", DefinitionField.ElementObject(DistanceBand)),
        DefinitionField.Object("world_scale", WorldScale),
        DefinitionField.Object("topology", Topology),
        DefinitionField.Object("deployment_and_opening_fairness", DeploymentFairness),
        DefinitionField.ArrayOf(
            "shared_important_site_placement_contract",
            DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Object(
            "visible_mining_opportunities_in_normal_view",
            DefinitionShape.Of(
                "how many mining opportunities a normal view shows",
                DefinitionField.Integer("target_minimum"),
                DefinitionField.Integer("target_maximum"),
                DefinitionField.Integer("hard_maximum"))),
        DefinitionField.Object("site_placement", SitePlacement),
        DefinitionField.Object("destructible_rock_rules", DestructibleRockRules),
        DefinitionField.Object("landmarks_and_repetition", DefinitionShape.Of(
            "the landmark and repetition contract",
            DefinitionField.Integer("biomes_per_seed"),
            DefinitionField.Integer("maximum_appearances_of_one_authored_structure_per_map"),
            DefinitionField.Flag("adjacent_region_repetition_allowed"),
            DefinitionField.Flag("orientation_variation_allowed"),
            DefinitionField.Flag("landmark_independent_of_rewards"),
            DefinitionField.Flag("landmark_guarantees_adjacent_resource"),
            DefinitionField.Flag("structure_recognition_reveals_global_position"),
            DefinitionField.Flag("mandatory_environmental_damage_hazards"))),
        DefinitionField.ArrayOf(
            "variation_independence", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf(
            "boundary_and_fog_presentation", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Object("valid_seed_contract", DefinitionShape.Of(
            "what makes a seed valid",
            DefinitionField.Text("validation_distance_metric"),
            DefinitionField.ArrayOf(
                "invalid_if_violated", DefinitionField.ElementOf(FieldShape.Text)))));

    /// <summary>The values the compiler derives for the map contract.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "reference_mech_speed_m_per_s",
            "the player baseline's movement speed. The contract converts between route distance "
                + "and base travel time with it, so it reads the baseline rather than copying it",
            "PLAYER-01"),
        DerivedField.At(
            "generation_version",
            "the map-generation version is part of build identity, which is generated; the "
                + "generator owns it and an authored copy would be a second writer",
            "build identity"),
        DerivedField.Nested(
            new[] { "destructible_rock_rules", "health_pack", "collection_center_distance_with_standard_mech_circle_m" },
            "the health pack's pickup radius plus the player's collision radius",
            "/destructible_rock_rules/health_pack/pickup_radius_m", "PLAYER-01"),
        DerivedField.Nested(
            new[] { "destructible_rock_rules", "health_pack", "source" },
            "the destructible rock's health-pack chance and its independence per rock, which the "
                + "rock already states as two typed fields",
            "/destructible_rock_rules/destructible_rock/health_pack_chance_percent"),
        DerivedField.Nested(
            new[] { "deployment_and_opening_fairness", "obstacle_free_radius_m" },
            "obstacle_free_radius_in_mining_zone_diameters multiplied by twice a site's extraction "
                + "zone radius. The relative form is authored so the clearance tracks the zone; "
                + "the absolute is derived so runtime and every report read one unit",
            "/deployment_and_opening_fairness/obstacle_free_radius_in_mining_zone_diameters",
            "SITE-04"),
    });
}
