
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

    public event Action<BuildingData> OnBuildingSelected;

    public void Setup(BuildingData data, int? placementCost = null)
    {
        if (data == null)
        {
            Debug.LogWarning("BuildingCardUI: Missing building data.");
            return;
        }

        buildingData = data;

        buildingName.text = data.BuildingName;
        int cost = placementCost ?? data.Price;
        buildingPrice.text = cost < 0 ? "Unavailable" :
            $"${cost:N0}\n<size=75%>Income ${data.DailyIncome:N0} / Upkeep ${data.DailyUpkeep:N0}\nNet {EconomyBudgetUI.SignedMoney(data.DailyNet)} / day</size>";
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
