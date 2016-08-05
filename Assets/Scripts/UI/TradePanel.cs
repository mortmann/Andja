using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class TradePanel : MonoBehaviour {
	public Text text;
	public City city;
	Unit unit;
	public GameObject fromShip;
	public GameObject toShip;
	public GameObject itemPrefab;
	int _pressedItem;

	int pressedItem{
		get{return _pressedItem;}
		set{
			_pressedItem = value;
		}
	}
	Dictionary<int,ItemUI> intToGameObject;
	Dictionary<int,Item> intToItem;
	public TradeRoute tradeRoute;
	public List<Unit> units;
	Dropdown shipDP;
	MapImage mi;
	public Slider amountSlider;
	void Start(){
		if (intToGameObject == null)//if thats null its not started yet
			Initialize ();
	}
	public void Initialize(){
		intToGameObject = new Dictionary<int, ItemUI> ();
		intToItem = new Dictionary<int, Item> ();
		shipDP=GetComponentInChildren<Dropdown> ();
		mi = GameObject.FindObjectOfType<MapImage> ();
		tradeRoute = new TradeRoute ();
		amountSlider.onValueChanged.AddListener (OnAmountSliderMoved);
	}
	public void OnAmountSliderMoved(float f){
		if(intToGameObject.ContainsKey (pressedItem)==false){
			return;
		}
		intToGameObject [this.pressedItem].ChangeItemCount (f);
	}
	public void Show(Unit unit){
		amountSlider.maxValue = unit.inventory.maxStackSize;
		this.unit = unit;
		ResetItemIcons ();
	}
	private void AddItemPrefabTo(Transform t){
		GameObject g = GameObject.Instantiate (itemPrefab);
		g.transform.SetParent (t);
		g.GetComponentInChildren<Slider> ().maxValue = unit.inventory.maxStackSize;
		g.GetComponentInChildren<Text> ().text= unit.inventory.maxStackSize+"t";
		//TODO add listener stuff
		EventTrigger trigger = g.GetComponent<EventTrigger> ();
		EventTrigger.Entry entry = new EventTrigger.Entry( );
		entry.eventID = EventTriggerType.PointerClick;
		int i = intToGameObject.Count;
		intToGameObject.Add (i,g.GetComponent<ItemUI> ()); 

		entry.callback.AddListener( ( data ) => { OnItemClick( i ); } );
		trigger.triggers.Add( entry );

	}
	public void GetClickedItemCity(Item i){
		if(pressedItem == -1){
			return;
		}
		ItemUI g = intToGameObject [pressedItem];
		g.SetItem (i, unit.inventory.maxStackSize);
//		pressedItem = -1;
		intToItem.Add (pressedItem,i.Clone ()); 
		if(intToItem.ContainsKey (pressedItem))
			intToItem [pressedItem].count=Mathf.RoundToInt(amountSlider.value);
		g.ChangeItemCount (amountSlider.value);
		//set stuff here orso what ever
		GameObject.FindObjectOfType<UIController> ().CloseRightUI ();
	}

	public void OnItemClick(int i){
		if(city == null){
			return;
		}
		pressedItem = i;
		GameObject.FindObjectOfType<UIController>().OpenCityInventory (city);
	}
	public Item[] GetToShip(){
		List<Item> items = new List<Item> ();
		for (int i = 0; i <intToItem.Count; i+=2) {
			if(intToItem.ContainsKey (i)==false){
				continue;
			}
			intToItem [i].count = Mathf.RoundToInt (intToGameObject[i].slider.value);
			items.Add (intToItem[i]);
		}
		return items.ToArray ();
	}
	public Item[] GetFromShip(){
		List<Item> items = new List<Item> ();
		for (int i = 1; i <intToItem.Count; i+=2) {
			if(intToItem.ContainsKey (i)==false){
				continue;
			}
			intToItem [i].count =Mathf.RoundToInt (intToGameObject[i].slider.value);
			items.Add (intToItem[i]);
		}
		return items.ToArray ();
	}

	public void ShowTradeRoute(){
		int v = shipDP.value;
		if(units [v].tradeRoute==null){
			units [v].tradeRoute = new TradeRoute ();		
		} 
		Show (units[v]);
		tradeRoute = new TradeRoute(units [v].tradeRoute);

		foreach(Warehouse w in mi.warehouseToGO.Keys){
			Toggle t = mi.warehouseToGO [w].GetComponent<Toggle> ();
			if (tradeRoute.Contains (w.City) == false) {
				t.GetComponentsInChildren<Text> () [1].text = "";//+tradeRoute.GetLastNumber();
				t.isOn = false;
			} else {
				t.GetComponentsInChildren<Text> () [1].text = ""+tradeRoute.GetNumberFor(w);
				t.isOn = true;
			}
		}
	}
	public void OnWarehouseClick(City c){
		if(tradeRoute.Contains (c)==false){
			return;
		}
//		if(city!=null){
//			tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());		
//			intToItem = new Dictionary<int, Item> ();
//			ResetItemIcons ();
//		}
//		city = c;	
		SetCity (c);
	}
	public void OnToggleClicked(Warehouse warehouse,Toggle t){
		if(tradeRoute == null){
			Debug.LogError ("NO TRADEROUTE"); 
			return;
		}

		if(t.isOn){
			SetCity (warehouse.City);
			//not that good
			tradeRoute.AddWarehouse (warehouse);
			t.GetComponentInChildren<Text> ().text=""+tradeRoute.GetLastNumber();
			text.text = warehouse.City.name;
		} else {
			t.GetComponentInChildren<Text> ().text="";
			tradeRoute.RemoveWarehouse (warehouse);
		}
	}
	public void OnTRSAVEButtonPressed(){
		tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());
		unit.tradeRoute = new TradeRoute(tradeRoute);
	}
	public void ResetItemIcons(){
		intToGameObject = new Dictionary<int, ItemUI> ();
		foreach(Transform t in fromShip.transform){
			GameObject.Destroy (t.gameObject);
		}
		foreach(Transform t in toShip.transform){
			GameObject.Destroy (t.gameObject);
		}
		for (int i = 0; i < unit.inventory.numberOfSpaces; i++) {
			//this order is important
			//DO NOT CHANGE THIS 
			//WITHOUT CHANGING THE RETURNING VALUES FOR
			//GET TO AND FROM SHIP!
			AddItemPrefabTo (toShip.transform); //even 0,2,4 ...
			AddItemPrefabTo (fromShip.transform); //uneven 1,3,5 ...
		}
	}
	public void addUnit(Unit u){
		if(units==null){
			units = new List<Unit> ();
		}
		units.Add (u); 
	}

	public void NextCity(bool right){
		tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());		
		Trade t = tradeRoute.GetNextTrade (city,right);
		SetCity (t.city);
	}

	public void SetCity(City c){
		if (city != null) {
			tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());		
		}
		text.text = c.name;
		intToItem = new Dictionary<int, Item> ();
		ResetItemIcons ();
		city = c;
		Trade t = tradeRoute.GetTradeFor (city);
		if(t==null){
			return;
		}
		int place=0;
		foreach(Item i in t.getting){
			intToGameObject [place].ChangeItemCount (i);
			place = +2;
		}
		place = 1;
		foreach(Item i in t.giving){
			intToGameObject [place].ChangeItemCount (i);
			place = +2;
		}
	}

	public void DeleteSelectedItem(){
		intToGameObject [pressedItem].SetItem (null, unit.inventory.maxStackSize);
		intToItem.Remove (pressedItem);
	}
}
