using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuildingCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image iconPlaceholder;

    public void Setup(string buildingName, string buildingCost, Sprite buildingIcon)
    {
        if (nameText != null) nameText.text = buildingName;
        if (costText != null) costText.text = buildingCost;
        if (iconPlaceholder != null && buildingIcon != null) iconPlaceholder.sprite = buildingIcon;
    }
}