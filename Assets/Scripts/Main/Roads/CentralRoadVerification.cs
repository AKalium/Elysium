using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using F = RoadNetworkTestSupport.Fixture;

public static class CentralRoadVerification
{
    [MenuItem("Elysium/Verify Central Road")]
    public static void Run()
    {
        try
        {
            StartupAndIslands();
            SeverAndReconnect();
            AlternateRoute();
            BulkDemolition();
            InputCancellationAndGraphics();
        }
        finally { Time.timeScale = 1; }
        Debug.Log("CENTRAL_ROAD_VERIFICATION_PASSED");
    }

    private static void StartupAndIslands()
    {
        var f = RoadNetworkTestSupport.Fresh();
        int initial = new SerializedObject(f.Wallet).FindProperty("startingBalance").intValue;
        Check(f.Wallet.Balance == initial && f.Grid.IsRoadConnected(f.Center), "Free connected starting tile at map center");
        int count = f.State.BuildingCount;
        RoadNetworkTestSupport.Invoke(f.Grid, "Start"); f.State.RefreshPopulation();
        Check(f.State.BuildingCount == count && f.Wallet.Balance == initial, "Startup cannot create duplicate tiles or charges");
        var island = f.Center + new Vector2Int(8, 0);
        Place(f, f.Road, island); Place(f, f.Road, f.Center + new Vector2Int(1, 1));
        Check(!f.Grid.IsRoadConnected(island) && !f.Grid.IsRoadConnected(f.Center + new Vector2Int(1, 1)), "Separate and diagonal-only roads are disconnected");
        int balance = f.Wallet.Balance;
        Check(!f.Grid.TryPlace(f.House, island + Vector2Int.up, out _) && f.Wallet.Balance == balance,
            "Disconnected road cannot support a building or charge a failed purchase");
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, island);
        Check(f.Grid.IsRoadConnected(island) && f.Grid.IsRoadConnected(f.Center + new Vector2Int(1, 1)), "Connecting islands updates network immediately");
        Place(f, f.House, island + Vector2Int.up);
    }

    private static void SeverAndReconnect()
    {
        var f = RoadNetworkTestSupport.Fresh();
        // The one end tile supports buildings on both sides of the street.
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(2, 0));
        var top = f.Center + new Vector2Int(2, 1);
        var bottom = f.Center + new Vector2Int(2, -2);
        var baseline = f.Economy.Forecast();
        int population = f.State.Population;
        Place(f, f.House, top); Place(f, f.House, bottom);
        int balance = f.Wallet.Balance;
        var end = f.Center + new Vector2Int(2, 0);
        f.Grid.DestroyBuilding(end);
        Cleared(f, top); Cleared(f, bottom);
        Check(f.State.Population == population, "Automatic demolition removes population");
        Check(f.Economy.Forecast().Income == baseline.Income &&
            f.Economy.Forecast().Upkeep == baseline.Upkeep - f.Road.GetComponent<GridPlaceable>().Definition.DailyUpkeep,
            "Automatic demolition removes daily finances");
        Check(f.Wallet.Balance == balance + Refund(f, f.Road) + 2 * Refund(f, f.House), "Road and both buildings refund once");
        balance = f.Wallet.Balance;
        f.Grid.DestroyBuilding(end); f.Grid.DestroyBuilding(top);
        Check(f.Wallet.Balance == balance, "No duplicate refunds");

        f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(6, 0));
        var home = f.Center + new Vector2Int(5, 1);
        Place(f, f.House, home);
        balance = f.Wallet.Balance;
        var bridge = f.Center + new Vector2Int(2, 0);
        f.Grid.DestroyBuilding(bridge);
        Cleared(f, home);
        Check(f.Grid.IsCellOccupied(f.Center + new Vector2Int(6, 0)) &&
            !f.Grid.IsRoadConnected(f.Center + new Vector2Int(6, 0)), "Distant buildings lose access even when local roads remain");
        Check(f.Wallet.Balance == balance + Refund(f, f.Road) + Refund(f, f.House), "Distant demolition refund");
        Place(f, f.Road, bridge);
        Check(f.Grid.IsRoadConnected(f.Center + new Vector2Int(6, 0)), "Replacing bridge reconnects network");
        Place(f, f.House, home);
        balance = f.Wallet.Balance;
        f.Grid.DestroyBuilding(f.Center);
        Cleared(f, home);
        Check(!f.Grid.IsRoadConnected(f.Center + Vector2Int.right) && f.Wallet.Balance == balance + Refund(f, f.House),
            "Removing free center disconnects city and grants no road refund");
        Check(!f.Grid.TryPlace(f.House, home, out _), "Buildings rejected while center is missing");
        Place(f, f.Road, f.Center); Place(f, f.House, home);
    }

    private static void AlternateRoute()
    {
        var f = RoadNetworkTestSupport.Fresh();
        var far = f.Center + new Vector2Int(6, 0);
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, far);
        // A second route runs below the original road, clear of the housing.
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(0, -2),
            f.Center + new Vector2Int(6, -2), far);
        var home = f.Center + new Vector2Int(5, 1);
        Place(f, f.House, home);
        f.Grid.DestroyBuilding(f.Center + new Vector2Int(2, 0));
        Check(f.Grid.IsCellOccupied(home) && f.Grid.IsRoadConnected(far), "Alternative route preserves buildings after first cut");
        f.Grid.DestroyBuilding(f.Center + new Vector2Int(3, -2));
        Cleared(f, home);

        // Access on a different edge of the whole footprint is also valid.
        f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(5, 0));
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(0, 5),
            f.Center + new Vector2Int(8, 5), f.Center + new Vector2Int(8, 2), f.Center + new Vector2Int(7, 2));
        home = f.Center + new Vector2Int(5, 1); Place(f, f.House, home);
        f.Grid.DestroyBuilding(f.Center + new Vector2Int(5, 0));
        Check(f.Grid.IsCellOccupied(home), "Connected road on far footprint edge preserves building");
        f.Grid.DestroyBuilding(f.Center + new Vector2Int(7, 2)); Cleared(f, home);
    }

    private static void BulkDemolition()
    {
        var f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(8, 0));
        var home = f.Center + new Vector2Int(6, 1); Place(f, f.House, home);
        int balance = f.Wallet.Balance;
        Delete(f, f.Center + new Vector2Int(2, 0), true, true, true);
        Delete(f, f.Center + new Vector2Int(5, 0), true, false, true);
        for (int x = 2; x <= 5; x++) Check(!f.Grid.IsCellOccupied(f.Center + new Vector2Int(x, 0)), "Fast drag clears skipped road tiles");
        Cleared(f, home);
        Check(f.Grid.IsCellOccupied(f.Center + new Vector2Int(7, 0)), "Disconnected road tiles remain on map");
        Check(f.Wallet.Balance == balance + 4 * Refund(f, f.Road) + Refund(f, f.House), "Bulk road and building refunds are exact");
        balance = f.Wallet.Balance;
        Delete(f, f.Center + new Vector2Int(2, 0), true, false, true);
        Check(f.Wallet.Balance == balance, "Retracing demolition does not refund again");

        f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(6, 0));
        home = f.Center + new Vector2Int(5, 1); Place(f, f.House, home);
        Delete(f, f.Center + new Vector2Int(4, 1), true, true, true);
        Delete(f, f.Center + new Vector2Int(8, 1), true, false, true);
        Check(f.Grid.IsCellOccupied(home), "Road brush does not directly erase buildings it crosses");
        Delete(f, home, true, true, false); Cleared(f, home);
        Check(f.Grid.IsCellOccupied(f.Center + new Vector2Int(5, 0)), "Single right-click demolishes building only");
    }

    private static void InputCancellationAndGraphics()
    {
        var f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(3, 0));
        var one = f.Center + Vector2Int.right; var two = one + Vector2Int.right; var three = two + Vector2Int.right;
        Delete(f, one, true, true, true); Delete(f, two, false, false, true); Delete(f, three, true, false, true);
        Check(f.Grid.IsCellOccupied(two) && f.Grid.IsCellOccupied(three), "UI interruption cancels demolition");
        Delete(f, three, true, true, false);
        Check(!f.Grid.IsCellOccupied(three), "Quick right-click still works");
        Delete(f, three, true, false, false); Delete(f, two, true, false, true);
        Check(f.Grid.IsCellOccupied(two), "Released drag cannot resume");
        Time.timeScale = 0; Delete(f, two, true, true, true);
        Check(f.Grid.IsCellOccupied(two), "Pause blocks demolition"); Time.timeScale = 1;
        var empty = f.Center + new Vector2Int(0, 3);
        Delete(f, empty, true, true, true);
        f.Grid.GetType().GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Grid, new object[] { false });
        Delete(f, two, true, false, true); Check(f.Grid.IsCellOccupied(two), "Focus loss cancels drag");
        Delete(f, empty, true, true, true); f.Grid.SelectPlaceable(-1); Delete(f, two, true, false, true);
        Check(f.Grid.IsCellOccupied(two), "Selection change cancels demolition");
        Delete(f, empty, true, true, true); Delete(f, new Vector2Int(-1, 0), true, false, true); Delete(f, two, true, false, true);
        Check(f.Grid.IsCellOccupied(two), "Leaving map cancels demolition");

        f = RoadNetworkTestSupport.Fresh();
        var cells = new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3) };
        foreach (var cell in cells) Place(f, f.Road, f.Center + cell);
        Delete(f, f.Center + cells[0], true, true, true); Delete(f, f.Center + cells[cells.Length - 1], true, false, true);
        foreach (var cell in cells) Check(!f.Grid.IsCellOccupied(f.Center + cell), "Diagonal demolition clears connected path");

        var rootRoad = UnityEngine.Object.FindAnyObjectByType<GridPlaceable>();
        var defaultSprite = (Sprite)new SerializedObject(f.Road.GetComponent<SpriteChanger>()).FindProperty("sprites").GetArrayElementAtIndex(0).objectReferenceValue;
        Check(rootRoad.GetComponent<SpriteRenderer>().sprite == defaultSprite, "Isolated central road graphic resets");
    }

    private static int Refund(F f, GameObject prefab) => Mathf.FloorToInt(prefab.GetComponent<GridPlaceable>().Definition.Price *
        new SerializedObject(f.Wallet).FindProperty("demolitionRefundRate").floatValue);
    private static void Place(F f, GameObject prefab, Vector2Int cell)
    { Check(f.Grid.TryPlace(prefab, cell, out _), "Place " + prefab.name + " at " + cell); }
    private static void Cleared(F f, Vector2Int anchor)
    { for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) Check(!f.Grid.IsCellOccupied(anchor + new Vector2Int(x, y)), "Disconnected footprint cleared"); }
    private static void Delete(F f, Vector2Int cell, bool onMap, bool pressed, bool held) => f.Grid.GetType()
        .GetMethod("ProcessDemolitionInput", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(f.Grid, new object[] { cell, onMap, pressed, held });
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Central road verification failed: " + message); }
}
