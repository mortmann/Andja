using UnityEngine;
using System.Collections.Generic;

public class EditorTileSpriteController : MonoBehaviour {

	public static EditorTileSpriteController Instance { get; protected set; }

	public Dictionary<string, List<Sprite>> typeTotileSpriteNames = new Dictionary<string,List<Sprite>>();
	EditorController ec;
	public Sprite oceanSprite;
	public List<Sprite> listOfShore;
	public GameObject background;
	public GameObject backgroundPrefab;

	Dictionary<EditorTile,GameObject> tileGameObjectMap;
	// Use this for initialization
	void Start () {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;
		tileGameObjectMap = new Dictionary<EditorTile, GameObject> ();
		LoadSprites ();
		background = Instantiate (backgroundPrefab);
		background.transform.position = new Vector3((EditorIsland.current.width/2)-0.5f,(EditorIsland.current.height/2)-0.5f , 0.1f);
		background.transform.localScale = new Vector3 (EditorIsland.current.width/10,0.1f,EditorIsland.current.height/10);
		background.GetComponent<Renderer> ().material.mainTexture.wrapMode = TextureWrapMode.Repeat;
		background.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (EditorIsland.current.width, EditorIsland.current.height);

		ec = EditorController.Instance;
		for (int x = 0; x < ec.editorIsland.width; x++) {
			for (int y = 0; y < ec.editorIsland.height; y++) {
				EditorTile tile_data = ec.GetTileAtWorldCoord (x, y);
				if(tile_data.Type==TileType.Ocean){
					continue;
				}
				GameObject tile_go = new GameObject();

				tile_go.name = "Tile_" + x + "_" + y;
				tile_go.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);

				SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
				sr.sortingLayerName = "Tile";
				sr.sprite = oceanSprite;
				tile_go.transform.SetParent(this.transform, true);
				tile_data.RegisterTileChangedCallback (OnTileChanged);
				tileGameObjectMap.Add(tile_data, tile_go);
				OnTileChanged (tile_data);
			}
		}

	}
	public void OnTileChanged(EditorTile tile_data) {
		if (tileGameObjectMap.ContainsKey(tile_data) == false) {
			if(tile_data.Type!=TileType.Ocean){
				GameObject tg = new GameObject();
				tg.name = "Tile_" + tile_data.X + "_" + tile_data.Y;
				tg.transform.position = new Vector3(tile_data.X , tile_data.Y , 0);
				tg.AddComponent<SpriteRenderer>();
				tg.transform.SetParent(this.transform, true);
				tile_data.RegisterTileChangedCallback (OnTileChanged);
				tileGameObjectMap.Add(tile_data, tg);
				OnTileChanged (tile_data);
			}
		}
		GameObject tile_go = tileGameObjectMap[tile_data];

		if (tile_data.Type == TileType.Ocean) {
			GameObject.Destroy (tile_go);
			tileGameObjectMap.Remove (tile_data);
			return;
		}
		if (tile_go == null) {
			Debug.LogError("tileGame ObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
			return;
		}
		SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer> ();
		sr.sortingLayerName = "Tile";



		if (tile_data.Type == TileType.Ocean) {
			sr.GetComponent<SpriteRenderer>().sprite = oceanSprite;
			return;
		} 
		if(typeTotileSpriteNames.ContainsKey (tile_data.Type.ToString ().ToLower ())==false){
			Debug.LogError("typeTotileSpriteNames's returned SpriteNames is null "+tile_data.Type.ToString ().ToLower ());
			return;
		}
		Sprite s;
		if(tile_data.Type==TileType.Shore){
			//take a random one?
			s = listOfShore [Random.Range (0, listOfShore.Count)];
		} else {
			s = typeTotileSpriteNames [tile_data.Type.ToString ().ToLower ()].Find (x => x.name == tile_data.SpriteName);
		}  
		if (s == null) {
			s = typeTotileSpriteNames [tile_data.Type.ToString ().ToLower ()] [0];
			Debug.LogWarning ("this SpriteName doesnt exist! " + tile_data.SpriteName) ; 
		}	
		sr.GetComponent<SpriteRenderer> ().sprite = s;

	}
		
	void LoadSprites() {
		
		typeTotileSpriteNames = new Dictionary<string, List<Sprite>>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TileSprites/");
		foreach (Sprite s in sprites) {
			string type = s.name.Split ('_') [0].ToLower ();
			if(typeTotileSpriteNames.ContainsKey (type)){
				typeTotileSpriteNames [type].Add (s);
			} else {
				List<Sprite> sts = new List<Sprite> ();
				sts.Add (s); 
				typeTotileSpriteNames.Add (type,sts);
			}
			if(type =="shore"){
				listOfShore.Add (s); 
			}
		}
	}

}
