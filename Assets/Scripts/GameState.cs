using UnityEngine;
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    private CurrencySystem currency;
    public int Balance => currency != null ? currency.Balance : 0;
    public int BuildingCount { get; private set; }
    public int ResidentialCount { get; private set; }
    public int MaxPopulation { get; private set; }
    public int Happiness { get; private set; }
    public int Pollution { get; private set; }
    public float ElapsedSeconds { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currency = FindAnyObjectByType<CurrencySystem>();
    }

    private void Update() {
        ElapsedSeconds += Time.deltaTime;
    }

    public void BuildingPlaced() => BuildingCount++;
    public void HousePlaced(int popInc)
    {
        MaxPopulation += popInc;
        ResidentialCount++;
    }
    public void BuildingRemoved(bool wasHouse = false)
    {
        BuildingCount = Mathf.Max(0, BuildingCount - 1);
        if (wasHouse)
        {
            ResidentialCount = Mathf.Max(0, ResidentialCount - 1);
            MaxPopulation = Mathf.Max(0, MaxPopulation - 1);
        }
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
