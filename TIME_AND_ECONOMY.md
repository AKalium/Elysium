# Sample scene time and economy

Open Assets/Scenes/SampleScene.unity and press Play. The top-right display shows the day, clock, balance, last settlement, and building costs. Keys 1/2/3 select house/tower/wall; right-click demolishes without a refund.

The day starts at 8 AM. A full day takes 300 real seconds at normal speed; the first midnight is 200 seconds after starting. Income and upkeep settle once at every midnight, including every crossed day in a long simulation step. No payout happens on startup.

| Building | Category | Purchase | Daily income | Daily upkeep |
| --- | --- | ---: | ---: | ---: |
| Wall | Neither | 10 | 0 | 2 |
| Tower | Residential | 100 | 25 | 5 |
| House A | Economic | 200 | 60 | 10 |

Starting balance is 1000. One of each costs 310 and earns a net 68 per midnight. Buildings active in this scene at midnight receive the full day's settlement, with no proration. Disabled/demolished buildings do not count. Upkeep can cause debt; unaffordable purchases fail without occupying cells or spending money.

Tune day duration, starting hour and speed on the scene's Time and Economy object. Set Paused from gameplay code or use Unity's time scale. Tune category rates and starting money on DailyEconomy. Assign categories on GridPlaceable prefabs; new buildings need no name-based logic. Midnight and BalanceChanged events support future systems and UI. The current HUD is a simple replaceable runtime display. Progress is session-only; saving and offline income are not implemented.
## Verification

With the project closed in Unity, run the installed Unity editor with `-batchmode -nographics -quit -projectPath <project-path> -executeMethod TimeEconomyVerification.Run -logFile <log-path>`. The scene verification checks purchases, invalid placement, midnight settlements, multiple days, pause, removed/inactive buildings, insufficient funds and debt. It modifies only the in-memory scene and does not save it. Success logs `TIME_ECONOMY_VERIFICATION_PASSED`.

During implementation, runtime and editor scripts compiled successfully against Unity 6000.6.2f1. Clock behavior passed a standalone harness using the actual TimeManager source with minimal Unity stubs. The full scene verification could not run because the project was already open in another editor process; visual/play-mode behavior remains to be checked.
