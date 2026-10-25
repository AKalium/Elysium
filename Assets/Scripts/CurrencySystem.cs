using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CurrencySystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingBalance = 1000;
    [SerializeField, Range(0f, 1f)] private float demolitionRefundRate = 0.5f;

    [SerializeField, Range(0f, 5f)] private float simulationSpeed = 1f;
    private EconomyLedger ledger;
    private readonly HashSet<BuildingIncome> producers = new();
    private Text timeLabel;
    private Text incomeLabel;
    public int Balance => (int)MoneyBalance;
    public decimal MoneyBalance => ledger == null ? 0m : ledger.Balance;
    public float SimulationSpeed => simulationSpeed;
    public double ElapsedSeconds { get; private set; }
    public decimal IncomePerSecond
    {
        get
        {
            decimal total = 0m;
            foreach (var producer in producers)
                if (producer != null && producer.isActiveAndEnabled) total += producer.IncomePerSecond;
            return total;
        }
    }
    public event Action<int> BalanceChanged;
    private Text balanceLabel;
    private Text selectionLabel;
    private GameObject hud;

    private void Awake()
    {
        ledger = new EconomyLedger(Mathf.Max(0, startingBalance));
        CreateHud();
        RefreshBalance();
    }

    public bool CanAfford(int amount) => ledger != null && ledger.CanAfford(amount);

    public bool TrySpend(int amount)
    {
        if (ledger == null || !ledger.TrySpend(amount)) return false;
        NotifyBalanceChanged();
        return true;
    }

    public void AddFunds(int amount) => AddMoney(amount);

    public void AddMoney(decimal amount)
    {
        if (ledger == null || amount <= 0m) return;
        ledger.Credit(amount);
        NotifyBalanceChanged();
    }

    public void RefundDemolition(int paidCost)
    {
        AddMoney(Math.Round(Math.Max(0, paidCost) * (decimal)Mathf.Clamp01(demolitionRefundRate), 2,
            MidpointRounding.AwayFromZero));
    }

    private void NotifyBalanceChanged()
    {
        RefreshBalance();
        BalanceChanged?.Invoke(Balance);
    }

    public void RegisterProducer(BuildingIncome producer)
    {
        if (producer != null) producers.Add(producer);
    }

    public void UnregisterProducer(BuildingIncome producer) => producers.Remove(producer);

    public void SetSimulationSpeed(float speed)
    {
        if (float.IsNaN(speed) || float.IsInfinity(speed)) return;
        simulationSpeed = Mathf.Clamp(speed, 0f, 5f);
    }

    private void Update()
    {
        // Economy speed is independent of camera/physics speed. Respect an external game pause.
        double seconds = Time.timeScale == 0f ? 0d : Time.unscaledDeltaTime * simulationSpeed;
        if (seconds > 0)
        {
            ElapsedSeconds += seconds;
            decimal earned = 0m;
            foreach (var producer in producers)
                if (producer != null && producer.isActiveAndEnabled) earned += producer.Advance(seconds);
            AddMoney(earned);
        }
        if (incomeLabel != null) incomeLabel.text = $"Income: ${IncomePerSecond:N2} / game second";
        if (timeLabel != null)
        {
            var elapsed = TimeSpan.FromSeconds(Math.Min(ElapsedSeconds, TimeSpan.MaxValue.TotalSeconds - 1));
            timeLabel.text = $"Time: {(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}    " +
                (simulationSpeed == 0f ? "Paused" : $"{simulationSpeed:0.#}x");
        }
    }

    public void ShowSelection(GameObject prefab, int cost)
    {
        if (selectionLabel == null) return;
        selectionLabel.text = prefab == null
            ? "1 Apartment | 2 Road | 3 Mayor | 4 Market"
            : $"{prefab.name}: {cost:N0} coins" + (CanAfford(cost) ? "" : " (not enough coins)");
        selectionLabel.color = prefab != null && !CanAfford(cost)
            ? new Color(1f, 0.55f, 0.5f) : Color.white;
    }

    public void RefreshBalance()
    {
        if (balanceLabel != null)
        {
            int count = GameState.Instance != null ? GameState.Instance.BuildingCount : 0;
            balanceLabel.text = $"Money: ${MoneyBalance:N2}    Buildings: {count}";
        }
    }

    private void CreateHud()
    {
        hud = new GameObject("Currency HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        rect.sizeDelta = new Vector2(640, 235);
        panel.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f, 0.92f);
        panel.GetComponent<Image>().raycastTarget = false;
        balanceLabel = CreateLabel(panel.transform, "Balance", 0, 52, 28);
        balanceLabel.color = new Color(1f, 0.85f, 0.4f);
        selectionLabel = CreateLabel(panel.transform, "Building Cost", 52, 48, 20);
        ShowSelection(null, 0);
        incomeLabel = CreateLabel(panel.transform, "Income Rate", 100, 36, 22);
        timeLabel = CreateLabel(panel.transform, "Game Time", 136, 36, 22);
        CreateSpeedButton(panel.transform, "Pause", 16, 0f);
        CreateSpeedButton(panel.transform, "1x", 166, 1f);
        CreateSpeedButton(panel.transform, "2x", 316, 2f);
        CreateSpeedButton(panel.transform, "5x", 466, 5f);
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

    private void CreateSpeedButton(Transform parent, string caption, float left, float speed)
    {
        var buttonObject = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(left, -183);
        rect.sizeDelta = new Vector2(140, 38);
        buttonObject.GetComponent<Image>().color = new Color(0.3f, 0.4f, 0.25f, 1f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(() => SetSimulationSpeed(speed));
        var label = CreateLabel(buttonObject.transform, "Label", 0, 38, 20);
        label.text = caption;
        label.alignment = TextAnchor.MiddleCenter;
    }

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud);
    }
}

