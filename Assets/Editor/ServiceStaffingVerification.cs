using System;
using UnityEditor;
using UnityEngine;

public static class ServiceStaffingVerification
{
    [MenuItem("Elysium/Verify Founding Bonus and Factory Upkeep")]
    public static void VerifyAgeBasedEconomy()
    {
        Check(GridPlaceable.FoundingBaseHappiness(1) == 100 && GridPlaceable.FoundingBaseHappiness(10) == 100,
            "Founding bonus lasts ten days");
        Check(GridPlaceable.FoundingBaseHappiness(11) == 95 && GridPlaceable.FoundingBaseHappiness(17) == 65 &&
            GridPlaceable.FoundingBaseHappiness(18) == 60 && GridPlaceable.FoundingBaseHappiness(100) == 60,
            "Founding bonus fades to normal base happiness");
        var first = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/fact.prefab"));
        var later = UnityEngine.Object.Instantiate(first);
        var solar = UnityEngine.Object.Instantiate(Resources.Load<BuildingData>("SolarFactory").BuildingPrefab);
        try
        {
            var a = first.GetComponent<GridPlaceable>();
            var b = later.GetComponent<GridPlaceable>();
            a.InitializeConstructionDay(1);
            b.InitializeConstructionDay(5);
            Check(a.UpkeepForDay(10) == 0 && a.UpkeepForDay(11) == 8, "First factory grace boundary");
            Check(b.UpkeepForDay(14) == 0 && b.UpkeepForDay(15) == 8, "Later factory has independent grace period");
            Check(a.UpkeepForDay(11) + b.UpkeepForDay(11) == 8, "Mixed-age factory upkeep");
            var s = solar.GetComponent<GridPlaceable>();
            s.InitializeConstructionDay(5);
            Check(s.UpkeepForDay(5) == 10 && s.UpkeepForDay(15) == 10, "Solar pays from construction");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(later);
            UnityEngine.Object.DestroyImmediate(solar);
        }
        Debug.Log("PASS: founding bonus fade and per-building factory upkeep boundaries.");
    }

