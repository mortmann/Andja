using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StructureSpriteController : MonoBehaviour {
	public Dictionary<Structure, GameObject> structureGameObjectMap;
	public Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();
	public Sprite circleSprite;
	public Sprite unitCircleSprite;

	BuildController bm;
	CameraController cc;
	World World {
		get { return WorldController.Instance.World; }
	}
	void Start (){
		structureGameObjectMap = new Dictionary<Structure, GameObject> ();
		// here load sprites
//		bm = GameObject.FindObjectOfType<BuildController>();
//		bm.RegisterStructureCreated (OnStrucutureCreated);
		LoadSprites ();
		cc = GameObject.FindObjectOfType<CameraController> ();
		if (EditorController.IsEditor){
			EditorController.Instance.RegisterOnStructureDestroyed (OnTileStructureDestroyed);
		}
	}

	void Update(){
		List<Structure> ts = new List<Structure> (structureGameObjectMap.Keys);
		foreach(Structure str in ts){
			if(cc.structureCurrentInCameraView.Contains (str)==false){
				GameObject.Destroy (structureGameObjectMap[str]);
				structureGameObjectMap.Remove (str);
			}
		}
		foreach (Structure str in cc.structureCurrentInCameraView) {
			if(structureGameObjectMap.ContainsKey (str)==false){
				OnStrucutureCreated (str);
			}
		}
	}
	public void OnStrucutureCreated(Structure structure) {		
		GameObject go = new GameObject ();
		structure.RegisterOnChangedCallback (OnStructureChanged);
		structure.RegisterOnDestroyCallback (OnStructureDestroyed);
		float x = 0;
		float y = 0;
		if (structure.TileWidth> 1) {
			x = 0.5f + ((float)structure.TileWidth) / 2 - 1;
		}
		if (structure.TileHeight> 1) {
			y = 0.5f + ((float)structure.TileHeight) / 2 - 1;
		}
		Tile t = structure.BuildTile;
		go.transform.position = new Vector3 (t.X + x,t.Y + y);
		go.transform.transform.eulerAngles = new Vector3 (0, 0, 360-structure.rotated);
		go.transform.SetParent (this.transform,true);
		go.name = structure.SmallName +"_"+structure.myBuildingTiles [0].ToString ();
		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sortingLayerName = "Structures";
		structureGameObjectMap.Add (structure,go);
		if (structure is Road) {
			((Road)structure).RegisterOnRoadCallback (OnRoadChange);
			GameObject gos= new GameObject();
			TextMesh text = gos.AddComponent<TextMesh> ();
			text.characterSize = 0.1f;
			text.anchor = TextAnchor.MiddleCenter;

			gos.transform.SetParent (go.transform);
			gos.transform.localPosition = Vector3.zero;
			gos.GetComponent<MeshRenderer>().sortingLayerName = "StructuresUI";
			if (((Road)structure).Route != null) {
//				text.text = ((Road)structure).Route.toString ();
			}
			Font ArialFont = (Font)Resources.GetBuiltinResource (typeof(Font), "Arial.ttf");
			text.font = ArialFont;
		} 

		SetSpriteRendererStructureSprite (go, structure);

		if(structure is OutputStructure && ((OutputStructure)structure).ContactRange>0){
			GameObject goContact = new GameObject ();
			CircleCollider2D cc2d = goContact.AddComponent<CircleCollider2D>();
			cc2d.radius = ((OutputStructure)structure).ContactRange;
			cc2d.isTrigger = true;
			goContact.transform.SetParent (go.transform);
			goContact.transform.localPosition = Vector3.zero;
			ContactColliderScript c = goContact.AddComponent<ContactColliderScript>();
			c.contact = ((OutputStructure)structure);
			goContact.name = "ContactCollider";
		}

		if (structure.HasHitbox) {
			BoxCollider2D col = go.AddComponent<BoxCollider2D> ();
			col.size = new Vector2 (sr.sprite.textureRect.size.x /sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
		}
	}
	void OnStructureChanged(Structure structure){			
		if(structure == null){
			Debug.LogError ("Structure change and its empty?");
			return;
		}
		if( structureGameObjectMap.ContainsKey (structure) == false){
//			Debug.LogError ("StructureSprite not in the Map to a gameobject! "+ structure.SmallName+"@"+ structure.myBuildingTiles[0].toString ());
			return;
		}
		if(structure is Growable){
			SpriteRenderer sr = structureGameObjectMap[structure].GetComponent<SpriteRenderer>();
			if(structureSprites.ContainsKey (structure.SmallName + "_" + ((Growable)structure).currentStage))
				sr.sprite = structureSprites[structure.SmallName + "_" + ((Growable)structure).currentStage];
		} 
		else if(structure is Warehouse){
			if (structure.extraUIOn == true) {
                GameObject go = new GameObject {
                    name = "RangeUI"
                };
                go.transform.position = structureGameObjectMap [structure].transform.position;
				go.transform.localScale = new Vector3 (((Warehouse)structure).ContactRange, ((Warehouse)structure).ContactRange, 0);
				SpriteRenderer sr = go.AddComponent<SpriteRenderer> ();
				sr.sprite = circleSprite;
				sr.sortingLayerName = "StructuresUI";
				go.transform.SetParent (structureGameObjectMap [structure].transform);
			} else {
				if(structureGameObjectMap [structure].transform.Find("RangeUI") !=null)
					GameObject.Destroy (structureGameObjectMap [structure].transform.Find("RangeUI").gameObject );
			}
		}
		else if(structure is HomeBuilding){
			SetSpriteRendererStructureSprite (structureGameObjectMap [structure], structure);
		}
	}

	void SetSpriteRendererStructureSprite(GameObject go, Structure str){
		SpriteRenderer sr = go.GetComponent<SpriteRenderer> ();
		if (structureSprites.ContainsKey (str.GetSpriteName())) {
			sr.sprite = structureSprites[str.GetSpriteName()];
		} else {
			sr.sprite =  structureSprites ["nosprite"];
			go.transform.localScale = new Vector3(str.TileWidth,str.TileHeight);
			go.transform.localRotation = Quaternion.identity;
		}
	}

	Sprite GetSprite(string name){
		if (structureSprites.ContainsKey (name)) {
			return structureSprites[name];
		} else {
			return structureSprites ["nosprite"];
		}
	}
	void OnTileStructureDestroyed(Tile t){
		OnStructureDestroyed (t.Structure);
	}
	void OnStructureDestroyed(Structure structure) {
		if(structureGameObjectMap.ContainsKey(structure)==false){
			return;
		}
		GameObject go = structureGameObjectMap [structure];
		GameObject.Destroy (go);
		structure.UnregisterOnChangedCallback (OnStructureChanged);
		structureGameObjectMap.Remove (structure);
	}
		
	public void OnRoadChange(Road road) {
		Structure s = road;
		SetSpriteRendererStructureSprite (structureGameObjectMap [s], s);
		if( road.Route != null) {
			structureGameObjectMap[s].GetComponentInChildren <TextMesh>().text = road.Route.toString ();
		}
	}

	void LoadSprites() {
		structureSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Structures/");
		foreach (Sprite s in sprites) {
//			Debug.Log (s.name);
			structureSprites[s.name] = s;
		}
    }
	public Sprite GetStructureSprite(Structure str){
		if(structureSprites.ContainsKey (str.GetSpriteName())==false){
			//FIXME this should be active in future 
			//fornow there arent many sprites anyway
//			Debug.LogError ("No Structure Sprite for that Name!");
			return null; 
		}
		return GetSprite (str.GetSpriteName());
	}
	public GameObject GetGameObject(Structure str){
		if(structureGameObjectMap.ContainsKey (str)==false){
			return null;
		}
		return structureGameObjectMap [str];
	}
	void OnDestroy() {
		
	}
}
