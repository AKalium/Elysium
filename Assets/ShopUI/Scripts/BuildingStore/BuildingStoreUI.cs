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
    [SerializeField] private Text balanceText; // MEOW
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private Text messageText; // MEOW

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
        if (buildingCards == null || buildingCards.Length == 0)
        {
            buildingCards = GetComponentsInChildren<BuildingCardUI>(true);
        }

        if (closeButton != null) closeButton.onClick.AddListener(CloseStore);
        if (openStoreButton != null)
        {
            openStoreButton.onClick.AddListener(OpenStore);
            if (storePanel != null) openStoreButton.gameObject.SetActive(!storePanel.activeSelf);
        }

        if (allButton != null) allButton.onClick.AddListener(() => FilterBuildings(-1));
        if (residentialButton != null) residentialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Residential));
        if (commercialButton != null) commercialButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Commercial));
        if (servicesButton != null) servicesButton.onClick.AddListener(
            () => FilterBuildings((int)BuildingData.BuildingCategory.Services));

        if (economy != null)
        {
            economy.BalanceChanged += UpdateBalance;
            UpdateBalance(economy.Balance);
        }

        if (messagePanel != null) messagePanel.SetActive(false);

        Debug.Log($"[Store Init] Found {buildings?.Length ?? 0} buildings and {buildingCards?.Length ?? 0} cards.");

        for (int i = 0; i < buildingCards.Length; i++)
        {
            if (buildingCards[i] == null) continue;

            if (buildings != null && i < buildings.Length)
            {
                buildingCards[i].Setup(buildings[i], placementAdapter != null
                    ? (int?)placementAdapter.GetPlacementCost(buildings[i]) : null);
            }

            buildingCards[i].OnBuildingSelected -= HandleBuildingSelected;
            buildingCards[i].OnBuildingSelected += HandleBuildingSelected;
            buildingCards[i].OnCardHovered -= HandleCardHovered;
            buildingCards[i].OnCardHovered += HandleCardHovered;
            buildingCards[i].OnCardUnhovered -= HandleCardUnhovered;
            buildingCards[i].OnCardUnhovered += HandleCardUnhovered;
        }
    }

    private void HandleCardHovered(BuildingCardUI card, BuildingData data)
    {
        if (buildingTooltip == null) return;

        int cost = placementAdapter != null
            ? placementAdapter.GetPlacementCost(data)
            : data.Price;

        buildingTooltip.Show(data, cost);
        Canvas.ForceUpdateCanvases();
        buildingTooltip.PositionBeside(card.GetComponent<RectTransform>());
    }

    private void HandleCardUnhovered(BuildingCardUI card)
    {
        if (buildingTooltip != null)
            buildingTooltip.Hide();
    }

    private void HandleBuildingSelected(BuildingData building)
    {
        Debug.Log("HandleBuildingSelected reached for: " + building.BuildingName);

        if (placementAdapter != null && !placementAdapter.HasRequiredMayorsOffice(building))
        {
            Debug.LogWarning("Blocked: Missing Mayor's Office!");
            ShowMessage(GridPlacementSystem.OfficeRequirementMessage(building));
            return;
        }

        int cost = placementAdapter != null ? placementAdapter.GetPlacementCost(building) : -1;
        if (cost < 0)
        {
            Debug.LogWarning("Blocked: Cost < 0");
            ShowMessage("This building is not available yet!");
            return;
        }

        if (cost > 0 && economy != null && !economy.CanAfford(cost))
        {
            Debug.LogWarning("Blocked: Cannot afford! Cost: " + cost);
            ShowMessage("Not enough money!");
            return;
        }

        if (placementAdapter == null)
        {
            Debug.LogError("Blocked: placementAdapter is NULL!");
            ShowMessage("Placement system not connected!");
            return;
        }

        if (!placementAdapter.SelectBuilding(building))
        {
            Debug.LogWarning("Blocked: SelectBuilding returned false!");
            ShowMessage("This building is not available yet!");
            return;
        }

        if (messagePanel != null) messagePanel.SetActive(false);
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
            if (buildings[i] == building && i < buildingCards.Length)
            {
                selectedCard = buildingCards[i];
                selectedCard.SetSelected(true);
                break;
            }
        }
    }

    private void UpdateBalance(int balance)
    {
        if (balanceText != null && economy != null)
        {
            balanceText.text = "$" + economy.ExactBalance.ToString("N2");
        }
    }

    public void ShowMessage(string message)
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        if (messageText != null) messageText.text = message;
        if (messagePanel != null) messagePanel.SetActive(true);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (messagePanel != null) messagePanel.SetActive(false);
        messageCoroutine = null;
    }

    private void FilterBuildings(int category)
    {
        for (int i = 0; i < buildings.Length && i < buildingCards.Length; i++)
        {
            bool visible = category == -1 || (int)buildings[i].Category == category;
            if (buildingCards[i] != null)
                buildingCards[i].gameObject.SetActive(visible);
        }
    }

    private void CloseStore()
    {
        if (storePanel != null) storePanel.SetActive(false);
        if (openStoreButton != null) openStoreButton.gameObject.SetActive(true);

        if (cityBudgetButton != null)
            cityBudgetButton.SetActive(true);
    }

    private void OpenStore()
    {
        for (int i = 0; i < buildings.Length && i < buildingCards.Length; i++)
        {
            if (buildingCards[i] != null)
            {
                buildingCards[i].Setup(buildings[i], placementAdapter != null
                    ? (int?)placementAdapter.GetPlacementCost(buildings[i]) : null);
            }
        }

        if (storePanel != null) storePanel.SetActive(true);
        if (openStoreButton != null) openStoreButton.gameObject.SetActive(false);

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