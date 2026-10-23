using UnityEngine;

[RequireComponent(typeof(TimeManager), typeof(DailyEconomy))]
public class TimeEconomyHUD : MonoBehaviour
{
    private Rect PanelRect => new Rect(Mathf.Max(0, Screen.width - 290), 10, 280, 220);

    public bool ContainsScreenPoint(Vector2 position)
    {
        return isActiveAndEnabled && PanelRect.Contains(new Vector2(position.x, Screen.height - position.y));
    }

    private TimeManager clock;
    private DailyEconomy economy;
    private void Awake()
    {
        clock = GetComponent<TimeManager>();
        economy = GetComponent<DailyEconomy>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.Label($"Day {clock.Day}  |  {clock.DisplayTime}");
        if (GUILayout.Button($"Speed: {clock.Speed:0.#}x  (click to change)"))
            clock.CycleSpeed();
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
        GUILayout.Label($"{label}: +{rates.dailyIncome}/-{rates.dailyUpkeep} daily");
    }
}
