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
    private const int WaterTile = 1;
    private const int DirtTile = 2;
    private static readonly Vector3 TileGroupOffset = new Vector3(1.95f, 0f, 0.95f);
    //TILE VARIABLES
    Dictionary<int, GameObject> tileset;                            //gives script access to prefabs
    Dictionary<int, GameObject> tileGroups;                         //for organization- groups tiles by category
    public GameObject grass;                                        // id - 0
    public GameObject water;                                        // id - 1
    public GameObject dirt;                                         // id - 2
    public GameObject sand;                                         // id - 3

    //MAP VARIABLES
    int mapWidth = 198;
    int mapHeight = 98;

    List<List<int>> noiseGrid = new List<List<int>>();              // stores values related to the perlin
    List<List<GameObject>> tileGrid = new List<List<GameObject>>(); // list of all game objects on screen
    
    //PERLIN VARIABLES
    float magnification = 20.0f;                                      // 'resolution' of the terrain - use range [4 , 24] when editing
    private float xOffset;
    private float yOffset;
    private bool seedInitialized;
    public int Seed { get; private set; }

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
        tileset.Add(0, grass);
        tileset.Add(WaterTile, water);
        tileset.Add(DirtTile, dirt);
        tileset.Add(3, sand);
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

    //returns int ranged [0, numTiles) from perlin noise map
    //!currently using hardcoded numbers! 
    int GetIdFromPerlin(int x, int y)
    {
        // Placement may query terrain before this component's Awake runs.
        InitializeSeed();
        float rawPerlin = Mathf.PerlinNoise(
            (x - xOffset) / magnification, 
            (y - yOffset) / magnification
        );                                                                  //generate noise

        float clamp = Mathf.Clamp(rawPerlin, 0.0f, 1.0f);                    //normalize values
        const int terrainTypeCount = 4;
        return Mathf.Min(Mathf.FloorToInt(clamp * terrainTypeCount), terrainTypeCount - 1);

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
