using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*IMPORTANT POSITIONS FOR MOVING THE NOISEMAP ACROSS GRID
* BOTTOM LEFT OF PLANE : VECTOR3(5,0,5)
* BOTTOM LEFT OF GRID : VECTOR3(2.05, 0 1.55) 
*/
public class PerlinNoiseMap : MonoBehaviour
{
    public const int TerrainSortingOrder = -10;
    
    private const int WaterTile = 0;
    private const int GrassTile = 1;
    private const int SandTile = 2;
    
    
    private static readonly Vector3 TileGroupOffset = new Vector3(1.95f, 0f, 0.95f);

    //TILE VARIABLES
    Dictionary<int, GameObject> tileset;                            //gives script access to prefabs
    Dictionary<int, GameObject> tileGroups;                         //for organization- groups tiles by category

    public GameObject water;                                        // id - 0
    public GameObject grass;                                        // id - 1
    public GameObject sand;                                         // id - 2

    //MAP VARIABLES
    int mapWidth = 198;
    int mapHeight = 98;

    List<List<int>> noiseGrid = new List<List<int>>();              // stores values related to the perlin
    List<List<GameObject>> tileGrid = new List<List<GameObject>>(); // list of all game objects on screen
    
    //PERLIN VARIABLES
    [Range(4f,80f)]float resolution = 30.0f;                                      // 'resolution' of the terrain - use range [4 , 24] when editing
    private float xOffset;
    private float yOffset;
    private bool seedInitialized;
    public int Seed { get; private set; }

    //ISLAND MASK VARIABLES
    [SerializeField, Range(0.1f, 1f)] private float islandRadius = 0.9f;  // fraction of the shorter half-dimension
    [SerializeField, Range(0f, 1f)]   private float edgeSoftness = 0.3f;  // 0 = hard coastline, 1 = fades from the center
    [SerializeField] private Vector2 islandCenterOffset = Vector2.zero;   // shift the circle in tile units

    //SAND OUTLINE VARIABLES
    [SerializeField, Range(1, 4)] private int sandWidth = 1;        // outline thickness in tiles
    [SerializeField] private bool sandIncludesDiagonals = true;     // false = thinner, cleaner edges

    private void Awake() => InitializeSeed();
    
    private void InitializeSeed()
    {
        if (seedInitialized) return;

        Seed = System.BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0);
        var random = new System.Random(Seed);
        xOffset = (float)(random.NextDouble() * 10000.0);
        yOffset = (float)(random.NextDouble() * 10000.0);
        seedInitialized = true;
    }
    

    public bool IsWaterAt(Vector2 worldPosition)
    {
        Vector2Int tile = WorldToTerrainTile(worldPosition);
        return tile.x >= 0 && tile.y >= 0 && tile.x < mapWidth && tile.y < mapHeight &&
            GetIdFromPerlin(tile.x, tile.y) == WaterTile;
    }

    private Vector2Int WorldToTerrainTile(Vector2 worldPosition)
    {
        Vector2 firstTileCenter = transform.TransformPoint(TileGroupOffset);
        Vector2 offset = worldPosition - firstTileCenter;
        return Vector2Int.FloorToInt(offset + Vector2.one * 0.5f);
    }

