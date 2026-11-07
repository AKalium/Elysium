using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Road status and non-destructive, Shift-hover demolition previews.</summary>
public sealed class RoadNetworkVisuals : MonoBehaviour
{
    [SerializeField] private Color connectedColor = new Color32(134, 224, 199, 255);
    [SerializeField] private Color disconnectedColor = new Color32(255, 158, 79, 255);
    [SerializeField] private Color centralColor = new Color32(255, 55, 55, 255);
    [SerializeField] private Color demolitionColor = new Color32(255, 102, 112, 255);
    private GridPlacementSystem grid;
    private GameObject worldRoot, uiRoot;
    private Material material;
    private Mesh statusMesh, fillMesh, outlineMesh;
    private MeshRenderer fillRenderer, outlineRenderer;
    private Text legend, centralLabel, tooltipText;
    private RectTransform canvasRect, centralRect, tooltipRect, legendRect;
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new();
    private int shownVersion = -1, previewVersion = -1;
    private Vector2Int previewCell;
    private bool initialized, showingPreview, hasFocus = true;
    public bool IsPreviewVisible => showingPreview;
    public string PreviewText => tooltipText == null ? "" : tooltipText.text;
    public Color ConnectedColor => connectedColor;
    public Color DisconnectedColor => disconnectedColor;
    public Color CentralColor => centralColor;

    public bool ContainsLegendScreenPoint(Vector2 position)
    {
        if (!initialized || !isActiveAndEnabled || !legendRect.gameObject.activeInHierarchy) return false;
        var canvas = canvasRect.GetComponent<Canvas>();
        return RectTransformUtility.RectangleContainsScreenPoint(legendRect, position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
    }

    public void Initialize(GridPlacementSystem source)
    {
        if (initialized || source == null) return;
        grid = source;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) { Debug.LogError("Road indicators require Sprites/Default.", this); return; }
        material = new Material(shader) { name = "Road indicator material" };
        worldRoot = new GameObject("Road Network Indicators");
        statusMesh = CreateMesh("Road Status Marks", 100, out _);
        fillMesh = CreateMesh("Demolition Fill", 200, out fillRenderer);
        outlineMesh = CreateMesh("Demolition Outlines", 201, out outlineRenderer);
        CreateUI();
        initialized = true;
        ClearPreview();
        RefreshIndicators();
    }

    private void OnEnable()
    {
        if (!initialized) return;
        worldRoot.SetActive(true); uiRoot.SetActive(true); shownVersion = -1;
    }

    private void OnDisable()
    {
        ClearPreview();
        foreach (var pair in originalColors) if (pair.Key != null) pair.Key.color = pair.Value;
        if (worldRoot != null) worldRoot.SetActive(false);
        if (uiRoot != null) uiRoot.SetActive(false);
        shownVersion = -1;
    }

    private void OnApplicationFocus(bool focused)
    { hasFocus = focused; if (!focused) ClearPreview(); }

    private void OnValidate() => shownVersion = -1;

    private void LateUpdate()
    {
        if (!initialized) return;
        if (shownVersion != grid.MapVersion) RefreshIndicators();
        PositionCenterLabel();
        var keyboard = Keyboard.current;
        bool inspect = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (!hasFocus || !inspect || !grid.HasMapHover || Time.timeScale == 0f || Mouse.current == null)
        { ClearPreview(); return; }
        if (previewCell != grid.HoveredCell || previewVersion != grid.MapVersion)
        {
            previewCell = grid.HoveredCell;
            ShowPreview(grid.PreviewDemolition(previewCell));
            previewVersion = grid.MapVersion;
        }
        PositionTooltip(Mouse.current.position.ReadValue());
    }

