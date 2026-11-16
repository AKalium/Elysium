
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
    [Header("Building Tooltip")]
    [SerializeField] private BuildingTooltipUI buildingTooltip;

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
    [SerializeField] private GameObject cityBudgetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openStoreButton;
    [Header("Placement Integration")]
    [SerializeField] private BuildingStoreAdapter placementAdapter;

    private Coroutine messageCoroutine;
    private BuildingCardUI selectedCard;
    private void Start()
    {
        if (placementAdapter == null) placementAdapter = FindAnyObjectByType<BuildingStoreAdapter>();
        RegisterResourceBuilding("Bridge");
        RegisterResourceBuilding("StoneBridge");
        RegisterResourceBuilding("WaterPark");
        RegisterResourceBuilding("SolarFactory");
        InitializeCards();
    }

    private void RegisterResourceBuilding(string resource)
    {
        var bridge = Resources.Load<BuildingData>(resource);
        if (bridge != null && placementAdapter != null)
        {
            placementAdapter.RegisterBuilding(bridge);
            if (System.Array.IndexOf(buildings, bridge) < 0)
            {
                System.Array.Resize(ref buildings, buildings.Length + 1);
                buildings[buildings.Length - 1] = bridge;
            }
        }
    }

    private void InitializeCards()
    {
        if (buildingCards.Length > 0 && buildingCards.Length < buildings.Length)
        {
            int existingCount = buildingCards.Length;
            BuildingCardUI template = buildingCards[existingCount - 1];
            System.Array.Resize(ref buildingCards, buildings.Length);
            for (int i = existingCount; i < buildingCards.Length; i++)
                buildingCards[i] = Instantiate(template, template.transform.parent);
        }

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
            buildingCards[i].OnCardHovered += HandleCardHovered;
            buildingCards[i].OnCardUnhovered += HandleCardUnhovered;
        }

        allButton.onClick.AddListener(() => FilterBuildings(-1));
        residentialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Residential));
        commercialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Commercial));
        servicesButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Services));

        closeButton.onClick.AddListener(CloseStore);
        openStoreButton.onClick.AddListener(OpenStore);
        openStoreButton.gameObject.SetActive(!storePanel.activeSelf);

        economy.BalanceChanged += UpdateBalance;
        UpdateBalance(economy.Balance);

        messagePanel.SetActive(false);
    }

    private void HandleCardHovered(BuildingCardUI card, BuildingData data)
    {
        if (buildingTooltip == null) return;

        int cost = placementAdapter != null
            ? placementAdapter.GetPlacementCost(data)
            : data.Price;

        buildingTooltip.Show(data, cost);

        Canvas.ForceUpdateCanvases();

        buildingTooltip.PositionBeside(
            card.GetComponent<RectTransform>()
        );
    }

    private void HandleCardUnhovered(BuildingCardUI card)
    {
        if (buildingTooltip != null)
            buildingTooltip.Hide();
    }

    
    private void HandleBuildingSelected(BuildingData building)
    {
    if (placementAdapter != null && !placementAdapter.HasRequiredMayorsOffice(building))
    {
        ShowMessage(GridPlacementSystem.OfficeRequirementMessage(building));
        return;
    }
    int cost = placementAdapter != null ? placementAdapter.GetPlacementCost(building) : -1;
    if (cost < 0)
    {
        ShowMessage("This building is not available yet!");
        return;
    }
    if (cost > 0 && !economy.CanAfford(cost))
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

    HighlightSelectedCard(building);

    Debug.Log("Ready to place: " + building.BuildingName);
    }

    private void HighlightSelectedCard(BuildingData building)
{
    if (selectedCard != null)
        selectedCard.SetSelected(false);

    selectedCard = null;

    for (int i = 0; i < buildings.Length; i++)
    {
        if (buildings[i] == building)
        {
            selectedCard = buildingCards[i];
            selectedCard.SetSelected(true);
            break;
        }
    }
}
    private void UpdateBalance(int balance)
    {
        balanceText.text = "$" + economy.ExactBalance.ToString("N2");
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
    openStoreButton.gameObject.SetActive(true);

    if (cityBudgetButton != null)
        cityBudgetButton.SetActive(true);
}

    private void OpenStore()
{
    for (int i = 0; i < buildings.Length && i < buildingCards.Length; i++)
        buildingCards[i].Setup(buildings[i], placementAdapter != null
            ? (int?)placementAdapter.GetPlacementCost(buildings[i]) : null);

    storePanel.SetActive(true);
    openStoreButton.gameObject.SetActive(false);

    if (cityBudgetButton != null)
        cityBudgetButton.SetActive(false);
}

    private void OnDestroy()
    {
        if (buildingCards != null)
        {
            foreach (BuildingCardUI card in buildingCards)
            {
                if (card != null)
                {
                    card.OnBuildingSelected -= HandleBuildingSelected;
                    card.OnCardHovered -= HandleCardHovered;
                    card.OnCardUnhovered -= HandleCardUnhovered;
                }
            }
        }

        if (economy != null)
            economy.BalanceChanged -= UpdateBalance;
    }
}
