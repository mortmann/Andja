using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnitSpriteController : MonoBehaviour {
    private Dictionary<string, Sprite> unitSprites;
    public Dictionary<Unit, GameObject> unitGameObjectMap;
	public GameObject unitGoalPrefab;
	private GameObject unitGoalGO;
	public GameObject unitPathPrefab;
	public GameObject unitCirclePrefab;

	private Unit circleUnit;
	private const string circleGOname = "buildrange_circle_gameobject";
	MouseController mouseController;
    World World {
        get { return World.Current; }
    }
    // Use this for initialization
    void Start () {
		
        unitGameObjectMap = new Dictionary<Unit, GameObject>();
        LoadSprites();
        World.RegisterUnitCreated(OnUnitCreated);
		foreach (var item in World.Units) {
			OnUnitCreated (item);
		}        		
		mouseController = MouseController.Instance;
		unitGoalGO = Instantiate (unitGoalPrefab);
		unitGoalGO.SetActive (false);

		BuildController.Instance.RegisterBuildStateChange (OnBuildStateChange);
    }


	void Update(){
		if(mouseController.mouseState == MouseState.Unit){
			if(mouseController.SelectedUnit==null){
				Debug.LogError ("There is something wrong with MouseController State Unit. No unitselected!");
				return;
			}
			//if we are here there is a unit we can show movement
			//if the unit is not at his destination we have to show it.
			if(mouseController.SelectedUnit.pathfinding.IsAtDest){
				//it doesnt move so return 
				unitGoalGO.SetActive (false);
				return;
			}
			unitGoalGO.SetActive (true);
			Pathfinding p = mouseController.SelectedUnit.pathfinding;
			unitGoalGO.transform.position = new Vector3 (p.dest_X, p.dest_Y);
		}
	}
	public void OnUnitCreated(Unit u) {
        // Create a visual GameObject linked to this data.
        // Create a 2d box collider around the unit


        // This creates a new GameObject and adds it to our scene.
		GameObject char_go = new GameObject();
		GameObject line_go = Instantiate (unitPathPrefab);
		line_go.transform.SetParent (char_go.transform);
        // Add our tile/GO pair to the dictionary.
        unitGameObjectMap.Add(u, char_go);
		SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
		sr.sortingLayerName = "Units";

		if(u.isShip){
			char_go.name = "Ship";
			sr.sprite = unitSprites["ship"];

		} else {
			sr.sprite = unitSprites["unit"];
			char_go.name = u.Name;
		}
        char_go.transform.SetParent(this.transform, true);
		char_go.AddComponent<UnitHoldingScript> ().unit=u;
		Rigidbody2D r2d = char_go.AddComponent<Rigidbody2D> (); 
		r2d.gravityScale = 0;       
		BoxCollider2D col = char_go.AddComponent<BoxCollider2D>();
		col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
		//u.width = sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit;
		//u.height = sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit;
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        u.RegisterOnChangedCallback(OnUnitChanged);
		u.RegisterOnDestroyCallback (OnUnitDestroy);
    }
    void OnUnitChanged(Unit c) {
        if (unitGameObjectMap.ContainsKey(c) == false) {
            Debug.LogError("OnUnitChanged -- trying to change visuals for character not in our map.");
            return;
        }
        GameObject char_go = unitGameObjectMap[c];
		if(c is Ship){
			if(((Ship)c).isOffWorld){
				char_go.SetActive (false);
			} else {
				char_go.SetActive (true);
			}
			//change this so it does use the rigidbody to move
			char_go.transform.position = new Vector3(c.X, c.Y, 0);
			char_go.transform.rotation = new Quaternion (0, 0, c.Rotation, 0);
		} else {
			char_go.transform.position = new Vector3(c.X, c.Y, 0);
			Quaternion q = char_go.transform.rotation;
			q.eulerAngles = new Vector3 (0, 0, c.Rotation);
			char_go.transform.rotation = q;
		}
    }
	void OnUnitDestroy(Unit c) {
		if (unitGameObjectMap.ContainsKey(c) == false) {
			Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
			return;
		}

		GameObject char_go = unitGameObjectMap[c];
		Destroy (char_go);
		unitGameObjectMap.Remove (c);
	}
    void LoadSprites() {
        unitSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Units/");
        foreach (Sprite s in sprites) {
            unitSprites[s.name] = s;
        }
    }
	void OnDestroy() {
		World.UnregisterUnitCreated (OnUnitCreated);
	}

	void OnBuildStateChange(BuildStateModes bsm){
		if(bsm != BuildStateModes.Build ){
			RemoveBuildCircle ();
			return;
		}
		CreateBuildCircle ();
	}
	void RemoveBuildCircle (){
		if(circleUnit==null){
			return; // can be because cheats
		}
		if(unitGameObjectMap.ContainsKey(circleUnit)==false){
			return;//maybe it has been destroyed or other bug calls this function twice or cheats cause to call this without create
		}
		GameObject go = unitGameObjectMap [circleUnit].transform.Find (circleGOname).gameObject;
		Destroy (go);
	}
	void CreateBuildCircle (){
		Unit u = mouseController.SelectedUnit;
		if(u==null){
			return;
		}
		Transform parent = unitGameObjectMap [u].transform;
		GameObject go = Instantiate (unitCirclePrefab);
		go.name = circleGOname;
		go.transform.localScale = new Vector3 (u.BuildRange, u.BuildRange);
		go.transform.SetParent (parent);
		go.transform.localPosition =new Vector3(0,0,-0.5f);
		circleUnit = u;
	}

}
