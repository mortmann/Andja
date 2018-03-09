using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ConsoleUI : MonoBehaviour {

	public GameObject TextPrefab;
	public Transform outputTransform;
	public InputField inputField;
	public bool cheats_enabled;
	// Use this for initialization
	void Start () {
		WriteToConsole("StartUP");
		inputField.Select ();
	}

	void OnEnable(){
		inputField.Select ();
	}
	// Update is called once per frame
	void Update () {
		
	}
	public void WriteToConsole(string text){
		GameObject go = Instantiate (TextPrefab);
		go.GetComponent<Text> ().text = text;
		go.GetComponent<LayoutElement> ().minWidth = outputTransform.GetComponent<RectTransform> ().rect.width;
		go.transform.SetParent (outputTransform);
		if(outputTransform.childCount>30){
			Destroy(outputTransform.GetChild(30).gameObject);
		}
	}

	public void ReadFromConsole(){
		string command = inputField.text;
		if(command.Trim().Length<=0){
			return;
		}
		string[] parameters = command.Split (' ');
		if(parameters.Length<1){
			return;
		}
		if(cheats_enabled==false){
			return;
		}
		bool happend = false;
		switch(parameters[0]){
		case "city":
			happend = HandleCityCommands (parameters.Skip(1).ToArray());
			break;
		case "unit":
			happend = HandleUnitCommands (parameters.Skip(1).ToArray());
			break;
		case "island":
			break;
		case "event":
			break;
		case "camera":
			int num = 0;
			happend = int.TryParse (parameters [1], out num);
			bool turn = num == 1;
			CameraController.Instance.devCameraZoom = turn;
			break;
		default:
			break;
		}
		if(happend){
			WriteToConsole (command + "! Command succesful executed!");
		} else {
			WriteToConsole (command + "! Command execution failed!");
		}
		inputField.text="";
	}

	bool HandleCityCommands(string[] parameters){
		if(parameters.Length<1){
			return false;
		}
		int player = -1000;
		int pos = 0;
		// anything can thats not a number can be the current player
		if(int.TryParse(parameters[pos],out player)==false){ 
			player = PlayerController.currentPlayerNumber;
		} else {
			pos++;
		}
		if(player<0){ // do we want to be able to console access to wilderness
			return false;
		}
		City c = CameraController.Instance.nearestIsland.FindCityByPlayer (player);
		if(c==null){
			return false;
		}
		switch(parameters[pos]){
		case "item":
			return changeItemInInventory (parameters.Skip (2).ToArray (),c.inventory);
		case "fillitup":
			return addAllItems (c.inventory);
		case "build":
			return addAllItems (c.inventory,true);
		case "name":
			break;
		case "player":
			break;
		case "event":
			break;
		default:
			break;
		}
		return false;
	}
	bool HandleUnitCommands(string[] parameters){
		if(parameters.Length<1){
			return false;
		}
		int player = -1000;
		int pos = 0;
		// anything can thats not a number can be the current player
		if(int.TryParse(parameters[pos],out player)==false){ 
			player = PlayerController.currentPlayerNumber;
		} else {
			pos++;
		}
		if(player<0){ // do we want to be able to console access to wilderness
			Debug.Log ("player<0");
			return false;
		}
		Unit u = MouseController.Instance.SelectedUnit;
		if(u==null){
			Debug.Log ("no unit selected");
			return false;
		}
		switch(parameters[pos]){
		case "item":
			return changeItemInInventory (parameters.Skip (1).ToArray (),u.inventory);
		case "build":
			u.inventory.addItem (new Item(1,50));
			u.inventory.addItem (new Item(2,50));
			return true;
		case "name":
			break;
		case "player":
			break;
		case "event":
			break;
		default:
			break;
		}
		return false;
	}
	public bool changeItemInInventory(string[] parameters, Inventory inv){
		int id = -1;
		int amount = 0; // amount can be plus for add or negative for remove
		if(parameters.Length!=2){
			return false;
		}
		if(int.TryParse(parameters[0],out id)==false){
			
			return false;
		}
		if(int.TryParse(parameters[1],out amount)==false){
			
			return false;
		}
		Debug.Log (id + " add " + amount);
		if(id<PrototypController.StartID){
			return false;
		}
		Item i = new Item (id, Mathf.Abs(amount));
		if(amount>0){
			inv.addItem (i);
		} else {
			inv.removeItemAmount (i);
		}
		return true;
	}

	private bool addAllItems(Inventory inv, bool onlyBuildItems=false){
		foreach(Item i in inv.items.Values){
			if(onlyBuildItems){
				if(i.Type!=ItemType.Build){
					continue;
				}
			}
			inv.addItem (new Item (i.ID, int.MaxValue));
		}
		return true;
	}
}
