using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingCategory { Neither, Residential, Economic, MayorsOffice }

[RequireComponent(typeof(TimeManager))]
[DefaultExecutionOrder(-100)]
public class DailyEconomy : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingMoney = 10000;
    [SerializeField] private CurrencySystem currency;
    private TimeManager clock;
    private long standaloneBalance;
    public long Balance => currency != null ? currency.Balance : standaloneBalance;
    public long LastIncome => LastReport?.Income ?? 0;
    public long LastUpkeep => LastReport?.Upkeep ?? 0;
    public long LastNet => LastIncome - LastUpkeep;
    public BudgetReport LastReport { get; private set; }
    public event Action<long> BalanceChanged;
    public event Action<BudgetReport> DaySettled;

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

    // Both forecasts and payouts use the exact same active-building snapshot.
    public BudgetReport Forecast()
    {
        var counts = new Dictionary<BuildingData, int>();
        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
            var data = building.Definition;
            if (data == null) continue;
            counts.TryGetValue(data, out int count);
            counts[data] = count + 1;
        }
        var lines = new List<BudgetReport.Line>();
        long net = 0;
        foreach (var entry in counts)
        {
            long income = (long)entry.Key.DailyIncome * entry.Value;
            long upkeep = (long)entry.Key.DailyUpkeep * entry.Value;
            lines.Add(new BudgetReport.Line(entry.Key.BuildingName, entry.Value, income, upkeep));
            net += income - upkeep;
        }
        lines.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return new BudgetReport(clock.Day + 1, Balance, BudgetReport.ClampBalance(Balance, net), lines);
    }

    private void SettleDay(int day)
    {
        var forecast = Forecast();
        long opening = Balance;
        if (currency != null) currency.ApplySettlement(forecast.Net);
        else
        {
            standaloneBalance = BudgetReport.ClampBalance(standaloneBalance, forecast.Net);
            BalanceChanged?.Invoke(Balance);
        }
        // Publish only after the shared wallet has received the settlement.
        LastReport = new BudgetReport(day, opening, Balance, forecast.Lines);
        DaySettled?.Invoke(LastReport);
    }
}
