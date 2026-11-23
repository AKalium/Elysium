
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupCitizenAppearances
{
    private const string SpriteFolder = "Assets/Sprites/Citizens";

    [MenuItem("Tools/Elysium/Generate Citizen Appearances")]
    public static void Generate()
    {
        CitizenSpawner spawner =
            Object.FindFirstObjectByType<CitizenSpawner>();

        if (spawner == null)
        {
            Debug.LogError("No CitizenSpawner found in the scene.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { SpriteFolder }
        );

        List<(string name, Dictionary<int, Sprite> frames)> characters = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Dictionary<int, Sprite> frames = new();

            foreach (Object asset in assets)
            {
                if (asset is not Sprite sprite)
                    continue;

                string prefix = name + "_";

                if (!sprite.name.StartsWith(prefix))
                    continue;

                string number = sprite.name.Substring(prefix.Length);

                if (int.TryParse(number, out int index))
                    frames[index] = sprite;
            }

            bool complete = true;

            for (int i = 4; i <= 11; i++)
            {
                if (!frames.ContainsKey(i))
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
                characters.Add((name, frames));
            else
                Debug.LogWarning($"Skipping {name}: missing walking frames.");
        }

        characters.Sort((a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        Undo.RecordObject(spawner, "Generate Citizen Appearances");

        SerializedObject serialized = new SerializedObject(spawner);
        SerializedProperty appearances =
            serialized.FindProperty("appearances");

        appearances.arraySize = characters.Count;

        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            SerializedProperty entry =
                appearances.GetArrayElementAtIndex(i);

            entry.FindPropertyRelative("characterName").stringValue =
                character.name;

            SetFrames(entry, "walkUp", character.frames, 4, 5);
            SetFrames(entry, "walkLeft", character.frames, 10, 11);
            SetFrames(entry, "walkDown", character.frames, 8, 9);
            SetFrames(entry, "walkRight", character.frames, 6, 7);
        }

        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

        Debug.Log(
            $"SUCCESS! Generated {characters.Count} citizen appearances."
        );
    }

    private static void SetFrames(
        SerializedProperty entry,
        string field,
        Dictionary<int, Sprite> frames,
        int first,
        int second)
    {
        SerializedProperty array = entry.FindPropertyRelative(field);

        array.arraySize = 2;
        array.GetArrayElementAtIndex(0).objectReferenceValue = frames[first];
        array.GetArrayElementAtIndex(1).objectReferenceValue = frames[second];
    }
}
