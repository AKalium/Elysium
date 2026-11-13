using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(GridPlacementSystem), typeof(ApartmentStatsUI))]
public sealed class CityPauseMenu : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private TimeManager clock;
    [SerializeField] private TutorialUI tutorial;
    private GridPlacementSystem grid;
    private ApartmentStatsUI apartment;
    private EconomyBudgetUI budget;
    private DisasterSystem disasters;
    private PollutionSystem pollution;
    private GameObject overlay;
    private CitySaveMenu menu;
    private bool ownsPause;
    private bool previousPaused;
    private float previousScale;
    private bool savePage;
    private static CityPauseMenu activeMenu;

    internal static bool BlocksGameplayInput => activeMenu != null && activeMenu.ownsPause;

    private void Start()
    {
        grid = GetComponent<GridPlacementSystem>();
        apartment = GetComponent<ApartmentStatsUI>();
        budget = FindAnyObjectByType<EconomyBudgetUI>();
        disasters = FindAnyObjectByType<DisasterSystem>();
        pollution = FindAnyObjectByType<PollutionSystem>();
        activeMenu = this;

        var save = CitySaveSlots.PendingLoad;
        CitySaveSlots.PendingLoad = null;
        if (save == null) return;

        try
        {
            grid.RestoreMap(save);
            clock.RestoreClock(save);
            pollution?.RestorePollution(save);
            disasters?.RestoreSchedule(save);
            grid.PlacementCamera.transform.position = save.cameraPosition;
            grid.PlacementCamera.orthographicSize = save.cameraZoom;
            tutorial.CloseTutorial();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not restore the saved city: {exception.Message}");
            CitySaveSlots.LoadError = "This city could not be loaded.";
            SceneManager.LoadScene(CitySaveSlots.MainMenuScene);
        }
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
        if (ownsPause)
        {
            if (savePage) menu.Back();
            else Resume();
            return;
        }

        if (grid.HasBuildingSelection || apartment.HasSelection || DisasterSystem.IsCutsceneOpen ||
            (budget != null && budget.IsReportOpen) || tutorial.IsOpen) return;
        Open();
    }

    private void Open()
    {
        if (overlay == null) CreateOverlay();
        previousPaused = clock.Paused;
        previousScale = Time.timeScale;
        ownsPause = true;
        clock.Paused = true;
        Time.timeScale = 0;
        overlay.SetActive(true);
        ShowPause();
    }

    private void ShowPause()
    {
        savePage = false;
        menu.ShowPause(Resume, ReturnToMainMenu, () =>
        {
            savePage = true;
            menu.ShowSlots();
        });
    }

    private void Resume()
    {
        if (!ownsPause) return;
        ownsPause = false;
        overlay.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        clock.Paused = previousPaused;
        Time.timeScale = previousScale;
    }

    private void OnDestroy()
    {
        Resume();
        if (activeMenu == this) activeMenu = null;
        if (overlay != null) Destroy(overlay);
    }

    private void ReturnToMainMenu()
    {
        Resume();
        SceneManager.LoadScene(CitySaveSlots.MainMenuScene);
    }

    private CitySave Capture()
    {
        var camera = grid.PlacementCamera;
        var save = new CitySave
        {
            version = CitySave.CurrentVersion,
            savedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            day = clock.Day,
            minuteOfDay = clock.MinuteOfDay,
            clockSpeed = clock.Speed,
            keyboardPaused = clock.KeyboardPaused,
            cameraPosition = camera.transform.position,
            cameraZoom = camera.orthographicSize,
            totalPollution = pollution != null ? pollution.TotalPollution : 0,
            lastDailyPollution = pollution != null ? pollution.LastDailyPollution : 0,
            lastDisasterDay = -1000
        };
        grid.CaptureMap(save);
        disasters?.CaptureSchedule(save);
        return save;
    }

    private void CreateOverlay()
    {
        overlay = new GameObject("Pause Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29000;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var shade = new GameObject("Pause Shade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(overlay.transform, false);
        shade.color = new Color(0, 0, 0, 0.55f);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.sizeDelta = Vector2.zero;

        var panel = new GameObject("Parchment", typeof(RectTransform), typeof(Image), typeof(Shadow)).GetComponent<Image>();
        panel.transform.SetParent(overlay.transform, false);
        panel.rectTransform.sizeDelta = new Vector2(940, 940);
        panel.sprite = menuButton.image.sprite;
        var shadow = panel.GetComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(0, -20);
        shadow.useGraphicAlpha = false;

        menu = new CitySaveMenu(panel.transform, menuButton, ShowPause, Capture);
    }
}
