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
        //		foreach (Transform item in content.transform) {
        //			Destroy (item.gameObject);
        //		}
        idToGO = new Dictionary<string, GameObject>();
        OffworldMarket ofm = WorldController.Instance.offworldMarket;
        items = BuildController.Instance.GetCopieOfAllItems();
        foreach (string i in ofm.itemIDtoPrice.Keys) {
            GameObject g = GameObject.Instantiate(itemPricePrefab);
            g.GetComponent<PriceTagUI>().Show(items[i], ofm.itemIDtoPrice[i]);
            g.transform.SetParent(content.transform);
            //			EventTrigger trigger = g.GetComponent<EventTrigger> ();
            //			EventTrigger.Entry entry = new EventTrigger.Entry( );
            //			entry.eventID = EventTriggerType.PointerClick;
            string t = i;
            g.GetComponent<PriceTagUI>().AddListener((data) => { OnClick(t); });
            //			entry.callback.AddListener( (data) => {OnClick(t);});
            //			trigger.triggers.Add( entry );
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