    public void RefreshIndicators()
    {
        if (!initialized) return;
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
        var live = new HashSet<SpriteRenderer>();
        foreach (var item in grid.GetPlacedObjects())
        {
            if (!item.IsRoad) continue;
            bool connected = grid.IsRoadConnected(item.Anchor);
            var tint = item.Anchor == grid.CentralRoadCell ? centralColor : connected ? connectedColor : disconnectedColor;
            foreach (var renderer in item.Object.GetComponentsInChildren<SpriteRenderer>())
            {
                live.Add(renderer);
                if (!originalColors.ContainsKey(renderer)) originalColors.Add(renderer, renderer.color);
                renderer.color = originalColors[renderer] * tint;
            }
            if (!connected)
            {
                var center = grid.CellToWorld(item.Anchor);
                float r = grid.CellSize * 0.24f;
                Line(vertices, colors, indices, center + new Vector3(-r, -r, -0.1f), center + new Vector3(r, r, -0.1f), disconnectedColor);
                Line(vertices, colors, indices, center + new Vector3(-r, r, -0.1f), center + new Vector3(r, -r, -0.1f), disconnectedColor);
            }
        }
        var removed = new List<SpriteRenderer>();
        foreach (var pair in originalColors)
            if (!live.Contains(pair.Key)) { if (pair.Key != null) pair.Key.color = pair.Value; removed.Add(pair.Key); }
        foreach (var renderer in removed) originalColors.Remove(renderer);
        var origin = grid.CellToWorld(grid.CentralRoadCell);
        float radius = grid.CellSize * 0.66f;
        var diamond = new[] { origin + new Vector3(0, radius, -0.12f), origin + new Vector3(radius, 0, -0.12f),
            origin + new Vector3(0, -radius, -0.12f), origin + new Vector3(-radius, 0, -0.12f) };
        for (int i = 0; i < 4; i++) Line(vertices, colors, indices, diamond[i], diamond[(i + 1) % 4], centralColor);
        ApplyMesh(statusMesh, vertices, colors, indices, MeshTopology.Lines);
        bool rootExists = grid.IsRoadConnected(grid.CentralRoadCell);
        centralRect.sizeDelta = new Vector2(rootExists ? 160 : 240, 36);
        centralLabel.text = rootExists ? "CENTRAL ROAD (0, 0)" : "CENTER (0, 0): REBUILD ROAD HERE";
        centralLabel.color = centralColor;
        legend.text = $"<b>ROAD ACCESS</b>  <color=#{ColorUtility.ToHtmlStringRGB(connectedColor)}>Connected</color> | " +
            $"<color=#{ColorUtility.ToHtmlStringRGB(disconnectedColor)}>X Disconnected</color> | <color=#{ColorUtility.ToHtmlStringRGB(centralColor)}>Diamond: center</color>\n" +
            "Hold Shift: demolition preview  |  Right-drag: demolish roads" +
            (rootExists ? "" : "\n<color=#FFD356>Central road missing: rebuild at the gold marker.</color>");
        shownVersion = grid.MapVersion;
        ClearPreview();
        PositionCenterLabel();
    }

    public void ShowPreview(DemolitionPlan plan)
    {
        if (!initialized || plan == null || plan.Targets.Count == 0) { ClearPreview(); return; }
        var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
        var edgeVertices = new List<Vector3>(); var edgeColors = new List<Color>(); var edgeIndices = new List<int>();
        foreach (var item in plan.Targets)
        {
            var center = grid.CellToWorld(item.Anchor);
            float left = center.x - grid.CellSize * 0.5f, bottom = center.y - grid.CellSize * 0.5f;
            float right = left + item.Footprint.x * grid.CellSize, top = bottom + item.Footprint.y * grid.CellSize;
            // Tall sprites extend above their occupied ground cells. Cover the
            // whole visible building as well as its footprint so roofs are not
            // left unmarked when the building is about to disappear.
            if (!item.IsRoad && item.Object != null)
                foreach (var renderer in item.Object.GetComponentsInChildren<SpriteRenderer>())
                {
                    var bounds = renderer.bounds;
                    left = Mathf.Min(left, bounds.min.x); right = Mathf.Max(right, bounds.max.x);
                    bottom = Mathf.Min(bottom, bounds.min.y); top = Mathf.Max(top, bounds.max.y);
                }
            var corners = new[] { new Vector3(left, bottom, -0.14f), new Vector3(right, bottom, -0.14f),
                new Vector3(right, top, -0.14f), new Vector3(left, top, -0.14f) };
            int start = vertices.Count;
            var fill = demolitionColor; fill.a = 0.24f;
            foreach (var corner in corners) { vertices.Add(corner); colors.Add(fill); }
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            for (int i = 0; i < 4; i++) Line(edgeVertices, edgeColors, edgeIndices, corners[i], corners[(i + 1) % 4], demolitionColor);
        }
        ApplyMesh(fillMesh, vertices, colors, indices, MeshTopology.Triangles);
        ApplyMesh(outlineMesh, edgeVertices, edgeColors, edgeIndices, MeshTopology.Lines);
        fillRenderer.enabled = outlineRenderer.enabled = true;
        tooltipText.text = "<b>DEMOLITION PREVIEW</b>\n" +
            (plan.RemovesCentralRoad ? "<color=#FF9FA7>Central road: city loses access</color>\n" : "") +
            $"{plan.RoadCount} road(s), {plan.BuildingCount} building(s)\nRefund: ${plan.Refund:N0}\n" +
            "<size=12>Right-click / right-drag to demolish</size>";
        tooltipRect.gameObject.SetActive(true);
        showingPreview = true;
    }

