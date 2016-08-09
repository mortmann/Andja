using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnitSpriteController : MonoBehaviour {
    private Dictionary<string, Sprite> unitSprites;
    private Dictionary<Unit, GameObject> unitGameObjectMap;

    World world {
        get { return WorldController.Instance.world; }
    }
    // Use this for initialization
    void Start () {
        unitGameObjectMap = new Dictionary<Unit, GameObject>();
        LoadSprites();
        world.RegisterUnitCreated(OnUnitCreated);
		foreach (var item in world.units) {
			OnUnitCreated (item);
		}        
    }

	public void OnUnitCreated(Unit u) {
        // Create a visual GameObject linked to this data.
        // Create a 2d box collider around the unit


        // This creates a new GameObject and adds it to our scene.
		GameObject char_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        unitGameObjectMap.Add(u, char_go);

        char_go.name = "Ship";
        char_go.transform.SetParent(this.transform, true);
		char_go.AddComponent<UnitHoldingScript> ().unit=u;
		Rigidbody2D r2d = char_go.AddComponent<Rigidbody2D> (); 
		r2d.gravityScale = 0;       
		BoxCollider2D col = char_go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(unitSprites["ship"].textureRect.size.x / unitSprites["ship"].pixelsPerUnit, unitSprites["ship"].textureRect.size.y / unitSprites["ship"].pixelsPerUnit);
//        unitSprites["ship"].rect.size / unitSprites["ship"].pixelsPerUnit;    
        u.width = unitSprites["ship"].textureRect.size.x / unitSprites["ship"].pixelsPerUnit;
        u.height = unitSprites["ship"].textureRect.size.y / unitSprites["ship"].pixelsPerUnit;
        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprites["ship"];
        sr.sortingLayerName = "Units";

		u.SetGameObject (char_go);
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        u.RegisterOnChangedCallback(OnUnitChanged);
    }
    void OnUnitChanged(Unit c) {
        //Debug.Log("OnFurnitureChanged");
        // Make sure the furniture's graphics are correct.

        if (unitGameObjectMap.ContainsKey(c) == false) {
            Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
            return;
        }

        GameObject char_go = unitGameObjectMap[c];
        //Debug.Log(furn_go);
        //Debug.Log(furn_go.GetComponent<SpriteRenderer>());

        //char_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);

        char_go.transform.position = new Vector3(c.X, c.Y, 0);
    }

    void LoadSprites() {
        unitSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Units/");
        foreach (Sprite s in sprites) {
            unitSprites[s.name] = s;
        }
    }
}
