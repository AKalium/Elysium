# Clock and population handoff

`Assets/UI/ClockHUD.prefab` is a linked instance in SampleScene. Double-click it
to edit the top-center panel, font, colors, spacing, or add other UI elements.
TimeEconomyHUD formats TimeManager's time into the assigned Text label. Turn off
Show Day for time alone. The UI lead's shop and currency display remain connected.

Each active GridPlaceable contributes its **Residents Provided** value. The
apartment prefab (`Assets/Prefabs/apt.prefab`) provides 10 residents, matching the
latest main branch's apartment population. Other buildings provide 0. Change
this field on housing prefabs to tune population. Preplaced houses and placement/
demolition are counted. The UI team can read GameState.Population and subscribe
to PopulationChanged. MaxPopulation is currently an alias for full occupancy;
there is no migration simulation yet.

Shop categories are Residential, Commercial, and Services. Daily economy categories
are Neither, Residential, and Economic. Apartments are Residential for daily
settlements; markets are Economic; roads and the Mayor's Office are Neither.
Per-building purchase prices exist; daily income/upkeep still use category rates.
Happiness and jobs remain unimplemented placeholders.

Main now supplies apartment, market, road, and Mayor's Office sprites and prefabs
under Assets/Sprites and Assets/Prefabs. SampleScene maps the CityHall shop entry
to the new Mayor's Office prefab, replacing the previously missing source prefab.
