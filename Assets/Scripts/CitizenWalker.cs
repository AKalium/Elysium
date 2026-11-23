
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CitizenWalker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float roadRefreshSeconds = 0.5f;

    [Header("Rendering")]
    [SerializeField] private int spriteOrder = 200;
    [SerializeField] private float citizenZ = -1f;

    private GridPlacementSystem grid;
    private CitizenAnimation citizenAnimation;

private void Start()
{
    citizenAnimation = GetComponent<CitizenAnimation>();
}

    private readonly Dictionary<Vector2Int, Vector3> roadCenters =
        new Dictionary<Vector2Int, Vector3>();

    private Vector2Int currentCell;
    private Vector2Int previousCell;
    private Vector2Int targetCell;

    private bool spawned;
    private bool moving;
    private bool warnedNoRoads;
    private float nextRefresh;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        GetComponent<SpriteRenderer>().sortingOrder = spriteOrder;
    }

    private void Update()
    {
        if (DisasterSystem.IsCutsceneOpen)
        {
            citizenAnimation?.SetMovement(Vector2.zero);
            return;
        }

        // Recheck the road network periodically.
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + roadRefreshSeconds;

            if (grid == null)
                grid = FindAnyObjectByType<GridPlacementSystem>();

            if (grid != null)
                RefreshRoads();
        }

        if (grid == null || roadCenters.Count == 0)
            return;

        // Wait until an actual road exists before spawning.
        if (!spawned || !roadCenters.ContainsKey(currentCell))
        {
            SpawnOnRoad();
            return;
        }

        // Wait for additional roads if there is nowhere to walk.
        if (!moving)
            ChooseNextCell();

        if (!moving)
        {
            citizenAnimation?.SetMovement(Vector2.zero);
            return;
        }

        // The destination road might have been demolished.
        if (!roadCenters.TryGetValue(targetCell, out Vector3 destination))
        {
            moving = false;
            transform.position = roadCenters[currentCell];
            return;
        }

        Vector2 direction = destination - transform.position;

        citizenAnimation?.SetMovement(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            walkSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, destination) < 0.01f)
        {
            transform.position = destination;

            previousCell = currentCell;
            currentCell = targetCell;
            moving = false;
        }
    }

    private void RefreshRoads()
    {
        var available = new Dictionary<Vector2Int, Vector3>();

        foreach (var item in grid.GetPlacedObjects())
        {
            if (!item.IsRoad || item.Object == null)
                continue;

            int width = Mathf.Max(1, item.Footprint.x);
            int height = Mathf.Max(1, item.Footprint.y);

            // Find the geometric center of the road's grid footprint.
            Vector3 logicalCenter = grid.CellToWorld(item.Anchor);

            logicalCenter += new Vector3(
                (width - 1) * grid.CellSize * 0.5f,
                (height - 1) * grid.CellSize * 0.5f,
                0f
            );

            // Account for offsets in the visible road sprite.
            SpriteRenderer roadSprite =
                item.Object.GetComponentInChildren<SpriteRenderer>();

            Vector3 visibleCenter = roadSprite != null
                ? roadSprite.bounds.center
                : logicalCenter;

            Vector3 visualOffset = visibleCenter - logicalCenter;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int cell =
                        item.Anchor + new Vector2Int(x, y);

                    Vector3 position =
                        grid.CellToWorld(cell) + visualOffset;

                    position.z = citizenZ;
                    available[cell] = position;
                }
            }
        }

        roadCenters.Clear();

        if (available.Count == 0)
        {
            if (!warnedNoRoads)
            {
                Debug.LogWarning(
                    "CitizenWalker: No roads detected yet. Waiting."
                );
                warnedNoRoads = true;
            }

            return;
        }

        warnedNoRoads = false;

        // Start from the central road, if present.
        Vector2Int root = grid.CentralRoadCell;

        if (!available.ContainsKey(root))
        {
            // Fallback: find the nearest real road.
            int bestDistance = int.MaxValue;

            foreach (var cell in available.Keys)
            {
                int distance =
                    Mathf.Abs(cell.x - root.x) +
                    Mathf.Abs(cell.y - root.y);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    root = cell;
                }
            }
        }

        // Find roads connected to this starting road.
        var queue = new Queue<Vector2Int>();

        queue.Enqueue(root);
        roadCenters[root] = available[root];

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;

                if (!available.ContainsKey(neighbor) ||
                    roadCenters.ContainsKey(neighbor))
                    continue;

                roadCenters[neighbor] = available[neighbor];
                queue.Enqueue(neighbor);
            }
        }
    }

    
    private void SpawnOnRoad()
    {
        if (roadCenters.Count == 0)
            return;

        // Pick a random connected road cell.
        var cells = new List<Vector2Int>(roadCenters.Keys);
        Vector2Int start = cells[Random.Range(0, cells.Count)];

        currentCell = start;
        previousCell = start;
        targetCell = start;

        transform.position = roadCenters[start];

        spawned = true;
        moving = false;
    }


    private void ChooseNextCell()
    {
        var neighbors = new List<Vector2Int>();

        foreach (Vector2Int direction in Directions)
        {
            Vector2Int neighbor = currentCell + direction;

            if (roadCenters.ContainsKey(neighbor))
                neighbors.Add(neighbor);
        }

        if (neighbors.Count == 0)
        {
            moving = false;
            return;
        }

        // Avoid turning around unless at a dead end.
        if (neighbors.Count > 1)
            neighbors.Remove(previousCell);

        targetCell = neighbors[Random.Range(0, neighbors.Count)];
        moving = true;
    }
}
