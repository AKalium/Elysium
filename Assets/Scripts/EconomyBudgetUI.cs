using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Editable prefab view; daily reports pause simulation until dismissed.</summary>
public sealed class EconomyBudgetUI : MonoBehaviour
{
    [SerializeField] private DailyEconomy economy;
    [SerializeField] private GameObject budgetPanel;
    [SerializeField] private Text budgetText;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private Text reportHeading;
    [SerializeField] private Text reportText;
    [SerializeField] private Button budgetButton;
    [SerializeField] private Button closeBudgetButton;
    [SerializeField] private Button closeReportButton;
    [SerializeField] private ScrollRect reportScroll;
    private readonly Queue<BudgetReport> pending = new Queue<BudgetReport>();
    private float nextRefresh;
    private TimeManager pausedClock;
    private bool ownsPause;
    private bool previousClockPaused;
    private float previousTimeScale;
    public bool IsReportOpen => reportPanel != null && reportPanel.activeSelf;
    public int PendingReports => pending.Count;
    public string ReportText => reportText != null ? reportText.text : "";
    public string BudgetText => budgetText != null ? budgetText.text : "";

    public bool ContainsScreenPoint(Vector2 position)
    {
        if (!isActiveAndEnabled) return false;
        var canvas = GetComponent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return Contains(reportPanel, position, camera) || Contains(budgetPanel, position, camera) ||
            (budgetButton != null && Contains(budgetButton.gameObject, position, camera));
    }

    private static bool Contains(GameObject target, Vector2 position, Camera camera) =>
        target != null && target.activeInHierarchy && target.transform is RectTransform rect &&
        RectTransformUtility.RectangleContainsScreenPoint(rect, position, camera);

