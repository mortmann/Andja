using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Concurrent;
/// <summary>
/// Sprite Names should follow this rules
/// HAVE TO BE SAME SIZE! -> for now 32px
/// HAVE TO BE RGBA32 BIT!
/// NO COMPRESSION!
/// AND FOLLOW THIS NAMEING RULE:
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
    public Sprite emptySprite;

    public GameObject karoOverlay;
	public GameObject tilePrefab;
    public GameObject darkLayer;
    public GameObject waterLayer;
    public GameObject water;

    public Sprite darkLayerSprite;
	
	public Material waterMaterial;
	public Material darkMaterial;
	Material clearMaterial;
	public Material highlightMaterial;
    private Dictionary<Island, Sprite> islandToSprite;
    private Dictionary<Island, Sprite> islandToMaskSprite;
    private int sizeOfSprites;
    private System.Diagnostics.Stopwatch islandSpriteStopWatch;
    private Dictionary<Island, SpriteMask> islandToCityMask;
    private bool apply;

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
            sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
			darkLayer.transform.localScale =  new Vector3 (1.25f*World.Width,1.25f*World.Height,0);
			darkLayer.name="DarkLayer";
			darkLayer.transform.SetParent (this.transform);
			darkLayer.SetActive (true);
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
        CreateIslandSprites(World.Current.IslandList);

        foreach (Island i in World.Current.IslandList) { 
            City c = i.FindCityByPlayer(PlayerController.currentPlayerNumber);
            if (c == null){
                continue;
            }
            foreach (Tile t in c.MyTiles) {
                OnTileChanged(t);
            }
        }
