using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeEconomyVerification
{
    // Run in an isolated editor process; changes remain in memory.
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        var wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>();
        var state = UnityEngine.Object.FindAnyObjectByType<GameState>();
        var shop = UnityEngine.Object.FindAnyObjectByType<BuildingStoreUI>();
        Check(clock && economy && grid && wallet && state && shop, "Scene wiring");
        Check(UnityEngine.Object.FindObjectsByType<CurrencySystem>().Length == 1, "One wallet");
        Check(new SerializedObject(shop).FindProperty("economy").objectReferenceValue == wallet, "Shop wallet");
        Check(new SerializedObject(economy).FindProperty("currency").objectReferenceValue == wallet, "Settlement wallet");
        foreach (var root in state.gameObject.scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "No missing scripts: " + transform.name);
        InvokeLifecycle(wallet, "Awake");
        InvokeLifecycle(state, "Awake");
        InvokeLifecycle(clock, "Awake");
        InvokeLifecycle(economy, "Awake");
        InvokeLifecycle(economy, "OnEnable");
        InvokeLifecycle(grid, "Awake");
        state.RefreshPopulation();
        int initialPopulation = state.Population;
        int initialBuildings = state.BuildingCount;
        int initialBalance = wallet.Balance;
        var adapter = UnityEngine.Object.FindAnyObjectByType<BuildingStoreAdapter>();
        string[] dataNames = { "Apartment", "Road", "CityHall", "Market" };
        string[] prefabNames = { "apt", "road", "mayors office", "market" };
        int[] prices = { 100, 50, 500, 200 };
        var prefabs = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ShopUI/BuildingData/" + dataNames[i] + ".asset");
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabNames[i] + ".prefab");
            Check(prefabs[i] && prefabs[i].GetComponent<SpriteRenderer>().sprite, "Building graphics: " + dataNames[i]);
            Check(adapter.GetPlacementCost(data) == prices[i], "Shop price: " + dataNames[i]);
            Check(adapter.SelectBuilding(data) && wallet.Balance == initialBalance, "Selection costs nothing");
        }
        Check(economy.Balance == initialBalance && state.Balance == initialBalance, "Shared starting balance");
        Check(Time.timeScale == 1, "Normal global speed");
        var preplaced = UnityEngine.Object.Instantiate(prefabs[0]);
        var housing = new SerializedObject(preplaced.GetComponent<GridPlaceable>());
        housing.FindProperty("residentsProvided").intValue = 3;
        housing.ApplyModifiedPropertiesWithoutUndo();
        state.RefreshPopulation();
        Check(state.Population == initialPopulation + 3, "Configurable preplaced housing");
        preplaced.SetActive(false);
        state.RefreshPopulation();
        Check(state.Population == initialPopulation, "Inactive housing excluded");
        UnityEngine.Object.DestroyImmediate(preplaced);
        var clockView = UnityEngine.Object.FindAnyObjectByType<TimeEconomyHUD>();
        Check(clockView && PrefabUtility.IsPartOfPrefabInstance(clockView), "Editable clock prefab");
        InvokeLifecycle(clockView, "Awake");
        Check(clockView.DisplayText == "Day 1  |  8:00 AM", "Clock display");
        Check(!grid.TryPlace(prefabs[0], new Vector2Int(40, 40), out _) && wallet.Balance == initialBalance, "Road rule costs nothing on failure");
        Check(grid.TryPlace(prefabs[1], new Vector2Int(39, 40), out var road), "Place road");
        Check(grid.TryPlace(prefabs[0], new Vector2Int(40, 40), out var house), "Place apartment");
        Check(grid.TryPlace(prefabs[3], new Vector2Int(37, 40), out var market), "Place market");
        Check(grid.TryPlace(prefabs[2], new Vector2Int(39, 36), out var hall), "Place Mayor's Office");
        int afterPurchase = initialBalance - 850;
        Check(wallet.Balance == afterPurchase && economy.Balance == afterPurchase, "Charge once");
        Check(state.Population == initialPopulation + 10 && state.BuildingCount == initialBuildings + 4, "Apartment population");
        Check(!grid.TryPlace(prefabs[1], new Vector2Int(39, 40), out _) && wallet.Balance == afterPurchase, "Overlap costs nothing");
        Check(!grid.TryPlace(prefabs[1], new Vector2Int(-1, 0), out _) && wallet.Balance == afterPurchase, "Bounds costs nothing");
        long expectedIncome = 0, expectedUpkeep = 0;
        foreach (var building in UnityEngine.Object.FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != state.gameObject.scene) continue;
            var rate = economy.GetRates(building.Category);
            expectedIncome += rate.dailyIncome;
            expectedUpkeep += rate.dailyUpkeep;
        }
        clock.AdvanceTime(199);
        Check(wallet.Balance == afterPurchase && clock.Day == 1, "No early payout");
        clock.AdvanceTime(1);
        Check(wallet.Balance == afterPurchase + expectedIncome - expectedUpkeep && state.Balance == wallet.Balance && economy.Balance == wallet.Balance, "Shared midnight settlement");
        Check(economy.LastIncome == expectedIncome && economy.LastUpkeep == expectedUpkeep, "Category rates");
        int settledBalance = wallet.Balance;
        clock.Paused = true;
        clock.AdvanceTime(300);
        Check(wallet.Balance == settledBalance && clock.Day == 2, "Pause");
        clock.Paused = false;
        grid.DestroyBuilding(new Vector2Int(40, 40));
        Check(wallet.Balance == settledBalance + 50 && !grid.IsCellOccupied(new Vector2Int(40, 40)), "Refund");
        Check(state.Population == initialPopulation && state.BuildingCount == initialBuildings + 3, "Population after demolition");
        grid.DestroyBuilding(new Vector2Int(40, 40));
        Check(wallet.Balance == settledBalance + 50, "No duplicate refund");
        Check(wallet.TrySpend(wallet.Balance), "Drain wallet");
        Check(!grid.TryPlace(prefabs[1], new Vector2Int(60, 60), out _), "Unaffordable placement");
        wallet.ApplySettlement(-20);
        Check(wallet.Balance == -20 && economy.Balance == -20 && state.Balance == -20, "Shared debt");
        var viewFields = new SerializedObject(clockView);
        var speedButton = (UnityEngine.UI.Button)viewFields.FindProperty("speedButton").objectReferenceValue;
        var speedLabel = (UnityEngine.UI.Text)viewFields.FindProperty("speedLabel").objectReferenceValue;
        Check(speedButton && speedLabel && clockView.GetComponent<UnityEngine.UI.GraphicRaycaster>(), "Speed control wiring");
        InvokeLifecycle(clockView, "OnEnable");
        Check(speedLabel.text == "1×", "Initial speed label");
        int speedDay = clock.Day;
        speedButton.onClick.Invoke();
        Check(clock.Speed == 2 && speedLabel.text == "2×", "Button selects 2x");
        clock.AdvanceTime(150);
        Check(clock.Day == speedDay + 1, "2x advances a day in 150 seconds");
        speedButton.onClick.Invoke();
        Check(clock.Speed == 4 && speedLabel.text == "4×", "Button selects 4x");
        int beforeFastSettlement = wallet.Balance;
        clock.AdvanceTime(150);
        Check(clock.Day == speedDay + 3 && wallet.Balance == beforeFastSettlement + economy.LastNet * 2, "4x applies each midnight settlement");
        speedButton.onClick.Invoke();
        Check(clock.Speed == 1 && speedLabel.text == "1×", "Button cycles back to 1x");
        InvokeLifecycle(clockView, "OnDisable");
        InvokeLifecycle(clockView, "OnEnable");
        speedButton.onClick.Invoke();
        Check(clock.Speed == 2, "Re-enabling does not duplicate click listeners");
        clock.Paused = true;
        clock.AdvanceTime(300);
        Check(clock.Day == speedDay + 3 && Time.timeScale == 1, "Pause works at increased speed without scaling UI time");
        InvokeLifecycle(clockView, "OnDisable");
        Debug.Log("TIME_ECONOMY_VERIFICATION_PASSED");
    }
    private static void InvokeLifecycle(MonoBehaviour component, string method)
    {
        component.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(component, null);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Time/economy verification failed: " + message);
    }
}
