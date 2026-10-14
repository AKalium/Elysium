
using System;
using UnityEngine;

public class TemporaryEconomy : MonoBehaviour
{
    [SerializeField] private int startingBalance = 10000;

    public int Balance { get; private set; }

    public event Action<int> OnBalanceChanged;

    private void Awake()
    {
        Balance = startingBalance;
    }

    public bool CanAfford(int price)
    {
        return price >= 0 && Balance >= price;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;

        Balance -= amount;
        OnBalanceChanged?.Invoke(Balance);

        return true;
    }
}
