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


    private GridPlacementSystem grid;




    private void Awake()
    {   grid = FindAnyObjectByType<GridPlacementSystem>();
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
