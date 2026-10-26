# Clock and population handoff

`Assets/UI/ClockHUD.prefab` is a linked instance in SampleScene. Double-click it
to edit the top-center panel, font, colors, spacing, or add other UI elements.
Its `TimeEconomyHUD` component only formats TimeManager's time into the assigned
Text label. Turn off Show Day for time alone. TimeManager still owns simulation
time; the clock display does not change economic behavior. The old currency,
cost, settlement, and speed-button overlays have been removed.

Population is currently a simple full-occupancy model: each active GridPlaceable
contributes its **Residents Provided** value. The placeable House A (1) prefab is
set to 1; non-housing buildings default to 0. Set this field on future house
prefabs rather than relying on object names or shop categories. GameState scans
the initial map, and building enable/disable and placement/demolition update the
count. The UI team can read `GameState.Population` and subscribe to
`PopulationChanged`. `MaxPopulation` is a compatibility alias for this count;
there is no separate migration or occupancy simulation yet.

There are currently two different category systems: the store uses Residential,
Commercial, and Services; the daily economy uses Neither, Residential, and
Economic. Individual placement purchase prices exist, but income and upkeep
still come from daily-economy category rates. Per-building income/upkeep and a
unified building definition are future work, not implemented in this UI change.

Happiness remains a placeholder. No job-count source or jobs-to-happiness formula
is implemented yet. Once defined, these can drive a separate UI meter without
changing the clock view.

City_Hall_Scaled.prefab references a missing parent prefab GUID
8411dd7fc7bfd9f48b2e10318c8b2a2e. The original prefab and its .meta file must be
restored, or a replacement prefab must be assembled and assigned deliberately.
Images uploaded to Drive do not themselves restore Unity prefab references.
