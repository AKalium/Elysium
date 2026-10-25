using System;

// Money is stored as decimal dollars; no fractional income is truncated.
public sealed class EconomyLedger
{
    public const decimal MaximumBalance = int.MaxValue;
    public decimal Balance { get; private set; }

    public EconomyLedger(decimal startingBalance)
    {
        Balance = Math.Min(MaximumBalance, Math.Max(0m, startingBalance));
    }

    public bool CanAfford(decimal amount) => amount >= 0m && amount <= Balance;

    public bool TrySpend(decimal amount)
    {
        if (!CanAfford(amount)) return false;
        Balance -= amount;
        return true;
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0m) return;
        Balance += Math.Min(amount, MaximumBalance - Balance);
    }
}

// Each placed building gets its own timer, starting at construction time.
public sealed class BuildingIncomeTimer
{
    public decimal Payout { get; }
    public double Interval { get; }
    public double RemainderSeconds { get; private set; }

    public BuildingIncomeTimer(decimal payout, double interval)
    {
        if (payout < 0m) throw new ArgumentOutOfRangeException(nameof(payout));
        if (double.IsNaN(interval) || double.IsInfinity(interval) || interval < 0.01)
            throw new ArgumentOutOfRangeException(nameof(interval));
        Payout = payout;
        Interval = interval;
    }

    public decimal Advance(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return 0m;
        double total = RemainderSeconds + seconds;
        double ticks = Math.Floor((total + 1e-9) / Interval);
        RemainderSeconds = Math.Max(0, total - ticks * Interval);
        if (Payout == 0m || ticks == 0) return 0m;
        // Cap extreme public API inputs before decimal conversion or multiplication.
        decimal cappedTicks = (decimal)Math.Min(ticks, (double)(EconomyLedger.MaximumBalance / Payout));
        return Math.Min(EconomyLedger.MaximumBalance, cappedTicks * Payout);
    }
}

