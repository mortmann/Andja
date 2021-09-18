using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class MapImage : MonoBehaviour {
        public GameObject mapCitySelectPrefab;
        public GameObject mapShipIconPrefab;
        public Image image;
        public GameObject mapParts;
        //for tradingroute
        public Dictionary<City, MapCitySelect> cityToMapSelect;

        public Dictionary<Unit, GameObject> unitToGO;
        public GameObject tradingMenu;
        private TradeRoutePanel tradeRoutePanel;

        private void Start() {
            cityToMapSelect = new Dictionary<City, MapCitySelect>();
            unitToGO = new Dictionary<Unit, GameObject>();
            World w = World.Current;

            tradeRoutePanel = tradingMenu.GetComponent<TradeRoutePanel>();
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
            foreach (Island item in w.Islands) {
                foreach (City c in item.Cities) {
                    if (c.IsWilderness()) {
                        continue;
                    }
                    OnCityCreated(c);
                }
            }
            w.RegisterUnitCreated(OnUnitCreated);
            Ship sh = null;
            foreach (Unit item in w.Units) {
                OnUnitCreated(item);
                if (item is Ship && sh == null)
                    sh = (Ship)item;
            }

            tradeRoutePanel.Initialize(this);
        }

        public void OnCityCreated(City c) {
            if (c == null || c.PlayerNumber != PlayerController.currentPlayerNumber) {
                return;
            }
            GameObject g = Instantiate(mapCitySelectPrefab);
            g.transform.SetParent(mapParts.transform, false);
            MapCitySelect mcs = g.GetComponent<MapCitySelect>();
            mcs.Setup(c);
            cityToMapSelect[c] = mcs;
        }

        public void OnUnitCreated(Unit u) {
            if (unitToGO.ContainsKey(u) || u == null || u.playerNumber != PlayerController.currentPlayerNumber) {
                return;
            }
            if (u.IsShip == false)
                return;
            RectTransform rt = mapParts.GetComponent<RectTransform>();
            World w = World.Current;

            GameObject g = GameObject.Instantiate(mapShipIconPrefab);
            g.transform.SetParent(mapParts.transform, false);
            Vector3 pos = new Vector3(u.X, u.Y, 0);
            pos.Scale(new Vector3(rt.rect.width / w.Width, rt.rect.height / w.Height));
            g.transform.localPosition = pos;
            unitToGO.Add(u, g);
        }

        // Update is called once per frame
        private void Update() {
            World w = World.Current;
            //if something changes reset it
            RectTransform rt = mapParts.GetComponent<RectTransform>();
            foreach (Unit item in w.Units) {
                if (item.IsShip == false) {
                    continue;
                }
                if (unitToGO.ContainsKey(item) == false) {
                    OnUnitCreated(item);
                    continue;
                }
                Vector3 pos = new Vector3(item.X, item.Y, 0);

                pos.Scale(new Vector3(rt.rect.width / w.Width, rt.rect.height / w.Height));
                unitToGO[item].transform.localPosition = pos;
            }
        }

    }
}