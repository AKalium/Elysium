using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>Scene-local disaster scheduling and a reusable in-scene Dawny conversation.</summary>
public sealed class DisasterSystem : MonoBehaviour
{
    [SerializeField] private TimeManager clock;
    [SerializeField] private PollutionSystem pollution;
    [SerializeField] private GridPlacementSystem grid;
    [SerializeField] private IntroDialogue dialoguePrefab;
    [SerializeField] private Texture2D[] idleForms;
    [SerializeField] private Texture2D[] talkingForms;
    [SerializeField] private Rect[] portraitUVs = {
        new Rect(0.06f, 0, 0.45f, 0.92f),
        new Rect(0.53f, 0, 0.44f, 0.92f),
        new Rect(0.53f, 0.62f, 0.31f, 0.36f) };
    [SerializeField, Range(0, 1)] private float dailyChance = 0.1f;
    [SerializeField, Min(0)] private int cooldownDays = 3;
    [SerializeField] private bool enableTestKeys = true;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 2f;
    private int lastDisasterDay = -1000;
    private bool pendingDailyRoll;
    private bool ownsPause;
    private bool previousPaused;
    private float previousTimeScale;
    private GameObject overlay;
    private IntroDialogue conversation;
    public static bool IsCutsceneOpen { get; private set; }
    public string LastSummary { get; private set; }
    private float summaryUntil;

    private void Awake()
    {
        if (clock == null) clock = FindAnyObjectByType<TimeManager>();
        if (pollution == null) pollution = FindAnyObjectByType<PollutionSystem>();
        if (grid == null) grid = FindAnyObjectByType<GridPlacementSystem>();
    }
    private void OnEnable() { if (clock != null) clock.Midnight += QueueDailyRoll; }
    private void OnDisable()
    {
        if (clock != null) clock.Midnight -= QueueDailyRoll;
        StopAllCoroutines();
        if (conversation != null) Destroy(conversation.gameObject);
        if (overlay != null) Destroy(overlay);
        ReleasePause();
    }
    private void QueueDailyRoll(int day) => pendingDailyRoll = true;

    private bool AnotherScreenOpen()
    {
        if (IsCutsceneOpen || clock == null || clock.Paused || Time.timeScale == 0) return true;
        var budget = FindAnyObjectByType<EconomyBudgetUI>();
        return (budget != null && budget.IsReportOpen) || FindAnyObjectByType<IntroDialogue>() != null;
    }

    private void Update()
    {
        if (AnotherScreenOpen()) return;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool typing = selected != null && (selected.GetComponent<InputField>() != null || selected.GetComponent<TMP_InputField>() != null);
        var keyboard = Keyboard.current;
        if (enableTestKeys && !typing && keyboard != null)
        {
            if (keyboard.kKey.wasPressedThisFrame) { Trigger(DisasterKind.Fire); return; }
            if (keyboard.lKey.wasPressedThisFrame) { Trigger(DisasterKind.Flood); return; }
        }
        if (!pendingDailyRoll) return;
        pendingDailyRoll = false;
        if (clock.Day - lastDisasterDay <= cooldownDays || pollution == null) return;
        bool flood = pollution.TotalPollution > 25 && Random.value < dailyChance;
        bool fire = pollution.TotalPollution > 50 && Random.value < dailyChance;
        if (flood || fire)
            Trigger(flood && fire ? (Random.value < 0.5f ? DisasterKind.Flood : DisasterKind.Fire)
                : fire ? DisasterKind.Fire : DisasterKind.Flood);
    }

    public static int DamageCount(DisasterKind kind, int eligibleCount) => eligibleCount <= 0 ? 0 :
        Mathf.Min(eligibleCount, kind == DisasterKind.Fire ? Mathf.CeilToInt(eligibleCount / 6f)
            : Mathf.Max(2, Mathf.CeilToInt(eligibleCount / 12f)));

