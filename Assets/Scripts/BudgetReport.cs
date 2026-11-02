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
        public long Upkeep { get; }
        public long Net => Income - Upkeep;
        public Line(string name, int count, long income, long upkeep)
        { Name = name; Count = count; Income = income; Upkeep = upkeep; }
    }

    public int Day { get; }
    public long Income { get; }
    public long Upkeep { get; }
    public long Net => Income - Upkeep;
    public long OpeningBalance { get; }
    public long ClosingBalance { get; }
    public IReadOnlyList<Line> Lines { get; }

    public BudgetReport(int day, long openingBalance, long closingBalance, IEnumerable<Line> lines)
    {
        Day = day;
        OpeningBalance = openingBalance;
        ClosingBalance = closingBalance;
        var copy = new List<Line>(lines);
        Lines = copy.AsReadOnly();
        foreach (var line in copy) { Income += line.Income; Upkeep += line.Upkeep; }
    }

    public static long ClampBalance(long balance, long net) =>
        Math.Max(int.MinValue, Math.Min(int.MaxValue, balance + net));
}
