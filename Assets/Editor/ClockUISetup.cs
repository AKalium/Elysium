using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClockUISetup
{
    public static void Install()
    {
        const string prefabPath = "Assets/UI/ClockHUD.prefab";
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            throw new System.InvalidOperationException("Clock prefab already exists; edit it directly to preserve UI changes.");
        foreach (var old in Object.FindObjectsByType<TimeEconomyHUD>()) Object.DestroyImmediate(old);
        if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
        var root = new GameObject("Clock HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 10;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = new GameObject("Clock Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -16);
        rect.sizeDelta = new Vector2(300, 52);
        panel.GetComponent<Image>().color = new Color32(46, 63, 48, 240);
        panel.GetComponent<Image>().raycastTarget = false;
        var label = new GameObject("Time Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(panel.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12, 4);
        labelRect.offsetMax = new Vector2(-12, -4);
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(247, 240, 221, 255);
        text.text = "Day 1  |  8:00 AM";
        text.raycastTarget = false;
        var view = root.AddComponent<TimeEconomyHUD>();
        var fields = new SerializedObject(view);
        fields.FindProperty("timeLabel").objectReferenceValue = text;
        fields.FindProperty("panel").objectReferenceValue = rect;
        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
        fields.Update();
        fields.FindProperty("clock").objectReferenceValue = Object.FindAnyObjectByType<TimeManager>();
        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CLOCK_PREFAB_INSTALLED");
        TimeEconomyVerification.Run();
    }
}