    public List<GridPlaceable> EligibleBuildings()
    {
        var result = new List<GridPlaceable>();
        if (grid == null) return result;
        foreach (var item in grid.GetPlacedObjects())
        {
            if (item.IsRoad || item.Object == null) continue;
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building == null || !building.isActiveAndEnabled || !building.IsOperational) continue;
            string id = building.Definition != null ? building.Definition.BuildingId : building.BuildingTypeId;
            if (building.Category == BuildingCategory.MayorsOffice || id == "city_hall" || id == "mayors_office" || id == "road") continue;
            result.Add(building);
        }
        return result;
    }

    // Manual triggers intentionally bypass thresholds and cooldown, but respect modal screens.
    public bool Trigger(DisasterKind kind)
    {
        if (AnotherScreenOpen() || dialoguePrefab == null) return false;
        var candidates = EligibleBuildings();
        if (candidates.Count == 0)
        {
            LastSummary = "No undamaged buildings are available for this disaster.";
            summaryUntil = Time.unscaledTime + 5;
            return false;
        }
        lastDisasterDay = clock.Day;
        pendingDailyRoll = false;
        StartCoroutine(PlayDisaster(kind, candidates));
        return true;
    }

    private IEnumerator PlayDisaster(DisasterKind kind, List<GridPlaceable> candidates)
    {
        previousPaused = clock.Paused;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        IsCutsceneOpen = true;
        clock.Paused = true;
        Time.timeScale = 0;
        overlay = new GameObject("Disaster Fade", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var shade = new GameObject("Black", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(overlay.transform, false);
        var rect = shade.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = shade.GetComponent<Image>();
        yield return Fade(image, 0, 1);

        int count = DamageCount(kind, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(i, candidates.Count);
            (candidates[i], candidates[pick]) = (candidates[pick], candidates[i]);
            var damage = candidates[i].GetComponent<DisasterDamage>() ?? candidates[i].gameObject.AddComponent<DisasterDamage>();
            damage.Apply(kind);
        }
        LastSummary = $"{(kind == DisasterKind.Fire ? "Fire" : "Flood")}: {count} building{(count == 1 ? "" : "s")} damaged. Sell damaged buildings to clear their space.";
        // Instantiate inactive so configuration happens before the dialogue starts.
        var holder = new GameObject("Disaster Conversation Holder");
        holder.SetActive(false);
        conversation = Instantiate(dialoguePrefab, holder.transform);
        int total = pollution != null ? pollution.TotalPollution : 0;
        int form = total > 50 ? 2 : total > 25 ? 1 : 0;
        string line = $"{(kind == DisasterKind.Fire ? "There was a fire" : "There was a small flood")} in Elysium. {count} building{(count == 1 ? " is" : "s are")} damaged and can no longer be used. You can sell them to clear space for rebuilding.";
        conversation.ConfigureConversation(line, idleForms[form], talkingForms[form], portraitUVs[form]);
        foreach (var uiCanvas in conversation.GetComponentsInChildren<Canvas>(true)) uiCanvas.sortingOrder = 30001;
        conversation.transform.SetParent(null, false);
        Destroy(holder);
        conversation.gameObject.SetActive(true);
        while (conversation != null && conversation.gameObject.activeSelf) yield return null;
        if (conversation != null) Destroy(conversation.gameObject);
        yield return Fade(image, 1, 0);
        Destroy(overlay);
        ReleasePause();
        summaryUntil = Time.unscaledTime + 8;
    }

    private IEnumerator Fade(Image image, float from, float to)
    {
        float elapsed = 0;
        while (elapsed < fadeSeconds)
        {
            image.color = new Color(0, 0, 0, Mathf.Lerp(from, to, elapsed / fadeSeconds));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        image.color = new Color(0, 0, 0, to);
    }

    private void ReleasePause()
    {
        if (!ownsPause) return;
        if (clock != null) clock.Paused = previousPaused;
        Time.timeScale = previousTimeScale;
        ownsPause = false;
        IsCutsceneOpen = false;
    }

    private void OnGUI()
    {
        if (IsCutsceneOpen || Time.unscaledTime >= summaryUntil || string.IsNullOrEmpty(LastSummary)) return;
        GUI.Box(new Rect(20, Screen.height - 80, Mathf.Min(700, Screen.width - 40), 60), LastSummary);
    }
}