/*FUNCTIONS*******************************************************************************************************/
    void Start()
    {
        CreateTileset();
        CreateTileGroups();                                          //Groups generated [child] tiles based on parent tile
        GenerateMap(); 
    }

    //painter's pallette of the tiles within the map
    void CreateTileset()
    {
        tileset = new Dictionary<int, GameObject>();
        tileset.Add(WaterTile, water);
        tileset.Add(GrassTile, grass);
        tileset.Add(SandTile, sand);
    }

    //child tile organizer
    void CreateTileGroups()
    {
        tileGroups = new Dictionary<int, GameObject>();
        
        //loops through tile pallette 
        foreach(KeyValuePair<int, GameObject> pair in tileset)
        {
            GameObject tileGroup = new GameObject(pair.Value.name);
            tileGroup.transform.parent = gameObject.transform;
            tileGroup.transform.localPosition = TileGroupOffset;
            tileGroups.Add(pair.Key, tileGroup);
        }

    }
    //Tile Generation loop
    void GenerateMap()
    {
        for(int x = 0; x < mapWidth; x++)
        {
            noiseGrid.Add(new List<int>());
            tileGrid.Add(new List<GameObject>());

            for(int y = 0; y < mapHeight; y++)
            {
                int tileID = GetIdFromPerlin(x, y);
                noiseGrid[x].Add(tileID);
                CreateTile(tileID, x, y);       
            }
        }


    }

    //returns 1 inside the island, 0 outside, smooth falloff in between
    float CircleMask(int x, int y)
    {
        // center of the map in tile coordinates (plus optional shift)
        float centerX = (mapWidth - 1) * 0.5f + islandCenterOffset.x;
        float centerY = (mapHeight - 1) * 0.5f + islandCenterOffset.y;

        // work in tile units (not normalized) so it's a true circle on a 198x98 map
        float dx = x - centerX;
        float dy = y - centerY;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        // radius is based on the shorter side so the circle fits inside the map
        float outer = Mathf.Min(mapWidth, mapHeight) * 0.5f * islandRadius;
        float inner = outer * (1f - edgeSoftness);

        float t = Mathf.InverseLerp(inner, outer, dist);   // 0 inside inner, 1 at/after outer
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    // water (0) or grass (1) with no sand
    int GetBaseId(int x, int y)
    {
        // Placement may query terrain before this component's Awake runs.
        InitializeSeed();
        float rawPerlin = Mathf.PerlinNoise(
            (x - xOffset) / resolution, 
            (y - yOffset) / resolution
        );                                                                  

        float masked = rawPerlin * CircleMask(x, y);          

        float clamp = Mathf.Clamp(masked, 0.0f, 1.0f);                    //normalize values

        const int terrainTypeCount = 2;
        return Mathf.Min(Mathf.FloorToInt(clamp * terrainTypeCount), terrainTypeCount - 1);

    }

    //grass that touches water becomes sand
    int GetIdFromPerlin(int x, int y)
    {
        int id = GetBaseId(x, y);
        if (id != GrassTile) return id;

        return TouchesWater(x, y) ? SandTile : GrassTile;
    }

    bool TouchesWater(int x, int y)
    {
        for (int dx = -sandWidth; dx <= sandWidth; dx++)
        for (int dy = -sandWidth; dy <= sandWidth; dy++)
        {
            if (dx == 0 && dy == 0) continue;

            // without diagonals, only count tiles in a straight line or a diamond-shaped range
            if (!sandIncludesDiagonals && Mathf.Abs(dx) + Mathf.Abs(dy) > sandWidth) continue;

            int nx = x + dx;
            int ny = y + dy;

            // out-of-bounds counts as land so the map edge doesn't become a sand border
            if (nx < 0 || ny < 0 || nx >= mapWidth || ny >= mapHeight) continue;

            if (GetBaseId(nx, ny) == WaterTile) return true;
        }
        return false;
    }
    
    //Instantiates the tiles to add onto the plane
    void CreateTile(int tileID, int x, int y)
    {
        GameObject tilePrefab = tileset[tileID];
        GameObject tileGp = tileGroups[tileID];
        GameObject tile = Instantiate(tilePrefab, tileGp.transform);
        // Terrain must sit beneath infrastructure (-1) and buildings (0).
        // Apply at generation too so scene prefab overrides cannot hide bridges.
        foreach (var renderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = TerrainSortingOrder;
        }

        tile.name = string.Format("tileX{0}:Y{1}", x , y);
        tile.transform.localPosition = new Vector3(x , y , 0);

        tileGrid[x].Add(tile);
    }





}
