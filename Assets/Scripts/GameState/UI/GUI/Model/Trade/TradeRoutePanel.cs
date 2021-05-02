using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class TradeRoutePanel : MonoBehaviour {
        public const int TradeAmountMaximum = 100;
        public Text text;
        public City city;
        public GameObject fromShip;
        public GameObject toShip;
        public GameObject itemPrefab;
        public GameObject shipTradeRoutePrefab;
        public GameObject tradeRouteElementPrefab;

        public Transform loadItemParent;
        public Transform unloadItemParent;
        public Transform currentShipList;
        public Transform allTradeRoutesList;

        private Item _currentlySelectedItem;

        private Item CurrentlySelectedItem {
            get { return _currentlySelectedItem; }
            set {
                if (_currentlySelectedItem != null && itemToGameObject.ContainsKey(_currentlySelectedItem))
                    itemToGameObject[_currentlySelectedItem].SetSelected(false);
                if (value != null)
                    itemToGameObject[value].SetSelected(true);
                _currentlySelectedItem = value;
            }
        }

        private Dictionary<Item, ItemUI> itemToGameObject;
        private Dictionary<TradeRoute, GameObject> tradeRouteToGameObject;
        private Dictionary<Ship, ShipElement> shipToGOElement;
        public int tradeRouteCityState = 0;
        public TradeRoute tradeRoute;
        private MapImage mi;
        public Slider amountSlider;

        private void Start() {
            if (itemToGameObject == null)//if thats null its not started yet
                Initialize();
        }

        public void Initialize() {
            foreach (Transform child in allTradeRoutesList.transform) {
                Destroy(child.gameObject);
            }
            foreach (Transform child in currentShipList.transform) {
                Destroy(child.gameObject);
            }
            tradeRouteToGameObject = new Dictionary<TradeRoute, GameObject>();
            shipToGOElement = new Dictionary<Ship, ShipElement>();
            itemToGameObject = new Dictionary<Item, ItemUI>();
            //intToItem = new Dictionary<int, Item> ();
            mi = GameObject.FindObjectOfType<MapImage>();
            amountSlider.maxValue = TradeAmountMaximum;
            amountSlider.value = 50;
            amountSlider.onValueChanged.AddListener(OnAmountSliderMoved);
            World.Current.RegisterUnitCreated(OnShipCreate);
            foreach (Unit item in World.Current.Units) {
                if (item.IsShip == false || item.IsPlayer() == false) {
                    continue;
                }
                OnShipCreate(item);
            }
            foreach (TradeRoute tr in PlayerController.CurrentPlayer.TradeRoutes) {
                AddTradeRouteToList(tr);
            }
            if (tradeRoute == null)
                CreateNewTradeRoute();
        }

        public void RemoveTradeRoute(TradeRoute tradeRoute) {
            PlayerController.CurrentPlayer.RemoveTradeRoute(tradeRoute);
            Destroy(tradeRouteToGameObject[tradeRoute]);
            tradeRouteToGameObject.Remove(tradeRoute);
        }

        public void OnShipDestroy(Unit unit, IWarfare warfare) {
            if (unit.IsPlayer() == false || unit is Ship == false)
                return;
            Ship ship = (Ship)unit;
            Destroy(shipToGOElement[ship].gameObject);
            shipToGOElement.Remove(ship);
        }

        public void OnShipCreate(Unit unit) {
            if (unit is Ship == false)
                return;
            Ship ship = (Ship)unit;
            unit.RegisterOnDestroyCallback(OnShipDestroy);
            AddShipToList(ship);
        }

        public void RemoveShipFromTradeRoute(Ship ship) {
            ship.StopTradeRoute();
            UpdateShipListOrder();
        }

        public void OnAmountSliderMoved(float f) {
            if (CurrentlySelectedItem == null || itemToGameObject.ContainsKey(CurrentlySelectedItem) == false) {
                return;
            }
            CurrentlySelectedItem.count = (int)f;
            itemToGameObject[CurrentlySelectedItem].ChangeItemCount(f);
            //tradeRoute.ChangeItemAmount(city, CurrentlySelectedItem);
        }

        public void OnItemClick(Item i, PointerEventData.InputButton button) {
            switch (button) {
                //SELECT ITEM
                case PointerEventData.InputButton.Left:
                    CurrentlySelectedItem = i;
                    break;
                //REMOVE ITEM
                case PointerEventData.InputButton.Right:
                    OnItemRemove(i);
                    break;
                //NOTHING
                case PointerEventData.InputButton.Middle:
                    break;
            }
        }

        public void CreateNewTradeRoute() {
            tradeRoute = new TradeRoute();
            PlayerController.CurrentPlayer.AddTradeRoute(tradeRoute);
            AddTradeRouteToList(tradeRoute);
            ShowTradeRoute(tradeRoute);
        }

        private void AddTradeRouteToList(TradeRoute tradeRoute) {
            GameObject go = Instantiate(tradeRouteElementPrefab);
            go.GetComponentInChildren<TradeRouteElement>().SetTradeRoute(tradeRoute, ShowTradeRoute, RemoveTradeRoute);
            go.transform.SetParent(allTradeRoutesList, false);
            tradeRouteToGameObject.Add(tradeRoute, go);
        }

        public void ShowTradeRoute(TradeRoute tr) {
            tradeRoute = tr;
            foreach (WarehouseStructure w in mi.warehouseToGO.Keys) {
                GameObject go = mi.warehouseToGO[w];
                if (tradeRoute.Contains(w.City) == false) {
                    go.GetComponent<MapCitySelect>().Unselect();
                }
                else {
                    go.GetComponent<MapCitySelect>().SelectAs(tradeRoute.GetNumberFor(w));
                }
            }
            foreach (Ship ship in tradeRoute.shipToNextStop.Keys) {
                AddShipToList(ship);
            }
            SetCity(tr.GetTrade(0)?.city);
        }

        private void AddShipToList(Ship ship) {
            GameObject shipGO = Instantiate(shipTradeRoutePrefab);
            ShipElement se = shipGO.GetComponentInChildren<ShipElement>();
            se.SetShip(ship, true, AddShipToTradeRoute, RemoveShipFromTradeRoute);
            shipGO.transform.SetParent(currentShipList, false);
            shipToGOElement.Add(ship, se);
            UpdateShipListOrder();
        }

        private void UpdateShipListOrder() {
            List<ShipElement> elements = new List<ShipElement>(shipToGOElement.Values);
            elements.Sort();
            for (int i = 0; i < elements.Count; i++) {
                elements[i].transform.SetSiblingIndex(i);
            }
        }

        private void AddShipToTradeRoute(Ship ship) {
            tradeRoute.AddShip(ship);
            UpdateShipListOrder();
        }

        public void OnWarehouseClick(City c) {
            if (tradeRoute.Contains(c) == false) {
                return;
            }
            SetCity(c);
        }

        public void OnShipAdd(Ship ship) {
            ship.SetTradeRoute(tradeRoute);
            AddShipToList(ship);
        }

        public void OnShipRemove(Ship ship) {
            ship.StopTradeRoute();
            Destroy(shipToGOElement[ship]);
            shipToGOElement.Remove(ship);
        }

        public void OnLoadItemAdd() {
            UIController.Instance.OpenCityInventory(city, AddLoadItem);
        }

        public void OnUnloadItemAdd() {
            UIController.Instance.OpenCityInventory(city, AddUnloadItem);
        }

        public void AddLoadItem(Item item) {
            AddItem(item, TradeTyp.Load);
        }

        public void AddUnloadItem(Item item) {
            AddItem(item, TradeTyp.Unload);
        }

        public void AddItem(Item item, TradeTyp typ) {
            GameObject gameObject = Instantiate(itemPrefab);
            ItemUI ui = gameObject.GetComponent<ItemUI>();
            ui.SetItem(item, TradeAmountMaximum);
            ui.AddClickListener((PointerEventData) => {
                OnItemClick(item, ((PointerEventData)PointerEventData).button);
            }
            );
            ui.ChangeItemCount(amountSlider.value);
            item.count = (int)amountSlider.value;
            switch (typ) {
                case TradeTyp.Load:
                    gameObject.transform.SetParent(loadItemParent, false);
                    gameObject.transform.SetSiblingIndex(loadItemParent.childCount - 2);
                    break;

                case TradeTyp.Unload:
                    gameObject.transform.SetParent(unloadItemParent, false);
                    gameObject.transform.SetSiblingIndex(unloadItemParent.childCount - 2);
                    break;
            }
            tradeRoute.AddItemToTrade(city, item, typ);
            itemToGameObject.Add(item, ui);
            CurrentlySelectedItem = item;
            GameObject.FindObjectOfType<UIController>().CloseRightUI();
        }

        public void OnItemRemove(Item item) {
            Destroy(itemToGameObject[item].gameObject);
            tradeRoute.RemoveItemFromTrade(city, item);
            itemToGameObject.Remove(item);
        }

        public void OnWarehouseToggleClicked(WarehouseStructure warehouse, Toggle t) {
            if (tradeRoute == null) {
                Debug.LogError("NO TRADEROUTE");
                return;
            }
            if (t.isOn) {
                SetCity(warehouse.City);
                //not that good
                tradeRoute.AddWarehouse(warehouse);
                t.GetComponentInChildren<Text>().text = "" + tradeRoute.GetLastNumber();
                text.text = warehouse.City.Name;
            }
            else {
                t.GetComponentInChildren<Text>().text = "";
                tradeRoute.RemoveWarehouse(warehouse);
            }
        }

        public void ResetItemIcons() {
            foreach (ItemUI i in itemToGameObject.Values) {
                GameObject.Destroy(i.transform.gameObject);
            }
            itemToGameObject.Clear();
        }

        public void NextCity(bool right) {
            if (tradeRoute.TradeStopNumber == 0)
                return;
            ResetItemIcons();
            tradeRouteCityState += right ? 1 : -1;
            tradeRouteCityState %= tradeRoute.TradeStopNumber;
            if (tradeRouteCityState < 0)
                tradeRouteCityState = tradeRoute.TradeStopNumber - 1;
            TradeRoute.Trade t = tradeRoute.GetTrade(tradeRouteCityState);

            SetCity(t.city);
        }

        public void SetCity(City c) {
            if(c != null) {
                text.text = c.Name;
                ResetItemIcons();
                city = c;
                TradeRoute.Trade t = tradeRoute.GetTradeFor(city);
                if (t == null) {
                    return;
                }
                foreach (Item i in t.load) {
                    AddItem(i, TradeTyp.Load);
                }
                foreach (Item i in t.unload) {
                    AddItem(i, TradeTyp.Unload);
                }
            }
            else {
                text.text = "";
                ResetItemIcons();
                city = null;
                UIController.Instance.CloseRightUI();
            }
        }

        private void OnDisable() {
        }
    }
}