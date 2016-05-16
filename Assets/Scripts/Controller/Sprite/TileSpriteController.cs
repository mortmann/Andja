using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileSpriteController : MonoBehaviour {
    Dictionary<Tile, GameObject> tileGameObjectMap;
    public Sprite waterSprite;
    public Sprite dirtSprite;
    // The pathfinding graph used to navigate our world map.
    World world {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start () {
		tileGameObjectMap = new Dictionary<Tile, GameObject>();

        for (int x = 0; x < world.Width; x++) {
            for (int y = 0; y < world.Height; y++) {
                GameObject tile_go = new GameObject();
                Tile tile_data = world.GetTileAt(x, y);
                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);
                SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
                sr.sprite = waterSprite;
                sr.sortingLayerName = "Tiles";
                tile_go.transform.SetParent(this.transform, true);

                tileGameObjectMap.Add(tile_data, tile_go);

                OnTileChanged(tile_data);
            }
        }
        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.RegisterTileChanged(OnTileChanged);
    }

    void OnTileChanged(Tile tile_data) {

        if (tileGameObjectMap.ContainsKey(tile_data) == false) {
            Debug.LogError("tileGameObjectMap doesn't contain the tile_data -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if (tile_go == null) {
            Debug.LogError("tileGameObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        if (tile_data.Type == TileType.Water) {
            tile_go.GetComponent<SpriteRenderer>().sprite = waterSprite;
        } else if (tile_data.Type == TileType.Dirt) {
            tile_go.GetComponent<SpriteRenderer>().sprite = dirtSprite;
        } else {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }


    }



}
