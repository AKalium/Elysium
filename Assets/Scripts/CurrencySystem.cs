using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class CurrencySystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingBalance = 1000;
    [SerializeField, Range(0f, 1f)] private float demolitionRefundRate = 0.5f;

    public int Balance { get; private set; }
    public event Action<int> BalanceChanged;

    private void Awake()
    {
        Balance = Mathf.Max(0, startingBalance);
    }

    public bool CanAfford(int amount) => amount >= 0 && amount <= Balance;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;
        Balance -= amount;

        BalanceChanged?.Invoke(Balance);
        return true;
    }

    public void AddFunds(int amount)
    {
        if (amount <= 0) return;
        ApplySettlement(amount);
    }

    // Daily upkeep may create debt; purchases still require sufficient funds.
    public void ApplySettlement(long amount)
    {
        Balance = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, (long)Balance + amount));

        BalanceChanged?.Invoke(Balance);
    }

    public void RefundDemolition(int paidCost)
    {
        AddFunds((int)Math.Floor(Math.Max(0, paidCost) * (double)Mathf.Clamp01(demolitionRefundRate)));
    }

}
