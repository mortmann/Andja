using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
namespace Andja.UI.Model {

    public class MapCitySelect : MonoBehaviour, IPointerClickHandler {
        public Text CityName;
        public Text Number;
        public Toggle toggle;
        public ICity City;

        public void Setup(ICity city) {
            toggle.onValueChanged.AddListener(ToggleCity);
            this.City = city;
            CityName.text = city.Name;
            if (city.Warehouse != null)
                OnWarehouseBuild(city.Warehouse);
            else {
                SetPosition(city.Tiles.First());
                OnWarehouseDestroy(null, null);
            }
            city.RegisterCityDestroy(OnCityDestroy);
        }

        private void OnCityDestroy(ICity city) {
            Destroy(this.gameObject);
        }

        public void OnWarehouseClick() {
            TradeRoutePanel.Instance.OnWarehouseClick(City);
        }

        public void ToggleCity(bool isOn) {
            int i = TradeRoutePanel.Instance.OnCityToggle(City, toggle.isOn);
            if (i > 0)
                SelectAs(i);
            else
                Unselect();
        }

        public void OnWarehouseDestroy(Structure str, IWarfare destroyer) {
            City.RegisterStructureAdded(OnWarehouseBuild);
            CanvasGroup cg = GetComponent<CanvasGroup>();
            //cg.interactable = false;
            cg.alpha = 0.5f;
        }

        private void OnWarehouseBuild(Structure structure) {
            if (structure is WarehouseStructure == false)
                return;
            structure.RegisterOnDestroyCallback(OnWarehouseDestroy);
            SetPosition(structure.BuildTile);
            CanvasGroup cg = GetComponent<CanvasGroup>();
            //cg.interactable = true;
            cg.alpha = 1f;
        }
        void SetPosition(Tile t) {
            MapImage mi = FindObjectOfType<MapImage>();
            RectTransform rt = mi.mapParts.GetComponent<RectTransform>();
            Vector3 pos = new Vector3(t.X, t.Y, 0);
            Vector3 scale = new Vector3(rt.rect.width / World.Current.Width, rt.rect.height / World.Current.Height);
            pos.Scale(scale);
            transform.localPosition = pos;
        }
        public void SelectAs(int number) {
            Number.text = "" + number;
            toggle.isOn = true;
        }

        internal void Unselect() {
            Number.text = "";
            toggle.isOn = false;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (eventData.hovered.Contains(toggle.gameObject))
                return; // return when it is the toggle clicked
            OnWarehouseClick();
        }
    }
}