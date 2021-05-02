using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Andja.UI {

    public class ShipElement : MonoBehaviour, IComparable<ShipElement>, IPointerClickHandler {
        public static ShipElement Selected;
        public Text NameText;
        public GameObject Outline;
        public Toggle ActiveToggle;
        private Action<Ship> onDelete;
        private Action<Ship> onAdd;
        public bool IsChecked => ActiveToggle.isOn;
        public string ShipName => ship.PlayerSetName;
        public Ship ship;

        private void Start() {
            ActiveToggle.onValueChanged.AddListener(OnToggle);
            Unselect();
        }

        private void OnToggle(bool check) {
            if (check)
                onAdd?.Invoke(ship);
            else
                onDelete?.Invoke(ship);
        }
        public void Select() {
            if (Selected != null)
                Selected.Unselect();
            Outline.SetActive(true);
        }
        public void Unselect() {
            Outline.SetActive(false);
        }
        public void SetShip(Ship ship, bool selected, Action<Ship> onAdd, Action<Ship> onDelete) {
            this.ship = ship;
            NameText.text = ship.PlayerSetName ?? ship.Name;
            this.onDelete += onDelete;
            this.onAdd += onAdd;
            if (selected)
                Select();
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

        public void OnPointerClick(PointerEventData eventData) {
            Select();
        }
    }
}