using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ApartmentStatsUI))]
public class GridPlacementSystem : MonoBehaviour {
    [SerializeField] private DailyEconomy economy;
    private TimeEconomyHUD timeHUD;
    private EconomyBudgetUI budgetUI;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;
    [SerializeField, Min(1)] private int gridWidth = 100;
    [SerializeField, Min(1)] private int gridHeight = 100;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-50f, -50f);
    [SerializeField] private GameObject[] placeablePrefabs = new GameObject[0];
    [SerializeField] private CurrencySystem currency;
    [SerializeField] private BuildingStoreUI storeUI;
    [SerializeField] private Color gridColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    [SerializeField] private Color validPlacementColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    private readonly Dictionary<Vector2Int, GameObject> cellOwners = new();
    private readonly Dictionary<GameObject, List<Vector2Int>> objectCells = new();
    private readonly Dictionary<GameObject, int> paidCosts = new();
    private static readonly Vector2Int[] NeighborDirections =
        { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
    private Mesh previewMesh;
    private Mesh gridMesh;
    private Material previewMaterial;
    private Material gridMaterial;
    private MeshRenderer previewRenderer;
    private GameObject ghostPreview;
private SpriteRenderer ghostRenderer;
private int ghostPrefabIndex = -1;

[SerializeField] private Color validGhostColor =
    new Color(0.4f, 1f, 0.4f, 0.5f);

[SerializeField] private Color invalidGhostColor =
    new Color(1f, 0.3f, 0.3f, 0.5f);
    private int selectedPrefabIndex = -1;
    public bool HasBuildingSelection => selectedPrefabIndex >= 0;
    private readonly HashSet<GameObject> pausePurchases = new();
    public void EndPauseRefunds() => pausePurchases.Clear();
    private void LateUpdate()
    {
        if (Time.timeScale > 0f) pausePurchases.Clear();
    }
    private int DemolitionRefund(GameObject building, int cost) =>
        Time.timeScale == 0f && pausePurchases.Contains(building)
            ? cost : currency.CalculateDemolitionRefund(cost);

    private int freeRoadsUsed;
    private int foundingApartmentsUsed;
    public int FreeRoadsRemaining => Mathf.Max(0, 10 - freeRoadsUsed);
    private Vector2Int hoveredCell;
    private Vector2Int hoveredFootprint = Vector2Int.one;
    private bool hasHoveredCell;
    private bool hoveredCellIsValid;
    private bool roadStrokeActive;
    private Vector2Int lastRoadCell;
    private readonly HashSet<Vector2Int> roadStrokeVisited = new();
    private bool demolitionStrokeActive;
    private Vector2Int lastDemolitionCell;
    private readonly HashSet<Vector2Int> demolitionStrokeVisited = new();
    private readonly HashSet<Vector2Int> connectedRoads = new();
    private bool roadNetworkDirty = true;
    private RoadNetworkVisuals roadVisuals;
    public int MapVersion { get; private set; }
    public float CellSize => cellSize;
    public Camera PlacementCamera => targetCamera;
    public bool HasMapHover => hasHoveredCell;
    public Vector2Int HoveredCell => hoveredCell;
    public Vector2Int ToCityCoordinates(Vector2Int cell) => cell - CentralRoadCell;
    public Vector2Int FromCityCoordinates(Vector2Int coordinates) => coordinates + CentralRoadCell;

    public Vector2Int GetBuildingCoordinates(GameObject building)
    {
        return objectCells.TryGetValue(building, out var cells) && cells.Count > 0
            ? ToCityCoordinates(cells[0]) : ToCityCoordinates(WorldToCell(building.transform.position));
    }

    public Vector2Int CentralRoadCell => new Vector2Int(gridWidth / 2, gridHeight / 2);

    private void Start()
    {
        if (IsRoadAt(CentralRoadCell)) return;
        foreach (var prefab in placeablePrefabs)
        {
            if (prefab == null || prefab.name != "road") continue;
            if (!TryPlaceInternal(prefab, CentralRoadCell, true, out _))
                Debug.LogError("Could not create the starting central road.", this);
            return;
        }
        Debug.LogError("GridPlacementSystem needs a road prefab for the starting central road.", this);
    }

    private void OnEnable() { if (roadVisuals != null) roadVisuals.enabled = true; }
    private void OnDisable()
    {
        ResetStrokes();
        if (roadVisuals != null) roadVisuals.enabled = false;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) ResetStrokes(); }

    private void ResetStrokes() { ResetRoadStroke(); ResetDemolitionStroke(); }

    private void Awake() 
    {
        if (GetComponent<ApartmentStatsUI>() == null) gameObject.AddComponent<ApartmentStatsUI>();
        if (economy == null) economy = FindAnyObjectByType<DailyEconomy>();
        timeHUD = FindAnyObjectByType<TimeEconomyHUD>();
        budgetUI = FindAnyObjectByType<EconomyBudgetUI>();
        if (currency == null) currency = GetComponent<CurrencySystem>();
        if (currency == null) currency = FindAnyObjectByType<CurrencySystem>();
        if (currency == null) currency = gameObject.AddComponent<CurrencySystem>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("GridPlacementSystem needs a camera, but no camera was assigned or tagged MainCamera.", this);
            enabled = false;
            return;
        }

        CreateGridVisual();
        CreatePlacementPreview();
        CreateGhostPreview();
    }

    private void Update()
{
    UpdateSelection();
    UpdateHoveredCell();
    GetComponent<ApartmentStatsUI>().ProcessSelectionInput();
    UpdatePlacementPreview();
    UpdateGhostPreview();
    HandlePlacementInput();
    HandleDeletionInput();
}

    private void OnDestroy() 
    {
        if (previewMesh != null)
        {
            Destroy(previewMesh);
        }

        if (gridMesh != null)
        {
            Destroy(gridMesh);
        }

        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
        }

        if (gridMaterial != null)
        {
            Destroy(gridMaterial);
        }
    }

    public Vector2Int WorldToCell(Vector2 worldPosition) 
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize),
            Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell) 
    {
        return new Vector3(
            gridOrigin.x + (cell.x + 0.5f) * cellSize,
            gridOrigin.y + (cell.y + 0.5f) * cellSize,
            0f);
    }

    public bool TryPlace(GameObject prefab, Vector2Int cell, out GameObject placedObject) 
        => TryPlaceInternal(prefab, cell, false, out placedObject);

    private bool TryPlaceInternal(GameObject prefab, Vector2Int cell, bool startingRoad, out GameObject placedObject)
    {
        placedObject = null;
        if (prefab == null)
        {
            return false;
        }

        var definition = prefab.GetComponent<GridPlaceable>()?.Definition;
        if (!HasRequiredMayorsOffice(definition))
        {
            if (storeUI != null) storeUI.ShowMessage("Build a Mayor's Office before constructing service buildings.");
            return false;
        }

        if (HasReachedBuildingLimit(prefab))
    {
    if (storeUI != null)
    {
        storeUI.ShowMessage("This building has reached its placement limit!");
    }

    return false;
    }

        Vector2Int footprint = GetFootprint(prefab);
        int cost = GetConstructionCost(prefab);
        if (cost < 0) return false;
        if (startingRoad) cost = 0;
        else if (cost > 0 && !currency.CanAfford(cost)) return false;
        if (!CanPlace(cell, footprint))
    {
    if (storeUI != null)
    {
        storeUI.ShowMessage("Can't place building here!");
    }

    return false;
}

        if (prefab.name != "road" && !HasCentralRoadAccess(cell, footprint))
        {
            if (storeUI != null)
            {
                storeUI.ShowMessage("Buildings need a road connected to the central road!");
            }
            return false;
        }

        Vector3 position = new Vector3(
            gridOrigin.x + (cell.x + footprint.x * 0.5f) * cellSize,
            gridOrigin.y + (cell.y + footprint.y * 0.5f) * cellSize,
            prefab.transform.position.z);
        placedObject = Instantiate(prefab, position, prefab.transform.rotation);
        if (cost > 0 && !currency.TrySpend(cost))
        {
            Destroy(placedObject);
            placedObject = null;
            return false;
        }

        List<Vector2Int> cells = new List<Vector2Int>(footprint.x * footprint.y);
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                Vector2Int occupiedCell = cell + new Vector2Int(x, y);
                cellOwners.Add(occupiedCell, placedObject);
                cells.Add(occupiedCell);
            }
        }

        if (prefab.name == "road" && freeRoadsUsed < 10) freeRoadsUsed++;
        var placedBuilding = placedObject.GetComponent<GridPlaceable>();
        if (placedBuilding != null && placedBuilding.Definition != null &&
            placedBuilding.Definition.BuildingId == "apartment" && foundingApartmentsUsed < 3)
        {
            placedBuilding.InitializeFoundingApartment();
            foundingApartmentsUsed++;
        }
        else if (placedBuilding != null && placedBuilding.IsApartment)
            placedBuilding.InitializeEmptyApartment();
        GameState.Instance?.RefreshPopulation();
        objectCells.Add(placedObject, cells);
        paidCosts.Add(placedObject, cost);
        if (Time.timeScale == 0f) pausePurchases.Add(placedObject);

        if (prefab.name =="road")
        {
            roadNetworkDirty = true;
            SetRoadSprite(placedObject, cell, footprint, false);    
        }

        MapVersion++;
        return true;
    }

    public void DestroyBuilding(Vector2Int cell) 
    {
        if (!cellOwners.TryGetValue(cell, out GameObject placedObject)) 
        {
            return;
        }

        bool removedRoad = IsRoadAt(cell);
        var affected = new HashSet<GameObject>();
        if (removedRoad && objectCells.TryGetValue(placedObject, out var roadCells))
        {
            foreach (var roadCell in roadCells)
                foreach (var direction in NeighborDirections)
                    if (cellOwners.TryGetValue(roadCell + direction, out var neighbor) && neighbor != placedObject)
                        affected.Add(neighbor);
        }

        if (objectCells.TryGetValue(placedObject, out List<Vector2Int> cells)) 
        {
            foreach (Vector2Int occupiedCell in cells)
            {
                if (cellOwners.TryGetValue(occupiedCell, out GameObject owner) &&
                    owner == placedObject)
                {
                    cellOwners.Remove(occupiedCell);
                }
            }

            objectCells.Remove(placedObject);
        }

        placedObject.SetActive(false);
        GameState.Instance?.RefreshPopulation();
        if (paidCosts.TryGetValue(placedObject, out int paidCost)) 
        {
            paidCosts.Remove(placedObject);
            currency.AddFunds(DemolitionRefund(placedObject, paidCost));
            pausePurchases.Remove(placedObject);
        }
        if (Application.isPlaying) Destroy(placedObject);
        else DestroyImmediate(placedObject);

        foreach (var neighbor in affected)
        {
            if (neighbor == null || !objectCells.TryGetValue(neighbor, out var neighborCells) || neighborCells.Count == 0) continue;
            var anchor = neighborCells[0];
            var footprint = GetFootprint(neighbor);
            if (IsRoadAt(anchor)) SetRoadSprite(neighbor, anchor, footprint, true);
        }
        if (removedRoad)
        {
            roadNetworkDirty = true;
            RemoveBuildingsWithoutCentralAccess();
        }
        MapVersion++;
    }

    public bool HasPreviousDayStats { get; private set; }
    public float PreviousHappiness { get; private set; }
    public float PreviousEmployment { get; private set; }
    public float PreviousOccupancy { get; private set; }
    public int PreviousPollution { get; private set; }

    public static string DailyChange(float current, float previous, bool available, string unit = "")
    {
        if (!available) return "(0" + unit + ")";
        float change = Mathf.Round(current - previous);
        return "(" + change.ToString("+0;-0;0") + unit + ")";
    }

    public void CapturePreviousDayStats()
    {
        int population = 0, capacity = 0, workforce = 0, employed = 0;
        long happinessTotal = 0;
        foreach (var item in GetPlacedObjects())
        {
            var home = item.Object.GetComponent<GridPlaceable>();
            if (home == null || !home.IsApartment) continue;
            capacity += home.HousingCapacity;
            ApartmentStatsUI.CalculateHappiness(this, home, out _, out int residents,
                out int workers, out int working, out _, out _, out int happiness);
            population += residents;
            workforce += workers;
            employed += working;
            happinessTotal += (long)residents * happiness;
        }
        PreviousHappiness = population > 0 ? (float)happinessTotal / population : 0;
        PreviousEmployment = workforce > 0 ? 100f * employed / workforce : 0;
        PreviousOccupancy = capacity > 0 ? 100f * population / capacity : 0;
        var pollution = FindAnyObjectByType<PollutionSystem>();
        PreviousPollution = pollution != null ? pollution.TotalPollution : 0;
        HasPreviousDayStats = true;
    }

    public void AddDailyResidents() => UpdateDailyPopulation(0);

    public string UpdateDailyPopulation(int completedDay, double dailyNetIncome = 0)
    {
        var vacancies = new List<GridPlaceable>();
        var departures = new Dictionary<GridPlaceable, int>();
        int unhappyDepartures = 0, unemployedDepartures = 0;
        int happinessArrivals = 0, earningsArrivals = 0;
        long weightedHappiness = 0;
        long population = 0;
        foreach (var item in GetPlacedObjects())
        {
            var home = item.Object.GetComponent<GridPlaceable>();
            if (home == null || !home.IsApartment || !home.isActiveAndEnabled) continue;
            if (home.ResidentsProvided < home.HousingCapacity) vacancies.Add(home);
            if (home.ResidentsProvided == 0) continue;
            ApartmentStatsUI.CalculateHappiness(this, home, out _, out int residents,
                out _, out int employed, out _, out _, out int happiness);
            int leaving = 0;
            if (residents >= 5)
            {
                if (completedDay >= 3 && happiness < 30) { leaving += 2; unhappyDepartures += 2; }
                if (completedDay >= 6 && employed == 0) { leaving += 2; unemployedDepartures += 2; }
            }
            if (leaving > 0) departures.Add(home, leaving);
            weightedHappiness += (long)happiness * residents;
            population += residents;
        }
        // Compare the unrounded, resident-weighted average strictly above 50.
        bool attractsResidents = population > 0 && weightedHappiness > 50 * population;
        // All departure decisions use the same end-of-day population and job allocation.
        foreach (var departure in departures)
        {
            departure.Key.RemoveResidents(departure.Value);
            if (!vacancies.Contains(departure.Key)) vacancies.Add(departure.Key);
        }
        int incomeArrivals = dailyNetIncome > 150
            ? 1 + 2 * (int)System.Math.Floor((dailyNetIncome - 150) / 400) : 0;
        int arrivals = (attractsResidents ? 3 : 0) + incomeArrivals;
        for (int i = 0; i < arrivals && vacancies.Count > 0; i++)
        {
            int index = Random.Range(0, vacancies.Count);
            var home = vacancies[index];
            home.TryAddResident();
            if (attractsResidents && i < 3) happinessArrivals++;
            else earningsArrivals++;
            if (home.ResidentsProvided >= home.HousingCapacity) vacancies.RemoveAt(index);
        }
        GameState.Instance?.RefreshPopulation();
        int arrived = happinessArrivals + earningsArrivals;
        int departed = unhappyDepartures + unemployedDepartures;
        long closing = population + arrived - departed;
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"Population at midnight: {population} → {closing} ({(arrived - departed).ToString("+0;-0;0")})");
        summary.AppendLine($"Arrivals: +{arrived}");
        summary.AppendLine($"  Happiness above 50: +{happinessArrivals}");
        summary.AppendLine($"  Net income incentive: +{earningsArrivals}");
        summary.AppendLine($"Departures: -{departed}");
        summary.AppendLine($"  Happiness below 30: -{unhappyDepartures}");
        summary.AppendLine($"  Zero employment: -{unemployedDepartures}");
        if (arrivals > arrived)
            summary.AppendLine($"{arrivals - arrived} potential arrivals could not move in because housing was full.");
        summary.AppendLine("These changes cover the midnight population update.");
        return summary.ToString();
    }

    private void HandleDeletionInput() 
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) { ResetDemolitionStroke(); return; }
        ProcessDemolitionInput(hoveredCell, hasHoveredCell,
            mouse.rightButton.wasPressedThisFrame, mouse.rightButton.isPressed);
    }

    private void ProcessDemolitionInput(Vector2Int cell, bool pointerOnMap, bool pressed, bool held)
    {
        if ((!held && !pressed) || !pointerOnMap ||
            cell.x < 0 || cell.y < 0 || cell.x >= gridWidth || cell.y >= gridHeight)
        {
            ResetDemolitionStroke();
            return;
        }
        if (pressed)
        {
            ResetDemolitionStroke();
            demolitionStrokeActive = true;
            lastDemolitionCell = cell;
            demolitionStrokeVisited.Add(cell);
            // A single right-click still demolishes any building. Subsequent
            // cells in a held stroke erase roads only.
            if (RoadSelected)
            {
                if (IsRoadAt(cell)) DestroyBuilding(cell);
            }
            else DestroyBuilding(cell);
        }
        else if (demolitionStrokeActive)
        {
            TraceStroke(lastDemolitionCell, cell, EraseRoadCell);
            lastDemolitionCell = cell;
        }
    }

    private void EraseRoadCell(Vector2Int cell)
    {
        if (demolitionStrokeVisited.Add(cell) && IsRoadAt(cell)) DestroyBuilding(cell);
    }

    private void ResetRoadStroke()
    {
        roadStrokeActive = false;
        roadStrokeVisited.Clear();
    }

    private static void TraceStroke(Vector2Int from, Vector2Int to, System.Action<Vector2Int> visit)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int stepX = from.x < to.x ? 1 : -1;
        int stepY = from.y < to.y ? 1 : -1;
        int error = dx - dy;
        var cell = from;
        while (cell != to)
        {
            int doubledError = error * 2;
            if (doubledError > -dy)
            {
                error -= dy;
                cell.x += stepX;
                visit(cell);
            }
            if (doubledError < dx)
            {
                error += dx;
                cell.y += stepY;
                visit(cell);
            }
        }
    }

    private void ResetDemolitionStroke()
    {
        demolitionStrokeActive = false;
        demolitionStrokeVisited.Clear();
    }

    public bool IsCellOccupied(Vector2Int cell) 
    {
        return cellOwners.ContainsKey(cell);
    }

    public void SelectPlaceable(int index) 
    {
        ResetStrokes();
        if (index < 0 || index >= placeablePrefabs.Length || placeablePrefabs[index] == null)
        {
            selectedPrefabIndex = -1;
            return;
        }
        selectedPrefabIndex = index;
    }
    public bool HasRequiredMayorsOffice(BuildingData definition)
    {
        if (definition == null || definition.BuildingId == "city_hall" ||
            definition.BuildingId == "road" || definition.BuildingId == "park" ||
            definition.Category != BuildingData.BuildingCategory.Services) return true;
        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            if (building.Category == BuildingCategory.MayorsOffice) return true;
        }
        return false;
    }

