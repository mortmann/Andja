﻿using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class OffWorldPanelUI : MonoBehaviour {
        public Button delete;
        public Button send;
        public GameObject onShip;
        public GameObject toBuy;
        public GameObject itemPrefab;
        public Dropdown shipDP;

        private int _pressedItem;

        private int PressedItem {
            get { return _pressedItem; }
            set {
                _pressedItem = value;
            }
        }

        private Dictionary<int, ItemUI> intToGameObject;
        private Dictionary<int, Item> intToItem;
        public List<Ship> ships;
        public Slider amountSlider;
        private Ship ship;
        private Dictionary<Unit, string> unitNames;

        private void Start() {
            intToGameObject = new Dictionary<int, ItemUI>();
            intToItem = new Dictionary<int, Item>();
            amountSlider.onValueChanged.AddListener(OnAmountSliderMoved);
            delete.onClick.AddListener(OnDeleteClick);
            send.onClick.AddListener(OnSendClick);
            unitNames = new Dictionary<Unit, string>();
            ships = new List<Ship>();
            foreach (Unit item in World.Current.Units) {
                if (item.IsShip == false || item.PlayerNumber != PlayerController.currentPlayerNumber) {
                    continue;
                }
                ships.Add((Ship)item);
                unitNames.Add((Ship)item, item.Name);
                item.RegisterOnDestroyCallback(OnShipDestroy);
                item.RegisterOnChangedCallback(OnShipChanged);
            }
            RefreshDropDownValues();
            shipDP.onValueChanged.AddListener(OnDropDownChange);
            PressedItem = 0;
            if (ships.Count > 0) {
                Show(ships[0]);
            }
            ResetItemIcons();
        }

        public void OnDropDownChange(int i) {
            Debug.Log(i);
            Show(ships[i]);
        }

        public void OnShipDestroy(Unit u, IWarfare warfare) {
            unitNames.Remove(u);
            shipDP.RefreshShownValue();
        }

        public void OnShipChanged(Unit u) {
            unitNames[u] = u.Name;
            shipDP.RefreshShownValue();
        }

        public void RefreshDropDownValues() {
            shipDP.ClearOptions();
            shipDP.AddOptions(new List<string>(unitNames.Values));
            shipDP.RefreshShownValue();
        }

        public void OnDeleteClick() {
            intToGameObject[PressedItem].SetItem(null, ship.Inventory.MaxStackSize);
            intToItem.Remove(PressedItem);
        }

        public void OnSendClick() {
            List<Item> list = new List<Item>(intToItem.Values);
            foreach (var item in list) {
                Debug.Log(item.ToString());
            }
            ship.SendToOffworldMarket(list.ToArray());
            unitNames.Remove(ship);
            ship = null;
            RefreshDropDownValues();
        }

        public void OnAmountSliderMoved(float f) {
            if (intToGameObject.ContainsKey(PressedItem) == false) {
                return;
            }
            intToGameObject[this.PressedItem].ChangeItemCount(f);
            intToItem[PressedItem].count = (int)f;
        }

        public void Show(Ship unit) {
            amountSlider.maxValue = unit.Inventory.MaxStackSize;
            this.ship = unit;
            ResetItemIcons();
        }

        private void AddItemPrefabTo(Transform t) {
            GameObject g = GameObject.Instantiate(itemPrefab);
            g.transform.SetParent(t, false);
            g.GetComponentInChildren<Slider>().maxValue = ship.Inventory.MaxStackSize;
            g.GetComponentInChildren<Text>().text = ship.Inventory.MaxStackSize + "t";
            //TODO add listener stuff
            int i = intToGameObject.Count;
            intToGameObject.Add(i, g.GetComponent<ItemUI>());
            g.GetComponent<ItemUI>().AddClickListener((s) => { OnItemClick(i); });
        }

        public void OnItemClick(int i) {
            PressedItem = i;
        }

        public void OnOffWorldItemClick(Item i) {
            intToGameObject[PressedItem].RefreshItem(i);
            intToItem.Add(PressedItem, i);
        }

        public void ResetItemIcons() {
            intToGameObject = new Dictionary<int, ItemUI>();
            foreach (Transform t in onShip.transform) {
                GameObject.Destroy(t.gameObject);
            }
            foreach (Transform t in toBuy.transform) {
                GameObject.Destroy(t.gameObject);
            }
            if (ship == null) {
                return;
            }
            for (int i = 0; i < ship.Inventory.NumberOfSpaces; i++) {
                AddItemPrefabTo(toBuy.transform);
                Item item = ship.Inventory.GetItemInSpace(i);
                GameObject g = GameObject.Instantiate(itemPrefab);
                g.GetComponent<ItemUI>().SetItem(item, ship.Inventory.MaxStackSize);
                g.transform.SetParent(onShip.transform, false);
            }
        }

        private void OnDisable() {
            foreach (Ship item in ships) {
                item.UnregisterOnChangedCallback(OnShipChanged);
                item.UnregisterOnDestroyCallback(OnShipDestroy);
            }
        }
    }
}