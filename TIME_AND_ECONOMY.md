# Time and economy integration

CurrencySystem owns one gameplay balance, starting at 10000 in SampleScene.
The shop, CurrencyHUD, GameState, and midnight DailyEconomy settlements use it.
The obsolete TemporaryEconomy script has been removed, following main.

Selecting a shop card is free; successful placement charges once. Scene prices
are apartment 100, road 50, Mayor's Office 500, and market 200. Cards display
these mapped placement prices. All non-road buildings require an adjacent road.
Demolition refunds 50% of the original price once.

The clock starts at 8 AM; SampleScene currently sets a full day to 50 real seconds at 1x. The editable
ClockHUD prefab sits at top center. Speed remains controllable via TimeManager.
At midnight, active buildings contribute their BuildingData income/upkeep:
apartments 25/5, markets 60/10, roads and Mayor's Office 0/2. Purchase prices and
daily rates are stored together in each definition. Upkeep can create debt. Inactive/demolished
buildings do not contribute. No save-game persistence is implemented.

With Unity closed, run the editor with:
`-batchmode -quit -projectPath <project> -executeMethod ElysiumDemoBuild.ValidateDemo -logFile <log>`.
This checks demo scenes, missing scripts, graphics, shared balance, prices,
road adjacency, placement, refunds, population, midnight settlement, pause,
and debt. Success logs ELYSIUM_DEMO_VALIDATION_PASSED. In-memory test scene
changes are never saved. See CLOCK_AND_POPULATION.md for the UI handoff.

See ECONOMY_BUDGET.md for the daily budget panel, new-day earnings popup,
per-building financial configuration, and additional verification command.
