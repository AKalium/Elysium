
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CityPollutionHUD : MonoBehaviour
{
    [SerializeField] private PollutionSystem pollution;
    [SerializeField] private GridPlacementSystem grid;
    [SerializeField] private TMP_Text pollutionText;
    [SerializeField] private TMP_Text pollutionStatusText;
    [SerializeField] private Image pollutionBarFill;

    [SerializeField] private int displayMaximum = 200;

    private void Update()
    {
        if (pollution == null || pollutionText == null ||
            pollutionBarFill == null)
            return;

        int total = pollution.TotalPollution;
        float percentage = Mathf.Clamp01((float)total / displayMaximum);

        pollutionBarFill.fillAmount = percentage;

        string status = "No daily data yet";
        string changeText = "";

        if (grid != null && grid.HasPreviousDayStats)
        {
            int change = total - grid.PreviousPollution;

            changeText = $" ({change:+0;-0;0})";

            if (change > 0)
                status = "Increasing+";
            else if (change < 0)
                status = "Decreasing-";
            else
                status = "Stable";
        }

        pollutionText.text = $"Pollution: {total}{changeText}";
        pollutionStatusText.text = status;

if (status == "Increasing")
    pollutionStatusText.color = new Color32(179, 65, 57, 255);
else if (status == "Decreasing")
    pollutionStatusText.color = new Color32(51, 139, 95, 255);
else
    pollutionStatusText.color = new Color32(102, 80, 60, 255);

        pollutionBarFill.color = Color.Lerp(
            new Color32(51, 139, 95, 255),
            new Color32(179, 65, 57, 255),
            percentage
        );
    }
}
