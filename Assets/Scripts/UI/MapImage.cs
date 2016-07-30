using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEditor;
public class MapImage : MonoBehaviour {
	public GameObject mapCitySelectPrefab;
	public GameObject mapShipIconPrefab;
	public Image image;
	public GameObject mapParts;
	GameObject cameraRect;
	CameraController cc;
	Texture2D tex;
	float refresh=0;
	//for tradingroute
	TradeRoute tradeRoute;
	public GameObject tradingMenu;
	List<Unit> units;
	Dictionary<Warehouse,GameObject> warehouseToGO;
	Dictionary<Unit,GameObject> unitToGO;

	// Use this for initialization
	void Start () {
		cc = GameObject.FindObjectOfType<CameraController> ();
		warehouseToGO = new Dictionary<Warehouse, GameObject> ();
		unitToGO = new Dictionary<Unit, GameObject> ();
		World w = World.current;
		tex = new Texture2D (w.Width, w.Height);
		Color[] p=tex.GetPixels ();
		int pixel=p.Length-1;
		for (int x = 0; x < w.Width; x++) {
			for (int y = 0; y < w.Height; y++) {
				if (w.GetTileAt (x,y).Type == TileType.Water) {
					p [y*w.Width+x] = Color.blue;			
				} else {
					p [y*w.Width+x] = Color.green;
				}
				pixel--;
			}
		}
		

		tex.SetPixels (p);
		tex.Apply ();
		Sprite s = Sprite.Create (tex, new Rect (0, 0, w.Width, w.Height), new Vector2 (100, 100));

		image.sprite = s;

		cameraRect = new GameObject ();
		cameraRect.transform.SetParent (mapParts.transform);
		cameraRect.AddComponent<Image> ().sprite = Resources.Load<Sprite>("Map/camerashadow");
		cameraRect.name = "CameraShadow";
		Color co = Color.black;
		co.a = 0.5f;
		cameraRect.GetComponent<Image> ().color = co;

		BuildController.Instance.RegisterCityCreated (OnCityCreated);
		w.RegisterUnitCreated (OnUnitCreated);
		units = new List<Unit> ();
		foreach (Unit item in w.units) {
			OnUnitCreated (item);
		}
		ShowTradeRoute ();
	}
	public void Show(){
		//do smth when it gets shown
	}
	public void OnCityCreated(City c){
//		PlayerController pc = PlayerController.Instance;
		RectTransform rt = mapParts.GetComponent<RectTransform> ();
		World w = World.current;

		if(c!=null){
			GameObject g = GameObject.Instantiate (mapCitySelectPrefab);
			g.transform.SetParent (mapParts.transform);
			Vector3 pos = new Vector3 (c.myWarehouse.BuildTile.X, c.myWarehouse.BuildTile.Y, 0);
			pos.Scale (new Vector3(rt.rect.width/w.Width,rt.rect.height/w.Height));
			g.transform.localPosition = pos;
			g.GetComponentInChildren<Text> ().text = c.name;
			g.GetComponent<Toggle > ().onValueChanged.AddListener (( data ) => { OnToggleClicked (c.myWarehouse); });
			c.myWarehouse.RegisterOnDestroyCallback (OnWarehouseDestroy);
			warehouseToGO.Add (c.myWarehouse, g);
		}
	}
	public void OnToggleClicked(Warehouse warehouse){
		if(tradeRoute == null){
			tradeRoute = new TradeRoute();
		}

		Toggle t = warehouseToGO [warehouse].GetComponent<Toggle> ();
		if(t.isOn){
			//not that good
			tradeRoute.AddWarehouse (warehouse);
			t.GetComponentsInChildren<Text> ()[1].text=""+tradeRoute.GetLastNumber();

		} else {
			t.GetComponentsInChildren<Text> ()[1].text="";
			tradeRoute.RemoveWarehouse (warehouse);
		}
	}
	public void OnWarehouseDestroy(Structure str){
		if(str is Warehouse == false){
			Debug.LogError ("MapImage OnWarehouseDestroy-" +str+" is no Warehouse");
			return;
		}
		Warehouse w = (Warehouse)str;
		GameObject.Destroy (warehouseToGO [w]);
		warehouseToGO.Remove(w);
		//TODO UPDATE ALL TRADE_ROUTES
	}
	public void OnUnitCreated(Unit u){
		RectTransform rt = mapParts.GetComponent<RectTransform> ();
		World w = World.current;

		if(u!=null){
			GameObject g = GameObject.Instantiate (mapShipIconPrefab);
			g.transform.SetParent (mapParts.transform);
			Vector3 pos = new Vector3 (u.X, u.Y, 0);
			pos.Scale (new Vector3(rt.rect.width/w.Width,rt.rect.height/w.Height));
			g.transform.localPosition = pos;
			unitToGO.Add (u, g);

			Dropdown d = tradingMenu.GetComponentInChildren<Dropdown> ();
			Dropdown.OptionData op = new Dropdown.OptionData(u.ToString ());// TODO change this to the name of the unit!
			d.options.Add (op); //doesnt take strings directly...
			d.RefreshShownValue (); // it doesnt update on its own! so we have todo it! 
			units.Add (u);
		}
	}
	public void ShowTradeRoute(){
		tradeRoute = units [tradingMenu.GetComponentInChildren<Dropdown> ().value].tradeRoute;
		foreach(Warehouse w in warehouseToGO.Keys){
			Toggle t = warehouseToGO [w].GetComponent<Toggle> ();
			if (tradeRoute.Contains (w) == false) {
				t.GetComponentsInChildren<Text> () [1].text = "";//+tradeRoute.GetLastNumber();
				t.isOn = false;
			} else {
				t.GetComponentsInChildren<Text> () [1].text = ""+tradeRoute.GetNumberFor(w);
				t.isOn = true;
	 		}
		}
	}
	// Update is called once per frame
	void Update () {
		World w = World.current;
		//if something changes reset it 
		RectTransform rt = mapParts.GetComponent<RectTransform> ();
		cameraRect.transform.localPosition = cc.middle * rt.rect.width/w.Width;
		Vector3 vec = cc.upper - cc.lower;
		vec /= Mathf.Clamp(cc.zoomLevel,40,cc.zoomLevel);// I dont get why this is working, but it does
		cameraRect.transform.localScale = 2*((vec));
		refresh -= Time.deltaTime;
		foreach (Unit item in w.units) {
			if(unitToGO.ContainsKey (item)==false){
				Debug.LogError ("unit got not added");
				OnUnitCreated (item);
				continue;
			}
			Vector3 pos = new Vector3 (item.X, item.Y, 0);

			pos.Scale (new Vector3(rt.rect.width/w.Width,rt.rect.height/w.Height));
			unitToGO [item].transform.localPosition = pos;
		}

	}
}
