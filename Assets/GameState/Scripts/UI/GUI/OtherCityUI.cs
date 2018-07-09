using UnityEngine;
using System.Collections;

public class OtherCityUI : MonoBehaviour {
	public City city { protected set; get;}
	public GameObject ItemsCanvas;
	public GameObject TradeItemPrefab;
	public GameObject ItemCanvas;
	// Use this for initialization
	public void Show (City c) {
		city = c;
		city.RegisterCityDestroy (OnCityDestroy);

		city.inventory.RegisterOnChangedCallback (OnInventoryChange);
		OnInventoryChange(city.inventory);
	}
	public void OnInventoryChange(Inventory inventory){
		foreach (Transform item in ItemsCanvas.transform) {
			Destroy (item.gameObject);
		}
		foreach (int itemID in city.itemIDtoTradeItem.Keys) {
			TradeItem ti = city.itemIDtoTradeItem [itemID];
			GameObject g = Instantiate (TradeItemPrefab);
			g.transform.SetParent (ItemCanvas.transform);
			TradeItemUI tiui = g.GetComponent<TradeItemUI> ();
			if (ti.selling) {
				//SELL show how much it has
				Item temp = city.inventory.GetItemWithIDClone (itemID);
				Item i = ti.SellItemAmount (temp);
				tiui.Show (i, city.inventory.MaxStackSize,ti.selling);
			} else {
				//BUY show how much it wants
				Item i = ti.BuyItemAmount (city.inventory.GetItemWithIDClone (itemID));
				tiui.Show (i, city.inventory.MaxStackSize,ti.selling);
			}
			tiui.UpdatePriceText (ti.price); 
			int id = itemID;
			tiui.AddListener ((data)=>{OnClickItemToTrade(id);}); 
		}
	}
	public void OnClickItemToTrade(int itemID, int amount = 50){
		Unit u = city.myWarehouse.inRangeUnits.Find (x => x.playerNumber == PlayerController.currentPlayerNumber);
		if(u==null && u.isShip==false){
			Debug.Log ("No Ship in Range"); 
			return;
		}			

		city.BuyFromCity (itemID, PlayerController.Instance.CurrPlayer, ((Ship)u), amount);
	}
	public void OnCityDestroy(City c){
		if(city != c){
			return;
		}
		UIController.Instance.HideCityUI (c);
	}
	void OnDisable(){
		if(city!=null){
			city.UnregisterCityDestroy (OnCityDestroy);
		}
	}
}
