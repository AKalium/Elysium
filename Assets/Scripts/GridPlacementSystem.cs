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
    [SerializeField] private PerlinNoiseMap terrain;
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
    public Rect WorldBounds => new Rect(gridOrigin, new Vector2(gridWidth, gridHeight) * cellSize);
    public Camera PlacementCamera => targetCamera;
    public bool HasMapHover => hasHoveredCell;
    public Vector2Int HoveredCell => hoveredCell;
    public Vector2Int ToCityCoordinates(Vector2Int cell) => cell - CentralRoadCell;
    public Vector2Int FromCityCoordinates(Vector2Int coordinates) => coordinates + CentralRoadCell;

    //AUDIO
    private AudioSource _Speaker;
    [SerializeField] private AudioClip _Clip;

    public Vector2Int GetBuildingCoordinates(GameObject building)
    {
        return objectCells.TryGetValue(building, out var cells) && cells.Count > 0
            ? ToCityCoordinates(cells[0]) : ToCityCoordinates(WorldToCell(building.transform.position));
    }

    private Vector2Int? startingRoadCell;
    public Vector2Int CentralRoadCell => startingRoadCell ?? new Vector2Int(gridWidth / 2, gridHeight / 2);

    private Vector2Int FindStartingRoadCell()
    {
        var center = new Vector2Int(gridWidth / 2, gridHeight / 2);
        if (!IsWaterCell(center)) return center;

        var closest = center;
        int closestDistance = int.MaxValue;
        for (int x = 0; x < gridWidth; x++)
        for (int y = 0; y < gridHeight; y++)
        {
            int distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
            if (distance >= closestDistance) continue;
            var candidate = new Vector2Int(x, y);
            if (IsWaterCell(candidate)) continue;
            closest = candidate;
            closestDistance = distance;
        }
        return closest;
    }

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
        if (terrain == null) terrain = FindAnyObjectByType<PerlinNoiseMap>();
        var bridge = Resources.Load<BuildingData>("Bridge");
        if (bridge != null) RegisterBuilding(bridge);
        var stoneBridge = Resources.Load<BuildingData>("StoneBridge");
        if (stoneBridge != null) RegisterBuilding(stoneBridge);
        var waterPark = Resources.Load<BuildingData>("WaterPark");
        if (waterPark != null) RegisterBuilding(waterPark);
        var solarFactory = Resources.Load<BuildingData>("SolarFactory");
        if (solarFactory != null) RegisterBuilding(solarFactory);
        startingRoadCell = FindStartingRoadCell();

        //initialize audio
        _Speaker = GetComponent<AudioSource>();

        if(_Speaker == null)
            Debug.Log("Jinkies");
        else
            _Speaker.clip = _Clip;
        
        //rest of the code
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
    if (DisasterSystem.IsCutsceneOpen) return;
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
            if (storeUI != null) storeUI.ShowMessage(OfficeRequirementMessage(definition));
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
        var placeable = prefab.GetComponent<GridPlaceable>();
        if (!CanPlace(cell, footprint, placeable != null && placeable.RequiresWater))
    {
    if (storeUI != null)
    {
        storeUI.ShowMessage("Can't place building here!");
    }

    return false;
}

        if ((placeable == null || !placeable.IsRoad) && !HasCentralRoadAccess(cell, footprint))
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
        placedBuilding?.InitializeConstructionDay(FindAnyObjectByType<TimeManager>()?.Day ?? 1);
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

        if (placedBuilding != null && placedBuilding.IsRoad)
        {
            roadNetworkDirty = true;
            SetRoadSprite(placedObject, cell, footprint, false);    
            RestoreRoadAccess();
        }

        MapVersion++;
        return true;
    }

    public bool TryUpgradeBridge(Vector2Int cell)
    {
        if (!cellOwners.TryGetValue(cell, out var owner) || owner == null) return false;
        var bridge = owner.GetComponent<GridPlaceable>();
        if (bridge == null || !bridge.IsBridge || bridge.IsStoneBridge) return false;
        if (bridge.IsDamaged)
        {
            storeUI?.ShowMessage("Demolish and rebuild damaged bridges before upgrading.");
            return false;
        }
        var stone = Resources.Load<BuildingData>("StoneBridge");
        var source = stone != null && stone.BuildingPrefab != null
            ? stone.BuildingPrefab.GetComponent<SpriteChanger>() : null;
        var changer = owner.GetComponent<SpriteChanger>();
        if (source == null || changer == null || currency == null) return false;
        if (!currency.TrySpend(GridPlaceable.BridgeUpgradeCost))
        {
            storeUI?.ShowMessage("Upgrading a wooden bridge to stone costs $100.");
            return false;
        }
        bridge.UpgradeBridgeDefinition(stone);
        changer.UseSpritesFrom(source);
        paidCosts.TryGetValue(owner, out int paid);
        paidCosts[owner] = paid + GridPlaceable.BridgeUpgradeCost;
        // Keep the existing pause-purchase status: upgrading an old bridge does
        // not make its original cost eligible for a full paused refund.
        var anchor = objectCells[owner][0];
        SetRoadSprite(owner, anchor, bridge.Footprint, false);
        owner.transform.localScale = stone.BuildingPrefab.transform.localScale;
        MapVersion++;
        storeUI?.ShowMessage("Bridge upgraded to flood-resistant stone for $100.");
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
            foreach (var step in TraceStroke(lastDemolitionCell, cell))
                EraseRoadCell(step);
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

    private static IEnumerable<Vector2Int> TraceStroke(Vector2Int from, Vector2Int to)
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
                yield return cell;
            }
            if (doubledError < dx)
            {
                error += dx;
                cell.y += stepY;
                yield return cell;
            }
        }
    }

    private void ResetDemolitionStroke()
    {
        demolitionStrokeActive = false;
        demolitionStrokeVisited.Clear();
    }

    public int RegisterBuilding(BuildingData building)
    {
        int index = System.Array.IndexOf(placeablePrefabs, building.BuildingPrefab);
        if (index >= 0) return index;
        index = placeablePrefabs.Length;
        System.Array.Resize(ref placeablePrefabs, index + 1);
        placeablePrefabs[index] = building.BuildingPrefab;
        return index;
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
            (definition.Category != BuildingData.BuildingCategory.Services && !RequiresLevelTwoOffice(definition))) return true;
        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            if (building.IsMayorsOffice && building.IsOperational &&
                building.OfficeLevel >= (RequiresLevelTwoOffice(definition) ? 2 : 1)) return true;
        }
        return false;
    }

    public static string OfficeRequirementMessage(BuildingData definition) =>
        RequiresLevelTwoOffice(definition)
            ? "Upgrade the Mayor's Office to level 2 to build " + definition.BuildingName + "."
            : "Build a Mayor's Office before constructing service buildings.";

    private static bool RequiresLevelTwoOffice(BuildingData definition) => definition != null &&
        (definition.BuildingId == "recycling_center" || definition.BuildingId == "water_park" ||
            definition.BuildingId == "solar factory");

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
    private bool CanPlace(Vector2Int anchor, Vector2Int footprint, bool bridge)
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
                Vector2Int cell = anchor + new Vector2Int(x, y);
                if (cellOwners.ContainsKey(cell) || IsWaterCell(cell) != bridge)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsWaterCell(Vector2Int cell) => terrain != null && terrain.IsWaterAt(CellToWorld(cell));

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
        if (!IsUsableRoadAt(CentralRoadCell) || (excluded != null && excluded.Contains(CentralRoadCell))) return;
        var frontier = new Queue<Vector2Int>();
        result.Add(CentralRoadCell);
        frontier.Enqueue(CentralRoadCell);
        while (frontier.Count > 0)
        {
            var cell = frontier.Dequeue();
            foreach (var direction in NeighborDirections)
            {
                var next = cell + direction;
                if ((excluded == null || !excluded.Contains(next)) && IsUsableRoadAt(next) && result.Add(next)) frontier.Enqueue(next);
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
        if (IsRoadAt(from)) direct.Add(cellOwners[from]);
        foreach (var cell in TraceStroke(from, to))
            if (IsRoadAt(cell)) direct.Add(cellOwners[cell]);
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
                if (!item.IsRoad && item.Object.GetComponent<GridPlaceable>()?.IsTemporarilyUnavailable != true &&
                    !BordersRoadNetwork(item.Anchor, item.Footprint, remainingNetwork)) targets.Add(item.Object);
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
            if (!IsRoadAt(anchor) && entry.Key.GetComponent<GridPlaceable>()?.IsTemporarilyUnavailable != true &&
                !HasCentralRoadAccess(anchor, GetFootprint(entry.Key))) disconnected.Add(anchor);
        }
        foreach (var anchor in disconnected) DestroyBuilding(anchor);
    }

    private bool IsRoadAt(Vector2Int cell)
    {
        return cellOwners.TryGetValue(cell, out GameObject owner) &&
            owner != null && owner.activeInHierarchy && owner.GetComponent<GridPlaceable>()?.IsRoad == true;
    }

    private bool IsUsableRoadAt(Vector2Int cell) => IsRoadAt(cell) &&
        !cellOwners[cell].GetComponent<GridPlaceable>().IsDamaged;

    // Disaster disconnections preserve occupancy and stored residents. They must
    // also survive demolition of the damaged bridge during its replacement.
    public void RefreshDisasterRoadAccess()
    {
        roadNetworkDirty = true;
        RebuildRoadNetwork();
        foreach (var item in GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building != null && !building.IsRoad)
                building.SetTemporarilyUnavailable(!HasCentralRoadAccess(item.Anchor, item.Footprint));
        }
        MapVersion++;
        GameState.Instance?.RefreshPopulation();
    }

    private void RestoreRoadAccess()
    {
        RebuildRoadNetwork();
        foreach (var item in GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building != null && building.IsTemporarilyUnavailable && HasCentralRoadAccess(item.Anchor, item.Footprint))
                building.SetTemporarilyUnavailable(false);
        }
        GameState.Instance?.RefreshPopulation();
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
        Mouse mouse = Mouse.current;
        if (keyboard == null) 
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame || mouse.rightButton.isPressed) 
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

    public bool IsPointerOverUI(Vector2 pointer)
    {
        var apartmentStats = GetComponent<ApartmentStatsUI>();
        return (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
            (apartmentStats != null && apartmentStats.ContainsScreenPoint(pointer)) ||
            (timeHUD != null && timeHUD.ContainsScreenPoint(pointer)) ||
            (budgetUI != null && budgetUI.ContainsScreenPoint(pointer)) ||
            (roadVisuals != null && roadVisuals.ContainsLegendScreenPoint(pointer));
    }

    private void UpdateHoveredCell() 
    {
        Mouse mouse = Mouse.current;
        hasHoveredCell = false;
        if (mouse == null) return;
        Vector2 pointer = mouse.position.ReadValue();
        if (IsPointerOverUI(pointer)) return;

        Ray ray = targetCamera.ScreenPointToRay(pointer);
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
        var placeable = selectedPrefabIndex >= 0
            ? placeablePrefabs[selectedPrefabIndex].GetComponent<GridPlaceable>()
            : null;
        hoveredCellIsValid = selectedPrefabIndex >= 0 &&
    CanPlace(hoveredCell, hoveredFootprint, placeable != null && placeable.RequiresWater) &&
    !HasReachedBuildingLimit(placeablePrefabs[selectedPrefabIndex]) &&
    HasRequiredMayorsOffice(placeable?.Definition) &&
    ((placeable != null && placeable.IsRoad) ||
        HasCentralRoadAccess(hoveredCell, hoveredFootprint)) &&
        (GetConstructionCost(selectedPrefabIndex) == 0 ||
         currency.CanAfford(GetConstructionCost(selectedPrefabIndex)));

hasHoveredCell = true;
}


    private bool RoadSelected => selectedPrefabIndex >= 0 &&
        placeablePrefabs[selectedPrefabIndex] != null &&
        placeablePrefabs[selectedPrefabIndex].GetComponent<GridPlaceable>()?.IsRoad == true;

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
        // A click on an existing wooden bridge is an upgrade action even when
        // no store item is selected. Holding the mouse does not repeat it.
        if (pressed && pointerOnMap && cellOwners.TryGetValue(cell, out var existing) &&
            existing != null && existing.GetComponent<GridPlaceable>() is GridPlaceable bridge &&
            bridge.IsBridge && !bridge.IsStoneBridge)
        {
            ResetRoadStroke();
            TryUpgradeBridge(cell);
            return;
        }
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
            foreach (var step in TraceStroke(lastRoadCell, cell))
            {
                PaintRoadCell(step);
                if (!roadStrokeActive) break;
            }
            lastRoadCell = cell;
        }
    }

    private void PaintRoadCell(Vector2Int cell)
    {
        var prefab = placeablePrefabs[selectedPrefabIndex];
        if (prefab.GetComponent<GridPlaceable>()?.IsBridge != true && IsWaterCell(cell))
        {
            ResetRoadStroke();
            return;
        }

        if (!roadStrokeVisited.Add(cell) || IsCellOccupied(cell)) return;
        TryPlace(prefab, cell, out _);
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
        gridMesh = new Mesh { name = "Placement Grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        var vertices = new List<Vector3>();
        var waterCells = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                waterCells[x, y] = IsWaterCell(new Vector2Int(x, y));

        for (int x = 0; x <= gridWidth; x++) 
        {
            float worldX = gridOrigin.x + x * cellSize;
            for (int y = 0; y < gridHeight; y++)
            {
                if (x > 0 && x < gridWidth && waterCells[x - 1, y] && waterCells[x, y]) continue;
                float worldY = gridOrigin.y + y * cellSize;
                vertices.Add(new Vector3(worldX, worldY, -0.05f));
                vertices.Add(new Vector3(worldX, worldY + cellSize, -0.05f));
            }
        }

        for (int y = 0; y <= gridHeight; y++) 
        {
            float worldY = gridOrigin.y + y * cellSize;
            for (int x = 0; x < gridWidth; x++)
            {
                if (y > 0 && y < gridHeight && waterCells[x, y - 1] && waterCells[x, y]) continue;
                float worldX = gridOrigin.x + x * cellSize;
                vertices.Add(new Vector3(worldX, worldY, -0.05f));
                vertices.Add(new Vector3(worldX + cellSize, worldY, -0.05f));
            }
        }

        int[] indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        gridMesh.SetVertices(vertices);
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

        ghostRenderer.sprite = prefab.GetComponent<GridPlaceable>()?.IsStoneBridge == true
            ? SpriteChanger.StoneConnectionSprite(3) : prefabRenderer.sprite;
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

        if (IsRoadAt(rightCell))
        {
            right = true;
            if (!cycle) SetRoadSprite(cellOwners[rightCell], rightCell, footprint, true);
        }
        if (IsRoadAt(leftCell))
        {
            left = true;
            if (!cycle) SetRoadSprite(cellOwners[leftCell], leftCell, footprint, true);
        }
        if (IsRoadAt(upCell))
        {
            up = true;
            if (!cycle) SetRoadSprite(cellOwners[upCell], upCell, footprint, true);
        }
        if (IsRoadAt(downCell))
        {
            down = true;
            if (!cycle) SetRoadSprite(cellOwners[downCell], downCell, footprint, true);
        }

        if (road.GetComponent<GridPlaceable>()?.IsBridge == true)
        {
            int connections = (right ? 1 : 0) | (left ? 2 : 0) | (up ? 4 : 0) | (down ? 8 : 0);
            road.GetComponent<SpriteChanger>().ChangeSprite(connections);
            return;
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
