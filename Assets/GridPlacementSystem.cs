using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacementSystem : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;
    [SerializeField, Min(1)] private int gridWidth = 100;
    [SerializeField, Min(1)] private int gridHeight = 100;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-50f, -50f);
    [SerializeField] private GameObject[] placeablePrefabs = new GameObject[0];
    [SerializeField] private Color gridColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    [SerializeField] private Color validPlacementColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.2f, 0.2f, 0.9f);

    private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    private Mesh previewMesh;
    private Mesh gridMesh;
    private Material previewMaterial;
    private Material gridMaterial;
    private MeshRenderer previewRenderer;
    private int selectedPrefabIndex = -1;
    private Vector2Int hoveredCell;
    private Vector2Int hoveredFootprint = Vector2Int.one;
    private bool hasHoveredCell;
    private bool hoveredCellIsValid;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("GridPlacementSystem needs a camera, but no camera was assigned or tagged MainCamera.", this);
            enabled = false;
            return;
        }

        CreateGridVisual();
        CreatePlacementPreview();
    }

    private void Update()
    {
        UpdateSelection();
        UpdateHoveredCell();
        UpdatePlacementPreview();
        HandlePlacementInput();
    }

    private void OnDestroy()
    {
        if (previewMesh != null)
        {
            Destroy(previewMesh);
        }

        if (gridMesh != null)
        {
            Destroy(gridMesh);
        }

        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
        }

        if (gridMaterial != null)
        {
            Destroy(gridMaterial);
        }
    }

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize),
            Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            gridOrigin.x + (cell.x + 0.5f) * cellSize,
            gridOrigin.y + (cell.y + 0.5f) * cellSize,
            0f);
    }

    public bool TryPlace(GameObject prefab, Vector2Int cell, out GameObject placedObject)
    {
        placedObject = null;
        if (prefab == null)
        {
            return false;
        }

        Vector2Int footprint = GetFootprint(prefab);
        if (!CanPlace(cell, footprint))
        {
            return false;
        }

        Vector3 position = new Vector3(
            gridOrigin.x + (cell.x + footprint.x * 0.5f) * cellSize,
            gridOrigin.y + (cell.y + footprint.y * 0.5f) * cellSize,
            prefab.transform.position.z);
        placedObject = Instantiate(prefab, position, prefab.transform.rotation);

        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                occupiedCells.Add(cell + new Vector2Int(x, y));
            }
        }

        return true;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        return occupiedCells.Contains(cell);
    }

    public void SelectPlaceable(int index)
    {
        if (index < 0 || index >= placeablePrefabs.Length || placeablePrefabs[index] == null)
        {
            selectedPrefabIndex = -1;
            return;
        }

        selectedPrefabIndex = index;
    }

    private bool CanPlace(Vector2Int anchor, Vector2Int footprint)
    {
        if (anchor.x < 0 || anchor.y < 0 ||
            anchor.x + footprint.x > gridWidth ||
            anchor.y + footprint.y > gridHeight)
        {
            return false;
        }

        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                if (occupiedCells.Contains(anchor + new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Vector2Int GetFootprint(GameObject prefab)
    {
        GridPlaceable placeable = prefab.GetComponent<GridPlaceable>();
        return placeable != null ? placeable.Footprint : Vector2Int.one;
    }

    private void UpdateSelection()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SelectPlaceable(-1);
            return;
        }

        Key[] numberKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3,
            Key.Digit4, Key.Digit5, Key.Digit6,
            Key.Digit7, Key.Digit8, Key.Digit9
        };

        for (int i = 0; i < numberKeys.Length && i < placeablePrefabs.Length; i++)
        {
            if (keyboard[numberKeys[i]].wasPressedThisFrame)
            {
                SelectPlaceable(i);
                return;
            }
        }
    }

    private void UpdateHoveredCell()
    {
        Mouse mouse = Mouse.current;
        hasHoveredCell = false;
        if (mouse == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane placementPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!placementPlane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 worldPosition = ray.GetPoint(distance);
        hoveredCell = WorldToCell(worldPosition);
        hoveredFootprint = selectedPrefabIndex >= 0
            ? GetFootprint(placeablePrefabs[selectedPrefabIndex])
            : Vector2Int.one;
        hoveredCellIsValid = selectedPrefabIndex >= 0 &&
            CanPlace(hoveredCell, hoveredFootprint);
        hasHoveredCell = true;
    }

    private void HandlePlacementInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame ||
            !hasHoveredCell || !hoveredCellIsValid)
        {
            return;
        }

        TryPlace(placeablePrefabs[selectedPrefabIndex], hoveredCell, out _);
    }

    private void CreateGridVisual()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find the Sprites/Default shader for the grid visual.", this);
            return;
        }

        GameObject gridObject = new GameObject("Grid Visual");
        MeshFilter meshFilter = gridObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();
        gridMesh = new Mesh { name = "Placement Grid" };
        int verticalLineCount = gridWidth + 1;
        int horizontalLineCount = gridHeight + 1;
        Vector3[] vertices = new Vector3[(verticalLineCount + horizontalLineCount) * 2];
        int[] indices = new int[vertices.Length];
        int vertex = 0;

        for (int x = 0; x <= gridWidth; x++)
        {
            float worldX = gridOrigin.x + x * cellSize;
            vertices[vertex] = new Vector3(worldX, gridOrigin.y, -0.05f);
            vertices[vertex + 1] = new Vector3(worldX, gridOrigin.y + gridHeight * cellSize, -0.05f);
            indices[vertex] = vertex;
            indices[vertex + 1] = vertex + 1;
            vertex += 2;
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            float worldY = gridOrigin.y + y * cellSize;
            vertices[vertex] = new Vector3(gridOrigin.x, worldY, -0.05f);
            vertices[vertex + 1] = new Vector3(gridOrigin.x + gridWidth * cellSize, worldY, -0.05f);
            indices[vertex] = vertex;
            indices[vertex + 1] = vertex + 1;
            vertex += 2;
        }

        gridMesh.vertices = vertices;
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
        gridMesh.RecalculateBounds();
        meshFilter.sharedMesh = gridMesh;

        gridMaterial = new Material(shader) { color = gridColor };
        meshRenderer.sharedMaterial = gridMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void CreatePlacementPreview()
    {
        GameObject previewObject = new GameObject("Placement Preview");
        MeshFilter meshFilter = previewObject.AddComponent<MeshFilter>();
        previewRenderer = previewObject.AddComponent<MeshRenderer>();
        previewMesh = new Mesh { name = "Placement Preview" };
        meshFilter.sharedMesh = previewMesh;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("Could not find the Sprites/Default shader for the placement preview.", this);
            return;
        }

        previewMaterial = new Material(shader);
        previewRenderer.sharedMaterial = previewMaterial;
        previewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        previewRenderer.receiveShadows = false;
        previewObject.SetActive(false);
    }

    private void UpdatePlacementPreview()
    {
        if (previewRenderer == null || previewMesh == null)
        {
            return;
        }

        bool showPreview = hasHoveredCell && selectedPrefabIndex >= 0;
        previewRenderer.gameObject.SetActive(showPreview);
        if (!showPreview)
        {
            return;
        }

        float left = gridOrigin.x + hoveredCell.x * cellSize;
        float bottom = gridOrigin.y + hoveredCell.y * cellSize;
        float right = left + hoveredFootprint.x * cellSize;
        float top = bottom + hoveredFootprint.y * cellSize;
        previewMesh.vertices = new[]
        {
            new Vector3(left, bottom, -0.02f),
            new Vector3(right, bottom, -0.02f),
            new Vector3(right, top, -0.02f),
            new Vector3(left, top, -0.02f)
        };
        previewMesh.SetIndices(new[] { 0, 1, 1, 2, 2, 3, 3, 0 }, MeshTopology.Lines, 0);
        previewMesh.RecalculateBounds();
        previewMaterial.color = hoveredCellIsValid
            ? validPlacementColor
            : invalidPlacementColor;
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
    }
}
