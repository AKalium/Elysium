using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EconomyBudgetSetup
{
    public const string PrefabPath = "Assets/UI/EconomyBudget.prefab";
    private static readonly Color Ink = new Color32(22, 34, 47, 255);
    private static readonly Color Paper = new Color32(235, 241, 236, 255);
    private static Font font;

    [MenuItem("Elysium/Set Up Economy Budget")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (var old in Object.FindObjectsByType<EconomyBudgetUI>())
            Object.DestroyImmediate(old.gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("Economy Budget", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var view = root.AddComponent<EconomyBudgetUI>();
        var toggle = Button(root.transform, "Budget Button", "CITY BUDGET", new Vector2(1, 1), new Vector2(-20, -75), new Vector2(180, 40));
        var panel = Box(root.transform, "Budget Panel", new Vector2(1, 1), new Vector2(-20, -125), new Vector2(340, 340), Ink);
        var budget = Label(panel, "Budget Text", 17, Paper);
        Stretch(budget.rectTransform, 18, 18, 18, 18);
        var overlay = Box(root.transform, "Daily Report Overlay", Vector2.zero, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.65f));
        Stretch(overlay, 0, 0, 0, 0);
        var report = Box(overlay, "Report Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 560), Ink);
        var heading = Label(report, "Report Heading", 22, new Color32(190, 225, 172, 255));
        Pin(heading.rectTransform, new Vector2(0, 1), new Vector2(28, -24), new Vector2(584, 38));
        var scrollObject = new GameObject("Report Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollObject.transform.SetParent(report, false);
        var viewport = scrollObject.GetComponent<RectTransform>();
        Stretch(viewport, 28, 28, 80, 76);
        scrollObject.GetComponent<Image>().color = new Color(1, 1, 1, 0.015f);
        var body = Label(viewport, "Report Text", 19, Paper);
        body.rectTransform.anchorMin = new Vector2(0, 1);
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.pivot = new Vector2(0.5f, 1);
        body.rectTransform.sizeDelta = Vector2.zero;
        var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = body.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 25;
        var close = Button(report, "Continue Button", "CONTINUE", new Vector2(1, 0), new Vector2(-28, 20), new Vector2(170, 40));
        var hint = Label(report, "Scroll Hint", 14, Paper);
        hint.text = "Scroll for building details";
        hint.alignment = TextAnchor.MiddleLeft;
        Pin(hint.rectTransform, Vector2.zero, new Vector2(28, 20), new Vector2(330, 40));
        overlay.gameObject.SetActive(false);
        var so = new SerializedObject(view);
        Set(so, "budgetPanel", panel.gameObject); Set(so, "budgetText", budget);
        Set(so, "reportPanel", overlay.gameObject); Set(so, "reportHeading", heading);
        Set(so, "reportText", body); Set(so, "budgetButton", toggle);
        Set(so, "closeReportButton", close); Set(so, "reportScroll", scroll);
        so.ApplyModifiedPropertiesWithoutUndo();
        if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ECONOMY_BUDGET_SETUP_PASSED");
    }

    private static void Set(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
    private static void Pin(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    { rt.anchorMin = rt.anchorMax = rt.pivot = anchor; rt.anchoredPosition = position; rt.sizeDelta = size; }
    private static void Stretch(RectTransform rt, float left, float right, float bottom, float top)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top); }
    private static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>(); Pin(rt, anchor, pos, size); return rt;
    }
    private static Text Label(Transform parent, string name, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = color;
        text.supportRichText = true; text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
    }
    private static Button Button(Transform parent, string name, string caption, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = Box(parent, name, anchor, pos, size, new Color32(177, 216, 161, 255));
        var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = rt.GetComponent<Image>();
        var text = Label(rt, "Label", 17, Ink); text.text = caption; text.alignment = TextAnchor.MiddleCenter;
        Stretch(text.rectTransform, 4, 4, 4, 4); return button;
    }
}
