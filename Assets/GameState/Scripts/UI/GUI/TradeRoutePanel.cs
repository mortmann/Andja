using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class TradeRoutePanel : MonoBehaviour {
	public Text text;
	public City city;
	Ship ship;
	public GameObject fromShip;
	public GameObject toShip;
	public GameObject itemPrefab;

    Item currentlySelectedItem;

    Dictionary<Item,ItemUI> itemToGameObject;

    List<Item> toAddItem;
    List<Item> toRemoveItem;


	public TradeRoute tradeRoute;
	public List<Ship> ships;
	public Dictionary<Unit,string> unitNames;
	Dropdown shipSelectionDropDown;
	MapImage mi;
	public Slider amountSlider;
	void Start(){
		if (itemToGameObject == null)//if thats null its not started yet
			Initialize ();
	}
	public void Initialize(){
		itemToGameObject = new Dictionary<Item, ItemUI> ();
		//intToItem = new Dictionary<int, Item> ();
		shipSelectionDropDown=GetComponentInChildren<Dropdown> ();
		mi = GameObject.FindObjectOfType<MapImage> ();
		tradeRoute = new TradeRoute ();
		amountSlider.onValueChanged.AddListener (OnAmountSliderMoved);
		unitNames = new Dictionary<Unit,string> ();
		ships = new List<Ship> ();
		foreach (Unit item in World.Current.Units) {
			if(item.IsShip==false||item.IsPlayerUnit() == false){
				continue;
			}
			ships.Add ((Ship) item); 
			unitNames.Add ((Ship) item,item.Name); 
			item.RegisterOnDestroyCallback (OnShipDestroy);
			item.RegisterOnChangedCallback (OnShipChanged);
		}
		RefreshDropDownValues ();
		shipSelectionDropDown.onValueChanged.AddListener (OnDropDownChange);
	}
	public void OnDropDownChange(int i){
		Show (ships[i]);
	}

	public void OnShipDestroy(Unit u){
		unitNames.Remove (u);
		shipSelectionDropDown.RefreshShownValue ();
	}
	public void OnShipChanged(Unit u){
		unitNames [u] = u.Name;
		shipSelectionDropDown.RefreshShownValue ();
	}

	public void OnAmountSliderMoved(float f){
		if(itemToGameObject.ContainsKey (currentlySelectedItem) ==false){
			return;
		}
		itemToGameObject [currentlySelectedItem].ChangeItemCount (f);
	}

	public void Show(Ship unit){
		amountSlider.maxValue = unit.inventory.MaxStackSize;
		this.ship = unit;
		ResetItemIcons ();
	}
	private void AddItemPrefabTo(Transform t){
		GameObject g = GameObject.Instantiate (itemPrefab);
		g.transform.SetParent (t);
		g.GetComponentInChildren<Slider> ().maxValue = ship.inventory.MaxStackSize;
		g.GetComponentInChildren<Text> ().text= ship.inventory.MaxStackSize+"t";
		//TODO add listener stuff
		EventTrigger trigger = g.GetComponent<EventTrigger> ();
        EventTrigger.Entry entry = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
		itemToGameObject.Add (i,g.GetComponent<ItemUI> ()); 

		entry.callback.AddListener( ( data ) => { OnItemClick( i ); } );
		trigger.triggers.Add( entry );

	}
	public void GetClickedItemCity(Item i){
		if(currentlySelectedItem == null){
			return;
		}
		ItemUI g = itemToGameObject [currentlySelectedItem];
		g.SetItem (i, ship.inventory.MaxStackSize);

		intToItem.Add (PressedItem,i.Clone ()); 
		if(intToItem.ContainsKey (PressedItem))
			intToItem [PressedItem].count=Mathf.RoundToInt(amountSlider.value);

		g.ChangeItemCount (amountSlider.value);
		//set stuff here orso what ever
		GameObject.FindObjectOfType<UIController> ().CloseRightUI ();
	}
	public void RefreshDropDownValues(){
		shipSelectionDropDown.ClearOptions ();
		shipSelectionDropDown.AddOptions (new List<string>(unitNames.Values));
		shipSelectionDropDown.RefreshShownValue ();
	} 
	public void OnItemClick(int i){
		if(city == null){
			return;
		}
		PressedItem = i;
		GameObject.FindObjectOfType<UIController>().OpenCityInventory (city);
	}
	public Item[] GetToShip(){
		List<Item> items = new List<Item> ();
		for (int i = 0; i <intToItem.Count; i+=2) {
			if(intToItem.ContainsKey (i)==false){
				continue;
			}
			intToItem [i].count = Mathf.RoundToInt (itemToGameObject[i].slider.value);
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
			intToItem [i].count =Mathf.RoundToInt (itemToGameObject[i].slider.value);
			items.Add (intToItem[i]);
		}
		return items.ToArray ();
	}

	public void ShowTradeRoute(){
		int v = shipSelectionDropDown.value;
		if(ships [v].tradeRoute==null){
			ships [v].tradeRoute = new TradeRoute ();		
		} 
		Show (ships[v]);
		tradeRoute = new TradeRoute(ships [v].tradeRoute);

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
			text.text = warehouse.City.Name;
		} else {
			t.GetComponentInChildren<Text> ().text="";
			tradeRoute.RemoveWarehouse (warehouse);
		}
	}
	public void OnTRSAVEButtonPressed(){
		tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());
		ship.tradeRoute = new TradeRoute(tradeRoute);
        ship.CurrentMainMode = UnitMainModes.TradeRoute;
	}
	public void ResetItemIcons(){
		itemToGameObject = new Dictionary<int, ItemUI> ();
		foreach(Transform t in fromShip.transform){
			GameObject.Destroy (t.gameObject);
		}
		foreach(Transform t in toShip.transform){
			GameObject.Destroy (t.gameObject);
		}
		for (int i = 0; i < ship.inventory.NumberOfSpaces; i++) {
			//this order is important
			//DO NOT CHANGE THIS 
			//WITHOUT CHANGING THE RETURNING VALUES FOR
			//GET TO AND FROM SHIP!
			AddItemPrefabTo (toShip.transform); //even 0,2,4 ...
			AddItemPrefabTo (fromShip.transform); //uneven 1,3,5 ...
		}
	}
	public void AddUnit(Unit u){
		if(u is Ship==false){
			return;
		}
		if(ships==null){
			ships = new List<Ship> ();
		}
		ships.Add ((Ship)u); 
	}

	public void NextCity(bool right){
		tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());		
		TradeRoute.Trade t = tradeRoute.GetNextTrade (city,right);
		SetCity (t.city);
	}

	public void SetCity(City c){
		if (city != null && tradeRoute.Contains(c)) {
			tradeRoute.SetCityTrade (city, GetToShip (), GetFromShip ());		
		}
		text.text = c.Name;
		intToItem = new Dictionary<int, Item> ();
		ResetItemIcons ();
		city = c;
		TradeRoute.Trade t = tradeRoute.GetTradeFor (city);
		if(t==null){
			return;
		}
		int place=0;
		foreach(Item i in t.getting){
			itemToGameObject [place].ChangeItemCount (i);
			place = +2;
		}
		place = 1;
		foreach(Item i in t.giving){
			itemToGameObject [place].ChangeItemCount (i);
			place = +2;
		}
        UIController.Instance.OpenCityInventory(c);
    }

    public void DeleteSelectedItem(){
		itemToGameObject [PressedItem].SetItem (null, ship.inventory.MaxStackSize);
		intToItem.Remove (PressedItem);
	}

	void OnDisable(){
        if (ships == null)
            return;
		foreach (Ship item in ships) {
			item.UnregisterOnChangedCallback (OnShipChanged);
			item.UnregisterOnChangedCallback (OnShipDestroy);
		}
	}
}
