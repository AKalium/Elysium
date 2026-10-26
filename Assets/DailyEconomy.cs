using System;
using UnityEngine;

public enum BuildingCategory { Neither, Residential, Economic }

[RequireComponent(typeof(TimeManager))]
public class DailyEconomy : MonoBehaviour
{
    [Serializable]
    public struct BuildingRates
    {
        [Min(0)] public int purchaseCost;
        [Min(0)] public int dailyIncome;
        [Min(0)] public int dailyUpkeep;
        public BuildingRates(int cost, int income, int upkeep)
        { purchaseCost = cost; dailyIncome = income; dailyUpkeep = upkeep; }
    }

    [SerializeField, Min(0)] private int startingMoney = 1000;
    [SerializeField] private BuildingRates neither = new BuildingRates(10, 0, 2);
    [SerializeField] private BuildingRates residential = new BuildingRates(100, 25, 5);
    [SerializeField] private BuildingRates economic = new BuildingRates(200, 60, 10);
    private TimeManager clock;
    [SerializeField] private CurrencySystem currency;
    private long standaloneBalance;
    public long Balance => currency != null ? currency.Balance : standaloneBalance;
    public long LastIncome { get; private set; }
    public long LastUpkeep { get; private set; }
    public long LastNet => LastIncome - LastUpkeep;
    public event Action<long> BalanceChanged;

    private void Awake()
    {
        clock = GetComponent<TimeManager>();
        if (currency == null) currency = FindAnyObjectByType<CurrencySystem>();
        standaloneBalance = startingMoney;
    }
    private void OnEnable()
    {
        clock.Midnight += SettleDay;
        if (currency != null) currency.BalanceChanged += NotifyBalance;
    }
    private void OnDisable()
    {
        if (clock != null) clock.Midnight -= SettleDay;
        if (currency != null) currency.BalanceChanged -= NotifyBalance;
    }
    private void NotifyBalance(int value) => BalanceChanged?.Invoke(value);

    public BuildingRates GetRates(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Residential: return residential;
            case BuildingCategory.Economic: return economic;
            default: return neither;
        }
    }

    public bool CanAfford(BuildingCategory category) => Balance >= GetRates(category).purchaseCost;
    public bool TryPurchase(BuildingCategory category)
    {
        if (!CanAfford(category)) return false;
        if (currency != null) return currency.TrySpend(GetRates(category).purchaseCost);
        standaloneBalance -= GetRates(category).purchaseCost;
        BalanceChanged?.Invoke(Balance);
        return true;
    }

    private void SettleDay(int day)
    {
        LastIncome = 0;
        LastUpkeep = 0;
        // One scan per day includes pre-placed and newly constructed buildings,
        // excludes prefab assets, and avoids a global registry across scenes.
        foreach (GridPlaceable building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            BuildingRates rates = GetRates(building.Category);
            LastIncome += rates.dailyIncome;
            LastUpkeep += rates.dailyUpkeep;
        }
        // Upkeep may create debt; do not silently discard costs at zero balance.
        if (currency != null) currency.ApplySettlement(LastNet);
        else
        {
            standaloneBalance += LastNet;
            BalanceChanged?.Invoke(Balance);
        }
    }
}
