using System;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }
    private CurrencySystem currency;
    public int Balance => currency != null ? currency.Balance : 0;
    public int BuildingCount { get; private set; }
    public int ResidentialCount { get; private set; }
    public int Population { get; private set; }
    // Population currently assumes all housing is occupied; there is no migration model yet.
    public int MaxPopulation => Population;
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
    public void RefreshPopulation()
    {
        int previous = Population;
        int residents = 0;
        BuildingCount = 0;
        ResidentialCount = 0;
        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            BuildingCount++;
            if (building.ResidentsProvided <= 0) continue;
            ResidentialCount++;
            residents += building.ResidentsProvided;
        }
        Population = residents;
        if (previous != Population) PopulationChanged?.Invoke(Population);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
