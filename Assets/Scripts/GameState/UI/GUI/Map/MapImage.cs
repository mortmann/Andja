using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
//using UnityEditor;
public class MapImage : MonoBehaviour {
    public GameObject mapCitySelectPrefab;
    public GameObject mapShipIconPrefab;
    public GameObject cameraRectPrefab;

    public Image image;
    public GameObject mapParts;
    GameObject cameraRect;
    CameraController cc;
    Texture2D tex;
    //for tradingroute
    public Dictionary<WarehouseStructure, GameObject> warehouseToGO;
    public Dictionary<Unit, GameObject> unitToGO;
    public GameObject tradingMenu;
    TradeRoutePanel tradeRoutePanel;
    // Use this for initialization
    void OnEnable() {
        cc = GameObject.FindObjectOfType<CameraController>();
        warehouseToGO = new Dictionary<WarehouseStructure, GameObject>();
        unitToGO = new Dictionary<Unit, GameObject>();
        World w = World.Current;
        //tex = new Texture2D(w.Width, w.Height);
        //Color[] p = tex.GetPixels();
        //int pixel = p.Length - 1;
        //for (int x = 0; x < w.Width; x++) {
        //    for (int y = 0; y < w.Height; y++) {
        //        if (w.GetTileAt(x, y).Type == TileType.Ocean) {
        //            p[y * w.Width + x] = Color.blue;
        //        }
        //        else {
        //            p[y * w.Width + x] = Color.green;
        //        }
        //        pixel--;
        //    }
        //}


        //tex.SetPixels(p);
        //tex.Apply();
        //Sprite s = Sprite.Create(tex, new Rect(0, 0, w.Width, w.Height), new Vector2(100, 100));

        //image.sprite = s;

        //cameraRect = Instantiate(cameraRectPrefab);
        //cameraRect.name = "CameraRect";
        //cameraRect.transform.SetParent(mapParts.transform);
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
    }
    public void Show() {
        //do smth when it gets shown
    }
    public void OnCityCreated(City c) {
        if (c == null || c.playerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        c.RegisterStructureAdded(OnWarehouseBuild);
        OnWarehouseBuild(c.warehouse);
    }

    private void OnWarehouseBuild(Structure structure) {
        if (structure is WarehouseStructure == false) {
            return;
        }
        WarehouseStructure warehouse = (WarehouseStructure)structure;
        RectTransform rt = mapParts.GetComponent<RectTransform>();
        World w = World.Current;
        GameObject g = GameObject.Instantiate(mapCitySelectPrefab);
        g.transform.SetParent(mapParts.transform);
        Vector3 pos = new Vector3(warehouse.BuildTile.X, warehouse.BuildTile.Y, 0);
        pos.Scale(new Vector3(rt.rect.width / w.Width, rt.rect.height / w.Height));
        g.transform.localPosition = pos;
        g.GetComponentInChildren<Text>().text = warehouse.City.Name;
        EventTrigger trigger = g.GetComponentInChildren<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) => { OnWarehouseClick(warehouse.City); });
        trigger.triggers.Add(entry);
        g.GetComponentInChildren<Toggle>().onValueChanged.AddListener((data) => { ToggleWarehouse(warehouse); });

        warehouse.RegisterOnDestroyCallback(OnWarehouseDestroy);
        warehouseToGO.Add(warehouse, g);
        warehouse.City.UnregisterStructureAdded(OnWarehouseBuild);
    }

    public void OnWarehouseClick(City c) {
        tradeRoutePanel.OnWarehouseClick(c);
    }

    public void ToggleWarehouse(WarehouseStructure warehouse) {
        Toggle t = warehouseToGO[warehouse].GetComponentInChildren<Toggle>();
        tradeRoutePanel.OnWarehouseToggleClicked(warehouse, t);
    }
    public void OnWarehouseDestroy(Structure str) {
        if (str is WarehouseStructure == false) {
            Debug.LogError("MapImage OnWarehouseDestroy-" + str + " is no Warehouse");
            return;
        }
        WarehouseStructure w = (WarehouseStructure)str;
        w.City.RegisterStructureAdded(OnWarehouseBuild);
        GameObject.Destroy(warehouseToGO[w]);
        warehouseToGO.Remove(w);
        //TODO UPDATE ALL TRADE_ROUTES
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
        g.transform.SetParent(mapParts.transform);
        Vector3 pos = new Vector3(u.X, u.Y, 0);
        pos.Scale(new Vector3(rt.rect.width / w.Width, rt.rect.height / w.Height));
        g.transform.localPosition = pos;
        unitToGO.Add(u, g);
    }

    // Update is called once per frame
    void Update() {
        World w = World.Current;
        //if something changes reset it 
        RectTransform rt = mapParts.GetComponent<RectTransform>();
        //cameraRect.transform.localPosition = cc.middle * rt.rect.width / w.Width;
        Vector3 vec = cc.upper - cc.lower;
        vec /= cc.zoomLevel; // Mathf.Clamp(cc.zoomLevel,CameraController.MaxZoomLevel,cc.zoomLevel);
        //cameraRect.transform.localScale = vec * (cc.zoomLevel / CameraController.MaxZoomLevel) * (rt.rect.width / w.Width);
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