    [MenuItem("Elysium/Verify Service Grace and Fire Protection")]
    public static void VerifyGraceAndFire()
    {
        Check(!DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Flood, 7, 1000) &&
            !DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Fire, 7, 1000), "No disasters before day 8");
        Check(!DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Flood, 8, 99) &&
            DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Flood, 8, 100), "Flood starts at 100 on day 8");
        Check(!DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Fire, 8, 149) &&
            DisasterSystem.MeetsNaturalDisasterThreshold(DisasterKind.Fire, 8, 150), "Fire starts at 150 on day 8");
        Check(ApartmentStatsUI.MissingServicePenalty(5, 1) == 2.5f, "Day 1 half penalty");
        Check(ApartmentStatsUI.MissingServicePenalty(7, 30) == 3.5f, "Day 30 half penalty");
        Check(ApartmentStatsUI.MissingServicePenalty(5, 31) == 5 &&
            ApartmentStatsUI.MissingServicePenalty(7, 31) == 7, "Day 31 full penalties");
        var recycling = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/recy_center.prefab");
        Check(recycling.GetComponent<BuildingPollution>().PollutionRemovalPerDay == 15, "Recycling removes 15");
        var f = RoadNetworkTestSupport.Fresh();
        f.Wallet.AddFunds(10000);
        var c = f.Center;
        RoadNetworkTestSupport.LayPath(f.Grid, c, c + new Vector2Int(0, -4));
        Check(f.Grid.TryPlace(f.House, c + Vector2Int.right, out var house), "Place covered home");
        Check(f.Grid.TryPlace(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/mayors office.prefab"),
            c + new Vector2Int(-4, 0), out var office), "Place office");
        Check(f.Grid.TryPlace(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/fire_station.prefab"),
            c + new Vector2Int(1, -4), out _), "Place fire station");
        var disaster = UnityEngine.Object.FindAnyObjectByType<DisasterSystem>();
        RoadNetworkTestSupport.Invoke(disaster, "Awake");
        var home = house.GetComponent<GridPlaceable>();
        Check(disaster.HasFireCoverage(home), "Fully staffed station covers nearby home");
        SetResidents(home, 1);
        Check(!disaster.HasFireCoverage(home), "Understaffed station cannot protect");
        SetResidents(home, 8);
        var distant = UnityEngine.Object.Instantiate(f.House, house.transform.position + Vector3.right * 100,
            Quaternion.identity).GetComponent<GridPlaceable>();
        var state = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(1729);
            int saves = 0;
            for (int trial = 0; trial < 64; trial++)
            {
                int damaged = disaster.ApplySelectedTargets(DisasterKind.Fire, new[] { home, distant }, out int saved);
                Check(distant.IsDamaged && saved <= 1 && damaged + saved == 2, "Uncovered target unchanged; no replacement targets");
                Check(!office.GetComponent<GridPlaceable>().IsDamaged, "Unselected building unaffected");
                saves += saved;
                foreach (var target in new[] { home, distant })
                    if (target.TryGetComponent<DisasterDamage>(out var damage)) UnityEngine.Object.DestroyImmediate(damage);
            }
            Check(saves > 0 && saves < 64, "Coverage sometimes saves selected targets");
            Check(disaster.ApplySelectedTargets(DisasterKind.Flood, new[] { home }, out int floodSaved) == 1 && floodSaved == 0,
                "Fire coverage does not prevent flood damage");
        }
        finally { UnityEngine.Random.state = state; UnityEngine.Object.DestroyImmediate(distant.gameObject); }
        Debug.Log("PASS: grace boundaries, recycling cleanup, staffed fire coverage and no replacement targets.");
    }

    [MenuItem("Elysium/Verify Bridge Upgrades and Sprite Bounds")]
    public static void VerifyVisualsAndBridgeUpgrade()
    {
        var f = RoadNetworkTestSupport.Fresh();
        f.Wallet.AddFunds(10000);
        var c = f.Center;
        RoadNetworkTestSupport.LayPath(f.Grid, c, c + Vector2Int.right);
        GridPlaceable bridge = null;
        foreach (var item in f.Grid.GetPlacedObjects())
            if (item.Anchor == c + Vector2Int.right) bridge = item.Object.GetComponent<GridPlaceable>();
        var settings = new SerializedObject(bridge);
        settings.FindProperty("definition").objectReferenceValue = Resources.Load<BuildingData>("Bridge");
        settings.ApplyModifiedPropertiesWithoutUndo();
        var money = f.Wallet.Balance;
        Check(f.Grid.TryUpgradeBridge(c + Vector2Int.right), "Upgrade succeeds");
        Check(f.Wallet.Balance == money - 100 && bridge.IsStoneBridge, "Upgrade costs exactly $100");
        Check(f.Grid.IsRoadConnected(c + Vector2Int.right), "Upgrade retains connectivity");
        Check(!f.Grid.TryUpgradeBridge(c + Vector2Int.right) && f.Wallet.Balance == money - 100,
            "Stone bridge cannot be charged twice");
        bridge.gameObject.AddComponent<DisasterDamage>().Apply(DisasterKind.Flood);
        Check(!bridge.IsDamaged, "Upgraded bridge resists flood");
        var renderer = bridge.GetComponent<SpriteRenderer>();
        Check(renderer.sortingOrder < 0, "Stone bridge renders behind buildings");
        foreach (var terrainName in new[] { "Water", "Grass", "Dirt", "Sand" })
        {
            var terrain = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/terrain-TX/" + terrainName + "_0.prefab");
            foreach (var ground in terrain.GetComponentsInChildren<SpriteRenderer>(true))
                Check(ground.sortingLayerID == renderer.sortingLayerID &&
                    ground.sortingOrder < renderer.sortingOrder, "Bridge visible above " + terrainName);
        }
        Check(PerlinNoiseMap.TerrainSortingOrder < renderer.sortingOrder, "Generated terrain stays below bridge");
        for (int mask = 0; mask < 16; mask++)
        {
            bridge.GetComponent<SpriteChanger>().ChangeSprite(mask);
            VerifyStoneTile(renderer.sprite, mask);
            Check(renderer.bounds.size.x <= 1.001f && renderer.bounds.size.y <= 1.001f, "Stone stays inside tile");
        }
        foreach (var name in new[] { "school", "mayors office", "apt", "fact", "market", "recy_center" })
        {
            var obj = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab"));
            try
            {
                var building = obj.GetComponent<GridPlaceable>();
                var visual = obj.GetComponent<SpriteRenderer>();
                Check(visual.sprite != null, "Sprite exists");
                var originalSize = visual.bounds.size;
                var originalPosition = obj.transform.position;
                if (name == "school") Check(building.Footprint == new Vector2Int(3, 2), "School is 3 wide by 2 deep");
                if (name == "mayors office")
                {
                    Check(building.TryUpgradeOffice(f.Wallet), "Office upgrades");
                    Check((visual.bounds.size - originalSize).sqrMagnitude < .0001f, "Office retains original visual size");
                    Check(obj.transform.position == originalPosition, "Office retains original placement");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        foreach (var speed in new[] { 2, 4, 8, 1 })
        {
            clock.CycleSpeed();
            Check(clock.Speed == speed, "Speed cycle " + speed);
        }
        Debug.Log("PASS: bridge upgrade, all connection meshes, school footprint, office bounds and 8x speed.");
    }

    [MenuItem("Elysium/Verify Bridge Recovery and Pollution")]
    public static void VerifyBridgeRecovery()
    {
        Check(BuildingPollution.FactoryPollutionForDay(14) == 8, "No pollution increase before day 15");
        Check(BuildingPollution.FactoryPollutionForDay(15) == 9, "First increase on day 15");
        Check(BuildingPollution.FactoryPollutionForDay(17) == 9, "Three-day interval");
        Check(BuildingPollution.FactoryPollutionForDay(18) == 10, "Second increase on day 18");
        Check(BuildingPollution.FactoryPollutionForDay(42) == 18 &&
            BuildingPollution.FactoryPollutionForDay(100) == 18, "Pollution cap remains 18");
        var park = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/park.prefab");
        Check(park.GetComponent<BuildingPollution>().PollutionRemovalPerDay == 6, "Park cleanup is 6");

        var f = RoadNetworkTestSupport.Fresh();
        f.Wallet.AddFunds(10000);
        var c = f.Center;
        RoadNetworkTestSupport.LayPath(f.Grid, c, c + new Vector2Int(5, 0));
        Check(f.Grid.TryPlace(f.House, c + new Vector2Int(5, 1), out var homeObject), "Place remote home");
        var home = homeObject.GetComponent<GridPlaceable>();
        int residents = home.ResidentsProvided;
        GridPlaceable bridge = null;
        foreach (var item in f.Grid.GetPlacedObjects())
            if (item.Anchor == c + new Vector2Int(3, 0)) bridge = item.Object.GetComponent<GridPlaceable>();
        // Use the flat-terrain fixture's occupied road cell as a wooden bridge.
        var bridgeSettings = new SerializedObject(bridge);
        bridgeSettings.FindProperty("definition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Resources/Bridge.asset");
        bridgeSettings.ApplyModifiedPropertiesWithoutUndo();
        bridge.gameObject.AddComponent<DisasterDamage>().Apply(DisasterKind.Flood);
        Check(home != null && home.IsTemporarilyUnavailable && !home.IsOperational, "Flood preserves and suspends disconnected home");
        Check(!f.Grid.IsRoadConnected(c + new Vector2Int(5, 0)), "Damaged bridge does not carry access");
        Check(f.Grid.PreviewDemolition(c + new Vector2Int(3, 0)).BuildingCount == 0, "Repair preview preserves home");
        f.Grid.DestroyBuilding(c + new Vector2Int(3, 0));
        Check(home != null && home.IsTemporarilyUnavailable, "Demolishing damaged bridge preserves home");
        Check(f.Grid.TryPlace(f.Road, c + new Vector2Int(3, 0), out _), "Replace crossing in flat fixture");
        Check(home.IsOperational && !home.IsTemporarilyUnavailable && home.ResidentsProvided == residents,
            "Reconnection restores home and original residents");

        var stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/stone_bridge.prefab");
        var stone = UnityEngine.Object.Instantiate(stonePrefab);
        try
        {
            var changer = stone.GetComponent<SpriteChanger>();
            var renderer = stone.GetComponent<SpriteRenderer>();
            for (int mask = 0; mask < 16; mask++)
            {
                changer.ChangeSprite(mask);
                VerifyStoneTile(renderer.sprite, mask);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(stone); }
        Debug.Log("PASS: bridge recovery, retained residents, 16 stone shapes and pollution boundaries.");
    }

    [MenuItem("Elysium/Verify Solar Revenue and Service Staffing")]
    public static void Run()
    {
        var solar = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Resources/SolarFactory.asset");
        var factory = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/ShopUI/BuildingData/Factory.asset");
        Check(DailyEconomy.GetWorkplaceRevenue(solar, 8, 39) == 80, "Eight solar workers add $80 before happiness bonuses");
        Check(DailyEconomy.GetWorkplaceRevenue(solar, 8, 90) == 100, "Solar happiness bonus");
        Check(DailyEconomy.GetWorkplaceRevenue(factory, 8, 39) == 56, "Factory remains $7 per worker before happiness bonuses");
        for (int happiness = 0; happiness <= 100; happiness++)
        {
            int expected = happiness < 40 ? 0 : happiness < 50 ? 5 : happiness < 60 ? 9 :
                happiness < 70 ? 13 : happiness < 80 ? 17 : happiness < 90 ? 21 : 25;
            Check(DailyEconomy.HappinessRevenueBonusPercent(happiness) == expected, "Revenue tier at " + happiness);
            Check(DailyEconomy.GetCitizenIncome(100, 0, happiness) == 100 + expected, "Resident revenue tier");
            Check(DailyEconomy.GetWorkplaceRevenue(solar, 10, happiness) == 100 + expected, "Workplace revenue tier");
        }

        var f = RoadNetworkTestSupport.Fresh();
        f.Wallet.AddFunds(10000);
        var c = f.Center;
        RoadNetworkTestSupport.LayPath(f.Grid, c, c + new Vector2Int(0, -4));
        Check(f.Grid.TryPlace(f.House, c + Vector2Int.right, out var homeObject), "Place home");
        var office = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/mayors office.prefab");
        Check(f.Grid.TryPlace(office, c + new Vector2Int(-4, 0), out _), "Place office");
        var school = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/school.prefab");
        Check(f.Grid.TryPlace(school, c + new Vector2Int(1, -4), out var serviceObject), "Place school");
        var home = homeObject.GetComponent<GridPlaceable>();
        var service = serviceObject.GetComponent<GridPlaceable>();
        SetResidents(home, 0);
        Check(!ApartmentStatsUI.IsServiceActive(f.Grid, service), "No workers: service inactive");
        SetResidents(home, 3); // ceil(3 * .66) = 2, school needs 3.
        Check(!ApartmentStatsUI.IsServiceActive(f.Grid, service), "Partial staffing: service inactive");
        ApartmentStatsUI.CalculateHappiness(f.Grid, home, out var covered, out _, out _, out _, out _, out _, out _);
        Check(!covered[0], "Understaffed school provides no education coverage");
        SetResidents(home, 4);
        Check(ApartmentStatsUI.IsServiceActive(f.Grid, service), "Full staffing activates service");
        ApartmentStatsUI.CalculateHappiness(f.Grid, home, out covered, out _, out _, out _, out _, out _, out _);
        Check(covered[0], "Fully staffed school provides coverage");
        SetResidents(home, 3);
        Check(!ApartmentStatsUI.IsServiceActive(f.Grid, service), "Population loss deactivates service again");
        Debug.Log("PASS: solar revenue and zero / partial / full service staffing transitions.");
    }

    private static void VerifyStoneTile(Sprite sprite, int mask)
    {
        var texture = sprite.texture;
        Check(texture.width == 32 && texture.height == 32, "Uniform stone tile dimensions");
        Check((texture.GetPixel(31, 16).a > 0) == ((mask & 1) != 0), "Right opening");
        Check((texture.GetPixel(0, 16).a > 0) == ((mask & 2) != 0), "Left opening");
        Check((texture.GetPixel(16, 31).a > 0) == ((mask & 4) != 0), "Top opening");
        Check((texture.GetPixel(16, 0).a > 0) == ((mask & 8) != 0), "Bottom opening");
        if (mask == 3)
        {
            Check(texture.GetPixel(0, 5).a == 0 && texture.GetPixel(3, 5).a > 0,
                "Raised handrail has visible gaps and support posts");
            Check(texture.GetPixel(0, 3).a > 0, "Handrail cap runs outside deck");
            for (int y = 0; y < 32; y++)
                Check(texture.GetPixel(0, y).a == texture.GetPixel(31, y).a, "Horizontal rails and deck join seamlessly");
        }
        if (mask == 12)
            for (int x = 0; x < 32; x++)
                Check(texture.GetPixel(x, 0).a == texture.GetPixel(x, 31).a, "Vertical rails and deck join seamlessly");
    }

    private static void SetResidents(GridPlaceable home, int count)
    {
        var serialized = new SerializedObject(home);
        serialized.FindProperty("residentsProvided").intValue = count;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Check(bool success, string message)
    {
        if (!success) throw new Exception(message);
    }
}
