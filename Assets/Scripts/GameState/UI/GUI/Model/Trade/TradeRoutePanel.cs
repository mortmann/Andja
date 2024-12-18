﻿using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Andja.UI.Model {

    public class TradeRoutePanel : MonoBehaviour {
        public static TradeRoutePanel Instance;
        public const int TradeAmountMaximum = 100;
        public Text text;

        public ICity city;
        public GameObject fromShip;
        public GameObject toShip;
        public GameObject itemPrefab;
        public GameObject shipTradeRoutePrefab;
        public GameObject tradeRouteElementPrefab;
        public MapLineManager tradeRouteLine;

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
        public Slider amountSlider;

        public void Initialize(MapImage mapImage) {
            Instance = this;
            foreach (Transform child in allTradeRoutesList.transform) {
                if (child.name == "NewTradeRoute")
                    continue;
                Destroy(child.gameObject);
            }
            foreach (Transform child in currentShipList.transform) {
                Destroy(child.gameObject);
            }
            tradeRouteToGameObject = new Dictionary<TradeRoute, GameObject>();
            shipToGOElement = new Dictionary<Ship, ShipElement>();
            itemToGameObject = new Dictionary<Item, ItemUI>();
            amountSlider.maxValue = TradeAmountMaximum;
            amountSlider.value = 50;
            amountSlider.onValueChanged.AddListener(OnAmountSliderMoved);
            World.Current.RegisterUnitCreated(OnShipCreate);
            foreach (Unit item in World.Current.Units) {
                if (item.IsShip == false || item.IsOwnedByCurrentPlayer() == false) {
                    continue;
                }
                OnShipCreate(item);
            }
            foreach (TradeRoute tr in PlayerController.CurrentPlayer.TradeRoutes) {
                AddTradeRouteToList(tr);
            }
            if (PlayerController.CurrentPlayer.TradeRoutes.Count == 0)
                CreateNewTradeRoute();
            else
                ShowTradeRoute(PlayerController.CurrentPlayer.TradeRoutes[0]);
        }

        public void RemoveTradeRoute(TradeRoute tradeRoute) {
            PlayerController.CurrentPlayer.RemoveTradeRoute(tradeRoute);
            Destroy(tradeRouteToGameObject[tradeRoute]);
            tradeRouteToGameObject.Remove(tradeRoute);
        }

        public void OnShipDestroy(Unit unit, IWarfare warfare) {
            if (unit.IsOwnedByCurrentPlayer() == false || unit is Ship == false)
                return;
            Ship ship = (Ship)unit;
            Destroy(shipToGOElement[ship].gameObject);
            shipToGOElement.Remove(ship);
        }

        public void OnShipCreate(Unit unit) {
            if (unit is Ship == false || unit.IsOwnedByCurrentPlayer() == false)
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
            foreach (Transform t in tradeRouteLine.transform)
                Destroy(t.gameObject);
            tradeRouteLine.SetTradeRoute(tr);
            UpdateCitySelects();
            //foreach (Ship ship in tradeRoute.Ships) {
            //    AddShipToList(ship);
            //}
            SetCity(tr.GetTrade(0)?.city);
        }

        private void UpdateCitySelects() {
            foreach (MapCitySelect ms in FindObjectsOfType<MapCitySelect>()) {
                if (tradeRoute.Contains(ms.City) == false) {
                    ms.Unselect();
                }
                else {
                    ms.SelectAs(tradeRoute.GetNumberFor(ms.City));
                }
            }
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

        public void OnWarehouseClick(ICity c) {
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
            UIController.Instance.OpenOwnedCityInventory(city, AddLoadItem);
        }

        public void OnUnloadItemAdd() {
            UIController.Instance.OpenOwnedCityInventory(city, AddUnloadItem);
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

        public int OnCityToggle(ICity city, bool selected) {
            if (tradeRoute == null) {
                Debug.LogError("NO TRADEROUTE");
                return -1;
            }
            if (selected) {
                SetCity(city);
                //not that good
                tradeRoute.AddCity(city);
                return tradeRoute.GetLastNumber();
            }
            else {
                tradeRoute.RemoveCity(city);
                UpdateCitySelects();
                return -1;
            }
        }

        public void ResetItemIcons() {
            foreach (ItemUI i in itemToGameObject.Values) {
                GameObject.Destroy(i.transform.gameObject);
            }
            itemToGameObject.Clear();
        }

        public void NextCity(bool right) {
            if (tradeRoute.TradeCount == 0)
                return;
            ResetItemIcons();
            tradeRouteCityState += right ? 1 : -1;
            tradeRouteCityState = (tradeRouteCityState + tradeRoute.TradeCount) % tradeRoute.TradeCount;
            TradeRoute.Trade t = tradeRoute.GetTrade(tradeRouteCityState);
            SetCity(t.city);
        }
        public void SetCity(ICity c) {
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


        private void OnDestroy() {
            Instance = null;
        }
    }
}