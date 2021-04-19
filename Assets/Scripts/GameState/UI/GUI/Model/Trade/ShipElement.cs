﻿using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ShipElement : MonoBehaviour, IComparable<ShipElement> {
        public Text NameText;
        public Toggle ActiveToggle;
        private Action<Ship> onDelete;
        private Action<Ship> onAdd;
        public bool IsChecked => ActiveToggle.isOn;
        public string ShipName => ship.PlayerSetName;
        public Ship ship;

        private void Start() {
            ActiveToggle.onValueChanged.AddListener(OnToggle);
        }

        private void OnToggle(bool check) {
            if (check)
                onAdd?.Invoke(ship);
            else
                onDelete?.Invoke(ship);
        }

        public void SetShip(Ship ship, bool selected, Action<Ship> onAdd, Action<Ship> onDelete) {
            this.ship = ship;
            NameText.text = ship.PlayerSetName;
            this.onDelete += onDelete;
            this.onAdd += onAdd;
        }

        public void SetToggle(bool active) {
            ActiveToggle.isOn = active;
        }

        public int CompareTo(ShipElement y) {
            //When the Checked same order by NAME
            if (IsChecked && y.IsChecked)
                return ShipName.CompareTo(y.ShipName);
            if (IsChecked == false && y.IsChecked == false)
                return ShipName.CompareTo(y.ShipName);
            //Otherwise sort by is on but on be infront!
            return -IsChecked.CompareTo(y.IsChecked);
        }
    }
}