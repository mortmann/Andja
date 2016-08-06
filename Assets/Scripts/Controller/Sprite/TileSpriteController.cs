using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileSpriteController : MonoBehaviour {
    Dictionary<Tile, GameObject> tileGameObjectMap;
	CameraController cc;
    public Sprite waterSprite;
    public Sprite dirtSprite;
	public Sprite mountainSprite;
	public Sprite darkLayerSprite;
	GameObject darkLayer;
	public GameObject waterLayer;
	public GameObject water;

	public Texture waterTexture;
	public Material waterMaterial;

	public Material darkMaterial;
	Material clearMaterial;
	public Material highlightMaterial;
    // The pathfinding graph used to navigate our world map.
    World world {
		get { return World.current; }
    }

    // Use this for initialization
    void Start () {
		cc = GameObject.FindObjectOfType<CameraController> ();
		tileGameObjectMap = new Dictionary<Tile, GameObject>();
		water = Instantiate (waterLayer);
		water.transform.position = new Vector3((world.Width/2)-0.5f,(world.Height/2)-0.5f , 0.1f);
		water.transform.localScale = new Vector3 (6+world.Width/10,0.1f,6+world.Height/10);
		water.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (world.Width, world.Height*3);

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.RegisterTileChanged(OnTileChanged);
		BuildController.Instance.RegisterBuildStateChange (OnBuildStateChance);

    }
	void OnBuildStateChance(BuildStateModes bsm){
		if(bsm != BuildStateModes.None){
			if (darkLayer != null) {
				return;
			}
			darkLayer = new GameObject ();
			darkLayer.transform.position = new Vector3((world.Width/2)-0.5f,(world.Height/2)-0.5f , 0);
			SpriteRenderer sr = darkLayer.AddComponent<SpriteRenderer> ();

			sr.sprite = darkLayerSprite;
			sr.sortingLayerName = "DarkLayer";
			darkLayer.transform.localScale = new Vector3 (world.Width,world.Height);
			darkLayer.name="DarkLayer";
			darkLayer.transform.SetParent (this.transform);
		} else {
			GameObject.Destroy (darkLayer);
			darkLayer = null;

		}
	}

    void OnTileChanged(Tile tile_data) {

        if (tileGameObjectMap.ContainsKey(tile_data) == false) {
//            Debug.LogError("tileGameObjectMap doesn't contain the tile_data -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if (tile_go == null) {
            Debug.LogError("tileGame ObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }
		SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer> ();
		if(clearMaterial==null){
			clearMaterial = sr.material;
		}
		
		if(tile_data.TileState == TileMark.Highlight){
			sr.material = highlightMaterial;
		} else 
			if(tile_data.TileState == TileMark.None ){
			sr.material = clearMaterial;
		} else
		if(tile_data.TileState == TileMark.Dark){
			sr.material = darkMaterial;
		}

		if (tile_data.Type != TileType.Water) {
			tile_go.GetComponent<SpriteRenderer> ().sortingLayerName = "Tile";
		}
        if (tile_data.Type == TileType.Water) {
            tile_go.GetComponent<SpriteRenderer>().sprite = waterSprite;
        } else if (tile_data.Type == TileType.Dirt) {
            tile_go.GetComponent<SpriteRenderer>().sprite = dirtSprite;
		} else if (tile_data.Type == TileType.Mountain) {
			tile_go.GetComponent<SpriteRenderer>().sprite = mountainSprite;
		} else {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }


    }
	public void Update(){
		int lowerX = (int)cc.lower.x - 1*(int)cc.zoomLevel/10;
		int upperX = (int)cc.upper.x + 3*(int)cc.zoomLevel/10;
		int lowerY = (int)cc.lower.y - 1*(int)cc.zoomLevel/10;
		int upperY = (int)cc.upper.y + 3*(int)cc.zoomLevel/10;
		List<Tile> ts = new List<Tile> (tileGameObjectMap.Keys);
		for (int i = 0; i < ts.Count; i++) {
			Tile tile_data = ts [i];
			if(tileGameObjectMap.ContainsKey (tile_data)){
				if(tile_data.X>lowerX&&tile_data.X<upperX&&tile_data.Y>lowerY&&tile_data.Y<upperY){
					continue;
				} 
				GameObject.Destroy (tileGameObjectMap[tile_data]);
				tileGameObjectMap.Remove (tile_data);
			}
		}

		for (int x = lowerX; x < upperX; x++) {
			for (int y=lowerY; y < upperY; y++) {
				Tile tile_data = world.GetTileAt(x, y);
				if(World.current.GetTileAt (x,y)==null||World.current.GetTileAt (x,y).Type == TileType.Water || tileGameObjectMap.ContainsKey (tile_data)){
					continue;
				}
				GameObject tile_go = new GameObject();

				tile_go.name = "Tile_" + x + "_" + y;
				tile_go.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);
				SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
				if (tile_data.Type != TileType.Water) {
					sr.sortingLayerName = "Tile";
				}
				if (tile_data.Type == TileType.Water) {
					sr.sprite = waterSprite;
				} else if (tile_data.Type == TileType.Dirt) {
					sr.sprite = dirtSprite;
				} else if (tile_data.Type == TileType.Mountain) {
					sr.sprite = mountainSprite;
				} else {
					Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
				}
				tile_go.transform.SetParent(this.transform, true);
				tileGameObjectMap.Add(tile_data, tile_go);
				OnTileChanged(tile_data);
			}
		}

	}


}
