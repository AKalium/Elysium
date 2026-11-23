using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Mathematics;

/// <summary>Scene-local clock. Other game systems can subscribe to Midnight.</summary>
public class TimeManager : MonoBehaviour
{
    [SerializeField, Min(1f)] private float secondsPerDay = 300f;
    [SerializeField, Range(0, 23)] private int startingHour = 8;
    [SerializeField, Min(0f)] private float speed = 1f;
    [SerializeField, Range(0f, 1f)] private float nightOverlayOpacity = 0.3f;
    [SerializeField] private Color nightOverlayColor = new Color(0.08f, 0.11f, 0.28f);
    [SerializeField, Range(0, 23)] private int dawnStartHour = 5;
    [SerializeField, Range(0, 23)] private int sunriseHour = 7;
    [SerializeField, Range(0, 23)] private int sunsetHour = 18;
    [SerializeField, Range(0, 23)] private int nightStartHour = 20;
    private double minuteOfDay;
    private Image nightOverlay;
    private bool keyboardPaused;
    private float resumeTimeScale = 1f;

    public int Day { get; private set; } = 1;
    private bool settlingDay;
    public int SimulationDay => settlingDay ? Day - 1 : Day;
    public int Hour => (int)minuteOfDay / 60;
    public int Minute => (int)minuteOfDay % 60;
    public bool Paused { get; set; }
    public float Speed => speed;
    internal double MinuteOfDay => minuteOfDay;
    internal bool KeyboardPaused => keyboardPaused;

    internal void RestoreClock(CitySave save)
    {
        Day = save.day;
        minuteOfDay = save.minuteOfDay;
        speed = save.clockSpeed;
        if (save.keyboardPaused) ToggleKeyboardPause();
    }

    public void CycleSpeed()
    {
        speed = speed < 2f ? 2f : speed < 4f ? 4f : speed < 8f ? 8f : 1f;
    }
    public event Action<int> Midnight;
    public string DisplayTime => $"{(Hour % 12 == 0 ? 12 : Hour % 12)}:{Minute:00} {(Hour < 12 ? "AM" : "PM")}";

    private void Awake()
    {
        minuteOfDay = startingHour * 60;
        CreateNightOverlay();
        UpdateDaylight();
    }

    private void CreateNightOverlay()
    {
        var canvasObject = new GameObject("DayNightOverlay", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -1;

        var overlayObject = new GameObject("NightTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        var rect = (RectTransform)overlayObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        nightOverlay = overlayObject.GetComponent<Image>();
        nightOverlay.raycastTarget = false;
        nightOverlay.color = Color.clear;
    }

    private void Update()
    {
        if (DisasterSystem.IsCutsceneOpen || CityPauseMenu.BlocksGameplayInput) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame))
        {
            var focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            bool editingText = focused != null && focused.GetComponent<InputField>() != null;
            var budget = FindAnyObjectByType<EconomyBudgetUI>();
            if (!editingText)
            {
                if (keyboard.tKey.wasPressedThisFrame) CycleSpeed();
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
            var economy = GetComponent<DailyEconomy>();
            string populationSummary = null;
            settlingDay = true;
            try
            {
                Midnight?.Invoke(Day);
                if (grid != null && grid.gameObject.scene == gameObject.scene)
                    populationSummary = grid.UpdateDailyPopulation(Day - 1, economy?.LastNet ?? 0);
            }
            finally { settlingDay = false; }
            economy?.CompleteDayReport(populationSummary);
            // A daily report may pause during the event. Stop at midnight even
            // when a long frame would otherwise advance through several days.
            if (Paused)
            {
                UpdateDaylight();
                return;
            }
        }
        minuteOfDay += minutes;
        UpdateDaylight();
    }

    private void UpdateDaylight()
    {
        float hour = (float)(minuteOfDay / 60d);
        float daylight;
        if (hour < dawnStartHour || hour >= nightStartHour)
        {
            daylight = 0f;
            foreach (var building in FindObjectsByType<GridPlaceable>())
            {
                GridPlaceable placeable = building.GetComponent<GridPlaceable>();
                if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
                if (placeable.name != "road(Clone)")
                {
                    SpriteChanger spriteChanger = placeable.GetComponent<SpriteChanger>();
                    if (UnityEngine.Random.value > 0.992f) spriteChanger.ChangeSprite(1);
                }
            }
        }
        else if (hour < sunriseHour)
        {
            daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(dawnStartHour, sunriseHour, hour));
        }
        else if (hour <= sunsetHour)
        {
            daylight = 1f;
            foreach (var building in FindObjectsByType<GridPlaceable>())
            {
                GridPlaceable placeable = building.GetComponent<GridPlaceable>();
                if (!building.isActiveAndEnabled || building.gameObject.scene != gameObject.scene) continue;
                if (building.name != "road(Clone)")
                {
                    SpriteChanger spriteChanger = placeable.GetComponent<SpriteChanger>();
                    if (UnityEngine.Random.value > 0.992f) spriteChanger.ChangeSprite(0);
                }
            }
            
        }
        else
        {
            daylight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunsetHour, nightStartHour, hour));
        }

        var overlayColor = nightOverlayColor;
        overlayColor.a *= (1f - daylight) * nightOverlayOpacity;
        nightOverlay.color = overlayColor;
    }
}