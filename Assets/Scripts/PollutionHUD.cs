using UnityEngine;
using UnityEngine.UI;

public class PollutionHUD : MonoBehaviour
{
    [SerializeField] private PollutionSystem pollution;
    [SerializeField] private Slider pollutionSlider;
    [SerializeField] private Text pollutionLabel;
    [SerializeField] private Image fillImage;
    [Tooltip("Display range only. Pollution can continue accumulating above this value.")]
    [SerializeField, Min(1)] private int displayMaximum = 100;
    [SerializeField] private Color lowColor = new Color32(51, 139, 95, 255);
    [SerializeField] private Color highColor = new Color32(179, 65, 57, 255);

    private Text happinessLabel;
    private Image happinessFill;
    private GridPlacementSystem grid;
    private float nextRefresh;

    private void Start()
    {
        grid = FindAnyObjectByType<GridPlacementSystem>();
        if (pollutionSlider == null || pollutionLabel == null) return;
        var meter = new GameObject("City Happiness", typeof(RectTransform), typeof(Image));
        var rect = meter.GetComponent<RectTransform>();
        rect.SetParent(pollutionSlider.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(80, 0);
        rect.sizeDelta = new Vector2(240, 24);
        meter.GetComponent<Image>().color = new Color32(28, 39, 50, 255);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        happinessFill = fill.GetComponent<Image>();
        happinessLabel = Instantiate(pollutionLabel, rect);
        happinessLabel.name = "Happiness Label";
        happinessLabel.fontSize = Mathf.Max(18, happinessLabel.fontSize);
        happinessLabel.alignment = TextAnchor.MiddleLeft;
        var label = happinessLabel.rectTransform;
        label.anchorMin = label.anchorMax = new Vector2(0, 1);
        label.pivot = new Vector2(0, 0);
        label.anchoredPosition = new Vector2(0, 6);
        label.sizeDelta = new Vector2(340, 32);
        RefreshHappiness();
    }

    private void RefreshHappiness()
    {
        if (happinessLabel == null || grid == null) return;
        long total = 0, residents = 0;
        foreach (var item in grid.GetPlacedObjects())
        {
            var home = item.Object.GetComponent<GridPlaceable>();
            if (home == null || home.ResidentsProvided <= 0) continue;
            ApartmentStatsUI.CalculateHappiness(grid, home, out _, out int count,
                out _, out _, out _, out _, out int happiness);
            total += (long)happiness * count;
            residents += count;
        }
        float average = residents > 0 ? (float)total / residents : 0;
        happinessLabel.text = residents > 0
            ? $"Happiness: {Mathf.RoundToInt(average)} / 100 {GridPlacementSystem.DailyChange(average, grid.PreviousHappiness, grid.HasPreviousDayStats)}" : "Happiness: No residents";
        happinessFill.rectTransform.anchorMax = new Vector2(average / 100f, 1);
        happinessFill.color = Color.Lerp(highColor, lowColor, average / 100f);
    }

    private void Awake()
    {
        if (pollution == null) pollution = FindAnyObjectByType<PollutionSystem>();
        if (pollutionSlider != null)
        {
            // A read-only meter: dragging must never alter the simulation.
            pollutionSlider.interactable = false;
            pollutionSlider.wholeNumbers = true;
        }
        RefreshDisplay();
    }

    private void LateUpdate()
    {
        RefreshDisplay();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.5f;
        RefreshHappiness();
    }

    public void RefreshDisplay()
    {
        int maximum = Mathf.Max(1, displayMaximum);
        int total = pollution != null ? pollution.TotalPollution : 0;
        if (pollutionSlider != null)
        {
            pollutionSlider.minValue = 0;
            pollutionSlider.maxValue = maximum;
            pollutionSlider.SetValueWithoutNotify(total);
        }
        if (pollutionLabel != null)
        {
            string change = grid != null
                ? GridPlacementSystem.DailyChange(total, grid.PreviousPollution, grid.HasPreviousDayStats) : "(0)";
            pollutionLabel.text = $"Pollution: {total} {change}";
        }
        if (fillImage != null)
            fillImage.color = Color.Lerp(lowColor, highColor, Mathf.Clamp01((float)total / maximum));
    }
}
