using UnityEngine;
using UnityEngine.UI;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private CurrencySystem currency;
    [SerializeField] private Text coinsText;
    [SerializeField] private Font pixelFont;

    [Header("Employment Bar")]
    [SerializeField] private Image employmentFill;
    [SerializeField] private Text employmentText;

    [Header("Occupancy Bar")]
    [SerializeField] private Image occupancyFill;
    [SerializeField] private Text occupancyText;

    private GridPlacementSystem grid;
    private float nextRefresh;

    private void Start()
    {
        grid = FindAnyObjectByType<GridPlacementSystem>();

        if (currency != null)
        {
            currency.BalanceChanged += UpdateCoins;
            UpdateCoins(currency.Balance);
        }
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh || grid == null) return;
        nextRefresh = Time.unscaledTime + 0.25f;

        int residents = 0, capacity = 0, workers = 0, employed = 0;
        foreach (var item in grid.GetPlacedObjects())
        {
            var home = item.Object.GetComponent<GridPlaceable>();
            if (home == null || !home.IsApartment) continue;
            residents += home.ResidentsProvided;
            capacity += home.HousingCapacity;
            workers += Mathf.CeilToInt(home.ResidentsProvided * 0.66f);
            foreach (var assignment in ApartmentStatsUI.GetWorkplaces(grid, home))
                employed += assignment.Value;
        }

        float employment = workers > 0 ? (float)employed / workers : 0;
        float occupancy = capacity > 0 ? (float)residents / capacity : 0;

        if (employmentFill != null)
            employmentFill.rectTransform.anchorMax = new Vector2(employment, 1);
        if (occupancyFill != null)
            occupancyFill.rectTransform.anchorMax = new Vector2(occupancy, 1);

        if (employmentText != null)
            employmentText.text = $"Employment: {employed}/{workers} ({employment:P0})";

        if (occupancyText != null)
            occupancyText.text = $"Occupancy: {residents}/{capacity} ({occupancy:P0})";
    }

    private void UpdateCoins(int balance)
    {
        if (coinsText != null && currency != null)
        {
            coinsText.text = "$" + currency.ExactBalance.ToString("N2");
        }
    }

    private void OnDestroy()
    {
        if (currency != null)
        {
            currency.BalanceChanged -= UpdateCoins;
        }
    }
}