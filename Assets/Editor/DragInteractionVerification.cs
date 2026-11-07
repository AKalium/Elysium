using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DragInteractionVerification
{
    [MenuItem("Elysium/Verify Drag Interactions")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var grid = UnityEngine.Object.FindAnyObjectByType<GridPlacementSystem>();
        var wallet = UnityEngine.Object.FindAnyObjectByType<CurrencySystem>();
        var state = UnityEngine.Object.FindAnyObjectByType<GameState>();
        var clock = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
        var economy = UnityEngine.Object.FindAnyObjectByType<DailyEconomy>();
        var view = UnityEngine.Object.FindAnyObjectByType<EconomyBudgetUI>();
        Invoke(wallet, "Awake"); Invoke(state, "Awake"); Invoke(clock, "Awake");
        Invoke(economy, "Awake"); Invoke(grid, "Awake");
        RoadNetworkTestSupport.Connect(grid, new Vector2Int(9, 10));
        var prefabs = new SerializedObject(grid).FindProperty("placeablePrefabs");
        int roadIndex = -1;
        int houseIndex = -1;
        GameObject house = null;
        for (int i = 0; i < prefabs.arraySize; i++)
        {
            var prefab = (GameObject)prefabs.GetArrayElementAtIndex(i).objectReferenceValue;
            if (prefab.name == "road") roadIndex = i;
            if (prefab.name == "apt") { house = prefab; houseIndex = i; }
        }
        Check(roadIndex >= 0 && house != null, "Placement definitions");
        grid.SelectPlaceable(roadIndex);
        int start = wallet.Balance, price = grid.GetConstructionCost(roadIndex);
        Input(grid, 10, 10, true, true, true);
        Input(grid, 15, 10, true, false, true);
        for (int x = 10; x <= 15; x++) Check(grid.IsCellOccupied(new Vector2Int(x, 10)), "Fast drag fills skipped tiles");
        Check(wallet.Balance == start - 6 * price, "One charge per road");
        Input(grid, 10, 10, true, false, true);
        Check(wallet.Balance == start - 6 * price, "Retracing does not charge again");
        Input(grid, 13, 13, true, false, true);
        Check(grid.IsCellOccupied(new Vector2Int(11, 10)) && grid.IsCellOccupied(new Vector2Int(11, 11)) &&
            grid.IsCellOccupied(new Vector2Int(12, 11)) && grid.IsCellOccupied(new Vector2Int(12, 12)) &&
            grid.IsCellOccupied(new Vector2Int(13, 12)) && grid.IsCellOccupied(new Vector2Int(13, 13)), "Diagonal drag stays connected");
        Input(grid, 14, 13, false, false, true);
        Input(grid, 18, 13, true, false, true);
        Check(!grid.IsCellOccupied(new Vector2Int(18, 13)), "UI interruption requires a fresh click");
        Input(grid, 18, 13, true, true, true);
        Check(grid.IsCellOccupied(new Vector2Int(18, 13)), "New stroke after UI interruption");
        Input(grid, 18, 13, true, false, false);
        Input(grid, 20, 13, true, false, true);
        Check(!grid.IsCellOccupied(new Vector2Int(20, 13)), "Released stroke cannot continue");
        Input(grid, 0, 0, false, true, true);
        Input(grid, 22, 13, true, false, true);
        Check(!grid.IsCellOccupied(new Vector2Int(22, 13)), "Pressing UI then moving onto map does not build");
        Input(grid, 24, 13, true, true, true);
        grid.SelectPlaceable(-1);
        Input(grid, 26, 13, true, false, true);
        Check(!grid.IsCellOccupied(new Vector2Int(26, 13)), "Escape/selection change cancels stroke");
        grid.SelectPlaceable(roadIndex);
        Input(grid, 15, 10, true, true, true);
        Input(grid, 20, 10, true, false, true);
        grid.SelectPlaceable(houseIndex);
        Input(grid, 14, 11, true, true, true);
        Check(grid.IsCellOccupied(new Vector2Int(14, 11)), "Single click places other buildings");
        Input(grid, 16, 11, true, false, true);
        Check(!grid.IsCellOccupied(new Vector2Int(16, 11)), "Holding does not paint non-road buildings");
        grid.SelectPlaceable(roadIndex);
        int beforeObstacle = wallet.Balance;
        Input(grid, 14, 11, true, true, true);
        Input(grid, 14, 14, true, false, true);
        Check(wallet.Balance == beforeObstacle - 2 * price && grid.IsCellOccupied(new Vector2Int(14, 14)),
            "Occupied building cells are skipped without charges");
        Input(grid, 28, 13, true, true, false);
        Check(grid.IsCellOccupied(new Vector2Int(28, 13)), "Quick press and release still places one road");
        Time.timeScale = 0;
        Input(grid, 30, 13, true, true, true);
        Check(!grid.IsCellOccupied(new Vector2Int(30, 13)), "Paused report prevents building");
        Time.timeScale = 1;
        wallet.TrySpend(wallet.Balance);
        wallet.AddFunds(price * 2);
        Input(grid, 40, 20, true, true, true);
        Input(grid, 45, 20, true, false, true);
        Check(wallet.Balance == 0 && grid.IsCellOccupied(new Vector2Int(41, 20)) &&
            !grid.IsCellOccupied(new Vector2Int(42, 20)), "Drag stops spending at available funds");

        Invoke(view, "OnEnable");
        try
        {
            var so = new SerializedObject(view);
            var panel = ((GameObject)so.FindProperty("budgetPanel").objectReferenceValue).GetComponent<RectTransform>();
            var close = (Button)so.FindProperty("closeBudgetButton").objectReferenceValue;
            var reopen = (Button)so.FindProperty("budgetButton").objectReferenceValue;
            var handle = panel.GetComponentInChildren<DraggableBudgetPanel>();
            Check(close != null && handle != null, "Close button and drag header wired");
            Canvas.ForceUpdateCanvases();
            var events = EventSystem.current;
            if (events == null) events = new GameObject("Verification EventSystem").AddComponent<EventSystem>();
            var header = handle.GetComponent<RectTransform>();
            var point = RectTransformUtility.WorldToScreenPoint(null, header.TransformPoint(header.rect.center));
            var data = new PointerEventData(events) { button = PointerEventData.InputButton.Left, position = point };
            Vector3 before = panel.localPosition;
            handle.OnBeginDrag(data);
            Check(panel.localPosition == before, "Picking up panel does not snap");
            data.position += new Vector2(-140, -60); handle.OnDrag(data);
            Check(panel.localPosition != before, "Dragging header moves window");
            data.position = new Vector2(100000, 100000); handle.OnDrag(data); handle.OnEndDrag(data);
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            var parent = (RectTransform)panel.parent;
            var min = parent.InverseTransformPoint(corners[0]); var max = parent.InverseTransformPoint(corners[2]);
            Check(min.x >= parent.rect.xMin - 0.1f && min.y >= parent.rect.yMin - 0.1f &&
                max.x <= parent.rect.xMax + 0.1f && max.y <= parent.rect.yMax + 0.1f, "Window stays on canvas");
            var panelPoint = RectTransformUtility.WorldToScreenPoint(null, panel.TransformPoint(panel.rect.center));
            Check(view.ContainsScreenPoint(panelPoint), "Moved budget window blocks map input");
            var moved = panel.localPosition;
            close.onClick.Invoke(); Check(!panel.gameObject.activeSelf, "X closes budget");
            Check(!view.ContainsScreenPoint(panelPoint), "Closed window releases map input");
            reopen.onClick.Invoke(); Check(panel.gameObject.activeSelf && panel.localPosition == moved, "Reopen preserves window position");
            Check(Time.timeScale == 1f, "Budget window does not pause game");
        }
        finally { Invoke(view, "OnDisable"); Time.timeScale = 1; }
        Debug.Log("DRAG_INTERACTION_VERIFICATION_PASSED");
    }

    private static void Input(GridPlacementSystem grid, int x, int y, bool onMap, bool pressed, bool held) =>
        grid.GetType().GetMethod("ProcessPlacementInput", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(grid, new object[] { new Vector2Int(x, y), onMap, pressed, held });
    private static void Invoke(MonoBehaviour target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Drag interaction verification failed: " + message); }
}
