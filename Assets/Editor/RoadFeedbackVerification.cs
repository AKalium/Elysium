using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RoadFeedbackVerification
{
    [MenuItem("Elysium/Verify Road Feedback")]
    public static void Run()
    {
        var f = RoadNetworkTestSupport.Fresh();
        var visuals = f.Grid.GetComponent<RoadNetworkVisuals>();
        Check(visuals != null, "Road feedback initializes automatically");
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(6, 0));
        Check(f.Grid.TryPlace(f.Road, f.Center + new Vector2Int(8, 0), out var island), "Place disconnected island");
        var home = f.Center + new Vector2Int(5, 1);
        Check(f.Grid.TryPlace(f.House, home, out var house), "Place dependent building");
        visuals.RefreshIndicators();
        var baseColor = f.Road.GetComponent<SpriteRenderer>().color;
        var centralRenderer = RoadRendererAt(f.Grid, f.Center);
        var connectedRenderer = RoadRendererAt(f.Grid, f.Center + Vector2Int.right);
        ColorNear(centralRenderer.color, baseColor * visuals.CentralColor, "Center uses gold tint");
        ColorNear(connectedRenderer.color, baseColor * visuals.ConnectedColor, "Connected road uses teal tint");
        ColorNear(island.GetComponent<SpriteRenderer>().color, baseColor * visuals.DisconnectedColor, "Island uses warning tint");
        Check(Field<Mesh>(visuals, "statusMesh").vertexCount == 12, "Disconnected X and central diamond are drawn");

        int balance = f.Wallet.Balance, population = f.State.Population, version = f.Grid.MapVersion;
        int count = f.Grid.GetPlacedObjects().Count;
        var bridge = f.Center + new Vector2Int(2, 0);
        var plan = f.Grid.PreviewDemolition(bridge);
        int expectedRefund = f.Wallet.CalculateDemolitionRefund(f.Road.GetComponent<GridPlaceable>().Definition.Price) +
            f.Wallet.CalculateDemolitionRefund(f.House.GetComponent<GridPlaceable>().Definition.Price);
        Check(plan.RoadCount == 1 && plan.BuildingCount == 1 && plan.Refund == expectedRefund, "Preview includes remote building and exact refund");
        Check(f.Wallet.Balance == balance && f.State.Population == population && f.Grid.MapVersion == version &&
            f.Grid.GetPlacedObjects().Count == count && f.Grid.IsRoadConnected(f.Center + new Vector2Int(6, 0)), "Preview does not mutate live city or connectivity");
        var housePlan = f.Grid.PreviewDemolition(home);
        Check(housePlan.RoadCount == 0 && housePlan.BuildingCount == 1, "Direct building preview targets one whole building");
        var centerPlan = f.Grid.PreviewDemolition(f.Center);
        Check(centerPlan.RemovesCentralRoad && centerPlan.BuildingCount == 1 &&
            centerPlan.Refund == f.Wallet.CalculateDemolitionRefund(f.House.GetComponent<GridPlaceable>().Definition.Price), "Center preview warns and does not refund free starting tile");
        var batch = f.Grid.PreviewRoadDemolition(bridge, f.Center + new Vector2Int(4, 0));
        Check(batch.RoadCount == 3 && batch.BuildingCount == 1, "Bulk preview includes all removed road tiles and dependent building");
        Check(f.Grid.PreviewRoadDemolition(new Vector2Int(int.MinValue, 0), new Vector2Int(int.MaxValue, 0)).Targets.Count == 0,
            "Off-map stroke preview is safely empty");

        // Price changes after purchase cannot alter the displayed refund.
        var definition = f.House.GetComponent<GridPlaceable>().Definition;
        int oldPrice = definition.Price;
        var fields = new SerializedObject(definition);
        try
        {
            fields.FindProperty("price").intValue = 999; fields.ApplyModifiedPropertiesWithoutUndo();
            Check(f.Grid.PreviewDemolition(bridge).Refund == expectedRefund, "Preview uses original paid price");
        }
        finally
        { fields.Update(); fields.FindProperty("price").intValue = oldPrice; fields.ApplyModifiedPropertiesWithoutUndo(); }

        visuals.ShowPreview(plan);
        Check(visuals.IsPreviewVisible && visuals.PreviewText.Contains("1 road(s), 1 building(s)"), "Preview displays destruction counts");
        Check(Field<Mesh>(visuals, "fillMesh").vertexCount == 8 && Field<Mesh>(visuals, "outlineMesh").vertexCount == 16,
            "Both road and whole building footprints are highlighted");
        Check(Field<Mesh>(visuals, "fillMesh").bounds.max.y >= house.GetComponent<SpriteRenderer>().bounds.max.y - 0.001f,
            "Preview covers the entire tall building graphic, including its roof");
        ColorNear(connectedRenderer.color, baseColor * visuals.ConnectedColor, "Preview does not overwrite road status tint");
        Canvas.ForceUpdateCanvases(); visuals.PositionTooltip(new Vector2(Screen.width - 1, Screen.height - 1));
        var tooltip = Field<RectTransform>(visuals, "tooltipRect"); var canvas = Field<RectTransform>(visuals, "canvasRect");
        var corners = new Vector3[4]; tooltip.GetWorldCorners(corners);
        var min = canvas.InverseTransformPoint(corners[0]); var max = canvas.InverseTransformPoint(corners[2]);
        Check(min.x >= canvas.rect.xMin - 0.1f && min.y >= canvas.rect.yMin - 0.1f &&
            max.x <= canvas.rect.xMax + 0.1f && max.y <= canvas.rect.yMax + 0.1f, "Tooltip stays on screen");
        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) Check(!graphic.raycastTarget, "Feedback graphics do not steal clicks from existing UI");
        var legendRect = Field<RectTransform>(visuals, "legendRect");
        Check(visuals.ContainsLegendScreenPoint(RectTransformUtility.WorldToScreenPoint(null, legendRect.TransformPoint(legendRect.rect.center))),
            "Opaque legend blocks construction and demolition underneath it");
        visuals.ClearPreview(); Check(!visuals.IsPreviewVisible, "Preview clears without demolition");
        RoadNetworkTestSupport.Invoke(visuals, "OnDisable");
        ColorNear(connectedRenderer.color, baseColor, "Disabling indicators restores original road colors");
        RoadNetworkTestSupport.Invoke(visuals, "OnEnable"); visuals.RefreshIndicators();
        ColorNear(connectedRenderer.color, baseColor * visuals.ConnectedColor, "Re-enable restores single tint without multiplying twice");
        visuals.ShowPreview(f.Grid.PreviewDemolition(f.Center));
        Check(visuals.PreviewText.Contains("city loses access"), "Center warning is visible");
        visuals.ShowPreview(f.Grid.PreviewDemolition(f.Center + new Vector2Int(40, 30)));
        Check(!visuals.IsPreviewVisible, "Empty tiles clear preview");

        f.Grid.DestroyBuilding(bridge);
        Check(f.Wallet.Balance == balance + plan.Refund && f.Grid.GetPlacedObjects().Count == count - plan.Targets.Count && house == null,
            "Actual demolition matches preview objects and refund");
        visuals.RefreshIndicators();
        ColorNear(RoadRendererAt(f.Grid, f.Center + new Vector2Int(6, 0)).color, baseColor * visuals.DisconnectedColor,
            "Severed branch updates its road indicator");
        Check(f.Grid.TryPlace(f.Road, bridge, out _), "Rebuild bridge");
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center + new Vector2Int(6, 0), f.Center + new Vector2Int(8, 0));
        visuals.RefreshIndicators();
        ColorNear(island.GetComponent<SpriteRenderer>().color, baseColor * visuals.ConnectedColor, "Reconnection clears island warning");
        Check(Field<Mesh>(visuals, "statusMesh").vertexCount == 8, "Connected city retains only center diamond");

        f = RoadNetworkTestSupport.Fresh();
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(6, 0));
        RoadNetworkTestSupport.LayPath(f.Grid, f.Center, f.Center + new Vector2Int(0, -2), f.Center + new Vector2Int(6, -2), f.Center + new Vector2Int(6, 0));
        Check(f.Grid.TryPlace(f.House, f.Center + new Vector2Int(5, 1), out _), "Place house with alternate route");
        Check(f.Grid.PreviewDemolition(f.Center + new Vector2Int(2, 0)).BuildingCount == 0, "Preview preserves alternate route");
        var doubleCut = f.Grid.PreviewRoadDemolition(f.Center + new Vector2Int(2, 0), f.Center + new Vector2Int(3, -2));
        Check(doubleCut.RoadCount == 2 && doubleCut.BuildingCount == 1, "Preview evaluates multiple cuts together");
        f.Grid.DestroyBuilding(f.Center);
        visuals = f.Grid.GetComponent<RoadNetworkVisuals>(); visuals.RefreshIndicators();
        Check(Field<UnityEngine.UI.Text>(visuals, "centralLabel").text.Contains("REBUILD"), "Missing center remains marked for rebuilding");
        Debug.Log("ROAD_FEEDBACK_VERIFICATION_PASSED");
    }

    private static SpriteRenderer RoadRendererAt(GridPlacementSystem grid, Vector2Int cell)
    {
        foreach (var item in grid.GetPlacedObjects()) if (item.IsRoad && item.Anchor == cell) return item.Object.GetComponent<SpriteRenderer>();
        throw new Exception("Missing road renderer at " + cell);
    }
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    private static void ColorNear(Color actual, Color expected, string message) => Check(
        Mathf.Abs(actual.r - expected.r) < 0.001f && Mathf.Abs(actual.g - expected.g) < 0.001f && Mathf.Abs(actual.b - expected.b) < 0.001f, message);
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Road feedback verification failed: " + message); }
}
