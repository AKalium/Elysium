using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeEconomyVerification
{
    // Run in batch mode on a clean editor process; this does not save scene changes.
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        Check(clock != null && economy != null && grid != null, "Scene wiring");
        clock.SendMessage("Awake");
        economy.SendMessage("Awake");
        economy.SendMessage("OnEnable");
        Check(economy.Balance == 1000 && clock.Hour == 8 && clock.Day == 1, "Starting state");
        string path = "Assets/New Folder/Medieval Buildings and Objects/Prefabs/buildings/";
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(path + "wall.prefab");
        var tower = AssetDatabase.LoadAssetAtPath<GameObject>(path + "tower (1).prefab");
        var house = AssetDatabase.LoadAssetAtPath<GameObject>(path + "house A (1).prefab");
        Check(grid.TryPlace(wall, new Vector2Int(0, 0), out var w), "Place wall");
        Check(grid.TryPlace(tower, new Vector2Int(20, 20), out var t), "Place tower");
        Check(grid.TryPlace(house, new Vector2Int(40, 40), out var h), "Place house");
        Check(economy.Balance == 690, "Purchase costs");
        Check(!grid.TryPlace(wall, new Vector2Int(0, 0), out _) && economy.Balance == 690, "Occupied cell costs nothing");
        Check(!grid.TryPlace(house, new Vector2Int(-1, 0), out _) && economy.Balance == 690, "Invalid cell costs nothing");
        clock.AdvanceTime(199);
        Check(economy.Balance == 690 && clock.Day == 1, "No early payout");
        clock.AdvanceTime(1);
        Check(economy.Balance == 758 && clock.Day == 2 && clock.Hour == 0 && clock.Minute == 0, "Exact midnight settlement");
        Check(economy.LastIncome == 85 && economy.LastUpkeep == 17, "Category totals");
        clock.AdvanceTime(600);
        Check(clock.Day == 4 && economy.Balance == 894, "Multiple crossed days");
        clock.Paused = true;
        clock.AdvanceTime(300);
        Check(clock.Day == 4 && economy.Balance == 894, "Pause");
        clock.Paused = false;
        UnityEngine.Object.DestroyImmediate(h);
        t.SetActive(false);
        clock.AdvanceTime(300);
        Check(economy.Balance == 892 && economy.LastNet == -2, "Removed and inactive buildings excluded");
        while (economy.TryPurchase(BuildingCategory.Economic)) { }
        long balance = economy.Balance;
        Check(!grid.TryPlace(house, new Vector2Int(60, 60), out _) && economy.Balance == balance && !grid.IsCellOccupied(new Vector2Int(60, 60)), "Insufficient funds");
        clock.AdvanceTime(300 * 50);
        Check(economy.Balance < 0, "Upkeep can create debt");
        Debug.Log("TIME_ECONOMY_VERIFICATION_PASSED");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Time/economy verification failed: " + message);
    }
}