    public void ClearPreview()
    {
        showingPreview = false;
        if (fillRenderer != null) fillRenderer.enabled = false;
        if (outlineRenderer != null) outlineRenderer.enabled = false;
        if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
        previewVersion = -1;
    }

    private Mesh CreateMesh(string name, int order, out MeshRenderer renderer)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(worldRoot.transform, false);
        var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return mesh;
    }

    private static void Line(List<Vector3> vertices, List<Color> colors, List<int> indices, Vector3 a, Vector3 b, Color color)
    {
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); colors.Add(color); colors.Add(color);
        indices.Add(start); indices.Add(start + 1);
    }

    private static void ApplyMesh(Mesh mesh, List<Vector3> vertices, List<Color> colors, List<int> indices, MeshTopology topology)
    {
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetIndices(indices, topology, 0); mesh.RecalculateBounds();
    }

    private void CreateUI()
    {
        uiRoot = new GameObject("Road Access Hints", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = uiRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = uiRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
        canvasRect = uiRoot.GetComponent<RectTransform>();
        legendRect = Panel("Road Legend", new Vector2(520, 74));
        legendRect.anchorMin = legendRect.anchorMax = legendRect.pivot = Vector2.zero;
        legendRect.anchoredPosition = new Vector2(20, 92);
        legend = Label(legendRect, "Legend Text", 14);
        tooltipRect = Panel("Demolition Tooltip", new Vector2(330, 120)); tooltipRect.pivot = Vector2.zero;
        tooltipText = Label(tooltipRect, "Demolition Details", 16);
        centralRect = Panel("Central Road Label", new Vector2(160, 36)); centralRect.pivot = new Vector2(0.5f, 1);
        centralLabel = Label(centralRect, "Center Text", 13); centralLabel.alignment = TextAnchor.MiddleCenter;
    }

    private RectTransform Panel(string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(uiRoot.transform, false);
        var image = go.GetComponent<Image>(); image.color = new Color32(22, 34, 47, 235); image.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; return rect;
    }

    private static Text Label(RectTransform parent, string name, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.supportRichText = true; text.color = new Color32(235, 241, 236, 255);
        text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft;
        var rect = text.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10, 8); rect.offsetMax = new Vector2(-10, -8);
        return text;
    }

    private void PositionCenterLabel()
    {
        if (grid.PlacementCamera == null) return;
        var point = grid.PlacementCamera.WorldToViewportPoint(grid.CellToWorld(grid.CentralRoadCell) - Vector3.up * grid.CellSize * 0.85f);
        bool visible = point.z > 0 && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
        centralRect.gameObject.SetActive(visible);
        if (!visible) return;
        centralRect.anchorMin = centralRect.anchorMax = new Vector2(point.x, point.y); centralRect.anchoredPosition = Vector2.zero;
    }

    public void PositionTooltip(Vector2 screenPosition)
    {
        if (!initialized || !showingPreview) return;
        var canvas = canvasRect.GetComponent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out var local)) return;
        local += new Vector2(18, 22);
        local.x = Mathf.Clamp(local.x, canvasRect.rect.xMin + 12, canvasRect.rect.xMax - tooltipRect.rect.width - 12);
        local.y = Mathf.Clamp(local.y, canvasRect.rect.yMin + 12, canvasRect.rect.yMax - tooltipRect.rect.height - 12);
        tooltipRect.localPosition = new Vector3(local.x, local.y, 0);
    }

    private void OnDestroy()
    {
        foreach (var pair in originalColors) if (pair.Key != null) pair.Key.color = pair.Value;
        Dispose(worldRoot); Dispose(uiRoot); Dispose(statusMesh); Dispose(fillMesh); Dispose(outlineMesh); Dispose(material);
    }

    private static void Dispose(Object obj)
    { if (obj == null) return; if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
}
