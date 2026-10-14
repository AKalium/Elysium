using UnityEngine;

[RequireComponent(typeof(TimeManager), typeof(DailyEconomy))]
public class TimeEconomyHUD : MonoBehaviour
{
    private TimeManager clock;
    private DailyEconomy economy;
    private void Awake()
    {
        clock = GetComponent<TimeManager>();
        economy = GetComponent<DailyEconomy>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Mathf.Max(0, Screen.width - 290), 10, 280, 190), GUI.skin.box);
        GUILayout.Label($"Day {clock.Day}  |  {clock.DisplayTime}");
        GUILayout.Label($"Money: ${economy.Balance}");
        GUILayout.Label($"Last midnight: +${economy.LastIncome} -${economy.LastUpkeep} = ${economy.LastNet}");
        ShowCost("1: House", BuildingCategory.Economic);
        ShowCost("2: Tower", BuildingCategory.Residential);
        ShowCost("3: Wall", BuildingCategory.Neither);
        GUILayout.Label("Income and upkeep apply at 12:00 AM.");
        GUILayout.EndArea();
    }

    private void ShowCost(string label, BuildingCategory category)
    {
        var rates = economy.GetRates(category);
        GUILayout.Label($"{label}: ${rates.purchaseCost} | +{rates.dailyIncome}/-{rates.dailyUpkeep} daily");
    }
}