    private void Start()
    {
        if (budgetPanel == null) return;
        var rect = budgetPanel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -125);
    }

    private void OnEnable()
    {
        if (economy == null) economy = FindAnyObjectByType<DailyEconomy>();
        if (economy != null) economy.DaySettled += ShowReport;
        if (budgetButton != null) budgetButton.onClick.AddListener(ToggleBudget);
        if (closeBudgetButton != null) closeBudgetButton.onClick.AddListener(CloseBudget);
        if (closeReportButton != null) closeReportButton.onClick.AddListener(DismissReport);
        RefreshBudget();
    }
    private void OnDisable()
    {
        if (economy != null) economy.DaySettled -= ShowReport;
        if (budgetButton != null) budgetButton.onClick.RemoveListener(ToggleBudget);
        if (closeBudgetButton != null) closeBudgetButton.onClick.RemoveListener(CloseBudget);
        if (closeReportButton != null) closeReportButton.onClick.RemoveListener(DismissReport);
        pending.Clear();
        if (reportPanel != null) reportPanel.SetActive(false);
        RestoreSimulation();
    }
    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        if (budgetPanel != null && budgetPanel.activeSelf) RefreshBudget();
    }
    private void ToggleBudget()
    {
        budgetPanel.SetActive(!budgetPanel.activeSelf);
        RefreshBudget();
    }
    public void CloseBudget() => budgetPanel.SetActive(false);
    public static string SignedMoney(double value) => value < 0 ? $"-${-value:N2}" : $"+${value:N2}";
    private static string Money(double value) => value < 0 ? $"-${-value:N2}" : $"${value:N2}";

    public void RefreshBudget()
    {
        if (economy == null || budgetText == null) return;
        var forecast = economy.Forecast();
        var text = new StringBuilder();
        text.AppendLine("<b>NEXT DAILY SETTLEMENT</b>");
        text.AppendLine($"Income                 {Money(forecast.Income)}");
        text.AppendLine($"Upkeep                -{Money(forecast.Upkeep)}");
        text.AppendLine($"<b>Net                         {SignedMoney(forecast.Net)}</b>");
        text.AppendLine($"Balance now           {Money(economy.ExactBalance)}");
        text.AppendLine($"<b>Projected balance    {Money(forecast.ClosingBalance)}</b>");
        text.AppendLine("<size=13>Estimate assumes the current buildings remain active.</size>");
        if (economy.LastReport == null) text.AppendLine("\nNo daily settlement yet.");
        else
        {
            var last = economy.LastReport;
            text.AppendLine($"\n<b>LAST SETTLEMENT · DAY {last.Day}</b>");
            text.AppendLine($"Income {Money(last.Income)}   Upkeep {Money(last.Upkeep)}");
            text.AppendLine($"Net {SignedMoney(last.Net)}   Balance {Money(last.ClosingBalance)}");
        }
        budgetText.text = text.ToString();
    }

    public void ShowReport(BudgetReport report)
    {
        if (IsReportOpen) { pending.Enqueue(report); return; }
        RenderReport(report);
    }
    private static string GetStartingDayReminder(int day)
    {
        switch (day)
        {
            case 3:
                return "Starting today, apartments with at least 5 residents and happiness below 30 lose 2 residents at the end of each day. The first check is at the end of day 3.";
            case 6:
                return "Starting today, apartments with at least 5 residents and no employed residents lose 2 residents at the end of each day. The first check is at the end of day 6. This stacks with the low-happiness penalty: qualifying for both removes 4 residents.";
            case 31:
                return "The introductory factory pollution rate has ended. Each factory now produces 16 pollution per day, up from 8. The higher emissions are first added at the end of day 31.";
            default:
                return null;
        }
    }

    private void RenderReport(BudgetReport report)
    {
        PauseSimulation();
        reportHeading.text = $"DAY {report.Day} · DAILY BUDGET";
        var text = new StringBuilder();
        text.AppendLine($"<b>Day {report.Day} begins</b> — results below cover day {report.Day - 1}.");
        string reminder = GetStartingDayReminder(report.Day);
        if (reminder != null)
        {
            text.AppendLine("\n<size=22><color=#FFD780><b>NEW RULE TODAY</b></color></size>");
            text.AppendLine(reminder);
            text.AppendLine();
        }
        text.AppendLine($"<size=28><b>{(report.Net >= 0 ? "Net earned" : "Net loss")}  {SignedMoney(report.Net)}</b></size>");
        text.AppendLine($"Income earned   {Money(report.Income)}");
        text.AppendLine($"Upkeep paid       {Money(report.Upkeep)}");
        text.AppendLine($"Balance             {Money(report.OpeningBalance)} → {Money(report.ClosingBalance)}");
        if (!string.IsNullOrEmpty(report.PopulationSummary))
        {
            text.AppendLine("\n<b>POPULATION CHANGES</b>");
            text.AppendLine(report.PopulationSummary);
        }
        text.AppendLine("\n<b>BUILDING BREAKDOWN</b>");
        if (report.Lines.Count == 0) text.AppendLine("No active buildings. No income or upkeep.");
        foreach (var line in report.Lines)
        {
            text.AppendLine($"\n<b>{line.Name} × {line.Count}</b>   {SignedMoney(line.Net)} net");
            text.AppendLine($"Income {Money(line.Income)}    Upkeep {Money(line.Upkeep)}");
        }
        text.AppendLine("\n<size=14>Settled at midnight for the buildings active at that time.\nGame paused. Select Continue to return to the city.</size>");
        reportText.text = text.ToString();
        reportPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (reportScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(reportScroll.content);
            reportScroll.StopMovement();
            // Content is anchored and pivoted at the top. Reset directly after sizing
            // so a queued report never inherits the previous report's scroll offset.
            reportScroll.content.anchoredPosition = Vector2.zero;
        }
        RefreshBudget();
    }
    public void DismissReport()
    {
        if (pending.Count > 0) RenderReport(pending.Dequeue());
        else
        {
            if (reportPanel != null) reportPanel.SetActive(false);
            RestoreSimulation();
        }
    }

    private void PauseSimulation()
    {
        // Queued reports share one pause; never overwrite the original state.
        if (ownsPause) return;
        pausedClock = economy != null ? economy.GetComponent<TimeManager>() : FindAnyObjectByType<TimeManager>();
        previousClockPaused = pausedClock != null && pausedClock.Paused;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        if (pausedClock != null) pausedClock.Paused = true;
        Time.timeScale = 0f;
    }

    private void RestoreSimulation()
    {
        if (!ownsPause) return;
        if (pausedClock != null) pausedClock.Paused = previousClockPaused;
        if (previousTimeScale > 0f) FindAnyObjectByType<GridPlacementSystem>()?.EndPauseRefunds();
        Time.timeScale = previousTimeScale;
        pausedClock = null;
        ownsPause = false;
    }
}
