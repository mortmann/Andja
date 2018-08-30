using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnitSpriteController : MonoBehaviour {
    private Dictionary<string, Sprite> unitSprites;
    public Dictionary<Unit, GameObject> unitGameObjectMap;
	public GameObject unitPathPrefab;
	public GameObject unitCirclePrefab;
    public Dictionary<Crate, GameObject> crateGameObjectMap;
    public Dictionary<Projectile, GameObject> projectileGameObjectMap;

    private Unit circleUnit;
	private const string circleGOname = "buildrange_circle_gameobject";
	MouseController mouseController;
    World World {
        get { return World.Current; }
    }
    // Use this for initialization
    void Start () {
        unitGameObjectMap = new Dictionary<Unit, GameObject>();
        crateGameObjectMap = new Dictionary<Crate, GameObject>();
        projectileGameObjectMap = new Dictionary<Projectile, GameObject>();
        LoadSprites();
        World.RegisterUnitCreated(OnUnitCreated);
        World.RegisterCrateSpawned(OnCrateSpawned);
        World.RegisterCrateDespawned(OnCrateDespawned);

        foreach (var item in World.Units) {
			OnUnitCreated (item);
		}        
        foreach(Crate c in World.Crates) {
            OnCrateSpawned(c);
        }
		mouseController = MouseController.Instance;
		BuildController.Instance.RegisterBuildStateChange (OnBuildStateChange);
    }


	void Update(){
	}
	public void OnUnitCreated(Unit u) {
        // Create a visual GameObject linked to this data.
        // Create a 2d box collider around the unit


        // This creates a new GameObject and adds it to our scene.
		GameObject unit_go = new GameObject();
		GameObject line_go = Instantiate (unitPathPrefab);
		line_go.transform.SetParent (unit_go.transform);
        // Add our tile/GO pair to the dictionary.
        unitGameObjectMap.Add(u, unit_go);
		SpriteRenderer sr = unit_go.AddComponent<SpriteRenderer>();
		sr.sortingLayerName = "Units";
        sr.sprite = unitSprites[u.Data.spriteBaseName];

        unit_go.transform.SetParent(this.transform, true);
		unit_go.AddComponent<ITargetableHoldingScript> ().Holding=u;
		Rigidbody2D r2d = unit_go.AddComponent<Rigidbody2D> (); 
		r2d.gravityScale = 0;       
		BoxCollider2D col = unit_go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, 
                                sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
		
		//u.width = sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit;
		//u.height = sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit;
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        u.RegisterOnChangedCallback(OnUnitChanged);
		u.RegisterOnDestroyCallback (OnUnitDestroy);
        u.RegisterOnbCreateProjectileCallback(OnProjectileCreated);
        OnUnitChanged(u);
    }

    private void OnProjectileCreated(Projectile projectile) {
        GameObject pro_go = new GameObject {
            name = "Projectile"
        };
        SpriteRenderer sr = pro_go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Units";
        sr.sprite = unitSprites["cannonball_1"];
        projectile.RegisterOnDestroyCallback(OnProjectileDestroy);
        BoxCollider2D col = pro_go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit,
                                sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
        pro_go.AddComponent<ProjectileHoldingScript>().myProjectile = projectile;
    }

    private void OnProjectileDestroy(Projectile pro) {
        projectileGameObjectMap.Remove(pro);
        pro.UnregisterOnDestroyCallback(OnProjectileDestroy);
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
        c.UnregisterOnChangedCallback(OnUnitChanged);
	}

    void OnCrateSpawned(Crate c) {
        //TODO: create a prefab?
        GameObject go = new GameObject();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprites["Crate"];
        go.AddComponent<CrateHoldingScript>().thisCrate = c;
        go.transform.SetParent(this.transform);
        go.name = "Crate";
        go.layer = 10;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        go.AddComponent<Rigidbody2D>().gravityScale = 0; //TODO: think about if this is good so!
        col.size = new Vector2(sr.sprite.textureRect.size.x / sr.sprite.pixelsPerUnit, sr.sprite.textureRect.size.y / sr.sprite.pixelsPerUnit);
        go.transform.position = c.position;
        crateGameObjectMap.Add(c, go);
    }
    void OnCrateDespawned(Crate c) {
        Destroy(crateGameObjectMap[c]);
        crateGameObjectMap.Remove(c);
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
