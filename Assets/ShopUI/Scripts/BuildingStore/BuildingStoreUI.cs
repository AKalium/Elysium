
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildingStoreUI : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData[] buildings;

    [Header("Card References")]
    [SerializeField] private BuildingCardUI[] buildingCards;

    [Header("Economy")]
    [SerializeField] private CurrencySystem economy;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private GameObject messagePanel;
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

    private Coroutine messageCoroutine;
    private void Start()
    {
        if (buildings.Length != buildingCards.Length)
        {
            Debug.LogError("Building count must match card count.");
            return;
        }

        for (int i = 0; i < buildings.Length; i++)
        {
            buildingCards[i].Setup(buildings[i], placementAdapter != null
                ? (int?)placementAdapter.GetPlacementCost(buildings[i]) : null);
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

        economy.BalanceChanged += UpdateBalance;
        UpdateBalance(economy.Balance);

        messagePanel.SetActive(false);
    }

    
    private void HandleBuildingSelected(BuildingData building)
    {
    int cost = placementAdapter != null ? placementAdapter.GetPlacementCost(building) : -1;
    if (cost < 0)
    {
        ShowMessage("This building is not available yet!");
        return;
    }
    if (!economy.CanAfford(cost))
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

    messagePanel.SetActive(false);

    Debug.Log("Ready to place: " + building.BuildingName);
    }


    private void UpdateBalance(int balance)
    {
        balanceText.text = "$" + balance.ToString("N0");
    }

    public void ShowMessage(string message)
{
    if (messageCoroutine != null)
    {
        StopCoroutine(messageCoroutine);
    }

    messageText.text = message;
    messagePanel.SetActive(true);

    messageCoroutine = StartCoroutine(HideMessageAfterDelay());
}

private IEnumerator HideMessageAfterDelay()
{
    yield return new WaitForSeconds(1.5f);

    messagePanel.SetActive(false);
    messageCoroutine = null;
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
            economy.BalanceChanged -= UpdateBalance;
    }
}
