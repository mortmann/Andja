using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CityInventoryUI : MonoBehaviour {
	public GameObject cityname;
	public GameObject contentCanvas;
	public GameObject itemPrefab;
	Dictionary<int, ItemUI> itemToGO;
	public bool trade;
	public City city;


	public void ShowInventory(City city, bool trade){
		if(city == null && this.city.name == city.name){
			
			return;
		}
		cityname.GetComponent<Text> ().text = city.name;
		this.city = city;
		this.trade = trade;
		city.myInv.RegisterOnChangedCallback (OnInventoryChange);

		var children = new List<GameObject>();
		foreach (Transform child in contentCanvas.transform) {
			children.Add (child.gameObject);
		}
		children.ForEach(child => Destroy(child));

		itemToGO = new Dictionary<int, ItemUI> ();
		foreach (Item item in city.myInv.items.Values) {
			GameObject go_i = GameObject.Instantiate (itemPrefab);
			go_i.name = item.name + " Item";
			ItemUI iui = go_i.GetComponent<ItemUI> ();
			itemToGO.Add (item.ID,iui);
			iui.SetItem (item,city.myInv.maxStackSize,true);
			// does this need to be here?
			// or can it be move to itemui?
			// changes in th future maybe
			Item i = item.Clone ();
			iui.AddListener (( data) => {
				OnItemClick (i);
			});

			go_i.transform.SetParent (contentCanvas.transform);
		}
	}

	void OnItemClick(Item item){		
		Debug.Log ("clicked " + item.ToString()); 
		
		if (trade) {
			//trade to ship
			city.tradeWithShip (city.myInv.getItemInInventory (item));
		} else {
			//select item for trademenu
			TradePanel tp = GameObject.FindObjectOfType<TradePanel> ();
			if(tp!=null)
				tp.GetClickedItemCity(item);
		}
	}


//	private void addItemGameObject(Item item){
//		if(item == null){
//			return;
//		}
//		GameObject go = GameObject.Instantiate (itemPrefab);
//		Slider s = go.GetComponentInChildren<Slider> ();
//		Text t = go.GetComponentInChildren<Text> ();
//		if (item.ID != -1) {
//			s.maxValue = city.myInv.maxStackSize;
//			s.value = item.count;
//			t.text = item.count + "t";
//			itemToGO.Add (item,go);
//		} else {
//			s.maxValue = city.myInv.maxStackSize;
//			s.value = 0;
//			t.text = 0 + "t";
//		}
//		AdjustSliderColor (s);
//		go.transform.SetParent (contentCanvas.transform);
//
//	}
	public void OnInventoryChange(Inventory changedInv){
		foreach(int i in changedInv.items.Keys){
			itemToGO [i].ChangeItemCount (city.myInv.items [i].count);
			itemToGO [i].ChangeMaxValue (city.myInv.maxStackSize);
		}
	}
}
