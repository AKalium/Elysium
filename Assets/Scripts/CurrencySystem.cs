using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class CurrencySystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingBalance = 1300;
    [SerializeField, Range(0f, 1f)] private float demolitionRefundRate = 0.5f;

    public int Balance { get; private set; }
    private double fractionalBalance;
    public double ExactBalance => Balance + fractionalBalance;
    public event Action<int> BalanceChanged;

    private void Awake()
{
    Balance = startingBalance;
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
    public void ApplySettlement(double amount)
    {
        double exact = Math.Max(int.MinValue, Math.Min(int.MaxValue, ExactBalance + amount));
        RestoreBalance(exact);
    }

    internal void RestoreBalance(double exact)
    {
        Balance = (int)Math.Floor(exact);
        fractionalBalance = exact - Balance;

        BalanceChanged?.Invoke(Balance);
    }

    public int CalculateDemolitionRefund(int paidCost) =>
        (int)Math.Floor(Math.Max(0, paidCost) * (double)Mathf.Clamp01(demolitionRefundRate));

    public void RefundDemolition(int paidCost) => AddFunds(CalculateDemolitionRefund(paidCost));

}
