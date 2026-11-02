using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EconomyBudgetVerification
{
    [MenuItem("Elysium/Verify Economy Budget")]
    public static void Run()
    {
        TimeEconomyVerification.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        var wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>();
        var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        var state = UnityEngine.Object.FindAnyObjectByType<GameState>();
        var view = UnityEngine.Object.FindAnyObjectByType<EconomyBudgetUI>();
        Check(view && PrefabUtility.IsPartOfPrefabInstance(view), "Linked budget prefab");
        Invoke(wallet, "Awake"); Invoke(state, "Awake"); Invoke(clock, "Awake");
        Invoke(economy, "Awake"); Invoke(economy, "OnEnable"); Invoke(grid, "Awake");
        RoadNetworkTestSupport.Connect(grid, new Vector2Int(39, 41));
        Invoke(view, "OnEnable");
        double dayLength = new SerializedObject(clock).FindProperty("secondsPerDay").floatValue;
        Check(!view.IsReportOpen && view.BudgetText.Contains("No daily settlement yet"), "No fictitious day-one earnings");
        var apartment = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ShopUI/BuildingData/Apartment.asset");
        var house = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/apt.prefab");
        var road = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/road.prefab");
        var adapter = UnityEngine.Object.FindAnyObjectByType<BuildingStoreAdapter>();
        var original = new SerializedObject(apartment);
        int price = apartment.Price, income = apartment.DailyIncome, upkeep = apartment.DailyUpkeep;
        try
        {
            original.FindProperty("price").intValue = 123;
            original.FindProperty("dailyIncome").intValue = 37;
            original.FindProperty("dailyUpkeep").intValue = 11;
            original.ApplyModifiedPropertiesWithoutUndo();
            Check(adapter.GetPlacementCost(apartment) == 123, "Shop uses edited definition price");
            var card = UnityEngine.Object.FindObjectsByType<BuildingCardUI>(FindObjectsInactive.Include)[0];
            card.Setup(apartment, adapter.GetPlacementCost(apartment));
            var cardLabel = (TMPro.TMP_Text)new SerializedObject(card).FindProperty("buildingPrice").objectReferenceValue;
            Check(cardLabel.text.Contains("Income $37") && cardLabel.text.Contains("Upkeep $11") &&
                cardLabel.text.Contains("+$26"), "Shop displays per-building daily finances before purchase");
            var before = economy.Forecast();
            int start = wallet.Balance;
            Check(grid.TryPlace(road, new Vector2Int(39, 40), out _), "Road purchase");
            Check(grid.TryPlace(house, new Vector2Int(40, 40), out _), "Apartment purchase");
            Check(wallet.Balance == start - 173, "Definition price charged once");
            var forecast = economy.Forecast();
            Check(forecast.Income == before.Income + 37 && forecast.Upkeep == before.Upkeep + 13, "Definition rates drive forecast");
            view.RefreshBudget();
            Check(view.BudgetText.Contains($"{forecast.ClosingBalance:N0}"), "Projected balance is displayed");
            int events = 0;
            BudgetReport snapshot = null;
            economy.DaySettled += report => { events++; snapshot = report; Check(report.ClosingBalance == wallet.Balance, "Popup follows wallet settlement"); };
            clock.AdvanceTime(dayLength * 2d / 3d - 1d);
            Check(events == 0 && !view.IsReportOpen, "No premature payout or popup");
            clock.AdvanceTime(1.001);
            Check(events == 1 && wallet.Balance == forecast.ClosingBalance, "One settlement at day boundary");
            Check(view.IsReportOpen && view.ReportText.Contains("Apartment") && view.ReportText.Contains($"{forecast.Income:N0}"), "Popup contains earnings and building details");
            var reportScroll = (UnityEngine.UI.ScrollRect)new SerializedObject(view).FindProperty("reportScroll").objectReferenceValue;
            Check(reportScroll.content.anchoredPosition == Vector2.zero, "Report opens at earnings summary");
            Check(snapshot.Day == 2 && snapshot.Net == forecast.Net, "Day and net earnings");
            Check(clock.Paused && Time.timeScale == 0f, "Report pauses simulation");
            original.Update(); original.FindProperty("price").intValue = 999;
            original.ApplyModifiedPropertiesWithoutUndo();
            grid.DestroyBuilding(new Vector2Int(40, 40));
            long afterRefund = wallet.Balance;
            Check(afterRefund == forecast.ClosingBalance + 61, "Refund uses original paid cost");
            grid.DestroyBuilding(new Vector2Int(40, 40));
            Check(wallet.Balance == afterRefund, "No duplicate demolition refund");
            var afterDemolition = economy.Forecast();
            Check(afterDemolition.Income == forecast.Income - 37 && afterDemolition.Upkeep == forecast.Upkeep - 11, "Demolished building excluded");
            Check(snapshot.Income == forecast.Income, "Settled snapshot remains unchanged");
            clock.AdvanceTime(dayLength * 2d);
            Check(events == 1 && clock.Day == 2 && wallet.Balance == afterRefund, "Open report prevents further days and payouts");
            view.DismissReport();
            Check(!clock.Paused && Time.timeScale == 1f, "Continue restores simulation");
            clock.AdvanceTime(dayLength * 3d);
            Check(events == 2 && clock.Day == 3 && clock.Hour == 0 && clock.Minute == 0,
                "Long frame stops at the first new-day popup");
            Check(wallet.Balance == afterRefund + afterDemolition.Net, "Only one new payout before pause");
            view.ShowReport(snapshot);
            Check(view.PendingReports == 1, "External reports still queue");
            view.DismissReport(); Check(view.IsReportOpen && clock.Paused && Time.timeScale == 0f, "Queued report retains pause");
            view.DismissReport(); Check(!view.IsReportOpen && !clock.Paused && Time.timeScale == 1f, "Final report restores simulation");
            clock.Paused = true;
            Time.timeScale = 0.5f;
            view.ShowReport(snapshot); view.DismissReport();
            Check(clock.Paused && Time.timeScale == 0.5f, "Pre-existing pause and global speed are preserved");
            clock.Paused = false; Time.timeScale = 1f;
            view.ShowReport(snapshot);
            Invoke(view, "OnDisable"); Invoke(view, "OnEnable");
            Check(!view.IsReportOpen && !clock.Paused && Time.timeScale == 1f, "Disabling UI releases pause and closes report");
            clock.AdvanceTime(dayLength);
            Check(view.IsReportOpen && view.PendingReports == 0, "Re-enable does not duplicate reports");
            view.DismissReport();
            foreach (var building in UnityEngine.Object.FindObjectsByType<GridPlaceable>()) building.gameObject.SetActive(false);
            Check(economy.Forecast().Income == 0 && economy.Forecast().Upkeep == 0, "Empty city has zero income and upkeep");
            view.ShowReport(new BudgetReport(9, 0, -20, new[] { new BudgetReport.Line("Services", 1, 0, 20) }));
            Check(view.ReportText.Contains("Net loss") && view.ReportText.Contains("-$20"), "Loss is clearly reported");
        }
        finally
        {
            original.Update(); original.FindProperty("price").intValue = price;
            original.FindProperty("dailyIncome").intValue = income;
            original.FindProperty("dailyUpkeep").intValue = upkeep;
            original.ApplyModifiedPropertiesWithoutUndo();
            Invoke(view, "OnDisable"); Invoke(economy, "OnDisable");
        }
        Debug.Log("ECONOMY_BUDGET_VERIFICATION_PASSED");
    }
    private static void Invoke(MonoBehaviour component, string method) => component.GetType()
        .GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(component, null);
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Budget verification failed: " + message); }
}
