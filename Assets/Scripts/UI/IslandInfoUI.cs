using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class IslandInfoUI : MonoBehaviour {
	CameraController cc;
	Text fertilityText;
	CanvasGroup cg;
	PlayerController pc;
	// Use this for initialization
	void Start () {
		cc = GameObject.FindObjectOfType<CameraController> ();
		pc = GameObject.FindObjectOfType<PlayerController> ();
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
			text+=item.name+" | ";
		}
		City c = cc.nearestIsland.myCities.Find (x => x.playerNumber == pc.number);
		if(c !=null){
			int count=0;
			foreach (int item in c.citizienCount) {
				count += item;
			}

			text += count+"P";
			text += " | " + c.cityBalance+"$";
		}
		fertilityText.text = text;
	}
}
