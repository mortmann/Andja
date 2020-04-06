using UnityEngine;
using System.Collections;

public class OtherCityUI : MonoBehaviour {
    public City city { protected set; get; }
    public GameObject ItemsCanvas;
    public GameObject TradeItemPrefab;
    public GameObject ItemCanvas;
    // Use this for initialization
    public void Show(City c) {
        city = c;
        city.RegisterCityDestroy(OnCityDestroy);

        city.Inventory.RegisterOnChangedCallback(OnInventoryChange);
        OnInventoryChange(city.Inventory);
    }
    public void OnInventoryChange(Inventory inventory) {
        foreach (Transform item in ItemsCanvas.transform) {
            Destroy(item.gameObject);
        }
        foreach (string itemID in city.itemIDtoTradeItem.Keys) {
            TradeItem ti = city.itemIDtoTradeItem[itemID];
            GameObject g = Instantiate(TradeItemPrefab);
            g.transform.SetParent(ItemCanvas.transform);
            TradeItemUI tiui = g.GetComponent<TradeItemUI>();
            if (ti.IsSelling) {
                //SELL show how much it has
                Item temp = city.Inventory.GetItemWithIDClone(itemID);
                Item i = ti.SellItemAmount(temp);
                tiui.Show(i, city.Inventory.MaxStackSize, ti.IsSelling);
            }
            if(ti.IsBuying) {
                //BUY show how much it wants
                Item i = ti.BuyItemAmount(city.Inventory.GetItemWithIDClone(itemID));
                tiui.Show(i, city.Inventory.MaxStackSize, ti.IsBuying);
            }
            tiui.UpdatePriceText(ti.price);
            string id = itemID;
            tiui.AddListener((data) => { OnClickItemToTrade(id); });
        }
    }
    public void OnClickItemToTrade(string itemID, int amount = 50) {
        Unit u = city.warehouse.inRangeUnits.Find(x => x.playerNumber == PlayerController.currentPlayerNumber);
        if (u == null && u.IsShip == false) {
            Debug.Log("No Ship in Range");
            return;
        }

        city.SellingTradeItem(itemID, PlayerController.Instance.CurrPlayer, ((Ship)u), amount);
    }
    public void OnCityDestroy(City c) {
        if (city != c) {
            return;
        }
        UIController.Instance.HideCityUI(c);
    }
    void OnDisable() {
        if (city != null) {
            city.UnregisterCityDestroy(OnCityDestroy);
        }
    }
}
