using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private CurrencySystem currency;
    [SerializeField] private TMP_Text coinsText;

    private GridPlacementSystem grid;
    private Image employmentFill;
    private Image occupancyFill;
    private TMP_Text employmentText;
    private TMP_Text occupancyText;
    private float nextRefresh;

    private void CreateRateBar(string name, float y, Color color, out Image fill, out TMP_Text label)
    {
        var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = bar.GetComponent<RectTransform>();
        rect.SetParent(coinsText.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(16, y);
        rect.sizeDelta = new Vector2(340, 24);
        bar.GetComponent<Image>().color = new Color32(22, 34, 47, 255);
        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill = fillObject.GetComponent<Image>();
        fill.color = color;
        fill.raycastTarget = false;
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label = labelObject.GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = coinsText.font;
        label.fontSize = 16;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
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
        employmentFill.rectTransform.anchorMax = new Vector2(employment, 1);
        occupancyFill.rectTransform.anchorMax = new Vector2(occupancy, 1);
        employmentText.text = $"Employment: {employed}/{workers} ({employment:P0}) {GridPlacementSystem.DailyChange(employment * 100, grid.PreviousEmployment, grid.HasPreviousDayStats, " pp")}";
        occupancyText.text = $"Occupancy: {residents}/{capacity} ({occupancy:P0}) {GridPlacementSystem.DailyChange(occupancy * 100, grid.PreviousOccupancy, grid.HasPreviousDayStats, " pp")}";
    }

    private void Start()
    {
        grid = FindAnyObjectByType<GridPlacementSystem>();
        CreateRateBar("Employment", 15, new Color32(36, 105, 146, 255), out employmentFill, out employmentText);
        CreateRateBar("Occupancy", -15, new Color32(45, 115, 74, 255), out occupancyFill, out occupancyText);
        currency.BalanceChanged += UpdateCoins;
        UpdateCoins(currency.Balance);
    }

    private void UpdateCoins(int balance)
    {
        coinsText.text = "$" + currency.ExactBalance.ToString("N2");
    }

    private void OnDestroy()
    {
        if (currency != null)
        {
            currency.BalanceChanged -= UpdateCoins;
        }
    }
}
