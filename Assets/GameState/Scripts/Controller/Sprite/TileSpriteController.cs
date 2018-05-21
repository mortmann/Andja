using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
/// <summary>
/// Sprite Names should follow this rule
/// climate_tiletyp_version_connection
/// if climate == null:
/// tiletyp_version_connectionSameType
/// there could be in future:
/// climate_tiletyp_2ndTileType_version_connectionSameType_connection2ndType
/// </summary>
public class TileSpriteController : MonoBehaviour {

	public static TileSpriteController Instance { get; protected set; }

	Dictionary<Tile, SpriteRenderer> tileSpriteRendererMap;
    public static Dictionary<string, Sprite> nameToSprite;
    private static Dictionary<TileType, Dictionary<string, List<string>>> typeTotileSpriteNames;

    public Sprite noSprite;
    public GameObject karoOverlay;
	public GameObject tilePrefab;
    GameObject darkLayer;
    public GameObject waterLayer;
    public GameObject water;

    public Sprite darkLayerSprite;
	
	public Material waterMaterial;
	public Material darkMaterial;
	Material clearMaterial;
	public Material highlightMaterial;

    public delegate TileMark TileDecider(Tile tile);
	public event TileDecider TileDeciderFunc;

    public enum TileSpriteClimate { cold, middle, warm, all }

    // The pathfinding graph used to navigate our world map.
    World World {
		get { return World.Current; }
    }

