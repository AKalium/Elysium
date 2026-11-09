using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DisasterSetup
{
    [MenuItem("Elysium/Install Disasters in Open Scene")]
    public static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<DisasterSystem>() != null) return;
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Disasters/NaturalDisasters.prefab");
        if (existing != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            return;
        }
        string[] names = { "level2_duck", "level2_duck_talking", "level3_duck", "level3_duck_talking" };
        foreach (string name in names)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Disasters/" + name + ".png");
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var root = new GameObject("Natural Disasters");
        var system = root.AddComponent<DisasterSystem>();
        var fields = new SerializedObject(system);
        fields.FindProperty("clock").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        fields.FindProperty("pollution").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PollutionSystem>();
        fields.FindProperty("grid").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        fields.FindProperty("dialoguePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Intro/Dialogue.prefab").GetComponent<IntroDialogue>();
        SetTextures(fields.FindProperty("idleForms"), "Assets/Intro/DawnyIdle.png", "Assets/Disasters/level2_duck.png", "Assets/Disasters/level3_duck.png");
        SetTextures(fields.FindProperty("talkingForms"), "Assets/Intro/DawnyTalking.png", "Assets/Disasters/level2_duck_talking.png", "Assets/Disasters/level3_duck_talking.png");
        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/Disasters/NaturalDisasters.prefab", InteractionMode.AutomatedAction);
        // Scene references belong to the instance, not the reusable prefab.
        fields.Update();
        fields.FindProperty("clock").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        fields.FindProperty("pollution").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PollutionSystem>();
        fields.FindProperty("grid").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    private static void SetTextures(SerializedProperty property, params string[] paths)
    {
        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
    }

    public static void InstallAndVerify()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Install();
        EditorSceneManager.SaveOpenScenes();
        Verify();
    }

    [MenuItem("Elysium/Verify Disaster Rules")]
    public static void Verify()
    {
        Require(DisasterSystem.DamageCount(DisasterKind.Fire, 7) == 2, "Fire rounds up");
        Require(DisasterSystem.DamageCount(DisasterKind.Flood, 25) == 3, "Flood fraction");
        Require(DisasterSystem.DamageCount(DisasterKind.Flood, 1) == 1, "Flood caps at available buildings");
        Require(DisasterSystem.DamageCount(DisasterKind.Flood, 0) == 0, "Empty map");
        var go = new GameObject("Disaster verification");
        try
        {
            var building = go.AddComponent<GridPlaceable>();
            building.InitializeFoundingApartment();
            var sprite = go.AddComponent<SpriteRenderer>();
            var damage = go.AddComponent<DisasterDamage>();
            Require(building.ResidentsProvided == 10, "Initial residents");
            damage.Apply(DisasterKind.Fire);
            Require(!building.IsOperational && building.ResidentsProvided == 0 && building.HousingCapacity == 0, "Damage disables housing");
            Require(!building.TryAddResident(), "Damaged housing cannot accept residents");
            Require(sprite.color.r > sprite.color.b, "Fire tint");
            damage.Apply(DisasterKind.Flood);
            Require(damage.Disaster == DisasterKind.Fire, "No repeated damage");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        Debug.Log("DISASTER VERIFICATION PASSED");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception("Disaster verification failed: " + message); }
}
