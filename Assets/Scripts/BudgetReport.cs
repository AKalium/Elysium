using System;
using System.Collections.Generic;

/// <summary>A snapshot: later construction cannot change an already settled report.</summary>
public sealed class BudgetReport
{
    public sealed class Line
    {
        public string Name { get; }
        public int Count { get; }
        public long Income { get; }
        public double Upkeep { get; }
        public double Net => Income - Upkeep;
        public Line(string name, int count, long income, double upkeep)
        { Name = name; Count = count; Income = income; Upkeep = upkeep; }
    }

    public string PopulationSummary { get; }
    public int Day { get; }
    public long Income { get; }
    public double Upkeep { get; }
    public double Net => Income - Upkeep;
    public double OpeningBalance { get; }
    public double ClosingBalance { get; }
    public IReadOnlyList<Line> Lines { get; }

    public BudgetReport(int day, double openingBalance, double closingBalance, IEnumerable<Line> lines, string populationSummary = null)
    {
        Day = day;
        PopulationSummary = populationSummary;
        OpeningBalance = openingBalance;
        ClosingBalance = closingBalance;
        var copy = new List<Line>(lines);
        Lines = copy.AsReadOnly();
        foreach (var line in copy) { Income += line.Income; Upkeep += line.Upkeep; }
    }

    public static long ClampBalance(long balance, double net) =>
        (long)Math.Max(int.MinValue, Math.Min(int.MaxValue, balance + net));
}
