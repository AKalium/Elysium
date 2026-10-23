
using System;
using UnityEngine;

public class TemporaryEconomy : MonoBehaviour
{
    [SerializeField] private int startingBalance = 10000;

    private CurrencySystem currency;
    private int standaloneBalance;
    public int Balance => currency != null ? currency.Balance : standaloneBalance;

    public event Action<int> OnBalanceChanged;

    private void Awake()
    {
        currency = FindAnyObjectByType<CurrencySystem>();
        standaloneBalance = startingBalance;
        if (currency != null) currency.BalanceChanged += ForwardBalance;
    }

    public bool CanAfford(int price)
    {
        return price >= 0 && Balance >= price;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;

        if (currency != null) return currency.TrySpend(amount);
        standaloneBalance -= amount;
        OnBalanceChanged?.Invoke(Balance);

        return true;
    }

    private void ForwardBalance(int balance) => OnBalanceChanged?.Invoke(balance);
    private void OnDestroy()
    {
        if (currency != null) currency.BalanceChanged -= ForwardBalance;
    }
}
