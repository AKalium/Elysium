
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image buildingIcon;
    [SerializeField] private TMP_Text buildingName;
    [SerializeField] private TMP_Text buildingPrice;
    [SerializeField] private Button selectButton;

    private BuildingData buildingData;
    private TimeManager clock;
    private int displayedPollution = -1;
    [SerializeField] private Image cardBackground;

[SerializeField] private Color normalColor = Color.white;
[SerializeField] private Color selectedColor = new Color(0.85f, 0.65f, 0.45f, 1f);

public void SetSelected(bool selected)
{
    if (cardBackground != null)
    {
        cardBackground.color = selected ? selectedColor : normalColor;
    }
}

    public event Action<BuildingData> OnBuildingSelected;

    public void Setup(BuildingData data, int? placementCost = null)
    {
        if (data == null)
        {
            Debug.LogWarning("BuildingCardUI: Missing building data.");
            return;
        }

        buildingData = data;
        RefreshJobsBadge(data.JobsProvided);
        displayedPollution = -1;
        RefreshPollutionBadge();

        buildingName.text = data.BuildingName;
        int cost = placementCost ?? data.Price;
        buildingPrice.text = cost < 0 ? "Unavailable" :
            $"${cost:N0}\n<size=75%>Income ${data.DailyIncome:N0} / Upkeep ${data.DailyUpkeep:N2}\nNet {EconomyBudgetUI.SignedMoney(data.DailyNet)} / day</size>";
        if (cost >= 0 && data.BuildingId == "apartment")
            buildingPrice.text = $"${cost:N0}\n<size=75%>Residents: workers $6 / others $1 daily\nUpkeep ${data.DailyUpkeep:N2} / day</size>";
        buildingPrice.enableAutoSizing = true;
        buildingPrice.fontSizeMin = 14;
        buildingPrice.fontSizeMax = 18;
        selectButton.interactable = cost >= 0;

        if (data.BuildingIcon != null)
        {
            buildingIcon.sprite = data.BuildingIcon;
            buildingIcon.enabled = true;
        }
        else
        {
            // Hide the placeholder image if no sprite is assigned.
            buildingIcon.enabled = false;
        }
    }

    private void RefreshJobsBadge(int jobs)
    {
        Transform existing = transform.Find("JobsBadge");
        if (existing == null && jobs == 0) return;
        if (existing == null)
        {
            var badge = new GameObject("JobsBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(transform, false);
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(6, -6);
            rect.sizeDelta = new Vector2(32, 32);
            var background = badge.GetComponent<Image>();
            background.color = new Color32(24, 46, 62, 255);
            background.raycastTarget = false;
            var textObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rect, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = buildingName.font;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            existing = rect;
        }
        existing.gameObject.SetActive(jobs > 0);
        existing.SetAsLastSibling();
        existing.GetComponentInChildren<TMP_Text>(true).text = jobs.ToString();
    }

    private void LateUpdate() => RefreshPollutionBadge();

    private void RefreshPollutionBadge()
    {
        if (buildingData == null) return;
        if (clock == null) clock = FindAnyObjectByType<TimeManager>();
        var pollution = buildingData.BuildingPrefab != null
            ? buildingData.BuildingPrefab.GetComponent<BuildingPollution>() : null;
        int amount = pollution != null ? pollution.GetPollutionForDay(clock != null ? clock.Day : 1) : 0;
        if (pollution != null && pollution.PollutionRemovalPerDay > 0)
            amount = -pollution.PollutionRemovalPerDay;
        if (displayedPollution == amount) return;
        displayedPollution = amount;
        Transform existing = transform.Find("PollutionBadge");
        if (existing == null && amount == 0) return;
        if (existing == null)
        {
            var badge = new GameObject("PollutionBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(transform, false);
            badge.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-6, -6);
            rect.sizeDelta = new Vector2(32, 32);
            var background = badge.GetComponent<Image>();
            background.color = new Color32(180, 35, 35, 255);
            background.raycastTarget = false;
            var textObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rect, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = buildingName.font;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            existing = rect;
        }
        existing.gameObject.SetActive(amount != 0);
        existing.GetComponent<Image>().color = amount < 0
            ? new Color32(30, 125, 65, 255) : new Color32(180, 35, 35, 255);
        existing.SetAsLastSibling();
        existing.GetComponentInChildren<TMP_Text>(true).text = Mathf.Abs(amount).ToString();
    }

    private void OnEnable()
    {
        selectButton.onClick.AddListener(HandleSelection);
    }

    private void OnDisable()
    {
        selectButton.onClick.RemoveListener(HandleSelection);
    }

    private void HandleSelection()
    {
        if (buildingData == null)
        {
            Debug.LogWarning("No building assigned to this card.");
            return;
        }

        OnBuildingSelected?.Invoke(buildingData);

        Debug.Log("Selected building: " + buildingData.BuildingName);
    }
}
