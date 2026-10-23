using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ElysiumDemoBuild
{
    public const string ProfilePath = "Assets/Settings/Build Profiles/Web - Desktop - Development.asset";
    public const string OutputPath = "Builds/WebGL-Demo";

    [MenuItem("Elysium/Build Web Demo")]
    public static void BuildWebDemo()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ValidateDemo();
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null) throw new Exception("Missing demo Build Profile: " + ProfilePath);
            Directory.CreateDirectory(OutputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = OutputPath,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Demo build failed: {report.summary.totalErrors} errors. See the Console/build log.");
            Debug.Log($"ELYSIUM_DEMO_BUILD_SUCCEEDED: {Path.GetFullPath(OutputPath)} ({report.summary.totalSize} bytes)");
        }
        finally
        {
            if (!Application.isBatchMode && setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    public static void ValidateDemo()
    {
        string[] scenes = { "ElysiumMainMenu", "IntroScene", "SampleScene" };
        var configured = EditorBuildSettings.scenes;
        if (configured.Length < scenes.Length) throw new Exception("Demo scenes are missing from Build Settings.");
        for (int i = 0; i < scenes.Length; i++)
        {
            string path = "Assets/Scenes/" + scenes[i] + ".unity";
            if (!configured[i].enabled || configured[i].path != path)
                throw new Exception("Expected enabled demo scene at index " + i + ": " + path);
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        throw new Exception("Missing script in " + path + ": " + child.name);
        }
        string folder = "Assets/Medieval Buildings and Objects/Prefabs/buildings/";
        foreach (string name in new[] { "wall", "tower (1)", "house A (1)" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".prefab");
            if (prefab == null) throw new Exception("Missing placeable prefab: " + name);
            var renderer = prefab.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null || renderer.sprite.texture == null)
                throw new Exception("Missing building graphics: " + name);
        }
        TimeEconomyVerification.Run();
        Debug.Log("ELYSIUM_DEMO_VALIDATION_PASSED");
    }
}

