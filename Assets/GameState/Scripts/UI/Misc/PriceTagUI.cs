using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class PriceTagUI : MonoBehaviour {

	public Text sellText;
	public Text buyText;
	public ItemUI itemUI;

	public void Show(Item item, int sell, int buy){
		item.count = 1;
		itemUI.SetItem (item,1);
		UpdatePrice (sell , buy );
	}
	public void UpdatePrice(int sell, int buy){
		sellText.text = "+" + sell;
		buyText.text  = "-" + buy;
	}
	public void AddListener(UnityAction<BaseEventData> ueb){
		EventTrigger trigger = GetComponentInChildren<EventTrigger> ();
		EventTrigger.Entry entry = new EventTrigger.Entry( );
		entry.eventID = EventTriggerType.PointerClick;

		entry.callback.AddListener( ueb );
		trigger.triggers.Add( entry );
	}
}