private bool HasReachedBuildingLimit(GameObject prefab)
{
    if (prefab == null || GameState.Instance == null)
        return false;

    GridPlaceable placeable = prefab.GetComponent<GridPlaceable>();

    if (placeable == null || placeable.MaxAllowed == 0)
        return false;

    if (string.IsNullOrEmpty(placeable.BuildingTypeId))
    {
        Debug.LogWarning("Limited building is missing a Building Type ID.", prefab);
        return true;
    }

    int existing = GameState.Instance.CountBuildingsOfType(
        placeable.BuildingTypeId);

    return existing >= placeable.MaxAllowed;
}
    private bool CanPlace(Vector2Int anchor, Vector2Int footprint) 
    {
        if (anchor.x < 0 || anchor.y < 0 ||
            anchor.x + footprint.x > gridWidth ||
            anchor.y + footprint.y > gridHeight)
        {
            return false;
        }

        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                if (cellOwners.ContainsKey(anchor + new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool HasCentralRoadAccess(Vector2Int anchor, Vector2Int footprint)
    {
        RebuildRoadNetwork();
        return BordersRoadNetwork(anchor, footprint, connectedRoads);
    }

    private static bool BordersRoadNetwork(Vector2Int anchor, Vector2Int footprint, ISet<Vector2Int> roads)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            if (roads.Contains(anchor + new Vector2Int(x, -1)) ||
                roads.Contains(anchor + new Vector2Int(x, footprint.y)))
            {
                return true;
            }
        }

        for (int y = 0; y < footprint.y; y++)
        {
            if (roads.Contains(anchor + new Vector2Int(-1, y)) ||
                roads.Contains(anchor + new Vector2Int(footprint.x, y)))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsRoadConnected(Vector2Int cell)
    {
        RebuildRoadNetwork();
        return connectedRoads.Contains(cell);
    }

    private void RebuildRoadNetwork()
    {
        if (!roadNetworkDirty) return;
        roadNetworkDirty = false;
        FloodRoadNetwork(connectedRoads, null);
    }

    private void FloodRoadNetwork(HashSet<Vector2Int> result, ISet<Vector2Int> excluded)
    {
        result.Clear();
        if (!IsRoadAt(CentralRoadCell) || (excluded != null && excluded.Contains(CentralRoadCell))) return;
        var frontier = new Queue<Vector2Int>();
        result.Add(CentralRoadCell);
        frontier.Enqueue(CentralRoadCell);
        while (frontier.Count > 0)
        {
            var cell = frontier.Dequeue();
            foreach (var direction in NeighborDirections)
            {
                var next = cell + direction;
                if ((excluded == null || !excluded.Contains(next)) && IsRoadAt(next) && result.Add(next)) frontier.Enqueue(next);
            }
        }
    }

    public IReadOnlyList<PlacedMapObject> GetPlacedObjects()
    {
        var result = new List<PlacedMapObject>();
        foreach (var entry in objectCells)
            if (entry.Key != null && entry.Key.activeInHierarchy && entry.Value.Count > 0)
                result.Add(new PlacedMapObject(entry.Key, entry.Value[0], GetFootprint(entry.Key), IsRoadAt(entry.Value[0])));
        return result.AsReadOnly();
    }

    public DemolitionPlan PreviewDemolition(Vector2Int cell)
    {
        var direct = new HashSet<GameObject>();
        if (cellOwners.TryGetValue(cell, out var target) && target != null) direct.Add(target);
        return BuildDemolitionPlan(direct);
    }

    public DemolitionPlan PreviewRoadDemolition(Vector2Int from, Vector2Int to)
    {
        var direct = new HashSet<GameObject>();
        if (from.x < 0 || from.y < 0 || to.x < 0 || to.y < 0 || from.x >= gridWidth ||
            from.y >= gridHeight || to.x >= gridWidth || to.y >= gridHeight) return BuildDemolitionPlan(direct);
        System.Action<Vector2Int> collect = cell => { if (IsRoadAt(cell)) direct.Add(cellOwners[cell]); };
        collect(from); TraceStroke(from, to, collect);
        return BuildDemolitionPlan(direct);
    }

    private DemolitionPlan BuildDemolitionPlan(HashSet<GameObject> targets)
    {
        if (targets.Count == 0) return new DemolitionPlan(System.Array.Empty<PlacedMapObject>(), 0, false);
        // Simulate removal in a separate network. Never disable objects, spend
        // money, alter occupancy, or mutate the live connectivity cache.
        var removedRoadCells = new HashSet<Vector2Int>();
        foreach (var target in targets)
            if (objectCells.TryGetValue(target, out var cells) && cells.Count > 0 && IsRoadAt(cells[0]))
                foreach (var cell in cells) removedRoadCells.Add(cell);
        var snapshot = GetPlacedObjects();
        if (removedRoadCells.Count > 0)
        {
            var remainingNetwork = new HashSet<Vector2Int>();
            FloodRoadNetwork(remainingNetwork, removedRoadCells);
            foreach (var item in snapshot)
                if (!item.IsRoad && !BordersRoadNetwork(item.Anchor, item.Footprint, remainingNetwork)) targets.Add(item.Object);
        }
        var result = new List<PlacedMapObject>();
        long refund = 0;
        foreach (var item in snapshot)
            if (targets.Contains(item.Object))
            {
                result.Add(item);
                if (paidCosts.TryGetValue(item.Object, out int cost)) refund += DemolitionRefund(item.Object, cost);
            }
        result.Sort((a, b) => a.Anchor.x != b.Anchor.x ? a.Anchor.x.CompareTo(b.Anchor.x) : a.Anchor.y.CompareTo(b.Anchor.y));
        return new DemolitionPlan(result, refund, removedRoadCells.Contains(CentralRoadCell));
    }

    private void RemoveBuildingsWithoutCentralAccess()
    {
        RebuildRoadNetwork();
        var disconnected = new List<Vector2Int>();
        foreach (var entry in objectCells)
        {
            if (entry.Key == null || entry.Value.Count == 0) continue;
            var anchor = entry.Value[0];
            if (!IsRoadAt(anchor) && !HasCentralRoadAccess(anchor, GetFootprint(entry.Key))) disconnected.Add(anchor);
        }
        foreach (var anchor in disconnected) DestroyBuilding(anchor);
    }

    private bool IsRoadAt(Vector2Int cell)
    {
        return cellOwners.TryGetValue(cell, out GameObject owner) &&
            owner != null && owner.activeInHierarchy && owner.name == "road(Clone)";
    }

    private Vector2Int GetFootprint(GameObject prefab) 
    {
        GridPlaceable placeable = prefab.GetComponent<GridPlaceable>();
        return placeable != null ? placeable.Footprint : Vector2Int.one;
    }

    private int GetConstructionCost(GameObject prefab) 
    {
        int index = System.Array.IndexOf(placeablePrefabs, prefab);
        return GetConstructionCost(index);
    }

    public int GetConstructionCost(int index)
    {
        // Refuse unpriced prefabs instead of accidentally granting free buildings.
        if (index < 0 || index >= placeablePrefabs.Length || placeablePrefabs[index] == null) return -1;
        var placeable = placeablePrefabs[index].GetComponent<GridPlaceable>();
        if (placeable == null || placeable.Definition == null) return -1;
        if (placeablePrefabs[index].name == "road" && FreeRoadsRemaining > 0) return 0;
        return placeable.Definition.Price;
    }

    private void UpdateSelection() 
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) 
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame) 
        {
            SelectPlaceable(-1);
            return;
        }

        Key[] numberKeys = {
            Key.Digit1, Key.Digit2, Key.Digit3,
            Key.Digit4, Key.Digit5, Key.Digit6,
            Key.Digit7, Key.Digit8, Key.Digit9
        };

        for (int i = 0; i < numberKeys.Length && i < placeablePrefabs.Length; i++) 
        {
            if (keyboard[numberKeys[i]].wasPressedThisFrame) 
            {
                SelectPlaceable(i);
                return;
            }
        }
    }

    private void UpdateHoveredCell() 
    {
        Mouse mouse = Mouse.current;
        hasHoveredCell = false;
        var apartmentStats = GetComponent<ApartmentStatsUI>();
        if (mouse != null && apartmentStats != null && apartmentStats.ContainsScreenPoint(mouse.position.ReadValue())) return;
        if (mouse == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
            (timeHUD != null && timeHUD.ContainsScreenPoint(mouse.position.ReadValue())) ||
            (budgetUI != null && budgetUI.ContainsScreenPoint(mouse.position.ReadValue())) ||
            (roadVisuals != null && roadVisuals.ContainsLegendScreenPoint(mouse.position.ReadValue())))
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane placementPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!placementPlane.Raycast(ray, out float distance)) 
        {
            return;
        }

        Vector3 worldPosition = ray.GetPoint(distance);
        hoveredCell = WorldToCell(worldPosition);
        hoveredFootprint = selectedPrefabIndex >= 0
            ? GetFootprint(placeablePrefabs[selectedPrefabIndex])
            : Vector2Int.one;
        hoveredCellIsValid = selectedPrefabIndex >= 0 &&
    CanPlace(hoveredCell, hoveredFootprint) &&
    !HasReachedBuildingLimit(placeablePrefabs[selectedPrefabIndex]) &&
    HasRequiredMayorsOffice(placeablePrefabs[selectedPrefabIndex].GetComponent<GridPlaceable>()?.Definition) &&
    (placeablePrefabs[selectedPrefabIndex].name == "road" ||
        HasCentralRoadAccess(hoveredCell, hoveredFootprint)) &&
        (GetConstructionCost(selectedPrefabIndex) == 0 ||
         currency.CanAfford(GetConstructionCost(selectedPrefabIndex)));

