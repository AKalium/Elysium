using UnityEngine;
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public int Balance { get; private set; } // TRUE BALANCE STORED IN CurrencySystem.cs
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
        Time.timeScale = 120;
    }

    private void Update() {
        ElapsedSeconds += Time.deltaTime;
    }

    public void BuildingPlaced() => BuildingCount++;
    public void HousePlaced(int popInc)
    {
        MaxPopulation++; 
        ResidentialCount =+ popInc;
    }
    public void BuildingRemoved()
    {
        BuildingCount = Mathf.Max(0, BuildingCount - 1);
        ResidentialCount--;
    }
}
