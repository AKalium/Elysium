# Time and economy integration

The feature branch now combines main's store, road placement, building footprints,
and demolition refunds with the clock and daily income/upkeep system.

CurrencySystem owns the single gameplay balance. DailyEconomy applies midnight
settlements to it; the store's TemporaryEconomy component forwards to it in
SampleScene. GameState and both HUDs read that same balance. The temporary wallet
retains standalone behavior for the separate store test scene.

Placement is the only purchase step. Selecting a store card does not spend money.
Cards display the mapped placement price; unmapped entries are unavailable.
SampleScene's current prices are house 100, tower 250, and road/wall 50. Demolition
refunds 50% of the original price once. House placement still requires an adjacent
road under main's existing road-placement rule.

The clock starts at 8 AM. A complete day takes 300 real seconds at 1x. The clock
button cycles 1x/2x/4x without accelerating camera controls or other game systems.
At midnight, each active GridPlaceable in the scene contributes according to its
category: Economic +60 income / 10 upkeep, Residential +25 / 5, Neither +0 / 2.
The original category assignments remain: house Economic, tower Residential,
road/wall Neither. Upkeep can create debt. Demolished/inactive buildings do not
contribute to future settlements. No save-game persistence is implemented.

The shop still contains entries without mapped placeable prefabs. Those are
marked unavailable rather than reporting a successful selection. Completing
those building assets is separate from this integration.

Verification: close Unity, then run the editor in batch mode with
`-batchmode -quit -projectPath <project> -executeMethod TimeEconomyVerification.Run -logFile <log>`.
This verifies shared balance, road-required placement, single purchase charges,
invalid placement, refunds, building counts, midnight settlements, pause, and
debt. Success logs `TIME_ECONOMY_VERIFICATION_PASSED`. The test never saves its
in-memory scene changes.