hasHoveredCell = true;
}


    private bool RoadSelected => selectedPrefabIndex >= 0 &&
        placeablePrefabs[selectedPrefabIndex] != null &&
        placeablePrefabs[selectedPrefabIndex].name == "road";

    private void HandlePlacementInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || mouse.rightButton.isPressed)
        {
            ResetRoadStroke();
            return;
        }
        ProcessPlacementInput(hoveredCell, hasHoveredCell,
            mouse.leftButton.wasPressedThisFrame, mouse.leftButton.isPressed);
    }

    private void ProcessPlacementInput(Vector2Int cell, bool pointerOnMap, bool pressed, bool held)
    {
        if ((!pressed && !held) || !pointerOnMap ||
            selectedPrefabIndex < 0 || cell.x < 0 || cell.y < 0 ||
            cell.x >= gridWidth || cell.y >= gridHeight)
        {
            ResetRoadStroke();
            return;
        }
        if (!RoadSelected)
        {
            ResetRoadStroke();
            if (pressed) TryPlace(placeablePrefabs[selectedPrefabIndex], cell, out _);
            return;
        }
        if (pressed)
        {
            ResetRoadStroke();
            roadStrokeActive = true;
            lastRoadCell = cell;
            PaintRoadCell(cell);
        }
        else if (roadStrokeActive)
        {
            TraceStroke(lastRoadCell, cell, PaintRoadCell);
            lastRoadCell = cell;
        }
    }

    private void PaintRoadCell(Vector2Int cell)
    {
        if (!roadStrokeVisited.Add(cell) || IsCellOccupied(cell)) return;
        TryPlace(placeablePrefabs[selectedPrefabIndex], cell, out _);
    }

    private void CreateGridVisual() 
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find the Sprites/Default shader for the grid visual.", this);
            return;
        }

        GameObject gridObject = new GameObject("Grid Visual");
        MeshFilter meshFilter = gridObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();
        gridMesh = new Mesh { name = "Placement Grid" };
        int verticalLineCount = gridWidth + 1;
        int horizontalLineCount = gridHeight + 1;
        Vector3[] vertices = new Vector3[(verticalLineCount + horizontalLineCount) * 2];
        int[] indices = new int[vertices.Length];
        int vertex = 0;

        for (int x = 0; x <= gridWidth; x++) 
        {
            float worldX = gridOrigin.x + x * cellSize;
            vertices[vertex] = new Vector3(worldX, gridOrigin.y, -0.05f);
            vertices[vertex + 1] = new Vector3(worldX, gridOrigin.y + gridHeight * cellSize, -0.05f);
            indices[vertex] = vertex;
            indices[vertex + 1] = vertex + 1;
            vertex += 2;
        }

        for (int y = 0; y <= gridHeight; y++) 
        {
            float worldY = gridOrigin.y + y * cellSize;
            vertices[vertex] = new Vector3(gridOrigin.x, worldY, -0.05f);
            vertices[vertex + 1] = new Vector3(gridOrigin.x + gridWidth * cellSize, worldY, -0.05f);
            indices[vertex] = vertex;
            indices[vertex + 1] = vertex + 1;
            vertex += 2;
        }

        gridMesh.vertices = vertices;
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
        gridMesh.RecalculateBounds();
        meshFilter.sharedMesh = gridMesh;

        gridMaterial = new Material(shader) { color = gridColor };
        meshRenderer.sharedMaterial = gridMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void CreateGhostPreview()
{
    ghostPreview = new GameObject("Building Ghost Preview");
    ghostRenderer = ghostPreview.AddComponent<SpriteRenderer>();

    // Keep the preview visible above regular buildings.
    ghostRenderer.sortingOrder = 100;

    // The ghost is initially hidden.
    ghostPreview.SetActive(false);
}

