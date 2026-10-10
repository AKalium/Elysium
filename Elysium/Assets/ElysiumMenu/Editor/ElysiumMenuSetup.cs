using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ElysiumMenuSetup
{
    [MenuItem("Elysium/Create Main Menu")]
    public static void CreateMenu()
    {
        const string path = "Assets/Scenes/ElysiumMainMenu.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            EditorUtility.DisplayDialog("Menu already exists", "Open " + path + " to use your existing menu. It has not been overwritten.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Main Menu Controller").AddComponent<ElysiumMainMenu>();
        EditorSceneManager.SaveScene(scene, path);
        var existing = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        existing.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = existing.ToArray();
        Selection.activeGameObject = GameObject.Find("Main Menu Controller");
        EditorUtility.DisplayDialog("Menu created", "Press Play to test. Check that your active Build Profile includes ElysiumMainMenu first and SampleScene second.", "OK");
    }
}
