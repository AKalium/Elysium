using TMPro;
using UnityEngine;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private CurrencySystem currency;
    [SerializeField] private TMP_Text coinsText;

    private void Start()
    {
        currency.BalanceChanged += UpdateCoins;
        UpdateCoins(currency.Balance);
    }

    private void UpdateCoins(int balance)
    {
        coinsText.text = "$" + balance.ToString("N0");
    }

    private void OnDestroy()
    {
        if (currency != null)
        {
            currency.BalanceChanged -= UpdateCoins;
        }
    }
}
