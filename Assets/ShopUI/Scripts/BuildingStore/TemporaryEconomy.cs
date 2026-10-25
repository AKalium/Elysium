using System;
using UnityEngine;

// Compatibility bridge: existing scene references keep working, but no second wallet exists.
public class TemporaryEconomy : MonoBehaviour
{
    [SerializeField] private CurrencySystem currency;
    public int Balance { get { ResolveWallet(); return currency != null ? currency.Balance : 0; } }
    public decimal MoneyBalance { get { ResolveWallet(); return currency != null ? currency.MoneyBalance : 0m; } }
    public event Action<int> OnBalanceChanged;
    private bool subscribed;

    private void Start() => ResolveWallet();

    private void ResolveWallet()
    {
        if (currency == null)
        {
            subscribed = false;
            currency = FindAnyObjectByType<CurrencySystem>();
            // The standalone shop test scene has no placement system.
            if (currency == null) currency = gameObject.AddComponent<CurrencySystem>();
        }
        if (!subscribed)
        {
            currency.BalanceChanged += ForwardBalance;
            subscribed = true;
        }
    }

    private void ForwardBalance(int balance) => OnBalanceChanged?.Invoke(balance);

    public bool CanAfford(int price)
    {
        ResolveWallet();
        return currency != null && currency.CanAfford(price);
    }

    public bool TrySpend(int amount)
    {
        ResolveWallet();
        return currency != null && currency.TrySpend(amount);
    }

    private void OnDestroy()
    {
        if (currency != null && subscribed) currency.BalanceChanged -= ForwardBalance;
    }
}

