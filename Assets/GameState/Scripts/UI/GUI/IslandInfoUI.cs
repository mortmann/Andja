using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class IslandInfoUI : MonoBehaviour {
	CameraController cc;
	Text fertilityText;
	CanvasGroup cg;
	// Use this for initialization
	void Start () {
		cc = CameraController.Instance;
		cg = GetComponent<CanvasGroup> ();
		fertilityText = GetComponentInChildren<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		if(cc.nearestIsland==null){
			fertilityText.text="| |";
			cg.alpha = 0;
			return;
		}
		cg.alpha = 1;
		string text="| ";
		foreach (Fertility item in cc.nearestIsland.myFertilities) {
			text+=item.Name+" | ";

		}
		City c = cc.nearestIsland.myCities.Find (x => x.playerNumber == PlayerController.currentPlayerNumber);
		if(c !=null){
			int count=0;
			foreach (int item in c.citizienCount) {
				count += item;
			}

			text += count+"P";
			text += " | " + c.Balance+"$";
			text += "\n";
			Item[] items = c.inventory.GetBuildMaterial ();
			for (int i = 0; i < items.Length; i++) {
				if(items[i]==null){
					continue;				
				}

				text += " | " + items[i].name+"="+items[i].count;	
			}
		}
		fertilityText.text = text;
	}
}
