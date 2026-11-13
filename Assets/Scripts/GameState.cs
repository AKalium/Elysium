using System;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }
    private CurrencySystem currency;
    public int Balance => currency != null ? currency.Balance : 0;
    public int BuildingCount { get; private set; }
    public bool MayorsOfficePlaced { get; private set; }
    public int ResidentialCount { get; private set; }
    public int Population { get; private set; }
    public int MaxPopulation { get; private set; }
    public event Action<int> PopulationChanged;
    public int Happiness { get; private set; }
    public int Pollution { get; private set; }
    public float ElapsedSeconds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1;
        currency = FindAnyObjectByType<CurrencySystem>();
    }
    private void Start() => RefreshPopulation();
    private void Update() => ElapsedSeconds += Time.deltaTime * 4;

    // Rebuild from map contents, including pre-placed houses and externally removed buildings.
    internal bool RestoringMap { get; set; }

    public void RefreshPopulation()
    {
        if (RestoringMap) return;
        int previous = Population;
        int residents = 0;
        BuildingCount = 0;
        ResidentialCount = 0;
        MaxPopulation = 0;
        MayorsOfficePlaced = false;

        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            GridPlaceable placeable = building.GetComponent<GridPlaceable>();
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            if (placeable.Category == BuildingCategory.MayorsOffice) MayorsOfficePlaced = true;
            BuildingCount++;
            if (building.HousingCapacity <= 0) continue;
            MaxPopulation += building.HousingCapacity;
            ResidentialCount++;
            residents += building.ResidentsProvided;
        }
        Population = residents;
        if (previous != Population) PopulationChanged?.Invoke(Population);
    }
    public int CountBuildingsOfType(string buildingTypeId)
{
    if (string.IsNullOrEmpty(buildingTypeId))
        return 0;

    int count = 0;

    foreach (GridPlaceable building in FindObjectsByType<GridPlaceable>(
                 FindObjectsSortMode.None))
    {
        if (building.gameObject.scene != gameObject.scene)
            continue;

        if (building.BuildingTypeId == buildingTypeId)
            count++;
    }

    return count;
}
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
