
using TMPro;
using UnityEngine;

public class BuildingTooltipUI : MonoBehaviour
{
    [SerializeField] private TMP_Text tooltipText;

    public void Show(BuildingData data, int cost)
    {
        if (data == null || tooltipText == null)
            return;

        BuildingPollution pollution = data.BuildingPrefab != null
            ? data.BuildingPrefab.GetComponent<BuildingPollution>()
            : null;
        
        TimeManager clock = FindAnyObjectByType<TimeManager>();

        int currentDay = clock != null ? clock.Day : 1;

        int pollutionGenerated = pollution != null
            ? pollution.GetPollutionForDay(currentDay)
            : 0;

        int pollutionRemoved = pollution != null
            ? pollution.PollutionRemovalPerDay
            : 0;

        int netPollution = pollutionGenerated - pollutionRemoved;


        string income = data.BuildingId == "apartment"
        ? "Depends on residents"
        : data.BuildingId == "factory" || data.BuildingId == "market"
            ? "Depends on workers"
            : data.DailyIncome > 0
                ? $"${data.DailyIncome:N0}/day"
                : "No direct income";

            string pollutionInfo = netPollution > 0
            ? $"+{netPollution}/day"
            : netPollution < 0
                ? $"{netPollution}/day"
                : "None";
        tooltipText.text =
            $"<size=125%><b>{data.BuildingName}</b></size>\n" +
                $"<color=#8A705A>BUILDING INFORMATION</color>\n\n" +
                $"Construction:  <b>${cost:N0}</b>\n" +
                $"Jobs provided:  <b>{data.JobsProvided}</b>\n" +
                $"Daily upkeep:  <b>${data.DailyUpkeep:N2}</b>\n" +
                $"Income:  <b>{income}</b>\n\n" +
                $"<color=#8A705A>ENVIRONMENT</color>\n" +
                $"Pollution:  <b>{pollutionInfo}</b>";
            

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


public void PositionBeside(RectTransform card)
{
    RectTransform tooltip = GetComponent<RectTransform>();
    RectTransform parent = transform.parent as RectTransform;

    if (card == null || parent == null) return;

    Bounds cardBounds =
        RectTransformUtility.CalculateRelativeRectTransformBounds(parent, card);

    float gap = 12f;
    float width = tooltip.rect.width;
    float height = tooltip.rect.height;

    // Determine which column the card belongs to.
    bool isLeftColumn = cardBounds.center.x < parent.rect.center.x;

    // Left column -> tooltip on the right.
    // Right column -> tooltip on the left.
    float x = isLeftColumn
        ? cardBounds.max.x + gap
        : cardBounds.min.x - gap - width;

    // Align the tooltip with the top of the card.
    float y = cardBounds.max.y - height;

    // Keep the tooltip within the Store.
    y = Mathf.Clamp(y, parent.rect.yMin, parent.rect.yMax - height);

    // Account for the tooltip's pivot.
    Vector2 position = new Vector2(
        x + width * tooltip.pivot.x,
        y + height * tooltip.pivot.y
    );

    tooltip.position = parent.TransformPoint(position);
}

}