//		BuildController.Instance.RegisterBuildStateChange (OnBuildStateChance);

    }

    public void Update() {
        if (apply) {
            foreach(SpriteMask sm in islandToCityMask.Values) {
                sm.sprite.texture.Apply();
                apply = false;
            }
        }
    }
    public void CreateIslandSprites(List<Island> islands) {
        if (EditorController.IsEditor)
            return;
        islandSpriteStopWatch = new System.Diagnostics.Stopwatch();
        islandSpriteStopWatch.Start();
        sizeOfSprites = 32;
        islandToSprite = new Dictionary<Island, Sprite>();
        islandToMaskSprite = new Dictionary<Island, Sprite>();
        Dictionary<string, Texture2D> spriteNameToTexture2D = new Dictionary<string, Texture2D>();
        foreach (string name in nameToSprite.Keys) {
            spriteNameToTexture2D.Add(name, ConvertSpriteToTexture(nameToSprite[name]));
        }
        foreach (Island i in islands) {
            
            int islandWidth = (i.Width + 1);
            int islandHeight = (i.Height + 1);
            int spriteWidth = islandWidth * sizeOfSprites;
            int spriteHeight = islandHeight * sizeOfSprites;

            //island sprite
            Texture2D islandSprite = new Texture2D(spriteWidth, spriteHeight, TextureFormat.RGBA32, true);
            islandSprite.SetPixels32(new Color32[spriteWidth * spriteHeight]);
            islandSprite.alphaIsTransparency = true;
            //island mask
            Texture2D masktexture = new Texture2D(i.Width + 1, i.Height + 1, TextureFormat.Alpha8, true);
            masktexture.SetPixels32(new Color32[(i.Width + 1) * (i.Height + 1)]);
            
            foreach (Tile tile_data in i.myTiles) {
                int x = (int)(tile_data.X - i.min.x);
                int y = (int)(tile_data.Y - i.min.y);
                string name = spriteNameToTexture2D.ContainsKey(tile_data.SpriteName) ? tile_data.SpriteName : "nosprite";
                Graphics.CopyTexture(spriteNameToTexture2D[name], 0, 0,
                                        0, 0, sizeOfSprites, sizeOfSprites,
                                        islandSprite, 0, 0, x* sizeOfSprites, y* sizeOfSprites);

                masktexture.SetPixel(x, y, new Color32(128, 128, 128, 128));
            }

            //System.IO.File.WriteAllBytes(Application.dataPath + "/../Pictures/" + i.myClimate + ".png", islandSprite.EncodeToPNG());
            islandSprite.Apply(true, false);
            islandToSprite.Add(i, Sprite.Create(islandSprite, new Rect(0, 0, islandSprite.width, islandSprite.height), Vector2.zero, 32));

            masktexture.filterMode = FilterMode.Point;
            masktexture.Apply();
            islandToMaskSprite.Add(i, Sprite.Create(masktexture, new Rect(0, 0, islandWidth, islandHeight), islandToSprite[i].pivot, 1));

        }
        islandToCityMask = new Dictionary<Island, SpriteMask>();
        foreach (Island i in islandToSprite.Keys) {
            //Island Sprites
            GameObject islandGO = new GameObject("Island");
            islandGO.transform.position = new Vector3(i.min.x-0.5f, i.min.y - 0.5f, 0);
            SpriteRenderer sr = islandGO.AddComponent<SpriteRenderer>();
            sr.sprite = islandToSprite[i];
            sr.sortingLayerName = "Tile";
            //Now we create the masks for the islands 
            GameObject cityMaskGameobject = new GameObject("IslandMask");
            cityMaskGameobject.transform.parent = islandGO.transform;
            cityMaskGameobject.transform.localPosition = Vector3.zero;
            SpriteMask sm = cityMaskGameobject.AddComponent<SpriteMask>();
            sm.isCustomRangeActive = true;
            sm.sortingLayerName = "DarkLayer";
            sm.frontSortingLayerID = 638755707; // UI Layer even tho its some strange number
            sm.sprite = islandToMaskSprite[i];
            sm.alphaCutoff = 1;
            islandToCityMask.Add(i, sm);
        }



        islandSpriteStopWatch.Stop();
        Debug.Log("Islandimage " + islandSpriteStopWatch.ElapsedMilliseconds + "ms (" + islandSpriteStopWatch.Elapsed.TotalSeconds + "s)! ");
    }

    private Sprite GetSpriteForName(string spriteName) {
        if (nameToSprite.ContainsKey(spriteName)) {
            return nameToSprite[spriteName];
        }
        return nameToSprite["nosprite"];
    }

    Texture2D ConvertSpriteToTexture(Sprite sprite) {
        try {
            if (sprite.rect.width != sprite.texture.width) {
                Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] newColors = sprite.texture.GetPixels((int)System.Math.Ceiling(sprite.textureRect.x),
                                                             (int)System.Math.Ceiling(sprite.textureRect.y),
                                                             (int)System.Math.Ceiling(sprite.textureRect.width),
                                                             (int)System.Math.Ceiling(sprite.textureRect.height));
                newText.SetPixels(newColors);
                newText.Apply();
                return newText;
            }
            else
                return sprite.texture;
        }
        catch {
            Debug.LogError("ConvertSpriteToTexture failed with " + sprite.name);
            return sprite.texture;
        }
    }

    void OnTileChanged(Tile tile_data) {
        //this is cheaper to compare than to look it up in a dictionary
        if (tile_data == null || tile_data.Type == TileType.Ocean) {
            if (EditorController.IsEditor && tileSpriteRendererMap.ContainsKey(tile_data)) {
                Destroy(tileSpriteRendererMap[tile_data].gameObject);
                tileSpriteRendererMap.Remove(tile_data);
            }
            return;
        }
        if (EditorController.IsEditor && tileSpriteRendererMap.ContainsKey(tile_data) == false) {
            SpawnTile(tile_data);
        }
        int x = (int)(tile_data.X - tile_data.MyIsland.min.x);
        int y = (int)(tile_data.Y - tile_data.MyIsland.min.y);
        islandToCityMask[tile_data.MyIsland].sprite.texture.SetPixel(x, y, new Color32(128, 128, 128, 255));

        apply = true;

        if (TileDeciderFunc != null) {
            darkLayer.SetActive(true);
            
            TileMark tm = TileDeciderFunc(tile_data);
            switch (tm) {
                case TileMark.None:
                //sr.material = clearMaterial;
                //					sr.sortingLayerName = "Tile";
                break;
                case TileMark.Highlight:
                //sr.material = highlightMaterial;
                //					sr.sortingLayerName = "Tile";
                break;
                case TileMark.Dark:
                //sr.material = darkMaterial;
                //					sr.sortingLayerName = "DarkTile";
                break;
            }
        }
        else {
            //sr.material = clearMaterial;
            if (darkLayer != null)
                darkLayer.SetActive(false);
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
	public void SpawnTile(Tile tile_data){
		if(EditorController.IsEditor&& tileSpriteRendererMap.ContainsKey (tile_data)){
			return;
		}

		GameObject tile_go = SimplePool.Spawn( tilePrefab, new Vector3(tile_data.X, tile_data.Y, 0), Quaternion.identity );
		tile_go.name = "Tile_" + tile_data.X + "_" + tile_data.Y;
		tile_go.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);
		SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer>();
		sr.sortingLayerName = "Tile";
		tile_go.transform.SetParent(this.transform, true);
		tileSpriteRendererMap.Add(tile_data, sr);
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
                if (EditorController.IsEditor)
                    Debug.Log("Missing Sprite for Shore " + tile_data.SpriteName);
                sr.sprite = nameToSprite["shore_"];
            }
        }

        if (sr.sprite == null) {
            sr.sprite = noSprite;
        }
        OnTileChanged(tile_data);


		
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
	public void AddDecider(TileDecider addDeciderFunc, bool isCityDecider = false){
		this.TileDeciderFunc += addDeciderFunc;
        if (TileDeciderFunc != null || TileDeciderFunc.GetInvocationList().Length > 0)
            darkLayer.SetActive(true);
        if (isCityDecider)
            islandToCityMask.ToList().ForEach(x=>x.Value.gameObject.SetActive(true));
    }
	public void ResetDecider(){
		//Debug.Log ("RESET");
		TileDeciderFunc = null;
		darkLayer.SetActive (false);
	}
	public void RemoveDecider(TileDecider removeFunc, bool isCityDecider = false) {
		TileDeciderFunc -= removeFunc;
		if(TileDeciderFunc==null || TileDeciderFunc.GetInvocationList().Length==0)
			darkLayer.SetActive (false);

        if (isCityDecider)
            islandToCityMask.ToList().ForEach(x => x.Value.gameObject.SetActive(false));

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
