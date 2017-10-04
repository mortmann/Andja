using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class TradePanel : MonoBehaviour {

	public Slider amountSlider;
	public Slider priceSlider;
	public GameObject TradeCanvas;
	public GameObject ItemPrefab;
	Dictionary<int,TradeItemUI> intToTradeItemUI;
	Dictionary<int,Item> intToItem;

	int pressedItem;
	City city;
	// Use this for initialization
	public void Show (City c) {
		city = c;
		amountSlider.maxValue = city.inventory.maxStackSize;
		amountSlider.onValueChanged.AddListener (OnAmountSliderChange);
		priceSlider.onValueChanged.AddListener (OnPriceSliderChange);
		intToItem = new Dictionary<int, Item> (); 
		foreach(Transform t in TradeCanvas.transform){
			GameObject.Destroy (t.gameObject);
		}
		intToTradeItemUI = new Dictionary<int, TradeItemUI> ();
		List<int> items = new List<int> (c.itemIDtoTradeItem.Keys);
		for (int i = 0; i < 3; i++) {
		 	GameObject g = GameObject.Instantiate (ItemPrefab);
			g.transform.SetParent (TradeCanvas.transform);
			if(c.itemIDtoTradeItem.Count<=i){
				g.GetComponent<TradeItemUI> ().Show (null, c.inventory.maxStackSize, OnSellBuyClick);
			} else {
				Item item = c.inventory.GetItemWithIDClone (items [i]);
				intToItem.Add (i,item); 
				g.GetComponent<TradeItemUI> ().Show (item, c.inventory.maxStackSize, OnSellBuyClick);
				g.GetComponent<TradeItemUI> ().ChangeItemCount (c.itemIDtoTradeItem [items [i]].count);
			}
			int temp = i;
			g.GetComponent<TradeItemUI> ().AddListener ((data)=>{OnItemClick(temp);}); 
			intToTradeItemUI.Add (i,g.GetComponent<TradeItemUI> ()); 
		}


	}
	public void OnItemSelected(Item item){
		if(city.itemIDtoTradeItem.ContainsKey (item.ID)){
			Debug.Log ("already in it"); 
			return;
		}
		item.count = Mathf.RoundToInt (amountSlider.value);
		if(intToTradeItemUI[pressedItem].item!=null){
			RemoveCurrentTradeItem (); 
		}
		intToTradeItemUI [pressedItem].SetItem (item,city.inventory.maxStackSize);
		intToItem.Add (pressedItem,item); 
		TradeItem ti = new TradeItem (item.ID, ((int)amountSlider.value), 
						((int)priceSlider.value), intToTradeItemUI [pressedItem].Sell);
		city.itemIDtoTradeItem.Add (item.ID,ti); 
		amountSlider.value = city.inventory.maxStackSize / 2;
		OnPriceSliderChange (50);
	}
	public void OnSellBuyClick(Item item,bool sell){
		if(item==null){
			return;
		}
		if(city.itemIDtoTradeItem.ContainsKey (item.ID)==false){
			return;
		}
		city.itemIDtoTradeItem [item.ID].selling = sell;
	}
	public void OnItemClick(int press){
		pressedItem = press;
	}
	public void OnAmountSliderChange(float f){
		intToTradeItemUI [pressedItem].ChangeItemCount (Mathf.RoundToInt(f));
		city.ChangeTradeItemAmount (intToTradeItemUI [pressedItem].item);
	}
	public void OnPriceSliderChange(float f){
		if (city.itemIDtoTradeItem.ContainsKey (intToItem [pressedItem].ID) == false) {
			Debug.Log ("OnPriceChange - item not found in tradeitems"); 
			return;
		}
		//GAME SIDE
		city.ChangeTradeItemPrice(intToItem [pressedItem].ID,Mathf.RoundToInt (f));

		//UI SIDE
		priceSlider.GetComponentInChildren<Text> ().text = "Price: "+f;
		intToTradeItemUI [pressedItem].UpdatePriceText (Mathf.RoundToInt (f));
	}
	public void OnDeleteClick(){
		RemoveCurrentTradeItem (); 		
	}
	private void RemoveCurrentTradeItem(){
		city.RemoveTradeItem (intToTradeItemUI [pressedItem].item);
		intToTradeItemUI [pressedItem].RefreshItem (null);
		intToItem.Remove (pressedItem); 
	}
}
