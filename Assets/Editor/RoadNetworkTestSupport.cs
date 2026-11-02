using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RoadNetworkTestSupport
{
    public sealed class Fixture
    {
        public GridPlacementSystem Grid;
        public CurrencySystem Wallet;
        public GameState State;
        public DailyEconomy Economy;
        public GameObject Road;
        public GameObject House;
        public Vector2Int Center => Grid.CentralRoadCell;
    }

    public static Fixture Fresh()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var fixture = new Fixture {
            Grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>(),
            Wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>(),
            State = UnityEngine.Object.FindAnyObjectByType<GameState>(),
            Economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>(),
            Road = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/road.prefab"),
            House = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/apt.prefab")
        };
        Invoke(fixture.Wallet, "Awake"); Invoke(fixture.State, "Awake");
        Invoke(UnityEngine.Object.FindAnyObjectByType<TimeManager>(), "Awake");
        Invoke(fixture.Economy, "Awake"); Invoke(fixture.Grid, "Awake");
        Invoke(fixture.Grid, "Start"); fixture.State.RefreshPopulation();
        return fixture;
    }

    // Infrastructure is created before a test records its initial wallet and
    // building counts, so tests still measure actual purchase/refund behavior.
    public static void Connect(GridPlacementSystem grid, Vector2Int target)
    {
        Invoke(grid, "Start");
        var from = grid.CentralRoadCell;
        LayPath(grid, from, new Vector2Int(target.x, from.y), target);
    }

    public static void LayPath(GridPlacementSystem grid, params Vector2Int[] points)
    {
        var road = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/road.prefab");
        for (int i = 0; i < points.Length; i++)
        {
            var current = i == 0 ? points[i] : points[i - 1];
            var target = points[i];
            while (true)
            {
                if (!grid.IsCellOccupied(current) && !grid.TryPlace(road, current, out _))
                    throw new Exception("Could not seed test road at " + current);
                if (current == target) break;
                if (current.x != target.x) current.x += current.x < target.x ? 1 : -1;
                else current.y += current.y < target.y ? 1 : -1;
            }
        }
    }

    public static void Invoke(MonoBehaviour target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
