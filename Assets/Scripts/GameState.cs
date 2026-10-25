using UnityEngine;

[DefaultExecutionOrder(-300)]
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }
    // Compatibility view; CurrencySystem is the only owner of money.
    public int Balance => wallet != null ? wallet.Balance : 0;
    public int BuildingCount { get; private set; }
    public int ResidentialCount { get; private set; }
    public int MaxPopulation { get; private set; }
    public int Happiness { get; private set; }
    public int Pollution { get; private set; }
    public float ElapsedSeconds { get; private set; }
    private CurrencySystem wallet;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        // The economy has its own speed controls; don't accelerate camera/physics 120x.
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (wallet == null) wallet = FindAnyObjectByType<CurrencySystem>();
        if (wallet != null) ElapsedSeconds = (float)wallet.ElapsedSeconds;
    }

    public void BuildingPlaced() => BuildingCount++;

    public void HousePlaced(int popInc)
    {
        ResidentialCount++;
        MaxPopulation += Mathf.Max(0, popInc);
    }

    public void BuildingRemoved() => BuildingRemoved(0);
    public void BuildingRemoved(bool residential) => BuildingRemoved(residential ? 1 : 0);

    public void BuildingRemoved(int populationCapacity)
    {
        BuildingCount = Mathf.Max(0, BuildingCount - 1);
        if (populationCapacity > 0)
        {
            ResidentialCount = Mathf.Max(0, ResidentialCount - 1);
            MaxPopulation = Mathf.Max(0, MaxPopulation - populationCapacity);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

