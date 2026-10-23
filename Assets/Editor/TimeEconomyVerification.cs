using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeEconomyVerification
{
    // Run on a clean editor process. Only the in-memory scene is modified.
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        var wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>();
        var state = UnityEngine.Object.FindAnyObjectByType<GameState>();
        var shop = UnityEngine.Object.FindAnyObjectByType<TemporaryEconomy>();
        Check(clock != null && economy != null && grid != null && wallet != null && state != null && shop != null, "Scene wiring");
        wallet.SendMessage("Awake");
        state.SendMessage("Awake");
        clock.SendMessage("Awake");
        economy.SendMessage("Awake");
        economy.SendMessage("OnEnable");
        shop.SendMessage("Awake");
        grid.SendMessage("Awake");
        var adapter = UnityEngine.Object.FindAnyObjectByType<BuildingStoreAdapter>();
        var houseData = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ShopUI/BuildingData/House.asset");
        var hallData = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ShopUI/BuildingData/CityHall.asset");
        Check(adapter != null && adapter.GetPlacementCost(houseData) == 100, "Shop uses actual placement cost");
        Check(adapter.SelectBuilding(houseData) && wallet.Balance == 1000, "Shop selection does not spend");
        Check(adapter.GetPlacementCost(hallData) == -1, "Unmapped City Hall is unavailable");
        Check(wallet.Balance == 1000 && economy.Balance == 1000 && shop.Balance == 1000 && state.Balance == 1000, "One starting balance");
        Check(Time.timeScale == 1, "No global 120x speed override");
        string path = "Assets/Medieval Buildings and Objects/Prefabs/buildings/";
        var road = AssetDatabase.LoadAssetAtPath<GameObject>(path + "wall.prefab");
        var tower = AssetDatabase.LoadAssetAtPath<GameObject>(path + "tower (1).prefab");
        var house = AssetDatabase.LoadAssetAtPath<GameObject>(path + "house A (1).prefab");
        Check(!grid.TryPlace(house, new Vector2Int(40, 40), out _) && wallet.Balance == 1000, "Road rule does not charge on failure");
        Check(grid.TryPlace(road, new Vector2Int(37, 40), out var r), "Place road");
        Check(grid.TryPlace(tower, new Vector2Int(20, 20), out var t), "Place tower");
        Check(grid.TryPlace(house, new Vector2Int(40, 40), out var h), "Place house next to road");
        Check(wallet.Balance == 600 && economy.Balance == 600 && shop.Balance == 600, "Charge once at upstream prices");
        Check(state.BuildingCount == 3 && state.ResidentialCount == 1, "Placement counts");
        Check(!grid.TryPlace(road, new Vector2Int(37, 40), out _) && wallet.Balance == 600, "Overlap costs nothing");
        Check(!grid.TryPlace(road, new Vector2Int(-1, 0), out _) && wallet.Balance == 600, "Out of bounds costs nothing");
        clock.AdvanceTime(199);
        Check(wallet.Balance == 600 && clock.Day == 1, "No early payout");
        clock.AdvanceTime(1);
        Check(wallet.Balance == 668 && shop.Balance == 668 && economy.Balance == 668 && state.Balance == 668, "Shared midnight settlement");
        Check(economy.LastIncome == 85 && economy.LastUpkeep == 17, "Preserved category rates");
        clock.Paused = true;
        clock.AdvanceTime(300);
        Check(wallet.Balance == 668 && clock.Day == 2, "Pause");
        clock.Paused = false;
        grid.DestroyBuilding(new Vector2Int(40, 40));
        Check(wallet.Balance == 718 && (h == null || !h.activeSelf) && !grid.IsCellOccupied(new Vector2Int(40, 40)), "Demolition refund and immediate exclusion");
        Check(state.BuildingCount == 2 && state.ResidentialCount == 0, "Demolition counts");
        grid.DestroyBuilding(new Vector2Int(40, 40));
        Check(wallet.Balance == 718, "No duplicate refund");
        clock.AdvanceTime(600);
        Check(wallet.Balance == 754 && clock.Day == 4, "Two days without demolished house");
        Check(wallet.TrySpend(wallet.Balance), "Drain balance");
        Check(!grid.TryPlace(tower, new Vector2Int(60, 60), out _) && !grid.IsCellOccupied(new Vector2Int(60, 60)), "Unaffordable placement");
        t.SetActive(false);
        clock.AdvanceTime(300);
        Check(wallet.Balance == -2 && shop.Balance == -2 && economy.Balance == -2, "Upkeep debt shared across systems");
        Debug.Log("TIME_ECONOMY_VERIFICATION_PASSED");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Time/economy verification failed: " + message);
    }
}
