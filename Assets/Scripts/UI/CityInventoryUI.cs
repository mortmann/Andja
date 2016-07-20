using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CityInventoryUI : MonoBehaviour {
	public GameObject cityname;
	public GameObject contentCanvas;
	public GameObject itemPrefab;
	Dictionary<int, GameObject> itemToGO;
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

		itemToGO = new Dictionary<int, GameObject> ();
		foreach (Item item in city.myInv.items.Values) {
			GameObject go_i = GameObject.Instantiate (itemPrefab);
			go_i.name = item.name + " Item";
			itemToGO.Add (item.ID,go_i);
			Slider s = go_i.GetComponentInChildren<Slider> ();
			s.maxValue = city.myInv.maxStackSize;
			s.value = item.count;
			EventTrigger trigger = go_i.GetComponent<EventTrigger> ();
			EventTrigger.Entry entry = new EventTrigger.Entry( );
			entry.eventID = EventTriggerType.PointerClick;
			Item i = item.Clone ();
			entry.callback.AddListener( ( data ) => { OnItemClick( i ); } );
			trigger.triggers.Add( entry );
			AdjustSliderColor (s);
			// set image here
			Text tons = go_i.GetComponentInChildren<Text> ();
			tons.text = item.count +"t";
			go_i.transform.SetParent (contentCanvas.transform);
		}
	}

	void OnItemClick(Item item){
		city.tradeWithShip (city.myInv.getItem (item));
	}

	void AdjustSliderColor(Slider s){
		if (s.value / s.maxValue < 0.2f) {
			s.GetComponentInChildren<Image> ().color = Color.red;
		} else {
			s.GetComponentInChildren<Image> ().color = Color.green;
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
			itemToGO [i].GetComponentInChildren<Text> ().text = city.myInv.items[i].count + "t wat";
			itemToGO [i].GetComponentInChildren<Slider> ().value = city.myInv.items[i].count;
			AdjustSliderColor (itemToGO[i].GetComponentInChildren<Slider> ());
		}
	}
	// Update is called once per frame
//	void Update () {
//		if(city.myInv != null){
//			foreach (int id in itemToGO.Keys) {
//				GameObject go = itemToGO [id];
//				Slider s = go.GetComponentInChildren<Slider> ();
//				s.value = city.myInv.items[id].count;
//				AdjustSliderColor (s);
//				Text t = go.GetComponentInChildren<Text> ();
//				t.text = city.myInv.items[id].count + "t";
//			}
//		}
//	}
}
