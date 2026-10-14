using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct BuildingData
{
    public string buildingName;
    public string buildingCost;
    public Sprite buildingIcon;
}

public class ShopManager : MonoBehaviour
{
    [Header("Shop References")]
    [Tooltip("Drag your BuildingCardPanel prefab here")]
    public GameObject cardPrefab;

    [Tooltip("Drag the container (like a panel or layout group) here")]
    public Transform shopContainer;

    [Header("Building Catalog")]
    public List<BuildingData> availableBuildings;

    void Start()
    {
        PopulateShop();
    }

    public void PopulateShop()
    {
        foreach (BuildingData building in availableBuildings)
        {
            // 1. Spawn the card prefab inside the shop container
            GameObject newCard = Instantiate(cardPrefab, shopContainer);

            // 2. Get the BuildingCard script on the spawned card
            BuildingCard cardUI = newCard.GetComponent<BuildingCard>();

            // 3. Set the text and icon data
            if (cardUI != null)
            {
                cardUI.Setup(building.buildingName, building.buildingCost, building.buildingIcon);
            }
        }
    }
}