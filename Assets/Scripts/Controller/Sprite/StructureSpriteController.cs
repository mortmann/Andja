using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class StructureSpriteController : MonoBehaviour {
	Dictionary<Structure, GameObject> structureGameObjectMap;
	Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();
	public Sprite circleSprite;
	BuildController bm;

	World world {
		get { return WorldController.Instance.world; }
	}

	void Start () {
		structureGameObjectMap = new Dictionary<Structure, GameObject> ();
		// here load sprites
		bm = GameObject.FindObjectOfType<BuildController>();
		bm.RegisterStructureCreated (OnStrucutureCreated);
		LoadSprites ();
	}
	void OnStrucutureCreated(Structure structure) {
		
		GameObject go = new GameObject ();
		structure.RegisterOnChangedCallback (OnStructureChanged);
		structure.RegisterOnDestroyCallback (OnStructureDestroyed);

		float x = 0;
		float y = 0;
		if (structure.tileWidth> 1) {
			x = 0.5f + ((float)structure.tileWidth) / 2 - 1;
		}
		if (structure.tileHeight> 1) {
			y = 0.5f + ((float)structure.tileHeight) / 2 - 1;
		}
		Tile t = structure.BuildTile;

		go.transform.position = new Vector3 (t.X + x,t.Y + y);
		go.transform.Rotate (Vector3.forward*structure.rotated); // = new Quaternion (0, 0, structure.rotated, 100);
		go.transform.SetParent (this.transform,true);
		go.name = structure.name +"_"+structure.myBuildingTiles [0].toString ();
		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sortingLayerName = "Structures";
		structureGameObjectMap.Add (structure,go);
		if (structure is Road) {
			((Road)structure).RegisterOnRoadCallback (OnRoadChange);
			Text text = go.AddComponent<Text> ();
			text.text = ((Road)structure).Route.toString ();
			Font ArialFont = (Font)Resources.GetBuiltinResource (typeof(Font), "Arial.ttf");
			text.font = ArialFont;
			text.material = ArialFont.material;
			if (structureSprites.ContainsKey (structure.name + structure.connectOrientation)) {
				sr.sprite = structureSprites [structure.name + structure.connectOrientation];
			} else {
				sr.sprite = structureSprites ["nosprite"];
			}
		} else if (structure is Growable) {
			if (structureSprites.ContainsKey (structure.name + "_" + ((Growable)structure).currentStage)) {
				sr.sprite = structureSprites [structure.name + "_" + ((Growable)structure).currentStage];
			} else {
				sr.sprite = structureSprites ["nosprite"];
			}
		} else {
			if (structureSprites.ContainsKey (structure.name)) {
				sr.sprite = structureSprites[structure.name];
			} else {
				Sprite sprite = structureSprites ["nosprite"];
				go.transform.localScale = new Vector3(structure.tileWidth,structure.tileHeight);
				sr.sprite = sprite;
			}
		}
		if (structure.hasHitbox) {
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
			Debug.LogError ("StructureSprite not in the Map to a gameobject! "+ structure.name+"@"+ structure.myBuildingTiles[0].toString ());
			return;
		}
		if(structure is Growable){
			SpriteRenderer sr = structureGameObjectMap[structure].GetComponent<SpriteRenderer>();
			sr.sprite = structureSprites[structure.name + "_" + ((Growable)structure).currentStage];
		}
		if(structure is Warehouse){
			GameObject go = new GameObject ();
			go.transform.position = structureGameObjectMap [structure].transform.position;
			go.transform.localScale = new Vector3(((Warehouse)structure).shipRange,((Warehouse)structure).shipRange,0);
			go.AddComponent<SpriteRenderer> ().sprite = circleSprite;
			go.transform.SetParent (structureGameObjectMap [structure].transform);
		}
	}
	void OnStructureDestroyed(Structure structure) {
		GameObject go = structureGameObjectMap [structure];
		GameObject.Destroy (go);
		structure.UnregisterOnChangedCallback (OnStructureChanged);
		structureGameObjectMap.Remove (structure);
	}
		
	public void OnRoadChange(Road road) {
		Structure s = road;
		structureGameObjectMap[s].GetComponent<Text>().text = road.Route.toString ();

		SpriteRenderer sr = structureGameObjectMap[s].GetComponent<SpriteRenderer>();
		if (structureSprites.ContainsKey (road.name + road.connectOrientation)) {
			sr.sprite = structureSprites [road.name + road.connectOrientation];
		} else {
			sr.sprite = structureSprites ["nosprite"];
		}
	}

	void LoadSprites() {
		structureSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Structures/");
		foreach (Sprite s in sprites) {
//			Debug.Log (s.name);
			structureSprites[s.name] = s;
		}
	}

}
