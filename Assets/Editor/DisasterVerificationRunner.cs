using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Repeatable Play Mode checks; never save the temporary test city.
[InitializeOnLoad]
public static class DisasterVerificationRunner
{
    static DisasterVerificationRunner()
    {
        EditorApplication.update += TickPlayCheck;
    }

    private static int phase;
    private static int cycle;
    private static double nextAction;
    private static int eligibleBefore;
    private static int dayBefore;
    private static float speedBefore;
    public static void RunPlayBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        BeginPlayCheck();
    }
    [MenuItem("Elysium/Verify Disasters in Play Mode")]
    public static void BeginPlayCheck()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != "Assets/Scenes/SampleScene.unity")
            throw new System.InvalidOperationException("Save SampleScene and exit Play Mode before verification.");
        phase = 0; cycle = 0; nextAction = 0;
        Directory.CreateDirectory("Logs");
        SessionState.SetBool("DisasterPlayCheck", true);
        EditorApplication.isPlaying = true;
    }

    private static void TickPlayCheck()
    {
        if (!SessionState.GetBool("DisasterPlayCheck", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < nextAction) return;
        try
        {
            var system = UnityEngine.Object.FindAnyObjectByType<DisasterSystem>();
            var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
            var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
            if (system == null || clock == null || grid == null) return;
            if (phase == 0)
            {
                UnityEngine.Object.FindAnyObjectByType<CurrencySystem>().AddFunds(100000);
                RoadNetworkTestSupport.Connect(grid, new Vector2Int(39, 41));
                RoadNetworkTestSupport.LayPath(grid, new Vector2Int(39, 41), new Vector2Int(65, 41));
                var house = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/apt.prefab");
                for (int x = 40; x < 64; x += 3)
                    Check(grid.TryPlace(house, new Vector2Int(x, 42), out _), "Seed apartments");
                var hall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/mayors office.prefab");
                Check(grid.TryPlace(hall, new Vector2Int(44, 39), out _), "Seed Mayor's Office");
                var market = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/market.prefab");
                Check(grid.TryPlace(market, new Vector2Int(50, 39), out _), "Seed workplace");
                clock.CycleSpeed();
                phase = 1;
                nextAction = EditorApplication.timeSinceStartup + 1;
            }
            else if (phase == 1)
            {
                var pollution = UnityEngine.Object.FindAnyObjectByType<PollutionSystem>();
                typeof(PollutionSystem).GetField("<TotalPollution>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(pollution, cycle == 0 ? 0 : cycle == 1 ? 30 : 60);
                eligibleBefore = system.EligibleBuildings().Count;
                dayBefore = clock.Day;
                speedBefore = clock.Speed;
                Check(system.Trigger(cycle == 1 ? DisasterKind.Flood : DisasterKind.Fire), "Trigger disaster");
                Check(!system.Trigger(DisasterKind.Fire), "Reject overlapping disaster");
                phase = 2;
                nextAction = EditorApplication.timeSinceStartup + 3;
            }
            else if (phase == 2)
            {
                Check(DisasterSystem.IsCutsceneOpen && clock.Paused && Time.timeScale == 0, "Cutscene pauses simulation");
                Check(clock.Day == dayBefore, "Clock did not advance");
                int expected = DisasterSystem.DamageCount(cycle == 1 ? DisasterKind.Flood : DisasterKind.Fire, eligibleBefore);
                Check(system.EligibleBuildings().Count == eligibleBefore - expected, "Damage count and exclusion");
                foreach (var item in grid.GetPlacedObjects())
                {
                    var building = item.Object.GetComponent<GridPlaceable>();
                    if (item.IsRoad || building.Category == BuildingCategory.MayorsOffice)
                        Check(building.IsOperational, "Roads and Mayor's Office protected");
                }
                var dialogue = UnityEngine.Object.FindAnyObjectByType<IntroDialogue>();
                Check(dialogue != null, "Dialogue displayed");
                var fields = new SerializedObject(dialogue);
                string texture = fields.FindProperty("idleTexture").objectReferenceValue.name;
                Check(texture == (cycle == 0 ? "DawnyIdle" : cycle == 1 ? "level2_duck" : "level3_duck"), "Dawny pollution form");
                dialogue.Continue();
                if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Logs/disaster-form-" + cycle + ".png");
                phase = 3;
                nextAction = EditorApplication.timeSinceStartup + 1;
            }
            else if (phase == 3)
            {
                UnityEngine.Object.FindAnyObjectByType<IntroDialogue>()?.Continue();
                phase = 4;
                nextAction = EditorApplication.timeSinceStartup + 2;
            }
            else if (phase == 4)
            {
                Check(!DisasterSystem.IsCutsceneOpen && !clock.Paused && Time.timeScale == 1 && clock.Speed == speedBefore, "Pause and speed restored");
                // Forecast must not include damaged apartments; their housing and jobs are also disabled.
                UnityEngine.Object.FindAnyObjectByType<DailyEconomy>().Forecast();
                foreach (var damage in UnityEngine.Object.FindObjectsByType<DisasterDamage>())
                    Check(damage.GetComponent<GridPlaceable>().ResidentsProvided == 0, "Damaged residents excluded");
                if (++cycle < 3) { phase = 1; return; }
                VerifyContributionsAndSale(system, grid);
                File.WriteAllText("Logs/DisasterPlayVerification.result", "PASS: all three portraits, fire/flood counts, protected buildings, repeat-damage exclusion, pause/resume at 2x, dialogue Continue, housing/jobs/income/pollution exclusion, and damaged-building sale/refund.");
                SessionState.SetBool("DisasterPlayCheck", false);
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }
        catch (System.Exception exception)
        {
            File.WriteAllText("Logs/DisasterPlayVerification.result", exception.ToString());
            SessionState.SetBool("DisasterPlayCheck", false);
            EditorApplication.isPlaying = false;
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    private static void Check(bool condition, string label)
    { if (!condition) throw new System.Exception("Disaster play verification: " + label); }

    private static void VerifyContributionsAndSale(DisasterSystem system, GridPlacementSystem grid)
    {
        foreach (var building in system.EligibleBuildings())
            (building.GetComponent<DisasterDamage>() ?? building.gameObject.AddComponent<DisasterDamage>()).Apply(DisasterKind.Fire);
        GameState.Instance.RefreshPopulation();
        Check(GameState.Instance.Population == 0 && GameState.Instance.MaxPopulation == 0, "Damaged housing removed from global totals");
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        Check(economy.Forecast().Income == 0, "Damaged homes and employers earn nothing");
        int expectedPollution = 0;
        foreach (var source in UnityEngine.Object.FindObjectsByType<BuildingPollution>())
        {
            var building = source.GetComponent<GridPlaceable>();
            if (building == null || building.IsOperational) expectedPollution += source.GetPollutionForDay(1) - source.PollutionRemovalPerDay;
        }
        var pollution = UnityEngine.Object.FindAnyObjectByType<PollutionSystem>();
        typeof(PollutionSystem).GetMethod("AddDailyPollution", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(pollution, new object[] { 2 });
        Check(pollution.LastDailyPollution == expectedPollution, "Damaged sources emit nothing");
        foreach (var item in grid.GetPlacedObjects())
        {
            var building = item.Object.GetComponent<GridPlaceable>();
            if (building.IsOperational || !building.IsApartment) continue;
            Check(ApartmentStatsUI.GetWorkplaces(grid, building).Count == 0, "Damaged workplaces supply no jobs");
            var wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>();
            int before = wallet.Balance;
            int refund = wallet.CalculateDemolitionRefund(building.Definition.Price);
            grid.DestroyBuilding(item.Anchor);
            Check(!grid.IsCellOccupied(item.Anchor) && wallet.Balance == before + refund, "Damaged building sells and frees its cells");
            break;
        }
    }
}
