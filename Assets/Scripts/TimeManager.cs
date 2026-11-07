using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>Scene-local clock. Other game systems can subscribe to Midnight.</summary>
public class TimeManager : MonoBehaviour
{
    [SerializeField, Min(1f)] private float secondsPerDay = 300f;
    [SerializeField, Range(0, 23)] private int startingHour = 8;
    [SerializeField, Min(0f)] private float speed = 1f;
    private double minuteOfDay;
    private bool keyboardPaused;
    private float resumeTimeScale = 1f;


    public int Day { get; private set; } = 1;
    public int Hour => (int)minuteOfDay / 60;
    public int Minute => (int)minuteOfDay % 60;
    public bool Paused { get; set; }
    public float Speed => speed;

    public void CycleSpeed()
    {
        speed = speed < 2f ? 2f : speed < 4f ? 4f : 1f;
    }
    public event Action<int> Midnight;
    public string DisplayTime => $"{(Hour % 12 == 0 ? 12 : Hour % 12)}:{Minute:00} {(Hour < 12 ? "AM" : "PM")}";

    private void Awake() => minuteOfDay = startingHour * 60;
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame))
        {
            var focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool editingText = focused != null &&
                (focused.GetComponent<InputField>() != null || focused.GetComponent<TMP_InputField>() != null);
            var budget = FindAnyObjectByType<EconomyBudgetUI>();
            if (!editingText)
            {
                //if (keyboard.dKey.wasPressedThisFrame) CycleSpeed();
                if (keyboard.spaceKey.wasPressedThisFrame && (budget == null || !budget.IsReportOpen))
                    ToggleKeyboardPause();
            }
        }
        AdvanceTime(Time.deltaTime);
    }

    private void ToggleKeyboardPause()
    {
        if (keyboardPaused)
        {
            FindAnyObjectByType<GridPlacementSystem>()?.EndPauseRefunds();
            keyboardPaused = false;
            Paused = false;
            Time.timeScale = resumeTimeScale;
            return;
        }
        if (Paused || Time.timeScale == 0f) return;
        resumeTimeScale = Time.timeScale;
        keyboardPaused = true;
        Paused = true;
        Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        if (keyboardPaused) Time.timeScale = resumeTimeScale;
    }


    // Public for simulation/testing; scaled delta time also respects Unity's pause.
    public void AdvanceTime(double elapsedSeconds)
    {
        if (Paused || elapsedSeconds <= 0 || double.IsNaN(elapsedSeconds) ||
            double.IsInfinity(elapsedSeconds)) return;
        double minutes = elapsedSeconds * 1440d / Math.Max(1d, secondsPerDay) * Math.Max(0d, speed);
        while (minutes >= 1440d - minuteOfDay)
        {
            minutes -= 1440d - minuteOfDay;
            minuteOfDay = 0;
            var grid = FindAnyObjectByType<GridPlacementSystem>();
            if (grid != null && grid.gameObject.scene == gameObject.scene) grid.CapturePreviousDayStats();
            Day++;
            Midnight?.Invoke(Day);
            var economy = GetComponent<DailyEconomy>();
            string populationSummary = null;
            if (grid != null && grid.gameObject.scene == gameObject.scene)
                populationSummary = grid.UpdateDailyPopulation(Day - 1, economy?.LastNet ?? 0);
            economy?.CompleteDayReport(populationSummary);
            // A daily report may pause during the event. Stop at midnight even
            // when a long frame would otherwise advance through several days.
            if (Paused) return;
        }
        minuteOfDay += minutes;
    }
}
