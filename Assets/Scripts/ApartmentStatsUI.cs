using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class ApartmentStatsUI : MonoBehaviour
{
    private GridPlacementSystem grid;
    private GridPlaceable selected;
    internal bool HasSelection => selected != null;
    private Rect panel;
    private Vector2 scroll;
    [System.NonSerialized] private GUIStyle bodyStyle;
    [System.NonSerialized] private GUIStyle titleStyle;
    [System.NonSerialized] private GUIStyle buttonStyle;

    private void UpdatePanelBounds()
    {
        float width = Mathf.Min(680, Screen.width - 24);
        float height = Mathf.Min(780, Screen.height - 32);
        panel = new Rect(Screen.width - width - 12, (Screen.height - height) * 0.5f, width, height);
    }
    private static readonly string[] services = { "school", "hospital", "police_station", "fire_station" };
    private readonly string[] labels = { "Education", "Healthcare", "Police", "Fire protection" };

    private void Awake() => grid = GetComponent<GridPlacementSystem>();

    private void OnEnable()
    {
        // GUIStyle contains native state. Recreate it after Unity reloads scripts.
        bodyStyle = null;
        titleStyle = null;
        buttonStyle = null;
    }

    public bool ContainsScreenPoint(Vector2 point)
    {
        UpdatePanelBounds();
        return selected != null && panel.Contains(new Vector2(point.x, Screen.height - point.y));
    }

    public void ProcessSelectionInput()
    {
        UpdatePanelBounds();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            selected = null;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || !grid.HasMapHover)
            return;
        selected = null;
        scroll = Vector2.zero;
        foreach (var item in grid.GetPlacedObjects())
        {
            if (!new RectInt(item.Anchor, item.Footprint).Contains(grid.HoveredCell)) continue;
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building != null && building.IsApartment) selected = building;
            break;
        }
    }

    public static void CalculateHappiness(GridPlacementSystem grid, GridPlaceable selected,
        out bool[] covered, out int residents, out int workforce, out int working,
        out int unemployment, out int pollution, out int happiness)
    {
        covered = new bool[services.Length];
        int[] missingPenalties = GetMissingServicePenalties(grid);
        foreach (var item in grid.GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building == null || !building.isActiveAndEnabled || !building.IsOperational) continue;
            string id = building.Definition != null ? building.Definition.BuildingId : building.BuildingTypeId;
            for (int i = 0; i < services.Length; i++)
            {
                if (id != services[i]) continue;
                if (Vector2.Distance(selected.transform.position, building.transform.position) <= (id == "hospital" ? 15 : 12) * grid.CellSize)
                    covered[i] = true;
            }
        }
        residents = selected.ResidentsProvided;
        workforce = Mathf.CeilToInt(residents * 0.66f);
        working = 0;
        foreach (var assignment in GetWorkplaces(grid, selected)) working += assignment.Value;
        unemployment = workforce > 0 ? Mathf.RoundToInt(20f * (workforce - working) / workforce) : 0;
        var pollutionSystem = FindAnyObjectByType<PollutionSystem>();
        pollution = pollutionSystem != null ? Mathf.Min(20, pollutionSystem.TotalPollution / 10) : 0;
        happiness = selected.BaseHappiness - unemployment - pollution;

        for (int i = 0; i < services.Length; i++)
        {
            int effect = covered[i] ? 10 : -missingPenalties[i];
            happiness += effect;

        }
        happiness = Mathf.Clamp(happiness + GetParkBonus(grid, selected), 0, 100);
    }

    private static int[] GetMissingServicePenalties(GridPlacementSystem grid)
    {
        var penalties = new int[] { 5, 5, 5, 5 };
        foreach (var item in grid.GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building == null || !building.isActiveAndEnabled || !building.IsOperational || building.Definition == null) continue;
            for (int i = 0; i < services.Length; i++)
                if (building.Definition.BuildingId == services[i]) penalties[i] = 7;
        }
        return penalties;
    }

    public static Dictionary<GridPlaceable, int> GetWorkplaces(GridPlacementSystem grid, GridPlaceable selected)
    {
        var result = new Dictionary<GridPlaceable, int>();
        if (grid == null) return result;
        var homes = new List<GridPlaceable>();
        var employers = new List<GridPlaceable>();
        foreach (var item in grid.GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building == null || !building.isActiveAndEnabled || !building.IsOperational) continue;
            if (building.ResidentsProvided > 0) homes.Add(building);
            if (building.Definition != null && building.Definition.JobsProvided > 0) employers.Add(building);
        }
        homes.Sort((a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));
        employers.Sort((a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));
        var filled = new Dictionary<GridPlaceable, int>();
        foreach (var home in homes) filled[home] = 0;
        int nextHome = 0;
        foreach (var employer in employers)
        {
            for (int slot = 0; slot < employer.Definition.JobsProvided; slot++)
            {
                int examined = 0;
                while (examined < homes.Count &&
                    filled[homes[nextHome]] >= Mathf.CeilToInt(homes[nextHome].ResidentsProvided * 0.66f))
                {
                    nextHome = (nextHome + 1) % homes.Count;
                    examined++;
                }
                if (examined == homes.Count) return result;
                var home = homes[nextHome];
                filled[home]++;
                if (home == selected)
                {
                    result.TryGetValue(employer, out int count);
                    result[employer] = count + 1;
                }
                nextHome = (nextHome + 1) % homes.Count;
            }
        }
        return result;
    }

    public static int GetWorkplaceIncomeBonus(Dictionary<GridPlaceable, int> workplaces)
    {
        int bonus = 0;
        foreach (var entry in workplaces)
        {
            string id = entry.Key.Definition.BuildingId;
            if (id == "factory") bonus += entry.Value * 7;
            if (id == "market") bonus += entry.Value * 3;
        }
        return bonus;
    }

    private static int GetParkBonus(GridPlacementSystem grid, GridPlaceable home)
    {
        int bonus = 0;
        foreach (var item in grid.GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building != null && building.IsOperational && building.Definition != null &&
                building.Definition.BuildingId == "park" &&
                Vector2.Distance(home.transform.position, building.transform.position) <= 8 * grid.CellSize)
                bonus += 5;
        }
        return Mathf.Min(15, bonus);
    }

    private static void SetWhiteText(GUIStyle style)
    {
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Color.white;
        style.onNormal.textColor = Color.white;
        style.onHover.textColor = Color.white;
        style.onActive.textColor = Color.white;
        style.onFocused.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (DisasterSystem.IsCutsceneOpen) return;
        if (selected == null || !selected.isActiveAndEnabled) return;
        CalculateHappiness(grid, selected, out var covered, out int residents, out int workforce,
            out int working, out int unemployment, out int pollution, out int happiness);
        int baseHappiness = selected.BaseHappiness;
        int capacity = selected.HousingCapacity;
        int revenueBonus = DailyEconomy.HappinessRevenueBonusPercent(happiness);
        var workplaces = GetWorkplaces(grid, selected);
        int parkBonus = GetParkBonus(grid, selected);
        int[] missingPenalties = GetMissingServicePenalties(grid);
        long dailyRevenue = DailyEconomy.GetCitizenIncome(residents, working, happiness);
        foreach (var workplace in workplaces)
            dailyRevenue += DailyEconomy.GetWorkplaceRevenue(workplace.Key.Definition, workplace.Value, happiness);
        UpdatePanelBounds();
        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                fontSize = 26, wordWrap = true,
                padding = new RectOffset(0, 0, 5, 5)
            };
            SetWhiteText(bodyStyle);
            titleStyle = new GUIStyle(bodyStyle) { fontSize = 32, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = bodyStyle.font, fontSize = 24 };
            SetWhiteText(titleStyle);
            SetWhiteText(buttonStyle);
        }
        Color previousColor = GUI.color;
        Color previousContentColor = GUI.contentColor;
        GUI.contentColor = Color.white;
        GUI.color = new Color(0.07f, 0.10f, 0.15f, 1);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(panel.x + 22, panel.y + 18, panel.width - 44, panel.height - 36));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Apartment population", titleStyle);
        if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(100), GUILayout.Height(46)))
            selected = null;
        GUILayout.EndHorizontal();
        GUILayout.Space(12);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("Residents: " + residents + " / " + capacity, bodyStyle);
        GUILayout.Label("Employed: " + working + " / " + workforce + " workers", bodyStyle);
        GUILayout.Label("Workplaces", titleStyle);
        foreach (var workplace in workplaces)
        {
            Vector2Int cell = grid.GetBuildingCoordinates(workplace.Key.gameObject);
            GUILayout.Label($"{workplace.Key.Definition.BuildingName} ({cell.x}, {cell.y}): {workplace.Value} workers", bodyStyle);
        }
        if (workplaces.Count == 0) GUILayout.Label("No workers assigned.", bodyStyle);
        GUILayout.Label("Average happiness: " + Mathf.Clamp(happiness, 0, 100) + " / 100", titleStyle);
        GUILayout.Space(14);
        GUILayout.Label($"Resident income: ${dailyRevenue:N0}  (+{revenueBonus}% happiness bonus)", bodyStyle);
        GUILayout.Label("Workers earn $6/day; factory workers add $7 and market workers add $3. Other residents earn $1/day.", bodyStyle);
        GUILayout.Label("Above 50 happiness: +12% revenue. At 90 or above: +25%.", bodyStyle);
        GUILayout.Space(14);
        GUILayout.Label("Happiness breakdown", titleStyle);
        GUILayout.Label("Base happiness    +" + baseHappiness, bodyStyle);
        for (int i = 0; i < services.Length; i++)
        {
            GUILayout.Label(labels[i] + (covered[i] ? " available    +10" : " missing    -" + missingPenalties[i]), bodyStyle);
        }
        GUI.color = Color.white;
        GUILayout.Label("Unemployment    -" + unemployment, bodyStyle);
        GUILayout.Label("Nearby parks    +" + parkBonus + " (up to 3 parks, +5 each)", bodyStyle);
        GUILayout.Label("City pollution    -" + pollution, bodyStyle);
        GUILayout.Space(14);
        GUILayout.Label("Hospitals cover 15 tiles; other services cover 12 tiles. 66% of residents are available to work, rounded up.", bodyStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.color = previousColor;
        GUI.contentColor = previousContentColor;
    }
}
