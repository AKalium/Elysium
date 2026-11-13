using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

[Serializable]
internal sealed class CitySave
{
    internal const int CurrentVersion = 1;
    public int version;
    public string savedUtc;
    public int day;
    public double minuteOfDay;
    public float clockSpeed;
    public bool keyboardPaused;
    public double balance;
    public int freeRoadsUsed;
    public int foundingApartmentsUsed;
    public List<Placement> placements;
    public Vector3 cameraPosition;
    public float cameraZoom;
    public int totalPollution;
    public int lastDailyPollution;
    public int lastDisasterDay;
    public bool pendingDisasterRoll;
    public bool hasPreviousDayStats;
    public float previousHappiness;
    public float previousEmployment;
    public float previousOccupancy;
    public int previousPollution;

    [Serializable]
    internal sealed class Placement
    {
        public string buildingId;
        public Vector2Int anchor;
        public Vector2Int footprint;
        public int paidCost;
        public int residents;
        public bool foundingApartment;
        public bool pausePurchase;
        public bool damaged;
        public DisasterKind disaster;
    }

    public bool IsValid =>
        version == CurrentVersion && placements != null &&
        DateTime.TryParseExact(savedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) &&
        day > 0 && minuteOfDay >= 0 && minuteOfDay < 1440 &&
        float.IsFinite(clockSpeed) && clockSpeed >= 0 &&
        balance >= int.MinValue && balance <= int.MaxValue &&
        freeRoadsUsed >= 0 && freeRoadsUsed <= 10 &&
        foundingApartmentsUsed >= 0 && foundingApartmentsUsed <= 3 &&
        totalPollution >= 0 && lastDisasterDay >= -1000 && lastDisasterDay <= day &&
        float.IsFinite(cameraPosition.x) && float.IsFinite(cameraPosition.y) && float.IsFinite(cameraPosition.z) &&
        float.IsFinite(cameraZoom) && cameraZoom > 0;

    public string LocalSaveTime => DateTime.ParseExact(savedUtc, "O", CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind).ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}

internal static class CitySaveSlots
{
    public const int Count = 3;
    public const string GameplayScene = "SampleScene";
    public const string MainMenuScene = "ElysiumMainMenu";

    public static CitySave PendingLoad { get; set; }
    public static string LoadError { get; set; }

    public enum SlotState { Empty, Filled, Unreadable }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        PendingLoad = null;
        LoadError = null;
    }

    public static SlotState Read(int slot, out CitySave save)
    {
        save = null;
        try
        {
            string json = ReadJson(SlotPath(slot));
            if (json == null) return SlotState.Empty;
            save = JsonUtility.FromJson<CitySave>(json);
            if (save != null && save.IsValid) return SlotState.Filled;
            save = null;
            return SlotState.Unreadable;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not read save slot {slot}: {exception.Message}");
            return SlotState.Unreadable;
        }
    }

    public static void Write(int slot, CitySave save)
    {
        string path = SlotPath(slot);
        string json = JsonUtility.ToJson(save);
#if UNITY_WEBGL && !UNITY_EDITOR
        // Compress the city to leave room for all three slots in browser storage.
        using var output = new MemoryStream();
        using (var compressed = new GZipStream(output, CompressionMode.Compress, true))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            compressed.Write(bytes, 0, bytes.Length);
        }
        if (ElysiumWriteSave(path, Convert.ToBase64String(output.ToArray())) == 0)
            throw new IOException("The browser could not store this save.");
#else
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
#endif
    }

    private static string ReadJson(string path)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string value = ElysiumReadSave(path);
        if (value == null) throw new IOException("Browser storage is unavailable.");
        if (value.Length == 0) return null;
        using var input = new MemoryStream(Convert.FromBase64String(value));
        using var compressed = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(compressed);
        return reader.ReadToEnd();
#else
        try { return File.ReadAllText(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
#endif
    }

    private static string SlotPath(int slot)
    {
        if (slot < 1 || slot > Count) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(Application.persistentDataPath,
            Application.isEditor ? "EditorSaves" : "Saves", $"elysium-slot-{slot}.json");
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern string ElysiumReadSave(string key);
    [DllImport("__Internal")] private static extern int ElysiumWriteSave(string key, string value);
#endif
}
