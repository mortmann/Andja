using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OffWorldMarketUI : MonoBehaviour {

    public GameObject content;
    public GameObject itemPricePrefab;
    Dictionary<string, GameObject> idToGO;
    public OffWorldPanelUI panel;
    Dictionary<string, Item> items;
    // Use this for initialization
    void Start() {
        OffworldMarket ofm = WorldController.Instance.offworldMarket;
        items = BuildController.Instance.GetCopieOfAllItems();
        idToGO = new Dictionary<string, GameObject>();
        foreach (Transform t in content.transform) {
            Destroy(t.gameObject);
        }
        foreach (string i in ofm.itemIDtoPrice.Keys) {
            GameObject g = GameObject.Instantiate(itemPricePrefab);
            g.GetComponent<PriceTagUI>().Show(items[i], ofm.itemIDtoPrice[i]);
            g.transform.SetParent(content.transform, false);
            string t = i;
            g.GetComponent<PriceTagUI>().AddListener((data) => { OnClick(t); });
            idToGO.Add(i, g);
        }
    }


    void OnClick(string t) {
        panel.OnOffWorldItemClick(items[t]);
    }
    // Update is called once per frame
    void Update() {
    }
}
