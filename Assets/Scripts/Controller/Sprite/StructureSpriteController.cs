using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class StructureSpriteController : MonoBehaviour {
	Dictionary<Structure, GameObject> structureGameObjectMap;
	Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();

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
		structure.RegisterOnChangedCallback (OnStrucutureChanged);
		float x = 0;
		float y = 0;
		if (structure.tileWidth> 1) {
			x = 0.5f + ((float)structure.tileWidth) / 2 - 1;
		}
		if (structure.tileHeight> 1) {
			y = 0.5f + ((float)structure.tileHeight) / 2 - 1;
		}
		Tile t = structure.myBuildingTiles[0];
//		float z;
//		if (structure.rotated == 0) {
//			z = 0;
//		}
		structure.RegisterOnDestroyCallback (OnStrucutureDestroyed);


		go.transform.position = new Vector3 (t.X + x,t.Y + y);
		go.transform.Rotate (Vector3.forward*structure.rotated); // = new Quaternion (0, 0, structure.rotated, 100);
//		go.transform.Rotate (new Vector3(0,0,structure.rotated));
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
			sr.sprite = structureSprites [structure.name + structure.connectOrientation];
		} else if (structure is Growable) {
			sr.sprite = structureSprites [structure.name + "_0"];
		} else {
			if (structure.hasHitbox) {
				BoxCollider2D col = go.AddComponent<BoxCollider2D> ();
				col.size = new Vector2 (structureSprites [structure.name].textureRect.size.x / structureSprites [structure.name].pixelsPerUnit, structureSprites [structure.name].textureRect.size.y / structureSprites [structure.name].pixelsPerUnit);
			}
			sr.sprite = structureSprites[structure.name];
		}
	}
	void OnStrucutureChanged(Structure structure){
		if(structure == null){
			Debug.LogError ("Structure change and its empty?");
			return;
		}
		if( structureGameObjectMap.ContainsKey (structure) == false){
			Debug.LogError ("StructureSprite not in the Map to a gameobject! " + structure.myBuildingTiles[0].toString ());
			return;
		}
		if(structure is Growable){
			SpriteRenderer sr = structureGameObjectMap[structure].GetComponent<SpriteRenderer>();
			sr.sprite = structureSprites[structure.name + "_" + ((Growable)structure).currentStage];
		}
	}
	void OnStrucutureDestroyed(Structure structure) {
		GameObject go = structureGameObjectMap [structure];
		GameObject.Destroy (go);
		structureGameObjectMap.Remove (structure);
	}
		
	public void OnRoadChange(Road road) {
		Structure s = road;
		structureGameObjectMap[s].GetComponent<Text>().text = road.Route.toString ();

		SpriteRenderer sr = structureGameObjectMap[s].GetComponent<Text>().GetComponent<SpriteRenderer>();
		sr.sprite = structureSprites[road.name + road.connectOrientation];
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