private void UpdateGhostPreview()
{
    if (ghostPreview == null)
        return;

    bool showGhost = hasHoveredCell && selectedPrefabIndex >= 0;

    if (!showGhost)
    {
        ghostPreview.SetActive(false);
        return;
    }

    GameObject prefab = placeablePrefabs[selectedPrefabIndex];

    if (prefab == null)
    {
        ghostPreview.SetActive(false);
        return;
    }

    // Update the sprite when the selected building changes.
    if (ghostPrefabIndex != selectedPrefabIndex)
    {
        SpriteRenderer prefabRenderer = prefab.GetComponent<SpriteRenderer>();

        if (prefabRenderer == null)
        {
            ghostPreview.SetActive(false);
            return;
        }

        ghostRenderer.sprite = prefabRenderer.sprite;
        ghostRenderer.flipX = prefabRenderer.flipX;
        ghostRenderer.flipY = prefabRenderer.flipY;

        ghostPreview.transform.localScale = prefab.transform.localScale;
        ghostPreview.transform.rotation = prefab.transform.rotation;

        ghostPrefabIndex = selectedPrefabIndex;
    }

    // Match the position used when actually placing the building.
    Vector2Int footprint = GetFootprint(prefab);

    ghostPreview.transform.position = new Vector3(
        gridOrigin.x + (hoveredCell.x + footprint.x * 0.5f) * cellSize,
        gridOrigin.y + (hoveredCell.y + footprint.y * 0.5f) * cellSize,
        prefab.transform.position.z
    );

    ghostRenderer.color = hoveredCellIsValid
        ? validGhostColor
        : invalidGhostColor;

    ghostPreview.SetActive(true);
}

    private void CreatePlacementPreview() 
    {
        GameObject previewObject = new GameObject("Placement Preview");
        MeshFilter meshFilter = previewObject.AddComponent<MeshFilter>();
        previewRenderer = previewObject.AddComponent<MeshRenderer>();
        previewMesh = new Mesh { name = "Placement Preview" };
        meshFilter.sharedMesh = previewMesh;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find the Sprites/Default shader for the placement preview.", this);
            return;
        }

        previewMaterial = new Material(shader);
        previewRenderer.sharedMaterial = previewMaterial;
        previewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        previewRenderer.receiveShadows = false;
        previewObject.SetActive(false);
    }

    private void UpdatePlacementPreview() 
    {
        if (previewRenderer == null || previewMesh == null)
        {
            return;
        }

        bool inspectingDemolition = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        bool showPreview = hasHoveredCell && selectedPrefabIndex >= 0 && !inspectingDemolition;
        previewRenderer.gameObject.SetActive(showPreview);
        if (!showPreview)
        {
            return;
        }

        float left = gridOrigin.x + hoveredCell.x * cellSize;
        float bottom = gridOrigin.y + hoveredCell.y * cellSize;
        float right = left + hoveredFootprint.x * cellSize;
        float top = bottom + hoveredFootprint.y * cellSize;
        previewMesh.vertices = new[]
        {
            new Vector3(left, bottom, -0.02f),
            new Vector3(right, bottom, -0.02f),
            new Vector3(right, top, -0.02f),
            new Vector3(left, top, -0.02f)
        };
        previewMesh.SetIndices(new[] { 0, 1, 1, 2, 2, 3, 3, 0 }, MeshTopology.Lines, 0);
        previewMesh.RecalculateBounds();
        previewMaterial.color = hoveredCellIsValid
            ? validPlacementColor
            : invalidPlacementColor;
    }

    private void SetRoadSprite(GameObject road, Vector2Int cell, Vector2Int footprint, bool cycle)
    {
        Vector2Int rightCell = cell + Vector2Int.right * footprint.x;
        Vector2Int leftCell = cell + Vector2Int.left;
        Vector2Int upCell = cell + Vector2Int.up * footprint.y;
        Vector2Int downCell = cell + Vector2Int.down;
        bool right = false;
        bool left = false;
        bool up = false;
        bool down = false;

        if (cellOwners.TryGetValue(rightCell, out GameObject possibleRoad) && possibleRoad.name == "road(Clone)")
        {
            right = true;
            if (cycle == false) SetRoadSprite(possibleRoad, rightCell, footprint, true);
        }
        if (cellOwners.TryGetValue(leftCell, out possibleRoad) && possibleRoad.name == "road(Clone)")
        {
            left = true;
            if (cycle == false) SetRoadSprite(possibleRoad, leftCell, footprint, true);
        }
        if (cellOwners.TryGetValue(upCell, out possibleRoad) && possibleRoad.name == "road(Clone)")
        {
            up = true;
            if (cycle == false) SetRoadSprite(possibleRoad, upCell, footprint, true);
        }
        if (cellOwners.TryGetValue(downCell, out possibleRoad) && possibleRoad.name == "road(Clone)")
        {
            down = true;
            if (cycle == false) SetRoadSprite(possibleRoad, downCell, footprint, true);
        }

        if ((right && left && up) || (right && left && down) || (right && up && down) || (left && up && down))
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(10);
        }
        else if (right && up)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(2);
        }
        else if (right && down)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(3);
        }
        else if (left && up)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(4);
        }
        else if (left && down)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(5);
        }
        else if (right && left)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(0);
        }
        else if (up && down)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(1);
        }
        else if (right)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(6);
        }
        else if (left)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(7);
        }
        else if (up)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(8);
        }
        else if (down)
        {
            SpriteChanger spriteChanger = road.GetComponent<SpriteChanger>();
            spriteChanger.ChangeSprite(9);
        }
        else
        {
            // Restore the default road graphic when its last neighbor is removed.
            road.GetComponent<SpriteChanger>().ChangeSprite(0);
        }
    }

    private void OnValidate() 
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
    }
}
