
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingStoreUI : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData[] buildings;

    [Header("Card References")]
    [SerializeField] private BuildingCardUI[] buildingCards;

    [Header("Economy")]
    [SerializeField] private TemporaryEconomy economy;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text messageText;

    [Header("Category Buttons")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button residentialButton;
    [SerializeField] private Button commercialButton;
    [SerializeField] private Button servicesButton;

    [Header("Store")]
    [SerializeField] private GameObject storePanel;
    [SerializeField] private Button closeButton;
    [Header("Placement Integration")]
    [SerializeField] private BuildingStoreAdapter placementAdapter;

    private void Start()
    {
        if (buildings.Length != buildingCards.Length)
        {
            Debug.LogError("Building count must match card count.");
            return;
        }

        for (int i = 0; i < buildings.Length; i++)
        {
            buildingCards[i].Setup(buildings[i]);
            buildingCards[i].OnBuildingSelected += HandleBuildingSelected;
        }

        allButton.onClick.AddListener(() => FilterBuildings(-1));
        residentialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Residential));
        commercialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Commercial));
        servicesButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Services));

        closeButton.onClick.AddListener(CloseStore);

        economy.OnBalanceChanged += UpdateBalance;
        UpdateBalance(economy.Balance);

        messageText.gameObject.SetActive(false);
    }

    
    private void HandleBuildingSelected(BuildingData building)
    {
    if (!economy.CanAfford(building.Price))
    {
        ShowMessage("Not enough money!");
        return;
    }

    if (placementAdapter == null)
    {
        ShowMessage("Placement system not connected!");
        return;
    }

    if (!placementAdapter.SelectBuilding(building))
    {
        ShowMessage("This building is not available yet!");
        return;
    }

    messageText.gameObject.SetActive(false);

    Debug.Log("Ready to place: " + building.BuildingName);
    }


    private void UpdateBalance(int balance)
    {
        balanceText.text = "$" + balance.ToString("N0");
    }

    private void ShowMessage(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
    }

    private void FilterBuildings(int category)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            bool visible = category == -1 ||
                (int)buildings[i].Category == category;

            buildingCards[i].gameObject.SetActive(visible);
        }
    }

    private void CloseStore()
    {
        storePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buildingCards != null)
        {
            foreach (BuildingCardUI card in buildingCards)
            {
                if (card != null)
                    card.OnBuildingSelected -= HandleBuildingSelected;
            }
        }

        if (economy != null)
            economy.OnBalanceChanged -= UpdateBalance;
    }
}
