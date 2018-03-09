﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TileSpriteController : MonoBehaviour {

	public static TileSpriteController Instance { get; protected set; }

	Dictionary<Tile, SpriteRenderer> tileSpriteRendererMap;
    public Sprite dirtSprite;
	public Sprite mountainSprite;
	public Sprite shoreSprite;
	public GameObject tilePrefab;

	public Sprite darkLayerSprite;
	GameObject darkLayer;
	public GameObject waterLayer;
	public GameObject water;
	public Dictionary<string, Sprite> typeTotileSpriteNames;
	public Material waterMaterial;
	public Material darkMaterial;
	Material clearMaterial;
	public Material highlightMaterial;

	public delegate TileMark TileDecider(Tile tile);
	public event TileDecider tileDeciderFunc;

    // The pathfinding graph used to navigate our world map.
    World world {
		get { return World.current; }
    }

    // Use this for initialization
    void Start () {
		if (Instance != null) {
			Debug.LogError("There should never be two mouse controllers.");
		}
		Instance = this;
		tileSpriteRendererMap = new Dictionary<Tile, SpriteRenderer>();
		water = Instantiate (waterLayer);
		water.transform.position = new Vector3((world.Width/2)-0.5f,(world.Height/2)-0.5f , 0.1f);
		water.transform.localScale = new Vector3 (6+world.Width/10,0.1f,6+world.Height/10);
		water.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (world.Width, world.Height*3);
		LoadSprites ();

		//DarkLayer probably gonna be changed
		darkLayer = new GameObject ();
		darkLayer.transform.position = new Vector3((world.Width/2)-0.5f,(world.Height/2)-0.5f , 0);
		SpriteRenderer sr = darkLayer.AddComponent<SpriteRenderer> ();

		sr.sprite = darkLayerSprite;
		sr.sortingLayerName = "DarkLayer";

		darkLayer.transform.localScale =  new Vector3 (1.25f*world.Width,1.25f*world.Height,0);
		darkLayer.name="DarkLayer";
		darkLayer.transform.SetParent (this.transform);
		darkLayer.SetActive (false);


        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.RegisterTileChanged(OnTileChanged);
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
		if(tile_data==null || tile_data.Type == TileType.Ocean){
			return;
		}
        if (tileSpriteRendererMap.ContainsKey(tile_data) == false) {
//            Debug.LogError("tileGameObjectMap doesn't contain the tile_data -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

		SpriteRenderer sr = tileSpriteRendererMap[tile_data];

		if (sr == null) {
            Debug.LogError("tileGame ObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }
		if(clearMaterial==null){
			clearMaterial = sr.material;
		}
		//for now the tile knows what a sprite has for one for know
		if(tile_data.SpriteName != null && typeTotileSpriteNames.ContainsKey (tile_data.SpriteName)){
			sr.sprite = typeTotileSpriteNames [tile_data.SpriteName];
		} 
		//the sprite gone missing? --- Made a mistake in nameing it?
		if (tile_data.Type == TileType.Dirt) {
			sr.sprite = dirtSprite;
		} else if (tile_data.Type == TileType.Mountain) {
			sr.sprite = mountainSprite;
		} else if (tile_data.Type == TileType.Shore) {
			sr.sprite = shoreSprite;
		} 
		else {
			Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
		}
    }

	public void DespawnTile(Tile t){
		if(tileSpriteRendererMap.ContainsKey(t)==false){
			return;
		}
		SimplePool.Despawn (tileSpriteRendererMap [t].gameObject);
		tileSpriteRendererMap.Remove (t);
	}
	public void SpawnTile(Tile t){
		GameObject tile_go = SimplePool.Spawn( tilePrefab, new Vector3(t.X, t.Y, 0), Quaternion.identity );
		tile_go.name = "Tile_" + t.X + "_" + t.Y;
		tile_go.transform.position = new Vector3(t.X , t.Y , 0);
		SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer>();
		sr.sortingLayerName = "Tile";
		tile_go.transform.SetParent(this.transform, true);
		tileSpriteRendererMap.Add(t, sr);
		OnTileChanged(t);
		//			}


		SpriteRenderer sprite = tileSpriteRendererMap[t];
		if(tileDeciderFunc!=null){
			darkLayer.SetActive (true);
			TileMark tm = tileDeciderFunc (t);
			switch (tm) {
			case TileMark.None:
				sprite.material = clearMaterial;
				//					sr.sortingLayerName = "Tile";
				break;
			case TileMark.Highlight:
				sprite.material = highlightMaterial;
				//					sr.sortingLayerName = "Tile";
				break;
			case TileMark.Dark:
				sprite.material = darkMaterial;
				//					sr.sortingLayerName = "DarkTile";
				break;
			}
		} else {
			sprite.material = clearMaterial;
			darkLayer.SetActive (false);
			//				sr.sortingLayerName = "Tile";
		}
	}

	TileMark TileCityDecider(Tile t){
		if(t.myCity.IsCurrPlayerCity ()){
			return TileMark.None;
		} else {
			return TileMark.Dark;
		}
	}

	void LoadSprites() {
		typeTotileSpriteNames = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
		foreach (Sprite s in sprites) {
			typeTotileSpriteNames.Add (s.name, s);
		}
	}
	void OnDestroy() {
		world.UnregisterTileChanged (OnTileChanged);
	}
	public void AddDecider(TileDecider addDeciderFunc){
		this.tileDeciderFunc += addDeciderFunc;
	}
	public void ResetDecider(){
		Debug.Log ("RESET");
		tileDeciderFunc = null;
		darkLayer.SetActive (false);
	}
	public void removeDecider(TileDecider removeFunc){
		tileDeciderFunc -= removeFunc;
		if(tileDeciderFunc==null || tileDeciderFunc.GetInvocationList().Length==0)
			darkLayer.SetActive (false);

	}
}
