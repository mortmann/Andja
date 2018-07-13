using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OffWorldMarketUI : MonoBehaviour {

	public GameObject content;
	public GameObject itemPricePrefab;
	Dictionary<int,GameObject> idToGO;
	public OffWorldPanelUI panel;
	Dictionary<int,Item> items;
	// Use this for initialization
	void Start () {
//		foreach (Transform item in content.transform) {
//			Destroy (item.gameObject);
//		}
		idToGO = new Dictionary<int, GameObject> ();
		OffworldMarket ofm = WorldController.Instance.offworldMarket;
		items = BuildController.Instance.GetCopieOfAllItems (); 
		foreach (int i in ofm.itemIDtoBuyPrice.Keys) {
			GameObject g = GameObject.Instantiate (itemPricePrefab);
			g.GetComponent<PriceTagUI> ().Show (items[i],ofm.itemIDtoSellPrice[i],ofm.itemIDtoBuyPrice[i]);
			g.transform.SetParent (content.transform);
//			EventTrigger trigger = g.GetComponent<EventTrigger> ();
//			EventTrigger.Entry entry = new EventTrigger.Entry( );
//			entry.eventID = EventTriggerType.PointerClick;
			int t = i;
			g.GetComponent<PriceTagUI> ().AddListener ((data) => {OnClick(t);});
//			entry.callback.AddListener( (data) => {OnClick(t);});
//			trigger.triggers.Add( entry );
			idToGO.Add (i,g);
		}


	}


	void OnClick(int t){Debug.Log (t); 
		panel.OnOffWorldItemClick (items[t]);
	}
	// Update is called once per frame
	void Update () {
		//UPDATE PRICES IF THEY CHANGE HERE OR IN A CALLBACK
	}
}
