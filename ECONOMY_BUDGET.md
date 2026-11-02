# Per-building finances and daily reports

A free road tile is created at the grid's center when gameplay starts. Extend
roads from this tile: buildings must border a road with a continuous horizontal
or vertical connection back to the center. Diagonal contact does not connect
roads. Separate road islands are allowed, but cannot support buildings until
joined to the central network. Building previews and purchases use this rule.
The starting tile has normal road upkeep, but no construction charge or refund.

Open `Assets/Scenes/SampleScene.unity` in Unity 6000.6.2f1. The **CITY BUDGET**
button toggles the budget panel. At midnight, the shared wallet receives the
daily settlement and a report opens for the new day (first report: Day 2).
It shows gross income, upkeep, net earnings or loss, opening/closing balance,
and totals for each building type. The game pauses while the report is open.
Continue dismisses it and restores the previous simulation pause/speed state.
Queued reports keep the game paused until the last report is dismissed.
Disabling the report UI also releases its pause. A long frame stops at the
first midnight report rather than advancing through additional days.

Drag the budget window by its title bar to move it. The window stays inside the
canvas. Its X button closes it; CITY BUDGET reopens it in the same position.
Moving or closing the budget window does not pause the game.

Select Road, then hold the left mouse button and drag across the map to paint
roads. Fast and diagonal strokes fill the intervening tiles with a connected
path. Existing or blocked tiles are skipped and only new roads are charged.
Release the mouse to finish. Entering UI, leaving the map, pausing, changing
selection or losing focus cancels the stroke; start again with a fresh click.
Other buildings still require individual clicks. Dragging cannot spend beyond
the available balance.

Hold the right mouse button and drag to demolish sections of road. Skipped
tiles between mouse positions are included. A right-click can still demolish
an individual building; dragging subsequently targets roads only. Roads can
be erased without selecting Road in the shop. UI, pause, focus loss, leaving
the map, releasing the button, and changing selection end the demolition stroke.

Removing a road recalculates the whole central network and automatically
demolishes buildings with no connected road alongside their full footprint,
including distant buildings cut off by a severed route. Buildings remain if
another route still reaches the center. Disconnected road tiles stay on the map
and can be reconnected. The center tile can also be demolished; this removes
road access for the whole city. Rebuilding a road at the same center cell
restores the network, and that replacement costs the normal road price.

## Road indicators and demolition preview

Connected roads are tinted teal. Disconnected road islands are orange and have
X marks. The center is gold with a diamond and a label; if its road is removed,
the marker remains and tells you where to rebuild. The road legend sits above
the time/speed panel in the lower-left corner.
The legend blocks map actions beneath it, while floating labels and the preview
tooltip do not intercept clicks or interfere with existing UI controls.

Hold **Shift** and hover a road or building to preview demolition before
right-clicking. Red outlines and translucent fills mark the entire footprints
of objects that would disappear, including distant buildings cut off from the
central network. The tooltip shows road/building counts and the exact refund,
and warns when removing the central road. Other disconnected roads that would
remain are not marked as demolished. Alternate routes are included in the
calculation. Release Shift, move over UI, or pause to clear the preview.

Previewing is read-only: it does not spend/refund money, demolish anything, or
change the live road network. Right-click/right-drag retains its existing
demolition behavior. Holding Shift temporarily suppresses left-click building
placement so inspecting a tile cannot accidentally construct something.
Automatically demolished buildings receive the usual demolition refund once,
release all occupied cells, and stop contributing population, finances and
pollution. Neighboring road graphics refresh after demolition.

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

Use **Elysium > Verify Drag Interactions** or execute
`DragInteractionVerification.Run` in batch mode for road strokes, charges,
UI interruption, window dragging/clamping and close/reopen checks.

Use **Elysium > Verify Road Demolition** or execute
`RoadDemolitionVerification.Run` for dependent building removal, alternative
roads, refunds, road graphics and right-button demolition strokes.

**Elysium > Verify Central Road** (`CentralRoadVerification.Run`) checks the
free/idempotent starting tile, disconnected islands, diagonal contact, network
connection and severing, alternate routes, central-road replacement, distant
building removal and bulk demolition. Existing financial and drag checks seed
connected infrastructure in memory before recording their starting totals.

**Elysium > Verify Road Feedback** (`RoadFeedbackVerification.Run`) checks road
colors/marks, safe prediction, alternate/multiple cuts, preview geometry and
tooltip, original-price refunds, and agreement with actual demolition.
