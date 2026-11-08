using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*IMPORTANT POSITIONS FOR MOVING THE NOISEMAP ACROSS GRID
* BOTTOM LEFT OF PLANE : VECTOR3(5,0,5)
* BOTTOM LEFT OF GRID : VECTOR3(2.05, 0 1.55) 
*/
public class PerlinNoiseMap : MonoBehaviour
{
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
    int xOffset = 0;                                                 // offsets used to move terrain image
    int yOffset = 0;

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
        tileset.Add(1, water);
        tileset.Add(2, dirt);
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
            tileGroup.transform.localPosition = new Vector3(1.95f, 0f, 0.95f);                     //use to change map position
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
                int tileID = GetIdFromPerlin(x,y);
                noiseGrid[x].Add(tileID);
                CreateTile(tileID, x, y);       
            }
        }


    }

    //returns int ranged [0, numTiles) from perlin noise map
    //!currently using hardcoded numbers! 
    int GetIdFromPerlin(int x, int y)
    {
        float rawPerlin = Mathf.PerlinNoise(
            (x - xOffset) / magnification, 
            (y - yOffset) / magnification
        );                                                                  //generate noise

        float clamp = Mathf.Clamp(rawPerlin, 0.0f, 1.0f);                    //normalize values
        float scale = clamp * tileset.Count;                                //match number of textures

        if(scale == 4) scale--;                                             //make range [0, numTiles)
        
        return Mathf.FloorToInt(scale);                                     //outputs int from range [0, numTiles)

    }

    //Instantiates the tiles to add onto the plane
    void CreateTile(int tileID, int x, int y)
    {
        GameObject tilePrefab = tileset[tileID];
        GameObject tileGp = tileGroups[tileID];
        GameObject tile = Instantiate(tilePrefab, tileGp.transform);

        tile.name = string.Format("tileX{0}:Y{1}", x , y);
        tile.transform.localPosition = new Vector3(x , y , 0);

        tileGrid[x].Add(tile);
    }





}
