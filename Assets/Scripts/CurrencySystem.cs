using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CurrencySystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingBalance = 10000;
    [SerializeField, Range(0f, 1f)] private float demolitionRefundRate = 0.5f;

    public int Balance { get; private set; }
    public event Action<int> BalanceChanged;
    private Text balanceLabel;
    private Text selectionLabel;
    private GameObject hud;

    private void Awake()
    {
        Balance = Mathf.Max(0, startingBalance);
        CreateHud();
        RefreshBalance();
    }

    public bool CanAfford(int amount) => amount >= 0 && amount <= Balance;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;
        Balance -= amount;
        RefreshBalance();
        BalanceChanged?.Invoke(Balance);
        return true;
    }

    public void AddFunds(int amount)
    {
        if (amount <= 0) return;
        Balance = (int)Math.Min(int.MaxValue, (long)Balance + amount);
        RefreshBalance();
        BalanceChanged?.Invoke(Balance);
    }

    public void RefundDemolition(int paidCost)
    {
        AddFunds((int)Math.Floor(Math.Max(0, paidCost) * (double)Mathf.Clamp01(demolitionRefundRate)));
    }

    public void ShowSelection(GameObject prefab, int cost)
    {
        if (selectionLabel == null) return;
        selectionLabel.text = prefab == null
            ? "Select a building: 1, 2, 3"
            : $"{prefab.name}: {cost:N0} coins" + (CanAfford(cost) ? "" : " (not enough coins)");
        selectionLabel.color = prefab != null && !CanAfford(cost)
            ? new Color(1f, 0.55f, 0.5f) : Color.white;
    }

    public void RefreshBalance()
    {
        if (balanceLabel != null) balanceLabel.text = $"Coins: {Balance:N0}         Buildings: {GameState.Instance.BuildingCount}";
    }

    private void CreateHud()
    {
        hud = new GameObject("Currency HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Currency Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(hud.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(540, 105);
        panel.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = false;
        balanceLabel = CreateLabel(panel.transform, "Balance", 0, 52, 28);
        balanceLabel.color = new Color(1f, 0.85f, 0.4f);
        selectionLabel = CreateLabel(panel.transform, "Building Cost", 52, 48, 20);
        ShowSelection(null, 0);
    }

    private Text CreateLabel(Transform parent, string labelName, float top, float height, int fontSize)
    {
        var labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(16, -top - height);
        rect.offsetMax = new Vector2(-16, -top);
        var label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        return label;
    }

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud);
    }
}
