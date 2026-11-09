using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingCategory { Neither, Residential, Economic, MayorsOffice }

[RequireComponent(typeof(TimeManager))]
[DefaultExecutionOrder(-100)]
public class DailyEconomy : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingMoney = 1300;
    [SerializeField] private CurrencySystem currency;
    private TimeManager clock;
    private long standaloneBalance;
    public long Balance => currency != null ? currency.Balance : standaloneBalance;
    public double ExactBalance => currency != null ? currency.ExactBalance : standaloneBalance;
    public long LastIncome => LastReport?.Income ?? 0;
    public double LastUpkeep => LastReport?.Upkeep ?? 0;
    public double LastNet => LastIncome - LastUpkeep;
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
        var incomes = new Dictionary<BuildingData, long>();
        var grid = FindAnyObjectByType<GridPlacementSystem>();
        foreach (var building in FindObjectsByType<GridPlaceable>())
        {
            if (!building.isActiveAndEnabled || !building.IsOperational || building.gameObject.scene != gameObject.scene) continue;
            var data = building.Definition;
            if (data == null) continue;
            counts.TryGetValue(data, out int count);
            counts[data] = count + 1;
            int happiness = 0;
            int employed = 0;
            if (data.BuildingId == "apartment" && grid != null &&
                grid.gameObject.scene == building.gameObject.scene)
                ApartmentStatsUI.CalculateHappiness(grid, building, out _, out _,
                    out _, out employed, out _, out _, out happiness);
            incomes.TryGetValue(data, out long income);
            incomes[data] = income + (building.IsApartment
                ? GetCitizenIncome(building.ResidentsProvided, employed, happiness) : 0);
            if (building.IsApartment)
            {
                foreach (var workplace in ApartmentStatsUI.GetWorkplaces(grid, building))
                {
                    var employer = workplace.Key.Definition;
                    incomes.TryGetValue(employer, out long employerIncome);
                    incomes[employer] = employerIncome + GetWorkplaceRevenue(employer, workplace.Value, happiness);
                }
            }
        }
        var lines = new List<BudgetReport.Line>();
        double net = 0;
        foreach (var entry in counts)
        {
            long income = incomes[entry.Key];
            double upkeep = (double)entry.Key.DailyUpkeep * entry.Value;
            lines.Add(new BudgetReport.Line(entry.Key.BuildingName, entry.Value, income, upkeep));
            net += income - upkeep;
        }
        lines.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return new BudgetReport(clock.Day + 1, ExactBalance, Math.Max(int.MinValue, Math.Min(int.MaxValue, ExactBalance + net)), lines);
    }

    public static int HappinessRevenueBonusPercent(int happiness)
    {
        if (happiness >= 90) return 25;
        return happiness > 50 ? 12 : 0;
    }

    public static long GetCitizenIncome(int residents, int employed, int happiness, int workplaceBonus = 0)
    {
        residents = Math.Max(0, residents);
        employed = Math.Max(0, Math.Min(residents, employed));
        long baseIncome = (long)employed * 6 + (residents - employed) + Math.Max(0, workplaceBonus);
        int bonus = HappinessRevenueBonusPercent(happiness);
        // Round each apartment's resident income to the nearest whole currency unit.
        return (baseIncome * (100 + bonus) + 50) / 100;
    }

    public static long GetWorkplaceRevenue(BuildingData employer, int workers, int happiness)
    {
        int extra = employer.BuildingId == "factory" ? 7 : employer.BuildingId == "market" ? 3 : 0;
        long earnings = (long)Math.Max(0, workers) * extra;
        return (earnings * (100 + HappinessRevenueBonusPercent(happiness)) + 50) / 100;
    }

    private void SettleDay(int day)
    {
        var forecast = Forecast();
        double opening = ExactBalance;
        if (currency != null) currency.ApplySettlement(forecast.Net);
        else
        {
            standaloneBalance = BudgetReport.ClampBalance(standaloneBalance, forecast.Net);
            BalanceChanged?.Invoke(Balance);
        }
        // Publish only after the shared wallet has received the settlement.
        LastReport = new BudgetReport(day, opening, ExactBalance, forecast.Lines);
    }

    public void CompleteDayReport(string populationSummary)
    {
        if (LastReport == null) return;
        LastReport = new BudgetReport(LastReport.Day, LastReport.OpeningBalance,
            LastReport.ClosingBalance, LastReport.Lines, populationSummary);
        DaySettled?.Invoke(LastReport);
    }
}
