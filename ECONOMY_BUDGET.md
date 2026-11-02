# Per-building finances and daily reports

Open `Assets/Scenes/SampleScene.unity` in Unity 6000.6.2f1. The **CITY BUDGET**
button toggles the budget panel. At midnight, the shared wallet receives the
daily settlement and a report opens for the new day (first report: Day 2).
It shows gross income, upkeep, net earnings or loss, opening/closing balance,
and totals for each building type. Continue dismisses it. Time continues while
the report is open; additional days queue separate reports in order.

The panel shows the last settlement and the next settlement's projected
income, upkeep, net, and balance. Forecasts assume the current active buildings
remain in place. Construction, demolition, and definition edits update the
forecast; previous reports remain immutable snapshots. Income is assessed at
midnight, without prorating buildings constructed partway through the day.

## Tune finances

Edit the BuildingData assets under `Assets/ShopUI/BuildingData`. Each contains
the purchase price, daily income, and daily upkeep. Its matching placeable
prefab references the same definition through GridPlaceable.Definition.
The shop, construction charges, forecasts and settlements read these assets;
there is no second category-rate table or scene construction-price array.

| Definition | Price | Income/day | Upkeep/day | Net/day |
| --- | ---: | ---: | ---: | ---: |
| Apartment | 100 | 25 | 5 | 20 |
| Road | 50 | 0 | 2 | -2 |
| CityHall (Mayor's Office) | 500 | 0 | 2 | -2 |
| Market | 200 | 60 | 10 | 50 |

Selecting a card is free. Successful placement charges once. Demolition refunds
50% of the amount actually paid, rounded down, even if the definition's price
has changed. Inactive or demolished buildings do not contribute to settlements.
The shop displays income, upkeep, and net per day before purchase. Unsupported
shop entries remain unavailable.

## Edit the UI

`Assets/UI/EconomyBudget.prefab` is linked in SampleScene. Its budget panel,
button, modal report, scrollable detail text and Continue button are editable.
The full-screen report overlay intercepts pointer clicks to protect the map.
The setup command **Elysium > Set Up Economy Budget** regenerates this prefab
and its scene instance; use it only when deliberately resetting the layout.

## Verify

With Unity closed:

```text
Unity.exe -batchmode -nographics -quit -projectPath <project> -executeMethod EconomyBudgetVerification.Run -logFile <log>
```

Success logs `TIME_ECONOMY_VERIFICATION_PASSED` and
`ECONOMY_BUDGET_VERIFICATION_PASSED`. Checks cover price/rate edits flowing from
one definition, purchase and refund totals, midnight timing, forecast accuracy,
demolition exclusion, immutable reports, report queuing, listener lifecycle,
losses, an empty city, debt, speed controls, and the existing road rules.
Verification changes remain in memory and original asset values are restored.