    // Use this for initialization
    void OnEnable () {
		if (Instance != null) {
			Debug.LogError("There should never be two mouse controllers.");
		}
		Instance = this;
		tileSpriteRendererMap = new Dictionary<Tile, SpriteRenderer>();

		water = Instantiate (waterLayer);

        LoadSprites();

		//DarkLayer probably gonna be changed
		if(EditorController.IsEditor==false){
			water.transform.position = new Vector3((World.Width/2)-0.5f,(World.Height/2)-0.5f , 0.1f);
			water.transform.localScale = new Vector3 (6+World.Width/10,0.1f,6+World.Height/10);
	//		water.GetComponent<Renderer> ().material = waterMaterial;
			water.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (World.Width, World.Height*3);

			darkLayer = new GameObject ();
			darkLayer.transform.position = new Vector3((World.Width/2)-0.5f,(World.Height/2)-0.5f , 0);
			SpriteRenderer sr = darkLayer.AddComponent<SpriteRenderer> ();
			sr.sprite = darkLayerSprite;
			sr.sortingLayerName = "DarkLayer";
			darkLayer.transform.localScale =  new Vector3 (1.25f*World.Width,1.25f*World.Height,0);
			darkLayer.name="DarkLayer";
			darkLayer.transform.SetParent (this.transform);
			darkLayer.SetActive (false);
		} else {
//			karoOverlayInstance = Instantiate ( karoOverlay );
//			karoOverlayInstance.GetComponent <MeshRenderer> ().material.mainTextureScale = new Vector2 (world.Width, world.Height);
//			karoOverlayInstance.transform.position = new Vector3((world.Width/2)-0.5f,(world.Height/2)-0.5f , 0);
//			karoOverlayInstance.transform.localScale =  new Vector3 (world.Width,1f,world.Height);
			water.transform.position = new Vector3((World.Width/2)-0.5f,(World.Height/2)-0.5f, 0.1f);
			water.transform.localScale = new Vector3 (World.Width/10,0.1f,World.Height/10);
			water.GetComponent<Renderer> ().material = waterMaterial;
			water.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (World.Width, World.Height);

		}

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        World.RegisterTileChanged(OnTileChanged);

//		BuildController.Instance.RegisterBuildStateChange (OnBuildStateChance);

    }
    //	void OnBuildStateChance(BuildStateModes bsm){
    //		if(bsm != BuildStateModes.None){
    //			if (darkLayer != null) {
    //				return;
    //			}
    //			darkLayer.SetActive (true);
    //		} else {
    //			darkLayer.SetActive (false);
    //			tileDeciderFunc = null;
    //		}
    //	}
    //
    void OnTileChanged(Tile tile_data) {
        //this is cheaper to compare than to look it up in a dictionary
        if (tile_data == null || tile_data.Type == TileType.Ocean) {
            if (EditorController.IsEditor && tileSpriteRendererMap.ContainsKey(tile_data)) {
                Destroy(tileSpriteRendererMap[tile_data].gameObject);
                tileSpriteRendererMap.Remove(tile_data);
            }
            return;
        }
        if (tileSpriteRendererMap.ContainsKey(tile_data) == false) {
            if (EditorController.IsEditor) {
                SpawnTile(tile_data);
                return;
            }
            return;
        }
        SpriteRenderer sr = tileSpriteRendererMap[tile_data];

        if (sr == null) {
            Debug.LogError("tileGame ObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }
        if (clearMaterial == null) {
            clearMaterial = sr.material;
        }
        //for now the tile knows what a sprite has for one for know
        if (tile_data.SpriteName != null && nameToSprite.ContainsKey(tile_data.SpriteName)) {
            sr.sprite = nameToSprite[tile_data.SpriteName];
        }
        //TODO: Fix it so far that this temporary fix isnt needed anymore
        if (tile_data.Type == TileType.Shore) {
            if (sr.sprite == null) {
                Debug.Log("Missing Sprite for Shore " + tile_data.SpriteName);
                sr.sprite = nameToSprite["shore_"];
            }
        }
		
        if(sr.sprite == null) {
            sr.sprite = noSprite;
        }
        if (TileDeciderFunc != null) {
            darkLayer.SetActive(true);
            TileMark tm = TileDeciderFunc(tile_data);
            switch (tm) {
                case TileMark.None:
                sr.material = clearMaterial;
                //					sr.sortingLayerName = "Tile";
                break;
                case TileMark.Highlight:
                sr.material = highlightMaterial;
                //					sr.sortingLayerName = "Tile";
                break;
                case TileMark.Dark:
                sr.material = darkMaterial;
                //					sr.sortingLayerName = "DarkTile";
                break;
            }
        }
        else {
            sr.material = clearMaterial;
            if (darkLayer != null)
                darkLayer.SetActive(false);
            //				sr.sortingLayerName = "Tile";
        }

    }
    public void DespawnTile(Tile t){
		if(tileSpriteRendererMap.ContainsKey(t)==false){
			return;
		}
        tileSpriteRendererMap[t].sprite = null; //removing for now because not everything has a sprite
        SimplePool.Despawn (tileSpriteRendererMap [t].gameObject);
		tileSpriteRendererMap.Remove (t);
	}
	public void SpawnTile(Tile t){
		if(EditorController.IsEditor&& tileSpriteRendererMap.ContainsKey (t)){
			return;
		}
		GameObject tile_go = SimplePool.Spawn( tilePrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		tile_go.name = "Tile_" + t.X + "_" + t.Y;
		tile_go.transform.position = new Vector3(t.X , t.Y , 0);
		SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer>();
		sr.sortingLayerName = "Tile";
		tile_go.transform.SetParent(this.transform, true);
		tileSpriteRendererMap.Add(t, sr);
		OnTileChanged(t);

		
	}

	TileMark TileCityDecider(Tile t){
		if(t.MyCity.IsCurrPlayerCity ()){
			return TileMark.None;
		} else {
			return TileMark.Dark;
		}
	}

    static void LoadSprites() {
		nameToSprite = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
		foreach (Sprite s in sprites) {
			nameToSprite.Add (s.name, s);
		}

        typeTotileSpriteNames = new Dictionary<TileType, Dictionary<string, List<string>>>();
        
        Climate current = EditorController.climate;
        foreach (string s in TileSpriteController.nameToSprite.Keys) {
            string part = s.Split('_')[0].ToLower();
            string climateIdentifier = TileSpriteClimate.all.ToString();
            //if the first identifier is a climate
            try {
                Climate climate = (Climate)Enum.Parse(typeof(Climate), part, true);
                climateIdentifier = climate.ToString();
                part = s.Split('_')[1].ToLower();
            }
            catch {

            }

            TileType type;
            try {
                type = (TileType)Enum.Parse(typeof(TileType), part, true);
            } catch {
                continue;
            }

            //			Debug.Log (type + " / " + s.name);
            if (typeTotileSpriteNames.ContainsKey(type) == false) {
                Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
                typeTotileSpriteNames.Add(type, dict);
            }
            if (typeTotileSpriteNames[type].ContainsKey(climateIdentifier)) {
                typeTotileSpriteNames[type][climateIdentifier].Add(s);
            }
            else {
                typeTotileSpriteNames[type].Add(climateIdentifier, new List<string> { s });
            }
        }
    }
	void OnDestroy() {
		World.UnregisterTileChanged (OnTileChanged);
	}
	public void AddDecider(TileDecider addDeciderFunc){
		this.TileDeciderFunc += addDeciderFunc;
	}
	public void ResetDecider(){
		//Debug.Log ("RESET");
		TileDeciderFunc = null;
		darkLayer.SetActive (false);
	}
	public void RemoveDecider(TileDecider removeFunc){
		TileDeciderFunc -= removeFunc;
		if(TileDeciderFunc==null || TileDeciderFunc.GetInvocationList().Length==0)
			darkLayer.SetActive (false);

	}
    public static List<string> GetSpriteNamesForType(TileType type, Climate climate) {
        string climateString = climate.ToString();
        if (typeTotileSpriteNames == null)
            LoadSprites();
        if (typeTotileSpriteNames.ContainsKey(type) == false)
            return null;
        if(typeTotileSpriteNames[type].ContainsKey(climateString) == false) {
            if (typeTotileSpriteNames[type].ContainsKey(TileSpriteClimate.all.ToString()) == false) {
                return null;
            }
            return typeTotileSpriteNames[type][TileSpriteClimate.all.ToString()];
        }
        return typeTotileSpriteNames[type][climateString];
    }


}
