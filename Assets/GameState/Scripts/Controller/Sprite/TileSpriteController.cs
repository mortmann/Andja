using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileSpriteController : MonoBehaviour {
    Dictionary<Tile, GameObject> tileGameObjectMap;
	CameraController cc;
    public Sprite dirtSprite;
	public Sprite mountainSprite;
	public Sprite darkLayerSprite;
	GameObject darkLayer;
	public GameObject waterLayer;
	public GameObject water;
	public Dictionary<string, Sprite> typeTotileSpriteNames;
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
		LoadSprites ();
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
		//this is cheaper to compare than to look it up in a dictionary
		if(tile_data==null || tile_data.Type == TileType.Ocean){
			return;
		}
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
		switch (tile_data.TileState) {
			case TileMark.None:
				sr.material = clearMaterial;
				break;
			case TileMark.Highlight:
				sr.material = highlightMaterial;
				break;
			case TileMark.Dark:
				sr.material = darkMaterial;
				break;
		}
		//Now we are at the point were the sprites gets assigned
		//for now the tile knows what a sprite has for one for know
		if(tile_data.SpriteName != null && typeTotileSpriteNames.ContainsKey (tile_data.SpriteName)){
			tile_go.GetComponent<SpriteRenderer> ().sprite = typeTotileSpriteNames [tile_data.SpriteName];
		} 
		//the sprite gone missing? --- Made a mistake in nameing it?
		if (tile_data.Type == TileType.Dirt) {
			tile_go.GetComponent<SpriteRenderer>().sprite = dirtSprite;
		} else if (tile_data.Type == TileType.Mountain) {
			tile_go.GetComponent<SpriteRenderer>().sprite = mountainSprite;
		} else {
			Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
		}
    }
	public void Update(){
		List<Tile> ts = new List<Tile> (tileGameObjectMap.Keys);
		foreach(Tile t in ts){
			if(cc.tilesCurrentInCameraView.Contains (t)==false){
				GameObject.Destroy (tileGameObjectMap[t]);
				tileGameObjectMap.Remove (t);
			}
		}
		foreach (Tile tile_data in cc.tilesCurrentInCameraView) {
			if(tileGameObjectMap.ContainsKey (tile_data)){
				continue;
			}
			GameObject tile_go = new GameObject();

			tile_go.name = "Tile_" + tile_data.X + "_" + tile_data.Y;
			tile_go.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);

			SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
			sr.sortingLayerName = "Tile";
			tile_go.transform.SetParent(this.transform, true);
			tileGameObjectMap.Add(tile_data, tile_go);
			OnTileChanged(tile_data);

		}

	}

	void LoadSprites() {
		typeTotileSpriteNames = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
		foreach (Sprite s in sprites) {
			typeTotileSpriteNames.Add (s.name, s);
		}
	}
}
